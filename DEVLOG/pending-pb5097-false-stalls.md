## PB5097: a queued group is not a stalled group, and a live gate is never silent

The loop's progress report mailed a STALL for every group the rolling wave had not started yet (wave 20261973 groups G and
H at 29 minutes, then every hour for 22 hours; waves 20262000, 20262001 and 20262004 the same) and for every gate that was
alive but quiet. Three mechanisms, one root: `progress.py` read "no report" as "running" and dated the group from the
ledger's `at`, which is stamped once for the whole wave, and read a gate's silence as the group's.

**Queued.** A group with no report and no act in the coordination directory (`group_activity`, whose first act is the
implementer's scratch directory, P16) is now `queued`: counted apart from `running` in the PROGRESS line
(`groups running N, queued M`), never a STALL, never over-size, never in the running list `group_sizes` records. The
rolling wave's implementer prompt makes that directory the first command (`startLine`, pinned by `test_rolling_wave.py`).

**Orphaned.** A queued group of a wave that began and has shown no act for `ORPHAN_IDLE_MIN` (120) minutes with no group
running has lost its runner. The unanswered dispatch also held its notes from the planner (22 hours for three compiler
notes, the addendum), so `progress.py --record` answers it with a `NOT STARTED` report (`answer_orphans`, never over an
existing report); a wave that never began (planned, awaiting the operator's GO) is not orphaned and keeps its hold.

**A gate leaves a heartbeat.** `gate_slot.Heartbeat` (an injected clock) and `HEARTBEAT_S` (120 s): `GateSlots.acquire`
writes `gate-slot: still waiting, k ahead (ticket t, ...; n s in the queue)` while its place does not move (26.7 minutes
were measured silent), and `Gate._run_hosts` writes `build-local: tests running, n of m test host(s) finished, s s since
the hosts started` while the hosts run (20 and more minutes). The gate's output is redirected into the group's scratch,
which `group_activity` already reads, so no second channel exists; `progress.self_test` holds two beats inside
`STALL_IDLE_MIN`.

**Proof.** New cases: `progress.py` (queued groups, a wave of more groups than workers, the unstarted wave, a gate in its
test phase and one in the queue past 20 minutes with a fresh heartbeat, the orphan and its answer), `gate_slot.py` (a
queued gate writes heartbeats on a stepped clock), `gate.py` (a test phase with silent hosts writes one, a stepped clock),
`test_rolling_wave.py` (the start line). Run against the old behaviour each fails: the queued check disabled leaves 12
`progress.py` FAILs (w10a, queued 1362 minutes, is mailed as a STALL); the heartbeat `say` disabled in `acquire` and in
`_run_hosts` fails the `gate_slot.py` and `gate.py` arms. No test holds a wall-clock limit.
