# Process re-evaluation of the conformance burn-down — 2026-09-04

**Frozen evidence.** This file records a measurement and the owner decisions taken on it. Like everything else
under `docs/rearchitecture/evidence/`, it is an agent's recorded output and is never edited afterwards to keep it
current — the current process lives in `.claude/skills/workstream/SKILL.md`, the current definition of DONE in
`docs/rearchitecture/DESIGN-spec-conformance-review.md` §1, the current state in
`docs/COBOLNET_REARCHITECTURE_PLAN.md` §0, and the open work in `kb/Work/`.

## 0. What was asked, and what was decided

The owner asked: **is there a more efficient process that completes the COBOL compiler to 100 % — the PHASE-14
traceability inventory at zero GAP — more quickly?** Efficiency was defined as rows closed *per day* **and** *per
token*, **without weakening the definition of DONE** (`DESIGN-spec-conformance-review.md` §1). Three proposals were
written independently against `main @ 2986fc4c` (throughput-first, cost-first, risk-first), then reconciled into
one synthesis measured on the tree, with every measured figure marked ⓜ and its probe script kept beside it. Both
documents are reproduced below.

### The three decisions, taken 2026-09-04 by bare question (verbatim)

1. *"May the agent that assigns a row's verdict also author that row's witness, given a mechanically checked pre-run
   predicted expectation and an independent blind replicate?"* — **NO: keep the lanes separate.**
2. *"May every agent be hard-capped — 160 turns read-only, 220 for an implementer — splitting the work rather than
   extending the transcript?"* — **YES.**
3. *"May one lander carry five clusters in one landing, one commit per cluster inside it, never more than one
   landing per lander transcript?"* — **YES.**

### The witness-first pipeline is DECLINED, and why

The synthesis's central recommendation — *"one derivation per rule"*, its stages **S2** (derive → predict → witness
→ run in one agent) and **S4** (a blind replicate replacing both of today's refuters) — is **declined**. It is
recorded here as **the analysis, not as adopted process**. Adjudication stays **verdict-only**; the golden lane
keeps writing witnesses as a separate lane with separate agents.

The owner's reason is **independent derivation of the evidence**. An agent that has already located and read the
implementing code has already formed an expectation *from the code*; a witness it then authors is checked against
that same single derivation, and the two guards the synthesis offers — a pre-run predicted expectation and a blind
replicate — both check the *record* of that one derivation rather than supplying a second one. Two lanes, each
reading the standard for itself, produce two derivations; that is what the measurement below shows is worth paying
for, because **every adjudication batch so far overturned some CONFORMS verdicts, and always downward** (§1.2 A4,
§4 "Explicitly NOT stopped"). The double derivation the synthesis identifies as the root defect (§1.1 A2) is
therefore assessed as the **price of the guarantee**, not as waste to be removed.

Everything in the synthesis that does not depend on S2/S4 stands and is adopted or remains open below.

### Adopted from this review

* Decisions 2 and 3 above — the turn caps and the five-cluster landing train.
* Process mechanics needing no owner decision, because they change no definition: **one mechanism per implementer**
  (the measured cost law makes the second mechanism in one transcript cost more than a fresh agent — 44.6 M vs
  40.3 M); **a free implementer slot is filled within the turn it frees**, from `work.py next`; the **comprehensive
  battery runs in a worktree cut at the batch head** (its phase 2 rebuilds and would otherwise freeze the lander
  ~45 min); **read-only fleets keep probing a pinned worktree**; the **ledger refresh after every GAP-moving
  landing** stays.

These land in `.claude/skills/workstream/SKILL.md` §2 and §3, in `templates/implementer-brief.md`, in the new
`templates/lander-train-brief.md`, and as a 160-turn cap with an optional `deferred` return in the three workflow
templates.

### Still open

`kb/Work/PB468` (`kind: decision`, `status: owner`) carries the six questions from §6 of the synthesis that have
**not** been put to the owner, verbatim, as bare questions: the implementer cap 3 → 6; table tests closing many
rows bound by a two-way drift test; effort tiers on checked steps; the ~120 already-OK SR/AR "shall" rows with
positive-only evidence; whether A.4.11 Report Writer stays claimed; and whether PB386's derivation arm extends
class by class. `kb/Work/PB469` (`kind: analysis`) carries the measurement itself.

### Reading the two documents below

They are reproduced **verbatim**, including their own heading levels and their own front matter, because they are
the measurement. Nothing in them has been renumbered, corrected or brought up to date; where they disagree with
the decisions above, the decisions above are what the project does. Their probe scripts and raw usage data were
written to the session scratchpad and are not part of the repository; every figure they mark ⓜ was measured on the
tree at the stated commit.

---

## A. The measured inputs (data brief, verbatim)

# Process re-evaluation — measured inputs (2026-09-04, main @ 89044aa0)

Question from the owner: **is there a more efficient process that completes the COBOL compiler to 100 % (the traceability inventory at zero GAP) more quickly?** Efficiency = rows closed per day AND per token, without weakening the definition of DONE.

## The definition of DONE (do not weaken; owner-owned)
`docs/rearchitecture/DESIGN-spec-conformance-review.md` §1: a row is OK only on a resolving verdict with SPEC-DERIVED evidence — a
`conformance:` / `unit:` / `conformance-test:` test whose expectation is computed from ISO/IEC 1989:2023, or (as of PB386, landing
today) an owner-signed, checkable DERIVATION for a rule with no observable obligation, or DOCUMENTED-NON-SUPPORT for a declined
optional module (owner decision per module). NIST / GnuCOBOL / legacy differential never close a row. CLAUDE.md rules 1–8 bind.

## Where the 3054 GAP rows sit (tests/version-matrix/traceability-inventory.json)
- rows 4311 · OK 1257 · GAP 3054 = **2809 never adjudicated** + **245 adjudicated-but-open**
  (PARTIAL 150 · DIVERGES 48 · NOT-IMPLEMENTED 36 · CONFORMS-but-untested 9 → 1 after today's PB386/PB383 landings · DNS-still-GAP 2)
- un-adjudicated by kind: **GR 1357 · SR 1104 · FMT 175 · DOC 173**
- un-adjudicated by clause: §14 790 (§14.9 statements 644, §14.6 54, §14.7 37) · §13 700 (§13.18 data description clauses 582) ·
  §8 412 (§8.4 references 175, §8.8 87, §8.3 71, §8.5 56) · §7 255 (§7.3 compiler directives 192, §7.2 63) · §12 232 (§12.4 131,
  §12.3 100) · Annex A.1 173 (DOC rows) · §11 98 · §9 96 · §6 23 · §10 18 · rest < 10
- adjudicated 1502: CONFORMS 922 (61 %) · DOCUMENTED-NON-SUPPORT 346 · PARTIAL 150 · DIVERGES 48 · NOT-IMPLEMENTED 36
- DNS by clause: §13 236 · §14 56 · §12 23 · §15 9 · §8 8 (declined optional modules: screen, format clause, validate, …)

## How OK rows were closed
- evidence family: conformance goldens 1055 · conformance-test (C# corpus tests) 142 · unit 58 · nist 2 (legacy, pre-rule)
- 934 distinct test-refs close 1257 rows → **2.53 rows per test on average**; 721 OK rows carry more than one test-ref
- the biggest single closers are drift/refusal tests over whole declined facilities (163, 59, 25, 25, 25, 22 rows each)

## Rate (docs/rearchitecture/evidence/ledger-trend.json)
- 3742 (08-29) → 3054 (09-03): **138 rows/day over 5 days → ~22 days at that rate**; but the rate was carried by DECLINES
  (screen handling −152, witness B2 −104, witness cluster A −49 → DNS 2 → 346) and by the golden lane (−151, −52). Registering an
  adjudication batch moves GAP little on its own: **batch b1 = 184 rules adjudicated → Δ−15** (its CONFORMS rows wait for witnesses).
- lane-3 batches: b1 15 subjects/184 rules · b2 20/295 · b3 17/230 (in flight now). Un-adjudicated 2809 ≈ 12 more batches at this size.

## The current pipeline (what each row costs today)
1. **Dossier generation** (`scripts/spec` tooling → `in-<slug>.json` per subject: rules, sibling parts, clause map, grammar/source/
   test/determination/register map, diagnostics). Mechanical.
2. **Adjudicate** (`.claude/skills/workstream/templates/wf_lane3_refute.js`, phase 1; Opus, 4-wide): per rule read spec + general
   format + dossier + COMPILER CODE, probe with the pinned `cobol.exe`, assign verdict, name an EXISTING spec-derived covering test or
   leave test-ref empty; cluster defects into findings. ⛔ "do not write goldens here" is in the prompt. Checkpoint per rule.
3. **Refute** (same template, phase 2; Opus): an independent refuter re-derives ONLY the CONFORMS verdicts (reads spec, code, probes),
   default-refutes when uncertain. Batch-2 measured: 2/4 overturned on set-p3, all downward; every batch so far overturned some.
4. **Registrar** (Agent, worktree; Opus): merges (`merge_batch.py`), clusters findings by MECHANISM into `kb/Work/` notes, re-probes
   findings on a fresh tree (the pin is old), `record_verdicts.py` (CONFORMS rows close only if they already carry a test-ref),
   DEVLOG, lands by push. ~1–2 h.
5. **Golden lane** (a later round; Opus writers per cluster of CONFORMS-but-untested rows): re-reads spec + code, writes spec-derived
   witnesses, refuter checks the witness pins the rule (not the program), lander lands. Round 2: 49 witnesses → 52 rows.
6. **Fix lane** (implementer in a worktree + lander; Opus): per `kb/Work/` note or cluster; 245 defective rows / ~110 actionable
   notes today; implementers are the only agents that BUILD; ≤3 concurrent by the owner's budget.
7. **Ledger** (`gen_ledger.py` + artifact publish) after every GAP-moving landing.

Concurrency budget (owner standing instruction 2026-09-02, `.claude/skills/workstream/SKILL.md`): 1 lander + ≤3 implementers + one
4-wide read-only chunk ≈ 8 agents; the fleet guard (`scripts/hooks/fleet_active_build.py`) freezes builds PER WORKING TREE (a
worktree agent may build its own tree while main-tree fleets run; main-tree builds are denied while any main-tree agent is live).
Weekly usage limits have paused the campaign three times; a resumed long transcript is the most expensive thing run.

## Measured agent costs this session (subagent tokens as reported by the harness; wall clock)
- batch-2 refute, 4 subjects (22 CONFORMS rows): 4 agents, 750 k tokens, 22.5 min, 243 tool uses → ~34 k tokens per CONFORMS row refuted
- PB383 implementer (1 mechanism, sweep of 10 sites, 2 goldens, 2 rows): 256 k tokens, 27 min, 131 tool uses
- PB386 implementer (new evidence kind, Python + C#, 5 drift tests, 8 determinations, 8 rows): 411 k tokens, 45 min, 208 tool uses
- PB383 lander (apply, verify, full gate, hold): 117 k tokens, 13 min
- earlier measurement (memory `project_lane_plan_2026-09-01.md`, different accounting that includes cache reads): fleet ≈ 500 M
  cache-read, adjudication ≈ 2.6 M per row, implementer agents ≈ 71 M each; ~10–12 B projected for the whole burn-down.
- adjudication wall clock: a 4-wide chunk of subjects ≈ 30–60 min; a 17-subject batch ≈ 4–5 h at 4-wide before its refute.

## Levers already known (not yet decided)
- Effort tiers: the owner's measured curves — verification/retrieval is nearly flat (medium ≈ high at 70–85 % of the cost); coding
  gives up ~2 points at medium for half the cost; hard reasoning has no free cut. Today every agent runs at the session effort.
- Rows per witness: 2.53 today; a subject-level writer could cover many rules with few programs.
- FMT rows (175) are general-format-vs-grammar questions — `.claude/skills/spec-grammar-conformance` audits formats against the
  ANTLR grammar with the PDF page as authority; a different, cheaper shape than semantic adjudication.
- DOC rows (173, Annex A.1): each needs a determination in `docs/CONFORMANCE.md` §7 + a witness or (PB386) a derivation; the A.1
  mechanism (`audit_annex_a1.py`) and the DOC-row selectors exist.
- Existing corpus: 616 positive goldens + 690 negative fixtures + the C# corpus tests may already pin un-adjudicated rules that
  nobody has linked (a linking pass is cheaper than a witness, but each link is still a judgment that the test pins THAT rule's branch
  with a spec-computed expectation).
- The adjudicator's probe programs are nearly witnesses already and are thrown away; the golden lane re-derives everything.

---

## B. The panel synthesis (verbatim)

# The recommended process — one derivation per rule, and stop starving the fix lane

**Synthesis of the three independent proposals. 2026-09-04, measured on `main` @ `2986fc4c`** — which is *not* the
tree the three proposals measured: the **lane-3 batch-2 registrar landed while they were writing**, and it changed
the diagnosis. Every number marked ⓜ I measured myself on this tree; the probe scripts are beside this file
(`syn_probe.py`, `syn_probe2.py`, `syn_model.py`, and the shared `usage.json`). Nothing here weakens
`DESIGN-spec-conformance-review.md` §1 — two of the new gates *tighten* §1(c).

---

## 1. The diagnosis

### 1.1 What all three agree on — and what b2 changed underneath them

**A1 — the 138 rows/day headline is not repeatable.** ⓜ `ledger-trend.json` now holds 19 points / 18 GAP-moving
landings, 08-29 → 09-04: **720 rows, mean 40.0, median 14.0, four landings closing zero.** ⓜ **305 of the 720 (42%)
came from three module-decline witness landings** (screen 152, witness B2 104, witness cluster A 49) and **203 more
from the golden lane** (151 + 52). Strip the declines and the earned rate is **(720−305)/6 = 69 rows/day**. All
three proposals found this independently; they differed only on 256 vs 305, and ⓜ 305 is right.

**A2 — the root defect is a double derivation, and b2 just re-measured it.** Adjudication emits a verdict with no
evidence; the golden lane later re-reads the same rule and the same code to produce the evidence. The cause is eight
words in `wf_lane3_refute.js` COMMON: *"CONFORMS-but-untested (empty test-ref) is expected; do not write goldens
here."* The adjudicator writes and runs probe programs under `{OUT}/probe/<slug>/` and throws them away.
ⓜ b1: 184 rules adjudicated → GAP **−15 (8%)**. ⓜ **b2, landed today (`073d764a`): 292 rules → GAP 3054 → 3022,
Δ−32 (11%)** — and it left behind **84 new `kb/Work` notes** and took the CONFORMS-but-untested band from **9 to
55**. ⓜ Golden-lane round 2 closed 52 rows with 49 witnesses = **1.06 rows/witness**, against an existence proof of
**163 rows on one test-ref** and 29 on one program.

**A3 — the lander is the serial resource and it is spent on tiny landings.** ⓜ median landing = 14 rows.

**A4 — independent verification of a closing verdict must never be sampled away.** Every batch overturned some
CONFORMS, always downward. All three refuse to weaken it; they differ only on its *shape*.

**A5 — unlinked evidence is already in the tree.** ⓜ `tests/conformance/**/*.cob` = **1311 programs; 769 named by an
inventory `test-ref`, 542 named by none**; 374 of the 542 cite an ISO clause in their own header; **1398
un-adjudicated rows sit in a section an unlinked program names.**

**A6 — THE FACT NONE OF THE THREE COULD SEE, because it landed after they started.** ⓜ On this tree:
GAP **3022** · un-adjudicated **2517** · **adjudicated-but-open 505** (PARTIAL 233 · DIVERGES 110 ·
NOT-IMPLEMENTED 105 · CONFORMS-untested 55 · DNS-GAP 2) — the proposals all reasoned from **245**. ⓜ `kb/Work` holds
**249 open/half notes claiming 487 inventory rows** (1.96 rows/note) — they reasoned from ~110 actionable.
**Two adjudication batches (471 rules) opened 169 notes; the fix lane closed ~16 mechanisms in the same six days.**
The register is filling ~10× faster than it drains. **The binding constraint is no longer the evidence lane. It is
the fix lane, and it was already the fix lane before this review started.**

### 1.2 Where they disagree, and how I resolve it

| question | throughput | cost | risk | ⓜ my measurement | verdict |
|---|---|---|---|---|---|
| tokens/row today | 188 k | 9.66 M | 99 k | **9.66 M all-tokens** (5.62 B / 246 agents / 582 rows); ⓜ cache-read = **97.6%** of it | Three *accountings*, not three estimates. **Use all-tokens** — that is what the weekly limit consumes. Multiply by 0.024 for the harness-visible figure. |
| the turn-cost law | not seen | `0.115·T + 0.00031·T²` | not seen | ⓜ **`0.1154·T + 0.000309·T²`, n=239** — reproduced to 3 significant figures | **Cost's strongest finding, and it survives.** ⓜ 18 of 239 agents ran ≥250 turns and burned **39% of 5.62 B**; ⓜ re-modelled at a 150-turn cap the same turns cost **45% less** — 2.19 B → 1.21 B, ~1 B of a single window, free. |
| may one agent derive *and* witness? | yes + a bite check | yes + a pre-run expectation | yes, **only behind a blind replicate** | — | **Risk wins.** The bite check proves the witness *discriminates*; only the blind replicate proves the expectation is the **spec's**. Adopt all three guards; they are cheap and they cover different failures. |
| SR rows = one negative fixture each | asserted | asserted | disputed | ⓜ of 2517 un-adjudicated: **SR 970, of which 696 contain "shall"**; **GR 1210, only 234 do** | **Throughput is right for SR, risk is right for GR.** ~700 rows need their own refusal witness ≈ 1:1; ~1200 GR rows batch by subject (ⓜ 193 GR subjects, median 4 rows, top 38). Honest rows/witness ≈ **2.5–3.0**, not 3.5. |
| more module declines available? | assumed | assumed | **no — A.4.11 Report Writer is Partial with a shipped implementation** | ⓜ `CONFORMANCE.md` confirms | **Risk is right and it is decisive.** Plan for zero free declines. |
| the fleet-guard freeze | "correct the guard's derivation" | not raised | not raised | ⓜ the guard's header states the derivation is true **by construction**: a workflow `agent()` with no isolation genuinely **is** in the main checkout | **Throughput's fix is refused.** The guard is right; the fleet agents have no tree. Give them one (§7), and fail-closed stays untouched. |
| defect density in what remains | 35% | 15.6% | ~25% blended | ⓜ **§14 adjudicated non-DNS 718 rows at 0.43 CONFORMS**; §15 552 at 0.93; §13 n=51 at 1.00, §8 n=34 at 0.97, §12 n=38 at 0.97 — **all three small samples are of declined or definitional rows** | **All three are optimistic.** ⓜ b1+b2 measured **169 notes from 471 rules = 0.36 notes/rule**. §13 (700 rows) and §8 (412) are essentially unmeasured. This is the dominant uncertainty in the whole plan — 529 to 1155 mechanisms (§3). |
| existing evidence integrity | not raised | not raised | 80–88 positive-only SR rows | ⓜ **120** OK SR/AR CONFORMS rows whose `test-ref` looks positive-only, **89 of them in §15** (name-based heuristic; risk sampled two and one was a false alarm) | Real, and bigger than reported. Must be swept before zero GAP is claimed — owner question 6. |

---

## 2. The recommendation

### 2.0 The owner's own lever, evaluated first — and it is right about *where*, wrong about *which*

> *"maybe do more fixing and/or more implementations per regression test"* — amortize every gate over more landed work.

Priced with the ⓜ-reproduced law `cost(M) = 0.1154·T + 0.000309·T²` (`syn_model.py`), at turn budgets anchored on
measured agents (implementer: ⓜ PB383 235 turns / 37.2 M and PB386 307 / 76.6 M for **one** mechanism each; lander:
ⓜ PB383 86 turns / 7.2 M, PB386 94 / 8.0 M, golden-lane round 1 249 / 52.9 M for **151 rows**):

**(a) More mechanisms per implementer — REJECTED, and this is the counter-intuitive result.**

| mechanisms in one implementer | turns | M tokens | **M per mechanism** | min per mechanism |
|---|---|---|---|---|
| 1 | 220 | 40.3 | **40.3** | 29 |
| 2 | 370 | 85.0 | **42.5** | 24 |
| 3 | 520 | 143.6 | **47.9** | 23 |
| 4 | 670 | 216.0 | **54.0** | 22 |

A mechanism costs ~150 turns; the fixed cost of an implementer is only ~70. The quadratic therefore eats the
amortization immediately: **the second mechanism in the same transcript costs 44.6 M, more than a whole fresh
implementer at 40.3 M.** Batching buys ~20% of wall clock and loses 5–34% of tokens — and ⓜ tokens are the binding
resource (three weekly-limit pauses in six days). **Recommend 1 mechanism per implementer** — 2 only when the two
notes share one code site *and* one gate filter, in which case the register's own clustering rule says they were one
mechanism to begin with. ⓜ The same law condemns the multi-landing lander: lander-3 carried 4 landings in 794 turns
for **295.9 M**; the same turns split into 4 fresh 198-turn landers model at **140 M — 53% less**.

**(b) More clusters per landing — ADOPTED, and it is the real win the owner is reaching for.**

| clusters in one landing | turns | M tokens | **M per cluster** | **min per cluster** |
|---|---|---|---|---|
| 1 | 75 | 10.4 | **10.4** | 9.8 |
| 3 | 115 | 17.4 | **5.8** | 5.0 |
| **5** | **155** | **25.3** | **5.1** | **4.0** |
| 8 | 215 | 39.1 | 4.9 | 3.5 |

A landing is ~90% fixed cost — bring the work in, build, gate, DEVLOG, commit, push — and ⓜ the corpus proves it:
the golden lander landed **151 rows for 52.9 M = 0.35 M/row**, while the PB383 lander landed **2 rows for 7.2 M =
3.6 M/row**, a **10× amortization**. **Recommend 4–6 clusters per landing (target 5), hard ≤160 turns**; past 6 the
curve is flat and the gate-attribution risk is not. Worth **2.0× on landing tokens and 2.4× on lander wall clock**,
and it is the only one of the three that is free of risk to the definition of DONE.

**(c) More landings per battery — ALREADY PAST ITS OPTIMUM; change *where* it runs, not *how often*.**
ⓜ Battery #42 covered **20 landings**: Conformance 5515 in 8 m 25 s, Unit 5293 in 2 m 9 s, characterization 2 s,
plus phase 2 (`guard-fast`, **which rebuilds**) and the 1323-case differential ≈ 45 min machine time. Its two reds
were both attributed **by inspection** (seven differential flips all carrying `COBOLNET1560` from one landing; one
manufactured citation red) — **no bisect was needed, in either of the last two non-green batteries.** A bisect over N
landings costs ⌈log₂N⌉ battery runs: N=10 → 3.3 runs, N=20 → 4.3 runs, i.e. batching 20 instead of 10 risks ~45 min
of extra machine time in a rare case to save 45 min in every case. **Keep the cadence at one battery per accumulated
batch (~10–15 landings)** and instead remove its real cost: **phase 2 rebuilds, so a battery on main freezes the
lander for ~45 min ≈ 1.5 landings ≈ 7 clusters.** Run it in a worktree cut at the batch head and that cost goes to
zero.

**(d) The finding that dwarfs all three.** ⓜ Three implementers × 6 dispatch-hours ÷ 45 min per mechanism = **~24
mechanisms/day of capacity. The measured fix lane delivered ~16 mechanisms in six days — under 15% utilization**,
because implementers were dispatched one cluster at a time, behind landings, behind the evidence lane. **The owner's
lever is aimed at exactly the right constraint and would buy 2×; filling the slots he already authorized buys 6×.**
The fix lane needs a standing queue of apply-ready contracts and a dispatch rule — *a free implementer slot is
filled within the turn it frees* — far more than it needs bigger implementers.

### 2.1 The pipeline, step by step

Name: **one derivation per rule.** A rule is read once, by one agent, which produces the verdict *and* the evidence
in the same pass; everything downstream either checks it mechanically or re-derives it blind.

**S0 — Dossier + candidate binder — script, 0 tokens/row** *(cost's S0 + throughput's dossier additions)*
`phase_b_batch.py` gains per row: the **shape class**, the **parent stem** for sublist rules, the subject's existing
`test-ref`s, and the **candidate evidence set** — the ⓜ 542 unlinked corpus programs joined by header clause (ⓜ 1398
rows have one), the negative fixtures joined through `DIAGNOSTICS.md`, the `.g4` rule, code sites, owning notes.
*Guard:* the existing *"the dossier is a MAP; its silence is not evidence"* warning extends verbatim to `candidates`.

**S1 — Link pass — medium effort, 4-wide, ~0.5 M/row** *(cost's S1 / throughput's L0)*
One agent per ~30 candidate rows: read the rule, read **one** candidate program, confirm one code site, decide
*pins / does not pin*. A rejected candidate **falls through to S2** — it never closes and never disappears.
*Guards (risk):* every link needs (a) a written spec derivation of the expected output, (b) an explicit
**counterfactual**, (c) the **rule-id assertion anchor added to the target golden's `.out`**. A mismatch between the
derivation and the landed `.out` is a **DIVERGES finding, never a re-baseline** — DESIGN §0 records that this
corpus's expectations were largely legacy-derived, so **this lane is expected to produce defects.**

**S2 — Derive → predict → witness → run — full effort, 4-wide, ≤160 turns, 10–14 rules/agent, ~2.6 M/row** *(all three)*
Per rule, in this order, and **the order is the guard**:
1. Read the rule and its general format; **enumerate its branches**.
2. Locate and read the implementing code.
3. **Write the expectation down before the compiler runs** — `expected_from_spec` prose plus the exact expected
   `.out` lines (or `.err` diagnostic), appended to `predict-<slug>.jsonl` as its own record, **before any run record**.
4. Author the witness **at its repo destination path** with its manifest entry, a unique `PROGRAM-ID`, the
   `*> reject-at:` first line for a negative, and **one labelled assertion line per claimed rule-id**. Batch a
   subject's GRs into one positive program; pair each constraint SR with its **legal complement**. Declare the
   **bite**: the single edit that must change the result.
5. Run the pinned `cobol.exe` **batched, one turn per subject** via `probe_runner.py` — not ⓜ ~11 tool uses per row.
   **The verdict falls out**: match → CONFORMS *with a test-ref, closable*; mismatch → PARTIAL/DIVERGES *with the
   repro already written at a corpus path*; refuses legal source → NOT-IMPLEMENTED, same.
6. Checkpoint the witness to its destination path the moment it passes.
*Guards:* predict-before-run (risk + cost), the bite (throughput), the legal complement (cost), ≤160 turns (cost, ⓜ).

**S3 — Mechanical witness gate — script, 0 tokens/row** *(cost's S3 + throughput's `validate_witnesses.py`/`bite_check.py`)*
`witness_gate.py` compiles and runs every draft on the pin and refuses a row failing any of: pre-run expectation ≡
observed · pair discrimination (the complement compiles clean **and** the violating member is refused, or the two
outputs differ) · the negative's `.err` names its diagnostic · the declared bite changes the result · the golden
**appears by name in the runner log**. Agents are dispatched only to failures.
*Guard on the guard:* `--self-test` fires its failure branch on a hand-broken pair before any run
(`green_gates_arent_evidence`).

**S4 — Blind expectation replicate — full effort, 100% of closing rows, ~0.4 M/row** *(risk — the single most
important structural change)*
A second agent gets **only** the rule text, its general format, and a **copy** of the `.cob` in its own directory:
no dossier, no code location, no pinned compiler, no `predict-*.jsonl`, **no sight of the observed output**. It
returns the expected `.out`; the workflow compares mechanically. This **replaces both** today's CONFORMS refuter and
the golden-lane refuter — strictly cheaper (no code read, no probing) and strictly stronger, because today's refuter
re-reads the same code the adjudicator read and can only agree with it.
*Guard:* the disagreement rate is reported per writer and must sit in **[3%, 25%]** — below 3% means blindness
leaked, above 25% means drafting is broken. Default-refute on uncertainty and the all-upheld red flag both stay.

**S5 — Registrar + lander, one landing per batch, ≥30 rows** — ~0.5 M/row *(throughput + cost)*
Merge records; cluster findings **by mechanism** into `kb/Work/`; copy drafts to destinations; register manifests;
`record_verdicts.py`; gate; DEVLOG; push. **One commit per subject inside the landing** so a red bisects. **The
batch's rows close on the batch's own landing**; a non-zero CONFORMS-untested band becomes a process alarm.
*Guards:* `record_verdicts.py` prints `rows_in / rows_applied / rows_dropped` **with the dropped rule-ids
enumerated** (risk — a shrinking population can never be silent); the registrar reconciles `read == decided +
|deferred|` with the deferred ids listed.

**FMT route — 164 rows, ~1.2 M/row** *(cost's route, risk's gate)* — `.claude/skills/spec-grammar-conformance`
against the **rendered PDF page**, converted by the existing `grammar_findings_to_batch.py`.
*Guards:* the **rendered page number and one line of what the render showed are required record fields** (the OCR'd
diagrams are lossy toward falsely-restrictive syntax), and an FMT row still closes on an **executable** test.

**DOC route — 173 rows, ~2.0 M/row** *(cost + throughput)* — `join_doc_rows.py` parses each row's governing-clause
parenthetical to its governing rule-id so the §7 determination reuses that rule's witness.
*Guard:* a mandatory **Constraint** cell — the governing clause with a `cite.py --check` line, or the literal
`none (A.1 item N imposes no constraint)`; an empty one fails.

**S6 — Fix lane — the constraint; run it FULL, not BIG** *(§2.0(a)+(d))*
**One mechanism per implementer**, fresh from an apply-ready contract that **carries the runnable repro S2 already
wrote at a corpus path** — the brief's opening move becomes *"rerun the repro at `<path>`"*, one turn, not the ~25
that `RE-PROBE EVERY MECHANISM` costs. **Hard ≤220 turns**; a mechanism that will not fit is split, never extended.
**Every implementer slot stays full**: a standing queue of contracts, ordered by `work.py next`, dispatched the turn
a slot frees. Their patches are landed **5 to a landing**.

---

## 3. The numbers

| | today (ⓜ measured) | recommended | basis |
|---|---|---|---|
| rows/day | **69 earned** (120 headline, 42% decline-carried) | **110** at the present 3-implementer cap · **165** at 6 | §3 derivation |
| all-in all-tokens per closed row | **17.5 M** | **9.6 M** | total ÷ 3022 |
| harness-visible tokens/row (ⓜ 2.4% of all-tokens) | ~420 k | ~230 k | same ratio |
| rows per landing | median **14** | **≥30** evidence · **5 clusters** fix | §2.0(b) |
| rows per witness | **1.06** | **2.5–3.0** | ⓜ 696 SR rows are ~1:1; 1210 GR rows batch by subject |
| fix-lane utilization | **<15%** of the 3-implementer cap | **>85%** | §2.0(d) |
| working days to zero GAP | **60+** | **28** at cap 3 · **18** at cap 6 | §3 derivation |
| calendar | — | **5–9 weeks** at the ⓜ observed ~50% duty cycle | three pauses in six days |
| total all-tokens remaining | **~53 B** | **~29 B** | below |

**Derivation.** *Evidence lane, 2517 rows:* S2 2.6 + S4 0.4 + S5 0.5 = **3.5 M/row → 8.8 B** (band 7–9 B; the model
says 3.0 M/row for S2 at 10–14 rules/agent, the measured b2 adjudicators say 1.4 M/row before witness authoring, and
witness authoring adds ~5 turns/rule). *Fix lane:* ⓜ b1+b2 yielded **0.36 notes per adjudicated rule**; applying
that to §14's remaining 498 and a blended 0.15 elsewhere gives **482 new + 249 open = 731 mechanisms × 27 M
(≤220-turn implementer with the repro pre-written) = 19.7 B**. *Landings:* ~150 at 25 M = 1.0 B less the k=5
amortization. *Batteries:* ~6 × 20 M attribution = 0.1 B. **Total ≈ 29 B.**
*Today's process straight-lined to zero GAP:* evidence paid twice (adjudication 1.4 + golden lane 5.5 + registrar
0.9 = 7.8 M/row → 19.6 B) + fix at ⓜ 40.3 M/mechanism uncapped (29.5 B) + ~365 two-cluster landings (3.8 B) =
**~53 B**. **1.8×.**
*Calendar:* the evidence lane is ⓜ fast — b2 adjudicated 295 rules in ~4–5 h at 4-wide; witness-first at ~1.5×
puts 2517 rows at ~60 fleet-hours ≈ **10 working days** owning 4 slots. The tail is the fix lane: 731 mechanisms ×
45 min = 548 agent-hours = **30 working days at 3-wide, 15 at 6-wide**, overlapping the evidence lane ⇒ **28 / 18**.

**The softest number, named: the note yield outside §14.** ⓜ It is measured at 0.36/rule on §14 statements and
**unmeasured on §13 (700 rows) and §8 (412)** — the only samples there are 51 and 34 rows of declined or
definitional content. At 0.05 the fix lane is 529 mechanisms / 14.3 B / **22 working days**; at 0.36 it is 1155 /
31.2 B / **40 days**. Nothing else in this plan moves the answer that far. **b3 is data-division clauses (§13.18) —
it measures this directly, and the pilot must report it as a first-class number.**

**A second-order risk, stated plainly:** witness-first *raises* the measured defect rate, because a witness that
runs finds what a code read misses. That is CLAUDE.md rules 3 and 4 working as designed, and it lengthens the tail.

---

## 4. What STOPS

Each with the measurement that condemns it.

1. **STOP producing "CONFORMS but no test" as a normal outcome.** Delete the eight words from
   `wf_lane3_refute.js` COMMON. ⓜ b1 8%, b2 11%; the untested band went 9 → 55 in one landing.
2. **STOP the golden lane as a separate lane.** It exists only because adjudication produces no evidence.
3. **STOP throwing away the adjudicator's probe programs.** They are witnesses minus a header comment.
4. **STOP any agent exceeding 160 turns for read-only work / 220 for an implementer.** ⓜ 18 of 239 agents ran ≥250
   turns and burned **39% of 5.62 B**; ⓜ re-split at 150 turns the same turns cost **45% less**.
5. **STOP carrying more than one landing per lander transcript.** ⓜ lander-3: 4 landings, 794 turns, **295.9 M** —
   modelled as 4 fresh landers, **140 M**.
6. **STOP batching mechanisms into one implementer** (§2.0(a)) — the second mechanism costs more than a fresh agent.
7. **STOP leaving implementer slots empty.** ⓜ <15% utilization of a cap the owner already authorized.
8. **STOP spending the lander on 1–2 cluster landings.** ⓜ 10.4 M/cluster at k=1 vs 5.1 M at k=5.
9. **STOP using an Opus agent to compile, run and diff drafts.** That is `witness_gate.py`.
10. **STOP refuting a CONFORMS by re-reading the code.** Re-derive from the rule text, blind, and run the witness.
11. **STOP `RE-PROBE EVERY MECHANISM` as an implementer's opening move** — the contract carries a runnable repro.
12. **STOP routing FMT (164) and DOC (173) rows through semantic adjudication** — both have cheaper built tooling.
13. **STOP re-fleeting the four-lens review.** ~1000 agents ≈ 4 B for 27 rows, and several register notes were
    defects the fleets introduced. The four lenses stay as step 6 of the implementer brief, on the agent's own diff.
14. **STOP publishing one burn-down rate.** ⓜ 42% of the window was declines; the flattering number mis-plans 3022.
15. **STOP treating `spec-rule-catalog.json` as a fixed denominator.** ⓜ generated 2026-08-08; no gate re-runs
    `extract_rule_catalog.py`, so a rule PB385's transcription repair adds can never appear as a GAP.
16. **STOP planning on another module decline.** ⓜ A.4.11 Report Writer is Partial with a shipped implementation.
17. **STOP running the battery on main** (phase 2 rebuilds — ⓜ ~45 min of frozen lander) — run it in a worktree at
    the batch head.
18. **STOP running read-only fleets without a tree of their own.** Not by loosening `fleet_active_build.py` — ⓜ its
    derivation is correct by construction — but by giving the fleet's agents an isolated worktree at the pin.

**Explicitly NOT stopped**, and any proposal to stop them is refused: independent verification on **100%** of
closing rows (never sampled — every batch so far overturned some, always downward); per-rule checkpointing to disk;
the pinned read-only worktree; one comprehensive battery per accumulated batch; the ledger refresh after every
GAP-moving landing; the **~8-agent concurrency budget as a default** (it is an error-containment cap, and ⓜ tokens —
not slots — are what the weekly limit rations); and full effort on every derivation, witness authoring and blind
replicate.

---

## 5. The pilot

Two stages. **Stage 0 costs nothing extra** — every input is work already owed — and exists to fire each new guard's
failure branch before any fan-out. **Stage 1 is the A/B that decides adoption.**

### Stage 0 — b2's own output, both lanes, ~0.2 B, one day
- **Evidence half:** b2's ⓜ **55 CONFORMS-untested rows** get their witnesses under the S2 contract (predict line,
  destination path, assertion anchor, bite) instead of through a golden round. This is the cheapest possible test of
  witness-first because the verdicts already exist.
- **Fix half — the owner's lever, measured directly:** b2's ⓜ **84 new notes** feed **three concurrent
  one-mechanism implementers kept continuously full**, landed **5 clusters per landing**. Report mechanisms/day,
  tokens/mechanism, and lander minutes per cluster against §2.0's predictions (24/day, 27 M, 4.0 min).
- **Fire every guard's failure branch first:** seed one characterization-shaped witness (`.out` copied from the
  compiler, no predict line) and confirm `witness_gate.py` refuses it; seed one record claiming a rule-id absent
  from its `.out` and confirm the anchor drift test goes red; hand the blind replicate one knowingly-wrong `.out`
  and confirm it disagrees. **A guard only ever observed passing has not been observed.**

### Stage 1 — lane-3 batch **b4**, A/B in one landing, ~0.9 B, ~2 working days, ≤8 agents throughout
- **Treatment: 12 subjects** through S0→S5 (witness-first, ≤160 turns, blind replicate).
- **Control: 5 subjects** through today's adjudicate-then-golden process — matched on rule count, **same day, same
  pin, one registrar landing**, so the GAP delta is attributable per arm. The control costs nothing extra and
  answers the *"the population was just easier"* objection a before/after cannot.
- **b3 runs unchanged in the meantime and is reported as a second, historical control** — and, because it is
  §13.18 data-division clauses, **b3 is also the measurement of the note yield outside §14** (§3's softest number).

### The success metric — pre-registered, mechanical, read off `record_verdicts.py`'s own output
**PRIMARY (adopt/reject):** rows closed **in the batch's own landing** ÷ rules adjudicated — **treatment ≥ 0.45**;
control expected ≤ 0.15 (ⓜ b1 0.08, b2 0.11).
**SECONDARY:** all-tokens per closed row in treatment **≤ 4.0 M** (ⓜ baseline 9.66 M). The control's figure is
recorded as **debt**, since its CONFORMS-untested rows still owe a golden round charged back to it when paid.
**LEVER METRIC (Stage 0):** mechanisms landed per day **≥ 15** (ⓜ baseline ~2.7) at **≤ 30 M/mechanism** and
**≤ 5 min of lander per cluster** (ⓜ baseline 9.8).
**INTEGRITY GATES — every one must hold or the pilot FAILS even if the primary passes:**
1. Blind-replicate disagreement in **[3%, 25%]**, reported per writer with direction; below 3% ⇒ blindness leaked
   and the arm is re-run against a fresh replicate seeded with one knowingly-wrong witness.
2. `witness_gate.py` **rejects ≥1 real draft**, its failure branch was fired deliberately in Stage 0, and **zero**
   rows close with a pre-run expectation differing from the landed `.out`.
3. **Every rule not closed is listed individually by rule-id with its reason.** No summary counts. **A silent cap is
   a failed pilot.**
4. **Zero rows closed on non-spec-derived evidence**; every closed row carries its `expected_from_spec` paragraph, a
   `cite.py --check`-ed citation, and its **own assertion anchor** in the `.out`.
5. `work.py check` clean; `SpecTraceabilityInventory`, `DefectiveRowCoverage`, `DerivedVerdict` and the new anchor
   test green; semgrep baseline not increased; comprehensive battery green at the batch close — **run in a worktree**.
6. Turns per agent reported — the leading indicator that S2 is drifting into the quadratic.

**KILL CRITERION:** treatment's closed/adjudicated **< 0.30**, **or** the gate refuses **>15%** of witnesses, **or**
the disagreement rate is **<3%** *and* a manual re-derivation of five random landed witnesses cannot reproduce their
`.out` from the standard alone. Then revert to the current pipeline for b5 and report why — **do not iterate on a
failed shape inside the same batch.**

**Cost: ~1.1 B all-tokens, ~3 working days, inside the 8-agent budget throughout.** ⓜ ~40% of it is owed regardless
(b2's 55 witnesses, b2's 84 notes, b4's adjudication, the control arm). The genuinely additional spend is the
tooling landing — `phase_b_batch.py` candidates + `probe_runner.py` + `witness_gate.py` ≈ 0.05 B — and the
duplicated derivation in the control arm.

---

## 6. Owner decisions — bare questions, in blocking order

**Blocking Stage 0:**

1. May the agent that assigns a row's verdict also author that row's witness, given a mechanically-checked pre-run
   predicted expectation and an independent blind replicate that never sees the observed output?
2. May every agent be hard-capped — 160 turns read-only, 220 for an implementer — splitting the work rather than the
   turn budget?
3. May one lander carry five clusters in one landing, with one commit per cluster inside it?

**Blocking the fan-out after the pilot:**

4. May the implementer cap rise from 3 to 6, accepting that this spends the weekly token budget faster in exchange
   for roughly halving the fix-lane calendar?
5. May one table test — a `conformance-test:`/`unit:` test with one `InlineData` row per rule-id — close many
   inventory rows, bound to the inventory by a two-way drift test?
6. May effort drop to medium on dossier generation, the link pass, validation triage and the FMT/DOC routes, while
   derivation, witness authoring and the blind replicate stay at full?

**Blocking the definition of DONE:**

7. Must the ⓜ 120 already-OK SR/AR "shall" rows that carry no rejection evidence be re-opened and re-witnessed
   before zero GAP is claimed?
8. Do you confirm A.4.11 Report Writer stays claimed — its ~230 rows adjudicated on their merits, with no further
   module decline available to the burn-down?
9. Does PB386's derivation arm extend, class by class with you signing each class, to the cross-reference and
   definitional rules a mechanical classifier flags — with the drift test refusing a derivation on any row that has
   an observable obligation?

---

## 7. What adoption changes, by path

**Workflow templates**
- `.claude/skills/workstream/templates/wf_lane3_refute.js` → **`wf_witness.js`** — delete *"do not write goldens
  here"*; add the witness contract (destination paths, manifest entry, `*> reject-at:` first line, per-batch
  `PROGRAM-ID` prefix, one positive per GR subject, one negative **pair** per constraint SR, the bite field, the
  predict-before-run ordering, branch enumeration); stages become **Derive+Witness (4-wide, ≤160 turns) → Validate
  (script) → Blind replicate (agent with no `${PIN}`, no dossier path, no observed output)**; every stage schema
  gains `read` / `decided` / `deferred[]`.
- `.claude/skills/workstream/templates/wf_lane1_draft.js`, `wf_lane1_validate.js` — retired as a lane; the validate
  stage becomes `witness_gate.py`.
- **NEW** `wf_link.js` (S1 over the ⓜ 542 unlinked programs) · `wf_fmt.js` (PDF-render-gated) · `wf_doc.js` (joined
  DOC rows) · **`lander-train-brief.md`** (five clusters, one build, one union filter, one commit per cluster,
  bisect on red).
- The read-only fleet's agents are dispatched **into an isolated worktree at the pin** so `fleet_active_build.py`
  can derive their tree; if the workflow `agent()` cannot take isolation, the fallback is explicit and costed: the
  next batch's pin is built **between** fleets, one serialization point per batch.

**Briefs**
- `registrar-brief.md` — absorbs the golden-lander steps; note-writers take the repro from the corpus path S2 wrote;
  reports rows/witness, blind-replicate disagreement rate, tokens per closed row, and `read == decided + deferred`
  with the deferred ids listed.
- `golden-lander-brief.md` — reduced to the procedure for goldens shipping with a landed fix; states that a non-zero
  CONFORMS-untested band is a process alarm.
- `implementer-brief.md` — **one mechanism**, hard ≤220 turns with a mandatory split instruction; *"rerun the repro
  at `<path>`"* replaces *"RE-PROBE EVERY MECHANISM"*.
- `lander-brief.md` — five clusters per landing; one commit per cluster; **one landing per transcript**.

**Scripts**
- `scripts/spec/phase_b_batch.py` — per-row `shape`, `parent_stem`, existing `test-ref`s, `candidates`; MAP warning
  extended to `candidates`.
- **NEW** `scripts/spec/probe_runner.py` — compile+run a directory of probes in **one** turn on the pin, appending
  the per-rule checkpoint lines itself.
- **NEW** `scripts/spec/witness_gate.py` — pre-run expectation ≡ observed · pair discrimination · the negative's
  `.err` names its diagnostic · the declared bite changes the result · the golden appears by name in the runner log;
  ships with `--self-test`.
- **NEW** `scripts/spec/join_doc_rows.py` — governing-clause parenthetical → governing rule-id, with
  `audit_annex_a1.py`'s withdrawn/optional splits.
- `scripts/spec/merge_batch.py` — refuse a witness-closing record with no predict line, or whose landed `.out` does
  not hash-match its predict line absent a re-derivation citing a **different** clause or ordinal.
- `scripts/spec/record_verdicts.py` — `evidence-kind` (`positive|negative|both`) and `branch`; **refuse a CONFORMS
  record for an SR/AR "shall" rule whose evidence is positive-only**; print `rows_in / rows_applied / rows_dropped`
  **with the dropped rule-ids**.
- `scripts/spec/gen_ledger.py` — split the trend into decline-witness rows vs per-rule-witness rows, and publish the
  **fix-lane fill-vs-drain** pair (ⓜ 169 notes opened / ~16 mechanisms closed in six days) beside the GAP series.
- `scripts/spec/audit_annex_a1.py` — mandatory Constraint cell; fail an empty one.
- `scripts/battery.sh` — document and default to running from a worktree cut at the batch head.

**Tests**
- **NEW** `RuleAssertionAnchorDriftTests` — every rule-id a row claims appears as an assertion anchor in its
  golden's `.out` (or the negative's `.err` expectation). ⓜ only 4 of 390 `tests/conformance/2023/*.out` satisfy
  this today, so land it **forward-only** from the pilot, with the existing rows swept as a measured backlog
  (owner question 7).
- **NEW** `InventoryTableCoverageDriftTests` — two-way binding between a table test's rows and the inventory.
  **Owner decision 5 is refused without this test.**
- `SpecTraceabilityInventoryDriftTests` — regenerate `spec-rule-catalog.json` from the pinned `specs` submodule and
  compare ids/sections/ordinals, so the denominator is **derived, not asserted**.

**Skill and docs**
- `.claude/skills/workstream/SKILL.md` §2 — record the ⓜ law `cost(M) = 0.115·T + 0.00031·T²` and the turn caps as
  the **reason** for the budget, replacing the anecdote; **one landing per lander transcript, one mechanism per
  implementer, five clusters per landing**; §3 — "never spend the lander on <30 rows outside a fix train", "run the
  battery in a worktree", "a free implementer slot is filled within the turn it frees", and the effort-tier rule:
  **effort may be reduced only on a step whose output a non-reduced step checks.**
- `docs/rearchitecture/DESIGN-spec-conformance-review.md` — §5.1/§9: the lane table becomes S0–S6 plus the shape
  routes; §9's "what a batch prompt MUST carry" gains the pre-run expectation, the witness pair and the branch
  enumeration. **§1 is not weakened** — add two mechanical readings of §1(c): *"covers it"* means the rule-id has its
  own assertion anchor, and a rejection obligation needs rejection evidence.
- `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 — live state only; the split burn-down rate replaces the single headline.
- `kb/Work/` — one `kind: decision` note for the pipeline change and one `kind: analysis` note carrying this
  measurement. **No new list, table, tracker or "remaining work" section anywhere** (CLAUDE.md rule 8).
