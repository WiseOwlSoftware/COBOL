CobolSharp Master Architecture Document (CIL‑Only)
=================================================

> **STATUS BANNER — read first.** This is a **system-overview / integration DESIGN REFERENCE**, not a status report.
> It describes the *target* end-to-end architecture of the compiler+runtime. **Implementation reality (2026-06-07,
> DEVLOG 441):**
> - **Stack: .NET 10 / C# 14** (`Directory.Build.props` `net10.0` / LangVersion 14, `global.json` SDK 10.0.x). Any
>   ".NET 9"/"net9.0"/"C# 13" phrasing in older copies was stale and has been corrected here.
> - **Backend is CIL-only via Mono.Cecil. There is NO custom VM, NO bytecode interpreter, NO event loop / cooperative
>   scheduler, NO custom WASM/AOT emitter.** CobolSharp emits a standard .NET assembly; CoreCLR/AOT/WASM are obtained
>   *only* via the stock `dotnet publish` toolchain. A Roslyn C# backend is a **future additive Stage-5 option** with
>   Cecil as the differential oracle.
> - **Implemented today (~core complete):** Preprocessor, Lexer, Parser (ANTLR4), Semantic Analyzer (10-pass), IR,
>   IL Generator + the **decomposed** CIL backend (`CodeGen/Emission/Cil*Emitter.cs`, `CodeGen/Lowering/*Lowerer.cs`),
>   Runtime Library (NumericEngine via `CobolNum`/`CobolDecimal`, `CobolString`, `CobolFileManager` for
>   sequential/indexed/relative, `SortRuntime`, `ReportWriterRuntime`, `InspectRuntime`, 94 intrinsics, Screen/
>   Terminal). M1 (COBOL-85) is COMPLETE. Guard: **1196 unit / 509 integration / 364 NIST**.
> - **DESIGN-ONLY (≈0 lines today, future phases):** Debugger/PDB symbol generation, LSP/IDE integration, the CLI
>   developer-flow verbs (`cobolsharp new/run/debug/test/publish`), Packaging/Distribution product surface,
>   Modernization & Migration toolkit, the Governance/Compliance/Multi-tenant/Observability/Sandbox layers, the
>   "event loop / cooperative scheduler" runtime model, and JSON/XML PARSE/GENERATE runtime. Treat every such section
>   below as a forward design target, not current behavior.
> - **Data model is migrating** to typed-native .NET (`char→string` UTF-16, `numeric→long/decimal`, `groups→record
>   struct`, `OCCURS→T[]`, `pointers→ManagedPointer`) behind `EnableTypedFields` (default OFF). The byte/`StorageArea`
>   /`StorageBlock` engine described in several sections is being **islanded** as a classifier-scoped fallback. There
>   is exactly ONE pointer carrier — **`ManagedPointer`** (no 8-byte handle, no PointerRegistry; `CobolDataPointer`
>   was renamed to it). See `docs/DATA_MODEL_ARCHITECTURE.md` + `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.
> - **Already-decomposed (do NOT call these god classes):** `IrExpression` (M001), `CilEmitter`→11 emitters (M003),
>   `BoundTreeBuilder`→9 binders (M004), 9 lowering classes.
>
> **The authoritative top-level SSOT is `docs/MASTER_PLAN.md`** (plan/sequencing/status); doctrine is `PROMPT.md`. This
> overview document does NOT supersede MASTER_PLAN and is NOT merged into it. *Consolidated from 6 prior architecture
> essays, 2026-06-07.*

Purpose
-------
This document provides a unified, end‑to‑end architectural overview of CobolSharp — a production‑quality COBOL compiler targeting .NET CIL and implementing ISO/IEC 1989:2023 semantics. It consolidates the system-overview / integration vision into a single, coherent reference suitable for maintainers, contributors, and system architects.

CobolSharp is designed to:
- Compile COBOL directly to .NET CIL (a standard verifiable assembly)
- Provide a modern development experience (LSP, debugging, refactoring) — *design target*
- Support full COBOL semantics (85 → 2023)
- Integrate with .NET libraries and tooling
- Enable modernization and migration of legacy COBOL systems
- Produce deterministic, verifiable, cross‑platform .NET assemblies

Top‑Level Architecture
----------------------
CobolSharp consists of the following major subsystems (★ = implemented today; ◇ = design-only / future phase):

1. Preprocessor ★
2. Lexer ★
3. Parser (ANTLR4) ★
4. Semantic Analyzer (10-pass) ★
5. IR / IL Generator ★
6. Optimization Pipeline (IR-level) ★ (partial)
7. CIL Backend (Mono.Cecil) ★
8. Runtime Library ★
9. Debugger ◇
10. LSP/IDE Integration ◇
11. Packaging & Distribution ◇
12. Modernization & Migration Toolkit ◇
13. Interop Architecture (COBOL ↔ .NET) ★ (CALL/INVOKE .NET; cookbook is the live doc)

These subsystems form a pipeline:

```
  Source Files
      ↓
  Preprocessor
      ↓
  Lexer
      ↓
  Parser
      ↓
  Semantic Model
      ↓
  IR / IL Generation
      ↓
  Optimization Pipeline
      ↓
  CIL Backend (Mono.Cecil)
      ↓
  .NET Assembly (DLL/EXE) + PDB
      ↓
  Runtime Execution (CoreCLR / AOT / WASM via stock `dotnet publish`)
      ↓
  Debugger (optional, design)
      ↓
  IDE/LSP (optional, design)
```

There is **no VM, no bytecode interpreter, no alternate/custom backend.** AOT and WASM are obtained solely through the standard .NET toolchain; CobolSharp itself only emits CIL.

Subsystem Summaries
-------------------

1. Preprocessor ★
-----------------
Responsibilities:
- COPY expansion, REPLACE / COPY REPLACING, pseudo‑text handling
- Conditional compilation (`>>DEFINE` / `>>IF` / `>>EVALUATE`), `>>SOURCE FORMAT`, `*>` inline comments
- Source mapping (original → expanded) for diagnostics/debugging

Outputs: expanded source + mapping tables.
(Source: `Preprocessor/CopyProcessor.cs`, `ConditionalCompilationProcessor.cs`, `ReferenceFormatProcessor.cs`, `NistPreprocessor.cs`.)

2. Lexer ★
----------
Tokenizes expanded source into `{token type, lexeme, source location}`. Supports fixed‑form and free‑form, continuation lines, compiler directives, and dialect‑aware tokenization. Includes a SUBSCRIPT lexer mode and PIC‑string lexing (PIC token + raw picture string + sign/scale/category attributes). Error handling: unterminated literal, invalid character, invalid numeric literal. (Generated ANTLR lexer: `Generated/CobolLexer.cs`; co-located grammar fragments in `Core/`.)

3. Parser ★
-----------
Builds the AST using an **ANTLR4-generated** grammar (LL(*), SLL two-stage parsing with `BailErrorStrategy` for speed). Dialect gates (85/2002/2014/2023). Error recovery (panic mode, synchronization tokens, best-effort continuation → error nodes). Post-85 features are **version-factored** into `Core/CobolXXXX.g4` fragments behind `{isYYYY()}?`-gated hooks (never inlined into the COBOL-85 base). AST normalization transforms THRU ranges → explicit paragraph lists, abbreviated IF → full IF/ELSE, abbreviated PERFORM → full loops, EVALUATE → normalized match tree.

AST node families: Program, Division, Section, Paragraph, Statement, Expression, Condition, Picture, File description, Class/Method (OO).

4. Semantic Analyzer ★
----------------------
Builds: symbol tables (program / data-division / file / class-method / scope stack), type graph, data-description tree, PERFORM control-flow graph, OO/generics model, file model, JSON/XML model. Ensures type correctness, data-layout correctness, control-flow correctness, file-I/O semantics, and intrinsic-function resolution.

**Type resolution** maps PIC → numeric/alphanumeric/national/boolean category; COMP/COMP-3/COMP-5 → binary/packed; group items → struct/record-struct; OO types → .NET types. **Storage layout** computes offsets, lengths, alignment, REDEFINES overlays, OCCURS tables, and DEPENDING-ON bounds. **Control-flow binding** resolves paragraph labels, PERFORM ranges, GO TO targets, declarative routing. **File binding** resolves FD/SD metadata, keys, record lengths, access modes.

Implemented as a 10-pass pipeline (see `docs/SEMANTIC-ANALYSIS-ARCHITECTURE.md`, `CATEGORY-RULES.md`, `SCOPE-RULES.md`). The Binder produces bound nodes only; lowering turns bound nodes into IR. The classifier (`RecordClassificationPass`) decides per-item whether the typed-native model or the byte fallback applies.

5. IR / IL Generator ★
----------------------
Produces CobolSharp IR (`IrModule` / `IrType` / `IrMethod` / `IrInstruction` / `IrExpression` / IR basic blocks). IR is backend-agnostic but CIL-friendly, fully typed, and structured for optimization. `IrExpression` was decomposed (M001).

6. Optimization Pipeline ★ (partial)
------------------------------------
IR-level passes (target set): control-flow simplification, dead-code elimination, constant folding/propagation, copy propagation, redundant-move elimination, strength reduction, loop optimization, branch optimization, peephole, optional generic specialization, data-layout metadata optimization. Guarantees: semantic preservation, verifiable IL, improved CIL emission quality. (Live contract: `docs/ir/IR-Expression-Contract.md`.)

7. CIL Backend ★
----------------
Consumes optimized IR and emits a standard .NET assembly via **Mono.Cecil**: .NET types/methods/fields, IL instructions, metadata, and PDB debug symbols. Responsibilities: explicit layout for COBOL data structures, REDEFINES overlays, OCCURS-DEPENDING-ON dynamic bounds, PERFORM lowering, exception-region emission (ON EXCEPTION / INVALID KEY / AT END), sequence-point emission, and calls into `CobolSharp.Runtime` for complex semantics.

The backend is **decomposed** (NOT a god class): `CilEmitter` orchestrates 11 emitters under `CodeGen/Emission/` (`CilDataEmitter`, `CilArithmeticEmitter`, `CilComparisonEmitter`, `CilExpressionEmitter`, `CilControlFlowEmitter`, `CilStringEmitter`, `CilFileIoEmitter`, `CilLocationEmitter`, `CilProgramStateEmitter`, `CilModuleSetup`, …) plus 9 lowering classes under `CodeGen/Lowering/`. Outputs: `.dll`/`.exe` + `.pdb`; AOT/WASM only via downstream `dotnet publish`. (Live design: `docs/IL-BYTECODE-GENERATION-DESIGN.md`, `docs/cilemitter/CilEmitter-Decomposition.md`.)

8. Runtime Library ★
--------------------
Implements COBOL semantics in purely-managed, cross-platform code (CoreCLR / AOT / WASM compatible). Engines:
- **NumericEngine** — `CobolNum` (unsigned-int substrate over BigInteger) / `CobolDecimal` (signed-scaled), `CobolRounding`; packed-decimal byte path is being islanded.
- **StringEngine** — `CobolString` (UTF-16), `InspectRuntime` (STRING/UNSTRING/INSPECT), case mapping, NATIONAL.
- **FileManager** — `CobolFileManager` + sequential/indexed/relative handlers, file status codes.
- **SortMergeEngine** — `SortRuntime`.
- **ReportEngine** — `ReportWriterRuntime` (page/line control, control breaks, rendering).
- **Screen/Terminal** — `TerminalSession`, screen attribute mapping, CRT status.
- **IntrinsicFunctionLibrary** — 94 intrinsics (`Intrinsics/IntrinsicFunctions.cs`).
- **Pointers** — single `ManagedPointer` carrier (GC-tracked managed reference; no native heap, no handle table, no `unsafe`).
- ExternalStorage (EXTERNAL shared), `GlobalUseDeclarativeRegistry`, `CobolProgramRegistry`, runtime guards / `CobolRuntimeException`.

*Design-target engines not yet implemented:* JsonEngine / XmlEngine (PARSE/GENERATE runtime), a full EC/exception ExceptionEngine, DateTimeEngine/CollationEngine as standalone services. The "event loop / cooperative scheduler / async interop" model in the integration essays is speculative — execution today is straight synchronous CIL.

9. Debugger ◇ (design-only, Phase E)
------------------------------------
Target: .NET debugging APIs (ICorDebug / Diagnostics Protocol) + PDB sequence points + semantic-model metadata. Features: breakpoints (line/conditional/hit-count), step in/out/over, paragraph/section stepping, PERFORM-flow visualization, variable inspection (PIC/USAGE aware), OCCURS/REDEFINES visualization, memory inspection, expression evaluation, declarative tracing (triggered declarative + ExceptionState + resume location). Compiler already emits sequence points + symbol names to support this later.

10. LSP/IDE Integration ◇ (design-only)
---------------------------------------
Target: syntax/semantic highlighting, completion, hover (PIC/USAGE/OCCURS/REDEFINES), signature help, go-to-definition, find references, rename refactoring, code actions, diagnostics, and debugger integration. The LSP would reuse the same incremental semantic model (preprocessor → lexer → parser → semantic passes update as the developer types).

11. Packaging & Distribution ◇ (design-only, Phase E)
-----------------------------------------------------
Target artifacts: `cobol.exe` (the CLI, post-rename), `cobolsharp-lsp.exe`, `CobolSharp.Runtime.dll`, tools (IL viewer, CFG viewer, data-layout inspector), templates, documentation. Channels: NuGet, ZIP/TAR, native installers, package managers, `dotnet tool install`. Properties: deterministic builds, code signing, reproducible artifacts. (A CI template exists but is disabled.)

12. Modernization & Migration Toolkit ◇ (design-only)
-----------------------------------------------------
Target: codebase analysis, dependency graphs, data-layout extraction, modernization advisor, automated refactoring engine, interop-layer generation, file-format migration tools, modernization reports, CI/CD modernization gate — for incremental modernization of legacy COBOL.

13. Interop Architecture (COBOL ↔ .NET) ★
-----------------------------------------
COBOL → .NET: `CALL "Namespace.Class::Method"`, `INVOKE object::Method`, .NET constructors, properties, generics, async via helpers. .NET → COBOL: COBOL classes become .NET classes; COBOL programs become callable entry points; marshaling metadata ensures type safety. Shared type system: PIC/USAGE → .NET types; group items → explicit-layout / record-struct types; OCCURS → arrays; REDEFINES → overlapping fields; 88-levels → enum-like constants. (The how-to lives in the separate live cookbook `docs/CobolSharp COBOL-to-C# Interop Cookbook.md`; the design-only essays in `docs/CobolSharp ... Interop Architecture ...md` are a distinct cluster.)

------------------------------------------------------------
APPENDIX A — Full System Map (ASCII)
------------------------------------------------------------
*(Consolidated from the End-to-End Architecture Diagram and Final Integration essays. The "Event Loop" box is a
design target only — current execution is synchronous CIL with no scheduler.)*

```
+---------------------------+
|        Source Files       |   *.cbl  /  *.cpy copybooks  /  build manifest
+-------------+-------------+
              |
              v
+---------------------------+
|         Compiler          |   Lex → Parse → Bind → IR → Optimize → CIL
+-------------+-------------+
              |
              v
+---------------------------+
|   Assembly + PDB          |   (+ ProgramRegistry — design)
+-------------+-------------+
              |
              v
+---------------------------+
|       Runtime Loader      |
+-------------+-------------+
              |
              v
+---------------------------+
|    ExecutionContext       |   StorageAreas (byte fallback, islanding) /
| typed-native fields/objs  |   typed record-struct + ManagedPointer
+-------------+-------------+
              |
              v
+---------------------------+
|  Subsystem calls (sync)   |
+---------------------------+
   |      |       |      |
   v      v       v      v
 File   JSON★    SORT   Screen
 I/O    XML◇            /Report
   \      |       |      /
    +-----+-------+-----+
              |
              v
+---------------------------+
|       Subsystems          |  Numeric / String / File / Sort / Report
| (JSON/XML = design-only)  |
+-------------+-------------+
              |
              v
+---------------------------+
|     Output / FS / Host    |
+---------------------------+
```
(★ implemented; ◇ design-only.)

------------------------------------------------------------
APPENDIX B — Subsystem Boundaries & I/O Contracts
------------------------------------------------------------
*(From the End-to-End Architecture Diagram essay. Inputs/outputs describe the target contract; the byte-image
"StorageBlock slice" inputs apply to the islanded fallback path — the typed path passes native values.)*

- **NumericEngine** — in: value + PIC metadata; out: numeric value / encoded bytes (decimal arithmetic, COMP-3/COMP-5, ROUNDED).
- **StringEngine** — in: string/field slices; out: modified fields / temporaries (STRING/UNSTRING, INSPECT, NATIONAL, case mapping).
- **FileManager** — in: FD metadata + record buffers; out: updated records + status codes (sequential/indexed/relative; indexed via B-tree; file status codes).
- **JsonEngine / XmlEngine** ◇ — in: fields + JSON/XML text; out: fields + ExceptionState (SAX parsing, GENERATE, error routing). *Runtime not yet implemented (Phase C).*
- **SortEngine** — in: record buffers + key metadata; out: sorted output (key extraction, collation; external merge sort is the target — current `SortRuntime` is in-memory).
- **ReportEngine** — in: report metadata + fields; out: UTF-16 text (page/line control, control breaks).

------------------------------------------------------------
APPENDIX C — Control-Flow & Program Model
------------------------------------------------------------
- **Paragraph execution** — each paragraph lowers to an IL method; control flows via PERFORM / GO TO / declaratives. Dispatch uses a symbol-based control-transfer engine (handles duplicate names, inverted PERFORM…THRU ranges, DECLARATIVES off-by-N).
- **CALL / ENTRY** — CALL creates a fresh program state with new storage (except COMMON / EXTERNAL); ENTRY is method dispatch with LINKAGE mapping. (Implemented; see the CALL/Program-Model cluster.)
- **Declaratives** — triggered by file errors, (future) JSON/XML errors, and standard exceptions; USE AFTER routing implemented, full EC exception model is Phase C.

------------------------------------------------------------
APPENDIX D — End-to-End Developer Experience (DESIGN TARGET)
------------------------------------------------------------
*(From the End-to-End Developer Experience Flow essay. The `cobolsharp …` CLI verbs are a design target; post-rename
the CLI is `cobol.exe`. None of these workflow verbs are implemented yet — the current CLI compiles+runs programs for
the guard/test harness. Treat this section as the intended UX, not present behavior.)*

Intended workflow: **new → edit (LSP) → build → run → debug → test → publish → maintain**, consistent across Windows/macOS/Linux and VS Code / JetBrains / Visual Studio / Vim, and across local / CI / production.

- **Project layout (target):** `/src` (Program.cbl, Copybooks/, Classes/, Data/, Tests/), `/build`, `/docs`, `cobolsharp.json` (dialect 85/2002/2014/2023, optimization level, CIL options, runtime settings, COPY search paths).
- **Build pipeline:** preprocess → lex → parse → semantic analysis → IR → optimize → CIL backend → emit assembly + PDB (+ optional AOT-ready). Outputs: `.exe`/`.dll`, PDB, diagnostics, artifacts.
- **Run / Publish:** loads the generated assembly + `CobolSharp.Runtime.dll`, initializes WORKING-STORAGE / FILE SECTION, executes under .NET. Deployment via stock `dotnet publish`: CoreCLR (default), `/p:PublishAot=true` (native), `-blazorwasm`/WASI (WASM). **CobolSharp does not implement its own WASM or native backend; it relies on .NET.**
- **Test harness (target):** unit / golden / integration / regression / conformance / cross-compiler-equivalence; diff output for golden tests; optional coverage. (Today: `scripts/guard.sh` + `guard-fast.sh` run unit + integration + NIST + conformance.)
- **Cross-cutting target UX:** incremental everything (preprocess/lex/parse/semantic/diagnostics/LSP), deep COBOL awareness (PIC/USAGE, OCCURS, REDEFINES, RENAMES, 88-levels, PERFORM flow, COPY/REPLACE, OO, JSON/XML, file I/O), reproducibility (deterministic builds, reproducible IL/PDB, locked deps, build manifests), CI/CD integration (GitHub Actions, Azure DevOps, GitLab CI, Jenkins, TeamCity).

------------------------------------------------------------
APPENDIX E — Determinism, Security, Governance, Multi-Tenancy (FORWARD/ASPIRATIONAL)
------------------------------------------------------------
*(From the Final Integration essay. These describe an aspirational production-hosting/governance layer. NONE of it is
implemented — there is no sandbox, no multi-tenant runtime, no observability/governance subsystem today. Retained as a
long-horizon vision; do not treat as architecture-in-place.)*

- **Determinism & reproducibility (goal):** same input → same output; same build → same IL; cross-platform equivalence `Eval_CoreCLR(P,I) = Eval_AOT(P,I) = Eval_WASM(P,I)`; deterministic SORT / file-index / REPORT / (future) JSON/XML.
- **Security & sandboxing (goal):** forbid reflection, dynamic loading, `unsafe`, raw network/OS access; rely on the WASM sandbox (no syscalls/threads/raw memory) and AOT hardening (no JIT, no dynamic IL) where deployed; optional safe mode (no CALL, no file I/O); COPY/REPLACE path sanitization. *Note: the data model now uses managed references (`ManagedPointer`); the runtime is purely managed with no `unsafe`/`stackalloc`/unmanaged memory.*
- **Observability (goal):** logs (paragraph entry, file I/O, JSON/XML events, SORT/MERGE, declaratives, ExceptionState), metrics (time, memory, I/O counts), tracing (paragraph/subsystem/I/O spans).
- **Governance & compliance (goal):** immutable build artifacts + hashes, audit logs (program/registry version, tenant ID, file ops, ExceptionState), regulatory alignment (SOX/HIPAA/PCI-DSS/GDPR/GLBA).
- **CI/CD & deployment (goal):** build → test → cross-target test → security scan → package → sign → deploy; version pinning (compiler/runtime/registry/config); rollback by previous build hash + registry + config.
- **Multi-tenant execution (goal):** per-tenant ExecutionContext / FileManager root / PRNG seed; per-tenant resource quotas (CPU/memory/file handles/JSON-XML depth); deterministic cooperative scheduling.

------------------------------------------------------------
APPENDIX F — Cross-Cutting Engineering Concerns
------------------------------------------------------------
*(From the Full System Integration Blueprint.)*
- **Source mapping (implemented for diagnostics; debugger/LSP consumers are design):** every stage preserves mapping — original source → preprocessed → tokens → AST → semantic nodes → IR → CIL → PDB — enabling accurate breakpoints/stepping/diagnostics/variable inspection.
- **Error handling:** preprocessor errors map to original source; lexer errors → recoverable tokens; parser errors → error nodes; semantic errors → diagnostics (CBLxxxx descriptors); backend errors → build failures; runtime errors → COBOL exceptions.
- **Performance strategy:** cached preprocessor expansions / semantic models, lazy expensive analyses, parallel CIL emission where safe, optimized runtime ops; (incremental LSP pipeline is a design target).
- **Testing strategy:** unit + golden (IL/CIL) + integration + cross-compiler-equivalence + conformance (ISO/IEC 1989:2023, in `tests/conformance/<ver>/`) + regression suite. Guard = `scripts/guard.sh` / `guard-fast.sh` (proven byte-identical via `guard-verify.sh`).

Summary
-------
The CobolSharp Master Architecture:
- Defines a complete, modern COBOL compiler targeting **.NET CIL only** (Mono.Cecil; no VM/interpreter)
- Provides a unified, modular, extensible system across compiler + runtime
- Implements full COBOL-85 semantics today, migrating to a typed-native .NET data model, with .NET integration
- Treats debugging/refactoring/CI-CD/packaging/governance/multi-tenancy as forward design targets
- Produces deterministic, verifiable, cross-platform .NET assemblies
- Defers all plan/sequencing/status to **`docs/MASTER_PLAN.md`** (the SSOT) and doctrine to `PROMPT.md`
