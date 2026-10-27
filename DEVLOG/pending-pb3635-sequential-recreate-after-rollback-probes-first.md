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
`KeyedConnector.TakeFileLock` takes `CreateNew` for that one caller (`newFileOnly`, threaded through `Lock` and `LockCreating`).
`FileCreatedAgainDriftTests` pins both organizations' creation as probe plus `CreateNew`;
`CommitRollbackRuntimeTests.A_keyed_write_creating_the_file_again_meets_*` pins the probe half for RELATIVE and INDEXED.

**The OPEN creation arms (branch review round 1, R1-1: the same root).** The first version left the OPEN arms that create a file
from an absent probe as they were (`FileMode.Create`, `Append`, `OpenOrCreate`) and called the window unreachable by a test. It is
reachable: the fault-injecting medium's `BeforeOperation` seam lands another run unit's OPEN OUTPUT between the probe and the creating
handle, wherever the medium opens it (every sequential handle and every covered keyed OPEN's journal). Fixed with one mechanism for
both organizations. A creation decided on an Absent probe takes a `FileMode.CreateNew` handle (sequential `OpenCore`,
`HeldWriteStream`, `OpenReader(creates)`; keyed `LockCreating(newFileOnly)`), not for an OUTPUT whose probe said Unauthorized (the file
may be there and OUTPUT replaces it). A refusal (`HostFile.IsAlreadyExists`, the host's `ERROR_FILE_EXISTS` or `EEXIST`) makes
`FileConnector.Open` run the OPEN again from its entry step, at most three attempts with PB3482's share-mode retry: the unsuccessful
attempt withdrew the journal it began (GR25), the new probe finds the file, and the OPEN is that of an existing file, as if the two run
units' OPENs had run one after the other. OPEN OUTPUT captures and replaces it, so a ROLLBACK restores its records instead of removing
the file as the run unit's own creation; I-O and EXTEND open it with `00`, where they used to truncate it (I-O, uncovered) or append and
answer `05`. The attempts running out answer `30`. The journal withdrawal became one rule in `FileConnector.WithdrawWhatTheOpenBegan`
(the keyed connector had it; the sequential one never withdrew a refused covered OPEN's START A journal, which a retry would have
captured into). The leads: the sequential CreateNew half of the WRITE now has a behavioural witness (`A_write_creating_the_file_again_meets_a_file_made_between_the_probe_and_the_handle_30`), and two doc comments name `CreateTheFileAgain`.
Witnesses: `SequentialCommitControlTests` (`An_open_creating_the_optional_file_meets_*` six rows, `A_covered_open_output_meets_*`,
`An_open_whose_creation_is_met_by_a_new_file_at_every_attempt_*`), `CommitRollbackRuntimeTests.A_keyed_open_creating_the_file_meets_*`
(eight rows). An uncovered keyed OPEN opens through the host directly, so no seam reaches it; `FileCreatedAgainDriftTests` pins it.

Review round 2. R2-1 (blocking): on Unix a `CreateNew` handle on a DIRECTORY fails with the same `EEXIST` as a file made meanwhile, and the
presence probe answers Absent for a directory by design, so every creating OPEN retried three times and answered `30` on Linux where
Windows (and the code before this note) answered `37`. `HostFile.IsAlreadyExists(e, path)` now excludes a directory and the new
`HostFile.IsDirectoryInTheWay` maps it to `37` on both hosts; witnesses `An_open_creating_the_file_meets_a_directory_at_the_path_37_at_once`
(sequential, six rows) and `A_keyed_open_creating_the_file_meets_a_directory_at_the_path_37_at_once` (keyed, eight rows), which the Linux
gate runs against the real `EEXIST`. R2-2: the two retry reasons are counted apart (`MaxFileMadeRetries`), so the bound is the documented
one and a share-mode refusal is not answered `61` because a file was made twice first. R2-3: the sequential WRITE re-creation now keeps its
journal in `_begunByOpen`/`_openChangedFile` and withdraws through `FileConnector.WithdrawWhatTheOpenBegan`, as its keyed twin does.

**Goldens.** None: the abnormal path is two-run-unit and host-state dependent, so it is covered by the Unit harness. Rows re-verdicted:
none (the note claimed no inventory row; `closes_rows: []` with its reason). Design doc DESIGN-runtime-library.md 2.2.2.14 updated.
