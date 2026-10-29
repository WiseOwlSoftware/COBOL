## The review lane waits on an uncollected hold, so a mended torn findings file never gets a second reviewer of the same round (PB4049)

**Mechanism.** The enqueue unit answers a torn-file hold by dispatching one `cobol-reviewer` by hand into the hold's findings
file, which for that hold is the lane's own `review-<branch>.jsonl`. `review_lane.standing` grants a lease to an open round only in a
file that is not the lane's own, and a torn-file hold is a `Plan(hold=True)` only while the file reads torn. Once the operator mended
the file, `plan_review` read the unit's reviewer's open round as the lane's own killed round ("the round to start again") and the next
supervisor tick started a second reviewer of the same round beside the unit's, appending to the same file. The ATTEMPT_LIMIT hold
was already safe (the count of failed lane reviewers still holds it); only the torn-file hold lacked a second guard.

**Re-probe.** The self-test arm written first (tear the own file, hold, mend by writing a fresh `start` under reviewer label
`enqueue-review-K2-r1`, tick) failed on the unfixed tree with a second reviewer started, and passes now.

**Fix.** The note named two directions; the lane took the second, at its root: `plan_review` returns a hold plan while the branch has
an UNCOLLECTED hold at its head, for any reason, so the lane starts no review until the unit runs `review_lane.py collect`. One rule
covers every hold the unit answers by hand, and the lane's own file stays the one place a branch's rounds live (sending the
reviewer to a second file would have forked the lease and every reader of it). After `collect`, an open round with the unit's
reviewer gone is the lane's to start again, as before. The lease table in the module doc, DESIGN-orchestrator-loop.md section 4.9.1
and units/enqueue.md step 2 state the rule; `ReviewLaneDriftTests` declares both new self-test arms.

**Owner requirements of campaign PB3608.** (1) The conflict finisher and (2) the restart-safe repair of a torn or unfinished file
were landed with PB3608 (`tick`, `plan_conflict`, `repair_interrupted_append`); this change touches neither and the existing arms
for both still pass.

**Register.** PB4049 landed, `closes_rows: []` (process tooling, no inventory row).
