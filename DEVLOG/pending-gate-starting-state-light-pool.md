## The gate learns when it is starting, a notes-only gate gets its own slot pool, and the record-age sentence is derived (PB3707, PB3724, PB3717)

Wave 20261703 group A, one root: the gate's bookkeeping in `scripts/run_gate_legs.py`, `scripts/gate_slot.py` and
`scripts/orchestrator/enqueue.py`. None of the three touches an ISO rule; each is a process defect from the PB1957 campaign.

**PB3707, the window before the lock.** `build-local` makes a run directory and spawns the gate child; the child takes the
worktree's gate lock and writes `gate.pid` only after interpreter start and imports. In that window the lock is free and there
is no pid, and two readers judged it differently and wrongly: `enqueue.tested_locally` read a young run with a free lock as
"a gate that died" (exit 14 UNTESTED, a finisher sent to gate a tip whose gate was about to run), and `gate_running` read any
run with no pid as running for ever, so a child that died on an import error hung `--await`. The re-probe on planted run
directories showed both. The fix is one reading, `run_gate_legs.gate_phase`: RUNNING (the lock is held by a gate writing the
run), STARTING (no pid, younger than `START_WINDOW_S` = 60 s by the UTC stamp in the run's name, and the launcher's
`gate.spawn` record, when written, names a live process) or DIED. A pid is never trusted alone (PB3662 R1-1), only inside the
window. `launch_and_follow` writes `gate.spawn` after the spawn, `write_pid` (under the lock) replaces it. The landing reads
STARTING as a verdict owed (exit 12 GATE BUSY), `--await` as PENDING; a child that is gone reads DIED at once. The
self-review found two races, both fixed here: the child's unlink of the spawn record can meet the launcher's open handle on
Windows (it is tolerated, since the pid file outranks the record), and a launcher whose record write fails now says so.

**PB3724, a notes-only gate waited for a full slot.** The full lane already classified the change before taking a slot; the
INERT lane did not need a full slot but took one for its Linux script self-tests (about a minute, PB3646), so every
kb/Work, DEVLOG or owner-decision landing queued behind the implementers' builds. `gate_slot.py` now has two pools (`Pool`):
the gate pool as before and a LIGHT pool (`light/`, own slot files, tickets, cap and hold history, coded default 2, an
implementor's choice the owner changes with `set-light-cap N`). `Gate._inert` takes a light slot; nothing holds both pools, so
neither can wait on the other; `status` prints both; `run --pool` wraps a command in either. The log now explains itself: a
gate waiting while its `k ahead` rises is told which later landing ticket is served first (PB3103), and the gate next in
line says it waits for the first released slot. Arms: `gate_slot.py --self-test` grew from 12 to 14 (light pool; explanation),
each seen RED on a planted defect, `GateSlotDriftTests.Arms` lists them, and a `run_gate_legs` arm pins the pool each lane
takes. Not measured: the inert gate's wall time beside three live implementer gates; the arms pin the queue shape.

**PB3717, a note id typed into a sentence.** `predates_landing` spelled `kb/Work PB3612` beside the structured
`RECORD_SCHEMA`. `SCHEMA_NOTES` (schema 3 is PB3536, 4 is PB3612) now pairs each schema with the note that introduced it and
the sentence is derived from it; a self-test arm fails when a bump of `RECORD_SCHEMA` has no row.

Rows re-verdicted: none (all three note `closes_rows: []` with a reason). Registered: nothing new outside the group.
Docs current: DESIGN-test-build-ci.md (§3.14.6, §3.14.12, §3.17.2 step 3, §3.18), the gate skill, DOC_INDEX.md, plan section 0.
