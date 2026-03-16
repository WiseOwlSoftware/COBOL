// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Status returned by MOVE operations.
/// </summary>
public struct MoveStatus
{
    public bool Truncated { get; set; }
}

/// <summary>
/// Status returned by arithmetic operations (for ON SIZE ERROR).
/// </summary>
public struct ArithmeticStatus
{
    public bool SizeError;
}

/// <summary>
/// PIC/USAGE-aware runtime for COBOL data movement, arithmetic, and comparison.
/// Public surface organized by (OperationKind × source CobolCategory × target CobolCategory).
/// All methods use byte[] + offset + length + PicDescriptor.
/// </summary>
public static class PicRuntime
{
    // ══════════════════════════════════════════════════════════
    // MOVE: Numeric → …
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericToNumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);

        // ISO §14.19.4: when moving to an unsigned numeric DISPLAY target,
        // the sign is not preserved; the magnitude is stored.
        if (!dstPic.IsSigned && dstPic.IsNumeric && !dstPic.HasEditing)
        {
            value = Math.Abs(value);
        }

        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, value);
    }

    public static void MoveNumericToNumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        string formatted = FormatNumericEdited(value, dstPic);
        MoveStringToBytes(dstArea, dstOffset, dstLength, formatted);
    }

    /// <summary>
    /// Format a numeric value into an edited picture string.
    /// Handles zero-suppress, currency, CR/DB based on EditingKind.
    /// </summary>
    public static string FormatNumericEdited(decimal value, PicDescriptor pic)
    {
        if (pic.EditPattern != null)
            return FormatByEditPattern(value, pic);

        if (pic.BlankWhenZero && value == 0m)
            return new string(' ', pic.StorageLength);

        bool negative = value < 0m;
        decimal absValue = Math.Abs(value);

        int scale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (scale < 0) scale = 0;

        decimal scaled = absValue * Pow10(scale);
        long intValue = (long)scaled;
        string digits = intValue.ToString(CultureInfo.InvariantCulture);

        if (digits.Length < pic.TotalDigits)
            digits = digits.PadLeft(pic.TotalDigits, '0');
        else if (digits.Length > pic.TotalDigits)
            digits = digits.Substring(digits.Length - pic.TotalDigits);

        // Split digits into integer and fraction parts
        int intDigits = pic.TotalDigits - pic.FractionDigits;
        string intPart = digits.Substring(0, intDigits);
        string fracPart = pic.FractionDigits > 0 ? digits.Substring(intDigits) : "";

        // Determine if decimal point insertion is needed
        // StorageLength > TotalDigits + sign chars means there's room for a decimal point
        bool hasSeparateSign = pic.IsSigned && pic.Editing != EditingKind.CreditDebit;
        int signChars = hasSeparateSign ? 1 : 0;
        int crDbChars = (pic.Editing == EditingKind.CreditDebit) ? 2 : 0;
        bool hasDecimalPoint = pic.FractionDigits > 0 &&
            pic.StorageLength > pic.TotalDigits + signChars + crDbChars;

        var chars = new char[pic.StorageLength];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = ' ';

        // Build output: [sign] [integer digits] [.] [fraction digits] [CR/DB]
        int pos = signChars; // reserve leading sign position

        // Right-justify integer digits in their field
        int intFieldWidth = intDigits;
        int intStart = pos + (intFieldWidth - intPart.Length);
        for (int i = 0; i < intPart.Length; i++)
        {
            int idx = intStart + i;
            if (idx >= 0 && idx < chars.Length)
                chars[idx] = intPart[i];
        }
        pos += intFieldWidth;

        // Decimal point
        if (hasDecimalPoint && pos < chars.Length)
            chars[pos++] = '.';

        // Fraction digits
        for (int i = 0; i < fracPart.Length && pos + i < chars.Length; i++)
            chars[pos + i] = fracPart[i];

        // Apply editing
        switch (pic.Editing)
        {
            case EditingKind.ZeroSuppress:
            {
                // Replace leading zeros with spaces (up to but not including decimal point)
                int suppressEnd = hasDecimalPoint ? signChars + intFieldWidth : chars.Length;
                for (int i = signChars; i < suppressEnd; i++)
                {
                    if (chars[i] == '0') chars[i] = ' ';
                    else break;
                }
                break;
            }

            case EditingKind.Currency:
                // Place $ before first non-space digit
                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] != ' ') { chars[i] = '$'; break; }
                }
                break;

            case EditingKind.CreditDebit:
                if (negative && chars.Length >= 2)
                {
                    chars[^2] = 'C';
                    chars[^1] = 'R';
                }
                break;
        }

        if (negative && hasSeparateSign)
        {
            chars[0] = '-';
        }

        return new string(chars);
    }

    private static string FormatByEditPattern(decimal value, PicDescriptor pic)
    {
        string pattern = pic.EditPattern!;
        bool negative = value < 0m;
        decimal absValue = Math.Abs(value);

        int scale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (scale < 0) scale = 0;
        decimal scaled = absValue * Pow10(scale);
        long intValue = (long)scaled;
        string digits = intValue.ToString(CultureInfo.InvariantCulture);
        if (digits.Length < pic.TotalDigits)
            digits = digits.PadLeft(pic.TotalDigits, '0');
        else if (digits.Length > pic.TotalDigits)
            digits = digits.Substring(digits.Length - pic.TotalDigits);

        // Pre-scan for sign symbols so we can distinguish fixed vs floating
        int plusCount = 0;
        int minusCount = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == '+') plusCount++;
            else if (p == '-') minusCount++;
        }

        // Single '-' and no '+' → fixed sign position (e.g. PIC -9(9).9(9))
        int fixedMinusIndex = -1;
        if (minusCount == 1 && plusCount == 0)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (char.ToUpperInvariant(pattern[i]) == '-')
                {
                    fixedMinusIndex = i;
                    break;
                }
            }
        }

        // Pass 1: Fill digit positions right-to-left, place insertion chars
        var output = new char[pattern.Length];
        int digitIdx = digits.Length - 1;

        for (int i = pattern.Length - 1; i >= 0; i--)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            switch (p)
            {
                case '9':
                case 'Z':
                case '*':
                case '+':
                case '-':
                case '$':
                    if (i == fixedMinusIndex && p == '-')
                    {
                        // Fixed sign: do not consume a digit here
                        output[i] = ' ';
                    }
                    else
                    {
                        output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                    }
                    break;

                case '.':
                    output[i] = '.';
                    break;

                case ',':
                    output[i] = ',';
                    break;

                case 'B':
                    output[i] = ' ';
                    break;

                case '/':
                    output[i] = '/';
                    break;

                case '0':
                    output[i] = '0';
                    break;

                case 'C': // CR
                    if (i + 1 < pattern.Length && char.ToUpperInvariant(pattern[i + 1]) == 'R')
                    {
                        output[i] = negative ? 'C' : ' ';
                        output[i + 1] = negative ? 'R' : ' ';
                    }
                    else output[i] = pattern[i];
                    break;

                case 'R': // second char of CR — already handled
                    if (i > 0 && char.ToUpperInvariant(pattern[i - 1]) == 'C')
                        break;
                    output[i] = pattern[i];
                    break;

                case 'D': // DB
                    if (i + 1 < pattern.Length && char.ToUpperInvariant(pattern[i + 1]) == 'B')
                    {
                        output[i] = negative ? 'D' : ' ';
                        output[i + 1] = negative ? 'B' : ' ';
                    }
                    else output[i] = pattern[i];
                    break;

                default:
                    output[i] = pattern[i];
                    break;
            }
        }

        // Pass 2: Left-to-right zero suppression for Z, *, +, -, $ floating symbols
        bool suppressing = true;
        for (int i = 0; i < pattern.Length && suppressing; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            switch (p)
            {
                case 'Z':
                    if (output[i] == '0') output[i] = ' ';
                    else suppressing = false;
                    break;

                case '*':
                    if (output[i] == '0') output[i] = '*';
                    else suppressing = false;
                    break;

                case '+':
                    if (output[i] == '0') output[i] = ' ';
                    else suppressing = false;
                    break;

                case '-':
                    if (i == fixedMinusIndex)
                        break; // Fixed sign does not participate in floating suppression
                    if (output[i] == '0') output[i] = ' ';
                    else suppressing = false;
                    break;

                case '$':
                    if (output[i] == '0') output[i] = ' ';
                    else suppressing = false;
                    break;

                case ',':
                    output[i] = ' ';
                    break;

                case '9':
                    suppressing = false;
                    break;

                case '.':
                    suppressing = false;
                    break;

                default:
                    break;
            }
        }

        // Handle floating sign symbols: find the rightmost suppressed +/- and place sign there
        bool hasFloatingPlus = plusCount > 0 && (plusCount + minusCount) > 1;
        bool hasFloatingMinus = minusCount > 0 && (plusCount + minusCount) > 1;

        if (hasFloatingPlus)
        {
            int signPos = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                char p = char.ToUpperInvariant(pattern[i]);
                if (p == '+' && output[i] == ' ')
                    signPos = i;
                else if (p == '+' && output[i] != ' ')
                    break;
            }
            if (signPos >= 0)
                output[signPos] = negative ? '-' : '+';
        }
        else if (hasFloatingMinus)
        {
            int signPos = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                char p = char.ToUpperInvariant(pattern[i]);
                if (p == '-' && i != fixedMinusIndex && output[i] == ' ')
                    signPos = i;
                else if (p == '-' && i != fixedMinusIndex && output[i] != ' ')
                    break;
            }
            if (signPos >= 0)
                output[signPos] = negative ? '-' : ' ';
        }

        // Handle floating $: place '$' at rightmost suppressed $ position
        int dollarCount = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (char.ToUpperInvariant(pattern[i]) == '$') dollarCount++;
        }

        if (dollarCount > 1)
        {
            int dollarPos = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                char p = char.ToUpperInvariant(pattern[i]);
                if (p == '$' && output[i] == ' ')
                    dollarPos = i;
                else if (p == '$' && output[i] != ' ')
                    break;
            }
            if (dollarPos >= 0)
                output[dollarPos] = '$';
        }
        else if (dollarCount == 1)
        {
            // Single $ is a fixed currency symbol, not floating
            for (int i = 0; i < pattern.Length; i++)
            {
                if (char.ToUpperInvariant(pattern[i]) == '$')
                {
                    output[i] = '$';
                    break;
                }
            }
        }

        // Fixed leading '-' (e.g. PIC -9(9).9(9)): show '-' only for negative, space for positive
        if (fixedMinusIndex >= 0)
        {
            output[fixedMinusIndex] = negative ? '-' : ' ';
        }

        return new string(output);
    }

    public static void MoveNumericToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        // Per ISO §14.19.4: numeric → alphanumeric strips sign (absolute value only)
        value = Math.Abs(value);
        int fractionScale = srcPic.FractionDigits + srcPic.LeadingScaleDigits;
        string formatted = FormatNumericForDisplay(value, fractionScale, srcPic.TotalDigits);
        MoveStringToBytes(dstArea, dstOffset, dstLength, formatted);
    }

    public static void MoveNumericToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveNumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Alphanumeric → …
    // ══════════════════════════════════════════════════════════

    public static void MoveAlphanumericToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // Left-justified, space-padded
        int copyLen = Math.Min(srcLength, dstLength);
        Array.Copy(srcArea, srcOffset, dstArea, dstOffset, copyLen);
        for (int i = copyLen; i < dstLength; i++)
            dstArea[dstOffset + i] = (byte)' ';
    }

    public static void MoveAlphanumericToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: NumericEdited → …
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericEditedToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // Treat numeric-edited as alphanumeric for MOVE to alpha targets
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericEditedToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: AlphanumericEdited → …
    // ══════════════════════════════════════════════════════════

    public static void MoveAlphanumericEditedToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericEditedToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Alphanumeric → Numeric
    // ══════════════════════════════════════════════════════════

    public static void MoveAlphanumericToNumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        string raw = Encoding.ASCII.GetString(srcArea, srcOffset, srcLength).Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, 0m);
            return;
        }

        raw = raw.Replace(",", "").Replace("$", "").Replace("CR", "").Replace("DB", "").Trim();

        if (!decimal.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                              CultureInfo.InvariantCulture, out var value))
        {
            value = 0m;
        }

        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: NumericEdited → Numeric
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericEditedToNumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        string raw = Encoding.ASCII.GetString(srcArea, srcOffset, srcLength).Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, 0m);
            return;
        }

        raw = raw.Replace(",", "").Replace("$", "").Replace("CR", "").Replace("DB", "").Replace("*", "").Trim();

        bool negative = raw.Contains('-');
        raw = raw.Replace("-", "").Replace("+", "");

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint,
                              CultureInfo.InvariantCulture, out var value))
        {
            value = 0m;
        }

        if (negative) value = -value;

        // ISO §14.19.4: unsigned target strips sign
        if (!dstPic.IsSigned && dstPic.IsNumeric && !dstPic.HasEditing)
        {
            value = Math.Abs(value);
        }

        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Alphanumeric → NumericEdited
    // ══════════════════════════════════════════════════════════

    public static void MoveAlphanumericToNumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        string raw = Encoding.ASCII.GetString(srcArea, srcOffset, srcLength).Trim();

        if (!decimal.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                              CultureInfo.InvariantCulture, out var value))
        {
            value = 0m;
        }

        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        string formatted = FormatNumericEdited(value, dstPic);
        MoveStringToBytes(dstArea, dstOffset, dstLength, formatted);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Figurative constants (SPACE, ZERO, HIGH-VALUE, etc.)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// MOVE figurative-constant TO field. Fills the entire destination field
    /// with the appropriate byte value. For numeric destinations with ZERO,
    /// encodes numeric zero instead.
    /// </summary>
    public static void MoveFigurativeToField(
        byte[] dstArea, int dstOffset, int dstLength,
        PicDescriptor dstPic, int figurativeKindInt)
    {
        var kind = (FigurativeKind)figurativeKindInt;

        if (dstPic.IsNumeric && kind == FigurativeKind.Zero)
        {
            EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, 0m);
            return;
        }

        // Alphanumeric or non-zero figurative: fill entire field with figurative byte
        byte b = kind switch
        {
            FigurativeKind.Zero => (byte)'0',
            FigurativeKind.Space => (byte)' ',
            FigurativeKind.HighValue => 0xFF,
            FigurativeKind.LowValue => 0x00,
            FigurativeKind.Quote => (byte)'"',
            FigurativeKind.Null => 0x00,
            _ => (byte)' '
        };
        for (int i = 0; i < dstLength; i++)
            dstArea[dstOffset + i] = b;
    }

    /// <summary>
    /// MOVE ALL "pattern" TO field. Repeats the pattern to fill the entire field.
    /// </summary>
    public static void MoveAllLiteralToField(
        byte[] dstArea, int dstOffset, int dstLength,
        byte[] pattern)
    {
        if (pattern.Length == 0)
        {
            for (int i = 0; i < dstLength; i++)
                dstArea[dstOffset + i] = (byte)' ';
            return;
        }
        int pos = 0;
        for (int i = 0; i < dstLength; i++)
        {
            dstArea[dstOffset + i] = pattern[pos];
            if (++pos >= pattern.Length) pos = 0;
        }
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Literal helpers (called by emitter for MOVE "lit" TO field)
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode = 0)
    {
        decimal value = ApplyScalingAndRounding(literal, destPic, roundingMode);
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Legacy aliases (keep CIL emitter working during transition)
    // ══════════════════════════════════════════════════════════

    public static void MoveNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode)
    {
        MoveNumericToNumeric(srcArea, srcOffset, srcLength, srcPic,
            destArea, destOffset, destLength, destPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MULTIPLY
    // ══════════════════════════════════════════════════════════

    public static void MultiplyNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal right = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        decimal value = left * right;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void MultiplyNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal,
        byte[] otherArea, int otherOffset, int otherLength, PicDescriptor otherPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal other = DecodeNumeric(otherArea, otherOffset, otherLength, otherPic);
        decimal value = literal * other;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // ADD
    // ══════════════════════════════════════════════════════════

    public static void AddNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal src = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        decimal value = dest + src;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void AddNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest + literal;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // SUBTRACT
    // ══════════════════════════════════════════════════════════

    public static void SubtractNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal src = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        decimal value = dest - src;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void SubtractNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest - literal;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // ADD/SUBTRACT with pre-accumulated operand sum
    // COBOL spec requires summing all operands first, then
    // applying the sum to each target (with per-target rounding).
    // ══════════════════════════════════════════════════════════

    public static void AddAccumulatedToField(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal accumulated, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest + accumulated;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    /// <summary>
    /// GIVING form: store accumulated value directly into target (target = accumulated).
    /// Does NOT add to the target's current value.
    /// </summary>
    public static void MoveAccumulatedToField(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal accumulated, int roundingMode, ref ArithmeticStatus status)
    {
        decimal value = ApplyScalingAndRounding(accumulated, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void SubtractAccumulatedFromField(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal accumulated, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest - accumulated;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // DIVIDE
    // ══════════════════════════════════════════════════════════

    public static void DivideNumeric(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal right = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        if (right == 0m)
        {
            status.SizeError = true;
            return;
        }
        decimal value = left / right;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    public static void DivideNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal,
        byte[] otherArea, int otherOffset, int otherLength, PicDescriptor otherPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal other = DecodeNumeric(otherArea, otherOffset, otherLength, otherPic);
        if (literal == 0m)
        {
            status.SizeError = true;
            return;
        }
        decimal value = other / literal;
        value = ApplyScalingAndRounding(value, destPic, roundingMode);
        if (WouldOverflow(value, destPic))
        {
            status.SizeError = true;
            return;
        }
        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ══════════════════════════════════════════════════════════
    // COMPARE: Numeric
    // ══════════════════════════════════════════════════════════

    /// <summary>Returns -1, 0, or 1.</summary>
    public static int CompareNumeric(
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal right = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        return Math.Sign(left - right);
    }

    public static int CompareNumericToLiteral(
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        decimal literal)
    {
        decimal left = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        return Math.Sign(left - literal);
    }

    // ══════════════════════════════════════════════════════════
    // COMPARE: Alphanumeric
    // ══════════════════════════════════════════════════════════

    /// <summary>Alphanumeric comparison using collating sequence. Returns -1, 0, or 1.</summary>
    public static int CompareAlphanumeric(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        int maxLen = Math.Max(leftLength, rightLength);
        for (int i = 0; i < maxLen; i++)
        {
            byte lb = i < leftLength ? leftArea[leftOffset + i] : (byte)' ';
            byte rb = i < rightLength ? rightArea[rightOffset + i] : (byte)' ';
            if (lb < rb) return -1;
            if (lb > rb) return 1;
        }
        return 0;
    }

    /// <summary>National comparison. Returns -1, 0, or 1.</summary>
    public static int CompareNational(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        // National uses 2-byte characters; for now treat same as alphanumeric
        return CompareAlphanumeric(leftArea, leftOffset, leftLength,
            rightArea, rightOffset, rightLength);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: NumericEdited → NumericEdited
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericEditedToNumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // Edited-to-edited: raw byte copy (same as alpha-to-alpha)
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: National stubs
    // ══════════════════════════════════════════════════════════

    public static void MoveNationalToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalEditedToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // Also add missing cross-category MOVE stubs for Numeric → National
    public static void MoveNumericToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveNumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveNumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericEditedToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNumericEditedToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericEditedToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveAlphanumericEditedToNationalEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // Decode: bytes → decimal
    // ══════════════════════════════════════════════════════════

    public static decimal DecodeNumeric(
        byte[] area, int offset, int length, PicDescriptor pic)
    {
        return pic.Usage switch
        {
            UsageKind.Display => DecodeDisplay(area, offset, length, pic),
            UsageKind.Comp3 or UsageKind.PackedDecimal => DecodeComp3(area, offset, length),
            UsageKind.Comp or UsageKind.Binary => DecodeCompBinary(area, offset, length, pic),
            _ => DecodeDisplay(area, offset, length, pic)
        };
    }

    /// <summary>
    /// DISPLAY numeric decoding:
    /// - Field contains digits only (no decimal point stored)
    /// - Uses PicDescriptor.FractionDigits to restore the implied decimal
    /// - Handles leading '-' for signed fields
    /// </summary>
    private static decimal DecodeDisplay(byte[] area, int offset, int length, PicDescriptor pic)
    {
        var s = Encoding.ASCII.GetString(area, offset, length);

        // BLANK WHEN ZERO
        if (pic.BlankWhenZero && string.IsNullOrWhiteSpace(s))
            return 0m;

        s = s.Trim();
        if (string.IsNullOrEmpty(s)) return 0m;

        // Extract sign based on SignStorageKind
        bool negative = false;
        switch (pic.SignStorage)
        {
            case SignStorageKind.TrailingSeparate:
                if (s.Length > 0 && s[^1] == '-') { negative = true; s = s[..^1].Trim(); }
                else if (s.Length > 0 && s[^1] == '+') { s = s[..^1].Trim(); }
                break;

            case SignStorageKind.LeadingSeparate:
                if (s[0] == '-') { negative = true; s = s[1..].Trim(); }
                else if (s[0] == '+') { s = s[1..].Trim(); }
                break;

            case SignStorageKind.TrailingOverpunch:
            {
                // Last byte is an overpunched digit encoding the sign
                if (s.Length > 0)
                {
                    var (digit, neg) = DecodeOverpunch((byte)s[^1]);
                    negative = neg;
                    s = s[..^1] + digit;
                }
                break;
            }

            case SignStorageKind.LeadingOverpunch:
            {
                // First byte is an overpunched digit encoding the sign
                if (s.Length > 0)
                {
                    var (digit, neg) = DecodeOverpunch((byte)s[0]);
                    negative = neg;
                    s = digit + s[1..];
                }
                break;
            }

            default:
                // Unsigned or None — try leading sign as fallback
                if (s[0] == '-') { negative = true; s = s[1..].Trim(); }
                else if (s[0] == '+') { s = s[1..].Trim(); }
                break;
        }

        if (string.IsNullOrEmpty(s)) return 0m;

        // Try to parse as integer (digits-only, no decimal point)
        if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out long intVal))
        {
            decimal result = intVal;

            // Apply implied decimal from FractionDigits + leading P scaling
            // Leading P = additional implied fraction positions not stored
            // Trailing P = additional implied integer positions not stored
            int totalFractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
            if (totalFractionScale > 0)
                result /= Pow10(totalFractionScale);
            if (pic.TrailingScaleDigits > 0)
                result *= Pow10(pic.TrailingScaleDigits);

            return negative ? -result : result;
        }

        // Fallback: try decimal parse (handles legacy data with embedded decimal)
        if (decimal.TryParse(s,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var fallback))
            return negative ? -fallback : fallback;

        return 0m;
    }

    private static decimal DecodeComp3(byte[] area, int offset, int length)
    {
        if (length == 0) return 0m;
        int lastByte = area[offset + length - 1];
        bool negative = (lastByte & 0x0F) == 0x0D;
        long intPart = 0;
        for (int i = offset; i < offset + length - 1; i++)
        {
            intPart = intPart * 10 + ((area[i] >> 4) & 0x0F);
            intPart = intPart * 10 + (area[i] & 0x0F);
        }
        intPart = intPart * 10 + ((lastByte >> 4) & 0x0F);
        return negative ? -intPart : intPart;
    }

    /// <summary>
    /// COMP/BINARY decoding: 2/4/8-byte signed big-endian integer.
    /// Applies FractionDigits and P scaling to produce a decimal value.
    /// </summary>
    private static decimal DecodeCompBinary(byte[] area, int offset, int length, PicDescriptor pic)
    {
        long raw = length switch
        {
            2 => (short)((area[offset] << 8) | area[offset + 1]),
            4 => (int)(
                    ((uint)area[offset] << 24) |
                    ((uint)area[offset + 1] << 16) |
                    ((uint)area[offset + 2] << 8) |
                    area[offset + 3]),
            8 => (long)(
                    ((ulong)area[offset] << 56) |
                    ((ulong)area[offset + 1] << 48) |
                    ((ulong)area[offset + 2] << 40) |
                    ((ulong)area[offset + 3] << 32) |
                    ((ulong)area[offset + 4] << 24) |
                    ((ulong)area[offset + 5] << 16) |
                    ((ulong)area[offset + 6] << 8) |
                    area[offset + 7]),
            _ => 0
        };

        decimal result = raw;

        // Apply implied decimal + leading P scaling
        int totalFractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (totalFractionScale > 0)
            result /= Pow10(totalFractionScale);
        if (pic.TrailingScaleDigits > 0)
            result *= Pow10(pic.TrailingScaleDigits);

        return result;
    }

    /// <summary>
    /// COMP/BINARY encoding: decimal → 2/4/8-byte signed big-endian integer.
    /// </summary>
    private static void EncodeCompBinary(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        // Apply scaling to get integer representation
        // Leading P = additional implied fraction (multiply to remove)
        // Trailing P = additional implied integer (divide to remove)
        decimal scaled = value;
        int totalFractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (totalFractionScale > 0)
            scaled *= Pow10(totalFractionScale);
        if (pic.TrailingScaleDigits > 0)
            scaled /= Pow10(pic.TrailingScaleDigits);

        long raw = (long)decimal.Truncate(scaled);

        // Unsigned field: store absolute value (COBOL strips sign on MOVE to unsigned)
        if (!pic.IsSigned && raw < 0)
            raw = -raw;

        switch (length)
        {
            case 2:
            {
                short s = (short)raw;
                area[offset] = (byte)((s >> 8) & 0xFF);
                area[offset + 1] = (byte)(s & 0xFF);
                break;
            }
            case 4:
            {
                int i = (int)raw;
                area[offset] = (byte)((i >> 24) & 0xFF);
                area[offset + 1] = (byte)((i >> 16) & 0xFF);
                area[offset + 2] = (byte)((i >> 8) & 0xFF);
                area[offset + 3] = (byte)(i & 0xFF);
                break;
            }
            case 8:
            {
                area[offset] = (byte)((raw >> 56) & 0xFF);
                area[offset + 1] = (byte)((raw >> 48) & 0xFF);
                area[offset + 2] = (byte)((raw >> 40) & 0xFF);
                area[offset + 3] = (byte)((raw >> 32) & 0xFF);
                area[offset + 4] = (byte)((raw >> 24) & 0xFF);
                area[offset + 5] = (byte)((raw >> 16) & 0xFF);
                area[offset + 6] = (byte)((raw >> 8) & 0xFF);
                area[offset + 7] = (byte)(raw & 0xFF);
                break;
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    // Encode: decimal → bytes
    // ══════════════════════════════════════════════════════════

    public static void EncodeNumeric(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        switch (pic.Usage)
        {
            case UsageKind.Comp3:
            case UsageKind.PackedDecimal:
                EncodeComp3(area, offset, length, value);
                break;
            case UsageKind.Comp:
            case UsageKind.Binary:
                EncodeCompBinary(area, offset, length, pic, value);
                break;
            default:
                EncodeDisplay(area, offset, length, pic, value);
                break;
        }
    }

    /// <summary>
    /// DISPLAY numeric encoding:
    /// - Implied decimal: no '.' stored; digits only
    /// - Uses PicDescriptor.FractionDigits to scale
    /// - Right-justified, zero-padded in field
    /// - Sign rendered as leading '-' when IsSigned and value &lt; 0
    /// </summary>
    private static void EncodeDisplay(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        // Clear field to spaces
        for (int i = 0; i < length; i++)
            area[offset + i] = (byte)' ';

        // BLANK WHEN ZERO
        if (pic.BlankWhenZero && value == 0m)
            return;

        bool isNegative = value < 0m;
        decimal absValue = Math.Abs(value);

        // Apply P scaling (inverse of decode):
        // Leading P = additional fraction → multiply to get stored integer
        // Trailing P = additional integer → divide to get stored integer
        if (pic.TrailingScaleDigits > 0)
            absValue /= Pow10(pic.TrailingScaleDigits);

        // Total fraction scale = FractionDigits + LeadingPScaling
        int scale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (scale < 0) scale = 0;

        // Scale to integer: 320.48 with scale=2 → 32048
        decimal scaled = absValue * Pow10(scale);
        long intValue = (long)scaled;

        // Digits-only string
        string digits = intValue.ToString(CultureInfo.InvariantCulture);

        // Determine available width (reserve 1 for separate sign if needed)
        bool separateSign = pic.SignStorage is SignStorageKind.LeadingSeparate
            or SignStorageKind.TrailingSeparate;
        int availableLength = (pic.IsSigned && separateSign) ? length - 1 : length;

        // Truncate from left if too long (SIZE ERROR should be handled separately)
        if (digits.Length > availableLength)
            digits = digits.Substring(digits.Length - availableLength);

        // Right-justify digits
        int digitStart = (pic.IsSigned && separateSign &&
            pic.SignStorage == SignStorageKind.LeadingSeparate) ? 1 : 0;
        int start = digitStart + (availableLength - digits.Length);
        for (int i = 0; i < digits.Length; i++)
            area[offset + start + i] = (byte)digits[i];

        // Zero-fill leading positions (COBOL numeric fields are zero-filled)
        for (int i = digitStart; i < start; i++)
            area[offset + i] = (byte)'0';

        // Sign handling
        if (pic.IsSigned && separateSign)
        {
            int signPos = pic.SignStorage == SignStorageKind.LeadingSeparate ? 0 : length - 1;
            area[offset + signPos] = isNegative ? (byte)'-' : (byte)'+';
        }
        else if (pic.IsSigned && !separateSign)
        {
            // Overpunch: encode sign into the zone nibble of a digit
            int overpunchPos = pic.SignStorage == SignStorageKind.LeadingOverpunch
                ? offset + digitStart        // first digit
                : offset + length - 1;       // last digit (default: trailing)
            byte digit = area[overpunchPos];
            area[overpunchPos] = EncodeOverpunch(digit, isNegative);
        }
    }

    // ── Overpunch tables (IBM ASCII convention) ──
    // Positive: 0→'{', 1→'A', 2→'B', ... 9→'I'
    // Negative: 0→'}', 1→'J', 2→'K', ... 9→'R'
    private static readonly char[] PositiveOverpunch = { '{', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I' };
    private static readonly char[] NegativeOverpunch = { '}', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R' };

    private static byte EncodeOverpunch(byte asciiDigit, bool negative)
    {
        int d = asciiDigit - '0';
        if (d < 0 || d > 9) d = 0;
        return (byte)(negative ? NegativeOverpunch[d] : PositiveOverpunch[d]);
    }

    /// <summary>
    /// Decode an overpunched byte back to a digit (0-9) and sign.
    /// Returns (digit char, isNegative).
    /// </summary>
    private static (char digit, bool negative) DecodeOverpunch(byte b)
    {
        char c = (char)b;
        // Positive: { A B C D E F G H I
        if (c == '{') return ('0', false);
        if (c >= 'A' && c <= 'I') return ((char)('1' + (c - 'A')), false);
        // Negative: } J K L M N O P Q R
        if (c == '}') return ('0', true);
        if (c >= 'J' && c <= 'R') return ((char)('1' + (c - 'J')), true);
        // Plain digit (unsigned or already decoded)
        if (c >= '0' && c <= '9') return (c, false);
        return ('0', false);
    }

    private static void EncodeComp3(byte[] area, int offset, int length, decimal value)
    {
        string s = Math.Abs(value).ToString("F0", CultureInfo.InvariantCulture);
        bool negative = value < 0;
        for (int i = offset; i < offset + length; i++) area[i] = 0;

        int digitCount = s.Length;
        int byteIdx = offset + length - 1;
        int digIdx = digitCount - 1;
        byte sign = (byte)(negative ? 0x0D : 0x0C);
        byte lastDigit = digIdx >= 0 ? (byte)(s[digIdx--] - '0') : (byte)0;
        area[byteIdx--] = (byte)((lastDigit << 4) | sign);
        while (byteIdx >= offset)
        {
            byte lo = digIdx >= 0 ? (byte)(s[digIdx--] - '0') : (byte)0;
            byte hi = digIdx >= 0 ? (byte)(s[digIdx--] - '0') : (byte)0;
            area[byteIdx--] = (byte)((hi << 4) | lo);
        }
    }

    // ══════════════════════════════════════════════════════════
    // Scaling / rounding
    // ══════════════════════════════════════════════════════════

    private static decimal ApplyScalingAndRounding(decimal value, PicDescriptor destPic, int roundingMode)
    {
        // Fraction scale: FractionDigits + leading P (e.g., PIC P(4)9 has scale 5)
        int fractionScale = destPic.FractionDigits + destPic.LeadingScaleDigits;
        if (fractionScale < 0) fractionScale = 0;

        // Trailing P: field stores multiples of 10^TrailingScaleDigits
        // e.g., PIC S99P → TrailingScaleDigits=1 → values are multiples of 10
        int trailingP = destPic.TrailingScaleDigits;

        if (trailingP > 0)
        {
            // Round or truncate to the nearest 10^trailingP
            decimal pFactor = Pow10(trailingP);
            return roundingMode switch
            {
                1 => Math.Round(value / pFactor, 0, MidpointRounding.AwayFromZero) * pFactor,
                _ => decimal.Truncate(value / pFactor) * pFactor
            };
        }

        // Standard fraction rounding
        decimal factor = Pow10(fractionScale);
        return roundingMode switch
        {
            1 => Math.Round(value, fractionScale, MidpointRounding.AwayFromZero),
            _ => decimal.Truncate(value * factor) / factor
        };
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════

    public static string FormatNumericForDisplay(decimal value, int fractionDigits, int totalDigits = 0)
    {
        if (fractionDigits > 0)
        {
            int intDigits = Math.Max(1, totalDigits - fractionDigits);
            string fmt = new string('0', intDigits) + "." + new string('0', fractionDigits);
            return value.ToString(fmt, CultureInfo.InvariantCulture);
        }
        int digits = totalDigits > 0 ? totalDigits : 1;
        return ((long)value).ToString(new string('0', digits), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Check if a value would overflow when encoded into the destination PIC.
    /// Returns true if SIZE ERROR should be raised.
    /// </summary>
    private static bool WouldOverflow(decimal value, PicDescriptor destPic)
    {
        decimal absValue = Math.Abs(value);

        switch (destPic.Usage)
        {
            case UsageKind.Comp:
            case UsageKind.Binary:
            {
                // COBOL spec: COMP overflow is based on PIC digit count, not binary capacity.
                // PIC 99 COMP holds 0-99 (2 digits), not 0-32767 (short.MaxValue).
                decimal scaled = absValue;
                int compScale = destPic.FractionDigits + destPic.LeadingScaleDigits;
                if (compScale > 0)
                    scaled *= Pow10(compScale);
                long raw;
                try { raw = checked((long)decimal.Truncate(scaled)); }
                catch (OverflowException) { return true; }

                int digits = CountDigits(Math.Abs(raw));
                return digits > destPic.TotalDigits;
            }

            case UsageKind.Comp3:
            case UsageKind.PackedDecimal:
            {
                // Include leading P scaling
                decimal scaled = absValue;
                int comp3Scale = destPic.FractionDigits + destPic.LeadingScaleDigits;
                if (comp3Scale > 0)
                    scaled *= Pow10(comp3Scale);
                long intVal;
                try { intVal = checked((long)decimal.Truncate(scaled)); }
                catch (OverflowException) { return true; }

                int digits = CountDigits(Math.Abs(intVal));
                int capacity = (destPic.StorageLength * 2) - 1;
                return digits > capacity;
            }

            default: // DISPLAY
            {
                decimal scaled = absValue;
                int totalScale = destPic.FractionDigits + destPic.LeadingScaleDigits;
                if (totalScale > 0)
                    scaled *= Pow10(totalScale);
                if (destPic.TrailingScaleDigits > 0)
                    scaled /= Pow10(destPic.TrailingScaleDigits);
                long intVal;
                try { intVal = checked((long)decimal.Truncate(scaled)); }
                catch (OverflowException) { return true; }

                // Count digits without floating-point conversion to avoid
                // precision loss for large values (e.g., 999999999999998765
                // rounds to 1.0E+18 as double, giving wrong digit count)
                int digits = CountDigits(Math.Abs(intVal));
                return digits > destPic.TotalDigits;
            }
        }
    }

    /// <summary>
    /// Count decimal digits in a non-negative long without floating-point conversion.
    /// Avoids precision loss that occurs with Math.Log10((double)value) for large values.
    /// </summary>
    private static int CountDigits(long value)
    {
        if (value == 0) return 1;
        int count = 0;
        while (value > 0)
        {
            count++;
            value /= 10;
        }
        return count;
    }

    private static decimal Pow10(int scale)
    {
        decimal result = 1m;
        for (int i = 0; i < scale; i++)
            result *= 10m;
        return result;
    }

    /// <summary>
    /// Returns the display-format string for a PIC field stored in a byte array.
    /// For numeric fields, decodes and formats with leading zeros.
    /// For alphanumeric fields, returns raw bytes as a string with trailing spaces trimmed.
    /// </summary>
    public static string GetDisplayString(
        byte[] area, int offset, int length, PicDescriptor pic)
    {
        if (pic.Category == CobolCategory.NumericEdited)
        {
            // Numeric-edited fields are already formatted — return raw bytes
            return Encoding.ASCII.GetString(area, offset, length).TrimEnd();
        }
        if (pic.Category == CobolCategory.Numeric && pic.Usage == UsageKind.Display)
        {
            // DISPLAY numeric: show the raw field content (preserves sign format)
            return Encoding.ASCII.GetString(area, offset, length).TrimEnd();
        }
        if (pic.Category.IsNumericLike())
        {
            decimal value = DecodeNumeric(area, offset, length, pic);
            return FormatNumericForDisplay(value, pic.FractionDigits, pic.TotalDigits);
        }
        // Alphanumeric / edited: return raw bytes as string, trim trailing spaces
        return Encoding.ASCII.GetString(area, offset, length).TrimEnd();
    }

    private static void MoveStringToBytes(byte[] area, int offset, int length, string value)
    {
        int copyLen = Math.Min(value.Length, length);
        for (int i = 0; i < length; i++)
            area[offset + i] = i < copyLen ? (byte)value[i] : (byte)' ';
    }
}
