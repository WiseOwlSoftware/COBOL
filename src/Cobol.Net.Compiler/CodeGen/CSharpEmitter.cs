// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.CodeGen;

using Core = CobolParserCore;
using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The Roslyn (C#) backend orchestrator: binds a program unit to a <see cref="BoundProgram"/> and renders it to
/// typed-native C# source, delegating to the decomposed emitters/renderers over a shared <see cref="EmitContext"/>
/// (COBOLNET_DESIGN §17 §2.2): <see cref="FieldEmitter"/> (DATA DIVISION), <see cref="NumericRenderer"/> (arithmetic),
/// <see cref="ConditionRenderer"/> (conditions), and <see cref="OperandText"/> (DISPLAY images). It consumes only the
/// bound tree for the PROCEDURE DIVISION — it never walks the parse tree (that is the binder's job) — and an
/// unsupported construct surfaces as a bound error node and emits a LOUD <c>NotImplemented</c> guard (§1.4).
/// </summary>
/// <remarks><b>Status — G2:</b> groups→record structs, OCCURS→arrays, qualified/subscripted refs, figurative +
/// signed DISPLAY, level-88. Control flow is a sequential paragraph call-chain (the G4 PC dispatcher replaces it).</remarks>
public sealed partial class CSharpEmitter : IOoBindHost
{
    private EmitContext _ctx = null!;
    private NumericRenderer _num = null!;
    private ConditionRenderer _cond = null!;
    private ReferenceResolver _refs = null!;
    private NameAllocator _names = null!;   // RUN-UNIT scope (set by CallEmitRunUnit) — rides every per-unit _ctx as Names

    // The emitter's mutable state model (Step 9b — EmitterState.cs): three cohesive per-scope objects the
    // decomposed collaborator emitters receive explicitly. Instance lifetime = one compilation, exactly like
    // the scattered fields they replace; the per-unit/per-statement mutation discipline is documented on each.
    private readonly DispatchState _dispatchState = new();
    private readonly EcState _ecState = new();
    private readonly CallUnitState _callState = new();

    // ── The per-unit collaborator emitters (Step 9c+): real classes over the fresh EmitContext/renderers,
    //    (re)constructed by NewUnitEmitters at each statement-bearing unit's context creation. During the
    //    incremental extraction they reach not-yet-extracted hub methods through THIS host (the phase doc's
    //    migration-wiring note); 9n replaces the host edges with the UnitEmitters composition root. ──
    private EvaluateEmitter _evaluate = null!;
    private InitializeEmitter _initialize = null!;
    private CorrespondingEmitter _corresponding = null!;
    private AlterSwitchEmitter _alterSwitch = null!;
    private AcceptDisplayEmitter _acceptDisplay = null!;
    private InspectEmitter _inspect = null!;
    private StringEmitter _strings = null!;
    private PtrEmitter _ptr = null!;
    private KeyedIoEmitter _keyedIo = null!;
    private SortEmitter _sort = null!;
    private ReportWriterEmitter _reportWriter = null!;
    private MoveEmitter _move = null!;
    private ArithmeticEmitter _arith = null!;

    /// <summary>(Re)construct the per-unit collaborator emitters over the just-created context/renderers —
    /// called immediately after the <c>_ctx</c>/<c>_num</c>/<c>_cond</c> per-unit re-creation (program classes
    /// and OO class units; interface units emit no statements and need none).</summary>
    private void NewUnitEmitters()
    {
        _evaluate = new EvaluateEmitter(_ctx, _cond, this);
        _initialize = new InitializeEmitter(_ctx, this);
        _corresponding = new CorrespondingEmitter(_ctx, _num, this);
        _alterSwitch = new AlterSwitchEmitter(_ctx);
        _acceptDisplay = new AcceptDisplayEmitter(_ctx);
        _inspect = new InspectEmitter(_ctx, _num, this);
        _strings = new StringEmitter(_ctx, _num, this);
        _ptr = new PtrEmitter(_ctx, _num, _ecState, this);
        _keyedIo = new KeyedIoEmitter(_ctx, _num, _refs, this);
        _sort = new SortEmitter(_ctx, _dispatchState, this);
        _reportWriter = new ReportWriterEmitter(_ctx, _num, _refs, this);
        _move = new MoveEmitter(_ctx, _num, _refs);
        _arith = new ArithmeticEmitter(_ctx, _num, _ecState, this);
    }

    /// <summary>BIND the WHOLE compilation group in <paramref name="tree"/> to an immutable
    /// <see cref="BoundCompilation"/> (multi-unit run-unit binding — interprogram design D3 / SSOT §18 #8), under
    /// the targeted EDITION (<paramref name="edition"/> — bind-time rejection diagnostics accumulate there; the
    /// driver fails the compile when any exist, BEFORE emit). A thin shim over
    /// <see cref="BinderDriver.Bind"/> (rearch PHASE-06 Step 2 — the Binder phase owns the orchestration) with
    /// THIS instance as the <see cref="IOoBindHost"/>: the OO bind bodies physically remain on this class's
    /// partials until P9. <see cref="EmitBound"/> renders C# from the result — codegen never runs on an errored
    /// tree.</summary>
    internal BoundCompilation Bind(Core.CompilationUnitContext tree, EditionContext? edition = null,
        IReadOnlyList<CobolNet.Frontend.Preprocessor.TurnEvent>? turnEvents = null)
        => new BinderDriver().Bind(tree, edition ?? new EditionContext(2023), turnEvents, this);

    /// <summary>Render typed-native C# from an already-bound immutable <see cref="BoundCompilation"/> (the emit
    /// half of the bind/emit split). Call on the SAME instance that hosted <see cref="Bind"/> — the interface
    /// data forests are instance state until P9 (see <c>CallEmitRunUnit</c>).</summary>
    internal string EmitBound(BoundCompilation comp) => CallEmitRunUnit(comp);

    // ── The IOoBindHost seam (P6→P9): BinderDriver reaches the emitter-hosted OO bind bodies through these;
    //    they only mutate binder state (never emit). BeginBind restores the shared-session fields the OO
    //    methods read (_turnState for ConfigureEc, _ooClasses for symbol resolution, the uid-band source). ──

    private BindSession? _bindSession;

    void IOoBindHost.BeginBind(BindSession session)
    {
        _bindSession = session;
        _turnState = session.Turn;
        _ooClasses = session.OoClasses;
    }

    void IOoBindHost.BindInterfaceData(OoInterfaceSymbol iface) => OoBindInterfaceData(iface, _bindSession!.Edition);
    void IOoBindHost.BindClassData(OoClassUnit cls) => OoBindClassData(cls, _bindSession!.Edition);
    void IOoBindHost.BindClassBody(OoClassUnit cls) => OoBindClassBody(cls);
    IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> IOoBindHost.InterfaceData => _ooIfaceData;

    // ── The PC dispatcher (COBOLNET_DESIGN §5) ────────────────────────────────────────────────────────────


    /// <summary>
    /// Emit the single program-counter dispatcher: each paragraph is a <c>case</c> in one <c>Dispatch</c> method;
    /// control is by pc value (GO TO sets pc; fall-through is pc+1; an out-of-line PERFORM is a recursive bounded
    /// <c>Dispatch(start, end)</c>). STOP RUN unwinds all frames via <c>StopRun</c>, caught at <c>Main</c>. This
    /// realizes the legacy's proven return-address / exit-bounded dispatch (DEVLOG 259–260) in idiomatic C#.
    /// </summary>

    private void EmitDispatcher(BoundProgram bound, CodeWriter w)
    {
        int n = bound.Paragraphs.Count;
        // Hooks are needed for the program's OWN declaratives (GR4a) — or, with none, for the outward GR4b walk
        // to a containing program's GLOBAL declaratives (IC233A: the contained unit has no declaratives, yet its
        // failing OPEN must fire the outer's USE GLOBAL).
        _dispatchState.UseDecls = bound.Declaratives is { Count: > 0 } || _dispatchState.OuterGlobalUse;
        w.Line();
        // The dispatcher internals use a `__` prefix — COBOL data-names cannot contain a double underscore — so they
        // never collide with a program's fields (e.g. a COBOL `01 N` and the paragraph count `__N`).
        w.Line($"private const int __N = {n};   // paragraph count");
        _alterSwitch.EmitFields(bound, w);   // the per-altered-paragraph mutable GO TO target fields (control-flow design D4)
        _reportWriter.EmitReportMembers(w);      // per-report engine fields + line compose methods (Verbs/ReportWriterEmitter.cs)
        w.Line();
        using (w.Block("public void __Activate()"))
        {
            // Register this program's SELECTed files at FIRST ACTIVATION of this instance (the IC114A lesson:
            // connectors belong to the program's entry, not the run-unit Main; a fresh instance after CANCEL /
            // an INITIAL activation re-registers — ISO §14.6.2.3.2). Run-unit CloseAll lives in the entry wrapper.
            if (_ctx.Data.Files.Count > 0)
            {
                using (w.Block("if (!__filesRegistered)"))
                {
                    w.Line("__filesRegistered = true;");
                    EmitFileRegistration(w);
                    // Report engines construct WITH the connectors (hazard: the report FD must be registered
                    // before the engine's first write — COBOLNET_REPORT_WRITER_DESIGN §4).
                    _reportWriter.EmitReportConstruction(bound, w);
                }
            }
            // Execution begins at the first NONdeclarative procedure (ISO §14.2.3 GR1) — declarative sections
            // occupy the pcs below EntryPc, entered only via __RunUse or an explicit PERFORM/GO TO (SR4).
            w.Line($"try {{ __Dispatch({bound.EntryPc}, -1); }} catch (ProgramReturn) {{ }}   // GOBACK / called-program EXIT PROGRAM returns to the activator here (ISO §14.9.18 GR2/GR3; §14.9.14 GR3)");
        }
        w.Line();
        // The machinery also emits for a declarative-FREE program whose statements carry enabled EC-I-O checking
        // (__IoCheckEc needs no declaratives to bridge status→EC and apply the fatal default) — gated so an
        // EC-free program's source is unchanged.
        if (_dispatchState.UseDecls || bound.Ec is { HasIoChecked: true }) EmitUseMachinery(bound, w);
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
    private void EmitDispatchMethod(BoundProgram bound, CodeWriter w, string header, int fromPc, int toPc)
    {
        using (w.Block(header))
        {
            w.Line("int __pc = __startPc;");
            using (w.Block("while ((uint)__pc < (uint)__N)"))
            {
                w.Line("bool __atExit = __pc == __exitPc;   // captured before the body overwrites __pc");
                using (w.Block("switch (__pc)"))
                {
                    for (int i = fromPc; i <= toPc; i++)
                    {
                        _dispatchState.CurrentPc = i;
                        using (w.Block($"case {i}:   // {bound.Paragraphs[i].CobolName}"))
                        {
                            if (!EmitParagraphBody(bound.Paragraphs[i], i))
                            {
                                w.Line($"__pc = {i + 1};");
                                w.Line("break;");
                            }
                        }
                    }
                    using (w.Block("default:")) { w.Line("__pc = __N;"); w.Line("break;"); }
                }
                w.Line("if (__atExit && __pc == __exitPc + 1) return __pc;   // a named THRU exit paragraph fell off its end");
            }
            w.Line("return __pc;");
        }
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
        if (decls.Count > 0)
        {
            w.Line($"private readonly bool[] __useActive = new bool[{decls.Count}];   // §14.9.49.4 GR2 re-entrancy guards");
            if (_ecState.Active)
            {
                // The EC-model form: __RunUse RETURNS the declarative's resume action (the dispatch result
                // protocol — CSharpEmitter.Exceptions.cs): a RESUME statement unwinds via ResumeSignal
                // (§14.9.33; the StopRun/ProgramReturn exception-as-control precedent) and __RunUse converts it
                // to the action; normal completion is -1 (§14.6.13.1.2). Emitted ONLY when the group uses the
                // EC model — an EC-free build keeps the void form byte-identical.
                using (w.Block("private int __RunUse(int __id, int __startPc, int __endPc)"))
                {
                    w.Line("if (__useActive[__id]) return -1;   // ISO §14.9.49.4 GR2 — an active USE procedure is not re-invoked");
                    w.Line("__useActive[__id] = true;");
                    w.Line("try { __Dispatch(__startPc, __endPc); }");
                    w.Line("catch (ResumeSignal __rs) { return __rs.TargetPc; }   // RESUME (§14.9.33) — the resume action");
                    w.Line("finally { __useActive[__id] = false; }");
                    w.Line("return -1;   // normal completion (§14.6.13.1.2)");
                }
            }
            else
            {
                using (w.Block("private void __RunUse(int __id, int __startPc, int __endPc)"))
                {
                    w.Line("if (__useActive[__id]) return;   // ISO §14.9.49.4 GR2 — an active USE procedure is not re-invoked");
                    w.Line("__useActive[__id] = true;");
                    w.Line("try { __Dispatch(__startPc, __endPc); } finally { __useActive[__id] = false; }");
                }
            }
            w.Line();
        }
        if (decls.Any(d => d.EcEntries is not null)) EcEmitDispatchSelector(bound, w);
        if (decls.Any(d => d.EoClassCsName is not null)) EcEmitObjDispatchSelector(bound, w);   // F4 (EC-OO)
        if (bound.Ec is { HasIoChecked: true }) EcEmitIoCheckEc(bound, w);
        if (!_dispatchState.UseDecls) return;   // an EC-only program (no F1/F2 declaratives) needs no plain __IoCheck hooks
        using (w.Block("private void __IoCheck(string __f, bool __atEnd, bool __invKey)"))
        {
            w.Line("string __st = CobolFile.Status(__f);");
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
                using (w.Block("switch (CobolFile.OpenModeOf(__f))"))   // open-mode scope (GR3b/GR6b–e)
                {
                    for (int i = 0; i < decls.Count; i++)
                        if (decls[i].ModeIndex is { } m)
                            w.Line($"case {m}: __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); return;");
                }
            // No local declarative qualified — walk OUTWARD to the nearest containing program with a USE GLOBAL
            // declarative (ISO §14.9.49.4 GR4b: "a qualifying declarative with the GLOBAL attribute in the next
            // inclusive directly containing source element", repeated outward). The declarative executes in the
            // DECLARING program's instance — its data (§8.4.6.2) — via the container's __RunGlobalUse.
            if (_dispatchState.OuterGlobalUse)
                w.Line("__outer.__RunGlobalUse(__f);");
        }
        w.Line();
    }

    /// <summary>The declarative hook after a verb's FILE STATUS store (GR6 — after the standard status routine,
    /// BEFORE the statement's phrase branches). A statement with ENABLED EC-I-O checking for this file (>>TURN,
    /// ISO §7.3.25) calls the EC-aware <c>__IoCheckEc</c> variant instead — same F1 behavior plus the §9.1.13.1
    /// status→EC raise, F3 selection and fatal default, returning a RESUME transfer pc when a declarative's
    /// RESUME redirected control (§14.9.33). A no-op for a declarative-free, checking-off program.</summary>
    internal void EmitUseHook(FileModel file, bool atEndHandled = false, bool invalidKeyHandled = false)
    {
        var w = _ctx.Writer;
        if (EcIoMaskFor(file) is not 0 and var mask)
        {
            int id = _ctx.Names.NextEc();
            var (stmt, loc) = EcStmtLoc(_ecState.Info!);
            w.Line($"int __ior{id} = __IoCheckEc({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, "
                + $"{(invalidKeyHandled ? "true" : "false")}, {mask}, {stmt}, {loc});");
            w.Line($"if (__ior{id} >= 0) {{ __pc = __ior{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            return;
        }
        if (!_dispatchState.UseDecls) return;
        w.Line($"__IoCheck({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, {(invalidKeyHandled ? "true" : "false")});");
    }


    /// <summary>Emit a paragraph body SENTENCE by sentence. When the paragraph contains a NEXT SENTENCE anywhere,
    /// each inter-sentence boundary gets a label (`__sentP_K:`) — the §14.9.19 GR6 implicit CONTINUE after the
    /// separator period; NEXT SENTENCE in the LAST sentence is the paragraph fall-through. Returns whether the
    /// body ends by transferring control out of the case.</summary>
    private bool EmitParagraphBody(BoundParagraph para, int pc)
    {
        var w = _ctx.Writer;
        var sentences = para.Sentences;
        bool needLabels = sentences.Any(ContainsNextSentence);
        bool terminated = false;
        for (int k = 0; k < sentences.Count; k++)
        {
            bool last = k == sentences.Count - 1;
            _dispatchState.SentenceEndLabel = needLabels && !last ? $"__sent{pc}_{k}" : null;
            // Unlike intra-sentence dead code, a LABELLED sentence boundary is reachable via NEXT SENTENCE even
            // after an unconditional transfer — so only skip remaining sentences when no labels exist.
            if (terminated && !needLabels) break;
            terminated = EmitStatementList(sentences[k]);
            if (needLabels && !last) w.Line($"__sent{pc}_{k}: ;");
        }
        _dispatchState.SentenceEndLabel = null;
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

    /// <summary>Emit a statement list (a paragraph case, an IF branch, or an inline-PERFORM body), suppressing dead
    /// code after an unconditional transfer; returns whether the list ends by transferring control out of the case.</summary>
    internal bool EmitStatementList(IReadOnlyList<BoundStatement> stmts)
    {
        bool terminated = false;
        foreach (var st in stmts)
        {
            if (terminated) break;   // unreachable after an unconditional GO TO / STOP / EXIT PARAGRAPH
            terminated = EmitStatement(st);
        }
        return terminated;
    }

    /// <summary>Emit one statement; returns true if it unconditionally transfers control out of the paragraph case.
    /// Dispatch is the generated exhaustive <see cref="IBoundStatementVisitor{T}"/> (PHASE-07 Step 6b): every bound
    /// statement leaf has a <c>Visit</c> in <c>CSharpEmitter.Dispatch.cs</c>, so a missing arm is a COMPILE error —
    /// the former 79-arm switch and its loud <c>default</c> are gone.</summary>
    private bool EmitStatement(BoundStatement s) => s.Accept(this);

    /// <summary>Emit <c>GO TO … DEPENDING ON sel</c> (ISO §14.9.20 Format 2): a 1-based selector picks a pc; an
    /// out-of-range value transfers nowhere and falls through to the next statement.</summary>
    private void EmitGoToDepending(BoundGoToDepending d)
    {
        var w = _ctx.Writer;
        int id = _ctx.Names.NextDep();
        w.Line($"int __dep{id} = (int)({_num.AsNum(d.Selector, ReceiverContext.None).Expr});");
        using (w.Block($"switch (__dep{id})"))
            for (int k = 0; k < d.Targets.Count; k++)
                w.Line($"case {k + 1}: __pc = {d.Targets[k]}; break;");
        w.Line($"if (__dep{id} >= 1 && __dep{id} <= {d.Targets.Count}) break;   // in range → transfer (exit the dispatcher switch)");
    }

    // DISPLAY lives on AcceptDisplayEmitter since Step 9c (the ACCEPT/DISPLAY collaborator).

    // ── MOVE (Verbs/MoveEmitter.cs since Step 9g) ── the forwarding shims keep the six collaborator
    //    callers + the Dispatch visit untouched during the incremental extraction; 9n retargets them. ──

    internal void EmitMove(BoundMove m) => _move.Emit(m);
    internal string ConvertSource(BoundOperand source, DataItem target) => _move.ConvertSource(source, target);

    // ── Arithmetic (Verbs/ArithmeticEmitter.cs since Step 9h) ── the service shims keep the five
    //    collaborator callers + the remaining core verbs untouched during the incremental extraction;
    //    9n retargets them. Narrow/BwzFlag are ArithmeticEmitter statics now. ──

    internal void EmitArith(SizeErrorPhrase? sizeErr, Action<bool> emitStores) => _arith.EmitArith(sizeErr, emitStores);
    internal void StoreArith(Place target, NumX value, CobolRounding mode) => _arith.StoreArith(target, value, mode);
    internal ReceiverContext RcvFor(Receiver r, bool inSizeError) => _arith.RcvFor(r, inSizeError);

    // ── IF / PERFORM / SET ─────────────────────────────────────────────────────────────────────────────

    private void EmitIf(BoundIf iff)
    {
        var w = _ctx.Writer;
        using (w.Block($"if ({_cond.Render(iff.Condition)})"))
            EmitStatementList(iff.Then);
        if (iff.Else.Count > 0)
            using (w.Block("else"))
                EmitStatementList(iff.Else);
    }

    private void EmitInlinePerform(BoundInlinePerform p) => EmitPerform(p.Control, () => EmitStatementList(p.Body), inline: true);

    /// <summary>An out-of-line PERFORM is a recursive bounded <c>Dispatch(start, end)</c> over the target pc range
    /// (the C# call stack is the return-address stack, COBOLNET_DESIGN §5.4).</summary>
    private void EmitOutOfLinePerform(BoundOutOfLinePerform p) =>
        EmitPerform(p.Control, () => _ctx.Writer.Line($"{_dispatchState.DispatchName}({p.StartPc}, {p.EndPc});"), inline: false);


    private void EmitPerform(BoundPerformControl control, Action body, bool inline)
    {
        var w = _ctx.Writer;
        switch (control)
        {
            case PerformTimes t:
                // The TIMES count is determined ONCE at the start of the PERFORM (ISO §14.9.28 GR7) — the body
                // modifying the count item must not change the iteration count (NC102A PFM-TEST-F2-6); a zero or
                // negative count runs the body zero times.
                int id = _ctx.Names.NextLoop();
                w.Line($"long __n{id} = {CountExpr(t.Count)};");
                using (w.Block($"for (long __i{id} = 0; __i{id} < __n{id}; __i{id}++)")) body();
                break;
            case PerformUntil u when u.TestAfter:
                using (w.Block("do")) body();
                w.Line($"while (!({_cond.Render(u.Until)}));");
                break;
            case PerformUntil u:
                using (w.Block($"while (!({_cond.Render(u.Until)}))")) body();
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
        var w = _ctx.Writer;
        var levels = v.Levels;
        if (!v.TestAfter)
        {
            foreach (var lv in levels) StoreSetTarget(lv.Var, _num.Render(lv.From, ReceiverContext.None));   // GR13a: left-to-right init
            EmitBefore(0);
            void EmitBefore(int k)
            {
                using (w.Block($"while (!({_cond.Render(levels[k].Until)}))"))
                {
                    if (k == levels.Count - 1)
                    {
                        body();
                        AugmentSetTarget(levels[k].Var, down: false, _num.Render(levels[k].By, ReceiverContext.None));
                    }
                    else
                    {
                        EmitBefore(k + 1);
                        // §14.9.28 GR13e ('85 6.20.4 GR10(d)1): the OUTER variable augments FIRST, THEN the inner
                        // re-initializes from its CURRENT FROM value — `AFTER B FROM A` must see the augmented A
                        // (NC201A PFM-TEST-F4-23: 3+2+1 = 6 iterations, not 3+3+2).
                        AugmentSetTarget(levels[k].Var, down: false, _num.Render(levels[k].By, ReceiverContext.None));
                        StoreSetTarget(levels[k + 1].Var, _num.Render(levels[k + 1].From, ReceiverContext.None));
                    }
                }
            }
        }
        else
        {
            StoreSetTarget(levels[0].Var, _num.Render(levels[0].From, ReceiverContext.None));
            EmitAfter(0);
            void EmitAfter(int k)
            {
                using (w.Block("while (true)"))
                {
                    if (k == levels.Count - 1) body();
                    else
                    {
                        StoreSetTarget(levels[k + 1].Var, _num.Render(levels[k + 1].From, ReceiverContext.None));   // reinit on each entry
                        EmitAfter(k + 1);
                    }
                    w.Line($"if ({_cond.Render(levels[k].Until)}) break;");
                    AugmentSetTarget(levels[k].Var, down: false, _num.Render(levels[k].By, ReceiverContext.None));
                }
            }
        }
    }

    private static string CountExpr(BoundOperand count) => count switch
    {
        BoundNumericLiteral n => n.Text,
        BoundFieldOperand f => f.Place.Read(),
        BoundOperandError e => LoudValue("long", e.Feature),
        _ => "1",
    };


    /// <summary>Serial SEARCH (ISO §14.9.37.4 GR5–8): scan from the index's CURRENT setting; each pass tests
    /// past-end (→ AT END) then the WHEN conditions in order (first true wins); none true → the index (and the
    /// in-step varied item, GR8) increments by 1. Emitted as a LABEL loop — not a C# while — so a GO TO inside a
    /// WHEN/AT END body (`__pc = k; break;`) breaks the DISPATCHER case, not a search loop (transfer-of-control
    /// out of SEARCH per GR5c/6b); a body that runs to completion jumps past the search.</summary>
    private void EmitSearch(BoundSearch s)
    {
        var w = _ctx.Writer;
        int id = _ctx.Names.NextSearch();
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
        var w = _ctx.Writer;
        w.Line($"__search{id}:");
        using (w.Block($"if ({s.IndexField} > {s.DependCount ?? $"{s.Count}L"})"))
        {
            bool terminated = s.AtEnd is { } at && EmitStatementList(at);
            if (!terminated) w.Line($"goto __searchEnd{id};");
        }
        foreach (var when in s.Whens)
            using (w.Block($"if ({_cond.Render(when.Condition)})"))
            {
                if (!EmitStatementList(when.Statements)) w.Line($"goto __searchEnd{id};");
            }
        w.Line($"{s.IndexField} += 1;");
        if (s.AlsoVaried is { } also) AugmentSetTarget(also, down: false, new NumX("1", 0));
        w.Line($"goto __search{id};");
        w.Line($"__searchEnd{id}: ;");
    }

    /// <summary><c>SET … TO value</c> (ISO §14.9.39 Format 1): the sender is evaluated ONCE into an integer temp
    /// (GR2 — "the value of the sending operand is determined once"), then each receiver takes it by kind: an
    /// index-name or index data item receives it unchanged (GR2a/GR2b — in the §3.5 model an index IS its 1-based
    /// occurrence number, so cross-table conversion is the identity); a numeric data item receives the occurrence
    /// number through its own PICTURE store (GR2c). Range checking (EC-RANGE-INDEX) awaits the EC model — COBOL-85
    /// has no exception conditions, so the unchecked store IS the 85 semantics.</summary>
    private void EmitSetTo(BoundSetTo s)
    {
        string tmp = $"__set{_ctx.Names.NextSet()}";
        _ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(_num.Render(s.Value, ReceiverContext.None), 0)});");
        foreach (var t in s.Targets) StoreSetTarget(t, new NumX(tmp, 0));
    }

    /// <summary><c>SET pointer… TO {NULL | pointer}</c> (ISO §14.9.39 Format 4; Phase-4b increment 1): copy
    /// the NULL singleton or the source pointer's carrier into each target in order (GR — a straight handle
    /// copy; a data pointer carries no PICTURE store).</summary>
    private void EmitSetPointer(BoundSetPointer s)
    {
        string src = s.ToNull ? "ManagedPointer.Null"
            : s.Address is { } a ? _ptr.AddressOfText(a)   // ADDRESS OF sender (F7; Phase-4b inc 2)
            : s.Source!.Read();
        foreach (var t in s.Targets)
            _ctx.Writer.Line(t.Write(src) + "   // SET pointer (ISO §14.9.39 Format 4/7)");
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2): the amount is evaluated ONCE
    /// (GR3), then each index is adjusted by it (GR4).</summary>
    private void EmitSetUpDown(BoundSetUpDown s)
    {
        string tmp = $"__set{_ctx.Names.NextSet()}";
        _ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(_num.Render(s.Amount, ReceiverContext.None), 0)});");
        foreach (var t in s.Targets) AugmentSetTarget(t, s.Down, new NumX(tmp, 0));
    }

    /// <summary>SET Format 14 (ISO §14.9.39 GR29; OCCURS DYNAMIC, data-model D9): the amount is evaluated ONCE,
    /// then the owning table's current capacity is set / raised / lowered through the runtime — new occurrences
    /// seeded (§8.5.1.9.5), clamped to the minimum, and EC-FLOW-SEARCH raised if a SEARCH of the same table is
    /// active (GR31). The register carries no storage; the operation is on the <c>CobolDynTable&lt;T&gt;</c> itself.</summary>
    private void EmitSetCapacity(BoundSetCapacity s)
    {
        string tmp = $"__cap{_ctx.Names.NextSet()}";
        _ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(_num.Render(s.Amount, ReceiverContext.None), 0)});");
        string call = s.Kind switch
        {
            SetCapacityKind.To => "SetCapacity",
            SetCapacityKind.UpBy => "CapacityUpBy",
            _ => "CapacityDownBy",
        };
        _ctx.Writer.Line($"{s.TablePath}.{call}({tmp});");
    }

    /// <summary>THE store into a SET-style target (shared by SET TO and PERFORM VARYING initialization): an
    /// index-name field or index data item takes the integer value UNCHANGED (§14.9.39 GR2a/2b — an index IS its
    /// occurrence number); a numeric data item takes it through its own PICTURE store (GR2c).</summary>
    private void StoreSetTarget(BoundSetTarget t, NumX value)
    {
        switch (t)
        {
            case SetIndexTarget ix:
                _ctx.Writer.Line($"{ix.IndexField} = (long)({NumericRenderer.Align(value, 0)});");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                _ctx.Writer.Line(p.Write($"(long)({NumericRenderer.Align(value, 0)})"));
                break;
            case SetPlaceTarget { Place: var p }:
                StoreArith(p, value, CobolRounding.Truncation);
                break;
        }
    }

    /// <summary>THE augment of a SET-style target by ±amount (shared by SET UP/DOWN BY and PERFORM VARYING):
    /// index-name / index data item → plain occurrence-number arithmetic; a numeric data item → an in-place add
    /// through its PICTURE store (legal as a VARYING induction variable, §14.9.28 GR13; a plain SET UP/DOWN on a
    /// numeric item is invalid COBOL — the edition validator will diagnose it, the behavior is the natural add).</summary>
    private void AugmentSetTarget(BoundSetTarget t, bool down, NumX amount)
    {
        string op = down ? "-" : "+";
        switch (t)
        {
            case SetIndexTarget ix:
                _ctx.Writer.Line($"{ix.IndexField} {op}= (long)({NumericRenderer.Align(amount, 0)});");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                _ctx.Writer.Line(p.Write($"(long)({p.Read()} {op} {NumericRenderer.Align(amount, 0)})"));
                break;
            case SetPlaceTarget { Place: var p }:
                StoreArith(p, _num.Combine(_num.FieldNum(p), op, amount, ReceiverContext.None), CobolRounding.Truncation);
                break;
        }
    }

    private void EmitSet(BoundSetConditions set)
    {
        foreach (var (parent, cond) in set.Sets)
        {
            var (low, _) = cond.Values[0];   // SET TO TRUE stores the first VALUE (ISO §14.9.39 Format 5)
            var pic = parent.Item.Pic;
            // A FIGURATIVE-word VALUE (SPACE/ZERO/QUOTE/HIGH-VALUE/LOW-VALUE, incl. ALL forms) fills the
            // conditional variable to its width (§8.3.3.6.4 GR2), not the WORD stored as characters — the
            // fill char is category-aware (national/boolean HIGH/LOW-VALUE = the D-N3 pin). '0' for boolean/
            // numeric ZERO. Only reaches the string categories here (numeric SET handles ZERO natively).
            string? figFill = pic is { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean } ? FigurativeWordFill(low, pic.Category) : null;
            string rhs = figFill is not null
                ? $"new string({figFill}, {pic!.Length})"
                : pic?.Category switch
            {
                // National joins the character store (its 88-VALUE is the prefix-stripped N"…" text);
                // a boolean parent stores its B"…" bits with the §14.6.8.6 zero pad.
                PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                    $"CobolString.Store({CsLiteral(CobolLiteral.Decode(low))}, {pic.Length})",
                PicCategory.Boolean =>
                    $"CobolString.Store({CsLiteral(CobolLiteral.Decode(low))}, {pic.Length}, justifiedRight: false, pad: '0')",
                PicCategory.Numeric =>
                    ArithmeticEmitter.Narrow($"CobolNum.Store({UnscaledAtScale(low, pic.Scale)}, {pic.Scale}, {parent.Item.ProfileName})", parent.Item),
                _ => LoudValue("string", $"SET condition '{cond.Name}' over a group parent"),
            };
            _ctx.Writer.Line(parent.Write(rhs));
        }
    }

    /// <summary>The category-aware C# <c>char</c>-literal a level-88 figurative-word VALUE fills with (SET TO
    /// TRUE, ISO §14.9.39 Format 5 + §8.3.3.6.4 GR2), or null when the operand is not a bare figurative word
    /// (a quoted / N"…" / B"…" / numeric literal takes the store path). Tolerates the ALL-prefixed spelling.</summary>
    private string? FigurativeWordFill(string raw, PicCategory cat)
    {
        string w = raw.Trim();
        if (w.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && w.Length > 3
            && (char.IsWhiteSpace(w[3]) || char.IsLetter(w[3])))
            w = w[3..].TrimStart();
        return FigurativeConstants.KindOf(w, includeNull: true) is { } k
            ? FigurativeConstants.Fill(k, _ctx.Data.Collating, cat) : null;   // the ONE service (P7 Step 4)
    }

    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ─────────────────────────────────────────────────────────────

    /// <summary>Emit the file registry init + one <c>Register</c> per SELECTed sequential file (at <c>Main</c> start).
    /// The ASSIGN target becomes a host path at run time (<c>CobolFile.ResolveHostPath</c>); the record width is the
    /// FD record-area image width. A non-sequential file is skipped here (its verbs emit loud guards).</summary>
    private void EmitFileRegistration(CodeWriter w)
    {
        foreach (var file in _ctx.Data.Files)
        {
            if (file.IsSortMerge) continue;   // an SD is the in-memory sort store (ISO §13.4.6) — never a host file
            if (!file.IsSequential) { _keyedIo.EmitRegistration(w, file); EmitSharingRegistration(w, file); continue; }   // relative/indexed connectors
            if (file.Records.Count == 0)
            {
                // A REPORT FILE legally has no record description (ISO §9.1.22 / §13.18.46) — it MUST still
                // register, or its OPEN falls through to the keyed registries and the report engine's writes go
                // into a void (the silent-OPEN-no-op hazard, COBOLNET_REPORT_WRITER_DESIGN §7). The record
                // width is the widest hosted report's line width.
                if (file.ReportNames.Count > 0)
                {
                    int width = Math.Max(1, _ctx.Data.Reports
                        .Where(r => ReferenceEquals(r.File, file))
                        .Select(r => r.LineWidth).DefaultIfEmpty(1).Max());
                    w.Line($"CobolFile.Register({FileKeyExpr(file)}, {CsLiteral(file.AssignTarget)}, " +
                           $"{width}, false, {(file.Optional ? "true" : "false")});");
                }
                continue;
            }
            bool lineSeq = file.Organization == FileOrganization.LineSequential;
            // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) — the connector
            // length-frames its records and enforces the GR14 '44' boundary checks.
            string vary = file.Varying is not null ? $", {file.VaryMin}, {file.VaryMax}" : "";
            w.Line($"CobolFile.Register({FileKeyExpr(file)}, {CsLiteral(file.AssignTarget)}, " +
                   $"{file.RecordWidth}, {(lineSeq ? "true" : "false")}, {(file.Optional ? "true" : "false")}{vary});");
            // A LINAGE file registers its logical-page evaluator (ISO §13.18.34 GR6): ONE closure for both the
            // literal (GR6a — a constant lambda) and data-name (GR6b — the connector re-reads at OPEN OUTPUT /
            // ADVANCING PAGE / page overflow) forms. The lambda READS the program fields at call time — it is
            // emitted in __Activate (an instance method), so they are in scope and never captured by value.
            if (file.Linage is { } lin)
                w.Line($"CobolFile.SetLinage({FileKeyExpr(file)}, () => ({LinageOpExpr(lin.Body)}, "
                       + $"{LinageOpExpr(lin.Footing)}, {LinageOpExpr(lin.Top)}, {LinageOpExpr(lin.Bottom)}));");
            EmitSharingRegistration(w, file);
        }
    }

    /// <summary>Emit the <c>CobolFile.RegisterSharing</c> call for a file that declares a SHARING and/or LOCK MODE
    /// clause (Phase 4d M2-FILE-1) — this marks the connector "sharing-active" so its OPEN routes through the
    /// physical-file registry (Table-19 → 61) and its READs through record-lock governance. Files without either
    /// clause emit nothing and keep the legacy exclusive path byte-for-byte (ISO §14.9.27 GR23 implementor
    /// default). A LOCK-MODE-only file has no sharing conflict posture, so its sharing maps to the neutral
    /// ALL OTHER (record locking observable, no spurious 61).</summary>
    private void EmitSharingRegistration(CodeWriter w, FileModel file)
    {
        if (file.Sharing == SharingMode.None && file.LockMode is null) return;
        string sharing = file.Sharing switch
        {
            SharingMode.NoOther => "FileSharing.NoOther",
            SharingMode.ReadOnly => "FileSharing.ReadOnly",
            _ => "FileSharing.AllOther",   // AllOther, or None (LOCK-MODE-only) → neutral default
        };
        string lockMode = (file.LockMode?.Kind ?? LockKind.None) switch
        {
            LockKind.Manual => "FileLockMode.Manual",
            LockKind.Automatic => "FileLockMode.Automatic",
            _ => "FileLockMode.None",
        };
        bool multiple = file.LockMode?.Multiple ?? false;
        w.Line($"CobolFile.RegisterSharing({FileKeyExpr(file)}, {sharing}, {lockMode}, "
            + $"{(multiple ? "true" : "false")});");
    }

    /// <summary>The C# <c>int</c> expression for one LINAGE clause operand (ISO §13.18.34 GR6): the fixed literal
    /// (GR6a), the data item's current value (GR6b — scale 0 by SR2's elementary-unsigned-integer rule, with a
    /// defensive rescale), or <c>0</c> for an absent TOP/BOTTOM/FOOTING phrase (GR1 — margins zero; footing 0 =
    /// no footing area). A declared data-name that does not resolve to storage fails loud (§1.4).</summary>
    private string LinageOpExpr(LinageOperand? op)
    {
        if (op is null) return "0";
        if (op.Literal is { } lit) return lit.ToString();
        if (op.Item is { } item && _refs.ResolveItem(item) is { } p)
        {
            var nx = _num.FieldNum(p);
            return nx.Scale == 0 ? $"(int)({nx.Expr})"
                : $"(int)CobolNum.Rescale({nx.Expr}, {nx.Scale}, 0, CobolRounding.Truncation)";
        }
        return LoudValue("int", $"LINAGE operand '{op.DataName}' is not resolvable to storage (ISO §13.18.34 SR2)");
    }

    private void EmitOpen(BoundOpen o)
    {
        var w = _ctx.Writer;
        // A SHARING override or RETRY phrase on the OPEN (ISO §14.9.27, COBOL-2002) routes every file in the
        // statement's mode group through the sharing-aware facade; a plain OPEN keeps the direct entry points.
        bool shared = o.SharingOverride is not null || o.Retry is not null;
        foreach (var (file, mode, unsupported) in o.Files)
        {
            if (unsupported is { } u) { w.Line(LoudStmt(u)); continue; }
            if (shared)
            {
                string modeEnum = mode switch
                {
                    BoundOpenMode.Output => "FileOpenMode.Output",
                    BoundOpenMode.Extend => "FileOpenMode.Extend",
                    BoundOpenMode.IO => "FileOpenMode.IO",
                    _ => "FileOpenMode.Input",
                };
                var (retryKind, retryAmount) = RenderRetry(o.Retry);
                string shHas = o.SharingOverride is not null ? "true" : "false";
                string shVal = o.SharingOverride is { } sm ? RuntimeSharing(sm) : "FileSharing.AllOther";
                w.Line($"CobolFile.OpenShared({FileKeyExpr(file)}, {modeEnum}, {shHas}, {shVal}, "
                    + $"{retryKind}, {retryAmount});");
            }
            else
            {
                string method = mode switch
                {
                    BoundOpenMode.Output => "OpenOutput",
                    BoundOpenMode.Extend => "OpenExtend",
                    BoundOpenMode.IO => "OpenIO",
                    _ => "OpenInput",
                };
                w.Line($"CobolFile.{method}({FileKeyExpr(file)});");
            }
            EmitStoreFileStatus(file);
            EmitUseHook(file);   // a failed OPEN reaches a mode-scoped USE via the being-opened mode (GR6b)
        }
    }

    /// <summary>Map a bound SHARING mode to the runtime <c>FileSharing</c> enum member (Phase 4d).</summary>
    private static string RuntimeSharing(SharingMode m) => m switch
    {
        SharingMode.NoOther => "FileSharing.NoOther",
        SharingMode.ReadOnly => "FileSharing.ReadOnly",
        _ => "FileSharing.AllOther",
    };

    /// <summary>Render a bound RETRY phrase (ISO §14.7.9) to the runtime <c>(FileRetryKind, int amount)</c> pair —
    /// the amount is the n-TIMES count (rendered as a C# int); SECONDS/FOREVER pass 0 (their amount is a
    /// single-run-unit no-op, deadlock-bailing to status 52).</summary>
    internal (string Kind, string Amount) RenderRetry(RetrySpec? retry) => retry switch
    {
        null => ("FileRetryKind.None", "0"),
        { Kind: RetryKind.Forever } => ("FileRetryKind.Forever", "0"),
        { Kind: RetryKind.Seconds } => ("FileRetryKind.Seconds", RetryAmount(retry.Amount)),
        _ => ("FileRetryKind.Times", RetryAmount(retry.Amount)),
    };

    private string RetryAmount(BoundExpr? amount) =>
        amount is null ? "0" : $"(int)({NumericRenderer.Align(_num.Render(amount, ReceiverContext.None), 0)})";

    /// <summary>Map a bound record-lock phrase to the runtime <c>FileRecordLock</c> enum member (Phase 4d).</summary>
    internal static string RuntimeRecordLock(BoundRecordLock l) => l switch
    {
        BoundRecordLock.WithLock => "FileRecordLock.WithLock",
        BoundRecordLock.WithNoLock => "FileRecordLock.WithNoLock",
        BoundRecordLock.IgnoringLock => "FileRecordLock.Ignoring",
        _ => "FileRecordLock.None",
    };

    private void EmitClose(BoundClose c)
    {
        var w = _ctx.Writer;
        foreach (var (file, kind) in c.Files)
        {
            string method = kind switch
            {
                BoundCloseKind.WithLock => "CloseWithLock",
                BoundCloseKind.ReelUnit => "CloseReelUnit",
                _ => "Close",
            };
            w.Line($"CobolFile.{method}({FileKeyExpr(file)});");
            EmitStoreFileStatus(file);
            EmitUseHook(file);
        }
    }

    /// <summary>UNLOCK file [RECORD[S]] (ISO §14.9.47, COBOL-2002): release the connector's record locks and set
    /// the I-O status (00, or 42 if not open). The two hooks let a USE declarative see the status like any I-O.</summary>
    private void EmitUnlock(BoundUnlock ul)
    {
        var w = _ctx.Writer;
        w.Line($"CobolFile.Unlock({FileKeyExpr(ul.File)}, {(ul.Records ? "true" : "false")});");
        EmitStoreFileStatus(ul.File);
        EmitUseHook(ul.File);
    }

    /// <summary>WRITE record [FROM x] [ADVANCING …] (ISO §14.9.46): a FROM operand first MOVEs into the record area,
    /// then the record's character image is written (plain, or with print-control advancing).</summary>
    private void EmitWrite(BoundWrite wr)
    {
        var w = _ctx.Writer;
        if (wr.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        if (wr.From is { } from) EmitMove(new BoundMove(from, [wr.Record]));
        string name = FileKeyExpr(wr.File);
        string image = OperandText.AsString(new BoundFieldOperand(wr.Record));
        if (wr.Advancing is { } adv)
        {
            string lines = adv.Page ? "-1" : LinesExpr(adv.Lines!);
            w.Line($"CobolFile.WriteAdvancing({name}, {image}, {lines}, {(adv.Before ? "true" : "false")});");
        }
        else
            w.Line(VaryingLengthArg(wr.File) is { } len
                ? $"CobolFile.Write({name}, {image}, {len});"
                : $"CobolFile.Write({name}, {image});");
        EmitStoreFileStatus(wr.File);
        EmitUseHook(wr.File);
        // END-OF-PAGE branches (ISO §14.9.51 GR27b/GR28): an end-of-page WRITE is SUCCESSFUL — the branch runs
        // after the status store (status 00, so no USE declarative competes). The flag is read in the `if`
        // HEADER before either body runs: a branch body may WRITE the same file again (SQ208M's footing loop
        // inside the AT phrase), which clobbers the connector's per-write flag.
        if (wr.AtEop is not null || wr.NotAtEop is not null)
        {
            using (w.Block($"if (CobolFile.EndOfPage({name}))"))
            {
                if (wr.AtEop is { } at) EmitStatementList(at);
            }
            if (wr.NotAtEop is { } not)
                using (w.Block("else"))
                    EmitStatementList(not);
        }
    }

    /// <summary>The record-length argument for a WRITE/REWRITE on a RECORD VARYING … DEPENDING file (ISO
    /// §13.18.43 GR13a — the DEPENDING item's content names the record length), or null when the statement
    /// writes the record's own size (GR13b/c — on a varying file the runtime takes the image's length; on a
    /// fixed file it pads to the record width).</summary>
    internal string? VaryingLengthArg(FileModel file) =>
        file is { Varying.DependingName: not null, VaryingDependingItem: { } d } && _refs.ResolveItem(d) is { } dep
            ? $"(int)CobolTable.Occ({dep.Read()})" : null;

    /// <summary>After a SUCCESSFUL read of a RECORD VARYING … DEPENDING file, store the just-read record's length
    /// into the DEPENDING item (ISO §13.18.43 GR15; GR12 — an unsuccessful READ leaves it unchanged, so the call
    /// site sits inside the success branch).</summary>
    internal void EmitReadLengthStore(FileModel file)
    {
        if (file is not { Varying.DependingName: not null, VaryingDependingItem: { } d }
            || _refs.ResolveItem(d) is not { } dep) return;
        StoreArith(dep, new NumX($"CobolFile.LastReadLength({FileKeyExpr(file)})", 0), CobolRounding.Truncation);
    }

    /// <summary>READ file [INTO x] [AT END …][NOT AT END …] (ISO §14.9.30): on success the record image is
    /// distributed into the FD record area (and, with INTO, MOVEd to the target); the AT END / NOT AT END imperative
    /// branches on the at-end condition. After an UNSUCCESSFUL read the record area's content is spec-UNDEFINED
    /// (§14.9.30 GR18 "unless otherwise specified…"); COBOL.NET's documented refinement is that the area is
    /// UNCHANGED — the store sits in the success branch only — extending the spec's own rule for every other
    /// unsuccessful I-O verb (REWRITE GR14 / WRITE GR15 / DELETE GR8 / START GR2 all say "unaffected"). The
    /// legacy's LOW-VALUE fill there was a byte-engine artifact (ST146A's golden is re-baselined over it,
    /// DEVLOG 570).</summary>
    private void EmitRead(BoundRead rd)
    {
        var w = _ctx.Writer;
        if (rd.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        string name = FileKeyExpr(rd.File);
        string tmp = $"__rd{_ctx.Names.NextRead()}";
        // The read record is made available in the WHOLE record area — store through the LARGEST record's view
        // (FileModel.AreaRecord, ISO §13.4.2); a shorter Records[0] window would truncate the splice (ST111A).
        Place? area = rd.File.AreaRecord is { } ar ? _refs.ResolveItem(ar) : null;
        using (w.Block($"if (CobolFile.Read({name}, out var {tmp}))"))
        {
            if (area is not null) EmitImageInto(area, tmp);
            EmitReadLengthStore(rd.File);   // §13.18.43 GR15 — the just-read length into DEPENDING
            EmitStoreFileStatus(rd.File);
            // READ … INTO is READ then MOVE the record area to the target (ISO §14.9.30 GR — group move).
            // §13.18.43 GR16a (a varying sender is the first DEPENDING-many bytes) is observationally identical
            // here: Read space-fills the area beyond the record, and the implicit MOVE of the category-
            // alphanumeric area space-fills the receiver the same way.
            if (rd.Into is { } into && area is not null)
                EmitMove(new BoundMove(new BoundFieldOperand(area), [into]));
            if (rd.NotAtEnd is { } not) EmitStatementList(not);
        }
        using (w.Block("else"))
        {
            EmitStoreFileStatus(rd.File);
            EmitUseHook(rd.File, atEndHandled: rd.AtEnd is not null);
            // The AT END imperative runs ONLY for the at-end status family (ISO 14.9.30 GR24c/d + 9.1.13.1 -
            // a 3x/4x failure is NOT an at-end condition; it reaches a USE declarative instead).
            if (rd.AtEnd is { } at)
                using (w.Block($"if (CobolFile.Status({name})[0] == '1')"))
                    EmitStatementList(at);
        }
    }

    private void EmitRewrite(BoundRewrite rw)
    {
        var w = _ctx.Writer;
        if (rw.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        if (rw.From is { } from) EmitMove(new BoundMove(from, [rw.Record]));
        string image = OperandText.AsString(new BoundFieldOperand(rw.Record));
        w.Line(VaryingLengthArg(rw.File) is { } len
            ? $"CobolFile.Rewrite({FileKeyExpr(rw.File)}, {image}, {len});"
            : $"CobolFile.Rewrite({FileKeyExpr(rw.File)}, {image});");
        EmitStoreFileStatus(rw.File);
        EmitUseHook(rw.File);
    }

    /// <summary>Store a read record image into the FD record area: a character-image group distributes via FromImage;
    /// an elementary / view record takes the image padded to its width.</summary>
    internal void EmitImageInto(Place record, string imageExpr)
    {
        var w = _ctx.Writer;
        // A character-image group record distributes the read image into its typed leaves via the generated FromImage.
        // A Tier-B view record (a multi-01 FD whose shared area is a synthesized REDEFINES) has no struct to call
        // FromImage on — its Read() is a string window — so splice the padded image into its backing via Write, as for
        // an elementary record. (Mirrors EmitGroupImage's RedefViewPlace handling.)
        if (record is not RedefViewPlace && record.Item.IsGroup)
        {
            // A mixed-usage record area distributes through the same generated FromImage — its BINARY/PACKED
            // leaves decode their zoned digit slices (the §13.18.60 USAGE GR4 implementor representation,
            // COBOLNET_DESIGN §14.4/§8.2: the record codec IS the AsImage/FromImage pair). Only a record with a
            // float/COMP-5/INDEX leaf stays the loud Tier-C island (§1.4) rather than emit a string-into-struct
            // assignment that fails the backend compile (the old ST133A/ST134A/SQ203A CS0029 class).
            if (!record.Item.IsImageCapable)
            {
                w.Line(LoudStmt($"record area '{record.Item.CobolName}' contains float/COMP-5/INDEX leaves — the "
                    + "Tier-C byte island (COBOLNET_DESIGN §4.2), deferred"));
                return;
            }
            w.Line($"{record.Read()}.FromImage(CobolString.Store({imageExpr}, {record.Item.ImageWidth}));");
            return;
        }
        w.Line(record.Write($"CobolString.Store({imageExpr}, {record.Item.Pic?.Length ?? record.Item.ImageWidth})"));
    }

    /// <summary>After an I/O verb, store the file's two-character I-O status into its FILE STATUS item (ISO §9.1.13),
    /// when the SELECT declared one.</summary>
    internal void EmitStoreFileStatus(FileModel file)
    {
        // ISO §12.4.5.8 / §9.1.13.1 — the two-character status is stored into the FILE STATUS item as part of
        // the I/O statement's execution, BEFORE any exception processing.
        if (file.FileStatusName is null) return;   // no FILE STATUS clause — nothing to store
        // An INHERITED GLOBAL file stores into the OWNER's status item through the __outer chain
        // (§12.4.5.8.4 GR1 NOTE 1 — the item is updated by contained-program references to the global
        // file-name even though it is a LOCAL name of the owner; map built per unit in CallEmitProgramClass).
        Place? place = _callState.InheritedStatusPlace.TryGetValue(file, out var inherited)
            ? inherited
            : file.FileStatusItem is { } own ? _refs.ResolveItem(own) : null;
        if (file.FileStatusItem is not { } item || place is null)
        {
            // §1.4 loud-guard doctrine: a DECLARED FILE STATUS name that did not resolve is never silent.
            _ctx.Writer.Line(LoudStmt($"FILE STATUS item '{file.FileStatusName}' is not resolvable to storage (ISO §12.4.5.8)"));
            return;
        }
        string status = $"CobolString.Store(CobolFile.Status({FileKeyExpr(file)}), {(item.Pic?.Length ?? item.ImageWidth)})";
        if (item.IsGroup && place is not RedefViewPlace)
        {
            // Same image-capability rule as every other group receiver (COBOLNET_DESIGN §14.4): a mixed-usage
            // status group distributes via FromImage; only a float/COMP-5/INDEX leaf stays loud (§1.4).
            if (!item.IsImageCapable)
            {
                _ctx.Writer.Line(LoudStmt($"FILE STATUS into group '{item.CobolName}' with a float/COMP-5/INDEX leaf (Tier-C byte island, deferred)"));
                return;
            }
            // A GROUP status item fills without conversion through the image facility (§14.9.25.4 GR4 — the
            // CCVS shape `01 SQ-FS2-STATUS. 03 KEY-1 PIC X. 03 KEY-2 PIC X.`); a struct field cannot take the
            // raw string write.
            _ctx.Writer.Line($"{place.Read()}.FromImage({status});");
            return;
        }
        _ctx.Writer.Line(place.Write(status));
    }

    /// <summary>The C# <c>int</c> expression for an ADVANCING line count (a literal or a numeric data-name).</summary>
    private string LinesExpr(BoundOperand lines) => lines switch
    {
        BoundNumericLiteral n => $"(int)({n.Text})",
        BoundFieldOperand f => $"(int)({_num.AsNum(f, ReceiverContext.None).Expr})",
        _ => "1",
    };
}
