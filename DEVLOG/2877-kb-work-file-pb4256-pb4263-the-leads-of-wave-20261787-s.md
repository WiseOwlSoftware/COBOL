## Entry 2877 — 2026-10-31 13:37 PDT — kb/Work: file PB4256-PB4263, the leads of wave 20261787's landings and the PB107 carrier's stranded-predecessor wait

PB4256 (MAJOR, process_only, silent) records that `enqueue.open_pull_requests` counts a pull request outside the merge queue with auto-merge off as one that will merge, so the PB107 carrier of PR #894 waited at exit 15 PREDECESSOR QUEUED on a pull request nothing would queue again; the enqueue unit closed #894 by hand at about 20:28Z. Two root-cause candidates are recorded for the implementer. PB3719 was checked and covers a different mechanism (a pull request of more than 100 files), so a new note was filed.

PB4257 (analysis, MINOR, cosmetic) records that a stray END-DISPLAY reads "cannot parse construct near" while END-ADD reads "unexpected", from PB4132's report. PB4258 records two statements in DESIGN-test-build-ci.md 3.17.6 that the run data contradict, from PB3773's review.

PB4259, PB4260 and PB4261 record PB4180's review leads: a declined enqueue handoff loses its reason under --cluster, the unit prompts name enqueue for rows rule 6 does not count, and the enqueue_chains measure counts legitimate chains (with the unmeasured resume-chain sibling folded in).

PB4262 records that landing and impact-map failures go to the operator whatever the lane, and PB4263 that the supervisor comments still say every stall goes to the operator mailbox, both from PB4190's report. PB4264 was allocated and left unused.

