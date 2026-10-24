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

**The resume on `w1157a-pb3340-sequential-journal` (wave 1157, group A, second implementer).** The STOP that parked this branch
was lifted (the Mythos decision of 17:1x: PB3340 continues here, not on the loop's branch). origin/main merged with one
conflict, in `kb/Work/PB3340.md` (main's 16:10 routing note against this branch's landed section): both kept, the routing
recorded as superseded. Checking the 37 fault tests against MANDATORY-PRACTICES I11 found two defects, both fixed here:

- **The RETRY phrase did not apply to the journal arm of the lock test.** Design 2.2.2.3 says "held, '51' (RETRY applies)",
  and §14.7.9.3 GR4 b) attempts a record operation conflict again, but the WRITE, REWRITE and READ steps answered '51' at
  the first attempt whatever the phrase said. Every journal arm is now ONE attempt function under `FileRegistry.RetryLoop`
  (`SequentialConnector.JournalLockTest`). An attempt that needs the entry mutex (a covered connector's first change) takes it
  for that attempt alone and gives it back before the wait. A holding of the run unit's OWN transaction, met through a connector
  no APPLY COMMIT names, cannot end while the statement runs. The attempt is then `RetryAttempt.InRunUnit`: no wait, and RETRY
  FOREVER ends with '52' (A.1 item 109), as for a region lock held inside the run unit. `SequentialJournal.Holding` carries its
  transaction's id for that, and `Holdings.Conflict` is the one place a journal holding becomes '51'. Witnesses:
  `unit:SequentialCommitControlTests.A_statement_meeting_another_run_units_journal_holding_retries_under_its_retry_phrase`
  (WRITE and READ: no phrase, two TIMES exhausted after two pauses, FOREVER completed by the holder's COMMIT on the third pause)
  and `A_holding_of_this_run_units_own_transaction_is_not_waited_for` (TIMES '51' and FOREVER '52' with no pause). Both were
  RED before the fix.
- **`LockedRecordStatusProducersDriftTests` would have been RED**: the branch had added three '51' producers in
  `SequentialConnector.CommitControl.cs` without re-adjudicating DOC-A.1-152. There is now one producer,
  `SequentialJournal.Holdings.Conflict`. The drift test names it, and DOC-A.1-152 (with DOC-A.1-109's deadlock clause) states
  the sequential holdings: the end of the file, a rewritten or appended record, the whole file once replaced.

The program-level exception paths now have a golden of their own,
`conformance:cases/14.7.9-RETRY/pb3340_sequential_holding_exception_paths` (filed under 14.7.9: the first gate refused it
under 14.9.36, a clause none of its rules belongs to). Three connectors of one run unit that no
APPLY COMMIT names meet a covered connector's transaction: G (USE declarative and FILE STATUS) gets '51', RETRY 2 TIMES '51',
RETRY FOREVER '52', each after its declarative. H (FILE STATUS alone) gets READ '51' on the appended record, '00' on an
unchanged one, READ IGNORING LOCK '00', REWRITE '51', OPEN OUTPUT '61' and DELETE FILE '62'. K (neither) continues past a
refused WRITE (§14.6.13.1.4 4)). After the COMMIT, G's WRITE is '00'.

**A RETRY FOREVER OPEN or DELETE FILE waited forever for its own run unit (sibling sweep, keyed files included).**
Probing the OPEN arm of the same sweep: a CLOSE under commit control keeps the file lock (§14.9.7.4 GR4), so
`OPEN EXTEND RETRY FOREVER G` beside a closed covered F on the same file never returned. That held for a sequential NO OTHER
F, for a RELATIVE one (the keyed engine of PB3338, already on main), and for a replaced sequential transaction (its
journal's '61'). `FileRegistry.SharedOpenAttempt` counted every '61' the host or the entry step reported as a holder outside
the run unit, and the DELETE FILE arms did the same for `IsHeldByAnother`'s '62'. Both now ask the registry whether the run
unit itself holds the file against the request: `RunUnitRefusesOpen` and `DeleteFileHeld`, over `RunUnitHoldsAgainst`.
That covers what a CLOSE kept, registered or retired, by the Table 19 column of its file lock. Where the host publishes
none (a NO OTHER handle's share mode is its lock), the column is the kept OPEN's, recorded once at the kept CLOSE
(`FileConnector.KeptOpenColumn`, `KeptFile.OpenColumn`). `OwnTransactionRefuses` covers the run unit's own sequential
transaction. Such a refusal is `InRunUnit`: FOREVER lands '61' or '62' at once, as DOC-A.1-109 already stated for a file
held by another connector of the run unit. Witnesses: `unit:SequentialCommitControlTests.An_open_or_delete_file_refused_by_what_this_run_unit_holds_is_not_waited_for`
(the NO OTHER row RED before the fix, its pause observer throwing instead of hanging) and
`conformance:cases/14.7.9-RETRY/pb3340_retry_forever_own_run_unit_hold` (sequential, relative and replaced-journal holds:
'61' and '62' at once, '00' after the COMMIT). DOC-A.1-109 and `docs/COBOLNET_FILES_DESIGN.md` D8 say so.

**The gates' reds.** The first Windows gate refused the exception golden filed under 14.9.36, a clause none of its rules
belongs to; it now lives under 14.7.9. The second found five tree-wide drift reds that the parked branch had never been gated
against. `LockedRecordStatusProducersDriftTests` is fixed above. `StoreCoherenceTests` now counts files by the class they
declare, so `SequentialConnector.CommitControl.cs` belongs to its class. `FrameDecoderDriftTests` names the journal's own
fixed-field decoders beside the paged store's. `SequentialJournal.HeaderBytes` is renamed `JournalHeaderBytes`, because the
old name belonged to the deleted whole-store model. README drops the declined SEQUENTIAL APPLY COMMIT bullet. Batch
`adjudication/w1157a2-pb3340/batch.json` retires GR-9.1.18.2-p1's witness `declined-apply-commit`, a deleted case. The Linux
gate (all legs, run under WSL) then found two reds that Windows cannot show. (1) Linux hides a lock directory beside F when
F's directory is at mode 000, so the entry step's listing failed and the OPEN answered '61' where
`FileAuthorityPresenceTests` requires '37'. A lock directory this process may not list is now the missing authority:
`Holdings.Unlistable`, '37' for a writable OPEN (§9.1.13.6 6) b)) and for a covered DELETE FILE (§14.9.10.4 GR16). The
cross-platform witness `A_lock_directory_this_process_may_not_list_is_the_authority_an_open_or_delete_file_lacks` uses
`FaultHostMedium.RefusesListing`. (2) `A_killed_run_units_transaction_is_rolled_back_by_the_next_writable_open` answered '61'
on Linux because `FaultHostMedium.Kill` did not drop the dying run unit's Table 19 column lock. That lock is an open file
description's lock on Linux and is not the medium's, while on Windows the NO OTHER share mode is the lock. A real death closes
it, so the medium now owns the column locks established under `OwnColumnLocks()`, and `Kill` gives them back. That Linux run
also found the WSL filesystem out of inodes, filled by 394 `~/linux-gate` clones that `linux-gate.sh` never removes. The ones
older than three hours were pruned, and the leak is a lead in the report.

Gates at a18dc8b55: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population
run: Conformance 8,632 · Unit 922 · Characterization 31 ...` (run 000004-20261024T013348Z) and `=== LINUX GATE: GREEN (legs
selftests unit characterization conformance guard selftests-built; HEAD a18dc8b55) ===`.
