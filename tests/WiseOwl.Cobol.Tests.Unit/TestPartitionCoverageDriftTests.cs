// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// THE drift gate for this assembly's partitioned test families (<see cref="TestPartitioning"/>, plan §11 A13).
/// </summary>
/// <remarks>
/// ⛔ A partitioned family FAILS OPEN. Delete one of <c>StorageFormNistEquivalenceTests_P0 … _P7</c> and an eighth
/// of the NIST corpus silently leaves the StorageForm prove-then-delete gate — with nothing red, and a test count
/// that merely drops by one. This test is what makes that impossible, and it is shape-driven: it discovers every
/// family in the assembly by structure (an abstract generic base constrained to <see cref="ITestPartitionSlot"/>),
/// so a NEW partitioned family is covered the moment it is written, with no registration step to forget.
/// </remarks>
public sealed class TestPartitionCoverageDriftTests
{
    /// <summary>Every partitioned family's concrete partitions cover its full row set exactly once.</summary>
    [Fact]
    public void EveryPartitionedFamily_CoversItsRowSet_ExactlyOnce()
        => TestPartitionAudit.AssertClean(typeof(TestPartitionCoverageDriftTests).Assembly);

    /// <summary>The audit's share check (kb/Work PB4146) fires on a planted partition that holds more than its even
    /// share and on one that holds less, and passes the stride's own split: 10 rows over 4 partitions is 3, 3, 2, 2,
    /// a share of 2..3 each.</summary>
    [Fact]
    public void TheShareCheck_NamesEveryPartitionOutsideItsEvenShare()
    {
        Assert.Equal((2, 3), TestPartitionAudit.Share(10, 4));
        int[] stride =
            [.. Enumerable.Range(0, 4).Select(k => TestPartitioning.Slice(Enumerable.Range(0, 10), k, 4).Count())];
        Assert.Equal([3, 3, 2, 2], stride);
        Assert.Empty(TestPartitionAudit.OutsideTheirShare(10, [.. stride.Select((n, k) => ($"P{k}", n))]));

        var outside = TestPartitionAudit.OutsideTheirShare(10, [("P0", 4), ("P1", 3), ("P2", 2), ("P3", 1)]);
        Assert.Equal([("P0", 4), ("P3", 1)], outside);
    }
}
