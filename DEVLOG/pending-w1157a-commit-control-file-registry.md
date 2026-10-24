## Commit and rollback for sequential files: the transaction journal (PB3340), and a deactivated clause releases its files (PB3541)

**PB3541 (wave 1157, group A).** A file connector put under commit control by an APPLY COMMIT clause stayed under it after
the clause was deactivated. Re-probed on 07bfc3606: a main program whose clause names only WS-X calls a subprogram whose
clause names the EXTERNAL RELATIVE file RELF, CANCELs it and COMMITs, so §14.9.7.4 GR3 (*"The COMMIT statement also
deactivates any APPLY COMMIT clauses in exited initial programs and canceled runtime elements"*) deactivates the
subprogram's clause; a third program, with no clause, then writes record 1 of RELF, and the main program's ROLLBACK undid
it (`RECORD 1 ABSENT`). §14.9.36.4 GR3 restores only *"files and data-items referenced in active APPLY COMMIT clauses"*, and
§12.4.6.3.4 GR5 makes a canceled element's clause active only until that deactivation. The mechanism: `TransactionControl`
dropped the retired participant and nothing undid `FileRegistry.RegisterCommitControl`, which has no inverse, so the
connector kept `CommitControlled` (its WRITEs journaled into the run unit's transaction, §14.6.13.1.3 item 8 still due on its
fatal statuses) and the implicit AUTOMATIC posture of §9.1.16.

The fix: each participant keeps the file connectors it put under commit control, and `TransactionControl.DeactivateRetired`,
the one deactivation (a successful COMMIT and every ROLLBACK), takes each file a deactivated clause named that no remaining
active clause names out of commit control through the new `FileRegistry.ReleaseCommitControl`, which clears the flag and puts
back the file control entry's own lock posture (the registration now records it; a displaced connector forgets it with its
share). A transaction whose rollback the medium refused stays owed: the registry finds it by its open transaction id, not by
the flag. Witnesses: `TransactionControlTests.A_file_whose_last_clause_is_deactivated_is_no_rollbacks_and_no_item_8s`
(COMMIT and ROLLBACK arms, RELATIVE and INDEXED), `A_deactivated_clause_gives_the_file_its_declared_lock_posture_back` (the
entry's MANUAL posture back, RED on a mutant that keeps the AUTOMATIC share) and the golden `cases/14.9.7-COMMIT/pb3541_deactivated_clause_releases_its_file`,
RED on a mutant that skips the release. DESIGN-runtime-library.md 2.2.1.10's compiler-half paragraph states the release.

**PB3492 re-probed and settled.** On this branch's build a RELATIVE
file under APPLY COMMIT answers '62' to a DELETE FILE after its CLOSE (its own kept handle read as another holder's by
`HostFile.IsHeldByAnother`), and the re-probe found a sibling with the same root: an OPEN OUTPUT of an existing keyed file
after a COMMIT, then a ROLLBACK, leaves the file empty (`RECORD 1 ABSENT`), because `KeyedConnector.Recreate` ends the kept
transaction when the creation lands. The owner question PB3492 carried is answered by the standard: the NOTE under §14.9.10.4
GR15 says such files *"can still be deleted, however if they are, then in the event of a rollback they will be restored"*, and
GR15's conflict is a file *"locked by another file connector"*. DESIGN-runtime-library.md 2.2.2.14 now settles the shape: the
keyed organizations' two capture verbs ride PB3340's journal (the before file copied through the kept handle, the `K`
record, then the creation or the removal; ROLLBACK restores the before file and rolls back the run unit's transaction inside
it), and it records the one primitive the journal design assumes and the runtime lacks, a directory sync, for the
`IHostMedium` seam to own.

**PB3340 (wave 1157, group A, the finisher).** §9.1.18.2 admits *"any type of file on a mass storage device"* except sort-merge
and report files, and the runtime gave commit and rollback to the keyed organizations only: the compiler refused a SEQUENTIAL
operand of APPLY COMMIT by name (COBOLNET1709). Implemented FROM DESIGN-runtime-library.md 2.2.2 (the owner's "option a", the
journal as transient files in the file's lock directory):

- **The seam first** (`IO/IHostMedium.cs`): the `HostFile` roles that change a file or a name, instance-scoped on the run unit
  (`FileRegistry.Medium`; the production medium is `HostFile` itself), plus the two roles the journal needed that the runtime
  lacked: `HostFile.OpenTransient` (the journal's files, shared with every reader, unbuffered) and `HostFile.SyncDirectory`
  (Linux and macOS `open(O_RDONLY)` plus `fsync`; on Windows the next file flush forces NTFS's and ReFS's metadata journal, the
  determination recorded beside A.1 item 75). `HostFile.WriteStreamOf` makes the write stream's one decision usable over a
  handle a medium opened. The entry mutex is a new region byte, 2^62 + 0x102, held as a lock file on every host
  (`SidecarRegionLocks.ForPath`) because the file it guards may not exist.
- **The journal** (`IO/Sharing/SequentialJournal.cs`): the WOLJ record (a CRC-32C header, then `L`, `R`, `K` and `D` records,
  a torn tail ends them), the five write-ahead rules, COMMIT's three regions, the one idempotent rollback sequence, and the
  entry step that reads every live transaction's holdings and settles every dead one. `SequentialTransactions` is the run
  unit's table of them, one per physical file, ordered with the keyed stores by `StoreTable.NextOrder`.
- **The covered connector** (`SequentialConnector.CommitControl.cs`, the connector made partial): one read-write handle kept
  across CLOSE and re-OPEN under readers and writers that leave it open; R2 inside `OverwriteInPlace`, R3 at the first append,
  R4 at an OPEN OUTPUT of an existing file, START `A` at a creation; after a ROLLBACK the writer lands at the restored end or
  creates the removed file again at its next WRITE, the reader keeps its position, and the LINAGE page state is restored from
  a snapshot. The entry step runs at every writable sequential OPEN, covered or not, before the presence probe
  (`FileConnector.EnterPhysicalFile`: a settle can create or remove the file); WRITE, REWRITE and READ under SHARING WITH ALL
  OTHER meet another live transaction's holdings ('51'; the READ through `FileRegistry.JournalConflict`, which the record-lock
  peek does not gate). A DELETE FILE under commit control captures and proceeds; its refusal by a live transaction is
  §9.1.13.9 2)'s '62', where the design text and the model had '61' (both corrected, the model's self-test GREEN).
- **The compiler**: COBOLNET1709 is deleted and retired, and with it `FileModel.IsCommitControlled` and
  `ApplyCommitModel.CommitControlledFiles`, which only narrowed the clause to its keyed files.

The witnesses run 2.2.2.12's rows over real bytes through `FaultHostMedium` (tests): every counted operation refused, made the
point of a death (handles and holders dropped as a kill drops them, unsynced writes kept) and of a power loss (only synced bytes
and directory-synced names survive; a second mode keeps the physical file's unsynced writes, the adversarial subset for a
write-ahead rule). Breaking R3 (the `L` record unsynced) or R4 (the `K` record before the before file) by hand turned their
rows RED. Goldens: `cases/14.9.36-ROLLBACK/pb3340_sequential_file_under_apply_commit` (record and line sequential: an append, a
REWRITE, an OPEN OUTPUT, a DELETE FILE and a creation undone, an append committed) and `pb695-apply-commit-no-on`, a refusal
until now and a positive case (its file OPTIONAL: a '35' on a file under commit control is fatal, and item 8's ROLLBACK would
restore the very FILE STATUS item the program displays). Ten inventory rows go CONFORMS; the three ROLLBACK rows stay PARTIAL
and move to PB3492, whose keyed capture (an OPEN OUTPUT or DELETE FILE of a keyed file, design 2.2.2.14) is the facility's last
gap; its NEXT steps are in the note.
