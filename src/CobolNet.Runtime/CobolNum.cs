// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// COBOL numeric value semantics over native .NET <see cref="long"/>/<see cref="decimal"/>: PICTURE truncation,
/// DISPLAY imaging, and (as it grows) the eight ROUNDED modes and ON SIZE ERROR. There is no byte storage — the
/// native value IS the datum; this type applies the COBOL rules at each operation.
/// </summary>
/// <remarks>
/// Bring-up slice: integer truncation + unsigned/signed DISPLAY imaging. The full oracle-verified arithmetic
/// (ScaleAndRound / TryStore over an exact base-10 carrier) is ported from the legacy byte-free CobolNum as the
/// arithmetic verbs need it (task G3).
/// </remarks>
public static class CobolNum
{
    /// <summary>Truncate an integer to the low-order <paramref name="digits"/> decimal digits (COBOL high-order
    /// truncation on store into a <c>PIC 9(digits)</c> integer, ISO §14.6.4). The sign is preserved.</summary>
    public static long TruncateInt(long value, int digits)
    {
        long modulus = Pow10(digits);
        return value % modulus;
    }

    /// <summary>Truncate/scale a decimal to <paramref name="digits"/> integer-plus-fraction digits with
    /// <paramref name="scale"/> fractional places (truncation, not rounding — the default COBOL store).</summary>
    public static decimal TruncateDecimal(decimal value, int digits, int scale)
    {
        // Drop fractional digits beyond the scale, then truncate high-order integer digits to fit the picture.
        decimal scaled = Math.Truncate(value * Pow10(scale)) / Pow10(scale);
        decimal modulus = (decimal)Pow10(digits) / Pow10(scale);
        return scaled % modulus;
    }

    /// <summary>
    /// The DISPLAY image of an UNSIGNED integer stored as <c>PIC 9(digits)</c>: the magnitude, zero-padded on the
    /// left to <paramref name="digits"/> characters (ISO §14.9.13 with §8.4.2 numeric editing rules).
    /// </summary>
    public static string FormatUnsignedDisplay(long value, int digits)
    {
        string s = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
        if (s.Length > digits) s = s[^digits..];     // high-order truncation to the picture width
        return s.PadLeft(digits, '0');
    }

    /// <summary>10 raised to <paramref name="n"/> as a <see cref="long"/> (n in 0..18).</summary>
    private static long Pow10(int n)
    {
        long r = 1;
        for (int i = 0; i < n; i++) r *= 10;
        return r;
    }
}
