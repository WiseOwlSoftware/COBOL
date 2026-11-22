// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Nodes;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The sections step 5 of DESIGN-listing.md section 11 builds, on a real compilation: the program map (section 6.5: each source
/// unit's name, kind, nesting and listing lines, §1985:X.3.1.4 GR1 for the nested program), the statistics and verbs (section
/// 6.8), the options (section 6.4) and <c>--listing-sections</c> (section 8: one request, one document, section 13 item 2).
/// Every expected value is read off the files the test writes: the listing order of section 4.4 (each library text's lines after
/// its COPY statement's line) gives each listing line, and the verbs, kinds and counts are those of the lines written here.
/// </summary>
public sealed class ListingSectionsOfACompilationTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingsections");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>A program with a nested program, a comment line, and a library text of one statement and one comment line copied
    /// twice into the procedure division. Its statements, as written: MOVE (line 9), ADD (each copy's line 1), IF and the MOVE
    /// nested in it (line 11), CALL (line 13), STOP (line 14), EXIT (line 19).</summary>
    private static readonly string[] Main =
    [
        "000100 IDENTIFICATION DIVISION.",
        "000200 PROGRAM-ID. STATS85.",
        "000300 DATA DIVISION.",
        "000400 WORKING-STORAGE SECTION.",
        "000500 01 A PIC 9(4).",
        "000600* A COMMENT LINE",
        "000700 PROCEDURE DIVISION.",
        "000800 MAIN-PARA.",
        "000900     MOVE 1 TO A.",
        "001000     COPY STEP85.",
        "001100     IF A > 0 MOVE 2 TO A END-IF.",
        "001200     COPY STEP85.",
        "001300     CALL \"INNER85\".",
        "001400     STOP RUN.",
        "001500 IDENTIFICATION DIVISION.",
        "001600 PROGRAM-ID. INNER85.",
        "001700 PROCEDURE DIVISION.",
        "001800 INNER-PARA.",
        "001900     EXIT PROGRAM.",
        "002000 END PROGRAM INNER85.",
        "002100 END PROGRAM STATS85.",
    ];

    private static readonly string[] Step = ["000100     ADD 1 TO A.", "000200*    A COMMENT IN THE LIBRARY TEXT"];

    /// <summary>The source lines that hold a COPY statement.</summary>
    private static readonly int[] CopyLines = [10, 12];

    /// <summary>The listing line of source line <paramref name="line"/> (section 4.4): each library text's lines follow its COPY
    /// statement's line.</summary>
    private static int ListingOf(int line) => line + Step.Length * CopyLines.Count(c => c < line);

    /// <summary>The listing line of line <paramref name="line"/> of the library text copied by the COPY statement on source line
    /// <paramref name="copyLine"/>.</summary>
    private static int ListingOf(int copyLine, int line) => ListingOf(copyLine) + line;

    private CompilerDriver.Result Compile(ListingRequest request, IEnumerable<string>? main = null)
    {
        string source = Path.Combine(_dir, "stats85.cob");
        File.WriteAllText(source, string.Join("\n", main ?? Main));
        File.WriteAllText(Path.Combine(_dir, "STEP85.cpy"), string.Join("\n", Step));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, EditionInfo.Of(85), Path.Combine(_dir, "stats85.dll"), CopyPaths: [_dir], Listing: request));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return result;
    }

    /// <summary>Section 6.5: the outer program spans the listing (an outermost unit owns the lines around it, section 4.5); the
    /// nested program runs from its IDENTIFICATION DIVISION header to its END PROGRAM marker, one level deeper, contained in the
    /// outer one.</summary>
    [Fact]
    public void TheProgramMap_NamesEachProgram_ItsNesting_AndItsLines()
    {
        var result = Compile(new ListingRequest());
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = result.Listing!;
        int last = ListingOf(Main.Length);
        Assert.Equal(
            [(0, "STATS85", ListingProgramKind.Program, (int?)null, 0, 1, last),
             (1, "INNER85", ListingProgramKind.Program, 0, 1, ListingOf(15), ListingOf(20))],
            listing.Programs.Select(p => (p.Id, p.Name, p.Kind, p.Parent, p.Depth, p.FirstLine, p.LastLine)));
        // The text: a page of its own, each unit under the one containing it.
        string text = File.ReadAllText(Path.Combine(_dir, "stats85.lst"));
        string page = Assert.Single(ListingPages.Of(text, ListingSection.Programs));
        Assert.Contains($"     1  program   {ListingOf(15),6}  {ListingOf(20),6}    INNER85", page, StringComparison.Ordinal);
    }

    /// <summary>Section 6.8: each verb as written, its count and the listing line of each of its statements (a statement in a
    /// library text copied twice is placed in each copy); the statements, nested ones included; the programs; the lines by kind
    /// and by file; and the diagnostics by severity, counted from the compilation's own records.</summary>
    [Fact]
    public void TheStatistics_CountTheVerbsAsWritten_AndTheLinesByKindAndFile()
    {
        var result = Compile(new ListingRequest());
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var statistics = result.Listing!.Statistics!;
        var parse = statistics.Parse!;
        Assert.Equal(
            [("ADD", 2, (int[])[ListingOf(10, 1), ListingOf(12, 1)]),
             ("CALL", 1, [ListingOf(13)]),
             ("EXIT", 1, [ListingOf(19)]),
             ("IF", 1, [ListingOf(11)]),
             ("MOVE", 2, [ListingOf(9), ListingOf(11)]),
             ("STOP", 1, [ListingOf(14)])],
            parse.Verbs.Select(v => (v.Verb, v.Count, v.Lines.ToArray())));
        Assert.Equal(8, parse.Statements);
        Assert.Equal(2, parse.Programs);

        // Lines: the source's 21 and two copies of the library text's 2; one comment line in the source, one in each copy.
        int copied = Step.Length * CopyLines.Length;
        Assert.Equal(Main.Length + copied, statistics.Lines.Lines);
        Assert.Equal([(ListingLineKind.Source, Main.Length - 1 + CopyLines.Length), (ListingLineKind.Comment, 1 + CopyLines.Length)],
            statistics.Lines.Kinds.Where(k => k.Count > 0).Select(k => (k.Kind, k.Count)));
        Assert.Equal((0, copied, 0), (statistics.Lines.Omitted, statistics.Lines.Copied, statistics.Lines.Unlisted));
        Assert.Equal([(1, Main.Length, 0), (2, copied, copied)], statistics.Files.Select(f => (f.File, f.Lines.Lines, f.Lines.Copied)));

        Assert.Equal(Enum.GetValues<ListingSeverity>().Select(s => result.Diagnostics.Count(d => Severity(d.Severity) == s)),
            statistics.Diagnostics.Select(d => d.Count));
        string page = Assert.Single(ListingPages.Of(File.ReadAllText(Path.Combine(_dir, "stats85.lst")), ListingSection.Statistics));
        // Section 6.8's row: the verb in a 24-column field, its count in 7, then its lines.
        Assert.Contains($"{"ADD",-24}{2,7}  {ListingOf(10, 1)} {ListingOf(12, 1)}", page, StringComparison.Ordinal);
    }

    private static ListingSeverity Severity(Frontend.Diagnostics.DiagnosticSeverity severity) => severity switch
    {
        Frontend.Diagnostics.DiagnosticSeverity.Error => ListingSeverity.Error,
        Frontend.Diagnostics.DiagnosticSeverity.Warning => ListingSeverity.Warning,
        _ => ListingSeverity.Info,
    };

    /// <summary>The verbs are read off the parse tree, so they are counted when the bind refuses the program (section 6.8).</summary>
    [Fact]
    public void TheVerbs_AreCounted_WhenTheBindFails()
    {
        var result = Compile(new ListingRequest(), Main.Select(l => l.Replace("CALL \"INNER85\"", "DISPLAY NO-SUCH-NAME", StringComparison.Ordinal)));
        Assert.Equal(CompilerDriver.Outcome.BindError, result.Status);
        Assert.Equal(ListingParse.Parsed, result.Listing!.Header.Parse);   // the bind failed, the parse read the whole source
        var verbs = result.Listing.Statistics!.Parse!.Verbs;
        Assert.Equal([ListingOf(13)], verbs.Single(v => v.Verb == "DISPLAY").Lines);
        Assert.DoesNotContain(verbs, v => v.Verb == "CALL");
        Assert.True(result.Listing.Statistics.Diagnostics.Single(d => d.Severity == ListingSeverity.Error).Count > 0);
    }

    /// <summary>Sections 6.5 and 6.8 (kb/Work PB5925 review R1-1): a syntax error makes the parse recover, and the recovery may
    /// skip source, so the program map and the statistics hold only what it kept. The header says the parse recovered, in the
    /// model and in the JSON, and the program map's and the statistics' pages each say so.</summary>
    [Fact]
    public void TheProgramMap_AndTheStatistics_SaySo_WhenTheParseRecovered()
    {
        var result = Compile(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json),
            Main.Select(l => l.Replace("CALL \"INNER85\"", "MOVEE 2 TO A", StringComparison.Ordinal)));
        Assert.Equal(CompilerDriver.Outcome.FrontendError, result.Status);
        var listing = result.Listing!;
        Assert.Equal(ListingParse.Recovered, listing.Header.Parse);
        Assert.NotNull(listing.Statistics!.Parse);   // counted still: what the recovery kept
        string text = File.ReadAllText(Path.Combine(_dir, "stats85.lst"));
        string Flat(ListingSection section) =>
            string.Join(" ", Assert.Single(ListingPages.Of(text, section)).Split('\n').Select(r => r.Trim()));
        Assert.Contains(TextListingRenderer.RecoveredNote("the programs, statements and verbs"), Flat(ListingSection.Statistics), StringComparison.Ordinal);
        Assert.Contains(TextListingRenderer.RecoveredNote("the source units"), Flat(ListingSection.Programs), StringComparison.Ordinal);
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(_dir, "stats85.lst.json")))!.AsObject();
        Assert.Equal(nameof(ListingParse.Recovered), json["header"]!["parse"]!.GetValue<string>());
    }

    /// <summary>Section 6.4: each option as given, Specified, beside the options left at their defaults; the listing request's
    /// own options; and the compiler and the host.</summary>
    [Fact]
    public void TheOptions_ShowWhatWasGiven_AndWhatWasLeftAtItsDefault()
    {
        var result = Compile(new ListingRequest(PageLength: 0));
        var options = result.Listing!.Options.ToDictionary(o => o.Name);
        Assert.Equal(("85 (ANSI X3.23-1985)", ListingOptionOrigin.Specified), (options["--std"].Value, options["--std"].Origin));
        Assert.Equal((_dir, ListingOptionOrigin.Specified), (options["--copy"].Value, options["--copy"].Origin));
        Assert.Equal((Path.Combine(_dir, "stats85.dll"), ListingOptionOrigin.Specified), (options["--output"].Value, options["--output"].Origin));
        Assert.Equal(("off", ListingOptionOrigin.Default), (options["--permissive"].Value, options["--permissive"].Origin));
        Assert.Equal(("0", ListingOptionOrigin.Specified), (options["--listing-page-length"].Value, options["--listing-page-length"].Origin));
        Assert.Equal(("standard", ListingOptionOrigin.Default), (options["--listing-sections"].Value, options["--listing-sections"].Origin));
        Assert.Equal((result.Listing.Header.CompilerVersion, ListingOptionOrigin.Host), (options["compiler"].Value, options["compiler"].Origin));
        string page = Assert.Single(ListingPages.Of(File.ReadAllText(Path.Combine(_dir, "stats85.lst")), ListingSection.Options));
        // Section 6.4's row: the name in a 24-column field, the origin in 11, then the value.
        Assert.Contains($"{"--std",-24}{"specified",-11}85 (ANSI X3.23-1985)", page, StringComparison.Ordinal);
    }

    /// <summary>Section 8 and section 13 item 2: the document holds the sections asked for and no other, in every format; a
    /// section not asked for has no status, and the parts of the document that are its are empty. The statistics count every line
    /// whether or not the source section is asked for.</summary>
    [Fact]
    public void OnlyTheSectionsAskedFor_AreInTheDocument_AndInEveryFormat()
    {
        var statisticsOnly = Compile(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json)
            { Sections = ListingSectionSet.Of(ListingSection.Statistics) }).Listing!;
        Assert.Equal([ListingSection.Statistics], statisticsOnly.Sections.Select(s => s.Section));
        Assert.Empty(statisticsOnly.Lines);
        Assert.Empty(statisticsOnly.Diagnostics);
        Assert.Empty(statisticsOnly.Options);
        Assert.Equal(2, statisticsOnly.Programs.Count);   // the lines and every page heading refer to them
        Assert.Equal(Main.Length + Step.Length * CopyLines.Length, statisticsOnly.Statistics!.Lines.Lines);
        string text = File.ReadAllText(Path.Combine(_dir, "stats85.lst"));
        Assert.Equal(text.Split(PageComposer.FormFeed), ListingPages.Of(text, ListingSection.Statistics));
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(_dir, "stats85.lst.json")))!.AsObject();
        Assert.Empty(json["lines"]!.AsArray());
        Assert.True(json.ContainsKey("statistics"));

        var sourceOnly = Compile(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json)
            { Sections = ListingSectionSet.Of(ListingSection.Source) }).Listing!;
        Assert.Equal([ListingSection.Source], sourceOnly.Sections.Select(s => s.Section));
        Assert.Equal(Main.Length + Step.Length * CopyLines.Length, sourceOnly.Lines.Count);
        Assert.Null(sourceOnly.Statistics);
        Assert.Empty(sourceOnly.Options);
        Assert.False(JsonNode.Parse(File.ReadAllText(Path.Combine(_dir, "stats85.lst.json")))!.AsObject().ContainsKey("statistics"));
        text = File.ReadAllText(Path.Combine(_dir, "stats85.lst"));
        Assert.Equal(text.Split(PageComposer.FormFeed), ListingPages.Of(text, ListingSection.Source));
    }

    /// <summary>Without the diagnostics section no diagnostic is placed on a line or printed, and the statistics still count them.</summary>
    [Fact]
    public void WithoutTheDiagnosticsSection_NoDiagnosticIsPrinted_ButEachIsCounted()
    {
        var result = Compile(new ListingRequest { Sections = ListingSectionSet.Of(ListingSection.Source, ListingSection.Statistics) },
            Main.Select(l => l.Replace("CALL \"INNER85\"", "DISPLAY NO-SUCH-NAME", StringComparison.Ordinal)));
        var listing = result.Listing!;
        Assert.Empty(listing.Diagnostics);
        Assert.All(listing.Lines, l => Assert.Empty(l.Diagnostics));
        Assert.Equal(result.Diagnostics.Count, listing.Statistics!.Diagnostics.Sum(d => d.Count));
        Assert.DoesNotContain(TextListingRenderer.Arrow, File.ReadAllText(Path.Combine(_dir, "stats85.lst")), StringComparison.Ordinal);
    }
}
