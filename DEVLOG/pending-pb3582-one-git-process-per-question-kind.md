## PB3582: one git process per question kind, never one per object (scripts/git_batch.py)

The script self-tests summed 1,444 s on CI's Windows runner and 144 s on Linux (merge-group run 40004159292). On this
branch GIT_TRACE2 counted the git processes each hot self-test starts: `prune_worktrees.py` 3,048, `landing_check.py`
1,471, `enqueue.py` 646, `devlog.py` 363. Nearly every one asked one question about one object: a `merge-base
--is-ancestor`, a `rev-list --count` and a `log -1` per branch, a `log -1` and two diffs per commit and parent, a
`rev-parse` per file. A Windows process start costs tens of milliseconds, so these scripts were bounded by process
creation, and the same loops run in every session's survey, landing check and planner. Four of the note's hot
self-tests turned out not to be git-bound at all (`guard-verify.sh`, `test_forbidden_commands.py` and the
`test_orchestrate_*.ps1` parts start bash, Python and pwsh, not git); they are returned as leads.

The fix is one module, `scripts/git_batch.py`, that answers a whole list of questions from one process each:
`Ancestry` (one resolve and one `git log --stdin <tips> --not <base>`: every commit beyond origin/main with its parents,
tree, date and subject, so ancestry, counts ahead and subjects come from memory; a tip that names no commit raises),
`diff_names`/`diff_patches` (one `diff-tree --stdin --always` over commit/parent pairs) and `object_ids`. The survey,
the landing check, the in-flight sets, the planner's classifier, carrier rule and closed-note credit, `enqueue.py`'s
worktree state and `devlog.py`'s tree reads moved onto it, and each old per-object helper went with them. Measured
after: 1,246, 846, 510 and 286 git processes for the same four self-tests, all GREEN. `git_batch.py --self-test`
holds each reader to the per-object command it replaces, and `GitBatchDriftTests` keeps per-object ancestry
questions out of the survey, the landing check and the planner. The design is DESIGN-test-build-ci.md section 3.14.10
item 5. No compiler behavior changed and no inventory row closed.
