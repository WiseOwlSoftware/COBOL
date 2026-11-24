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
    /// <summary>Every argument at one scale: the selection and the ordinal the renderer's arms emit (<c>MaxAt</c>, <c>OrdMaxAt</c>), and
    /// the aligned <c>MAX - MIN</c> that RANGE is (§2023:15.76.4 RV1), the one entry point of the aligned extremes (kb/Work PB2627).</summary>
    [Fact]
    public void TheCommonScaleList_TheLeftmostExtreme_AndItsOrdinal()
    {
        Int128[] xs = [3, 7, 1, 7, 1];
        int[] s = [0, 0, 0, 0, 0];
        Assert.Equal((Int128)7, CobolIntrinsics.MaxAt(0, store: false, xs, s));
        Assert.Equal(2, CobolIntrinsics.OrdMaxAt(xs, s));              // the leftmost of the two 7s
        Assert.Equal((Int128)1, CobolIntrinsics.MinAt(0, store: false, xs, s));
        Assert.Equal(3, CobolIntrinsics.OrdMinAt(xs, s));
        Assert.Equal((Int128)6, CobolIntrinsics.RangeScaled(xs));
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

    [Fact]
    public void TheExactValueLane_SelectsByAlgebraicValue_TheLeftmostOnATie()
    {
        // 1.5 (15 at scale 1), 2 (an SDIDI 20E-1), 1.50 (150 at scale 2), 2 (an integer), -0 and 0: kb/Work PB6055
        // routed this lane (native MAX / MIN / ORD-MAX / ORD-MIN over exact values) through the one kernel.
        CobolExact[] xs = [CobolExact.From(15, 1), CobolExact.FromDec(new CobolDec(20, -1)), CobolExact.From(150, 2), CobolExact.From(2, 0)];
        Assert.Equal(2, CobolIntrinsics.OrdMaxExact(xs));                // 2 and 2: the leftmost
        Assert.Equal(1, CobolIntrinsics.OrdMinExact(xs));                // 1.5 and 1.50: the leftmost
        Assert.Equal(0, CobolExact.Compare(xs[1], CobolIntrinsics.MaxExact(xs)));
        Assert.Equal(0, CobolExact.Compare(xs[0], CobolIntrinsics.MinExact(xs)));
        Assert.Equal(1, CobolIntrinsics.OrdMinExact(CobolExact.From(-0, 0), CobolExact.From(0, 3)));
    }

    [Fact]
    public void TheWideLane_TheLeftmostExtreme_AndItsOrdinal()
    {
        CobolWide[] xs = [CobolWide.From(3), CobolWide.From(7), CobolWide.From(1), CobolWide.From(7), CobolWide.From(1)];
        Assert.Equal(2, CobolIntrinsics.OrdMaxWide(xs));
        Assert.Equal(3, CobolIntrinsics.OrdMinWide(xs));
        Assert.Equal(0, CobolWide.Compare(CobolWide.From(7), 0, CobolIntrinsics.MaxWide(xs), 0));
        Assert.Equal(0, CobolWide.Compare(CobolWide.From(1), 0, CobolIntrinsics.MinWide(xs), 0));
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
