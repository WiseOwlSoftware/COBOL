// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Common;
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
        diagnostics.ReportError(code, message, location, span);
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
    /// yet no token is text the compiler OMITTED (a false conditional-compilation branch, ISO §7.3.16.4 GR 3: "all lines of
    /// text-1 are omitted from the resultant text"; a debugging line compiled without debugging mode), never a cut's effect,
    /// and is not asked about.</item>
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
        // text and yet stands tokenless was omitted by a stage after the reference format (a false >>IF branch, a debugging
        // line without debugging mode), so it is not asked about. The column is immaterial: the question is of the line.
        for (int line = previous.Line + 1; line < offending.Line; line++)
            if (cuts.MarginRCutAt(line, 0) is { } between && !between.Cut.HoldsProgramText && !between.Cut.IsSequenceNumber
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
        string shown = dropped.Text.Length > MaxDroppedShown ? dropped.Text[..MaxDroppedShown] + "..." : dropped.Text;
        // A line whose program-text area is blank has no "end" to name: all of its text lies past margin R.
        (string cutAt, string remedy) = dropped.HoldsProgramText
            ? ($"the program text of this line was cut at margin R: it ends at position {dropped.LastProgramTextPosition}, and what "
               + $"follows, '{shown}' (positions {dropped.FirstCutPosition} to {dropped.LastCutPosition}), is outside the "
               + "program-text area and was ignored (ISO §6.3.1).",
               $"Shorten the line to end at position {dropped.LastProgramTextPosition} or continue it on the next line.")
            : ($"this line has no program text: all that it holds, '{shown}' (positions {dropped.FirstCutPosition} to "
               + $"{dropped.LastCutPosition}), lies past margin R, outside the program-text area, so it was ignored and the line "
               + "reads as blank (ISO §6.3.1).",
               $"Write the text within positions 8 to {dropped.LastProgramTextPosition}.");
        diagnostics.ReportWarning(DiagnosticCatalog.ProgramTextPastMarginR.Code,
            $"{cutAt} The syntax error at {errorAt} may be this: {consequence}. {remedy}", cut.CutLocation, TextSpan.Empty);
    }

    /// <summary>The most characters of the cut text a note quotes.</summary>
    private const int MaxDroppedShown = 24;
}
