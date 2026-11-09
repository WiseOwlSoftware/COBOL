## Entry 3355 — 2026-11-08 19:50 PST — PB5008: the 1985 audit rechecks its KEPT and CHANGED decisions; the refuter keeps REMOVED

kb/Work PB5008 asked whether the 1985 audit's refute pass is an independent check. It upheld 1,345 of the 1,371
adjudications it read. A sample audit by four `cobol-adjudicator`s found 5 of 18 upheld CHANGED decisions and 3 of 15
upheld KEPT decisions wrong. The refuter had upheld all 8. The 12 REMOVED and 15 map + verify KEPT records it sampled
were right. The refuter ran the same model at the same effort over the same candidates, so its uphold rate measured
agreement, not accuracy. The errors came in three kinds:
- a 1985 rule elsewhere already allows what the record calls new;
- the record leaves out the 2023 rule that states the requirement;
- 2023 turned a compile-time refusal into a run-time exception condition.

**The fix** (`scripts/spec/audit_batch.py`; DESIGN-spec-conformance-review.md §10.2, §10.3). The new `recheck` pass
sends one request per KEPT or CHANGED adjudication, at effort `max` with 32,000 output tokens. Each request tries to
disprove the decision and carries what each kind of error needs:
- the whole 1985 clause and the ten other 1985 rules most like the rule by wording;
- every rule, of every kind, of the 2023 clauses that hold the feature and of each clause a decided rule lies in;
- the map's word candidates beyond those.

The answer is the change record the rechecker finds. `upholds` accepts the decision only when that record is the same
kind into the same set of 2023 rules. A rule the rechecker adds is one the decision left out, so the rule goes to a
`cobol-adjudicator`. Each decided kind has one check, read from its `to` flag (`checker`): the recheck for KEPT and
CHANGED, the refuter for REMOVED. The refuter's prompt now has it write the strongest counter-reading before it judges.
Any check that answered and disputes a decision makes it contested (`dispositions`), so the run's earlier refute answers
on KEPT and CHANGED decisions still count. `change-batch` skips a rule whose stored record came from another source, so
re-running it never undoes a fleet adjudication. The map pass's wording rank became `Wording`, and the recheck uses it
to rank the 1985 rules. `subject_clauses`, `feature_clauses`, `quoted_rule` and `quoted_2023` now each live in one place,
shared by the map and recheck plans.

**Corrections.** 7 of the 8 wrong records were rewritten through `record_verdicts.py --edition 1985`, with source
`adjudication kb/Work PB5008`. The 1985 GAP count fell from 1,087 to 1,085. The eighth, `1985-GR-VII.1.3.5-4`, maps to
the "Logic error." item of §2023:9.1.13.1. That item is prose the 2023 catalog does not hold as a rule, so no `to` can
name it, and the record stays as it is until the catalog holds the list.

**The run.** The recheck pass planned 1,056 items on `audit-1985/state-route` and sent a 20-item canary. kb/Work PB4995
owns the full submit and the adjudication of what the recheck disputes. Until then the store keeps the earlier records,
but the run counts them as decided only once their recheck answers.

**The drafts plan reads answers (review R1-1).** Withholding `change` until the recheck answers shrank `plan_drafts`,
which read `change`: the run's held 965 drafts re-derived as 930, and `write_plan` would have refused every later
re-plan. `plan_drafts` now takes an OWN-ROW rule and every rule an adjudicate or recheck answer reads as CHANGED,
REMOVED or OPEN. Answers are only ever added, so the plan only grows. On `state-route` it re-plans to 989 and keeps all
965 held items.

**Self-test.** `test_audit_batch.py` covers:
- which check each kind takes;
- the recheck plan's 1985 and 2023 context and its cache layers;
- its answer shape, including a KEPT answer refused when it reports an exception condition;
- what `upholds` compares;
- a disputing check that answered;
- a fleet adjudication the run's batch does not overwrite.

