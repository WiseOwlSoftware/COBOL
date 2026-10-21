## PB3493, PB3494: what a CLOSE under commit control keeps belongs to the physical file, and an OPEN OUTPUT ends it only when the new file exists

A connector under commit control keeps its handle, its record locks, its file lock and the run unit's transaction when it is
CLOSEd (§14.9.6.4 GR9, §14.9.7.4 GR4). Two reviews of PB3483 found two ways that kept state was lost or misused. Both
notes reproduced on this build; the baseline run is in `E:/COBOL-coord/scratch/w1141b/probe-baseline-evidence.log` (ten of the
fourteen new cases fail on the merge base).

**PB3493: the OPEN OUTPUT released the kept state before it had its own handle.** `Lock(create: true)` called `ReleaseKept`
first, so an OUTPUT the host or the medium then refused (a third run unit opening in the share-mode window, a refused write
or sync of the creation) left the connector without its transaction, as a death leaves it (§14.9.27.4 GR25: *"If the
execution of the OPEN statement is unsuccessful, the file is not affected"*). Test: the medium refuses the creation's second
write, or its first sync; before the fix the OPEN answered '30' and `TransactionOf` read 0 where the CLOSE's id stood.
`KeyedConnector.Recreate` now makes the new empty store in a store of its own, off the run unit's table, through the OPEN's
handle and beside the kept attachment. `PagedFile.Create` already undoes a refused creation page by page and hands back no
store, so a refusal changes nothing; only a creation that landed gives the kept state back and registers the created store
(`StoreTable.AttachCreated(host, created)`). The one failure that lands the creation is the final sync's, after the
truncation: `Create` now hands the store back with its '30', and the kept state ends with the file it described.

The sweep found the same ordering in two siblings. The absent OPTIONAL I-O/EXTEND arm released the kept state before its
handle; it releases after. The absent OPTIONAL INPUT arm attached beside the kept store without releasing it, so the table
kept the removed file's store: a file made anew by another run unit then read as the old one. Both end the kept state of a
file that is no longer there, after the new handle is taken.

**PB3494: the kept state belonged to the connector, so an ASSIGN USING to another file adopted the old file's handle and
store.** After a CLOSE under commit control, a MOVE to the ASSIGN USING item and an OPEN associate the connector with a
second file (§12.4.5.3 GR3 b). The OPEN took the first file's kept handle and store: READ of record 5 of a three-record
second file answered '00' with the first file's record. The decision is that the changes made to the first file stay changes
to a file the connector's APPLY COMMIT clause names (§14.9.7.4 GR3, §14.9.36.4 GR3), so its transaction, record locks and
file lock are carried until the COMMIT or ROLLBACK, as for a displaced connector (PB3484). `KeptFile` (new) is the kept
state keyed by its physical file: the handle, endpoint, file lock, and now the file's path, physical-file state, store and
restoring trees. `KeyedConnector.TakeKept` hands it to the registry; `FileRegistry.Associate` does so when the association moved
to another file, and the displaced-connector path (`Retire`) uses the same hand-over, so one mechanism serves both. COMMIT and
ROLLBACK find it through `TransactionHolders`, a refused one is told through the file-name's connector, the connector that
opens the same file again takes it back (`InheritKept`), and the run unit's end commits and releases it. On the way:
`IndexedConnector.Restoring` read the connector's current store for the frame slots when it ran, so a ROLLBACK after the
connector had left the store could not decode the before-images; the restoring trees now take the slots from the file they
restore. The Table 19 column the first part of the note said was amended on the old file was the same root: the connector
holds no kept lock after the hand-over, so the OPEN takes its column on the new file.

**Tests** (`CommitRollbackRuntimeTests`, both organizations): creation refused at a write and at a sync (4), final sync refused
(2), creation landed (2), a vanished OPTIONAL file on INPUT and I-O (4; effective on a host that lets a file be removed while a
handle stands, so on Linux, and on Windows the case asserts the host refuses the removal), a kept state left behind by an
association with another file ending in COMMIT, ROLLBACK, a return to the first file and the run unit's end (8), and a refused
ROLLBACK of such a state (1). The design (2.2.1.10 As built (3)) says what is carried and where; PB3493 and PB3494 are
landed with `closes_rows: []` and their reasons.

**Self-review.** The silent-failure pass looked at every path that answers '30' with state changed: `Recreate` releases only
when `FileReplaced`, and a refused COMMIT or ROLLBACK of a retired state is told by name. The test pass checked that the
creation-refused cases fail without the fix (they do) and that the abnormal paths are covered. The comment pass corrected the
`TryRestoreKeptFileLock` remark that said an unsuccessful OUTPUT created the file anew. The first gate failed one drift test
(`SharedReadCoherenceDriftTests`: the keyed lifecycle sizes the input-output areas in one place), so `Recreate` and `Attach`
share `AttachHandleTo`.

**Gates.** Windows, at `898b2315a` (run `TestResults/build-local/20261021T221239Z-622f69`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN — … Conformance 10,724/18,047 · Unit 34,118/35,285 · Characterization 5/36 cases ran`; the creation and
association cases ran in leg 1. The first gate (`…T220701Z-25adee`) was RED on the drift test above. Linux (`--legs unit,conformance`)
at the same head: conformance GREEN 18,047/18,047, unit RED on four cases of the vanished-file test, which is effective only on
Linux: it wrote through a connector name it never registered (`Write(maker, …)` without `name: "S"`). Fixed in the next commit; the
Linux unit leg has to run again on it (the wave's stop file ended this run before it could).
