## Entry 2332 — 2026-10-21 14:24 PDT — PB3485: the transactions' holdings are the file's keys, not their bytes

PB3337's HOLDINGS and UNDO trees (the locks a transaction keeps in the file, and the before-images a ROLLBACK restores) were
ordered by their bytes, on the design's own sentence "the keys are identities, compared for equality, never for a collating
sequence". The note asked whether a key the file treats as equal can have a different byte image. It can: an INDEXED file
whose collating sequence makes two key images one key (`ALPHABET … ALSO`, "A" and "a" in one position) has a record whose
identity no byte compare decides (§12.4.5.12.4 GR1, *"The equality or inequality is based on the collating sequence used for
the file according to the rules for a relation condition"*; §14.9.51.4 GR35 for a unique alternate key's value).

**Re-probe.** Seed "abcd"; run unit A, under commit control, DELETEs it by the image "abcd" (holding R"abcd"); run unit B
WRITEs "ABCD" and got '00' where the holding makes it '51' (§9.1.16, DOC-A.1-152). The same defect had three arms, all "a key
is its bytes":

1. The UNDO and HOLDINGS trees. A holding taken under one image was invisible under another, and a transaction that changed
   one record under two images kept two before-images, the second one the first change's after-image, for the ROLLBACK to
   restore. `HoldingsKeyOrder` (Store/Transactions.cs) now orders them: a record holding through the prime tree's order, a
   value holding through its alternate key number and then that key's order. The page layer retires a transaction (the OPEN's
   fence, the restart rule, a COMMIT's retirement) with no connector in reach and reads a tree by the order it was built
   under, so `PagedFile.Open` and `Create` take the order (`PagedFile.HoldingsOrder`) from the connector; 20 test callers
   changed, with no default to forget. A key number the table has none for (the `COBOLNET_KEYCHECK` opt-out) compares as
   bytes.
2. The record's identity. REWRITE and DELETE took the statement's image of the key, so a ROLLBACK re-inserted the record
   under it and changed the key image its prime tree holds. `IndexedStore.Get` now also returns the key the tree stores
   (`BTree.TryGet(key, out storedKey, out value)`); REWRITE, DELETE and the random READ use it.
3. The sibling the sweep found: the §9.1.16 record lock was the statement's image too
   (`IndexedConnector.MutationTargetRecordIdCore`, `FindRandom`'s prime arm), so a READ WITH LOCK by "abcd" did not conflict
   with a REWRITE by "ABCD" in another run unit. Both now name the record by its stored image, as the sequential READ and
   the alternate-key paths already did.

**Goldens.** `tests/Cobol.Net.Tests.Unit/Store/HoldingsKeyOrderTests.cs`, 11 cases, nine of which fail with the fix removed:
the lock test of a DELETE, a REWRITE and a freed unique value by every image; two changes under two images restore the first
image and the stored key image; the record lock across run units; a dead owner rolled back by the next writer and by the next
OPEN; a COMMIT refused at every read, write and sync under variant images (its reversal included: '22' after sync 2, '51'
before the slot); the order itself, with its record numbers and its refusal of a partial key. The 404 cases of the Store
namespace pass with the changed signatures. The design (2.2.1.10 As built (4)) says the keys are ordered by the file's key
orders; it had said they compare as bytes.

**Gates.** Windows: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Unit 33,905 cases in leg 1, the 11 new
ones among them). Linux, `--legs unit,conformance`: `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built;
HEAD e23bc072c)`, 18,047 conformance cases.

**Self-review** (pr-test-analyzer, silent-failure-hunter, comment-analyzer, type-design-analyzer on the new type). The
order throws `NotSupportedException` for the partial-key members rather than guessing, because no seek of these trees takes
a partial key; `MarksOf` returns zeros, which `BTree` stores in the root header. No inventory row is claimed or closed.

