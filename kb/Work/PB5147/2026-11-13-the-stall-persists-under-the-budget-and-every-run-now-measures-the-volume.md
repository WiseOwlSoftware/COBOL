## Step taken 2026-11-13 (wave 20262022 group E) - the stall persists under the 210 s budget, and every run now measures the volume. NOT CLOSED: the cause is still owed

**No red since the budget.** Since PB5154's own-clock budget (210 s) landed on 2026-11-10, 161 merge-group runs completed
(147 green, 14 red) and none of the 14 reds names CM102M (`gh run view <id> --log-failed | grep CM102M`, 0 in each). So the
timeout probe of the previous step (`durable flush on the run volume`, printed only on a timeout) has never fired.

**The stall is still there, absorbed by the budget.** CM102M's duration, mined from the `Conformance.trx` artifacts of the 40
latest completed merge-group runs (runs 43313667155 to 43552061076, 2026-11-12 21:47 to 2026-11-14 03:31 UTC; the miner is
`cm102m_durations.py` in this wave's scratch directory `w20262022e`):

| host | runs | min | median | max | over 100 s |
|---|---|---|---|---|---|
| Linux shard 2 | 30 | 90.4 s | 90.7 s | 91.3 s | 0 |
| Windows shard 3 | 24 | 93.1 s | 94.5 s | 121.1 s | 5 (100.4, 103.5, 106.5, 113.8, 121.1) |

The program waits 90 s on its own clock. Its other work takes under 1 s on Linux every time, and 3 s to 31 s on Windows, where it
also took 88 s or more in the two runs of 2026-11-10 that timed out. The Windows tail is the same stall at a smaller size. Linux,
which runs the same program with the same 544 device flushes (`fdatasync`), never shows it. That fits the flush hypothesis (a
`FlushFileBuffers` on the runner's OS volume, which the scratch directory under `%TEMP%` lives on), but it does not prove it.

**Not reproduced on this host.** The test took 97 s alone and 93 s beside a process doing 20 462 write+`fsync` rounds of 64 KiB on
the same volume (5.9 ms each) for its whole run. A local SSD does not slow one file's flush for another, so this host cannot
reproduce a cloud volume's throttling. That is consistent with the hypothesis and does not test it.

**The step (this landing).** `CommunicationCcvsTests.CM102M_ReportsNoFailure` writes `HostLoad.DurableFlushLine(dir)` to its
test output on EVERY run, so each merge group's `Conformance.trx` records the run volume's flush time (median and worst of 5
probes) in the result's `<StdOut>`, next to the test's duration. Locally: `durable flush on the run volume: median 1.2 ms, worst
2.1 ms of 5 probes`.

**Still owed (PB5147 stays OPEN).** Mine about 20 runs' trx again, this time reading the `<StdOut>` line beside `duration`. If
slow runs (over 100 s) show a high flush median, the cause is the volume's flush time against 544 flushes, and the product
question is the per-SEND device flush (DESIGN-communication-facility §6 "Devices", step 2, which orders the device before page
2). That question becomes its own note with these numbers. If slow runs show a fast flush, the flush is ruled out and the stall
is elsewhere: the sequential log file's parked entry mutex, or the store's landing writes.
