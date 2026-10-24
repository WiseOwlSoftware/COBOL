## kb/Work: file PB3625 — the planner orders reports by integer wave number across lanes

The wave-20261027 loop unit found that `plan_wave.py` planned a PB3340 finisher on `worktree-wf_f6f89a51-99e-1`
from its SPLIT report `w20261024a`, while `w1157a-pb3340-sequential-journal` already contains that branch, carries a
newer DONE report (`w1157a2`) and was being committed on by the Mythos lane. `load_reports` orders reports by
`Report.wave`, an integer, so a date-numbered wave (20261024) outranks every serial wave (1157) whatever the write
times. The unit dropped the group before launch (`plan_wave.py --release 20261027a`) and filed PB3625 with the
fix shape (one cross-lane order key, a sweep of every wave comparison, a fixture); PB3170 is its sibling.
