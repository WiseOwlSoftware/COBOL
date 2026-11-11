## The controller's decisions on the program-runner design (2026-11-11, Mythos session, under the owner's delegation)

The design beside this file (`2026-11-11-design.md`) was written read-only by an Opus planner from the diagnosis in
`E:\COBOL-coord\scratch\gate-perf\findings.md`: a Conformance run starts about 8,000 processes, one per compiled program,
and on this Windows box process creation degrades from about 7 ms to between 0.5 and 2 s with uptime and load, which turns
a three-minute run into an hour while the CPU sits idle.

**What is decided now.**

1. **Stage 0 is built, on its own merits.** One way for a test to run a compiled program (`ProgramRun.Run`), replacing
   the two that exist today (`CutRunner.RunExit` in Conformance and `WiseOwlCobolTestBase.RunAssembly` in 47 Unit files,
   which pin the locale differently and normalize differently: a rule 4 duplication). Launch counts per kind and a
   process-creation probe go into `verdict.json`, and the gate's first lines print the probe, so a sick box says so
   instead of looking like a slow gate. The probe is reported and never asserted. It starts after kb/Work PB5230 lands,
   because both edit `tests/_shared`.
2. **Stages 1 to 3 wait for one measurement.** The owner has been asked to reboot the box and to restart or update the
   APC PowerChute service, whose agent holds 1.37 million handles. If that keeps process creation near 7 ms, the runner
   buys 10 to 30 percent on Conformance and is not worth its machinery (a runner executable, a pool, six detectors and
   an oracle): it is not built, and this note records why. If the degradation returns within days whatever is done,
   Stage 1 (start children without .NET's process-creation lock) and then Stages 2 and 3 are built in the order the
   design gives.
3. **The design's three questions, answered for the case that the later stages are built.**
   - D1: the merge group stays in PROCESS mode. The shipped shape of the product (a program that is its own process)
     remains the net before main; CLAUDE.md's Testing rule gains the sixth first-run class the design words.
   - D2: a case that must run as a process is pinned on the test, `[ProgramRunsAsProcess(Note = "PBnnnn")]`, with a drift
     test that fails once the cited note is closed.
   - D3: both product questions are decided on their own merits, each by its own note and with host-level witnesses,
     never to suit the runner. (i) A run unit hosted in a custom AssemblyLoadContext is a way an application embeds the
     product, so the sibling-module probe loading into `AssemblyLoadContext.Default` (`ProgramTable.cs:1107`) is a
     defect to file now. (ii) `RuntimeConfig` entries stating when each is read (per use, per run unit, per process) is
     part of the documented embedding contract and is worth having whatever happens to the runner.

**The constraint that binds every stage:** no test-only seam in product code. A change to the runtime is made only when it
is the product's own hosting interface, and a case that differs between the two modes is a finding about the mode, pinned
by a recorded reason, never passed silently (owner 2026-11-10: "If it cannot be resolved, there is a problem/bug").
