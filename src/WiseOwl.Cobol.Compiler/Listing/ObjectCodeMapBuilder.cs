// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// The object-code map's builder (DESIGN-listing.md section 6.9): each sequence point the assembly's PDB holds is placed on the listing
/// line of the physical position it names, through the one index (<see cref="ListingLineIndex"/>), and becomes a
/// <see cref="ListingObjectRange"/>. A point in no file of the listing (the text of an expansion compiled from the repository) is placed
/// on no line and left out.
/// </summary>
/// <remarks>⛔ A COPYBOOK COPIED TWICE (section 6.9): its statements are two statements at one physical position, and a PDB names only the
/// position. A statement compiles to one first sequence point, spanning the statement, and a later one for each stretch of its own code
/// after a statement nested in it, spanning its verb (<c>CodeWriter.MapStatement</c>). The first points of one position are paired, in
/// metadata and IL order (the order the statements were compiled in, which is their order in the text), with the listing lines that show
/// that position, in listing order, one to one when the counts agree, and each later point goes with the first point before it. When the
/// counts do not agree (a statement compiled more than once, or a copy whose statement compiled to no code), the k-th first point goes to
/// the k-th line and every later one to the last.</remarks>
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
                ? map.ExpandedIndexAt(position.Key.Line, position.Key.Column - 1) + 1
                : position.Key.Column;
            // A statement's first point spans the statement, its later ones (its code after a statement nested in it) only its verb: the
            // first point of a position in compiled order is a first point, and so is every one spanning what it spans.
            var ordered = position.OrderBy(p => p.Method).ThenBy(p => p.Start).ToList();
            var first = (ordered[0].EndLine, ordered[0].EndColumn);
            int occurrence = -1;
            foreach (var point in ordered)
            {
                if ((point.EndLine, point.EndColumn) == first) occurrence++;
                placed.Add((point, lines[Math.Min(occurrence, lines.Count - 1)], column));
            }
        }

        // The methods that hold a placed point, numbered in metadata order.
        var methodIds = new Dictionary<int, int>();
        foreach (int method in placed.Select(p => p.Point.Method).Distinct().Order()) methodIds[method] = methodIds.Count + 1;
        var methods = methodIds.Select(m => (Id: m.Value, Method: facts.Methods[m.Key]))
            .Select(m => new ListingMethod(m.Id, m.Method.Token, m.Method.Type, m.Method.Name, m.Method.CodeSize)).ToList();
        var ranges = placed
            .OrderBy(p => p.Line).ThenBy(p => p.Column).ThenBy(p => p.Point.Method).ThenBy(p => p.Point.Start)
            .Select(p => new ListingObjectRange(p.Line, p.Column, methodIds[p.Point.Method], p.Point.Start, p.Point.End, p.Point.Instructions))
            .ToList();
        return new ListingObjectCodeMap(assembly, methods, ranges);
    }
}
