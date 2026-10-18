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
/// connectors' coherence were PB3333, PB3334 and PB3335's, and run now as cases of their own. Each owner turns its rows
/// into running cases when it lands; this class checks only that every row left names the held slice, a section and an
/// expected result.
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
        // The later core groups' paths. (PB3333's rows — the relative statements, their coherence, the lock test beside
        // the publication and the tree's COBOL surface for a relative file — run in RelativeEngineTests and, over the
        // engine, in CrossRunUnitKeyedStoreDriftTests' Org.Relative rows; the torn page's '30' through a compiled program,
        // with and without a USE procedure and a FILE STATUS clause and the phrases ignored, in the Conformance project's
        // RelativeEnginePermanentErrorWitnessTests; the phrases taken and not taken for '22', '23', '24' and '10' in the
        // relative goldens of conformance-suite/cases/9.1.13.4-, 9.1.13.5- and 9.1.14-, pb1192_relative_write_boundary_85
        // among them. PB3334's — the INDEXED connector's statements across run units with their alternate keys, ordinals
        // and '02' look-ahead, START over the trees, the refusals and deaths at every write of every statement, OPEN and
        // CLOSE included, and the boundary at a WRITE, a REWRITE and a DELETE — run in IndexedEngineTests,
        // FileBoundaryTests, CrossRunUnitKeyedStoreDriftTests' Org.Indexed rows and the golden
        // conformance-suite/cases/14.9.41-START/pb3334_start_indexed_tree_operators; the torn page's '30' and the
        // boundary's '24' through a compiled program, in every USE and FILE STATUS combination, in
        // IndexedEnginePermanentErrorWitnessTests. PB3335's — the connectors' coherence, StoreTable's attach and detach, the
        // cache dropped on adoption, a reader's entry beside a writable sibling with its torn and refused page 2, the
        // re-adoption's refusal at a CLOSE, the crashed-file reader of R6's last row — in StoreCoherenceTests.) What is
        // left is the held transaction slice's.
    ];

    [Fact]
    public void Every_pending_row_names_its_owning_group_a_section_and_an_expected_result()
    {
        Assert.NotEmpty(Rows);
        Assert.All(Rows, row =>
        {
            Assert.Equal("PB3337", row.Owner);
            Assert.StartsWith("2.2.1.1", row.Section);
            Assert.False(string.IsNullOrWhiteSpace(row.Row));
            Assert.False(string.IsNullOrWhiteSpace(row.Expected));
        });
        Assert.Equal(Rows.Count, Rows.Select(r => (r.Section, r.Row)).Distinct().Count());
    }
}
