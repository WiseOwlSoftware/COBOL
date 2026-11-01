## Entry 2959 — 2026-11-01 14:45 PST — PB4097: a dead APPLY COMMIT owner's rollback is every reader's, so no run unit reads its uncommitted change

A run unit under APPLY COMMIT that is killed leaves its changes since the last COMMIT in the file. §9.1.18.5 rolls them back
and only then releases the dead run unit's file and record locks, so a run unit that opens or reads the file afterwards must never
see them. The runtime did the rollback ("the dead-owner step") only at the OPEN and the statement entry of a connector with a
writable handle. An OPEN INPUT reader read the file as it stood. A design paragraph and DOC-A.1-75 called that Annex D.2.5's
tolerated read; PB4028's adjudication overturned it, and this entry is the fix.

**Re-probe.** On the branch before the fix, both organizations reproduced with the note's probes (a relative file: the reader
printed `REL 0007 R-SEVEN` after the owner was killed, and the IGNORING LOCK reader the same; a sequential file: `SEQ S-CHANGED`).
After a writable OPEN had run, the same readers printed the committed state. After the fix the readers print `REL 0001 R-ONE`
and `SEQ S-ONE / SEQ S-TWO` straight after the kill, with the live owner's `OPEN 61` unchanged.

**The fix.** Every access now settles first.

- *Keyed (RELATIVE and INDEXED).* A connector with no writable handle runs the dead-owner step through a handle opened for the
  purpose (`KeyedConnector.SettleDeadOwnersBeside`). A scratch store of the organization is adopted through a new read/write
  handle under the exclusive store mutex taken through that handle, `RollBackDeadOwners` runs with the connector's declared trees,
  and the store's CLOSE promotes. It runs at the connector's OPEN (`SettleDeadOwnersOfTheOpen`): a dead owner is looked for after
  the attach, the connector's own handle goes back first because a host with mandatory share modes refuses a writer beside INPUT's
  `READ ONLY` handle, and the OPEN takes its handle again. It also runs at every statement's coherent entry (`Enter`), for the
  readers whose share mode admits another run unit's writer: `KeyedStore.FindDeadOwner` reads the TRANSACTIONS tree only when
  page 2 names a transaction, the shared mutex is let go for the step and the statement enters again. The IGNORING LOCK read,
  START and READ ADVANCING ON LOCK are covered with the plain READ because every verb reaches the entry. A holder a reader meets
  dead at its lock test, having died after the entry's probe, is refused '51' (`HoldingsGate`) and the RETRY wait settles it.
- *Sequential.* The entry step now runs at OPEN INPUT as at every OPEN (`FileConnector.EnterPhysicalFile`; a live transaction's
  holdings refuse nothing there, a lock directory the process cannot list is '37'), and a READ that tests no lock (IGNORING LOCK,
  ADVANCING ON LOCK) settles the dead transactions all the same (`SequentialConnector.SettleDeadTransactions`, asked by
  `FileRegistry.JournalConflict`). Its settle already wrote through a handle of the entering run unit's own.
- *A reader that cannot write the file* is refused, never shown the dead change: '37' at the OPEN (the settle's handle raised
  `UnauthorizedAccessException`, §14.9.27.4 GR3), '61' for a share mode another run unit's handle refuses, '30' at a statement's
  entry (§9.1.13.6 1)). The note's fallback named '61' at OPEN and '51' at READ; those speak of another connector's locks and none
  stands once the owner is dead.
- The settle handle's share mode is `FileLockPosture.ForSettling`, and `SequentialJournal.Enter` reads a spelling the host cannot
  make absolute (`PathTooLongException`) as naming no lock directory, which keeps an OPEN INPUT of such a spelling at '35'.

**Docs.** DESIGN-runtime-library.md 2.2.1.10 (the overturned paragraph replaced by "The dead owner's rollback is every reader's
too", "As built" (2), the READ IGNORING LOCK sentence), the concurrency table row, and 2.2.2.8; CONFORMANCE.md A.4.3, DOC-A.1-28
(a non-COBOL program reading a live transaction's changes is the one left) and DOC-A.1-75.

**Witnesses.** `DeadOwnerReaderTests` (both organizations; READ ONLY and ALL OTHER readers; the IGNORING LOCK read; an owner dying
beside an open reader; the live owner still refused '61' and not rolled back; a read-only file refused '37') and two sequential
tests in `SequentialCommitControlTests`. Disabling the settle fails 10 of the 12 keyed cases, and the sequential entry step fails 4.
The tests that encoded the overturned determination now expect the rollback. There is no conformance-suite case: a case is one
program run once and cannot kill a run unit; the witnesses kill it with `DyingPageMedium` and `FaultHostMedium.Kill`.

**Row.** GR-9.1.18.5-p1 stays CONFORMS and gains seven witnesses and eight sites (batch `w20261808b-pb4097-dead-owner-reader`,
GAP 19 -> 19). kb/Work PB4097 is landed with `closes_rows: ["GR-9.1.18.5-p1"]`.

**Self-review.** Architecture: one mechanism (the dead-owner step) reached by every access, no wrapper or second copy; the
transaction-id listing is shared by `RollBackDeadOwners` and `FindDeadOwner`. Full code: the rollback is the clause's, not a
paraphrase; every § was checked with `cite.py --check`. Performance: a reader pays nothing while page 2 names no transaction;
an OPEN INPUT now costs one lock-directory listing, as every writable OPEN did. Duplication: none. The first gate went RED on
two cases: the drift test that keeps every FileShare in FileLockPosture, and the 40,000-character ASSIGN target an OPEN INPUT
now met in the entry step. Both are fixed. A host that cannot carry the liveness lock at all (`Unknowable`) still reads as alive,
the documented DOC-A.1-75 posture, so a reader there is not protected.

**Gate.** Windows `=== BUILD-LOCAL GATE: GREEN — Conformance 20,482/20,482 · Unit 39,239/39,239 · Characterization 37/37 cases ran
(skipped 0)` (run 000002-20261101T175356Z); `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built inert-watch;
HEAD 5538beb6f) ===`.

**Review round 1 (four blocking findings, all answered).** (R1-1) A READ's settle the host refused (the file read-only to the
reader, SHARING WITH ALL OTHER, owner killed beside it) escaped as an `UnauthorizedAccessException` out of the run unit, plain and
IGNORING LOCK alike; the keyed arm already gave '30'. `SequentialConnector.ReadHoldingsGate(previous, testsLocks)` now owns the
whole sequential READ entry step (the separate `SettleDeadTransactions` is gone, one method per job) and maps a host failure to the
permanent error. (R1-2) The OPEN INPUT's new entry step threw `ArgumentException` for a root path (`C:/`, `/`) that answered '05'
before; `SequentialJournal.Enter` now treats a spelling the host cannot make absolute or a root as listing no transactions
(`SidecarRegionLocks`' own three exception types), which also repairs the writable OPENs that reached the same throw. (R1-3) The
sequential OPEN INPUT answered '30' where the keyed one answered '37' for a settle refused for authority; `FileConnector.OpenOnce`
now maps the entry step's `UnauthorizedAccessException` to '37' for every organization. (R1-4) The keyed entry-time refusal is
pinned (read-only file after the reader's OPEN, owner killed: '30', then '30' again, IGNORING LOCK included). The lead R1-5, a
comment saying the OPEN's handle goes back only on some hosts, now says what the code does. New tests: two in
`SequentialCommitControlTests`, four in `DeadOwnerReaderTests`, five in `FileAuthorityPresenceTests`; the inventory row retires the
folded `SettleDeadTransactions` site (batch `w20261808b-pb4097-review-r1`).

