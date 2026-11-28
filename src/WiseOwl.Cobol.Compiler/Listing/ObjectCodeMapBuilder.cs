// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>
/// The object-code map's builder (DESIGN-listing.md section 6.9): each sequence point the assembly's PDB holds is placed on the listing
/// line of the physical position it names, through the one index (<see cref="ListingLineIndex"/>), and becomes a
/// <see cref="ListingObjectRange"/>. A point in no file of the listing (the text of an expansion compiled from the repository) is placed
/// on no line and left out.
/// </summary>
/// <remarks>⛔ A COPYBOOK COPIED TWICE (section 6.9): its statements are two statements at one physical position, and a PDB names only the
/// position. A statement compiles to one first sequence point, spanning the statement, and a later one for each stretch of its own code
/// after a statement nested in it, spanning its verb (<c>CodeWriter.MapStatement</c>); a statement whose code begins with a store an
/// optimized build may drop can hold two first points, one after the other. The first points of one position are paired, in metadata and
/// IL order (the order the statements were compiled in, which is their order in the text), with the listing lines that show that position,
/// in listing order, an equal share to each line when they divide evenly (one to one, or two to one), and each later point goes with the
/// first point before it. When they do not divide evenly (a statement compiled more than once, or a copy whose statement compiled to no
/// code), the k-th first point goes to the k-th line and every later one to the last.</remarks>
internal static class ObjectCodeMapBuilder
{
    /// <summary>The map of <paramref name="facts"/>, read from the assembly at <paramref name="assembly"/>.</summary>
    public static ListingObjectCodeMap Build(ObjectCodeFacts facts, string assembly, ListingLineIndex index, FrontendFacts frontend,
        IReadOnlyDictionary<string, string> filesByFullPath, IReadOnlyList<TextInclusion> inclusions)
    {
        // Each file of the listing by its full path (what the reader resolves a PDB's document name to, SourceDocuments.PathOf), with the
        // texts that list it: the source (inclusion null) and each inclusion of it, in listing order.
        var texts = new Dictionary<string, (string File, List<int?> Inclusions)>(StringComparer.Ordinal);
        foreach (var (full, file) in filesByFullPath)
            texts[full] = (file, Path.GetFullPath(frontend.SourcePath) == full ? [null] : []);
        foreach (var inclusion in inclusions.Where(i => i.File is not null))
            texts[Path.GetFullPath(inclusion.File!)].Inclusions.Add(inclusion.Id);

        var placed = new List<(ObjectSequencePoint Point, int Line, int Column)>();
        foreach (var position in facts.Points.GroupBy(p => (p.Document, p.Line, p.Column)))
        {
            if (!texts.TryGetValue(position.Key.Document, out var text)) continue;
            var lines = text.Inclusions.Select(i => index.Of(i, position.Key.Line)).OfType<int>().Order().ToList();
            if (lines.Count == 0) continue;
            int column = frontend.Formats.TryGetValue(text.File, out var map)
                ? map.CharacterPositionAt(position.Key.Line, position.Key.Column - 1)
                : position.Key.Column;
            var ordered = position.OrderBy(p => p.Method).ThenBy(p => p.Start).ToList();
            var copies = Copies([.. ordered.Select(p => (p.EndLine, p.EndColumn))], lines.Count);
            for (int i = 0; i < ordered.Count; i++) placed.Add((ordered[i], lines[copies[i]], column));
        }

        // The methods that hold a placed point, numbered in metadata order.
        var methodIds = new Dictionary<int, int>();
        foreach (int method in placed.Select(p => p.Point.Method).Distinct().Order()) methodIds[method] = methodIds.Count + 1;
        var methods = methodIds.Select(m => (Id: m.Value, Method: facts.Methods[m.Key]))
            .Select(m => new ListingMethod(m.Id, m.Method.Token, m.Method.Type, m.Method.Name, m.Method.CodeSize, m.Method.Regions)).ToList();
        var regions = RegionLines(facts, placed.Select(p => p.Point));
        var ranges = placed
            .OrderBy(p => p.Line).ThenBy(p => p.Column).ThenBy(p => p.Point.Method).ThenBy(p => p.Point.Start)
            .Select(p => (Placed: p, Lines: regions[p.Point]))
            .Select(p => new ListingObjectRange(p.Placed.Line, p.Placed.Column, methodIds[p.Placed.Point.Method], p.Placed.Point.Start,
                p.Placed.Point.End, p.Placed.Point.Instructions, p.Placed.Point.Instructions is null ? null : p.Lines.Marks, p.Lines.Depth))
            .ToList();
        return new ListingObjectCodeMap(assembly, methods, ranges);
    }

    /// <summary>The exception-handling lines each placed point's stretch prints, and the depth it begins at (kb/Work PB5951): each method's
    /// clauses laid out once (<see cref="ExceptionRegionLayout.Marks"/>) and shared among its placed stretches in IL order
    /// (<see cref="ExceptionRegionLayout.Place"/>), so a line of a stretch the map leaves out prints in a neighbour.</summary>
    private static Dictionary<ObjectSequencePoint, (IReadOnlyList<ListingRegionMark> Marks, int Depth)> RegionLines(ObjectCodeFacts facts,
        IEnumerable<ObjectSequencePoint> placed)
    {
        var lines = new Dictionary<ObjectSequencePoint, (IReadOnlyList<ListingRegionMark>, int)>(ReferenceEqualityComparer.Instance);
        foreach (var method in placed.GroupBy(p => p.Method))
        {
            var info = facts.Methods[method.Key];
            var stretches = method.OrderBy(p => p.Start).ToList();
            var (marks, depths) = ExceptionRegionLayout.Place([.. stretches.Select(p => (p.Start, p.End))],
                ExceptionRegionLayout.Marks(info.Regions, info.Type + "::" + info.Name), info.CodeSize);
            for (int i = 0; i < stretches.Count; i++) lines[stretches[i]] = (marks[i], depths[i]);
        }
        return lines;
    }

    /// <summary>Which of <paramref name="lines"/> listing lines of one position each of its points goes to, the points given by their ends
    /// in compiled order. A statement's first point spans the statement, its later ones (its code after a statement nested in it) only its
    /// verb: the first point of a position in compiled order is a first point, and so is every one spanning what it spans. A statement may
    /// hold two first points (its code begins with a store an optimized build may drop, <c>CodeWriter.MapLine</c>), and each copy of it
    /// compiles to the same code, so when the first points divide evenly among the lines each line takes an equal share in order; when
    /// they do not, the k-th first point goes to the k-th line and every later one to the last. Each later point goes with the first point
    /// before it.</summary>
    internal static int[] Copies(IReadOnlyList<(int EndLine, int EndColumn)> ends, int lines)
    {
        var first = ends[0];
        int firsts = ends.Count(e => e == first);
        int share = firsts % lines == 0 ? firsts / lines : 1;
        var copies = new int[ends.Count];
        int occurrence = -1;
        for (int i = 0; i < ends.Count; i++)
        {
            if (ends[i] == first) occurrence++;
            copies[i] = Math.Min(occurrence / share, lines - 1);
        }
        return copies;
    }
}
