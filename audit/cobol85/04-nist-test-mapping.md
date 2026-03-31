# NIST Test Mapping

**Date:** 2026-03-30 | **Mode:** DRY-RUN (read-only) | **Auditor:** Claude Opus 4.6
**Evidence sources:** NIST baselines in `tests/nist/valid/`, NIST programs in `tests/nist/programs/`, `audit/cobol85/source-snapshot.txt`

---

## NC-Series (95 programs, 95 baselines, all compile+run, 77 FAIL* locked)

| Test ID | Focus Area | Binders/Runtime | FAIL* | Notes |
|---------|-----------|-----------------|:-----:|-------|
| NC101A | MULTIPLY Format 1, ROUNDED | Arithmetic, PicRuntime | 0 | |
| NC102A | PERFORM Formats 1-3 | ControlFlow | 0 | |
| NC103A–NC107A | Nucleus Level 1 core | Various | 0 | 5 tests |
| NC108M | Module features | Various | **1** | |
| NC109M–NC110M | Module features | Various | 0 | 2 tests |
| NC111A–NC127A | Nucleus Level 1 | Various | 0 | 15 tests (NC128-NC130 don't exist) |
| NC131A–NC141A | Nucleus Level 1 | Various | 0 | 11 tests |
| NC170A | MULTIPLY Format 2 | Arithmetic, PicRuntime | 0 | |
| NC171A | DIVIDE Format 1 | Arithmetic, PicRuntime | 0 | |
| NC172A | DIVIDE Format 2 | Arithmetic, PicRuntime | 0 | |
| NC173A | DIVIDE Format 3 | Arithmetic, PicRuntime | 0 | |
| NC174A | SET, mnemonic names | Data | 0 | |
| NC175A–NC177A | Nucleus Level 1 | Various | 0 | 3 tests |
| NC201A | PERFORM Formats 3-4, VARYING | ControlFlow | **5** | VARYING edge cases |
| NC202A | ADD Format 3 | Arithmetic | 0 | |
| NC203A | DIVIDE Format 4 | Arithmetic | 0 | |
| NC204M | Nucleus Level 2 module | Various | 0 | |
| NC205A | Nucleus Level 2 | Various | 0 | |
| NC206A | Elementary item access | Expression | 0 | |
| NC207A | Qualified names Format 1 | Expression | 0 | |
| NC208A | Qualified names Formats 1-2 | Expression | **3** | |
| NC209A | Nucleus Level 2 | Various | **6** | |
| NC210A | Nucleus Level 2 | Various | 0 | |
| NC211A | Abbreviated conditions | Condition | 0 | |
| NC214M | Nucleus Level 2 module | Various | 0 | |
| NC215A | Continuation, strings | Expression | **6** | Preprocessor continuation |
| NC216A | Nucleus Level 2 features | Various | **8** | |
| NC217A | Nucleus Level 2 | Various | 0 | |
| NC218A | UNSTRING | String, StorageHelpers | **9** | Largest string test |
| NC219A | HIGH-VALUE, LOW-VALUE | Condition, PicRuntime | **1** | |
| NC220M | Nucleus Level 2 module | Various | 0 | **Runtime hang (undiagnosed)** |
| NC221A–NC224A | Nucleus Level 2 | Various | 0 | 4 tests |
| NC225A | EVALUATE | ControlFlow | **7** | Matching edge cases |
| NC231A–NC236A | Table handling | Expression | 0 | 6 tests |
| NC237A | SEARCH ALL Format 2 | ControlFlow | **3** | **Runtime hang risk** |
| NC238A–NC245A | Table handling | Expression | 0 | 8 tests |
| NC246A | Qualified names, subscripts | Expression, Condition | **14** | **Highest FAIL* count** |
| NC247A | OCCURS Format 2 (ODO) | Expression | **7** | |
| NC248A | Table handling | Expression | 0 | |
| NC250A | IF statement, abbreviations | ControlFlow, Condition | **4** | |
| NC251A | DIVIDE Format 5 | Arithmetic | 0 | |
| NC252A | REDEFINES, RENAMES | SemanticBuilder | **3** | |
| NC253A | SUBTRACT Format 3 | Arithmetic | 0 | |
| NC254A | Switch settings, SET ON/OFF | Data | 0 | |
| NC302M–NC303M | Nucleus Level 2 modules | Various | 0 | 2 tests |
| NC401M | ALPHABET, collating sequence | SemanticBuilder | 0 | |

### FAIL* by Component (from NIST baselines)

| Component | Tests | FAIL* | Key Tests |
|-----------|:-----:|:-----:|-----------|
| Expression (subscripts/qual) | 4 | 30 | NC246A(14), NC247A(7), NC208A(3), NC215A(6) |
| ControlFlow (PERFORM/EVAL) | 3 | 15 | NC225A(7), NC201A(5), NC237A(3) |
| Various (Level 2) | 3 | 15 | NC216A(8), NC209A(6), NC108M(1) |
| String (UNSTRING) | 2 | 10 | NC218A(9), NC219A(1) |
| Condition (IF abbrev) | 1 | 4 | NC250A(4) |
| SemanticBuilder (REDEFINES) | 1 | 3 | NC252A(3) |
| **Total** | **14** | **77** | |

---

## Non-NC Suites (364 programs, 0 baselines — from source-snapshot.txt)

| Suite | Programs | Focus | Status |
|-------|:--------:|-------|--------|
| SQ | 85 | Sequential file I/O | Unknown — 0 baselines |
| IC | 47 | Inter-program communication (CALL) | Unknown — 0 baselines |
| IF | 45 | Intrinsic functions | Unknown — 0 baselines |
| IX | 42 | Indexed file I/O | Unknown — 0 baselines |
| ST | 40 | Sorting (SORT/MERGE) | Unknown — 0 baselines |
| RL | 35 | Relative file I/O | Unknown — 0 baselines |
| SM | 17 | Source manipulation (COPY/REPLACE) | Unknown — 0 baselines |
| DB | 15 | Debugging module | Unknown — 0 baselines |
| SG | 13 | Segmentation | Unknown — 0 baselines |
| CM | 9 | Communication module | Unknown — 0 baselines |
| OB | 9 | Obsolete features | Unknown — 0 baselines |
| RW | 6 | Report Writer | Unknown — 0 baselines |
