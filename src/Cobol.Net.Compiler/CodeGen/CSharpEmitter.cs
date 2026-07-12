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
    private SetEmitter _set = null!;
    private ControlFlowEmitter _controlFlow = null!;
    private SequentialIoEmitter _seqIo = null!;
    private EcEmitter _ecEmit = null!;

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
        _set = new SetEmitter(_ctx, _num, _arith, _ptr);
        _controlFlow = new ControlFlowEmitter(_ctx, _num, _cond, _dispatchState, _set, this);
        _seqIo = new SequentialIoEmitter(_ctx, _num, _refs, _dispatchState, _ecState, _callState, _keyedIo, _arith, this);
        _ecEmit = new EcEmitter(_ctx, _ecState, _dispatchState, this);
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
        if (decls.Any(d => d.EcEntries is not null)) _ecEmit.EmitDispatchSelector(bound, w);
        if (decls.Any(d => d.EoClassCsName is not null)) _ecEmit.EmitObjDispatchSelector(bound, w);   // F4 (EC-OO)
        if (bound.Ec is { HasIoChecked: true }) _ecEmit.EmitIoCheckEc(bound, w);
        if (!_dispatchState.UseDecls) return;   // an EC-only program (no F1/F2 declaratives) needs no plain __IoCheck hooks
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
            if (_dispatchState.OuterGlobalUse)
                w.Line("__outer.__RunGlobalUse(__f);");
        }
        w.Line();
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
    internal bool EmitStatement(BoundStatement s) => s.Accept(this);

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

    // ── Control flow / SET / sequential file-I/O live on Verbs/{ControlFlow,Set,SequentialIo}Emitter.cs
    //    since Step 9i/9j; the service shims below keep the collaborator callers + the dispatcher core
    //    untouched during the incremental extraction (9n retargets them). ──

    internal void EmitUseHook(FileModel file, bool atEndHandled = false, bool invalidKeyHandled = false)
        => _seqIo.EmitUseHook(file, atEndHandled, invalidKeyHandled);
    internal void EmitStoreFileStatus(FileModel file) => _seqIo.EmitStoreFileStatus(file);
    internal void EmitImageInto(Place record, string imageExpr) => _seqIo.EmitImageInto(record, imageExpr);
    internal void EmitReadLengthStore(FileModel file) => _seqIo.EmitReadLengthStore(file);
    internal string? VaryingLengthArg(FileModel file) => _seqIo.VaryingLengthArg(file);
    internal (string Kind, string Amount) RenderRetry(RetrySpec? retry) => _seqIo.RenderRetry(retry);
    internal void EmitFileRegistration(CodeWriter w) => _seqIo.EmitFileRegistration(w);

}
