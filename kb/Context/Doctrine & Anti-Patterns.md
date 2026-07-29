---
title: Doctrine & Anti-Patterns
area: context
status: draft
last_updated: 2026-07-23
related_files:
  - CONSTRAINTS.md
  - PROMPT.md
  - CLAUDE.md
tags:
  - cobolsharp
  - context
---

# Doctrine & Anti-Patterns

The engineering doctrine governing COBOL.NET's development, from [[CONSTRAINTS]] + [[PROMPT]]. Every rule exists
because it was violated and corrected. Complements the process rules in [[kb/Context/Goals]].

## Anti-pattern catalog (must actively search for & correct)

### Architectural / layering
- **God objects** `[GodObject]` — classes mixing parsing, semantic analysis, and codegen → split into focused
  components. (The bind→emit boundary in [[kb/Architecture/Module Overview]] is the concrete guard.)
- **Leaky abstractions / cross-layer reach-through** `[LayerViolation]` — lower layers depending on higher layers →
  enforce `Lexer → Parser → Bound Tree → Codegen → Runtime`.
- **Hidden global state / static mutable** `[GlobalState]` → explicit config objects, immutable data (`BinderContext`/
  `EmitContext`).
- **Ad-hoc feature flags / scattered dialect checks** `[ScatteredFlags]` → centralize in the one
  `VersionConformancePass` (see [[kb/Semantics/Passes]]).

### Code-level
- **Deep nesting / switch pyramids** `[DeepNesting]` → smaller methods, pattern matching, data-driven tables.
- **Copy-paste logic** `[Duplication]` → extract canonical helpers (the source-generated bound visitor kills the
  205-duplicated-arm case).
- **Primitive obsession** `[PrimitiveObsession]` → domain types (PIC descriptors, token kinds, numeric formats).
- **Magic constants** `[MagicValues]`, **null hazards** `[NullHazard]`, **exception-as-control-flow**
  `[ExceptionMisuse]`, **tight I/O coupling** `[IOBinding]`.

### Performance / memory
- **Excessive hot-path allocations** `[HotAlloc]`, **inefficient data structures** `[DataStructureMisfit]`,
  **unbounded caches** `[UnboundedGrowth]`.

## The singular-pattern principle
One mechanism per job = the *best* one. Two coexisting mechanisms is the anti-pattern. Fork only if genuinely better
AND migrate everything to it. Resolve the structural singular-pattern at **design** time (one lexer/parser/evaluator;
leverage ANTLR/Roslyn over hand-rolled). Realized examples: the single `Place` lvalue, the one PC dispatcher, the one
`VersionConformancePass`, the one `ConstructRegistry.Check` funnel.

## Migration phases (CONSTRAINTS.md)
A 9-phase modernization frame: (1) project/build modernization → (2) lexer → (3) parser/grammar → (4) semantic model &
symbol tables → (5) bound tree & semantic normalization (the bound tree is the single semantic model — no separate
lowered IR) → (6) codegen & runtime separation → (7) numeric/PIC/editing → (8) diagnostics/tooling → (9) final
consolidation & cleanup.

## Session rituals
- **Start:** load & summarize state from the plan's §0 + recent DEVLOG; identify phase/TODOs/regressions; confirm scope.
- **End:** summarize changes; report test status; update DEVLOG + the plan §0; propose next steps.

## Key concepts
- Anti-patterns are named with tags (`[GodObject]`, `[LayerViolation]`, …) and must be actively hunted.
- Singular pattern is the master principle; two mechanisms for one job is the smell.
- Strict one-direction layering; immutable context objects instead of global state.
- Session start/end rituals keep the plan §0 and DEVLOG current.

## See also
- [[kb/Context/Goals]] — the process rules.
- [[kb/Architecture/High-Level Design]] · [[kb/Architecture/Module Overview]] — where the layering is enforced.
- [[kb/Spec/Constraints]] — the spec-side constraints.

## Backlinks
- [[kb/Context/MOC]] · [[kb/Index]] — link here.
