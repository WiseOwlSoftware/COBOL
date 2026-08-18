// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

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
