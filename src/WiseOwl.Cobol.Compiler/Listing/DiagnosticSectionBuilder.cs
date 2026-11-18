// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// The diagnostics section (DESIGN-listing.md section 6.3): every diagnostic the compilation reported of its source, each placed
/// on the listing line it names through the one index (<see cref="ListingLineIndex.Place"/>), its column converted to a position
/// of the line's listed text, and its citations read from its message by the one reader (<see cref="ListingCitation.Scan"/>).
/// </summary>
/// <remarks>A diagnostic whose location is no file the listing lists (a diagnostic about the compilation as a whole; before step 8
/// of the design, a backend diagnostic of the generated C#) is placed on no line and names no file, so the listing never names
/// generated source; the summary lists it. A diagnostic on a line the listing does not print (one suppressed by a LISTING
/// directive) keeps its line: the summary names it, and the source section prints only the lines it prints.</remarks>
internal static class DiagnosticSectionBuilder
{
    /// <summary>The diagnostics of <paramref name="facts"/>, in listing order (placed ones by line and column, then the rest in
    /// report order), each id its 1-based place in that order; and each line's ids written to its draft.</summary>
    public static List<ListingDiagnostic> Build(IReadOnlyList<Diagnostic> reported, FrontendFacts frontend, ListingLineIndex index,
        IReadOnlyDictionary<string, int> fileIds, IReadOnlyList<LineDraft> drafts)
    {
        var placed = new List<(int? Line, int Column, int Order, Diagnostic Diagnostic)>(reported.Count);
        for (int order = 0; order < reported.Count; order++)
        {
            var d = reported[order];
            int? line = d.Location is { } at && fileIds.ContainsKey(at.FileName) ? index.Place(at) : null;
            int column = line is not null && frontend.Formats.TryGetValue(d.Location!.Value.FileName, out var map)
                ? map.ExpandedIndexAt(d.Location.Value.Line + 1, d.Location.Value.Column) + 1
                : (d.Location?.Column ?? 0) + 1;
            placed.Add((line, column, order, d));
        }
        var ordered = placed.OrderBy(p => p.Line ?? int.MaxValue).ThenBy(p => p.Line is null ? 0 : p.Column).ThenBy(p => p.Order).ToList();
        var diagnostics = new List<ListingDiagnostic>(ordered.Count);
        foreach (var (line, column, _, d) in ordered)
        {
            int id = diagnostics.Count + 1;
            var draft = line is { } number ? drafts[number - 1] : null;
            diagnostics.Add(new ListingDiagnostic(id, d.Code, SeverityOf(d.Severity), d.Message, line, draft?.File, draft?.FileLine,
                draft is null ? null : column, ListingCitation.Scan(d.Message)));
            draft?.AddDiagnostic(id);
        }
        return diagnostics;
    }

    /// <summary>The listing's severity of a compiler severity: every member by its own name (the Unit assembly's
    /// <c>DiagnosticRecordsTests.TheSeverityMap_IsTotal</c> holds the map total).</summary>
    internal static ListingSeverity SeverityOf(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => ListingSeverity.Error,
        DiagnosticSeverity.Warning => ListingSeverity.Warning,
        DiagnosticSeverity.Info => ListingSeverity.Info,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "a diagnostic severity with no listing severity"),
    };
}
