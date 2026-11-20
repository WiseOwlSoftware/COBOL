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

    /// <summary>The pages <see cref="PageComposer.Compose"/> writes of <paramref name="rows"/>.</summary>
    private static string Composed(IEnumerable<ListingRow> rows, int pageLength, Func<int, string?, IReadOnlyList<string>> heading)
    {
        using var writer = new StringWriter();
        PageComposer.Compose(writer, rows, pageLength, heading);
        return writer.ToString();
    }

    [Fact]
    public void AMarkedRow_StartsAPage_WithAFormFeed()
    {
        var text = Composed(
            [new("a", false, "P"), new("b", true, "P"), new("c", false, "P")], pageLength: 0, Heading);
        Assert.Equal(["H1 P\n--\na\n", "H2 P\n--\nb\nc\n"], Pages(text));
    }

    /// <summary>A page holds its length, heading included: 2 heading lines and a length of 4 leave 2 rows a page.</summary>
    [Fact]
    public void AFullPage_StartsTheNext()
    {
        var text = Composed(
            [new("1", false, null), new("2", false, null), new("3", false, null), new("4", false, null), new("5", false, null)],
            pageLength: 4, Heading);
        Assert.Equal(["H1\n--\n1\n2\n", "H2\n--\n3\n4\n", "H3\n--\n5\n"], Pages(text));   // "H1 " written without its trailing space
    }

    /// <summary>The heading names the program of the page's first row that belongs to one (§1985:VI.3.3.4 GR1); a page
    /// whose rows belong to none names the last program before it.</summary>
    [Fact]
    public void TheHeading_NamesTheProgramOfThePagesFirstRow()
    {
        var text = Composed(
            [new("a", false, "OUTER"), new("b", true, null), new("c", false, "INNER"), new("d", true, null)], pageLength: 0, Heading);
        Assert.Equal(["H1 OUTER\n--\na\n", "H2 INNER\n--\nb\nc\n", "H3 INNER\n--\nd\n"], Pages(text));
    }

    /// <summary>The paginator streams (kb/Work PB5941): a page is written to the writer when it closes, before a row after it is
    /// read, so no section is held whole on its way to its file.</summary>
    [Fact]
    public void APage_IsWritten_BeforeTheRowsAfterItAreRead()
    {
        using var writer = new StringWriter();
        IEnumerable<ListingRow> Rows()
        {
            yield return new("a", false, "P");
            yield return new("b", true, "P");
            Assert.Equal("H1 P\n--\na\n", writer.ToString());   // page 1 closed when "b" began page 2
            yield return new("c", false, "P");
        }
        Assert.Equal(2, PageComposer.Compose(writer, Rows(), 0, Heading));
        Assert.Equal(["H1 P\n--\na\n", "H2 P\n--\nb\nc\n"], Pages(writer.ToString()));
    }

    /// <summary>An empty section is one page, its heading only; a break on the first row starts no empty page; trailing spaces
    /// are not written.</summary>
    [Fact]
    public void EdgeRows()
    {
        Assert.Equal("H1\n--\n", Composed([], 60, Heading));
        Assert.Equal("H1 P\n--\nx\n", Composed([new("x   ", true, "P")], 60, Heading));
    }
}
