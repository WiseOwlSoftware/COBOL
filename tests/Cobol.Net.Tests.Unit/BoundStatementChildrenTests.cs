// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using CobolNet.Binding.Bound;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Guards the generated <c>BoundStatementTree.StatementChildren</c> (Cobol.Net.Compiler.SourceGen; PHASE-07 Step 6g) —
/// the ONE drift-proof source of the statement-tree shape that the walker consumers will recurse over. Completeness is
/// by construction (the generator reads every property via the semantic model), so these tests lock the two things a
/// generation bug could still break: the traversal MECHANISM (direct child statements come back, and exactly one level
/// deep — not transitively), and NULL-SAFETY of every generated arm (a partially-populated node must never throw).
/// </summary>
public sealed class BoundStatementChildrenTests
{
    [Fact]
    public void DirectChildren_AreReturned()
    {
        var a = new BoundStop();
        var b = new BoundNop();
        var seq = new BoundSequence([a, b]);
        Assert.Equal(new BoundStatement[] { a, b }, seq.StatementChildren());
    }

    [Fact]
    public void Recursion_IsOneLevelOnly_NotTransitive()
    {
        var leaf = new BoundStop();
        var inner = new BoundSequence([leaf]);
        var outer = new BoundSequence([inner]);
        // The direct child is the inner sequence itself — the deep leaf is reached only by a walker that recurses.
        Assert.Equal(new BoundStatement[] { inner }, outer.StatementChildren());
    }

    [Fact]
    public void LeafWithNoNestedStatements_IsEmpty()
    {
        Assert.Empty(new BoundStop().StatementChildren());
    }

    /// <summary>Every one of the ~79 generated arms is null-safe: called on an uninitialized node (all reference
    /// props null), StatementChildren must return an empty sequence without throwing — this exercises each arm's
    /// <c>Nz</c>/<c>One</c>/<c>?.</c>/<c>?? Empty</c> guards. A generator that emitted an unguarded child access
    /// (a missing <c>Nz</c>, a bare <c>?.</c> gap) would NRE here.</summary>
    [Fact]
    public void EveryArm_IsNullSafe_OnAnUninitializedNode()
    {
        var leaves = typeof(BoundStatement).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(BoundStatement).IsAssignableFrom(t))
            .ToList();
        Assert.True(leaves.Count >= 70, $"expected the full BoundStatement leaf set, found {leaves.Count}");

        foreach (var t in leaves)
        {
            var node = (BoundStatement)RuntimeHelpers.GetUninitializedObject(t);
            var children = node.StatementChildren().ToList();   // force enumeration (SelectMany is lazy)
            Assert.NotNull(children);
        }
    }
}
