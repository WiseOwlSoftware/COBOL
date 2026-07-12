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
    internal ReportWriterBinder Rw => _rwBinder ??= new ReportWriterBinder(Ctx);
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

    private ArithmeticBinder? _arithmeticBinder;
    internal ArithmeticBinder Arith => _arithmeticBinder ??= new ArithmeticBinder(Ctx, this);

    private ExpressionBinder? _exprBinder;
    internal ExpressionBinder Expr => _exprBinder ??= new ExpressionBinder(Ctx, this);

    private EcBinder? _ecBinder;
    internal EcBinder Ec => _ecBinder ??= new EcBinder(Ctx, this);

    private OoBinder? _ooBinder;
    internal OoBinder Oo => _ooBinder ??= new OoBinder(Ctx, this);

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
    /// <summary>PUBLIC forwarder (BinderDriver / the OO bind half configure the EC context per unit) —
    /// re-points to the collaborator at the 10t final wiring like every other host edge.</summary>
    public void ConfigureEc(TurnState turn, string programName) => Ec.ConfigureEc(turn, programName);
    internal EcFeatures BuildEcFeatures() => Ctx.EcState.BuildFeatures();
    internal void EcNoteFunction() => Ec.EcNoteFunction();
    internal BoundStatement BindSetLastException() => Ec.BindSetLastException();
    internal BoundRaising? EcBindRaising(Core.RaisingPhraseContext raising, int line, string verb) => Ec.EcBindRaising(raising, line, verb);
    internal void EcLoadPdRaising(IReadOnlyList<string> ecNames, IReadOnlyList<string> classes) => Ec.EcLoadPdRaising(ecNames, classes);
    internal BoundExpr BindExpr(IParseTree node) => Expr.BindExpr(node);
    internal BoundExpr BindOperandExpr(IParseTree node) => Expr.BindOperandExpr(node);
    internal BoundOperand LiteralOperand(Core.LiteralContext lit) => Expr.LiteralOperand(lit);
    internal BoundOperand FieldOperand(Core.DataReferenceContext dref) => Expr.FieldOperand(dref);
    internal BoundStringLiteral NationalLiteralOperand(string raw) => Expr.NationalLiteralOperand(raw);
    internal BoundStringLiteral BooleanLiteralOperand(string raw) => Expr.BooleanLiteralOperand(raw);
    internal static BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig) => ExpressionBinder.FigurativeOperand(fig);
    internal string CheckLiteral(string text) => Expr.CheckLiteral(text);
    internal string? IndexFieldOf(Core.DataReferenceContext dref) => Expr.IndexFieldOf(dref);
    internal List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) => Expr.ResolveTargets(targets);
    internal Place? ResolveReceiving(Core.DataReferenceContext dref) => Expr.ResolveReceiving(dref);
    internal List<Receiver> Receivers(IEnumerable<Core.ReceivingArithmeticOperandContext> ops) => Expr.Receivers(ops);
    internal List<Receiver> Receivers(IEnumerable<Core.MultiplyByOperandContext> ops) => Expr.Receivers(ops);
    internal List<Receiver> Receivers(IEnumerable<Core.ComputeStoreContext> stores) => Expr.Receivers(stores);
    internal CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase) => Expr.RoundingOf(phrase);
    internal BoundStatement OoBindSetObjectRef(IReadOnlyList<Core.DataReferenceContext> targetRefs,
        Core.DataReferenceContext? senderRef, bool senderNull, bool senderSelf, bool senderSuper)
        => Oo.OoBindSetObjectRef(targetRefs, senderRef, senderNull, senderSelf, senderSuper);
    internal static Core.DataReferenceContext? OoExtractBareReference(Core.ArithmeticExpressionContext e) => OoBinder.OoExtractBareReference(e);
    internal BoundStatement OoBindMethodGoback(Core.GobackStatementContext g) => Oo.OoBindMethodGoback(g);
    internal BoundStatement OoBindExitMethod(Core.ExitStatementContext e) => Oo.OoBindExitMethod(e);
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
    internal CorrespondingBinder Corr => _corrBinder ??= new CorrespondingBinder(Ctx, this);
    private InitializeBinder Init => _initializeBinder ??= new InitializeBinder(Ctx, this);

    private readonly List<(string Cobol, string Method, Core.SentenceContext[] Sentences)> _paras = [];
    private readonly Dictionary<string, int> _paraIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SectionInfo> _sections = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SectionInfo?> _paraSection = [];   // per-pc owning section (parallel to _paras;
                                                              // the ambient CURRENT section lives on Ctx — 10s)

    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — set by the run-unit emitter before
    /// binding so INVOKE resolves classes/methods defined anywhere in the group. Null ⇔ empty group.</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The CLASS whose method bodies this binder is binding (set by the emitter's OoBindClassBody;
    /// null in a program unit) — the SELF/SUPER resolution root (§8.4.3.8: SELF resolves on the current
    /// class's chain, SUPER starts at its BASE; slice 3b).</summary>
    public OoClassSymbol? OoCurrentClass { get; set; }

    /// <summary>True while binding the FACTORY roster (§11.4): SELF/SUPER resolve over the FACTORY interface
    /// (§14.9.23.3 SR4f/h) and SELF|SUPER "NEW" binds the ACTIVE-CLASS creation form (§16.2.1).</summary>
    public bool OoInFactory { get; set; }

    private readonly List<OoMethodScope?> _paraMethod = [];   // per-pc owning method (parallel to _paras; the
                                                              // ambient CURRENT scope lives on Ctx — 10s)

    /// <summary>True while binding a statement inside a METHOD body — the D8 context switch (GOBACK →
    /// method return; EXIT PROGRAM → §14.9.14.3 SR7 violation).</summary>
    internal bool InMethod => Ctx.CurrentMethodScope is not null;

    /// <summary>Bind a program unit's PROCEDURE DIVISION into a <see cref="BoundProgram"/>.</summary>
    public BoundProgram Bind(Core.ProgramUnitContext program)
    {
        if (program.procedureDivision() is not { } pd) return new BoundProgram([]);
        Ec.EcCollectPdRaising(pd);   // the PD-header RAISING list (§14.2.1) — consumed by the GOBACK/EXIT SR2 check
        CollectParagraphs(pd);

        var bound = new List<BoundParagraph>(_paras.Count);
        for (int i = 0; i < _paras.Count; i++)
        {
            // Section + cursor as ONE scoped bind position (10s; no method overlay in a program unit) —
            // §8.4.2.2 in-section resolution + the RESUME SR1/SR2 / §15.30 location anchoring.
            using var _ = Ctx.EnterMethodScope(_paraSection[i], null, i);
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in _paras[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            bound.Add(new BoundParagraph(_paras[i].Cobol, sentences));
        }
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
        if (Ctx.CurrentMethodScope is { } ms)
            ms.Paras.TryAdd(name, _paras.Count);   // method-local declaration (§11.7)
        else
            _paraIndex.TryAdd(name, _paras.Count); // first definition wins for the global fallback
        section?.Paras.TryAdd(name, _paras.Count); // in-section map for qualified / same-section resolution
        _paraSection.Add(section);
        _paraMethod.Add(Ctx.CurrentMethodScope);
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
        if (Ctx.CurrentMethodScope is { } m)
        {
            if (qualifier is not null)
                return m.Sections.TryGetValue(qualifier, out var mq) && mq.Paras.TryGetValue(head, out int mqpc)
                    ? (mqpc, mqpc) : null;
            if (Ctx.CurrentSection is { } mcur && mcur.Paras.TryGetValue(head, out int mlocal)) return (mlocal, mlocal);
            if (m.Paras.TryGetValue(head, out int mpc)) return (mpc, mpc);
            if (m.Sections.TryGetValue(head, out var msec)) return (msec.StartPc, msec.EndPc);
            return null;
        }
        if (qualifier is not null)
            return _sections.TryGetValue(qualifier, out var q) && q.Paras.TryGetValue(head, out int qpc)
                ? (qpc, qpc) : null;
        if (Ctx.CurrentSection is { } cur && cur.Paras.TryGetValue(head, out int local)) return (local, local);
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
    internal SectionInfo? CurrentSection { get => Ctx.CurrentSection; set => Ctx.CurrentSection = value; }

    private string MethodOf(string cobolName) =>
        _paraIndex.TryGetValue(cobolName, out int i) ? _paras[i].Method : "P_" + cobolName.Replace('-', '_');

    /// <summary>Appended to unknown-procedure guards bound inside a method: names resolve METHOD-LOCALLY
    /// (§11.7), so a reference to a sibling method's paragraph fails HERE by design (the legacy trap-#10
    /// cross-method reject) — the hint tells the reader why the name a human can see is "unknown".</summary>
    internal string OoScopeHint => InMethod
        ? " (method-local resolution, ISO §11.7 — paragraphs of sibling methods and of the driver program are not visible in a method)"
        : "";

    /// <summary>
    /// Bind a CLASS body: every method's paragraphs flatten into the class's ONE pc space (source order), each
    /// method holding its contiguous exit-bounded range — the emit-into-a-type spine's binding half. The part-2
    /// scope binds parameterless void methods completely; a method's own data division, PD USING/RETURNING/
    /// RAISING formals, and declaratives are recognized-but-staged loud (port slice 2), never silently skipped.
    /// </summary>
    public BoundProgram BindMethodRoster(OoClassSymbol cls, IReadOnlyList<OoMethodSymbol> roster)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        var methods = new List<BoundMethod>(roster.Count);

        foreach (var m in roster)
        {
            if (m.PropertySubject is not null)
            {
                // A PROPERTY-clause-SYNTHESIZED accessor: no COBOL body exists — the emitter renders the
                // direct field read/write (D-P1; observably identical to the §13.18.42 GR1/GR2 implicit
                // MOVE methods). It still occupies a roster slot (override/implements machinery applies).
                m.EntryPc = _paras.Count;
                m.EndPc = _paras.Count - 1;   // empty body by construction
                methods.Add(new BoundMethod(m.Name, m.CsName, m.EntryPc, m.EndPc));
                continue;
            }
            // A method IS a source element (§14.9.18.3 SR2/SR4a): its OWN PD-header RAISING partition
            // (D-EO8) becomes the binder's per-element sets while its body binds.
            EcLoadPdRaising(m.RaisingEcNames, m.RaisingClasses);
            // The method's DATA (LINKAGE → params-as-locals, LOCAL-STORAGE → locals, method-WS → statics) was
            // bound by DataBinder.OoBindMethodData before any body binds; here we link its name scope so the
            // per-pc switch below activates §11.7 GR5 shadowing while this method's statements bind.
            var scope = new OoMethodScope { Data = m.DataScope };
            Ctx.CurrentMethodScope = scope;   // the COLLECTION cursor (AddParagraph registers method-locally)
            m.EntryPc = _paras.Count;
            if (m.Ctx.procedureDivision() is { } pd)
            {
                if (pd.declarativePart().Length > 0)
                    data.Edition.Error(DiagnosticCatalog.OoMethodDeclaratives,
                        $"class '{cls.Name}', method '{m.Name}': DECLARATIVES inside a method (ISO §14.2.1) "
                        + "are recognized but not yet implemented (owning roadmap phase: Phase 3, OO port)");
                foreach (var unit in pd.procedureUnit())
                {
                    if (unit.paragraphDefinition() is { } para)
                        AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
                    else if (unit.sectionDefinition() is { } section)
                    {
                        // A section inside a method is a method-local pc range (the legacy COBOL0116 reject is
                        // superseded: with per-method scopes the range cannot truncate or leak — trap #5).
                        var info = new SectionInfo(section.sectionName().GetText(), _paras.Count);
                        foreach (var p in section.paragraphDefinition())
                            AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                        info.EndPc = _paras.Count - 1;
                        scope.Sections.TryAdd(info.Name, info);
                    }
                }
            }
            m.EndPc = _paras.Count - 1;
            methods.Add(new BoundMethod(m.Name, m.CsName, m.EntryPc, m.EndPc));
        }
        Ctx.CurrentMethodScope = null;

        var bound = new List<BoundParagraph>(_paras.Count);
        for (int i = 0; i < _paras.Count; i++)
        {
            // The ordered quadruple (section → method scope → §11.7 GR5 data shadowing → cursor) is ONE
            // scoped operation (10s) — set coherently, restored coherently on dispose.
            using var _ = Ctx.EnterMethodScope(_paraSection[i], _paraMethod[i], i);
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in _paras[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            bound.Add(new BoundParagraph(_paras[i].Cobol, sentences));
        }
        return new BoundProgram(bound, 0, null, BuildEcFeatures(), methods);
    }

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
        core = Oo.OoWrapPropertyOps(core, mark);
        return Ec.EcWrap(s, core);
    }

    private BoundStatement BindStatementCore(Core.StatementContext s) => s switch
    {
        _ when s.displayStatement() is { } d => Accept.BindDisplay(d),
        _ when s.moveStatement() is { } m => Move.Bind(m),
        _ when s.addStatement() is { } a => Arith.BindAdd(a),
        _ when s.subtractStatement() is { } sub => Arith.BindSubtract(sub),
        _ when s.multiplyStatement() is { } mul => Arith.BindMultiply(mul),
        _ when s.divideStatement() is { } div => Arith.BindDivide(div),
        _ when s.computeStatement() is { } c => Arith.BindCompute(c),
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
        _ when s.invokeStatement() is { } inv => Oo.OoBindInvoke(inv),   // §14.9.23 — OO method invocation (2002+ grammar-gated)
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
        _ when s.raiseStatement() is { } ra => Ec.BindRaise(ra),               // EC model (ISO §14.9.29; 2002+ gated)
        _ when s.resumeStatement() is { } rs => Ec.BindResume(rs),             // EC model (ISO §14.9.33; 2002+ gated)
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

    // ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────

    internal static IEnumerable<Core.DataReferenceContext> DataRefs(IParseTree node)
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
