## Pipeline step 8: the landing gates nothing, waits for the gate lock, and every merge-queue red gets a class (PB4331, PB4204, PB4271); step 11 begun: CLAUDE.md Testing states the new rule (PB4334)

Step 8 of the pipeline redesign (kb/Work PB4323, PIPELINE-REVIEW.md §10 and §5.1), one implementer for the three files
that share it (`enqueue.py`, `land_lane.py`, `landing_measure.py`).

**Re-probe.** On main 8fd4ad387 `enqueue.py land` still carried the merged-head re-gate: `merge_overlap` picked the
files main and the branch both changed, `gate_once` ran `build-local` on the merged head through `run_gates`, a NOT RUN
was measured on main by `gate_main`, and a lane I10 does not review had its tip gated by the landing. `tested_locally`
carried `running`, consulted only on the not-ok path, so an older GREEN record let a landing write a worktree under a
running gate (PB4204's 2026-10-31 case). The landing lane ended every merge-queue bounce as BOUNCED for a finisher with
no class (PB4271). Baseline: `enqueue.py --self-test` 74 arms PASS.

**The landing gates nothing.** The re-gate arm and all its machinery are deleted (`merge_overlap`, `gate_once`,
`run_gates`, `gate_main`, `MainRun`, `MAIN_RUNS`, `unslotted`, `GateRow`, `GATES_*`, `host_gates`, `SLOT_DRIVER`; the
gate driver's `unslotted` NOT RUN cause goes with them). A lane I10 does not review gates its own tip like any branch
(exit 14 without a record; the enqueue unit's clerk now gates before it lands: notes take the inert lane). `EXIT_GATE`
is `EXIT_MERGE_INCOMPLETE`: exit 5 now means only a clean merge of main the merged tree could not complete.

**The gate lock, one question (PB4204).** `gate_busy` asks the worktree's gate lock (and a STARTING run) before the
record is read and again before every later write (each catch-up merge and the stamp): a gate that runs is exit 12
whatever record exists, and the lane's existing wait lands the head again once the lock is free. `Tested` carries
evidence only (`running` deleted; `run` names the covering run for the lane).

**Every red gets a class (PB4271).** The queue watch reads a merge-group bounce's facts (`red_facts`: the failed run's
red cases, downloaded into the worktree's `TestResults/ci-bounce` and checked against its provenance marker through the
new `queue_facts.bounce_source`, beside the GREEN record of the queued head, following a REUSED record to the run that
measured it) and `classify_red` decides INFRA (no test failed, or the run timed out), RUNNER (every red case ran GREEN
in the record, or the record is the inert lane's), GENUINE (a case the record listed but did not run), MERGE (a case
the record lacks) or UNCLASSIFIED. INFRA and RUNNER are re-queued once with no agent (`enqueue.requeue`) and filed
against the pipeline (`landing_measure.record_red`: the new `reds` record and the existing `infrastructure` cause);
every other class, a second red of a re-queued landing, the same bounce read again and a refused re-queue end the
watch BOUNCED for the finisher with the class on its line. `test_population.red_cases` names the red cases (one
iterator under it and `red_classes`).

**Self-tests.** enqueue.py: the gate arms deleted; new arms for a running gate beside an older covering GREEN record
(exit 12, nothing written), a STARTING run, an unreviewed lane with no record (exit 14) and a true textual merge
landed with no gate; 60 arms PASS. land_lane.py arm 18 (classify_red's every class, red_facts on a planted worktree,
the watch's re-queue-once rule); 95 PASS. landing_measure.py's `reds` arm; queue_facts.py's `bounce_source` arm;
test_population.py's `red_cases` arm. The C# drift tests that hold the arms by name follow.

**Docs.** MANDATORY-PRACTICES L1, L2, L12, O11; the finisher brief's BOUNCED arm; landing.md; the gate skill;
cost-law-and-budget.md; units/enqueue.md steps 3 and 5; DESIGN-test-build-ci.md §3.17.2 step 3 and
DESIGN-orchestrator-loop.md §4.9 and §16.6. The whole-pipeline rewrite of DESIGN-test-build-ci.md is step 11 (PB4334).

**A flake found and fixed at its root.** The first gate (run 000001-20261101T145055Z) was RED on one case,
`RecordLayoutNoticeTests.OutputMode_IsNeverReported`: it swaps the process-wide `Console.Error` to capture the record
layout notice, and another class's harness cleanup printed `SCRATCH-LEFT` into the swapped stream. The class shared the
`process-globals` collection (PB126), which serialized only its own members. It now has its own collection with
parallelization disabled (`ConsoleErrorCollection`), so it runs alone after the parallel ones (0.1 s); the swap is the
only in-process stderr swap in the test assemblies (`Console.SetError` swept).

**Step 11, begun (PB4334).** After step 8 was gated GREEN (run 000002-20261101T151522Z), the owner-approved CLAUDE.md
"Testing" wording of PIPELINE-REVIEW.md §13 is applied, its placeholders filled (2026-10-31, kb/Work PB4323) and one
clause adjusted to what landed: a tooling change runs the code lane plus its self-tests, not the self-tests alone.
MANDATORY-PRACTICES I1 states THE RULE and its five first-run classes, I2 that a flake is a defect never re-gated past;
the gate and workstream skills, DOC_INDEX's landing entry, kb/Work PB1957 (restated to the five classes; its drift test
still to build) and the memory note follow. DESIGN-test-build-ci.md gains §0, THE PIPELINE AS IT IS, and current
titles for §3.15 and §3.17; its rewrite proper (the history of the targeted leg out of §1 to §3.18) is PB4334's rest.

**Register.** PB4331, PB4204 and PB4271 landed (no inventory row; process tooling). PB3804's `SLOT_DRIVER` comment item
left with its code. Lead: `gate_slot.py`'s landing gate class (CLASS_ENV=landing, served first) has no starter now.
