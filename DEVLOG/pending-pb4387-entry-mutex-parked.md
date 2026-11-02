## PB4387: an uncovered sharing writer's entry mutex is parked between statements, not published and retired around each record

An uncovered sequential writer (no APPLY COMMIT) whose file lock admits another writer holds the sidecar entry mutex around
every WRITE and REWRITE (PB3340 R1-1: no other transaction may begin inside one change). The mutex is a lock file in the
file's `.wolk-*` directory, and each statement published it (create, lock, probe, rename), listed the directory, then retired
the file (rename, delete) and the directory (list, rename, delete): about ten metadata operations a record.

**Re-probe.** A program writing 20,000 records to a SEQUENTIAL file `LOCK MODE IS AUTOMATIC SHARING WITH ALL OTHER` took 37.7 s
on this host (Dev Drive, ReFS), 6.6 s with no sharing clause, and 5.3 s with the mutex skipped by a throwaway experiment, so the
mutex cost 32 s, 1.6 ms a record. The note reproduced.

**Fix shape.** The connector owns one entry-mutex endpoint for its life (`HostRegionLocks.KeptEntryMutex`, reached through
`IHostMedium.KeptEntryMutex` so the fault-injecting medium still sees it; created at the first guarded change, disposed with the
connector's regions at its CLOSE or an abandoned OPEN). The endpoint is a `SidecarRegionLocks` made by `ParkingForPath`: a byte it
releases is not retired but PARKED, the holder's lock file renamed (`SidecarLockFiles.LockFileHolder.Rename`, which keeps the
open file description and so the lock) from `r<byte>-X-<id>` to `i<byte>-<id>`, a name no `Look` reads, so a parked file holds
against no one. The next hold renames it back and runs the same publish-then-test `Look`. Per statement: one rename, one listing,
one rename. The parked file keeps the lock directory standing for the connector's life and the CLOSE retires both, through
`RetireDirectory`'s unchanged rule. Rejected: holding the mutex from OPEN to CLOSE (it would keep every other writer's OPEN out
for the connector's life). A parking endpoint's first publication sweeps the dead idle files of its byte, so a writer killed
while parked leaves litter that the next writer's start removes; a parked file removed under the endpoint is published again.
`TryHoldEntryMutex` had one caller left and was folded into `HoldEntryMutex`. After: 11.3 s for the 20,000 records, the mutex's
share about 6 s (0.3 ms a record). The remaining cost is the rename, listing, rename and the journal listing every entry step makes.

**Goldens.** `unit:SequentialEntryMutexParkingTests` (7 cases): the COUNT of publications through `SidecarLockFiles.RaceInjector`
(1 for 25 WRITEs, 1 for 3 REWRITEs) and the lock directory's names between statements (one parked file, the same one every time),
two writers interleaving (held excludes, parked does not), the CLOSE and the endpoint's disposal retire the directory, a killed
writer's parked file is swept, a parked file removed underneath is published again, and a WRITE the medium refuses gives the byte
back. No ISO rule is involved (a performance fix of a runtime lock path), so no conformance golden and no row closes
(`closes_rows: []`). `SequentialJournalSeamDriftTests` now bans `HostRegionLocks.KeptEntryMutex` past the medium too.

**Self-review.** Architecture: the parking is one mechanism in the one lock-file endpoint, reached through the medium seam; the
connector holds a lease object, not a second protocol. Code: the hold keeps publish-then-test, so exclusion is unchanged; a
refused rename closes the holder and publishes, never reuses a half-renamed file. Performance: the lease is one object per
connector. Duplication: the held and idle names are written once each (`HeldName`, `IdleName`). Sibling sweep:
`git grep -n "EntryMutexByte\|\.EntryMutex(\|KeptEntryMutex" -- src` shows the mutex taken per record only by
`UncoveredChangeGuard` (WRITE and REWRITE); every other user is per OPEN, per transaction, or per COMMIT. Found and not fixed:
the same publish-and-retire-per-hold pattern exists for the store mutex and record locks of the sidecar HOST (macOS), which
`SidecarHost.Open` endpoints never park; it cannot be measured here.

**Gates.** Windows: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,540/20,540 · Unit 39,144/39,144 · Characterization 37/37 cases ran (skipped 0)` (run 000001-20261102T014256Z; the seven new cases Passed in `Unit.trx`). Linux: `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built inert-watch; HEAD 917544760) ===`. `python scripts/semgrep/verify.py`: PASS, no count raised.
