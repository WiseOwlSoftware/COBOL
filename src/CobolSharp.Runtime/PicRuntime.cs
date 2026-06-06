// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using CobolSharp.Runtime.Numeric;

namespace CobolSharp.Runtime;

/// <summary>
/// Status returned by arithmetic operations (for ON SIZE ERROR).
/// </summary>
public struct ArithmeticStatus
{
    public bool SizeError { get; set; }
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

        // COMP-1/COMP-2 floating-point destinations: no fixed-point scaling.
        if (dstPic.Usage is not (UsageKind.Comp1 or UsageKind.Comp2))
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
        if (pic.BlankWhenZero && value == 0m)
            return new string(' ', pic.StorageLength);

        if (pic.EditPattern != null)
            return FormatByEditPattern(value, pic);

        bool negative = value < 0m;
        decimal absValue = Math.Abs(value);

        int scale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (scale < 0) scale = 0;

        decimal scaled = absValue * Pow10(scale);
        string digits = decimal.Truncate(scaled).ToString("F0", CultureInfo.InvariantCulture);

        if (digits.Length < pic.TotalDigits)
            digits = digits.PadLeft(pic.TotalDigits, '0');
        else if (digits.Length > pic.TotalDigits)
            digits = digits[^pic.TotalDigits..];

        // Split digits into integer and fraction parts
        int intDigits = pic.TotalDigits - pic.FractionDigits;
        string intPart = digits[..intDigits];
        string fracPart = pic.FractionDigits > 0 ? digits[intDigits..] : "";

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

        // Decimal point (respects DECIMAL-POINT IS COMMA)
        char decimalChar = pic.Environment.DecimalPointIsComma ? ',' : '.';
        if (hasDecimalPoint && pos < chars.Length)
            chars[pos++] = decimalChar;

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
                // Place currency symbol before first non-space digit
                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] != ' ') { chars[i] = pic.Environment.CurrencyOutputChar; break; }
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
        var env = pic.Environment;
        char currencyChar = char.ToUpperInvariant(env.CurrencySign);
        bool decimalPointIsComma = env.DecimalPointIsComma;

        // Pre-scan: count sign and currency symbols to distinguish fixed vs floating.
        int plusCount = 0, minusCount = 0, currencyPrescan = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == '+') plusCount++;
            else if (p == '-') minusCount++;
            else if (p == currencyChar) currencyPrescan++;
        }

        bool isFixedMinus = (minusCount == 1 && plusCount == 0);
        bool isFixedPlus = (plusCount == 1 && minusCount == 0);
        bool isFixedCurrency = (currencyPrescan == 1);

        // Count TRUE digit positions: 9, Z, *, plus floating $, +, -.
        // Fixed $, +, - are NOT digit positions.
        int trueDigitCount = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == '9' || p == 'Z' || p == '*') trueDigitCount++;
            else if (p == currencyChar && !isFixedCurrency) trueDigitCount++;
            else if (p == '+' && !isFixedPlus) trueDigitCount++;
            else if (p == '-' && !isFixedMinus) trueDigitCount++;
        }

        // Floating symbols reserve one position for the symbol itself.
        // Effective digit capacity = trueDigitCount - 1 when floating.
        bool hasFloating = (currencyPrescan > 1) || (plusCount > 1) || (minusCount > 1);
        int effectiveDigitCount = hasFloating ? trueDigitCount - 1 : trueDigitCount;

        // Apply trailing P scaling (same as EncodeDisplay): divide by 10^trailingP
        if (pic.TrailingScaleDigits > 0)
            absValue /= Pow10(pic.TrailingScaleDigits);

        // Build digit string based on effective digit count
        int scale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (scale < 0) scale = 0;
        decimal scaled = absValue * Pow10(scale);
        string digits = decimal.Truncate(scaled).ToString("F0", CultureInfo.InvariantCulture);
        if (digits.Length < effectiveDigitCount)
            digits = digits.PadLeft(effectiveDigitCount, '0');
        else if (digits.Length > effectiveDigitCount)
            digits = digits[^effectiveDigitCount..];

        // Pass 1: Fill digit positions right-to-left, place insertion/fixed chars
        var output = new char[pattern.Length];
        int digitIdx = digits.Length - 1;

        for (int i = pattern.Length - 1; i >= 0; i--)
        {
            char p = char.ToUpperInvariant(pattern[i]);

            // Currency character (env-dependent, checked before switch)
            if (p == currencyChar)
            {
                if (isFixedCurrency)
                    output[i] = env.CurrencyOutputChar;
                else
                    output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                continue;
            }

            switch (p)
            {
                case '9':
                case 'Z':
                case '*':
                    // Always a digit position
                    output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;

                case '+':
                    if (isFixedPlus)
                    {
                        // Fixed sign: show +/-
                        output[i] = negative ? '-' : '+';
                    }
                    else
                    {
                        // Floating sign: acts as digit position
                        output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                    }
                    break;

                case '-':
                    if (isFixedMinus)
                    {
                        // Fixed sign: show - or space
                        output[i] = negative ? '-' : ' ';
                    }
                    else
                    {
                        // Floating sign: acts as digit position
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

        // Pass 2: Left-to-right zero suppression for floating symbols (Z, *, +, -, $).
        // Stops at fixed digit positions (9) or decimal point (.).
        bool suppressing = true;
        // Asterisk fill: detect from pattern (not just suppression pass — * may appear after decimal)
        bool asteriskFill = pattern.Contains('*', StringComparison.OrdinalIgnoreCase);
        bool allIntegerSuppressed = true;
        for (int i = 0; i < pattern.Length && suppressing; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            switch (p)
            {
                case 'Z':
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;

                case '*':
                    asteriskFill = true;
                    if (output[i] == '0') output[i] = '*';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;

                case '+':
                    if (isFixedPlus) break;
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;

                case '-':
                    if (isFixedMinus) break;
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;

                default:
                    if (p == currencyChar && !isFixedCurrency)
                    {
                        if (output[i] == '0') output[i] = ' ';
                        else { suppressing = false; allIntegerSuppressed = false; }
                    }
                    break;

                case ',':
                case 'B':
                    output[i] = asteriskFill ? '*' : ' ';
                    break;

                case '.':
                    suppressing = false;
                    break;

                case '9':
                    suppressing = false;
                    allIntegerSuppressed = false; // fixed 9 in integer → no full-field blanking
                    break;
            }
        }

        // Post-pass: if entire integer part was suppressed AND value is zero AND
        // the field has no fixed '9' positions anywhere (entire field is floating),
        // suppress the fraction too. Insertion chars (.) stay in asterisk-fill mode.
        bool hasFixed9 = pattern.Contains('9');
        bool fullFieldBlanked = false;
        if (allIntegerSuppressed && value == 0m && !hasFixed9)
        {
            fullFieldBlanked = true;
            for (int i = 0; i < output.Length; i++)
            {
                char p = char.ToUpperInvariant(pattern[i]);
                if (asteriskFill)
                {
                    // Asterisk fill: replace digit positions with *, keep . as .
                    if (p == '.' || p == ',')
                        output[i] = p == '.' ? '.' : '*';
                    else
                        output[i] = '*';
                }
                else
                {
                    output[i] = ' ';
                }
            }
        }

        // Skip floating symbol placement when the entire field was blanked to spaces
        // (value is zero and all positions suppressed). Floating symbols only make sense
        // when there's a non-zero value to display.
        if (fullFieldBlanked && !asteriskFill)
            return new string(output);

        // Handle floating symbols: place at rightmost suppressed position in the floating zone.
        // The floating zone includes the symbol positions AND any insertion chars (,/B)
        // between the last floating symbol and the first non-floating digit position.
        bool hasFloatingPlus = plusCount > 0 && (plusCount + minusCount) > 1;
        bool hasFloatingMinus = minusCount > 0 && (plusCount + minusCount) > 1;

        if (hasFloatingPlus)
        {
            int signPos = FindFloatingPlacement(pattern, output, '+');
            if (signPos >= 0)
                output[signPos] = negative ? '-' : '+';
        }
        else if (hasFloatingMinus)
        {
            int signPos = FindFloatingPlacement(pattern, output, '-');
            if (signPos >= 0)
                output[signPos] = negative ? '-' : ' ';
        }

        // Handle floating currency: place symbol at rightmost suppressed position.
        if (currencyPrescan > 1)
        {
            int currencyPos = FindFloatingPlacement(pattern, output, currencyChar);
            if (currencyPos >= 0)
                output[currencyPos] = env.CurrencyOutputChar;
        }

        return new string(output);
    }

    /// <summary>
    /// Returns true if all digit characters in the output from startIndex onward are '0'.
    /// Used to decide whether the decimal point and fraction can be suppressed when
    /// the entire integer part was already suppressed.
    /// </summary>
    private static bool AllFractionZero(char[] output, int startIndex, string pattern)
    {
        for (int i = startIndex; i < output.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            // Only check actual digit positions (9, Z, *, +, -, currency)
            if (p == '9' || p == 'Z' || p == '*' || p == '+' || p == '-')
            {
                if (output[i] != '0') return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Find the rightmost suppressed position within the floating zone for a floating symbol.
    /// The floating zone includes the symbol's own positions AND any insertion chars
    /// (comma, B) that appear between the last floating symbol and the first digit.
    /// </summary>
    private static int FindFloatingPlacement(string pattern, char[] output, char floatChar)
    {
        char floatUpper = char.ToUpperInvariant(floatChar);
        int lastSuppressed = -1;
        bool inFloatingZone = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == floatUpper)
            {
                inFloatingZone = true;
                if (output[i] == ' ')
                    lastSuppressed = i;
                else
                    break; // hit a non-suppressed floating symbol → zone ends
            }
            else if (inFloatingZone && (p == ',' || p == 'B') && output[i] == ' ')
            {
                // Suppressed insertion char within the floating zone
                lastSuppressed = i;
            }
            else if (inFloatingZone)
            {
                break; // hit a non-floating char → zone ends
            }
        }
        return lastSuppressed;
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
        // Convert numeric to display representation (same as MoveNumericToAlphanumeric)
        decimal value = DecodeNumeric(srcArea, srcOffset, srcLength, srcPic);
        value = Math.Abs(value);
        int fractionScale = srcPic.FractionDigits + srcPic.LeadingScaleDigits;
        string formatted = FormatNumericForDisplay(value, fractionScale, srcPic.TotalDigits);

        // Write display string to a temporary buffer, then apply alphanumeric edit pattern
        byte[] tempArea = new byte[formatted.Length];
        for (int i = 0; i < formatted.Length; i++)
            tempArea[i] = (byte)formatted[i];

        var tempPic = new PicDescriptor(0, 0, false, false, true, false,
            formatted.Length, UsageKind.Display, CobolCategory.Alphanumeric,
            SignStorageKind.None, EditingKind.None, false, 0, 0, null);

        MoveAlphanumericToAlphanumericEdited(tempArea, 0, formatted.Length, tempPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: Alphanumeric → …
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// MOVE string literal TO numeric field. Converts the string to a byte buffer
    /// and delegates to MoveAlphanumericToNumeric for proper right-justified digit extraction.
    /// </summary>
    public static void MoveStringLiteralToNumeric(
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        string value)
    {
        byte[] srcBuf = Encoding.ASCII.GetBytes(value);
        var srcPic = new PicDescriptor(0, 0, false, false, true, false,
            srcBuf.Length, UsageKind.Display, CobolCategory.Alphanumeric,
            SignStorageKind.None, EditingKind.None, false, 0, 0, null);
        MoveAlphanumericToNumeric(srcBuf, 0, srcBuf.Length, srcPic,
            dstArea, dstOffset, dstLength, dstPic, 0);
    }

    public static void MoveAlphanumericToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        if (dstPic.IsJustifiedRight)
        {
            // ISO §13.16.35: JUSTIFIED RIGHT — right-justify receiving field.
            // When source > target: truncate from the LEFT (keep rightmost chars).
            // When source < target: pad on the LEFT with spaces.
            if (srcLength > dstLength)
            {
                int skipLeft = srcLength - dstLength;
                Array.Copy(srcArea, srcOffset + skipLeft, dstArea, dstOffset, dstLength);
            }
            else
            {
                int pad = dstLength - srcLength;
                for (int i = 0; i < pad; i++)
                    dstArea[dstOffset + i] = (byte)' ';
                Array.Copy(srcArea, srcOffset, dstArea, dstOffset + pad, srcLength);
            }
        }
        else
        {
            // Left-justified, space-padded
            int copyLen = Math.Min(srcLength, dstLength);
            Array.Copy(srcArea, srcOffset, dstArea, dstOffset, copyLen);
            for (int i = copyLen; i < dstLength; i++)
                dstArea[dstOffset + i] = (byte)' ';
        }
    }

    public static void MoveAlphanumericToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        if (dstPic.EditPattern == null)
        {
            // No edit pattern — fall back to plain alphanumeric move
            MoveAlphanumericToAlphanumeric(srcArea, srcOffset, srcLength, srcPic,
                dstArea, dstOffset, dstLength, dstPic, roundingMode);
            return;
        }

        // Apply alphanumeric edit pattern:
        // A = takes next input character (alphabetic position)
        // X = takes next input character (any character position)
        // B = inserts space
        // 0 = inserts zero
        // / = inserts slash
        string pattern = dstPic.EditPattern;
        int srcIdx = 0;
        for (int i = 0; i < pattern.Length && i < dstLength; i++)
        {
            char editChar = pattern[i];
            switch (editChar)
            {
                case 'A':
                case 'X':
                    // Data position — take next source character
                    if (srcIdx < srcLength)
                        dstArea[dstOffset + i] = srcArea[srcOffset + srcIdx++];
                    else
                        dstArea[dstOffset + i] = (byte)' ';
                    break;
                case 'B':
                    // Insert space
                    dstArea[dstOffset + i] = (byte)' ';
                    break;
                case '0':
                    // Insert zero
                    dstArea[dstOffset + i] = (byte)'0';
                    break;
                case '/':
                    // Insert slash
                    dstArea[dstOffset + i] = (byte)'/';
                    break;
                default:
                    // Unknown edit character — treat as data position
                    if (srcIdx < srcLength)
                        dstArea[dstOffset + i] = srcArea[srcOffset + srcIdx++];
                    else
                        dstArea[dstOffset + i] = (byte)' ';
                    break;
            }
        }
        // Pad remaining destination with spaces
        for (int i = pattern.Length; i < dstLength; i++)
            dstArea[dstOffset + i] = (byte)' ';
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

        // Detect sign from CR/DB suffixes and leading/trailing minus before stripping
        bool negative = raw.Contains('-') ||
                         raw.Contains("CR", StringComparison.OrdinalIgnoreCase) ||
                         raw.Contains("DB", StringComparison.OrdinalIgnoreCase);

        raw = raw.Replace(",", "").Replace(srcPic.Environment.CurrencyOutputChar.ToString(), "")
                 .Replace("CR", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("DB", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("*", "").Replace("/", "").Replace(" ", "")
                 .Replace("-", "").Replace("+", "").Trim();

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint,
                              CultureInfo.InvariantCulture, out var value))
        {
            value = 0m;
        }

        if (negative) value = -value;

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

        // Detect sign from CR/DB suffixes and leading/trailing minus before stripping
        bool negative = raw.Contains('-') ||
                         raw.Contains("CR", StringComparison.OrdinalIgnoreCase) ||
                         raw.Contains("DB", StringComparison.OrdinalIgnoreCase);

        raw = raw.Replace(",", "").Replace(srcPic.Environment.CurrencyOutputChar.ToString(), "")
                 .Replace("CR", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("DB", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("*", "").Replace("/", "").Replace(" ", "")
                 .Replace("-", "").Replace("+", "").Trim();

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

        // Numeric-edited destination with ZERO: format 0 through the edit pattern
        if (dstPic.Category == CobolCategory.NumericEdited && kind == FigurativeKind.Zero)
        {
            string formatted = FormatNumericEdited(0m, dstPic);
            MoveStringToBytes(dstArea, dstOffset, dstLength, formatted);
            return;
        }

        // Alphanumeric-edited destination: fill with figurative byte then apply edit pattern
        if (dstPic.Category == CobolCategory.AlphanumericEdited && dstPic.EditPattern != null)
        {
            byte figurativeByte = FigurativeToByte(kind);

            // Create a source buffer filled with the figurative byte
            byte[] srcBuf = new byte[dstLength];
            for (int i = 0; i < srcBuf.Length; i++)
                srcBuf[i] = figurativeByte;

            var dummyPic = new PicDescriptor();
            MoveAlphanumericToAlphanumericEdited(
                srcBuf, 0, srcBuf.Length, dummyPic,
                dstArea, dstOffset, dstLength, dstPic, 0);
            return;
        }

        // Plain numeric destination with ZERO: encode numeric zero
        if (dstPic.IsNumeric && kind == FigurativeKind.Zero)
        {
            EncodeNumeric(dstArea, dstOffset, dstLength, dstPic, 0m);
            return;
        }

        // National destination: fill each character position with the UTF-16 form of the figurative
        // character (national SPACE = U+0020, etc.; ISO §8.1.2 rule 7). Single-byte fill would corrupt.
        if (dstPic.Category.IsNationalLike())
        {
            int code = kind switch
            {
                FigurativeKind.Zero => '0',
                FigurativeKind.Space => ' ',
                FigurativeKind.Quote => '"',
                FigurativeKind.HighValue => 0xFFFF,
                FigurativeKind.LowValue => 0x0000,
                FigurativeKind.Null => 0x0000,
                _ => ' '
            };
            byte lo = (byte)(code & 0xFF), hi = (byte)((code >> 8) & 0xFF);
            for (int i = 0; i + 1 < dstLength; i += 2)
            {
                dstArea[dstOffset + i] = lo;
                dstArea[dstOffset + i + 1] = hi;
            }
            return;
        }

        // Default: fill entire field with figurative byte
        byte b = FigurativeToByte(kind);
        for (int i = 0; i < dstLength; i++)
            dstArea[dstOffset + i] = b;
    }

    private static byte FigurativeToByte(FigurativeKind kind) => kind switch
    {
        FigurativeKind.Zero => (byte)'0',
        FigurativeKind.Space => (byte)' ',
        FigurativeKind.HighValue => 0xFF,
        FigurativeKind.LowValue => 0x00,
        FigurativeKind.Quote => (byte)'"',
        FigurativeKind.Null => 0x00,
        _ => (byte)' '
    };

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
        // COMP-1/COMP-2 floating-point fields: no scaling — store the value directly.
        decimal value = destPic.Usage is UsageKind.Comp1 or UsageKind.Comp2
            ? literal
            : ApplyScalingAndRounding(literal, destPic, roundingMode);

        // Numeric-edited targets: format using edit pattern, not raw encode
        if (destPic.Category == CobolCategory.NumericEdited)
        {
            string formatted = FormatNumericEdited(value, destPic);
            // Write formatted string to destination
            for (int i = 0; i < destLength; i++)
                destArea[destOffset + i] = i < formatted.Length ? (byte)formatted[i] : (byte)' ';
            return;
        }

        EncodeNumeric(destArea, destOffset, destLength, destPic, value);
    }

    // ComputeAndStore removed — MoveAccumulatedToField provides the identical
    // "store decimal with overflow detection" behavior for all arithmetic paths.

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
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
    }

    public static void MultiplyNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal,
        byte[] otherArea, int otherOffset, int otherLength, PicDescriptor otherPic,
        int roundingMode, ref ArithmeticStatus status)
    {
        decimal other = DecodeNumeric(otherArea, otherOffset, otherLength, otherPic);
        decimal value = literal * other;
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
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
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
    }

    public static void AddNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest + literal;
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
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
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
    }

    public static void SubtractNumericLiteral(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal literal, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest - literal;
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
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
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
    }

    /// <summary>
    /// GIVING form: store accumulated value directly into target (target = accumulated).
    /// Does NOT add to the target's current value.
    /// </summary>
    public static void MoveAccumulatedToField(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal accumulated, int roundingMode, ref ArithmeticStatus status)
    {
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, accumulated, roundingMode, ref status);
    }

    public static void SubtractAccumulatedFromField(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal accumulated, int roundingMode, ref ArithmeticStatus status)
    {
        decimal dest = DecodeNumeric(destArea, destOffset, destLength, destPic);
        decimal value = dest - accumulated;
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
    }

    /// <summary>
    /// Unified arithmetic result storage: scale, check overflow, encode.
    /// Routes numeric-edited targets through FormatNumericEdited.
    /// All arithmetic operations (ADD/SUB/MUL/DIV/COMPUTE GIVING) converge here.
    /// </summary>
    private static void StoreArithmeticResult(
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        decimal value, int roundingMode, ref ArithmeticStatus status)
    {
        // COMP-1/COMP-2 floating-point receivers: no fixed-point scaling/overflow — encode the full IEEE value
        // directly (mirrors the MOVE-to-float guard in MoveNumericToNumeric).
        if (destPic.Usage is UsageKind.Comp1 or UsageKind.Comp2)
        {
            EncodeNumeric(destArea, destOffset, destLength, destPic, value);
            return;
        }

        // Data-model migration Stage 1 (docs/DATA_MODEL_ARCHITECTURE.md §10): the value-level
        // scale → round → capacity → SIZE ERROR decision is delegated to CobolNum, the typed BigInteger
        // numeric core, proven byte-identical to the legacy ApplyScalingAndRounding + WouldOverflow +
        // ROUNDED MODE PROHIBITED path by CobolNumDifferentialTests. The byte encode/decode stays here.
        // TryStore returns false — the ON SIZE ERROR condition (ISO §14.9.4: capacity exceeded, or PROHIBITED
        // with an inexact result) — and leaves the receiver unchanged.
        if (!CobolNum.TryStore(CobolDecimal.FromDecimal(value), NumProfile.FromDescriptor(destPic),
                               (CobolRounding)roundingMode, out CobolDecimal stored))
        {
            status.SizeError = true;
            return;
        }
        value = stored.ToDecimal();

        if (destPic.Category == CobolCategory.NumericEdited)
        {
            string formatted = FormatNumericEdited(value, destPic);
            MoveStringToBytes(destArea, destOffset, destLength, formatted);
        }
        else
        {
            EncodeNumeric(destArea, destOffset, destLength, destPic, value);
        }
    }

    /// <summary>
    /// Safe decimal division for COMPUTE/GIVING expression evaluation.
    /// Returns 0 and sets SizeError on divide-by-zero instead of throwing.
    /// Called from CIL-emitted expression trees where decimal.op_Division
    /// would throw DivideByZeroException before ON SIZE ERROR can fire.
    /// </summary>
    public static decimal SafeDivide(decimal left, decimal right, ref ArithmeticStatus status)
    {
        if (right == 0m)
        {
            status.SizeError = true;
            return 0m;
        }
        return left / right;
    }

    public static decimal SafeRemainder(decimal left, decimal right, ref ArithmeticStatus status)
    {
        if (right == 0m)
        {
            status.SizeError = true;
            return 0m;
        }
        return decimal.Remainder(left, right);
    }

    /// <summary>
    /// COBOL DIVIDE REMAINDER: R = dividend - truncatedQuotient × divisor.
    /// The quotient is truncated to the GIVING field's precision (fractionDigits)
    /// per COBOL-85 §14.9.11 GR4. This differs from mathematical modulo which uses
    /// the exact quotient.
    /// </summary>
    public static void ComputeCobolRemainder(
        decimal dividend, decimal divisor, decimal rawQuotient,
        int givingFractionDigits,
        byte[] destArea, int destOffset, int destLength, PicDescriptor destPic,
        ref ArithmeticStatus status)
    {
        if (divisor == 0m)
        {
            status.SizeError = true;
            return;
        }

        // Truncate quotient to the GIVING field's precision (no rounding)
        decimal truncatedQuotient = rawQuotient;
        if (givingFractionDigits >= 0)
        {
            decimal scale = Pow10(givingFractionDigits);
            truncatedQuotient = decimal.Truncate(rawQuotient * scale) / scale;
        }

        decimal remainder = dividend - truncatedQuotient * divisor;
        remainder = ApplyScalingAndRounding(remainder, destPic, 0);

        // Numeric edited destinations: format with edit pattern, not raw encode
        if (destPic.Category == CobolCategory.NumericEdited)
        {
            string formatted = FormatNumericEdited(remainder, destPic);
            MoveStringToBytes(destArea, destOffset, destLength, formatted);
        }
        else
        {
            EncodeNumeric(destArea, destOffset, destLength, destPic, remainder);
        }
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
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
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
        StoreArithmeticResult(destArea, destOffset, destLength, destPic, value, roundingMode, ref status);
    }

    // ══════════════════════════════════════════════════════════
    // COMPARE: Numeric
    // ══════════════════════════════════════════════════════════

    /// <summary>Returns -1, 0, or 1.</summary>
    public static int CompareNumeric(
        byte[] leftArea, int leftOffset, int leftLength, PicDescriptor leftPic,
        byte[] rightArea, int rightOffset, int rightLength, PicDescriptor rightPic)
    {
        // Mixed numeric-vs-alphanumeric: COBOL-85 pseudo-MOVE comparison.
        // The numeric operand is treated as if moved to an alphanumeric field
        // (sign stripped, formatted as unsigned DISPLAY), then compared as strings.
        bool leftIsNumeric = leftPic.Category == CobolCategory.Numeric;
        bool rightIsNumeric = rightPic.Category == CobolCategory.Numeric;

        if (leftIsNumeric && !rightIsNumeric)
        {
            // Left is numeric, right is alphanumeric — pseudo-MOVE left
            decimal val = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
            string unsigned = FormatNumericForDisplay(Math.Abs(val), leftPic.FractionDigits, leftPic.TotalDigits);
            string rightStr = System.Text.Encoding.ASCII.GetString(rightArea, rightOffset, rightLength).TrimEnd();
            return string.Compare(unsigned, rightStr, StringComparison.Ordinal);
        }

        if (!leftIsNumeric && rightIsNumeric)
        {
            // Right is numeric, left is alphanumeric — pseudo-MOVE right
            decimal val = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
            string unsigned = FormatNumericForDisplay(Math.Abs(val), rightPic.FractionDigits, rightPic.TotalDigits);
            string leftStr = System.Text.Encoding.ASCII.GetString(leftArea, leftOffset, leftLength).TrimEnd();
            return string.Compare(leftStr, unsigned, StringComparison.Ordinal);
        }

        // Both numeric — standard numeric comparison
        decimal leftVal = DecodeNumeric(leftArea, leftOffset, leftLength, leftPic);
        decimal rightVal = DecodeNumeric(rightArea, rightOffset, rightLength, rightPic);
        return Math.Sign(leftVal - rightVal);
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

    /// <summary>
    /// National comparison (ISO §8.8.4.1.2). Operands are UTF-16LE; the shorter is extended on the right
    /// with national spaces (U+0020). Compares whole character positions (code units), not bytes — so the
    /// ordering is correct for code points ≥ U+0100, unlike a little-endian byte-wise compare. Returns -1/0/1.
    /// </summary>
    public static int CompareNational(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        int leftChars = leftLength / 2;
        int rightChars = rightLength / 2;
        int maxChars = Math.Max(leftChars, rightChars);
        for (int i = 0; i < maxChars; i++)
        {
            int lc = i < leftChars
                ? leftArea[leftOffset + i * 2] | (leftArea[leftOffset + i * 2 + 1] << 8)
                : ' ';
            int rc = i < rightChars
                ? rightArea[rightOffset + i * 2] | (rightArea[rightOffset + i * 2 + 1] << 8)
                : ' ';
            if (lc < rc) return -1;
            if (lc > rc) return 1;
        }
        return 0;
    }

    /// <summary>
    /// Compare a national field (UTF-16LE) against a (decoded) literal string, character by character;
    /// the shorter side is extended on the right with national spaces (U+0020). Returns -1/0/1. Used for
    /// `IF national-item = N"…"` (and the ASCII-subset `= "…"`). Collating-sequence national compare deferred.
    /// </summary>
    public static int CompareNationalToString(byte[] area, int offset, int length, string value)
    {
        int fieldChars = length / 2;
        int maxChars = Math.Max(fieldChars, value.Length);
        for (int i = 0; i < maxChars; i++)
        {
            int fc = i < fieldChars
                ? area[offset + i * 2] | (area[offset + i * 2 + 1] << 8)
                : ' ';
            int vc = i < value.Length ? value[i] : ' ';
            if (fc < vc) return -1;
            if (fc > vc) return 1;
        }
        return 0;
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: NumericEdited → NumericEdited
    // ══════════════════════════════════════════════════════════

    public static void MoveNumericEditedToNumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        // De-edit source to get numeric value, then re-edit into destination format.
        // Strip editing characters (commas, currency, CR/DB, etc.) and parse to decimal.
        string raw = Encoding.ASCII.GetString(srcArea, srcOffset, srcLength).Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            string zeroFormatted = FormatNumericEdited(0m, dstPic);
            MoveStringToBytes(dstArea, dstOffset, dstLength, zeroFormatted);
            return;
        }

        bool negative = raw.Contains('-') ||
                         raw.Contains("CR", StringComparison.OrdinalIgnoreCase) ||
                         raw.Contains("DB", StringComparison.OrdinalIgnoreCase);

        raw = raw.Replace(",", "").Replace(srcPic.Environment.CurrencyOutputChar.ToString(), "")
                 .Replace("CR", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("DB", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("*", "").Replace("/", "").Replace(" ", "")
                 .Replace("-", "").Replace("+", "").Trim();

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint,
                              CultureInfo.InvariantCulture, out var value))
        {
            value = 0m;
        }

        if (negative) value = -value;

        value = ApplyScalingAndRounding(value, dstPic, roundingMode);
        string formatted = FormatNumericEdited(value, dstPic);
        MoveStringToBytes(dstArea, dstOffset, dstLength, formatted);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE: National stubs
    // ══════════════════════════════════════════════════════════

    // National data is stored as UTF-16LE: two bytes per national character position. The national
    // space (figurative SPACE / editing 'B') is U+0020 (ISO §8.1.2 rule 7); 0x20 0x00 little-endian.
    private const byte NationalSpaceLo = 0x20;
    private const byte NationalSpaceHi = 0x00;

    /// <summary>
    /// Copy <paramref name="srcChars"/> UTF-16LE character positions into a national receiver of
    /// <paramref name="dstChars"/> positions: left-justified with national-space fill / right truncation,
    /// or right-justified (pad/truncate on the left) when the receiver is JUSTIFIED RIGHT
    /// (ISO §14.6.8.5, §13.16.35.4). Operates on whole 2-byte units; no decode needed.
    /// </summary>
    private static void WriteNationalChars(
        byte[] src, int srcOff, int srcChars,
        byte[] dst, int dstOff, int dstChars,
        bool justifiedRight)
    {
        if (justifiedRight)
        {
            if (srcChars >= dstChars)
            {
                // Keep the rightmost dstChars character positions of the source.
                Array.Copy(src, srcOff + (srcChars - dstChars) * 2, dst, dstOff, dstChars * 2);
            }
            else
            {
                int pad = dstChars - srcChars;
                for (int i = 0; i < pad; i++)
                {
                    dst[dstOff + i * 2] = NationalSpaceLo;
                    dst[dstOff + i * 2 + 1] = NationalSpaceHi;
                }
                Array.Copy(src, srcOff, dst, dstOff + pad * 2, srcChars * 2);
            }
        }
        else
        {
            int copyChars = Math.Min(srcChars, dstChars);
            Array.Copy(src, srcOff, dst, dstOff, copyChars * 2);
            for (int i = copyChars; i < dstChars; i++)
            {
                dst[dstOff + i * 2] = NationalSpaceLo;
                dst[dstOff + i * 2 + 1] = NationalSpaceHi;
            }
        }
    }

    public static void MoveNationalToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        WriteNationalChars(srcArea, srcOffset, srcLength / 2,
            dstArea, dstOffset, dstLength / 2, dstPic.IsJustifiedRight);
    }

    /// <summary>
    /// MOVE of a national literal (N"…") — or, for the ASCII subset, an alphanumeric literal — into a
    /// national receiver. The literal's characters are encoded UTF-16LE and stored per
    /// <see cref="WriteNationalChars"/> (ISO §14.6.8.5).
    /// </summary>
    public static void MoveStringLiteralToNational(
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic, string value)
    {
        byte[] srcBytes = Encoding.Unicode.GetBytes(value);  // UTF-16LE, 2 bytes per code unit
        WriteNationalChars(srcBytes, 0, value.Length,
            dstArea, dstOffset, dstLength / 2, dstPic.IsJustifiedRight);
    }

    /// <summary>
    /// Initialize every occurrence of a national OCCURS field (or a single national item, occursCount=1)
    /// with the same VALUE literal, encoded UTF-16LE, left-justified with national-space fill. Mirrors
    /// <see cref="StorageHelpers.MoveStringToOccursField"/> for the national category.
    /// </summary>
    public static void MoveNationalLiteralToOccursField(
        byte[] area, int baseOffset, int elementSize, int occursCount, string value)
    {
        byte[] srcBytes = Encoding.Unicode.GetBytes(value);
        int dstChars = elementSize / 2;
        for (int occ = 0; occ < occursCount; occ++)
            WriteNationalChars(srcBytes, 0, value.Length,
                area, baseOffset + occ * elementSize, dstChars, justifiedRight: false);
    }

    // ══════════════════════════════════════════════════════════
    // MOVE / COMPARE: Boolean (PIC 1 / USAGE BIT) — one byte per position ('0'/'1')
    // ══════════════════════════════════════════════════════════

    private const byte BooleanZero = (byte)'0';

    /// <summary>
    /// MOVE boolean ← boolean (ISO §14.6.8.6): left-justified, zero-fill ('0') on the right, truncate on the
    /// right; JUSTIFIED RIGHT pads/truncates on the left. Boolean positions are stored one byte each ('0'/'1').
    /// </summary>
    public static void MoveBooleanToBoolean(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        if (dstPic.IsJustifiedRight)
        {
            if (srcLength > dstLength)
            {
                Array.Copy(srcArea, srcOffset + (srcLength - dstLength), dstArea, dstOffset, dstLength);
            }
            else
            {
                int pad = dstLength - srcLength;
                for (int i = 0; i < pad; i++) dstArea[dstOffset + i] = BooleanZero;
                Array.Copy(srcArea, srcOffset, dstArea, dstOffset + pad, srcLength);
            }
        }
        else
        {
            int copyLen = Math.Min(srcLength, dstLength);
            Array.Copy(srcArea, srcOffset, dstArea, dstOffset, copyLen);
            for (int i = copyLen; i < dstLength; i++) dstArea[dstOffset + i] = BooleanZero;
        }
    }

    /// <summary>MOVE of a boolean literal (B"0101") into a boolean receiver — store the '0'/'1' bytes,
    /// zero-fill / right-truncate per <see cref="MoveBooleanToBoolean"/>.</summary>
    public static void MoveStringLiteralToBoolean(
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic, string value)
    {
        byte[] src = Encoding.ASCII.GetBytes(value);
        MoveBooleanToBoolean(src, 0, src.Length, dstPic, dstArea, dstOffset, dstLength, dstPic, 0);
    }

    /// <summary>Initialize every occurrence of a boolean OCCURS field (or a single boolean item) with the same
    /// VALUE literal's '0'/'1' bytes, zero-filled. Mirrors MoveStringToOccursField for the boolean category.</summary>
    public static void MoveBooleanLiteralToOccursField(
        byte[] area, int baseOffset, int elementSize, int occursCount, string value)
    {
        byte[] src = Encoding.ASCII.GetBytes(value);
        int copyLen = Math.Min(src.Length, elementSize);
        for (int occ = 0; occ < occursCount; occ++)
        {
            int offset = baseOffset + occ * elementSize;
            Array.Copy(src, 0, area, offset, copyLen);
            for (int i = copyLen; i < elementSize; i++) area[offset + i] = BooleanZero;
        }
    }

    /// <summary>Boolean comparison: byte-wise on the '0'/'1' positions; the shorter operand is extended on the
    /// right with '0' (ISO §8.8.4.2). Returns -1/0/1.</summary>
    public static int CompareBoolean(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength)
    {
        int maxLen = Math.Max(leftLength, rightLength);
        for (int i = 0; i < maxLen; i++)
        {
            byte lb = i < leftLength ? leftArea[leftOffset + i] : BooleanZero;
            byte rb = i < rightLength ? rightArea[rightOffset + i] : BooleanZero;
            if (lb < rb) return -1;
            if (lb > rb) return 1;
        }
        return 0;
    }

    /// <summary>Compare a boolean field against a (decoded) literal string; shorter side extended with '0'.</summary>
    public static int CompareBooleanToString(byte[] area, int offset, int length, string value)
    {
        int maxLen = Math.Max(length, value.Length);
        for (int i = 0; i < maxLen; i++)
        {
            int fc = i < length ? area[offset + i] : '0';
            int vc = i < value.Length ? value[i] : '0';
            if (fc < vc) return -1;
            if (fc > vc) return 1;
        }
        return 0;
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

    /// <summary>
    /// Narrow the UTF-16LE national source to one byte per character (the Latin-1 subset; a code point
    /// &gt; U+00FF substitutes '?'). Full implementor correspondence + EC-DATA-CONVERSION are deferred.
    /// Returns a byte buffer of the source's character count.
    /// </summary>
    private static byte[] NarrowNationalToBytes(byte[] srcArea, int srcOffset, int srcLength)
    {
        int srcChars = srcLength / 2;
        byte[] narrow = new byte[srcChars];
        for (int i = 0; i < srcChars; i++)
        {
            int ch = srcArea[srcOffset + i * 2] | (srcArea[srcOffset + i * 2 + 1] << 8);
            narrow[i] = ch <= 0xFF ? (byte)ch : (byte)'?';
        }
        return narrow;
    }

    public static void MoveNationalToAlphanumeric(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        byte[] narrow = NarrowNationalToBytes(srcArea, srcOffset, srcLength);
        MoveAlphanumericToAlphanumeric(narrow, 0, narrow.Length, srcPic,
            dstArea, dstOffset, dstLength, dstPic, roundingMode);
    }

    public static void MoveNationalToAlphanumericEdited(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        byte[] narrow = NarrowNationalToBytes(srcArea, srcOffset, srcLength);
        MoveAlphanumericToAlphanumericEdited(narrow, 0, narrow.Length, srcPic,
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

    /// <summary>
    /// Numeric → national: render the numeric value as its display digit string (sign stripped, per
    /// ISO §14.6.8.5) and store it UTF-16-encoded into the national receiver.
    /// </summary>
    public static void MoveNumericToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        decimal value = Math.Abs(DecodeNumeric(srcArea, srcOffset, srcLength, srcPic));
        int fractionScale = srcPic.FractionDigits + srcPic.LeadingScaleDigits;
        string formatted = FormatNumericForDisplay(value, fractionScale, srcPic.TotalDigits);
        MoveStringLiteralToNational(dstArea, dstOffset, dstLength, dstPic, formatted);
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

    /// <summary>
    /// Widen each one-byte source character to a UTF-16 code unit (the Latin-1 subset correspondence;
    /// the high byte is 0) and store it national-aligned (left-justified, national-space pad / right
    /// truncate, or JUSTIFIED RIGHT) into the national receiver. ISO §14.6.8.5; full implementor
    /// correspondence + EC-DATA-CONVERSION deferred. Also serves NumericEdited/AlphanumericEdited
    /// sources, whose stored bytes are already display characters.
    /// </summary>
    public static void MoveAlphanumericToNational(
        byte[] srcArea, int srcOffset, int srcLength, PicDescriptor srcPic,
        byte[] dstArea, int dstOffset, int dstLength, PicDescriptor dstPic,
        int roundingMode)
    {
        byte[] wide = new byte[srcLength * 2];
        for (int i = 0; i < srcLength; i++)
            wide[i * 2] = srcArea[srcOffset + i];   // high byte stays 0
        WriteNationalChars(wide, 0, srcLength, dstArea, dstOffset, dstLength / 2,
            dstPic.IsJustifiedRight);
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
        RuntimeGuard.Buffer(area, offset, length, "numeric-decode");
        return pic.Usage switch
        {
            UsageKind.Display => DecodeDisplay(area, offset, length, pic),
            UsageKind.Comp3 or UsageKind.PackedDecimal => DecodeComp3(area, offset, length, pic),
            UsageKind.Comp or UsageKind.Binary => DecodeCompBinary(area, offset, length, pic),
            UsageKind.Comp5 => DecodeComp5(area, offset, length, pic),
            UsageKind.Comp1 => DecodeComp1(area, offset),
            UsageKind.Comp2 => DecodeComp2(area, offset),
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

    private static decimal DecodeComp3(byte[] area, int offset, int length, PicDescriptor pic)
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

        decimal result = negative ? -intPart : intPart;

        // Apply implied decimal + leading P scaling (same as COMP/BINARY)
        int totalFractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (totalFractionScale > 0)
            result /= Pow10(totalFractionScale);
        if (pic.TrailingScaleDigits > 0)
            result *= Pow10(pic.TrailingScaleDigits);

        return result;
    }

    /// <summary>
    /// COMP-1 decoding: read 4 bytes as IEEE 754 single-precision float, return as decimal.
    /// </summary>
    private static decimal DecodeComp1(byte[] area, int offset)
    {
        float value = BitConverter.ToSingle(area, offset);
        return (decimal)value;
    }

    /// <summary>
    /// COMP-2 decoding: read 8 bytes as IEEE 754 double-precision float, return as decimal.
    /// </summary>
    private static decimal DecodeComp2(byte[] area, int offset)
    {
        double value = BitConverter.ToDouble(area, offset);
        return (decimal)value;
    }

    /// <summary>
    /// COMP-1 encoding: convert decimal to IEEE 754 single-precision float, write 4 bytes.
    /// </summary>
    private static void EncodeComp1(byte[] area, int offset, decimal value)
    {
        float f = (float)value;
        byte[] bytes = BitConverter.GetBytes(f);
        Array.Copy(bytes, 0, area, offset, 4);
    }

    /// <summary>
    /// COMP-2 encoding: convert decimal to IEEE 754 double-precision float, write 8 bytes.
    /// </summary>
    private static void EncodeComp2(byte[] area, int offset, decimal value)
    {
        double d = (double)value;
        byte[] bytes = BitConverter.GetBytes(d);
        Array.Copy(bytes, 0, area, offset, 8);
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
    /// COMP-5 decoding: 2/4/8-byte native-endian (little-endian on .NET) integer.
    /// Uses full binary capacity — PIC digit count does not constrain the value.
    /// Unsigned PICs use unsigned reads to access the full positive range.
    /// </summary>
    private static decimal DecodeComp5(byte[] area, int offset, int length, PicDescriptor pic)
    {
        long raw = length switch
        {
            // 1-byte form (BINARY-CHAR): no PIC'd COMP-5 is ever 1 byte, but the COBOL-2002 fixed-width
            // BINARY-CHAR usage is. Signed → sbyte two's-complement (-128..127); unsigned → 0..255.
            1 => pic.IsSigned ? (sbyte)area[offset] : area[offset],
            2 => pic.IsSigned
                ? BinaryPrimitives.ReadInt16LittleEndian(area.AsSpan(offset, 2))
                : (long)BinaryPrimitives.ReadUInt16LittleEndian(area.AsSpan(offset, 2)),
            4 => pic.IsSigned
                ? BinaryPrimitives.ReadInt32LittleEndian(area.AsSpan(offset, 4))
                : (long)BinaryPrimitives.ReadUInt32LittleEndian(area.AsSpan(offset, 4)),
            8 => BinaryPrimitives.ReadInt64LittleEndian(area.AsSpan(offset, 8)),
            _ => 0
        };

        decimal result = raw;

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

        // COBOL truncation: by PIC digit count, not by binary capacity.
        // PIC 9 COMP → 1 digit → mod 10; PIC S999 COMP → 3 digits → mod 1000.
        if (pic.TotalDigits > 0 && pic.TotalDigits < 18)
        {
            long modBase = (long)Pow10(pic.TotalDigits);
            raw = raw % modBase;
        }

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

    /// <summary>
    /// COMP-5 encoding: decimal → 2/4/8-byte native-endian (little-endian) integer.
    /// No PIC-based truncation — value truncates only at binary capacity.
    /// </summary>
    private static void EncodeComp5(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        decimal scaled = value;
        int totalFractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (totalFractionScale > 0)
            scaled *= Pow10(totalFractionScale);
        if (pic.TrailingScaleDigits > 0)
            scaled /= Pow10(pic.TrailingScaleDigits);

        long raw = (long)decimal.Truncate(scaled);

        // COMP-5: NO PIC-based truncation (unlike COMP/BINARY).
        // Value uses the full binary capacity of the storage size.

        // Unsigned field: store absolute value (COBOL strips sign on MOVE to unsigned)
        if (!pic.IsSigned && raw < 0)
            raw = -raw;

        switch (length)
        {
            case 1:
                // BINARY-CHAR: low byte of the two's-complement value (signed sbyte or unsigned byte).
                area[offset] = (byte)raw;
                break;
            case 2:
                BinaryPrimitives.WriteInt16LittleEndian(
                    area.AsSpan(offset, 2), (short)raw);
                break;
            case 4:
                BinaryPrimitives.WriteInt32LittleEndian(
                    area.AsSpan(offset, 4), (int)raw);
                break;
            case 8:
                BinaryPrimitives.WriteInt64LittleEndian(
                    area.AsSpan(offset, 8), raw);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // Encode: decimal → bytes
    // ══════════════════════════════════════════════════════════

    public static void EncodeNumeric(
        byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        RuntimeGuard.Buffer(area, offset, length, "numeric-encode");
        switch (pic.Usage)
        {
            case UsageKind.Comp3:
            case UsageKind.PackedDecimal:
                EncodeComp3(area, offset, length, pic, value);
                break;
            case UsageKind.Comp:
            case UsageKind.Binary:
                EncodeCompBinary(area, offset, length, pic, value);
                break;
            case UsageKind.Comp5:
                EncodeComp5(area, offset, length, pic, value);
                break;
            case UsageKind.Comp1:
                EncodeComp1(area, offset, value);
                break;
            case UsageKind.Comp2:
                EncodeComp2(area, offset, value);
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

        // Digits-only string (use decimal.Truncate to avoid long overflow
        // on high-precision fields like PIC 9V9(17) where scaling exceeds Int64)
        string digits = decimal.Truncate(scaled).ToString("F0", CultureInfo.InvariantCulture);

        // Determine available width (reserve 1 for separate sign if needed)
        bool separateSign = pic.SignStorage is SignStorageKind.LeadingSeparate
            or SignStorageKind.TrailingSeparate;
        int availableLength = (pic.IsSigned && separateSign) ? length - 1 : length;

        // Truncate from left if too long (SIZE ERROR should be handled separately)
        if (digits.Length > availableLength)
            digits = digits[^availableLength..];

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

    private static void EncodeComp3(byte[] area, int offset, int length, PicDescriptor pic, decimal value)
    {
        // Apply scaling to get integer representation (same as COMP/BINARY)
        decimal scaled = value;
        int totalFractionScale = pic.FractionDigits + pic.LeadingScaleDigits;
        if (totalFractionScale > 0)
            scaled *= Pow10(totalFractionScale);
        if (pic.TrailingScaleDigits > 0)
            scaled /= Pow10(pic.TrailingScaleDigits);

        scaled = decimal.Truncate(scaled);
        string s = Math.Abs(scaled).ToString("F0", CultureInfo.InvariantCulture);
        // An unsigned packed item stores the absolute value — the operational sign is not retained
        // (ISO §13.18.40 / §14.9.25 GR8). EncodeDisplay/EncodeCompBinary already honor IsSigned; this
        // path did not, so an unsigned PIC 9(n) COMP-3 wrongly kept a negative sign nibble (0x0D) and
        // decoded back as negative. (Surfaced by the data-model numeric differential oracle.)
        bool negative = pic.IsSigned && value < 0;
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

    // ── ROUNDED MODE ordinals (ISO §14.9.4, COBOL-2002) — shared with the compiler's mode mapping ──
    /// <summary>No ROUNDED (default) / MODE TRUNCATION — drop the excess digits (toward zero).</summary>
    public const int RoundTruncation = 0;
    /// <summary>Default ROUNDED — round to nearest; ties away from zero.</summary>
    public const int RoundNearestAwayFromZero = 1;
    /// <summary>Always round away from zero (magnitude up) regardless of the dropped digits.</summary>
    public const int RoundAwayFromZero = 2;
    /// <summary>Round to nearest; ties to the nearest even digit (banker's rounding).</summary>
    public const int RoundNearestEven = 3;
    /// <summary>Round to nearest; ties toward zero.</summary>
    public const int RoundNearestTowardZero = 4;
    /// <summary>No rounding permitted; an inexact result raises the SIZE ERROR condition (EC-SIZE-TRUNCATION)
    /// and leaves the receiver unchanged — the arithmetic store path detects this via <c>CobolNum.TryStore</c>
    /// (ISO §14.9.4).</summary>
    public const int RoundProhibited = 5;
    /// <summary>Round toward positive infinity (ceiling).</summary>
    public const int RoundTowardGreater = 6;
    /// <summary>Round toward negative infinity (floor).</summary>
    public const int RoundTowardLesser = 7;

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
            // Reduce to integer multiples of 10^trailingP, then re-scale.
            decimal pFactor = Pow10(trailingP);
            return RoundToIntegerByMode(value / pFactor, roundingMode) * pFactor;
        }

        // Standard fraction rounding: scale up to an integer, round per mode, scale back.
        decimal factor = Pow10(fractionScale);
        return RoundToIntegerByMode(value * factor, roundingMode) / factor;
    }

    /// <summary>
    /// Round a scaled value to an integer per the ISO ROUNDED MODE (§14.9.4). The eight modes
    /// differ only in how the dropped fraction is resolved; PROHIBITED produces the truncated
    /// (toward-zero) integer here — its "rounding not permitted → size error" behavior is the
    /// caller's responsibility (the arithmetic store path), not this pure helper's.
    /// </summary>
    private static decimal RoundToIntegerByMode(decimal scaled, int roundingMode) => roundingMode switch
    {
        RoundNearestAwayFromZero => Math.Round(scaled, 0, MidpointRounding.AwayFromZero),
        RoundAwayFromZero        => scaled >= 0m ? Math.Ceiling(scaled) : Math.Floor(scaled),
        RoundNearestEven         => Math.Round(scaled, 0, MidpointRounding.ToEven),
        RoundNearestTowardZero   => NearestTowardZero(scaled),
        RoundTowardGreater       => Math.Ceiling(scaled),
        RoundTowardLesser        => Math.Floor(scaled),
        _                        => decimal.Truncate(scaled),   // RoundTruncation, RoundProhibited
    };

    /// <summary>Round to the nearest integer with ties broken toward zero: sign · ceil(|x| − 0.5).</summary>
    private static decimal NearestTowardZero(decimal scaled)
    {
        decimal r = Math.Ceiling(Math.Abs(scaled) - 0.5m);
        return scaled < 0m ? -r : r;
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
            // Numeric-edited fields are already formatted — return raw bytes. BLANK WHEN ZERO / fully
            // zero-suppressed fields are all spaces; TrimEnd would collapse the field to "" — preserve the
            // PICTURE-width blank field instead (ISO §13.18.3).
            string rawEdited = Encoding.ASCII.GetString(area, offset, length);
            string trimmedEdited = rawEdited.TrimEnd();
            return trimmedEdited.Length == 0 && length > 0 ? rawEdited : trimmedEdited;
        }
        if (pic.Category == CobolCategory.Numeric && pic.Usage == UsageKind.Display)
        {
            // DISPLAY numeric: show the raw field content (preserves sign format)
            return Encoding.ASCII.GetString(area, offset, length).TrimEnd();
        }
        if (pic.Usage is UsageKind.Comp1 or UsageKind.Comp2)
        {
            // Floating-point DISPLAY: the natural IEEE magnitude (shortest round-trip), not the synthetic
            // fixed-point 18-digit form FormatNumericForDisplay would produce for a PIC-less float.
            return FormatFloatForDisplay(area, offset, pic.Usage);
        }
        if (pic.Category.IsNumericLike())
        {
            decimal value = DecodeNumeric(area, offset, length, pic);
            return FormatNumericForDisplay(value, pic.FractionDigits, pic.TotalDigits);
        }
        // National / national-edited: stored UTF-16LE (2 bytes per character) — decode as Unicode so the
        // device receives the national characters (ISO §14.9.11), not a byte-mangled ASCII view.
        if (pic.Category.IsNationalLike())
        {
            return Encoding.Unicode.GetString(area, offset, length).TrimEnd();
        }
        // Alphanumeric / edited: return raw bytes as string, trim trailing spaces
        return Encoding.ASCII.GetString(area, offset, length).TrimEnd();
    }

    /// <summary>
    /// DISPLAY of a COMP-1/COMP-2 (binary floating-point) item: render the natural IEEE magnitude as its shortest
    /// round-tripping decimal string (an integral value has no decimal point), rather than the synthetic
    /// fixed-point 18-digit form. The raw field bytes are the little-endian IEEE float/double (see DecodeComp1/2).
    /// </summary>
    private static string FormatFloatForDisplay(byte[] area, int offset, UsageKind usage)
    {
        return usage == UsageKind.Comp1
            ? BitConverter.ToSingle(area, offset).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : BitConverter.ToDouble(area, offset).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void MoveStringToBytes(byte[] area, int offset, int length, string value)
    {
        int copyLen = Math.Min(value.Length, length);
        for (int i = 0; i < length; i++)
            area[offset + i] = i < copyLen ? (byte)value[i] : (byte)' ';
    }

    // ══════════════════════════════════════════════════════════
    // CLASS CONDITIONS (IS NUMERIC, IS ALPHABETIC, etc.)
    // ══════════════════════════════════════════════════════════

    public static bool IsNumericClass(byte[] area, int offset, int length, PicDescriptor pic)
    {
        // COBOL-85 §6.3.4.1: For alphanumeric/group items, NUMERIC = digits 0-9 only.
        // For numeric items, signs and decimal points are allowed per the PIC.
        // Spaces are NOT digits and cause IS NUMERIC to return false.
        bool isNumericCategory = pic.Category == CobolCategory.Numeric;

        // Determine which position (if any) holds an overpunch or separate sign
        int overpunchPos = -1;
        int separateSignPos = -1;
        if (isNumericCategory && pic.IsSigned)
        {
            switch (pic.SignStorage)
            {
                case SignStorageKind.TrailingOverpunch:
                    overpunchPos = length - 1;
                    break;
                case SignStorageKind.LeadingOverpunch:
                    overpunchPos = 0;
                    break;
                case SignStorageKind.TrailingSeparate:
                    separateSignPos = length - 1;
                    break;
                case SignStorageKind.LeadingSeparate:
                    separateSignPos = 0;
                    break;
            }
        }

        for (int i = 0; i < length; i++)
        {
            char c = (char)area[offset + i];
            if (c >= '0' && c <= '9') continue;

            // Overpunch sign position: accept overpunch-encoded digits
            // Positive 0-9: { A B C D E F G H I
            // Negative 0-9: } J K L M N O P Q R
            if (i == overpunchPos)
            {
                if (c == '{' || (c >= 'A' && c <= 'I') ||
                    c == '}' || (c >= 'J' && c <= 'R'))
                    continue;
            }

            // Separate sign position: accept + or -
            if (i == separateSignPos && (c == '+' || c == '-')) continue;

            return false;
        }
        return true;
    }

    public static bool IsAlphabeticClass(byte[] area, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            char c = (char)area[offset + i];
            // ISO 1989:2023 §8.8.4.4: the alphabetic class is the closed set {A–Z, a–z, space} — NOT the
            // Unicode-wide letter set. char.IsLetter would wrongly accept accented/non-Latin letters.
            if (c == ' ') continue;
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) return false;
        }
        return true;
    }

    public static bool IsAlphabeticLowerClass(byte[] area, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            char c = (char)area[offset + i];
            if (c == ' ') continue;
            if (c < 'a' || c > 'z') return false;
        }
        return true;
    }

    public static bool IsAlphabeticUpperClass(byte[] area, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            char c = (char)area[offset + i];
            if (c == ' ') continue;
            if (c < 'A' || c > 'Z') return false;
        }
        return true;
    }

    /// <summary>
    /// User-defined CLASS condition: returns true if every byte in the field
    /// is in the validBytes set.
    /// </summary>
    public static bool IsInUserClass(byte[] area, int offset, int length, byte[] validBytes)
    {
        for (int i = 0; i < length; i++)
        {
            byte b = area[offset + i];
            bool found = false;
            for (int j = 0; j < validBytes.Length; j++)
            {
                if (validBytes[j] == b)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }

    /// <summary>
    /// Alphanumeric comparison with a custom collating sequence.
    /// The collatingSequence is a 256-byte array mapping character ordinal → sort weight.
    /// Returns -1, 0, or 1.
    /// </summary>
    public static int CompareAlphanumericWithSequence(
        byte[] leftArea, int leftOffset, int leftLength,
        byte[] rightArea, int rightOffset, int rightLength,
        byte[] collatingSequence)
    {
        int maxLen = Math.Max(leftLength, rightLength);
        for (int i = 0; i < maxLen; i++)
        {
            byte lb = i < leftLength ? leftArea[leftOffset + i] : (byte)' ';
            byte rb = i < rightLength ? rightArea[rightOffset + i] : (byte)' ';
            int lw = collatingSequence[lb];
            int rw = collatingSequence[rb];
            if (lw < rw) return -1;
            if (lw > rw) return 1;
        }
        return 0;
    }
}
