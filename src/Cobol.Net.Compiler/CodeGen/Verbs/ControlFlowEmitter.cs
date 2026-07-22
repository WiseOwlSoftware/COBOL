// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;

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
        // X3.23-1985 USE FOR DEBUGGING (VCR 7.17): an in-range GO TO … DEPENDING transfer is DEBUG-CONTENTS SPACES,
        // DEBUG-LINE the GO TO DEPENDING statement's own line.
        string cause = dispatch.DebugActive ? $" __dbgCause = DebugCause.Transfer; __dbgLine = {d.SourceLine};" : "";
        using (w.Block($"switch (__dep{id})"))
            for (int k = 0; k < d.Targets.Count; k++)
                w.Line($"case {k + 1}:{cause} __pc = {d.Targets[k]}; break;");
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

    // A nested inline PERFORM within an F3 region resets F3Cur to None around its body so a plain EXIT PERFORM
    // inside it breaks the INNER loop (§14.9.14.4 GR5a — "the most closely preceding, unterminated inline PERFORM"),
    // not the enclosing F3 PERFORM.
    public void EmitInlinePerform(BoundInlinePerform p) => EmitPerform(p.Control, () =>
    {
        var s = dispatch.SetF3Region(F3Region.None, 0);
        Statements.EmitStatementList(p.Body);
        dispatch.RestoreF3Region(s);
    }, inline: true);

    /// <summary>Emit a Format-3 (exception-checking) PERFORM (ISO §14.9.28 Format 3) via the pc-RANGE interceptor
    /// (design SSOT §9.3.3). imperative-statement-1 is emitted INLINE inside a try (under the bind-time GR14
    /// overlay); a raise site within it consults the installed <c>PerformFrame</c> (GR17). The frame's matcher does
    /// tier-ordered WHEN selection (§14.9.49.4 GR3c-g) and runs the matching imp-2 (+ WHEN COMMON imp-4) as bounded
    /// pc-ranges via <c>__RunF3</c>/<c>__RunUse</c>. FINALLY (imp-5) is the INLINE trailing block, reached on every
    /// NON-fatal exit path (normal fall-off / imp-1 EXIT-PERFORM goto / a handler EXIT-PERFORM caught here); a fatal,
    /// unresumed EC throws PAST it (the chosen standard-defect default, §9.6 Q5). The frame pops in the finally
    /// BEFORE FINALLY so imp-5 behaves as if in a Format-2 PERFORM (GR21).</summary>
    public void EmitExceptionPerform(BoundExceptionPerform p)
    {
        var w = ctx.Writer;
        int n = p.PerformId;

        // Install the interceptor frame — the tier-ordered WHEN matcher closure (pure match arithmetic + __RunF3;
        // no goto/RESUME/EXIT inside it, so C#'s no-goto-out-of-a-lambda restriction never bites).
        w.Line("ExceptionState.PushPerformFrame(new PerformFrame { Matcher = (__ec, __f) =>");
        w.Line("{");
        w.Indent();
        EmitMatcherArms(p, w);
        w.Outdent();
        w.Line("}});");

        void EmitImp1()
        {
            var s = dispatch.SetF3Region(F3Region.Imp1, n);
            Statements.EmitStatementList(p.Imp1);   // inline, bound under the GR14 overlay
            dispatch.RestoreF3Region(s);
        }
        using (w.Block("try"))
        {
            // The handler EXIT-PERFORM catch (only when a handler contains EXIT PERFORM) sits in a NESTED try so it
            // is inside the finally that pops the frame; without it, imp-1 goes directly under the outer try.
            if (p.HandlerHasExit)
            {
                using (w.Block("try")) EmitImp1();
                w.Line($"catch (ExitPerformSignal __eps{n}) when (__eps{n}.Id == {n}) {{ }}   // handler EXIT PERFORM → §14.9.14.4 GR4");
            }
            else EmitImp1();
        }
        w.Line("finally { ExceptionState.PopPerformFrame(); }");
        w.Line($"__f3fin{n}: ;   // implicit CONTINUE preceding FINALLY (§14.9.14.4 GR4 / §14.9.28.4 GR16)");
        if (p.FinallyBody is { } fb)
        {
            var s = dispatch.SetF3Region(F3Region.Finally, n);
            Statements.EmitStatementList(fb);   // imp-5 inline; skipped on the fatal-throw path
            dispatch.RestoreF3Region(s);
        }
        w.Line($"__f3end{n}: ;   // end of PERFORM");
    }

    /// <summary>Emit the F3 matcher body: one <c>if (test) return __RunF3(...)</c> per WHEN operand, SORTED into the
    /// §14.9.49.4 GR3c-g tiers (file+L3 → file+L2 / bare-file → L3 → L2 → L1/EC-ALL; source order only within a
    /// tier, GR17), then WHEN OTHER (imp-3, GR18) as the unconditional fallback, else <c>NoMatch</c> (fall to the USE
    /// declaratives). A tier-4 EC-ALL operand is the catch-all (its <c>true</c> test makes a following OTHER/NoMatch
    /// unreachable, so it terminates the body). Every handler runs with its WHEN COMMON (imp-4) via <c>__RunF3</c>.</summary>
    private void EmitMatcherArms(BoundExceptionPerform p, CodeWriter w)
    {
        var arms = new List<(int Tier, int W, int O, string Test, int Imp2Pc)>();
        for (int wi = 0; wi < p.Whens.Count; wi++)
        {
            var m = p.Whens[wi];
            if (m.OpenMode is not null) continue;   // open-mode WHEN is a STAGED sub-GAP (COBOLNET0899 at bind, §9.7)
            for (int oi = 0; oi < m.Operands.Count; oi++)
            {
                var (tier, test) = ClassifyOperand(m.Operands[oi]);
                arms.Add((tier, wi, oi, test, m.Imp2Pc));
            }
        }
        arms.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier)
            : a.W != b.W ? a.W.CompareTo(b.W) : a.O.CompareTo(b.O));

        (int cu, int cpc) = p.CommonPc is int cp ? (HandlerUseId(cp), cp) : (-1, -1);
        bool catchAll = false;
        foreach (var arm in arms)
        {
            string call = $"__RunF3({HandlerUseId(arm.Imp2Pc)}, {arm.Imp2Pc}, {cu}, {cpc})";
            if (arm.Test == "true") { w.Line($"return {call};   // WHEN EC-ALL (§14.9.49.4 GR3g)"); catchAll = true; break; }
            w.Line($"if ({arm.Test}) return {call};");
        }
        if (catchAll) return;
        if (p.OtherPc is int opc)
            w.Line($"return __RunF3({HandlerUseId(opc)}, {opc}, {cu}, {cpc});   // WHEN OTHER (imp-3, §14.9.28.4 GR18)");
        else
            w.Line("return PerformFrame.NoMatch;   // no WHEN/OTHER selects → fall to __EcDispatch (USE), GR17 tail");
    }

    /// <summary>Classify one WHEN operand into its §14.9.49.4 GR3c-g tier + the runtime match test (mirrors
    /// <c>__EcDispatch</c>'s per-tier tests so the two never drift). A bare file-name (Ec null) → any EC-I-O
    /// associated with the file (tier 1, file+I-O ≈ level-2).</summary>
    private static (int Tier, string Test) ClassifyOperand(BoundWhenOperand op)
    {
        if (op.Ec is null)
            return (1, $"ExceptionCatalog.IsIoName(__ec) && __f == {FileKeyExpr(op.File!)}");
        int level = ExceptionCatalog.TryGet(op.Ec, out var info) ? info.Level : 3;
        if (op.File is not null)
            return level == 3
                ? (0, $"__f == {FileKeyExpr(op.File)} && __ec == {CsLiteral(op.Ec)}")
                : (1, $"__f == {FileKeyExpr(op.File)} && ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(op.Ec)})");
        return level switch
        {
            3 => (2, $"__ec == {CsLiteral(op.Ec)}"),
            2 => (3, $"ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(op.Ec)})"),
            _ => (4, "true"),   // level 1 = EC-ALL
        };
    }

    /// <summary>The <c>__useActive</c> re-entrancy-array id of an appended handler pc — its slot sits above the
    /// declarative slots (§9.1): <c>DeclCount + (pc − F3HandlerBasePc)</c>.</summary>
    private int HandlerUseId(int pc) => dispatch.DeclCount + (pc - dispatch.F3HandlerBasePc!.Value);

    /// <summary>An out-of-line PERFORM is a recursive bounded <c>Dispatch(start, end)</c> over the target pc range
    /// (the C# call stack is the return-address stack, COBOLNET_DESIGN §5.4). X3.23-1985 USE FOR DEBUGGING (VCR 7.17):
    /// the FIRST entry into the range is a plain-PERFORM transfer (DEBUG-CONTENTS SPACES); every subsequent loop
    /// iteration is DEBUG-CONTENTS "PERFORM LOOP" — a per-PERFORM first-iteration flag carries that (any loop form).</summary>
    public void EmitOutOfLinePerform(BoundOutOfLinePerform p)
    {
        var w = ctx.Writer;
        if (dispatch.DebugActive)
        {
            // DEBUG-LINE for a PERFORM/iteration trigger is the PERFORM statement's own line on EVERY iteration
            // (DB101A PERF-ITERATION-TEST :611-617); DEBUG-CONTENTS is SPACES on entry, "PERFORM LOOP" on re-iter.
            int fid = ctx.Names.NextLoop();
            w.Line($"bool __dbgFirst{fid} = true;");
            EmitPerform(p.Control, () =>
            {
                w.Line($"__dbgCause = __dbgFirst{fid} ? DebugCause.Transfer : DebugCause.PerformLoop; __dbgFirst{fid} = false; __dbgLine = {p.SourceLine};");
                w.Line($"{dispatch.DispatchName}({p.StartPc}, {p.EndPc});");
            }, inline: false);
        }
        else
            EmitPerform(p.Control, () => w.Line($"{dispatch.DispatchName}({p.StartPc}, {p.EndPc});"), inline: false);
    }


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
            case PerformForever:
                // UNTIL EXIT (§14.9.28.4 GR11, 2023): the condition never becomes true. An inline EXIT PERFORM emits
                // `break` (StatementEmitter.Visit(BoundExitPerform)); an out-of-line loop escapes only via GOBACK/STOP.
                using (w.Block("while (true)")) body();
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
