# Section 4: Test Coverage & NIST Compliance

## 4.1 Test Framework and Infrastructure

CobolSharp uses **xUnit** as its sole test framework. Both test projects reference `xunit`,
`xunit.runner.visualstudio`, and `Microsoft.NET.Test.Sdk` via NuGet. There is no use of NUnit
or MSTest anywhere in the codebase.

Two test projects exist:

| Project | Path | Focus |
|---------|------|-------|
| `CobolSharp.Tests.Unit` | `tests/CobolSharp.Tests.Unit/` | Isolated unit tests for individual compiler subsystems |
| `CobolSharp.Tests.Integration` | `tests/CobolSharp.Tests.Integration/` | End-to-end compile-and-run tests against inline COBOL source |

Both projects reference `CobolSharp.Compiler` and `CobolSharp.Runtime`. There is no separate
test project for the CLI.

A third test corpus -- the NIST CCVS85 suite -- lives under `tests/nist/` and is exercised
via a shell script (`scripts/nist-batch.sh`), not through the xUnit runner. This is a notable
architectural choice: NIST tests are run out-of-band rather than as parameterized xUnit test
cases.

### Reported Test Counts (from CLAUDE.md, 2026-03-21)

| Category | Count |
|----------|-------|
| Unit tests | 217 pass |
| Integration tests | 184 pass, 1 skip |
| NIST tests at 100% | 31 (in guard script) |

---

## 4.2 Unit Test Coverage by Compiler Phase

The unit tests are organized into subdirectories that mirror compiler subsystems. The table
below maps each test file to the compiler phase it covers and the number of `[Fact]`/`[Theory]`
test methods it contains.

### Semantics / Binding / Validation (7 files, ~89 test methods)

| File | Methods | Covers |
|------|---------|--------|
| `Semantics/BoundTreeValidatorTests.cs` | 28 | Expression type enforcement on PERFORM, IF, EVALUATE (CBL23xx/24xx/25xx diagnostics) |
| `Semantics/SymbolValidatorTests.cs` | 16 | Duplicate detection, REDEFINES validation, linkage section rules (CBL31xx diagnostics) |
| `Semantics/CategoryCompatibilityTests.cs` | 13 | MOVE legality matrix, COMPARE compatibility, arithmetic operand rules across all COBOL categories |
| `Semantics/DataItemClassifierTests.cs` | 12 | Data item classification: OCCURS constraints, storage layout, group vs. elementary |
| `Semantics/DiagnosticReachabilityTests.cs` | 8 | Verifies that specific diagnostic codes are reachable through their validation APIs |
| `Semantics/ExpressionTypeTests.cs` | 8 | ExpressionType derivation from PIC clauses: integer/decimal/alphanumeric/comp/index |
| `Semantics/ArithmeticDiagnosticTests.cs` | 4 | Arithmetic type system: alphanumeric operand/result rejection (CBL26xx) |

A shared base class `DiagnosticTestBase` provides `GetDiagnostics(string cobolSource)` which
compiles inline COBOL and returns diagnostics. Most semantic tests inherit from this base
and test by asserting on diagnostic codes (e.g., `AssertHasDiagnostic(diags, "CBL3101")`).

### Runtime (4 files, ~72 test methods)

| File | Methods | Covers |
|------|---------|--------|
| `Runtime/IntrinsicFunctionTests.cs` | 30 | All intrinsic functions: ABS, SQRT, MOD, FACTORIAL, PI, MAX, MIN, SUM, MEAN, MEDIAN, RANGE, LOWER-CASE, UPPER-CASE, etc. |
| `Runtime/PicRuntimeMoveTests.cs` | 20 | PIC-to-PIC MOVE operations: numeric-to-numeric, sign storage conversions (trailing separate, trailing overpunch, leading separate), COMP-3, alphanumeric moves |
| `Runtime/CobolFieldTests.cs` | 16 | CobolField type: initialization, get/set numeric, decimal places, truncation, signed fields, alphanumeric, group fields, redefinition |
| `Runtime/SequentialFileTests.cs` | 6 | Sequential file I/O: write/read round-trip, end-of-file detection, close/reopen, line-sequential mode, file status codes |

### Preprocessor (1 file, 4 test methods)

| File | Methods | Covers |
|------|---------|--------|
| `Preprocessor/FixedFormTests.cs` | 4 | Fixed-form detection, column stripping, continuation lines, large-file performance |

### Common Infrastructure (1 file, 8 test methods)

| File | Methods | Covers |
|------|---------|--------|
| `Common/SourceTextTests.cs` | 8 | SourceText line tracking, line/column number computation, location retrieval, line text extraction |

### Phases Without Dedicated Unit Tests

The following compiler phases have **no direct unit test coverage** -- they are exercised only
indirectly through integration tests:

- **Parsing / Grammar** (`Parsing/`, `Grammar/`, `Generated/`) -- The ANTLR4-generated parser
  and custom error strategy have no isolated parser-level tests. The error strategy is tested
  indirectly via `CobolErrorStrategyTests` in the integration project.
- **Code Generation / CIL Emission** (`CodeGen/CilEmitter.cs`, `CodeGen/Binder.cs`) -- No
  unit tests for IL generation; correctness is validated only through end-to-end execution.
- **IR Lowering** (`IR/IrInstruction.cs`, `IR/IrMethod.cs`, `IR/IrModule.cs`) -- The
  intermediate representation layer has no unit tests.
- **Flow Analysis** (`FlowAnalysis/BasicBlock.cs`, `ParagraphReachabilityAnalyzer.cs`,
  `PerformRangeChecker.cs`) -- No direct unit tests.
- **SemanticBuilder / Binder** (`Semantics/SemanticBuilder.cs`) -- The main binding pass that
  converts parse trees to bound trees is not unit-tested in isolation.
- **COPY preprocessor** (`Preprocessor/CopyProcessor.cs`) -- Tested only through integration
  tests (e.g., `CopyStatement_ExpandsCopybook`).
- **NistPreprocessor** (`Preprocessor/NistPreprocessor.cs`) -- No unit tests; exercised only
  when running NIST programs.

---

## 4.3 Integration Test Approach

### EndToEndTests (169 test methods, ~4,870 lines)

The file `tests/CobolSharp.Tests.Integration/EndToEndTests.cs` is the primary integration
test suite. Each test follows an identical pattern:

1. Write inline COBOL source to a temp directory
2. Invoke `Compilation.Compile()` to produce a .NET DLL
3. Execute the DLL via `dotnet <output.dll>` as a child process
4. Assert on stdout, stderr, and exit code
5. Clean up temp files via `IDisposable`

Process execution has a 10-second timeout (`process.WaitForExit(10000)`).

**Coverage breadth.** The 169 tests span a wide range of COBOL features:

- **Arithmetic**: ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE (including subscripted operands, GIVING, ROUNDED, SIZE ERROR)
- **Control flow**: IF/ELSE, EVALUATE (single/multi-subject, ALSO, ranges, TRUE matching), PERFORM (TIMES, UNTIL, VARYING, AFTER, inline, thru, nested), GO TO (simple, DEPENDING), EXIT (PROGRAM, PARAGRAPH, SECTION, PERFORM), NEXT SENTENCE, GOBACK
- **Data manipulation**: MOVE, INITIALIZE, SET (conditions, indexes, UP BY/DOWN BY), STRING, UNSTRING, INSPECT (TALLYING, REPLACING, CONVERTING)
- **File I/O**: OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START, file status codes (00, 10, 35), sequential and indexed organization, line-sequential mode
- **Table handling**: Subscripts (constant, variable, expression, 2D, 3D), reference modification, SEARCH (linear and binary/ALL), OCCURS with VALUE and REDEFINES
- **Data types**: Signed fields (trailing separate, trailing overpunch), COMP-3, BLANK WHEN ZERO, JUSTIFIED RIGHT, USAGE inheritance, DECIMAL-POINT IS COMMA
- **Miscellaneous**: COPY, ACCEPT FROM DATE/TIME/DAY/DAY-OF-WEEK, abbreviated relations, sections

**One skipped test.** `CallStatement_EmitsDiagnostic` is marked `[Fact(Skip = "CALL statement not yet lowered to CIL")]`, confirming that inter-program CALL is unimplemented at the IL level.

### CobolErrorStrategyTests (5 test methods)

Located at `tests/CobolSharp.Tests.Integration/CobolErrorStrategyTests.cs`, these tests
validate the compiler's custom ANTLR error recovery against real NIST programs that exercise
edge cases:

- `Detects_UnrecognizedClause` -- BLANK WHEN ZERO not yet parsed (NC134A)
- `Detects_UnexpectedStatus` -- STATUS as identifier conflict (NC211A, NC254A)
- `Detects_ProgramMisplaced` -- PROGRAM as identifier conflict (NC215A, NC219A)
- `StatusReserved_HasCode0200` -- Verifies diagnostic code COBOL0200
- `ErrorCountCap_NC203A_AtMost20` -- Ensures error count is capped at 20

### DiagnosticNormalization (test utility)

`DiagnosticNormalization.cs` is a helper class (not a test class) that normalizes diagnostic
messages for stable assertions: collapsing whitespace, normalizing quotes, stripping numeric
values, and canonicalizing common phrasings. This allows tests to survive wording changes
without churn.

---

## 4.4 NIST Test Suite: Compliance and Methodology

### Source Material

The NIST CCVS85 (COBOL Compiler Validation System, 1985 standard, version 4.2) test suite
is stored under `tests/nist/`:

| Path | Contents |
|------|----------|
| `tests/nist/newcob.zip` | Original NIST archive |
| `tests/nist/extracted/newcob.val` | Extracted validation file |
| `tests/nist/programs/` | 459 individual `.cob` test programs (NC, IC, IF, ST, SQ, RL, DB, CM, OB, SM series) |
| `tests/nist/output/` | Compiled DLLs and runtimeconfig files |
| `tests/nist/valid/` | 54 captured output files from successful runs |

Of the 459 programs, 95 are in the NC (Nucleus) series, which is the primary focus of the
compiler at this stage.

### Execution Methodology

NIST tests are **not** wired into the xUnit test runner. Instead, they are executed via the
shell script `scripts/nist-batch.sh`, which:

1. Compiles each test program using `dotnet run --project src/CobolSharp.CLI -- --nist <source> -o <output>`
2. The `--nist` flag activates `NistPreprocessor.Process()`, which replaces site-specific
   XXXXX placeholders (XXXXX055 for printer file, XXXXX082/083 for computer names, etc.)
3. Runs the compiled DLL via `dotnet <output.dll>`
4. Parses the CCVS85 output file looking for the summary line (e.g., "093 OF 093 TESTS WERE EXECUTED SUCCESSFULLY")
5. Reports PASS/FAIL/SKIP per test and writes CSV summary + failure log

The script supports parallel execution (4 jobs), stop-on-first-failure mode, and category
filtering (ARITH, CTRL, IO).

### 31 NIST Tests at 100% Pass Rate (in guard script)

Per CLAUDE.md, the following NIST NC-series tests achieve 100% pass rate (all internal
sub-tests successful):

**Arithmetic (NC1xx):** NC101A, NC102A, NC103A, NC104A, NC105A, NC106A, NC107A, NC111A,
NC112A, NC115A, NC116A, NC117A, NC118A, NC119A, NC120A, NC122A, NC123A, NC124A, NC126A,
NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A

**Table Handling (NC17x):** NC170A, NC171A, NC172A, NC173A, NC175A, NC176A, NC177A

**Control Flow (NC2xx):** NC202A, NC203A, NC206A, NC207A, NC210A, NC221A, NC222A, NC224A

**Data (NC2xx high):** NC239A, NC240A, NC241A, NC248A, NC251A, NC253A

Each passing NIST test contains dozens to hundreds of individual sub-tests. For example,
NC101A (MULTIPLY format 1) contains 93 sub-tests, all passing. This represents significant
coverage of the COBOL-85 nucleus specification.

### NIST Tests with Captured Output but Not at 100%

The `tests/nist/valid/` directory contains 54 output files, which is 15 more than the 39
listed as 100% in CLAUDE.md. The additional files (NC108M through NC253A range) likely
represent tests that compile and run but have some failing sub-tests, or were added after
the CLAUDE.md snapshot.

### NIST Tests Not Yet Attempted or Not Compiling

Of 95 NC-series programs:
- 39 pass at 100%
- ~15 additional compile and produce output
- ~41 do not compile or are not attempted

Known blockers documented in CLAUDE.md:
- **NC121M**: Subscripted DIVIDE (subscripted operands in DIVIDE GIVING)
- **NC220M**: Infinite loop at runtime (hang)
- **NC201A, NC250A, NC252A**: VALUE THRU in level-88 (grammar gap)
- **NC233A, NC237A, NC238A, NC247A**: ASCENDING/DESCENDING KEY in OCCURS
- **NC211A, NC254A**: STATUS as paragraph name (reserved word conflict)
- **NC215A, NC219A**: PROGRAM as paragraph name (reserved word conflict)

Non-NC series (IC, IF, ST, SQ, RL, DB, CM, OB, SM) are entirely untested -- these cover
inter-program communication, intrinsic functions, sequential I/O, relative I/O, indexed I/O,
debugging, communications, and report writing features.

---

## 4.5 Test Quality Assessment

### Strengths

1. **Diagnostic-driven validation tests.** The semantic unit tests use a clean pattern of
   asserting on specific diagnostic codes (e.g., `AssertHasDiagnostic(diags, "CBL3101")`).
   Each diagnostic code has both positive tests (code is emitted for bad input) and negative
   tests (code is not emitted for valid input). This is disciplined, reviewable, and
   regression-resistant.

2. **Error path coverage in semantics.** The `BoundTreeValidatorTests` (28 methods),
   `SymbolValidatorTests` (16 methods), and `DiagnosticReachabilityTests` (8 methods)
   collectively test error detection for: duplicate symbols, invalid REDEFINES, OCCURS on
   wrong levels, type mismatches in IF/EVALUATE/PERFORM, and arithmetic type violations.
   This is solid error-path coverage for the validation layer.

3. **Runtime edge cases.** `CobolFieldTests` tests truncation, signed fields, and boundary
   conditions. `PicRuntimeMoveTests` covers sign storage conversions between different PIC
   representations (trailing overpunch, trailing separate, leading separate, COMP-3). These
   are the kinds of subtle COBOL semantics that cause real-world bugs.

4. **Integration test breadth.** 169 end-to-end tests cover a wide feature surface. The
   inline COBOL source pattern makes each test self-contained and readable.

5. **DiagnosticNormalization.** The normalization utility for diagnostic messages is a
   thoughtful approach to preventing test fragility from wording changes.

6. **NIST as ground truth.** Using the official NIST CCVS85 suite provides external
   validation independent of the developer's assumptions about COBOL semantics.

### Weaknesses

1. **No unit tests for parsing.** The ANTLR4 grammar and custom error strategy are tested
   only at the integration level. Parse tree shape, error recovery behavior, and grammar
   ambiguity resolution are not tested in isolation. A malformed grammar change could pass
   all existing tests if the affected construct is not covered by integration tests.

2. **No unit tests for code generation.** The CIL emitter (`CilEmitter.cs`) and the binder
   (`Binder.cs`) have zero unit tests. Correctness is validated only by running compiled
   programs. If the emitter generates subtly wrong IL that happens to produce correct output
   for existing test cases, the error would go undetected.

3. **No unit tests for IR lowering.** The intermediate representation layer is not tested
   independently.

4. **No unit tests for flow analysis.** BasicBlock construction, paragraph reachability,
   and PERFORM range checking are untested in isolation.

5. **NIST tests are not in xUnit.** Running NIST tests requires manual invocation of a
   shell script. They are not part of `dotnet test` and therefore not part of any CI/CD
   pipeline. A regression in a passing NIST test would not be caught by the standard test
   runner.

6. **No parameterized NIST integration.** The 39 passing NIST tests could be wired as
   `[Theory]` with `[InlineData]` to make them part of the automated test suite. This is a
   significant gap: the most rigorous tests (NIST) are the least automated.

7. **Limited file I/O unit tests.** `SequentialFileTests` has only 6 tests. Indexed file
   organization, relative file organization, and the runtime's file status handling are
   tested only through integration tests.

8. **Monolithic integration test file.** All 169 integration tests live in a single 4,868-line
   file (`EndToEndTests.cs`). This makes the file difficult to navigate and review. Feature
   grouping into separate test classes (arithmetic, control flow, I/O, etc.) would improve
   maintainability.

9. **No negative compilation tests in integration.** The integration tests almost exclusively
   test successful compilation and execution. Programs that should fail to compile (and produce
   specific errors) are tested only in the unit test layer via `DiagnosticTestBase`.

10. **No performance or stress tests.** There are no benchmarks, no compilation-time tests,
    and no tests for large programs or deeply nested structures.

---

## 4.6 Coverage Gap Summary

| Compiler Phase | Unit Tests | Integration Coverage | Gap Severity |
|----------------|-----------|---------------------|--------------|
| Preprocessor (fixed-form) | 4 tests | Implicit in all NIST tests | Low |
| Preprocessor (COPY) | None | 1 integration test | Medium |
| Preprocessor (NIST) | None | Used in NIST batch only | Low (test utility) |
| Parsing / Grammar | None | All integration tests exercise it | **High** |
| Semantic Analysis / Binding | 89 tests | All integration tests exercise it | Low |
| Flow Analysis | None | Indirect via PERFORM tests | Medium |
| IR Lowering | None | None | Medium |
| Code Generation (CIL) | None | All integration tests exercise it | **High** |
| Runtime (fields/PIC) | 66 tests | All integration tests exercise it | Low |
| Runtime (file I/O) | 6 tests | 12+ integration tests | Medium |
| Runtime (intrinsics) | 30 tests | Limited integration coverage | Low |
| NIST validation | Not in xUnit | 31 tests via shell script | **High** (automation gap) |

### Key Metrics

- **Total test methods**: ~340 (169 integration + 5 error strategy + ~173 unit, including
  InlineData expansions for Theory tests)
- **Lines of test code**: ~6,500 (4,868 integration + ~1,600 unit)
- **NIST coverage**: 39 of 95 NC-series programs (41%), 39 of 459 total programs (8.5%)
- **Skipped tests**: 1 (`CallStatement_EmitsDiagnostic` -- CALL not yet lowered to CIL)
- **Compiler phases with zero unit tests**: 5 (parsing, code gen, IR, flow analysis, COPY preprocessing)
