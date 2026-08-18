// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;

namespace CobolNet.Editions.Diagnostics;

/// <summary>
/// The first-class compiler-diagnostic catalogue (rearch PHASE 02, P2.10). ONE home, below both the frontend and
/// the compiler, for the diagnostics that used to be bare <c>COBOLNETnnnn</c> string literals: the edition band
/// (single-sourced from <see cref="EditionCodes"/>), the digit-capacity codes, the reused <c>COBOLNET1533</c>
/// strong-type rules split by ISO §, and — the bulk — the <c>COBOLNET0899</c> "recognized but not implemented"
/// catch-all, split into one addressable descriptor per deferred feature / per validation rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>The 0899 split (fixes P8).</b> <c>COBOLNET0899</c> was emitted from ~44 sites conflating two categories:
/// (a) LEGAL-but-not-yet-implemented features (national data, several Report Writer clauses, OO refinements) and
/// (b) genuine SEMANTIC VALIDATION errors that happen to share the code (an unresolved Report Writer CONTROL /
/// SOURCE / SUM operand, a receiving-side LINE-COUNTER, …). Every site now references a descriptor here. The
/// emitted CODE stays <c>COBOLNET0899</c> (byte-stable — goldens and the differential net pin the text), but each
/// site is now addressable by a stable <see cref="DiagnosticDescriptor.Id"/>. The DEFERRALS carry the shared
/// <see cref="RecognizedNotImplemented"/> suppress family so a developer can mute all "not implemented yet"
/// diagnostics at once WITHOUT also muting the validation errors; the validation descriptors keep the default
/// (suppress-by-code) family. Reclassifying the validation errors onto proper per-rule codes (out of the 0899
/// bucket) is a tracked follow-on (P3 validator work) — it changes emitted codes, so it re-baselines goldens
/// under review and is out of P2's byte-stable scope.
/// </para>
/// <para>
/// <b>Reused-code disambiguation.</b> <c>COBOLNET1533</c> is emitted for THREE distinct strong-type rules
/// (§14.9.25.3 SR2 MOVE, §8.8.4.4.3 SR1 class condition, §8.8.4.2.3 SR1 comparison). Each now has its own
/// descriptor / <see cref="DiagnosticDescriptor.Id"/> while keeping the shared emitted code.
/// </para>
/// <para>
/// <b>Scope.</b> P2 catalogues the edition band + the 0899 split + the 1533 reuse — the P8-named problems. The
/// broader "every one of the ~163 compiler codes → a descriptor + a <c>sink.Report</c>" migration (and unifying
/// the frontend's parse-layer descriptor catalogue + its three-value <c>DiagnosticSeverity</c> down into this
/// home) is the P7 follow-on. <see cref="All"/> reflects the public descriptor fields, so a new descriptor is
/// picked up by <c>docs/DIAGNOSTICS.md</c> generation and the drift test without a hand-maintained list.
/// </para>
/// </remarks>
public static class DiagnosticCatalog
{
    /// <summary>The shared emitted code for the "recognized but not implemented" bucket (kept byte-stable).</summary>
    private const string NotImplemented = "COBOLNET0899";

    /// <summary>The shared emitted code for the strong-type rules (§8.5 strong typing; reused across 3 rules).</summary>
    private const string StrongType = "COBOLNET1533";

    /// <summary>The <c>--suppress</c> family grouping every LEGAL-but-deferred-feature diagnostic (the honest
    /// half of the old 0899 catch-all) so it can be muted as a group during development.</summary>
    public const string RecognizedNotImplemented = "recognized-not-implemented";

    // ── Edition band (single-sourced from EditionCodes) ──────────────────────────────────────────────
    public static readonly DiagnosticDescriptor EditionIntroduction = new(
        EditionCodes.Introduction, "edition-introduction", EditionSeverity.Error,
        "A construct is used below the edition that introduced it (requires a newer --std).", "ISO §ann. per construct");
    public static readonly DiagnosticDescriptor EditionReservedWord = new(
        EditionCodes.ReservedWord, "edition-reserved-word", EditionSeverity.Error,
        "A word reserved in the targeted edition is used as a user-defined word.", "ISO §8.9");
    public static readonly DiagnosticDescriptor EditionRemovedConstruct = new(
        EditionCodes.RemovedConstruct, "edition-removed-construct", EditionSeverity.Error,
        "A construct removed by the targeted edition is used (error strict / warning permissive).", "ISO ann. E.2");
    public static readonly DiagnosticDescriptor EditionObsoleteFlag = new(
        EditionCodes.ObsoleteFlag, "edition-obsolete-flag", EditionSeverity.Warning,
        "An obsolete/archaic element is used (still conforming; flagged).", "ISO §4.2.12/§4.2.13, ann. F.2");

    // ── Digit-capacity band (emitted by EditionContext.CheckDigitCapacity) ───────────────────────────
    public static readonly DiagnosticDescriptor DigitCapacityOver31 = new(
        "COBOLNET0801", "digit-capacity-over-31", EditionSeverity.Error,
        "A fixed-point item/literal exceeds the 31-digit ISO limit.", "ISO §8.3.1.2");
    // ── The §8.3.2.1 word-length ceiling — ONE rule (CobolWordRule), reported from the tree-walk funnel
    //    (VersionConformancePass.VisitCobolWord) AND the directive stages (>>TURN operands, >>DEFINE names),
    //    which never reach the tree walk (kb/Work R05's sweep). ─────────────────────────────────────────
    public static readonly DiagnosticDescriptor WordLengthExceeded = new(
        "COBOLNET1567", "word-length-exceeded", EditionSeverity.Error,
        "A COBOL word exceeds the edition's length ceiling: 63 characters at COBOL-2023 (Annex E.3.3 item 11 — "
        + "a relaxation, so a 32..63-character word below 2023 is a length error, not an introduction gate), "
        + "31 at 2002/2014, 30 at 1985.", "ISO §8.3.2.1");
    public static readonly DiagnosticDescriptor DigitCapacityOver18Pre2002 = new(
        "COBOLNET0802", "digit-capacity-over-18-pre-2002", EditionSeverity.Error,
        "A fixed-point item/literal exceeds the 18-digit COBOL-85 limit (19–31 need --std 2002+).", "ISO §8.3.1.2");

    // ── Compiler-directing facility band (emitted by the frontend directive processors). Registered by the
    //    P13 plan-vs-spec remediation (review finding C1's recurrence guard): every emitted code must be a
    //    catalog descriptor so the next-free allocation scan and DIAGNOSTICS.md see it — these four were bare
    //    frontend literals, the same channel that shipped the COBOLNET1573 collision. ─────────────────────────
    public static readonly DiagnosticDescriptor TurnDirectiveMalformed = new(
        "COBOLNET0718", "turn-directive-malformed", EditionSeverity.Error,
        "A >>TURN directive is malformed: the format is '>>TURN {exception-name [file-name]…}… CHECKING "
        + "{ON [WITH LOCATION] | OFF}' — an unexpected word, a missing CHECKING phrase, or a repeated "
        + "exception-name/file-name combination is rejected (ISO §7.3.25.2 / §7.3.25.3 SR1, SR3).",
        "ISO §7.3.25.2 / §7.3.25.3 SR1/SR3");
    public static readonly DiagnosticDescriptor TurnFileNameNonIo = new(
        "COBOLNET0719", "turn-file-name-non-io", EditionSeverity.Error,
        "A >>TURN file-name may follow only an exception-name beginning 'EC-I-O' (ISO §7.3.25.3 SR4).",
        "ISO §7.3.25.3 SR4");
    public static readonly DiagnosticDescriptor TurnDirectiveBelow2002 = new(
        "COBOLNET0875", "turn-directive-below-2002", EditionSeverity.Error,
        ">>TURN is the COBOL-2002+ exception-condition checking directive — it requires --std 2002 or later "
        + "(ISO §7.3.25).", "ISO §7.3.25");
    // ── Written exception-name resolution — ONE funnel (EcNameResolution; kb/Work R05). The unknown-name and
    //    introduction-gate texts existed as four verbatim copies each before the funnel. ────────────────────────
    public static readonly DiagnosticDescriptor EcNameUnknown = new(
        "COBOLNET0711", "ec-name-unknown", EditionSeverity.Error,
        "A written exception-name is neither in the §14.6.13.1 catalog nor a valid EC-USER-/EC-IMP- open-family "
        + "name (suffix of basic letters/digits/hyphen/underscore, not ending in hyphen or underscore).",
        "ISO §14.6.13.1.1");
    public static readonly DiagnosticDescriptor EcNameIntroducedLater = new(
        "COBOLNET0878", "ec-name-introduced-later", EditionSeverity.Error,
        "An exception-name belongs to a family introduced by a later edition than the targeted one (the "
        + "2023-only families — VERSION_CHANGE_REFERENCE rows 40/61).", "ISO §14.6.13.1");
    public static readonly DiagnosticDescriptor EcNameWiderThanStatus = new(
        "COBOLNET1636", "ec-name-wider-than-exception-status", EditionSeverity.Warning,
        "A level-3 exception-name is longer than the 31-character value FUNCTION EXCEPTION-STATUS returns "
        + "(§15.33.3 r1 fixes the width while COBOL-2023 words run to 63 characters, §8.3.2.1, and the "
        + "§14.6.13.1.1 open-family suffixes are unbounded) — this name and any other sharing its first 31 "
        + "characters are indistinguishable through that one function. Checking, declarative selection, and "
        + "WHEN matching use the full name. See COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN §15.33.",
        "ISO §15.33.3 r1 / §8.3.2.1 / §14.6.13.1.1");
    public static readonly DiagnosticDescriptor PropagateDirective = new(
        "COBOLNET0883", "propagate-directive", EditionSeverity.Error,
        "The >>PROPAGATE directive's compile-time diagnostics (ISO §7.3.21): below --std 2002 the directive is "
        + "rejected (the introduction gate); at 2002+ an operand other than ON or OFF is rejected, never "
        + "silently accepted (§7.3.21.2).", "ISO §7.3.21 / §7.3.21.2");

    // ── COBOLNET1540/1541/1545 — concatenation expressions, one code per rule (§8.8.3) ───────────────
    public static readonly DiagnosticDescriptor ConcatClassMismatch = new(
        "COBOLNET1540", "concat-class-mismatch", EditionSeverity.Error,
        "Both operands of a concatenation expression shall be of the same class — alphanumeric, boolean, or "
        + "national (a figurative constant takes the other operand's class).", "ISO §8.8.3.2 SR1");
    public static readonly DiagnosticDescriptor ConcatAllFigurative = new(
        "COBOLNET1541", "concat-all-figurative", EditionSeverity.Error,
        "Neither operand of a concatenation expression shall be a figurative constant that begins with the "
        + "word ALL.", "ISO §8.8.3.2 SR1");
    public static readonly DiagnosticDescriptor ConcatResultTooLong = new(
        "COBOLNET1545", "concat-result-too-long", EditionSeverity.Error,
        "The value resulting from concatenation shall be at most 8,191 character positions (alphanumeric, "
        + "boolean, or national).", "ISO §8.8.3.2 SR2–SR4");

    // ── COBOLNET1547/1548/1549 — constant entries + CONSTANT RECORD, one code per rule family (§13.10 /
    //    §13.18.15; P10 Step 15). 1547 = the §13.10 constant-entry syntax rules; 1548 = the receiving-operand
    //    rejection (a constant substitutes a LITERAL — §13.10.3 SR2/GR1 — and §13.18.15.3 SR2 forbids storing
    //    into a structured constant); 1549 = the CONSTANT RECORD structural rules (§13.18.15.3 SR1 +
    //    §13.16.3 SR3/SR6/SR13). 1540–1546 taken; 1550/1551/1552 are unallocated mid-band holes (the PHASE-12 earmark expired unused); 1560-band (PHASE-13). ──
    public static readonly DiagnosticDescriptor ConstantEntryRule = new(
        "COBOLNET1547", "constant-entry-rule", EditionSeverity.Error,
        "A constant entry violates a §13.10 syntax rule (figurative operand SR6; non-literal / exponentiation / "
        + "division-by-zero in the compile-time expression §7.3.6; duplicate constant-name SR9; ANY-LENGTH / "
        + "dynamic-length LENGTH operand SR10/SR12; non-integer constant where an integer is required SR2).",
        "ISO §13.10.3 / §7.3.6.2");
    public static readonly DiagnosticDescriptor ConstantAsReceiver = new(
        "COBOLNET1548", "constant-as-receiver", EditionSeverity.Error,
        "A constant-name or a data item of a CONSTANT RECORD shall not be specified as a receiving operand — a "
        + "constant substitutes a literal, and a structured constant's content cannot be modified.",
        "ISO §13.10.3 SR2 / §13.18.15.3 SR2");
    public static readonly DiagnosticDescriptor ConstantRecordRule = new(
        "COBOLNET1549", "constant-record-rule", EditionSeverity.Error,
        "A CONSTANT RECORD clause violates a structural rule: WS/LS sections only (SR1); level-01 only, no "
        + "REDEFINES, and no ANY LENGTH / BASED / BLANK WHEN ZERO / SYNCHRONIZED / TYPEDEF on the record or any "
        + "subordinate (§13.16.3 SR3/SR6/SR13).", "ISO §13.18.15.3 / §13.16.3");

    // ── COBOLNET0899 — the staged-loud constant-entry legs (recognized, not yet implemented) ─────────────
    public static readonly DiagnosticDescriptor ConstantFromCompilationVariable = new(
        NotImplemented, "constant-from-compilation-variable", EditionSeverity.Error,
        "CONSTANT … FROM compilation-variable-name (§13.10.4 GR1 — the >>DEFINE tie-in) is recognized but not "
        + "yet implemented: the preprocessor's compilation-variable store (ConditionalCompilationProcessor) is "
        + "local to the text stage and not reachable at bind time; the position-correct (SR8 'currently true') "
        + "capture across COPY expansion is the recorded residue.", "ISO §13.10 (FROM phrase)",
        RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ConstantByteLength = new(
        NotImplemented, "constant-byte-length", EditionSeverity.Error,
        "CONSTANT … AS BYTE-LENGTH OF (§13.10.4 GR5 — defined by the §15.14 BYTE-LENGTH intrinsic) is "
        + "recognized but not yet implemented: the §15.14 intrinsic itself is a Deferred catalog row, and the "
        + "byte-width authority lands ONCE, with it (the singular-pattern rule).", "ISO §13.10.4 GR5 / §15.14",
        RecognizedNotImplemented);

    // ── COBOLNET0899 — the CALL Format-2 legs (fix-queue PB46, CALL half) ────────────────────────────────
    // §14.9.4.2 Format 2 is `CALL {identifier-1|literal-1} AS {NESTED | program-prototype-name-1}`. The AS brace
    // has TWO arms with different dependencies: NESTED is supported, the prototype-name arm is not, and a reader
    // who hits the wall needs to be told WHICH half is missing rather than seeing an unresolved call.
    public static readonly DiagnosticDescriptor CallAsPrototypeName = new(
        NotImplemented, "call-as-prototype-name", EditionSeverity.Error,
        "CALL … AS program-prototype-name requires the program-prototype registry. ISO §14.9.4.3 syntax rule 16 "
        + "makes program-prototype-name-1 a name \"specified in a program-specifier in the REPOSITORY "
        + "paragraph\", and §12.3.8.2's program-specifier (PROGRAM program-prototype-name-1 [AS literal-3]) has "
        + "no repositoryEntry alternative, so no source can declare one. The sibling arm, CALL … AS NESTED, is "
        + "supported. Tracked as the P13 prototype registry.",
        "ISO §14.9.4.3 SR16 / §12.3.8.2", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor CallAsNestedNeedsLiteral = new(
        NotImplemented, "call-as-nested-needs-literal", EditionSeverity.Error,
        "CALL … AS NESTED names its program by literal-1. ISO §14.9.4.3 syntax rule 15: \"If the NESTED phrase "
        + "is specified, literal-1 shall be specified. Literal-1 shall be the same as the program-name specified "
        + "in a PROGRAM-ID paragraph of a common program … or of a program that is directly contained in the "
        + "calling program.\" An identifier target cannot name a contained program at compile time.",
        "ISO §14.9.4.3 SR15");
    public static readonly DiagnosticDescriptor CallContentOperandFormat = new(
        NotImplemented, "call-content-operand-format", EditionSeverity.Error,
        "This BY CONTENT operand belongs to a different CALL format. ISO §14.9.4.2 Format 1's BY CONTENT admits "
        + "\"{ identifier-2 } …\" and nothing else; the expression operands (arithmetic-expression-1, "
        + "boolean-expression-1) are Format 2's, which the AS phrase selects. A boolean expression is additionally "
        + "not yet carried across a CALL boundary — the INVOKE side has BoundInvokeArg.ContentBool and the CALL "
        + "argument model has no counterpart.",
        "ISO §14.9.4.2 Formats 1 and 2", RecognizedNotImplemented);

    // ── COBOLNET0899 — the staged-loud standard-arithmetic leg (P10 Step 12) ─────────────────────────────
    public static readonly DiagnosticDescriptor ArithmeticStandardIntrinsic = new(
        NotImplemented, "arithmetic-standard-intrinsic",  EditionSeverity.Error,
        "Under ARITHMETIC IS STANDARD / STANDARD-DECIMAL, ISO §15.4.1 r1 requires this function's returned "
        + "value to EQUAL its equivalent arithmetic expression evaluated in the standard-decimal intermediate "
        + "(SDIDI, §8.8.1.5) — the ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION equivalent "
        + "expressions carry inexact divisions, and the native IEEE-double engine (§15.4.1's native-arithmetic "
        + "approximation license) cannot honor that equality; staged loud until the CobolDec evaluations land "
        + "so a program depending on standard-decimal function results never silently gets native ones.",
        "ISO §15.4.1 / §8.8.1.5.1", RecognizedNotImplemented);

    // ── COBOLNET0899 — the staged-loud pointer-usage legs (P10 Step 7) ────────────────────────────────────
    public static readonly DiagnosticDescriptor UsageFunctionPointer = new(
        NotImplemented, "usage-function-pointer", EditionSeverity.Error,
        "USAGE FUNCTION-POINTER (§13.18.60 — a function-pointer data item) is recognized but not yet "
        + "implemented: its target identities are FUNCTION PROTOTYPES (§11.5 Format 2 / the repository "
        + "function-specifier), which are the P13 repository work — the pointer lands with them.",
        "ISO §13.18.60 (FUNCTION-POINTER phrase)", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ProgramPointerRestricted = new(
        NotImplemented, "program-pointer-restricted", EditionSeverity.Error,
        "USAGE PROGRAM-POINTER TO program-prototype-name (§13.18.60 GR25 — a RESTRICTED program-pointer, "
        + "confined to NULL or a same-signature program's address) is recognized but not yet implemented: "
        + "signature matching needs the program-prototype registry (P13); the unrestricted form is live.",
        "ISO §13.18.60 GR25 / SR22", RecognizedNotImplemented);

    // ── COBOLNET1533 — strong typing, split by rule (§8.5) ───────────────────────────────────────────
    public static readonly DiagnosticDescriptor StrongMoveMismatch = new(
        StrongType, "strong-move-mismatch", EditionSeverity.Error,
        "MOVE to/from a strongly-typed group requires a group of the same type.", "ISO §14.9.25.3 SR2");
    public static readonly DiagnosticDescriptor StrongClassCondition = new(
        StrongType, "strong-class-condition", EditionSeverity.Error,
        "A strongly-typed group item may not appear in a class condition.", "ISO §8.8.4.4.3 SR1");
    public static readonly DiagnosticDescriptor StrongCompareMismatch = new(
        StrongType, "strong-compare-mismatch", EditionSeverity.Error,
        "A strongly-typed group may be compared only with a group of the same type.", "ISO §8.8.4.2.3 SR1");

    // ── COBOLNET1535 — reused across two rules (the 1533 disambiguation pattern; code byte-stable) ───
    public static readonly DiagnosticDescriptor StrongCompareOrdering = new(
        "COBOLNET1535", "strong-compare-ordering", EditionSeverity.Error,
        "A strongly-typed group whose elementary items include class boolean, message-tag, object, or pointer "
        + "may be compared only for equality or inequality — an ordering relation on such a group is a syntax "
        + "error.", "ISO §8.8.4.2.3 SR4");
    public static readonly DiagnosticDescriptor TypedefRenamesStaged = new(
        "COBOLNET1535", "typedef-renames-staged", EditionSeverity.Error,
        "A level-66 RENAMES inside a TYPEDEF (part of the type per §13.18.58.4 GR1) is recognized but not yet "
        + "cloned into TYPE references.", "ISO §13.18.58.4 GR1", RecognizedNotImplemented);

    // ── COBOLNET1555/1556/1557 — the SAME AS clause, one code per rule family (§13.18.49 / §13.16.3;
    //    P10 Step 16). 1555 = the SUBJECT-entry rules (what the SAME AS entry itself may look like);
    //    1556 = the REFERENCED-entry rules (what data-name-1 may be); 1557 = the cycle rules.
    //    1550/1551/1552 are unallocated holes (the PHASE-12 earmark expired unused); 1553/1554 taken; 1558 = EXTERNAL type declarations. ──
    public static readonly DiagnosticDescriptor SameAsEntryRule = new(
        "COBOLNET1555", "same-as-entry-rule", EditionSeverity.Error,
        "A SAME AS entry violates a subject-entry rule: no clause other than CONSTANT RECORD, entry-name, "
        + "EXTERNAL, GLOBAL, level-number, and OCCURS may share the entry (§13.16.3 SR12); the entry shall not "
        + "be immediately followed by a subordinate or level-88 entry (§13.18.49 SR2); a level-77 subject "
        + "requires an elementary data-name-1 (SR8); no group containing the subject may carry a GROUP-USAGE, "
        + "SIGN, or USAGE clause (SR9).", "ISO §13.18.49.3 / §13.16.3 SR12");
    public static readonly DiagnosticDescriptor SameAsReferencedEntry = new(
        "COBOLNET1556", "same-as-referenced-entry", EditionSeverity.Error,
        "A SAME AS reference violates a data-name-1 rule: the target shall resolve to exactly one elementary "
        + "item or level-1 group item of the file/working-storage/local-storage/linkage section (§13.18.49 SR7); "
        + "it shall not be subject to any OCCURS clause (SR1) nor itself carry one (SR5); it shall not carry a "
        + "CONSTANT RECORD clause (SR10); in the file section its description shall not contain a USAGE OBJECT "
        + "REFERENCE item (SR6).", "ISO §13.18.49.3");
    public static readonly DiagnosticDescriptor SameAsCycle = new(
        "COBOLNET1557", "same-as-cycle", EditionSeverity.Error,
        "A SAME AS reference is cyclic: neither data-name-1's description nor any subordinate of the subject "
        + "may directly or indirectly reference the subject or a group it is subordinate to, via SAME AS (SR3) "
        + "or a TYPE clause (SR4).", "ISO §13.18.49.3 SR3/SR4");

    // ── COBOLNET1558 — EXTERNAL type declarations (§13.18.22 / §13.18.58; P10 Step 16) ───────────────
    // ── COBOLNET1559 — the report-group PRESENT WHEN / VARYING / multiple-COLUMN syntax rules, one code for
    //    the rule family (§13.15.3 / §13.18.64.3; P10 Step 13 — the SameAsEntryRule bundling precedent).
    //    1560-band stays earmarked (PHASE-13). ──
    public static readonly DiagnosticDescriptor ReportGroupClauseRule = new(
        "COBOLNET1559", "report-group-clause-rule", EditionSeverity.Error,
        "A report group description entry violates a PRESENT WHEN / VARYING syntax rule: a PRESENT WHEN "
        + "condition shall not reference a sum counter, LINE-COUNTER, PAGE-COUNTER, or another report section "
        + "data item (§13.15.3 SR16); GROUP INDICATE shall not share an entry with PRESENT WHEN (§13.15.3 "
        + "SR17); a VARYING entry shall also contain an OCCURS clause or a multiple LINE or multiple COLUMN "
        + "clause (§13.18.64.3 SR1); its data-name shall not be defined elsewhere in the source element (SR2) "
        + "nor referenced in arithmetic-expression-1 of the same clause (SR3).",
        "ISO §13.15.3 SR16/SR17 / §13.18.64.3 SR1–SR3");

    public static readonly DiagnosticDescriptor ExternalTypeRule = new(
        "COBOLNET1558", "external-type-rule", EditionSeverity.Error,
        "An EXTERNAL type declaration is misused: a data description containing an EXTERNAL type shall be at "
        + "level-number 1 (§13.18.22 GR2), and an external record whose type declaration is strongly typed "
        + "requires that type declaration to be external too (§13.18.22 SR5).", "ISO §13.18.22 SR5 / GR2/GR3");

    // ── COBOLNET0899 — national data (category not yet implemented) ──────────────────────────────────
    public static readonly DiagnosticDescriptor NationalData = new(
        NotImplemented, "national-data", EditionSeverity.Error,
        "National-category data (PIC N / USAGE NATIONAL, national numeric/boolean, national keys) is recognized "
        + "but not yet implemented.", "ISO §8.5 / §13.18.60", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor NationalThroughRange = new(
        NotImplemented, "national-through-range", EditionSeverity.Error,
        "A condition-name THROUGH range over a national conditional variable is not yet implemented.",
        "ISO §13.18.63 SR31", RecognizedNotImplemented);

    // ── COBOLNET0899 — PICTURE/USAGE staging ─────────────────────────────────────────────────────────
    public static readonly DiagnosticDescriptor UsageKeywordUnmappedInternal = new(
        NotImplemented, "usage-keyword-unmapped-internal", EditionSeverity.Error,
        "Internal: a grammar-accepted USAGE keyword has no ParseUsage mapping (a compiler defect).",
        "ISO §13.18.60", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ConstructStagedNotImplemented = new(
        NotImplemented, "construct-staged-not-implemented", EditionSeverity.Error,
        "A registry-recognized construct is available at this edition but not yet implemented (staged loud).",
        "COMPLETION_ROADMAP_COUNCIL", RecognizedNotImplemented);

    // ── COBOLNET0899 — Report Writer, deferred features ──────────────────────────────────────────────
    public static readonly DiagnosticDescriptor ReportGlobalClause = new(
        NotImplemented, "report-global-clause", EditionSeverity.Error,
        "The GLOBAL clause on a report description is not yet implemented.", "ISO §13.18.27", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportCodeClause = new(
        NotImplemented, "report-code-clause", EditionSeverity.Error,
        "The CODE clause on a report description is not yet implemented.", "ISO §13.18.12", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportLineNextPage = new(
        NotImplemented, "report-line-next-page", EditionSeverity.Error,
        "LINE … NEXT PAGE is not yet implemented.", "ISO §13.18.35", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportNextGroupClause = new(
        NotImplemented, "report-next-group-clause", EditionSeverity.Error,
        "The NEXT GROUP clause is not yet implemented.", "ISO §13.18.37", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportOccursInGroup = new(
        NotImplemented, "report-occurs-in-group", EditionSeverity.Error,
        "OCCURS (repeating entries) in a report group description is not yet implemented.",
        "ISO §13.18.38", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportMultipleLine = new(
        NotImplemented, "report-multiple-line", EditionSeverity.Error,
        "A multiple LINE clause (vertical repetition — the §13.18.35.4 GR9 OCCURS equivalence) is not yet "
        + "implemented; the report-group OCCURS family stages with it.", "ISO §13.18.35.3 SR10", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportVaryingCounterInExpression = new(
        NotImplemented, "report-varying-counter-in-expression", EditionSeverity.Error,
        "A report VARYING counter referenced inside a FROM/BY expression of a VARYING clause is not yet "
        + "implemented (legal in arithmetic-expression-2 per §13.18.64.3 SR3).", "ISO §13.18.64.3 SR3", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportConditionFunction = new(
        NotImplemented, "report-condition-function", EditionSeverity.Error,
        "A FUNCTION reference inside a report PRESENT WHEN condition is not yet implemented.",
        "ISO §13.18.41", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportIndicateRelativeColumn = new(
        NotImplemented, "report-indicate-relative-column", EditionSeverity.Error,
        "GROUP INDICATE on an entry with a relative (PLUS) COLUMN operand is not yet implemented.",
        "ISO §13.18.29 / §13.18.14", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportNonDisplayItem = new(
        NotImplemented, "report-non-display-item", EditionSeverity.Error,
        "A non-DISPLAY printable report item is not supported.", "ISO §13.15", RecognizedNotImplemented);
    // SUPPRESS PRINTING (§14.9.45) syntax-rule violation: the statement may appear ONLY in a USE BEFORE
    // REPORTING procedure (§14.9.45.3 SR1), which fixes the affected report group (§14.9.45.4 GR1). Written
    // anywhere else there is no group to inhibit — a genuine user error, not a non-support.
    public static readonly DiagnosticDescriptor ReportSuppressContext = new(
        "COBOLNET1581", "report-suppress-context", EditionSeverity.Error,
        "A SUPPRESS statement may appear only in a USE BEFORE REPORTING procedure.", "ISO §14.9.45.3 SR1");
    // §12.4.5.7 file-control COLLATING SEQUENCE (INDEXED record-key collating).
    public static readonly DiagnosticDescriptor FileCollatingKey = new(
        "COBOLNET1582", "file-collating-key", EditionSeverity.Error,
        "A file-control COLLATING SEQUENCE clause is malformed: it applies only to an INDEXED file, at most one "
        + "file-level clause is allowed, and every key-level name shall be a declared RECORD/ALTERNATE RECORD KEY "
        + "named in at most one clause.", "ISO §12.4.5.7.3 SR3-SR8");
    public static readonly DiagnosticDescriptor FileCollatingAlphabet = new(
        "COBOLNET1583", "file-collating-alphabet", EditionSeverity.Error,
        "A file-control COLLATING SEQUENCE clause names an alphabet that is not declared in SPECIAL-NAMES or is of "
        + "the wrong class for the key.", "ISO §12.4.5.7.3 SR1/SR2/SR7");
    public static readonly DiagnosticDescriptor FileCollatingNationalUnsupported = new(
        "COBOLNET1584", "file-collating-national-unsupported", EditionSeverity.Warning,
        "A NATIONAL alphabet on a file-control COLLATING SEQUENCE clause is recognized but national-key collating "
        + "for indexed files is not yet implemented — the key orders natively.", "ISO §12.4.5.7", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor DefineNoOverrideRedefinition = new(
        "COBOLNET1618", "define-no-override-redefinition", EditionSeverity.Error,
        "A >>DEFINE directive redefines a compilation variable to a different value without the OVERRIDE phrase "
        + "(the previous definition was neither OFF nor the same value).", "ISO §7.3.11.3 SR2");
    public static readonly DiagnosticDescriptor DirectiveExpressionViolation = new(
        "COBOLNET1619", "directive-expression-violation", EditionSeverity.Error,
        "A compiler-directive expression is malformed — a syntax error, or a formation-rule violation such as a "
        + "floating-point literal / figurative constant / concatenation in a directive (§7.3.3 SR10), a non-literal "
        + "or wrong-category operand, an exponentiation or division-by-zero in a compile-time arithmetic expression, "
        + "or a category-mismatched / non-numeric-ordering constant-conditional relation.",
        "ISO §7.3.6 / §7.3.7 / §7.3.8");
    // §7.3.14 / §7.3.15 migration-flagging directives — the warning channel (one code per directive; each emit
    // carries the specific option's message + GR4/Annex-E citation). Warning: a flag NEVER fails a compile.
    public static readonly DiagnosticDescriptor Flag02Warning = new(
        "COBOLNET1620", "flag-02-incompatibility", EditionSeverity.Warning,
        "A construct is flagged by an active >>FLAG-02 option — a 2002-to-2014 incompatibility potentially "
        + "affecting existing programs (the specific option + change is named in the message).", "ISO §7.3.14");
    public static readonly DiagnosticDescriptor Flag14Warning = new(
        "COBOLNET1621", "flag-14-incompatibility", EditionSeverity.Warning,
        "A construct is flagged by an active >>FLAG-14 option — a 2014-to-2023 incompatibility potentially "
        + "affecting existing programs (the specific option + change is named in the message).", "ISO §7.3.15");
    public static readonly DiagnosticDescriptor FlagDirectiveMalformed = new(
        "COBOLNET1622", "flag-directive-malformed", EditionSeverity.Error,
        "A >>FLAG-02 / >>FLAG-14 directive is malformed — an unknown option word, no option or ALL named, ALL "
        + "combined with individual options, or (FLAG-14) a missing ON/OFF phrase.", "ISO §7.3.14.2 / §7.3.15.2");
    // §7.3.10 COBOL-WORDS directive — a malformed directive or a syntax-rule violation. Error: an ill-formed or
    // rule-violating word-modification would silently mis-shape the reserved/context/function word tables.
    public static readonly DiagnosticDescriptor CobolWordsDirectiveInvalid = new(
        "COBOLNET1623", "cobol-words-directive-invalid", EditionSeverity.Error,
        "A >>COBOL-WORDS directive is malformed or violates a syntax rule — a missing/unknown option word, a "
        + "missing WITH/BY, a non-plain-alphanumeric literal (SR2), a placement after the first IDENTIFICATION "
        + "DIVISION (SR1), a word used in more than one directive (SR5), an existing word that is not a reserved / "
        + "context-sensitive / intrinsic-function word (SR3), or a new word that is not a valid user-defined word "
        + "or is itself reserved/context/intrinsic (SR4). The message names the specific rule.",
        "ISO §7.3.10.2 / §7.3.10.3");
    public static readonly DiagnosticDescriptor ReportSourceOtherReportCounter = new(
        NotImplemented, "report-source-other-report-counter", EditionSeverity.Error,
        "A SOURCE referencing another report's counter is not yet implemented.", "ISO §8.4.3.15 SR2", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportSourceSubscripted = new(
        NotImplemented, "report-source-subscripted", EditionSeverity.Error,
        "A subscripted or reference-modified SOURCE operand is not yet implemented.", "ISO §13.18.53", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportSumCrossReport = new(
        NotImplemented, "report-sum-cross-report", EditionSeverity.Error,
        "SUM … OF report-name (a cross-report sum) is not yet implemented.", "ISO §13.18.54.3 SR4g", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportSumRolledTotal = new(
        NotImplemented, "report-sum-rolled-total", EditionSeverity.Error,
        "A SUM addend naming another sum counter (rolled totals) is not yet implemented.",
        "ISO §13.18.54.4 GR6", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportMultipleOnFile = new(
        NotImplemented, "report-multiple-on-file", EditionSeverity.Error,
        "Multiple reports on one file (REPORTS ARE …) are not yet implemented.", "ISO §13.18.46", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ReportPageCounterReceiving = new(
        NotImplemented, "report-page-counter-receiving", EditionSeverity.Error,
        "PAGE-COUNTER as a receiving operand (legal) is not yet implemented.", "ISO §8.4.3.15", RecognizedNotImplemented);

    // ── COBOLNET0899 — Report Writer, semantic validation (genuine errors on the shared code) ─────────
    public static readonly DiagnosticDescriptor ReportGroupBefore01 = new(
        NotImplemented, "report-group-before-01", EditionSeverity.Error,
        "A report group entry appears before any 01-level entry.", "ISO §13.15");
    public static readonly DiagnosticDescriptor ReportColumnWithoutLine = new(
        NotImplemented, "report-column-without-line", EditionSeverity.Error,
        "A COLUMN clause has no LINE clause in effect.", "ISO §13.18.14");
    public static readonly DiagnosticDescriptor ReportItemMissingPicture = new(
        NotImplemented, "report-item-missing-picture", EditionSeverity.Error,
        "A printable report item has no PICTURE clause.", "ISO §13.16");
    public static readonly DiagnosticDescriptor ReportPageTypeRequiresPage = new(
        NotImplemented, "report-page-type-requires-page", EditionSeverity.Error,
        "A PAGE HEADING/FOOTING group requires a PAGE clause defining the page limit.", "ISO §13.18.57.3 SR12");
    public static readonly DiagnosticDescriptor ReportNotInFile = new(
        NotImplemented, "report-not-in-file", EditionSeverity.Error,
        "A report is not named in any file description entry's REPORT clause.", "ISO §13.18.46 / §13.14");
    public static readonly DiagnosticDescriptor ReportControlOperandUnresolved = new(
        NotImplemented, "report-control-operand-unresolved", EditionSeverity.Error,
        "A CONTROL operand does not resolve to a data item.", "ISO §13.18.16.3 SR3");
    public static readonly DiagnosticDescriptor ReportControlTypeOperand = new(
        NotImplemented, "report-control-type-operand", EditionSeverity.Error,
        "A TYPE CH/CF operand is not an operand of the CONTROL clause.", "ISO §13.18.57.3 SR10/SR11");
    public static readonly DiagnosticDescriptor ReportSourceOperandUnresolved = new(
        NotImplemented, "report-source-operand-unresolved", EditionSeverity.Error,
        "A SOURCE operand does not resolve to a data item.", "ISO §13.18.53.3 SR4");
    public static readonly DiagnosticDescriptor ReportSumAddendUnresolved = new(
        NotImplemented, "report-sum-addend-unresolved", EditionSeverity.Error,
        "A SUM addend does not resolve to a data item outside the report section.", "ISO §13.18.54.3 SR5");
    public static readonly DiagnosticDescriptor ReportResetNotControlOperand = new(
        NotImplemented, "report-reset-not-control-operand", EditionSeverity.Error,
        "A RESET ON operand is not an operand of the CONTROL clause.", "ISO §13.18.54.3 SR8");
    public static readonly DiagnosticDescriptor ReportLineCounterReceiving = new(
        NotImplemented, "report-line-counter-receiving", EditionSeverity.Error,
        "LINE-COUNTER shall not be referenced as a receiving operand.", "ISO §8.4.3.15.3 SR3");
    public static readonly DiagnosticDescriptor ReportCounterQualifierNotReport = new(
        NotImplemented, "report-counter-qualifier-not-report", EditionSeverity.Error,
        "A LINE/PAGE-COUNTER qualifier shall name a report description entry.", "ISO §8.4.3.15 SR2 / §8.4.2.2");
    public static readonly DiagnosticDescriptor ReportCounterNoReport = new(
        NotImplemented, "report-counter-no-report", EditionSeverity.Error,
        "A LINE/PAGE-COUNTER reference has no report, or is ambiguous across reports.", "ISO §8.4.3.15");
    public static readonly DiagnosticDescriptor ReportGenerateNeedsControl = new(
        NotImplemented, "report-generate-needs-control", EditionSeverity.Error,
        "GENERATE report-name requires a CONTROL clause in the report description.", "ISO §14.9.16.3 SR2");
    public static readonly DiagnosticDescriptor ReportGenerateNotDetail = new(
        NotImplemented, "report-generate-not-detail", EditionSeverity.Error,
        "GENERATE names a report group that is not a DETAIL group.", "ISO §14.9.16.3 SR1");

    // ── COBOLNET0899 — object-oriented refinements (deferred) ────────────────────────────────────────
    public static readonly DiagnosticDescriptor OoFactoryObjectReference = new(
        NotImplemented, "oo-factory-object-reference", EditionSeverity.Error,
        "USAGE OBJECT REFERENCE FACTORY OF is recognized but not yet implemented.", "ISO §13.18.60", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OoBasedInClass = new(
        NotImplemented, "oo-based-in-class", EditionSeverity.Error,
        "BASED data / ADDRESS OF in a class definition's data division is not yet implemented.",
        "ISO §13.18.60", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OoExternalMethodWorkingStorage = new(
        NotImplemented, "oo-external-method-working-storage", EditionSeverity.Error,
        "EXTERNAL on a method WORKING-STORAGE item is not yet implemented.", "ISO §14.5", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OoInterfacePropertyPrototype = new(
        NotImplemented, "oo-interface-property-prototype", EditionSeverity.Error,
        "A GET/SET PROPERTY prototype in an interface is not yet implemented.", "ISO §10.6.2", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OoMethodDeclaratives = new(
        NotImplemented, "oo-method-declaratives", EditionSeverity.Error,
        "DECLARATIVES inside a method are recognized but not yet implemented.", "ISO §14.2.1", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OoMethodRaisingLast = new(
        NotImplemented, "oo-method-raising-last", EditionSeverity.Error,
        "RAISING LAST EXCEPTION inside a method is not yet implemented.", "ISO §14.9.18.3 SR5", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OoGroupValuedProperty = new(
        NotImplemented, "oo-group-valued-property", EditionSeverity.Error,
        "A group-valued object-property reference is not yet implemented.", "ISO §8.4.3.9.4", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor AnyLengthReturning = new(
        NotImplemented, "any-length-returning", EditionSeverity.Error,
        "ANY LENGTH on a RETURNING item (legal per §13.18.2.3 SR3b) is recognized but not yet implemented — the "
        + "return crossing cannot carry the activator's receiver length yet (the ANY LENGTH formal-parameter leg "
        + "is fully implemented).", "ISO §13.18.2.3 SR3b / §13.18.2.4 GR1", RecognizedNotImplemented);

    // ── COBOLNET0899 — inter-program header-formal deferrals (P10 Step 10) ──────────────────────────
    public static readonly DiagnosticDescriptor ByValueFormalCarrier = new(
        NotImplemented, "by-value-formal-carrier", EditionSeverity.Error,
        "A BY VALUE formal parameter of class object, pointer, or of floating-point usage is legal "
        + "(§14.2.2 SR2) but its value-copy carrier is not yet implemented — only a fixed-point numeric "
        + "BY VALUE formal is carried (the §14.2.3 GR10 detached-cell copy).",
        "ISO §14.2.2 SR2 / §14.2.3 GR10", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor OptionalFormal = new(
        NotImplemented, "optional-formal", EditionSeverity.Error,
        "An OPTIONAL formal parameter in the procedure division header is recognized (§14.2.2 using-phrase) "
        + "but the OPTIONAL/OMITTED formal model is not yet implemented (the omitted-argument condition, "
        + "§8.8.4.8).", "ISO §14.2.2 / §14.2.3 GR3", RecognizedNotImplemented);

    // ── COBOLNET0899 — miscellaneous deferrals ───────────────────────────────────────────────────────
    public static readonly DiagnosticDescriptor ExternalRecordNotCellBacked = new(
        NotImplemented, "external-record-not-cell-backed", EditionSeverity.Error,
        "An EXTERNAL record cannot be cell-backed (a restriction of the current EXTERNAL model).",
        "ISO §13.18.24", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor RecursiveContainedWs = new(
        NotImplemented, "recursive-contained-working-storage", EditionSeverity.Error,
        "A RECURSIVE program that directly contains programs and declares WORKING-STORAGE is recognized but "
        + "not yet implemented — the shared-static WS model (one last-used copy across activations) does not "
        + "yet compose with contained-program GLOBAL/__outer bridges.",
        "ISO §13.5.4 GR1 / §14.6.2.3.3 / §13.18.27 GR2", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor RecursiveWsPointerBacked = new(
        NotImplemented, "recursive-working-storage-pointer-backed", EditionSeverity.Error,
        "BASED data or an ADDRESS-OF-taken record in the WORKING-STORAGE of a RECURSIVE program or function "
        + "is recognized but its static cell/bridge storage is not yet implemented (the cell and the implicit "
        + "data-address pointer are per-instance today, which would re-initialize per activation).",
        "ISO §13.5.4 GR1 / §14.6.2.3.2 #5", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ValueNumericEditedOversize = new(
        "COBOLNET1570", "value-numeric-edited-oversize", EditionSeverity.Error,
        "At COBOL-2023 an alphanumeric edited-image literal in the VALUE clause of a numeric-edited item is checked "
        + "against the PICTURE size (ISO §13.18.63 SR4/SR5) — a literal longer than the edited width is rejected "
        + "(before 2023 it was stored truncated). Under --permissive the check is a warning (a removed-capability "
        + "posture); the national/alphanumeric class mismatch is the separate COBOLNET0898 check.",
        "ISO §13.18.63 SR4/SR5 / Annex E.2 item 27 (VCR row 34)");
    public static readonly DiagnosticDescriptor DebugSubFacilityStaged = new(
        "COBOLNET1571", "debug-sub-facility-staged", EditionSeverity.Error,
        "The X3.23-1985 USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES trigger leg + the DEBUG-ITEM special "
        + "register are modeled at --std 85; the data-name (incl. ALL REFERENCES OF), file-name, and cd-name subject "
        + "kinds and the SORT/MERGE INPUT/OUTPUT-procedure DEBUG-CONTENTS cause are staged — rejected loud rather "
        + "than compiled with a missing/stale trigger.",
        "VCR Table 7 row 7.17 (X3.23-1985 debug module)");
    public static readonly DiagnosticDescriptor MergeInSortMergeProc = new(
        "COBOLNET1572", "merge-in-sort-merge-proc", EditionSeverity.Error,
        "At COBOL-2023 a MERGE statement is prohibited in the output procedure of another MERGE, or the input or "
        + "output procedure of a file-format SORT (the prior standard allowed it with conflicting rules; SORT "
        + "already disallowed it). A bind-time procedure-range cross-pass rejects it at --std 2023; below 2023 the "
        + "runtime EC-SORT-MERGE-ACTIVE seam is the checking-off dynamic net.",
        "ISO §14.9.24 / Annex E.2 item 20 (VCR row 27)");
    public static readonly DiagnosticDescriptor ExceptionFileArgumentNotFile = new(
        "COBOLNET1574", "exception-file-argument-not-file", EditionSeverity.Error,
        "The argument of FUNCTION EXCEPTION-FILE / EXCEPTION-FILE-N shall be the name of a file connector specified "
        + "in an FD statement (ISO §15.28.3 rule 1 / §15.29.3) — the given name does not resolve to a declared file.",
        "ISO §15.28.3 rule 1 / §15.29.3 (VCR rows 68/69)");
    public static readonly DiagnosticDescriptor ExternalFileStatusConsistency = new(
        "COBOLNET1573", "external-file-status-consistency", EditionSeverity.Error,
        "At COBOL-2023, for an external file all corresponding file control entries in the run unit shall specify the "
        + "FILE STATUS clause naming the same corresponding external data item (ISO §12.4.5.3 GR1(i); §14.8.4.2; Annex "
        + "E.2 item 12) — a corresponding SELECT omitting FILE STATUS, or naming a non-external / different external "
        + "item, is rejected. Below 2023 the requirement did not exist.",
        "ISO §12.4.5.3 GR1(i) / §14.8.4.2 / Annex E.2 item 12 (VCR row 18)");
    public static readonly DiagnosticDescriptor ExternalRelativeKeyConsistency = new(
        "COBOLNET1575", "external-relative-key-consistency", EditionSeverity.Error,
        "At COBOL-2023, for an external relative file all corresponding file control entries in the run unit shall "
        + "specify the RELATIVE KEY clause naming the same corresponding external data item (ISO §12.4.5.3 GR1(h); "
        + "§14.8.4.2; Annex E.2 item 24) — a corresponding SELECT omitting RELATIVE KEY, or naming a non-external / "
        + "different external item, is rejected. Below 2023 the requirement did not exist.",
        "ISO §12.4.5.3 GR1(h) / §14.8.4.2 / Annex E.2 item 24 (VCR row 31)");
    public static readonly DiagnosticDescriptor ExternalFileItemNotExternal = new(
        "COBOLNET1624", "external-file-item-not-external", EditionSeverity.Error,
        "At COBOL-2023, for an external file connector the FILE STATUS, RELATIVE KEY and LINAGE data items shall "
        + "themselves be external data items (ISO §14.8.4.2; Annex E.2 item 9) — a file/relative-key/linage clause "
        + "naming a non-external item is rejected. Enforced per connector regardless of describer count. Below 2023 "
        + "the requirement did not exist.",
        "ISO §14.8.4.2 / Annex E.2 item 9");
    // 1625 — the fixed-point-numeric VALUE range/sign syntax rules (§13.18.63.3 SR2/SR3; the 08xx value/picture band
    // is fully allocated, so the next-free scan lands this in the 16xx band alongside the 1570 numeric-edited VALUE
    // check). Edition-invariant (present 85/2002/2014/2023).
    public static readonly DiagnosticDescriptor ValueNumericOutOfRange = new(
        "COBOLNET1625", "value-numeric-out-of-range", EditionSeverity.Error,
        "A fixed-point numeric VALUE literal is not a permissible value in the range the PICTURE indicates: it is "
        + "not representable in the subject without truncation of a leading or trailing nonzero digit (SR2), or a "
        + "negative literal seeds an unsigned subject (SR3). A syntax-rule violation, rejected at bind time rather "
        + "than silently mis-stored as an out-of-range native value.",
        "ISO §13.18.63.3 SR2/SR3");
    // 1626 — the character-operand USAGE syntax rules of INSPECT / STRING / UNSTRING (DA7). These constructs were
    // already REJECTED correctly; the defect was the STAGE. Each violation was reported as a run-time
    // NotImplementedCobolFeatureException, so an illegal program compiled clean and then crashed when control
    // reached the statement — where the standard promises a compile-time error. Edition-invariant: all three rules
    // are present unchanged at 85/2002/2014/2023, so this is NOT edition-gated and needs no introduction axis.
    public static readonly DiagnosticDescriptor CharacterOperandUsage = new(
        "COBOLNET1626", "character-operand-usage", EditionSeverity.Error,
        "An INSPECT, STRING or UNSTRING operand that must be a character item is not one. INSPECT identifier-1 "
        + "shall be an alphanumeric/national GROUP item or an ELEMENTARY item of usage display or national "
        + "(§14.9.22.3 SR1 — note the rule admits a group outright and constrains only an elementary operand); "
        + "STRING's identifiers other than the POINTER shall be usage display or national (§14.9.43.3 SR1); and "
        + "UNSTRING's INTO receiver shall be usage display with category alphabetic/alphanumeric/numeric, or usage "
        + "national with category national/numeric (§14.9.48.3 SR4). A binary, packed, float, index or pointer "
        + "ELEMENTARY operand has no character image and is rejected at bind time. ⛔ A GROUP receiver is NOT "
        + "rejected: §14.9.43.4 GR3a transfers into STRING's receiver \"in accordance with the MOVE statement rules "
        + "for alphanumeric-to-alphanumeric moves\", so a group takes whatever an alphanumeric MOVE may deposit, "
        + "including a group holding a BINARY/PACKED leaf (V59 gave those leaves a byte image).",
        "ISO §14.9.22.3 SR1 / §14.9.43.3 SR1 / §14.9.48.3 SR4");
    // 1627 — the ISO §15.3 intrinsic ARGUMENT-CLASS screen (fix-queue PB1). IntrinsicCatalog declared an
    // ArgKinds class code on all 79 rows and IntrinsicSig.ArgKind had ZERO callers, so no §15 argument rule was
    // enforced from the table built for it: FUNCTION REVERSE(<PIC 9(4)>) and FUNCTION ABS(<PIC X(4)>) both
    // compiled clean and produced garbage. Edition-invariant — §15.3's argument types are unchanged across
    // 85/2002/2014/2023 — so this is NOT edition-gated and needs no introduction axis. The --permissive leniency
    // mirrors DA6/COBOLNET0844, which settled the sibling §8.8.1.1 question for ARITHMETIC operands; one
    // mechanism for one question.
    public static readonly DiagnosticDescriptor IntrinsicArgumentClass = new(
        "COBOLNET1627", "intrinsic-argument-class", EditionSeverity.Error,
        "An intrinsic-function argument is not of the class its argument rule requires. ISO §15.3 defines the "
        + "argument types: type 10 Numeric admits \"an arithmetic expression or a numeric data item\", type 6 "
        + "Integer admits an integer-valued arithmetic expression or an integer data item, and types 1/2/9 admit "
        + "the alphabetic/alphanumeric/national family (a strongly-typed group item counting as alphanumeric). "
        + "The MAX/MIN/ORD-MAX/ORD-MIN family instead carries a NEGATIVE rule — §15.71.3 r1 and siblings exclude "
        + "class boolean, message-tag, object and pointer. ⛔ CLASS, not category: §8.5.2.1 Table 2 puts "
        + "NUMERIC-EDITED under class ALPHANUMERIC when its usage is display, so PIC ZZ9.99 is not a legal "
        + "numeric argument however numeric it looks. An operand whose class is not statically decidable is "
        + "never rejected. --permissive downgrades this to a warning and proceeds with the existing coercion.",
        "ISO §15.3 / §8.5.2.1 Table 2 / §4.2.2 para 3 (which leaves the determination to the implementor)");
    // 1628 — CALL … USING BY VALUE operand class (ISO §14.9.4.3 SR22). Surfaced by the pre-merge GnuCOBOL
    // differential: two AGREE_ACCEPT→WE_REJECT flips traced to DA6's §8.8.1.1 arithmetic screen firing on a BY
    // VALUE operand, because the grammar production is named arithmeticExpression and the binder took that at
    // its word. The VERDICT was right — SR22 does exclude an alphanumeric operand — but COBOLNET0844 quoted a
    // rule about arithmetic expressions at a programmer who had broken a CALL rule. Edition-invariant in
    // SUBSTANCE, but BY VALUE itself is a COBOL-2002 introduction, so it is unreachable below --std 2002 and
    // needs no separate introduction axis of its own.
    public static readonly DiagnosticDescriptor CallByValueOperandClass = new(
        "COBOLNET1628", "call-by-value-operand-class", EditionSeverity.Error,
        "A CALL … USING BY VALUE operand is not of a class the standard permits to be passed by value. ISO "
        + "§14.9.4.3 SR22: \"If identifier-4 or its corresponding formal parameter is specified with a BY VALUE "
        + "phrase, identifier-4 shall be of class numeric, object, or pointer.\" An alphanumeric, national or "
        + "boolean operand is therefore rejected — GnuCOBOL accepts one as an extension, assuming BY CONTENT, "
        + "which is a different passing mode and not what the source asked for. --permissive accepts it with a "
        + "warning. ⛔ CLASS, not category (§8.5.2.1 Table 2): a numeric-edited item is class ALPHANUMERIC when "
        + "its usage is display, so it is excluded however numeric it looks.",
        "ISO §14.9.4.3 SR22 / §8.5.2.1 Table 2");
    // 1629 — reference-modifying a FUNCTION result whose function is not alphanumeric/boolean/national (ISO
    // §8.4.3.3.3 SR2). Opened by fix-queue PB8, which made the shape PARSE for the first time: before it, every
    // ref-modified function-identifier died at COBOL0001 and the class question could not even be asked. The
    // rule is edition-invariant — reference modification of a function-identifier is in the 1989 intrinsic
    // amendment's model and unchanged since — so no introduction axis of its own.
    public static readonly DiagnosticDescriptor RefModFunctionResultClass = new(
        "COBOLNET1629", "ref-mod-function-result-class", EditionSeverity.Error,
        "Reference modification is applied to the result of a function that is not of a class the standard "
        + "permits to be reference-modified. ISO §8.4.3.3.3 SR2: \"If identifier-1 is a function-identifier, it "
        + "shall reference an alphanumeric, boolean, or national function.\" A NUMERIC or INTEGER function "
        + "(FUNCTION PI, FUNCTION MAX over numerics, FUNCTION LENGTH …) is therefore rejected: §15.2 gives it a "
        + "numeric temporary, and §8.4.3.3.4 GR1 has no character positions to number in one. ⛔ This is the "
        + "FUNCTION's declared type (§15.2), not the shape of its arguments — FUNCTION MAX over alphanumeric "
        + "arguments IS an alphanumeric function and is legal here. --permissive does NOT relax it: unlike a "
        + "removed-construct leniency there is no defined value to fall back on.",
        "ISO §8.4.3.3.3 SR2 / §15.2");
    // 1630 — reference-modifying a reference modification (ISO §8.4.3.3.3 SR3). Found by the PB8 sibling sweep
    // (CLAUDE.md rule 4), NOT by PB8's own repro: the grammar's `dataReferenceSuffix*` admits unlimited
    // refModParts, and ReferenceResolver kept only the FIRST of each carrier via `??=` while the DEFAULT-mode
    // form outranked the SUBSCRIPT-mode one — so `MOVE A (3:4)(2:2)` compiled clean and silently returned
    // A(2:2), neither the composition nor the rejection the standard requires. Closes the traceability row
    // SR-8.4.3.3.3-3, which stood at state GAP with an empty code-location.
    public static readonly DiagnosticDescriptor RefModOfRefMod = new(
        "COBOLNET1630", "ref-mod-of-ref-mod", EditionSeverity.Error,
        "A reference modification is applied to something that is already reference-modified. ISO §8.4.3.3.3 "
        + "SR3: \"Identifier-1 shall not be a reference-modification format identifier.\" §8.4.3.3.4 GR5 numbers "
        + "positions within the item identifier-1 references, and a ref-mod result is a NEW unique data item "
        + "(GR5), so a second modifier has no defined base to count from. Write the composed positions directly: "
        + "A (3:4)(2:2) is A (4:2). ⛔ A SUBSCRIPT followed by a reference modification — T(I) (2:3) — is a "
        + "different and entirely legal shape (§8.4.3.1.4 GR1 a→g) and is not affected; only a SECOND reference "
        + "modification is rejected.",
        "ISO §8.4.3.3.3 SR3 / §8.4.3.3.4 GR5");
    // 1631 — a FORMATTED-*/INTEGER-OF-FORMATTED-DATE/… format argument that is not one of the §15.3.1–§15.3.4
    // formats, or is the wrong KIND for the function (fix-queue PB11). Before this, the format was validated
    // character-wise only, so any string assembled from legal subfields was accepted and the function
    // FABRICATED a value — `FORMATTED-DATE("hhmmss" …)` returned "000000". Edition-invariant in substance; the
    // functions themselves are 2014+ (§15.39–§15.41), so it is unreachable below that.
    public static readonly DiagnosticDescriptor DateTimeFormatKindMismatch = new(
        "COBOLNET1631", "date-time-format-kind", EditionSeverity.Error,
        "A date/time FORMAT argument is not a format the standard defines, or is the wrong KIND for the "
        + "function. ISO §15.3.1.1 fixes SIX date formats (basic and extended, for calendar, ordinal and week "
        + "dates), §15.3.2 twelve time formats (four common-time shapes × local / UTC / offset), and §15.3.4 "
        + "makes a combined format a date format, an uppercase T, and a time format. §15.39.3 r2 requires a "
        + "DATE format, §15.41.3 r2 a TIME format and §15.40.3 r2 a COMBINED one. ⛔ BASIC AND EXTENDED NEVER "
        + "MIX: `YYYY-MMDD` and `YYYY-MM-DDThhmmss` are built entirely from legal subfields and are still not "
        + "formats, which is why membership is tested rather than each field in isolation.",
        "ISO §15.3.1.1 / §15.3.2 / §15.3.4 / §15.39.3 r2 / §15.40.3 r2 / §15.41.3 r2");
    // PB10's INSPECT half. Deliberately NOT an INSPECT-specific code: §8.4.3.2.3 SR1 is ONE rule about
    // function-identifiers in RECEIVING positions, and PB10's remaining positions plus PB17 want the same
    // verdict. Naming it after the rule rather than the statement is what stops the next site minting a second
    // code for the same sentence (feedback_one_rule_one_place).
    public static readonly DiagnosticDescriptor FunctionIdentifierReceiving = new(
        "COBOLNET1632", "function-identifier-receiving", EditionSeverity.Error,
        "A function-identifier is written where the statement MODIFIES the operand. ISO §8.4.3.2.3 SR1: \"A "
        + "function-identifier shall not be specified as a receiving operand.\" A function returns a temporary "
        + "value (§15.4), so there is nothing for the statement to store into. ⛔ THIS IS POSITION-SPECIFIC, NOT "
        + "STATEMENT-SPECIFIC: §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so every "
        + "identifier-N SENDING position admits one — INSPECT identifier-1 is legal in Format 1 (TALLYING), "
        + "where §14.9.22.4 GR1 treats it as sending, and barred in Formats 2/3/4 (REPLACING / "
        + "TALLYING-and-REPLACING / CONVERTING), where GR7 replaces its characters and GR20 makes format 4 "
        + "execute as a format 2 over the same identifier-1. Move the function result into a data item first.",
        "ISO §8.4.3.2.3 SR1 (with §14.9.22.4 GR1/GR7/GR20 for INSPECT's per-format split)");
    // PB11's VALUE half. Decidable at BIND time and therefore a diagnostic rather than a run-time check:
    // §15.40.3 r1 / §15.41.3 r1 make argument-1 a LITERAL, so the format's zone is known at compile time, and the
    // offset argument's PRESENCE is syntactic. Before this the argument was accepted and silently DISCARDED.
    public static readonly DiagnosticDescriptor DateTimeOffsetArgumentNotPermitted = new(
        "COBOLNET1633", "datetime-offset-argument-not-permitted", EditionSeverity.Error,
        "An offset-from-UTC argument is supplied for a format whose time portion is a LOCAL time — it carries no "
        + "place to put an offset. ISO §15.41.3 r5: \"Argument-3 shall not be specified if the time portion of "
        + "the format in argument-1 is neither a UTC format nor an offset format\"; §15.40.3 r6 says the same of "
        + "FORMATTED-DATETIME's argument-4. A UTC format ends in 'Z' and an offset format carries an explicit "
        + "'+hhmm' / '+hh:mm' subformat (§15.3.3.4–§15.3.3.6); a plain 'hhmmss' / 'hh:mm:ss' is local. ⚠ The "
        + "converse is NOT an error: omitting the argument for a UTC or offset format is explicitly legal and "
        + "evaluates as though 0 were specified (§15.40.3 r7 / §15.41.3 r6).",
        "ISO §15.40.3 r6 / §15.41.3 r5 (zone per §15.3.3.4–§15.3.3.6)");
    // PB47. A SYNTAX RULE violation, so a compile-time diagnostic — and that is the whole point of the entry:
    // an invalid pairing used to compile clean and reach a RUN-TIME NotImplementedCobolFeatureException reading
    // "a COBOL feature that is not yet implemented", which is doubly wrong (it IS implemented; the source is
    // inadmissible) and is coverage-shaped, since a WHEN branch that never executes never reported at all.
    public static readonly DiagnosticDescriptor EvaluateOperandCombinationInvalid = new(
        "COBOLNET1634", "evaluate-operand-combination-invalid", EditionSeverity.Error,
        "This EVALUATE selection subject and selection object may not be paired. ISO §14.9.13.3 syntax rule 10: "
        + "\"The permissible combinations of selection subject and selection object operands are indicated in "
        + "Table 15, Combination of operands in the EVALUATE statement\", and the cell for this pairing is blank "
        + "— \"a space indicates an invalid combination\". Most often this is a value object written against a "
        + "TRUE/FALSE subject, whose only permissible objects are a condition, TRUE/FALSE, or ANY.",
        "ISO §14.9.13.3 SR10 (Table 15)");
    // R03. A SYNTAX RULE violation that used to produce a SILENT WRONG ANSWER rather than any diagnostic: the hex
    // decoders answer "" for a malformed digit count and every caller took that as the literal's value, so
    // `FUNCTION LENGTH(X"414")` said 1. Three formats state the grouping rule and they differ — §8.3.3.2.3 r6
    // (alphanumeric, pairs), §8.3.3.5.3 r5 (national, four for the D-N1 UTF-16 unit) — while §8.3.3.4.3 r3
    // states NO grouping rule for BX at all, because §8.3.3.4.4 GR5 maps each digit on its own. The check is
    // therefore per-format, and BX is deliberately unscreened.
    public static readonly DiagnosticDescriptor HexLiteralDigitGrouping = new(
        "COBOLNET1635", "hex-literal-digit-grouping", EditionSeverity.Error,
        "A hexadecimal literal's digits do not form whole characters. ISO §8.3.3.2.3 rule 6 (X\"…\") and "
        + "§8.3.3.5.3 rule 5 (NX\"…\") each require every hexadecimal character sequence to consist of the number "
        + "of digits that map to one character — two for an alphanumeric character, four for a national one. A "
        + "hexadecimal-BOOLEAN literal (BX\"…\") has no such rule: §8.3.3.4.4 GR5 maps each digit independently "
        + "to four boolean characters, so any digit count is well formed there.",
        "ISO §8.3.3.2.3 r6 / §8.3.3.5.3 r5");
    // kb/Work R16 (ledger F11). A SYNTAX RULE violation that used to compile clean and abort at RUN TIME
    // ("computed expression in a string context") — DISPLAY IX, MOVE IX TO an alphanumeric, STRING IX — or,
    // worse, silently compute (MOVE IX TO a numeric item), against the SAME judgment the W2 review already
    // landed for class-index DATA ITEMS (COBOLNET0809 rejects a MOVE of one at every edition). §13.18.38.3 r7
    // is a closed LIST, so the diagnostic names it and the legal spelling.
    public static readonly DiagnosticDescriptor IndexNameContext = new(
        "COBOLNET1637", "index-name-context", EditionSeverity.Error,
        "An index-name is written where the statement needs an identifier. An index-name is not an identifier "
        + "(ISO §8.4.3.1.2 defines the identifier formats and an index-name is none of them), and "
        + "§13.18.38.3 r7 closes the list of contexts that may reference one: a subscript, the VARYING phrase "
        + "of PERFORM or SEARCH, the SET statement, and a relation-condition operand. To use the index's "
        + "occurrence number elsewhere, SET a data item to it first (SET data-item TO index-name, §14.9.39) "
        + "and reference the data item.",
        "ISO §13.18.38.3 r7 / §8.4.3.1.2");
    // kb/Work R19 (ledger F18). The OMITTED arm one line above it reports (COBOLNET1544) while this arm
    // returned a silent BoundOperandError — the program compiled with zero diagnostics and threw
    // NotImplementedCobolFeatureException at run time. §4.2.2 ¶3 obliges the warning mechanism to indicate a
    // syntactically-distinguishable violation, and a reserved phrase word in an argument slot of a function
    // that takes no phrase is exactly that. Generic to every catalogued function — reported at the ONE arm,
    // never per function.
    public static readonly DiagnosticDescriptor IntrinsicArgumentNotAValue = new(
        "COBOLNET1638", "intrinsic-argument-not-a-value", EditionSeverity.Error,
        "An intrinsic-function argument position holds a reserved phrase word (LEADING, TRAILING, LAST, "
        + "FIRST, ANY, START, AFTER, ALPHANUMERIC, NATIONAL) or a literal form the position does not admit. "
        + "The phrase words belong to the few functions whose §15.x.2 general format names them (FIND-STRING, "
        + "SUBSTITUTE, CONVERT, TRIM, MODULE-NAME); every other function's format admits only its arguments — "
        + "§15.3's argument types are identifiers, literals, and expressions, never a bare reserved word.",
        "ISO §15.3 / §4.2.2 ¶3 (the per-function §15.x.2 general format decides)");
    // kb/Work R30. Found while probing R22's name-collision control: MOVE NO-SUCH-NAME TO X and DISPLAY
    // NO-SUCH-NAME compiled with zero diagnostics, exit 0, and threw NotImplementedCobolFeatureException at
    // run time — in EVERY reference position measured (sender, receiver, arithmetic operand, condition,
    // STRING, CALL USING, subscript, PERFORM UNTIL). ReferenceResolver.Resolve staged the unresolved
    // fallthrough as a runtime loud; §4.2.2 ¶3 obliges the indication, and a typo is never a feature gap.
    // Reported at the ONE chokepoint (Resolve's undefined arm, deduped per source reference); the
    // type-discriminating probe sites with a legal alternative on failure (the SET format sniffs, INVOKE's
    // class-name receiver, EXCEPTION-OBJECT, the boolean/float reroutes) read the silent Probe form, so a
    // legal alternative reading never draws it. Unsupported-SHAPE nulls of a name that DID resolve keep the
    // documented references-then-fail-loud staging — that debt is a different register entry than a typo.
    // kb/Work R34. The differential's syn_copy:630 ("COPY: recursive replacement") exposed the shape: GnuCOBOL
    // extends COPY REPLACING to reach nested-COPY text; ISO forbids the COMBINATION outright — §7.2.3.4 GR10
    // "If the REPLACING phrase is specified, the library text shall not contain a COPY statement" (GR12 permits
    // nesting, ≥5 levels, only WITHOUT replacing). Our preprocessor recursed OUTSIDE the replacement scope, so
    // the illegal source produced arbitrary partial text and a misleading downstream COBOLNET1639 on whatever
    // name failed to materialize. The verdict (reject) was right by accident; this descriptor makes the REASON
    // right. Emitted by CopyProcessor.ResolveOneCopy at the outer COPY's line. The GnuCOBOL replacement-reaches-
    // nested-text semantics remain a vendor-dialect-axis candidate, adjudicated separately.
    // kb/Work R39 (found by R36's adjudication probes): `REPLACE LEADING "PREFIX-" BY SPACES` — the GCOS/ACU
    // vendor spelling — was silently HALF-PARSED: no diagnostic on the statement, nothing applied, and the
    // failure surfaced downstream as COBOLNET1639 on the never-replaced name. REPLACE's operands were NEVER
    // literals in any ISO edition (unlike COPY's, which 2023 removed — COBOLNET0902's territory): the
    // §7.2.4.2 general format admits pseudo-text/partial-word operands only, and §7.2.4.3 SR7 bars literals
    // as partial-words explicitly. Emitted by CopyProcessor.ApplyReplaceStatements via the same
    // NoteNonPseudoText hook the COPY gate rides — one detector, two rules, each cited at its own site.
    // §12.3.3 SR1 — a contained program has NO configuration section of its own: the container's applies to it
    // (§12.3.4 GR1), which is why DataBinder.InheritConfiguration copies the whole configuration-derived state
    // into every containee before it binds (kb/Work PB60 / AR-15.67.3-5 — a contained program under
    // DECIMAL-POINT IS COMMA parsed NUMVAL("123,45") as 0 and NUMVAL("123.45") as 123.45).
    // §15.68.3 r3 — NUMVAL-C / TEST-NUMVAL-C without argument-2 in a unit whose SPECIAL-NAMES paragraph specifies
    // two or more distinct currency strings (kb/Work PB60 / AR-15.68.3-3: the former single-symbol model
    // injected whichever clause bound last, silently).
    // §15.3 over SUBSTITUTE's `{ argument-2 argument-3 } …` (kb/Work PB81): the elements of a table(ALL) would form
    // the from/to PAIRS at run time — staged loud at bind rather than thrown at run time.
    public static readonly DiagnosticDescriptor SubstituteAllSubscript = new(
        NotImplemented, "substitute-all-subscript-argument", EditionSeverity.Error,
        "FUNCTION SUBSTITUTE with a table(ALL) argument is recognized but not yet implemented: ISO §15.3 makes the ALL "
        + "stand for every occurrence, so the argument-2/argument-3 pairs of §15.87.2 would be formed at run time from "
        + "the enumerated elements (with the FIRST/LAST/ANYCASE modes attaching to the pair each element starts) — the "
        + "bind-time pairing cannot express a runtime count. Write the pairs.", "ISO §15.3 / §15.87.2",
        RecognizedNotImplemented);
    // §15.3 — the ALL subscript in an intrinsic argument (kb/Work PB62): admissible only "when the definition of a
    // function permits an argument to be repeated a variable number of times"; the former bind-time expansion ran
    // for every function and let `FUNCTION MOD(E(ALL) B)` bind over a one-occurrence table.
    public static readonly DiagnosticDescriptor AllSubscriptNotRepeatable = new(
        "COBOLNET1645", "all-subscript-not-repeatable-argument", EditionSeverity.Error,
        "A table(ALL) argument is written to an intrinsic function whose general format does not repeat an argument. "
        + "ISO §15.3: \"When the definition of a function permits an argument to be repeated a variable number of "
        + "times, a table may be referenced by specifying the data-name and any qualifiers that identify the table, "
        + "followed immediately by subscripting where one or more of the subscripts is the word ALL.\" The ALL "
        + "subscript stands for every occurrence, so it belongs only where the format is `{ argument } …` (MAX, MIN, "
        + "SUM, MEAN, MEDIAN, MIDRANGE, RANGE, ORD-MAX, ORD-MIN, VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE, CONCAT, "
        + "SUBSTITUTE, TRIM's argument-2). Write the occurrence you mean.",
        "ISO §15.3 (the ALL subscript)");
    public static readonly DiagnosticDescriptor NumvalCAmbiguousCurrency = new(
        "COBOLNET1644", "numval-c-ambiguous-currency", EditionSeverity.Error,
        "FUNCTION NUMVAL-C or TEST-NUMVAL-C is written without argument-2 (and without LOCALE) in a compilation "
        + "unit whose SPECIAL-NAMES paragraph specifies two or more distinct currency strings. ISO §15.68.3 rule 3: "
        + "\"If neither argument-2 nor the LOCALE keyword is specified, there shall be only one currency string for "
        + "the compilation unit, either the default currency sign or a currency string specified in the "
        + "SPECIAL-NAMES paragraph.\" Name the intended currency string as argument-2.",
        "ISO §15.68.3 rule 3 (via §15.94.3 rule 1 for TEST-NUMVAL-C)");
    public static readonly DiagnosticDescriptor ConfigurationSectionInContainedProgram = new(
        "COBOLNET1643", "configuration-section-in-contained-program", EditionSeverity.Error,
        "A CONFIGURATION SECTION is specified in a program that is contained within another program. ISO "
        + "§12.3.3 SR1: \"The configuration section shall not be specified in a program that is contained within "
        + "another program\" — the containing program's configuration section (SPECIAL-NAMES, OBJECT-COMPUTER, "
        + "SOURCE-COMPUTER, REPOSITORY) applies to every directly or indirectly contained program (§12.3.4 GR1). "
        + "Move the entries to the outermost program.",
        "ISO §12.3.3 SR1 / §12.3.4 GR1");
    public static readonly DiagnosticDescriptor ReplaceOperandNotPseudoText = new(
        "COBOLNET1641", "replace-operand-not-pseudo-text", EditionSeverity.Error,
        "A REPLACE statement operand is not pseudo-text. REPLACE's general format (§7.2.4.2) admits "
        + "==pseudo-text== (and ==partial-word== under LEADING/TRAILING) operands only — a bare literal, "
        + "identifier or word is not a REPLACE operand in any ISO edition, and §7.2.4.3 SR7 explicitly bars "
        + "alphanumeric, boolean and national literals as partial-words. Write ==operand== (empty "
        + "==== deletes under LEADING/TRAILING).",
        "ISO §7.2.4.2 / §7.2.4.3 SR7");
    public static readonly DiagnosticDescriptor CopyReplacingNestedCopy = new(
        "COBOLNET1640", "copy-replacing-nested-copy", EditionSeverity.Error,
        "A COPY statement with the REPLACING phrase names library text that itself contains a COPY statement. "
        + "ISO §7.2.3.4 GR10: \"If the REPLACING phrase is specified, the library text shall not contain a "
        + "COPY statement\"; nesting (at least 5 levels) is permitted only without REPLACING (GR12). Flatten "
        + "the copybook, or drop the REPLACING phrase.",
        "ISO §7.2.3.4 GR10 / GR12");
    public static readonly DiagnosticDescriptor UndefinedReference = new(
        "COBOLNET1639", "undefined-reference", EditionSeverity.Error,
        "A statement references a name that no declaration in the source element defines, or that the written "
        + "qualifiers (or an unqualified ambiguity) leave unidentified. ISO §8.4.2.1: \"In order to use a "
        + "resource, a statement shall contain a reference that uniquely identifies that resource\"; §8.4.2.2 "
        + "requires qualification to establish uniqueness when spellings collide.",
        "ISO §8.4.2.1 / §8.4.2.2");
    // 1576 renumbered FROM a bare-literal "COBOLNET1573" in RefModZeroLengthDirectiveProcessor that collided with
    // ExternalFileStatusConsistency above (the P13 plan-vs-spec review finding C1, DEVLOG 907): the frontend emit
    // bypassed this catalog, so the Wave E catalog-only next-free scan could not see the claim. The descriptor now
    // lives HERE and the frontend emits via its Id — allocation stays catalog-visible.
    public static readonly DiagnosticDescriptor RefModZeroLengthMalformedOperand = new(
        "COBOLNET1576", "ref-mod-zero-length-malformed-operand", EditionSeverity.Error,
        "The >>REF-MOD-ZERO-LENGTH directive takes exactly one of the ON or OFF phrases (ISO §7.3.23.2; OFF is the "
        + "processor default in the absence of the directive) — any other operand is rejected, never silently "
        + "accepted.",
        "ISO §7.3.23.2 (VCR row 30)");
    // 1577 renumbered FROM a bare-literal "COBOLNET1518" in DataBinder that collided with the A.4.9 locale-module
    // non-support meaning (the P13 review batch-3 finding V11 — the THIRD collision of the class; 1518 stays
    // solely = locale non-support as CONFORMANCE.md item 25 documents).
    public static readonly DiagnosticDescriptor MethodRedefinesScope = new(
        "COBOLNET1577", "method-redefines-scope", EditionSeverity.Error,
        "A method data item's REDEFINES target shall be a preceding item in the SAME method scope — a method "
        + "item may not redefine object or program data (ISO §13.18.44.3).",
        "ISO §13.18.44.3");
    // ── Wave H — the §4.2.6 ¶3 / §4.2.13 RECOGNIZE-AND-NAME band. These are WARNINGS, not errors: the
    //    facilities are optional (§4.2.7) or processor-dependent (§4.2.6), so we need not implement them —
    //    but §4.2.6 ¶3 makes the compile-time warning MECHANISM mandatory ("shall provide a warning mechanism
    //    at compile time to indicate use of syntactically-detectable processor-dependent language elements not
    //    supported"), and §14.6.13.1.1 licenses raising NO exception conditions for them. So the program
    //    COMPILES, RUNS, and the facility is inert. Before this band these constructs produced a GENERIC parse
    //    error, which satisfied neither the warning obligation nor the "never a silent wrong answer" rule. ──
    public static readonly DiagnosticDescriptor McsFacilityUnsupported = new(
        "COBOLNET1578", "mcs-facility-unsupported", EditionSeverity.Warning,
        "The asynchronous messaging facility (SEND/RECEIVE, ISO §14.9.31/§14.9.38) is a processor-dependent "
        + "element (§4.2.6; Annex A.3 item 4) that is not supported — the statement is accepted but performs no "
        + "message I-O, and no EC-MCS-* condition is raised (§14.6.13.1.1). See docs/CONFORMANCE.md §4.",
        "ISO §4.2.6 ¶3 / Annex A.3 item 4 / §14.9.31 / §14.9.38", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor CommitRollbackUnsupported = new(
        "COBOLNET1579", "commit-rollback-unsupported", EditionSeverity.Warning,
        "The commit and rollback facility (COMMIT/ROLLBACK, ISO §14.9.7/§14.9.36) is a processor-dependent "
        + "element (§4.2.6; Annex A.3 items 6-7) that is not supported — the statement is accepted but performs "
        + "no transaction control and behaves as CONTINUE, and no EC-FLOW-COMMIT/ROLLBACK condition is raised "
        + "(§14.6.13.1.1). See docs/CONFORMANCE.md §4.",
        "ISO §4.2.6 ¶3 / Annex A.3 items 6-7 / §14.9.7 / §14.9.36", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ValidateFacilityUnsupported = new(
        "COBOLNET1580", "validate-facility-unsupported", EditionSeverity.Warning,
        "The VALIDATE facility (ISO §14.9.50) is an OPTIONAL element (§4.2.7; Annex A.4.14) and, at COBOL-2023, "
        + "additionally OBSOLETE (§4.2.13; Annex F.2 item 5) — it is not supported. The statement is accepted "
        + "but performs no content validation, and no EC-VALIDATE-* condition is raised (§14.6.13.1.1). Fires at "
        + "2002/2014/2023 (the facility exists from 2002); at --std 85 VALIDATE is a user word, not a statement. "
        + "See docs/CONFORMANCE.md §4.",
        "ISO §4.2.7 / Annex A.4.14 / §4.2.13 / Annex F.2 item 5 / §14.9.50", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor StrongGroupOrderingSignedLeaf = new(
        NotImplemented, "strong-group-ordering-signed-leaf", EditionSeverity.Error,
        "An ORDERING relation (<, >, <=, >=) between strongly-typed groups containing a SIGNED numeric "
        + "elementary item is legal (§8.8.4.2.3 SR4 restricts only boolean/message-tag/object/pointer contents) "
        + "but not yet implemented: §8.8.4.2.12 orders strongly-typed groups ELEMENT BY ELEMENT — a signed "
        + "numeric pair compares ALGEBRAICALLY (§8.8.4.2.4), which the whole-group character-image comparison "
        + "cannot honor (the overpunch/separate sign breaks lexical=algebraic). Equality and every "
        + "unsigned/alphanumeric-leaf ordering ARE carried by the image comparison (provably element-equivalent "
        + "for a fixed same-type profile).", "ISO §8.8.4.2.12 / §8.8.4.2.4", RecognizedNotImplemented);

    /// <summary>Every descriptor declared above (reflected, so a new field is picked up automatically by the
    /// <c>docs/DIAGNOSTICS.md</c> generator and the drift test — no hand-maintained list to forget).</summary>
    public static IReadOnlyList<DiagnosticDescriptor> All { get; } = typeof(DiagnosticCatalog)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
        .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
        .OrderBy(d => d.Code, StringComparer.Ordinal)
        .ThenBy(d => d.Id, StringComparer.Ordinal)
        .ToList();
}
