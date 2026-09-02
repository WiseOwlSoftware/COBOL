// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// THE drift gate for this assembly's partitioned test families (<see cref="TestPartitioning"/>, plan §11 A13).
/// </summary>
/// <remarks>
/// ⛔ A partitioned family FAILS OPEN. Delete one of <c>VersionMatrixTests_P0 … _P11</c> and a twelfth of the
/// version matrix simply never runs: no error, no red, just a smaller — and entirely plausible — test count on a
/// leg that already changes count every wave. This test is what makes that impossible, and it is shape-driven:
/// it discovers every family in the assembly by structure (an abstract generic base constrained to
/// <see cref="ITestPartitionSlot"/>), so a NEW partitioned family is covered the moment it is written, with no
/// registration step to forget. See <see cref="TestPartitionAudit"/> for the three ways a family can lose rows
/// and the check that catches each.
/// </remarks>
public sealed class TestPartitionCoverageDriftTests
{
    /// <summary>Every partitioned family's concrete partitions cover its full row set exactly once.</summary>
    [Fact]
    public void EveryPartitionedFamily_CoversItsRowSet_ExactlyOnce()
        => TestPartitionAudit.AssertClean(typeof(TestPartitionCoverageDriftTests).Assembly);
}
