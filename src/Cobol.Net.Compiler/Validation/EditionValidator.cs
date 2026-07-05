// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Validation;

/// <summary>
/// The per-edition VALIDATION pass (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.2) — the
/// syntax-side half of the four-compilers-in-one obligation: every construct carries (1) its full ISO behavior in
/// every edition that HAS it and (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced,
/// COBOLNET0900), reserves its spelling (0901), removed it (0902), or obsoleted it (0903 — see
/// <see cref="EditionCodes"/>). The validator walks the RAW parse tree — syntax-only gating lives here; gating
/// that needs bind/type information (e.g. the MOVE rows) stays binder-side — but EVERY severity decision routes
/// through <see cref="EditionContext.Removed"/> / the construct registry: one policy, several emit sites.
/// </summary>
/// <remarks>
/// The walk derives from the generated <see cref="CobolParserCoreBaseVisitor{Result}"/> (ANTLR runs
/// <c>-no-listener -visitor</c>, so no listener exists to attach to); overrides MUST return
/// <c>base.VisitChildren(ctx)</c> (or <c>base.VisitXxx(ctx)</c>) to keep descending. Hooked by
/// <see cref="CompilerDriver.Compile"/> between <see cref="EditionContext"/> construction and
/// <c>CSharpEmitter.Emit</c>, with a fail-fast on <see cref="EditionContext.HasErrors"/> BEFORE Emit — a
/// removed or not-yet-introduced construct may have no emit path at all. Validator diagnostics ride the SAME
/// <see cref="EditionContext"/> channels as binder gating (no separate outcome kind).
/// The Wave-1 construct gates (P2.6) and the §8.9 reserved-word funnel (P2.4 — <c>VisitCobolWord</c>) land on
/// this skeleton in their own change sets, each with its VERSION_CHANGE_REFERENCE row and ISO § citation.
/// </remarks>
public sealed class EditionValidator(EditionContext edition) : CobolParserCoreBaseVisitor<object?>
{
    private readonly EditionContext _edition = edition;
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

    /// <summary>Run the pass over a parsed compilation unit, recording diagnostics on the
    /// <see cref="EditionContext"/> passed at construction.</summary>
    public void Validate(CobolParserCore.CompilationUnitContext tree) => Visit(tree);

    // ── P2.6 removal gates: every override routes through ConstructRegistry.Check (one policy) ─────────────

    /// <summary>LABEL RECORDS (FD) — obsolete '85 element DELETED by ISO/IEC 1989:2002; the 2023 FD clause set
    /// (§13.18) has no LABEL clause. The FIRST removal gate, shipped in the SAME commit as the permissive flip
    /// (every NIST FD writes this clause — 243/459 programs).</summary>
    public override object? VisitLabelRecordsClause(CobolParserCore.LabelRecordsClauseContext ctx)
    {
        ConstructRegistry.Check(_edition, "label-records-removed-2002", "the FD LABEL RECORDS clause");
        return base.VisitChildren(ctx);
    }

    /// <summary>VALUE OF (FD) — obsolete '85 label-field clause, deleted 2002 (P2.6).</summary>
    public override object? VisitValueOfClause(CobolParserCore.ValueOfClauseContext ctx)
    {
        ConstructRegistry.Check(_edition, "value-of-removed-2002", "the FD VALUE OF clause");
        return base.VisitChildren(ctx);
    }

    /// <summary>DATA RECORDS (FD **and** SD — one grammar rule, so ONE enforcement site; the former
    /// DataBinder SD-only 0873 gate MIGRATED here, P2.6/Table-7 row 7.1 follow-up). Keeps its pinned 0873.</summary>
    public override object? VisitDataRecordsClause(CobolParserCore.DataRecordsClauseContext ctx)
    {
        ConstructRegistry.Check(_edition, "data-records-removed-2002", "the FD/SD DATA RECORDS clause");
        return base.VisitChildren(ctx);
    }

    /// <summary>MULTIPLE FILE [TAPE] (I-O-CONTROL) — reel-sharing description, deleted 2002 (P2.6).</summary>
    public override object? VisitMultipleFileClause(CobolParserCore.MultipleFileClauseContext ctx)
    {
        ConstructRegistry.Check(_edition, "multiple-file-tape-removed-2002", "the I-O-CONTROL MULTIPLE FILE clause");
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
                    ConstructRegistry.Check(_edition, "memory-size-removed-2002", "the OBJECT-COMPUTER MEMORY SIZE clause");
                    break;
                case "SEGMENT-LIMIT":
                    ConstructRegistry.Check(_edition, "segment-limit-removed-2002", "the OBJECT-COMPUTER SEGMENT-LIMIT clause");
                    break;
                case "DEBUGGING":
                    ConstructRegistry.Check(_edition, "debugging-mode-removed-2002", "the SOURCE-COMPUTER WITH DEBUGGING MODE clause");
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
    { ConstructRegistry.Check(_edition, "identification-comments-removed-2002", "the AUTHOR paragraph"); return base.VisitChildren(ctx); }
    public override object? VisitInstallationParagraph(CobolParserCore.InstallationParagraphContext ctx)
    { ConstructRegistry.Check(_edition, "identification-comments-removed-2002", "the INSTALLATION paragraph"); return base.VisitChildren(ctx); }
    public override object? VisitDateWrittenParagraph(CobolParserCore.DateWrittenParagraphContext ctx)
    { ConstructRegistry.Check(_edition, "identification-comments-removed-2002", "the DATE-WRITTEN paragraph"); return base.VisitChildren(ctx); }
    public override object? VisitDateCompiledParagraph(CobolParserCore.DateCompiledParagraphContext ctx)
    { ConstructRegistry.Check(_edition, "identification-comments-removed-2002", "the DATE-COMPILED paragraph"); return base.VisitChildren(ctx); }
    public override object? VisitSecurityParagraph(CobolParserCore.SecurityParagraphContext ctx)
    { ConstructRegistry.Check(_edition, "identification-comments-removed-2002", "the SECURITY paragraph"); return base.VisitChildren(ctx); }

    /// <summary>REMARKS — a '74 carryover the grammar accepts for CCVS; flagged ≥2002 ONLY (never at 85 —
    /// CCVS-85 programs write it; the 85 FIPS flagger is future strictness work, P2.6).</summary>
    public override object? VisitRemarksParagraph(CobolParserCore.RemarksParagraphContext ctx)
    { ConstructRegistry.Check(_edition, "remarks-removed-2002", "the REMARKS paragraph"); return base.VisitChildren(ctx); }

    /// <summary>STOP literal (Format 2) — obsolete '85 element deleted 2002 (P2.6). Its 85 SEMANTICS are
    /// implemented binder-side this same change set (the silent bind-as-STOP-RUN mis-bind fixed).</summary>
    public override object? VisitStopStatement(CobolParserCore.StopStatementContext ctx)
    {
        if (ctx.literal() is not null)
            ConstructRegistry.Check(_edition, "stop-literal-removed-2002", "the STOP literal statement");
        return base.VisitChildren(ctx);
    }

    /// <summary>OPEN … REVERSED — obsolete '85 tape phrase deleted 2002 (P2.6; NO REWIND stays — it survives
    /// into 2023 §14.9.26).</summary>
    public override object? VisitOpenFileSpec(CobolParserCore.OpenFileSpecContext ctx)
    {
        if (ctx.REVERSED() is not null)
            ConstructRegistry.Check(_edition, "open-reversed-removed-2002", "the OPEN REVERSED phrase");
        return base.VisitChildren(ctx);
    }

    /// <summary>CLOSE … WITH LOCK — REMOVED 2014→2023 (Annex E deletion; VCR row 7; P2.6).</summary>
    public override object? VisitCloseOption(CobolParserCore.CloseOptionContext ctx)
    {
        if (ctx.LOCK() is not null)
            ConstructRegistry.Check(_edition, "close-with-lock-removed-2023", "the CLOSE WITH LOCK phrase");
        return base.VisitChildren(ctx);
    }

    /// <summary>EXIT METHOD / EXIT FUNCTION — introduced 2002 (0900 below), REMOVED 2023 (Annex E; VCR rows
    /// 5/6) — the dual-obligation window rows; EXIT PROGRAM — ARCHAIC in 2023 (0903 warning; VCR 89).</summary>
    public override object? VisitExitStatement(CobolParserCore.ExitStatementContext ctx)
    {
        if (ctx.METHOD() is not null)
            ConstructRegistry.Check(_edition, "exit-method-window", "the EXIT METHOD statement");
        else if (ctx.FUNCTION() is not null)
            ConstructRegistry.Check(_edition, "exit-function-window", "the EXIT FUNCTION statement");
        else if (ctx.PROGRAM() is not null)
            ConstructRegistry.Check(_edition, "exit-program-archaic-2023", "the EXIT PROGRAM statement");
        return base.VisitChildren(ctx);
    }

    /// <summary>NEXT SENTENCE — ARCHAIC in 2023 (0903 warning; VCR 90; P2.6).</summary>
    public override object? VisitNextSentenceStatement(CobolParserCore.NextSentenceStatementContext ctx)
    {
        ConstructRegistry.Check(_edition, "next-sentence-archaic-2023", "the NEXT SENTENCE phrase");
        return base.VisitChildren(ctx);
    }

    /// <summary>A WORKING-STORAGE SECTION in a METHOD definition — legal 2002/2014 (D3: static-field
    /// semantics, shared across instances and persistent across activations, §11.7), BANNED by 2023
    /// (§13.5.3 SR 1: within a class definition WS may appear only in a factory or instance definition,
    /// "but not in a method definition" — OO deep-dive Spec correction #1). The dual window: 0900 below
    /// 2002, 0902 at 2023, silent between; under <c>--permissive</c> the pre-removal static semantics
    /// stand (the §10 #1 migration contract).</summary>
    public override object? VisitMethodDefinition(CobolParserCore.MethodDefinitionContext ctx)
    {
        if (ctx.dataDivision()?.workingStorageSection() is not null)
            ConstructRegistry.Check(_edition, "method-working-storage-window",
                "a WORKING-STORAGE SECTION in a method definition");
        return base.VisitChildren(ctx);
    }

    // ── The W3 notInGrammar 85-acceptance gates (VCR Table 7 rows 7.15–7.18; DEVLOG 599): four obsolete '85
    //    elements DELETED by ISO 2002 that formerly had no grammar at all (generic parse errors at EVERY
    //    edition — the G1 co-equal-diagnostic violation). Each now parses unconditionally, binds inert at 85,
    //    and gates here. ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>RERUN (I-O-CONTROL) — the '85 checkpoint hint, deleted 2002 (RERUN is absent from the whole
    /// 2023 text, §8.9 @10661–10662 included). Parsed-and-ignored at 85 (a null rerun facility is conforming).</summary>
    public override object? VisitRerunClause(CobolParserCore.RerunClauseContext ctx)
    {
        ConstructRegistry.Check(_edition, "rerun-removed-2002", "the I-O-CONTROL RERUN clause");
        return base.VisitChildren(ctx);
    }

    /// <summary>ENTER — the '85 other-language entry statement, deleted 2002 (absent from the 2023 text, §8.9
    /// @10459–10460 included). Comment-equivalent (BoundNop) at 85 — the conforming COBOL-only posture.</summary>
    public override object? VisitEnterStatement(CobolParserCore.EnterStatementContext ctx)
    {
        ConstructRegistry.Check(_edition, "enter-removed-2002", "the ENTER statement");
        return base.VisitChildren(ctx);
    }

    /// <summary>USE FOR DEBUGGING — the '85 debug facility's declarative, deleted 2002 with the whole facility
    /// (the DEBUG-* register family included; §8.9 @10407–10408). Sub-shape gate on the one useStatement rule
    /// (the STOP-literal pattern); the WITH DEBUGGING MODE companion is row 7.9's gate above.</summary>
    public override object? VisitUseStatement(CobolParserCore.UseStatementContext ctx)
    {
        if (ctx.DEBUGGING() is not null)
            ConstructRegistry.Check(_edition, "use-for-debugging-removed-2002", "the USE FOR DEBUGGING declarative");
        return base.VisitChildren(ctx);
    }

    /// <summary>A section-header segment-number — the '85 Segmentation module's priority number, deleted 2002
    /// (the word 'segment' is absent from the 2023 text; the SEGMENT-LIMIT companion is row 7.8). Parsed and
    /// ignored at 85 (all segments resident — a conforming posture).</summary>
    public override object? VisitSectionDefinition(CobolParserCore.SectionDefinitionContext ctx)
    {
        if (ctx.integerLiteral() is not null)
            ConstructRegistry.Check(_edition, "segment-numbers-removed-2002",
                $"the segment-number on section '{ctx.sectionName().GetText()}'");
        return base.VisitChildren(ctx);
    }

    /// <summary>The declarative-section twin of <see cref="VisitSectionDefinition"/> (one registry row, the
    /// site named per header — the identification-comments pattern). ALSO the '85 comment-treatment seam
    /// (row 7.17): without WITH DEBUGGING MODE, X3.23-1985 compiles a USE FOR DEBUGGING section as if it
    /// were comment lines — so the walk visits ONLY the USE statement (its ≥2002 0902 gate must still fire;
    /// the construct is present in source) and never the section body, keeping the §8.9 funnel off the
    /// unimplemented DEBUG-* register references inside (DB103M is the corpus witness: no switch, 95
    /// register references, designed to run with the sections inert). The binder twin is
    /// StatementBinder.DeclCollectSection.</summary>
    public override object? VisitDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx)
    {
        if (ctx.integerLiteral() is not null)
            ConstructRegistry.Check(_edition, "segment-numbers-removed-2002",
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
        // user words below 2023; their keyword occurrences parse through the {is2023()}?-gated operator
        // alternative, never a name slot — so, like the EC band, they are position-safe to check everywhere.
        CobolLexer.XOR, CobolLexer.EXCLUSIVE_OR,
        // The X3.23-1985 notInGrammar 85-acceptance words (VCR Table 7 rows 7.15–7.18): '85-reserved, user
        // words at later editions per the §8.9 table (RERUN/ENTER free ≥2002, DEBUGGING ≥2014, the rest
        // ≥2023). Their keyword occurrences parse through dedicated rules (rerunClause / enterStatement —
        // whose operands are deliberately NOT cobolWord / the USE FOR DEBUGGING format), never a name slot —
        // position-safe to check everywhere.
        CobolLexer.FACTORY,   // §11.4 (2002+): keyword slots are factoryParagraph/END FACTORY/FACTORY OF only — position-blind safe (the EC-band argument)
        CobolLexer.OVERRIDE,  // §11.7 (2002+): the METHOD-ID attribute slot is a direct token — position-blind safe
        CobolLexer.GET, CobolLexer.PROPERTY, CobolLexer.INTERFACE,   // §11.6/§11.7/§13.18.42 (2002+): keyword slots are direct tokens (selector/clause/repository/END INTERFACE) — position-blind safe. IMPLEMENTS is §8.10 context-sensitive: NEVER here.
        CobolLexer.RERUN, CobolLexer.ENTER, CobolLexer.EVERY, CobolLexer.CLOCK_UNITS,
        CobolLexer.DEBUGGING, CobolLexer.REFERENCES, CobolLexer.PROCEDURES,
    ];

    /// <summary>
    /// The §8.9 reserved-word funnel (P2.4): every user-defined word reaches the tree through the
    /// <c>cobolWord</c> rule — IDENTIFIER plus the allowlisted context-keyword tokens — so ONE text-based check
    /// here covers 2023-new words that lex as IDENTIFIER (COMMIT, FINALLY, …) AND the EC-band tokens the 2023
    /// edition reserves (RAISE/RAISING/RESUME/CONDITION/EC). The grammar stays a permissive superset ("legal
    /// user word at every edition"); the VALIDATOR enforces per edition — position-blind for
    /// <see cref="CheckedTokenTypes"/>, position-AWARE for every other allowlisted context keyword
    /// (<see cref="IsProvableUserWordPosition"/> — the P2.8 W2 subset; see the note on the set). Only
    /// high-confidence rows reject (<see cref="ReservedWordSet.RejectsAt"/> — the conservative policy);
    /// severity routes through <see cref="EditionContext.Removed"/> (error strict / warning permissive, the
    /// 0901 band row).
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
        if (_reservedWords.RejectsAt(word, _edition.DialectLevel) && (_flaggedWords ??= []).Add(word))
        {
            // Under WITH DEBUGGING MODE a DEBUG-* occurrence is the X3.23-1985 REGISTER (DEBUG-ITEM family),
            // not a user-defined word — a legal '85 reference to a facility this compiler defers (VCR Table 7
            // row 7.17; the switch-ABSENT case never gets here — comment treatment skips the section body).
            // Diagnose the truth (0899 not-implemented) instead of a false §8.9 violation.
            if (_debuggingModeDeclared && word.StartsWith("DEBUG-", StringComparison.Ordinal))
                _edition.Error("COBOLNET0899",
                    $"the X3.23-1985 debug register '{word}' is recognized, but the '85 debug facility "
                    + "(DEBUG-ITEM registers, debugging-section invocation) is not implemented — deferred "
                    + "with the golden-less DB series (VCR Table 7 row 7.17)");
            else
                _edition.Removed(EditionCodes.ReservedWord,
                    $"'{word}' is a reserved word in COBOL-{_edition.DialectLevel} and cannot be used as a "
                    + "user-defined word (ISO §8.9)");
        }
        return base.VisitChildren(ctx);
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
    /// the funnel's no-false-reject guarantee (VCR scope-limit rule).
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
}
