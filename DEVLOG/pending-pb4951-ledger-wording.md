## PB4951 follow-up: the ledger names the fix lane's population as work.fixable

PB4951 made `work.fixable` the fix lane's population, but five passages in `gen_ledger.py` still told the reader the
fix lane plans ".agent-fleet.json's population": the module docstring, the loud no-lane flag, the estimate's inputs line,
the remaining-work lead-in and the page footer. Ledger v340 rendered the stale wording beside the corrected count. Each
now names `work.fixable`. `.agent-fleet.json` stays in the page's input set, because `fix_clusters.py` still reads its
source roots and harm weights from it.
