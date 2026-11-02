## Entry 3008 — 2026-11-02 12:36 PST — PB4452 follow-up (doc): DESIGN-orchestrator-loop states the multiple-editors rule

`docs/rearchitecture/DESIGN-orchestrator-loop.md`'s description of the planner's grouping (item 1b) now matches the
planner. Past what one implementer holds, a group sharing only files with another runs beside it, and only a group
sharing a member is that group's overflow. `check_practices.py` O2 refuses only two groups that edit one member
(`inflight.hold`). This doc edit landed in its own branch, so the script change (branch `pb4454-o2-members`) keeps the
tooling lane, because a `docs/` path forces the full gate (kb/Work PB4453).

