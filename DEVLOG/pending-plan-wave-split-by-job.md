## PB3728: plan_wave.py split by job, plan() in five stages, the pipeline scripts' import DAG declared once

The first split of the pipeline-scripts restructure (kb/Work PB3726, tier 2; DESIGN-orchestrator-loop.md §16.3). The
2,472-line planner did six jobs, and every fix appended to it. It is now five library modules with one job each and a
plan in named stages:

- `reports.py`, THE fleet-report reader: a report's name, status line, header fields, review stamp, the one finished
  predicate, the one order, the notes a report answers; and the branch survey's second copy of the reader, moved out of
  `prune_worktrees.py` (its report record is `IndexedReport`, with `report_index`, `newest_report` and `age_minutes`).
- `inflight.py`: what is in flight (worktrees, unlanded branches, the ledger's open groups, the units a diff touches)
  and the ONE meeting rule (`Claim`, `collision`).
- `clusters.py`: the plannable population (notes, a report branch's own lead notes, the notes a branch closes, the fix
  lane's and a campaign's clusters, tiers).
- `groups.py`: a group's shape, model, cost, predecessor instruction, standing requirements and rendering.
- `dispatch_ledger.py`: the ledger's one writer, its hold and its release, and the partition's measurement.
- `plan_wave.py` keeps the plan and the CLI: `plan()` builds a `PlanState` and runs `plan_held`, `plan_finishers`,
  `plan_fill`, `plan_partition` (which letters each group as it places it, since a later group's wait names the letters
  of the chains it meets) and `plan_budget`.

The move was mechanical: a script read the planner's syntax tree and wrote each module's members in their original
order with the comments above them, and a second re-pointed every `plan_wave.<name>` spelling, refusing wherever a
caller's own local name (`reports`, `groups`, `clusters`) would have shadowed the new module; those callers import the
names themselves. No wrapper, alias or re-export is left: no `plan_wave.<moved name>` or
`prune_worktrees.<moved name>` spelling remains under scripts/, .claude/, tests/, conformance-suite/harness/, .github/
or docs/. Two pre-existing unused imports went with it (`devlog` in the planner, `json` in landing_check.py), and
review_findings.py's alias `REVIEW_LINE = plan_wave.REVIEW_LINE` became a read of `reports.REVIEW_LINE`.

Where the drafted table and the code disagreed, the design now follows the code (§16.3 lists each): the notes a branch
carries belong to `clusters.py`, `ledger_slug` to `reports.py`, `DISPATCH_LEDGER` and `ledger_groups` stay in `coord.py`
(they were never the planner's), `report_since` stays for `review_coverage.py`, and `MAKE_SPECS` stays with the CLI. The
survey's branch matcher is NOT the header's branch field: over the 802 reports on disk, 206 headers name another branch
set under it, chiefly a successor naming its predecessor's branch. Unifying them would change which branch the survey
credits a report to, so it is `reports.BRANCH_MENTION` beside `BRANCH_FIELD` and a lead of its own.

The import DAG (§16.10) is declared once, in the new `scripts/script_layering.py` (its lines, its modules with the step
that creates each, and the splits not yet landed), and `ScriptLayeringDriftTests` runs its `--check` in every gate's leg
1. The planner's line is a chain (coord, reports, branch_classify, inflight, clusters, groups, dispatch_ledger,
plan_wave). A module whose split has not landed stands for the modules its split creates, so `inflight.py` may import
`prune_worktrees` for the survey's git facts until PB3731 moves them. An import inside a `self_test` is test code. A
declared module that existed before, or whose step's note is landed, must exist, so a rename never reads as "not created
yet". The audit's self-test plants each rule's violation. `GitBatchDriftTests` now holds the new in-flight readers too,
and `MergeDriverDriftTests` reads the predecessor-merge instructions where they now live, `groups.py`.

The oracle: `test_plan_wave.py` passed 460 of 460 checks with the identical 416 `check(` labels, and the golden
`testdata/plan_wave_default.json` is untouched. Every self-test passed under `scripts/self_tests.py` (119). The two live
dry runs (`--cluster PB1957 --from-budget --dry-run` and the fix lane's `--budget-points 3 --dry-run`) printed
byte-identical plans from the base scripts and the split ones over one coordination state, the base run made by checking
the base scripts out in this worktree between the two. Self-review found and fixed two docstrings that named a reader
the code does not have, a self-test write that stated no `newline=` (the gate's LINE ENDINGS audit), and the audit's
own blind spot (rule 6). No inventory row moved: process tooling, `closes_rows: []` with its reason.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` on bc32eb51a (run 000002-20261026T024004Z,
Linux half GREEN); the final head's gate is in the report.
