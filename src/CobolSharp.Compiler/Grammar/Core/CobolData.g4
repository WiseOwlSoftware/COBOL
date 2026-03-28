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

sortMergeDescriptionEntry
    : SD fileName fileDescriptionClauses? DOT dataDescriptionEntry*
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
    | labelRecordsClause
    | dataRecordsClause
    | linageClause
    | genericFileDescriptionClause
    ;

// LABEL RECORD(S) IS/ARE — obsolete COBOL-85 FD clause, semantically inert
labelRecordsClause
    : LABEL (RECORD | RECORDS) IS? (STANDARD | OMITTED | IDENTIFIER+)
    ;

// DATA RECORD(S) IS/ARE — obsolete COBOL-74 FD clause, semantically inert
dataRecordsClause
    : DATA RECORD IS? IDENTIFIER+
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
    : IDENTIFIER
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
valueClause
    : (VALUE | VALUES) (IS | ARE)? valueItem (COMMA? valueItem)*
    ;

valueItem
    : valueRange
    | valueOperand+
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
    : INITIALIZE dataReferenceList initializeReplacingPhrase?
    ;

initializeReplacingPhrase
    : REPLACING initializeReplacingItem+
    ;

initializeReplacingItem
    : ALPHABETIC DATA? BY (dataReference | literal)
    | ALPHANUMERIC DATA? BY (dataReference | literal)
    | NUMERIC DATA? BY (dataReference | literal)
    | (ALPHANUMERIC EDITED | ALPHANUMERIC_EDITED) DATA? BY (dataReference | literal)
    | (NUMERIC EDITED | NUMERIC_EDITED) DATA? BY (dataReference | literal)
    ;
