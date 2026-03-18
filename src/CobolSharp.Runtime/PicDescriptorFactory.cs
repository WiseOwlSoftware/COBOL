// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime;

/// <summary>
/// Canonical factory for creating PicDescriptor from a PIC clause string.
/// Shared between compiler (for layout/analysis) and runtime (for tests).
/// </summary>
public static class PicDescriptorFactory
{
    /// <summary>
    /// Create a PicDescriptor from a raw PIC body string (e.g., "S9(5)V99", "ZZ,ZZ9.99").
    /// Does not include the "PIC" or "PICTURE" keyword — just the pattern.
    /// </summary>
    public static PicDescriptor FromPicBody(
        string picBody,
        UsageKind usage = UsageKind.Display,
        bool isSigned = false,
        SignStorageKind signStorage = SignStorageKind.None,
        bool blankWhenZero = false)
    {
        var text = picBody.Trim().ToUpperInvariant();

        // Pre-scan: count $, +, - to distinguish fixed (single) vs floating (multiple).
        // Fixed symbols are literal insertions, NOT digit positions.
        // Floating symbols act as digit positions for zero suppression.
        int dollarTotal = 0, plusTotal = 0, minusTotal = 0;
        foreach (char ch in text)
        {
            if (ch == '$') dollarTotal++;
            else if (ch == '+') plusTotal++;
            else if (ch == '-') minusTotal++;
        }
        bool singleDollar = dollarTotal == 1;
        bool singlePlus = plusTotal == 1 && minusTotal == 0;
        bool singleMinus = minusTotal == 1 && plusTotal == 0;

        int pos = 0;

        int integerDigits = 0;
        int fractionDigits = 0;
        bool pastDecimal = false;
        bool edited = false;

        bool hasNumericChars = false;   // 9, V, P, S
        bool hasAlphaChars = false;     // X, A
        bool hasNationalChars = false;  // N
        bool hasRealDigits = false;     // any 9/edited-digit

        int leadingPScaling = 0;
        int trailingPScaling = 0;
        int insertionChars = 0;         // ., B, /, , etc.

        while (pos < text.Length)
        {
            char c = text[pos];

            switch (c)
            {
                case 'S':
                    isSigned = true;
                    hasNumericChars = true;
                    pos++;
                    break;

                case '9':
                {
                    hasNumericChars = true;
                    hasRealDigits = true;
                    int count = ParseRepeatCount(text, ref pos);
                    if (pastDecimal)
                        fractionDigits += count;
                    else
                        integerDigits += count;
                    break;
                }

                case 'V':
                    hasNumericChars = true;
                    pastDecimal = true;
                    pos++;
                    break;

                case 'X':
                {
                    hasAlphaChars = true;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'A':
                {
                    hasAlphaChars = true;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'N':
                {
                    hasNationalChars = true;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'P':
                {
                    hasNumericChars = true;
                    int count = ParseRepeatCount(text, ref pos);
                    if (!hasRealDigits)
                        leadingPScaling += count;
                    else
                        trailingPScaling += count;
                    break;
                }

                case '.':
                {
                    edited = true;
                    pastDecimal = true;
                    insertionChars++;
                    pos++;
                    break;
                }

                case ',':
                case 'B':
                case '/':
                {
                    edited = true;
                    insertionChars++;
                    pos++;
                    break;
                }

                case 'Z':
                case '*':
                {
                    // Always digit positions (floating suppression)
                    edited = true;
                    int count = ParseRepeatCount(text, ref pos);
                    if (pastDecimal)
                        fractionDigits += count;
                    else
                        integerDigits += count;
                    hasNumericChars = true;
                    hasRealDigits = true;
                    break;
                }

                case '$':
                {
                    edited = true;
                    int count = ParseRepeatCount(text, ref pos);
                    hasNumericChars = true;
                    hasRealDigits = true;
                    if (singleDollar)
                    {
                        // Fixed currency: literal insertion, NOT a digit position
                        insertionChars += count;
                    }
                    else
                    {
                        // Floating currency: digit positions
                        if (pastDecimal)
                            fractionDigits += count;
                        else
                            integerDigits += count;
                    }
                    break;
                }

                case '+':
                case '-':
                {
                    edited = true;
                    int count = ParseRepeatCount(text, ref pos);
                    hasNumericChars = true;
                    hasRealDigits = true;
                    bool isFixed = (c == '+' && singlePlus) || (c == '-' && singleMinus);
                    if (isFixed)
                    {
                        // Fixed sign: literal insertion, NOT a digit position
                        insertionChars += count;
                    }
                    else
                    {
                        // Floating sign: digit positions
                        if (pastDecimal)
                            fractionDigits += count;
                        else
                            integerDigits += count;
                    }
                    break;
                }

                case '0':
                {
                    // Zero insertion character, NOT a digit position
                    edited = true;
                    int count = ParseRepeatCount(text, ref pos);
                    insertionChars += count;
                    hasNumericChars = true;
                    break;
                }

                case 'C':
                    if (pos + 1 < text.Length && text[pos + 1] == 'R')
                    {
                        edited = true;
                        insertionChars += 2;
                        pos += 2;
                    }
                    else pos++;
                    break;

                case 'D':
                    if (pos + 1 < text.Length && text[pos + 1] == 'B')
                    {
                        edited = true;
                        insertionChars += 2;
                        pos += 2;
                    }
                    else pos++;
                    break;

                default:
                    pos++;
                    break;
            }
        }

        // Leading P with no explicit decimal: digits after leading P are fractional.
        if (!pastDecimal && leadingPScaling > 0 && integerDigits > 0 && fractionDigits == 0)
        {
            fractionDigits = integerDigits;
            integerDigits = 0;
        }

        int totalDigits = integerDigits + fractionDigits;

        // Category lattice (mirrors PicUsageResolver)
        CobolCategory category;
        if (hasNumericChars && !hasAlphaChars && !hasNationalChars)
            category = edited ? CobolCategory.NumericEdited : CobolCategory.Numeric;
        else if (hasNationalChars && !hasNumericChars && !hasAlphaChars)
            category = edited ? CobolCategory.NationalEdited : CobolCategory.National;
        else if (hasAlphaChars || (!hasNumericChars && !hasNationalChars && edited))
            category = edited ? CobolCategory.AlphanumericEdited : CobolCategory.Alphanumeric;
        else if (edited && hasNumericChars)
            category = CobolCategory.NumericEdited;
        else
            category = CobolCategory.Unknown;

        // Storage length (DISPLAY/NATIONAL only; COMP/COMP-3 handled by EncodeNumeric)
        int storageLength = ComputeDisplayStorageLength(
            category,
            integerDigits,
            fractionDigits,
            insertionChars,
            isSigned,
            signStorage);

        // Editing kind
        var editingKind = EditingKind.None;
        if (edited)
        {
            if (text.Contains("CR") || text.Contains("DB"))
                editingKind = EditingKind.CreditDebit;
            else if (text.Contains('$'))
                editingKind = EditingKind.Currency;
            else if (text.Contains('Z') || text.Contains('*') || blankWhenZero)
                editingKind = EditingKind.ZeroSuppress;
            else
                editingKind = EditingKind.Custom;
        }

        // Expanded edit pattern (for numeric-edited and alphanumeric-edited)
        string? editPattern = null;
        if (category == CobolCategory.NumericEdited)
        {
            var expanded = ExpandPattern(text);
            editPattern = expanded
                .Replace("S", string.Empty)
                .Replace("V", string.Empty)
                .Replace("P", string.Empty);
        }
        else if (category == CobolCategory.AlphanumericEdited)
        {
            editPattern = ExpandPattern(text);
        }

        return new PicDescriptor(
            totalDigits: totalDigits,
            fractionDigits: fractionDigits,
            isSigned: isSigned,
            isNumeric: category.IsNumericLike(),
            isAlphanumeric: category.IsAlphanumericLike(),
            hasEditing: edited,
            storageLength: storageLength,
            usage: usage,
            category: category,
            signStorage: signStorage,
            editing: editingKind,
            blankWhenZero: blankWhenZero,
            leadingScaleDigits: leadingPScaling,
            trailingScaleDigits: trailingPScaling,
            editPattern: editPattern);
    }

    /// <summary>
    /// Expand PIC repeat notation: "9(5)" → "99999", "-9(9).9(9)" → "-999999999.999999999".
    /// P is expanded as-is here; callers decide whether it contributes to storage.
    /// </summary>
    public static string ExpandPattern(string pic)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < pic.Length; i++)
        {
            char c = pic[i];
            if (i + 1 < pic.Length && pic[i + 1] == '(')
            {
                int start = i + 2;
                int end = start;
                while (end < pic.Length && char.IsDigit(pic[end])) end++;
                if (end < pic.Length && pic[end] == ')' && end > start)
                {
                    int count = int.Parse(pic.Substring(start, end - start));
                    for (int k = 0; k < count; k++)
                        sb.Append(c);
                    i = end;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static int ParseRepeatCount(string text, ref int pos)
    {
        pos++; // consume the letter

        if (pos >= text.Length || text[pos] != '(')
            return 1;

        int start = ++pos; // skip '('
        while (pos < text.Length && char.IsDigit(text[pos]))
            pos++;

        if (pos >= text.Length || text[pos] != ')')
            return 1;

        var numText = text.Substring(start, pos - start);
        pos++; // skip ')'

        return int.TryParse(numText, out var n) && n > 0 ? n : 1;
    }

    private static int ComputeDisplayStorageLength(
        CobolCategory category,
        int integerDigits,
        int fractionDigits,
        int insertionChars,
        bool isSigned,
        SignStorageKind signStorage)
    {
        int baseLength = integerDigits + fractionDigits + insertionChars;

        // For alpha/national, integerDigits already counts characters.
        int length = baseLength;

        if (isSigned &&
            (signStorage == SignStorageKind.LeadingSeparate ||
             signStorage == SignStorageKind.TrailingSeparate))
        {
            length++;
        }

        return length;
    }
}
