// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The listing hooks whose rule text is the SAME sentence in 2002, 2014 and 2023 (DESIGN-listing.md section 2.2, rows L-6, L-7,
/// L-8, L-10 to L-14, L-16), written once and run at each edition by that edition's own class (<see cref="Listing2002Tests"/>,
/// <see cref="Listing2014Tests"/>, <see cref="Listing2023Tests"/>), which cites ITS edition's rule for each case: each edition
/// is judged by its own text, and the body that observes the rule is one. Every expected value is read off the case's own
/// source text and the rule, never off a captured listing.
/// </summary>
internal sealed class ListingHookCases(string dir, int edition)
{
    /// <summary>Compiles <paramref name="lines"/> at the edition with a text and JSON listing (page length 0: only a '/' line, a
    /// PAGE directive and a new section start a page, so every form feed is a rule's).</summary>
    public (CompilerDriver.Result Result, ListingDocument Listing, string Text) Compile(string name, string[] lines,
        IReadOnlyDictionary<string, string>? copybooks = null, bool checkOnly = true)
    {
        string source = Path.Combine(dir, name + ".cob");
        File.WriteAllText(source, string.Join("\n", lines));
        foreach (var (book, text) in copybooks ?? new Dictionary<string, string>()) File.WriteAllText(Path.Combine(dir, book), text);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(dir, name + ".dll"),
            DialectLevel: edition, CopyPaths: [dir], CheckOnly: checkOnly,
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json, PageLength: 0)));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        ListingJsonContract.AssertHolds(listing);
        return (result, listing, File.ReadAllText(Path.Combine(dir, name + ".lst")));
    }

    /// <summary>The source line (inclusion null) at the 1-based physical line.</summary>
    public static ListingLine Source(ListingDocument listing, int fileLine) =>
        listing.Lines.Single(l => l.Inclusion is null && l.FileLine == fileLine);

    /// <summary>The 1-based physical line of the source line holding <paramref name="text"/>.</summary>
    public static int LineOf(string[] lines, string text) => Array.FindIndex(lines, l => l.Contains(text, StringComparison.Ordinal)) + 1;

    /// <summary>Whether the text listing prints <paramref name="line"/>'s source row: its listing number, then the line as written.</summary>
    public static bool Printed(string text, ListingLine line) =>
        text.Split('\n').Any(r => r.Length > 6 && r[..6].Trim() == line.Number.ToString(CultureInfo.InvariantCulture)
            && r.TrimEnd().EndsWith(line.Text.TrimEnd(), StringComparison.Ordinal));

    private string ProgramId => "L" + edition.ToString(CultureInfo.InvariantCulture);

    /// <summary>The program shell's head every case of this edition writes (and an edition's own cases, through this one body: listing
    /// step 4b review lead R1-2).</summary>
    public string[] Prologue =>
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. " + ProgramId + ".",
        "       DATA DIVISION.",
        "       WORKING-STORAGE SECTION.",
    ];

    /// <summary>The program shell's tail every case writes.</summary>
    public static readonly string[] Epilogue =
    [
        "       PROCEDURE DIVISION.",
        "           STOP RUN.",
    ];

    /// <summary>LISTING GR2 (the default is ON), GR3 (each LISTING directive is listed), GR4 (OFF stops the source lines after it,
    /// a second OFF still listed), GR5 (ON, specified or implied by a bare <c>&gt;&gt;LISTING</c>, lists them again).</summary>
    public void ListingOffThenOn()
    {
        string[] lines =
        [
            .. Prologue,
            "       01 SHOWN-BEFORE PIC X.",
            "       >>LISTING OFF",
            "       01 HIDDEN-1 PIC X.",
            "       >>LISTING OFF",
            "       01 HIDDEN-2 PIC X.",
            "       >>LISTING",
            "       01 SHOWN-AFTER PIC X.",
            .. Epilogue,
        ];
        var (_, listing, text) = Compile("lstoff", lines);
        var hidden = new[] { LineOf(lines, "HIDDEN-1"), LineOf(lines, "HIDDEN-2") };
        for (int n = 1; n <= lines.Length; n++)
        {
            var line = Source(listing, n);
            Assert.Equal(!hidden.Contains(n), line.Listed);
            Assert.Equal(line.Listed, Printed(text, line));
        }
        Assert.Equal(ListingLineKind.Directive, Source(listing, LineOf(lines, ">>LISTING OFF")).Kind);
    }

    /// <summary>PAGE GR2: a listed PAGE directive ejects a page and is then listed; under LISTING OFF it is a source line the
    /// LISTING rules stop, so it is neither listed nor ejects a page (DESIGN-listing.md section 2.3). With page length 0 the text
    /// holds one form feed for the listed PAGE directive and one before the diagnostic summary.</summary>
    public void PageDirective()
    {
        string[] lines =
        [
            .. Prologue,
            "       01 A PIC X.",
            "       >>PAGE THE DATA ITEMS FOLLOW",
            "       01 B PIC X.",
            "       >>LISTING OFF",
            "       >>PAGE NOT LISTED",
            "       01 C PIC X.",
            "       >>LISTING ON",
            .. Epilogue,
        ];
        var (_, listing, text) = Compile("lstpage", lines);
        var page = Source(listing, LineOf(lines, "THE DATA ITEMS"));
        var hiddenPage = Source(listing, LineOf(lines, "NOT LISTED"));
        Assert.True(page.Listed);
        Assert.True(page.PageBreakBefore);
        Assert.False(hiddenPage.Listed);
        Assert.False(hiddenPage.PageBreakBefore);
        Assert.Equal([page.Number], listing.Lines.Where(l => l.PageBreakBefore).Select(l => l.Number));

        string[] pages = ListingPages.Of(text, ListingSection.Source);
        Assert.Equal(2, pages.Length);   // the page before the PAGE directive, the page it starts
        string first = pages[1].Split('\n').Skip(ListingRequest.TextHeadingLines).First(r => r.Length > 0);
        Assert.EndsWith(lines[page.FileLine - 1], first.TrimEnd(), StringComparison.Ordinal);
    }

    /// <summary>PAGE GR3: with no source listing a PAGE directive has no effect: the compilation is the one without the line,
    /// with no listing, no compiler output and no diagnostic.</summary>
    public void PageDirective_WithNoListing_HasNoEffect()
    {
        string source = Path.Combine(dir, "nolstpage.cob");
        File.WriteAllText(source, string.Join("\n", (string[])[.. Prologue, "       >>PAGE NO LISTING", "       01 A PIC X.", .. Epilogue]));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(dir, "nolstpage.dll"),
            DialectLevel: edition, CheckOnly: true));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Null(result.Listing);
        Assert.Empty(result.CompileOutput);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>COPY GR4: with SUPPRESS the library text is not listed and the COPY statement itself is. The library text's lines
    /// are in the model (the JSON keeps them), not listed; the text prints the COPY statement and no line or banner of the text.</summary>
    public void CopySuppress()
    {
        string[] lines = [.. Prologue, "       COPY SUPPBOOK SUPPRESS.", "       01 AFTER-COPY PIC X.", .. Epilogue];
        var (_, listing, text) = Compile("lstsupp", lines,
            new Dictionary<string, string> { ["SUPPBOOK.cpy"] = "       01 IN-COPY-1 PIC X.\n       01 IN-COPY-2 PIC X." });
        var inclusion = Assert.Single(listing.Inclusions);
        Assert.True(inclusion.Suppress);
        var copied = listing.Lines.Where(l => l.Inclusion == inclusion.Id).ToList();
        Assert.Equal(["       01 IN-COPY-1 PIC X.", "       01 IN-COPY-2 PIC X."], copied.Select(l => l.Text));
        Assert.All(copied, l => Assert.False(l.Listed));
        Assert.True(Source(listing, LineOf(lines, "COPY SUPPBOOK")).Listed);
        Assert.True(Source(listing, LineOf(lines, "AFTER-COPY")).Listed);
        Assert.DoesNotContain("IN-COPY", text, StringComparison.Ordinal);
        Assert.DoesNotContain("COPY SUPPBOOK ->", text, StringComparison.Ordinal);
        Assert.Contains("COPY SUPPBOOK SUPPRESS.", text, StringComparison.Ordinal);
    }

    /// <summary>COPY GR5: at the end of the library text the LISTING directive in effect for the COPY statement is in effect
    /// again. A LISTING OFF inside the library text stops the library text's own lines after it, not the source lines after the
    /// COPY; and a COPY met under OFF leaves OFF in effect after a library text that turned it ON.</summary>
    public void ListingStateAtTheEndOfLibraryText()
    {
        string[] lines =
        [
            .. Prologue,
            "       COPY OFFBOOK.",
            "       01 AFTER-OFFBOOK PIC X.",
            "       >>LISTING OFF",
            "       COPY ONBOOK.",
            "       01 AFTER-ONBOOK PIC X.",
            "       >>LISTING ON",
            .. Epilogue,
        ];
        string[] offBook = ["       01 BOOK-SHOWN PIC X.", "       >>LISTING OFF", "       01 BOOK-HIDDEN PIC X."];
        string[] onBook = ["       01 ON-BOOK-HIDDEN PIC X.", "       >>LISTING ON", "       01 ON-BOOK-SHOWN PIC X."];
        var (_, listing, _) = Compile("lstgr5", lines, new Dictionary<string, string>
        {
            ["OFFBOOK.cpy"] = string.Join("\n", offBook),
            ["ONBOOK.cpy"] = string.Join("\n", onBook),
        });
        Assert.Equal(2, listing.Inclusions.Count);
        var (off, on) = (listing.Inclusions[0], listing.Inclusions[1]);
        Assert.Equal(offBook, listing.Lines.Where(l => l.Inclusion == off.Id).Select(l => l.Text));
        Assert.Equal([true, true, false], listing.Lines.Where(l => l.Inclusion == off.Id).Select(l => l.Listed));
        Assert.Equal(onBook, listing.Lines.Where(l => l.Inclusion == on.Id).Select(l => l.Text));
        Assert.Equal([false, true, true], listing.Lines.Where(l => l.Inclusion == on.Id).Select(l => l.Listed));
        // The source lines: every one listed but the COPY ONBOOK statement and the line after it, which the OFF before them governs.
        var hidden = new[] { LineOf(lines, "COPY ONBOOK"), LineOf(lines, "AFTER-ONBOOK") };
        for (int n = 1; n <= lines.Length; n++)
            Assert.Equal(!hidden.Contains(n), Source(listing, n).Listed);
    }

    /// <summary>The comment-line rule (6.2.6.1 at 2002, 6.3.6.1 at 2014): a '/' line causes page ejection followed by printing of the
    /// line; a '*' line prints at the next available line position.</summary>
    public void SlantAndAsteriskLines()
    {
        string[] lines =
        [
            .. Prologue,
            "       01 A PIC X.",
            "      * AN ASTERISK COMMENT LINE",
            "       PROCEDURE DIVISION.",
            "      / A SLANT COMMENT LINE",
            "           STOP RUN.",
        ];
        var (_, listing, text) = Compile("lstslant", lines);
        var slant = Source(listing, LineOf(lines, "A SLANT"));
        var asterisk = Source(listing, LineOf(lines, "AN ASTERISK"));
        Assert.Equal(ListingLineKind.PageEjectComment, slant.Kind);
        Assert.Equal(ListingLineKind.Comment, asterisk.Kind);
        Assert.Equal([slant.Number], listing.Lines.Where(l => l.PageBreakBefore).Select(l => l.Number));

        string[] pages = ListingPages.Of(text, ListingSection.Source);
        var slantPage = pages.Single(p => p.Contains(lines[slant.FileLine - 1], StringComparison.Ordinal)).Split('\n');
        Assert.EndsWith(lines[slant.FileLine - 1], slantPage[ListingRequest.TextHeadingLines].TrimEnd(), StringComparison.Ordinal);
        var asteriskPage = pages.Single(p => p.Contains(lines[asterisk.FileLine - 1], StringComparison.Ordinal)).Split('\n');
        int at = Array.FindIndex(asteriskPage, r => r.EndsWith(lines[asterisk.FileLine - 1], StringComparison.Ordinal));
        Assert.EndsWith(lines[asterisk.FileLine - 2], asteriskPage[at - 1], StringComparison.Ordinal);
    }

    /// <summary>WHEN-COMPILED RV3 (15.80.2 at 2002, 15.92.2 at 2014): the returned value denotes the same time as the compilation
    /// date and time in the listing. RV1's 21 positions are computed from the instant the listing heading carries; the instant lies
    /// between two clock readings taken around the compilation, so it is the compilation's.</summary>
    public void WhenCompiled_DenotesTheListingsInstant()
    {
        string[] lines =
        [
            .. Prologue,
            "       01 WC PIC X(21).",
            "       PROCEDURE DIVISION.",
            "           MOVE FUNCTION WHEN-COMPILED TO WC.",
            "           DISPLAY WC.",
            "           STOP RUN.",
        ];
        var before = DateTimeOffset.Now;
        var (_, listing, text) = Compile("lstwhen", lines, checkOnly: false);
        var after = DateTimeOffset.Now;
        var instant = listing.Header.CompiledAt;
        Assert.InRange(instant, before, after);
        Assert.Contains(instant.ToString("yyyy'-'MM'-'dd HH':'mm':'ss zzz", CultureInfo.InvariantCulture), text.Split('\n')[0],
            StringComparison.Ordinal);

        var (ok, stdout, detail) = CutRunner.Run(Path.Combine(dir, "lstwhen.dll"), dir);
        Assert.True(ok, detail);
        Assert.Equal(WhenCompiledOf(instant), stdout.Trim());
    }

    /// <summary>The 21 character positions WHEN-COMPILED returns for <paramref name="instant"/>, from the returned-value table every
    /// edition prints (§1989:A.2.46.3 RV1, §2002:15.80.2 RV1, §2014:15.92.2 RV1, §2023:15.99.3 RV1): the year, month, day, hours,
    /// minutes, seconds and hundredths, then the sign and the hours and minutes of the offset from Coordinated Universal Time.</summary>
    public static string WhenCompiledOf(DateTimeOffset instant)
    {
        var offset = instant.Offset;
        return instant.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
            + (instant.Millisecond / 10).ToString("00", CultureInfo.InvariantCulture)
            + (offset < TimeSpan.Zero ? '-' : '+')
            + Math.Abs(offset.Hours).ToString("00", CultureInfo.InvariantCulture) + Math.Abs(offset.Minutes).ToString("00", CultureInfo.InvariantCulture);
    }
}
