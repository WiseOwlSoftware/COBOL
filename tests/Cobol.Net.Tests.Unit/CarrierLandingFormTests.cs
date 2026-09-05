// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB77 — every carrier's landing into a fixed-point receiver has TWO forms, chosen by the LANDING and never
/// by the value: the CHECKED landing (ON SIZE ERROR / EC-SIZE checking) SATURATES past the Int128 carrier so the
/// receiver's capacity check raises the size error (PB13); the UNCHECKED landing (a MOVE — ISO §14.6.8.2 r4
/// "truncation on either end"; the no-phrase arithmetic store — §14.6.13.1.3 item 8; INVOKE BY CONTENT) keeps the
/// LOW-ORDER digits, never a sentinel. The SDIDI carrier had both since PB74 (<c>CobolDec.ToUnscaledChecked</c> /
/// <c>ToUnscaled</c>); the native exact family (<c>CobolIntrinsics.Rescaled</c>) and the float family
/// (<c>CobolFloat.ToScaled</c>, <c>CobolIntrinsics.FromDouble</c>) had only the saturating one, so
/// <c>MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)</c> stored 884105727 — the low digits of <c>Int128.MaxValue</c>.
/// </summary>
/// <remarks>Every expected value is derived by EXACT arithmetic on the sending value (Python <c>Decimal</c> over
/// the binary64's exact expansion), never observed: 1.0E+40 as a double is 10000000000000000303786028427003666890752;
/// -2.5E+40 is -25000000000000000155002161260194579873792.</remarks>
public sealed class CarrierLandingFormTests
{
    private static readonly Int128 Ten38 = Int128.Parse("100000000000000000000000000000000000000");

    // ── the exact-expansion kernel ────────────────────────────────────────────────────────────────────────────

    /// <summary>The 38 low-order digits of the exact binary64 expansion, sign kept, at scale 0.</summary>
    [Fact]
    public void LowOrderDigits_PastTheCarrier_AreTheExactExpansions()
    {
        Assert.Equal(Int128.Parse("303786028427003666890752"), CobolFloat.LowOrderDigits(1.0e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("-155002161260194579873792"), CobolFloat.LowOrderDigits(-2.5e40, 0, CobolRounding.Truncation));
    }

    /// <summary>A landing scale multiplies the exact expansion (the fraction digits of an integer are zeros); an
    /// exact half at a 31-digit scale — 20000000.5 × 10^31 = 2×10^38 + 5×10^30 — keeps its low 38 digits.</summary>
    [Fact]
    public void LowOrderDigits_AtScale_MultiplyTheExactExpansion()
    {
        Assert.Equal(Int128.Parse("303786028427003666890752") * 1_000_000_000, CobolFloat.LowOrderDigits(1.0e40, 9, CobolRounding.Truncation) % Ten38);
        Assert.Equal(Int128.Parse("5000000000000000000000000000000"), CobolFloat.LowOrderDigits(20000000.5, 31, CobolRounding.Truncation));
    }

    /// <summary>The kernel is correct for EVERY finite double (so a future exact-everywhere determination is one
    /// branch away): a fraction rounds per the COBOL mode through the ONE <c>RoundDiv</c> kernel, with the sign's
    /// directed modes honoured.</summary>
    [Theory]
    [InlineData(2.5, CobolRounding.Truncation, 2)]
    [InlineData(2.5, CobolRounding.NearestAwayFromZero, 3)]
    [InlineData(2.5, CobolRounding.NearestEven, 2)]
    [InlineData(3.5, CobolRounding.NearestEven, 4)]
    [InlineData(2.5, CobolRounding.NearestTowardZero, 2)]
    [InlineData(2.75, CobolRounding.NearestTowardZero, 3)]
    [InlineData(-2.5, CobolRounding.TowardLesser, -3)]
    [InlineData(-2.5, CobolRounding.TowardGreater, -2)]
    [InlineData(-2.5, CobolRounding.AwayFromZero, -3)]
    [InlineData(0.1, CobolRounding.Truncation, 0)]
    [InlineData(1.0e25, CobolRounding.Truncation, 10000000000000000905969664.0)]
    public void LowOrderDigits_InsideTheCarrier_RoundsExactlyPerMode(double v, CobolRounding mode, double expected) =>
        Assert.Equal((Int128)expected, CobolFloat.LowOrderDigits(v, 0, mode));

    // ── the float family's two landings ───────────────────────────────────────────────────────────────────────

    /// <summary>Inside the carrier the two landings are ONE function (the same product, the same rounding).</summary>
    [Theory]
    [InlineData(1.15, 2)]
    [InlineData(-8.2, 1)]
    [InlineData(1.0e25, 0)]
    [InlineData(123456.789, 9)]
    public void ToScaledUnchecked_InsideTheCarrier_IsToScaled(double v, int scale)
    {
        foreach (CobolRounding mode in Enum.GetValues<CobolRounding>())
            Assert.Equal(CobolFloat.ToScaled(v, scale, mode), CobolFloat.ToScaledUnchecked(v, scale, mode));
    }

    /// <summary>Past the carrier: the CHECKED landing saturates (the store's capacity check raises); the UNCHECKED one
    /// lands the exact low-order digits — and a non-finite value lands zero, never a sentinel to truncate.</summary>
    [Fact]
    public void ToScaled_PastTheCarrier_SaturatesChecked_LandsLowOrderDigitsUnchecked()
    {
        Assert.Equal(Int128.MaxValue, CobolFloat.ToScaled(1.0e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.MinValue, CobolFloat.ToScaled(-2.5e40, 5, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("303786028427003666890752"), CobolFloat.ToScaledUnchecked(1.0e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("-155002161260194579873792"), CobolFloat.ToScaledUnchecked(-2.5e40, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.MaxValue, CobolFloat.ToScaled(double.PositiveInfinity, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Zero, CobolFloat.ToScaledUnchecked(double.PositiveInfinity, 0, CobolRounding.Truncation));
        Assert.Equal(Int128.Zero, CobolFloat.ToScaledUnchecked(double.NaN, 3, CobolRounding.Truncation));
    }

    /// <summary>The working-scale quantizer (<c>FromDouble</c>, the arithmetic-receiver landing) follows the same
    /// two-form rule: checked saturates, unchecked lands the low-order digits at the working scale.</summary>
    [Fact]
    public void FromDouble_PastTheCarrier_FollowsTheLanding()
    {
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.FromDouble(1.0e40, 9, checkedLanding: true));
        Assert.Equal(Int128.Parse("303786028427003666890752") * 1_000_000_000 % Ten38, CobolIntrinsics.FromDouble(1.0e40, 9) % Ten38);
        Assert.Equal(Int128.Zero, CobolIntrinsics.FromDouble(double.PositiveInfinity, 9));
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.FromDouble(double.PositiveInfinity, 9, checkedLanding: true));
        Assert.Equal(CobolIntrinsics.FromDouble(2.5, 0), CobolIntrinsics.FromDouble(2.5, 0, checkedLanding: true));   // inside: one function
    }

    // ── the native exact family's two landings ───────────────────────────────────────────────────────────────

    /// <summary>NUMVAL-F("5E+30") at working scale 9 is 5×10^39 — past the carrier: checked saturates, unchecked
    /// keeps the low-order digits (all zero, so 0 — what a MOVE into PIC V9(9) / PIC 9(5) must store, §14.6.8.2 r4).
    /// <para>⛔ NUMVAL AND NUMVAL-C ARE NOT HERE, AND THAT IS THE POINT (kb/Work PB251). §15.67.4 r1 / §15.68.4 r1
    /// fix their returned value with no arithmetic-mode qualification, so they have no scaled-<c>Int128</c>
    /// projection to land at all — the SDIDI carries every §15.67.3 r3-conforming argument exactly, and a
    /// 31-digit significand that USED to keep only its low 29 digits × 10^9 here is now simply the value.
    /// NUMVAL-F keeps this landing because §15.69.4 r2 grants its native value an approximation.</para></summary>
    [Fact]
    public void NumvalFamily_PastTheCarrier_FollowsTheLanding()
    {
        Assert.Equal(Int128.MaxValue, CobolIntrinsics.NumvalF("5E+30", 9, checkedLanding: true));
        Assert.Equal(Int128.Zero, CobolIntrinsics.NumvalF("5E+30", 9));
        Assert.Equal(Int128.Parse("-1000000000000000"), CobolIntrinsics.NumvalF("-1E+6", 9));   // inside: unchanged
        // The same 31-digit argument through the ONE NUMVAL projection: exact, with no landing to choose.
        Assert.Equal(Int128.Parse("1234567890123456789012345678901"),
            CobolIntrinsics.NumvalDec("1234567890123456789012345678901").ToUnscaled(0, CobolRounding.Truncation));
        Assert.Equal(Int128.Parse("1234567890123456789012345678901"),
            CobolIntrinsics.NumvalCDec("$1234567890123456789012345678901", "$").ToUnscaled(0, CobolRounding.Truncation));
    }

    // ── the emitter names the landing at every site ─────────────────────────────────────────────────────────

    /// <summary>The float landing has NO default form: <c>RuntimeApi.FloatToScaled</c> takes the landing as a
    /// required argument, so a new site must say which store it is — the compiler enforces it. This pins the
    /// signature so a "convenience" default cannot creep back.</summary>
    [Fact]
    public void FloatToScaled_TakesTheLandingForm_WithNoDefault()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Roslyn", "RuntimeApi.cs"));
        var m = Regex.Match(src, @"public static string FloatToScaled\(string value, string scale, CobolRounding mode, bool checkedLanding\)");
        Assert.True(m.Success, "RuntimeApi.FloatToScaled must take `bool checkedLanding` with NO default (kb/Work PB77) — the "
            + "landing form is the caller's statement to make.");
        Assert.Contains("nameof(CobolFloat.ToScaledUnchecked)", src);
    }

    /// <summary>The quantizer / exact-family renders that reach the runtime with a working scale carry the ONE
    /// <c>CheckedFlag</c> — <c>FromDouble</c>, <c>FromDoubleBounded</c>, native <c>**</c>, NUMVAL-F. A site that
    /// renders <c>FromDouble(...)</c> or a NUMVAL-F value call without it lands a no-phrase store on the sentinel
    /// again.
    /// <para>⛔ NUMVAL AND NUMVAL-C ARE NO LONGER ON THIS LIST, AND THE SECOND HALF ASSERTS THEY DO NOT COME BACK
    /// (kb/Work PB251). §15.67.4 r1 / §15.68.4 r1 fix their returned value with no arithmetic-mode qualification,
    /// so they render on the SDIDI carrier in every mode and have no working scale to land at — which is a
    /// STRONGER statement than carrying the flag, because there is no landing left to get wrong. NUMVAL-F keeps
    /// both because §15.69.4 r2 grants its native value an approximation.</para></summary>
    [Fact]
    public void EveryWorkingScaleRender_CarriesTheCheckedFlag()
    {
        string ir = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));
        string nr = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));
        foreach (Match m in Regex.Matches(ir + nr, @"RuntimeApi\.Intrinsic\(""FromDouble(Bounded)?"",[^;]*;"))
            Assert.True(m.Value.Contains("{CheckedFlag}", StringComparison.Ordinal),
                $"a FromDouble render without the landing form (kb/Work PB77): {m.Value}");
        // NUMVAL-F: the value render (not the TEST- validator, which returns a position, scale 0) — the case arm
        // from its label to the next case label.
        foreach (string name in new[] { "NumvalF" })
        {
            int at = ir.IndexOf($"case \"{name}\":", StringComparison.Ordinal);
            Assert.True(at >= 0, $"the {name} value render is gone from IntrinsicRenderer — re-point this guard.");
            int next = ir.IndexOf("case \"", at + 8, StringComparison.Ordinal);
            string arm = ir[at..(next < 0 ? ir.Length : next)];
            Assert.True(arm.Contains("{CheckedFlag}", StringComparison.Ordinal),
                $"the {name} value render lacks the landing form (kb/Work PB77): {arm}");
        }
        // ⛔ AND NUMVAL / NUMVAL-C HAVE NO WORKING-SCALE RENDER AT ALL (kb/Work PB251). A `case "Numval"` here
        // would mean a compile-time scale had come back for a value §15.67.4 r1 fixes — the defect that printed
        // 0.123456 for FUNCTION NUMVAL("0.1234567"). Asserting its ABSENCE is what keeps the removal permanent;
        // asserting the flag on a resurrected arm would only make the wrong answer well-formed.
        foreach (string name in new[] { "Numval", "NumvalC" })
            Assert.DoesNotContain($"case \"{name}\":", ir, StringComparison.Ordinal);
    }
}
