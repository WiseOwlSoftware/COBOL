# PB6059 - second occurrence: wave 20262143 replayed three of account 1's PB1754 branches

`plan_wave.py --cluster PB3160` (account 2, 2026-11-22 ~20:35) planned groups A (PB2723), B (PB2620) and C (PB2652) as
FINISH-FIRST over account 1's unlanded wave-20262139 branches (worktree-wf_46d3a156-23e-10, -3, -6), whose DEVLOG entries
are `pending-pb1754-group-*`: architecture work of account 1's PB1754 campaign. The notes carry `cluster: ["PB3160"]`, so
the 1985 lane's campaign planner took them. Account 1's earlier answer that nothing of its 1985 work was live did not
cover these branches, and account 2 launched without reading the predecessors' DEVLOG names.

Cost: three opus implementers (part of wave 20262143's 3.34M subagent tokens). Each returned SPLIT and gated GREEN on a
new branch (worktree-wf_6f9eca3a-cc1-19, -20, -21). Account 1 took all three (mailbox 2026-11-23 02:53): cc1-20 is
already the base of its wave 20262147 group A; cc1-21 duplicates its running group B and is abandoned once that lands; cc1-19
goes to its next finish-first plan. Its group B had planned over the same predecessor in parallel, so the fault ran both ways.

PB6059's fix covers this case: a FINISH-FIRST group over a branch another account dispatched is held, not finished. Until it
lands, the operator checks every FINISH-FIRST group's predecessor (its DEVLOG name and dispatching wave) before launch.
