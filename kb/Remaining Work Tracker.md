---
title: Remaining Work Tracker
area: modernization
status: live
last_updated: 2026-07-23
related_files:
  - tests/version-matrix/constructs.json
  - docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md
  - docs/COBOLNET_REARCHITECTURE_PLAN.md
tags:
  - cobolsharp
  - modernization
  - tracker
---

# Remaining Work Tracker

> **This note is the canonical remaining-work tracker — track it here from now on.** It replaces reading the pending
> flags out of `constructs.json` by hand. When an item lands, check its box here (and reconcile the source-of-truth
> docs). Two distinct workstreams, each currently **16 items**. See [[kb/Modernization/Tasks]].

## A. Unimplemented ISO constructs (16 — catalogued, not yet implemented)

Sourced from the `status: "pending"` rows of `tests/version-matrix/constructs.json` (the version matrix *skips* their
compile assertions until the owning phase flips them active). Full context: [[kb/Spec/Lookup/Construct Catalogue]].

### USAGE / numeric representation (6)
- [ ] **`usage-float-binary128-2014`** — USAGE FLOAT-BINARY-128 (IEEE binary128) · §13.18.60.4 GR16 · diag `COBOLNET1564`
- [ ] **`usage-float-decimal16-2014`** — USAGE FLOAT-DECIMAL-16 (IEEE decimal64) · §13.18.60.4 GR17 · `COBOLNET1564`
- [ ] **`usage-float-decimal34-2014`** — USAGE FLOAT-DECIMAL-34 (IEEE decimal128) · §13.18.60.4 GR18 · `COBOLNET1564`
- [ ] **`usage-function-pointer-2014`** — USAGE FUNCTION-POINTER (needs P13 function prototypes) · §13.18.60 · `COBOLNET0900`
- [ ] **`usage-pointer-to-type-2014`** — USAGE POINTER TO type-name (restricted data-pointer) · §13.18.60.2 · `COBOLNET0900`
- [ ] **`pic-external-float-2002`** — external floating-point PICTURE (symbol E) · §13.18.40 · `COBOLNET0900`

### Arithmetic (2)
- [ ] **`arithmetic-standard-binary-2014`** — OPTIONS ARITHMETIC IS STANDARD-BINARY (binary128; obsolete 2023) · §11.9.5/§8.8.1.4 · `COBOLNET0900`
- [ ] **`arithmetic-intermediate-precision-2023`** — implementor-defined arithmetic behavior variant · §8.8 / Annex E.2 · (behavior variant)

### Data (1)
- [ ] **`national-edited-2002`** — national-edited data (PIC N with B 0 /) · §8.5.2.11 · `COBOLNET0900`

### Object orientation (3)
- [ ] **`implements-clause-2002`** — IMPLEMENTS clause (FACTORY/OBJECT) · §11.8 · `COBOLNET0900`
- [ ] **`method-property-selector-2002`** — METHOD-ID GET/SET PROPERTY selector · §11.7 · `COBOLNET0900`
- [ ] **`inline-method-invocation-2023`** — in-line method invocation `identifier(args)` · §8.4.3 · `COBOLNET0900`

### Procedure / EC (2)
- [ ] **`end-accept-2002`** — END-ACCEPT scope terminator · §14.9.1 · `COBOLNET0816`
- [ ] **`use-after-exception-object-2002`** — USE AFTER EXCEPTION OBJECT declarative (EC-OO) · §14.9.49.2 Fmt 4 · `COBOLNET0876`
- [ ] **`perform-exception-checking-2023`** — Format-3 PERFORM … WHEN (exception interceptor) · §14.9.28.2 Fmt 3 · `COBOLNET0900`

### Report Writer (1)
- [ ] **`report-multi-line-2002`** — multiple LINE clause (repeating lines) · §13.18.35 Fmt 1 · `COBOLNET0900`

## B. Conformance-fix-queue remainder (16 — verified bugs to fix)

A **distinct** workstream (both happen to number 16): the verified, spec-derived defect fixes still open in the
`CONFORMANCE-FIX-QUEUE` (30 landed / 16 remain as of 2026-07-23). These are conformance *bugs*, not unimplemented
constructs. The SSOT is [[docs/rearchitecture/CONFORMANCE-FIX-QUEUE]]; the coordinated design is
[[docs/rearchitecture/DESIGN-ec-oo-superbatch]]. Do not enumerate item IDs here (they drift) — track them in the SSOT
and mirror the count:

- [ ] **EC-infra + OO super-batch** — the coordinated fixes sharing the `EcBinder`/`EcEmitter`/`ExceptionState` scaffold (kept serial under one design pass).
- [ ] **Owner-decided remainder** — CA14 (uniform-introduction policy) · V59 (Tier-C byte[] canonical).

See [[kb/Modernization/Audit Artifacts]] and [[kb/Modernization/Tasks]].

## How to use this tracker
1. When a pending construct lands: check its box in **§A**, and in `constructs.json` flip `status` off `pending` (the version-matrix drift test will then enforce it).
2. When a fix-queue item lands: update the `CONFORMANCE-FIX-QUEUE` LANDED header (the live tally) and check the relevant box in **§B**.
3. Keep the counts in this note's intro in sync; update `last_updated`.

## See also
- [[kb/Spec/Lookup/Construct Catalogue]] — all 183 constructs (the 16 here are its `pending` rows).
- [[kb/Modernization/Tasks]] — the phase roadmap these feed.
- [[kb/Modernization/Audit Artifacts]] — the audits behind the fix queue.
- [[kb/Spec/Version Targeting]] — how pending constructs are edition-gated.

## Backlinks
- [[kb/Modernization/MOC]] · [[kb/Index]] — link here.

<!-- BEGIN generated: spec-reconciliation (scripts/spec/tracker_section.py) -->

## C. Spec transcription corrections (210 confirmed — repair the markdown, not the compiler)

**COMPLETE** — all 1261 pages reconciled, every claim adjudicated.

Discrepancies between the canonical ISO PDF and `specs/ISO_COBOL.md`, found by the `spec-reconcile` workflow and each one adversarially verified. **SSOT:** [[docs/rearchitecture/spec-reconciliation/REPORT|REPORT.md]] (per-finding detail plus the verifier's reasoning) and `LEDGER.json`. Do not enumerate individual findings here — they drift; this section is generated by `scripts/spec/tracker_section.py`.

**210 confirmed · 41 normative · 123 structural · 46 cosmetic**

Why this matters more than its size suggests: **every normative finding so far is falsely restrictive** — it makes legal COBOL look illegal.

⛔ **A diagram defect is TWO items, not one.** The markdown is wrong (repair it here), and the grammar may have been written FROM the wrong diagram — in which case the compiler rejects legal COBOL and that is a **§B fix-queue bug**. Proven on GOBACK: the figure lost its choice-indicator bars, `CobolParserCore.g4` encodes `(raisingPhrase | statusPhrase)?` calling them "mutually-exclusive", and `GOBACK RAISING … WITH NORMAL STATUS` is rejected with *no viable alternative at input 'WITH'*. The same inheritance hit ON SIZE ERROR once already (fixed 2026-07-19). Run `.claude/workflows/spec-grammar-impact.js` over the normative pages to classify each as INHERITED (compiler bug) or NOT-INHERITED (doc-only) — with a repro, never a reading.

- [ ] **Directive figure notes — misstated underlining (required vs optional words)** — 21 finding(s) across 20 page(s): p88, p102, p106, p111, p203, p204, p288, p294, p298, p363, p471, p508, p533, p606 … (+6 more)
- [ ] **Lost choice-indicator bars — bracket reads 'at most one' instead of 'zero or more'** — 11 finding(s) across 8 page(s): p373, p607, p632, p635, p687, p722, p776, p799
- [ ] **Lost brackets/braces — an optional clause reads as mandatory** — 6 finding(s) across 5 page(s): p203, p320, p389, p645, p687
- [ ] **Other normative diagram/text defects** — 3 finding(s) across 3 page(s): p28, p232, p490
- [ ] **Structural — misplaced content, headings, split or detached items** — 123 finding(s) across 119 page(s): p18, p28, p34, p37, p52, p57, p64, p140, p144, p146, p154, p155, p164, p183 … (+105 more)
- [ ] **Cosmetic — dropped front matter, formatting** — 46 finding(s) across 45 page(s): p27, p136, p205, p209, p231, p254, p258, p264, p289, p315, p342, p344, p451, p470 … (+31 more)

**Repair discipline:** correct each FAMILY as a whole — the seven ON/OFF directive notes must agree with each other or the inconsistency merely moves. After each family: re-run `python scripts/spec/extract_rule_catalog.py` (the catalog derives from this file) and confirm the page-anchor count is unchanged at 1261, since the anchors are load-bearing for `render-spec-page.py`.

<!-- END generated: spec-reconciliation -->
