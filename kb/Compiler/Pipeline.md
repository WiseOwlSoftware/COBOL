---
title: Compiler Pipeline
area: compiler
status: draft
last_updated: 2026-07-23
related_files:
  - docs/COBOLNET_PIPELINE_DESIGN.md
  - docs/rearchitecture/DESIGN-version-conformance-pipeline.md
  - src/Cobol.Net.Frontend/Pipeline/Frontend.cs
  - docs/COBOLNET_DESIGN.md
tags:
  - cobolsharp
  - compiler
---

# Compiler Pipeline

COBOL.NET is a **6-phase compiler** that translates COBOL into idiomatic, typed-native C# compiled in-process by
Roslyn. There is **no byte `ProgramState` substrate** and — the defining architectural decision — **no lowered
basic-block/SSA IR**. The bound semantic tree is the single model; emitters only *render* it
([[docs/COBOLNET_PIPELINE_DESIGN]], decision D1). See [[kb/IR/Node Types]] and
[[kb/Architecture/High-Level Design]].

```
 source.cob
     │
     ▼
┌─────────────┐   reference-format normalize · >>directives (cond. compile) ·
│ PREPROCESS  │   COPY/REPLACE expansion · NIST placeholder fixups
└─────┬───────┘   (Cobol.Net.Frontend/Preprocessor, Frontend.Preprocess)
      ▼
┌─────────────┐   ANTLR lexer (DEFAULT/PICMODE/SUBSCRIPT/COMMENT modes)
│  LEX        │   + ZERO→ZERO_ARITH rewrite + >>COBOL-WORDS retype
└─────┬───────┘
      ▼
┌─────────────┐   SUPERSET grammar — recognizes the UNION of all editions;
│  PARSE      │   two-stage SLL(bail) → LL(recover). NO edition {isXXXX()}? gates
└─────┬───────┘   → CST (CobolParserCore.CompilationUnitContext)
      ▼
┌─────────────┐   resolve symbols/qualifiers/subscripts/refmod; typed & categorized
│  BIND       │   BOUND TREE = ALL semantics. EDITION-AGNOSTIC
└─────┬───────┘   (Cobol.Net.Compiler/Binding → BoundProgram)
      ▼
┌───────────────────────┐  the SOLE edition gate (85/2002/2014/2023).
│ VersionConformancePass │  TWO-ARM walk. strict=reject, permissive=warn
└─────┬─────────────────┘  ── HALT before emit if any diagnostics ──
      ▼
┌─────────────┐   bound→bound normalization (MOVE CORR, PERFORM VARYING AFTER,
│  DESUGAR    │   condition-names). NOT a second tree type.
└─────┬───────┘
      ▼
┌─────────────┐   ICodeGenBackend over the ONE backend-neutral bound tree
│  EMIT       │   --backend roslyn|cil (default roslyn). Emitters RENDER only.
└─────┬───────┘   RoslynBackend: decomposed emitter → idiomatic C# (.g.cs)
      ▼
   Roslyn compile → assembly (+ PDB, net10.0)
```

## Why no lowered IR

A branch-based IR exists in the *legacy* compiler only because its target (CIL) lacks `if`/`while`/`switch`/`try`. C#
has all of them, so lowering to branches and reconstructing structure both wastes work and destroys the readable
output the owner mandates. MOVE/IF/EVALUATE/PERFORM map ~1:1 to C#. "No lowered IR" means no *shared* lowering phase —
the future-additive `CilBackend` (Mono.Cecil) does its *own private* structure→branch lowering inside the backend,
never a phase RoslynBackend consumes. See [[kb/IR/Node Types]].

> Implementation note: in the code today, `Desugar` is not a single standalone pass — the small amount of
> bound→bound normalization happens inside individual binder verbs. The implemented terminal sequence is
> `bind → VersionConformancePass → FlagConformancePass → emit`. See [[kb/Semantics/Passes]].

## Control flow — the one non-1:1 case

Paragraphs/sections become **PC-index cases in one dispatcher** (`int pc; while(...){ switch(pc){...} }`), not separate
C# methods (owner-locked, D2). This makes GO TO / ALTER / PERFORM-THRU expressible and dissolves an identifier-collision
class. See [[kb/IR/Control Flow]].

## Loud-failure invariant

Bind-success ⇒ emit MUST produce compilable C#. Any Roslyn error on generated code is an ICE (surfaced with the
`.g.cs` path), never a silent `// TODO` (D8).

## Key concepts
- 6 phases: Preprocess → Lex → Parse → Bind → VersionConformancePass → (Desugar) → Emit → Roslyn.
- Bound tree = the single model; **no lowered IR**; emitters only render.
- Edition gating is its **own dedicated pass**, post-bind, and HALTS before emit on any error.
- Grammar parses a **superset**; the binder is **edition-agnostic**; one `EditionInfo` carrier threads CLI→driver.
- Control flow = a single per-program **PC dispatcher**.
- Selectable backend behind `ICodeGenBackend` (`roslyn` default, `cil` future).

## See also
- [[kb/Compiler/Phases]] — each phase in detail.
- [[kb/Compiler/Build System]] — how the pipeline is built & regenerated.
- [[kb/Diagrams/Compiler Pipeline Diagram]] — the visual version.
- [[kb/Semantics/Passes]] · [[kb/Runtime/Execution Model]]

## Backlinks
- [[kb/Index]] · [[kb/Compiler/MOC]] — link here.
- [[kb/Architecture/High-Level Design]] — references the pipeline.
