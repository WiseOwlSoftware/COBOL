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

    /// <summary>Which of <paramref name="stretches"/> (one method's stretches, by their half-open IL ranges, disjoint, in offset order)
    /// prints each line of <paramref name="marks"/> (the method's lines in print order), and the depth each stretch begins at. A line
    /// prints in the stretch whose code it bounds: an OPENING line at offset X in the stretch holding X (<c>Start &lt;= X &lt; End</c>),
    /// before the instruction at X; a CLOSING line at X in the stretch holding the block's last instruction (<c>Start &lt; X &lt;= End</c>),
    /// after it. So a block that lies inside one stretch opens and closes in it, and a block ending where the next statement's code begins
    /// closes under the statement whose code it ends, never at the top of the next one (kb/Work PB5951 review round 1). A line no stretch
    /// owns that way (its code is before the method's first visible sequence point, or in a stretch the map does not place) prints in the
    /// next stretch, before its first instruction, or after the last stretch's last instruction when none follows. So every line prints
    /// once, in each stretch in the order <see cref="Marks"/> gave, and each stretch begins at the depth the lines of the stretches before
    /// it leave: the number of blocks that contain its start when the stretch before it ends there.</summary>
    /// <param name="codeSize">The size of the body, which bounds every offset.</param>
    public static (List<ListingRegionMark>[] Lines, int[] Depths) Place(IReadOnlyList<(int Start, int End)> stretches,
        IReadOnlyList<ListingRegionMark> marks, int codeSize)
    {
        var lines = new List<ListingRegionMark>[stretches.Count];
        for (int s = 0; s < stretches.Count; s++) lines[s] = [];
        int at = 0;
        foreach (var mark in marks)
        {
            if (mark.Offset > codeSize)
                throw new InvalidDataException($"a {mark.Boundary} line at IL offset {mark.Offset} lies past the body's end ({codeSize})");
            // The first stretch that does not end before the line's code: for an opening line, one ending after its offset; for a
            // closing line, one ending at or after it. Marks come in offset order, closings before openings at one offset, so the
            // walk only moves forward.
            bool opens = ListingRegionBoundaries.Opens(mark.Boundary);
            while (at < stretches.Count && (opens ? stretches[at].End <= mark.Offset : stretches[at].End < mark.Offset)) at++;
            lines[at < stretches.Count ? at : stretches.Count - 1].Add(mark);
        }
        var depths = new int[stretches.Count];
        for (int s = 1; s < stretches.Count; s++)
            depths[s] = depths[s - 1] + lines[s - 1].Sum(m => ListingRegionBoundaries.Opens(m.Boundary) ? 1 : -1);
        return (lines, depths);
    }
}
