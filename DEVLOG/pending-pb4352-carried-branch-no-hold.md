## PB4352: a branch carried by a finished one no longer deadlocks the planner

The resume unit found two DECISION rows idle for more than five hours, each with one blocking branch-review finding
(`worktree-wf_2201113e-54d-26`: PB4240 implements the keyword-folding direction the owner reversed; `-54d-29`: the
cut-short reading wins over a longer name reading). The survey says the next wave plans their finishers, but
`plan_wave.py --drain` planned none. Each finisher waited on a branch the other one carries: `54d-2` (merged into
`54d-26`) met the PB4207 group, and `54d-25` and `54d-4` (merged into `54d-29`) met the PB4240 group. That is the PB3528
deadlock one level down. The exemption compared the exact branch name, and a carried branch has its own name.

`plan_partition` now reads every in-flight branch as the branch its work lands through (`carrier`, the same function the
finisher arms use) when it builds a finisher's claim, exempts the group's own work, and maps a meeting to this wave's
finisher. The self-test's PB4352 block fails on main's planner (both notes waiting, 0 groups) and passes with the fix
(504/504). On the live state the 54d-29 finisher is planned, and the 54d-26 finisher waits only on
`worktree-wf_39ac352f-b1c-1`, which is unlanded and gates RED on PB4338.
