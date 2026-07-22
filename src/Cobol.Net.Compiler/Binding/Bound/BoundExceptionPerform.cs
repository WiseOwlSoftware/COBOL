// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The exception-checking PERFORM bound nodes (ISO §14.9.28 Format 3, COBOL-2023). These records STAY in the
// CobolNet.Binding.Bound namespace so the source-generated exhaustive visitor + StatementChildren key on them:
// BoundExceptionPerform is a BoundStatement LEAF (forces a Visit arm on every IBoundStatementVisitor). The handler
// bodies imp-2/3/4 (WHEN / WHEN OTHER / WHEN COMMON) are NOT held here — they are appended synthetic pc-range
// paragraphs (referenced by pc), run via the reused __RunUse (the pc-RANGE interceptor, design §9); so this node's
// only statement-bearing children (walked by StatementChildren / UsageCollectionPass) are the INLINE parts, imp-1
// and FINALLY (imp-5). BoundExceptionMatch / BoundWhenOperand are NOT statement-bearing (pc + operand data only).

/// <summary><c>PERFORM [WITH LOCATION] imp-1 {WHEN …}… [WHEN OTHER …] [WHEN COMMON …] [FINALLY …] END-PERFORM</c>
/// (ISO §14.9.28 Format 3): a per-statement exception interceptor scoped to imperative-statement-1
/// (<paramref name="Imp1"/>, emitted INLINE) — NOT a block try/catch. A raised, enabled exception condition detected
/// during imp-1 is dispatched (GR17) to the tier-first matching <paramref name="Whens"/> handler (imp-2, at its
/// <c>Imp2Pc</c>); an unmatched enabled condition runs WHEN OTHER (imp-3, <paramref name="OtherPc"/>, GR18); after a
/// handler, WHEN COMMON (imp-4, <paramref name="CommonPc"/>, GR19) runs; <paramref name="FinallyBody"/> (FINALLY,
/// imp-5, GR16) is the INLINE end of the PERFORM. Nonfatal → resume in place after the raising statement (GR20);
/// fatal → §14.6.13.1.3. <paramref name="WithLocation"/> carried the LOCATION phrase into the implicit imp-1 TURN
/// (GR14, applied at bind). <paramref name="PerformId"/> disambiguates nested F3 PERFORMs (labels + the
/// <c>ExitPerformSignal</c> boundary); <paramref name="HandlerHasExit"/> gates the handler EXIT-PERFORM catch.</summary>
public sealed record BoundExceptionPerform(
    IReadOnlyList<BoundStatement> Imp1,
    IReadOnlyList<BoundExceptionMatch> Whens,
    int? OtherPc,
    int? CommonPc,
    IReadOnlyList<BoundStatement>? FinallyBody,
    bool WithLocation,
    int PerformId,
    bool HandlerHasExit) : BoundStatement;

/// <summary>One ordinary WHEN phrase of a Format-3 PERFORM — its selector plus the pc of its bound handler body
/// (imperative-statement-2, <paramref name="Imp2Pc"/>, a single appended synthetic paragraph). The selector is
/// exactly ONE of: an open-<paramref name="OpenMode"/> (WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND — a STAGED runtime
/// match, §5.4-1); or an <paramref name="Operands"/> list — each operand an exception-name (with an optional paired
/// FILE, §14.9.28.3 SR16), or a bare file-name (WHEN EXCEPTION file-name-1…, matching any EC-I-O associated with
/// that file). The emitter sorts operands into the §14.9.49.4 GR3c–g tiers (file+L3 → file+L2 → L3 → L2 → L1);
/// first TIER match wins (GR17), source order only within a tier.</summary>
public sealed record BoundExceptionMatch(
    string? OpenMode,
    IReadOnlyList<BoundWhenOperand> Operands,
    int Imp2Pc);

/// <summary>One WHEN operand: an <paramref name="Ec"/> exception-name (level 1/2/3), an optional paired
/// <paramref name="File"/> (SR16 requires an EC-I-O name when FILE is given), or — when <paramref name="Ec"/> is
/// null — a bare file-name (the WHEN EXCEPTION file-name-1… form: match any EC-I-O associated with the file).</summary>
public sealed record BoundWhenOperand(string? Ec, FileModel? File);
