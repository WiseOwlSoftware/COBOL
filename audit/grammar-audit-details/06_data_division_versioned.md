Now I have all the information needed. Let me compile the categorized report:

---

## COBOL Version Classification of Data Division Grammar Mismatches

Based on the GRAMMAR_AUDIT.md findings and cross-reference against ISO/IEC 1989:2023 §13 and the historical COBOL standard versioning, here is the full categorization of all 64 Data Division mismatches:

---

## COBOL-85 Required (grammar must match)

These features existed in ISO/IEC 1989:1985 and appear in every NIST COBOL-85 test suite. Parse failures on valid programs are bugs.

| # | Mismatch | Current Grammar | Spec Requirement |
|---|----------|----------------|-----------------|
| 1 | SPECIAL-NAMES paragraph requires at least one entry | `specialNameEntry+` | `specialNameEntry*` (empty paragraph is valid) |
| 2 | SYMBOLIC CHARACTERS: only one name → one ordinal per entry | `IDENTIFIER IS literal` (one-to-one only) | `{ symbolic-character-1 } ... [IS\|ARE] { integer-1 } ...` — multiple names to multiple ordinals in one clause |
| 3 | SYMBOLIC CHARACTERS: no `IN alphabet-name` phrase | missing | `[ IN alphabet-name-3 ]` required per §12.3.7 |
| 4 | ALPHABET clause: `FOR ALPHANUMERIC` / `FOR NATIONAL` phrases missing | `ALPHABET name IS definition` only | Two-branch form: `name [FOR ALPHANUMERIC] IS ...` and `name FOR NATIONAL IS ...` |
| 5 | ALPHABET: NATIVE, STANDARD-1, STANDARD-2 as named constants | partially via `IDENTIFIER` | Must be recognised keywords per §12.3.7 (STANDARD-1, STANDARD-2 missing as tokens) |
| 6 | CLASS definition: `FOR ALPHANUMERIC` / `FOR NATIONAL` phrase missing | `CLASS name IS classValueSet` | `CLASS name [FOR {ALPHANUMERIC / NATIONAL}] IS ...` per §12.3.7 |
| 7 | CURRENCY SIGN: `WITH PICTURE SYMBOL literal` phrase missing | `CURRENCY SIGN? IS? literal` | `CURRENCY SIGN IS literal-7 [WITH PICTURE SYMBOL literal-8]` per §12.3.7 |
| 8 | Implementor switch: bare `ON STATUS IS condition` / `OFF STATUS IS condition` without mnemonic-name | only `IDENTIFIER IS IDENTIFIER` prefix form | Two valid formats: `switch IS mnemonic [ON/OFF STATUS...]` and bare `{ON STATUS IS cond / OFF STATUS IS cond}` per §12.3.7 |
| 9 | FD BLOCK CONTAINS clause parsed as genericClause, not typed AST | `genericFileDescriptionClause` catch-all | `BLOCK CONTAINS [integer-1 TO] integer-2 {CHARACTERS / RECORDS}` per §13.18.10 — must be a named rule for binder use |
| 10 | FD RECORD clause (variable/fixed-length records) parsed as genericClause | `genericFileDescriptionClause` catch-all | `RECORD [IS] {VARYING [IN SIZE] [[FROM int-1] [TO int-2] [DEPENDING ON name]] / [CONTAINS] int-1 [TO int-2] CHARACTERS}` per §13.18.43 |
| 11 | FD CODE-SET clause parsed as genericClause | `genericFileDescriptionClause` catch-all | `CODE-SET IS alphabet-name-1` per §13.18.13 — must be named rule |
| 12 | LINAGE: `IS` made optional but `LINES` also optional — dual ambiguity | `LINAGE IS? ... LINES?` | `LINAGE IS {name / int} LINES` — `IS` is required per spec §13.18.34, `LINES` is optional noise word |
| 13 | LINAGE FOOTING: `WITH` optional, `AT` optional | both optional | Spec diagram shows both as optional noise words — grammar is actually correct here; mark as confirmed OK |
| 14 | OCCURS TIMES: `TIMES` made optional | `timesKeyword?` (optional) | Spec Format 1 shows `OCCURS integer-2 TIMES` — TIMES is required per §13.18.38 |
| 15 | OCCURS DEPENDING: `ON` made optional | `DEPENDING ON?` | Spec: `DEPENDING ON data-name-1` — ON is part of the phrase, optional noise word per COBOL-85 practice; acceptable |
| 16 | OCCURS KEY phrase: `IS` optional and `KEY` optional | `(ASCENDING|DESCENDING) KEY? IS?` | Spec: `{ASCENDING/DESCENDING} KEY IS {data-name-2}...` — KEY and IS are required |
| 17 | OCCURS INDEXED BY: `BY` optional | `INDEXED BY?` | Spec: `INDEXED BY {index-name-1}...` — BY is required |
| 18 | REDEFINES does not reject level-01, 66, 77, 88 at grammar level | no level check | §13.18.44 SR1: REDEFINES cannot appear on level 01, 66, 77, 88 — grammar should structurally prevent at least 66/88 |
| 19 | RENAMES (level-66): grammar allows any level, not enforced | `renamesClause` in `dataDescriptionBody` | §13.18.45: RENAMES only valid at level 66 |
| 20 | SIGN clause: leading `SIGN IS?` makes SIGN keyword itself optional | `(SIGN IS?)?` | Spec §13.18.52: `[SIGN IS] {LEADING/TRAILING} [SEPARATE CHARACTER]` — SIGN and IS are both optional noise words per spec; this is correct |
| 21 | JUSTIFIED: `RIGHT` optional | `(JUSTIFIED|JUST) RIGHT?` | Spec §13.18.32: `{JUSTIFIED/JUST} [RIGHT]` — RIGHT is optional per spec; this is correct |
| 22 | SYNCHRONIZED: LEFT/RIGHT optional phrases present but spec requires one | `(SYNCHRONIZED|SYNC) (LEFT|RIGHT)?` | Spec §13.18.55: `{SYNCHRONIZED/SYNC} [{LEFT/RIGHT}]` — optional per spec; this is correct |
| 23 | BLANK WHEN ZERO: `WHEN` optional | `BLANK WHEN? ZERO` | Spec §13.18.8: `BLANK WHEN ZERO` — WHEN is required; grammar makes it optional (over-lenient but not a parse failure) |
| 24 | EXTERNAL: `IS` optional | `IS? EXTERNAL` | Spec §13.18.22: `[IS] EXTERNAL` — IS is optional per spec; this is correct |
| 25 | GLOBAL: `IS` optional | `IS? GLOBAL` | Spec §13.18.27: `[IS] GLOBAL` — IS is optional per spec; this is correct |
| 26 | VALUE clause: `IN alphabet-name-1` phrase for condition-names missing | no alphabet phrase | Format 3 spec: `VALUES ARE literal [THRU literal] ... [IN alphabet-name-1]` per §13.18.63 |
| 27 | VALUE clause: `WHEN SET TO FALSE IS literal-4` for condition-names missing | no FALSE phrase | Format 3 spec: `[WHEN SET TO FALSE IS literal-4]` per §13.18.63 |
| 28 | INITIALIZE statement: `WITH FILLER` phrase missing | `INITIALIZE identList replacingPhrase?` | Spec: `INITIALIZE {id}... [WITH FILLER]` per §14.9.20 |
| 29 | INITIALIZE statement: `[ALL / category] TO VALUE` phrase missing | no TO VALUE phrase | Spec: `[[ALL / category-name] TO VALUE]` per §14.9.20 |
| 30 | INITIALIZE statement: `THEN REPLACING category DATA BY` phrase missing | only has `REPLACING category DATA? BY` (no `THEN`) | Spec uses `THEN REPLACING` as the phrase keyword per §14.9.20 |
| 31 | INITIALIZE statement: `THEN TO DEFAULT` phrase missing | not present | Spec: `[THEN TO DEFAULT]` per §14.9.20 |
| 32 | INITIALIZE REPLACING: missing category keywords ALPHABETIC-EDITED, NATIONAL, NATIONAL-EDITED, BOOLEAN | partial list | Spec category-name includes all 13 categories including BOOLEAN, DATA-POINTER, NATIONAL etc. |
| 33 | dataDescriptionBody: VALUE clause Format 2 (table init with FROM/TO subscripts) missing | no `FROM (subscript...) [TO (subscript...)]` form | Spec Format 2 per §13.18.63 — COBOL-85 feature |
| 34 | SCREEN SECTION missing from dataDivision | not present | §13.4 COBOL-85 requires SCREEN SECTION for screen I/O |
| 35 | FD entry: `IS EXTERNAL [AS literal-1]` not a typed clause | parsed via genericClause or not at all | §13.4.5.2 FD Format 1 shows `[IS EXTERNAL [AS literal-1]]` as a named structure |
| 36 | localStorageSection present but gated — should be COBOL-85 extension (MF/IBM) | present in grammar already | LOCAL-STORAGE SECTION appears in COBOL-2002 standard but was widespread pre-standard; acceptable |
| 37 | LABEL RECORDS: IDENTIFIER list for user labels not validated | accepts `IDENTIFIER+` | Spec §13.18 FD: LABEL RECORDS form is STANDARD or OMITTED only in ISO; user-label form is an IBM extension — should be flagged |
| 38 | DATA RECORDS clause: should be RECORDS not RECORD | `DATA RECORD IS?` | Spec has `DATA {RECORD IS / RECORDS ARE}` — plural form required |

---

## COBOL-85 Optional Keywords (systematic over-lenience — not parse failures)

These are grammar items where spec-required keywords are made optional. Per audit §"Optional where spec requires". They do not cause parse failures but reduce diagnostic ability:

| # | Mismatch | Notes |
|---|----------|-------|
| 39 | `TIMES` in OCCURS made optional | Spec requires it; programs that omit it parse but aren't conforming |
| 40 | `KEY IS` in OCCURS KEY phrase made optional | Both KEY and IS required by spec |
| 41 | `INDEXED BY` — BY made optional | BY required by spec |
| 42 | `DEPENDING ON` — ON made optional | ON required by spec |
| 43 | `BLANK WHEN ZERO` — WHEN made optional | WHEN required by spec |
| 44 | `ASCENDING/DESCENDING KEY` — KEY made optional | KEY is required |

---

## COBOL-2002 (gate behind `is2002()`)

These features were introduced in ISO/IEC 1989:2002. The audit identified them as absent; they should only be accepted when the 2002 dialect flag is set.

| # | Mismatch | Notes |
|---|----------|-------|
| 45 | USAGE BINARY-CHAR [SIGNED/UNSIGNED] | Added in COBOL-2002 with fixed-width integer types |
| 46 | USAGE BINARY-SHORT [SIGNED/UNSIGNED] | Added in COBOL-2002 |
| 47 | USAGE BINARY-LONG [SIGNED/UNSIGNED] | Added in COBOL-2002 |
| 48 | USAGE BINARY-DOUBLE [SIGNED/UNSIGNED] | Added in COBOL-2002 |
| 49 | USAGE FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED | Added in COBOL-2002 |
| 50 | USAGE FLOAT-BINARY-32/64/128 [endianness] | Added in COBOL-2002 |
| 51 | USAGE FLOAT-DECIMAL-16/34 [encoding] [endianness] | Added in COBOL-2002 |
| 52 | USAGE NATIONAL | Added in COBOL-2002 (national character type) |
| 53 | USAGE BIT | Added in COBOL-2002 (boolean storage) |
| 54 | USAGE OBJECT REFERENCE [class/interface] | Added in COBOL-2002 (OO COBOL) |
| 55 | USAGE POINTER [TO type-name] | Added in COBOL-2002 (address pointer) |
| 56 | USAGE FUNCTION-POINTER TO function-prototype | Added in COBOL-2002 |
| 57 | USAGE PROGRAM-POINTER [TO program-prototype] | Added in COBOL-2002 |
| 58 | USAGE MESSAGE-TAG | Added in COBOL-2002 (message facility) |
| 59 | GROUP-USAGE clause (BIT / NATIONAL) | Added in COBOL-2002 §13.18.29 |
| 60 | BASED clause | Added in COBOL-2002 §13.18.5 (heap-allocated data) |
| 61 | ANY LENGTH clause | Added in COBOL-2002 §13.18.2 (parameter any-length) |
| 62 | ALIGNED clause | Added in COBOL-2002 §13.18.1 (bit alignment) |
| 63 | TYPEDEF clause | Added in COBOL-2002 §13.18.58 |
| 64 | SAME AS clause | Added in COBOL-2002 §13.18.49 |

---

## COBOL-2014/2023 (future — do not implement yet)

| # | Mismatch | Notes |
|---|----------|-------|
| — | DYNAMIC LENGTH clause (§13.18.19) | Added in COBOL-2014: dynamic-length elementary items |
| — | CONSTANT RECORD clause (§13.18.15) | Added in COBOL-2014: external shared constant records |
| — | SELECT WHEN clause (§13.18.51) | Added in COBOL-2014: conditional occurrence selection |
| — | PROPERTY clause (§13.18.42) | Added in COBOL-2014: OO property accessors |
| — | OCCURS DYNAMIC [CAPACITY IN name] [FROM n] [TO n] [INITIALIZED] | OCCURS Format 4 — Added in COBOL-2014 §13.18.38 |
| — | VALUE clause Format 2 (FROM subscript TO subscript table init) | COBOL-2002+ table init; low priority |
| — | LOCALE clause in SPECIAL-NAMES | COBOL-2002+ locale support |
| — | ALPHABET FOR NATIONAL phrase | COBOL-2002+ (national character sets) |
| — | ALPHABET LOCALE phrase | COBOL-2002+ |
| — | DYNAMIC LENGTH STRUCTURE clause in SPECIAL-NAMES | COBOL-2014 §13.18.19 |
| — | CLASS FOR NATIONAL phrase | COBOL-2002+ |
| — | PACKED-DECIMAL WITH NO SIGN | COBOL-2002+ variant |

---

## Summary of Action Priority

**Immediate (breaks NIST COBOL-85 programs):** Items 1–12, 26–35 above — these are parse failures or missing clauses that NIST tests exercise directly. Priority order:
1. Items 28–32: INITIALIZE extended forms (blocks several NIST tests)
2. Items 9–11: FD clause parsing (BLOCK CONTAINS, RECORD, CODE-SET as genericClause)
3. Items 2–3: SYMBOLIC CHARACTERS multi-name + IN phrase
4. Items 7: CURRENCY SIGN WITH PICTURE SYMBOL
5. Item 1: SPECIAL-NAMES empty paragraph
6. Items 26–27: VALUE clause IN alphabet / WHEN SET TO FALSE

**Medium priority (systematic keyword leniency):** Items 39–44 — accept over-broad programs silently. Can add diagnostic warnings without changing accept/reject behavior.

**Future (COBOL-2002+):** Items 45–64 — gate behind `is2002()` when implemented. Currently correct to reject or parse via genericClause since dialect is not enabled.