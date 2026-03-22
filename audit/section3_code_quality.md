# Section 3: Code Quality & Patterns

## 3.1 C# Language Version and .NET Target

The project targets **.NET 9.0** with **C# 13** (`LangVersion 13`), configured centrally via
`/Directory.Build.props`. The SDK version is pinned in `/global.json` to `9.0.312` with
`rollForward: latestPatch`.

Key build settings applied solution-wide:

| Setting                   | Value    | Assessment |
|---------------------------|----------|------------|
| `TargetFramework`         | net9.0   | Current LTS-adjacent (good) |
| `LangVersion`             | 13       | Latest stable for .NET 9 |
| `Nullable`                | enable   | Full nullable reference type enforcement |
| `TreatWarningsAsErrors`   | true     | Strict — forces clean compilation |
| `ImplicitUsings`          | enable   | Reduces boilerplate |

Central package management is used via `/Directory.Packages.props`, with version pinning for all
five dependencies (Antlr4.Runtime.Standard 4.13.1, Mono.Cecil 0.11.6, xunit 2.9.2, etc.).

**Assessment**: The build configuration follows modern .NET best practices. Nullable annotations
combined with warnings-as-errors creates a strong compile-time safety net. Central version
management avoids dependency version drift across the five projects.

## 3.2 Modern C# Feature Adoption

The codebase makes meaningful use of modern C# features across all compiler phases.

### Records (19 usages in hand-written code)

Records are used for immutable value types throughout the compiler and runtime:

- `sealed record DiagnosticDescriptor`, `sealed record Diagnostic` (`Diagnostics/`)
- `sealed record IrTemp`, `sealed record IrField`, `sealed record IrGlobal` (`IR/`)
- `readonly record struct TextSpan`, `readonly record struct IrValue`, `readonly record struct StorageLocation` (value-semantic types)
- `sealed record CompilationResult`, `sealed record PicLayout`, `sealed record DataTypeSymbol`
- `sealed record PicEnvironment` (Runtime)

Records are chosen appropriately: value-semantic types that benefit from structural equality get
`record struct`, reference types with identity get `sealed record`. No misuse of records for
mutable state was observed.

### Switch Expressions (6 usages)

Switch expressions appear in appropriate contexts:

- `BoundTreeBuilder.cs` line 31: `Typed<T>()` method uses nested switch expression for expression
  type assignment — a 20-line switch expression that cleanly maps bound node types to result types.
- `RecordLayoutBuilder.cs` line 144: Binary size selection by USAGE kind.
- `FieldSizeCalculator.cs` line 45: `ComputeBinarySize()` maps digit counts to byte sizes.
- `PicRuntime.cs` line 851: `FigurativeToByte()` maps figurative constants to byte values.
- `CilEmitter.cs` line 2599: `GetPicForLocation()` pattern-matches on `IrLocation` subtypes.

### Pattern Matching (~190 `is` pattern usages)

Pattern matching is used extensively, particularly in `BoundTreeBuilder.cs` (111 occurrences)
and `Binder.cs` (41 occurrences). Common patterns include:

- Declaration patterns: `ctx.dataReference() is { } delimId` (null-check with binding)
- Type patterns in switch statements: `case BoundPerformStatement perf:` (BoundTreeValidator)
- Property patterns in the CilEmitter for IR instruction dispatch

### Primary Constructors (18 usages)

Used for lightweight classes with injected dependencies:

- `BasicBlock(int id, bool isExit = false)` (`FlowAnalysis/`)
- `IrMethod(string name, IrType? returnType)`, `IrBasicBlock(string name)` (`IR/`)
- `PerformRangeChecker(SymbolTable symbols, DiagnosticBag diagnostics)` (`FlowAnalysis/`)
- `CobolErrorListener(DiagnosticBag diagnostics, string sourcePath)` (`Parsing/`)
- `CopyProcessor(IEnumerable<string>? searchPaths = null)` (`Preprocessor/`)
- Several `GenericClauseNode` subtypes (`Semantics/`)

### Collection Expressions

The C# 12 collection expression syntax `[]` is used for field initializers throughout the
codebase (e.g., `private readonly List<DataSymbol> _dataItemsInOrder = [];` in
`SemanticBuilder.cs` line 22). The `new List<T>()` form still appears in 107 locations —
predominantly in `BoundTreeBuilder.cs` (63) where local lists are constructed inside methods.
This is a minor inconsistency but not a quality concern.

### Features Not Used

- **Spans/stackalloc**: Only a single `ReadOnlySpan<byte>` usage in `CobolField.cs` line 42.
  The runtime does extensive `byte[]` manipulation (PicRuntime at 2045 lines) without Span-based
  APIs. This is a potential performance optimization opportunity, though the current approach is
  correct.
- **File-scoped types**: Not used (not needed given the project structure).
- **Raw string literals**: Not observed; string interpolation is used consistently.

## 3.3 Code Style Consistency

### Naming Conventions

Naming is highly consistent across all 55,000+ lines of hand-written code:

| Element            | Convention           | Adherence |
|--------------------|----------------------|-----------|
| Classes            | PascalCase           | 100% (`BoundTreeBuilder`, `CilEmitter`, `PicRuntime`) |
| Interfaces         | IPascalCase          | 100% (`IFileHandler`) |
| Public properties  | PascalCase           | 100% |
| Private fields     | `_camelCase`         | 100% (`_semantic`, `_diagnostics`, `_paragraphMethods`) |
| Local variables    | camelCase            | 100% |
| Methods            | PascalCase           | 100% |
| Enums              | PascalCase members   | 100% (`BoundNodeKind`, `OpenMode`, `IrBinaryOp`) |
| Constants          | PascalCase           | 100% (diagnostics: `CBL0901`, `CBL1001`, etc.) |
| Type parameters    | T prefix             | 100% (`Typed<T>()`) |
| Namespaces         | Dot-separated Pascal | 100% (`CobolSharp.Compiler.Semantics.Bound`) |

COBOL is case-insensitive, and the codebase correctly handles this via
`StringComparer.OrdinalIgnoreCase` on all symbol-lookup dictionaries (50 usages across 17
files). Zero instances of `.ToUpper()` or `.ToLower()` for case normalization were found --
the correct `OrdinalIgnoreCase` approach is used universally.

### Sealed Class Discipline

Classes are marked `sealed` by default (201 occurrences across 35 files). The only unsealed
classes are abstract base types (`BoundNode`, `BoundExpression`, `BoundStatement`,
`IrInstruction`, `IrType`, `IrLocation`) and the ANTLR-generated code. This prevents
unintended inheritance and enables JIT devirtualization.

### File-Scoped Namespaces

All hand-written source files use the C# 10 file-scoped namespace syntax (`namespace X;`).
No block-scoped namespace declarations were found outside the ANTLR-generated code.

### XML Documentation

Key public APIs have XML doc comments. Coverage is strong on:
- All IR instruction classes (purpose and semantics documented)
- BoundNode subtypes (especially where COBOL semantics are non-obvious)
- Runtime methods (PicRuntime operations document ISO standard references)
- Diagnostic descriptors (each carries a code, severity, and message template)

### Copyright Headers

Every hand-written source file begins with the standard copyright header:
```
// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1.
```

## 3.4 Complexity Hotspots

### Largest Files (excluding generated ANTLR code)

| File | Lines | Role | Assessment |
|------|-------|------|------------|
| `BoundTreeBuilder.cs` | 3,249 | Parse tree to bound tree | Large but inherently driven by COBOL grammar breadth |
| `Binder.cs` | 3,148 | Bound tree to IR lowering | Same justification — one method per COBOL statement type |
| `CilEmitter.cs` | 2,894 | IR to CIL emission | One handler per IR instruction type |
| `PicRuntime.cs` | 2,045 | COBOL data movement/arithmetic | COBOL PIC semantics are inherently complex |
| `IrInstruction.cs` | 1,233 | IR instruction definitions | Many small sealed classes; low per-class complexity |
| `BoundNodes.cs` | 1,229 | Bound node definitions | Same pattern as IrInstruction.cs |
| `SemanticBuilder.cs` | 883 | Symbol table construction | Moderate size, well-structured |

**Analysis of the top 3 files:**

`BoundTreeBuilder.cs` (3,249 lines) is a visitor over the ANTLR parse tree with one `Visit*`
or `Bind*` method per COBOL statement type. Each method is 20-80 lines. The complexity is
inherent to COBOL's grammar breadth (MOVE, PERFORM, IF, EVALUATE, STRING, UNSTRING, INSPECT,
SEARCH, etc.). There is no god-class issue here — the class has a single responsibility
(syntax-to-bound-tree translation) with a wide but shallow method surface.

`Binder.cs` (3,148 lines) mirrors this pattern: one `Lower*` method per bound statement type.
The dispatch is clean via a single `LowerStatement` switch. Methods are individually small.

`CilEmitter.cs` (2,894 lines) follows the same one-handler-per-instruction pattern. The main
`EmitInstruction` method at line ~660 is a large `switch` (30+ cases), but each case delegates
to a focused `Emit*` method. This is the standard pattern for instruction emitters.

**Verdict**: The large files reflect the breadth of COBOL language features, not poor
decomposition. Each follows the Visitor or Emitter pattern where a single class must handle
all node types. The per-method complexity is reasonable.

### Deepest Nesting

The deepest nesting observed is 4-5 levels in `BoundTreeBuilder.cs` methods that handle
compound COBOL statements (STRING/UNSTRING with multiple optional clauses). This is acceptable
given the grammar structure.

## 3.5 Anti-Pattern Assessment

### Patterns Observed (Positive)

1. **Immutable data flow**: Bound nodes and IR instructions are constructed once and not mutated.
   Properties are get-only throughout BoundNodes.cs and IrInstruction.cs.

2. **Diagnostic descriptor registry**: All diagnostic codes are centralized in
   `DiagnosticDescriptors.cs` (348 lines) as `static readonly` fields with structured codes
   (CBL0901, CBL1001, etc.). No magic strings for error codes.

3. **Type-safe dispatch**: Statement lowering uses exhaustive type-pattern switches rather than
   string-based or enum-based dispatch. The `default` case in `CilEmitter.EmitInstruction`
   throws `NotSupportedException`, catching unhandled instruction types at compile-test time.

4. **Clean phase separation**: The `Compilation.cs` facade (239 lines) orchestrates six phases
   (Preprocess, Lex/Parse, Grammar validation, Semantic analysis, Bind/IR, CIL emit) with
   each phase delegated to a dedicated class. No phase leaks into another.

5. **No mutable shared state**: Each compilation creates fresh instances of `DiagnosticBag`,
   `SemanticModel`, `Binder`, and `CilEmitter`. No static mutable state outside of ANTLR
   generated code.

6. **Correct COBOL case handling**: All 50 uses of `StringComparer.OrdinalIgnoreCase` are on
   symbol dictionaries and lookup maps. Zero `.ToUpper()/.ToLower()` calls.

### Potential Concerns

1. **Console.Error debug output in CilEmitter**: `CilEmitter.cs` lines 82-84 contain
   `Console.Error.WriteLine` calls that emit diagnostic information about IR methods during
   compilation. There are 6 such calls in CilEmitter and 25 in the CLI. These should ideally
   be behind a verbosity flag or use a proper logging abstraction. Not a bug, but a polish item.

2. **`object Value` in `BoundLiteralExpression`**: The `Value` property at `BoundNodes.cs` line
   65 is typed as `object`, requiring runtime type checks at consumption sites (e.g.,
   `IrLoadConst.Value` at `IrInstruction.cs` line 88, and `CilEmitter.EmitLoadConst` at line
   755 which switches on `int`, `long`, `string`, `bool`). A discriminated union or generic
   approach would be more type-safe, though this is a common pragmatic choice in compilers.

3. **Three duplicate XML doc comment blocks**: `CilEmitter.cs` lines 2651-2658 have three
   consecutive `<summary>` blocks for what appears to be a single method
   (`EmitElementAddress`). These look like leftover revisions that were not cleaned up.

4. **`new` keyword hiding base property**: `IrCheckFileAtEnd` at `IrInstruction.cs` line 469
   declares `public new IrValue Result { get; }`, which hides the base class `Result` property.
   This is a code smell — it means the base and derived `Result` can diverge. The
   `TreatWarningsAsErrors` setting means this was intentional (the `new` keyword suppresses
   CS0108), but it deserves a comment explaining why.

### No Anti-Patterns Found

- No god classes (each class has a focused responsibility)
- No deeply nested conditionals beyond 5 levels
- No magic numbers (constants like storage sizes have documented defaults)
- No string concatenation in hot paths (string interpolation used correctly)
- No `#region` blocks or `#if` directives in hand-written code
- No `#pragma warning disable` in hand-written code (only in ANTLR-generated files)

## 3.6 Technical Debt Markers

### TODO Comments (2 instances)

1. `/src/CobolSharp.CLI/Program.cs` line 148:
   `// TODO: pass standard to Compilation when grammar overlays are wired up`
   — Minor: deferred standards-version configuration.

2. `/src/CobolSharp.Compiler/Semantics/Bound/BoundTreeBuilder.cs` line 3067:
   `// TODO: proper function binding`
   — The `BindPrimaryExpr` method returns a placeholder `BoundLiteralExpression(0m)` for
   function calls. This means intrinsic functions like `FUNCTION LENGTH` return zero in
   arithmetic expressions.

### Stub Implementations (5 instances)

1. **RETURN statement** (`Binder.cs` lines 1352-1364): Emits a display message and always takes
   the AT END path. Documented as "sort/merge, not yet supported."

2. **CALL statement** (`Binder.cs` lines 1371-1383): Emits a display message and always takes
   the ON EXCEPTION path. Documented as "inter-program linkage not yet supported."

3. **ReportWriterValidator** (`Semantics/ReportWriterValidator.cs` line 10): Entire class is a
   documented stub — "full validation deferred until Report Writer codegen is implemented."

4. **National data type stubs** (`PicRuntime.cs` lines 1303, 1378): MOVE operations involving
   National (UTF-16) data types are stubbed.

5. **Console input** (`AcceptRuntime.cs` line 29): Plain ACCEPT fills with spaces rather than
   reading console input.

### Dead Code

No dead code was identified. All classes, methods, and types sampled are referenced from active
compilation paths. The `InternalsVisibleTo` attribute on the Compiler project enables unit test
access to internal types without exposing them publicly.

## 3.7 Summary

| Dimension | Rating | Notes |
|-----------|--------|-------|
| Build configuration | Excellent | Modern .NET 9 / C# 13, nullable, warnings-as-errors |
| Modern C# adoption | Strong | Records, patterns, switch expressions, primary constructors |
| Naming consistency | Excellent | Zero deviations from .NET conventions observed |
| Sealed class discipline | Excellent | Default-sealed with abstract bases only where needed |
| Phase separation | Excellent | Clean pipeline: Preprocess, Parse, Semantic, Bind, IR, Emit |
| Anti-patterns | Minimal | `object Value` typing and `new` property hiding are minor |
| Technical debt | Low | 2 TODOs, 5 documented stubs, all for unimplemented COBOL features |
| Complexity management | Good | Large files reflect language breadth, not poor decomposition |
| Documentation | Good | XML docs on public APIs; copyright headers universal |
