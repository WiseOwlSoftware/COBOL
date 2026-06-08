# COBOL.NET — Architecture (greenfield COBOL → C# compiler)

> ⛔ **SUPERSEDED FOR DEPTH by `docs/COBOLNET_DESIGN.md`** (the decision-complete SSOT — pipeline/bound-tree, data
> model, numeric, control-flow PC-dispatcher, REDEFINES, strings, files, interprogram, OO, conditions/exceptions,
> intrinsics, project reorg/rename, no-god-class structure, C# 14 usage, the §18 settled decisions, and the G0–G8
> build order). This file remains the brief overview; where it conflicts, `COBOLNET_DESIGN.md` wins — notably the
> §3 numeric table below, corrected to native `long`/`Int128`-unscaled (NO `decimal`).

> **Status: LIVE / under construction.** This is the single source of truth for the blank-slate rewrite
> directed by the owner on 2026-06-08: *"Move entirely to .NET representations for COBOL objects … totally
> rewrite it … the best possible COBOL to .NET implementation."* It supersedes the byte-substrate compiler in
> `src/CobolSharp.Compiler` (the *legacy* engine, kept only as a differential oracle until cut-over, task G8) and
> the gated migration roadmap in `RECORD_STRUCT_STORAGE_DESIGN.md`. See memory
> `feedback_complete_dotnet_migration_no_byte`.

## 1. North Star

The best **native** .NET implementation of COBOL. A COBOL record **is** a .NET `record struct`; a COBOL
elementary item **is** a native .NET field (`string` / `long` / `Int128` / `bool` / `float` / `double` / typed array). A
COBOL program is a .NET class; COBOL OO classes are .NET classes. **There is no byte-array storage substrate** —
no `ProgramState`, no `byte[]`-at-offset model, no `(byte[],offset,length)` operations. A byte image exists only
*transiently* at an unavoidable boundary (file I/O / `CODE-SET`, a runtime-API call that needs bytes), built into
a fresh scratch buffer, never persisted.

The compiler translates COBOL to **idiomatic, readable C# source** and compiles it with **Roslyn**. Readable
output is a feature: it is inspectable, debuggable, trustworthy, and optimized by the C# compiler + JIT. Codegen is
behind a **selectable `ICodeGenBackend`** (`--backend roslyn|cil`, default roslyn): a backend-neutral bound tree
feeds either the **Roslyn** backend (idiomatic C# source — the v1 deliverable) or a future **CIL** backend
(typed-native IL via Mono.Cecil — no C#-compile step / no Roslyn dependency, for AOT/direct-IL). See
`COBOLNET_DESIGN.md` §1.1/§18.

## 2. Pipeline

```
source.cob
  → Front-end (REUSED, src/CobolSharp.Compiler, front-end only — CobolNet.Frontend.Frontend):
        Preprocess (reference-format, >>directives, COPY, NIST placeholders) → Lex → Parse → parse tree
  → Bind: typed semantic model — every data item gets a .NET type (no byte offsets)        [task G2]
  → Lower: a C#-oriented model / direct emission                                            [tasks G3–G4]
  → Emit C# (CobolNet.CodeGen.CSharpEmitter → CodeWriter)                                    [all tasks]
  → Compile C# (CobolNet.CodeGen.RoslynBackend → Roslyn CSharpCompilation) → assembly + .g.cs
Runtime: CobolNet.Runtime — CobolNum (numeric), CobolString (character), ManagedPointer, format/file helpers
         (reused from the clean, oracle-verified typed substrates; NO byte engine)
```

**Reuse line (deliberate, audited):** only the ANTLR grammar + lexer/parser/preprocessor (a declarative,
ISO-derived *spec* artifact) and the clean typed runtime substrates are reused. Nothing from the legacy byte
engine — `ProgramState`, `StorageHelpers`, `StorageLayoutComputer`, `PicRuntime`'s byte-window APIs, the byte
`IrLocation`/emit spine — is used. The conformance corpus (`tests/nist/`, `tests/conformance/`) and
`specs/ISO_COBOL.md` are the target + oracle.

## 3. Data model (COBOL → .NET types)

| COBOL | .NET |
|---|---|
| `PIC X(n)` / `A` / `PIC N` (national) | `string` (UTF-16) |
| `PIC 9(n)` / `S9(n)` unsigned/signed **integer** | `long` (PIC-truncated via `CobolNum`) |
| signed/scaled `S9(n)V9(m)`, packed `COMP-3` | native `long` (≤18 digits) / `Int128` (19–38) holding the **unscaled** value; scale is compile-time metadata (NO `decimal`) |
| `COMP-1` / `COMP-2` | `float` / `double` |
| `PIC 1` / `USAGE BIT` | `bool` |
| `01`/group | nested `record struct` |
| fixed `OCCURS n` | `T[]` |
| `OCCURS DEPENDING ON` | `T[]` + a length field |
| `USAGE POINTER` / `BASED` / `ADDRESS OF` | `ManagedPointer` (managed ref; no native heap) |
| `USAGE OBJECT REFERENCE` / OO class | a .NET class reference / class |

Numeric correctness backbone: PIC metadata (digits / scale / sign / usage) is threaded into every `CobolNum`
operation, which applies COBOL truncation, the eight `ROUNDED` modes, and `ON SIZE ERROR` on native
native `long`/`Int128` unscaled values. (`CobolNum`/`CobolString` are oracle-verified against the legacy codec.)

**Deferred unbounded cases** (task G6 — designed to need no persistent byte State): `REDEFINES`/`RENAMES`
storage overlay, whole-group-as-alphanumeric (materialize struct → fresh scratch at the byte-API boundary), and
file-record serialization (typed record ↔ bytes only on the external medium).

## 4. Control flow (COBOL → C#)

The make-or-break design. COBOL control flow (`PERFORM`/`PERFORM THRU`/`VARYING`/`TIMES`/`UNTIL`, `GO TO`,
`ALTER`, paragraph/section fall-through, `GOBACK`/`STOP RUN`, DECLARATIVES) is **ported from the legacy
compiler's proven PC/dispatch design** (DEVLOG 259–260: return-address `PERFORM…THRU`, symbol-based control
transfer, exit-bounded `Dispatch(entry,last)`, DECLARATIVES handling) — a design that is *orthogonal* to the byte
data model. Paragraphs/sections become labeled regions driven by a program-counter loop (C# `goto`/labels handle
arbitrary transfers); `PERFORM` ranges use the exit-bounded dispatch. **Correctness over idiomatic for v1**:
arbitrary COBOL control flow is not always structured C#; "pretty" is a later optimization for well-behaved
subsets. (Task G4.)

## 5. Project layout

- `src/CobolNet/` — the compiler (exe `cobol`). `Frontend/` (parse), `CodeGen/` (`CSharpEmitter`, `CodeWriter`,
  `RoslynBackend`), `Program.cs` (CLI).
- `src/CobolNet.Runtime/` — the runtime the generated C# calls (planned; starts from the clean substrates).
- `src/CobolSharp.*` — legacy (front-end reused; byte engine retired at G8).

## 6. Roadmap (tasks)

- **G1 ✅** Bootstrap + HELLO end-to-end (preprocess→parse→emit C#→Roslyn→run). `DISPLAY` of literals, `STOP RUN`.
- **G2** Data division → typed C# (elementary fields, groups→record struct, tables→arrays; VALUE init).
- **G3** Core verbs (`MOVE`, arithmetic, `IF`/`EVALUATE`, `DISPLAY`/`ACCEPT`, `PERFORM` inline) on typed values.
- **G4** Control-flow engine (port the PC/dispatch design) — `PERFORM THRU`, `GO TO`, `ALTER`, fall-through.
- **G5** Drive the NIST corpus to green (NC → SM/IC/IF → SQ/RL/IX → ST), then the conformance corpus.
- **G6** Deferred data cases: `REDEFINES`/`RENAMES`, whole-group alphanumeric, file serialization.
- **G7** M2/M3/M4 features (OO→.NET classes, UDF, pointers, national/boolean, JSON/XML, intrinsics) per `--standard`.
- **G8** Cut over: retire the byte engine, rename to COBOL.NET / `cobol.exe`, final architecture/doc pass.

## 7. Conventions

- Latest .NET 10 / C# 14 idioms where they aid clarity (records, primary constructors, collection expressions,
  pattern matching, `using` scopes). Full XML doc comments on public surface + inline rationale on non-obvious code.
- The generated C# is written to `<name>.g.cs` next to the assembly — always inspectable.
- Conformance is the oracle: every feature is driven by (and proven against) `tests/nist/valid/*.txt` or a
  `tests/conformance/<ver>/` `.out`.
