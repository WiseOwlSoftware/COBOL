## PB3267: a branch whose review ended FINDINGS is a carrier in the branch survey, so its predecessors land with it

`prune_worktrees.contained()` took its carriers from `CARRIER_STATES`, and a DONE branch whose tip carries a FINDINGS stamp is
a DECISION row, so it was never a carrier. A predecessor with no report of its own that sits inside such a branch read
DECISION too, although every commit it holds lands with the branch once the planner's review finisher has answered the findings.
On 2026-10-17 the survey listed `...-fa2-10` and `...-fa2-13` that way and a resume unit decided them by hand (PB3346, PB3317).

**Re-probe.** The note's other direction (a FINDINGS branch absorbed by a successor, its finisher never planned) was already fixed
by PB3311 (`owned()` excludes a carried branch, so the planner plans its finisher) and PB3381 (`carrying_findings` holds a WAITING
TO LAND successor on the unanswered findings); a new self-test row (`hpred` inside an unreviewed `hsucc`) passes on the unfixed
tree and pins it. The mirror direction (`gpred` inside FINDINGS-stamped `gsucc`) failed on the unfixed tree: DECISION.

**Fix.** `contained()` reads a carrier from `CARRIER_STATES` or from `finisher_due(row)` (a finished branch not on main whose FINDINGS
stamp covers its tip: the planner's own test). `lifecycle` and `finisher_due` share one `stamp_held`. Goldens: none (no COBOL
construct); the regression is `prune_worktrees.py --self-test` (rows `gpred`/`gsucc`, `hpred`/`hsucc`, the owned set, the verdict
counts). PB3267 `open` to `landed`, `closes_rows: []` with its reason. Open sibling, already filed: PB3386 (a SPLIT predecessor is
not carried either).
