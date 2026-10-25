## The landing lane waits for its gates and a predecessor, and four holes between the local gates and CI close (PB3662, PB3145, PB3646, PB3649, PB3661, PB3648)

This landing carries two branches' work, one entry (a landing carries exactly one). Group A (PB3662, PB3145) is first;
group A2 (PB3646, PB3649, PB3661, PB3648, wave 20261104) follows it under "Group A2" below.

Two landing stops (kb/Work PB3662, PB3145) shared one cause: `enqueue.py land` read a state that only time resolves as a
terminal stop, so each branch sat STOPPED until the operator re-armed it by hand.

**PB3662, a gate still running.** Since PB3612 `land` gates nothing and reads the worktree's gate records. A run that is
still going has no `verdict.json`, and `tested_locally` counted it as one more reason for exit 14, UNTESTED, which the lane
classes STOPPED. The lane launches a landing the moment the survey reads a new head, which is exactly when the implementer
or the resume unit has started its gate. Re-probed with a planted run (a run directory whose `gate.pid` names a live
process, no verdict): UNTESTED, exit 14, STOPPED. The fix is at the one reader: `run_gate_legs.live_gate(lock_path)` is the
worktree's gate lock held. `Gate.run` takes that OS lock first and holds it until its verdict file is written, and the OS
drops it when the gate dies, so it is held exactly while a gate runs. The first version read the run's `gate.pid` and
asked whether that process lived; the branch review (R1-1) showed Windows reuses a dead gate's process id, so a gate that
died at a reboot would have read as running for ever (exit 12, and the lane waiting with no escalation). Reading the lock
needs no process id. `run_gate_legs.run_dirs` is now the single run listing (the private `Gate._runs` is deleted and its
seven callers changed). `Tested.running` carries the live gate and `land` ends exit 12, GATE BUSY (nothing ran, nothing is
red, no report line); `gate_lock_free` (exit 12's wait in the lane) is the same lock read, so the same head relaunches the
moment the gate ends, with no `rearm`. A run with no verdict whose gate lock is free (the gate died) counts as no record.

**PB3145, a merged predecessor's open pull request.** A branch that merged a predecessor carries the predecessor's stamped
DEVLOG entry until that pull request merges. `devlog.py stamp` folds only pending entries (PB3123), so the branch adds two
stamped entries, `pull_request_text` finds no single one to title the pull request with, and `land` ended exit 6 NO ENTRY,
a stop. Main resolves it by itself (59.8 min for PB3112, 116.9 min for b3b-9, a hand landing for PB3267). The new reader
`enqueue.open_predecessors` lists the open pull requests into main from GitHub (the branch's own pull request, which a
bounce reuses, excluded) with the files each current head changes, and maps each to the carried entries it adds (review
R1-2: the first version required the pull request's CURRENT head to be an ancestor of the branch, so a head that moved after
the branch merged it, a bounce re-landed with newer main, ended exit 6 again; the file list is the reading, and
only a list GitHub cut off at 100 files still asks the ancestry); `awaited_predecessors` answers a wait only when the landing's entries less those the open
predecessors own leave exactly one. `land` then ends exit 15, PREDECESSOR QUEUED (nothing queued, no report line, the merge
and the stamp kept). GitHub unreadable is exit 9, retried once; a predecessor pull request that closed unmerged is no longer
open, so the same head ends exit 6 NO ENTRY as before, a finisher's. The lane carries exit 15 in `WAITS` and asks the same
reader through `predecessor_free` (unreadable GitHub is not free), so no second reader of "which pull request owns this
entry" exists.

**Sibling sweep.** The other retry-when-free exits (3, 12, 13) each wait on their own reader; 12 now has two producers
(a held lock and an owed verdict) sharing one wait, and 15 joins the table in the lane's module doc, MANDATORY-PRACTICES
L12, `landing.md`, `units/enqueue.md`, DESIGN-orchestrator-loop.md (two rows and the self-test list), DESIGN-test-build-ci.md
and DOC_INDEX.md. No other reader of "no verdict file" and no other exit-6 NO ENTRY producer exists in `scripts/`.

**Tests.** Self-test arms: `run_gate_legs` (`live_gate`), `enqueue` (a held gate lock is 12; a run with no verdict whose
`gate.pid` names a live process but whose lock is free is 14; a GREEN run beside a running gate covers; a moved predecessor
head still owns its entry; a wait with the branch's own pull request in the list, unreadable GitHub, a closed predecessor, then
the merged predecessor lands and is titled from its one entry), `land_lane` (exit 12 for a held gate lock and exit 15 each wait
and relaunch the same head with no rearm). `LandingLaneDriftTests` and `LandingPipelineDriftTests` hold the new arms by name.
No registry rows are claimed (process-only notes).

## Group A2 — four process holes between what the local gates run and what CI runs

All four are `process_only` notes of cluster PB1957 (the CI invariant); none claims an inventory row.

**PB3646, the Linux self-tests in the inert lane.** CI's `audits` job runs `self_tests.py` on Linux on every event, the inert
ones included. `linux-gate.sh` ended GREEN in its inert lane before any leg, and the Windows gate's inert lane ended before
its Linux half, so for a `kb/` or `DEVLOG/` change no local process ran those self-tests. The note's alternative (argue the
self-tests cannot be affected) is false: they are audits that read the inert paths by design, which is why the inert watch
exempts them. Fix at both processes: the Linux gate's inert lane runs the `selftests` leg (clone of HEAD, no build; the
GnuCOBOL corpus copy and fetch moved below the lane decision, since the inert lane reads none), `--legs inert` is the same run for a caller that
already classified the change, and `run_gate_legs.Gate._inert` takes a slot on a host that runs the Linux half, runs it after
the audits, records it as the Linux half and ends BUSY, NOT RUN or RED on it as the full lane does (`_linux_stopped`, factored
from the full lane's inline arm). `_linux_attested` and `enqueue.tested_locally` no longer exempt an inert record, so an
inert record from before this change attests nothing on a host that runs the half. Measured: 25 s of self-tests, 45 s for the
whole Linux inert run.

**PB3649, exit 12's second holder.** `linux-gate.sh` answers BUSY when another Linux gate of the tree runs; the Windows gate
ends BUSY on it; `enqueue` ends exit 12 with a message that always blamed the Windows gate lock; `land_lane.wait_free` probed
only that lock. New `linux-gate.sh --probe` (owner files only, no SDK) answers the same BUSY line, now naming the running
gate's pid and start time (`busy_line`, shared with `begin_gate`), or FREE; `Host.linux_gate_holder` asks it (no WSL, a hung or
unanswerable probe reads as no holder and is SAID: a retry that meets BUSY again touches nothing, a wait that never ends
costs the branch); `gate_lock_free` is free only when the gate lock is free and no Linux gate runs; the `enqueue` message
quotes the gate's own verdict line, which names the source.

**PB3661, the unreadable trx.** `gate_plan.trx_results` parsed every previous-gate trx with a bare `ET.parse`, so a torn file
ended the plan with `ParseError … line 104295, column 36` and no path, and the Linux half emptied `TestResults/linux-gate/`
before anyone could look. `test_population.trx_root` is now the one reader (`UnreadableTrx` names and carries the file);
`read_trx`, the planner and `profile-test-parallelism.py` read through it; the gate driver keeps the file in the run directory
as `unreadable-<dir>-<name>.bad` and the plan note names both; `gate_plan.py --audit-trx-readers` (a `GateLegDriftTests`
fact) fails any second reader. An unreadable side source (the Linux reds) still costs the whole plan, pinned: leg 2 is not
run locally, so dropping that tier 0a could hide a red.

**PB3648, the disk bound.** Measured a full-lane `linux-gate.sh --nice` by sampling its gate directory: peak 1,714,628 KB and
101,682 inodes with two builds of 49.5 s and 49.8 s running concurrently, against PB3194's ~1 GB and ~80,000 for one clone. The
second build stays (isolation from the inert watch; the assemblies bake in the clone path). `MIN_FREE_INODES` and `MIN_FREE_KB`
are now `GATES_HELD` (12) times `GATE_INODES` (110,000) and `GATE_KB` (1,800 MiB), and every gate prints its own footprint and a
`FOOTPRINT NOTE` when it is over them, so the next growth is a log line.

**What the full Linux run showed.** A full-lane Linux run of the branch (unit 38,254, characterization 36, conformance 18,450,
guard, both self-test legs) was GREEN except `LinuxGateDriftTests.…AgreeOnTheirArguments`, which pinned `targeted)` (fixed:
`targeted|inert)`, plus the inert mode's driver command), and an inert-watch RED that was the footprint SAMPLER's `find` listing the
watched directories during the run (an strace of the unit leg showed no open of an inert path): measuring a gate while its
watch runs perturbs the watch, which is why `footprint_report` runs after the watch ends.

**Tests.** Self-tests: `linux-gate.sh` (the BUSY line, `--probe`, `--legs inert`, the bounds, the footprint line and note),
`run_gate_legs` (torn trx kept and named; the Linux holder; the inert lane's Linux half, its RED/NOT RUN/BUSY, an old inert
record), `land_lane`, `enqueue`, `gate_plan`, `test_population`. Drift tests: `InertLaneDriftTests`, `GateLegDriftTests`,
`LandingPipelineDriftTests`, `LinuxGateDriftTests`.
