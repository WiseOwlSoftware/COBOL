// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
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
        new(1, 1, 1, null, 0, ListingLineForm.Fixed, kind, "", omitted, true, false, null, replacement, null);

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
        new(version, 85, "ANSI X3.23-1985", Instant, source, ListingOutcome.Success);

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
            [new ListingInclusion(1, null, 1, 1, 1, "REC", null, 2, ListingInclusionOutcome.Found)],
            [
                new(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "       COPY REC.", false, true, false, 0, ListingReplacement.None, null),
                new(2, 2, 1, 1, 1, ListingLineForm.Fixed, ListingLineKind.Source, "       01 R PIC X.", false, true, false, 0, ListingReplacement.None, null),
            ],
            [new ListingProgram(0, "P", ListingProgramKind.Program, null, 0, 1, 2)],
            [new ListingSectionStatus(ListingSection.Source, true, null), new ListingSectionStatus(ListingSection.Programs, true, null)]);
        var request = new ListingRequest(PageWidth: ListingRequest.MinimumPageWidth);
        string[] rows = TextListingRenderer.Render(document, request).Split('\n');
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
}
