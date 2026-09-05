// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Data division rules: sections, file descriptions, data description entries,
// and the INITIALIZE statement.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolData;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// DATA DIVISION
// ==========================================

dataDivision
    : DATA DIVISION DOT
      fileSection?
      workingStorageSection?
      localStorageSection?
      linkageSection?
      reportSection?
      screenSection?
    ;

// ==========================================
// FILE SECTION
// ==========================================

fileSection
    : FILE SECTION DOT (fileDescriptionEntry | sortMergeDescriptionEntry)*
    ;

fileDescriptionEntry
    : FD fileName fileDescriptionClauses? DOT dataDescriptionEntry*
    ;

// SD entry — per §13.4.6.3, only RECORD clause is permitted (not BLOCK, CODE-SET, etc.)
sortMergeDescriptionEntry
    : SD fileName sortMergeDescriptionClauses? DOT dataDescriptionEntry*
    ;

sortMergeDescriptionClauses
    : sortMergeDescriptionClause+
    ;

sortMergeDescriptionClause
    : recordClause
    | dataRecordsClause
    | genericFileDescriptionClause
    ;

fileDescriptionClauses
    : fileDescriptionClause+
    ;

fileDescriptionClause
    : organizationClause
    | accessModeClause
    | recordKeyClause
    | alternateKeyClause
    | fileStatusClause
    | blockContainsClause
    | recordClause
    | codeSetClause
    | formatClause
    | labelRecordsClause
    | dataRecordsClause
    | valueOfClause
    | fileGlobalExternalClause
    | linageClause
    | reportClause
    | genericFileDescriptionClause
    ;

// FORMAT clause (§13.18.24.2, printed general format p403 — RENDERED, not read off the OCR).
//
// ⛔ RECOGNIZE-ONLY. The FORMAT clause is item 1) of Annex A.4.8, an OPTIONAL language element this
// implementation does NOT claim (docs/CONFORMANCE.md §5). A.4.1: "An implementation shall accept the syntax
// and provide the functionality for an optional element only when support for that language element is
// claimed by the implementor" — so the clause is parsed in order to be REFUSED BY NAME at bind
// (DataBinder → COBOLNET1705), never bound and never silently ignored. Compiling it inert would change
// which bytes reach the medium (§13.18.24.4 GR1/GR2) — a wrong answer, not a missing facility, which is why
// this is an Error and not the additive-facility warning band (COBOLNET1560/1578/1579/1580).
//
// SHAPE: `FORMAT { | BIT | CHARACTER | NUMERIC | } DATA`. The three alternatives are enclosed in CHOICE
// INDICATORS (the `|` bars just inside the braces), so §5.2.6.4 makes them ONE OR MORE, each at most once,
// IN ANY ORDER — `FORMAT BIT CHARACTER DATA` is legal source, not a syntax error. `DATA` is NOT underlined
// in the printed format: an optional word. The "each at most once" half is a §5.2.6.4 syntax rule an EBNF
// `+` cannot express; it is not enforced separately because the whole clause is refused either way.
formatClause
    : FORMAT (BIT | CHARACTER | NUMERIC)+ DATA?
    ;

// REPORT(S) clause (§13.18.46): names the report(s) produced on this file by the Report Writer.
reportClause
    : (REPORT IS? | REPORTS ARE?) reportName+
    ;

// IS GLOBAL / IS EXTERNAL on an FD (§13.18.27/§13.18.22): GLOBAL makes the file-name and
// record visible to contained programs; EXTERNAL shares the file across the run unit.
// Parsed here; GLOBAL visibility is handled by nested-program name resolution.
fileGlobalExternalClause
    : IS? (GLOBAL | EXTERNAL)
    ;

// BLOCK CONTAINS clause (§13.18.10)
blockContainsClause
    : BLOCK CONTAINS? integerLiteral (TO integerLiteral)? (CHARACTERS | RECORDS)?
    ;

// RECORD clause (§13.18.43) — fixed-length, variable-length, or VARYING forms
recordClause
    : RECORD CONTAINS? integerLiteral (TO integerLiteral)? CHARACTERS?
    | RECORD IS? VARYING IN? SIZE? (FROM? integerLiteral)? (TO integerLiteral)? CHARACTERS? (DEPENDING ON? dataReference)?
    ;

// CODE-SET clause (§13.18.13.2 — the 2002 two-class format; kb/Work PB110): IS alphabet-name-1
// [alphabet-name-2], or the FOR ALPHANUMERIC / FOR NATIONAL phrases — one or both, any order (the inner brace
// carries choice indicators, §5.2.6.4; the binder enforces each class at most once). The '85 one-name form is
// the first alternative's degenerate case.
codeSetClause
    : CODE_SET IS? cobolWord cobolWord?
    | CODE_SET codeSetForPhrase+
    ;

codeSetForPhrase
    : FOR (ALPHANUMERIC | NATIONAL) IS? cobolWord
    ;

// LABEL RECORD(S) IS/ARE — obsolete COBOL-85 FD clause, semantically inert
labelRecordsClause
    : LABEL (RECORD IS? | RECORDS ARE?) (STANDARD | OMITTED | cobolWord+)
    ;

// DATA RECORD(S) IS/ARE — obsolete COBOL-74 FD clause, semantically inert
dataRecordsClause
    : DATA (RECORD IS? | RECORDS ARE?) cobolWord+
    ;

// VALUE OF implementor-name IS data-name/literal — obsolete COBOL-85 FD clause
// (§13.18 removed feature), semantically inert. Operands are implementor-defined label
// fields; consume the word/literal/IS sequence until the next clause keyword or the period.
valueOfClause
    : VALUE OF (cobolWord | literal | IS)+
    ;

// LINAGE clause (ISO §13.18.34) — page-based printing for sequential files
linageClause
    : LINAGE IS? (dataReference | integerLiteral) LINES?
      linageFootingPhrase?
      linageLinesAtTopPhrase?
      linageLinesAtBottomPhrase?
    ;

linageFootingPhrase
    : WITH? FOOTING AT? (dataReference | integerLiteral)
    ;

linageLinesAtTopPhrase
    : LINES? AT? TOP (dataReference | integerLiteral)
    ;

linageLinesAtBottomPhrase
    : LINES? AT? BOTTOM (dataReference | integerLiteral)
    ;

genericFileDescriptionClause
    : genericClause
    ;

// ==========================================
// OTHER DATA SECTIONS
// ==========================================

workingStorageSection
    : WORKING_STORAGE SECTION DOT dataDescriptionEntry*
    ;

localStorageSection
    : LOCAL_STORAGE SECTION DOT dataDescriptionEntry*
    ;

linkageSection
    : LINKAGE SECTION DOT linkageEntry*
    ;

linkageEntry
    : dataDescriptionEntry
    | linkageProcedureParameter
    ;

// Procedure parameters (COBOL 2002+)
linkageProcedureParameter
    : {is2002()}? levelNumber dataName? parameterDescriptionBody DOT
    ;

parameterDescriptionBody
    : parameterPassingClause (dataDescriptionClause+)?
    ;

parameterPassingClause
    : USING (BY? REFERENCE | BY? VALUE | BY? CONTENT)? dataReference   // BY optional everywhere (kb/Work PB130)
    ;

// ==========================================
// DATA DESCRIPTION ENTRIES
// ==========================================

dataDescriptionEntry
    : levelNumber dataName? dataDescriptionBody DOT
    ;

levelNumber
    : INTEGERLIT
    ;

dataName
    : cobolWord
    | FILLER
    | PROCEDURE    // NC205A: PROCEDURE used as a data name (77 PROCEDURE-DIVISION PIC X)
    // kb/Work PB137: the reservation-gated words leave cobolWord exactly where §8.9 reserves them (so operand
    // lists cannot absorb the bare facility verbs), but a DECLARATION naming one must still PARSE so the §8.9
    // funnel's targeted COBOLNET0901 can NAME the reserved word instead of a generic parse error (the
    // user-word-commit pin). ⛔ THIS WAS A HAND-WRITTEN LIST OF TWO WORDS (COMMIT/ROLLBACK) and it silently
    // rotted: CRT and CURSOR became reservation-gated with kb/Work PB301 and were never added here, so
    // `01 CRT PIC X.` at --std 2002 answered COBOL0001 "no viable alternative" instead of naming §8.9.
    // `reservedGatedWord` is GENERATED from the SAME cobol-words.json `reservationGated` flag that generates
    // the cobolWord gates (kb/Work PB300, CLAUDE.md rule 5), so the two halves cannot drift apart and the next
    // gated word is automatic; CobolWordsDriftTests pins both directions. PROCEDURE stays a separate
    // alternative on purpose — it is reserved at EVERY edition and NC205A must keep compiling, so it must NOT
    // reach the funnel.
    | reservedGatedWord
    ;

dataDescriptionBody
    : constantEntryBody
    | dataDescriptionClauses
    | renamesClause
    ;

// Constant entry (ISO §13.10, COBOL-2002): {1|01} constant-name CONSTANT [IS GLOBAL]
// {AS {arithmetic-expression-1 | BYTE-LENGTH OF data-name-1 | literal-1 | LENGTH OF data-name-2}
//  | FROM compilation-variable-name-1}.
// SUPERSET PARSE at every edition — the COBOL-2002 introduction gate is the VersionConformancePass parse arm
// (VisitConstantEntryBody → constant-entry-2002 → COBOLNET0900 below 2002). LL-disjoint from the clause list
// (no dataDescriptionClause begins with CONSTANT except constantRecordClause, whose SECOND token RECORD
// separates it) and from renamesClause (RENAMES). The binder (DataBinder.Constants.cs) folds the entry into
// the compile-time constant table — a constant occupies NO storage (§13.10.4 GR1/GR3: references substitute
// the literal).
constantEntryBody
    : CONSTANT (IS? GLOBAL)? (AS constantValue | FROM cobolWord)
    ;

// The AS operand (§13.10.2). LENGTH OF is listed FIRST so it wins over arithmeticExpression's qualified-
// dataReference reading of the same tokens (`LENGTH OF X` — LENGTH is a cobolWord). A single numeric literal
// rides arithmeticExpression and is re-classified as a LITERAL by the binder (§13.10.3 SR1); the BYTE-LENGTH
// form (no dedicated token — §15.14 BYTE-LENGTH is itself a deferred intrinsic) rides arithmeticExpression as
// the qualified dataReference `BYTE-LENGTH OF x` and is recognized by the binder (staged loud until the
// §15.14 byte-width authority lands).
constantValue
    : LENGTH OF dataReference
    | nonNumericLiteral
    | arithmeticExpression
    ;

// ==========================================
// DATA DESCRIPTION CLAUSES
// ==========================================

dataDescriptionClauses
    : dataDescriptionClause*
    ;

dataDescriptionClause
    : pictureClause
    | usageClause
    | occursClause
    | redefinesClause
    | valueClause
    | signClause
    | syncClause
    | justifiedClause
    | blankWhenZeroClause
    | constantRecordClause   // COBOL-2002 §13.18.15; superset parse, introduction-gated by VersionConformancePass ParseArm.VisitConstantRecordClause
    | propertyClause   // COBOL-2002; parses at all editions (superset), introduction-gated post-bind by VersionConformancePass ParseArm.VisitPropertyClause (rearch 14g.2). (The VALUE-list PROPERTY guards below are KEPT — they are value-operand disambiguation, not an edition gate.)
    | externalClause
    | globalClause
    | typeClause
    | typedefClause
    | sameAsClause
    | basedClause
    | anyLengthClause
    | dynamicLengthClause
    | genericDataClause
    | groupUsageClause   // COBOL-2002 §13.18.29 (kb/Work PB79); superset parse, introduction-gated by VersionConformancePass ParseArm.VisitGroupUsageClause
    | selectWhenClause   // ISO §13.18.51 — Annex A.4.8 item 2), DECLINED: recognize-only, refused by name at bind (COBOLNET1705)
    | {is2002()}? validationClause   // ISO §13.16.2 validation-clauses — Annex A.4.14, DECLINED: recognize-only, refused by name at bind (COBOLNET1708); the rule and its rationale are in Grammar/Core/CobolDeclined.g4
    ;

// SELECT WHEN clause (§13.18.51.2, printed general format p481 — RENDERED).
//
// ⛔ RECOGNIZE-ONLY, the twin of formatClause above: item 2) of Annex A.4.8, an OPTIONAL element this
// implementation does not claim (docs/CONFORMANCE.md §5), parsed so it can be REFUSED BY NAME at bind
// (COBOLNET1705). An inert SELECT WHEN would select the WRONG record description entry
// (§13.18.51.4 GR1/GR2) with a status-45 failure path — a wrong answer, so Error, not the warning band.
//
// SHAPE: `SELECT WHEN { condition-name-1 | OTHER }`. Plain braces, NO choice indicators: exactly one of the
// two. SELECT, WHEN and OTHER are underlined (required words); condition-name-1 is a user-defined word.
// OTHER leads so ANTLR's first-match cannot route it into the conditionName slot (OTHER is reserved at every
// edition and is not cobolWord-admitted, so this is belt-and-braces, not load-bearing).
selectWhenClause
    : SELECT WHEN (OTHER | conditionName)
    ;

// condition-name-1 of the SELECT WHEN clause (§13.18.51.2) — a user-defined word, the level-88 name slot.
conditionName
    : cobolWord
    ;

// GROUP-USAGE clause (COBOL-2002 §13.18.29): the group item is treated as an elementary item of usage bit /
// category boolean (BIT) or usage national / category national (NATIONAL) — data-model design D20. Only the
// GROUP-USAGE token is unique to this clause; BIT / NATIONAL stand alone here (no USAGE prefix), which is why
// they are dedicated tokens rather than cobolWord-admitted here.
groupUsageClause
    : GROUP_USAGE IS? (BIT | NATIONAL)
    ;

// EXTERNAL clause (§13.18.22) — shared storage across run unit
propertyClause
    : PROPERTY (WITH? NO (GET | SET))? (IS? FINAL)?   // §13.18.42.2 :21146-21148 (WITH optional per the IS?-style tolerance)
    ;

externalClause
    : IS? EXTERNAL
    ;

// BASED clause (COBOL-2002 §13.18.5) — level 01/77 only; the item is a template with an implicit
// data-address pointer (initially NULL) and NO storage until SET ADDRESS OF / ALLOCATE gives it one.
basedClause
    : BASED   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitBasedClause (rearch 14g.2)
    ;

// CONSTANT RECORD clause (COBOL-2002 §13.18.15) — identifies a STRUCTURED CONSTANT: the record's content is
// its normal initial content (§13.18.15.4 GR1 — as though INITIALIZE … WITH FILLER ALL TO VALUE THEN TO
// DEFAULT), and neither the record nor any subordinate may be a receiving operand (SR2 → COBOLNET1548 at
// bind). Structural SRs (§13.18.15 SR1 WS/LS-only; §13.16.3 SR3/SR6/SR13) bind-check in DataBinder.
constantRecordClause
    : CONSTANT RECORD   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitConstantRecordClause
    ;

// ANY LENGTH clause (COBOL-2002 §13.18.2) — the length of a LINKAGE item varies at runtime with the length
// of the corresponding argument (GR1). UNGATED superset parse (the basedClause pattern); the SR1–SR4 shape
// rules bind-check in DataBinder and the placement sweeps.
anyLengthClause
    : ANY LENGTH   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitAnyLengthClause
    ;

// DYNAMIC LENGTH clause (ISO §8.5.1.10 / §13.18.19, COBOL-2014) — a variable-length, minimum-length-zero PIC X or
// PIC N string. Format: DYNAMIC LENGTH [dynamic-length-structure-name] [LIMIT IS? integer]. UNGATED superset parse
// (the anyLengthClause pattern); the §13.18.19.3 SR1 (PICTURE exactly one N or X), §13.16.3 SR18 (permitted
// co-clauses), and the structure-name non-support bind-check in DataBinder (COBOLNET1561/1562/1563), and the
// COBOL-2014 introduction gate is VersionConformancePass ParseArm.VisitDynamicLengthClause → COBOLNET0900 below
// 2014. LL-disjoint from occursClause: occursClause always leads with OCCURS (its Format-4 dynamic-capacity
// alternative has DYNAMIC only as the SECOND token); here DYNAMIC is the leading token, and DYNAMIC appears
// nowhere else at the start of a dataDescriptionClause — so tokens DYNAMIC/LENGTH/LIMIT need no lexer change.
dynamicLengthClause
    : DYNAMIC LENGTH cobolWord? (LIMIT IS? integerLiteral)?
    ;

// GLOBAL clause (§13.18.27) — visible to contained programs
globalClause
    : IS? GLOBAL
    ;

// TYPE clause (ISO §13.18.57; the TYPEDEF clause it names is §13.18.58): PROVISIONAL COBOL-2002 edge — the former {is2023()}? gate was
// PROVABLY wrong (ISO-validation, DEVLOG 582: TYPEDEF has ~33 hits in the 2023 spec body yet ZERO Annex E
// 2014→2023 change rows ⇒ it predates 2023). The 2002-vs-2014 refinement is blocked on the older standards
// (roadmap decision 1 provisional policy; tests/version-matrix/constructs.json row type-clause-2002).
typeClause
    : TYPE IS? IDENTIFIER   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitTypeClause (rearch 14g.2)
    ;

// TYPEDEF clause (ISO §13.18.58, COBOL-2002; data-model D17) — marks this data description entry as a TYPE
// DECLARATION (a named template; it allocates no storage). STRONG (§13.18.58.2) makes the type strongly-typed.
// LL-disjoint from externalClause/globalClause (IS? EXTERNAL | GLOBAL): the keyword after the optional IS differs.
typedefClause
    : IS? TYPEDEF STRONG?   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitTypedefClause (recognition; rearch 14g.2, DEVLOG 734)
    ;

// SAME AS clause (ISO §13.18.49, COBOL-2002): the subject takes the SAME data description as data-name-1's
// entry, subordinates included (GR1/GR2 — coded in place, minus data-name-1's level/name/CONSTANT RECORD/
// EXTERNAL/GLOBAL/REDEFINES/SELECT WHEN; subordinate levels renumber). §13.16.3 SR12 composes it only with
// CONSTANT RECORD / entry-name / EXTERNAL / GLOBAL / level-number / OCCURS — enforced at bind (COBOLNET1555).
// The target may be QUALIFIED (OF/IN — data-name-1 is an ordinary data-name reference) but never subscripted
// (§13.18.49 SR1 — data-name-1 shall not be subject to any OCCURS clause). LL-disjoint from every other
// dataDescriptionClause (unique leading token SAME; the I-O-CONTROL sameArea rule is a different context).
// Expansion rides the ONE TYPEDEF clone machinery (DataBinder.ExpandSameAs → CloneItem; data-model D17).
sameAsClause
    : SAME AS cobolWord ((OF | IN) cobolWord)*   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitSameAsClause (recognition; the typedefClause pattern)
    ;

genericDataClause
    : genericClause
    ;

// PIC Clause — PIC/PICTURE triggers PICMODE in the lexer, which emits a single PIC_STRING token (IS is consumed
// by PICMODE). The optional trailing EDITING phrases (ISO §13.18.40.2 Format 1, COBOL-2023) are lexed in DEFAULT
// mode: PIC_STRING stops at whitespace and pops PICMODE, so ` EDITING …` follows as an ordinary token stream.
// The whole EDITING group is additive/repeatable; SR11 (distinct character-1) is enforced at bind. The 2023
// introduction gate is VersionConformancePass ParseArm.VisitPictureClause (recognition on editingPhrase presence).
pictureClause
    : PIC PIC_STRING editingPhrase* pictureLocalePhrase?
    ;

// PICTURE Format 2 (locale) — `PIC IS character-string-1 LOCALE [IS locale-name-1] SIZE IS integer-1` (ISO
// §13.18.40.2; LIVE since kb/Work PB64 T6 — the item is fixed-point numeric-edited with locale editing, bound by
// DataBinder's format-2 arm; the 2002 introduction is the picture-locale-format2-2002 construct gate, so the
// predicate is NOT edition-gated — see pictureLocaleAhead's comment). LOCALE is not a lexer token (a plain word
// at COBOL-85, reserved 2002+), so the arm is text-predicated; the first cobolWord IS the word LOCALE. Both IS
// words are optional (non-underlined in the §13.18.40.2 figure; §5.2.3) — `LOCALE FR SIZE 12` is legal, and the
// required IS this rule used to demand before locale-name-1 rejected legal source (kb/Work PB114). A superset
// parse admits editingPhrase* alongside; format 2 has no EDITING phrase and the binder diagnoses the pairing.
pictureLocalePhrase
    : {pictureLocaleAhead()}? cobolWord (IS? cobolWord)? SIZE IS? integerLiteral
    ;

// EDITING character-1 { IS literal-1 | FOR { NEGATIVE/POSITIVE choice } } (ISO §13.18.40.2 Format 1). character-1
// and the literals are quoted literals (bind-validated: SR8 legal letter, SR9 class/≤50). `IS` is optional noise
// (non-underlined in the figure). The FOR sub-group carries CHOICE INDICATORS (§5.2.6.4) — NEGATIVE and/or
// POSITIVE, each at most once, in either order — so it is TWO ordered alternatives, not the exclusive stacked
// braces the OCR transcription implied. Parse-wide/bind-narrow: `literal` (broad) surfaces SR violations as NAMED
// bind diagnostics, never an ANTLR parse error.
editingPhrase
    : EDITING literal ( IS? literal | FOR editingForPhrase )
    ;

editingForPhrase
    : NEGATIVE IS? literal ( POSITIVE IS? literal )?
    | POSITIVE IS? literal ( NEGATIVE IS? literal )?
    ;

// USAGE Clause (ISO §13.18.60.2): `[USAGE IS] usage-keyword` — the USAGE keyword is OPTIONAL for EVERY usage, so
// the bare and the prefixed spellings are ONE alternative over ONE usageKeyword rule (kb/Work PB95: the former
// hand-listed bare alternatives omitted POINTER, OBJECT REFERENCE, NATIONAL, BIT, PROGRAM-POINTER and
// FUNCTION-POINTER, so `PIC 1(3) BIT` was a parse error). The optional binarySign applies to the COBOL-2002
// BINARY-CHAR/SHORT/LONG/DOUBLE usages; it is grammatically tolerated after any usageKeyword and rejected by the
// binder for non-binary ones, as is noSignPhrase off PACKED-DECIMAL (§13.18.60.4 GR11, 2023). A 2002 usage word
// that is a USER word at 85 (BIT, NATIONAL, PROGRAM-POINTER, FUNCTION-POINTER — all cobolWord) needs no predicate
// here: in `05 BIT.` the entry-NAME alternative is tried first and wins (an '85 item named BIT), and `05 X BIT.`
// reads as the usage, which the binder / VersionConformancePass then names as the 2002 introduction — the
// superset-parse / bind-narrow direction of DESIGN-version-conformance-pipeline.
usageClause
    : (USAGE IS?)? usageKeyword binarySign? noSignPhrase? floatFormatPhrase*
    ;

// USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.2 / GR11 — a COBOL-2023 addition): no trailing sign nibble.
// Grammatically tolerated after any usageKeyword; the binder rejects it on a non-PACKED-DECIMAL usage (COBOLNET1565)
// and rejects an 'S' picture with NO SIGN (SR31, COBOLNET1566). WITH is the conventional optional noise word.
noSignPhrase
    : WITH? NO SIGN
    ;

// The floating-point FORMAT phrases of the USAGE clause (ISO §13.18.60.2 general format, verified against the
// PRINTED page — PDF p.533 / printed 503): `FLOAT-BINARY-32/-64/-128 [ endianness-phrase ]`, and FLOAT-DECIMAL-16/
// -34 followed by a BRACKETED CHOICE-INDICATOR group over { encoding-phrase, endianness-phrase }. §5.2.6.4 gives
// that group its semantics — "When enclosed by brackets, zero or more of the alternatives contained within the
// choice indicators shall be specified, but any single alternative may be specified only once" and "The
// alternatives may be specified in any order" — which IS the `*` written here plus a binder duplicate screen
// (COBOLNET1718). The `*` is on the CLAUSE, not inside usageKeyword, and that placement is load-bearing:
// DataBinder.UsageKeyword derives the canonical keyword from the usageKeyword node, so a phrase INSIDE it would
// glue ("FLOAT-BINARY-32HIGH-ORDER-LEFT") and fall to PictureAnalyzer.ParseUsage's internal-error arm — the exact
// failure that method's own doc comment records for bare `BINARY-CHAR SIGNED` (the W2 loud-guard sweep).
// Grammatically tolerated after ANY usageKeyword, the established binarySign/noSignPhrase posture: the binder
// rejects an endianness-phrase on a non-standard-float usage (COBOLNET1716, §13.18.60.4 GR19c/d scope it) and an
// encoding-phrase on anything but FLOAT-DECIMAL-16/-34 (COBOLNET1717, GR20). The 2014 introduction gate is
// VersionConformancePass ParseArm.VisitUsageClause (Constructs.UsageFloatFormatPhrase2014) — NOT a parse-time
// {is2014()}? predicate (COBOLNET_DESIGN §1.1: this tree has no parse-time edition predicates).
floatFormatPhrase
    : encodingPhrase
    | endiannessPhrase
    ;

// encoding-phrase / endianness-phrase (ISO §13.18.60.2 "where encoding-phrase is" / "where endianness-phrase is").
// ONE definition, THREE citing clauses: the USAGE clause above, the OPTIONS FLOAT-BINARY clause (§11.9.8) and the
// OPTIONS FLOAT-DECIMAL clause (§11.9.9) — the two OPTIONS rules live in CobolParserCore.g4 and reach these
// through the import merge. They are DEFINED here, in the imported fragment, so the reference direction is always
// importing-grammar → imported-rule.
encodingPhrase
    : BINARY_ENCODING
    | DECIMAL_ENCODING
    ;

endiannessPhrase
    : HIGH_ORDER_LEFT
    | HIGH_ORDER_RIGHT
    ;

usageKeyword
    : DISPLAY
    | COMPUTATIONAL
    | COMPUTATIONAL_1
    | COMPUTATIONAL_2
    | COMPUTATIONAL_3
    | COMPUTATIONAL_4
    | COMPUTATIONAL_5
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | COMP_4
    | COMP_5
    | FLOAT_SHORT
    | FLOAT_LONG
    | FLOAT_EXTENDED
    | FLOAT_BINARY_32 | FLOAT_BINARY_64 | FLOAT_BINARY_128     // §13.18.60.4 GR14-16 IEEE binary32/64/128 (2014)
    | FLOAT_DECIMAL_16 | FLOAT_DECIMAL_34                       // §13.18.60.4 GR17-18 IEEE decimal64/128 (2014)
    | BINARY_CHAR
    | BINARY_SHORT
    | BINARY_LONG
    | BINARY_DOUBLE
    | BINARY
    | PACKED_DECIMAL
    | INDEX
    | NATIONAL
    | BIT
    | dataPointerUsage       // USAGE POINTER [TO type-name] (§13.18.60.2; the TO form is a RESTRICTED data-pointer, GR23)
    | programPointerUsage    // USAGE PROGRAM-POINTER [TO prototype] (§13.18.60 GR24/GR25, 2002) — introduction-gated post-bind (VersionConformancePass UsageConstructId)
    | functionPointerUsage   // USAGE FUNCTION-POINTER [TO prototype] (§13.18.60, 2002) — superset parse; semantics STAGED LOUD (function prototypes = P13)
    | objectReferenceUsage   // USAGE OBJECT REFERENCE [class] (OO/2002) — introduction-gated at BIND time (PicInfo.ParseUsage → ConstructRegistry.Check), like NATIONAL/BIT/POINTER above
    ;

// USAGE POINTER [TO type-name-1] (ISO §13.18.60.2 general format, verified against the PRINTED page — PDF p.533
// = printed 503 prints `POINTER [ TO type-name-1 ]`). The TO form declares a RESTRICTED data-pointer: §13.18.60.4
// GR23 — "If type-name-1 is specified, this data item is a restricted data-pointer. A restricted data-pointer
// shall contain only the predefined address NULL or the address of a data item of the specified type." Written to
// MIRROR its programPointerUsage / functionPointerUsage neighbours below, which is why it is a RULE and not a
// bare terminal with a tail: DataBinder.UsageKeyword derives the canonical keyword from this node, so the operand
// must not be glued into it ("POINTERT" — the OBJECT REFERENCE / PROGRAM-POINTER precedent). §13.18.60.3 SR18
// additionally requires the SUBJECT of a `TO type-name` entry to carry TYPEDEF, screened in the binder.
dataPointerUsage
    : POINTER (TO cobolWord)?
    ;

// USAGE PROGRAM-POINTER [TO program-prototype-name-1] (ISO §13.18.60 :22686): a program-pointer data item —
// may contain the address of a program (GR24; for a COBOL program, the address of an OUTERMOST program). The
// TO form declares a RESTRICTED program-pointer (GR25 — only NULL or a same-signature program's address);
// restriction semantics are STAGED LOUD until the prototype registry lands (P13).
programPointerUsage
    : PROGRAM_POINTER (TO cobolWord)?
    ;

// USAGE FUNCTION-POINTER [TO function-prototype-name-1] (ISO §13.18.60): superset parse at every edition;
// the semantics stage LOUD (function prototypes are the P13 repository work).
functionPointerUsage
    : FUNCTION_POINTER (TO cobolWord)?
    ;

// SIGNED (default) / UNSIGNED phrase on a fixed-width binary usage (ISO §13.18.60).
binarySign
    : SIGNED
    | UNSIGNED
    ;

// OCCURS Clause. Each fixed bound (integer-1/integer-2) is an occursBound: an integer literal OR — COBOL-2002
// §13.10.3 SR2 — an integer CONSTANT-NAME ("if constant-name-1 is an integer, it may also be used to specify …
// repetition"; the OCCURS format's integer positions are literal positions, so a constant substitutes there per
// §13.10.4 GR1/GR3). The constant is resolved at BIND time from the compile-time constant table
// (DataBinder.Constants.cs) — a cobolWord bound in a program with no such constant rejects loud (COBOLNET1547).
occursClause
    : OCCURS occursBound (TO occursBound)? timesKeyword?
      (DEPENDING ON? dataReference)?
      occursKeyClause*
      (INDEXED BY? dataReferenceList)?
    // Format 4 — a DYNAMIC-capacity table (ISO §13.18.38 Format 4, COBOL-2014; D9). LL-disjoint from Format 1/2
    // on the token after OCCURS (DYNAMIC is not an integerLiteral). Phrases are order-independent (occursDynamicPhrase*);
    // duplicate/SR28 checks are bind-time (COBOLNET1522). Edition-gated so a pre-2014 probe upgrades to COBOLNET0900.
    | OCCURS DYNAMIC occursDynamicPhrase* occursKeyClause* (INDEXED BY? dataReferenceList)?   // introduction-gated post-bind by VersionConformancePass ParseArm.VisitOccursClause (rearch 14g.3)
    ;

occursDynamicPhrase
    : CAPACITY IN? dataReference   // CAPACITY IN data-name-3 (the current-capacity register, §13.18.38 GR15)
    | FROM integerLiteral         // integer-4 — the minimum / initial capacity (GR16)
    | TO integerLiteral           // integer-5 — the expected capacity (GR17)
    | INITIALIZED                 // seed new occurrences per §8.5.1.9.5
    ;

// A fixed OCCURS bound: integer-1/integer-2 (§13.18.38), or an integer constant-name (§13.10.3 SR2).
occursBound
    : integerLiteral
    | cobolWord
    ;

occursKeyClause
    : (ASCENDING | DESCENDING) KEY? IS? dataReference+
    ;

timesKeyword
    : TIMES
    ;

// REDEFINES Clause
redefinesClause
    : REDEFINES dataReference
    ;

// RENAMES (Level 66)
renamesClause
    : RENAMES dataReference ((THRU | THROUGH) dataReference)?
    ;

// VALUE Clause — IS is optional noise word
// For level-88 condition entries, valueItem supports THRU ranges.
// Format 3 (§13.18.63): WHEN SET TO FALSE IS literal for condition-names;
//                        IN alphabet-name for character comparisons.
// The valueItem loop guard: at 2002+ PROPERTY is reserved (ISO 8.9) so it can NEVER be a constant-name
// operand — without the predicate the greedy loop consumes it as a cobolWord and the propertyClause
// (13.18.42) that follows VALUE never matches. At 85 PROPERTY stays a legal user word (the XOR recipe).
valueClause
    // Format 2 (table, ISO §13.18.63.2, COBOL-2002) — literals keyed to occurrence subscripts by a MANDATORY FROM
    // phrase. This arm is FIRST: the mandatory FROM terminates the operand loop, so ALL(*) selects it whenever FROM
    // is present; a bare-list-first ordering would consume the literals then die on FROM (DEVLOG note). FROM/TO are
    // not subscript-trigger words, so `FROM (1)` lexes as DEFAULT LPAREN/RPAREN. The 2002 introduction gate is
    // recognition-fired by VersionConformancePass ParseArm.VisitValueClause on a valueClauseTablePhrase.
    : (VALUE | VALUES) (IS | ARE)? valueClauseTablePhrase+
    | (VALUE | VALUES) (IS | ARE)? valueItem ({!(is2002() && TokenStream.LA(1)==PROPERTY)}? COMMA? valueItem)*
      (WHEN SET TO FALSE_ IS? literal)?
      (IN IDENTIFIER)?
      // Format 5 (content-validation-entry, ISO §13.18.63.2) — the DECLINED A.4.14 tail
      // `[IS|ARE] {INVALID|VALID} [WHEN condition-1]`, refused by name with COBOLNET1708 at bind. Written as
      // a tail of the condition-name arm because formats 3 and 5 share their literal/THRU list; the printed
      // format-5 figure differs only in dropping the IS/ARE connective before the list, which this arm's
      // `(IS|ARE)?` already tolerates as a superset. {is2002()}? at the left edge of the optional block: VALID
      // is a user-defined word at COBOL-85 (§8.9).
      ({is2002()}? validateValidPhrase)?
    ;

// One Format-2 table phrase: a literal list, then FROM (subscript-1 …) [TO (subscript-2 …)]. The subscripts are
// integer literals (SR19), one per OCCURS dimension (SR20/SR21) — validated at bind (COBOLNET1585-1590).
valueClauseTablePhrase
    : valueClauseOperand (COMMA? valueClauseOperand)*
      FROM LPAREN integerLiteral (COMMA? integerLiteral)* RPAREN
      (TO LPAREN integerLiteral (COMMA? integerLiteral)* RPAREN)?
    ;

valueItem
    : valueClauseRange
    | valueClauseOperand ({!(is2002() && TokenStream.LA(1)==PROPERTY)}? valueClauseOperand)*
    ;

// SIGN Clause
signClause
    : (SIGN IS?)? (LEADING | TRAILING) (SEPARATE CHARACTER?)?
    ;

// JUSTIFIED / SYNCHRONIZED
justifiedClause
    : (JUSTIFIED | JUST) RIGHT?
    ;

syncClause
    : (SYNCHRONIZED | SYNC) (LEFT | RIGHT)?
    ;

// BLANK [WHEN] ZERO — WHEN is optional per COBOL-85
blankWhenZeroClause
    : BLANK WHEN? ZERO
    ;

// 88-LEVEL CONDITION ENTRIES — handled through valueClause with THRU support.
// Level number and condition name are already consumed by dataDescriptionEntry.
// The conditionEntry88 / valueSet / valueRange rules have been removed;
// valueClause now supports THRU ranges via valueItem for level-88 entries.

// ==========================================
// INITIALIZE (§14.9.20)
// ==========================================

initializeStatement
    : INITIALIZE dataReferenceList (WITH? FILLER)?
      initializeCategoryToValue?
      initializeReplacingPhrase?
      initializeDefaultPhrase?
    ;

// [ALL | category-name] TO VALUE (§14.9.20)
initializeCategoryToValue
    : (ALL | initializeCategory)? TO VALUE
    ;

initializeReplacingPhrase
    : THEN? REPLACING initializeReplacingItem+
    ;

// THEN TO DEFAULT (§14.9.20). DEFAULT is now a token (added for the OPTIONS paragraph, ISO §11.9.6), so the
// phrase matches it directly — tightening the grammar to reject `TO <other-word>` (which the old IDENTIFIER form
// accepted). DEFAULT remains a legal data-name elsewhere (it is in cobolWord).
initializeDefaultPhrase
    : THEN? TO DEFAULT
    ;

// ⛔ ADDITIVE — §14.9.20.3 SR4 states it outright: "a MOVE statement with identifier-2 or literal-1 as the
// SENDING item", so identifier-2 admits a function-identifier (§8.4.3.1.2 Format 1; §8.4.3.2.3 SR1 bars one
// only from a RECEIVING operand). See the writeFrom note above for why this adds an alternative instead of
// collapsing to `moveSendingOperand`: the accessors are load-bearing for the shared legacy binders.
initializeReplacingItem
    : initializeCategory DATA? BY (functionCall | dataReference | literal)
    ;

// Category names for INITIALIZE REPLACING and TO VALUE phrases.
// BOOLEAN, DATA-POINTER, FUNCTION-POINTER, PROGRAM-POINTER, NATIONAL,
// OBJECT-REFERENCE are COBOL-2002+ and require lexer tokens not yet defined.
initializeCategory
    : ALPHABETIC
    | ALPHANUMERIC
    | NUMERIC
    | ALPHANUMERIC EDITED
    | ALPHANUMERIC_EDITED
    | NUMERIC EDITED
    | NUMERIC_EDITED
    ;
