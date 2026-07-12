// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The control-flow verb emitter (P7 Step 9i — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): IF, inline/out-of-line PERFORM (TIMES/UNTIL/VARYING), serial SEARCH, and
/// GO TO … DEPENDING. The out-of-line PERFORM's bounded dispatch reads <see cref="DispatchState.DispatchName"/>;
/// VARYING/SEARCH index advances ride the ONE SET-target store pair on <see cref="SetEmitter"/>.</summary>
internal sealed class ControlFlowEmitter(EmitContext ctx, NumericRenderer num, ConditionRenderer cond,
    DispatchState dispatch, SetEmitter set)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (IF branches,
    /// inline-PERFORM bodies, and SEARCH arms nest arbitrary statement lists — a cyclic edge no ctor order
    /// can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>Emit <c>GO TO … DEPENDING ON sel</c> (ISO §14.9.20 Format 2): a 1-based selector picks a pc; an
    /// out-of-range value transfers nowhere and falls through to the next statement.</summary>
    public void EmitGoToDepending(BoundGoToDepending d)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextDep();
        w.Line($"int __dep{id} = (int)({num.AsNum(d.Selector, ReceiverContext.None).Expr});");
        using (w.Block($"switch (__dep{id})"))
            for (int k = 0; k < d.Targets.Count; k++)
                w.Line($"case {k + 1}: __pc = {d.Targets[k]}; break;");
        w.Line($"if (__dep{id} >= 1 && __dep{id} <= {d.Targets.Count}) break;   // in range → transfer (exit the dispatcher switch)");
    }

    // DISPLAY lives on AcceptDisplayEmitter since Step 9c (the ACCEPT/DISPLAY collaborator).


    public void EmitIf(BoundIf iff)
    {
        var w = ctx.Writer;
        using (w.Block($"if ({cond.Render(iff.Condition)})"))
            Statements.EmitStatementList(iff.Then);
        if (iff.Else.Count > 0)
            using (w.Block("else"))
                Statements.EmitStatementList(iff.Else);
    }

    public void EmitInlinePerform(BoundInlinePerform p) => EmitPerform(p.Control, () => Statements.EmitStatementList(p.Body), inline: true);

    /// <summary>An out-of-line PERFORM is a recursive bounded <c>Dispatch(start, end)</c> over the target pc range
    /// (the C# call stack is the return-address stack, COBOLNET_DESIGN §5.4).</summary>
    public void EmitOutOfLinePerform(BoundOutOfLinePerform p) =>
        EmitPerform(p.Control, () => ctx.Writer.Line($"{dispatch.DispatchName}({p.StartPc}, {p.EndPc});"), inline: false);


    private void EmitPerform(BoundPerformControl control, Action body, bool inline)
    {
        var w = ctx.Writer;
        switch (control)
        {
            case PerformTimes t:
                // The TIMES count is determined ONCE at the start of the PERFORM (ISO §14.9.28 GR7) — the body
                // modifying the count item must not change the iteration count (NC102A PFM-TEST-F2-6); a zero or
                // negative count runs the body zero times.
                int id = ctx.Names.NextLoop();
                w.Line($"long __n{id} = {CountExpr(t.Count)};");
                using (w.Block($"for (long __i{id} = 0; __i{id} < __n{id}; __i{id}++)")) body();
                break;
            case PerformUntil u when u.TestAfter:
                using (w.Block("do")) body();
                w.Line($"while (!({cond.Render(u.Until)}));");
                break;
            case PerformUntil u:
                using (w.Block($"while (!({cond.Render(u.Until)}))")) body();
                break;
            case PerformVarying v:
                EmitVarying(v, body);
                break;
            default:   // PerformOnce — an inline body runs once via do/while(false); an out-of-line call is unconditional
                if (inline) { using (w.Block("do")) body(); w.Line("while (false);"); }
                else body();
                break;
        }
    }

    /// <summary>PERFORM VARYING … [AFTER …] (ISO §14.9.28 GR13), leftmost level outermost.
    /// TEST BEFORE (GR13a/d/e): all induction variables initialize left-to-right ONCE; nested <c>while(!cond)</c>
    /// loops; the innermost loop runs the body then augments its variable; when an inner condition goes true, its
    /// variable RESETS to FROM and the variable to its LEFT augments (GR13e.2a–c) before the outer retest.
    /// TEST AFTER (GR13b/c): body-first loops — the innermost tests after the body (false → augment, repeat);
    /// when true the next level out tests (false → augment it, REINITIALIZE the inner variable, run again).
    /// FROM/BY render inline so each set/augment re-reads their current contents (GR12).</summary>
    private void EmitVarying(PerformVarying v, Action body)
    {
        var w = ctx.Writer;
        var levels = v.Levels;
        if (!v.TestAfter)
        {
            foreach (var lv in levels) set.StoreSetTarget(lv.Var, num.Render(lv.From, ReceiverContext.None));   // GR13a: left-to-right init
            EmitBefore(0);
            void EmitBefore(int k)
            {
                using (w.Block($"while (!({cond.Render(levels[k].Until)}))"))
                {
                    if (k == levels.Count - 1)
                    {
                        body();
                        set.AugmentSetTarget(levels[k].Var, down: false, num.Render(levels[k].By, ReceiverContext.None));
                    }
                    else
                    {
                        EmitBefore(k + 1);
                        // §14.9.28 GR13e ('85 6.20.4 GR10(d)1): the OUTER variable augments FIRST, THEN the inner
                        // re-initializes from its CURRENT FROM value — `AFTER B FROM A` must see the augmented A
                        // (NC201A PFM-TEST-F4-23: 3+2+1 = 6 iterations, not 3+3+2).
                        set.AugmentSetTarget(levels[k].Var, down: false, num.Render(levels[k].By, ReceiverContext.None));
                        set.StoreSetTarget(levels[k + 1].Var, num.Render(levels[k + 1].From, ReceiverContext.None));
                    }
                }
            }
        }
        else
        {
            set.StoreSetTarget(levels[0].Var, num.Render(levels[0].From, ReceiverContext.None));
            EmitAfter(0);
            void EmitAfter(int k)
            {
                using (w.Block("while (true)"))
                {
                    if (k == levels.Count - 1) body();
                    else
                    {
                        set.StoreSetTarget(levels[k + 1].Var, num.Render(levels[k + 1].From, ReceiverContext.None));   // reinit on each entry
                        EmitAfter(k + 1);
                    }
                    w.Line($"if ({cond.Render(levels[k].Until)}) break;");
                    set.AugmentSetTarget(levels[k].Var, down: false, num.Render(levels[k].By, ReceiverContext.None));
                }
            }
        }
    }

    private static string CountExpr(BoundOperand count) => count switch
    {
        BoundNumericLiteral n => n.Text,
        BoundFieldOperand f => PlaceRenderer.Read(f.Place),
        BoundOperandError e => LoudValue("long", e.Feature),
        _ => "1",
    };


    /// <summary>Serial SEARCH (ISO §14.9.37.4 GR5–8): scan from the index's CURRENT setting; each pass tests
    /// past-end (→ AT END) then the WHEN conditions in order (first true wins); none true → the index (and the
    /// in-step varied item, GR8) increments by 1. Emitted as a LABEL loop — not a C# while — so a GO TO inside a
    /// WHEN/AT END body (`__pc = k; break;`) breaks the DISPATCHER case, not a search loop (transfer-of-control
    /// out of SEARCH per GR5c/6b); a body that runs to completion jumps past the search.</summary>
    public void EmitSearch(BoundSearch s)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextSearch();
        if (s.FromStart) w.Line($"{s.IndexField} = 1;");   // SEARCH ALL ignores the initial setting (GR9)
        // An OCCURS DYNAMIC table brackets the scan with EnterSearch/ExitSearch so a SET Format 14 on that same
        // table WHILE searching raises EC-FLOW-SEARCH (ISO §14.9.39 GR31; data-model D9). A try/finally is required
        // because the WHEN/AT-END arms `goto __searchEnd` OUT of the scan — ExitSearch must run on every exit path.
        if (s.DynTable is { } dt)
        {
            w.Line($"{dt}.EnterSearch();");
            using (w.Block("try")) EmitSearchScan(s, id);
            w.Line($"finally {{ {dt}.ExitSearch(); }}");
        }
        else EmitSearchScan(s, id);
    }

    /// <summary>The serial-SEARCH scan loop (ISO §14.9.37.4): past-end → AT END; else each WHEN in order; none true →
    /// advance the index (and the VARYING item) and loop. The AT-END bound is the table's MAXIMUM occurrence count —
    /// or, for an occurs-depending table its CURRENT count, or (D9) a dynamic table's current <c>Capacity</c>
    /// (GR4/GR9 → §13.18.38 GR7/§8.5.1.9.1). Extracted so a dynamic table can wrap it in an EnterSearch/ExitSearch
    /// try/finally.</summary>
    private void EmitSearchScan(BoundSearch s, int id)
    {
        var w = ctx.Writer;
        w.Line($"__search{id}:");
        // The AT-END bound: a dynamic table's current Capacity, an occurs-depending table's current count
        // (CobolTable.Occ over data-name-1's place), else the compile-time maximum.
        string bound = s.DynTable is { } dt ? $"{dt}.Capacity"
            : s.DependItem is { } dp ? RuntimeApi.TableOcc(PlaceRenderer.Read(dp))
            : $"{s.Count}L";
        using (w.Block($"if ({s.IndexField} > {bound})"))
        {
            bool terminated = s.AtEnd is { } at && Statements.EmitStatementList(at);
            if (!terminated) w.Line($"goto __searchEnd{id};");
        }
        foreach (var when in s.Whens)
            using (w.Block($"if ({cond.Render(when.Condition)})"))
            {
                if (!Statements.EmitStatementList(when.Statements)) w.Line($"goto __searchEnd{id};");
            }
        w.Line($"{s.IndexField} += 1;");
        if (s.AlsoVaried is { } also) set.AugmentSetTarget(also, down: false, new NumX("1", 0));
        w.Line($"goto __search{id};");
        w.Line($"__searchEnd{id}: ;");
    }

}
