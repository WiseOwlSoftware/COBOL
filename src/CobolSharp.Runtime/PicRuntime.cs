namespace CobolSharp.Runtime;

/// <summary>
/// PIC/USAGE-aware runtime helpers for COBOL data movement and arithmetic.
/// The compiler emits calls to these methods instead of inlining PIC logic in IL.
///
/// Supported formats:
///   DISPLAY plain: ASCII digits with optional leading +/-
///   DISPLAY zoned: overpunch sign in last byte (EBCDIC-style {/} A-I J-R mapping)
///   DISPLAY edited: commas, currency, zero suppression, CR/DB
///   COMP-3: packed decimal, BCD nibble pairs, C/D/F sign nibble
///
/// Rounding modes: Truncate (default), RoundHalfUp (ROUNDED phrase)
/// </summary>
public static class PicRuntime
{
    // ══════════════════════════════════════
    // Unified entry points
    // ══════════════════════════════════════

    public static decimal DecodeNumeric(
        byte[] storage, int offset, int length,
        int fractionDigits, bool isSigned, bool isZoned, bool hasEditing,
        int usage)
    {
        return usage switch
        {
            0 when hasEditing => DecodeDisplayEdited(storage, offset, length, fractionDigits),
            0 when isZoned => DecodeDisplayZoned(storage, offset, length, fractionDigits),
            0 => DecodeDisplayPlain(storage, offset, length, fractionDigits, isSigned),
            3 => DecodeComp3(storage, offset, length, fractionDigits),
            _ => throw new NotSupportedException($"DecodeNumeric: unsupported usage {usage}")
        };
    }

    public static void EncodeNumeric(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned, bool isZoned, bool hasEditing,
        string? editPattern, int usage, decimal value, int roundingMode)
    {
        value = ApplyRounding(value, fractionDigits, roundingMode);

        switch (usage)
        {
            case 0 when hasEditing:
                EncodeDisplayEdited(storage, offset, length, totalDigits, fractionDigits,
                    isSigned, editPattern, value);
                break;
            case 0 when isZoned:
                EncodeDisplayZoned(storage, offset, length, totalDigits, fractionDigits, value);
                break;
            case 0:
                EncodeDisplayPlain(storage, offset, length, totalDigits, fractionDigits,
                    isSigned, value);
                break;
            case 3:
                EncodeComp3(storage, offset, length, totalDigits, fractionDigits, value);
                break;
            default:
                throw new NotSupportedException($"EncodeNumeric: unsupported usage {usage}");
        }
    }

    // ══════════════════════════════════════
    // High-level MOVE helpers
    // ══════════════════════════════════════

    /// <summary>
    /// Numeric → numeric MOVE. Decode source, encode destination.
    /// Handles cross-format moves (e.g., DISPLAY → COMP-3).
    /// </summary>
    public static void MoveNumeric(
        byte[] dest, int destOffset, int destLength,
        int destTotalDigits, int destFractionDigits, bool destSigned,
        bool destIsZoned, bool destHasEditing, string? destEditPattern,
        int destUsage, int destRounding,
        byte[] src, int srcOffset, int srcLength,
        int srcFractionDigits, bool srcSigned, bool srcIsZoned, bool srcHasEditing,
        int srcUsage)
    {
        var value = DecodeNumeric(src, srcOffset, srcLength, srcFractionDigits,
            srcSigned, srcIsZoned, srcHasEditing, srcUsage);
        EncodeNumeric(dest, destOffset, destLength, destTotalDigits, destFractionDigits,
            destSigned, destIsZoned, destHasEditing, destEditPattern,
            destUsage, value, destRounding);
    }

    /// <summary>
    /// Alphanumeric → alphanumeric MOVE. Left-justified, space-padded.
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

    /// <summary>
    /// Numeric → alphanumeric MOVE. Right-justified in alpha field.
    /// </summary>
    public static void MoveNumericToAlpha(
        byte[] dest, int destOffset, int destLength,
        byte[] src, int srcOffset, int srcLength,
        int srcFractionDigits, bool srcSigned, bool srcIsZoned, bool srcHasEditing,
        int srcUsage)
    {
        var value = DecodeNumeric(src, srcOffset, srcLength, srcFractionDigits,
            srcSigned, srcIsZoned, srcHasEditing, srcUsage);
        string text = FormatNumericForDisplay(value, srcFractionDigits, srcSigned);

        int padLen = Math.Max(0, destLength - text.Length);
        for (int i = 0; i < padLen; i++)
            dest[destOffset + i] = (byte)' ';
        for (int i = 0; i < Math.Min(text.Length, destLength); i++)
            dest[destOffset + padLen + i] = (byte)text[i];
    }

    // ══════════════════════════════════════
    // DISPLAY plain codec
    // ══════════════════════════════════════

    public static decimal DecodeDisplayPlain(
        byte[] storage, int offset, int length, int fractionDigits, bool isSigned)
    {
        if (length == 0) return 0m;

        bool negative = false;
        int idx = offset;
        int end = offset + length;

        if (isSigned && idx < end)
        {
            byte first = storage[idx];
            if (first == (byte)'+') { idx++; }
            else if (first == (byte)'-') { negative = true; idx++; }
            else
            {
                byte last = storage[end - 1];
                if (last == (byte)'-') { negative = true; end--; }
                else if (last == (byte)'+') { end--; }
            }
        }

        long intPart = 0;
        for (int i = idx; i < end; i++)
        {
            byte b = storage[i];
            if (b >= (byte)'0' && b <= (byte)'9')
                intPart = intPart * 10 + (b - (byte)'0');
        }

        decimal value = ApplyScale(intPart, fractionDigits);
        return negative ? -value : value;
    }

    public static void EncodeDisplayPlain(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned, decimal value)
    {
        for (int i = offset; i < offset + length; i++)
            storage[i] = (byte)' ';

        bool negative = value < 0;
        long intPart = ScaleToInteger(Math.Abs(value), fractionDigits);

        int pos = offset + length - 1;
        int digitsWritten = 0;

        while (pos >= offset && (intPart > 0 || digitsWritten < totalDigits))
        {
            storage[pos] = (byte)('0' + (int)(intPart % 10));
            intPart /= 10;
            pos--;
            digitsWritten++;
        }

        if (isSigned && pos >= offset)
            storage[pos] = negative ? (byte)'-' : (byte)'+';
    }

    // ══════════════════════════════════════
    // DISPLAY zoned/overpunch codec
    // ══════════════════════════════════════

    public static decimal DecodeDisplayZoned(
        byte[] storage, int offset, int length, int fractionDigits)
    {
        if (length == 0) return 0m;

        long intPart = 0;
        for (int i = offset; i < offset + length - 1; i++)
        {
            byte b = storage[i];
            if (b >= (byte)'0' && b <= (byte)'9')
                intPart = intPart * 10 + (b - (byte)'0');
        }

        int lastDigit = DecodeOverpunch(storage[offset + length - 1], out bool negative);
        intPart = intPart * 10 + lastDigit;

        decimal value = ApplyScale(intPart, fractionDigits);
        return negative ? -value : value;
    }

    public static void EncodeDisplayZoned(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, decimal value)
    {
        for (int i = offset; i < offset + length; i++)
            storage[i] = (byte)' ';

        bool negative = value < 0;
        long intPart = ScaleToInteger(Math.Abs(value), fractionDigits);

        int pos = offset + length - 1;
        int digitsWritten = 0;

        // Last digit → overpunch
        int lastDigit = (int)(intPart % 10);
        intPart /= 10;
        storage[pos] = EncodeOverpunch(lastDigit, negative);
        pos--;
        digitsWritten++;

        while (pos >= offset && (intPart > 0 || digitsWritten < totalDigits))
        {
            storage[pos] = (byte)('0' + (int)(intPart % 10));
            intPart /= 10;
            pos--;
            digitsWritten++;
        }
    }

    // ── Overpunch tables ──

    private static int DecodeOverpunch(byte b, out bool negative)
    {
        char c = (char)b;
        negative = false;

        // Plain digit
        if (c >= '0' && c <= '9') return c - '0';

        // Positive overpunch: { A B C D E F G H I → 0..9
        switch (c)
        {
            case '{': return 0;
            case 'A': return 1; case 'B': return 2; case 'C': return 3;
            case 'D': return 4; case 'E': return 5; case 'F': return 6;
            case 'G': return 7; case 'H': return 8; case 'I': return 9;
        }

        // Negative overpunch: } J K L M N O P Q R → 0..9
        negative = true;
        return c switch
        {
            '}' => 0,
            'J' => 1, 'K' => 2, 'L' => 3,
            'M' => 4, 'N' => 5, 'O' => 6,
            'P' => 7, 'Q' => 8, 'R' => 9,
            _ => 0
        };
    }

    private static byte EncodeOverpunch(int digit, bool negative)
    {
        if (!negative)
            return digit switch
            {
                0 => (byte)'{', 1 => (byte)'A', 2 => (byte)'B', 3 => (byte)'C',
                4 => (byte)'D', 5 => (byte)'E', 6 => (byte)'F', 7 => (byte)'G',
                8 => (byte)'H', 9 => (byte)'I', _ => (byte)'0'
            };

        return digit switch
        {
            0 => (byte)'}', 1 => (byte)'J', 2 => (byte)'K', 3 => (byte)'L',
            4 => (byte)'M', 5 => (byte)'N', 6 => (byte)'O', 7 => (byte)'P',
            8 => (byte)'Q', 9 => (byte)'R', _ => (byte)'0'
        };
    }

    // ══════════════════════════════════════
    // DISPLAY edited codec
    // ══════════════════════════════════════

    public static decimal DecodeDisplayEdited(
        byte[] storage, int offset, int length, int fractionDigits)
    {
        // Strip non-digit/non-sign chars, interpret as plain numeric
        bool negative = false;
        long intPart = 0;

        for (int i = offset; i < offset + length; i++)
        {
            char c = (char)storage[i];
            if (c == '-') { negative = true; continue; }
            if (c == '+') continue;
            if (c >= '0' && c <= '9') { intPart = intPart * 10 + (c - '0'); continue; }
            // Commas, periods, currency, CR, DB — skip
        }

        decimal value = ApplyScale(intPart, fractionDigits);
        return negative ? -value : value;
    }

    public static void EncodeDisplayEdited(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, bool isSigned,
        string? editPattern, decimal value)
    {
        // Format as plain digits first
        bool negative = value < 0;
        long intPart = ScaleToInteger(Math.Abs(value), fractionDigits);

        // Collect digits right-to-left
        int[] digits = new int[totalDigits];
        for (int i = totalDigits - 1; i >= 0; i--)
        {
            digits[i] = (int)(intPart % 10);
            intPart /= 10;
        }

        // Fill storage with spaces
        for (int i = offset; i < offset + length; i++)
            storage[i] = (byte)' ';

        if (editPattern == null)
        {
            // No pattern — fall back to plain
            EncodeDisplayPlain(storage, offset, length, totalDigits, fractionDigits, isSigned, value);
            return;
        }

        // Overlay digits into edit pattern from right to left
        int digitIdx = totalDigits - 1;
        for (int i = Math.Min(editPattern.Length, length) - 1; i >= 0; i--)
        {
            char p = editPattern[i];
            if (p == '9' || p == 'Z' || p == '*')
            {
                if (digitIdx >= 0)
                    storage[offset + i] = (byte)('0' + digits[digitIdx--]);
                else
                    storage[offset + i] = (p == '9') ? (byte)'0' : (byte)' ';
            }
            else
            {
                storage[offset + i] = (byte)p; // literal: comma, period, currency, etc.
            }
        }
    }

    // ══════════════════════════════════════
    // COMP-3 (packed decimal) codec
    // ══════════════════════════════════════

    public static decimal DecodeComp3(
        byte[] storage, int offset, int length, int fractionDigits)
    {
        if (length == 0) return 0m;

        int lastByte = storage[offset + length - 1];
        int signNibble = lastByte & 0x0F;
        bool negative = signNibble == 0x0D;

        long intPart = 0;

        // All bytes except last: two digits per byte
        for (int i = offset; i < offset + length - 1; i++)
        {
            int b = storage[i];
            intPart = intPart * 10 + ((b >> 4) & 0x0F);
            intPart = intPart * 10 + (b & 0x0F);
        }

        // Last byte: high nibble = digit, low nibble = sign
        intPart = intPart * 10 + ((lastByte >> 4) & 0x0F);

        decimal value = ApplyScale(intPart, fractionDigits);
        return negative ? -value : value;
    }

    public static void EncodeComp3(
        byte[] storage, int offset, int length,
        int totalDigits, int fractionDigits, decimal value)
    {
        for (int i = offset; i < offset + length; i++)
            storage[i] = 0;

        bool negative = value < 0;
        long intPart = ScaleToInteger(Math.Abs(value), fractionDigits);

        // Collect digits MS-first
        int[] digits = new int[totalDigits];
        for (int i = totalDigits - 1; i >= 0; i--)
        {
            digits[i] = (int)(intPart % 10);
            intPart /= 10;
        }

        // Pack: last byte = (lastDigit << 4) | sign
        int signNibble = negative ? 0x0D : 0x0C;
        int byteIdx = offset + length - 1;
        int digIdx = totalDigits - 1;

        int lastDigit = digIdx >= 0 ? digits[digIdx--] : 0;
        storage[byteIdx] = (byte)((lastDigit << 4) | signNibble);
        byteIdx--;

        // Remaining bytes: two digits per byte
        while (byteIdx >= offset)
        {
            int low = digIdx >= 0 ? digits[digIdx--] : 0;
            int high = digIdx >= 0 ? digits[digIdx--] : 0;
            storage[byteIdx] = (byte)((high << 4) | low);
            byteIdx--;
        }
    }

    // ══════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════

    public static string FormatNumericForDisplay(decimal value, int fractionDigits, bool isSigned)
    {
        if (fractionDigits > 0)
            return value.ToString("0." + new string('0', fractionDigits));
        return ((long)value).ToString();
    }

    private static decimal ApplyRounding(decimal value, int fractionDigits, int roundingMode)
    {
        if (fractionDigits < 0) fractionDigits = 0;

        return roundingMode switch
        {
            0 => // Truncate
                decimal.Truncate(value * Pow10(fractionDigits)) / Pow10(fractionDigits),
            1 => // RoundHalfUp
                Math.Round(value, fractionDigits, MidpointRounding.AwayFromZero),
            _ => value
        };
    }

    private static decimal ApplyScale(long intPart, int fractionDigits)
    {
        if (fractionDigits <= 0) return intPart;
        return intPart / Pow10(fractionDigits);
    }

    private static long ScaleToInteger(decimal value, int fractionDigits)
    {
        if (fractionDigits <= 0) return (long)decimal.Truncate(value);
        return (long)decimal.Truncate(value * Pow10(fractionDigits));
    }

    private static decimal Pow10(int scale)
    {
        decimal result = 1m;
        for (int i = 0; i < scale; i++)
            result *= 10m;
        return result;
    }
}
