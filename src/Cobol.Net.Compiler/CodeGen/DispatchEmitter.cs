// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The PC dispatcher (COBOLNET_DESIGN §5; a real collaborator since P7 Step 9n): each paragraph is a
/// <c>case</c> in one <c>Dispatch</c> method; control is by pc value (GO TO sets pc; fall-through is pc+1; an
/// out-of-line PERFORM is a recursive bounded <c>Dispatch(start, end)</c>). STOP RUN unwinds all frames via
/// <c>StopRun</c>, caught at <c>Main</c>. This realizes the legacy's proven return-address / exit-bounded
/// dispatch (DEVLOG 259–260) in idiomatic C#. Parameterized over program AND class units — the
/// <c>__MDispatch</c> swap rides <see cref="DispatchState.DispatchName"/> (the OO emit-into-a-type reuse).
/// </summary>
internal sealed class DispatchEmitter(EmitContext ctx, DispatchState dispatchState, EcState ecState,
    AlterSwitchEmitter alterSwitch, ReportWriterEmitter reportWriter, SequentialIoEmitter seqIo,
    EcEmitter ec, StatementEmitter statements)
{
    /// <summary>Emit the single program-counter dispatcher for a program class: the <c>__Activate</c> entry
    /// (file registration at first activation — the IC114A lesson), the USE machinery when declared, and the
    /// instance <c>__Dispatch</c> over the full paragraph range.</summary>
    public void EmitDispatcher(BoundProgram bound, CodeWriter w)
    {
        int n = bound.Paragraphs.Count;
        // Hooks are needed for the program's OWN declaratives (GR4a) — or, with none, for the outward GR4b walk
        // to a containing program's GLOBAL declaratives (IC233A: the contained unit has no declaratives, yet its
        // failing OPEN must fire the outer's USE GLOBAL).
        dispatchState.UseDecls = bound.Declaratives is { Count: > 0 } || dispatchState.OuterGlobalUse;
        // Exception-checking (Format-3) PERFORM handler context: the declarative count + the first appended handler
        // pc let ControlFlowEmitter derive each handler's __useActive id (DeclCount + pc − base). Null base ⇒ no F3.
        dispatchState.DeclCount = bound.Declaratives?.Count ?? 0;
        dispatchState.F3HandlerBasePc = bound.F3HandlerBasePc;
        // X3.23-1985 USE FOR DEBUGGING procedure-trigger facility (VCR Table 7 row 7.17): active when a
        // procedure-subject debugging declarative was collected under WITH DEBUGGING MODE. Gates all debug
        // scaffolding (zero-scaffolding invariant — a non-debug program is byte-identical).
        dispatchState.DebugActive = bound.DebugSubjects is { Count: > 0 };
        dispatchState.DebugByPc = bound.DebugSubjects?.ToDictionary(s => s.SubjectPc)
            ?? new Dictionary<int, BoundDebugSubject>();
        w.Line();
        // The dispatcher internals use a `__` prefix — COBOL data-names cannot contain a double underscore — so they
        // never collide with a program's fields (e.g. a COBOL `01 N` and the paragraph count `__N`).
        w.Line($"private const int __N = {n};   // paragraph count");
        alterSwitch.EmitFields(bound, w);   // the per-altered-paragraph mutable GO TO target fields (control-flow design D4)
        reportWriter.EmitReportMembers(w);      // per-report engine fields + line compose methods (Verbs/ReportWriterEmitter.cs)
        if (dispatchState.DebugActive)
        {
            // The X3.23-1985 DEBUG-ITEM special register + its per-trigger transfer cause (VCR Table 7 row 7.17).
            w.Line("private readonly DebugItem __dbgItem = new DebugItem();   // X3.23-1985 DEBUG-ITEM register (VCR 7.17)");
            w.Line("private DebugCause __dbgCause;   // the transfer-of-control cause of the next debug trigger (default Transfer → SPACES)");
            w.Line("private int __dbgLine;   // the CAUSING statement's source line — DEBUG-LINE for a non-START-PROGRAM trigger");
            w.Line("private bool __dbgBusy;   // a debugging declarative is not itself debugged (re-entrancy guard)");
        }
        w.Line();
        using (w.Block("public void __Activate()"))
        {
            // Register this program's SELECTed files at FIRST ACTIVATION of this instance (the IC114A lesson:
            // connectors belong to the program's entry, not the run-unit Main; a fresh instance after CANCEL /
            // an INITIAL activation re-registers — ISO §14.6.2.3.2). Run-unit CloseAll lives in the runtime RunMain boundary.
            if (ctx.Data.Files.Count > 0)
            {
                using (w.Block("if (!__filesRegistered)"))
                {
                    w.Line("__filesRegistered = true;");
                    seqIo.EmitFileRegistration(w);
                    // Report engines construct WITH the connectors (hazard: the report FD must be registered
                    // before the engine's first write — COBOLNET_REPORT_WRITER_DESIGN §4).
                    reportWriter.EmitReportConstruction(bound, w);
                }
            }
            // Execution begins at the first NONdeclarative procedure (ISO §14.2.3 GR1) — declarative sections
            // occupy the pcs below EntryPc, entered only via __RunUse or an explicit PERFORM/GO TO (SR4).
            // X3.23-1985: the FIRST execution of the first nondeclarative procedure is DEBUG-CONTENTS "START PROGRAM".
            dispatchState.EmitDebugCause(w, "StartProgram");
            // The top-level run is bounded at the last MAIN paragraph (F3HandlerBasePc − 1) when the unit has
            // appended Format-3 handler pc-ranges above it, so fall-through off the last real paragraph ENDS the run
            // unit (§14.9.18) and never runs into the synthetic handlers; a non-F3 unit renders the literal -1 (the
            // whole pc space) — byte-identical (the wall, design §9.5.3). Only this top-level call is walled; every
            // bounded __RunUse / out-of-line PERFORM passes its own exit pc.
            int topExit = bound.F3HandlerBasePc is int hb ? hb - 1 : -1;
            w.Line($"try {{ __Dispatch({bound.EntryPc}, {topExit}); }} catch (ProgramReturn) {{ }}   // GOBACK / called-program EXIT PROGRAM returns to the activator here (ISO §14.9.18 GR2/GR3; §14.9.14 GR3)");
        }
        w.Line();
        // The machinery also emits for a declarative-FREE program whose statements carry enabled EC-I-O checking
        // (__IoCheckEc needs no declaratives to bridge status→EC and apply the fatal default) — gated so an
        // EC-free program's source is unchanged.
        if (dispatchState.UseDecls || bound.Ec is { HasIoChecked: true } || bound.Ec is { HasF3Perform: true })
            EmitUseMachinery(bound, w);   // an F3 PERFORM needs __RunUse/__EcPerform even with no USE declaratives (§14.9.28)
        if (dispatchState.DebugActive) EmitDebugRunner(w);
        EmitDispatchMethod(bound, w, "private int __Dispatch(int __startPc, int __exitPc)",
            0, bound.Paragraphs.Count - 1);
    }

    // The dispatch-method NAME the statement emitters call for a bounded range is DispatchState.DispatchName
    // (Step 9b — see EmitterState.cs for the __Dispatch / __MDispatch contract).

    /// <summary>Emit one dispatch-method body over the pc slice [<paramref name="fromPc"/>..<paramref name="toPc"/>]
    /// — SHARED by program classes (via <see cref="EmitDispatcher"/>: the full range as the instance
    /// <c>__Dispatch</c>) and COBOL classes (<c>OoEmitMethod</c>: each METHOD-ID's contiguous slice of the
    /// class's ONE pc space as a local function; a pc outside the slice hits <c>default:</c> and exits — the
    /// emit-into-a-type parameterization of the OO deep-dive).</summary>
    public void EmitDispatchMethod(BoundProgram bound, CodeWriter w, string header, int fromPc, int toPc,
        int handlerFromPc = -1, int handlerToPc = -1)
    {
        void EmitCase(int i)
        {
            dispatchState.CurrentPc = i;
            using (w.Block($"case {i}:   // {bound.Paragraphs[i].CobolName}"))
            {
                // An appended Format-3 handler pc-range (imp-2/3/4): mark the region so an EXIT PERFORM in
                // its body throws ExitPerformSignal to the owning PERFORM boundary (§14.9.14.4 GR4), not a
                // dispatcher break. Owner = F3HandlerOwners[i − base] (the PerformId).
                bool isHandler = bound.F3HandlerBasePc is int hb && i >= hb;
                var f3saved = isHandler
                    ? dispatchState.SetF3Region(F3Region.Handler, bound.F3HandlerOwners![i - bound.F3HandlerBasePc!.Value])
                    : default;
                // X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17): a debug SUBJECT procedure fires
                // its debugging declarative just BEFORE its own body — populate DEBUG-ITEM from the
                // transfer cause (__dbgCause, set by whatever transferred control here) and run the
                // section over its bounded pc range.
                if (dispatchState.DebugActive && dispatchState.DebugByPc.TryGetValue(i, out var subj))
                    w.Line($"__RunDebug({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(subj.SubjectName, true)}, "
                        + $"{subj.SourceLine}, {subj.SectionStartPc}, {subj.SectionEndPc});");
                if (!EmitParagraphBody(bound.Paragraphs[i], i))
                {
                    // Sequential fall-through into pc+1 is DEBUG-CONTENTS "FALL THROUGH"; DEBUG-LINE is
                    // this paragraph's LAST statement (the causing statement that fell through, DB101A
                    // FALL-THROUGH-TEST :403-407).
                    dispatchState.EmitDebugCause(w, "FallThrough", bound.Paragraphs[i].SourceLine);
                    w.Line($"__pc = {i + 1};");
                    w.Line("break;");
                }
                if (isHandler) dispatchState.RestoreF3Region(f3saved);
            }
        }
        using (w.Block(header))
        {
            w.Line("int __pc = __startPc;");
            using (w.Block("while ((uint)__pc < (uint)__N)"))
            {
                w.Line("bool __atExit = __pc == __exitPc;   // captured before the body overwrites __pc");
                using (w.Block("switch (__pc)"))
                {
                    for (int i = fromPc; i <= toPc; i++) EmitCase(i);
                    // The method's Format-3 handler pc-ranges (design SSOT §9.10) — a NON-contiguous second case set
                    // (appended above the whole class pc space, so outside [EntryPc..EndPc]); entered ONLY via the
                    // method-local __RunUse(id, hpc, hpc). Empty (handlerFromPc < 0) for a program's __Dispatch — whose
                    // main loop [0..Count−1] already covers its appended handlers — so the program path is byte-identical.
                    if (handlerFromPc >= 0)
                        for (int i = handlerFromPc; i <= handlerToPc; i++) EmitCase(i);
                    using (w.Block("default:")) { w.Line("__pc = __N;"); w.Line("break;"); }
                }
                w.Line("if (__atExit && __pc == __exitPc + 1) return __pc;   // a named THRU exit paragraph fell off its end");
            }
            w.Line("return __pc;");
        }
    }

    /// <summary>Emit the X3.23-1985 debug trigger runner (VCR Table 7 row 7.17): space-fill DEBUG-ITEM, set
    /// DEBUG-LINE/DEBUG-NAME/DEBUG-CONTENTS for the triggering occurrence, then run the debugging declarative body
    /// over the bounded pc range. Guarded by the OBJECT-TIME switch (<c>RunUnit.DebugMode</c>, default ON — the CCVS
    /// posture) and a re-entrancy flag (a debugging declarative is not itself debugged). Emitted ONLY for a
    /// debug-active program.</summary>
    private void EmitDebugRunner(CodeWriter w)
    {
        using (w.Block("private void __RunDebug(string __nm, int __subjLine, int __ds, int __de)"))
        {
            w.Line("if (!RunUnit.Current.DebugMode) return;   // the X3.23-1985 object-time debug switch (default ON)");
            w.Line("if (__dbgBusy) return;");
            w.Line("__dbgBusy = true;");
            // DEBUG-LINE: START PROGRAM has no causing statement — it is the subject's own first executable
            // statement (DB101A START-PROGRAM-TEST :257-264); every other cause is the causing (transferring)
            // statement, carried in __dbgLine by the transfer site.
            w.Line("int __line = __dbgCause == DebugCause.StartProgram ? __subjLine : __dbgLine;");
            w.Line("__dbgItem.Populate(__line, __nm, __dbgCause);");
            w.Line("try { __Dispatch(__ds, __de); } finally { __dbgBusy = false; }");
        }
        w.Line();
    }

    /// <summary>Emit the USE-declaratives machinery (ISO §14.9.49; emitted ONLY when the program declares USE
    /// procedures — a declarative-free program's generated source is byte-identical): the per-section
    /// re-entrancy guards (GR2 — instance state, reset by a fresh activation instance §14.6.2.3.2), the
    /// <c>__RunUse</c> bounded-dispatch invoker, and the <c>__IoCheck</c> selector (GR3/GR5/GR6 + §9.1.13.1:
    /// after an unsuccessful I-O status not covered by the statement's own AT END / INVALID KEY phrase, run at
    /// most ONE declarative — file-scoped first, then the open-mode scope incl. a file in the process of being
    /// opened).</summary>
    private void EmitUseMachinery(BoundProgram bound, CodeWriter w)
    {
        var decls = bound.Declaratives ?? [];
        // The exception-checking (Format-3) PERFORM handler bodies (imp-2/3/4) are appended pc-ranges invoked by the
        // SAME __RunUse; each needs a __useActive slot above the declarative slots (§14.9.28.4 GR17). H is 0 until the
        // pc-range synthesis wave, so a non-F3-PERFORM unit's array is byte-identical.
        int f3Handlers = bound.F3HandlerBasePc is int hb ? bound.Paragraphs.Count - hb : 0;
        if (decls.Count > 0 || bound.Ec is { HasF3Perform: true })
        {
            w.Line($"private readonly bool[] __useActive = new bool[{decls.Count + f3Handlers}];   // §14.9.49.4 GR2 re-entrancy guards");
            if (ecState.Active)
                // The EC-model form: __RunUse RETURNS the declarative's resume action (the dispatch result
                // protocol — EcEmitter): a RESUME statement unwinds via ResumeSignal (§14.9.33; the
                // StopRun/ProgramReturn exception-as-control precedent) and __RunUse converts it to the
                // action; normal completion is -1 (§14.6.13.1.2). Emitted ONLY when the group uses the
                // EC model — an EC-free build keeps the void form byte-identical.
                using (w.Block("private int __RunUse(int __id, int __startPc, int __endPc)")) EmitRunUseBody(w, ecModel: true);
            else
                using (w.Block("private void __RunUse(int __id, int __startPc, int __endPc)")) EmitRunUseBody(w, ecModel: false);
            w.Line();
        }
        if (decls.Any(d => d.EcEntries is not null)) ec.EmitDispatchSelector(bound, w);
        if (decls.Any(d => d.EoClassCsName is not null)) ec.EmitObjDispatchSelector(bound, w);   // F4 (EC-OO)
        if (bound.Ec is { HasIoChecked: true }) ec.EmitIoCheckEc(bound, w);
        if (ecState.UnitHasF3Perform) ec.EmitPerformInterceptor(w);   // __EcPerform + __RunF3 (§14.9.28 F3 interceptor)
        if (!dispatchState.UseDecls) return;   // an EC-only program (no F1/F2 declaratives) needs no plain __IoCheck hooks
        using (w.Block("private void __IoCheck(string __f, bool __atEnd, bool __invKey)"))
        {
            w.Line($"string __st = {RuntimeApi.FileStatus("__f")};");
            w.Line("if (__st.Length == 0 || __st[0] == '0') return;   // successful — no declarative (ISO §14.9.49.4 GR6)");
            w.Line("if (__atEnd && __st[0] == '1') return;    // the statement's AT END phrase covers the at-end family (§9.1.13.1)");
            w.Line("if (__invKey && __st[0] == '2') return;   // the statement's INVALID KEY phrase covers its family (§9.1.13.1)");
            if (decls.Any(d => d.Files.Count > 0))
                using (w.Block("switch (__f)"))   // file-name scope first (GR3a/GR5)
                {
                    for (int i = 0; i < decls.Count; i++)
                        foreach (var f in decls[i].Files)
                            w.Line($"case {FileKeyExpr(f)}: __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); return;");
                }
            if (decls.Any(d => d.ModeIndex is not null))
                using (w.Block($"switch ({RuntimeApi.FileOpenModeOf("__f")})"))   // open-mode scope (GR3b/GR6b–e)
                {
                    for (int i = 0; i < decls.Count; i++)
                        if (decls[i].ModeIndex is { } m)
                            w.Line($"case {m}: __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); return;");
                }
            // No local declarative qualified — walk OUTWARD to the nearest containing program with a USE GLOBAL
            // declarative (ISO §14.9.49.4 GR4b: "a qualifying declarative with the GLOBAL attribute in the next
            // inclusive directly containing source element", repeated outward). The declarative executes in the
            // DECLARING program's instance — its data (§8.4.6.2) — via the container's __RunGlobalUse.
            if (dispatchState.OuterGlobalUse)
                w.Line("__outer.__RunGlobalUse(__f);");
        }
        w.Line();
    }

    /// <summary>Emit the shared <c>__RunUse</c> body (bounded-dispatch invoker, ISO §14.9.49.4 GR2 re-entrancy guard).
    /// Renders <see cref="DispatchState.DispatchName"/> — <c>__Dispatch</c> as a class member (a program), <c>__MDispatch</c>
    /// as a method-local function (an OO method's F3 PERFORM, design SSOT §9.10) — so the ONE body serves both scopes
    /// (the C3 correction: the former literal <c>__Dispatch</c> hardcode would name a nonexistent method inside a class).
    /// <paramref name="ecModel"/> = the int RESUME-returning form (an EC-model group) vs the void form.</summary>
    internal void EmitRunUseBody(CodeWriter w, bool ecModel)
    {
        w.Line($"if (__useActive[__id]) return{(ecModel ? " -1" : "")};   // ISO §14.9.49.4 GR2 — an active USE procedure is not re-invoked");
        w.Line("__useActive[__id] = true;");
        if (ecModel)
        {
            w.Line($"try {{ {dispatchState.DispatchName}(__startPc, __endPc); }}");
            w.Line("catch (ResumeSignal __rs) { return __rs.TargetPc; }   // RESUME (§14.9.33) — the resume action");
            w.Line("finally { __useActive[__id] = false; }");
            w.Line("return -1;   // normal completion (§14.6.13.1.2)");
        }
        else
            w.Line($"try {{ {dispatchState.DispatchName}(__startPc, __endPc); }} finally {{ __useActive[__id] = false; }}");
    }

    /// <summary>Emit a paragraph body SENTENCE by sentence. When the paragraph contains a NEXT SENTENCE anywhere,
    /// each inter-sentence boundary gets a label (`__sentP_K:`) — the §14.9.19 GR6 implicit CONTINUE after the
    /// separator period; NEXT SENTENCE in the LAST sentence is the paragraph fall-through. Returns whether the
    /// body ends by transferring control out of the case.</summary>
    private bool EmitParagraphBody(BoundParagraph para, int pc)
    {
        var w = ctx.Writer;
        var sentences = para.Sentences;
        bool needLabels = sentences.Any(ContainsNextSentence);
        bool terminated = false;
        for (int k = 0; k < sentences.Count; k++)
        {
            bool last = k == sentences.Count - 1;
            dispatchState.SentenceEndLabel = needLabels && !last ? $"__sent{pc}_{k}" : null;
            // Unlike intra-sentence dead code, a LABELLED sentence boundary is reachable via NEXT SENTENCE even
            // after an unconditional transfer — so only skip remaining sentences when no labels exist.
            if (terminated && !needLabels) break;
            terminated = statements.EmitStatementList(sentences[k]);
            if (needLabels && !last) w.Line($"__sent{pc}_{k}: ;");
        }
        dispatchState.SentenceEndLabel = null;
        return terminated;
    }

    /// <summary>True when a statement list contains a <see cref="BoundNextSentence"/> anywhere — recursing over the
    /// generated <see cref="BoundStatementTree.StatementChildren"/> (PHASE-07 Step 6g), which is the ONE drift-proof
    /// enumeration of every nesting container the binder produces (IF/EVALUATE arms, inline-PERFORM & SEARCH bodies,
    /// READ/keyed/WRITE/CALL/arithmetic phrases), so a new container node is covered automatically. A missed container
    /// would emit a goto with NO label → a loud backend compile failure, never a silent misjump. (Replaces the former
    /// hand-maintained walker, which had missed EVALUATE/CALL/WRITE phrase bodies.)</summary>
    private static bool ContainsNextSentence(IReadOnlyList<BoundStatement> stmts) => stmts.Any(HasNextSentence);

    private static bool HasNextSentence(BoundStatement s) =>
        s is BoundNextSentence || s.StatementChildren().Any(HasNextSentence);
}
