## PB4323 and PB4395 close: the pipeline redesign is on main, and the tooling lane is measured

PR #991 (DEVLOG 2961) put the last steps of the PB4323 pipeline redesign on main. PB4395's one unrun acceptance item is
now measured there: a planted tooling-only change to scripts/orchestrator/steer.ps1, gated twice on a fresh worktree of
10ae76f52, took the tooling lane both times with no build and no test host, in 1,226 s the first time (41 self-tests ran
whose inputs the landing had just changed) and 135 s the second (28 ran, 114 reused). PB4395 and the PB4323 lead flip to
landed; PB4333 stays half by its own design (its differential is built only when the growth trigger fires). The note's
stale "Left" paragraph is replaced by what landed and what stays in C#. Register only; no code changed.
