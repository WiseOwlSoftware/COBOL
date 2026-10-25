// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE DECIMAL DEVICE FORM OF A NUMERIC ITEM — what a DISPLAY statement shows of a numeric operand under
/// <c>decimal</c>, the default of the compile option <c>--numeric-display</c>
/// (kb/Work PB3505, owner decision kb/Work PB3467). ISO/IEC 1989:2023 §14.9.11.4 GR1 — "Any conversion of data
/// required between literal-1 or the data item referenced by identifier-1 and the device is defined by the
/// implementor" — leaves the form to the implementor, and docs/CONFORMANCE.md DOC-A.1-56 documents ours: the
/// digits at the value's positions with the decimal point where the scale puts it, and a signed item's sign as a
/// leading <c>+</c> or <c>-</c> character (GnuCOBOL 3.2.0's default form). The other value of the option,
/// <c>digits</c>, is the digit image <see cref="CobolNum.FormatDisplay(Int128, in NumProfile)"/> writes and
/// is not reached from here.
/// <para>ONE composition (<see cref="Compose"/>) serves every carrier — a native value (<c>Int128</c>, <c>UInt128</c>)
/// and a character image (an item stored as its image, a REDEFINES / reference-modification window) — so the
/// form cannot differ between two storages of one value (§13.18.44.4 GR1: one storage, two descriptions).</para>
/// </summary>
public static partial class CobolNum
{
    /// <summary>The decimal device form of a value carried natively as its unscaled integer
    /// (<c>long</c>, <c>ulong</c> and <c>Int128</c> carriers all bind here). A BINARY-CAPACITY item
    /// (COMP-5, BINARY-CHAR … BINARY-DOUBLE) holds any value of its container, past its PICTURE's digit count, so it
    /// shows the container's digits (<see cref="DeviceDigits"/>) and a value is never cut to the picture.</summary>
    /// <param name="commaMode">The unit's decimal separator: <c>,</c> under DECIMAL-POINT IS COMMA (§12.3.7.4 GR14 a),
    /// else <c>.</c>.</param>
    public static string DisplayDecimal(Int128 unscaled, in NumProfile receiver, bool commaMode) =>
        Compose(unscaled < 0, CobolDec.UAbs(unscaled).ToString(CultureInfo.InvariantCulture), receiver, commaMode);

    /// <summary>The unsigned-wide twin (a 16-byte unsigned COMP-5's UInt128 carrier), distinctly named for the reason
    /// <see cref="FormatDisplayU"/> is: an int constant converts to both <c>Int128</c> and <c>UInt128</c>.</summary>
    public static string DisplayDecimalU(UInt128 unscaled, in NumProfile receiver, bool commaMode) =>
        Compose(false, unscaled.ToString(CultureInfo.InvariantCulture), receiver, commaMode);

    /// <summary>The decimal device form of an item whose storage IS its character image — the sign-aware DISPLAY
    /// image <see cref="FormatDisplay(Int128, in NumProfile)"/> writes (an item the whole-group analysis stored as its image, a
    /// REDEFINES view or reference-modification window, a BINARY/PACKED window decoded and re-imaged). The digit
    /// positions are carried through AS STORED, so content a group MOVE deposited that is not a digit stays visible
    /// (§14.6.13.2: it is undefined, and a decode-and-reformat round trip would turn it into zeros); only the sign
    /// position is read, by the item's own <see cref="NumProfile.SignKind"/> and over-punch convention.</summary>
    public static string DisplayDecimal(string image, in NumProfile receiver, bool commaMode)
    {
        if (!receiver.Signed) return Compose(false, image, receiver, commaMode, padTo: false);
        bool negative = false;
        string digits = image;
        switch (receiver.SignKind)
        {
            case NumericSign.BinaryMinus:
                if (image.Length > 0 && image[0] == '-') { negative = true; digits = image[1..]; }
                break;
            case NumericSign.LeadingSeparate:
                if (image.Length > 0) { negative = image[0] == '-'; digits = image[1..]; }
                break;
            case NumericSign.TrailingSeparate:
                if (image.Length > 0) { negative = image[^1] == '-'; digits = image[..^1]; }
                break;
            case NumericSign.LeadingOverpunch:
                if (image.Length > 0 && ZonedSign.TryUnpunch(receiver.SignEncoding, image[0], out int lead, out negative))
                    digits = (char)('0' + lead) + image[1..];
                break;
            default:   // TrailingOverpunch, the USAGE DISPLAY default
                if (image.Length > 0 && ZonedSign.TryUnpunch(receiver.SignEncoding, image[^1], out int tail, out negative))
                    digits = image[..^1] + (char)('0' + tail);
                break;
        }
        return Compose(negative, digits, receiver, commaMode, padTo: false);
    }

    /// <summary>The digits an <see cref="Int128"/> unscaled value holds: the widest capacity the device conversion
    /// reads, whatever the item (a BINARY-CAPACITY item keeps every digit of its container, past its PICTURE's).</summary>
    private const int DeviceReadDigits = 38;

    /// <summary>⛔ THE ACCEPT-SIDE READER OF THE DECIMAL DEVICE FORM (kb/Work PB3513, the inverse of
    /// <see cref="DisplayDecimal(Int128, in NumProfile, bool)"/>; ISO/IEC 1989:2023 §14.9.1.4 GR1 — "Any conversion of data
    /// required between the device and the data item referenced by identifier-1 is defined by the implementor";
    /// docs/CONFORMANCE.md DOC-A.1-1). It converts the characters an ACCEPT of a fixed-point numeric item transferred
    /// (<paramref name="window"/>: the item's own size for a character-form item, the whole record for a binary or
    /// packed one, <c>AcceptSource.DeviceNumeric</c>) to the item's UNSCALED value, exactly as GnuCOBOL 3.2.0 does
    /// (measured 2026-10-23, the precedence tiebreaker of CLAUDE.md rule 1):
    /// <list type="bullet">
    ///   <item>leading spaces, then an optional sign (<c>+</c> or <c>-</c>; an unsigned item drops it), then the number:
    ///         digits, ONE decimal separator (the program's, a comma under DECIMAL-POINT IS COMMA, §12.3.7.4 GR14 a),
    ///         with spaces and the other separator character skipped wherever they stand;</item>
    ///   <item>a character that is none of those makes the WHOLE text invalid and the value zero: a trailing sign
    ///         (<c>12.5-</c>), a second separator, a letter. No exception is set (§14.6.13.2: undefined content);</item>
    ///   <item>the fraction keeps its first <see cref="NumProfile.FractionDigits"/> digits (truncation, never rounding);
    ///         integer digits past the item's capacity lose their HIGH-ORDER end, so the item keeps the low-order digits
    ///         (§14.6.8.2 rule 4, "zero fill or truncation on either end", the alignment a MOVE applies) — the unscaled value is
    ///         reduced to the PICTURE's digit count here, a BINARY-CAPACITY item's container excepted: it keeps every digit its
    ///         container holds and the value wraps to the container (<see cref="WrapBinary"/>, DOC-A.1-208), as GnuCOBOL's does.
    ///         The characters ahead of the last integer digit dropped are skipped unexamined, a non-digit among them
    ///         included (kb/Work PB3573: <c>Y123</c> into <c>PIC 9(2)V99</c> is 23.00, <c>1Y23</c> is invalid); an item
    ///         stored as characters drops what passes its own integer positions, a binary or packed one converts through a
    ///         <see cref="DeviceReadDigits"/>-digit intermediate, so a non-digit ahead of its surplus is skipped only past
    ///         that many integer digits;</item>
    ///   <item>an item whose characters ARE the window (<see cref="NumericByteForm.Zoned"/>, DISPLAY and NATIONAL) is read
    ///         only until its last digit position is filled, so whatever follows is never examined (<c>12.50CR</c> into
    ///         <c>PIC 9(5)V99</c> is 12.50); a binary or packed item reads the whole text, so the same <c>CR</c> is invalid.</item>
    /// </list>
    /// A P-scaled item is read by VALUE: <c>PIC 9(3)PP</c> given 1200 holds 12 (its digits are the hundreds), and a
    /// packed <c>PIC PP9(3)</c> given .00123 holds 123 (a DISPLAY one is cut by its three-character window first).</summary>
    /// <param name="window">The transferred characters.</param>
    /// <param name="receiver">The receiving item's profile.</param>
    /// <param name="commaMode">The unit's decimal separator: <c>,</c> under DECIMAL-POINT IS COMMA, else <c>.</c>.</param>
    public static Int128 ParseDeviceDecimal(string window, in NumProfile receiver, bool commaMode)
    {
        char point = commaMode ? ',' : '.';
        char group = commaMode ? '.' : ',';
        int i = 0, n = window.Length;
        while (i < n && window[i] == ' ') i++;
        bool negative = false;
        if (i < n && window[i] is '+' or '-') { negative = window[i] == '-'; i++; }

        bool binaryCapacity = receiver.Truncation == NumericTruncation.BinaryCapacity;
        bool characterForm = receiver.ByteForm == NumericByteForm.Zoned;
        int scale = receiver.FractionDigits;
        int digits = binaryCapacity ? DeviceReadDigits : Math.Min(receiver.Digits, DeviceReadDigits);
        int fractionCap = Math.Max(scale, 0);
        int integerCap = Math.Min(Math.Max(digits - scale, 0), DeviceReadDigits);

        // The integer digits the text carries: every digit before the first separator, junk or not. The FIRST-stage
        // conversion holds only so many of them (readCap): an item stored as characters converts into itself, so its
        // own integer positions; a binary or packed one converts through a DeviceReadDigits-wide intermediate, so a
        // surplus past its PICTURE is cut by the second stage (the value reduction below), not here. The surplus of the
        // first stage (count - readCap) is dropped from the FRONT of the run, never from its end, and the characters
        // that stand ahead of the last dropped digit are skipped unexamined, a non-digit among them included
        // (GnuCOBOL 3.2.0's cob_move_alphanum_to_display, which advances over characters until that many digits are
        // consumed; measured 2026-10-25: `Y123` into PIC 9(2)V99 holds 23.00, where `1Y23` is invalid).
        int readCap = characterForm ? integerCap : DeviceReadDigits;
        int count = 0;
        for (int k = i; k < n && window[k] != point; k++)
            if (char.IsAsciiDigit(window[k])) count++;
        int skip = Math.Max(0, count - readCap);
        for (int dropped = 0; dropped < skip; i++)
            if (char.IsAsciiDigit(window[i])) dropped++;

        Int128 integer = 0, fraction = 0;
        int integerRead = skip, fractionRead = 0;
        bool pointSeen = false;
        for (; i < n; i++)
        {
            // The last digit position is filled: a character item stops here and the rest of the window is ignored.
            if (characterForm && integerRead == count && fractionRead == fractionCap) break;
            char c = window[i];
            if (char.IsAsciiDigit(c))
            {
                int d = c - '0';
                if (!pointSeen)
                {
                    integerRead++;
                    integer = integer * 10 + d;
                }
                else if (fractionRead < fractionCap)
                {
                    fraction = fraction % Pow10.AsWide(DeviceReadDigits - 1) * 10 + d;   // the low-order digits are the ones kept
                    fractionRead++;
                }
            }
            else if (c == point)
            {
                if (pointSeen) return 0;
                pointSeen = true;
            }
            else if (c != ' ' && c != group) return 0;
        }

        // The unscaled value: the integer digits cut to the item's integer positions (the high-order end of a surplus
        // a binary or packed item's wider first stage let through), then at the scale plus the fraction padded to the
        // scale, then the store's high-order truncation to the item's digits.
        integer %= Pow10.AsWide(integerCap);
        Int128 value;
        if (scale >= 0)
        {
            int pad = fractionCap - fractionRead;
            Int128 high = scale < digits ? integer * Pow10.AsWide(scale) : 0;
            Int128 low = pad >= digits ? 0 : fraction % Pow10.AsWide(digits - pad) * Pow10.AsWide(pad);
            value = high + low;
        }
        else value = -scale > DeviceReadDigits ? 0 : integer / Pow10.AsWide(-scale);
        if (negative && receiver.Signed) value = -value;
        return binaryCapacity ? WrapBinary(value, receiver) : value % Pow10.AsWide(digits);
    }

    /// <summary>The digit positions a numeric item shows under the decimal device form: its PICTURE's, except a
    /// BINARY-CAPACITY item's, which are its container's (the decimal digits of the largest unsigned value of
    /// <see cref="NumProfile.StorageLength"/> bytes: 3, 5, 10, 20 and 39 for 1, 2, 4, 8 and 16 bytes — GnuCOBOL 3.2.0's
    /// width for COMP-5 and BINARY-CHAR … BINARY-DOUBLE, signed and unsigned alike).</summary>
    private static int DeviceDigits(in NumProfile item) =>
        item.Truncation == NumericTruncation.BinaryCapacity && item.StorageLength > 0
            ? Math.Max(item.Digits, (int)Math.Ceiling(item.StorageLength * 8 * 0.301029995663981195))
            : item.Digits;

    /// <summary>The ONE composition: the sign character, the digit run and the decimal point at the item's scale
    /// (<see cref="NumProfile.FractionDigits"/>, the net of V fraction digits, leading P and trailing P).
    /// <list type="bullet">
    ///   <item>scale &gt; 0 — the point stands before the last <c>scale</c> digits; a scale past the digit run (leading
    ///         <c>P</c>) writes the point and then the zeros the Ps stand for: <c>PIC PP9(3)</c> holding .00123 is <c>.00123</c>;</item>
    ///   <item>scale &lt; 0 — trailing <c>P</c> positions are zeros that occupy no storage and are written:
    ///         <c>PIC 9(3)PP</c> holding 1200 is <c>01200</c>. DISPLAY is none of the operations a) to d) of §13.18.40.4 GR14 (symbol P)
    ///         that use the item's algebraic value, so the Ps are not counted in the item's size; writing their zeros is this GR1
    ///         conversion's choice (the value as a reader writes it, DOC-A.1-56), and the transfer is the converted text's size (GR6);</item>
    ///   <item>a SIGNED item always shows <c>+</c> or <c>-</c>, so zero is <c>+</c>: LEADING, except that an item the
    ///         program wrote <c>SIGN TRAILING SEPARATE</c> shows it where the program put it, trailing
    ///         (<c>001.50-</c>) — the character position §13.18.52.4 GR6 a) gives it;</item>
    /// </list></summary>
    private static string Compose(bool negative, string digits, in NumProfile item, bool commaMode, bool padTo = true)
    {
        if (padTo) digits = digits.PadLeft(DeviceDigits(item), '0');
        int scale = item.FractionDigits;
        string body;
        if (scale > 0)
        {
            char separator = commaMode ? ',' : '.';
            body = scale >= digits.Length
                ? separator + new string('0', scale - digits.Length) + digits
                : digits[..^scale] + separator + digits[^scale..];
        }
        else body = scale < 0 ? digits + new string('0', -scale) : digits;
        if (!item.Signed) return body;
        string sign = negative ? "-" : "+";
        return item.SignKind == NumericSign.TrailingSeparate ? body + sign : sign + body;
    }
}
