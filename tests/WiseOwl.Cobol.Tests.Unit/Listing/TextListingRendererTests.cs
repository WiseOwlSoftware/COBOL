// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>The text renderer's row rules (DESIGN-listing.md section 6.2): the source text's 80 positions, a longer line
/// wrapped onto further rows, a position being one character (a supplementary-plane character is one), and the marker
/// column's priority.</summary>
public sealed class TextListingRendererTests
{
    [Fact]
    public void Wrap_CutsAtEightyPositions()
    {
        string eighty = new('A', 80);
        Assert.Equal([eighty], TextListingRenderer.Wrap(eighty));
        Assert.Equal([eighty, "B"], TextListingRenderer.Wrap(eighty + "B"));
        Assert.Equal([eighty, eighty], TextListingRenderer.Wrap(eighty + eighty));
        Assert.Equal([""], TextListingRenderer.Wrap(""));
        // 79 ASCII characters and two supplementary-plane characters: 81 positions in 83 UTF-16 units.
        string wide = new string('A', 79) + "\U0001D11E\U0001D11E";
        Assert.Equal([new string('A', 79) + "\U0001D11E", "\U0001D11E"], TextListingRenderer.Wrap(wide));
    }

    private static ListingLine Line(ListingLineKind kind, bool omitted = false,
        ListingReplacement replacement = ListingReplacement.None) =>
        new(1, 1, 1, null, 0, ListingLineForm.Fixed, kind, "", omitted, true, false, null, replacement, null, [], null, [], []);

    [Fact]
    public void TheMarker_FollowsTheLinesKind_OmissionAndReplacementFirst()
    {
        Assert.Equal('/', TextListingRenderer.MarkerOf(Line(ListingLineKind.PageEjectComment)));
        Assert.Equal('*', TextListingRenderer.MarkerOf(Line(ListingLineKind.Comment)));
        Assert.Equal('D', TextListingRenderer.MarkerOf(Line(ListingLineKind.DebuggingLine)));
        Assert.Equal('-', TextListingRenderer.MarkerOf(Line(ListingLineKind.Continuation)));
        Assert.Equal('>', TextListingRenderer.MarkerOf(Line(ListingLineKind.Directive)));
        Assert.Equal(' ', TextListingRenderer.MarkerOf(Line(ListingLineKind.Source)));
        Assert.Equal('E', TextListingRenderer.MarkerOf(Line(ListingLineKind.Source, replacement: ListingReplacement.DateCompiled)));
        Assert.Equal('X', TextListingRenderer.MarkerOf(Line(ListingLineKind.Comment, omitted: true)));
        Assert.Equal([' ', '1', '9', '+'], new[] { 0, 1, 9, 10 }.Select(TextListingRenderer.DepthMark));
    }

    /// <summary>A release build's version: a 40-character commit as build metadata (kb/Work PB5923 review R1-1).</summary>
    private const string ReleaseVersion = "30.0.0+dda08fc9681234567890abcdef1234567890ab";

    private static readonly DateTimeOffset Instant = new(2026, 11, 18, 9, 30, 15, TimeSpan.FromHours(-8));

    private const string InstantText = "2026-11-18 09:30:15 -08:00";

    private static ListingHeader Header(string version = ReleaseVersion, string source = "/src/p.cob") =>
        new(version, 85, "ANSI X3.23-1985", Instant, source, ListingOutcome.Success, ListingParse.Parsed);

    /// <summary>DESIGN-listing.md section 6.1: the compilation instant (§1985:VI.3.4.1, §1989:A.2.46.3 RV3), the program-name
    /// (§1985:VI.3.3.4 GR1) and the page number never yield to the compiler's version; the version yields first, at its end.
    /// A program-name at <c>--std 85</c> is a user-defined word of at most 30 characters.</summary>
    [Theory]
    [InlineData(ListingRequest.MinimumPageWidth)]
    [InlineData(ListingRequest.DefaultPageWidth)]
    [InlineData(ListingRequest.MaximumPageWidth)]
    public void TheHeading_KeepsTheProgramTheInstantAndThePage_TheVersionYields(int width)
    {
        string name = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234";
        string line = TextListingRenderer.IdentificationLine(Header(), name, 12345, width);
        Assert.Equal(width, line.Length);
        Assert.EndsWith("  PAGE 12345", line, StringComparison.Ordinal);
        Assert.Contains("ANSI X3.23-1985 (--std 85)  " + name + "  " + InstantText + "  ", line, StringComparison.Ordinal);
        // The version yields first: at the default width only the end of its build metadata, at the narrowest page all of it.
        Assert.StartsWith(width == ListingRequest.MinimumPageWidth ? "ANSI X3.23-1985" : "WiseOwl COBOL 30.0.0+dda08fc968", line,
            StringComparison.Ordinal);
    }

    /// <summary>The line is the fields separated by two spaces when everything fits, the page number ending at the page's
    /// last column.</summary>
    [Fact]
    public void TheHeading_WhenItFits_IsEveryFieldWhole()
    {
        string line = TextListingRenderer.IdentificationLine(Header("1.2.3"), "OUTER85", 1, ListingRequest.DefaultPageWidth);
        Assert.Equal(("WiseOwl COBOL 1.2.3  ANSI X3.23-1985 (--std 85)  OUTER85  " + InstantText).PadRight(126) + "PAGE 1", line);
    }

    /// <summary>A program-name longer than the narrowest page leaves it (a 2023 alphanumeric literal can be) yields last, at
    /// its end, after the version and the edition have; the instant and the page number are still whole.</summary>
    [Fact]
    public void TheHeading_CutsAProgramNameOnlyWhenNothingElseIsLeftToYield()
    {
        string name = new('N', 90);
        string line = TextListingRenderer.IdentificationLine(Header(), name, 7, ListingRequest.MinimumPageWidth);
        Assert.Equal(ListingRequest.MinimumPageWidth, line.Length);
        Assert.StartsWith("NNNN", line, StringComparison.Ordinal);
        Assert.EndsWith("N  " + InstantText + "  PAGE 7", line, StringComparison.Ordinal);
    }

    /// <summary>The second heading line keeps the END of a long source path (the file's name), and a COPY banner naming a long
    /// path wraps in the source columns as a long line does: no row of the page is wider than the page.</summary>
    [Fact]
    public void ALongPath_NeverWidensARow()
    {
        string dir = "/" + string.Join("/", Enumerable.Range(0, 20).Select(i => "directory" + i));
        string source = dir + "/main.cob", library = dir + "/REC.cpy";
        var document = new ListingDocument(Header(source: source),
            [new ListingFile(1, source, ListingFileRole.Source, "00"), new ListingFile(2, library, ListingFileRole.Library, "00")],
            [new ListingInclusion(1, null, 1, 1, 1, "REC", null, 2, ListingInclusionOutcome.Found, false)],
            [
                new(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "       COPY REC.", false, true, false, 0, ListingReplacement.None, null, [], null, [], []),
                new(2, 2, 1, 1, 1, ListingLineForm.Fixed, ListingLineKind.Source, "       01 R PIC X.", false, true, false, 0, ListingReplacement.None, null, [1], null, [], []),
            ],
            [new ListingProgram(0, "P", ListingProgramKind.Program, null, 0, 1, 2, null)],
            [new ListingDiagnostic(1, "COBOL0001", ListingSeverity.Error, "a message of many words " + string.Join(" ", Enumerable.Repeat("word", 60)),
                2, 2, 1, 8, [], null)],
            [], [], [], null, null, null, null,
            [new ListingSectionStatus(ListingSection.Source, true, null), new ListingSectionStatus(ListingSection.Programs, true, null),
             new ListingSectionStatus(ListingSection.Diagnostics, true, null)]);
        var request = new ListingRequest(PageWidth: ListingRequest.MinimumPageWidth);
        // A form feed opens each page after the first; it is no column.
        string[] rows = [.. ListingRendering.Text(document, request).Split('\n').Select(r => r.TrimStart(PageComposer.FormFeed))];
        Assert.All(rows, r => Assert.True(r.Length <= request.PageWidth, $"{r.Length} columns: {r}"));
        Assert.StartsWith("SOURCE  ...", rows[1], StringComparison.Ordinal);
        Assert.EndsWith("/directory19/main.cob", rows[1], StringComparison.Ordinal);
        Assert.Equal(request.PageWidth, rows[1].Length);
        // The banner's rows, their source columns joined, are the whole banner.
        int banner = Array.FindIndex(rows, r => r.Contains("COPY REC -> ", StringComparison.Ordinal));
        string joined = string.Concat(rows.Skip(banner).TakeWhile(r => !r.Contains("01 R PIC X.", StringComparison.Ordinal))
            .Select(r => r[(TextListingRenderer.TextColumn - 1)..]));
        Assert.Equal("COPY REC -> " + library + " (depth 1)", joined);
    }

    /// <summary>DESIGN-listing.md section 6.2, columns 11-16 (kb/Work PB5950): the file line is printed only where it is not the
    /// listing number. Before the first inclusion the two agree and the column is blank; on a line of a library text, and on a
    /// line of the source after an inclusion, they differ and the column names the line in its own file, right-aligned.</summary>
    [Fact]
    public void TheFileLineColumn_IsBlankWhereItIsTheListingNumber()
    {
        var document = new ListingDocument(Header(),
            [new ListingFile(1, "/src/p.cob", ListingFileRole.Source, "00"), new ListingFile(2, "/src/REC.cpy", ListingFileRole.Library, "00")],
            [new ListingInclusion(1, null, 1, 2, 2, "REC", null, 2, ListingInclusionOutcome.Found, false)],
            [
                new(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "       WORKING-STORAGE SECTION.", false, true, false, 0, ListingReplacement.None, null, [], null, [], []),
                new(2, 1, 2, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "       COPY REC.", false, true, false, 0, ListingReplacement.None, null, [], null, [], []),
                new(3, 2, 1, 1, 1, ListingLineForm.Fixed, ListingLineKind.Source, "       01 R PIC X.", false, true, false, 0, ListingReplacement.None, null, [], null, [], []),
                new(4, 1, 3, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "       01 S PIC X.", false, true, false, 0, ListingReplacement.None, null, [], null, [], []),
            ],
            [new ListingProgram(0, "P", ListingProgramKind.Program, null, 0, 1, 4, null)],
            [], [], [], [], null, null, null, null,
            [new ListingSectionStatus(ListingSection.Source, true, null), new ListingSectionStatus(ListingSection.Programs, true, null)]);
        string[] rows = [.. ListingRendering.Text(document, new ListingRequest()).Split('\n').Select(r => r.TrimStart(PageComposer.FormFeed))];
        string Row(string text) => Assert.Single(rows, r => r.EndsWith(text, StringComparison.Ordinal));
        // Columns 1-6 the listing number, 11-16 the file line: blank, blank, the library's line 1, the source's line 3.
        Assert.Equal("     1           ", Row("WORKING-STORAGE SECTION.")[..17]);
        Assert.Equal("     2           ", Row("COPY REC.")[..17]);
        Assert.Equal("     3 1       1 ", Row("01 R PIC X.")[..17]);
        Assert.Equal("     4         3 ", Row("01 S PIC X.")[..17]);
    }

    private static ListingDiagnostic Diagnostic(int id, ListingSeverity severity, string message, int? line = 1, int? column = 8) =>
        new(id, "COBOL0001", severity, message, line, line is null ? null : 1, line, line is null ? null : column, [], null);

    /// <summary>A diagnostic under a line printed whole (DESIGN-listing.md section 6.3): a caret under its column, in the source
    /// columns (a supplementary-plane character before it being one position), then the arrow row at the source text's column.</summary>
    [Fact]
    public void ADiagnostic_PrintsACaretUnderItsColumn_ThenItsRow()
    {
        string[] rows = [.. TextListingRenderer.DiagnosticRows(Diagnostic(1, ListingSeverity.Error, "no such name"), "       MOVE X TO Y.", 132)];
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1 + 7) + "^", rows[0]);
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1) + "==> error COBOL0001 (col 8): no such name", rows[1]);
        Assert.Equal(2, rows.Length);
        // \U0001D11E is two UTF-16 units and one character position: the X after it is position 2, and the caret stands one
        // position on; position 3 is the text's end plus one, and 4 lies past it.
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1 + 1) + "^", TextListingRenderer.CaretRow("\U0001D11EX", 2));
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1 + 2) + "^", TextListingRenderer.CaretRow("\U0001D11EX", 3));
        Assert.Null(TextListingRenderer.CaretRow("\U0001D11EX", 4));
        Assert.Null(TextListingRenderer.CaretRow("AB", 4));
        // Under a line not printed whole (wrapped or replaced) there is no caret, and a diagnostic with no column names none.
        Assert.Single(TextListingRenderer.DiagnosticRows(Diagnostic(1, ListingSeverity.Warning, "w"), null, 132));
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1) + "==> info COBOL0001: i",
            Assert.Single(TextListingRenderer.DiagnosticRows(Diagnostic(1, ListingSeverity.Info, "i", line: null), "X", 132)));
    }

    /// <summary>A long message wraps at a space onto rows indented under it, none wider than the page, and the rows read back as
    /// the whole message.</summary>
    [Fact]
    public void ALongMessage_WrapsUnderItself_NoRowWiderThanThePage()
    {
        string message = string.Join(" ", Enumerable.Range(1, 60).Select(n => "word" + n)) + " " + new string('x', 150);
        string[] rows = [.. TextListingRenderer.DiagnosticRows(Diagnostic(1, ListingSeverity.Error, message), null, 100)];
        Assert.True(rows.Length > 3);
        Assert.All(rows, r => Assert.True(r.Length <= 100, $"{r.Length}: {r}"));
        Assert.All(rows.Skip(1), r => Assert.StartsWith(new string(' ', TextListingRenderer.TextColumn - 1 + TextListingRenderer.Arrow.Length), r, StringComparison.Ordinal));
        // Broken at spaces, the 150-position word cut where it does not fit: the rows hold the whole text, in order.
        string expected = "==> error COBOL0001 (col 8): " + message;
        Assert.Equal(expected.Replace(" ", "", StringComparison.Ordinal),
            string.Concat(rows.Select(r => r.Replace(" ", "", StringComparison.Ordinal))));
        Assert.All(rows.SkipLast(2), r => Assert.Matches("word[0-9]+$", r));
    }
    /// <summary>The summary (DESIGN-listing.md section 6.3): the counts by severity, then each diagnostic in the document's order
    /// with its listing line (or '-' for one placed on no line) and its file, line and column.</summary>
    [Fact]
    public void TheSummary_CountsBySeverity_ThenListsEachDiagnostic()
    {
        var document = new ListingDocument(Header(), [new ListingFile(1, "main.cob", ListingFileRole.Source, "00")], [],
            [new(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "       MOVE X TO Y.", false, true, false, null,
                ListingReplacement.None, null, [1, 2], null, [], [])],
            [], [Diagnostic(1, ListingSeverity.Error, "e"), Diagnostic(2, ListingSeverity.Warning, "w"), Diagnostic(3, ListingSeverity.Warning, "g", line: null)],
            [], [], [], null, null, null, null,
            [new ListingSectionStatus(ListingSection.Source, true, null), new ListingSectionStatus(ListingSection.Diagnostics, true, null)]);
        Assert.Equal(
        [
            "Errors: 1   Warnings: 2   Information: 0",
            "",
            "     1  error COBOL0001 main.cob(1,8): e",
            "     1  warning COBOL0001 main.cob(1,8): w",
            "     -  warning COBOL0001: g",
        ], TextListingRenderer.SummaryRows(document, 132));
        // In the whole listing the summary starts a page after the source section, whose line carries both diagnostics.
        string[] pages = ListingRendering.Text(document, new ListingRequest()).Split(PageComposer.FormFeed);
        Assert.Equal(2, pages.Length);
        Assert.Contains("==> error COBOL0001 (col 8): e", pages[0], StringComparison.Ordinal);
        Assert.Contains("==> warning COBOL0001 (col 8): w", pages[0], StringComparison.Ordinal);
        Assert.DoesNotContain("COBOL0001: g", pages[0], StringComparison.Ordinal);
        Assert.Equal("DIAGNOSTICS", pages[1].Split('\n')[1]);
        Assert.Contains("     -  warning COBOL0001: g", pages[1], StringComparison.Ordinal);
    }
}