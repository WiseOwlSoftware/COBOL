## PB3337: transactions, COMMIT and ROLLBACK in the keyed store engine's runtime (first slice)

kb/Work PB3337 is the runtime half of the commit-and-rollback module, the owner's "Option A" of 2026-10-19. It is
built FROM docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.10, as corrected through review round 5. The re-probe
on the worktree's build found no transaction code at all. `FileRegistry` had no Commit, Rollback or
RegisterCommitControl, and nothing wrote the TRANSACTIONS or HOLDINGS roots, although the meta page has carried their
fields since PB3331.

**The engine.** `Store/Transactions.cs` is the one place that writes the three trees' keys and values:
TRANSACTIONS maps an id to its UNDO root, an UNDO tree maps (kind, identity) to a before-image or to nothing, and the
HOLDINGS tree maps (kind, identity) to the owning id. The same file holds `TransactionRetirement`, the one walk of a
transaction's UNDO tree that removes every key it meets from the HOLDINGS tree, removes the entry and empties the UNDO
tree. `BTree.Clear` supersedes every page of a tree so the statement releases them.

Every WRITE, REWRITE and DELETE through a connector under commit control writes its before-image and its holdings in
the same page statement as the change (`KeyedStore.Change`). The holdings are the record's identity, which stays held
after a DELETE, and every unique alternate key value the change frees. A first change draws its id by taking the id's
liveness byte, skipping held ones, and claims it through `PagedFile.Begin(drawnTransaction)`. A first change that is
refused gives the byte back.

COMMIT is a promotion with `committing` set to the id (`PagedFile.Commit`). Its retirement retires the transaction, and
the restart rule's completion and R6's fence retire through the same `Retire`. A refused sync 2 REVERSES under the
COMMIT's own mutex hold: the retirement runs as a rollback through the organization's `IRestoringTrees`, followed by a
promotion. When the reversal is refused too, the file is marked UNACKNOWLEDGED, and a later ROLLBACK goes through
`RollBackUnacknowledged`. ROLLBACK and a dead owner's rollback are one statement (`KeyedStore.RollBack`). The
dead-owner step runs at every exclusive coherent entry and at every writable OPEN.

**The lock test.** `KeyedConnector.HoldingsGate` runs after the invalid key conditions and before the record is touched,
for WRITE, REWRITE and DELETE in both organizations. The READ half runs in `FileRegistry.HoldingsConflict`, beside the
region test. A holding of the run unit's own transaction, met through a covered connector, lets the statement proceed.
Any other holding probes its owner's byte at that moment. A live owner answers '51', and RETRY applies. A dead owner is
rolled back at the test, and the statement is evaluated again.

**The registry and the termination.** `RegisterCommitControl` sets §9.1.16's implicit LOCK MODE IS AUTOMATIC WITH LOCK
ON MULTIPLE RECORDS. `Commit` works file by file, in the order of the run unit's open stores, per the owner's PB3454
decision. A refused file gets '30', and its name is returned for the compiler's EC-I-O-PERMANENT-ERROR. `Rollback`
releases the READ locks and the kept handles. A CLOSE under commit control keeps its handle while the transaction is
open. `CloseAll` commits. An abnormal ending, which `RunUnit.NoteEnding` records as an exception filter at the run-unit
boundaries, runs the file-only rollback first. The commit posture is exempt from the 255 and 15 ceilings.

**Docs.** DOC-A.1-152 is re-adjudicated from "Condition absent" to "Provided: one circumstance", a transaction's
holding. `LockedRecordStatusProducersDriftTests` now expects three producers. DOC-A.1-154 and DOC-A.1-155 gain the commit
posture's exemption. DOC-A.1-129 records the 1 015-byte prime key bound under commit control. DOC-A.1-75 records how a
dead owner is rolled back. Design 2.2.1's STATUS and 2.2.1.10's "As built" paragraph describe what was built.

**Tests.** `CommitRollbackRuntimeTests` holds 23 connector-level cases over two and three run units:
- ROLLBACK, COMMIT and the READ locks;
- the holdings against another run unit and against an uncovered sibling connector;
- every holding found by COMMIT and by ROLLBACK, checked with the HOLDINGS-equals-UNDO drift assertion;
- the dead owner, and a holder that dies during a RETRY;
- two transactions on one file;
- a CLOSE followed by a ROLLBACK;
- termination;
- 300 records with no ceiling;
- a refused first change;
- a COMMIT refused in region 1 and in region 3.

Eight `PendingStoreCases` rows now run as cases. The crash-point and interleaving rows stay pending; this slice
returns SPLIT with them as NEXT.
