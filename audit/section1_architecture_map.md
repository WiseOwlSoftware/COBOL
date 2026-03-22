# CobolSharp Architecture Map

## Solution Structure

```
CobolSharp.sln
  src/
    CobolSharp.CLI/          -- Command-line driver (compile, preprocess subcommands)
    CobolSharp.Compiler/     -- All compiler phases (lexer through emitter)
    CobolSharp.Runtime/      -- Runtime library linked into compiled programs
  tests/
    CobolSharp.Tests.Unit/
    CobolSharp.Tests.Integration/
```

## Compilation Pipeline

The `Compilation` class (`src/CobolSharp.Compiler/Compilation.cs`) is the top-level facade.
It wires six sequential phases:

```
Source text
  -> Phase 0: Preprocess (reference-format normalization, COPY expansion, NIST fixups)
  -> Phase 1: Lex + Parse (ANTLR4 lexer/parser -> CST)
  -> Phase 2: Grammar invariant validation (debug)
  -> Phase 3: Semantic analysis (SemanticBuilder -> SemanticModel)
  -> Phase 4: Validation passes (paragraphs, storage layout, data items, file status, symbols)
  -> Phase 5: Bind (BoundTreeBuilder -> BoundProgram; Binder -> IrModule)
  -> Phase 6: CIL emission (CilEmitter -> .NET assembly via Mono.Cecil)
```

---

## Subsystem Details

### 1. Preprocessor

**Responsibilities.** Normalizes COBOL fixed-format (columns 1-6, 7, 8-72) to free-form text.
Expands COPY/REPLACING statements by locating copybooks on a configurable search path.
Optionally applies NIST test-suite substitutions (XXXXX### placeholders).

**Key types:**
- `ReferenceFormatProcessor` -- `src/CobolSharp.Compiler/Preprocessor/ReferenceFormatProcessor.cs`
- `CopyProcessor` -- `src/CobolSharp.Compiler/Preprocessor/CopyProcessor.cs`
- `NistPreprocessor` -- `src/CobolSharp.Compiler/Preprocessor/NistPreprocessor.cs`

**Data flow:** Raw source file (string) -> preprocessed free-form text (string).

---

### 2. Lexer and Parser (Grammar)

**Responsibilities.** Tokenizes preprocessed COBOL source via an ANTLR4-generated lexer and
parses it into a concrete syntax tree (CST). The grammar is split across multiple .g4 files
for modularity. A custom error listener and error strategy provide COBOL-specific diagnostics.
`CobolParserCoreBase` adds semantic predicates (dialect gating, paragraph-name disambiguation).

**Key types:**
- `CobolLexer` (generated) -- `src/CobolSharp.Compiler/Generated/CobolLexer.cs`
- `CobolParserCore` (generated) -- `src/CobolSharp.Compiler/Generated/CobolParserCore.cs`
- `CobolParserCoreBase` -- `src/CobolSharp.Compiler/Parsing/CobolParserCoreBase.cs`
- `CobolErrorListener` -- `src/CobolSharp.Compiler/Parsing/CobolErrorListener.cs`
- `CobolErrorStrategy` -- `src/CobolSharp.Compiler/Parsing/CobolErrorStrategy.cs`

**Grammar files** (in `src/CobolSharp.Compiler/Grammar/`):
`CobolLexer.g4`, `CobolParserCore.g4`, `CobolPreprocessor.g4`, `CobolDialect.g4`,
`CobolParserOO.g4`, `CobolParserJsonXml.g4`, `CobolParserGenerics.g4`

**Data flow:** Preprocessed text (string) -> `CompilationUnitContext` (ANTLR CST root).

---

### 3. Semantic Analysis

**Responsibilities.** Walks the CST to collect all symbol declarations (data items, paragraphs,
sections, files), resolves PIC/USAGE to concrete types, computes storage layout (byte offsets
and sizes for all data items), and validates structural constraints (paragraph references,
data-item classification, file status consistency, symbol resolution).

**Key types:**
- `SemanticBuilder` -- `src/CobolSharp.Compiler/Semantics/SemanticBuilder.cs`
  Visitor pass that collects symbols and builds the `DataSymbol` tree from COBOL level numbers.
- `SemanticModel` -- `src/CobolSharp.Compiler/Semantics/SemanticModel.cs`
  Central read-only surface consumed by later phases. Holds `ProgramSymbol`, `SymbolTable`,
  PIC environment, implementor switches.
- `SymbolTable` / `Symbol` / `DataSymbol` -- `src/CobolSharp.Compiler/Semantics/SymbolTable.cs`,
  `Symbol.cs`, `DataSymbol.cs`
- `PicUsageResolver` -- `src/CobolSharp.Compiler/Semantics/PicUsageResolver.cs`
- `StorageLayoutComputer` -- `src/CobolSharp.Compiler/Semantics/StorageLayoutComputer.cs`
- `ReferenceResolver` -- `src/CobolSharp.Compiler/Semantics/ReferenceResolver.cs`
- Validators: `ParagraphValidator`, `DataItemClassifier`, `FileStatusValidator`,
  `SymbolValidator` -- all in `src/CobolSharp.Compiler/Semantics/`

**Data flow:** `CompilationUnitContext` (CST) -> `SemanticModel` (symbols, types, layout).

---

### 4. Bound Tree (Typed AST)

**Responsibilities.** Walks the CST a second time with the resolved `SemanticModel` to produce
a fully typed, symbol-resolved bound tree. Every statement and expression node carries resolved
symbols and COBOL category/type information. After construction, `BoundTreeValidator` enforces
expression type constraints and `ProcedureGraph` performs flow analysis.

**Key types:**
- `BoundTreeBuilder` -- `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs`
  ANTLR visitor that builds `BoundProgram` from CST + `SemanticModel`.
- `BoundNode` (abstract base) -- `src/CobolSharp.Compiler/Semantics/Bound/BoundNodes.cs`
- `BoundExpression` -- abstract base; subclasses: `BoundLiteralExpression`,
  `BoundIdentifierExpression`, `BoundBinaryExpression`, `BoundFigurativeExpression`,
  `BoundReferenceModificationExpression`, `BoundConditionNameExpression`
- `BoundStatement` -- abstract base; ~25 concrete statement types (Move, Perform, If,
  Display, Read, Write, Evaluate, Arithmetic, Search, String, Unstring, Call, etc.)
- `BoundTreeValidator` -- `src/CobolSharp.Compiler/Semantics/Bound/BoundTreeValidator.cs`
- `CorrespondingMatcher` -- `src/CobolSharp.Compiler/Semantics/Bound/CorrespondingMatcher.cs`

**Flow analysis:**
- `ProcedureGraph` -- `src/CobolSharp.Compiler/Semantics/ProcedureGraph.cs`
- `ParagraphReachabilityAnalyzer` -- `src/CobolSharp.Compiler/FlowAnalysis/ParagraphReachabilityAnalyzer.cs`
- `PerformRangeChecker` -- `src/CobolSharp.Compiler/FlowAnalysis/PerformRangeChecker.cs`

**Data flow:** `CompilationUnitContext` + `SemanticModel` -> `BoundProgram` (typed AST).

---

### 5. IR / Lowering (Binder)

**Responsibilities.** Lowers the `BoundProgram` into a CIL-oriented intermediate representation.
Each COBOL paragraph becomes an `IrMethod` returning an int (program counter for GO TO dispatch).
The main entry point dispatches paragraphs via `while (pc >= 0) pc = paragraphs[pc]()`.
Builds `IrRecordType` definitions with explicit byte offsets mirroring COBOL's contiguous
storage model. Uses a basic-block CFG: each `IrBasicBlock` holds a linear instruction sequence
terminated by a branch or return.

**Key types:**
- `Binder` -- `src/CobolSharp.Compiler/CodeGen/Binder.cs`
  Orchestrates BoundTree -> IR lowering. Manages paragraph indexing, PERFORM exit stacks,
  sentence boundaries.
- `RecordLayoutBuilder` -- `src/CobolSharp.Compiler/CodeGen/RecordLayoutBuilder.cs`
- `StorageLocation` -- `src/CobolSharp.Compiler/CodeGen/StorageLocation.cs`
- `IrModule` -- `src/CobolSharp.Compiler/IR/IrModule.cs`
  Top-level container: types, methods, globals.
- `IrMethod` / `IrBasicBlock` -- `src/CobolSharp.Compiler/IR/IrMethod.cs`
- `IrInstruction` (abstract base) -- `src/CobolSharp.Compiler/IR/IrInstruction.cs`
  ~20 concrete instructions: `IrMove`, `IrLoadField`, `IrStoreField`,
  `IrPerformInlineTimes`, branch/jump/return, arithmetic, I/O calls, etc.
- `IrType` / `IrRecordType` / `IrPrimitiveType` / `IrField` -- `src/CobolSharp.Compiler/IR/IrType.cs`

**Data flow:** `BoundProgram` + `SemanticModel` -> `IrModule` (basic-block CFG with typed instructions).

---

### 6. CIL Emitter

**Responsibilities.** Translates an `IrModule` into a runnable .NET assembly using Mono.Cecil.
Maps IR record types to explicit-layout structs with `[FieldOffset]` attributes, IR methods to
static CIL methods on a program class, and IR instructions to IL opcodes. Creates a static
`ProgramState` field (holding WORKING-STORAGE and FILE SECTION byte arrays) initialized in
the static constructor. Sets the assembly entry point to the generated `Main` method.

**Key types:**
- `CilEmitter` -- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs`
  Static `EmitAssembly(IrModule, name, semanticModel)` creates an `AssemblyDefinition`.

**Data flow:** `IrModule` + `SemanticModel` -> `AssemblyDefinition` (Mono.Cecil) -> .dll on disk.

---

### 7. Runtime Library

**Responsibilities.** Provides the runtime services that compiled COBOL programs link against.
Implements COBOL data semantics (PIC-based field access, MOVE with truncation/padding,
decimal arithmetic), file I/O (sequential, indexed), ACCEPT, INSPECT, and DISPLAY.
Each compiled program's generated class calls static methods on these runtime types.

**Key types:**
- `CobolProgram` -- `src/CobolSharp.Runtime/CobolProgram.cs`
  Abstract base class for compiled programs. Provides `Display()`, `MoveNumeric()`,
  `MoveAlphanumeric()`.
- `ProgramState` / `StorageArea` -- `src/CobolSharp.Runtime/StorageArea.cs`
  Byte-array-backed storage areas (WORKING-STORAGE, FILE SECTION).
- `PicDescriptor` / `PicDescriptorFactory` -- `src/CobolSharp.Runtime/PicDescriptor.cs`,
  `PicDescriptorFactory.cs`
  PIC clause interpretation: digit counts, decimal positions, sign handling.
- `PicRuntime` -- `src/CobolSharp.Runtime/PicRuntime.cs`
  Core MOVE/arithmetic/comparison logic operating on byte-array fields.
- `FileRuntime` -- `src/CobolSharp.Runtime/FileRuntime.cs`
  OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START for sequential and indexed files.
- `InspectRuntime` -- `src/CobolSharp.Runtime/InspectRuntime.cs`
- `AcceptRuntime` -- `src/CobolSharp.Runtime/AcceptRuntime.cs`

---

### 8. Cross-Cutting: Diagnostics

**Responsibilities.** Uniform diagnostic reporting across all phases. Each diagnostic carries
a descriptor (code like CBL0601), severity (error/warning/info), and optional source location.

**Key types:**
- `Diagnostic` -- `src/CobolSharp.Compiler/Diagnostics/Diagnostic.cs`
- `DiagnosticBag` -- `src/CobolSharp.Compiler/Diagnostics/DiagnosticBag.cs`
- `DiagnosticDescriptors` -- `src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs`
- `DiagnosticSeverity` -- `src/CobolSharp.Compiler/Diagnostics/DiagnosticSeverity.cs`

---

## End-to-End Data Flow Summary

```
COBOL source file (.cob)
  |
  v
[ReferenceFormatProcessor] -> free-form text
  |
  v
[CopyProcessor] -> COPY-expanded text
  |
  v
[CobolLexer] -> token stream
  |
  v
[CobolParserCore] -> CompilationUnitContext (CST)
  |
  v
[SemanticBuilder] -> SemanticModel (symbols, types, layout)
  |
  v
[BoundTreeBuilder] -> BoundProgram (typed AST)
  |
  v
[Binder] -> IrModule (basic-block CFG)
  |
  v
[CilEmitter] -> AssemblyDefinition (Mono.Cecil)
  |
  v
.NET assembly (.dll)
```
