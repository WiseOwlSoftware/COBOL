## PB3487: filed the loop defect that starts campaign and wave units the planner cannot fill

While PR #351 (PB3337's first slice) sat in GitHub's merge queue, the loop ran four opus units in eight minutes
(campaign, enqueue, wave, campaign), and the two waves and two campaigns each planned 0 groups. `next_unit.py`
chooses `campaign` from `work.py`'s register-only readiness, while `plan_wave.plan` holds every candidate on the
in-flight partition (PB3337 awaiting landing; PB3338, PB3340, PB3460 and PB1278 colliding with in-flight or DECISION
worktrees). Filed as kb/Work PB3487 (MINOR, process-only, silent); no code change in this landing.
