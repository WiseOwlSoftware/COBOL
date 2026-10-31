// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ANTLR4 error listener that feeds syntax errors into a <see cref="DiagnosticBag"/>.
/// Extracts [COBOLxxxx] diagnostic codes from messages produced by CobolErrorStrategy
/// and caps total errors at <see cref="MaxErrors"/> to prevent cascading noise.
/// </summary>
public sealed class CobolErrorListener(DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null)
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
        diagnostics.ReportError(code, message, location, span);
    }
}
