// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The control-flow verb binder (P7 Step 10l — the audit-extension class mirroring the emitter-side
/// <c>CodeGen/Verbs/ControlFlowEmitter</c>; a RECORDED deviation from the plan sketch's three-class split
/// [Perform/If/ControlFlow] — one class matches the emitter topology and the singular-pattern rule):
/// STOP RUN/literal, GO TO (plain / DEPENDING / the ALTER-subsystem delegations — host edges until 10n),
/// EXIT (paragraph/perform/program/section/method/function — the Oo/Udf/Ec dispatches ride host hooks until
/// 10r/10s), IF, and the PERFORM family (inline/out-of-line/TIMES/UNTIL/VARYING — the one control resolver
/// covering both grammar shapes, the NC106A/NC176A lesson). The condition/expression/blocks spine stays on
/// the host until 10o/10q/10t.</summary>
internal sealed class ControlFlowBinder(BinderContext ctx, StatementBinder host)
{
    public BoundStatement BindStop(Core.StopStatementContext stop)
    {
        // STOP RUN … WITH STATUS (§14.9.42) is a COBOL-2002 introduction; the edition gate (StopRunStatus2002)
        // moved to the post-bind VersionConformancePass (Step 14d), reading BoundStop.HasStatusPhrase.
        // §8.8.3.3 GR3: a concatenation expression stands anywhere a literal of its class may — fold a
        // STOP literal-1 concat to the equivalent single literal before decoding (GetText on the whole
        // literal context would glue the operands and mis-decode).
        return stop.literal() is { } slit
            ? new BoundStopLiteral(slit.nonNumericLiteral()?.concatenationExpression() is { } ce
                ? ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating).Value
                : CobolLiteral.Decode(slit.GetText()))
            : new BoundStop { HasStatusPhrase = stop.stopStatusPhrase() is not null };
    }

    public BoundStatement BindGoTo(Core.GoToStatementContext g)
    {
        var names = g.procedureName();
        if (g.dataReference() is { } sel && names.Length >= 1)   // GO TO p1 p2 … DEPENDING ON sel
        {
            var targets = new List<int>();
            foreach (var n in names)
            {
                // A section target transfers to its first paragraph (ISO §14.9.17 GR1).
                if (ctx.Table.ResolveProcedure(n) is not { } range) return new BoundUnsupported($"GO TO unknown procedure '{n.GetText()}'{host.OoScopeHint}");
                targets.Add(range.Start);
            }
            return new BoundGoToDepending(host.Expr.FieldOperand(sel), targets);
        }
        if (names.Length == 0) return host.Alter.AlterBindBareGoTo(g);   // the 85-only target-less GO TO (ALTER subsystem)
        if (ctx.Table.ResolveProcedure(names[0]) is not { } target)
            return new BoundUnsupported($"GO TO unknown procedure '{names[0].GetText()}'{host.OoScopeHint}");
        return host.Alter.AlterGoTo(g, target.Start);   // alterable when the owning paragraph is an ALTER target, else plain GO TO
    }

    public BoundStatement BindExit(Core.ExitStatementContext e)
    {
        if (e.PARAGRAPH() is not null) return new BoundExitParagraph();
        if (e.PERFORM() is not null) return new BoundExitPerform(e.CYCLE() is not null);
        if (e.PROGRAM() is not null)   // §14.9.14 GR2/GR3 — CONTINUE in a non-called program, return-to-caller in a called one (runtime-contextual)
        {
            if (host.InMethod)   // §14.9.14.3 SR7: EXIT PROGRAM only in a PROGRAM procedure division
            {
                ctx.Edition.Error("COBOLNET0827",
                    "EXIT PROGRAM may be specified only in a program procedure division, not in a method "
                    + "(ISO §14.9.14.3 SR7 — a method returns via GOBACK)");
                return new BoundNop();
            }
            if (e.raisingPhrase() is { } raising)   // Format 2's RAISING tail (§14.9.14.2) — re-raise in the activator
                return host.Ec.EcBindRaising(raising, e.Start.Line, "EXIT PROGRAM") is { } r
                    ? new BoundExitProgram(r)
                    : new BoundUnsupported("EXIT PROGRAM RAISING identifier (exception object — the OO wave; ISO §14.9.14.3)");
            return new BoundExitProgram();
        }
        if (e.SECTION() is not null) return new BoundUnsupported("EXIT SECTION");        // needs section bounds — later
        if (e.METHOD() is not null) return host.Oo.OoBindExitMethod(e);   // method-return synonym ≤2014; 0902 at 2023 (validator)
        if (e.FUNCTION() is not null) return host.Udf.UdfBindExitFunction(e);   // function-return synonym ≤2014; 0900/0902 window (validator)
        return new BoundNop();   // bare EXIT
    }


    public BoundStatement BindIf(Core.IfStatementContext iff)
    {
        var thenBlocks = new List<Core.StatementBlockContext>();
        var elseBlocks = new List<Core.StatementBlockContext>();
        bool seenElse = false;
        foreach (var child in StatementBinder.Children(iff))
        {
            if (child is ITerminalNode t && t.Symbol.Type == CobolLexer.ELSE) seenElse = true;
            else if (child is Core.StatementBlockContext sb) (seenElse ? elseBlocks : thenBlocks).Add(sb);
        }
        return new BoundIf(host.Cond.BindCondition(iff.condition()), host.BindBlocks(thenBlocks), host.BindBlocks(elseBlocks));
    }


    public BoundStatement BindPerform(Core.PerformStatementContext p)
    {
        var names = p.procedureName();
        if (names.Length == 0)
            return new BoundInlinePerform(BindPerformControl(p), host.BindBlocks(p.statementBlock()));

        // Out-of-line: the resolved pc range [start, end] — a paragraph (start==end), a SECTION (its whole
        // paragraph range, ISO §14.9.28 — first statement of its first paragraph through last of its last), or
        // the THRU composition (first procedure's start through the last procedure's end).
        if (ctx.Table.ResolveProcedure(names[0]) is not { } first)
            return new BoundUnsupported($"PERFORM unknown procedure '{names[0].GetText()}'{host.OoScopeHint}");
        (int start, int end) = first;
        if ((p.THRU() is not null || p.THROUGH() is not null) && names.Length >= 2)
        {
            if (ctx.Table.ResolveProcedure(names[1]) is not { } thru) return new BoundUnsupported($"PERFORM THRU unknown procedure '{names[1].GetText()}'{host.OoScopeHint}");
            // An INVERTED range (the THRU procedure physically precedes the first, reached by GO TO — NC102A
            // PFM-TEST-F1-10) is legal: the dispatcher returns when the exit procedure completes, wherever it is.
            end = thru.End;
        }
        else if (start > end)
            return new BoundNop();   // PERFORM of an EMPTY section runs nothing (no first statement, ISO §14.9.28)

        return new BoundOutOfLinePerform(start, end, BindPerformControl(p));
    }

    /// <summary>Bind the OPTIONAL control phrase (TIMES / UNTIL / VARYING) of a PERFORM. Per ISO §14.9.28 the phrase
    /// is independent of the THRU range (general format: <c>PERFORM proc-1 [THRU proc-2] [times|until|varying]</c>),
    /// but the grammar exposes it in two shapes: a direct child (<c>PERFORM proc TIMES</c>, alternatives without
    /// THRU) or wrapped in <c>performOptions</c> (the <c>PERFORM proc THRU proc [performOptions]</c> alternative and
    /// the inline <c>performOptions+</c> form). Resolving only the direct child dropped the count/condition on a THRU
    /// range, silently running the range once instead of N times (§14.9.28 GR9) — the NC106A/NC176A defect
    /// (DEVLOG 514). This one resolver handles every shape for both inline and out-of-line PERFORM.</summary>
    private BoundPerformControl BindPerformControl(Core.PerformStatementContext p)
    {
        var opt = p.performOptions().FirstOrDefault();
        if ((p.performTimes() ?? opt?.performTimes()) is { } t) return new PerformTimes(CountOperand(t));
        if ((p.performUntil() ?? opt?.performUntil()) is { } u) return new PerformUntil(host.Cond.BindCondition(u.condition()), u.AFTER() is not null);
        if ((p.performVarying() ?? opt?.performVarying()) is { } v) return BindVarying(v);
        return new PerformOnce();
    }

    /// <summary>Bind a VARYING phrase (ISO §14.9.28 Format 4) into its ordered induction levels — the VARYING
    /// level first, then each AFTER level left-to-right. TEST AFTER is the phrase's own <c>TEST AFTER</c> (the
    /// AFTER tokens of the after-levels live in their sub-contexts, not here).</summary>
    private BoundPerformControl BindVarying(Core.PerformVaryingContext v)
    {
        var levels = new List<VaryingLevel>();
        if (BindVaryingLevel(v.dataReference(), v.arithmeticExpression(), v.condition()) is not { } head)
            return Unsupported($"PERFORM VARYING induction variable '{v.dataReference().GetText()}'");
        levels.Add(head);
        foreach (var a in v.performVaryingAfter())
        {
            if (BindVaryingLevel(a.dataReference(), a.arithmeticExpression(), a.condition()) is not { } level)
                return Unsupported($"PERFORM VARYING AFTER induction variable '{a.dataReference().GetText()}'");
            levels.Add(level);
        }
        return new PerformVarying(levels, v.TEST() is not null && v.AFTER() is not null);
    }

    /// <summary>One induction level: the variable is a SET-style target (index-name or data item); the expression
    /// array is [FROM] or [FROM, BY] (BY omitted ⇒ augment 1, GR12).</summary>
    private VaryingLevel? BindVaryingLevel(
        Core.DataReferenceContext dref, Core.ArithmeticExpressionContext[] exprs, Core.ConditionContext cond)
    {
        if (host.Set.SetTargetOf(dref) is not { } var) return null;
        BoundExpr from = host.Expr.BindExpr(exprs[0]);
        BoundExpr by = exprs.Length > 1 ? host.Expr.BindExpr(exprs[1]) : new BoundNumLiteral("1");
        return new VaryingLevel(var, from, by, host.Cond.BindCondition(cond));
    }

    private static BoundPerformControl Unsupported(string feature) => new PerformTimes(new BoundOperandError(feature));

    private BoundOperand CountOperand(Core.PerformTimesContext t) =>
        t.integerLiteral() is { } lit ? new BoundNumericLiteral(lit.GetText())
        : t.dataReference() is { } d ? host.Expr.FieldOperand(d)
        : new BoundNumericLiteral("1");
}
