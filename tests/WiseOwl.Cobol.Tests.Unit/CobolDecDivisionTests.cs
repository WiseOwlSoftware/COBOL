// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// SDIDI division (<c>CobolDec.Div</c>, ISO §8.8.1.5.3) when the numerator has FAR FEWER digits than the
/// denominator (kb/Work PB83 — found landing PB69): the pre-scale that gives the quotient its 34–36 significant
/// digits can exceed 10³⁸ (up to 10⁷³ for a 1-digit numerator over a 38-digit denominator), and it was CAPPED at
/// 10³⁸ while the result exponent still subtracted the uncapped amount — so `100000 / 123456789012345678901234567890`
/// answered 0 under STANDARD-DECIMAL (8.1E-25 owed), wrong by 10^(scaleUp − 38). The scale is now applied in two exact
/// steps (inside the numerator's Int128 headroom, then through the 256-bit product). Expected values are the exact
/// decimal quotients rounded to 34 significant digits (§8.8.1.5.2 NOTE 2), derived independently.
/// </summary>
public sealed class CobolDecDivisionTests
{
    private static CobolDec Dec(string sig, int exp) => new(Int128.Parse(sig), exp);

    [Fact]
    public void ShortNumerator_LongDenominator_KeepsTheFullPreScale()
    {
        // 100000 / 123456789012345678901234567890 = 8.10000007290000066339000603685715493…E-25 → 34 significant digits, nearest
        var q = CobolDec.Div(Dec("100000", 0), Dec("123456789012345678901234567890", 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal("8100000072900000663390006036857155E-58", $"{q.Sig}E{q.Exp}");
    }

    [Fact]
    public void OneDigitNumerator_ThirtyEightDigitDenominator_TheWidestPreScale()
    {
        // 5 / 99999999999999999999999999999999999999 = 5.0000000000000000000000000000000000000025E-38 → 34 digits:
        // 5000000000000000000000000000000000E-71 (the next digit is 0, no round-up)
        var q = CobolDec.Div(Dec("5", 0), Dec("99999999999999999999999999999999999999", 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal("5000000000000000000000000000000000E-71", $"{q.Sig}E{q.Exp}");
    }

    [Fact]
    public void ApproximatePowerOverExactPower_IsTheirRatio()
    {
        // The PB69 shape: A³ past the Int128 window (the double approximation 9.99999999999997E44) over the exact
        // 30-digit A² — the quotient is ≈ 999999999999998.99, not the 999.99 the capped pre-scale produced.
        var a3 = CobolIntrinsics.PowNativeIntDec(999999999999999, 3);
        var a2 = CobolIntrinsics.PowNativeIntDec(999999999999999, 2);
        var q = CobolDec.Div(a3, a2, CobolRounding.NearestAwayFromZero);
        double approx = q.ToDouble();
        Assert.InRange(approx, 999999999999998.0, 999999999999999.5);
    }

    [Fact]
    public void ExactIntegerRemainder_StaysExact_OnTheSdidi()
    {
        // MOD/REM over two exact integers that fit Int128 take the exact remainder (kb/Work PB69) — a 30-digit
        // native power reaches ModDec as an exact SDIDI, and the SDIDI EAE would round the 30-digit product.
        var a2 = CobolIntrinsics.PowNativeIntDec(999999999999999, 2);   // 999999999999998000000000000001
        var m = CobolIntrinsics.ModDec(CobolRounding.NearestAwayFromZero, a2, CobolDec.From(1000000007, 0));
        Assert.Equal("13657001E0", $"{m.Sig}E{m.Exp}");
        var r = CobolIntrinsics.RemDec(CobolRounding.NearestAwayFromZero, new CobolDec(-11, 0), CobolDec.From(5, 0));
        Assert.Equal("-1E0", $"{r.Sig}E{r.Exp}");                        // §15.77.4 — the sign follows the dividend
    }

    // ── kb/Work PB3954: an EXACT quotient / root is held at ISO/IEC 60559:2020 5.4.1's preferred exponent ───────────────
    // ISO §8.8.1.5.3 (division) and §15.84.4 r2 (SQRT) take their definitions from 60559, whose exact result is the
    // representation NEAREST the preferred exponent (the dividend's less the divisor's; half the operand's rounded down);
    // only an INEXACT result fills the 34 digits. Expected values are derived by hand from the exact decimal results.

    [Theory]
    [InlineData("6", 0, "3", 0, "2E0")]                       // 6 / 3 = 2: preferred exponent 0 - 0
    [InlineData("10", 0, "4", 0, "25E-1")]                    // 2.5 is exact and needs the digit below the preferred exponent
    [InlineData("1", 0, "4", 0, "25E-2")]
    [InlineData("150", -2, "5", -1, "30E-1")]                 // 1.50 / 0.5 = 3.0: preferred exponent -2 - -1 = -1, so 30E-1
    [InlineData("100", 0, "4", 0, "25E0")]
    [InlineData("1200", 0, "3", 0, "400E0")]                  // trailing zeros of the INTEGER quotient stay: its exponent is already the preferred one
    [InlineData("5", 2, "5", 0, "1E2")]                       // 5E2 / 5 = 1E2: preferred exponent 2 - 0
    [InlineData("1", 0, "3", 0, "3333333333333333333333333333333333E-34")]   // inexact: all 34 digits
    [InlineData("2", 0, "3", 0, "6666666666666666666666666666666667E-34")]
    public void AnExactQuotient_IsHeldAtThePreferredExponent_AnInexactOneFillsTheDigits(
        string dividend, int dividendExp, string divisor, int divisorExp, string expected)
    {
        var q = CobolDec.Div(Dec(dividend, dividendExp), Dec(divisor, divisorExp), CobolRounding.NearestAwayFromZero);
        Assert.Equal(expected, $"{q.Sig}E{q.Exp}");
        // Round-to-odd (the native lane's SDIDI quotient) reduces the same exact value the same way; the inexact ones
        // differ only in the last digit (odd), so they are compared by value below.
        var odd = CobolDec.DivToOdd(Dec(dividend, dividendExp), Dec(divisor, divisorExp));
        if (!expected.EndsWith("E-34")) Assert.Equal(expected, $"{odd.Sig}E{odd.Exp}");
    }

    [Fact]
    public void AnExactQuotientPastThirtyFourDigits_KeepsTheExponentTheFormatNeeds()
    {
        // 10^37 / 1 is exact but has 38 digits: decimal128 holds it at exponent 4, the nearest the format reaches 0.
        var q = CobolDec.Div(Dec("10000000000000000000000000000000000000", 0), Dec("1", 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal("1000000000000000000000000000000000E4", $"{q.Sig}E{q.Exp}");
    }

    [Fact]
    public void ANegativeExactQuotient_IsHeldAtThePreferredExponent()
    {
        var q = CobolDec.Div(Dec("-6", 0), Dec("3", 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal("-2E0", $"{q.Sig}E{q.Exp}");
    }

    [Theory]
    [InlineData("4", 0, "2E0")]
    [InlineData("25", -2, "5E-1")]                            // sqrt(0.25) = 0.5: preferred exponent floor(-2 / 2) = -1
    [InlineData("100", 0, "10E0")]
    [InlineData("1", 4, "1E2")]                               // sqrt(1E4) = 1E2: preferred exponent 2
    public void AnExactRoot_IsHeldAtThePreferredExponent(string sig, int exp, string expected)
    {
        var r = CobolDec.Sqrt(Dec(sig, exp), CobolRounding.NearestAwayFromZero);
        Assert.Equal(expected, $"{r.Sig}E{r.Exp}");
    }

    [Fact]
    public void AnInexactRoot_FillsTheThirtyFourDigits()
    {
        // sqrt(2) = 1.41421356237309504880168872420969807856967…: 34 digits, the next digits 07 round down
        var r = CobolDec.Sqrt(Dec("2", 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal("1414213562373095048801688724209698E-33", $"{r.Sig}E{r.Exp}");
    }

    [Theory]
    [InlineData(new[] { 1L, 2L, 3L }, "2E0")]               // FUNCTION MEAN (1 2 3): the repro, 2 and not 2.000…0 (34 digits)
    [InlineData(new[] { 1L, 2L }, "15E-1")]
    [InlineData(new[] { 1L, 1L }, "1E0")]
    [InlineData(new[] { 1L, 2L, 4L }, "2333333333333333333333333333333333E-33")]
    public void TheMean_IsHeldAtThePreferredExponentWhenExact(long[] values, string expected)
    {
        var m = CobolIntrinsics.MeanDec(CobolRounding.NearestAwayFromZero, values.Select(v => CobolDec.From(v, 0)).ToArray());
        Assert.Equal(expected, $"{m.Sig}E{m.Exp}");
    }

    [Fact]
    public void ThePastWindowPower_IsAnApproximation_NeverASentinel()
    {
        // A³ for a 15-digit A is 45 digits: the SDIDI carries the owner-decided double approximation, and two
        // different powers compare as their magnitudes — the saturated Int128.MaxValue sentinel that made
        // `A ** 4 > A ** 3` FALSE and `A ** 3 = A ** 4` TRUE is gone (kb/Work PB69).
        var a3 = CobolIntrinsics.PowNativeIntDec(999999999999999, 3);
        var a4 = CobolIntrinsics.PowNativeIntDec(999999999999999, 4);
        Assert.True(CobolDec.Compare(a4, a3) > 0);
        Assert.NotEqual(0, CobolDec.Compare(a3, a4));
        Assert.InRange(a3.ToDouble(), 9.9e44, 1.0e45);
    }
}
