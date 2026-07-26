# DESIGN — Full Implementation ↔ Specification Conformance Review (P14 Step-0, spec-first)

> **STATUS: DESIGN — decision-complete methodology + plan.** This is the exhaustive, spec-anchored conformance review
> that is the PHASE-14 definition of DONE (owner decision D13: commercial-quality, 100% CONFORMING to ISO/IEC
> 1989:2023 per §4.2.16 with correct support for 1985/2002/2014). It re-grounds the P14 Step-0 "traceability
> inventory" as **spec-first and exhaustive** — the spec text is the ORACLE, not a differential. Spec-first is the
> only going priority (memory `feedback_spec_is_the_oracle`).

## 0. Why this exists — the lesson that forced it

The historical test strategy is entirely **differential + happy-path**: NIST-85 goldens, the GnuCOBOL compile
accept/reject differential, and our own conformance corpus whose "expected" outputs were largely derived from the
legacy byte-engine. **None of these is a spec oracle**, and a differential is structurally BLIND to a spec-violation
that both implementations share — and many defects were *ported from* the legacy, so the differential and the bug came
from the same place. Two spec-first audits (design-doc `wf_480d50f5`, code `wf_4ce42db6`) found **~44 candidate
conformance bugs** those checks never could. But those audits were **bounded SAMPLING** — 14 behavioral areas, one
agent each, a handful of rules apiece — not exhaustive coverage. 100% conformance (D13) requires an EXHAUSTIVE,
traceable review: every normative rule → its implementation → a verified verdict → a spec-derived test.

## 1. Definition of DONE

Every normative ISO/IEC 1989:2023 rule — and its 1985/2002/2014 applicability — has:
- **(a)** a located implementation (a traceability link into `src/Cobol.Net.*`), OR a recorded **owner
  documented-non-support decision** (optional modules only, per §4.2.16);
- **(b)** a conformance **verdict VERIFIED against the spec TEXT** (not against the legacy or GnuCOBOL);
- **(c)** a **spec-derived test** that covers it — the expected value COMPUTED FROM THE SPEC, never copied from an
  implementation oracle.

The traceability inventory at **zero unresolved GAP** = P14 DONE (D13). This supersedes differential-based confidence.

## 2. The unit of review — the normative rule

The spec expresses requirements as: **Syntax Rules (SR) + General Rules (GR)** per statement (§14.9.x) and clause
(§11/§12/§13.x); **general formats** (the grammar); **§8 concepts** (classes/categories §8.5, conditions §8.8.4,
reference/subscript/ref-mod §8.4, standard conversions §8.5.1, arithmetic/boolean expressions §8.8); **§15 intrinsic**
definitions; **Annex A** required-documented-behavior; **Annex E** edition deltas; **Annex F** obsolete/archaic. Each
rule = `(rule-id, §-anchor, subject, kind, edition-applicability, text)`. The complete rule set is the coverage
**DENOMINATOR** — the thing the audits never enumerated.

## 3. The three phases

### Phase A — build the spec-rule catalog (the denominator)
Extract EVERY normative rule from `specs/ISO_COBOL.md` into a structured, machine-checkable catalog. The spec's SR/GR
are numbered under "Syntax rules" / "General rules" headings per section, so extraction is tractable (parse by
§-section). **Output:** `docs/rearchitecture/spec-rule-catalog.json` — one entry per rule:
`{ id, section, subject, kind: SR|GR|format|concept|intrinsic|doc-behavior, editions, text }`. A **completeness
critic** cross-checks the catalog against the spec's TOC + section numbering, because an OMITTED rule is silent false
confidence. Orchestrated: fan out over the §-clauses; loop-until-dry.

### Phase B — map + verify (the numerator)
For each rule (batched by statement/clause), an agent: **(1)** reads the rule + general format; **(2)** LOCATES the
implementing code (binder/emitter/runtime) — the traceability link; **(3)** VERIFIES conformance across the applicable
editions and adversarial edge cases; **(4)** assigns a **verdict** — `CONFORMS | DIVERGES | PARTIAL | NOT-IMPLEMENTED
| DOCUMENTED-NON-SUPPORT | NEEDS-OWNER-DECISION`; **(5)** records the covering spec-derived test, or flags
`test-needed`. Results accumulate into the **traceability inventory** (§4), incremental and RESUMABLE (a rule's
verdict persists across sessions). Orchestration: fan out by clause; completeness critics surface un-verdicted rules;
candidate verdicts are agent-surfaced ⇒ **re-verified** before they are trusted.

### Phase C — close the gaps
- **DIVERGES** → the **§24 fix-queue** (`PHASE-13-plan-vs-spec-review.md`): spec-first fix + spec-derived golden.
  (V54–V59 + `CODE-SPEC-AUDIT.md` CA1–CA38 are the FIRST INSTALLMENT of this queue.)
- **NOT-IMPLEMENTED** → implement to spec, OR an **owner** documented-non-support decision (optional modules only).
- **CONFORMS-but-untested** → write the spec-derived golden (so coverage is DURABLE, not merely asserted).
- Inventory at **zero GAP** = DONE.

## 4. The traceability inventory (SSOT) — schema

One row per rule, at `docs/rearchitecture/spec-traceability-inventory.*` (generated + maintained):

| field | meaning |
|---|---|
| `rule-id` / `section` / `subject` / `kind` | from the Phase-A catalog |
| `editions` | 85 / 2002 / 2014 / 2023 applicability (introduced / removed / changed) |
| `code-location` | the implementing `file:line` (the traceability link), or `—` if not implemented |
| `verdict` | CONFORMS / DIVERGES / PARTIAL / NOT-IMPLEMENTED / DOCUMENTED-NON-SUPPORT / NEEDS-OWNER-DECISION |
| `test-ref` | the spec-derived golden/xUnit covering it, or `test-needed` |
| `notes` | the §24 item id for a DIVERGES; the owner decision for a non-support |

`scripts/session-probe.ps1` already reports the inventory GAP band ("invent: not built yet") — that GAP count becomes
the **burn-down metric** for P14.

## 5. Execution model

Workflow-orchestrated (owner: max parallelism for disjoint work). **Fan out by §-clause** (disjoint units).
**Incremental accumulation** into the inventory — resumable across sessions; the GAP count is the burn-down. **Loop-
until-dry + completeness critics** so nothing is silently uncovered. Every Phase-C fix lands with its **spec-derived
golden** and a comprehensive gate (tiered testing, `feedback_tiered_gates`). This is many sessions
of work; it is paced by the inventory GAP, not big-banged.

## 6. Governance / relationship to existing artifacts

- **This IS P14 Step-0** (the traceability inventory), re-grounded spec-first + exhaustive — supersedes any
  feature/legacy-checklist framing of Step-0.
- **Divergences → §24** (`PHASE-13-plan-vs-spec-review.md`) — the verified defect/fix-queue SSOT.
- **Editions → the VERSION TEST MATRIX** (`VERSION_CHANGE_REFERENCE.md` + `VERSION_TEST_MATRIX_DESIGN.md`) — the
  (construct × edition) dimension; introduction/continuity/behavior invariants.
- **Seed / first installment:** `DESIGN-SPEC-RECONCILIATION.md` (design-doc audit, 54 conflicts) + `CODE-SPEC-AUDIT.md`
  (code audit, CA1–CA38). The full review is their exhaustive completion.
- **Spec-first discipline:** `feedback_spec_is_the_oracle`, `feedback_spec_is_the_oracle`, `feedback_spec_scopes_not_tests`
  (implement/verify COMPLETELY to the spec — tests verify, never scope).

## 7. Risks + discipline

- **Catalog exhaustiveness** — a missing rule is silent false confidence; the completeness critic + TOC cross-check
  guard it. The denominator's completeness is itself reviewed.
- **Verdicts must be spec-derived**, never differential — that is the entire point; a "matches the legacy" verdict is
  invalid.
- **Candidate verdicts are agent-surfaced** ⇒ VERIFIED before any code change (the verify-then-fix loop already in
  flight for the ~44).
- **Scale** — incremental/resumable inventory; the GAP burn-down is the metric; do NOT attempt it in one pass.
- **Optional-module non-support is an OWNER decision** (D13), recorded in `CONFORMANCE.md`, never an agent's call.
