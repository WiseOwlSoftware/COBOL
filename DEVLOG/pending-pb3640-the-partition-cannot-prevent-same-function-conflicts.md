## PB3640: the file-set partition cannot prevent same-function conflicts; two attempted rules abandoned

Wave 20261030 ran PB3628 and PB3275 side by side, and both rewrote the NOT QUEUED rule in `prune_worktrees.lifecycle`,
so PR #505 conflicted after PB3275 landed. The operator tried two planner rules on branch
`fix-pb3263-unknown-subsystem-prefix`. The first partitioned an unknown file set by the area's top segment, and its
review showed the register files the orchestrator's scripts under five top segments, so open pairs still met nowhere.
The second met an unknown set with everything, which held finishers back a wave and failed five planner checks. Both are
abandoned, and nothing of the branch landed.

A complete partition would not have saved this one either. PB3628's own site block named only `land_lane.py`, and its
change also edited `prune_worktrees.py`. A declared file set is a prediction. PB3640 records the remedies that do cut the
cost, in PB1957 at tier 2: dispatch the conflict finisher the moment a landing stops on a conflict, keep branches short
(PB3638), and require site blocks on every note a running campaign names.

The same entry records the abandonment of PB2878 part 1's branch, `worktree-wf_0e51b14b-ced-2`. Part 2 carried its
commits and landed both parts through PR #501.
