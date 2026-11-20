// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Xml;
using System.Xml.Linq;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The HTML page colours a line by its spans and computes none (DESIGN-listing.md section 7.2; kb/Work PB5949): each span of the
/// model is one element of its class around exactly the characters it covers, a diagnostic's marked column nests inside the span
/// that holds it, the text between spans is printed bare, and the text listing prints the line as written whatever its spans.
/// </summary>
public sealed class HtmlSyntaxSpanTests
{
    private static ListingDocument Document(ListingLine line) => JsonListingWriterTests.EveryMember() with { Lines = [line] };

    private static ListingLine Line(string text, IReadOnlyList<ListingSpan> spans, IReadOnlyList<int>? diagnostics = null) =>
        new(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, text, false, true, false, 0, ListingReplacement.None, null,
            diagnostics ?? [], null, [], spans);

    private static XElement TextOf(string page)
    {
        using var reader = XmlReader.Create(new StringReader(page), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var row = XDocument.Load(reader).Descendants().Single(e => (string?)e.Attribute("id") == HtmlListingRenderer.LineId(1));
        return row.Elements("span").Single(e => (string?)e.Attribute("class") == "tx");
    }

    [Fact]
    public void EachSpan_IsOneElementOfItsClass_AroundExactlyItsText()
    {
        //                    1234567890123456789012345
        const string text = "000100     MOVE A TO B-1.";
        var spans = new[]
        {
            new ListingSpan(1, 6, ListingSpanClass.SequenceArea), new ListingSpan(12, 4, ListingSpanClass.ReservedWord),
            new ListingSpan(17, 1, ListingSpanClass.UserWord), new ListingSpan(19, 2, ListingSpanClass.ReservedWord),
            new ListingSpan(22, 3, ListingSpanClass.UserWord), new ListingSpan(25, 1, ListingSpanClass.Separator),
        };
        var document = Document(Line(text, spans));
        var tx = TextOf(ListingRendering.Html(document, new ListingRequest(Formats: ListingFormats.Html)));
        Assert.Equal(text, tx.Value);
        Assert.Equal(spans.Select(s => (HtmlListingRenderer.SpanClass(s.Class), text.Substring(s.Column - 1, s.Length))),
            tx.Elements("span").Select(e => ((string)e.Attribute("class")!, e.Value)));
        // The text listing ignores the spans: the row is the line as written.
        Assert.Contains(text, ListingRendering.Text(document, new ListingRequest()), StringComparison.Ordinal);
    }

    [Fact]
    public void AMarkedColumn_NestsInsideTheSpanThatHoldsIt()
    {
        const string text = "MOVE AB TO C.";
        var document = Document(Line(text, [new ListingSpan(6, 2, ListingSpanClass.UserWord)], diagnostics: [1]))
            with { Diagnostics = [new ListingDiagnostic(1, "COBOL0001", ListingSeverity.Error, "e", 1, 1, 1, 7, [], null)] };
        var tx = TextOf(ListingRendering.Html(document, new ListingRequest(Formats: ListingFormats.Html)));
        var word = Assert.Single(tx.Elements("span"));
        Assert.Equal("AB", word.Value);
        Assert.Equal("B", Assert.Single(word.Elements("mark")).Value);
    }
}
