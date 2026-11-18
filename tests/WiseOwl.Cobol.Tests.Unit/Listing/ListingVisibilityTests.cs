// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// <see cref="ListingVisibility"/> over hand-built line lists, one fact per rule of DESIGN-listing.md section 4.5.
/// </summary>
public sealed class ListingVisibilityTests
{
    private static List<LineDraft> Lines(params PhysicalLineKind[] kinds) =>
    [
        .. kinds.Select((kind, i) => new LineDraft
        {
            Number = i + 1, File = 1, FileLine = i + 1, Inclusion = null, Depth = 0, Form = ListingLineForm.Fixed, Kind = kind,
            Text = "",
        }),
    ];

    /// <summary>§1985:IV.7.2.4: "The slant in the indicator area causes page ejection prior to printing the comment line in
    /// the listing of the source program"; an asterisk line prints "at the next available line position in the listing".</summary>
    [Fact]
    public void ASlantLine_StartsAPage_AndAnAsteriskLineDoesNot()
    {
        var lines = Lines(PhysicalLineKind.Source, PhysicalLineKind.Comment, PhysicalLineKind.PageEjectComment, PhysicalLineKind.Source);
        ListingVisibility.Apply(lines, [1]);
        Assert.Equal([false, false, true, false], lines.Select(l => l.PageBreakBefore));
    }

    /// <summary>At <c>--std 85</c> no directive exists, so every line is listed, whatever its kind.</summary>
    [Fact]
    public void EveryLine_IsListed()
    {
        var lines = Lines([.. Enum.GetValues<PhysicalLineKind>().Where(k => k != PhysicalLineKind.CommentEntryCandidate)]);
        ListingVisibility.Apply(lines, [1]);
        Assert.All(lines, l => Assert.True(l.Listed));
    }

    /// <summary>Each outermost program after the first starts a page (DESIGN-listing.md section 6.1), so a heading names one
    /// program (§1985:VI.3.3.4 GR1); the first starts none, whatever order the starts are handed in.</summary>
    [Fact]
    public void EachOutermostProgramAfterTheFirst_StartsAPage()
    {
        var lines = Lines(PhysicalLineKind.Source, PhysicalLineKind.Source, PhysicalLineKind.Source, PhysicalLineKind.Source,
            PhysicalLineKind.Source);
        ListingVisibility.Apply(lines, [5, 1, 3]);
        Assert.Equal([false, false, true, false, true], lines.Select(l => l.PageBreakBefore));
    }
}
