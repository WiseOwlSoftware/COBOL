// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ MAX / MIN AND ORD-MAX / ORD-MIN NAME THE SAME ARGUMENT ON EVERY CARRIER (kb/Work PB2635): §2023:15.59.4 RV1/RV2 select
/// "the content of the argument-1 having the greatest value", the leftmost on a tie, and §2023:15.71.4 is the ordinal of
/// that argument. One kernel (<c>CobolIntrinsics.ExtremeIndex</c>) owns the rule; these rows pin each carrier's entry
/// points to it, ties included, and the binary64 lane with a NaN argument, where <c>Enumerable.Max</c> / <c>Min</c> (which
/// MAX / MIN used to call) and the ordinal loop named different arguments.
/// </summary>
public sealed class SelectionRuleCarrierAgreementTests
{
    [Fact]
    public void TheAlignedLane_TheLeftmostExtreme_AndItsOrdinal()
    {
        Int128[] xs = [3, 7, 1, 7, 1];
        Assert.Equal((Int128)7, CobolIntrinsics.MaxScaled(xs));
        Assert.Equal(2, CobolIntrinsics.OrdMax(xs));                   // the leftmost of the two 7s
        Assert.Equal((Int128)1, CobolIntrinsics.MinScaled(xs));
        Assert.Equal(3, CobolIntrinsics.OrdMin(xs));
    }

    [Fact]
    public void TheUnalignedPairLane_SelectsByValue_NotByUnscaledDigits()
    {
        // 2.5 (25 at scale 1), 3 (3 at scale 0), 3.00 (300 at scale 2): the greatest VALUE is 3, first at ordinal 2.
        Int128[] v = [25, 3, 300];
        int[] s = [1, 0, 2];
        Assert.Equal(2, CobolIntrinsics.OrdMaxAt(v, s));
        Assert.Equal((Int128)300, CobolIntrinsics.MaxAt(2, store: false, v, s));
        Assert.Equal(1, CobolIntrinsics.OrdMinAt(v, s));
        Assert.Equal((Int128)250, CobolIntrinsics.MinAt(2, store: false, v, s));
    }

    [Fact]
    public void TheStandardDecimalLane_TheLeftmostExtreme_AndItsOrdinal()
    {
        CobolDec[] xs = [new(5, 0), new(50, -1), new(4, 0)];        // 5, 5.0, 4: equal values, the leftmost wins
        Assert.Equal(new CobolDec(5, 0), CobolIntrinsics.MaxDec(xs));
        Assert.Equal(1, CobolIntrinsics.OrdMaxDec(xs));
        Assert.Equal(3, CobolIntrinsics.OrdMinDec(xs));
    }

    [Fact]
    public void TheCharacterLane_TheLeftmostExtreme_AndItsOrdinal()
    {
        string[] xs = ["B", "C ", "C", "A"];                         // "C " and "C" compare equal under space padding
        Assert.Equal("C ", CobolIntrinsics.MaxString(xs));
        Assert.Equal(2, CobolIntrinsics.OrdMaxString(xs));
        Assert.Equal("A", CobolIntrinsics.MinString(xs));
        Assert.Equal(4, CobolIntrinsics.OrdMinString(xs));
    }

    [Theory]
    [InlineData(new[] { double.NaN, 1.0 })]
    [InlineData(new[] { 1.0, double.NaN })]
    [InlineData(new[] { 2.0, double.NaN, 3.0, 3.0 })]
    public void TheBinary64Lane_TheValueIsTheArgumentAtTheOrdinal_EvenWithANaN(double[] xs)
    {
        Assert.Equal(xs[(int)CobolIntrinsics.OrdMaxReal(xs) - 1], CobolIntrinsics.MaxReal(xs));
        Assert.Equal(xs[(int)CobolIntrinsics.OrdMinReal(xs) - 1], CobolIntrinsics.MinReal(xs));
    }
}
