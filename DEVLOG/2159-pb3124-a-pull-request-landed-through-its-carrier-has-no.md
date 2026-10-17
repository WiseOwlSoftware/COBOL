## Entry 2159 — 2026-10-17 15:00 PDT — PB3124: a pull request landed through its carrier has no merge-group run naming it

kb/Work/PB3124 gained a recurrence section. PR #149 was removed from the merge queue for a merge_conflict, never re-queued, and landed inside its successor's PR #190; GitHub marked both merged with the same commit (b05001e4c446d69e1833a06f3c3bf84468911f2a). `landing_measure.py complete` found no merge-group run naming #149 and reported its CI wall UNMEASURED, while the carrier's run had measured it. The note's fix shape now says a pull request whose mergeCommit equals another's landed through it, so `complete` should record `landed_through` and use the carrier's run, and its self-test needs a carrier-landed case. The note stays open.

