## A Python self-test that can read the wall clock must also pass with the clock moved forward 400 days (PB4358)

**The defect.** PB4353's self-test planted merge-group runs dated the day before it was written and read them through a
real 24-hour window. It passed on its branch, in its merge group and on main, then went RED for every pull request at
2026-11-01T09:00Z. Two things hid it. No gate or CI job ran a self-test at any clock but today's. And after the expiry
the local gates still answered REUSED/GREEN, because the PASS store keys a recorded PASS on file content only, while
CI, which never reuses, ran the test fresh: the CI invariant broke a second way.

**What the re-probe measured.** PB4353's sweep had shifted ONE clock (Python's) through a `usercustomize` shim and got
6 and 7 false reds, each a test that compares Python's clock with a second, real one (file modification times, a
holder's process start time, git commit times). This session repeated it through the real runner: the shim now sits in
the runner's own `sitecustomize` (the tracer's directory, so every Python child inherits it), and the shift is applied
to a self-test only when it can read the clock.

**The fix.** `scripts/self_test_clock.py` (new) holds the rule. `reads_wall_clock` reads the syntax tree of the tracked
`.py` files a self-test's run opened (the runner's own files left out) and says whether any can call `now`, `utcnow`,
`today`, `time.time/time_ns/localtime/gmtime/ctime/asctime/strftime`; an over-approximation is harmless, since the second
run is the semantic check. `Runner.run_one` then runs such a self-test a second time in the same budget place with
`COBOL_SELF_TEST_CLOCK_SHIFT_DAYS=400`; the shim moves `datetime.now/utcnow/today`, `date.today`, `time.time/time_ns` and
the argument-less `time.localtime/gmtime/ctime/asctime/strftime` by exactly that, once each (the module's self-test reads
every one in a child against the unshifted clock). A PASS is recorded only when both runs pass, with `clock: {shift_days,
until}`; `PassStore.lookup` refuses a record past `until` (`re-run: the clock proof of the PASS of … ended …`), and the
schema is now 3 so no earlier PASS is reused. The first run always starts without the shift variable, so a runner inside
a shifted process still measures the real clock (an arm caught this: the runner's own self-test had run shifted).
A self-test that compares the clock with a real one the shift cannot move declares it among its first five lines:
`# SELF-TEST-CLOCK: real — <why>` (six declare it: `gate_slot.py`, `prune_worktrees.py`, `spec/impact_store.py`,
`orchestrator/test_plan_wave.py`, `orchestrator/test_alloc.py` and the runner's own self-test).

**Chosen over the note's candidates.** libfaketime (candidate 1) would still leave process start times and PowerShell's
own clock unmoved and cannot run on the Windows hosts the implementers use; a syntax-tree-only rule (candidate 2) judges
intent. The hybrid runs the proof where the clock can be read and lets a self-test opt out visibly, with its reason.

**Measured.** A full run on this host shifted 49 of 138 self-tests (all green; 55 reach the clock) and the five Python
ones named above went red for the reason PB4353 gave, until they declared the real clock. The proof adds about 30 % to the
summed seconds of a full run (CI), and nothing to a gate whose reused PASSes are inside their horizon.

**Review lenses.** Architecture: the rule lives in one module (`self_test_clock.py`) that the tracer, the runner and the
PASS store call; no second copy of the clock list (the shim's self-test derives its coverage from the detection's
constants). Full code: the first run scrubs the shift variable, so a nested runner measures the real clock (found by an
arm, fixed). Silent failure: a malformed `clock` record or an unreadable closure file never counts as proof (the record
is unusable; an unreadable file reads no clock, as an un-opened one would). Performance: the detection is cached by
source, and the proof run adds no trace. Duplication: none.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 000001-20261101T141849Z; SELF-TESTS
GREEN, 138 ran; leg 1 Unit 5,701 and Characterization 37 cases GREEN).

**Residue, named.** A PowerShell or bash self-test is not shifted (`Get-Date` and `date` read a clock no Python process
moves), and neither is a clock that is not Python's; those self-tests still run at today's clock in every gate, CI and the
nightly. A clock every process agrees on (libfaketime on Linux) is the next step if one of them plants a dated fixture.

**Goldens.** `self_test_clock.py --self-test` (detection arms; the shim against the unshifted clock for every function;
the horizon) and `self_tests.py --self-test` (a fixture written at arm time and compared with the wall clock is RED in
the proof run and records no PASS; the same test deriving its fixture from the clock, a child that must agree with its
parent, a self-test that cannot read the clock and one declared real; an mtime comparison undeclared is RED; the recorded
horizon; reuse inside it and a fresh run past it).

**Register.** PB4358 landed; PB1957 row 57 and DESIGN-test-build-ci.md §3.14.10 item 5 record the rule.
