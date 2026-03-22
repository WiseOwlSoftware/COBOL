# Section 1: Executive Summary & Project Overview

## 1.1 What CobolSharp Is

CobolSharp is a COBOL-to-.NET compiler written in C# that compiles COBOL source programs into
.NET assemblies (CIL bytecode). The compiler targets **.NET 9** (SDK 9.0.312) and uses **C# 13**
language features. It is authored by Brent Rector and licensed under the MIT license.

The project's stated goal is to build a production-quality COBOL compiler that fully implements
the **ISO/IEC 1989:2023** standard (the current COBOL specification, spanning 1,261 pages and
2,090 sections). The primary use case is COBOL modernization: allowing organizations to compile
existing COBOL business logic into standard .NET assemblies (.dll / .exe) that can be consumed
directly from C#, F#, or other .NET languages without interop boundaries.

## 1.2 Current State

As of 2026-03-21, the project is on the **nist-phase-d** branch. Test status:

| Test Category       | Count  | Status          |
|---------------------|--------|-----------------|
| Integration tests   | 176    | Pass (1 skip)   |
| Unit tests          | 195    | Pass            |
| NIST COBOL-85 tests | 39     | 100% pass       |

The 39 passing NIST tests span the NC1xx (nucleus) and NC2xx (nucleus extended) families,
covering core language features: data definitions, MOVE semantics, arithmetic statements,
conditional expressions, PERFORM control flow, I/O statements, and level-88 conditions.

Known gaps at the time of this audit include:

- CALL/USING/RETURNING is bound and validated but the IR backend is a stub (no inter-program
  linkage).
- SORT/MERGE is parsed but not semantically analyzed or code-generated.
- Alternate keys, VALUE THRU in level-88, and ASCENDING/DESCENDING KEY in OCCURS are not yet
  implemented.
- Two NIST tests remain non-passing: NC121M (subscripted operands in DIVIDE GIVING) and NC220M
  (runtime infinite loop).

## 1.3 Key Technical Decisions

The project documents five key technical decisions (KTDs). Each was evaluated against alternatives
with explicit tradeoff analysis.

### KTD-1: Target Platform -- .NET (CIL)

.NET was chosen over LLVM IR, JVM bytecode, and native code generation. The primary rationale is
.NET's built-in `decimal` type: a 128-bit base-10 numeric type that maps directly to COBOL's
base-10 fixed-point arithmetic. On LLVM or native targets, the project would need to build or
integrate a decimal arithmetic library. The .NET ecosystem also provides the strongest interop
story for the modernization use case (compiled COBOL assemblies are standard .NET DLLs).

### KTD-2: Implementation Language -- C#

C# was selected over F#, Rust, and C/C++. The rationale is ecosystem alignment: the compiler
itself runs on the same platform it targets, yielding a single build system and debug experience.
Roslyn (the C# compiler, itself written in C#) serves as an architectural reference. F# was
considered for its superior pattern matching and discriminated unions but was rejected on
contributor accessibility grounds.

### KTD-3: Parser Strategy -- Hand-Written Recursive Descent (with ANTLR Lexer)

A hand-written recursive descent parser was chosen over ANTLR-generated parsers, PEG parsers,
and LALR tools. COBOL's context-sensitive grammar -- PICTURE clauses that reuse operator
characters, fixed-form reference format with column-dependent semantics, COPY/REPLACE
preprocessing, and implicit scope terminators -- breaks most parser generators. The actual
implementation uses a hybrid approach: **ANTLR 4.13.2 generates the lexer and a parse tree**
from grammar files (`CobolLexer.g4`, `CobolParserCore.g4`), while semantic tree construction
and context-sensitive disambiguation are handled by hand-written C# code in the `Parsing/`
layer (including a custom `CobolParserCoreBase`, error listener, and error strategy).

### KTD-4: CIL Emission -- Mono.Cecil

Mono.Cecil was chosen over System.Reflection.Emit, IKVM.Reflection, text-based IL emission, and
Roslyn-based C# transpilation. Mono.Cecil provides an object-model API for constructing .NET
assemblies, supports writing to disk, and generates portable PDB files for source-level debugging.
The transpile-to-C# approach was rejected because COBOL control flow constructs (GO TO, ALTER,
PERFORM THRU) have no clean mapping to C# source code.

### KTD-5: Numeric Representation -- Dual-Layer

The compiler uses a dual-layer numeric representation: `byte[]` arrays for storage and .NET
`decimal` for computation. This is required because COBOL programs routinely perform byte-level
operations on numeric fields (REDEFINES, group MOVE, INSPECT on packed decimal storage) that
cannot be represented by `decimal` alone, while arithmetic operations benefit from `decimal`'s
built-in base-10 precision. The marshal/unmarshal cost on arithmetic operations is the accepted
tradeoff.

## 1.4 Architecture Overview

The compiler follows a classical multi-phase pipeline:

```
COBOL Source (.cob / .cbl)
        |
        v
  1. Preprocessor        COPY statement expansion, REPLACE directives,
                          fixed-form reference format handling
        |
        v
  2. Lexer                ANTLR-generated tokenizer from CobolLexer.g4
        |
        v
  3. Parser               ANTLR-generated parse tree from CobolParserCore.g4,
                          with hand-written base class for context-sensitive rules
        |
        v
  4. Semantic Analysis    SemanticBuilder constructs SemanticModel with SymbolTable;
                          type checking, PIC validation, name resolution, data
                          hierarchy, scope analysis; produces bound tree
        |
        v
  5. IR Generation        Binder lowers bound tree to IR (IrModule / IrMethod /
                          IrInstruction); flow analysis via ProcedureGraph
        |
        v
  6. CIL Code Gen         CilEmitter writes .NET assembly via Mono.Cecil,
                          RecordLayoutBuilder maps COBOL data to .NET storage
        |
        v
  .NET Assembly (.dll / .exe)
```

### Solution Structure

The solution (`CobolSharp.sln`) contains five projects:

| Project                            | Role                                                    |
|------------------------------------|---------------------------------------------------------|
| `CobolSharp.Compiler`              | Core compiler: preprocessing, lexing, parsing, semantic analysis, IR, CIL emission |
| `CobolSharp.Runtime`               | Runtime support library linked into compiled assemblies |
| `CobolSharp.CLI`                   | Command-line interface for invoking the compiler        |
| `CobolSharp.Tests.Unit`            | 195 unit tests targeting compiler internals             |
| `CobolSharp.Tests.Integration`     | 176 integration tests compiling and executing COBOL programs end-to-end |

The compiler project is organized into the following internal modules:

- **Preprocessor/**: `CopyProcessor`, `NistPreprocessor`, `ReferenceFormatProcessor` -- handles
  COPY expansion, NIST test harness preprocessing, and fixed-form/free-form source format.
- **Grammar/**: ANTLR grammar files (`CobolLexer.g4`, `CobolParserCore.g4`).
- **Generated/**: ANTLR-generated C# lexer and parser (build-time artifact).
- **Parsing/**: `CobolParserCoreBase`, `CobolErrorListener`, `CobolErrorStrategy` -- hand-written
  parser support for context-sensitive disambiguation and error recovery.
- **Semantics/**: `SemanticBuilder`, `SemanticModel`, `SymbolTable`, `TypeSystem`,
  `ArithmeticTypeSystem`, `PicUsageResolver`, `StorageLayoutComputer`, `ReferenceResolver`,
  `FieldSizeCalculator`, `DataItemClassifier`, `ProcedureGraph`, and the `Bound/` subtree
  containing bound tree node types and the `BoundTreeValidator`.
- **IR/**: `IrModule`, `IrMethod`, `IrInstruction`, `IrType` -- intermediate representation
  between the semantic model and CIL emission.
- **CodeGen/**: `Binder` (lowers semantic model to IR), `CilEmitter` (emits CIL via Mono.Cecil),
  `RecordLayoutBuilder` (maps COBOL record structures to .NET memory layout), `StorageLocation`.
- **Diagnostics/**: Compiler warning and error infrastructure.
- **FlowAnalysis/**: Control flow analysis.
- **Common/**: Shared utilities.

### Build Configuration

- Target framework: `net9.0`
- Language version: C# 13
- Nullable reference types: enabled globally
- Warnings treated as errors: enabled globally (`TreatWarningsAsErrors`)
- ANTLR parser regeneration is integrated into the MSBuild pipeline via a PowerShell script
  (`GenerateIfNewer.ps1`) that runs before build when grammar files are newer than generated
  output.

## 1.5 ISO Standard Coverage

The compiler targets **ISO/IEC 1989:2023**, the current edition of the COBOL standard. Validation
against the standard is measured primarily through the **NIST COBOL-85 test suite**, which
provides a well-established conformance baseline. The 39 currently passing NIST tests cover
nucleus-level features defined in the standard's core sections.

The project plans to continue expanding NIST coverage through systematic sweeps (the current
branch name, `nist-phase-d`, reflects this phased approach). Full ISO/IEC 1989:2023 conformance
remains the long-term objective, with features like inter-program communication (CALL), SORT/MERGE,
report writer, and alternate record keys on the implementation roadmap.
