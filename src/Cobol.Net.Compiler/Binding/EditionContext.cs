// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Common;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// The per-compilation EDITION context — the bind-side half of the four-compilers-in-one mission. As of
/// rearchitecture PHASE 02 this is a thin ADAPTER over the two things it always really was: an immutable
/// <see cref="Editions.EditionInfo"/> value (the SINGLE source of the targeted ISO edition — 85 / 2002 / 2014 /
/// 2023, the CLI's <c>--std</c> — and the strict/permissive <c>--permissive</c> axis) plus a stringly-typed
/// diagnostic sink (the <see cref="Diagnostics"/> / <see cref="Warnings"/> channels). Splitting the two ends the
/// triple-sourcing of <c>DialectLevel</c> (P5) and lets the ONE <see cref="Editions.ConstructRegistry.Check"/>
/// funnel — now living BELOW the frontend in <c>Cobol.Net.Editions</c> — report here through
/// <see cref="IDiagnosticSink"/> without the frontend and the compiler each re-encoding the severity policy.
/// The public surface is unchanged, so every existing call site compiles untouched (the ~290-site adapter
/// guarantee; the actual retirement of this adapter is deferred to P7).
/// </summary>
/// <remarks>
/// Two channels, one policy seam (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.1):
/// <list type="bullet">
/// <item><see cref="Diagnostics"/> is ERRORS-ONLY — <see cref="CompilerDriver"/> fails the compile on ANY entry,
/// so nothing below error severity may ever be appended there.</item>
/// <item><see cref="Warnings"/> is the non-failing channel (obsolete/archaic 0903 flags; removed constructs under
/// permissive); <see cref="Warning"/> is its only writer. The driver surfaces it on every
/// <see cref="CompilerDriver.Result"/>, success or not.</item>
/// <item><see cref="Removed"/> is the ONE severity decision for removed-construct gating: error when strict,
/// warning when permissive — one policy, several emit sites (feedback_one_mechanism_per_job). The layer-neutral twin
/// is <see cref="Editions.EditionSeverityPolicy"/>, which <see cref="Report"/> bridges back onto these channels.</item>
/// </list>
/// </remarks>
public sealed class EditionContext(int dialectLevel, bool permissive = false) : IDiagnosticSink
{
    /// <summary>The immutable edition value — the SINGLE source of the dialect year + the permissive axis (P5).
    /// Constructed with the non-validating record ctor to preserve the historical tolerance of any
    /// <paramref name="dialectLevel"/>; NEW code that needs the {85,2002,2014,2023} guard uses
    /// <see cref="Editions.EditionInfo.Of(int, bool)"/>.</summary>
    public EditionInfo Edition { get; } = new(dialectLevel, permissive);

    /// <summary>This context AS the structured report channel handed to
    /// <see cref="Editions.ConstructRegistry.Check"/> (it bridges to the legacy string channels via
    /// <see cref="Report"/>).</summary>
    public IDiagnosticSink Sink => this;

    /// <summary>The targeted ISO edition year (85 / 2002 / 2014 / 2023) — delegated to <see cref="Edition"/>.</summary>
    public int DialectLevel => Edition.Year;

    /// <summary>The severity axis (VERSION_TEST_MATRIX_DESIGN §10 #1): strict (default) rejects removed
    /// constructs; permissive accepts them with a warning and the pre-removal semantics (the migration mode).
    /// Introduction gating (a construct NEWER than the edition, 0900) is an error on BOTH axes.</summary>
    public bool Permissive => Edition.Permissive;

    /// <summary>Bind-time rejection diagnostics. Any entry fails the compilation (CompilerDriver → BindError).</summary>
    public List<string> Diagnostics { get; } = [];

    /// <summary>Non-failing edition diagnostics (obsolete/archaic flags; removed constructs under
    /// <see cref="Permissive"/>). Printed to stderr by the CLI, carried on <see cref="CompilerDriver.Result"/>;
    /// never fails a compile. <see cref="Warning"/> is the only writer.</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>True when any failing diagnostic has been recorded (the CompilerDriver fail-fast gate).</summary>
    public bool HasErrors => Diagnostics.Count > 0;

    /// <summary>The fixed-point digit capacity of the targeted edition: 18 at COBOL-85, 31 at 2002+ (ISO
    /// §8.3.1.2 fixed-point literals 1–31 digits; the §14.7 composite-of-operands rules; PICTURE digit
    /// positions) — delegated to <see cref="Edition"/>.</summary>
    public int MaxDigits => Edition.MaxDigits;

    // ── The diagnostic cursor and the source-line map (kb/Work PB82) ──────────────────────────────────────────
    // A bind-time diagnostic used to be a bare "error CODE: message" — no file, no line, no column — because the
    // ~160 report sites pass no location and the channel accepted none. The fix is STRUCTURAL: the walkers
    // (StatementBinder per statement, DataBinder per entry, the parse-tree conformance arms per node) position the
    // cursor with `using var _ = edition.At(ctx)`, and Error/Warning stamp the prefix AUTOMATICALLY — no report
    // site changes and a new one cannot forget. The cursor is in RESULTANT-text space (the ANTLR token line after
    // COPY / REPLACE / continuation joins); the prefix maps it to the file and line the USER edits through
    // LineMap, the origin table the preprocessing chain built (Frontend.LineMap). SourceLineOf is the same map for
    // the bound tree's user-facing lines (DEBUG-LINE, EXCEPTION-LOCATION's third part).

    /// <summary>The RESULTANT-line → source-origin map of the compilation unit being bound; the driver sets it from
    /// the frontend. Null = the identity map on <see cref="SourceFile"/> (a unit-test bind without a frontend).</summary>
    public SourceLineMap? LineMap { get; set; }

    /// <summary>The main source file — the identity origin when no <see cref="LineMap"/> is set.</summary>
    public string SourceFile { get; set; } = "<source>";

    /// <summary>The <see cref="IDiagnosticSink.Cursor"/> — REAL here (the interface default is a no-op).</summary>
    public DiagnosticCursor Cursor { get; set; }

    /// <summary>The source origin (file, 1-based physical line) of RESULTANT line <paramref name="resultantLine"/>
    /// — the ONE mapping every user-facing consumer of a token line goes through.</summary>
    public SourceOrigin OriginOf(int resultantLine) =>
        LineMap?.Origin(resultantLine) ?? new SourceOrigin(SourceFile, resultantLine);

    /// <summary>The user's source line for RESULTANT line <paramref name="resultantLine"/> (the line in the file that
    /// physically holds it — the main source or a copybook).</summary>
    public int SourceLineOf(int resultantLine) => OriginOf(resultantLine).Line;

    /// <summary>The <c>file(line,col): </c> prefix for the current cursor — rendered by <see cref="SourceLocation"/>
    /// itself (the ONE format source; the parse-layer diagnostics print the same object), or empty when no cursor
    /// is set — a diagnostic about the compilation unit as a whole.</summary>
    private string Prefix() =>
        Cursor.IsSet ? OriginOf(Cursor.Line).ToLocation(Cursor.Column) + ": " : "";

    /// <summary>Record an edition-gating error (fails the compile).</summary>
    public void Error(string code, string message) => Diagnostics.Add($"{Prefix()}error {code}: {message}");

    /// <summary>Record a diagnostic keyed by a catalogue <see cref="DiagnosticDescriptor"/> (P2.10 — the
    /// first-class registry replacing bare <c>COBOLNETnnnn</c> string literals). Emits the descriptor's
    /// <see cref="DiagnosticDescriptor.Code"/> with the site-composed <paramref name="message"/> — byte-identical
    /// to the former bare-code call — while binding the site to a stable, documented, suppressible identity
    /// (<see cref="DiagnosticDescriptor.Id"/>). All P2-catalogued descriptors reachable here are error-severity;
    /// warning-severity descriptors route through <see cref="Warning"/> at their own sites.</summary>
    public void Error(DiagnosticDescriptor descriptor, string message) => Error(descriptor.Code, message);

    /// <summary>Record a non-failing edition diagnostic (the 0903 obsolete/archaic flags; removed constructs
    /// under <see cref="Permissive"/> via <see cref="Removed"/>).</summary>
    public void Warning(string code, string message) => Warnings.Add($"{Prefix()}warning {code}: {message}");

    /// <summary>The <see cref="Warning"/> twin of the descriptor-keyed <see cref="Error(DiagnosticDescriptor,string)"/>.
    /// ⛔ ITS ABSENCE WAS A DEFECT, not a gap: a descriptor-carrying site that wanted a warning had no descriptor
    /// overload to reach for, so <c>--permissive</c> sites passed <c>descriptor.Id</c> to the string overload
    /// instead and emitted the kebab SLUG in the code position. That shipped in COBOLNET1627 (PB1) and
    /// COBOLNET1628 (PB6) — both documented under codes they never printed. Warning and Error must offer the
    /// SAME shape, or the asymmetry writes the bug for the next caller.
    /// <see cref="DiagnosticEmitFormDriftTests"/> keeps it from returning.</summary>
    public void Warning(DiagnosticDescriptor descriptor, string message) => Warning(descriptor.Code, message);

    /// <summary>THE severity seam for removed-construct gating (P2.1): a construct the targeted edition REMOVED
    /// is an error under strict, a warning (with the pre-removal semantics preserved) under
    /// <see cref="Permissive"/>. Every removed/reserved-word emit site routes its severity decision through
    /// here — never a local strictness test.</summary>
    // NOTE (the OVERRIDE/FINAL wave, DEVLOG 605): this seam now also carries DOCUMENTED-DIALECT-LENIENCY
    // gating (e.g. §11.7 SR4a redefinition-without-OVERRIDE, 0837 — error strict, warning + the pre-wave
    // name-match inference under --permissive), not only removed-construct gating: ONE policy seam, never a
    // parallel Lenient() method or a local Permissive test (feedback_one_mechanism_per_job).
    public void Removed(string code, string message)
    {
        if (Permissive) Warning(code, message);
        else Error(code, message);
    }

    /// <summary>⛔ THE ONE SEAM for a DECLINED OPTIONAL/PROCESSOR-DEPENDENT ELEMENT — every Annex A.3/A.4
    /// facility this implementation does not claim is named through here, at bind, once per occurrence.
    /// <para>The DESCRIPTOR's <see cref="DiagnosticDescriptor.Severity"/> chooses the disposition, so the
    /// policy lives in the catalogue (where it is documented and drift-tested) and never in a local test at
    /// the site: <see cref="EditionSeverity.Warning"/> ⇒ ACCEPT-INERT (the facility is additive — the program
    /// compiles, runs, and the facility does nothing: COBOLNET1560/1578/1579/1580, satisfying §4.2.6 ¶3's
    /// mandatory compile-time warning mechanism); <see cref="EditionSeverity.Error"/> ⇒ REFUSE (an inert
    /// compile would change the ANSWER, so Annex A.4.1's "shall accept the syntax … only when support … is
    /// claimed" applies: COBOLNET1560/1705/1706/1707).</para>
    /// <para>⛔ THE BAND SPLITS ON THE LICENCE, NOT ON TASTE, and that is why there is ONE method and not two:
    /// an Annex A.3 processor-dependent facility is accepted-and-warned under §4.2.6, while an Annex A.4
    /// optional module that is Not claimed is refused. Both are "name the facility, then bind to a no-op or
    /// refuse"; only the severity differs, so the severity lives in the descriptor and every declined site —
    /// A.3 and A.4, data surface and statement surface — calls THIS, never a local <c>if (severity == …)</c>
    /// and never a second Report/Declined twin (feedback_one_mechanism_per_job).</para>
    /// <para>⛔ NOT routed through <see cref="Removed"/>: that is the strict/permissive migration seam for
    /// constructs an edition REMOVED, and a declined optional element has no pre-removal semantics to
    /// preserve. <c>--permissive</c> therefore does not move an A.3/A.4 decline in either direction — which is
    /// also, measurably, how the accept-inert rows have always behaved.</para>
    /// <para><paramref name="seen"/> is the site's naming of WHICH element of the module was written (the
    /// module owns the code, the site owns the spelling); omit it and the descriptor's own
    /// <see cref="DiagnosticDescriptor.Title"/> is the whole message — the byte-identical shape the
    /// accept-inert sites emitted before this seam existed.</para></summary>
    public void Declined(DiagnosticDescriptor descriptor, string? seen = null)
    {
        string message = seen is null ? descriptor.Title : $"{seen}: {descriptor.Title}";
        if (descriptor.Severity == EditionSeverity.Error) Error(descriptor.Code, message);
        else Warning(descriptor.Code, message);
    }

    /// <summary>The <see cref="IDiagnosticSink"/> bridge: render a structured <see cref="EditionDiagnostic"/>
    /// back onto the legacy string channels with byte-identical text — an <see cref="EditionSeverity.Error"/>
    /// goes to <see cref="Error"/> (fails the compile), a <see cref="EditionSeverity.Warning"/> to
    /// <see cref="Warning"/>. This is how the layer-neutral <see cref="Editions.ConstructRegistry.Check"/>
    /// produces exactly the diagnostics the old direct <c>edition.Error/Removed/Warning</c> calls did.</summary>
    public void Report(in EditionDiagnostic d)
    {
        if (d.Severity == EditionSeverity.Error) Error(d.Code, d.Message);
        else Warning(d.Code, d.Message);
    }

    /// <summary>Check a fixed-point digit-position count against the edition cap (ISO §8.3.1.2 / §13.18.40):
    /// 19–31 digits require COBOL-2002+; more than 31 is invalid at every edition.</summary>
    public void CheckDigitCapacity(int digits, string what)
    {
        if (digits <= MaxDigits) return;
        if (digits > 31)
            Error("COBOLNET0801", $"{what} has {digits} digit positions; ISO/IEC 1989 limits fixed-point items "
                + "and literals to 31 digits (ISO §8.3.1.2)");
        else
            Error("COBOLNET0802", $"{what} has {digits} digit positions; COBOL-85 limits fixed-point items and "
                + $"literals to 18 digits — 19–31 digits require --std 2002 or later (targeting COBOL-{DialectLevel})");
    }
}
