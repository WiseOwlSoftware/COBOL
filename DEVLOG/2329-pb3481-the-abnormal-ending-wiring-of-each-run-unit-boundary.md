## Entry 2329 — 2026-10-21 12:53 PDT — PB3481: the abnormal-ending wiring of each run-unit boundary of PB3337 now has a witness that can fail

Two low leads from the branch review of PB3337 (kb/Work PB3481). No runtime code changed: R1-5 was a test-coverage gap
over code that was already right, and R3-2 was answered on main by PB3483 while this branch waited to land.

**R1-5, the abnormal-ending wiring.** `RunUnit.NoteEnding` records how a run unit ended, as an exception filter at
`ProgramTable.RunOnThisThread`, at `RunUnit.Run` and, called directly, at `ProgramTable.CallFromHost`; `RunUnit.Terminate`
then chooses the file-only ROLLBACK (ISO 9.1.18.5, 14.6.12) over the implicit CLOSE's COMMIT. Only `RunUnit.Run` had a
witness. PB3337's note said the other boundaries "need a compiled program (PB3338)", and the re-probe showed they do not:
`ProgramTable.RegisterModule` takes any `ICobolProgram`, so a stub program that writes a record under commit control and
then returns, STOPs RUN, or fails reaches `RunMain` and `CallProgram(name, null, ...)` (the host boundary) as a compiled one
would. `A_program_boundary_commits_a_normal_ending_and_rolls_back_an_abnormal_one` runs nine cases: the main boundary for a
return, a STOP RUN, a `CobolFatalException`, a `CobolImplementorFatalException`, a `CobolCallException` and a failure no
condition names; the host boundary for a STOP RUN and the two fatals. Removing the `RunOnThisThread` filter fails the four
main-boundary rollback cases; removing the `NoteEnding` call in `CallFromHost` fails the two host rollback cases.
`RunModule` shares `RunOnThisThread`, and its own extra step (the process exit code) is not exercised here because the
runtime writes `Environment.ExitCode` there and a test must not set its host's.

**R3-2, the retake witness.** The branch first answered it with a READ ONLY form of
`An_unsuccessful_reopen_leaves_the_kept_file_lock_standing` and two `SidecarColumnLockHostDriftTests` cases over
`RunUnitFileLock.Retake`. Main then landed PB3483, which deleted the give-back-and-retake itself (the kept column is
replaced on its own hold by `RunUnitFileLock.Amend` and put back by `Revert`), rewrote that case into a discriminating
witness of the new mechanism and added `RunUnitFileLockAmendTests`. Merging origin/main conflicted in the case; the
resolution keeps main's form and drops the branch's `Retake` witnesses, whose method no longer exists.

Rows: none (the note claims no inventory row and closes none). Design: 2.2.1.10's status paragraph names the boundary
witnesses. The gate's verdict line is recorded in the report.

