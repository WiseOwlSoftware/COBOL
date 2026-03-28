# CobolSharp Grammar-vs-Spec Audit

**Date:** 2026-03-28 (updated with version categorization)
**Method:** 10 parallel agents (5 initial audit + 5 version categorization) compared every
.g4 grammar rule against ISO syntax diagrams, token by token, then categorized each mismatch
by the COBOL specification version that introduced the feature.
**Scope:** All 14 grammar files (3,225 lines) against the full ISO spec (specs/ISO_COBOL.md)

This audit was performed AFTER the initial feature-presence audit (SPEC_COMPLIANCE_AUDIT.md)
revealed that grammar completeness gaps were being missed. This audit compares syntax diagrams
character by character, not just feature presence.

---

## Version-Categorized Summary

All mismatches categorized by the COBOL spec version that introduced the feature.
Version attribution verified against ISO Annex E (substantive changes) and historical specs.

### COBOL-85 Grammar Gaps (must fix for compliance)

| Area | Count | Key Items |
|------|------:|-----------|
| Data Division | 38 | FD clauses (BLOCK CONTAINS, RECORD, CODE-SET) as genericClause; INITIALIZE extended forms; SYMBOLIC CHARACTERS multi-name; CURRENCY WITH PICTURE SYMBOL; VALUE IN alphabet/WHEN SET TO FALSE |
| Procedure Division | 27 | DISPLAY UPON/NO ADVANCING; CORR synonym; SET TO ON/OFF; START FIRST/LAST; SORT table format; FROM literal on WRITE/REWRITE/RELEASE; STOP WITH STATUS |
| Environment Division | 27 | Empty paragraph failures (5); RECORD SEQUENTIAL; ASSIGN required+USING; SAME AREA clause; keyword optionality (13 items) |
| Expressions/Conditions | 1 | Chained exponentiation (A**B**C) |
| Lexer tokens | 45 | UPON, CORR, CONTAINS, SAME, CONFIGURATION, INPUT-OUTPUT, NATIVE, STANDARD-1, STANDARD-2, CODE-SET, LINAGE-COUNTER + Report Writer tokens |
| **Total COBOL-85** | **~138** | |

### COBOL-2002 Grammar Gaps (gate behind is2002())

| Area | Count | Key Items |
|------|------:|-----------|
| Data Division | 20 | 15 USAGE alternatives; GROUP-USAGE; BASED; ALIGNED; TYPEDEF; SAME AS |
| Procedure Division | 15 | ROUNDED MODE IS; retry-phrase; SHARING; LOCK; PERFORM UNTIL EXIT; BY VALUE/RETURNING on CALL |
| Environment Division | 17 | REPOSITORY; LOCK MODE; SHARING; file COLLATING SEQUENCE; LOCALE; FOR NATIONAL on ALPHABET/CLASS/SYMBOLIC |
| Expressions/Conditions | 10 | General arithmetic subscripts; floating-point literals; IS OMITTED; boolean/national literals; concatenation (&); boolean expressions (B-AND etc.) |
| Lexer tokens | 60 | Boolean/national/float type tokens; OO tokens; transaction tokens; exception management |
| **Total COBOL-2002** | **~122** | |

### COBOL-2014/2023 Grammar Gaps (future)

| Area | Count | Key Items |
|------|------:|-----------|
| Data Division | 12 | DYNAMIC LENGTH; CONSTANT RECORD; SELECT WHEN; OCCURS DYNAMIC |
| Procedure Division | 8 | XOR/EXCLUSIVE-OR; CONTINUE AFTER SECONDS; PERFORM Format 3; INSPECT BACKWARD; GOBACK WITH STATUS |
| Environment Division | 4 | ORDER TABLE; DYNAMIC LENGTH STRUCTURE; UCS-4/UTF-8/UTF-16 |
| Expressions/Conditions | 5 | XOR; B-SHIFT-* operators; FARTHEST-FROM-ZERO; NEAREST-TO-ZERO; IN-ARITHMETIC-RANGE |
| Lexer tokens | 13 | 2014/2023 reserved words |
| **Total COBOL-2014/2023** | **~42** | |

### Key Version Corrections (from Annex E analysis)

Several features were initially miscategorized. Corrections based on ISO Annex E:
- **XOR/EXCLUSIVE-OR**: COBOL-2023 (NOT 2002 as initially assumed)
- **PERFORM UNTIL EXIT**: COBOL-2023 (NOT 2002)
- **CONTINUE AFTER SECONDS**: COBOL-2023 (NOT 2002)
- **INSPECT BACKWARD**: COBOL-2023 (NOT 2014)
- **GOBACK WITH STATUS**: COBOL-2023 (STOP RUN WITH STATUS is 2002)
- **RECORD DELIMITER STANDARD-1**: COBOL-85 (NOT 2014/2023)
- **RESERVE clause**: COBOL-85, but misplaced in grammar (SPECIAL-NAMES instead of SELECT)
- **IS <>**: COBOL-2002 (but treat as practical COBOL-85 target since ubiquitous)

---

## Detailed Findings (original audit)

| Area | Agent | Critical | High | Medium | Low | Total |
|------|-------|:--------:|:----:|:------:|:---:|------:|
| Data Division | 1 | 10 | 15 | 20 | 19 | 64 |
| Procedure Division (38 stmts) | 2 | 22 | 4 | 0 | 0 | 26+ |
| Environment Division | 3 | 9 | 6 | 0 | 3 | 48 |
| Expressions/Conditions | 4 | 6 | 12 | 4 | 10 | 32 |
| Lexer tokens | 5 | ~10 | ~20 | ~30 | ~50 | ~110 |

**Cross-statement systematic gaps:**
- ROUNDED MODE IS: missing from all 5 arithmetic statements
- retry-phrase: missing from OPEN, READ, WRITE, REWRITE, DELETE
- CORR abbreviation: missing from ADD, SUBTRACT, MOVE
- IS/WITH/ON optional where spec requires them: dozens of instances
- FROM literal: WRITE, REWRITE, RELEASE accept only dataReference, not literal

---

## Critical Gaps (will cause parse failures on valid COBOL-85 programs)

### Grammar Structure
1. SOURCE-COMPUTER/OBJECT-COMPUTER: computer-name not optional (empty paragraph fails)
2. SPECIAL-NAMES: `specialNameEntry+` should be `*` (empty paragraph fails)
3. FILE-CONTROL: `fileControlClauseGroup+` should be `*` (empty fails)
4. I-O-CONTROL: entries required but should be optional

### Missing Clauses
5. ASSIGN clause optional in grammar but required in spec; USING form missing
6. RECORD SEQUENTIAL organization missing (only plain SEQUENTIAL)
7. LOCK MODE clause missing from SELECT
8. SHARING clause missing from SELECT and OPEN
9. COLLATING SEQUENCE clause missing from SELECT
10. BLOCK CONTAINS, RECORD, CODE-SET clauses in FD: genericClause only
11. WITH PICTURE SYMBOL in CURRENCY SIGN: missing

### Statement Gaps
12. DISPLAY: UPON mnemonic-name, WITH NO ADVANCING, END-DISPLAY all missing
13. ACCEPT: FROM mnemonic-name, screen format, END-ACCEPT missing
14. INITIALIZE: WITH FILLER, ALL TO VALUE, THEN TO DEFAULT, THEN REPLACING all missing
15. SET: TO ON/OFF (switch-setting), 8+ formats entirely absent
16. SORT: table sort (Format 2) absent; WITH DUPLICATES malformed
17. START: FIRST/LAST missing; KEY structure wrong; WITH LENGTH absent
18. STOP: WITH ERROR/NORMAL STATUS missing
19. EXIT PROGRAM/GOBACK: RAISING phrase missing
20. USE: GLOBAL, EXCEPTION synonym, INPUT/OUTPUT/I-O/EXTEND modes missing
21. WRITE/REWRITE: FILE form, literal in FROM, retry-phrase, locking missing
22. READ: RECORD keyword, locking phrases, retry-phrase missing

### Expression Gaps
23. XOR / EXCLUSIVE-OR logical operator: missing from lexer, grammar, precedence
24. Chained exponentiation (A ** B ** C): `?` should be `*`
25. General arithmetic in subscripts: only restricted forms supported
26. IS <> operator form missing
27. Floating-point literals (1.5E+3): no lexer token
28. IS OMITTED condition: not in any condition rule

---

## High-Priority Gaps

### Missing Literal Types
- Boolean literals B"..." / BX"..."
- National literals N"..." / NX"..."
- Zero-length hex literals X""
- Floating-point numeric literals

### Missing Class Condition Forms
- BOOLEAN, FARTHEST-FROM-ZERO, FLOAT-INFINITY, FLOAT-NOT-A-NUMBER (+ QUIET/SIGNALING),
  IN-ARITHMETIC-RANGE, NEAREST-TO-ZERO (all COBOL-2002+)

### Missing Expression Types
- Concatenation expression (& operator)
- Boolean expression (B-AND, B-OR, B-XOR, B-NOT, B-SHIFT-*)

### USAGE Clause
- 15 of 19 spec-defined usage alternatives missing (only BINARY, COMP, DISPLAY,
  PACKED-DECIMAL, INDEX present). Missing: BINARY-CHAR/SHORT/LONG/DOUBLE, all FLOAT-*,
  NATIONAL, BIT, OBJECT REFERENCE, POINTER, MESSAGE-TAG, FUNCTION-POINTER, PROGRAM-POINTER

### Data Description Clauses
- 9 clauses entirely absent: ALIGNED, ANY LENGTH, BASED, CONSTANT RECORD, DYNAMIC LENGTH,
  GROUP-USAGE, PROPERTY, SAME AS, SELECT WHEN

### VALUE Clause
- WHEN SET TO FALSE IS literal-4: missing
- IN alphabet-name-1: missing
- Format 2 (table format with FROM/TO subscripts): missing

### SPECIAL-NAMES
- SYMBOLIC CHARACTERS: no multi-name, no ARE, no IN alphabet-name
- ALPHABET: FOR NATIONAL/FOR ALPHANUMERIC missing; LOCALE handling broken
- CLASS: FOR NATIONAL missing; IN alphabet-name missing
- LOCALE clause: entirely absent
- Implementor switch: direct ON/OFF STATUS form (no IS mnemonic) missing

---

## Systematic Patterns

### "Optional where spec requires" (dozens of instances)
The grammar makes many spec-required keywords optional. While this makes the parser
more lenient (accepting non-conforming programs), it means the compiler cannot validate
that programs use correct syntax. Examples:
- IS after LINAGE, GLOBAL, EXTERNAL, ACCESS MODE, RECORD KEY, FILE STATUS
- WHEN in BLANK WHEN ZERO
- RIGHT in JUSTIFIED RIGHT
- TIMES in OCCURS
- BY in INDEXED BY
- ON in DEPENDING ON, ON ASCENDING
- KEY in ASCENDING KEY, DESCENDING KEY
- STATUS in ON STATUS IS, OFF STATUS IS
- WITH in WITH DUPLICATES, WITH NO REWIND
- RECORD in ALTERNATE RECORD KEY

### Missing from all I/O statements
- retry-phrase (RETRY FOREVER | RETRY n TIMES | RETRY FOR n SECONDS)
- WITH LOCK / WITH NO LOCK record locking
- FILE file-name-1 alternative (WRITE FILE, REWRITE FILE, READ FILE)

### Missing from all arithmetic statements
- ROUNDED MODE IS (TRUNCATION | NEAREST-TOWARD-ZERO | NEAREST-EVEN | ...)

---

## Lexer Gaps

### Missing Reserved Words (~110)
Many are for unimplemented features (OO, Report Writer, message facility, boolean/national
types, float usage types). Key COBOL-85 words missing as tokens: CONFIGURATION, INPUT-OUTPUT,
BLOCK, CODE-SET, CONTAINS, UPON, SAME, NATIVE, STANDARD-1, STANDARD-2, CORR, COMMA (as keyword).

### Extra Tokens (~20)
IBM extensions (COMP-1/2/3/5, COMPUTATIONAL-1/2/3/5), archaic words (DATE-WRITTEN,
DATE-COMPILED, AUTHOR, INSTALLATION, SECURITY, REMARKS, PROCEED, GOBACK), context-sensitive
words promoted to reserved (CYCLE, PREVIOUS, RECURSIVE, PARAGRAPH, YYYYMMDD, YYYYDDD).

### Figurative Constants
All 6 standard formats covered. ALL symbolic-character-1 (Format 7) missing.

### Separators
Mostly correct. Missing: `::` (OO invocation), `==` (pseudo-text delimiter, handled in
preprocessor). Semicolons slightly over-permissive (skipped without requiring trailing space).

---

## Recommendations

### Phase 1: Fix COBOL-85 grammar compliance (breaks NIST tests)
1. Make computer-name optional in SOURCE/OBJECT-COMPUTER
2. Allow empty SPECIAL-NAMES, FILE-CONTROL, I-O-CONTROL
3. Add RECORD SEQUENTIAL organization
4. Fix ASSIGN to be required + add USING form
5. Add CORR as synonym for CORRESPONDING
6. Fix INITIALIZE (WITH FILLER, TO VALUE, THEN TO DEFAULT)
7. Add DISPLAY UPON, WITH NO ADVANCING
8. Fix START (FIRST/LAST, KEY structure)
9. Fix STOP (WITH STATUS)
10. Add retry-phrase to I/O statements
11. Add FROM literal to WRITE/REWRITE/RELEASE
12. Fix chained exponentiation (? to *)

### Phase 2: Fix spec-required-keywords-made-optional
- Systematic pass to change `IS?` → `IS` etc. where spec requires the keyword
- Add compiler warnings for missing noise words (not errors — too many real programs omit them)

### Phase 3: COBOL-2002+ grammar additions
- XOR/EXCLUSIVE-OR, boolean expressions, concatenation expressions
- Float literals, boolean literals, national literals
- USAGE alternatives (BINARY-CHAR/SHORT/LONG/DOUBLE, FLOAT-*, etc.)
- Additional data description clauses
- ROUNDED MODE IS

### Phase 4: Completeness
- ~110 missing reserved words (add as tokens, even if features unimplemented)
- I/O locking (WITH LOCK/NO LOCK, LOCK MODE, SHARING)
- Report Writer grammar (beyond stubs)
