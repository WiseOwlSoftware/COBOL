## PB4929: abandon the superseded first-attempt branch

The branch survey listed `worktree-wf_a62b5670-5f6-6` as a DECISION row. It held the first PB4929 implementer's SPLIT WIP
checkpoint (6d7977f746). That work continued on `worktree-wf_3969346e-be2-1` (d76f436f44) and landed with entry 3344, so
the old branch carries nothing main lacks. PB4929's note now records the branch ABANDONED, so `prune_worktrees.py --apply
--abandon` can delete it (owner rule 2026-10-08, kb/Work PB2600).
