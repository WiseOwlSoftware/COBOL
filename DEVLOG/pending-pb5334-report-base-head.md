## PB5334: a report's `Base/Head: <base> / <head>` header no longer reads its base as its head

Three DONE branches of wave 20262000 (PB5141, PB5197 with PB5261, PB5223) sat 130 to 171 minutes outside the review lane.
Their implementers wrote the report header as `Base/Head: <base> / <head>`, which 6 reports on disk use (with `**Base / head:**`), and
`reports.header_sha`'s fallback read `Head: <base>` from it. The branch survey then saw a report describing a commit
that was not the branch tip and called each branch a DECISION, so no reviewer started, and the loop mailed a STALL for
each. `reports.BASE_HEAD` now reads the combined field first, its first sha the base and its second the head, and
`test_plan_wave.py` checks both (552/552). kb/Work PB5334.
