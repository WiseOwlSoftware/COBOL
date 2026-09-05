// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;        // Place subtypes (RefModPlace, …), PicCategory, Usage
using CobolNet.Common;         // CobolLiteral — the ONE literal decoder / §8.3.3 hex-grouping rule (R03)
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;  // BoundUnit / OoClassUnit — the bound model (P6)
using CobolNet.Binding.Passes; // GroupBindContext — this pass is the manifest's NAMED terminal pass (P6 Step 4)
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog / EditionDiagnostic / EditionCodes / EditionSeverity(Policy) — the §8.9 funnel
using CobolNet.Frontend.Common;        // CobolWordRule — the ONE §8.3.2.1 word-length ceiling (shared with the directive stages)
using CobolNet.Frontend.Generated;     // CobolParserCore / CobolLexer / CobolParserCoreBaseVisitor — the parse-tree arm
using CobolNet.Frontend.Parsing;       // CobolKeywordTokens — the reverse vocab map (>>COBOL-WORDS SR3/SR4 category)

using CobolNet.Compiler.Oo;

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
/// method scope). <b>14g.3–14g.5 (DONE)</b> completed the DATA/PIC/OO migration: OO class/interface + OCCURS DYNAMIC
/// (14g.3, parse-arm); file SHARING/LOCK-MODE + SPECIAL-NAMES FOR + PD RETURNING/RAISING (14g.4, parse-arm — the recon's
/// bound-arm SHARING/LOCK-MODE reclassified for the same drop-proof reason); FUNCTION-PROTOTYPE (14g.5, bound-arm over
/// <c>BoundUnit.IsPrototype</c>) + REPOSITORY CLASS/INTERFACE/PROPERTY (14g.5, parse-arm) + the external-float /
/// national-edited PICTURE skeletons (14g.5, bound-arm via <c>PicInfo.SkeletonGate</c> — the recovered category erases
/// the identity, so PicInfo's own exact detection carries the 0900 forward). The one principled exception is the
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
    /// <paramref name="sink"/>. The manifest's NAMED TERMINAL pass (P6 Step 4 — <c>BindPipeline.GroupTail</c>,
    /// Requires <c>StorageComputed</c>, Produces <c>EditionConformanceChecked</c>): it runs INSIDE
    /// <c>BinderDriver.Bind</c> after every other pass, so the Bind result already carries every edition
    /// diagnostic and the driver HALTs before emit if the sink then carries errors (rearch exit criterion 9 — no
    /// codegen on an errored tree; a <c>CheckOnly</c> verdict is complete without emit).</summary>
    public static void Run(GroupBindContext group, EditionInfo edition, IDiagnosticSink sink)
    {
        var pass = new VersionConformancePass(edition, sink);
        // The per-compilation-group EFFECTIVE reserved-word set (ISO §7.3.10 D9 seam): the generated §8.9 table
        // composed with the >>COBOL-WORDS overlay (RESERVE adds, UNDEFINE/SUBSTITUTE remove). Empty map ⇒ Default.
        var reservedWords = ReservedWordSet.Compose(group.Session.CobolWords);
        // >>COBOL-WORDS SR3/SR4 category validation (§7.3.10.3) — needs all three registries (reserved via the §8.9
        // table + the lexer vocab, intrinsic via IntrinsicCatalog); the frontend validated SR1/SR2/SR5 already.
        ValidateCobolWords(group.Session.CobolWords, edition, sink);
        // ── PARSE-tree arm (Step 14h): ONE walk of the raw compilation unit, firing every SYNTACTIC
        //    introduction/removal/phrase gate + the §8.9 reserved-word funnel on the construct's RECOGNITION
        //    (absorbs the former EditionValidator). Recognition-based so a below-edition construct that ALSO
        //    has a semantic error still names its edition — the bound node it would have produced may be
        //    dropped (BoundUnsupported/BoundNop), but its parse node is always present (DEVLOG 724). ──
        new ParseArm(pass, reservedWords).VisitPositioned(group.Tree);
        // ── BOUND-tree arm: the genuinely-SEMANTIC gates (MOVE figurative-category; the file-org / USAGE /
        //    pointer-category conditioned STATEMENT gates) + the DATA-attribute gates (Step 14g — every
        //    source-declared DataItem's resolved USAGE / PICTURE category), which need a resolved bound fact. ──
        foreach (var unit in group.Units)
        {
            // FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2) — a COBOL-2002 introduction. Bound-arm: BoundUnit.IsPrototype
            // is set at unit creation and every unit (top-level, nested, function) is a BoundUnit in group.Units, so it
            // is scope-exact + drop-proof, with the former MakeUnit Check's constant where-string (Step 14g.5).
            if (unit.IsPrototype)
                pass.Check(Constructs.FunctionPrototype2002, "a FUNCTION-ID … IS PROTOTYPE (function prototype)");
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

    /// <summary>&gt;&gt;COBOL-WORDS SR3/SR4 category validation (ISO §7.3.10.3): the EXISTING word (literal-1/3/4)
    /// must be a reserved word, context-sensitive word, or intrinsic-function name (SR3); the NEW word
    /// (literal-2/5/6) must be none of those (SR4 — the §8.3.2.2 well-formedness is the frontend half). Reserved /
    /// context membership comes from the §8.9 table + the lexer vocabulary (<see cref="CobolKeywordTokens"/>);
    /// intrinsic from <see cref="IntrinsicCatalog"/>. Both checks err AWAY from rejecting legal source (the
    /// no-false-reject principle): SR3 accepts on ANY membership signal, SR4 rejects only on a CERTAIN one.</summary>
    private static void ValidateCobolWords(CobolWordsMap map, EditionInfo edition, IDiagnosticSink sink)
    {
        if (map.IsEmpty) return;
        foreach (var op in map.Ops)
        {
            if (op.Existing is { } e && !IsExistingWordCategory(e))
                ReportCobolWordsInvalid(sink, $"the existing word '{e}' is not a reserved word, "
                    + "context-sensitive word, or intrinsic-function name (ISO §7.3.10.3 SR3)");
            if (op.New is { } n && IsReservedCategory(n, edition))
                ReportCobolWordsInvalid(sink, $"the new word '{n}' is a reserved / context-sensitive / "
                    + "intrinsic-function word and cannot be a user-defined word (ISO §7.3.10.3 SR4)");
        }
    }

    /// <summary>SR3 membership — broad (accept on any signal): a lexer keyword/context token, an intrinsic-function
    /// name, or ANY §8.9 table entry.</summary>
    private static bool IsExistingWordCategory(string w) =>
        CobolKeywordTokens.IsKeyword(w) || IntrinsicCatalog.TryGet(w, out _) || ReservedWords.Find(w) is not null;

    /// <summary>SR4 membership — narrow (reject only when certain): a lexer keyword/context token, an
    /// intrinsic-function name, or a HIGH-CONFIDENCE reserved-at-edition table entry.</summary>
    private static bool IsReservedCategory(string w, EditionInfo edition) =>
        CobolKeywordTokens.IsKeyword(w) || IntrinsicCatalog.TryGet(w, out _)
        || (ReservedWords.Find(w) is { Confidence: "high" } r && r.IsReservedAt(edition.Year));

    private static void ReportCobolWordsInvalid(IDiagnosticSink sink, string message) =>
        sink.Report(new EditionDiagnostic(DiagnosticCatalog.CobolWordsDirectiveInvalid.Code, EditionSeverity.Error,
            "cobol-words-directive-invalid", message, "", "ISO §7.3.10.3"));

    private void WalkProgram(BoundProgram? prog)
    {
        if (prog is null) return;
        foreach (var para in prog.Paragraphs)
            foreach (var stmt in para.Statements)
                WalkStatement(stmt);
        GateMergeInSortMergeProc(prog);
    }

    /// <summary>VCR 27 (ISO §14.9.24; Annex E.2 item 20): at COBOL-2023 a MERGE statement is PROHIBITED in the
    /// output procedure of another MERGE or the input/output procedure of a file-format SORT (the prior standard
    /// allowed it with conflicting rules; SORT already disallowed it). A bind-time cross-pass over the paragraph-pc
    /// ranges — a paragraph's pc IS its index in <see cref="BoundProgram.Paragraphs"/>, the same pc space as the
    /// SORT/MERGE procedure ranges (<c>SortRange</c> → the ProcedureTable). Below 2023 the runtime
    /// EC-SORT-MERGE-ACTIVE seam is the (checking-off) net, so this fires only at ≥2023.</summary>
    private void GateMergeInSortMergeProc(BoundProgram prog)
    {
        if (_edition.Year < 2023) return;

        // Pass A — the prohibited paragraph-pc ranges: every file-format SORT's input/output procedure + every
        // MERGE's output procedure (a SORT/MERGE with only USING/GIVING files contributes no procedure range).
        var prohibited = new List<(int Start, int End)>();
        void Collect(BoundStatement s)
        {
            if (s is BoundSort { InputProcedure: { } ip }) prohibited.Add(ip);
            if (s is BoundSort { OutputProcedure: { } op }) prohibited.Add(op);
            if (s is BoundMerge { OutputProcedure: { } mop }) prohibited.Add(mop);
            foreach (var c in s.StatementChildren()) Collect(c);
        }
        foreach (var para in prog.Paragraphs)
            foreach (var stmt in para.Statements) Collect(stmt);
        if (prohibited.Count == 0) return;

        // Pass B — flag every MERGE whose ENCLOSING paragraph pc falls within a prohibited range (a MERGE nested in
        // an IF/inline-PERFORM is still in that paragraph). The owning MERGE's own paragraph is never in its own
        // output-proc range (a distinct named procedure), so a MERGE never false-flags itself.
        // The 2023 prohibition is a REMOVAL of a prior-edition capability, so its severity follows the removal
        // policy — an Error under strict, downgraded to a Warning (compile succeeds) under --permissive migration
        // mode (EditionSeverityPolicy), matching the version matrix's RemovedConstruct_CompilesPermissive contract.
        var severity = EditionSeverityPolicy.For(ConstructAvailability.Removed, _edition);
        void Flag(BoundStatement s, int paraPc)
        {
            if (s is BoundMerge m && prohibited.Any(r => paraPc >= r.Start && paraPc <= r.End))
                _sink.Report(new EditionDiagnostic("COBOLNET1572", severity, "merge-in-sort-merge-proc",
                    $"MERGE '{m.File.CobolName}' is prohibited in the output procedure of another MERGE or the input "
                    + "or output procedure of a file SORT (ISO §14.9.24; COBOL-2023, Annex E.2 item 20)",
                    $"MERGE '{m.File.CobolName}'", "ISO §14.9.24; Annex E.2 item 20"));
            // kb/Work PB137 — the batch-8 finding verbatim: this pass implemented exactly the SR2 ban with a
            // MERGE-only predicate; COMMIT (§14.9.7.3 SR2) and ROLLBACK (§14.9.36.3 SR2) are its siblings,
            // reachable only now that the bind produces an identity-bearing node.
            if (s is BoundCommitRollback cr && prohibited.Any(r => paraPc >= r.Start && paraPc <= r.End))
                _sink.Report(new EditionDiagnostic(DiagnosticCatalog.CommitRollbackContext.Code,
                    EditionSeverity.Error, "commit-rollback-context",
                    $"{(cr.IsCommit ? "COMMIT" : "ROLLBACK")} shall not be specified in the input or output "
                    + $"procedure of a MERGE or file SORT statement (ISO {(cr.IsCommit ? "§14.9.7.3" : "§14.9.36.3")} SR2)",
                    cr.IsCommit ? "COMMIT" : "ROLLBACK", "ISO §14.9.7.3 SR2 / §14.9.36.3 SR2"));
            foreach (var c in s.StatementChildren()) Flag(c, paraPc);
        }
        for (int i = 0; i < prog.Paragraphs.Count; i++)
            foreach (var stmt in prog.Paragraphs[i].Statements) Flag(stmt, i);
    }

    private void WalkStatement(BoundStatement s)
    {
        GateStatement(s);
        Recurse(s);
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
            case BoundSetSize:
                // SET [SIZE OF] dynamic-length-item TO n (§14.9.39 Format 16) — a 2023 introduction. Semantic (the
                // target must be dynamic-length), so it stays a bound-tree gate — one arm covers the explicit SIZE
                // OF form and the bare re-routed form (both bind to BoundSetSize).
                Check(Constructs.SetDynLengthSize2023, "the SET [SIZE OF] … TO length statement (dynamic-length item, Format 16)"); break;

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
            case BoundRead sr:
                // The sequential-organization READ binds the same §14.9.30 GR22 phrase (P10 Step 8 — it was
                // previously dropped at bind, so this arm is the phrase's first sequential-leg gate).
                if (sr.AdvancingOnLock)
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

    // ── The complete nested-statement traversal (PHASE-07 Step 6h) ────────────────────────────────────────────
    // Descends EVERY container via the generated BoundStatementTree.StatementChildren — the ONE drift-proof source
    // of the statement-tree shape — so a gate nested inside IF/EVALUATE/PERFORM/SEARCH/an ON-phrase escapes nothing,
    // and a NEW container node is covered automatically (the former hand-listed switch was synced-by-prose against
    // the binder's own traversals). Leaves yield no children.
    private void Recurse(BoundStatement s)
    {
        foreach (var child in s.StatementChildren())
            WalkStatement(child);
    }

    // ── Step 14g: the DATA-attribute (USAGE / PICTURE-category) edition gates ────────────────────────────────
    // Genuinely SEMANTIC — identity is a RESOLVED DataItem attribute (its USAGE / PICTURE category), never parse
    // presence, so a bound-arm walk is the correct home. Fires ONCE per SOURCE declaration: DataBinder's
    // ConformanceForest excludes the post-bind TYPE-clones + compiler temps (which the binder never re-analyzed
    // and so never gated), reproducing the former per-entry PictureAnalyzer.ParseUsage/Analyze gates byte-for-byte.
    private void GateData(DataBinder data)
    {
        foreach (var item in data.ConformanceForest())
            GateDataItem(item, $"data item '{item.CobolName ?? "FILLER"}'");
        // Report printable items are SYNTHETIC DataItems in the RD model (off the forest) — their national/boolean
        // PICTURE gates fired in Analyze with the report where-string (DataBinder.Reports.cs); reproduce that here.
        foreach (var report in data.Reports)
        {
            foreach (var grp in report.Groups)
                foreach (var line in grp.Lines)
                    foreach (var field in line.Fields)
                        GateDataItem(field.PrintItem,
                            $"RD '{report.Name}' printable item '{field.PrintItem.CobolName ?? "FILLER"}'");
            // SUM counters (§13.18.54) analyze their PICTURE for the counter scale (GR1) in a DISTINCT Analyze call off
            // both the forest AND the printable-item walk (DataBinder.Reports.cs:BindSumClause), so an external-float /
            // national-edited SUM-counter picture carries its 0900 here on ReportSumModel.SkeletonGate (DEVLOG 740 —
            // the 14g.5 review found the former inline gate was dropped). Fires once per SUM counter — so a NON-printable
            // SUM gets its one 0900, and a PRINTABLE SUM (also a print item, gated above) gets both, exactly as the
            // former two Analyze sites did.
            foreach (var sum in report.Sums)
                if (sum.SkeletonGate is { } sumSkeletonId) Check(sumSkeletonId, sum.SkeletonWhere);
        }
    }

    /// <summary>Gate one resolved DataItem's USAGE / PICTURE-category edition attribute. At most ONE gate fires
    /// (the categories are mutually exclusive).</summary>
    private void GateDataItem(DataItem item, string where)
    {
        if (UsageConstructId(item) is { } id) Check(id, where);
        if (item.Pic is { } pic && PictureConstructId(pic) is { } picId) Check(picId, where);
    }

    /// <summary>The 2002-introduction gate a PICTURE's SHAPE carries (or null when version-invariant) — the ONE
    /// function for every Analyze site: the forest / report printable items (<see cref="GateDataItem"/>) and the
    /// report SUM-counter scale Analyze (<c>ReportSumModel.SkeletonGate</c>, DataBinder.Reports.cs — a distinct call
    /// whose PicInfo is otherwise discarded, DEVLOG 740). The floating-point numeric-edited form (symbol E — LIVE,
    /// data-model design D21 / kb/Work PB66) keys on <see cref="PicInfo.IsFloatEdited"/>; the recognized-but-
    /// unimplemented national-edited skeleton on <see cref="PicInfo.SkeletonGate"/> (its category was RECOVERED to
    /// Alphanumeric, so no category key can see it — Step 14g.5).</summary>
    internal static string? PictureConstructId(PicInfo pic) =>
        pic.IsFloatEdited ? Constructs.PicExternalFloat2002 : pic.SkeletonGate;

    /// <summary>The 2002-introduction USAGE / PICTURE-category of a resolved item, or null when version-invariant.
    /// Keyed on the resolved <c>(OwnUsage, Pic.Category, Pic.Usage)</c>: <see cref="DataItem.OwnUsage"/> is mandatory
    /// because a group-header USAGE sheds <c>Pic</c> to null (<c>DataBinder.ResolveIndexItems</c>), leaving only the
    /// own keyword; the <c>Pic.Usage</c> member (never <c>IsFloat</c>/<c>ClrType</c>) carries the identity for the
    /// picture-less usages — <c>FloatLong</c>/<c>FloatExtended</c> share a <c>double</c> ClrType with COMP-2.</summary>
    private static string? UsageConstructId(DataItem item)
    {
        var cat = item.Pic?.Category;
        var pu = item.Pic?.Usage;
        var ou = item.OwnUsage;
        return
            cat is PicCategory.National || ou is Usage.National ? Constructs.NationalData2002
            : cat is PicCategory.Boolean || ou is Usage.Bit ? Constructs.BooleanData2002
            : pu is Usage.Pointer || ou is Usage.Pointer ? Constructs.UsagePointer2002
            : cat is PicCategory.ProgramPointer || pu is Usage.ProgramPointer || ou is Usage.ProgramPointer
                ? Constructs.UsageProgramPointer2002
            : ou is Usage.FunctionPointer ? Constructs.UsageFunctionPointer2014   // staged loud at 2014+; Pic is the recovery shape, so OwnUsage carries the identity
            : cat is PicCategory.ObjectReference || pu is Usage.ObjectReference || ou is Usage.ObjectReference
                ? Constructs.UsageObjectReference2002
            : pu is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
              || ou is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? Constructs.UsageBinaryCharFamily2002
            : pu is Usage.FloatShort || ou is Usage.FloatShort ? Constructs.UsageFloatShort2002
            : pu is Usage.FloatLong || ou is Usage.FloatLong ? Constructs.UsageFloatLong2002
            : pu is Usage.FloatExtended || ou is Usage.FloatExtended ? Constructs.UsageFloatExtended2002
            // The 2014 IEEE interchange floats binary32/64 (LIVE) — the 0900 introduction gate below 2014. The
            // FLOAT-BINARY-128 / FLOAT-DECIMAL-16/34 non-support forms have no construct row: their operative
            // diagnostic is COBOLNET1564 (processor-dependent non-support, Annex A.3), fired by ParseUsage at every
            // edition, so a redundant 0900 introduction gate below 2014 would only add noise.
            : pu is Usage.FloatBinary32 || ou is Usage.FloatBinary32 ? Constructs.UsageFloatBinary322014
            : pu is Usage.FloatBinary64 || ou is Usage.FloatBinary64 ? Constructs.UsageFloatBinary642014
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
        // slot is optional, and a cobolWord token that lands in it is always the entry's NAME.
        // ⚠ THE GROUND FOR THAT CHANGED and is re-derived here rather than inherited. It used to read "NO
        // dataDescriptionClause alternative begins with a cobolWord-admitted token", which the declined-A.4.14
        // validationClause arm (Grammar/Core/CobolDeclined.g4) made FALSE: DEFAULT, DESTINATION, PRESENT,
        // VAL-STATUS and VALIDATE-STATUS all lead an alternative AND all ride cobolWord (they must, so
        // `01 DESTINATION PIC X.` keeps drawing the named 0901 rather than a parse error). The CONCLUSION
        // survives on a stronger footing: ANTLR's prediction here is FULL-CONTEXT, so the token lands in a
        // DataNameContext only when the whole entry parses with it as the name — and a program whose entry
        // parses that way IS naming something with the word, which is the §8.3.2.1 violation the 0901 band
        // reports. The mis-parse worry that keeps the report-group and screen entry-name slots UNCHECKED (a
        // greedy `reportGroupName?` swallowing a COLUMN keyword) does not arise: each validation-clause
        // alternative diverges from the name reading within one or two tokens. The witness that it stays true
        // is `conformance:negative/declined-validate-entry-name-still-0901`.
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
    private sealed class ParseArm(VersionConformancePass p, ReservedWordSet reservedWords)
        : CursorFollowingVisitor(p._sink)   // the cursor follows the walk (kb/Work PB82)
    {
        private readonly VersionConformancePass _p = p;
        // The effective reserved-word set for THIS compilation group (P2.4/D9 seam): the generated §8.9 table
        // composed with the 2023 >>COBOL-WORDS overlay (RESERVE/UNDEFINE/SUBSTITUTE); ReservedWordSet.Default when
        // the group has no directive (byte-identical).
        private readonly ReservedWordSet _reservedWords = reservedWords;
        // One COBOLNET0901 per distinct word per compilation (P2.4) — not one per occurrence.
        private HashSet<string>? _flaggedWords;
        // One COBOLNET1567 per distinct over-long word per compilation (the §8.3.2.1 length ceiling).
        private HashSet<string>? _overlongWords;
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
            if (ctx.statusPhrase() is not null)
                _p.Check(Constructs.StopRunStatus2002, "the STOP RUN … WITH NORMAL/ERROR STATUS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>USAGE … WITH NO SIGN (ISO §13.18.60.4 GR11) — a COBOL-2023 addition. Recognition-based on the
        /// parsed noSignPhrase (NO SIGN is a modifier on Usage.Packed, which exists at every edition, so it cannot
        /// be keyed on the resolved Usage enum). The binder separately rejects NO SIGN on a non-Packed usage (1565)
        /// and an 'S' picture (SR31, 1566); this arm owns only the below-2023 introduction gate.
        /// <para>The USAGE clause's float FORMAT phrases (endianness-phrase / encoding-phrase, ISO §13.18.60.2
        /// general format) are a COBOL-2014 addition and gate from the SAME override — a disjoint alternative, the
        /// STOP-statement precedent. Recognition-based for the same reason: the phrase is a MODIFIER, and the
        /// grammar tolerates it after any usageKeyword (the binder narrows — COBOLNET1716/1706/1707), so the
        /// resolved Usage enum cannot key it. A below-2014 program that writes it on a standard float usage draws
        /// this gate AND the usage keyword's own (usage-float-binary32-2014 etc.): two distinct 2014 language
        /// elements were written, and Annex A.3 item 18 names the phrase separately from item 17's usages.</para></summary>
        public override object? VisitUsageClause(CobolParserCore.UsageClauseContext ctx)
        {
            if (ctx.noSignPhrase() is not null)
                _p.Check(Constructs.UsagePackedNoSign2023, "USAGE PACKED-DECIMAL WITH NO SIGN");
            if (ctx.floatFormatPhrase().Length > 0)
                _p.Check(Constructs.UsageFloatFormatPhrase2014,
                    "the USAGE clause's endianness-phrase / encoding-phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>CONTINUE AFTER arithmetic-expression SECONDS (ISO §14.9.9) — the timed-pause phrase is a
        /// COBOL-2023 addition; plain CONTINUE is 1985-continuous. Recognition-based on the phrase's presence (the
        /// arithmeticExpression child ≡ the AFTER … SECONDS phrase); parse-arm so a below-edition occurrence names
        /// its edition even though the phrase would otherwise bind to a no-op (DEVLOG 724).</summary>
        public override object? VisitContinueStatement(CobolParserCore.ContinueStatementContext ctx)
        {
            if (ctx.arithmeticExpression() is not null)
                _p.Check(Constructs.ContinueAfter2023, "the CONTINUE AFTER … SECONDS phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>WRITE … BEFORE ADVANCING … AFTER ADVANCING … (ISO §14.9.51 SR17) — specifying BOTH advancing
        /// phrases on one WRITE is a COBOL-2023 addition. Gate on the CO-OCCURRENCE (two writeAdvancePhrase children),
        /// not on ADVANCING itself — a single BEFORE or AFTER is edition-invariant. Recognition-based (DEVLOG 724).</summary>
        public override object? VisitWriteBeforeAfter(CobolParserCore.WriteBeforeAfterContext ctx)
        {
            if (ctx.writeAdvancePhrase().Length == 2)
                _p.Check(Constructs.WriteBeforeAndAfterAdvancing2023, "the combined WRITE BEFORE AND AFTER ADVANCING phrases");
            return base.VisitChildren(ctx);
        }

        /// <summary>PERFORM … UNTIL EXIT (ISO §14.9.28.4 GR11) — the infinite-loop phrase is a COBOL-2023 addition
        /// (plain UNTIL condition is edition-invariant). Recognition-based on the EXIT alternative of performUntil;
        /// parse-arm so a below-2023 occurrence names its edition even though it drops to a bound control node.</summary>
        public override object? VisitPerformUntil(CobolParserCore.PerformUntilContext ctx)
        {
            if (ctx.EXIT() is not null)
                _p.Check(Constructs.PerformUntilExit2023, "the PERFORM UNTIL EXIT phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format-3 (exception-checking) PERFORM (ISO §14.9.28.2 Format 3) — a COBOL-2023 addition.
        /// Recognition-based on any WHEN / WHEN OTHER / WHEN COMMON / FINALLY phrase or a [WITH] LOCATION head (the
        /// same discriminator the binder uses); disjoint from the UNTIL EXIT gate above. Parse-arm so a below-2023
        /// occurrence names its edition even though it binds to a BoundExceptionPerform.</summary>
        public override object? VisitPerformStatement(CobolParserCore.PerformStatementContext ctx)
        {
            // The ONE Format-3 discriminator, shared with the binder (ControlFlowBinder.IsFormat3), so the
            // COBOLNET0900 gate here and the COBOLNET0899 staged-reject there cannot drift apart.
            if (Binding.Procedure.ControlFlowBinder.IsFormat3(ctx))
                _p.Check(Constructs.PerformExceptionChecking2023, "the Format-3 (exception-checking) PERFORM");
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
            else if (ctx.SECTION() is not null)   // §14.9.14 Format 4 — a COBOL-2002 structured-exit introduction
                _p.Check(Constructs.ExitSection2002, "the EXIT SECTION statement");
            else if (ctx.PARAGRAPH() is not null)   // §14.9.14.2 Format 4 — the COBOL-2002 EXIT PARAGRAPH twin of EXIT SECTION
                _p.Check(Constructs.ExitParagraph2002, "the EXIT PARAGRAPH statement");
            else if (ctx.PERFORM() is not null)     // §14.9.14.2 Format 3 — EXIT PERFORM [CYCLE], a COBOL-2002 structured exit
                _p.Check(Constructs.ExitPerform2002, "the EXIT PERFORM statement");
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
            // The EC-model declarative formats (Exec Step E — folded from the ProcedureTableBuilder inline
            // gates): F4 = USE AFTER {EXCEPTION OBJECT | EO} (§14.9.49.2, the EC-OO selector); F3 = USE AFTER
            // {EXCEPTION CONDITION | EC} (§14.9.49.2). Both are 2002 introductions, recognized from the
            // alternative's own tokens (DEVLOG 724 — recognition-based, never a bound-arm drop).
            if (ctx.OBJECT() is not null || ctx.EO() is not null)
                _p.Check(Constructs.UseAfterExceptionObject2002, "USE AFTER EXCEPTION OBJECT (Format 4)");
            else if (ctx.CONDITION() is not null || ctx.EC() is not null)
                _p.Check(Constructs.UseAfterExceptionCondition2002, "USE AFTER EXCEPTION CONDITION (Format 3)");
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
        // constructId + where-string (byte-identical). The InGatedDataEntry guard reproduces the binder's reach — it
        // fires only inside a non-66/88 dataDescriptionEntry (BindEntries intercepts level-66/88 BEFORE the clause
        // loop, and these four clauses never appear in a report/screen entry), so a clause mis-attached to a
        // condition/renames entry, where the 0900 would be spurious, is skipped.

        /// <summary>The BASED clause (ISO §13.18.5) — a COBOL-2002 introduction (a storage template with an implicit
        /// data-address pointer). The level-01/77 placement SR stays in the binder; this arm only names the edition.</summary>
        public override object? VisitBasedClause(CobolParserCore.BasedClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.BasedClause2002, "the BASED clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The GROUP-USAGE clause (ISO §13.18.29) — a COBOL-2002 introduction (a bit group / national group,
        /// data-model design D20; kb/Work PB79). SR1–SR3 stay in the binder (COBOLNET1653 + the shared §13.18.60.4 GR1
        /// leaf conformance); this arm only names the edition.</summary>
        public override object? VisitGroupUsageClause(CobolParserCore.GroupUsageClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.GroupUsageClause2002, "the GROUP-USAGE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The ANY LENGTH clause (ISO §13.18.2) — a COBOL-2002 introduction (a LINKAGE item whose length
        /// tracks the corresponding argument at runtime, GR1). Parse-arm (recognition) like the BASED gate:
        /// <c>DataItem.IsAnyLength</c> is cleared by the binder on every SR1/SR2/SR3/SR4 shape violation, so a
        /// bound-arm home would drop the 0900 on exactly the declaration-error paths. The §13.18.2 placement SRs
        /// stay bind-time (DataBinder.BindEntry + the unit/method sweeps).</summary>
        public override object? VisitAnyLengthClause(CobolParserCore.AnyLengthClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.AnyLengthClause2002, "the ANY LENGTH clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The DYNAMIC LENGTH clause (ISO §8.5.1.10 / §13.18.19) — a COBOL-2014 introduction (a
        /// variable-length, minimum-length-zero PIC X/N elementary string). Parse-arm (recognition) like the ANY
        /// LENGTH gate: <c>DataItem.IsDynamicLength</c> is cleared by the binder on every SR1/SR18 shape violation,
        /// so a bound-arm home would drop the 0900 on exactly the declaration-error paths. The §13.18.19.3 /
        /// §13.16.3 SR18 shape SRs stay bind-time (DataBinder.BindEntry, COBOLNET1561/1562/1563).</summary>
        public override object? VisitDynamicLengthClause(CobolParserCore.DynamicLengthClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.DynamicLengthItem2014, "the DYNAMIC LENGTH clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The PICTURE EDITING phrase (ISO §13.18.40.2 Format 1) — a COBOL-2023 introduction (user-defined
        /// picture editing via the new reserved word EDITING; Annex E.3.3 item 19). Recognition-based on the presence
        /// of an <c>editingPhrase</c> under the picture clause: the phrase is a purely syntactic 2023 marker wherever
        /// it appears, and <c>PictureAnalyzer</c> RECOVERS the item on the render-staged (sign-control / multi-char /
        /// floating) forms, so a bound-arm gate would drop the 0900 on those paths. NOT <c>InGatedDataEntry</c>-guarded —
        /// unlike BASED/ANY LENGTH, a PICTURE clause legally carries EDITING in the report section too. Fires once per
        /// editing-bearing PICTURE clause; the §13.18.40.3 SR8–SR25 shape rules stay bind-time (PictureAnalyzer,
        /// COBOLNET1591–1602).</summary>
        public override object? VisitPictureClause(CobolParserCore.PictureClauseContext ctx)
        {
            if (ctx.editingPhrase().Length > 0) _p.Check(Constructs.PictureEditing2023, "the PICTURE EDITING phrase");
            // PICTURE format 2 (the LOCALE phrase, §13.18.40.2) — a COBOL-2002 introduction with the locale
            // facility (A.4.9 item 8; kb/Work PB64 T6). Recognition-based here in the PARSE arm because the
            // binder recovers/clears on every SR32–SR37 violation, so a bound-arm gate would drop the 0900 on
            // exactly those paths. NOT also in PictureConstructId — the two arms are disjoint by the class
            // contract and a double gate doubles the diagnostic.
            if (ctx.pictureLocalePhrase() is not null) _p.Check(Constructs.PictureLocaleFormat22002, "the PICTURE LOCALE phrase (format 2)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format 2 (table) VALUE clause (ISO §13.18.40 → §13.18.63.2) — a COBOL-2002 introduction
        /// (literals keyed to OCCURS occurrences by a mandatory FROM (subscript) phrase). Recognition-based on the
        /// presence of a <c>valueClauseTablePhrase</c>: the binder DROPS the table spec on every §13.18.63.3 SR
        /// violation (no OCCURS, bad subscript, …), so a bound-arm gate would lose the 0900 on those paths — the
        /// TYPEDEF/ANY LENGTH drop-proof lesson. Fires once per written table VALUE; the SR16–SR23 shape rules stay
        /// bind-time (DataBinder, COBOLNET1585–1590).</summary>
        public override object? VisitValueClause(CobolParserCore.ValueClauseContext ctx)
        {
            if (ctx.valueClauseTablePhrase().Length > 0) _p.Check(Constructs.ValueTableFormat2002, "the Format 2 (table) VALUE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The TYPE IS type-name clause (the TYPEDEF family, ISO §13.18.58; D17) — a COBOL-2002 introduction.
        /// Fires once per written <c>TYPE IS</c> occurrence: the ExpandTypes clones are DataItem objects, not parse
        /// nodes, so a TYPEDEF referenced N times yields exactly N typeClause nodes (matching the former per-entry
        /// binder Check). The §13.18.57.3 placement SRs stay bind-time.</summary>
        public override object? VisitTypeClause(CobolParserCore.TypeClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.TypeClause2002, "the TYPE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The PROPERTY clause (ISO §13.18.42, OO) — a COBOL-2002 introduction. The OO property SEMANTICS
        /// are bound independently in <c>DataBinder.Oo.OoBindPropertyClauses</c> (which reads the propertyClause node
        /// directly), so the storage-clause loop no longer touches it; this arm only gates the edition.</summary>
        public override object? VisitPropertyClause(CobolParserCore.PropertyClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.PropertyClause2002, "the PROPERTY clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>A constant entry (ISO §13.10 — <c>01 name CONSTANT …</c>) — a COBOL-2002 introduction.
        /// Recognition-based on the dedicated <c>constantEntryBody</c> alternative: the binder folds the entry
        /// into the compile-time constant table and produces NO DataItem (a bound-arm gate would have no node
        /// to key on), and a §13.10 SR violation abandons the fold — so recognition is the drop-proof home
        /// (the TYPEDEF lesson). Fires once per written entry; the §13.10 SRs stay bind-time
        /// (DataBinder.Constants.cs, COBOLNET1547).</summary>
        public override object? VisitConstantEntryBody(CobolParserCore.ConstantEntryBodyContext ctx)
        {
            string name = (ctx.Parent as CobolParserCore.DataDescriptionBodyContext)?.Parent
                is CobolParserCore.DataDescriptionEntryContext e ? e.dataName()?.GetText() ?? "?" : "?";
            _p.Check(Constructs.ConstantEntry2002, $"the constant entry '{name}' (01 … CONSTANT)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The CONSTANT RECORD clause (ISO §13.18.15) — a COBOL-2002 introduction (a structured
        /// constant). Recognition-based like the BASED/ANY LENGTH gates: <c>DataItem.IsConstantRecord</c> is
        /// cleared by the binder on every §13.16.3 SR3/SR6/SR13 shape violation, so a bound-arm home would drop
        /// the 0900 on exactly the declaration-error paths. The placement SRs stay bind-time (COBOLNET1549).</summary>
        public override object? VisitConstantRecordClause(CobolParserCore.ConstantRecordClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.ConstantRecord2002, "the CONSTANT RECORD clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SAME AS clause (ISO §13.18.49; P10 Step 16) — a COBOL-2002 introduction (the
        /// TYPEDEF-family data-description edge). Recognition-based (the typedefClause pattern): the subject's
        /// SameAsName is nulled by ExpandSameAs during bind — and cleared entirely on a §13.16.3 SR12
        /// composition violation — so a bound-arm gate would drop the 0900 on exactly those paths. One Check
        /// per written SAME AS clause.</summary>
        public override object? VisitSameAsClause(CobolParserCore.SameAsClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.SameAsClause2002, "the SAME AS clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The TYPEDEF [STRONG] clause (ISO §13.18.58; D17) — a COBOL-2002 introduction (a type DECLARATION).
        /// Recognition-based (the 14g.2-review correction, DEVLOG 734), NOT bound-arm: the typedef ITEM is dropped
        /// from ConformanceForest whenever RegisterTypeDecl rejects it (unnamed/FILLER, duplicate type-name) or it
        /// binds into method LocalRoots/StaticRoots, so a bound-arm gate lost the 0900 on those declaration-error
        /// paths. The parse node is always present — one Check per written TYPEDEF, matching the former binder site.</summary>
        public override object? VisitTypedefClause(CobolParserCore.TypedefClauseContext ctx)
        {
            if (InGatedDataEntry(ctx)) _p.Check(Constructs.TypedefDef2002, "the TYPEDEF clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>A strongly-typed EXTERNAL type declaration (ISO §13.18.22.3 SR1/SR5; §8.5.3; Annex E.3.3 item 20) —
        /// a COBOL-2023 introduction: E.3.3 item 20 ("External data items may now be strongly typed"), corroborated by
        /// E.2 item 10 ("previously external items could not be strongly typed"), so
        /// it is specifically the <c>STRONG</c>+<c>EXTERNAL</c> combination on a level-1 TYPEDEF that is new. A WEAK
        /// <c>TYPEDEF IS EXTERNAL</c> (no STRONG) was already valid in COBOL-2002 (§13.18.58.3 SR3), so it is NOT gated
        /// here; plain EXTERNAL is 1985-continuous and plain TYPEDEF is the separate COBOL-2002 gate above. Gate on the
        /// CO-OCCURRENCE of the EXTERNAL clause with a STRONG TYPEDEF clause on ONE dataDescriptionEntry. SR5 forces a
        /// strongly-typed external record's type to itself be a strong external type declaration, so gating the
        /// DECLARATION covers both faces (the declaration AND the "strongly-typed external item" that must reference
        /// it). Recognition-based, keyed on the externalClause node (the TYPEDEF/DEVLOG-734 drop-proof lesson: a
        /// bound-arm gate would lose the 0900 whenever RegisterTypeDecl rejects the typedef). Fires once per written
        /// strong-external-typedef entry.</summary>
        public override object? VisitExternalClause(CobolParserCore.ExternalClauseContext ctx)
        {
            if (InGatedDataEntry(ctx) && EntryHasStrongTypedef(ctx))
                _p.Check(Constructs.ExternalTypeDeclaration2023, "a strongly-typed EXTERNAL type declaration");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether the dataDescriptionEntry enclosing <paramref name="ctx"/> also carries a <c>TYPEDEF STRONG</c>
        /// clause — i.e. <paramref name="ctx"/> (an externalClause) sits on a STRONGLY-TYPED type DECLARATION (the 2023
        /// combination, E.3 item 10), not a plain external data item nor a weak (COBOL-2002) external type declaration.
        /// externalClause and typedefClause are both <c>dataDescriptionClause</c> alternatives under the entry's
        /// <c>dataDescriptionClauses</c> list, so the sibling scan is over that list; <c>typedefClause.STRONG()</c> is
        /// the STRONG phrase (the same test <c>DataBinder</c> uses at its typedef bind, §13.18.58.2).</summary>
        private static bool EntryHasStrongTypedef(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.DataDescriptionEntryContext e)
                    return e.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause()
                        ?.Any(c => c.typedefClause()?.STRONG() is not null) ?? false;
            return false;
        }

        /// <summary>The report-group PRESENT WHEN clause (ISO §13.18.41 Format 1; P10 Step 13) — a COBOL-2002
        /// introduction (the 2002 RW modernization; PRESENT itself is §8.9-reserved "added 2002").
        /// Recognition-based: the rule is report-section-exclusive (never shared with data/screen entries), and
        /// the binder drops the condition on its own staging paths, so recognition is the drop-proof home.</summary>
        public override object? VisitReportPresentWhenClause(CobolParserCore.ReportPresentWhenClauseContext ctx)
        {
            _p.Check(Constructs.ReportPresentWhen2002, "the PRESENT WHEN clause (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The report-group VARYING clause (ISO §13.18.64; P10 Step 13) — a COBOL-2002 introduction
        /// (the repetition-counter half of the 2002 RW modernization). Recognition-based; report-section-exclusive
        /// rule. The §13.18.64.3 SRs stay bind-time (COBOLNET1559).</summary>
        public override object? VisitReportVaryingClause(CobolParserCore.ReportVaryingClauseContext ctx)
        {
            _p.Check(Constructs.ReportVarying2002, "the VARYING clause (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The 2002 COLUMN-clause forms (ISO §13.18.14 Format 1; P10 Step 13): more than one operand
        /// (the SR10 "multiple COLUMN clause"), a relative PLUS operand, or the COL/COLS/COLUMNS/NUMBERS/ARE
        /// spellings — the COBOL-85 form was exactly <c>COLUMN NUMBER IS integer-1</c>. Fires at most once per
        /// written clause; report-section-exclusive rule.</summary>
        public override object? VisitReportColumnClause(CobolParserCore.ReportColumnClauseContext ctx)
        {
            if (ctx.COL() is not null || ctx.COLS() is not null || ctx.COLUMNS() is not null
                || ctx.NUMBERS() is not null || ctx.ARE() is not null
                || ctx.reportColumnOperand().Length > 1
                || ctx.reportColumnOperand().Any(o => o.PLUSWORD() is not null))
                _p.Check(Constructs.ReportMultiColumn2002, "the multiple/relative COLUMN clause forms (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>The 2002 LINE-clause forms (ISO §13.18.35 Format 1; P10 Step 13): more than one operand
        /// (the SR10 "multiple LINE clause") or the LINES/NUMBERS/ARE spellings — the COBOL-85 form was
        /// <c>LINE NUMBER IS</c> with ONE operand. The repetition itself also stages LOUD at bind
        /// (COBOLNET0899 report-multiple-line). Report-section-exclusive rule.</summary>
        public override object? VisitReportLineClause(CobolParserCore.ReportLineClauseContext ctx)
        {
            if (ctx.LINES() is not null || ctx.NUMBERS() is not null || ctx.ARE() is not null
                || ctx.reportLineOperand().Length > 1)
                _p.Check(Constructs.ReportMultiLine2002, "the multiple LINE clause form (report group description)");
            return base.VisitChildren(ctx);
        }

        /// <summary>Whether <paramref name="ctx"/> (a data-description clause) sits inside a real
        /// <c>dataDescriptionEntry</c> whose level is NEITHER 66 nor 88 — i.e. exactly the entries the binder routes
        /// through <c>DataBinder.BindEntry</c>'s storage-clause loop (the former gate site). It excludes two families:
        /// (1) a level-66 RENAMES / level-88 condition-name entry, which <c>BindEntries</c> intercepts BEFORE the
        /// clause loop (<c>lvl is 66 or 88</c> → continue), so a clause mis-attached to one was never gated; and
        /// (2) — the 14g.3-review correction (DEVLOG 736) — a report-group or screen-section entry, which reuse the
        /// SHARED <c>occursClause</c> rule (CobolReportWriter.g4 / CobolScreen.g4) but are bound by neither BindEntry
        /// nor <c>OdoBindOccursSpec</c> (report groups → COBOLNET0899; screen sections are unbound), so a bare tree
        /// walk over-fired OCCURS DYNAMIC there. A POSITIVE "inside a gated data entry" test reproduces the binder's
        /// exact reach; the four data-only clauses (BASED/TYPE/PROPERTY/TYPEDEF, never shared with report/screen)
        /// always satisfy it, so their behavior is unchanged.</summary>
        private static bool InGatedDataEntry(Antlr4.Runtime.RuleContext ctx)
        {
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.DataDescriptionEntryContext e)
                    return e.levelNumber()?.GetText() is not ("66" or "88");
            return false;   // no dataDescriptionEntry ancestor — a report-group / screen-section clause the binder never gated
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
        /// untouched. The <see cref="InGatedDataEntry"/> guard restricts firing to a DATA-description entry — the sole
        /// reach of the former <c>OdoBindOccursSpec</c> (called only from <c>BindEntry</c>): the same <c>occursClause</c>
        /// rule ALSO appears in report-group and screen-section entries, which the binder never gated, so a bare tree
        /// walk over-fired there (the 14g.3-review correction, DEVLOG 736).</summary>
        public override object? VisitOccursClause(CobolParserCore.OccursClauseContext ctx)
        {
            if (ctx.DYNAMIC() is not null && InGatedDataEntry(ctx))
                _p.Check(Constructs.OccursDynamic2014, "the OCCURS DYNAMIC clause");
            return base.VisitChildren(ctx);
        }

        // ── Step 14g.4: the file-control / SPECIAL-NAMES / PROCEDURE-DIVISION-header clause gates ───────────────
        // Seven COBOL-2002 introductions on config/file-control/PD-header clauses, ALL parse-arm (recognition). None
        // needs a resolved fact — each is a dedicated clause node with a constant where-string. SHARING/LOCK-MODE were
        // recon-classified bound-arm ("key on FileModel.Sharing/LockMode"), but that is the SAME inverted rationale the
        // 14g.2 review corrected for TYPEDEF: a bound-arm gate keyed on the resolved FileModel would DROP the 0900 if
        // the file is discarded on a SELECT error, whereas the binder fired it on the clause's PRESENCE in the
        // file-control loop — so recognition is byte-exact and drop-proof. The scope match holds because every program
        // unit (top-level AND nested — CallCollectUnits recurses nestedProgram), every class, and every factory gets
        // its OWN DataBinder whose BindDeclarations runs SwitchBindSpecialNames + BindFileControl + CallBindLinkage; the
        // parse-arm's single whole-tree walk equals the UNION of those per-scope binder walks. The one scope asymmetry
        // is PROCEDURE DIVISION RETURNING/RAISING: the procedureDivision rule is SHARED by program and method PDs, but
        // CallBindLinkage gates PROGRAM units only (methods bind via OoBindMethodData) — so those two gates carry the
        // InMethodDefinition guard (the 14g.3 shared-rule lesson).

        /// <summary>The SELECT … SHARING clause (ISO §12.4.5.15) — a COBOL-2002 introduction. Its OPEN-statement twin
        /// (same constructId, the "OPEN SHARING phrase" where-string) already gates from <see cref="VisitOpenStatement"/>;
        /// both fire parse-arm.</summary>
        public override object? VisitSharingClause(CobolParserCore.SharingClauseContext ctx)
        { _p.Check(Constructs.FileSharingClause2002, "the SHARING clause"); return base.VisitChildren(ctx); }

        /// <summary>The SELECT … LOCK MODE clause (ISO §12.4.5.9) — a COBOL-2002 introduction. The §12.4.5.9 SR2
        /// (WITH LOCK ON MULTIPLE RECORDS vs sequential) validation stays in the binder; this arm only names the edition.</summary>
        public override object? VisitLockModeClause(CobolParserCore.LockModeClauseContext ctx)
        { _p.Check(Constructs.LockModeClause2002, "the LOCK MODE clause"); return base.VisitChildren(ctx); }

        /// <summary>The file-control COLLATING SEQUENCE clause (ISO §12.4.5.7 — programmable INDEXED record-key
        /// collating) — a COBOL-2002 introduction. Parses at all editions (superset); this arm names the edition
        /// below 2002. Recognition-fire on the clause's presence, drop-proof on a SELECT error (DEVLOG 724).</summary>
        public override object? VisitFileCollatingSequenceClause(CobolParserCore.FileCollatingSequenceClauseContext ctx)
        { _p.Check(Constructs.FileCollatingClause2002, "the file COLLATING SEQUENCE clause"); return base.VisitChildren(ctx); }

        /// <summary>The SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause (ISO §12.4.5.6.2) — a COBOL-2023
        /// addition (Introduction p.27 / Annex E.3.3 item 42). Parses at all editions (superset); this arm names
        /// the edition below 2023. Recognition-fire on the dedicated phrase rule (DEVLOG-736-safe).</summary>
        public override object? VisitAlternateKeySuppressWhen(CobolParserCore.AlternateKeySuppressWhenContext ctx)
        { _p.Check(Constructs.AlternateKeySuppressWhen2023, "the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause"); return base.VisitChildren(ctx); }

        /// <summary>ALPHABET … FOR ALPHANUMERIC/NATIONAL (ISO §12.3.7) — a COBOL-2002 introduction; the base ALPHABET
        /// clause is version-invariant. One of the three SPECIAL-NAMES FOR-phrase sites (all one constructId +
        /// where-string), gated once per clause on the FOR phrase's presence (either the ISO position between the
        /// name and IS, or the accepted postfix superset — the <c>alphabetForPhrase</c> subrule covers both).
        /// The UCS-4/UTF-8/UTF-16 coded-set phrases (§12.3.7.2, the FOR NATIONAL branch) are §8.9
        /// CONTEXT-SENSITIVE words arriving as plain cobolWord entries — recognized here BY TEXT (never lexer
        /// keywords) and gated as their own 2002 introduction (alphabet-national-2002).</summary>
        public override object? VisitAlphabetClause(CobolParserCore.AlphabetClauseContext ctx)
        {
            if (ctx.alphabetForPhrase().Length > 0)
                _p.Check(Constructs.SpecialNamesForNational2002, "the FOR ALPHANUMERIC/NATIONAL phrase");
            if (ctx.alphabetDefinition() is { } def && def.alphabetEntry() is [{ ChildCount: 1 } entry]
                && entry.GetChild(0) is CobolParserCore.CobolWordContext w
                && w.GetText().ToUpperInvariant() is "UCS-4" or "UTF-8" or "UTF-16")
                _p.Check(Constructs.AlphabetNational2002, $"the ALPHABET {w.GetText().ToUpperInvariant()} phrase");
            // `IS LOCALE [locale-name-2]` (§12.3.7.2, either branch) — the locale facility's collating sequence, a 2002
            // introduction (Annex A.4.9 item 10; kb/Work PB101). LOCALE is a plain word below 2002 (a code-name there),
            // so the phrase is recognized by SHAPE, the same test the binder applies (DataBinder.IsAlphabetLocalePhrase).
            if (ctx.alphabetDefinition() is { } ldef && CobolNet.Binding.DataBinder.IsAlphabetLocalePhrase(ldef))
                _p.Check(Constructs.AlphabetLocale2002, "the ALPHABET LOCALE phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SPECIAL-NAMES ORDER TABLE clause (ISO §12.3.7.2 — the cultural ordering table
        /// STANDARD-COMPARE names, §12.3.7.4 GR17): a 2002 introduction, gated on recognition (kb/Work PB101).
        /// <para>⚠ The grammar recognizes the clause at EVERY edition (unlike the LOCALE clause's edition-gated
        /// predicate — TABLE is reserved at all four, so ORDER + TABLE has no competing '85 reading), which is
        /// precisely what makes this gate reachable below 2002: the answer there is this explanatory
        /// introduction diagnostic rather than a parse error at the word TABLE.</para></summary>
        public override object? VisitOrderTableClause(CobolParserCore.OrderTableClauseContext ctx)
        {
            _p.Check(Constructs.OrderTable2002, "the ORDER TABLE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §12.3.6.2 — Annex A.4.9 item 7; kb/Work PB64
        /// T5): a 2002 introduction, gated on recognition (the clause parses at every edition — CLASSIFICATION is a
        /// context-sensitive word matched by text).</summary>
        public override object? VisitCharacterClassificationClause(CobolParserCore.CharacterClassificationClauseContext ctx)
        {
            _p.Check(Constructs.CharacterClassification2002, "the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>The SPECIAL-NAMES LOCALE clause (ISO §12.3.7.2 — the locale facility's declaration, Annex A.4.9 item
        /// 10; kb/Work PB64 T1): a 2002 introduction, gated on recognition. The grammar recognizes the clause by SHAPE at
        /// every edition (the '85 switch shapes are excluded by its predicate), so below 2002 the answer is this
        /// explanatory introduction diagnostic rather than a parse error at the clause's own literal.</summary>
        public override object? VisitLocaleClause(CobolParserCore.LocaleClauseContext ctx)
        {
            _p.Check(Constructs.SpecialNamesLocale2002, "the SPECIAL-NAMES LOCALE clause");
            return base.VisitChildren(ctx);
        }

        /// <summary>SET LOCALE … (ISO §14.9.39 Format 11) / SET … TO LOCALE … (Format 12) — the locale facility's statements
        /// (Annex A.4.9 item 9; kb/Work PB64 T1), 2002 introductions gated on recognition. The two formats share one parse
        /// rule; the words' POSITION tells them apart (format 12's LOCALE follows the identifier and TO).</summary>
        public override object? VisitSetLocaleStatement(CobolParserCore.SetLocaleStatementContext ctx)
        {
            bool save = ctx.cobolWord(0).Start.TokenIndex > ctx.dataReference().Start.TokenIndex;
            _p.Check(save ? Constructs.SetSaveLocale2002 : Constructs.SetLocale2002,
                save ? "SET … TO LOCALE (save-locale, §14.9.39 Format 12)" : "SET LOCALE (set-locale, §14.9.39 Format 11)");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROGRAM COLLATING SEQUENCE's national surface (ISO §12.3.6.2) — alphabet-name-2 of the
        /// two-name IS form and the FOR ALPHANUMERIC/FOR NATIONAL forms are 2002 introductions (the 85 format is
        /// the single-name IS form); one gate per clause on recognition.</summary>
        public override object? VisitProgramCollatingSequenceClause(CobolParserCore.ProgramCollatingSequenceClauseContext ctx)
        {
            if (ctx.collatingForPhrase().Length > 0 || ctx.cobolWord().Length > 1)
                _p.Check(Constructs.ProgramCollatingNational2002,
                    "the PROGRAM COLLATING SEQUENCE alphabet-name-2 / FOR ALPHANUMERIC/NATIONAL forms");
            return base.VisitChildren(ctx);
        }

        /// <summary>OBJECT-COMPUTER clauses WITHOUT computer-name-1 (ISO §12.3.6.2 — the name is optional and the
        /// clauses may follow the period directly): a 2002 relaxation — X3.23-1985 hung MEMORY SIZE / PROGRAM COLLATING
        /// SEQUENCE / SEGMENT-LIMIT off a REQUIRED computer-name (kb/Work PB78). The empty paragraph is legal at every
        /// edition; only a clause without a name is gated. CHARACTER CLASSIFICATION itself BINDS since kb/Work PB64 T5
        /// (the A.4.9 locale module is implemented; the COBOLNET1518 non-support era is over).</summary>
        public override object? VisitObjectComputerParagraph(CobolParserCore.ObjectComputerParagraphContext ctx)
        {
            if (ctx.computerName() is null && ctx.objectComputerClause().Length > 0)
                _p.Check(Constructs.ComputerNameOptional2002, "an OBJECT-COMPUTER clause without computer-name-1");
            return base.VisitChildren(ctx);
        }

        /// <summary>CLASS … FOR ALPHANUMERIC/NATIONAL (ISO §12.3.7) — the second SPECIAL-NAMES FOR-phrase site (matching
        /// SwitchBindClass's <c>cd.FOR()</c>).</summary>
        public override object? VisitClassDefinitionClause(CobolParserCore.ClassDefinitionClauseContext ctx)
        {
            if (ctx.FOR() is not null) _p.Check(Constructs.SpecialNamesForNational2002, "the FOR ALPHANUMERIC/NATIONAL phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>SYMBOLIC CHARACTERS … FOR ALPHANUMERIC/NATIONAL (ISO §12.3.7) — the third SPECIAL-NAMES FOR-phrase
        /// site (matching the <c>sc.FOR()</c> check). The base SYMBOLIC CHARACTERS clause stays accepted-inert.</summary>
        public override object? VisitSymbolicCharactersClause(CobolParserCore.SymbolicCharactersClauseContext ctx)
        {
            if (ctx.FOR() is not null) _p.Check(Constructs.SpecialNamesForNational2002, "the FOR ALPHANUMERIC/NATIONAL phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The FD CODE-SET clause's NATIONAL half (ISO §13.18.13.2; kb/Work PB110) — alphabet-name-2 and the
        /// FOR phrases entered at 2002 with the national repertoire; the '85 format was the single-name IS form.</summary>
        public override object? VisitCodeSetClause(CobolParserCore.CodeSetClauseContext ctx)
        {
            if (ctx.codeSetForPhrase().Length > 0 || ctx.cobolWord().Length > 1)
                _p.Check(Constructs.CodeSetNational2002, "the CODE-SET national phrases (alphabet-name-2 / FOR)");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROCEDURE DIVISION … RETURNING (ISO §14.2) — a COBOL-2002 introduction. Gated only OUTSIDE a method
        /// (the shared procedureDivision rule serves both, but CallBindLinkage — the former gate site — runs for program
        /// units only; a method's RETURNING binds through OoBindMethodData, ungated). §14.2.3 GR6/SR1 stay bind-time.</summary>
        public override object? VisitReturningClause(CobolParserCore.ReturningClauseContext ctx)
        {
            if (!InMethodDefinition(ctx))
                _p.Check(Constructs.ProcedureReturning2002, "the PROCEDURE DIVISION RETURNING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROCEDURE DIVISION … RAISING (ISO §14.2.2) — a COBOL-2002 introduction. Same program-only scope as
        /// RETURNING; the RAISING clause's EC semantics stay in StatementBinder.Exceptions.</summary>
        public override object? VisitRaisingClause(CobolParserCore.RaisingClauseContext ctx)
        {
            if (!InMethodDefinition(ctx))
                _p.Check(Constructs.ProcedureRaising2002, "the PROCEDURE DIVISION RAISING phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>PROCEDURE DIVISION USING … BY VALUE (ISO §14.2.2 using-phrase :23641) — a COBOL-2002
        /// introduction, the header-side twin of <see cref="Constructs.CallByValue2002"/> (one-construct-one-gate:
        /// §14.2 header vs §14.9.4 statement). Fires on RECOGNITION, once per BY VALUE parameter phrase, in
        /// programs, functions, AND methods alike (a method-header occurrence below 2002 is already inside the
        /// gated class construct — the extra where-string only sharpens the report). The §14.2.2 SR2 class
        /// restriction and the §14.2.3 GR10 value-copy semantics stay bind-time (DataBinder.CallBindLinkage).</summary>
        public override object? VisitUsingByValue(CobolParserCore.UsingByValueContext ctx)
        {
            _p.Check(Constructs.PdHeaderByValue2002, "the PROCEDURE DIVISION USING BY VALUE phrase");
            return base.VisitChildren(ctx);
        }

        /// <summary>The LOCAL-STORAGE SECTION (ISO §13.6 — automatic data) — a COBOL-2002 introduction. Fires on
        /// RECOGNITION wherever the shared <c>dataDivision</c> rule carries it (programs, functions, AND methods
        /// alike — a method occurrence below 2002 is already inside the gated class construct; the extra 0900 only
        /// sharpens the report, the VisitUsingByValue posture). The §13.6.4 GR1 activation-state semantics
        /// (automatic data — initial state on every activation, §14.6.2.3.2) stay bind/emit-time
        /// (<c>DataBinder.LocalStorageRoots</c> + the P10 RECURSIVE-WS activation-state model).</summary>
        public override object? VisitLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx)
        {
            _p.Check(Constructs.LocalStorageSection2002, "the LOCAL-STORAGE SECTION");
            return base.VisitChildren(ctx);
        }

        // ── Step 14g.5: the REPOSITORY specifier gates ────────────────────────────────────────────────────────
        /// <summary>A REPOSITORY CLASS / INTERFACE / PROPERTY / PROGRAM specifier (ISO §12.3.8) — each a COBOL-2002
        /// introduction. Parse-arm (recognition): the entry is one <c>repositoryEntry</c> node, mirroring the binder's
        /// PROPERTY→PROGRAM→INTERFACE→CLASS <c>else if</c> order and its name-embedding where-strings. The
        /// FUNCTION-intrinsic alternatives are version-invariant (ungated). ⚠ Like the SPECIAL-NAMES / file-control
        /// gates, REPOSITORY is in the CONFIGURATION SECTION, so for a CLASS the former per-scope binder gated it
        /// 0/1/2× (the flagged OO-env double/zero-bind, DEVLOG 738); the parse-arm fires the spec-correct ONCE. The
        /// property and program-prototype NAMES still register for reference resolution in <c>DataBinder</c>.</summary>
        public override object? VisitRepositoryEntry(CobolParserCore.RepositoryEntryContext ctx)
        {
            if (ctx.PROPERTY() is not null && ctx.propertyName() is { } pn)
                _p.Check(Constructs.RepositoryProperty2002, $"REPOSITORY PROPERTY '{pn.GetText()}'");
            // §12.3.8.2's program-specifier (kb/Work PB237) — the ONE surface that declares a program-prototype-name.
            // The whole program-prototype facility is 2002 (the '85 CALL general format has Format 1 only), so the
            // declaration gate and the Format-2 CALL / prototype CANCEL gates all key on the same edition.
            else if (ctx.PROGRAM() is not null && ctx.programPrototypeName() is { } ppn)
                _p.Check(Constructs.RepositoryProgram2002, $"REPOSITORY PROGRAM '{ppn.GetText()}'");
            else if (ctx.INTERFACE() is not null && ctx.interfaceName() is { } ifn)
                _p.Check(Constructs.RepositoryInterface2002, $"REPOSITORY INTERFACE '{ifn.GetText()}'");
            else if (ctx.CLASS() is not null && ctx.className() is { } cn)
                _p.Check(Constructs.RepositoryClass2002, $"REPOSITORY CLASS '{cn.GetText()}'");
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
            // GOBACK itself is a 2002 introduction (§14.9.18; §14.9.16 in 2002 — Exec Step E, folded from the
            // CallBinder gate). Fires only for a BARE GOBACK: when RETURNING is present its more-specific 0900
            // (above) subsumes the 0880, and a METHOD GOBACK is a method return, never an activation return
            // (§14.9.18.4 GR4 — the same InMethodDefinition exclusion the binder's InMethod short-circuit gave).
            else if (ctx.dataReference() is null && !InMethodDefinition(ctx))
                _p.Check(Constructs.GobackBare2002, "the GOBACK statement");
            // GOBACK … WITH NORMAL/ERROR STATUS (§14.9.18.2) — a COBOL-2023 introduction (annex item 32: GOBACK
            // "now allows the same status phrase as the STOP statement"). DISTINCT edition from the STOP-status
            // gate (StopRunStatus2002 = 2002); the shared statusPhrase rule is 2002-gated on STOP, 2023 on GOBACK.
            if (ctx.statusPhrase() is not null && !InMethodDefinition(ctx))
                _p.Check(Constructs.GobackStatus2023, "the GOBACK … WITH NORMAL/ERROR STATUS phrase");
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
            // Both phrases hang off the ONE callExceptionPhrases container (either order — ISO 5.2.6.4).
            var callExc = ctx.callExceptionPhrases();
            if (callExc?.callOnExceptionPhrase()?.OVERFLOW() is not null
                || callExc?.callNotOnExceptionPhrase()?.OVERFLOW() is not null)
                _p.Check(Constructs.CallOnOverflowRemoved2023, "the CALL statement");
            // CALL … RETURNING (§14.9.4) — a 2002 introduction (Exec Step E — folded from the CallBinder gate).
            if (ctx.callReturningPhrase() is not null)
                _p.Check(Constructs.CallReturning2002, "CALL … RETURNING");
            return base.VisitChildren(ctx);
        }

        /// <summary>END-ACCEPT (ISO §14.9.1, a 2002 introduction). Mirrors the binder's <c>AcceptHasTerminator</c>
        /// token scan EXACTLY. NOTE — a grammar gap: <c>acceptStatement</c> has no <c>END_ACCEPT</c> alternative, so
        /// the scan finds nothing and this gate fires ZERO times today (as the bound-arm's
        /// BoundAccept.HasEndTerminator did); kept in the correct home so it lights up if the grammar gains the
        /// terminator.</summary>
        /// <summary>END-DISPLAY (§14.9.11.2, a COBOL-2002 addition like every explicit scope terminator of the
        /// 2002 wave) — the registered end-accept twin had a gate and this had NONE, so `DISPLAY X END-DISPLAY`
        /// was silently accepted at 85 (kb/Work PB134).</summary>
        public override object? VisitDisplayStatement(CobolParserCore.DisplayStatementContext ctx)
        {
            for (int i = 0; i < ctx.ChildCount; i++)
                if (ctx.GetChild(i) is Antlr4.Runtime.Tree.ITerminalNode { Symbol.Type: CobolLexer.END_DISPLAY })
                {
                    _p.Check(Constructs.EndDisplay2002, "the DISPLAY statement");
                    break;
                }
            return base.VisitChildren(ctx);
        }

        public override object? VisitAcceptStatement(CobolParserCore.AcceptStatementContext ctx)
        {
            for (int i = 0; i < ctx.ChildCount; i++)
                if (ctx.GetChild(i) is Antlr4.Runtime.Tree.ITerminalNode { Symbol.Type: CobolLexer.END_ACCEPT })
                {
                    _p.Check(Constructs.EndAccept2002, "the ACCEPT statement");
                    break;
                }
            // The four-digit-year temporal phrases (§14.9.1 — the 1985 formats list only bare DATE / DAY /
            // DAY-OF-WEEK / TIME): a 2002 introduction (Exec Step E — folded from the AcceptDisplayBinder gate).
            if (ctx.acceptSource() is { } src && (src.YYYYMMDD() ?? src.YYYYDDD()) is not null)
                _p.Check(Constructs.AcceptFourDigitYear2002,
                    src.DATE() is not null ? "ACCEPT FROM DATE YYYYMMDD" : "ACCEPT FROM DAY YYYYDDD");
            return base.VisitChildren(ctx);
        }

        // ── Exec Step E: the folded binder-inline gates (plan §4.1 task #13) ────────────────────────────────
        // Each was an inline `DialectLevel` gate that moved VERBATIM with its verb at P7 Step 10; folded here so
        // the two-arm pass is the ONE gating funnel and every gate enters the version matrix (its registry row
        // carries the negative witness). Recognition-based (DEVLOG 724): a below-edition construct names its
        // edition even when it ALSO fails to bind. The catalog-driven per-NAME windows (the D8 intrinsic windows,
        // the ExceptionCatalog EC-name windows, the PictureAnalyzer symbol rows, the digit caps) STAY binder-side
        // by design — their version facts live in their own tables, not in constructs.json — as do the two
        // sanctioned BEHAVIORAL edition reads (the <2002 keyword-omitted FUNCTION routing, the ≥2002 MOVE CORR
        // pair-selection window) and the SYNCHRONIZED-on-group site (sync-on-group-2023 — bind-side because
        // "has subordinates" is not a parse-tree fact, but through the canonical Check funnel since CA14, so it
        // is no longer a severity exception: NO site makes an introduction lenient).

        /// <summary>INSPECT BACKWARD (ISO §14.9.22.2; VCR row 77) — a COBOL-2023 introduction.</summary>
        public override object? VisitInspectStatement(CobolParserCore.InspectStatementContext ctx)
        {
            if (ctx.BACKWARD() is not null)
                _p.Check(Constructs.InspectBackward2023, "INSPECT BACKWARD");
            return base.VisitChildren(ctx);
        }

        /// <summary>The INITIALIZE post-85 surface (ISO §14.9.20) — WITH FILLER / TO VALUE / TO DEFAULT / the
        /// THEN connective, each a 2002 introduction with its own pinned code (REPLACING itself is COBOL-85).</summary>
        public override object? VisitInitializeStatement(CobolParserCore.InitializeStatementContext ctx)
        {
            if (ctx.FILLER() is not null)
                _p.Check(Constructs.InitializeFiller2002, "INITIALIZE … WITH FILLER");
            if (ctx.initializeCategoryToValue() is not null)
                _p.Check(Constructs.InitializeToValue2002, "INITIALIZE … TO VALUE");
            if (ctx.initializeDefaultPhrase() is not null)
                _p.Check(Constructs.InitializeToDefault2002, "INITIALIZE … TO DEFAULT");
            if (ctx.initializeReplacingPhrase()?.THEN() is not null)
                _p.Check(Constructs.InitializeThenReplacing2002, "INITIALIZE … THEN REPLACING");
            return base.VisitChildren(ctx);
        }

        /// <summary>RELEASE … FROM literal-1 (ISO §14.9.32.2) — X3.23-1985 allows only an identifier as the
        /// FROM operand; the literal form is 2002+.</summary>
        public override object? VisitReleaseFrom(CobolParserCore.ReleaseFromContext ctx)
        {
            if (ctx.literal() is not null)
                _p.Check(Constructs.ReleaseFromLiteral2002, "RELEASE … FROM literal-1");
            return base.VisitChildren(ctx);
        }

        /// <summary>The Format-2 in-place table SORT (ISO §14.9.40) — a 2002 introduction. The F2 shape is
        /// syntactic: a SORT with neither USING/GIVING nor INPUT/OUTPUT procedures (F1 requires an input AND an
        /// output leg, §14.9.40.2), so recognition never needs the operand's resolved name class.</summary>
        public override object? VisitSortStatement(CobolParserCore.SortStatementContext ctx)
        {
            if (ctx.sortUsingPhrase() is null && ctx.sortInputProcedurePhrase() is null
                && ctx.sortGivingPhrase() is null && ctx.sortOutputProcedurePhrase() is null)
                _p.Check(Constructs.TableSort2002, "SORT of a table (Format 2, ISO §14.9.40)");
            return base.VisitChildren(ctx);
        }

        /// <summary>SORT/MERGE COLLATING SEQUENCE's national surface (ISO §14.9.40.2 / §14.9.24.2 —
        /// §14.9.40.3 SR2 national collating): alphabet-name-2 of the IS form and the FOR ALPHANUMERIC/FOR
        /// NATIONAL forms are 2002 introductions (the national class); one gate per phrase on recognition.</summary>
        public override object? VisitSortCollatingPhrase(CobolParserCore.SortCollatingPhraseContext ctx)
        {
            if (ctx.cobolWord().Length > 1 || ctx.collatingForPhrase().Length > 0)
                _p.Check(Constructs.SortCollatingNational2002, "COLLATING SEQUENCE alphabet-name-2 / FOR forms");
            return base.VisitChildren(ctx);
        }

        /// <summary>RAISE (ISO §14.9.29) — the 2002+ exception-condition statement.</summary>
        public override object? VisitRaiseStatement(CobolParserCore.RaiseStatementContext ctx)
        { _p.Check(Constructs.RaiseStatement2002, "the RAISE statement"); return base.VisitChildren(ctx); }

        /// <summary>RESUME (ISO §14.9.33) — the 2002+ exception-recovery statement.</summary>
        public override object? VisitResumeStatement(CobolParserCore.ResumeStatementContext ctx)
        { _p.Check(Constructs.ResumeStatement2002, "the RESUME statement"); return base.VisitChildren(ctx); }

        /// <summary>SET LAST EXCEPTION TO OFF (ISO §14.9.39 Format 13) — the 2002+ saved-exception form.</summary>
        public override object? VisitSetLastExceptionStatement(CobolParserCore.SetLastExceptionStatementContext ctx)
        { _p.Check(Constructs.SetLastException2002, "SET LAST EXCEPTION TO OFF"); return base.VisitChildren(ctx); }

        /// <summary>The statement-level RAISING phrase (ISO §14.9.18.2 / §14.9.14.2 F2 — the ONE rule GOBACK and
        /// the EXIT forms share) — 2002+ exception propagation. Distinct from the PROCEDURE DIVISION header
        /// RAISING clause (<c>raisingClause</c> → procedure-raising-2002).</summary>
        public override object? VisitRaisingPhrase(CobolParserCore.RaisingPhraseContext ctx)
        { _p.Check(Constructs.StatementRaising2002, "the GOBACK / EXIT … RAISING phrase"); return base.VisitChildren(ctx); }

        /// <summary>PROGRAM-ID … RECURSIVE (ISO §11.10) — a 2002 introduction. Recognized by TOKEN TYPE within
        /// the attribute list (the END-ACCEPT scan idiom), never by word text — an AS-literal cannot collide.</summary>
        public override object? VisitProgramIdAttributes(CobolParserCore.ProgramIdAttributesContext ctx)
        {
            if (FindsToken(ctx, CobolLexer.RECURSIVE))
                _p.Check(Constructs.ProgramIdRecursive2002, "PROGRAM-ID … RECURSIVE");
            return base.VisitChildren(ctx);
        }

        private static bool FindsToken(Antlr4.Runtime.Tree.IParseTree node, int tokenType)
        {
            if (node is Antlr4.Runtime.Tree.ITerminalNode t) return t.Symbol.Type == tokenType;
            for (int i = 0; i < node.ChildCount; i++)
                if (FindsToken(node.GetChild(i), tokenType)) return true;
            return false;
        }

        /// <summary>The OPTIONS paragraph (ISO §11.9) — a 2002 introduction (the 2002 standard added it with the
        /// ARITHMETIC clause; Annex E.2 item 21 back-derives the container from obsolete-in-2014 Standard
        /// Arithmetic). The binder still returns <c>OptionsModel.Default</c> below 2002 (silent routing — this
        /// arm owns the diagnostic). The 2014-only clauses carry their own arms below (P10 Step 12).</summary>
        public override object? VisitOptionsParagraph(CobolParserCore.OptionsParagraphContext ctx)
        { _p.Check(Constructs.OptionsParagraph2002, "the OPTIONS paragraph"); return base.VisitChildren(ctx); }

        /// <summary>The ARITHMETIC clause's mode keywords (ISO §11.9.5; P10 Step 12). STANDARD is the dual-window
        /// 2002 mode: introduced 2002, obsolete 2014, DROPPED by 2023 (Annex E.2 item 21 — use STANDARD-DECIMAL);
        /// the Removed verdict is error-strict / warning-permissive (a permissive 2023 compile continues under
        /// standard arithmetic). STANDARD-BINARY / STANDARD-DECIMAL are 2014 keywords of the 2002 clause — their
        /// introduction edges live HERE now that the paragraph parses at 2002 (NATIVE needs no arm: the paragraph
        /// row IS its introduction).</summary>
        public override object? VisitArithmeticMethod(CobolParserCore.ArithmeticMethodContext ctx)
        {
            if (ctx.STANDARD() is not null)
                _p.Check(Constructs.ArithmeticStandard2002, "ARITHMETIC IS STANDARD");
            if (ctx.STANDARD_DECIMAL() is not null)
                _p.Check(Constructs.ArithmeticStandardDecimal2014, "ARITHMETIC IS STANDARD-DECIMAL");
            if (ctx.STANDARD_BINARY() is not null)
                _p.Check(Constructs.ArithmeticStandardBinary2014, "ARITHMETIC IS STANDARD-BINARY");
            return base.VisitChildren(ctx);
        }

        // ── The 2014-only OPTIONS clauses (ISO §11.9.6–§11.9.11; P10 Step 12): with the paragraph gate at
        // 2002, each 2014 clause fires its own introduction row on RECOGNITION. ENTRY-CONVENTION is gated 2014
        // conservatively (the in-repo 85/2002 evidence chain establishes only the ARITHMETIC clause at 2002 —
        // the constructs.json row records the ambiguity). ──────────────────────────────────────────────────

        /// <summary>DEFAULT ROUNDED (ISO §11.9.6) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitDefaultRoundedClause(CobolParserCore.DefaultRoundedClauseContext ctx)
        { _p.Check(Constructs.OptionsDefaultRounded2014, "the DEFAULT ROUNDED clause"); return base.VisitChildren(ctx); }

        /// <summary>INTERMEDIATE ROUNDING (ISO §11.9.11) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitIntermediateRoundingClause(CobolParserCore.IntermediateRoundingClauseContext ctx)
        { _p.Check(Constructs.OptionsIntermediateRounding2014, "the INTERMEDIATE ROUNDING clause"); return base.VisitChildren(ctx); }

        /// <summary>ENTRY-CONVENTION (ISO §11.9.7) — gated 2014 conservatively (see the block note).</summary>
        public override object? VisitEntryConventionClause(CobolParserCore.EntryConventionClauseContext ctx)
        { _p.Check(Constructs.OptionsEntryConvention2014, "the ENTRY-CONVENTION clause"); return base.VisitChildren(ctx); }

        /// <summary>FLOAT-BINARY (ISO §11.9.8) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitFloatBinaryClause(CobolParserCore.FloatBinaryClauseContext ctx)
        { _p.Check(Constructs.OptionsFloatBinary2014, "the FLOAT-BINARY clause"); return base.VisitChildren(ctx); }

        /// <summary>FLOAT-DECIMAL (ISO §11.9.9) — a 2014 clause of the 2002 OPTIONS paragraph.</summary>
        public override object? VisitFloatDecimalClause(CobolParserCore.FloatDecimalClauseContext ctx)
        { _p.Check(Constructs.OptionsFloatDecimal2014, "the FLOAT-DECIMAL clause"); return base.VisitChildren(ctx); }

        /// <summary>OPTIONS INITIALIZE (ISO §11.9.10) — a 2023 clause of the 2002 OPTIONS paragraph (Annex E §E.3.3
        /// item 33: a NEW 2023 clause using already-reserved words, not a 2014 clause).</summary>
        public override object? VisitOptionsInitializeClause(CobolParserCore.OptionsInitializeClauseContext ctx)
        { _p.Check(Constructs.OptionsInitialize2023, "the OPTIONS INITIALIZE clause"); return base.VisitChildren(ctx); }

        /// <summary>CURRENCY SIGN … WITH PICTURE SYMBOL (ISO §12.3.7) — the 1985 clause had only the
        /// single-character literal form; the PICTURE SYMBOL form is 2002+.</summary>
        public override object? VisitCurrencySignClause(CobolParserCore.CurrencySignClauseContext ctx)
        {
            if (ctx.PIC() is not null)
                _p.Check(Constructs.CurrencyPictureSymbol2002, "CURRENCY SIGN … WITH PICTURE SYMBOL");
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

        /// <summary>⭐ THE boolean-operator introduction gate — ONE rule, called once per GRAMMAR SITE that hosts
        /// a top-level <c>booleanExpression</c>. B-AND/B-OR/B-XOR/B-NOT are a COBOL-2002 introduction (ISO
        /// §8.7.2) and the four boolean SHIFT operators a COBOL-2023 one (§8.8.2 rule 8).
        /// <para>⚠ THE GATE IS PER-SITE, NOT PER-NODE, AND THAT IS DELIBERATE: the <c>booleanExpression</c>
        /// tiers nest through parentheses and through the relation form, so a gate hanging off the tier rule
        /// itself would fire once per nesting level and multiply the diagnostic. Passing a site's operand(s)
        /// here fires exactly once for that site, matching the binder's own altitude
        /// (<c>be.Any(HasBoolOp)</c> in BindPrimaryBoolean).</para>
        /// <para>⛔ IT WAS COPIED PER SITE UNTIL PB46 ADDED A THIRD ONE. Two identical four-line bodies is the
        /// point at which a new grammar site silently ships UNGATED — a 2023 shift operator accepted under
        /// <c>--std 2002</c>. The hosting sites are enumerated by <c>BooleanExpressionGateSiteDriftTests</c>,
        /// which reads the .g4 files, so a fourth one fails a test instead of passing silently.</para></summary>
        private void GateBooleanOperators(params CobolParserCore.BooleanExpressionContext?[] operands)
        {
            if (operands.Any(b => b is not null && HasBoolOp(b)))
                _p.Check(Constructs.BooleanOperators2002, "the boolean operators (B-AND/B-OR/B-XOR/B-NOT)");
            if (operands.Any(b => b is not null && HasShiftOp(b)))
                _p.Check(Constructs.BooleanShiftOperators2023, "the boolean shift operators (B-SHIFT-L/R/LC/RC)");
        }

        /// <summary>Site — a bare boolean-expression CALL argument (§14.9.4.2 Format 2's keyword-less
        /// boolean-expression-1; kb/Work PB130 — the callArgument alternation gained the guarded arm and
        /// the BooleanExpressionGateSiteDrift net demanded this gate the same day).</summary>
        public override object? VisitCallArgument(CobolParserCore.CallArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OMITTED argument");
            return base.VisitChildren(ctx);
        }

        /// <summary>The omitted-argument facility's other three surfaces (kb/Work PB133 wave C) — all
        /// COBOL-2002 introductions on one construct row: the BY REFERENCE OMITTED spelling, the OPTIONAL
        /// formal-parameter phrase (§14.2.2), and the §8.8.4.8 omitted-argument condition.</summary>
        public override object? VisitCallByReference(CobolParserCore.CallByReferenceContext ctx)
        {
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OMITTED argument");
            return base.VisitChildren(ctx);
        }

        public override object? VisitUsingParameter(CobolParserCore.UsingParameterContext ctx)
        {
            if (ctx.OPTIONAL() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OPTIONAL formal parameter");
            return base.VisitChildren(ctx);
        }

        public override object? VisitUsingByReference(CobolParserCore.UsingByReferenceContext ctx)
        {
            if (ctx.OPTIONAL() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the OPTIONAL formal parameter");
            return base.VisitChildren(ctx);
        }

        public override object? VisitComparisonExpression(CobolParserCore.ComparisonExpressionContext ctx)
        {
            if (ctx.OMITTED() is not null)
                _p.Check(Constructs.OmittedArguments2002, "the omitted-argument condition");
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 1 — a boolean expression in a CONDITION (§8.8.4.2.2 relation / §8.8.4.3 simple
        /// condition). A B-op-free comparison uses the untouched shared comparison rule and never enters here.</summary>
        public override object? VisitPrimaryCondition(CobolParserCore.PrimaryConditionContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 2 — the COMPUTE Format 2 RHS (ISO §14.9.8). The F1 arithmetic alternative has no
        /// <c>booleanExpression</c>.</summary>
        public override object? VisitComputeStatement(CobolParserCore.ComputeStatementContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 4 — <c>CALL … USING BY CONTENT boolean-expression-1</c> (ISO §14.9.4.2 Format 2;
        /// fix-queue PB46 CALL half). ⚙ THIS OVERRIDE EXISTS BECAUSE THE DRIFT TEST DEMANDED IT: widening
        /// <c>callByContent</c> added a fourth grammar site and
        /// <c>BooleanExpressionGateSiteDriftTests</c> — written one item earlier for exactly this — failed with
        /// "grammar rule(s) admit a booleanExpression with no introduction gate: callByContent". The list is
        /// derived from the <c>.g4</c> files, so it saw the new site the moment it appeared.</summary>
        public override object? VisitCallByContent(CobolParserCore.CallByContentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>Site 3 — <c>INVOKE … USING BY CONTENT boolean-expression-1</c> (ISO §14.9.23.2; fix-queue
        /// PB46). The BooleanOperators2002 half is unreachable in practice — INVOKE is itself a COBOL-2002
        /// introduction, so no edition admits the statement but not the operators — but the SHIFT half is
        /// live: <c>BY CONTENT B1 B-SHIFT-L 2</c> is a 2023 construct inside a 2002 statement, and without
        /// this call it would compile clean under <c>--std 2002</c> and <c>--std 2014</c>.</summary>
        public override object? VisitInvokeArgument(CobolParserCore.InvokeArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
            return base.VisitChildren(ctx);
        }

        /// <summary>An intrinsic-function ARGUMENT that is a boolean expression (§8.4.3.2.3 SR8; kb/Work PB65 —
        /// `INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B)`): the same boolean-operator introduction gates as every other
        /// boolean-expression site (the 2002 data / the 2023 shift operators).</summary>
        public override object? VisitFunctionArgument(CobolParserCore.FunctionArgumentContext ctx)
        {
            GateBooleanOperators(ctx.booleanExpression());
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
            // §8.3.3.2.3 r6 / §8.3.3.5.3 r5 — the hexadecimal GROUPING rule (fix-queue R03). Checked HERE because
            // this override is the one place that sees EVERY nonNumericLiteral in the unit, VALUE clauses and
            // level-88 operands included — not only the statement-scoped ones the introduction gates above screen.
            // ⛔ IT IS DELIBERATELY NOT A LEXER RULE. A lexer that refused an odd digit count would not reject the
            // program: the token would simply fail to match, `X"414"` would split into an IDENTIFIER and a
            // STRINGLIT, and the literal would be back in the SILENT-degradation hole R03 exists to close. The
            // token must match so that something is left to diagnose.
            // ⚠ The pass's own §8.3.2.1 word-length check (VisitCobolWord) is the precedent for a syntax rule
            // living in this walker: it is the tree walk, not a version-only walk.
            foreach (var t in new[] { ctx.HEXLIT(), ctx.NATLIT(), ctx.BOOLLIT() })
                if (t is not null && CobolLiteral.HexGroupViolation(t.GetText()) is { } why)
                    _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.HexLiteralDigitGrouping.Code,
                        EditionSeverity.Error, DiagnosticCatalog.HexLiteralDigitGrouping.Id,
                        $"the literal {t.GetText()} {why}", "", "ISO §8.3.3"));
            return base.VisitChildren(ctx);
        }

        /// <summary>The <c>ALL literal</c> figurative's literal-1 (ISO §8.3.3.6.3 SR2 — kb/Work PB71): a national or
        /// boolean literal-1 is the same COBOL-2002 introduction as the bare literal (statement-scoped, as above —
        /// a VALUE clause's is the data/PIC gate's), and a hexadecimal literal-1 of any class takes the §8.3.3 digit
        /// GROUPING check. The tokens are direct children of figurativeConstant, not of nonNumericLiteral, so the
        /// override above never saw them — `ALL B"1"` at --std 85 was ungated.</summary>
        public override object? VisitFigurativeConstant(CobolParserCore.FigurativeConstantContext ctx)
        {
            var ops = ctx.allLiteral()?.allLiteralOperand() ?? [];
            if (ops.Length == 0) return base.VisitChildren(ctx);
            bool nat = ops.Any(o => o.NATLIT() is not null), bl = ops.Any(o => o.BOOLLIT() is not null);
            if ((nat || bl) && InStatement(ctx))
                _p.Check(nat ? Constructs.NationalData2002 : Constructs.BooleanData2002,
                    nat ? "the figurative ALL N\"…\"" : "the figurative ALL B\"…\"");
            // A concatenated literal-1 uses the & operator — the COBOL-2002 introduction (§8.8.3), position-blind.
            if (ops.Length > 1) _p.Check(Constructs.ConcatOperator2002, "a concatenation expression (the & operator) as ALL literal-1");
            bool malformedHex = false;
            foreach (var o in ops)
                if ((o.HEXLIT() ?? o.NATLIT() ?? o.BOOLLIT()) is { } t && CobolLiteral.HexGroupViolation(t.GetText()) is { } why)
                {
                    malformedHex = true;
                    _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.HexLiteralDigitGrouping.Code,
                        EditionSeverity.Error, DiagnosticCatalog.HexLiteralDigitGrouping.Id,
                        $"the literal {t.GetText()} {why}", "", "ISO §8.3.3"));
                }
            // §8.3.3.6.3 SR2 — literal-1 "shall be neither a figurative constant nor a zero-length literal" (a
            // malformed hexadecimal literal-1 decodes to nothing and is already reported above); the operands of a
            // concatenated literal-1 are of ONE class (§8.8.3.2 SR1).
            if (!malformedHex && ops.All(o => CobolLiteral.Decode(o.GetText()).Length == 0))
                _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.AllLiteralZeroLength.Code,
                    EditionSeverity.Error, DiagnosticCatalog.AllLiteralZeroLength.Id,
                    $"'{ctx.GetText()}': the literal-1 of an ALL figurative shall not be a zero-length literal (ISO §8.3.3.6.3 SR2)",
                    "", "ISO §8.3.3.6.3 SR2"));
            if (ops.Select(o => CobolLiteral.ClassOf(o.GetText())).Distinct().Count() > 1)
                _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.ConcatClassMismatch.Code,
                    EditionSeverity.Error, DiagnosticCatalog.ConcatClassMismatch.Id,
                    $"'{ctx.GetText()}': the operands of a concatenated ALL literal-1 shall be of the same class (ISO §8.8.3.2 SR1)",
                    "", "ISO §8.8.3.2 SR1"));
            return base.VisitChildren(ctx);
        }

        /// <summary>A concatenation expression — the <c>&amp;</c> operator joining literals (ISO §8.8.3) — is a
        /// COBOL-2002 introduction (concat-operator-2002; roadmap D6). POSITION-BLIND, unlike the national/
        /// boolean literal gate above: §8.8.3.3 GR3 lets a concat stand anywhere a literal may (VALUE clauses,
        /// SPECIAL-NAMES operands, statement operands, FUNCTION arguments), and no data-side gate covers the
        /// construct — recognition of the parse node IS the one funnel. One Check per concatenation
        /// expression; the &amp; token appears in no other rule, so recognition is exact. The §8.8.3.2 SR
        /// checks (same class / no ALL figurative / 8,191 cap) are the binder's ConcatFolder — edition-
        /// invariant semantics, not gates.</summary>
        public override object? VisitConcatenationExpression(CobolParserCore.ConcatenationExpressionContext ctx)
        {
            _p.Check(Constructs.ConcatOperator2002, "a concatenation expression (the & operator)");
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

        /// <summary>Whether a boolean-expression subtree contains any boolean SHIFT operator terminal (ISO §8.8.2,
        /// 2023). A DISTINCT construct/edition from HasBoolOp (2023 vs 2002) — a program using only shift operators
        /// at --std 2002 must get the shift's COBOLNET0900, not the boolean-operators-2002 message.</summary>
        private static bool HasShiftOp(Antlr4.Runtime.Tree.IParseTree t)
        {
            if (t is Antlr4.Runtime.Tree.ITerminalNode term)
                return term.Symbol.Type is CobolLexer.B_SHIFT_L or CobolLexer.B_SHIFT_R
                    or CobolLexer.B_SHIFT_LC or CobolLexer.B_SHIFT_RC;
            for (int i = 0; i < t.ChildCount; i++)
                if (HasShiftOp(t.GetChild(i))) return true;
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
            // The 2002 GROUP-USAGE clause word (ISO §13.18.29; D20/PB79): a user word at 85, funnel-0901'd at ≥2002.
            // Its keyword occurrence parses only through groupUsageClause, never a name slot — position-safe.
            CobolLexer.GROUP_USAGE,
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
            CobolLexer.CONSTANT,  // §13.10/§13.18.15 (2002+): keyword slots are the constantEntryBody head and `CONSTANT RECORD` — direct tokens, never a name slot — position-blind safe (P10 Step 15; the PROTOTYPE precedent)
            CobolLexer.AS,        // §13.10 (2002+): the sole keyword slot is the constantEntryBody `AS` — a direct token, never a name slot — position-blind safe (the CONSTANT twin)
            CobolLexer.PROGRAM_POINTER,  // §13.18.60 (2002+): the sole keyword slot is the programPointerUsage head — a direct token — position-blind safe (P10 Step 7)
            CobolLexer.FUNCTION_POINTER, // §13.18.60 (2014+): the sole keyword slot is the functionPointerUsage head — a direct token — position-blind safe (P10 Step 7)
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
        /// <summary>True when <paramref name="ctx"/> is the SOLE word of a bare (unsuffixed, operator-free)
        /// function argument — the shape a §15 phrase word occupies. The walk ascends the sole-child
        /// expression spine (primary → unary → power → multiplicative → additive → arithmeticExpression);
        /// any operator, suffix, or extra operand makes the word a provable operand.</summary>
        private static bool IsBareFunctionArgumentWord(CobolParserCore.CobolWordContext ctx)
        {
            if (ctx.Parent is not CobolParserCore.DataReferenceContext dref
                || dref.dataReferenceSuffix().Length != 0)
                return false;
            Antlr4.Runtime.RuleContext? n = dref.Parent;
            while (n is CobolParserCore.PrimaryExpressionContext or CobolParserCore.UnaryExpressionContext
                or CobolParserCore.PowerExpressionContext or CobolParserCore.MultiplicativeExpressionContext
                or CobolParserCore.AdditiveExpressionContext or CobolParserCore.ArithmeticExpressionContext)
            {
                if (n.ChildCount != 1) return false;   // an operator / second operand — provably an operand
                n = n.Parent;
            }
            return n is CobolParserCore.FunctionArgumentContext;
        }

        public override object? VisitCobolWord(CobolParserCore.CobolWordContext ctx)
        {
            // §8.3.2.1 word-length ceiling — a COBOL word is "not more than 63 characters" at COBOL-2023; the
            // limit was 31 in 2002/2014 and 30 in 1985 (E.3.3 item 11: a 2023 RELAXATION — the OPPOSITE direction
            // from a new-feature introduction gate, so it is NOT a 0900 construct). Fire below 2023 for 32–63-char
            // words, and at EVERY edition for >63 (a hard cap). Checked for every COBOL word regardless of role
            // (reserved words are all short — only user-defined words reach the limit); deduped per distinct word.
            string raw = ctx.Start.Text;
            int max = CobolWordRule.MaxLength(_p._edition.Year);   // ONE ceiling — the directive stages share it
            // §8.3.2.1's OTHER half — the word CHARACTER SET (fix-queue R02). The same sentence that caps the
            // length names the admissible characters: "…the basic special characters HYPHEN AND UNDERSCORE. The
            // hyphen or underscore shall not appear as the first or last character in such words." The
            // underscore is a COBOL-2002 introduction ('85's set is hyphen-only), and the lexer admits it at
            // every edition (superset-parse), so this is where a below-2002 use is named.
            // ⚠ Deduped per distinct word through the SAME set the length check uses: one diagnostic per word
            // per compilation, matching the §8.9 funnel's posture rather than one per occurrence.
            if (raw.Contains('_') && (_overlongWords ??= []).Add("_" + raw.ToUpperInvariant()))
                _p.Check(Constructs.UserWordUnderscore2002, $"the underscore in the COBOL word '{raw}'");
            if (raw.Length > max && (_overlongWords ??= []).Add(raw.ToUpperInvariant()))
                _p._sink.Report(new EditionDiagnostic(DiagnosticCatalog.WordLengthExceeded.Code,
                    EditionSeverity.Error, DiagnosticCatalog.WordLengthExceeded.Id,
                    CobolWordRule.LengthViolation(raw, _p._edition.Year)!, "", "ISO §8.3.2.1"));
            if (!CheckedTokenTypes.Contains(ctx.Start.Type) && !IsProvableUserWordPosition(ctx))
                return base.VisitChildren(ctx);
            string word = ctx.Start.Text.ToUpperInvariant();
            // A BARE word argument of a function reference may be a §15 PHRASE WORD (FIND-STRING ANYCASE,
            // CONVERT HEX/NAT/ANUM/BYTE, MODULE-NAME CURRENT/ACTIVATING/NESTED/STACK/TOP-LEVEL, …) — a use OF
            // the reserved word, not a user-defined-word use (§8.4.3.2 SR8 + the per-function §15 argument
            // rules; the EXCEPTION-OBJECT precedent below). The position is NOT provable: only the sole-word
            // shape skips — a subscripted/qualified/compound argument is provably an operand and stays
            // funneled, and a genuine data-item collision is still caught at its DECLARATION slot. (P7
            // Step 12: arguments parse as real trees, so phrase words reach cobolWord — the former SUB-token
            // capture never surfaced them here.)
            if (IsBareFunctionArgumentWord(ctx))
                return base.VisitChildren(ctx);
            // The ENTRY-CONVENTION value slot (§11.9.7 general format: ENTRY-CONVENTION IS {COBOL |
            // entry-convention-name}): COBOL there is the FORMAT'S OWN keyword alternative — grammar-matched
            // as cobolWord exactly so the funnel decides by context — and an implementor entry-convention-name
            // is a use of an implementor name, never a user-defined-word DEFINITION. Skip the funnel for the
            // slot (the EXCEPTION-OBJECT predefined-register precedent; P10 Step 12 — surfaced when the
            // OPTIONS paragraph began parsing at 2002 and the options-entry-convention-2014 matrix row's
            // 2014 cell hit the COBOL reservation).
            if (ctx.Parent is CobolParserCore.EntryConventionClauseContext)
                return base.VisitChildren(ctx);
            // The SPECIAL-NAMES LOCALE clause (§12.3.7 general format:
            // `LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 }`). LOCALE is not a lexer token, so
            // the clause's OWN KEYWORD is grammar-matched as a cobolWord — and the funnel was reading it as a
            // user-defined word, reporting `'LOCALE' is a reserved word … and cannot be used as a user-defined
            // word` about a program that uses it as nothing of the sort. The clause already diagnoses correctly
            // (then COBOLNET1518, the era's item-10 non-support; the clause BINDS since PB64 T1); the second diagnostic was a FALSE
            // statement about the source, printed beside a true one (fix-queue PB27).
            // ⚠ POSITION-EXACT, because the three cobolWord slots are three different KINDS of word:
            //   [0] the keyword LOCALE — a use OF the reserved word            → exempt
            //   [1] locale-name-1      — a genuine USER-DEFINED word           → stays funneled
            //   [2] external-locale-name-1 — an IMPLEMENTOR name whose allowable values §12.3.7 GR5 leaves to
            //       the implementor, never a user-defined-word definition      → exempt (ENTRY-CONVENTION's
            //       entry-convention-name is the same shape and the same precedent)
            // Exempting the whole clause would have hidden a real §8.9 violation in slot [1].
            // `CALL … AS NESTED` (§14.9.4.2 Format 2; fix-queue PB46 CALL half). The AS phrase is grammar-matched
            // as `AS cobolWord` because the brace's two arms are the RESERVED word NESTED and a
            // program-prototype-NAME, and only the binder can tell them apart. NESTED there is a use OF the
            // reserved word, not a user-defined word — the same shape as the LOCALE clause below.
            // ⚠ EXACTLY THE WORD NESTED, because the OTHER arm genuinely IS a user-defined word:
            // program-prototype-name-1 is declared in the REPOSITORY paragraph (§14.9.4.3 SR16), so a reserved
            // word written there is a real §8.9 violation and must keep reaching the funnel.
            if (ctx.Parent is CobolParserCore.CallAsPhraseContext
                && string.Equals(ctx.Start.Text, "NESTED", StringComparison.OrdinalIgnoreCase))
                return base.VisitChildren(ctx);
            if (ctx.Parent is CobolParserCore.LocaleClauseContext locale
                && locale.cobolWord() is { Length: > 1 } localeWords
                && !ReferenceEquals(ctx, localeWords[1]))
                return base.VisitChildren(ctx);
            // The SPECIAL-NAMES ORDER TABLE clause (§12.3.7.2 general format:
            // `ORDER TABLE ordering-name-1 IS literal-9`; kb/Work PB101). The PB27 shape again — ORDER is not a
            // lexer token, so the clause's OWN KEYWORD is grammar-matched as a cobolWord and the funnel would
            // report `'ORDER' is a reserved word … and cannot be used as a user-defined word` about a program
            // that uses it as nothing of the sort (ORDER is reserved from 2002, reserved-words.json r2002).
            // ⚠ POSITION-EXACT, because the two cobolWord slots are two different KINDS of word:
            //   [0] the keyword ORDER   — a use OF the reserved word          → exempt
            //   [1] ordering-name-1     — a genuine USER-DEFINED word (§12.3.7.3 SR9 lets it be referenced only
            //       in STANDARD-COMPARE, but it is DECLARED here)             → stays funneled
            if (ctx.Parent is CobolParserCore.OrderTableClauseContext order
                && order.cobolWord() is { Length: > 0 } orderWords
                && ReferenceEquals(ctx, orderWords[0]))
                return base.VisitChildren(ctx);
            // The ALPHABET clause's `IS LOCALE [locale-name-2]` phrase (§12.3.7.2; kb/Work PB100): LOCALE is the phrase's
            // own keyword (not a lexer token — it arrives as the first definition entry's cobolWord) and locale-name-2 is a
            // REFERENCE to a SPECIAL-NAMES locale-name (LIVE since PB64 T1/T3 — the named IS LOCALE alphabet). The PB27 shape.
            if (ctx.Parent is CobolParserCore.AlphabetEntryContext ae
                && ae.Parent is CobolParserCore.AlphabetDefinitionContext adef
                && CobolNet.Binding.DataBinder.IsAlphabetLocalePhrase(adef))
                return base.VisitChildren(ctx);
            // PICTURE Format 2's `LOCALE [IS locale-name-1] SIZE IS integer-1` (§13.18.40.2; kb/Work PB100, LIVE
            // since PB64 T6): LOCALE is the phrase's own keyword (a use OF the reserved word — exempt) and
            // locale-name-1 a REFERENCE to a SPECIAL-NAMES locale-name (reference positions stay unchecked by
            // the funnel's policy; SR37 is the binder's COBOLNET1664).
            if (ctx.Parent is CobolParserCore.PictureLocalePhraseContext)
                return base.VisitChildren(ctx);
            // The OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (§12.3.6.2; kb/Work PB78) — the same shape as the
            // LOCALE clause: CLASSIFICATION is the clause's own keyword (not a lexer token), and each locale-phrase is
            // either a format keyword (LOCALE / SYSTEM-DEFAULT / USER-DEFAULT — uses OF reserved words) or a
            // REFERENCE to a SPECIAL-NAMES locale-name (reference positions stay unchecked by the funnel's policy).
            // The clause BINDS since PB64 T5 (A.4.9 item 7 claimed); a second, false statement
            // about SYSTEM-DEFAULT being "used as a user-defined word" is exactly the PB27 shape.
            if (ctx.Parent is CobolParserCore.CharacterClassificationClauseContext or CobolParserCore.ClassificationForPhraseContext)
                return base.VisitChildren(ctx);
            // SET LOCALE … / SET … TO LOCALE … (§14.9.39 Formats 11/12; kb/Work PB92, implemented PB64 T1): the cobolWords
            // are the format's own keywords (LOCALE, the locale categories, LC_ALL, USER-DEFAULT) — uses OF reserved
            // words (§8.9 context-sensitive: the LC_ words are NOT reserved, they are recognized by text in this
            // statement only). Its TO operand is a dataReference whose word may itself be one of the format's keywords
            // (USER-DEFAULT / SYSTEM-DEFAULT) — an IDENTIFIER token is checked position-blind, so the whole statement's
            // subtree is exempt (an ancestor walk, the EXCEPTION-OBJECT shape).
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.SetLocaleStatementContext)
                    return base.VisitChildren(ctx);
            // Inside a DECLINED Annex-A.4 construct (Grammar/Core/CobolDeclined.g4) — the PB27/PB100 shape, and
            // the sharpest instance of it, because the whole construct is being REFUSED by name in the same
            // compile. §13.18.62.2's ON group names FORMAT / CONTENT / RELATION, which are the clause's OWN
            // keywords: FORMAT is §8.9-reserved from 2002 but has no lexer token and RELATION is §8.10
            // context-sensitive, so both arrive as cobolWord and the funnel printed "'FORMAT' is a reserved
            // word … and cannot be used as a user-defined word" beside COBOLNET1708 — a FALSE statement about
            // the source, printed next to the true one. §4.2.6 does not require diagnosing syntax WITHIN
            // unsupported syntax, and one diagnostic per declined construct is the posture DeclinedFacilityPass
            // itself takes (it does not descend either). Whole-subtree, by ancestor walk: an IDENTIFIER token is
            // checked position-blind, so a per-slot exemption would leave the next declined clause's keywords
            // exposed (feedback_two_arm_dispatch).
            for (Antlr4.Runtime.RuleContext? a = ctx.Parent; a is not null; a = a.Parent)
                if (a is CobolParserCore.ValidationClauseContext
                    or CobolParserCore.ValidateValidPhraseContext
                    or CobolParserCore.ApplyCommitClauseContext)
                    return base.VisitChildren(ctx);
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
            // Under WITH DEBUGGING MODE a DEBUG-* occurrence is the X3.23-1985 REGISTER (DEBUG-ITEM family), not a
            // user-defined word — a legal '85 reference to the now-modeled debug facility (VCR Table 7 row 7.17;
            // the switch-ABSENT case never gets here — comment treatment skips the section body). The binder
            // resolves it to a DebugRegisterPlace, so ACCEPT it here (no §8.9 violation, no not-implemented note).
            if (_debuggingModeDeclared && word.StartsWith("DEBUG-", StringComparison.Ordinal))
                return base.VisitChildren(ctx);
            if (_reservedWords.RejectsAt(word, _p._edition.Year) && (_flaggedWords ??= []).Add(word))
            {
                // The §8.9 reserved-word severity routes through the ONE policy — and the VERDICT is COMPUTED
                // from the word's own reservation interval, never asserted here. Hard-coding `Removed` made
                // --permissive accept a RE-RESERVED word (RECEIVE, END-RECEIVE) as a user-defined word at
                // COBOL-85, where the '85 communication module reserves it and no conforming '85 program could
                // contain one — a not-yet-introduced construct getting the migration mode's leniency (CA14's
                // policy; these two were found by the introduction-axis theory the fix added).
                _p._sink.Report(new EditionDiagnostic(EditionCodes.ReservedWord,
                    EditionSeverityPolicy.For(_reservedWords.UserWordVerdictAt(word, _p._edition.Year), _p._edition),
                    "edition-reserved-word",
                    $"'{word}' is a reserved word in COBOL-{_p._edition.Year} and cannot be used as a "
                    + "user-defined word (ISO §8.9)", "", "ISO §8.9"));
            }
            return base.VisitChildren(ctx);
        }

        /// <summary>kb/Work PB137: dataName's reservation-gated direct-token alternatives (COMMIT/ROLLBACK)
        /// keep DECLARATIONS parseable at 2023 precisely so this funnel can NAME the reserved word — they
        /// bypass cobolWord entirely, so the §8.9 check must meet them here with the SAME one-per-word
        /// report the cobolWord walk emits.</summary>
        public override object? VisitDataName(CobolParserCore.DataNameContext ctx)
        {
            string? word = ctx.COMMIT() is not null ? "COMMIT" : ctx.ROLLBACK() is not null ? "ROLLBACK" : null;
            if (word is not null && _reservedWords.RejectsAt(word, _p._edition.Year) && (_flaggedWords ??= []).Add(word))
                _p._sink.Report(new EditionDiagnostic(EditionCodes.ReservedWord,
                    EditionSeverityPolicy.For(_reservedWords.UserWordVerdictAt(word, _p._edition.Year), _p._edition),
                    "edition-reserved-word",
                    $"'{word}' is a reserved word in COBOL-{_p._edition.Year} and cannot be used as a "
                    + "user-defined word (ISO §8.9)", "", "ISO §8.9"));
            return base.VisitChildren(ctx);
        }
    }
}
