// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// The value-level numeric engine for COBOL.NET, operating entirely on hardware-native <see cref="long"/>.
/// </summary>
/// <remarks>
/// <para>A COBOL fixed-point datum is represented as a native integer holding its <b>unscaled</b> value (every
/// digit, with the decimal point implied by a compile-time scale) — exactly the COBOL definition of fixed-point.
/// So all arithmetic is native integer math; nothing uses the software <c>decimal</c> or <c>BigInteger</c> types.
/// (Pictures wider than 18 digits — COBOL-2002+ allows 31–38 — use a fixed-size <c>Int128</c> value-type escape
/// hatch (far cheaper than <c>BigInteger</c>), added when a program needs it; <c>COMP-1</c>/<c>COMP-2</c> are
/// <c>float</c>/<c>double</c> and bypass this engine.)</para>
/// <para>COBOL arithmetic operates on the algebraic VALUE of operands regardless of representation (ISO §8.8.1):
/// the compiler aligns operand scales, does the native integer op, and hands the result here with the receiver's
/// <see cref="NumProfile"/>; this rescales to the receiver's scale (rounding by one of the eight modes), truncates
/// any high-order digits beyond the picture, and applies the unsigned-magnitude rule. Representation (DISPLAY /
/// COMP / COMP-3 / COMP-5) changes only the capacity discipline and the external byte image — not the value.</para>
/// </remarks>
public static class CobolNum
{
    /// <summary>
    /// Rescale an unscaled integer from <paramref name="fromScale"/> to <paramref name="toScale"/> fractional
    /// digits: widening multiplies by a power of ten (exact); narrowing divides, rounding with <paramref name="mode"/>.
    /// </summary>
    public static long Rescale(long value, int fromScale, int toScale, CobolRounding mode)
    {
        if (toScale == fromScale) return value;
        if (toScale > fromScale) return value * Pow10(toScale - fromScale);
        return RoundDiv(value, Pow10(fromScale - toScale), mode);
    }

    /// <summary>
    /// Store an arithmetic result (the unscaled integer <paramref name="value"/> at <paramref name="valueScale"/>)
    /// into the receiver: rescale to the receiver's scale (rounding with <paramref name="mode"/>), drop any
    /// high-order digits beyond the picture (the no-ON-SIZE-ERROR behavior), and apply the unsigned-magnitude rule
    /// for an unsigned receiver (ISO §14.9.25 GR8). Returns the receiver's stored unscaled integer.
    /// </summary>
    public static long Store(long value, int valueScale, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation)
    {
        long v = Rescale(value, valueScale, receiver.FractionScale, mode);
        if (receiver.Truncation != NumericTruncation.BinaryCapacity)
            v %= Pow10(receiver.Digits);   // high-order digit truncation (COMP-5 wraps by width — later slice)
        return receiver.Signed ? v : Math.Abs(v);
    }

    /// <summary>
    /// The COBOL DISPLAY image of a fixed-point value: its unscaled digits, zero-padded on the left to the
    /// picture's digit count, with no decimal point (the point is implied). A signed item carries its sign per the
    /// receiver's <see cref="NumProfile.SignKind"/> (over-punch, separate sign, or a binary leading minus); an
    /// unsigned item is the bare magnitude.
    /// </summary>
    public static string FormatDisplay(long unscaled, in NumProfile receiver) =>
        receiver.Signed
            ? FormatDisplaySigned(unscaled, receiver)
            : FormatUnsignedDisplay(unscaled, receiver.Digits);

    // IBM-ASCII over-punch tables (ISO §8.5.1.2 / NIST-verified against the legacy): the units digit fused with the
    // operational sign. Positive 0–9 → "{ABCDEFGHI"; negative 0–9 → "}JKLMNOPQR".
    private const string PositiveOverpunch = "{ABCDEFGHI";
    private const string NegativeOverpunch = "}JKLMNOPQR";

    /// <summary>
    /// The DISPLAY image of a <b>signed</b> fixed-point value, applying the receiver's sign convention to the
    /// zero-padded magnitude digits (COBOLNET_DESIGN §6.4):
    /// <list type="bullet">
    ///   <item><see cref="NumericSign.TrailingOverpunch"/>/<see cref="NumericSign.LeadingOverpunch"/> — fuse the
    ///         sign onto the last / first digit via the over-punch tables;</item>
    ///   <item><see cref="NumericSign.LeadingSeparate"/>/<see cref="NumericSign.TrailingSeparate"/> — a leading /
    ///         trailing <c>+</c>/<c>-</c> character (always present);</item>
    ///   <item><see cref="NumericSign.BinaryMinus"/> — a leading <c>-</c> only when negative (positive/zero bare).</item>
    /// </list>
    /// </summary>
    public static string FormatDisplaySigned(long unscaled, in NumProfile receiver)
    {
        string mag = FormatUnsignedDisplay(unscaled, receiver.Digits);
        bool neg = unscaled < 0;
        return receiver.SignKind switch
        {
            NumericSign.BinaryMinus => neg ? "-" + mag : mag,
            NumericSign.LeadingSeparate => (neg ? "-" : "+") + mag,
            NumericSign.TrailingSeparate => mag + (neg ? "-" : "+"),
            NumericSign.LeadingOverpunch => Overpunch(mag, 0, neg),
            _ => Overpunch(mag, mag.Length - 1, neg),   // TrailingOverpunch (the default)
        };
    }

    /// <summary>Replace the digit at <paramref name="pos"/> of <paramref name="mag"/> with its signed over-punch.</summary>
    private static string Overpunch(string mag, int pos, bool negative)
    {
        if (pos < 0 || pos >= mag.Length) return mag;   // no digit positions (Digits == 0)
        int v = mag[pos] - '0';
        if ((uint)v > 9) return mag;
        char op = (negative ? NegativeOverpunch : PositiveOverpunch)[v];
        return mag[..pos] + op + mag[(pos + 1)..];
    }

    /// <summary>The DISPLAY image of an unsigned integer with <paramref name="digits"/> digit positions: the
    /// magnitude's low <paramref name="digits"/> digits, zero-padded.</summary>
    public static string FormatUnsignedDisplay(long value, int digits)
    {
        if (digits <= 0) return "";
        long v = value % Pow10(digits);
        string s = (v < 0 ? -v : v).ToString(CultureInfo.InvariantCulture);
        return s.PadLeft(digits, '0');
    }

    /// <summary>
    /// Divide two fixed-point operands and return the quotient as an unscaled integer at <paramref name="resultScale"/>
    /// fractional digits, rounding with <paramref name="mode"/>. Operands are given as unscaled integers with their
    /// own scales; the computation is exact native integer math (<c>a/10^aScale ÷ b/10^bScale</c> rendered at
    /// <paramref name="resultScale"/>). A zero divisor returns 0 (the caller raises ON SIZE ERROR — later slice).
    /// </summary>
    public static long Divide(long a, int aScale, long b, int bScale, int resultScale, CobolRounding mode)
    {
        if (b == 0) return 0;
        int exp = bScale + resultScale - aScale;     // quotient_unscaled = round(a × 10^exp / b)
        long num = a, den = b;
        if (exp >= 0) num *= Pow10(exp); else den *= Pow10(-exp);
        if (den < 0) { num = -num; den = -den; }     // RoundDiv requires a positive divisor
        return RoundDiv(num, den, mode);
    }

    /// <summary>
    /// Integer division of <paramref name="value"/> by <paramref name="divisor"/> rounding the (nonzero) remainder
    /// per a COBOL ROUNDED mode — the kernel for scale reduction. <paramref name="divisor"/> is a positive power of ten.
    /// </summary>
    private static long RoundDiv(long value, long divisor, CobolRounding mode)
    {
        long q = value / divisor, rem = value % divisor;
        if (rem == 0) return q;
        int sign = value < 0 ? -1 : 1;
        long twiceRem = Math.Abs(rem) * 2;
        return mode switch
        {
            CobolRounding.Truncation or CobolRounding.Prohibited => q,                 // toward zero
            CobolRounding.AwayFromZero => q + sign,
            CobolRounding.TowardGreater => value > 0 ? q + 1 : q,                       // ceiling
            CobolRounding.TowardLesser => value < 0 ? q - 1 : q,                        // floor
            CobolRounding.NearestAwayFromZero => twiceRem >= divisor ? q + sign : q,
            CobolRounding.NearestTowardZero => twiceRem > divisor ? q + sign : q,
            CobolRounding.NearestEven => twiceRem > divisor || (twiceRem == divisor && q % 2 != 0) ? q + sign : q,
            _ => q,
        };
    }

    /// <summary>10^n as a <see cref="long"/> (n in 0..18 — within long's range).</summary>
    private static long Pow10(int n)
    {
        long r = 1;
        for (int i = 0; i < n; i++) r *= 10;
        return r;
    }
}
