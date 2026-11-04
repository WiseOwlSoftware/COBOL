## PB4593: the fix lane re-finished a note the Mythos lane held

The operator loop's wave unit of 2026-11-04 10:40 dispatched a review finisher (w20261862a) for PB4573's round-2 findings,
while the Mythos lane's dispatch w20261860a held the note and its own finisher was answering the same findings on a new
branch, worktree-wf_09e5ef0b-71c-7 (reviewed CLEAN, landing as #1115). The duplicate, worktree-wf_acba161d-4d1-1, fixed
the same two findings again. Two guards should have stopped the plan: the IN FLIGHT arm reads only the branch the newest
report names, and the other lane's ledger entry did not hold the note. PB4593 records the mechanism to find, the root fix
and its tests, and decides the duplicate branch ABANDONED.
