// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// THE COBOL-85 GATE OF THE FLOATING COMMENT INDICATOR, asked of the text the text stages have FINISHED with (kb/Work PB5470),
/// and answered once the debugging comment treatment has decided which of that text is program text (kb/Work PB5588).
/// COBOL-85 has no floating comment indicator: below COBOL-2002 the fixed-form logical conversion leaves <c>*&gt;</c> in the
/// text as the ordinary text-word it is (§1985:III, "Text-word": "Any other sequence of contiguous COBOL characters except
/// comment lines and the word 'COPY'"), so COPY and REPLACE read it in library text and in pseudo-text, and only a
/// <c>*&gt;</c> that SURVIVES them into the text the lexer reads is the 2002 introduction the edition refuses
/// (<c>floating-comment-indicator-2002</c>, COBOL0900, and COBOL2495 when it follows a character directly). The lexer
/// still takes the rest of the line as the comment it is written as, so one refusal is the only difference a COBOL-2002
/// reading has made. Free form, and every edition from 2002, never reach this gate: their conversion removed the comments.
/// <para>Two steps, because two stages hold the facts. <see cref="Note"/> reads the finished text and finds each <c>*&gt;</c>;
/// <see cref="ReferenceFormatDiagnostics.ReportSurvivingFloatingComments"/> reports the ones that are program text, after
/// <c>DebuggingLineRewriter</c> has made a debugging section's and a debugging line's text comment where no WITH DEBUGGING MODE
/// applies (§1985:XV.2.1.3 GR2, §1985:VI.4.3.4 GR5: "compiled as if they were comment lines"), and a comment line may hold any
/// character of the computer's character set (§1985:III, "Comment Line"). Line-count preserving: it reads the text and
/// changes nothing.</para>
/// </summary>
internal static class SurvivingFloatingComments
{
    /// <summary>Hand <paramref name="gates"/> every floating comment indicator <paramref name="text"/> still holds outside a
    /// literal, when its edition has none.</summary>
    public static void Note(MappedText text, ReferenceFormatDiagnostics gates)
    {
        if (gates.FixedFormReadsFloatingComments) return;
        string source = text.Text;
        int pos = 0, line = 1, counted = 0;   // the line of the text the lexer reads, counted up to the last indicator
        while (TextWordScanner.TryNext(source, ref pos, out var word))
        {
            if (word.Kind != TextWordKind.CharacterString) continue;
            int at = word.Value.IndexOf(CobolSourceText.CommentIndicator, StringComparison.Ordinal);
            if (at < 0) continue;
            int indicator = word.Start + at;
            var span = text.PointAt(indicator).Spans[0];
            for (; counted < indicator; counted++)
                if (source[counted] == '\n') line++;
            // the line's start is preceded by an implied space
            bool unseparated = indicator > 0 && !CobolSpace.IsSeparator(source[indicator - 1]);
            gates.NoteSurvivingFloatingComment(new(line, span.Origin.File, span.Origin.Line, span.SourceColumn, unseparated));
        }
    }
}
