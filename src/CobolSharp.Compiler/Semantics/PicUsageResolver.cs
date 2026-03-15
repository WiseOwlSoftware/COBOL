// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Resolves a data item's PIC string + USAGE clause into a concrete ITypeSymbol.
/// Called by the binder when creating DataSymbols.
/// </summary>
public static class PicUsageResolver
{
    public static ITypeSymbol ResolveForDataItem(
        string dataName,
        string? picString,
        UsageKind usage,
        DiagnosticBag diagnostics,
        int line)
    {
        PicLayout? layout = null;

        if (picString != null)
        {
            layout = ParsePic(picString, diagnostics, line);
        }

        var category = layout?.Category ?? CobolCategory.Unknown;
        bool isNumeric = category.IsNumericLike();
        bool isAlpha = category.IsAlphanumericLike();
        bool isBool = false;

        // Group items (no PIC) are alphanumeric by default
        if (picString == null && usage == UsageKind.Display)
        {
            isAlpha = true;
            category = CobolCategory.Alphanumeric;
        }

        var name = BuildTypeName(dataName, layout, usage);
        return new DataTypeSymbol(name, isNumeric, isAlpha, isBool, layout, usage);
    }

    private static string BuildTypeName(string dataName, PicLayout? pic, UsageKind usage)
    {
        if (pic == null)
            return $"{dataName}:{usage}";
        return $"{dataName}:PIC({pic.Category},{pic.Length})/{usage}";
    }

    /// <summary>
    /// Parse a PIC string into a PicLayout with CobolCategory classification.
    /// </summary>
    private static PicLayout ParsePic(string picString, DiagnosticBag diagnostics, int line)
    {
        var text = picString.Trim();
        int pos = 0;
        bool signed = false;
        int integerDigits = 0;
        int fractionDigits = 0;
        bool pastDecimal = false;
        bool edited = false;
        bool hasNumericChars = false;   // 9, V, P, S
        bool hasAlphaChars = false;     // X, A
        bool hasNationalChars = false;  // N

        while (pos < text.Length)
        {
            char c = char.ToUpperInvariant(text[pos]);

            switch (c)
            {
                case 'S':
                    signed = true;
                    hasNumericChars = true;
                    pos++;
                    break;

                case '9':
                {
                    hasNumericChars = true;
                    int count = ParseRepeatCount(text, ref pos);
                    if (pastDecimal)
                        fractionDigits += count;
                    else
                        integerDigits += count;
                    break;
                }

                case 'V':
                    // Implied decimal point
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
                    // Scaling position
                    hasNumericChars = true;
                    int count = ParseRepeatCount(text, ref pos);
                    if (pastDecimal)
                        fractionDigits += count;
                    else
                        integerDigits += count;
                    break;
                }

                case 'Z':
                case '*':
                case '+':
                case '-':
                case '$':
                case 'B':
                case '0':
                case '/':
                case ',':
                case '.':
                {
                    // Editing symbols
                    edited = true;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'C':
                    // CR
                    if (pos + 1 < text.Length && char.ToUpperInvariant(text[pos + 1]) == 'R')
                    {
                        edited = true;
                        pos += 2;
                    }
                    else pos++;
                    break;

                case 'D':
                    // DB
                    if (pos + 1 < text.Length && char.ToUpperInvariant(text[pos + 1]) == 'B')
                    {
                        edited = true;
                        pos += 2;
                    }
                    else pos++;
                    break;

                default:
                    // Unknown PIC character — skip
                    pos++;
                    break;
            }
        }

        int length = integerDigits + fractionDigits;

        // Classify into CobolCategory using the full lattice
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

        return new PicLayout(category, length, integerDigits, fractionDigits, signed, edited);
    }

    /// <summary>
    /// At current position (pointing to a PIC character like 9, X, A),
    /// consume it and check for (n) repeat count.
    /// Returns the repeat count (1 if no parentheses).
    /// </summary>
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
}

/// <summary>
/// Maps USAGE keyword text to UsageKind enum.
/// </summary>
public static class UsageMapper
{
    public static UsageKind FromUsageKeyword(string? keyword)
    {
        if (keyword == null)
            return UsageKind.Display;

        return keyword.ToUpperInvariant() switch
        {
            "DISPLAY" => UsageKind.Display,
            "COMP" or "COMPUTATIONAL" => UsageKind.Comp,
            "COMP-1" or "COMPUTATIONAL-1" => UsageKind.Comp1,
            "COMP-2" or "COMPUTATIONAL-2" => UsageKind.Comp2,
            "COMP-3" or "COMPUTATIONAL-3" => UsageKind.Comp3,
            "BINARY" => UsageKind.Binary,
            "PACKED-DECIMAL" => UsageKind.PackedDecimal,
            "INDEX" => UsageKind.Index,
            "POINTER" => UsageKind.Pointer,
            _ => UsageKind.Object
        };
    }
}
