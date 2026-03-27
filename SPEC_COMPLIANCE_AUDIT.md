# CobolSharp Spec Compliance Audit

**Date:** 2026-03-27
**Spec:** ISO/IEC 1989:2023 (COBOL), with COBOL-85 as primary target
**Method:** 8 parallel agents audited every spec feature against the implementation
**Scope:** Grammar, binding, lowering, CIL emission, runtime, and testing

---

## Executive Summary

| Category | Fully Impl. | Partial | Not Impl. | Spec Violations |
|----------|:-----------:|:-------:|:---------:|:---------------:|
| Data Division | 25 | 12 | 15 | 11 |
| Procedure Division (38 statements) | 15 | 17 | 4 | 10 |
| Expressions & Conditions | 12 | 4 | 7 | 6 |
| File I/O | 16 | 8 | 14 | 10 |
| Environment Division | 6 | 5 | 7 | 5 |
| Data Movement (MOVE) | 16 | 7 | 7 | 8 |
| Intrinsic Functions (94 total) | 33 | 15 | 38 | 8 |
| SORT/MERGE & Table Handling | 5 | 5 | 14 | 7 |

---

## Critical Runtime Bugs (produce wrong results NOW)

These are the highest priority — they silently produce incorrect output for valid COBOL programs.

### P0: Bugs that corrupt data or crash

| # | Bug | Impact | Location |
|---|-----|--------|----------|
| 1 | **NumericEdited->NumericEdited MOVE produces zero.** Routes through `DecodeDisplay` which can't parse edited strings. Any `MOVE edited-A TO edited-B` with different patterns returns 0. | Data corruption | `CilEmitter.cs` dispatch + `PicRuntime.cs` |
| 2 | **OPEN drops all but first clause.** `OPEN INPUT A OUTPUT B` only opens A; B is silently never opened. | Files never opened | `BoundTreeBuilder.cs:809-811` |
| 3 | **READ INVALID KEY drives wrong condition.** Uses AT END check (status "10") instead of INVALID KEY check. Random/keyed READ failures don't trigger INVALID KEY branch. | Wrong control flow | `BoundTreeBuilder.cs:887-902` |
| 4 | **WRITE/REWRITE INVALID KEY silently discarded.** Parsed but binder ignores it entirely. | Missing error handling | `BoundTreeBuilder.cs` BindWrite/BindRewrite |
| 5 | **File status codes 43/44/47 misassigned vs ISO.** 43 should be "last I/O not READ before DELETE/REWRITE"; we use it for "no read permission". 47 should be "not open for input"; we use it for "record length error". | Wrong status codes | `FileStatus.cs` |
| 6 | **User-defined CLASS names crash compiler.** `BoundTreeBuilder` throws `InvalidOperationException` instead of producing a diagnostic. | Compiler crash | `BoundTreeBuilder.cs:2768` |
| 7 | **LOCAL-STORAGE treated as WORKING-STORAGE.** Items silently access FileSection storage, corrupting file record buffers. Never re-initialized between invocations. | Data corruption | `CilEmitter.cs:3153-3155` |
| 8 | **Class condition on ref-mod subject crashes.** `LowerClassCondition` throws if subject is `BoundReferenceModificationExpression`. | Compiler crash | `Binder.cs:2739-2759` |

### P1: Wrong computation results

| # | Bug | Impact | Location |
|---|-----|--------|----------|
| 9 | **PERFORM WITH TEST AFTER silently ignored.** All loops execute as TEST BEFORE regardless of source. One extra or missing iteration. | Wrong loop count | `BoundTreeBuilder.cs` (no `IsTestAfter` flag) |
| 10 | **Source subscript re-evaluated per target.** `MOVE A(I) TO I, B(I)` — I is modified on first store; second target uses wrong subscript. | Wrong data | `Binder.cs:813` (loop) |
| 11 | **DECIMAL-POINT IS COMMA in edited PIC.** Both branches produce same character — `.` and `,` cases are identical. | Wrong formatting | `PicRuntime.cs:270-278` |
| 12 | **INTEGER intrinsic: Math.Truncate instead of Math.Floor.** `INTEGER(-1.5)` returns -1 instead of -2. | Wrong result | `IntrinsicFunctions.cs` |
| 13 | **MOD intrinsic: C# % instead of floor-based modulo.** `MOD(-11, 5)` returns -1 instead of 4. | Wrong result | `IntrinsicFunctions.cs` |
| 14 | **WRITE ADVANCING with identifier always outputs 1 line.** Dynamic line spacing ignored. | Wrong output | `BoundTreeBuilder.cs:768` |
| 15 | **ACCEPT DATE returns 8 digits (YYYYMMDD) even without YYYYMMDD qualifier.** 6-digit format (YYMMDD) not distinguished. | Wrong data size | Runtime ACCEPT |
| 16 | **Signed DISPLAY default not trailing overpunch.** Items without explicit SIGN clause get `SignStorage=None` instead of `TrailingOverpunch`. | Wrong sign encoding | `PicUsageResolver.cs` |
| 17 | **IndexedFileHandler key comparison uses TrimEnd().** Fixed-width keys with trailing spaces compare incorrectly. | Wrong key matching | `IndexedFileHandler.cs` |
| 18 | **RelativeFileHandler reads key as Int32 bytes.** COBOL numeric PIC items store ASCII digits, not binary. | Wrong record number | `RelativeFileHandler.cs` |
| 19 | **SEARCH ALL uses linear scan, not binary search.** Correct results but O(n) not O(log n). | Performance | `Binder.cs:3274` |
| 20 | **SEARCH VARYING clause silently dropped.** VARYING target never incremented. | Wrong variable state | `BoundTreeBuilder.cs` BindSearch |

---

## Major Missing Features

### Not implemented at all (no binding, no IR, no runtime)

| Feature | Spec § | Impact |
|---------|--------|--------|
| **SORT statement** | 14.9.40 | Any program using SORT fails to compile (COBOL0110) |
| **MERGE statement** | 14.9.24 | Any program using MERGE fails to compile |
| **RELEASE statement** | 14.9.32 | Required for SORT INPUT PROCEDURE |
| **SD file descriptions** | 13.18 | Sort files can't be declared (parse failure) |
| **Intrinsic function binder** | 15.x | All 94 FUNCTION calls return 0 (runtime exists but unreachable) |
| **XOR / EXCLUSIVE-OR** | 8.8.4.11 | Missing from lexer, grammar, bound nodes, IR |
| **User-defined CLASS conditions** | 8.8.4.4 | Crashes compiler (no symbol table, no runtime) |
| **ALPHABET / collating sequence** | 12.3.7 | Parsed but never applied to comparisons |
| **SYMBOLIC CHARACTERS** | 12.3.7 | Parsed but never registered as figurative constants |
| **EXTERNAL clause on data items** | 13.18.22 | Shared storage across programs not supported |
| **GLOBAL clause on data items** | 13.18.27 | Nested program visibility not supported |
| **OCCURS DEPENDING ON runtime** | 13.18.38 | Active count never consulted; static max used |
| **USE declaratives** | 14.9.49 | Parsed but never invoked on I/O errors |
| **LINAGE clause + END-OF-PAGE** | 13.18.34 | Print file page control not supported |
| **RELATIVE KEY IS clause** | 12.4.5 | Relative file random access broken |
| **EXIT PERFORM CYCLE** | 14.9.14 | No CYCLE keyword; can't skip to loop increment |

### Parse-only (grammar exists, no semantic processing)

| Feature | Notes |
|---------|-------|
| Report Writer (GENERATE, INITIATE, TERMINATE) | Grammar stubs only |
| COMP-1/COMP-2 arithmetic | Sized correctly but treated as integers, not IEEE 754 |
| National data (PIC N) | Parsed but stored as single-byte; no UTF-16 |
| Screen Section | Not in grammar |
| Communication Section | Not in grammar |
| SYNCHRONIZED alignment | Parsed, no padding computed |

---

## Missing Validation (compile-time checks absent)

| Check | Spec Rule | Consequence |
|-------|-----------|-------------|
| MOVE ZERO to Alphabetic | 14.9.25.3 SR 6 | Silently accepted |
| HIGH-VALUE/LOW-VALUE/QUOTE to Numeric | 14.9.25.3 SR 5 | Silently accepted |
| Numeric noninteger to Alphanumeric | Table 16 | Silently accepted |
| BLANK WHEN ZERO with JUSTIFIED | 13.18.8 | No cross-clause check |
| OCCURS on level-66 | 13.18.38 SR 3 | Accepted instead of rejected |
| VALUE on REDEFINES items | 13.18.63 SR 10 | Accepted instead of rejected |
| VALUE on OCCURS subordinates | 13.18.63 SR 11 | Accepted instead of rejected |
| REDEFINES clause ordering | 13.18.44 SR 1 | Any clause order accepted |
| RENAMES THRU physical ordering | 13.18.46.4 GR 6 | FROM/THRU order not validated |
| Open-mode enforcement | Table 20 | READ on OUTPUT, WRITE on INPUT succeed |
| SEARCH ALL WHEN must be KEY equality | 14.9.37 SR 7-11 | Any condition accepted |
| CORRESPONDING excludes RENAMES | 14.7.6 rule 4 | Level-66 items not excluded |
| CORRESPONDING move legality per pair | 14.7.6 rule 2 | Category compatibility not checked |
| Sign condition on non-numeric literal | 8.8.4.7.3 | `"ABC" IS POSITIVE` accepted |

---

## Missing Category: CobolCategory.Alphabetic

PIC A items are classified as `Alphanumeric` instead of a distinct `Alphabetic` category. This causes:
- MOVE ZERO to PIC A silently accepted (should be error)
- MOVE Numeric to PIC A silently accepted (should be error per Table 16)
- No alphabetic-specific validation on input data
- Class condition `IS ALPHABETIC` works but category-based dispatch is wrong

---

## Intrinsic Functions: 94 Total, 0 Reachable from COBOL Source

The binder (`BoundTreeBuilder.cs:2287-2294`) emits COBOL0110 warning and returns literal `0m` for every `FUNCTION` call. The runtime library (`IntrinsicFunctions.cs`) implements 48 functions (33 correct, 15 with wrong semantics), but none are reachable. 38 functions have no runtime code at all.

**Wrong semantics in runtime (even if wired):** INTEGER (Truncate not Floor), MOD (C# % not floor-modulo), WHEN-COMPILED (16 chars not 21), BYTE-LENGTH/LENGTH (runtime string length not declared size), TRIM (no LEADING/TRAILING), NUMVAL/NUMVAL-C (naive parsing), DATE-TO-YYYYMMDD/YEAR-TO-YYYY (missing optional args), MAX/MIN/ORD-MAX/ORD-MIN (numeric only, spec allows alphanumeric), SUBSTITUTE (single pair only).

---

## File I/O: Status Code Misassignment

| Our Code | Our Meaning | ISO Meaning |
|----------|-------------|-------------|
| 43 | No read permission | Last I/O before DELETE/REWRITE was not successful READ |
| 44 | No write permission | Boundary violation (record too large) |
| 47 | Record length error | READ/START on file not open in INPUT or I-O mode |

**Missing status codes:** 02, 04, 05, 06, 07, 09, 14, 34, 39, 46, 48, 49.

---

## ROUNDED MODE (affects ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE)

The spec defines 8 rounding modes: AWAY-FROM-ZERO, NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN, NEAREST-TOWARD-ZERO, PROHIBITED, TOWARD-GREATER, TOWARD-LESSER, TRUNCATION. The grammar accepts only bare `ROUNDED` with no `MODE IS` sub-phrase. The runtime always uses NEAREST-AWAY-FROM-ZERO. Programs that specify `ROUNDED MODE IS TRUNCATION` or `ROUNDED MODE IS NEAREST-EVEN` will get wrong results.

---

## Priority Recommendations

### Immediate (P0 — data corruption / crashes)
1. Fix OPEN multi-clause (drops files)
2. Fix READ INVALID KEY condition check
3. Fix WRITE/REWRITE INVALID KEY binding
4. Fix file status code assignments
5. Fix NumericEdited->NumericEdited MOVE
6. Fix LOCAL-STORAGE storage area routing
7. Add diagnostic for user-defined CLASS (instead of crash)
8. Fix class condition on ref-mod subject

### High (P1 — wrong computation)
9. Fix PERFORM WITH TEST AFTER
10. Fix MOVE source subscript evaluation order
11. Fix DECIMAL-POINT IS COMMA in edited PIC
12. Fix INTEGER/MOD intrinsic semantics
13. Fix signed DISPLAY default (trailing overpunch)
14. Wire intrinsic function binder (48 functions ready in runtime)
15. Fix WRITE ADVANCING with identifier

### Medium (P2 — missing features)
16. Implement SORT/MERGE/RELEASE + SD
17. Implement OCCURS DEPENDING ON runtime
18. Add CobolCategory.Alphabetic
19. Implement XOR/EXCLUSIVE-OR
20. Implement user-defined CLASS conditions
21. Implement ALPHABET / collating sequence
22. Implement EXIT PERFORM CYCLE
23. Fix open-mode enforcement in file handlers

### Low (P3 — completeness)
24. Implement missing file status codes
25. Add ROUNDED MODE phrase
26. Implement remaining 38 intrinsic functions
27. Implement USE declaratives
28. Implement LINAGE + END-OF-PAGE
29. Implement RELATIVE KEY IS
30. Add missing validation checks (14 items above)
