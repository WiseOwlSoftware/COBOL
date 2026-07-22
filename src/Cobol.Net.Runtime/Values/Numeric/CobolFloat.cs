// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The floating-point (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) value helpers (numeric design D16). A float
/// elementary item holds a native IEEE <see cref="float"/>/<see cref="double"/>; the arithmetic pipeline evaluates
/// any float-bearing expression in binary64. This class is the boundary between that native-double world and the
/// scaled-integer substrate (<see cref="CobolNum"/>): <see cref="ToScaled"/> lands a double into an unscaled
/// <see cref="Int128"/> at a receiver's scale (so the existing store funnel applies ROUNDED + SIZE ERROR), and
/// <see cref="Display(double)"/> renders the algebraic value for DISPLAY (ISO §14.9.11 GR1 — implementor-defined).
/// </summary>
public static class CobolFloat
{
    /// <summary>DISPLAY of a float item (§14.9.11 GR1, implementor-defined): the .NET shortest round-trippable
    /// decimal in the invariant culture (a leading '-' only when negative; E-notation for extreme magnitudes, as
    /// .NET produces). The ONE float→string path — never a bare <c>.ToString()</c>.</summary>
    public static string Display(float v) => v.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc cref="Display(float)"/>
    public static string Display(double v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>The checked read of a standard-float SENDING operand (ISO §14.6.13.2 item 3): return the value, but
    /// when EC-DATA-NOT-FINITE checking is enabled and the content is NaN or ±Infinity, raise the fatal
    /// EC-DATA-NOT-FINITE (via <see cref="ExceptionState.FloatNotFiniteError"/>). Always-emitted at the two float
    /// sending-read chokepoints (the numeric-value read and the string-image read), mirroring the always-emitted
    /// <c>CobolString.RefMod</c>: the fast path is a single <c>IsFinite</c> test then an immediate return
    /// (JIT-inlinable), so a directive-free run pays only that test and is byte-behaviour-identical. The four
    /// exemptions (class condition, sign condition, same-usage MOVE, VALIDATE) are realized as a RAW read at those
    /// sites, so this wrap never appears there.</summary>
    public static double Sending(double v)
    {
        if (!double.IsFinite(v))
            ExceptionState.FloatNotFiniteError("a NaN or infinite standard-float sending operand was referenced (ISO §14.6.13.2 item 3)");
        return v;
    }

    /// <inheritdoc cref="Sending(double)"/>
    public static float Sending(float v)
    {
        if (!float.IsFinite(v))
            ExceptionState.FloatNotFiniteError("a NaN or infinite standard-float sending operand was referenced (ISO §14.6.13.2 item 3)");
        return v;
    }

    /// <summary>The checked store of a MOVE algebraic value into a SINGLE-precision standard-float receiver (ISO
    /// §14.9.25.4 GR4 step 4a): cast to <see cref="float"/> and, when a FINITE source overflows the single-precision
    /// exponent range to ±Infinity and EC-DATA-OVERFLOW checking is enabled, raise the fatal EC-DATA-OVERFLOW (via
    /// <see cref="ExceptionState.FloatOverflowError"/>). The <c>double.IsFinite(src)</c> guard keeps a NaN/±Infinity
    /// source out of overflow (that is EC-DATA-NOT-FINITE at the sending read, or the valid §14.6.8.3 GR1 result under
    /// checking OFF). The test is cast-based — never <c>Math.Abs(src) &gt; float.MaxValue</c>, since a double in
    /// (float.MaxValue, ~3.4028235678e38] rounds to a FINITE <c>float.MaxValue</c>, not ±Infinity. A double receiver
    /// cannot overflow from a finite double, so it keeps a bare cast (no checked store).</summary>
    public static float StoreSingleChecked(double src)
    {
        float r = (float)src;
        if (double.IsFinite(src) && float.IsInfinity(r))
            ExceptionState.FloatOverflowError("a MOVE algebraic value overflows the single-precision float receiver (ISO §14.9.25.4 GR4 step 4a)");
        return r;
    }

    /// <summary>Convert a native double to an UNSCALED <see cref="Int128"/> at <paramref name="scale"/> fraction
    /// digits, rounded per <paramref name="mode"/> — the double→scaled-integer landing for a store INTO a fixed-point
    /// receiver (D16). The result then flows through the existing <c>CobolNum.Store</c>/<c>TryStore</c> funnel (whose
    /// rescale is identity, since we land AT the receiver scale — no double-rounding), which applies the digit
    /// capacity + SIZE ERROR check. NaN → 0 (implementor-defined; the resulting 0 is in range and exact, so the
    /// store commits it silently — NO SIZE ERROR / EC-SIZE is raised for a NaN source). ±Infinity and any magnitude
    /// beyond the wide engine SATURATE to <see cref="Int128.MaxValue"/>/<see cref="Int128.MinValue"/> so that
    /// capacity check fires SIZE ERROR reliably — never a silent-wrong store.</summary>
    public static Int128 ToScaled(double v, int scale, CobolRounding mode)
    {
        if (double.IsNaN(v)) return Int128.Zero;
        double scaled = v * Pow10.AsDouble(scale);
        // Int128.MaxValue ≈ 1.7014e38 — saturate at/above it (and ±Inf) before the (Int128) cast, whose behavior
        // is otherwise undefined for an out-of-range double.
        if (scaled >= 1.7014118e38) return Int128.MaxValue;
        if (scaled <= -1.7014118e38) return Int128.MinValue;
        double r = mode switch
        {
            CobolRounding.Truncation        => Math.Truncate(scaled),
            CobolRounding.NearestAwayFromZero => Math.Round(scaled, MidpointRounding.AwayFromZero),
            CobolRounding.AwayFromZero      => scaled < 0 ? Math.Floor(scaled) : Math.Ceiling(scaled),
            CobolRounding.NearestEven       => Math.Round(scaled, MidpointRounding.ToEven),
            // NEAREST-TOWARD-ZERO (§14.9.4 GR6): round to the NEAREST value; break an EXACT tie toward zero. NOT
            // MidpointRounding.ToZero — that is DIRECTED rounding (plain truncation of every value: 2.7→2 wrong).
            CobolRounding.NearestTowardZero => NearestTowardZero(scaled),
            CobolRounding.TowardGreater     => Math.Ceiling(scaled),
            CobolRounding.TowardLesser      => Math.Floor(scaled),
            // PROHIBITED: the value is landed truncated here; the emitter gates the STORE with InexactAtScale so an
            // inexact float→fixed transfer raises SIZE ERROR + leaves the receiver unchanged (§14.7.5 r7) before
            // this lands — see CobolFloat.InexactAtScale + the size-error branch in CSharpEmitter.StoreArith.
            CobolRounding.Prohibited        => Math.Truncate(scaled),
            _                               => Math.Truncate(scaled),
        };
        return (Int128)r;
    }

    /// <summary>Round <paramref name="x"/> to the NEAREST integer, breaking an EXACT half tie TOWARD ZERO (COBOL
    /// ROUNDED MODE NEAREST-TOWARD-ZERO, §14.9.4 GR6). A fraction &gt; ½ rounds away from zero; &lt; ½ or an exact ½
    /// truncates toward zero. (.NET's <c>MidpointRounding.ToZero</c> is DIRECTED rounding — it truncates every value,
    /// not just ties — so it is wrong for this mode.)</summary>
    private static double NearestTowardZero(double x)
    {
        double t = Math.Truncate(x);
        return Math.Abs(x - t) > 0.5 ? t + Math.Sign(x) : t;
    }

    /// <summary>True when <paramref name="v"/> carries a nonzero fraction beyond <paramref name="scale"/> digits — an
    /// INEXACT float→fixed transfer that ROUNDED MODE PROHIBITED must reject with SIZE ERROR / EC-SIZE-TRUNCATION,
    /// leaving the receiver unchanged (ISO §14.7.5 r7). Judged at the double level (a double's fraction can extend
    /// past any fixed decimal guard count). NaN/±Inf are handled by the store's saturation path, not here.</summary>
    public static bool InexactAtScale(double v, int scale)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return false;
        double s = v * Pow10.AsDouble(scale);
        return s != Math.Truncate(s);
    }

}
