// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>The text renderer's row rules (DESIGN-listing.md section 6.2): the source text's 80 positions, a longer line
/// wrapped onto further rows, a position being one character (a supplementary-plane character is one), and the marker
/// column's priority.</summary>
public sealed class TextListingRendererTests
{
    [Fact]
    public void Wrap_CutsAtEightyPositions()
    {
        string eighty = new('A', 80);
        Assert.Equal([eighty], TextListingRenderer.Wrap(eighty));
        Assert.Equal([eighty, "B"], TextListingRenderer.Wrap(eighty + "B"));
        Assert.Equal([eighty, eighty], TextListingRenderer.Wrap(eighty + eighty));
        Assert.Equal([""], TextListingRenderer.Wrap(""));
        // 79 ASCII characters and two supplementary-plane characters: 81 positions in 83 UTF-16 units.
        string wide = new string('A', 79) + "\U0001D11E\U0001D11E";
        Assert.Equal([new string('A', 79) + "\U0001D11E", "\U0001D11E"], TextListingRenderer.Wrap(wide));
    }

    private static ListingLine Line(ListingLineKind kind, bool omitted = false,
        ListingReplacement replacement = ListingReplacement.None) =>
        new(1, 1, 1, null, 0, ListingLineForm.Fixed, kind, "", omitted, true, false, null, replacement, null);

    [Fact]
    public void TheMarker_FollowsTheLinesKind_OmissionAndReplacementFirst()
    {
        Assert.Equal('/', TextListingRenderer.MarkerOf(Line(ListingLineKind.PageEjectComment)));
        Assert.Equal('*', TextListingRenderer.MarkerOf(Line(ListingLineKind.Comment)));
        Assert.Equal('D', TextListingRenderer.MarkerOf(Line(ListingLineKind.DebuggingLine)));
        Assert.Equal('-', TextListingRenderer.MarkerOf(Line(ListingLineKind.Continuation)));
        Assert.Equal('>', TextListingRenderer.MarkerOf(Line(ListingLineKind.Directive)));
        Assert.Equal(' ', TextListingRenderer.MarkerOf(Line(ListingLineKind.Source)));
        Assert.Equal('E', TextListingRenderer.MarkerOf(Line(ListingLineKind.Source, replacement: ListingReplacement.DateCompiled)));
        Assert.Equal('X', TextListingRenderer.MarkerOf(Line(ListingLineKind.Comment, omitted: true)));
        Assert.Equal([' ', '1', '9', '+'], new[] { 0, 1, 9, 10 }.Select(TextListingRenderer.DepthMark));
    }
}
