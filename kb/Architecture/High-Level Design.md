---
title: High-Level Design
area: architecture
status: draft
last_updated: 2026-07-23
related_files:
  - docs/COBOLNET_DESIGN.md
  - docs/COBOLNET_ARCHITECTURE.md
  - docs/COBOLNET_REARCHITECTURE_PLAN.md
  - docs/rearchitecture/DESIGN-version-conformance-pipeline.md
  - src/Cobol.Net.Compiler/Binding/Model/Place.cs
tags:
  - cobolsharp
  - architecture
---

# High-Level Design

COBOL.NET is a blank-slate rewrite (`src/Cobol.Net.*`, exe `cobol.exe`) that translates COBOL into **idiomatic,
typed-native C# source compiled by Roslyn**. The North Star (owner decision **D13**): a commercial-quality,
decades-sustainable compiler **100% conforming to ISO/IEC 1989:2023** and to all prior editions (1985/2002/2014). The
design SSOT is [[docs/COBOLNET_DESIGN]]; the live plan is [[docs/COBOLNET_REARCHITECTURE_PLAN]] §0.

## The "best native .NET implementation" mapping
A direct structural correspondence, not a byte emulator:
- A COBOL **record → a .NET `record struct`** (nested for groups).
- An **elementary item → a native .NET field** (`PIC X(n)` → `string`; `PIC 1` → `bool`; `OCCURS n` → `T[]`).
- A **program → a class**; paragraphs/sections are labels, not methods.
- Member access falls straight out: `VAL OF ITEMS(2) OF WS-REC` → `WsRec.Items[2 - 1].Val`.

## The four owner-locked invariants (§1.2)
Design *within* these, never relitigate:
1. **No byte-array storage substrate.** A record IS a `record struct`; there is no `ProgramState`, no `byte[]`-at-offset.
   A byte image exists only *transiently* at unavoidable boundaries (file I/O, CODE-SET, a genuine mixed-USAGE REDEFINES
   pun), built into a scratch buffer, never persisted as program data.
2. **Numerics are native.** Fixed-point → a native integer holding the **unscaled** value (scale is compile-time
   metadata): ≤18 digits → `long`, 19–38 → `Int128`; `COMP-1/2` → `float`/`double`; `COMP-5` → native int by width.
   **No `decimal`, no `BigInteger`.**
3. **Control flow is a single program-counter dispatcher.** Paragraphs/sections are PC cases in one
   `int Dispatch(int startPc, int exitPc)`; `GO TO` sets the PC, fall-through is PC++, `PERFORM` is a recursive bounded
   dispatch. Only STOP RUN / GOBACK use exceptions. See [[kb/IR/Control Flow]].
4. **Output is idiomatic, readable C#** where the construct allows; correctness wins over prettiness for irregular
   control flow.

## Bound-tree-only pipeline (NO shared lowered IR)
Six phases: `Frontend (preprocess→lex→parse)` → `Bind (edition-agnostic, typed BoundProgram preserving COBOL
structure)` → `VersionConformancePass (the ONE edition gate; HALTs before emit)` → `Desugar (bound→bound)` →
`Backend`. The legacy basic-block IR is gone; the backend-neutral bound tree is the single model both backends
consume. See [[kb/Compiler/Pipeline]] and [[kb/IR/Node Types]].

## `ICodeGenBackend` selectable dual backend (§18.23, `--backend roslyn|cil`, default roslyn)
- **RoslynBackend (primary, v1):** bound tree → readable C# source → `CSharpCompilation` → assembly + PDB.
- **CilBackend (future-additive):** bound tree → typed-native CIL via Mono.Cecil, with its OWN *private*
  structure→branch lowering (no Roslyn dependency; AOT/direct-IL). The two can cross-check in the differential harness.

## Two universal abstractions
- **`Place`** — the one typed-lvalue model (item + accessor chain + subscripts + ref-mod + 88 value-set), built once
  by `ReferenceResolver`, consumed identically by MOVE/arithmetic/INSPECT/STRING/files/CALL-by-reference. See
  [[kb/IR/Data Flow]].
- **The PC dispatcher** — the one control-flow model.

## Strict layering, no god classes
`Lexer → Parser → Bound Tree → CodeGen → Runtime`, one direction. Shared state lives in an immutable context object
(`BinderContext`, `EmitContext`), one file per statement-family. **G0–G8 build order:** G0 (project reorg) DONE; G1
bootstrap DONE; G2–G4 done; **G5 NIST corpus COMPLETE; G6 REDEFINES COMPLETE**; **G7 per-edition correctness IN
PROGRESS** (the current CONFORMANCE-FIX-QUEUE / P14 arc); G8 legacy cut-over + rename pending. See
[[kb/Modernization/Tasks]].

## Key concepts
- North Star: best native .NET impl of COBOL — record→record struct, elementary→native field, program→class; full ISO-2023 + 85/2002/2014.
- Four locked invariants: no byte substrate; native numerics (long/Int128 unscaled, no decimal); PC-dispatcher control flow; idiomatic C#.
- Bound-tree-only — no shared lowered IR.
- `ICodeGenBackend`: Roslyn (primary) + CIL (future, private lowering).
- `Place` + PC dispatcher = the spine of consistency; loud-failure invariant (never a silent no-op).
- Strict Lexer→Parser→BoundTree→CodeGen→Runtime layering; context objects, not god classes.

## See also
- [[kb/Architecture/Module Overview]] — the assembly topology realizing this.
- [[kb/Compiler/Pipeline]] · [[kb/IR/Node Types]] · [[kb/Runtime/Execution Model]]
- [[kb/Spec/Version Targeting]] — the four-editions mission.

- **Canonical docs (SSOT, now in-graph):** [[docs/COBOLNET_DESIGN]] (the decision-complete design SSOT) · [[docs/COBOLNET_ARCHITECTURE]] · [[docs/COBOLNET_REARCHITECTURE_PLAN]] (§0 live plan).

## Backlinks
- [[kb/Index]] — "Start here."
- [[kb/Architecture/MOC]] — indexes this note.
- [[kb/Compiler/Pipeline]] · [[kb/IR/Node Types]] — reference it.
