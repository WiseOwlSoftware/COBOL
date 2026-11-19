// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Xml;
using System.Xml.Linq;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Html;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The HTML listing (DESIGN-listing.md section 7.2) over the hand-built document that holds every member of the model
/// (<see cref="JsonListingWriterTests.EveryMember"/>). Each expected value is read from that document, never from a captured page:
/// the page is read back with an XML reader (the renderer writes well-formed markup), and each assertion finds an element by the id
/// the renderer's own id functions name and compares its text with the model's.
/// </summary>
public sealed class HtmlListingRendererTests
{
    internal static XDocument Read(string html)
    {
        using var reader = XmlReader.Create(new StringReader(html), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        return XDocument.Load(reader);
    }

    private static XDocument Page(ListingDocument document) => Read(HtmlListingRenderer.Render(document, new ListingRequest()));

    private static XElement ById(XDocument page, string id) =>
        page.Descendants().Single(e => (string?)e.Attribute("id") == id);

    private static string Span(XElement line, string cssClass) =>
        line.Elements("span").Single(e => (string?)e.Attribute("class") == cssClass).Value;

    private static bool HasClass(XElement element, string cssClass) =>
        ((string?)element.Attribute("class") ?? "").Split(' ').Contains(cssClass);

    /// <summary>Section 7.2: "One self-contained file ... no external resource". Every link is to an element of the page, every
    /// id is unique, nothing is loaded from elsewhere, and the page is well-formed (the XML reader would throw).</summary>
    [Fact]
    public void ThePage_IsSelfContained_AndEveryLinkLandsOnTheElementItNames()
    {
        var page = Page(JsonListingWriterTests.EveryMember());
        var ids = page.Descendants().Select(e => (string?)e.Attribute("id")).OfType<string>().ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        var hrefs = page.Descendants("a").Select(a => (string)a.Attribute("href")!).ToList();
        Assert.NotEmpty(hrefs);
        Assert.All(hrefs, href => Assert.Contains(href[1..], ids));
        Assert.All(hrefs, href => Assert.StartsWith("#", href, StringComparison.Ordinal));
        Assert.DoesNotContain(page.Descendants(), e => e.Attribute("src") is not null);
        Assert.DoesNotContain(page.Descendants(), e => e.Name.LocalName is "link" or "img" or "iframe" or "object" or "embed");
        string style = page.Descendants("style").Single().Value;
        Assert.DoesNotContain("url(", style, StringComparison.Ordinal);
        Assert.DoesNotContain("@import", style, StringComparison.Ordinal);
    }

    /// <summary>Every line of the document is a row with its own id, its number, its file line and its text as the model holds it:
    /// the written text, or for the DATE-COMPILED paragraph's first line the replacement printed in its place (§1985:VI.3.4.4 GR1).</summary>
    [Fact]
    public void EveryLine_IsARow_WithItsText()
    {
        var document = JsonListingWriterTests.EveryMember();
        var page = Page(document);
        foreach (var line in document.Lines)
        {
            var row = ById(page, HtmlListingRenderer.LineId(line.Number));
            Assert.Equal(line.Replacement == ListingReplacement.DateCompiled ? line.Replaced : line.Text, Span(row, "tx"));
            Assert.Equal(line.FileLine.ToString(System.Globalization.CultureInfo.InvariantCulture), Span(row, "fl"));
            Assert.Equal(line.PageBreakBefore, HasClass(row, "pb"));
        }
    }

    /// <summary>Section 7.2: "each inclusion is a collapsible block; suppressed and omitted lines are present but collapsed and
    /// greyed". Line 3 is inclusion 1's (a library text, not listed): it stands in inclusion 1's open block, inside a collapsed block
    /// that says why; the source's own lines stand outside every block.</summary>
    [Fact]
    public void AnInclusion_IsABlock_AndALineNotListed_IsCollapsedInIt()
    {
        var document = JsonListingWriterTests.EveryMember();
        var page = Page(document);
        var inclusion = ById(page, HtmlListingRenderer.InclusionId(1));
        Assert.Equal("details", inclusion.Name.LocalName);
        Assert.NotNull(inclusion.Attribute("open"));
        Assert.StartsWith("COPY REC OF LIB -> lib/REC.cpy (depth 1)", inclusion.Element("summary")!.Value, StringComparison.Ordinal);
        var line3 = ById(page, HtmlListingRenderer.LineId(3));
        var run = line3.Parent!;
        Assert.Equal("details", run.Name.LocalName);
        Assert.Null(run.Attribute("open"));
        Assert.Equal("1 line not listed (3)", run.Element("summary")!.Value);
        Assert.Same(inclusion, run.Parent);
        Assert.True(HasClass(line3, "off"));
        foreach (int number in new[] { 1, 2, 4 })
            Assert.DoesNotContain(ById(page, HtmlListingRenderer.LineId(number)).Ancestors(), e => e.Name.LocalName == "details");
    }

    /// <summary>A suppressed inclusion (§2023:7.2.3.4 GR4: the library text is not listed) is a collapsed block; an inclusion whose
    /// text is listed is open.</summary>
    [Fact]
    public void ASuppressedInclusion_IsCollapsed()
    {
        var document = JsonListingWriterTests.EveryMember();
        document = document with
        {
            Inclusions = [document.Inclusions[0] with { Suppress = true }, document.Inclusions[1]],
        };
        var inclusion = ById(Page(document), HtmlListingRenderer.InclusionId(1));
        Assert.Null(inclusion.Attribute("open"));
        Assert.EndsWith(" SUPPRESS", inclusion.Element("summary")!.Value, StringComparison.Ordinal);
    }

    /// <summary>Section 7.2: "each diagnostic is anchored on its line and listed in a summary panel that links to it". Diagnostic 1
    /// stands under line 1 with its severity, code, column and message, its column (8) marked in the line's text; the summary links
    /// to it; diagnostic 2, placed on no line, is in the summary with no link.</summary>
    [Fact]
    public void ADiagnostic_IsAnchoredUnderItsLine_AndTheSummaryLinksToIt()
    {
        var document = JsonListingWriterTests.EveryMember();
        var page = Page(document);
        var diagnostic = document.Diagnostics[0];
        var anchored = ById(page, HtmlListingRenderer.DiagnosticId(diagnostic.Id));
        Assert.Equal($"==> error {diagnostic.Code} (col {diagnostic.Column}): {diagnostic.Message}", anchored.Value);
        var line = ById(page, HtmlListingRenderer.LineId(diagnostic.Line!.Value));
        Assert.Same(line, anchored.ElementsBeforeSelf().Last());
        Assert.True(HasClass(line, "hasdg"));
        Assert.Equal(document.Lines[0].Text[diagnostic.Column!.Value - 1].ToString(), line.Descendants("mark").Single().Value);
        var summary = ById(page, HtmlListingRenderer.SectionId(ListingSection.Diagnostics));
        Assert.Contains(summary.Descendants("a"), a => (string?)a.Attribute("href") == "#" + HtmlListingRenderer.DiagnosticId(diagnostic.Id));
        var unplaced = summary.Descendants("tr").Single(tr => tr.Value.Contains(document.Diagnostics[1].Code, StringComparison.Ordinal));
        Assert.Empty(unplaced.Descendants("a"));
        Assert.Equal("-", unplaced.Elements("td").First().Value);
    }

    /// <summary>A diagnostic's column counts a character outside the Basic Multilingual Plane as one position (as every listing
    /// record's column does), so the mark falls on the character the column names, not on half of a surrogate pair.</summary>
    [Fact]
    public void TheMarkedColumn_CountsASupplementaryPlaneCharacter_AsOne()
    {
        var document = JsonListingWriterTests.EveryMember();
        const string Text = "  \U0001D11E X";
        document = document with
        {
            Lines = [document.Lines[0] with { Text = Text }, .. document.Lines.Skip(1)],
            Diagnostics = [document.Diagnostics[0] with { Column = 5 }, document.Diagnostics[1]],
        };
        var line = ById(Page(document), HtmlListingRenderer.LineId(1));
        Assert.Equal("X", line.Descendants("mark").Single().Value);
        Assert.Equal(Text, Span(line, "tx"));
    }

    /// <summary>Under a line a replacing action changed, each line of its compiled text (DESIGN-listing.md section 6.2); after a
    /// DISPLAY directive's line its transfer (§2023:7.3.12.4 GR1, GR2); under a statement's line, its object code, each stretch with
    /// its method and IL range, its instructions when they were read (section 6.9).</summary>
    [Fact]
    public void UnderALine_StandItsCompiledText_ItsDisplay_AndItsObjectCode()
    {
        var document = JsonListingWriterTests.EveryMember();
        var page = Page(document);
        var line = document.Lines[3];
        var row = ById(page, HtmlListingRenderer.LineId(line.Number));
        var after = row.ElementsAfterSelf().ToList();
        Assert.Equal(line.Replaced!.Split('\n'), after.Where(e => HasClass(e, "rp")).Select(e => Span(e, "tx")));
        Assert.Equal("DISPLAY: " + document.Displays.Single().Text, after.Single(e => HasClass(e, "dsp")).Value);
        var objectCode = after.Where(e => HasClass(e, "obj")).ToList();
        var ranges = document.ObjectCode!.Ranges.Where(r => r.Line == line.Number).ToList();
        Assert.Equal(ranges.Count, objectCode.Count);
        for (int i = 0; i < ranges.Count; i++)
        {
            var method = document.ObjectCode.Methods.Single(m => m.Id == ranges[i].Method);
            Assert.Equal($"col {ranges[i].Column}: {method.Type}::{method.Name}  IL_{ranges[i].Start:X4}-IL_{ranges[i].End:X4}",
                objectCode[i].Element("summary")!.Value);
            Assert.Equal(ranges[i].Instructions?.Count ?? 0, objectCode[i].Descendants("tr").Count());
        }
        Assert.Equal(document.ObjectCode.Ranges[0].Instructions!.Select(x => x.OpCode),
            objectCode[0].Descendants("tr").Select(tr => tr.Elements("td").ElementAt(1).Value));
    }

    /// <summary>The links between the listing's own records: a data description line's margin to its data map entry, an entry's facts
    /// to the entries they name, the object-code map's lines and methods, the verbs' lines.</summary>
    [Fact]
    public void TheRecords_LinkToEachOther()
    {
        var document = JsonListingWriterTests.EveryMember();
        var page = Page(document);
        var margin = ById(page, HtmlListingRenderer.LineId(1)).Elements("a").Single(a => (string?)a.Attribute("class") == "mg");
        Assert.Equal("#" + HtmlListingRenderer.DataItemId(document.Lines[0].Margin!.Item), (string?)margin.Attribute("href"));
        Assert.Equal("OFF=0 LEN=5 DISPLAY", margin.Value);
        var renames = ById(page, HtmlListingRenderer.DataItemId(2));   // REDEFINES #1, RENAMES #1 THRU #1
        Assert.Equal(3, renames.Descendants("a").Count(a => (string?)a.Attribute("href") == "#" + HtmlListingRenderer.DataItemId(1)));
        var file = ById(page, HtmlListingRenderer.FileEntryId(1));     // LINE 1, RECORDS 1, RECORD KEY REC (#1), ALTERNATE KEY N (#2), REPORTS #7
        Assert.Equal(["#L1", "#M1", "#M1", "#M2", "#M7"], file.Descendants("a").Select(a => (string)a.Attribute("href")!));
        var objectMap = ById(page, HtmlListingRenderer.SectionId(ListingSection.ObjectMap));
        Assert.Contains(objectMap.Descendants("a"), a => (string?)a.Attribute("href") == "#" + HtmlListingRenderer.MethodId(2));
        var statistics = ById(page, HtmlListingRenderer.SectionId(ListingSection.Statistics));
        Assert.Equal(2, statistics.Descendants("a").Count(a => (string?)a.Attribute("href") == "#" + HtmlListingRenderer.LineId(4)));
    }

    /// <summary>Each section the document holds is a block in the order every renderer presents them (<see cref="ListingSections.Order"/>),
    /// titled by its one title; a section that could not be produced says why (DESIGN-listing.md section 4.3).</summary>
    [Fact]
    public void EachSection_IsABlock_InTheOrder_AndOneNotProducedSaysWhy()
    {
        var document = JsonListingWriterTests.EveryMember();
        var page = Page(document);
        var blocks = page.Descendants("section").ToList();
        var expected = ListingSections.Order.Where(s => document.Sections.Any(status => status.Section == s)).ToList();
        Assert.Equal(expected.Select(HtmlListingRenderer.SectionId), blocks.Select(b => (string)b.Attribute("id")!));
        Assert.Equal(expected.Select(ListingSections.Title), blocks.Select(b => b.Element("h2")!.Value));
        foreach (var status in document.Sections.Where(s => !s.Produced))
            Assert.Equal("Not produced: " + status.Reason, ById(page, HtmlListingRenderer.SectionId(status.Section)).Element("p")!.Value);
    }

    /// <summary>The model's text is printed as text: the characters markup gives a meaning are escaped, in an element and in an
    /// attribute, so a source line holding them reads back as written and adds no element.</summary>
    [Fact]
    public void MarkupInTheSource_IsPrintedAsText()
    {
        var document = JsonListingWriterTests.EveryMember();
        const string Text = "MOVE \"<b>&amp;'\" TO X.";
        document = document with
        {
            Header = document.Header with { MainSource = "a\"<b>.cob" },
            Lines = [document.Lines[0] with { Text = Text, Diagnostics = [] }, .. document.Lines.Skip(1)],
        };
        var page = Page(document);
        Assert.Equal(Text, Span(ById(page, HtmlListingRenderer.LineId(1)), "tx"));
        Assert.Empty(page.Descendants("b"));
        Assert.StartsWith("a\"<b>.cob", page.Descendants("title").Single().Value, StringComparison.Ordinal);
    }
}
