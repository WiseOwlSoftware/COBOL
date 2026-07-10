// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;        // Place subtypes (RefModPlace, …), PicCategory, Usage
using CobolNet.Binding.Bound;
using CobolNet.CodeGen;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog / EditionDiagnostic / EditionCodes / EditionSeverity(Policy) — the §8.9 funnel
using CobolNet.Frontend.Generated;     // CobolParserCore / CobolLexer / CobolParserCoreBaseVisitor — the parse-tree arm

namespace CobolNet.Validation;

/// <summary>
/// THE single post-bind edition-conformance pass (rearch PHASE-03 Step 14;
/// <c>docs/rearchitecture/DESIGN-version-conformance-pipeline.md</c> §2.4). It runs over the bound run unit AFTER
/// bind and BEFORE emit and is the sole owner of edition gating, routing every version-gated construct through the
/// ONE <see cref="ConstructRegistry.Check"/> funnel so the binder is edition-AGNOSTIC (zero <c>Check</c> calls of
/// its own).
/// </summary>
/// <remarks>
/// TWO-ARM by design (§1/§2.4 of the pipeline design):
/// <list type="bullet">
/// <item><b>Parse-tree arm</b> (<see cref="ParseArm"/>) — walks the raw <c>CompilationUnitContext</c> and fires
/// every SYNTACTIC introduction/removal/phrase gate + the §8.9 reserved-word funnel on the construct's
/// RECOGNITION (it absorbs the former <c>EditionValidator</c>). Recognition-based because an introduction gate
/// must name its edition even when the below-edition construct ALSO has a semantic error: the bound node it would
/// have produced is dropped (<c>BoundUnsupported</c>/<c>BoundNop</c>) on that path, but its PARSE node is always
/// present (the load-bearing finding, DEVLOG 724 — a bound-arm intro gate silently dropped the 0900).</item>
/// <item><b>Bound-tree arm</b> (<see cref="GateStatement"/>/<see cref="GateMove"/>) — the END-STATE home of only
/// the genuinely-SEMANTIC gates whose identity is a RESOLVED bound fact, never mere presence: MOVE
/// figurative-category (source × each receiver's picture), and the gates conditioned on file-organization /
/// access-mode / USAGE / pointer-category (SET object-reference, SET pointer UP/DOWN, INVOKE, READ PREVIOUS,
/// START FIRST/LAST, START WITH LENGTH, READ ADVANCING ON LOCK). A naive parse-arm would over- or under-fire
/// these. No bound node carries a raw parse context — the <c>BoundTree.cs</c> invariant stands.</item>
/// </list>
/// The two arms are disjoint: a <c>Check</c> for any one construct fires from EXACTLY one arm, the losing site
/// deleted as its twin lands. <b>Migration state:</b> Step 14h (DONE) moved every SYNTACTIC statement/expression/
/// literal gate + the §8.9 funnel to the parse-arm (14h.1 foundation → 14h.4b), so the binder holds ZERO
/// statement-level edition Checks. Step 14g is relocating the DATA/PICTURE/OO gates: <b>14g.1 (DONE)</b> moved the 8
/// PicInfo USAGE / PICTURE-category gates to the bound-arm <see cref="GateData"/> enumerator over
/// <c>DataBinder.ConformanceForest()</c>; <b>14g.2 (DONE)</b> moved all four data-description-clause gates
/// (BASED/TYPE/PROPERTY/TYPEDEF) to the parse-arm on RECOGNITION — none needs a resolved fact, and a bound-arm home
/// drops the 0900 on a declaration-error path (the review correction, DEVLOG 734: even init-only <c>IsTypedef</c>
/// fails, because the typedef ITEM is discarded from the forest when RegisterTypeDecl rejects it or it binds into
/// method scope). STILL
/// bind-time pending: OO class/interface + OCCURS-DYNAMIC (→ parse-arm), SPECIAL-NAMES-FOR + file SHARING/LOCK-MODE
/// (→ bound-arm) + PD RETURNING/RAISING (→ parse-arm), and FUNCTION-PROTOTYPE + REPOSITORY + skeleton E/national-edited
/// (14g.3–14g.5; the plan is in the PHASE-03 doc). The one principled exception is the
/// UDF-invocation gate (an intrinsic FUNCTION and a user-function call are
/// syntactically identical — only the repository-resolved name set separates them), which stays BIND-TIME
/// (<c>StatementBinder.Udf.cs</c>) where it already fires on recognition before operand binding.
/// </remarks>
internal sealed class VersionConformancePass
{
    private readonly EditionInfo _edition;
    private readonly IDiagnosticSink _sink;

    private VersionConformancePass(EditionInfo edition, IDiagnosticSink sink)
    {
        _edition = edition;
        _sink = sink;
    }

    /// <summary>Gate every version-gated construct in the bound <paramref name="group"/>, reporting to
    /// <paramref name="sink"/>. The driver runs this between bind and emit and HALTs before emit if the sink then
    /// carries errors (rearch exit criterion 9 — no codegen on an errored tree).</summary>
    public static void Run(CSharpEmitter.BoundRunUnit group, EditionInfo edition, IDiagnosticSink sink)
    {
        var pass = new VersionConformancePass(edition, sink);
        // ── PARSE-tree arm (Step 14h): ONE walk of the raw compilation unit, firing every SYNTACTIC
        //    introduction/removal/phrase gate + the §8.9 reserved-word funnel on the construct's RECOGNITION
        //    (absorbs the former EditionValidator). Recognition-based so a below-edition construct that ALSO
        //    has a semantic error still names its edition — the bound node it would have produced may be
        //    dropped (BoundUnsupported/BoundNop), but its parse node is always present (DEVLOG 724). ──
        new ParseArm(pass).Visit(group.Tree);
        // ── BOUND-tree arm: the genuinely-SEMANTIC gates (MOVE figurative-category; the file-org / USAGE /
        //    pointer-category conditioned STATEMENT gates) + the DATA-attribute gates (Step 14g — every
        //    source-declared DataItem's resolved USAGE / PICTURE category), which need a resolved bound fact. ──
        foreach (var unit in group.Units)
        {
            pass.WalkProgram(unit.Bound);
            pass.GateData(unit.Data);
        }
        foreach (var cls in group.Classes)
        {
            // A class body's OBJECT and FACTORY halves are two bound programs over one class; both carry statements
            // (each METHOD-ID is a pc slice of the ONE dispatch space, so walking Paragraphs covers every method).
            pass.WalkProgram(cls.Bound);
            pass.WalkProgram(cls.FactoryBound);
            pass.GateData(cls.Data);
            pass.GateData(cls.FactoryData);
        }
    }

    private void WalkProgram(BoundProgram? prog)
    {
        if (prog is null) return;
        foreach (var para in prog.Paragraphs)
            foreach (var stmt in para.Statements)
                WalkStatement(stmt);
    }

    private void WalkStatement(BoundStatement s)
    {
        GateStatement(s);
        Recurse(s);
    }

    private void WalkList(IReadOnlyList<BoundStatement>? list)
    {
        if (list is null) return;
        foreach (var s in list) WalkStatement(s);
    }

    private void Check(string constructId, string where)
        => ConstructRegistry.Check(_edition, _sink, constructId, where);

    // ── The bound-tree arm — statement-level edition gates ──────────────────────────────────────────────────
    // The constructs whose IDENTITY is the bound-node TYPE or a resolved node ATTRIBUTE. Step 14h migrates the
    // SYNTACTIC introduction/removal/phrase gates from here to the ParseArm (fire-on-recognition, DEVLOG 724);
    // the genuinely-SEMANTIC gates (MOVE-category, file-org/USAGE/pointer-category conditioned) stay. During the
    // 14h.2–14h.4 migration a Check fires from EXACTLY one arm — the losing site is deleted as its parse-arm
    // twin lands. Each Check keeps the exact constructId + where-string of its former binder site (byte-identical).
    private void GateStatement(BoundStatement s)
    {
        switch (s)
        {
            // (UNLOCK / FREE / ALTER / DELETE FILE / SET ADDRESS / ALLOCATE — self-identifying statement gates —
            // migrated to the ParseArm in Step 14h.2: they fire on RECOGNITION, so a below-edition occurrence
            // names its edition even when the statement also fails to bind, DEVLOG 724.)
            case BoundSetObjectRef:
                Check(Constructs.SetObjectReference2002, "the SET … TO object-reference statement (Format 5)"); break;
            case BoundSetPointerUpDown:
                Check(Constructs.PointerArithmetic2002, "SET pointer UP/DOWN BY (ISO §14.9.39 Format 10)"); break;

            // (OPEN-SHARING / GOBACK-RETURNING / CALL-BY-VALUE / CALL-ON-OVERFLOW / STOP-RUN-STATUS / END-ACCEPT —
            // phrase gates whose presence is purely syntactic — migrated to the ParseArm in Step 14h.3.)
            case BoundInvoke or BoundInvokeUniversal:
                Check(Constructs.Invoke2002, "the INVOKE statement"); break;
            case BoundMove mv:
                GateMove(mv); break;
            case BoundKeyedRead kr:
                // Two independent 2002 phrases on one READ; both gate, in the binder's order (§14.9.30).
                if (kr.Kind == KeyedReadKind.Previous)
                    Check(Constructs.ReadPrevious2002, "READ … PREVIOUS");
                if (kr.AdvancingOnLock)
                    Check(Constructs.RecordLockPhrase2002, "the READ … ADVANCING ON LOCK phrase");
                break;
            case BoundKeyedStart ks:
                // START FIRST/LAST positioning (§14.9.41) and the WITH LENGTH partial-key phrase — two independent
                // 2002 introductions; both gate, in the binder's order.
                if (ks.Mode is KeyedStartMode.First or KeyedStartMode.Last)
                    Check(Constructs.StartFirstLast2002, $"START {(ks.Mode == KeyedStartMode.Last ? "LAST" : "FIRST")}");
                if (ks.Length is not null)
                    Check(Constructs.StartWithLength2002, "the START … WITH LENGTH phrase");
                break;
        }
    }

    // ── MOVE figurative-constant category gates (ISO §14.9.25.3 SR5) ─────────────────────────────────────────
    // Genuinely SEMANTIC: which of the three edition rows applies depends on the source figurative × each
    // receiver's RESOLVED picture — re-derived here from the bound MOVE (Group B). Mirrors the binder's former
    // MoveFigurativeEditionGates classification EXACTLY (same figText, same per-target exemptions, same
    // integer/QUOTE/digit-only split, same where-string); the binder keeps only the SR1 class-index error (0809,
    // version-invariant) and the pre-removal StoreAsImage marking.
    private void GateMove(BoundMove m)
    {
        var all = m.Source as BoundAllLiteral;
        string figText = m.Source switch
        {
            BoundFigurative { Kind: 'S' } => "SPACE",
            BoundFigurative { Kind: 'Q' } => "QUOTE",
            BoundFigurative { Kind: 'H' } => "HIGH-VALUE",
            BoundFigurative { Kind: 'L' } => "LOW-VALUE",
            BoundAllLiteral a => $"ALL \"{a.Literal}\"",
            _ => string.Empty,
        };
        if (figText.Length == 0) return;   // not an alphanumeric-figurative / ALL source — SR5 does not reach it
        foreach (var t in m.Targets)
        {
            // Exemptions (§14.9.25.3 SR5): a ref-mod receiver (unique elementary alphanumeric), a group receiver
            // (a conversion-free character copy), a non-numeric receiver, or class index (SR1-errored in the binder).
            if (t is RefModPlace || t.Item.IsGroup || t.Item.Pic is not { } pic) continue;
            if (pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited)) continue;
            if (pic.Usage is Usage.Index) continue;
            string where = $"MOVE {figText} TO {t.Item.CobolName}";
            bool integerReceiver = pic is { Category: PicCategory.Numeric, IsFloat: false, Scale: <= 0 };
            if (all is { IsDigitOnly: true, Literal.Length: 1 } && integerReceiver)
                // SR5's surviving exception — valid everywhere, obsolete at 2023 (0903; SR5 NOTE / Annex F.2).
                Check(Constructs.MoveAllDigitIntegerObsolete2023, where);
            else if (m.Source is BoundFigurative { Kind: 'Q' })
                // QUOTE→numeric — obsolete 2014 (Annex E.2 item 21) then removed 2023 (dual-window row).
                Check(Constructs.MoveQuoteNumericObsolete2014, where);
            else
                // Every other shape — REMOVED by ISO 2023 (Annex E.2 item 1 bullet 1; 0902 — VCR row 1).
                Check(Constructs.MoveAlphanumericFigurativeRemoved2023, where);
        }
    }

    // ── The complete nested-statement traversal ─────────────────────────────────────────────────────────────
    // EVERY container that can hold a gated statement is descended, so a gate nested inside IF / EVALUATE / PERFORM
    // / SEARCH / an ON-phrase escapes nothing. Cross-checked against the binder's own traversals
    // (BoundStores.StoreKindOf + the phrase fields); a missed container would silently drop a nested gate. Leaves
    // (the default arm) yield no children.
    private void Recurse(BoundStatement s)
    {
        switch (s)
        {
            case BoundSequence x: WalkList(x.Steps); break;
            case BoundEcChecked x: WalkStatement(x.Inner); break;
            case BoundIf x: WalkList(x.Then); WalkList(x.Else); break;
            case BoundEvaluate x:
                foreach (var w in x.Whens) WalkList(w.Statements);
                WalkList(x.Other); break;
            case BoundInlinePerform x: WalkList(x.Body); break;
            case BoundAddTo x: WalkSizeErr(x.SizeError); break;
            case BoundAddGiving x: WalkSizeErr(x.SizeError); break;
            case BoundSubtractFrom x: WalkSizeErr(x.SizeError); break;
            case BoundSubtractGiving x: WalkSizeErr(x.SizeError); break;
            case BoundMultiplyBy x: WalkSizeErr(x.SizeError); break;
            case BoundMultiplyGiving x: WalkSizeErr(x.SizeError); break;
            case BoundDivideInto x: WalkSizeErr(x.SizeError); break;
            case BoundDivideGiving x: WalkSizeErr(x.SizeError); break;
            case BoundDivideRemainder x: WalkSizeErr(x.SizeError); break;
            case BoundCompute x: WalkSizeErr(x.SizeError); break;
            case BoundCorresponding x: WalkSizeErr(x.SizeError); break;
            case BoundSearch x:
                WalkList(x.AtEnd);
                foreach (var w in x.Whens) WalkList(w.Statements); break;
            case BoundStringStmt x: WalkList(x.OnOverflow); WalkList(x.NotOnOverflow); break;
            case BoundUnstringStmt x: WalkList(x.OnOverflow); WalkList(x.NotOnOverflow); break;
            case BoundWrite x: WalkList(x.AtEop); WalkList(x.NotAtEop); break;
            case BoundRead x: WalkList(x.AtEnd); WalkList(x.NotAtEnd); break;
            case BoundKeyedRead x:
                WalkList(x.AtEnd); WalkList(x.NotAtEnd);
                WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedWrite x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedRewrite x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedDelete x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedStart x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedDeleteFile x: WalkList(x.OnException); WalkList(x.NotOnException); break;
            case BoundReturn x: WalkList(x.AtEnd); WalkList(x.NotAtEnd); break;
            case BoundCallProgram x: WalkList(x.OnException); WalkList(x.NotOnException); break;
            default: break;   // a leaf statement — no nested statements
        }
    }

    private void WalkSizeErr(SizeErrorPhrase? p)
    {
        if (p is null) return;
        WalkList(p.OnError);
        WalkList(p.NotOnError);
    }

    // ── Step 14g: the DATA-attribute (USAGE / PICTURE-category) edition gates ────────────────────────────────
    // Genuinely SEMANTIC — identity is a RESOLVED DataItem attribute (its USAGE / PICTURE category), never parse
    // presence, so a bound-arm walk is the correct home. Fires ONCE per SOURCE declaration: DataBinder's
    // ConformanceForest excludes the post-bind TYPE-clones + compiler temps (which the binder never re-analyzed
    // and so never gated), reproducing the former per-entry PicInfo.ParseUsage/Analyze gates byte-for-byte.
    private void GateData(DataBinder data)
    {
        foreach (var item in data.ConformanceForest())
            GateDataItem(item, $"data item '{item.CobolName ?? "FILLER"}'");
        // Report printable items are SYNTHETIC DataItems in the RD model (off the forest) — their national/boolean
        // PICTURE gates fired in Analyze with the report where-string (DataBinder.Reports.cs); reproduce that here.
        foreach (var report in data.Reports)
            foreach (var grp in report.Groups)
                foreach (var line in grp.Lines)
                    foreach (var field in line.Fields)
                        GateDataItem(field.PrintItem,
                            $"RD '{report.Name}' printable item '{field.PrintItem.CobolName ?? "FILLER"}'");
    }

    /// <summary>Gate one resolved DataItem's USAGE / PICTURE-category edition attribute. At most ONE gate fires
    /// (the categories are mutually exclusive).</summary>
    private void GateDataItem(DataItem item, string where)
    {
        if (UsageGateId(item) is { } id) Check(id, where);
    }

    /// <summary>The 2002-introduction USAGE / PICTURE-category of a resolved item, or null when version-invariant.
    /// Keyed on the resolved <c>(OwnUsage, Pic.Category, Pic.Usage)</c>: <see cref="DataItem.OwnUsage"/> is mandatory
    /// because a group-header USAGE sheds <c>Pic</c> to null (<c>DataBinder.ResolveIndexItems</c>), leaving only the
    /// own keyword; the <c>Pic.Usage</c> member (never <c>IsFloat</c>/<c>ClrType</c>) carries the identity for the
    /// picture-less usages — <c>FloatLong</c>/<c>FloatExtended</c> share a <c>double</c> ClrType with COMP-2.</summary>
    private static string? UsageGateId(DataItem item)
    {
        var cat = item.Pic?.Category;
        var pu = item.Pic?.Usage;
        var ou = item.OwnUsage;
        return
            cat is PicCategory.National || ou is Usage.National ? Constructs.NationalData2002
            : cat is PicCategory.Boolean || ou is Usage.Bit ? Constructs.BooleanData2002
            : pu is Usage.Pointer || ou is Usage.Pointer ? Constructs.UsagePointer2002
            : cat is PicCategory.ObjectReference || pu is Usage.ObjectReference || ou is Usage.ObjectReference
                ? Constructs.UsageObjectReference2002
            : pu is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
              || ou is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? Constructs.UsageBinaryCharFamily2002
            : pu is Usage.FloatShort || ou is Usage.FloatShort ? Constructs.UsageFloatShort2002
            : pu is Usage.FloatLong || ou is Usage.FloatLong ? Constructs.UsageFloatLong2002
            : pu is Usage.FloatExtended || ou is Usage.FloatExtended ? Constructs.UsageFloatExtended2002
            : null;
    }

    /// <summary>
    /// Whether this <c>cobolWord</c> occurrence sits in a grammar position that is UNAMBIGUOUSLY a
    /// user-defined-word use (ISO §8.3.2.2), so a context-keyword token there is provably the program NAMING
    /// something with that word — the §8.3.2.1 rule-1 violation ("reserved words shall not be used as
    /// user-defined words"; restated §8.3.2.4.1) the 0901 band enforces. CONSERVATIVE by design (P2.8 W2 —
    /// the RW104A adversarial review): a slot qualifies only when NO clause/statement keyword admitted by
    /// <c>cobolWord</c> can legally occupy it, proven against the grammar rule by rule below. Mis-parse-prone
    /// OPTIONAL entry-name slots stay UNCHECKED — the report-group entry-name (<c>reportGroupName?</c>
    /// swallows the keyword of RW102A/103A/104A's report COLUMN clause, §13.18.14) and the screen entry-name
    /// (<c>screenName?</c> would swallow a screen attribute keyword) — as does every REFERENCE position
    /// (dataReference, qualification, procedure-name refs, DISPLAY UPON, SPECIAL-NAMES operands, …), keeping
    /// the funnel's no-false-reject guarantee (VCR scope-limit rule). Consumed by the <see cref="ParseArm"/>
    /// §8.9 funnel and by <c>ReservedWordPositionTests</c> (kept <c>internal static</c> for that witness).
    /// </summary>
    internal static bool IsProvableUserWordPosition(CobolParserCore.CobolWordContext ctx) => ctx.Parent switch
    {
        // The data-description / linkage-parameter ENTRY-NAME slot (§13.16 level-number data-name-1): the
        // slot is optional, but NO dataDescriptionClause alternative begins with a cobolWord-admitted token
        // (PIC/USAGE/OCCURS/VALUE/… are dedicated tokens outside cobolWord; bare NATIONAL/BIT usages require
        // the USAGE prefix in this grammar), so a cobolWord token here is always the entry's name.
        CobolParserCore.DataNameContext
        {
            Parent: CobolParserCore.DataDescriptionEntryContext
                or CobolParserCore.LinkageProcedureParameterContext
        } => true,
        // A paragraph/section DEFINITION (§14.4.2/§14.4.3: section-name SECTION. / paragraph-name.): the name
        // stands at a procedure-unit boundary followed by [SECTION] DOT — no statement in cobolWord's token
        // set can begin a sentence there, so the word is the procedure's name. REFERENCES (GO TO / PERFORM /
        // RESUME AT targets) route through procedureName under OTHER parents and stay unchecked.
        CobolParserCore.ProcedureNameContext
        {
            Parent: CobolParserCore.ParagraphNameContext or CobolParserCore.SectionNameContext
        } => true,
        // The SELECT clause file-name (§12.4.5.1 general formats: SELECT [OPTIONAL] file-name-1): the name is
        // mandatory and directly follows SELECT [OPTIONAL], before any file-control clause keyword — the slot
        // is always the file being DEFINED. FD/statement file-name REFERENCES stay unchecked.
        CobolParserCore.FileNameContext { Parent: CobolParserCore.FileControlClauseGroupContext } => true,
        // The PROGRAM-ID / FUNCTION-ID / END-marker program-name (§11.4.2 / §11.5 / §10.6.1): program-names
        // are user-defined words (§8.3.2.2) and every programName site names the source unit itself, directly
        // after a dedicated header token — no keyword can occupy the slot.
        CobolParserCore.ProgramNameContext => true,
        _ => false,
    };

    /// <summary>
    /// The parse-tree arm of the conformance pass (rearch PHASE-03 Step 14h): the ABSORBED
    /// <c>EditionValidator</c>. It walks the raw compilation unit (ANTLR <c>-no-listener -visitor</c>, so
    /// every override returns <c>base.VisitChildren</c>/<c>base.VisitXxx</c> to keep descending) and routes
    /// every SYNTACTIC edition gate through the enclosing pass's <see cref="Check"/> — one policy, several
    /// emit sites. Recognition-based (fires on the construct's OWN tokens being present), so it names an
    /// edition even for a below-edition construct that ALSO fails to bind. Runs AFTER bind (the driver
    /// dropped the pre-bind fail-fast), so a below-edition construct surfaces BOTH its edition diagnostic and
    /// its bind diagnostics — intended (both are true; the tests are contains-based).
    /// </summary>
    private sealed class ParseArm(VersionConformancePass p) : CobolParserCoreBaseVisitor<object?>
    {
        private readonly VersionConformancePass _p = p;
        // The effective reserved-word set for THIS compilation unit (P2.4/D9 seam): the generated §8.9 table is
        // only the default layer — the 2023 COBOL-WORDS directive mutates the set per unit (roadmap Phase 7).
        private readonly ReservedWordSet _reservedWords = ReservedWordSet.Default;
        // One COBOLNET0901 per distinct word per compilation (P2.4) — not one per occurrence.
        private HashSet<string>? _flaggedWords;
        // Whether the CURRENT source unit declares SOURCE-COMPUTER … WITH DEBUGGING MODE (the X3.23-1985
        // compile-time debug switch): decides the USE FOR DEBUGGING posture (VCR Table 7 row 7.17) — without it
        // a debugging section is comment-treated (never walked); with it, DEBUG-* register references diagnose
        // 0899 (the deferred facility), not 0901. Reset per top-level program unit; nestedProgram subtrees are
        // walked within the outer unit, so contained programs INHERIT the switch (the '85 rule).
        private bool _debuggingModeDeclared;

        /// <summary>Reset the per-source-unit state at each TOP-LEVEL unit (nested programs keep the outer's).</summary>
        public override object? VisitProgramUnit(CobolParserCore.ProgramUnitContext ctx)
        {
            _debuggingModeDeclared = false;
            return base.VisitChildren(ctx);
        }

        // ── P2.6 removal gates: every override routes through the pass's ONE Check (one policy) ────────────

        /// <summary>LABEL RECORDS (FD) — obsolete '85 element DELETED by ISO/IEC 1989:2002; the 2023 FD clause
        /// set (§13.18) has no LABEL clause.</summary>
        public override object? VisitLabelRecordsClause(CobolParserCore.LabelRecordsClauseContext ctx)
        {
            _p.Check(Constructs.LabelRecordsRemoved2002, "the FD LABEL RECORDS clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>VALUE OF (FD) — obsolete '85 label-field clause, deleted 2002 (P2.6).</summary>
        public override object? VisitValueOfClause(CobolParserCore.ValueOfClauseContext ctx)
        {
            _p.Check(Constructs.ValueOfRemoved2002, "the FD VALUE OF clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>DATA RECORDS (FD **and** SD — one grammar rule, so ONE enforcement site). Keeps its pinned
        /// 0873.</summary>
        public override object? VisitDataRecordsClause(CobolParserCore.DataRecordsClauseContext ctx)
        {
            _p.Check(Constructs.DataRecordsRemoved2002, "the FD/SD DATA RECORDS clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>MULTIPLE FILE [TAPE] (I-O-CONTROL) — reel-sharing description, deleted 2002 (P2.6).</summary>
        public override object? VisitMultipleFileClause(CobolParserCore.MultipleFileClauseContext ctx)
        {
            _p.Check(Constructs.MultipleFileTapeRemoved2002, "the I-O-CONTROL MULTIPLE FILE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SOURCE-/OBJECT-COMPUTER attribute SINK (the grammar swallows the obsolete '85 clauses as
        /// raw tokens — <c>~(DOT|PROGRAM)+</c>), so the deleted elements hiding in it are gated by TOKEN-TEXT scan
        /// (P2.6): MEMORY SIZE, SEGMENT-LIMIT, WITH DEBUGGING MODE — each its own registry row/VCR item.</summary>
        public override object? VisitComputerAttributes(CobolParserCore.ComputerAttributesContext ctx)
        {
            for (int i = 0; i < ctx.ChildCount; i++)
            {
                switch (ctx.GetChild(i).GetText().ToUpperInvariant())
                {
                    case "MEMORY":
                        _p.Check(Constructs.MemorySizeRemoved2002, "the OBJECT-COMPUTER MEMORY SIZE clause");
                        break;
                    case "SEGMENT-LIMIT":
                        _p.Check(Constructs.SegmentLimitRemoved2002, "the OBJECT-COMPUTER SEGMENT-LIMIT clause");
                        break;
                    case "DEBUGGING":
                        _p.Check(Constructs.DebuggingModeRemoved2002, "the SOURCE-COMPUTER WITH DEBUGGING MODE clause");
                        // The switch also drives the USE FOR DEBUGGING posture (row 7.17): the configuration
                        // section precedes the procedure division in the walk, so the flag is set before any
                        // declarative section is visited.
                        _debuggingModeDeclared = true;
                        break;
                }
            }
            return base.VisitChildren(ctx);
        }

        /// <summary>The five identification comment paragraphs — obsolete '85 elements deleted 2002 (P2.6; one
        /// registry row, the paragraph named per site).</summary>
        public override object? VisitAuthorParagraph(CobolParserCore.AuthorParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the AUTHOR paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitInstallationParagraph(CobolParserCore.InstallationParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the INSTALLATION paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitDateWrittenParagraph(CobolParserCore.DateWrittenParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the DATE-WRITTEN paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitDateCompiledParagraph(CobolParserCore.DateCompiledParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the DATE-COMPILED paragraph"); return base.VisitChildren(ctx); }
        public override object? VisitSecurityParagraph(CobolParserCore.SecurityParagraphContext ctx)
        { _p.Check(Constructs.IdentificationCommentsRemoved2002, "the SECURITY paragraph"); return base.VisitChildren(ctx); }

        /// <summary>REMARKS — a '74 carryover the grammar accepts for CCVS; flagged ≥2002 ONLY (never at 85 —
        /// CCVS-85 programs write it; the 85 FIPS flagger is future strictness work, P2.6).</summary>
        public override object? VisitRemarksParagraph(CobolParserCore.RemarksParagraphContext ctx)
        { _p.Check(Constructs.RemarksRemoved2002, "the REMARKS paragraph"); return base.VisitChildren(ctx); }

        /// <summary>STOP literal (Format 2) — obsolete '85 element deleted 2002 (P2.6). The STOP RUN … WITH
        /// STATUS phrase (Format 1, a 2002 INTRODUCTION) is the disjoint alternative, gated in the same
        /// override (14h.3).</summary>
        public override object? VisitStopStatement(CobolParserCore.StopStatementContext ctx)
        {
            if (ctx.literal() is not null)
                _p.Check(Constructs.StopLiteralRemoved2002, "the STOP literal statement");
            // STOP RUN … WITH NORMAL/ERROR STATUS (Format 1) — a COBOL-2002 INTRODUCTION (14h.3), a disjoint
            // alternative from the STOP literal (Format 2) removal above; both fire from this one override.
            if (ctx.stopStatusPhrase() is not null)
                _p.Check(Constructs.StopRunStatus2002, "the STOP RUN … WITH NORMAL/ERROR STATUS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>OPEN … REVERSED — obsolete '85 tape phrase deleted 2002 (P2.6; NO REWIND stays — it survives
        /// into 2023 §14.9.26).</summary>
        public override object? VisitOpenFileSpec(CobolParserCore.OpenFileSpecContext ctx)
        {
            if (ctx.REVERSED() is not null)
                _p.Check(Constructs.OpenReversedRemoved2002, "the OPEN REVERSED phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>CLOSE … WITH LOCK — REMOVED 2014→2023 (Annex E deletion; VCR row 7; P2.6).</summary>
        public override object? VisitCloseOption(CobolParserCore.CloseOptionContext ctx)
        {
            if (ctx.LOCK() is not null)
                _p.Check(Constructs.CloseWithLockRemoved2023, "the CLOSE WITH LOCK phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>EXIT METHOD / EXIT FUNCTION — introduced 2002 (0900 below), REMOVED 2023 (Annex E; VCR rows
        /// 5/6) — the dual-obligation window rows; EXIT PROGRAM — ARCHAIC in 2023 (0903 warning; VCR 89).</summary>
        public override object? VisitExitStatement(CobolParserCore.ExitStatementContext ctx)
        {
            if (ctx.METHOD() is not null)
                _p.Check(Constructs.ExitMethodWindow, "the EXIT METHOD statement");
            else if (ctx.FUNCTION() is not null)
                _p.Check(Constructs.ExitFunctionWindow, "the EXIT FUNCTION statement");
            else if (ctx.PROGRAM() is not null)
                _p.Check(Constructs.ExitProgramArchaic2023, "the EXIT PROGRAM statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>NEXT SENTENCE — ARCHAIC in 2023 (0903 warning; VCR 90; P2.6).</summary>
        public override object? VisitNextSentenceStatement(CobolParserCore.NextSentenceStatementContext ctx)
        {
            _p.Check(Constructs.NextSentenceArchaic2023, "the NEXT SENTENCE phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>A WORKING-STORAGE SECTION in a METHOD definition — legal 2002/2014 (D3: static-field
        /// semantics, shared across instances and persistent across activations, §11.7), BANNED by 2023
        /// (§13.5.3 SR 1). The dual window: 0900 below 2002, 0902 at 2023, silent between; under
        /// <c>--permissive</c> the pre-removal static semantics stand (the §10 #1 migration contract).</summary>
        public override object? VisitMethodDefinition(CobolParserCore.MethodDefinitionContext ctx)
        {
            if (ctx.dataDivision()?.workingStorageSection() is not null)
                _p.Check(Constructs.MethodWorkingStorageWindow,
                    "a WORKING-STORAGE SECTION in a method definition");
            return base.VisitChildren(ctx);
        }

        // ── The W3 notInGrammar 85-acceptance gates (VCR Table 7 rows 7.15–7.18; DEVLOG 599): four obsolete '85
        //    elements DELETED by ISO 2002 that formerly had no grammar at all. Each now parses unconditionally,
        //    binds inert at 85, and gates here. ──────────────────────────────────────────────────────────────

        /// <summary>RERUN (I-O-CONTROL) — the '85 checkpoint hint, deleted 2002. Parsed-and-ignored at 85.</summary>
        public override object? VisitRerunClause(CobolParserCore.RerunClauseContext ctx)
        {
            _p.Check(Constructs.RerunRemoved2002, "the I-O-CONTROL RERUN clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>ENTER — the '85 other-language entry statement, deleted 2002. Comment-equivalent (BoundNop)
        /// at 85 — the conforming COBOL-only posture.</summary>
        public override object? VisitEnterStatement(CobolParserCore.EnterStatementContext ctx)
        {
            _p.Check(Constructs.EnterRemoved2002, "the ENTER statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>USE FOR DEBUGGING — the '85 debug facility's declarative, deleted 2002 with the whole
        /// facility. Sub-shape gate on the one useStatement rule (the STOP-literal pattern).</summary>
        public override object? VisitUseStatement(CobolParserCore.UseStatementContext ctx)
        {
            if (ctx.DEBUGGING() is not null)
                _p.Check(Constructs.UseForDebuggingRemoved2002, "the USE FOR DEBUGGING declarative");
            return base.VisitChildren(ctx);
        }

        /// <summary>A section-header segment-number — the '85 Segmentation module's priority number, deleted
        /// 2002. Parsed and ignored at 85 (all segments resident — a conforming posture).</summary>
        public override object? VisitSectionDefinition(CobolParserCore.SectionDefinitionContext ctx)
        {
            if (ctx.integerLiteral() is not null)
                _p.Check(Constructs.SegmentNumbersRemoved2002,
                    $"the segment-number on section '{ctx.sectionName().GetText()}'");
            return base.VisitChildren(ctx);
        }

        /// <summary>The declarative-section twin of <see cref="VisitSectionDefinition"/> (one registry row, the
        /// site named per header). ALSO the '85 comment-treatment seam (row 7.17): without WITH DEBUGGING MODE,
        /// X3.23-1985 compiles a USE FOR DEBUGGING section as if it were comment lines — so the walk visits ONLY
        /// the USE statement (its ≥2002 0902 gate must still fire) and never the section body, keeping the §8.9
        /// funnel off the unimplemented DEBUG-* register references inside (DB103M is the corpus witness).</summary>
        public override object? VisitDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx)
        {
            if (ctx.integerLiteral() is not null)
                _p.Check(Constructs.SegmentNumbersRemoved2002,
                    $"the segment-number on declarative section '{ctx.sectionName().GetText()}'");
            if (!_debuggingModeDeclared
                && ctx.sentence() is { Length: > 0 } sentences
                && sentences[0].statement() is { Length: 1 } first
                && first[0].useStatement() is { } use && use.DEBUGGING() is not null)
            {
                Visit(use);
                return null;
            }
            return base.VisitChildren(ctx);
        }

        // ── Step 14g.2: the data-description-clause introduction gates (BASED / TYPE / PROPERTY / TYPEDEF) ─────
        // COBOL-2002 introductions on a data-description entry, ALL parse-arm (recognition). None needs a resolved
        // fact — each is a single dataDescriptionClause alternative, so the parse node IS the identity — and a
        // bound-arm home would DROP the 0900 on a declaration-error path (the DEVLOG-724 flaw): DataItem.IsBased is
        // reset to false for a LINKAGE item (DataBinder.Linkage.cs); DataItem.TypeRefName is nulled by ExpandTypes
        // the moment the clone is materialized; the PROPERTY identity is consumed entirely by the OO property binder;
        // and — the 14g.2-review correction (DEVLOG 734) — although DataItem.IsTypedef is init-only, the typedef
        // ITEM is discarded from ConformanceForest whenever RegisterTypeDecl rejects it (an unnamed/FILLER typedef, a
        // duplicate type-name) or it binds into a method's LocalRoots/StaticRoots (off the forest), so a bound-arm
        // TYPEDEF gate silently dropped the 0900 on exactly those paths. Recognition fixes all four uniformly: the
        // parse node is always present, so each fires once per written clause with the former BindEntry site's exact
        // constructId + where-string (byte-identical). A level-66/88 entry is intercepted by BindEntries BEFORE the
        // storage-clause loop (the former gate site), so a clause mis-attached to one was never gated — the
        // InConditionOrRenamesEntry guard reproduces that (the permissive grammar admits these clauses under a
        // level-88 dataDescriptionClauses body, where they are malformed and the 0900 would be spurious).

        /// <summary>The BASED clause (ISO §13.18.5) — a COBOL-2002 introduction (a storage template with an implicit
        /// data-address pointer). The level-01/77 placement SR stays in the binder; this arm only names the edition.</summary>
        public override object? VisitBasedClause(CobolParserCore.BasedClauseContext ctx)
        {
            if (!InConditionOrRenamesEntry(ctx)) _p.Check(Constructs.BasedClause2002, "the BASED clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The TYPE IS type-name clause (the TYPEDEF family, ISO §13.18.58; D17) — a COBOL-2002 introduction.
        /// Fires once per written <c>TYPE IS</c> occurrence: the ExpandTypes clones are DataItem objects, not parse
        /// nodes, so a TYPEDEF referenced N times yields exactly N typeClause nodes (matching the former per-entry
        /// binder Check). The §13.18.57.3 placement SRs stay bind-time.</summary>
        public override object? VisitTypeClause(CobolParserCore.TypeClauseContext ctx)
        {
            if (!InConditionOrRenamesEntry(ctx)) _p.Check(Constructs.TypeClause2002, "the TYPE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The PROPERTY clause (ISO §13.18.42, OO) — a COBOL-2002 introduction. The OO property SEMANTICS
        /// are bound independently in <c>DataBinder.Oo.OoBindPropertyClauses</c> (which reads the propertyClause node
        /// directly), so the storage-clause loop no longer touches it; this arm only gates the edition.</summary>
        public override object? VisitPropertyClause(CobolParserCore.PropertyClauseContext ctx)
        {
            if (!InConditionOrRenamesEntry(ctx)) _p.Check(Constructs.PropertyClause2002, "the PROPERTY clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The TYPEDEF [STRONG] clause (ISO §13.18.58; D17) — a COBOL-2002 introduction (a type DECLARATION).
        /// Recognition-based (the 14g.2-review correction, DEVLOG 734), NOT bound-arm: the typedef ITEM is dropped
        /// from ConformanceForest whenever RegisterTypeDecl rejects it (unnamed/FILLER, duplicate type-name) or it
        /// binds into method LocalRoots/StaticRoots, so a bound-arm gate lost the 0900 on those declaration-error
        /// paths. The parse node is always present — one Check per written TYPEDEF, matching the former binder site.</summary>
        public override object? VisitTypedefClause(CobolParserCore.TypedefClauseContext ctx)
        {
            if (!InConditionOrRenamesEntry(ctx)) _p.Check(Constructs.TypedefDef2002, "the TYPEDEF clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> (a data-description clause) sits inside a level-66 RENAMES or
        /// level-88 condition-name entry — the two levels <c>BindEntries</c> intercepts BEFORE the storage-clause
        /// loop (DataBinder.cs — <c>lvl is 66 or 88</c> → continue), so the former bind-time gate never ran for a
        /// clause mis-attached to one. The permissive grammar still admits these clauses under a level-88
        /// <c>dataDescriptionClauses</c> body, so the parse-arm skips them to stay byte-neutral. (Level-66 uses a
        /// <c>renamesClause</c> body that cannot contain them — guarded for symmetry with the binder's skip.)</summary>
        private static bool InConditionOrRenamesEntry(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.DataDescriptionEntryContext e)
                    return e.levelNumber()?.GetText() is "66" or "88";
            return false;
        }

        // ── Step 14g.3: the OO class/interface definition + OCCURS DYNAMIC gates ───────────────────────────────
        // Two OO-2002 introductions (a CLASS-ID / INTERFACE-ID compilation unit) + one 2014 introduction (OCCURS
        // DYNAMIC). All parse-arm: a class/interface gate would UNDER-count from a bound-arm home — OoClassTable
        // drops a duplicate/colliding definition with a `continue` BEFORE it enters the built table, yet the former
        // gate fired for EVERY parse node (before that continue) — and the OCCURS DYNAMIC gate would OVER-count a
        // per-DataItem bound-arm walk (a TYPEDEF template's dynamic table is cloned into each TYPE reference, but the
        // former gate fired once per source occursClause). Recognition fires once per parse node — byte-identical to
        // the former OoClassTable.Build / OdoBindOccursSpec Checks (same constructId + where-string, incl. the
        // name-embedding class/interface where-strings). Class/interface definitions are top-level compilation units
        // (not data-description entries), so they need no level-66/88 guard; OCCURS DYNAMIC does (the binder skipped
        // those levels before OdoBindOccursSpec ran).

        /// <summary>A class definition (CLASS-ID compilation unit, ISO §11.2/§11.3) — a COBOL-2002 introduction. The
        /// where-string embeds the class-name exactly as the former OoClassTable.Build site (<c>className(0)</c> — the
        /// class's own name, not an INHERITS base).</summary>
        public override object? VisitClassDefinition(CobolParserCore.ClassDefinitionContext ctx)
        {
            _p.Check(Constructs.ClassDefinition2002,
                $"class definition '{ctx.classIdParagraph().className(0).GetText()}' (CLASS-ID compilation unit)");
            return base.VisitChildren(ctx);
        }

        /// <summary>An interface definition (INTERFACE-ID compilation unit, ISO §11.5/§11.6) — a COBOL-2002
        /// introduction. The where-string embeds <c>interfaceName(0)</c> (the interface's own name), matching the
        /// former OoClassTable.Build site.</summary>
        public override object? VisitInterfaceDefinition(CobolParserCore.InterfaceDefinitionContext ctx)
        {
            _p.Check(Constructs.InterfaceDefinition2002,
                $"interface definition '{ctx.interfaceName(0).GetText()}' (INTERFACE-ID compilation unit)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The OCCURS DYNAMIC clause (a dynamic-capacity table, ISO §13.18.38 Format 4; data-model D9) — a
        /// COBOL-2014 introduction. Gated on the DYNAMIC alternative of the shared <c>occursClause</c> rule (matching
        /// <c>OdoBindOccursSpec</c>'s <c>occ.DYNAMIC()</c> test), once per source clause; a fixed / Format-2 OCCURS is
        /// untouched. The <see cref="InConditionOrRenamesEntry"/> guard reproduces the binder's level-66/88 skip.</summary>
        public override object? VisitOccursClause(CobolParserCore.OccursClauseContext ctx)
        {
            if (ctx.DYNAMIC() is not null && !InConditionOrRenamesEntry(ctx))
                _p.Check(Constructs.OccursDynamic2014, "the OCCURS DYNAMIC clause");
            return base.VisitChildren(ctx);
        }

        // ── Step 14h.2: the SELF-IDENTIFYING statement gates ─────────────────────────────────────────────────
        // Each construct is ONE dedicated grammar rule → the parse node IS the identity; the gate fires once per
        // statement on RECOGNITION (never per operand — a multi-operand FREE / a compound ALTER is ONE node), so a
        // below-edition occurrence names its edition even when the statement ALSO fails to bind (DEVLOG 724 — the
        // bound-arm dropped ALLOCATE's + DELETE FILE's 0900 on the BoundNop/BoundUnsupported error paths). Each
        // keeps its former bound-arm/bind-time where-string verbatim.

        /// <summary>UNLOCK (ISO §14.9.47) — a COBOL-2002 introduction.</summary>
        public override object? VisitUnlockStatement(CobolParserCore.UnlockStatementContext ctx)
        { _p.Check(Constructs.UnlockStatement2002, "the UNLOCK statement"); return base.VisitChildren(ctx); }

        /// <summary>FREE (ISO §14.9.15) — a COBOL-2002 introduction (one Check for the whole pointer list).</summary>
        public override object? VisitFreeStatement(CobolParserCore.FreeStatementContext ctx)
        { _p.Check(Constructs.Free2002, "the FREE statement"); return base.VisitChildren(ctx); }

        /// <summary>ALTER (ISO §14.9.2) — obsolete '85, REMOVED by 2002 (one Check for a compound ALTER).</summary>
        public override object? VisitAlterStatement(CobolParserCore.AlterStatementContext ctx)
        { _p.Check(Constructs.AlterRemoved2002, "the ALTER statement"); return base.VisitChildren(ctx); }

        /// <summary>DELETE FILE (ISO 2023 §14.9.10 Format 2) — a COBOL-2023 introduction. Recognition-based so a
        /// below-2023 DELETE FILE names its edition even when the file is undeclared (the binder returns a
        /// BoundUnsupported before a BoundKeyedDeleteFile — the bound-arm dropped it, DEVLOG 724).</summary>
        public override object? VisitDeleteFileStatement(CobolParserCore.DeleteFileStatementContext ctx)
        { _p.Check(Constructs.DeleteFile2023, "the DELETE FILE statement"); return base.VisitChildren(ctx); }

        /// <summary>SET ADDRESS OF (ISO §14.9.39 Format 7) — a COBOL-2002 introduction. The ONE
        /// <c>setAddressStatement</c> rule carries BOTH forms (receiver <c>SET ADDRESS OF x TO p</c> + sender
        /// <c>SET p TO ADDRESS OF x</c>), which the bound-arm identified as two node shapes; one parse override
        /// unifies them (one Check per statement).</summary>
        public override object? VisitSetAddressStatement(CobolParserCore.SetAddressStatementContext ctx)
        { _p.Check(Constructs.SetAddress2002, "SET ADDRESS OF (ISO §14.9.39 Format 7)"); return base.VisitChildren(ctx); }

        /// <summary>ALLOCATE (ISO §14.9.3) — a COBOL-2002 introduction. Recognition-based so a below-2002 ALLOCATE
        /// names its edition even when its RETURNING fails to resolve (SR3/0869; the bind-time gate that formerly
        /// carried this — StatementBinder.Ptr.cs — is removed this commit, DEVLOG 724).</summary>
        public override object? VisitAllocateStatement(CobolParserCore.AllocateStatementContext ctx)
        { _p.Check(Constructs.Allocate2002, "the ALLOCATE statement"); return base.VisitChildren(ctx); }

        // ── Step 14h.3: the PHRASE statement gates ───────────────────────────────────────────────────────────
        // A phrase's presence within a statement rule is purely syntactic — detectable from the parse tree without
        // any resolved fact. Each fires ONCE per statement (an Any-over-the-child-list where a statement can carry
        // the phrase on several clauses, matching the binder which collapsed them to one bound attribute). The
        // STOP RUN … STATUS gate rides the existing VisitStopStatement above.

        /// <summary>OPEN … SHARING (ISO §14.9.27) — a COBOL-2002 introduction. Fires ONCE per OPEN even when several
        /// openClauses carry SHARING (BindOpen collapses them to one BoundOpen.SharingOverride).</summary>
        public override object? VisitOpenStatement(CobolParserCore.OpenStatementContext ctx)
        {
            if (ctx.openClause().Any(c => c.sharingPhrase() is not null))
                _p.Check(Constructs.FileSharingClause2002, "the OPEN SHARING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>GOBACK … RETURNING/GIVING (ISO §14.9.18) — a COBOL-2002 introduction. The RETURNING operand is
        /// the ONLY direct <c>dataReference</c> child of <c>gobackStatement</c> (the RAISING tail nests its own), so
        /// its presence ≡ the phrase. EXCLUDE a method context: inside a METHOD, GOBACK is a method-return
        /// (<c>BoundMethodReturn</c>, ungated) — CallBindGoback short-circuits to OoBindMethodGoback when InMethod,
        /// so the former bound-arm gate (keyed on BoundGoback.ReturningSource) never saw it. A UDF GOBACK is NOT
        /// excluded (it binds through CallBindGoback like a program). Recognition-based so an undeclared RETURNING
        /// operand (a BoundUnsupported before BoundGoback) still names its edition (DEVLOG 724).</summary>
        public override object? VisitGobackStatement(CobolParserCore.GobackStatementContext ctx)
        {
            if (ctx.dataReference() is not null && !InMethodDefinition(ctx))
                _p.Check(Constructs.GobackReturning2002, "GOBACK … RETURNING");
            return base.VisitChildren(ctx);
        }

        /// <summary>CALL phrases: BY VALUE (§14.9.4, a 2002 introduction) then ON OVERFLOW (the COBOL-74 synonym for
        /// ON EXCEPTION, REMOVED at 2023) — both from the one CALL statement, in the binder's order (arguments bind
        /// before the exception phrases). BY VALUE fires ONCE per statement (Any over callArgument — the binder
        /// collapsed the per-argument checks); OVERFLOW fires on either exception phrase using the spelling
        /// (matching BoundCallProgram.UsedOverflowSpelling).</summary>
        public override object? VisitCallStatement(CobolParserCore.CallStatementContext ctx)
        {
            if (ctx.callUsingPhrase()?.callArgument().Any(a => a.callByValue() is not null) == true)
                _p.Check(Constructs.CallByValue2002, "the CALL … BY VALUE phrase");
            if (ctx.callOnExceptionPhrase()?.OVERFLOW() is not null
                || ctx.callNotOnExceptionPhrase()?.OVERFLOW() is not null)
                _p.Check(Constructs.CallOnOverflowRemoved2023, "the CALL statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>END-ACCEPT (ISO §14.9.1, a 2002 introduction). Mirrors the binder's <c>AcceptHasTerminator</c>
        /// token scan EXACTLY. NOTE — a grammar gap: <c>acceptStatement</c> has no <c>END_ACCEPT</c> alternative, so
        /// the scan finds nothing and this gate fires ZERO times today (as the bound-arm's
        /// BoundAccept.HasEndTerminator did); kept in the correct home so it lights up if the grammar gains the
        /// terminator.</summary>
        public override object? VisitAcceptStatement(CobolParserCore.AcceptStatementContext ctx)
        {
            for (int i = 0; i < ctx.ChildCount; i++)
                if (ctx.GetChild(i) is Antlr4.Runtime.Tree.ITerminalNode { Symbol.Type: CobolLexer.END_ACCEPT })
                {
                    _p.Check(Constructs.EndAccept2002, "the ACCEPT statement");
                    break;
                }
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> is nested inside a METHOD definition (the parse-tree analogue of
        /// the binder's <c>InMethod</c> flag) — the GOBACK-RETURNING exclusion.</summary>
        private static bool InMethodDefinition(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.MethodDefinitionContext) return true;
            return false;
        }

        // ── Step 14h.4a: the clean expression/phrase gates (one unambiguous detection point each) ─────────────

        /// <summary>The logical XOR / EXCLUSIVE-OR operator (ISO §8.8.4.9) — a COBOL-2023 introduction. A
        /// <c>ChildCount &gt; 1</c> means an XOR/EXCLUSIVE_OR terminal was matched between two
        /// <c>logicalAndExpression</c> operands (a bare below-2023 <c>logicalAndExpression</c> is one child,
        /// untouched — the same guard BindXorSequence used).</summary>
        public override object? VisitLogicalXorExpression(CobolParserCore.LogicalXorExpressionContext ctx)
        {
            if (ctx.ChildCount > 1)
                _p.Check(Constructs.LogicalXorOperator2023, "the logical XOR operator");
            return base.VisitChildren(ctx);
        }

        /// <summary>The target-less <c>GO TO.</c> (ISO §14.9.17; the ANSI-85 alterable GO TO, REMOVED by 2002) — no
        /// procedure-name AND no DEPENDING operand, exactly the BindGoTo→AlterBindBareGoTo condition (a
        /// <c>GO TO DEPENDING</c> with no names is malformed, not bare, and takes a different path).</summary>
        public override object? VisitGoToStatement(CobolParserCore.GoToStatementContext ctx)
        {
            if (ctx.procedureName().Length == 0 && ctx.dataReference() is null)
                _p.Check(Constructs.BareGotoRemoved2002, "the GO TO statement");
            return base.VisitChildren(ctx);
        }

        /// <summary>ROUNDED MODE IS (ISO §14.7.4) — a COBOL-2014 introduction (the explicit MODE phrase + the 8-mode
        /// set); a bare ROUNDED is version-invariant. One <c>roundedPhrase</c> per receiver → one node here,
        /// matching the per-receiver RoundingOf gate.</summary>
        public override object? VisitRoundedPhrase(CobolParserCore.RoundedPhraseContext ctx)
        {
            if (ctx.roundingModeName() is not null)
                _p.Check(Constructs.RoundedModeIs2014, "the ROUNDED MODE IS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The RETRY phrase (ISO §14.7.9) on OPEN/READ/WRITE/REWRITE/DELETE/DELETE-FILE — a COBOL-2002
        /// introduction. ONE grammar rule at six sites → one override; the phrase's very EXISTENCE is already
        /// governed by the grammar (the OPEN site's <c>{is2002()||retryPhraseAhead()}?</c> forward-detect only
        /// enters the phrase on an unambiguous numeric tail), so presence IS the gate — matching the former
        /// GateRetryIntro (once per phrase).</summary>
        public override object? VisitRetryPhrase(CobolParserCore.RetryPhraseContext ctx)
        {
            _p.Check(Constructs.RetryPhrase2002, "the RETRY phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The verb record-lock phrase (WITH LOCK / WITH NO LOCK / IGNORING LOCK) on READ/WRITE/REWRITE
        /// (ISO §14.9.30/.51/.35) — a COBOL-2002 introduction. The where-string names the verb from the parent
        /// statement type (matching CheckRecordLockPhrase's <c>verb</c> argument). DISTINCT from the READ …
        /// ADVANCING ON LOCK occurrence (same constructId, a different where-string) that STAYS bound-arm — both
        /// can fire on one READ; they are not merged.</summary>
        public override object? VisitRecordLockPhrase(CobolParserCore.RecordLockPhraseContext ctx)
        {
            string verb = ctx.Parent switch
            {
                CobolParserCore.WriteStatementContext => "WRITE",
                CobolParserCore.RewriteStatementContext => "REWRITE",
                _ => "READ",   // readStatement — the sequential (StatementBinder) and keyed (KeyedIo) READ both route here
            };
            _p.Check(Constructs.RecordLockPhrase2002, $"a record-lock phrase on {verb}");
            return base.VisitChildren(ctx);
        }

        // ── Step 14h.4b: the boolean-operator + national/boolean LITERAL gates (the delicate cases) ────────────
        // (1) The boolean OPERATORS gate is detected at the primaryCondition / computeStatement ALTITUDE with a
        //     whole-subtree HasBoolOp scan — never per booleanExpression node: the tiers nest via parentheses /
        //     the relation form, so a per-node gate would over-count. (2) The national/boolean LITERAL gates fire
        //     for a PROCEDURE-DIVISION statement operand only (a StatementContext ancestor); a data-division VALUE
        //     literal is left to the data/PIC gate (its item's national/boolean USAGE, Step 14g) — firing here too
        //     would double the below-2002 diagnostic.

        /// <summary>The boolean operators B-AND/B-OR/B-XOR/B-NOT (ISO §8.7.2) in a CONDITION — a COBOL-2002
        /// introduction. Fires ONCE per primaryCondition that carries a B-operator anywhere in its
        /// booleanExpression operand(s) (matching BindPrimaryBoolean's <c>be.Any(HasBoolOp)</c>); a B-op-free
        /// comparison uses the untouched shared comparison rule and never enters here.</summary>
        public override object? VisitPrimaryCondition(CobolParserCore.PrimaryConditionContext ctx)
        {
            if (ctx.booleanExpression().Any(HasBoolOp))
                _p.Check(Constructs.BooleanOperators2002, "the boolean operators (B-AND/B-OR/B-XOR/B-NOT)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The boolean operators in a COMPUTE Format 2 RHS (ISO §14.9.8) — the second BooleanOperators2002
        /// site (matching BindComputeBoolean). The F1 arithmetic alternative has no <c>booleanExpression</c>.</summary>
        public override object? VisitComputeStatement(CobolParserCore.ComputeStatementContext ctx)
        {
            if (ctx.booleanExpression() is { } be && HasBoolOp(be))
                _p.Check(Constructs.BooleanOperators2002, "the boolean operators (B-AND/B-OR/B-XOR/B-NOT)");
            return base.VisitChildren(ctx);
        }

        /// <summary>National (<c>N"…"</c>) and boolean (<c>B"…"</c>) literals as a PROCEDURE-DIVISION statement
        /// operand — COBOL-2002 introductions (ISO §8.3.3.5 / §8.3.3.4). Scoped to a StatementContext ancestor so a
        /// DATA-division VALUE literal is left to the data/PIC gate (Step 14g) — firing here too would double the
        /// below-2002 diagnostic. One Check per literal occurrence, matching the binder's NationalLiteralOperand /
        /// BooleanLiteralOperand / boolean-expression-operand paths.</summary>
        public override object? VisitNonNumericLiteral(CobolParserCore.NonNumericLiteralContext ctx)
        {
            bool nat = ctx.NATLIT() is not null;
            if ((nat || ctx.BOOLLIT() is not null) && InStatement(ctx))
                _p.Check(nat ? Constructs.NationalData2002 : Constructs.BooleanData2002,
                    nat ? "national literal N\"…\"" : "boolean literal B\"…\"");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> sits inside a procedure-division statement (a StatementContext
        /// ancestor) — the scope that reaches the binder's literal-operand gates, excluding data-division VALUE.</summary>
        private static bool InStatement(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.StatementContext) return true;
            return false;
        }

        /// <summary>Whether a boolean-expression subtree contains any B-operator terminal — the discriminator
        /// between a genuine boolean expression and a bare operand parsed through the booleanExpression rule.
        /// Mirrors the binder's <c>HasBoolOp</c> (StatementBinder.Boolean.cs), which stays there for its own
        /// channel-routing use (a pure predicate, duplicated across the two layers it serves).</summary>
        private static bool HasBoolOp(Antlr4.Runtime.Tree.IParseTree t)
        {
            if (t is Antlr4.Runtime.Tree.ITerminalNode term)
                return term.Symbol.Type is CobolLexer.B_AND or CobolLexer.B_OR or CobolLexer.B_XOR or CobolLexer.B_NOT;
            for (int i = 0; i < t.ChildCount; i++)
                if (HasBoolOp(t.GetChild(i))) return true;
            return false;
        }

        // Which cobolWord token TYPES the funnel checks POSITION-BLIND (P2.4, refined — DEVLOG 585): IDENTIFIER
        // occurrences are ALWAYS genuine words (the lexer didn't tokenize them), and they carry the whole
        // newly-reserved payload (the Annex-E 2023 additions lex as IDENTIFIER). The six EC-band tokens are ALSO
        // checked everywhere — §8.9-reserved since 2002 (except the context-sensitive STATEMENT, inert in the
        // table) and their keyword uses parse through dedicated statement rules, never a name slot. The REMAINING
        // allowlisted tokens (the screen/report/OPTIONS bands: COL, COLUMN, AUTO, …) are checked POSITION-AWARE
        // instead (the P2.8 W2 adversarial review of the former blanket exclusion): the permissive grammar can
        // bind their KEYWORD occurrences into optional entry-name slots (RW104A binds the report COLUMN clause's
        // keyword into the report-group entry-name slot), so a position-blind check false-rejects conforming
        // CCVS-85 programs. They reject only in a position provably a user-word use — IsProvableUserWordPosition.
        private static readonly HashSet<int> CheckedTokenTypes =
        [
            CobolLexer.IDENTIFIER,
            CobolLexer.RAISE, CobolLexer.RAISING, CobolLexer.RESUME,
            CobolLexer.CONDITION, CobolLexer.EC, CobolLexer.STATEMENT,
            // The 2023 logical-operator words (the W3 XOR regating, VCR rows 32/41): admitted to cobolWord as
            // user words below 2023; their keyword occurrences parse through the operator alternative, never a
            // name slot — so, like the EC band, they are position-safe to check everywhere.
            CobolLexer.XOR, CobolLexer.EXCLUSIVE_OR,
            // The 2002 boolean operators (ISO §8.7.2): user words at 85, funnel-0901'd at ≥2002. Their keyword
            // occurrences parse only through the booleanExpression tiers, never a name slot — position-safe.
            CobolLexer.B_AND, CobolLexer.B_OR, CobolLexer.B_XOR, CobolLexer.B_NOT,
            // The 2002 file-sharing §8.9-reserved words (SHARING/RETRY/UNLOCK): user words at 85, 0901'd ≥2002.
            // Keyword occurrences parse only through the gated sharing/lock rules, never a name slot.
            CobolLexer.SHARING, CobolLexer.RETRY, CobolLexer.UNLOCK,
            // The X3.23-1985 notInGrammar 85-acceptance words (VCR Table 7 rows 7.15–7.18): '85-reserved, user
            // words at later editions per the §8.9 table (RERUN/ENTER free ≥2002, DEBUGGING ≥2014, the rest
            // ≥2023). Their keyword occurrences parse through dedicated rules, never a name slot.
            CobolLexer.FACTORY,   // §11.4 (2002+): keyword slots are factoryParagraph/END FACTORY/FACTORY OF only — position-blind safe (the EC-band argument)
            CobolLexer.OVERRIDE,  // §11.7 (2002+): the METHOD-ID attribute slot is a direct token — position-blind safe
            CobolLexer.GET, CobolLexer.PROPERTY, CobolLexer.INTERFACE,   // §11.6/§11.7/§13.18.42 (2002+): keyword slots are direct tokens — position-blind safe. IMPLEMENTS is §8.10 context-sensitive: NEVER here.
            CobolLexer.PROTOTYPE, // §11.5 (2002+): the keyword occurs only in the functionIdParagraph `IS PROTOTYPE` tail — a direct token, never a name slot — position-blind safe (the UDF-3 wave)
            CobolLexer.RERUN, CobolLexer.ENTER, CobolLexer.EVERY, CobolLexer.CLOCK_UNITS,
            CobolLexer.DEBUGGING, CobolLexer.REFERENCES, CobolLexer.PROCEDURES,
        ];

        /// <summary>
        /// The §8.9 reserved-word funnel (P2.4): every user-defined word reaches the tree through the
        /// <c>cobolWord</c> rule — IDENTIFIER plus the allowlisted context-keyword tokens — so ONE text-based
        /// check here covers 2023-new words that lex as IDENTIFIER (COMMIT, FINALLY, …) AND the EC-band tokens
        /// the 2023 edition reserves (RAISE/RAISING/RESUME/CONDITION/EC). The grammar stays a permissive superset
        /// ("legal user word at every edition"); this arm enforces per edition — position-blind for
        /// <see cref="CheckedTokenTypes"/>, position-AWARE for every other allowlisted context keyword
        /// (<see cref="IsProvableUserWordPosition"/> — the P2.8 W2 subset). Only high-confidence rows reject
        /// (<see cref="ReservedWordSet.RejectsAt"/> — the conservative policy); severity routes through
        /// <see cref="EditionSeverityPolicy"/> (error strict / warning permissive, the 0901 band row).
        /// </summary>
        public override object? VisitCobolWord(CobolParserCore.CobolWordContext ctx)
        {
            if (!CheckedTokenTypes.Contains(ctx.Start.Type) && !IsProvableUserWordPosition(ctx))
                return base.VisitChildren(ctx);
            string word = ctx.Start.Text.ToUpperInvariant();
            // EXCEPTION-OBJECT inside an objectReference operand (SET sender, RAISE operand) is a reference to
            // the PREDEFINED register (§8.4.3.6 — the EC-OO wave), not a user-defined word: the reservation
            // (§8.9, 2002+) is exactly what makes the reference unambiguous. Any other position (declarations,
            // non-object operands) keeps the 0901 funnel.
            if (word == "EXCEPTION-OBJECT")
                for (Antlr4.Runtime.RuleContext? a = ctx.Parent, guard = null; a is not null && a != guard; a = a.Parent)
                {
                    // SetToValueStatement: `SET x TO EXCEPTION-OBJECT` PARSES as the Format-1 value shape
                    // (alternative order) and re-routes to Format 5 at bind — same register reference.
                    if (a is CobolParserCore.ObjectReferenceContext or CobolParserCore.SetToValueStatementContext)
                        return base.VisitChildren(ctx);
                    if (a is CobolParserCore.StatementContext) break;   // far enough — not an object operand
                }
            if (_reservedWords.RejectsAt(word, _p._edition.Year) && (_flaggedWords ??= []).Add(word))
            {
                // Under WITH DEBUGGING MODE a DEBUG-* occurrence is the X3.23-1985 REGISTER (DEBUG-ITEM family),
                // not a user-defined word — a legal '85 reference to a facility this compiler defers (VCR Table 7
                // row 7.17; the switch-ABSENT case never gets here — comment treatment skips the section body).
                // Diagnose the truth (0899 not-implemented) instead of a false §8.9 violation.
                if (_debuggingModeDeclared && word.StartsWith("DEBUG-", StringComparison.Ordinal))
                    _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.DebugRegisterFacility.Code,
                        EditionSeverity.Error, DiagnosticCatalog.DebugRegisterFacility.Id,
                        $"the X3.23-1985 debug register '{word}' is recognized, but the '85 debug facility "
                        + "(DEBUG-ITEM registers, debugging-section invocation) is not implemented — deferred "
                        + "with the golden-less DB series (VCR Table 7 row 7.17)", "",
                        DiagnosticCatalog.DebugRegisterFacility.IsoSection));
                else
                    // The §8.9 reserved-word removal-of-spelling severity routes through the ONE policy (P3 step 2:
                    // was EditionContext.Removed; now EditionSeverityPolicy over the structured sink — byte-identical).
                    _p._sink.Report(new EditionDiagnostic(EditionCodes.ReservedWord,
                        EditionSeverityPolicy.For(ConstructAvailability.Removed, _p._edition), "edition-reserved-word",
                        $"'{word}' is a reserved word in COBOL-{_p._edition.Year} and cannot be used as a "
                        + "user-defined word (ISO §8.9)", "", "ISO §8.9"));
            }
            return base.VisitChildren(ctx);
        }
    }
}
