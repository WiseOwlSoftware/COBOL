// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The ONE home of the unscaled-decimal REDUCTIONS over a significand-and-exponent pair (kb/Work PB2657; the
/// <c>DecUnscaled</c> target of DESIGN-architecture-review §8.3's <c>Runtime.CobolDec</c> row), beside <see cref="Pow10"/>,
/// whose table both read. Before this, the divide-by-ten loop was spelled six times in <c>CobolDec</c> (two
/// <c>DigitCount</c> overloads, the SQRT digit count, the binary64 lift's trailing-zero strip, <c>TryIntegerValue</c> and
/// <c>IsUnitMagnitude</c>), once in <c>CobolIntrinsics.NvfScan</c> and once in <c>CobolIntrinsics.TryExactInteger</c>.
/// ⛔ Do not write another: a digit count or a trailing-zero strip over an <see cref="Int128"/> significand reads this.
/// </summary>
internal static class DecUnscaled
{
    /// <summary>The count of decimal digits of the magnitude <paramref name="mag"/>; 0 counts as one digit. Read off the
    /// <see cref="Pow10"/> table by comparison, never by repeated division: the smallest n with mag &lt; 10^n, and 39 for a
    /// magnitude of 10^38 or more (UInt128 holds up to 39 digits; the table stops at 10^38).</summary>
    public static int DigitCount(UInt128 mag)
    {
        int n = 1;
        while (n <= 38 && mag >= (UInt128)Pow10.AsWide(n)) n++;
        return n;
    }

    /// <summary>The pair (<paramref name="sig"/>, <paramref name="exp"/>) with its trailing zero digits moved from the
    /// significand into the exponent, while the exponent stays below <paramref name="ceiling"/> — the same value held at the
    /// fewest places that carry it. <paramref name="ceiling"/> 0 normalizes only the fraction places (an integer value then
    /// has exponent 0 or more: 0.00 is 0 × 10^0); <see cref="int.MaxValue"/> strips every trailing zero of a NONZERO
    /// significand. A zero has no last nonzero digit to stop at, so its exponent rises straight to the ceiling (or stays,
    /// when already there), in one step rather than one division per place.</summary>
    public static (Int128 Sig, int Exp) StripTrailingZeros(Int128 sig, int exp, int ceiling)
    {
        if (sig == 0) return (sig, Math.Max(exp, ceiling));
        while (exp < ceiling && sig % 10 == 0) { sig /= 10; exp++; }
        return (sig, exp);
    }
}
