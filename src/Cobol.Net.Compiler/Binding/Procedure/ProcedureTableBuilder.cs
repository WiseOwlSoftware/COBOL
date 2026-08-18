// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The ONE procedure table + its builders (P7 Step 10t — the plan's `ProcedureTableBuilder`, per-unit on
/// <see cref="BinderContext.Table"/>): the pc space (`_paras ∥ _paraSection ∥ _paraMethod` in LOCKSTEP —
/// every AddParagraph appends to all three), the section map (ISO §14.4.3), the method-local scope maps
/// (§11.7 — registered through the ambient <see cref="BinderContext.CurrentMethodScope"/> collection
/// cursor), <see cref="ResolveProcedure"/> (§8.4.2.2 — explicit OF/IN → in-section → global → section-name,
/// method-confined inside a method), and the DECLARATIVES half (ISO §14.2.4 / §14.9.49 USE — each
/// declarative section joins the same pc space; the USE sentence binds into a <see cref="BoundDeclarative"/>
/// scope, never a bound statement; the §14.9.49.4 GR7 handler exit pc is computed here with the CCVS
/// termination-tail accommodation, see <see cref="DeclHandlerEndPc"/>).
/// </summary>
internal sealed class ProcedureTableBuilder(BinderContext ctx)
{
    private readonly List<(string Cobol, string Method, Core.SentenceContext[] Sentences)> _paras = [];
    private readonly Dictionary<string, int> _paraIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SectionInfo> _sections = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SectionInfo?> _paraSection = [];   // per-pc owning section (parallel to _paras;
                                                              // the ambient CURRENT section lives on Ctx — 10s)


    private readonly List<OoMethodScope?> _paraMethod = [];   // per-pc owning method (parallel to _paras; the
                                                              // ambient CURRENT scope lives on Ctx — 10s)

    private readonly List<int> _paraLine = [];   // per-pc source line (parallel to _paras) — the X3.23-1985
                                                 // DEBUG-LINE register value for a debug-subject procedure (VCR 7.17)

    // ── Procedure table (paragraphs + sections, ISO §14.4.3 / §8.4.2.2) ─────────────────────────────────────

    /// <summary>Register one paragraph (name + uniquified method key + its sentences) at the next pc. Inside a
    /// METHOD body (<see cref="_currentMethodScope"/> set — the class-body collection) the name declares
    /// METHOD-LOCALLY (ISO §11.7 — sibling methods may reuse names; cross-method resolution must FAIL), so it
    /// registers in the method's own map, never the program-global fallback.</summary>
    public void AddParagraph(string name, Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        ctx.Data.ScreenRepositoryIntrinsicName(name, "paragraph-name");   // §8.3.2.1 rule 5 (kb/Work PB65)
        string baseName = "P_" + name.Replace('-', '_').Replace('.', '_');
        string method = baseName;
        for (int n = 2; !used.Add(method); n++) method = $"{baseName}_{n}";
        if (ctx.CurrentMethodScope is { } ms)
            ms.Paras.TryAdd(name, _paras.Count);   // method-local declaration (§11.7)
        else
            _paraIndex.TryAdd(name, _paras.Count); // first definition wins for the global fallback
        section?.Paras.TryAdd(name, _paras.Count); // in-section map for qualified / same-section resolution
        _paraSection.Add(section);
        _paraMethod.Add(ctx.CurrentMethodScope);
        _paraLine.Add(sentences.Length > 0 ? ctx.SourceLine(sentences[0]) : 0);   // DEBUG-LINE source line (VCR 7.17)
        _paras.Add((name, method, sentences));
    }

    /// <summary>Add the ISO §14.4.3 paragraph-name-OMITTED paragraph — "one or more successive sentences
    /// following the procedure division header or a section header". It takes a pc like any other paragraph so
    /// the dispatcher executes it, but it is DELIBERATELY registered in NO name map: having no paragraph-name it
    /// can never be the target of PERFORM / GO TO (§8.4.2.2 resolves procedure-NAMES only), and inventing a
    /// synthetic name would make it referenceable and collide with a user word. The display name carries spaces
    /// so it is not a well-formed COBOL word; the C# method name is generated independently.</summary>
    public void AddAnonymousParagraph(Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        if (sentences.Length == 0) return;
        string method = "P__Anon";
        for (int n = 2; !used.Add(method); n++) method = $"P__Anon_{n}";
        _paraSection.Add(section);
        _paraMethod.Add(ctx.CurrentMethodScope);
        _paraLine.Add(ctx.SourceLine(sentences[0]));
        // The paragraph-name-OMITTED paragraph (§14.4.3) has NO name — the empty string, never a display
        // placeholder (kb/Work PB63 / RV-15.30.3-2: EXCEPTION-LOCATION's r2b2 field printed the placeholder where
        // the standard defines an empty procedure field or the bare section-name).
        _paras.Add(("", method, sentences));
    }

    // ── Exception-checking (Format-3) PERFORM handler pc-ranges (ISO §14.9.28.4 GR17) ───────────────────────
    // The WHEN / WHEN OTHER / WHEN COMMON handler bodies (imp-2/3/4) are bound IN LEXICAL CONTEXT (in
    // EcBindExceptionPerform — correct §8.4.2.2 scope + the GR14 overlay already popped) and registered here as
    // synthetic, UNREFERENCEABLE pc-range paragraphs. They are APPENDED ABOVE the whole main pc space by
    // StatementBinder after the main bind loop (so `_paras.Count` is the frozen main count throughout binding),
    // then walled off the top-level fall-through by the dispatcher (design §9.5.3). imp-1 and imp-5 (FINALLY) stay
    // inline in the host paragraph; only imp-2/3/4 become pc-ranges (run via the reused __RunUse).
    private readonly List<BoundParagraph> _f3Handlers = [];
    private readonly List<int> _f3Owners = [];   // owning PerformId per handler (parallel to _f3Handlers)
    private readonly List<OoMethodScope?> _f3HandlerMethod = [];   // owning method scope per handler (null for a
                                                                    // program unit; parallel to _f3Handlers — the
                                                                    // per-method slice source, design SSOT §9.10)

    /// <summary>The first appended Format-3 handler pc = the frozen main paragraph count (declaratives + all
    /// nondeclarative paragraphs). A handler registered as the k-th lands at this pc + k, matching its eventual
    /// index once StatementBinder appends the side-list.</summary>
    public int HandlerBasePc => _paras.Count;
    public IReadOnlyList<BoundParagraph> F3Handlers => _f3Handlers;
    public IReadOnlyList<int> F3HandlerOwners => _f3Owners;

    /// <summary>The owning method scope of each appended Format-3 handler (parallel to <see cref="F3Handlers"/>;
    /// null in a program unit) — the source of each method's contiguous handler sub-range (design SSOT §9.10).</summary>
    public IReadOnlyList<OoMethodScope?> F3HandlerMethods => _f3HandlerMethod;

    /// <summary>Register one already-bound Format-3 handler body (imp-2/3/4) as a synthetic pc-range paragraph and
    /// return its pc (<see cref="HandlerBasePc"/> + the registration ordinal — dense, collision-free). The body is a
    /// single sentence-group (a handler has no paragraph structure); an empty body still gets one no-op pc (the
    /// bounded <c>__RunUse</c>/<c>__Dispatch(pc,pc)</c> needs a range). Registered in NO name map — unreferenceable
    /// (the <see cref="AddAnonymousParagraph"/> precedent). Nesting-safe: a handler that itself binds an inner F3
    /// PERFORM registers the inner handlers first (lower ordinals); each pc still equals its final appended index.</summary>
    public int AddF3Handler(IReadOnlyList<BoundStatement> body, int performId, int line)
    {
        int pc = _paras.Count + _f3Handlers.Count;
        _f3Handlers.Add(new BoundParagraph("(exception-checking PERFORM handler)", new[] { body }, line));
        _f3Owners.Add(performId);
        _f3HandlerMethod.Add(ctx.CurrentMethodScope);   // the owning method (null in a program) — the per-method slice
        return pc;
    }

    public void CollectParagraphs(Core.ProcedureDivisionContext pd)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);

        // DECLARATIVES first (ISO §14.2.3 GR1 — execution begins with the first NONdeclarative procedure; the
        // declarative sections share the ONE pc space, entered only via the USE dispatch or an explicit
        // PERFORM/GO TO — SR4). The walk records the BoundDeclarative scopes (StatementBinder.Declaratives.cs).
        foreach (var dp in pd.declarativePart())
            foreach (var sec in dp.declarativeSection())
            {
                using var _ = ctx.Edition.At(sec);   // the declarative section's position (kb/Work PB82)
                DeclCollectSection(sec, used);
            }
        _entryPc = _paras.Count;

        // §14.4.3 — sentences written directly after the PROCEDURE DIVISION header, with no paragraph-name.
        // They form the first nondeclarative paragraph, so they must take the ENTRY pc (execution begins with
        // the first nondeclarative procedure, §14.2.3 GR1) — hence before the procedureUnit walk.
        AddAnonymousParagraph(pd.sentence(), null, used);

        foreach (var unit in pd.procedureUnit())
        {
            using var _ = ctx.Edition.At(unit);   // the paragraph / section header's position (kb/Work PB82)
            if (unit.paragraphDefinition() is { } para)
                AddParagraph(para.paragraphName().GetText(), para.sentence(), null, used);
            else if (unit.sectionDefinition() is { } section)
            {
                // A section's paragraphs are contiguous in the pc sequence, so the section IS a pc range:
                // GO TO section transfers to its first paragraph (ISO §14.9.17), PERFORM section runs first
                // statement of its first paragraph through last statement of its last (ISO §14.9.28).
                ctx.Data.ScreenRepositoryIntrinsicName(section.sectionName().GetText(), "section-name");   // §8.3.2.1 rule 5 (kb/Work PB65)
                var info = new SectionInfo(section.sectionName().GetText(), _paras.Count);
                // §14.4.3 — a section header may likewise be followed directly by unnamed sentences; they are
                // the section's first paragraph, so GO TO / PERFORM <section> enters them (§14.9.17/§14.9.28).
                AddAnonymousParagraph(section.sentence(), info, used);
                foreach (var p in section.paragraphDefinition())
                    AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
                info.EndPc = _paras.Count - 1;
                _sections.TryAdd(info.Name, info);
            }
        }

        // X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17): resolve the debug SUBJECTS now that every
        // nondeclarative procedure is in the pc space — the ALL PROCEDURES / procedure-name legs bind to real
        // trigger points; the data-name / file-name / cd-name subject kinds and the SORT/MERGE cause taxonomy
        // are staged loud (COBOLNET1571). No-op unless WITH DEBUGGING MODE collected a debug declarative.
        FinalizeDebug(pd);
    }

    /// <summary>Resolve a procedure-name reference to its inclusive pc range (ISO §8.4.2.2): a section name is its
    /// paragraph range; a paragraph is (pc, pc). The head/qualifier are taken from the context's CHILDREN — never
    /// <c>GetText()</c> of the whole context, which concatenates <c>PAR-1A OF SEC-1</c> into an unmatchable key.
    /// Resolution order: explicit <c>OF/IN section</c> qualifier → the named section's own map; unqualified → a
    /// paragraph of the CURRENT section (implicit qualification of duplicated names), then the global first-defined
    /// paragraph, then a section name. Null when unknown (the caller fails loud).</summary>
    public (int Start, int End)? ResolveProcedure(Core.ProcedureNameContext pn)
    {
        string head = pn.GetChild(0).GetText();
        string? qualifier = pn.ChildCount >= 3 ? pn.GetChild(2).GetText() : null;
        // Inside a METHOD body resolution is CONFINED to the method's own maps (ISO §11.7 — method-local
        // procedure names; a cross-method PERFORM/GO TO resolves to nothing and the caller fails loud, the
        // legacy trap-#10 rule made structural).
        if (ctx.CurrentMethodScope is { } m)
        {
            if (qualifier is not null)
                return m.Sections.TryGetValue(qualifier, out var mq) && mq.Paras.TryGetValue(head, out int mqpc)
                    ? (mqpc, mqpc) : null;
            if (ctx.CurrentSection is { } mcur && mcur.Paras.TryGetValue(head, out int mlocal)) return (mlocal, mlocal);
            if (m.Paras.TryGetValue(head, out int mpc)) return (mpc, mpc);
            if (m.Sections.TryGetValue(head, out var msec)) return (msec.StartPc, msec.EndPc);
            return null;
        }
        if (qualifier is not null)
            return _sections.TryGetValue(qualifier, out var q) && q.Paras.TryGetValue(head, out int qpc)
                ? (qpc, qpc) : null;
        if (ctx.CurrentSection is { } cur && cur.Paras.TryGetValue(head, out int local)) return (local, local);
        if (_paraIndex.TryGetValue(head, out int pc)) return (pc, pc);
        if (_sections.TryGetValue(head, out var sec)) return (sec.StartPc, sec.EndPc);
        return null;
    }

    /// <summary>The pc space (name + uniquified method key + sentences) — the bind loops walk it; parallel
    /// in LOCKSTEP to <see cref="ParaSections"/> (owning section) and <see cref="ParaMethods"/> (owning
    /// method scope). Every <see cref="AddParagraph"/> appends to all three.</summary>
    public IReadOnlyList<(string Cobol, string Method, Core.SentenceContext[] Sentences)> Paragraphs => _paras;
    public IReadOnlyList<SectionInfo?> ParaSections => _paraSection;
    public IReadOnlyList<OoMethodScope?> ParaMethods => _paraMethod;

    private int _entryPc;
    private readonly List<BoundDeclarative> _declaratives = [];

    /// <summary>The narrow declarative surface EcBinder's RESUME SR1–SR3 checks read (10r; hoists to
    /// ProcedureTableBuilder at 10t and these host edges delete).</summary>
    public int EntryPc => _entryPc;
    public IReadOnlyList<BoundDeclarative> Declaratives => _declaratives;
    private readonly HashSet<string> _declScopedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _declScopedModes = [];
    private readonly HashSet<ReportGroupModel> _declReportGroups = [];   // §14.9.49 SR9 — one Format-2 USE per group

    /// <summary>Collect one declarative section into the pc space: the USE sentence (SR1 — the section's first
    /// sentence), an anonymous paragraph for any further leading sentences (the CCVS handler-before-the-first-
    /// paragraph shape, e.g. SQ103A), then the named paragraphs.</summary>
    private void DeclCollectSection(Core.DeclarativeSectionContext sec, HashSet<string> used)
    {
        string name = sec.sectionName().GetText();
        ctx.Data.ScreenRepositoryIntrinsicName(name, "section-name");   // §8.3.2.1 rule 5 (kb/Work PB65)
        var info = new SectionInfo(name, _paras.Count);

        // SR1: the first sentence consists of exactly one USE statement.
        var leading = sec.sentence();
        DeclScope? scope = null;
        if (leading.Length == 0
            || leading[0].statement() is not { Length: 1 } first
            || first[0].useStatement() is not { } use)
            ctx.Edition.Error("COBOLNET0897", $"declarative section '{name}': the first sentence shall consist "
                + "of a single USE statement (ISO §14.2.4 / §14.9.49 SR1)");
        else if (use.DEBUGGING() is not null)
        {
            // X3.23-1985 USE FOR DEBUGGING (the '85 debug facility, deleted by ISO 2002 — 0902-gated ≥2002 by
            // the version-conformance pass, VCR Table 7 row 7.17). Accepted-inert at 85 per the '85 rules: WITHOUT
            // SOURCE-COMPUTER … WITH DEBUGGING MODE the whole debugging section is compiled as if it were
            // comment lines (skip it — nothing binds, its names leave the pc space). WITH the switch the section
            // IS compiled and, with the object-time switch ON (RunUnit.DebugMode default true — the CCVS posture),
            // its ON procedure-name / ALL PROCEDURES triggers fire (the DEBUG-ITEM register family is modeled).
            // The section body is collected below (an ordinary pc range invoked by the emitted __RunDebug, not a
            // BoundDeclarative — scope stays null); FinalizeDebug records the subjects once every procedure is in.
            if (!ctx.Data.DebuggingModeDeclared) return;
            ctx.Data.ActivateDebugRegisters();   // make DEBUG-* references resolvable while this section binds
            _pendingDebug.Add((info, use));      // info.EndPc is filled after the body is collected below
            // fall through to collect the section body into the pc space (scope stays null — no BoundDeclarative)
        }
        else
            scope = DeclBindUse(use, name);

        // Leading sentences past the USE form an anonymous paragraph at the section start (handler bodies that
        // CCVS writes directly under the section header).
        if (leading.Length > 1)
            AddParagraph(name, leading.Skip(1).ToArray(), info, used);
        foreach (var p in sec.declarativeParagraph())
            AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
        // An empty handler still needs ONE pc so the bounded dispatch has a range (a no-op paragraph).
        if (_paras.Count == info.StartPc)
            AddParagraph(name, [], info, used);

        info.EndPc = _paras.Count - 1;
        _sections.TryAdd(info.Name, info);

        if (scope is { } s)
            _declaratives.Add(new BoundDeclarative(
                name, info.StartPc, info.EndPc, DeclHandlerEndPc(sec, info), s.Files, s.ModeIndex, s.Global, s.Report,
                s.EcEntries, s.EoClassCsName));
    }

    // ── X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17) ────────────────────────────────────────────────
    // The debug declaratives collected under WITH DEBUGGING MODE (section info + the USE statement), pending
    // subject resolution once every nondeclarative procedure is in the pc space (FinalizeDebug).
    private readonly List<(SectionInfo Section, Core.UseStatementContext Use)> _pendingDebug = [];
    private readonly List<BoundDebugSubject> _debugSubjects = [];

    /// <summary>The X3.23-1985 debug-facility trigger subjects (VCR Table 7 row 7.17) — each a nondeclarative
    /// procedure whose entry the emitter instruments to populate DEBUG-ITEM and run the debugging declarative;
    /// empty unless a procedure-subject USE FOR DEBUGGING was collected under WITH DEBUGGING MODE.</summary>
    public IReadOnlyList<BoundDebugSubject> DebugSubjects => _debugSubjects;

    /// <summary>Resolve the collected debug declaratives to trigger SUBJECTS now that the whole pc space exists
    /// (X3.23-1985 debug module, VCR Table 7 row 7.17). <c>ALL PROCEDURES</c> and a bare procedure-name bind to
    /// real trigger points at NONdeclarative procedures (a debugging declarative is never debugged — the '85
    /// ALL PROCEDURES exclusion, DB101A "USE PROCEDURE NOT EXECUTED"). The data-name (incl. ALL REFERENCES OF),
    /// file-name, and cd-name subject kinds are STAGED — rejected loud COBOLNET1571 (their after-statement /
    /// after-I-O trigger insertion and the DEBUG-SUB subscript rendering are not modeled). A SORT/MERGE INPUT/
    /// OUTPUT procedure that is also a debug subject is likewise staged (the SORT INPUT/OUTPUT/MERGE OUTPUT
    /// DEBUG-CONTENTS cause is not modeled — rejecting avoids a silent wrong cause).</summary>
    private void FinalizeDebug(Core.ProcedureDivisionContext pd)
    {
        if (_pendingDebug.Count == 0) return;
        foreach (var (section, use) in _pendingDebug)
        {
            foreach (var t in use.useDebugTarget())
            {
                if (t.PROCEDURES() is not null)          // ALL PROCEDURES → every nondeclarative procedure
                {
                    for (int pc = _entryPc; pc < _paras.Count; pc++)
                        AddDebugSubject(pc, section);
                }
                else if (t.REFERENCES() is not null)     // ALL REFERENCES OF identifier-1 → data-name subject (staged)
                {
                    ctx.Edition.Error("COBOLNET1571", $"declarative section '{section.Name}': USE FOR DEBUGGING "
                        + "ON ALL REFERENCES OF a data item (the after-statement data trigger with DEBUG-CONTENTS/"
                        + "DEBUG-SUB rendering) is recognized but not modeled — only ON procedure-name / ALL "
                        + "PROCEDURES is implemented (X3.23-1985 debug module; VCR Table 7 row 7.17)");
                }
                else if (t.dataReference() is { } dr)    // bare name: procedure-name, else file/data/cd (staged)
                {
                    string nm = dr.cobolWord()?.GetText() ?? dr.GetText();
                    if (dr.dataReferenceSuffix().Length == 0 && _paraIndex.TryGetValue(nm, out int ppc))
                        AddDebugSubject(ppc, section);                       // procedure-name (paragraph)
                    else if (dr.dataReferenceSuffix().Length == 0 && _sections.TryGetValue(nm, out var psec))
                        for (int pc = psec.StartPc; pc <= psec.EndPc; pc++)  // procedure-name (section → its paragraphs)
                            AddDebugSubject(pc, section);
                    else
                        ctx.Edition.Error("COBOLNET1571", $"declarative section '{section.Name}': USE FOR DEBUGGING "
                            + $"ON '{nm}' names a file-name, cd-name, or data item (not a procedure-name of this "
                            + "program) — only the ON procedure-name / ALL PROCEDURES leg is modeled; the file / "
                            + "data / communication debug triggers are not (X3.23-1985 debug module; VCR Table 7 row 7.17)");
                }
            }
        }

        // A SORT/MERGE INPUT/OUTPUT procedure that is also a debug subject would trigger with a stale cause — the
        // SORT INPUT/OUTPUT / MERGE OUTPUT DEBUG-CONTENTS taxonomy is not modeled. Reject loud rather than emit a
        // silent wrong cause (X3.23-1985; the DB2xx SORT/MERGE witnesses are staged).
        if (_debugSubjects.Count > 0)
        {
            var subjectPcs = _debugSubjects.Select(s => s.SubjectPc).ToHashSet();
            foreach (var (name, rng) in SortMergeProcedureRanges(pd))
                if (Enumerable.Range(rng.Start, rng.End - rng.Start + 1).Any(subjectPcs.Contains))
                {
                    ctx.Edition.Error("COBOLNET1571", $"the SORT/MERGE {name} PROCEDURE is also a USE FOR "
                        + "DEBUGGING subject: the SORT INPUT/OUTPUT / MERGE OUTPUT DEBUG-CONTENTS cause is not "
                        + "modeled (only the plain-transfer / fall-through / PERFORM-loop / START-PROGRAM causes "
                        + "are) — X3.23-1985 debug module; VCR Table 7 row 7.17");
                    break;
                }
        }
    }

    /// <summary>Record one debug trigger subject (the subject pc's name + source line + the debugging section's
    /// invokable pc range) — deduplicated so a procedure named twice does not double-fire.</summary>
    private void AddDebugSubject(int pc, SectionInfo section)
    {
        if (pc < 0 || pc >= _paras.Count) return;
        if (_debugSubjects.Any(s => s.SubjectPc == pc)) return;
        _debugSubjects.Add(new BoundDebugSubject(
            pc, _paras[pc].Cobol, _paraLine[pc], section.StartPc, section.EndPc));
    }

    /// <summary>The pc range of each SORT/MERGE INPUT/OUTPUT PROCEDURE in the procedure division (for the debug
    /// SORT/MERGE-overlap staging check). Resolves the phrase's procedure-name(s) via the ordinary procedure
    /// table (THRU forms span first..last).</summary>
    private List<(string Kind, (int Start, int End) Range)> SortMergeProcedureRanges(
        Core.ProcedureDivisionContext pd)
    {
        var results = new List<(string, (int, int))>();
        void Add(string kind, Core.ProcedureNameContext[] pns)
        {
            if (pns.Length == 0) return;
            if (ResolveProcedure(pns[0]) is not { } first) return;
            var last = pns.Length > 1 ? ResolveProcedure(pns[^1]) : first;
            if (last is { } l) results.Add((kind, (first.Start, l.End)));
        }
        void Walk(Antlr4.Runtime.Tree.IParseTree node)
        {
            switch (node)
            {
                case Core.SortInputProcedurePhraseContext ip: Add("INPUT", ip.procedureName()); return;
                case Core.SortOutputProcedurePhraseContext op: Add("OUTPUT", op.procedureName()); return;
                case Core.MergeOutputProcedurePhraseContext mo: Add("OUTPUT", mo.procedureName()); return;
            }
            for (int i = 0; i < node.ChildCount; i++) Walk(node.GetChild(i));
        }
        Walk(pd);
        return results;
    }

    /// <summary>One USE statement's bound trigger scope: Format 1's files/mode (+GLOBAL), Format 2's report
    /// group, or Format 3's (exception-name, file) entries (ISO §14.9.49).</summary>
    private readonly record struct DeclScope(
        IReadOnlyList<FileModel> Files, int? ModeIndex, bool Global, ReportGroupModel? Report,
        IReadOnlyList<(string Ec, FileModel? File)>? EcEntries = null, string? EoClassCsName = null);

    /// <summary>Bind the USE statement's trigger scope (ISO §14.9.49): Format 1's file list or open mode; the
    /// GLOBAL phrase drives the cross-program GR4b dispatch (the emitter's <c>__RunGlobalUse</c> containment
    /// walk). <c>ON file-name</c> resolves against <c>FilesByName</c>, which includes containers' GLOBAL FDs
    /// (§13.18.30 — merged by <c>CallBindUnit</c>; IC234A's contained USE names the outer's GLOBAL file).
    /// Format 2 (BEFORE REPORTING, SR9) names a report group — the section becomes the group's
    /// before-reporting hook, invoked by the report engine just before the group is produced (GR8; wired in
    /// <c>CSharpEmitter.ReportWriter.cs</c>). The same group shall not appear in two such statements (SR9).</summary>
    private DeclScope? DeclBindUse(Core.UseStatementContext use, string sectionName)
    {
        bool global = use.GLOBAL() is not null;
        if (use.useEcEntry() is { Length: > 0 } ecEntries)
            return DeclBindUseF3(ecEntries, sectionName);
        if (use.OBJECT() is not null || use.EO() is not null)
        {
            // Format 4 (§14.9.49.2 — ONE class/interface operand; SR15 EO ≡ EXCEPTION OBJECT). GR3: for an
            // OBJECT raise, F4 selection REPLACES the F1/F3 tiers (the generated __EcObjDispatch, D-EO7).
            // use-after-exception-object-2002: the pass owns the edition gate (Exec Step E).
            ctx.EcState.F3 = true;   // the ONE "EC declaratives present" feature bit — F4 rides the same group gate
            string cname = use.cobolWord().GetText();
            if (ctx.Data.OoClasses?.Find(cname) is not { } cls)
            {
                ctx.Edition.Error("COBOLNET0859",
                    $"declarative section '{sectionName}': USE AFTER EXCEPTION OBJECT '{cname}' does not "
                    + "name a class of the compilation group (ISO §14.9.49.3 SR16; interface entries are "
                    + "the interface-RAISING refinement)");
                return null;
            }
            return new DeclScope([], null, global, null, EoClassCsName: cls.CsName);
        }
        if (use.REPORTING() is not null)
        {
            // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1 — identifier-1 references a report group
            // (SR9), optionally qualified by its report-name (the procedureName's OF/IN tail).
            var pn = use.procedureName();
            string head = pn.GetChild(0).GetText();
            string? qualifier = pn.ChildCount >= 3 ? pn.GetChild(2).GetText() : null;
            foreach (var report in ctx.Data.Reports)
            {
                if (qualifier is not null && !report.Name.Equals(qualifier, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (report.Groups.FirstOrDefault(g =>
                        head.Equals(g.Name, StringComparison.OrdinalIgnoreCase)) is { } group)
                {
                    if (!_declReportGroups.Add(group))
                        ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': report group "
                            + $"'{head}' already has a USE BEFORE REPORTING procedure (ISO §14.9.49 SR9)");
                    return new DeclScope([], null, global, group);
                }
            }
            ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE BEFORE REPORTING "
                + $"'{head}' does not name a report group (ISO §14.9.49 SR9)");
            return null;
        }
        var target = use.useOnTarget();
        if (target is null) return null;

        // Mode scope (GR3b/GR6b–e) — the index IS the runtime FileOpenMode ordinal (the compiler references
        // the runtime enum; both sides stay aligned by construction).
        int? mode = target.INPUT() is not null ? (int)Runtime.IO.FileOpenMode.Input
            : target.OUTPUT() is not null ? (int)Runtime.IO.FileOpenMode.Output
            : target.EXTEND() is not null ? (int)Runtime.IO.FileOpenMode.Extend
            : target.I_O() is not null ? (int)Runtime.IO.FileOpenMode.IO
            : null;
        if (mode is { } m)
        {
            if (!_declScopedModes.Add(m))
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': this open mode already "
                    + "has a USE procedure in this source element (ISO §14.9.49 SR7)");
            return new DeclScope([], m, global, null);
        }

        var files = new List<FileModel>();
        foreach (var fn in target.fileName())
        {
            string fname = fn.GetText();
            if (!ctx.Data.FilesByName.TryGetValue(fname, out var file))
            {
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE names unknown "
                    + $"file '{fname}' (ISO §14.9.49)");
                continue;
            }
            if (file.IsSortMerge)
            {
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE may not name the "
                    + $"sort/merge file '{fname}' (ISO §14.9.49 SR2)");
                continue;
            }
            if (!_declScopedFiles.Add(fname))
                ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': file '{fname}' already "
                    + "has a USE procedure in this source element (ISO §14.9.49 SR8)");
            files.Add(file);
        }
        return new DeclScope(files, null, global, null);
    }

    /// <summary>Bind a Format-3 USE statement's scope (ISO §14.9.49.2 — <c>USE AFTER {EXCEPTION CONDITION | EC}
    /// {exception-name-1 | exception-name-2 {FILE file-name-2}…}…</c>): validate every exception-name against
    /// the §14.6.13.1 catalog (level 1/2/3 all legal — the GR3c–g tiers select by level), SR13 (a file-scoped
    /// name shall begin EC-I-O), SR14 (no duplicate (ec, file) pair across the USE statements of one procedure
    /// division), and the per-name edition window. The whole format is 2002+ (the EC model's introduction).</summary>
    private DeclScope? DeclBindUseF3(Core.UseEcEntryContext[] entries, string sectionName)
    {
        // use-after-exception-condition-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.F3 = true;
        var pairs = new List<(string Ec, FileModel? File)>();
        foreach (var entry in entries)
        {
            string raw = entry.cobolWord().GetText();
            // Resolution + the 0711/0878/1636 diagnostics live in the ONE funnel (kb/Work R05). ⚠ The funnel
            // gates the introduction edition at EVERY level where this site's former copy guarded Level == 3 —
            // a LEVEL-2 name of a 2023-only family (EC-MCS) in a USE entry slipped the old gate at 2002/2014.
            if (!EcNameResolution.TryResolve(ctx.Edition, raw,
                    $"declarative section '{sectionName}'", out var info)) continue;
            var fileNames = entry.fileName();
            if (fileNames.Length > 0 && !Runtime.Exceptions.ExceptionCatalog.IsIoName(info.Name))
            {
                ctx.Edition.Error("COBOLNET0715", $"declarative section '{sectionName}': FILE may be specified "
                    + $"only with an exception-name beginning 'EC-I-O' — '{info.Name}' does not (ISO §14.9.49.3 SR13)");
                continue;
            }
            if (fileNames.Length == 0)
            {
                AddPair(info.Name, null);
                continue;
            }
            foreach (var fn in fileNames)
            {
                string fname = fn.GetText();
                if (!ctx.Data.FilesByName.TryGetValue(fname, out var file))
                {
                    ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE names unknown "
                        + $"file '{fname}' (ISO §14.9.49)");
                    continue;
                }
                if (file.IsSortMerge)
                {
                    ctx.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE may not name "
                        + $"the sort/merge file '{fname}' (ISO §14.9.49.3 SR2)");
                    continue;
                }
                AddPair(info.Name, file);
            }
        }
        return new DeclScope([], null, Global: false, null, pairs);

        void AddPair(string ec, FileModel? file)
        {
            // SR14: the same (exception-name, file-name) pair shall not appear in more than one USE statement
            // within the same procedure division (the set spans sections — _declEcPairs is per division).
            if (!ctx.EcState.DeclEcPairs.Add(ec + "|" + (file?.CobolName ?? "")))
                ctx.Edition.Error("COBOLNET0716", $"declarative section '{sectionName}': the exception-name/"
                    + $"file pair '{ec}{(file is null ? "" : " FILE " + file.CobolName)}' is already specified in "
                    + "another USE statement of this procedure division (ISO §14.9.49.3 SR14)");
            else
                pairs.Add((ec, file));
        }
    }

    /// <summary>The pc the bounded handler dispatch ends at (§14.9.49.4 GR7 — normally the section's last
    /// paragraph). CCVS ACCOMMODATION (documented deviation, the legacy's empirically-validated SQ212A rule):
    /// some CCVS programs place an UNREFERENCED termination tail (CLOSE-FILES → footer → STOP RUN) inside the
    /// declarative section after a trivial exit paragraph; the NIST golden requires the handler to RETURN at
    /// that exit paragraph (the tail stays in pc space — an explicit GO TO still reaches it on the fatal path).
    /// Rule: the LAST paragraph whose statements are all bare EXIT/CONTINUE that is still followed by a
    /// paragraph containing STOP RUN / EXIT PROGRAM / GOBACK ⇒ HandlerEndPc = that exit paragraph's pc. It must
    /// be the LAST such (the boundary adjoining the tail): the handler body's own PERFORM … THRU exit points
    /// (SQ212A's FAIL-ROUTINE-EX1 before EXIT-PARA) are also trivial-exit paragraphs, and bounding at an
    /// earlier one lets a handler GO TO past it fall through into the termination tail.</summary>
    private int DeclHandlerEndPc(Core.DeclarativeSectionContext sec, SectionInfo info)
    {
        var paras = sec.declarativeParagraph();
        int firstNamedPc = info.EndPc - paras.Length + 1;   // leading anonymous paragraph (if any) precedes
        for (int i = paras.Length - 2; i >= 0; i--)
        {
            if (!DeclIsTrivialExit(paras[i])) continue;
            for (int j = i + 1; j < paras.Length; j++)
                if (DeclTerminatesRunUnit(paras[j]))
                    return firstNamedPc + i;
        }
        return info.EndPc;
    }

    private static bool DeclIsTrivialExit(Core.DeclarativeParagraphContext p)
    {
        var sentences = p.sentence();
        if (sentences.Length == 0) return true;   // an empty named paragraph is a pure exit point
        foreach (var s in sentences)
            foreach (var st in s.statement())
            {
                if (st.continueStatement() is not null) continue;
                if (st.exitStatement() is { } e
                    && e.PARAGRAPH() is null && e.PERFORM() is null && e.SECTION() is null
                    && e.PROGRAM() is null && e.METHOD() is null && e.FUNCTION() is null)
                    continue;
                return false;
            }
        return true;
    }

    private static bool DeclTerminatesRunUnit(Core.DeclarativeParagraphContext p)
    {
        foreach (var s in p.sentence())
            foreach (var st in s.statement())
                if (st.stopStatement() is not null || st.gobackStatement() is not null
                    || st.exitStatement() is { } e && e.PROGRAM() is not null)
                    return true;
        return false;
    }
}
