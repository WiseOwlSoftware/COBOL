## PB3638: the owner sets a developer's 10-15 minute fix cycle as the loop's bar

At midday on 2026-10-24 the owner objected that work ran for hours with no updates while other items waited, and set
the bar: "As an individual developer, I can make a bug fix, compile it to ensure it compiles cleanly, and run various
local tests to ensure it's working, all within 10-15 mins".

The morning's campaign wave shows where the time goes. PB3611 was fixed and reviewed in 12 minutes, so a small fix can
meet the bar. But the planner chained PB3112, PB2878 and PB3612 in id order, so the owner's tier-1 decision PB3612
started two hours after the wave began. The unit reported two milestones in those two hours. Across 343 landings since
2026-10-17, a finished branch waited a median of 34 minutes (p90 2.2 hours) between DONE and the merge queue.

PB3638 records the bar and four defects at tier 1 of PB1957: chain order must follow tier, a wave plans one link per
chain and a unit hands off at about an hour, the supervisor and the wave report progress every ten minutes to the log
and the operator's mailbox, and every mechanically resolvable stop after DONE is resolved by the loop at once.
`landing_measure.py summary` is to report each stage's median and p90 against its budget.

The same hour the operator dispatched PB1957's ready notes (PB3628, PB2699, PB3625, PB3275, PB3310 and their
successors) as wave 20261030 beside the loop's running wave, planned by `plan_wave.py` so the dispatch ledger keeps
the two apart.
