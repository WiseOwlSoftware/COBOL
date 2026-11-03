// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

public static partial class ReferenceFormatProcessor
{
    /// <summary>The logical conversion of ONE CARRIED comment-entry candidate (<see cref="CommentEntryCarrier"/>: the header
    /// line and the lines of its entry, in order) once <see cref="CommentEntryResolver"/> knows the division the compiled
    /// text stands in (docs/CONFORMANCE.md D-CE2; kb/Work PB4281). The carried lines are physical lines of one text in one
    /// reference format, so the two outcomes are the ones the conversion would have produced had it known:
    /// <list type="bullet">
    /// <item><paramref name="inIdentificationDivision"/>: a comment-entry (docs/CONFORMANCE.md D-CE1). The paragraph HEADER
    /// stays program text — the parser and the one removal gate (<c>VersionConformancePass</c>, COBOL0902 from 2002) must
    /// see the paragraph in fixed form as they do in free form (kb/Work PB1494, PB1758) — and only the entry is
    /// discarded. The entry's end is implied by the next Area-A word (COBOL-85), so the paragraph's terminating period is
    /// written explicitly: <c>AUTHOR. .</c> is the paragraph <c>AUTHOR DOT DOT</c> with no content. No reference-format
    /// diagnostic is asked of text that is commentary.</item>
    /// <item>otherwise: a paragraph of the program, converted as the ordinary lines they are, through the same walker and
    /// the compilation's reference-format diagnostics.</item>
    /// </list>
    /// The result has at most one line per carried line; the caller keeps the slots the carriers held.</summary>
    internal static (List<string> Lines, List<LineOrigin> Origins) ConvertCommentEntryCandidate(
        IReadOnlyList<CommentEntryCarrier.Carried> candidate, string file, bool inIdentificationDivision,
        ReferenceFormatDiagnostics? gates)
    {
        bool isFixed = candidate[0].IsFixed;
        if (!inIdentificationDivision)
        {
            bool? noComments = false;   // the converter reads no comment-entry where the division is none
            return ConvertSegment([.. candidate.Select(c => c.Line)], isFixed, gates, file, candidate[0].CcvsIndicators, ref noComments);
        }
        var builder = new LogicalLineBuilder(file, gates, isFixed);
        foreach (var carried in candidate)
        {
            var line = carried.Line;
            // The program text and where it begins in the line: a fixed-form line's program-text area, a free-form line whole.
            var (area, index) = isFixed ? FixedProgramText(line.Text) : (line.Text, 0);
            var site = new TextSite(line.Number, index, line.Columns);
            if (carried.IsHeader) builder.Emit(CommentEntryHeader(area), site);
            else builder.Discard(site);
        }
        return builder.Result;
    }

    private static (string Area, int Index) FixedProgramText(string text)
    {
        var line = new FixedFormLine(text);
        return (line.ProgramText, line.ProgramTextIndex);
    }
}
