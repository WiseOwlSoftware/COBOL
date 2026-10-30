## The loop's adoption of main no longer races another fetch (kb/Work PB4046)

`test_orchestrate_adopt.ps1` failed in CI's Windows self-tests twice on 2026-10-29 (merge-group runs 40918334805 and
40987962746), and the loop's PB3797 finisher found why. The supervisor started the landing lane's tick in the background
and ran `adopt_main.py` at once. The tick's survey ran `git fetch` in the same checkout, both fetches updated
`refs/remotes/origin/main`, and the one that lost failed with "cannot lock ref". When the adoption lost, it kept the old
commit for a whole unit. The self-test passed locally only because an authenticated `gh` slowed the tick's GitHub reads by
1 to 3 seconds; CI's `gh` has no token and fails at once.

Three changes remove the race and the gap that hid it:
- `adopt_main.py` fetches origin's main into a ref only it writes, `refs/adopt-main/main`, with `--refmap=` and
  `--no-write-fetch-head`. No fetch elsewhere in the repository (a landing, a finisher, another session) can fail it.
- The supervisor adopts BEFORE the iteration's lane tick, and never beside one: `Wait-LaneTick` waits up to 120 s for a
  tick still running, and one that outlasts the wait defers the adoption to the next iteration.
- The orchestrate self-tests put a `gh` that fails at once first on every supervisor run's PATH, as on CI, so local
  timing can no longer hide what CI sees.

The tests involve no timing. `test_adopt_main.py` section 10 holds the lock a concurrent fetch would hold on
`refs/remotes/origin/main` and requires the adoption to go through. On main's adopt_main it fails with CI's own message.
`test_orchestrate_adopt.ps1` scenario 6 runs a four-second fake tick and requires that no adoption interval overlaps a
tick's (29/29). On main's supervisor the overlap is caught (28/29).
