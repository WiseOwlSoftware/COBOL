// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Xml.Linq;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The diagnostic summary's table of flagged constructs by edition (DESIGN-listing.md section 6.11; kb/Work PB5947), over hand-built
/// diagnostics: the builder groups the edition flags the diagnostics carry by edition, kind and construct, never reading a code or
/// a message; the text listing prints the table under the summary's list and the HTML page prints the same words with each listing
/// line a link to its diagnostic. Every expected value is read off the diagnostics the test builds.
/// </summary>
public sealed class FlaggedConstructTests
{
    private static readonly ListingFlag ExitProgram =
        new(ListingFlagKind.Obsolete, "exit-program-archaic-2023", "the EXIT PROGRAM statement", 2023);
    private static readonly ListingFlag CloseLock = new(ListingFlagKind.Removed, "close-with-lock-removed-2023", "the CLOSE WITH LOCK phrase", 2023);
    private static readonly ListingFlag Comp3 = new(ListingFlagKind.Extension, "usage-comp-3", "USAGE COMP-3", 2023);
    private static readonly ListingFlag Debugging = new(ListingFlagKind.Obsolete, "debugging-line-removed-2014", "the fixed-form debugging line", 2002);

    private static ListingDiagnostic Diagnostic(int id, int? line, ListingFlag? flag, string code = "COBOL0903") =>
        new(id, code, ListingSeverity.Warning, "words that are not read", line, line is null ? null : 1, line, line is null ? null : 8, [], flag);

    /// <summary>Six diagnostics in listing order: two flag the EXIT PROGRAM statement (one placed on no line), one each the CLOSE WITH
    /// LOCK phrase, USAGE COMP-3 and the 2002 debugging line, and one is no flag though its code is an edition flag's (the table reads
    /// the flag, never the code).</summary>
    private static readonly ListingDiagnostic[] Six =
    [
        Diagnostic(1, 3, Comp3, "COBOL2894"),
        Diagnostic(2, 5, ExitProgram),
        Diagnostic(3, 7, null),
        Diagnostic(4, 9, CloseLock, "COBOL0902"),
        Diagnostic(5, 11, Debugging),
        Diagnostic(6, null, ExitProgram),
    ];

    [Fact]
    public void TheTable_GroupsTheFlags_ByEditionKindAndConstruct()
    {
        var table = DiagnosticSectionBuilder.FlaggedConstructs(Six);
        Assert.Equal(
        [
            new ListingFlaggedConstruct(2002, ListingFlagKind.Obsolete, Debugging.Construct, Debugging.Description, [5]),
            new ListingFlaggedConstruct(2023, ListingFlagKind.Obsolete, ExitProgram.Construct, ExitProgram.Description, [2, 6]),
            new ListingFlaggedConstruct(2023, ListingFlagKind.Removed, CloseLock.Construct, CloseLock.Description, [4]),
            new ListingFlaggedConstruct(2023, ListingFlagKind.Extension, Comp3.Construct, Comp3.Description, [1]),
        ], table, Equivalent.Instance);
        Assert.Empty(DiagnosticSectionBuilder.FlaggedConstructs([Diagnostic(1, 1, null)]));
    }

    /// <summary>The compiler's flag becomes the listing's: every kind of <see cref="EditionFlagKind"/> maps to the
    /// <see cref="ListingFlagKind"/> of its own name, the construct, its description and the edition unchanged, and the two enums have
    /// the same members, so a kind added to either without the other is red here.</summary>
    [Fact]
    public void TheFlagKindMap_IsTotal()
    {
        foreach (var kind in Enum.GetValues<EditionFlagKind>())
        {
            var flag = DiagnosticSectionBuilder.FlagOf(new EditionFlag(kind, "row", "the row", 2014))!;
            Assert.Equal((kind.ToString(), "row", "the row", 2014), (flag.Kind.ToString(), flag.Construct, flag.Description, flag.Edition));
        }
        Assert.Equal(Enum.GetNames<EditionFlagKind>(), Enum.GetNames<ListingFlagKind>());
        Assert.Null(DiagnosticSectionBuilder.FlagOf(null));
        Assert.Equal(["obsolete", "removed", "extension"], Enum.GetValues<ListingFlagKind>().Select(FlaggedConstructText.KindWord));
    }

    private static ListingDocument Document(IReadOnlyList<ListingDiagnostic> diagnostics) => new(
        new ListingHeader("30.0.0", 2023, "ISO/IEC 1989:2023", DateTimeOffset.UnixEpoch, "main.cob", ListingOutcome.Success, ListingParse.Parsed),
        [new ListingFile(1, "main.cob", ListingFileRole.Source, "00")], [],
        [.. Enumerable.Range(1, 11).Select(n => new ListingLine(n, 1, n, null, 0, ListingLineForm.Fixed, ListingLineKind.Source,
            "       DISPLAY 1.", false, true, false, null, ListingReplacement.None, null,
            [.. diagnostics.Where(d => d.Line == n).Select(d => d.Id)], null, []))],
        [], diagnostics, DiagnosticSectionBuilder.FlaggedConstructs(diagnostics), [], [], null, null, null,
        [new ListingSectionStatus(ListingSection.Source, true, null), new ListingSectionStatus(ListingSection.Diagnostics, true, null)]);

    /// <summary>The text listing's summary ends with the table: a blank row, the title, the headings, then a row per construct with
    /// its edition, kind, count, construct and the listing line of each of its diagnostics ('-' for one on no line).</summary>
    [Fact]
    public void TheSummary_EndsWithTheTable()
    {
        var rows = TextListingRenderer.SummaryRows(Document(Six), ListingRequest.DefaultPageWidth).ToList();
        int title = rows.IndexOf(FlaggedConstructText.Title);
        Assert.True(title > 0, "the summary prints no table title");
        Assert.Equal("", rows[title - 1]);
        Assert.Equal(
        [
            "EDITION  KIND      COUNT  CONSTRUCT; LINES",
            "   2002  obsolete      1  the fixed-form debugging line (debugging-line-removed-2014); 11",
            "   2023  obsolete      2  the EXIT PROGRAM statement (exit-program-archaic-2023); 5 -",
            "   2023  removed       1  the CLOSE WITH LOCK phrase (close-with-lock-removed-2023); 9",
            "   2023  extension     1  USAGE COMP-3 (usage-comp-3); 3",
        ], rows.Skip(title + 1));
        Assert.DoesNotContain(FlaggedConstructText.Title, TextListingRenderer.SummaryRows(Document([Diagnostic(1, 1, null)]), 132));
    }

    /// <summary>A long construct wraps under the construct column, no row wider than the page.</summary>
    [Fact]
    public void ALongConstruct_WrapsUnderItsColumn()
    {
        var flag = new ListingFlag(ListingFlagKind.Removed, "row", string.Join(" ", Enumerable.Repeat("word", 40)), 2023);
        var rows = FlaggedConstructText.Rows(Document([Diagnostic(1, 2, flag)]), ListingRequest.MinimumPageWidth).ToList();
        var entry = rows.Skip(3).ToList();
        Assert.True(entry.Count > 1, "the construct was to be longer than the page");
        Assert.All(entry, r => Assert.True(r.Length <= ListingRequest.MinimumPageWidth, r));
        int construct = entry[0].IndexOf("word", StringComparison.Ordinal);   // the construct column: after edition, kind and count
        Assert.Equal(26, construct);
        Assert.All(entry.Skip(1), r => Assert.StartsWith(new string(' ', construct), r, StringComparison.Ordinal));
        Assert.Equal($"2023removed1{flag.Description}(row);2".Replace(" ", "", StringComparison.Ordinal),
            string.Concat(entry).Replace(" ", "", StringComparison.Ordinal));
    }

    /// <summary>The HTML page's diagnostics section holds the same table in the text listing's words, each listing line a link to the
    /// diagnostic under its line, and a diagnostic placed on no line printed '-' with no link.</summary>
    [Fact]
    public void ThePage_HoldsTheTable_LinkingEachLineToItsDiagnostic()
    {
        var page = HtmlListingRendererTests.Read(HtmlListingRenderer.Render(Document(Six), new ListingRequest()));
        var title = page.Descendants("h3").Single(h => h.Value == FlaggedConstructText.Title);
        var table = title.ElementsAfterSelf("table").First();
        Assert.Equal(FlaggedConstructText.Headings, table.Descendants("th").Select(th => th.Value));
        var rows = table.Element("tbody")!.Elements("tr").ToList();
        Assert.Equal(4, rows.Count);
        string[] Cells(XElement row) => [.. row.Elements("td").Select(td => td.Value)];
        Assert.Equal(["2023", "obsolete", "2", "the EXIT PROGRAM statement (exit-program-archaic-2023)", "5 -"], Cells(rows[1]));
        Assert.Equal(["#" + HtmlListingRenderer.DiagnosticId(2)], rows[1].Descendants("a").Select(a => (string?)a.Attribute("href")));
        Assert.Equal(["#" + HtmlListingRenderer.DiagnosticId(1)], rows[3].Descendants("a").Select(a => (string?)a.Attribute("href")));
        var none = HtmlListingRendererTests.Read(HtmlListingRenderer.Render(Document([Diagnostic(1, 1, null)]), new ListingRequest()));
        Assert.DoesNotContain(none.Descendants("h3"), h => h.Value == FlaggedConstructText.Title);
    }

    /// <summary>Two table entries are equal when every member is, the diagnostics' ids compared as a sequence (a record's own equality
    /// compares the list by reference).</summary>
    private sealed class Equivalent : IEqualityComparer<ListingFlaggedConstruct>
    {
        public static readonly Equivalent Instance = new();

        public bool Equals(ListingFlaggedConstruct? x, ListingFlaggedConstruct? y) =>
            x is not null && y is not null && (x.Edition, x.Kind, x.Construct, x.Description) == (y.Edition, y.Kind, y.Construct, y.Description)
            && x.Diagnostics.SequenceEqual(y.Diagnostics);

        public int GetHashCode(ListingFlaggedConstruct obj) => HashCode.Combine(obj.Edition, obj.Kind, obj.Construct);
    }
}
