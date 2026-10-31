## Entry 2821 — 2026-10-30 17:27 PDT — The loop's chained enqueue units are filed as a bottleneck (PB4180)

On 2026-10-30, between 16:41 and 17:21 PDT, the orchestrator loop ran three `enqueue` units in a row. Each handed off
`enqueue` again to wait for a pull request to merge. That waiting is the landing lane's job, and two campaigns had ready
work. `units.jsonl` shows the pattern since 2026-10-01: 28 chains, 75 units, 49.6 hours of the one supervisor's time.
`next_unit.py` obeys a handoff-named unit before every other rule, so nothing broke the chain until the operator
steered a campaign. kb/Work PB4180, in the bottleneck campaign PB4147, records the mechanism and the acceptance: a
handoff names a successor only for work no lane owns.

