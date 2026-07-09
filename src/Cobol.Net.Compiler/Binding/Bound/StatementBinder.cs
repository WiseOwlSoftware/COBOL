// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Runtime;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// Builds the <see cref="BoundProgram"/> from a parsed program unit: it resolves every reference to a
/// <see cref="Place"/>, decodes every literal, and binds every expression / condition / statement into a bound node
/// exactly once (COBOLNET_DESIGN §2). The backend then renders the bound tree — it never re-walks the parse tree.
/// </summary>
public sealed partial class StatementBinder(DataBinder data, ReferenceResolver refs)
{
    private readonly List<(string Cobol, string Method, Core.SentenceContext[] Sentences)> _paras = [];
    private readonly Dictionary<string, int> _paraIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SectionInfo> _sections = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SectionInfo?> _paraSection = [];   // per-pc owning section (parallel to _paras)
    private SectionInfo? _currentSection;                     // the section whose paragraph is being bound

    /// <summary>A PROCEDURE DIVISION section (ISO §14.4.3): its contiguous paragraph pc range — paragraphs flatten
    /// into the one pc sequence in source order, so a section IS the inclusive range [StartPc, EndPc] (empty section
    /// ⇒ StartPc &gt; EndPc) — and its own paragraph map for qualified procedure-name resolution (ISO §8.4.2.2:
    /// <c>para OF section</c>, and the same-section implicit resolution of duplicated paragraph names).</summary>
    private sealed class SectionInfo(string name, int startPc)
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
    private (int Start, int End)? ResolveProcedure(Core.ProcedureNameContext ctx)
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
        int udfMark = _udfPendingCalls.Count;
        int mark = data.OoPendingPropertyOps.Count;
        var core = BindStatementCore(s);
        core = UdfWrapCalls(core, udfMark);
        core = OoWrapPropertyOps(core, mark);
        return EcWrap(s, core);
    }

    private BoundStatement BindStatementCore(Core.StatementContext s) => s switch
    {
        _ when s.displayStatement() is { } d => BindDisplay(d),
        _ when s.moveStatement() is { } m => BindMove(m),
        _ when s.addStatement() is { } a => BindAdd(a),
        _ when s.subtractStatement() is { } sub => BindSubtract(sub),
        _ when s.multiplyStatement() is { } mul => BindMultiply(mul),
        _ when s.divideStatement() is { } div => BindDivide(div),
        _ when s.computeStatement() is { } c => BindCompute(c),
        _ when s.ifStatement() is { } iff => BindIf(iff),
        _ when s.performStatement() is { } p => BindPerform(p),
        _ when s.setStatement() is { } set => BindSet(set),
        _ when s.searchStatement() is { } se => BindSearch(se),
        _ when s.evaluateStatement() is { } ev => BindEvaluate(ev),
        _ when s.inspectStatement() is { } ins => BindInspect(ins),
        _ when s.searchAllStatement() is { } sa => BindSearchAll(sa),
        _ when s.goToStatement() is { } g => BindGoTo(g),
        _ when s.alterStatement() is { } al => BindAlter(al),   // 85-only; rejected ≥2002 inside BindAlter (deleted by ISO/IEC 1989:2002)
        _ when s.exitStatement() is { } e => BindExit(e),
        _ when s.openStatement() is { } o => BindOpen(o),
        _ when s.closeStatement() is { } c => BindClose(c),
        _ when s.writeStatement() is { } w => BindWrite(w),
        _ when s.readStatement() is { } r => BindRead(r),
        _ when s.rewriteStatement() is { } rw => BindRewrite(rw),
        _ when s.startStatement() is { } st => KeyedBindStart(st),
        _ when s.deleteStatement() is { } del => KeyedBindDelete(del),
        _ when s.deleteFileStatement() is { } dfs => KeyedBindDeleteFile(dfs),
        _ when s.unlockStatement() is { } ul => BindUnlock(ul),
        _ when s.stringStatement() is { } sstr => BindString(sstr),
        _ when s.unstringStatement() is { } suns => BindUnstring(suns),
        _ when s.acceptStatement() is { } ac => BindAccept(ac),
        _ when s.initializeStatement() is { } ini => BindInitialize(ini),
        _ when s.continueStatement() is not null => new BoundNop(),
        _ when s.nextSentenceStatement() is not null => new BoundNextSentence(),
        // STOP RUN vs STOP literal (X3.23-1985 Format 2 — communicate to the operator, then CONTINUE): the
        // literal form no longer silently binds as STOP RUN (the DEVLOG-578 mis-bind; edition-gated ≥2002 by
        // the validator, its 85 semantics implemented via BoundStopLiteral).
        _ when s.stopStatement() is { } stop => BindStop(stop),
        _ when s.gobackStatement() is { } gb => CallBindGoback(gb),   // §14.9.18 — called-program return; 2002+ gated
        _ when s.invokeStatement() is { } inv => OoBindInvoke(inv),   // §14.9.23 — OO method invocation (2002+ grammar-gated)
        _ when s.callStatement() is { } call => CallBindCall(call),
        _ when s.cancelStatement() is { } cancel => CallBindCancel(cancel),
        _ when s.entryStatement() is not null => new BoundUnsupported("ENTRY (ISO/IEC 1989 defines no ENTRY statement — vendor extension; interprogram design)"),
        // ENTER language-name [routine-name] (X3.23-1985 Nucleus, deleted by ISO 2002 — 0902-gated ≥2002 by
        // the EditionValidator, VCR Table 7 row 7.16): comment-equivalent when only COBOL is supported — the
        // conforming '85 posture; accepted-inert as a no-op.
        _ when s.enterStatement() is not null => new BoundNop(),
        _ when s.sortStatement() is { } srt => BindSort(srt),
        _ when s.mergeStatement() is { } mrg => BindMerge(mrg),
        _ when s.releaseStatement() is { } rls => BindRelease(rls),
        _ when s.returnStatement() is { } ret => BindReturn(ret),
        _ when s.initiateStatement() is { } rwi => RwBindInitiate(rwi),     // Report Writer (ISO §14.9.21)
        _ when s.generateStatement() is { } rwg => RwBindGenerate(rwg),     // Report Writer (ISO §14.9.16)
        _ when s.terminateStatement() is { } rwt => RwBindTerminate(rwt),   // Report Writer (ISO §14.9.46)
        _ when s.raiseStatement() is { } ra => BindRaise(ra),               // EC model (ISO §14.9.29; 2002+ gated)
        _ when s.resumeStatement() is { } rs => BindResume(rs),             // EC model (ISO §14.9.33; 2002+ gated)
        _ when s.allocateStatement() is { } al => PtrBindAllocate(al),      // dynamic storage (ISO §14.9.3; Phase-4b inc 2)
        _ when s.freeStatement() is { } fr => PtrBindFree(fr),              // dynamic storage (ISO §14.9.15; Phase-4b inc 2)
        _ => new BoundUnsupported($"statement '{FirstToken(s)}'"),
    };

    /// <summary>STOP RUN [WITH {NORMAL|ERROR} [STATUS …]] / STOP literal (ISO §14.9.42). The status phrase is a
    /// COBOL-2002 introduction — bind-time introduction gate (rearch bind-time migration Cluster 4; the parse-time
    /// {is2002()}? predicate is gone). The phrase has no runtime effect in this compiler, so the gate is its only
    /// binder obligation.</summary>
    private BoundStatement BindStop(Core.StopStatementContext stop)
    {
        if (stop.stopStatusPhrase() is not null)
            ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.StopRunStatus2002, "the STOP RUN … WITH NORMAL/ERROR STATUS phrase");
        return stop.literal() is { } slit
            ? new BoundStopLiteral(DecodeCobolString(slit.GetText()))
            : new BoundStop();
    }

    private BoundStatement BindGoTo(Core.GoToStatementContext g)
    {
        var names = g.procedureName();
        if (g.dataReference() is { } sel && names.Length >= 1)   // GO TO p1 p2 … DEPENDING ON sel
        {
            var targets = new List<int>();
            foreach (var n in names)
            {
                // A section target transfers to its first paragraph (ISO §14.9.17 GR1).
                if (ResolveProcedure(n) is not { } range) return new BoundUnsupported($"GO TO unknown procedure '{n.GetText()}'{OoScopeHint}");
                targets.Add(range.Start);
            }
            return new BoundGoToDepending(FieldOperand(sel), targets);
        }
        if (names.Length == 0) return AlterBindBareGoTo(g);   // the 85-only target-less GO TO (ALTER subsystem)
        if (ResolveProcedure(names[0]) is not { } target)
            return new BoundUnsupported($"GO TO unknown procedure '{names[0].GetText()}'{OoScopeHint}");
        return AlterGoTo(g, target.Start);   // alterable when the owning paragraph is an ALTER target, else plain GO TO
    }

    private BoundStatement BindExit(Core.ExitStatementContext e)
    {
        if (e.PARAGRAPH() is not null) return new BoundExitParagraph();
        if (e.PERFORM() is not null) return new BoundExitPerform(e.CYCLE() is not null);
        if (e.PROGRAM() is not null)   // §14.9.14 GR2/GR3 — CONTINUE in a non-called program, return-to-caller in a called one (runtime-contextual)
        {
            if (InMethod)   // §14.9.14.3 SR7: EXIT PROGRAM only in a PROGRAM procedure division
            {
                data.Edition.Error("COBOLNET0827",
                    "EXIT PROGRAM may be specified only in a program procedure division, not in a method "
                    + "(ISO §14.9.14.3 SR7 — a method returns via GOBACK)");
                return new BoundNop();
            }
            if (e.raisingPhrase() is { } raising)   // Format 2's RAISING tail (§14.9.14.2) — re-raise in the activator
                return EcBindRaising(raising, e.Start.Line, "EXIT PROGRAM") is { } r
                    ? new BoundExitProgram(r)
                    : new BoundUnsupported("EXIT PROGRAM RAISING identifier (exception object — the OO wave; ISO §14.9.14.3)");
            return new BoundExitProgram();
        }
        if (e.SECTION() is not null) return new BoundUnsupported("EXIT SECTION");        // needs section bounds — later
        if (e.METHOD() is not null) return OoBindExitMethod(e);   // method-return synonym ≤2014; 0902 at 2023 (validator)
        if (e.FUNCTION() is not null) return UdfBindExitFunction(e);   // function-return synonym ≤2014; 0900/0902 window (validator)
        return new BoundNop();   // bare EXIT
    }

    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────

    private BoundStatement BindOpen(Core.OpenStatementContext o)
    {
        var opens = new List<(FileModel, BoundOpenMode, string?)>();
        SharingMode? sharing = null;
        RetrySpec? retry = null;
        foreach (var clause in o.openClause())
        {
            BoundOpenMode mode = MapOpenMode(clause.openMode());
            // OPEN SHARING phrase (ISO §14.9.27) overrides the SELECT SHARING clause for this OPEN; the RETRY
            // phrase (ISO §14.7.9) governs a locked-file re-attempt. Both are per-statement.
            if (clause.sharingPhrase() is { } sp)   // OPEN SHARING phrase (§14.9.27) — COBOL-2002, bind-time gate (residue migration #3; the {is2002()}? predicate + reverse-signature arm are gone)
            {
                ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.FileSharingClause2002, "the OPEN SHARING phrase");
                if (sp.sharingMode() is { } sm) sharing = MapSharingMode(sm);
            }
            GateRetryIntro(clause.retryPhrase());   // §14.7.9 introduction gate (residue migration #4)
            if (clause.retryPhrase() is { } rp) retry = BindRetry(rp);
            foreach (var spec in clause.openFileSpec())
            {
                string name = spec.dataReference().GetText();
                if (!data.FilesByName.TryGetValue(name, out var file))
                    return new BoundUnsupported($"OPEN of undeclared file '{name}'");
                // §14.9.27 SR8: OPEN … SHARING WITH ALL OTHER (clause or phrase) requires a LOCK MODE clause.
                var effShare = sharing ?? file.Sharing;
                if (effShare is SharingMode.AllOther && file.LockMode is null)
                    data.Edition.Error("COBOLNET1512", $"OPEN of file '{name}' with SHARING WITH ALL OTHER "
                        + "requires the file to have a LOCK MODE clause (ISO §14.9.27 SR8)");
                opens.Add((file, mode, UnsupportedOrg(file, "OPEN")));
            }
        }
        return new BoundOpen(opens) { SharingOverride = sharing, Retry = retry };
    }

    /// <summary>Map a SHARING mode context (ISO §12.4.5.15) at the binder layer (the DataBinder twin serves the
    /// SELECT clause).</summary>
    private static SharingMode MapSharingMode(Core.SharingModeContext m) =>
        m.READ() is not null ? SharingMode.ReadOnly
        : m.NO() is not null ? SharingMode.NoOther
        : SharingMode.AllOther;

    /// <summary>The RETRY phrase (ISO §14.7.9) introduction gate — COBOL-2002, on OPEN/READ/WRITE/REWRITE/DELETE/
    /// DELETE FILE. RETRY parses at all editions (superset — the parse-time <c>{is2002()}?</c> predicates are gone;
    /// the OPEN site uses a forward-detect); the gate fires HERE, once per statement carrying the phrase, so a
    /// below-2002 RETRY is an exact COBOLNET0900 (residue migration #4). Kept separate from <see cref="BindRetry"/>
    /// because most verbs treat the phrase as a documented no-op residue and never bind it.</summary>
    private void GateRetryIntro(Core.RetryPhraseContext? rp)
    {
        if (rp is not null)
            ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.RetryPhrase2002, "the RETRY phrase");
    }

    /// <summary>Bind a RETRY phrase (ISO §14.7.9). The n-TIMES amount is a bounded re-attempt count; FOR n
    /// SECONDS / FOREVER are single-run-unit no-ops (no competing process releases — named residue).</summary>
    private RetrySpec BindRetry(Core.RetryPhraseContext rp) =>
        rp.FOREVER() is not null ? new RetrySpec(RetryKind.Forever, null)
        : rp.SECONDS() is not null ? new RetrySpec(RetryKind.Seconds, BindExpr(rp.arithmeticExpression()))
        : new RetrySpec(RetryKind.Times, BindExpr(rp.arithmeticExpression()));

    private BoundStatement BindClose(Core.CloseStatementContext c)
    {
        var closes = new List<(FileModel, BoundCloseKind)>();
        foreach (var phrase in c.closeFilePhrase())
        {
            string name = phrase.fileName().GetText();
            if (!data.FilesByName.TryGetValue(name, out var file))
                return new BoundUnsupported($"CLOSE of undeclared file '{name}'");
            BoundCloseKind kind = phrase.closeOption() is { } opt
                ? opt.LOCK() is not null ? BoundCloseKind.WithLock
                : opt.REEL() is not null || opt.UNIT() is not null ? BoundCloseKind.ReelUnit
                : BoundCloseKind.Normal
                : BoundCloseKind.Normal;
            closes.Add((file, kind));
        }
        return new BoundClose(closes);
    }

    private BoundStatement BindWrite(Core.WriteStatementContext w)
    {
        GateRetryIntro(w.retryPhrase());   // §14.7.9 introduction gate — before the sequential/keyed split (residue migration #4)
        Place? record = null;
        FileModel? file = null;
        if (w.recordName()?.dataReference() is { } rn && refs.Resolve(rn) is { } place)
        {
            record = place;
            file = FileOfRecord(place);
        }
        else if (w.fileName() is { } fn && data.FilesByName.TryGetValue(fn.GetText(), out var f) && f.AreaRecord is { } far)
        {
            // The file-name fallback has no named record — write the WHOLE record area through the largest
            // record's view (FileModel.AreaRecord, ISO §13.4.2).
            file = f;
            record = refs.ResolveItem(far);
        }
        if (file is null || record is null)
            return new BoundUnsupported($"WRITE record '{w.recordName()?.GetText() ?? w.fileName()?.GetText()}' not resolvable to a file");
        CheckRecordLockPhrase(file, w.recordLockPhrase(), "WRITE");   // §14.9.51 SR22 → COBOLNET1512
        if (!file.IsSequential) return KeyedBindWrite(w, file, record);   // relative/indexed WRITE (ISO 14.9.51 GR29-42)

        // END-OF-PAGE phrases (ISO §14.9.51 GR27b/GR28): blocks[0] = AT EOP, blocks[1] = NOT AT EOP — the grammar
        // rule `writeAtEndOfPage : AT? (END_OF_PAGE|EOP) statementBlock (NOT AT? (END_OF_PAGE|EOP) statementBlock)?`
        // fixes that order (the readAtEnd block shape).
        List<BoundStatement>? atEop = null, notAtEop = null;
        if (w.writeAtEndOfPage() is { } eop)
        {
            // SR19 (the silent-drop bug class): the END-OF-PAGE / NOT END-OF-PAGE phrase requires a LINAGE clause
            // in the file's file description entry — a bind-time rejection, never a dropped branch.
            if (file.Linage is null)
                data.Edition.Error("COBOLNET0860", $"WRITE … END-OF-PAGE on file '{file.CobolName}', whose file "
                    + "description entry has no LINAGE clause (ISO §14.9.51 SR19)");
            // SR18: ADVANCING PAGE and END-OF-PAGE shall not both be specified in a single WRITE statement.
            if (w.writeBeforeAfter()?.PAGE() is not null)
                data.Edition.Error("COBOLNET0861", "WRITE … ADVANCING PAGE with an END-OF-PAGE phrase: the two "
                    + "shall not both be specified in a single WRITE statement (ISO §14.9.51 SR18)");
            var blocks = eop.statementBlock();
            if (blocks.Length >= 1) atEop = BindBlocks([blocks[0]]);
            if (blocks.Length >= 2) notAtEop = BindBlocks([blocks[1]]);
        }
        // SR13: with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES mnemonic (the
        // implementor positioning rules and the logical-page model are mutually exclusive).
        if (file.Linage is not null && w.writeBeforeAfter() is { } wba && wba.dataReference() is { } mref
            && AcceptMnemonics(wba).ContainsKey(mref.GetText()))
            data.Edition.Error("COBOLNET0862", $"WRITE … ADVANCING mnemonic-name on file '{file.CobolName}', whose "
                + "file description entry contains a LINAGE clause (ISO §14.9.51 SR13)");

        return new BoundWrite(file, record, WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal()),
            BindAdvancing(w.writeBeforeAfter()), UnsupportedOrg(file, "WRITE"), atEop, notAtEop);
    }

    private BoundStatement BindRead(Core.ReadStatementContext r)
    {
        GateRetryIntro(r.retryPhrase());   // §14.7.9 introduction gate — before the sequential/keyed split (residue migration #4)
        string name = r.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"READ of undeclared file '{name}'");
        if (!file.IsSequential) return KeyedBindRead(r, file);   // relative/indexed READ F1/F2 (ISO 14.9.30; KeyedIo partial)
        CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ");   // §14.9.30 SR3/SR4 → COBOLNET1512 (sequential leg: SR-validated, effect residue)
        Place? into = r.readInto()?.dataReference() is { } d ? refs.Resolve(d) : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
        {
            var blocks = ae.statementBlock();
            if (blocks.Length >= 1) atEnd = BindBlocks([blocks[0]]);
            if (blocks.Length >= 2) notAtEnd = BindBlocks([blocks[1]]);
        }
        return new BoundRead(file, into, atEnd, notAtEnd, UnsupportedOrg(file, "READ"));
    }

    private BoundStatement BindRewrite(Core.RewriteStatementContext rw)
    {
        GateRetryIntro(rw.retryPhrase());   // §14.7.9 introduction gate — before the sequential/keyed split (residue migration #4)
        Place? record = rw.recordName()?.dataReference() is { } rn ? refs.Resolve(rn) : null;
        FileModel? file = record is not null ? FileOfRecord(record) : null;
        if (file is null || record is null)
            return new BoundUnsupported($"REWRITE record '{rw.recordName()?.GetText()}' not resolvable to a file");
        CheckRecordLockPhrase(file, rw.recordLockPhrase(), "REWRITE");   // §14.9.35 SR4 → COBOLNET1512
        if (!file.IsSequential) return KeyedBindRewrite(rw, file, record);   // relative/indexed REWRITE (ISO 14.9.35 GR18-25)
        return new BoundRewrite(file, record, WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal()),
            UnsupportedOrg(file, "REWRITE"));
    }

    /// <summary>The FROM operand of a WRITE/REWRITE (a data reference or a literal), or null when absent.</summary>
    private BoundOperand? WriteSource(Core.DataReferenceContext? dref, Core.LiteralContext? lit) =>
        lit is not null ? LiteralOperand(lit) : dref is not null ? FieldOperand(dref) : null;

    /// <summary>Bind the <c>{BEFORE|AFTER} ADVANCING …</c> phrase (ISO §14.9.46), or null for a plain WRITE.
    /// An ADVANCING operand naming a SPECIAL-NAMES mnemonic (<c>XXXXX073 IS MNEMONIC-NAME</c>, SQ207M) positions
    /// per the IMPLEMENTOR's rules for the associated feature (§14.9.46 GR — mnemonic-name-1); this
    /// implementation's rule, inherited from the legacy oracle and encoded by the NIST goldens, is a ZERO-line
    /// advance (the write lands on the current line).</summary>
    private BoundAdvancing? BindAdvancing(Core.WriteBeforeAfterContext? ctx)
    {
        if (ctx is null) return null;
        bool before = ctx.BEFORE() is not null;
        if (ctx.PAGE() is not null) return new BoundAdvancing(before, true, null);
        BoundOperand lines =
            ctx.integerLiteral() is { } il ? new BoundNumericLiteral(il.GetText())
            : ctx.dataReference() is { } d ? AcceptMnemonics(ctx).ContainsKey(d.GetText())
                ? new BoundNumericLiteral("0") : FieldOperand(d)
            : ctx.literal() is { } lit ? LiteralOperand(lit)
            : new BoundNumericLiteral("1");
        return new BoundAdvancing(before, false, lines);
    }

    private static BoundOpenMode MapOpenMode(Core.OpenModeContext m) =>
        m.OUTPUT() is not null ? BoundOpenMode.Output
        : m.EXTEND() is not null ? BoundOpenMode.Extend
        : m.I_O() is not null ? BoundOpenMode.IO
        : BoundOpenMode.Input;

    /// <summary>The owning <see cref="FileModel"/> of a record reference: the file whose records include the
    /// reference's top-level (01) record. Null if the reference is not an FD record.</summary>
    private FileModel? FileOfRecord(Place record)
    {
        DataItem root = record.Item;
        while (root.Parent is { } p) root = p;
        foreach (var f in data.Files)
            if (f.Records.Contains(root)) return f;
        // An inherited GLOBAL FD's record (ISO §13.18.30 — the record-names of a GLOBAL FD are GLOBAL names):
        // the owning file is a CONTAINER's FileModel, present in this unit only through the FilesByName merge
        // (CallBindUnit) — a contained program's WRITE/REWRITE of the owner's record resolves to the owner's
        // ONE connector (IC233A's family; never a second mapping mechanism).
        foreach (var f in data.FilesByName.Values)
            if (f.Records.Contains(root)) return f;
        return null;
    }

    /// <summary>A loud-reason string when <paramref name="file"/>'s organization is not yet implemented (relative /
    /// indexed in the sequential slice), so the verb emits a runtime not-implemented guard; null when supported.</summary>
    private static string? UnsupportedOrg(FileModel file, string verb) =>
        // A sort-merge (SD) file may be referenced ONLY by SORT/MERGE/RELEASE/RETURN (ISO §13.4.6 SR3/SR4).
        // Every ISO §12.4.5.10 organization (sequential, line sequential, relative, indexed) now has a dedicated
        // bind/emit path — the relative/indexed verbs route through the KeyedIo partial and OPEN/CLOSE flow
        // through the CobolFile facade's keyed registries. Retained as the single seam a future organization
        // gates on (loud, never silent).
        file.IsSortMerge ? $"{verb} on sort-merge file '{file.CobolName}' — an SD file-name may appear only in SORT/MERGE/RELEASE/RETURN (ISO §13.4.6 SR3/SR4)"
        : null;

    private BoundStatement BindDisplay(Core.DisplayStatementContext display)
    {
        var ops = new List<BoundOperand>();
        foreach (IParseTree child in Children(display))
            switch (child)
            {
                case Core.LiteralContext lit: ops.Add(LiteralOperand(lit)); break;
                case Core.DataReferenceContext dref: ops.Add(FieldOperand(dref)); break;
                // DISPLAY FUNCTION … (ISO §8.4.4.1 — an identifier includes a function-identifier; §14.9.11.2).
                case Core.FunctionCallContext fc: ops.Add(IntrinsicOperand(fc)); break;
            }
        return new BoundDisplay(ops, display.displayNoAdvancing() is not null);
    }

    private BoundStatement BindMove(Core.MoveStatementContext move)
    {
        if (move.CORRESPONDING() is not null || move.CORR() is not null)   // Format 2 — BOTH tokens (§14.9.25.3 SR11)
            return BindCorresponding(CorrVerb.Move, move.dataReference(), CobolRounding.Truncation, null);
        if (move.moveSendingOperand() is not { } send || move.moveReceivingPhrase()?.dataReferenceList() is not { } targets)
            return new BoundUnsupported("MOVE CORRESPONDING / unsupported MOVE form");
        BoundOperand source = send.literal() is { } lit ? LiteralOperand(lit)
            : send.dataReference() is { } dref ? FieldOperand(dref)
            // MOVE FUNCTION … TO targets (ISO §14.9.25 + §15.2 — a function is a sending item of its category).
            : send.functionCall() is { } sfc ? IntrinsicOperand(sfc)
            : new BoundOperandError("MOVE source");
        var resolved = ResolveTargets(targets.dataReference());
        // The §14.9.25.3 SR5 edition gates (VCR rows 1 / 92 / 128) + the SR1 class-index check: an
        // alphanumeric figurative or ALL "literal" moving to a numeric / numeric-edited receiver — 0902
        // removed at 2023 except the digit-only-ALL-to-integer case, which is 0903 obsolete
        // (StatementBinder.MoveFigurative.cs).
        MoveFigurativeEditionGates(source, resolved);
        // The Table 16 boolean/national legality arms + SR7 (Phase 4a — StatementBinder.MoveFigurative.cs).
        MoveCategoryLegality(source, resolved);
        // A ref-mod slice store on a numeric-DISPLAY receiver needs image backing for ANY sender (§8.4.2.4;
        // the W2 adversarial-review round-trip-loss fix — see MarkRefModStoreImage).
        MarkRefModStoreImage(resolved);
        CheckStrongMove(source, resolved);   // §14.9.25.3 SR2 — a strongly-typed group receiver wants a same-type sender (D17 inc 2)
        return new BoundMove(source, resolved);
    }

    /// <summary>ISO §14.9.25.3 SR2 (data-model D17): if a receiving operand is a strongly-typed group, the sending
    /// operand shall be a group item of the SAME type (§8.5.3.3 — a strong record accepts only a same-type whole-record
    /// source; its individual fields are still set by ordinary field MOVEs, and a strong-type SENDER to a non-strong
    /// receiver is permitted per Table 16). A mismatch → COBOLNET1533.</summary>
    private void CheckStrongMove(BoundOperand source, IReadOnlyList<Place> receivers)
    {
        DataItem? sender = source is BoundFieldOperand sf ? sf.Place.Item : null;
        foreach (var r in receivers)
        {
            if (!r.Item.IsStrongGroup) continue;
            if (sender is null || !DataItem.SameStrongType(sender, r.Item))
                data.Edition.Error(DiagnosticCatalog.StrongMoveMismatch, "MOVE to strongly-typed group "
                    + $"'{r.Item.CobolName ?? r.Item.CsName}': the sending operand shall be a group item of the same "
                    + "type (ISO §14.9.25.3 SR2 / §8.5.3.3)");
        }
    }

    private BoundStatement BindAdd(Core.AddStatementContext add)
    {
        if (add.addOperandList() is not { } operands) return BindAddCorresponding(add);   // Format 3 (§14.9.2.2)
        var addends = operands.addOperand().Select(BindExpr).ToList();
        var sizeErr = BindSizeError(add.arithmeticOnSizeError());
        if (add.addGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §14.9.1 Format 3: the TO operand is an addend, NOT a
            // receiver; only the GIVING operands receive). Previously the TO operand was dropped from the sum.
            if (add.addToPhrase() is { } toAddend)
                addends.AddRange(DataRefs(toAddend).Select(BindExpr));
            var givingRecv = Receivers(giving.receivingArithmeticOperand());
            CheckComposite("ADD", addends, givingRecv);
            return new BoundAddGiving(addends, givingRecv, sizeErr);
        }
        if (add.addToPhrase() is { } to)
        {
            var recv = Receivers(to.receivingArithmeticOperand());
            CheckComposite("ADD", addends, recv);
            return new BoundAddTo(addends, recv, sizeErr);
        }
        return new BoundUnsupported("ADD form");
    }

    private BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.subtractOperandList() is not { } operands) return BindSubtractCorresponding(sub);   // Format 3 (§14.9.44.2)
        var minuends = operands.subtractOperand().Select(BindExpr).ToList();
        var sizeErr = BindSizeError(sub.arithmeticOnSizeError());
        if (sub.subtractGivingPhrase() is { } giving && sub.subtractFromPhrase()?.subtractFromOperand() is { } from)
        {
            var fromX = BindExpr(from);
            var recv = Receivers(giving.receivingArithmeticOperand());
            CheckComposite("SUBTRACT", [.. minuends, fromX], recv);
            return new BoundSubtractGiving(minuends, fromX, recv, sizeErr);
        }
        if (sub.subtractFromPhrase()?.subtractFromOperand() is { } targets)
        {
            var recv = Receivers(targets.receivingArithmeticOperand());
            CheckComposite("SUBTRACT", minuends, recv);
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
            CheckComposite("MULTIPLY", [a, b], recv);
            return new BoundMultiplyGiving(a, b, recv, sizeErr);
        }
        // In-place: each BY operand is itself the receiver (target ← target × a).
        var byRecv = Receivers(byOps);
        CheckComposite("MULTIPLY", [a], byRecv);
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
            CheckComposite("DIVIDE", [dividend, divisor], quotients);
            return new BoundDivideRemainder(dividend, divisor, quotients[0], r, sizeErr);
        }

        if (div.divideIntoPhrase() is { } into)
        {
            if (div.divideGivingPhrase() is { } giving)
            {
                var dividendX = BindExpr(into.divideIntoOperand());
                var recv = Receivers(giving.receivingArithmeticOperand());
                CheckComposite("DIVIDE", [dividendX, a], recv);
                return new BoundDivideGiving(dividendX, a, recv, sizeErr);
            }
            var intoRecv = Receivers(into.divideIntoOperand().receivingArithmeticOperand());
            CheckComposite("DIVIDE", [a], intoRecv);
            return new BoundDivideInto(a, intoRecv, sizeErr);   // target ← target ÷ a
        }
        if (div.divideByPhrase() is { } byPhrase && div.divideGivingPhrase() is { } gv)
        {
            var divisorX = BindExpr(byPhrase.divideOperand());
            var recv = Receivers(gv.receivingArithmeticOperand());
            CheckComposite("DIVIDE", [a, divisorX], recv);
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

    private BoundStatement BindIf(Core.IfStatementContext iff)
    {
        var thenBlocks = new List<Core.StatementBlockContext>();
        var elseBlocks = new List<Core.StatementBlockContext>();
        bool seenElse = false;
        foreach (var child in Children(iff))
        {
            if (child is ITerminalNode t && t.Symbol.Type == CobolLexer.ELSE) seenElse = true;
            else if (child is Core.StatementBlockContext sb) (seenElse ? elseBlocks : thenBlocks).Add(sb);
        }
        return new BoundIf(BindCondition(iff.condition()), BindBlocks(thenBlocks), BindBlocks(elseBlocks));
    }

    private List<BoundStatement> BindBlocks(IEnumerable<Core.StatementBlockContext> blocks) =>
        blocks.SelectMany(b => b.statement()).Select(BindStatement).ToList();

    // ── ON SIZE ERROR phrase (ISO §14.7.5) ───────────────────────────────────────────────────────────────────

    private SizeErrorPhrase? BindSizeError(Core.ArithmeticOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), StartsWithNot(ctx));

    private SizeErrorPhrase? BindSizeError(Core.ComputeOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), StartsWithNot(ctx));

    /// <summary>Build the phrase from the (1 or 2) statement blocks. Both <c>arithmeticOnSizeError</c> and
    /// <c>computeOnSizeError</c> have the shape <c>ON SIZE ERROR b1 (NOT ON SIZE ERROR b2)? | NOT ON SIZE ERROR b1</c>;
    /// the NOT-only alternative is detected by its leading <c>NOT</c> token.</summary>
    private SizeErrorPhrase BuildSizeError(Core.StatementBlockContext[] blocks, bool notOnly)
    {
        if (notOnly) return new SizeErrorPhrase(null, BindBlocks([blocks[0]]));
        var onErr = blocks.Length >= 1 ? BindBlocks([blocks[0]]) : null;
        var notErr = blocks.Length >= 2 ? BindBlocks([blocks[1]]) : null;
        return new SizeErrorPhrase(onErr, notErr);
    }

    private static bool StartsWithNot(IParseTree ctx) =>
        ctx.ChildCount > 0 && ctx.GetChild(0) is ITerminalNode t && t.Symbol.Type == CobolLexer.NOT;

    private BoundStatement BindPerform(Core.PerformStatementContext p)
    {
        var names = p.procedureName();
        if (names.Length == 0)
            return new BoundInlinePerform(BindPerformControl(p), BindBlocks(p.statementBlock()));

        // Out-of-line: the resolved pc range [start, end] — a paragraph (start==end), a SECTION (its whole
        // paragraph range, ISO §14.9.28 — first statement of its first paragraph through last of its last), or
        // the THRU composition (first procedure's start through the last procedure's end).
        if (ResolveProcedure(names[0]) is not { } first)
            return new BoundUnsupported($"PERFORM unknown procedure '{names[0].GetText()}'{OoScopeHint}");
        (int start, int end) = first;
        if ((p.THRU() is not null || p.THROUGH() is not null) && names.Length >= 2)
        {
            if (ResolveProcedure(names[1]) is not { } thru) return new BoundUnsupported($"PERFORM THRU unknown procedure '{names[1].GetText()}'{OoScopeHint}");
            // An INVERTED range (the THRU procedure physically precedes the first, reached by GO TO — NC102A
            // PFM-TEST-F1-10) is legal: the dispatcher returns when the exit procedure completes, wherever it is.
            end = thru.End;
        }
        else if (start > end)
            return new BoundNop();   // PERFORM of an EMPTY section runs nothing (no first statement, ISO §14.9.28)

        return new BoundOutOfLinePerform(start, end, BindPerformControl(p));
    }

    /// <summary>Bind the OPTIONAL control phrase (TIMES / UNTIL / VARYING) of a PERFORM. Per ISO §14.9.28 the phrase
    /// is independent of the THRU range (general format: <c>PERFORM proc-1 [THRU proc-2] [times|until|varying]</c>),
    /// but the grammar exposes it in two shapes: a direct child (<c>PERFORM proc TIMES</c>, alternatives without
    /// THRU) or wrapped in <c>performOptions</c> (the <c>PERFORM proc THRU proc [performOptions]</c> alternative and
    /// the inline <c>performOptions+</c> form). Resolving only the direct child dropped the count/condition on a THRU
    /// range, silently running the range once instead of N times (§14.9.28 GR9) — the NC106A/NC176A defect
    /// (DEVLOG 514). This one resolver handles every shape for both inline and out-of-line PERFORM.</summary>
    private BoundPerformControl BindPerformControl(Core.PerformStatementContext p)
    {
        var opt = p.performOptions().FirstOrDefault();
        if ((p.performTimes() ?? opt?.performTimes()) is { } t) return new PerformTimes(CountOperand(t));
        if ((p.performUntil() ?? opt?.performUntil()) is { } u) return new PerformUntil(BindCondition(u.condition()), u.AFTER() is not null);
        if ((p.performVarying() ?? opt?.performVarying()) is { } v) return BindVarying(v);
        return new PerformOnce();
    }

    /// <summary>Bind a VARYING phrase (ISO §14.9.28 Format 4) into its ordered induction levels — the VARYING
    /// level first, then each AFTER level left-to-right. TEST AFTER is the phrase's own <c>TEST AFTER</c> (the
    /// AFTER tokens of the after-levels live in their sub-contexts, not here).</summary>
    private BoundPerformControl BindVarying(Core.PerformVaryingContext v)
    {
        var levels = new List<VaryingLevel>();
        if (BindVaryingLevel(v.dataReference(), v.arithmeticExpression(), v.condition()) is not { } head)
            return Unsupported($"PERFORM VARYING induction variable '{v.dataReference().GetText()}'");
        levels.Add(head);
        foreach (var a in v.performVaryingAfter())
        {
            if (BindVaryingLevel(a.dataReference(), a.arithmeticExpression(), a.condition()) is not { } level)
                return Unsupported($"PERFORM VARYING AFTER induction variable '{a.dataReference().GetText()}'");
            levels.Add(level);
        }
        return new PerformVarying(levels, v.TEST() is not null && v.AFTER() is not null);
    }

    /// <summary>One induction level: the variable is a SET-style target (index-name or data item); the expression
    /// array is [FROM] or [FROM, BY] (BY omitted ⇒ augment 1, GR12).</summary>
    private VaryingLevel? BindVaryingLevel(
        Core.DataReferenceContext dref, Core.ArithmeticExpressionContext[] exprs, Core.ConditionContext cond)
    {
        if (SetTargetOf(dref) is not { } var) return null;
        BoundExpr from = BindExpr(exprs[0]);
        BoundExpr by = exprs.Length > 1 ? BindExpr(exprs[1]) : new BoundNumLiteral("1");
        return new VaryingLevel(var, from, by, BindCondition(cond));
    }

    private static BoundPerformControl Unsupported(string feature) => new PerformTimes(new BoundOperandError(feature));

    private BoundOperand CountOperand(Core.PerformTimesContext t) =>
        t.integerLiteral() is { } lit ? new BoundNumericLiteral(lit.GetText())
        : t.dataReference() is { } d ? FieldOperand(d)
        : new BoundNumericLiteral("1");

    /// <summary>Bind a SET statement, dispatching by format (ISO §14.9.39; COBOLNET_DESIGN §12.3). The COBOL-85
    /// surface — Format 1 index/value assignment, Format 2 UP/DOWN BY, Format 4 condition-name TO TRUE — binds here;
    /// the later-edition formats (switches need SPECIAL-NAMES, pointers/objects their 2002 subsystems, TO FALSE the
    /// 2002 FALSE phrase) fail loud by NAME until their subsystem lands.</summary>
    private BoundStatement BindSet(Core.SetStatementContext set)
    {
        if (set.setLastExceptionStatement() is not null) return BindSetLastException();   // F13 (ISO §14.9.39; 2002+)
        if (set.setToValueStatement() is { } tv) return BindSetTo(tv);
        if (set.setIndexStatement() is { } ud) return BindSetUpDown(ud);
        if (set.setBooleanStatement() is { } b) return BindSetCondition(b);
        if (set.setSwitchStatement() is { } sw) return SwitchBindSet(sw);   // Format 3 — external switches (ISO §14.9.39)
        if (set.setAddressStatement() is { } sa)
            return PtrBindSetAddress(sa);   // F7 both directions + ADDRESS OF senders (Phase-4b inc 2)
        if (set.setObjectReferenceStatement() is { } sor)
        {
            // A POINTER target (§14.9.39 Format 4 — SET pointer TO NULL/pointer) is bound BEFORE the
            // object-reference Format 5: both share the `SET dataRef+ TO objectReference` shape.
            if (sor.dataReference().Length > 0 && refs.Resolve(sor.dataReference(0))?.Item.Pic?.Category
                    is PicCategory.Pointer)
                return BindSetPointer(sor.dataReference(),
                    sor.objectReference().dataReference(), sor.objectReference().NULL_() is not null,
                    sor.objectReference().SELF() is not null || sor.objectReference().SUPER() is not null);
            return OoBindSetObjectRef(sor.dataReference(),
                senderRef: sor.objectReference().dataReference(),
                senderNull: sor.objectReference().NULL_() is not null,
                senderSelf: sor.objectReference().SELF() is not null,
                senderSuper: sor.objectReference().SUPER() is not null);
        }
        return new BoundUnsupported($"SET form '{set.GetText()}'");
    }

    /// <summary><c>SET receivers… TO value</c> (ISO §14.9.39 Format 1). Receivers may mix index-names and data
    /// items; the sender is any integer-valued operand (an index-name sender reads its occurrence number, §3.5).</summary>
    /// <summary>SET data-pointer assignment (§14.9.39 Format 4; Phase-4b increment 1): every target shall
    /// be USAGE POINTER (COBOLNET0869 otherwise); the sender is the NULL figurative or another data pointer
    /// (SELF/SUPER are object-only — 0869). ADDRESS OF senders/receivers are increment 2 (staged loud).</summary>
    private BoundStatement BindSetPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, Core.DataReferenceContext? senderRef,
        bool toNull, bool senderIsSelfSuper)
    {
        if (senderIsSelfSuper)
        {
            data.Edition.Error("COBOLNET0869",
                "SET … TO SELF/SUPER: SELF and SUPER are object references, not data pointers "
                + "(ISO §14.9.39 Format 4/5 — the sender of a pointer SET is NULL or another pointer)");
            return new BoundNop();
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (refs.Resolve(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                data.Edition.Error("COBOLNET0869",
                    $"SET '{t.GetText()}': the receiving operand of a data-pointer SET shall be USAGE POINTER "
                    + "(ISO §14.9.39 Format 4)");
                return new BoundNop();
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (senderRef is null) return new BoundUnsupported("SET pointer — sender shape");
            if (refs.Resolve(senderRef) is not { } sp || sp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                data.Edition.Error("COBOLNET0869",
                    $"SET … TO '{senderRef?.GetText()}': a data-pointer sender shall be NULL or another "
                    + "USAGE POINTER item (ISO §14.9.39 Format 4; ADDRESS OF senders are a later increment)");
                return new BoundNop();
            }
            source = sp;
        }
        return new BoundSetPointer(targets, source, toNull);
    }

    private BoundStatement BindSetTo(Core.SetToValueStatementContext tv)
    {
        // SET Format 14 (ISO §14.9.39; the OCCURS DYNAMIC feature, data-model D9): a CAPACITY-register target
        // reroutes to a capacity change. It runs BEFORE the F4/F5 pointer/object reroutes — a register is numeric,
        // so it would otherwise fall through to the Format-1 store and throw at CapacityRegisterPlace.Write.
        if (DynTryBindSetCapacity(tv.dataReference(), tv.arithmeticExpression(), SetCapacityKind.To) is { } dcap)
            return dcap;
        // The Format-5 SEMANTIC re-route (D-U7): `SET U TO A` parses HERE (alternative order — a
        // dataReference sender is an arithmeticExpression prefix), but an object-reference TARGET selects
        // §14.9.39 Format 5. Detect on the FIRST target; mixed target categories then fail SR8 inside.
        if (tv.dataReference() is { Length: > 0 } tds
            && OoExtractBareReference(tv.arithmeticExpression()) is { } senderDref)
        {
            var t0 = refs.Resolve(tds[0])?.Item.Pic?.Category;
            var s0 = refs.Resolve(senderDref)?.Item.Pic?.Category;
            // A POINTER on either side selects Format 4 (SET pointer TO pointer) — the Format-1 numeric
            // path cannot carry a ManagedPointer.
            if (t0 is PicCategory.Pointer || s0 is PicCategory.Pointer)
                return BindSetPointer(tds, senderDref, toNull: false, senderIsSelfSuper: false);
            // Either side being an object reference selects Format 5 (§14.9.39 F5; D-U7).
            if (t0 is PicCategory.ObjectReference || s0 is PicCategory.ObjectReference)
                return OoBindSetObjectRef(tds, senderDref, senderNull: false, senderSelf: false, senderSuper: false);
        }
        var targets = new List<BoundSetTarget>();
        foreach (var dref in tv.dataReference())
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{dref.GetText()}'");
            targets.Add(t);
        }
        return new BoundSetTo(targets, BindExpr(tv.arithmeticExpression()));
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2) — with the Format-10
    /// data-pointer re-route on the FIRST target's category (the D-U7 semantic-re-route pattern; the two
    /// formats share one grammar shape).</summary>
    private BoundStatement BindSetUpDown(Core.SetIndexStatementContext ud)
    {
        if (PtrTryBindSetUpDown(ud) is { } ptr) return ptr;   // F10 — pointer arithmetic (Phase-4b inc 2)
        if (DynTryBindSetCapacity(ud.dataReference(), ud.arithmeticExpression(),
                ud.DOWN() is not null ? SetCapacityKind.DownBy : SetCapacityKind.UpBy) is { } dcap)
            return dcap;   // F14 — dynamic-capacity change (OCCURS DYNAMIC, D9)
        var targets = new List<BoundSetTarget>();
        foreach (var dref in ud.dataReference())
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{dref.GetText()}'");
            targets.Add(t);
        }
        return new BoundSetUpDown(targets, BindExpr(ud.arithmeticExpression()), ud.DOWN() is not null);
    }

    /// <summary>SET Format 14 (ISO §14.9.39; OCCURS DYNAMIC, data-model D9): reroute when the FIRST target resolves
    /// to a dynamic-table CAPACITY register — <c>SET reg {TO | UP BY | DOWN BY} n</c> changes the table's current
    /// capacity. A non-register first target returns <see langword="null"/> so the normal Format-1/2 path continues
    /// (the non-consuming peek idiom, mirroring <c>PtrTryBindSetUpDown</c>). The register is the SOLE receiver of a
    /// capacity SET (one capacity per statement); a second/mixed target is COBOLNET1524.</summary>
    private BoundStatement? DynTryBindSetCapacity(
        IReadOnlyList<Core.DataReferenceContext> targets, Core.ArithmeticExpressionContext amount, SetCapacityKind kind)
    {
        // A PURE capacity-register peek (NOT refs.Resolve, which would route an OO `prop OF obj` first target through
        // the property hook and enqueue a spurious pending op — OCCURS DYNAMIC review #7).
        if (targets.Count == 0 || refs.CapacityRegisterFor(targets[0]) is not { } cap) return null;
        if (targets.Count > 1)
        {
            data.Edition.Error("COBOLNET1524",
                $"SET '{cap.RegisterItem.CobolName}' {SetCapacityKindText(kind)}: a dynamic-table CAPACITY register "
                + "is the sole receiver of a SET Format 14 statement (ISO §14.9.39; §13.18.38 Format 4)");
            return new BoundNop();
        }
        return new BoundSetCapacity(cap.TablePath, BindExpr(amount), kind);
    }

    private static string SetCapacityKindText(SetCapacityKind kind) =>
        kind switch { SetCapacityKind.To => "TO", SetCapacityKind.UpBy => "UP BY", _ => "DOWN BY" };

    /// <summary>A SET receiving operand: an INDEXED BY index-name (its <c>long</c> field) or a resolvable data item
    /// (an index data item or an integer item — the emitter dispatches on its usage).</summary>
    private BoundSetTarget? SetTargetOf(Core.DataReferenceContext dref) =>
        IndexFieldOf(dref) is { } ix ? new SetIndexTarget(ix)
        : ResolveReceiving(dref) is { } p ? new SetPlaceTarget(p)   // a SET receiver IS a receiving operand
        : null;

    /// <summary><c>SET condition-name+ TO TRUE</c> (ISO §14.9.39 Format 4). TO FALSE needs the 2002 <c>WHEN SET TO
    /// FALSE</c> VALUE phrase (SR7) — loud until the 88 model captures it.</summary>
    private BoundStatement BindSetCondition(Core.SetBooleanStatementContext b)
    {
        if (b.TRUE_() is null)
            return new BoundUnsupported("SET condition-name TO FALSE (the VALUE … WHEN SET TO FALSE phrase, COBOL-2002+, ISO §14.9.39 SR7)");
        var sets = new List<(Place, Condition88)>();
        foreach (var dref in b.dataReference())
        {
            if (ConditionOf(dref) is not { } cond) return new BoundUnsupported($"SET '{dref.GetText()}' TO TRUE (not a condition-name)");
            // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
            if (refs.ResolveForItem(dref, cond.Parent) is not { } parent)
                return new BoundUnsupported($"SET condition '{cond.Name}' (unresolvable conditional variable)");
            sets.Add((parent, cond));
        }
        return new BoundSetConditions(sets);
    }

    /// <summary>Bind a serial SEARCH (ISO §14.9.37 Format 1). The searched operand names a table with INDEXED BY
    /// (SR1); the scan uses the table's FIRST index — unless VARYING names another index OF THE SAME TABLE, which
    /// then IS the search index (GR8a); VARYING a different table's index or a data item increments that item in
    /// step with the search index (GR8b/c). SEARCH ALL (Format 2) is the binary-search wave (needs OCCURS KEY
    /// capture); NOT AT END is a non-ISO extension — both fail loud by name.</summary>
    private BoundStatement BindSearch(Core.SearchStatementContext s)
    {
        var drefs = s.dataReference();
        string tableName = drefs[0].cobolWord()?.GetText() ?? drefs[0].GetText();
        if (data.LookupData(tableName) is not { } candidates
            || candidates.FirstOrDefault(i => i.IsTable) is not { } table)   // fixed OR dynamic (D9)
            return new BoundUnsupported($"SEARCH of non-table '{tableName}'");
        if (table.IndexNames.Count == 0)
            return new BoundUnsupported($"SEARCH table '{tableName}' without INDEXED BY (ISO §14.9.37 SR1)");
        // A dynamic table NESTED under another table has no whole-table path (TablePath null), so the AT-END bound
        // (§8.5.1.9.1 current capacity) and the EnterSearch/ExitSearch bracket cannot be addressed by name — a
        // subscripted capacity path over the enclosing indices is a later increment. Reject rather than let
        // SearchBound fall back to Count=0 and silently scan ZERO occurrences (OCCURS DYNAMIC review #5; D9).
        if (table.IsDynamicTable && refs.TablePath(table) is null)
            return new BoundUnsupported($"SEARCH of the dynamic-capacity table '{tableName}' nested under another "
                + "table (the scan bound over its current capacity needs a subscripted access path — a later increment)");

        string searchIx = data.IndexFieldFor(table.IndexNames[0]);   // scope-aware (method cell first, M2-OO-1h step 4)
        BoundSetTarget? also = null;
        if (drefs.Length > 1)   // the VARYING phrase
        {
            var v = drefs[1];
            if (IndexFieldOf(v) is { } vix)
            {
                if (table.IndexNames.Any(n => data.IndexFieldFor(n) == vix)) searchIx = vix;   // same table (GR8a)
                else also = new SetIndexTarget(vix);                                          // other table (GR8b)
            }
            else if (refs.Resolve(v) is { } p) also = new SetPlaceTarget(p);                  // data item (GR8c)
            else return new BoundUnsupported($"SEARCH VARYING '{v.GetText()}'");
        }

        List<BoundStatement>? atEnd = null;
        if (s.searchAtEndClause() is { } ae)
        {
            if (ae.NOT() is not null) return new BoundUnsupported("SEARCH NOT AT END (non-ISO extension)");
            atEnd = BindBlocks(ae.statementBlock());
        }
        var whens = s.searchWhenClause()
            .Select(wc => new BoundSearchWhen(BindCondition(wc.condition()), BindBlocks(wc.statementBlock())))
            .ToList();
        return new BoundSearch(searchIx, table.Occurs ?? 0, also, atEnd, whens,
            DependCount: OdoModel.SearchBound(table, refs),
            DynTable: table.IsDynamicTable ? refs.TablePath(table) : null);   // EC-FLOW-SEARCH bracket (GR31, D9)
    }

    /// <summary>Bind <c>SEARCH ALL</c> (ISO §14.9.37 Format 2 — the binary-search form). The initial index setting
    /// is ignored (GR9) and the technique is implementor-specified: this implementation scans from occurrence 1,
    /// conformant since Format 2 requires the table ordered by its OCCURS KEYs (SR7) and the WHEN tests key
    /// equality. Bound onto the same <see cref="BoundSearch"/> machinery with <c>FromStart</c>.</summary>
    private BoundStatement BindSearchAll(Core.SearchAllStatementContext s)
    {
        string tableName = s.dataReference().cobolWord()?.GetText() ?? s.dataReference().GetText();
        if (data.LookupData(tableName) is not { } candidates
            || candidates.FirstOrDefault(i => i.IsTable) is not { } table)   // fixed OR dynamic (D9)
            return new BoundUnsupported($"SEARCH ALL of non-table '{tableName}'");
        if (table.IndexNames.Count == 0)
            return new BoundUnsupported($"SEARCH ALL table '{tableName}' without INDEXED BY (ISO §14.9.37 SR1)");
        if (table.IsDynamicTable && refs.TablePath(table) is null)   // nested dynamic — see BindSearch (review #5, D9)
            return new BoundUnsupported($"SEARCH ALL of the dynamic-capacity table '{tableName}' nested under another "
                + "table (the scan bound over its current capacity needs a subscripted access path — a later increment)");

        List<BoundStatement>? atEnd = null;
        if (s.searchAtEndClause() is { } ae)
        {
            if (ae.NOT() is not null) return new BoundUnsupported("SEARCH NOT AT END (non-ISO extension)");
            atEnd = BindBlocks(ae.statementBlock());
        }
        var whens = s.searchAllWhenClause()
            .Select(wc => new BoundSearchWhen(BindCondition(wc.condition()), BindBlocks(wc.statementBlock())))
            .ToList();
        return new BoundSearch(data.IndexFieldFor(table.IndexNames[0]), table.Occurs ?? 0,
            AlsoVaried: null, atEnd, whens, FromStart: true, DependCount: OdoModel.SearchBound(table, refs),
            DynTable: table.IsDynamicTable ? refs.TablePath(table) : null);   // EC-FLOW-SEARCH bracket (GR31, D9)
    }

    /// <summary>The C# <c>long</c> index field when <paramref name="dref"/> is a bare INDEXED BY index-name
    /// (ISO §13.18.38 — index-names are a separate name class living in <see cref="DataBinder.IndexFields"/>,
    /// not the data-item tree), else <see langword="null"/>.</summary>
    private string? IndexFieldOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord()?.GetText() is { } w
        && data.TryGetVisibleIndexField(w, out var f) ? f : null;

    // ── Operands & expressions ─────────────────────────────────────────────────────────────────────────────

    private BoundOperand LiteralOperand(Core.LiteralContext lit)
    {
        var nn = lit.nonNumericLiteral();
        if (nn?.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (nn?.STRINGLIT() is { } s) return new BoundStringLiteral(DecodeCobolString(s.GetText()));
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
    private BoundStringLiteral NationalLiteralOperand(string raw)
    {
        ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.NationalData2002, "national literal N\"…\"");
        string value = DecodeCobolString(raw);
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
    private BoundStringLiteral BooleanLiteralOperand(string raw)
    {
        ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.BooleanData2002, "boolean literal B\"…\"");
        string value = DecodeCobolString(raw);
        if (value.Length > 8191)
            data.Edition.Error("COBOLNET0814", $"boolean literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.4 SR1)");
        return new BoundStringLiteral(value) { Category = PicCategory.Boolean };
    }

    /// <summary>Bind a figurative constant to a bound operand. <c>ALL "literal"</c> (a multi-character figurative,
    /// ISO §8.3.3.6.4 Format 6) → <see cref="BoundAllLiteral"/>; <c>ALL ZEROS</c> etc. are the single-character
    /// figurative repeated to width, identical to the bare word. (ALL HEXLIT / NULL stay a later slice.)</summary>
    private static BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig)
    {
        if (fig.STRINGLIT() is { } allLit) return new BoundAllLiteral(DecodeCobolString(allLit.GetText()));
        if (fig.ZERO() is not null) return new BoundFigurative('Z');
        if (fig.SPACE() is not null) return new BoundFigurative('S');
        if (fig.HIGH_VALUE() is not null) return new BoundFigurative('H');
        if (fig.LOW_VALUE() is not null) return new BoundFigurative('L');
        if (fig.QUOTE_() is not null) return new BoundFigurative('Q');
        if (fig.NULL_() is not null) return new BoundFigurative('N');
        return new BoundOperandError($"figurative constant '{fig.GetText()}'");
    }

    private BoundOperand FieldOperand(Core.DataReferenceContext dref) =>
        KeywordOmittedFunction(dref) is { } kof ? OperandOf(kof)   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundComputedOperand(new BoundLinageCounterRef(lcf))
                : new BoundOperandError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15) — RWCS registers, intercepted ahead of name resolution
        // (the LINAGE-COUNTER idiom); a BoundExprError inside the computed wrapper stays loud (§1.4).
        : RwCounterExpr(dref) is { } rcx ? new BoundComputedOperand(rcx)
        : IndexFieldOf(dref) is { } ix ? new BoundComputedOperand(new BoundIndexRef(ix))
        : refs.Resolve(dref) is { } p ? new BoundFieldOperand(p) : new BoundOperandError(RefFailure(dref));

    /// <summary>The loud-failure text for an unresolvable data reference — when the name belongs to a REJECTED
    /// shared-storage class (a Tier-C / national REDEFINES, an unsupported cell shape), the class's
    /// <c>RejectReason</c> rides along so the runtime loud names WHY, not just the reference (the
    /// design's "references then fail loud" contract, made self-explanatory).</summary>
    private string RefFailure(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        string? reason = data.LookupData(name)
            ?.Select(i => i.Class)
            .FirstOrDefault(c => c is { Tier: RedefinesTier.Rejected, RejectReason: not null })
            ?.RejectReason;
        return reason is null ? $"reference '{dref.GetText()}'" : $"reference '{dref.GetText()}' — {reason}";
    }

    /// <summary>Bind a data reference in a numeric-expression position: an INDEXED BY index-name reads its
    /// occurrence number (valid in SET/SEARCH/relations, ISO §13.18.38); the LINAGE-COUNTER register reads its
    /// file's runtime counter (ISO §8.4.3.14 GR1 — an unsigned integer); otherwise the resolved item's value.
    /// The ONE dataReference→<see cref="BoundExpr"/> mapping, used by every expression path.</summary>
    private BoundExpr RefExpr(Core.DataReferenceContext dref) =>
        KeywordOmittedFunction(dref) is { } kof ? kof   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundLinageCounterRef(lcf)
                : new BoundExprError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15): in the PROCEDURE DIVISION the registers may appear
        // wherever an integer item may (SR1) — read from the report's engine instance, never storage.
        : RwCounterExpr(dref) is { } rcx ? rcx
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
    private List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) =>
        targets.Select(ResolveReceiving).OfType<Place>().ToList();

    // ── ROUNDED phrase → rounding mode + receiver resolution (ISO §14.7.4) ───────────────────────────────────

    /// <summary>The rounding mode a (possibly absent) ROUNDED phrase selects (ISO §14.7.4.3). No phrase → TRUNCATION
    /// (rule 2); a bare <c>ROUNDED</c> → the program's DEFAULT ROUNDED mode (rule 1 / §11.9.6 — the OPTIONS
    /// <c>DEFAULT ROUNDED MODE IS x</c> clause, defaulting to NEAREST-AWAY-FROM-ZERO when absent); an explicit
    /// <c>MODE IS x</c> → the named mode (via the shared <see cref="RoundingModes"/> mapping).</summary>
    private CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase)
    {
        if (phrase is null) return CobolRounding.Truncation;
        if (phrase.roundingModeName() is { } mode)
        {
            // The explicit MODE IS phrase (and the 8-mode set) is ISO 2014+ (§14.7.4); at 85/2002 a bare ROUNDED
            // means the single nearest-away-from-zero rounding and MODE IS is rejected.
            ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.RoundedModeIs2014, "the ROUNDED MODE IS phrase");
            return RoundingModes.Map(mode);
        }
        return data.Options.DefaultRounding;
    }

    /// <summary>The per-edition COMPOSITE-OF-OPERANDS check (ISO §14.7 rule 2, NATIVE arithmetic, the four
    /// arithmetic statements ONLY — COMPUTE expressions are explicitly exempt, §8.8.1.2 r7): the hypothetical item
    /// superimposing the statement's fixed-point operands aligned on their decimal points shall not exceed the
    /// edition's digit cap (18 at COBOL-85; the 2023 text says 31). Float/binary-native operands are excluded
    /// (rule 2b — the composite is then over the remaining operands).</summary>
    private void CheckComposite(string verb, IEnumerable<BoundExpr> operands, IEnumerable<Receiver> receivers)
    {
        if (data.Options.Arithmetic != ArithmeticMode.Native) return;   // §14.7 r2 applies to native only
        int maxInt = 0, maxFrac = 0;
        void Shape(int digits, int scale)
        {
            maxInt = Math.Max(maxInt, digits - scale);   // a negative (P-scaled) scale ADDS integer positions
            maxFrac = Math.Max(maxFrac, Math.Max(0, scale));
        }
        void OfExpr(BoundExpr e)
        {
            switch (e)
            {
                case BoundNumRef { Place.Item.Pic: { Category: PicCategory.Numeric, IsFloat: false } p }:
                    Shape(p.Digits, p.Scale);
                    break;
                case BoundNumLiteral lit:
                    string t = lit.Text.TrimStart('+', '-');
                    int dot = t.IndexOf('.');
                    Shape(t.Count(char.IsAsciiDigit), dot < 0 ? 0 : t.Length - dot - 1);
                    break;
            }
        }
        foreach (var e in operands) OfExpr(e);
        foreach (var r in receivers)
            if (r.Place.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } rp)
                Shape(rp.Digits, rp.Scale);

        // The cap is 31 at EVERY edition (ISO §14.7 rule 2a — the 2023 text). A COBOL-85-specific tightening to
        // 18 was considered and REFUTED by the conformance corpus itself: CCVS-85 NC101A multiplies 9(3)V9(3) by
        // 9(18) (composite 21) as a deliberate SIZE ERROR test, and every conforming '85 implementation accepts
        // it — so the 18-digit figure does not govern the composite (it caps '85 PICTURE/literal capacity only).
        int composite = maxInt + maxFrac;
        if (composite <= 31) return;
        data.Edition.Error("COBOLNET0805",
            $"{verb}: the composite of operands spans {composite} digits ({maxInt} integer + {maxFrac} fraction); "
            + "ISO/IEC 1989 caps the composite of operands at 31 digits (§14.7 rule 2)");
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
    private BoundExpr BindExpr(IParseTree node) => node switch
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
    private string CheckLiteral(string text)
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
        if (pe.functionCall() is { } fc) return BindIntrinsic(fc);
        return new BoundExprError("primary-expression operand");
    }

    /// <summary>Descend an operand-wrapper node to its inner arithmetic expression, or its leaf literal / data
    /// ref. The wrapper chain can nest the expression MORE than one level deep (<c>comparisonOperand →
    /// valueOperand → arithmeticExpression</c>, CobolExpressions.g4), so the walk is BREADTH-FIRST to the
    /// shallowest match — a depth-first leaf grab would collapse a multi-term operand to its first data
    /// reference (a sign condition's operand is the WHOLE expression, ISO §8.8.4.3 — NC250A IF--TEST-55/56).</summary>
    private BoundExpr BindOperandExpr(IParseTree node)
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

    /// <summary>The carried subject + relational operator for ABBREVIATED COMBINED RELATION CONDITIONS (ISO §8.8.4.12).
    /// In a paren-free sequence of relations joined by AND/OR/XOR, a succeeding relation may omit the subject (operator
    /// stated, e.g. the <c>&lt; C</c> in <c>A &gt; B OR &lt; C</c>) or the subject AND operator (a bare operand, e.g.
    /// <c>A = B AND C</c> ≡ <c>A = C</c>). GR1 inserts the last STATED subject and the last STATED operator.
    /// <see cref="Subject"/> is set only by a fully-stated relation; <see cref="Op"/> by a full OR an abbreviated
    /// relation. A complete non-relational simple condition (class / sign / condition-name / parenthesized) terminates
    /// the insertion. Threaded left-to-right (source order) as a mutable holder.</summary>
    private sealed class AbbrevCarry
    {
        public BoundOperand? Subject;
        public string? Op;
        public void Reset() { Subject = null; Op = null; }
    }

    private BoundCondition BindCondition(IParseTree node) => BindCondition(node, new AbbrevCarry());

    private BoundCondition BindCondition(IParseTree node, AbbrevCarry carry) => node switch
    {
        Core.ConditionContext c => BindCondition(c.GetChild(0), carry),
        Core.LogicalOrExpressionContext orExpr => BindFlatSequence(orExpr, "||", carry),
        Core.LogicalXorExpressionContext xorExpr => BindXorSequence(xorExpr, carry),
        Core.LogicalAndExpressionContext andExpr => BindFlatSequence(andExpr, "&&", carry),
        Core.AbbreviatedAndChainContext chain => BindFlatSequence(chain, "&&", carry),
        Core.UnaryLogicalExpressionContext u => u.NOT() is not null
            ? new BoundNot(BindCondition(u.primaryCondition(), carry)) : BindCondition(u.primaryCondition(), carry),
        Core.AbbreviatedRelationContext ar => BindAbbreviatedRelation(ar, carry),
        Core.PrimaryConditionContext p => BindPrimary(p, carry),
        _ => new BoundConditionError("unsupported condition form"),
    };

    /// <summary>Bind a left-to-right logical sequence (an OR / XOR / AND chain, or an abbreviated-AND chain), threading
    /// the abbreviation <paramref name="carry"/> through every operand in SOURCE ORDER so a later abbreviated relation
    /// sees the subject / operator an earlier one established. A lone operand returns its own condition (no wrapper).
    /// A user-function reference in a NON-FIRST operand of an AND/OR chain is conditionally evaluated
    /// (§8.8.4.13 r1 short-circuit / r2 function timing) — guarded loud, the hoist cannot honor it.</summary>
    private BoundCondition BindFlatSequence(IParseTree ctx, string op, AbbrevCarry carry)
    {
        var parts = new List<BoundCondition>();
        for (int i = 0; i < ctx.ChildCount; i++)
        {
            var ch = ctx.GetChild(i);
            if (ch is ITerminalNode) continue;   // the AND / OR / XOR / EXCLUSIVE-OR connective tokens
            int udfMark = _udfPendingCalls.Count;
            parts.Add(BindCondition(ch, carry));
            if (parts.Count > 1) UdfGuardConditionalOperand(udfMark, op);
        }
        return parts.Count == 1 ? parts[0] : new BoundLogical(op, parts);
    }

    /// <summary>The logical XOR / EXCLUSIVE-OR operator (ISO §8.8.4.9) is a COBOL-2023 introduction. It parses at all
    /// editions (superset — the <c>{is2023()}?</c> predicate is gone); the introduction gate fires HERE, only when the
    /// operator is genuinely present (<c>ChildCount &gt; 1</c> ⇒ an <c>XOR</c>/<c>EXCLUSIVE_OR</c> terminal was matched
    /// between two operands), so a bare below-2023 <c>logicalAndExpression</c> is untouched. Residue migration #1
    /// (DESIGN-version-conformance-pipeline.md) — the reverse-signature arm is deleted.</summary>
    private BoundCondition BindXorSequence(Core.LogicalXorExpressionContext xorExpr, AbbrevCarry carry)
    {
        if (xorExpr.ChildCount > 1)
            ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.LogicalXorOperator2023, "the logical XOR operator");
        return BindFlatSequence(xorExpr, "^", carry);
    }

    private BoundCondition BindPrimary(Core.PrimaryConditionContext p, AbbrevCarry carry)
    {
        // COBOL-2002 boolean forms (the boolExprAhead()-gated primaryCondition alt) — a boolean relation
        // (§8.8.4.2.2) or a simple boolean condition (§8.8.4.3).
        if (p.booleanExpression() is { Length: > 0 } be) return BindPrimaryBoolean(be, p.comparisonOperator(), carry);
        if (p.comparisonExpression() is { } cmp) return BindComparison(cmp, carry);
        if (p.condition() is { } inner)
        {
            // A parenthesized condition is a complete simple condition: a FRESH abbreviation scope inside, and the
            // insertion terminates for the enclosing sequence (ISO §8.8.4.12.4 GR1).
            var bound = BindCondition(inner, new AbbrevCarry());
            carry.Reset();
            return bound;
        }
        return new BoundConditionError("boolean-literal condition");
    }

    /// <summary>An abbreviated relation with the subject omitted (<c>comparisonOperator comparisonOperand</c>): the
    /// carried subject is inserted and the newly-stated operator becomes the carried operator (ISO §8.8.4.12.4 GR1).</summary>
    private BoundCondition BindAbbreviatedRelation(Core.AbbreviatedRelationContext ar, AbbrevCarry carry)
    {
        if (carry.Subject is not { } subject)
            return new BoundConditionError("abbreviated relation with no preceding relation subject");
        string op = MapOperator(ar.comparisonOperator().GetText());
        carry.Op = op;
        return CheckedRelational(subject, op, ComparisonOperand(ar.comparisonOperand()));
    }

    /// <summary>The §8.8.4.4.3 class-condition operand rules for the boolean category (the one class the
    /// data increment newly introduces): SR8 — NUMERIC requires an operand whose usage is display or national
    /// or whose category is numeric, so a USAGE BIT boolean is rejected (a DISPLAY-form boolean is admitted);
    /// SR4 — ALPHABETIC / ALPHABETIC-LOWER / ALPHABETIC-UPPER / class-name (<paramref name="kind"/> 'A'/'U'/
    /// 'L'/'C') shall not be specified for a boolean operand at all. Both → COBOLNET0844.</summary>
    private void CheckClassConditionOperand(BoundOperand op, char kind)
    {
        // §8.8.4.4.3 SR1 (data-model D17): a strongly-typed group item may not appear in a class condition — it has
        // its own unique class and category (the type-name), not one of the general classes a class condition tests.
        if (op is BoundFieldOperand fg && fg.Place.Item.IsStrongGroup)
        {
            data.Edition.Error(DiagnosticCatalog.StrongClassCondition, "a strongly-typed group item may not appear in a class condition — "
                + "it has its own unique class and category (ISO §8.8.4.4.3 SR1)");
            return;
        }
        PicInfo? pic = op switch
        {
            BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic,
            BoundFieldOperand f => f.Place.Item.Pic,
            _ => null,
        };
        if (pic is not { Category: PicCategory.Boolean }) return;
        if (kind is 'N' && pic.Usage is Usage.Bit)
            data.Edition.Error("COBOLNET0844", "the NUMERIC class condition requires an operand whose usage is "
                + "display or national or whose category is numeric — a USAGE BIT boolean item is none of these "
                + "(ISO §8.8.4.4.3 SR8)");
        else if (kind is 'A' or 'U' or 'L' or 'C')
            data.Edition.Error("COBOLNET0844", "ALPHABETIC / ALPHABETIC-LOWER / ALPHABETIC-UPPER / a class-name "
                + "shall not be specified for a boolean operand (ISO §8.8.4.4.3 SR4)");
    }

    private BoundCondition BindComparison(Core.ComparisonExpressionContext cmp, AbbrevCarry carry)
    {
        var operands = cmp.comparisonOperand();
        bool not = cmp.NOT() is not null;

        if (cmp.className() is { } cls)
        {
            carry.Reset();   // a class condition is a complete simple condition — terminates the abbreviation
            char? kind = cls.NUMERIC() is not null ? 'N'
                : cls.ALPHABETIC() is not null ? 'A'
                : cls.ALPHABETIC_UPPER() is not null ? 'U'
                : cls.ALPHABETIC_LOWER() is not null ? 'L'
                : null;
            if (kind is { } k && operands.Length >= 1)
            {
                var opnd = ComparisonOperand(operands[0]);
                CheckClassConditionOperand(opnd, k);
                return new BoundClassCondition(opnd, k, not);
            }
            // A SPECIAL-NAMES user-defined class (§12.3.7): membership over the expanded character set.
            if (cls.cobolWord() is { } ucls && operands.Length >= 1
                && data.UserClasses.TryGetValue(ucls.GetText(), out string? members))
            {
                var opnd = ComparisonOperand(operands[0]);
                CheckClassConditionOperand(opnd, 'C');   // SR4 also forbids a class-name for a boolean operand
                return new BoundUserClassCondition(opnd, members, not);
            }
            return new BoundConditionError($"class condition '{cls.GetText()}'");
        }

        if (cmp.POSITIVE() is not null || cmp.NEGATIVE() is not null || cmp.ZERO() is not null)
        {
            carry.Reset();
            char kind = cmp.POSITIVE() is not null ? 'P' : cmp.NEGATIVE() is not null ? 'N' : 'Z';
            return new BoundSignCondition(BindOperandExpr(operands[0]), kind, not);
        }


        if (cmp.comparisonOperator() is { } opCtx && operands.Length >= 2)
        {
            // A fully-stated relation establishes the subject + operator for any following abbreviated relation in the
            // sequence (ISO §8.8.4.12.4 GR1 — "the last preceding stated subject … and the last stated operator").
            BoundOperand subject = ComparisonOperand(operands[0]);
            string op = MapOperator(opCtx.GetText());
            carry.Subject = subject;
            carry.Op = op;
            BoundOperand right = ComparisonOperand(operands[1]);
            // Object relations (ISO §8.8.4.2.1 Format 3 :9591 — D-U8): class-object operands admit ONLY
            // [NOT] EQUAL, and SR5 (:9614) requires BOTH operands of class object (figurative NULL rides —
            // it is a class-object sender). Reference IDENTITY (§8.8.4.2.15 :9769) renders in the
            // ConditionRenderer's object branch. Typed-vs-typed of UNRELATED classes is LEGAL (identity is
            // simply false); ordering operators and object-vs-non-object mixes are COBOLNET0868.
            static bool IsObjOperand(BoundOperand o) =>
                o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.ObjectReference;
            if (IsObjOperand(subject) || IsObjOperand(right))
            {
                if (op is not ("==" or "!="))
                    data.Edition.Error("COBOLNET0868",
                        "an object-reference relation admits only [NOT] EQUAL / '=' / '<>' "
                        + "(ISO §8.8.4.2.1 Format 3 — ordering is undefined for references)");
                else if (!(IsObjOperand(subject) || subject is BoundFigurative { Kind: 'N' })
                         || !(IsObjOperand(right) || right is BoundFigurative { Kind: 'N' }))
                    data.Edition.Error("COBOLNET0868",
                        "both operands of an object-reference relation shall be of class object — an "
                        + "object reference or the NULL figurative (ISO §8.8.4.2.1 SR5)");
            }
            // Data-pointer relations (ISO §8.8.4.1.3 / §8.8.4.2 — pointers admit ONLY [NOT] EQUAL, against
            // another pointer or the NULL figurative; the renderer's pointer branch does SameTarget identity).
            static bool IsPtrOperand(BoundOperand o) =>
                o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.Pointer;
            if (IsPtrOperand(subject) || IsPtrOperand(right))
            {
                if (op is not ("==" or "!="))
                    data.Edition.Error("COBOLNET0869",
                        "a data-pointer relation admits only [NOT] EQUAL (ISO §8.8.4.1.3 — pointers are "
                        + "not ordered)");
                else if (!(IsPtrOperand(subject) || subject is BoundFigurative { Kind: 'N' })
                         || !(IsPtrOperand(right) || right is BoundFigurative { Kind: 'N' }))
                    data.Edition.Error("COBOLNET0869",
                        "both operands of a data-pointer relation shall be a data pointer or NULL "
                        + "(ISO §8.8.4.1.3)");
            }
            // (A boolean-EXPRESSION relation — `IF (a B-AND b) = c` — is staged residue this increment; the
            // item↔item boolean compares of the data increment ride CheckedRelational's 0844 guard below.)
            return CheckedRelational(subject, op, right);
        }

        // A bare single operand — resolve as a sole-operand condition (88 / switch / simple-boolean / abbreviated).
        if (operands.Length == 1)
            return BindSoleOperandCondition(operands[0].valueOperand(), () => ComparisonOperand(operands[0]), carry);

        return new BoundConditionError($"condition '{cmp.GetText()}'");
    }

    /// <summary>A bare single operand is either a level-88 condition-name (a complete simple condition —
    /// terminates the abbreviation), a switch-status condition-name (§8.8.4.6), a SIMPLE BOOLEAN CONDITION over
    /// a length-1 boolean item/literal (§8.8.4.3), or — within an abbreviated sequence — a relation with BOTH
    /// subject and operator omitted (§8.8.4.12; the trailing C in `A = B AND C` ≡ `A = C`). A condition-name
    /// takes precedence. Shared by the generic path and the boolean-alt unwrap (a B-op-free bare operand).</summary>
    private BoundCondition BindSoleOperandCondition(Core.ValueOperandContext? vo, System.Func<BoundOperand> bindOperand, AbbrevCarry carry)
    {
        if (vo?.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref && ConditionOf(dref) is { } cond)
        {
            carry.Reset();
            // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
            return refs.ResolveForItem(dref, cond.Parent) is { } parent
                ? new BoundCondition88(parent, cond)
                : new BoundConditionError($"condition-name '{cond.Name}' (unresolvable conditional variable)");
        }
        // A switch-status condition-name — resolved AFTER level-88 (NC211A: a name defined as both → the 88
        // wins), BEFORE the abbreviated-carry fallback.
        if (vo?.arithmeticExpression() is { } swx && SoleDataRef(swx) is { } swr && SwitchCondOf(swr) is { } swCond)
        {
            carry.Reset();
            return swCond;
        }
        // A SIMPLE BOOLEAN CONDITION over a bare length-1 boolean item/literal (§8.8.4.3).
        if (vo is not null && IsBooleanValueOperand(vo))
        {
            carry.Reset();
            return BindSimpleBooleanCondition(BindBoolOperandValue(vo));
        }
        if (carry is { Subject: { } subject, Op: { } op })
            return CheckedRelational(subject, op, bindOperand());
        return new BoundConditionError($"condition '{vo?.GetText() ?? "operand"}'");
    }

    /// <summary>The ONE <see cref="BoundRelational"/> construction checkpoint — the §8.8.4.2.2 boolean
    /// relation rules ride every site (IF / EVALUATE pairing + ranges / PERFORM UNTIL / SEARCH): a boolean
    /// operand compares only with another boolean operand (§8.8.4.2.1 F1 SR2/SR3 exclude class boolean from
    /// the general relation — a class mix is 0844) and only for [in]equality (Format 2 — an ordering operator
    /// on boolean operands is 0844; an EVALUATE THRU range over a boolean subject trips the same check,
    /// §14.9.13.3 SR4). Figurative ZERO is boolean zeros by context (§8.3.3.6.4 GR4); every other figurative
    /// against a boolean operand is non-boolean.</summary>
    internal BoundRelational CheckedRelational(BoundOperand left, string op, BoundOperand right)
    {
        static bool IsBoolOperand(BoundOperand o) => o switch
        {
            BoundBoolOperand => true,   // a boolean EXPRESSION (B-op tier, increment 2)
            BoundStringLiteral { Category: PicCategory.Boolean } => true,
            BoundAllLiteral { Category: PicCategory.Boolean } => true,
            BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic?.Category is PicCategory.Boolean,
            BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Boolean,
            _ => false,
        };
        bool lb = IsBoolOperand(left), rb = IsBoolOperand(right);
        if (lb || rb)
        {
            static bool BoolCompatible(BoundOperand o) =>
                o is BoundFigurative { Kind: 'Z' } || o switch
                {
                    BoundBoolOperand => true,
                    BoundStringLiteral { Category: PicCategory.Boolean } => true,
                    BoundAllLiteral { Category: PicCategory.Boolean } => true,
                    BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic?.Category is PicCategory.Boolean,
                    BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Boolean,
                    _ => false,
                };
            if (!(BoolCompatible(left) && BoolCompatible(right)))
                data.Edition.Error("COBOLNET0844", "a boolean operand may be compared only with another "
                    + "boolean operand or the figurative constant ZERO (ISO §8.8.4.2.2; §8.8.4.2.1 F1 "
                    + "SR2/SR3 exclude class boolean from the general relation)");
            else if (op is not ("==" or "!="))
                data.Edition.Error("COBOLNET0844", "boolean operands compare for equality only — an ordering "
                    + "relation is not defined for class boolean (ISO §8.8.4.2.2 Format 2)");
        }
        // §8.8.4.2.3 SR1 (data-model D17): if either operand is a strongly-typed group, both shall be of the same
        // type (§8.5.3.3). This is the ONE relation checkpoint, so it also covers EVALUATE pairings/ranges,
        // PERFORM UNTIL, and SEARCH WHEN. (SR4 — a strong group with boolean/object/pointer elements admits only
        // equality — is staged residue, inc 4.)
        DataItem? sl = left is BoundFieldOperand fl ? fl.Place.Item : null;
        DataItem? sr = right is BoundFieldOperand fr ? fr.Place.Item : null;
        if (sl?.IsStrongGroup == true || sr?.IsStrongGroup == true)
        {
            if (sl is null || sr is null || !DataItem.SameStrongType(sl, sr))
                data.Edition.Error(DiagnosticCatalog.StrongCompareMismatch, "a strongly-typed group may be compared only with a group of the "
                    + "same type (ISO §8.8.4.2.3 SR1 / §8.5.3.3)");
            // §8.8.4.2.3 SR4 (D17 inc 4, staged loud): a strong group whose elements include class boolean,
            // object-reference, or pointer may be compared only for equality — an ordering relation on such a group
            // is not defined/implemented.
            else if (op is not ("==" or "!=") && (ContainsNonOrderableLeaf(sl) || ContainsNonOrderableLeaf(sr)))
                data.Edition.Error("COBOLNET1535", "a strongly-typed group containing a boolean, object-reference, "
                    + "or pointer element may be compared only for equality (ISO §8.8.4.2.3 SR4) — an ordering "
                    + "relation is not implemented (data-model D17 residue)");
        }
        return new BoundRelational(left, op, right);
    }

    /// <summary>True when a group (or elementary) item has any leaf of class boolean / object-reference / pointer —
    /// the categories that make a strongly-typed group comparable only for equality (ISO §8.8.4.2.3 SR4).</summary>
    private static bool ContainsNonOrderableLeaf(DataItem item)
    {
        if (item.IsElementary)
            return item.Pic?.Category is PicCategory.Boolean or PicCategory.ObjectReference or PicCategory.Pointer;
        foreach (var c in item.Children)
            if (ContainsNonOrderableLeaf(c)) return true;
        return false;
    }

    /// <summary>Bind a comparison operand: a non-numeric literal, a sole data reference, or a numeric expression.</summary>
    private BoundOperand ComparisonOperand(Core.ComparisonOperandContext operand) =>
        ComparisonOperandOf(operand.valueOperand());

    /// <summary>Bind a <c>valueOperand</c> as a comparison operand (the shared body of <see cref="ComparisonOperand"/>
    /// and the boolean-alt unwrap path — feedback_singular_pattern).</summary>
    private BoundOperand ComparisonOperandOf(Core.ValueOperandContext? vo)
    {
        if (vo?.nonNumericLiteral()?.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (vo?.nonNumericLiteral()?.STRINGLIT() is { } s) return new BoundStringLiteral(DecodeCobolString(s.GetText()));
        if (vo?.nonNumericLiteral()?.NATLIT() is { } nat) return NationalLiteralOperand(nat.GetText());
        if (vo?.nonNumericLiteral()?.BOOLLIT() is { } bl) return BooleanLiteralOperand(bl.GetText());
        if (vo?.arithmeticExpression() is { } expr)
            return SoleDataRef(expr) is { } dref ? FieldOperand(dref)
                // A sole numeric LITERAL stays a literal operand — against an alphanumeric/group operand it
                // participates as its WRITTEN character form, leading zeros intact (ISO §8.8.4.2.1), which a
                // computed wrapper would lose.
                : SoleNumLiteral(expr) is { } lit ? new BoundNumericLiteral(CheckLiteral(lit))
                : new BoundComputedOperand(BindExpr(expr));
        return new BoundOperandError("comparison operand");
    }

    /// <summary>Resolve a condition-name reference, honoring OF/IN qualifiers (ISO §8.4.2.2 Format 2: a
    /// condition-name qualifies by its conditional variable and/or the variable's containing groups, innermost
    /// first) — duplicate 88 names across tables select by the qualifier chain.</summary>
    private Condition88? ConditionOf(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        // §11.7 GR5 — a method-local 88 (under LINKAGE / LOCAL-STORAGE / method-WS data) shadows object data;
        // see ReferenceResolver.ResolveUnqualified for the data-name half of the rule.
        List<Condition88>? list;
        if (data.ActiveMethodScope is { } ms && ms.Conditions.TryGetValue(name, out list) && list.Count > 0) { }
        else if (!data.Conditions.TryGetValue(name, out list) || list.Count == 0) return null;
        var qualifiers = dref.dataReferenceSuffix()
            .Select(sfx => sfx.qualification()?.cobolWord().GetText())
            .OfType<string>().ToList();
        if (qualifiers.Count == 0) return list[0];
        return list.FirstOrDefault(c => MatchesQualifiers(c.Parent, qualifiers));
    }

    /// <summary>True when each qualifier (innermost→outermost) names the conditional variable itself or one of
    /// its containing groups, in nesting order.</summary>
    private static bool MatchesQualifiers(DataItem parent, List<string> qualifiers)
    {
        DataItem? n = parent;
        foreach (string q in qualifiers)
        {
            while (n is not null && !string.Equals(n.CobolName, q, StringComparison.OrdinalIgnoreCase)) n = n.Parent;
            if (n is null) return false;
            n = n.Parent;
        }
        return true;
    }

    // ── Operator mapping + helpers (ported from the former emitter) ──────────────────────────────────────────

    private static string MapOperator(string raw)
    {
        string t = raw.ToUpperInvariant().Replace("IS", "").Replace("THAN", "").Replace("TO", "");
        if (t.Contains("<>")) return "!=";
        bool not = t.Contains("NOT");
        bool orEqual = t.Contains(">=") || t.Contains("<=") || t.Contains("OREQUAL");
        string baseOp =
            t.Contains('>') || t.Contains("GREATER") ? (orEqual ? ">=" : ">")
            : t.Contains('<') || t.Contains("LESS") ? (orEqual ? "<=" : "<")
            : "==";
        if (!not) return baseOp;
        return baseOp switch { ">" => "<=", ">=" => "<", "<" => ">=", "<=" => ">", "==" => "!=", _ => "==" };
    }

    private static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return ((Core.PrimaryExpressionContext)n).dataReference();
    }

    /// <summary>The raw text of an arithmetic expression that is a SOLE numeric literal, else null.</summary>
    private static string? SoleNumLiteral(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        var pe = (Core.PrimaryExpressionContext)n;
        return pe.numericLiteral()?.GetText() ?? (pe.ZERO_ARITH() is not null ? "0" : null);
    }

    private static IEnumerable<Core.DataReferenceContext> DataRefs(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.DataReferenceContext dref) yield return dref;
            else foreach (var inner in DataRefs(child)) yield return inner;
        }
    }

    /// <summary>Decode a COBOL <c>STRINGLIT</c> (<c>"…"</c> with doubled <c>""</c>) — or a national/boolean
    /// literal (<c>N"…"</c>/<c>B"…"</c>, ISO §8.3.3.5/§8.3.3.4: the prefix letter is part of the token) — to
    /// its character value. One of THREE deliberate per-layer twins (DataBinder.DecodeString /
    /// EmitText.DecodeCobolString — Binding must not depend on CodeGen.Emit); keep the three in sync.</summary>
    private static string DecodeCobolString(string raw)
    {
        if (raw.Length >= 3 && raw[0] is 'N' or 'n' or 'B' or 'b' && raw[1] is '"' or '\'')
            raw = raw[1..];
        // Unwrap EITHER delimiter (ISO §8.3.1.2 — the apostrophe form is equal-standing; doubled opening
        // quote = one embedded quote). Keep in sync with the EmitText/DataBinder twins.
        return raw.Length >= 2 && raw[0] is '"' or '\'' && raw[^1] == raw[0]
            ? raw[1..^1].Replace(new string(raw[0], 2), raw[0].ToString())
            : raw;
    }

    private static IEnumerable<IParseTree> Children(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++) yield return node.GetChild(i);
    }

    private static string FirstToken(IParseTree node) =>
        node.ChildCount > 0 ? node.GetChild(0).GetText() : node.GetText();
}
