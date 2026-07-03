// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Validation;

/// <summary>
/// The per-edition VALIDATION pass (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.2) — the
/// syntax-side half of the four-compilers-in-one obligation: every construct carries (1) its full ISO behavior in
/// every edition that HAS it and (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced,
/// COBOLNET0900), reserves its spelling (0901), removed it (0902), or obsoleted it (0903 — see
/// <see cref="EditionCodes"/>). The validator walks the RAW parse tree — syntax-only gating lives here; gating
/// that needs bind/type information (e.g. the MOVE rows) stays binder-side — but EVERY severity decision routes
/// through <see cref="EditionContext.Removed"/> / the construct registry: one policy, several emit sites.
/// </summary>
/// <remarks>
/// The walk derives from the generated <see cref="CobolParserCoreBaseVisitor{Result}"/> (ANTLR runs
/// <c>-no-listener -visitor</c>, so no listener exists to attach to); overrides MUST return
/// <c>base.VisitChildren(ctx)</c> (or <c>base.VisitXxx(ctx)</c>) to keep descending. Hooked by
/// <see cref="CompilerDriver.Compile"/> between <see cref="EditionContext"/> construction and
/// <c>CSharpEmitter.Emit</c>, with a fail-fast on <see cref="EditionContext.HasErrors"/> BEFORE Emit — a
/// removed or not-yet-introduced construct may have no emit path at all. Validator diagnostics ride the SAME
/// <see cref="EditionContext"/> channels as binder gating (no separate outcome kind).
/// The Wave-1 construct gates (P2.6) and the §8.9 reserved-word funnel (P2.4 — <c>VisitCobolWord</c>) land on
/// this skeleton in their own change sets, each with its VERSION_CHANGE_REFERENCE row and ISO § citation.
/// </remarks>
public sealed class EditionValidator(EditionContext edition) : CobolParserCoreBaseVisitor<object?>
{
    private readonly EditionContext _edition = edition;

    /// <summary>Run the pass over a parsed compilation unit, recording diagnostics on the
    /// <see cref="EditionContext"/> passed at construction.</summary>
    public void Validate(CobolParserCore.CompilationUnitContext tree) => Visit(tree);
}
