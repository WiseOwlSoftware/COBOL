## PB3481: the abnormal-ending wiring and the kept-lock retake of PB3337 now have witnesses that can fail

Two low leads from the branch review of PB3337 (kb/Work PB3481). No runtime code changed: both were test-coverage gaps
over code that was already right, so the work is two witnesses, each proven able to fail.

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

**R3-2, the retake witness.** `An_unsuccessful_reopen_leaves_the_kept_file_lock_standing` held SHARING WITH NO OTHER, whose
kept handle is `FileShare.None` and refuses the second run unit by itself on every host, so deleting both
`RetakeKeptFileLock` calls left it green. A holds SHARING WITH READ ONLY now (`FileShare.Read`, a `LOCK_SH` flock on Linux),
so on an advisory-lock host the kept handle admits B and only the retaken Table 19 column lock refuses it. On Windows the
mandatory share mode refuses B and `RunUnitFileLock.HostOfThisProcess` is null, so the connector case cannot discriminate
there. The retake itself is witnessed on every host, over the lock-file host, by
`SidecarColumnLockHostDriftTests.ARetakenLockRefusesAsTheOneFirstTakenDid` (every sharing mode x open mode pair, 144 cases:
given back means admits, retaken means Table 19 refuses, retaking a held lock is idempotent) and
`ARetakeThatMeetsARefusedColumnIsRefusedAndHoldsNothing`. A `Retake` that does nothing fails 145 of the 305 cases in that
class.

Rows: none (the note claims no inventory row and closes none). Design: 2.2.1.10's status paragraph names the boundary
witnesses. The gate's verdict line is recorded in the report.
