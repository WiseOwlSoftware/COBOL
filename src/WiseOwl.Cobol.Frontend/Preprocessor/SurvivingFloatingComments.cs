// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// THE COBOL-85 GATE OF THE FLOATING COMMENT INDICATOR, asked of the text the text stages have FINISHED with (kb/Work PB5470).
/// COBOL-85 has no floating comment indicator: below COBOL-2002 the fixed-form logical conversion leaves <c>*&gt;</c> in the
/// text as the ordinary text-word it is (§1985:III, "Text-word": "Any other sequence of contiguous COBOL characters except
/// comment lines and the word 'COPY'"), so COPY and REPLACE read it in library text and in pseudo-text, and only a
/// <c>*&gt;</c> that SURVIVES them into the text the lexer reads is the 2002 introduction the edition refuses
/// (<c>floating-comment-indicator-2002</c>, COBOL0900, and COBOL2495 when it follows a character directly). The lexer
/// still takes the rest of the line as the comment it is written as, so one refusal is the only difference a COBOL-2002
/// reading has made. Free form, and every edition from 2002, never reach this gate: their conversion removed the comments.
/// <para>Line-count preserving: it reads the text and reports, and changes nothing.</para>
/// </summary>
internal static class SurvivingFloatingComments
{
    /// <summary>Report every floating comment indicator <paramref name="text"/> still holds outside a literal, when
    /// <paramref name="gates"/>'s edition has none.</summary>
    public static void Report(MappedText text, ReferenceFormatDiagnostics gates)
    {
        if (gates.FixedFormReadsFloatingComments) return;
        string source = text.Text;
        int pos = 0;
        while (TextWordScanner.TryNext(source, ref pos, out var word))
        {
            if (word.Kind != TextWordKind.CharacterString) continue;
            int at = word.Value.IndexOf(CobolSourceText.CommentIndicator, StringComparison.Ordinal);
            if (at < 0) continue;
            int indicator = word.Start + at;
            var span = text.PointAt(indicator).Spans[0];
            gates.OnFloatingComment(span.Origin.File, span.Origin.Line, span.SourceColumn);
            if (indicator > 0 && !CobolSpace.IsSeparator(source[indicator - 1]))   // the line's start is preceded by an implied space
                gates.OnUnseparatedFloatingComment(span.Origin.File, span.Origin.Line, span.SourceColumn);
        }
    }
}
