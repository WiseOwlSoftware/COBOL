// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The intrinsic-function runtime (ISO/IEC 1989:2023 §15; COBOLNET_INTRINSICS_DESIGN spine 1). One static partial
/// class per the deep-dive's runtime home: this file holds the shared conversion spine; the families live in
/// <c>CobolIntrinsics.Float.cs</c> (§15.4.1 double math), <c>CobolIntrinsics.Exact.cs</c> (exact scaled-long
/// numerics + NUMVAL), and <c>CobolIntrinsics.Text.cs</c> (character functions); date/time is
/// <see cref="CobolDate"/>. Typed-native discipline (design D1): exact numerics flow as UNSCALED
/// <see cref="Int128"/>/<see cref="long"/> values at a known decimal scale, floating-point math computes in
/// <see cref="double"/> (the §15.4.1 native-arithmetic implementor-approximation license), strings are UTF-16
/// <see cref="string"/>s. NO decimal, NO byte substrate.
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>
    /// THE one canonical double → scaled-long quantization (every floating-point intrinsic result funnels through
    /// here — singular-pattern rule). <paramref name="scale"/> is the working fraction-digit count the emitter
    /// chose (≥ 9 for float functions — hazard H1's scale floor). Rounds (never truncates) at the quantization
    /// point: ISO §15.4.1 makes the returned value an implementor-defined APPROXIMATION of the equivalent
    /// arithmetic expression, and rounding is strictly the better approximation (hazard H2 — truncation turns the
    /// double artifact LOG10(1000) = 2.9999999999999996 into 2.999999999). NaN (ACOS of |x|&gt;1, SQRT of a negative)
    /// maps to the EC-ARGUMENT-FUNCTION default result 0 — §15.3: "the implementor defines the result of the function
    /// reference" while EC checking is disabled. ±∞ SATURATES to long.Max/MinValue: it is NOT necessarily a domain
    /// error — a LEGAL class-numeric argument whose e**x / 10**x result merely overflows binary64 (EXP(710) ≈ 2.25e308
    /// = +∞) is a genuine huge result under §14.7.5 receiver handling, exactly like a FINITE over-range value
    /// (EXP10(30)). The domain-edge −∞ of LOG(0)/LOG10(0) is a real §15.55.3/§15.56.3 violation and is caught at the
    /// FUNCTION BODY (see <see cref="Float.Log"/>), never here — so this saturation cannot mask it. (CA24.)
    /// <para>
    /// ⛔ <b><paramref name="scale"/> IS NOT FREE — IT MUST BE <c>ReceiverContext.FloatWorkingScale</c> (PB13).</b>
    /// This function saturating is SAFE only when the caller chose a scale that leaves the sentinel above the
    /// receiver's digit capacity, so the store's capacity check raises the size error. That is a property of the
    /// CALLER, not of this function — it cannot be checked here, because a scaled unscaled-value carries no
    /// receiver. <c>CobolFloat.ToScaled</c> gets the same guarantee for free by landing AT the receiver's scale;
    /// this one lands at a WORKING scale, and a later rescale down is what used to hide the sentinel.
    /// </para>
    /// </summary>
    /// <remarks>
    /// ⛔ RETURNS <see cref="Int128"/>, NOT <see cref="long"/> — and that is a CORRECTNESS fix, not a widening for
    /// comfort (fix-queue PB5). The scaled domain of this compiler IS Int128 (every <c>…Scaled</c> body takes
    /// one), but this function saturated at <c>long.MaxValue</c>. Its caller quantizes at
    /// <c>ws = max(Receiver.Scale, 9)</c>, so the old clamp bit at |value| ≈ <b>9.2 × 10⁹</b> — an utterly
    /// ordinary COBOL magnitude. A twelve-digit money field is routine, and every float-family result at or above
    /// that magnitude was silently replaced by 9223372036.85:
    /// <code>
    ///   01 R PIC 9(12)V99.
    ///       COMPUTE R = FUNCTION ANNUITY(10000000000 1)   *> §15.9.4 r1b gives exactly 10000000001.00
    ///         ON SIZE ERROR ... NOT ON SIZE ERROR ...     *> prints NO SIZE ERROR
    ///   R = 00922337203685
    /// </code>
    /// <c>SQRT(1e20)</c>, <c>EXP(23.3)</c>, <c>ABS</c> and <c>MAX</c> over a COMP-2 all produced the same constant.
    /// No diagnostic, no size error — §14.7.4 never saw an overflow because the value had already been clamped to
    /// something that fits. §15.4.1's native-arithmetic licence permits an implementor-defined APPROXIMATION of
    /// the equivalent arithmetic expression; 9223372036.85 is not an approximation of 10000000001.
    /// <para>
    /// ⛔ <b>THE CLAIM THAT USED TO SIT HERE — "past the 10¹⁸ any PICTURE can describe, so the saturation is now
    /// unreachable from a declarable receiver" — IS FALSE, AND BELIEVING IT IS WHY PB5 WAS RECORDED AS CLOSED.</b>
    /// It is wrong twice over. (1) A PICTURE can describe far more than 10¹⁸: <c>PictureAnalyzer</c> caps digit
    /// positions at 31 (CA33), so <c>PIC 9(31)</c> is legal and holds 10³⁰ comfortably. (2) The clamp does not
    /// need a receiver at all — it fires at the FUNCTION's quantization point, before any store.
    /// Both are reproduced (fix-queue PB13):
    /// <code>
    ///   01 R PIC 9(31).
    ///       COMPUTE R = FUNCTION EXP(70) ON SIZE ERROR … NOT ON SIZE ERROR …
    ///   -> prints NO SIZE ERROR;  R = 0170141183460469231731687303715
    ///      where §15.34.4 r1 + §15.4.1 require ≈2.5154386709191670×10³⁰ — wrong by a factor of ~15, silently.
    ///
    ///   IF FUNCTION EXP10(30) = FUNCTION EXP10(31)   -> TRUE
    ///   -> two values a FACTOR OF TEN apart compare equal, with no receiver in the statement: a relation
    ///      operand renders under ReceiverContext.None, so ws = 9 and both sides saturate to Int128.MaxValue.
    /// </code>
    /// The saturation is therefore REACHABLE and SILENT, which is the defect PB5 fixed one instance of.
    /// ⚠ Do not "fix" this by widening the clamp: at ws = 9 the intermediate needs
    /// (receiver integer digits + 9) decimal digits, and Int128 supplies ~38, so a 31-digit receiver is 2 digits
    /// short no matter what constant is chosen.
    /// </para>
    /// <para>
    /// ✅ <b>BOTH CASES ARE CLOSED, AND THE FIX IS ENTIRELY EMITTER-SIDE — WHICH THE QUEUE ENTRY SAID IT COULD NOT
    /// BE.</b> Its recipe read "the runtime must stop silently saturating, which no emitter-side choice can
    /// reach"; that followed from taking the only emitter lever to be the working SCALE. The actual lever is
    /// WHETHER TO QUANTIZE AT ALL, and using it makes this function's saturation correct as it stands:
    /// <list type="number">
    ///   <item><b>With a receiver</b> — <c>ReceiverContext.FloatWorkingScale</c> caps the working scale at the
    ///         receiver's Int128 headroom (ws ≤ 38 − integer digits). A value that fits can no longer saturate,
    ///         and a value that does not saturates to a sentinel that still exceeds the receiver's capacity after
    ///         the rescale — so the store RAISES the size error (§14.7.5 case 5: a native intermediate out of its
    ///         implementor-defined range). <c>COMPUTE R = FUNCTION EXP(700)</c> into <c>PIC 9(31)</c> now reports
    ///         SIZE ERROR where it used to store a saturated value silently. Nothing here had to change.</item>
    ///   <item><b>With no receiver</b> — the render never reaches this function. §15.4.1 leaves "the
    ///         characteristics and representation of the returned value" to the implementor under native
    ///         arithmetic, and COBOL.NET's determination is that the float family's value IS a binary64; the
    ///         quantization exists only to land it in a fixed-point receiver, whose scale defines it. A relation
    ///         then compares natively (§8.8.4.2.4) and the text channel renders through
    ///         <c>CobolFloat.Display</c>, which is what docs/CONFORMANCE.md already required.</item>
    /// </list>
    /// ⚠ So this function is NOT the place to add a raise. Doing so would convert an EC-SIZE-TRUNCATION that the
    /// capacity check already reports correctly into an EC-SIZE-OVERFLOW with different fatality, for no gain.
    /// What keeps the guarantee true is <c>FloatQuantizeHeadroomDriftTests</c>, which proves both invariants over
    /// every legal picture shape and fails if a caller hand-rolls the working scale again.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The EC-ARGUMENT-FUNCTION screen for a float-family result that is NOT quantized — the receiver-less and
    /// float-receiver arms of <c>IntrinsicRenderer.RenderFloat</c>, which keep the value in binary64 (PB13).
    /// <para>
    /// ⛔ THIS EXISTS BECAUSE PB13 MOVED THE RAISE SITE OUT FROM UNDER THOSE ARMS. <see cref="FromDouble"/> is
    /// where an out-of-domain NaN became the §15.3 default result (or the raise, under checking), and keeping a
    /// receiver-less render in binary64 skips it — so <c>COMPUTE R = FUNCTION ACOS(2)</c> yielded the default 0
    /// while the receiver-less <c>IF FUNCTION ACOS(2) = 0</c> propagated a raw NaN and compared FALSE. Two things
    /// were wrong with that: a function's returned value must not depend on the shape of its receiver (§15.4 puts
    /// it in a temporary of the function's own characteristics), and with EC-ARGUMENT-FUNCTION checking ENABLED
    /// §14.6.13.1 requires the condition to be RAISED — which cannot happen if nothing looks at the value.
    /// </para>
    /// <para>Found by the Phase-B refute stage on §15.55 (LOG), against code landed the same day. The domain
    /// violation itself is still caught at the FUNCTION BODY where the body can name its own rule (see
    /// <see cref="Float.Log"/>, §15.55.3 r2); this is the catch-all for the families whose out-of-domain result
    /// is simply NaN — ACOS/ASIN of |x|&gt;1 (§15.8.3/§15.10.3), SQRT of a negative (§15.84.3).</para>
    /// <para>±∞ is deliberately NOT screened here, exactly as <see cref="FromDouble"/> does not screen it: an
    /// overflowing EXP is a legal huge result, not a domain error (CA24).</para>
    /// </summary>
    /// <summary>
    /// ⛔ THE §15.3 INTEGER-ARGUMENT LANDING (fix-queue PB22) — the ONE place a wide intrinsic argument narrows
    /// to the <c>long</c> the integer bodies take. The emitter used to write a bare <c>(long)(…)</c> over an
    /// <c>Int128</c>-typed expression, and <c>RoslynBackend</c> sets no <c>checkOverflow</c>, so that cast WRAPS
    /// MODULO 2⁶⁴ — silently, and BEFORE the function's own range guard can see the value.
    /// <para>
    /// The guard inside <c>CobolDate.IntegerOfDay</c> (§15.5.2 — 1601..9999 / 1..366) is correct and was simply
    /// unreachable: <c>FUNCTION INTEGER-OF-DAY(P * 100 + 62)</c> with <c>P PIC 9(18) VALUE 184467440737115466</c>
    /// evaluates 18446744073711546662, which is 2⁶⁴ + 1995046, so the cast delivered 1995046 and the function
    /// answered 143951 — a plausible date, from an argument nineteen orders of magnitude away, with NO
    /// EC-ARGUMENT-FUNCTION even under enabled checking.
    /// </para>
    /// <para>
    /// ⚠ ONE CAST FED SEVEN RENDERER ARMS over eleven functions, which is why this is a landing rather than
    /// seven repairs: a value the receiving body cannot represent is an incorrect argument (§15.3), and the
    /// place to say so is where the narrowing happens.
    /// </para></summary>
    public static long IntegerArg(Int128 v) =>
        v >= long.MinValue && v <= long.MaxValue
            ? (long)v
            : Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {v} is outside the range the function can represent (ISO §15.3)");

    /// <summary>The <see cref="IntegerArg(Int128)"/> twin for a FLOATING-POINT argument — a distinct NAME rather
    /// than an overload, deliberately: an integer literal converts implicitly to BOTH <c>Int128</c> and
    /// <c>double</c>, so an overload pair would make <c>FUNCTION FACTORIAL(5)</c> a CS0121 ambiguity. That exact
    /// collision is documented in <c>CobolIntrinsics.RealArgs.cs</c> and it broke six corpus programs once.
    /// <para>Reached only by FACTORIAL, the one arm PB21 routes to its exact body with a float argument.</para></summary>
    public static long IntegerArgReal(double v) =>
        double.IsFinite(v) && v > -9.2e18 && v < 9.2e18
            ? (long)Math.Truncate(v)
            : Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {v} is outside the range the function can represent (ISO §15.3)");

    /// <summary>The WIDE floating-point twin of <see cref="IntegerArgReal"/> — the Int128-carrier intake for the
    /// ONE §15 integer argument whose domain exceeds <c>long</c>: BOOLEAN-OF-INTEGER's argument-1 (§15.13.3 r1
    /// admits any positive integer, and the bit configuration of a value at or above 2⁶³ is legal and
    /// representable, fix-queue PB65 / RV-15.13.4-1 D1). The range guard follows <see cref="FromDouble"/>'s
    /// saturation constant: 1.7e38 is BELOW <c>Int128.MaxValue</c>, so the boundary value cannot slip through
    /// the cast (the PB22 argument, at the wide carrier).</summary>
    public static Int128 IntegerArgWideReal(double v) =>
        double.IsFinite(v) && v > -1.7e38 && v < 1.7e38
            ? (Int128)Math.Truncate(v)
            : Exceptions.ExceptionState.ArgumentError(
                $"the intrinsic integer argument {v} is outside the range the function can represent (ISO §15.3)");

    public static double RealResult(double d) => double.IsNaN(d)
        ? Exceptions.ExceptionState.ArgumentError("floating-point intrinsic argument out of domain (NaN result)")
        : d;

    /// <summary>The scale-37 maxima of the bounded codomains (fix-queue PB65 / RV-15.75.4-1): the largest
    /// unscaled magnitude PERMITTED inside each codomain, at 37 fraction digits — so the per-scale limit is one
    /// exact integer division, <c>max37 / 10^(37−scale)</c> (floor, for a positive dividend), never a double
    /// computation that loses the bound past 15 digits. For the OPEN unit bound the constant is 10³⁷ − 1
    /// (equality excluded); for the irrational π bounds it is ⌊bound·10³⁷⌋ (equality unreachable — the bound is
    /// not a decimal), independently derived: π to 40 places via Decimal in python, floor at 37. Both fit
    /// Int128 (max ≈ 1.70e38).</summary>
    public static readonly Int128 CodomainBelowOne37 = Int128.Parse("9999999999999999999999999999999999999");
    public static readonly Int128 CodomainHalfPi37 = Int128.Parse("15707963267948966192313216916397514420");
    public static readonly Int128 CodomainPi37 = Int128.Parse("31415926535897932384626433832795028841");

    /// <summary>
    /// ⛔ THE BOUNDED-CODOMAIN QUANTIZER (fix-queue PB65 / RV-15.75.4-1). <see cref="FromDouble"/>'s
    /// away-from-zero rounding exists to absorb binary64 artifacts (SQRT(10) ** 2 → 10, NIST IF136A) and must
    /// stay — but for a function whose §15.x.4 rule bounds the returned value, the top half-ulp of the codomain
    /// rounds OUT of it: RANDOM's [0, 1) produced exactly 1.000000000 in a PIC 9V9(9) receiver (§15.75.4 r1's
    /// "less than one" is strict), and ASIN(1) at scale 9 rounded to 1.570796327 &gt; π/2 — outside §15.10.4 r1's
    /// CLOSED bound, because the bound is irrational and the nearest scaled value above it is not "equal to π/2".
    /// The DOUBLE never exits the codomain (Math.PI/2 &lt; π/2; NextDouble() &lt; 1); only the quantization does,
    /// which is why the clamp acts on the QUANTIZED value. The clamp is symmetric in magnitude — every family
    /// member's codomain is symmetric or zero-anchored on the safe side (ACOS ≥ 0, RANDOM ≥ 0 hold in double
    /// already). The limit divides exactly from the scale-37 constant; <paramref name="scale"/> ≤ 31 always
    /// (a working scale is capped at the receiver's Int128 headroom and a PICTURE at 31 digits, §13.18.40.3 SR14).
    /// </summary>
    public static Int128 FromDoubleBounded(double d, int scale, Int128 max37)
    {
        Int128 q = FromDouble(d, scale);
        Int128 limit = max37 / Pow10.AsWide(37 - scale);
        return q > limit ? limit : q < -limit ? -limit : q;
    }

    public static Int128 FromDouble(double d, int scale)
    {
        // EC-ARGUMENT-FUNCTION raise point (§14.6.13.1.6 — the exception-condition table gives it Fatal): the
        // §15.3 default 0 when checking is off, the raise (throw) when the statement carries enabled checking (the
        // ambient gate). NaN only — an over-range ±∞ is a legal overflow that saturates like a finite over-range
        // value (the receiver store size-truncates).
        if (double.IsNaN(d)) return Exceptions.ExceptionState.ArgumentError("floating-point intrinsic argument out of domain (NaN result)");
        if (double.IsInfinity(d)) return d > 0 ? Int128.MaxValue : Int128.MinValue;
        double scaled = d * Pow10.AsDouble(scale);
        if (scaled >= 1.7e38) return Int128.MaxValue;
        if (scaled <= -1.7e38) return Int128.MinValue;
        return (Int128)Math.Round(scaled, MidpointRounding.AwayFromZero);
    }

}
