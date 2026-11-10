## Entry 3403 — 2026-11-09 18:59 PST — PB4140 recurs: a decided "superseded" DECISION row loops the resume unit

A resume unit decided to land worktree-wf_30df8fbb-94c-1 (a DONE PB5044 follow-up whose only unique content is the
one-glyph monospaced `M` witness in `test_figure_raster.py`) and named `next_unit: enqueue`. Because the lead note PB5044
is landed, `prune_worktrees.lifecycle` still read the branch DECISION. `next_unit.py` rule 5 does not count a DECISION
row as enqueue work, so the supervisor ran a second resume unit instead. That unit got a diff-only re-review of the
conflict merge f14ed081a8 (CLEAN) and landed the branch directly through `enqueue.py land` (PR #1432). kb/Work/PB4140
records the recurrence and the second cost, and its pinning self-test now also covers `next_unit`'s choice. No compiler
or tooling change; no inventory row closed.

