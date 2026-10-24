## Entry 2475 — 2026-10-24 13:42 PDT — PB2699: the loop adopts landed changes by itself, between units, once CI is green on them

The loop plans from one main checkout, and every landing is pushed from a worktree, so the checkout fell one landing
behind origin/main with each merge. Wave 1042 planned from a register seven commits old, wave 1079 ran a planner older
than the one on main, and a landed process fix did not take effect until the operator fast-forwarded the checkout and
restarted the supervisor by hand. The owner (2026-10-24 ~10:58 PDT): "fix the loop so it picks up landed changes itself.
We should adopt efficiency improvements as soon as they are available and stable".

Re-probe: still reproduced on this tree. `autostart.ps1` held the only pull, `orchestrate.ps1` never touched the
checkout, and `plan_wave.py` planned from any checkout without a word.

Fix, at the one place: `scripts/orchestrator/adopt_main.py adopt`. It fetches origin/main and fast-forwards the checkout
only when the checkout is on main, strictly behind, and has no tracked modification but `settings.local.json` (never a
stash, a merge or a reset), and only when CI's push run on main at that commit has succeeded; a red, pending or
unrecorded run, an unreadable answer and a missing GitHub CLI keep the last adopted commit and say why (an unobserved
CI is never a pass). It records the adopted commit in `<coord>/adopted-main.json`.

- `orchestrate.ps1` runs it at the top of every iteration, between units (a hold that ends loops back through it; a dry
  run says it would and adopts nothing). When the fast-forward changed `SUPERVISOR_FILES` (the one script the running
  process holds in memory; the Python tools, `units/*.md`, the schema and `steer.ps1` are read afresh at each use) it
  re-executes itself from the checkout: lock released to the successor, its arguments passed, `-Unit` (consumed at start)
  dropped, `-Cluster` as the lanes still running, and the in-memory counters carried (`-CarryFailures`, `-CarryRan`,
  `-CarryLastStarted`: the circuit breaker, `-MaxUnits` and the campaign alternation survive). New seam `-GhExe`.
- `autostart.ps1` calls the same routine; its inline `git pull --ff-only` is deleted.
- `plan_wave.py` exits 2 naming both shas when its checkout lacks the adopted commit; `--allow-stale` plans from it
  knowingly. The floor is the adopted commit, not origin/main, because a red main is deliberately not adopted.
  `units/wave.md` tells the wave unit to end `failed` on that line, never to pass `--allow-stale`.
- Design: `DESIGN-orchestrator-loop.md` section 4.10, the parameter list, section 10a and the test inventory.

Tests (self-tests, found and run by `scripts/self_tests.py`): `test_adopt_main.py` (adopted, current, red / pending /
unrecorded / unreadable / garbled CI, a green re-run of a red run, the restart trigger, a tracked modification, the
settings file, an untracked file, ahead, diverged, off main, a dry run, the guard, a missing CLI, `plan_wave.py`
refusing) and `test_orchestrate_adopt.ps1` (a stable landing adopted between units and the unit then runs; a red main
kept and the unit still runs; a supervisor change restarting it with arguments, counters and the lock handed over; a
dry run). Also green: the core, ledger and autostart supervisor parts and `test_plan_wave.py` (338/338).

Review round 1 (R1-1, blocking): the restart trigger was computed only from the adoption's own fast-forward, but the
attended session also fast-forwards the checkout by hand when `ledger_state.py` says a ledger publish is owed, after which
`adopt` reports `current` and a supervisor on an older script ran on indefinitely. Fixed at the root: the supervisor passes
the checkout HEAD it loaded its script at (`--loaded`), and `supervisor_changed` is now the diff of `SUPERVISOR_FILES`
between that commit and the checkout now, in every outcome. `test_adopt_main.py` section 4b covers a hand fast-forward
(current), a kept outcome, no loaded commit and an unreadable one; `test_orchestrate_adopt.ps1` still proves the wiring.

Sweep: the other readers of the checkout (`work.py`, `alloc.py`, `next_unit.py`) are called by the supervisor and the
units after the adoption, so they read the adopted tree; only `plan_wave.py` plans from it and carries the guard.

