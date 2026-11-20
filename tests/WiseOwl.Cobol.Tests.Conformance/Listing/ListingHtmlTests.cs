// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Xml;
using System.Xml.Linq;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The HTML listing end to end (DESIGN-listing.md sections 7.2 and 8): a compilation that asks for <c>html</c> writes
/// <c>BASE.lst.html</c>, the rendering of the one document the compiler built. Every expected value comes from the input files the
/// test wrote or from the rule cited, never from a captured page: the page is read back with an XML reader and its rows found by
/// the renderer's id functions.
/// <list type="bullet">
/// <item>§1985:IV.7.1, §1985:IV.7.4: every physical line of the source and of the library text is a row whose text is the file's own
/// line, the library text's rows in the inclusion's block, after the COPY statement's line;</item>
/// <item>a data description line's margin links to its data map entry, and a statement's line holds its object code;</item>
/// <item>a compilation the binder refuses still writes its page, the error under the line it names with the column marked.</item>
/// </list>
/// </summary>
public sealed class ListingHtmlTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listinghtml");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private const string Main = """
        000100 IDENTIFICATION DIVISION.
        000200 PROGRAM-ID. HTML85.
        000300 DATA DIVISION.
        000400 WORKING-STORAGE SECTION.
        000500 01 GROUP-A.
        000600     COPY HREC.
        000700 PROCEDURE DIVISION.
        000800 MAIN-PARA.
        000900     DISPLAY FIELD-1.
        001000     STOP RUN.
        """;

    private const string Rec = """
        000100     05 FIELD-1 PIC X(4) VALUE "HTML".
        000200*    A COMMENT IN THE LIBRARY TEXT
        """;

    private (CompilerDriver.Result Result, string Html, string Source, string Copybook) Compile(ListingRequest request, string main = Main)
    {
        string source = Path.Combine(_dir, "html85.cob"), copybook = Path.Combine(_dir, "HREC.cpy");
        File.WriteAllText(source, main);
        File.WriteAllText(copybook, Rec);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, "html85.dll"),
            DialectLevel: 85, CopyPaths: [_dir], Listing: request));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return (result, Path.GetFullPath(Path.Combine(_dir, "html85.lst.html")), source, copybook);
    }

    private static XDocument Read(string path)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        return XDocument.Load(reader);
    }

    private static XElement ById(XDocument page, string id) => page.Descendants().Single(e => (string?)e.Attribute("id") == id);

    private static bool IsRow(XElement element) => ((string?)element.Attribute("class") ?? "").Split(' ').Contains("ln");

    private static string TextOf(XElement row) => row.Elements("span").Single(e => (string?)e.Attribute("class") == "tx").Value;

    [Fact]
    public void EveryLine_IsARow_TheLibraryTextInItsBlock_AndTheRecordsLinked()
    {
        var request = new ListingRequest(Formats: ListingFormats.Html) { Sections = ListingSectionSet.All };
        var (result, html, source, copybook) = Compile(request);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Contains(html, result.OutputFiles);
        Assert.False(File.Exists(Path.Combine(_dir, "html85.lst")));   // html alone writes no text listing
        Assert.Equal(ListingRendering.Html(result.Listing!, request), File.ReadAllText(html));

        var page = Read(html);
        var rows = page.Descendants("div").Where(IsRow).ToList();
        string[] main = File.ReadAllText(source).Split('\n'), rec = File.ReadAllText(copybook).Split('\n');
        Assert.Equal([.. main.Take(6), .. rec, .. main.Skip(6)], rows.Select(TextOf));

        // The library text's rows are the inclusion's block, which its banner opens; the source's rows stand in no block.
        var inclusion = ById(page, HtmlListingRenderer.InclusionId(1));
        Assert.Equal(rec, inclusion.Descendants("div").Where(d => d.Attribute("id") is not null).Select(TextOf));
        Assert.StartsWith("COPY HREC -> ", inclusion.Element("summary")!.Value, StringComparison.Ordinal);
        Assert.DoesNotContain(rows.Take(6), r => r.Ancestors("details").Any());

        // FIELD-1's line links its margin to FIELD-1's data map entry, whose line links back.
        var field = rows[6];
        string href = (string)field.Elements("a").Single(a => (string?)a.Attribute("class") == "mg").Attribute("href")!;
        var entry = ById(page, href[1..]);
        Assert.Contains(entry.Elements("td"), td => td.Value == "FIELD-1");
        Assert.Contains(entry.Descendants("a"), a => (string?)a.Attribute("href") == "#" + (string)field.Attribute("id")!);

        // The DISPLAY statement's line holds its object code: a block with its instructions.
        var display = rows.Single(r => TextOf(r).Contains("DISPLAY FIELD-1", StringComparison.Ordinal));
        var code = display.ElementsAfterSelf().TakeWhile(e => !IsRow(e)).Where(e => (string?)e.Attribute("class") == "obj").ToList();
        Assert.NotEmpty(code);
        Assert.All(code, block => Assert.NotEmpty(block.Descendants("tr")));
    }

    /// <summary>A program the binder refuses (a reference to a data-name no data description declares) still writes its page: the
    /// error stands under the line it names and the column its record names (the written word's first character) is marked.</summary>
    [Fact]
    public void ARefusedProgram_WritesItsPage_WithTheErrorUnderItsLine()
    {
        string main = Main.Replace("DISPLAY FIELD-1.", "MOVE NOSUCH TO FIELD-1.", StringComparison.Ordinal);
        var request = new ListingRequest(Formats: ListingFormats.Html);
        var (result, html, source, _) = Compile(request, main);
        Assert.False(result.Success);
        var page = Read(html);
        var error = result.Listing!.Diagnostics.First(d => d.Severity == ListingSeverity.Error && d.Message.Contains("NOSUCH", StringComparison.Ordinal));
        var anchored = ById(page, HtmlListingRenderer.DiagnosticId(error.Id));
        var line = anchored.ElementsBeforeSelf().Last(e => (string?)e.Attribute("id") is { } id && id.StartsWith('L'));
        string written = File.ReadAllText(source).Split('\n').Single(l => l.Contains("NOSUCH", StringComparison.Ordinal));
        Assert.Equal(written, TextOf(line));
        Assert.Equal(written.IndexOf("NOSUCH", StringComparison.Ordinal) + 1, error.Column);
        Assert.Equal("N", line.Descendants("mark").Single().Value);
    }
}
