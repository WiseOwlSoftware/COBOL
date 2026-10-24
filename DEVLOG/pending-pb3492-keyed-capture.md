## PB3492: a keyed file replaced, removed or created under commit control is restored by ROLLBACK, and its DELETE FILE after a CLOSE proceeds

kb/Work PB3492 (cluster PB3353), wave 20261028 group A, the fourth attempt at the note and the keyed half of PB3340's journal.

**Re-probe.** On the base (9f815f941) the note's program printed `RECORD 1 ABSENT FS=23` after a ROLLBACK of an OPEN OUTPUT
that replaced a RELATIVE file (§14.9.36.4 GR3 restores *"the state they were in at the execution of the last COMMIT
statement"*: record 1 present) and `DELETE AFTER CLOSE FS=62` (the NOTE under §14.9.10.4 GR15: *"Such files can still be
deleted"*; GR15's conflict is a file *"locked by another file connector"*). A sibling of the same root reproduced too: an OPEN
OUTPUT that creates a keyed file under commit control, then ROLLBACK, left the file in place (an OPTIONAL OPEN INPUT answered
`00` where the state at the start of the run unit had no file and the answer is `05`).

**Mechanism and fix.** The keyed store keeps its UNDO in the file, so it cannot undo a verb that replaces or removes the whole
file. The keyed organizations became the journal's second client (DESIGN-runtime-library.md 2.2.2.14, rewritten to describe
the code): `FileConnector` now owns `CommitControlled`, the run unit's journals and the entry step for both organizations;
`KeyedConnector.JournalCapture` copies an existing file into the before file (R4) before an OPEN OUTPUT's creation and
`JournalCreation` begins START `A` before a creation; `FileRegistry.DeleteCovered` serves both organizations; `CommitKeyed`
promotes the keyed transaction and then decides the journal, and `RollbackKeyed` restores the before file and, before the
journal's `D R`, has the run unit's store adopt the restored file and run the dead-owner step, which rolls back the captured
transaction (its liveness byte left with the handle the verb gave back). An unsuccessful OPEN that did not affect the file
withdraws the journal it began (§14.9.27.4 GR25), so PB3493's refused-creation witness is unchanged. The '62' itself was the
Table 19 column lock: on Windows its holder is a handle on the file (`HandleColumnLockHost`) and it refused the removal; the
removal now gives the column back with the kept handle, for the sequential organization too (the same defect under `SHARING
WITH READ ONLY` or `ALL OTHER`). An uncovered DELETE FILE now runs the entry step (2.2.2.8: "at every DELETE FILE"), so a
dead run unit's capture is settled before the delete and no later settle brings the deleted file back. Determination (2.2.2.14):
a creation's ROLLBACK under a connector still OPEN keeps the file its OPEN created, with no records, and the creation standing.

**Witnesses.** Golden `conformance:cases/14.9.36-ROLLBACK/pb3492_keyed_file_replaced_or_removed` (RELATIVE and INDEXED:
replacement, removal, COMMIT, creation; the edition gate is PB3338's `pb3338_apply_commit_below_2023`). Unit
(`CommitRollbackRuntimeTests`): the capture, removal and creation rows refused at every operation of the journal's medium
(I11), the DELETE FILE after a CLOSE under NO OTHER and ALL OTHER, a dead run unit's capture settled by the next OPEN and by an
uncovered DELETE FILE, and the creation's ROLLBACK with the connector closed and open. Three PB3493 tests that encoded the
defect (the ROLLBACK after a refused final sync found nothing to restore; another run unit's OPEN beside a landed capture) now
expect the restore and the capture's (ALL) holding.

**Rows.** GR-9.1.18.4-p1, GR-9.1.18.5-p1, GR-14.9.36.4-3 PARTIAL → CONFORMS (batch `adjudication/w20261028a-pb3492/batch.json`,
GAP 53 → 50); Annex A.4.3 and A.3 items 6–7 Claimed in docs/CONFORMANCE.md, and README's declined list drops commit and rollback.

**Gate.** See the branch report (`w20261028a-PB3492-report.md`) for the verdict lines of the Windows gate and the Linux gate.
