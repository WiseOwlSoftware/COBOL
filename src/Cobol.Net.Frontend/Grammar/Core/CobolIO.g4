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
    // COBOL-2002 file sharing / record locking (ISO §12.4.5.15 / §12.4.5.9) — unique leading tokens
    // (SHARING / LOCK), {is2002()}?-gated, ADDITIVE (the DEVLOG 621/622 lesson).
    | sharingClause   // COBOL-2002; parses at all editions (superset), introduction-gated post-bind by VersionConformancePass ParseArm.VisitSharingClause (rearch 14g.4)
    | lockModeClause   // LOCK MODE (LOCK/MODE hard-reserved) introduction-gated post-bind by VersionConformancePass ParseArm.VisitLockModeClause (rearch 14g.4)
    | fileCollatingSequenceClause   // §12.4.5.7 INDEXED per-key collating; parses at all editions (superset), introduction-gated post-bind by VersionConformancePass
    | vendorFileControlClause
    ;

// ISO §12.4.5.7 COLLATING SEQUENCE clause — the collating sequence for the record keys of an INDEXED file.
// Format 1 (file-level): reuses the shared `collatingForPhrase` (FOR ALPHANUMERIC / FOR NATIONAL — §5.2.6.4
// choice indicators, one-or-more any-order) and the `IS alphabet-name-1 [alphabet-name-2]` form. Format 2
// (key-level, OF-led): names specific RECORD KEY / ALTERNATE RECORD KEY items and their alphabet-name-3. Parses
// at all editions (superset); the post-85 FOR-split / national / key-level legs are introduction-gated at bind,
// and SR1-8 (alphabet class, single file-level clause, key existence, no subscript, single clause per key) are
// enforced by the binder — the grammar is a permissive shape (the sharing/lock precedent). `OF` disambiguates
// Format 2 from Format 1; list it first so the parser commits on the OF token.
fileCollatingSequenceClause
    : COLLATING? SEQUENCE
      ( OF cobolWord+ IS? cobolWord                 // Format 2: OF {data-name-1 | record-key-name-1}… IS alphabet-name-3
      | collatingForPhrase+                         // Format 1: {FOR ALPHANUMERIC | FOR NATIONAL} IS alphabet-name …
      | IS? cobolWord cobolWord?                    // Format 1: IS alphabet-name-1 [alphabet-name-2]
      )
    ;

// COBOL-2002 SHARING clause (ISO §12.4.5.15): the file-connector sharing mode.
sharingClause
    : SHARING WITH? sharingMode
    ;
sharingMode
    : ALL OTHER?
    | NO OTHER?
    | READ ONLY
    ;

// COBOL-2002 LOCK MODE clause (ISO §12.4.5.9): MANUAL / AUTOMATIC (no EXCLUSIVE), with an optional
// single/multiple record-lock granularity.
lockModeClause
    : LOCK MODE IS? (MANUAL | AUTOMATIC) lockOnPhrase?
    ;
lockOnPhrase
    : WITH? LOCK ON? MULTIPLE? (RECORD | RECORDS)
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
// (⚠ KEY is NOT required — see below), and the CCVS suite writes `RECORD data-name` without KEY (e.g.
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
      alternateKeySuppressWhen?
    ;

// ISO §12.4.5.6.2 — SUPPRESS WHEN literal-1: alternate-key suppression (a COBOL-2023 addition). literal-1 is the
// key suppression value: a record's alternate access path is withheld when the key equals literal-1 (§12.4.5.6.4
// GR6). Fixed order — DUPLICATES precedes SUPPRESS. Parses at all editions (superset); introduction-gated at 2023
// by VersionConformancePass ParseArm.VisitAlternateKeySuppressWhen (recognition-fire on this dedicated rule).
alternateKeySuppressWhen
    : SUPPRESS WHEN literal
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
    : openMode (sharingPhrase)? ({is2002() || retryPhraseAhead()}? retryPhrase)? openFileSpec+   // both phrases superset-parsed, gated at BIND (BindOpen → Check(FileSharingClause2002) / GateRetryIntro → Check(RetryPhrase2002)). SHARING is unambiguous; RETRY sits in the openFileSpec+ name-list, so below 2002 (where RETRY is a legal §8.9 user word) the forward-detect retryPhraseAhead() only enters the phrase on an UNAMBIGUOUS numeric tail (n TIMES | FOR? n SECONDS — an integer can never be a file name) with a file name still to follow; RETRY FOREVER / bare RETRY stay file names (fail-safe).
    ;

// COBOL-2002 OPEN SHARING phrase (ISO §14.9.27) — overrides the file-control SHARING clause for this OPEN.
sharingPhrase
    : SHARING WITH? sharingMode
    ;

// COBOL-2002 RETRY phrase (ISO §14.7.9) on OPEN / READ / WRITE / REWRITE / DELETE — how to react to a lock.
retryPhrase
    : RETRY (arithmeticExpression TIMES | FOR? arithmeticExpression SECONDS | FOREVER)
    ;

// COBOL-2002 record-lock phrase on READ / WRITE / REWRITE (ISO §14.9.30 etc.). Order: WITH? NO LOCK before
// WITH? LOCK so `WITH NO LOCK` is not shadowed by `WITH LOCK`.
recordLockPhrase
    : IGNORING LOCK
    | WITH? NO LOCK
    | WITH? LOCK
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
      (readAdvancingOnLock)?    // ADVANCING ON LOCK (ISO §14.9.30 fmt1) — introduction-gated at BIND time (KeyedBindRead → Check(RecordLockPhrase2002))
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      (recordLockPhrase)?   // introduction-gated at BIND time (CheckRecordLockPhrase → Check(RecordLockPhrase2002))
      readAtEnd?
      readInvalidKey?
      END_READ?

    ;

// COBOL-2002 READ … ADVANCING ON LOCK (ISO §14.9.30 GR22): skip-scan locked records on NEXT/PREVIOUS.
readAdvancingOnLock
    : ADVANCING ON LOCK
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
// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
readAtEnd
    : AT? END statementBlock
      (NOT AT? END statementBlock)?
    | NOT AT? END statementBlock
      (AT? END statementBlock)?
    ;

// ⚠ NOT A LENIENCY — THIS IS CONFORMANCE. Recorded as "Leniency L1" on the reasoning that KEY is
// "unbracketed → required". That criterion is wrong: ISO 5.2.2/5.2.3 make UNDERLINING the test for a required
// word, not bracketing. Bracketing marks whether a whole PHRASE may be omitted; underlining marks whether a
// WORD within it must be written. Measured on the printed pages, INVALID carries an underline rule and KEY
// carries none in ALL FIVE statements that have the phrase — DELETE (p635), READ (p722), REWRITE (p740),
// START (p784) and WRITE (p816) — so `INVALID <imperative>` is conforming ISO. The same mistake was made for
// the RECORD KEY clause above (p359: RECORD and SOURCE underlined, KEY and IS not) and for COLLATING in
// SORT/MERGE (p687, p776). The legacy compiler still DIAGNOSES these under strict/warn modes, i.e. it reports
// conforming source; that is filed as SR2 in CONFORMANCE-FIX-QUEUE.md. The grammar accepting them is correct.
// Retained history: the CCVS suite
// and 1980s/90s compilers tolerate "INVALID <imperative>" without it. The dropped 'KEY' is accepted
// in DialectMode.Default and diagnosed under named-strict modes by
// DialectStrictnessChecks.CheckInvalidKeyNoiseWord (called from FileIoBinder). 'INVALID' is a reserved
// word, so this relaxation is unambiguous. Applies to all five INVALID KEY phrases below.
// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
readInvalidKey
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// WRITE (§14.9.46 — full expansion)
// ==========================================

writeStatement
    : WRITE (recordName | FILE fileName)
      writeFrom?
      writeBeforeAfter?
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      (recordLockPhrase)?   // introduction-gated at BIND time (CheckRecordLockPhrase → Check(RecordLockPhrase2002))
      writeAtEndOfPage?
      writeInvalidKey?
      END_WRITE?

    ;

// ⛔ ADDITIVE, not a rewrite to `moveSendingOperand` — and the reason is worth recording. §14.9.51.4 GR5a makes this phrase equivalent to `MOVE identifier-1 TO record-name-1`,
// so a function-identifier belongs here: §8.4.3.1.2 Format 1 makes one an IDENTIFIER and §8.4.3.2.3 SR1
// bars it only from RECEIVING operands, so `FROM FUNCTION UPPER-CASE(X)` was legal source we rejected
// with COBOL0001 (fix-queue PB10). Replacing the alternatives with a single shared rule is the tidier
// shape and was tried FIRST; it deletes the generated `.dataReference()`/`.literal()` accessors and so
// breaks ~8 call sites across BOTH compilers — this grammar is shared with the legacy
// `CobolSharp.Compiler`, which survives until the P15 cut-over. Adding an alternative keeps every
// existing accessor, so the legacy binders compile untouched. The unification belongs to P15, when the
// legacy side is deleted rather than migrated.
writeFrom
    : FROM (functionCall | dataReference | literal)
    ;

// ISO §14.9.51: one or (COBOL-2023, SR17) BOTH of BEFORE/AFTER ADVANCING. The combined form is introduction-gated
// at 2023 and rejects PAGE (SR17); a single phrase is edition-invariant (85+).
writeBeforeAfter
    : writeAdvancePhrase writeAdvancePhrase?
    ;

writeAdvancePhrase
    : (BEFORE | AFTER) ADVANCING?
      ( PAGE
      | (dataReference | integerLiteral | literal) (LINE | LINES)?
      )
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
writeAtEndOfPage
    : AT? (END_OF_PAGE | EOP) statementBlock
      (NOT AT? (END_OF_PAGE | EOP) statementBlock)?
    | NOT AT? (END_OF_PAGE | EOP) statementBlock
      (AT? (END_OF_PAGE | EOP) statementBlock)?
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
writeInvalidKey
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
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
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      (recordLockPhrase)?   // introduction-gated at BIND time (CheckRecordLockPhrase → Check(RecordLockPhrase2002))
      rewriteInvalidKeyPhrase?
      END_REWRITE?

    ;

// ⛔ ADDITIVE, not a rewrite to `moveSendingOperand` — and the reason is worth recording. §14.9.35.4 makes this phrase the same MOVE,
// so a function-identifier belongs here: §8.4.3.1.2 Format 1 makes one an IDENTIFIER and §8.4.3.2.3 SR1
// bars it only from RECEIVING operands, so `FROM FUNCTION UPPER-CASE(X)` was legal source we rejected
// with COBOL0001 (fix-queue PB10). Replacing the alternatives with a single shared rule is the tidier
// shape and was tried FIRST; it deletes the generated `.dataReference()`/`.literal()` accessors and so
// breaks ~8 call sites across BOTH compilers — this grammar is shared with the legacy
// `CobolSharp.Compiler`, which survives until the P15 cut-over. Adding an alternative keeps every
// existing accessor, so the legacy binders compile untouched. The unification belongs to P15, when the
// legacy side is deleted rather than migrated.
rewriteFrom
    : FROM (functionCall | dataReference | literal)
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
rewriteInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// DELETE RECORD (§14.9.11)
// ==========================================

deleteStatement
    : DELETE fileName RECORD?
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      deleteInvalidKeyPhrase?
      END_DELETE?

    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
deleteInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
    ;

// ==========================================
// DELETE FILE (§14.9.10 — COBOL 2023)
// ==========================================

deleteFileStatement
    : DELETE FILE OVERRIDE? fileName
      // §14.9.10.2 Format 2's {file-name-1}… repetition (kb/Work PB134; GR12 — as-if one statement per
      // name). The loop continuation is PREDICATED on the lookahead not being a phrase keyword: RETRY /
      // ON / NOT / EXCEPTION / END-DELETE all lex as word tokens the edition-shared cobolWord can match
      // (reservation is a per-edition BIND screen), so a greedy fileName+ swallowed `RETRY …` as a
      // second file-name — the gate's delete_file_sharing red. Left-edge predicate per the standing rule.
      ({TokenStream.LA(1) != RETRY && TokenStream.LA(1) != ON && TokenStream.LA(1) != NOT && TokenStream.LA(1) != EXCEPTION && TokenStream.LA(1) != END_DELETE}? fileName)*
      (retryPhrase)?   // COBOL-2002 (§14.7.9); superset-parsed, introduction-gated at BIND (GateRetryIntro → Check(RetryPhrase2002)) — residue migration #4. The file is already named before RETRY here, so no name-list ambiguity (unlike OPEN).
      deleteFileOnException?
      END_DELETE?

    ;

// COBOL-2002 UNLOCK statement (ISO §14.9.47): release all this-connector record locks on the file.
unlockStatement
    : UNLOCK fileName (RECORD | RECORDS)?
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
deleteFileOnException
    : ON? EXCEPTION statementBlock
      (NOT ON? EXCEPTION statementBlock)?   // ON is not underlined (§5.2.3 optional word; kb/Work PB134)
    | NOT ON EXCEPTION statementBlock
      (ON EXCEPTION statementBlock)?
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
    : KEY IS? comparisonOperator? dataReference (startWithLength)?   // WITH LENGTH introduction-gated at BIND time (StatementBinder.KeyedIo → Check(StartWithLength2002))
    ;

startWithLength
    : WITH LENGTH arithmeticExpression
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
startInvalidKeyPhrase
    : INVALID KEY? statementBlock
      (NOT INVALID KEY? statementBlock)?
    | NOT INVALID KEY? statementBlock
      (INVALID KEY? statementBlock)?
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
    // COLLATING SEQUENCE {IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS alphabet-name-1 |
    // FOR NATIONAL IS alphabet-name-2}…} (ISO §14.9.40.2 / §14.9.24.2). alphabet-name-2 + the FOR forms
    // are the 2002 national class — gated on recognition (VisitSortCollatingPhrase).
    : COLLATING? SEQUENCE (collatingForPhrase+ | IS? cobolWord (cobolWord)?)
    ;

// The ONE FOR-class collating subrule (ISO §12.3.6.2 / §14.9.40.2 — the PROGRAM COLLATING SEQUENCE clause
// and the SORT/MERGE COLLATING SEQUENCE phrase share it).
collatingForPhrase
    : FOR (ALPHANUMERIC | NATIONAL) IS? cobolWord
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

// ⛔ ADDITIVE, not a rewrite to `moveSendingOperand` — and the reason is worth recording. §14.9.32.4 makes this phrase the same MOVE,
// so a function-identifier belongs here: §8.4.3.1.2 Format 1 makes one an IDENTIFIER and §8.4.3.2.3 SR1
// bars it only from RECEIVING operands, so `FROM FUNCTION UPPER-CASE(X)` was legal source we rejected
// with COBOL0001 (fix-queue PB10). Replacing the alternatives with a single shared rule is the tidier
// shape and was tried FIRST; it deletes the generated `.dataReference()`/`.literal()` accessors and so
// breaks ~8 call sites across BOTH compilers — this grammar is shared with the legacy
// `CobolSharp.Compiler`, which survives until the P15 cut-over. Adding an alternative keeps every
// existing accessor, so the legacy binders compile untouched. The unification belongs to P15, when the
// legacy side is deleted rather than migrated.
releaseFrom
    : FROM (functionCall | dataReference | literal)
    ;

// ==========================================
// STRING (§14.9.41)
// ==========================================

stringStatement
    : STRING stringSendingPhrase+ stringIntoPhrase stringWithPointer? stringOnOverflow? END_STRING?
    ;

// ── The two STRING/UNSTRING SENDING-operand shapes, named ONCE each ──────────────────────────────────────
// The general formats distinguish them, so the grammar does too rather than repeating an alternative list in
// four productions:
//   §14.9.48.2  `UNSTRING identifier-1`               -> an IDENTIFIER only; a literal sender is not admitted.
//   §14.9.43.2  `STRING {identifier-1 | literal-1}`   -> an identifier OR a literal.
// The sender is therefore a strict SUBSET of the operand, and strUnstrOperand says so by referencing it.
//
// DA4: functionCall is FIRST in the sender — §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so
// every "identifier-N" SENDING position admits one. It is keyword-led (FUNCTION …), so it can never be shadowed
// by dataReference (ANTLR takes the first matching alternative). §8.4.3.2.3 SR1 bars a function-identifier only
// from a RECEIVING operand, which is why the INTO phrases below are untouched. A keyword-OMITTED function (a
// repository name + parens) still parses as a dataReference and is resolved by the binder's
// KeywordOmittedFunction path — unchanged.
strUnstrSender
    : functionCall | dataReference
    ;

strUnstrOperand
    : strUnstrSender | literal | figurativeConstant
    ;

stringSendingPhrase
    : strUnstrOperand delimitedByPhrase?
    ;

delimitedByPhrase
    : DELIMITED BY? (ALL)? (strUnstrOperand | SIZE)
    ;

stringIntoPhrase
    : INTO dataReference
    ;

stringWithPointer
    : WITH? POINTER dataReference
    ;

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
stringOnOverflow
    : ON? OVERFLOW statementBlock (NOT ON? OVERFLOW statementBlock)?
    | NOT ON? OVERFLOW statementBlock (ON? OVERFLOW statementBlock)?
    ;

// ==========================================
// UNSTRING (§14.9.44)
// ==========================================

unstringStatement
    : UNSTRING strUnstrSender
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
    : (ALL)? strUnstrOperand
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

// ISO 5.2.6.4: the positive and negative phrases are enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER.
// The reversed order was rejected until 2026-07-19 (the transcription had dropped the bars); the shape
// below matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
unstringOnOverflow
    : ON? OVERFLOW statementBlock (NOT ON? OVERFLOW statementBlock)?
    | NOT ON? OVERFLOW statementBlock (ON? OVERFLOW statementBlock)?
    ;

// ==========================================
// INSPECT (§14.9.21 — COBOL-85)
// ==========================================

// identifier-1 admits a FUNCTION-IDENTIFIER, but only in Format 1 (PB10). §8.4.3.1.2 Format 1 makes a
// function-identifier an IDENTIFIER, so every identifier-N position admits one unless a syntax rule excludes it —
// and §8.4.3.2.3 SR1 excludes it from a RECEIVING operand. INSPECT is FORMAT-DEPENDENT here, which is why the
// grammar cannot decide it alone: identifier-1 is SENDING only in Format 1 (TALLYING). §14.9.22.4 GR1 concedes
// only that "for purposes of determining its length, identifier-1 is treated as a sending data item" — a scoped
// concession that would be unnecessary if it were generally sending — and GR7 has each match "tallied (format 1)
// or replaced by literal-3 (format 2)", while GR20 makes format 4 execute AS a format 2 over the same
// identifier-1. So Formats 2/3/4 MODIFY it and bar a function-identifier. ⛔ THE BINDER SCREENS PER FORMAT
// (InspectBinder, COBOLNET1632); widening the grammar alone would ACCEPT ILLEGAL SOURCE.
// ⛔ ADDITIVE — an ALTERNATIVE, never a rewrite to a shared rule: this grammar is shared with the legacy
// CobolSharp.Compiler until the P15 cut-over, and collapsing the rule would DELETE the generated
// .dataReference() accessor its binder reads. The legacy binder guards on the new null instead.
inspectStatement
    : INSPECT BACKWARD? (functionCall | dataReference)
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

// ⛔ A FUNCTION-IDENTIFIER IS ADMISSIBLE HERE (ISO §8.4.3.1.2 Format 1 makes it an identifier; fix-queue PB45).
// §14.9.22.2 writes every one of these operands as `identifier-n | literal-n`, and each use of inspectChar is a
// SENDING position — the TALLYING pattern, and BOTH sides of REPLACING (the item being inspected, identifier-1, is
// the only receiver and is a separate rule). So `INSPECT S TALLYING N FOR ALL FUNCTION TRIM(X)` is conforming
// source that was a PARSE error. Additive and unambiguous: functionCall begins with the FUNCTION token, so it
// cannot be confused with the dataReference alternative or with the {IsBareInspectOperand()}? bare form above.
inspectChar
    : literal
    | functionCall
    | dataReference
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
