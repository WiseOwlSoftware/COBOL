// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The D-FRA split rule (docs/CONFORMANCE.md §3; kb/Work PB981) — <see cref="CobolVarGroup.FromContiguous"/>, the
/// ONE decomposition of a variable-length record read back from a file. A WRITE sends the record "as though it
/// were in fact contiguous with its neighbors" (ISO §8.5.1.11.2), so the read record carries no marker of where a
/// dynamic member ends; each component takes as many whole units as the record holds beyond the fixed material
/// still to come, up to its maximum. These pin the rule's three stated properties: an exact inverse of the
/// composer for ONE variable-length member wherever it sits, the EARLIER component taking the excess when there
/// are several, and a short record leaving every component empty.
/// </summary>
public sealed class CobolVarGroupContiguousTests
{
    // Record layout A X(3) · D dynamic (max 20) · B X(2): fixed run 5, D at fixed offset 3.
    private static CobolVarGroup Split(string record) =>
        CobolVarGroup.FromContiguous(record, 5, [3], [1], [20L]);

    [Fact]
    public void OneDynamicMember_InTheMiddle_IsTheExactInverseOfTheComposer()
    {
        var v = Split("ABC" + "HELLO" + "XY");
        Assert.Equal("ABCXY", v.Fixed);
        Assert.Equal("HELLO", v.Dyn(0));
    }

    [Fact]
    public void ARecordShorterThanTheFixedRun_LeavesTheComponentEmpty_AndTheFixedRunShort()
    {
        var v = Split("SHOR");
        Assert.Equal("SHOR", v.Fixed);
        Assert.Equal("", v.Dyn(0));
        Assert.True(v.HasDyn(0));
    }

    [Fact]
    public void AComponentStopsAtItsMaximum_AndTheFixedMaterialFollowsIt()
    {
        var v = CobolVarGroup.FromContiguous("ABC" + "0123456789" + "XY", 5, [3], [1], [4L]);
        Assert.Equal("0123", v.Dyn(0));
        Assert.Equal("ABC45", v.Fixed);   // the fixed material after D is read from D's end, left to right
    }

    [Fact]
    public void SeveralComponents_TheEarlierTakesTheExcess()
    {
        // K X(1) · D1 dynamic · D2 dynamic: fixed run 1, both at fixed offset 1.
        var v = CobolVarGroup.FromContiguous("K" + "AB" + "CD", 1, [1, 1], [1, 1], [10L, 10L]);
        Assert.Equal("K", v.Fixed);
        Assert.Equal("ABCD", v.Dyn(0));
        Assert.Equal("", v.Dyn(1));
    }

    [Fact]
    public void ATableComponent_TakesWholeElementsOnly()
    {
        // K X(1) · T dynamic table of 3-character elements (max 5) · Z X(1): fixed run 2, T at fixed offset 1.
        var v = CobolVarGroup.FromContiguous("K" + "AAABBBC" + "Z", 2, [1], [3], [5L]);
        Assert.Equal("AAABBB", v.Dyn(0));
        Assert.Equal("KC", v.Fixed);   // the partial element is not taken; the fixed run reads on from there
    }
}
