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
        "A fixed-point item/literal exceeds the 31-digit ISO limit.", "ISO §8.3.3.3.2");
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
        "A fixed-point item/literal exceeds the 18-digit COBOL-85 limit (19–31 need --std 2002+).", "ISO §8.3.3.3.2");

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
    //    §13.16.3 SR3/SR6/SR13). 1540–1546 taken; 1550/1551/1552 are unallocated mid-band holes (the PHASE-12 earmark expired unused); 1560 = the Annex A.4.2 screen-handling refusal (below). ──
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
    // The ONE §14.9.4.3 SR15 diagnostic (kb/Work PB131) — both sentences: literal-1 required, and the name
    // resolved at BIND time against the containment tree (a directly contained program, or a visible common
    // program per §8.4.6.3). Replaced the 0899-staged CallAsNestedNeedsLiteral: this is a permanent
    // conformance rejection, not recognized-not-implemented debt.
    // The §13.18.44.3 SR12/SR14 REDEFINES class screen (kb/Work PB179 — the Step D Tier-D arm's bind
    // half): a permanent conformance rejection of the ENTRY-level shapes the rules name. The NESTED
    // pointer/object-leaf shape is NOT these rules' letter and takes ComputeTier's staged-loud arm instead.
    // The L1–L3 phrase-placement leniency family (kb/Work PB144), finally gated. THREE syntax rules across
    // READ/REWRITE/DELETE close a phrase out of a particular access mode or organization, and all three were
    // bound unconditionally with a "CCVS-lenient" comment and NO strict arm — so strict `--std 2023` accepted
    // source the standard forbids and said nothing. One reusable descriptor rather than three near-identical
    // ones, on the COBOLNET1694 precedent: the SHAPE is one rule ("a phrase is written where this statement's
    // syntax rules forbid it") and each site's message quotes its own §/SR. Emitted through
    // StatementValidation.ScreenForbiddenPhrase → EditionContext.Removed, so it is an ERROR under strict and a
    // WARNING with an UNCHANGED bind under --permissive, which is what keeps the CCVS-85 corpus compiling.
    public static readonly DiagnosticDescriptor IoPhraseForbiddenHere = new(
        "COBOLNET1720", "io-phrase-forbidden-here", EditionSeverity.Error,
        "An input-output statement specifies a phrase its syntax rules exclude for that file's organization or "
        + "access mode. ISO §14.9.10.3 syntax rule 2: \"The INVALID KEY and the NOT INVALID KEY phrases shall "
        + "not be specified for a DELETE RECORD statement that references a file that is in sequential access "
        + "mode.\" ISO §14.9.35.3 syntax rule 2: \"Neither the INVALID KEY phrase nor the NOT INVALID KEY "
        + "phrase shall be specified for a REWRITE statement that references a file with sequential "
        + "organization or a file with relative organization and sequential access mode.\" ISO §14.9.30.3 "
        + "syntax rule 6: \"None of the phrases ADVANCING, AT END, NEXT, NOT AT END, or PREVIOUS shall be "
        + "specified if ACCESS MODE RANDOM is specified in the file control entry for file-name-1.\" "
        + "Under --permissive the phrase is tolerated and bound with its pre-gate semantics (it is dead in the "
        + "status-first branches, never silently rerouted), which is the documented dialect leniency the "
        + "CCVS-85 corpus depends on.",
        "ISO §14.9.10.3 SR2 · §14.9.35.3 SR2 · §14.9.30.3 SR6");

    public static readonly DiagnosticDescriptor RedefinesPointerObject = new(
        "COBOLNET1697", "redefines-pointer-object", EditionSeverity.Error,
        "ISO §13.18.44.3 syntax rule 12: \"The REDEFINES clause shall not be specified for a data item of "
        + "class object, message-tag, or pointer or a strongly-typed group item\"; syntax rule 14: "
        + "\"Data-name-2 shall not be of class object, message-tag, or pointer, a strongly-typed group "
        + "item, or an item subordinate to a strongly-typed group item.\"",
        "ISO §13.18.44.3 SR12/SR14");
    // The §13.18.44.3 SR17 REDEFINES screen (kb/Work PB177 arm C — the decision-free half). SR17 is a
    // SYMMETRIC rule naming BOTH sides of the entry, and only its dynamic-CAPACITY half was screened (as
    // COBOLNET1525, and under SR5's number — SR5's own sentence is "Neither the original definition nor the
    // redefinition shall include an occurs-depending table", a DIFFERENT rule, which COBOLNET0855 already
    // enforces). The dynamic-LENGTH half was not merely unscreened but SILENTLY MIS-MODELLED: StorageFormPass
    // classifies IsDynamicLength BEFORE the Tier-B view arm, so `01 A PIC X(8). 01 B REDEFINES A. 05 D PIC X
    // DYNAMIC LENGTH.` gave the view its OWN disjoint native string — two storages for one shared area, no
    // diagnostic (measured: MOVE "ZZ" TO D left A unchanged, violating §13.18.44.4 GR1). "Variable-length
    // group" is §8.5.1.12.1's defined term: a group with a dynamic-length elementary item or a
    // dynamic-capacity table subordinate.
    public static readonly DiagnosticDescriptor RedefinesVariableLength = new(
        "COBOLNET1698", "redefines-variable-length", EditionSeverity.Error,
        "ISO §13.18.44.3 syntax rule 17: \"Neither data-name-2 nor the subject of the entry shall be a "
        + "variable-length group or a dynamic-length elementary item.\" A variable-length group is "
        + "\"a group item whose data description has at least one dynamic-length elementary item or "
        + "dynamic-capacity table as a subordinate item\" (ISO §8.5.1.12.1).",
        "ISO §13.18.44.3 SR17");

    public static readonly DiagnosticDescriptor CallAsNestedScope = new(
        "COBOLNET1676", "call-as-nested-scope", EditionSeverity.Error,
        "CALL … AS NESTED names its program by literal-1. ISO §14.9.4.3 syntax rule 15: \"If the NESTED phrase "
        + "is specified, literal-1 shall be specified. Literal-1 shall be the same as the program-name specified "
        + "in a PROGRAM-ID paragraph of a common program as specified in 8.4.6.3, Scope of program-names, or of "
        + "a program that is directly contained in the calling program.\"",
        "ISO §14.9.4.3 SR15");
    // ── The PB132 CALL operand screens (kb/Work PB132): each predicate existed in the tree and was
    // unreached from CALL — these are the cited rejections for the shapes the binder silently accepted. ──
    public static readonly DiagnosticDescriptor CallOperandSection = new(
        "COBOLNET1677", "call-operand-section", EditionSeverity.Error,
        "A CALL operand shall reference a data item defined in the file, working-storage, local-storage, or "
        + "linkage section — ISO §14.9.4.3 syntax rule 3 for an argument, rule 7 for the RETURNING item. A "
        + "SCREEN SECTION entry is not such an item (and the SCREEN SECTION is an unsupported optional "
        + "facility — COBOLNET1560).",
        "ISO §14.9.4.3 SR3/SR7");
    public static readonly DiagnosticDescriptor CallByReferenceObjectData = new(
        "COBOLNET1678", "call-by-reference-object-data", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 3: \"If the BY REFERENCE phrase is specified or implied, identifier-2 "
        + "shall not be defined in the working-storage or file section of a factory or an instance object.\" "
        + "Object data is shared state; only BY CONTENT or BY VALUE may carry it out of a method's CALL.",
        "ISO §14.9.4.3 SR3");
    public static readonly DiagnosticDescriptor CallByReferenceOperandKind = new(
        "COBOLNET1679", "call-by-reference-operand-kind", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 10 (Format 1): \"If the BY REFERENCE phrase is specified or implied for "
        + "an identifier-2, that identifier shall be neither a strongly-typed group item nor a data item of "
        + "class object or pointer.\" A prototype-less callee cannot preserve the type discipline these items "
        + "carry; Format 2 admits them under §14.8.2's conformance rules.",
        "ISO §14.9.4.3 SR10");
    public static readonly DiagnosticDescriptor CallVariableLengthGroup = new(
        "COBOLNET1680", "call-variable-length-group", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 12 (Format 1): \"Identifier-2 shall not reference a variable-length "
        + "group.\" A group with a DYNAMIC LENGTH elementary item or a dynamic-capacity table subordinate to "
        + "it (§8.5.1.12.1) has no prototype-less byte image to pass.",
        "ISO §14.9.4.3 SR12 / §8.5.1.12.1");
    public static readonly DiagnosticDescriptor CallTargetCategory = new(
        "COBOLNET1681", "call-target-category", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 1: \"Identifier-1 shall be defined as an alphanumeric, national, or "
        + "program-pointer data item.\" Any other identifier target (numeric, boolean, alphabetic, index, "
        + "object) is not a program name carrier.",
        "ISO §14.9.4.3 SR1");
    public static readonly DiagnosticDescriptor CallAsNestedContext = new(
        "COBOLNET1682", "call-as-nested-context", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 13 (Format 2): \"The NESTED phrase may be specified only in a program "
        + "definition.\" A function, method, or interface definition contains no programs, so AS NESTED has "
        + "nothing to name there.",
        "ISO §14.9.4.3 SR13");
    public static readonly DiagnosticDescriptor CallBitAlignment = new(
        "COBOLNET1683", "call-bit-alignment", EditionSeverity.Error,
        "A bit data item passed BY REFERENCE (ISO §14.9.4.3 syntax rule 6) or used as the CALL RETURNING "
        + "item (rule 8) shall be aligned on a byte boundary, and its subscripts and reference-modification "
        + "leftmost position shall consist of only fixed-point numeric literals or all-literal arithmetic "
        + "expressions without exponentiation — the referenced address must be statically byte-aligned.",
        "ISO §14.9.4.3 SR6/SR8 / §8.5.1.6.3");
    public static readonly DiagnosticDescriptor CallArgumentCount = new(
        "COBOLNET1684", "call-argument-count", EditionSeverity.Error,
        "ISO §14.8.2.1: \"The number of arguments in the activating element shall be equal to the number of "
        + "formal parameters in the activated element, with the exception of trailing formal parameters that "
        + "are specified with an OPTIONAL phrase in the procedure division header of the activated element and "
        + "omitted from the list of arguments of the activating element.\" With AS NESTED the callee's header "
        + "is known at compile time, so the mismatch is a diagnostic here rather than a run-time "
        + "EC-PROGRAM-ARG-MISMATCH.",
        "ISO §14.8.2.1");
    public static readonly DiagnosticDescriptor CallOmittedNeedsOptional = new(
        "COBOLNET1685", "call-omitted-needs-optional", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 24: \"If the OMITTED phrase is specified, the OPTIONAL phrase shall be "
        + "specified for the corresponding formal parameter in the procedure division header.\"",
        "ISO §14.9.4.3 SR24");
    public static readonly DiagnosticDescriptor OmittedConditionOperand = new(
        "COBOLNET1686", "omitted-condition-operand", EditionSeverity.Error,
        "ISO §8.8.4.8 syntax rule 1: \"Data-name-1 shall be a formal parameter defined in the source element "
        + "in which this condition is specified.\" The omitted-argument condition asks whether an argument was "
        + "provided to THIS program, function, or method — an ordinary data item has no such property.",
        "ISO §8.8.4.8 SR1");
    public static readonly DiagnosticDescriptor CallArgumentMode = new(
        "COBOLNET1687", "call-argument-mode", EditionSeverity.Error,
        "The argument's passing mode does not match its corresponding formal parameter's. ISO §14.9.4.3 "
        + "syntax rule 19: with BY CONTENT or BY REFERENCE specified or implied for an argument, BY REFERENCE "
        + "shall be specified or implied for the corresponding formal parameter; rule 21: with BY VALUE "
        + "specified for an argument, BY VALUE shall be specified for the corresponding formal parameter. With "
        + "AS NESTED the callee's header is known at compile time, so the mismatch is a diagnostic here.",
        "ISO §14.9.4.3 SR19/SR21");
    public static readonly DiagnosticDescriptor CallArgumentConformance = new(
        "COBOLNET1688", "call-argument-conformance", EditionSeverity.Error,
        "A BY REFERENCE argument does not conform to its corresponding formal parameter. ISO §14.8.2.3.2 "
        + "(elementary, the NESTED-call regime): the definitions \"shall have the same ALIGN, BLANK WHEN ZERO, "
        + "DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses\"; §14.8.2.2 (groups): an alphanumeric "
        + "group or elementary alphanumeric formal of the same or smaller byte count, strongly-typed pairs of "
        + "the same type. The violation is EC-PROGRAM-ARG-MISMATCH at run time; with AS NESTED it is a "
        + "diagnostic at compile time.",
        "ISO §14.8.2.2 / §14.8.2.3.2");
    public static readonly DiagnosticDescriptor ArithmeticFormatOperand = new(
        "COBOLNET1689", "arithmetic-format-operand", EditionSeverity.Error,
        "An arithmetic statement's operand does not fit the format its phrases select. The GIVING forms of "
        + "ADD / SUBTRACT / MULTIPLY / DIVIDE print ONE sending `{identifier | literal}` TO/FROM/BY/INTO "
        + "operand with no ROUNDED; the non-GIVING forms print receiving identifiers only, so a literal or "
        + "function-identifier operand there is illegal; and every BY form of DIVIDE prints GIVING. The old "
        + "binders silently dropped the extra operands and the ROUNDED, or crashed.",
        "ISO §14.9.2.2 / §14.9.44.2 / §14.9.26.2 / §14.9.12.2");
    public static readonly DiagnosticDescriptor CommitRollbackContext = new(
        "COBOLNET1690", "commit-rollback-context", EditionSeverity.Error,
        "A COMMIT or ROLLBACK statement in a context its syntax rules ban: \"This statement shall not be "
        + "specified in a recursive source element\" (ISO §14.9.7.3 SR1 / §14.9.36.3 SR1 — a function or "
        + "method is always recursive, §8.6.6) and \"shall not be specified in the input or output procedure "
        + "of a MERGE or file SORT statement\" (SR2 of both).",
        "ISO §14.9.7.3 / §14.9.36.3");
    public static readonly DiagnosticDescriptor AcceptVariableLengthGroup = new(
        "COBOLNET1691", "accept-variable-length-group", EditionSeverity.Error,
        "An ACCEPT receiver references a variable-length group: a group with a DYNAMIC LENGTH elementary item "
        + "or a dynamic-capacity table subordinate to it at any depth (ISO §8.5.1.12). \"Neither identifier-1 "
        + "nor identifier-2 shall reference a variable-length group\" — both the device and the temporal "
        + "format exclude it.",
        "ISO §14.9.1.3");
    public static readonly DiagnosticDescriptor SortMergeFileInIoStatement = new(
        "COBOLNET1692", "sd-file-io-statement", EditionSeverity.Error,
        "An input-output statement names a sort-merge (SD) file. \"A sort-merge file is referenced only by a "
        + "SORT, MERGE, RELEASE, or RETURN statement\" (and a SORT/MERGE USING/GIVING) — CLOSE, DELETE, DELETE "
        + "FILE, OPEN, READ, REWRITE, START, UNLOCK and WRITE all reject it at compile time. The old posture "
        + "let CLOSE and the DELETE forms compile and run against an unregistered connector, whose fail-open "
        + "status read '00'.",
        "ISO §13.4.6.3");
    public static readonly DiagnosticDescriptor ClosePhraseOrganization = new(
        "COBOLNET1693", "close-phrase-organization", EditionSeverity.Error,
        "A CLOSE statement's NO REWIND, REEL, or UNIT phrase on a file whose organization is not sequential: "
        + "\"The NO REWIND, REEL, and UNIT phrases may be used only with files that are of sequential "
        + "organization.\" The WITH LOCK phrase is not organization-restricted. The old acceptance degraded at "
        + "run time to a stale FILE STATUS value with no defined meaning.",
        "ISO §14.9.6.3");
    public static readonly DiagnosticDescriptor BasedRecordSubstrate = new(
        "COBOLNET1695", "based-record-substrate", EditionSeverity.Error,
        "A BASED record has a subordinate item the shared byte cell cannot carry — a NATIONAL leaf (two bytes "
        + "per character position over a byte-addressed cell), a USAGE BIT leaf (the bit-packing residue), or a "
        + "pointer-class leaf (POINTER/PROGRAM-POINTER/FUNCTION-POINTER/OBJECT-REFERENCE, which has no byte "
        + "form at all): the ALLOCATE/ADDRESS pointer bridge is recognized but not yet implemented for it "
        + "(kb/Work PB164). Every NUMERIC leaf now rides the cell on its pinned byte form — DISPLAY, BINARY, "
        + "PACKED-DECIMAL, COMP-5, the IEEE float family and USAGE INDEX all have one — so a COMP leaf no "
        + "longer draws this. Previously the class was rejected SILENTLY at bind and the program crashed at run "
        + "time on its first ALLOCATE, while the EXTERNAL twin of the same failure always diagnosed at bind.",
        "ISO §13.18.5 / §14.9.3");
    public static readonly DiagnosticDescriptor CancelTargetCategory = new(
        "COBOLNET1696", "cancel-target-category", EditionSeverity.Error,
        "ISO §14.9.5.3 syntax rule 1: \"Identifier-1 shall be defined as an alphanumeric or national data "
        + "item.\" CANCEL's admitted list is NARROWER than CALL's (§14.9.4.3 SR1 also admits a "
        + "program-pointer item — COBOLNET1681's rule); any other identifier target is not a program-name "
        + "carrier (kb/Work PB154: CANCEL of a numeric item compiled clean and no-opped silently).",
        "ISO §14.9.5.3 SR1");
    public static readonly DiagnosticDescriptor OperandClassExcluded = new(
        "COBOLNET1694", "operand-class-excluded", EditionSeverity.Error,
        "A statement operand is of a class its syntax rules exclude. The recurring shape (kb/Work PB148): "
        + "\"Identifier-1 shall not reference a data item of class …\" closes a class list per statement "
        + "(DISPLAY's §14.9.11.3 SR1 excludes message-tag, object and pointer — where 'class pointer' spans "
        + "the data-, function- and program-pointer categories), and §13.18.60.3 SR10 closes the reference "
        + "contexts for an index DATA item. The reusable gate also rejects the word NULL in such slots — NULL "
        + "is the predefined object reference/address (§8.4.3.7/§8.4.3.10), not a §8.3.3.6.2 figurative "
        + "constant. The old fall-throughs printed a pointer's CLR carrier text, an object's ToString, an "
        + "index item's EMPTY zero-digit image, and U+0000 for NULL.",
        "ISO §14.9.11.3 / §13.18.60.3");
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
    //    COBOLNET1560 is NO LONGER an earmark: it is the Annex A.4.2 screen-handling refusal, declared with its
    //    procedure-division twin COBOLNET1707 at the end of this file (kb/Work PB260). ──
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
    // ⛔ CITATION REPAIRED (kb/Work PB177 arm C). This descriptor and its two message sites all cited
    // "ISO §13.18.16.3 SR3" for an UNRESOLVABLE CONTROL operand — a REAL clause answering a DIFFERENT
    // question: SR3 is "Data-name-1 shall not be subject to any OCCURS clauses." Nothing in §13.18.16.3
    // governs resolution failure; that is ordinary name resolution, §8.4.2.1. `cite.py --check` on the
    // clause NUMBER alone would have passed either way — the exact failure mode CLAUDE.md rule 1 names.
    public static readonly DiagnosticDescriptor ReportControlOperandUnresolved = new(
        NotImplemented, "report-control-operand-unresolved", EditionSeverity.Error,
        "A CONTROL operand does not resolve to a data item.", "ISO §8.4.2.1");
    // The §13.18.16.3 CONTROL-operand SYNTAX RULES over the SHAPE of data-name-1 (kb/Work PB177 arm C) —
    // ONE screen, one arm per rule, so the next rule on this clause drops in beside them instead of
    // becoming a fourth place the clause is enforced. All three were unscreened and MEASURED so: SR3 (an
    // operand subject to an OCCURS clause) and SR5 (an operand with an occurs-depending table subordinate)
    // compiled and ran silently; SR7 (a variable-length group) compiled and staged a RUNTIME Tier-C loud,
    // where a SYNTAX rule requires a compile-time rejection.
    public static readonly DiagnosticDescriptor ReportControlOperandShape = new(
        "COBOLNET1699", "report-control-operand-shape", EditionSeverity.Error,
        "ISO §13.18.16.3 syntax rule 3: \"Data-name-1 shall not be subject to any OCCURS clauses.\"; "
        + "syntax rule 5: \"The entry specified by data-name-1 shall not have an occurs-depending table "
        + "subordinate to it.\"; syntax rule 7: \"Data-name-1 shall not reference a variable-length group.\"",
        "ISO §13.18.16.3 SR3/SR5/SR7");
    // An INDEX CONTROL operand is illegal for a reason that lives OUTSIDE §13.18.16.3, which is why it gets its
    // own code rather than riding COBOLNET1699's clause list (kb/Work PB177 arm C follow-up): §13.18.60.3 SR10
    // closes the set of contexts in which an index data item may be referenced explicitly, and §8.4.5 makes a
    // data-division clause naming a data item exactly such an explicit reference. It was a RUNTIME loud —
    // ReportWriterEmitter's §13.18.16.4 GR3 "no character image" backstop — where a syntax rule requires the
    // compile-time rejection. (The FLOAT operand of the same runtime guard is NOT this: no syntax rule bars it,
    // and it stays loud because the RESTORE half has no float channel — see that guard's own comment.)
    public static readonly DiagnosticDescriptor ReportControlOperandIndex = new(
        "COBOLNET1700", "report-control-operand-index", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 10: \"An index data item may be referenced explicitly only in a SEARCH or "
        + "SET statement, a relation condition, an intrinsic function argument, an inline method invocation "
        + "argument, the USING phrase of a procedure division header, or the USING phrase of a CALL or INVOKE "
        + "statement.\" A CONTROL clause naming the item is an explicit reference (§8.4.5).",
        "ISO §13.18.60.3 SR10 / §8.4.5");
    // ⛔ §13.18.44.3 SR5 SENTENCE 1, RESTORED AS THE OBJECT-SIDE AUTHORITY (kb/Work PB177 arm C follow-up).
    // SR5 is FOUR sentences and each side of this family quotes a different one. Sentence 4 ("Neither the
    // original definition nor the redefinition shall include an occurs-depending table") is COBOLNET0855's, and
    // reading only that one produced the claim that the OCCURS-bearing data-name-2 "NO syntax rule literally
    // names" — which is false: sentence 1 is "The data description entry for data-name-2 shall not contain an
    // OCCURS clause", and OCCURS DYNAMIC is Format 4 OF THE OCCURS CLAUSE (§13.18.38), so sentence 1 covers the
    // dynamic-capacity object as squarely as the fixed one. That claim was true only of the SUBJECT side, which
    // keeps COBOLNET1525's §13.18.44.4 GR1 / §8.5.1.9.1 storage-model reasoning.
    // Measured before this screen: `05 T PIC X(3) OCCURS 4. 05 R REDEFINES T PIC X(12).` compiled CLEAN and ran.
    public static readonly DiagnosticDescriptor RedefinesTargetOccurs = new(
        "COBOLNET1701", "redefines-target-occurs", EditionSeverity.Error,
        "ISO §13.18.44.3 syntax rule 5, sentence 1: \"The data description entry for data-name-2 shall not "
        + "contain an OCCURS clause.\" (Sentence 2 permits data-name-2 to be SUBORDINATE to such an item; that "
        + "is a different shape and is not screened here. Sentence 4's occurs-depending prohibition is "
        + "COBOLNET0855's, and owns the entries where data-name-2 IS an occurs-depending table.)",
        "ISO §13.18.44.3 SR5");
    // ⛔ THE GROUP-LEVEL VALUE SUBORDINATE SCREEN (kb/Work PB184). §13.18.63.3 SR13/SR14 restrict what may sit
    // UNDER an entry that carries a group-level VALUE, and NONE of the three restrictions existed: measured on
    // 8ca74a3d, `01 GV VALUE "40537". 05 GB PIC 9 COMP-5 OCCURS 5.` compiled CLEAN and left every occurrence
    // ZERO; so did a JUSTIFIED / SYNCHRONIZED subordinate and a subordinate carrying its own VALUE.
    // ⚠ PB184 was registered as a §13.18.63.4 GR5 DISTRIBUTION gap — "the group area is initialized without
    // consideration for the individual elementary or group items contained within this group", so the byte-form
    // leaves should take their slice of the literal's BYTES. That premise is refuted by SR14: a COMP-5 leaf
    // under an alphanumeric group item carrying a VALUE is not usage DISPLAY, so the program is NOT CONFORMING
    // and there is no area to distribute. GR5 is already implemented for the population SR14 admits (every
    // subordinate usage DISPLAY, where the character image IS the byte image). The missing half was the
    // DIAGNOSTIC on the complement, never a widening of the distributor — the
    // [[validate_the_premise_not_only_the_rule]] shape.
    // SR14 is TWO conjuncts in one sentence and SR13's second sentence is the third restriction on the same
    // subject; all three live here so a future case is automatic rather than a fourth unscreened arm.
    public static readonly DiagnosticDescriptor GroupValueSubordinate = new(
        "COBOLNET1702", "group-value-subordinate", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 14: \"If a VALUE clause is specified at the group level, subordinate items "
        + "within that group shall not be described with a JUSTIFIED or SYNCHRONIZED clause, and all data items "
        + "subordinate to an alphanumeric group item shall be explicitly or implicitly described with usage "
        + "DISPLAY.\"; syntax rule 13, sentence 2: \"The VALUE clause shall not be specified at subordinate "
        + "levels within this group.\" (Syntax rule 16 applies both to the format 2 (table) VALUE.)",
        "ISO §13.18.63.3 SR13/SR14");
    // The SUBJECT half of the same screen. SR1 restricts what the VALUE-carrying entry may BE, and it is the
    // rule that keeps SR14's second conjunct honest: §13.18.29.4 GR3 makes a group an ALPHANUMERIC group item
    // only when it is "not strongly typed and is not a variable-length group", so without SR1 those two shapes
    // reach the SR14 usage arm and are rejected under a rule that does not govern them. Measured (probe, this
    // landing): `01 ST IS TYPEDEF STRONG VALUE "ABCD". 05 SY PIC 9(4) COMP.` and `01 GV VALUE "ABCDE".
    // 05 GB PIC 9(4) COMP OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 5.` each drew COBOLNET1702 naming SR14,
    // while the rule they actually violate — SR1 — was unenforced.
    public static readonly DiagnosticDescriptor GroupValueSubjectShape = new(
        "COBOLNET1703", "group-value-subject-shape", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 1: \"The subject of the entry shall not be a strongly-typed group item or "
        + "a variable-length group.\" (A variable-length group is §8.5.1.12.1's — a group with a dynamic-length "
        + "elementary item or a dynamic-capacity table subordinate to it.)",
        "ISO §13.18.63.3 SR1");
    // A group-level VALUE whose area is BIT-PACKED (kb/Work PB207). §13.18.63.4 GR5 initializes "the group
    // area"; for a group with a USAGE BIT descendant that area is ceil(bits/8) PACKED bytes laid out by the
    // §8.5.1.6.3 walk, not a character run, so the positional CHARACTER slice both initializer lanes implement
    // has no meaning over it. Measured on 8ca74a3d and unchanged by PB184's widening: the multi-member shape
    // `01 BG GROUP-USAGE BIT VALUE B"1010". 05 B1 PIC 1(2). 05 B2 PIC 1(2).` CRASHED the emitter with an
    // unhandled ArgumentOutOfRangeException, and the single-member shape silently stored ONE boolean position
    // where the literal has four. Staged LOUD rather than either: a wrong answer that compiles is worse than a
    // named refusal, and PB207 carries the real fix (a boolean-position area rule for both lanes).
    public static readonly DiagnosticDescriptor BitGroupLevelValue = new(
        NotImplemented, "bit-group-level-value", EditionSeverity.Error,
        "A group-level VALUE clause on a group whose area is bit-packed (a USAGE BIT item is subordinate to it) "
        + "is recognized but not yet implemented.", "ISO §13.18.63.4 GR5 / §8.5.1.6.3", RecognizedNotImplemented);
    // ⛔ THE TERMINATION-STATUS PHRASE HAS ITS OWN SYNTAX RULES, and none of them existed (kb/Work PB169). The
    // operand was bound through the ARITHMETIC funnel, so §8.8.1.1 — a rule that does not govern this position —
    // rejected the two shapes the position's own rules explicitly admit: measured on 9a89fbd1, both
    // `STOP RUN WITH ERROR STATUS "ABEND"` (SR3's conditional presupposes the non-numeric literal; SR4 bars only
    // a zero-length one) and `STOP RUN WITH ERROR STATUS WS-CODE` with `WS-CODE PIC X(3)` (SR2 admits a data item
    // "with usage display") drew COBOLNET0844. Meanwhile the position's ACTUAL rules went unenforced in both
    // directions: `STATUS ""`, `STATUS 1.5` and `STATUS <index-name>` all compiled clean. This code carries the
    // rules; the §8.8.1.1 screen no longer reaches the site at all.
    public static readonly DiagnosticDescriptor TerminationStatusOperand = new(
        "COBOLNET1704", "termination-status-operand", EditionSeverity.Error,
        "ISO §14.9.42.3 syntax rule 2: \"Identifier-1 shall reference an integer data item or a data item with "
        + "usage display or usage national.\"; syntax rule 3: \"If literal-1 is numeric, it shall be an "
        + "integer.\"; syntax rule 4: \"Literal-1 shall not be a zero-length literal.\" (GOBACK's §14.9.18.3 "
        + "SR6/SR7/SR8 are the same three rules for the same shared phrase, over identifier-2 — GOBACK's "
        + "identifier-1 is the RAISING object.) The code also carries the slot's ADMISSIBILITY where the "
        + "operand is neither identifier-1 nor literal-1: NULL is a predefined address / object reference "
        + "(§8.4.3.10.1) whose §8.4.3.10.3 SR1 admits it only in INITIALIZE/SET, a prototype argument, or a "
        + "pointer-or-object-reference relation condition.",
        "ISO §14.9.42.3 SR2/SR3/SR4");
    // ── The §13.18.60.3 USAGE DECLARATION-PLACEMENT family (kb/Work PB183) ────────────────────────────────
    // Three syntax rules about WHERE a usage phrase may be written, none of which existed anywhere in the
    // compiler. SR14 is the headline: measured on 2acbd842, `01 G. 05 P USAGE POINTER.` compiled and ran, as
    // did a pointer member of a WEAK typedef template and a `05 Q SAME AS P` copy of a level-1 pointer. The
    // rule was verified against the PRINTED page (folio 505, PDF 535) before the screen was written — the
    // reading is restrictive enough that the falsely-restrictive-OCR hazard had to be excluded, and it was:
    // the transcription is character-for-character the printed rule.
    //
    // ⛔ SR14's list OMITS INDEX while the neighbouring SR4 INCLUDES it. That is deliberate drafting, not an
    // oversight to "unify": `05 IX USAGE INDEX.` inside an ordinary group is LEGAL and a positive golden pins
    // it. The two rules take two descriptors and two predicates for exactly that reason.
    public static readonly DiagnosticDescriptor UsageDeclarationPlacement = new(
        "COBOLNET1724", "usage-declaration-placement", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 14: \"A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, "
        + "FUNCTION-POINTER, or PROGRAM-POINTER phrase may be specified only for an elementary data item at "
        + "level 1 or an elementary data item subordinate to a type declaration that includes the STRONG "
        + "phrase.\" (A level-77 entry satisfies the first arm: §13.11.1 makes the level-1 and level-77 "
        + "spellings ALTERNATIVES for one data element that \"bear[s] no hierarchical relationship to any "
        + "other data item\", and §8.5.1.3.2 puts a 77 entry outside the level system altogether — \"three "
        + "types of entries exist for which there is no true concept of level\". The group form is reached "
        + "through §13.18.60.4 GR1, which applies a group's usage \"only to each elementary item in the "
        + "group\" — at that item's own level.)",
        "ISO §13.18.60.3 SR14");
    public static readonly DiagnosticDescriptor UsageObjectReferenceFileSection = new(
        "COBOLNET1725", "usage-object-reference-file-section", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 15: \"The USAGE OBJECT REFERENCE clause shall not be specified in the "
        + "file section.\" (The SAME AS twin — §13.18.49.3 SR6, a file-section SAME AS whose data-name-1 "
        + "description contains an object reference — is COBOLNET1556; this is the DIRECT declaration arm, "
        + "which had no screen at all.)",
        "ISO §13.18.60.3 SR15");
    public static readonly DiagnosticDescriptor UsageConstantRecord = new(
        "COBOLNET1726", "usage-constant-record", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 4: \"The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, "
        + "and PROGRAM-POINTER phrases shall not be specified in a data item described with the CONSTANT "
        + "RECORD clause, or in any item subordinate to a data item described with the CONSTANT RECORD "
        + "clause.\" (SIX phrases — INDEX is in THIS rule's list and NOT in SR14's.)",
        "ISO §13.18.60.3 SR4");
    // The OPTIONS INITIALIZE clause's ONE syntax rule (kb/Work PB152). ⛔ "hexadecimal-alphanumeric literal" is a
    // DEFINED TERM, not loose wording for "alphanumeric or hex": §8.3.3.2.2 gives the alphanumeric literal exactly
    // two formats — format 1 `"…"` / `'…'` and format 2 `X"…"` / `X'…'` — and "hexadecimal-alphanumeric" names
    // format 2. So `INITIALIZE ALL TO "Z"` is NOT a conforming spelling; `X"5A"` is. Measured on 2acbd842, the
    // decoder took `raw[0]` for ANY shape, so `INITIALIZE ALL TO "AB"` silently became 'A' — a fill character the
    // program never asked for, from a literal the standard does not admit. (PB151's own golden was written with
    // the format-1 spelling and is repaired to X"51" by this landing.)
    public static readonly DiagnosticDescriptor OptionsInitializeFillLiteral = new(
        "COBOLNET1727", "options-initialize-fill-literal", EditionSeverity.Error,
        "ISO §11.9.10.3 syntax rule 1: \"Literal-1 shall specify a one-byte hexadecimal-alphanumeric literal.\" "
        + "(A hexadecimal-alphanumeric literal is §8.3.3.2.2's FORMAT 2 — X\"…\" or X'…' — whose "
        + "hex-character-sequence \"shall be composed of hexadecimal digits\" (§8.3.3.2.3 SR5). One byte is "
        + "exactly two hexadecimal digits.)",
        "ISO §11.9.10.3 SR1");
    // ⛔ THE THREE SCREENS THAT NARROW THE USAGE CLAUSE'S FLOAT FORMAT PHRASES (kb/Work PB174). The grammar
    // parses `floatFormatPhrase*` after ANY usageKeyword — the established binarySign / noSignPhrase posture, a
    // superset parse the binder narrows (DESIGN-version-conformance-pipeline's parse-wide/bind-narrow direction).
    // The general format is what scopes each phrase: §13.18.60.2 prints the endianness-phrase ONLY on
    // FLOAT-BINARY-32/-64/-128 and FLOAT-DECIMAL-16/-34, and the encoding-phrase ONLY on FLOAT-DECIMAL-16/-34
    // (verified against the PRINTED page, PDF p.533 = printed 503). GR19c/d and GR20c corroborate by naming the
    // usage families the OPTIONS clauses supply the IMPLIED phrase for.
    public static readonly DiagnosticDescriptor UsageEndiannessPhraseScope = new(
        "COBOLNET1716", "usage-endianness-phrase-scope", EditionSeverity.Error,
        "ISO §13.18.60.2 general format: the endianness-phrase is written only with the standard floating-point "
        + "usages — FLOAT-BINARY-32, FLOAT-BINARY-64, FLOAT-BINARY-128, FLOAT-DECIMAL-16 and FLOAT-DECIMAL-34. "
        + "§13.18.60.4 general rule 19 scopes its meaning the same way: c) \"For the standard binary "
        + "floating-point usages, if neither the HIGH-ORDER-LEFT phrase nor the HIGH-ORDER-RIGHT phrase is "
        + "specified, 11.9.8, FLOAT-BINARY clause, specifies which of these phrases is implied.\" and d) its "
        + "standard-decimal twin. The implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/"
        + "-EXTENDED) are outside both — GR13/GR21 leave their representation to the implementor, and COBOL.NET "
        + "pins them big-endian (Annex A.1 item 48).",
        "ISO §13.18.60.2 / §13.18.60.4 GR19c-d");
    public static readonly DiagnosticDescriptor UsageEncodingPhraseScope = new(
        "COBOLNET1717", "usage-encoding-phrase-scope", EditionSeverity.Error,
        "ISO §13.18.60.2 general format: the encoding-phrase is written only with the standard DECIMAL "
        + "floating-point usages, FLOAT-DECIMAL-16 and FLOAT-DECIMAL-34 — it is absent from every other line of "
        + "the figure, the standard BINARY float usages included. §13.18.60.4 general rule 20a says the same in "
        + "prose: \"The BINARY-ENCODING phrase specifies that the encoding of the information in a data item "
        + "described with any standard decimal floating-point usage is the binary encoding as specified in "
        + "ISO/IEC 60559:2020, 3.5.\"",
        "ISO §13.18.60.2 / §13.18.60.4 GR20");
    public static readonly DiagnosticDescriptor UsageFloatFormatPhraseRepeated = new(
        "COBOLNET1718", "usage-float-format-phrase-repeated", EditionSeverity.Error,
        "ISO §5.2.6.4, Choice indicators: \"When enclosed by brackets, zero or more of the alternatives "
        + "contained within the choice indicators shall be specified, but any single alternative may be "
        + "specified only once.\" — the FLOAT-DECIMAL-16/-34 phrase group of the §13.18.60.2 general format is "
        + "exactly such a bracketed choice-indicator group over { encoding-phrase, endianness-phrase }, so each "
        + "phrase may appear at most once (in either order, per the same clause: \"The alternatives may be "
        + "specified in any order.\"). The standard BINARY float usages carry a single bracketed "
        + "endianness-phrase, which is likewise at most one.",
        "ISO §5.2.6.4 / §13.18.60.2");
    // ⛔ THE EXPRESSION FORMATION TABLES (kb/Work PB158). §8.8.1.2 Table 3 and §8.8.2 Table 4 each state which
    // ordered pairs of adjacent symbols an expression may contain; a '—' cell is an invalid pair. Most cells are
    // excluded structurally by the expression tiers — MEASURED, one probe per cell: ten of Table 3's thirteen are
    // already hard parse errors and two more cannot form at all, because COBOL reads a '(' after an identifier as
    // a subscript. The tiers admit exactly one cell from each table, and this code carries both, because they are
    // one rule ("an invalid adjacent pair") over two tables. The arithmetic cell CANNOT be closed in the grammar:
    // §8.3.3.3.2 rule 2 makes an ADJACENT sign part of the numeric literal, so `- -2` is the permissible
    // (unary, literal) pair while `- - 2` is the invalid (unary, unary) one — and in the default lexer mode both
    // emit MINUS MINUS INTEGERLIT, so only the TOKEN POSITIONS separate them. A tier rejecting both would reject
    // legal source, the worse failure.
    public static readonly DiagnosticDescriptor ExpressionFormationPair = new(
        "COBOLNET1719", "expression-formation-pair", EditionSeverity.Error,
        "ISO §8.8.1.2 Table 3, Combinations of symbols in arithmetic expressions: \"The letter 'P' indicates a "
        + "permissible pair of symbols. The character '—' indicates an invalid pair.\" Row \"Unary + or −\" × "
        + "column \"Unary + or −\" is '—', so a unary operator may not be immediately followed by another unary "
        + "operator. §8.8.2 Table 4 is the boolean counterpart: its B-NOT row × B-NOT column is likewise '—', and "
        + "§8.8.4.11.3's Table 5 NOTE states the same restriction for conditions outright — \"the pair 'NOT NOT' "
        + "is not permissible\". The sign-adjacency carve-out is §8.3.3.3.2 rule 2: a numeric literal is a "
        + "character-string and \"If a sign is used, it shall appear as the leftmost character of the literal\", "
        + "so a sign written against the digits belongs to the literal and forms the PERMISSIBLE (unary, literal) "
        + "pair instead.",
        "ISO §8.8.1.2 Table 3 / §8.8.2 Table 4");
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
        "A RECURSIVE program that directly contains programs and declares WORKING-STORAGE or a FILE SECTION "
        + "is recognized but not yet implemented — the shared-static storage model (one last-used copy "
        + "across activations, §8.6.4 covering both sections; kb/Work PB168) does not yet compose with "
        + "contained-program GLOBAL/__outer bridges.",
        "ISO §13.5.4 GR1 / §8.6.4 / §14.6.2.3.3 / §13.18.27 GR2", RecognizedNotImplemented);
    // (RefModBitGroupSlice was DELETED by kb/Work PB173, which implemented the model it deferred: a bit group's
    // reference modification is a BitImagePlace over the UNPACKED boolean string, so the boolean channel's
    // BOOLEAN positions and the substrate's positions are the same positions — §8.4.3.3.4 GR5a. It carried the
    // shared 0899 recognized-not-implemented code, so no number is freed and none is reallocated. Verified
    // before removal: no `.err` fixture in the corpus expected it, so no green test was pinning the gap open.)
    public static readonly DiagnosticDescriptor RecursiveWsPointerBacked = new(
        NotImplemented, "recursive-working-storage-pointer-backed", EditionSeverity.Error,
        "An ADDRESS-OF-taken record in the WORKING-STORAGE of a RECURSIVE program or function is recognized "
        + "but its static addressable-cell storage is not yet implemented (the cell is per-instance today, "
        + "which would re-initialize per activation). The BASED half landed with kb/Work PB154: a BASED "
        + "root's data lives in its allocated cell and its data-address pointer is a static bridge field "
        + "that CANCEL resets to NULL (§14.6.2.3.2 action 5).",
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
    // 1631 — a FORMATTED-*/INTEGER-OF-FORMATTED-DATE/… format argument that is not one of the §15.3.1–§15.3.3
    // formats, or is the wrong KIND for the function (fix-queue PB11). Before this, the format was validated
    // character-wise only, so any string assembled from legal subfields was accepted and the function
    // FABRICATED a value — `FORMATTED-DATE("hhmmss" …)` returned "000000". Edition-invariant in substance; the
    // functions themselves are 2014+ (§15.39–§15.41), so it is unreachable below that.
    public static readonly DiagnosticDescriptor DateTimeFormatKindMismatch = new(
        "COBOLNET1631", "date-time-format-kind", EditionSeverity.Error,
        "A date/time FORMAT argument is not a format the standard defines, or is the wrong KIND for the "
        + "function. ISO §15.3.1.1 fixes SIX date formats (basic and extended, for calendar, ordinal and week "
        + "dates), §15.3.2 twelve time formats (four common-time shapes × local / UTC / offset), and §15.3.3.7 "
        + "makes a combined format a date format, an uppercase T, and a time format. §15.39.3 r2 requires a "
        + "DATE format, §15.41.3 r2 a TIME format and §15.40.3 r2 a COMBINED one. ⛔ BASIC AND EXTENDED NEVER "
        + "MIX: `YYYY-MMDD` and `YYYY-MM-DDThhmmss` are built entirely from legal subfields and are still not "
        + "formats, which is why membership is tested rather than each field in isolation.",
        "ISO §15.3.1.1 / §15.3.2 / §15.3.3.7 / §15.39.3 r2 / §15.40.3 r2 / §15.41.3 r2");
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
    // (The former SubstituteAllSubscript stage — SUBSTITUTE with a table(ALL) argument — landed as the run-time
    // pairing CobolIntrinsics.SubstituteFlat, kb/Work PB81, 2026-08-18.)
    // §15.3 — the ALL subscript in an intrinsic argument (kb/Work PB62): admissible only "when the definition of a
    // function permits an argument to be repeated a variable number of times"; the former bind-time expansion ran
    // for every function and let `FUNCTION MOD(E(ALL) B)` bind over a one-occurrence table.
    // §14.9.28.3 SR2 — the PERFORM … TIMES count "shall be an integer" (kb/Work PB86): a non-integer data item was
    // accepted and its UNSCALED digits iterated (PIC 9V9 VALUE 1.2 → 12 times); a non-integer function was a
    // parse error in one spelling and ran once in the other.
    // §8.4.3.3.3 SR1 — the identifier-1 a reference modification may name (kb/Work PB70): every excluded shape
    // (a strongly-typed / variable-length group, a numeric item of a non-DISPLAY usage, an edited or numeric item
    // subordinate to a strongly-typed group, an index / pointer / object reference) used to fall to a run-time
    // NotImplemented — and a receiving one to a silent drop.
    // §8.3.3.6.3 SR2 — the ALL figurative's literal-1 "shall be neither a figurative constant nor a zero-length
    // literal" (kb/Work PB71): `MOVE ALL "" TO X` compiled and stored spaces.
    public static readonly DiagnosticDescriptor AllLiteralZeroLength = new(
        "COBOLNET1648", "all-literal-zero-length", EditionSeverity.Error,
        "The literal-1 of the figurative constant ALL literal-1 is a zero-length literal. ISO §8.3.3.6.3 SR2: "
        + "\"Literal-1 shall be an alphanumeric, boolean, or national literal … The literal shall be neither a "
        + "figurative constant nor a zero-length literal.\" Write at least one character.", "ISO §8.3.3.6.3 SR2");
    // §8.3.2.1 rule 5 — an intrinsic-function-name "identified in a function-specifier in the REPOSITORY paragraph"
    // shall not be used as a user-defined word (kb/Work PB65 FMT-15.43.2 / FMT-15.58.2): under `REPOSITORY. FUNCTION
    // HIGHEST-ALGEBRAIC INTRINSIC.` a table named HIGHEST-ALGEBRAIC compiled clean and `HIGHEST-ALGEBRAIC(A1)`
    // silently read the table element where §15.43.4 requires +999.
    public static readonly DiagnosticDescriptor RepositoryIntrinsicNameAsUserWord = new(
        "COBOLNET1649", "repository-intrinsic-name-as-user-word", EditionSeverity.Error,
        "A user-defined word (a data-name, condition-name, index-name, file-name, paragraph or section name) spells "
        + "an intrinsic-function-name that the REPOSITORY paragraph identifies in a function-specifier — FUNCTION "
        + "name INTRINSIC, or FUNCTION ALL INTRINSIC. ISO §8.3.2.1 rule 5: intrinsic-function-names may be used as "
        + "user-defined words except for LENGTH, RANDOM, SIGN, SUM and \"intrinsic function names identified in a "
        + "function-specifier in the REPOSITORY paragraph\" — that identification is what lets a reference omit the "
        + "word FUNCTION (§8.4.3.2.3 SR2), so the same word cannot also name a data item. Rename the item, or take "
        + "the function out of the REPOSITORY and write FUNCTION name(…) at each reference.", "ISO §8.3.2.1 rule 5");
    // §7.3.17 — the LEAP-SECOND directive's syntax (kb/Work PB65): SR1 "shall not be specified within a
    // compilation unit"; the operand is ON (optional word) or OFF.
    public static readonly DiagnosticDescriptor LeapSecondDirectiveSyntax = new(
        "COBOLNET1650", "leap-second-directive-syntax", EditionSeverity.Error,
        "The >>LEAP-SECOND directive is malformed or misplaced: its operand is ON (an optional word — a bare "
        + ">>LEAP-SECOND selects ON) or OFF (ISO §7.3.17.2), and the directive shall not be specified within a "
        + "compilation unit (§7.3.17.3 SR1) — it precedes the first IDENTIFICATION DIVISION of the compilation group "
        + "and governs the whole group.", "ISO §7.3.17");
    // §14.9.43.3 / §14.9.48.3 — the STRING and UNSTRING operand rules that are not about USAGE (those are
    // COBOLNET1626): a reference-modified, edited, JUSTIFIED or strongly-typed STRING receiver (SR4/SR5/SR6), a
    // POINTER / COUNT IN / TALLYING item that is not an integer without P (STRING SR7, UNSTRING SR5/SR6), an
    // UNSTRING sender that is not category alphanumeric or national (SR2), DELIMITER IN / COUNT IN without
    // DELIMITED BY (UNSTRING SR7), a variable-length group operand (STRING SR11 / UNSTRING SR10). kb/Work PB88:
    // each was a `BoundUnsupported` — the program compiled clean and died at the statement.
    public static readonly DiagnosticDescriptor StringUnstringOperandRule = new(
        "COBOLNET1651", "string-unstring-operand-rule", EditionSeverity.Error,
        "A STRING or UNSTRING operand violates one of the statement's syntax rules: STRING's INTO receiver shall not "
        + "be reference-modified (§14.9.43.3 SR4), edited or JUSTIFIED (SR5), a strongly-typed group (SR6) or a "
        + "variable-length group (SR11), and its POINTER shall be an elementary integer without P (SR7); UNSTRING's "
        + "sender shall be category alphanumeric or national (§14.9.48.3 SR2), DELIMITER IN / COUNT IN need a "
        + "DELIMITED BY phrase (SR7), COUNT IN / TALLYING IN / POINTER items are integers without P (SR5/SR6), and no "
        + "operand may be a variable-length group (SR10). Rejected at bind — the statement is not run.",
        "ISO §14.9.43.3 / §14.9.48.3");
    // kb/Work PB78: the OBJECT-COMPUTER paragraph's clauses (§12.3.6.2's bracketed choice indicators — §5.2.6.4:
    // zero or more, EACH AT MOST ONCE, any order) are a list since computer-name-1 became optional in the grammar.
    public static readonly DiagnosticDescriptor ObjectComputerDuplicateClause = new(
        "COBOLNET1652", "object-computer-duplicate-clause", EditionSeverity.Error,
        "An OBJECT-COMPUTER clause (PROGRAM COLLATING SEQUENCE, CHARACTER CLASSIFICATION) is specified more than once "
        + "in the paragraph. ISO §12.3.6.2's format encloses the clauses in choice indicators within brackets, which "
        + "§5.2.6.4 defines as zero or more of the alternatives, each at most once, in any order. Keep one.",
        "ISO §12.3.6.2 / §5.2.6.4");
    // kb/Work PB79 (data-model design D20): the GROUP-USAGE clause's syntax rules — SR1 (a group item that is not
    // strongly typed and not a variable-length group), SR2/SR3 (no explicit USAGE on the subject; every subordinate
    // group the same GROUP-USAGE). The subordinate LEAF conformance rides the shared §13.18.60.4 GR1 usage-inheritance
    // check (COBOLNET0881 / the national-form staged 0899 legs), never a second copy here.
    public static readonly DiagnosticDescriptor GroupUsageRule = new(
        "COBOLNET1653", "group-usage-rule", EditionSeverity.Error,
        "A GROUP-USAGE clause violates one of its syntax rules: it is specified on an entry that is not a group item, "
        + "on a strongly-typed group, or on a variable-length group (SR1); a USAGE clause is explicitly specified on "
        + "the same entry (SR2/SR3 — the usage is implied); or a subordinate group declares the other GROUP-USAGE "
        + "(SR2/SR3 — every subordinate group is the same, explicitly or implicitly).",
        "ISO §13.18.29.3");
    // kb/Work PB93: a REDEFINES data-name-2 / RENAMES data-name-2/-3 that names no preceding entry was accepted
    // SILENTLY (the program-scope REDEFINES miss left RedefinesTargetName set against a null RedefinesTarget, so the
    // layout consumers disagreed; the RENAMES miss just `continue`d). §8.4.2.1: a statement shall contain a reference
    // that uniquely identifies the resource — a name that identifies nothing is an error at every edition.
    public static readonly DiagnosticDescriptor RedefinesTargetUnresolved = new(
        "COBOLNET1654", "redefines-target-unresolved", EditionSeverity.Error,
        "The REDEFINES clause's data-name-2 does not name a preceding data description entry in the same scope. "
        + "ISO §13.18.44.3 SR4/SR7/SR10 place data-name-2 among the entries preceding the subject at the same level; "
        + "§8.4.2.1 requires every reference to identify a resource. Name the entry that defines the area.",
        "ISO §13.18.44.3 / §8.4.2.1");
    public static readonly DiagnosticDescriptor RenamesOperandUnresolved = new(
        "COBOLNET1655", "renames-operand-unresolved", EditionSeverity.Error,
        "A RENAMES clause's data-name-2 or data-name-3 does not name an elementary item or group of elementary items in "
        + "the same record (ISO §13.18.45.3 SR4; §8.4.2.1 requires every reference to identify a resource). Name an "
        + "item of the record the level-66 entry follows.",
        "ISO §13.18.45.3 / §8.4.2.1");
    // kb/Work PB93 (sweep): SR7 — "Multiple redefinitions of the same storage area shall each specify as data-name-2
    // the data-name of the entry that originally defined the area" — a REDEFINES naming a REDEFINER is illegal ISO
    // source that GnuCOBOL/IBM accept as a chain (the anchor is chased); error strict, warning + the chain semantics
    // under --permissive (the documented-dialect-leniency seam, EditionContext.Removed).
    public static readonly DiagnosticDescriptor RedefinesOfRedefinition = new(
        "COBOLNET1656", "redefines-of-redefinition", EditionSeverity.Error,
        "A REDEFINES clause names an entry that is itself a redefinition. ISO §13.18.44.3 SR7 requires each of multiple "
        + "redefinitions of one storage area to name the entry that ORIGINALLY defined the area. Name the original "
        + "entry; under --permissive the chain is accepted with this warning (the anchor is the original entry).",
        "ISO §13.18.44.3 SR7");
    // kb/Work PB94: §13.18.63.3 SR2 ("If the category of the subject of the entry is numeric, all literals in the VALUE
    // clause shall be numeric") and SR4 ("If the item is of category alphabetic, alphanumeric, or alphanumeric-edited
    // literals in the VALUE clause shall be alphanumeric literals") were unenforced — `PIC 9 VALUE "abc"` reached the
    // C# backend (CS0103), `PIC 99 VALUE "7"` and `PIC X(2) VALUE 12` compiled silently. Error strict at every
    // edition; under --permissive the REPRESENTABLE vendor leniency (a digits-only alphanumeric literal on a numeric
    // item is that number; a numeric literal on an alphanumeric item is its digits, left-justified; a character
    // figurative on a numeric item is ZERO — a native numeric holds no character fill) is a warning. The national /
    // boolean halves of the same family (SR5 / SR10) are the pre-catalog COBOLNET0898 band.
    public static readonly DiagnosticDescriptor ValueLiteralClass = new(
        "COBOLNET1657", "value-literal-class", EditionSeverity.Error,
        "A VALUE clause literal's class does not match the subject's category: a numeric item takes numeric literals "
        + "(or figurative ZERO) only (ISO §13.18.63.3 SR2); an alphabetic, alphanumeric or alphanumeric-edited item "
        + "takes alphanumeric literals only (SR4). Under --permissive a representable value is stored with this "
        + "warning; a value no numeric item can hold is an error on both axes.",
        "ISO §13.18.63.3 SR2 / SR4");
    // kb/Work PB66 (data-model design D21): the floating-point numeric-edited PICTURE (the symbol E) — its syntax rules.
    public static readonly DiagnosticDescriptor PictureFloatEdited = new(
        "COBOLNET1658", "picture-float-edited", EditionSeverity.Error,
        "A floating-point numeric-edited PICTURE (a significand and an exponent separated by the symbol E) violates its "
        + "form: E and the decimal point may appear only once (ISO §13.18.40.3 SR12 b); the exponent shall be +9, +99, "
        + "+999 or +9999 (§13.18.40.4 GR13 b); the significand admits only 9, B, 0, /, comma, the point and one leading "
        + "+ or − (§13.18.40.6 Table 10 row E — no floating insertion, no zero suppression, no S/V/P/CR/DB/currency, "
        + "no EDITING character); the significand carries 1 to 36 digit positions (§13.18.40.3 SR15).",
        "ISO §13.18.40.3 / §13.18.40.4 GR13 b / §13.18.40.6");
    // kb/Work PB66 / PB97: §13.18.63.3 SR6 — a numeric-edited item's numeric VALUE literal shall be of the item's FORM:
    // a fixed-point numeric-edited item takes a fixed-point literal, a floating-point numeric-edited item a
    // floating-point literal; ZERO and the zero forms are legal for either. Reaches the item VALUE and the level-88 set.
    public static readonly DiagnosticDescriptor ValueEditedLiteralForm = new(
        "COBOLNET1659", "value-edited-literal-form", EditionSeverity.Error,
        "The form of a numeric VALUE literal for a numeric-edited item shall match the item's form (ISO §13.18.63.3 SR6): "
        + "a fixed-point numeric-edited item takes a fixed-point literal, a floating-point numeric-edited item a "
        + "floating-point literal (a mantissa and an exponent, e.g. 1.5E+3); the figurative constant ZERO and the "
        + "integer / decimal literal zero are legal for either.",
        "ISO §13.18.63.3 SR6");
    // kb/Work PB66: §15.43.4 r1 / §15.58.4 r1 — the well-formedness of a floating-point numeric-edited argument-1 of
    // HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC: its extreme shall pass an IN-ARITHMETIC-RANGE test, i.e. lie within the
    // intermediate data item's range for the arithmetic mode in effect (§8.8.4.4.4 GR3 l).
    public static readonly DiagnosticDescriptor AlgebraicFloatEditedRange = new(
        "COBOLNET1660", "algebraic-float-edited-range", EditionSeverity.Error,
        "FUNCTION HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC: the floating-point numeric-edited argument-1's data description "
        + "entry shall be such that its value farthest from zero (all-nines significand at the maximum exponent) would "
        + "pass an IN-ARITHMETIC-RANGE test under the arithmetic mode in effect — the intermediate data item cannot hold "
        + "it (ISO §15.43.4 r1 / §15.58.4 r1; §8.8.4.4.4 GR3 l). Narrow the exponent, or change ARITHMETIC.",
        "ISO §15.43.4 r1 / §15.58.4 r1");
    // kb/Work PB99: §8.3.3.3.3 SR2/SR3/SR4 — the floating-point literal's form — and r3's implementor-defined exponent
    // range (binary64 for a procedure-division literal, D16; the receiver's binary form for a VALUE on a FLOAT item).
    public static readonly DiagnosticDescriptor FloatingLiteral = new(
        "COBOLNET1661", "floating-point-literal", EditionSeverity.Error,
        "A floating-point numeric literal shall have a significand of 1 to 36 digits with a decimal point, an exponent of at "
        + "most four digits, and — when the significand is zero — a zero exponent and no negative sign (ISO §8.3.3.3.3 "
        + "SR2/SR3/SR4); its value shall lie within the implementor-defined exponent range (r3): the IEEE binary64 range "
        + "(about 4.9E-324 to 1.8E+308) for a literal that evaluates in an arithmetic expression or seeds a FLOAT-LONG / "
        + "FLOAT-BINARY-64 item, the binary32 range for FLOAT-SHORT / FLOAT-BINARY-32; a floating-point numeric-edited "
        + "VALUE keeps the exact value.",
        "ISO §8.3.3.3.3 SR2/SR3/SR4/r3");
    // kb/Work PB101 (DESIGN-locale-facility §4.9, increment T7): the SPECIAL-NAMES ORDER TABLE clause and
    // FUNCTION STANDARD-COMPARE. §12.3.7.4 GR17 leaves the allowable content of literal-9 to the implementor, so a
    // literal this implementation's collation engine cannot resolve is NOT a syntax error — the program is legal
    // and §15.85.4 r2 defines its runtime outcome (EC-ORDER-NOT-SUPPORTED at every reference). A WARNING is
    // therefore the only conforming severity, and saying nothing would leave a program whose every
    // STANDARD-COMPARE is inoperative looking clean at compile time.
    public static readonly DiagnosticDescriptor OrderTableUnresolved = new(
        "COBOLNET1662", "order-table-unresolved", EditionSeverity.Warning,
        "The SPECIAL-NAMES ORDER TABLE clause's literal-9 does not name a cultural ordering table this "
        + "implementation provides. ISO §12.3.7.4 GR17 leaves the allowable content of literal-9 to the "
        + "implementor; COBOL.NET accepts the default table 'ISO 14651_2020_TABLE1' (case-insensitive, the space "
        + "and the underscore interchangeable) and, as an implementor extension, a CLDR locale tag naming a "
        + "tailored collation. Every FUNCTION STANDARD-COMPARE reference to this ordering-name sets "
        + "EC-ORDER-NOT-SUPPORTED at run time (§15.85.4 r2). The clause itself stays legal.",
        "ISO §12.3.7.4 GR17 / §15.85.4 r2");
    public static readonly DiagnosticDescriptor StandardCompareArgument = new(
        "COBOLNET1663", "standard-compare-argument", EditionSeverity.Error,
        "A FUNCTION STANDARD-COMPARE argument violates ISO §15.85.3: ordering-name-1 shall be a name associated "
        + "with a cultural ordering table in the ORDER TABLE clause of the SPECIAL-NAMES paragraph (r5; §15.3 "
        + "argument type 12, and §12.3.7.3 SR9 makes this function the only place such a name may be specified), "
        + "and argument-4 shall be a positive nonzero integer (r6). The §15.85.2 general format admits at most one "
        + "of each, so a second ordering-name or a second level violates the format.",
        "ISO §15.85.3 r5 / r6 / §15.85.2");
    // kb/Work PB64 T1 (DESIGN-locale-facility §7 rules a–e): the syntax rules of the locale facility that become
    // reachable once the SPECIAL-NAMES LOCALE clause and SET formats 11/12 are ACCEPTED rather than refused by name.
    public static readonly DiagnosticDescriptor LocaleNameUndeclared = new(
        "COBOLNET1664", "locale-name-undeclared", EditionSeverity.Error,
        "A locale-name is referenced that no SPECIAL-NAMES LOCALE clause in scope declares — the ONE diagnostic for every "
        + "locale-name reference site, which names the rule it cites: the ALPHABET clause's IS LOCALE locale-name-2 "
        + "(ISO §12.3.7.3 SR24), SET LOCALE's locale-name-1 (§14.9.39.3 SR26), and the later increments' LOCALE phrases "
        + "(PICTURE §13.18.40.3 SR37; LOCALE-COMPARE / -DATE / -TIME / UPPER-CASE / LOWER-CASE; CHARACTER CLASSIFICATION "
        + "§12.3.6.3 SR3). Declare it: LOCALE locale-name IS external-locale-name | literal.",
        "ISO §12.3.7.3 SR24 / §14.9.39.3 SR26 / §12.3.7.4 GR1");
    public static readonly DiagnosticDescriptor LocaleNameDuplicate = new(
        "COBOLNET1665", "locale-name-duplicate", EditionSeverity.Error,
        "The same locale-name is declared by more than one SPECIAL-NAMES LOCALE clause of one paragraph. A user-defined "
        + "word of one type is unique within its scope (ISO §8.3.2.2); the LOCALE clause is repeatable (§12.3.7.2) so "
        + "several LOCALES may be declared, each under its own name.", "ISO §8.3.2.2 / §12.3.7.2");
    public static readonly DiagnosticDescriptor SetLocaleCategories = new(
        "COBOLNET1666", "set-locale-categories", EditionSeverity.Error,
        "The first operand of SET LOCALE (ISO §14.9.39.2 format 11) is malformed: a category is specified more than "
        + "once (the category brace carries choice indicators — §5.2.6.4: one or more of LC_ALL, LC_COLLATE, LC_CTYPE, "
        + "LC_MESSAGES, LC_MONETARY, LC_NUMERIC, LC_TIME, each at most once, in any order), a word that is not a locale "
        + "category appears in the list, or USER-DEFAULT is combined with a category (the outer brace is a plain "
        + "alternation — categories OR USER-DEFAULT).", "ISO §14.9.39.2 format 11 / §5.2.6.4");
    public static readonly DiagnosticDescriptor SetLocaleUserDefaultSource = new(
        "COBOLNET1667", "set-locale-user-default-source", EditionSeverity.Error,
        "SET LOCALE USER-DEFAULT TO USER-DEFAULT / SYSTEM-DEFAULT: if USER-DEFAULT is specified as the first operand, "
        + "identifier-10 or locale-name-1 shall be specified in the TO phrase (ISO §14.9.39.3 SR25) — the user default "
        + "is set FROM a named or saved locale (§14.9.39.4 GR22), never from itself or the system default.",
        "ISO §14.9.39.3 SR25 / §14.9.39.4 GR22");
    public static readonly DiagnosticDescriptor SetLocalePointerCategory = new(
        "COBOLNET1668", "set-locale-pointer-category", EditionSeverity.Error,
        "The identifier of SET LOCALE … TO identifier-10 (ISO §14.9.39.3 SR27) or of SET identifier-11 TO LOCALE (SR28) "
        + "shall reference an elementary data item of category data-pointer (USAGE POINTER) — the saved-locale handle "
        + "of §14.9.39.4 GR26/GR27 is a pointer value, stored and read only through such an item.",
        "ISO §14.9.39.3 SR27 / SR28");
    public static readonly DiagnosticDescriptor LocaleAlphabetNotACharacterSet = new(
        "COBOLNET1669", "locale-alphabet-not-a-charset", EditionSeverity.Error,
        "An alphabet defined with the LOCALE phrase (ALPHABET … [FOR NATIONAL] IS LOCALE [locale-name]) is referenced "
        + "where a CODED CHARACTER SET is required — a class condition's alphabet-name-1 (ISO §8.8.4.4.3 SR2: 'shall not "
        + "reference an alphabet associated with a locale'; the same rule governs the IN phrases of SYMBOLIC CHARACTERS "
        + "and CLASS, §12.3.7.3 SR16g / SR17d, and the CODE-SET clause, §13.18.13.3 SR1 / SR2). A LOCALE alphabet defines "
        + "a collating sequence only (§12.3.7.4 GR7, Table 6) — it names no set of characters. Name a coded-character-set "
        + "alphabet (NATIVE, STANDARD-1/2, UCS-4, UTF-8, UTF-16) instead.",
        "ISO §8.8.4.4.3 SR2 / §12.3.7.3 SR16g, SR17d");
    public static readonly DiagnosticDescriptor SymbolicCharactersViolation = new(
        "COBOLNET1670", "symbolic-characters-clause", EditionSeverity.Error,
        "A SYMBOLIC CHARACTERS clause violates one of ISO §12.3.7.3 SR16's sub-rules (the message names which): a) a "
        + "given symbolic-character-1 may be specified only once within the paragraph's SYMBOLIC CHARACTERS clauses; "
        + "b/c) the names pair with the integers by position, one-to-one; e/f) the ordinal position shall exist in the "
        + "native character set of the clause's class or, under IN, in the character set referenced by alphabet-name-3, "
        + "which shall define a set of that class. (A LOCALE alphabet under IN is COBOLNET1669 — SR16 g.)",
        "ISO §12.3.7.3 SR16");
    public static readonly DiagnosticDescriptor ClassClauseViolation = new(
        "COBOLNET1671", "class-clause", EditionSeverity.Error,
        "A SPECIAL-NAMES CLASS clause names an ordinal position that does not exist: a numeric literal-5/-6 shall be "
        + "within the range one through the number of characters in the native character set or, when the IN phrase is "
        + "specified, in the character set referenced by alphabet-name-4 (ISO §12.3.7.3 SR17 b2; the ordinal resolves in "
        + "THAT set — §12.3.7.4 GR12 a). (A LOCALE alphabet under IN is COBOLNET1669 — SR17 d.)",
        "ISO §12.3.7.3 SR17");
    public static readonly DiagnosticDescriptor CodeSetClauseViolation = new(
        "COBOLNET1672", "code-set-clause", EditionSeverity.Error,
        "An FD CODE-SET clause violates one of ISO §13.18.13.3's syntax rules (the message names which): SR1/SR2 — "
        + "alphabet-name-1 / alphabet-name-2 shall reference an alphabet defining an alphanumeric / national coded "
        + "character set (a LOCALE alphabet is COBOLNET1669; a class mismatch or an undeclared name is named here); "
        + "each class at most once (§5.2.6.4) — or names a coded character set whose on-medium representation differs "
        + "from the native encoding, which this processor does not provide (Annex A §A.3 item 27 — the CODE-SET clause "
        + "is dependent upon a device capable of supporting the specified code; documented non-support, CONFORMANCE.md "
        + "§2 row 27: NATIVE and the identity-correspondence sets STANDARD-1 / STANDARD-2 / UTF-16 are supported).",
        "ISO §13.18.13.3; Annex A §A.3 item 27");
    public static readonly DiagnosticDescriptor PictureLocaleFormat2Violation = new(
        "COBOLNET1673", "picture-locale-format2", EditionSeverity.Error,
        "A format 2 (LOCALE) PICTURE clause violates one of ISO §13.18.40.3's syntax rules — the message names "
        + "which: SR32 — not in (or subordinate to) a CONSTANT RECORD item; SR33 — character-string-1 shall "
        + "contain at least one 'Z' or '9'; SR34 — each of '+', '.', the currency symbol at most once; SR35 — 1 "
        + "through 31 digit positions; SR36 — the currency symbol and '+' only left of the decimal point "
        + "position; the §13.18.40.6 Table 11 precedence (the symbols are ONLY '+', the currency symbol, 'Z', "
        + "'9', '.' — '+' first, the currency symbol before every digit, no '9' before any 'Z', so the legal "
        + "shape is [+] [cs] Z… 9… [. Z…|9…]); or an EDITING phrase beside the LOCALE phrase (format 2 has no "
        + "EDITING phrase — it is format 1's).",
        "ISO §13.18.40.3 SR32-SR37 / §13.18.40.6 Table 11");
    public static readonly DiagnosticDescriptor SignClauseWithLocalePicture = new(
        "COBOLNET1674", "sign-clause-with-locale-picture", EditionSeverity.Error,
        "A SIGN clause is specified for a data item whose PICTURE clause carries the LOCALE phrase (format 2). "
        + "ISO §13.16.3 SR19 (data description) / §13.17.3 SR9 (screen description): \"If the LOCALE phrase of "
        + "the PICTURE clause is specified, the SIGN clause shall not be specified\" — a locale-edited item's "
        + "sign representation comes from the locale (§13.18.40.5 rule 13), never from a SIGN clause. (A report "
        + "group description entry carries NO such rule — §13.15.3 — and the pair is legal there.)",
        "ISO §13.16.3 SR19 / §13.17.3 SR9");
    public static readonly DiagnosticDescriptor RefModIdentifierNotPermitted = new(
        "COBOLNET1647", "ref-mod-identifier-not-permitted", EditionSeverity.Error,
        "Reference modification is applied to an item ISO §8.4.3.3.3 SR1 does not admit as identifier-1: a boolean, "
        + "national, alphanumeric or alphabetic item, an alphanumeric group item, an edited item or a numeric item of "
        + "usage DISPLAY or NATIONAL (each not subordinate to a strongly-typed group), or a group that is neither "
        + "strongly-typed nor variable-length (§8.5.1.12). Reference-modify a permitted item, or REDEFINES the storage "
        + "with a character item.", "ISO §8.4.3.3.3 SR1");
    // The receiving-side residue (kb/Work PB70): a data reference that RESOLVED to a declared item but whose shape
    // this compiler does not implement as a receiving operand. It used to be dropped from the receiver list by
    // .OfType<Place>() — `MOVE "Z" TO OK1 TB(2:1) OK2` moved into OK1 and OK2 and silently skipped TB.
    public static readonly DiagnosticDescriptor ReceivingReferenceNotImplemented = new(
        NotImplemented, "receiving-reference-shape-not-implemented", EditionSeverity.Error,
        "A receiving operand names a declared item in a reference shape COBOL.NET does not yet implement as a receiver "
        + "(COBOLNET_DESIGN §1.4: an unsupported shape fails loud, never silently). The statement is rejected rather "
        + "than run with the receiver dropped.", "COBOLNET_DESIGN §1.4",
        RecognizedNotImplemented);
    // kb/Work PB128: arithmetic RESULTANTS never had a compile-time category screen — COMPUTE X-item = 1
    // compiled and died in StoreArith's run-time loud, where §4.2.2 requires a compile-time mechanism.
    public static readonly DiagnosticDescriptor ArithmeticResultantCategory = new(
        "COBOLNET1675", "arithmetic-resultant-category", EditionSeverity.Error,
        "An arithmetic statement's resultant identifier is not of a category its syntax rule admits. The in-place "
        + "receivers (ADD TO / SUBTRACT FROM / MULTIPLY BY / DIVIDE INTO) shall reference elementary NUMERIC data "
        + "items; the GIVING resultants, DIVIDE's REMAINDER and COMPUTE's identifier-1 admit elementary numeric or "
        + "NUMERIC-EDITED items. A group, an alphanumeric/alphabetic/boolean item, an index data item "
        + "(§13.18.60.3 SR10's closed reference list), or a reference-modified slice (§8.4.3.3.4 GR6c makes it "
        + "category alphanumeric) is not a legal resultant.",
        "ISO §14.9.2.3 SR2/SR4 · §14.9.8.3 SR1 · §14.9.12.3 SR1/SR2 · §14.9.26.3 SR1/SR2 · §14.9.44.3");
    public static readonly DiagnosticDescriptor PerformTimesCountNotInteger = new(
        "COBOLNET1646", "perform-times-count-not-integer", EditionSeverity.Error,
        "The PERFORM … TIMES count is not an integer. ISO §14.9.28.3 SR2: \"Each identifier shall reference a numeric "
        + "elementary item described in the data division. Identifier-1 shall be an integer.\" — an integer data item "
        + "(category numeric, no fraction digits, not USAGE INDEX), an integer literal, or a function-identifier whose "
        + "type is integer (§15.2 type 5). Write an integer count.",
        "ISO §14.9.28.3 SR2");
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
    // ── THE DECLINED-OPTIONAL-ELEMENT BAND (§4.2.6 ¶3 / §4.2.7 / §4.2.13 / Annex A.3–A.4) ─────────────────
    //    ONE mechanism, TWO dispositions, and the descriptor's own Severity is what chooses between them —
    //    every site routes through `EditionContext.Declined(descriptor, seen)`, never a local strictness test
    //    and never a second helper (feedback_one_mechanism_per_job).
    //
    //    • ACCEPT-INERT (Warning) — COBOLNET1578 / 1579 / 1580. These facilities are ADDITIVE: the
    //      program still means what it says with them absent (no message I-O, COMMIT behaves as
    //      CONTINUE, VALIDATE validates nothing), so §4.2.6 ¶3's mandatory compile-time warning mechanism
    //      ("shall provide a warning mechanism at compile time to indicate use of syntactically-detectable
    //      processor-dependent language elements not supported") is the whole obligation, and §14.6.13.1.1
    //      licenses raising NO exception conditions. The program COMPILES, RUNS, and the facility is inert.
    //      Before this band these constructs produced a GENERIC parse error, which satisfied neither the
    //      warning obligation nor the "never a silent wrong answer" rule.
    //
    //    • REFUSE (Error) — COBOLNET1560 / 1705 / 1706 / 1707. These facilities are NOT additive: compiled
    //      inert they change the ANSWER (which bytes reach the medium; which record description entry is
    //      selected; a whole-record-area write standing in for a §14.9.51.4 GR8 implicit record; a screen
    //      ACCEPT re-read as the device format, which transfers the wrong data). A.4.1's first sentence
    //      is the licence to refuse: "An implementation shall accept the syntax and provide the functionality
    //      for an optional element only when support for that language element is claimed by the implementor."
    //      Unclaimed ⇒ the syntax is not accepted. The strictness axis does NOT move these: --permissive is
    //      the REMOVED-construct / documented-leniency migration seam (EditionContext.Removed) and there is no
    //      "pre-removal semantics" to preserve here — matching the accept-inert rows above, which --permissive
    //      likewise does not move.
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
    // ── 1560 / 1705 / 1706 / 1707 — the REFUSE half of the band (see the header above). One code per A.4
    //    MODULE (A.4.2 takes two, split at the division boundary — see the header below it), not per
    //    clause: the module is the unit §4.2.7 makes the implementor document and Annex A.4 makes optional, so
    //    a user who reads CONFORMANCE.md §5 finds exactly one row per code. The `seen` half of the message is
    //    composed at the site and names WHICH element of the module was written.
    public static readonly DiagnosticDescriptor FormatSelectWhenUnclaimed = new(
        "COBOLNET1705", "a48-format-select-when-unclaimed", EditionSeverity.Error,
        "the FORMAT clause (ISO §13.18.24) and the SELECT WHEN clause (ISO §13.18.51) are the two items of "
        + "Annex A.4.8, an OPTIONAL language element (§4.2.7) for which this implementation claims NO support "
        + "(docs/CONFORMANCE.md §5, row A.4.8) — Annex A.4.1: an implementation shall accept the syntax for an "
        + "optional element ONLY when support for it is claimed, so the clause is refused rather than accepted "
        + "inert. Refused at EVERY edition: an inert FORMAT changes which bytes reach the medium "
        + "(§13.18.24.4 GR1) and an inert SELECT WHEN selects the wrong record description entry "
        + "(§13.18.51.4 GR1) with an I-O status 45 path (§9.1.13.7 rule 5) — a wrong answer, not a missing "
        + "facility, which is why this is an Error and not the COBOLNET1578/1579/1580 accept-inert band. "
        + "At --std 85 the FORMAT clause additionally cannot be written at all: §8.9 reserves FORMAT only from "
        + "2002, so there the word is a user-defined name.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.8 items 1-2 / §13.18.24 / §13.18.51");
    public static readonly DiagnosticDescriptor WriteRewriteFileUnclaimed = new(
        "COBOLNET1706", "a413-write-rewrite-file-unclaimed", EditionSeverity.Error,
        "the FILE phrase of the WRITE statement (ISO §14.9.51) and of the REWRITE statement (ISO §14.9.35) — "
        + "`WRITE FILE file-name-1 FROM …` / `REWRITE FILE file-name-1 RECORD FROM …` — are the two items of "
        + "Annex A.4.13, an OPTIONAL language element (§4.2.7) for which this implementation claims NO support "
        + "(docs/CONFORMANCE.md §5, row A.4.13) — Annex A.4.1: an implementation shall accept the syntax for an "
        + "optional element ONLY when support for it is claimed. The two STATEMENTS are mandatory and fully "
        + "supported; only this one alternative of `{ record-name-1 | FILE file-name-1 }` is declined "
        + "(A.4.1 NOTE 1: the higher-level cross-referenced construct is not optional), so plain "
        + "`WRITE record-name-1` / `REWRITE record-name-1` are unaffected. Refused at EVERY edition and in "
        + "BOTH printed formats (§14.9.51.2 Format 1 sequential and Format 2 random): the declined phrase has "
        + "its own implicit-record semantics (§14.9.51.4 GR8, §14.9.35.4 GR9) that a whole-record-area write "
        + "does not implement, so accepting it inert would be a wrong answer.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.13 items 1-2 / §14.9.51 / §14.9.35");
    public static readonly DiagnosticDescriptor StrongGroupOrderingSignedLeaf = new(
        NotImplemented, "strong-group-ordering-signed-leaf", EditionSeverity.Error,
        "An ORDERING relation (<, >, <=, >=) between strongly-typed groups containing a SIGNED numeric "
        + "elementary item is legal (§8.8.4.2.3 SR4 restricts only boolean/message-tag/object/pointer contents) "
        + "but not yet implemented: §8.8.4.2.12 orders strongly-typed groups ELEMENT BY ELEMENT — a signed "
        + "numeric pair compares ALGEBRAICALLY (§8.8.4.2.4), which the whole-group character-image comparison "
        + "cannot honor (the overpunch/separate sign breaks lexical=algebraic). Equality and every "
        + "unsigned/alphanumeric-leaf ordering ARE carried by the image comparison (provably element-equivalent "
        + "for a fixed same-type profile).", "ISO §8.8.4.2.12 / §8.8.4.2.4", RecognizedNotImplemented);

    // ── Annex A.4.2 — ACCEPT and DISPLAY SCREEN HANDLING, the largest DECLINED optional module (kb/Work PB260).
    //
    //    ⛔ WHY AN ERROR AND NOT THE §4.2.6 RECOGNIZE-AND-WARN OF 1578/1579/1580. Those three name Annex A.3
    //    PROCESSOR-DEPENDENT elements, whose licence is §4.2.6 — "the decision to provide support … is within an
    //    implementor's discretion" plus a mandatory compile-time WARNING mechanism, i.e. accept-and-flag. Screen
    //    handling is an Annex A.4 OPTIONAL module, and A.4.1's licence reads the other way: "An implementation
    //    shall accept the syntax and provide the functionality for an optional element ONLY WHEN support for that
    //    language element is claimed by the implementor." docs/CONFORMANCE.md §5 already documents the consequence
    //    — for a Not-claimed module "a parse error or a named error is the conforming posture". A.4.1 ¶2 extends
    //    the licence from the 27 named elements to every syntax rule, general rule and exception condition hanging
    //    off them, which is what carries the screen description entry's clauses and the EC-SCREEN family.
    //
    //    ⛔ WHY TWO CODES. A Format-3 ACCEPT, a Format-2 DISPLAY and a Format-6 SET can only name a screen-name,
    //    and a screen-name can only be declared in a SCREEN SECTION — so EVERY statement witness necessarily
    //    carries the data-division surface too and would draw 1560 whether or not the STATEMENT was diagnosed.
    //    One code would make every procedure-division witness pass for the wrong reason
    //    (feedback_green_gates_arent_evidence). Splitting the module at the division boundary makes each `.err`
    //    an actual observation: 1560 can never be produced by a statement site, 1707 never by a data site.
    /// <summary>COBOLNET1560 — the A.4.2 DATA/ENVIRONMENT surface: the SCREEN SECTION header (§13.9), every
    /// screen description entry clause (§13.17 / §13.18.x), and the SPECIAL-NAMES CURSOR and CRT STATUS clauses
    /// (§12.3.7). The emitted message NAMES the construct seen — <see cref="Binding.ScreenFacility"/> is the one
    /// funnel and derives the clause name and its ISO § from the parse-tree rule.</summary>
    public static readonly DiagnosticDescriptor ScreenFacilityUnsupported = new(
        "COBOLNET1560", "screen-facility-unsupported", EditionSeverity.Error,
        "A SCREEN SECTION construct (the section header §13.9, a screen description entry §13.17, one of its "
        + "clauses §13.18.x, or the SPECIAL-NAMES CURSOR / CRT STATUS clause §12.3.7) is part of the OPTIONAL "
        + "screen handling module (§4.2.7; Annex A.4.2), for which COBOL.NET claims no support — A.4.1 admits "
        + "an optional element's syntax only when support is claimed, so it is refused by name rather than "
        + "silently accepted and dropped. The facility exists from COBOL-2002. See docs/CONFORMANCE.md §5.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.2 / §13.9 / §13.17 / §12.3.7");
    /// <summary>COBOLNET1707 — the A.4.2 PROCEDURE-division surface: ACCEPT format 3 (screen, §14.9.1), DISPLAY
    /// format 2 (screen, §14.9.11 — A.4.2 item 9 misprints the cross-reference as 14.9.10, which is DELETE),
    /// SET format 6 (attribute, §14.9.39), and the EC-SCREEN exception-names in the six contexts A.4.2 item 10
    /// names (RAISING on EXIT / GOBACK / the procedure division header, USE, the PERFORM WHEN phrase, RAISE, and
    /// the &gt;&gt;TURN directive).</summary>
    public static readonly DiagnosticDescriptor ScreenStatementUnsupported = new(
        "COBOLNET1707", "screen-statement-unsupported", EditionSeverity.Error,
        "A screen-handling STATEMENT or exception-name — ACCEPT format 3 (§14.9.1), DISPLAY format 2 (§14.9.11), "
        + "SET format 6 ATTRIBUTE (§14.9.39), or an EC-SCREEN exception-name in a RAISING phrase, a USE "
        + "statement, a PERFORM WHEN phrase, a RAISE statement or a >>TURN directive — is part of the OPTIONAL "
        + "screen handling module (§4.2.7; Annex A.4.2 items 1, 9, 10, 24), for which COBOL.NET claims no "
        + "support. A.4.1 admits an optional element's syntax only when support is claimed; a screen ACCEPT or "
        + "DISPLAY silently re-read as its device format would transfer the wrong data, and a catalogued "
        + "EC-SCREEN name with no raise site reads as implemented to every consumer that can see it. "
        + "See docs/CONFORMANCE.md §5.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.2 items 1, 9, 10, 24 / §14.9.1 / §14.9.11 / §14.9.39");

    // ── THE THREE BAND CODES THAT HAD NO DESCRIPTOR AT ALL (kb/Work PB175) ────────────────────────────
    //    COBOLNET0869 / 0881 / 1529 were emitted from 38 BARE STRING LITERALS across five binders and from
    //    nowhere else — no catalogue row, so no `docs/DIAGNOSTICS.md` row, no drift test, and nothing for
    //    `session-probe`'s next-free scan or the suppress-key machinery to see. They are BANDS: one code over
    //    a family of neighbouring rules, the shape COBOLNET1720 (`io-phrase-forbidden-here`, three syntax
    //    rules) and COBOLNET1707 (four statement formats) already use, so the descriptor's Title states the
    //    BAND and each site composes the rule it caught. Converting the sites changes no emitted byte:
    //    `Error(DiagnosticDescriptor, string)` forwards to `Error(descriptor.Code, string)`.
    /// <summary>COBOLNET0869 — the POINTER / ADDRESS OPERAND band: what may be written where a pointer,
    /// an address or an object reference is expected, and what a restricted data-pointer narrows that to.
    /// Covers ISO §14.9.39 (SET Formats 5–7 — SET ADDRESS OF / a pointer receiver / an index-name),
    /// §8.4.3.11 and §8.4.3.13 (the ADDRESS OF and LENGTH OF special registers as operands), §8.8.4.2.16
    /// (the pointer relation condition), §14.9.3 (ALLOCATE's RETURNING operand) and Annex D.9.2.2 (the
    /// restricted data-pointer, whose target type constrains every one of the above).</summary>
    public static readonly DiagnosticDescriptor PointerOperandShape = new(
        "COBOLNET0869", "pointer-operand-shape", EditionSeverity.Error,
        "A pointer, address or object-reference OPERAND is not of a shape its statement admits — the SET statement's pointer formats (ISO §14.9.39), the ADDRESS OF / LENGTH OF special registers (§8.4.3.11, §8.4.3.13), the pointer relation condition (§8.8.4.2.16), the ALLOCATE statement's RETURNING operand (§14.9.3), or the target-type restriction a RESTRICTED data-pointer (Annex D.9.2.2) puts on all of them. The site names the rule it caught.",
        "ISO §14.9.39 / §8.4.3.11 / §8.4.3.13 / §8.8.4.2.16 / §14.9.3 / Annex D.9.2.2");
    /// <summary>COBOLNET0881 — the USAGE-CLAUSE COMPATIBILITY band: which other data description clauses may
    /// share an entry with which usage. Covers ISO §13.18.60.3 (the USAGE clause's own syntax rules, incl.
    /// SR18's restricted-pointer TYPEDEF requirement) and §13.18.60.4, plus the clauses those rules exclude —
    /// PICTURE (§13.18.40.3), VALUE (§13.18.63), BLANK WHEN ZERO, JUSTIFIED, SIGN and SYNCHRONIZED.</summary>
    public static readonly DiagnosticDescriptor UsageClauseCompatibility = new(
        "COBOLNET0881", "usage-clause-compatibility", EditionSeverity.Error,
        "A data description entry combines a USAGE with a clause its syntax rules exclude, or omits one they require (ISO §13.18.60.3 / §13.18.60.4) — a PICTURE (§13.18.40.3) or VALUE (§13.18.63) clause on a usage that admits neither, a restricted `USAGE POINTER TO type-name` whose subject carries no TYPEDEF clause (SR18), and the neighbouring clause exclusions. The site names the rule it caught.",
        "ISO §13.18.60.3 / §13.18.60.4 / §13.18.40.3 / §13.18.63");
    /// <summary>COBOLNET1529 — the TYPE DECLARATION band: the shape a TYPEDEF entry and a TYPE reference must
    /// have. Covers ISO §13.18.58 (the TYPEDEF clause), §13.18.57.3 (the TYPE clause's syntax rules) and
    /// §8.5.3.1 / §8.5.3.3 (type declarations and strong typing — including SR1's prohibition on an
    /// ELEMENTARY type definition carrying the STRONG phrase).</summary>
    public static readonly DiagnosticDescriptor TypeDeclarationShape = new(
        "COBOLNET1529", "type-declaration-shape", EditionSeverity.Error,
        "A TYPEDEF entry or a TYPE reference is malformed (ISO §13.18.58 TYPEDEF, §13.18.57.3 TYPE, §8.5.3.1 / §8.5.3.3 type declarations and strong typing) — a type declaration at the wrong level or under another entry, an unnamed (FILLER) one, TYPEDEF combined with a clause it excludes, or an ELEMENTARY type definition carrying the STRONG phrase, which §8.5.3.1 forbids. The site names the rule it caught.",
        "ISO §13.18.58 / §13.18.57.3 / §8.5.3.1 / §8.5.3.3");

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
