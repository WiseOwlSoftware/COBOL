// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// The ONE adapter from an <see cref="EditionDiagnostic"/> (what <see cref="ConstructRegistry.Check"/> produces)
/// to the frontend's <see cref="DiagnosticBag"/>, in one of two forms: ANCHORED at one source location, or CURSORED.
///
/// <para>It existed as FOUR byte-identical private <c>BagSink</c> classes — one each in the COBOL-WORDS, FLAG,
/// LEAP-SECOND and REF-MOD-ZERO-LENGTH directive processors — because each of those stages ran its own copy of
/// the edition gate. kb/Work PB725 moved the gate to the single point where a <c>&gt;&gt;</c> word is recognized,
/// which left one caller and one adapter. The severity mapping is the registry's, never a local decision: an
/// <see cref="EditionSeverity.Error"/> verdict is an error and anything else a warning, with the strict/permissive
/// choice already made by <see cref="EditionSeverityPolicy"/> upstream.</para>
///
/// <para>The CURSORED form (kb/Work PB3845) serves a stage that reports at MANY positions: it keeps the diagnostic
/// cursor (<see cref="IDiagnosticSink.Cursor"/>, set through <c>sink.At(line, column)</c>) and locates each report
/// through <c>locate</c> — the resultant-text position the walker names, as the physical file, line and column the user
/// edits (<see cref="SourceLineMap.Locate(int, int, string)"/>). Both forms share the one mapping in
/// <see cref="Report"/>.</para>
/// </summary>
internal sealed class BagSink : IDiagnosticSink
{
    private readonly DiagnosticBag _bag;
    private readonly SourceLocation _loc;
    private readonly Func<int, int, SourceLocation>? _locate;

    /// <summary>A sink whose every diagnostic carries <paramref name="loc"/>.</summary>
    public BagSink(DiagnosticBag bag, SourceLocation loc)
    {
        _bag = bag;
        _loc = loc;
    }

    /// <summary>A sink that locates each diagnostic from the cursor the reporting stage set.</summary>
    public BagSink(DiagnosticBag bag, Func<int, int, SourceLocation> locate)
    {
        _bag = bag;
        _locate = locate;
    }

    public DiagnosticCursor Cursor { get; set; }

    public void Report(in EditionDiagnostic d) => _bag.Report(d.Code,
        d.Severity == EditionSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
        d.Message, _locate is null ? _loc : _locate(Cursor.Line, Cursor.Column), default);
}
