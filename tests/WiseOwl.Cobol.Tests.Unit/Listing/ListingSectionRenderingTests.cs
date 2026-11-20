// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The sections step 5 of DESIGN-listing.md section 11 adds to the text listing (sections 6.4, 6.5, 6.8 and 7.1): the options,
/// the program map and the statistics with the verbs, each on pages of its own in the order of section 7.1, each page heading
/// naming the first outermost program (section 6.1); a section the listing could not produce prints why; and the line counts
/// of the statistics. Every expected value is read off the hand-built document below or counted from its lines by hand.
/// </summary>
public sealed class ListingSectionRenderingTests
{
    private static readonly DateTimeOffset Instant = new(2026, 11, 18, 9, 30, 15, TimeSpan.FromHours(-8));

    private static ListingLine Line(int number, ListingLineKind kind, int? inclusion = null, bool omitted = false, bool listed = true,
        int file = 1) =>
        new(number, file, number, inclusion, inclusion is null ? 0 : 1, ListingLineForm.Fixed, kind, "       LINE " + number, omitted,
            listed, false, 0, ListingReplacement.None, null, [], null, []);

    /// <summary>Six lines: three of program text (one omitted), a comment, a blank line, and two lines of a library text, one
    /// of them not listed.</summary>
    private static readonly ListingLine[] Lines =
    [
        Line(1, ListingLineKind.Source),
        Line(2, ListingLineKind.Comment),
        Line(3, ListingLineKind.Source, omitted: true),
        Line(4, ListingLineKind.Source, inclusion: 1, file: 2),
        Line(5, ListingLineKind.Blank, inclusion: 1, listed: false, file: 2),
        Line(6, ListingLineKind.Source),
    ];

    [Fact]
    public void TheLineCounts_CountEachKindOnce_AndTheOmittedCopiedAndUnlistedLines()
    {
        var counts = StatisticsSectionBuilder.Count(Lines);
        Assert.Equal(6, counts.Lines);
        Assert.Equal(Enum.GetValues<ListingLineKind>(), counts.Kinds.Select(k => k.Kind));   // every kind, in enum order
        Assert.Equal([(ListingLineKind.Source, 4), (ListingLineKind.Comment, 1), (ListingLineKind.Blank, 1)],
            counts.Kinds.Where(k => k.Count > 0).Select(k => (k.Kind, k.Count)));
        Assert.Equal((1, 2, 1), (counts.Omitted, counts.Copied, counts.Unlisted));
        var none = StatisticsSectionBuilder.Count([]);
        Assert.Equal((0, 0, 0, 0), (none.Lines, none.Omitted, none.Copied, none.Unlisted));
        Assert.All(none.Kinds, k => Assert.Equal(0, k.Count));
    }

    private static ListingDocument Document(IReadOnlyList<ListingSectionStatus> sections, ListingStatistics? statistics = null,
        IReadOnlyList<ListingOption>? options = null, ListingDataMap? dataMap = null, ListingObjectCodeMap? objectCode = null) => new(
        new ListingHeader("30.0.0", 2023, "ISO/IEC 1989:2023", Instant, "/src/p.cob", ListingOutcome.Success, ListingParse.Parsed),
        [new ListingFile(1, "/src/p.cob", ListingFileRole.Source, "00"), new ListingFile(2, "/src/lib/REC.cpy", ListingFileRole.Library, "00")],
        [new ListingInclusion(1, null, 1, 3, 3, "REC", null, 2, ListingInclusionOutcome.Found, false)],
        Lines,
        [new ListingProgram(0, "OUTER", ListingProgramKind.Program, null, 0, 1, 6, null),
         new ListingProgram(1, "INNER", ListingProgramKind.Program, 0, 1, 4, 5, null)],
        [], [], [],
        options ?? [new ListingOption("--std", "2023 (ISO/IEC 1989:2023)", ListingOptionOrigin.Default),
                    new ListingOption("--copy", "/a/very/long/library/directory/" + new string('x', 120), ListingOptionOrigin.Specified),
                    new ListingOption("compiler", "30.0.0", ListingOptionOrigin.Host)],
        statistics,
        dataMap,
        objectCode,
        sections);

    private static ListingStatistics Statistics(ListingParseStatistics? parse) => new(
        StatisticsSectionBuilder.Count(Lines),
        [new ListingFileStatistics(1, StatisticsSectionBuilder.Count(Lines.Where(l => l.File == 1))),
         new ListingFileStatistics(2, StatisticsSectionBuilder.Count(Lines.Where(l => l.File == 2)))],
        [new ListingSeverityCount(ListingSeverity.Error, 0), new ListingSeverityCount(ListingSeverity.Warning, 2),
         new ListingSeverityCount(ListingSeverity.Info, 0)],
        parse);

    private static ListingSectionStatus Produced(ListingSection section) => new(section, true, null);

    /// <summary>Section 7.1: options, source, program map, statistics, diagnostics, each starting a page whose heading's second
    /// line is its title; outside the source section the heading names the first outermost program (section 6.1).</summary>
    [Fact]
    public void EachSection_StartsAPage_InTheOrderOfSection71()
    {
        var document = Document([.. Enum.GetValues<ListingSection>().Select(Produced)],
            Statistics(new ListingParseStatistics(2, 3, [new ListingVerb("MOVE", 2, [1, 6]), new ListingVerb("STOP", 1, [6])])),
            dataMap: new ListingDataMap([], []), objectCode: new ListingObjectCodeMap("p.dll", [], []));
        string[] pages = ListingRendering.Text(document, new ListingRequest()).Split(PageComposer.FormFeed);
        Assert.Equal(["OPTIONS", "SOURCE  /src/p.cob", "PROGRAMS", "DATA MAP", "STATISTICS", "OBJECT MAP", "OBJECT CODE", "DIAGNOSTICS"],
            pages.Select(p => p.Split('\n')[1]));
        Assert.All(pages.Where(p => !p.Split('\n')[1].StartsWith("SOURCE", StringComparison.Ordinal)),
            p => Assert.Contains("  OUTER  ", p.Split('\n')[0], StringComparison.Ordinal));
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8"], pages.Select(p => p.Split('\n')[0].Split(' ')[^1]));
    }

    /// <summary>Section 6.9: the object-code map's rows (the assembly, each method with its token and IL size, each statement's range
    /// at its listing line and column) and the object code's (each range's heading, the line as written when the document holds it, each
    /// instruction under it), every value read off the hand-built map.</summary>
    [Fact]
    public void TheObjectCodeMap_AndTheObjectCode_PrintWhatTheMapHolds()
    {
        var map = new ListingObjectCodeMap("out/p.dll",
            [new ListingMethod(1, 0x06000003, "Cobol.p.OUTER", "__Dispatch", 40, [])],
            [new ListingObjectRange(1, 12, 1, 0x12, 0x2A, [new ListingInstruction(0x12, "ldarg.0", null),
                 new ListingInstruction(0x13, "ldstr", "\"A\""), new ListingInstruction(0x18, "br.s", "IL_002A")], [], 0),
             new ListingObjectRange(6, 12, 1, 0x2A, 0x30, [], [], 0)]);
        Assert.Equal(
        [
            "ASSEMBLY  out/p.dll",
            "",
            "METHOD TOKEN        SIZE  NAME",
            "     1 0x06000003     40  Cobol.p.OUTER::__Dispatch",
            "",
            "  LINE  COL METHOD  IL RANGE",
            "     1   12      1  IL_0012-IL_002A",
            "     6   12      1  IL_002A-IL_0030",
        ], ObjectCodeText.MapRows(map, 132));
        Assert.Equal(
        [
            "     1   12  Cobol.p.OUTER::__Dispatch  IL_0012-IL_002A",
            "             " + Lines[0].Text.TrimEnd(),
            "             IL_0012  ldarg.0",
            "             IL_0013  ldstr        \"A\"",
            "             IL_0018  br.s         IL_002A",
            "",
            "     6   12  Cobol.p.OUTER::__Dispatch  IL_002A-IL_0030",
            "             " + Lines[5].Text.TrimEnd(),
        ], ObjectCodeText.CodeRows(map, Lines, 132));
        // Without the source section the document holds no lines, and the rows name the line by its number alone.
        Assert.Equal("     6   12  Cobol.p.OUTER::__Dispatch  IL_002A-IL_0030", ObjectCodeText.CodeRows(map, [], 132).Last());
    }

    /// <summary>A document asked for one section prints that section alone, its pages numbered from 1.</summary>
    [Fact]
    public void OnlyTheSectionsTheDocumentHolds_ArePrinted()
    {
        string text = ListingRendering.Text(Document([Produced(ListingSection.Programs)]), new ListingRequest());
        Assert.DoesNotContain(PageComposer.FormFeed, text);
        string[] rows = text.Split('\n');
        Assert.Equal("PROGRAMS", rows[1]);
        Assert.EndsWith("PAGE 1", rows[0], StringComparison.Ordinal);
    }

    /// <summary>Section 6.5: each unit's id, kind, first and last line, its name indented two columns per level of nesting.</summary>
    [Fact]
    public void TheProgramMap_ShowsEachUnit_IndentedByItsNesting()
    {
        var document = Document([Produced(ListingSection.Programs)]);
        Assert.Equal(
        [
            "    ID  KIND       FIRST    LAST  NAME",
            "     0  program        1       6  OUTER",
            "     1  program        4       5    INNER",
        ], ProgramMapText.Rows(document.Programs, ListingParse.Parsed, 132));
        Assert.Equal(ProgramMapText.NameColumn, "     0  program        1       6  ".Length);
    }

    /// <summary>Section 6.4: each row's name, origin and value; a long value wraps under the value column, no row wider than the
    /// page.</summary>
    [Fact]
    public void TheOptions_PrintNameOriginAndValue_WrappingALongValue()
    {
        var document = Document([Produced(ListingSection.Options)]);
        var rows = OptionsText.Rows(document.Options, ListingRequest.MinimumPageWidth).ToList();
        Assert.Equal("OPTION                  ORIGIN     VALUE", rows[0]);
        Assert.Equal("--std                   default    2023 (ISO/IEC 1989:2023)", rows[1]);
        Assert.StartsWith("--copy                  specified  /a/very/long/library/directory/", rows[2], StringComparison.Ordinal);
        Assert.Equal(new string(' ', 35) + "x", rows[3][..36]);   // the value goes on under itself
        Assert.Equal("compiler                host       30.0.0", rows[^1]);
        Assert.All(rows, r => Assert.True(r.Length <= ListingRequest.MinimumPageWidth, r));
        // The rows hold the whole value, in order.
        Assert.Equal(document.Options[1].Value, string.Concat(rows[2][35..], string.Concat(rows.Skip(3).SkipLast(1).Select(r => r.Trim()))));
    }

    /// <summary>Section 6.8: the lines by kind (only the kinds that have lines), each file's lines, the programs, statements and
    /// diagnostics, and each verb with its count and its lines.</summary>
    [Fact]
    public void TheStatistics_PrintTheCounts_AndEachVerbWithItsLines()
    {
        var statistics = Statistics(new ListingParseStatistics(2, 3, [new ListingVerb("MOVE", 2, [1, 6]), new ListingVerb("STOP", 1, [6])]));
        var document = Document([Produced(ListingSection.Statistics)], statistics);
        var rows = StatisticsText.Rows(statistics, document.Files, ListingParse.Parsed, 132).ToList();
        Assert.Equal(
        [
            "LINES                         6",
            "  source                      4",
            "  comment                     1",
            "  blank                       1",
            "  omitted                     1",
            "  copied                      2",
            "  not listed                  1",
            "",
            "FILES",
            "     1        4  /src/p.cob",
            "                 source 3, comment 1, omitted 1, copied 0, not listed 0",
            "     2        2  /src/lib/REC.cpy",
            "                 source 1, blank 1, omitted 0, copied 2, not listed 1",
            "",
            "PROGRAMS                      2",
            "STATEMENTS                    3",
            "DIAGNOSTICS             error 0   warning 2   info 0",
            "",
            "VERB                      COUNT  LINES",
            "MOVE                          2  1 6",
            "STOP                          1  6",
        ], rows);
    }

    /// <summary>A source that was not parsed has no programs, statements or verbs to count, and the statistics say so.</summary>
    [Fact]
    public void Statistics_OfASourceNotParsed_SaySo()
    {
        var statistics = Statistics(parse: null);
        var rows = StatisticsText.Rows(statistics, Document([]).Files, ListingParse.NotParsed, 132).ToList();
        Assert.Contains("PROGRAMS AND STATEMENTS: not counted, the source was not parsed", rows);
        Assert.DoesNotContain(rows, r => r.StartsWith("VERB", StringComparison.Ordinal));
    }

    /// <summary>Sections 6.5 and 6.8 (kb/Work PB5925 review R1-1): after a parse that recovered from a syntax error, the program
    /// map and the statement count each say they hold only what the recovery kept, the note wrapped to the page; a complete parse
    /// prints no note.</summary>
    [Fact]
    public void TheProgramMap_AndTheStatistics_OfARecoveredParse_SaySo()
    {
        const int Width = ListingRequest.MinimumPageWidth;
        string note = TextListingRenderer.RecoveredNote("the programs, statements and verbs");
        var statistics = Statistics(new ListingParseStatistics(2, 3, [new ListingVerb("MOVE", 3, [1, 6, 6])]));
        var document = Document([Produced(ListingSection.Statistics)], statistics);
        var rows = StatisticsText.Rows(statistics, document.Files, ListingParse.Recovered, Width).ToList();
        int statements = rows.IndexOf("STATEMENTS                    3");
        int diagnostics = rows.FindIndex(r => r.StartsWith("DIAGNOSTICS", StringComparison.Ordinal));
        Assert.Equal(note, string.Join(" ", rows[(statements + 1)..diagnostics].Select(r => r.Trim())));
        Assert.All(rows[(statements + 1)..diagnostics], r => Assert.StartsWith("  ", r, StringComparison.Ordinal));
        Assert.All(rows, r => Assert.True(r.Length <= Width, r));
        Assert.DoesNotContain(StatisticsText.Rows(statistics, document.Files, ListingParse.Parsed, Width), r => r.Contains("recovered", StringComparison.Ordinal));

        var map = ProgramMapText.Rows(document.Programs, ListingParse.Recovered, Width).ToList();
        int blank = map.IndexOf("");
        Assert.Equal(1 + document.Programs.Count, blank);   // the heading and each unit, then the note
        Assert.Equal(TextListingRenderer.RecoveredNote("the source units"), string.Join(" ", map[(blank + 1)..].Select(r => r.Trim())));
        Assert.All(map, r => Assert.True(r.Length <= Width, r));
        Assert.DoesNotContain(ProgramMapText.Rows(document.Programs, ListingParse.Parsed, Width), r => r.Contains("recovered", StringComparison.Ordinal));
    }

    /// <summary>DESIGN-listing.md section 4.3: a section is never left out silently; one the listing could not produce prints its
    /// title and why.</summary>
    [Fact]
    public void ASectionNotProduced_PrintsWhy()
    {
        string text = ListingRendering.Text(Document([new ListingSectionStatus(ListingSection.Programs, false, "the source was not parsed")]),
            new ListingRequest());
        string[] rows = text.Split('\n');
        Assert.Equal("PROGRAMS", rows[1]);
        Assert.Equal("Not produced: the source was not parsed", rows[3]);
    }

    /// <summary>Every word map of the new sections is total over its enum (a member added without a word is red).</summary>
    [Fact]
    public void EveryWordMap_IsTotal()
    {
        Assert.Equal(Enum.GetValues<ListingLineKind>().Length,
            Enum.GetValues<ListingLineKind>().Select(StatisticsText.LineKindWord).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Enum.GetValues<ListingProgramKind>().Length,
            Enum.GetValues<ListingProgramKind>().Select(ProgramMapText.KindWord).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Enum.GetValues<ListingOptionOrigin>().Length,
            Enum.GetValues<ListingOptionOrigin>().Select(OptionsText.OriginWord).Distinct(StringComparer.Ordinal).Count());
    }
}
