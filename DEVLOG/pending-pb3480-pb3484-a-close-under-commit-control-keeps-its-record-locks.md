## PB3480 and PB3484: a CLOSE under commit control keeps its record locks, and a connector that leaves the registry holding what the CLOSE kept stays the COMMIT's

Two defects in the same place, the state PB3337 made a CLOSE of a connector under APPLY COMMIT keep (its handle, its Table 19 file lock, its attachment to the
run unit's store and the transaction in it). Both are runtime defects in `src/Cobol.Net.Runtime/IO/`; neither claims an inventory row.

**PB3480 (medium, wrong answer).** `FileRegistry.SharedClose` released a connector's record locks on every CLOSE, including a file in an APPLY COMMIT clause,
where §14.9.6.4 GR9 says the CLOSE does not (*"Except when the file is specified in an APPLY COMMIT clause, the file lock and any record locks associated with the
file connector ... are released by the execution of the CLOSE statement"*) and §9.1.16 releases them at COMMIT and ROLLBACK. Re-probed on the base: unit A READs
record 1 under commit control and CLOSEs; run unit B's READ of record 1 answered '00', not '51'. The R2-1 witness had pinned the '00'; it pins '51' now.
The fix is not the one-line guard the note's title suggests. The record locks are bytes published through the handle's region endpoint, and the CLOSE keeps that
endpoint, but a re-OPEN that takes a handle of its own (an INPUT re-OPEN after an I-O close, the R2-1 shape) gives the kept handle back, and with it every byte it
holds - the note's "second release" hazard, and worse than a double release: the locks would silently vanish from the host while this run unit's table still
claimed them. So the kept endpoint became the one owner of the locks published through it: `PhysicalFileTable.ReleaseThrough` (called by
`KeyedConnector.ReleaseKept` and `CarryKeptOver`), and `PhysicalFileTable.Rehome` moves them to the re-OPEN's endpoint inside the attach's store-mutex hold, beside
`KeyedStore.HoldLivenessThrough`, before the kept handle goes; a re-OPEN that fails moves them back (`AbandonOpen`, GR25). `FileConnector.CloseKeepsLocks` is the one
decision: the registry's CLOSE asks it and `KeyedConnector.CloseCore` keeps the handle on the same answer.

**PB3484 (low, wrong answer).** A connector displaced by the re-registration of its element after a CANCEL (`CloseDisplaced`, kb/Work PB168), or dropped with its
object (`CloseAndDrop`), kept its handle forever: another run unit's OPEN WITH NO OTHER stayed '61' after the COMMIT. Re-probed on the base, both. The note's fix
shape (release the kept state on every path that drops a connector) was **wrong**, and the correction is in the notes: §12.4.6.3.4 GR5 keeps a canceled element's
APPLY COMMIT clause active until a COMMIT or ROLLBACK deactivates it, and §14.9.7.4 GR3 gives the COMMIT the changes of the files of the active clauses, so
releasing at displacement would have turned a program's CANCEL, CALL, COMMIT sequence into a silent rollback by the next dead-owner step. The connector is RETIRED
instead (`FileRegistry._retired`): `TransactionConnectors` finds its transaction, and one `ReleaseAllKept` (COMMIT, ROLLBACK, `CloseAll` and so `Reset`) gives back what it
kept. `CloseAll`'s pending-object-close drain used to drop connectors before the release loop; the retired list covers it. The release also collects every
failure instead of stopping at the first, so one refusing handle cannot leave the next connector holding its own.

**Witnesses** (`CommitRollbackRuntimeTests`, 85 cases now; both organizations): the record locks across CLOSE, a second CLOSE ('42'), a re-OPEN INPUT and a re-OPEN I-O,
ended by COMMIT and by ROLLBACK; a refused re-OPEN (an INDEXED EXTEND re-open refused at its read of the prime root, after the attach) leaves them standing - the case
that fails when `AbandonOpen`'s move back is removed; a displaced connector, closed or still open, changed or only read, committed or rolled back; the replacement
connector sharing the displaced one's transaction; an instance file dropped by `CloseAndDrop` and by the queued GC close; `Reset` and `CloseAll`. On the base, 33 of the
81 cases then written fail; the other 48 are the existing ones and the `Reset`/`CloseAll` siblings, which were already right and are now pinned.

**Sibling sweep.** The arms of "a connector stops being in `_files`": `Register*` (via `CloseDisplaced`), `CloseAndDrop`, `CloseAll`'s drain and `Reset` are all covered; the
SEQUENTIAL organization has no APPLY COMMIT (PB3340), so `CloseKeepsLocks` is false there and its CLOSE is unchanged. Found and not fixed: a re-OPEN with a different
dynamic ASSIGN after a CLOSE under commit control adopts the OLD file's kept handle and store (reads the old file's records; repro in the report). It needs the kept state
to be a holder of its own, keyed by the physical file, not a field of the connector - a redesign of PB3337's structure, so it is a lead for a note, not part of this change.

**Self-review** against silent-failure-hunter, pr-test-analyzer and comment-analyzer: `Rehome` leaves a lock that another run unit took in the share-mode window recorded in this
table alone (documented at the method and in 2.2.1.10, the same window the liveness byte has); the release loops no longer abort at the first failure; two comments quoted the
standard in a paraphrase and were rewritten to the checked text. Docs: DOC-A.1-75, 2.2.1.10 As built (3), FILES_DESIGN's CLOSE paragraph.

**Branch review, round 1.** The reviewer found one blocking defect and two leads, all fixed here. R1-1 (blocking): a replacement connector's OPEN under SHARING WITH NO OTHER (the
I-O default) was refused '61' by the CLOSEd connector it displaced, whose kept handle (the share mode that is what is left of its file lock) belongs to the same run unit;
§9.1.13.9 1) owes '61' only against a file open by another file connector, and §9.1.15's lock is against other run units. The re-OPEN of the same connector gave the handle
back early or adopted it; the replacement got neither. The shape is now one: at the replacement's OPEN `FileRegistry.ReclaimRetired` hands the retired connector's kept state
to it (`KeyedConnector.InheritKept`: same name, organization and physical file, under commit control), so the replacement's OPEN IS a re-OPEN and every rule written for that
(adoption, early give-back, carry-over, the failed OPEN's retake) applies unchanged; an unmatched retired connector stays retired. 12 new cases (closed or open at the
displacement, read or write, both organizations) fail on the code before; they also pin '61' against another run unit until the COMMIT. R1-2 (lead): a kept handle given back
early and a re-OPEN then refused after the attach dropped the kept record locks out of this run unit's table with the new handle; `PhysicalFileTable.Unpublish` (the byte
half of `Rehome`) now takes them off the host and keeps the table entries until the COMMIT or ROLLBACK. R1-3 (lead): a retired connector's record locks the host could not
publish were never reached (its minted key leaves the registry); `ReleaseAllKept` now releases them by name through the connector's own physical-file state, tested with a
region-lock host that answers unavailable for record bytes. The review's R1-2 scenario (NO OTHER) sets no record locks at all (§12.4.5.9.4 GR3), so its witness forces
the early give-back with an OPEN phrase SHARING WITH NO OTHER over an ALL OTHER connector instead; both lead witnesses were mutation-checked.

Gate: `LEG 1 ONLY (batched gating, PB2515): GREEN`, run 20261021T182319Z-8a5f1f, Unit 34,189 cases ran, no failure.
