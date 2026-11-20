// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ THE ONE LAYOUT OF A METHOD'S EXCEPTION-HANDLING STRUCTURE (DESIGN-listing.md section 6.9, kb/Work PB5951): the clauses the reader
/// read from the body become the lines ILDASM prints among the instructions (ECMA-335 II.19), each at its IL offset with its nesting
/// depth, and each stretch of the object-code map takes the lines it prints. No renderer orders, nests or places a region.
/// </summary>
/// <remarks>
/// <para>The BLOCKS of a body are its protected blocks (one per distinct try range: several clauses protecting the same block share it, as
/// a <c>catch</c> and a second <c>catch</c> of one C# <c>try</c> do), each filter clause's filter block (from its filter start to its
/// handler) and each clause's handler. ECMA-335 I.12.4.2.7 requires two blocks to be disjoint or one to contain the other, so the blocks
/// form a tree, and its lines are the order a walk of the tree meets them: at one offset every block ending there closes before any block
/// starting there opens, the innermost closing first and the outermost opening first. A body whose blocks overlap is no body this layout
/// can print, and fails loudly.</para>
/// </remarks>
internal static class ExceptionRegionLayout
{
    /// <summary>A block of the structure: the half-open IL range it covers (<c>From</c> up to <c>To</c>), the lines that open and close
    /// it, and its clause (1-based in the body's list).</summary>
    private readonly record struct Block(int From, int To, ListingRegionBoundary Open, ListingRegionBoundary Close, int Region);

    /// <summary>The lines of <paramref name="regions"/> (a body's clauses, in the body's order) in the order they print, each with its
    /// offset and depth. <paramref name="method"/> names the method in the failure of a body whose blocks overlap.</summary>
    public static IReadOnlyList<ListingRegionMark> Marks(IReadOnlyList<ListingExceptionRegion> regions, string method)
    {
        if (regions.Count == 0) return [];
        var blocks = new List<Block>();
        var tries = new HashSet<(int, int)>();
        for (int i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            // A protected block several clauses share is one block, named by the first of them.
            if (tries.Add((region.TryStart, region.TryEnd)))
                blocks.Add(new Block(region.TryStart, region.TryEnd, ListingRegionBoundary.TryStart, ListingRegionBoundary.TryEnd, i + 1));
            if (region.Kind == ListingRegionKind.Filter)
                blocks.Add(new Block(region.FilterStart ?? throw new InvalidDataException($"{method}: filter clause {i + 1} has no filter start"),
                    region.HandlerStart, ListingRegionBoundary.FilterStart, ListingRegionBoundary.FilterEnd, i + 1));
            blocks.Add(new Block(region.HandlerStart, region.HandlerEnd, ListingRegionBoundary.HandlerStart, ListingRegionBoundary.HandlerEnd,
                i + 1));
        }
        foreach (var block in blocks)
            if (block.From >= block.To)
                throw new InvalidDataException($"{method}: clause {block.Region}'s {block.Open} block covers no IL ({block.From} to {block.To})");

        // Opened in offset order, an outer block before a block it contains (the longer first; a clause nested in another comes first in
        // the body's list, so of two blocks of one range the later clause's is the outer).
        blocks.Sort((a, b) => a.From != b.From ? a.From.CompareTo(b.From)
            : a.To != b.To ? b.To.CompareTo(a.To)
            : b.Region.CompareTo(a.Region));
        var marks = new List<ListingRegionMark>(2 * blocks.Count);
        var open = new Stack<Block>();
        int next = 0;
        while (next < blocks.Count || open.Count > 0)
        {
            int nextStart = next < blocks.Count ? blocks[next].From : int.MaxValue;
            if (open.Count > 0 && open.Peek().To <= nextStart)
            {
                var closed = open.Pop();
                marks.Add(new ListingRegionMark(closed.To, closed.Close, closed.Region, open.Count));
                continue;
            }
            var block = blocks[next++];
            if (open.Count > 0 && block.To > open.Peek().To)
                throw new InvalidDataException($"{method}: clause {block.Region}'s {block.Open} block ({block.From} to {block.To}) overlaps "
                    + $"clause {open.Peek().Region}'s {open.Peek().Open} block ({open.Peek().From} to {open.Peek().To}) without being "
                    + "inside it (ECMA-335 I.12.4.2.7)");
            marks.Add(new ListingRegionMark(block.From, block.Open, block.Region, open.Count));
            open.Push(block);
        }
        return marks;
    }

    /// <summary>Which of <paramref name="stretches"/> (one method's stretches, by their IL range, in offset order) prints each line of
    /// <paramref name="marks"/> (the method's lines in print order), and the depth each stretch begins at. A line prints in the stretch
    /// whose range holds its offset, or that ends at the end of the body (<paramref name="codeSize"/>) when the line is there; a line in
    /// code no stretch holds (before the method's first visible sequence point, or a stretch the map does not place) prints in the next
    /// stretch, before its first instruction, or after the last stretch's last instruction when none follows. So every line prints once,
    /// and in each stretch in the order <see cref="Marks"/> gave.</summary>
    public static (List<ListingRegionMark>[] Lines, int[] Depths) Place(IReadOnlyList<(int Start, int End)> stretches,
        IReadOnlyList<ListingRegionMark> marks, int codeSize)
    {
        var lines = new List<ListingRegionMark>[stretches.Count];
        for (int s = 0; s < stretches.Count; s++) lines[s] = [];
        var first = new int[stretches.Count];   // each stretch's first line, as an index into marks: its own, or the next one after
        Array.Fill(first, -1);
        int at = 0;
        for (int m = 0; m < marks.Count; m++)
        {
            int offset = marks[m].Offset;
            while (at < stretches.Count && stretches[at].End <= offset && !(stretches[at].End == codeSize && offset == codeSize)) at++;
            int stretch = at < stretches.Count ? at : stretches.Count - 1;
            if (first[stretch] < 0) first[stretch] = m;
            lines[stretch].Add(marks[m]);
        }
        var depths = new int[stretches.Count];
        for (int s = 0; s < stretches.Count; s++)
        {
            // The depth before the stretch's first line: before its first region line, or before the first line of the method at or
            // after its start (the stretch prints none, so none lies inside it).
            int m = first[s] >= 0 ? first[s] : FirstAtOrAfter(marks, stretches[s].Start);
            depths[s] = m < marks.Count ? DepthBefore(marks[m]) : 0;
        }
        return (lines, depths);
    }

    /// <summary>The depth in effect just before <paramref name="mark"/> prints: its own depth before an opening line, one deeper before a
    /// closing one.</summary>
    public static int DepthBefore(ListingRegionMark mark) => ListingRegionBoundaries.Opens(mark.Boundary) ? mark.Depth : mark.Depth + 1;

    private static int FirstAtOrAfter(IReadOnlyList<ListingRegionMark> marks, int offset)
    {
        int m = 0;
        while (m < marks.Count && marks[m].Offset < offset) m++;
        return m;
    }
}
