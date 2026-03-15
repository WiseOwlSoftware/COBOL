# COBOL-85 Full Compliance Plan

Goal: Pass all NIST COBOL-85 test suite modules. Priority: get basic functionality
working first (all math, display, moves), then control flow, then I/O, then advanced.

## NIST Test Suite Modules (391 programs total)

| Module | Programs | Description |
|--------|----------|-------------|
| NC | 95 | Nucleus (core language) |
| SQ | 84 | Sequential I/O |
| IX | 29 | Indexed I/O |
| RL | 26 | Relative I/O |
| ST | 25 | SORT/MERGE |
| SM | 13 | Source manipulation (COPY/REPLACE) |
| IC | 25 | Inter-program communication (CALL) |
| IF | 45 | Intrinsic Functions |
| SG | 13 | Segmentation |
| RW | 6 | Report Writer |
| DB | 15 | Debugging |
| CM | 9 | Communication |
| OB | 5 | Obsolete features |

---

## Phase A: Core Arithmetic (blocks ~40 NC tests)

All arithmetic must work with ON SIZE ERROR, ROUNDED, multi-target, and all formats.

### A1: SUBTRACT (Format 1 + 2 + 3)
- **Needed:** IrPicSubtract + IrPicSubtractLiteral, LowerSubtract with ON SIZE ERROR
- **Pattern:** Clone from LowerMultiply (multi-target, statement-level ArithmeticStatus)
- **Tests:** NC106A (F1, SIZE ERROR, ROUNDED), NC119A (F1+F2, SIGN), NC175A (F2, SIZE ERROR), NC253A (F3), NC111A (truncation), NC112A (multi-operand)

### A2: DIVIDE (Format 1 + 2 + 3 + 4 + 5)
- **Needed:** IrPicDivide + IrPicDivideLiteral, LowerDivide with ON SIZE ERROR, REMAINDER
- **DIVIDE has 5 formats** — INTO, BY, GIVING, REMAINDER, GIVING REMAINDER
- **Tests:** NC117A (SIGN), NC171A (F1), NC172A (F2), NC173A (F3), NC203A (F4), NC251A (F5)

### A3: ADD ON SIZE ERROR
- **Needed:** LowerAdd conditional blocks (same pattern as LowerMultiply)
- **Tests:** NC118A (F1+F2, SIGN), NC176A (F1, SIZE ERROR), NC177A (F2, SIZE ERROR), NC202A (F3)

### A4: COMPUTE
- **Needed:** BoundComputeStatement, expression tree lowering to arithmetic IR
- **Expressions:** +, -, *, /, ** with proper precedence and parentheses
- **Must support:** ON SIZE ERROR, ROUNDED, multi-target
- **Tests:** Used pervasively; no dedicated NC test but required by many

---

## Phase B: Core Data Movement + Conditions (blocks ~25 NC tests)

### B1: MOVE — complete all formats
- **Needed:** Verify all MOVE Format 1 paths (numeric, alphanumeric, edited, group)
- **MOVE Format 2:** MOVE CORRESPONDING
- **Tests:** NC104A, NC105A, NC209A, NC112A (multi-operand)

### B2: Figurative Constants — complete set
- **Needed:** HIGH-VALUE, LOW-VALUE, ALL literal in comparisons and MOVEs
- **Current:** ZERO, SPACE, QUOTE work. HIGH-VALUE/LOW-VALUE may need byte-level handling
- **Tests:** NC107A, NC219A

### B3: SIGN clause
- **Needed:** LEADING/TRAILING SEPARATE sign storage
- **Current:** Only leading separate implemented; trailing embedded, trailing separate needed
- **Tests:** NC116A, NC117A, NC118A, NC119A, NC120A

### B4: Numeric editing (full)
- **Needed:** Verify all PIC editing symbols: Z, *, +, -, $, B, 0, /, CR, DB, P
- **Tests:** NC114M, NC124A, NC125A

### B5: IF conditions — complete
- **Needed:**
  - Class conditions (NUMERIC, ALPHABETIC, ALPHABETIC-LOWER/UPPER)
  - Abbreviated combined relations (`A > B AND < C`)
  - Condition-names (level 88)
  - Switch settings
  - Nested IF (up to 22 levels per NC210A)
- **Tests:** NC103A, NC174A, NC210A, NC211A, NC250A, NC254A

### B6: NEXT SENTENCE
- **Needed:** Jump to the next period-terminated sentence
- **Tests:** NC103A

---

## Phase C: Control Flow (blocks ~15 NC tests)

### C1: EVALUATE — full implementation
- **Needed:** BoundEvaluateStatement, WHEN/WHEN OTHER/WHEN THRU, subject matching
- **Subjects:** identifiers, literals, TRUE, FALSE, arithmetic expressions
- **Tests:** NC225A

### C2: PERFORM VARYING / UNTIL
- **Needed:** VARYING FROM BY UNTIL, TEST BEFORE/AFTER, nested VARYING (AFTER clause)
- **PERFORM Format 4:** VARYING with multiple AFTER clauses
- **Tests:** NC102A, NC201A, NC241A, NC242A, NC243A

### C3: GO TO DEPENDING ON
- **Needed:** Already implemented (CIL switch). Verify with tests.
- **Tests:** NC102A

### C4: ALTER
- **Needed:** Runtime GO TO indirection (obsolete but tested)
- **Tests:** NC303M

---

## Phase D: Tables + Subscripting + Indexing (blocks ~25 NC tests)

### D1: OCCURS clause — verify all forms
- **Needed:** Fixed OCCURS, OCCURS DEPENDING ON (variable length)
- **Tests:** NC247A

### D2: Subscripting — all dimensions
- **Needed:** 1D, 2D, 3D, 7D table access with integer/data-name subscripts
- **Tests:** NC132A, NC134A, NC136A, NC206A, NC240A, NC243A, NC246A

### D3: Index-names + SET
- **Needed:** SET Format 1 (index to value), SET Format 2 (UP BY/DOWN BY), relative indexing
- **Tests:** NC131A, NC133A, NC135A, NC137A, NC139A, NC140A, NC141A, NC244A

### D4: SEARCH Format 1 (serial)
- **Needed:** Already implemented. Verify with multi-dimensional tables, VARYING, AT END
- **Tests:** NC231A, NC232A, NC234A, NC235A, NC236A, NC238A

### D5: SEARCH ALL (Format 2 — binary search)
- **Needed:** Binary search with ASCENDING/DESCENDING KEY
- **Tests:** NC233A, NC237A, NC238A

### D6: Qualification (IN/OF)
- **Needed:** Data-name qualification for ambiguous names (up to 5 levels)
- **Tests:** NC206A, NC207A, NC208A

### D7: Reference modification
- **Needed:** Already implemented. Verify edge cases.
- **Tests:** NC224A

---

## Phase E: String Handling (blocks ~5 NC tests)

### E1: INSPECT — all formats
- **Needed:** Verify TALLYING, REPLACING, CONVERTING, CHARACTERS, ALL/LEADING/FIRST, BEFORE/AFTER
- **Tests:** NC115A, NC122A, NC216A, NC221A

### E2: STRING — verify
- **Needed:** Already implemented. Verify POINTER, OVERFLOW, NOT OVERFLOW, END-STRING
- **Tests:** NC217A

### E3: UNSTRING — verify
- **Needed:** Already implemented. Verify POINTER, TALLYING, OVERFLOW, END-UNSTRING
- **Tests:** NC218A

### E4: INITIALIZE
- **Needed:** Already implemented. Verify with qualified/subscripted items
- **Tests:** NC223A

---

## Phase F: Sequential I/O (84 SQ tests)

### F1: Full sequential file operations
- **Needed:** All OPEN modes (INPUT, OUTPUT, I-O, EXTEND), REWRITE, FILE STATUS codes
- **Tests:** SQ101A through SQ401M

### F2: WRITE BEFORE/AFTER ADVANCING
- **Needed:** PAGE advancing, BEFORE ADVANCING, integer/mnemonic advancing
- **Current:** AFTER ADVANCING 1 works; need full implementation

### F3: Variable-length records
- **Needed:** RECORD CONTAINS min TO max, RECORD IS VARYING

---

## Phase G: Source Manipulation (13 SM tests)

### G1: COPY / REPLACE — verify
- **Needed:** Already implemented. Verify nested COPY, REPLACING, pseudo-text
- **Tests:** SM101A through SM401M

---

## Phase H: Inter-Program Communication (25 IC tests)

### H1: CALL with parameters
- **Needed:** BY REFERENCE, BY CONTENT, BY VALUE, RETURNING
- **Linkage section** binding to caller's data
- **Tests:** IC101A through IC401M

### H2: CANCEL
- **Needed:** Already no-op. May need to reset program state.

---

## Phase I: Indexed + Relative I/O (55 tests)

### I1: Indexed file I/O
- **Needed:** Full IFileHandler.Indexed: READ KEY, START, WRITE, REWRITE, DELETE
- **Tests:** IX101A through IX401M (29 tests)

### I2: Relative file I/O
- **Needed:** Full IFileHandler.Relative: RELATIVE KEY, random/dynamic access
- **Tests:** RL101A through RL401M (26 tests)

---

## Phase J: SORT/MERGE (25 ST tests)

### J1: SORT USING/GIVING
- **Needed:** Temp file management, merge sort, multi-key sorting
- **Tests:** ST101A through ST401M

### J2: MERGE
- **Needed:** Multiple input files, single output

---

## Phase K: Intrinsic Functions (45 IF tests)

### K1: Verify all 60+ functions
- **Already implemented** in IntrinsicFunctions.cs. Need to run IF tests to verify.
- **Tests:** IF101A through IF402M

---

## Phase L: Optional/Advanced (34 tests)

### L1: Segmentation (SG, 13 tests) — low priority
### L2: Report Writer (RW, 6 tests) — low priority
### L3: Debugging (DB, 15 tests) — low priority
### L4: Communication (CM, 9 tests) — may skip (obsolete in 2023)

---

## Implementation Order Summary

| Priority | Phase | What | Tests Unblocked |
|----------|-------|------|-----------------|
| **1** | A1-A4 | All arithmetic (SUBTRACT, DIVIDE, COMPUTE, ADD SIZE ERROR) | ~40 NC |
| **2** | B1-B6 | Data movement, conditions, SIGN, editing, figurative constants | ~25 NC |
| **3** | C1-C4 | EVALUATE, PERFORM VARYING/UNTIL, ALTER | ~15 NC |
| **4** | D1-D7 | Tables, subscripting, indexing, SEARCH ALL, qualification | ~25 NC |
| **5** | E1-E4 | String handling verification | ~5 NC |
| **6** | F1-F3 | Sequential I/O (full) | 84 SQ |
| **7** | G1 | COPY/REPLACE verification | 13 SM |
| **8** | H1-H2 | CALL with parameters | 25 IC |
| **9** | K1 | Intrinsic functions verification | 45 IF |
| **10** | I1-I2 | Indexed + Relative I/O | 55 IX+RL |
| **11** | J1-J2 | SORT/MERGE | 25 ST |
| **12** | L1-L4 | Optional modules | 34 SG+RW+DB+CM |

**Estimated test coverage after each phase:**
- After Phase A: NC101A + ~40 more NC tests
- After Phase D: Most of NC module (~80/95)
- After Phase F: NC + SQ modules (~180/391)
- After Phase K: NC + SQ + SM + IF (~320/391)
- After Phase J: Full suite (~391/391)
