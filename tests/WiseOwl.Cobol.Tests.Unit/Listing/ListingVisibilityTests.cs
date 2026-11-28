// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Compiler.Listing;
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
        ListingVisibility.Apply(lines, [1], DirectiveEffects.None);
        Assert.Equal([false, false, true, false], lines.Select(l => l.PageBreakBefore));
    }

    /// <summary>At <c>--std 85</c> no directive exists, so every line is listed, whatever its kind.</summary>
    [Fact]
    public void EveryLine_IsListed()
    {
        var lines = Lines([.. Enum.GetValues<PhysicalLineKind>().Where(k => k != PhysicalLineKind.CommentEntryCandidate)]);
        ListingVisibility.Apply(lines, [1], DirectiveEffects.None);
        Assert.All(lines, l => Assert.True(l.Listed));
    }

    /// <summary>Each outermost program after the first starts a page (DESIGN-listing.md section 6.1), so a heading names one
    /// program (§1985:VI.3.3.4 GR1); the first starts none, whatever order the starts are handed in.</summary>
    [Fact]
    public void EachOutermostProgramAfterTheFirst_StartsAPage()
    {
        var lines = Lines(PhysicalLineKind.Source, PhysicalLineKind.Source, PhysicalLineKind.Source, PhysicalLineKind.Source,
            PhysicalLineKind.Source);
        ListingVisibility.Apply(lines, [5, 1, 3], DirectiveEffects.None);
        Assert.Equal([false, false, true, false, true], lines.Select(l => l.PageBreakBefore));
    }

    private static DirectiveEffects Effects(IReadOnlyList<(int From, bool On)>? states = null, int[]? directives = null,
        int[]? pages = null, int[]? suppressed = null) =>
        new(states ?? [], (directives ?? []).ToHashSet(), (pages ?? []).ToHashSet(), (suppressed ?? []).ToHashSet());

    private static List<LineDraft> Sources(int count) => Lines([.. Enumerable.Repeat(PhysicalLineKind.Source, count)]);

    /// <summary>§2023:7.3.18.3 GR2: "The default LISTING directive is '>>LISTING ON'"; GR4: after OFF "source lines shall not be
    /// listed until a LISTING directive specifying or implying the ON phrase is encountered, with the exception that another
    /// LISTING OFF directive shall be listed"; GR5: after ON they are listed; GR3: "Each LISTING directive shall be listed, even
    /// if the listing is being suppressed by a LISTING directive". Lines: 1 source, 2 OFF, 3 source, 4 OFF, 5 source, 6 ON,
    /// 7 source.</summary>
    [Fact]
    public void ListingOff_StopsTheLinesAfterIt_EveryListingDirectiveIsListed_AndOnResumes()
    {
        var lines = Sources(7);
        ListingVisibility.Apply(lines, [1], Effects([(3, false), (5, false), (7, true)], directives: [2, 4, 6]));
        Assert.Equal([true, true, false, true, false, true, true], lines.Select(l => l.Listed));
    }

    /// <summary>Two state changes that govern from one line (a library text ending, §2023:7.2.3.4 GR5, then a LISTING directive
    /// right after it) act in the order given: the last one made wins.</summary>
    [Fact]
    public void ChangesFromOneLine_ActInTheOrderMade()
    {
        var lines = Sources(3);
        ListingVisibility.Apply(lines, [1], Effects([(2, true), (2, false)]));
        Assert.Equal([true, false, false], lines.Select(l => l.Listed));
    }

    /// <summary>§2023:7.2.3.4 GR4: "If the SUPPRESS phrase is specified, library text incorporated as a result of COPY statement
    /// processing is not listed. If a listing is being produced, the COPY statement itself is listed." A LISTING directive inside
    /// the suppressed text is not listed either: GR3's exception names suppression by a LISTING directive only.</summary>
    [Fact]
    public void SuppressedLibraryText_IsNotListed_ButItsCopyStatementIs()
    {
        var lines = Sources(4);
        ListingVisibility.Apply(lines, [1], Effects(directives: [3], suppressed: [2, 3]));
        Assert.Equal([true, false, false, true], lines.Select(l => l.Listed));
    }

    /// <summary>§2023:7.3.19.4 GR2: "a PAGE directive shall cause page ejection followed by listing of the PAGE directive"; under
    /// LISTING OFF the PAGE line is a source line GR4 stops, so it is not listed and ejects nothing (DESIGN-listing.md section
    /// 2.3). Lines: 1 source, 2 PAGE, 3 OFF, 4 PAGE, 5 source.</summary>
    [Fact]
    public void APageDirective_StartsAPage_OnlyWhereItIsListed()
    {
        var lines = Sources(5);
        ListingVisibility.Apply(lines, [1], Effects([(4, false)], directives: [3], pages: [2, 4]));
        Assert.Equal([true, true, true, false, false], lines.Select(l => l.Listed));
        Assert.Equal([false, true, false, false, false], lines.Select(l => l.PageBreakBefore));
    }

    /// <summary>A '/' comment line under LISTING OFF is not listed, so it ejects no page (DESIGN-listing.md section 2.3).</summary>
    [Fact]
    public void ASlantLineUnderListingOff_IsNotListed_AndStartsNoPage()
    {
        var lines = Lines(PhysicalLineKind.Directive, PhysicalLineKind.PageEjectComment, PhysicalLineKind.Source);
        ListingVisibility.Apply(lines, [1], Effects([(2, false)], directives: [1]));
        Assert.Equal([true, false, false], lines.Select(l => l.Listed));
        Assert.All(lines, l => Assert.False(l.PageBreakBefore));
    }
}
