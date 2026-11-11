## PB5323 filed; the reboot's process-creation figure and the #1510 bounce recorded

Three register entries the reboot left owed, no code.

kb/Work PB5323 (new, `kind: defect`, `process_only: true`) is lead R2-1 of the group B branch review (pull request #1513):
the OdoDependence roster drift test accepts `Role.AccessPath` for any Place kind that is not a decorator, while `Rebind`
has an arm for three kinds only, so a new leaf kind passes the drift tests and aborts the compiler. The lead was checked
against the branch (`OdoDependence.cs` lines 71 to 73 and 98 to 111; `OdoDependenceRosterDriftTests.cs#EveryRoleMatchesItsKind`)
before it was filed. The note names the fix: one interface over the kinds that carry access paths, so `Rebind` has one arm.

kb/Work/PB5320/2026-11-11-after-the-reboot.md records the figure PB5320's decision waits on: process creation measured
7.4 ms median (min 7.2, max 25.6; 15 launches) three minutes after the 12:27 PST boot, against 2.0 s at 09:02 PST before it.

kb/Work/PB1957/2026-11-11-pr1510-ci-gate-never-started.md records a class (e) finding: pull request #1510 left the merge
queue on `checks_timed_out` with all 19 check runs of its merge commit `success` or `skipped`, because GitHub never
started the required `ci-gate` job. The branch is unchanged and is re-queued once.
