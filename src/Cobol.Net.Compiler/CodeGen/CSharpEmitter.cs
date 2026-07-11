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

    private int _currentPc;     // the paragraph index being emitted (for EXIT PARAGRAPH / fall-through)
    private string? _sizeErrVar; // the current __sizeErr flag while emitting a checked arithmetic body (else null)

    /// <summary>
    /// Emit the single program-counter dispatcher: each paragraph is a <c>case</c> in one <c>Dispatch</c> method;
    /// control is by pc value (GO TO sets pc; fall-through is pc+1; an out-of-line PERFORM is a recursive bounded
    /// <c>Dispatch(start, end)</c>). STOP RUN unwinds all frames via <c>StopRun</c>, caught at <c>Main</c>. This
    /// realizes the legacy's proven return-address / exit-bounded dispatch (DEVLOG 259–260) in idiomatic C#.
    /// </summary>
    private bool _useDecls;   // the program being emitted declares USE procedures (drives the __IoCheck hooks)
    private bool _callOuterGlobalUse;   // a CONTAINING program has USE … GLOBAL declaratives (ISO §14.9.49.4 GR4b — the child's __IoCheck walks outward; set by CallEmitProgramClass)

    private void EmitDispatcher(BoundProgram bound, CodeWriter w)
    {
        int n = bound.Paragraphs.Count;
        // Hooks are needed for the program's OWN declaratives (GR4a) — or, with none, for the outward GR4b walk
        // to a containing program's GLOBAL declaratives (IC233A: the contained unit has no declaratives, yet its
        // failing OPEN must fire the outer's USE GLOBAL).
        _useDecls = bound.Declaratives is { Count: > 0 } || _callOuterGlobalUse;
        w.Line();
        // The dispatcher internals use a `__` prefix — COBOL data-names cannot contain a double underscore — so they
        // never collide with a program's fields (e.g. a COBOL `01 N` and the paragraph count `__N`).
        w.Line($"private const int __N = {n};   // paragraph count");
        AlterEmitFields(bound, w);   // the per-altered-paragraph mutable GO TO target fields (control-flow design D4)
        RwEmitReportMembers(w);      // per-report engine fields + line compose methods (CSharpEmitter.ReportWriter.cs)
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
                    RwEmitReportConstruction(bound, w);
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
        if (_useDecls || bound.Ec is { HasIoChecked: true }) EmitUseMachinery(bound, w);
        EmitDispatchMethod(bound, w, "private int __Dispatch(int __startPc, int __exitPc)",
            0, bound.Paragraphs.Count - 1);
    }

    /// <summary>The dispatch-method NAME the statement emitters call for a bounded range (out-of-line PERFORM,
    /// SORT/MERGE procedures): <c>__Dispatch</c> for a program's instance method; <c>__MDispatch</c> while a
    /// COBOL-class METHOD body emits — its dispatcher is a LOCAL FUNCTION of the emitted method, so the
    /// method's LINKAGE/LOCAL-STORAGE locals are capturable (OO deep-dive D3/D6, slice 2).</summary>
    private string _dispatchName = "__Dispatch";

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
                        _currentPc = i;
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
            if (_ecActive)
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
        if (!_useDecls) return;   // an EC-only program (no F1/F2 declaratives) needs no plain __IoCheck hooks
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
            if (_callOuterGlobalUse)
                w.Line("__outer.__RunGlobalUse(__f);");
        }
        w.Line();
    }

    /// <summary>The declarative hook after a verb's FILE STATUS store (GR6 — after the standard status routine,
    /// BEFORE the statement's phrase branches). A statement with ENABLED EC-I-O checking for this file (>>TURN,
    /// ISO §7.3.25) calls the EC-aware <c>__IoCheckEc</c> variant instead — same F1 behavior plus the §9.1.13.1
    /// status→EC raise, F3 selection and fatal default, returning a RESUME transfer pc when a declarative's
    /// RESUME redirected control (§14.9.33). A no-op for a declarative-free, checking-off program.</summary>
    private void EmitUseHook(FileModel file, bool atEndHandled = false, bool invalidKeyHandled = false)
    {
        var w = _ctx.Writer;
        if (EcIoMaskFor(file) is not 0 and var mask)
        {
            int id = _ctx.Names.NextEc();
            var (stmt, loc) = EcStmtLoc(_ecInfo!);
            w.Line($"int __ior{id} = __IoCheckEc({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, "
                + $"{(invalidKeyHandled ? "true" : "false")}, {mask}, {stmt}, {loc});");
            w.Line($"if (__ior{id} >= 0) {{ __pc = __ior{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            return;
        }
        if (!_useDecls) return;
        w.Line($"__IoCheck({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, {(invalidKeyHandled ? "true" : "false")});");
    }

    private string? _sentenceEndLabel;   // the goto target NEXT SENTENCE jumps to (null in the last sentence)

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
            _sentenceEndLabel = needLabels && !last ? $"__sent{pc}_{k}" : null;
            // Unlike intra-sentence dead code, a LABELLED sentence boundary is reachable via NEXT SENTENCE even
            // after an unconditional transfer — so only skip remaining sentences when no labels exist.
            if (terminated && !needLabels) break;
            terminated = EmitStatementList(sentences[k]);
            if (needLabels && !last) w.Line($"__sent{pc}_{k}: ;");
        }
        _sentenceEndLabel = null;
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
    private bool EmitStatementList(IReadOnlyList<BoundStatement> stmts)
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

    private void EmitDisplay(BoundDisplay d)
    {
        // DISPLAY shows the sign-aware image (deSign defaults false — the operational sign is part of the displayed
        // zoned representation, unlike a move to an alphanumeric receiver).
        var parts = d.Operands.Select(o => OperandText.AsString(o)).ToList();
        string image = parts.Count == 0 ? "\"\"" : string.Join(" + ", parts);
        _ctx.Writer.Line(d.NoAdvancing ? $"System.Console.Write({image});" : $"System.Console.WriteLine({image});");
    }

    /// <summary>Render one MOVE — a PURE per-kind renderer since P7 Step 7: the dispatch travels on the node
    /// (<see cref="BoundMove.Kinds"/>, classified once by <see cref="MoveClassifier"/> at construction — ISO
    /// §14.9.25.4 GR4's elementary-vs-group decision incl. the sender side, NC105A MOVE-TEST-F1-16/-17/-20/-36/-38);
    /// the emitter re-derives nothing.</summary>
    private void EmitMove(BoundMove m)
    {
        for (int i = 0; i < m.Targets.Count; i++)
        {
            var target = m.Targets[i];
            switch (m.Kinds[i])
            {
                case MoveKind.RefModSlice:
                    // The slice takes the source's characters (SpliceInto left-justifies, space-fills, and
                    // truncates to the slice length), so pass the raw image, not a full-width store. EXCEPT a
                    // figurative source, which fills EVERY position of the slice (ISO §8.3.3.6.4 GR2 — repeated
                    // to the associated fixed-length item; §8.4.3.3 GR5/GR6 make the slice a unique fixed-length
                    // item); the fill char is category-aware (national/boolean = the D-N3 pin, not the PCS extreme).
                    var rmp = (RefModPlace)target;
                    _ctx.Writer.Line(m.Source is BoundFigurative fig
                        ? rmp.WriteFill(FigurativeConstants.Fill(fig.Kind, _ctx.Data.Collating, rmp.Inner.Item.Pic?.Category))
                        : rmp.Write(OperandText.AsString(m.Source)));
                    break;
                case MoveKind.Group:
                    EmitGroupMove(target, m.Source);
                    break;
                case MoveKind.GroupToElementary:
                    EmitGroupToElementaryMove(target, m.Source);
                    break;
                case MoveKind.FigurativeToNumericImage:
                    EmitFigurativeToNumericImage(target, m.Source);
                    break;
                case MoveKind.Convert:
                    _ctx.Writer.Line(target.Write(ConvertSource(m.Source, target.Item)));
                    break;
            }
        }
    }

    /// <summary>MOVE of an alphanumeric figurative constant (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE) or an ALL
    /// "literal" containing a non-digit into an ELEMENTARY NUMERIC receiver — the PRE-REMOVAL semantics of a
    /// construct ISO/IEC 1989:2023 REMOVED (§14.9.25.3 SR5; Annex E.2 item 1 bullet 1 — permitted through 2014).
    /// The binder's "move-alphanumeric-figurative-removed-2023" gate carries the 0902 diagnostics; this emit path
    /// exists regardless, reachable at --std 85/2002/2014 and at 2023 --permissive. Provisional (ratified decision
    /// 1 — the legacy oracle is the interim authority for pre-2023 semantics of removed constructs): the fill
    /// CHARACTERS are deposited as the receiver's character image, repeated to its width (§8.3.3.6.4 GR2), exactly
    /// the legacy byte engine's fill — MOVE QUOTE TO PIC 9(3) leaves three quotation marks, IS NUMERIC is then
    /// false, and a later numeric read decodes deterministically (§14.6.13.2: a non-digit contributes no digit).
    /// The binder flagged an eligible numeric-DISPLAY receiver <see cref="DataItem.StoreAsImage"/> (REUSING the
    /// §14.9 MOVE GR4 whole-group image substrate — never a parallel mechanism), so the store is a plain image
    /// write; a Tier-B REDEFINES window / NumericImagePlace writes its character image directly. Only a
    /// non-DISPLAY numeric receiver (BINARY / PACKED / COMP-5 / float — no character image in the typed-native
    /// model) remains a NARROW loud guard (§1.4). Eligibility is the NODE's <see cref="MoveKind"/> (classified
    /// once — figurative ZERO and digit-only ALL classify as Convert, VALUE moves not fills).</summary>
    private void EmitFigurativeToNumericImage(Place target, BoundOperand source)
    {
        // The KIND already decided eligibility (MoveClassifier — numeric receiver + S/Q/H/L figurative or
        // non-digit ALL); an unmatched shape here is a classifier/renderer drift bug, not a fallthrough.
        string image = source switch
        {
            BoundFigurative { Kind: 'S' or 'Q' or 'H' or 'L' } f =>
                $"new string({FigurativeConstants.Fill(f.Kind, _ctx.Data.Collating)}, {target.Item.ImageWidth})",
            BoundAllLiteral { IsDigitOnly: false } a =>
                CsLiteral(EmitText.RepeatToWidth(a.Literal, target.Item.ImageWidth)),
            _ => throw new InvalidOperationException(
                $"MoveKind.FigurativeToNumericImage with a non-fill source ({source.GetType().Name}) — "
                + "MoveClassifier and this renderer have drifted"),
        };
        _ctx.Writer.Line(target.Item.StoreAsImage || target is RedefViewPlace or NumericImagePlace
            ? target.Write(image)
            : LoudStmt($"MOVE of an alphanumeric figurative constant into the numeric item "
                + $"'{target.Item.CobolName}' without image-backed storage (a BINARY/PACKED/COMP-5/float or "
                + "Tier-A shared-storage receiver has no character image to fill — narrow pre-2023 residue of "
                + "the move ISO 2023 removed, §14.9.25.3 SR5 / Annex E.2 item 1)"));
    }

    // IsGroupSender lives on MoveClassifier since P7 Step 7 (the ONE GR4 sender-side test).

    /// <summary>MOVE from a GROUP sender into an ELEMENTARY receiver — a GROUP MOVE (ISO §14.9.25.4 GR4): treated
    /// "exactly as if it were an alphanumeric to alphanumeric elementary move, except that there is no conversion
    /// of data from one form of internal representation to another", the receiving area "filled without
    /// consideration for the individual elementary or group items" — NO numeric conversion (F1-16), NO editing
    /// into a numeric-edited or alphanumeric-edited mask (F1-38 / F1-20, F1-36; GR5 editing applies only to valid
    /// ELEMENTARY moves), NO de-editing. Alignment is §14.6.8 alphanumeric: left-justified, right space-fill /
    /// right truncation — and the receiver's JUSTIFIED still applies ("exactly as if … elementary move";
    /// §13.18.34 attaches to the receiver). The raw image is then deposited by the receiver's STORAGE shape:
    /// string-backed receivers store the width-fitted image directly; a native typed numeric receiver deposits
    /// then decodes through the ONE storage-form bridge (<c>CobolNum.StoreDisplay</c> — the deterministic zoned
    /// decode of possibly-incompatible content that §14.6.13.2 permits; EC-DATA-INCOMPATIBLE is a later EC
    /// slice). A float receiver has no character image — loud (§1.4, the Tier-C island rule).</summary>
    private void EmitGroupToElementaryMove(Place target, BoundOperand source)
    {
        var item = target.Item;
        if (!item.IsImageCapable)
        {
            _ctx.Writer.Line(LoudStmt($"group MOVE into '{item.CobolName}' (a float/COMP-5/INDEX receiver has no character image — Tier-C, COBOLNET_DESIGN §4.2)"));
            return;
        }
        // The width-fitted image (§14.6.8): receiver character-position count via the ONE canonical ImageWidth
        // (V occupies no position; SIGN SEPARATE adds one; P adds none — §13.18.40). deSign is moot for a group.
        string image = $"CobolString.Store({OperandText.AsString(source)}, {item.ImageWidth}{(item.Justified ? ", justifiedRight: true" : "")})";
        // A native typed numeric receiver (long/Int128 backing) needs the decode half of the bridge; every
        // string-backed shape — alphanumeric [edited], numeric-edited, StoreAsImage numeric, a Tier-B
        // RedefViewPlace char window, a NumericImagePlace (its Write IS the decode) — stores the image as-is.
        bool nativeNumeric = item.Pic is { Category: PicCategory.Numeric } && !item.StoreAsImage
            && target is not RedefViewPlace and not NumericImagePlace;
        _ctx.Writer.Line(nativeNumeric
            ? target.Write($"CobolNum.StoreDisplay({image}, {item.ProfileName}, {target.Read()})")
            : target.Write(image));
    }

    /// <summary>MOVE into a whole group (alphanumeric semantics, ISO §14.9 MOVE GR4 — no conversion, filled without
    /// consideration for subordinate items): the source's character image fills the group's leaves via
    /// <c>FromImage</c>. Handles any image-capable group — alphanumeric, numeric-edited, numeric-DISPLAY (native or
    /// stored as its character image, <see cref="DataItem.StoreAsImage"/>), and BINARY/PACKED leaves (their image
    /// slice is the zoned digit form, the §13.18.60 USAGE GR4 implementor representation — COBOLNET_DESIGN §14.4).
    /// Only a group with a float/COMP-5/INDEX leaf stays the genuine Tier-C byte-island (deferred, loud).</summary>
    private void EmitGroupMove(Place target, BoundOperand source)
    {
        if (!target.Item.IsCharacterImage)
        {
            // A group MOVE is a content copy of the underlying representation, without conversion (ISO
            // §14.9.25.4 GR4 — both operands treated as elementary alphanumeric). When the source is a group
            // whose flattened leaf LAYOUT is positionally identical to the receiver's (same usage / digits /
            // scale / sign / width leaf-by-leaf — NC107A's all-COMP U5 → U9), that representation copy is
            // exactly a memberwise leaf copy — kept FIRST: under the digit-image representation both paths are
            // correct (GR4's representation copy ≡ memberwise copy for identical layouts), but the memberwise
            // path skips the encode/decode round trip and is the locked NC107A shape.
            if (source is BoundFieldOperand { Place.Item: { IsGroup: true } srcGroup }
                && AlignedLeafPairs(srcGroup, target.Item) is { } pairs
                && pairs.Select(p => (S: _refs.ResolveItem(p.Src), T: _refs.ResolveItem(p.Tgt))).ToList()
                    is { } resolved && resolved.All(r => r.S is not null && r.T is not null))
            {
                foreach (var (s, t) in resolved)
                    _ctx.Writer.Line(t!.Write(s!.Read()));
                return;
            }
            // Non-aligned layouts (ST127A's 10-leaf WS twin → 11-leaf SD record) and class-view sources
            // (ST134A's SAME-RECORD-AREA window → SD record) fall through to the image path below: an
            // image-capable receiver distributes the source's character image via FromImage exactly like a
            // character group — its fixed-point leaves decode their zoned slices (GR4: filled without
            // consideration for the individual items, over the implementor digit-image representation). Only
            // the genuinely incapable receiver (float/COMP-5/INDEX leaf) stays loud (§1.4).
            if (!target.Item.IsImageCapable)
            {
                _ctx.Writer.Line(LoudStmt($"MOVE to group '{target.Item.CobolName}' with a float/COMP-5/INDEX leaf (Tier-C byte island, deferred — COBOLNET_DESIGN §4.2)"));
                return;
            }
        }
        int width = target.Item.ImageWidth;
        // §8.8.4.1: an alphanumeric group receiver is treated as an elementary alphanumeric item, so a signed-numeric
        // source drops its operational sign here too (§14.9.25.4 GR6a) — deSign:true (a no-op for a non-numeric source).
        // ALL "literal" repeats to the RECEIVER width (ISO §8.3.3.6.4 GR2) — space-padding it would fill the group's
        // tail leaves with blanks instead of the repeated pattern (NC243A's 7-dim table seed).
        string image = source is BoundFigurative f
            ? $"new string({FigurativeConstants.Fill(f.Kind, _ctx.Data.Collating)}, {width})"
            : source is BoundAllLiteral all
            ? CsLiteral(EmitText.RepeatToWidth(all.Literal, width))
            : $"CobolString.Store({OperandText.AsString(source, deSign: true)}, {width})";
        // ISO §13.18.38 GR8: an occurs-depending group RECEIVER with data-name-1 OUTSIDE the group uses only the
        // CURRENT-count part (positions past the count are not modified, GR8a); with data-name-1 INSIDE, the MAXIMUM
        // length is used (GR8b — the normal full-width FromImage). A Tier-B REDEFINES group view's image IS its
        // character window. A normal record-struct group distributes the image into its typed leaves via FromImage.
        _ctx.Writer.Line(target switch
        {
            OdoGroupPlace { DependingInside: false } odo => odo.ReceiveInto(image),
            RedefViewPlace => target.Write(image),
            // A group receiver nested under an OCCURS DYNAMIC level (data-model D9): distribute the image through the
            // RECEIVING accessor (RefReceiving grows-and-seeds past the current capacity, §8.5.1.9.3), NOT target.Read()
            // (=RefSending, which drops an out-of-capacity write into benign scratch — silent data loss).
            DynTablePlace dyn => $"{dyn.ReceivingPath}.FromImage({image});",
            _ => $"{target.Read()}.FromImage({image});",
        });
    }

    /// <summary>The positionally-paired leaves of two groups whose flattened layouts are IDENTICAL — each pair
    /// shares usage, digit count, scale, sign and storage shape, and neither group involves OCCURS, REDEFINES,
    /// RENAMES or reference ambiguity (so <see cref="ReferenceResolver.ResolveItem"/> is exact). Null when the
    /// layouts differ — the caller falls back to the Tier-C loud guard. (ISO §14.9.25.4 GR4: the group move
    /// copies the underlying representation; identical layouts make that a memberwise copy.)</summary>
    private static List<(DataItem Src, DataItem Tgt)>? AlignedLeafPairs(DataItem source, DataItem target)
    {
        // An OCCURS anywhere on the ancestor chain means the group reference is an ELEMENT (needs subscripts
        // this item-level resolution cannot supply) — not this fast path.
        for (var a = source; a is not null; a = a.Parent) if (a.Occurs is not null) return null;
        for (var a = target; a is not null; a = a.Parent) if (a.Occurs is not null) return null;
        var src = new List<DataItem>();
        var tgt = new List<DataItem>();
        if (!Flatten(source, src) || !Flatten(target, tgt) || src.Count != tgt.Count || src.Count == 0)
            return null;
        for (int i = 0; i < src.Count; i++)
        {
            var (a, b) = (src[i].Pic!, tgt[i].Pic!);
            if (a.Usage != b.Usage || a.Category != b.Category || a.Digits != b.Digits || a.Scale != b.Scale
                || a.Signed != b.Signed || a.Length != b.Length || src[i].StoreAsImage != tgt[i].StoreAsImage)
                return null;
        }
        return src.Zip(tgt).ToList();

        // Declaration-order leaves; false when the subtree has a shape the pairing cannot prove equivalent.
        static bool Flatten(DataItem item, List<DataItem> leaves)
        {
            if (item.Occurs is not null || item.RedefinesTargetName is not null
                || item.Renames66.Count > 0 || item.Class is not null)
                return false;
            if (!item.IsGroup)
            {
                if (item.Pic is null) return false;
                leaves.Add(item);
                return true;
            }
            foreach (var c in item.Children)
                if (!Flatten(c, leaves))
                    return false;
            return true;
        }
    }

    /// <summary>The compile-time numeric value of a digit-only <c>ALL "literal"</c> moving to a numeric receiver
    /// (ISO §8.3.3.6.4 GR2 repetition + §14.9.25.4 GR6d3b — the digit string spans the receiver's digit positions,
    /// fraction digits included, so the unscaled value IS the repeated digit run at the receiver's scale). ≤18
    /// digit positions fold to a native <c>long</c> literal; a WIDE receiver (19–31 digits, COBOL-2002+ — ISO
    /// §8.3.1.2) decodes its digit run through the ONE deterministic digit decode (<c>CobolNum.FromAlphanumeric</c>,
    /// Int128 — numeric design D1).</summary>
    private static NumX AllDigitFill(string literal, PicInfo pic)
    {
        string digits = EmitText.RepeatToWidth(literal, Math.Max(pic.Digits, 1));
        return digits.Length <= 18
            ? new NumX($"{long.Parse(digits, System.Globalization.CultureInfo.InvariantCulture)}L", pic.Scale)
            : new NumX($"CobolNum.FromAlphanumeric({CsLiteral(digits)})", pic.Scale);
    }

    /// <summary>True when a MOVE source is a NUMERIC operand (a numeric literal/expression, figurative ZERO, or a
    /// numeric data item) — the §14.9.25.4 GR5 editing path into a numeric-edited receiver applies only to these;
    /// an alphanumeric source moves as plain characters.</summary>
    private static bool IsNumericOperand(BoundOperand source) => source switch
    {
        BoundNumericLiteral or BoundComputedOperand => true,
        BoundFigurative { Kind: 'Z' } => true,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Numeric,
        _ => false,
    };

    /// <summary>The C# expression a MOVE source converts to when stored into <paramref name="target"/>.</summary>
    private string ConvertSource(BoundOperand source, DataItem target)
    {
        var pic = target.Pic!;
        // A figurative constant fills the receiver to its width (ISO §8.3.1.2 / §14.9.24) — EXCEPT figurative
        // ZERO into a numeric-edited receiver, which is the numeric value 0 EDITED into the mask (§14.9.25.4 GR5;
        // 'ZZ9.99' shows '  0.00', not a zero-fill), and EXCEPT an alphanumeric-EDITED receiver, whose insertion
        // positions keep their characters under the editing move (GR5 — MOVE SPACES TO 'XXXBXX/XX' yields '/' at
        // its position, NC223A INI-TEST-GF-1; handled in the switch below).
        if (source is BoundFigurative f && pic.Category is not PicCategory.Numeric
            && !(f.Kind is 'Z' && pic.Category is PicCategory.NumericEdited)
            && !(pic.Category is PicCategory.Alphanumeric && pic.EditMask is not null))
            // Category-aware fill: a national/boolean receiver's HIGH/LOW-VALUE is the D-N3 pin, never the
            // ALPHANUMERIC program collating sequence's extreme (§8.3.3.6 GR6/GR7 over the national sequence).
            return $"new string({FigurativeConstants.Fill(f.Kind, _ctx.Data.Collating, pic.Category)}, {pic.Length})";
        // ALL "literal" repeats the literal to the receiver width (ISO §8.3.3.6.4 GR2).
        if (source is BoundAllLiteral a && pic.Category is not PicCategory.Numeric)
            return CsLiteral(EmitText.RepeatToWidth(a.Literal, pic.Length));

        switch (pic.Category)
        {
            // A NUMERIC source moving to a numeric-edited receiver is EDITED into the receiver's picture
            // (ISO §14.9.25.4 GR5 — alignment + editing); an alphanumeric source stays a plain character move.
            case PicCategory.NumericEdited when IsNumericOperand(source):
                NumX e = _num.AsNum(source, ReceiverContext.None);
                // A float (Real) source lands into the edited receiver via CobolFloat.ToScaled at the MASK's fraction
                // scale (MOVE truncates toward zero, §14.6.8.2) — CobolEdit.Format takes a scaled Int128, not a double
                // (D16 review: the numeric-edited path was missed by the Real integration → CS1503). NB the mask scale
                // is CobolEdit.MaskScale, NOT pic.Scale (a numeric-edited item's Scale is 0 — the point is in the mask).
                int ems = CobolEdit.MaskScale(pic.EditMask!, _ctx.Data.CurrencyPicSymbol, _ctx.Data.DecimalPointIsComma);
                string editVal = e.Real ? $"CobolFloat.ToScaled({e.Expr}, {ems}, CobolRounding.Truncation)" : e.Expr;
                int editScale = e.Real ? ems : e.Scale;
                return $"CobolEdit.Format({editVal}, {editScale}, {CsLiteral(pic.EditMask!)}{BwzFlag(target)}{EditCfg()})";
            // An ELEMENTARY ALPHANUMERIC source into a numeric-edited receiver IS a legal move (§14.9.25.3
            // Table 16): the sending characters are treated as an unsigned integer and EDITED into the mask
            // (§14.9.25.4 GR5 — NC104A MOVE-TEST-F1-39: "12345" → $12,345.00), never a plain character copy.
            // (A GROUP sender never reaches here — GR4 makes that a group move, no editing: EmitGroupToElementaryMove.)
            case PicCategory.NumericEdited:
                return $"CobolEdit.Format(CobolNum.FromAlphanumeric({OperandText.AsString(source, deSign: true)}), 0, {CsLiteral(pic.EditMask!)}{BwzFlag(target)}{EditCfg()})";
            // An ALPHANUMERIC-EDITED receiver places the source's characters into its X/A/9 positions with B 0 /
            // insertion (ISO §14.9.25.4 GR5 — alignment + editing; §13.18.40 simple insertion).
            case PicCategory.Alphanumeric when pic.EditMask is { } amask:
                // A figurative source supplies its fill for EVERY data position (§8.3.1.2 — repeated to width).
                string aeSrc = source is BoundFigurative ff
                    ? $"new string({FigurativeConstants.Fill(ff.Kind, _ctx.Data.Collating)}, {pic.Length})"
                    : OperandText.AsString(source, deSign: true);
                return $"CobolEdit.FormatAlphanumeric({aeSrc}, {CsLiteral(amask)})";
            case PicCategory.Alphanumeric:
                // A signed numeric source drops its operational sign into an alphanumeric receiver (ISO §14.9.25.4 GR6a);
                // a JUSTIFIED receiver right-justifies (left space-fill / left truncation, §14.9.25.4 GR6c).
                return $"CobolString.Store({OperandText.AsString(source, deSign: true)}, {pic.Length}{(target.Justified ? ", justifiedRight: true" : "")})";
            // A NATIONAL receiver stores exactly like alphanumeric on the character substrate (§14.6.8.5 —
            // left-justify, national-space pad, right truncation; JUSTIFIED per §13.18.32): A→N widening,
            // N→N, 9→N digit imaging, and boolean→N all ride AsString under the D-N4 Latin-1 identity
            // correspondence (§14.9.25.4 GR6/GR6a).
            case PicCategory.National:
                return $"CobolString.Store({OperandText.AsString(source, deSign: true)}, {pic.Length}{(target.Justified ? ", justifiedRight: true" : "")})";
            // A BOOLEAN receiver pads/left-fills with boolean ZEROS (§14.6.8.6; JUSTIFIED §13.18.32 GR2).
            // Figurative ZERO already early-returned above as a '0' fill; the SR7-illegal figurative shapes
            // never reach emit (bind-rejected, MoveCategoryLegality).
            case PicCategory.Boolean:
                return $"CobolString.Store({OperandText.AsString(source, deSign: true)}, {pic.Length}, "
                    + $"justifiedRight: {(target.Justified ? "true" : "false")}, pad: '0')";
            case PicCategory.Numeric:
                // A digit-only ALL "literal" repeats across the RECEIVER's digit positions (ISO §8.3.3.6.4 GR2 —
                // repetition to the associated item's size, truncated from the right; §14.9.25.4 GR6d3b — a
                // figurative sending operand takes the receiver's digit count, fraction digits included), so
                // every digit position holds the pattern: ALL "5" → PIC 9(3) stores 555; → PIC 9V9 stores 5.5
                // (unscaled 55 at scale 1 — legacy-oracle confirmed). Valid at EVERY edition for the
                // single-digit-ALL → integer case (§14.9.25.3 SR5, obsolete-flagged 0903 at 2023 by the binder);
                // the non-integer / multi-character shapes are 0902-gated at 2023 and keep these pre-removal
                // semantics at 85/2002/2014 and under --permissive (ALL "57" → PIC 9(3) stores 575, the legacy
                // oracle's '85-obsolete-element behavior — provisional, ratified decision 1). This was the
                // BoundAllLiteral runtime-loud latent bug (W2 track A): the move compiled then died in AsNum.
                // A float RECEIVER (COMP-1/2/FLOAT-*, D16) holds the algebraic value in a native float/double —
                // no PICTURE, no scaled-integer store, no SIZE ERROR (IEEE overflow is Inf, a valid value;
                // §14.6.8.3 GR1). Emit a native cast to its ClrType; a single-precision receiver rounds via (float).
                if (pic.IsFloat)
                    return $"({pic.ClrType})({NumericRenderer.Real(_num.AsNum(source, ReceiverContext.None))})";
                NumX n = source is BoundAllLiteral { IsDigitOnly: true } allDigit
                    ? AllDigitFill(allDigit.Literal, pic)
                    : _num.AsNum(source, ReceiverContext.None);
                // A float SOURCE lands into the fixed receiver via CobolFloat.ToScaled at the receiver scale (MOVE
                // truncates toward zero — §14.6.8.2 GR2/GR4 implementor-defined) then the ordinary store funnel
                // (rescale identity ⇒ no double-rounding; the digit-capacity + SIZE ERROR check still applies).
                int recvScaleM = target.Pic!.Scale;
                string nExpr = n.Real ? $"CobolFloat.ToScaled({n.Expr}, {recvScaleM}, CobolRounding.Truncation)" : n.Expr;
                int nScale = n.Real ? recvScaleM : n.Scale;
                string stored = Narrow($"CobolNum.Store({nExpr}, {nScale}, {target.ProfileName})", target);
                // A whole-group-aliased numeric-DISPLAY receiver stores its character image, not the raw long.
                return target.StoreAsImage ? $"CobolNum.FormatDisplay({stored}, {target.ProfileName})" : stored;
            default:
                return "default";
        }
    }

    // ── Arithmetic ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>In-place arithmetic (ADD TO / SUBTRACT FROM / MULTIPLY BY): each receiver ← receiver op Σoperands,
    /// rounded by the receiver's ROUNDED mode (ISO §14.7.4), under the statement's ON SIZE ERROR phrase if any.</summary>
    private void EmitInPlace(IReadOnlyList<Receiver> targets, string op, IReadOnlyList<BoundExpr> operands, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The operand sum is the ONE initial evaluation (ISO §14.7.7 GR4 + NOTE 3): with several receivers it
            // must be materialized, or a receiver that aliases an operand (ADD A TO B A C) would poison the
            // receivers stored after it when the inlined expression re-reads the field. The fold itself is
            // receiver-INDEPENDENT (GR4 -- the initial evaluation precedes any receiver's involvement): None.
            NumX value = _num.Fold(operands, ReceiverContext.None with { InSizeError = ise });
            if (targets.Count > 1) value = Snapshot(value);
            foreach (var r in targets)
                StoreArith(r.Place, _num.Combine(_num.FieldNum(r.Place), op, value, RcvFor(r, ise)), r.Rounding);
        });

    /// <summary>GIVING arithmetic: the value is computed once and stored into each receiver, rounded by that
    /// receiver's own ROUNDED mode (ISO §14.7.5 rule 4 — one value, stored left-to-right into each resultant).</summary>
    private void EmitGiving(IReadOnlyList<Receiver> targets, Func<ReceiverContext, NumX> value, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The RHS is computed FOR the receiver SET -- the widest receiver scale, Real only when EVERY
            // receiver is float -- the §14.7.7 GR4 one-initial-evaluation shape EmitCompute's multi-target path
            // established (D16). Pre-P7.3 this RHS rendered under the PREVIOUS statement's leftover Target*
            // state (EmitGiving never called SetTarget) -- the H1 staleness ReceiverContext kills.
            var rcv = new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise);
            NumX v = value(rcv);
            // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): materialized with several receivers so a receiver
            // aliasing a sender cannot change the value the remaining receivers store.
            if (targets.Count > 1) v = Snapshot(v);
            foreach (var r in targets) StoreArith(r.Place, v, r.Rounding);
        });

    private void EmitDivide(IReadOnlyList<Receiver> targets, BoundExpr? dividend, BoundExpr divisor, SizeErrorPhrase? sizeErr)
        => EmitArith(sizeErr, ise =>
        {
            // The SENDERS are identified/evaluated ONCE (ISO §14.9.12.4 GR5 → §14.7.7 GR4 + NOTE 3): with several
            // receivers both operands are materialized, so a receiver that aliases the dividend or the divisor
            // (DIVIDE b INTO a GIVING x a y — NC172A/NC173A) cannot poison the quotients stored after it. The
            // DIVISION itself still renders per receiver, at that receiver's scale + ROUNDED mode — equal to
            // rounding the spec's intermediate to each resultant. Sub-expression quotients inside the operands
            // render at the widest receiver scale (the intermediate must not lose receiver-visible digits).
            var opRcv = new ReceiverContext(targets.Max(t => ScaleOf(t.Place)),
                targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise);
            NumX divisorX = _num.Render(divisor, opRcv);
            NumX? dividendX = dividend is not null ? _num.Render(dividend, opRcv) : null;
            if (targets.Count > 1)
            {
                divisorX = Snapshot(divisorX);
                if (dividendX is { } dx) dividendX = Snapshot(dx);
            }
            foreach (var r in targets)
            {
                // The quotient renders at the receiver's OWN scale + ROUNDED mode.
                NumX num = dividendX ?? _num.FieldNum(r.Place);          // INTO-no-GIVING divides the target
                StoreArith(r.Place, _num.Combine(num, "/", divisorX, RcvFor(r, ise)), r.Rounding);
            }
        });

    /// <summary><c>DIVIDE … GIVING q REMAINDER r</c> (ISO §14.9.12 GR7): the remainder is defined from the
    /// INTERMEDIATE quotient TRUNCATED at the quotient receiver's scale — even when the stored quotient is ROUNDED
    /// — as <c>remainder = dividend − (intermediate quotient × divisor)</c>; the subtraction aligns scales exactly.
    /// The quotient stores with its OWN rounding (recomputed at the receiver's mode when not truncation).</summary>
    private void EmitDivideRemainder(BoundDivideRemainder d)
        => EmitArith(d.SizeError, ise =>
        {
            var w = _ctx.Writer;
            int qs = ScaleOf(d.Quotient.Place);
            // The senders render FOR the quotient receiver (its scale governs the intermediate, GR6c/GR7);
            // pre-P7.3 they rendered under the previous statement's leftover Target* state (H1).
            var rcv = new ReceiverContext(qs, d.Quotient.Place.Item.Pic is { IsFloat: true },
                CobolRounding.Truncation, ise);
            // Both senders are materialized (§14.9.12.4 GR5 — one item identification/evaluation): each appears in
            // SEVERAL emitted expressions (kernel call(s) + the remainder back-multiply), and the quotient stores
            // BEFORE the remainder is formed — a quotient receiver aliasing a sender must not poison the remainder.
            NumX dividend = Snapshot(_num.Render(d.Dividend, rcv)), divisor = Snapshot(_num.Render(d.Divisor, rcv));
            // The SUBSIDIARY quotient is truncated to the GIVING receiver's digits/scale (ISO §14.9.12 GR6c) —
            // a DIRECT kernel call at EXACTLY the receiver scale, not the renderer's working-scale promotion
            // (which yields the quotient at the dividend's higher scale and poisons the remainder multiply).
            string fn = ise ? "DivideOrThrow" : "Divide";
            string qt = $"__q{_ctx.Names.NextStoreTmp()}";
            w.Line($"Int128 {qt} = CobolNum.{fn}({dividend.Expr}, {dividend.Scale}, {divisor.Expr}, {divisor.Scale}, {qs}, CobolRounding.Truncation);");
            var product = new NumX($"({qt} * {divisor.Expr})", qs + divisor.Scale);
            NumX remainder = _num.Combine(dividend, "-", product, rcv);   // GR7: dividend − subsidiaryQuotient × divisor
            StoreArith(d.Quotient.Place,
                d.Quotient.Rounding == CobolRounding.Truncation
                    ? new NumX(qt, qs)
                    : new NumX($"CobolNum.{fn}({dividend.Expr}, {dividend.Scale}, {divisor.Expr}, {divisor.Scale}, {qs}, CobolRounding.{d.Quotient.Rounding})", qs),
                d.Quotient.Rounding);
            StoreArith(d.Remainder, remainder, CobolRounding.Truncation);   // REMAINDER has no ROUNDED phrase
        });

    /// <summary>COMPUTE: the RHS is rendered per receiver (so a quotient is computed at that receiver's scale + mode)
    /// then stored, rounded by the receiver's ROUNDED mode, under the ON SIZE ERROR phrase if any.</summary>
    /// <summary>COMPUTE Format 2 — boolean-compute (ISO §14.9.8): render the boolean RHS ONCE, resize to the
    /// GR3 width (the max static boolean-ITEM positions in the expression; 0 = all-literal, no intermediate
    /// resize — the per-receiver store fits it), then store into each elementary boolean receiver with the
    /// §14.6.8.6 left-align / zero-fill / truncate discipline (CobolString.Store, pad '0'; JUSTIFIED honored).
    /// A multi-receiver COMPUTE materializes the value once (the §14.7.7-shaped once-evaluation).</summary>
    private void EmitComputeBoolean(BoundComputeBoolean cb)
    {
        string value = BooleanRenderer.Render(cb.Rhs);
        if (cb.Gr3Width > 0) value = $"CobolBool.Resize({value}, {cb.Gr3Width})";
        // One evaluation for multiple receivers (a boolean expr can read an item a prior receiver aliases).
        if (cb.Targets.Count > 1)
        {
            string tmp = $"__be{_ctx.Names.NextStoreTmp()}";
            _ctx.Writer.Line($"string {tmp} = {value};");
            value = tmp;
        }
        foreach (var t in cb.Targets)
        {
            int width = t is RefModPlace ? -1 : t.Item.Pic?.Length ?? 0;
            string store = width < 0
                ? value   // a ref-mod boolean receiver — the slice write fits via SpliceInto (pad '0')
                : $"CobolString.Store({value}, {width}, justifiedRight: {(t.Item.Justified ? "true" : "false")}, pad: '0')";
            _ctx.Writer.Line(t.Write(store));
        }
    }

    private void EmitCompute(BoundCompute c)
        => EmitArith(c.SizeError, ise =>
        {
            if (c.Targets.Count > 1)
            {
                // ONE initial evaluation (§14.7.7 GR4 + NOTE 3): the RHS renders ONCE — at the widest receiver
                // scale so no receiver-visible digit is lost — is materialized, and every receiver stores from the
                // temp with its own ROUNDED mode. Re-rendering per receiver would re-read senders a prior
                // receiver may alias. Real only when EVERY target is float (D16).
                var rcv = new ReceiverContext(c.Targets.Max(t => ScaleOf(t.Place)),
                    c.Targets.All(t => t.Place.Item.Pic is { IsFloat: true }), CobolRounding.Truncation, ise);
                NumX v = Snapshot(_num.Render(c.Rhs, rcv));
                foreach (var r in c.Targets)
                    StoreArith(r.Place, v, r.Rounding);
                return;
            }
            foreach (var r in c.Targets)
                StoreArith(r.Place, _num.Render(c.Rhs, RcvFor(r, ise)), r.Rounding);
        });

    /// <summary>The <see cref="ReceiverContext"/> for receiver <paramref name="r"/> (P7 Step 3 — the pure
    /// factory replacing the mutable <c>SetTarget</c> context writes).</summary>
    private ReceiverContext RcvFor(Receiver r, bool inSizeError) =>
        new(ScaleOf(r.Place), r.Place.Item.Pic is { IsFloat: true }, r.Rounding, inSizeError);

    /// <summary>The optional <c>blankWhenZero</c> argument text for a numeric-edited store when the receiver
    /// carries BLANK WHEN ZERO (ISO §13.18.8 — zero stores all spaces, MOVE and arithmetic alike).</summary>
    private static string BwzFlag(DataItem item) => item.BlankWhenZero ? ", blankWhenZero: true" : "";

    /// <summary>The program's SPECIAL-NAMES editing-config arguments (<see cref="EmitContext.EditCfgArgs"/>).</summary>
    private string EditCfg() => _ctx.EditCfgArgs;

    /// <summary>Materialize a rendered sender/initial-evaluation into a local temp (ISO §14.7.7 GR4 + NOTE 3 —
    /// ONE initial evaluation; results independent of sender/receiver storage overlap). Inlining the expression
    /// into each receiver's store would re-read its fields after earlier receivers stored.</summary>
    private NumX Snapshot(NumX v)
    {
        string t = $"__ie{_ctx.Names.NextStoreTmp()}";
        _ctx.Writer.Line(v.Dec ? $"CobolDec {t} = {v.Expr};" : $"Int128 {t} = {v.Expr};");
        return v with { Expr = t };
    }

    /// <summary>
    /// Run an arithmetic statement's per-receiver stores (<paramref name="emitStores"/>), wrapping them in the
    /// two-phase ON SIZE ERROR machinery (ISO §14.7.5) when <paramref name="sizeErr"/> is present: a <c>__sizeErr</c>
    /// flag is set by any per-receiver overflow (<c>TryStore</c> false — phase b, the other receivers still store,
    /// rule 2) or by a <c>CobolSizeError</c> raised during evaluation (e.g. a zero divisor — phase a, no receiver
    /// changes, rule 4); the ON / NOT ON SIZE ERROR imperative then runs once. With no phrase the stores run
    /// unchecked (the plain <c>CobolNum.Store</c> path) — behavior unchanged.
    /// </summary>
    private void EmitArith(SizeErrorPhrase? sizeErr, Action<bool> emitStores)
    {
        var w = _ctx.Writer;
        // EC-SIZE checking (>>TURN … EC-SIZE … CHECKING ON, ISO §7.3.25): an ENABLED statement routes through
        // the same two-phase TryStore/try-catch shape even WITHOUT the phrase, latching WHICH Table 13 condition
        // occurred so the §14.9.49 F3 selection and the fatal default see the precise level-3 name. Checking off
        // + no phrase = the unchecked fast path, byte-identical (deep-dive D10 / SSOT §18.16).
        var ecSize = EcEnabledSizeNames();
        if (sizeErr is null && ecSize.Count == 0) { emitStores(false); return; }

        string flag = $"__sizeErr{_ctx.Names.NextSizeErr()}";
        w.Line($"bool {flag} = false;");
        string? ecnVar = null;
        if (ecSize.Count > 0)
        {
            ecnVar = $"__sizeEc{_ctx.Names.NextEc()}";
            w.Line($"string {ecnVar} = \"\";");
            _sizeErrEcVar = ecnVar;
        }
        _sizeErrVar = flag;
        using (w.Block("try")) emitStores(true);   // checked renders: DivideOrThrow / MulChecked (§14.7.5)
        // A zero divisor / PROHIBITED-inexact quotient raises CobolSizeError; an intermediate that overflows the
        // long engine raises OverflowException (the checked(...) the store wraps the value in). Both are the
        // statement's size error condition (ISO §14.7.5 — the phrase ENABLES checking, incl. case 5 intermediate
        // overflow). >long-range overflow still needs the Int128 carrier (G3).
        if (ecnVar is not null)
        {
            int cid = _ctx.Names.NextEc();
            w.Line($"catch (CobolSizeError __cse{cid}) {{ {flag} = true; {ecnVar} = __cse{cid}.EcName; }}");
            w.Line($"catch (System.OverflowException) {{ {flag} = true; {ecnVar} = \"EC-SIZE-OVERFLOW\"; }}");
        }
        else
        {
            w.Line($"catch (CobolSizeError) {{ {flag} = true; }}");
            w.Line($"catch (System.OverflowException) {{ {flag} = true; }}");
        }
        _sizeErrVar = null;
        _sizeErrEcVar = null;

        if (ecnVar is not null)
            EcEmitSizeHandling(flag, ecnVar, ecSize, hasPhrase: sizeErr?.OnError is not null);

        if (sizeErr?.OnError is { } on)
        {
            using (w.Block($"if ({flag})")) EmitStatementList(on);
            if (sizeErr.NotOnError is { } notAlso)
                using (w.Block("else")) EmitStatementList(notAlso);
        }
        else if (sizeErr?.NotOnError is { } not)
            using (w.Block($"if (!{flag})")) EmitStatementList(not);
    }

    /// <summary>Store an arithmetic result into a numeric target place, rounding to the receiver scale with
    /// <paramref name="mode"/> (the receiver's ROUNDED phrase, ISO §14.7.4). Inside an ON SIZE ERROR statement
    /// (<see cref="_sizeErrVar"/> set) it uses the checked <c>CobolNum.TryStore</c> — on overflow / PROHIBITED-inexact
    /// it sets the flag and leaves the receiver unchanged (§14.7.5); otherwise the plain <c>CobolNum.Store</c>.</summary>
    private void StoreArith(Place target, NumX value, CobolRounding mode)
    {
        var w = _ctx.Writer;
        // A numeric-edited receiver stores the EDITED image of the result (ISO §14.7.7 — arithmetic results store
        // per the MOVE editing rules). ROUNDED applies BEFORE editing: the value is rescaled to the mask's
        // fraction scale with the receiver's mode (§14.7.4), then formatted.
        if (target.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } mask })
        {
            int ms = CobolEdit.MaskScale(mask, _ctx.Data.CurrencyPicSymbol, _ctx.Data.DecimalPointIsComma);
            // The narrowing rescale: under ON SIZE ERROR / EC-SIZE, a PROHIBITED-inexact transfer to an edited
            // receiver is a size error (ISO §14.7.4.3 r7 — the receiver stays UNCHANGED). The Dec path's
            // .ToUnscaled and the numeric path's TryStore already throw/flag on that; the Int128 edited path used
            // plain Rescale (silent truncation) — the DEVLOG-610-audited PROHIBITED leak. Use RescaleChecked in
            // the checked branch so all three receiver categories agree; the unchecked branch stays silent
            // (matching the numeric Store path's no-phrase behavior).
            string Aligned(bool checkedPath) =>
                // A float (Real) result lands at the mask scale via CobolFloat.ToScaled with the receiver's ROUNDED
                // mode (D16 review: the edited-receiver arithmetic path was missed by the Real integration → CS1503).
                value.Real ? $"CobolFloat.ToScaled({value.Expr}, {ms}, CobolRounding.{mode})"
                : value.Dec ? $"({value.Expr}).ToUnscaled({ms}, CobolRounding.{mode})"
                : value.Scale == ms ? value.Expr
                : $"CobolNum.{(checkedPath ? "RescaleChecked" : "Rescale")}({value.Expr}, {value.Scale}, {ms}, CobolRounding.{mode})";
            // Under ON SIZE ERROR an edited resultant is capacity-checked too (ISO §14.7.5 case 3 + storing rule
            // 2): an aligned |value| exceeding the mask's digit positions sets the flag and leaves the receiver
            // UNCHANGED — Format's silent high-order truncation is MOVE behavior only (§14.9.25).
            if (_sizeErrVar is { } eflag)
            {
                string img = $"__sv{_ctx.Names.NextStoreTmp()}";
                // EC-SIZE checking latches the Table 13 condition: a store whose significant digits do not fit
                // the receiver is EC-SIZE-TRUNCATION ("significant digits truncated in store").
                string onFail = _sizeErrEcVar is { } ecn1 ? $"{{ {eflag} = true; {ecn1} = \"EC-SIZE-TRUNCATION\"; }}" : $"{eflag} = true;";
                w.Line($"if (!CobolEdit.TryFormat({Aligned(true)}, {ms}, {CsLiteral(mask)}, out var {img}{BwzFlag(target.Item)}{EditCfg()})) {onFail}");
                w.Line($"else {target.Write(img)}");
                return;
            }
            w.Line(target.Write($"CobolEdit.Format({Aligned(false)}, {ms}, {CsLiteral(mask)}{BwzFlag(target.Item)}{EditCfg()})"));
            return;
        }
        // A float RECEIVER (COMP-1/2/FLOAT-*, D16) takes the algebraic value as a native cast — no PICTURE, no
        // scaled store, no SIZE ERROR (IEEE overflow is Inf, a valid value; §14.6.8.3 GR1); ROUNDED is a no-op
        // (the receiver holds the exact algebraic value). BEFORE the fixed-point guard below.
        if (target.Item.Pic is { IsFloat: true })
        {
            w.Line(target.Write($"({target.Item.Pic.ClrType})({NumericRenderer.Real(value)})"));
            return;
        }
        if (target.Item.Pic is not { Category: PicCategory.Numeric, IsFloat: false })
        {
            w.Line(LoudStmt($"arithmetic into a non-fixed-point target '{target.Item.CobolName ?? target.Read()}'"));
            return;
        }
        string profile = target.Item.ProfileName;
        // A float (Real) arithmetic result lands into this FIXED receiver via CobolFloat.ToScaled at the receiver
        // scale with the receiver's ROUNDED mode (D16), then flows through the ordinary store funnel (rescale
        // identity ⇒ no double-rounding; capacity + SIZE ERROR still apply). A STANDARD-DECIMAL intermediate stores
        // through the SDIDI overloads (the §14.7 final transfer).
        int recvScale = target.Item.Pic!.Scale;
        string valExprA = value.Real ? $"CobolFloat.ToScaled({value.Expr}, {recvScale}, CobolRounding.{mode})" : value.Expr;
        string args = value.Dec ? $"{value.Expr}, {profile}"
            : value.Real ? $"{valExprA}, {recvScale}, {profile}"
            : $"{value.Expr}, {value.Scale}, {profile}";
        if (_sizeErrVar is { } flag)
        {
            string tmp = $"__sv{_ctx.Names.NextStoreTmp()}";
            // Intermediate long-engine overflow is detected upstream by the checked multiply the renderer emits in a
            // size-error context (CobolNum.MulChecked → OverflowException, caught by the statement's try, §14.7.5
            // case 5). We do NOT wrap the value in checked(...) here: a constant subexpression would then overflow at
            // COMPILE time (CS0220) and reject valid COBOL — the runtime helper avoids that by not constant-folding.
            // Under EC-SIZE checking the receiver-capacity failure latches EC-SIZE-TRUNCATION (Table 13 —
            // "significant digits truncated in store"; the §14.7.5 size error on the final transfer).
            string onFail = _sizeErrEcVar is { } ecn2 ? $"{{ {flag} = true; {ecn2} = \"EC-SIZE-TRUNCATION\"; }}" : $"{flag} = true;";
            // A float (Real) source under ROUNDED MODE PROHIBITED: an inexact transfer is a size error and leaves the
            // receiver UNCHANGED (§14.7.5 r7). ToScaled already truncated the fraction, so the store's own PROHIBITED
            // check cannot see it — gate on InexactAtScale first (D16 review finding).
            if (value.Real && mode == CobolRounding.Prohibited)
            {
                w.Line($"if (CobolFloat.InexactAtScale({value.Expr}, {recvScale})) {onFail}");
                w.Line($"else if (!CobolNum.TryStore({args}, CobolRounding.{mode}, out var {tmp})) {onFail}");
            }
            else
                w.Line($"if (!CobolNum.TryStore({args}, CobolRounding.{mode}, out var {tmp})) {onFail}");
            // On success store the value (a whole-group-aliased numeric-DISPLAY receiver stores its character image).
            w.Line($"else {target.Write(target.Item.StoreAsImage ? $"CobolNum.FormatDisplay({tmp}, {profile})" : Narrow(tmp, target.Item))}");
            return;
        }
        string stored = $"CobolNum.Store({args}, CobolRounding.{mode})";
        w.Line(target.Write(target.Item.StoreAsImage ? $"CobolNum.FormatDisplay({stored}, {profile})" : Narrow(stored, target.Item)));
    }

    /// <summary>The receiver's working scale: an edited receiver's is its MASK's fraction scale (a `.`-pointed
    /// mask has PicInfo.Scale 0 — the point lives in the mask, not in V); a numeric item's is its PIC scale.</summary>
    /// <summary>Wrap a wide (Int128) stored value for assignment into a NARROW receiver field: a ≤18-digit item
    /// stores as native <c>long</c> (the value is already truncated/rounded to the receiver's digits, so the cast
    /// is exact); a 19+-digit item (the 2002+ wide tier) stores the Int128 directly.</summary>
    private static string Narrow(string expr, DataItem item) =>
        item.Pic is { Digits: > 18 } ? expr : $"(long)({expr})";

    private int ScaleOf(Place p) =>
        p.Item.Pic is { Category: PicCategory.NumericEdited, EditMask: { } m }
            ? CobolEdit.MaskScale(m, _ctx.Data.CurrencyPicSymbol, _ctx.Data.DecimalPointIsComma)
        : p.Item.Pic?.Scale ?? 0;

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
        EmitPerform(p.Control, () => _ctx.Writer.Line($"{_dispatchName}({p.StartPc}, {p.EndPc});"), inline: false);


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
            : s.Address is { } a ? PtrAddressOfText(a)   // ADDRESS OF sender (F7; Phase-4b inc 2)
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
                    Narrow($"CobolNum.Store({UnscaledAtScale(low, pic.Scale)}, {pic.Scale}, {parent.Item.ProfileName})", parent.Item),
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
            if (!file.IsSequential) { KeyedEmitRegistration(w, file); EmitSharingRegistration(w, file); continue; }   // relative/indexed connectors
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
    private (string Kind, string Amount) RenderRetry(RetrySpec? retry) => retry switch
    {
        null => ("FileRetryKind.None", "0"),
        { Kind: RetryKind.Forever } => ("FileRetryKind.Forever", "0"),
        { Kind: RetryKind.Seconds } => ("FileRetryKind.Seconds", RetryAmount(retry.Amount)),
        _ => ("FileRetryKind.Times", RetryAmount(retry.Amount)),
    };

    private string RetryAmount(BoundExpr? amount) =>
        amount is null ? "0" : $"(int)({NumericRenderer.Align(_num.Render(amount, ReceiverContext.None), 0)})";

    /// <summary>Map a bound record-lock phrase to the runtime <c>FileRecordLock</c> enum member (Phase 4d).</summary>
    private static string RuntimeRecordLock(BoundRecordLock l) => l switch
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
    private string? VaryingLengthArg(FileModel file) =>
        file is { Varying.DependingName: not null, VaryingDependingItem: { } d } && _refs.ResolveItem(d) is { } dep
            ? $"(int)CobolTable.Occ({dep.Read()})" : null;

    /// <summary>After a SUCCESSFUL read of a RECORD VARYING … DEPENDING file, store the just-read record's length
    /// into the DEPENDING item (ISO §13.18.43 GR15; GR12 — an unsuccessful READ leaves it unchanged, so the call
    /// site sits inside the success branch).</summary>
    private void EmitReadLengthStore(FileModel file)
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
    private void EmitImageInto(Place record, string imageExpr)
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
    private void EmitStoreFileStatus(FileModel file)
    {
        // ISO §12.4.5.8 / §9.1.13.1 — the two-character status is stored into the FILE STATUS item as part of
        // the I/O statement's execution, BEFORE any exception processing.
        if (file.FileStatusName is null) return;   // no FILE STATUS clause — nothing to store
        // An INHERITED GLOBAL file stores into the OWNER's status item through the __outer chain
        // (§12.4.5.8.4 GR1 NOTE 1 — the item is updated by contained-program references to the global
        // file-name even though it is a LOCAL name of the owner; map built per unit in CallEmitProgramClass).
        Place? place = _callInheritedStatusPlace.TryGetValue(file, out var inherited)
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
