# DESIGN — Full Implementation ↔ Specification Conformance Review (P14 Step-0, spec-first)

> **STATUS: DESIGN — decision-complete methodology + plan.** This is the exhaustive, spec-anchored conformance review
> that is the PHASE-14 definition of DONE (owner decision D13: commercial-quality, 100% CONFORMING to ISO/IEC
> 1989:2023 per §2023:4.2.1 with correct support for 1985/2002/2014). It re-grounds the P14 Step-0 "traceability
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
- **(a)** a located implementation (a traceability link into `src/WiseOwl.Cobol.*`), OR a recorded **owner
  documented-non-support decision** (optional modules only, per §2023:4.2.16);
- **(b)** a conformance **verdict VERIFIED against the spec TEXT** (not against the legacy or GnuCOBOL);
- **(c)** a **spec-derived test** that covers it — the expected value COMPUTED FROM THE SPEC, never copied from an
  implementation oracle; **OR, only for a rule that carries no observable obligation, the owner-signed
  DERIVATION of §1.1 below.**

The traceability inventory at **zero unresolved GAP** = P14 DONE (D13). This supersedes differential-based confidence.

### 1.1 The DERIVATION — the (c) alternative for a rule with no observable obligation (owner, `kb/Work/PB386`, 2026-09-03)

⛔ **This is the ONE widening of §1 an agent did not make, and the bounds are the owner's, not a paraphrase.**
Some normative rules impose no obligation a conforming program can observe: the standard declares the result
*undefined* (§2023:4.4 2): *"Situations in which the results of executing a statement are explicitly undefined or
unpredictable are identified in A.2, Undefined language element list. A COBOL run unit that allows these
situations to happen is a conforming run unit"* — `cite.py --check 4.4` OK); or the rule's antecedent cannot be
populated in this implementation; or its consequent cannot be told apart from another rule's. For such a rule
§1(c) is not merely unmet, it is **unmeetable**: any golden written for it would pin an IMPLEMENTATION CHOICE as
though it were the standard's answer, which is exactly what the schema's `spec-derived` clause exists to prevent.
The measured population before the door opened was **eight rows** — every CONFORMS-but-untested row on the tree
except `DOC-A.1-93`, which is a real defect (`kb/Work/PB383`).

A **derivation** is a determination recorded in `docs/CONFORMANCE.md` §8, keyed by the inventory `rule-id`,
naming ONE of four arms and signed by the owner. It stands in place of §1(c) **and of nothing else**: (a) and
(b) are paid in full, and a `kind: DOC` row still pays its register anchor and its observability cost.

| arm | what it claims | how it is CHECKED |
|---|---|---|
| `undefined-A.2` | the standard itself lists this rule as explicitly undefined | **MECHANICAL.** The `Names` cell reads `A.2 item <n>`; the checker resolves Annex A.2 item *n*'s own trailing citation (`(14.9.5, CANCEL statement, General rule 11)`) against the rule catalog and requires **this row's `rule-id` to be among the ids it resolves to**. Naming an item that does not cover the row is refused. |
| `unpopulatable-antecedent` | the antecedent is false for every state this implementation can reach | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must STATE the closed set that makes it unreachable (≥20 characters — a dash or an `n/a` is refused). The argument itself has a reader, and the owner's signature is that reader. |
| `indistinguishable-consequent` | the consequent is byte-identical to another rule's, so no conforming program can distinguish them | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must be a `rule-id` that EXISTS in the catalog and is not this row's own. |
| `definitional` | the rule DEFINES a property of the user's program (§2023:4.4's conforming run unit) and obliges the implementation to nothing — owner decision `kb/Work/R43` item 4, extending `PB386` | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must open `defines` and state the property and whose it is (≥20 characters after the word). A rule whose subject is the implementation or the implementor is never this arm. |

**What is REFUSED, by the writer and by the gate.** Each of these is a bound the owner set, encoded rather than
described:

1. a derivation on a row that already carries a **spec-derived `test-ref`** — the row demonstrably HAS an
   observable obligation, so the premise of the whole mechanism is false for it;
2. a derivation on a row whose **verdict does not resolve** — a derivation explains why no test can exist, not
   why a DIVERGES is acceptable;
3. a derivation on a row of a kind that **obliges a register determination which `docs/CONFORMANCE.md` does not
   carry** — the documentation obligation is not yet stated, so there is nothing for the derivation to be about;
4. an `undefined-A.2` arm naming an item that **does not resolve to this row**;
5. a **missing or unknown owner signature**: the `Signed` cell must be a key of the schema's `signatures` map, one
   key per dated owner decision that signed rows (`owner: 2026-09-03`, PB386 and the rows signed under it through
   2026-09-25; `owner: 2026-10-08`, the PB1636 batch). A new batch signing adds its key in the change that writes
   its rows, so the cell records WHEN its argument was read;
6. a `derivation` field that is not the anchor **COMPUTED** from the row's own rule-id, or for which
   `docs/CONFORMANCE.md` §8 carries **no row**.

⚖ **How this coexists with `PB280` Q2 ("no" for anchor-only DOC rows), which it does NOT reverse.** A DOC row
asks the implementor to STATE a choice, so "there is nothing to observe" is unfalsifiable there and cannot close
a row — that answer stands. `DOC-A.1-19` is admitted for the opposite reason: its determination **is** stated,
in §7, and it names a greenfield site; only its WITNESS is impossible, because §2023:14.9.5.4 GR10's branch is
reachable only through the GR2 locate step this runtime never performs, so the effect is byte-identical to
GR7's mandated no-op. **The distinction is encoded by ORDER, not by a special case**: `state_for` charges the
kind's anchor and observability costs FIRST and consults the derivation only where a spec-derived `test-ref`
would have been consulted. A DOC row whose only location is its §7 anchor therefore still computes `GAP`, with
a derivation or without one — refusal 3 above adds the second half, that a DOC row §7 does not document cannot
carry a derivation at all.

## 2. The unit of review — the normative rule

The spec expresses requirements as: **Syntax Rules (SR) + General Rules (GR)** per statement (§2023:14.9.x) and clause
(§11/§12/§13.x); **general formats** (the grammar); **§8 concepts** (classes/categories §2023:8.5, conditions §2023:8.8.4,
reference/subscript/ref-mod §2023:8.4, standard conversions §2023:8.5.1, arithmetic/boolean expressions §2023:8.8); **§15 intrinsic**
definitions; **Annex A** required-documented-behavior; **Annex E** edition deltas; **Annex F** obsolete/archaic. Each
rule = `(rule-id, §-anchor, subject, kind, edition-applicability, text)`. The complete rule set is the coverage
**DENOMINATOR** — the thing the audits never enumerated.

## 3. The three phases

### Phase A — build the spec-rule catalog (the denominator)
Extract EVERY normative rule from `specs/ISO_COBOL_2023.md` into a structured, machine-checkable catalog. The spec's SR/GR
are numbered under "Syntax rules" / "General rules" headings per section, so extraction is tractable (parse by
§-section). **Output:** `docs/rearchitecture/spec-rule-catalog.json` — one entry per rule:
`{ id, section, subject, kind: SR|GR|format|concept|intrinsic|doc-behavior, editions, text }`.
**THE RULE IS THE UNIT** (owner decision 2026-08-08, landed by kb/Work PB55): one entry per TOP-LEVEL numbered rule
of a rule block; every sub-item — lettered, or a numbered list nested under one, at any depth — is its parent rule's
text, addressed by citation path (`9) c) 2.`), and verifying the rule means verifying every sub-item, in that one
row. A block may also print SEVERAL lists at its own margin, each under its own prose lead-in (§2023:14.7.5, §2023:9.3.6):
those are rules in their own right and take sub-list ids `<kind>-<section>-L<sublist>.<n>`. The transcription's
indentation cannot tell a flattened nested list from such a peer list, so the difference is MEASURED on the PDF
(`scripts/spec/measure_sublist_levels.py`, recorded in `scripts/spec/data/sublist-levels.json`) and the extractor's
`--check` fails on a sub-list the record does not hold at level 0. A **completeness
critic** cross-checks the catalog against the spec's TOC + section numbering, because an OMITTED rule is silent false
confidence. Orchestrated: fan out over the §-clauses; loop-until-dry.

**A general-format (FMT) row is a format the clause PRINTS, not a heading that says "General format(s)"** (kb/Work
PB1536): a candidate heading yields a row only when its body carries a diagram or the "Format N" labels §2023:5.2.1 gives a
multi-format construct. A candidate with neither is a PARENT clause about formats (§5.2 itself — skipped and printed)
or a leaf whose diagram the transcription lost (a loud `--check` failure: restore the `<pre>`/`<u>` block from the
rendered page). `CatalogCoverageDriftTests.EveryFmtRow_CarriesADiagramOrAFormatLabel` holds the bar over the written
catalog.

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

**An edition's inventory is a VIEW: its catalog's rules joined with its store's adjudications, each rule's state
computed** (owner 2026-11-08: "We must eliminate redundant code and data. The new implementation should be as if we
originally architected it"; kb/Work PB4992). Each fact has one home:

| fact | its one home |
|---|---|
| the rule: id, citation, section or locator, kind, ordinal, subject, module, level, box, text, status (normal, archaic or obsolete, read from the text's own lists) | the edition's catalog (`extract_rule_catalog.py --edition`), regenerated from the text |
| the adjudication: verdict, code-location, test-ref, derivation, notes, and for an older edition the change record `next` (§10.2) | the edition's store, one file per adjudicated rule |
| the text the adjudication was decided against | the store file's `rule-digest`, compared with the catalog's text on every read |
| the state (OK or GAP) | nowhere: computed by the view, never stored |
| the edition band of a rule | derived from the lineage (§10.2); the store keeps only what no held text decides yet |

**The store.** One JSON file per adjudicated rule, `tests/version-matrix/inventory/<rule-id>.json` for 2023 and
`tests/version-matrix/inventory-<edition>/<rule-id>.json` for an older edition (kb/Work PB4148, PB4991), written only
through `scripts/spec/record_verdicts.py` (§8). A rule nobody has adjudicated has no file: the catalog is the
denominator, and an unadjudicated rule is a GAP by the view. It sits beside `constructs.json` and the rest of the
machine-read test data rather than under `docs/`, because its reader is the battery, not a person. **Why a file per
row:** the merge queue runs no merge driver (kb/Work PB3642), so a catalogue that two landings both edit admits one pull
request at a time, and the one 4,214-row file was the second cause of landing holds (PB4148). Two landings that close
different rows touch different files and merge with git's plain merge; a conflict means two branches changed the SAME
row. A file is named by its rule id, nothing else lives in the directory, and two names differing only in case are
refused (`merge_drivers.py --self-test`, its real-tree arm).

**The view, ONE PER LANGUAGE.** `scripts/spec/inventory_view.py` (`inventory(edition)` over a working tree, its root
the repository's or another worktree's, and `inventory_at(rev, edition)` for a commit: every file the state rule reads,
the store, the catalog, the schema, `docs/CONFORMANCE.md`'s registers and the Annex A.2 list, read as that one tree has
it, `inventory_schema.Tree`) and
`tests/_shared/TraceabilityInventory.cs` join the catalog and the store; the state is computed by the one state rule
(`inventory_schema.Schema.state_for`, and for an older edition the chain, §10.2) and, on the C# side, by its twin
(`SpecTraceabilityInventoryDriftTests.DerivedState`), which `EveryRuleState_AgreesWithThePythonView` holds to the Python
view's state of every rule (`inventory_view.py --states`). Every reader reads the view; none opens a store file or a
catalog to rebuild it. The store (`inventory_store.py`) is the files only, and the view is a second module because it
is a second job. The view refuses: a store file of a rule the catalog no longer has, a store file holding a field no
store file holds (a catalog field or a state copied in), and a store file whose `rule-digest` is not the catalog's text
(an adjudication decided against other text, kb/Work PB698; `record_verdicts.py` accepts that one only for a rule its
batch re-adjudicates). The store refuses a stray or misnamed file and two names that differ only in case. The refusals
are said once, in Python: `inventory_view.py --check` is the gate's INVENTORY VIEW audit, in every lane, and no C# test
re-checks them; the C# tests hold the C# view to the Python one. The view's order is the spec's (section or locator,
ordinal, id). The editions are DATA, `inventory-schema.json`'s `editions.of`, read by both languages: each edition's
year, its band spelling and, for a held edition, its catalog, locator, store directory, legacy single file and the
`adjudicated` fields its store holds (beside `store.keys`). No code names an edition's year: adding an edition is an
entry there (`lineage.py --self-test` adds a 2027 and watches the order, the bridges and the store follow).

| store field | meaning |
|---|---|
| `rule-id` | the catalog id; the file's name |
| `rule-digest` | the catalog text the adjudication was decided against (`inventory_view.rule_digest`'s squash) |
| `verdict` | one of the six defined in `inventory-schema.json` — the vocabulary lives THERE, not here, so it cannot drift between this doc and the tools |
| `code-location` | the traceability link: `path` or `path#Symbol` (see the correction below), or `""` if not located |
| `test-ref` | the spec-derived test covering it, in a form that RESOLVES on disk (§8), or `""` |
| `derivation` | §1.1's owner-signed alternative to `test-ref`, for a rule with no observable obligation: the register anchor `docs/CONFORMANCE.md#DRV-<rule-id>`, **COMPUTED from the row's own rule-id**, or `""` |
| `editions` | (2023 only) the band within the editions no held text decides yet: a run from the introducing edition through 2023 among 2002 and 2014. Whether the rule exists at 85 is the lineage's (§10.2): the `85` token leaves this field when the 1985 change records are complete and a check finds the stored and the derived 85 agree (kb/Work PB4996) |
| `next` | (an older edition only) the change record against the next held edition (§10.2) |
| `notes` | the fix-queue item id for a DIVERGES; the owner decision for a non-support |

`scripts/session-probe.ps1` reports each edition's live GAP count from the view — the **burn-down metric** for P14.

> **⛔ DESIGN CORRECTION, superseding the `file:line` this table used to specify.** `code-location` is a **path,
> optionally plus a `#Symbol`** — never a line number. A line number rots on the next edit to the file, so 3,790
> rows of them would hold the battery gate permanently red for reasons having nothing to do with conformance, and
> a gate that cries wolf is a gate nobody reads. A symbol survives refactoring, moves with the code it names, and
> fails only when the thing it points at actually stops existing — which is the one event worth failing on.

> **⚙ THE `code-location` OF A `kind: DOC` ROW (Annex A.1), and why it has a second form.** A DOC row is not a
> behavioural rule: it is an obligation to SPECIFY and to DOCUMENT an implementor-defined element (§2023:4.2.5), and a
> binder symbol alone cannot discharge that — a DETERMINATION in `docs/CONFORMANCE.md` §7 can. So a DOC row's
> `code-location` is **its §7 register anchor, plus every greenfield implementing site**:
> `docs/CONFORMANCE.md#DOC-A.1-<n>; src/WiseOwl.Cobol.…#Symbol[; …]`.
>
> **The anchor is COMPUTED from the row's own rule-id, never chosen** —
> `inventory-schema.json` → `kinds.DOC.anchor-template` expands `{rule-id}`, which is why §7's item key is the
> rule-id `DOC-A.1-<n>` and not the bare item number. Two things follow. A determination filed under a
> neighbouring item cannot be *spelled*, which is the structural answer to `kb/Work/A11` (the §2023:15.3.3.2
> determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's accuracy). And a bare number
> could never have been an anchor at all: the gate resolves a `#Symbol` by a word search over the file, so `#7` —
> which five live rows carried, all `state: OK` — matched the digit 7 anywhere in a 790-line document.
> `code-location.anchored-files` now holds each such file to its real anchor spaces (`DOC-A.1-<n>` for §7,
> `4.2.16` for §1, `A.4.<n>` for §5) and forbids citing one bare.
>
> **A DOC row closes exactly as every other row does**: its own anchor, at least one site matching
> `kinds.DOC.implementation-pattern` (`^src/WiseOwl\.Cobol\.` — the same greenfield predicate `audit_annex_a1.py`'s
> source sweep applies), and a spec-derived `test-ref`. ⛔ There is **no policy arm**: a row whose only location
> is its §7 anchor stays a GAP. **The owner answered that question `no` on 2026-09-02 (`kb/Work/PB280` Q2)** —
> §1(a) is not widened — on the ground that the claim such an arm would rest on is unfalsifiable: greenfield
> source cites just **13 of the 222** items in `implementor item N` comments, so for the other 209 "there is
> nothing in the compiler to observe" would be contradicted by nothing *and confirmed by nothing*
> (`feedback_verdict_evidence_invariant`). An item with genuinely nothing to observe is discharged by an
> explicit owner decline per item, or stays open. The §7 row's machine-readable **`Pinned by`** cell names the
> same spec-derived test(s), and `audit_annex_a1.py` holds the two in agreement — the determination and its
> evidence are one artifact, which is the half the computed anchor does not cover.
>
> ⚠ **The anchor is owed by the rows with a determination to point at — and the REGISTER decides which those
> are, not the verdict.** CONFORMS, PARTIAL and DIVERGES each assert something about what §7 says, so each owes
> its own row unconditionally. `DOCUMENTED-NON-SUPPORT` has **two grounds on a DOC row**, and they differ exactly
> here:
>
> | ground | §7 row | anchor | how it closes |
> |---|---|---|---|
> | the conditioning facility is not implemented, so A.1's preamble **withdraws** the item ("the item is not required if the optional or processor-dependent feature is not implemented") | must **not** exist | not owed | its module's **witness** alone (items 84, 85, 173, 86) |
> | the element is **optional and not provided** — `kb/Work/PB280` Q1, owner, 2026-09-02 | **exists**: stating the non-provision *is* the determination | owed, like CONFORMS | anchor + a site + a witness (items 127, 206) |
>
> `kinds.DOC.anchor-exempt-verdicts` therefore names the verdicts that **may** be exempt, and one predicate per
> language (`Schema.anchor_obliged` / `AnchorObliged`) asks the register, over one parser per language
> (`inventory_schema.section7_rows` / `tests/_shared/ConformanceRegister.cs`). `audit_annex_a1.py` applies the
> same test — the DNS selectors' rows *minus the ones §7 documents* (`unreachable_items`, emitted as `--json`'s
> `unreachable`) — so the writer, the drift gate, the register gate and the REMAINING counter answer alike.
> ⛔ The first half was found only by landing the A.1 lane and the derived-verdict lane onto one tree
> (2026-09-02): the A.1 lane wrote the anchor rule where every DOC row was a determination, the derived lane then
> stamped four DOC rows `DOCUMENTED-NON-SUPPORT`, and the writer refused the whole batch. ⛔ The second half was
> wrong from the moment PB280 Q1 was answered, and silently: keyed on the verdict, items 127 and 206 would have
> been excused their own determination, `check_pins` would have stopped holding their witness in agreement with
> §7's `Pinned by` at the moment the witness landed, and the audit would have printed "⊘ CANNOT ARISE" about an
> element this register documents in full — dropping REMAINING by two for no work done.

## 5. Execution model — two concurrent lanes, five row-shape lanes (owner decision PB278, 2026-09-01)

Workflow-orchestrated (owner: max parallelism for disjoint work). **Fan out by §-clause** (disjoint units).
**Incremental accumulation** into the inventory — resumable across sessions; the GAP count is the burn-down. **Loop-
until-dry + completeness critics** so nothing is silently uncovered. Every Phase-C fix lands with its **spec-derived
golden** and a comprehensive gate (tiered testing, `feedback_tiered_gates`). This is many sessions of work; it is
paced by the inventory GAP, not big-banged.

**⚖ Adjudication and fixing are TWO LANES THAT RUN CONCURRENTLY** (`kb/Work/PB278`, replacing the 2026-08-09
"backlog to zero before adjudication" order). The measurement behind it: of 3,636 GAP rows on 2026-09-01, 3,361 had
never been adjudicated while thirteen per-wave review fleets (~1,000 agents, ~4B cache-read tokens in three days)
had moved the GAP by 27 rows. The two lanes never write the same thing: the adjudication lane reads a `git
worktree` pinned at a commit and writes verdict batch files; the fix lane edits the main tree.

### 5.1 The lanes, ordered by rows closed per token

| lane | rows (2026-09-01) | unit of work | what closes a row |
|---|---|---|---|
| **Golden** | CONFORMS rows with an empty `test-ref` (139) + DOCUMENTED-NON-SUPPORT rows without a witness (18) | one writer agent per subject family (~10–14 rows), one refuter per file checking ONLY that each expected value is derivable from the cited rule text | a spec-derived golden (or witness) registered in the manifest; no compiler change |
| **Derived verdict** | rules made unreachable by an A.4 module the owner has declared *Not claimed* (`CONFORMANCE.md` §5) | one `derived-verdicts` selector per module in `inventory-schema.json` (DATA — the PB198 mechanism) + one witness negative test per module | the selector + the witness; the drift test holds the population |
| **Adjudication** | never-adjudicated rules (3,361) | `phase_b_batch.py` batches of ≤20 rules per agent, **dossier-fed** (§5.2), refuters read only the CONFORMS / DNS candidates | a verified verdict; CONFORMS rows then go to the golden lane, defective rows to `kb/Work/` |
| **Fix** | defective rows (PARTIAL / NOT-IMPLEMENTED / DIVERGES) via their `kb/Work` notes | clusters by MECHANISM, not by note; the proven loop (implementer → self-review → fleet → director → lander) with the fleet sized in §5.3 | the fix + golden + re-verdict in one change set; one comprehensive battery per cluster batch |
| **A.1 documentation** | Annex A.1 DOC rows (222; 49 now carry a verdict, 45 §7 determinations exist) | the schema's `kinds.DOC` defines what a DOC row costs (§4); then batches by statement family | a §7 determination as `code-location` + a witness golden as `test-ref` — **except** a DOC row an A.4 module the owner declined has **withdrawn**, which belongs to the **Derived verdict** lane instead and closes on that module's witness alone (A.1's preamble makes the item not required, so there is no determination to file: §4, §8.1; four rows — items 84, 85, 173, 86 — as of 2026-09-02). ⚠ A DOC row the derived lane stamps because the element is **optional and not provided** (PB280 Q1) is NOT that case: it has a determination, so it stays on this lane's evidence rules in full (items 127, 206) |

⛔ **THE ADJUDICATION LANE IS VERDICT-ONLY, AND THE GOLDEN LANE STAYS A SEPARATE LANE** (owner decision,
2026-09-04, `kb/Work/PB468` Q1). It was put to the owner as a bare question — *may the agent that assigns a row's
verdict also author that row's witness, given a mechanically checked pre-run predicted expectation and an
independent blind replicate?* — and the answer was **no: keep the lanes separate.** The proposal it declines is
recorded whole, with the measurement that motivated it, in
`docs/rearchitecture/evidence/PROCESS-REVIEW-2026-09-04.md`; that evidence is frozen and is **the analysis, not
this document's process.**

The reason is **independent derivation of the evidence.** An agent that has located and read the implementing code
has already formed its expectation *from that code*; a witness the same agent then authors is checked against that
one derivation, and a pre-run predicted expectation or a blind replicate checks the *record* of it rather than
supplying a second one. Two lanes each read the standard for themselves and produce **two** derivations, and the
second one demonstrably earns its cost: every adjudication batch so far has overturned some CONFORMS verdicts, and
always downward. §1's definition of DONE is therefore unchanged, and so is the price it implies — the same rule is
read twice, once to verdict it and once to witness it. That cost is accepted as the price of the guarantee; it is
not waste, and it is not to be removed by merging the lanes. The measured consequence, recorded so nobody
rediscovers it as a surprise: an adjudication batch on its own moves the GAP only ~8–11 %, because its CONFORMS
rows wait for the golden lane.

The same decision left **`kb/Work/PB468` open with six further questions** the owner has not yet been asked — the
implementer cap, table tests closing many rows, effort tiers, the already-OK "shall" rows with positive-only
evidence, Report Writer, and extending PB386's derivation arm. Three of those would change §1 or §5 if answered
yes; none of them is in force.

**Batch order in the adjudication lane** alternates a *defect-rich* batch — §14 statement groups, grouped by
mechanism (the I/O statements together, the string statements together) so the notes it produces already cluster —
with a *closure-rich* batch (§13, then §12, §11, §8, §7, the small clauses; §13/§12 adjudicate ~96% CONFORMS where
§14 adjudicates 64%). The lane stays one batch ahead of the fix lane: the GAP moves every battery and the fix lane
never starves.

### 5.2 The dossier — agents verify, they do not search

Phase-B batch 8 agents spent ~120 turns each, most of them discovering what they were to verify. The batch
generator therefore hands each agent, beside the rule texts: the `.g4` rules for the construct, the binder /
emitter / runtime files that cite the clause, every test and golden that cites it, and the `CONFORMANCE.md`
determinations for it. Discovery is a grep; verification is the agent's job.

### 5.3 The fleet is sized to the cluster, and it runs once

A fleet per wave at ~100 agents was the largest token sink relative to burn-down, and five of the last eleven
findings were defects the wave itself introduced. So: the implementer runs the four review lenses as a **self-review
checklist before** any fleet; the fleet is **~40 agents** (four lenses, two refuters per finding) and runs **once
per cluster**; implementer agents receive the apply-ready contract, not a discovery brief, with a turn budget.
Refute stages on anything that CLOSES a row are never dropped (§9).

Two mechanical enablers, each its own change set: the A13 collection split (landed 2026-09-02 as partitions — the
Conformance leg fell 721 → 600 s, not the ~10× predicted, because the box saturates at 24 physical cores; the next
lever is a persistent compile host), and the pinned-worktree convention that lets the adjudication lane read while the
fix lane builds.

**⛔ How every lane is RUN is the owner's standing instruction of 2026-09-02, and its operational SSOT is the
`workstream` skill (`.claude/skills/workstream/SKILL.md`, with the brief and workflow templates):** checkpoint to disk
(WIP commits + `STATUS.md` per worktree; one JSON line per rule per workflow stage), replace a killed agent with a
fresh one reading the checkpoint, keep to the concurrency budget (one lander + ≤3 implementers + one four-wide
read-only chunk), land finished work first with one lander on main at a time, allocate ids centrally. The fleet
sizing above is a consequence of that budget, not a separate rule.

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

An inventory row is a claim, and nothing writes one but this mechanism: the store holds adjudications only (§4),
so the one way a verdict, a witness or a change record enters it is a checked batch. That is not a convenience. **The GAP count is the definition of v1.0** (D13), so a row is a claim about completion, and
an unaudited claim about completion is a self-report. The mechanism below exists to make each of the three claims
in §1 — located, verified, tested — cost what it is supposed to cost.

**Nobody hand-edits the inventory.** A reviewer or a Phase-B agent emits a **batch file** of verdict records;
`scripts/spec/record_verdicts.py` merges it.

```
python scripts/spec/record_verdicts.py --dry-run batch.json     # validate + report the GAP delta, write nothing
python scripts/spec/record_verdicts.py batch.json [more.json]   # merge
dotnet test tests/WiseOwl.Cobol.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

A record sets only the adjudicated fields (plus the two retirement control fields below) and is keyed by `rule-id`:

```json
{"rule-id": "AR-15.7-1", "verdict": "CONFORMS",
 "code-location": "src/WiseOwl.Cobol.Compiler/Binder/Intrinsics/IntrinsicSignatures.cs#Abs",
 "test-ref": "conformance:2023/intrinsic_abs; unit:IntrinsicArgumentRuleTests.Abs_RejectsAlphanumeric",
 "editions": "85,2002,2014,2023", "notes": ""}
```

**Witnesses MERGE; statements are restated (kb/Work PB959).** `code-location` and `test-ref` are sets of
witnesses (`inventory_schema.WITNESS_FIELDS`): the writer UNIONS a record's values with the row's, so a record that
names one new test adds it and a record that omits the field keeps what the row had. `verdict`, `editions`,
`notes` and `derivation` are single statements (`inventory_schema.STATEMENT_FIELDS`): a record that names one
restates it and a record that omits one leaves it as the row has it (kb/Work PB3795, PB3375; until then an omitted
`notes` was written as empty and four landings erased 25 rows' forensic prose with every gate green). A statement
never goes from saying something to saying nothing (`inventory_schema.statement_losses`, asked by the writer and by
the auditor: writing a field empty over a non-empty one is refused, and the `derivation` of a row that can no longer
stand on it, one that now carries a spec-derived `test-ref` or whose verdict no longer resolves, is the one excuse:
`Schema.derivation_superseded`, the first two refusals of `Derivation.refusals`; a record re-adjudicating a
derivation-closed rule to a defect verdict writes `"derivation": ""` to withdraw it), and a record that changes the verdict must restate `notes`, whose prose argues the old
verdict. Three refinements, each measured:

- **A witness leaves a row only by a named, reasoned retirement** — `"retire-witnesses": "<ref>; <ref>"` plus
  `"retire-reason"`. Each ref must be on the row; the writer drops it and appends `retired-witness: <ref>
  (<reason>)` to the row's `notes`, which is the durable mark the gate below reads. Any other loss is refused.
- **A re-adjudication re-sites `code-location`.** A record that CHANGES the verdict and names a code-location
  replaces that field (`inventory_schema.resites`): a PARTIAL row's code-location names the defect site, and a
  union would present it as implementation evidence for the CONFORMS record that closes it. The inventory's own
  history showed this is the dominant legitimate removal (1,105 of 1,714). `test-ref` is never re-sited.
- **A witness-only record** — `{"rule-id": …, "test-ref": …}`, no verdict and no statement field — adds
  witnesses to an adjudicated row and restates nothing, so it is safe to re-apply on a merged tree after another
  batch re-adjudicated the same row.

**The count is gated, not only the resolution.** `SpecTraceabilityInventoryDriftTests` checks that each surviving
reference RESOLVES and deliberately not how many there are, so until PB959 a batch that subtracted valid evidence
left every gate green. `scripts/spec/audit_witness_loss.py --check` (run by `build-local.{sh,ps1}` and
`battery.sh`) compares the inventory with the merge-base with main and is RED on any witness lost without a
retirement mark or a re-site, and on any statement (`notes` prose, `derivation`, `verdict`, `editions`) that went
from non-empty to empty; `--history` replays the file's git history under the merge rule and `--restore-batch`
emits the batch owing back what the old overwrite silently removed (witness-only records for the witnesses, full
records restating the row's own verdict for the erased `notes`).

**A symbol must be in the code, and the prose is read too (kb/Work PB762).** One predicate,
`SpecTraceabilityInventoryDriftTests.SymbolResolves`, decides whether a `#Symbol` still exists, for a `code-location`
and for a `path#Symbol` spelling in a row's `notes`: the symbol must be in the file's CODE (a C# file's tokens, so a
comment, a doc comment and a disabled region do not count but a string literal does; a grammar file's text without its
comments; any other file's whole text), and in a C# file a dotted symbol is a qualified name, so the member must be
DECLARED in that type of the file (a Roslyn walk of its declarations; a member of another type of the same file does not
resolve it, kb/Work PB3278) while a simple name need only be used in the code. The old whole-body word search let `FileRegistry.cs#ReadLockGovern` resolve on three
CONFORMS rows for as long as a comment narrated the method's removal, and left `notes`, the field a reader opens to
learn why a row has its verdict, unread: a rename re-pointed the field the gate checks and five rows then
contradicted themselves, a dated erratum stapled beneath prose that still described the dead method. The prose is
`notes` up to the first `retired-witness:` mark (the schema's `notes.retired-witness-mark`, read by both engines); a
mark is history by construction and may name a symbol that is gone. The writer's half is a shape rule decidable from
the record and the row (`inventory_schema.retired_symbols_left_in_prose`): once a witness is retired its symbol may not
appear in the prose unless the row still cites it at another file, so the history of the change goes in the
retirement's reason and the prose describes the code as it is. What no resolver can read is a BARE identifier in old
prose: it also narrates history and the standard's own words, so a stale bare name is caught only at the next
retirement, and the rows that predate the rule are a measured residue (kb/Work PB762).

**The five parts, and why each is where it is.**

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` | the verdict vocabulary, each verdict's `resolves` flag and required evidence, the legal editions, the `code-location` pattern, the `test-ref` forms, and **`kinds`** — the PER-KIND evidence rules (§4) |
| the Python loader | `scripts/spec/inventory_schema.py` | parsing that schema, deriving `state`, and the ATOMIC inventory write |
| the writer | `scripts/spec/record_verdicts.py` | validating a batch's SHAPE and merging it all-or-nothing — witnesses by union (PB959) |
| the witness-count gate | `scripts/spec/audit_witness_loss.py` | that no row LOSES a witness without a retirement or a re-site (PB959) |
| the battery gate | `tests/WiseOwl.Cobol.Tests.Unit/SpecTraceabilityInventoryDriftTests.cs` | REFERENTIAL integrity, continuously |
| the register audit | `scripts/spec/audit_annex_a1.py`, a gate audit (ANNEX A.1 REGISTER) | that `docs/CONFORMANCE.md` §7 is internally sound and agrees with the inventory |

- **`kinds` is orthogonal to `verdicts`, and that is the point.** `verdicts` says what a VERDICT costs; `kinds`
  says what a ROW OF THIS KIND costs. Before it existed the schema described only one kind of claim — a
  behavioural rule located in a binder and pinned by a golden — so all 222 Annex A.1 rows sat verdict-less while
  §7 already carried 39 determinations, a third of that work invisible to the burn-down. Adding a kind is an
  edit to the JSON; the two evaluators read it, and neither carries a copy.

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

### 8.1 The DERIVED verdict — one determination, applied by a predicate rather than by a list

Some verdicts are not adjudicated per row at all. When the owner declines a whole optional module, every rule the
module conditions becomes unreachable *at once*, and there is nothing left to decide about any single one of them.
Recording that by hand produced the failure kb/Work PB198 measured: sixteen rules on one question carrying **four
different verdicts** — NOT-IMPLEMENTED (9), blank (2), NEEDS-OWNER-DECISION (4) and CONFORMS (1).

**Two shapes qualify, and the second one is newer than the mechanism's own description of itself.** Until
2026-09-02 "the decision makes the rules unreachable" was written down as if it were the definition; PB280 Q1 is
the counter-example. Its rows are perfectly **reachable** — an A.1 documentation obligation for an element a user
can write today — and what is common to all of them is the **adjudication**: the owner answered, once, a question
each of up to 30 rows asks in the same words ("an A.1-OPTIONAL element §7 records as *Not provided.* closes as
DOCUMENTED-NON-SUPPORT"). A selector of the second shape wants reading twice as carefully, because its rows are
live surface; the protection is that its arms turn on **what the owner wrote** — the requirement class the
standard states and the determination this repository filed — never on an agent's reading of a rule's text.

So the module's scope is written **once, as DATA**, in `inventory-schema.json` under `derived-verdicts`, and
**both engines read it**: `DerivedSelector` in `scripts/spec/inventory_schema.py` (driven by
`scripts/spec/derive_verdict_batch.py`, which emits the batch) and `DerivedVerdictDriftTests.Select` (which holds
the population every build). Neither carries a selector. ⚠ That claim was *aspirational* from PB198 until
2026-09-02 — no Python consumer of `derived-verdicts` existed, so the PB198 batch was hand-made and the schema's
own `$comment` asserted a second reader that was not there.

**A selector is `arms`: a disjunction of conjunctions.** Within an arm every field present must hold; a rule is
selected when any arm holds and no entry-level `excludes-patterns` matches its text. The fields exist because the
standard scopes a module in more than one way and each way was, at some point, the one a text-only predicate
missed:

| field | the scoping mechanism it reads | the module that needed it |
|---|---|---|
| `sections` | the rule's own CLAUSE — §2023:8.8.1.4 is *titled* for the declined mode and its rules never repeat the phrase | A.4.3, A.4.8, A.4.14, PB198 |
| `pattern` | the rule's TEXT — the facility's functionality stated from inside a mandatory clause | PB198, A.4.14 |
| both, as an AND-gate | a term that is STATEMENT-LOCAL (`file-name-1` means the FD's own subject in §2023:13.4.5.4) or an OPERAND NAME (§2023:12.3.7's `data-name-1` IS the CURSOR operand) | A.4.13, A.4.2 |
| `xref-sections` + `kinds` | an Annex A.1 documentation obligation citing a declined clause, keyed on the same clause numbers as the clause arm | A.4.8, A.4.14 |
| `excludes-kinds`, per arm | a general format (kind FMT) is evidence about a CLAUSE, never one of its formats | A.4.2, A.4.3 |
| `requirement` | the A.1 **requirement class the standard itself states** ("This item is optional."), carried on the catalog row — A.1's preamble writes its licences per class | PB280 Q1 |
| `determination-prefix` | what **`docs/CONFORMANCE.md` §7 says** about the element, read off the register row keyed by the rule's own id — which is exactly the anchor `kinds.DOC.anchor-template` computes | PB280 Q1 |

Two properties belong to the **matcher**, not to any selector, because a convention each author must remember is
a convention one author will forget. Clause prefixes match **component-wise** — §2023:13.18.30 is not inside §2023:13.18.3,
and a raw `StartsWith` had the screen selector taking 543 rules instead of 156, flipping 32 CONFORMS rows to
non-support. And the text is **hyphen-normalised** before matching, because 29 catalog rules spell an operand name
with U+2011 where the rest use ASCII `-`, which silently *under*-selects — the direction that leaves the drift
test green.

⛔ **`determination-prefix` is the first arm field that reads an artifact outside the catalog, and it has to be:
a DOC row's implementation IS its determination.** Three consequences, each load-bearing. The §7 table therefore
has **one parser per language** — `inventory_schema.section7_rows` and `tests/_shared/ConformanceRegister.cs` —
because three readers now need it (the register audit, `anchor_obliged`, this arm). An item with **no** §7 row is
never selected: a missing determination is not a negative one. And the match is on the cell's **opening** words
with markdown emphasis stripped, never `contains` — item 127's determination discusses what is *not* provided
three sentences into its body, and the opening words are the register's answer to A.1's question.

⛔ **THE `requirement` AXIS IS STILL NOT FALSIFIABLE AGAINST TODAY'S DOCUMENT; the `determination` axis
BECAME so on 2026-09-09.** When this landed, all 30 A.1-optional items and all 47 §7 rows agreed — the only
"Not provided." determinations were on optional items, and the only optional items with a determination said
"Not provided." — so a predicate that had dropped `requirement` altogether, or matched *any* determination,
selected the same two rows and left `DerivedVerdictDriftTests` green. ⭐ Re-measured 2026-09-09 the second half
is no longer true: §7 now carries **58** rows, and **5** of A.1's 30 optional items have a §7 row (10, 127,
150, 184, 206) of which only **2** open "Not provided." — item 206 left the set when owner decision Q30
(`kb/Work/PB592`) found that §2023:13.18.60.4 GR12's SIGNED minimum bands are OPEN at the bottom and the element is
PROVIDED. A predicate that matched *any* determination now selects five rows and this file's own live
assertion (every selected row's cell opens "Not provided.") turns RED. The `requirement` axis is still blind
live, because every "Not provided." row remains on an optional item. That is precisely the
shape `feedback_green_gates_arent_evidence` names, so each axis is driven against a **fabricated** catalog and a
**fabricated** register by `python scripts/spec/derive_verdict_batch.py --self-test`, which
`TheSelectorEngine_ProvesEveryAxisCanFail` shells every build and which asserts the **case names**, because a
shrinking self-test still exits 0. The axis that most needs it is `requirement`: a **required** item whose §7 row
said "Not provided." is a conformance *defect*, and stamping it DOCUMENTED-NON-SUPPORT would document
non-conformance as if the owner had licensed it.

**Three properties keep a derived verdict honest, and they are what distinguish it from a shortcut:**

1. **It may never overwrite an adjudication.** A selected row may be blank, or may already carry the selector's
   own verdict (agreement — the reassuring direction). A row carrying CONFORMS, PARTIAL or DIVERGES means the
   determination and a human reading of the code disagree, so `derive_verdict_batch.py` **refuses the whole
   batch** rather than reporting a rewrite the way an adjudication batch legitimately does.
2. **One row, one determination.** Two selectors claiming the same rule would leave it carrying whichever batch
   ran last; the generator refuses it and `NoTwoSelectors_ClaimTheSameRow` asserts it against the data.
3. **It does not close the row.** DOCUMENTED-NON-SUPPORT resolves, but §1(c) still demands a spec-derived test
   proving the documented posture is what *actually happens* — and for a declined module that is the module's
   **witness**: a negative case showing the construct is refused, by name. The 2026-09-02 A.4 landing stamped
   **308 rows** and moved the GAP by **zero**, which is the correct and expected outcome; the PB280 Q1 landing
   stamped **2** and moved it by zero for the same reason (their witness debt was `kb/Work/PB373`, DISCHARGED
   2026-09-09 — item 127 on `conformance:cases/A.1-Implementor-defined-language-element-list/pb373_object_computer_name_inert`, and item 206 by leaving the
   selector altogether when owner decision Q30 / `kb/Work/PB592` found the element IS provided, closing CONFORMS
   on `conformance:cases/A.1-Implementor-defined-language-element-list/pb592_binary_usage_range_bounds`).

**The printed FORMAT band is a catalog field, and a selector arm reads it** (kb/Work PB284). The standard scopes a
rule three ways — by clause, by words, and by the group label printed above it ("FORMAT 2", "SEQUENTIAL FILES",
"ALL FORMATS"). `extract_rule_catalog.py` carries the third as `band` (the label as printed) and `band-formats` (the
format numbers it names) on every SR/GR row printed under one, and no longer lets the label leak into the text
before it. The `formats` arm field selects a rule whose `band-formats` is a non-empty SUBSET of the arm's numbers —
solely, because a rule under "FORMATS 1 AND 2" also holds for format 1 — and is only legal beside `sections`, since a
format number is its clause's own (PRESENT WHEN's FORMAT 2 is not ACCEPT's). The operand arm stays: §2023:14.9.1.3 prints no
band at all and SR-14.9.1.3-5 is scoped by the names `identifier-3` / `identifier-4`; neither mechanism subsumes the
other. The eight rows this reached (five in `validate-only`, three in `screen-handling-only`) are the ones
the old `$format-band-residue` block recorded as unreachable.

**Prose concept clauses are rows too** (kb/Work PB284 part 3, PB479). A clause whose rules are running prose with no
numbered marker (§2023:9.1.16 record locking, §2023:9.1.17, §2023:9.1.18.x commit and rollback) was invisible to the numbered scan
and to every completeness guard. The manifest's `paragraph-blocks` key admits such a clause, with its rule kind and
the reason, and the extractor emits one row per normative paragraph (`GR-9.1.16-p1`, field `shape: paragraph`).
`audit_catalog_coverage.py` holds each admitted clause to the same standard as a numbered one: the rows' text is the
clause's text, sentence for sentence.

**What the gate actually enforces** (eleven facts, and it is proven able to fail — see below): the inventory covers
the catalog exactly, no row contradicts its catalog entry, every verdict is in the vocabulary, every stored `state`
equals the derived one, every verdict carries its required evidence, every edition name is real, every
`code-location` names a file that exists and a symbol still in it, and every `test-ref` resolves —
`conformance:<edition>/<case>` and `nist:`/`characterization:` to a `.cob` on disk, `unit:<Class>.<Method>` and
`conformance-test:<Class>.<Method>` to a method really declared in that class. Plus the two `kinds` facts (§4):
every kind-anchored row whose VERDICT claims a determination carries the anchor its OWN rule-id computes (§4's
`anchor-exempt-verdicts` carve-out), and every fragment on an `anchored-files` path is a legal anchor of that
file's space — an anchored file being cited bare included.

**And the register audit runs every build.** `audit_annex_a1.py --check --json` was wired into nothing until
2026-09-02 — not `battery.sh`, not the CI workflow, not `build-local.*` — so the A.1 register's own correctness
was enforced when a human remembered. it is now a gate audit (`audit_annex_a1.py --check`, kb/Work PB4453), run in every lane, following
the precedent of a Unit class that shells a Python gate, which puts it in the per-commit wave-local
gate, battery phase 1 and both CI unit jobs at once. It asserts its POPULATION (a run that filed nothing is not a
pass), that every §7 row is filed under the item A.1 names, that no `DOC-A.1-<n>` token occurs outside a row key,
that each row's `Pinned by` agrees with its inventory row's spec-derived `test-ref`, that every verdicted DOC row
has a determination, and that the script's own `--self-test` still names every case it drives.

> **⚠ A gate observed only passing is indistinguishable from a gate that inspects nothing**
> (`feedback_green_gates_arent_evidence`), and this one will spend most of its life green over rows nobody has
> touched. Two things answer that. `TheseChecks_ActuallyFail_OnAFabricatedInventory` drives every check above
> against rows built to break it — one defect class at a time, plus positive controls so the failures cannot come
> from a checker that rejects everything — and it runs on every build. And the gate was confirmed against the
> REAL artifact once, by corrupting the inventory (then one file, `traceability-inventory.json`) in place: a row hand-promoted to `OK` and a
> CONFORMS row citing a file and a golden that do not exist turned exactly three tests red, then green again on
> restore.

### 8.2 The DERIVATION — how §1.1's evidence kind is recorded and checked

§1.1 says WHAT a derivation is and what it may not do; this says where the parts live. **The reference shape is
the DOC row's, reused rather than paralleled**: an anchor COMPUTED from the row's own rule-id, carried on the
row, cross-checked against a register PARSED by one reader per language.

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` → `derivation` | the record field, the anchor template, the §8 heading, the owner signatures (one per dated owner decision), and the four arms with their `names-pattern` + `check` |
| the register | `docs/CONFORMANCE.md` **§8** | the determinations themselves — one row per closed rule, keyed `DRV-<rule-id>` |
| the register parser | `inventory_schema.register_section` / `tests/_shared/ConformanceRegister.cs` | ONE markdown-table reader per language, now taking the HEADING and stopping at the next `## ` — §7 and §8 are two calls, not two parsers |
| the undefined-element list | `tests/version-matrix/annex-a2-undefined.json`, generated by `scripts/spec/extract_annex_a2.py` | Annex A.2's 66 items, each item's own trailing citation, and the catalog `rule-id`s that citation RESOLVES to |
| the state rule | `Schema.state_for` / `DerivedState` | the same clause, in both engines, in the same change set (`kb/Work/PB315`'s lesson) |
| the writer | `record_verdicts.validate` | the six refusals of §1.1, at record time |
| the gates | `SpecTraceabilityInventoryDriftTests` · `AnnexA2UndefinedListDriftTests` | that every derivation row still holds, and that the A.2 artifact still equals the spec |

- **The A.2 list is a GENERATED ARTIFACT, not a parse at check time, and that is the shape that makes the next
  A.2 row automatic.** The extraction lives in exactly one place — a Python script that reads
  `specs/ISO_COBOL_2023.md` and resolves each item's citation against `spec-rule-catalog.json` — and both engines then
  read the resolved `rule-ids` as data. Parsing the spec markdown in C# as well would be the second parser this
  design spends its whole length avoiding, and re-parsing 66 items per row would put a 1.3 MB read on the path
  `EveryRuleState_AgreesWithThePythonView` walks 4,311 times. `extract_annex_a2.py --check` regenerates and diffs, so
  the artifact cannot go stale silently; `AnnexA2UndefinedListDriftTests` runs it every build, the
  gate-audit precedent of `audit_annex_a1.py` (kb/Work PB4453).
- **Adding an A.2-arm row costs one register row.** The item→rule-id resolution is mechanical, so the next rule
  the standard declares undefined needs no edit to any list — only the determination and its `A.2 item <n>`.
- **⛔ §7's `DOC-A.1-<n>` token invariant had to learn the second register.** `audit_annex_a1.py` asserts the
  COUNT of each `DOC-A.1-<n>` token in `CONFORMANCE.md` equals the number of §7 rows keyed by it — a check that
  exists because an inventory row resolves its anchor by a word search over the WHOLE file, so a prose mention
  would otherwise read as a filed determination. A §8 row keyed `DRV-DOC-A.1-19` is a second, legitimate
  occurrence. The invariant is therefore stated over BOTH registers (`§7 rows keyed T` + `§8 rows keyed DRV-T`),
  and `check_pins` — which is what actually holds a DOC row's evidence in agreement with §7 — was widened at the
  same time from "closes on a spec-derived test-ref" to "closes", so the guard that matters did not weaken.

## 9. RUNNING A PHASE-B BATCH — the fan-out, and what its prompt MUST carry

A batch is: pick the next contiguous clause block → generate one input file per subject → fan out one agent per
subject → hand each result to an INDEPENDENT agent told to overturn it → merge → gate.

```
python scripts/spec/phase_b_batch.py 15.32-15.44     # writes scratchpad/phase-b/in-<slug>.json, prints the slugs
#   … each file carries the subject's DOSSIER; --no-dossier omits it, --include-adjudicated is NOT for a batch …
#   … run the workflow (one adjudicate agent + one refute agent per slug) …
python scripts/spec/record_verdicts.py --dry-run scratchpad/phase-b/out-*.json
python scripts/spec/record_verdicts.py scratchpad/phase-b/out-*.json
dotnet test tests/WiseOwl.Cobol.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

`phase_b_batch.py` excludes rules that already carry a verdict, so a re-run after a partial batch is safe.

**Every input file also carries a DOSSIER — the discovery, pre-computed, so the agent VERIFIES instead of
SEARCHING.** Batch 8 (2026-09-01) cost 574M cache-read tokens over 39 agents for 224 rows, and most of each
agent's ~120 turns went on the same three greps — where is the grammar rule, where is the binder and emitter,
where are the tests — a search with one answer per subject that was being bought once per agent. The dossier
computes it once per subject and pastes it into each of that subject's part files: the ANTLR rules (matched on
the rule name, on the clause its comment block cites, and on the construct keyword), every `src/WiseOwl.Cobol.*` file
citing the subject's clause with the citing lines, the same for `tests/` including the `.cob` goldens, the
`docs/CONFORMANCE.md` determinations, the `kb/Work/` notes whose `spec_refs` or `inventory_rows` fall inside the
subject **with their `status`** — which is how §8 item 8's "do not re-report a landed fix" gets answered without
anybody hand-writing the list it forbids — and the citing `docs/DIAGNOSTICS.md` rows. Nothing is hand-mapped:
the clause set comes from the catalog rows plus their construct root, and the keyword from the catalog's own
`subject` field, because a hand-kept construct→file table would be a sixth work register (CLAUDE.md rule 8) whose
staleness would be invisible — a dossier missing an entry looks exactly like a construct that has none.
⛔ **That is also the dossier's limit, and every file repeats it to the agent in `how-to-read`: it is a selector,
so it is evidence about what it RETURNED and none about what it dropped.** An empty list means "this derivation
found none", never "the compiler has none" — 8 of the catalog's 599 subjects are prose with no keyword to search
with at all, and code implementing a rule without citing its clause is invisible to it. A NOT-IMPLEMENTED verdict
still needs the agent's own search. The generator prints a per-subject count table, and a row of zeros is the
signal that that one agent starts cold.

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
   "§2023:12.3.7 GR7 k3 … distinct ascending" that came out of *our own design doc*, and the one-level-short shape
   (§2023:13.18.60 where the rule is in §2023:13.18.60.4).
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
   constant is admissible wherever ANY class it can present is admissible (§2023:8.3.3.6.4 GR4; fix-queue PB48) — so
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

## 10. THE EDITION PARAMETER — the 1985 audit (kb/Work PB4886)

`--std=1985` is audited against ANSI X3.23-1985 itself (`specs/ANSI_COBOL_1985.md`, with Amendments 1 and 2), not against
the reconstructed `VERSION_CHANGE_REFERENCE.md` and the NIST CCVS85 suite alone. **It is this audit with an edition
parameter, not a second audit:** the same three phases (§3), the same definition of done (§1), the same inventory store (one directory per edition, §10.2) and
its one recording path (§4, §8), and the same rule that an omitted rule is silent false confidence (§7). Only the reader of
the standard's structure, the row key and the change record between adjacent editions are per edition, and each is below. The 2002
edition (kb/Work PB4813) takes the same path once it is transcribed; it is ISO-structured, so its reader is closer to
2023's.

### 10.1 The 1985 rule catalog and its key

`python scripts/spec/extract_rule_catalog.py --edition 1985` reads the 1985 document in its own structure
(`scripts/spec/rule_catalog_1985.py`, self-test on planted text):

- **The locator is a Roman section and that section's own paragraph number** (`§1985:VI.5.7.3`), because paragraph numbers
  restart in each section; Amendment 1's Section A (the Intrinsic Function module) is cited `§1989:A.2.5.3`. The readers
  of that structure are `scripts/spec/citation.py`'s (`SECTION_85`, `PARAGRAPH_85`, `FOLIO_85`, `AMENDMENT_85`), so the
  catalog and `cite.py --check` cannot disagree about what a paragraph is.
- **A rule block** is a paragraph headed Syntax Rules (SR), General Rules (GR), Arguments (AR) or Returned Values (RV),
  headed with `#` or, where the transcription did not head it, a plain line opening with its number; a **rule** is a
  column-0 `(n)`, and its lettered `a.` items and their `1)` items are its text (THE RULE IS THE UNIT, §3). A General
  Format paragraph is an FMT row. The page-break furniture (the folio, the page's running header) is never rule text.
- **The row key** is `<year>-<kind>-<locator>-<n>` (`1985-SR-VI.5.7.3-1`; a list restarted inside a block `-L<k>.<n>`, an
  FMT row `1985-FMT-VI.5.8.2`), with its citation (`§1985:VI.5.7.3 SR1`). A CITATION NAMES ONE RULE (kb/Work PB4969): a rule of a
  restarted list is cited by its list, `GRL2.1` for the id's `-L2.1` (`citation.rule_designator`, the one writer; the format's
  `SUBLIST_RULE`), so `§1985:IV.4.2.2.2.3 GR1` is the first list's rule 1 alone, and the extractor's `--check` fails on two
  rows that share a citation (a row cited by a printed page, `§1985:p.XIII-56`, is exempt: the page is no rule's citation).
  The 2023 catalog carries no `citation` field. The year prefix names the edition (or amendment) the
  rule is from, in the 1985 store (§10.2).
- **The conformance requirements are the FRAME, not rows** (kb/Work PB4968). Paragraph 1.5 of Section I (§1985:I.1.5: what a
  conforming implementation must support, the subsets, the optional modules, the obsolete elements of §1985:I.1.5.2.3) is
  unnumbered prose, and the 2023 audit treats its conformance clause the same way: §2023:4.2 enters the definition of DONE
  (§1) and docs/CONFORMANCE.md by direct citation, and only its numbered lists (§2023:4.2.10) are catalog rows. So the 1985
  conformance statement (kb/Work PB4885) cites §1985:I.1.5 and its sub-paragraphs directly as its frame, each requirement
  answered in docs/CONFORMANCE.md, and the catalog gains no row.
- **Module and level.** Sections VI to XVI are the eleven modules and Section A the twelfth. Every module but Sort-Merge,
  Report Writer and Intrinsic Function has two levels (§1985:I.1.2; Amendment 1 adds the third one-level module), and the
  standard boxes every specification unique to level 2. A row's `box` is `none`, `part` or `whole` (the prose box is the
  transcription's one markup, `rule_catalog_1985.BOX_SPAN`; a general format's is FIGURE-STYLE.md rule 9's double line),
  and its `level` is 2 only when the whole rule is boxed in a two-level module. A `part` row is a level-1 rule with a
  level-2 span, verified at both levels. A box in a one-level module is a finding. The high subset (§1985:I.1.5.1) is
  every row of the required modules at its module's highest level.
- **The amendment.** The amended 1985 is a different language level for edition gating, so a row carries its `document`
  (the base standard, or Amendment 1 for Section A) and `amended_by`: Amendment 1's Addendum 1 and Amendment 2 are
  tables of changes, a row per change keyed by the printed page it begins on and a paragraph and rule, and a change that
  names a rule records its amendment there. A change that ADDS a rule ("add the following as the new general rule 4") is a
  row of its amendment, cited by the amendment's own page (`§1993:p.B-14`), and keeps the number the amendment gives it;
  the base rules keep their 1985 numbers, which are the ones 1985 citations use.
- **Completeness** is held as for 2023: a rule-shaped heading no kind resolves, a recognised block that yields no rule,
  an ordinal that neither continues nor restarts its list, a box in a one-level module, an amendment change naming a rule
  the catalog cannot find (read from the change's directive, never from the new text it carries), a rule after a scan gap
  that no entry places, an adjudication the transcription no longer has, a duplicate id, a rule of an admitted paragraph
  its kind map does not name, and a row whose citation `cite.py --check` would refuse (`rule_catalog_1985.cite_check`,
  `citation.validate` on every row) are each reported, and `--check` fails on them. The paragraphs that DEFINE the rule
  kinds (§1985:IV.3) are outside the denominator, as §2023:5.3 is. **The kind table is ONE**: `extract_rule_catalog.KINDS`
  types both editions' headings, its "Editing Rules" (GR) and "Precedence Rules" (SR) included, the same PICTURE rules
  §2023:5.3.3 and §2023:5.3.2 type, and `extract_rule_catalog.RULE_SHAPED` is the one test of a rule-shaped heading.
- **The content-keyed scan is the 2023 arm's, with the edition as its parameter** (§7, kb/Work PB29). A guard keyed on
  what a block is CALLED cannot find one called something else, so the reader measures EVERY paragraph whose heading names
  no rule block, in every section and appendix (`extract_rule_catalog.ordinal_shape`, the one measure of both editions:
  ordinals, lists, longest run), and compares the result with the committed manifest
  `docs/rearchitecture/spec-unharvested-rule-blocks-1985.json` (`UNHARVESTED_OF[1985]`, the 2023 manifest's shape, keyed
  by citation because the base text's Appendix A and Amendment 1's Section A both have an `A.2.1`). A paragraph measured
  and undeclared, declared and no longer measured, or with a moved ordinal count is a drift (`unharvested_report`, one
  report for both editions), and `--check` fails on it. Each paragraph's disposition is an adjudication by §1985:IV.3.1
  and IV.3.2, read with the 2023 manifest's disposition of the same text: `rules` ADMITS it as a rule block of its `kind`
  (`admitted_kinds`: one kind, a per-item map, a per-sub-list map whose entry may itself be a per-item map, as
  VI.6.3.1.1.2 needs), and `not-rules` records why its list states no requirement (a worked example, an index, a
  technique the text calls one, a glossary entry, the history and the element lists of the appendices, a verbatim
  restatement of a module paragraph that is admitted). The blocks whose heading names rules that KINDS does not type
  ("Standard Alignment Rules", "Formation and Evaluation Rules", the Report Writer's counter and presentation rules,
  IV.4.2.1 Separators and VI.5.15.6) are entries of the same manifest: there is no second adjudication table in the code.
  `EXCLUDED_BLOCKS` (IV.3, VI.6.4, XIII.3.10) and `KNOWN_EMPTY_BLOCKS` (VI.5.9.6) stay, as in the 2023 arm. The Index and
  the appendices open no rule block: a rule-block heading there is reported.
- **The scan's own defects.** The scan prints folios X-11 and X-12 twice (PDF pages 527-528); the transcription keeps
  both copies under `citation.SCAN_REPEAT_85`'s comment, and the catalog reads no rule from the repeat. Where the scan
  lacks printed pages (`citation.SCAN_GAP_85`), the text after the gap is never the block before it; a page that resumes a
  paragraph whose heading is on a missing page names it in `rule_catalog_1985.GAP_RESUMES` (PDF page 634: the TYPE
  clause's syntax rules 5-7, rows `1985-SR-XIII.3.20.3-5` to `-7`: paragraph 3.20.3 of Section XIII). That paragraph has
  no region in the transcription, since its heading is on a missing page, so its rows cite the printed page that owns
  them (`§1985:p.XIII-56`), as an amendment's added rule cites its own page.

The catalog is written to `docs/rearchitecture/spec-rule-catalog-1985.json` and held by the gate's RULE CATALOG 1985
audit (`--edition 1985 --check`: no finding, and the committed catalog is this run's). On the reconciled file it counts
2,253 rows from 664 blocks (SR 645, GR 1,253, AR 65, RV 80, FMT 210) and no finding; the content-keyed scan measures 90
paragraphs (756 ordinals, an upper bound), of which the manifest admits 53 (254 rows) and records 37 as not rules.

### 10.2 Rule lineage: one rule list per edition, one change record per adjacent pair (owner 2026-11-08, kb/Work PB4991)

The owner's model (2026-11-08): "separate lists for each version's rules. 1985 sets the original aka base set of them.
Then 2002 can do one of three things for each 1985 rule": keep it (it applies to 2002 and is in effect a 2002 rule),
deprecate it (it will be removed later), remove it, and it can define rules new to 2002, "and similarly for later
versions". The mechanism below was approved the same day; it replaces the earlier plan of 1985 rows that carried a 2023
verdict by reference into the one 2023 store.

**One rule list per edition, from that edition's own text.** Each edition has its catalog (§10.1 for 1985, §3 for 2023;
2002 and 2014 when transcribed, kb/Work PB4813), and its inventory is the view of §4 over a store of its own,
`tests/version-matrix/inventory/` for 2023 and `tests/version-matrix/inventory-<edition>/` for each earlier one
(`inventory_store.py` and the view `inventory_view.py`, each of which takes the edition; `inventory_schema.Edition`,
read from the schema's `editions.of`, carries what differs: the catalog, the key that locates a rule, the store's
directory and fields). A reader names the edition it reads, or reads the newest held one.

**Change is recorded once, between ADJACENT editions only** (85 to 2002, 2002 to 2014, 2014 to 2023, and 2023 to 2027),
never between every pair. Each rule of the older edition carries one classification against the next held edition, in a
`next` field on its own row (so the record shares the row's file and adds no register):

| kind | meaning | what the compiler owes |
|---|---|---|
| KEPT | the next edition states the same requirement (`to`: its rule or rules) | one verdict and one test, good at both editions |
| CHANGED | same subject, a different requirement | its own expected behaviour at each side |
| REMOVED | no counterpart | the older edition accepts it; the newer refuses it (syntax) or does not do it (semantics) |

A rule of the newer edition that no older row's `to` names is NEW; that is derived, never written. A `next` stores the
rule ids it links, its `reason` (the words that differ, or what deleted the feature) and its `source` (the audit run or
the adjudication); its evidence is the citation of the older rule and of each `to` rule, read from their catalogs,
never stored a second time, and each one `cite.py --check`s against its own text. The record's shape is DATA, the
schema's `next` (kb/Work PB4993): the keys it may carry, the fields that state a verdict, and each kind's flags (does it
name `to` rules, must it state its `reason`, is its verdict its chain's: `chained`, KEPT alone), read as
`inventory_schema.ChangeRecord` by `lineage.next_errors` and `lineage.state_of`; no code spells a kind. The one writer
is `record_verdicts.py --edition <older edition>`.

**Deprecation is not a fourth kind.** The standard itself marks an element obsolete or archaic in an edition
(§1985:I.1.5.2.3 requires a conforming implementation to support the obsolete elements of what it claims), so `status`
is a fact of that edition's rule, read from its text into its catalog: KEPT into a row whose status is obsolete, then REMOVED at the next edition, is the
deprecate-then-remove lifecycle. A catalog row's `status`, in every edition's catalog, is `normal`, or one clause per
standing naming the list entries that confer it (`obsolete: C.1(n)`, `archaic: F.1(n)`; two standings join with `; `).
The authority is the edition's own enumerated lists: 1985's obsolete list (§1985:C.1, 18 entries); 2023's archaic list
(§2023:F.1, 2 entries, which §2023:4.2.12 defines) and obsolete list (§2023:F.2, 5 entries, which §2023:4.2.13
defines). They are held once as data per edition (`docs/rearchitecture/spec-element-standing-<edition>.json`,
`extract_rule_catalog.STANDING_OF`): per list its standing, its locator and its remark sections; per entry its number,
the terms that name the element in a rule, the sections that specify it, a section's own extra terms (VALUE's VALID and
INVALID phrases), and a SHARED section's numbered formats that are wholly the element (`section-formats`: the EXIT
statement's Format 2 (program), the data description entry's Format 4 (validation), PRESENT WHEN's Format 2
(validation), VALUE's Format 5 (content-validation-entry)), whose band's every syntax and general rule is marked, by the
rule's `band-formats`, whether its text names a term or not; the entry's name is the list's own text. Both catalog arms apply them through one call (`element_standing.apply`). The
marking (`scripts/spec/element_standing.py`) is decided per SECTION: an OWN section, whose heading names one of the
entry's terms (THE ALTER STATEMENT, THE RERUN CLAUSE, the Debug and Segmentation modules; 2023's FLAG-02 directive,
standard-binary arithmetic, the VALIDATE statement and the clauses that exist only for it), marks every rule; a SHARED
one (an I-O-CONTROL paragraph beside SAME, a file description entry, VI 3.2 beside PROGRAM-ID, the OPEN, STOP, GO TO,
DISABLE and ENABLE statements; 2023's EXIT, IF, SEARCH and MOVE statements, the ARITHMETIC clause, the data description
entry and the PRESENT WHEN, VALUE and VARYING clauses) marks only the syntax and general rules that name a term, never
its general format, whose later fate is its own CHANGED record. OWN or SHARED is derived from the heading, never held.
The drift checks, which the RULE CATALOG and RULE CATALOG 1985 audits fail on, hold the data to the text, per list: the
entries are exactly the list's items, every section is a locator of the text, every sentence that gives an element the
list's standing ("is an obsolete element", "is an archaic feature") falls in a section some entry names (or a remark
section the data names: the 1985 level tables' Z notation, a 2023 definition's note, Annex A's lists, Annex D's
concepts), every sentence giving a NAMED format the standing ("The Program format of the EXIT statement is an archaic
feature", "The validation format of the data description is an obsolete feature") names a format an entry declares, a
declared format is a numbered format of its section, a format's number is never a term (it misses the band's rules that
do not repeat it), every entry marks a rule (counted by the marking itself, so a shared general format naming a term does not
count; 1985 entry 1, double character substitution, is prose outside any rule block and entry 4, MEMORY SIZE, is named
only in the shared OBJECT-COMPUTER format, so both are declared `no-rule`, checked both ways), and no shared section's
format and no rule outside its entries' sections is marked. 1985: 143 rules obsolete. 2023: 13 archaic and 106
obsolete. A later edition's lists are one more data file read by the same code.

**A verdict is held once per chain.** A run of KEPT links is one chain; its verdict, code location and test live on the
chain's NEWEST row, and an older row in it holds none (the schema refuses them there), so 2023's adjudicated work serves
every 1985 rule that carries into it without a copy. The older row's state is the chain's, evaluated at its own edition:
the chain's test must also be placed at that edition (the conformance suite's placement, DESIGN-conformance-suite.md).
CHANGED and REMOVED rows, and NEW ones, carry verdicts of their own. The fleet's work is therefore only what changed.
Both languages compute it: Python's `lineage.state_of` and the C# view's `StateAt` in the drift test over
`TraceabilityInventory.RowsOf`, held to each other for every held edition by
`EveryOlderEditionsRuleState_AgreesWithThePythonView`. The session probe and the ledger report each held edition's rules and
GAP side by side (`inventory_view.edition_views`); the ledger's trend series stays the newest edition's GAP.

**The audit fills the change records** (§10.3). Its passes are detections. A SAME that the verify pass upholds is
recorded as KEPT through the one recording path (`record_verdicts.py --edition 1985`, a batch the run itself writes from
its dispositions, `audit_batch.py change-batch`, as the 2023 derived batches are written by `derive_verdict_batch.py`);
a DISPUTED rule, and a DIFFERS or NONE answer (a CHANGED or REMOVED candidate), is decided by the `adjudicate` pass and
checked by one independent pass per decided kind (§10.3, kb/Work PB4995, PB5008): the `recheck` for a KEPT or CHANGED
decision, the `refute` pass for a REMOVED one. An upheld decision is recorded through the same batch, and only what they
leave contested goes to a fleet adjudicator, whose decision is recorded through `record_verdicts.py --edition 1985` with
itself as the `source` and outranks the run's: the run's batch never writes over it. The 1985 audit's decided rules are
recorded (759 upheld SAME rules, kb/Work PB4993, and the upheld adjudications, kb/Work PB4995; 7 of them corrected by
the sample audit of kb/Work PB5008). A record states its `reason` or omits the
key: an empty reason is not stored (`lineage.next_errors`). A CHANGED or REMOVED rule then takes a verdict of its own
under `--std=<edition>`, its witness a draft from the drafts pass that `run_drafts.py` ran and the fleet checked against
the rule text.

**Everything else is derived from the chains, never written by hand:** each edition's GAP count (the session probe and
the ledger report one per edition), the conformance statement of each edition (kb/Work PB4885 for 1985), the conformance
suite's edition bands (a band is a chain's span: DESIGN-conformance-suite.md §3.5), the drift test that the
compiler's edition gates (`constructs.json` windows, the grammar's version predicates) agree with the lineage's edges,
and **each diagnostic's citation at each edition** (kb/Work PB4950, the owner on 2026-11-08: "a way for a
compilation error to accurately describe the edition + rule violated"). A diagnostic cites one rule; walking that rule's
chain gives the rule of the compilation's edition (a 2023 SR read back along KEPT and CHANGED links to its 1985 rule),
and a chain that does not reach that edition gives the gate's own message from its edge: the construct is NEW at a later
edition, or REMOVED after an earlier one, each with the citation that proves it. The per-edition citations are generated
from the chains into the compiler's diagnostic data and checked by a drift test, never kept by hand.

**The band at an older edition is the chains' (kb/Work PB4996).** A rule of the newest edition exists at an older held
edition exactly when a KEPT record of that edition carries into it, and a bridged record carries it through every edition
the bridge spans (`lineage.spanned`); a CHANGED record proves no band, since the newer rule's requirement is not the
older one's (the older rule holds its own verdict). That rule has one definition, `lineage.chain_bands`, which maps each
rule a KEPT chain reaches to the positions it proves; the conformance suite's band reader (S5, kb/Work PB4683) takes the
positions below the newest edition from it. Until the stored `85` token and the derived one agree it stays stored, and
the view measures the distance (`lineage.band_drift`, built on `chain_bands`, printed by `inventory_view.py --check` and
listed by `--band-drift`): rules a KEPT chain reaches whose stored band lacks 85 or an edition the bridge spans
(`unstored`), rules whose stored band names 85 that no KEPT chain reaches (`unproven`), and the 1985 rules not yet
classified, while any of which remains `unproven` is provisional. The first set is a wrong band or a wrong KEPT record,
never decided by a script: each is adjudicated, through `record_verdicts.py` (the band) or `record_verdicts.py --edition
1985` (the record), by the fleet adjudication of slice 4 (kb/Work PB4995) and the refutation of the audit's upheld
records (kb/Work PB5008). When all three are empty the `85` token leaves the stored `editions` and every reader takes
the band at 85 from `chain_bands`; until then the token is the only 85 position the 1026 unproven rules have, and
deleting it would place them nowhere.

**The edition gates rest on the lineage's edges (kb/Work PB4996).** `scripts/spec/edition_gates.py --check` (the gate's
EDITION GATES audit) reads every gate, a `constructs.json` window and a version predicate of the grammar (citing what its
rule's comment cites), and holds each edge the held editions span to the rules its citation names. An edge between two
held editions, a START or an END, rests on a cited rule that changes between them: a rule of the newer one that is NEW or
that a CHANGED record names, or a rule of the older one whose record is REMOVED or CHANGED. Either side carries either
edge: a start is a construct the newer text adds (a new rule, or an alternative added to a format the older text has,
the common case) or a restriction it lifts (the 1985 rule "Identifier-1 must not be reference modified", REMOVED); an
end is a construct it drops or a restriction it adds. What a kind means is the schema's `next` (`chained`), never
spelled in code. **A citation proves an edge only when every rule it names changes** (branch review R1-1): a clause that
gained anything after 1985 holds changing rules beside KEPT ones, so a gate citing the whole clause would pass whichever
of its constructs it moved. A gate names the SR or GR its edge rests on. **A general format proves only a whole
diagram's edge** (branch review R2-1): the catalog holds one FMT rule per clause (a general-format subclause, or `Format
n`, names it), every format and alternative of the statement in it, so a CHANGED one would prove any construct of the
statement; a NEW newer FMT rule (a new statement) proves a start, an older one REMOVED an end, and nothing else. A
construct the standard states only in its diagram, with no SR or GR of its own (END-ACCEPT, the optional IDENTIFICATION
DIVISION header, USE without PROCEDURE: 12 windows at this slice), has no edge the lineage proves, and is held open in
`edition_gates.FORMAT_ONLY` until the catalog numbers each format's alternatives; a new window resting on a general format
alone is refused. An SR or GR has no direction in the lineage (a NEW rule adds a construct or a restriction), so an
inverted window citing one is the version matrix's to catch, not this audit's. A gate whose cited rules are all KEPT contradicts its chain, and the audit refuses it until the gate or the record is
corrected (two records were, by this slice: 1985-GR-VII.2.6.3-2 KEPT into line sequential organization, and the 1985
FILLER rules KEPT into 2023's wider entry-name clause); so is a gate citing nothing of either edition, one whose every
citation mixes changing rules with KEPT ones, or one resting on an unclassified rule. Three
gates lie outside the rules: a word's reservation (a gate citing only the word lists, §2023:8.9 and §2023:8.10, whose
edges `reserved-words.json` and `context-sensitive-words.json` carry), an edge specified in prose the catalog holds no
rule of (the 1985 continuation of lines, §1985:IV.7.2.2; the 2023 floating indicators, §2023:6.2.3.1), which the
CITATIONS audit still holds to the text, and a window wholly between two held editions, which waits for the edition
between them. A finding a known compiler defect holds open is listed in the audit (`HELD_OPEN`, checked both ways) with
the note that fixes it.

**A missing edition is bridged, provisionally.** Until the 2002 and 2014 texts are transcribed, the 1985 rows' `next`
points straight at 2023 and is marked `bridge`. When 2002 is held, its two adjacent records replace the bridge, and
composing them must give the bridge's answer: a cross-check of both audits, held by a drift test, at no extra cost.

**Why it stays maintainable.** A new edition costs one rule list, one adjacent change record and the adjudication of what
changed; every change carries a checked citation in both texts; and the drift tests hold that every older rule is
classified exactly once and every newer rule is reached or NEW.

**The slices** (kb/Work PB4992 to PB4997, in this order):
1. the inventory as the view of §4, in both languages: a store per edition holding adjudication only (no catalog
   copy, no stored state, no file for an unadjudicated rule), every reader moved onto the view, and the rebuild's
   seeding retired into the view's own checks;
2. the change record `next` and its recording path, the catalog's `status`, and the derived batch that records the
   audit's upheld SAME answers as KEPT;
3. chain state in the view, in both languages, and the GAP count per edition in the probe and the ledger;
4. the fleet adjudication of the disputed and own-row rules against `--std=1985`;
5. the derived bands (`lineage.chain_bands`, which the conformance suite's S5 reads), the `85` token leaving the
   stored `editions` once the stored and the derived 85 agree (the band drift measures how far apart they are, and
   slice 4's adjudication closes it), and the edition-gate drift test (the EDITION GATES audit);
6. the 1985 conformance statement (kb/Work PB4885).

### 10.3 The Message Batches arm — `scripts/spec/audit_batch.py`

The independent JUDGMENTS go to Opus 5.5 through the Message Batches API (owner 2026-11-07, kb/Work PB4886), in passes,
each a DETECTION a fleet agent turns into a verdict, never a verdict itself:

| pass | one request per | answer |
|---|---|---|
| `route` | 1985 clause (the paragraph that holds its rule blocks), with its subject, module and first three rules, and the list of every 2023 clause that has rules | ROUTED with the 2023 clauses that state the same feature (at most 8, closest first), or NONE |
| `map` | 1985 catalog rule, with its candidates (below); planned only once every route item is answered | SAME, DIFFERS or NONE, the 2023 ids and the differing words |
| `verify` | SAME answer | UPHELD, REFUTED or UNSURE: the adversarial pass tries to refute it |
| `claims` | 1985 claim of `VERSION_CHANGE_REFERENCE.md` (a JSON list of claims the fleet prepares, each with its 1985 locator) | CONFIRMED, REFUTED or NOT-ADDRESSED, with the 1985 `locator` and `rule` (null where no numbered rule decides) as fields of their own |
| `drafts` | rule whose disposition is a row of its own, and rule an adjudicate or recheck answer reads as CHANGED, REMOVED or OPEN (read from the answers, which only grow, so a re-plan only grows the plan) | a minimal program and the outcome the rule requires (for a refusal, the violating line and a conforming control), which `scripts/spec/run_drafts.py` compiles and runs under `--std=1985` and the fleet checks against the rule text before any use |
| `adjudicate` | DISPUTED or OWN-ROW rule, with its map candidates and both readers' answers (kb/Work PB4995) | its change record: KEPT or CHANGED (the 2023 candidates it carries into, and for CHANGED the differing words) or REMOVED (2023 drops it), or OPEN when the texts do not decide |
| `refute` | REMOVED adjudication, with every candidate the adjudicator saw | the strongest reading on which the requirement survives, first; then UPHELD, REFUTED or UNSURE |
| `recheck` | KEPT or CHANGED adjudication (kb/Work PB5008), at the highest effort, with its whole 1985 clause and the 1985 rules most like it by wording, every rule of every kind of the 2023 clauses that hold its feature and of each clause a decided rule lies in, and the map's worded candidates beyond them | framed to DISPROVE the decision: (a) another 1985 rule bearing on the element, (b) every 2023 rule that states any part of the requirement, (c) a refusal made a run-time exception condition; then the change record the texts support, with `rules_1985` and `exception` |

**A rule's candidates are drawn from the strongest signal down** (`audit_batch.candidates`, kb/Work PB4971, owner
2026-11-08: "Use the strongest appropriate signals"):
1. every 2023 rule of the SAME KIND in the clauses its clause was ROUTED to, or shares a heading with (`subject_key`:
   `THE START STATEMENT` and `START statement` are one), in catalog order and uncapped;
2. then the k rules most like it by wording that step 1 did not name (the inverse-document-frequency weight of the words of
   their headings and text, the same kind preferred; 15 by default).
Wording alone was measured on the first 1985 run (k = 15): 464 NONE answers, 217 of them giving "no candidate addresses
it" rather than a deleted feature; a heading alone matches no 2023 clause for 598 of the 2,253 rules, because 1985 repeats
a clause per module and names clauses differently. The route pass is the signal that reads the clause as a whole, one
request per clause (247 for 1985), so it is cheap; the heading and the wording stay as the routes' complement.

`results` writes each mapped rule's disposition (CARRIES, OWN-ROW, DISPUTED, or still open) and its DECIDED change
(`audit_batch.dispositions`, the one derivation): KEPT for CARRIES, and the adjudicate pass's answer once the check its
kind takes (`audit_batch.checker`, read from the kind's `to` flag: the recheck when it names 2023 rules, the refuter when
it names none) UPHELD it and no other check that answered disputes it. The recheck upholds a decision only when the
record it finds is the same one, the same kind into the same set of 2023 rules (`audit_batch.upholds`): a rule it adds
is one the decision omits. An OPEN or disputed adjudication is `contested`, a fleet adjudicator's (`cobol-adjudicator`),
never a vote. `python scripts/spec/audit_batch.py change-batch` turns every decided change into a `record_verdicts.py --edition
1985` batch (`change_records`, the one writer), leaving out each rule whose stored record another source (a fleet
adjudication) decided.

**Why KEPT and CHANGED take the recheck** (kb/Work PB5008, 2026-11-08). The refute pass upheld 1,345 of the first 1,371
decisions it read, and a sample audit by `cobol-adjudicator`s from both texts found 5 of 18 upheld CHANGED and 3 of 15
upheld KEPT decisions wrong, every one of them upheld by the refuter (12 REMOVED and 15 map + verify KEPT records: no
error). The refuter, the same model at the same effort shown the same candidates, measured agreement, not accuracy. The
errors fell in three modes, which the recheck's three questions name: a 1985 rule elsewhere already allows what the
record calls new; the record omits the 2023 rule that states the requirement (the commonest: a definition restated in
another clause, a group-parameter rule, the general rule of §2023:14.7.7 over every arithmetic statement); and a
compile-time refusal that 2023 turned into a run-time exception condition (§2023:14.9.39.4 GR2). So the recheck reads
whole clauses at the highest effort, and the refuter keeps REMOVED, its prompt stating the strongest counter-reading
before it judges. `python scripts/spec/run_drafts.py --state DIR` compiles and runs each draft at `--std=1985` through the
conformance harness (`conformance-suite/harness/run.py`'s `Run` and the WiseOwl adapter, the one judge of a program's
outcome): an output draft is an accepted test (`Run.accept`), and a refused draft a FLAGGED test (`Run.flagged`), so a
refusal counts only when it is the rule's: the draft names the line that violates the rule and carries a control (the
program with that line made conforming), the control must compile and the diagnostic must be AT that line. Every stored
draft names both, since the drafts pass's answer shape requires them; the 291 refused drafts written before it did were
withdrawn (`audit_batch.py withdraw`) and drafted again (kb/Work PB4995). It writes `DIR/drafts/runs.json`: MATCH,
MISMATCH or NO VERDICT per rule,
naming the compiler binary (its `--version` and the digest of its assemblies, never a checkout's HEAD). A MATCH is a candidate witness, placed in the conformance suite only after the fleet checks the draft
against the rule text; a MISMATCH is a defect or a suspect draft for the fleet to tell apart. The tool's rules:

- **Restartable**, on the ONE batch state the transcriber shares (`scripts/spec/message_batches.py`, `BatchRun`): an
  intent written before every create and the batch after, a crash between them adopted from the server's list on the next
  run, the usage of every billed reply recorded under its batch and custom id (a retried item's earlier reply stays in the
  spend `--max-usd` checks). A pass's items are planned once (`items.json`, every input its prompts need);
  each answer is written once and never overwritten; a reply outside the pass's answer shape is kept in `raw/` and its item
  retried. When the shape grows (a refused draft's violating line and control), `withdraw` moves each stored answer the
  shape now refuses to `raw/` (`<custom id>.withdrawn.json`) and `retry` asks for it again, so the shape stays the one rule
  of what an answer is and no reader keeps an arm for an older one.
- **A canary before each full submit**: `submit --canary N` first, and a full submit is refused until that pass holds a
  collected canary with a valid answer. **`--max-usd` on every submit and retry**, against spent plus in-flight plus this
  submit, priced by the canary's measured cost per request. The map pass on the unreconciled file estimates about $0.017
  a request, about $34 for the pass, before a canary measures it. Until a canary is proven, `retry` resubmits that
  canary's own unanswered items, as the canary, and nothing else (kb/Work PB4930).
- **A request is built in cache layers, most shared first** (kb/Work PB4998, after Anthropic's notice of 2026-11-08 that
  this account's direct API traffic had a low prompt-cache hit rate): the pass's instructions are the request's cached
  `system` (`message_batches.cached_system`), a group's shared part is its cached first user block
  (`message_batches.cached_text`), and the item's own part comes last (`audit_batch.prompt_parts`). The route pass's group is
  every request (the list of every 2023 clause, about 7,000 tokens); a map request's is the clause's candidates, which every
  rule of that 1985 clause and kind shares (`Candidates.clause`), so the rules of one clause read it once, measured on the
  re-run at 47 % of the map pass's input. The TTL is an hour (`message_batches.CACHE_TTL`), because a batch runs for
  longer than five minutes and its cache hits are best-effort; `usd` prices the cache writes (twice the input price) and
  reads (a twentieth on Opus 5.5) the replies report (`message_batches.usage_of`). A prefix below the model's minimum (512
  tokens on Opus 5.5: each pass's instructions alone) caches nothing and costs nothing.
- **One citation shape** (kb/Work PB4886): many 1985 paragraphs a reader cites are a Function, a General Format or prose
  with no numbered rule, so an answer names its `locator` and its `rule` as two fields (`audit_batch.CITATION_FIELDS`, the
  prompt's words), read through `citation.py` and validated against the 1985 transcription. The catalog citation every
  pass quotes for its rule must parse too (`plan --pass map` refuses a catalog whose row does not); its rule kinds are
  `citation.RULE_KINDS` (SR, GR, AR, RV), the kinds both catalogs write.
- **Public text only**: a prompt carries the tracked transcriptions' text, never a page image; a request whose text
  matches any per-copy stamp shape (`verify_publishable`) is refused before any batch.
- **The key** comes through `scripts/api_key.py` with `--api-account`; each batch record in a pass's `state.json` names
  the account that created it, never the key. A batch belongs to that account, so while one is uncollected (or a
  create's intent is unreconciled) every command bills its account; once every batch is collected a pass may move to
  another account, so one pass can spend two accounts' credit, and `status` prints each account's spend
  (`message_batches.BatchRun.load_key`, `pinned_account`, `billing_line`; kb/Work PB4995). The self-test (`scripts/spec/test_audit_batch.py`) uses a fake client (`scripts/spec/fake_batches.py`,
  the transcriber's self-test's too) and opens no key file.

The passes run after the reconciled 1985 file (PB4850) and its catalog exist; the verdicts stay with the fleet (§5, §8).
