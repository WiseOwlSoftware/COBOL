// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Parsing;

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

        _errorCount++;

        // Extract [COBOLxxxx] code prefix from message if present
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

        // ANTLR's `line` is 1-based and counts the RESULTANT text (after COPY / REPLACE / continuation joins);
        // SourceLocation.Line is 0-based (its ToString adds the 1 back). Passing the token line straight through
        // reported every parse error one line late — kb/Work PB82. The map names the file and physical line the
        // user edits (a copybook's own path and line for an error inside copied text).
        var origin = lineMap?.Origin(line) ?? new SourceOrigin(sourcePath, line);
        var location = new SourceLocation(origin.File, 0, Math.Max(origin.Line - 1, 0), charPositionInLine);
        var span = new TextSpan(offendingSymbol?.StartIndex ?? 0,
            offendingSymbol?.StopIndex ?? 0);
        diagnostics.ReportError(code, message, location, span);
    }
}
