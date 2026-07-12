// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Bound;

// The EVALUATE bound nodes (P7 Step 10d: the binder half moved to Binding/Procedure/Verbs/EvaluateBinder.cs;
// these records STAY here — the source-generated visitor and StatementChildren key on this namespace).

/// <summary><c>EVALUATE</c> (ISO §14.9.13), bound at COMPILE time to a chained selection (COBOLNET_DESIGN §5.3):
/// each WHEN's match is ONE <see cref="BoundCondition"/> — the AND over its subject↔object pairs, with
/// consecutive WHEN phrases OR-ed over a shared body (§14.9.13 GR — multiple WHEN phrases preceding one
/// imperative). The first true arm's statements run; WHEN OTHER is the else tail.</summary>
public sealed record BoundEvaluate(
    IReadOnlyList<BoundEvaluateWhen> Whens, IReadOnlyList<BoundStatement>? Other) : BoundStatement;

/// <summary>One selectable EVALUATE arm: its composed match condition and its statements.</summary>
public sealed record BoundEvaluateWhen(BoundCondition Match, IReadOnlyList<BoundStatement> Statements);
