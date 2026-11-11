## Step taken 2026-11-11 (wave 20261998 A) - what the stall is made of, and the probe that names it. NOT CLOSED: the cause is still owed

**Re-probe (Debug build, this host).** `CM102M_ReportsNoFailure` Passed in 1 m 36 s. With a temporary stopwatch (not kept) on
every durable flush of the run (`McsStore.Transmit`'s `FlushToDisk` and `PagedFile.TrySync`) the run made **544 device
flushes (1189.7 ms in all, 2.2 ms each, worst 5.3 ms) and 7 store syncs (12.9 ms)**. So here the program spends 90 s in
its three 30-second `DELAY-FOR-30-SECS` spins (CPU 1:1 with the clock, the PB5073 review's sample) and about 1.6 s in
everything else.

**What the runner's red says.** Both bounced attempts (runs 42841304077, 42835167709) died at 120 s with 31.3-31.5 s of CPU on
a host 13-15 % busy: one delay done (about 30 s of spin), the rest of the 120 s off the processor on an idle host, so the
process was BLOCKED, not starved and not spinning. The only blocking calls the program makes in bulk are those 544
durable device flushes (each SEND flushes its device file before the store commits the new end, DESIGN-communication-facility
"Transmit"): 88 s of stall over the 35 or so sends before the second delay and the 510 after it is about 160 ms per flush, 70
times what this host takes, a latency a cloud volume's `FlushFileBuffers` can show. That is a hypothesis that fits both
numbers; it is not a measurement of the runner.

**The guard (this landing).** The harness now measures it instead of guessing it: a timed-out observation's line carries
`durable flush on the run volume: median N ms, worst M ms of 5 probes` (`HostLoad.DurableFlushLine`, called from
`ProcessObserver.Observe` before the kill, in the run's own working directory). The next red states the volume's flush time
beside the processor time and the host load: a median near 160 ms confirms the hypothesis; a median near 2 ms means the
block is elsewhere and the line has ruled the flush out. Tests: `TheDurableFlushLine_IsTheMedianAndWorstOfTheProbes_OrSaysItWasNotProbed`,
`TheDurableFlushProbe_TimesFlushesOnTheDirectory_AndLeavesNothingBehind`, and the existing non-observation test now asserts the
line.

**What was not changed, and why.** The 544 flushes are the designed durability contract of a device (a power loss keeps
what a statement answered; `McsStoreCrashMatrixTests`), so reducing them is a redesign of the MCS commit protocol and not a
test-harness matter. The stated own-clock wait (PB5154, 210 s for CM102M) stays: it is right whatever the flush costs
(a 90 s wait is not a load multiple), and it is the budget, not the cause. If the probe line shows a slow volume, the
product question (a SEND to a device paying one volume flush each) becomes its own note with that line as evidence.

**Still owed (PB5147 stays OPEN).** The probe is the step taken, not the fix: nothing yet says why a runner's CM102M
spent about 88 s off the processor. Owed: read the next red's `durable flush on the run volume` line. If the median is
high, the cause is the volume's flush time against the 544 flushes, and the fix is the product question above (or a
decision recorded against it). If it is low, the flush is ruled out and the block is elsewhere (stack the process at the
kill, the store mutex, the device file). Either way the structural guard owed is one that fails when a program's
durable work times out of proportion to its measured cost, never a longer limit. The probe's directory is the one the
child runs in (`HostLoad.RunDirectory`, review R1-2). PB5154's stated own-clock wait stays, as a budget.
