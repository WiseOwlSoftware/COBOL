// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// The fixed-form CLASSIFIER of the ISO §2023:6.5 logical conversion of one fixed-form text — a whole file, one SOURCE
/// FORMAT segment, or a copybook (kb/Work PB1491). It reads the indicator area and the program-text area of each
/// physical line and hands the program text to the <see cref="LogicalLineBuilder"/>, which owns the resultant lines,
/// the latest logical line and the literal state (kb/Work PB1804):
/// <list type="bullet">
/// <item>§2023:6.5 GR2 "If the line is a comment line or a blank line, that line is logically discarded" — a comment line is
/// a fixed <c>*</c> or <c>/</c> indicator or a line whose first character-string is the floating comment indicator;
/// the builder keeps its slot as an EMPTY line.</item>
/// <item>The program-text area is ALWAYS character positions 8–72: a physical line shorter than margin R is read as if
/// space-filled to it (docs/CONFORMANCE.md DOC-A.1-157, §2023:6.1 GR1c) "The implementor shall specify the meaning of lines
/// and character positions"), so a literal continued by the fixed continuation indicator carries every position up
/// to margin R — §2023:6.3.5: "any spaces at the end of the fixed-form continued line are part of the literal".</item>
/// </list>
/// </summary>
internal sealed class FixedFormConverter(ReferenceFormatDiagnostics? gates, string file, bool ccvsIndicators,
    bool? startsInIdentificationDivision, CommentEntryPins? pins, int inclusion, Func<string, string>? asReplaced,
    PhysicalLineRecord? kinds)
{
    private readonly LogicalLineBuilder _builder = new(file, gates, fixedForm: true, kinds);

    /// <summary>True while inside an obsolete IDENTIFICATION comment-entry paragraph (AUTHOR/INSTALLATION/etc.):
    /// its free-text body is commentary until the next Area-A header. See <see cref="LogicalLineConverter.IsCommentEntryParagraph"/>.</summary>
    private bool _inCommentEntry;

    /// <summary>Whether the comment-entry paragraph in progress is a CANDIDATE: the division was not known when its
    /// header was read (<see cref="DivisionCursor"/>), so its lines are carried, not read
    /// (<see cref="CommentEntryCarrier"/>).</summary>
    private bool _carryingCommentEntry;

    /// <summary>Which division the text read so far is in — the one rule the COPY driver shares
    /// (<see cref="DivisionCursor"/>, kb/Work PB2739). A comment-entry paragraph exists only in an IDENTIFICATION
    /// DIVISION (kb/Work PB1494: a PROCEDURE DIVISION paragraph named REMARKS was dropped with every line after it). A
    /// source text starts in one (a program's IDENTIFICATION DIVISION header is where it begins), library text in the
    /// division its COPY statement stands in.</summary>
    private readonly DivisionCursor _division = new(startsInIdentificationDivision, directiveLines: true);   // this stage precedes conditional compilation, which at an edition with compiler directives reads every >> line it meets as a directive and gates it (kb/Work PB725, PB5803)

    /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION (null: not known) — the state the next
    /// SOURCE FORMAT segment of the same text starts in.</summary>
    public bool? InIdentificationDivision => _division.InIdentificationDivision;

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

    /// <summary>The kind of line an indicator-area character marks (§2023:6.3.3 "The indicator area identifies the type
    /// of a source line in accordance with the indicators specified in 6.2.2"), as the ONE
    /// <see cref="PhysicalLineKind"/> the conversion records for the line (DESIGN-listing.md section 4.4.1): the slant is
    /// a comment line that ejects a page in the listing (§1985:IV.7.2.4), the asterisk one that does not. The NIST CCVS
    /// conventions — letters §2023:6.2.2 does not list — are a dialect, honored only under <c>--nist</c> (kb/Work PB1494).</summary>
    private PhysicalLineKind KindOf(int indicator) => indicator switch
    {
        ' ' => PhysicalLineKind.Source,
        '*' => PhysicalLineKind.Comment,
        '/' => PhysicalLineKind.PageEjectComment,
        FixedFormLine.ContinuationIndicator => PhysicalLineKind.Continuation,
        'D' or 'd' => PhysicalLineKind.DebuggingLine,       // COBOL-85's debugging line
        _ when !ccvsIndicators => PhysicalLineKind.NotAnIndicator,
        'S' or 's' or 'Y' or 'y' or 'P' or 'p' or 'J' or 'j' or 'H' or 'h' or 'E' or 'e' or 'U' or 'u' or 'B' or 'b' => PhysicalLineKind.CcvsExcluded,
        _ => PhysicalLineKind.Source,                       // CCVS: a primary-configuration line
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
        _builder.Classify(site, kind);   // the indicator's answer; the builder refines a source line (blank, comment, directive)
        switch (CommentEntryOf(kind, fixedLine, area, lineNo))
        {
            // The division is not known here (DivisionCursor), so the line is CARRIED for CommentEntryResolver to decide
            // on the text every stage between has finished with (docs/CONFORMANCE.md D-CE2; kb/Work PB4281).
            case CommentEntryText.CandidateHeader:
                _builder.Classify(site, PhysicalLineKind.CommentEntryCandidate);
                // Where its comment-entry begins SHOULD the line head a comment-entry paragraph: the listing reads it only
                // for a candidate the parse settled as one (DESIGN-listing.md section 7.2).
                _builder.Discard(site, LogicalLineConverter.CommentEntryStart(area), SourceTextClass.CommentEntry);
                _builder.AddCarrier(CommentEntryCarrier.Encode(isHeader: true, isFixed: true, ccvsIndicators, inclusion, line), site);
                return;
            case CommentEntryText.CandidateBody:
                _builder.Classify(site, PhysicalLineKind.CommentEntryCandidate);
                _builder.AddCarrier(CommentEntryCarrier.Encode(isHeader: false, isFixed: true, ccvsIndicators, inclusion, line), site);
                return;
            case CommentEntryText.Header:
                // The paragraph HEADER stays program text — the parser and the one removal gate
                // (VersionConformancePass, COBOL0902 from 2002) must see the paragraph in fixed form as they do in
                // free form (kb/Work PB1494, PB1758) — and only the comment-entry is discarded. The entry's end is
                // implied by the next Area-A word (COBOL-85), so the logical conversion writes the paragraph's
                // terminating period explicitly: `AUTHOR. .` is the paragraph `AUTHOR DOT DOT` with no content.
                _builder.Emit(LogicalLineConverter.CommentEntryHeader(area, site, file, gates, periodRequired: true), site);
                _builder.Discard(site, LogicalLineConverter.CommentEntryStart(area), SourceTextClass.CommentEntry);
                return;
            case CommentEntryText.Body:
                _builder.Classify(site, PhysicalLineKind.CommentEntry);
                _builder.Discard(site);
                return;
        }

        switch (kind)
        {
            case PhysicalLineKind.Comment or PhysicalLineKind.PageEjectComment:   // a comment line (§2023:6.2.2 fixed comment indicators; §2023:6.5 GR2)
                _builder.Discard(site);
                break;

            case PhysicalLineKind.DebuggingLine:
                // §2023:6.2.2 lists no debugging indicator at COBOL-2023: the facility is obsolete at 2002 and removed at
                // 2014 (kb/Work R61, row debugging-line-removed-2014). Whether the line is SOURCE or COMMENT is the
                // program's SOURCE-COMPUTER ... WITH DEBUGGING MODE clause's to say, and the clause is further down the
                // text than any stage that has read this line, so the line is CARRIED: DebuggingLineRewriter decides
                // per source unit after lexing (kb/Work PB1705, owner decision R56). Its tokens stay matchable by COPY
                // REPLACING / REPLACE as if the D were absent (TextWordScanner skips the carrier).
                gates?.OnDebuggingLine(file, lineNo);
                _builder.CheckNoFloatingDebuggingOnFixedIndicator(area, site);   // 2002 clause 6.1.2.1 SR8
                _builder.EmitDebugging(DebugLineCarrier.Text, area, site, FixedFormLine.IndicatorColumn);
                break;

            // 'S' and 'Y' (the CCVS debug-suite letters, §2023:6.2.2 lists neither) are excluded the same way: they are the
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
            // as switch status names). WiseOwl COBOL implements switches (§2023:12.3.7.4 GR2), so 'A' is the active
            // configuration (kept as ordinary code) and 'B' the excluded alternate: kept, NC211A's B level-88s and
            // its SPECIAL-NAMES switch names are one condition-name declared twice, which §2023:8.4.2.2.1 refuses on an
            // unqualified reference (kb/Work PB3347).
            case PhysicalLineKind.CcvsExcluded:
                _builder.Discard(site);
                break;

            case PhysicalLineKind.Continuation:
                _builder.CheckNoFloatingDebuggingOnFixedIndicator(area, site);   // 2002 clause 6.1.2.1 SR8
                _builder.Add(area, site, fixedContinuation: true);
                break;

            case PhysicalLineKind.NotAnIndicator:
                gates?.OnInvalidIndicator(file, lineNo, indicator);   // COBOL2616, then read as a source line
                AddSourceLine(area, site);
                break;

            default:
                AddSourceLine(area, site);
                break;
        }
    }

    /// <summary>A source line: a debugging line when its program text begins with the floating debugging indicator
    /// <c>&gt;&gt;D</c> (2002 clause 6.1.2; the area is space-filled to margin R), otherwise program text.</summary>
    private void AddSourceLine(string area, TextSite site)
    {
        if (_builder.TryEmitFloatingDebugging(area, site, spaceFilled: true)) _builder.Classify(site, PhysicalLineKind.DebuggingLine);
        else _builder.Add(area, site, fixedContinuation: false);
    }

    /// <summary>Classify a line against the comment-entry paragraph in progress. A comment, debugging or
    /// excluded-alternate line is processed by its own arm; a continuation line inside the paragraph continues
    /// its commentary.</summary>
    private CommentEntryText CommentEntryOf(PhysicalLineKind kind, FixedFormLine line, string area, int lineNumber)
    {
        if (kind == PhysicalLineKind.Continuation)
        {
            if (!_inCommentEntry) return CommentEntryText.None;
            // A candidate's body is decided later, by CommentEntryResolver, which reports this where the candidate proves
            // to be a comment-entry (a continuation line of program text is legal): only a certain one is reported here.
            if (!_carryingCommentEntry) gates?.OnCommentEntryContinued(file, lineNumber);
            return BodyOfParagraph();
        }
        if (kind is not (PhysicalLineKind.Source or PhysicalLineKind.NotAnIndicator)) return CommentEntryText.None;
        // The latest logical line awaits a floating-indicator continuation (§2023:6.5 GR8): this line is its continuation
        // line, whatever it spells, and neither opens nor ends a comment-entry.
        if (_builder.AwaitsFloatingContinuation) return CommentEntryText.None;
        // COBOL-85's comment-entry paragraph begins at an Area-A word and ends at the next one — the only place the
        // Area A / Area B line matters here. WHERE the text is (the division) is read from the words of the LOGICAL
        // lines before this one, wherever they begin (§2023:6.3.1: the program-text area has no such line), by the one
        // DivisionCursor rule.
        // A blank or comment line is a new logical line of nothing (§2023:6.5 GR2): a continuation line after it still joins
        // the latest logical line, so that line is not complete and the cursor must not read it yet (kb/Work PB4241).
        if (_builder.TryFirstText(area, out _)) _builder.ReadInto(_division);
        string? firstAreaAWord = LogicalLineConverter.FirstAreaAWord(line, area);
        // A comment-entry paragraph exists only in an IDENTIFICATION DIVISION: certain there, a paragraph of the program
        // elsewhere, and a candidate when the division is not known — unless an earlier run of the text stages proved
        // this candidate to be program text (CommentEntryPins), which then reads as the paragraph it is.
        if (_division.InIdentificationDivision is not false && firstAreaAWord is not null
            && LogicalLineConverter.IsCommentEntryParagraph(firstAreaAWord, asReplaced) && !IsPinned(lineNumber))
        {
            _inCommentEntry = true;                     // start (or continue, back-to-back) a comment-entry
            _carryingCommentEntry = _division.InIdentificationDivision is null;
            return _carryingCommentEntry ? CommentEntryText.CandidateHeader : CommentEntryText.Header;
        }
        if (_inCommentEntry && firstAreaAWord is null) return BodyOfParagraph();   // Area-B text (or blank): the entry goes on
        _inCommentEntry = false;                        // the next Area-A word ends it
        return CommentEntryText.None;
    }

    /// <summary>Whether the candidate opening at physical line <paramref name="lineNumber"/> of this inclusion is known to be
    /// program text (<see cref="CommentEntryPins"/>); asked only where the division is not known.</summary>
    private bool IsPinned(int lineNumber) => _division.InIdentificationDivision is null && pins?.Contains(file, inclusion, lineNumber) == true;

    private CommentEntryText BodyOfParagraph() => _carryingCommentEntry ? CommentEntryText.CandidateBody : CommentEntryText.Body;
}
