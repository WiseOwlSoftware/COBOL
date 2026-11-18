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
/// ⛔ THE 1985 STANDARD MINIMUM OF THE SOURCE LISTING (kb/Work PB5923; DESIGN-listing.md sections 2.1 and 10 item 1). Each
/// case compiles a small program at <c>--std 85</c> with a listing and asserts on the model and on the rendered text. Every
/// expected value comes from a rule and from the input files the test wrote, never from a captured listing:
/// <list type="bullet">
/// <item>§1985:IV.7.1: "each compiler accepts source programs written in reference format and produces an output listing
/// of the source program in reference format"; §1985:IV.7.4: "The entries on the output listing need be indented only if
/// the input is indented" — every physical line of the source and of each copybook is listed once per inclusion, as the
/// file holds it.</item>
/// <item>§1985:IV.7.2.4: "The slant in the indicator area causes page ejection prior to printing the comment line in the
/// listing of the source program"; an asterisk comment line prints "at the next available line position in the
/// listing".</item>
/// <item>§1985:VI.3.4.4 GR1: the DATE-COMPILED paragraph "is replaced during compilation with a paragraph of the form"
/// <c>DATE-COMPILED. current date.</c>; §1985:VI.3.4.1: it "provides the compilation date in the Identification Division
/// source program listing"; §1989:A.2.46.3 RV3: WHEN-COMPILED "must denote the same time as the compilation date and time if
/// provided in the listing of the source program".</item>
/// <item>§1985:VI.3.3.4 GR1 and §1985:X.3.1.4 GR1: "The program-name identifies the source program, the object program, and
/// all listings pertaining to a particular program" — each page heading names the innermost program of its first line.</item>
/// </list>
/// The Conformance assembly cannot reach the compile-clock seam (it is internal to the compiler), so the instant is the one the
/// listing records, and the third case proves it is the compilation's: it lies between two clock readings taken around the
/// compilation, and the heading, the DATE-COMPILED row and the program's WHEN-COMPILED all denote it.
/// </summary>
public sealed class Listing1985Tests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listing85");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>A fixed-form program with a comment-entry, a copybook copied twice, an asterisk line, a slant line, a
    /// WHEN-COMPILED reference and a nested program.</summary>
    private const string Main = """
        000100 IDENTIFICATION DIVISION.
        000200 PROGRAM-ID. OUTER85.
        000300 DATE-COMPILED. NOT YET.
        000400     STILL THE COMMENT-ENTRY.
        000500 ENVIRONMENT DIVISION.
        000600 DATA DIVISION.
        000700 WORKING-STORAGE SECTION.
        000800 01 GROUP-A.
        000900     COPY REC85.
        001000 01 GROUP-B.
        001100     COPY REC85.
        001200 01 WC PIC X(21).
        001300* AN ASTERISK COMMENT LINE
        001400 PROCEDURE DIVISION.
        001500/ A SLANT COMMENT LINE
        001600 MAIN-PARA.
        001700     MOVE FUNCTION WHEN-COMPILED TO WC.
        001800     DISPLAY WC.
        001900     CALL "INNER85".
        002000     STOP RUN.
        002100 IDENTIFICATION DIVISION.
        002200 PROGRAM-ID. INNER85.
        002300 PROCEDURE DIVISION.
        002400 INNER-PARA.
        002500     EXIT PROGRAM.
        002600 END PROGRAM INNER85.
        002700 END PROGRAM OUTER85.
        """;

    private const string Rec = """
        000100     05 FIELD-1 PIC X(4).
        000200*    A COMMENT IN THE LIBRARY TEXT
        000300     05 FIELD-2 PIC 9(4).
        """;

    private (CompilerDriver.Result Result, string Source, string Copybook) Compile(ListingRequest request, string main = Main)
    {
        string source = Path.Combine(_dir, "outer85.cob");
        string copybook = Path.Combine(_dir, "REC85.cpy");
        File.WriteAllText(source, main);
        File.WriteAllText(copybook, Rec);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, "outer85.dll"),
            DialectLevel: 85, CopyPaths: [_dir], Listing: request));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);   // section 10 item 5: every document
        return (result, source, copybook);
    }

    /// <summary>The file's physical lines as the compiler's line-entry stage defines them (DOC-A.1-156: a LINE FEED ends a
    /// line, a text with n LINE FEEDs has n + 1 lines), read from the file the test wrote.</summary>
    private static string[] LinesOf(string path) => File.ReadAllText(path).Split('\n');

    [Fact]
    public void EveryPhysicalLine_OfTheSourceAndOfEachInclusion_IsListedAsTheFileHoldsIt()
    {
        var (result, source, copybook) = Compile(new ListingRequest());
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        string[] main = LinesOf(source), rec = LinesOf(copybook);

        // The expected listing order (DESIGN-listing.md section 4.4): the source's lines, and after each COPY statement's
        // line (9 and 11) the library text's lines, one level deeper.
        var expected = new List<(string Text, int Depth, int FileLine)>();
        for (int n = 1; n <= main.Length; n++)
        {
            expected.Add((main[n - 1], 0, n));
            if (n is 9 or 11) expected.AddRange(rec.Select((t, i) => (t, 1, i + 1)));
        }
        Assert.Equal(expected, listing.Lines.Select(l => (l.Text, l.Depth, l.FileLine)));
        Assert.Equal([.. Enumerable.Range(1, expected.Count)], listing.Lines.Select(l => l.Number));

        // Two inclusions of one file: two blocks, each named by its own inclusion, each after its COPY statement's line.
        Assert.Equal(2, listing.Inclusions.Count);
        Assert.All(listing.Inclusions, i => Assert.Equal(("REC85", ListingInclusionOutcome.Found, 1), (i.TextName, i.Outcome, i.Depth)));
        Assert.Equal([9, 9 + rec.Length + 2], listing.Inclusions.Select(i => i.CopyLine));
        foreach (var inclusion in listing.Inclusions)
            Assert.Equal(Enumerable.Range(inclusion.CopyLastLine + 1, rec.Length),
                listing.Lines.Where(l => l.Inclusion == inclusion.Id).Select(l => l.Number));

        // Each file once, by its role and by the SHA-256 of the bytes the test wrote.
        Assert.Equal(
            [(Path.GetFullPath(source), ListingFileRole.Source, Sha256(source)), (Path.GetFullPath(copybook), ListingFileRole.Library, Sha256(copybook))],
            listing.Files.Select(f => (Path.GetFullPath(f.Path), f.Role, f.Sha256)));

        // The rendered text prints each listed line's own text from column 18 (§1985:IV.7.4: indented as the input is).
        string text = File.ReadAllText(Path.Combine(_dir, "outer85.lst"));
        foreach (string line in main.Where(l => l.Length > 0 && !l.Contains("DATE-COMPILED", StringComparison.Ordinal)
                     && !l.Contains("COMMENT-ENTRY", StringComparison.Ordinal)))
            Assert.Contains(line.TrimEnd(), text, StringComparison.Ordinal);
        Assert.Contains(Path.GetFullPath(Path.Combine(_dir, "outer85.lst")), result.OutputFiles);
    }

    private static string Sha256(string path) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void ASlantLine_IsTheFirstRowOfANewPage_AndAnAsteriskLineFollowsItsPredecessor()
    {
        var (result, source, _) = Compile(new ListingRequest(PageLength: 0));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        string[] main = LinesOf(source);
        string slant = main.Single(l => l.Length > 6 && l[6] == '/');
        string asterisk = main.Single(l => l.Length > 6 && l[6] == '*');
        string beforeAsterisk = main[Array.IndexOf(main, asterisk) - 1];

        string[] pages = File.ReadAllText(Path.Combine(_dir, "outer85.lst")).Split(PageComposer.FormFeed);
        // The page that holds the slant line: its first row after the heading IS the slant line.
        var slantPage = pages.Single(p => p.Contains(slant, StringComparison.Ordinal)).Split('\n');
        Assert.EndsWith(slant, slantPage[ListingRequest.TextHeadingLines].TrimEnd(), StringComparison.Ordinal);
        // The asterisk line prints on the page of the line before it, in the next row.
        var asteriskPage = pages.Single(p => p.Contains(asterisk, StringComparison.Ordinal)).Split('\n');
        int at = Array.FindIndex(asteriskPage, r => r.EndsWith(asterisk, StringComparison.Ordinal));
        Assert.EndsWith(beforeAsterisk, asteriskPage[at - 1], StringComparison.Ordinal);

        var model = result.Listing!;
        Assert.True(model.Lines.Single(l => l.Text == slant).PageBreakBefore);
        Assert.False(model.Lines.Single(l => l.Text == asterisk).PageBreakBefore);
        Assert.Equal(ListingLineKind.PageEjectComment, model.Lines.Single(l => l.Text == slant).Kind);
        Assert.Equal(ListingLineKind.Comment, model.Lines.Single(l => l.Text == asterisk).Kind);
    }

    [Fact]
    public void DateCompiled_TheHeading_AndWhenCompiled_DenoteTheCompilationInstant()
    {
        var before = DateTimeOffset.Now;
        var (result, source, _) = Compile(new ListingRequest());
        var after = DateTimeOffset.Now;
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = result.Listing!;
        var instant = listing.Header.CompiledAt;
        Assert.InRange(instant, before, after);

        // §1985:VI.3.4.4 GR1: the paragraph is replaced by "DATE-COMPILED. current date." — printed in place of the written
        // paragraph, from the paragraph word on; the comment-entry's further line is replaced with it and not printed.
        string[] main = LinesOf(source);
        string written = main[2];
        string replacement = written[..written.IndexOf("DATE-COMPILED", StringComparison.Ordinal)]
            + "DATE-COMPILED. " + instant.ToString("yyyy'-'MM'-'dd HH':'mm':'ss zzz", CultureInfo.InvariantCulture) + ".";
        var head = listing.Lines.Single(l => l.Inclusion is null && l.FileLine == 3);
        Assert.Equal((ListingReplacement.DateCompiled, replacement, written), (head.Replacement, head.Replaced, head.Text));
        Assert.Equal(ListingReplacement.DateCompiledEntry, listing.Lines.Single(l => l.Inclusion is null && l.FileLine == 4).Replacement);
        string text = File.ReadAllText(Path.Combine(_dir, "outer85.lst"));
        Assert.Contains(replacement, text, StringComparison.Ordinal);
        Assert.DoesNotContain(main[3].TrimEnd(), text, StringComparison.Ordinal);
        Assert.DoesNotContain("NOT YET", text, StringComparison.Ordinal);
        // The heading carries the same instant.
        Assert.Contains(instant.ToString("yyyy'-'MM'-'dd HH':'mm':'ss zzz", CultureInfo.InvariantCulture), text.Split('\n')[0], StringComparison.Ordinal);

        // §1989:A.2.46.3 RV1 positions 1-21 of the instant, and RV3: the program's WHEN-COMPILED denotes the same time.
        var (ok, stdout, detail) = CutRunner.Run(Path.Combine(_dir, "outer85.dll"), _dir);
        Assert.True(ok, detail);
        Assert.Equal(ListingHookCases.WhenCompiledOf(instant), stdout.Trim());
    }

    [Fact]
    public void EachPageHeading_NamesTheInnermostProgramOfItsFirstLine()
    {
        // A page of the heading and three rows, so pages start inside each program.
        var (result, source, _) = Compile(new ListingRequest(PageLength: ListingRequest.TextHeadingLines + 3));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        string[] main = LinesOf(source);
        // From the source text: INNER85 is the lines from its IDENTIFICATION DIVISION header (21) to its END PROGRAM (26).
        int innerFirst = Array.FindIndex(main, l => l.Contains("PROGRAM-ID. INNER85", StringComparison.Ordinal));   // 0-based: line 21
        int innerLast = Array.FindIndex(main, l => l.Contains("END PROGRAM INNER85", StringComparison.Ordinal)) + 1;
        string ProgramOfSourceLine(int fileLine) => fileLine >= innerFirst && fileLine <= innerLast ? "INNER85" : "OUTER85";

        var listing = result.Listing!;
        Assert.Equal([("OUTER85", 0), ("INNER85", 1)], listing.Programs.Select(p => (p.Name, p.Depth)));
        foreach (var line in listing.Lines.Where(l => l.Inclusion is null))
            Assert.Equal(ProgramOfSourceLine(line.FileLine), listing.Programs.Single(p => p.Id == line.Program).Name);

        string[] all = File.ReadAllText(Path.Combine(_dir, "outer85.lst")).Split(PageComposer.FormFeed);
        // The diagnostics summary ends the listing on a page of its own, naming the first outermost program (section 6.1); the
        // pages before it are the source section's.
        Assert.Equal("DIAGNOSTICS", all[^1].Split('\n')[1]);
        Assert.Contains("(--std 85)  OUTER85  ", all[^1].Split('\n')[0], StringComparison.Ordinal);
        string[] pages = all[..^1];
        Assert.True(pages.Length > 4, $"only {pages.Length} pages");
        int checkedPages = 0;
        foreach (string page in pages)
        {
            string[] rows = page.Split('\n');
            // The first row of the page that is a numbered source row: its listing number names its line.
            string? first = rows.Skip(ListingRequest.TextHeadingLines).FirstOrDefault(r => r.Length > 6 && int.TryParse(r[..6], out _));
            if (first is null) continue;
            var line = listing.Lines[int.Parse(first[..6], CultureInfo.InvariantCulture) - 1];
            string program = listing.Programs.Single(p => p.Id == line.Program).Name;
            Assert.Contains($"(--std 85)  {program}  ", rows[0], StringComparison.Ordinal);
            checkedPages++;
        }
        Assert.Contains(pages, p => p.Split('\n')[0].Contains("  INNER85  ", StringComparison.Ordinal));
        Assert.True(checkedPages == pages.Length, $"{pages.Length - checkedPages} page(s) held no source row");
    }

    /// <summary>A listing is produced on every outcome once the source was read (DESIGN-listing.md section 4.6), and a section
    /// that cannot be built says so; a source that does not exist gives no listing.</summary>
    [Fact]
    public void AFailedCompilation_StillListsItsSource()
    {
        // A front-end error: a syntax error in the procedure division.
        var (frontend, source, _) = Compile(new ListingRequest(), Main.Replace("STOP RUN.", "STOP RUN RUN RUN.", StringComparison.Ordinal));
        Assert.Equal(CompilerDriver.Outcome.FrontendError, frontend.Status);
        Assert.Equal(ListingOutcome.FrontendError, frontend.Listing!.Header.Outcome);
        Assert.Equal(LinesOf(source).Length + 2 * LinesOf(Path.Combine(_dir, "REC85.cpy")).Length, frontend.Listing.Lines.Count);
        Assert.Contains(frontend.Listing.Programs, p => p.Name == "OUTER85");
        // DESIGN-listing.md section 4.3: the programs are read off the tree the parser built, recovered or not.
        Assert.Equal([(ListingSection.Source, true, (string?)null), (ListingSection.Programs, true, null), (ListingSection.Diagnostics, true, null)],
            frontend.Listing.Sections.Select(s => (s.Section, s.Produced, s.Reason)));
        Assert.True(File.Exists(Path.Combine(_dir, "outer85.lst")));

        // A bind error: a reference to a name the program does not declare.
        var (bind, _, _) = Compile(new ListingRequest(), Main.Replace("DISPLAY WC.", "DISPLAY NO-SUCH-NAME.", StringComparison.Ordinal));
        Assert.Equal(CompilerDriver.Outcome.BindError, bind.Status);
        Assert.Equal(ListingOutcome.BindError, bind.Listing!.Header.Outcome);
        Assert.Equal([(ListingSection.Source, true, (string?)null), (ListingSection.Programs, true, null), (ListingSection.Diagnostics, true, null)],
            bind.Listing.Sections.Select(s => (s.Section, s.Produced, s.Reason)));

        // No source: nothing was read, so nothing is listed.
        var missing = CompiledProgramCache.CompileCold(new CompilerDriver.Options(Path.Combine(_dir, "absent.cob"), DialectLevel: 85,
            Listing: new ListingRequest()));
        Assert.Equal(CompilerDriver.Outcome.SourceNotFound, missing.Status);
        Assert.Null(missing.Listing);
    }
}
