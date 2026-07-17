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
    public static readonly DiagnosticDescriptor DigitCapacityOver18Pre2002 = new(
        "COBOLNET0802", "digit-capacity-over-18-pre-2002", EditionSeverity.Error,
        "A fixed-point item/literal exceeds the 18-digit COBOL-85 limit (19–31 need --std 2002+).", "ISO §8.3.1.2");

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
    //    §13.16.3 SR3/SR6/SR13). 1540–1546 taken; 1550/1551/1552 earmarked (PHASE-12); 1560-band (PHASE-13). ──
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
    //    1550/1551/1552 stay earmarked (PHASE-12); 1553/1554 taken; 1558 = EXTERNAL type declarations. ──
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
    public static readonly DiagnosticDescriptor ReportNonDisplayItem = new(
        NotImplemented, "report-non-display-item", EditionSeverity.Error,
        "A non-DISPLAY printable report item is not supported.", "ISO §13.15", RecognizedNotImplemented);
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
    public static readonly DiagnosticDescriptor DebugRegisterFacility = new(
        NotImplemented, "debug-register-facility", EditionSeverity.Error,
        "The X3.23-1985 debug facility (DEBUG-ITEM registers, debugging-section invocation) is not implemented.",
        "VCR Table 7 row 7.17", RecognizedNotImplemented);
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
