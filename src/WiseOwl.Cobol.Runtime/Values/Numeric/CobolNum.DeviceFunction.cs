// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Runtime;

public static partial class CobolNum
{
    /// <summary>
    /// ⛔ THE DEVICE FORM OF A NUMERIC FUNCTION'S RETURNED VALUE WHEN THE VALUE ALONE DESCRIBES ITS TEMPORARY (kb/Work PB3514;
    /// ISO/IEC 1989:2023 §15.4 - "The evaluation of a function produces a returned value in a temporary elementary data item";
    /// §15.4.1 - "When native arithmetic is in effect, the characteristics and representation of the returned value are defined
    /// by the implementor"; §14.9.11.4 GR1; docs/CONFORMANCE.md DOC-A.1-92). The standard leaves the temporary's description to
    /// the implementor, so CLAUDE.md rule 1's precedence gives it to GnuCOBOL 3.2.0, whose <c>cob_alloc_field</c> derives it
    /// from the value (<see cref="ReturnedValueProfile"/>), and DISPLAY then shows the temporary as it shows any numeric item of
    /// that description (<see cref="Compose"/>): <c>FUNCTION INTEGER(-12.5)</c> is <c>-000000013</c>, <c>FUNCTION NUMVAL("12.50")</c>
    /// is <c>00000012.5</c>, <c>FUNCTION NUMVAL("0.000000001")</c> is <c>.000000001</c>.
    /// </summary>
    /// <param name="value">The exact returned value (an SDIDI, which carries any scale the value has).</param>
    /// <param name="commaMode">The unit's decimal separator (§12.3.7.4 GR14 a).</param>
    public static string DeviceFunctionText(CobolDec value, bool commaMode)
    {
        NumProfile temporary = ReturnedValueProfile(value, out string digits);
        return Compose(value.Sig < 0, digits, temporary, commaMode);
    }

    /// <summary>The same text for a returned value carried as a scaled integer (<c>unscaled</c> at <paramref name="scale"/> fraction
    /// digits, the native exact lanes') - the value lifted to an SDIDI with no rounding, then <see cref="DeviceFunctionText(CobolDec, bool)"/>.</summary>
    public static string DeviceFunctionText(Int128 unscaled, int scale, bool commaMode) =>
        DeviceFunctionText(CobolDec.From(unscaled, scale), commaMode);

    /// <summary>
    /// ⛔ THE DESCRIPTION OF THE TEMPORARY A VALUE DESCRIBES, GnuCOBOL 3.2.0's <c>cob_alloc_field</c>:
    /// <list type="number">
    ///   <item>trailing zeros of the fraction are dropped first (<c>12.50</c> is <c>12.5</c>, <c>0.00</c> is <c>0</c>);</item>
    ///   <item>a value whose magnitude fits 31 bits (32 for a non-negative one) with at most 9 fraction digits is a 4-byte
    ///         binary item of <b>9</b> digit positions; one that fits 63 bits (64) with at most 18 fraction digits is an 8-byte
    ///         binary item of <b>20</b>; any other is a DISPLAY item of exactly its digits (at least its fraction digits);</item>
    ///   <item>the item is SIGNED exactly when the value is negative, so a positive value shows no sign and a negative one a
    ///         leading <c>-</c>, and a copy of the item that holds the magnitude (ABS) keeps the sign position.</item>
    /// </list>
    /// <para>Two things GnuCOBOL does are NOT copied, because each shows a figure the value does not have: a 10-digit value
    /// of a 4-byte item (<c>2147483647</c>) loses its high-order digit there (<c>147483647</c>), and a DISPLAY item's digit count
    /// there is a GMP size ESTIMATE that can exceed the true count by one (a leading zero). The digit count here is
    /// <c>max(positions, the value's digits)</c>, so no digit is ever cut and none is invented.</para>
    /// </summary>
    /// <param name="value">The exact returned value.</param>
    /// <param name="digits">The magnitude's digits after the trailing fraction zeros are dropped (unpadded).</param>
    internal static NumProfile ReturnedValueProfile(CobolDec value, out string digits)
    {
        bool negative = value.Sig < 0;
        digits = CobolDec.UAbs(value.Sig).ToString(CultureInfo.InvariantCulture);
        int scale = 0;
        if (value.Sig == 0) digits = "0";
        else if (value.Exp >= 0) digits += new string('0', value.Exp);   // an integer value written with a positive exponent
        else
        {
            scale = -value.Exp;
            int end = digits.Length;
            while (scale > 0 && end > 1 && digits[end - 1] == '0') { end--; scale--; }   // cob_trim_decimal
            digits = digits[..end];
        }

        // mpz_sizeinbase (value, 2): the bit length of the magnitude, 1 for zero. A magnitude of more than 38 digits is past 64 bits.
        int bits = digits.Length > 38 ? int.MaxValue : Math.Max(1, 128 - (int)UInt128.LeadingZeroCount(UInt128.Parse(digits, CultureInfo.InvariantCulture)));
        int sign = negative ? 1 : 0;
        int positions = bits < 33 - sign && scale < 10 ? 9
                      : bits < 65 - sign && scale < 19 ? 20
                      : 0;
        return new NumProfile
        {
            Digits = Math.Max(positions, Math.Max(digits.Length, scale)),
            FractionDigits = scale,
            Signed = negative,
            SignKind = NumericSign.LeadingSeparate,
            Truncation = NumericTruncation.DigitCount,
            ByteForm = NumericByteForm.Zoned,
        };
    }
}
