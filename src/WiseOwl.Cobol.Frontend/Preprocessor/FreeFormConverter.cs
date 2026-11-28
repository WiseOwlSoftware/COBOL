// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>The free-form CLASSIFIER of the ISO §2023:6.5 logical conversion — a whole text, one SOURCE FORMAT segment of
/// it, or a copybook. A free-form line is "copied to the resultant compilation group" (§2023:6.5 GR7) once its comment is
/// removed (§2023:6.5 GR2, §2023:6.5 GR3); the only line that is not copied as a new logical line is the continuation line of a
/// literal continued with a floating literal continuation indicator (§2023:6.5 GR8, kb/Work PB1359), which the
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
internal sealed class FreeFormConverter(ReferenceFormatDiagnostics? gates, string file, bool? startsInIdentificationDivision,
    CommentEntryPins? pins, int inclusion, bool gatesLineLength, Func<string, string>? asReplaced, PhysicalLineRecord? kinds)
{
    private readonly LogicalLineBuilder _builder = new(file, gates, fixedForm: false, kinds);

    /// <summary>Which division the text read so far is in: the ONE rule (<see cref="DivisionCursor"/>), as in the
    /// fixed-form converter, because a comment-entry paragraph exists only in an IDENTIFICATION DIVISION.</summary>
    private readonly DivisionCursor _division = new(startsInIdentificationDivision, directiveLines: true);   // this stage precedes conditional compilation, which at an edition with compiler directives reads every >> line it meets as a directive and gates it (kb/Work PB725, PB5803)

    /// <summary>Whether the text read so far ends in an IDENTIFICATION DIVISION (null: not known) — the state the next
    /// SOURCE FORMAT segment of the same text starts in.</summary>
    public bool? InIdentificationDivision => _division.InIdentificationDivision;

    /// <summary>Convert the lines of one text, returning the resultant lines and, per resultant line, the physical
    /// lines and columns it came from (kb/Work PB82, PB1801).</summary>
    public (List<string> Lines, List<LineOrigin> Origins) Convert(ReadOnlySpan<PhysicalLine> physicalLines)
    {
        foreach (var line in physicalLines)
        {
            var site = new TextSite(line.Number, Index: 0, line.Columns);
            _builder.Classify(site, PhysicalLineKind.Source);   // the builder refines it (blank, comment, directive)
            // A comment line (§2023:6.5 GR2) is neither a header nor words of a division, as in fixed form.
            var entry = _builder.TryFirstText(line.Text, out int first) ? BeginsCommentEntry(line.Text, first, line.Number) : CommentEntryText.None;
            // The 255-position gate is asked of EVERY physical line, here and once: this conversion runs before conditional
            // compilation, so a line of an omitted branch is gated as any other (§2023:6.1 SR3a), a carried candidate's too, and
            // CommentEntryResolver, which converts the candidate later, never asks it again (kb/Work PB4281, review findings
            // R1-2 and R2-1).
            if (gatesLineLength) LogicalLineConverter.ReportLineLength(gates, file, line);
            if (entry != CommentEntryText.None)
            {
                // The division is not known (DivisionCursor) for a candidate: the line is CARRIED for CommentEntryResolver
                // to decide on the text every stage between has finished with (docs/CONFORMANCE.md D-CE2; kb/Work PB4281).
                if (entry == CommentEntryText.Header)
                {
                    _builder.Emit(LogicalLineConverter.CommentEntryHeader(line.Text, site, file, gates, periodRequired: false), site);
                    _builder.Discard(site, LogicalLineConverter.CommentEntryStart(line.Text), SourceTextClass.CommentEntry);
                }
                else
                {
                    _builder.Classify(site, PhysicalLineKind.CommentEntryCandidate);
                    // Where its comment-entry begins should the line head a comment-entry paragraph, as in fixed form.
                    _builder.Discard(site, LogicalLineConverter.CommentEntryStart(line.Text), SourceTextClass.CommentEntry);
                    _builder.AddCarrier(CommentEntryCarrier.Encode(isHeader: true, isFixed: false, ccvsIndicators: false, inclusion, line), site);
                }
                continue;
            }
            // The floating debugging indicator >>D heads a debugging line in free form too (2002 clause 6.1.2).
            if (_builder.TryEmitFloatingDebugging(line.Text, site, spaceFilled: false))
            {
                _builder.Classify(site, PhysicalLineKind.DebuggingLine);
                continue;
            }
            _builder.Add(line.Text, site, fixedContinuation: false);
        }
        _builder.ReadInto(_division);   // the division the text ENDS in is the next segment's start
        return _builder.Result;
    }

    /// <summary>Whether the line whose first character-string begins at <paramref name="first"/> opens a comment-entry
    /// paragraph: its first word names one (<see cref="LogicalLineConverter.IsCommentEntryParagraph"/>), the line is not the continuation line a
    /// floating literal continuation indicator asks for (§2023:6.5 GR8, whose text is the literal's whatever it spells), and the
    /// text stands in an IDENTIFICATION DIVISION (<see cref="CommentEntryText.Header"/>) or in none the division could tell
    /// (<see cref="CommentEntryText.CandidateHeader"/>).</summary>
    private CommentEntryText BeginsCommentEntry(string text, int first, int lineNumber)
    {
        if (_builder.AwaitsFloatingContinuation) return CommentEntryText.None;
        _builder.ReadInto(_division);   // the division is that of the logical lines before this one
        if (!LogicalLineConverter.IsCommentEntryParagraph(text[first..LogicalLineConverter.WordEnd(text, first)], asReplaced)) return CommentEntryText.None;
        return _division.InIdentificationDivision switch
        {
            true => CommentEntryText.Header,
            null when pins?.Contains(file, inclusion, lineNumber) != true => CommentEntryText.CandidateHeader,   // unless a run proved it program text (CommentEntryPins)
            _ => CommentEntryText.None,
        };
    }
}
