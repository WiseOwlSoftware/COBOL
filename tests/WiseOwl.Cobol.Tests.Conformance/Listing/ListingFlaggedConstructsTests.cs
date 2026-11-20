// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Nodes;
using System.Xml.Linq;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The table of flagged constructs by edition in the diagnostic summary (DESIGN-listing.md section 6.11; kb/Work PB5947): a program
/// compiled at <c>--std 2023 --permissive --flag-extensions</c> that writes one construct of each kind the edition flags, in the front
/// end and in the binder: a fixed-form continuation line, which is obsolete at 2023 (§2023:4.2.13 over Annex F.2) and, splicing a
/// word, removed at 2023 (Annex E.2); a debugging line, removed at 2014; the CLOSE WITH LOCK phrase, removed at 2023; the EXIT PROGRAM
/// statement twice, archaic at 2023 (§2023:4.2.12 over Annex F.1); and USAGE COMP-3, a nonstandard extension the warning mechanism
/// flags (§2023:4.2.10). Under <c>--permissive</c> each removal is a warning that keeps the program compiling. The expected table is
/// read off the source the test writes (the line each construct stands on) and the compiler's registers (each construct's row: its
/// edition and description), never off a captured listing; the text listing, the JSON and the HTML page each hold it.
/// </summary>
public sealed class ListingFlaggedConstructsTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingflags");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. FLAGS1.",
        "       ENVIRONMENT DIVISION.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        "           SELECT F ASSIGN TO \"flags1.dat\".",
        "       DATA DIVISION.",
        "       FILE SECTION.",
        "       FD F.",
        "       01 R PIC X.",
        "       WORKING-STORAGE SECTION.",
        "       01 COUNTER PIC 9.",
        "       01 PACKED PIC 9(3) USAGE COMP-3.",
        "       PROCEDURE DIVISION.",
        "           MOVE 1 TO COUN",
        "      -    TER.",
        "      D    DISPLAY COUNTER.",
        "           CLOSE F WITH LOCK.",
        "           EXIT PROGRAM.",
        "           EXIT PROGRAM.",
        "           STOP RUN.",
    ];

    /// <summary>The 1-based lines of <see cref="Source"/> that satisfy <paramref name="written"/>.</summary>
    private static int[] LinesOf(Func<string, bool> written) =>
        [.. Source.Select((text, i) => (text, Line: i + 1)).Where(x => written(x.text)).Select(x => x.Line)];

    /// <summary>The table the source calls for: each construct with the edition its register row names and the lines it is written
    /// on, in the table's order (edition, kind, construct).</summary>
    private static IEnumerable<(int Edition, ListingFlagKind Kind, string Construct, string Description, int[] Lines)> Expected()
    {
        ConstructDialectStatus Row(string id) => ConstructRegistry.Find(id)!;
        var debugging = Row(Constructs.DebuggingLineRemoved2014);
        var col7 = Row(Constructs.Col7ContinuationObsolete2023);
        var exitProgram = Row(Constructs.ExitProgramArchaic2023);
        var closeLock = Row(Constructs.CloseWithLockRemoved2023);
        var wordContinuation = Row(Constructs.FixedFormWordContinuationRemoved2023);
        var comp3 = NonstandardExtensionRegister.Get(ExtensionIds.UsageComp3);
        int[] continuation = LinesOf(l => l.Length > 6 && l[6] == '-');
        return
        [
            (debugging.RemovedIn!.Value, ListingFlagKind.Removed, debugging.Id, debugging.Display, LinesOf(l => l.Length > 6 && l[6] == 'D')),
            (col7.ObsoleteIn!.Value, ListingFlagKind.Obsolete, col7.Id, col7.Display, continuation),
            (exitProgram.ObsoleteIn!.Value, ListingFlagKind.Obsolete, exitProgram.Id, exitProgram.Display, LinesOf(l => l.Contains("EXIT PROGRAM", StringComparison.Ordinal))),
            (closeLock.RemovedIn!.Value, ListingFlagKind.Removed, closeLock.Id, closeLock.Display, LinesOf(l => l.Contains("WITH LOCK", StringComparison.Ordinal))),
            (wordContinuation.RemovedIn!.Value, ListingFlagKind.Removed, wordContinuation.Id, wordContinuation.Display, continuation),
            (2023, ListingFlagKind.Extension, comp3.Id, comp3.Display, LinesOf(l => l.Contains("COMP-3", StringComparison.Ordinal))),
        ];
    }

    private CompilerDriver.Result Compile()
    {
        string source = Path.Combine(_dir, "flags1.cob");
        File.WriteAllText(source, string.Join("\n", Source) + "\n");
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, "flags1.dll"),
            DialectLevel: 2023, Permissive: true, FlagExtensions: true,
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json | ListingFormats.Html)));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return result;
    }

    [Fact]
    public void TheTable_CountsEachFlaggedConstruct_ByEdition()
    {
        var result = Compile();
        Assert.Equal(CompilerDriver.Outcome.Success, result.Status);
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        var byId = listing.Diagnostics.ToDictionary(d => d.Id);
        var expected = Expected().ToList();
        Assert.Equal(expected.Select(e => (e.Edition, e.Kind, e.Construct, e.Description, string.Join(" ", e.Lines))),
            listing.FlaggedConstructs.Select(c => (c.Edition, c.Kind, c.Construct, c.Description,
                string.Join(" ", c.Diagnostics.Select(id => byId[id].FileLine)))));
        // The main source is the only file, so a diagnostic's listing line is its file line.
        Assert.All(listing.FlaggedConstructs.SelectMany(c => c.Diagnostics), id => Assert.Equal(byId[id].FileLine, byId[id].Line));

        // The table is every edition flag the compilation reported, each once: the diagnostics of the three flag codes, as the
        // compiler printed them (each a warning under --permissive), and no other.
        string[] flagCodes = ["COBOL0902", "COBOL0903", "COBOL2894"];
        Assert.Equal(listing.Diagnostics.Where(d => flagCodes.Contains(d.Code)).Select(d => d.Id).Order(),
            listing.FlaggedConstructs.SelectMany(c => c.Diagnostics).Order());
        Assert.All(listing.Diagnostics.Where(d => flagCodes.Contains(d.Code)), d => Assert.Contains(
            result.Diagnostics, r => r.Code == d.Code && r.Message == d.Message && r.Severity == Frontend.Diagnostics.DiagnosticSeverity.Warning));

        // The text listing's summary ends with the table, in the words FlaggedConstructText composes of the expected values.
        string text = File.ReadAllText(Path.Combine(_dir, "flags1.lst"));
        string summary = string.Join("\n", ListingPages.Of(text, ListingSection.Diagnostics));
        string[] rows = summary.Split('\n');
        int title = Array.IndexOf(rows, FlaggedConstructText.Title);
        Assert.True(title > 0, "the summary prints no table of flagged constructs");
        string[] want = [.. expected.Select(e => FlaggedConstructText.EditionText(e.Edition) + " " + FlaggedConstructText.KindWord(e.Kind)
            + " " + e.Lines.Length + " " + e.Description + " (" + e.Construct + "); " + string.Join(" ", e.Lines))];
        string Squeezed(string s) => string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        // Each entry is its first row and the rows indented under it; the page heading of a later page is not part of it.
        var entries = new List<string>();
        foreach (string row in rows.Skip(title + 2))
        {
            if (row.Length == 0 || char.IsLetter(row[0])) break;
            if (row.StartsWith(new string(' ', 26), StringComparison.Ordinal) && entries.Count > 0) entries[^1] += " " + row.Trim();
            else entries.Add(row);
        }
        Assert.Equal(want, entries.Select(Squeezed));

        // The JSON holds the same table.
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(_dir, "flags1.lst.json")))!;
        Assert.Equal(expected.Select(e => (e.Edition, e.Kind.ToString(), e.Construct)),
            json["flaggedConstructs"]!.AsArray().Select(c => ((int)c!["edition"]!, (string)c["kind"]!, (string)c["construct"]!)));

        // The HTML page holds it in the same words, each line a link to its diagnostic under that line.
        var page = XDocument.Parse(File.ReadAllText(Path.Combine(_dir, "flags1.lst.html")), LoadOptions.PreserveWhitespace);
        var table = page.Descendants("h3").Single(h => h.Value == FlaggedConstructText.Title).ElementsAfterSelf("table").First();
        var pageRows = table.Element("tbody")!.Elements("tr").ToList();
        Assert.Equal(expected.Count, pageRows.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            var cells = pageRows[i].Elements("td").Select(td => td.Value).ToArray();
            Assert.Equal([FlaggedConstructText.EditionText(expected[i].Edition), FlaggedConstructText.KindWord(expected[i].Kind),
                expected[i].Lines.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                expected[i].Description + " (" + expected[i].Construct + ")", string.Join(" ", expected[i].Lines)], cells);
            Assert.Equal(listing.FlaggedConstructs[i].Diagnostics.Select(id => "#" + HtmlListingRenderer.DiagnosticId(id)),
                pageRows[i].Descendants("a").Select(a => (string?)a.Attribute("href")));
        }
    }

    /// <summary>The table is part of the diagnostics section: a listing without that section holds no table, and a compilation that
    /// flags nothing holds an empty one.</summary>
    [Fact]
    public void TheTable_IsTheDiagnosticsSections()
    {
        string source = Path.Combine(_dir, "quiet.cob");
        File.WriteAllText(source, string.Join("\n", Source) + "\n");
        var withoutSection = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, "quiet.dll"),
            DialectLevel: 2023, Permissive: true, FlagExtensions: true,
            Listing: new ListingRequest(Formats: ListingFormats.Json) { Sections = ListingSectionSet.Of(ListingSection.Source) }));
        Assert.Empty(Assert.IsType<ListingDocument>(withoutSection.Listing).FlaggedConstructs);

        File.WriteAllText(source, string.Join("\n", Source.Where((_, i) => i < 13 && i != 12).Append("       PROCEDURE DIVISION.").Append("           STOP RUN.")) + "\n");
        var quiet = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, "quiet.dll"),
            DialectLevel: 2023, FlagExtensions: true, Listing: new ListingRequest(Formats: ListingFormats.Json)));
        Assert.Equal(CompilerDriver.Outcome.Success, quiet.Status);
        var listing = Assert.IsType<ListingDocument>(quiet.Listing);
        Assert.Empty(listing.FlaggedConstructs);
        Assert.DoesNotContain(listing.Diagnostics, d => d.Flag is not null);
    }
}
