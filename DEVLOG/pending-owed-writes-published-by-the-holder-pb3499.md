## The Windows second OPEN attempt yields only to an INPUT opener that owes writes (PB3499)

PB3499 was a low-severity lead from the branch review of PB3482: on Windows the second attempt of `FileConnector.Open`
(the OPEN made again with `FileLockPosture.WhenOwedWritesAreAdmitted`, `FileShare.ReadWrite`) was gated by "does ANY other
run unit's INPUT opener hold a column" (`RunUnitFileLock.AnotherInputOpenerHolds`). That is a proxy for the one case that
needs it, an INPUT opener whose transaction still owes its COMMIT or ROLLBACK and so holds a handle open for writing.

**Re-probe.** A new witness (`FileLockPostureDriftTests.AnInputOpenerThatOwesNoWritesDoesNotAdmitAReadOnlyOpenBesideAnOutsideWriter`:
an ALL OTHER reader open, a bare writer open, then `OPEN INPUT` under `SHARING WITH READ ONLY`) answered '00' for all three
organizations; the same OPEN with no reader open answers '61'. The OPEN's handle then stayed `FileShare.ReadWrite` for its whole
life, so a SHARING WITH READ ONLY file lock was defeated by an unrelated reader.

**The open judgment: a wrong answer.** §9.1.15 2) arbitrates file connectors and an outside writer is not one, so the
standard leaves it to the implementor (A.1 item 75), and neither GnuCOBOL nor IBM/Micro Focus has a Windows share-mode
determination to follow. This project's own determination (DOC-A.1-75, DESIGN-runtime-library.md 2.2.1.8) already said that
such a program still refuses a READ ONLY request '61' with no INPUT opener that owes writes beside it; the code contradicted its
determination, so the code moved.

**The fix, at the root.** The fact the share mode needs is not a Table 19 column, so the holder now publishes it itself:
`IColumnLockHost.Establish` takes `owesWrites`, and `HandleColumnLockHost` holds one more exclusive byte, of a sixth range
past the columns' (`HostRegionLocks.OwedWritesRangeStart`), in the same request as the column, through the same handle. A
superseding request carries the byte, adds it or gives it back; a refused request rolls its byte back; `Revert` publishes the
superseded state. `RunUnitFileLock.AnotherOwesWrites` tests the range with the asker's own byte left out, and replaced
`AnotherInputOpenerHolds` and the host's `AnotherHolds(column)` (both deleted). The connector says it before its body from what
a CLOSE under commit control kept (`FileConnector.OwesWrites`, `KeyedConnector.WillTakeWritableHandle`, which is the one
expression `KeyedConnector.Lock` opens the handle by, now `HandleAccess`), so a kept column is replaced by one that already
owes and there is no instant in which an INPUT column stands without it; on the arm that takes its column after the body (a
CLOSE under SHARING WITH NO OTHER kept a `FileShare.None` handle that publishes no column) the handle the body took says it
(`FileLock.CanWrite`). The Linux and lock-file hosts ignore it: they have no mandatory share mode to yield.

**Witnesses.** The re-probe test (red before, green after, three organizations); `HandleColumnLockHostDriftTests`:
`AHolderThatOwesWritesIsSeenByAnotherHolderAndOnlyByAnother` (readers owe nothing however many hold an INPUT column; carried,
added, given back, restored by `Revert`, rolled back with a refused request, taken afresh), `OwingWritesChangesNoAnswerOfTheTable`
(every request) and `TheOwedWritesRangeLiesPastEveryColumnsRange`; `CommitRollbackRuntimeTests.An_input_opener_whose_column_is_taken_afresh_after_a_NO_OTHER_close_publishes_that_it_owes_writes`
for the second arm. Two mutations were run and each turned its witness red: the keyed connector never owing (the eight
`An_input_opener_that_owes_its_transactions_writes_admits_what_Table_19_admits` cases) and `TakeRunUnitFileLock` dropping the flag on
the fresh `Take` (both cases of the new NO OTHER test). The two PB3498 witnesses (`AnOpenWhoseSecondAttemptIsRefusedToo...`,
`A_reopen_refused_on_its_second_attempt_too...`) had used a plain reader as the writer's owner, the proxy again, and now hold a lock
that owes writes.

**Rows.** No inventory row is claimed or closed (`closes_rows: []` with its reason). Docs: DESIGN-runtime-library.md (the Windows
host bullet, 2.2.1.8's byte map, the DETERMINATION, 2.2.1.10 As built (3)) and CONFORMANCE.md DOC-A.1-75.

**Gates.** Windows: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `000001-20261023T172205Z`, head
243a3e429). Linux, the host-I/O group's own run: `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD 243a3e429) ===`.

**Self-review** (pr-test-analyzer, silent-failure-hunter, comment-analyzer): the one silent-failure shape on the path, an
unobservable or exhausted owed-writes range, answers "does not owe" (the share mode does not yield, '61'), never the other way;
the first draft's `Take` assumed a fresh lock owes nothing, which the NO OTHER-close arm falsified, so both arms now carry it.

Branch review round 1 found the rollback of `HandleColumnLockHost.Establish` when the owed-writes byte cannot be published untested (the column byte the request
already added must be unlocked, a fresh handle disposed, a superseded hold left standing). `AnOwedWritesByteThatCannotBePublishedFailsTheRequestAndGivesBackTheColumnByteAlreadyAdded`
holds the whole owed-writes range from an outside handle and asserts, for a first request and for a superseding `Amend`, `Unavailable`, the column range free again,
the kept column still refusing, and no handle left on the file; dropping the `added` unlock in the `finally` turns it red (mutation measured). The design doc named a
`KeyedConnector.OwesWrites` that does not exist; it now names `FileConnector.OwesWrites` over `KeyedConnector.WillTakeWritableHandle`. Re-gate: leg 1 GREEN; Linux gate
(unit, conformance) GREEN on the new head.
