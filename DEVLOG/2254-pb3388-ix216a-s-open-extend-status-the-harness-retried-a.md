## Entry 2254 — 2026-10-19 10:47 PDT — PB3388 - IX216A's OPEN EXTEND status: the harness retried a killed program over its own leftover files

**The observation.** One Linux gate red in 17,812 (`NistProgram_MatchesGolden("IX216A")`, head c95af8611, 6 m 22 s):
`OPEN EXTEND: EXP: 05 FAIL* OPEN-TEST-GF-01`. IX216A opens an absent OPTIONAL indexed file with OPEN EXTEND and expects status
05, because section 14.9.27.4 GR17 (checked with `cite.py`) makes that OPEN create the file and then set the status to '05'.
The note asked for a repro and a root cause, not a re-gate.

**What the re-probe showed.** On a current build IX216A passes in a fresh directory, and run a SECOND time in the same directory
prints exactly the red's line (`OPEN EXTEND: EXP: 05 FAIL* OPEN-TEST-GF-01 ... IX-3; 1.3.4 (1) D; STATUS AFTER OPEN EXTEND`),
because the first run created `xxxxx025.txt` and the OPEN now finds it present (00). The runtime is right in both cases. What
could put a program in a non-fresh directory in a run that uses a GUID directory per program? `ProcessObserver.ObserveOrThrow`:
a child that does not complete in the 120 s budget is killed and re-attempted once, serialized, in the SAME directory, on the
stated ground that "nothing was learned on the first attempt, so there is no result being discarded". That ground holds for a
child that leaves nothing behind and not for a program that writes files. The red's 6 m 22 s is longer than the budget plus the
serialized retry could take unless a retry happened. The Linux run's `cobolnet-harness-observations.log` was not kept, so that
the red run retried is inferred, not read; the failure signature is reproduced exactly. The keyed-store engine did not change
the odds: the hazard does not depend on the engine, and the SM/RL/IX/SQ chains, every ACCEPT and every file-writing test
through `CutRunner` carried it.

**The fix.** A retry is a measurement of the same input, and a program's input includes the directory it runs in.
`tests/_shared/RunDirectoryState.cs` records the run directory before the first attempt (files up to 1 MiB by content, so a
same-length in-place REWRITE is seen; larger files, the copied runtime assembly among them, by length and write time) and
`ObserveOrThrow(..., runDirectory:)` restores it before the retry: files the killed attempt created are deleted, files it
rewrote or removed are put back. A large file the killed attempt changed cannot be put back, and the retry is then not made: a
`HarnessNonObservationException` that names the file, never a value measured over another input. The three call sites that run a
compiled program (`CutRunner.RunExit`, which `NistRunner` and ~700 tests go through, `AcceptDifferentialTests.AcceptRun`,
`CobolNetTestBase.RunAssembly`) name their directory; a tool run in the repository names none and is not touched.

**Sibling arm.** `scripts/guard-fast.sh`'s serial re-observation (the other retry in the tree) re-runs the group through
`guard-run-group.sh`, which makes a fresh `mktemp` directory per invocation, so it never ran over leftovers; recorded in
DESIGN-test-build-ci.md 3.10. The drift test `EveryRunOfACompiledProgram_NamesItsRunDirectory` keeps a fourth `dotnet <dll>`
call site from being added without the argument.

**Tests (ProcessObservationDriftTests, the owner's I-O exception-path rule).** The retry policy is its own method, `ProcessObserver.ObserveWithRetry(command, budget, runDirectory, attempt)`, and the tests drive it with an in-process attempt: the first attempt creates a file, rewrites a data file in place and reports TimedOut, as a killed program leaves them; the second reports what it found. A retry restores the created file, the in-place rewrite and the subdirectory (exit 0, not 9); an unrestorable large file raises with the file named and `attempt2: not run` and the retry is never made; a caller that names no directory keeps the old behaviour (exit 9), which pins that the argument is what restores; a retry that also times out raises after restoring. The first version ran a real cmd.exe or sh child under a literal 1 s budget, so which attempt a test observed depended on host load (branch review R1-1, kb/Work PB1590, PB3022): fixed at the root by separating the policy from the process launch rather than widening the budget. The process-level wiring stays covered by the existing never-finishes tests. The runtime's own I-O statuses are untouched.

**Register.** PB3388 landed, `closes_rows: []` (a harness defect; no inventory row changed verdict).

