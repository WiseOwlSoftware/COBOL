---
title: Lookup — Constraints
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - CONSTRAINTS.md
  - specs/ISO_COBOL.md
  - docs/CONFORMANCE.md
  - src/Cobol.Net.Editions/EditionInfo.cs
tags:
  - cobolsharp
  - spec
  - lookup
---

# Lookup — Constraints

Two constraint families: **language/spec constraints** (what a conforming COBOL program may do, and how the compiler
enforces limits) and **engineering constraints** (the development doctrine from [[CONSTRAINTS]]). See
[[kb/Spec/Constraints]] and [[kb/Context/Doctrine & Anti-Patterns]].

## Language / spec constraints
| Constraint | Description | Spec Source | Compiler Enforcement | Semantic Rule | Related Notes |
|---|---|---|---|---|---|
| Edition availability | a construct exists only in editions ≥ its `introducedIn` | Annex E / editions | `VersionConformancePass` (0900) | intro gating | [[kb/Semantics/Passes]] |
| Removed constructs | a removed construct rejects (strict) / warns (permissive) | Annex F / VCR | `VersionConformancePass` (0902) | removal gating | [[kb/Spec/Version Targeting]] |
| Reserved words | a reserved word cannot be a user-defined word | §8.9 | §8.9 reserved-word funnel (0901) | position-aware check | [[kb/Semantics/Validation Rules]] |
| Digit capacity | max 18 digits pre-2002, 31 after | §8.8 / editions | `EditionContext.CheckDigitCapacity` (0801/0802) | `MaxDigits` | [[kb/Semantics/Validation Rules]] |
| MOVE category compatibility | operand classes must be move-compatible | §14.9.25.3 SR1 | `MoveBinder` (0809) | category matrix | [[kb/Semantics/Validation Rules]] |
| Reference format | fixed-form column areas / free-form rules | §6 | preprocessor normalize | indicator gates | [[kb/Compiler/Phases]] |
| External-file consistency | matching SELECTs share external status/key items | §12.4 / §9.1 | binder + pass (1573/1575/1624) | cross-connector | [[kb/Semantics/Validation Rules]] |
| REDEFINES legality | no VALUE on a redefiner; SYNC/type puns limited | §13.16 REDEFINES | data binder (4-tier reject-D) | overlay rules | [[kb/IR/Data Flow]] |
| Documented non-support | MCS / COMMIT-ROLLBACK / VALIDATE / SCREEN / LOCALE | §4 / Annex A.4 | recognize+warn (1560 band) / reject (1518) | §4.2.6 | [[kb/Spec/Overview]] |
| Loud-failure | a recognized-but-unimplemented case fails loudly, never silently | project rule (D8) | `BoundUnsupported` / ICE | no silent no-op | [[kb/Architecture/High-Level Design]] |

## Engineering constraints (anti-patterns)
| Constraint | Description | Spec Source | Compiler Enforcement | Semantic Rule | Related Notes |
|---|---|---|---|---|---|
| No god objects `[GodObject]` | one responsibility per class | CONSTRAINTS.md | bind→emit boundary; one file per family | — | [[kb/Architecture/Module Overview]] |
| Strict layering `[LayerViolation]` | Lexer→Parser→BoundTree→Codegen→Runtime, one direction | CONSTRAINTS.md | no assembly cycle; Runtime is a leaf | — | [[kb/Architecture/Module Overview]] |
| No hidden global state `[GlobalState]` | immutable context objects | CONSTRAINTS.md | `BinderContext`/`EmitContext` | — | [[kb/Context/Doctrine & Anti-Patterns]] |
| No scattered dialect flags `[ScatteredFlags]` | centralize edition gating | CONSTRAINTS.md | one `VersionConformancePass` | — | [[kb/Semantics/Passes]] |
| No duplication `[Duplication]` | canonical helpers; source-gen visitor | CONSTRAINTS.md | `BoundVisitorGenerator` (compile-error on gap) | — | [[kb/IR/Node Types]] |
| Singular pattern | one best mechanism per job | CONSTRAINTS.md / PROMPT.md | `Place`, PC dispatcher, one gate | — | [[kb/Context/Doctrine & Anti-Patterns]] |
| No transitional hacks / workarounds | fix the root cause; propagate type changes fully | PROMPT.md | design-time singular pattern | — | [[kb/Context/Goals]] |
| No byte substrate | typed-native only; bytes transient at boundaries | owner directive | no `ProgramState` in `Cobol.Net.*` | — | [[kb/Architecture/High-Level Design]] |

## See also
- [[kb/Spec/Constraints]] — the prose version (spec-side).
- [[kb/Context/Doctrine & Anti-Patterns]] — the full anti-pattern catalog.
- [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Version Targeting]]

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Spec/Constraints]] · [[kb/Context/Doctrine & Anti-Patterns]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Preprocess & Validate phases.
