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
/// <c>decimal</c>).
///
/// SPECIAL-NAMES threading (ISO §12.3.7): <c>currency</c> is the program's currency PICTURE SYMBOL (GR13;
/// default <c>$</c> per SR25); <c>commaMode</c> is DECIMAL-POINT IS COMMA (GR14 — the decimal and grouping
/// separators EXCHANGE functionality; §13.18.40.2 SR13: "the rules for the symbol period apply to the symbol
/// comma, and the rules for the symbol comma apply to the symbol period", and §13.18.40.6: "the precedence rules
/// … are interchanged"). The public entries CANONICALIZE: under comma-mode the mask's <c>.</c>/<c>,</c> are
/// swapped so the core logic always sees dot-as-decimal, and the rendered output swaps back — which realizes
/// GR14b exactly (the inserted decimal separator IS the comma, the inserted grouping separator IS the period),
/// including zero suppression absorbing GROUPING periods and stopping at the COMMA decimal position (§13.18.40.5
/// — the legacy engine's dead-flag bug, fixed here per spec).
/// </summary>
public static class CobolEdit
{
    /// <summary>Swap <c>.</c>↔<c>,</c> — the §13.18.40.2 SR13 role exchange, applied to a mask entering the
    /// dot-canonical core and to the rendered output leaving it (the swap is its own inverse).</summary>
    private static string SwapSeparators(string s)
    {
        var a = s.ToCharArray();
        for (int i = 0; i < a.Length; i++)
            a[i] = a[i] switch { '.' => ',', ',' => '.', _ => a[i] };
        return new string(a);
    }

    /// <summary>Format <paramref name="value"/> (unscaled, with <paramref name="valueScale"/> fraction digits)
    /// into <paramref name="picture"/> — the EXPANDED edited picture (repeats unrolled, uppercased, the implied
    /// decimal point <c>V</c> retained; <c>V</c> occupies no output position).</summary>
    public static string Format(Int128 value, int valueScale, string picture, bool blankWhenZero = false,
        char currency = '$', bool commaMode = false)
    {
        if (commaMode)
        {
            // Canonicalize the mask (dot = decimal), render, swap the rendered separators back (GR14b).
            string canonical = Format(value, valueScale, SwapSeparators(picture), blankWhenZero, currency);
            return SwapSeparators(canonical);
        }
        bool negative = value < 0;

        // The output pattern: V marks the implied point but holds no character position (ISO §13.18.40.3).
        string pattern = picture.Replace("V", "").Replace("P", "");   // V and P hold no output position (§13.18.40.3)

        // BLANK WHEN ZERO (ISO §13.18.8): a zero value stores ALL spaces — before any editing.
        if (blankWhenZero && value == 0) return new string(' ', pattern.Length);

        // Pre-scan (on the full picture): fixed vs floating sign/currency — ONE occurrence is a fixed-insertion
        // character; TWO OR MORE form a floating string whose members are also digit positions (§13.18.40.4).
        // The currency symbol is the program's CURRENCY SIGN PICTURE SYMBOL (ISO §12.3.7 GR13; '$' per SR25).
        char currencyChar = char.ToUpperInvariant(currency);
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
        Int128 scaled = CobolNum.Rescale(value, valueScale, fracDigits, CobolRounding.Truncation);
        string digits = Int128.Abs(scaled).ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    /// <summary>DE-EDIT a numeric-edited item's image back to its numeric value (ISO §14.9.25.4 GR5 — a
    /// numeric-edited SENDER moved to a numeric receiver, the COBOL-85 de-editing move): every digit POSITION of
    /// the mask contributes its image digit (a suppressed/replaced position — space, <c>*</c>, a floating symbol's
    /// landing spot — contributes zero), yielding the unscaled value at the mask's fraction scale
    /// (<see cref="MaskScale"/>); the value is negative when the image carries the mask's negative sign
    /// (<c>-</c> anywhere a sign can land, or a non-blank CR/DB).</summary>
    public static Int128 DeEdit(string image, string picture, char currency = '$', bool commaMode = false)
    {
        if (commaMode) picture = SwapSeparators(picture);   // canonicalize (§13.18.40.2 SR13) — digit POSITIONS are unchanged
        string pattern = picture.Replace("V", "").Replace("P", "");   // V and P hold no output position (§13.18.40.3)
        char currencyChar = char.ToUpperInvariant(currency);
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == currencyChar) cs++;
        }
        bool fixedPlus = plus == 1 && minus == 0, fixedMinus = minus == 1 && plus == 0, fixedCs = cs == 1;

        Int128 value = 0;
        bool negative = false;
        for (int i = 0; i < pattern.Length && i < image.Length; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            char c = image[i];
            bool digitPos = p is '9' or 'Z' or '*'
                || (p == currencyChar && !fixedCs)
                || (p == '+' && !fixedPlus)
                || (p == '-' && !fixedMinus);
            if (digitPos) { value = value * 10 + (c is >= '0' and <= '9' ? c - '0' : 0); continue; }
            if (c == '-') negative = true;                      // a fixed sign position holding minus
            else if (p == 'C' && c == 'C') negative = true;     // CR rendered (negative value)
            else if (p == 'D' && c == 'D') negative = true;     // DB rendered
        }
        if (image.Contains('-')) negative = true;               // a floating minus landed inside its zone
        return negative ? -value : value;
    }

    /// <summary>Checked numeric→edited conversion for ARITHMETIC stores under ON SIZE ERROR (ISO §14.7.5 — size
    /// error condition case 3: after decimal-point alignment, |value| exceeds the mask's digit positions; storing
    /// rule 2: that resultant is left UNCHANGED and only the flag is set, the statement's other receivers still
    /// store). MOVE keeps the unchecked <see cref="Format"/> — high-order truncation IS the defined MOVE behavior
    /// (§14.9.25).</summary>
    public static bool TryFormat(Int128 value, int valueScale, string picture, out string image,
        bool blankWhenZero = false, char currency = '$', bool commaMode = false)
    {
        var (capacity, fracDigits) = MaskCapacity(picture, currency, commaMode);
        Int128 scaled = CobolNum.Rescale(value, valueScale, fracDigits, CobolRounding.Truncation);
        if (Int128.Abs(scaled) >= CobolNum.Pow10Wide(capacity)) { image = string.Empty; return false; }
        image = Format(value, valueScale, picture, blankWhenZero, currency, commaMode);
        return true;
    }

    /// <summary>The mask's total digit-position capacity (9/Z/* plus floating-string members, less the ONE
    /// position the floating symbol itself occupies) and its fraction scale — the §14.7.5 size-error bound.
    /// Mirrors <see cref="Format"/>'s prologue exactly. Public so the compiler can reuse the ONE canonical
    /// edited digit-position count for the HIGHEST/LOWEST-ALGEBRAIC PICTURE fold (§15.43/§15.58; singular-pattern).</summary>
    public static (int Capacity, int FracDigits) MaskCapacity(string picture, char currency = '$', bool commaMode = false)
    {
        if (commaMode) picture = SwapSeparators(picture);   // canonicalize (§13.18.40.2 SR13)
        string pattern = picture.Replace("V", "").Replace("P", "");   // V and P hold no output position (§13.18.40.3)
        char currencyChar = char.ToUpperInvariant(currency);
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == currencyChar) cs++;
        }
        bool fixedPlus = plus == 1 && minus == 0, fixedMinus = minus == 1 && plus == 0, fixedCs = cs == 1;
        int digits = 0;
        foreach (char raw in pattern)
        {
            char p = char.ToUpperInvariant(raw);
            if (p is '9' or 'Z' or '*') digits++;
            else if (p == currencyChar && !fixedCs) digits++;
            else if (p == '+' && !fixedPlus) digits++;
            else if (p == '-' && !fixedMinus) digits++;
        }
        bool hasFloating = cs > 1 || plus > 1 || minus > 1;
        return (hasFloating ? digits - 1 : digits,
                FractionDigits(picture, currencyChar, fixedCs, fixedPlus, fixedMinus));
    }

    /// <summary>The mask's fraction scale — digit positions right of the point (<c>V</c> or <c>.</c>). Public so
    /// the compiler can fold the working scale of an edited RECEIVER at emit time (a quotient/ROUNDED result must
    /// be computed and rounded AT this scale before editing, ISO §14.7.4/§14.7.7).</summary>
    public static int MaskScale(string picture, char currency = '$', bool commaMode = false)
    {
        if (commaMode) picture = SwapSeparators(picture);   // canonicalize (§13.18.40.2 SR13) — the comma IS the decimal position
        char currencyChar = char.ToUpperInvariant(currency);
        int plus = 0, minus = 0, cs = 0;
        foreach (char raw in picture)
        {
            char p = char.ToUpperInvariant(raw);
            if (p == '+') plus++;
            else if (p == '-') minus++;
            else if (p == currencyChar) cs++;
        }
        return FractionDigits(picture, currencyChar, cs == 1, plus == 1 && minus == 0, minus == 1 && plus == 0);
    }

    /// <summary>Digit positions to the right of the point — the <c>V</c> in the picture, or the <c>.</c> insertion
    /// character (only one of the two may appear, ISO §13.18.40.3).</summary>
    private static int FractionDigits(string picture, char currencyChar, bool fixedCs, bool fixedPlus, bool fixedMinus)
    {
        // PICTURE P scaling positions (§13.18.40.3): trailing P → a NEGATIVE mask scale (the value is a multiple
        // of 10^P — PIC ZZZPP aligns 900 to unscaled 9, NC124A PICTURE-TEST-30); leading P → every digit position
        // is fractional (scale = P-count + digit positions). P never coexists with V-fraction digits.
        int pCount = 0;
        foreach (char raw in picture) if (char.ToUpperInvariant(raw) == 'P') pCount++;
        if (pCount > 0)
        {
            string up = picture.ToUpperInvariant();
            int lastDigitPos = up.LastIndexOfAny(['9', 'Z', '*']);
            if (lastDigitPos >= 0 && up.IndexOf('P', lastDigitPos) > lastDigitPos) return -pCount;
            int digitPositions = 0;
            foreach (char c in up) if (c is '9' or 'Z' or '*') digitPositions++;
            return pCount + digitPositions;
        }
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

    /// <summary>ALPHANUMERIC-EDITED formatting (ISO §13.18.40 — X/A/9 with B 0 / simple insertion): source
    /// characters fill the X/A/9 positions left-to-right (space-padded when exhausted); each insertion position
    /// supplies its character (B → space).</summary>
    public static string FormatAlphanumeric(string source, string picture)
    {
        var output = new char[picture.Length];
        int si = 0;
        for (int i = 0; i < picture.Length; i++)
        {
            char p = char.ToUpperInvariant(picture[i]);
            output[i] = p switch
            {
                'X' or 'A' or '9' => si < source.Length ? source[si++] : ' ',
                'B' => ' ',
                '0' => '0',
                '/' => '/',
                _ => picture[i],
            };
        }
        return new string(output);
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
