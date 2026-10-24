## Entry 2489 — 2026-10-24 16:53 PDT — PB3661 and PB3660 join the PB1957 campaign; PB3609 records a landing-lane recurrence

The owner asked on 2026-10-24 whether every loop performance improvement is in use. The operator checked today's gate
records and found that PB3661 had already cost a whole-population gate. The last gate on PB2974's branch (run
000005-20261024T225004Z) printed `no plan (ParseError: not well-formed (invalid token) …)` and ran all 18,394
Conformance and 38,135 Unit cases where leg 1 is about 10,200 and 36,800. Both PB3661 and PB3660 were filed in clusters
of their own, `["PB3661"]` and `["PB3660"]`, which no campaign lane plans. The loop runs `-Cluster PB1957,PB3608`, so
neither would ever be dispatched. Both are gate and landing defects of the kind PB1957 collects, so they now name
`cluster: ["PB1957"]`. PB3661 is tier 1 beside PB3638, because a single unreadable trx turns the targeted gate into a
population run. PB3660 is tier 2, because no branch with a gate record older than PB3612 is waiting.

PB3609 gains its recurrence. At 16:33 PDT the landing lane started `enqueue.py land` on PB2974's branch 14 minutes after
pull request #512 merged. The survey read the merged branch IN FLIGHT, so `rearm_hold`'s LANDED arm never applied. The
attempt ended exit 6 NO ENTRY without harm.

