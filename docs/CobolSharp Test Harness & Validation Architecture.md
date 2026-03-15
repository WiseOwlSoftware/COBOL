CobolSharp Test Harness & Validation Architecture (CIL‑Only)
===========================================================

High‑level goals
----------------
- Validate every subsystem of the CobolSharp compiler and runtime using a deterministic, automated, and reproducible test framework.
- Ensure correctness of:
  - Preprocessor
  - Lexer
  - Parser
  - Semantic analyzer
  - IL generator
  - Optimization pipeline
  - CIL backend
  - Runtime library
  - Debugger integration
- Guarantee COBOL‑85 → COBOL‑2023 conformance.
- Remove all VM‑related tests; CIL is the single execution target.

Test suite layers
-----------------
The test harness is composed of:

1. Unit tests
2. Golden tests
3. Integration tests
4. Cross‑compiler tests
5. Fuzzing tests
6. Performance tests
7. Conformance tests (ISO/IEC 1989:2023)
8. Regression suite

Each layer validates a different part of the system.

Directory layout
----------------
tests/
  unit/
    preprocessor/
    lexer/
    parser/
    semantic/
    runtime/
  golden/
    il/
    diagnostics/
    data-layout/
  integration/
    end-to-end/
    backend-cil/
  cross-compiler/
    gnu-cobol/
    micro-focus/
  fuzz/
    lexer/
    parser/
    semantic/
  performance/
    large-programs/
  conformance/
    iso-tests/
  regression/
    bugs/
    edge-cases/

Unit tests
----------
Focused tests for individual components.

Preprocessor tests:
- COPY expansion
- REPLACE rules
- Pseudo-text handling
- Nested COPY
- COPY REPLACING inside COPY

Lexer tests:
- Tokenization of all COBOL constructs
- Lexer modes (comment, copy, pseudo-text)
- Continuation lines
- Fixed-form vs free-form

Parser tests:
- Grammar rule coverage
- Error recovery behavior
- Dialect gating (85/2002/2014/2023)

Semantic tests:
- Symbol table construction
- Type binding
- Data description tree
- REDEFINES, RENAMES, OCCURS
- PERFORM graph
- OO/generics binding
- File I/O semantics

Runtime tests:
- NumericEngine (packed decimal, rounding, size error)
- FileManager (sequential, indexed, relative)
- SortMergeEngine
- JsonEngine / XmlEngine
- StringEngine
- DateTimeEngine
- CollationEngine

Golden tests
------------
Golden tests compare actual output to expected output stored in files.

Examples:
- Golden IL output
- Golden optimized IL
- Golden diagnostics
- Golden runtime output

Structure:
program.cbl  
expected.il  
expected.optimized.il  
expected.output  
expected.diagnostics  

Integration tests
-----------------
Full end‑to‑end tests:

1. Preprocess
2. Lex
3. Parse
4. Semantic analysis
5. IL generation
6. Optimization
7. CIL backend codegen
8. Execute under .NET
9. Compare output to expected

Covers:
- Real COBOL programs
- Multi‑file projects
- COPYbook‑heavy codebases
- OO programs
- JSON/XML programs
- Generics programs

Cross‑compiler tests
--------------------
Validates CobolSharp behavior against other COBOL compilers:

- GnuCOBOL
- Micro Focus (if available)

Process:
- Compile same COBOL program with both compilers
- Compare outputs
- Compare diagnostics (where applicable)
- Compare data layout (PIC/USAGE/OCCURS)

Ensures CobolSharp matches industry behavior.

Fuzzing tests
-------------
Randomized input generation to stress‑test robustness.

Lexer fuzzing:
- Random characters
- Random COBOL‑like tokens
- Random COPY/REPLACE patterns

Parser fuzzing:
- Random token streams
- Randomly mutated COBOL programs

Semantic fuzzing:
- Random data descriptions
- Random PERFORM graphs
- Random OO/generic constructs

Goals:
- Crash resistance
- Error recovery validation
- No infinite loops
- No unbounded memory growth

Performance tests
-----------------
Measure:
- Preprocessor throughput
- Lexer throughput
- Parser throughput
- Semantic analysis speed
- IL generation speed
- CIL backend codegen speed
- Runtime performance

Large‑program benchmarks:
- 10k‑line COBOL programs
- 100k‑line COBOL programs
- COPYbook‑heavy workloads
- Deep OO hierarchies
- Large OCCURS tables

Conformance tests
-----------------
Tests derived from ISO/IEC 1989:2023:

- Syntax conformance
- Semantic conformance
- Data description conformance
- File I/O conformance
- Exception handling conformance
- JSON/XML conformance
- Generics conformance

Regression suite
----------------
Every fixed bug gets a test.

Structure:
tests/regression/bug‑####/  
  input.cbl  
  expected.output  
  expected.diagnostics  

Ensures:
- No regressions
- No reintroduced bugs
- Stable behavior across releases

Test harness engine
-------------------
The harness orchestrates:

- Preprocessing
- Lexing
- Parsing
- Semantic analysis
- IL generation
- Optimization
- CIL backend codegen
- Execution under .NET
- Output capture
- Diffing against expected results

Features:
- Parallel execution
- Snapshot updating (with approval)
- Detailed diffing (line‑by‑line, token‑by‑token)
- Reproducible test environments

Debugging support
-----------------
When a test fails:
- Show diff of expected vs actual
- Show IL dump
- Show CFG dump
- Show data layout tree
- Show symbol table
- Show preprocessor expansion
- Show CIL disassembly
- Show runtime state (file handles, storage blocks)

Continuous integration
----------------------
CI pipeline runs:
- Unit tests
- Golden tests
- Integration tests
- Regression tests
- Conformance tests
- Performance smoke tests

On:
- Every commit
- Every PR
- Nightly builds

Summary
-------
The CobolSharp test harness:
- Validates every subsystem of the compiler and runtime
- Uses unit, golden, integration, fuzzing, and conformance tests
- Ensures correctness, stability, and regression protection
- Provides deep debugging tools
- Scales to large COBOL codebases
- Guarantees long‑term reliability of the compiler
- Is fully aligned with the CIL‑only architecture
