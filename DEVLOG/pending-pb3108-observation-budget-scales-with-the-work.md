## PB3108: the test harness's budget for a child process scales with the work it was started to do

**The defect.** The test harness bounded every child process it observed by one fixed 120 s, sized for one program
(compile it, or run it). The conformance suite's two-runner drift test,
`ConformanceSuiteRunnerDriftTests.BothRunners_GiveEverySuiteTestTheSameVerdict`, handed that budget to ONE `run.py`
process that compiles and runs the whole suite. It went RED on load alone eight times between 2026-10-18 and
2026-10-22: on the landing lane's Linux gates, on a clerk's notes-only landing, and on CI's merge group (PR #400, run
39867759611), always a HARNESS NON-OBSERVATION with `python` killed at 120 000 ms on both attempts, while the same test
passed in 38 s to 13 min elsewhere.

**The re-probe.** The suite is 39 tests at four editions, not 2,000 programs: `run.py` starts 78 processes for it (35
accepted tests compile and run; 4 flagged tests compile their control and themselves), 8 at a time, 29.9 s on a quiet
32-processor host, the slowest test 9.3 s. With the unit at 20 s (`COBOLNET_RUN_TIMEOUT_MS=20000`, still twice the
slowest test) the old drift test was a non-observation on both attempts. The observation log named the command as a bare
`python`: the observer printed `ProcessStartInfo.Arguments`, which a caller that builds `ArgumentList` leaves empty.

**The fix: one budget rule over the work.** `tests/_shared/ObservationBudget.cs` gives every observed process one
harness unit (120 s, `COBOLNET_RUN_TIMEOUT_MS`) plus one per serial round of the child processes it runs,
`1 + ceil(children / parallel)`. A program or a script is `OneProcess`. `ProcessObserver.TimeoutMs` is deleted and
every caller moved; `PythonInstrument.Run` takes the budget as its first parameter, so every caller states its work.
The drift test observes `run.py` once per generated edition suite with `--jobs` stated, at
`ForChildren(2 x tests, jobs)`; with the same 20 s unit it passes (3 of 3, 40 s). A timeout now reports the budget and
the work it covered, the process's own processor time, and the host's load across the attempt
(`tests/_shared/HostLoad.cs`: the busy share from `/proc/stat` or `GetSystemTimes`, and Linux's load averages), so a
reader can tell a starved process from a hung one. The unit stays the floor for one program: 120 s against a recorded
cost of at most 9.3 s for a suite test and 7.0 s and 3.8 s for the two single-program witnesses in the note, a
multiple of 13 to 32 for load.

**The log is kept.** Every CI `dotnet test` step writes `harness-observations.log` into its results directory and its
job uploads it with the TRX, which `CiHarnessLogDriftTests` holds (a planted-negative test shows it fires on a missing,
a misplaced and an unuploaded log). The local gate writes one log per leg host into its run directory
(`run_gate_legs.py`'s `leg_env`, a new self-test arm), and the Linux gate one per leg. The observer creates the log's
directory, since `dotnet test` creates its results directory only when it writes the TRX.

**Tests.** `ProcessObservationDriftTests` gains the rule as a nine-row table, its edges (one unit for one process, the
int clamp, the refusals), a slow child observed under the budget of its work and a non-observation under one unit whose
report names its work, its processor time and the host's load, the load line's arithmetic, and the command line with
an `ArgumentList`. `NoWallClockAssertionDriftTests` treats a literal unit handed to `ObservationBudget.For` as a literal
time limit.

**Rows.** None: a test-harness defect with no ISO rule (`closes_rows: []` with its reason).

**Self-review.** Architecture: one budget rule in one type, read by the observer, the interpreter probes and every
script caller; the run.py hang guard stays run.py's own. Code: the in-process runner's NO VERDICT for a program killed
at its budget and run.py's NO VERDICT for its 600 s hang guard are still compared as verdicts by the drift test,
reported as a lead. Performance: two cheap reads of the host's processor times per observed process. Duplication: the
run.py parallelism formula is stated once in the drift test and passed to run.py.
