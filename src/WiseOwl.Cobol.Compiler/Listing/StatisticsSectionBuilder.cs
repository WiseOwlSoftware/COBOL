// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>
/// The statistics section (DESIGN-listing.md section 6.8): the lines by kind and by file, counted from the finished lines; the
/// diagnostics by severity, counted from the diagnostics the compilation reported; and, when the source was parsed, the programs,
/// the statements and each verb with every listing line a statement of it begins on, read off the parse tree
/// (<see cref="SourceUnitFacts.Statements"/>) and placed through the one index (<see cref="ListingLineIndex"/>). It counts every
/// line of the listing whether or not the source section is asked for, and holds no timing.
/// </summary>
internal static class StatisticsSectionBuilder
{
    public static ListingStatistics Build(IReadOnlyList<ListingLine> lines, IReadOnlyList<ListingFile> files,
        IReadOnlyList<ListingDiagnostic> diagnostics, IReadOnlyList<ListingProgram> programs, SourceUnitFacts? units,
        ListingLineIndex index)
    {
        var byFile = lines.ToLookup(l => l.File);
        return new ListingStatistics(
            Count(lines),
            [.. files.Select(f => new ListingFileStatistics(f.Id, Count(byFile[f.Id])))],
            [.. Enum.GetValues<ListingSeverity>().Select(s => new ListingSeverityCount(s, diagnostics.Count(d => d.Severity == s)))],
            units is null ? null : new ListingParseStatistics(programs.Count, units.Statements.Count, Verbs(units.Statements, index)));
    }

    /// <summary>The counts of <paramref name="lines"/>: every kind once, in the order of <see cref="ListingLineKind"/>.</summary>
    internal static ListingLineCounts Count(IEnumerable<ListingLine> lines)
    {
        var kinds = Enum.GetValues<ListingLineKind>();
        var perKind = new Dictionary<ListingLineKind, int>(kinds.Length);
        int total = 0, omitted = 0, copied = 0, unlisted = 0;
        foreach (var line in lines)
        {
            total++;
            perKind[line.Kind] = perKind.GetValueOrDefault(line.Kind) + 1;
            if (line.Omitted) omitted++;
            if (line.Inclusion is not null) copied++;
            if (!line.Listed) unlisted++;
        }
        return new ListingLineCounts(total, [.. kinds.Select(k => new ListingKindCount(k, perKind.GetValueOrDefault(k)))],
            omitted, copied, unlisted);
    }

    /// <summary>Each verb of <paramref name="statements"/> in the ordinal order of its word, its count and the listing line each of
    /// its statements begins on, in listing order; a statement on a resultant line no listing line holds counts and names no line.</summary>
    private static List<ListingVerb> Verbs(IReadOnlyList<SourceStatement> statements, ListingLineIndex index) =>
        [.. statements.GroupBy(s => s.Verb, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ListingVerb(g.Key, g.Count(), [.. g.Select(s => index.Of(s.At.Line)).OfType<int>().Order()]))];
}
