// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Bound;

// The EVALUATE bound nodes (P7 Step 10d: the binder half moved to Binding/Procedure/Verbs/EvaluateBinder.cs;
// these records STAY here — the source-generated visitor and StatementChildren key on this namespace).

/// <summary><c>EVALUATE</c> (ISO §2023:14.9.13), bound at COMPILE time to a chained selection (DESIGN §5.3):
/// each WHEN phrase's match is ONE <see cref="BoundCondition"/> — the AND over its subject↔object pairs — and
/// consecutive WHEN phrases over a shared body keep separate matches, OR-ed in source order at emission (§2023:14.9.13 GR — multiple
/// WHEN phrases preceding one imperative). The first true arm's statements run; WHEN OTHER is the else tail.</summary>
public sealed record BoundEvaluate(
    IReadOnlyList<BoundEvaluateWhen> Whens, IReadOnlyList<BoundStatement>? Other) : BoundStatement, IBoundEvaluatedPhrases
{
    /// <summary>Each WHEN PHRASE's match, flattened across the arms in source order (an arm holds one or more phrases over a shared body): a
    /// phrase is evaluated only when every earlier phrase was false, so each is its own moment for the USE FOR DEBUGGING planner.</summary>
    public IReadOnlyList<object> PhraseOperands => [.. Whens.SelectMany(w => w.Matches).Select(m => (object)m)];
}

/// <summary>One selectable EVALUATE arm: the match of each WHEN phrase written over its body (the arm is selected when ANY is true, tested in
/// source order, short-circuit) and its statements.</summary>
public sealed record BoundEvaluateWhen(IReadOnlyList<BoundCondition> Matches, IReadOnlyList<BoundStatement> Statements);
