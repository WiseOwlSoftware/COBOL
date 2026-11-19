// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>The statistics section of the text listing (DESIGN-listing.md section 6.8): the line counts, the programs,
/// statements and diagnostics, and the verbs.</summary>
public static class StatisticsText
{
    /// <summary>The statistics section's rows (DESIGN-listing.md section 6.8): the lines by kind, then each file's lines, then
    /// the programs, statements and diagnostics by severity, then each verb with its count and its listing lines (IBM's
    /// VBREF). A count shows only the line kinds that have lines; the model holds every kind. After a parse that recovered from a
    /// syntax error (<paramref name="parseOutcome"/>) the statement count says it holds only the statements the recovery kept.</summary>
    public static IEnumerable<string> Rows(ListingStatistics statistics, IReadOnlyList<ListingFile> files, ListingParse parseOutcome, int width)
    {
        yield return TextListingRenderer.Columns(("LINES", StatisticNameWidth)) + CountText(statistics.Lines.Lines);
        foreach (var (name, count) in CountsOf(statistics.Lines)) yield return TextListingRenderer.Columns(("  " + name, StatisticNameWidth)) + CountText(count);
        yield return "";
        yield return "FILES";
        var paths = files.ToDictionary(f => f.Id, f => f.Path);
        const int Detail = 6 + 2 + CountWidth + 2;   // the column a file's path and its counts start at
        foreach (var file in statistics.Files)
        {
            foreach (string row in TextListingRenderer.Indented(string.Create(CultureInfo.InvariantCulture,
                         $"{file.File,6}  {CountText(file.Lines.Lines)}  {paths[file.File]}"), 0, Detail, width))
                yield return row;
            foreach (string row in TextListingRenderer.Indented(string.Join(", ", CountsOf(file.Lines).Select(c => string.Create(CultureInfo.InvariantCulture,
                         $"{c.Name} {c.Count}"))), Detail, Detail, width))
                yield return row;
        }
        yield return "";
        if (statistics.Parse is { } parseCounts)
        {
            yield return TextListingRenderer.Columns(("PROGRAMS", StatisticNameWidth)) + CountText(parseCounts.Programs);
            yield return TextListingRenderer.Columns(("STATEMENTS", StatisticNameWidth)) + CountText(parseCounts.Statements);
            if (parseOutcome is ListingParse.Recovered)
                foreach (string row in TextListingRenderer.Indented(TextListingRenderer.RecoveredNote("the programs, statements and verbs"), 2, 2, width))
                    yield return row;
        }
        else yield return "PROGRAMS AND STATEMENTS: not counted, the source was not parsed";
        yield return TextListingRenderer.Columns(("DIAGNOSTICS", StatisticNameWidth)) + string.Join("   ", statistics.Diagnostics.Select(d =>
            string.Create(CultureInfo.InvariantCulture, $"{TextListingRenderer.SeverityWord(d.Severity)} {d.Count}")));
        if (statistics.Parse is not { } verbs) yield break;
        yield return "";
        yield return TextListingRenderer.Columns(("VERB", StatisticNameWidth)) + "COUNT".PadLeft(CountWidth) + "  LINES";
        foreach (var verb in verbs.Verbs)
            foreach (string row in TextListingRenderer.Indented(TextListingRenderer.Columns((verb.Verb, StatisticNameWidth)) + CountText(verb.Count) + "  "
                         + string.Join(" ", verb.Lines.Select(l => l.ToString(CultureInfo.InvariantCulture))),
                         0, StatisticNameWidth + CountWidth + 2, width))
                yield return row;
    }

    /// <summary>The named counts of one count of lines: each line kind that has lines, then the omitted, copied and unlisted lines.</summary>
    public static IEnumerable<(string Name, int Count)> CountsOf(ListingLineCounts counts) =>
        counts.Kinds.Where(k => k.Count > 0).Select(k => (LineKindWord(k.Kind), k.Count))
            .Append(("omitted", counts.Omitted)).Append(("copied", counts.Copied)).Append(("not listed", counts.Unlisted));

    private static string CountText(int count) => count.ToString(CultureInfo.InvariantCulture).PadLeft(CountWidth);

    private const int StatisticNameWidth = 24, CountWidth = 7;

    /// <summary>The words a line kind is printed as.</summary>
    public static string LineKindWord(ListingLineKind kind) => kind switch
    {
        ListingLineKind.Source => "source",
        ListingLineKind.Comment => "comment",
        ListingLineKind.PageEjectComment => "page-eject comment",
        ListingLineKind.DebuggingLine => "debugging",
        ListingLineKind.Continuation => "continuation",
        ListingLineKind.Directive => "directive",
        ListingLineKind.CommentEntry => "comment-entry",
        ListingLineKind.CcvsExcluded => "CCVS-excluded",
        ListingLineKind.NotAnIndicator => "not an indicator",
        ListingLineKind.Blank => "blank",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a line kind with no word"),
    };
}
