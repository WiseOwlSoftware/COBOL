namespace CobolSharp.Runtime;

/// <summary>
/// PIC/USAGE-aware runtime helpers for COBOL data movement and arithmetic.
/// The compiler emits calls to these methods instead of inlining PIC logic in IL.
/// This keeps the emitter simple and makes PIC semantics testable in isolation.
///
/// Supported formats:
///   DISPLAY numeric: ASCII digits with optional leading +/- (no editing)
///   COMP-3: packed decimal, nibble layout, C/D/F sign nibble
/// </summary>
public static class PicRuntime
{
    // ══════════════════════════════════════
    // Unified entry points
    // ══════════════════════════════════════

    /// <summary>
    /// Decode a COBOL numeric field from its storage bytes into a decimal value.
    /// Dispatches based on USAGE.
    /// </summary>
    public static decimal DecodeNumeric(
        byte[] storage, int offset, int length,
        int fractionDigits, bool isSigned, int usage)
    {
        return usage switch
        {
            0 => DecodeDisplayNumeric(storage, offset, length, fractionDigits, isSigned),
            3 => DecodeComp3(storage, offset, length, fractionDigits),
            _ => throw new NotSupportedException($"DecodeNumeric: unsupported usage {usage}")
        };
    }

    /// <summary>
    /// Encode a decimal value into a COBOL numeric field's storage bytes.
    /// Dispatches based on USAGE.
    /// </summary>
    public static void EncodeNumeric(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned,
        int usage, decimal value)
    {
        switch (usage)
        {
            case 0:
                EncodeDisplayNumeric(storage, offset, length, totalDigits, fractionDigits, isSigned, value);
                break;
            case 3:
                EncodeComp3(storage, offset, length, totalDigits, fractionDigits, value);
                break;
            default:
                throw new NotSupportedException($"EncodeNumeric: unsupported usage {usage}");
        }
    }

    // ══════════════════════════════════════
    // MOVE helpers (high-level)
    // ══════════════════════════════════════

    /// <summary>
    /// Numeric → numeric MOVE (different PIC/USAGE).
    /// Handles: scale adjustment, sign, truncation, DISPLAY/COMP-3 conversion.
    /// </summary>
    public static void MoveNumeric(
        byte[] dest, int destOffset, int destLength,
        int destTotalDigits, int destFractionDigits, bool destSigned, int destUsage,
        byte[] src, int srcOffset, int srcLength,
        int srcFractionDigits, bool srcSigned, int srcUsage)
    {
        var value = DecodeNumeric(src, srcOffset, srcLength, srcFractionDigits, srcSigned, srcUsage);
        EncodeNumeric(dest, destOffset, destLength, destTotalDigits, destFractionDigits, destSigned, destUsage, value);
    }

    /// <summary>
    /// Alphanumeric → alphanumeric MOVE.
    /// Left-justified, space-padded/truncated.
    /// </summary>
    public static void MoveAlpha(
        byte[] dest, int destOffset, int destLength,
        byte[] src, int srcOffset, int srcLength)
    {
        int copyLen = Math.Min(srcLength, destLength);
        Array.Copy(src, srcOffset, dest, destOffset, copyLen);

        // Pad remainder with spaces
        for (int i = copyLen; i < destLength; i++)
            dest[destOffset + i] = (byte)' ';
    }

    /// <summary>
    /// Numeric → alphanumeric MOVE.
    /// Converts numeric value to display string, right-justified in alpha field.
    /// </summary>
    public static void MoveNumericToAlpha(
        byte[] dest, int destOffset, int destLength,
        byte[] src, int srcOffset, int srcLength,
        int srcFractionDigits, bool srcSigned, int srcUsage)
    {
        var value = DecodeNumeric(src, srcOffset, srcLength, srcFractionDigits, srcSigned, srcUsage);
        string text = FormatNumericForDisplay(value, srcFractionDigits, srcSigned);

        // Right-justify numeric in alpha field
        int padLen = Math.Max(0, destLength - text.Length);
        for (int i = 0; i < padLen; i++)
            dest[destOffset + i] = (byte)' ';
        for (int i = 0; i < Math.Min(text.Length, destLength); i++)
            dest[destOffset + padLen + i] = (byte)text[i];
    }

    // ══════════════════════════════════════
    // DISPLAY numeric codec
    // ══════════════════════════════════════

    /// <summary>
    /// Decode DISPLAY numeric bytes into a decimal value.
    /// Format: one ASCII digit per byte, optional leading +/- sign.
    /// </summary>
    public static decimal DecodeDisplayNumeric(
        byte[] storage, int offset, int length,
        int fractionDigits, bool isSigned)
    {
        if (length == 0) return 0m;

        bool negative = false;
        int idx = offset;
        int end = offset + length;

        // Check for leading sign
        if (isSigned && idx < end)
        {
            byte first = storage[idx];
            if (first == (byte)'+')
            {
                idx++;
            }
            else if (first == (byte)'-')
            {
                negative = true;
                idx++;
            }
            else
            {
                // Check trailing sign
                byte last = storage[end - 1];
                if (last == (byte)'-')
                {
                    negative = true;
                    end--;
                }
                else if (last == (byte)'+')
                {
                    end--;
                }
            }
        }

        // Parse digits
        long intPart = 0;
        for (int i = idx; i < end; i++)
        {
            byte b = storage[i];
            if (b >= (byte)'0' && b <= (byte)'9')
                intPart = intPart * 10 + (b - (byte)'0');
        }

        // Apply scale
        decimal value = intPart;
        if (fractionDigits > 0)
            value /= Pow10(fractionDigits);

        return negative ? -value : value;
    }

    /// <summary>
    /// Encode a decimal value into DISPLAY numeric bytes.
    /// Right-justified digits, optional leading sign.
    /// </summary>
    public static void EncodeDisplayNumeric(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned,
        decimal value)
    {
        // Fill with spaces
        for (int i = offset; i < offset + length; i++)
            storage[i] = (byte)' ';

        bool negative = value < 0;
        decimal absValue = Math.Abs(value);

        // Scale up to integer representation
        long intPart = (long)decimal.Truncate(absValue * Pow10(fractionDigits));

        // Write digits right-to-left
        int pos = offset + length - 1;
        int digitsWritten = 0;

        while (pos >= offset && (intPart > 0 || digitsWritten < totalDigits))
        {
            int digit = (int)(intPart % 10);
            intPart /= 10;
            storage[pos] = (byte)('0' + digit);
            pos--;
            digitsWritten++;
        }

        // Leading sign
        if (isSigned && pos >= offset)
        {
            storage[pos] = negative ? (byte)'-' : (byte)'+';
        }
    }

    // ══════════════════════════════════════
    // COMP-3 (packed decimal) codec
    // ══════════════════════════════════════

    /// <summary>
    /// Decode COMP-3 packed decimal bytes into a decimal value.
    /// Nibble layout: each byte = two BCD digits, except last byte
    /// where low nibble = sign (C=positive, D=negative, F=unsigned positive).
    /// </summary>
    public static decimal DecodeComp3(
        byte[] storage, int offset, int length, int fractionDigits)
    {
        if (length == 0) return 0m;

        int end = offset + length;

        // Last byte: high nibble = digit, low nibble = sign
        int lastByte = storage[end - 1];
        int signNibble = lastByte & 0x0F;
        bool negative = signNibble == 0x0D;

        // Collect all digits
        long intPart = 0;

        // Process bytes before the last: two digits per byte
        for (int i = offset; i < end - 1; i++)
        {
            int b = storage[i];
            int high = (b >> 4) & 0x0F;
            int low = b & 0x0F;

            intPart = intPart * 10 + high;
            intPart = intPart * 10 + low;
        }

        // Last byte: high nibble only (low nibble is sign)
        int lastHigh = (lastByte >> 4) & 0x0F;
        intPart = intPart * 10 + lastHigh;

        // Apply scale
        decimal value = intPart;
        if (fractionDigits > 0)
            value /= Pow10(fractionDigits);

        return negative ? -value : value;
    }

    /// <summary>
    /// Encode a decimal value into COMP-3 packed decimal bytes.
    /// Nibble layout: two BCD digits per byte, last low nibble = sign.
    /// Sign: C = positive, D = negative.
    /// </summary>
    public static void EncodeComp3(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, decimal value)
    {
        // Zero out
        for (int i = offset; i < offset + length; i++)
            storage[i] = 0;

        bool negative = value < 0;
        decimal absValue = Math.Abs(value);

        // Scale up to integer
        long intPart = (long)decimal.Truncate(absValue * Pow10(fractionDigits));

        // Collect digits least-significant first
        int[] digits = new int[totalDigits];
        for (int i = totalDigits - 1; i >= 0; i--)
        {
            digits[i] = (int)(intPart % 10);
            intPart /= 10;
        }

        // Pack into bytes, most-significant first
        // Last byte: high nibble = last digit, low nibble = sign
        int signNibble = negative ? 0x0D : 0x0C;
        int byteIndex = offset + length - 1;
        int digitIndex = totalDigits - 1;

        int lastDigit = digitIndex >= 0 ? digits[digitIndex--] : 0;
        storage[byteIndex] = (byte)((lastDigit << 4) | signNibble);
        byteIndex--;

        // Remaining bytes: two digits per byte
        while (byteIndex >= offset)
        {
            int low = digitIndex >= 0 ? digits[digitIndex--] : 0;
            int high = digitIndex >= 0 ? digits[digitIndex--] : 0;
            storage[byteIndex] = (byte)((high << 4) | low);
            byteIndex--;
        }
    }

    // ══════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════

    /// <summary>
    /// Format numeric value as a display string.
    /// </summary>
    public static string FormatNumericForDisplay(decimal value, int fractionDigits, bool isSigned)
    {
        if (fractionDigits > 0)
        {
            string fmt = "0." + new string('0', fractionDigits);
            return value.ToString(fmt);
        }
        return ((long)value).ToString();
    }

    private static decimal Pow10(int scale)
    {
        decimal result = 1m;
        for (int i = 0; i < scale; i++)
            result *= 10m;
        return result;
    }
}
