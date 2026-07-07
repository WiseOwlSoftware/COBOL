// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

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
        double scaled = v * Pow10(scale);
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
        double s = v * Pow10(scale);
        return s != Math.Truncate(s);
    }

    /// <summary>10^<paramref name="n"/> as a double (n ≥ 0; a float item has no negative scale).</summary>
    private static double Pow10(int n)
    {
        double r = 1;
        for (int i = 0; i < n; i++) r *= 10;
        return r;
    }
}
