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

using CobolNet.Compiler.Oo;

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
    //    sibling collaborators directly through THIS host's accessors (e.g. host.Expr.BindExpr) — the 10t
    //    final wiring: this class is now dispatch + the mark/drain wrap protocol + the composition root. ──
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
    internal ReportWriterBinder Rw => _rwBinder ??= new ReportWriterBinder(Ctx, this);
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
    internal ControlFlowBinder ControlFlow => _controlFlowBinder ??= new ControlFlowBinder(Ctx, this);
    private SetBinder? _setBinder;
    internal SetBinder Set => _setBinder ??= new SetBinder(Ctx, this);
    private SearchBinder? _searchBinder;
    private SearchBinder Search => _searchBinder ??= new SearchBinder(Ctx, this);
    private SetAlterBinder? _setAlterBinder;
    internal SetAlterBinder Alter => _setAlterBinder ??= new SetAlterBinder(Ctx);

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

    /// <summary>The binder's public EC entry point (BinderDriver / the OO bind half configure the compilation
    /// group's TurnState + this unit's PROGRAM-ID per bound unit); the state lands on <c>ctx.EcState</c>.</summary>
    public void ConfigureEc(TurnState turn, string programName) => Ec.ConfigureEc(turn, programName);

    /// <summary>The compilation group's user-function signature table (FUNCTION-ID name → RETURNING +
    /// USING descriptions), built by the run-unit emitter between the DATA and PROCEDURE bind phases.
    /// Null in unit-test direct construction and in class-unit binders — every user-function reference
    /// there fails loud (COBOLNET1505).</summary>
    public IReadOnlyDictionary<string, UserFunctionSignature>? UserFunctions { get; set; }

    /// <summary>The programs an AS NESTED call in THIS unit may name (§14.9.4.3 SR15: directly contained in
    /// the caller, or a visible COMMON program), each with its bound PD-header formals — built per unit in
    /// BinderDriver.BindUnitProcedure, after every unit's DATA has bound (the UserFunctions precedent), so
    /// GR9's the-formal-decides mode derivation is a bind-time lookup (kb/Work PB131).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<LinkageFormal>>? NestedCallables { get; set; }

    /// <summary>The unit's RECURSIVE attribute — explicit, or inherited per §11.10.4 GR4 (kb/Work PB137:
    /// §14.9.7.3 SR1 bans COMMIT/ROLLBACK in a recursive source element). Set by BinderDriver.</summary>
    public bool UnitRecursive { get; set; }

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
    internal InitializeBinder Init => _initializeBinder ??= new InitializeBinder(Ctx, this);

    /// <summary>The group's pass-1 class symbol table (deep-dive D1) — set by the run-unit emitter before
    /// binding so INVOKE resolves classes/methods defined anywhere in the group. Null ⇔ empty group.</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The CLASS whose method bodies this binder is binding (per-binder-instance configuration, set
    /// ONCE at construction by <c>Oo/OoDriver</c> — init-only since P9 Step 6; null in a program unit) — the
    /// SELF/SUPER resolution root (§8.4.3.8: SELF resolves on the current
    /// class's chain, SUPER starts at its BASE; slice 3b).</summary>
    public OoClassSymbol? OoCurrentClass { get; init; }

    /// <summary>True when this binder binds the FACTORY roster (§11.4; per-binder-instance configuration,
    /// init-only since P9 Step 6): SELF/SUPER resolve over the FACTORY interface
    /// (§14.9.23.3 SR4f/h) and SELF|SUPER "NEW" binds the ACTIVE-CLASS creation form (§16.2.1).</summary>
    public bool OoInFactory { get; init; }


    /// <summary>True while binding a statement inside a METHOD body — the D8 context switch (GOBACK →
    /// method return; EXIT PROGRAM → §14.9.14.3 SR7 violation).</summary>
    internal bool InMethod => Ctx.CurrentMethodScope is not null;

    /// <summary>Install the D18 segment-materialization hook on this binder's <see cref="ReferenceResolver"/>
    /// (fix-queue PB17). Called from BOTH procedure-bind entry points — <see cref="Bind"/> and
    /// <see cref="BindMethodRoster"/> — because the primary constructor has no body to install it in, and because
    /// a resolver only needs the hook once a PROCEDURE DIVISION is actually being bound. Idempotent.
    /// <para>⛔ THIS IS THE ONLY EDGE FROM THE PROCEDURE BINDER BACK INTO THE RESOLVER, and it is a delegate, not
    /// a type reference: the dependency is one-way by design (<c>StatementBinder(DataBinder, ReferenceResolver)</c>),
    /// so a resolver that never gets one keeps the pre-D18 loud posture — which is exactly what the DATA-division
    /// throwaway resolvers in <c>DataBinder.Constants</c>/<c>Ptr</c> should have.</para></summary>
    private void AttachSegmentMaterializer() => refs.MaterializeSegment ??= MaterializeSubscriptSegment;

    /// <summary>Bind a program unit's PROCEDURE DIVISION into a <see cref="BoundProgram"/>.</summary>
    public BoundProgram Bind(Core.ProgramUnitContext program)
    {
        AttachSegmentMaterializer();
        // Report-section PRESENT WHEN conditions + VARYING expressions (§13.18.41/§13.18.64) bind through the
        // procedure-phase binders (they resolve ordinary data references) — before statement binding, and even
        // for a PD-less unit (the report emission does not require a PROCEDURE DIVISION).
        if (Ctx.Data.Reports.Count > 0) Rw.BindReportGroupClauses();
        if (program.procedureDivision() is not { } pd) return new BoundProgram([]);
        Ec.EcCollectPdRaising(pd);   // the PD-header RAISING list (§14.2.1) — consumed by the GOBACK/EXIT SR2 check
        var table = Ctx.Table;
        table.CollectParagraphs(pd);

        var bound = new List<BoundParagraph>(table.Paragraphs.Count);
        for (int i = 0; i < table.Paragraphs.Count; i++)
        {
            // Section + cursor as ONE scoped bind position (10s; no method overlay in a program unit) —
            // §8.4.2.2 in-section resolution + the RESUME SR1/SR2 / §15.30 location anchoring.
            using var _ = Ctx.EnterMethodScope(table.ParaSections[i], null, i);
            var sentences = new List<IReadOnlyList<BoundStatement>>();
            foreach (var sentence in table.Paragraphs[i].Sentences)
                sentences.Add(sentence.statement().Select(BindStatement).ToList());
            // The paragraph's LAST executable statement source line — the X3.23-1985 DEBUG-LINE for a FALL THROUGH
            // trigger (VCR 7.17; only used when the debug facility is active). 0 for an empty paragraph.
            int lastLine = table.Paragraphs[i].Sentences
                .SelectMany(s => s.statement()).LastOrDefault() is { } last ? Ctx.SourceLine(last) : 0;
            bound.Add(new BoundParagraph(table.Paragraphs[i].Cobol, sentences, lastLine));
        }
        // Append the exception-checking (Format-3) PERFORM handler pc-ranges (imp-2/3/4) above the whole main pc
        // space (ISO §14.9.28.4 GR17; §9.1-C). handlerBase == table.HandlerBasePc == the frozen main count, so
        // bound[handlerBase + k] is the k-th handler — matching the pc AddF3Handler stored on each BoundExceptionMatch.
        int handlerBase = bound.Count;
        bound.AddRange(table.F3Handlers);
        return new BoundProgram(bound, table.EntryPc, table.Declaratives, Ctx.EcState.BuildFeatures(),
            DebugSubjects: table.DebugSubjects.Count > 0 ? table.DebugSubjects : null,
            F3HandlerBasePc: table.F3Handlers.Count > 0 ? handlerBase : null,
            F3HandlerOwners: table.F3Handlers.Count > 0 ? table.F3HandlerOwners : null);
    }

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
        AttachSegmentMaterializer();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var methods = new List<BoundMethod>(roster.Count);
        var table = Ctx.Table;
        // scope → owning method, so the appended Format-3 handler pc-ranges (keyed by their binding-time
        // OoMethodScope) map back to each method's OoMethodBinding for the per-method slice (design SSOT §9.10).
        var scopeToMethod = new Dictionary<OoMethodScope, OoMethodSymbol>();

        foreach (var m in roster)
        {
            if (m.PropertySubject is not null)
            {
                // A PROPERTY-clause-SYNTHESIZED accessor: no COBOL body exists — the emitter renders the
                // direct field read/write (D-P1; observably identical to the §13.18.42 GR1/GR2 implicit
                // MOVE methods). It still occupies a roster slot (override/implements machinery applies).
                m.Binding!.EntryPc = table.Paragraphs.Count;
                m.Binding!.EndPc = table.Paragraphs.Count - 1;   // empty body by construction
                methods.Add(new BoundMethod(m.Name, m.CsName, m.Binding!.EntryPc, m.Binding!.EndPc));
                continue;
            }
            // A method IS a source element (§14.9.18.3 SR2/SR4a): its OWN PD-header RAISING partition
            // (D-EO8) becomes the binder's per-element sets while its body binds.
            Ec.EcLoadPdRaising(m.RaisingEcNames, m.RaisingClasses);
            // The method's DATA (LINKAGE → params-as-locals, LOCAL-STORAGE → locals, method-WS → statics) was
            // bound by DataBinder.OoBindMethodData before any body binds; here we link its name scope so the
            // per-pc switch below activates §11.7 GR5 shadowing while this method's statements bind.
            var scope = new OoMethodScope { Data = m.DataScope, MethodName = m.Name };
            scopeToMethod[scope] = m;         // §9.10 — handler pc-ranges map back to this method via its scope
            Ctx.CurrentMethodScope = scope;   // the COLLECTION cursor (AddParagraph registers method-locally)
            m.Binding!.EntryPc = table.Paragraphs.Count;
            if (m.Ctx.procedureDivision() is { } pd)
            {
                if (pd.declarativePart().Length > 0)
                    data.Edition.Error(DiagnosticCatalog.OoMethodDeclaratives,
                        $"class '{cls.Name}', method '{m.Name}': DECLARATIVES inside a method (ISO §14.2.1) "
                        + "are recognized but not yet implemented (owning roadmap phase: Phase 3, OO port)");
                // §14.4.3 — a method's procedure division may also open with unnamed sentences (same rule as a
                // program's; the header form is shared). They take the method's ENTRY pc, set just above.
                table.AddAnonymousParagraph(pd.sentence(), null, used);
                foreach (var unit in pd.procedureUnit())
                {
                    if (unit.paragraphDefinition() is { } para)
                        table.AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
                    else if (unit.sectionDefinition() is { } section)
                    {
                        // A section inside a method is a method-local pc range (the legacy COBOL0116 reject is
                        // superseded: with per-method scopes the range cannot truncate or leak — trap #5).
                        var info = new SectionInfo(section.sectionName().GetText(), table.Paragraphs.Count);
                        table.AddAnonymousParagraph(section.sentence(), info, used);
                        foreach (var p in section.paragraphDefinition())
                            table.AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                        info.EndPc = table.Paragraphs.Count - 1;
                        scope.Sections.TryAdd(info.Name, info);
                    }
                }
            }
            m.Binding!.EndPc = table.Paragraphs.Count - 1;
            methods.Add(new BoundMethod(m.Name, m.CsName, m.Binding!.EntryPc, m.Binding!.EndPc));
        }
        Ctx.CurrentMethodScope = null;

        var bound = new List<BoundParagraph>(table.Paragraphs.Count);
        for (int i = 0; i < table.Paragraphs.Count; i++)
        {
            // The ordered quadruple (section → method scope → §11.7 GR5 data shadowing → cursor) is ONE
            // scoped operation (10s) — set coherently, restored coherently on dispose.
            using var _ = Ctx.EnterMethodScope(table.ParaSections[i], table.ParaMethods[i], i);
            // §11.9.4 GR1 (kb/Work PB135): a method's own OPTIONS model governs ITS body only — swapped in
            // for this pc's sentences (the binder reads ctx.Options per statement; statements bind HERE, in
            // the per-pc pass, not in the roster registration loop above) and restored after.
            var savedOptions = data.Options;
            if (table.ParaMethods[i] is { } mScope && scopeToMethod.TryGetValue(mScope, out var mSym)
                && mSym.MethodOptions is { } mOpt)
                data.Options = mOpt;
            try
            {
                var sentences = new List<IReadOnlyList<BoundStatement>>();
                foreach (var sentence in table.Paragraphs[i].Sentences)
                    sentences.Add(sentence.statement().Select(BindStatement).ToList());
                bound.Add(new BoundParagraph(table.Paragraphs[i].Cobol, sentences));
            }
            finally { data.Options = savedOptions; }
        }
        // Append the exception-checking (Format-3) PERFORM handler pc-ranges (imp-2/3/4) above the whole class pc
        // space (ISO §14.9.28.4 GR17; design SSOT §9.10 — the SAME allocation the program path uses at Bind():163).
        // Empty until the F3-in-a-method un-reject (increment M4) — a class with no method-F3 stays byte-identical.
        int handlerBase = bound.Count;
        bound.AddRange(table.F3Handlers);
        StampMethodHandlerSlices(handlerBase, table.F3HandlerMethods, scopeToMethod, roster);
        return new BoundProgram(bound, 0, null, Ctx.EcState.BuildFeatures(), methods,
            F3HandlerBasePc: table.F3Handlers.Count > 0 ? handlerBase : null,
            F3HandlerOwners: table.F3Handlers.Count > 0 ? table.F3HandlerOwners : null);
    }

    /// <summary>Stamp each method's contiguous Format-3 handler sub-range onto its <see cref="OoMethodBinding"/>
    /// (design SSOT §9.10). The class handler pc-space partitions into per-method runs in method order — a method's
    /// handlers are all appended while its paragraphs bind (the pc-order second bind loop). Asserts that invariant
    /// LOUDLY (a future reorder that breaks contiguity must fail here, never emit a mis-sliced dispatch).</summary>
    private static void StampMethodHandlerSlices(int handlerBase, IReadOnlyList<OoMethodScope?> handlerMethods,
        Dictionary<OoMethodScope, OoMethodSymbol> scopeToMethod, IReadOnlyList<OoMethodSymbol> roster)
    {
        if (handlerMethods.Count == 0) return;
        int k = 0;
        while (k < handlerMethods.Count)
        {
            var sc = handlerMethods[k];
            int start = k;
            while (k < handlerMethods.Count && ReferenceEquals(handlerMethods[k], sc)) k++;
            if (sc is null || !scopeToMethod.TryGetValue(sc, out var m)) continue;   // a program-unit handler (no method)
            if (m.Binding!.HandlerCount != 0)
                throw new InvalidOperationException(
                    $"Format-3 handler contiguity invariant violated: method '{m.Name}' has a non-contiguous handler run "
                    + "(design SSOT §9.10 assumes per-method handler pc-ranges are appended consecutively)");
            m.Binding!.HandlerStartPc = handlerBase + start;
            m.Binding!.HandlerCount = k - start;
        }
    }

    // ── Statements ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind one statement, then apply the compile-time TurnState fold (StatementBinder.Exceptions.cs):
    /// a statement under enabled EC checking wraps in <see cref="BoundEcChecked"/>; checking-off binds the bare
    /// node — the zero-scaffolding gate (ISO §7.3.25.4 GR1 default OFF; deep-dive D10).</summary>
    private BoundStatement BindStatement(Core.StatementContext s)
    {
        // The diagnostic cursor (kb/Work PB82): every diagnostic reported while THIS statement binds — its own
        // verb binder, its operands, its conditions — carries the statement's source position; a nested statement
        // positions its own and restores this one on exit.
        using var _ = data.Edition.At(s);
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
        // ── Wave H — recognize-and-name the unsupported facilities (ISO §4.2.6 ¶3 makes the compile-time
        //    warning mechanism MANDATORY even where the facility itself need not be implemented). Each binds
        //    to BoundNop: the program compiles, runs, and the facility is inert — never a silent wrong answer,
        //    and never an EC (licensed off by §14.6.13.1.1). ──
        _ when s.mcsReceiveStatement() is not null || s.mcsSendStatement() is not null
            => BindUnsupportedFacility(DiagnosticCatalog.McsFacilityUnsupported),
        _ when s.commitFacilityStatement() is not null || s.rollbackFacilityStatement() is not null
            => BindCommitRollback(s.commitFacilityStatement() is not null),
        _ when s.validateFacilityStatement() is not null
            => BindUnsupportedFacility(DiagnosticCatalog.ValidateFacilityUnsupported),
        _ when s.continueStatement() is { } cont => ControlFlow.BindContinue(cont),
        _ when s.nextSentenceStatement() is not null => new BoundNextSentence(Ctx.SourceLine(s)),
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
        _ when s.suppressStatement() is { } rws => Rw.BindSuppress(rws),     // Report Writer (ISO §14.9.45)
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
        : rp.SECONDS() is not null ? new RetrySpec(RetryKind.Seconds, Expr.BindExpr(rp.arithmeticExpression()))
        : new RetrySpec(RetryKind.Times, Expr.BindExpr(rp.arithmeticExpression()));

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

    /// <summary>Recognize-and-name an unsupported facility: emit its NAMED §4.2.6/§4.2.13 warning once per
    /// site on the non-failing channel and bind to <see cref="BoundNop"/>. The program still compiles and
    /// runs; the facility is inert. This is the ONE mechanism for the band — do not add a parallel
    /// Lenient()/Unsupported() helper (feedback_one_mechanism_per_job); it routes through the same
    /// <c>EditionContext.Warning</c> channel the SCREEN non-support warning already uses.</summary>
    private BoundStatement BindUnsupportedFacility(DiagnosticDescriptor d)
    {
        Ctx.Edition.Warning(d.Code, d.Title);
        return new BoundNop();
    }

    /// <summary>COMMIT / ROLLBACK (kb/Work PB137): the §4.2.6 named warning keeps the documented A.3
    /// non-support posture, §14.9.7.3/§14.9.36.3 SR1 rejects the statement in a RECURSIVE source element
    /// at bind (a function and a method are ALWAYS recursive, §8.6.6), and the IDENTITY node lets the
    /// VersionConformancePass enforce SR2's SORT/MERGE procedure ban.</summary>
    private BoundStatement BindCommitRollback(bool isCommit)
    {
        string verb = isCommit ? "COMMIT" : "ROLLBACK";
        string cite = isCommit ? "§14.9.7.3 SR1" : "§14.9.36.3 SR1";
        Ctx.Edition.Warning(DiagnosticCatalog.CommitRollbackUnsupported.Code,
            DiagnosticCatalog.CommitRollbackUnsupported.Title);
        if (UnitRecursive || InMethod || UdfSelfName is not null)
        {
            Ctx.Edition.Error(DiagnosticCatalog.CommitRollbackContext,
                $"{verb} shall not be specified in a recursive source element (ISO {cite}; a function or "
                + "method is always recursive, §8.6.6)");
            return new BoundNop();
        }
        return new BoundCommitRollback(isCommit);
    }
}
