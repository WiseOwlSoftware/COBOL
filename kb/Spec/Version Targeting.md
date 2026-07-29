---
title: Version Targeting & Conformance
area: spec
status: draft
last_updated: 2026-07-23
related_files:
  - docs/VERSION_CHANGE_REFERENCE.md
  - docs/VERSION_TEST_MATRIX_DESIGN.md
  - tests/version-matrix/constructs.json
  - tests/version-matrix/reserved-words.json
  - docs/GnuCOBOL extensions.md
  - src/Cobol.Net.Editions/ConstructRegistry.cs
tags:
  - cobolsharp
  - spec
---

# Version Targeting & Conformance

The compiler is architected as **"four compilers in one."** The owner premise: *"Conceptually we have a different
COBOL compiler for each ISO edition (1985/2002/2014/2023); we should test it as such."* `cobol --std V prog.cob`
**is** the "COBOL-V compiler," and for every construct × every edition V it must produce the edition-correct outcome —
accept with the V behavior, or reject with an edition-appropriate diagnostic.

## Edition gating

A single `VersionConformancePass` (the sole edition gate) enforces this with two arms — a **parse-tree arm**
(syntactic introduction/removal/phrase gates + the §8.9 per-edition reserved-word funnel) and a **bound-tree arm**
(semantic statement-level gates). `DialectLevel` threads bind-side only, so the future CIL backend inherits gating
for free. See [[kb/Semantics/Passes]]. Diagnostics band **COBOLNET0900–0903**:

- `0900` construct requires COBOL-YYYY (introduction gating)
- `0901` word reserved in COBOL-YYYY used as a user word (§8.9)
- `0902` construct removed in COBOL-YYYY (error strict / warning permissive)
- `0903` obsolete/archaic-element flag (warning)

## The change checklist (VCR)

[[docs/VERSION_CHANGE_REFERENCE]] is the edition-gating ledger — ~130 rows cataloguing every documented
edition-to-edition change: Annex E.2 (2014→2023 substantive), E.3.2/E.3.3 (new features/words), Annex F
(archaic/obsolete), and a growing Table 7 of 85→2002 deletions (LABEL RECORDS, VALUE OF, DATA RECORDS, ALTER, OPEN
REVERSED, ENTER, RERUN, …). Each row carries a machine-readable anchor (`<!-- gate:ID -->`, `<!-- pin-to-spec -->`,
`<!-- ref-only -->`, `<!-- todo -->`); a generated status index + `VcrDriftTests` prevent staleness.

> **Scope caveat.** The 2023 spec documents the 2014→2023 delta completely but only *partially* the 85→2002 and
> 2002→2014 deltas — those must be derived from the older standards ("confirm against the older standard before gating").

## The test matrix

[[docs/VERSION_TEST_MATRIX_DESIGN]] designs the **(construct × edition)** matrix, sourced from
`tests/version-matrix/constructs.json`. The expected-outcome function `f(case,V)` is *computed*, never hand-ticked:
`V < introducedIn` → REJECT; `removedIn ≤ V` → REJECT/removed; else COMPILE (emit `behaviorVariants[V]` if any). Three
correctness **invariants**:

- **INV-1 — Continuity**: every COBOL-85 program compiles at V ≥ 85 *unless* it hits a removed feature or a
  newly-reserved-word collision. The NIST sweep found **342 programs compile at 85 AND 2002/2014/2023 — zero breaks**.
- **INV-2 — Introduction-gating**: a construct introduced in E is rejected below E with the edition diagnostic.
- **INV-3 — Behavior-correctness**: behavior-variant constructs emit `behaviorVariants[V]` (no confirmed variant yet;
  the investigated de-sign/DISPLAY differences are version-*invariant* pin-to-spec).

## `--permissive`

Orthogonal to `--std`: it downgrades removed-construct rejections (`0902`) from errors to warnings for migration.
Every named `--std` is strict by default.

## GnuCOBOL extensions register

[[docs/GnuCOBOL extensions]] catalogs non-ISO constructs the compiler deliberately does **not** support. It sorts
findings into **four buckets** (vendor extension / ISO-optional-not-claimed / ISO-mandatory-wrongly-refused-*our bug* /
NEEDS-VERIFICATION). Rows marked **NEEDS VERIFICATION** are *not* adjudicated against ISO and may be our bugs — never
cite the doc as authority that something is non-ISO until its row says CONFIRMED.

## Key concepts
- "Four compilers in one"; `--std`, default 2023; strict vs `--permissive`.
- Single two-arm `VersionConformancePass`; COBOLNET0900–0903.
- VCR ledger (~130 rows) with drift-guarded gate anchors.
- Computed `f(case,V)`; INV-1 continuity / INV-2 introduction / INV-3 behavior.
- GnuCOBOL register: four buckets; NEEDS-VERIFICATION ≠ authority.

## See also
- [[kb/Semantics/Passes]] — the VersionConformancePass mechanism.
- [[kb/Semantics/Version Gating & Reserved Words]] — the design record: superset+pass+table vs. predicates / four grammars / lexer toggling.
- [[kb/Semantics/Validation Rules]] — the reserved-word funnel & severity policy.
- [[kb/Spec/Language Features]] — the feature surface being gated.
- [[kb/Modernization/Tasks]] — the P14 traceability inventory.

## Backlinks
- [[kb/Spec/MOC]] — indexes this note.
- [[kb/Index]] — lists this as a major note.
- Lookup: [[kb/Spec/Lookup/Construct Catalogue]] (183 constructs × edition) · [[kb/Spec/Lookup/Semantic Rules]].
- Pipeline: [[kb/Compiler/Pipeline-to-ISO-Mapping]] — the Validate (edition-gating) phase maps here.
