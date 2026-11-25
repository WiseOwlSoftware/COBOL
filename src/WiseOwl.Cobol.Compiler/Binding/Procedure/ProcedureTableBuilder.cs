// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The ONE procedure table + its builders (P7 Step 10t — the plan's `ProcedureTableBuilder`, per-unit on
/// <see cref="BinderContext.Table"/>): the pc space (`_paras ∥ _paraSection ∥ _paraMethod` in LOCKSTEP —
/// every AddParagraph appends to all three), the section map (ISO §2023:14.4.3), the method-local scope maps
/// (a method definition is its own source element — §2023:3.164 with §2023:3.165 and §2023:11.7.1 — so §2023:8.4.6.1 confines
/// its paragraph-names to it; registered through the ambient
/// <see cref="BinderContext.CurrentMethodScope"/> collection cursor), <see cref="ResolveProcedureOperand"/> (§2023:8.4.2.2 — explicit OF/IN → in-section → global → section-name,
/// method-confined inside a method), and the DECLARATIVES half (ISO §2023:14.3 / §2023:14.9.49 USE — each
/// declarative section joins the same pc space; the USE sentence binds into a <see cref="BoundDeclarative"/>
/// scope, never a bound statement; the use procedure's range IS that section's own
/// <see cref="SectionInfo.Range"/>, §2023:14.9.49.3 SR1 + §2023:14.4.2).
/// <para>A USE statement's trigger scope is <see cref="UseStatementBinder"/>'s and the USE FOR DEBUGGING subjects and targets
/// <see cref="DebugDeclarativeResolver"/>'s; the table holds the pc space, the sections, the declarative scopes and the
/// procedure-name resolution over them (kb/Work PB2345; docs/rearchitecture/DESIGN-architecture-review.md §8.3).</para>
/// </summary>
internal sealed class ProcedureTableBuilder(BinderContext ctx)
{
    private readonly List<(string Cobol, string Method, Core.SentenceContext[] Sentences)> _paras = [];
    // The paragraph-names and section-names are the one store of names' (SymbolTable.DeclareParagraph, DeclareSection), which
    // remembers a repeated spelling: §2023:8.4.2.2.1 asks whether a spelling is UNIQUE (kb/Work PB466, PB5932).
    private readonly List<SectionInfo?> _paraSection = [];   // per-pc owning section (parallel to _paras;
                                                              // the ambient CURRENT section lives on Ctx — 10s)


    private readonly List<OoMethodScope?> _paraMethod = [];   // per-pc owning method (parallel to _paras; the
                                                              // ambient CURRENT scope lives on Ctx — 10s)

    private readonly List<int> _paraLine = [];   // per-pc source line (parallel to _paras) — the X3.23-1985
                                                 // DEBUG-LINE register value for a debug-subject procedure (VCR 7.17)
    // ⛔ per-pc line of the paragraph-NAME (parallel to _paras; 0 for a paragraph-name-OMITTED paragraph) — the line a
    // diagnostic that names a declaration points the user to. NOT _paraLine: that is the first sentence's line, kept
    // for DEBUG-LINE, and it named the statement under the header, or no line for a paragraph of no sentences, in
    // every "declared N times" message (kb/Work PB5091).
    private readonly List<int> _paraHeaderLine = [];

    // ── Procedure table (paragraphs + sections, ISO §2023:14.4.3 / §2023:8.4.2.2) ─────────────────────────────────────

    /// <summary>Register one paragraph (name + uniquified method key + its sentences) at the next pc. Inside a
    /// METHOD body (<see cref="_currentMethodScope"/> set — the class-body collection) the name declares
    /// METHOD-LOCALLY (ISO §2023:8.4.6.1 over the method's own source element, §2023:3.164 — sibling methods may reuse
    /// names and cross-method resolution must FAIL), so it registers in the method's own map, never the
    /// program-global fallback.</summary>
    public void AddParagraph(Core.ProcedureNameContext header, Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        string name = HeaderName(header, "paragraph");
        ctx.Data.DeclareUserWord(name, UserWordKind.ParagraphName);   // §2023:8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
        string baseName = "P_" + DataItem.IdentifierCharacters(name);   // extended letters too (PB1402)
        string method = baseName;
        for (int n = 2; !used.Add(method); n++) method = $"{baseName}_{n}";
        // ⛔ DECLARE, never TryAdd (kb/Work PB466): a repeated spelling is KEPT, so §2023:8.4.2.2.1's "No other name
        // has the identical spelling" stays answerable at the reference. The FIRST definition is still what an
        // unambiguous lookup returns; what changed is that the map now knows there was a second.
        // Method-local inside a method body (§2023:8.4.6.1 / §2023:3.164), else program-wide; and in its section's own map, for a
        // qualified or same-section reference.
        ctx.Symbols.DeclareParagraph(name, header, _paras.Count, section, ctx.CurrentMethodScope);
        _paraSection.Add(section);
        _paraMethod.Add(ctx.CurrentMethodScope);
        _paraLine.Add(sentences.Length > 0 ? ctx.SourceLine(sentences[0]) : 0);   // DEBUG-LINE source line (VCR 7.17)
        _paraHeaderLine.Add(ctx.SourceLine(header));
        _paras.Add((name, method, sentences));
    }

    /// <summary>Add the ISO §2023:14.4.3 paragraph-name-OMITTED paragraph — "one or more successive sentences
    /// following the procedure division header or a section header". It takes a pc like any other paragraph so
    /// the dispatcher executes it, but it is DELIBERATELY registered in NO name map: having no paragraph-name it
    /// can never be the target of PERFORM / GO TO (§2023:8.4.2.2 resolves procedure-NAMES only), and inventing a
    /// synthetic name would make it referenceable and collide with a user word. The display name carries spaces
    /// so it is not a well-formed COBOL word; the C# method name is generated independently.</summary>
    public void AddAnonymousParagraph(Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        if (sentences.Length == 0) return;
        AddUnnamedParagraph(sentences, section, used);
    }

    /// <summary>The ONE pc a section with ZERO paragraphs owns where its execution must be a point the dispatcher can
    /// see: a declarative section (§2023:14.9.49.3 SR1: "zero, one, or more procedural paragraphs" — the section is still
    /// SELECTED, §2023:14.9.49.4 GR3, so it needs a pc to dispatch to), and any section of a program that compiles a USE FOR
    /// DEBUGGING section (§1985:XV.3.2.4 GR3 a: the debugging section runs "Immediately before each execution of the named
    /// procedure", and PERFORM of an empty section and the fall-through into it each execute it, kb/Work PB5568). It is a
    /// paragraph-name-OMITTED paragraph with no sentences: like <see cref="AddAnonymousParagraph"/> it is named nowhere
    /// (kb/Work PB990 — it used to be registered under the SECTION's own name, so every such section declared a paragraph
    /// its source never wrote, and <c>PERFORM</c> of the section was ambiguous with it). Its DEBUG-LINE is the section header's
    /// line where §1985:XV.3.2.4 GR16a asks for "the first statement of that procedure" (an empty first section's START
    /// PROGRAM): the section has none, and GR12 leaves the identification to the implementor (the golden
    /// pb5633_debug_line_empty_section_85 pins it, kb/Work PB5633).</summary>
    public void AddEmptyAnonymousParagraph(SectionInfo section, HashSet<string> used)
    {
        AddUnnamedParagraph([], section, used);
        _paraLine[^1] = section.HeaderLine;
    }

    private void AddUnnamedParagraph(Core.SentenceContext[] sentences, SectionInfo? section, HashSet<string> used)
    {
        string method = "P__Anon";
        for (int n = 2; !used.Add(method); n++) method = $"P__Anon_{n}";
        _paraSection.Add(section);
        _paraMethod.Add(ctx.CurrentMethodScope);
        _paraLine.Add(sentences.Length > 0 ? ctx.SourceLine(sentences[0]) : 0);
        _paraHeaderLine.Add(0);   // no paragraph-name, so no declaration to point at
        // The paragraph-name-OMITTED paragraph (§2023:14.4.3) has NO name — the empty string, never a display
        // placeholder (kb/Work PB63 / RV-15.30.3-2: EXCEPTION-LOCATION's r2b2 field printed the placeholder where
        // the standard defines an empty procedure field or the bare section-name).
        _paras.Add(("", method, sentences));
    }

    // ── Exception-checking (Format-3) PERFORM handler pc-ranges (ISO §2023:14.9.28.4 GR17) ───────────────────────
    // The WHEN / WHEN OTHER / WHEN COMMON handler bodies (imp-2/3/4) are bound IN LEXICAL CONTEXT (in
    // EcBindExceptionPerform — correct §2023:8.4.2.2 scope + the GR14 overlay already popped) and registered here as
    // synthetic, UNREFERENCEABLE pc-range paragraphs. They are APPENDED ABOVE the whole main pc space by
    // StatementBinder after the main bind loop (so `_paras.Count` is the frozen main count throughout binding),
    // then walled off the top-level fall-through by the dispatcher (design §9.5.3). imp-1 and imp-5 (FINALLY) stay
    // inline in the host paragraph; only imp-2/3/4 become pc-ranges (run via the reused __RunUse).
    private readonly List<BoundParagraph> _f3Handlers = [];
    private readonly List<int> _f3Owners = [];   // owning PerformId per handler (parallel to _f3Handlers)
    private readonly List<OoMethodScope?> _f3HandlerMethod = [];   // owning method scope per handler (null for a
                                                                    // program unit; parallel to _f3Handlers — the
                                                                    // per-method slice source, design SSOT §9.10)

    public IReadOnlyList<BoundParagraph> F3Handlers => _f3Handlers;
    public IReadOnlyList<int> F3HandlerOwners => _f3Owners;

    /// <summary>The owning method scope of each appended Format-3 handler (parallel to <see cref="F3Handlers"/>;
    /// null in a program unit) — the source of each method's contiguous handler sub-range (design SSOT §9.10).</summary>
    public IReadOnlyList<OoMethodScope?> F3HandlerMethods => _f3HandlerMethod;

    /// <summary>Register one already-bound Format-3 handler body (imp-2/3/4) as a synthetic pc-range paragraph and
    /// return its pc: the frozen main paragraph count (declaratives + all nondeclarative paragraphs) + the
    /// registration ordinal — dense, collision-free, and equal to its index once StatementBinder appends the
    /// side-list (the base it records as <c>BoundProgram.F3HandlerBasePc</c>). The body is a
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

    /// <summary>The NONDECLARATIVE portion's walk, ONE for a program's procedure division (<see cref="CollectParagraphs"/>)
    /// and a method's (<c>StatementBinder.BindClassBody</c>, with <see cref="BinderContext.CurrentMethodScope"/> set, so
    /// every name registers method-locally): the unnamed lead sentences at the next pc, then each paragraph and section
    /// at its header's position, every procedure-name declared through the one funnel. The method arm kept its own copy
    /// of this walk, which declared no section-name and reported a procedure-name's §2023:8.3.2.2 conflict without a
    /// position (kb/Work PB2921).</summary>
    public void CollectNondeclaratives(Core.ProcedureDivisionContext pd, HashSet<string> used)
    {
        // §2023:14.4.3 — sentences written directly after the PROCEDURE DIVISION header, with no paragraph-name.
        // They form the first nondeclarative paragraph, so they must take the ENTRY pc (execution begins with
        // the first nondeclarative procedure, §2023:14.2.3 GR1) — hence before the paragraph and section walks.
        AddAnonymousParagraph(pd.sentence(), null, used);

        // The grammar writes the division's own paragraphs ahead of its sections, because every paragraph after a
        // section header is that section's (§2023:14.4.2; kb/Work PB4230), so the two walks below run in source order.
        foreach (var para in pd.paragraphDefinition())
        {
            using var _ = ctx.Edition.At(para);   // the paragraph header's position (kb/Work PB82)
            AddParagraph(para.paragraphName().procedureName(), para.sentence(), null, used);
        }
        foreach (var section in pd.sectionDefinition())
        {
            using var _ = ctx.Edition.At(section);   // the section header's position (kb/Work PB82)
            // A section's paragraphs are contiguous in the pc sequence, so the section IS a pc range:
            // GO TO section transfers to its first paragraph (ISO §2023:14.9.17), PERFORM section runs first
            // statement of its first paragraph through last statement of its last (ISO §2023:14.9.28).
            string sectionName = HeaderName(section.sectionName().procedureName(), "section");
            ctx.Data.DeclareUserWord(sectionName, UserWordKind.SectionName);   // §2023:8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
            var info = new SectionInfo(sectionName, _paras.Count, ctx.SourceLine(section),
                segment: Segmentation.ValueOf(section.integerLiteral()));
            // §2023:14.4.3 — a section header may likewise be followed directly by unnamed sentences; they are
            // the section's first paragraph, so GO TO / PERFORM <section> enters them (§2023:14.9.17/§2023:14.9.28).
            AddAnonymousParagraph(section.sentence(), info, used);
            foreach (var p in section.paragraphDefinition())
                AddParagraph(p.paragraphName().procedureName(), p.sentence(), info, used);
            // Zero paragraphs ⇒ the range stays EMPTY (§2023:14.4.2) — unless a debugging section is compiled, when the empty
            // section is a procedure a debug trigger can name and so owns its pc (kb/Work PB5568). The declaratives were
            // collected first, so _pendingDebug is complete here.
            if (_paras.Count == info.StartPc && _pendingDebug.Count > 0) AddEmptyAnonymousParagraph(info, used);
            info.CloseAt(_paras.Count - 1);
            ctx.Symbols.DeclareSection(info, section.sectionName().procedureName(), ctx.CurrentMethodScope);
        }
    }

    /// <summary>⛔ THE ONE READER OF A PROCEDURE HEADER'S NAME (kb/Work PB4238): the bare paragraph-name or section-name
    /// every header declaration and every pc-space entry is keyed on. The grammar's <c>paragraphName</c> /
    /// <c>sectionName</c> share <c>procedureName</c> with the references, so the parse admits <c>P1 OF S1.</c> and
    /// <c>S1 OF X SECTION.</c>; ISO §2023:14.2.1's general formats print <c>section-name-1 SECTION.</c> and
    /// <c>paragraph-name-1.</c>, and §2023:14.4.1 gives qualification ("a paragraph-name (that may be qualified)") to the
    /// procedure-name that REFERS to a procedure. A qualified header is refused here (COBOL5716), by name and as
    /// written; the head word is still returned and declared so the one error is not followed by a "not defined" for
    /// every reference to the procedure. Reading the node's text instead glued the words into one name
    /// (<c>P1OFS1</c>) that <c>PERFORM P1OFS1</c> then ran (the shape that PB2040 closed for the file-name operand).</summary>
    /// <param name="kind">"paragraph" or "section", for the message.</param>
    private string HeaderName(Core.ProcedureNameContext pn, string kind)
    {
        if (!ProcedureNameShape.IsQualified(pn)) return ProcedureNameShape.Head(pn);
        using (ctx.Edition.At(pn))   // the HEADER's position — a paragraph in a section is collected under its section's At
            ctx.Edition.Error(DiagnosticCatalog.QualifiedProcedureHeader,
                $"{kind}-name '{ProcedureNameShape.AsWritten(pn)}' is written qualified in its "
                + $"{kind} header — a {kind}-name that is being defined is written alone ("
                + ctx.Edition.Edition.CiteRule("§1985:IV.6.4.1.4.2: \"section-name SECTION.\", \"paragraph-name.\"",
                    "§2023:14.2.1: \"section-name-1 SECTION.\", \"paragraph-name-1.\"")
                + "); qualification ("
                + ctx.Edition.Edition.Cite("§1985:IV.4.3.8.1", "§2023:8.4.2.2.2") + ") belongs to a "
                + "reference"
                + ctx.Edition.Edition.Cite("", ", and §2023:14.4.1 gives it to the procedure-name that refers to a paragraph")
                + ". Write "
                + $"'{ProcedureNameShape.Head(pn)}' in the header and qualify the references.");
        return ProcedureNameShape.Head(pn);
    }

    public void CollectParagraphs(Core.ProcedureDivisionContext pd)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);

        // DECLARATIVES first (ISO §2023:14.2.3 GR1 — execution begins with the first NONdeclarative procedure; the
        // declarative sections share the ONE pc space, entered only via the USE dispatch or an explicit PERFORM —
        // §2023:14.9.49.3 SR4 admits no other reference into a declarative section from outside it, and
        // ResolveProcedureOperand enforces it, kb/Work PB362). The walk records the BoundDeclarative scopes
        // (StatementBinder.Declaratives.cs).
        foreach (var dp in pd.declarativePart())
            foreach (var sec in dp.declarativeSection())
            {
                using var _ = ctx.Edition.At(sec);   // the declarative section's position (kb/Work PB82)
                DeclCollectSection(sec, used);
            }
        _entryPc = _paras.Count;
        CollectNondeclaratives(pd, used);

        // X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17): resolve the debug SUBJECTS now that every
        // nondeclarative procedure is in the pc space — the ALL PROCEDURES / procedure-name legs bind to real
        // trigger points; the data-name, ALL REFERENCES OF, file-name and cd-name targets are collected for the statement
        // binder's funnel to plan against (CONTROL_FLOW_DESIGN D20). No-op unless WITH DEBUGGING MODE collected a debug declarative.
        _debugContentsWidth = new DebugDeclarativeResolver(ctx, this, _debugSubjects, _debugTargets, _debugContentsWidth)
            .FinalizeDebug(_pendingDebug);
    }

    /// <summary>⛔ THE ONE <c>procedure-name</c> OPERAND RESOLUTION for a STATEMENT (kb/Work PB390): resolve, or
    /// REPORT COBOL1639 and return null. Every statement whose general format prints procedure-name — PERFORM,
    /// GO TO (both formats), ALTER, RESUME AT, SORT/MERGE INPUT and OUTPUT PROCEDURE — enters here, so a name that
    /// resolves to nothing is a compile-time DIAGNOSTIC by construction and not a per-site habit.
    /// <para>⛔ THIS EXISTS BECAUSE THE SILENT TWIN WAS USED EIGHT TIMES. Each site turned the null into a
    /// <c>BoundUnsupported</c> — which the emitter renders as <c>NotImplemented.Run(...)</c> — so a misspelled
    /// PERFORM COMPILED, produced an assembly, and aborted the run unit claiming WiseOwl COBOL had not implemented a
    /// feature; on a path the flow skipped it said nothing at all. ISO §2023:4.2.2 ¶2 requires the compile-time
    /// mechanism. The message itself lives in the ONE syntax-rule catalog
    /// (<see cref="Validation.StatementValidation.RejectProcedureName"/>), never here.</para>
    /// <para>⛔ THE DECLARATIVES BOUNDARY IS TESTED HERE, ONCE, FOR EVERY STATEMENT (kb/Work PB362). ISO §2023:14.9.49.3
    /// SR3 and SR4 restrict every procedure-name REFERENCE, and this is the funnel every statement reference passes
    /// through, so a verb that takes a procedure-name inherits both rules by construction. The quiet prescan
    /// (<see cref="ResolveProcedureQuiet"/>) is not a reference of its own and is not tested.</para></summary>
    /// <param name="pn">The procedure-name as written.</param>
    /// <param name="verb">The statement or phrase for the message, e.g. "PERFORM", "SORT INPUT PROCEDURE".</param>
    /// <param name="rule">The caller's OWN syntax rule quoted with its citation (PERFORM §2023:14.9.28.3 SR12/SR13);
    /// "" where the statement states none and §2023:8.4.2.1 alone decides (GO TO, ALTER, RESUME AT).</param>
    /// <param name="kind">Which statement writes the reference, for the declaratives boundary (ISO §2023:14.9.49.3
    /// SR3/SR4). Only PERFORM and RESUME pass anything but the default — see
    /// <see cref="ProcedureReferenceKind"/>.</param>
    public ResolvedProcedure? ResolveProcedureOperand(
        Core.ProcedureNameContext pn, string verb, string rule = "",
        ProcedureReferenceKind kind = ProcedureReferenceKind.Other)
    {
        var resolved = Resolve(pn, report: true);
        if (resolved.Procedure is { } procedure)
            return ctx.Validation.CheckDeclarativesBoundary(procedure, ctx.CurrentSection, ProcedureNameShape.AsWritten(pn), verb, kind)
                ? procedure : null;
        string head = ProcedureNameShape.Head(pn);
        // ⛔ "IDENTIFIES MORE THAN ONE" IS NOT "IDENTIFIES NONE" (kb/Work PB466). Both fail §2023:8.4.2.1's "a
        // reference that uniquely identifies that resource", but they are different rules with different
        // repairs, so the ambiguous case gets its own message and its own code rather than being folded into
        // COBOL1639's "no paragraph or section carries that name", which would be a false statement about
        // a program that carries the name twice.
        if (resolved.Ambiguity is { } ambiguity)
        {
            ctx.Validation.RejectAmbiguousProcedureName(ambiguity, verb);
            return null;
        }
        ctx.Validation.RejectProcedureName(ProcedureNameShape.AsWritten(pn), head, verb, rule, ctx.CurrentMethodScope is not null);
        return null;
    }

    /// <summary>Resolve a procedure-name reference to its inclusive pc range (ISO §2023:8.4.2.2): a section name is its
    /// paragraph range; a paragraph is (pc, pc). The head/qualifier are taken from the context's CHILDREN — never
    /// <c>GetText()</c> of the whole context, which concatenates <c>PAR-1A OF SEC-1</c> into an unmatchable key.
    /// Resolution order: explicit <c>OF/IN section</c> qualifier → the named section's own map; unqualified → a
    /// paragraph of the CURRENT section (implicit qualification of duplicated names), then the global first-defined
    /// paragraph, then a section name. Null when unknown.
    /// <para>⛔ QUIET, AND THE NAME SAYS SO (kb/Work PB390): this arm reports NOTHING, so it belongs only to a
    /// PRESCAN that is not the statement's own bind — the ALTER switch-field prescan and the USE FOR DEBUGGING
    /// SORT/MERGE-overlap scan, each of which runs before (or beside) the bind that will report. A STATEMENT
    /// operand goes through <see cref="ResolveProcedureOperand"/>, which cannot resolve to nothing in
    /// silence.</para>
    /// <para>An AMBIGUOUS name resolves to nothing here, exactly as an unknown one does: a prescan has no use
    /// for "some procedure with this spelling", and the statement's own operand bind reports the ambiguity and
    /// fails the compile either way (kb/Work PB466).</para></summary>
    public ResolvedProcedure? ResolveProcedureQuiet(Core.ProcedureNameContext pn) => Resolve(pn, report: false).Procedure;

    /// <summary>A statement's procedure-name, resolved from where it is written: its method and the CURRENT section.</summary>
    private ProcedureResolution Resolve(Core.ProcedureNameContext pn, bool report) => Identified(pn, ctx.CurrentSection, report);

    /// <summary>⛔ THE ONE §2023:8.4.2.2 procedure-name resolution, <see cref="SymbolTable.Identify"/> (kb/Work PB5932): one body for
    /// every scope (a METHOD body's own procedure-names when one is current, since a method definition is a contained source unit
    /// and so its own source element, §2023:11.7.1, §2023:3.165, §2023:3.164, to which §2023:8.4.6.1 confines its paragraph-names and
    /// section-names; the program's otherwise), the qualifier, rule 6's containing section and rule 1's count over paragraphs and
    /// sections together. What the table identifies becomes the resolution here, where the pc space is: a paragraph's owning
    /// section is its <see cref="ParaSections"/> entry, the one structure that says which section a pc belongs to; and the
    /// candidates of an ambiguous name are described, each at its source line, for the message (kb/Work PB466, PB5091).</summary>
    /// <param name="written">The procedure-name as written: a <c>procedureName</c>, a USE FOR DEBUGGING operand, or one word.</param>
    /// <param name="referencingSection">The section CONTAINING the reference, which §2023:8.4.2.2.1 SR6 is measured against.
    /// Passed in rather than read from the ambient cursor, because the debug subject is resolved after collection, when the cursor
    /// no longer stands where the reference was written.</param>
    /// <param name="report">The reference itself (true), or a prescan or probe that classifies it first (false).</param>
    internal ProcedureResolution Identified(ParserRuleContext written, SectionInfo? referencingSection, bool report) =>
        ctx.Symbols.Identify(written, NameClass.Procedure, report, site: new ProcedureSite(ctx.CurrentMethodScope, referencingSection))?.Symbol switch
        {
            ParagraphDefinition paragraph => Found(PcRange.At(paragraph.Pc), SectionOfPc(paragraph.Pc)),
            SectionInfo section => Found(section.Range, section, isSection: true),
            ProcedureCandidates candidates => Ambiguous(candidates.Rule, candidates.Name, Describe(candidates), candidates.Section?.Name),
            _ => default,
        };

    /// <summary>A resolution that SUCCEEDED, carrying the range together with the section that owns the
    /// denoted procedure — the ONE constructor for the success case, so no path can forget the section
    /// (kb/Work PB433).</summary>
    private static ProcedureResolution Found(PcRange range, SectionInfo? section, bool isSection = false) =>
        new(new ResolvedProcedure(range, section, isSection), null);

    /// <summary>The section that owns <paramref name="pc"/> — <see cref="ParaSections"/>, which
    /// <c>AddParagraph</c> and <c>AddAnonymousParagraph</c> fill in lockstep with the pc space. Out of range (a
    /// synthetic Format-3 handler pc, which is in no name map and so never resolves here) answers null.</summary>
    private SectionInfo? SectionOfPc(int pc) =>
        (uint)pc < (uint)_paraSection.Count ? _paraSection[pc] : null;

    private static ProcedureResolution Ambiguous(
        ProcedureAmbiguityRule rule, string name, IReadOnlyList<string> candidates, string? section = null) =>
        new(null, new ProcedureAmbiguity(rule, name, candidates, section));

    /// <summary>The candidates of an ambiguous procedure-name, described for the diagnostic in declaration order, each at its source
    /// line (the user has to find the OTHER declaration). For §2023:8.4.2.2.3 SR7 they are the section's OWN repeated paragraphs; for
    /// rule 1 every procedure carrying the spelling, paragraphs first (each with its containing section), then sections.</summary>
    private List<string> Describe(ProcedureCandidates candidates)
    {
        var described = new List<string>(candidates.Paragraphs.Count + candidates.Sections.Count);
        foreach (int pc in candidates.Paragraphs)
            described.Add($"paragraph '{candidates.Name}'"
                + (candidates.Rule == ProcedureAmbiguityRule.InSectionDuplicate ? ""
                    : _paraSection[pc] is { } s ? $" in section '{s.Name}'" : " (in no section)")
                + AtLine(pc));
        foreach (var sec in candidates.Sections) described.Add(DescribeSection(sec));
        return described;
    }

    private static string DescribeSection(SectionInfo sec) => $"section '{sec.Name}' at line {sec.HeaderLine}";

    /// <summary>Where paragraph <paramref name="pc"/> is DECLARED — the line of its paragraph-name (kb/Work PB5091), or
    /// nothing for a pc that has none (a paragraph-name-omitted paragraph, an appended handler range).</summary>
    private string AtLine(int pc) => pc < _paraHeaderLine.Count && _paraHeaderLine[pc] > 0 ? $" at line {_paraHeaderLine[pc]}" : "";

    /// <summary>The pc space (name + uniquified method key + sentences) — the bind loops walk it; parallel
    /// in LOCKSTEP to <see cref="ParaSections"/> (owning section) and <see cref="ParaMethods"/> (owning
    /// method scope). Every <see cref="AddParagraph"/> appends to all three.</summary>
    public IReadOnlyList<(string Cobol, string Method, Core.SentenceContext[] Sentences)> Paragraphs => _paras;
    public IReadOnlyList<SectionInfo?> ParaSections => _paraSection;

    /// <summary>The source line of each pc's first sentence (the section header's for an empty section's pc): the
    /// X3.23-1985 DEBUG-LINE of a debug-subject procedure (VCR 7.17), read by <see cref="DebugDeclarativeResolver"/>.</summary>
    internal IReadOnlyList<int> ParaLines => _paraLine;

    /// <summary>The COBOL-85 segment-number of each of <paramref name="count"/> pcs (kb/Work PB5084; a paragraph outside every
    /// section, and an appended handler paragraph, is segment 0), or null when no section is an independent segment: the run
    /// time needs the map only to return an independent segment to its initial state (§1985:XVI.1.4.3), so a program without
    /// one keeps the generated source it had.</summary>
    public IReadOnlyList<int>? SegmentsPerPc(int count)
    {
        if (!_paraSection.Any(s => s is { } section && SegmentNumber.IsIndependent(section.Segment))) return null;
        var segments = new int[count];
        for (int pc = 0; pc < count && pc < _paraSection.Count; pc++) segments[pc] = _paraSection[pc]?.Segment ?? 0;
        return segments;
    }
    public IReadOnlyList<OoMethodScope?> ParaMethods => _paraMethod;

    private int _entryPc;
    private readonly List<BoundDeclarative> _declaratives = [];
    /// <summary>The USE statements of the procedure division being collected (§2023:14.9.49.3 SR7-SR14 are per procedure
    /// division, so a method's declaratives take a fresh one).</summary>
    private UseStatementBinder _use = new(ctx);

    /// <summary>The narrow declarative surface EcBinder's RESUME SR1–SR3 checks read (10r; hoists to
    /// ProcedureTableBuilder at 10t and these host edges delete).</summary>
    public int EntryPc => _entryPc;
    public IReadOnlyList<BoundDeclarative> Declaratives => _declaratives;

    /// <summary>Collect a METHOD's declaratives portion (kb/Work PB1010; ISO §2023:14.2.2 SR10 — "Formats 1 and 2 may be
    /// specified in a source element if and only if that source element is a function definition, a function
    /// prototype definition, a method definition, a program definition, or a program prototype definition"), at
    /// the current end of the class's ONE pc space and under the method scope the caller has entered
    /// (<see cref="BinderContext.CurrentMethodScope"/>), through the SAME <see cref="DeclCollectSection"/> a
    /// program's declaratives take. Returns the method's own <see cref="BoundDeclarative"/>s in source order —
    /// §2023:14.9.49.4 GR3 analyzes "the USE statements in the source element", and a method IS the source element
    /// that contains its statements (GR4 a)), so the emitter selects over exactly this list for them.</summary>
    public IReadOnlyList<BoundDeclarative> CollectMethodDeclaratives(Core.ProcedureDivisionContext pd, HashSet<string> used)
    {
        _use = new UseStatementBinder(ctx);
        int first = _declaratives.Count;
        foreach (var dp in pd.declarativePart())
            foreach (var sec in dp.declarativeSection())
            {
                using var _ = ctx.Edition.At(sec);
                DeclCollectSection(sec, used);
            }
        return _declaratives.Count == first ? [] : [.. _declaratives.Skip(first)];
    }


    /// <summary>Collect one declarative section into the pc space: the USE sentence (SR1 — the section's first
    /// sentence), an anonymous paragraph for any further leading sentences (the CCVS handler-before-the-first-
    /// paragraph shape, e.g. SQ103A), then the named paragraphs.</summary>
    private void DeclCollectSection(Core.DeclarativeSectionContext sec, HashSet<string> used)
    {
        string name = HeaderName(sec.sectionName().procedureName(), "section");
        ctx.Data.DeclareUserWord(name, UserWordKind.SectionName);   // §2023:8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
        // isDeclarative: THE fact §2023:14.9.28.3 SR11 (and GO TO's / ALTER's analogous constraints) asks about,
        // recorded where it is known rather than re-derived from pc arithmetic later.
        var info = new SectionInfo(name, _paras.Count, ctx.SourceLine(sec), isDeclarative: true,
            segment: Segmentation.ValueOf(sec.integerLiteral()));

        // SR1: the first sentence consists of exactly one USE statement.
        var leading = sec.sentence();
        UseStatementBinder.DeclScope? scope = null;
        if (leading.Length == 0
            || leading[0].statement() is not { Length: 1 } first
            || first[0].useStatement() is not { } use)
        {
            ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{name}': the first sentence shall consist "
                + "of a single USE statement (ISO §2023:14.3 / §2023:14.9.49.3 SR1)");
            _nonDebuggingDeclarativeSeen = true;
        }
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
            if (!ctx.Data.State.Configuration.DebuggingModeDeclared) return;
            // §1985:XV.3.2.3 SR1: "Debugging section(s), if specified, must appear together immediately after the
            // DECLARATIVES header." — so no other declarative section may precede one (kb/Work PB5081).
            if (_nonDebuggingDeclarativeSeen)
                ctx.Edition.Error(DiagnosticCatalog.DebuggingSectionPlacement, $"declarative section '{name}' is a debugging "
                    + "section and follows a declarative section that is not one; §1985:XV.3.2.3 SR1: \"Debugging section(s), if "
                    + "specified, must appear together immediately after the DECLARATIVES header.\"");
            info.MarkDebugging();
            ctx.Data.Symbols.ActivateDebugRegisters();   // make DEBUG-* references resolvable while this section binds
            _pendingDebug.Add((info, use));      // info.EndPc is filled after the body is collected below
            // fall through to collect the section body into the pc space (scope stays null — no BoundDeclarative)
        }
        else
        {
            _nonDebuggingDeclarativeSeen = true;
            scope = _use.DeclBindUse(use, name);
            // The USE statement is COMPLETE — fold its operands into the procedure division's registers. Until
            // this point a repeated operand belongs to the statement being bound, and §2023:14.9.49.3 SR7/SR8/SR9/
            // SR14 each forbid the repeat only across "more than one USE statement".
            _use.DeclEndUseStatement();
        }

        // Leading sentences past the USE form an anonymous paragraph at the section start (handler bodies that
        // CCVS writes directly under the section header) — §2023:14.4.3's paragraph-name-OMITTED paragraph, named
        // NOWHERE. ⛔ It used to be registered through AddParagraph under the SECTION's own name (kb/Work PB990), which
        // declared a paragraph the source never wrote: a legal `PERFORM handler-section` drew COBOL2121 "declared 2
        // times (paragraph 'H' in section 'H'; section 'H')", and §2023:8.3.2.2's one-type-per-word census found the section
        // name used as a paragraph-name.
        AddAnonymousParagraph(leading.Skip(1).ToArray(), info, used);
        foreach (var p in sec.declarativeParagraph())
            AddParagraph(p.paragraphName().procedureName(), p.sentence(), info, used);
        // A declarative section with ZERO paragraphs is legal (§2023:14.9.49.3 SR1 — "zero, one, or more procedural
        // paragraphs"; §2023:14.4.2), and it is still SELECTED: §2023:14.9.49.4 GR3 says "the first declarative that
        // satisfies the selection criteria is executed and no other declaratives are executed", so the search
        // stops at it and nothing else may run. Giving it ONE no-op pc keeps that a plain bounded dispatch
        // rather than a second selector shape — and it is the invariant DispatchState.RunUseCall asserts when it
        // refuses an empty PcRange. Relax this and the SELECTOR ARMS, not the renderer, are what must learn to
        // say "selected, ran nothing".
        if (_paras.Count == info.StartPc)
            AddEmptyAnonymousParagraph(info, used);

        info.CloseAt(_paras.Count - 1);
        // A METHOD's declarative section is a name in the METHOD's procedure division (§2023:8.3.2.2.28 — "A section-name
        // identifies a section in the procedure division"), declared where the method's other sections are — the
        // same split AddParagraph makes for paragraph-names (kb/Work PB1010).
        ctx.Symbols.DeclareSection(info, sec.sectionName().procedureName(), ctx.CurrentMethodScope);

        if (scope is { } s)
            // ISO §2023:14.9.49.3 SR1: "The remainder of the section shall consist of zero, one, or more procedural
            // paragraphs that define the procedures to be used" — so the use procedure IS the section, whose
            // extent §2023:14.4.2 fixes at the next section header or END DECLARATIVES. The range is taken whole;
            // nothing inspects paragraph shape to shorten it (kb/Work PB367).
            _declaratives.Add(new BoundDeclarative(
                name, info.Range, s.Files, s.ModeIndex, s.Global, s.Report, s.EcEntries, s.Eo));
    }

    // ── X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17) ────────────────────────────────────────────────
    // The debug declaratives collected under WITH DEBUGGING MODE (section info + the USE statement), pending
    // subject resolution once every nondeclarative procedure is in the pc space (DebugDeclarativeResolver.FinalizeDebug).
    private readonly List<(SectionInfo Section, Core.UseStatementContext Use)> _pendingDebug = [];

    /// <summary>Whether a declarative section that is not a debugging section has been collected — the fact §1985:XV.3.2.3
    /// SR1 asks of each debugging section after it (<see cref="DeclCollectSection"/>).</summary>
    private bool _nonDebuggingDeclarativeSeen;
    private readonly List<BoundDebugSubject> _debugSubjects = [];

    /// <summary>Whether <paramref name="pc"/> lies in a USE FOR DEBUGGING section (a declarative section with no <see cref="BoundDeclarative"/>): the
    /// position fact the "not in the declaratives" placement rules ask through <see cref="EnclosingContext.InDeclarative"/> (kb/Work PB5275).</summary>
    public bool InDebuggingSection(int pc) => _pendingDebug.Exists(p => pc >= p.Section.StartPc && pc <= p.Section.EndPc);

    /// <summary>The X3.23-1985 debug-facility trigger subjects (VCR Table 7 row 7.17) — each a nondeclarative
    /// procedure whose entry the emitter instruments to populate DEBUG-ITEM and run the debugging declarative;
    /// empty unless a procedure-subject USE FOR DEBUGGING was collected under WITH DEBUGGING MODE.</summary>
    public IReadOnlyList<BoundDebugSubject> DebugSubjects => _debugSubjects;

    /// <summary>Whether a debug subject lies in the declaratives portion - a USE procedure, whose execution a statement causes
    /// (§1985:XV.3.2.4 GR21), so each statement carries the DEBUG-LINE that cause reports (<see cref="BoundStatement.DebugLine"/>).</summary>
    public bool DebugsUseProcedure => _debugSubjects.Any(s => s.SubjectPc < _entryPc);

    private readonly List<BoundDebugTarget> _debugTargets = [];

    /// <summary>The widest DEBUG-CONTENTS the debug targets need, as <see cref="DebugDeclarativeResolver.FinalizeDebug"/> returns it.</summary>
    private int _debugContentsWidth = WiseOwl.Cobol.Runtime.DebugItem.MinContentsWidth;

    /// <summary>The data-name, ALL REFERENCES OF and cd-name USE FOR DEBUGGING targets, resolved by <see cref="DebugDeclarativeResolver.FinalizeDebug"/> before any
    /// statement binds (X3.23-1985 USE FOR DEBUGGING general rules 5, 6 and 8; docs/CONTROL_FLOW_DESIGN.md D20). The statement
    /// binder's one funnel plans each statement's debug fires against them (<c>DebugTriggerPlanner</c>); empty unless WITH DEBUGGING MODE
    /// collected such a declarative.</summary>
    public IReadOnlyList<BoundDebugTarget> DebugTargets => _debugTargets;

    /// <summary>The DEBUG-CONTENTS width the register is declared with: the largest of 30 and every target's image width
    /// (X3.23-1985 USE FOR DEBUGGING general rule 15).</summary>
    public int DebugContentsWidth => ctx.Data.State.Implicit.DebugContentsWidth;

    /// <summary>Whether a debugging section activated the DEBUG-ITEM register family (the only programs in which DEBUG-ITEM names anything).</summary>
    public bool HasDebugRegisters => ctx.Data.State.Implicit.DebugRegisters.Count > 0;

    /// <summary>Whether <paramref name="item"/> is the DEBUG-ITEM group or one of its named members (X3.23-1985 USE FOR DEBUGGING syntax rule 11).</summary>
    public bool IsDebugRegister(DataItem item) => ctx.Data.State.Implicit.IsDebugRegister(item);

    /// <summary>The three DEBUG-SUB-n items (general rule 10), each its own `DataItem` with its own profile; empty when no debugging section activated the register.</summary>
    public IReadOnlyList<DataItem> DebugSubRegisters => ctx.Data.State.Implicit.DebugSubRegisters;
}
