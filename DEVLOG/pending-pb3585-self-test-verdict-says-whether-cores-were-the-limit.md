## The self-test verdict says whether the cores were the limit; the budget stays one place per core (PB3585)

kb/Work PB3585 asked whether CI's 4-core Windows runner would run the script self-tests faster with more budget places
than cores. The note's hypothesis was that the self-tests spend their time waiting on process starts and disk. It
asked for runs at 4, 8 and 12 places on CI. An implementer cannot make those runs: a dispatch run is the whole matrix,
and an implementer does not push. So the measurement now comes from every run.

**The reading.** `scripts/self_tests.py` has a `HostCpu` reading: two samples of `GetSystemTimes` on Windows or
`/proc/stat` on Linux, the reading `tests/_shared/HostLoad.cs` takes for a harness non-observation. The GREEN verdict
line's budget clause now ends `the host's P processors u% busy`. Near 100 % the cores were the limit and more places
cannot help. Well below it, `--budget N` runs one phase at another width. That option replaces `--jobs`, which no
caller passed and which only narrowed the thread pool. No gate or CI job passes `--budget`. The pool is one thread per
place.

**What was measured.**
- CI keeps every place busy. Merge-group runs 40058976771, 40057441922 and 40048830853 took 290, 369 and 236 s for
  1,031, 1,332 and 812 summed self-test seconds. From the job logs' timestamps, four ran at once from the 20th second
  until the last long self-test.
- The phase needs about 85 % of a 4-core runner's processor time. Run inside a Job object pinned to 4 logical
  processors on this host, the phase's processes used 988 CPU-seconds and started 12,996 processes, against the 1,160
  processor-seconds a 4-core runner has in 290 s. That run's wall time (1,919 s) does not count: three other gates kept
  the host 86 % busy and starved it. The 8- and 12-place runs were stopped for the same reason.
- The order costs more than the cap. Self-tests start in discovery order, so `prune_worktrees.py` started at 196 s of
  286 s and ran alone for the last 50 s. Running the longest first needs last durations. The PASS store has them, but
  CI starts with an empty store. That is a lead, not part of this change.

**Decision.** The default stays one place per core. After the landing, read the busy share in the `windows-self-tests`
job's verdict line. Under about 70 %, measure `--budget 8` and `--budget 12` on that runner before changing the
default. DESIGN-test-build-ci.md §3.14.10 item 2 records the reading, the numbers and the decision.

**Witnesses.** New `self_tests.py --self-test` arms cover the busy share's arithmetic, a missing or non-advancing
reading, this host's reading, the verdict line, and `--budget`'s refusals of 0, -2, x and 1.5.
