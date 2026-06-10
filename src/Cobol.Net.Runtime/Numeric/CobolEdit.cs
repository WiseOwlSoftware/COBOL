// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Numeric-edited PICTURE formatting (ISO §13.18.40.4 editing rules): simple insertion (<c>B 0 / ,</c>), special
/// insertion (<c>.</c>), fixed insertion (<c>cs</c>, leading <c>+ -</c>, trailing <c>+ - CR DB</c>), floating
/// insertion (<c>$$… ++… --…</c>), and zero suppression/replacement (<c>Z *</c>). The ONE numeric→edited
/// conversion — used by MOVE to a numeric-edited receiver and by arithmetic GIVING/COMPUTE stores (§14.7.7: the
/// result is stored per the MOVE editing rules). The algorithm is the legacy engine's, proven over the NIST-85
/// corpus, re-hosted on the typed-native substrate (unscaled <see cref="long"/> + scale; no byte areas, no
/// <c>decimal</c>). DECIMAL-POINT IS COMMA and multi-character CURRENCY SIGN are SPECIAL-NAMES features that
/// thread in when their binder support lands.
/// </summary>
public static class CobolEdit
{
    /// <summary>Format <paramref name="value"/> (unscaled, with <paramref name="valueScale"/> fraction digits)
    /// into <paramref name="picture"/> — the EXPANDED edited picture (repeats unrolled, uppercased, the implied
    /// decimal point <c>V</c> retained; <c>V</c> occupies no output position).</summary>
    public static string Format(long value, int valueScale, string picture)
    {
        bool negative = value < 0;

        // The output pattern: V marks the implied point but holds no character position (ISO §13.18.40.3).
        string pattern = picture.Replace("V", "");

        // Pre-scan (on the full picture): fixed vs floating sign/currency — ONE occurrence is a fixed-insertion
        // character; TWO OR MORE form a floating string whose members are also digit positions (§13.18.40.4).
        const char currencyChar = '$';
        int plusCount = 0, minusCount = 0, currencyCount = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plusCount++;
            else if (p == '-') minusCount++;
            else if (p == currencyChar) currencyCount++;
        }
        bool isFixedPlus = plusCount == 1 && minusCount == 0;
        bool isFixedMinus = minusCount == 1 && plusCount == 0;
        bool isFixedCurrency = currencyCount == 1;

        // Digit capacity: 9/Z/* always; floating $/+/- are digit positions, but the floating string reserves ONE
        // position for the symbol itself.
        int trueDigitCount = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p is '9' or 'Z' or '*') trueDigitCount++;
            else if (p == currencyChar && !isFixedCurrency) trueDigitCount++;
            else if (p == '+' && !isFixedPlus) trueDigitCount++;
            else if (p == '-' && !isFixedMinus) trueDigitCount++;
        }
        bool hasFloating = currencyCount > 1 || plusCount > 1 || minusCount > 1;
        int effectiveDigitCount = hasFloating ? trueDigitCount - 1 : trueDigitCount;

        // The mask's fraction scale: digit positions after the point (V in the picture, or the '.' insertion).
        int fracDigits = FractionDigits(picture, currencyChar, isFixedCurrency, isFixedPlus, isFixedMinus);

        // Align the operand to the mask's scale (truncation — §14.9.25 GR: excess fraction digits truncate) and
        // render the absolute digit string at the mask's capacity (excess INTEGER digits truncate high-order).
        long scaled = CobolNum.Rescale(value, valueScale, fracDigits, CobolRounding.Truncation);
        string digits = Math.Abs(scaled).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (digits.Length < effectiveDigitCount) digits = digits.PadLeft(effectiveDigitCount, '0');
        else if (digits.Length > effectiveDigitCount) digits = digits[^effectiveDigitCount..];

        // Pass 1 — right-to-left: fill digit positions, place insertion and fixed characters.
        var output = new char[pattern.Length];
        int digitIdx = digits.Length - 1;
        for (int i = pattern.Length - 1; i >= 0; i--)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == currencyChar)
            {
                output[i] = isFixedCurrency ? currencyChar : digitIdx >= 0 ? digits[digitIdx--] : '0';
                continue;
            }
            switch (p)
            {
                case '9' or 'Z' or '*':
                    output[i] = digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;
                case '+':
                    output[i] = isFixedPlus ? (negative ? '-' : '+') : digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;
                case '-':
                    output[i] = isFixedMinus ? (negative ? '-' : ' ') : digitIdx >= 0 ? digits[digitIdx--] : '0';
                    break;
                case '.': output[i] = '.'; break;
                case ',': output[i] = ','; break;
                case 'B': output[i] = ' '; break;
                case '/': output[i] = '/'; break;
                case '0': output[i] = '0'; break;
                case 'C':   // CR — spaces when the value is not negative (§13.18.40.4 fixed insertion)
                    if (i + 1 < pattern.Length && char.ToUpperInvariant(pattern[i + 1]) == 'R')
                    {
                        output[i] = negative ? 'C' : ' ';
                        output[i + 1] = negative ? 'R' : ' ';
                    }
                    else output[i] = pattern[i];
                    break;
                case 'R':   // second char of CR — placed by the 'C' case
                    if (!(i > 0 && char.ToUpperInvariant(pattern[i - 1]) == 'C')) output[i] = pattern[i];
                    break;
                case 'D':   // DB
                    if (i + 1 < pattern.Length && char.ToUpperInvariant(pattern[i + 1]) == 'B')
                    {
                        output[i] = negative ? 'D' : ' ';
                        output[i + 1] = negative ? 'B' : ' ';
                    }
                    else output[i] = pattern[i];
                    break;
                default: output[i] = pattern[i]; break;
            }
        }

        // Pass 2 — left-to-right zero suppression/replacement (Z → space, * → asterisk; a floating symbol's zone
        // suppresses like Z). Suppression stops at the first significant digit, a fixed '9', or the point.
        bool suppressing = true;
        bool asteriskFill = pattern.Contains('*');
        bool allIntegerSuppressed = true;
        for (int i = 0; i < pattern.Length && suppressing; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == currencyChar && !isFixedCurrency)
            {
                if (output[i] == '0') output[i] = ' ';
                else { suppressing = false; allIntegerSuppressed = false; }
                continue;
            }
            switch (p)
            {
                case 'Z':
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '*':
                    if (output[i] == '0') output[i] = '*';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '+' when !isFixedPlus:
                case '-' when !isFixedMinus:
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case ',' or 'B':
                    output[i] = asteriskFill ? '*' : ' ';   // insertion inside a suppressed zone suppresses too
                    break;
                case '.':
                    suppressing = false;
                    break;
                case '9':
                    suppressing = false;
                    allIntegerSuppressed = false;
                    break;
            }
        }

        // Zero + every position suppressible (no fixed 9 anywhere) → the whole field blanks (spaces, or asterisk
        // fill keeping the actual decimal point — §13.18.40.4 zero-suppression rules).
        bool fullFieldBlanked = false;
        if (allIntegerSuppressed && scaled == 0 && !pattern.Contains('9'))
        {
            fullFieldBlanked = true;
            for (int i = 0; i < output.Length; i++)
            {
                char p = char.ToUpperInvariant(pattern[i]);
                output[i] = asteriskFill ? (p == '.' ? '.' : '*') : ' ';
            }
        }
        if (fullFieldBlanked && !asteriskFill) return new string(output);

        // Floating symbol placement: the symbol lands at the rightmost suppressed position of its floating zone.
        if (plusCount > 0 && plusCount + minusCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, '+');
            if (pos >= 0) output[pos] = negative ? '-' : '+';
        }
        else if (minusCount > 0 && plusCount + minusCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, '-');
            if (pos >= 0) output[pos] = negative ? '-' : ' ';
        }
        if (currencyCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, currencyChar);
            if (pos >= 0) output[pos] = currencyChar;
        }

        return new string(output);
    }

    /// <summary>The mask's fraction scale — digit positions right of the point (<c>V</c> or <c>.</c>). Public so
    /// the compiler can fold the working scale of an edited RECEIVER at emit time (a quotient/ROUNDED result must
    /// be computed and rounded AT this scale before editing, ISO §14.7.4/§14.7.7).</summary>
    public static int MaskScale(string picture)
    {
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in picture)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == '$') cs++;
        }
        return FractionDigits(picture, '$', cs == 1, plus == 1 && minus == 0, minus == 1 && plus == 0);
    }

    /// <summary>Digit positions to the right of the point — the <c>V</c> in the picture, or the <c>.</c> insertion
    /// character (only one of the two may appear, ISO §13.18.40.3).</summary>
    private static int FractionDigits(string picture, char currencyChar, bool fixedCs, bool fixedPlus, bool fixedMinus)
    {
        int point = picture.IndexOf('V');
        if (point < 0) point = picture.IndexOf('.');
        if (point < 0) return 0;
        int n = 0;
        for (int i = point + 1; i < picture.Length; i++)
        {
            char p = char.ToUpperInvariant(picture[i]);
            if (p is '9' or 'Z' or '*') n++;
            else if (p == currencyChar && !fixedCs) n++;
            else if (p == '+' && !fixedPlus) n++;
            else if (p == '-' && !fixedMinus) n++;
            else if (p is 'C' or 'D') break;   // CR/DB
        }
        return n;
    }

    /// <summary>The rightmost suppressed position within a floating symbol's zone (the symbol's own positions plus
    /// suppressed <c>,</c>/<c>B</c> insertions inside it).</summary>
    private static int FindFloatingPlacement(string pattern, char[] output, char floatChar)
    {
        char target = char.ToUpperInvariant(floatChar);
        int lastSuppressed = -1;
        bool inZone = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == target)
            {
                inZone = true;
                if (output[i] == ' ') lastSuppressed = i;
                else break;
            }
            else if (inZone && p is ',' or 'B' && output[i] == ' ') lastSuppressed = i;
            else if (inZone) break;
        }
        return lastSuppressed;
    }
}
