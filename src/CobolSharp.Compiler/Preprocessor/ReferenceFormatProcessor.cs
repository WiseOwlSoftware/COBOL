// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Compiler.Preprocessor;

/// <summary>
/// Detects and converts fixed-form COBOL reference format to free-form.
/// Fixed-form: columns 1-6 sequence, 7 indicator, 8-72 source, 73+ comment.
/// Free-form: no column restrictions.
/// </summary>
public static class ReferenceFormatProcessor
{
    /// <summary>Length of the sequence number area (columns 1-6).</summary>
    private const int SequenceAreaLength = 6;

    /// <summary>Column index of the indicator area (column 7, zero-based index 6).</summary>
    private const int IndicatorColumn = 6;

    /// <summary>Column index where the source area begins (column 8, zero-based index 7).</summary>
    private const int SourceAreaStart = 7;

    /// <summary>Maximum width of the source area (columns 8-72 = 65 characters).</summary>
    private const int SourceAreaWidth = 65;

    /// <summary>Minimum percentage of lines that must match fixed-form pattern for detection.</summary>
    private const int FixedFormThresholdPercent = 60;

    /// <summary>
    /// Auto-detect whether source is fixed-form or free-form, and normalize to free-form.
    /// </summary>
    public static string NormalizeToFreeForm(string sourceText)
    {
        return IsFixedForm(sourceText) ? ConvertFixedToFree(sourceText) : sourceText;
    }

    /// <summary>
    /// Heuristic detection of fixed-form. Checks:
    /// - Lines are consistently >= 7 chars
    /// - Column 7 often contains space, *, or -
    /// - Columns 1-6 are often digits or spaces
    /// </summary>
    public static bool IsFixedForm(string sourceText)
    {
        if (sourceText.Contains("*>"))
            return false;

        var lines = sourceText.Split('\n');
        int fixedIndicators = 0;
        int totalLines = 0;
        bool hasNumericSequence = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            totalLines++;

            if (line.Length > IndicatorColumn)
            {
                char indicator = line[IndicatorColumn];
                if (indicator is ' ' or '*' or '/' or 'D' or 'd' or '-')
                {
                    bool seqOk = true;
                    for (int i = 0; i < SequenceAreaLength && i < line.Length; i++)
                    {
                        if (!char.IsDigit(line[i]) && line[i] != ' ')
                        {
                            seqOk = false;
                            break;
                        }
                    }
                    if (seqOk)
                    {
                        fixedIndicators++;
                        for (int i = 0; i < SequenceAreaLength && i < line.Length; i++)
                        {
                            if (char.IsDigit(line[i]))
                            {
                                hasNumericSequence = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        return totalLines > 0 && hasNumericSequence &&
               fixedIndicators * 100 / totalLines > FixedFormThresholdPercent;
    }

    /// <summary>
    /// Convert fixed-form source to free-form by stripping columns 1-6 and 73+,
    /// handling indicator column (7), and joining continuation lines.
    /// </summary>
    public static string ConvertFixedToFree(string sourceText)
    {
        var lines = sourceText.Split('\n');
        var result = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (line.Length < SourceAreaStart)
            {
                result.AppendLine();
                continue;
            }

            char indicator = line[IndicatorColumn];
            string sourceArea = line[SourceAreaStart..];
            if (sourceArea.Length > SourceAreaWidth)
                sourceArea = sourceArea[..SourceAreaWidth];

            switch (indicator)
            {
                case '*' or '/':
                    result.AppendLine($"*> {sourceArea.TrimEnd()}");
                    break;

                case 'D' or 'd' or 'S' or 's' or 'Y' or 'y':
                    result.AppendLine($"*> DEBUG: {sourceArea.TrimEnd()}");
                    break;

                case '-':
                    HandleContinuation(result, sourceArea);
                    break;

                default:
                    // Preserve trailing spaces when inside an unclosed string literal —
                    // these spaces are part of the literal content and must survive for
                    // continuation line joining (ISO §6.2.2).
                    result.AppendLine(HasUnclosedString(sourceArea)
                        ? sourceArea : sourceArea.TrimEnd());
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Returns true if the source area ends with an unclosed string literal
    /// (odd number of unescaped quotes). Trailing spaces after an unclosed literal
    /// are part of the literal content and must not be trimmed.
    /// </summary>
    private static bool HasUnclosedString(string line)
    {
        bool inString = false;
        char quoteChar = '\0';
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (!inString && c is '"' or '\'')
            {
                inString = true;
                quoteChar = c;
            }
            else if (inString && c == quoteChar)
            {
                if (i + 1 < line.Length && line[i + 1] == quoteChar)
                    i++; // skip escaped (doubled) quote
                else
                    inString = false;
            }
        }
        return inString;
    }

    private static void HandleContinuation(StringBuilder result, string sourceArea)
    {
        // Remove the last newline from result
        while (result.Length > 0 && result[result.Length - 1] is '\n' or '\r')
            result.Length--;

        // Per ISO 6.2.2: if continuation starts with a quote (after spaces),
        // it's continuing a non-numeric literal. The opening quote on the
        // continuation line replaces the continuation point.
        string trimmedCont = sourceArea.TrimStart();
        if (trimmedCont.Length > 0 && trimmedCont[0] is '"' or '\'')
        {
            result.Append(trimmedCont[1..]);
            result.AppendLine();
        }
        else
        {
            result.AppendLine(trimmedCont);
        }
    }
}
