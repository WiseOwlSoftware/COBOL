## kb/Work PB3345: a live review finisher's carrier does not hold its reviewed branch's findings

The operator filed kb/Work PB3345, a sibling of PB3311. On 2026-10-17 the attended session's rolling wave (wave 1110,
PB3339) stamped `FINDINGS 2` on its implementer's branch, and its own review finisher started in a new worktree that
merged that branch and committed the fix. `plan_wave.py --dry-run` still planned a second PB3339 finisher.

The ledger group is released by the implementer's report. The reviewed branch has no lock, so it is unowned.
`branch_carrier` names only finished carriers. The note's fix shape: the FINDINGS arm waits while a live-locked branch
contains the reviewed one, with a self-test. Notes only; no source change.
