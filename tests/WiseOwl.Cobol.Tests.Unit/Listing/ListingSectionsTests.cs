// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The listing's sections (DESIGN-listing.md section 8, <c>--listing-sections</c>): one enum, <see cref="ListingSection"/>; one
/// table of spellings, <see cref="ListingSections"/>; and the set a request holds, <see cref="ListingSectionSet"/>, whose
/// <c>all</c> is every member of the enum. Every switch over the enum is called for every member here, so a section added to the
/// enum without its spelling, its title or its place in the text listing's order is red.
/// </summary>
public sealed class ListingSectionsTests
{
    private static readonly ListingSection[] Every = Enum.GetValues<ListingSection>();

    [Fact]
    public void EverySection_HasOneSpelling_OneTitle_AndOnePlaceInTheTextOrder()
    {
        Assert.Equal(Every.Length, Every.Select(ListingSections.Spelling).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Every.Length, Every.Select(TextListingRenderer.TitleOf).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Every.Order(), TextListingRenderer.SectionOrder.Order());
        Assert.DoesNotContain(Every.Select(ListingSections.Spelling), s => s is ListingSections.StandardSpelling or ListingSections.AllSpelling);
    }

    /// <summary>Section 8's lists: <c>standard</c> is the source, the diagnostics, the options, the program map and the
    /// statistics; <c>all</c> is every section.</summary>
    [Fact]
    public void StandardAndAll_AreTheDesignsSets()
    {
        Assert.Equal([ListingSection.Source, ListingSection.Programs, ListingSection.Diagnostics, ListingSection.Options, ListingSection.Statistics],
            ListingSectionSet.Standard.Each());
        Assert.Equal(Every, ListingSectionSet.All.Each());
        Assert.Equal(Every.Length, ListingSectionSet.All.Count);
        Assert.Equal(ListingSectionSet.Standard, new ListingRequest().Sections);
    }

    public static TheoryData<string> EverySpelling() => [.. Enum.GetValues<ListingSection>().Select(ListingSections.Spelling)];

    [Theory]
    [MemberData(nameof(EverySpelling))]
    public void EachSpelling_ParsesToItsSection_IgnoringCaseAndSpaces(string spelling)
    {
        var section = Every.Single(s => ListingSections.Spelling(s) == spelling);
        Assert.True(ListingSections.TryParse(" " + spelling.ToUpperInvariant() + " ", out var set, out string? problem), problem);
        Assert.Equal(ListingSectionSet.Of(section), set);
        Assert.Equal(spelling, set.ToString());
    }

    [Fact]
    public void AList_IsTheUnionOfItsWords()
    {
        Assert.True(ListingSections.TryParse("source,statistics", out var set, out _));
        Assert.Equal([ListingSection.Source, ListingSection.Statistics], set.Each());
        Assert.Equal("source,statistics", set.ToString());
        Assert.True(ListingSections.TryParse("standard,source", out set, out _));
        Assert.Equal(ListingSectionSet.Standard, set);
        Assert.Equal("standard", set.ToString());
        Assert.True(ListingSections.TryParse("all", out set, out _));
        Assert.Equal(ListingSectionSet.All, set);
    }

    [Theory]
    [InlineData("", "between each two commas")]
    [InlineData("source,,options", "between each two commas")]
    [InlineData("source,data", "(got 'data')")]
    [InlineData("xref", "(got 'xref')")]
    public void AnEmptyOrUnknownWord_IsRefused(string list, string why)
    {
        Assert.False(ListingSections.TryParse(list, out _, out string? problem));
        Assert.Contains(why, problem, StringComparison.Ordinal);
    }

    /// <summary>A request holds at least one section, whichever way it is built (DESIGN-listing.md section 4.2: a request is never one
    /// the listing would fail).</summary>
    [Fact]
    public void ARequest_RefusesNoSection()
    {
        Assert.NotNull(ListingRequest.SectionsProblem(default));
        Assert.Null(ListingRequest.SectionsProblem(ListingSectionSet.Of(ListingSection.Options)));
        Assert.Throws<ArgumentException>(() => new ListingRequest { Sections = default });
        Assert.Throws<ArgumentException>(() => new ListingRequest() with { Sections = default });
    }
}
