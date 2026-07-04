// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// File I/O statements and file control (INPUT-OUTPUT SECTION).
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolIO;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// INPUT-OUTPUT SECTION
// ==========================================

inputOutputSection
    : INPUT_OUTPUT SECTION DOT
      fileControlParagraph?
      ioControlParagraph?
    ;

// FILE-CONTROL.
fileControlParagraph
    : FILE_CONTROL DOT fileControlClauseGroup*
    ;

// ISO §12.4.5.2 SR1: SELECT comes first; all following clauses (including ASSIGN) may appear in
// ANY ORDER. So ASSIGN is just one of the order-free fileControlClauses, not a fixed-position slot.
fileControlClauseGroup
    : SELECT OPTIONAL? fileName
      fileControlClauses*
      DOT
    ;

assignClause
    : ASSIGN TO? assignTarget (USING dataReference)?
    | ASSIGN USING dataReference
    ;

assignTarget
    : cobolWord
    | STRINGLIT
    ;

fileControlClauses
    : assignClause
    // relativeKeyClause precedes organizationClause: both can begin with RELATIVE (organizationType
    // has a bare RELATIVE), and under leniency L2 `RELATIVE data-name` (no KEY) is the key clause while
    // a lone RELATIVE is the organization. Trying the key clause first means `RELATIVE <data-name>`
    // binds the key; a bare RELATIVE (no following data-name) fails the key clause and falls through to
    // organizationClause. `ORGANIZATION RELATIVE` is unaffected (it begins with ORGANIZATION).
    | relativeKeyClause
    | organizationClause
    | accessModeClause
    // recordKeyClause and recordDelimiterClause both begin with RECORD. With KEY now optional in
    // recordKeyClause (leniency L3), disambiguation rests on the operand: DELIMITER is a reserved token
    // so `RECORD DELIMITER …` cannot satisfy recordKeyClause's dataReference and falls through to
    // recordDelimiterClause. List recordKeyClause first; the parser back-tracks to the delimiter form.
    | recordKeyClause
    | recordDelimiterClause
    | alternateKeyClause
    | fileStatusClause
    | fileReserveClause
    | paddingCharacterClause
    | vendorFileControlClause
    ;

fileReserveClause
    : RESERVE integerLiteral (AREA | AREAS)?
    ;

// ISO §12.4.5.9 PADDING CHARACTER clause: PADDING [CHARACTER] IS {data-name-1 | literal-1}.
// An obsolete block-padding control with no effect on CobolSharp's record model — parsed and ignored.
paddingCharacterClause
    : PADDING CHARACTER? IS? (literal | dataReference)
    ;

// ISO §12.4.5.11 RECORD DELIMITER clause: RECORD DELIMITER IS {STANDARD-1 | feature-name-1}. Specifies
// the method of determining the length of a variable-length record; CobolSharp length-frames variable
// records itself (4-byte prefix), so this is parsed and ignored.
recordDelimiterClause
    : RECORD DELIMITER IS? (STANDARD_1 | cobolWord)
    ;

// ISO §12.4.5.10: the leading "ORGANIZATION IS" is optional — a bare organization type
// (e.g. a lone SEQUENTIAL) is a valid ORGANIZATION clause.
organizationClause
    : (ORGANIZATION IS?)? organizationType
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

// IS is an optional word in the RECORD KEY / ALTERNATE RECORD KEY clauses (ISO §12.4.5 — the CCVS
// suite writes "RECORD KEY data-name" without IS).
//
// Leniency L3 (see docs/dialect-strictness.md): ISO §12.4.5.12 requires `RECORD KEY IS data-name`
// (KEY is unbracketed → required), but the CCVS suite writes `RECORD data-name` without KEY (e.g.
// IX103A `RECORD IX-FS1-KEY`). The grammar parses the permissive superset `RECORD KEY? IS?
// dataReference`; the no-KEY form is accepted in DialectMode.Default and diagnosed under named-strict
// modes by the inline check in SemanticBuilder.VisitFileControlClauseGroup (CBL3615/3616). Disambiguation
// from recordDelimiterClause still holds: DELIMITER is a reserved token, so `RECORD DELIMITER …` cannot
// match dataReference and falls through to recordDelimiterClause (likewise RECORD CONTAINS/VARYING in an FD).
recordKeyClause
    : RECORD KEY? IS? dataReference
    ;

alternateKeyClause
    : ALTERNATE RECORD? KEY? IS? dataReference
      (WITH? DUPLICATES)?
    ;

// ISO §12.4.5.8.2: only STATUS is a required keyword (FILE STATUS IS data-name-1, STATUS underlined);
// FILE and IS are optional, so a bare "STATUS data-name" is a valid FILE STATUS clause.
fileStatusClause
    : FILE? STATUS IS? dataReference
    ;

// Leniency L2 (see docs/dialect-strictness.md): ISO §12.4.5.13 requires `RELATIVE KEY IS data-name`,
// but the CCVS suite writes `RELATIVE data-name` without KEY (e.g. RL109A `RELATIVE RL-FR1-KEY`). The
// grammar parses the permissive superset `RELATIVE KEY? IS? dataReference`; the no-KEY form is accepted
// in DialectMode.Default and diagnosed under named-strict modes by
// DialectStrictnessChecks.CheckRelativeKeyNoiseWord (called from SemanticBuilder). The dataReference is
// captured as the relative key either way, so random/dynamic WRITE/REWRITE/DELETE position correctly.
relativeKeyClause
    : RELATIVE KEY? IS? dataReference
    ;

vendorFileControlClause
    : genericClause
    ;

// I-O-CONTROL. The paragraph holds one or more clauses terminated by a period; per ISO §12.4.6 the
// clauses are not individually period-terminated, but compilers commonly tolerate a period after each,
// so accept an optional period after every clause (SQ206A writes two SAME clauses before one period).
ioControlParagraph
    : I_O_CONTROL DOT (ioControlClause DOT?)*
    ;

ioControlClause
    : sameClause
    | multipleFileClause
    | rerunClause
    | genericClause
    ;

// ISO §12.4.6.4 SAME clause. In every format only SAME (and RECORD/SORT/SORT-MERGE) is a required word;
// AREA and FOR are optional words (Format 1 `SAME AREA FOR file-1 …` underlines only SAME), so the
// "SAME AREA" clause may be written `SAME file-1 file-2` (as SQ206A does). Files may be comma-separated.
sameClause
    : SAME (RECORD | SORT | SORT_MERGE)? AREA? FOR? fileName (COMMA? fileName)*
    ;

// MULTIPLE FILE TAPE clause (obsolete; removed from later standards). Describes several files sharing
// one physical reel — irrelevant to disk storage, so parsed and ignored.
multipleFileClause
    : MULTIPLE FILE TAPE? (CONTAINS? multipleFileTapeEntry (COMMA? multipleFileTapeEntry)*)?
    ;

multipleFileTapeEntry
    : fileName (POSITION integerLiteral)?
    ;

// X3.23-1985 RERUN clause (I-O-CONTROL) — a checkpoint hint stating WHEN rerun records are written; the
// rerun mechanism itself is implementor-defined, so a null rerun facility accepts and ignores it (parsed
// and ignored, the MULTIPLE FILE posture). Obsolete '85 element DELETED by ISO 2002 (RERUN is absent from
// the 2023 text entirely, §8.9 included); the EditionValidator flags it COBOLNET0902 ≥2002
// (`rerun-removed-2002`, VCR Table 7 row 7.15).
//   RERUN [ON {file-name-1 | implementor-name-1}]
//         EVERY { [END OF] {REEL|UNIT} OF file-name-2
//               | integer-1 RECORDS [OF file-name-2]
//               | integer-2 CLOCK-UNITS
//               | condition-name-1 }
// The ON operand and condition-name-1 (a switch-status condition) are both plain words → cobolWord.
rerunClause
    : RERUN (ON cobolWord)? EVERY rerunEvery
    ;

rerunEvery
    : (END OF)? (REEL | UNIT) OF fileName
    | integerLiteral (RECORDS (OF fileName)? | CLOCK_UNITS)
    | cobolWord
    ;

// ==========================================
// OPEN / CLOSE (§14.9.25, §14.9.7)
// ==========================================

openStatement
    : OPEN openClause+
    ;

openClause
    : openMode openFileSpec+
    ;

// Each opened file may carry an obsolete tape phrase (ISO §14.9.25): REVERSED or WITH NO REWIND —
// vertical/tape positioning hints with no effect on disk files, parsed and ignored.
openFileSpec
    : dataReference (REVERSED | WITH? NO REWIND)?
    ;

openMode
    : INPUT
    | OUTPUT
    | I_O
    | EXTEND
    ;

closeStatement
    : CLOSE closeFilePhrase (closeFilePhrase)*
    ;

closeFilePhrase
    : fileName closeOption?
    ;

closeOption
    : (REEL | UNIT) (FOR? REMOVAL)?
    | WITH? NO REWIND
    | WITH? LOCK
    ;

// ==========================================
// READ (§14.9.30 — full expansion)
// ==========================================

readStatement
    : READ (fileName | FILE fileName)
      readDirection?
      RECORD?
      readInto?
      readKey?
      readAtEnd?
      readInvalidKey?
      END_READ?

    ;

readDirection
    : (NEXT | PREVIOUS) RECORD?
    ;

readInto
    : INTO dataReference
    ;

readKey
    : KEY IS? dataReference
    ;

// AT is an optional word in the at-end phrase (ISO §6 optional-word rule; the CCVS suite and
// mainstream compilers accept "READ … RECORD END …" without AT).
readAtEnd
    : AT? END statementBlock
      (NOT AT? END statementBlock)?
    ;

// Leniency L1 (see docs/dialect-strictness.md): the grammar parses the permissive superset
// "INVALID KEY?" — 'KEY' is required by the ISO statement formats (unbracketed), but the CCVS suite
// and 1980s/90s compilers tolerate "INVALID <imperative>" without it. The dropped 'KEY' is accepted
// in DialectMode.Default and diagnosed under named-strict modes by
// DialectStrictnessChecks.CheckInvalidKeyNoiseWord (called from FileIoBinder). 'INVALID' is a reserved
// word, so this relaxation is unambiguous. Applies to all five INVALID KEY phrases below.
readInvalidKey
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
    ;

// ==========================================
// WRITE (§14.9.46 — full expansion)
// ==========================================

writeStatement
    : WRITE (recordName | FILE fileName)
      writeFrom?
      writeBeforeAfter?
      writeAtEndOfPage?
      writeInvalidKey?
      END_WRITE?

    ;

writeFrom
    : FROM (dataReference | literal)
    ;

writeBeforeAfter
    : (BEFORE | AFTER) ADVANCING?
      ( PAGE
      | (dataReference | integerLiteral | literal) (LINE | LINES)?
      )
    ;

writeAtEndOfPage
    : AT? (END_OF_PAGE | EOP) statementBlock
      (NOT AT? (END_OF_PAGE | EOP) statementBlock)?
    ;

writeInvalidKey
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
    ;

recordName
    : dataReference
    ;

// ==========================================
// REWRITE (§14.9.36)
// ==========================================

rewriteStatement
    : REWRITE (recordName | FILE fileName)
      rewriteFrom?
      rewriteInvalidKeyPhrase?
      END_REWRITE?

    ;

rewriteFrom
    : FROM (dataReference | literal)
    ;

rewriteInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
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
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
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
// START (§14.9.41)
// ==========================================

startStatement
    : START fileName
      (FIRST | LAST | startKeyPhrase)?
      startInvalidKeyPhrase?
      END_START?

    ;

// ISO §14.9.41: START … KEY [IS] [relational-operator] {data-name | record-key-name}. The phrase is
// an optional relational operator + key data-name (the left operand — the key of reference — is
// implicit), NOT a full comparison. The operator may be omitted (then EQUAL is assumed), e.g.
// "START f KEY IS data-name". comparisonOperator absorbs its own leading IS, so a separate optional
// IS handles the no-operator form.
startKeyPhrase
    : KEY IS? comparisonOperator? dataReference ({is2002()}? startWithLength)?
    ;

startWithLength
    : WITH LENGTH arithmeticExpression
    ;

startInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
    ;

// ==========================================
// SORT (§14.9.40)
// ==========================================

// SORT Format 1 (file sort): requires USING/GIVING or INPUT/OUTPUT PROCEDURE
// SORT Format 2 (table sort, §14.9.40): no USING/GIVING — in-place sort
// Disambiguation deferred to semantic layer (file vs table target).
sortStatement
    : SORT sortFileName
      sortKeyPhrase+
      sortDuplicatesPhrase?
      sortCollatingPhrase?
      ( ( sortUsingPhrase | sortInputProcedurePhrase )
        ( sortGivingPhrase | sortOutputProcedurePhrase ) )?
      END_SORT?

    ;

sortFileName
    : dataReference
    ;

sortKeyPhrase
    : ON? (ASCENDING | DESCENDING) KEY? dataReferenceList?
    ;

sortDuplicatesPhrase
    : WITH? DUPLICATES IN? cobolWord?    // cobolWord matches ORDER (not a lexer token)
    ;

sortCollatingPhrase
    // COLLATING is required by the ISO SORT/MERGE format; the CCVS suite (ST139A) writes the phrase as
    // `SEQUENCE alphabet-name` with COLLATING omitted, so the keyword is OPTIONAL in the permissive
    // superset and the omission is flagged under strict modes (leniency L5, docs/dialect-strictness.md).
    : COLLATING? SEQUENCE IS? cobolWord (cobolWord)?
    ;

sortUsingPhrase
    : USING dataReferenceList
    ;

sortGivingPhrase
    : GIVING dataReferenceList
    ;

sortInputProcedurePhrase
    : INPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
    ;

sortOutputProcedurePhrase
    : OUTPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
    ;

// ==========================================
// MERGE (§14.9.22)
// ==========================================

mergeStatement
    : MERGE mergeFileName
      mergeKeyPhrase+
      sortCollatingPhrase?
      mergeUsingPhrase
      ( mergeGivingPhrase | mergeOutputProcedurePhrase )?
      END_MERGE?

    ;

mergeFileName
    : dataReference
    ;

mergeKeyPhrase
    : ON? (ASCENDING | DESCENDING) KEY? dataReferenceList
    ;

mergeUsingPhrase
    : USING dataReferenceList
    ;

mergeGivingPhrase
    : GIVING dataReferenceList
    ;

mergeOutputProcedurePhrase
    : OUTPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?
    ;

// ==========================================
// RETURN (§14.9.34)
// ==========================================

returnStatement
    : RETURN fileName RECORD?
      (INTO dataReference)?
      returnAtEndPhrase?
      END_RETURN?

    ;

// AT is optional in the AT END phrase (ISO §14.9.39 — "AT" is an optional reserved word),
// and RECORD above is optional, so "RETURN f END …" and "RETURN f RECORD AT END …" both parse.
// ISO §14.9.34.3 SR4: the AT END and NOT AT END phrases may be written in REVERSED order.
returnAtEndPhrase
    : AT? END statementBlock
      (NOT AT? END statementBlock)?
    | NOT AT? END statementBlock
      (AT? END statementBlock)?
    ;

// ==========================================
// RELEASE (§14.9.33)
// ==========================================

releaseStatement
    : RELEASE dataReference
      releaseFrom?

    ;

releaseFrom
    : FROM (dataReference | literal)
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
    : DELIMITED BY? (ALL)? (dataReference | literal | figurativeConstant | SIZE)
    ;

stringIntoPhrase
    : INTO dataReference
    ;

stringWithPointer
    : WITH? POINTER dataReference
    ;

stringOnOverflow
    : ON? OVERFLOW statementBlock (NOT ON? OVERFLOW statementBlock)?
    | NOT ON? OVERFLOW statementBlock
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
    : DELIMITED BY? unstringDelimiterItem (OR unstringDelimiterItem)*
    ;

unstringDelimiterItem
    : (ALL)? (dataReference | literal | figurativeConstant)
    ;

unstringIntoPhrase
    : INTO unstringIntoTarget+
    ;

unstringIntoTarget
    : dataReference
      (DELIMITER IN? dataReference)?
      (COUNT IN? dataReference)?
    ;

unstringWithPointer
    : WITH? POINTER dataReference
    ;

unstringTallying
    : TALLYING IN? dataReference
    ;

unstringOnOverflow
    : ON? OVERFLOW statementBlock (NOT ON? OVERFLOW statementBlock)?
    | NOT ON? OVERFLOW statementBlock
    ;

// ==========================================
// INSPECT (§14.9.21 — COBOL-85)
// ==========================================

inspectStatement
    : INSPECT BACKWARD? dataReference
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
    : FOR inspectCountPhrase+
    ;

// ALL/LEADING are transitive across the bare operands that follow them (GR 10), so a
// count phrase may omit the adjective: "FOR LEADING ""S"" ""S"" ""T""" lists three
// operands. That bare form is ambiguous with the next counter in multi-counter TALLYING
// ("c1 FOR ALL x  c2 FOR ALL y"): a greedy parser swallows c2 as a pattern of c1. The
// IsBareInspectOperand() predicate resolves it — a data-name immediately followed by FOR
// is the next counter, so the bare alternative declines it and the count-phrase loop ends.
inspectCountPhrase
    : CHARACTERS inspectDelimiters?
    | (ALL | LEADING | FIRST | TRAILING) inspectChar inspectDelimiters?
    | {IsBareInspectOperand()}? inspectChar inspectDelimiters?
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
    | (ALL | LEADING | FIRST | TRAILING)? inspectChar BY inspectChar inspectDelimiters?
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
