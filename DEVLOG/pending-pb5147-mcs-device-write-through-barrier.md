## PB5147: a SEND's MCS device barrier is one write-through write, not a flush per statement

**Re-probe.** `CM102M_ReportsNoFailure` passed on this host's Debug build in 1 m 39 s, its probe line `durable flush on the run
volume: median 1.6 ms, worst 2.2 ms of 5 probes`. The merge-group stall does not reproduce on a local SSD, so the evidence is the
runners': the flush line every CM102M run has written to its trx since PR #1614, mined from 40 merge-group runs (30 with a CM102M
result). Linux took 90.5 s to 92.4 s with a flush median under 2.2 ms every time; Windows took 93.2 s to 106.3 s with a flush
median of 3.8 ms to 828.5 ms, the two highest medians in two of the three slowest runs. A runner's `FlushFileBuffers` swings by
two orders of magnitude, and CM102M paid it once per SEND, 544 times.

**The fix.** `McsStore.Transmit` opens the device `FileOptions.WriteThrough` (`FILE_FLAG_WRITE_THROUGH` / `O_SYNC`) and makes
no flush per statement: the write returns with its bytes and the size change it makes on the medium, which is the barrier
DESIGN-communication-facility §6 "Devices" step 2 needs before page 2 lands. Microsoft's CreateFile page names this flag as the
replacement for a `FlushFileBuffers` after each write. A device the statement creates is still flushed, and the MCS directory is
synced once (`HostFile.SyncDirectory`), because an empty file's name is metadata a write-through write is not documented to
carry. That closes a sibling hole: on Linux the device's creation had no directory sync, so a power loss could take the file
while page 2 kept its committed end. The design doc's step 2 says this now.

**Measured.** The probe times both barriers on every CM102M run: here `durable flush on the run volume: median 2.3 ms, worst
3.2 ms of 5 probes; write-through append: median 0.1 ms, worst 0.2 ms of 5 probes`, and CM102M took 1 m 34 s; all thirteen
`CommunicationCcvsTests` passed.

**Guard.** `McsDeviceBarrierDriftTests` (Roslyn over `Runtime/Communication`) fails on a flush of the MCS that is not under a
device's creation and on a device write through a handle not opened write-through; restoring the old `Transmit` failed both.
No inventory row is involved (`closes_rows: []`); PB5147 is landed.
