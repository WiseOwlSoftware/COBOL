**2026-11-15: branch `adopt-2002-s1` is ABANDONED (operator decision).**

Branch `adopt-2002-s1` ABANDONED: its two commits, f377f29d4e and 8ebed7c4e0, say "not for landing" ("adopt-2002 S1 step 1:
1,313 KEPT records by rule (unflagged exact pairs), not for landing"; "adopt-2002 S1 step 2: widen 500 paired targets'
cases to accept 2002 (measurement, not for landing)"). They were the S1 measurement, and their results are
recorded in this note (PB5574) and carried forward by PB5679 (`2026-11-15-superseded-by-pb5679.md`). Nothing on the
branch is to land; `python scripts/prune_worktrees.py --apply --abandon adopt-2002-s1` may remove its worktree and
branch once this record is on main. Recorded by the registrar landing `records-1985-reread`.
