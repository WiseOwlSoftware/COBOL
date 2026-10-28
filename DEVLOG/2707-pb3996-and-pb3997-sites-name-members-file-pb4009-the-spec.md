## Entry 2707 — 2026-10-28 14:46 PDT — PB3996 and PB3997 sites name members; file PB4009 (the spec renderer states a root-only site twice)

Planning wave 20261742 for PB3996 and PB3997 failed. PB3996's `**Sites:**` line named only `CallBinder.cs`, which is also
the group's root, so the rendered spec stated the same file twice. `check_practices.py` refused it under PB3777's
states-twice check, and the planner wrote nothing.

Both notes' Sites now name members, as the register's convention asks: `CallBinder.cs#BindCall` for PB3996, and
`DataBinder.cs#ValidateNumericValue` with `AlgebraicRange.cs#Of` for PB3997. The renderer's own defect is filed as PB4009:
a spec its checker refuses whenever a note's site is exactly the root file.

