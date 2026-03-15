// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// PIC/USAGE-aware runtime for COBOL data movement and numeric encoding.
/// All methods use byte[] + offset + length (not Span) for Cecil compatibility.
///
/// Supported: DISPLAY numeric, COMP-3 packed decimal, zoned/overpunch, edited.
/// </summary>
public static class PicRuntime
{
    // ══════════════════════════════════════
    // PIC-aware numeric MOVE (identifier → identifier)
    // ══════════════════════════════════════

    /// <summary>
    /// MOVE numeric TO numeric: decode source, scale/round, encode into destination.
    /// usage: 0=DISPLAY, 3=COMP-3
    /// rounding: 0=truncate, 1=round half up
    /// </summary>
    public static void MoveNumeric(
        byte[] dest, int destOffset, int destLength,
        int destTotalDigits, int destFractionDigits, bool destSigned, int destUsage,
        byte[] src, int srcOffset, int srcLength,
        int srcTotalDigits, int srcFractionDigits, bool srcSigned, int srcUsage,
        int roundingMode)
    {
        // 1. Decode source
        decimal value = DecodeNumeric(src, srcOffset, srcLength,
            srcFractionDigits, srcSigned, srcUsage);

        // 2. Scale/round to destination
        value = ApplyScalingAndRounding(value, destFractionDigits, roundingMode);

        // 3. Encode into destination
        EncodeNumeric(dest, destOffset, destLength,
            destTotalDigits, destFractionDigits, destSigned, destUsage, value);
    }

    /// <summary>
    /// VALUE numeric initialization: encode a decimal literal into a field
    /// according to its PIC/USAGE.
    /// </summary>
    public static void MoveNumericLiteral(
        byte[] dest, int destOffset, int destLength,
        int destTotalDigits, int destFractionDigits, bool destSigned, int destUsage,
        decimal literal, int roundingMode = 0)
    {
        decimal value = ApplyScalingAndRounding(literal, destFractionDigits, roundingMode);
        EncodeNumeric(dest, destOffset, destLength,
            destTotalDigits, destFractionDigits, destSigned, destUsage, value);
    }

    // ══════════════════════════════════════
    // Alphanumeric MOVE (existing, unchanged)
    // ══════════════════════════════════════

    /// <summary>
    /// MOVE alpha TO alpha: left-justified, space-padded.
    /// </summary>
    public static void MoveAlpha(
        byte[] dest, int destOffset, int destLength,
        byte[] src, int srcOffset, int srcLength)
    {
        int copyLen = Math.Min(srcLength, destLength);
        Array.Copy(src, srcOffset, dest, destOffset, copyLen);
        for (int i = copyLen; i < destLength; i++)
            dest[destOffset + i] = (byte)' ';
    }

    // ══════════════════════════════════════
    // Decode: bytes → decimal
    // ══════════════════════════════════════

    public static decimal DecodeNumeric(
        byte[] data, int offset, int length,
        int fractionDigits, bool isSigned, int usage)
    {
        return usage switch
        {
            0 => DecodeDisplay(data, offset, length, fractionDigits, isSigned),
            3 => DecodeComp3(data, offset, length, fractionDigits),
            _ => DecodeDisplay(data, offset, length, fractionDigits, isSigned)
        };
    }

    private static decimal DecodeDisplay(
        byte[] data, int offset, int length, int fractionDigits, bool isSigned)
    {
        if (length == 0) return 0m;

        bool negative = false;
        int start = offset;
        int end = offset + length;

        // Check for leading sign
        if (isSigned && start < end)
        {
            byte first = data[start];
            if (first == (byte)'+') start++;
            else if (first == (byte)'-') { negative = true; start++; }
        }

        // Parse digits
        long intPart = 0;
        for (int i = start; i < end; i++)
        {
            byte b = data[i];
            if (b >= (byte)'0' && b <= (byte)'9')
                intPart = intPart * 10 + (b - (byte)'0');
        }

        decimal value = intPart;
        if (fractionDigits > 0)
            value /= Pow10(fractionDigits);

        return negative ? -value : value;
    }

    private static decimal DecodeComp3(
        byte[] data, int offset, int length, int fractionDigits)
    {
        if (length == 0) return 0m;

        int lastByte = data[offset + length - 1];
        bool negative = (lastByte & 0x0F) == 0x0D;

        long intPart = 0;
        for (int i = offset; i < offset + length - 1; i++)
        {
            intPart = intPart * 10 + ((data[i] >> 4) & 0x0F);
            intPart = intPart * 10 + (data[i] & 0x0F);
        }
        intPart = intPart * 10 + ((lastByte >> 4) & 0x0F);

        decimal value = intPart;
        if (fractionDigits > 0)
            value /= Pow10(fractionDigits);

        return negative ? -value : value;
    }

    // ══════════════════════════════════════
    // Encode: decimal → bytes
    // ══════════════════════════════════════

    public static void EncodeNumeric(
        byte[] data, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned, int usage,
        decimal value)
    {
        switch (usage)
        {
            case 3:
                EncodeComp3(data, offset, length, totalDigits, fractionDigits, value);
                break;
            default:
                EncodeDisplay(data, offset, length, totalDigits, fractionDigits, isSigned, value);
                break;
        }
    }

    private static void EncodeDisplay(
        byte[] data, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned,
        decimal value)
    {
        // Fill with spaces
        for (int i = offset; i < offset + length; i++)
            data[i] = (byte)' ';

        bool negative = value < 0;
        long intPart = (long)Math.Abs(decimal.Truncate(value * Pow10(fractionDigits)));

        // Write digits right-to-left
        int pos = offset + length - 1;
        int digitsWritten = 0;
        while (pos >= offset && (intPart > 0 || digitsWritten < totalDigits))
        {
            data[pos] = (byte)('0' + (int)(intPart % 10));
            intPart /= 10;
            pos--;
            digitsWritten++;
        }

        if (isSigned && pos >= offset)
            data[pos] = negative ? (byte)'-' : (byte)'+';
    }

    private static void EncodeComp3(
        byte[] data, int offset, int length,
        int totalDigits, int fractionDigits, decimal value)
    {
        for (int i = offset; i < offset + length; i++)
            data[i] = 0;

        bool negative = value < 0;
        long intPart = (long)Math.Abs(decimal.Truncate(value * Pow10(fractionDigits)));

        int[] digits = new int[totalDigits];
        for (int i = totalDigits - 1; i >= 0; i--)
        {
            digits[i] = (int)(intPart % 10);
            intPart /= 10;
        }

        int signNibble = negative ? 0x0D : 0x0C;
        int byteIdx = offset + length - 1;
        int digIdx = totalDigits - 1;

        int lastDigit = digIdx >= 0 ? digits[digIdx--] : 0;
        data[byteIdx] = (byte)((lastDigit << 4) | signNibble);
        byteIdx--;

        while (byteIdx >= offset)
        {
            int low = digIdx >= 0 ? digits[digIdx--] : 0;
            int high = digIdx >= 0 ? digits[digIdx--] : 0;
            data[byteIdx] = (byte)((high << 4) | low);
            byteIdx--;
        }
    }

    // ══════════════════════════════════════
    // Scaling / rounding
    // ══════════════════════════════════════

    private static decimal ApplyScalingAndRounding(decimal value, int fractionDigits, int roundingMode)
    {
        if (fractionDigits < 0) fractionDigits = 0;
        return roundingMode switch
        {
            1 => Math.Round(value, fractionDigits, MidpointRounding.AwayFromZero),
            _ => decimal.Truncate(value * Pow10(fractionDigits)) / Pow10(fractionDigits)
        };
    }

    private static decimal Pow10(int n)
    {
        decimal r = 1m;
        for (int i = 0; i < n; i++) r *= 10m;
        return r;
    }

    // ══════════════════════════════════════
    // Display formatting (for DISPLAY statement)
    // ══════════════════════════════════════

    public static string FormatNumericForDisplay(decimal value, int fractionDigits)
    {
        if (fractionDigits > 0)
            return value.ToString("0." + new string('0', fractionDigits), CultureInfo.InvariantCulture);
        return ((long)value).ToString(CultureInfo.InvariantCulture);
    }
}
