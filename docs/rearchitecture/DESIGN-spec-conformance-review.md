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

One row per rule, at **`tests/version-matrix/traceability-inventory.json`**, generated by
`scripts/spec/build_inventory.py` from the Phase-A catalog and adjudicated only through
`scripts/spec/record_verdicts.py` (§8). It sits beside `constructs.json` and the rest of the machine-read test data
rather than under `docs/`, because its reader is the battery, not a person.

| field | meaning |
|---|---|
| `rule-id` / `section` / `subject` / `kind` / `ordinal` / `page` | from the Phase-A catalog — REGENERATED on every rebuild, never hand-edited |
| `editions` | 85 / 2002 / 2014 / 2023 applicability (introduced / removed / changed) |
| `code-location` | the traceability link: `path` or `path#Symbol` (see the correction below), or `""` if not located |
| `verdict` | one of the six defined in `inventory-schema.json` — the vocabulary lives THERE, not here, so it cannot drift between this doc and the tools |
| `test-ref` | the spec-derived test covering it, in a form that RESOLVES on disk (§8), or `""` |
| `notes` | the fix-queue item id for a DIVERGES; the owner decision for a non-support |
| `state` | **derived, never written** — `OK` only when the verdict resolves AND every field that verdict requires is filled; otherwise `GAP` |

`scripts/session-probe.ps1` reports the live GAP count — that is the **burn-down metric** for P14.

> **⛔ DESIGN CORRECTION, superseding the `file:line` this table used to specify.** `code-location` is a **path,
> optionally plus a `#Symbol`** — never a line number. A line number rots on the next edit to the file, so 3,790
> rows of them would hold the battery gate permanently red for reasons having nothing to do with conformance, and
> a gate that cries wolf is a gate nobody reads. A symbol survives refactoring, moves with the code it names, and
> fails only when the thing it points at actually stops existing — which is the one event worth failing on.

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

## 8. HOW A VERDICT IS RECORDED — the Phase-B mechanism

Phase A left a 3,790-row inventory and no way to write a verdict into one: `build_inventory.py` only ever
*preserves* the adjudicated fields across a rebuild, and nothing checked what a row claimed. That gap is not a
missing convenience. **The GAP count is the definition of v1.0** (D13), so a row is a claim about completion, and
an unaudited claim about completion is a self-report. The mechanism below exists to make each of the three claims
in §1 — located, verified, tested — cost what it is supposed to cost.

**Nobody hand-edits the inventory.** A reviewer or a Phase-B agent emits a **batch file** of verdict records;
`scripts/spec/record_verdicts.py` merges it.

```
python scripts/spec/record_verdicts.py --dry-run batch.json     # validate + report the GAP delta, write nothing
python scripts/spec/record_verdicts.py batch.json [more.json]   # merge
dotnet test tests/Cobol.Net.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

A record sets only the five adjudicated fields and is keyed by `rule-id`:

```json
{"rule-id": "AR-15.7-1", "verdict": "CONFORMS",
 "code-location": "src/Cobol.Net.Compiler/Binder/Intrinsics/IntrinsicSignatures.cs#Abs",
 "test-ref": "conformance:2023/intrinsic_abs; unit:IntrinsicArgumentRuleTests.Abs_RejectsAlphanumeric",
 "editions": "85,2002,2014,2023", "notes": ""}
```

**The four parts, and why each is where it is.**

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` | the verdict vocabulary, each verdict's `resolves` flag and required evidence, the legal editions, the `code-location` pattern, the `test-ref` forms |
| the Python loader | `scripts/spec/inventory_schema.py` | parsing that schema, deriving `state`, and the ATOMIC inventory write |
| the writer | `scripts/spec/record_verdicts.py` | validating a batch's SHAPE and merging it all-or-nothing |
| the battery gate | `tests/Cobol.Net.Tests.Unit/SpecTraceabilityInventoryDriftTests.cs` | REFERENTIAL integrity, continuously |

- **The vocabulary is data, in one file.** Three consumers read it — the row builder, the writer and the C# gate —
  and none carries a copy. Adding a verdict is an edit to the JSON and nothing else. This is CLAUDE.md rule 5
  applied where drift is most likely, and the DA wave's lesson (`feedback_one_rule_one_place`) applied before the
  fact rather than after it.
- **Shape at record time, reference at battery time — with NO overlap.** The writer checks that the rule-id is
  real, the verdict is in the vocabulary, the required fields are filled, the editions are legal and each
  reference parses. It does **not** check that a `code-location` symbol exists or that a `test-ref` names a real
  test. That belongs to the gate, because it has to keep holding as the tree changes underneath a row recorded
  sessions ago, and implementing it in both places would be the duplication this whole design is trying to avoid.
- **All-or-nothing, then an atomic replace.** Every record across every named batch validates before any of them
  merges, and the write goes through a temp file. A half-applied batch is the worst outcome available: the rows
  that DID land look exactly like reviewed work, and nothing afterwards can tell them apart.
- **`state` is recomputed, never believed.** Both the Python side and the gate derive it from the verdict and its
  evidence. Closing a GAP by hand-editing a string fails the battery.
- **⛔ ONLY A SPEC-DERIVED TEST CLOSES A ROW — §1(c), enforced rather than trusted.** Each `test-ref` form carries
  `spec-derived`: `conformance` / `unit` / `conformance-test` yes; **`nist` and `characterization` no**, because a
  NIST CCVS golden and a characterization snapshot are regression NETS (CLAUDE.md rule 1) and a differential is
  structurally blind to a violation both implementations share. An xUnit form *can* be spec-derived, so the form
  alone cannot decide it — `disqualifying-method-patterns` additionally strikes any test whose method name says
  otherwise (`*_MatchesLegacy`, `*_MatchesGnuCobol`, `*_MatchesOracle`). A non-qualifying ref is still worth
  recording as corroboration; it simply cannot be what closes the row.
- **`requires` is NOT "what closes the row".** `requires` is the evidence a RECORD needs to be well-formed —
  evidence for the VERDICT. Closing additionally needs the spec-derived test. Keeping them apart is what makes
  **CONFORMS-but-untested** expressible, the Phase-C category §3 names: the rule verified against the code, no
  test yet pinning it, row still a GAP and one golden from done. Folding the test into `requires` would force
  every such row to be misrecorded as PARTIAL — or closed on whatever test happened to touch the function.

> **⚠ BOTH OF THE ABOVE WERE LEARNED FROM THE FIRST BATCH, NOT DESIGNED IN.** It returned 19 CONFORMS rows
> carrying a covering test, and **7 rested on nothing but a NIST golden or a `*_MatchesLegacy` differential** —
> they would have moved the v1.0 burn-down on exactly the evidence this document's §1(c) rules out. The cause was
> upstream of the agents: the fan-out prompt said which forms RESOLVE and never that a cover must be spec-derived,
> and the schema listed `nist:` with nothing marking it differential. Any future fan-out prompt must state the
> spec-derived requirement explicitly. `session-probe` had the same blind spot in the same direction — it counted
> "test-needed" as an EMPTY `test-ref` rather than as a row that did not close, under-reporting 11 as 4.

**What the gate actually enforces** (nine facts, and it is proven able to fail — see below): the inventory covers
the catalog exactly, no row contradicts its catalog entry, every verdict is in the vocabulary, every stored `state`
equals the derived one, every verdict carries its required evidence, every edition name is real, every
`code-location` names a file that exists and a symbol still in it, and every `test-ref` resolves —
`conformance:<edition>/<case>` and `nist:`/`characterization:` to a `.cob` on disk, `unit:<Class>.<Method>` and
`conformance-test:<Class>.<Method>` to a method really declared in that class.

> **⚠ A gate observed only passing is indistinguishable from a gate that inspects nothing**
> (`feedback_green_gates_arent_evidence`), and this one will spend most of its life green over rows nobody has
> touched. Two things answer that. `TheseChecks_ActuallyFail_OnAFabricatedInventory` drives every check above
> against rows built to break it — one defect class at a time, plus positive controls so the failures cannot come
> from a checker that rejects everything — and it runs on every build. And the gate was confirmed against the
> REAL artifact once, by corrupting `traceability-inventory.json` in place: a row hand-promoted to `OK` and a
> CONFORMS row citing a file and a golden that do not exist turned exactly three tests red, then green again on
> restore.

## 9. RUNNING A PHASE-B BATCH — the fan-out, and what its prompt MUST carry

A batch is: pick the next contiguous clause block → generate one input file per subject → fan out one agent per
subject → hand each result to an INDEPENDENT agent told to overturn it → merge → gate.

```
python scripts/spec/phase_b_batch.py 15.32-15.44     # writes scratchpad/phase-b/in-<slug>.json, prints the slugs
#   … run the workflow (one adjudicate agent + one refute agent per slug) …
python scripts/spec/record_verdicts.py --dry-run scratchpad/phase-b/out-*.json
python scripts/spec/record_verdicts.py scratchpad/phase-b/out-*.json
dotnet test tests/Cobol.Net.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

`phase_b_batch.py` excludes rules that already carry a verdict, so a re-run after a partial batch is safe.

### ⛔ The refute stage is not optional, and the evidence is now three batches deep

Every batch's refute stage overturned verdicts, and **every overturn was a DOWNGRADE** — the pass only ever
removes confidence. It has also found defects the adjudicator looked straight at:

- **PB5** (the quantizer saturating at 9.2 × 10⁹) — the adjudicator had sampled only small receivers, where the
  clamp cannot bite. The refuter read the implementation instead of sampling outputs.
- **PB7** (every zero-argument intrinsic unreachable in the keyword-omitted form) — invisible to output-sampling,
  because the program COMPILES and fails at run time.

So: **an all-CONFORMS report is a red flag, not a good result**, and that sentence belongs in the refuter's prompt.

### What every batch prompt must carry — each line paid for by a defect

1. **The spec is the only oracle**, and the legacy / GnuCOBOL / existing goldens are regression nets.
2. **Validate every citation** with `cite.py --check`. Give the concrete precedents: the fabricated
   "§12.3.7 GR7 k3 … distinct ascending" that came out of *our own design doc*, and the one-level-short shape
   (§13.18.60 where the rule is in §13.18.60.4).
3. **Only a SPEC-DERIVED test closes a row.** Name `nist:`, `characterization:` and any `*MatchesLegacy` method
   as non-qualifying, and say that CONFORMS-but-untested is a legitimate, expected outcome. The first batch
   offered seven rows that would otherwise have closed on a differential.
4. **Editions are never derived from `IntrinsicCatalog`'s own row** — that is deriving the answer from the code
   under review, and it produced a wrong window in batch 1.
5. **CONFORMS costs something**: code not located ⇒ NOT-IMPLEMENTED; an edge unverified ⇒ PARTIAL; a rule with
   two halves where one was verified ⇒ PARTIAL, always.
6. **`DOCUMENTED-NON-SUPPORT` is never an agent's to choose** (owner decision, D13).
7. **`code-location` is `path#Symbol`, never a line number.**
8. **The fixes that have already landed**, so agents do not re-report them — and so they know that a function
   ABSENT from `IntrinsicArgumentRules.Verified` is genuinely unscreened, because that table is deliberately
   partial and grows as this review adjudicates each clause.
   ⛔ **THE LIST IS NOT WRITTEN HERE AND MUST NOT BE.** It read "PB1–PB7" while PB8–PB11 and PB13 had all landed,
   then "PB1–PB11 and PB13" while a dozen more had — a hand-maintained roster inside the very doc that warns
   against one, which is what CLAUDE.md rule 8 forbids. **Generate it when writing the batch prompt:**
   `python scripts/spec/work.py stats`, or `status: landed` over `kb/Work/`. A stale roster is worse than none:
   it tells an agent a fixed defect is open and the batch is spent re-reporting it.
   ⚠ Also tell agents that `Verified` now screens by CLASS SETS, not one class per operand — a figurative
   constant is admissible wherever ANY class it can present is admissible (§8.3.3.6.4 GR4; fix-queue PB48) — so
   "the screen accepts ZERO here AND there" is correct behaviour, not a missing rule.
9. **Read-only, and no `dotnet build`/`dotnet test`** — other work is usually in flight.

### ⚠ Reading the results

- **Wait for the workflow's completion notification.** Output files exist from stage one and are REWRITTEN by
  stage two; reading early published a wrong GAP number once, and because every overturn is a downgrade, an early
  read biases the result *upward*.
- **Read the notification's `<failures>` block before its results.** One run reported "completed" with 5 of 12
  refuters dead on API 529, leaving unrefuted stage-one output in their files. `resumeFromRunId` re-runs only the
  failures.
- **Cluster before triaging.** The finding count is not the defect count: batch 1's 42 open rows were three root
  causes for 31 of them; batch 2's 41 were 34 of a single one.
