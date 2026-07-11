// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Correctness guard for the <c>BoundVisitorGenerator</c> (Cobol.Net.Compiler.SourceGen; PHASE-07 Step 6). The
/// generator emits, per <c>[BoundNode]</c> root, an <c>I{Root}Visitor&lt;T&gt;</c> whose Visit overloads must cover
/// EXACTLY the non-abstract records deriving from that root. Because the generator runs every build off the live
/// type graph it cannot drift from a committed artifact — but this test proves its DISCOVERY stays correct: the
/// ground truth is the runtime type hierarchy, so a leaf the generator failed to enumerate (or a stale Visit for a
/// deleted leaf) fails here. It is also the tripwire for a new root added without <c>[BoundNode]</c>: that root's
/// leaves would be unhandled and its <c>Add</c> below would reference a non-existent interface (a compile error).
/// </summary>
public sealed class BoundVisitorGeneratorTests
{
    // (root, generated open-generic visitor interface) — one row per [BoundNode] root.
    private static readonly (Type Root, Type Visitor)[] Roots =
    {
        (typeof(BoundStatement),      typeof(IBoundStatementVisitor<>)),
        (typeof(BoundExpr),           typeof(IBoundExprVisitor<>)),
        (typeof(BoundCondition),      typeof(IBoundConditionVisitor<>)),
        (typeof(BoundOperand),        typeof(IBoundOperandVisitor<>)),
        (typeof(BoundBoolExpr),       typeof(IBoundBoolExprVisitor<>)),
        (typeof(BoundPerformControl), typeof(IBoundPerformControlVisitor<>)),
        (typeof(BoundSetTarget),      typeof(IBoundSetTargetVisitor<>)),
    };

    [Theory]
    [MemberData(nameof(RootIndices))]
    public void VisitorInterface_Covers_ExactlyTheCompiledLeaves(int rootIndex)
    {
        var (root, visitor) = Roots[rootIndex];

        var groundTruth = root.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t != root && root.IsAssignableFrom(t))
            .ToHashSet();

        var covered = visitor.GetMethods()
            .Where(m => m.Name == "Visit")
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToHashSet();

        var missing = groundTruth.Except(covered).Select(t => t.Name).OrderBy(n => n).ToList();
        var orphan = covered.Except(groundTruth).Select(t => t.Name).OrderBy(n => n).ToList();
        Assert.True(missing.Count == 0 && orphan.Count == 0,
            $"{root.Name} visitor coverage: leaves-with-no-Visit=[{string.Join(",", missing)}] "
            + $"Visit-with-no-leaf=[{string.Join(",", orphan)}] — the BoundVisitorGenerator discovery is stale/broken");
    }

    public static IEnumerable<object[]> RootIndices() => Enumerable.Range(0, Roots.Length).Select(i => new object[] { i });
}
