// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// The diagnostic CURSOR (kb/Work PB82): the RESULTANT-text position — the ANTLR token line (1-based) and column
/// (0-based) — of the construct a walker is currently binding or validating. A sink that carries a cursor stamps
/// it onto every diagnostic reported while it is set, so no report site passes a location and a new site cannot
/// forget one. <see cref="Line"/> 0 is "no cursor" (a diagnostic about the compilation unit as a whole).
/// The RESULTANT line is mapped to the user's source file and line by the sink (the compiler-side
/// <c>EditionContext</c> consults the preprocessing chain's <c>SourceLineMap</c>), never by the reporter.
/// </summary>
public readonly record struct DiagnosticCursor(int Line, int Column)
{
    /// <summary>True when a position is set (a positive line).</summary>
    public bool IsSet => Line > 0;
}

/// <summary>Restores the sink's previous cursor when disposed — the RAII half of
/// <see cref="DiagnosticCursorExtensions.At(IDiagnosticSink, int, int)"/>. A struct: positioning is on the
/// per-statement / per-entry path and must not allocate.</summary>
public readonly struct DiagnosticCursorScope(IDiagnosticSink sink, DiagnosticCursor saved) : IDisposable
{
    public void Dispose() => sink.Cursor = saved;
}

/// <summary>The ONE way a walker positions a sink's cursor: <c>using var _ = sink.At(line, column);</c> around the
/// bind / validation of one construct — nested constructs restore the outer position on exit, so the cursor at
/// any moment is the INNERMOST construct being processed.</summary>
public static class DiagnosticCursorExtensions
{
    /// <summary>Position <paramref name="sink"/> at RESULTANT <paramref name="line"/> (1-based; 0 = clear) and
    /// <paramref name="column"/> (0-based) until the returned scope is disposed.</summary>
    public static DiagnosticCursorScope At(this IDiagnosticSink sink, int line, int column)
    {
        var saved = sink.Cursor;
        sink.Cursor = new DiagnosticCursor(line, column);
        return new DiagnosticCursorScope(sink, saved);
    }

    /// <summary>Position at a captured cursor; an UNSET cursor leaves the current position standing (a synthetic
    /// item reports at whatever construct is being processed, never at "line 0").</summary>
    public static DiagnosticCursorScope At(this IDiagnosticSink sink, DiagnosticCursor at) =>
        at.IsSet ? sink.At(at.Line, at.Column) : sink.At(sink.Cursor.Line, sink.Cursor.Column);
}
