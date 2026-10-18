// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>One PENDING case: a row of docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12, 2.2.1.16 or 2.2.1.17
/// whose path a later group builds, with its expected result taken from the design text. It is not a test yet (no
/// <c>Skip</c>: the gate would count a skipped case); the group named <see cref="Owner"/> turns it into a running case in
/// its own landing.</summary>
internal sealed record PendingCase(string Section, string Row, string Owner, string Expected);

/// <summary>
/// ⛔ THE ROWS OF THE CRASH MATRIX AND THE INTERLEAVINGS WHOSE PATHS THE PAGE LAYER DOES NOT OWN ARE WRITTEN DOWN WITH
/// THEIR EXPECTED RESULTS BEFORE THE ENGINE THAT RUNS THEM EXISTS (kb/Work PB3331, "The first deliverable"; PB3330's
/// Route from 2026-10-18). The held transaction slice (COMMIT, ROLLBACK, the dead-owner step and the holdings) is kb/Work
/// PB3337's; the record statements' lock tests and lock publication, the alternate-key and relative statements and the
/// connectors' coherence are PB3333, PB3334 and PB3335's. Each owner turns its rows into running cases when it lands;
/// this class checks only that every row names an owner that is not the page layer, a section and an expected result.
/// </summary>
public sealed class PendingStoreCases
{
    internal static IReadOnlyList<PendingCase> Rows { get; } =
    [
        // 2.2.1.12's adoption matrix, the transaction column.
        new("2.2.1.12 R6", "every matrix row × a transaction open at the crash", "PB3337",
            "after the adoption every before-image of the dead owner re-applied by the first writable statement (an OPEN's R6 step 5 or a live run unit's coherent entry, whichever comes first) and left in place by an INPUT OPEN; a consistent tree in every cell"),
        new("2.2.1.12 R6", "an OPEN by a third run unit after one of two holders died, with a transaction", "PB3337",
            "by the live holder's next statement if it comes before the OPEN, else by the OPEN, with the live holder's own records untouched either way"),
        // 2.2.1.12's R5 restart rows for COMMIT.
        new("2.2.1.12 R5", "a COMMIT refused before its slot write", "PB3337",
            "unwound by the next entry (another run unit's statement or the owner's own ROLLBACK, which then restores every before-image); its transaction, UNDO tree and holdings intact until then"),
        new("2.2.1.12 R5", "a COMMIT refused in its retirement (after sync 2)", "PB3337",
            "the COMMIT reports success, and the next entry completes its retirement"),
        new("2.2.1.12 R5", "a COMMIT whose sync 2 is refused (finding R5-1)", "PB3337",
            "REVERSES under its own mutex hold and reports failure; afterwards the records are as at the previous COMMIT and the HOLDINGS and TRANSACTIONS trees hold nothing of t, through the owner's ROLLBACK, another run unit's statement, the next OPEN and a power loss keeping any subset of {the COMMIT's slot, the reversal's pages, the reversal's slot}"),
        new("2.2.1.12 R5", "the reversal's own page write or its promotion's sync refused", "PB3337",
            "the COMMIT reports failure, the ROLLBACK reports failure while the sync is refused and success once a sync lands; no run reports a ROLLBACK successful while the medium holds the COMMIT's slot as its highest valid slot"),
        // 2.2.1.12's transaction drift tests.
        new("2.2.1.12 transaction", "two run units under APPLY COMMIT on disjoint records", "PB3337",
            "A's ROLLBACK restores A's records to A's last COMMIT and leaves B's as B left them; a CLOSE then ROLLBACK restores the last COMMIT"),
        new("2.2.1.12 transaction", "a first change under commit control refused at its n-th write (R4-2)", "PB3337",
            "no liveness byte held afterwards; page 2's transaction mint above the drawn id; another run unit draws the next id with no wait"),
        new("2.2.1.12 locks", "A under APPLY COMMIT DELETEs K5 without a READ; B WRITEs K5", "PB3337",
            "'51'; after A's ROLLBACK the file holds A's K5 as at A's last COMMIT"),
        new("2.2.1.12 locks", "A WRITEs K1; B READ K1 / READ IGNORING LOCK / REWRITE K1", "PB3337", "'51' / the record / '51'"),
        new("2.2.1.12 locks", "A REWRITEs K1's unique alternate key X to Y; B WRITEs K2 with X, then with Y", "PB3337", "'51', then '22'"),
        new("2.2.1.12 locks", "every holding is found (R5-2): X to Y to Z, W then DELETE, then COMMIT or ROLLBACK", "PB3337",
            "B's WRITE with Y and with W answers '00'; the HOLDINGS tree is EMPTY; after every statement its key set equals the union of the UNDO trees' key sets"),
        new("2.2.1.12 dead owner", "A under APPLY COMMIT DELETEs K5 (unique X) and is killed; B WRITEs K5, K7 with X", "PB3337",
            "the entry rolls A back first: '22', '22'; B READs K5 as at A's last COMMIT; a third run unit's INPUT read between finds K5 absent, after B's statement present"),
        new("2.2.1.12 dead owner", "the holder's death at every point of another statement (R4-1)", "PB3337",
            "'22' (rolled back at B's test) or '51' (alive at B's test, RETRY then '22'), never '00'; no record B wrote overwritten by A's rollback"),
        // 2.2.1.16's crash points of the transaction paths.
        new("2.2.1.16", "COMMIT: before the claim / after the claim / after the slot write / after sync 2 / after the retirement", "PB3337",
            "rolled back / unwound then rolled back / completed or rolled back by the slot alone / committed / complete"),
        new("2.2.1.16", "COMMIT with sync 2 refused and the owner alive: the reversal rows", "PB3337",
            "committed where the reversal never landed, rolled back wherever it did, and rolled back for every later crash once its slot synced"),
        new("2.2.1.16", "ROLLBACK and the dead-owner step: before / after its page 2", "PB3337", "redone (idempotent) / complete"),
        new("2.2.1.16", "a transaction's first change, before page 2", "PB3337", "no entry; the byte went with the handle"),
        // The refused-statement table's transaction rows.
        new("2.2.1.16 refused", "a transaction's first change / ROLLBACK / COMMIT at each point / the dead-owner rollback, refused", "PB3337",
            "as the table's rows: the byte given back; '30' replayed; unwound or completed by the slot; the next writer's entry redoes it"),
        // 2.2.1.17's rows with transactions, locks and dead owners.
        new("2.2.1.17", "A's statement ∥ B's ROLLBACK; A's COMMIT ∥ B's ROLLBACK; the reversal rows; B dies with a transaction (both rows); B's transaction begins / gives its byte back ∥ A's dead-owner step", "PB3337",
            "as the table: '51' on B's records with RETRY; either order durable for A, restored for B; B never completes a COMMIT A was told failed; A rolls B back before its own statement or at its lock test"),
        new("2.2.1.17 round 4", "a lock holder's death at every point of another run unit's statement", "PB3337",
            "the round-4 table: K5 '22', K7/X '22', K9 '00'; a READ lock goes with the handle"),
        // The later core groups' paths.
        new("2.2.1.17", "A's lock test (READ, WRITE, REWRITE, DELETE) ∥ B's lock publication", "PB3333",
            "the record-lock region, publish-then-test (PB2660): '51' with RETRY outside the statement"),
        new("2.2.1.17", "A's statement ∥ B's statement through the RELATIVE connector under every sharing mode but NO OTHER", "PB3333",
            "the coherence condition of 2.2.1.7 (AdmitsAnotherRunUnit with the mutex); every I-O status the statement answers, the exception ones included ('30' for a refused write or sync, '51' for a held record), AT END / INVALID KEY taken and not taken, with and without USE and FILE STATUS"),
        new("2.2.1.16 refused", "the boundary (the format's 2^32 pages, the host's EFBIG) at a WRITE, a REWRITE and a DELETE through the RELATIVE connector", "PB3333",
            "WRITE and REWRITE: '24', the INVALID KEY phrase taken and NOT INVALID KEY not taken (§9.1.14, §9.1.13.5 item 4); DELETE: '30', no invalid key condition, EC-I-O-PERMANENT-ERROR with and without a USE declarative and a FILE STATUS item (§9.1.13.6 item 1; §14.9.10.4 GR4 gives a DELETE the invalid key condition only for a record the file does not contain); the record still present and the file unchanged in every cell"),
        new("2.2.1.16 refused", "the boundary (the format's 2^32 pages, the host's EFBIG) at a WRITE, a REWRITE and a DELETE through the INDEXED connector", "PB3334",
            "the same as the relative rows, the DELETE's rule being §14.9.10.4 GR3: '24' with INVALID KEY at the WRITE and REWRITE, '30' with no invalid key condition at the DELETE"),
        new("2.2.1.17", "A's statement ∥ B's statement through the INDEXED connector: alternate keys, ordinals, collations", "PB3334",
            "the same, plus §14.9.30.4 GR26's duplicate order kept across run units and the '02' look-ahead"),
        new("2.2.1.17", "the connectors' coherence: StoreTable attach/detach, the page cache dropped on adoption", "PB3335",
            "a sibling connector's change visible instantly; another run unit's change adopted at the next coherent entry, the cache dropped"),
        // The B+ tree's COBOL surface (kb/Work PB3332 built the tree; its statements are the connectors').
        new("2.2.1.12 tree", "START with each relational operator over the tree's seek, a full key and a partial key, the key found and not found", "PB3334",
            "§14.9.41.4 GR17 c) to e): the first record satisfying the comparison (LESS and its kin: the last, the file searched in reverse) becomes the position, '00' with NOT INVALID KEY; none satisfies it: '23' with INVALID KEY taken; each with and without a USE declarative and a FILE STATUS item"),
        new("2.2.1.12 tree", "READ NEXT / READ PREVIOUS stepping off the tree's last / first entry, and across leaves another run unit split", "PB3333",
            "AT END taken with '10' at the edge and NOT AT END on every entry before; a cursor whose tree another run unit changed re-seeks from its key after the coherent entry (BTree.Adopt) and returns the next record of the new tree"),
        new("2.2.1.12 tree", "WRITE of a present RELATIVE record number or prime key: the tree's Insert declines", "PB3333",
            "'22' with INVALID KEY taken and the file unchanged; absent: '00' with NOT INVALID KEY"),
        new("2.2.1.12 tree", "READ, REWRITE and DELETE by a key the tree does not hold", "PB3333",
            "'23' with INVALID KEY taken; present: '00' with NOT INVALID KEY"),
        new("2.2.1.12 tree", "a record statement whose tree read meets a torn or stale page (R3)", "PB3333",
            "'30' (§9.1.13.6 item 1) at the statement, the file unchanged, the connector in §9.1.13.1's permanent error; EC-I-O-PERMANENT-ERROR with and without a USE declarative and a FILE STATUS item, execution continuing or terminating as the spec rule says"),
    ];

    [Fact]
    public void Every_pending_row_names_its_owning_group_a_section_and_an_expected_result()
    {
        Assert.NotEmpty(Rows);
        Assert.All(Rows, row =>
        {
            Assert.Contains(row.Owner, new[] { "PB3333", "PB3334", "PB3335", "PB3337" });
            Assert.StartsWith("2.2.1.1", row.Section);
            Assert.False(string.IsNullOrWhiteSpace(row.Row));
            Assert.False(string.IsNullOrWhiteSpace(row.Expected));
        });
        Assert.Equal(Rows.Count, Rows.Select(r => (r.Section, r.Row)).Distinct().Count());
    }
}
