## Entry 2869 — 2026-10-31 12:31 PDT — kb/Work PB3124 recurs: PR #896 recorded a bounce that was PR #894's

The enqueue unit ran `landing_measure.py complete` on 2026-10-31, and it recorded PR #896 with one bounce. GitHub removed #896
from the merge queue only once, with reason `merged`. Its one failed merge-group run (41230045564) was a group built on PR #894's
group commit 4985e16a6, and it failed the same `RunUnitStateDriftTests` test as #894's own run, which is a defect #894 carries.
This is PB3124's original mechanism: `complete` assigns a failed run to the pull request its `pr-<n>-` ref names, so a group
stacked on a red entry ahead of it counts against an innocent pull request. The note gains this recurrence and its evidence, and
its status stays open. The bounce stays UNATTRIBUTED in the record.

