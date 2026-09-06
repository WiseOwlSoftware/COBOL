// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The ONE adapter from an <see cref="EditionDiagnostic"/> (what <see cref="ConstructRegistry.Check"/> produces)
/// to the text-manipulation stage's <see cref="DiagnosticBag"/>, anchored at one source location.
///
/// <para>It existed as FOUR byte-identical private <c>BagSink</c> classes — one each in the COBOL-WORDS, FLAG,
/// LEAP-SECOND and REF-MOD-ZERO-LENGTH directive processors — because each of those stages ran its own copy of
/// the edition gate. kb/Work PB725 moved the gate to the single point where a <c>&gt;&gt;</c> word is recognized,
/// which left one caller and one adapter. The severity mapping is the registry's, never a local decision: an
/// <see cref="EditionSeverity.Error"/> verdict is an error and anything else a warning, with the strict/permissive
/// choice already made by <see cref="EditionSeverityPolicy"/> upstream.</para>
/// </summary>
internal sealed class BagSink(DiagnosticBag bag, SourceLocation loc) : IDiagnosticSink
{
    public void Report(in EditionDiagnostic d) => bag.Report(d.Code,
        d.Severity == EditionSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
        d.Message, loc, default);
}
