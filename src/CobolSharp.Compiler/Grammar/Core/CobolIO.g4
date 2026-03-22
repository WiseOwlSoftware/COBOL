// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// File I/O statements and file control (INPUT-OUTPUT SECTION).
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolIO;

// ==========================================
// INPUT-OUTPUT SECTION
// ==========================================

inputOutputSection
    : IDENTIFIER SECTION DOT
      fileControlParagraph?
      ioControlParagraph?
    ;

// FILE-CONTROL.
fileControlParagraph
    : FILE_CONTROL DOT fileControlClauseGroup+
    ;

fileControlClauseGroup
    : SELECT fileName
      ( ASSIGN TO assignTarget )?
      fileControlClauses*
      DOT
    ;

assignTarget
    : IDENTIFIER
    | STRINGLIT
    ;

fileControlClauses
    : organizationClause
    | accessModeClause
    | recordKeyClause
    | alternateKeyClause
    | fileStatusClause
    | vendorFileControlClause
    ;

organizationClause
    : ORGANIZATION IS? organizationType
    ;

organizationType
    : LINE SEQUENTIAL
    | SEQUENTIAL
    | RELATIVE
    | INDEXED
    ;

accessModeClause
    : ACCESS MODE? IS? accessMode
    ;

accessMode
    : SEQUENTIAL
    | RANDOM
    | DYNAMIC
    ;

recordKeyClause
    : 'RECORD' KEY IS dataReference
    ;

alternateKeyClause
    : ALTERNATE KEY IS dataReference
      (WITH? DUPLICATES)?
    ;

fileStatusClause
    : FILE STATUS IS dataReference
    ;

vendorFileControlClause
    : genericClause
    ;

// I-O-CONTROL.
ioControlParagraph
    : I_O_CONTROL DOT ioControlEntry+
    ;

ioControlEntry
    : genericClause DOT
    ;

// ==========================================
// OPEN / CLOSE (§14.9.25, §14.9.7)
// ==========================================

openStatement
    : OPEN openClause+
    ;

openClause
    : openMode dataReference+
    ;

openMode
    : INPUT
    | OUTPUT
    | I_O
    | EXTEND
    ;

closeStatement
    : CLOSE dataReferenceList
    ;

// ==========================================
// READ (§14.9.30 — full expansion)
// ==========================================

readStatement
    : READ fileName
      readDirection?
      readInto?
      readKey?
      readAtEnd?
      readInvalidKey?
      END_READ?

    ;

readDirection
    : (NEXT | PREVIOUS) RECORD
    ;

readInto
    : INTO dataReference
    ;

readKey
    : KEY IS dataReference
    ;

readAtEnd
    : AT END statementBlock
      (NOT AT END statementBlock)?
    ;

readInvalidKey
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

// ==========================================
// WRITE (§14.9.46 — full expansion)
// ==========================================

writeStatement
    : WRITE recordName
      writeFrom?
      writeBeforeAfter?
      writeInvalidKey?
      END_WRITE?

    ;

writeFrom
    : FROM dataReference
    ;

writeBeforeAfter
    : ('BEFORE' | 'AFTER') ADVANCING (dataReference | integerLiteral | literal) (LINE | LINES)?
    ;

writeInvalidKey
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

recordName
    : dataReference
    ;

// ==========================================
// REWRITE (§14.9.36)
// ==========================================

rewriteStatement
    : REWRITE recordName
      (FROM dataReference)?
      rewriteInvalidKeyPhrase?
      END_REWRITE?

    ;

rewriteInvalidKeyPhrase
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

// ==========================================
// DELETE RECORD (§14.9.11)
// ==========================================

deleteStatement
    : DELETE fileName RECORD?
      deleteInvalidKeyPhrase?
      END_DELETE?

    ;

deleteInvalidKeyPhrase
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

// ==========================================
// DELETE FILE (§14.9.10 — COBOL 2023)
// ==========================================

deleteFileStatement
    : DELETE FILE fileName
      deleteFileOnException?
      END_DELETE?

    ;

deleteFileOnException
    : ON EXCEPTION statementBlock
      (NOT ON EXCEPTION statementBlock)?
    ;

// ==========================================
// START (§14.9.38)
// ==========================================

startStatement
    : START fileName
      startKeyPhrase?
      startInvalidKeyPhrase?
      END_START?

    ;

startKeyPhrase
    : KEY IS comparisonExpression
    ;

startInvalidKeyPhrase
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

// ==========================================
// SORT (§14.9.40)
// ==========================================

sortStatement
    : SORT sortFileName
      sortKeyPhrase*
      sortUsingPhrase?
      sortGivingPhrase?
      sortInputProcedurePhrase?
      sortOutputProcedurePhrase?
      END_SORT?

    ;

sortFileName
    : dataReference
    ;

sortKeyPhrase
    : (ASCENDING | DESCENDING) KEY dataReferenceList
    ;

sortUsingPhrase
    : USING dataReferenceList
    ;

sortGivingPhrase
    : GIVING dataReferenceList
    ;

sortInputProcedurePhrase
    : INPUT PROCEDURE IS procedureName
    ;

sortOutputProcedurePhrase
    : OUTPUT PROCEDURE IS procedureName
    ;

// ==========================================
// MERGE (§14.9.22)
// ==========================================

mergeStatement
    : MERGE mergeFileName
      mergeKeyPhrase+
      mergeUsingPhrase
      mergeOutputProcedurePhrase?
      mergeGivingPhrase?
      END_MERGE?

    ;

mergeFileName
    : dataReference
    ;

mergeKeyPhrase
    : (ASCENDING | DESCENDING) KEY dataReferenceList
    ;

mergeUsingPhrase
    : USING dataReferenceList
    ;

mergeGivingPhrase
    : GIVING dataReferenceList
    ;

mergeOutputProcedurePhrase
    : OUTPUT PROCEDURE IS procedureName
    ;

// ==========================================
// RETURN (§14.9.34)
// ==========================================

returnStatement
    : RETURN fileName RECORD
      (INTO dataReference)?
      returnAtEndPhrase?
      END_RETURN?

    ;

returnAtEndPhrase
    : AT END statementBlock
      (NOT AT END statementBlock)?
    ;

// ==========================================
// RELEASE (§14.9.33)
// ==========================================

releaseStatement
    : RELEASE dataReference
      (FROM dataReference)?

    ;

// ==========================================
// STRING (§14.9.41)
// ==========================================

stringStatement
    : STRING stringSendingPhrase+ stringIntoPhrase stringWithPointer? stringOnOverflow? END_STRING?
    ;

stringSendingPhrase
    : (dataReference | literal | figurativeConstant)
      delimitedByPhrase?
    ;

delimitedByPhrase
    : DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant | SIZE)
    ;

stringIntoPhrase
    : INTO dataReference
    ;

stringWithPointer
    : WITH POINTER dataReference
    ;

stringOnOverflow
    : ON OVERFLOW statementBlock
      (NOT ON OVERFLOW statementBlock)?
    ;

// ==========================================
// UNSTRING (§14.9.44)
// ==========================================

unstringStatement
    : UNSTRING dataReference
      unstringDelimiterPhrase?
      unstringIntoPhrase+
      unstringWithPointer?
      unstringTallying?
      unstringOnOverflow?
      END_UNSTRING?

    ;

unstringDelimiterPhrase
    : DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant)
    ;

unstringIntoPhrase
    : INTO dataReference
      (DELIMITER IN dataReference)?
      (COUNT IN dataReference)?
    ;

unstringWithPointer
    : WITH POINTER dataReference
    ;

unstringTallying
    : TALLYING IN dataReference
    ;

unstringOnOverflow
    : ON OVERFLOW statementBlock
      (NOT ON OVERFLOW statementBlock)?
    ;

// ==========================================
// INSPECT (§14.9.21 — COBOL-85)
// ==========================================

inspectStatement
    : INSPECT dataReference
      ( inspectTallyingPhrase inspectReplacingPhrase?
      | inspectReplacingPhrase
      | inspectConvertingPhrase )
    ;

// ----- TALLYING -----

inspectTallyingPhrase
    : TALLYING inspectTallyingItem+
    ;

inspectTallyingItem
    : dataReference inspectForClause+
    ;

inspectForClause
    : FOR inspectCountPhrase
    ;

inspectCountPhrase
    : CHARACTERS inspectDelimiters?
    | ALL inspectChar inspectDelimiters?
    | LEADING inspectChar inspectDelimiters?
    | FIRST inspectChar inspectDelimiters?
    | TRAILING inspectChar inspectDelimiters?
    ;

inspectChar
    : dataReference
    | literal
    | figurativeConstant
    ;

// ----- REPLACING -----

inspectReplacingPhrase
    : REPLACING inspectReplacingItem+
    ;

inspectReplacingItem
    : CHARACTERS BY inspectChar inspectDelimiters?
    | ALL inspectChar BY inspectChar inspectDelimiters?
    | LEADING inspectChar BY inspectChar inspectDelimiters?
    | FIRST inspectChar BY inspectChar inspectDelimiters?
    | TRAILING inspectChar BY inspectChar inspectDelimiters?
    ;

// ----- CONVERTING -----

inspectConvertingPhrase
    : CONVERTING inspectChar
      TO inspectChar
      inspectBeforeAfterPhrase*
    ;

inspectBeforeAfterPhrase
    : BEFORE INITIAL_? inspectChar
    | AFTER INITIAL_? inspectChar
    ;

inspectDelimiters
    : BEFORE INITIAL_? inspectChar (AFTER INITIAL_? inspectChar)?
    | AFTER INITIAL_? inspectChar (BEFORE INITIAL_? inspectChar)?
    ;
