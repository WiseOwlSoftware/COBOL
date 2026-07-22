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
        // lives in the post-bind VersionConformancePass (Step 14d), reading the PARSE tree (ctx.statusPhrase()).
        // The status VALUE → process-exit-code wiring is decoded here into BoundStop.Status (§14.9.42.4 GR5).
        // §8.8.3.3 GR3: a concatenation expression stands anywhere a literal of its class may — fold a
        // STOP literal-1 concat to the equivalent single literal before decoding (GetText on the whole
        // literal context would glue the operands and mis-decode).
        return stop.literal() is { } slit
            ? new BoundStopLiteral(slit.nonNumericLiteral()?.concatenationExpression() is { } ce
                ? ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating).Value
                : CobolLiteral.Decode(slit.GetText()))
            : new BoundStop(BindTerminationStatus(stop.statusPhrase()));
    }

    /// <summary>Decode a shared <c>statusPhrase</c> (<c>WITH? (ERROR|NORMAL) (STATUS (dataReference|literal)?)?</c>,
    /// ISO §14.9.42.2 / §14.9.18.2) into a <see cref="TerminationStatus"/>, or null when the phrase is absent. The
    /// ERROR/NORMAL keyword is mandatory when the phrase is present; the STATUS value operand is optional (§14.9.42.4
    /// GR5 / §14.9.18.4 GR10 — an integer literal or a display/national/integer data item, bound as a numeric
    /// expression). Shared by STOP RUN and GOBACK (the same grammar rule).</summary>
    internal TerminationStatus? BindTerminationStatus(Core.StatusPhraseContext? sp)
    {
        if (sp is null) return null;
        BoundExpr? value = sp.dataReference() is { } d ? host.Expr.BindExpr(d)
            : sp.literal() is { } l ? host.Expr.BindExpr(l)
            : null;
        return new TerminationStatus(sp.ERROR() is not null, value);
    }

    /// <summary>CONTINUE [AFTER arithmetic-expression-1 SECONDS] (ISO §14.9.9). Plain CONTINUE is a 1985-continuous
    /// no-op (<see cref="BoundNop"/>). The AFTER … SECONDS timed-pause phrase (COBOL-2023, introduction-gated on the
    /// phrase by the VersionConformancePass) binds to a <see cref="BoundContinueAfter"/>. Whether
    /// EC-CONTINUE-LESS-THAN-ZERO checking is enabled at this statement is captured from the TurnState NOW (a bound
    /// node carries no parse line), so the runtime raises the nonfatal exception (GR1b) only under CHECKING ON.</summary>
    public BoundStatement BindContinue(Core.ContinueStatementContext cont)
    {
        if (cont.arithmeticExpression() is not { } secs) return new BoundNop();   // plain CONTINUE — a §14.9.9 no-op
        bool checkLtz = ctx.EcState.Turn.Enabled("EC-CONTINUE-LESS-THAN-ZERO", null, cont.Start.Line);
        return new BoundContinueAfter(host.Expr.BindExpr(secs), checkLtz);
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
            return new BoundGoToDepending(host.Expr.FieldOperand(sel), targets, g.Start.Line);
        }
        if (names.Length == 0) return host.Alter.AlterBindBareGoTo(g);   // the 85-only target-less GO TO (ALTER subsystem)
        if (ctx.Table.ResolveProcedure(names[0]) is not { } target)
            return new BoundUnsupported($"GO TO unknown procedure '{names[0].GetText()}'{host.OoScopeHint}");
        return host.Alter.AlterGoTo(g, target.Start);   // alterable when the owning paragraph is an ALTER target, else plain GO TO
    }

    public BoundStatement BindExit(Core.ExitStatementContext e)
    {
        if (e.PARAGRAPH() is not null) return new BoundExitParagraph(e.Start.Line);
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
        {
            // Format 3 (exception-checking, §14.9.28.2 Format 3) — any WHEN phrase, or a [WITH] LOCATION head,
            // marks the inline PERFORM as exception-checking. Everything else is a Format-2 inline PERFORM.
            if (IsFormat3(p))
                return BindExceptionPerform(p);
            return new BoundInlinePerform(BindPerformControl(p), host.BindBlocks(p.statementBlock()));
        }

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

        return new BoundOutOfLinePerform(start, end, BindPerformControl(p), p.Start.Line);
    }

    /// <summary>An inline PERFORM is Format 3 (exception-checking) iff it carries any WHEN phrase (ordinary /
    /// OTHER / COMMON), a FINALLY phrase, or a [WITH] LOCATION head (§14.9.28.2 Format 3). The ONE discriminator —
    /// the binder (here) and the COBOLNET0900 introduction gate (<c>VersionConformancePass.VisitPerformStatement</c>)
    /// share it, so the 0899↔0900 hand-off cannot drift (DEVLOG-724-class hazard).</summary>
    internal static bool IsFormat3(Core.PerformStatementContext p) =>
        p.performWhenPhrase().Length > 0 || p.performWhenOther() is not null
        || p.performWhenCommon() is not null || p.performFinally() is not null
        || p.performInlineHead()?.performLocationPhrase() is not null;

    /// <summary>Bind a Format-3 (exception-checking) PERFORM (ISO §14.9.28 Format 3) — delegated to the EC binder,
    /// which owns the WHEN-operand resolution, the GR14 TurnState overlay, and the §14.9.28.3 syntax rules /
    /// cross-statement bans.</summary>
    private BoundStatement BindExceptionPerform(Core.PerformStatementContext p) => host.Ec.EcBindExceptionPerform(p);

    /// <summary>Bind the OPTIONAL control phrase (TIMES / UNTIL / VARYING) of a PERFORM. Per ISO §14.9.28 the phrase
    /// is independent of the THRU range (general format: <c>PERFORM proc-1 [THRU proc-2] [times|until|varying]</c>),
    /// but the grammar exposes it in two shapes: a direct child (<c>PERFORM proc TIMES</c>, alternatives without
    /// THRU) or wrapped in <c>performOptions</c> (the <c>PERFORM proc THRU proc [performOptions]</c> alternative and
    /// the inline <c>performOptions+</c> form). Resolving only the direct child dropped the count/condition on a THRU
    /// range, silently running the range once instead of N times (§14.9.28 GR9) — the NC106A/NC176A defect
    /// (DEVLOG 514). This one resolver handles every shape for both inline and out-of-line PERFORM.</summary>
    private BoundPerformControl BindPerformControl(Core.PerformStatementContext p)
    {
        // The optional control phrase appears in three tree shapes: a direct child (the out-of-line
        // `PERFORM proc TIMES` alternatives), the THRU form's `performOptions?`, or the inline head's
        // `performInlineHead performOptions+` (the Formats-2/3 merge moved the inline options under the head).
        var opt = p.performOptions() ?? p.performInlineHead()?.performOptions().FirstOrDefault();
        if ((p.performTimes() ?? opt?.performTimes()) is { } t) return new PerformTimes(CountOperand(t));
        if ((p.performUntil() ?? opt?.performUntil()) is { } u)
        {
            // UNTIL EXIT (§14.9.28.4 GR11, 2023): an infinite loop (a condition that never becomes true). The
            // grammar gives EXIT its own alternative, so SR8's "no TEST with EXIT" is structural; escape is the
            // programmer's job (inline: EXIT PERFORM; out-of-line: GOBACK/STOP). Introduction-gated in the pass.
            if (u.EXIT() is not null) return new PerformForever();
            // The UNTIL condition is evaluated per iteration (§14.9.28 GR6/GR13), so a user-function
            // reference inside it activates per evaluation — the drained-suffix wrapper, never the
            // once-per-statement hoist (§8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2).
            int udfMark = host.Udf.PendingCount;
            var cond = host.Cond.BindCondition(u.condition());
            return new PerformUntil(host.Udf.UdfAttachPerEvaluation(cond, udfMark), u.AFTER() is not null);
        }
        if ((p.performVarying() ?? opt?.performVarying()) is { } v) return BindVarying(v);
        return new PerformOnce();
    }

    /// <summary>Bind a VARYING phrase (ISO §14.9.28 Format 4) into its ordered induction levels — the VARYING
    /// level first, then each AFTER level left-to-right. TEST AFTER is the phrase's own <c>TEST AFTER</c> (the
    /// AFTER tokens of the after-levels live in their sub-contexts, not here).</summary>
    private BoundPerformControl BindVarying(Core.PerformVaryingContext v)
    {
        var levels = new List<VaryingLevel>();
        if (BindVaryingLevel(v.dataReference(), v.arithmeticExpression(), v.condition(), firstLevel: true) is not { } head)
            return Unsupported($"PERFORM VARYING induction variable '{v.dataReference().GetText()}'");
        levels.Add(head);
        foreach (var a in v.performVaryingAfter())
        {
            if (BindVaryingLevel(a.dataReference(), a.arithmeticExpression(), a.condition(), firstLevel: false) is not { } level)
                return Unsupported($"PERFORM VARYING AFTER induction variable '{a.dataReference().GetText()}'");
            levels.Add(level);
        }
        return new PerformVarying(levels, v.TEST() is not null && v.AFTER() is not null);
    }

    /// <summary>One induction level: the variable is a SET-style target (index-name or data item); the expression
    /// array is [FROM] or [FROM, BY] (BY omitted ⇒ augment 1, GR12). User-function evaluation cardinality per
    /// window (§8.4.3.2.4 GR1/GR6a): the UNTIL condition re-evaluates per iteration — its activations attach
    /// per-evaluation; a FIRST-level FROM evaluates exactly once at loop start (GR13a/GR13b init) — its
    /// activations stay statement-hoisted (exact); an AFTER-level FROM (re-evaluated on each outer augment,
    /// GR13e.2) and any BY (evaluated per augment, GR12) stage LOUD — the narrowed 1509 residue.</summary>
    private VaryingLevel? BindVaryingLevel(
        Core.DataReferenceContext dref, Core.ArithmeticExpressionContext[] exprs, Core.ConditionContext cond,
        bool firstLevel)
    {
        if (host.Set.SetTargetOf(dref) is not { } var) return null;
        int fromMark = host.Udf.PendingCount;
        BoundExpr from = host.Expr.BindExpr(exprs[0]);
        if (!firstLevel)
            host.Udf.UdfStagePerEvaluationResidue(fromMark,
                "a PERFORM VARYING AFTER level's FROM operand (re-evaluated per outer augment, §14.9.28 GR13e.2)");
        int byMark = host.Udf.PendingCount;
        BoundExpr by = exprs.Length > 1 ? host.Expr.BindExpr(exprs[1]) : new BoundNumLiteral("1");
        host.Udf.UdfStagePerEvaluationResidue(byMark,
            "a PERFORM VARYING BY operand (evaluated per augment, §14.9.28 GR12)");
        int untilMark = host.Udf.PendingCount;
        return new VaryingLevel(var, from, by,
            host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(cond), untilMark));
    }

    private static BoundPerformControl Unsupported(string feature) => new PerformTimes(new BoundOperandError(feature));

    private BoundOperand CountOperand(Core.PerformTimesContext t) =>
        t.integerLiteral() is { } lit ? new BoundNumericLiteral(lit.GetText())
        : t.dataReference() is { } d ? host.Expr.FieldOperand(d)
        : new BoundNumericLiteral("1");
}
