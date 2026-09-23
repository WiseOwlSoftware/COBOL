// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <c>CobolTable.AllArgs</c>'s <c>lead</c> lambda renders the FIRST element the walk enumerates — argument-1 when
/// the table(ALL) leads the list (ISO §15.3 rule 14: "The order of the implicit specification of each occurrence
/// is from left to right") — so PRESENT-VALUE's rate is screened and turned into its discount base on the element's
/// exact carrier, not on the enumerated binary64 (kb/Work PB1000). Which element is first is the walk's to say: an
/// inner ALL level may range over NOTHING for the first outer occurrence.
/// </summary>
public sealed class TableAllLeadTests
{
    [Fact]
    public void TheLeadRendersTheFirstEnumeratedElementOnly()
    {
        var got = CobolTable.AllArgs(
            [_ => 3L], idx => $"e{idx[0]}", idx => $"LEAD{idx[0]}");
        Assert.Equal(["LEAD1", "e2", "e3"], got);
    }

    [Fact]
    public void TheFirstElementIsTheWalksFirst_EvenWhenAnInnerLevelIsEmptyAtTheFirstOuterOccurrence()
    {
        // Outer 1..2; inner count 0 at outer 1, 2 at outer 2 — argument-1 is (2,1), not (1,1).
        var got = CobolTable.AllArgs(
            [_ => 2L, idx => idx[0] == 1 ? 0L : 2L], idx => $"e{idx[0]}{idx[1]}", idx => $"LEAD{idx[0]}{idx[1]}");
        Assert.Equal(["LEAD21", "e22"], got);
    }

    [Fact]
    public void WithoutALead_EveryElementTakesTheElementLambda() =>
        Assert.Equal(["e1", "e2"], CobolTable.AllArgs([_ => 2L], idx => $"e{idx[0]}"));
}
