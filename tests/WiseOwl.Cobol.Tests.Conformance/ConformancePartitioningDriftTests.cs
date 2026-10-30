// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ NO SERIAL COLLECTION OF THE CONFORMANCE LEG MAY GROW BACK INTO ITS WALL (kb/Work PB2527, PB4146): the conformance
/// suite's runner (<see cref="ConformanceSuiteTestsBase{TSlot}"/>) keeps at least its measured 24 partitions and the
/// optional-word subset sweep at least its measured 8, and the sweep's row is one spelling, never a whole format. No
/// bound on a partition's SIZE exists: <see cref="TestPartitionAudit"/> proves each partition holds its even share of
/// its family, so the suite and the sweep grow with no edit here and nothing turns red by count alone.
/// </summary>
/// <remarks>
/// xUnit v2 runs one test class as one serial collection (<see cref="TestPartitioning"/>). The w1033 gate diagnosis
/// measured the Conformance leg 32-wide for about 90-160 s and then 95-220 s at 1-4 threads, because the corpus ran as
/// 3 partitions of about 1,546 cases (246-282 s each) and the optional-word sweep as one 23-row class (131-152 s).
/// Replaying the measured durations onto 32 workers modeled the leg 247 s to 118 s with 24 corpus partitions, an 8-way
/// optional-word split and longest-first order (the order is the gate plan's, timed from the shared timings store:
/// <c>scripts/gate_plan.py</c>'s self-test arms).
/// <para>Why the floors and not a size bound (DESIGN-test-build-ci.md §3.11): a partition's time is the family's time
/// over P, and the leg's ideal wall is the leg's time over the host's T workers, so a partition stays under the wall
/// while P ≥ T × (the family's share of the leg). The family's rows set neither side: a suite that doubles doubles
/// every partition and the leg alike. A bound on rows per partition therefore guarded nothing the floor does not, and
/// turned red on growth alone.</para>
/// </remarks>
public sealed class ConformancePartitioningDriftTests
{
    /// <summary>The conformance suite's measured floor (kb/Work PB2527): fewer partitions made each a serial pole of
    /// the Conformance leg.</summary>
    private const int SuiteFloor = 24;

    /// <summary>The optional-word sweep's measured floor (kb/Work PB2527).</summary>
    private const int OptionalWordFloor = 8;

    [Fact]
    public void TheSuiteRunner_KeepsItsMeasuredPartitionFloor()
        => Assert.True(ConformanceSuiteTestsBase<Slot0>.Partitions >= SuiteFloor,
            $"ConformanceSuiteTestsBase.Partitions is {ConformanceSuiteTestsBase<Slot0>.Partitions}; the measured floor "
            + $"is {SuiteFloor} (kb/Work PB2527): fewer partitions make each a serial pole of the Conformance leg again.");

    /// <summary>The sweep keeps its floor, and its row is ONE spelling — <c>(format, mask)</c> — so a format's power
    /// set spreads across the partitions by the stride instead of landing whole in one of them (a format of seven
    /// optional words is 128 compiles).</summary>
    [Fact]
    public void TheOptionalWordSweep_KeepsItsMeasuredPartitionFloor_AndSlicesSpellings()
    {
        Assert.True(OptionalWordSubsetDriftTestsBase<Slot0>.Partitions >= OptionalWordFloor,
            $"OptionalWordSubsetDriftTestsBase.Partitions is {OptionalWordSubsetDriftTestsBase<Slot0>.Partitions}; the "
            + $"measured floor is {OptionalWordFloor} (kb/Work PB2527).");
        var rows = OptionalWordSubsetDriftTestsBase<Slot0>.AllCases().ToList();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.True(row is [string, int],
            $"an optional-word row is (format, mask), one spelling; ({string.Join(", ", row)}) is not"));
        Assert.Equal(rows.Count, rows.Select(r => ((string)r[0], (int)r[1])).Distinct().Count());
        Assert.True(rows.Count > rows.Select(r => r[0]).Distinct().Count(),
            "every optional-word row is a whole format again: the stride can no longer spread a format's spellings");
    }
}
