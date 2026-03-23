# Section 6: Remediation Roadmap & Recommendations

## Overview

This section presents a prioritized remediation plan for the CobolSharp compiler, organized
by criticality. The plan reflects the current state as of 2026-03-22: 39 NIST tests passing
at 100%, 195 unit tests, 176 integration tests (1 skip), and the modernization ledger showing
all 9 migration phases complete. The remaining work falls into correctness blockers, feature
completions, code quality improvements, and future considerations.

---

## Priority 1 — Critical: Correctness Blockers

These items produce wrong results, infinite loops, or compilation failures for valid COBOL
programs. They must be fixed before any release claim.

### P1-1: NC220M Infinite Loop at Runtime

| Attribute | Detail |
|-----------|--------|
| Complexity | M |
| NIST tests blocked | NC220M |
| Dependencies | None |
| Risk | High — a hang is worse than a wrong answer; it blocks automated test runs |

**Problem:** NC220M enters an infinite loop at runtime. The root cause has not been diagnosed.
Likely candidates: incorrect PERFORM THRU scope resolution, missing loop termination condition
in VARYING/UNTIL lowering, or a control flow edge causing a cycle in the generated CIL.

**Remediation:**
1. Run NC220M under a debugger with a timeout. Capture the CIL basic block where execution loops.
2. Compare the generated control flow graph against the source PERFORM/GO TO structure.
3. Fix the IR lowering or flow analysis that produces the incorrect back-edge.
4. Verify fix does not regress any of the 39 passing NIST tests.

**Done when:** NC220M runs to completion and produces correct output matching the expected NIST results.

---

### P1-2: NC121M Subscripted DIVIDE GIVING

| Attribute | Detail |
|-----------|--------|
| Complexity | S |
| NIST tests blocked | NC121M |
| Dependencies | None |
| Risk | Medium — affects subscripted targets in arithmetic, a common pattern |

**Problem:** DIVIDE ... GIVING with subscripted operands fails. The binder or IR lowering does
not correctly resolve subscript expressions when they appear as GIVING targets in a DIVIDE
statement.

**Remediation:**
1. Examine `EmitDivideStatement` and the bound tree construction for DIVIDE GIVING.
2. Ensure subscripted data references are lowered to indexed storage accesses (not bare field references).
3. Add a targeted integration test for DIVIDE GIVING with subscripted targets.
4. Verify NC121M passes.

**Done when:** NC121M passes at 100% and subscripted GIVING targets work for all arithmetic statements.

---

### P1-3: VALUE THRU in Level-88 Conditions (Grammar Gap)

| Attribute | Detail |
|-----------|--------|
| Complexity | M |
| NIST tests blocked | NC201A, NC250A, NC252A |
| Dependencies | None |
| Risk | Medium — level-88 VALUE THRU is common in production COBOL |

**Problem:** The grammar does not support `VALUE value-1 THRU value-2` on level-88 condition
names. The parser rejects these programs outright.

**Remediation:**
1. Extend the grammar rule for level-88 VALUE clauses to accept `THRU`/`THROUGH` ranges.
2. Update `SemanticBuilder` to populate `ConditionSymbol.ValueRanges` with range pairs.
3. Update the binder to generate range-check conditions (`value >= low AND value <= high`).
4. Regenerate the parser from the updated `.g4` files.
5. Verify NC201A, NC250A, NC252A pass.

**Done when:** All three NIST tests pass. Level-88 conditions with VALUE THRU produce correct
boolean evaluation at runtime.

---

### P1-4: ASCENDING/DESCENDING KEY in OCCURS

| Attribute | Detail |
|-----------|--------|
| Complexity | M |
| NIST tests blocked | NC233A, NC237A, NC238A, NC247A |
| Dependencies | P1-3 may overlap in grammar changes |
| Risk | Medium — KEY clause affects SEARCH ALL correctness |

**Problem:** OCCURS ... ASCENDING/DESCENDING KEY is parsed into `OccursInfo` but not fully
wired for SEARCH ALL binary search generation. The generated code does not use the key
ordering for comparison.

**Remediation:**
1. Wire `OccursInfo.AscendingKeys` / `DescendingKeys` into `EmitSearchStatement` for SEARCH ALL.
2. Generate binary search logic using the declared key fields and their ordering.
3. Verify the four blocked NIST tests pass.

**Done when:** NC233A, NC237A, NC238A, NC247A pass. SEARCH ALL uses declared keys for ordered comparison.

---

## Priority 2 — High: Feature Completions

These items are parsed and partially bound but produce stubs or no-ops at runtime. They
represent incomplete compiler features that limit the programs CobolSharp can compile.

### P2-1: CALL/USING/RETURNING Full IR (Remove Stub)

| Attribute | Detail |
|-----------|--------|
| Complexity | L |
| Dependencies | None |
| Risk | High — inter-program calls are fundamental to COBOL systems |

**Current state:** `BoundCallStatement` and `BoundCallArgument` exist. Binder and validator
are wired. IR lowering emits a "CALL not implemented" display message and takes the exception path.

**Remediation:**
1. Implement parameter marshaling for BY REFERENCE (pass address), BY CONTENT (copy then pass),
   and BY VALUE (pass value).
2. Implement program lookup: search loaded assemblies for a type matching the called program name.
3. Wire RETURNING to capture the return value into the specified data item.
4. Handle ON EXCEPTION / NOT ON EXCEPTION control flow.
5. Fix the grammar gap: bare arguments without explicit `BY` keyword should default to BY REFERENCE.
6. Add integration tests for static subprogram calls within the same assembly.

**Done when:** A COBOL CALL statement invokes a separately compiled COBOL subprogram in the
same assembly, passes parameters correctly by all three modes, and returns values.

---

### P2-2: SORT/MERGE Full Implementation

| Attribute | Detail |
|-----------|--------|
| Complexity | L |
| Dependencies | P2-5 (file I/O completeness helps) |
| Risk | Medium — SORT is heavily used in batch COBOL |

**Current state:** INPUT/OUTPUT PROCEDURE variants work (they just call named paragraphs).
SORT USING/GIVING (file-based) is a stub. MERGE is not implemented.

**Remediation:**
1. Implement SORT USING: read all records from input file(s), sort by declared keys, write to output.
2. Implement SORT GIVING: write sorted records to output file(s).
3. Implement MERGE: merge pre-sorted input files by declared keys.
4. Use .NET `List<T>.Sort` or `Array.Sort` with a custom comparer built from the KEY clause.
5. Handle SD (sort description) file declarations properly.

**Done when:** SORT USING/GIVING and MERGE produce correct sorted/merged output files. Relevant
NIST sort tests pass.

---

### P2-3: Alternate Keys (Indexed Files)

| Attribute | Detail |
|-----------|--------|
| Complexity | L |
| Dependencies | Indexed file I/O backend (P2-5) |
| Risk | Low-medium — affects indexed file programs only |

**Current state:** Not parsed. The grammar does not support ALTERNATE RECORD KEY in SELECT.

**Remediation:**
1. Extend the grammar for SELECT to accept ALTERNATE RECORD KEY ... WITH DUPLICATES.
2. Add `AlternateKeys` to `FileSymbol`.
3. Extend `IFileHandler` to support multi-key indexing.
4. Implement alternate key lookup in the indexed file handler.

**Done when:** Programs using ALTERNATE RECORD KEY compile, and keyed READ/START operations
work with alternate keys.

---

### P2-4: START Statement (Full Implementation)

| Attribute | Detail |
|-----------|--------|
| Complexity | M |
| Dependencies | P2-5 (indexed file backend) |
| Risk | Medium — START is required for keyed sequential access |

**Current state:** Stub. Grammar has a known gap (`KEY IS comparisonExpression` requires two
operands, but standard COBOL START KEY has one operand with a comparison operator).

**Remediation:**
1. Fix the grammar: `KEY IS comparisonOp dataReference` (one operand, not a full comparison expression).
2. Wire `IFileHandler.Start` to position the file cursor.
3. Handle INVALID KEY / NOT INVALID KEY control flow.

**Done when:** START positions the file correctly for subsequent READ NEXT operations.

---

### P2-5: Indexed and Relative File I/O Backends

| Attribute | Detail |
|-----------|--------|
| Complexity | XL |
| Dependencies | None |
| Risk | High — many production COBOL programs use indexed files |

**Current state:** `IFileHandler` interface defined. Sequential file handler works. Indexed
and relative handlers are skeletons.

**Remediation:**
1. Implement `IndexedFileHandler` backed by a B+ tree or LiteDB (per KTD-7 in PROJECT_PLAN.md).
2. Support key-sequential access (READ NEXT after START), random access (READ with KEY), and
   dynamic access mode.
3. Implement `RelativeFileHandler` using fixed-length record files with seek arithmetic.
4. Wire REWRITE and DELETE for indexed files.

**Done when:** Programs using indexed and relative file organizations compile and execute
correctly. REWRITE, DELETE, START, and READ (sequential + random) all work.

---

### P2-6: Reserved Word Conflicts (STATUS/PROGRAM as Paragraph Names)

| Attribute | Detail |
|-----------|--------|
| Complexity | S |
| Dependencies | None |
| Risk | Low — niche but causes parse failures on valid programs |

**Current state:** STATUS and PROGRAM are reserved words, preventing their use as paragraph
names. Valid COBOL programs using these as paragraph names fail to parse.

**Remediation:**
1. Implement context-sensitive keyword handling: in procedure division, allow reserved words
   as paragraph names when they appear in label position (column 8-11 or followed by a period
   as a paragraph header).
2. Add tests for programs using STATUS and PROGRAM as paragraph names.

**Done when:** Programs with STATUS or PROGRAM as paragraph names compile and execute correctly.

---

## Priority 3 — Medium: Code Quality Improvements

These items are recommended by PROMPT.md audit criteria and the CONSTRAINTS.md anti-pattern
catalog. They improve maintainability, correctness confidence, and long-term sustainability.

### P3-1: Eliminate All Silent Statement Skips

| Attribute | Detail |
|-----------|--------|
| Complexity | S |
| Dependencies | None |
| Risk | None — purely additive diagnostics |

**Problem:** Per IMPLEMENTATION-STATUS.md, `BoundTreeBuilder.BindStatement` returns `null` for
unrecognized statement types, and these nulls propagate silently. The binder's catch-all `break`
silently drops statements.

**Remediation:**
1. Replace every `return null` in `BindStatement` with a diagnostic emission (ICE or warning).
2. Replace the catch-all `break` in `LowerStatement` with an ICE diagnostic.
3. Ensure no statement type is silently ignored.

**Done when:** Every unhandled statement type produces a visible diagnostic at compile time.

---

### P3-2: Remove Legacy CobolProgram/CobolField Types

| Attribute | Detail |
|-----------|--------|
| Complexity | M |
| Dependencies | Unit test migration |
| Risk | Low — requires rewriting unit tests to use ProgramState/PicRuntime |

**Problem:** Per MIGRATION_LEDGER.md Phase 9, `CobolProgram` and `CobolField` are legacy
runtime types retained only because 119+ unit tests depend on them.

**Remediation:**
1. Migrate unit tests to use `ProgramState` and `PicRuntime` APIs.
2. Remove `CobolProgram`, `CobolField`, and the `Types/` directory.
3. Verify all tests pass with the new APIs.

**Done when:** No references to `CobolProgram` or `CobolField` remain in the codebase.

---

### P3-3: Complete Deferred Semantic Features

| Attribute | Detail |
|-----------|--------|
| Complexity | M per feature |
| Dependencies | Varies |
| Risk | Medium — each is a spec compliance gap |

Per TECHNICAL-DEBT.md, these features parse but have incomplete semantics or codegen:

| Feature | Complexity |
|---------|-----------|
| Duplicate data-name resolution (IN/OF qualification) | M |
| Class conditions (IF NUMERIC, IF ALPHABETIC) | S |
| NEXT SENTENCE binding | S |
| SIGN clause (trailing separate, overpunch variants) | M |
| Abbreviated combined relations (`A > B AND < C`) | M |
| ROUNDED phrase (actual rounding instead of truncation) | S |

**Done when:** Each feature produces correct output per the ISO spec, verified by test.

---

### P3-4: Remaining C# 13 Adoption

| Attribute | Detail |
|-----------|--------|
| Complexity | S |
| Dependencies | None |
| Risk | None |

Per MIGRATION_LEDGER.md architectural notes: "Continue C# 13 adoption in remaining source
files (IR, codegen, runtime)." Apply primary constructors, collection expressions, and modern
patterns to files not yet touched in Phases 1-9.

**Done when:** Codebase-wide sweep confirms consistent use of modern C# features.

---

### P3-5: Package Version Updates

| Attribute | Detail |
|-----------|--------|
| Complexity | S |
| Dependencies | None |
| Risk | Low — requires regression testing |

Per MIGRATION_LEDGER.md: "Consider package updates (xunit, Microsoft.NET.Test.Sdk, Mono.Cecil)."
Update NuGet packages to latest compatible versions via Directory.Packages.props.

**Done when:** All packages at latest stable versions, all tests pass.

---

## Priority 4 — Low: Future Considerations

These items are not blocking any current use cases but represent the path toward full
ISO 2023 compliance and production readiness.

### P4-1: Report Writer (INITIATE/GENERATE/TERMINATE)

| Attribute | Detail |
|-----------|--------|
| Complexity | XL |
| Dependencies | None |
| Risk | Low urgency — Report Writer is declining in use |

Report Writer requires a complete runtime subsystem for line counting, group indication,
control breaks, and page overflow. Grammar support exists (CobolReportWriter.g4).

---

### P4-2: OO COBOL (CLASS, INVOKE, METHOD)

| Attribute | Detail |
|-----------|--------|
| Complexity | XL |
| Dependencies | None |
| Risk | Low urgency — OO COBOL adoption is minimal |

Would require class compilation, method dispatch, and object lifecycle management on .NET.

---

### P4-3: Exception Handling (RAISE/RESUME)

| Attribute | Detail |
|-----------|--------|
| Complexity | L |
| Dependencies | None |
| Risk | Low urgency — ISO 2023 feature, not in COBOL-85 NIST suite |

Map COBOL exception handling to .NET try/catch/throw semantics.

---

### P4-4: Screen Section (ACCEPT/DISPLAY with Screen)

| Attribute | Detail |
|-----------|--------|
| Complexity | L |
| Dependencies | Terminal I/O library |
| Risk | Low urgency — batch-oriented programs do not use Screen Section |

---

### P4-5: ALTER Statement — **DONE**

Implemented with version-aware behavior: error in COBOL-2002+ (CBL3601), warning+full
runtime support in COBOL-85/Default (CBL3602). Runtime alter indirection table (`int[]`)
for mutable GO TO targets. Bare GO TO (CBL3605/3606) supported. Zero overhead for
non-ALTER programs. `--standard` CLI option wired through to compilation pipeline.

---

### P4-6: CI/CD Pipeline (GitHub Actions)

| Attribute | Detail |
|-----------|--------|
| Complexity | M |
| Dependencies | None |
| Risk | None — infrastructure only |

Per KTD-10, GitHub Actions with matrix builds (Windows + Linux + macOS) should be established
once cross-platform file I/O is exercised.

---

### P4-7: Dynamic CALL (Cross-Assembly Program Loading)

| Attribute | Detail |
|-----------|--------|
| Complexity | L |
| Dependencies | P2-1 (basic CALL) |
| Risk | Low urgency — needed for multi-assembly COBOL systems |

Extend CALL to load programs from separate .NET assemblies at runtime via
`Assembly.Load` / `Activator.CreateInstance`.

---

## Suggested Phasing and Ordering

The work should proceed in waves, respecting dependencies and maximizing NIST test coverage
gains at each step.

### Wave 1: Correctness Blockers (Target: 46+ NIST tests passing)

Order:
1. **P1-2** (NC121M subscripted DIVIDE) — smallest fix, immediate NIST gain
2. **P1-1** (NC220M hang) — requires diagnosis first, but eliminates a runtime hazard
3. **P1-3** (VALUE THRU grammar) — unlocks 3 NIST tests, grammar-only change
4. **P1-4** (ASCENDING/DESCENDING KEY) — unlocks 4 NIST tests, depends on OccursInfo already in place

Estimated effort: 2-3 sessions.

### Wave 2: Core Feature Completions (Target: production-viable compiler)

Order:
1. **P2-6** (reserved word conflicts) — small fix, removes parse failures
2. **P2-1** (CALL full IR) — high value, foundational for multi-program systems
3. **P2-4** (START statement) — grammar fix + handler wiring
4. **P2-2** (SORT/MERGE) — depends on file I/O maturity
5. **P2-5** (indexed/relative file I/O) — largest item, enables P2-3 and P2-4
6. **P2-3** (alternate keys) — depends on P2-5

Estimated effort: 5-8 sessions.

### Wave 3: Code Quality and Spec Compliance

Order:
1. **P3-1** (eliminate silent skips) — quick win, improves diagnostic coverage
2. **P3-3** (deferred semantic features) — one feature at a time, each independently testable
3. **P3-2** (remove legacy types) — requires unit test migration, do when bandwidth allows
4. **P3-4** and **P3-5** (C# modernization, package updates) — mechanical, low risk

Estimated effort: 3-5 sessions.

### Wave 4: Future Work

Items P4-1 through P4-7 are independent and can be tackled in any order based on user demand.
Each is a self-contained project.

---

## Dependency Graph

```
P1-2 (subscripted DIVIDE) ──────────────────────────────────> standalone
P1-1 (NC220M hang) ─────────────────────────────────────────> standalone
P1-3 (VALUE THRU) ──────────────────────────────────────────> standalone
P1-4 (KEY in OCCURS) ──────────────────────────────────────-> standalone (OccursInfo exists)

P2-6 (reserved words) ─────────────────────────────────────-> standalone
P2-1 (CALL full IR) ────────────────────────────────────────-> P4-7 (dynamic CALL)
P2-4 (START) ───────────────────────────────────────────────-> P2-5 (indexed files)
P2-5 (indexed/relative files) ──────────────────────────────-> P2-3 (alternate keys)
                                                             -> P2-4 (START full test)
P2-2 (SORT/MERGE) ──────────────────────────────────────────-> P2-5 (for file-based SORT)

P3-1 (silent skips) ────────────────────────────────────────-> standalone
P3-2 (legacy types) ────────────────────────────────────────-> standalone (unit test rewrite)
P3-3 (semantic features) ──────────────────────────────────-> standalone (each independent)
```

---

## Success Criteria

The remediation roadmap is complete when all of the following are true:

1. **All NIST COBOL-85 Nucleus tests pass at 100%.** The current 39/~50 must reach full
   coverage across the NC (Nucleus) test suite. NC121M, NC220M, NC201A, NC233A, NC237A,
   NC238A, NC247A, NC250A, NC252A all pass.

2. **Zero stubs in code generation.** Every statement that the binder accepts must produce
   real CIL code, not a "not implemented" display message. CALL, SORT (USING/GIVING), START,
   MERGE all produce correct runtime behavior.

3. **Zero silent skips.** No statement type is silently dropped by the binder or lowering
   pipeline. Every unhandled construct produces a compile-time diagnostic.

4. **No runtime hangs.** All compiled programs terminate (or run continuously only when the
   source program specifies it). NC220M and any similar cases are resolved.

5. **Clean codebase per PROMPT.md standards.** No legacy types (CobolProgram/CobolField),
   consistent C# 13 usage, all packages current, no dead code, no duplicated logic.

6. **All tests green.** Unit tests, integration tests, and NIST conformance tests all pass
   with zero failures and zero unexplained skips.

7. **Indexed file I/O operational.** At least one indexed file backend works for READ, WRITE,
   REWRITE, DELETE, and START with key-sequential and random access modes.

8. **CALL interop works.** A COBOL program can call a separately compiled COBOL subprogram
   within the same assembly, passing parameters by reference, content, and value.

---

## Complexity Key

| Size | Definition |
|------|-----------|
| S | < 1 session. Localized change, few files, clear fix. |
| M | 1-2 sessions. Multiple files, some design decisions, moderate testing. |
| L | 2-4 sessions. Cross-cutting change, new subsystem wiring, significant testing. |
| XL | 4+ sessions. New runtime subsystem, major grammar changes, extensive test suite. |
