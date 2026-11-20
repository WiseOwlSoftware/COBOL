// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// The diagnostics section (DESIGN-listing.md section 6.3): every diagnostic the compilation reported of its source, each placed
/// on the listing line it names through the one index (<see cref="ListingLineIndex.Place"/>), its column converted to a position
/// of the line's listed text, and its citations read from its message by the one reader (<see cref="ListingCitation.Scan"/>).
/// </summary>
/// <remarks>A diagnostic names its file as the compilation read it (a front-end or bind diagnostic) or by its full path (a backend's
/// diagnostic on a line mapped to a COBOL statement, DESIGN-listing.md section 6.9); either finds its listed file, the second through
/// the listing's files by full path (<c>ListingBuilder</c>). A diagnostic whose location is no file the listing lists (a diagnostic about
/// the compilation as a whole; a backend diagnostic of code no statement owns) is placed on no line and names no file, so the listing
/// never names generated source; the summary lists it. A diagnostic on a line the listing does not print (one suppressed by a LISTING
/// directive) keeps its line: the summary names it, and the source section prints only the lines it prints.</remarks>
internal static class DiagnosticSectionBuilder
{
    /// <summary>The diagnostics <paramref name="reported"/>, in listing order (placed ones by line and column, then the rest in
    /// report order), each id its 1-based place in that order. The builder writes each line's ids to its draft
    /// (<see cref="LineDraft.AddDiagnostic"/>) when the diagnostics section is asked for.</summary>
    public static List<ListingDiagnostic> Build(IReadOnlyList<Diagnostic> reported, FrontendFacts frontend, ListingLineIndex index,
        IReadOnlyDictionary<string, int> fileIds, IReadOnlyDictionary<string, string> filesByFullPath, IReadOnlyList<LineDraft> drafts)
    {
        var placed = new List<(int? Line, int Column, int Order, Diagnostic Diagnostic)>(reported.Count);
        for (int order = 0; order < reported.Count; order++)
        {
            var d = reported[order];
            var at = d.Location is { } location ? Listed(location, fileIds, filesByFullPath) : null;
            int? line = at is { } listed ? index.Place(listed) : null;
            int column = line is not null && frontend.Formats.TryGetValue(at!.Value.FileName, out var map)
                ? map.CharacterPositionAt(at.Value.Line + 1, at.Value.Column)
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
                draft is null ? null : column, ListingCitation.Scan(d.Message), FlagOf(d.Flag)));
        }
        return diagnostics;
    }

    /// <summary>The table of flagged constructs by edition (DESIGN-listing.md section 6.11): the edition flags of
    /// <paramref name="diagnostics"/> as the compiler raised them (<see cref="ListingDiagnostic.Flag"/>), never re-derived from a code or
    /// a message, one entry per edition, kind and construct, in that order, each naming its diagnostics in listing order.</summary>
    public static List<ListingFlaggedConstruct> FlaggedConstructs(IReadOnlyList<ListingDiagnostic> diagnostics) =>
        [.. diagnostics.Where(d => d.Flag is not null)
            .GroupBy(d => (d.Flag!.Edition, d.Flag.Kind, d.Flag.Construct))
            .OrderBy(g => g.Key.Edition).ThenBy(g => g.Key.Kind).ThenBy(g => g.Key.Construct, StringComparer.Ordinal)
            .Select(g => new ListingFlaggedConstruct(g.Key.Edition, g.Key.Kind, g.Key.Construct, g.First().Flag!.Description,
                [.. g.Select(d => d.Id)]))];

    /// <summary>The listing's edition flag of a compiler's: every kind by its own name (the Unit assembly's
    /// <c>DiagnosticRecordsTests.TheFlagKindMap_IsTotal</c> holds the map total).</summary>
    internal static ListingFlag? FlagOf(EditionFlag? flag) => flag is null ? null : new ListingFlag(flag.Kind switch
    {
        EditionFlagKind.Obsolete => ListingFlagKind.Obsolete,
        EditionFlagKind.Removed => ListingFlagKind.Removed,
        EditionFlagKind.Extension => ListingFlagKind.Extension,
        _ => throw new ArgumentOutOfRangeException(nameof(flag), flag.Kind, "an edition flag kind with no listing kind"),
    }, flag.Construct, flag.Description, flag.Edition);

    /// <summary><paramref name="at"/> naming its file as the listing lists it: as it stands when the listing lists that name, else by its
    /// full path; null when the listing lists no such file.</summary>
    private static SourceLocation? Listed(SourceLocation at, IReadOnlyDictionary<string, int> fileIds,
        IReadOnlyDictionary<string, string> filesByFullPath)
    {
        if (fileIds.ContainsKey(at.FileName)) return at;
        if (at.FileName.Length == 0 || !filesByFullPath.TryGetValue(Path.GetFullPath(at.FileName), out string? listed)) return null;
        return new SourceLocation(listed, at.Position, at.Line, at.Column);
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
