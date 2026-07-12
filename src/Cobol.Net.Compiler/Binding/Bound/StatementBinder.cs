// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using Antlr4.Runtime.Tree;
using CobolNet.Runtime;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using Microsoft.CodeAnalysis.CSharp;

using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// Builds the <see cref="BoundProgram"/> from a parsed program unit: it resolves every reference to a
/// <see cref="Place"/>, decodes every literal, and binds every expression / condition / statement into a bound node
/// exactly once (COBOLNET_DESIGN §2). The backend then renders the bound tree — it never re-walks the parse tree.
/// </summary>
public sealed partial class StatementBinder(DataBinder data, ReferenceResolver refs)
{
    // ── The Step-10 collaborator seam (P7; the phase doc's §Step 10 AS-BUILT PLAN): ONE BinderContext per
    //    binder instance (= per unit / class roster) and ONE instance of each verb collaborator (their
    //    counters/memos are per-unit lifetime). Lazy — the primary-ctor fields are captured, not fields, so
    //    eager initializers cannot reference them. During the incremental extraction the collaborators reach
    //    not-yet-extracted spine members through THIS host (the Step-9 migration-wiring precedent); the 10t
    //    final wiring retargets them and thins this class to dispatch + the composition root. ──
    private BinderContext? _binderCtx;
    private InspectBinder? _inspectBinder;
    private EvaluateBinder? _evaluateBinder;
    private StringUnstringBinder? _stringUnstringBinder;
    private MoveBinder? _moveBinder;
    private CorrespondingBinder? _corrBinder;
    private InitializeBinder? _initializeBinder;
    private BinderContext Ctx => _binderCtx ??= new BinderContext(data, refs);
    private InspectBinder Inspect => _inspectBinder ??= new InspectBinder(Ctx, this);
    private EvaluateBinder Evaluate => _evaluateBinder ??= new EvaluateBinder(Ctx, this);
    private StringUnstringBinder Strings => _stringUnstringBinder ??= new StringUnstringBinder(Ctx, this);
    private MoveBinder Move => _moveBinder ??= new MoveBinder(Ctx, this, Corr);
    private ReportWriterBinder? _rwBinder;
    private ReportWriterBinder Rw => _rwBinder ??= new ReportWriterBinder(Ctx);
    private FileLockBinder? _fileLockBinder;
    private FileLockBinder FileLock => _fileLockBinder ??= new FileLockBinder(Ctx, this);
    private PtrBinder? _ptrBinder;
    internal PtrBinder Ptr => _ptrBinder ??= new PtrBinder(Ctx, this);
    private KeyedIoBinder? _keyedIoBinder;
    private KeyedIoBinder KeyedIo => _keyedIoBinder ??= new KeyedIoBinder(Ctx, this, FileLock);
    private SequentialIoBinder? _seqIoBinder;
    internal SequentialIoBinder SeqIo => _seqIoBinder ??= new SequentialIoBinder(Ctx, this, KeyedIo, FileLock);
    private AcceptDisplayBinder? _acceptBinder;
    private AcceptDisplayBinder Accept => _acceptBinder ??= new AcceptDisplayBinder(Ctx, this);
    private SortBinder? _sortBinder;
    private SortBinder Sort => _sortBinder ??= new SortBinder(Ctx, this, SeqIo);
    private CallBinder? _callBinder;
    private CallBinder Call => _callBinder ??= new CallBinder(Ctx, this);
    private UdfBinder? _udfBinder;
    internal UdfBinder Udf => _udfBinder ??= new UdfBinder(Ctx, this);
    private IntrinsicBinder? _intrinsicBinder;
    internal IntrinsicBinder Intrinsic => _intrinsicBinder ??= new IntrinsicBinder(Ctx, this);
    private ControlFlowBinder? _controlFlowBinder;
    private ControlFlowBinder ControlFlow => _controlFlowBinder ??= new ControlFlowBinder(Ctx, this);
    private SetBinder? _setBinder;
    private SetBinder Set => _setBinder ??= new SetBinder(Ctx, this);
    private SearchBinder? _searchBinder;
    private SearchBinder Search => _searchBinder ??= new SearchBinder(Ctx, this);
    private SetAlterBinder? _setAlterBinder;
    internal SetAlterBinder Alter => _setAlterBinder ??= new SetAlterBinder(Ctx, this);

    private ConditionBinder? _conditionBinder;
    internal ConditionBinder Cond => _conditionBinder ??= new ConditionBinder(Ctx, this);

    // Host forwarders for the collaborator callers + the remaining core spine sites — flip at 10t.
    internal BoundCondition BindCondition(IParseTree node) => Cond.BindCondition(node);
    internal BoundRelational CheckedRelational(BoundOperand left, string op, BoundOperand right) => Cond.CheckedRelational(left, op, right);
    internal Condition88? ConditionOf(Core.DataReferenceContext dref) => Cond.ConditionOf(dref);
    internal void CheckClassConditionOperand(BoundOperand op, char kind) => Cond.CheckClassConditionOperand(op, kind);
    internal static string MapOperator(string raw) => ConditionBinder.MapOperator(raw);
    internal static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr) => ConditionBinder.SoleDataRef(expr);
    internal static string? SoleNumLiteral(Core.ArithmeticExpressionContext expr) => ConditionBinder.SoleNumLiteral(expr);
    internal BoundBoolExpr BindBoolExpr(Core.BooleanExpressionContext bctx) => Cond.BindBoolExpr(bctx);
    internal static int Gr3Width(BoundBoolExpr e) => ConditionBinder.Gr3Width(e);
    internal BoundStatement SwitchBindSet(Core.SetSwitchStatementContext sw) => Alter.SwitchBindSet(sw);
    internal BoundStatement AlterGoTo(Core.GoToStatementContext g, int writtenTarget) => Alter.AlterGoTo(g, writtenTarget);
    internal BoundStatement AlterBindBareGoTo(Core.GoToStatementContext g) => Alter.AlterBindBareGoTo(g);

    /// <summary>Host forwarder (ControlFlowBinder's VARYING induction targets) — flips at 10t.</summary>
    internal BoundSetTarget? SetTargetOf(Core.DataReferenceContext dref) => Set.SetTargetOf(dref);

    /// <summary>Host forwarder for the collaborator callers (MoveBinder / AcceptDisplayBinder) — flips to a
    /// direct ctor ref at the 10t final wiring.</summary>
    internal BoundOperand IntrinsicOperand(Core.FunctionCallContext fc) => Intrinsic.IntrinsicOperand(fc);

    /// <summary>The compilation group's user-function signature table (FUNCTION-ID name → RETURNING +
    /// USING descriptions), built by the run-unit emitter between the DATA and PROCEDURE bind phases.
    /// Null in unit-test direct construction and in class-unit binders — every user-function reference
    /// there fails loud (COBOLNET1505).</summary>
    public IReadOnlyDictionary<string, UserFunctionSignature>? UserFunctions { get; set; }

    /// <summary>The containing FUNCTION-ID unit's own function name, when this binder binds a function
    /// definition's body — §8.4.6.6: a referenced function-prototype-name shall be "the user-function-name
    /// of the containing function definition OR a function-prototype-name declared in the REPOSITORY
    /// paragraph", so self-recursion needs NO repository entry (a present self-entry is ignored per
    /// §12.3.8 GR11 — same resolution either way). Null in program units.</summary>
    public string? UdfSelfName { get; set; }

    /// <summary>True when the unit being bound is a nested (contained) program — set from <c>BoundUnit.Parent</c>
    /// at binder construction. Gates FUNCTION MODULE-NAME NESTED (§15.65.3 argument rule 1 — NESTED shall be
    /// specified only within a contained program).</summary>
    public bool InNestedProgram { get; init; }
    private CorrespondingBinder Corr => _corrBinder ??= new CorrespondingBinder(Ctx, this);
    private InitializeBinder Init => _initializeBinder ??= new InitializeBinder(Ctx, this);

    private readonly List<(string Cobol, string Method, Core.SentenceContext[] Sentences)> _paras = [];
    private readonly Dictionary<string, int> _paraIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SectionInfo> _sections = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SectionInfo?> _paraSection = [];   // per-pc owning section (parallel to _paras)
    private SectionInfo? _currentSection;                     // the section whose paragraph is being bound

    /// <summary>A PROCEDURE DIVISION section (ISO §14.4.3): its contiguous paragraph pc range — paragraphs flatten
    /// into the one pc sequence in source order, so a section IS the inclusive range [StartPc, EndPc] (empty section
    /// ⇒ StartPc &gt; EndPc) — and its own paragraph map for qualified procedure-name resolution (ISO §8.4.2.2:
    /// <c>para OF section</c>, and the same-section implicit resolution of duplicated paragraph names).</summary>
    internal sealed class SectionInfo(string name, int startPc)
    {
        public string Name { get; } = name;
        public int StartPc { get; } = startPc;
        public int EndPc { get; set; } = startPc - 1;
        public Dictionary<string, int> Paras { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Bind a program unit's PROCEDURE DIVISION into a <see cref="BoundProgram"/>.</summary>
    public BoundProgram Bind(Core.ProgramUnitContext program)
    {
        if (program.procedureDivision() is not { } pd) return new BoundProgram([]);
        EcCollectPdRaising(pd);   // the PD-header RAISING list (§14.2.1) — consumed by the GOBACK/EXIT SR2 check
        CollectParagraphs(pd);

        var bound = new List<BoundParagraph>(_paras.Count);
        for (int i = 0; i < _paras.Count; i++)
        {
            _currentSection = _paraSection[i];   // ISO §8.4.2.2 — unqualified names resolve in-section first
            _currentBindPc = i;                  // RESUME SR1/SR2 declarative context + §15.30 location anchoring
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in _paras[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            bound.Add(new BoundParagraph(_paras[i].Cobol, sentences));
        }
        _currentSection = null;
        _currentBindPc = -1;
        return new BoundProgram(bound, _entryPc, _declaratives, BuildEcFeatures());
    }

    // ── Procedure table (paragraphs + sections, ISO §14.4.3 / §8.4.2.2) ─────────────────────────────────────

    /// <summary>Register one paragraph (name + uniquified method key + its sentences) at the next pc. Inside a
    /// METHOD body (<see cref="_currentMethodScope"/> set — the class-body collection) the name declares
    /// METHOD-LOCALLY (ISO §11.7 — sibling methods may reuse names; cross-method resolution must FAIL), so it
    /// registers in the method's own map, never the program-global fallback.</summary>
    private void AddParagraph(string name, Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        string baseName = "P_" + name.Replace('-', '_').Replace('.', '_');
        string method = baseName;
        for (int n = 2; !used.Add(method); n++) method = $"{baseName}_{n}";
        if (_currentMethodScope is { } ms)
            ms.Paras.TryAdd(name, _paras.Count);   // method-local declaration (§11.7)
        else
            _paraIndex.TryAdd(name, _paras.Count); // first definition wins for the global fallback
        section?.Paras.TryAdd(name, _paras.Count); // in-section map for qualified / same-section resolution
        _paraSection.Add(section);
        _paraMethod.Add(_currentMethodScope);
        _paras.Add((name, method, sentences));
    }

    private void CollectParagraphs(Core.ProcedureDivisionContext pd)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);

        // DECLARATIVES first (ISO §14.2.3 GR1 — execution begins with the first NONdeclarative procedure; the
        // declarative sections share the ONE pc space, entered only via the USE dispatch or an explicit
        // PERFORM/GO TO — SR4). The walk records the BoundDeclarative scopes (StatementBinder.Declaratives.cs).
        foreach (var dp in pd.declarativePart())
            foreach (var sec in dp.declarativeSection())
                DeclCollectSection(sec, used);
        _entryPc = _paras.Count;

        foreach (var unit in pd.procedureUnit())
        {
            if (unit.paragraphDefinition() is { } para)
                AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
            else if (unit.sectionDefinition() is { } section)
            {
                // A section's paragraphs are contiguous in the pc sequence, so the section IS a pc range:
                // GO TO section transfers to its first paragraph (ISO §14.9.17), PERFORM section runs first
                // statement of its first paragraph through last statement of its last (ISO §14.9.28).
                var info = new SectionInfo(section.sectionName().GetText(), _paras.Count);
                foreach (var p in section.paragraphDefinition())
                    AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                info.EndPc = _paras.Count - 1;
                _sections.TryAdd(info.Name, info);
            }
        }
    }

    /// <summary>Resolve a procedure-name reference to its inclusive pc range (ISO §8.4.2.2): a section name is its
    /// paragraph range; a paragraph is (pc, pc). The head/qualifier are taken from the context's CHILDREN — never
    /// <c>GetText()</c> of the whole context, which concatenates <c>PAR-1A OF SEC-1</c> into an unmatchable key.
    /// Resolution order: explicit <c>OF/IN section</c> qualifier → the named section's own map; unqualified → a
    /// paragraph of the CURRENT section (implicit qualification of duplicated names), then the global first-defined
    /// paragraph, then a section name. Null when unknown (the caller fails loud).</summary>
    internal (int Start, int End)? ResolveProcedure(Core.ProcedureNameContext ctx)
    {
        string head = ctx.GetChild(0).GetText();
        string? qualifier = ctx.ChildCount >= 3 ? ctx.GetChild(2).GetText() : null;
        // Inside a METHOD body resolution is CONFINED to the method's own maps (ISO §11.7 — method-local
        // procedure names; a cross-method PERFORM/GO TO resolves to nothing and the caller fails loud, the
        // legacy trap-#10 rule made structural).
        if (_currentMethodScope is { } m)
        {
            if (qualifier is not null)
                return m.Sections.TryGetValue(qualifier, out var mq) && mq.Paras.TryGetValue(head, out int mqpc)
                    ? (mqpc, mqpc) : null;
            if (_currentSection is { } mcur && mcur.Paras.TryGetValue(head, out int mlocal)) return (mlocal, mlocal);
            if (m.Paras.TryGetValue(head, out int mpc)) return (mpc, mpc);
            if (m.Sections.TryGetValue(head, out var msec)) return (msec.StartPc, msec.EndPc);
            return null;
        }
        if (qualifier is not null)
            return _sections.TryGetValue(qualifier, out var q) && q.Paras.TryGetValue(head, out int qpc)
                ? (qpc, qpc) : null;
        if (_currentSection is { } cur && cur.Paras.TryGetValue(head, out int local)) return (local, local);
        if (_paraIndex.TryGetValue(head, out int pc)) return (pc, pc);
        if (_sections.TryGetValue(head, out var sec)) return (sec.StartPc, sec.EndPc);
        return null;
    }

    /// <summary>The emitted paragraphs (name + method + sentences), exposed for the backend's method loop.</summary>
    public IReadOnlyList<(string Cobol, string Method, Core.SentenceContext[] Sentences)> Paragraphs => _paras;

    /// <summary>The per-pc owning-section list + the ambient in-section cursor — the NARROW procedure-table
    /// surface SetAlterBinder's prepass reads/saves/restores (P7 Step 10n; the table hoists to
    /// ProcedureTableBuilder at 10t and these host edges delete).</summary>
    internal IReadOnlyList<SectionInfo?> ParaSections => _paraSection;
    internal SectionInfo? CurrentSection { get => _currentSection; set => _currentSection = value; }

    private string MethodOf(string cobolName) =>
        _paraIndex.TryGetValue(cobolName, out int i) ? _paras[i].Method : "P_" + cobolName.Replace('-', '_');

    // ── Statements ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind one statement, then apply the compile-time TurnState fold (StatementBinder.Exceptions.cs):
    /// a statement under enabled EC checking wraps in <see cref="BoundEcChecked"/>; checking-off binds the bare
    /// node — the zero-scaffolding gate (ISO §7.3.25.4 GR1 default OFF; deep-dive D10).</summary>
    private BoundStatement BindStatement(Core.StatementContext s)
    {
        // Object-property references (D-P2) and user-function activations (M2-UDF-1): mark-on-entry /
        // drain-own-suffix — a reference resolved while THIS statement bound (including in its condition)
        // belongs to THIS statement's wrap; one resolved inside a nested statement was already drained by
        // that statement's own BindStatement. The UDF wrap is the INNER sequence (function activations are
        // always pre-ops, §8.4.3.2.3 SR1), so a property argument's GET — a pre-op of the OUTER property
        // wrap — still runs before the activation that consumes its temp.
        int udfMark = Udf.PendingCount;
        int mark = data.OoPendingPropertyOps.Count;
        var core = BindStatementCore(s);
        core = Udf.UdfWrapCalls(core, udfMark);
        core = OoWrapPropertyOps(core, mark);
        return EcWrap(s, core);
    }

    private BoundStatement BindStatementCore(Core.StatementContext s) => s switch
    {
        _ when s.displayStatement() is { } d => Accept.BindDisplay(d),
        _ when s.moveStatement() is { } m => Move.Bind(m),
        _ when s.addStatement() is { } a => BindAdd(a),
        _ when s.subtractStatement() is { } sub => BindSubtract(sub),
        _ when s.multiplyStatement() is { } mul => BindMultiply(mul),
        _ when s.divideStatement() is { } div => BindDivide(div),
        _ when s.computeStatement() is { } c => BindCompute(c),
        _ when s.ifStatement() is { } iff => ControlFlow.BindIf(iff),
        _ when s.performStatement() is { } p => ControlFlow.BindPerform(p),
        _ when s.setStatement() is { } set => Set.BindSet(set),
        _ when s.searchStatement() is { } se => Search.BindSearch(se),
        _ when s.evaluateStatement() is { } ev => Evaluate.Bind(ev),
        _ when s.inspectStatement() is { } ins => Inspect.Bind(ins),
        _ when s.searchAllStatement() is { } sa => Search.BindSearchAll(sa),
        _ when s.goToStatement() is { } g => ControlFlow.BindGoTo(g),
        _ when s.alterStatement() is { } al => Alter.BindAlter(al),   // 85-only; rejected ≥2002 inside the pass gate (deleted by ISO/IEC 1989:2002)
        _ when s.exitStatement() is { } e => ControlFlow.BindExit(e),
        _ when s.openStatement() is { } o => SeqIo.BindOpen(o),
        _ when s.closeStatement() is { } c => SeqIo.BindClose(c),
        _ when s.writeStatement() is { } w => SeqIo.BindWrite(w),
        _ when s.readStatement() is { } r => SeqIo.BindRead(r),
        _ when s.rewriteStatement() is { } rw => SeqIo.BindRewrite(rw),
        _ when s.startStatement() is { } st => KeyedIo.BindStart(st),
        _ when s.deleteStatement() is { } del => KeyedIo.BindDelete(del),
        _ when s.deleteFileStatement() is { } dfs => KeyedIo.BindDeleteFile(dfs),
        _ when s.unlockStatement() is { } ul => FileLock.BindUnlock(ul),
        _ when s.stringStatement() is { } sstr => Strings.BindString(sstr),
        _ when s.unstringStatement() is { } suns => Strings.BindUnstring(suns),
        _ when s.acceptStatement() is { } ac => Accept.BindAccept(ac),
        _ when s.initializeStatement() is { } ini => Init.Bind(ini),
        _ when s.continueStatement() is not null => new BoundNop(),
        _ when s.nextSentenceStatement() is not null => new BoundNextSentence(),
        // STOP RUN vs STOP literal (X3.23-1985 Format 2 — communicate to the operator, then CONTINUE): the
        // literal form no longer silently binds as STOP RUN (the DEVLOG-578 mis-bind; edition-gated ≥2002 by
        // the validator, its 85 semantics implemented via BoundStopLiteral).
        _ when s.stopStatement() is { } stop => ControlFlow.BindStop(stop),
        _ when s.gobackStatement() is { } gb => Call.BindGoback(gb),   // §14.9.18 — called-program return; 2002+ gated
        _ when s.invokeStatement() is { } inv => OoBindInvoke(inv),   // §14.9.23 — OO method invocation (2002+ grammar-gated)
        _ when s.callStatement() is { } call => Call.BindCall(call),
        _ when s.cancelStatement() is { } cancel => Call.BindCancel(cancel),
        _ when s.entryStatement() is not null => new BoundUnsupported("ENTRY (ISO/IEC 1989 defines no ENTRY statement — vendor extension; interprogram design)"),
        // ENTER language-name [routine-name] (X3.23-1985 Nucleus, deleted by ISO 2002 — 0902-gated ≥2002 by
        // the version-conformance pass, VCR Table 7 row 7.16): comment-equivalent when only COBOL is supported — the
        // conforming '85 posture; accepted-inert as a no-op.
        _ when s.enterStatement() is not null => new BoundNop(),
        _ when s.sortStatement() is { } srt => Sort.BindSort(srt),
        _ when s.mergeStatement() is { } mrg => Sort.BindMerge(mrg),
        _ when s.releaseStatement() is { } rls => Sort.BindRelease(rls),
        _ when s.returnStatement() is { } ret => Sort.BindReturn(ret),
        _ when s.initiateStatement() is { } rwi => Rw.BindInitiate(rwi),     // Report Writer (ISO §14.9.21)
        _ when s.generateStatement() is { } rwg => Rw.BindGenerate(rwg),     // Report Writer (ISO §14.9.16)
        _ when s.terminateStatement() is { } rwt => Rw.BindTerminate(rwt),   // Report Writer (ISO §14.9.46)
        _ when s.raiseStatement() is { } ra => BindRaise(ra),               // EC model (ISO §14.9.29; 2002+ gated)
        _ when s.resumeStatement() is { } rs => BindResume(rs),             // EC model (ISO §14.9.33; 2002+ gated)
        _ when s.allocateStatement() is { } al => Ptr.BindAllocate(al),      // dynamic storage (ISO §14.9.3; Phase-4b inc 2)
        _ when s.freeStatement() is { } fr => Ptr.BindFree(fr),              // dynamic storage (ISO §14.9.15; Phase-4b inc 2)
        _ => new BoundUnsupported($"statement '{FirstToken(s)}'"),
    };

    /// <summary>STOP RUN [WITH {NORMAL|ERROR} [STATUS …]] / STOP literal (ISO §14.9.42). The status phrase is a
    /// COBOL-2002 introduction — bind-time introduction gate (rearch bind-time migration Cluster 4; the parse-time
    /// {is2002()}? predicate is gone). The phrase has no runtime effect in this compiler, so the gate is its only
    /// binder obligation.</summary>
    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────

    /// <summary>Bind a RETRY phrase (ISO §14.7.9). The n-TIMES amount is a bounded re-attempt count; FOR n
    /// SECONDS / FOREVER are single-run-unit no-ops (no competing process releases — named residue).</summary>
    internal RetrySpec BindRetry(Core.RetryPhraseContext rp) =>
        rp.FOREVER() is not null ? new RetrySpec(RetryKind.Forever, null)
        : rp.SECONDS() is not null ? new RetrySpec(RetryKind.Seconds, BindExpr(rp.arithmeticExpression()))
        : new RetrySpec(RetryKind.Times, BindExpr(rp.arithmeticExpression()));

    private BoundStatement BindAdd(Core.AddStatementContext add)
    {
        if (add.addOperandList() is not { } operands) return Corr.BindAddCorresponding(add);   // Format 3 (§14.9.2.2)
        var addends = operands.addOperand().Select(BindExpr).ToList();
        var sizeErr = BindSizeError(add.arithmeticOnSizeError());
        if (add.addGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §14.9.1 Format 3: the TO operand is an addend, NOT a
            // receiver; only the GIVING operands receive). Previously the TO operand was dropped from the sum.
            if (add.addToPhrase() is { } toAddend)
                addends.AddRange(DataRefs(toAddend).Select(BindExpr));
            var givingRecv = Receivers(giving.receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("ADD", addends, givingRecv);
            return new BoundAddGiving(addends, givingRecv, sizeErr);
        }
        if (add.addToPhrase() is { } to)
        {
            var recv = Receivers(to.receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("ADD", addends, recv);
            return new BoundAddTo(addends, recv, sizeErr);
        }
        return new BoundUnsupported("ADD form");
    }

    private BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.subtractOperandList() is not { } operands) return Corr.BindSubtractCorresponding(sub);   // Format 3 (§14.9.44.2)
        var minuends = operands.subtractOperand().Select(BindExpr).ToList();
        var sizeErr = BindSizeError(sub.arithmeticOnSizeError());
        if (sub.subtractGivingPhrase() is { } giving && sub.subtractFromPhrase()?.subtractFromOperand() is { } from)
        {
            var fromX = BindExpr(from);
            var recv = Receivers(giving.receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("SUBTRACT", [.. minuends, fromX], recv);
            return new BoundSubtractGiving(minuends, fromX, recv, sizeErr);
        }
        if (sub.subtractFromPhrase()?.subtractFromOperand() is { } targets)
        {
            var recv = Receivers(targets.receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("SUBTRACT", minuends, recv);
            return new BoundSubtractFrom(minuends, recv, sizeErr);
        }
        return new BoundUnsupported("SUBTRACT form");
    }

    private BoundStatement BindMultiply(Core.MultiplyStatementContext mul)
    {
        if (mul.multiplyOperand() is not { } aCtx) return new BoundUnsupported("MULTIPLY form");
        var a = BindExpr(aCtx);
        var byOps = mul.multiplyByOperand();
        var sizeErr = BindSizeError(mul.arithmeticOnSizeError());
        if (mul.multiplyGivingPhrase() is { } giving && byOps.Length > 0)
        {
            var b = BindExpr(byOps[0]);
            var recv = Receivers(giving.receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("MULTIPLY", [a, b], recv);
            return new BoundMultiplyGiving(a, b, recv, sizeErr);
        }
        // In-place: each BY operand is itself the receiver (target ← target × a).
        var byRecv = Receivers(byOps);
        Ctx.Validation.CheckComposite("MULTIPLY", [a], byRecv);
        return new BoundMultiplyBy(a, byRecv, sizeErr);
    }

    private BoundStatement BindDivide(Core.DivideStatementContext div)
    {
        if (div.divideOperand() is not { } aCtx) return new BoundUnsupported("DIVIDE form");
        var a = BindExpr(aCtx);   // INTO: the divisor; BY: the dividend
        var sizeErr = BindSizeError(div.arithmeticOnSizeError());

        // DIVIDE … GIVING q REMAINDER r (ISO §14.9.12 Formats 4–5): exactly one GIVING receiver (SR6).
        if (div.divideRemainderPhrase() is { } rem)
        {
            if (div.divideGivingPhrase() is not { } g) return new BoundUnsupported("DIVIDE REMAINDER without GIVING");
            var quotients = Receivers(g.receivingArithmeticOperand());
            if (quotients.Count != 1) return new BoundUnsupported("DIVIDE REMAINDER quotient receiver");
            if (refs.Resolve(rem.dataReference()) is not { } r)
                return new BoundUnsupported($"DIVIDE REMAINDER receiver '{rem.dataReference().GetText()}'");
            BoundExpr dividend = div.divideIntoPhrase() is { } i ? BindExpr(i.divideIntoOperand())
                : div.divideByPhrase() is not null ? a
                : a;
            BoundExpr divisor = div.divideIntoPhrase() is not null ? a
                : div.divideByPhrase() is { } b ? BindExpr(b.divideOperand())
                : a;
            Ctx.Validation.CheckComposite("DIVIDE", [dividend, divisor], quotients);
            return new BoundDivideRemainder(dividend, divisor, quotients[0], r, sizeErr);
        }

        if (div.divideIntoPhrase() is { } into)
        {
            if (div.divideGivingPhrase() is { } giving)
            {
                var dividendX = BindExpr(into.divideIntoOperand());
                var recv = Receivers(giving.receivingArithmeticOperand());
                Ctx.Validation.CheckComposite("DIVIDE", [dividendX, a], recv);
                return new BoundDivideGiving(dividendX, a, recv, sizeErr);
            }
            var intoRecv = Receivers(into.divideIntoOperand().receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("DIVIDE", [a], intoRecv);
            return new BoundDivideInto(a, intoRecv, sizeErr);   // target ← target ÷ a
        }
        if (div.divideByPhrase() is { } byPhrase && div.divideGivingPhrase() is { } gv)
        {
            var divisorX = BindExpr(byPhrase.divideOperand());
            var recv = Receivers(gv.receivingArithmeticOperand());
            Ctx.Validation.CheckComposite("DIVIDE", [a, divisorX], recv);
            return new BoundDivideGiving(a, divisorX, recv, sizeErr);
        }
        return new BoundUnsupported("DIVIDE form");
    }

    private BoundStatement BindCompute(Core.ComputeStatementContext compute)
    {
        // COMPUTE Format 2 — boolean-compute (ISO §14.9.8; the {is2002()}? grammar alternative).
        if (compute.booleanExpression() is { } boolExpr) return BindComputeBoolean(compute, boolExpr);
        if (compute.arithmeticExpression() is not { } expr) return new BoundUnsupported("COMPUTE without an expression");
        // F1 → F2 re-route: `COMPUTE bool-item = bool-item` parses as Format 1 (a sole-identifier RHS predicts
        // the arithmetic alt), so a boolean receiver or a sole boolean-category RHS re-routes to the boolean
        // bind (the "ANTLR alternative-order reality" precedent). A boolean RHS/receiver never reaches the
        // numeric channel.
        bool receiverBoolean = compute.computeStore().Length > 0
            && refs.Resolve(compute.computeStore(0).dataReference()) is { Item.Pic.Category: PicCategory.Boolean };
        bool rhsBoolean = SoleDataRef(expr) is { } d && refs.Resolve(d) is { Item.Pic.Category: PicCategory.Boolean };
        if (receiverBoolean || rhsBoolean)
        {
            BoundBoolExpr rerouted = SoleDataRef(expr) is { } sd && refs.Resolve(sd) is { } sp
                    && (sp is RefModPlace rm2 ? rm2.Inner.Item.Pic?.Category : sp.Item.Pic?.Category) is PicCategory.Boolean
                ? new BoundBoolRef(sp)
                : new BoundBoolError($"COMPUTE boolean receiver takes a boolean expression, not '{expr.GetText()}' "
                    + "(ISO §14.9.8 Format 2)");
            return BuildComputeBoolean(compute, rerouted);
        }
        var rhs = BindExpr(expr);
        return new BoundCompute(rhs, Receivers(compute.computeStore()), BindSizeError(compute.computeOnSizeError()));
    }

    private BoundStatement BindComputeBoolean(Core.ComputeStatementContext compute, Core.BooleanExpressionContext boolExpr)
    {
        // The COBOL-2002 boolean-operator introduction gate on COMPUTE Format 2 (BooleanOperators2002) fires on
        // RECOGNITION in the VersionConformancePass parse-arm (VisitComputeStatement, HasBoolOp on the F2
        // booleanExpression); Step 14h.4b.
        var rhs = BindBoolExpr(boolExpr);
        // SR3 (§14.9.8 :26575): the expression shall not consist solely of an ALL literal.
        if (rhs is BoundBoolAll)
            data.Edition.Error("COBOLNET1511", "a boolean COMPUTE expression shall not consist solely of an ALL "
                + "literal (ISO §14.9.8 Format 2 SR3)");
        return BuildComputeBoolean(compute, rhs);
    }

    /// <summary>Shared tail for both the direct Format-2 bind and the F1→F2 re-route: receiver conformance
    /// (SR2 — elementary boolean), the ROUNDED / SIZE-ERROR prohibition (F2 has neither), the GR3 store width.</summary>
    private BoundStatement BuildComputeBoolean(Core.ComputeStatementContext compute, BoundBoolExpr rhs)
    {
        if (compute.computeOnSizeError() is not null)
            data.Edition.Error("COBOLNET1511", "ON SIZE ERROR may not be specified on a boolean COMPUTE "
                + "(ISO §14.9.8 Format 2 — no size-error phrase)");
        var targets = new List<Place>();
        foreach (var store in compute.computeStore())
        {
            if (store.roundedPhrase() is not null)
                data.Edition.Error("COBOLNET1511", "ROUNDED may not be specified on a boolean COMPUTE "
                    + "(ISO §14.9.8 Format 2)");
            if (refs.Resolve(store.dataReference()) is not { } p)
            {
                data.Edition.Error("COBOLNET1511", $"COMPUTE receiver '{store.dataReference().GetText()}' is unresolvable");
                continue;
            }
            var cat = p is RefModPlace rm ? rm.Inner.Item.Pic?.Category : p.Item.Pic?.Category;
            if (cat is not PicCategory.Boolean)
                data.Edition.Error("COBOLNET1511", $"the receiver '{store.dataReference().GetText()}' of a boolean "
                    + "COMPUTE shall be an elementary boolean item (ISO §14.9.8 Format 2 SR2)");
            targets.Add(p);
        }
        return new BoundComputeBoolean(rhs, targets, Gr3Width(rhs));
    }

    internal List<BoundStatement> BindBlocks(IEnumerable<Core.StatementBlockContext> blocks) =>
        blocks.SelectMany(b => b.statement()).Select(BindStatement).ToList();

    // ── ON SIZE ERROR phrase (ISO §14.7.5) ───────────────────────────────────────────────────────────────────

    internal SizeErrorPhrase? BindSizeError(Core.ArithmeticOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), PhraseBlocks.StartsWithNot(ctx));

    internal SizeErrorPhrase? BindSizeError(Core.ComputeOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), PhraseBlocks.StartsWithNot(ctx));

    /// <summary>Build the phrase from the (1 or 2) statement blocks — the shared two-branch shape via the ONE
    /// <see cref="PhraseBlocks.Split"/> extractor (P7 Step 10b).</summary>
    private SizeErrorPhrase BuildSizeError(Core.StatementBlockContext[] blocks, bool notFirst)
    {
        var (onErr, notErr) = PhraseBlocks.Split(blocks, notFirst, b => BindBlocks([b]));
        return new SizeErrorPhrase(onErr, notErr);
    }

    /// <summary>The C# <c>long</c> index field when <paramref name="dref"/> is a bare INDEXED BY index-name
    /// (ISO §13.18.38 — index-names are a separate name class living in <see cref="DataBinder.IndexFields"/>,
    /// not the data-item tree), else <see langword="null"/>.</summary>
    internal string? IndexFieldOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord()?.GetText() is { } w
        && data.Symbols.TryResolveIndex(w, data.ActiveScope, out var f) ? f : null;

    // ── Operands & expressions ─────────────────────────────────────────────────────────────────────────────

    internal BoundOperand LiteralOperand(Core.LiteralContext lit)
    {
        var nn = lit.nonNumericLiteral();
        if (nn?.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (nn?.STRINGLIT() is { } s) return new BoundStringLiteral(CobolLiteral.Decode(s.GetText()));
        // National N"…" (§8.3.3.5) / boolean B"…" (§8.3.3.4) literals — LIVE (Phase 4a): the introduction
        // gate rides every occurrence (0900 below 2002); content/size guards are the 0814 band. The lexer
        // already restricts a BOOLLIT's content to [01]+ (CobolLexer.g4).
        if (nn?.NATLIT() is { } nat) return NationalLiteralOperand(nat.GetText());
        if (nn?.BOOLLIT() is { } b) return BooleanLiteralOperand(b.GetText());
        return new BoundNumericLiteral(CheckLiteral(lit.GetText()));   // edition digit cap (ISO §8.3.1.2)
    }

    /// <summary>Bind an <c>N"…"</c> national literal (ISO §8.3.3.5): SR1 caps the length at 8,191 national
    /// positions; the track-(a) repertoire is Latin-1 (chars ≤ U+00FF, D-N4) — a wider character needs the
    /// staged alphanumeric↔national correspondence (§8.3.3.5 SR2/GR3 + §8.1.2) and errors 0814, never a
    /// silent mojibake store.</summary>
    internal BoundStringLiteral NationalLiteralOperand(string raw)
    {
        // NationalData2002 (the N"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        string value = CobolLiteral.Decode(raw);
        if (value.Length > 8191)
            data.Edition.Error("COBOLNET0814", $"national literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.5 SR1)");
        if (value.Any(c => c > 'ÿ'))
            data.Edition.Error("COBOLNET0814", "national literal contains a character outside the Latin-1 "
                + "repertoire — the alphanumeric↔national correspondence for wider characters is not yet "
                + "implemented (Phase 4a residue; ISO §8.3.3.5 SR2/GR3, §8.1.2)");
        return new BoundStringLiteral(value) { Category = PicCategory.National };
    }

    /// <summary>Bind a <c>B"…"</c> boolean literal (ISO §8.3.3.4): SR1 caps the length at 8,191 boolean
    /// positions; SR2 ('0'/'1' only) is lexer-enforced.</summary>
    internal BoundStringLiteral BooleanLiteralOperand(string raw)
    {
        // BooleanData2002 (the B"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        string value = CobolLiteral.Decode(raw);
        if (value.Length > 8191)
            data.Edition.Error("COBOLNET0814", $"boolean literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.4 SR1)");
        return new BoundStringLiteral(value) { Category = PicCategory.Boolean };
    }

    /// <summary>Bind a figurative constant to a bound operand. <c>ALL "literal"</c> (a multi-character figurative,
    /// ISO §8.3.3.6.4 Format 6) → <see cref="BoundAllLiteral"/>; <c>ALL ZEROS</c> etc. are the single-character
    /// figurative repeated to width, identical to the bare word. (ALL HEXLIT / NULL stay a later slice.)</summary>
    internal static BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig)
    {
        if (fig.STRINGLIT() is { } allLit) return new BoundAllLiteral(CobolLiteral.Decode(allLit.GetText()));
        if (fig.ZERO() is not null) return new BoundFigurative('Z');
        if (fig.SPACE() is not null) return new BoundFigurative('S');
        if (fig.HIGH_VALUE() is not null) return new BoundFigurative('H');
        if (fig.LOW_VALUE() is not null) return new BoundFigurative('L');
        if (fig.QUOTE_() is not null) return new BoundFigurative('Q');
        if (fig.NULL_() is not null) return new BoundFigurative('N');
        return new BoundOperandError($"figurative constant '{fig.GetText()}'");
    }

    internal BoundOperand FieldOperand(Core.DataReferenceContext dref) =>
        Intrinsic.KeywordOmittedFunction(dref) is { } kof ? IntrinsicBinder.OperandOf(kof)   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundComputedOperand(new BoundLinageCounterRef(lcf))
                : new BoundOperandError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15) — RWCS registers, intercepted ahead of name resolution
        // (the LINAGE-COUNTER idiom); a BoundExprError inside the computed wrapper stays loud (§1.4).
        : Rw.CounterExpr(dref) is { } rcx ? new BoundComputedOperand(rcx)
        : IndexFieldOf(dref) is { } ix ? new BoundComputedOperand(new BoundIndexRef(ix))
        : refs.Resolve(dref) is { } p ? new BoundFieldOperand(p) : new BoundOperandError(RefFailure(dref));

    /// <summary>The loud-failure text for an unresolvable data reference — when the name belongs to a REJECTED
    /// shared-storage class (a Tier-C / national REDEFINES, an unsupported cell shape), the class's
    /// <c>RejectReason</c> rides along so the runtime loud names WHY, not just the reference (the
    /// design's "references then fail loud" contract, made self-explanatory).</summary>
    private string RefFailure(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        string? reason = data.Symbols.TryResolve(name, data.ActiveScope, out var named)
            ? named.Select(i => i.Class)
                .FirstOrDefault(c => c is { Tier: RedefinesTier.Rejected, RejectReason: not null })
                ?.RejectReason
            : null;
        return reason is null ? $"reference '{dref.GetText()}'" : $"reference '{dref.GetText()}' — {reason}";
    }

    /// <summary>Bind a data reference in a numeric-expression position: an INDEXED BY index-name reads its
    /// occurrence number (valid in SET/SEARCH/relations, ISO §13.18.38); the LINAGE-COUNTER register reads its
    /// file's runtime counter (ISO §8.4.3.14 GR1 — an unsigned integer); otherwise the resolved item's value.
    /// The ONE dataReference→<see cref="BoundExpr"/> mapping, used by every expression path.</summary>
    private BoundExpr RefExpr(Core.DataReferenceContext dref) =>
        Intrinsic.KeywordOmittedFunction(dref) is { } kof ? kof   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundLinageCounterRef(lcf)
                : new BoundExprError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15): in the PROCEDURE DIVISION the registers may appear
        // wherever an integer item may (SR1) — read from the report's engine instance, never storage.
        : Rw.CounterExpr(dref) is { } rcx ? rcx
        : IndexFieldOf(dref) is { } ix ? new BoundIndexRef(ix)
        : refs.Resolve(dref) is { } p ? new BoundNumRef(p)
        : new BoundExprError(RefFailure(dref));

    /// <summary>Resolve a LINAGE-COUNTER reference to its file (ISO §8.4.3.14): in the grammar alternative
    /// <c>LINAGE_COUNTER ((OF|IN) cobolWord)?</c> the cobolWord IS the file-name qualifier. Unqualified, the
    /// register resolves only when exactly ONE file has a LINAGE clause — with several, qualification is
    /// required (§8.4.3.14 SR3 / §8.4.2.2). Null (the caller binds a loud error) for no/an ambiguous match,
    /// with a bind-time diagnostic naming the rule.</summary>
    private FileModel? LinageFileOf(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord() is { } q)   // qualified: LINAGE-COUNTER OF/IN file-name
        {
            if (data.FilesByName.TryGetValue(q.GetText(), out var named) && named.Linage is not null) return named;
            data.Edition.Error("COBOLNET0863", $"LINAGE-COUNTER OF '{q.GetText()}': the qualifier shall name a "
                + "file whose file description entry contains a LINAGE clause (ISO §8.4.3.14 / §13.18.34 GR7a)");
            return null;
        }
        var linageFiles = data.Files.Where(f => f.Linage is not null).ToList();
        if (linageFiles.Count == 1) return linageFiles[0];
        data.Edition.Error("COBOLNET0864", linageFiles.Count == 0
            ? "LINAGE-COUNTER referenced, but no file description entry contains a LINAGE clause (ISO §8.4.3.14 — "
              + "the register is generated by the presence of a LINAGE clause)"
            : "unqualified LINAGE-COUNTER with more than one LINAGE file: qualify by file-name (ISO §8.4.3.14 "
              + "SR3 / §8.4.2.2 Qualification)");
        return null;
    }

    // Receiving references resolve through ResolveReceiving (StatementBinder.ReportWriter.cs) — the ONE
    // receiving-side chokepoint: a report counter receiver is rejected at bind (LINE-COUNTER illegal per ISO
    // §8.4.3.15 SR3; PAGE-COUNTER staged loud) instead of being SILENTLY dropped by .OfType<Place>() (§1.4).
    internal List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) =>
        targets.Select(ResolveReceiving).OfType<Place>().ToList();

    // ── ROUNDED phrase → rounding mode + receiver resolution (ISO §14.7.4) ───────────────────────────────────

    /// <summary>The rounding mode a (possibly absent) ROUNDED phrase selects (ISO §14.7.4.3). No phrase → TRUNCATION
    /// (rule 2); a bare <c>ROUNDED</c> → the program's DEFAULT ROUNDED mode (rule 1 / §11.9.6 — the OPTIONS
    /// <c>DEFAULT ROUNDED MODE IS x</c> clause, defaulting to NEAREST-AWAY-FROM-ZERO when absent); an explicit
    /// <c>MODE IS x</c> → the named mode (via the shared <see cref="RoundingModes"/> mapping).</summary>
    internal CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase)
    {
        if (phrase is null) return CobolRounding.Truncation;
        if (phrase.roundingModeName() is { } mode)
        {
            // The explicit MODE IS phrase (and the 8-mode set) is ISO 2014+ (§14.7.4); at 85/2002 a bare ROUNDED
            // means the single nearest-away-from-zero rounding. The RoundedModeIs2014 introduction gate fires on
            // RECOGNITION in the VersionConformancePass parse-arm (VisitRoundedPhrase, roundingModeName != null); 14h.4a.
            return RoundingModes.Map(mode);
        }
        return data.Options.DefaultRounding;
    }

    // ── The RECEIVING chokepoint (hoisted from the ReportWriter partial at 10f — the shared receiving
    //    spine 5 pipelines consume; final home ExpressionBinder at 10q). ──

    /// <summary>Resolve a RECEIVING data reference to its <see cref="Place"/> — the ONE receiving-side
    /// chokepoint (MOVE targets, arithmetic resultants, SET receivers). A report counter here is rejected at
    /// bind time: LINE-COUNTER shall not be a receiving operand (ISO §8.4.3.15 SR3 — illegal); PAGE-COUNTER as a
    /// receiver is legal but not yet implemented (staged loud). Without this guard the
    /// <c>.OfType&lt;Place&gt;()</c> receiver pipelines would DROP the counter silently — a silent-miscompile
    /// hazard (§1.4).</summary>
    internal Place? ResolveReceiving(Core.DataReferenceContext dref)
    {
        if (dref.LINE_COUNTER() is not null)
        {
            data.Edition.Error(DiagnosticCatalog.ReportLineCounterReceiving,
                "LINE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.15.3 SR3)");
            return null;
        }
        if (dref.PAGE_COUNTER() is not null)
        {
            data.Edition.Error(DiagnosticCatalog.ReportPageCounterReceiving, "PAGE-COUNTER as a receiving operand (ISO §8.4.3.15 — legal; the "
                + "program assigns page numbers) is not yet implemented");
            return null;
        }
        var place = refs.Resolve(dref);
        // The OCCURS DYNAMIC CAPACITY register (§13.18.38 SR30–32; D9) is set ONLY by a SET Format 14 statement
        // (which reroutes BEFORE this chokepoint). Any other receiving use — MOVE/arithmetic resultant/ordinary SET
        // receiver — is illegal; reject it here rather than reach CapacityRegisterPlace.Write (an internal throw).
        if (place is CapacityRegisterPlace cap)
        {
            data.Edition.Error("COBOLNET1523", $"the CAPACITY register '{cap.RegisterItem.CobolName}' shall not be a "
                + "receiving operand except in a SET statement Format 14 (ISO §13.18.38 SR30–32)");
            return null;
        }
        return place;
    }

    /// <summary>Resolve <c>receivingArithmeticOperand</c>s (the GIVING / TO / FROM / INTO resultants) to
    /// <see cref="Receiver"/>s, each carrying its own ROUNDED mode; an unresolvable reference is dropped.</summary>
    private List<Receiver> Receivers(IEnumerable<Core.ReceivingArithmeticOperandContext> ops) =>
        ops.Select(o => ResolveReceiving(o.dataReference()) is { } p ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the in-place <c>MULTIPLY … BY</c> receivers (<c>multiplyByOperand</c> = receiving operand +
    /// optional ROUNDED), each carrying its own mode; a literal BY operand (only valid in a GIVING form) is dropped.</summary>
    private List<Receiver> Receivers(IEnumerable<Core.MultiplyByOperandContext> ops) =>
        ops.Select(o => o.receivingOperand()?.dataReference() is { } d && ResolveReceiving(d) is { } p
                ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the <c>COMPUTE</c> resultants (<c>computeStore</c> = data reference + optional ROUNDED).</summary>
    private List<Receiver> Receivers(IEnumerable<Core.ComputeStoreContext> stores) =>
        stores.Select(s => ResolveReceiving(s.dataReference()) is { } p ? new Receiver(p, RoundingOf(s.roundedPhrase())) : null)
              .OfType<Receiver>().ToList();

    /// <summary>Bind any numeric node (expression, operand wrapper, literal, or data reference) to a bound expression.</summary>
    internal BoundExpr BindExpr(IParseTree node) => node switch
    {
        Core.ArithmeticExpressionContext a => BindExpr(a.GetChild(0)),
        Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext => BindChain(node),
        Core.PowerExpressionContext p => BindPower(p),
        Core.UnaryExpressionContext u => u.primaryExpression() is { } pr ? BindExpr(pr)
            : u.addOp().GetText() == "-" ? new BoundNegate(BindExpr(u.unaryExpression())) : BindExpr(u.unaryExpression()),
        Core.PrimaryExpressionContext pe => BindPrimary(pe),
        Core.LiteralContext l => NumLiteral(l),
        Core.DataReferenceContext d => RefExpr(d),
        _ => BindOperandExpr(node),   // operand wrappers (addOperand, multiplyByOperand, …)
    };

    /// <summary>A numeric literal expression from a <c>literal</c> node, mapping a figurative ZERO (incl. <c>ALL ZEROS</c>)
    /// to <c>0</c> (ISO §8.3.1.2 — ZERO is a valid numeric operand); a non-numeric figurative (SPACE / HIGH-VALUE / …)
    /// in a numeric context is a loud error rather than the raw word rendered as an identifier. A national or
    /// boolean literal is NOT a numeric operand (§8.8.1.1 — arithmetic operands shall be numeric): COBOLNET0844
    /// at bind, never raw literal text spliced into the generated expression.</summary>
    private BoundExpr NumLiteral(Core.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.figurativeConstant() is { } fig)
            return fig.ZERO() is not null ? new BoundNumLiteral("0")
                : new BoundExprError($"figurative constant '{fig.GetText()}' in a numeric context");
        if (lit.nonNumericLiteral() is { } nn && (nn.NATLIT() ?? nn.BOOLLIT()) is not null)
        {
            data.Edition.Error("COBOLNET0844", $"a {(nn.NATLIT() is not null ? "national" : "boolean")} "
                + "literal is not a numeric operand (ISO §8.8.1.1 — arithmetic operands shall be numeric)");
            return new BoundExprError($"literal '{lit.GetText()}' in a numeric context");
        }
        return new BoundNumLiteral(CheckLiteral(lit.GetText()));
    }

    /// <summary>Normalize the decimal separator (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a — the comma form
    /// canonicalizes to dot-decimal so every emit-side decoder sees one shape) and edition-gate the digit count
    /// (ISO §8.3.1.2 — 1..18 at COBOL-85, 1..31 at 2002+). The ONE literal chokepoint for the expression paths.</summary>
    internal string CheckLiteral(string text)
    {
        text = data.NormalizeNumericLiteral(text);
        int digits = text.Count(char.IsAsciiDigit);
        data.Edition.CheckDigitCapacity(digits, $"numeric literal '{text}'");
        return text;
    }

    private BoundExpr BindChain(IParseTree node)
    {
        BoundExpr? acc = null;
        char op = '+';
        foreach (var child in Children(node))
        {
            if (child is Core.AddOpContext or Core.MulOpContext) op = child.GetText()[0];
            else { var x = BindExpr(child); acc = acc is null ? x : new BoundBinary(acc, op, x); }
        }
        return acc ?? new BoundNumLiteral("0");
    }

    private BoundExpr BindPower(Core.PowerExpressionContext p)
    {
        var bases = p.unaryExpression();
        BoundExpr acc = BindExpr(bases[0]);
        for (int i = 1; i < bases.Length; i++) acc = new BoundPower(acc, BindExpr(bases[i]));
        return acc;
    }

    private BoundExpr BindPrimary(Core.PrimaryExpressionContext pe)
    {
        if (pe.numericLiteral() is { } num) return new BoundNumLiteral(CheckLiteral(num.GetText()));
        if (pe.ZERO_ARITH() is not null) return new BoundNumLiteral("0");
        if (pe.dataReference() is { } dref) return RefExpr(dref);
        if (pe.arithmeticExpression() is { } paren) return BindExpr(paren);
        // FUNCTION call (ISO §15; the 1989 Intrinsic Function Module) — StatementBinder.Intrinsics.cs.
        if (pe.functionCall() is { } fc) return Intrinsic.BindIntrinsic(fc);
        return new BoundExprError("primary-expression operand");
    }

    /// <summary>Descend an operand-wrapper node to its inner arithmetic expression, or its leaf literal / data
    /// ref. The wrapper chain can nest the expression MORE than one level deep (<c>comparisonOperand →
    /// valueOperand → arithmeticExpression</c>, CobolExpressions.g4), so the walk is BREADTH-FIRST to the
    /// shallowest match — a depth-first leaf grab would collapse a multi-term operand to its first data
    /// reference (a sign condition's operand is the WHOLE expression, ISO §8.8.4.3 — NC250A IF--TEST-55/56).</summary>
    internal BoundExpr BindOperandExpr(IParseTree node)
    {
        var queue = new Queue<IParseTree>();
        queue.Enqueue(node);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            for (int i = 0; i < n.ChildCount; i++)
            {
                var c = n.GetChild(i);
                if (c is Core.ArithmeticExpressionContext ae) return BindExpr(ae);
                if (c is Core.LiteralContext l) return NumLiteral(l);
                if (c is Core.DataReferenceContext d) return RefExpr(d);
                queue.Enqueue(c);
            }
        }
        return new BoundNumLiteral("0");
    }

    // ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<Core.DataReferenceContext> DataRefs(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.DataReferenceContext dref) yield return dref;
            else foreach (var inner in DataRefs(child)) yield return inner;
        }
    }

    internal static IEnumerable<IParseTree> Children(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++) yield return node.GetChild(i);
    }

    private static string FirstToken(IParseTree node) =>
        node.ChildCount > 0 ? node.GetChild(0).GetText() : node.GetText();
}
