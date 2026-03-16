// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
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
        string body = picBody.Trim().ToUpperInvariant();

        // Expand repeat notation: 9(5) → 99999
        string expanded = ExpandPattern(body);

        // Detect category
        bool hasEditing = ContainsEditChars(expanded);
        bool hasNumericChars = ContainsNumericChars(body);
        bool hasAlphaChars = body.Contains('X') || body.Contains('A');

        CobolCategory category;
        if (hasNumericChars && !hasAlphaChars)
            category = hasEditing ? CobolCategory.NumericEdited : CobolCategory.Numeric;
        else if (hasAlphaChars)
            category = hasEditing ? CobolCategory.AlphanumericEdited : CobolCategory.Alphanumeric;
        else
            category = CobolCategory.Alphanumeric;

        // Count digits and fractions
        int totalDigits = 0;
        int fractionDigits = 0;
        int leadingScaleDigits = 0;
        bool pastDecimal = false;
        bool hasRealDigits = false;

        foreach (char c in expanded)
        {
            switch (c)
            {
                case 'S':
                    isSigned = true;
                    break;
                case '9':
                    hasRealDigits = true;
                    totalDigits++;
                    if (pastDecimal) fractionDigits++;
                    break;
                case 'V':
                    pastDecimal = true;
                    break;
                case 'Z': case '*': case '+': case '-': case '$':
                    totalDigits++;
                    if (pastDecimal) fractionDigits++;
                    break;
                case '.':
                    if (hasEditing) pastDecimal = true;
                    break;
                case 'P':
                    if (!hasRealDigits) leadingScaleDigits++;
                    break;
            }
        }

        // Compute storage length
        int storageLength;
        if (category == CobolCategory.Alphanumeric || category == CobolCategory.AlphanumericEdited)
        {
            storageLength = expanded.Replace("S", "").Length;
        }
        else if (hasEditing)
        {
            // Edited: storage = expanded pattern length minus S
            storageLength = expanded.Replace("S", "").Replace("V", "").Length;
        }
        else
        {
            // Pure numeric: digits only
            storageLength = totalDigits;
            if (isSigned && (signStorage is SignStorageKind.LeadingSeparate or SignStorageKind.TrailingSeparate))
                storageLength++;
        }

        // Determine editing kind
        var editingKind = EditingKind.None;
        if (hasEditing)
        {
            if (expanded.Contains("CR") || expanded.Contains("DB"))
                editingKind = EditingKind.CreditDebit;
            else if (expanded.Contains('$'))
                editingKind = EditingKind.Currency;
            else if (expanded.Contains('Z') || expanded.Contains('*'))
                editingKind = EditingKind.ZeroSuppress;
            else
                editingKind = EditingKind.Custom;
        }

        // Edit pattern: only for numeric-edited, use expanded form minus S and V
        string? editPattern = null;
        if (category == CobolCategory.NumericEdited)
        {
            editPattern = expanded.Replace("S", "").Replace("V", "");
        }

        return new PicDescriptor(
            totalDigits: totalDigits,
            fractionDigits: fractionDigits,
            isSigned: isSigned,
            isNumeric: category.IsNumericLike(),
            isAlphanumeric: category.IsAlphanumericLike(),
            hasEditing: hasEditing,
            storageLength: storageLength,
            usage: usage,
            category: category,
            signStorage: signStorage,
            editing: editingKind,
            blankWhenZero: blankWhenZero,
            leadingScaleDigits: leadingScaleDigits,
            trailingScaleDigits: 0,
            editPattern: editPattern);
    }

    /// <summary>
    /// Expand PIC repeat notation: "9(5)" → "99999", "-9(9).9(9)" → "-999999999.999999999".
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

    private static bool ContainsEditChars(string expanded)
    {
        foreach (char c in expanded)
        {
            if ("Z*+-$B/,".Contains(c)) return true;
            // '.' is editing only if there are also numeric edit chars
            // '-' at start is a sign position in editing
        }
        return false;
    }

    private static bool ContainsNumericChars(string body)
    {
        foreach (char c in body)
        {
            if ("9SVSP".Contains(c)) return true;
        }
        return false;
    }
}
