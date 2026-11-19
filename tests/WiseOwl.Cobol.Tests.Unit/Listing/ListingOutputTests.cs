// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ THE ONE TABLE OF LISTING FORMATS IS TOTAL (DESIGN-listing.md sections 7 and 8): every member of
/// <see cref="ListingFormats"/> has a spelling, an extension and a renderer in <see cref="ListingOutput"/>, whose switches throw
/// on a member they do not know, so a format added to the enum without its row is red here. And the request's one rule for
/// formats (<see cref="ListingRequest.FormatsProblem"/>), which the command line and the request's constructor both ask.
/// </summary>
public sealed class ListingOutputTests
{
    public static TheoryData<ListingFormats> EveryFormat() => [.. Enum.GetValues<ListingFormats>()];

    [Theory]
    [MemberData(nameof(EveryFormat))]
    public void EveryFormat_HasASpellingAnExtensionAndARenderer(ListingFormats format)
    {
        Assert.True(ListingOutput.TryParse(ListingOutput.Spelling(format), out var parsed));
        Assert.Equal(format, parsed);
        Assert.True(ListingOutput.TryParse(ListingOutput.Spelling(format).ToUpperInvariant(), out _));
        Assert.StartsWith(".lst", ListingOutput.Extension(format), StringComparison.Ordinal);
        Assert.NotEmpty(ListingOutput.Render(format, JsonListingWriterTests.EveryMember(), new ListingRequest(Formats: format)));
        Assert.Equal(format, ListingOutput.All & format);
        Assert.Equal([format], ListingOutput.Each(format));
    }

    /// <summary>Each format's file and renderer are the design's (section 8): text is <c>BASE.lst</c>, JSON <c>BASE.lst.json</c>,
    /// and each renders the document through its own renderer.</summary>
    [Fact]
    public void TheFormats_AreTheDesignsFiles()
    {
        var document = JsonListingWriterTests.EveryMember();
        var request = new ListingRequest();
        Assert.Equal(".lst", ListingOutput.Extension(ListingFormats.Text));
        Assert.Equal(".lst.json", ListingOutput.Extension(ListingFormats.Json));
        Assert.Equal(TextListingRenderer.Render(document, request), ListingOutput.Render(ListingFormats.Text, document, request));
        Assert.Equal(JsonListingWriter.Write(document), ListingOutput.Render(ListingFormats.Json, document, request));
        Assert.Equal("text|json", ListingOutput.Spellings);
        Assert.Equal([ListingFormats.Text, ListingFormats.Json], ListingOutput.Each(ListingFormats.Json | ListingFormats.Text));
        Assert.False(ListingOutput.TryParse("html", out _));   // step 9 of the design's section 11 adds it, with its renderer
    }

    [Fact]
    public void ARequest_IsTextByDefault() => Assert.Equal(ListingFormats.Text, new ListingRequest().Formats);

    [Theory]
    [InlineData(null, 0, "at least one format")]
    [InlineData(null, 4, "one of text|json")]
    [InlineData("-", 3, "one format (got text, json)")]
    public void ARequestWithNoFormats_OrTwoOnStandardOutput_IsRefused(string? outputBase, int formats, string problem)
    {
        Assert.Contains(problem, ListingRequest.FormatsProblem(outputBase, (ListingFormats)formats), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => new ListingRequest(outputBase, Formats: (ListingFormats)formats));
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("out/x", 2)]
    [InlineData("-", 2)]
    [InlineData("-", 1)]
    public void ARequestOfKnownFormats_IsAccepted(string? outputBase, int formats)
    {
        Assert.Null(ListingRequest.FormatsProblem(outputBase, (ListingFormats)formats));
        Assert.Equal((ListingFormats)formats, new ListingRequest(outputBase, Formats: (ListingFormats)formats).Formats);
    }

    /// <summary>The request refuses a request the listing would fail however it is built (review R1-3): the constructor, an
    /// object initializer, a <c>with</c> expression and <see cref="ListingRequest.WithDestination"/> all ask the same rules.</summary>
    [Fact]
    public void ARequestTheListingWouldFail_IsRefused_HoweverItIsBuilt()
    {
        var two = new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json);
        var toStandardOutput = new ListingRequest(ListingRequest.StandardOutput);
        Assert.Throws<ArgumentException>(() => toStandardOutput.WithDestination(ListingRequest.StandardOutput, ListingFormats.Text | ListingFormats.Json));
        Assert.Throws<ArgumentException>(() => two.WithDestination(ListingRequest.StandardOutput, two.Formats));
        Assert.Throws<ArgumentException>(() => two.WithDestination(two.OutputBase, 0));
        Assert.Throws<ArgumentException>(() => new ListingRequest(PageLength: ListingRequest.MinimumPageLength - 1));
        Assert.Throws<ArgumentException>(() => new ListingRequest() { PageLength = -1 });
        Assert.Throws<ArgumentException>(() => new ListingRequest(PageWidth: ListingRequest.MinimumPageWidth - 1));
        Assert.Throws<ArgumentException>(() => new ListingRequest() with { PageWidth = ListingRequest.MaximumPageWidth + 1 });
        // and each rule's edge is accepted, by every route
        Assert.Equal(ListingFormats.Json, toStandardOutput.WithDestination(ListingRequest.StandardOutput, ListingFormats.Json).Formats);
        Assert.Equal("out/x", two.WithDestination("out/x", two.Formats).OutputBase);
        Assert.Equal(0, (two with { PageLength = 0 }).PageLength);
        Assert.Equal(ListingRequest.MinimumPageLength, new ListingRequest { PageLength = ListingRequest.MinimumPageLength }.PageLength);
        Assert.Equal(ListingRequest.MaximumPageWidth, new ListingRequest(PageWidth: ListingRequest.MaximumPageWidth).PageWidth);
        Assert.Equal(new ListingRequest("out/x", 0, 200, ListingFormats.Json), new ListingRequest("out/x", 0, 200).WithDestination("out/x", ListingFormats.Json));
    }

    /// <summary>The destination is one choice (listing step 2 review lead R2-1): the output base and the formats change together and
    /// are judged as the pair, so moving between two valid destinations never passes through an invalid one, and the page geometry
    /// is kept. The rule (FormatsProblem): standard output takes one format alone; a file base takes any non-empty set.</summary>
    [Fact]
    public void TheDestination_IsChangedAsOnePair()
    {
        var toStandardOutput = new ListingRequest(ListingRequest.StandardOutput, 0, 200);
        var both = toStandardOutput.WithDestination("out/x", ListingFormats.Text | ListingFormats.Json);
        Assert.Equal(new ListingRequest("out/x", 0, 200, ListingFormats.Text | ListingFormats.Json), both);
        var back = both.WithDestination(ListingRequest.StandardOutput, ListingFormats.Json);
        Assert.Equal(new ListingRequest(ListingRequest.StandardOutput, 0, 200, ListingFormats.Json), back);
    }
}
