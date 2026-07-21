// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The exception-checking PERFORM bound nodes (ISO §14.9.28 Format 3, COBOL-2023). These records STAY in the
// CobolNet.Binding.Bound namespace so the source-generated exhaustive visitor + StatementChildren key on them:
// BoundExceptionPerform is a BoundStatement LEAF (forces a Visit arm on every IBoundStatementVisitor); the helper
// records BoundExceptionWhen / BoundWhenOperand are NOT BoundStatement, so BoundExceptionWhen surfaces as a
// "statement-bearing" helper whose Body statements StatementChildren walks (the BoundEvaluateWhen precedent).

/// <summary><c>PERFORM [WITH LOCATION] imp-1 {WHEN …}… [WHEN OTHER …] [WHEN COMMON …] [FINALLY …] END-PERFORM</c>
/// (ISO §14.9.28 Format 3): a per-statement exception interceptor scoped to imperative-statement-1
/// (<paramref name="Imp1"/>) — NOT a block try/catch. A raised, enabled exception condition detected during imp-1
/// is dispatched (GR17) to the first written-order matching <paramref name="Whens"/> handler; an unmatched enabled
/// condition runs <paramref name="OtherBody"/> (WHEN OTHER, GR18); after a handler, <paramref name="CommonBody"/>
/// (WHEN COMMON, GR19) runs; <paramref name="FinallyBody"/> (FINALLY, GR16) is the end of the PERFORM. Nonfatal →
/// resume in place after the raising statement (GR20); fatal → §14.6.13.1.3. <paramref name="WithLocation"/> carries
/// the LOCATION phrase into the implicit imp-1 TURN (GR14).</summary>
public sealed record BoundExceptionPerform(
    IReadOnlyList<BoundStatement> Imp1,
    IReadOnlyList<BoundExceptionWhen> Whens,
    IReadOnlyList<BoundStatement>? OtherBody,
    IReadOnlyList<BoundStatement>? CommonBody,
    IReadOnlyList<BoundStatement>? FinallyBody,
    bool WithLocation) : BoundStatement;

/// <summary>One ordinary WHEN phrase of a Format-3 PERFORM (imperative-statement-2, <paramref name="Body"/>). Its
/// selector is exactly ONE of: an open-<paramref name="OpenMode"/> (WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND — a
/// STAGED runtime match, §5.4-1); or an <paramref name="Operands"/> list — each operand is an exception-name (with
/// an optional paired FILE, §14.9.28.3 SR16), or a bare file-name (WHEN EXCEPTION file-name-1…, matching any
/// EC-I-O associated with that file). Match uses the USE GR3a–3g hierarchy (level-1 EC-ALL / level-2 parent /
/// level-3 self), first match wins.</summary>
public sealed record BoundExceptionWhen(
    string? OpenMode,
    IReadOnlyList<BoundWhenOperand> Operands,
    IReadOnlyList<BoundStatement> Body);

/// <summary>One WHEN operand: an <paramref name="Ec"/> exception-name (level 1/2/3), an optional paired
/// <paramref name="File"/> (SR16 requires an EC-I-O name when FILE is given), or — when <paramref name="Ec"/> is
/// null — a bare file-name (the WHEN EXCEPTION file-name-1… form: match any EC-I-O associated with the file).</summary>
public sealed record BoundWhenOperand(string? Ec, FileModel? File);
