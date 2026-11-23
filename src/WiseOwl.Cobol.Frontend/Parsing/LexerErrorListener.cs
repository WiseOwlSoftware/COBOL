// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE LEXER REPORTS INTO THE COMPILATION'S DIAGNOSTICS, NEVER ON THE CONSOLE (kb/Work PB4233). A lexer left with
/// ANTLR's default listener (<c>ConsoleErrorListener</c>) prints a recognition error to standard error, drops the text
/// it could not match and goes on, so the compile it belongs to succeeds: <c>01 X PIC.</c> matched no PICMODE rule and
/// compiled with the next line's <c>X(3)</c> as its picture. Every mode of <c>CobolLexer.g4</c> is total, so an error here
/// is a compiler defect (COBOL5704, <see cref="DiagnosticCatalog.LexerTextUnrecognized"/>), and it fails the compile.
/// <para>The errors are COLLECTED, not reported as they occur, because the main lex runs more than once: a fixed-form
/// debugging line that is source inside an open picture or subscript region is blanked and the text lexed again
/// (<c>Frontend.LexAndParseOnce</c>, kb/Work PB1913), and only the last lexer read the text the parse sees. The caller
/// reports that lexer's errors (<see cref="ReportInto"/>). The lexer reports to it alone: <see cref="CobolTokenStream.Lexer"/>,
/// the one construction of a lexer, removes every other listener.</para>
/// </summary>
internal sealed class LexerErrorListener : IAntlrErrorListener<int>
{
    private readonly List<(int Line, int Column, string Message)> _errors = [];

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
        => _errors.Add((line, charPositionInLine, msg));

    /// <summary>The first text line, among those <paramref name="onLine"/> accepts, of an error the lexer met, or null when none.</summary>
    public int? FirstLineWhere(Func<int, bool> onLine)
        => _errors.Where(e => onLine(e.Line)).Select(e => (int?)e.Line).Min();

    /// <summary>Report every error the lexer met into <paramref name="diagnostics"/>, located through
    /// <paramref name="lineMap"/> as a syntax error is.</summary>
    public void ReportInto(DiagnosticBag diagnostics, string sourcePath, SourceLineMap lineMap)
    {
        foreach (var (line, column, message) in _errors)
            diagnostics.ReportError(DiagnosticCatalog.LexerTextUnrecognized.Code,
                $"{DiagnosticCatalog.LexerTextUnrecognized.Title} The lexer reported: {message}.",
                lineMap.Locate(line, column, sourcePath), default);
    }
}
