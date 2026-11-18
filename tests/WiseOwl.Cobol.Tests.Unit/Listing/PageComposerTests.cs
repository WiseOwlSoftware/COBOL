// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// <see cref="PageComposer"/> over hand-built rows (DESIGN-listing.md section 7.1): a page break before a marked row and when
/// the page is full, a form feed before every page after the first, the heading naming the program of the page's first row
/// that has one, LF line ends.
/// </summary>
public sealed class PageComposerTests
{
    private static IReadOnlyList<string> Heading(int page, string? program) => [$"H{page} {program}", "--"];

    private static string[] Pages(string text) => text.Split(PageComposer.FormFeed);

    [Fact]
    public void AMarkedRow_StartsAPage_WithAFormFeed()
    {
        var text = PageComposer.Compose(
            [new("a", false, "P"), new("b", true, "P"), new("c", false, "P")], pageLength: 0, Heading);
        Assert.Equal(["H1 P\n--\na\n", "H2 P\n--\nb\nc\n"], Pages(text));
    }

    /// <summary>A page holds its length, heading included: 2 heading lines and a length of 4 leave 2 rows a page.</summary>
    [Fact]
    public void AFullPage_StartsTheNext()
    {
        var text = PageComposer.Compose(
            [new("1", false, null), new("2", false, null), new("3", false, null), new("4", false, null), new("5", false, null)],
            pageLength: 4, Heading);
        Assert.Equal(["H1\n--\n1\n2\n", "H2\n--\n3\n4\n", "H3\n--\n5\n"], Pages(text));   // "H1 " written without its trailing space
    }

    /// <summary>The heading names the program of the page's first row that belongs to one (§1985:VI.3.3.4 GR1); a page
    /// whose rows belong to none names the last program before it.</summary>
    [Fact]
    public void TheHeading_NamesTheProgramOfThePagesFirstRow()
    {
        var text = PageComposer.Compose(
            [new("a", false, "OUTER"), new("b", true, null), new("c", false, "INNER"), new("d", true, null)], pageLength: 0, Heading);
        Assert.Equal(["H1 OUTER\n--\na\n", "H2 INNER\n--\nb\nc\n", "H3 INNER\n--\nd\n"], Pages(text));
    }

    /// <summary>An empty section is one page, its heading only; a break on the first row starts no empty page; trailing spaces
    /// are not written.</summary>
    [Fact]
    public void EdgeRows()
    {
        Assert.Equal("H1\n--\n", PageComposer.Compose([], 60, Heading));
        Assert.Equal("H1 P\n--\nx\n", PageComposer.Compose([new("x   ", true, "P")], 60, Heading));
    }
}
