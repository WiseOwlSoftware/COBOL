## Entry 2426 — 2026-10-23 17:21 PDT — Commit control on the file registry: a deactivated clause releases its files (PB3541); PB670: at --std 85 the file sharing facility is not in effect, so no clause-less connector refuses another and no OPEN answers 61

### Commit control on the file registry: a deactivated clause releases its files (PB3541)

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

**PB3492 re-probed and settled; PB3340 not started (the group SPLIT at the note boundary).** On this branch's build a RELATIVE
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

**PB3340, the journal and its medium (wave 20261024, group A).** The predecessor's five commits were cherry-picked onto
5229bde65 and re-gated GREEN (run 000001). The sequential journal of DESIGN-runtime-library.md 2.2.2 is now code, written
from the executable model (`scripts/models/sequential_journal_model.py`) operation for operation:
`IO/Sharing/SequentialJournal.cs` holds the three transient files, the id drawing, the record format (a 24-byte header with
its CRC-32C, then `L`, `R`, `K` and `D` records each CRC-framed, a torn tail ending the records), the write-ahead rules R1 to
R5, COMMIT's three regions, the idempotent ROLLBACK, the settle, the entry step, the lock test's journal arm and the entry
mutex (`HostRegionLocks.EntryMutexByte`, held through a new handle-free `SidecarRegionLocks.WithoutHandle` on every host).
Every operation it makes on the operating environment goes through the new `IHostMedium` seam, whose production medium
spends three new `HostFile` roles (the transient journal file, the restore handle, the directory sync of
`IO/Sharing/HostDirectory.cs`: `FlushFileBuffers` on a backup-semantics directory handle on Windows, `open` and `fsync`
elsewhere). `tests/Cobol.Net.Tests.Unit/Journal/` runs 2.2.2.12's rows over real files through a fault-injecting medium that
refuses, kills or cuts the power at every operation (four power-loss shapes: everything unsynced lost, all kept, the data
file's kept and the journal's lost, the converse), each followed by the next entry, and asserts the model's I1 and I3. Its
worth was proved by mutation: each of R1, R2, R3 and R4 broken, and the branch review's R1-1 put back, turned the sweep RED on
its own rows. The SEQUENTIAL connector does not drive the journal yet; its kept handle, the verb hooks and the compiler's
acceptance of the operand are the rest of PB3340.

### PB670: at --std 85 the file sharing facility is not in effect, so no clause-less connector refuses another and no OPEN answers 61

Owner determination 2026-10-23, option (i). Re-probe on this build before the change: two clause-less SELECTs on one file at `--std 85`
(`OPEN INPUT` x2, then `OPEN I-O`, `OPEN EXTEND`, `OPEN OUTPUT` beside them) answered 00 00 61 61 61 and 61 61 for the later pairs; the same program
under `cobc 3.2 -std=cobol85` (WSL) answers 00 for every OPEN. Table 19 and its status 61 are the COBOL-2002 file sharing module (Annex A.4.7, optional;
Annex A.4.1 makes its "exception conditions, and I-O status values" optional with it), and section 9.1.15 lets the implementor define the default
sharing mode as "a mode completely specified by the implementor". At 85 that mode restricts nobody.

**The estimate was too small.** The note named "a DialectBehavior row read by SharedOpenAttempt". Skipping the Table 19 loop there was one of four things:
1. The first connector's host handle carries a share mode derived from its sharing mode (`FileLockPosture.OfSharingMode`), so a NO OTHER or READ ONLY
   default refuses a sibling at the operating system. The 85 default is ALL OTHER (`FileRegistry.ImplementorDefaultSharing(mode, edition)`, a changed public
   signature), whose `FileShare.ReadWrite` admits every sibling.
2. The cross-run-unit column lock (`RunUnitFileLock`) refuses an in-process sibling like any other holder (OFD and handle locks do not know a process's own
   descriptors), so `FileConnector.Open` takes none at 85.
3. A host refusal by a process outside the run unit answered 61, a status 1985 does not define; at 85 it is 30 (section 9.1.13.6 item 1).
4. The unit test over every pair of opens found what the arbitration had been hiding: a keyed `OPEN OUTPUT` beside a connector that holds the file threw
   `InvalidOperationException` from `StoreTable.AttachCreated` (PB754's guard assumed Table 19 had run). An OPEN has only I-O statuses as outcomes, so at 85
   (`arbitrated: false`) the created store replaces the path's entry, as for a file replaced at the path from outside (PB3533); the holders keep the store they
   have and meet an empty file (`'10'`, section 14.9.27.4 GR18). From 2002 the guard stays loud.

One read for all four: `DialectBehavior.FileSharingFacility` (ChangedIn 2002), through `FileConnector.SharingFacilityInEffect`. Its ledger row is Table 7 row 7.39
of the version change reference (a derived edge, stated as such). `DialectBehaviors.Row.VcrRow` became a string for it (a Table 7 row is `7.39`), so the runtime
major is 15 and the ABI pin moved. `DialectBehaviorRegistryDriftTests` holds the new row to the ledger in both directions.

**Goldens and tests.** `conformance:wiseowl/cases/A.1-Implementor-defined-language-element-list/pb670_cobol85_no_sharing_facility` (accept 85; the statuses equal
GnuCOBOL's; the 2002 control is the existing `pb322_default_sharing_by_open_mode`). `SharingFacilityEditionTests`: every pair of open modes in every organization
at 85 (all 00, both CLOSEs 00) and at 2002 (Table 19's cell), the default mode by edition, an outsider's exclusive handle (30 at 85, 61 from 2002, file unaffected,
re-OPEN succeeds), a second run unit, two EXTENDs keeping both records, the empty file a holder meets after another connector's OUTPUT (all three organizations),
and GR5 visibility between two updaters. `CrossRunUnitKeyedStoreDriftTests` gained the unarbitrated creation. Rows: DOC-A.1-77 and DOC-A.1-131 stay CONFORMS and
gain the 85 band (`closes_rows: []` with its reason). The NIST 85 corpus (364 programs) ran green with the posture change before any test was written.

**Cost, and the review that measured it (R1-1).** The first cut gave every 85 connector the ALL OTHER posture, so the sequential writer flushed each release and the
reader re-anchored at every frame (the paths 2002 sharers use). The NIST 85 corpus (small files) showed nothing; a 3,000,000-record file did: WRITE 6.3 s at 85
against 1.1 s at 2002, READ 8.8 s against 5.5 s (reviewer), 9.4 s and 9.9 s against 1.2 s and 5.5 s on my debug build. The root cause is that the posture was read as "a
sibling may exist" for the life of the OPEN. Now `FileConnector.OtherConnectorsMayShare` asks the run unit's physical-file table whether another connector is open on
the file: alone, the writer buffers (as the 2002 clause-less OUTPUT does) and the reader compares the release generation only; a sibling that arrives later is owed the
records already written, so `FileRegistry.SharedOpenAttempt` has every connector open on the file `ReleaseBufferedRecords` before the incoming body runs (a host
refusal there is held by the writer and answered by its own next WRITE or CLOSE, '34'/'30', never by the opener); while the sibling is open each WRITE reaches the
medium at once, and when it closes the writer buffers again. After: the same programs run WRITE 1.14 s and READ 5.36 s at 85, 1.06 s and 5.72 s at 2002.
`SharingFacilityEditionTests` pin the buffering, the arrival, the departure, the 2002 controls and the refusal (Windows: a locked byte range; Linux: /dev/full).
Not changed: the keyed stores still run coherent at 85 (a store mutex per statement, because the engine writes shared pages and an outside process could
otherwise corrupt it): 9.2 s against 8.1 s on 200,000 INDEXED writes and reads, about 10 %, left as a lead. Cross-process protection at 85 is the operating
environment's alone.

**Self-review.** Architecture: one predicate, one mechanism per job (arbitration, default mode, run-unit lock, host status, keyed creation). Code: the old
one-argument `ImplementorDefaultSharing` was changed at every caller (no overload). Duplication: none. Found and fixed in the files read: the stale "(an owner
question)" sentence in `pb143_shared_visibility`, PB322's open observation now answered here. Silent failures: the status mapping of a host refusal is the only place
a new value could hide, and it is pinned both ways.

Gate: first run 000001-20261023T223458Z RED on two unit rows of my own GR5 test (a 5-character payload in an 8-byte record), fixed; run 000002-20261023T223918Z: `LEG 1 ONLY (batched gating, PB2515): GREEN` at aedca289b, the golden `SuiteTest_IsNoWorseThanItsVerdictAllows(edition: "85", id: "pb670_cobol85_no_sharing_facility")` Passed; Linux (unit, conformance, self-tests): `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD aedca289b) ===`.

Review round 1 (R1-1): gate run 000001-20261023T230921Z RED on `StoreCoherenceTests` (it pins which file asks `AdmitsAnotherRunUnit`; the question moved to `FileConnector.OtherConnectorsMayShare`), fixed in 2c49a574e; run `TestResults/build-local/000002-20261023T231857Z`: `LEG 1 ONLY (batched gating, PB2515): GREEN` (Unit 35,166/35,807 incl. the 5 new `SharingFacilityEditionTests` rows Passed, Conformance 10,436/18,327); `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD 2c49a574e) ===` (the /dev/full arm of the refusal test ran there); semgrep verify PASS.

**Review round 2 (R2-1).** Narrowing the 85 reader to the release generation (R1-1) made a sibling OPEN OUTPUT's truncation invisible: `OpenCore`'s OUTPUT arm announced nothing, so an open INPUT reader kept serving read-ahead records the file no longer held (the reviewer's tr.cob: 50 records, OPEN INPUT + READ, OPEN OUTPUT on a second SELECT of the same path, READ answered 00 OLD-0002 where the file is empty; at the round-1 head the per-frame re-anchor answered 10). Root fix: the OUTPUT arm calls `NoteRelease()` (a truncation is a writer of the medium; the old comment "a truncating sibling cannot arise" is true only from 2002, where Table 19 refuses it). Pinned by `AtCobol85_ASiblingOutputThatTruncatesTheFileIsHeardByAnOpenReader` (fails without the call: the READ answers 00) and, as the sibling-arm sweep, `AtCobol85_ASiblingOutputThatTruncatesAKeyedStoreIsHeardByAnOpenReader` (RELATIVE, INDEXED: already correct, the store is read per statement). Other writers of the medium swept: EXTEND appends (reader finds them through the release generation), REWRITE (NoteRelease), the delete/replace of a keyed store (per-statement coherence). Citation checked: 14.9.30.4 21) c) "existing record in the physical file".

Review round 2 gate: run `TestResults/build-local/000001-20261023T234903Z`: `LEG 1 ONLY (batched gating, PB2515): GREEN` (Unit 35,180/35,811 incl. the 4 new truncation rows Passed, Conformance 10,461/18,327); `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD ec063deb2) ===`; semgrep verify PASS.

