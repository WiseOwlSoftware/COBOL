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
    /// double artifact LOG10(1000) = 2.9999999999999996 into 2.999999999). NaN and ±∞ (ACOS of |x|&gt;1, SQRT of
    /// a negative, LOG of ≤ 0 …) map to the EC-ARGUMENT-FUNCTION default result 0 — §15.3: "the implementor
    /// defines the result of the function reference" while EC checking is disabled (an infinity only arises from
    /// an argument-rule violation at a domain edge, e.g. LOG(0) → −∞ — the same EC condition as the NaN cases);
    /// FINITE values beyond the long range saturate (a genuine huge result, e.g. EXP10(30) — the receiver's own
    /// store then size-truncates).
    /// </summary>
    public static long FromDouble(double d, int scale)
    {
        if (double.IsNaN(d) || double.IsInfinity(d))
            // EC-ARGUMENT-FUNCTION raise point (§14.6.13.1.1 Table 13, fatal): the §15.3 default result 0 when
            // checking is off; the raise when the statement carries enabled checking (the ambient gate).
            return Exceptions.ExceptionState.ArgumentError("floating-point intrinsic argument out of domain (NaN/infinity result)");
        double scaled = d * Pow10.AsDouble(scale);
        if (scaled >= 9.2e18) return long.MaxValue;
        if (scaled <= -9.2e18) return long.MinValue;
        return (long)Math.Round(scaled, MidpointRounding.AwayFromZero);
    }

}
