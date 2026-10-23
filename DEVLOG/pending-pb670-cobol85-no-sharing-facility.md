## PB670: at --std 85 the file sharing facility is not in effect, so no clause-less connector refuses another and no OPEN answers 61

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
