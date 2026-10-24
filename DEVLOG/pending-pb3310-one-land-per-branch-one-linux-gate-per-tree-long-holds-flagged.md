## PB3310: one land per branch, one Linux gate per tree, long slot holds flagged

On 2026-10-16 two `enqueue.py land` runs of one branch overlapped. The second run's Linux gate deleted the first's WSL
clone while its `dotnet test` was still running in it, the test hung for 6 h 40 min, and the first run held one of the
three gate slots the whole time. Nothing refused the second run, nothing named the slot, and the operator's peer session
found it by reading a process list. Recurrences on 2026-10-19 and 2026-10-22 (a hand-run `land` beside the landing lane's
attempt, which the loser read as a red gate).

Re-probe on the current tree: the clone half had already gone with PB3194 (the gate directory is keyed by the run's pid
and removed on every exit path), and PB3527 made a held gate lock exit 12 instead of a red. Three things were still true.

**The land lock.** `enqueue.py land` now takes an exclusive OS lock in the worktree's git directory
(`cobol-land.lock`, `gate_slot.ExclusiveLock`) before its first read of the worktree and releases it in `end()`, the one
exit every outcome passes through. A second run prints `=== ENQUEUE: REFUSED — another land of <branch> runs (<holder>)
===` and exits 13: nothing merged, gated, stamped or reported, no BLOCKED line, no finisher. The OS frees the lock when
its holder dies, so "a lock whose pid is dead is taken over" needs no pid bookkeeping. The landing lane treats 13 like 12
(`WAITS`, `wait_free` probes the same lock). It is per worktree, not a file in the coordination directory: a branch is
checked out in one worktree, and the lane already finds the gate lock that way. A dry run takes none.

**One Linux gate per tree.** `linux-gate.sh` emptied `TestResults/linux-gate` (the planner's "newest run" directory)
before anything else, so a second run on a tree deleted the first's logs. `begin_gate` now looks, under an flock on a
dotfile, for a running gate of the same tree (its owner file names the tree and a pid with its start time) and answers
`=== LINUX GATE: BUSY — … ===`, exit 3, before touching anything. BUSY is the vocabulary the gate driver and `enqueue.py`
already read as neither a pass nor a red.

**Long holds.** `gate_slot.py` records each released hold's duration per kind (`gate`, `Linux gate`,
`record_impact_map`) in `holds.json` and `status` marks a held slot older than 5 times its kind's median (at least five
recorded) `LONG HOLD`. The limit comes from the gates' own history, not a constant per leg; the flag is advice only, the
operator verifies the pid before stopping anything. `session-probe.ps1` prints those lines, and a hold history it cannot
read, instead of dropping them.

Tests: `gate_slot.py --self-test` arm 12 (kind rule, baseline and its minimum, flag threshold, a real held slot aged
three hours, a corrupt history that must not hide the holders); `enqueue.py --self-test` (a second land refused at the
lock with exit 13 and nothing touched, a dry run beside it, the lock released by an early outcome, the Linux gate's BUSY
line read as BUSY from the script's own text); `land_lane.py --self-test` (exit 13 waits on the land lock);
`linux-gate.sh --self-test` (BUSY with nothing created, another tree, an ended gate; run under WSL as well, where flock is
real); `LandingPipelineDriftTests` and `GateSlotDriftTests` pin the order (lock before any read), the lane's and the Linux
gate's answers and the arm list.
