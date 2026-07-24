---
title: Lookup — Semantic Rules
area: spec-lookup
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs
  - src/Cobol.Net.Compiler/Binding/Procedure/Verbs/MoveBinder.cs
  - src/Cobol.Net.Editions/ConstructDialectStatus.cs
  - docs/COBOLNET_VALIDATION_DESIGN.md
tags:
  - cobolsharp
  - spec
  - lookup
  - semantics
---

# Lookup — Semantic Rules

The semantic rules the compiler enforces, mapped to the ISO source, the pass/binder that enforces them, the IR node
they attach to, and the runtime consequence. See [[kb/Semantics/Validation Rules]] and
[[kb/Semantics/Passes]]. Validation splits into **edition-invariant** (binder) vs **edition-conditional**
(`VersionConformancePass`) rules.

## Data & category rules
| Rule | Description | Spec Source | Validation Pass | IR Node | Runtime Impact |
|---|---|---|---|---|---|
| MOVE class legality | index/pointer/object/message-tag operands rejected | §14.9.25.3 SR1 | `MoveBinder` (0809, invariant) | `BoundMove` | — (compile error) |
| MOVE figurative category | figurative→numeric obsolete/removed by edition | §14.9.25.3 SR5 | `VersionConformancePass.GateMove` | `BoundMove` | category convert |
| Category compatibility | receiver category constrains sender | §14.9.25 | `MoveBinder` | `BoundMove` | truncation/edit |
| Digit-capacity | ≤18 digits pre-2002, ≤31 after | §8.8 / editions | `EditionContext.CheckDigitCapacity` (0801/0802) | numeric `PicInfo` | overflow behavior |
| REDEFINES overlay legality | one canonical backing; type/SYNC puns limited | §13.16 REDEFINES | data binder (4-tier) | `RedefViewPlace` | view accessor |
| VALUE on redefiner | a redefining view emits no stored VALUE (SR9) | §13.16 | data binder | init nodes | init from original |
| Whole-group-as-alphanumeric | group operand fills without regard to leaves (GR4) | §14.9.25.3 GR4 | `StorageFormPass` | `StorageForm.CharImage` | image materialize |

## Control-flow rules
| Rule | Description | Spec Source | Validation Pass | IR Node | Runtime Impact |
|---|---|---|---|---|---|
| GO TO DEPENDING range | out-of-range selector → fall-through | §14.9.17 GR2 | binder | `BoundGoToDepending` | inner switch default |
| PERFORM range bounding | THRU range is exit-bounded, return-address | §14.9.28 | binder | `BoundOutOfLinePerform` | recursive Dispatch |
| VARYING AFTER re-init | inner indices re-init each outer increment | §14.9.28 | binder | `BoundPerformControl` | loop nesting |
| EXIT format gating | EXIT PARAGRAPH/SECTION/PERFORM introduced 2002 | §14.9.14 GR5-7 | `VersionConformancePass` (0900) | `BoundExit*` | pc/return |
| ALTER removal | ALTER removed in 2002 | §14.9.3 / Annex F | `VersionConformancePass` (0902) | `BoundAlter` | alter field (85) |
| Duplicate paragraph names | resolved by symbol identity, not text | §14.4 | binder | `BoundParagraph` | correct target |

## Conditions & exception rules
| Rule | Description | Spec Source | Validation Pass | IR Node | Runtime Impact |
|---|---|---|---|---|---|
| Condition precedence | NOT > AND > XOR > OR, fully parenthesized | §8.8.4.2 | binder | `BoundLogical`/`BoundNot` | short-circuit eval |
| Abbreviated combined | subject/relation carried across operands | §8.8.4.2 | binder | `BoundRelational` | expansion |
| Level-88 value-set | VALUE/THRU defines the truth set | §8.8.4.1.4 | binder | `BoundCondition88` | bool property |
| Class condition | NUMERIC/ALPHABETIC/CLASS test | §8.8.4.1.3 | binder | `BoundClassCondition` | class test |
| EC checking scope | ECs off by default; `>>TURN` folds at compile | §14.6.12 / §7.3.13 | `EcFeatures` fold | `BoundEcChecked` | EC engine |
| EC hierarchy & fatality | Table 13 exception hierarchy | §14.6.12 Table 13 | binder | `BoundRaise` | fatal/nonfatal |

## File & I-O rules
| Rule | Description | Spec Source | Validation Pass | IR Node | Runtime Impact |
|---|---|---|---|---|---|
| FILE STATUS semantics | two-char status set after each verb | §9.1.13 | binder + runtime | file verb nodes | status codes |
| External-file consistency | matching SELECTs share external status/key items | §12.4 / §9.1 | binder + pass (1573/1575/1624) | file model | external binding |
| Open-mode legality | verb legality depends on OPEN mode | §14.9 I-O | binder + connector | `BoundOpen`,… | state machine |
| Key ascending (WRITE/START) | indexed prime/alt key ordering | §14.9 | binder + connector | keyed I/O nodes | 21/22 status |

## Edition-gating rules (the sole gate)
| Rule | Description | Spec Source | Validation Pass | IR Node | Runtime Impact |
|---|---|---|---|---|---|
| Introduction gate (0900) | construct requires COBOL-YYYY | Annex E / `constructs.json` | `VersionConformancePass` (ParseArm) | any node | — |
| Removal gate (0902) | construct removed in COBOL-YYYY | Annex F / VCR | `VersionConformancePass` | any node | strict reject / permissive warn |
| Reserved-word gate (0901) | word reserved in target edition used as user word | §8.9 | §8.9 funnel (ParseArm) | `cobolWord` | — |
| Obsolete flag (0903) | archaic/obsolete element warning | Annex F | `VersionConformancePass` | any node | warning |
| Severity policy | strict=error / permissive=warn for removals | project / editions | `EditionSeverityPolicy` | — | — |
| Migration flags (1620/1621) | `>>FLAG-02`/`>>FLAG-14` warnings | §7.3.14/.15 | `FlagConformancePass` | flag visitor | — |

## See also
- [[kb/Semantics/Validation Rules]] · [[kb/Semantics/Passes]]
- [[kb/Spec/Lookup/IR Mapping]] · [[kb/Spec/Lookup/Runtime Mapping]]
- [[kb/Diagrams/Semantic Validation Flow]] · [[kb/Diagrams/Semantic Rule Flow]]


## Flow Diagrams
- [[kb/Diagrams/IR-to-Semantic-to-Runtime-Flow]] — each rule shown in context: IR node → semantic rule → runtime behavior.

## Backlinks
- [[kb/Spec/Lookup/Index]] · [[kb/Semantics/Validation Rules]] — link here.
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Bind & Validate phases.
