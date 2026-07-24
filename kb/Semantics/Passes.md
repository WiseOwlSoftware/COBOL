---
title: Semantics — Passes & Edition Gating
area: semantics
status: draft
last_updated: 2026-07-23
related_files:
  - src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs
  - src/Cobol.Net.Compiler/Validation/FlagConformancePass.cs
  - src/Cobol.Net.Compiler/Binding/BinderDriver.cs
  - src/Cobol.Net.Editions/EditionInfo.cs
  - docs/rearchitecture/DESIGN-version-conformance-pipeline.md
  - docs/rearchitecture/DESIGN-edition-framework.md
tags:
  - cobolsharp
  - semantics
---

# Semantics — Passes & Edition Gating

The pipeline is **`parse → bind → VersionConformancePass → FlagConformancePass → emit`**. Parse recognizes the
**union of all editions** (superset grammar, no `{isYYYY()}?` predicates save two load-bearing forward-detects). Bind
produces the complete `BoundProgram` and is **edition-agnostic**.

> There is **no separate "Desugar"/lowering pass** — the bound tree is the final compilation model (no lowered IR by
> design; see [[kb/IR/Node Types]]). What desugaring exists happens inside individual binder verbs; emit is
> pure codegen over a valid bound tree.

## Bind (edition-agnostic)
`BinderDriver` binds every unit's DATA division before any procedure body (two-phase, for forward function/OO
references), then runs the whole-group manifest tail `BindPipeline.GroupTail()`:
`ProcedureBinding → UsageCollectionPass → StorageFormPass → VersionConformancePass`. Each pass declares
`Requires`/`Produces` watermarks (DAG-validated). A **closed exception ledger** holds the few legitimately bind-time
version facts (the one UDF-invocation `Check`; catalog-driven per-name windows; digit-capacity caps; two behavioral
edition reads; the owner-disposition SYNC-on-group site).

## VersionConformancePass — the SOLE edition gate
Runs as the manifest's named terminal pass (`Requires StorageComputed`, `Produces EditionConformanceChecked`), so the
Bind result already carries every edition diagnostic. **Two arms:**
- **Parse-tree arm** (`ParseArm`) fires every *syntactic* introduction/removal/phrase/literal gate + the §8.9
  reserved-word funnel on the construct's **recognition** — because a below-edition construct that *also* fails to bind
  keeps its parse node but drops its bound node (a bound-arm intro gate would silently drop the `0900`).
- **Bound-tree arm** (`GateStatement`/`GateMove`/`GateData`) fires only genuinely *semantic* gates whose identity is a
  resolved bound fact: MOVE figurative-category, and gates conditioned on file-organization / access-mode / USAGE /
  pointer-category.

All ~88 gate sites route through the ONE `ConstructRegistry.Check(edition, sink, id, where)`. The two arms are
disjoint. The driver's single `HasErrors` gate then **halts before emit**, so codegen never runs on an errored tree.
See [[kb/Compiler/Pipeline]].

## FlagConformancePass — sibling migration-flagging
Runs right after — an orthogonal axis: directive-state-driven (`>>FLAG-02` §7.3.14 / `>>FLAG-14` §7.3.15), always a
**Warning** (`COBOLNET1620`/`1621`), fires regardless of `--std`. A parse-tree visitor (flags are line-sensitive); a
no-op when no FLAG directive is present.

## The edition framework
`EditionInfo(Year, Permissive)` is the immutable single source of the dialect year (`Has(introducedIn)`, `MaxDigits`
= 18 pre-2002 / 31 after); four valid editions **85/2002/2014/2023**, default **2023**. `--std` selects the year;
`--permissive` downgrades removals to warnings via `EditionSeverityPolicy`. The **superset-parse +
construct-identity-recovered-by-the-pass** approach keeps version *numbers* single-sourced in
`constructs.json`/`ConstructRegistry` while version *identity* is recovered from the parse/bound node — no drift. See
[[kb/Spec/Version Targeting]].

## Why one pass, not scattered checks
Edition conformance is one concern with one owner and error-gated phase boundaries — ruling out the four decayed
patterns (reverse-signature recognizer, per-rule grammar predicates, binder-embedded checks, emit-time gating). See
[[kb/Context/Doctrine & Anti-Patterns]].

## Key concepts
- Order: parse (superset) → bind (edition-agnostic) → VersionConformancePass (sole gate, HALT-on-error) → FlagConformancePass (sibling) → emit.
- No lowered IR / no standalone desugar pass.
- Two-arm gating: parse-arm on recognition (drop-proof), bound-arm on resolved semantic facts.
- One `ConstructRegistry.Check` funnel; binder holds zero statement-level Checks.
- `EditionInfo` = single dialect-year source; `EditionSeverityPolicy` = single strict/permissive seam.
- Manifest watermarks (`Requires`/`Produces`) DAG-validate pass order.

## See also
- [[kb/Semantics/Validation Rules]] — the checks these passes run.
- [[kb/Compiler/Phases]] — the version-conformance mechanism in the frontend context.
- [[kb/Spec/Version Targeting]] · [[kb/IR/Node Types]]

## Backlinks
- [[kb/Semantics/MOC]] · [[kb/Index]] — link here.
- [[kb/Compiler/Pipeline]] · [[kb/Spec/Version Targeting]] — reference it.
- Lookup: [[kb/Spec/Lookup/Semantic Rules]] · [[kb/Spec/Lookup/Construct Catalogue]] (the gated constructs).
