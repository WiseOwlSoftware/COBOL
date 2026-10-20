## PB3473, PB3152: a report answers the notes its group was dispatched with, never the ids its header mentions

Wave 1135's planner printed `awaiting landing: fix-pb3465-never-work-paths-20261020: PB3337` and planned only PB3466,
because the PB3465 report's opening paragraph says "Wave 1134's plan held PB3337 ..." and `plan_wave.parse_report` took
every PB id in a report header as a note the report answers. PB3337's own SPLIT branch (the top of `work.py next`) got no
finisher. Wave 1076 had met the same mechanism from the other side (PB3152): a FINDINGS report's header named open
PB3101, so the review finisher was planned under PB3101. The branch review of the filing branch (R1-1) found PB3473 was
PB3152's `awaiting` arm; one mechanism, fixed once, both notes closed.

`Report.ids` is deleted. `Report.notes` replaces it and `plan_wave.answered_notes` defines it: the report's lead (the id in
its file name) plus the notes of the dispatch-ledger group the report `answers` (PB3311's predicate: its slug, written at
or after the group's `at`). The ledger records every dispatched group's notes mechanically, so no agent has to write a
field it may forget; a report no ledger group knows answers its lead alone. `plan()`'s `newest`, `attempts` and
`classify_inflight` read it. The branch review's blocking finding (R1-1) was that a note a DONE branch closes beyond
its dispatch (`PB3383 (+ PB925)`) lost the header channel that used to hold it: it was planned a second time. The
branch's own register is the mechanical record, so `closed_by_branch` lists the `kb/Work` notes the branch flipped to
`landed` or `retired` since `origin/main` and `add_closed_notes` adds them to each finished report on an unlanded
branch (section 6b'''00: the flipped note is held, a merely touched one is planned). The first idea, a header-field reader shared with `prune_worktrees.newest_report`
(PB3084), was dropped: a note id has no header field. `test_plan_wave.py` section 6b'''0 puts the mention in the HEADER of a
FINDINGS and a CLEAN report and checks the notes, the awaiting set, the finisher's lead and `attempts` (six checks that
fail on the old reading); two older fixtures that relied on a header mention for a multi-note group now record the group
in a ledger. Sweep: no other reader of note ids in report text exists in `scripts/`. Self-test 324/324.
