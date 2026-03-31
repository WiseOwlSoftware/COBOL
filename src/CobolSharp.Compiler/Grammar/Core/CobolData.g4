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
    | linageClause
    | genericFileDescriptionClause
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
    : dataDescriptionClauses
    | renamesClause
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
    | externalClause
    | globalClause
    | typeClause
    | genericDataClause
    ;

// EXTERNAL clause (§13.18.22) — shared storage across run unit
externalClause
    : IS? EXTERNAL
    ;

// GLOBAL clause (§13.18.27) — visible to contained programs
globalClause
    : IS? GLOBAL
    ;

// TYPE clause (COBOL-2023 — threaded from CobolParserGenerics)
typeClause
    : {is2023()}? TYPE IS? IDENTIFIER
    ;

genericDataClause
    : genericClause
    ;

// PIC Clause — PIC/PICTURE triggers PICMODE in the lexer,
// which emits a single PIC_STRING token. IS is consumed by PICMODE.
pictureClause
    : PIC PIC_STRING
    ;

// USAGE Clause
usageClause
    : USAGE IS? usageKeyword        // full form: USAGE IS DISPLAY
    | DISPLAY                        // bare keyword forms (no USAGE prefix)
    | COMPUTATIONAL                  // per ISO §13.16 — USAGE keyword is optional
    | COMPUTATIONAL_1
    | COMPUTATIONAL_2
    | COMPUTATIONAL_3
    | COMPUTATIONAL_5
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | COMP_5
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
    | COMPUTATIONAL_5
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | COMP_5
    | BINARY
    | PACKED_DECIMAL
    | INDEX
    ;

// OCCURS Clause
occursClause
    : OCCURS integerLiteral (TO integerLiteral)? timesKeyword?
      (DEPENDING ON? dataReference)?
      occursKeyClause*
      (INDEXED BY? dataReferenceList)?
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
valueClause
    : (VALUE | VALUES) (IS | ARE)? valueItem (COMMA? valueItem)*
      (WHEN SET TO FALSE_ IS? literal)?
      (IN IDENTIFIER)?
    ;

valueItem
    : valueClauseRange
    | valueClauseOperand+
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

// THEN TO DEFAULT (§14.9.20) — DEFAULT is not a reserved word; matches as IDENTIFIER
initializeDefaultPhrase
    : THEN? TO IDENTIFIER   /* semantic check: IDENTIFIER must be "DEFAULT" */
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
