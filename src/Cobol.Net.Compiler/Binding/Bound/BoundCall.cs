// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding.Bound;

// The CALL / CANCEL / EXIT PROGRAM / GOBACK bound nodes (P7 Step 10j: the binder half moved to
// Binding/Procedure/Verbs/CallBinder.cs; these types STAY here — Udf cross-constructs
// BoundCallProgram, the VersionConformancePass gates read UsedOverflowSpelling/ReturningSource, and
// the source-generated visitor keys on this namespace).

// ── Bound nodes — CALL / CANCEL / EXIT PROGRAM / GOBACK (ISO §14.9.4 / §14.9.5 / §14.9.14 / §14.9.18;
//    COBOLNET_INTERPROGRAM_DESIGN D1–D4) ────────────────────────────────────────────────────────────────────────

/// <summary>One CALL USING argument: its resolved pass mode (the §14.9.4.4 GR5 transitivity already applied at
/// bind time), and either a resolved <see cref="Place"/> (a data-reference argument) or a bound
/// <see cref="Value"/> operand (a literal — inherently BY CONTENT — or a BY VALUE expression, §14.9.4.3 SR4).</summary>
public sealed record BoundCallArg(CobolPassMode Mode, Place? Place, BoundOperand? Value);

/// <summary><c>CALL {literal|identifier} [USING …] [RETURNING …] [ON …][NOT ON …]</c> (ISO §14.9.4 Format 1).
/// <paramref name="LiteralName"/> is the static target (SR2 — a non-zero-length alphanumeric literal);
/// <paramref name="DynamicName"/> the runtime-resolved identifier target (GR3b). The exception phrases carry
/// the bound imperatives; the OVERFLOW-vs-EXCEPTION spelling is edition-gated at bind time and semantically
/// identical here (at 85 the only exception condition IS the resolution failure the OVERFLOW phrase catches).</summary>
public sealed record BoundCallProgram(
    string? LiteralName,
    BoundOperand? DynamicName,
    IReadOnlyList<BoundCallArg> Args,
    Place? Returning,
    IReadOnlyList<BoundStatement>? OnException,
    IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement
{
    /// <summary>True when this node is the lowering of a user-defined FUNCTION reference (M2-UDF): a locate
    /// miss stamps EC-FUNCTION-NOT-FOUND (Fatal, ISO §8.4.3.2.4 GR6b / Table 13) rather than the CALL's
    /// EC-PROGRAM-NOT-FOUND. Runtime dispatch is otherwise identical (the shared activation ABI).</summary>
    public bool IsFunction { get; init; }

    /// <summary>True when this CALL was written with the archaic ON OVERFLOW spelling (in either the ON or the NOT
    /// ON phrase) — the COBOL-74-carried synonym for ON EXCEPTION, REMOVED at ISO 2023 (Annex E.2 item 1c). The
    /// edition gate reads this in <see cref="Validation.VersionConformancePass"/> (rearch PHASE-03 Step 14d); the
    /// bound handlers are otherwise identical to the ON EXCEPTION form.</summary>
    public bool UsedOverflowSpelling { get; init; }

    /// <summary>True when <see cref="DynamicName"/> is a PROGRAM-POINTER operand (ISO §14.9.4 SR1 :26082 —
    /// identifier-1 may reference a program-pointer data item; GR :26177 — the item "contains the location of
    /// the program being called"): the emitter activates through <c>ProgramRegistry.CallPointer</c> (the held
    /// ProgramPointer) instead of the name-string read (P10 Step 7).</summary>
    public bool IsPointerTarget { get; init; }
}

/// <summary><c>CANCEL {literal|identifier}…</c> (ISO §14.9.5): each target's next CALL finds its initial state
/// (GR3); contained programs cascade in reverse source order (GR4); open files close implicitly (GR9).</summary>
public sealed record BoundCancel(
    IReadOnlyList<(string? LiteralName, BoundOperand? DynamicName)> Targets) : BoundStatement;

/// <summary><c>EXIT PROGRAM [RAISING …]</c> (ISO §14.9.14 Format 2): in a program NOT under the control of a
/// calling runtime element it is equivalent to CONTINUE (GR2 — "no exception condition is raised even if the
/// RAISING phrase is specified"); in a called program it returns to the activator per the GOBACK rules (GR3),
/// staging <paramref name="Raising"/> for re-raise in the activator. The distinction is a RUNTIME property of
/// the activation, so the bound node is unconditional and the emitted code tests the activation flag.
/// (Archaic at 2023 — Annex F.1; flagged, not rejected.)</summary>
public sealed record BoundExitProgram(BoundRaising? Raising = null) : BoundStatement;

/// <summary><c>GOBACK [RETURNING x] [RAISING …]</c> (ISO §14.9.18): terminates the executing program — return to
/// the caller in a called program (GR2), STOP-equivalent in a main program (GR3). <paramref name="ReturningSource"/>
/// moves into the procedure-division RETURNING item before return (the activation result, GR2);
/// <paramref name="Raising"/> stages an exception condition for re-raise in the activator. COBOL-2002+.</summary>
public sealed record BoundGoback(Place? ReturningSource, BoundRaising? Raising = null) : BoundStatement;
