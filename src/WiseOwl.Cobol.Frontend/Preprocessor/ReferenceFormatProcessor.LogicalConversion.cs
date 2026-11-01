// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

public static partial class ReferenceFormatProcessor
{
    /// <summary>
    /// The fixed-form CLASSIFIER of the ISO §6.5 logical conversion of one fixed-form text — a whole file, one SOURCE
    /// FORMAT segment, or a copybook (kb/Work PB1491). It reads the indicator area and the program-text area of each
    /// physical line and hands the program text to the <see cref="LogicalLineBuilder"/>, which owns the resultant lines,
    /// the latest logical line and the literal state (kb/Work PB1804):
    /// <list type="bullet">
    /// <item>§6.5 2) "If the line is a comment line or a blank line, that line is logically discarded" — a comment line is
    /// a fixed <c>*</c> or <c>/</c> indicator or a line whose first character-string is the floating comment indicator;
    /// the builder keeps its slot as an EMPTY line.</item>
    /// <item>The program-text area is ALWAYS character positions 8–72: a physical line shorter than margin R is read as if
    /// space-filled to it (docs/CONFORMANCE.md DOC-A.1-157, §6.1 1) c) "The implementor shall specify the meaning of lines
    /// and character positions"), so a literal continued by the fixed continuation indicator carries every position up
    /// to margin R — §6.3.5: "any spaces at the end of the fixed-form continued line are part of the literal".</item>
    /// </list>
    /// </summary>
    private sealed class FixedFormConverter(ReferenceFormatDiagnostics? gates, string file, bool ccvsIndicators,
        bool startsInIdentificationDivision)
    {
        private readonly LogicalLineBuilder _builder = new(file, gates, fixedForm: true);

        /// <summary>True while inside an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR/INSTALLATION/etc.):
        /// its free-text body is commentary until the next Area-A header. See <see cref="CommentEntryParagraphs"/>.</summary>
        private bool _inCommentEntry;

        /// <summary>Which division the text read so far is in — the one rule the COPY driver shares
        /// (<see cref="DivisionCursor"/>, kb/Work PB2739). A comment-entry paragraph exists only in an IDENTIFICATION
        /// DIVISION (kb/Work PB1494: a PROCEDURE DIVISION paragraph named REMARKS was dropped with every line after it). A
        /// source text starts in one (a program's IDENTIFICATION DIVISION header is where it begins), library text in the
        /// division its COPY statement stands in.</summary>
        private readonly DivisionCursor _division = new(startsInIdentificationDivision);

        /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION — the state the next SOURCE FORMAT
        /// segment of the same text starts in.</summary>
        public bool InIdentificationDivision => _division.InIdentificationDivision;

        /// <summary>Convert the lines of one text — a whole file, one SOURCE FORMAT segment of it, or a copybook —
        /// returning the resultant lines and, per resultant line, the physical lines and columns it came from (kb/Work
        /// PB82, PB1801). Each <see cref="PhysicalLine.Number"/> is the file's own, so a segment reports file lines, never
        /// segment-relative ones.</summary>
        public (List<string> Lines, List<LineOrigin> Origins) Convert(ReadOnlySpan<PhysicalLine> physicalLines)
        {
            foreach (var line in physicalLines)
                ConvertLine(line);
            _builder.ReadInto(_division);   // the division the text ENDS in is the next segment's start
            return _builder.Result;
        }

        /// <summary>The kind of line an indicator-area character marks (§6.3.3 "The indicator area identifies the type
        /// of a source line in accordance with the indicators specified in 6.2.2"). The NIST CCVS conventions — letters
        /// §6.2.2 does not list — are a dialect, honored only under <c>--nist</c> (kb/Work PB1494).</summary>
        private enum LineKind { Source, Comment, Continuation, Debugging, CcvsExcluded, NotAnIndicator }

        private LineKind KindOf(int indicator) => indicator switch
        {
            ' ' => LineKind.Source,
            '*' or '/' => LineKind.Comment,
            FixedFormLine.ContinuationIndicator => LineKind.Continuation,
            'D' or 'd' => LineKind.Debugging,                   // COBOL-85's debugging line
            _ when !ccvsIndicators => LineKind.NotAnIndicator,
            'S' or 's' or 'Y' or 'y' or 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u' or 'B' or 'b' => LineKind.CcvsExcluded,
            _ => LineKind.Source,                               // CCVS: a primary-configuration line
        };

        private void ConvertLine(PhysicalLine line)
        {
            var fixedLine = new FixedFormLine(line.Text);
            var indicator = fixedLine.Indicator;
            string area = fixedLine.ProgramText;
            int lineNo = line.Number;
            // The program-text area is resultant text from its first character, which is position 8 (kb/Work PB1801).
            var site = new TextSite(lineNo, fixedLine.ProgramTextIndex, line.Columns);
            var kind = KindOf(indicator.Value);
            switch (CommentEntryOf(kind, fixedLine, area))
            {
                case CommentEntryText.Header:
                    // The paragraph HEADER stays program text — the parser and the one removal gate
                    // (VersionConformancePass, COBOL0902 from 2002) must see the paragraph in fixed form as they do in
                    // free form (kb/Work PB1494, PB1758) — and only the comment-entry is discarded. The entry's end is
                    // implied by the next Area-A word (COBOL-85), so the logical conversion writes the paragraph's
                    // terminating period explicitly: `AUTHOR. .` is the paragraph `AUTHOR DOT DOT` with no content.
                    _builder.Emit(CommentEntryHeader(area), site);
                    return;
                case CommentEntryText.Body:
                    _builder.Discard(site);
                    return;
            }

            switch (kind)
            {
                case LineKind.Comment:   // a comment line (§6.2.2 fixed comment indicators; §6.5 2))
                    _builder.Discard(site);
                    break;

                case LineKind.Debugging:
                    // §6.2.2 lists no debugging indicator at COBOL-2023: the facility is obsolete at 2002 and removed at
                    // 2014 (kb/Work R61, row debugging-line-removed-2014). Whether the line is SOURCE or COMMENT is the
                    // program's SOURCE-COMPUTER ... WITH DEBUGGING MODE clause's to say, and the clause is further down the
                    // text than any stage that has read this line, so the line is CARRIED: DebuggingLineRewriter decides
                    // per source unit after lexing (kb/Work PB1705, owner decision R56). Its tokens stay matchable by COPY
                    // REPLACING / REPLACE as if the D were absent (TextWordScanner skips the carrier).
                    gates?.OnDebuggingLine(file, lineNo);
                    _builder.EmitDebugging(DebugLineCarrier, area, site);
                    break;

                // 'S' and 'Y' (the CCVS debug-suite letters, §6.2.2 lists neither) are excluded the same way: they are the
                // --nist dialect, not the D indicator, so the WITH DEBUGGING MODE clause that keeps a D line (PB1705) never
                // keeps them — they were comment lines before the D line's carrier became a rewritable one, and stay so.
                //
                // CCVS optional/alternate source lines: the file-I/O suites tag auxiliary or
                // alternate-configuration lines in the indicator column — 'P' (an optional scratch
                // file such as the INDEXED RAW-DATA member, assigned to an X-card the program's
                // header does not declare) and 'J' (an alternate ASSIGN target beside the primary
                // space-indicator one). The primary configuration is the space-indicator lines, so
                // these alternates are excluded for a standard run (treated as comment lines).
                //
                // 'H' (CLOSE … REEL) and 'E' (CLOSE … UNIT) tag the multi-reel/multi-volume tape
                // feature (with REELUNIT counters) — an optional feature WiseOwl COBOL does not support;
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
                //
                // 'A'/'B' are the matched pair of the SWITCH-STATUS tests (NC174A, NC211A, NC254A): the 'A' lines are
                // the configuration of an implementor WITH external switches (the SPECIAL-NAMES switch entries and the
                // IF on their status condition-names), the 'B' lines what a processor WITHOUT them substitutes ("SWITCHES
                // NOT IMPLEMENTED", and in NC211A level-88 entries spelling the very condition-names the A lines declare
                // as switch status names). WiseOwl COBOL implements switches (§12.3.7.4 GR2), so 'A' is the active
                // configuration (kept as ordinary code) and 'B' the excluded alternate: kept, NC211A's B level-88s and
                // its SPECIAL-NAMES switch names are one condition-name declared twice, which §8.4.2.2.1 refuses on an
                // unqualified reference (kb/Work PB3347).
                case LineKind.CcvsExcluded:
                    _builder.Discard(site);
                    break;

                case LineKind.Continuation:
                    _builder.Add(area, site, fixedContinuation: true);
                    break;

                case LineKind.NotAnIndicator:
                    gates?.OnInvalidIndicator(file, lineNo, indicator);   // COBOL2616, then read as a source line
                    _builder.Add(area, site, fixedContinuation: false);
                    break;

                default:
                    _builder.Add(area, site, fixedContinuation: false);
                    break;
            }
        }

        /// <summary>What a line is to an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR / INSTALLATION /
        /// DATE-WRITTEN / DATE-COMPILED / SECURITY / REMARKS): not part of one, the header line that opens it, or
        /// the Area-B body of its comment-entry.</summary>
        private enum CommentEntryText { None, Header, Body }

        /// <summary>Classify a line against the comment-entry paragraph in progress. A comment, debugging or
        /// excluded-alternate line is processed by its own arm; a continuation line inside the paragraph continues
        /// its commentary.</summary>
        private CommentEntryText CommentEntryOf(LineKind kind, FixedFormLine line, string area)
        {
            if (kind == LineKind.Continuation) return _inCommentEntry ? CommentEntryText.Body : CommentEntryText.None;
            if (kind is not (LineKind.Source or LineKind.NotAnIndicator)) return CommentEntryText.None;
            // The latest logical line awaits a floating-indicator continuation (§6.5 8)): this line is its continuation
            // line, whatever it spells, and neither opens nor ends a comment-entry.
            if (_builder.AwaitsFloatingContinuation) return CommentEntryText.None;
            // COBOL-85's comment-entry paragraph begins at an Area-A word and ends at the next one — the only place the
            // Area A / Area B line matters here. WHERE the text is (the division) is read from the words of the LOGICAL
            // lines before this one, wherever they begin (§6.3.1: the program-text area has no such line), by the one
            // DivisionCursor rule.
            // A blank or comment line is a new logical line of nothing (§6.5 2)): a continuation line after it still joins
            // the latest logical line, so that line is not complete and the cursor must not read it yet (kb/Work PB4241).
            if (LogicalLineBuilder.TryFirstText(area, out _)) _builder.ReadInto(_division);
            string? firstAreaAWord = FirstAreaAWord(line, area);
            if (_division.InIdentificationDivision && firstAreaAWord is not null && CommentEntryParagraphs.Contains(firstAreaAWord))
            {
                _inCommentEntry = true;                     // start (or continue, back-to-back) a comment-entry
                return CommentEntryText.Header;
            }
            if (_inCommentEntry && firstAreaAWord is null) return CommentEntryText.Body;   // Area-B text (or blank): the entry goes on
            _inCommentEntry = false;                        // the next Area-A word ends it
            return CommentEntryText.None;
        }
    }

    /// <summary>Convert one text, or one SOURCE FORMAT segment of it, in its reference format: the fixed-form or the
    /// free-form classifier, each feeding the one <see cref="LogicalLineBuilder"/>. <paramref name="inIdentificationDivision"/>
    /// is the division the segment starts in, and on return the one it ends in: the next segment starts there, whatever its
    /// format.</summary>
    private static (List<string> Lines, List<LineOrigin> Origins) ConvertSegment(ReadOnlySpan<PhysicalLine> lines, bool isFixed,
        ReferenceFormatDiagnostics? gates, string file, bool ccvsIndicators, ref bool inIdentificationDivision)
    {
        if (isFixed)
        {
            var fixedForm = new FixedFormConverter(gates, file, ccvsIndicators, inIdentificationDivision);
            var converted = fixedForm.Convert(lines);
            inIdentificationDivision = fixedForm.InIdentificationDivision;
            return converted;
        }
        var freeForm = new FreeFormConverter(gates, file, inIdentificationDivision);
        var result = freeForm.Convert(lines);
        inIdentificationDivision = freeForm.InIdentificationDivision;
        return result;
    }

    /// <summary>The free-form CLASSIFIER of the ISO §6.5 logical conversion — a whole text, one SOURCE FORMAT segment of
    /// it, or a copybook. A free-form line is "copied to the resultant compilation group" (§6.5 7)) once its comment is
    /// removed (§6.5 2) and 3)); the only line that is not copied as a new logical line is the continuation line of a
    /// literal continued with a floating literal continuation indicator (§6.5 8), kb/Work PB1359), which the
    /// <see cref="LogicalLineBuilder"/> joins to its latest logical line. A discarded or joined line keeps its slot as an
    /// empty line only when discarded; each resultant line's origin is the physical line and column it was read from.
    /// <para>⛔ A COMMENT-ENTRY IS COMMENTARY IN FREE FORM TOO (docs/CONFORMANCE.md D-CE1; kb/Work PB3245, review finding
    /// R1-1). A comment-entry is arbitrary text (an apostrophe in a name, an unclosed quotation mark, a comma with no space
    /// after it), so in neither format does it reach the lexer as program text: inside an IDENTIFICATION DIVISION, a line
    /// whose first word is a comment-entry paragraph's word is written as the paragraph's header (<c>AUTHOR. .</c>, exactly
    /// as the fixed-form converter writes it), and the rest of the line, the entry, is discarded. No edition of the
    /// standard has both a comment-entry and free form (COBOL-2002 introduced free form and removed the comment-entry), so
    /// where a free-form entry ends is the implementor's to say, and CLAUDE.md rule 1 takes GnuCOBOL 3.2's <c>-free</c>
    /// reading: the paragraph word first on its line, the entry the rest of that line, the next line program text.</para>
    /// </summary>
    private sealed class FreeFormConverter(ReferenceFormatDiagnostics? gates, string file, bool startsInIdentificationDivision)
    {
        private readonly LogicalLineBuilder _builder = new(file, gates, fixedForm: false);

        /// <summary>Which division the text read so far is in: the ONE rule (<see cref="DivisionCursor"/>), as in the
        /// fixed-form converter, because a comment-entry paragraph exists only in an IDENTIFICATION DIVISION.</summary>
        private readonly DivisionCursor _division = new(startsInIdentificationDivision);

        /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION — the state the next SOURCE FORMAT
        /// segment of the same text starts in.</summary>
        public bool InIdentificationDivision => _division.InIdentificationDivision;

        /// <summary>Convert the lines of one text, returning the resultant lines and, per resultant line, the physical
        /// lines and columns it came from (kb/Work PB82, PB1801).</summary>
        public (List<string> Lines, List<LineOrigin> Origins) Convert(ReadOnlySpan<PhysicalLine> physicalLines)
        {
            foreach (var line in physicalLines)
            {
                int positions = CharacterPositions.Count(line.Text);
                if (positions > FreeFormMaxPositions) gates?.OnFreeFormLineTooLong(file, line.Number, positions);
                var site = new TextSite(line.Number, Index: 0, line.Columns);
                // A comment line (§6.5 2)) is neither a header nor words of a division, as in fixed form.
                if (LogicalLineBuilder.TryFirstText(line.Text, out int first) && BeginsCommentEntry(line.Text, first))
                {
                    _builder.Emit(CommentEntryHeader(line.Text), site);
                    continue;
                }
                _builder.Add(line.Text, site, fixedContinuation: false);
            }
            _builder.ReadInto(_division);   // the division the text ENDS in is the next segment's start
            return _builder.Result;
        }

        /// <summary>Whether the line whose first character-string begins at <paramref name="first"/> opens a comment-entry
        /// paragraph: the text stands in an IDENTIFICATION DIVISION, the line's first word is one of
        /// <see cref="CommentEntryParagraphs"/>, and the line is not the continuation line a floating literal continuation
        /// indicator asks for (§6.5 8)), whose text is the literal's whatever it spells.</summary>
        private bool BeginsCommentEntry(string text, int first)
        {
            if (_builder.AwaitsFloatingContinuation) return false;
            _builder.ReadInto(_division);   // the division is that of the logical lines before this one
            return _division.InIdentificationDivision && CommentEntryParagraphs.Contains(text[first..WordEnd(text, first)]);
        }
    }

    /// <summary>The most character positions a free-form line may have (§6.1 3) a): "ranging from a minimum of 0 to a
    /// maximum of 255") — positions as DOC-A.1-157 counts them, so on the expanded line. Asked in the free-form arm
    /// because a fixed-form line may run past margin R and only its program-text area counts.</summary>
    internal const int FreeFormMaxPositions = 255;

    /// <summary>
    /// If the line's program text begins in area A (<see cref="FixedFormLine.BeginsInAreaA"/>, the ONE definition of
    /// the area), return the word it begins with (the run of non-space, non-period characters), else null. Used for the
    /// COBOL-85 comment-entry paragraph, which begins and ends at an Area-A word (<c>CommentEntryOf</c>); which DIVISION
    /// the text is in is never decided by the area a word begins in (<see cref="DivisionCursor"/>, kb/Work PB2739).
    /// </summary>
    private static string? FirstAreaAWord(FixedFormLine line, string programText)
    {
        if (!line.BeginsInAreaA) return null;   // blank line, or text begins in area B (not a header)
        int start = line.ProgramTextStart;
        return programText[start..WordEnd(programText, start)];
    }

    /// <summary>The logical-conversion text of a comment-entry paragraph's header line, in either reference format: the
    /// paragraph word at its own position, its period, and the explicit terminating period that stands for the discarded
    /// comment-entry. <paramref name="programText"/> begins with the word, after any spaces.</summary>
    private static string CommentEntryHeader(string programText)
        => programText[..WordEnd(programText, FirstNonSpace(programText))] + ". .";

    /// <summary>The end (exclusive) of the word that begins at <paramref name="start"/>: the run of characters up to a
    /// space or a period. How a comment-entry paragraph's header word is read, in both reference formats.</summary>
    private static int WordEnd(string text, int start)
    {
        int end = start;
        while (end < text.Length && text[end] is not (' ' or '.')) end++;
        return end;
    }
}
