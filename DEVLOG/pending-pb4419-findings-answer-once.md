## PB4419: an absorbed finisher states the review-findings answer rule once

Wave unit 20261815 planned group A, ONE step over `DataBinder.cs` (kb/Work PB4396). The step absorbed the finishers of
two branches whose reviews stamped FINDINGS (54d-29 for PB4208, 54d-26 for PB4240). `finisher_pred` ended each of
their preds with the same answer-by-id sentence, and `Group.absorb` joined the two preds as they were. The rendered
spec therefore said the sentence twice, `check_practices.py`'s PB3777 once-check went RED, and the wave launched
nothing. The unit filed the defect and made the fix the unit's whole job, which is what the unit rules require when a
planner bug makes the plan unusable.

The fix moves the sentence into `groups.FINDINGS_ANSWER`, a constant that names no branch, and adds
`groups.join_preds`, which `absorb` now calls. It keeps each branch's own text in order and states the answer rule
once, at the end. A new check in `test_plan_wave.py` absorbs two FINDINGS finishers, renders the spec and runs the
once-check over it. The self-test passes 521/521. The sibling sweep found that the other code that builds a pred
(the extra branch-work clause, DRAIN and `continuation_pred`) adds its clause once per group, so none of it repeats.
