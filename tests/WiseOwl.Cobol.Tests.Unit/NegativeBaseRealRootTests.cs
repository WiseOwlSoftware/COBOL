// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB5186 - the COBOL-85 negative-base rule, <c>CobolIntrinsics.PowNativeReal(double, CobolExact)</c>. §1985:VI.6.2.3 GR5c
/// raises the size error condition only "If no real number exists as the result of the evaluation": an exponent p/q in lowest terms
/// with q odd has the real result <c>±|b|^(p/q)</c>, negative when p is odd, and an even q has none. Expected values are the real roots
/// worked by hand (the fifth root of -32 is -2); every arm of the exponent's exact form is reached: a scale above, at and below zero,
/// a zero exponent, a negative one, an SDIDI value, and a float item's content, whose exact binary value has an even denominator.
/// </summary>
public sealed class NegativeBaseRealRootTests
{
    private static string Ec(Action body) => Assert.Throws<CobolSizeError>(body).EcName;

    [Theory]
    [InlineData(2, 1, -2.0)]      // 0.2 = 1/5: the fifth root of -32
    [InlineData(4, 1, 4.0)]       // 0.4 = 2/5: its square
    [InlineData(-2, 1, -0.5)]     // -0.2 = -1/5: the reciprocal of -2
    [InlineData(6, 1, -8.0)]      // 0.6 = 3/5
    [InlineData(10, 1, -32.0)]    // 1.0 = 1/1, an integer written with a fraction digit
    [InlineData(20, 1, 1024.0)]   // 2.0 = 2/1
    [InlineData(200, 3, -2.0)]    // 0.200 = 1/5 at scale 3: the reduction cancels 2^3 and 5^2
    [InlineData(0, 1, 1.0)]       // 0.0: a zero exponent over a nonzero base is 1
    public void A_NegativeBase_ToAnOddDenominator_IsItsRealRoot(long unscaled, int scale, double expected)
    {
        Assert.Equal(expected, CobolIntrinsics.PowNativeReal(-32, CobolExact.From(unscaled, scale)), 12);
    }

    [Theory]
    [InlineData(5, 1)]            // 0.5 = 1/2: no real square root of -4
    [InlineData(25, 2)]           // 0.25 = 1/4
    [InlineData(1, 1)]            // 0.1 = 1/10
    [InlineData(-15, 1)]          // -1.5 = -3/2
    public void A_NegativeBase_ToAnEvenDenominator_IsTheSizeError(long unscaled, int scale) =>
        Assert.Equal("EC-SIZE-EXPONENTIATION", Ec(() => CobolIntrinsics.PowNativeReal(-4, CobolExact.From(unscaled, scale))));

    [Fact]
    public void A_FloatItemsContent_HasAnEvenDenominator_AndAnSdidiValue_IsReadExactly()
    {
        // 0.2 as a binary64 is 3602879701896397 / 2^54: no real root of a negative base.
        Assert.Equal("EC-SIZE-EXPONENTIATION", Ec(() => CobolIntrinsics.PowNativeReal(-32, CobolExact.FromBinary64(0.2))));
        // An SDIDI 2E-1 is 0.2 = 1/5.
        Assert.Equal(-2.0, CobolIntrinsics.PowNativeReal(-32, CobolExact.FromDec(new CobolDec(2, -1))), 12);
        // An SDIDI 1E1 is 10, held at scale -1: an even integer, so (-32) ** 10 is 2^50.
        Assert.Equal(1125899906842624.0, CobolIntrinsics.PowNativeReal(-32, CobolExact.FromDec(new CobolDec(1, 1))));
        // An SDIDI 3E1 is 30: even, positive.
        Assert.True(CobolIntrinsics.PowNativeReal(-2, CobolExact.FromDec(new CobolDec(3, 1))) > 0);
    }

    [Fact]
    public void A_ZeroOrPositiveBase_IsTheEditionsCommonRule()
    {
        Assert.Equal(2.0, CobolIntrinsics.PowNativeReal(4, CobolExact.From(5, 1)), 12);
        Assert.Equal(0.0, CobolIntrinsics.PowNativeReal(0, CobolExact.From(5, 1)));
        Assert.Equal("EC-SIZE-EXPONENTIATION", Ec(() => CobolIntrinsics.PowNativeReal(0, CobolExact.From(0, 1))));
        Assert.Equal("EC-SIZE-EXPONENTIATION", Ec(() => CobolIntrinsics.PowNativeReal(0, CobolExact.From(-5, 1))));
    }

    [Fact]
    public void TheCheckedEntry_RangeChecksTheRealRoot()
    {
        Assert.Equal(-2.0, CobolIntrinsics.PowNativeRealChecked(-32, CobolExact.From(2, 1), SizeChecks.Range), 12);
        Assert.Equal("EC-SIZE-OVERFLOW", Ec(() => CobolIntrinsics.PowNativeRealChecked(-1e300, CobolExact.From(30, 1), SizeChecks.Range)));
    }

    [Fact]
    public void TheExactOperandArm_TakesTheSameRule()
    {
        Assert.Equal(-2.0, CobolExact.PowReal(CobolExact.From(-32, 0), CobolExact.From(2, 1), negativeBaseRealRoots: true).ToDouble(), 12);
        Assert.Equal("EC-SIZE-EXPONENTIATION", Ec(() => CobolExact.PowReal(CobolExact.From(-32, 0), CobolExact.From(2, 1))));
    }
}
