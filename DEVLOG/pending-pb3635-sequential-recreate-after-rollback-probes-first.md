## PB3635: the sequential WRITE that creates the file again after a ROLLBACK probes for another run unit's file first

**What the re-probe showed.** A run unit with a record or line sequential file under APPLY COMMIT writes, ROLLBACKs (the file did not
exist at the last COMMIT, so the ROLLBACK removes it, §14.9.36.4 GR3) and, with the connector still open, WRITEs again; the WRITE
creates the file again. `SequentialConnector.BeginAppending`'s removed-file arm began a START A journal and opened the writer
`FileMode.Append` with no check of the path. If another run unit had made a file there since, the WRITE answered `00` and appended to
it (a COMMIT kept the other's record plus ours; a ROLLBACK deleted the other run unit's whole file as ours); with the other run unit's
file still open the WRITE was refused but the journal it began (`txn-0.journal`, `txn-0.live`) stayed behind, and a later run unit's
OPEN EXTEND settled it and removed the other's file. Five new rows in `SequentialCommitControlTests` reproduced it before the fix.
The keyed twin, `KeyedConnector.CreateTheFileAgain`, already answered `30` (§9.1.13.6 item 1) from a presence probe.

**The fix.** `BeginAppending` is now `CreateTheFileAgain`, the keyed twin's decision under the entry mutex the lock test holds: a
`HostFile.Probe` that is not Absent answers `30`; the creating handle is taken `FileMode.CreateNew` (not Append), so a file made between
the probe and the open is refused too; an unsuccessful creation leaves the connector on no file, withdraws the journal this WRITE began
when it created no file (`SequentialTransactions.Withdraw`), and otherwise leaves the file it made to item 8's ROLLBACK
(§14.6.13.1.3 8). `JournalLockTest`'s proceed step now returns a status (`Func<string?>`), so the refusal is `30` directly and not an
`IOException` routed through `ForWriteFailure`.

**The sibling arm.** The note asked for the probe-then-create window of the keyed `CreateTheFileAgain` to be checked against an
uncovered OPEN OUTPUT, which takes no entry mutex. It was real: the probe was followed by a handle taken `FileMode.OpenOrCreate`, so a
file made after the probe would have been adopted and its empty store created over it, the START A journal then deleting it at ROLLBACK.
`KeyedConnector.TakeFileLock` takes `CreateNew` for that one caller (`newFileOnly`, threaded through `Lock` and `LockCreating`). A
deterministic test cannot land between the probe and the open, so `FileCreatedAgainDriftTests` pins both organizations' creation as
probe plus `CreateNew`; `CommitRollbackRuntimeTests.A_keyed_write_creating_the_file_again_meets_*` pins the probe half for RELATIVE and
INDEXED.

**Goldens.** None: the abnormal path is two-run-unit and host-state dependent, so it is covered by the Unit harness. Rows re-verdicted:
none (the note claimed no inventory row; `closes_rows: []` with its reason). Design doc DESIGN-runtime-library.md 2.2.2.14 updated.
