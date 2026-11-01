// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;

namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ THE ONE DEPTH-FIRST WALK OF A PARSE SUBTREE THE BINDER ASKS FOR BY NODE (kb/Work PB4302). A recursive
/// <c>yield</c> iterator (<c>foreach (var d in Walk(child)) yield return d;</c>) hands every node up through one
/// <c>MoveNext</c> per level above it and allocates one iterator per interior node: O(nodes x depth) calls and, over the
/// corpus, a fifth of the binder's allocation (<c>DataBinder.PtrDescendants</c> alone was 22.7%). This walk keeps its own
/// stack, so a node costs one push and one pop whatever its depth, and the tree under a node is not touched until the
/// walk reaches it.
/// <para>Document (pre-)order, as the source reads: a node before its children, children left to right. A walk that must
/// know where a subtree ENDS (<see cref="StorageClaimScan"/> records the span of operands under a region) keeps an explicit
/// stack of its own; one that only needs the nodes asks here.</para></summary>
internal static class ParseTreeWalk
{
    /// <summary>Every node under <paramref name="node"/>, <paramref name="node"/> itself excluded.</summary>
    public static IEnumerable<IParseTree> Descendants(IParseTree node)
    {
        var pending = new Stack<IParseTree>();
        PushChildren(pending, node);
        while (pending.Count > 0)
        {
            var next = pending.Pop();
            yield return next;
            PushChildren(pending, next);
        }
    }

    /// <summary><paramref name="node"/>, then every node under it.</summary>
    public static IEnumerable<IParseTree> SelfAndDescendants(IParseTree node)
    {
        yield return node;
        foreach (var d in Descendants(node)) yield return d;
    }

    private static void PushChildren(Stack<IParseTree> pending, IParseTree node)
    {
        for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
    }
}
