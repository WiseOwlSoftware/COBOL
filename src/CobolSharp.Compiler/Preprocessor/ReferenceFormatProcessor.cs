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
    /// Tracks literal/quote state across lines to correctly handle doubled-quotes
    /// ("") that straddle continuation boundaries (ISO §6.2.2).
    /// </summary>
    public static string ConvertFixedToFree(string sourceText)
    {
        var lines = sourceText.Split('\n');
        var result = new StringBuilder();

        // Literal state carried across lines for continuation decisions
        bool inLiteral = false;
        bool pendingQuote = false; // true when last char was first half of potential "" pair

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (line.Length < SourceAreaStart)
            {
                result.AppendLine();
                inLiteral = false;
                pendingQuote = false;
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
                    inLiteral = false;
                    pendingQuote = false;
                    break;

                case 'D' or 'd' or 'S' or 's' or 'Y' or 'y':
                    result.AppendLine($"*> DEBUG: {sourceArea.TrimEnd()}");
                    inLiteral = false;
                    pendingQuote = false;
                    break;

                case '-':
                    HandleContinuation(result, sourceArea,
                        ref inLiteral, ref pendingQuote);
                    break;

                default:
                    // Normal line: emit immediately.
                    // Preserve trailing spaces when inside an unclosed literal.
                    inLiteral = false;
                    pendingQuote = false;
                    ScanLiteralState(sourceArea, ref inLiteral, ref pendingQuote);
                    result.AppendLine(inLiteral ? sourceArea : sourceArea.TrimEnd());
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Scan a source area to determine the literal/quote state at end of line.
    /// Does NOT modify the content — only updates inLiteral and pendingQuote.
    /// </summary>
    private static void ScanLiteralState(string sourceArea,
        ref bool inLiteral, ref bool pendingQuote)
    {
        for (int i = 0; i < sourceArea.Length; i++)
        {
            char c = sourceArea[i];

            if (c is not ('"' or '\''))
            {
                if (pendingQuote)
                {
                    // Previous quote was NOT doubled — it closed the literal
                    inLiteral = false;
                    pendingQuote = false;
                }
                continue;
            }

            // c is a quote character
            if (!inLiteral)
            {
                inLiteral = true;
                pendingQuote = false;
            }
            else if (pendingQuote)
            {
                // Second half of a doubled-quote pair
                pendingQuote = false;
            }
            else
            {
                // First quote inside a literal — could be closing or first half of ""
                pendingQuote = true;
            }
        }
    }

    /// <summary>
    /// Handle a continuation line (column 7 = '-'). Joins to the previous line.
    /// When continuing a literal, decides whether the continuation's opening quote is:
    /// - a resume marker (strip it) — normal case
    /// - the second half of a "" pair (keep it) — when previous line ended mid-pair
    /// </summary>
    private static void HandleContinuation(StringBuilder result, string sourceArea,
        ref bool inLiteral, ref bool pendingQuote)
    {
        // Remove the last newline from result to join with previous line
        while (result.Length > 0 && result[result.Length - 1] is '\n' or '\r')
            result.Length--;

        if (!inLiteral)
        {
            // Not in a literal — strip trailing spaces from previous line and
            // append trimmed continuation content (§6.2.4: first non-space of
            // continuation immediately follows last non-space of preceding line)
            while (result.Length > 0 && result[result.Length - 1] == ' ')
                result.Length--;
            string trimmed = sourceArea.TrimStart();
            result.AppendLine(trimmed);
            ScanLiteralState(trimmed, ref inLiteral, ref pendingQuote);
            return;
        }

        // We're inside an unclosed literal from the previous line.
        // Find the first non-blank character.
        int firstNonBlank = 0;
        while (firstNonBlank < sourceArea.Length && sourceArea[firstNonBlank] == ' ')
            firstNonBlank++;

        if (firstNonBlank >= sourceArea.Length)
        {
            result.AppendLine();
            return;
        }

        char firstChar = sourceArea[firstNonBlank];

        if (firstChar is not ('"' or '\''))
        {
            // Not a quote — non-literal continuation
            result.Append(sourceArea[firstNonBlank..]);
            result.AppendLine();
            ScanLiteralState(sourceArea[firstNonBlank..], ref inLiteral, ref pendingQuote);
            return;
        }

        if (pendingQuote)
        {
            // Previous line ended with the first half of a "" pair.
            // This quote is the SECOND HALF — data, not a resume marker.
            // Keep this quote (append it).
            pendingQuote = false;

            // Check if the NEXT character is also a quote — if so, it's
            // the resume marker for the continuing literal and must be stripped.
            int contentStart = firstNonBlank + 1;
            if (contentStart < sourceArea.Length && sourceArea[contentStart] is '"' or '\'')
            {
                // Append the paired quote, then skip the resume marker
                result.Append(sourceArea[firstNonBlank]);
                result.Append(sourceArea[(contentStart + 1)..]);
                result.AppendLine();
                ScanLiteralState(sourceArea[(contentStart + 1)..], ref inLiteral, ref pendingQuote);
            }
            else
            {
                // No resume marker follows — just append everything
                result.Append(sourceArea[firstNonBlank..]);
                result.AppendLine();
                ScanLiteralState(sourceArea[(firstNonBlank + 1)..], ref inLiteral, ref pendingQuote);
            }
        }
        else
        {
            // Normal literal continuation: strip the resume marker quote.
            result.Append(sourceArea[(firstNonBlank + 1)..]);
            result.AppendLine();
            // Re-scan the appended content (after the stripped quote)
            ScanLiteralState(sourceArea[(firstNonBlank + 1)..], ref inLiteral, ref pendingQuote);
        }
    }
}
