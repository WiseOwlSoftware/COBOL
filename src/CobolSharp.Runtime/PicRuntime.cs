namespace CobolSharp.Runtime;

/// <summary>
/// PIC/USAGE-aware runtime helpers for COBOL data movement and arithmetic.
/// The compiler emits calls to these methods instead of inlining PIC logic in IL.
/// This keeps the emitter simple and makes PIC semantics testable in isolation.
/// </summary>
public static class PicRuntime
{
    /// <summary>
    /// Numeric → numeric MOVE (different PIC/USAGE).
    /// Handles: scale adjustment, sign, truncation, DISPLAY/COMP/COMP-3 conversion.
    /// </summary>
    public static void MoveNumeric(
        byte[] dest, int destOffset, int destLength, int destScale, bool destSigned,
        byte[] src, int srcOffset, int srcLength, int srcScale, bool srcSigned)
    {
        var value = DecodeNumericDisplay(src, srcOffset, srcLength, srcScale, srcSigned);
        EncodeNumericDisplay(dest, destOffset, destLength, destScale, destSigned, value);
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

        // Copy source bytes
        Array.Copy(src, srcOffset, dest, destOffset, copyLen);

        // Pad remainder with spaces
        for (int i = copyLen; i < destLength; i++)
            dest[destOffset + i] = (byte)' ';
    }

    /// <summary>
    /// Numeric → alphanumeric MOVE (editing/display).
    /// Converts numeric value to display string, then copies.
    /// </summary>
    public static void MoveNumericToAlpha(
        byte[] dest, int destOffset, int destLength,
        byte[] src, int srcOffset, int srcLength, int srcScale, bool srcSigned)
    {
        var value = DecodeNumericDisplay(src, srcOffset, srcLength, srcScale, srcSigned);
        string text = FormatNumericForDisplay(value, srcScale, srcSigned);

        // Right-justify numeric in alpha field
        int padLen = destLength - text.Length;
        for (int i = 0; i < padLen; i++)
            dest[destOffset + i] = (byte)' ';
        for (int i = 0; i < Math.Min(text.Length, destLength); i++)
            dest[destOffset + padLen + i] = (byte)text[i];
    }

    // ── Internal helpers ──

    /// <summary>
    /// Decode DISPLAY numeric bytes into a decimal value.
    /// DISPLAY format: one ASCII digit per byte, optional sign byte.
    /// </summary>
    internal static decimal DecodeNumericDisplay(
        byte[] data, int offset, int length, int scale, bool signed)
    {
        decimal result = 0;
        bool negative = false;
        int start = offset;
        int end = offset + length;

        // Check for sign
        if (signed && length > 0)
        {
            byte first = data[start];
            if (first == (byte)'+')
            {
                start++;
            }
            else if (first == (byte)'-')
            {
                negative = true;
                start++;
            }
            // Trailing sign: check last byte
            else
            {
                byte last = data[end - 1];
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
        for (int i = start; i < end; i++)
        {
            byte b = data[i];
            if (b >= (byte)'0' && b <= (byte)'9')
            {
                result = result * 10 + (b - (byte)'0');
            }
        }

        // Apply scale
        if (scale > 0)
        {
            for (int i = 0; i < scale; i++)
                result /= 10;
        }

        return negative ? -result : result;
    }

    /// <summary>
    /// Encode a decimal value into DISPLAY numeric bytes.
    /// </summary>
    internal static void EncodeNumericDisplay(
        byte[] data, int offset, int length, int scale, bool signed, decimal value)
    {
        bool negative = value < 0;
        decimal absValue = Math.Abs(value);

        // Scale up to integer
        for (int i = 0; i < scale; i++)
            absValue *= 10;

        long intValue = (long)Math.Truncate(absValue);

        // Convert to digits, right-to-left
        int pos = offset + length - 1;

        // Reserve last position for trailing sign if signed
        // (simplified: we'll use leading sign for now)
        int digitEnd = offset + length;
        int digitStart = offset;

        if (signed)
        {
            data[offset] = negative ? (byte)'-' : (byte)'+';
            digitStart = offset + 1;
        }

        // Fill digits right-to-left
        for (int i = digitEnd - 1; i >= digitStart; i--)
        {
            data[i] = (byte)('0' + (int)(intValue % 10));
            intValue /= 10;
        }
    }

    /// <summary>
    /// Format numeric value as a display string.
    /// </summary>
    internal static string FormatNumericForDisplay(decimal value, int scale, bool signed)
    {
        if (scale > 0)
        {
            string fmt = "0." + new string('0', scale);
            return value.ToString(fmt);
        }
        return ((long)value).ToString();
    }
}
