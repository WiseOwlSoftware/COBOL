// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The INSPECT bound nodes (P7 Step 10c: the binder half moved to Binding/Procedure/Verbs/InspectBinder.cs;
// these records/enums STAY here — the source-generated visitor and the emitter key on this namespace, and
// the Tally/Replace enum ordinals are runtime ABI).

/// <summary>An INSPECT TALLYING operand-kind (ISO §14.9.22.2 Format 1: ALL / LEADING / CHARACTERS). The ordinals
/// match the runtime <c>CobolInspect.Tally*</c> constants — the emitter passes them straight through.</summary>
public enum InspectTallyKind { All = 0, Leading = 1, Characters = 2 }

/// <summary>An INSPECT REPLACING operand-kind (ISO §14.9.22.2 Format 2: ALL / FIRST / LEADING / CHARACTERS).
/// Ordinals match the runtime <c>CobolInspect.Replace*</c> constants.</summary>
public enum InspectReplaceKind { All = 0, First = 1, Leading = 2, Characters = 3 }

/// <summary>One flattened TALLYING operand: its counter (identifier-2 — counts ADD into it, §14.9.22.4 GR11),
/// kind, pattern (null for CHARACTERS, whose implied 1-character operand always matches — GR8e), and per-operand
/// BEFORE/AFTER delimiters (GR9). The flattening order across ALL counters of the statement IS the GR8a shared
/// comparison-cycle order.</summary>
public sealed record BoundInspectTally(
    Place Counter, InspectTallyKind Kind, BoundOperand? Pattern, BoundOperand? Before, BoundOperand? After);

/// <summary>One flattened REPLACING operand: kind, pattern (null for CHARACTERS), equal-length replacement
/// (§14.9.22.4 GR14 — a figurative replacement was already expanded to the pattern size at bind time, SR6), and
/// per-operand BEFORE/AFTER delimiters (GR9). Source order = the GR8a shared-cycle order.</summary>
public sealed record BoundInspectReplace(
    InspectReplaceKind Kind, BoundOperand? Pattern, BoundOperand Replacement, BoundOperand? Before, BoundOperand? After);

/// <summary>The CONVERTING phrase (ISO §14.9.22.2 Format 4): the positional from→to character maps (GR20) and the
/// single BEFORE/AFTER region. A figurative <paramref name="To"/> was expanded to <paramref name="From"/>'s size
/// at bind time (SR9/GR22).</summary>
public sealed record BoundInspectConvert(BoundOperand From, BoundOperand To, BoundOperand? Before, BoundOperand? After);

/// <summary>INSPECT (ISO §14.9.22). Formats 1–3 carry the flattened tallying/replacing operand lists; format 4
/// carries <see cref="Converting"/>. A format 3 executes as two successive statements — tallying then replacing —
/// over the same identifier-1 (GR19). <see cref="Backward"/> reverses the scan direction (2023-only, gated at
/// bind time).</summary>
public sealed record BoundInspect(
    Place Target,
    IReadOnlyList<BoundInspectTally> Tallying,
    IReadOnlyList<BoundInspectReplace> Replacing,
    BoundInspectConvert? Converting,
    bool Backward) : BoundStatement;
