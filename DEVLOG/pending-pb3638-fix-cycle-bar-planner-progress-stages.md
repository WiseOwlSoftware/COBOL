## PB3638 + PB3446 + PB3641 + PB3650: the loop's fix-cycle bar in code (tier-ordered chains, one link per wave, a 60-minute hand-off, progress lines, per-stage landing measure), one report-coverage reader, the group cap's honest measurement

Wave 20261106 group A (PB1957 campaign), finishing wave 20261031 A, which the operator's 16:39 fleet stop split.
The predecessor's one commit (c90a3f3a0, PB3446 + PB3650) was cherry-picked onto main 302648e01; its conflicts with
PB3653's `locked` and PB3652's survey rows were resolved (the self-test's branch count 66 -> 67).

**PB3446** (planner and survey disagreed on a resumed FINDINGS branch). For a FINDINGS-stamped DONE report followed by
a commit no report describes, `plan_wave.plan` said WAITING FOR REVIEW while `prune_worktrees.lifecycle()` read
DECISION, and no reviewer is ever dispatched for a DECISION row. `prune_worktrees.report_since` is now the one
report-coverage rule, `prune_worktrees.Coverage` (replacing `review_coverage`) answers `.review` and `.report` from one
graph, and the planner's stale-stamp arm says RESUMED where the report does not cover the tip. Drift test: the
prune_worktrees self-test runs both readers over the same three FINDINGS branches (`findings-resumed` was red before).

**PB3650.** `load_reports`' docstring: the name breaks a tie only between reports of one wave written in one tick.

**PB3641** (the group cap's provenance). The PB3611 source string fitted 66 + 13 turns per note from report TITLES
(188 of 316 counted no note), claimed a 190-turn SPLIT onset nothing computed, and carried the wave id as its date.
The measurement is tracked now, `scripts/orchestrator/turns_by_group_size.py` (self-test), and counts the notes on each
report's DISPATCH SPEC first line. On 2026-10-24: 287 reports joined; 1 note n=185 mean 81 turns, 2 n=59 115, 3 n=16
158, 4 n=7 161, 5 n=18 159, 6 and 7 one report each; line about 61 + 23 per note. So a group of 8 sits near 245 turns,
past the 220-turn cap, and groups of 3 to 5 SPLIT about half the time. `wave.max_notes_per_group_source` says so, calls
8 an extrapolation and gives the cap's real reason (the landing rate). `cost.source` says `per_group_tokens` and
`per_note_tokens` are still the unmeasured 2026-10-04 seed and what a token re-fit would need.

**PB3638** (the owner's 2026-10-24 bar: a simple fix in 10-15 minutes; the loop took hours). Four mechanisms:
1. Tier order. Wave 20261029's specs show A and A2 were untiered finishers and A3 the tier-1 PB3612: every finisher was
   placed before every cluster. `plan(tiers=...)` now places all groups in effective-tier order (a finisher takes its
   notes' tier; `lift_tiers` keeps a blocker's group ahead of its dependent's), finishers first inside a tier.
2. One chain link per wave: `model_rules.json` `wave.links_per_chain` = 1 (the owner's 2026-10-23 rule); later links
   wait, named, for the next wave. `orchestrate.ps1 -UnitSoftMinutes 60` writes STOP-UNIT (wind-down `time-soft`,
   the context soft cap's semantics: a running fleet finishes).
3. Progress: `scripts/orchestrator/progress.py` (self-test) prints a PROGRESS line (ledger groups by state, landings
   since the unit started, the oldest waiting item) and STALL lines (running past 60 min, DONE unreviewed past 15);
   the supervisor runs it every `-ProgressSeconds` (600) through its one background starter `Start-PyBackground`
   (the checkpoint and the lane tick moved onto it) and posts each stalled item to the operator's mailbox once. Its
   first live run named w20261031g running 283 minutes.
4. Per-stage measure: `landing_measure.stage_times` records `dispatched` and `review_clean` at enqueue (review_clean had
   0 of 540 records); `summary` prints dispatch -> DONE -> review clean -> enqueued -> merged and the two budgeted spans,
   OVER when the median exceeds the bar (live: enqueued -> merged median 17 min against 15, OVER).

Tests: `test_plan_wave.py` 9c (418/418), `test_orchestrate_stop.ps1` 7d (33/33), the landing_measure self-test's new
arm (11 arms, held by name in `LandingMeasureDriftTests`), `LandingLaneDriftTests` reading the lane tick through
`Start-PyBackground`. Docs: DESIGN-orchestrator-loop.md sections 4.3 and 9, DESIGN-test-build-ci.md section 3.17.5, the
workstream SKILL.md, units/wave.md. No inventory row is claimed or closed (process tooling).

Self-review: one background starter instead of three copies; `_spread` replaces two hand-built median/p90 dicts;
`landing_measure._when` became `iso_time` (progress.py reads it); a stage stamp that would break the record's order is
left null so `validate` never refuses the whole record at enqueue.

Gate (run 000002-20261025T042734Z, on 11f9c688e): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN —
… Conformance 8,158/18,508 · Unit 36,898/38,220 · Characterization 36/36 cases ran (skipped 0) in 1 of 2 leg(s) · Linux
half (WSL clone of HEAD) GREEN …`. The first gate (000001) was RED at the LINE ENDINGS audit (two self-test writes in
turns_by_group_size.py stated no `newline=`), fixed before any leg ran.
