## Entry 3369 — 2026-11-09 01:26 PST — PB4995: `audit_batch.py seed` checks a batch of fleet decisions with the run's own recheck and refute passes

The 1985 recheck (account2, $229.66) contested 712 change records, and the owner chose a `cobol-adjudicator` decision for
every one, then one more max-effort recheck of those decisions before they stand (approved 2026-11-09, capped at $165 of
account2's API credit). The recheck pass planned only from a run's own adjudicate answers, and a run's plan is written
once, so the fleet's decisions had no way in.

`seed --state NEW --upstream DIR --decisions BATCH` starts a run whose OWN adjudicate pass is the upstream items of the
rules the `record_verdicts` batch decides, each answered by its record (kind, 2023 rules, reason, and the record's
`source`), and which reads the passes `--inherit` names (route, map, verify by default) from the upstream run through
`upstream.json` (`pass_dir`; `Pass.inherited`). Every command but `status` refuses an inherited pass, so nothing writes
into the upstream run. The recheck and refute plans, `results` and `change-batch` then work on the fleet's decisions
as on any adjudication, with no second planner. The self-test seeds a run from a fleet record, plans its recheck from it
with the upstream routes, and refuses an inherited plan, a non-empty directory and a rule the upstream run never
adjudicated.

