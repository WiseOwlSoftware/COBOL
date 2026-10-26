---
title: Module Overview (Assembly Topology)
area: architecture
status: draft
last_updated: 2026-07-23
related_files:
  - docs/rearchitecture/DESIGN-module-topology.md
  - docs/PROJECT_ORG_DESIGN.md
  - docs/rearchitecture/CRITIQUE-encapsulation.md
  - src/WiseOwl.Cobol.Compiler.SourceGen/BoundVisitorGenerator.cs
  - src/WiseOwl.Cobol.Cli/Program.cs
tags:
  - wiseowl-cobol
  - architecture
---

# Module Overview (Assembly Topology)

The solution is a **five-assembly greenfield tree** (`src/WiseOwl.Cobol.*`) plus a **source-generator project** (the legacy
engine is deleted from `main`: `docs/rearchitecture/LEGACY-ARCHIVE.md`). The one structural addition over the original
four-project split is the leaf **`WiseOwl.Cobol.Editions`**, so both Frontend and Compiler can share one
construct/reserved-word catalogue without a cycle.

## Dependency graph (from the live `.csproj` files)
```
                    WiseOwl.Cobol.Editions   (LEAF — no refs)
                       ▲            ▲
                       │            │
   WiseOwl.Cobol.Frontend ─┘            └── WiseOwl.Cobol.Compiler ──► WiseOwl.Cobol.Runtime  (LEAF)
        (→ Editions,                          │  ▲
         Antlr4.Runtime)                      │  │ (analyzer-only, netstandard2.0,
                       ▲                      │  └─ ReferenceOutputAssembly=false)
                       └──────────────────────┤
                                              ├──► WiseOwl.Cobol.Frontend
   WiseOwl.Cobol.Compiler.SourceGen ──────────────┘    WiseOwl.Cobol.Compiler.SourceGen
   (source generator)                              (+ Microsoft.CodeAnalysis.CSharp)

   WiseOwl.Cobol.Cli (exe → cobol.exe) ──► WiseOwl.Cobol.Compiler ──► (System.CommandLine)
```

## Per-project responsibility
- **`WiseOwl.Cobol.Editions`** *(leaf)* — the construct/reserved-word catalogue, edition-severity policy, the immutable
  `EditionInfo` value, and the diagnostic-code registry. Pure data + policy; referenced by both Frontend and Compiler.
- **`WiseOwl.Cobol.Frontend`** (→ Editions, `Antlr4.Runtime.Standard`) — preprocessor, ANTLR lexer/parser, parse tree,
  frontend diagnostics. Subfolders: `Grammar/`, `Generated/`, `Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`,
  `Pipeline/`, `Cst/`, `Expressions/`. See [[kb/Compiler/Phases]].
- **`WiseOwl.Cobol.Runtime`** *(leaf)* — the typed-native runtime kernel the *generated* programs bind against. Subfolders:
  `Values/` (CobolNum, NumProfile), `Verbs/`, `Control/`, `Exceptions/` (EC model), `IO/`, `Intrinsics/`. No compiler
  back-reference. See [[kb/Runtime/Execution Model]].
- **`WiseOwl.Cobol.Compiler`** (→ Editions, Frontend, Runtime; + SourceGen as analyzer) — binder + bind pipeline + CodeGen +
  Roslyn backend + `CompilerDriver`. Subsystems: `Binding/` (`Model/`, `Bound/`, `Passes/`, `Procedure/`,
  `Validation/`), `CodeGen/` (`Emit/`, `DataDivision/`, `Verbs/`, `Roslyn/`), `Oo/`, top-level `Validation/`.
- **`WiseOwl.Cobol.Compiler.SourceGen`** — a Roslyn source generator (`BoundVisitorGenerator.cs`) emitting the exhaustive
  `IBoundVisitor<T>` dispatch so a missing bound-node arm is a **compile error**. Wired analyzer-only
  (`OutputItemType=Analyzer`, `ReferenceOutputAssembly=false`; netstandard2.0). See [[kb/IR/Node Types]].
- **`WiseOwl.Cobol.Cli`** (→ Compiler; `AssemblyName=cobol`) — thin `System.CommandLine` shell (`Program.cs` +
  `CliOptions.cs`); parses `--std`/`--backend`, orchestrates files.
- **Legacy `Legacy.{Compiler,Runtime,CLI}`** — the byte-engine, kept **only** as a differential oracle (opt-in
  `WISEOWL_COBOL_LEGACY_DIFFERENTIAL=1`), deleted at G8. See [[kb/Modernization/Tasks]].

## Layering / dependency-direction rules
Dependencies flow one way — Editions ← Frontend/Runtime ← Compiler ← Cli — with **no assembly cycle and no
Runtime→Compiler back-reference**. The **bind→emit boundary** is the hard rule: the binder produces the
backend-neutral bound tree and resolves *all* semantics; CodeGen is a pure renderer that must never mutate the
binder's model or re-discover semantics. Shared state travels in `BinderContext`/`EmitContext`, never a god class.
(The historic `StoreAsImage`/`IndexFields` write-backs are the defects the rearchitecture closes.) See
[[kb/Context/Doctrine & Anti-Patterns]].

## Key concepts
- 5 greenfield assemblies + 1 source-gen + 3 legacy (oracle-only).
- `WiseOwl.Cobol.Editions` = a new leaf so Frontend + Compiler share ONE catalogue (no cycle).
- Runtime is a dependency-free leaf; Compiler references Frontend/Editions/Runtime + SourceGen (analyzer).
- Folder = sub-namespace; one file per public type.
- Source-generated exhaustive bound-visitor: a missing dispatch arm is a compile error.
- Bind→Emit boundary: binder owns semantics, emitter renders only.

## See also
- [[kb/Architecture/High-Level Design]] · [[kb/Compiler/Build System]]
- [[kb/IR/Node Types]] · [[kb/Runtime/Execution Model]]
- [[kb/Diagrams/IR Graph Overview]]

## Backlinks
- [[kb/Architecture/MOC]] · [[kb/Index]] — link here.

> Doc-vs-tree note: the live tree places the data model under `Binding/Model/`; `DESIGN-module-topology.md` proposes
> promoting it to a top-level `Model/`. The assembly-level graph above is taken from the live `.csproj` files and is
> authoritative; some finer folder targets in the topology design are still in-progress rearchitecture waves.
