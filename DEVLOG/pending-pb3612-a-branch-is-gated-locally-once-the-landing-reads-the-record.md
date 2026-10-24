## A branch is gated locally once, at its head; the landing reads the record and gates nothing (PB3612)

kb/Work PB3612 (owner decision 2026-10-23 ~20:40 PDT, after "Taking one hour to finish a branch is stupidly ridiculously
slow … Developers merge separate branches into a continually moving main all the time without problems. Maybe the rare
merge conflict, which they address. Why don't we do that?", then "yes"). Measured first: PB3140's landing ran THREE local
gate runs (4,615 s) because main moved twice while it waited for a gate slot, each run a Windows leg 1 plus all four
Linux legs; five branches took about an hour each from CLEAN to queued; `landing_measure.py` put the lane at 1.47 changes
per hour. The CI-invariant practice of 2026-10-04 (every landing re-gates the merge result on both hosts) is replaced by:
the implementer gates the head once, on both hosts; the merge with main is the merge queue's to test.

**Re-probe.** The note is a decision, so the probe is the code it names: `enqueue.py land` ran `run_gates` (Windows
`build-local` plus a Linux row, `linux-gate.sh --nice`, the four whole legs, under `gate_slot.py run`) on the merged head,
and re-gated whenever the branch's head differed from its `enqueue-gated` mark by anything but main's commits. All of that
is gone.

**Mechanism 1 — the Linux half of the targeted leg runs INSIDE the implementer's gate** (`run_gate_legs.py`,
`scripts/linux-gate.sh`; DESIGN-test-build-ci.md §3.17.3, the item PB3022 left open). After a green Windows half, under
the slot the gate still holds, `Gate._linux_leg` starts `bash scripts/linux-gate.sh --legs targeted --plan <run>/plan.json
--plan-legs <n>`: a WSL clone of the committed HEAD, built once, running the driver's own `--plan-leg` (`plan_leg`) on the
SAME plan's legs through `Gate._run_leg`, the population check and the identity check against that clone's listing and
binaries (a case the plan never heard of runs in leg 1; the plan is copied beside the results so the Windows run's
identity records are not overwritten). No audits, no slot of its own, no inert classification (the Windows half decided
the lane; the inert lane runs no Linux half). A red Linux half is a RED gate naming the host; one that measured nothing
(no WSL, no SDK, a full disk) ends the gate NOT RUN with the Linux gate's own cause, classified once in
`NOT_RUN_CAUSES`. The verdict file gains `linux` (verdict, cause, the committed `tree` it measured) and `head_tree`; a
reused answer needs a GREEN Linux half of this head's tree. The Linux results land in `TestResults/linux-gate/`, where
`gate_plan.previous_gates` already reads them as tier 0a. `linux-gate.sh` keeps its whole-leg form as the tool that
reproduces a Linux CI red.

**Mechanism 2 — `enqueue.py land` gates nothing; it reads the record.** `tested_locally` reads the worktree's gate runs and
refuses (exit 13, UNTESTED, before anything is merged; the report gains `BLOCKED: enqueue — no local gate is recorded GREEN
…`) a tip no run covers: GREEN with exit 0, measured the committed head's own tree (`tree` equals `head_tree`: an
implementer who gated uncommitted edits tested something no commit carries), a GREEN Linux half of that tree on a host
that runs one, and a head that IS the tip or is followed only by main's commits, clean merges and DEVLOG-only commits
(`prune_worktrees.unreviewed_since`, the review's coverage rule). The one exception, the case a developer would also
re-test: when bringing main in changed a file the branch also changes (`merge_overlap`; files a merge driver merges by
key and files no test reads are set aside), the one gate runs once on the merged head. The lanes MANDATORY-PRACTICES I10
does not review write no gated report, so with no record the landing gates their tip once itself. Deleted, not wrapped:
the Linux gate row, `SLOT_WRAP` and `gate_argv` (no row runs outside the driver's slot any more), `GATED_MARK` and
`gated_covers`. The self-test arms that drove the gate through those paths now drive the lane/overlap path; new arms: an
untested tip, the evidence rules (a later RED does not unrecord a GREEN; an uncommitted tree, no Linux half, a non-zero
exit and a foreign head do), coverage after main's commits, an overlap that runs one gate and a disjoint merge that runs
none, the set-aside files, main moving while the pull request is queued. The land lane and the loop's exit table treat 13
as a STOP for a finisher.

**Mechanism 3 — measurement.** `landing_measure.py` records `landing_gate_runs` (0 or 1; null before the rule, said
UNMEASURED) and `summary` prints a row for it beside PB3140's 3 runs; "the first five landings under the rule" are read
with `summary --since`.

**Mechanism 4 — docs, same change set.** MANDATORY-PRACTICES I1, I8, L1, L2, L10, L12; the gate skill; the fix-lane and
implementer briefs and the dispatch-spec template; `make_dispatch_specs.LINUX_GATE` and its pins; the finisher brief and
the enqueue unit (exit 13); the agent definition; DESIGN-test-build-ci.md §3.15, §3.17.2, §3.17.3, §3.18;
DESIGN-orchestrator-loop.md; CLAUDE.md's Testing paragraph (owner-owned text, edited to the decision's words); README and
DOC_INDEX. `LinuxGateDriftTests` gains a test that the driver's Linux command and the script's targeted mode spell their
arguments the same; `LandingPipelineDriftTests` lists the new arms.

**Deliberate difference from the contract.** The overlap exception runs the ONE gate (Windows leg 1 and its Linux half),
not a Windows-only leg: the gate has one shape (PB3022), and a Windows-only flag would be the mode PB3022 deleted.

**Goldens.** None: gate and landing tooling, no compiler behaviour. Rows: none (`closes_rows: []`, PB3612 landed;
PB3022's item 10 recorded as shipped).

**Gate.** Run `TestResults/build-local/000004-20261024T201417Z` at 53e24a6ec (the first gate, 000001, was RED in the
CITATIONS audit: the note named DESIGN sections with the section sign; 000002 at edf1cc6a1 was GREEN before two
self-review fixes; 000003 was stopped on purpose, because a note edit had to precede the last gate):
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … Conformance 10,112/18,394 · Unit 36,910/38,134 ·
Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) · Linux half (WSL clone of HEAD) GREEN · slot 2 of 3 …`.
The Linux half is real, end to end: `leg targeted: GREEN in 77s — === PLAN LEG: GREEN — leg(s) 1 of plan d57b467a363a:
Conformance 10,112/18,394 · Unit 36,910/38,134 · Characterization 5/36 cases ran ===`, the inert watch GREEN, and
`enqueue.tested_locally` reads that record as covering the tip. The new `LinuxGateDriftTests` test ran in leg 1.

**Self-review.** Architecture: one reader of the gate record (`tested_locally`), one Linux runner (the driver's
`--plan-leg`), the dead slot-wrapping machinery deleted. Full code: found and fixed an unreadable side of the merge read as
"no overlap" (now a gate), and a silent copy of the Linux results (now loud). Performance: the Linux half adds the clone
and build (46 s) and the leg (59-77 s) to a gate the implementer already runs, and removes three whole gates (about 4,600 s)
from a landing. Duplication: the Linux half reuses `Gate._run_leg`, `_check_populations` and `_check_identity`.

**Branch review round 1 (finisher).** R1-1 (blocking): the one gate for a merge that changed a file the branch also
changes was decided by comparing HEAD after this run's `merge_main` with the head the run started at, so a re-run after
a run that had made the overlapping merge commit (its gate BUSY, NOT RUN for an environment cause, or the process died)
started at that merge commit, saw no merge, and queued the pull request with the shared-file merge never tested. Fixed at
the root: `tested_locally` now returns the head its covering record measured (`Tested.gated`), and `land` reads the
overlap against that head, so the decision no longer depends on how many runs it took. New self-test arm: a first run
whose gate is BUSY leaves the merge commit and exits 12; the re-run still runs the one gate and records one. R1-2 (lead):
the section 7d comment of `test_plan_wave.py` named the landing's Linux gate; it now says the targeted Linux half is in
the implementer's own gate.
