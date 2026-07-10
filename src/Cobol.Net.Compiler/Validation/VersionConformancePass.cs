// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;        // Place subtypes (RefModPlace, …), PicCategory, Usage
using CobolNet.Binding.Bound;
using CobolNet.CodeGen;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog / EditionDiagnostic / EditionCodes / EditionSeverity(Policy) — the §8.9 funnel
using CobolNet.Frontend.Generated;     // CobolParserCore / CobolLexer / CobolParserCoreBaseVisitor — the parse-tree arm
using CobolNet.Runtime;   // CobolPassMode (CALL argument passing mode)

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
/// deleted as its twin lands. <b>Migration state (Step 14h in progress):</b> 14h.1 stands up the parse-arm
/// (EditionValidator absorbed) and moves the driver's fail-fast post-bind; the SYNTACTIC introduction/removal/
/// phrase STATEMENT gates (UNLOCK/FREE/ALTER/DELETE-FILE/SET-ADDRESS; OPEN-SHARING/GOBACK-RETURNING/CALL-BY-VALUE/
/// CALL-ON-OVERFLOW/STOP-STATUS/END-ACCEPT) are STILL in the bound-arm here and migrate to the parse-arm in
/// 14h.2–14h.4. The one principled exception is the UDF-invocation gate (an intrinsic FUNCTION and a
/// user-function call are syntactically identical — only the repository-resolved name set separates them), which
/// stays BIND-TIME (<c>StatementBinder.Udf.cs</c>) where it already fires on recognition before operand binding.
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
        //    pointer-category conditioned gates) that need a resolved bound fact, never mere presence. ──
        foreach (var unit in group.Units)
            pass.WalkProgram(unit.Bound);
        foreach (var cls in group.Classes)
        {
            // A class body's OBJECT and FACTORY halves are two bound programs over one class; both carry statements
            // (each METHOD-ID is a pc slice of the ONE dispatch space, so walking Paragraphs covers every method).
            pass.WalkProgram(cls.Bound);
            pass.WalkProgram(cls.FactoryBound);
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
            case BoundUnlock:
                Check(Constructs.UnlockStatement2002, "the UNLOCK statement"); break;
            // NOTE: ALLOCATE (Allocate2002) is an INTRODUCTION gate that fires on the construct's RECOGNITION — it
            // must fire even when binding errors before a BoundAllocate is produced (a below-2002 ALLOCATE with a
            // bad RETURNING; EditionGateDiagnosticTests.Allocate_At85). A bound-arm node gate loses it on that error
            // path, so it stays BIND-TIME (StatementBinder.Ptr.cs) until Step 14h moves ALL introduction gates to
            // the presence-based post-bind PARSE-arm. Same for UDF (UserFunctionInvocation2002) below.
            case BoundFree:
                Check(Constructs.Free2002, "the FREE statement"); break;
            case BoundSetObjectRef:
                Check(Constructs.SetObjectReference2002, "the SET … TO object-reference statement (Format 5)"); break;
            case BoundSetPointerUpDown:
                Check(Constructs.PointerArithmetic2002, "SET pointer UP/DOWN BY (ISO §14.9.39 Format 10)"); break;
            case BoundAlter:
                Check(Constructs.AlterRemoved2002, "the ALTER statement"); break;
            case BoundKeyedDeleteFile:
                Check(Constructs.DeleteFile2023, "the DELETE FILE statement"); break;
            // SET ADDRESS OF (§14.9.39 Format 7) has two bound shapes from the one PtrBindSetAddress site: the
            // receiver form (SET ADDRESS OF x TO p) is a distinctive BoundSetAddressOfBased; the sender form
            // (SET p TO ADDRESS OF x) is a BoundSetPointer carrying an ADDRESS-OF source. Both gate identically.
            case BoundSetAddressOfBased:
            case BoundSetPointer { Address: not null }:
                Check(Constructs.SetAddress2002, "SET ADDRESS OF (ISO §14.9.39 Format 7)"); break;

            // ── Gates conditioned on a resolved node ATTRIBUTE the binder already recorded ──────────────────────
            case BoundOpen { SharingOverride: not null }:
                Check(Constructs.FileSharingClause2002, "the OPEN SHARING phrase"); break;
            case BoundGoback { ReturningSource: not null }:
                Check(Constructs.GobackReturning2002, "GOBACK … RETURNING"); break;
            case BoundCallProgram cp:
                // (UserFunctionInvocation2002 for a UDF reference is an INTRODUCTION gate that must fire on
                // RECOGNITION even when the function is undefined — it stays BIND-TIME until Step 14h; see the
                // ALLOCATE note above.)
                // CALL … BY VALUE (§14.9.4). The binder fired once per explicit BY VALUE argument; the bound node
                // keeps each argument's pass mode, so gate once when the CALL uses value passing (the argument
                // list Any-check — the tested single-argument case is diagnostically identical).
                if (cp.Args.Any(a => a.Mode == CobolPassMode.Value))
                    Check(Constructs.CallByValue2002, "the CALL … BY VALUE phrase");
                // ON OVERFLOW spelling (the COBOL-74 synonym for ON EXCEPTION) — REMOVED at ISO 2023; gate AFTER
                // BY VALUE (the binder's order: args bind before the exception phrases).
                if (cp.UsedOverflowSpelling)
                    Check(Constructs.CallOnOverflowRemoved2023, "the CALL statement");
                break;
            case BoundStop { HasStatusPhrase: true }:
                Check(Constructs.StopRunStatus2002, "the STOP RUN … WITH NORMAL/ERROR STATUS phrase"); break;
            case BoundInvoke or BoundInvokeUniversal:
                Check(Constructs.Invoke2002, "the INVOKE statement"); break;
            case BoundAccept { HasEndTerminator: true }:
                Check(Constructs.EndAccept2002, "the ACCEPT statement"); break;
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
