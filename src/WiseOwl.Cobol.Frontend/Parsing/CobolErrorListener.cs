// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ANTLR4 error listener that feeds syntax errors into a <see cref="DiagnosticBag"/>.
/// Extracts [COBOLxxxx] diagnostic codes from messages produced by CobolErrorStrategy
/// and caps total errors at <see cref="MaxErrors"/> to prevent cascading noise. Given the compilation's
/// <see cref="IMarginRCutLocator"/> it also names the margin-R cut that may be why a syntax error stands where it does
/// (<see cref="NoteMarginRCut"/>, kb/Work PB2555).
/// </summary>
public sealed class CobolErrorListener(DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null,
    IMarginRCutLocator? cuts = null)
    : BaseErrorListener
{
    /// <summary>
    /// Maximum number of parse errors to report per file.
    /// After this limit, additional errors are silently dropped.
    /// </summary>
    public const int MaxErrors = 20;

    private int _errorCount;

    /// <summary>Start offsets of the offending tokens already reported as COBOL0901 (kb/Work PB693).
    /// ANTLR can raise TWO syntax errors on one offending token — the prediction failure and
    /// <c>CobolErrorStrategy</c>'s recovery message — and the strategy names the word's §8.9 violation in both, so
    /// without this the user is told twice that the word is reserved. ISO §8.3.2.1 rule 1 is violated ONCE by one
    /// occurrence of the word, and that is how often it is reported; bounded by <see cref="MaxErrors"/>.</summary>
    private readonly HashSet<int> _reservedWordReported = [];

    /// <summary>The physical lines (file, 0-based line) whose margin-R cut has been noted already: one note per cut, however
    /// many syntax errors stand beside it (kb/Work PB2555).</summary>
    private readonly HashSet<(string File, int Line)> _cutNoted = [];

    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        if (_errorCount >= MaxErrors)
            return;

        // Extract the [COBOLxxxx] code prefix from the message if present. ⛔ THE CODE IS THE STRATEGY'S (kb/Work PB693,
        // PB4132). The §8.9 arm, a syntax error on a reserved reservation-gated word written where a user-defined word
        // stands (COBOL0901), is decided where every other diagnosis of the token is ranked
        // (CobolErrorStrategy.ReservedWordUse), so a misplaced keyword's own diagnosis, a stray END-DISPLAY's COBOL0306,
        // is never overwritten after the fact here.
        string code = Diagnostics.DiagnosticDescriptors.COBOL0001.Code;
        string message = msg;
        if (msg.StartsWith('['))
        {
            int closeBracket = msg.IndexOf(']');
            if (closeBracket > 1)
            {
                code = msg[1..closeBracket];
                message = msg[(closeBracket + 1)..].TrimStart();
            }
        }

        // Dropped BEFORE the error is counted, so a suppressed duplicate does not eat a slot of the MaxErrors budget.
        if (code == WiseOwl.Cobol.Editions.EditionCodes.ReservedWord && offendingSymbol is not null
            && !_reservedWordReported.Add(offendingSymbol.StartIndex))
            return;

        _errorCount++;

        // ANTLR's `line` and `charPositionInLine` count the RESULTANT text (after the reference-format conversion,
        // COPY, REPLACE and continuation joins). The map names the file, physical line AND column the user edits (a
        // copybook's own path and line for an error inside copied text — kb/Work PB82; the column of the physical
        // line, not of the program-text area the fixed-form conversion kept — kb/Work PB1688, PB1801).
        var location = lineMap?.Locate(line, charPositionInLine, sourcePath)
                       ?? new SourceOrigin(sourcePath, line).ToLocation(charPositionInLine);
        var span = new TextSpan(offendingSymbol?.StartIndex ?? 0,
            offendingSymbol?.StopIndex ?? 0);
        // ⛔ A CAUSE READS BEFORE ITS EFFECT (kb/Work PB4131): the cut precedes the error it may explain.
        NoteMarginRCut(recognizer, offendingSymbol, location);
        NoteWithdrawnWord(recognizer, offendingSymbol, location);
        NoteDirectingStatementSynonym(recognizer, offendingSymbol, location);
        diagnostics.ReportError(code, message, location, span);
    }

    /// <summary>⛔ A WORD A &gt;&gt;COBOL-WORDS DIRECTIVE WITHDREW IS NAMED BESIDE THE SYNTAX ERROR IT MAY CAUSE (kb/Work
    /// PB4360; ISO §7.3.10.4 GR3, GR4). After <c>&gt;&gt;COBOL-WORDS UNDEFINE "DISPLAY"</c> the word DISPLAY "shall no
    /// longer be reserved or restricted in any way", and any syntax requiring it "shall not be available for use in this
    /// compilation group": <c>DISPLAY "X"</c> is a user-defined word and a literal, not a statement, and the bare COBOL0001
    /// at the literal said nothing of the directive that made it so. The withdrawn word stands at the offending token or
    /// right before it (the statement it would begin failed at its first operand), the evidence the margin-R note reads
    /// too; <see cref="CobolWordsMap.RemovedBy"/> names the directive, as COBOL5776 does for an intrinsic function name
    /// (kb/Work PB4309). Reported once per directive, as a warning beside the error, at the directive.</summary>
    private void NoteWithdrawnWord(IRecognizer? recognizer, IToken? offending, SourceLocation errorAt)
    {
        if (offending is null || recognizer is not CobolParserCoreBase { CobolWords: { IsEmpty: false } words }) return;
        foreach (var token in (ReadOnlySpan<IToken?>)[offending, PreviousToken(recognizer, offending)])
        {
            // The word the SOURCE wrote: a synonym of a word the lexer has a token for carries its original's text
            // (kb/Work PB4573), and SHOW is no withdrawn DISPLAY.
            if (token is null || WrittenSource.Of(token) is not { } written || words.RemovedBy(written) is not { } op) continue;
            if (!_withdrawnNoted.Add(op)) return;
            string word = CobolKeywords.Spelling(written);
            diagnostics.ReportWarning(DiagnosticCatalog.WordWithdrawnBeforeSyntaxError.Code,
                $"{op.Withdrew(word)}: '{written}' is a user-defined word in this compilation group, and syntax that requires the "
                + $"word {word} is not available in it. The syntax error at {errorAt} may be this.",
                lineMap?.Locate(op.At.Line, op.At.Column, sourcePath) ?? new SourceOrigin(sourcePath, op.At.Line).ToLocation(op.At.Column),
                TextSpan.Empty);
            return;
        }
    }

    /// <summary>The directives already named beside a syntax error (<see cref="NoteWithdrawnWord"/> and
    /// <see cref="NoteDirectingStatementSynonym"/>): one note each.</summary>
    private readonly HashSet<CobolWordsOp> _withdrawnNoted = [];

    /// <summary>⛔ A SYNONYM OF COPY OR REPLACE IS NAMED BESIDE THE SYNTAX ERROR IT MAY CAUSE (kb/Work PB4488; ISO §7.3.10.4
    /// GR6). "A COBOL-WORDS directive does not affect any Compiler directing statements or Compiler directives", and the
    /// compiler directing statements are the COPY and REPLACE statements (§7.2.2.2;
    /// <see cref="CobolWordsMap.CompilerDirectingStatementWords"/>), found and applied in Steps 1 to 3 of §7.2.1, before the
    /// directive's actions in Step 4. After <c>EQUATE "COPY" WITH "CPY"</c> the line <c>CPY BOOK.</c> is no COPY statement:
    /// CPY is a synonym for a word that begins no statement of the compilation stage, and the syntax error at BOOK said "a
    /// period may be missing" of a sentence that has its period. The error stands (the source is not legal); the synonym
    /// stands at the offending token or right before it, the evidence <see cref="NoteWithdrawnWord"/> reads too, and
    /// <see cref="CobolWordsMap.DirectingStatementSynonymOf"/> names the directive. Reported once per directive, as a warning
    /// beside the error, at the directive.</summary>
    private void NoteDirectingStatementSynonym(IRecognizer? recognizer, IToken? offending, SourceLocation errorAt)
    {
        if (offending is null || recognizer is not CobolParserCoreBase { CobolWords: { IsEmpty: false } words }) return;
        foreach (var token in (ReadOnlySpan<IToken?>)[offending, PreviousToken(recognizer, offending)])
        {
            if (token?.Text is not { } written || words.DirectingStatementSynonymOf(written) is not { } op) continue;
            if (!_withdrawnNoted.Add(op)) return;
            (string verb, string joiner) = op.Action == CobolWordsAction.Equate ? ("EQUATE", "WITH") : ("SUBSTITUTE", "BY");
            diagnostics.ReportWarning(DiagnosticCatalog.DirectingStatementSynonymBeforeSyntaxError.Code,
                $">>COBOL-WORDS {verb} \"{op.Existing}\" {joiner} \"{op.New}\": a COBOL-WORDS directive does not affect a compiler "
                + $"directing statement (ISO §7.3.10.4 GR6), so '{written}' begins no {op.Existing} statement; a {op.Existing} "
                + $"statement is written with the word {op.Existing}. The syntax error at {errorAt} may be this.",
                lineMap?.Locate(op.At.Line, op.At.Column, sourcePath) ?? new SourceOrigin(sourcePath, op.At.Line).ToLocation(op.At.Column),
                TextSpan.Empty);
            return;
        }
    }

    /// <summary>Name the margin-R cut that may be why the syntax error at <paramref name="errorAt"/> stands there (kb/Work
    /// PB2555; ISO §6.3.1: the program-text area ends at margin R, which the implementor fixes, so the text past it is not
    /// program text and the reference format drops it). The error is the cut's effect in three shapes, each with its evidence
    /// in the token stream, and only where the cut text may have been program text (<see cref="MarginRCut"/>):
    /// <list type="bullet">
    /// <item>the OFFENDING token ends the program text of its line AT margin R and the cut text begins right there
    /// (<see cref="LocatedMarginRCut.EndsAtMarginR"/>): a word the margin cut in two, <c>END-SEN</c> for <c>END-SEND</c>;</item>
    /// <item>the token BEFORE the offending one is the last of a physical line that was cut and the offending token stands on
    /// a later line: the statement the line began ended early and the parse failed at the next one, <c>STOP</c> after a
    /// <c>CALL</c> whose last arguments lay past margin R;</item>
    /// <item>a line BETWEEN the two tokens holds text ONLY past margin R: its program-text area is blank, so it reads as blank
    /// and a statement written there vanished wholly (kb/Work PB4372). A line between the tokens that HOLDS program text and
    /// yet no token is one a later stage consumed (a directive, a COPY statement), and a line the compiler never read is
    /// likewise no cut's effect, however little program text it holds (kb/Work PB4412): one a text stage OMITTED (a false
    /// conditional-compilation branch, ISO §7.3.16.4 GR 3: "all lines of text-1 are omitted from the resultant text", which
    /// <see cref="IMarginRCutLocator.MarginRCutAt"/> answers null for) or a debugging line compiled without debugging mode
    /// (<see cref="DebuggingLineRewriter.AbsentLines"/>). Neither is asked about.</item>
    /// </list>
    /// Reported once per cut, as a warning beside the error.</summary>
    private void NoteMarginRCut(IRecognizer? recognizer, IToken? offending, SourceLocation errorAt)
    {
        if (cuts is null || offending is null) return;
        if (offending.StopIndex >= offending.StartIndex
            && cuts.MarginRCutAt(offending.Line, offending.Column + (offending.StopIndex - offending.StartIndex)) is { } inWord
            && inWord.EndsAtMarginR && !inWord.Cut.IsSequenceNumber)
        {
            ReportMarginRCut(inWord, $"the word '{offending.Text}' ends at position {inWord.Cut.LastProgramTextPosition} and the text after it "
                + "was cut, so the word may have been longer", errorAt);
            return;
        }
        if (PreviousToken(recognizer, offending) is not { } previous) return;
        if (cuts.MarginRCutAt(previous.Line, previous.Column + (previous.StopIndex - previous.StartIndex)) is { } before
            && !before.Cut.IsSequenceNumber
            && (before.At.FileName, before.At.Line) != (errorAt.FileName, errorAt.Line))
            ReportMarginRCut(before, "the statement that line began may have continued past it", errorAt);
        // A line between the two tokens holds no token, so none anchors a question about it. One that holds text ONLY past
        // margin R reads as blank, and a statement written there vanishes wholly (kb/Work PB4372). A line that HOLDS program
        // text and yet stands tokenless is one a stage after the reference format consumed (a directive, a COPY statement),
        // so it is not asked about. Nor is a line the compiler never read, which holds no program text either (kb/Work
        // PB4412): one a text stage OMITTED (a false >>IF branch; the locator answers null for it, LineOrigin.Omitted) or a
        // debugging line without debugging mode (a comment line: the rewriter's decision, read from the token stream).
        // The column is immaterial: the question is of the line.
        var absentDebugging = previous.Line + 1 < offending.Line && recognizer is Parser { TokenStream: { } stream }
            ? DebuggingLineRewriter.AbsentLines(stream, previous.TokenIndex, offending.TokenIndex)
            : null;
        for (int line = previous.Line + 1; line < offending.Line; line++)
            if (absentDebugging?.Contains(line) != true
                && cuts.MarginRCutAt(line, 0) is { } between && !between.Cut.HoldsProgramText && !between.Cut.IsSequenceNumber
                && (between.At.FileName, between.At.Line) != (errorAt.FileName, errorAt.Line))
                ReportMarginRCut(between, "the statement written on that line vanished, so the parse failed at the next one", errorAt);
    }

    /// <summary>The token before <paramref name="token"/> on the parser's default channel, or null when there is none or no
    /// stream to ask (a report made outside a parse).</summary>
    private static IToken? PreviousToken(IRecognizer? recognizer, IToken token)
    {
        if (recognizer is not Parser { TokenStream: { } stream } || token.TokenIndex <= 0) return null;
        for (int i = token.TokenIndex - 1; i >= 0; i--)
            if (stream.Get(i) is { Channel: TokenConstants.DefaultChannel, StopIndex: var stop, StartIndex: var start } found
                && stop >= start)
                return found;
        return null;
    }

    private void ReportMarginRCut(LocatedMarginRCut cut, string consequence, SourceLocation errorAt)
    {
        if (!_cutNoted.Add((cut.At.FileName, cut.At.Line))) return;
        var dropped = cut.Cut;
        string shown = CharacterPositions.Count(dropped.Text) > MaxDroppedShown
            ? dropped.Text[..CharacterPositions.IndexAt(dropped.Text, MaxDroppedShown)] + "..."
            : dropped.Text;
        // The message counts POSITIONS, which a tab fills and a character is one of; the location beside it is the COLUMN the
        // file holds the cut at (kb/Work PB4373), so a line whose two differ says so rather than state two margins.
        string held = dropped.Column == dropped.LastProgramTextPosition
            ? ""
            : " (a tab counts the positions it fills, so the column this warning names, where the file holds the cut, is less than that)";
        // A line whose program-text area is blank has no "end" to name: all of its text lies past margin R (kb/Work PB4372).
        (string cutAt, string remedy) = dropped.HoldsProgramText
            ? ($"the program text of this line was cut at margin R: it ends at position {dropped.LastProgramTextPosition}{held}, and what "
               + $"follows, '{shown}' (positions {dropped.FirstCutPosition} to {dropped.LastCutPosition}), is outside the "
               + "program-text area and was ignored (ISO §6.3.1).",
               $"Shorten the line to end at position {dropped.LastProgramTextPosition} or continue it on the next line.")
            : ($"this line has no program text: all that it holds, '{shown}' (positions {dropped.FirstCutPosition} to "
               + $"{dropped.LastCutPosition}), lies past margin R, after position {dropped.LastProgramTextPosition}{held}, outside "
               + "the program-text area, so it was ignored and the line reads as blank (ISO §6.3.1).",
               $"Write the text within positions 8 to {dropped.LastProgramTextPosition}.");
        diagnostics.ReportWarning(DiagnosticCatalog.ProgramTextPastMarginR.Code,
            $"{cutAt} The syntax error at {errorAt} may be this: {consequence}. {remedy}", cut.CutLocation, TextSpan.Empty);
    }

    /// <summary>The most characters of the cut text a note quotes.</summary>
    private const int MaxDroppedShown = 24;
}
