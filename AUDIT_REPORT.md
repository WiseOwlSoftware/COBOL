# CobolSharp Compiler Audit Report

**Date:** 2026-03-22
**Branch:** nist-phase-d
**Auditor:** Claude (automated deep audit)

---

## Table of Contents

1. [Architecture Map](#1-architecture-map)
2. [Feature Coverage](#2-feature-coverage)
   - 2a. Divisions, Data Description, Numeric Behavior
   - 2b. Control Flow, Statements, Expressions
   - 2c. File I/O, CALL, Diagnostics
3. [Code Quality & Patterns](#3-code-quality--patterns)
4. [Testing & Validation](#4-testing--validation)
   - 4a. Validator and Diagnostic Gaps
   - 4b. Test Suite Analysis
5. [Remediation Plan](#5-remediation-plan)
   - 5a. Phases 1–3 (Grammar, Semantics, Lowering)
   - 5b. Phases 4–5 (CIL Emission, Test Suite)
6. [Execution Protocol & Roadmap](#6-execution-protocol--roadmap)

---

# 1. Architecture Map

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
- `SemanticModel` -- `src/CobolSharp.Compiler/Semantics/SemanticModel.cs`
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
- `BoundNode` (abstract base) -- `src/CobolSharp.Compiler/Semantics/Bound/BoundNodes.cs`
- `BoundExpression` -- abstract base; subclasses: `BoundLiteralExpression`,
  `BoundIdentifierExpression`, `BoundBinaryExpression`, `BoundFigurativeExpression`,
  `BoundReferenceModificationExpression`, `BoundConditionNameExpression`
- `BoundStatement` -- abstract base; ~25 concrete statement types
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
- `RecordLayoutBuilder` -- `src/CobolSharp.Compiler/CodeGen/RecordLayoutBuilder.cs`
- `StorageLocation` -- `src/CobolSharp.Compiler/CodeGen/StorageLocation.cs`
- `IrModule` -- `src/CobolSharp.Compiler/IR/IrModule.cs`
- `IrMethod` / `IrBasicBlock` -- `src/CobolSharp.Compiler/IR/IrMethod.cs`
- `IrInstruction` (abstract base) -- `src/CobolSharp.Compiler/IR/IrInstruction.cs`
- `IrType` / `IrRecordType` / `IrPrimitiveType` / `IrField` -- `src/CobolSharp.Compiler/IR/IrType.cs`

**Data flow:** `BoundProgram` + `SemanticModel` -> `IrModule` (basic-block CFG with typed instructions).

---

### 6. CIL Emitter

**Responsibilities.** Translates an `IrModule` into a runnable .NET assembly using Mono.Cecil.
Maps IR record types to explicit-layout structs with `[FieldOffset]` attributes, IR methods to
static CIL methods on a program class, and IR instructions to IL opcodes.

**Key types:**
- `CilEmitter` -- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs`

**Data flow:** `IrModule` + `SemanticModel` -> `AssemblyDefinition` (Mono.Cecil) -> .dll on disk.

---

### 7. Runtime Library

**Responsibilities.** Provides the runtime services that compiled COBOL programs link against.
Implements COBOL data semantics (PIC-based field access, MOVE with truncation/padding,
decimal arithmetic), file I/O (sequential, indexed), ACCEPT, INSPECT, and DISPLAY.

**Key types:**
- `CobolProgram` -- `src/CobolSharp.Runtime/CobolProgram.cs`
- `ProgramState` / `StorageArea` -- `src/CobolSharp.Runtime/StorageArea.cs`
- `PicDescriptor` / `PicDescriptorFactory` -- `src/CobolSharp.Runtime/PicDescriptor.cs`
- `PicRuntime` -- `src/CobolSharp.Runtime/PicRuntime.cs`
- `FileRuntime` -- `src/CobolSharp.Runtime/FileRuntime.cs`
- `InspectRuntime` -- `src/CobolSharp.Runtime/InspectRuntime.cs`
- `AcceptRuntime` -- `src/CobolSharp.Runtime/AcceptRuntime.cs`

---

### 8. Cross-Cutting: Diagnostics

**Key types:**
- `Diagnostic` -- `src/CobolSharp.Compiler/Diagnostics/Diagnostic.cs`
- `DiagnosticBag` -- `src/CobolSharp.Compiler/Diagnostics/DiagnosticBag.cs`
- `DiagnosticDescriptors` -- `src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs`
- `DiagnosticSeverity` -- `src/CobolSharp.Compiler/Diagnostics/DiagnosticSeverity.cs`

---

## End-to-End Data Flow Summary

```
COBOL source file (.cob)
  -> [ReferenceFormatProcessor] -> free-form text
  -> [CopyProcessor] -> COPY-expanded text
  -> [CobolLexer] -> token stream
  -> [CobolParserCore] -> CompilationUnitContext (CST)
  -> [SemanticBuilder] -> SemanticModel (symbols, types, layout)
  -> [BoundTreeBuilder] -> BoundProgram (typed AST)
  -> [Binder] -> IrModule (basic-block CFG)
  -> [CilEmitter] -> AssemblyDefinition (Mono.Cecil)
  -> .NET assembly (.dll)
```

---

# 2. Feature Coverage

## 2a. Divisions, Data Description, Numeric Behavior

### Divisions / Sections

| Feature | Status | Quality | Notes |
|---|---|---|---|
| IDENTIFICATION DIVISION | Implemented | Spec-true | PROGRAM-ID extracted; other paragraphs parsed but not semantically used |
| ENVIRONMENT DIVISION | Implemented | Spec-true | Container recognized; delegates to CONFIGURATION and INPUT-OUTPUT |
| CONFIGURATION SECTION | Implemented | Spec-true | SOURCE-COMPUTER, OBJECT-COMPUTER parsed; SPECIAL-NAMES: CURRENCY SIGN, DECIMAL-POINT IS COMMA, implementor switches all handled |
| INPUT-OUTPUT SECTION | Implemented | Spec-true | FILE-CONTROL with SELECT/ASSIGN/ORGANIZATION/ACCESS/STATUS/KEY fully bound to FileSymbol |
| DATA DIVISION | Implemented | Spec-true | Container for all data sections |
| FILE SECTION | Implemented | Spec-true | FD records, record descriptors, SELECT/FD consistency validated (CBL0601) |
| WORKING-STORAGE SECTION | Implemented | Spec-true | Full layout computation with REDEFINES family tracking |
| LINKAGE SECTION | Implemented | Spec-true | Parsed, symbols created; USING/RETURNING bound |
| LOCAL-STORAGE SECTION | Implemented | Spec-true | Parsed and symbols created with LocalStorage area kind |
| PROCEDURE DIVISION | Implemented | Spec-true | USING/RETURNING clauses, sections, paragraphs, full statement binding |

### Data Description

| Feature | Status | Quality | Notes |
|---|---|---|---|
| PIC numeric (9, S, V, P) | Implemented | Spec-true | Full support: IntegerDigits, FractionDigits, LeadingPScaling, TrailingPScaling, IsSigned |
| PIC alphanumeric (X) | Implemented | Spec-true | |
| PIC edited (Z, *, CR, DB, $, B, 0, /) | Implemented | Spec-true | NumericEdited and AlphanumericEdited categories |
| USAGE DISPLAY | Implemented | Spec-true | Default usage; SIGN IS SEPARATE adds 1 byte |
| USAGE COMP / BINARY | Implemented | Spec-true | 2/4/8-byte big-endian; overflow based on PIC digit count |
| USAGE COMP-3 / PACKED-DECIMAL | Implemented | Spec-true | BCD encoding with trailing sign nibble |
| USAGE COMP-5 | Implemented | Extension | Native binary: little-endian byte order, full binary capacity (no PIC-based truncation) |
| SIGN clause | Implemented | Spec-true | LEADING/TRAILING, SEPARATE CHARACTER; group-level propagation |
| JUSTIFIED clause | Implemented | Spec-true | JUSTIFIED RIGHT: right-justify on MOVE, left-truncate |
| OCCURS fixed | Implemented | Spec-true | MaxOccurs, INDEXED BY, subscript resolution |
| OCCURS DEPENDING ON | Implemented | Spec-true | Variable-length tables with resolved DEPENDING ON symbol |
| OCCURS ASCENDING/DESCENDING KEY | Implemented | Spec-true | **Research correction**: Fully parsed, stored in OccursInfo, validated in SEARCH ALL. DEVLOG "known gap" entry is stale. |
| REDEFINES | Implemented | Spec-true | Deferred resolution, family tracking for max extent |
| RENAMES (level 66) | Implemented | Spec-true | Full end-to-end: parse, resolve FROM/THRU, validate (CBL0810-0812), compute storage alias, bind, emit. THROUGH synonym supported |
| VALUE clause | Implemented | Spec-true | Literal and figurative constant values |
| 88-level condition names | Implemented | Spec-true | VALUE ranges with THRU; single values and ranges both supported |
| VALUE THRU in level-88 | Implemented | Spec-true | **Research correction**: Grammar (`valueRange`), SemanticBuilder (`ConditionSymbol.AddRange`), Binder (`LowerConditionName` range checks) all handle THRU correctly. NC201A/NC250A/NC252A failures are caused by other parse issues, not VALUE THRU. |
| Alignment / storage semantics | Implemented | Spec-true | Byte-level layout; no word-alignment (correct for COBOL-85) |

### Numeric Behavior

| Feature | Status | Quality | Notes |
|---|---|---|---|
| DISPLAY numeric encode/decode | Implemented | Spec-true | Overpunch sign (leading/trailing), separate sign character |
| COMP/BINARY arithmetic | Implemented | Spec-true | 2/4/8-byte big-endian; decimal scaling via FractionDigits |
| COMP-3 packed decimal | Implemented | Spec-true | BCD with trailing sign nibble (0xC positive, 0xD negative) |
| COMP-5 native binary | Implemented | Extension | Little-endian encode/decode via BinaryPrimitives; overflow based on binary capacity, not PIC digit count |
| ROUNDED phrase | Implemented | Spec-true | MidpointRounding.AwayFromZero |
| Truncation | Implemented | Spec-true | Numeric: truncate to PIC digit capacity; alphanumeric: right-truncate |
| Overflow behavior | Implemented | Spec-true | Checks PIC digit count for COMP |
| SIZE ERROR handling | Implemented | Spec-true | Full bind/lower/emit pipeline |

---

## 2b. Control Flow, Statements, and Expressions

### Control Flow

| Feature | Status | Quality | Notes |
|---|---|---|---|
| PERFORM (inline) | Implemented | Spec-true | InlineStatements list holds body; lowered to loop/branch IR |
| PERFORM (out-of-line) | Implemented | Spec-true | Single-paragraph call via IrPerform |
| PERFORM THRU | Implemented | Spec-true | Dynamic dispatch loop over paragraph index range |
| PERFORM VARYING | Implemented | Spec-true | Nested AFTER clauses supported via linked BoundPerformVarying.Next |
| PERFORM UNTIL | Implemented | Spec-true | UNTIL condition checked at top of loop; TIMES also supported |
| GO TO | Implemented | Spec-true | Lowers to IrReturnConst with paragraph index |
| GO TO DEPENDING ON | Implemented | Spec-true | Switch-based dispatch on selector value |
| ALTER | Implemented | Spec-true | Version-aware: error in COBOL-2002+ (CBL3601), warning+full support in COBOL-85/Default (CBL3602). Runtime alter indirection table for mutable GO TO targets. Bare GO TO supported (CBL3605/3606) |
| Fall-through rules | Implemented | Spec-true | Paragraph methods return next index; cross-section fall-through emits CBL3002 |
| Section/paragraph entry/exit | Implemented | Spec-true | EXIT SECTION/PARAGRAPH correctly implemented |

### Statements

| Feature | Status | Quality | Notes |
|---|---|---|---|
| IF/ELSE/END-IF | Implemented | Spec-true | Conditional branch IR |
| EVALUATE/WHEN | Implemented | Spec-true | Multi-subject, value THRU ranges, WHEN OTHER |
| MOVE (elementary) | Implemented | Spec-true | Full PIC-aware move with category-based dispatch |
| MOVE (group) | Implemented | Spec-true | Group moves are byte-level copies |
| MOVE (CORRESPONDING) | Implemented | Spec-true | Pairs computed at bind time |
| ADD | Implemented | Spec-true | Unified arithmetic; GIVING, ROUNDED, ON SIZE ERROR |
| SUBTRACT | Implemented | Spec-true | Same model as ADD; CORRESPONDING variant |
| MULTIPLY | Implemented | Spec-true | BY and GIVING forms |
| DIVIDE | Implemented | Spec-true | INTO/BY/GIVING/REMAINDER |
| COMPUTE | Implemented | Spec-true | Full arithmetic expression evaluation |
| STRING | Implemented | Spec-true | DELIMITED BY, INTO, POINTER, ON OVERFLOW |
| UNSTRING | Implemented | Spec-true | DELIMITED BY ALL, COUNT IN/DELIMITER IN, POINTER, TALLYING |
| INSPECT TALLYING | Implemented | Spec-true | ALL/LEADING/CHARACTERS with BEFORE/AFTER INITIAL |
| INSPECT REPLACING | Implemented | Spec-true | ALL/FIRST/LEADING/CHARACTERS with BEFORE/AFTER INITIAL |
| INSPECT CONVERTING | Implemented | Spec-true | FROM/TO character sets with BEFORE/AFTER INITIAL |
| ACCEPT | Implemented | Spec-true | DATE/TIME/DAY/etc. |
| DISPLAY | Implemented | Spec-true | Multiple operands; PIC-aware formatting |
| EXIT (plain) | Implemented | Spec-true | No-op per spec |
| EXIT PROGRAM | Implemented | Spec-true | Returns from called program's Entry method; distinct from plain EXIT |
| STOP RUN | Implemented | Spec-true | Exits paragraph dispatch loop |
| GOBACK | Implemented | Spec-true | Returns from called program; distinct from STOP RUN |
| CONTINUE | Implemented | Spec-true | No-op |
| INITIALIZE | Implemented | Spec-true | Category-based REPLACING supported |

### Expressions

| Feature | Status | Quality | Notes |
|---|---|---|---|
| Arithmetic expressions | Implemented | Spec-true | Full operator precedence; exponentiation |
| Relational conditions | Implemented | Spec-true | IS NOT variants; PIC-aware comparison |
| Class conditions | Implemented | Spec-true | NUMERIC, ALPHABETIC, ALPHABETIC-LOWER/UPPER |
| Sign conditions | Implemented | Spec-true | IS [NOT] POSITIVE/NEGATIVE/ZERO; lowered as comparison against zero |
| Condition-name (88-level) | Implemented | Spec-true | Expands to parent = value; SET TO TRUE supported |
| Combined conditions (AND/OR) | Implemented | Spec-true | Short-circuit evaluation |
| Negated conditions (NOT) | Implemented | Spec-true | NOT condition negates any condition (comparison, sign, class, condition-name, parenthesized) |

---

## 2c. File I/O, CALL, Diagnostics

### File I/O

| Feature | Status | Quality | Notes |
|---|---|---|---|
| SELECT/ASSIGN | Implemented | Spec-true | Literal and identifier ASSIGN targets |
| ORGANIZATION (SEQ/REL/IDX) | Implemented | Spec-true | SEQUENTIAL, RELATIVE, INDEXED, LINE SEQUENTIAL |
| ACCESS MODE (SEQ/RND/DYN) | Implemented | Spec-true | Stored on FileSymbol; validated |
| RECORD KEY | Implemented | Spec-true | Primary key parsed, resolved, offset computed for IndexedFileHandler |
| ALTERNATE KEY | Implemented | Spec-true | Parsed (ALTERNATE RECORD KEY IS ... WITH DUPLICATES), stored in FileSymbol.AlternateKeys, registered at runtime; IndexedFileHandler maintains secondary indices with duplicate support |
| FILE STATUS | Implemented | Spec-true | Full pipeline: validation, IR generation, CIL emission |
| OPEN (all modes) | Implemented | Spec-true | INPUT/OUTPUT/I-O/EXTEND; EXTEND validated (CBL0701) |
| CLOSE | Implemented | Spec-true | Multi-file CLOSE |
| READ (sequential) | Implemented | Spec-true | AT END / NOT AT END; FILE STATUS update |
| READ (random/keyed) | Implemented | Spec-true | RANDOM/DYNAMIC access emits IrReadByKey; calls FileRuntime.ReadByKey → IFileHandler.ReadByKey |
| READ INTO | Implemented | Spec-true | INTO target bound; implicit MOVE emitted |
| WRITE (basic) | Implemented | Spec-true | Full pipeline |
| WRITE FROM | Implemented | Spec-true | Validated (CBL1801) |
| WRITE ADVANCING | Implemented | Spec-true | BEFORE/AFTER ADVANCING with integer lines; PAGE advancing (form-feed) |
| REWRITE | Implemented | Spec-true | Organization check (CBL1901) |
| REWRITE FROM | Implemented | Spec-true | FROM source MOVEd to record before rewrite (same pattern as WRITE FROM) |
| DELETE | Implemented | Spec-true | INVALID KEY paths; organization validated (CBL2001) |
| START (KEY IS) | Implemented | Spec-true | Key condition extracted from bound tree; maps to StartCondition enum (Equal/Greater/GreaterOrEqual/Less/LessOrEqual) |

### Runtime I/O Handlers

| Handler | Status | Notes |
|---|---|---|
| SequentialFileHandler | Implemented | Fixed-length and line-sequential; all OPEN modes |
| IndexedFileHandler | Implemented | SortedDictionary in-memory; single primary key only |
| RelativeFileHandler | Implemented | Fixed-length records; 1-based record number |

### CALL / USING / RETURNING

| Feature | Status | Quality | Notes |
|---|---|---|---|
| CALL statement | Implemented | Spec-true | Static CALL via CobolProgramRegistry; Entry method per program; paragraph dispatch in Entry |
| BY REFERENCE | Implemented | Spec-true | CobolDataPointer into caller's WorkingStorage; callee LINKAGE items alias caller's bytes |
| BY CONTENT | Implemented | Spec-true | CobolDataPointer.CreateByContent copies argument bytes |
| BY VALUE | Implemented | Dialect-gated | Grammar gated by `is2002()`; copy semantics (same as BY CONTENT); value encoded in source location |
| RETURNING | Implemented | Spec-true | RETURNING target added as extra BY REFERENCE arg in CobolDataPointer array; callee writes via LINKAGE |
| ON EXCEPTION / NOT ON EXCEPTION | Implemented | Spec-true | Branch on registry resolve result; unresolvable programs take ON EXCEPTION path |
| Linkage Section | Implemented | Spec-true | Layout computed with relative offsets; accessed via CobolDataPointer fields |
| PROCEDURE DIVISION USING | Implemented | Spec-true | Parameters resolved to LINKAGE DataSymbols; mapped to Entry args in CIL |
| ENTRY statement | Implemented | Spec-true | Alternate entry points; Entry_<name> methods generated; registered in CobolProgramRegistry |
| EXIT PROGRAM | Implemented | Spec-true | Returns from called program's Entry method (was broken — no-op before) |
| GOBACK | Implemented | Spec-true | Returns from called program; distinct from STOP RUN |
| Dynamic CALL | Implemented | Spec-true | Target name read from data item at runtime via GetDisplayString; registry-based resolution; Assembly.LoadFrom discovery |
| INITIAL program | Implemented | Spec-true | IsInitial captured from PROGRAM-ID; ResetState re-creates ProgramState at Entry start |
| CANCEL statement | Implemented | Spec-true | Grammar accepts literals and identifiers; CobolProgramRegistry.Cancel removes program |
| Inter-program communication | Implemented | Spec-true | Same-process shared-address-space via CobolDataPointer; CobolProgramRegistry for dispatch |

### Diagnostics Infrastructure

| Feature | Status | Notes |
|---|---|---|
| Error reporting mechanism | Implemented | DiagnosticBag accumulates records; HasErrors check |
| Diagnostic code catalog | Implemented | 169 descriptors across COBOL0001-COBOL0600 and CBL0601-CBL3606 |
| Source location tracking | Implemented | File path, offset, line, column; TextSpan |
| Error recovery in parser | Implemented | 25+ pattern-matched hints for common COBOL mistakes |
| Error cap | Implemented | Max 20 parse errors per file |

### Diagnostic Code Ranges

| Range | Area | Count |
|---|---|---|
| CBL0601-0602 | SELECT/FD consistency | 1 |
| CBL0701 | OPEN enforcement | 1 |
| CBL0801-0803 | Data item classification | 3 |
| CBL0901-0905 | MOVE enforcement | 5 |
| CBL1001-1004 | VALUE clause | 4 |
| CBL1101-1105 | OCCURS / DEPENDING ON | 5 |
| CBL1201-1205 | SEARCH / SEARCH ALL | 5 |
| CBL1301-1304 | STRING | 4 |
| CBL1401-1406 | UNSTRING | 6 |
| CBL1501-1503 | INSPECT | 3 |
| CBL1601-1605 | START | 5 |
| CBL1701-1704 | READ | 4 |
| CBL1801-1803 | WRITE | 3 |
| CBL1901-1902 | REWRITE | 2 |
| CBL2001 | DELETE | 1 |
| CBL2101-2102 | RETURN (sort/merge) | 2 |
| CBL2201 | RELEASE | 1 |
| CBL2301-2308 | PERFORM | 8 |
| CBL2401-2402 | IF | 2 |
| CBL2501-2503 | EVALUATE | 3 |
| CBL2601-2605 | Arithmetic | 5 |
| CBL3001-3004 | Flow analysis | 4 |
| CBL3101-3114 | Scope & symbols | 14 |
| CBL3201-3206 | File status | 6 |
| CBL3301-3310 | CALL/USING/RETURNING | 6 |
| CBL3401-3406 | Report Writer | 6 |
| CBL3501-3502 | Strict COBOL-85 mode | 2 |

### Summary of Feature Coverage Gaps (Section 2c)

All major File I/O and CALL features are now implemented:

1. ~~**ALTERNATE KEY**~~: **DONE** — parsed, stored in FileSymbol, registered at runtime with secondary indices.
2. ~~**READ random/keyed**~~: **DONE** — IrReadByKey instruction; FileRuntime.ReadByKey dispatches to IFileHandler.ReadByKey.
3. ~~**WRITE BEFORE ADVANCING / PAGE**~~: **DONE** — IrWriteAdvancing with IsBefore flag; PAGE emits form-feed.
4. ~~**CALL inter-program linkage**~~: **DONE** — full implementation via CobolProgramRegistry + Entry methods.
5. ~~**PROCEDURE DIVISION USING/RETURNING**~~: **DONE** — parameters resolved to LINKAGE DataSymbols.
6. ~~**Inter-program communication**~~: **DONE** — same-process shared-address-space via CobolDataPointer.

---

# 3. Code Quality & Patterns

Codebase: 97 C# source files (excluding generated ANTLR code).

## 3.1 Meaningless Wrappers — **RESOLVED**

- ~~`BindDataReference`~~: **Inlined** at single call site and deleted.
- ~~`BindFullExpression`~~: **Eliminated**. All 12 callers updated to call `BindAdditiveExpression(ctx.additiveExpression())` directly. Wrapper method deleted.

---

## 3.2 Duplicated Logic — **ALL RESOLVED**

### ~~Two complete expression binding implementations~~ — **RESOLVED**
Path B deleted (~90 lines). Single expression chain remains.

### ~~`GetPicForLocation` duplicated~~ — **RESOLVED**
Moved to `IrLocationExtensions.GetPic()` extension method in `IR/IrLocationExtensions.cs`.
Both private copies in Binder and CilEmitter deleted.

### ~~INVALID KEY branching duplicated 3x~~ — **RESOLVED**
Extracted `LowerConditionalBranch()` helper in Binder. LowerRead, LowerDelete, LowerStart,
and LowerCall all delegate to it. ~54 lines of duplication → 1 shared helper + 4 one-line calls.

### ~~Arithmetic target binding repeated 6x~~ — **RESOLVED**
Extracted `BindArithmeticTargets()` helper in BoundTreeBuilder. 7 foreach loops replaced
across BindAdd, BindSubtract, BindMultiply, BindDivide.

### ~~Fake source locations repeated 69+ times~~ — **RESOLVED**
Created `SourceLocation.None` and `TextSpan.Empty` static factories. All 44 occurrences
across 12 files replaced. Redundant `s_noLocation`/`s_noSpan` fields in Binder deleted.

---

## 3.3 Ad-hoc and Hacky Code Paths

### ~~Ad-hoc diagnostic codes~~ — **RESOLVED**

All 55 ad-hoc codes migrated to centralized `DiagnosticDescriptors`. 175 total descriptors.

### IR stub for RETURN

- **RETURN stub** (Binder.cs): Emits DISPLAY message, always takes AT END path. (SORT/MERGE not yet implemented.)
- ~~**CALL stub**~~: **RESOLVED**.

### ~~Function calls return constant zero~~ — **RESOLVED**

Diagnostic COBOL0110 now emitted for unimplemented FUNCTION calls. Zero literal fallback retained
for graceful degradation but the warning alerts the programmer.

### Power operator

- `CilEmitter.cs`: `Math.Pow` emission already implemented for Power operator.

### ~~`StartCondition` is a magic number~~ — **RESOLVED**

KeyCondition extracted from bound tree and mapped to StartCondition enum.

### ~~REWRITE FROM lowering~~ — **RESOLVED**

FROM source MOVEd to record before rewrite.

### ~~String literal fallback for unresolved identifiers~~ — **RESOLVED**

Diagnostic COBOL0110 now emitted before the string literal fallback. Typos and missing
declarations are surfaced as warnings instead of silently producing wrong results.

### Stale XML doc comments

- `CilEmitter.cs`: Three consecutive `<summary>` blocks on `EmitElementAddress`. Low priority.

---

## 3.4 Dead Code — **MOSTLY RESOLVED**

| Item | Status | Notes |
|---|---|---|
| ~~`CompilationOptions`~~ | **In use** | Now actively used for `--standard` dialect gating |
| ~~`ReportWriterValidator`~~ | **Deleted** | Empty stub removed; CBL3401-3406 descriptors also deleted |
| ~~`GetDataReferenceName`~~ | **Deleted** | Zero callers |
| ~~`BindDataReference`~~ | **Deleted** | Inlined at single call site |
| CBL3105-3107 | Forward decl | GLOBAL/LOCAL features not yet implemented |
| CBL3301-3305 | Partially wired | CBL3304 (RETURNING not LINKAGE) now wired in ValidateCall; others need compile-time linking |
| CBL3501-3502 | Forward decl | Strict COBOL-85 mode features |

---

## 3.5 TODO / FIXME / HACK Comments — **RESOLVED**

Both TODOs addressed:
- ~~`Program.cs:148`~~: `--standard` is now wired to CompilationOptions. TODO removed.
- ~~`BoundTreeBuilder.cs:3067`~~: Was in deleted duplicate path B. Function calls now emit COBOL0110 diagnostic.

**Count**: 0 TODO comments remaining.

---

## 3.6 NotSupportedException / NotImplementedException Throws

All 5 occurrences are in `CilEmitter.cs` and serve as exhaustive switch guards (lines 745, 829, 2604, 2634, 2748). Acceptable as defensive programming for "impossible state" detection.

---

## 3.7 Overly Complex Methods (>80 lines) — **PARTIALLY RESOLVED**

Two methods that mixed distinct concerns were split:

- ~~`EmitProgramState` (206 lines)~~: **Split** into 6 focused methods: `EmitProgramState` (32-line orchestrator), `EmitProgramStateAllocation` (13), `EmitValueClauseInitialization` (73), `EmitAlterTableInitialization` (23), `EmitResetStateMethod` (18), `ComputeOccursExtent` (25).
- ~~`Bind` (149 lines)~~: **Split** into 5 focused methods: `Bind` (28-line orchestrator), `CreateParagraphStubs` (15), `ScanAlterTargets` (17), `LowerAllParagraphs` (49), `PopulateModuleMetadata` (17).

### Remaining (inherent complexity — accepted)

These are dispatch switches or spec-matching implementations where the complexity is irreducible:

| Method | Lines | Category |
|---|---|---|
| `EmitInstruction` | ~410 | CIL dispatch switch (standard compiler pattern) |
| `EmitUnstringStatement` | ~151 | UNSTRING spec complexity (DELIMITED BY, INTO, POINTER, etc.) |
| `EmitExpression` | ~132 | Expression type dispatch |
| `BindPerform` | ~119 | PERFORM has 5 forms per spec |
| `LowerConditionName` | ~117 | Level-88 expansion with VALUE ranges/THRU |
| `LowerStatement` | ~118 | Statement type dispatch |
| `LowerDivide` | ~110 | DIVIDE has 4 forms per spec |
| `BindDataReferenceWithSubscripts` | ~108 | Multi-dimensional subscript handling |
| `BindInspect` | ~101 | INSPECT TALLYING/REPLACING/CONVERTING |
| `EmitStringStatement` | ~96 | STRING spec complexity |
| `LowerComparison` | ~91 | Numeric/alphanumeric/literal matrix |

---

## 3.8 Code Quality Priority Summary

### High Priority — ALL RESOLVED
1. ~~**Duplicate expression binding** (3.2)~~ — **RESOLVED**: path B deleted, single chain.
2. ~~**Function calls silently return zero** (3.3)~~ — **RESOLVED**: COBOL0110 diagnostic emitted.
3. ~~**Unresolved identifiers become string literals** (3.3)~~ — **RESOLVED**: COBOL0110 diagnostic emitted.
4. ~~**START always uses Equal condition** (3.3)~~ — **RESOLVED**: KeyCondition extracted.
5. ~~**REWRITE FROM not lowered** (3.3)~~ — **RESOLVED**: FROM MOVEd to record before rewrite.

### Medium Priority
6. ~~**Ad-hoc diagnostic codes** (3.3)~~ — **RESOLVED**.
7. ~~**Fake source locations** (3.2)~~ — **RESOLVED**: `SourceLocation.None` / `TextSpan.Empty`.
8. ~~**`GetPicForLocation` duplication** (3.2)~~ — **RESOLVED**: shared extension method.
9. ~~**Branching pattern duplication** (3.2)~~ — **RESOLVED**: `LowerConditionalBranch` helper.
10. ~~**CALL stub**~~ — **RESOLVED**.

### Low Priority
11. ~~**Dead code** (3.4)~~ — **MOSTLY RESOLVED**.
12. ~~**Wrapper method** (3.1)~~ — **RESOLVED**: both wrappers eliminated.
13. ~~**Receiving target binding pattern** (3.2)~~ — **RESOLVED**: `BindArithmeticTargets` helper.
14. ~~**Stale XML doc comments** (3.3)~~ — **RESOLVED**: duplicate summary blocks removed.

---

# 4. Testing & Validation

## 4a. Validator and Diagnostic Gaps

### Existing Validators

| Validator | File | Post/Pre-binding | Diagnostics |
|---|---|---|---|
| BoundTreeValidator | `Semantics/Bound/BoundTreeValidator.cs` | Post | CBL0701, CBL1601-1703, CBL1801-2001, CBL2101, CBL2302-2503, CBL3310 |
| SymbolValidator | `Semantics/SymbolValidator.cs` | Pre | CBL3101-3104, CBL3107, CBL3110-3113 |
| DataItemClassifier | `Semantics/DataItemClassifier.cs` | Pre | CBL0801-0803, CBL1001-1004, CBL1103-1104 |
| ParagraphValidator | `Semantics/ParagraphValidator.cs` | Pre | Ad-hoc "SEM" warnings (no CBL code) |
| FileStatusValidator | `Semantics/FileStatusValidator.cs` | Pre | CBL3201-3204 |
| ProcedureGraph | `Semantics/ProcedureGraph.cs` | Post | CBL3001-3004 |
| ReportWriterValidator | `Semantics/ReportWriterValidator.cs` | Stub | Empty body. CBL3401-3406 defined but never emitted. |

### Dormant Diagnostics (47 of 107 defined descriptors never emitted)

| Category | Dormant Codes | Count |
|---|---|---|
| MOVE enforcement | CBL0902-0905 | 4 |
| VALUE clause | CBL1003-1004 | 2 |
| OCCURS | CBL1102 | 1 |
| SEARCH | CBL1201, CBL1203, CBL1205 | 3 |
| STRING | CBL1302-1303 | 2 |
| UNSTRING | CBL1402-1404 | 3 |
| INSPECT | CBL1503 | 1 |
| START | CBL1602, CBL1604-1605 | 3 |
| READ | CBL1704 | 1 |
| WRITE | CBL1802-1803 | 2 |
| RETURN | CBL2102 | 1 |
| RELEASE | CBL2201 | 1 |
| PERFORM | CBL2301 | 1 |
| IF/comparison | CBL2402 | 1 |
| Arithmetic | CBL2604 | 1 |
| Flow analysis | CBL3003 | 1 |
| Scope & symbols | CBL3105-3106, CBL3108-3109, CBL3114 | 5 |
| File status | CBL3205-3206 | 2 |
| CALL | CBL3301-3305 | 5 |
| Report Writer | CBL3402-3406 | 5 |
| Strict COBOL-85 | CBL3501-3502 | 2 |
| **Total** | | **47** |

### Missing Validation Gaps

**Immediately actionable** (no new infrastructure needed):
1. CBL3302 -- BY REFERENCE argument must be identifier (check in `ValidateCall`)
2. CBL1704 -- READ INTO target type (check in `ValidateRead`)
3. CBL1802/1803 -- WRITE ADVANCING type (check in `ValidateWrite`)
4. CBL3108/3109 -- USING/RETURNING linkage validation (check in `SymbolValidator`)
5. CBL3114 -- REDEFINES subordinate to OCCURS (check in `SymbolValidator.ValidateRedefines`)
6. CBL1602/1604 -- START KEY expression/type (check in `ValidateStart`)

**Requires new infrastructure**:
- CBL0702 (open-state tracking) -- flow-sensitive file state analysis
- CBL3206 (FILE STATUS unchecked) -- flow-sensitive analysis
- CBL3301/3303-3305 -- inter-program metadata for CALL validation
- CBL3401-3406 -- Report Writer codegen

---

## 4b. Test Suite Analysis

### Test Framework and Counts

| Category | Count | Framework |
|---|---|---|
| Unit tests | 217 pass | xUnit |
| Integration tests | 189 pass, 1 skip | xUnit |
| NIST tests at 100% | 39 of 95 NC-series (41%) | Shell script (not xUnit) |

### Unit Test Coverage by Phase

| Phase | File(s) | Tests | Focus |
|---|---|---|---|
| Preprocessing | FixedFormTests | 4 | Fixed-form column handling |
| Source model | SourceTextTests | 8 | Source text infrastructure |
| Runtime: fields | CobolFieldTests | 16 | PIC field storage/access |
| Runtime: moves | PicRuntimeMoveTests | 20 | MOVE semantics between PIC types |
| Runtime: intrinsics | IntrinsicFunctionTests | 30 | ABS, SQRT, MOD, MAX, MIN, etc. |
| Runtime: I/O | SequentialFileTests | 6 | Sequential file operations |
| Semantics: binding | BoundTreeValidatorTests | 28 | IF/PERFORM/EVALUATE type enforcement |
| Semantics: symbols | SymbolValidatorTests | 16 | Symbol table diagnostics |
| Semantics: types | ExpressionTypeTests, CategoryCompatibilityTests | 15 | Type classification and compatibility |
| Semantics: data | DataItemClassifierTests | 12 | Data item classification |
| Semantics: diagnostics | ArithmeticDiagnosticTests, DiagnosticReachabilityTests | 12 | Arithmetic and reachability diagnostics |

### Phases with ZERO Unit Tests

- **Parsing / Grammar** -- tested only through integration tests
- **Code Generation / CIL Emission** -- validated only through end-to-end execution
- **IR Lowering** -- no unit tests
- **Flow Analysis** -- no direct unit tests
- **SemanticBuilder / Binder** -- not unit-tested in isolation
- **COPY preprocessor** -- only via integration tests

### Integration Test Approach

`EndToEndTests.cs` (4,868 lines, 169 tests) compiles inline COBOL via `Compilation.Compile()`, runs the DLL as a subprocess, and asserts on stdout. True end-to-end: source text in, program output out. 10-second process timeout.

One skipped test: `CallStatement_EmitsDiagnostic` (CALL not yet lowered to CIL).

### NIST CCVS85 Compliance

**39 NIST NC-series tests at 100%:**
- Arithmetic: NC101A-NC107A, NC111A, NC112A, NC115A-NC120A, NC122A-NC124A, NC126A, NC127A, NC131A, NC132A, NC136A, NC137A, NC140A, NC141A
- Table Handling: NC170A-NC173A, NC175A-NC177A
- Control Flow: NC202A, NC203A, NC206A, NC207A, NC210A, NC221A, NC222A, NC224A
- Data: NC239A-NC241A, NC248A, NC251A, NC253A

**Known blockers:**
- NC220M: infinite loop at runtime (IrElementRef destination issue likely)
- NC201A, NC250A, NC252A: period-terminated inline PERFORM and other parse issues (NOT VALUE THRU -- research corrected this)
- NC233A, NC237A, NC247A: runtime PERFORM VARYING / SEARCH issues (NOT ASCENDING KEY grammar -- research corrected this)
- NC211A, NC254A: STATUS as keyword in SPECIAL-NAMES
- NC215A, NC219A: PROGRAM as keyword in OBJECT-COMPUTER

**Note:** NC121M (subscripted DIVIDE GIVING) was **already fixed** in DEVLOG Entry 126 (2026-03-20). The CLAUDE.md "known gap" entry is stale.

### Test Quality Assessment

**Strengths:**
1. Diagnostic-driven validation tests with clean pattern of asserting on specific CBL codes
2. Runtime edge case coverage (truncation, signed fields, sign storage conversions)
3. Integration test breadth (169 tests across wide COBOL feature surface)
4. NIST as external ground truth independent of developer assumptions

**Weaknesses:**
1. No unit tests for parsing, code generation, IR lowering, or flow analysis
2. NIST tests not in xUnit -- not part of `dotnet test` or CI/CD
3. Monolithic integration test file (4,868 lines, single file)
4. No negative compilation tests in integration suite
5. Limited file I/O unit tests (6 sequential only)

### Coverage Gap Severity

| Compiler Phase | Unit Tests | Integration | Gap Severity |
|---|---|---|---|
| Preprocessor (fixed-form) | 4 tests | Implicit in NIST | Low |
| Preprocessor (COPY) | None | 1 integration test | Medium |
| Parsing / Grammar | None | All integration tests | **High** |
| Semantic Analysis | 89 tests | All integration tests | Low |
| Flow Analysis | None | Indirect via PERFORM | Medium |
| IR Lowering | None | None | Medium |
| Code Generation (CIL) | None | All integration tests | **High** |
| Runtime (fields/PIC) | 66 tests | All integration tests | Low |
| Runtime (file I/O) | 6 tests | 12+ integration tests | Medium |
| NIST validation | Not in xUnit | 39 via shell script | **High** (automation gap) |

---

# 5. Remediation Plan

## 5a. Phase 1 (Grammar/Parsing), Phase 2 (Semantic Analysis), Phase 3 (Lowering/IR)

> **Research corrections applied**: Deep codebase research found that VALUE THRU and ASCENDING/DESCENDING KEY are already fully implemented. The CLAUDE.md "known gap" entries for these are stale. NC201A/NC250A/NC252A failures are caused by other parse issues; NC233A/NC237A/NC247A failures are caused by runtime PERFORM VARYING and SEARCH issues. Additionally, NC121M (subscripted DIVIDE GIVING) was already fixed in DEVLOG Entry 126.

### Phase 1: Grammar and Parsing Hardening (GRA-xx)

#### GRA-01: Fix STATUS in SPECIAL-NAMES (was "VALUE THRU" -- corrected)

**Goal:** Allow `ON STATUS IS condition-name` in implementor switch entries.

**Scope:** `Grammar/Core/CobolSpecialNames.g4`, `SemanticBuilder.cs`

**Problem:** The `STATUS` token is a keyword, so `(ON IDENTIFIER)?` cannot match `ON STATUS IS condition-name`. The current rule requires IDENTIFIER but STATUS lexes as a keyword token.

**Required changes:**
1. Change `implementorSwitchEntry` to accept `ON STATUS? IS? IDENTIFIER` (allow both `ON STATUS IS cond-name` and shortened `ON cond-name` form).
2. Update SemanticBuilder to extract ON/OFF condition names from the new rule shape.

**Acceptance criteria:** NC174A, NC211A, NC254A NIST tests pass (STATUS as keyword in SPECIAL-NAMES).

---

#### GRA-02: Fix PROGRAM COLLATING SEQUENCE in OBJECT-COMPUTER

**Goal:** Allow `PROGRAM COLLATING SEQUENCE IS alphabet-name` in OBJECT-COMPUTER paragraph.

**Scope:** `CobolParserCore.g4` (objectComputerParagraph / computerAttributes rule)

**Problem:** `PROGRAM` is a keyword token; `computerAttributes` only accepts `IDENTIFIER | STRINGLIT | INTEGERLIT` and cannot consume it.

**Required changes:**
1. Add `PROGRAM` to the `computerAttributes` alternative list (minimal fix), OR
2. Add a dedicated `programCollatingSequenceClause` rule (proper fix).

**Acceptance criteria:** NC215A, NC219A, NC114M, NC214M NIST tests pass.

---

#### GRA-03: Allow Reserved Words as Paragraph Names

**Goal:** Allow STATUS and PROGRAM to be used as paragraph names in context-sensitive positions.

**Scope:** `CobolParserCore.g4` (paragraphName rule), possibly `CobolLexer.g4`

**Required changes:** Extend `paragraphName` rule to accept context-sensitive reserved words. Use semantic predicates or expanded identifier rule.

**Acceptance criteria:** Programs using STATUS or PROGRAM as paragraph names parse correctly.

---

#### GRA-04: Complete SORT/MERGE Statement Binding

**Goal:** Add bound nodes and IR stubs for SORT and MERGE (grammar already fully defined).

**Scope:** `BoundNodes.cs`, `BoundTreeBuilder.cs`, `BoundTreeValidator.cs`, `Binder.cs`

**Required changes:**
1. Add `BoundSortStatement` and `BoundMergeStatement` to BoundNodes.
2. Add `BindSort()` and `BindMerge()` in BoundTreeBuilder.
3. Add validation rules (file must be SD, keys must be subordinate).
4. Add IR lowering stubs in Binder.
5. Add SD (Sort Description) support in SemanticBuilder if needed.

**Acceptance criteria:** SORT and MERGE statements bind without errors; IR stubs emit diagnostic messages.

---

#### GRA-05: Wire Alternate Record Key Semantics

**Goal:** Visit ALTERNATE KEY clauses in SemanticBuilder and store on FileSymbol.

**Scope:** `SemanticBuilder.cs`, `ProgramSymbol.cs` (FileSymbol)

**Required changes:**
1. Add `AlternateKeys` property to FileSymbol.
2. Add `alternateKeyClause` visitor in `VisitFileControlClauseGroup`.
3. Update READ KEY validation (CBL1703) to check alternate keys.

**Acceptance criteria:** SELECT statements with ALTERNATE RECORD KEY store key info; READ KEY validation includes alternate keys.

---

### Phase 2: Semantic Analysis and Binding (SEM-xx)

#### SEM-01: Fix Duplicate Expression Binding

**Goal:** Eliminate the ~90 lines of duplicate arithmetic expression binding.

**Scope:** `BoundTreeBuilder.cs` (lines ~2236-2330 and ~2994-3082)

**Required changes:** Delete path B (`BindArithmeticExpr` chain). Replace with null-guarded call to `BindFullExpression`.

**Acceptance criteria:** Single expression binding code path. All 176+195+39 tests pass.

---

#### SEM-02: Fix Silent Zero Return for Function Calls

**Goal:** Implement intrinsic function evaluation instead of returning literal zero.

**Scope:** `BoundTreeBuilder.cs` (lines 2325-2327 and 3067-3068)

**Required changes:**
1. Create `BoundFunctionCallExpression` node.
2. Implement at minimum FUNCTION LENGTH and FUNCTION CURRENT-DATE.
3. Add `IrIntrinsicCall` IR instruction and CIL emission.
4. Emit diagnostic for unrecognized function names.

**Acceptance criteria:** No intrinsic function silently returns zero. Diagnostic for unknown functions.

---

#### SEM-03: Fix Silent String Literal Fallback for Unresolved Identifiers

**Goal:** Emit a diagnostic instead of silently converting unresolved identifiers to string literals.

**Scope:** `BoundTreeBuilder.cs` (lines 3137, 3081, 2991)

**Required changes:**
1. Emit diagnostic (e.g., `CBL0401`) instead of creating string literal.
2. Return `BoundErrorExpression` to prevent downstream issues.

**Acceptance criteria:** Typos in data names produce clear diagnostics. No false positives on valid programs.

---

#### SEM-04: Fix Hardcoded START Key Condition

**Goal:** Use actual KEY IS comparison operator from COBOL source instead of always Equal.

**Scope:** `Binder.cs` (line ~1308)

**Required changes:** Read `start.KeyCondition` from `BoundStartStatement`, extract comparison operator, map to `StartCondition` enum, pass to `IrStartFile`.

**Acceptance criteria:** `START file KEY IS GREATER THAN data-name` positions correctly.

---

#### SEM-05: Wire PROCEDURE DIVISION USING/RETURNING

**Goal:** Populate ProcedureParameter lists from PROCEDURE DIVISION USING/RETURNING.

**Scope:** `SemanticBuilder.cs`, `SemanticModel.cs`

**Required changes:**
1. Add visitor for USING clause in SemanticBuilder.
2. Resolve parameters against LINKAGE SECTION.
3. Store in SemanticModel.
4. Emit diagnostic for invalid parameter references.

---

#### SEM-06: Activate Dormant Diagnostic Descriptors

**Goal:** Wire the 47 defined-but-never-emitted diagnostic descriptors to actual validation checks.

**Required changes:** For each dormant descriptor: identify COBOL-85 rule, add validation check, add unit test (positive + negative). Remove truly unnecessary descriptors.

**Acceptance criteria:** Zero dormant descriptors remain.

---

### Phase 3: Lowering and IR Design (LOW-xx)

#### LOW-01: Fix NC220M Infinite Loop

**Goal:** Diagnose and fix the runtime hang in NC220M.

**Scope:** `Binder.cs` (PERFORM VARYING lowering), `CilEmitter.cs`

**Research finding:** Likely cause is `ADD 1 TO TABLE7-NUM(INDEX7)` writing to wrong location via IrElementRef destination, causing UNTIL condition to never become true. Needs runtime debugging to confirm.

**Acceptance criteria:** NC220M completes within 10 seconds with correct output.

---

#### LOW-02: Implement REWRITE FROM Lowering

**Goal:** Emit implicit MOVE from FROM data item to record area before rewriting.

**Scope:** `Binder.cs` (LowerRewrite, lines 1240-1249)

**Required changes:** Add the same MOVE pattern already used in `LowerWrite` lines 1078-1085.

**Acceptance criteria:** `REWRITE record FROM data-item` correctly moves data before rewriting. 7-line fix.

---

#### LOW-03: Centralize INVALID KEY / AT END Branching

**Goal:** Extract repeated branching pattern into shared helper.

**Scope:** `Binder.cs` (LowerRead, LowerDelete, LowerStart -- 54 lines of near-duplicate)

**Required changes:** Create `LowerConditionalBranch(...)` helper. Replace 3 copy-pasted patterns.

**Acceptance criteria:** No duplicated branching code. All I/O tests pass.

---

#### LOW-04: Eliminate Duplicate GetPicForLocation

**Goal:** Remove duplicated method from Binder.cs and CilEmitter.cs.

**Required changes:** Move to shared utility (e.g., `IrLocation` extension method).

---

#### LOW-05: Route All Diagnostics Through DiagnosticDescriptor

**Goal:** Eliminate 50+ raw string diagnostic codes.

**Required changes:** Define descriptors for COBOL0400-0513 and COBOL0001-0312. Update all emission sites.

**Acceptance criteria:** Zero raw string diagnostic codes in codebase.

---

#### LOW-06: Fix ADD/SUBTRACT/DIVIDE ResolveLocation Consistency

**Goal:** Use `ResolveExpressionLocation` (not `ResolveLocation`) for arithmetic targets.

**Research finding:** MULTIPLY uses `ResolveExpressionLocation` which handles ref-mod targets; ADD/SUBTRACT/DIVIDE use `ResolveLocation` which silently drops ref-mod. Minor consistency gap -- latent bug for ref-mod targets in non-MULTIPLY arithmetic.

---

## 5b. Phase 4 (Backend / CIL Emission) and Phase 5 (Diagnostics and Test Suite)

### Phase 4: Backend / CIL Emission (CIL-xx)

#### CIL-01: Eliminate IrRuntimeCall String Dispatch

Replace stringly-typed `IrRuntimeCall` with typed IR instructions for all file I/O operations (`IrOpenFile`, `IrCloseFile`, `IrRegisterFileHandler`, `IrInitFileRuntime`).

---

#### CIL-02: ~~Implement CALL Inter-Program Linkage~~ — **DONE**

Fully implemented: `IrCallProgram`, `CobolProgramRegistry`, `CobolDataPointer`, Entry methods,
LINKAGE access, BY REFERENCE/CONTENT/VALUE, RETURNING, ON EXCEPTION, ENTRY, CANCEL, INITIAL.

---

#### CIL-03: Implement RETURN (Sort/Merge) CIL Emission

Replace RETURN stub with `IrReturnRecord` instruction. Requires `SortRuntime.ReturnRecord()` in runtime project.

---

#### CIL-04: Implement SORT/MERGE Lowering and CIL Emission

Define `IrSort`/`IrMerge` instructions. Implement `SortRuntime` with key-based comparison. Support both USING/GIVING and INPUT/OUTPUT PROCEDURE forms.

---

#### CIL-05: ~~Implement WRITE BEFORE/AFTER ADVANCING~~ — **DONE**

Renamed `IrWriteAfterAdvancing` → `IrWriteAdvancing` with `IsBefore` flag and PAGE (-1) sentinel.
BEFORE/AFTER/PAGE all implemented. Dynamic operand (data-item advance count) not yet supported
(only integer literal advance count).

---

#### CIL-06: READ INTO and WRITE FROM with Non-Record Targets

Verify READ INTO emits read-then-MOVE. Verify WRITE FROM emits MOVE-then-write. Ensure group-to-group moves with correct size semantics.

---

### Phase 5: Diagnostics and Test Suite (TST-xx)

#### TST-01: Create Formal Diagnostic Catalog Document

Generate `docs/diagnostics.md` from `DiagnosticDescriptors.cs` with Code, Severity, Message Template, COBOL-85 Section, Phase columns.

---

#### TST-02: Audit Diagnostic Emission for Raw String Codes

Convert all `ReportError(string code, ...)` calls to use `DiagnosticDescriptor` references. Consider making raw-code overloads `[Obsolete]`.

---

#### TST-03: Add Negative Diagnostic Tests for All Statement Validators

One unit test per CBL diagnostic code. Priority areas: STRING (CBL1301-1304), UNSTRING (CBL1401-1406), INSPECT (CBL1501-1503), START (CBL1601-1605), FILE STATUS (CBL3201-3206), CALL (CBL3301-3310).

---

#### TST-04: Add Positive Semantic Tests for Core COBOL-85 Rules

At least 20 new positive tests covering major statement categories, asserting `HasErrors == false` for valid programs.

---

#### TST-05: Organize Integration Tests by COBOL Feature Area

Split `EndToEndTests.cs` (169 tests, 4,868 lines) into feature-focused classes: `ArithmeticEndToEndTests.cs`, `FileIOEndToEndTests.cs`, `ControlFlowEndToEndTests.cs`, etc. Extract shared `CompileAndRun` helper into base class.

---

#### TST-06: Add CIL Emission Unit Tests

Create `tests/CobolSharp.Tests.Unit/CodeGen/CilEmitterTests.cs`. Test individual IR instruction emission via Mono.Cecil inspection. At least 10 tests covering major instruction categories.

---

#### TST-07: Add NIST Test Coverage Tracking

Create automated mechanism to track NIST test status (pass/fail/skip). CI-friendly output format. Flag regressions between runs. Current baseline: 39 at 100%.

---

#### TST-08: Add Diagnostic Severity and Code Stability Tests

Reflection-based tests asserting: all codes match `CBL[0-9]{4}`, no duplicate codes, valid format placeholders, severity is Error/Warning/Info. Snapshot baseline for regression detection.

---

# 6. Execution Protocol & Roadmap

## 6a. Constraints for Future Sessions

### A1. Task Independence
Each task must be implementable independently without re-designing architecture. A task must not require changes to the compiler pipeline architecture (lexer/parser/binder/IR/codegen layering).

### A2. Sequential Task Execution
Pick the next Planned task from the tracking table. Do not skip or reorder tasks unless a dependency requires it. One task in progress at a time.

### A3. No Alternative Architectures
The pipeline is settled: Preprocess -> Lex/Parse -> SemanticModel -> Validate -> Bind -> IR Lower -> CIL Emit. If a task seems to require architectural change, STOP and document a Plan Revision Proposal in DEVLOG.md.

### A4. Coverage Matrix and Status Tracking
Always update CLAUDE.md test counts, NIST test list, and known gaps after every session. Always update DEVLOG.md.

### A5. Regression Gates
No session may end with fewer passing tests than it started with. Run `dotnet test` before and after. Never skip tests or use `[Skip]` to hide failures.

### A6. Frozen Interfaces
- `PicDescriptor` 17-parameter constructor (CIL newobj emission)
- `IFileHandler` interface methods
- `AcceptRuntime.Accept` signature (AcceptSourceKind)
- `FigurativeKind` enum values (cast to int at CIL boundary)
- Generated parser files (only regenerated from .g4 changes)

---

## 6b. Remediation Roadmap

### Priority 1 — Critical: Correctness Blockers

| ID | Issue | Complexity | NIST Blocked | Status |
|---|---|---|---|---|
| P1-1 | NC220M infinite loop at runtime | M | NC220M | Open |
| P1-2 | NC121M subscripted DIVIDE GIVING | — | NC121M | **Already fixed** (DEVLOG Entry 126) |
| P1-3 | NC201A/NC250A/NC252A parse failures | M | 3 tests | Open (NOT VALUE THRU -- corrected by research) |
| P1-4 | STATUS in SPECIAL-NAMES keyword conflict | S | NC174A, NC211A, NC254A | Open |
| P1-5 | PROGRAM COLLATING SEQUENCE keyword conflict | S | NC215A, NC219A | Open |

### Priority 2 — High: Feature Completions

| ID | Issue | Complexity | Dependencies |
|---|---|---|---|
| P2-1 | ~~CALL/USING/RETURNING full IR~~ | ~~L~~ — **DONE** | |
| P2-2 | SORT/MERGE full implementation | L | P2-5 |
| P2-3 | Alternate keys (indexed files) | L | P2-5 |
| P2-4 | START statement full implementation | M | P2-5 |
| P2-5 | Indexed and relative file I/O backends | XL | None |
| P2-6 | Reserved words as paragraph names | S | None |

### Priority 3 — Medium: Code Quality

| ID | Issue | Complexity |
|---|---|---|
| P3-1 | Eliminate all silent statement skips | S |
| P3-2 | Remove legacy CobolProgram/CobolField types | M |
| P3-3 | Complete deferred semantic features (qualification, sign, abbreviated relations) | M each |
| P3-4 | C# 13 adoption in remaining files | S |
| P3-5 | Package version updates | S |

### Priority 4 — Low: Future Considerations

| ID | Issue | Complexity |
|---|---|---|
| P4-1 | Report Writer (INITIATE/GENERATE/TERMINATE) | XL |
| P4-2 | OO COBOL (CLASS, INVOKE, METHOD) | XL |
| P4-3 | Exception handling (RAISE/RESUME) | L |
| P4-4 | Screen Section (ACCEPT/DISPLAY with Screen) | L |
| P4-5 | ~~ALTER statement~~ | ~~M~~ — **DONE** |
| P4-6 | CI/CD pipeline (GitHub Actions) | M |
| P4-7 | ~~Dynamic CALL~~ | ~~L~~ — **DONE** |

---

## Suggested Phasing

### Wave 1: Correctness Blockers (Target: 46+ NIST tests passing)

1. **P1-4** (STATUS in SPECIAL-NAMES) — small grammar fix, unblocks 3 NIST tests
2. **P1-5** (PROGRAM COLLATING SEQUENCE) — small grammar fix, unblocks 2+ NIST tests
3. **P1-1** (NC220M hang) — requires runtime debugging
4. **P1-3** (NC201A/NC250A/NC252A) — parse issue diagnosis

Estimated effort: 2-3 sessions.

### Wave 2: Core Feature Completions (Target: production-viable compiler)

1. **P2-6** (reserved word conflicts) — small fix
2. **P2-1** (CALL full IR) — high value, foundational
3. **P2-4** (START statement) — grammar fix + handler wiring
4. **P2-5** (indexed/relative file I/O) — largest item
5. **P2-2** (SORT/MERGE) — depends on file I/O maturity
6. **P2-3** (alternate keys) — depends on P2-5

Estimated effort: 5-8 sessions.

### Wave 3: Code Quality and Spec Compliance

1. **P3-1** (eliminate silent skips) — quick win
2. **P3-3** (deferred semantic features) — one at a time
3. **P3-2** (remove legacy types) — requires test migration
4. **P3-4** and **P3-5** (modernization, package updates) — mechanical

Estimated effort: 3-5 sessions.

---

## Dependency Graph

```
P1-4 (STATUS keyword) ────────────────────────────────────> standalone
P1-5 (PROGRAM keyword) ───────────────────────────────────> standalone
P1-1 (NC220M hang) ───────────────────────────────────────> standalone
P1-3 (NC201A parse) ──────────────────────────────────────> standalone

P2-6 (reserved words) ────────────────────────────────────> standalone
P2-1 (CALL full IR) ──────────────────────────────────────> P4-7 (dynamic CALL)
P2-4 (START) ─────────────────────────────────────────────> P2-5 (indexed files)
P2-5 (indexed/relative files) ────────────────────────────> P2-3 (alternate keys)
                                                           > P2-4 (START full test)
P2-2 (SORT/MERGE) ────────────────────────────────────────> P2-5 (for file-based SORT)

P3-1 (silent skips) ──────────────────────────────────────> standalone
P3-2 (legacy types) ──────────────────────────────────────> standalone (test rewrite)
P3-3 (semantic features) ─────────────────────────────────> standalone (each independent)
```

---

## Task Status Tracking

| ID | Task | Status | Completed | Notes |
|---|---|---|---|---|
| T-001 | Fix NC121M subscripted DIVIDE GIVING | Done | 2026-03-20 | Fixed in DEVLOG Entry 126 |
| T-002 | Fix NC220M infinite loop at runtime | Planned | | IrElementRef destination likely cause |
| T-003 | Fix NC201A/NC250A/NC252A parse failures | Planned | | NOT VALUE THRU (research corrected) |
| T-004 | STATUS in SPECIAL-NAMES grammar fix | Planned | | Unblocks NC174A, NC211A, NC254A |
| T-005 | PROGRAM COLLATING SEQUENCE grammar fix | Planned | | Unblocks NC215A, NC219A |
| T-006 | Reserved words as paragraph names | Planned | | Context-sensitive keyword handling |
| T-007 | CALL IR implementation (inter-program) | **Done** | | Full implementation with Entry methods + CobolProgramRegistry |
| T-008 | SORT/MERGE full IR implementation | Planned | | Currently parse only |
| T-009 | Alternate keys (ISAM) | **Done** | | Parsed, stored, registered, secondary indices maintained |
| T-010 | REWRITE FROM lowering (7-line fix) | Planned | | FROM move not emitted |
| T-011 | START key condition | **Done** | | KeyCondition extracted from bound tree → StartCondition enum |
| T-012 | Duplicate expression binding elimination | Planned | | ~90 lines of duplication |
| T-013 | Silent zero for function calls | Planned | | No diagnostic emitted |
| T-014 | Unresolved identifier string fallback | Planned | | No diagnostic emitted |
| T-015 | Route diagnostics through descriptors | Planned | | 50+ raw string codes |

---

## Success Criteria

The remediation roadmap is complete when:

1. **All NIST COBOL-85 Nucleus tests pass at 100%.** NC220M, NC201A, NC233A, NC237A, NC247A, NC250A, NC252A all pass.
2. **Zero stubs in code generation.** CALL produces correct runtime behavior. SORT, MERGE remain stubs.
3. **Zero silent skips.** Every unhandled construct produces a compile-time diagnostic.
4. **No runtime hangs.** All compiled programs terminate correctly.
5. **Clean codebase per PROMPT.md standards.** No legacy types, consistent C# 13, no dead code, no duplicated logic.
6. **All tests green.** Unit, integration, and NIST tests all pass with zero unexplained failures.
7. **Indexed file I/O operational.** READ, WRITE, REWRITE, DELETE, START all work for indexed files.
8. **CALL interop works.** Inter-program calls with BY REFERENCE/CONTENT/VALUE parameters.

---

## Complexity Key

| Size | Definition |
|---|---|
| S | < 1 session. Localized change, few files, clear fix. |
| M | 1-2 sessions. Multiple files, some design decisions. |
| L | 2-4 sessions. Cross-cutting change, new subsystem wiring. |
| XL | 4+ sessions. New runtime subsystem, major grammar changes. |

---

*End of Audit Report.*
