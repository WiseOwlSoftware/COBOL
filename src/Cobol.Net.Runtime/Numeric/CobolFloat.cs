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
    /// capacity + SIZE ERROR check. NaN → 0 (the store latches EC-SIZE under EC checking); ±Infinity and any
    /// magnitude beyond the wide engine SATURATE to <see cref="Int128.MaxValue"/>/<see cref="Int128.MinValue"/> so
    /// that capacity check fires SIZE ERROR reliably — never a silent-wrong store.</summary>
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
            CobolRounding.NearestTowardZero => Math.Round(scaled, MidpointRounding.ToZero),
            CobolRounding.TowardGreater     => Math.Ceiling(scaled),
            CobolRounding.TowardLesser      => Math.Floor(scaled),
            // PROHIBITED with a float source: land truncated; the store's prohibited-inexact check operates on the
            // (identity) rescale, so a float fractional drop under PROHIBITED is a documented Phase-6 residue.
            CobolRounding.Prohibited        => Math.Truncate(scaled),
            _                               => Math.Truncate(scaled),
        };
        return (Int128)r;
    }

    /// <summary>10^<paramref name="n"/> as a double (n ≥ 0; a float item has no negative scale).</summary>
    private static double Pow10(int n)
    {
        double r = 1;
        for (int i = 0; i < n; i++) r *= 10;
        return r;
    }
}
