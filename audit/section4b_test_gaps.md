# Section 4b: Test Suite Analysis and Gap Inventory

## Test Framework

**xUnit** (via `xunit` and `xunit.runner.visualstudio` NuGet packages). Both test projects
reference `Microsoft.NET.Test.Sdk`. No NUnit or MSTest usage anywhere.

## Test Project Structure

```
tests/
  CobolSharp.Tests.Unit/          217 passing tests
    Common/
      SourceTextTests.cs           8 [Fact]
    Preprocessor/
      FixedFormTests.cs            4 [Fact]
    Runtime/
      CobolFieldTests.cs          16 [Fact]
      IntrinsicFunctionTests.cs   30 [Fact]
      Comp5Tests.cs               16 [Fact], 1 [Theory] (6 InlineData)
      PicRuntimeMoveTests.cs      20 [Fact]
      SequentialFileTests.cs       6 [Fact]
    Semantics/
      ArithmeticDiagnosticTests.cs     4 [Fact]
      BoundTreeValidatorTests.cs      28 [Fact]
      CategoryCompatibilityTests.cs    7 [Fact], 6 [Theory]
      DataItemClassifierTests.cs      12 [Fact]
      DiagnosticReachabilityTests.cs   8 [Fact]
      DiagnosticTestBase.cs           (base class, no test methods)
      ExpressionTypeTests.cs           8 [Fact]
      SymbolValidatorTests.cs         16 [Fact]
  CobolSharp.Tests.Integration/   184 passing, 1 skip
    EndToEndTests.cs             177 [Fact], 3 [Theory]
    CobolErrorStrategyTests.cs     2 [Fact], 3 [Theory]
```

**Totals**: 346 `[Fact]` + 9 `[Theory]` = 355 test methods across 16 files.

## Unit Test Inventory by Compiler Phase

| Phase | File(s) | Tests | Coverage Focus |
|-------|---------|-------|----------------|
| **Preprocessing** | FixedFormTests | 4 | Fixed-form column handling |
| **Source model** | SourceTextTests | 8 | Source text infrastructure |
| **Runtime: fields** | CobolFieldTests | 16 | PIC field storage/access |
| **Runtime: moves** | PicRuntimeMoveTests | 20 | MOVE semantics between PIC types |
| **Runtime: intrinsics** | IntrinsicFunctionTests | 30 | ABS, SQRT, MOD, MAX, MIN, etc. |
| **Runtime: I/O** | SequentialFileTests | 6 | Sequential file operations |
| **Semantics: binding** | BoundTreeValidatorTests | 28 | IF/PERFORM/EVALUATE type enforcement |
| **Semantics: symbols** | SymbolValidatorTests | 16 | Symbol table diagnostics |
| **Semantics: types** | ExpressionTypeTests, CategoryCompatibilityTests | 15 | Type classification and compatibility |
| **Semantics: data** | DataItemClassifierTests | 12 | Data item classification |
| **Semantics: diagnostics** | ArithmeticDiagnosticTests, DiagnosticReachabilityTests | 12 | Arithmetic and reachability diagnostics |

**Observation**: Unit tests are concentrated in runtime (72 tests, 37%) and semantic validation
(83 tests, 43%). Parser/grammar coverage is tested only indirectly through integration tests.
The preprocessor has minimal coverage (4 tests).

## Integration Test Approach

`EndToEndTests.cs` (4868 lines, 169 `[Fact]`, 3 `[Theory]`) is a single monolithic file
that compiles inline COBOL source via `Compilation.Compile()`, runs the resulting DLL as a
subprocess (`dotnet <output.dll>`), and asserts on stdout. Each test creates a temp directory,
writes source, compiles, executes, and checks output. This is a true end-to-end pattern:
source text in, program output out.

`CobolErrorStrategyTests.cs` (5 tests) validates error recovery behavior during parsing.

The integration suite is the primary correctness guarantee for the compiler. At 172 tests
it covers a wide range of COBOL features, but all tests live in a single file with no
organizational subdivision by COBOL feature area.

## NIST CCVS85 Compliance Status

**95 NC-series programs** are extracted in `tests/nist/programs/`. The full NIST corpus
contains 459 `.cob` files across all modules (NC, IC, CM, DB, IF, etc.).

**39 of 95 NC tests pass at 100%** (41% pass rate). Passing tests:

- Nucleus arithmetic: NC101A-NC107A, NC111A, NC112A, NC115A-NC120A, NC122A-NC124A,
  NC126A, NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A
- Nucleus data/control: NC170A-NC173A, NC175A-NC177A
- Nucleus level-88/conditions: NC202A, NC203A, NC206A, NC207A, NC210A
- Nucleus tables: NC221A, NC222A, NC224A, NC239A-NC241A, NC248A, NC251A, NC253A

**Known blockers for remaining NC tests**:
- NC121M: subscripted operands in DIVIDE GIVING
- NC220M: infinite loop at runtime
- NC201A, NC250A, NC252A: VALUE THRU grammar gap for level-88
- NC233A, NC237A, NC238A, NC247A: ASCENDING/DESCENDING KEY in OCCURS
- Reserved word conflicts preventing STATUS/PROGRAM as paragraph names

**NIST tooling**: `scripts/nist-batch.sh` provides parallel batch execution with CSV
summary and failure logging. `tools/extract-nist.sh` extracts individual programs from
`newcob.val`. The batch runner is configured for Windows paths (`E:/CobolSharp`) and is
not integrated into `dotnet test`.

## Missing Test Categories

### No dedicated tests exist for:

1. **Parser/grammar unit tests** -- No direct AST assertion tests. Parsing is only tested
   through end-to-end compilation. Regressions in parse tree structure are invisible until
   they cause downstream failures.

2. **Negative compilation tests** -- `CobolErrorStrategyTests` has 5 tests for parse errors.
   No systematic coverage of invalid programs that should produce specific diagnostics
   (e.g., type mismatches, undefined references, duplicate definitions).

3. **IR/code generation** -- No unit tests for the intermediate representation layer. The
   bound tree to IL translation is only tested via end-to-end tests.

4. **COPY/REPLACE preprocessing** -- No tests for copybook inclusion or REPLACE directives.
   Only 4 fixed-form tests exist for the preprocessor.

5. **File I/O beyond sequential** -- Indexed and relative file operations have no dedicated
   test coverage. `SequentialFileTests` has 6 tests.

6. **Inter-program CALL** -- **RESOLVED**: CALL fully implemented with Entry methods,
   CobolProgramRegistry, BY REFERENCE/CONTENT/VALUE, RETURNING, INITIAL, CANCEL, ENTRY,
   dynamic CALL. Integration tests cover basic CALL semantics.

7. **SORT/MERGE** -- Parse-only; no binding, validation, or runtime tests.

8. **Decimal precision edge cases** -- No tests for ON SIZE ERROR, ROUNDED,
   intermediate result precision, or 18-digit arithmetic boundaries.

9. **Reference modification** -- Likely tested in integration but no dedicated unit tests
   for `WS-FIELD(start:length)` semantics.

10. **INSPECT/STRING/UNSTRING** -- Complex string manipulation verbs have no isolated tests.

### Integration test structural gaps:

- All 172 tests in a single 4868-line file with no categorization
- No test tagging/traits for selective execution by feature area
- No performance/timeout tests (the 10-second WaitForExit is the only guard)
- NIST batch runner is not wired into CI (`dotnet test` does not run NIST)

## Proposed Canonical Test Layout

```
tests/
  CobolSharp.Tests.Unit/
    Common/                          (source infrastructure)
    Preprocessor/
      FixedFormTests.cs
      CopyReplaceTests.cs            NEW - copybook and REPLACE
    Parser/
      ArithmeticParseTests.cs        NEW - AST assertions
      ControlFlowParseTests.cs       NEW
      DataDivisionParseTests.cs      NEW
    Semantics/
      (existing files)
      NegativeDiagnosticTests.cs     NEW - invalid programs, expected errors
    Runtime/
      (existing files)
      DecimalPrecisionTests.cs       NEW - SIZE ERROR, ROUNDED
      IndexedFileTests.cs            NEW
      StringVerbTests.cs             NEW - INSPECT/STRING/UNSTRING
    CodeGen/
      IrEmissionTests.cs             NEW - bound tree to IL translation

  CobolSharp.Tests.Integration/
    Arithmetic/                      Split from EndToEndTests.cs
      BasicArithmeticTests.cs
      DecimalArithmeticTests.cs
    ControlFlow/
      PerformTests.cs
      GoToTests.cs
      EvaluateTests.cs
    DataHandling/
      MoveTests.cs
      TableTests.cs
      Level88Tests.cs
    FileIO/
      SequentialTests.cs
      IndexedTests.cs
    ErrorRecovery/
      CobolErrorStrategyTests.cs

  CobolSharp.Tests.Nist/             NEW project
    NistTestRunner.cs                Parameterized [Theory] over NC programs
    NistComplianceTests.cs           One [Fact] per NC test, with [Trait("NIST","NC")]
```

**Key recommendations**:
1. Split `EndToEndTests.cs` (4868 lines) into feature-area files using `[Trait]` attributes
2. Create a `CobolSharp.Tests.Nist` project that wraps the batch runner as xUnit tests,
   enabling `dotnet test --filter "NIST"` execution and CI integration
3. Add parser-level unit tests that assert on AST node types without full compilation
4. Build a negative test suite: one test per `CBLxxxx` diagnostic code, verifying the
   message text and source location
5. Fix the NIST batch runner path from `E:/CobolSharp` to a relative or configurable path
