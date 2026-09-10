// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

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
/// SPECIAL-NAMES threading (ISO §12.3.7): <c>currency</c> is the mask's currency PICTURE SYMBOL (GR13; the
/// compiler CANONICALIZES every mask's currency symbol to <c>$</c> at bind — <c>PictureAnalyzer</c> — so
/// generated code always passes the default) and <c>currencyString</c> is the currency STRING that symbol expands
/// to when it is not the single character <c>$</c> (§12.3.7.3 r23: "Literal-7 may have any length"; §13.18.40.4
/// GR14: "The first occurrence of the currency symbol adds the number of characters in the currency string to the
/// size of the item. Each subsequent occurrence of the currency symbol adds one" — kb/Work PB60 / AR-15.68.3-3).
/// The core edits on the LOGICAL one-character-per-symbol image and <see cref="ExpandCurrency"/> /
/// <see cref="CollapseCurrency"/> map that image to and from the physical one, so the multi-character string
/// touches no editing rule; <c>commaMode</c> is DECIMAL-POINT IS COMMA (GR14 — the decimal and grouping
/// separators EXCHANGE functionality; §13.18.40.2 SR13: "the rules for the symbol period apply to the symbol
/// comma, and the rules for the symbol comma apply to the symbol period", and §13.18.40.6: "the precedence rules
/// … are interchanged"). The public entries CANONICALIZE: under comma-mode the mask's <c>.</c>/<c>,</c> are
/// swapped so the core logic always sees dot-as-decimal, and the rendered output swaps back — which realizes
/// GR14b exactly (the inserted decimal separator IS the comma, the inserted grouping separator IS the period),
/// including zero suppression absorbing GROUPING periods and stopping at the COMMA decimal position (§13.18.40.5
/// — the legacy engine's dead-flag bug, fixed here per spec).
/// </summary>
public static partial class CobolEdit
{
    /// <summary>One resolved PICTURE EDITING phrase for the single-character render (ISO §13.18.40.5): the user
    /// editing <paramref name="Char1"/> renders as <paramref name="Neg"/> when the value is negative and
    /// <paramref name="Pos"/> otherwise. The simple-insertion (IS) form is sign-independent (<c>Neg == Pos</c> =
    /// the single-character literal); the extended sign-control (FOR) form selects by sign, the unspecified side
    /// defaulting to a space (SR12c). character-1 is NEVER a digit position (SR8 excludes every digit/edit symbol),
    /// so it holds no digit and no fraction. Multi-character literals and floating (character-1 repeated ≥2 under a
    /// FOR phrase) require abandoning the 1:1 mask model — a documented P14 render GAP staged loud at bind
    /// (COBOLNET0899); they never reach here.
    /// <para><paramref name="SimpleInsertion"/> is the phrase's FORM, and it is what decides whether this
    /// character-1 belongs to a zero-suppression or floating string (§13.18.40.5 rules 6 and 7: "Any of the
    /// simple insertion editing symbols embedded in this string or to the immediate right of this string are part
    /// of the string"). It is the IS form: rule 3 — "the symbols 'B', '0', '/', ',' and, if literal=1 is
    /// specified, character-1 are used as the simple insertion editing symbols"; the FOR form is FIXED insertion
    /// — rule 5 — "When character-1 is used, and is not a simple insertion character, it represents literal-2, or
    /// literal-3 as the insertion characters". ⛔ It is NOT derivable from <c>Neg == Pos</c>: a FOR phrase may
    /// name the same literal on both sides (<c>PIC ZT9 EDITING "T" FOR NEGATIVE IS ":" POSITIVE IS ":"</c>),
    /// which is fixed insertion and must survive the suppression walk that eats an IS-form ':' beside it.</summary>
    public readonly record struct EditRule(char Char1, char Neg, char Pos, bool SimpleInsertion);

    /// <summary>⛔ THE SIMPLE INSERTION EDITING SYMBOLS — ISO §13.18.40.5 rule 3, written down ONCE: "the symbols
    /// 'B', '0', '/', ',' and, if literal=1 is specified, character-1 are used as the simple insertion editing
    /// symbols". Every consumer of the set reads <see cref="IsSimpleInsertionSymbol"/> or
    /// <see cref="TrySimpleInsertion"/> — <see cref="Format"/>'s pass 1 (place the insertion character), its
    /// pass 2 (a symbol inside a suppression string takes the replacement character),
    /// <see cref="FindFloatingPlacement"/> (a symbol inside a floating string is a landing position for the
    /// floating character), <see cref="FormatAlphanumeric"/> and the floating-point significand renderer
    /// (<c>RenderFloat</c>) — because the set was previously spelled out at
    /// each site and the copies drifted to the pair {',', 'B'}, so <c>PIC ZZ/ZZ</c> and <c>PIC ZZ0ZZ</c> kept
    /// their insertion character where no significant numeric character stood to its left and <c>PIC ++/++9</c>
    /// landed its sign four positions early (kb/Work PB490). <c>CobolEditSimpleInsertionDriftTests</c> fails if
    /// the consumers disagree again.</summary>
    public static ReadOnlySpan<char> SimpleInsertionSymbols => "B0/,";

    /// <summary>Is <paramref name="maskChar"/> one of the picture's SIMPLE INSERTION editing symbols
    /// (§13.18.40.5 rule 3)? <paramref name="grouping"/> is the character playing the GROUPING separator's part —
    /// ',' in the dot-canonical core, and the caller's own choice on a RAW picture, because DECIMAL-POINT IS
    /// COMMA exchanges the two (§13.18.40.2 SR13: "the rules for the symbol period apply to the symbol comma, and
    /// the rules for the symbol comma apply to the symbol period"). Whichever of the pair is NOT the grouping
    /// separator is the decimal point, whose editing is SPECIAL insertion (rule 4), never simple.</summary>
    public static bool IsSimpleInsertionSymbol(char maskChar, char grouping = ',')
    {
        char u = char.ToUpperInvariant(maskChar);
        return u is ',' or '.' ? maskChar == grouping : SimpleInsertionSymbols.IndexOf(u) >= 0;
    }

    /// <summary>The picture's simple insertion symbols INCLUDING every IS-form PICTURE EDITING character-1, with
    /// the character each one inserts: §13.18.40.5 rule 3 — "Simple insertion editing results in the insertion
    /// character occupying the same character position in the edited item as the associated symbol occupies in
    /// character-string-1" — over §13.18.40.4 GR14's per-symbol text ('B' "a character position into which the
    /// character space will be inserted", '0' "the character zero", '/' "the character slant", ',' "the character
    /// comma"). The mask given here is the DOT-CANONICAL one <see cref="Format"/> works on, so the grouping
    /// separator is ','.</summary>
    private static bool TrySimpleInsertion(char maskChar, EditRule[]? edits, out char inserted)
    {
        char u = char.ToUpperInvariant(maskChar);
        if (edits is not null)
            foreach (var e in edits)
                if (e.SimpleInsertion && u == char.ToUpperInvariant(e.Char1))
                {
                    inserted = e.Pos;   // rule 3 is sign-independent: the IS form carries Neg == Pos == literal-1
                    return true;
                }
        if (!IsSimpleInsertionSymbol(u)) { inserted = '\0'; return false; }
        inserted = u == 'B' ? ' ' : maskChar;   // B inserts a space; 0 / and , insert themselves
        return true;
    }

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
        char currency = '$', bool commaMode = false, EditRule[]? edits = null, string? currencyString = null)
    {
        // A currency STRING other than the symbol itself (§12.3.7.4 GR13 — a one-character '#' for the '$'
        // canonical symbol, or a multi-character "USD"; §13.18.40.4 GR14): edit the LOGICAL image (one position
        // per symbol), then expand the one currency position into the string — a multi-character string makes
        // the physical item len(string) − 1 wider.
        if (currencyString is not null && currencyString != currency.ToString())
            return ExpandCurrency(Format(value, valueScale, picture, blankWhenZero, currency, commaMode, edits), currency, currencyString);
        if (commaMode)
        {
            // Canonicalize the mask (dot = decimal), render, swap the rendered separators back (GR14b).
            string canonical = Format(value, valueScale, SwapSeparators(picture), blankWhenZero, currency, false, edits);
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
        // ⛔ The widening is the STORE-CAP form (kb/Work PB639): the digits this mask could never show are
        // dropped decimally BEFORE the multiply, so the high-order truncation two lines below is the LOW-ORDER
        // digits of the true value (§14.6.8.2 r4 "zero fill or truncation on either end"), not the low-order
        // digits of an Int128 wrap. MOVE 10^30 TO PIC Z(8)9.9(9) showed 123822295.304634368 for 0.000000000.
        Int128 scaled = CobolNum.RescaleStoreCap(value, valueScale, fracDigits, CobolRounding.Truncation);
        string digits = Int128.Abs(scaled).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (digits.Length < effectiveDigitCount) digits = digits.PadLeft(effectiveDigitCount, '0');
        else if (digits.Length > effectiveDigitCount) digits = digits[^effectiveDigitCount..];

        // Pass 1 — right-to-left: fill digit positions, place insertion and fixed characters.
        var output = new char[pattern.Length];
        int digitIdx = digits.Length - 1;
        for (int i = pattern.Length - 1; i >= 0; i--)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            // SIMPLE INSERTION (ISO §13.18.40.5 rule 3) — 'B', '0', '/', ',' and every IS-form PICTURE EDITING
            // character-1, from the ONE set (TrySimpleInsertion), resolved BEFORE the metacharacter switch so a
            // literal that happens to be a mask symbol (':', '/', '(', …) is never re-interpreted. Not a digit
            // position (SR8 for character-1; GR14 for the four symbols) — the digit index is untouched.
            if (TrySimpleInsertion(pattern[i], edits, out char simpleIns)) { output[i] = simpleIns; continue; }
            // FIXED INSERTION with an extended editing sign control symbol (rule 5, Table 8): the FOR form selects
            // Neg on a negative value, Pos otherwise (SR12c default = space).
            if (edits is not null)
            {
                bool matched = false;
                foreach (var e in edits)
                    if (p == char.ToUpperInvariant(e.Char1)) { output[i] = negative ? e.Neg : e.Pos; matched = true; break; }
                if (matched) continue;
            }
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
                case '.': output[i] = '.'; break;   // SPECIAL insertion (§13.18.40.5 rule 4)
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
        // suppresses like Z). §13.18.40.5 rule 7 a): "the corresponding replacement character is placed into any
        // character position immediately preceding whichever of the following is encountered first" — the first
        // nonzero numeric character, the first character position for which no zero suppression is specified, or
        // the decimal point position (rule 6 a) is the same walk for a floating string, its landing placed below).
        // ⛔ THE POSITIONS ARE THE STRING'S. Rules 6 and 7 both say "Any of the simple insertion editing symbols
        // embedded in this string or to the immediate right of this string are part of the string" — EMBEDDED or
        // RIGHT, never left, so a simple insertion symbol takes the replacement character only once a member of
        // the string (Z, * or a floating symbol) has been seen. `PIC 0ZZ9` MOVE 1 keeps its leading zero ("0  1")
        // while `PIC ZZ0ZZ` MOVE 12 suppresses the embedded one ("   12"); §13.18.40.4 GR15 says the same from
        // the VALIDATE side — "the character zero if the symbol '0' is neither part of floating insertion editing
        // nor of zero suppression with replacement editing; otherwise, the character zero or, if no significant
        // numeric character appears to its left, the corresponding floating insertion character or replacement
        // character respectively" (kb/Work PB490: the set was the hand-written pair {',', 'B'} and the membership
        // test was missing altogether, so a leading ',' was eaten and an embedded '0' or '/' survived).
        bool suppressing = true;
        bool inString = false;   // a Z / * / floating member has been seen — only then is an insertion "part of the string"
        bool asteriskFill = pattern.Contains('*');
        bool allIntegerSuppressed = true;
        for (int i = 0; i < pattern.Length && suppressing; i++)
        {
            char p = char.ToUpperInvariant(pattern[i]);
            if (p == currencyChar && !isFixedCurrency)
            {
                inString = true;
                if (output[i] == '0') output[i] = ' ';
                else { suppressing = false; allIntegerSuppressed = false; }
                continue;
            }
            switch (p)
            {
                case 'Z':
                    inString = true;
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '*':
                    inString = true;
                    if (output[i] == '0') output[i] = '*';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '+' when !isFixedPlus:
                case '-' when !isFixedMinus:
                    inString = true;
                    if (output[i] == '0') output[i] = ' ';
                    else { suppressing = false; allIntegerSuppressed = false; }
                    break;
                case '.':
                    suppressing = false;
                    break;
                case '9':
                    suppressing = false;
                    allIntegerSuppressed = false;
                    break;
                default:
                    // Inside the string: a simple insertion symbol is part of it and takes the replacement
                    // character; anything else is "the first character position for which no zero suppression
                    // with replacement is specified" and ends the walk. Outside it (a leading insertion, a
                    // leading fixed sign or currency) nothing is replaced and the string has yet to start.
                    if (!inString) break;
                    if (TrySimpleInsertion(pattern[i], edits, out _)) output[i] = asteriskFill ? '*' : ' ';
                    else suppressing = false;
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
            int pos = FindFloatingPlacement(pattern, output, '+', edits);
            if (pos >= 0) output[pos] = negative ? '-' : '+';
        }
        else if (minusCount > 0 && plusCount + minusCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, '-', edits);
            if (pos >= 0) output[pos] = negative ? '-' : ' ';
        }
        if (currencyCount > 1)
        {
            int pos = FindFloatingPlacement(pattern, output, currencyChar, edits);
            if (pos >= 0) output[pos] = currencyChar;
        }

        return new string(output);
    }

    /// <summary>DE-EDIT a numeric-edited item's image back to its numeric value (ISO §14.9.25.4 GR5 — a
    /// numeric-edited SENDER moved to a numeric receiver, the COBOL-85 de-editing move): every digit POSITION of
    /// the mask contributes its image digit (a suppressed/replaced position — space, <c>*</c>, a floating symbol's
    /// landing spot — contributes zero), yielding the unscaled value at the mask's fraction scale
    /// (<see cref="MaskScale"/>); the value is negative when the image carries the mask's negative sign
    /// (<c>-</c> anywhere a sign can land, or a non-blank CR/DB). Under EC-DATA-INCOMPATIBLE checking (ISO
    /// §14.6.13.2 rule 4: content "not a possible result for any editing operation in that data item" → the
    /// exception, the result undefined) the image is verified to be an editing result — <see cref="Format"/> of the
    /// de-edited value at the mask's scale (with the item's BLANK WHEN ZERO, <paramref name="blankWhenZero"/>) must
    /// reproduce it: Format IS the one editor, so the round trip is the exact test (kb/Work PB66 sweep — the
    /// floating-point form's <c>DeEditFloat</c> validates position by position); the fatal exception is raised
    /// before any receiver is written. With checking off the digit positions are read as before.</summary>
    public static Int128 DeEdit(string image, string picture, char currency = '$', bool commaMode = false,
        EditRule[]? edits = null, string? currencyString = null, bool blankWhenZero = false)
    {
        string physical = image;
        // A currency string other than the symbol itself: collapse the physical image to the logical one first
        // (§12.3.7.4 GR13 — the string is "de-edited from the data item when it is used as a sending item").
        if (currencyString is not null && currencyString != currency.ToString())
            image = CollapseCurrency(image, picture, currency, currencyString);
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
            // A user EDITING character-1 holds no digit; a sign-control (FOR) character-1 whose image char is its
            // NEGATIVE literal recovers the sign (ISO §13.18.40.5 — the de-editing MOVE, §14.9.25.4 GR5).
            if (edits is not null)
            {
                bool isChar1 = false;
                foreach (var e in edits)
                    if (p == char.ToUpperInvariant(e.Char1)) { isChar1 = true; if (e.Neg != e.Pos && c == e.Neg) negative = true; break; }
                if (isChar1) continue;
            }
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
        Int128 result = negative ? -value : value;
        if (ExceptionState.DataIncompatibleChecking)
        {
            string expected = Format(result, MaskScale(picture, currency, commaMode: false), picture, blankWhenZero,
                currency, commaMode: false, edits, currencyString);
            if (commaMode) expected = SwapSeparators(expected);
            if (expected != physical)
                ExceptionState.DataIncompatibleError(
                    $"the content '{physical}' is not a possible result of editing into {picture} (a de-editing MOVE, ISO 14.6.13.2 rule 4)");
        }
        return result;
    }

    /// <summary>Checked numeric→edited conversion for ARITHMETIC stores under ON SIZE ERROR (ISO §14.7.5 — size
    /// error condition case 3: after decimal-point alignment, |value| exceeds the mask's digit positions; storing
    /// rule 2: that resultant is left UNCHANGED and only the flag is set, the statement's other receivers still
    /// store). MOVE keeps the unchecked <see cref="Format"/> — high-order truncation IS the defined MOVE behavior
    /// (§14.9.25).</summary>
    public static bool TryFormat(Int128 value, int valueScale, string picture, out string image,
        bool blankWhenZero = false, char currency = '$', bool commaMode = false, EditRule[]? edits = null,
        string? currencyString = null)
    {
        var (capacity, fracDigits) = MaskCapacity(picture, currency, commaMode);
        // ⛔ The capacity test may never look at a WRAPPED alignment (kb/Work PB639): a widening the Int128
        // carrier cannot form is further from zero than any mask can show (§14.7.5 case 3), and the wrap can
        // land back inside `capacity` — a guard that silently passes exactly the magnitudes it exists to catch.
        if (!CobolNum.WideningFits(value, fracDigits - valueScale)) { image = string.Empty; return false; }
        Int128 scaled = CobolNum.Rescale(value, valueScale, fracDigits, CobolRounding.Truncation);
        if (Int128.Abs(scaled) >= CobolNum.Pow10Wide(capacity)) { image = string.Empty; return false; }
        image = Format(value, valueScale, picture, blankWhenZero, currency, commaMode, edits, currencyString);
        return true;
    }

    // ── The multi-character currency string (§13.18.40.4 GR14; kb/Work PB60 / AR-15.68.3-3) ────────────────────
    // The editing rules are written for a currency SYMBOL that occupies one logical position; the currency STRING
    // it stands for may have any length (§12.3.7.3 r23). GR14 sizes the item so that the FIRST occurrence of the
    // symbol contributes the whole string and every other occurrence one character — which is exactly "edit the
    // logical image, then let the ONE rendered currency position expand". Fixed insertion: the string sits where
    // the symbol sits (§13.18.40.5 r5). Floating insertion (r6a): the single rendered occurrence sits immediately
    // before the first nonzero digit / the first non-floating position / the decimal point, so it expands there;
    // a zero value under r6b renders NO currency at all ("all character positions will contain the space
    // character"), so the physical image is all spaces at the physical width. The collapse is the inverse.

    /// <summary>Logical → physical: the ONE rendered currency character (there is at most one — fixed insertion
    /// renders the symbol once, floating insertion lands it once) becomes the whole string; an image with no
    /// currency character (a floating zero, or BLANK WHEN ZERO) is padded to the physical width with leading
    /// spaces — every position is a space either way.</summary>
    private static string ExpandCurrency(string logical, char currency, string currencyString)
    {
        int idx = logical.IndexOf(char.ToUpperInvariant(currency));
        if (idx < 0) idx = logical.IndexOf(char.ToLowerInvariant(currency));
        if (idx < 0) return new string(' ', currencyString.Length - 1) + logical;
        return string.Concat(logical.AsSpan(0, idx), currencyString, logical.AsSpan(idx + 1));
    }

    /// <summary>Physical → logical, for de-editing (the inverse of <see cref="ExpandCurrency"/>): a FIXED currency
    /// symbol's string sits at the symbol's own mask position; a FLOATING string is wherever it landed — the first
    /// occurrence of the string in the image (§12.3.7.3 r23 keeps digits, the sign characters and the separators
    /// out of a currency string, and the mask's insertion characters cannot spell it inside its own zone); an image
    /// holding no string (a floating zero) simply drops the extra leading spaces.</summary>
    private static string CollapseCurrency(string image, string picture, char currency, string currencyString)
    {
        char currencyChar = char.ToUpperInvariant(currency);
        string pattern = picture.Replace("V", "").Replace("P", "");
        int occurrences = 0, fixedPos = -1;
        for (int i = 0; i < pattern.Length; i++)
            if (char.ToUpperInvariant(pattern[i]) == currencyChar) { occurrences++; fixedPos = i; }
        if (occurrences == 0) return image;
        int extra = currencyString.Length - 1;
        int idx = occurrences == 1 && fixedPos + currencyString.Length <= image.Length
                  && string.CompareOrdinal(image, fixedPos, currencyString, 0, currencyString.Length) == 0
            ? fixedPos
            : image.IndexOf(currencyString, StringComparison.Ordinal);
        if (idx < 0)
        {
            // No string rendered (a floating zero under r6b, or a BLANK WHEN ZERO image): drop the extra spaces.
            int drop = Math.Min(extra, image.Length);
            return image[drop..];
        }
        return string.Concat(image.AsSpan(0, idx), currencyChar.ToString(), image.AsSpan(idx + currencyString.Length));
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
        // ⛔ The leading/trailing split anchors on EVERY digit position — 9/Z/* AND a FLOATING string's member
        // occurrences (§13.18.40.6 Table 10 puts 'P (left of decimal point)' beside floating cs and +/−) — never
        // on only the literal 9/Z/* (kb/Work PB155: `PIC $$$$PP` has no 9/Z/* at all, so its rightmost P run
        // read as LEADING and the mask scale came out +2 where the value is a multiple of 10^2, scale −2).
        // A FIXED single +/−/cs is not a digit position and must not anchor (a trailing fixed sign sits right
        // of a trailing P run: 99PPCR).
        int pCount = 0;
        foreach (char raw in picture) if (char.ToUpperInvariant(raw) == 'P') pCount++;
        if (pCount > 0)
        {
            string up = picture.ToUpperInvariant();
            bool IsDigitPos(char c) => c is '9' or 'Z' or '*'
                || (c == currencyChar && !fixedCs) || (c == '+' && !fixedPlus) || (c == '-' && !fixedMinus);
            int lastDigitPos = -1, digitPositions = 0;
            bool sawFloating = false;
            for (int i = 0; i < up.Length; i++)
                if (IsDigitPos(up[i]))
                {
                    lastDigitPos = i;
                    // the LEFTMOST occurrence of a floating string is the sign/currency itself, not a digit
                    if (up[i] is not ('9' or 'Z' or '*') && !sawFloating) { sawFloating = true; continue; }
                    digitPositions++;
                }
            if (lastDigitPos >= 0 && up.IndexOf('P', lastDigitPos) > lastDigitPos) return -pCount;
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

    /// <summary>ALPHANUMERIC-EDITED formatting (ISO §13.18.40.4 GR7 — "at least one symbol 'A' or one symbol 'X',
    /// and at least one instance of character-1 or one of the symbols from the set 'B', '0', '/'"; §13.18.40.5
    /// Table 7 gives the category SIMPLE INSERTION and nothing else): source characters fill the X/A/9 positions
    /// left-to-right (space-padded when exhausted) and every insertion position supplies its character from the
    /// ONE set (<see cref="TrySimpleInsertion"/>) — rule 3, "the insertion character occupying the same character
    /// position in the edited item as the associated symbol occupies in character-string-1".
    /// <para>⛔ <paramref name="edits"/> is the same <c>EditRule[]</c> channel the numeric-edited path takes, and
    /// it is not optional in practice: GR7 names character-1 as an alphanumeric-edited constituent, so
    /// <c>PIC XXTXX EDITING "T" IS ":"</c> renders <c>AB:CD</c>, not the mask LETTER (kb/Work PB490 — this arm of
    /// the dispatch had no rules parameter at all and every emit site dropped <c>PicInfo.EditingRules</c>; the
    /// compiler now reaches this method only through <c>RuntimeApi.EditFormatAlphanumeric(value, pic)</c>, which
    /// renders the mask and the rules together from the one <c>PicInfo</c>). A FOR-phrase character-1 cannot occur
    /// here — §13.18.40.3 SR12 limits it to numeric and numeric-edited items and SR12 b) to the symbols
    /// 9 . cs P V Z — so every rule reaching this method is simple insertion.</para></summary>
    public static string FormatAlphanumeric(string source, string picture, EditRule[]? edits = null)
    {
        var output = new char[picture.Length];
        int si = 0;
        for (int i = 0; i < picture.Length; i++)
        {
            char p = char.ToUpperInvariant(picture[i]);
            output[i] = p is 'X' or 'A' or '9' ? si < source.Length ? source[si++] : ' '
                : TrySimpleInsertion(picture[i], edits, out char ins) ? ins
                : picture[i];
        }
        return new string(output);
    }

    /// <summary>The rightmost suppressed position within a floating symbol's string — its own occurrences plus the
    /// SIMPLE INSERTION symbols the string absorbs (ISO §13.18.40.5 rule 6: "Any of the simple insertion editing
    /// symbols embedded in this string or to the immediate right of this string are part of the string"), read
    /// from the ONE set (<see cref="TrySimpleInsertion"/>). Rule 6 a) then lands "a single occurrence of the
    /// replacement character(s) … into the character position(s) immediately preceding whichever of the following
    /// is encountered first" — the first nonzero numeric character, the first character position for which no
    /// floating insertion editing is specified, or the decimal point position — which is exactly the rightmost
    /// position of the string that pass 2 left suppressed; "Any character positions preceding this (these)
    /// insertion character(s) will contain the space character", which pass 2 has already done.</summary>
    private static int FindFloatingPlacement(string pattern, char[] output, char floatChar, EditRule[]? edits)
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
            else if (inZone && output[i] == ' ' && TrySimpleInsertion(pattern[i], edits, out _)) lastSuppressed = i;
            else if (inZone) break;
        }
        return lastSuppressed;
    }
}
