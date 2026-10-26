// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN INT128 LANDING AT A WORKING SCALE MUST NEVER SATURATE SILENTLY (fix-queue PB13; since kb/Work PB3839 the
/// one remaining working-scale landing is an SDIDI intermediate's, <c>ReceiverContext.WorkingScale</c> at
/// <c>SdidiLandingScaleFloor</c>: the float family lands at the receiver's own scale, <c>FloatLanding</c>).
/// <para>
/// The landing that PB13 found (<c>CobolIntrinsics.FromDouble</c> at a float working scale) put a value at a WORKING scale and saturated at <c>Int128.MaxValue</c>.
/// The store then rescales working→receiver scale, which DIVIDES the saturation sentinel back down — so the
/// receiver's digit-capacity check, the one thing that would have raised the size error, never sees it. With the
/// old flat <c>ws = max(Scale, 9)</c>, <c>COMPUTE R = FUNCTION EXP(70)</c> into a <c>PIC 9(31)</c> stored
/// 0170141183460469231731687303715 — wrong by a factor of ~15 — and reported NO SIZE ERROR.
/// </para>
/// <para>
/// <see cref="ReceiverContext.WorkingScale"/> is the fix and this class is its proof. The two invariants
/// below are what make the capped working scale SUFFICIENT rather than merely better, and neither is observable
/// from any single golden: a golden shows one receiver behaving, these show that EVERY legal picture does.
/// The corpus goldens <c>pb13_float_quantize_headroom</c> and <c>pb13_float_quantize_siblings</c> pin the
/// behaviour; this pins the rule.
/// </para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FloatQuantizeHeadroomDriftTests
{
    /// <summary>Every (integer-digits, scale) pair a legal PICTURE can present: ISO §13.18.40.3 SR14 caps the
    /// total digit positions at 31.</summary>
    public static TheoryData<int, int> LegalPictureShapes()
    {
        var data = new TheoryData<int, int>();
        for (int intDigits = 0; intDigits <= 31; intDigits++)
            for (int scale = 0; scale + intDigits <= 31; scale++)
                data.Add(intDigits, scale);
        return data;
    }

    private static ReceiverContext Rcv(int intDigits, int scale) =>
        new(scale, Real: false, CobolRounding.Truncation, InSizeError: false, IntegerDigits: intDigits);

    /// <summary>
    /// INVARIANT 1 — a value that FITS the receiver never saturates. The receiver holds |v| &lt; 10^intDigits, the
    /// quantizer forms v × 10^ws, and Int128 holds any value below 10³⁸; so intDigits + ws ≤ 38 is exactly the
    /// condition under which a representable value survives quantization.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalPictureShapes))]
    public void AValueThatFitsTheReceiver_NeverSaturates(int intDigits, int scale)
    {
        int ws = Rcv(intDigits, scale).WorkingScale(ReceiverContext.SdidiLandingScaleFloor);
        Assert.True(intDigits + ws <= ReceiverContext.IntermediateDigits,
            $"PIC with {intDigits} integer digits at scale {scale} quantizes at ws={ws}: "
            + $"{intDigits}+{ws} > {ReceiverContext.IntermediateDigits} Int128 digits, so a value that FITS the "
            + "receiver would saturate — the PB13 defect.");
    }

    /// <summary>
    /// INVARIANT 2 — and the one that makes the cap SUFFICIENT: a value that does NOT fit always trips the
    /// receiver's capacity check, so the saturation can no longer be silent. The sentinel descales to
    /// 1.7014×10^(38−ws), which must exceed the receiver's maximum 10^intDigits − 1.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalPictureShapes))]
    public void AValueThatOverflowsTheReceiver_AlwaysTripsTheCapacityCheck(int intDigits, int scale)
    {
        int ws = Rcv(intDigits, scale).WorkingScale(ReceiverContext.SdidiLandingScaleFloor);
        Assert.True(ReceiverContext.IntermediateDigits - ws >= intDigits,
            $"PIC with {intDigits} integer digits at scale {scale} quantizes at ws={ws}: the Int128.MaxValue "
            + $"sentinel descales to ~1.7e{ReceiverContext.IntermediateDigits - ws}, which does NOT exceed the "
            + $"receiver's 10^{intDigits} capacity — a saturated value would store SILENTLY.");
    }

    /// <summary>No receiver-visible fraction digit is ever surrendered to the cap: the working scale stays at or
    /// above the receiver's own scale for every legal picture (38 − intDigits ≥ 7 ≥ scale whenever
    /// intDigits + scale ≤ 31).</summary>
    [Theory]
    [MemberData(nameof(LegalPictureShapes))]
    public void TheWorkingScale_NeverFallsBelowTheReceiverScale(int intDigits, int scale)
    {
        int ws = Rcv(intDigits, scale).WorkingScale(ReceiverContext.SdidiLandingScaleFloor);
        Assert.True(ws >= scale, $"PIC with {intDigits} integer digits at scale {scale} quantizes at ws={ws} — "
            + "below the receiver's own scale, so the receiver loses fraction digits it can hold.");
    }

    /// <summary>
    /// ⛔ THE BLAST RADIUS IS MEASURED, NOT ASSERTED. The cap BINDS only past <c>38 − floor</c> integer digits; below
    /// that the floor still wins and the working scale is byte-identical to the pre-PB13 <c>max(Scale, floor)</c>.
    /// That is why landing PB13 moved no ordinary-picture behaviour — and this test is what keeps that true, so a
    /// future change to the rule cannot quietly re-scale every money field in the corpus.
    /// </summary>
    [Theory]
    [MemberData(nameof(LegalPictureShapes))]
    public void BelowThirtyIntegerDigits_TheRuleIsUnchangedFromTheOldFloor(int intDigits, int scale)
    {
        if (intDigits > ReceiverContext.IntermediateDigits - ReceiverContext.SdidiLandingScaleFloor) return;
        Assert.Equal(Math.Max(scale, ReceiverContext.SdidiLandingScaleFloor),
            Rcv(intDigits, scale).WorkingScale(ReceiverContext.SdidiLandingScaleFloor));
    }

    /// <summary>A receiver-less context keeps the bare floor — the rule must still answer for it, and it must
    /// answer with the floor rather than a cap derived from an absent receiver.</summary>
    [Fact]
    public void TheReceiverlessContext_KeepsTheFloor_AndIsMarkedReceiverless()
    {
        Assert.Equal(ReceiverContext.SdidiLandingScaleFloor,
            ReceiverContext.None.WorkingScale(ReceiverContext.SdidiLandingScaleFloor));
        Assert.True(ReceiverContext.None.Receiverless,
            "ReceiverContext.None must be marked Receiverless — that flag, not IntegerDigits == 0, is what lets a "
            + "float-family render keep its binary64 value. An all-fraction receiver (PIC V9(9)) also has zero "
            + "integer digits but DOES define a quantization scale.");
    }

    /// <summary>
    /// ⛔ SOURCE-FORM GUARD: every working-scale site must consume the ONE rule. No runtime test can see this
    /// mistake — a site that hand-rolls <c>Math.Max(rcv.Scale, N)</c> produces correct output for every ordinary
    /// picture and is wrong only past the headroom, which is precisely how PB13 survived PB5.
    /// <para>
    /// ⚠ THE FLOOR IS PART OF THE PATTERN, NOT PART OF THE SITE. The first version of this guard matched only
    /// <c>, 9)</c> — the float family's floor — and passed while three NUMVAL-family sites carried the identical
    /// defect at floor 6. It caught the float sites on its very first run and MISSED those, so the alternation is
    /// on <c>\d+</c> deliberately. (It did earn its keep immediately: it found the NUMVAL-F site, whose runtime
    /// still clamped at <c>long.MaxValue</c> — PB5's own defect, in the sibling PB5 never swept; that site has since been
    /// deleted by kb/Work PB3839, which made NUMVAL-F the SDIDI value its siblings are.)
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs")]
    [InlineData("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "NumericRenderer.cs")]
    public void EveryQuantizeSite_ConsumesTheOneWorkingScaleRule(string proj, string a, string b, string file)
    {
        string src = File.ReadAllText(TestRepo.Src(proj, a, b, file));
        if (file == "NumericRenderer.cs")
            Assert.Contains("WorkingScale(", src, StringComparison.Ordinal);   // the SDIDI landing's one working-scale site
        // The pre-PB13 hand-rolled form, at ANY floor and in any spelling that reads a receiver's scale.
        var offender = Regex.Match(src, @"Math\.Max\(\s*[\w\.]*[Rr]eceiver?\w*\.Scale\s*,\s*\d+\s*\)");
        Assert.False(offender.Success,
            $"{file} still computes a working scale by hand: '{offender.Value}'. The capacity cap lives on "
            + "ReceiverContext.WorkingScale; a hand-rolled max() silently drops it (PB13).");
    }
}
