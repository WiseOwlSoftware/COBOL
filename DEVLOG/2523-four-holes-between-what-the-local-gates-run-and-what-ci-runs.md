## Entry 2523 — 2026-10-24 22:49 PDT — Four holes between what the local gates run and what CI runs close (PB3646, PB3649, PB3661, PB3648)

Group A2 of the PB1957 wave 20261104. It merged group A's branch (PB3662, PB3145) at its stamped head, so it carries
DEVLOG 2522 as main will have it and lands after group A's pull request (exit 15, PREDECESSOR QUEUED, waits for it).

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

**Review round 1 of group A2 (R1-1, the sibling arm of the reused process id).** The landing's reader of a running gate had
become the held gate lock because Windows reuses a dead gate's pid, but `run_gate_legs.watch` (the reader behind `--await`,
the rolling wave's await and `plan_wave.Report.gate_state`) still decided "alive" by `alive(read_pid(run))`. A handed-over gate
whose host rebooted mid-gate read RUNNING for as long as any process held its pid, so the report was never finished and the wave
waited on it. `watch` now takes the running test as an argument: `gate_running(run)` (the run's worktree's gate lock, held by a
gate writing THIS run; `lock_of_run` derives the lock from the run's path) for every reader of a gate it did not start, and the
follower's own spawned child for the gate it just launched. Because a lock-based reader cannot tell "dead" from "not started
yet", the gate now writes `gate.pid` itself UNDER its lock and the launcher writes none: a pid file proves the lock was taken
(a BUSY gate writes none). Self-tests: a reused pid (alive process, free lock) and another run's lock read DIED; the run's own
lock reads PENDING; a BUSY gate leaves no pid file; `test_plan_wave` and `test_rolling_wave` plant a real worktree and hold its
lock for the RUNNING arms and add a reused-pid arm that reads ENDED. R1-2 (lead): `profile-test-parallelism.py` in single-file
mode returned 0 for an unreadable trx; it now prints the error to stderr and returns 2, as for "no test results".

