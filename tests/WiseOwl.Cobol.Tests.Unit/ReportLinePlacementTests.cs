// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <see cref="ReportLinePlacement"/> is the one line-placement arithmetic of a report group (kb/Work PB1003): the run-time
/// engine (<c>CobolReport.FirstLineTarget</c>, <c>SubsequentTarget</c>, <c>ApplyNextGroup</c>) and the binder's
/// compile-time screens (<c>ReportSectionBinder.MinimumLastLine</c>) both ask it, so this one test pins the rule for both.
/// Each expected value is the cited rule's, on the page of the PAGE clause <c>PAGE LIMIT 12 HEADING 2 FIRST DETAIL 4
/// LAST DETAIL 8 FOOTING 9</c>.
/// </summary>
public sealed class ReportLinePlacementTests
{
    private static readonly ReportPageRegions Page = new(paged: true, heading: 2, firstDetail: 4, footing: 9);
    private static readonly ReportPageRegions Unpaged = new(paged: false, heading: 0, firstDetail: 0, footing: 0);

    /// <summary>§2023:13.18.35.4 GR5: the first line of each group type, absolute and relative, with and without the
    /// group GR5 places it after.</summary>
    [Theory]
    // GR5a — an absolute first line is integer-1 whatever precedes it.
    [InlineData(ReportGroupKind.ReportHeading, ReportLineKind.Absolute, 3, false, null, 3)]
    [InlineData(ReportGroupKind.ReportFooting, ReportLineKind.Absolute, 11, false, 10L, 11)]
    // GR5b1 — a report heading: HEADING + integer-2 − 1.
    [InlineData(ReportGroupKind.ReportHeading, ReportLineKind.Relative, 1, false, null, 2)]
    // GR5b2 — a page heading: after the report heading's final LINE-COUNTER when one is on the page, else HEADING + n − 1.
    [InlineData(ReportGroupKind.PageHeading, ReportLineKind.Relative, 1, false, null, 2)]
    [InlineData(ReportGroupKind.PageHeading, ReportLineKind.Relative, 1, false, 3L, 4)]
    // GR5b3 — a body group: FIRST DETAIL when it is the first on the page, whatever integer-2 says; else LINE-COUNTER + n.
    [InlineData(ReportGroupKind.Detail, ReportLineKind.Relative, 3, false, null, 4)]
    [InlineData(ReportGroupKind.Detail, ReportLineKind.Relative, 3, false, 5L, 8)]
    [InlineData(ReportGroupKind.ControlFooting, ReportLineKind.Relative, 1, false, 6L, 7)]
    // GR5b4 — a page footing: FOOTING + integer-2, whatever body group precedes it.
    [InlineData(ReportGroupKind.PageFooting, ReportLineKind.Relative, 1, false, null, 10)]
    [InlineData(ReportGroupKind.PageFooting, ReportLineKind.Relative, 1, false, 7L, 10)]
    // GR5b5 / §1985:XIII.3.10.10 GR3b — a report footing: after the page footing's final LINE-COUNTER, else FOOTING + n.
    [InlineData(ReportGroupKind.ReportFooting, ReportLineKind.Relative, 2, false, null, 11)]
    [InlineData(ReportGroupKind.ReportFooting, ReportLineKind.Relative, 3, false, 10L, 13)]
    // §2023:13.18.57.4 GR7f — the bare ON NEXT PAGE of a report footing: on a page by itself, at the HEADING integer.
    [InlineData(ReportGroupKind.ReportFooting, ReportLineKind.Relative, 0, true, 10L, 2)]
    public void FirstLine_IsTheGroupTypesGr5Placement(ReportGroupKind group, ReportLineKind kind, int value, bool nextPage,
        long? preceding, long expected) =>
        Assert.Equal(expected, ReportLinePlacement.FirstLine(group, kind, value, value, nextPage, Page, preceding));

    /// <summary>§2023:13.18.35.4 GR5c: a report not divided into pages places every relative first line, report heading and
    /// report footing included, at LINE-COUNTER + integer-2, from zero before the first group (kb/Work PB1247).</summary>
    [Theory]
    [InlineData(ReportGroupKind.ReportHeading, null, 1)]
    [InlineData(ReportGroupKind.ReportFooting, 7L, 9)]
    [InlineData(ReportGroupKind.Detail, 4L, 6)]
    public void FirstLine_Unpaged_IsLineCounterPlusInteger2(ReportGroupKind group, long? preceding, long expected)
    {
        int integer2 = group == ReportGroupKind.ReportHeading ? 1 : 2;
        Assert.Equal(expected, ReportLinePlacement.FirstLine(group, ReportLineKind.Relative, integer2, integer2, false, Unpaged,
            preceding));
    }

    /// <summary>A STEP line's first placement is its written integer-2 (<see cref="ReportGroupLine.RelativeBase"/>), not its
    /// displacement: there is no preceding occurrence to step from.</summary>
    [Fact]
    public void FirstLine_StepLine_PlacesByItsWrittenInteger2() =>
        Assert.Equal(7, ReportLinePlacement.FirstLine(ReportGroupKind.Detail, ReportLineKind.Step, value: 4, relativeBase: 2,
            nextPage: false, Page, preceding: 5));

    /// <summary>§2023:13.18.35.4 GR7 and §2023:13.18.38.4 GR12c/GR12d: a later line.</summary>
    [Theory]
    [InlineData(ReportLineKind.Absolute, 8, 0, 5, 0, 8)]     // GR7a — integer-1
    [InlineData(ReportLineKind.Relative, 2, 0, 5, 0, 7)]     // GR7b — the previous line + integer-2
    [InlineData(ReportLineKind.Step, 3, 1, 9, 4, 7)]         // GR12 — the anchor + the displacement, not the previous line
    [InlineData(ReportLineKind.Step, 3, 1, 9, 0, 10)]        // an unseeded anchor (GR2b) — re-anchors on the previous line
    public void SubsequentLine_IsGr7WithTheStepAnchor(ReportLineKind kind, int value, int relativeBase, long previous,
        long anchor, long expected) =>
        Assert.Equal(expected, ReportLinePlacement.SubsequentLine(kind, value, relativeBase, previous, anchor));

    /// <summary>The anchor a line seeds: its own line, or a re-anchoring STEP line's less its displacement, so the next
    /// occurrence lands its displacement below the first.</summary>
    [Theory]
    [InlineData(ReportLineKind.Relative, 2, 6, 6)]
    [InlineData(ReportLineKind.Step, 3, 10, 7)]
    public void AnchorSeed_IsTheFirstOccurrencesLine(ReportLineKind kind, int value, long target, long expected) =>
        Assert.Equal(expected, ReportLinePlacement.AnchorSeed(kind, value, target));

    /// <summary>§2023:13.18.37.4 GR3/GR5 and §1985:XIII.3.10.6 GR5, §1985:XIII.3.10.9 GR5: the LINE-COUNTER a report heading's
    /// or page footing's NEXT GROUP clause leaves after its last line 10.</summary>
    [Fact]
    public void AfterHeadingOrFootingNextGroup_IsTheNextGroupsLineCounter()
    {
        Assert.Equal(11, ReportLinePlacement.AfterHeadingOrFootingNextGroup(new ReportNextGroup(ReportNextGroupKind.Absolute, 11), 10));
        Assert.Equal(12, ReportLinePlacement.AfterHeadingOrFootingNextGroup(new ReportNextGroup(ReportNextGroupKind.Relative, 2), 10));
        Assert.Equal(10, ReportLinePlacement.AfterHeadingOrFootingNextGroup(null, 10));
    }
}
