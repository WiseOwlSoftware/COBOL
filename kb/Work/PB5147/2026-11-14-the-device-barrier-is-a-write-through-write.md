## Landed 2026-11-14 (wave 20262039 group G) - a SEND's device barrier is one write-through write, not a flush per statement

**The volume's flush time, mined beside the duration.** The `durable flush on the run volume` line every CM102M run has written
to its `<StdOut>` since PR #1614 (2026-11-14 04:38 UTC), read from the `Conformance.trx` of the merge-group runs from 2026-11-14
17:06 to 2026-11-15 04:20 UTC (40 runs, 30 with a CM102M result; miner `E:\COBOL-coord\scratch\w20262039g\mine.py`, log `mine.log`):
- Linux: 90.5 s to 92.4 s, flush median 0.0 ms to 2.2 ms in every run.
- Windows shard 3: 93.2 s to 106.3 s, flush median 3.8 ms to 828.5 ms. The two highest medians (727.2 ms, 828.5 ms) came with two of
  the three slowest runs (102.6 s, 106.3 s); a 142.5 ms median came with 95.2 s and a 4.4 ms median with 105.9 s. Five probes after
  the program has exited sample the volume at one moment, not across its 544 sends, so the line shows that a runner's
  `FlushFileBuffers` swings by two orders of magnitude between and within runs, which Linux's `fsync` never does, but it does not
  time the program's own flushes.

**The mechanism and the fix.** The only blocking call CM102M makes in bulk is the device flush each SEND made
(`McsStore.Transmit`, `RandomAccess.FlushToDisk`): `FlushFileBuffers` asks the volume to flush its whole write cache, so its cost is
the runner's, not the statement's. The barrier the design needs (§6 "Devices": the device bytes are durable before page 2 lands)
is now ONE write-through write: the device is opened `FileOptions.WriteThrough` (`FILE_FLAG_WRITE_THROUGH` / `O_SYNC`), which
Microsoft documents as the replacement for "calling the FlushFileBuffers function after each write, which can cause unnecessary
performance penalties" and as flushing "any metadata changes ... that result from processing the request" (CreateFile, "Caching
Behavior"), so the size an append makes is durable with it. A device the statement creates is still flushed and its directory
synced once (`HostFile.SyncDirectory`), because "the file metadata may still be cached (for example, when creating an empty file)"
(same page); that also closes a sibling hole on Linux, where the device's first creation had no directory sync, so a power loss
could take the file while page 2 kept its committed end. The cut `SetLength` makes over a dead tail stays cached: a tail a power
loss keeps lies past the committed end, which the next run unit's first statement cuts.

**Measured here (Debug, this host).** The probe now times both barriers: `durable flush on the run volume: median 2.3 ms, worst
3.2 ms of 5 probes; write-through append: median 0.1 ms, worst 0.2 ms of 5 probes`. CM102M took 1 m 39 s before the change and
1 m 34 s after, all thirteen `CommunicationCcvsTests` green.

**The structural guard.** `McsDeviceBarrierDriftTests` (Roslyn, over `Runtime/Communication`) fails when a flush of the MCS is
not under a device's creation, or when a device write goes through a handle not opened write-through; both arms were seen to
fail with the old `Transmit` restored. The merge group's trx keeps the two-barrier line on every CM102M run, so the next reader
sees the runner's write-through cost beside its flush cost. A write-through stall on a runner would be a new defect with that
line as its evidence.
