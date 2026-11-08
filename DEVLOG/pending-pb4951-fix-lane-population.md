## PB4951: the fix lane plans every harmed note `next` ranks, not only `kind: defect`

After the PB4949 audit flagged six analyses, ledger v337 still listed them among the open compiler notes no lane plans,
along with PB4929 and PB4923, the two notes `work.py next` ranked first. The cause was two predicates for one question.
`next` ranks any open, ready, harmed, not process-only note. The planner's population was `.agent-fleet.json`'s
`kind: defect` and `skip_flag`, and `ledger_plan.lanes_of` read the same keys. So the loop never dispatched the work the
register put at the top of its list.

`work.fixable` is now the one predicate. `actionable` uses it, `clusters.ready_notes_view` copies only fixable ready notes
for `fix_clusters.py`, and `ledger_plan.lanes_of` gives the fix lane exactly when it holds. `.agent-fleet.json` drops
`kind` and `skip_flag`, and `ledger_plan.remaining` drops its `fleet` argument, with every caller changed. On the register
at e72a54d641 the planner's view is the 14 notes `next` lists, and the notes no lane plans fall from 16 to 7, all of them
decisions that await the owner. New self-test cases in test_plan_wave and ledger_plan pin a harmed analysis in and an
unharmed or process-only note out. The orchestrator-loop design and DOC_INDEX describe the new population.
