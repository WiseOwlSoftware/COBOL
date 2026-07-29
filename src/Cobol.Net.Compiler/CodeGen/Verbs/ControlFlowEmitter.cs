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

    /// <summary>The EC raise-site dispatcher (property-wired by <see cref="UnitEmitters"/>, the same cyclic edge as
    /// <see cref="Statements"/>) — a SEARCH with EC-RANGE checking ON but NO AT END phrase dispatches the raised
    /// range EC to an applicable USE declarative / Format-3 WHEN via <see cref="EcEmitter.EcDispatchExpr"/>
    /// (ISO §14.9.37.4 GR1b2; CA36).</summary>
    internal EcEmitter Ec { get; set; } = null!;

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

    // EmitPerform(inline:true) brackets the body with a fresh F3Region.Inline(pid) + the __pcont/__pexit labels, so
    // an EXIT PERFORM here targets THIS loop and a nested inline PERFORM (setting its OWN Inline id) targets the inner
    // loop — §14.9.14.4 GR5a "the most closely preceding, unterminated inline PERFORM". No manual region reset needed.
    public void EmitInlinePerform(BoundInlinePerform p) =>
        EmitPerform(p.Control, () => Statements.EmitStatementList(p.Body), inline: true);

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
            // §14.9.28.4 GR14 covers imp-5 too: the implicit POP ALL sits "immediately preceding the END PERFORM
            // phrase", so FINALLY runs inside the TURN OFF ALL window just as the WHEN bodies do (__RunF3 does the
            // same for imp-2/3/4). A `goto` out of this try to __f3end is legal C#, so EXIT PERFORM in imp-5
            // (§14.9.28.4 GR16) still reaches the implicit CONTINUE following END-PERFORM.
            w.Line($"var __ckfin{n} = ExceptionState.PushAllCheckingOff();   // GR14 implicit PUSH ALL + TURN OFF ALL");
            using (w.Block("try"))
                Statements.EmitStatementList(fb);   // imp-5 inline; skipped on the fatal-throw path
            w.Line($"finally {{ ExceptionState.PopAllChecking(__ckfin{n}); }}   // GR14 implicit POP ALL");
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
            if (m.OpenMode is { } mode)
            {
                // WHEN EXCEPTION INPUT | OUTPUT | I-O | EXTEND — GR3b open-mode scope (tier 1): an EC-I-O whose file
                // is CURRENTLY OPEN in that mode. (An OPEN-failure's mode is best-effort — the connector reports its
                // mode only once open; §9.7.) __f is null for a non-I-O condition or a file-less EC-I-O RAISE.
                arms.Add((1, wi, 0,
                    $"ExceptionCatalog.IsIoName(__ec) && __f is not null && {RuntimeApi.FileOpenModeOf("__f")} == {ModeOrdinal(mode)}",
                    m.Imp2Pc));
                continue;
            }
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

    /// <summary>Classify one WHEN operand into its §14.9.49.4 GR3a-g tier + the runtime match test (the EC-name
    /// tiers mirror <c>__EcDispatch</c>'s per-tier tests so the two never drift). GR17 selects across the WHEN
    /// operands by GR3's a→g priority: bare file-name (GR3a) > open-mode (GR3b, in the caller) > file+L3 (GR3c) >
    /// file+L2 (GR3d) > L3 (GR3e) > L2 (GR3f) > L1/EC-ALL (GR3g); source order only WITHIN a tier.</summary>
    private static (int Tier, string Test) ClassifyOperand(BoundWhenOperand op)
    {
        if (op.Ec is null)   // bare file-name — GR3a (any EC-I-O associated with the file), the HIGHEST tier
            return (0, $"ExceptionCatalog.IsIoName(__ec) && __f == {FileKeyExpr(op.File!)}");
        int level = ExceptionCatalog.TryGet(op.Ec, out var info) ? info.Level : 3;
        if (op.File is not null)
            return level == 3
                ? (2, $"__f == {FileKeyExpr(op.File)} && __ec == {CsLiteral(op.Ec)}")                             // GR3c
                : (3, $"__f == {FileKeyExpr(op.File)} && ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(op.Ec)})"); // GR3d
        return level switch
        {
            3 => (4, $"__ec == {CsLiteral(op.Ec)}"),                                                              // GR3e
            2 => (5, $"ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(op.Ec)})"),                                  // GR3f
            _ => (6, "true"),   // level 1 = EC-ALL                                                               // GR3g
        };
    }

    /// <summary>The <c>FileOpenMode</c> ordinal of a WHEN EXCEPTION open-mode keyword (INPUT/OUTPUT/I-O/EXTEND) —
    /// matches the binder's `useOnTarget` mode mapping and the runtime enum.</summary>
    private static int ModeOrdinal(string mode) => mode switch
    {
        "INPUT" => (int)CobolNet.Runtime.IO.FileOpenMode.Input,
        "OUTPUT" => (int)CobolNet.Runtime.IO.FileOpenMode.Output,
        "EXTEND" => (int)CobolNet.Runtime.IO.FileOpenMode.Extend,
        "I-O" => (int)CobolNet.Runtime.IO.FileOpenMode.IO,
        _ => -1,
    };

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


    /// <summary>Emit a PERFORM's loop scaffold. For an inline PERFORM this wrapper brackets the body with the
    /// per-PERFORM EXIT-PERFORM machinery: a fresh <see cref="F3Region.Inline"/> region, a <c>__pcont{id}</c> label at
    /// the loop-control boundary (target of EXIT PERFORM CYCLE — falls through to the VARYING augment + re-test), and a
    /// <c>__pexit{id}</c> label just past the loop (target of EXIT PERFORM — leaves EVERY nested VARYING level).
    /// §14.9.14.4 GR5/GR6 require leaving/continuing the WHOLE inline PERFORM, which a bare C# break/continue cannot do
    /// across the nested loops a multi-level VARYING emits (CA31/CA32). An out-of-line PERFORM never contains its own
    /// EXIT PERFORM (SR8), so it takes the bare loop with no region/labels.</summary>
    private void EmitPerform(BoundPerformControl control, Action body, bool inline)
    {
        if (!inline) { EmitPerformLoop(control, body, inline: false); return; }
        var w = ctx.Writer;
        int pid = ctx.Names.NextLoop();
        var saved = dispatch.SetF3Region(F3Region.Inline, pid);
        EmitPerformLoop(control, () => { body(); w.Line($"__pcont{pid}: ;"); }, inline: true);
        dispatch.RestoreF3Region(saved);
        w.Line($"__pexit{pid}: ;");
    }

    private void EmitPerformLoop(BoundPerformControl control, Action body, bool inline)
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
                // UNTIL EXIT (§14.9.28.4 GR11, 2023): the condition never becomes true. An inline EXIT PERFORM leaves
                // via `goto __pexit` (StatementEmitter.Visit(BoundExitPerform)); an out-of-line loop escapes only via GOBACK/STOP.
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
            foreach (var lv in levels) InitVaryingTarget(v, lv);   // GR13a: left-to-right init
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
                        InitVaryingTarget(v, levels[k + 1]);
                    }
                }
            }
        }
        else
        {
            InitVaryingTarget(v, levels[0]);
            EmitAfter(0);
            void EmitAfter(int k)
            {
                using (w.Block("while (true)"))
                {
                    if (k == levels.Count - 1) body();
                    else
                    {
                        InitVaryingTarget(v, levels[k + 1]);   // reinit on each entry
                        EmitAfter(k + 1);
                    }
                    w.Line($"if ({cond.Render(levels[k].Until)}) break;");
                    set.AugmentSetTarget(levels[k].Var, down: false, num.Render(levels[k].By, ReceiverContext.None));
                }
            }
        }
    }

    /// <summary>Initialize a PERFORM VARYING (or AFTER) level's target from its FROM operand (GR13). When the target
    /// is an INDEX-NAME initialized from a data-item FROM and EC-RANGE-PERFORM-VARYING checking is enabled
    /// (§14.9.28.4 GR3), materialize the FROM value ONCE, raise the fatal EC when it is not positive (the runtime
    /// tests the DATA-ITEM value, GR3 — the throw is caught by the FatalAmbientGates guard for USE-F3 dispatch), then
    /// assign the index; otherwise the plain store (byte-identical). GR3 governs FROM initialization only, so the
    /// BY/augment sites are unaffected. A literal FROM (BoundNumLiteral) and an index-name FROM (BoundIndexRef) are
    /// out of GR3 scope and take the plain path.</summary>
    private void InitVaryingTarget(PerformVarying v, VaryingLevel lv)
    {
        if (v.CheckIndexRange && lv.Var is SetIndexTarget ix && lv.From is BoundNumRef)
        {
            string tmp = $"__pv{ctx.Names.NextVary()}";
            ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(num.Render(lv.From, ReceiverContext.None), 0)});");
            ctx.Writer.Line($"ExceptionState.PerformVaryingIndexError({tmp}, "
                + $"{EmitText.CsLiteral("PERFORM VARYING index-name initialized from a non-positive item (ISO 14.9.28.4 GR3)")});");
            ctx.Writer.Line($"{ix.IndexField} = {tmp};");
            return;
        }
        set.StoreSetTarget(lv.Var, num.Render(lv.From, ReceiverContext.None));
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

    /// <summary>The SEARCH scan loop (ISO §14.9.37.4). An INITIAL-index guard runs BEFORE the loop: for a serial
    /// SEARCH a starting index &lt; 1 or &gt; the highest permissible occurrence is unsuccessful and, under checking,
    /// sets EC-RANGE-SEARCH-INDEX (GR4); for SEARCH ALL (the index is already forced to 1, GR9) only an empty table
    /// is unsuccessful and sets EC-RANGE-SEARCH-NO-MATCH. The loop then tries each WHEN in order; none true → the
    /// index (and the GR8 VARYING item) advance and an advance-past-end check sets EC-RANGE-SEARCH-NO-MATCH (GR6/GR9).
    /// Both failure sites route to ONE shared AT-END emission. The <c>&lt; 1</c> serial guard is emitted
    /// UNCONDITIONALLY (a correctness fix — the pre-slice loop-top <c>&gt; bound</c> check let a zero/negative index
    /// read a phantom scratch occurrence); only the EC <c>Set</c> calls are checking-gated. The AT-END bound is the
    /// table's MAXIMUM occurrence count — or an occurs-depending table's CURRENT count, or (D9) a dynamic table's
    /// current <c>Capacity</c> (§13.18.38 GR7/§8.5.1.9.1). Extracted so a dynamic table can wrap it in an
    /// EnterSearch/ExitSearch try/finally.</summary>
    private void EmitSearchScan(BoundSearch s, int id)
    {
        var w = ctx.Writer;
        string bound = s.DynTable is { } dt ? $"{dt}.Capacity"
            : s.DependItem is { } dp ? RuntimeApi.TableOcc(PlaceRenderer.Read(dp))
            : $"{s.Count}L";
        // CA36 (ISO §14.9.37.4 GR1b2): when the AT END phrase is ABSENT and EC-RANGE checking is ON, a raised
        // EC-RANGE-SEARCH-INDEX/-NO-MATCH must transfer to an applicable exception-processing statement (a USE
        // AFTER EXCEPTION CONDITION declarative / Format-3 PERFORM WHEN) and, if control returns, to the end of the
        // SEARCH. Track which range EC was raised so the shared AT-END funnel can DISPATCH it (mirror
        // EcEmitter.EmitOverflow's no-phrase dispatch). Emitted ONLY in that niche, so every other SEARCH is
        // byte-identical.
        bool dispatchEc = s.AtEnd is null && (s.CheckSearchIndex || s.CheckSearchNoMatch);
        string ecVar = $"__searchEc{id}";
        if (dispatchEc) w.Line($"string {ecVar} = null;");
        void RaiseRange(string ecName)
        {
            w.Line($"ExceptionState.Set(\"{ecName}\", false);");
            if (dispatchEc) w.Line($"{ecVar} = \"{ecName}\";");
        }
        // (1) Initial-index guard (§14.9.37.4 GR4) — before the loop label.
        string initGuard = s.FromStart ? $"{s.IndexField} > {bound}"
                                        : $"{s.IndexField} < 1 || {s.IndexField} > {bound}";
        using (w.Block($"if ({initGuard})"))
        {
            if (s.FromStart)
            {
                if (s.CheckSearchNoMatch) RaiseRange("EC-RANGE-SEARCH-NO-MATCH");
            }
            else if (s.CheckSearchIndex)
                RaiseRange("EC-RANGE-SEARCH-INDEX");
            w.Line($"goto __searchAtEnd{id};");
        }
        w.Line($"__search{id}:");
        // (2) the WHEN conditions, first true wins (a body that completes jumps past the search).
        foreach (var when in s.Whens)
            using (w.Block($"if ({cond.Render(when.Condition)})"))
            {
                if (!Statements.EmitStatementList(when.Statements)) w.Line($"goto __searchEnd{id};");
            }
        // (3) advance the index (+ the GR8 varied item); an advance past the end is unsuccessful → NO-MATCH.
        w.Line($"{s.IndexField} += 1;");
        if (s.AlsoVaried is { } also) set.AugmentSetTarget(also, down: false, new NumX("1", 0));
        using (w.Block($"if ({s.IndexField} > {bound})"))
        {
            if (s.CheckSearchNoMatch) RaiseRange("EC-RANGE-SEARCH-NO-MATCH");
            w.Line($"goto __searchAtEnd{id};");
        }
        w.Line($"goto __search{id};");
        // (4) the shared AT-END emission — both failure sites reach it (emitted ONCE), then the search-end label.
        w.Line($"__searchAtEnd{id}: ;");
        // GR1b2: AT END absent + checking on → dispatch the raised range EC to an applicable USE declarative / F3
        // WHEN; >=0 = RESUME AT a procedure (transfer via the dispatcher break); -1/-2/-3 (declarative ran / RESUME
        // NEXT / no handler) fall through to the end of the SEARCH (nonfatal — §14.6.13.1.4 #3/#4).
        if (dispatchEc)
            using (w.Block($"if ({ecVar} != null)"))
            {
                w.Line($"int __searchR{id} = {Ec.EcDispatchExpr(ecVar, "\"\"")};");
                w.Line($"if (__searchR{id} >= 0) {{ __pc = __searchR{id}; break; }}");
            }
        bool terminated = s.AtEnd is { } at && Statements.EmitStatementList(at);
        if (!terminated) w.Line($"goto __searchEnd{id};");
        w.Line($"__searchEnd{id}: ;");
    }

}
