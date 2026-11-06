## Entry 3080 — 2026-11-04 10:12 PST — PB2293's six superseded campaign branches are abandoned

The PB2293 campaign left six branches that `prune_worktrees.py` could not place: three DECISION rows (667-1, e13-1, a07-1)
and three in-flight rows they replay (8f8-1, 596-1, 549-1). Every note they carry is `landed` on main, most of them through
wave 20261856 A (#1101). `git cherry` against origin/main finds each branch either fully patch-equivalent or holding only
WIP checkpoints whose rebased twins, with the same subject, are on main (fc8ae19a3, 7bbf21969, 7baeb8559, 17f35eb9f). The
files they add that main lacks are only unstamped `DEVLOG/pending-*.md` entries, stamped when the carrying branch landed. The
note PB2293 now records each branch ABANDONED with its reason, so `prune_worktrees.py --apply --abandon` can remove them.

