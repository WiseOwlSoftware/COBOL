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
    /// = +∞) is a genuine huge result under §14.7.4 receiver handling, exactly like a FINITE over-range value
    /// (EXP10(30)). The domain-edge −∞ of LOG(0)/LOG10(0) is a real §15.55.3/§15.56.3 violation and is caught at the
    /// FUNCTION BODY (see <see cref="Float.Log"/>), never here — so this saturation cannot mask it. (CA24.)
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
    /// At scale 9 the Int128 ceiling is ≈1.7 × 10²⁹, which is past the 10¹⁸ any PICTURE can describe, so the
    /// saturation is now unreachable from a declarable receiver rather than merely further away.
    /// </para>
    /// </remarks>
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
