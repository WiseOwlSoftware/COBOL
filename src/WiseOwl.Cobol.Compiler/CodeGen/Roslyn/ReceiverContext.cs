// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// The RECEIVER a numeric render is computed FOR (P7 Step 3; DESIGN-codegen-backend §2.5): the receiving item's
/// scale (the division/power working scale, ISO §8.8.1/§14.7.4), its INTEGER-DIGIT CAPACITY (the float
/// landing headroom — see <see cref="WorkingScale"/>), whether it is a floating-point receiver (the
/// whole RHS then evaluates in IEEE binary64 — D16), its ROUNDED mode, and which size error conditions the statement
/// TAKES (<see cref="SizeChecks"/>: a sum renders the checked <c>AddChecked</c> when it takes EC-SIZE-OVERFLOW;
/// ISO §14.7.5, §14.6.13.1.1). Passed BY PARAMETER into every public numeric-render entry — replacing the
/// four mutable <c>EmitContext.Target*</c>/<c>InSizeErrorContext</c> fields whose manual set/reset discipline
/// was the H1 staleness hazard (a receiver-less render inheriting the PREVIOUS statement's receiver). A site with
/// no receiver in scope passes <see cref="None"/> (scale 0, no capacity, fixed-point, truncation, unchecked).
/// </summary>
/// <param name="IntegerDigits">
/// The receiver's INTEGER digit positions — its §13.18.40.3 SR14 <c>DigitPositions</c> less its scale, never
/// negative. <b>0 means "no integer-digit constraint"</b>, which is true both of a receiver-less context
/// (<see cref="None"/>) and of an all-fraction receiver such as <c>PIC V9(9)</c>; the two want the same working
/// scale, so one encoding serves both. Over-counting is SAFE and under-counting is not (see
/// <see cref="WorkingScale"/>), which is why the count is taken from <c>DigitPositions</c> — the measure
/// that includes Z/*/floating-sign/P positions — rather than from the '9' count.
/// </param>
/// <param name="Receiverless">
/// True for <see cref="None"/> alone: NO fixed-point receiver governs this render (a relation operand, a sign
/// condition, a DISPLAY/STRING text operand, a MOVE source — whose landing its own emitter owns). It is a
/// SEPARATE flag from <c>IntegerDigits == 0</c> deliberately: an all-fraction receiver such as <c>PIC V9(9)</c>
/// also has no integer digits, but it does have a scale that defines a quantization, and only a genuinely
/// receiver-less render may keep the float family's value in binary64
/// (<c>IntrinsicRenderer.RenderFloat</c> / <c>NumericRenderer.Power</c>, PB13).
/// </param>
/// <param name="FloatEdited">
/// True when the resultant identifier is a FLOATING-POINT numeric-edited item (data-model design D21 /
/// kb/Work PB66). Such a receiver has NO fixed fraction scale for the ROUNDED transfer — the result normalizes
/// into the mask and the store rounds its significand to the mask's digits by the receiver's mode
/// (<c>CobolEdit.TryFormatFloat</c> / <c>FormatFloatStore</c>, kb/Work PB2638) — so <see cref="Scale"/> carries the
/// mask's significand scale only as a working-scale hint (<c>RuntimeApi.ReceiverScaleOf</c>) and is NOT the
/// scale a final transfer rounds at. <see cref="FloatLanding"/> is the one reader.
/// </param>
internal readonly record struct ReceiverContext(
    int Scale, bool Real, CobolRounding Rounding, SizeChecks Checks, int IntegerDigits = 0,
    bool Receiverless = false, bool FloatEdited = false)
{
    /// <summary>The receiver-less context: scale 0, no capacity, not floating, TRUNCATION, no size-error checking.</summary>
    public static readonly ReceiverContext None =
        new(0, false, CobolRounding.Truncation, SizeChecks.None, 0, Receiverless: true);

    /// <summary>⛔ THE ONE RULE FOR "THE CONTEXT OF A RESULTANT DESCRIBED BY <paramref name="pic"/>" — its scale
    /// (<c>PicInfo.ReceiverScale</c>), its floating-point-ness, its INTEGER digit positions (measured from
    /// <c>DigitPositions</c>, never from the '9' count: over-counting is safe, under-counting is not — see
    /// <see cref="IntegerDigits"/>) and its floating-point-edited-ness. A null PICTURE (an index, an object) is
    /// <see cref="None"/>'s scale 0 / no capacity, still bearing a receiver.
    /// <para>It is what an ARITHMETIC statement builds for each resultant identifier (<c>ArithmeticEmitter.RcvFor</c>)
    /// and what a CALL or INVOKE argument expression builds for its FORMAL PARAMETER: §14.2.3 GR9/GR10 make the
    /// crossing "a COMPUTE statement without the ROUNDED phrase" into a record of the formal's description
    /// (kb/Work PB289), so the expression is evaluated FOR that description — a quotient to the formal's own
    /// fraction digits, a floating-point formal in binary64 — and never at the receiver-less working scale, which
    /// silently dropped the digits the formal could hold. Two lanes, one rule: before this factory the arithmetic
    /// statement and the argument crossing each derived the context, and the crossing derived none.</para></summary>
    public static ReceiverContext Of(PicInfo? pic, CobolRounding rounding, SizeChecks checks)
    {
        int scale = pic?.ReceiverScale() ?? 0;
        return new ReceiverContext(scale, pic is { IsFloat: true }, rounding, checks,
            pic is null ? 0 : Math.Max(0, pic.DigitPositions - scale), FloatEdited: pic is { IsFloatEdited: true });
    }

    /// <summary>⛔ THE ONE RULE FOR "THE CONTEXT OF A SET OF RESULTANTS" (kb/Work PB2694): the context a statement with
    /// SEVERAL resultant identifiers renders its once-evaluated operands (or its one initial evaluation) FOR. One
    /// rendered value has to land in every receiver, so the set takes the WIDEST receiver scale (no receiver-visible
    /// digit is lost), the widest INTEGER part (the most constraining float-quantization headroom, PB13), is
    /// floating-point only when EVERY receiver is (D16), and is <see cref="FloatEdited"/> when ANY receiver is — a
    /// floating-point numeric-edited resultant has no fraction scale to carry a quotient AT, so a nested quotient
    /// rendered for the set must not be cut to the widest FIXED receiver's scale plus guard digits (the digits that
    /// receiver's normalized significand needs are far below them). It is <see cref="Truncation"/>-moded: the set has
    /// no single ROUNDED mode — each receiver's own final transfer is rendered by <see cref="Of"/> (kb/Work PB2694;
    /// before it five emit sites each spelled this out again and none carried the floating-point edited flag).</summary>
    public static ReceiverContext OfSet(IReadOnlyList<PicInfo?> pics, SizeChecks checks)
    {
        var (scale, integerDigits) = PicInfo.WidestReceiving(pics);
        bool allFloat = true, anyFloatEdited = false;
        foreach (var pic in pics)
        {
            allFloat &= pic is { IsFloat: true };
            anyFloatEdited |= pic is { IsFloatEdited: true };
        }
        return new ReceiverContext(scale, allFloat, CobolRounding.Truncation, checks, integerDigits,
            FloatEdited: anyFloatEdited);
    }

    /// <summary>Decimal digits the <c>Int128</c> intermediate carrier holds:<c>Int128.MaxValue</c> ≈ 1.7014×10³⁸,
    /// so any value below 10³⁸ is representable (numeric design D1 — the "Int128 escape boundary").</summary>
    public const int IntermediateDigits = PicInfo.ScaledCarrierDigits;

    /// <summary>
    /// The floor <c>NumericRenderer.Landed</c> uses when it lands an SDIDI intermediate into the exact
    /// <c>Int128</c> lane for a VALUE-SEMANTICS consumer that supplies no scale of its own (an intrinsic argument,
    /// a SET pointer UP/DOWN BY amount, an ALLOCATE character count): 6 fraction digits. (Not a CALL argument: one
    /// no formal can see crosses at its SDIDI's own exponent, <c>CobolArgAdapt.SdidiValue</c>, kb/Work PB3677.) It is the ONLY consumer of the constant.
    /// <para>⛔ IT IS NOT A NUMVAL RULE, AND CALLING IT ONE HELD A WRONG ANSWER OPEN (kb/Work PB251). Named
    /// <c>NumvalScaleFloor</c>, it read as though §15.67/§15.68/§15.69 prescribed a 6-digit working scale. They
    /// prescribe no working scale at all: §15.67.4 r1 / §15.68.4 r1 fix the returned value as "the numeric value
    /// represented by argument-1" with no arithmetic-mode qualification, so NUMVAL and NUMVAL-C render on the
    /// SDIDI carrier in every mode and never consult this constant, and neither does NUMVAL-F, whose §15.69.4 r2
    /// approximation licence is resolved to r3's exact value (kb/Work PB3839). What remains is an IMPLEMENTOR
    /// choice for the one landing with no receiver to derive a scale from — the <see cref="WorkingScale"/> CAP
    /// above it is a correctness requirement, this floor is not.</para>
    /// </summary>
    public const int SdidiLandingScaleFloor = 6;

    /// <summary>
    /// The WORKING SCALE an SDIDI intermediate lands at in the exact <c>Int128</c> lane for this receiver (the ONE place
    /// that rule is written down, feedback_one_rule_one_place): the receiver's scale, never below
    /// <paramref name="floor"/>, CAPPED so that the scaled value still fits the carrier: <c>ws ≤ 38 − IntegerDigits</c>.
    /// <para>
    /// ⛔ <b>THE CAP IS A CORRECTNESS REQUIREMENT, NOT A TUNING KNOB (fix-queue PB13).</b> A landing puts the
    /// value at this working scale and SATURATES at <c>Int128.MaxValue</c>; the store then rescales ws→receiver
    /// scale, which DIVIDES the saturation sentinel back down and so destroys the very evidence the receiver's
    /// digit-capacity check exists to see. With a flat <c>ws = max(Scale, 9)</c> a <c>PIC 9(31)</c> receiver
    /// needed 31+9 = 40 digits where Int128 supplies 38, so <c>COMPUTE R = FUNCTION EXP(70)</c> stored
    /// 0170141183460469231731687303715 — wrong by a factor of ~15 — and reported NO SIZE ERROR.
    /// </para>
    /// <para>
    /// The cap makes BOTH outcomes correct, and the second one is why it is sufficient rather than merely better:
    /// <list type="number">
    ///   <item>a value that FITS the receiver never saturates — |v| &lt; 10^IntegerDigits and
    ///         ws ≤ 38 − IntegerDigits give |v|×10^ws &lt; 10³⁸ &lt; Int128.MaxValue;</item>
    ///   <item>a value that does NOT fit always trips the receiver's capacity check — the sentinel descales to
    ///         1.7014×10^(38−ws) ≥ 1.7014×10^IntegerDigits, which exceeds the receiver's maximum
    ///         10^IntegerDigits − 1 for every ws ≤ 38 − IntegerDigits. So the saturation can no longer be
    ///         SILENT: it is guaranteed to raise the size error condition (§14.7.5 case 5).</item>
    /// </list>
    /// That is the same discipline <c>CobolFloat.ToScaled</c> already relies on — a landing past the carrier is a size error exactly when
    /// the landing scale keeps the sentinel above the receiver's capacity, and unsafe when a later rescale shrinks
    /// it back into range. UNDER-counting <see cref="IntegerDigits"/> would break invariant 2; over-counting only
    /// costs fraction digits, which is why the count is deliberately generous.
    /// </para>
    /// <para>
    /// The cap only BINDS for receivers with more than 38 − <paramref name="floor"/> integer digits (below that the
    /// floor wins), so it is behaviour-neutral for every ordinary picture. It can never fall below the
    /// receiver's own scale for a legal picture: §13.18.40.3 SR14 caps digit positions at 31, so
    /// 38 − IntegerDigits ≥ 38 − 31 = 7 ≥ Scale whenever IntegerDigits + Scale ≤ 31.
    /// </para>
    /// <para>⛔ A FLOOR IS A CHOICE AND MUST NEVER BE READ AS A FUNCTION'S RULE (kb/Work PB251): the NUMVAL floor
    /// that used to be listed here was never §15.67's — the family has no working scale at all — and the name
    /// alone kept a truncating native lane looking spec-derived. Every floor passed here is an implementor choice
    /// under §8.8.1.2 / §15.4.1; the cap is the only part the standard forces. (The float family has no floor of its
    /// own: its value lands at the receiver's own scale, <see cref="FloatLanding"/>.)</para>
    /// </summary>
    public int WorkingScale(int floor)
    {
        int want = Math.Max(Scale, floor);
        int headroom = IntermediateDigits - Math.Clamp(IntegerDigits, 0, IntermediateDigits);
        return Math.Max(0, Math.Min(want, headroom));
    }

    /// <summary>
    /// ⛔ THE ONE PLACE a NATIVE-arithmetic float value's LANDING is decided — WHETHER it is quantized into the
    /// fixed-point <c>Int128</c> lane at all, and when it is, at which SCALE and with which ROUNDING MODE
    /// (kb/Work PB647, then PB653). The §15.4.1 float-intrinsic family
    /// (<c>IntrinsicRenderer.RenderFloatNative</c>) and native <c>**</c> (<c>NumericRenderer.Power</c>) are its
    /// two consumers, and they must agree because one returned value must not land two ways.
    /// <para>
    /// <paramref name="finalTransfer"/> is the caller's <c>NumericRenderer.Outermost</c> — TRUE exactly when the
    /// value IS the value transferred to a single resultant identifier. There are exactly TWO answers, quantize at a
    /// fixed-point resultant or keep the binary64, and the second covers the last two shapes:
    /// <list type="bullet">
    ///   <item><b>The final transfer into a fixed-point resultant</b> quantizes AT that identifier's scale with
    ///     the statement's own ROUNDED mode. §14.7.4.1: "If, after decimal point alignment, the number of places
    ///     in the fractional part of the result of an arithmetic operation is greater than the number of places
    ///     provided for the fraction of the resultant identifier, truncation is relative to the size provided for
    ///     the resultant identifier" (cite.py-verified) — the decision is made ONCE, at the receiver's scale, so
    ///     the store's rescale is the identity and no second rounding can contradict it.</item>
    ///   <item><b>A FLOATING-POINT numeric-edited resultant</b> (<see cref="FloatEdited"/>) is no final transfer
    ///     for this purpose: it has no fixed fraction scale to round at. The result normalizes into the mask and
    ///     the STORE rounds its significand to the mask's digits by the receiver's mode (kb/Work PB2638; data-model
    ///     D21 / kb/Work PB66), so the value keeps its binary64 like every shape below. ⛔ It used to land at the
    ///     capped ≥ 9 working scale with TRUNCATION first (kb/Work PB3829's sweep), which is exactly the loss of
    ///     small magnitudes a floating mask exists to avoid: <c>COMPUTE FE ROUNDED = FUNCTION SQRT(1.5241383936E-24)</c>
    ///     into <c>PIC +9.999E+99</c> displayed +0.000E+00 where the same value through <c>+ 0</c> displayed
    ///     +1.235E-12.</item>
    ///   <item><b>EVERYTHING ELSE KEEPS THE BINARY64 AND QUANTIZES NOT AT ALL</b> — a NESTED intermediate, a
    ///     float receiver, a receiver-less render. There is no transfer here, so there is no scale to quantize
    ///     TO; the ONE quantization happens at the receiver store, on the whole expression's value.</item>
    /// </list>
    /// </para>
    /// <para>
    /// ⛔ WHY THE NESTED ARM QUANTIZES NOTHING, AND WHY A GUARD-DIGIT WORKING SCALE IS NOT THE ANSWER
    /// (kb/Work PB653 — the defect this arm closes). PB647 landed a nested intermediate at the ≥ 9 float working
    /// scale with TRUNCATION, which truncates the returned value one digit too early and lets every operation
    /// above it propagate the loss: with <c>R PIC 9V9(9)</c>, <c>COMPUTE R = FUNCTION SQRT(3) * 2</c> gave
    /// 3.464101614 where a <c>COMP-2</c> item holding the IDENTICAL binary64 gave the correct 3.464101615, and
    /// <c>COMPUTE R = FUNCTION SQRT(10) ** 2</c> gave 9.999999998 against the COMP-2 arm's 10.000000000.
    /// §15.4.1 forbids the split outright — "the returned value is the same for all instances of a given function
    /// within a single execution of the runtime element so long as the value and order of the arguments, the
    /// collating sequence, and the locale are unchanged" — and the RELATION channel, which never quantized,
    /// already AGREED with the COMP-2 item, so the arithmetic channel was the one arm out of step
    /// (feedback_two_arm_dispatch).
    /// </para>
    /// <para>
    /// The obvious repair — give the nested arm guard digits, as <c>NumericRenderer.Divide</c>'s nested arm
    /// does — is WRONG, and measurably so: a landing does not know how many multiplications sit above it, so a
    /// guarded scale ACCUMULATES. At <c>CobolNum.NestedQuotientGuardDigits</c>'s 14 a nested float operand would land at scale 23,
    /// and <c>FUNCTION SQRT(10) * FUNCTION SQRT(10)</c> would multiply two scale-23 operands into a scale-46
    /// product — past the <see cref="IntermediateDigits"/> carrier, where the unchecked no-phrase store WRAPS.
    /// <c>Divide</c> can afford a guard only because it adds the guard to its operands' PRECISION, never to an
    /// intermediate's scale, and falls to the SDIDI when its digit bound passes the carrier (kb/Work PB3538: the
    /// guard it once added to the dividend's scale compounded with depth exactly this way); a float landing has
    /// no such bound. Keeping the binary64 removes the question instead of tuning it: the value IS
    /// then the same object a <c>COMP-2</c> item holds, <c>CombineCore</c>'s <c>Real</c> lane evaluates the whole
    /// expression in IEEE binary64 (numeric design D16, on §8.8.1.3's "implementor-defined method of evaluating
    /// an arithmetic expression, an arithmetic statement, the SUM clause, and all integer and numeric
    /// functions"), and §15.4.1's identity holds BY CONSTRUCTION across the arithmetic, relation, text and MOVE
    /// channels rather than by arithmetic luck.
    /// </para>
    /// <para>
    /// ⛔ NO LANDING CLAMPS TO A FUNCTION'S CODOMAIN (kb/Work PB3829, overturning PB65's clamp). A §15.x.4 bound is
    /// a rule on the RETURNED value, which every arm here leaves untouched; the final-transfer arm then rounds it
    /// once at the resultant identifier per §14.7.4.3, and the value it lands may lie past the bound (ACOS(-1)
    /// ROUNDED into <c>9V9999</c> is 3.1416, beyond π) or fail to fit (a size error, §14.7.5), exactly as the same
    /// value does when it arrives through a nested expression or a <c>COMP-2</c> item.
    /// </para>
    /// <para>
    /// ⛔ WHAT PB647 ORIGINALLY CLOSED, KEPT BECAUSE IT IS WHY THE FINAL-TRANSFER ARM READS AS IT DOES. The
    /// quantizer hard-coded <see cref="CobolRounding.NearestAwayFromZero"/> at the working scale in BOTH
    /// positions, so with <c>S PIC 9V9(9)</c>, <c>MOVE FUNCTION SQRT(3) TO S</c> gave 1.732050807
    /// (§14.6.8.2 rule 4 — "the data is aligned by decimal point and is transferred to the receiving digits with
    /// zero fill or truncation on either end") while <c>COMPUTE S = FUNCTION SQRT(3)</c> gave 1.732050808, and
    /// <c>SQRT(0.9999999999)</c> into <c>PIC 9V9</c> split across a whole tenth (0.9 against 1.0). The same
    /// hard-coding made the ROUNDED phrase a NO-OP on this family, where §14.7.4.3 rule 2 says "If the ROUNDED
    /// phrase is not specified, execution is as if ROUNDED MODE IS TRUNCATION had been specified".
    /// </para>
    /// <para>
    /// ⚠ <c>NUMVAL-F</c> DOES NOT CONSULT THIS DECISION, AND THAT IS NOT DRIFT (kb/Work PB3839). Its value is not a
    /// binary64: §15.69.4 r2 grants native arithmetic "an approximation" and the implementor's determination is r3's
    /// exact value in every mode and channel (<c>IntrinsicCarrierRules.ReturnsExactParse</c>), so a fixed-point
    /// receiver rounds it ONCE at its own scale and a floating-point receiver converts it once, through the SDIDI landing.
    /// </para>
    /// </summary>
    public FloatLandingDecision FloatLanding(bool finalTransfer) =>
        finalTransfer && RoundsAtItsScale ? FloatLandingDecision.At(Scale, Rounding) : FloatLandingDecision.Binary64;

    /// <summary>⛔ THE ONE PREDICATE "A FINAL TRANSFER INTO THIS RESULTANT ROUNDS AT <see cref="Scale"/> WITH
    /// <see cref="Rounding"/>": a receiver that is neither receiver-less, nor floating-point, nor a FLOATING-POINT
    /// numeric-edited item (whose <see cref="Scale"/> is only the mask's working-scale hint: the value normalizes into
    /// the mask, data-model D21 / kb/Work PB66). The exact-product transfer (<c>NumericRenderer.Settle</c>, the
    /// <c>MulAtScale</c> arm of <c>Multiply</c>), the outermost quotient (<c>Divide</c>), the MAX/MIN selection
    /// (<c>IntrinsicRenderer</c>) and <see cref="FloatLanding"/> ask it; a floating-point edited receiver otherwise saw
    /// <c>COMPUTE FE = A * A</c> (A = 10^20) carried at its hint scale and wrapped to 0, and <c>1 / 3000000</c> truncated
    /// to 3.3E-07 (train 1039b review of kb/Work PB1900; golden 2023/pb1900_float_edited_resultant_scale).</summary>
    public bool RoundsAtItsScale => !Receiverless && !Real && !FloatEdited;
}

/// <summary>
/// The answer <see cref="ReceiverContext.FloatLanding"/> gives: either the value STAYS a binary64
/// (<see cref="Binary64"/> — a nested intermediate, a float receiver, a receiver-less render) or it is
/// QUANTIZED into the fixed-point <c>Int128</c> lane at <see cref="Scale"/> with <see cref="Mode"/>.
/// <para>⛔ IT IS A DECISION AND NOT A SCALE, DELIBERATELY (kb/Work PB653): exactly TWO answers, quantize at a
/// fixed-point resultant's scale and mode or keep the binary64 (PB3829 removed the third, the clamp to a function's
/// codomain). While the method returned a bare <c>(scale, mode)</c> pair, "do not quantize at all" had nowhere to
/// live, so BOTH consumers wrote the receiver-shape half of the rule out again for themselves —
/// <c>if (Receiver.Real || Receiver.Receiverless)</c> in <c>IntrinsicRenderer.RenderFloatNative</c> and
/// <c>if (b.Real || e.Real || _rcv.Real || _rcv.Receiverless)</c> in <c>NumericRenderer.Power</c> — and neither
/// of those two copies learned about the nested case. <c>Quantize</c> is what makes the NEXT consumer inherit
/// the whole rule instead of half of it; <c>FloatLandingModeDriftTests</c> holds that true.</para>
/// </summary>
internal readonly record struct FloatLandingDecision(bool Quantize, int Scale, CobolRounding Mode)
{
    /// <summary>Keep the value in IEEE binary64 — there is no transfer here, so there is no scale to quantize to.</summary>
    public static readonly FloatLandingDecision Binary64 = new(false, 0, CobolRounding.Truncation);

    /// <summary>Quantize into the <c>Int128</c> lane at <paramref name="scale"/> with <paramref name="mode"/>.</summary>
    public static FloatLandingDecision At(int scale, CobolRounding mode) => new(true, scale, mode);
}
