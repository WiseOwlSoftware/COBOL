## PB3337: transactions, COMMIT and ROLLBACK in the keyed store engine's runtime

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

**The second slice (wave 1137, the finisher of wave 1133's SPLIT).** It cherry-picked the four commits above onto main
and merged PB3471's branch, whose `FileHarness` and `FileOrg` the transaction tests now use. The re-probe on that build
ran the 23 cases green and found every crash-point and interleaving row still pending.

*Every row runs.* `TransactionFaultMatrixTests` puts real registries, real handles and the host's locks on one host file
and sends every run unit's PAGES through `KeyedStore.MediumObserver` to one shared `MemoryPageMedium`, the deterministic
fault-injecting medium of 2.2.1.12. A `DyingPageMedium` and a counting `RefusingPageMedium` sit over each run unit's
handle. Each case measures its path once and then faults it at every point of that measurement, on both organizations.
COMMIT killed at every write is rolled back before its slot write and committed after it; under a power loss it is
committed only once sync 2 ran. COMMIT refused at every read, write and sync answers by its region: intact with '51' to
another run unit before sync 2, reversed ('22' to the other run unit) at sync 2, and committed when the retirement is
refused. The reversal refused at every operation marks the file UNACKNOWLEDGED, and ROLLBACK answers '30' until a sync
lands. The reversal killed at every write is committed before its page 2 and rolled back after it; a power loss that
keeps only the COMMIT's slot is committed until the reversal's promotion syncs. ROLLBACK, the dead-owner step and a first
change are each killed and refused at every point. The holder's death lands at every operation of another run unit's
WRITE through a new witness seam, `FileRegistry.ReleaseLivenessForWitness`. That run unit's WRITE of a held record answers
'22', or '51' and then '22', never '00', and its WRITE of a record nobody holds answers '00' in every window. The
adoption matrix's transaction column, two holders, two transactions, power loss and the per-file COMMIT over two files
run too. 55 cases; a mutation of the slot boundary or of the '51' window turns nine of them red. `PendingStoreCases` is
deleted.

*GR4's '61'.* §14.9.7.4 GR4 releases the file lock of a file "closed and not reopened prior to the COMMIT statement", so
until then it holds. The CLOSE kept the handle, which is the whole lock on Windows, but it released the Table 19 column
bytes that a host without mandatory share modes publishes. On Linux another run unit's `OPEN WITH NO OTHER` therefore
succeeded against a file under a kept lock. The CLOSE now hands its run-unit file lock to the kept handle
(`FileConnector.KeepRunUnitFileLock`), COMMIT and ROLLBACK release it with the handle, and the connector's own re-OPEN gives
it back just before taking its own (`ReleaseKeptFileLock`). The witness is
`A_file_closed_under_commit_control_keeps_its_file_lock_until_the_commit`.

*Witness (e) of 2.2.1.11.* `A_transaction_costs_its_own_records_only` covers three file shapes. A ROLLBACK of 40 records
writes 30 pages at depth 2, and 104 under two alternate keys. It stays within m × (k + 1) × d plus the HOLDINGS pages
plus the statement's bookkeeping, and leaves another run unit's records byte-identical. The COMMIT after it reads its
UNDO tree once.

*The design, corrected from what the cases found.* 2.2.1.11's ROLLBACK row gave "+ 2 + 1" for the statement's
bookkeeping, which leaves out the ledger's path copy. 2.2.1.12's R5 row said a power loss keeping "any subset" of the
COMMIT's slot and the reversal's writes leaves the previous COMMIT. That holds only once the reversal's promotion has
synced; a power loss during the reversal follows 2.2.1.16's reversal rows. The 2.2.1 STATUS paragraph and 2.2.1.10's
"As built" (3) were updated too. PB3337 is landed and closes DOC-A.1-152; PB3338 is unblocked.

*Self-review.* The two transaction test classes shared one tree walker, now `TransactionTrees`, and their private `Org`,
`Tmp` and `TryDelete` copies are gone in favour of `FileHarness`. Two issues are left as leads: the re-OPEN window of a
kept handle whose share mode refuses the new one, and HOLDINGS keys compared as bytes under an ALPHABET.
