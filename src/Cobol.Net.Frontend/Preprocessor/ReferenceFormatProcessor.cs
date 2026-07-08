// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

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

    /// <summary>Width of Area A (columns 8-11). A division/section/paragraph header begins here;
    /// continuation/free text of a comment-entry is indented into Area B (column 12+).</summary>
    private const int AreaAWidth = 4;

    /// <summary>
    /// The obsolete IDENTIFICATION DIVISION "comment-entry" paragraphs (ISO 1989:1985 §III; obsolete,
    /// removed in COBOL-2002). Their content is free-form commentary — any characters, including embedded
    /// periods, reserved words, numbers and quoted strings, spanning one or more lines until the next
    /// Area-A header. That free text cannot be reliably bounded by a token grammar (e.g. the FCTC address
    /// in RW101A's INSTALLATION contains "...AUTOMATED DATA AND..." and "5203 LEESBURG PIKE"), so we treat
    /// the whole paragraph as commentary in the column-aware preprocessor: comment out the header and its
    /// content up to the next Area-A header. The paragraphs are optional (identificationParagraph*), so the
    /// parser simply never sees them — no grammar change, no embedded-period/terminating-period edge cases.
    /// </summary>
    private static readonly HashSet<string> CommentEntryParagraphs = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUTHOR", "INSTALLATION", "DATE-WRITTEN", "DATE-COMPILED", "SECURITY", "REMARKS",
    };

    /// <summary>
    /// Remove NIST CCVS archive-control lines ('*HEADER,…' and '*END-OF,…') that delimit
    /// members inside newcob.val. They begin in column 1 (the sequence area), so reference-format
    /// normalization would otherwise read column 7 as an indicator and emit the rest of the line
    /// (e.g. ',SM102A') as stray code. These markers are never valid COBOL, so strip them from
    /// the raw text before any other processing.
    /// </summary>
    public static string StripNistArchiveMarkers(string sourceText)
    {
        var lines = sourceText.Split('\n');
        var kept = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("*HEADER,", StringComparison.Ordinal) ||
                trimmed.StartsWith("*END-OF,", StringComparison.Ordinal))
                continue;
            kept.Add(line);
        }
        return string.Join('\n', kept);
    }

    /// <summary>The reference format a <c>&gt;&gt;SOURCE FORMAT</c> directive selects (Auto = no directive present).</summary>
    private enum DeclaredFormat { Auto, Fixed, Free }

    /// <summary>
    /// Matches a COBOL-2002 <c>&gt;&gt;SOURCE FORMAT [IS] {FREE|FIXED}</c> compiler directive (ISO §7.2) on a line,
    /// case-insensitively. The directive may be preceded by a sequence area / indicator in fixed-ish layouts, so
    /// the match is anchored to the <c>&gt;&gt;</c> rather than column 1. A trailing <c>.</c> is tolerated.
    /// </summary>
    private static readonly Regex SourceFormatDirective = new(
        @"^[\s\d]*>>\s*SOURCE\s+FORMAT\s+(?:IS\s+)?(FREE|FIXED)\s*\.?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Auto-detect whether source is fixed-form or free-form, and normalize to free-form.
    /// A <c>&gt;&gt;SOURCE FORMAT</c> directive, if present, overrides the structural heuristic (the spec-mandated
    /// explicit selector). The directive lines are consumed. NOTE: only a single whole-file declaration is honored
    /// — mid-file format switching is a documented WS-2002-FORMAT follow-up; the first directive wins.
    /// </summary>
    public static string NormalizeToFreeForm(string sourceText)
        => NormalizeToFreeForm(sourceText, dialectLevel: 85, permissive: false, diagnostics: null, sourcePath: "<source>");

    /// <summary>
    /// The edition-aware overload (W3 preprocessor threading, VCR rows 2/4/94 — DEVLOG 598): fixed-form
    /// continuation carries TWO per-edition obligations the column-blind path cannot see downstream:
    /// the col-7 hyphen indicator itself is OBSOLETE at 2023 (Annex F.2 item 4 → COBOLNET0903, once per
    /// compilation), and continuing a COBOL WORD across lines is REMOVED at 2023 (Annex E.2 item 1 bullet 2
    /// → COBOLNET0902, error strict / warning permissive — the pre-removal join semantics preserved).
    /// </summary>
    public static string NormalizeToFreeForm(
        string sourceText, int dialectLevel, bool permissive, DiagnosticBag? diagnostics, string sourcePath)
    {
        var gates = diagnostics is null ? null : new EditionGates(dialectLevel, permissive, diagnostics, sourcePath);
        var declared = DetectDeclaredFormat(sourceText);
        if (declared != DeclaredFormat.Auto)
        {
            string stripped = StripSourceFormatDirectives(sourceText);
            return declared == DeclaredFormat.Fixed ? ConvertFixedToFree(stripped, gates) : stripped;
        }
        return IsFixedForm(sourceText) ? ConvertFixedToFree(sourceText, gates) : sourceText;
    }

    /// <summary>
    /// The per-compilation edition gates of the fixed-form continuation mechanism (registry rows
    /// <c>col7-continuation-obsolete-2023</c> / <c>fixed-form-word-continuation-removed-2023</c>; the emit
    /// sites live HERE because only the column-aware pass can see the indicator — the metadata stays
    /// registry-canonical, and the severity policy mirrors <c>EditionContext</c>: removed = error strict /
    /// warning permissive, obsolete = warning always. One diagnostic per file per gate.
    /// </summary>
    private sealed class EditionGates(int dialectLevel, bool permissive, DiagnosticBag diagnostics, string sourcePath)
    {
        private bool _col7Flagged, _wordFlagged;

        /// <summary>Any col-7 '-' continuation — OBSOLETE at 2023 (Annex F.2 item 4; VCR row 94).</summary>
        public void OnContinuation(int line)
        {
            if (dialectLevel < 2023 || _col7Flagged) return;
            _col7Flagged = true;
            diagnostics.ReportWarning("COBOLNET0903",
                "the fixed continuation indicator (hyphen in column 7) is obsolete as of COBOL-2023 "
                + "(Annex F.2 item 4; use the floating continuation indicator) — first use at line " + line,
                new Common.SourceLocation(sourcePath, 0, line, IndicatorColumn), default);
        }

        /// <summary>A continuation that SPLICES a COBOL word across lines — REMOVED at 2023
        /// (Annex E.2 item 1 bullet 2; VCR row 2). Pre-removal join semantics are preserved either way.</summary>
        public void OnWordContinuation(int line)
        {
            if (dialectLevel < 2023 || _wordFlagged) return;
            _wordFlagged = true;
            const string msg = "continuation of a COBOL word in fixed-form reference format was removed in "
                + "COBOL-2023 (Annex E.2 item 1 bullet 2) — first use at line ";
            var loc = new Common.SourceLocation(sourcePath, 0, line, IndicatorColumn);
            if (permissive)
                diagnostics.ReportWarning("COBOLNET0902", msg + line, loc, default);
            else
                diagnostics.ReportError("COBOLNET0902", msg + line, loc, default);
        }
    }

    /// <summary>Return the format the first <c>&gt;&gt;SOURCE FORMAT</c> directive selects, or Auto if none.</summary>
    private static DeclaredFormat DetectDeclaredFormat(string sourceText)
    {
        foreach (var rawLine in sourceText.Split('\n'))
        {
            var m = SourceFormatDirective.Match(rawLine.TrimEnd('\r'));
            if (m.Success)
                return m.Groups[1].Value.Equals("FIXED", StringComparison.OrdinalIgnoreCase)
                    ? DeclaredFormat.Fixed : DeclaredFormat.Free;
        }
        return DeclaredFormat.Auto;
    }

    /// <summary>
    /// Blank out every <c>&gt;&gt;SOURCE FORMAT</c> directive line (the directive is consumed by the preprocessor).
    /// The lines are emptied rather than removed so downstream source-line numbers stay aligned for diagnostics.
    /// </summary>
    private static string StripSourceFormatDirectives(string sourceText)
    {
        var lines = sourceText.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (SourceFormatDirective.IsMatch(lines[i].TrimEnd('\r')))
                lines[i] = "";
        }
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Heuristic detection of fixed-form. Checks:
    /// - Lines are consistently >= 7 chars
    /// - Column 7 often contains space, *, or -
    /// - Columns 1-6 are often digits or spaces
    /// </summary>
    public static bool IsFixedForm(string sourceText)
    {
        // NOTE: do NOT treat the presence of a *> floating comment (COBOL-2002, ISO §6.2.3) as proof of
        // free-form. *> is legal in BOTH fixed and free reference format; a file with a genuine fixed-format
        // column structure (numeric sequence area + consistent column-7 indicators) is fixed-format that merely
        // uses inline comments, and must still be column-normalized. Classification is therefore driven purely
        // by the structural heuristic below; ConvertFixedToFree strips any inline *> from the source area.
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
    public static string ConvertFixedToFree(string sourceText) => ConvertFixedToFree(sourceText, gates: null);

    private static string ConvertFixedToFree(string sourceText, EditionGates? gates)
    {
        var lines = sourceText.Split('\n');
        var result = new StringBuilder();
        int lineNo = 0;

        // Literal state carried across lines for continuation decisions
        bool inLiteral = false;
        bool pendingQuote = false; // true when last char was first half of potential "" pair

        // True while inside an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR/INSTALLATION/etc.):
        // its free-text body is commented out until the next Area-A header. See CommentEntryParagraphs.
        bool inCommentEntry = false;

        foreach (var rawLine in lines)
        {
            lineNo++;
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

            // Obsolete comment-entry handling: applies only to ordinary code lines (the '-' continuation
            // and the '*'/'/'/'D'/excluded-indicator lines fall through to the switch, which already
            // comments or joins them — a comment-entry body is plain space-indicator text).
            bool excludedIndicator =
                indicator is '*' or '/' or 'D' or 'd' or 'S' or 's' or 'Y' or 'y'
                          or 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u' or '-';
            if (!excludedIndicator)
            {
                string? firstAreaAWord = FirstAreaAWord(sourceArea);
                if (firstAreaAWord is not null && CommentEntryParagraphs.Contains(firstAreaAWord))
                {
                    // Start (or continue, for back-to-back comment paragraphs) a comment-entry.
                    inCommentEntry = true;
                    result.AppendLine($"*> {sourceArea.TrimEnd()}");
                    inLiteral = false;
                    pendingQuote = false;
                    continue;
                }
                if (inCommentEntry)
                {
                    if (firstAreaAWord is not null)
                    {
                        // An Area-A header that is NOT a comment paragraph = the next real paragraph or
                        // division (ENVIRONMENT/DATA/PROCEDURE/section/program). End the comment-entry and
                        // let this line be processed normally below.
                        inCommentEntry = false;
                    }
                    else
                    {
                        // Area-B free text of the comment-entry: keep as commentary.
                        result.AppendLine($"*> {sourceArea.TrimEnd()}");
                        inLiteral = false;
                        pendingQuote = false;
                        continue;
                    }
                }
            }

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

                // CCVS optional/alternate source lines: the file-I/O suites tag auxiliary or
                // alternate-configuration lines in the indicator column — 'P' (an optional scratch
                // file such as the INDEXED RAW-DATA member, assigned to an X-card the program's
                // header does not declare) and 'J' (an alternate ASSIGN target beside the primary
                // space-indicator one). The primary configuration is the space-indicator lines, so
                // these alternates are excluded for a standard run (treated as comment lines).
                //
                // 'H' (CLOSE … REEL) and 'E' (CLOSE … UNIT) tag the multi-reel/multi-volume tape
                // feature (with REELUNIT counters) — an optional feature CobolSharp does not support;
                // the CCVS pairs each such block with a replacement line ('I' for REEL, 'F' for UNIT,
                // e.g. MOVE "CLOSE REEL DELETED" TO RE-MARK) that becomes the controlling IF's body
                // once the 'H'/'E' lines (which carry the period) are deleted. Excluding 'H'/'E' and
                // keeping the replacement (a normal line) yields the intended no-multi-volume program
                // — and stops CLOSE … REEL/UNIT from prematurely closing the file mid write-loop.
                // (Only 'H'/'E' are excluded; 'F' is also used as ordinary code in the IC suite.)
                //
                // 'T'/'U' are a matched ALTERNATE PAIR that completes an intentionally-incomplete record
                // layout (IX207A/IX208A): the base (space-indicator) FD record omits the key/alternate-key
                // filler bytes, and exactly ONE of the T or U variant lines supplies them — base+T and
                // base+U each total the declared RECORD length, but keeping BOTH overflows it and shifts the
                // key offsets away from the fixed-width FILE-RECORD-INFO work area the records are written
                // through. The test's own working-storage key images use the 'T' form, so 'T' is the active
                // configuration (kept as ordinary code) and 'U' is the excluded alternate.
                case 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u':
                    result.AppendLine($"*> {sourceArea.TrimEnd()}");
                    inLiteral = false;
                    pendingQuote = false;
                    break;

                case '-':
                    gates?.OnContinuation(lineNo);
                    // The WORD-continuation shape (removed 2023, VCR row 2): a NON-literal continuation whose
                    // splice joins two COBOL word characters (§6.2.4 — the continuation's first nonblank
                    // immediately follows the preceding line's last nonblank). Literal continuation and
                    // non-word splices (e.g. after a period or parenthesis) are NOT word continuation.
                    if (gates is not null && !inLiteral
                        && LastNonSpace(result) is { } prevCh && IsCobolWordChar(prevCh)
                        && sourceArea.TrimStart() is { Length: > 0 } contText && IsCobolWordChar(contText[0]))
                        gates.OnWordContinuation(lineNo);
                    HandleContinuation(result, sourceArea,
                        ref inLiteral, ref pendingQuote);
                    break;

                default:
                    // Normal line: strip any inline *> comment (COBOL-2002) that lies outside a literal, then
                    // emit. Preserve trailing spaces when inside an unclosed literal.
                    inLiteral = false;
                    pendingQuote = false;
                    string code = StripInlineComment(sourceArea);
                    ScanLiteralState(code, ref inLiteral, ref pendingQuote);
                    result.AppendLine(inLiteral ? code : code.TrimEnd());
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// If the source area begins with a word in Area A (its first non-space character falls within
    /// columns 8-11, i.e. the first <see cref="AreaAWidth"/> characters), return that word (the run of
    /// non-space, non-period characters), else null. Used to detect division/section/paragraph headers,
    /// which begin in Area A, versus Area-B continuation/free text, which is indented to column 12+.
    /// </summary>
    private static string? FirstAreaAWord(string sourceArea)
    {
        int start = 0;
        while (start < sourceArea.Length && sourceArea[start] == ' ') start++;
        if (start >= sourceArea.Length || start >= AreaAWidth)
            return null; // blank line, or text begins in Area B (not a header)
        int end = start;
        while (end < sourceArea.Length && sourceArea[end] is not (' ' or '.')) end++;
        return sourceArea[start..end];
    }

    /// <summary>
    /// Truncate a fixed-format source area at a floating <c>*&gt;</c> inline comment (COBOL-2002, ISO §6.2.3)
    /// that lies outside any string literal. Everything from the <c>*&gt;</c> to end of line is commentary. The
    /// lexer would skip it regardless, but stripping it here keeps the cross-line literal-state scan from being
    /// confused by quotes/apostrophes inside the comment text (which would otherwise mis-join a following
    /// continuation line). A <c>*&gt;</c> embedded in a literal (e.g. <c>"A*&gt;B"</c>) is preserved.
    /// </summary>
    private static string StripInlineComment(string sourceArea)
    {
        bool inLiteral = false;
        char quote = '\0';
        for (int i = 0; i + 1 < sourceArea.Length; i++)
        {
            char c = sourceArea[i];
            if (inLiteral)
            {
                if (c == quote)
                {
                    // A doubled quote ("") is an embedded quote that stays in the literal; a lone quote closes it.
                    if (i + 1 < sourceArea.Length && sourceArea[i + 1] == quote) i++;
                    else inLiteral = false;
                }
            }
            else if (c is '"' or '\'')
            {
                inLiteral = true;
                quote = c;
            }
            else if (c == '*' && sourceArea[i + 1] == '>')
            {
                return sourceArea[..i];
            }
        }
        return sourceArea;
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

    /// <summary>A COBOL word-forming character (§8.3.1 — letters, digits, hyphen; the underscore joined at
    /// 2002 and rides the same splice rule).</summary>
    private static bool IsCobolWordChar(char c) => char.IsLetterOrDigit(c) || c is '-' or '_';

    /// <summary>The last non-space, non-newline character accumulated so far (the splice's LEFT side), or
    /// null when the previous content is empty.</summary>
    private static char? LastNonSpace(StringBuilder sb)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
            if (sb[i] is not (' ' or '\n' or '\r')) return sb[i];
        return null;
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
