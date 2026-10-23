## Entry 2391 — 2026-10-22 21:42 PDT — status_guard.py blocks a read-only reviewer: filed as PB3560

The round-4 reviewer of wave 1152 group A (PB3536) was refused at turn end by the STATUS-AT stamp guard, because the orchestrator's own docstring commit had moved the branch head past the stamp in STATUS.md, though a read-only reviewer may write nothing in that worktree; the hook let the stop through only on the second try. PB3560 records the fix: apply the guard only to roles that write the worktree (implementer, finisher), recognized as the write-refusing hooks recognize read-only roles, and have the orchestrator's own commits restamp STATUS.md in the same step.

