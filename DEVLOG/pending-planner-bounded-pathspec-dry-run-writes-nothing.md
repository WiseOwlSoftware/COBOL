## The planner survives a mid-merge worktree, a dry run writes nothing, and a wave and a campaign are planned in one directory

Wave 20261737 group A, the three open PB1957 notes on the wave planner (`scripts/orchestrator/plan_wave.py`,
`inflight.py`, `test_plan_wave.py`).

**PB3786 (crash).** `inflight_units` put a worktree's whole uncommitted shared set on `git diff -U0 HEAD`'s command line.
A finisher in the middle of `merge_drivers.py merge origin/main` held 3,331 changed paths and every plan died with
`WinError 206`. Re-probed on a 300-file set with 120-character names (over the 32,767-character limit): the same
`FileNotFoundError`. The committed arm had the guard (PB3582) and the uncommitted arm did not. One rule now serves both:
`inflight.bounded_pathspec` (past `PATHSPEC_CHARS`, no pathspec) and `inflight.shared_hunks` (keep the shared files' hunks
of the pathspec-free diff; the committed arm used to read every file of the whole diff, not only the shared ones). The test
plants the mid-merge worktree and asserts the pathspec form, the bounded form (same members) and the real size. Sweep of
computed path lists on a git command line: `land_lane.conflict_brief` (`git log -- *files`, the conflicted files, bounded by
what a finisher resolves), `devlog.py` (`rm -- *removed`), and constant lists elsewhere; the conflict brief is the one
caller that would need the rule if a merge ever conflicts in thousands of files.

**PB3803 (a dry run that writes).** `--dry-run` wrote `clusters-open.json`, `clusters-half.json` (and their `-notes` copies),
`clusters-campaign-<lead>.json` and, a write the note did not name, a regenerated `member-index/<treeKey>.members.json.gz`.
`main` now owns a `contextlib.ExitStack`; a dry run plans in a temporary directory the stack removes, and
`IndexPolicy.record_dir` sends a regenerated member index there while the coordination directory's cache is still read.
The test snapshots a coordination directory (mtime and bytes) around a dry run of the fix lane and of a campaign, red on the
old code, and checks that the same plan without `--dry-run` records its views and prints the dry run's table. The other
`--dry-run` flags in `scripts/orchestrator/` (`adopt_main`, `cancel_superseded`, `enqueue`) each return before they change
anything and have self-tests; `orchestrate.ps1 -DryRun` creates its own logs and `scratch/dryrun-<pid>` by design. A duplicated
`FIX_CLUSTERS` line in `clusters.py` was removed.

**PB3746 (a watch point).** One test plans a coordination directory holding a campaign's SPLIT branches, an unanswered
campaign dispatch in the real ledger and the fix lane's clusters on the same files: the campaign branch is finished once,
the fix-lane group on its file follows that finisher, the dispatched note and a live agent's branch wait, and the free file's
group runs beside everything. It passes on the current planner.

`docs/rearchitecture/DESIGN-orchestrator-loop.md` carries both rules (the coordination-directory table, the planner's
outputs, the units read). The three notes are landed with `closes_rows: []` (process tooling, no inventory row).
