// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>⛔ THE ONE WALK OVER EVERY OPERAND POSITION OF A VALUE TREE (kb/Work PB6223, PB6328). A consumer that asks
/// "does any operand under this part carry X" (EC-ARGUMENT-FUNCTION's intrinsic call, EC-PROGRAM-NOT-FOUND's program
/// address) enumerates <see cref="SelfAndDescendants"/> and tests each node; it keeps no hand-written switch over the
/// condition, expression, operand and boolean hierarchies, whose `_ => false` default made a class condition, a
/// range membership, a user-function window and a CANCEL target silent. Completeness is by construction:
/// <see cref="BoundStatementTree.ValueChildren"/> is generated from every property of every value leaf (the walk that
/// builds <see cref="BoundStatementTree.OwnValueParts"/>), so a new node, or a new operand slot on an old one, is walked
/// by existing. <c>BoundValueWalkDriftTests</c> fails when a value-holding shape is a kind the generator does not read.</summary>
public static class BoundValueWalk
{
    /// <summary><paramref name="part"/> and every value node (<see cref="BoundExpr"/>, <see cref="BoundCondition"/>,
    /// <see cref="BoundOperand"/>, <see cref="BoundBoolExpr"/>) nested under it, parents before children. Statements
    /// nested in a user-function activation list are not entered: each is an EC region of its own (kb/Work PB441).</summary>
    public static IEnumerable<object> SelfAndDescendants(object part)
    {
        var pending = new Stack<object>();
        pending.Push(part);
        while (pending.Count > 0)
        {
            object node = pending.Pop();
            yield return node;
            foreach (object child in BoundStatementTree.ValueChildren(node))
                pending.Push(child);
        }
    }
}
