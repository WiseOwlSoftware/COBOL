Now I have the full INITIALIZE spec. Let me compile the INITIALIZE mismatches:

```
MISMATCH: initializeStatement — INITIALIZE spec format
  Spec:    INITIALIZE { identifier-1 } ... [ WITH FILLER ]
           [ { ALL | category-name } TO VALUE ]
           [ THEN REPLACING { category-name DATA BY { identifier-2 | literal-1 } } ... ]
           [ THEN TO DEFAULT ]
  Grammar: INITIALIZE dataReferenceList initializeReplacingPhrase?
  Gap:     WITH FILLER phrase absent
           ALL TO VALUE or category-name TO VALUE phrase absent
           THEN REPLACING (with THEN) — grammar uses plain REPLACING without THEN
           THEN TO DEFAULT phrase absent
```

```
MISMATCH: initializeReplacingItem — category-name list incomplete
  Spec:    category-name includes: ALPHABETIC | ALPHANUMERIC | ALPHANUMERIC-EDITED | BOOLEAN
           | DATA-POINTER | FUNCTION-POINTER | MESSAGE-TAG | NATIONAL | NATIONAL-EDITED
           | NUMERIC | NUMERIC-EDITED | OBJECT-REFERENCE | PROGRAM-POINTER
  Grammar: ALPHABETIC | ALPHANUMERIC | NUMERIC | (ALPHANUMERIC EDITED | ALPHANUMERIC_EDITED) 
           | (NUMERIC EDITED | NUMERIC_EDITED)
  Gap:     Missing: BOOLEAN, DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, NATIONAL, NATIONAL-EDITED,
           OBJECT-REFERENCE, PROGRAM-POINTER
```

```
MISMATCH: initializeReplacingItem — DATA keyword optionality
  Spec:    category-name DATA BY { identifier-2 | literal-1 }
           DATA is required in the REPLACING phrase (not optional).
  Grammar: ALPHABETIC DATA? BY ...
  Gap:     Grammar makes DATA optional; spec requires DATA in the REPLACING sub-phrase.
```

---

## Summary Table

The following table summarizes all mismatches by category:

| # | Rule | Type | Description |
|---|------|------|-------------|
| 1 | dataDivision | Missing | SCREEN SECTION not present |
| 2 | workingStorageSection etc. | Missing | constant-entry and type-declaration-entry not modeled as distinct variants |
| 3 | fileDescriptionEntry | Missing | IS EXTERNAL [ AS literal-1 ] not as named FD clause |
| 4 | fileDescriptionEntry | Missing | IS GLOBAL not as named FD clause |
| 5 | fileDescriptionEntry | Missing | BLOCK CONTAINS clause entirely absent as named rule |
| 6 | fileDescriptionEntry | Missing | RECORD clause entirely absent as named rule |
| 7 | fileDescriptionEntry | Missing | CODE-SET clause entirely absent as named rule |
| 8 | fileDescriptionEntry | Missing | FORMAT { BIT \| CHARACTER \| NUMERIC } DATA entirely absent |
| 9 | fileDescriptionEntry | Missing | REPORT IS / REPORTS ARE clause entirely absent |
| 10 | sortMergeDescriptionEntry | Wrong | Accepts all fileDescriptionClauses; spec only allows record-clause |
| 11 | labelRecordsClause | Extension | Not in ISO 2023; ARE not accepted for plural RECORDS |
| 12 | dataRecordsClause | Extension | Not in ISO 2023; RECORDS (plural) + ARE missing |
| 13 | linageClause | Optional→Required | IS is optional; spec requires IS |
| 14 | linageClause | Optional→Required | LINES is optional; spec requires LINES |
| 15 | linageFootingPhrase | Optional→Required | WITH is optional; spec requires WITH |
| 16 | linageFootingPhrase | Optional→Required | AT is optional; spec requires AT |
| 17 | linageLinesAtTopPhrase | Optional→Required | LINES and AT are optional; spec requires both |
| 18 | linageLinesAtBottomPhrase | Optional→Required | LINES and AT are optional; spec requires both |
| 19 | dataDescriptionClauses | Missing | ALIGNED clause absent (no lexer token) |
| 20 | dataDescriptionClauses | Missing | ANY LENGTH clause absent |
| 21 | dataDescriptionClauses | Missing | BASED clause absent |
| 22 | dataDescriptionClauses | Missing | CONSTANT RECORD clause absent |
| 23 | dataDescriptionClauses | Missing | DYNAMIC LENGTH clause absent |
| 24 | dataDescriptionClauses | Missing | GROUP-USAGE clause absent (no lexer token) |
| 25 | dataDescriptionClauses | Missing | PROPERTY clause absent |
| 26 | dataDescriptionClauses | Missing | SAME AS clause absent |
| 27 | dataDescriptionClauses | Missing | SELECT WHEN clause absent |
| 28 | dataDescriptionClauses | Missing | IS TYPEDEF [ STRONG ] not wired into dataDescriptionClauses |
| 29 | dataDescriptionClauses | Missing | validation-clauses entirely absent |
| 30 | pictureClause | Missing | EDITING phrase (Format 1 extended) absent |
| 31 | pictureClause | Missing | LOCALE/SIZE format (Format 2) absent |
| 32 | usageClause | Missing | BINARY-CHAR/SHORT/LONG/DOUBLE [ SIGNED \| UNSIGNED ] |
| 33 | usageClause | Missing | BIT usage |
| 34 | usageClause | Missing | FLOAT-BINARY-32/64/128 [ endianness-phrase ] |
| 35 | usageClause | Missing | FLOAT-DECIMAL-16/34 [ encoding-phrase \| endianness-phrase ] |
| 36 | usageClause | Missing | FLOAT-EXTENDED / FLOAT-LONG / FLOAT-SHORT |
| 37 | usageClause | Missing | MESSAGE-TAG usage |
| 38 | usageClause | Missing | NATIONAL usage |
| 39 | usageClause | Missing | OBJECT REFERENCE [...] |
| 40 | usageClause | Missing phrase | PACKED-DECIMAL WITH NO SIGN phrase absent |
| 41 | usageClause | Missing | POINTER as usageKeyword (POINTER exists only in I/O grammar) |
| 42 | usageClause | Missing | FUNCTION-POINTER TO function-prototype-name-1 |
| 43 | usageClause | Missing | PROGRAM-POINTER [ TO program-prototype-name-1 ] |
| 44 | usageClause | Extension | COMP-1/2/3/5 and COMPUTATIONAL-1/2/3/5 not in ISO 2023 |
| 45 | occursClause | Optional→Required | TIMES is optional; spec requires TIMES |
| 46 | occursClause | Optional→Required | ON is optional in DEPENDING ON; spec requires ON |
| 47 | occursKeyClause | Optional→Required | KEY and IS are optional; spec requires both |
| 48 | occursClause | Optional→Required | BY is optional in INDEXED BY; spec requires BY |
| 49 | occursClause | Missing | Format 4 (OCCURS DYNAMIC) entirely absent |
| 50 | occursClause | Missing | STEP phrase (report-writer Format 3) absent |
| 51 | externalClause | Optional→Required | IS is optional; spec requires IS |
| 52 | externalClause | Missing phrase | AS literal-1 phrase entirely absent |
| 53 | globalClause | Optional→Required | IS is optional; spec requires IS |
| 54 | justifiedClause | Optional→Required | RIGHT is optional; spec requires RIGHT |
| 55 | blankWhenZeroClause | Optional→Required | WHEN is optional; spec requires WHEN |
| 56 | valueClause | Missing phrase | IN alphabet-name-1 absent from condition-name VALUE |
| 57 | valueClause | Missing phrase | WHEN SET TO FALSE IS literal-4 absent |
| 58 | valueClause | Missing format | Format 2 (table format with FROM/TO subscripts) absent |
| 59 | initializeStatement | Missing | WITH FILLER phrase absent |
| 60 | initializeStatement | Missing | ALL TO VALUE / category-name TO VALUE phrase absent |
| 61 | initializeStatement | Missing | THEN keyword before REPLACING absent |
| 62 | initializeStatement | Missing | THEN TO DEFAULT phrase absent |
| 63 | initializeReplacingItem | Missing | BOOLEAN, DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, NATIONAL, NATIONAL-EDITED, OBJECT-REFERENCE, PROGRAM-POINTER category-names absent |
| 64 | initializeReplacingItem | Optional→Required | DATA keyword is optional; spec requires DATA |

---

## Key Observations

**Largest gaps by area:**

1. **FD entry clauses**: BLOCK CONTAINS, RECORD clause, CODE-SET, EXTERNAL/GLOBAL (on FD), FORMAT, and REPORT clauses are all handled by `genericFileDescriptionClause` rather than typed rules. This means they cannot be validated, bound, or emitted with any semantic precision.

2. **USAGE clause**: 15 of the 19 spec-defined usage alternatives are absent from the grammar. The grammar only handles the COBOL-74/85 core (BINARY, COMPUTATIONAL, COMP, DISPLAY, PACKED-DECIMAL, INDEX). All ISO 2023 additions (BINARY-CHAR/SHORT/LONG/DOUBLE, FLOAT-*, NATIONAL, BIT, OBJECT REFERENCE, POINTER, MESSAGE-TAG, FUNCTION-POINTER, PROGRAM-POINTER) are missing.

3. **Data description clauses**: 9 clauses from the Format 1 spec (ALIGNED, ANY LENGTH, BASED, CONSTANT RECORD, DYNAMIC LENGTH, GROUP-USAGE, PROPERTY, SAME AS, SELECT WHEN, TYPEDEF, validation-clauses) are absent.

4. **VALUE clause**: The WHEN SET TO FALSE phrase (critical for condition-name initialization) and the IN alphabet-name collating-sequence phrase are both missing.

5. **Optional-vs-required noise words**: Many keywords that the spec treats as required (IS after LINAGE, GLOBAL, EXTERNAL; WHEN in BLANK WHEN ZERO; RIGHT in JUSTIFIED; TIMES in OCCURS; KEY/IS in OCCURS key phrase; BY in INDEXED BY; WITH/AT in LINAGE sub-phrases) are made optional in the grammar. This means the grammar is more permissive than the spec allows, accepting syntactically invalid programs without error.

**Items that are deliberate extensions (not spec violations):**
- LABEL RECORDS clause (COBOL-74/85 backward compat)
- DATA RECORDS clause (COBOL-74/85 backward compat)
- COMP-1/2/3/5, COMPUTATIONAL-1/2/3/5 (IBM/MF extensions)
- `is2023()` guard on TYPE clause (correct gating of a 2023 feature)