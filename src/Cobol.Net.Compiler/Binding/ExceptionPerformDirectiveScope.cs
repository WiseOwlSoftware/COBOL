// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Procedure;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The implicit PUSH ALL / POP ALL of ISO/IEC 1989:2023 §14.9.28.4 GR14 as ordinary directive-stack ops (kb/Work
/// PB1004): "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1.
/// Immediately preceding the END PERFORM phrase, there is an implicit POP ALL …".
///
/// <para>⛔ ONE MECHANISM. The two ops go through the SAME <see cref="DirectiveStateStack"/> as a written
/// <c>&gt;&gt;PUSH ALL</c> / <c>&gt;&gt;POP ALL</c> (§7.3.22.4 GR2 / §7.3.20.4 GR3), so EVERY directive state the
/// stack carries — not only TURN — is saved at the end of imperative-statement-1 and restored before END-PERFORM:
/// a <c>&gt;&gt;REF-MOD-ZERO-LENGTH ON</c> or <c>&gt;&gt;FLAG-14 … ON</c> written in a WHEN or FINALLY phrase no
/// longer outlives the statement. Only the parse tree can place them, so they are collected here, before binding,
/// and merged into every <see cref="DirectiveTimeline{T}"/> by <see cref="DirectiveResults.WithStackOps"/>. The
/// "TURN OFF ALL" half of the same sentence is <see cref="TurnState.WithAllDisabledFrom"/>, and the closing
/// implicit TURN OFF is the GR14 enable overlay being dropped after imperative-statement-1.</para>
///
/// <para>Placement, in the resultant-line frame every directive event uses: the PUSH at the LAST line of
/// imperative-statement-1 (every directive written after it — a directive occupies its own line — is pushed
/// over), the POP at the END-PERFORM line (every construct from there on sees the restored state). Ops are
/// returned in TOKEN order, so on a shared line an inner PERFORM's POP precedes an outer one's PUSH or POP.</para>
/// </summary>
internal static class ExceptionPerformDirectiveScope
{
    /// <summary>Every exception-checking PERFORM's implicit PUSH ALL / POP ALL in <paramref name="tree"/>, in
    /// source order.</summary>
    public static IReadOnlyList<DirectiveStackOp> ImplicitOps(Antlr4.Runtime.Tree.IParseTree tree)
    {
        var collector = new Collector();
        collector.Visit(tree);
        collector.Ops.Sort((a, b) => a.Token.CompareTo(b.Token));
        return [.. collector.Ops.Select(o => o.Op)];
    }

    private sealed class Collector : CobolParserCoreBaseVisitor<object?>
    {
        public List<(int Token, DirectiveStackOp Op)> Ops { get; } = [];

        public override object? VisitPerformStatement(Core.PerformStatementContext p)
        {
            // The ONE Format-3 discriminator the binder and the edition gate share (ControlFlowBinder.IsFormat3);
            // only the inline arm carries statementBlock + END-PERFORM.
            if (p.procedureName().Length == 0 && p.statementBlock() is { Stop: { } imp1End }
                && p.END_PERFORM() is { } end && ControlFlowBinder.IsFormat3(p))
            {
                Ops.Add((imp1End.TokenIndex, new DirectiveStackOp(imp1End.Line, DirectiveStackKind.Push, null)));
                Ops.Add((end.Symbol.TokenIndex, new DirectiveStackOp(end.Symbol.Line, DirectiveStackKind.Pop, null)));
            }
            return VisitChildren(p);
        }
    }
}
