```
MISMATCH: INITIALIZE — WITH FILLER phrase missing
  Spec: INITIALIZE { identifier-1 } ... [ WITH FILLER ]
  Grammar: INITIALIZE dataReferenceList initializeReplacingPhrase? (no WITH FILLER)
  Gap: WITH FILLER entirely absent

MISMATCH: INITIALIZE — TO VALUE phrase missing
  Spec: [ { ALL | category-name } TO VALUE ]
  Grammar: no TO VALUE phrase
  Gap: The VALUE initialization form absent

MISMATCH: INITIALIZE — THEN REPLACING (THEN keyword) not present
  Spec: THEN REPLACING { category-name DATA BY ... } ...
  Grammar: initializeReplacingPhrase: REPLACING (no THEN prefix)
  Gap: THEN keyword before REPLACING absent; spec requires THEN REPLACING

MISMATCH: INITIALIZE — THEN TO DEFAULT phrase missing
  Spec: [ THEN TO DEFAULT ]
  Grammar: none
  Gap: THEN TO DEFAULT entirely absent

MISMATCH: INITIALIZE — initializeReplacingItem missing many category-names
  Spec: category-name = ALPHABETIC | ALPHANUMERIC | ALPHANUMERIC-EDITED | BOOLEAN | DATA-POINTER | FUNCTION-POINTER | MESSAGE-TAG | NATIONAL | NATIONAL-EDITED | NUMERIC | NUMERIC-EDITED | OBJECT-REFERENCE | PROGRAM-POINTER
  Grammar: ALPHABETIC | ALPHANUMERIC | NUMERIC | ALPHANUMERIC-EDITED | NUMERIC-EDITED (5 categories only)
  Gap: Missing BOOLEAN, DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, NATIONAL, NATIONAL-EDITED, OBJECT-REFERENCE, PROGRAM-POINTER
```

---

## INSPECT

**Spec Format 1 (tallying):** `INSPECT [BACKWARD] identifier-1 TALLYING tallying-phrase`
**Spec Format 2 (replacing):** `INSPECT [BACKWARD] identifier-1 REPLACING replacing-phrase`
**Spec Format 3 (tallying+replacing):** `INSPECT [BACKWARD] identifier-1 TALLYING tallying-phrase REPLACING replacing-phrase`
**Spec Format 4 (converting):** `INSPECT [BACKWARD] identifier-1 CONVERTING {identifier-6|literal-4} TO {identifier-7|literal-5} [after-before-phrase]`

**Grammar:**
```antlr
inspectStatement
    : INSPECT dataReference
      ( inspectTallyingPhrase inspectReplacingPhrase?
      | inspectReplacingPhrase
      | inspectConvertingPhrase )
    ;
```

```
MISMATCH: INSPECT — BACKWARD keyword missing from all formats
  Spec: INSPECT [BACKWARD] identifier-1 ...
  Grammar: INSPECT dataReference (no BACKWARD option)
  Gap: BACKWARD scanning direction entirely absent

MISMATCH: INSPECT TALLYING — FIRST phrase in tallying not in spec
  Spec tallying-phrase: FOR { CHARACTERS | ALL | LEADING } ... (no FIRST)
  Grammar: inspectCountPhrase includes FIRST inspectChar inspectDelimiters?
  Gap: FIRST is a REPLACING-only phrase per spec; not valid in TALLYING. Grammar over-accepts.

MISMATCH: INSPECT TALLYING — TRAILING phrase not in spec for either tallying or replacing
  Spec: tallying uses CHARACTERS/ALL/LEADING; replacing uses CHARACTERS/ALL/LEADING/FIRST
  Grammar: both inspectCountPhrase and inspectReplacingItem include TRAILING
  Gap: TRAILING is not a spec phrase in §14.9.22; it's a vendor extension (IBM/MF)

MISMATCH: INSPECT CONVERTING — converting phrase accepts only one identifier-6/literal-4 and one identifier-7/literal-5 per spec
  Spec Format 4: CONVERTING {identifier-6|literal-4} TO {identifier-7|literal-5} [after-before-phrase]
  Grammar: inspectConvertingPhrase: CONVERTING inspectChar TO inspectChar inspectBeforeAfterPhrase*
  Gap: Grammar is correct structurally. Note: inspectChar covers identifier and literal.

MISMATCH: INSPECT — inspectDelimiters allows only one BEFORE and one AFTER, but grammar allows BEFORE and AFTER together in both orders
  Spec: after-before-phrase: { AFTER INITIAL {id-4|lit-2} | BEFORE INITIAL {id-4|lit-2} }  (one phrase per operand)
  Grammar: inspectDelimiters: BEFORE INITIAL_? inspectChar (AFTER INITIAL_? inspectChar)? | AFTER INITIAL_? inspectChar (BEFORE INITIAL_? inspectChar)?
  Gap: Grammar allows both BEFORE+AFTER (two delimiters per operand) which is spec-correct; acceptable.
  Note: INITIAL_ (with underscore) — confirm this is the INITIAL keyword; should map to "INITIAL" in COBOL.
```

---

## MERGE

**Spec:**
```
MERGE file-name-1 { ON { ASCENDING | DESCENDING } KEY { data-name-1 } ... } ...
  [ COLLATING SEQUENCE { IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS alphabet-name-1} {FOR NATIONAL IS alphabet-name-2} } ]
  USING file-name-2 { file-name-3 } ...
  [ OUTPUT PROCEDURE IS procedure-name-1 [ { THROUGH | THRU } procedure-name-2 ] ]
  [ GIVING { file-name-4 } ... ]
```
**Grammar:**
```antlr
mergeStatement
    : MERGE mergeFileName
      mergeKeyPhrase+
      sortCollatingPhrase?
      mergeUsingPhrase
      ( mergeGivingPhrase | mergeOutputProcedurePhrase )?
      END_MERGE?
    ;
mergeKeyPhrase: ON? (ASCENDING | DESCENDING) KEY? dataReferenceList;
sortCollatingPhrase: COLLATING SEQUENCE IS? IDENTIFIER (IDENTIFIER)?;
mergeUsingPhrase: USING dataReferenceList;
mergeOutputProcedurePhrase: OUTPUT PROCEDURE IS? procedureName ((THRU | THROUGH) procedureName)?;
```

```
MISMATCH: MERGE — ON keyword is optional in grammar but required in spec
  Spec: ON { ASCENDING | DESCENDING } KEY { data-name-1 } ...
  Grammar: mergeKeyPhrase: ON? (ASCENDING | DESCENDING) KEY? dataReferenceList
  Gap: ON? — spec requires ON

MISMATCH: MERGE — KEY keyword is optional in grammar but required in spec
  Spec: KEY { data-name-1 } ...
  Grammar: KEY?
  Gap: KEY? — spec requires KEY

MISMATCH: MERGE — COLLATING SEQUENCE phrase uses IDENTIFIER for alphabet-name, not typed alphabet-name
  Spec: IS alphabet-name-1 [alphabet-name-2] or FOR ALPHANUMERIC IS alphabet-name-1 / FOR NATIONAL IS alphabet-name-2
  Grammar: COLLATING SEQUENCE IS? IDENTIFIER (IDENTIFIER)?
  Gap: FOR ALPHANUMERIC IS ... and FOR NATIONAL IS ... alternatives missing

MISMATCH: MERGE — USING accepts only dataReferenceList (min one), spec requires at least two files (file-name-2 { file-name-3 } ...)
  Spec: USING file-name-2 { file-name-3 } ... (two or more files required)
  Grammar: USING dataReferenceList (one-or-more)
  Gap: Grammar allows single file in USING; spec requires ≥2 (semantic gap)

MISMATCH: MERGE — END-MERGE not in spec
  Spec: no END-MERGE scope terminator defined for MERGE
  Grammar: END_MERGE? present
  Gap: END-MERGE is a vendor extension (IBM), not in ISO spec. Over-permissive.

MISMATCH: MERGE — OUTPUT PROCEDURE phrase: IS is optional in grammar but required in spec
  Spec: OUTPUT PROCEDURE IS procedure-name-1
  Grammar: OUTPUT PROCEDURE IS? procedureName
  Gap: IS? — spec requires IS

MISMATCH: MERGE — GIVING/OUTPUT PROCEDURE should be mutually exclusive but are combined
  Spec: Either OUTPUT PROCEDURE or GIVING, not both
  Grammar: ( mergeGivingPhrase | mergeOutputProcedurePhrase )? — correctly mutually exclusive; acceptable.
```

---

## MOVE

**Spec Format 1:** `MOVE { identifier-1 | literal-1 } TO { identifier-2 } ...`
**Spec Format 2:** `MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4`

**Grammar:**
```antlr
moveStatement
    : MOVE CORRESPONDING dataReference TO dataReference
    | MOVE moveSendingOperand moveReceivingPhrase
    ;
moveSendingOperand: literal | functionCall | dataReference;
moveReceivingPhrase
    : TO dataReferenceList
    | CORRESPONDING dataReference TO dataReference
    ;
```

```
MISMATCH: MOVE — CORR abbreviation missing
  Spec: MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4
  Grammar: MOVE CORRESPONDING dataReference TO dataReference (CORR not present)
  Gap: CORR abbreviation not accepted

MISMATCH: MOVE — duplicate CORRESPONDING alternatives in grammar
  Grammar has CORRESPONDING in both the first alternative and in moveReceivingPhrase: CORRESPONDING dataReference TO dataReference
  Gap: Structural ambiguity — MOVE CORRESPONDING X TO Y could match either path; grammar should unify

MISMATCH: MOVE — moveReceivingPhrase has CORRESPONDING sub-alternative that conflicts with first alternative
  Grammar: moveReceivingPhrase: TO dataReferenceList | CORRESPONDING dataReference TO dataReference
  Gap: The second alternative of moveReceivingPhrase is a Format 2 form but is reachable after a moveSendingOperand, which is wrong — CORRESPONDING form should not have a sending operand before it
```

---

## MULTIPLY

**Spec Format 1:** `MULTIPLY {id-1|lit-1} BY { identifier-2 [rounded-phrase] } ... [ON SIZE ERROR ...] [END-MULTIPLY]`
**Spec Format 2:** `MULTIPLY {id-1|lit-1} BY {id-2|lit-2} GIVING { identifier-3 [rounded-phrase] } ... [ON SIZE ERROR ...] [END-MULTIPLY]`

**Grammar:**
```antlr
multiplyStatement
    : MULTIPLY multiplyOperand BY multiplyByOperand+ multiplyGivingPhrase? arithmeticOnSizeError? END_MULTIPLY?
    ;
multiplyByOperand: receivingOperand ROUNDED?;
```

```
MISMATCH: MULTIPLY — ROUNDED phrase is bare keyword only
  Spec: rounded-phrase = ROUNDED [MODE IS mode-name]
  Grammar: ROUNDED? (bare only)
  Gap: ROUNDED MODE IS not supported

MISMATCH: MULTIPLY — Format 1 and Format 2 conflated: multiplyByOperand uses receivingOperand (dataReference|literal)
  Spec Format 1: BY { identifier-2 [ROUNDED] } ... (identifiers only, each is a receiving target)
  Spec Format 2: BY {identifier-2|literal-2} GIVING identifier-3 (one operand, may be literal, before GIVING)
  Grammar: multiplyByOperand: receivingOperand ROUNDED? where receivingOperand: dataReference | literal
  Gap: In Format 1, literals should not be receiving operands (can't store into literal); grammar over-accepts
```

---

## OPEN

**Spec:**
```
OPEN { INPUT | OUTPUT | I-O | EXTEND } [sharing-phrase] [retry-phrase] { file-name-1 [WITH NO REWIND] } ... ...
```
**Grammar:**
```antlr
openStatement: OPEN openClause+;
openClause: openMode dataReference+;
openMode: INPUT | OUTPUT | I_O | EXTEND;
```

```
MISMATCH: OPEN — sharing-phrase missing
  Spec: [SHARING WITH { ALL OTHER | NO OTHER | READ ONLY }]
  Grammar: no sharing phrase
  Gap: SHARING WITH entirely absent

MISMATCH: OPEN — retry-phrase missing
  Spec: [retry-phrase]
  Grammar: none
  Gap: RETRY phrase (§14.7.9) entirely absent from OPEN

MISMATCH: OPEN — WITH NO REWIND per-file clause missing
  Spec: file-name-1 [WITH NO REWIND]
  Grammar: openClause: openMode dataReference+ (no NO REWIND per file)
  Gap: WITH NO REWIND absent

MISMATCH: OPEN — each mode can open multiple files; spec treats each mode as its own phrase with sharing, retry, and file list; grammar's openClause collapses all files under one mode but misses the nested structure
  Spec: OPEN {mode} [sharing] [retry] {file [NO REWIND]} ... may repeat for different modes
  Grammar: openClause: openMode dataReference+ (no sharing, no retry, no NO REWIND)
  Summary: sharing, retry, and NO REWIND all missing
```

---

## PERFORM

**Spec Format 1 (out-of-line):**
```
PERFORM procedure-name-1 [ { THROUGH | THRU } procedure-name-2 ]
  [ times-phrase | until-phrase | varying-phrase ]
```
**Spec Format 2 (inline):**
```
PERFORM [ times-phrase | until-phrase | varying-phrase ]
  imperative-statement-1 END-PERFORM
```
**Spec varying-phrase:**
```
[WITH TEST {BEFORE|AFTER}]
VARYING {identifier-2|index-name-1} FROM {identifier-3|index-name-2|literal-1}
[BY {identifier-4|literal-2}] UNTIL condition-1
[AFTER {identifier-5|index-name-3} FROM {identifier-6|index-name-4|literal-3}
 [BY {identifier-7|literal-4}] UNTIL condition-2] ...
```

**Grammar:**
```antlr
performVarying
    : (WITH? TEST (BEFORE | AFTER))?
      VARYING dataReference FROM arithmeticExpression
      BY arithmeticExpression
      UNTIL condition
      performVaryingAfter*
    ;
performVaryingAfter
    : AFTER dataReference FROM arithmeticExpression
      BY arithmeticExpression
      UNTIL condition
    ;
```

```
MISMATCH: PERFORM VARYING — BY phrase is required in grammar but optional in spec
  Spec: [BY {identifier-4|literal-2}] (optional, default = 1)
  Grammar: VARYING dataReference FROM arithmeticExpression BY arithmeticExpression UNTIL condition
  Gap: BY clause is mandatory in grammar; spec says it's optional with default of 1

MISMATCH: PERFORM VARYING — FROM accepts arithmeticExpression but spec allows index-name specifically
  Spec: FROM {identifier-3 | index-name-2 | literal-1}
  Grammar: FROM arithmeticExpression
  Gap: arithmeticExpression is a superset; index-names may or may not parse as arithmeticExpression — likely fine in practice but semantically distinct

MISMATCH: PERFORM VARYING — AFTER BY phrase also required in grammar but optional in spec
  Spec: AFTER ... [BY identifier-7|literal-4] UNTIL condition-2
  Grammar: performVaryingAfter: AFTER dataReference FROM arithmeticExpression BY arithmeticExpression UNTIL condition
  Gap: BY is mandatory; same gap as VARYING

MISMATCH: PERFORM — WITH TEST: WITH is optional in grammar
  Spec: WITH TEST {BEFORE|AFTER}
  Grammar: WITH? TEST (BEFORE | AFTER)
  Gap: Spec requires WITH; grammar makes it optional. Common lenience.

MISMATCH: PERFORM — UNTIL EXIT phrase not modeled
  Spec: until-phrase: [WITH TEST {BEFORE|AFTER}] UNTIL { condition-1 | EXIT }
  Grammar: performUntil: (WITH? TEST (BEFORE | AFTER))? UNTIL condition
  Gap: UNTIL EXIT alternative absent (spec §14.9.28 SR 8)

MISMATCH: PERFORM Format 3 (exception-checking) entirely absent
  Spec: PERFORM [WITH LOCATION] imperative-statement-1 {WHEN EXCEPTION ... imperative-statement-2} ... [WHEN OTHER EXCEPTION ...] [WHEN COMMON EXCEPTION ...] [FINALLY ...] END-PERFORM
  Grammar: no Format 3 at all
  Gap: Exception-checking PERFORM (2023 feature) entirely unimplemented
```

---

## READ

**Spec Format 1 (sequential):**
```
READ file-name-1 { NEXT | PREVIOUS } RECORD [ INTO identifier-1 ]
  [ ADVANCING ON LOCK ] [ IGNORING LOCK ] [ retry-phrase ]
  [ WITH LOCK ] [ WITH NO LOCK ]
  [ AT END imperative-statement-1 ]
  [ NOT AT END imperative-statement-2 ]
  [ END-READ ]
```
**Spec Format 2 (random):**
```
READ file-name-1 RECORD [ INTO identifier-1 ]
  [ IGNORING LOCK ] [ retry-phrase ] [ WITH LOCK ] [ WITH NO LOCK ]
  [ KEY IS { data-name-1 | record-key-name-1 } ]
  [ INVALID KEY imperative-statement-3 ]
  [ NOT INVALID KEY imperative-statement-4 ]
  [ END-READ ]
```
**Grammar:**
```antlr
readStatement
    : READ fileName
      readDirection?
      readInto?
      readKey?
      readAtEnd?
      readInvalidKey?
      END_READ?
    ;
readDirection: (NEXT | PREVIOUS) RECORD;
readKey: KEY IS dataReference;
```

```
MISMATCH: READ — RECORD keyword missing between file-name and other clauses for Format 2
  Spec Format 2: READ file-name-1 RECORD [INTO ...] ...
  Grammar: READ fileName readDirection? readInto? ... (RECORD only in readDirection for sequential)
  Gap: Format 2 (random) requires RECORD keyword after file-name; grammar does not enforce it

MISMATCH: READ — ADVANCING ON LOCK phrase missing
  Spec: [ ADVANCING ON LOCK ]
  Grammar: none
  Gap: ADVANCING ON LOCK entirely absent

MISMATCH: READ — IGNORING LOCK phrase missing
  Spec: [ IGNORING LOCK ]
  Grammar: none
  Gap: IGNORING LOCK entirely absent

MISMATCH: READ — retry-phrase missing
  Spec: [ retry-phrase ]
  Grammar: none
  Gap: RETRY phrase absent from READ

MISMATCH: READ — WITH LOCK / WITH NO LOCK phrases missing
  Spec: [ WITH LOCK ] [ WITH NO LOCK ]
  Grammar: none
  Gap: Record locking phrases absent from READ

MISMATCH: READ — KEY IS: record-key-name-1 alternative not in grammar
  Spec: KEY IS { data-name-1 | record-key-name-1 }
  Grammar: KEY IS dataReference (dataReference only)
  Gap: record-key-name-1 (defined with SOURCE phrase in FD) not modeled as distinct alternative; likely parsed as dataReference but semantically distinct
```

---

## RELEASE

**Spec:**
```
RELEASE record-name-1 [ FROM { identifier-1 | literal-1 } ]
```
**Grammar:**
```antlr
releaseStatement
    : RELEASE dataReference
      (FROM dataReference)?
    ;
```

```
MISMATCH: RELEASE — FROM clause accepts only dataReference, not literal
  Spec: FROM { identifier-1 | literal-1 }
  Grammar: FROM dataReference (literal not accepted)
  Gap: literal missing from FROM phrase
```

---

## RETURN

**Spec:**
```
RETURN file-name-1 RECORD [ INTO identifier-1 ]
  AT END imperative-statement-1
  [ NOT AT END imperative-statement-2 ]
  [ END-RETURN ]
```
**Grammar:**
```antlr
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
```

```
MISMATCH: RETURN — AT END phrase is optional in grammar but required in spec
  Spec: AT END imperative-statement-1 (required — no brackets)
  Grammar: returnAtEndPhrase? (optional)
  Gap: Grammar allows RETURN without AT END; spec mandates it
```

---

## REWRITE

**Spec:**
```
REWRITE { record-name-1 | FILE file-name-1 } RECORD
  [ FROM { identifier-1 | literal-1 } ]
  [ retry-phrase ]
  [ WITH LOCK | WITH NO LOCK ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-REWRITE ]
```
**Grammar:**
```antlr
rewriteStatement
    : REWRITE recordName
      (FROM dataReference)?
      rewriteInvalidKeyPhrase?
      END_REWRITE?
    ;
```

```
MISMATCH: REWRITE — FILE file-name-1 form not supported
  Spec: REWRITE { record-name-1 | FILE file-name-1 } RECORD
  Grammar: REWRITE recordName (record-name only)
  Gap: FILE file-name-1 alternative absent

MISMATCH: REWRITE — RECORD keyword missing
  Spec: REWRITE record-name-1 RECORD (RECORD required by spec syntax rule)
  Grammar: REWRITE recordName (no RECORD keyword)
  Gap: RECORD keyword absent from grammar

MISMATCH: REWRITE — FROM accepts only dataReference, not literal
  Spec: FROM { identifier-1 | literal-1 }
  Grammar: FROM dataReference
  Gap: literal missing

MISMATCH: REWRITE — retry-phrase missing
  Spec: [ retry-phrase ]
  Grammar: none
  Gap: RETRY phrase absent

MISMATCH: REWRITE — WITH LOCK / WITH NO LOCK phrases missing
  Spec: [ WITH LOCK ] [ WITH NO LOCK ]
  Grammar: none
  Gap: Locking phrases absent
```

---

## SEARCH

**Spec Format 1 (serial):**
```
SEARCH identifier-1 [ VARYING { identifier-2 | index-name-1 } ]
  [ AT END imperative-statement-1 ]
  { WHEN condition-1 { imperative-statement-2 | NEXT SENTENCE } } ...
  [ END-SEARCH ]
```
**Spec Format 2 (all):**
```
SEARCH ALL identifier-1
  [ AT END imperative-statement-1 ]
  WHEN { data-name-1 { IS EQUAL TO | IS = } { identifier-3 | literal-1 | arithmetic-expression-1 } | condition-name-1 }
       [ AND { data-name-2 { IS EQUAL TO | IS = } { identifier-4 | literal-2 | arithmetic-expression-2 } | condition-name-2 } ] ...
  { imperative-statement-2 | NEXT SENTENCE }
  [ END-SEARCH ]
```
**Grammar:**
```antlr
searchStatement
    : SEARCH dataReference (VARYING dataReference)?
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
    ;
searchWhenClause: WHEN condition statementBlock*;
searchAtEndClause
    : AT END statementBlock (NOT AT END statementBlock)?
    | END statementBlock
    ;
searchAllStatement
    : SEARCH ALL dataReference
      searchAllKeyPhrase?
      searchAtEndClause?
      searchAllWhenClause+
      END_SEARCH?
    ;
searchAllKeyPhrase: KEY IS dataReference;
searchAllWhenClause: WHEN condition statementBlock*;
```

```
MISMATCH: SEARCH ALL — searchAllKeyPhrase (KEY IS dataReference) not in spec
  Spec Format 2: No KEY IS phrase exists in SEARCH ALL format
  Grammar: searchAllKeyPhrase: KEY IS dataReference
  Gap: This is a non-spec clause; should be removed or flagged as extension

MISMATCH: SEARCH ALL — WHEN clause structure simplified
  Spec: WHEN data-name-1 {IS EQUAL TO|IS=} {identifier-3|literal-1|arith-expr-1} [AND ...] imperative-statement-2
  Grammar: WHEN condition statementBlock* (condition catches any condition, not specifically the EQUAL TO key search form)
  Gap: The grammar accepts any condition in SEARCH ALL WHEN, but the spec restricts WHEN to equality tests on KEY fields and condition-names. Not a parse failure but semantic over-acceptance.

MISMATCH: SEARCH Format 1 — VARYING accepts dataReference but spec allows identifier or index-name-1
  Spec: VARYING { identifier-2 | index-name-1 }
  Grammar: VARYING dataReference
  Gap: index-name-1 (with INDEXED BY) is a distinct case — parsed as dataReference but semantically distinct; acceptable in practice.

MISMATCH: SEARCH — AT END grammar adds NOT AT END which is not in spec
  Spec: [ AT END imperative-statement-1 ] (no NOT AT END)
  Grammar: searchAtEndClause: AT END statementBlock (NOT AT END statementBlock)?
  Gap: NOT AT END in SEARCH is a vendor extension (not in §14.9.37 of ISO 2023)

MISMATCH: SEARCH — END (no AT) alternative in searchAtEndClause
  Spec: AT END imperative-statement-1 (AT is required)
  Grammar: | END statementBlock (AT omitted alternative)
  Gap: Bare END without AT is not spec syntax
```

---

## SET

**Spec Format 1 (index-assignment):** `SET {index-name-1|identifier-1} ... TO {arithmetic-expression-1|index-name-2|identifier-2}`
**Spec Format 2 (index-arithmetic):** `SET {index-name-3} ... {UP BY|DOWN BY} arithmetic-expression-2`
**Spec Format 3 (switch-setting):** `SET {mnemonic-name-1} ... TO {ON|OFF} ...`
**Spec Format 4 (condition-setting):** `SET {condition-name-1} ... TO {TRUE|FALSE}`
**Spec Format 5 (object-reference):** `SET {identifier-3} ... TO {object-class-name-1|identifier-4}`
**Spec Formats 6-17:** attribute, data-pointer, function-pointer, program-pointer, data-pointer-arithmetic, locale, dynamic-capacity-table, numeric-content, dynamic-length, message-tag

**Grammar:**
```antlr
setStatement
    : setToValueStatement
    | setBooleanStatement
    | setAddressStatement
    | setObjectReferenceStatement
    | setIndexStatement
    ;
setToValueStatement: SET dataReference+ TO arithmeticExpression;
setBooleanStatement: SET dataReference+ TO (TRUE_ | FALSE_);
setAddressStatement: SET ADDRESS OF dataReference TO dataReference;
setObjectReferenceStatement: {is2002()}? SET dataReference TO objectReference;
setIndexStatement: SET dataReference+ (UP|DOWN) BY arithmeticExpression;
```

```
MISMATCH: SET Format 3 — switch-setting (mnemonic TO ON/OFF) missing
  Spec: SET {mnemonic-name-1} ... TO {ON|OFF} ...
  Grammar: no switch-setting variant
  Gap: SET TO ON/OFF entirely absent

MISMATCH: SET Format 6 — screen ATTRIBUTE setting missing
  Spec: SET screen-name-1 ATTRIBUTE {BELL|BLINK|HIGHLIGHT|LOWLIGHT|REVERSE-VIDEO|UNDERLINE} {OFF|ON} ...
  Grammar: none
  Gap: Screen attribute setting entirely absent

MISMATCH: SET Format 7 — data-pointer-assignment missing
  Spec: SET {ADDRESS OF data-name-1|identifier-5} ... TO identifier-6
  Grammar: setAddressStatement: SET ADDRESS OF dataReference TO dataReference (partial)
  Gap: setAddressStatement only handles ADDRESS OF form; identifier-5 (pointer identifier as target) alternative absent

MISMATCH: SET Format 8/9 — function-pointer/program-pointer assignment missing
  Spec: SET {identifier-12} ... TO identifier-13 (function-pointer) / SET {identifier-7} ... TO identifier-8 (program-pointer)
  Grammar: none (collapsed into setToValueStatement which only accepts arithmeticExpression, not pointer value)
  Gap: Pointer assignments absent

MISMATCH: SET Format 10 — data-pointer-arithmetic missing
  Spec: SET {identifier-9} ... {UP|DOWN} BY arithmetic-expression-3
  Grammar: setIndexStatement covers UP/DOWN BY but is semantically for indexes; data-pointer arithmetic distinct
  Gap: Semantic distinction missing (parsed but not correctly typed)

MISMATCH: SET Format 11/12 — LOCALE forms missing
  Spec: SET LOCALE ... TO ...
  Grammar: none
  Gap: Locale setting entirely absent

MISMATCH: SET Format 13 — LAST EXCEPTION TO OFF missing
  Spec: SET LAST EXCEPTION TO OFF
  Grammar: none
  Gap: Exception clearing absent

MISMATCH: SET Format 14 — dynamic-capacity-table missing
  Spec: SET data-name-2 {UP BY|DOWN BY|TO} {integer-1|arithmetic-expression-4}
  Grammar: none
  Gap: Dynamic capacity table absent

MISMATCH: SET Format 15 — numeric-content (FLOAT-INFINITY etc.) missing
  Spec: SET CONTENT OF {identifier-14}… TO {FARTHEST-FROM-ZERO|FLOAT-INFINITY|FLOAT-NOT-A-NUMBER|...}
  Grammar: none
  Gap: Entirely absent

MISMATCH: SET Format 16 — dynamic-length (SIZE OF) missing
  Spec: SET [SIZE OF] data-name-3 TO {integer-2|arithmetic-expression-5}
  Grammar: none
  Gap: Entirely absent

MISMATCH: SET Format 17 — message-tag assignment missing
  Spec: SET data-name-4 TO {data-name-5|NULL}
  Grammar: setObjectReferenceStatement: SET dataReference TO objectReference (catches NULL but conflates with OO)
  Gap: Message-tag specific form not distinguished
```

---

## SORT

**Spec Format 1 (file):**
```
SORT file-name-1 { ON { ASCENDING | DESCENDING } KEY { data-name-1 } ... } ...
  [ WITH DUPLICATES IN ORDER ]
  [ COLLATING SEQUENCE { IS alphabet-name-1 [alphabet-name-2] | FOR ALPHANUMERIC IS ... | FOR NATIONAL IS ... } ]
  { INPUT PROCEDURE IS procedure-name-1 [{THROUGH|THRU} procedure-name-2] | USING { file-name-2 } ... }
  { OUTPUT PROCEDURE IS procedure-name-3 [{THROUGH|THRU} procedure-name-4] | GIVING { file-name-3 } ... }
```
**Spec Format 2 (table):**
```
SORT data-name-2 [ ON { ASCENDING | DESCENDING } KEY [data-name-1] … ] …
  [ WITH DUPLICATES IN ORDER ]
  [ COLLATING SEQUENCE ... ]
```
**Grammar:**
```antlr
sortStatement
    : SORT sortFileName
      sortKeyPhrase+
      sortDuplicatesPhrase?
      sortCollatingPhrase?
      ( sortUsingPhrase | sortInputProcedurePhrase )
      ( sortGivingPhrase | sortOutputProcedurePhrase )?
      END_SORT?
    ;
sortKeyPhrase: ON? (ASCENDING | DESCENDING) KEY? dataReferenceList;
sortDuplicatesPhrase: WITH? DUPLICATES IN? IDENTIFIER?;
sortCollatingPhrase: COLLATING SEQUENCE IS? IDENTIFIER (IDENTIFIER)?;
sortUsingPhrase: USING dataReferenceList;
```

```
MISMATCH: SORT — ON is optional in grammar but required in spec
  Spec: ON { ASCENDING | DESCENDING } KEY ...
  Grammar: ON?
  Gap: ON should be required

MISMATCH: SORT — KEY is optional in grammar but required in spec Format 1
  Spec: KEY { data-name-1 } ... (required in Format 1)
  Grammar: KEY?
  Gap: KEY? makes KEY optional

MISMATCH: SORT — sortDuplicatesPhrase: WITH is optional, IDENTIFIER? is strange
  Spec: WITH DUPLICATES IN ORDER
  Grammar: WITH? DUPLICATES IN? IDENTIFIER?
  Gap: WITH required by spec; ORDER should be the keyword after IN, not IDENTIFIER?; IN is optional in grammar but must precede ORDER

MISMATCH: SORT — COLLATING SEQUENCE FOR ALPHANUMERIC/FOR NATIONAL alternatives missing
  Spec: { IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS alphabet-name-1} {FOR NATIONAL IS alphabet-name-2} }
  Grammar: COLLATING SEQUENCE IS? IDENTIFIER (IDENTIFIER)?
  Gap: FOR ALPHANUMERIC IS... and FOR NATIONAL IS... absent

MISMATCH: SORT — INPUT PROCEDURE: IS is optional but required by spec
  Spec: INPUT PROCEDURE IS procedure-name-1
  Grammar: INPUT PROCEDURE IS?
  Gap: IS should be required

MISMATCH: SORT — OUTPUT PROCEDURE: IS is optional but required by spec
  Spec: OUTPUT PROCEDURE IS procedure-name-3
  Grammar: OUTPUT PROCEDURE IS?
  Gap: IS should be required

MISMATCH: SORT — GIVING/OUTPUT PROCEDURE are optional in grammar but spec requires one or the other
  Spec: Either OUTPUT PROCEDURE or GIVING is required
  Grammar: ( sortGivingPhrase | sortOutputProcedurePhrase )? — makes it optional
  Gap: Missing enforcement that one output form is required

MISMATCH: SORT Format 2 (table sort) entirely absent
  Spec: SORT data-name-2 [ON {ASCENDING|DESCENDING} KEY [data-name-1]...] ... [WITH DUPLICATES IN ORDER] [COLLATING SEQUENCE ...]
  Grammar: sortStatement requires sortUsingPhrase or sortInputProcedurePhrase — always file sort; table sort form not present
  Gap: Table SORT entirely unimplemented

MISMATCH: SORT — END-SORT not in spec
  Spec: No END-SORT scope terminator
  Grammar: END_SORT? present
  Gap: Vendor extension
```

---

## START

**Spec:**
```
START file-name-1
  [ FIRST | KEY relational-operator { data-name-1 | record-key-name-1 } [WITH LENGTH arithmetic-expression-1] | LAST ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-START ]
```
**Grammar:**
```antlr
startStatement
    : START fileName
      startKeyPhrase?
      startInvalidKeyPhrase?
      END_START?
    ;
startKeyPhrase: KEY IS comparisonExpression;
```

```
MISMATCH: START — FIRST phrase missing
  Spec: [ FIRST | KEY relational-operator ... | LAST ]
  Grammar: startKeyPhrase: KEY IS comparisonExpression (only KEY form)
  Gap: FIRST and LAST keywords not supported in START

MISMATCH: START — LAST phrase missing
  Spec: LAST alternative
  Grammar: none
  Gap: LAST absent

MISMATCH: START — KEY IS should be KEY (IS is optional noise word)
  Spec: KEY relational-operator data-name-1 (IS is not in the spec syntax; it's KEY directly followed by relational-operator)
  Grammar: startKeyPhrase: KEY IS comparisonExpression
  Gap: IS is present but not spec-defined; relational operator placement differs. The spec uses KEY <relational-op> data-name, not KEY IS condition.

MISMATCH: START KEY — WITH LENGTH arithmetic-expression-1 phrase missing
  Spec: KEY relational-operator data-name-1 [WITH LENGTH arithmetic-expression-1]
  Grammar: KEY IS comparisonExpression (no LENGTH phrase)
  Gap: WITH LENGTH entirely absent

MISMATCH: START — KEY clause structure wrong
  Spec: KEY relational-operator {data-name-1|record-key-name-1}
  Grammar: KEY IS comparisonExpression
  Gap: comparisonExpression is too broad; spec requires specifically a relational-operator followed by a data-name, not a full condition expression
```

---

## STOP

**Spec:** `STOP RUN [ WITH { ERROR | NORMAL } STATUS [ identifier-1 | literal-1 ] ]`

**Grammar:**
```antlr
stopStatement: STOP RUN;
```

```
MISMATCH: STOP — WITH ERROR/NORMAL STATUS phrase missing
  Spec: STOP RUN [ WITH { ERROR | NORMAL } STATUS [ identifier-1 | literal-1 ] ]
  Grammar: STOP RUN (bare only)
  Gap: WITH ERROR/NORMAL STATUS entirely absent
```

---

## STRING

**Spec:**
```
STRING { {identifier-1|literal-1} ... [DELIMITED BY {identifier-2|literal-2|SIZE}] } ...
INTO identifier-3
[WITH POINTER identifier-4]
[ON OVERFLOW imperative-statement-1]
[NOT ON OVERFLOW imperative-statement-2]
[END-STRING]
```
**Grammar:**
```antlr
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
stringOnOverflow
    : ON OVERFLOW statementBlock
      (NOT ON OVERFLOW statementBlock)?
    ;
```

```
MISMATCH: STRING — delimitedByPhrase accepts ALL before delimiter, but spec does not allow ALL in STRING
  Spec: DELIMITED BY {identifier-2|literal-2|SIZE} (no ALL)
  Grammar: DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant | SIZE)
  Gap: ALL is a vendor extension; not in ISO STRING format

MISMATCH: STRING — ON OVERFLOW: ON keyword should be required
  Spec: ON OVERFLOW imperative-statement-1
  Grammar: stringOnOverflow: ON OVERFLOW statementBlock (ON is present — correct)
  Note: This is actually fine; ON is present in grammar.

MISMATCH: STRING — WITH is required before POINTER in spec
  Spec: WITH POINTER identifier-4
  Grammar: stringWithPointer: WITH POINTER dataReference (WITH is required — correct)
  Note: This is fine.

MISMATCH: STRING — stringSendingPhrase groups operands under one DELIMITED phrase incorrectly
  Spec: The structure is: { {id-1|lit-1} ... [DELIMITED BY ...] } ... where multiple sending items share one delimiter
  Grammar: stringSendingPhrase: (dataReference|literal|figurativeConstant) delimitedByPhrase? — each item has its own optional delimiter
  Note: This is acceptable structurally; the grammar allows each item to have a delimiter, which is spec-consistent.
```

---

## SUBTRACT

**Spec Format 1:** `SUBTRACT {id-1|lit-1} ... FROM { identifier-2 [rounded-phrase] } ... [ON SIZE ERROR ...] [END-SUBTRACT]`
**Spec Format 2:** `SUBTRACT {id-1|lit-1} ... FROM {id-2|lit-2} GIVING { identifier-3 [rounded-phrase] } ... [ON SIZE ERROR ...] [END-SUBTRACT]`
**Spec Format 3:** `SUBTRACT {CORRESPONDING|CORR} identifier-4 FROM identifier-5 [rounded-phrase] [ON SIZE ERROR ...] [END-SUBTRACT]`

**Grammar:**
```antlr
subtractStatement
    : SUBTRACT CORRESPONDING dataReference FROM dataReference ROUNDED? arithmeticOnSizeError? END_SUBTRACT?
    | SUBTRACT subtractOperandList subtractFromPhrase? subtractGivingPhrase? arithmeticOnSizeError? END_SUBTRACT?
    ;
subtractFromPhrase: FROM subtractFromOperand;
subtractFromOperand
    : receivingArithmeticOperand (receivingArithmeticOperand)*
    | receivingOperand
    ;
```

```
MISMATCH: SUBTRACT — CORR abbreviation missing
  Spec: SUBTRACT { CORRESPONDING | CORR }
  Grammar: SUBTRACT CORRESPONDING (CORR absent)
  Gap: CORR not accepted

MISMATCH: SUBTRACT — FROM phrase is optional in grammar but required in spec
  Spec: SUBTRACT ... FROM ... (FROM is required in all formats)
  Grammar: subtractFromPhrase? (optional)
  Gap: Grammar allows SUBTRACT operands with no FROM, which is invalid per spec

MISMATCH: SUBTRACT — subtractFromOperand conflates Format 1 and Format 2
  Spec Format 1: FROM { identifier-2 [ROUNDED] } ... (receiving operands)
  Spec Format 2: FROM {identifier-2|literal-2} GIVING identifier-3 (source operand before GIVING)
  Grammar: subtractFromOperand: receivingArithmeticOperand+ | receivingOperand — same ambiguity as MULTIPLY

MISMATCH: SUBTRACT — ROUNDED phrase is bare keyword only
  Spec: rounded-phrase = ROUNDED [MODE IS mode-name]
  Grammar: ROUNDED? (bare only)
  Gap: ROUNDED MODE IS not supported
```

---

## UNSTRING

**Spec:**
```
UNSTRING identifier-1
  [ DELIMITED BY [ALL] {identifier-2|literal-1} [OR [ALL] {identifier-3|literal-2}] … ]
INTO { identifier-4 [DELIMITER IN identifier-5] [COUNT IN identifier-6] } …
[WITH POINTER identifier-7]
[TALLYING IN identifier-8]
[ON OVERFLOW imperative-statement-1]
[NOT ON OVERFLOW imperative-statement-2]
[END-UNSTRING]
```
**Grammar:**
```antlr
unstringDelimiterPhrase
    : DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant)
    ;
unstringIntoPhrase
    : INTO unstringIntoTarget+
    ;
```

```
MISMATCH: UNSTRING — DELIMITED BY only accepts one delimiter; spec allows OR {ALL} {identifier-3|literal-2} ... chain
  Spec: DELIMITED BY [ALL] {id-2|lit-1} [OR [ALL] {id-3|lit-2}] …
  Grammar: unstringDelimiterPhrase: DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant)
  Gap: OR alternative delimiters not supported — can only specify one delimiter

MISMATCH: UNSTRING — INTO is separate rule but spec has INTO as part of main statement
  Note: Grammar has unstringIntoPhrase: INTO unstringIntoTarget+ — structurally correct; acceptable.
```

---

## USE

**Spec Format 1:** `USE [GLOBAL] AFTER STANDARD {EXCEPTION|ERROR} PROCEDURE ON { {file-name-1}... | INPUT | OUTPUT | I-O | EXTEND }`
**Spec Format 2:** `USE [GLOBAL] BEFORE REPORTING identifier-1`
**Spec Format 3:** `USE AFTER {EXCEPTION CONDITION|EC} { exception-name-1 | exception-name-2 FILE file-name-2 } ...`
**Spec Format 4:** `USE AFTER {EXCEPTION OBJECT|EO} { object-class-name-1 | interface-name-1 }`

**Grammar:**
```antlr
useStatement
    : USE BEFORE REPORTING procedureName
    | USE AFTER STANDARD ERROR PROCEDURE ON fileName+
    ;
```

```
MISMATCH: USE — GLOBAL phrase missing from both formats
  Spec: USE [GLOBAL] AFTER ... and USE [GLOBAL] BEFORE ...
  Grammar: no GLOBAL keyword in either alternative
  Gap: GLOBAL modifier entirely absent

MISMATCH: USE Format 1 — EXCEPTION synonym for ERROR missing
  Spec: USE AFTER STANDARD {EXCEPTION|ERROR} PROCEDURE ON ...
  Grammar: USE AFTER STANDARD ERROR PROCEDURE ON ... (ERROR only)
  Gap: EXCEPTION keyword not accepted as synonym

MISMATCH: USE Format 1 — INPUT/OUTPUT/I-O/EXTEND alternatives missing
  Spec: ... PROCEDURE ON { {file-name-1}... | INPUT | OUTPUT | I-O | EXTEND }
  Grammar: USE AFTER STANDARD ERROR PROCEDURE ON fileName+ (file-names only)
  Gap: INPUT, OUTPUT, I-O, EXTEND mode alternatives absent

MISMATCH: USE Format 3 — EXCEPTION CONDITION / EC exception-name form missing
  Spec: USE AFTER {EXCEPTION CONDITION|EC} { exception-name-1 | exception-name-2 FILE file-name-2 } ...
  Grammar: none
  Gap: Exception-name based USE declaratives entirely absent

MISMATCH: USE Format 4 — EXCEPTION OBJECT / EO form missing
  Spec: USE AFTER {EXCEPTION OBJECT|EO} { object-class-name-1 | interface-name-1 }
  Grammar: none
  Gap: Entirely absent

MISMATCH: USE — REPORTING uses procedureName but spec uses identifier-1 (report-group name)
  Spec: USE [GLOBAL] BEFORE REPORTING identifier-1 (identifier-1 is a report-group name)
  Grammar: USE BEFORE REPORTING procedureName
  Gap: procedureName is a procedure-name not a data-name/report-group-name; semantically incorrect
```

---

## WRITE

**Spec Format 1 (sequential):**
```
WRITE { record-name-1 | FILE file-name-1 } [ FROM { identifier-1 | literal-1 } ]
  [ { BEFORE | AFTER } ADVANCING { {identifier-2|integer-1} [LINE|LINES] | mnemonic-name-1 | PAGE } ]
  [ retry-phrase ]
  [ WITH LOCK ] [ WITH NO LOCK ]
  [ AT { END-OF-PAGE | EOP } imperative-statement-1 ]
  [ NOT AT { END-OF-PAGE | EOP } imperative-statement-2 ]
  [ END-WRITE ]
```
**Spec Format 2 (random):**
```
WRITE { record-name-1 | FILE file-name-1 } [ FROM { identifier-1 | literal-1 } ]
  [ retry-phrase ] [ WITH LOCK ] [ WITH NO LOCK ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-WRITE ]
```
**Grammar:**
```antlr
writeStatement
    : WRITE recordName
      writeFrom?
      writeBeforeAfter?
      writeAtEndOfPage?
      writeInvalidKey?
      END_WRITE?
    ;
writeFrom: FROM dataReference;
writeBeforeAfter
    : (BEFORE | AFTER) ADVANCING
      ( PAGE
      | (dataReference | integerLiteral | literal) (LINE | LINES)?
      )
    ;
```

```
MISMATCH: WRITE — FILE file-name-1 form not supported
  Spec: WRITE { record-name-1 | FILE file-name-1 } ...
  Grammar: WRITE recordName (record-name only)
  Gap: FILE file-name-1 alternative absent

MISMATCH: WRITE — FROM accepts only dataReference, not literal
  Spec: FROM { identifier-1 | literal-1 }
  Grammar: writeFrom: FROM dataReference
  Gap: literal missing from FROM phrase

MISMATCH: WRITE — retry-phrase missing
  Spec: [ retry-phrase ]
  Grammar: none
  Gap: RETRY phrase absent from WRITE

MISMATCH: WRITE — WITH LOCK / WITH NO LOCK phrases missing
  Spec: [ WITH LOCK ] [ WITH NO LOCK ]
  Grammar: none
  Gap: Locking phrases absent

MISMATCH: WRITE — ADVANCING mnemonic-name-1 accepts any literal, not specifically mnemonic
  Spec: ADVANCING mnemonic-name-1 (a SPECIAL-NAMES mnemonic)
  Grammar: ADVANCING (dataReference | integerLiteral | literal) (LINE|LINES)?
  Note: literal would match a mnemonic parsed as an identifier via dataReference; acceptable in practice.

MISMATCH: WRITE — ADVANCING BEFORE/AFTER: the spec says BEFORE or AFTER is required before ADVANCING
  Spec: [ { BEFORE | AFTER } ADVANCING ... ]
  Grammar: (BEFORE | AFTER) ADVANCING — correct when present. The whole clause is optional. OK.

MISMATCH: WRITE Format 2 — ADVANCING clause should not be present in random format
  Spec Format 2: No ADVANCING phrase
  Grammar: writeBeforeAfter? appears before writeInvalidKey? — could be specified for random WRITE
  Gap: Grammar allows ADVANCING for any WRITE; spec restricts it to sequential (Format 1). Semantic gap.
```

---

## Additional Statements Not Listed in Task But Present in Grammar

### ENTRY (non-spec extension)
```
MISMATCH: ENTRY — not in ISO 1989:2023
  Spec: ENTRY is not part of ISO COBOL 2023 §14.9
  Grammar: entryStatement: ENTRY literal usingClause?
  Note: IBM extension; present as known vendor extension. Not a conformance issue if clearly marked.
```

### GOBACK (additional comparison note)
```
MISMATCH: GOBACK — already covered above (RAISING and WITH STATUS missing)
```

---

## Summary Table of All Mismatches by Severity

### Critical Gaps (significant conformance holes):

1. **ACCEPT** — END-ACCEPT missing; Format 3 (screen) entirely absent; device FROM mnemonic missing
2. **CONTINUE** — AFTER arithmetic SECONDS phrase missing (2023 feature)
3. **DELETE RECORD** — RECORD not enforced; retry-phrase missing
4. **DELETE FILE** — OVERRIDE missing; retry-phrase missing; single file only
5. **DISPLAY** — UPON missing; NO ADVANCING missing; END-DISPLAY missing; screen format absent
6. **EXIT PROGRAM** — RAISING phrase entirely missing
7. **GOBACK** — RAISING and WITH STATUS phrases entirely missing
8. **INITIALIZE** — WITH FILLER missing; TO VALUE missing; THEN REPLACING (THEN keyword) wrong; THEN TO DEFAULT missing; category-names incomplete
9. **INSPECT** — BACKWARD entirely missing; TRAILING is vendor extension wrongly included; FIRST in tallying is wrong
10. **OPEN** — SHARING WITH missing; retry-phrase missing; WITH NO REWIND per-file missing
11. **PERFORM** — BY phrase required but optional in spec; UNTIL EXIT missing; Format 3 (exception-checking) entirely absent
12. **READ** — RECORD keyword not enforced for Format 2; ADVANCING ON LOCK missing; IGNORING LOCK missing; retry-phrase missing; WITH LOCK/NO LOCK missing
13. **RETURN** — AT END incorrectly made optional
14. **REWRITE** — FILE form missing; RECORD keyword missing; literal in FROM missing; retry-phrase missing; locking missing
15. **SEARCH** — NOT AT END non-spec extension; KEY IS in SEARCH ALL non-spec; FIRST/LAST in START absent
16. **SET** — switch-setting (ON/OFF) missing; screen ATTRIBUTE missing; 8+ formats entirely absent
17. **SORT** — WITH DUPLICATES IN ORDER malformed; table sort (Format 2) entirely absent; INPUT/OUTPUT PROCEDURE IS keyword incorrect
18. **START** — FIRST/LAST phrases missing; KEY structure wrong; WITH LENGTH missing
19. **STOP** — WITH ERROR/NORMAL STATUS missing
20. **UNSTRING** — OR alternative delimiters not supported
21. **USE** — GLOBAL missing; EXCEPTION synonym missing; INPUT/OUTPUT/I-O/EXTEND modes missing; Formats 3 and 4 entirely absent
22. **WRITE** — FILE form missing; literal in FROM missing; retry-phrase missing; locking missing

### Consistent Cross-Statement Gaps:

- **ROUNDED MODE IS** — missing from ALL arithmetic statements (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE). Grammar only accepts bare ROUNDED keyword; spec §14.7.4 ROUNDED phrase includes optional `MODE IS {TRUNCATION|NEAREST-TOWARD-ZERO|NEAREST-EVEN|NEAREST-AWAY-FROM-ZERO|PROHIBITED}`
- **retry-phrase** — missing from OPEN, READ, WRITE, REWRITE, DELETE. Spec §14.7.9 RETRY phrase: `RETRY { FOREVER | arithmetic-expression-1 TIMES | FOR arithmetic-expression-2 SECONDS }`
- **CORR/CORRESPONDING** — CORR abbreviation missing from ADD, SUBTRACT, MOVE (all three have CORRESPONDING but not CORR)
- **IS optional where spec requires it** — across DEPENDING ON?, KEY IS?, PROCEDURE ON? — multiple places where IS/ON are required by spec but marked optional in grammar
- **WITH optional where spec requires it** — CLOSE WITH NO REWIND, SET ... WITH TEST, OPEN WITH NO REWIND