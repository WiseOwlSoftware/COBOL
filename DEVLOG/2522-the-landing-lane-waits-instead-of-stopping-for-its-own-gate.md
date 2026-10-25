## Entry 2522 — 2026-10-24 22:36 PDT — The landing lane waits, instead of stopping, for its own gate and for a merged predecessor's pull request

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

