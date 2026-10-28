## File PB3988: a FINDINGS branch that another branch replays reads DECISION and would get a finisher of its own

The resume unit of 2026-10-28 decided the DECISION row `notes-pb1086-sites`: its one commit is replayed on the PB2100
implementer chain (`worktree-wf_c731503d-cbd-1`, `worktree-wf_82a2b4c0-fe8-1`), whose own commits answer all six of
its review's findings, so the branch is recorded ABANDONED in PB3988 and its work lands through the chain. The row read
DECISION because `prune_worktrees.contained()`'s replay arm skips a FINDINGS-stamped row, and the planner's PB3404
answered arm reads only a carrier that contains the branch, never one that replays it; the next wave would have planned
a finisher for findings already answered. PB3988 is new (PB3404 and PB3386 are landed, PB3668 is the replay arm's
docstring), with the fix direction recorded and not decided.
