## PB4419: an absorbed group's pred states every sentence once

Wave unit 20261815 planned group A, ONE step over `DataBinder.cs` (kb/Work PB4396). The step absorbed the finishers of
two branches whose reviews stamped FINDINGS. `finisher_pred` ended each of their preds with the same answer-by-id
sentence, and `Group.absorb` joined the two preds as they were. The rendered spec said the sentence twice,
`check_practices.py`'s PB3777 once-check went RED, and the wave launched nothing.

The first fix moved that one sentence into `groups.FINDINGS_ANSWER` and joined it once. The branch review (R1-1) showed
the shape was wrong, not the sentence: every pred builder writes sentences the same for each branch, so two groups of
any other kind (two branches with no report, two UNLANDED or LANDED finishers, two half notes), the CONTINUATION a
group is planned with, and the `--drain` clause added to every group before the partition, all said theirs twice. The
sibling sweep of the first fix had looked at where a clause is appended once per group, not at what `absorb` does to two
groups that each carry it.

The root fix is the pred's shape. A pred is FACTS (a branch, a note, a report) and RULES (the builder's
same-for-every-branch sentences). `groups.shared_rule` declares a rule; `groups.join_preds` keeps every group's facts in
order and states each declared rule once, at the end, and drops a later copy of any other repeated sentence
(`once_rule.without_repeats`). The six rules (`RESUME_RULE`, `UNLANDED_RULE`, `LANDED_RULE`, `HALF_RULE`,
`FINDINGS_ANSWER`, `DRAIN_CLAUSE`) are now sentences of their own, so a dropped copy never leaves a fact with a dangling
colon. What belongs to the finished group is no longer built per group: `Group.join` records the finished branches a
group is planned onto (`absorb` merges them) and `Group.close_pred`, called once per placed group, states the
CONTINUATION and the DRAIN clause once. The sentence rule itself moved to `scripts/orchestrator/once_rule.py`, which
`check_practices.py` imports in place of its own copy, so the test and the repair read the same sentences. The half
note's reading order had two copies (`plan_finishers` and `branch_work_pred`); `half_clause` is the one.

Reproduced on the unfixed code: ten kinds of pred, joined pairwise as `absorb` joined them, repeat a sentence in all but
the carried pairs; two `continuation_pred`s repeat five sentences and two DRAIN clauses two. The test joins one pred of
every kind with every kind (a sentence twice, a sentence lost, or a branch missing fails), absorbs two groups that each
joined a finished branch and closes them under drain, and checks the drain plan's real absorbed group and the fix lane's
two groups planned onto one finished branch. Self-test 527/527. The rules' wording changed, so
`testdata/plan_wave_default.json` was regenerated; its diff is only the wording of those sentences.
