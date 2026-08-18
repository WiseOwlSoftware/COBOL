// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The PB56 Dec-carrier intrinsic bodies (<c>CobolIntrinsics.Dec.cs</c>) — §15.4.1 r1's equivalent
/// arithmetic expressions evaluated ON the SDIDI, spec-derived expected values throughout.
/// </summary>
/// <remarks>
/// The defect these pin out: the interim landing truncated Dec operands at working scale 6, so a
/// sub-microscale operand reached every exact-family body as ZERO (kb/Work PB56; the found instance was
/// <c>FUNCTION SIGN(1e-9 − 0)</c> returning 0 against §15.81.4 r1a's +1). The agreement theory is the
/// drift half: on an all-fixed-point shared domain the Dec body and the exact Int128 body are
/// digit-identical (<c>COBOLNET_NUMERIC_DESIGN.md</c> D3's documented equivalence), so the two engines
/// cannot drift apart without a red here.
/// </remarks>
public sealed class CobolIntrinsicsDecTests
{
    private static CobolDec D(long unscaled, int scale) => CobolDec.From(unscaled, scale);

    private static void AssertDec(CobolDec expected, CobolDec actual) =>
        Assert.True(CobolDec.Compare(expected, actual) == 0,
            $"expected {expected.ToFunctionText()} got {actual.ToFunctionText()}");

    // ── The found instance and its siblings: sub-working-scale operands are no longer zero ──────────────

    [Fact]
    public void SignDec_SubMicroscaleOperand_IsPlusOne()
    {
        Assert.Equal(1, CobolIntrinsics.SignDec(D(1, 9)));      // 1e-9 — the PB56 headline shape
        Assert.Equal(-1, CobolIntrinsics.SignDec(D(-1, 34)));   // −1e-34, finer than any fixed ws
        Assert.Equal(0, CobolIntrinsics.SignDec(D(0, 0)));
    }

    [Fact]
    public void MaxMinDec_SubMicroscale_SelectsByValue_LeftmostTie()
    {
        AssertDec(D(1, 9), CobolIntrinsics.MaxDec(D(1, 9), D(0, 0)));       // 1e-9 > 0
        AssertDec(D(-1, 9), CobolIntrinsics.MinDec(D(0, 0), D(-1, 9)));     // −1e-9 < 0
        // §15.59.4 r2: equal values (different representations) — the LEFTMOST argument is returned.
        var leftmost = CobolIntrinsics.MaxDec(D(1000, 3), D(1, 0));         // both exactly 1
        Assert.Equal(-3, leftmost.Exp);                                     // identity, not just value
        Assert.Equal(2, CobolIntrinsics.OrdMaxDec(D(1, 0), D(2, 0), D(2, 0)));  // first greatest, §15.71.4 r2
        Assert.Equal(1, CobolIntrinsics.OrdMinDec(D(3, 9), D(4, 9)));
    }

    // ── Floor / truncation semantics (§15.44 / §15.49 / §15.42) ─────────────────────────────────────────

    [Fact]
    public void FloorTruncFraction_NegativeNonIntegral_DifferByOne()
    {
        AssertDec(D(-3, 0), CobolIntrinsics.FloorDec(D(-25, 1)));           // INTEGER(−2.5) = −3
        AssertDec(D(-2, 0), CobolIntrinsics.TruncDec(D(-25, 1)));           // INTEGER-PART(−2.5) = −2
        AssertDec(D(2, 0), CobolIntrinsics.FloorDec(D(2, 0)));              // integral fixed point
        AssertDec(D(75, 2), CobolIntrinsics.FractionPartDec(CobolRounding.NearestAwayFromZero, D(275, 2)));
    }

    // ── MOD / REM over the SDIDI (§15.64.4 r1 / §15.77.4 r1), including the sign table ─────────────────

    [Theory]
    [InlineData(11, 5, 1)]
    [InlineData(-11, 5, 4)]
    [InlineData(11, -5, -4)]
    [InlineData(-11, -5, -1)]
    public void ModDec_SignTable_AgreesWithExactCarrier(long a, long b, long expected)
    {
        AssertDec(D(expected, 0), CobolIntrinsics.ModDec(CobolRounding.NearestAwayFromZero, D(a, 0), D(b, 0)));
        Assert.Equal(CobolIntrinsics.ModScaled(a, b), (System.Int128)expected);
    }

    [Fact]
    public void ModDec_SubMicroscale_IsExact() =>
        AssertDec(D(1, 9), CobolIntrinsics.ModDec(CobolRounding.NearestAwayFromZero, D(7, 9), D(3, 9)));

    // ── The formerly staged inexact-EAE four (COBOLNET0899 retired with these bodies) ───────────────────

    [Fact]
    public void AnnuityDec_RateZero_IsReciprocalOfPeriods() =>
        AssertDec(D(25, 2), CobolIntrinsics.AnnuityDec(CobolRounding.NearestAwayFromZero, D(0, 0), 4));

    [Fact]
    public void PresentValueDec_Rate1_SumsDiscountedAmounts() =>
        // §15.74.4 r1: 8/(1+1)^1 + 8/(1+1)^2 = 4 + 2 = 6.
        AssertDec(D(6, 0), CobolIntrinsics.PresentValueDec(CobolRounding.NearestAwayFromZero, D(1, 0), D(8, 0), D(8, 0)));

    [Fact]
    public void VarianceDec_OneTwoThree_IsTwoThirds()
    {
        var v = CobolIntrinsics.VarianceDec(CobolRounding.NearestAwayFromZero, D(1, 0), D(2, 0), D(3, 0));
        Assert.Equal((System.Int128)66666, v.ToUnscaled(5, CobolRounding.Truncation));
    }

    [Fact]
    public void StdDevDec_ConstantList_IsZero() =>
        Assert.Equal(0, CobolIntrinsics.StdDevDec(CobolRounding.NearestAwayFromZero, D(2, 0), D(2, 0), D(2, 0)).Sig);

    // ── The drift half: Dec bodies agree with the exact Int128 bodies on the shared domain ─────────────
    // An all-fixed-point argument list stays on the exact family at the dispatch, so this agreement is what
    // keeps that routing choice HONEST — if the engines disagreed, the dispatch would be selecting answers.

    [Theory]
    [InlineData(new long[] { 31, -47, 500, 0 })]
    [InlineData(new long[] { 7, 7, 3 })]
    public void VariadicFamily_DecAgreesWithExact_OnFixedPointDomain(long[] xs)
    {
        var dec = System.Array.ConvertAll(xs, x => D(x, 2));                // all at scale 2
        var exact = System.Array.ConvertAll(xs, x => (System.Int128)x);
        const CobolRounding m = CobolRounding.NearestAwayFromZero;

        Assert.Equal(CobolIntrinsics.MaxScaled(exact), CobolIntrinsics.MaxDec(dec).ToUnscaled(2, CobolRounding.Truncation));
        Assert.Equal(CobolIntrinsics.MinScaled(exact), CobolIntrinsics.MinDec(dec).ToUnscaled(2, CobolRounding.Truncation));
        Assert.Equal(CobolIntrinsics.SumScaled(exact), CobolIntrinsics.SumDec(m, dec).ToUnscaled(2, CobolRounding.Truncation));
        Assert.Equal(CobolIntrinsics.RangeScaled(exact), CobolIntrinsics.RangeDec(m, dec).ToUnscaled(2, CobolRounding.Truncation));
        // Median/Midrange: the exact bodies return at scale s+1 (the ×10/2 discipline) — compare there.
        Assert.Equal(CobolIntrinsics.MedianScaled(exact), CobolIntrinsics.MedianDec(m, dec).ToUnscaled(3, CobolRounding.Truncation));
        Assert.Equal(CobolIntrinsics.MidrangeScaled(exact), CobolIntrinsics.MidrangeDec(m, dec).ToUnscaled(3, CobolRounding.Truncation));
    }

    // ── The NUMVAL family under STANDARD-DECIMAL (PB60, RV-15.67.4-1a): the one scan lifted EXACTLY ─────
    // §15.4.1 places the returned value in an SDIDI; §15.67.4 r1 / §15.68.4 r1 / §15.69.4 r3 fix it as "the
    // numeric value represented by argument-1" — no working scale, no receiver, no approximation.

    private const CobolRounding Nafz = CobolRounding.NearestAwayFromZero;

    [Fact]
    public void NumvalDec_IsTheParsedValueAtTheParsedScale()
    {
        var v = CobolIntrinsics.NumvalDec("1.2345678");
        Assert.Equal((System.Int128)12345678, v.Sig);                        // identity: the PARSED scale (7),
        Assert.Equal(-7, v.Exp);                                             //   never the native ≥6 floor
        AssertDec(D(-5, 1), CobolIntrinsics.NumvalDec("0.5CR"));             // §15.67.4 r2 — CR negates
        AssertDec(D(-5, 1), CobolIntrinsics.NumvalDec(" - 0.5 "));           // §15.67.3 r2 — spaces before the digit
        AssertDec(D(1500, 3), CobolIntrinsics.NumvalDec("1.500"));           // trailing zeros keep their scale
        // A 34-digit argument is legal under the standard-decimal cap (§15.67.3 r4) and exact (§8.8.1.5.2's
        // 34 digits) — the native projection saturated its Int128 rescale on this input.
        var n34 = CobolIntrinsics.NumvalDec("1234567890123456789012345678901234");
        Assert.Equal(System.Int128.Parse("1234567890123456789012345678901234"), n34.Sig);
        Assert.Equal(0, n34.Exp);
        AssertDec(D(125, 1), CobolIntrinsics.NumvalDec("12,5", commaMode: true));   // §15.67.3 r5
    }

    [Fact]
    public void NumvalCDec_ConsumesCurrencyAndGrouping_Exactly()
    {
        AssertDec(D(12345678901234, 10), CobolIntrinsics.NumvalCDec("$1,234.5678901234", "$"));   // §15.68.4 r2
        AssertDec(D(-12345, 2), CobolIntrinsics.NumvalCDec("R123.45CR", "R"));                    // the r4a position (PB60)
        AssertDec(D(-12345, 2), CobolIntrinsics.NumvalCDec("usd 123.45-", "USD", anycase: true)); // r4f
    }

    [Fact]
    public void NumvalFDec_LiftsTheExponent_ThroughTheOneRangeCheck()
    {
        AssertDec(D(15, 9), CobolIntrinsics.NumvalFDec(Nafz, "1.5E-8"));                  // 15 × 10^(−8−1)
        AssertDec(D(123456789012345, 17), CobolIntrinsics.NumvalFDec(Nafz, "1.23456789012345E-3"));
        var big = CobolIntrinsics.NumvalFDec(Nafz, "1E+40");                               // in range, exact
        Assert.Equal((System.Int128)1, big.Sig);
        Assert.Equal(40, big.Exp);
        AssertDec(D(-350, 1), CobolIntrinsics.NumvalFDec(Nafz, " - 35E+0 "));              // §15.69.3 r5's legal spaces
        // A 4-digit E-exponent can leave decimal128 (§8.8.1.5.2 r2) — the ONE range check every SDIDI result gets.
        var ex = Assert.Throws<CobolSizeError>(() => CobolIntrinsics.NumvalFDec(Nafz, "1E+9999"));
        Assert.Equal("EC-SIZE-OVERFLOW", ex.EcName);
    }

    /// <summary>The drift half for this family: on the shared fixed-point domain the SDIDI projection, landed
    /// at the native projection's working scale, is digit-identical to the native projection — the two
    /// arithmetic modes read ONE scan and cannot disagree about a conforming argument's value.</summary>
    [Theory]
    [InlineData("12.5")]
    [InlineData("-0.001")]
    [InlineData("1234567.891")]
    [InlineData("12.5CR")]
    [InlineData(" + 7 ")]
    public void NumvalFamily_DecAgreesWithNative_OnTheSharedDomain(string text)
    {
        Assert.Equal(CobolIntrinsics.Numval(text, 6), CobolIntrinsics.NumvalDec(text).ToUnscaled(6, CobolRounding.Truncation));
        Assert.Equal(CobolIntrinsics.NumvalC(text, "$", 6), CobolIntrinsics.NumvalCDec(text, "$").ToUnscaled(6, CobolRounding.Truncation));
        if (!text.Contains("CR"))   // NUMVAL-F has no CR form (§15.69.3 r1)
            Assert.Equal(CobolIntrinsics.NumvalF(text, 9), CobolIntrinsics.NumvalFDec(Nafz, text).ToUnscaled(9, CobolRounding.Truncation));
    }

    /// <summary>The reject projections are SHARED: a non-conforming argument yields the §15.3 default (0)
    /// from both modes' value functions, through the one message per family (checking off here).</summary>
    [Theory]
    [InlineData("-12-")]                                     // neither §15.67.3 r1 format
    [InlineData("12345678901234567890123456789012345")]      // 35 digits — past the standard-decimal cap too
    [InlineData("")]                                         // r1c — no digit anywhere
    public void NumvalFamily_RejectProjection_IsSharedAcrossModes(string text)
    {
        Assert.Equal((System.Int128)0, CobolIntrinsics.Numval(text, 6, digitCap: 34));
        AssertDec(D(0, 0), CobolIntrinsics.NumvalDec(text));
        Assert.Equal((System.Int128)0, CobolIntrinsics.NumvalF(text, 9, digitCap: 34));
        AssertDec(D(0, 0), CobolIntrinsics.NumvalFDec(Nafz, text));
    }
}
