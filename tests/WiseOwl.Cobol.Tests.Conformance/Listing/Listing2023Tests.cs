// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using static WiseOwl.Cobol.Tests.Conformance.Listing.ListingHookCases;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The standard's listing hooks at <c>--std 2023</c> (DESIGN-listing.md section 10 item 2; step 4a of section 11): the LISTING
/// directive (§2023:7.3.18.3 GR2-GR5), the PAGE directive (§2023:7.3.19.4 GR2), COPY SUPPRESS and the LISTING state at the end
/// of library text (§2023:7.2.3.4 GR4, GR5), PUSH and POP of the LISTING state (§2023:7.3.22.4 GR1, §2023:7.3.20.4 GR1) with the
/// implicit POP ALL of an exception-checking PERFORM (§2023:14.9.28.4 GR14), the DISPLAY directive's transfers to the listing
/// (§2023:7.3.12.4 GR1, GR2, GR5 a), GR6), and the lines REPLACE and COPY REPLACING change (§2023:7.2.4.4 GR8, §2023:7.2.3.4 GR8).
/// Each expected value is read off the test's own source text and the rule, never off a captured listing.
/// </summary>
public sealed class Listing2023Tests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listing2023");

    private readonly ListingHookCases _cases;

    public Listing2023Tests() => _cases = new ListingHookCases(_dir, 2023);

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>
    /// §2023:7.3.18.3 GR2: "The default LISTING directive is '>>LISTING ON'"; GR4: "If OFF is specified, source lines shall not be
    /// listed until a LISTING directive specifying or implying the ON phrase is encountered, with the exception that another
    /// LISTING OFF directive shall be listed"; GR5: with ON "specified or implied" they are listed again (a bare <c>&gt;&gt;LISTING</c>
    /// implies it: ON is not underlined in §2023:7.3.18.2); GR3: "Each LISTING directive shall be listed, even if the listing is
    /// being suppressed by a LISTING directive". The hidden lines are in the model, not listed, and not printed.
    /// </summary>
    [Fact]
    public void ListingOff_StopsTheLinesAfterIt_UntilOn_AndEveryListingDirectiveIsListed() => _cases.ListingOffThenOn();

    /// <summary>
    /// §2023:7.3.19.4 GR2: "If a source listing is being produced, a PAGE directive shall cause page ejection followed by listing of
    /// the PAGE directive". Under LISTING OFF a PAGE directive line is a source line §2023:7.3.18.3 GR4 stops and GR3 does not
    /// exempt, so it is neither listed nor ejects a page (DESIGN-listing.md section 2.3). With page length 0 the text holds exactly
    /// one form feed for the listed PAGE directive and one before the diagnostic summary.
    /// </summary>
    [Fact]
    public void APageDirective_EjectsAPage_ThenIsListed_ButNotUnderListingOff() => _cases.PageDirective();

    /// <summary>
    /// §2023:7.2.3.4 GR4: "If the SUPPRESS phrase is specified, library text incorporated as a result of COPY statement processing is
    /// not listed. If a listing is being produced, the COPY statement itself is listed." The library text's lines are in the model
    /// (the JSON keeps them), not listed; the text listing prints the COPY statement and no line or banner of the library text.
    /// </summary>
    [Fact]
    public void CopySuppress_ListsTheCopyStatement_ButNotTheLibraryText() => _cases.CopySuppress();

    /// <summary>
    /// §2023:7.2.3.4 GR5: "At the completion of copying the library text into the compilation group, the LISTING directive that is in
    /// effect for the COPY statement itself is considered to be in effect, regardless of any LISTING directives in the library text."
    /// A LISTING OFF inside the library text stops the library text's own lines after it, and not the source lines after the COPY.
    /// </summary>
    [Fact]
    public void AListingOffInLibraryText_DoesNotOutliveIt() => _cases.ListingStateAtTheEndOfLibraryText();

    /// <summary>§2023:7.3.19.4 GR3: "If a source listing is not being produced, a PAGE directive shall have no effect".</summary>
    [Fact]
    public void APageDirective_WithNoListing_HasNoEffect() => _cases.PageDirective_WithNoListing_HasNoEffect();

    /// <summary>
    /// §2023:6.3.7.2: "If a source listing is being produced, a comment line identified by the fixed comment indicator slant (/)
    /// causes page ejection followed by printing of the comment line"; an asterisk comment line prints at the next available line
    /// position.
    /// </summary>
    [Fact]
    public void ASlantLine_StartsAPage_AndAnAsteriskLineFollowsItsPredecessor() => _cases.SlantAndAsteriskLines();

    /// <summary>
    /// §2023:15.99.3 RV3: "The returned value shall denote the same time as the compilation date and time if provided in the
    /// listing and in the generated object code".
    /// </summary>
    [Fact]
    public void WhenCompiled_DenotesTheInstantTheListingCarries() => _cases.WhenCompiled_DenotesTheListingsInstant();

    /// <summary>
    /// §2023:7.3.22.4 GR1: "If directive-name is specified, the state of the directive is saved"; §2023:7.3.20.4 GR1 restores it. A
    /// LISTING OFF between a PUSH LISTING and its POP stops the lines up to the POP (the POP line itself is a source line under OFF),
    /// and the state the PUSH saved, ON, governs the lines after the POP.
    /// </summary>
    [Fact]
    public void PopListing_RestoresTheStatePushListingSaved()
    {
        string[] lines =
        [
            .. _cases.Prologue,
            "       >>PUSH LISTING",
            "       >>LISTING OFF",
            "       01 IN-PUSH PIC X.",
            "       >>POP LISTING",
            "       01 AFTER-POP PIC X.",
            .. ListingHookCases.Epilogue,
        ];
        var (_, listing, _) = _cases.Compile("lstpush", lines);
        Assert.False(Source(listing, LineOf(lines, "IN-PUSH")).Listed);
        Assert.False(Source(listing, LineOf(lines, ">>POP")).Listed);
        Assert.True(Source(listing, LineOf(lines, ">>LISTING OFF")).Listed);
        Assert.True(Source(listing, LineOf(lines, "AFTER-POP")).Listed);
    }

    /// <summary>
    /// §2023:14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1.
    /// Immediately preceding the END PERFORM phrase, there is an implicit POP ALL." ALL saves the LISTING state (§2023:7.3.22.4 GR2),
    /// so a LISTING OFF in the WHEN phrase stops the handler's lines, and the END PERFORM phrase and every line after it are listed.
    /// </summary>
    [Fact]
    public void AListingOffInAWhenPhrase_EndsAtEndPerform()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. L2023GR14.",
            "       PROCEDURE DIVISION.",
            "           PERFORM",
            "               DISPLAY \"IMP1\"",
            "           WHEN EC-SIZE",
            "       >>LISTING OFF",
            "               DISPLAY \"HANDLER\"",
            "           END-PERFORM",
            "           DISPLAY \"AFTER\"",
            "           STOP RUN.",
        ];
        var (_, listing, _) = _cases.Compile("lstgr14", lines);
        Assert.False(Source(listing, LineOf(lines, "HANDLER")).Listed);
        int endPerform = LineOf(lines, "END-PERFORM");
        for (int n = 1; n <= lines.Length; n++)
            Assert.Equal(n != LineOf(lines, "HANDLER"), Source(listing, n).Listed);
        Assert.True(Source(listing, endPerform).Listed);
    }

    /// <summary>
    /// §2023:14.9.28.4 GR14's implicit POP ALL stands before END PERFORM, so it has acted when a COPY after END-PERFORM is met, and
    /// §2023:7.2.3.4 GR5 ("the LISTING directive that is in effect for the COPY statement itself is considered to be in effect")
    /// puts back ON at the end of the library text. A directive in the library text is the next one the driver meets; the
    /// implicit op keyed to it acted there, after the state at the COPY had been taken, and the lines after the COPY were hidden
    /// (kb/Work PB5925 review R1-1). Every line but the handler's is listed, the library text's included.
    /// </summary>
    [Fact]
    public void ACopyAfterEndPerform_IsGovernedByTheStateTheImplicitPopRestored()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. L2023GR5.",
            "       PROCEDURE DIVISION.",
            "           PERFORM",
            "               DISPLAY \"IMP1\"",
            "           WHEN EC-SIZE",
            "       >>LISTING OFF",
            "               DISPLAY \"HANDLER\"",
            "           END-PERFORM",
            "           COPY DIRBOOK.",
            "           DISPLAY \"AFTER-COPY\"",
            "           STOP RUN.",
        ];
        string[] book = ["       >>DEFINE BOOKVAR 1", "           DISPLAY \"IN-BOOK\""];
        var (_, listing, text) = _cases.Compile("lstgr5pop", lines, new Dictionary<string, string> { ["DIRBOOK.cpy"] = string.Join("\n", book) });
        int handler = LineOf(lines, "HANDLER");
        for (int n = 1; n <= lines.Length; n++)
            Assert.Equal(n != handler, Source(listing, n).Listed);
        var inclusion = Assert.Single(listing.Inclusions);
        Assert.All(listing.Lines.Where(l => l.Inclusion == inclusion.Id), l => Assert.True(l.Listed));
        Assert.True(Printed(text, Source(listing, LineOf(lines, "AFTER-COPY"))));
    }

    /// <summary>
    /// The implicit POP ALL of an END PERFORM written in library text acts where it stands, inside the library text
    /// (§2023:14.9.28.4 GR14: "Immediately preceding the END PERFORM phrase"), so the END-PERFORM line is listed; and at the end of
    /// the library text §2023:7.2.3.4 GR5 puts back the LISTING state "in effect for the COPY statement itself", which is the
    /// handler's OFF: the COPY line and the lines after it are not listed (kb/Work PB5925 review R1-1, the end of library text).
    /// </summary>
    [Fact]
    public void AnEndPerformInLibraryText_PopsThere_AndGr5RestoresTheStateAtTheCopy()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. L2023GR5E.",
            "       PROCEDURE DIVISION.",
            "           PERFORM",
            "               DISPLAY \"IMP1\"",
            "           WHEN EC-SIZE",
            "       >>LISTING OFF",
            "               DISPLAY \"HANDLER\"",
            "           COPY ENDBOOK.",
            "           DISPLAY \"AFTER-COPY\"",
            "           STOP RUN.",
        ];
        string[] book = ["               DISPLAY \"BOOK-HANDLER\"", "           END-PERFORM"];
        var (_, listing, _) = _cases.Compile("lstgr5end", lines, new Dictionary<string, string> { ["ENDBOOK.cpy"] = string.Join("\n", book) });
        var inclusion = Assert.Single(listing.Inclusions);
        var copied = listing.Lines.Where(l => l.Inclusion == inclusion.Id).ToList();
        Assert.Equal(book, copied.Select(l => l.Text));
        Assert.Equal([false, true], copied.Select(l => l.Listed));
        int offFrom = LineOf(lines, "HANDLER");
        for (int n = 1; n <= lines.Length; n++)
            Assert.Equal(n < offFrom, Source(listing, n).Listed);
    }

    /// <summary>
    /// §2023:7.3.12.4 GR1: the operands are transferred "to the source listing or compile-time-device-1 or both in the order listed";
    /// GR6: "If the UPON phrase is not specified, the default is as if UPON LISTING were specified"; GR5 a): LISTING transfers "to the
    /// same device as that used for source listings"; GR2: with a listing, "the data transfer shall take place irrespective of
    /// whether it is suppressed by the LISTING directive". The listing transfers are the listing's, at their directives, in order;
    /// only the SYSERR transfer is the compiler's own output; and each prints as <c>DISPLAY: text</c> where its directive stands,
    /// the suppressed one included.
    /// </summary>
    [Fact]
    public void DisplayUponListing_LandsInTheListing_AtItsDirective_EvenUnderListingOff()
    {
        string[] lines =
        [
            .. _cases.Prologue,
            "       >>DISPLAY \"FIRST\" 1",
            "       >>LISTING OFF",
            "       >>DISPLAY \"SECOND\" UPON LISTING",
            "       >>DISPLAY \"TO-ERR\" UPON SYSERR",
            "       >>LISTING ON",
            .. ListingHookCases.Epilogue,
        ];
        var (result, listing, text) = _cases.Compile("lstdisp", lines);
        var first = Source(listing, LineOf(lines, "\"FIRST\""));
        var second = Source(listing, LineOf(lines, "\"SECOND\""));
        Assert.False(second.Listed);
        Assert.Equal([new ListingDisplay(first.Number, "FIRST 1"), new ListingDisplay(second.Number, "SECOND")], listing.Displays);
        Assert.Equal([new CompileOutputLine(CompileOutputStream.Error, "TO-ERR")], result.CompileOutput);

        string[] rows = text.Split('\n');
        int firstRow = Array.FindIndex(rows, r => r.TrimEnd().EndsWith(lines[first.FileLine - 1], StringComparison.Ordinal));
        string display = new string(' ', TextListingRenderer.TextColumn - 1) + "DISPLAY: ";
        Assert.Equal(display + "FIRST 1", rows[firstRow + 1]);
        int secondRow = Array.FindIndex(rows, r => r == display + "SECOND");
        int off = Array.FindIndex(rows, r => r.TrimEnd().EndsWith(">>LISTING OFF", StringComparison.Ordinal));
        int on = Array.FindIndex(rows, r => r.TrimEnd().EndsWith(">>LISTING ON", StringComparison.Ordinal));
        Assert.True(off < secondRow && secondRow < on, $"DISPLAY: SECOND at row {secondRow}, between {off} and {on}");
    }

    /// <summary>
    /// §2023:7.3.12.4 GR2: "If the compiler does not produce a source listing, the result of the DISPLAY directive is defined by the
    /// implementor": DOC-A.1-53 and DOC-A.1-54 send it to the compiler's standard output, as a <see cref="CompileOutputStream.Listing"/>
    /// transfer the command line writes there.
    /// </summary>
    [Fact]
    public void DisplayUponListing_WithNoListing_IsTheCompilersOutput()
    {
        string source = Path.Combine(_dir, "nolst.cob");
        File.WriteAllText(source, string.Join("\n", (string[])[.. _cases.Prologue, "       >>DISPLAY \"FIRST\"", "       >>DISPLAY \"SECOND\" UPON LISTING", .. ListingHookCases.Epilogue]));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, EditionInfo.Of(2023), Path.Combine(_dir, "nolst.dll"), CheckOnly: true));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Null(result.Listing);
        Assert.Equal([new CompileOutputLine(CompileOutputStream.Listing, "FIRST"), new CompileOutputLine(CompileOutputStream.Listing, "SECOND")],
            result.CompileOutput);
    }

    /// <summary>
    /// §2023:7.2.4.4 GR8 (REPLACE) and §2023:7.2.3.4 GR8 (COPY REPLACING): each matched occurrence is replaced, so what is compiled is
    /// not what is written. The listing prints each changed line as written, marked 'R', with the compiled text under it: the line
    /// REPLACE changed, and a copybook copied twice with different REPLACING operands as two blocks, each with its own replacement.
    /// A line no match touched is unmarked.
    /// </summary>
    [Fact]
    public void ReplaceAndReplacing_MarkEachChangedLine_WithTheTextCompiled()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. L2023R.",
            "       REPLACE ==:V:== BY ==VALUE 7==.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       01 NUM PIC 9 :V:.",
            "       COPY REPBOOK REPLACING ==:N:== BY ==FIRST-ITEM==.",
            "       COPY REPBOOK REPLACING ==:N:== BY ==SECOND-ITEM==.",
            .. ListingHookCases.Epilogue,
        ];
        var (_, listing, text) = _cases.Compile("lstrepl", lines, new Dictionary<string, string> { ["REPBOOK.cpy"] = "       01 :N: PIC X." });
        var replaced = Source(listing, LineOf(lines, ":V:."));
        Assert.Equal(ListingReplacement.Replacing, replaced.Replacement);
        Assert.Equal("01 NUM PIC 9 VALUE 7.", replaced.Replaced!.Trim());
        Assert.Equal(2, listing.Inclusions.Count);
        foreach (var (inclusion, name) in listing.Inclusions.Zip(["FIRST-ITEM", "SECOND-ITEM"]))
        {
            var copied = listing.Lines.Single(l => l.Inclusion == inclusion.Id);
            Assert.Equal("       01 :N: PIC X.", copied.Text);
            Assert.Equal(ListingReplacement.Replacing, copied.Replacement);
            Assert.Equal($"01 {name} PIC X.", copied.Replaced!.Trim());
        }
        // Exactly those three lines were changed: the REPLACE and COPY statements themselves and every other line are as written.
        Assert.Equal([replaced.Number, .. listing.Lines.Where(l => l.Inclusion is not null).Select(l => l.Number)],
            listing.Lines.Where(l => l.Replacement == ListingReplacement.Replacing).Select(l => l.Number));

        string[] rows = text.Split('\n');
        int row = Array.FindIndex(rows, r => r.TrimEnd().EndsWith(lines[replaced.FileLine - 1], StringComparison.Ordinal));
        Assert.Equal('R', rows[row][8]);
        Assert.Equal('R', rows[row + 1][8]);
        Assert.Equal("01 NUM PIC 9 VALUE 7.", rows[row + 1][(TextListingRenderer.TextColumn - 1)..].Trim());
    }
}
