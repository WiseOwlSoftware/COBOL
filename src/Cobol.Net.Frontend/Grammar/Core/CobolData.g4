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

// SD entry — per §13.4.14, only RECORD clause is permitted (not BLOCK, CODE-SET, etc.)
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
    | labelRecordsClause
    | dataRecordsClause
    | valueOfClause
    | fileGlobalExternalClause
    | linageClause
    | reportClause
    | genericFileDescriptionClause
    ;

// REPORT(S) clause (§13.18.46): names the report(s) produced on this file by the Report Writer.
reportClause
    : (REPORT IS? | REPORTS ARE?) reportName+
    ;

// IS GLOBAL / IS EXTERNAL on an FD (§13.18.30/§13.18.23): GLOBAL makes the file-name and
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

// CODE-SET clause (§13.18.13)
codeSetClause
    : CODE_SET IS? cobolWord
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

// LINAGE clause (ISO §13.16) — page-based printing for sequential files
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
    : USING (BY REFERENCE | BY VALUE | BY CONTENT)? dataReference
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
    | basedClause
    | anyLengthClause
    | genericDataClause
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

// GLOBAL clause (§13.18.27) — visible to contained programs
globalClause
    : IS? GLOBAL
    ;

// TYPE clause (TYPEDEF family, ISO §13.18.58): PROVISIONAL COBOL-2002 edge — the former {is2023()}? gate was
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

genericDataClause
    : genericClause
    ;

// PIC Clause — PIC/PICTURE triggers PICMODE in the lexer,
// which emits a single PIC_STRING token. IS is consumed by PICMODE.
pictureClause
    : PIC PIC_STRING
    ;

// USAGE Clause. The optional binarySign applies to the COBOL-2002 BINARY-CHAR/SHORT/LONG/DOUBLE usages
// (ISO §13.18.60); it is grammatically tolerated after any usageKeyword and ignored for non-binary ones.
usageClause
    : USAGE IS? usageKeyword binarySign?   // full form: USAGE IS DISPLAY  /  USAGE IS BINARY-CHAR SIGNED
    | DISPLAY                        // bare keyword forms (no USAGE prefix)
    | COMPUTATIONAL                  // per ISO §13.16 — USAGE keyword is optional
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
    | (BINARY_CHAR | BINARY_SHORT | BINARY_LONG | BINARY_DOUBLE) binarySign?   // bare BINARY-xxx [SIGNED|UNSIGNED]
    | BINARY
    | PACKED_DECIMAL
    | INDEX
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
    | BINARY_CHAR
    | BINARY_SHORT
    | BINARY_LONG
    | BINARY_DOUBLE
    | BINARY
    | PACKED_DECIMAL
    | INDEX
    | NATIONAL
    | BIT
    | POINTER
    | programPointerUsage    // USAGE PROGRAM-POINTER [TO prototype] (§13.18.60 GR24/GR25, 2002) — introduction-gated post-bind (VersionConformancePass UsageConstructId)
    | functionPointerUsage   // USAGE FUNCTION-POINTER [TO prototype] (§13.18.60, 2002) — superset parse; semantics STAGED LOUD (function prototypes = P13)
    | objectReferenceUsage   // USAGE OBJECT REFERENCE [class] (OO/2002) — introduction-gated at BIND time (PicInfo.ParseUsage → ConstructRegistry.Check), like NATIONAL/BIT/POINTER above
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
    : (VALUE | VALUES) (IS | ARE)? valueItem ({!(is2002() && TokenStream.LA(1)==PROPERTY)}? COMMA? valueItem)*
      (WHEN SET TO FALSE_ IS? literal)?
      (IN IDENTIFIER)?
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

initializeReplacingItem
    : initializeCategory DATA? BY (dataReference | literal)
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
