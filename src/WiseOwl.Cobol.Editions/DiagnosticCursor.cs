// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

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

    /// <summary>The <see cref="DiagnosticRegion"/> the construct sits in, null for the user's own text (kb/Work PB4469). It travels
    /// with the position, so a diagnostic a post-build pass reports from a captured cursor is attributed exactly as one reported while
    /// the construct bound.</summary>
    public DiagnosticRegion? Region { get; init; }
}

/// <summary>⛔ THE TEXT OF AN EXPANSION, AND WHO ANSWERS FOR IT (kb/Work PB4469, PB4483). A parameterized class's expansion is the
/// skeleton's tokens with the formals replaced (ISO §12.3.8.4 GR5), and the group that asked for it binds that text to type its callers.
/// A sink reads the region of the cursor to know which text a diagnostic is about. Two kinds of text, two rules:
/// <list type="bullet">
/// <item><b>A skeleton written in this group</b> (<see cref="ReportedAt"/> unset) is the user's own text: the group reports it at the
/// positions the user wrote it at, once for every expansion (a fact about one position is one fact), wherever the expansion is placed.</item>
/// <item><b>A skeleton read from the repository</b> (<see cref="ReportedAt"/> set) is a text this compilation never read as a file. A
/// diagnostic about it is reported at the one place the user wrote it down, the EXPANDS entry, naming the expansion
/// (<see cref="InTheExpansion"/>), and only by the compilation that COMPILES the text: the group for an expansion in its own assembly, the
/// expansion's own compilation for one in an assembly of its own, and the compilation that made it for one imported from the
/// repository. The group withdraws what it raised for any other (<c>EditionContext.WithdrawWarningsOf</c>).</item>
/// </list>
/// Identity is the object: two regions of one name are two regions.</summary>
/// <param name="name">What the region is, for the reader of a debugger or a message (an expansion's name).</param>
/// <param name="reportedAt">Where every diagnostic of a text read from the repository is reported (the EXPANDS entry); unset for the
/// user's own text.</param>
public sealed class DiagnosticRegion(string name, DiagnosticCursor reportedAt = default)
{
    /// <summary>What the region is (an expansion's name).</summary>
    public string Name { get; } = name;

    /// <summary>Where a diagnostic of the region's text is reported when that text is not the user's own: the EXPANDS entry. Unset
    /// (<see cref="DiagnosticCursor.IsSet"/> false) for a skeleton written in this group, whose diagnostics keep their own positions.</summary>
    public DiagnosticCursor ReportedAt { get; } = reportedAt;

    /// <summary>The text is a skeleton read from the repository: it is reported at <see cref="ReportedAt"/>, names the expansion, and is
    /// answered for by the compilation that compiles it.</summary>
    public bool IsRepositoryText => ReportedAt.IsSet;

    /// <summary>The words that follow the message of a diagnostic reported at the EXPANDS entry, so the text it concerns can be found —
    /// the ONE spelling, for the group's report and for the report of an expansion's own compilation alike.</summary>
    public static string InTheExpansion(string name) => $"[in the expansion '{name}']";

    /// <inheritdoc />
    public override string ToString() => Name;
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

    /// <summary>Position at a captured cursor, its <see cref="DiagnosticCursor.Region"/> with it; an UNSET cursor leaves the current
    /// position standing (a synthetic item reports at whatever construct is being processed, never at "line 0").</summary>
    public static DiagnosticCursorScope At(this IDiagnosticSink sink, DiagnosticCursor at)
    {
        var saved = sink.Cursor;
        if (at.IsSet) sink.Cursor = at;
        return new DiagnosticCursorScope(sink, saved);
    }
}
