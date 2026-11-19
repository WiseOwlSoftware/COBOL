// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The pages of a text listing, by section (DESIGN-listing.md section 7.1): every page after the first begins with a form feed,
/// and the second line of a page's heading is its section's title (<see cref="ListingSections.Title"/>; the source
/// section's is followed by the source path). A test of one section reads that section's pages, so the sections printed beside
/// it (the options before the source, the program map and the statistics after it) never move what it checks.
/// </summary>
internal static class ListingPages
{
    /// <summary>The pages of <paramref name="text"/> that belong to <paramref name="section"/>, in order.</summary>
    public static string[] Of(string text, ListingSection section) =>
        [.. text.Split(PageComposer.FormFeed).Where(page => SectionOf(page) == section)];

    /// <summary>The section a page belongs to, read from its heading's second line.</summary>
    public static ListingSection SectionOf(string page)
    {
        string title = page.Split('\n')[1];
        foreach (var section in Enum.GetValues<ListingSection>())
            if (title == ListingSections.Title(section) || title.StartsWith(ListingSections.Title(section) + "  ", StringComparison.Ordinal))
                return section;
        Assert.Fail($"a page whose heading names no section: '{title}'");
        return default;
    }
}
