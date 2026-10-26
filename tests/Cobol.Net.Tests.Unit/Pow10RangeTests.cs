// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB2633 and PB648 - <c>Pow10.AsWide</c> / <c>FiveAsWide</c> answer ONLY inside their tables. The fallback loop
/// they used to carry wrapped the Int128 silently past 10^38 and returned 1 for a negative exponent, which is how the
/// codomain clamp (since retired, kb/Work PB3829) at a trailing-P receiver's scale of -2 formed 10^39 as a negative
/// number and clamped every ACOS result to -1 (= -100 in PIC S9PP).
/// </summary>
/// <remarks>Every expected value is computed by BigInteger arithmetic or from the cited rounding rule, never
/// observed.</remarks>
public sealed class Pow10RangeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(18)]
    [InlineData(37)]
    [InlineData(38)]
    public void AsWide_InTable_IsTheExactPower(int n) =>
        Assert.Equal((Int128)BigInteger.Pow(10, n), Pow10.AsWide(n));

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(39)]
    [InlineData(40)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void AsWide_OutsideTheTable_FailsLoudly_NeverWrapsOrAnswersOne(int n) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Pow10.AsWide(n));

    [Theory]
    [InlineData(0)]
    [InlineData(27)]
    [InlineData(54)]
    public void FiveAsWide_InTable_IsTheExactPower(int n) =>
        Assert.Equal((Int128)BigInteger.Pow(5, n), Pow10.FiveAsWide(n));

    [Theory]
    [InlineData(-1)]
    [InlineData(55)]
    [InlineData(56)]
    public void FiveAsWide_OutsideTheTable_FailsLoudly(int n) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Pow10.FiveAsWide(n));

    /// <summary>The caller the hardening exposed: an Int128 significand reaches 39 digits, where the numerator pre-scale's
    /// headroom (38 - digits) is -1 and <c>AsWide(-1)</c> used to answer 1 while the quotient's exponent still counted
    /// the step, so 10^38 x 10^-35 (= 1000) over 10^20 came out ten times too large. Exact: 1000 / 10^20 = 10^-17.</summary>
    [Fact]
    public void CobolDecDiv_OfA39DigitNumerator_ScalesByTheIntendedPower()
    {
        var a = new CobolDec(Int128.Parse("100000000000000000000000000000000000000"), -35);
        var b = new CobolDec(Int128.Parse("100000000000000000000"), 0);
        Assert.Equal(0, CobolDec.Compare(CobolDec.Div(a, b, CobolRounding.NearestEven), new CobolDec(1, -17)));
    }

    /// <summary>The PB2633 receiver shape, landed by the one float quantizer (kb/Work PB3829 retired the codomain clamp
    /// that first exposed the wrapped power): a trailing-P receiver has a NEGATIVE scale, and the float lands ONCE at it
    /// with the statement's mode (§14.7.4.3). One unit at scale s is 10^-s, so ACOS(-1) = 3.14... is 0.314... units at
    /// scale -1 and far less below: truncation and the nearest modes give 0 units, AWAY-FROM-ZERO (r3) gives 1 unit,
    /// TOWARD-LESSER (r9) of ASIN(-1) = -1.57... gives -1 unit and TOWARD-GREATER (r8) gives 0. ACOS(0.5) truncated is
    /// 0 units at every such scale, never the -1 the wrapped 10^39 produced.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-11)]
    [InlineData(-31)]
    public void FromDouble_AtATrailingPScale_RoundsOnceAtThatScale(int scale)
    {
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDouble(Math.Acos(0.5), scale, CobolRounding.Truncation));
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDouble(Math.Acos(-1.0), scale, CobolRounding.NearestAwayFromZero));
        Assert.Equal(Int128.One, CobolIntrinsics.FromDouble(Math.Acos(-1.0), scale, CobolRounding.AwayFromZero));
        Assert.Equal(Int128.NegativeOne, CobolIntrinsics.FromDouble(Math.Asin(-1.0), scale, CobolRounding.TowardLesser));
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDouble(Math.Asin(-1.0), scale, CobolRounding.TowardGreater));
    }
}
