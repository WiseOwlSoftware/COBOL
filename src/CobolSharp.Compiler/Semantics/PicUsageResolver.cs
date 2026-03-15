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

        var category = layout?.Category ?? PicCategory.Unknown;
        bool isNumeric = category == PicCategory.Numeric;
        bool isAlpha = category == PicCategory.Alphanumeric;
        bool isBool = category == PicCategory.Boolean;

        // Group items (no PIC) are alphanumeric by default
        if (picString == null && usage == UsageKind.Display)
            isAlpha = true;

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
    /// Parse a PIC string into a PicLayout. First-pass: classifies numeric vs
    /// alphanumeric, computes length/scale/sign. Conservative — flags editing
    /// symbols for later refinement.
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
        var category = PicCategory.Unknown;

        while (pos < text.Length)
        {
            char c = char.ToUpperInvariant(text[pos]);

            switch (c)
            {
                case 'S':
                    signed = true;
                    pos++;
                    break;

                case '9':
                {
                    category = PicCategory.Numeric;
                    int count = ParseRepeatCount(text, ref pos);
                    if (pastDecimal)
                        fractionDigits += count;
                    else
                        integerDigits += count;
                    break;
                }

                case 'V':
                    // Implied decimal point
                    pastDecimal = true;
                    pos++;
                    break;

                case 'X':
                {
                    if (category == PicCategory.Unknown)
                        category = PicCategory.Alphanumeric;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'A':
                {
                    if (category == PicCategory.Unknown)
                        category = PicCategory.Alphanumeric;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'N':
                {
                    if (category == PicCategory.Unknown)
                        category = PicCategory.National;
                    int count = ParseRepeatCount(text, ref pos);
                    integerDigits += count;
                    break;
                }

                case 'P':
                {
                    // Scaling position
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
                    if (category == PicCategory.Unknown)
                        category = PicCategory.Edited;
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

        if (category == PicCategory.Unknown)
            category = edited ? PicCategory.Edited : PicCategory.Unknown;

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
