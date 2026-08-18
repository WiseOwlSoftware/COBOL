// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Validation;

/// <summary>
/// A parse-tree visitor whose <see cref="IDiagnosticSink.Cursor"/> FOLLOWS the walk (kb/Work PB82): each rule node
/// is positioned for the duration of its own visit and the enclosing node's position is restored on exit, so a
/// gate fired from any <c>VisitXxx</c> — before OR after it visits the children — names the construct it recognized.
/// The ONE mechanism for the parse-tree passes (the version-conformance ParseArm, the migration-flag pass); a new
/// tree-walking validator derives from this and inherits the positioning.
/// </summary>
internal abstract class CursorFollowingVisitor(IDiagnosticSink sink) : CobolParserCoreBaseVisitor<object?>
{
    /// <summary>The sink whose cursor follows the walk — the same sink the pass reports to.</summary>
    protected IDiagnosticSink Sink { get; } = sink;

    /// <summary>Visit the root with its own position set; every node below is positioned by <see cref="VisitChildren"/>.</summary>
    public object? VisitPositioned(ParserRuleContext root)
    {
        using var _ = Sink.At(root);
        return Visit(root);
    }

    public override object? VisitChildren(IRuleNode node)
    {
        object? result = DefaultResult;
        int n = node.ChildCount;
        for (int i = 0; i < n && ShouldVisitNextChild(node, result); i++)
        {
            var child = node.GetChild(i);
            if (child is ParserRuleContext prc)
            {
                using var _ = Sink.At(prc);
                result = AggregateResult(result, child.Accept(this));
            }
            else
                result = AggregateResult(result, child.Accept(this));
        }
        return result;
    }
}
