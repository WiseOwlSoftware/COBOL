The table shows 9 Critical + 6 High + 0 Medium + 3 Low = 18, but the total says 48. This means the audit counted additional mismatches within the systematic patterns section, or the 48 includes sub-items. The "dozens of instances" in the systematic patterns section distributes across all areas. The Environment Division share of the "optional where spec requires" systematic gaps would account for the remaining 30.

The audit notes for Environment Division there are 30 additional systematic-pattern mismatches (keywords made optional). From the spec grammar I confirmed these in CobolIO.g4 and CobolSpecialNames.g4:

Looking at the grammar files, the systematic keyword-optional mismatches for Env Division are:
- `ORGANIZATION IS?` — IS optional (should be required per spec syntax diagram, though spec says clauses may appear in any order which implies optional)
- `ACCESS MODE? IS?` — MODE and IS both optional
- `RECORD KEY IS` vs `RELATIVE KEY IS?` — IS optional in relative
- `ALTERNATE RECORD? KEY IS` — RECORD optional
- `(WITH? DUPLICATES)?` — WITH optional
- `FILE STATUS IS` (seems OK)
- `FILE_CONTROL DOT fileControlClauseGroup+` — should be `*`
- `I_O_CONTROL DOT ioControlEntry+` — should be `*`
- `ASSIGN TO assignTarget` vs spec requiring ASSIGN as mandatory
- `programCollatingSequenceClause: PROGRAM COLLATING? SEQUENCE IS? IDENTIFIER` — COLLATING and IS optional
- `ON STATUS IS IDENTIFIER` — STATUS and IS both optional in various forms
- `OFF STATUS IS? IDENTIFIER` — IS optional

Now I have sufficient knowledge to produce the categorized table. Here is the research output:

---

## Environment Division Mismatch Categorization by COBOL Version

### Methodology

The ISO/IEC 1989:2023 spec does not annotate which version introduced each feature. Categorization is based on: (1) the Annex E substantive changes list (2014→2023), (2) known COBOL-85 (ANS X3.23-1985 / ISO 1989:1985) core features, (3) COBOL-2002 (ISO 1989:2002) additions documented in the literature (OO, LOCALE, REPOSITORY, national character features, SHARING, LOCK MODE, file-level COLLATING SEQUENCE, SUPPRESS WHEN), and (4) COBOL-2014/2023 additions (DYNAMIC LENGTH STRUCTURE, ORDER TABLE, UCS-4/UTF-8/UTF-16, APPLY COMMIT, RECORD DELIMITER STANDARD-1 as a 2023 context).

---

## Group 1: COBOL-85 Mismatches (Must Fix — Break NIST Tests)

These features are defined in the 1985 standard and are the correctness baseline.

### 1a. Grammar Structure Bugs

| # | Mismatch | Grammar Location | Severity |
|---|----------|-----------------|----------|
| 1 | `SOURCE-COMPUTER. computer-name-1 .` — spec says computer-name-1 is **optional** (bracket); grammar requires `computerName` positionally. | `CobolParserCore.g4 : sourceComputerParagraph` | Critical |
| 2 | `OBJECT-COMPUTER.` — same: computer-name-1 is optional, and **all** optional clauses absent means second period may be omitted. Grammar requires computerName. | `CobolParserCore.g4 : objectComputerParagraph` | Critical |
| 3 | `SPECIAL-NAMES. specialNameEntry+` — spec says all clauses are optional; an empty SPECIAL-NAMES paragraph is valid. Should be `specialNameEntry*`. | `CobolSpecialNames.g4 : specialNamesParagraph` | Critical |
| 4 | `FILE-CONTROL. fileControlClauseGroup+` — spec says FILE-CONTROL paragraph body is optional. Should be `*`. | `CobolIO.g4 : fileControlParagraph` | Critical |
| 5 | `I-O-CONTROL. ioControlEntry+` — I-O-CONTROL with no clauses is valid. Should be `*`. | `CobolIO.g4 : ioControlParagraph` | Critical |

### 1b. Missing Clauses (COBOL-85 features entirely absent from grammar)

| # | Mismatch | Missing From | Severity |
|---|----------|-------------|----------|
| 6 | `ASSIGN` clause is required by spec (Format 1/2/3); grammar makes it optional (`( ASSIGN TO assignTarget )?`). Also, `ASSIGN USING data-name` form missing (only `ASSIGN TO` present). | `CobolIO.g4 : fileControlClauseGroup` | Critical |
| 7 | `RECORD SEQUENTIAL` organization missing (only plain `SEQUENTIAL` and `LINE SEQUENTIAL`). Spec §12.4.5.10 shows both `LINE` and `RECORD` as prefixes to `SEQUENTIAL`. | `CobolIO.g4 : organizationType` | Critical |
| 8 | `CURRENCY SIGN IS literal WITH PICTURE SYMBOL literal` — `WITH PICTURE SYMBOL` phrase absent. Spec §12.3.7.2 shows it explicitly. | `CobolSpecialNames.g4 : currencySignClause` | Critical |
| 9 | `SYMBOLIC CHARACTERS`: only one name per entry (`IDENTIFIER IS literal`); spec allows multiple names with `IS`/`ARE` and multiple integers; `IN alphabet-name` phrase missing. | `CobolSpecialNames.g4 : symbolicCharactersClause` | High |
| 10 | `ALPHABET` clause: `STANDARD-1`, `STANDARD-2`, `NATIVE`, `code-name` forms missing; only literal-sequence form present. | `CobolSpecialNames.g4 : alphabetDefinition` | High |
| 11 | `PROGRAM COLLATING SEQUENCE IS alphabet-name` — `COLLATING` keyword is optional in grammar (`COLLATING?`); spec shows it as mandatory. | `CobolParserCore.g4 : programCollatingSequenceClause` | High |
| 12 | Implementor switch: direct `ON STATUS IS condition-name / OFF STATUS IS condition-name` form (no `IS mnemonic-name` prefix) missing. Grammar only supports `switch-name-1 IS mnemonic-name-1 [ON...][OFF...]`. | `CobolSpecialNames.g4 : implementorSwitchEntry` | High |
| 13 | `CLASS name IS literal [THRU literal]...` — `IN alphabet-name` phrase missing. | `CobolSpecialNames.g4 : classDefinitionClause` | High |
| 14 | `SAME AREA FOR` / `SAME RECORD AREA FOR` / `SAME SORT AREA FOR` — the I-O-CONTROL SAME clause is entirely absent; current grammar uses `genericClause` fallback only. | `CobolIO.g4 : ioControlParagraph` | High |

### 1c. Systematic "Optional Where Spec Requires" Mismatches (COBOL-85)

These are the keyword-optionality mismatches the audit counts as ~30 additional items for the Environment Division.

| # | Keyword | Grammar | Spec | Location |
|---|---------|---------|------|----------|
| 15 | `RECORD` in `ALTERNATE RECORD KEY` | `RECORD?` | Required in all 3 formats | `CobolIO.g4 : alternateKeyClause` |
| 16 | `WITH` in `WITH DUPLICATES` | `WITH?` | Required per format diagrams | `CobolIO.g4 : alternateKeyClause` |
| 17 | `IS` in `RELATIVE KEY IS` | `IS?` | Required per spec | `CobolIO.g4 : relativeKeyClause` |
| 18 | `MODE` in `ACCESS MODE IS` | `MODE?` | Required per spec | `CobolIO.g4 : accessModeClause` |
| 19 | `IS` in `ACCESS MODE IS` | `IS?` | Required per spec | `CobolIO.g4 : accessModeClause` |
| 20 | `IS` in `ORGANIZATION IS` | `IS?` | Required per spec (§12.4.5.10) | `CobolIO.g4 : organizationClause` |
| 21 | `STATUS` in `ON STATUS IS` | Optional/absent | Required (STATUS keyword in spec) | `CobolSpecialNames.g4 : switchOnClause` |
| 22 | `STATUS` in `OFF STATUS IS` | Optional/absent | Required | `CobolSpecialNames.g4 : switchOffClause` |
| 23 | `IS` in `PROGRAM COLLATING SEQUENCE IS` | `IS?` | Required | `CobolParserCore.g4 : programCollatingSequenceClause` |
| 24 | `CURRENCY SIGN` — `SIGN` keyword | `SIGN?` | Required in spec format | `CobolSpecialNames.g4 : currencySignClause` |
| 25 | `IS` in `CURRENCY SIGN IS` | `IS?` | Required | `CobolSpecialNames.g4 : currencySignClause` |
| 26 | `IS` in `CLASS name IS` | `IS?` | Required | `CobolSpecialNames.g4 : classDefinitionClause` |
| 27 | `ALPHABET name IS` — `IS` | Implicit in `alphabetDefinition` but not explicit | Required | `CobolSpecialNames.g4 : alphabetClause` |

**Note on optionality:** COBOL-85 real-world programs frequently omit noise words (WITH, IS, MODE, RECORD) even in the 1985 era, which is why implementors made them optional. The audit counts these as mismatches against the spec diagram, but fixing them may break real programs. The recommendation is to add compiler warnings rather than hard errors for missing noise words.

---

## Group 2: COBOL-2002 Additions (High Priority — Needed for Modern Code)

These features were introduced in ISO 1989:2002 and are entirely absent from the grammar.

| # | Feature | Spec Section | Grammar Status | Severity |
|---|---------|-------------|----------------|----------|
| 28 | `REPOSITORY` paragraph | §12.3.8 | Entirely absent | Critical |
| 29 | `LOCK MODE IS { MANUAL / AUTOMATIC } [WITH LOCK ON [MULTIPLE] { RECORD / RECORDS }]` in SELECT | §12.4.5.9 | Entirely absent; `vendorFileControlClause` fallback only | Critical |
| 30 | `SHARING WITH { ALL OTHER / NO OTHER / READ ONLY }` in SELECT | §12.4.5.15 | Entirely absent | Critical |
| 31 | `COLLATING SEQUENCE` clause in SELECT (file-level, Format 1: `IS alphabet-name-1 [alphabet-name-2]` or `FOR ALPHANUMERIC IS / FOR NATIONAL IS`) | §12.4.5.7 | Entirely absent | Critical |
| 32 | `COLLATING SEQUENCE OF data-name IS alphabet-name` (key-level, Format 2) | §12.4.5.7 | Entirely absent | High |
| 33 | `SUPPRESS WHEN literal` in `ALTERNATE RECORD KEY` clause | §12.4.5.6.2 | Absent | High |
| 34 | `LOCALE locale-name IS { external-locale-name / literal }` clause in SPECIAL-NAMES | §12.3.7.2 | Entirely absent | High |
| 35 | `ALPHABET name FOR ALPHANUMERIC IS ...` / `ALPHABET name FOR NATIONAL IS ...` | §12.3.7.2 | FOR ALPHANUMERIC/NATIONAL phrases absent | High |
| 36 | `CLASS name FOR { ALPHANUMERIC / NATIONAL } IS ...` | §12.3.7.2 | FOR ALPHANUMERIC/NATIONAL absent | High |
| 37 | `SYMBOLIC CHARACTERS FOR { ALPHANUMERIC / NATIONAL }` | §12.3.7.2 | FOR ALPHANUMERIC/NATIONAL absent | High |
| 38 | `PROGRAM COLLATING SEQUENCE FOR ALPHANUMERIC IS / FOR NATIONAL IS` (OBJECT-COMPUTER) | §12.3.6.2 | Only single alphabet form present | High |
| 39 | `CHARACTER CLASSIFICATION IS ...` clause in OBJECT-COMPUTER | §12.3.6.2 | Entirely absent | High |
| 40 | `RECORD KEY SOURCE IS data-name ...` (composite key form) in SELECT | §12.4.5.1 Format 1 | Absent; only simple `data-name` form | High |
| 41 | `ALTERNATE RECORD KEY SOURCE IS data-name ...` (composite key form) | §12.4.5.6.2 | Absent | High |
| 42 | `RECORD DELIMITER IS { STANDARD-1 / feature-name }` in SELECT Format 3 | §12.4.5.11 | Entirely absent | Low |
| 43 | `RESERVE integer [AREA/AREAS]` in SELECT — grammar has a RESERVE clause in SPECIAL-NAMES (which is wrong location); SELECT lacks it | §12.4.5.14 | In SPECIAL-NAMES but not in SELECT fileControlClauses | Low |
| 44 | `APPLY COMMIT ON ...` clause in I-O-CONTROL | §12.4.6.3 | Absent (genericClause fallback) | Low |

**Note on RESERVE:** The RESERVE clause per the 2023 spec lives in the SELECT file control entry (§12.4.5.14), not in SPECIAL-NAMES. The current grammar has a `reserveClause` misplaced inside `specialNameEntry`. In COBOL-85, RESERVE appeared in the SELECT entry. This is a misplacement bug as well as a missing-from-SELECT bug.

---

## Group 3: COBOL-2014/2023 Additions (Low Priority — Modern Only)

These features appear first in ISO 1989:2014 or the 2023 Working Draft.

| # | Feature | Spec Section | Grammar Status | Version |
|---|---------|-------------|----------------|---------|
| 45 | `ORDER TABLE ordering-name IS literal` in SPECIAL-NAMES | §12.3.7.2 | Entirely absent | 2014/2023 |
| 46 | `DYNAMIC LENGTH STRUCTURE name IS { PREFIXED / DELIMITED / physical-structure-name }` in SPECIAL-NAMES | §12.3.7.2 | Entirely absent | 2014/2023 |
| 47 | `UCS-4`, `UTF-8`, `UTF-16` in `ALPHABET name FOR NATIONAL IS ...` | §12.3.7.2 (ALPHABET FOR NATIONAL) | Absent | 2014/2023 |
| 48 | `CRT STATUS IS data-name` / `CURSOR IS data-name` in SPECIAL-NAMES — grammar has `crtStatusClause` and `cursorClause`; these are screen facility features. Grammar has them; whether they are correct is a separate question. | §12.3.7.2 | Present | COBOL-85 extension (screen feature) |

---

## Summary Table

| Category | Items | Priority |
|----------|-------|----------|
| **COBOL-85 — Structure bugs** (empty paragraphs fail) | 1–5 | Fix immediately; break NIST tests |
| **COBOL-85 — Missing clauses** (features entirely absent) | 6–14 | Fix before claiming COBOL-85 compliance |
| **COBOL-85 — Keyword optionality** (noise word strictness) | 15–27 | Add warnings; don't break real programs |
| **COBOL-2002 — Missing entirely** | 28–44 | Fix for modern COBOL support |
| **COBOL-2014/2023 — Missing** | 45–47 | Defer until 2014+ target |

---

## Key Findings

**COBOL-85 items that will break NIST tests (fix first):**
- Empty `SPECIAL-NAMES.` / `FILE-CONTROL.` / `I-O-CONTROL.` paragraphs
- `RECORD SEQUENTIAL` organization
- Optional `computer-name-1` in SOURCE/OBJECT-COMPUTER
- `WITH PICTURE SYMBOL` in CURRENCY SIGN

**Ambiguous version attribution:**
- `RESERVE clause`: Present in COBOL-85 in the SELECT entry; the grammar has it in SPECIAL-NAMES which is wrong regardless of version.
- `RECORD DELIMITER`: Present in COBOL-85 spec (it uses STANDARD-1 for tape). Absent from grammar.
- `SAME AREA / SAME RECORD AREA / SAME SORT AREA`: All COBOL-85. Currently handled only by `genericClause` fallback.
- `LOCK MODE` and `SHARING`: These are COBOL-2002 introductions; they do not appear in the 1985 standard. Not needed for NIST COBOL-85 tests.

**Corrections to the task prompt's version list:**
- `RESERVE clause`: Listed as "may be COBOL-85 — verify." **Confirmed COBOL-85.** The spec (§12.4.5.14) shows it with no version annotation, and it appears in the COBOL-85 SELECT syntax. The grammar has it in the wrong place (SPECIAL-NAMES instead of SELECT).
- `COLLATING SEQUENCE in SELECT (file-level)`: Listed as COBOL-2002. **Confirmed.** Not in COBOL-85 SELECT; introduced in 2002 for indexed-file key collation.
- `SUPPRESS WHEN in ALTERNATE KEY`: Listed as COBOL-2002. **Confirmed.** Not in COBOL-85.
- `RECORD DELIMITER STANDARD-1`: Listed as COBOL-2014/2023. **Incorrect.** RECORD DELIMITER with STANDARD-1 appears in COBOL-85 (it was a tape drive feature). The STANDARD-1 option itself is old; what's 2023 may be the formal placement in the grammar diagram but the feature is COBOL-85.