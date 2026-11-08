## Entry 3335 — 2026-11-08 14:55 PST — The lineage slices PB4994, PB4995 and PB4996 run in parallel: their blocked_by chain was not a dependency

The lineage slices were filed as one linear blocked_by chain (PB4993 → PB4994 → PB4995 → PB4996), so the planner ran
one per wave. Only two of those links are real. PB4994 computes chain state from the `next` record whose shape and
validator are already on main (`lineage.next_errors`, `lineage.state_of`). PB4995's adjudicators decide KEPT, CHANGED
or REMOVED and record through `record_verdicts.py --edition 1985`, also on main. PB4996 derives bands from the same
lineage module. None of them waits on another's result, and their shared files merge as any two developers' edits do.
The links are removed. PB4997 (the conformance statement reports PB4995's verdicts) and PB4950 (its citations come
from the adjudicated chains) keep their dependency on PB4995.

