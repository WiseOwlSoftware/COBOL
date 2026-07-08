// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// One structured edition diagnostic — the value <see cref="ConstructRegistry.Check"/> writes to an
/// <see cref="IDiagnosticSink"/>, replacing the stringly-typed <c>$"error {code}: {msg}"</c> concatenation
/// that lived on the old <c>EditionContext</c>. Rendering to text happens once, at the consumer boundary
/// (the compiler-side adapter reconstructs the exact legacy strings; a future unified diagnostics layer will
/// render from the descriptor — rearch P4/P7).
/// </summary>
/// <param name="Code">The diagnostic code (a 0900–0903 band member, or a pinned 08xx).</param>
/// <param name="Severity">Error (fails the compile) or Warning (never fails) — from <see cref="EditionSeverityPolicy"/>.</param>
/// <param name="ConstructId">The <c>constructs.json</c> row id (empty for a non-registry diagnostic).</param>
/// <param name="Message">The fully-rendered human message.</param>
/// <param name="Where">Localizer text ("FD OUT-FILE", "paragraph P1", …).</param>
/// <param name="Citation">ISO § / VCR-row citation.</param>
/// <remarks>
/// No source <c>Location</c> is carried in PHASE 02: <c>Cobol.Net.Editions</c> is BELOW
/// <c>Cobol.Net.Frontend</c> and cannot reference the frontend's <c>SourceLocation</c>; the compiler-side
/// adapter's <c>List&lt;string&gt;</c> channels do not need it. A shared location type unifies in P4/P7.
/// </remarks>
public readonly record struct EditionDiagnostic(
    string Code,
    EditionSeverity Severity,
    string ConstructId,
    string Message,
    string Where,
    string Citation);
