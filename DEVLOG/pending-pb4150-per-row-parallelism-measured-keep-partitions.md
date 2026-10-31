## PB4150 measured: per-row test parallelism does not beat the partition classes after M6, so the partitions stay

kb/Work PB4150 asked whether a framework that runs a test class's rows in parallel (Meziantou.Xunit.ParallelTestFramework)
now beats the hand-written partition classes, since DESIGN-test-build-ci.md section 3.11's verdict that the class-split
lever was exhausted had been measured under the ANTLR lexer lock that M6 removed. This landing records the experiment
and the determination. No code and no package change.

**Re-probe.** The note's premise holds on main at `3abfbbdbb`: the profile of an arm-A trx
(`scripts/profile-test-parallelism.py`) shows every partition class with sum equal to span (serial), and xunit 2.9.2
decompiled confirms one collection per class and the `conservative` algorithm's semaphore of `maxParallelThreads`
collections.

**The experiment.** The Conformance assembly (19,881 cases; compiled-program cache off, server GC, 32 threads) in five
arms, alternated round by round, each run inside a gate slot: A main as it is; Ag A with `parallelAlgorithm:
aggressive`; B A under Meziantou 2.3.0 with `aggressive`; B2 B with the conformance suite collapsed into one class; A64
A at 64 threads. Medians: A 197 s (n = 9), Ag 218 s (9), A64 212 s (3), B 234 s (6), B2 176 s (6). The host was shared
with the fleet's gates throughout (0–4 other slots held, system CPU 55–96 %), and arm A alone ranged 170–277 s
(standard deviation 43.5 s). B against A in the same round ran from 54 s faster to 161 s slower. The pass sets were
identical by name key in every run, except the two drift tests each variant trips by construction (the assembly's
framework, and B2's partition floor).

**What it found.** Under xunit's default algorithm the package is unbounded: its assembly runner skips xunit's
collection semaphore and starts every case with `Task.Run`, and the first arm-B run reached 1,720 threads, timed a child
out twice in `DisplayUponTests` and was stopped at 10 minutes. With `aggressive` it is bounded, but it schedules the
assembly's long pole (`ConformanceSuiteRunnerDriftTests.BothRunners_GiveEverySuiteTestTheSameVerdict`, one case of
50–205 s) behind thousands of rows: the pole ended four of six B runs, against one of nine A runs. It also lets 32
NIST rows run at once where the six NIST partitions allow six. It names its own `[assembly: Xunit.TestFramework]`, which
cannot coexist with `GateTestFramework`. A mid-run stack sample showed 26 of the 32 test threads waiting on a child
process: the leg is bound by its threads waiting on compiled programs and by the host's load, not by the class
structure.

**Determination.** Keep the partition classes. DESIGN-test-build-ci.md section 3.11.1 now carries the post-M6
measurements in place of the pre-M6 "exhausted" verdict, section 3.11 names the rejected third lever, and section
3.14.5's M6 paragraph points to the result. PB4150 is landed with `closes_rows: []` (process only). The Unit assembly
was not measured, since B did not clear the bar.

**Self-review.** Every number in the design doc and the note was re-read against the run records (the walls table, the
pole start and end times, the paired differences and the thread counts). One claim was corrected before commit: the
first draft listed `process-globals` classes as racing under per-row parallelism, but the package leaves an explicit
collection serial; only `AcceptSourceTests` (`Console.In` in each of five facts) would race.

**Gate.** See the branch's gate record.
