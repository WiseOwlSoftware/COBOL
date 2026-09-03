// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions.Diagnostics;

/// <summary>
/// A first-class descriptor for one diagnostic (rearch PHASE 02, P2.10 — fixes P8: the compiler emitted codes
/// as bare string literals, with the <c>COBOLNET0899</c> catch-all spread across ~44 sites and codes reused
/// across unrelated rules, so nothing was documentable, suppressible, or drift-bound). A descriptor gives each
/// diagnostic a STABLE identity (<see cref="Id"/>) independent of its emitted <see cref="Code"/> number, a
/// documented ISO citation and severity, and a <see cref="SuppressKey"/> family — the raw material for
/// <c>docs/DIAGNOSTICS.md</c>, <c>--suppress</c>, and the drift test.
/// </summary>
/// <param name="Code">The emitted <c>COBOLNETnnnn</c> code. NOT unique across descriptors: several descriptors
/// deliberately share a code (the <c>COBOLNET0899</c> recognized-not-implemented family; the reused
/// <c>COBOLNET1533</c> strong-type rules) — the code is what the user sees, <see cref="Id"/> is the identity.</param>
/// <param name="Id">The STABLE, unique kebab-case slug — survives code renumbering; the addressable identity used
/// by <c>--suppress</c>, the version matrix, and snapshot review.</param>
/// <param name="Severity">Error | Warning. Uses <see cref="EditionSeverity"/> (the Editions-native two-value
/// severity) — every P2-catalogued diagnostic is one of those two. The eventual unification with the frontend's
/// three-value <c>CobolNet.Frontend.Diagnostics.DiagnosticSeverity</c> (which adds Info) is the P7 diagnostic
/// merge; deferred so P2 adds no third parallel severity type (feedback_one_mechanism_per_job).</param>
/// <param name="Title">A short human summary of what the diagnostic reports — the <c>docs/DIAGNOSTICS.md</c> row
/// text and the eventual <c>--explain</c> text. NOT the runtime message: the emitted message is composed at the
/// call site (many of these diagnostics interpolate item names / ISO §s that only the site has). Converting the
/// composed messages to descriptor-owned format strings is the broader P7 migration.</param>
/// <param name="IsoSection">The ISO/IEC 1989:2023 § (or roadmap) citation this diagnostic enforces.</param>
/// <param name="SuppressKey">The <c>--suppress</c> family key. <see langword="null"/> ⇒ falls back to
/// <see cref="Code"/> (so an edition-band code, unique per descriptor, is suppressed by its code; the shared
/// <c>COBOLNET0899</c> family sets this explicitly — e.g. <c>recognized-not-implemented</c> — so a developer can
/// mute all deferred-feature diagnostics as a group without also muting genuine validation errors on the same
/// code).</param>
public sealed record DiagnosticDescriptor(
    string Code,
    string Id,
    EditionSeverity Severity,
    string Title,
    string IsoSection,
    string? SuppressKey = null,
    bool PermissiveInert = false)
{
    /// <summary>The effective <c>--suppress</c> family key: <see cref="SuppressKey"/> when set, else
    /// <see cref="Code"/>.</summary>
    public string ResolvedSuppressKey => SuppressKey ?? Code;
}
