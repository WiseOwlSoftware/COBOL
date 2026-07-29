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
public static partial class CobolNum
{
    /// <summary>
    /// Rescale an unscaled integer from <paramref name="fromScale"/> to <paramref name="toScale"/> fractional
    /// digits: widening multiplies by a power of ten (exact); narrowing divides, rounding with <paramref name="mode"/>.
    /// </summary>
    public static Int128 Rescale(Int128 value, int fromScale, int toScale, CobolRounding mode)
    {
        if (toScale == fromScale) return value;
        if (toScale > fromScale) return value * Pow10Wide(toScale - fromScale);
        return RoundDiv(value, Pow10Wide(fromScale - toScale), mode);
    }

    /// <summary>The size-error-CHECKED sibling of <see cref="Rescale"/> for a numeric-EDITED receiver's final
    /// transfer (ISO §14.7.4.3 r7): under <see cref="CobolRounding.Prohibited"/> an inexact narrowing throws
    /// <see cref="CobolSizeError"/> — the caller's ON SIZE ERROR / EC-SIZE machinery catches it and leaves the
    /// receiver UNCHANGED. Mirror of the numeric path's <see cref="TryStore"/> Prohibited check and the
    /// standard-decimal <c>CobolDec.ToUnscaled</c> throw, so all three receiver categories agree. Every other
    /// mode rescales normally (a rounding mode is never a size error by rescale).</summary>
    public static Int128 RescaleChecked(Int128 value, int fromScale, int toScale, CobolRounding mode)
    {
        if (mode == CobolRounding.Prohibited && IsInexactAtScale(value, fromScale, toScale))
            throw new CobolSizeError("ROUNDED MODE IS PROHIBITED on an inexact transfer to an edited receiver "
                + "(ISO §14.7.4.3 r7 — EC-SIZE-TRUNCATION; the receiver is left unchanged)");
        return Rescale(value, fromScale, toScale, mode);
    }

    /// <summary>
    /// Store an arithmetic result (the unscaled integer <paramref name="value"/> at <paramref name="valueScale"/>)
    /// into the receiver: rescale to the receiver's scale (rounding with <paramref name="mode"/>), drop any
    /// high-order digits beyond the picture (the no-ON-SIZE-ERROR behavior), and apply the unsigned-magnitude rule
    /// for an unsigned receiver (ISO §14.9.25 GR8). Returns the receiver's stored unscaled integer.
    /// </summary>
    public static Int128 Store(Int128 value, int valueScale, in NumProfile receiver,
        CobolRounding mode = CobolRounding.Truncation)
    {
        Int128 v = Rescale(value, valueScale, receiver.FractionScale, mode);
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
            return WrapBinary(v, receiver);   // native two's-complement width (COMP-5 / BINARY-CHAR family)
        v %= Pow10Wide(receiver.Digits);      // high-order digit truncation (DISPLAY / COMP / BINARY)
        return receiver.Signed ? v : Int128.Abs(v);
    }

    /// <summary>
    /// Store an arithmetic result with SIZE ERROR checking (ISO §14.7.5 phase-b): rescale to the receiver scale
    /// (rounding with <paramref name="mode"/>) and bounds-check against the picture before committing. Returns
    /// <c>false</c> — leaving <paramref name="stored"/> meaningless and the receiver to be left unchanged by the
    /// caller — when (a) the rescaled value's magnitude exceeds the receiver's digit capacity (high-order overflow,
    /// §14.7.5 case 3), or (b) <paramref name="mode"/> is <see cref="CobolRounding.Prohibited"/> and the rescale is
    /// inexact (a nonzero fraction would be dropped, §14.7.4.3 r7). Otherwise stores the value (unsigned-magnitude
    /// rule applied, §14.9.25 GR8) and returns <c>true</c>. This is the checked sibling of <see cref="Store"/>, used
    /// only when an ON SIZE ERROR phrase is present.
    /// </summary>
    public static bool TryStore(Int128 value, int valueScale, in NumProfile receiver, CobolRounding mode, out Int128 stored)
    {
        stored = 0;
        // PROHIBITED: an inexact rescale is a size error regardless of capacity (§14.7.4.3 r7) — receiver unchanged.
        if (mode == CobolRounding.Prohibited && IsInexactAtScale(value, valueScale, receiver.FractionScale))
            return false;

        Int128 v = Rescale(value, valueScale, receiver.FractionScale, mode);

        // BinaryCapacity (COMP-5 / BINARY-CHAR family): the SIZE ERROR boundary is the native two's-complement
        // range of the byte width (ISO §13.18.60.4 GR12) — signed [-2^(bits-1), 2^(bits-1)), unsigned magnitude
        // [0, 2^bits) (§14.9.25 GR8 for the unsigned-magnitude rule).
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
        {
            if (!InBinaryRange(v, receiver)) return false;
            stored = receiver.Signed ? v : Int128.Abs(v);
            return true;
        }

        // High-order capacity check by digit count (DISPLAY/COMP/BINARY). The compare avoids
        // Math.Abs(long.MinValue) by bounding both signs against the positive limit.
        Int128 limit = Pow10Wide(receiver.Digits);   // Digits ≤ 38 within the wide engine
        if (v >= limit || v <= -limit) return false;

        // Unsigned receiver stores the magnitude (§14.9.25 GR8); v is bounded (it passed the capacity check),
        // so Math.Abs is safe.
        stored = receiver.Signed ? v : Int128.Abs(v);
        return true;
    }

    /// <summary>Store a STANDARD-DECIMAL intermediate (§8.8.1.5) into the receiver: the §14.7 final transfer —
    /// the SDIDI rescales to the receiver's scale with the statement's ROUNDED <paramref name="mode"/>, then the
    /// normal capacity rules apply.</summary>
    public static Int128 Store(CobolDec value, in NumProfile receiver, CobolRounding mode = CobolRounding.Truncation)
    {
        Int128 v = value.ToUnscaled(receiver.FractionScale, mode);
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
            return WrapBinary(v, receiver);
        v %= Pow10Wide(receiver.Digits);
        return receiver.Signed ? v : Int128.Abs(v);
    }

    /// <summary>The SIZE-ERROR-checked sibling of the SDIDI store: false on capacity overflow or a
    /// PROHIBITED-inexact transfer (receiver to be left unchanged by the caller).</summary>
    public static bool TryStore(CobolDec value, in NumProfile receiver, CobolRounding mode, out Int128 stored)
    {
        stored = 0;
        Int128 v;
        try { v = value.ToUnscaled(receiver.FractionScale, mode); }
        catch (CobolSizeError) { return false; }   // PROHIBITED-inexact transfer (§14.7.4.3 r7)
        if (receiver.Truncation == NumericTruncation.BinaryCapacity)
        {
            if (!InBinaryRange(v, receiver)) return false;
            stored = receiver.Signed ? v : Int128.Abs(v);
            return true;
        }
        Int128 limit = Pow10Wide(receiver.Digits);
        if (v >= limit || v <= -limit) return false;
        stored = receiver.Signed ? v : Int128.Abs(v);
        return true;
    }

    /// <summary>Reduce an unscaled value to the native two's-complement range of a BinaryCapacity receiver's
    /// storage width — the deterministic no-ON-SIZE-ERROR truncation for COMP-5 / the BINARY-CHAR family (the
    /// width analog of the DigitCount path's <c>%= 10^Digits</c>). A signed receiver folds by modulo 2^bits into
    /// [-2^(bits-1), 2^(bits-1)) (exactly a native sbyte/short/int/long cast); an unsigned receiver stores the
    /// magnitude (ISO §14.9.25 GR8) reduced modulo 2^bits into [0, 2^bits). ISO §13.18.60.4 GR12/GR21.</summary>
    private static Int128 WrapBinary(Int128 value, in NumProfile receiver)
    {
        int bits = 8 * receiver.StorageLength;
        Int128 modulus = (Int128)1 << bits;
        if (!receiver.Signed)
            return Int128.Abs(value) % modulus;
        Int128 m = ((value % modulus) + modulus) % modulus;   // non-negative residue in [0, 2^bits)
        return m >= (modulus >> 1) ? m - modulus : m;          // fold the high half to the negative range
    }

    /// <summary>Whether a value fits the native two's-complement range of a BinaryCapacity receiver's storage
    /// width — the SIZE ERROR test for COMP-5 / the BINARY-CHAR family: signed [-2^(bits-1), 2^(bits-1));
    /// unsigned magnitude [0, 2^bits) (ISO §13.18.60.4 GR12; §14.9.25 GR8 for the unsigned-magnitude rule).</summary>
    private static bool InBinaryRange(Int128 value, in NumProfile receiver)
    {
        int bits = 8 * receiver.StorageLength;
        Int128 modulus = (Int128)1 << bits;
        if (!receiver.Signed)
            return Int128.Abs(value) < modulus;
        Int128 half = modulus >> 1;
        return value >= -half && value < half;
    }

    /// <summary>True when rescaling <paramref name="value"/> from <paramref name="fromScale"/> to a smaller
    /// <paramref name="toScale"/> would drop a nonzero fraction (an inexact transfer).</summary>
    private static bool IsInexactAtScale(Int128 value, int fromScale, int toScale) =>
        toScale < fromScale && value % Pow10Wide(fromScale - toScale) != 0;

    /// <summary>Multiply two unscaled operands with overflow checking against the long engine's range: raises
    /// <see cref="OverflowException"/> (mapped to the size error condition, ISO §14.7.5 case 5) when the product
    /// exceeds <see cref="long"/>. Emitted only inside a statement that carries an ON SIZE ERROR phrase — so a
    /// no-phrase multiply stays a bare unchecked <c>*</c>. Being a method call (not a constant expression), a
    /// constant product is checked at run time, never folded to a compile-time error. (Products beyond the long
    /// range need the Int128 carrier; deferred — see the numeric design.)</summary>
    public static Int128 MulChecked(Int128 a, Int128 b) => checked(a * b);

    /// <summary>
    /// Divide (see <see cref="Divide"/>) but raise <see cref="CobolSizeError"/> on a zero divisor (ISO §14.7.5 case
    /// 2) instead of returning 0. Emitted only inside a statement that carries an ON SIZE ERROR phrase, so a
    /// division in a statement WITHOUT the phrase keeps <see cref="Divide"/>'s behavior unchanged.
    /// </summary>
    public static Int128 DivideOrThrow(Int128 a, int aScale, Int128 b, int bScale, int resultScale, CobolRounding mode)
    {
        if (b == 0) throw new CobolSizeError("divide by zero", "EC-SIZE-ZERO-DIVIDE");
        // ROUNDED MODE IS PROHIBITED: an inexact quotient AT resultScale is a size error (§14.7.4.3 r7). When the
        // division rounds directly at the receiver scale (the outermost-division case), the inexactness is consumed
        // inside Divide, so it must be detected here from the exact remainder rather than by the receiver's TryStore.
        if (mode == CobolRounding.Prohibited && DivisionLosesPrecision(a, aScale, b, bScale, resultScale))
            throw new CobolSizeError("PROHIBITED rounding on an inexact quotient");
        return Divide(a, aScale, b, bScale, resultScale, mode);
    }

    /// <summary>True when <c>a/10^aScale ÷ b/10^bScale</c> cannot be represented exactly at <paramref name="resultScale"/>
    /// fractional digits (a nonzero remainder after radix alignment) — used to detect a PROHIBITED violation in a
    /// division. Mirrors <see cref="Divide"/>'s scaling. Assumes <paramref name="b"/> != 0.</summary>
    private static bool DivisionLosesPrecision(Int128 a, int aScale, Int128 b, int bScale, int resultScale)
    {
        int exp = bScale + resultScale - aScale;
        Int128 num = a, den = b;   // wide radix alignment — mirrors Divide exactly
        if (exp >= 0) num *= Pow10Wide(exp); else den *= Pow10Wide(-exp);
        return num % den != 0;
    }

    /// <summary>
    /// The COBOL DISPLAY image of a fixed-point value: its unscaled digits, zero-padded on the left to the
    /// picture's digit count, with no decimal point (the point is implied). A signed item carries its sign per the
    /// receiver's <see cref="NumProfile.SignKind"/> (over-punch, separate sign, or a binary leading minus); an
    /// unsigned item is the bare magnitude.
    /// </summary>
    public static string FormatDisplay(Int128 unscaled, in NumProfile receiver) =>
        receiver.Signed
            ? FormatDisplaySigned(unscaled, receiver)
            : FormatUnsignedDisplay(unscaled, receiver.Digits);

    /// <summary>Storage-form bridge (the <c>CobolTable.Occ</c> pattern): a numeric-DISPLAY field whose backing the
    /// post-bind whole-group analysis turned into its character IMAGE is already display-formatted — pass through.
    /// Lets the compiler emit ONE expression for an image view whose field's storage form is decided later.</summary>
    public static string FormatDisplay(string image, in NumProfile receiver) => image;

    /// <summary>Store a (possibly spliced) DISPLAY image back into a numeric field — the write half of an image
    /// view over a numeric item (reference modification / a RENAMES span leaf, ISO §8.4.2.4 / §13.18.45). The
    /// overload set is the storage-form bridge: the <paramref name="current"/> dummy selects the conversion for
    /// the field's ACTUAL storage (native <c>long</c>/<c>Int128</c> → sign-aware decode; an image-stored string
    /// field keeps the image).</summary>
    public static long StoreDisplay(string image, in NumProfile receiver, long current) =>
        (long)ParseDisplay(image, receiver);

    /// <inheritdoc cref="StoreDisplay(string, in NumProfile, long)"/>
    public static Int128 StoreDisplay(string image, in NumProfile receiver, Int128 current) =>
        ParseDisplay(image, receiver);

    /// <inheritdoc cref="StoreDisplay(string, in NumProfile, long)"/>
    public static string StoreDisplay(string image, in NumProfile receiver, string current) => image;

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
    public static string FormatDisplaySigned(Int128 unscaled, in NumProfile receiver)
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
    public static string FormatUnsignedDisplay(Int128 value, int digits)
    {
        if (digits <= 0) return "";
        Int128 v = value % Pow10Wide(digits);
        string s = (v < 0 ? -v : v).ToString(CultureInfo.InvariantCulture);
        return s.PadLeft(digits, '0');
    }

    /// <summary>
    /// ⛔ THE TEXT IMAGE OF AN INTRINSIC FUNCTION'S RETURNED VALUE (DA2). A function's value lives in a
    /// "temporary elementary data item" (ISO §15.4) whose characteristics, under native arithmetic, are
    /// explicitly <b>defined by the implementor</b> (§15.4.1); §14.9.11.4 GR1 likewise makes any conversion
    /// between a DISPLAY operand and the device implementor-defined. COBOL.NET's determination — documented as
    /// an Annex A.1 item — is the <b>literal form of the value</b>: the significant digits with no leading-zero
    /// padding, a leading <c>-</c> when negative, and a decimal point followed by exactly
    /// <paramref name="scale"/> fraction digits when the value is scaled.
    /// <para>
    /// ⛔ WHY THIS RULE AND NOT A FIXED FIELD WIDTH: an intrinsic whose arguments are all literals is FOLDED at
    /// compile time to a numeric literal, and a numeric literal renders as its own text (<c>DISPLAY 42</c> →
    /// <c>42</c>). If a computed result rendered in a padded fixed width instead, then
    /// <c>DISPLAY FUNCTION LENGTH(X)</c> and <c>DISPLAY FUNCTION ORD(C)</c> would print in different formats for
    /// no reason a COBOL programmer can see — the optimization would have become OBSERVABLE. One rule, so the
    /// fold cannot be detected.
    /// </para>
    /// </summary>
    /// <param name="deSign">⛔ §14.9.25.4 GR6a — "If the sending operand is described as being signed numeric, the
    /// operational sign is not moved." True for every context that MOVES this value to an alphanumeric/national/
    /// edited receiver, compares it as text (§8.8.4.2.5 routes that through the MOVE rules), or INSPECTs it: the
    /// MAGNITUDE travels, never the sign. This is a GENERAL rule with no implementor latitude — the §15.4.1 /
    /// §14.9.11.4 GR1 latitude this determination rests on covers the FORM of the text, not whether the sign
    /// travels. False for DISPLAY, where the sign IS part of the value being shown.</param>
    public static string FormatFunctionText(Int128 unscaled, int scale, bool deSign = false)
    {
        if (deSign && unscaled < 0) unscaled = -unscaled;
        bool neg = unscaled < 0;
        string digits = (neg ? -unscaled : unscaled).ToString(CultureInfo.InvariantCulture);
        string body;
        if (scale <= 0)
        {
            // A NEGATIVE scale is a P-scaled value: the Ps are trailing zeros that occupy no storage
            // (§13.18.40.3 symbol-P operations item b), so they are restored here.
            body = scale < 0 ? digits + new string('0', -scale) : digits;
        }
        else
        {
            // Left-pad so a value smaller than one full unit still shows the leading "0." (1 unscaled at
            // scale 2 is 0.01, never .01).
            digits = digits.PadLeft(scale + 1, '0');
            body = digits[..^scale] + "." + digits[^scale..];
        }
        return neg ? "-" + body : body;
    }

    /// <summary>
    /// Decode a USAGE DISPLAY numeric item's character image back to its unscaled <see cref="long"/> value — the
    /// inverse of <see cref="FormatDisplay"/> (COBOLNET_DESIGN §6.4). The image is the zoned digit run at the item's
    /// scale (no decimal point — the point is implied); the operational sign is decoded per the receiver's
    /// <see cref="NumProfile.SignKind"/> (over-punch, separate <c>+</c>/<c>-</c>, or a leading minus). Non-digit
    /// characters (e.g. the spaces a whole-group MOVE legitimately deposits — ISO/IEC 1989:2023 §14.9 MOVE GR4 fills
    /// without consideration for subordinate items) contribute no digit; an all-non-digit image decodes to 0, since
    /// using incompatible data in a numeric context is undefined (§14.6.13.2 / the EC-DATA-INCOMPATIBLE condition),
    /// so a deterministic 0 is conformant.
    /// </summary>
    /// <summary>The unsigned integer value of an ALPHANUMERIC operand used in a numeric context (ISO §14.9.25.4
    /// GR6 — an alphanumeric sending item moving to a numeric receiver is treated as an UNSIGNED integer;
    /// §14.6.13.2 — incompatible content decodes deterministically: a non-digit position contributes no digit, an
    /// all-non-digit image is 0).</summary>
    public static Int128 FromAlphanumeric(string image)
    {
        if (string.IsNullOrEmpty(image)) return 0;
        Int128 mag = 0;
        foreach (char c in image)
            if (c is >= '0' and <= '9') mag = mag * 10 + (c - '0');
        return mag;
    }

    public static Int128 ParseDisplay(string image, in NumProfile receiver)
    {
        if (string.IsNullOrEmpty(image)) return 0;
        var chars = image.ToCharArray();   // a mutable copy so a sign carrier can be reduced to its digit
        bool negative = false;

        if (receiver.Signed)
        {
            switch (receiver.SignKind)
            {
                case NumericSign.LeadingSeparate:
                    negative = chars[0] == '-';
                    if (chars[0] is '-' or '+') chars[0] = ' ';
                    break;
                case NumericSign.TrailingSeparate:
                    negative = chars[^1] == '-';
                    if (chars[^1] is '-' or '+') chars[^1] = ' ';
                    break;
                case NumericSign.BinaryMinus:
                    negative = chars[0] == '-';
                    if (chars[0] == '-') chars[0] = ' ';
                    break;
                case NumericSign.LeadingOverpunch:
                    DecodeOverpunch(chars, 0, ref negative);
                    break;
                default:   // TrailingOverpunch (the no-SIGN-clause default)
                    DecodeOverpunch(chars, chars.Length - 1, ref negative);
                    break;
            }
        }

        Int128 mag = 0;
        foreach (char c in chars)
            if (c is >= '0' and <= '9')
                mag = mag * 10 + (c - '0');
        return negative ? -mag : mag;
    }

    /// <summary>Reduce the over-punch character at <paramref name="pos"/> (in place) to its underlying digit, setting
    /// <paramref name="negative"/> when the punch is in the negative table (IBM-ASCII <c>{A-I</c> positive,
    /// <c>}J-R</c> negative). A plain digit or non-digit at the position is left as-is (sign stays positive).</summary>
    private static void DecodeOverpunch(char[] chars, int pos, ref bool negative)
    {
        if (pos < 0 || pos >= chars.Length) return;
        int p = PositiveOverpunch.IndexOf(chars[pos]);
        if (p >= 0) { chars[pos] = (char)('0' + p); return; }
        int n = NegativeOverpunch.IndexOf(chars[pos]);
        if (n >= 0) { chars[pos] = (char)('0' + n); negative = true; }
    }

    /// <summary>
    /// Divide two fixed-point operands and return the quotient as an unscaled integer at <paramref name="resultScale"/>
    /// fractional digits, rounding with <paramref name="mode"/>. Operands are given as unscaled integers with their
    /// own scales (<c>a/10^aScale ÷ b/10^bScale</c> rendered at <paramref name="resultScale"/>). The radix alignment
    /// (<c>a × 10^exp</c>) runs in <see cref="Int128"/> — an 18-significant-digit dividend scaled by the receiver's
    /// fraction digits exceeds the long range MID-computation even though the QUOTIENT fits (ISO §8.8.1: arithmetic
    /// operates on the algebraic values; intermediate width is the implementor's problem, not the program's). A zero
    /// divisor returns 0 (the caller raises ON SIZE ERROR — later slice).
    /// </summary>
    public static Int128 Divide(Int128 a, int aScale, Int128 b, int bScale, int resultScale, CobolRounding mode)
    {
        if (b == 0) return 0;
        int exp = bScale + resultScale - aScale;     // quotient_unscaled = round(a × 10^exp / b)
        Int128 num = a, den = b;
        if (exp >= 0) num *= Pow10Wide(exp); else den *= Pow10Wide(-exp);
        if (den < 0) { num = -num; den = -den; }     // RoundDiv requires a positive divisor
        return RoundDiv(num, den, mode);
    }

    /// <summary>
    /// Integer division of <paramref name="value"/> by <paramref name="divisor"/> rounding the (nonzero) remainder
    /// per a COBOL ROUNDED mode — the kernel for scale reduction, in <see cref="Int128"/> so radix-aligned
    /// intermediates beyond the long range round exactly. <paramref name="divisor"/> is a positive power of ten.
    /// </summary>
    private static Int128 RoundDiv(Int128 value, Int128 divisor, CobolRounding mode)
    {
        Int128 q = value / divisor, rem = value % divisor;
        if (rem == 0) return q;
        int sign = value < 0 ? -1 : 1;
        Int128 twiceRem = Int128.Abs(rem) * 2;
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

    /// <summary>10^n as an <see cref="Int128"/> (n in 0..38 — the wide intermediate range, COBOLNET_DESIGN §18 #4)
    /// — a name-stable wrapper over the ONE <see cref="Pow10"/> table (DESIGN-runtime-library §2.3).
    /// Internal: <see cref="CobolEdit.TryFormat"/> uses it for the edited-receiver digit-capacity bound.</summary>
    internal static Int128 Pow10Wide(int n) => Pow10.AsWide(n);
}
