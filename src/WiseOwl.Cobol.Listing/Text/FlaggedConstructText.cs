// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>The diagnostic summary's table of flagged constructs by edition (DESIGN-listing.md section 6.11): each construct an
/// edition flag names (<see cref="ListingDocument.FlaggedConstructs"/>), with the edition and kind of its flag, its count and the
/// listing lines of its diagnostics. The words are the text listing's and the HTML page's alike.</summary>
public static class FlaggedConstructText
{
    /// <summary>The table's title.</summary>
    public const string Title = "FLAGGED CONSTRUCTS BY EDITION";

    /// <summary>The table's headings: the edition, the kind, the count, the construct and the listing lines.</summary>
    public static readonly IReadOnlyList<string> Headings = ["EDITION", "KIND", "COUNT", "CONSTRUCT", "LINES"];

    /// <summary>The table's rows under the summary's list (<see cref="TextListingRenderer.SummaryRows"/>): a blank row, the title,
    /// the headings and a row per construct, its construct and lines wrapped under the construct column; nothing when no
    /// diagnostic is an edition flag.</summary>
    public static IEnumerable<string> Rows(ListingDocument document, int width)
    {
        if (document.FlaggedConstructs.Count == 0) yield break;
        var diagnostics = document.Diagnostics.ToDictionary(d => d.Id);
        yield return "";
        yield return Title;
        yield return Lead(Headings[0], Headings[1], Headings[2]) + Headings[3] + "; " + Headings[4];
        foreach (var construct in document.FlaggedConstructs)
            foreach (string row in TextListingRenderer.Indented(
                         Lead(EditionText(construct.Edition), KindWord(construct.Kind), CountText(construct)) + ConstructText(construct)
                         + "; " + LinesText(construct, diagnostics), 0, ConstructColumn, width))
                yield return row;
    }

    /// <summary>The edition as <c>--std</c> spells its year.</summary>
    public static string EditionText(int edition) => edition.ToString(CultureInfo.InvariantCulture);

    /// <summary>The word a flag's kind is printed as.</summary>
    public static string KindWord(ListingFlagKind kind) => kind switch
    {
        ListingFlagKind.Obsolete => "obsolete",
        ListingFlagKind.Removed => "removed",
        ListingFlagKind.Extension => "extension",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a flag kind with no word"),
    };

    /// <summary>How many diagnostics flag the construct.</summary>
    public static string CountText(ListingFlaggedConstruct construct) =>
        construct.Diagnostics.Count.ToString(CultureInfo.InvariantCulture);

    /// <summary>The construct as both renderers print it: its description, then its identifier in parentheses.</summary>
    public static string ConstructText(ListingFlaggedConstruct construct) => $"{construct.Description} ({construct.Construct})";

    /// <summary>The listing line of each diagnostic that flags the construct, in listing order, <c>-</c> for one placed on no line.</summary>
    public static string LinesText(ListingFlaggedConstruct construct, IReadOnlyDictionary<int, ListingDiagnostic> diagnostics) =>
        string.Join(" ", construct.Diagnostics.Select(id => diagnostics[id].Line?.ToString(CultureInfo.InvariantCulture) ?? "-"));

    private static string Lead(string edition, string kind, string count) =>
        edition.PadLeft(EditionWidth) + "  " + kind.PadRight(KindWidth) + count.PadLeft(CountWidth) + "  ";

    private const int EditionWidth = 7, KindWidth = 10, CountWidth = 5;

    /// <summary>The column a construct starts at, and its wrapped rows.</summary>
    private const int ConstructColumn = EditionWidth + 2 + KindWidth + CountWidth + 2;
}
