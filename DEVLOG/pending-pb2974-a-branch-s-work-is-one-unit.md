## PB2974: a branch's work is one unit, so one dispatch holding a note holds the whole report

Wave 1056's first plan re-dispatched three wave-1053 SPLIT branches that wave 1055's finishers were finishing. The
ledger recorded each finisher under only some of its branch's notes (w1055A PB2727, PB2729, PB2773 of the four on
w1053b), `held_by_dispatch` held exactly those, and `plan()` built a finisher over the same branch from the rest. Three
agents would have redone work already in flight and landed duplicate commits.

Re-probe: still reproduced. A SPLIT report of three notes with a ledger dispatch holding one of them planned a second
finisher over the same branch for the other two.

Fix, in `plan_wave.plan()`, one rule for both arms that continue a branch: when an unanswered dispatch holds ANY note
of a report's finisher set, or of an unlanded branch's notes, the others wait too, naming the held note and its
dispatch. A finisher continues one branch, so its notes are never split between two agents. The sibling arm: an
UNLANDED branch's finisher truncated its notes at the group cap, and the notes past the cap were planned again as
clusters beside it; it is now one finisher per branch, as the report arm already was.

`test_plan_wave.py` section 6d drives it through the real ledger (`record_dispatch`, `held_by_dispatch`,
`load_reports`): the three-note report with one held note plans no group in either lane or under `--drain`, the notes
of other clusters still plan, the dispatch's own report releases the hold, an unlanded branch with a held note plans
no finisher for the rest, and a branch past the cap stays one finisher. The section fails on the old code. The design
doc's planner step 1 and the module docstring name the rule.

Not closed: telling a live agent's lock on a SPLIT branch from a recent touch (the note's 2026-10-16 paragraph) needs
a lock-only signal that the branch survey does not expose; the ledger hold is the signal a dispatched finisher leaves.
