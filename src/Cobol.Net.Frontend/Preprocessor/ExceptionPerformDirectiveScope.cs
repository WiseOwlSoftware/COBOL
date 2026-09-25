// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Frontend.Preprocessor;

using Core = CobolParserCore;

/// <summary>
/// The implicit PUSH ALL / POP ALL of ISO/IEC 1989:2023 §14.9.28.4 GR14 as ordinary directive-stack ops (kb/Work
/// PB1004, PB1066): "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1.
/// Immediately preceding the END PERFORM phrase, there is an implicit POP ALL …".
///
/// <para>⛔ ONE MECHANISM. The two ops go through the SAME <see cref="DirectiveStateStack"/> as a written
/// <c>&gt;&gt;PUSH ALL</c> / <c>&gt;&gt;POP ALL</c> (§7.3.22.4 GR2 / §7.3.20.4 GR3), so EVERY directive state the
/// stack carries is saved at the end of imperative-statement-1 and restored before END-PERFORM — in BOTH places
/// that state lives: the post-COPY line-scoped timelines (TURN, REF-MOD-ZERO-LENGTH, FLAG-02/14 bound options —
/// <see cref="DirectiveResults.WithStackOps"/>), and the conditional-compilation driver's compilation-variable table
/// and frontend-inline FLAG options (<see cref="ConditionalCompilationProcessor"/>'s implicit-op program, keyed to
/// its directive encounters by <see cref="ConditionalCompilationResult.KeyImplicitOps"/>). Only the parse tree can
/// place the ops, so the front end collects them after the parse and — when a DEFINE or FLAG written inside a
/// bracket would change what a later directive sees — re-runs the text manipulation with them (kb/Work PB1066;
/// <c>Frontend.Parse</c>). The "TURN OFF ALL" half of the same sentence is the binder's
/// <c>TurnState.WithAllDisabledFrom</c>.</para>
///
/// <para>Placement, in the resultant-line frame every directive event uses: the PUSH at the LAST line of
/// imperative-statement-1 (every directive written after it — a directive occupies its own line — is pushed
/// over), the POP at the END-PERFORM line (every construct from there on sees the restored state). Ops are
/// returned in TOKEN order, so on a shared line an inner PERFORM's POP precedes an outer one's PUSH or POP.</para>
/// </summary>
public static class ExceptionPerformDirectiveScope
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
            // The ONE Format-3 discriminator the binder and the edition gate share (PerformFormat.IsFormat3);
            // only the inline arm carries statementBlock + END-PERFORM.
            if (p.procedureName().Length == 0 && p.statementBlock() is { Stop: { } imp1End }
                && p.END_PERFORM() is { } end && PerformFormat.IsFormat3(p))
            {
                Ops.Add((imp1End.TokenIndex, new DirectiveStackOp(imp1End.Line, DirectiveStackKind.Push, null)));
                Ops.Add((end.Symbol.TokenIndex, new DirectiveStackOp(end.Symbol.Line, DirectiveStackKind.Pop, null)));
            }
            return VisitChildren(p);
        }
    }
}
