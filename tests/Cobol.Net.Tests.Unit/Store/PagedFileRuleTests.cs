// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;
using Xunit;
using Fault = CobolNet.Tests.Unit.Store.MemoryPageMedium.Fault;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE SIX RULES OF THE GENERATION LEDGER HOLD ON EVERY WRITE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.6
/// R1-R5 and §2.2.1.12's drift tests; kb/Work PB3331). Each case drives <see cref="PagedFile"/> over the deterministic
/// <see cref="MemoryPageMedium"/> and judges it from the medium's own log and pages (<see cref="StoreKit"/>'s auditor),
/// never from the engine's account of itself; every expected value is the rule's text.
/// </summary>
public sealed class PagedFileRuleTests
{
    // ── R1: a page that either slot's tree references is never written ─────────────────────────────────────────

    [Fact]
    public void R1_no_write_ever_targets_a_page_the_committed_or_the_work_tree_references()
    {
        var (medium, _, store) = StoreKit.NewStore();
        for (int i = 1; i <= 12; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i, $"v{i}"));
        for (int i = 1; i <= 12; i += 2) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i, $"w{i}", blobPages: 2));
        for (int i = 2; i <= 12; i += 3) Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, i));
        Assert.Equal(StoreStatus.Ok, store.Promote());
        for (int i = 20; i <= 30; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i, $"x{i}"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, status, again) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, status);
        for (int i = 1; i <= 10; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(again!, i, $"y{i}"));
        StoreKit.AssertR1(medium);
        StoreKit.AssertNoStampReused(medium);
    }

    // ── R2: the ledger ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void R2_the_ledger_lists_exactly_the_pages_allocated_since_the_promotion_after_every_statement()
    {
        var (medium, _, store) = StoreKit.NewStore();
        StoreKit.AssertR2(medium);
        for (int i = 1; i <= 8; i++)
        {
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i % 3, $"r{i}", blobPages: i % 2));
            StoreKit.AssertR2(medium);
        }
        Assert.Equal(StoreStatus.Ok, store.Promote());
        StoreKit.AssertR2(medium);
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, 1));
        StoreKit.AssertR2(medium);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 7, "seven"));
        StoreKit.AssertR2(medium);
    }

    [Fact]
    public void R2_a_released_page_is_reused_only_by_a_statement_later_than_the_one_that_released_it()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a"));
        var beforeRewrite = store.Meta.PrimeRoot.Page;
        long mark = medium.LastSeq;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "b"));   // releases the old leaf and the old root
        Assert.DoesNotContain(medium.Writes, w => w.Seq > mark && w.Page == beforeRewrite);
        Assert.Equal(LedgerState.Released, store.LedgerEntries[beforeRewrite].State);
        mark = medium.LastSeq;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "c"));   // the next statement takes the released pages first
        Assert.Contains(medium.Writes, w => w.Seq > mark && w.Page == beforeRewrite);
        StoreKit.AssertR1(medium);
    }

    [Fact]
    public void R2_a_statement_never_allocates_after_it_starts_writing_the_ledger()
    {
        var (medium, _, store) = StoreKit.NewStore();
        for (int i = 1; i <= 6; i++)
        {
            long mark = medium.LastSeq;
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i, $"r{i}", blobPages: i % 3));
            var writes = medium.Writes.Where(w => w.Seq > mark).ToList();
            Assert.Equal(MetaPage.WorkSlot, writes[^1].Page);   // page 2 last
            StoreKit.Referenced(medium, StoreKit.MetasAt(medium, medium.LastSeq).Work, medium.LastSeq, out var ledgerChain);
            int first = writes.FindIndex(w => ledgerChain.Contains(w.Page));
            Assert.True(first >= 0);
            Assert.All(writes.Skip(first).SkipLast(1), w => Assert.Contains(w.Page, ledgerChain));
        }
    }

    /// <summary>R2's GENERATION CEILING: every claim of a history based on B lies in (B, B + 4 096]; the statement whose
    /// claim would pass it promotes first — and so does every later one, on its own base.</summary>
    [Fact]
    public void R2_the_statement_whose_claim_would_pass_the_ceiling_promotes_before_it_claims()
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        ulong baseGeneration = store.Meta.CommittedGeneration;
        int promotions = 0;
        for (int i = 0; i < (int)PagedFile.GenerationCeiling + 10; i++)
        {
            ulong before = store.Meta.CommittedGeneration;
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i % 5, $"r{i}"));
            if (store.Meta.CommittedGeneration != before) promotions++;
            Assert.InRange(store.Meta.Generation, store.Meta.CommittedGeneration + 1,
                store.Meta.CommittedGeneration + PagedFile.GenerationCeiling);
        }
        Assert.Equal(1, promotions);
        Assert.True(store.Meta.CommittedGeneration > baseGeneration + PagedFile.GenerationCeiling);
        StoreKit.AssertNoStampReused(medium);
    }

    [Fact]
    public void R2_a_ledger_past_1024_pages_promotes_at_the_next_statement()
    {
        var (_, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "big", blobPages: PagedFile.LedgerCheckpoint + 10));
        Assert.True(store.LedgerEntries.Count > PagedFile.LedgerCheckpoint);
        ulong baseBefore = store.Meta.CommittedGeneration;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "next"));
        Assert.NotEqual(baseBefore, store.Meta.CommittedGeneration);
        Assert.True(store.LedgerEntries.Count < 16);
        Assert.Equal("1=big;2=next;", StoreKit.Show(store));
    }

    // ── R3: stamped pointers, and no (page, generation) pair twice ─────────────────────────────────────────────

    [Fact]
    public void R3_a_page_served_at_an_older_version_is_refused_as_torn_in_a_statement_and_rejected_at_OPEN()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, _, b) = StoreKit.OpenAs(medium, "B");
        for (int i = 0; i < 4; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(b!, 1, $"v{i}"));   // pages reused
        uint leaf = LeafOf(b!, medium);
        Assert.True(medium.Writes.Count(w => w.Page == leaf) > 1, "the test needs a reused page");
        medium.ServeOlder(leaf);
        Assert.Null(StoreKit.ReadAll(b!));                                     // '30' in a statement
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(b!, 1, "w"));
        var (_, status, c) = StoreKit.OpenAs(medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.False(c!.Adoption.WorkAdopted);                                 // the work slot rejected
        Assert.Equal("1=a;", StoreKit.Show(c));                                // the committed meta
    }

    /// <summary>Finding R2-2's scenario: a work slot rejected after a torn page and the history restarted — the rerun's
    /// first stamp is the fence's, so no (page, generation) pair is written twice.</summary>
    [Fact]
    public void R3_no_stamp_is_reused_across_a_rejection_and_a_restart()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, _, a) = StoreKit.OpenAs(medium, "A");
        for (int i = 0; i < 6; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(a!, i, $"r{i}"));
        uint torn = LeafOf(a!, medium);
        medium.PowerLoss(keep: _ => true);
        medium.Tear(torn);
        var (_, status, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.False(b!.Adoption.WorkAdopted);
        for (int i = 0; i < 8; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(b, i, $"s{i}"));
        StoreKit.AssertNoStampReused(medium);
        ulong abandonedTop = medium.Writes.Where(w => w.Unit == "A" && w.Page >= MetaPage.MetaPages).Max(w => w.Generation);
        Assert.All(medium.Writes.Where(w => w.Unit == "B" && w.Page >= MetaPage.MetaPages),
            w => Assert.True(w.Generation > abandonedTop, $"the rerun stamped {w.Generation}, not above the abandoned {abandonedTop}"));
        StoreKit.AssertR1(medium);
    }

    /// <summary>The same with the abandoned history's mint UNREADABLE: a power loss keeps every page the six statements
    /// wrote and loses every version of page 2 they wrote, so page 2 names the history's start and nothing on the
    /// medium records how far it claimed. Only the fence (F = G + 4 097, above every claim R2's ceiling allows a history
    /// based on G) keeps the rerun's stamps off the surviving pages' — R3's corollary, finding R2-2.</summary>
    [Fact]
    public void R3_no_stamp_is_reused_when_the_abandoned_historys_page_2_is_lost_and_its_pages_survive()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, _, a) = StoreKit.OpenAs(medium, "A");
        for (int i = 0; i < 6; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(a!, i, $"r{i}", blobPages: 1));
        medium.PowerLoss(keep: w => w.Page != MetaPage.WorkSlot);
        var (_, status, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal("", StoreKit.Show(b!));
        for (int i = 0; i < 8; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(b!, i, $"s{i}", blobPages: 1));
        StoreKit.AssertNoStampReused(medium);
        ulong abandonedTop = medium.Writes.Where(w => w.Unit == "A" && w.Page >= MetaPage.MetaPages).Max(w => w.Generation);
        Assert.All(medium.Writes.Where(w => w.Unit == "B" && w.Page >= MetaPage.MetaPages),
            w => Assert.True(w.Generation > abandonedTop, $"the rerun stamped {w.Generation}, not above the abandoned {abandonedTop}"));
        Assert.Equal("0=s0;1=s1;2=s2;3=s3;4=s4;5=s5;6=s6;7=s7;", StoreKit.Show(b!));
    }

    /// <summary>Finding R3-4's scenario: A's WRITE claims g + 1, writes the lowest FREE page and dies before page 2; B's
    /// WRITE finds the mint at g + 1 and claims above it; the medium keeps A's page and B's page 2 and loses B's page
    /// — and the OPEN rejects the work slot: the committed meta, never a torn state.</summary>
    [Theory]
    [InlineData(nameof(Fault.Death))]
    [InlineData(nameof(Fault.Refuse))]
    public void R3_no_stamp_is_reused_across_one_run_units_dead_or_refused_statement_and_anothers_next(string faultName)
    {
        var fault = Enum.Parse<Fault>(faultName);
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "base"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (ha, _, a) = StoreKit.OpenAs(medium, "A");
        var (_, _, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, b!.Enter());
        ha.FaultAtWrite(3, fault);   // the claim, then the first data page, then the fault
        if (fault == Fault.Death) Assert.Throws<SimulatedDeath>(() => StoreKit.Put(a!, 2, "dead"));
        else Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(a!, 2, "refused"));
        ulong aClaim = MetaPage.Decode(medium.Snapshot(MetaPage.WorkSlot), MetaPage.WorkSlot)!.StampMint;
        long mark = medium.LastSeq;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(b, 3, "live"));
        Assert.True(b.Meta.Generation > aClaim, "B claims above A's mint");
        StoreKit.AssertNoStampReused(medium);
        var aPages = medium.Writes.Where(w => w.Unit == "A" && w.Generation == aClaim && w.Page >= 3).Select(w => w.Page).ToHashSet();
        Assert.NotEmpty(aPages);
        Assert.Contains(medium.Writes, w => w.Unit == "B" && w.Seq > mark && aPages.Contains(w.Page));   // B reused A's page
        // Keep A's dead pages and every write of B's but its writes over them: B's page 2 then names B's stamp on a page
        // the medium holds at A's, and R3 alone rejects the work slot.
        medium.PowerLoss(keep: w => w.Unit == "A" || (w.Unit == "B" && !aPages.Contains(w.Page)));
        var (_, status, c) = StoreKit.OpenAs(medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.False(c!.Adoption.WorkAdopted);
        Assert.Equal("1=base;", StoreKit.Show(c));
    }

    /// <summary>Finding R4-3: a NO OTHER store's statement refused at its n-th write and then its CLOSE — the retirement's
    /// stamps lie above the refused claim; the power-loss variant that keeps the retirement's page 2 and loses its write
    /// of a page the refused statement had stamped rejects the work slot at the next OPEN.</summary>
    [Fact]
    public void R3_a_NO_OTHER_store_refused_mid_statement_then_closed_stamps_its_retirement_above_the_refused_claim()
    {
        var (medium, handle, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "base"));
        handle.FaultAtWrite(2, Fault.Refuse);
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(store, 2, "refused"));
        ulong refusedClaim = store.LastClaim;
        var refusedPages = medium.Writes.Where(w => w.Generation == refusedClaim && w.Page >= 3).Select(w => w.Page).ToHashSet();
        Assert.NotEmpty(refusedPages);
        long mark = medium.LastSeq;
        Assert.Equal(StoreStatus.Ok, store.Close());
        Assert.All(medium.Writes.Where(w => w.Seq > mark && w.Page >= 3), w => Assert.True(w.Generation > refusedClaim));
        StoreKit.AssertNoStampReused(medium);
        Assert.Equal("1=base;", StoreKit.Show(store));

        // Rerun to the same point, then lose the CLOSE's write of a page the refused statement stamped.
        var (medium2, handle2, store2) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store2, 1, "base"));
        handle2.FaultAtWrite(2, Fault.Refuse);   // the refused leaf is grown, so the CLOSE's retirement grows into it
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(store2, 2, "refused"));
        ulong claim2 = store2.LastClaim;
        var stamped = medium2.Writes.Where(w => w.Generation == claim2 && w.Page >= 3).Select(w => w.Page).ToHashSet();
        long mark2 = medium2.LastSeq;
        handle2.FaultAtSync(3, Fault.Death);   // the CLOSE's third sync, after its retirement: the retirement unsynced
        Assert.Throws<SimulatedDeath>(() => store2.Close());
        var reused = medium2.Writes.Where(w => w.Seq > mark2 && stamped.Contains(w.Page)).Select(w => w.Seq).ToHashSet();
        Assert.NotEmpty(reused);   // the retirement re-used a page the refused statement had stamped
        Assert.All(medium2.Writes.Where(w => reused.Contains(w.Seq)), w => Assert.True(w.Generation > claim2));
        medium2.PowerLoss(keep: w => !reused.Contains(w.Seq));
        var (_, status, reopened) = StoreKit.OpenAs(medium2, "B");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal("1=base;", StoreKit.Show(reopened!));
        StoreKit.AssertNoStampReused(medium2);
    }

    [Fact]
    public void R3_a_refused_statement_and_the_same_run_units_next_statement_through_a_second_connector_never_share_a_stamp()
    {
        var (medium, handle, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a"));
        handle.FaultAtWrite(2, Fault.Refuse);
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(store, 2, "refused"));
        ulong refused = store.LastClaim;
        // A second connector of the same run unit attaches to the same store (§2.2.1.7, "Within a run unit").
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 3, "c"));
        Assert.True(store.Meta.Generation > refused);
        StoreKit.AssertNoStampReused(medium);
        Assert.Equal("1=a;3=c;", StoreKit.Show(store));
    }

    [Fact]
    public void R3_a_changing_statement_under_the_mutex_writes_page_2_twice_the_claim_first_and_the_completion_last()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a"));
        var before = store.Meta;
        long mark = medium.LastSeq;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "b"));
        var writes = medium.Writes.Where(w => w.Seq > mark).ToList();
        Assert.Equal(MetaPage.WorkSlot, writes[0].Page);
        Assert.Equal(MetaPage.WorkSlot, writes[^1].Page);
        Assert.Equal(2, writes.Count(w => w.Page == MetaPage.WorkSlot));
        var claim = MetaPage.Decode(medium.SnapshotAt(MetaPage.WorkSlot, writes[0].Seq), MetaPage.WorkSlot)!;
        Assert.Equal(before.Generation, claim.Generation);           // roots, base and generation unchanged
        Assert.Equal(before.PrimeRoot, claim.PrimeRoot);
        Assert.Equal(before.CommittedGeneration, claim.CommittedGeneration);
        Assert.Equal(store.Meta.Generation, claim.StampMint);        // the claim names the statement's generation
        Assert.All(writes.Skip(1).SkipLast(1), w => Assert.Equal(claim.StampMint, w.Generation));
    }

    [Fact]
    public void R3_no_mint_on_page_2_ever_decreases_across_deaths_refusals_promotions_and_fences()
    {
        var (medium, handle, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a"));
        Assert.Equal(StoreStatus.Ok, store.Begin(1, out var st, ordinals: 3, transactionIds: 1));
        st!.Abandon();
        handle.FaultAtWrite(2, Fault.Refuse);
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(store, 2, "b"));
        Assert.Equal(StoreStatus.Ok, store.Promote());
        var (hb, _, b) = StoreKit.OpenAs(medium, "B");
        hb.FaultAtWrite(2, Fault.Death);
        Assert.Throws<SimulatedDeath>(() => StoreKit.Put(b!, 3, "c"));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 4, "d"));
        var (_, _, c) = StoreKit.OpenAs(medium, "C");
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(c!, 5, "e"));
        StoreKit.AssertMintsNeverDecrease(medium);
        StoreKit.AssertNoStampReused(medium);
    }

    // ── R4: a freed page waits for the promotion ───────────────────────────────────────────────────────────────

    /// <summary>Finding R3-2: after a CLOSE (the next OPEN's fence included) or a checkpoint, and ten WRITEs, no write
    /// targets a page the committed meta's ledger root reaches: the promoted ledger's own pages are freed, not free.</summary>
    [Fact]
    public void R4_after_a_CLOSE_and_ten_WRITEs_no_write_targets_a_page_the_committed_ledger_reaches()
    {
        foreach (bool reopen in new[] { true, false })
        {
            var (medium, _, store) = StoreKit.NewStore();
            for (int i = 0; i < 5; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i, $"r{i}"));
            var writer = store;
            if (reopen)
            {
                Assert.Equal(StoreStatus.Ok, store.Close());
                writer = StoreKit.OpenAs(medium, "B").Store!;
            }
            else
                Assert.Equal(StoreStatus.Ok, store.Promote());
            var (a, b, _) = StoreKit.MetasAt(medium, medium.LastSeq);
            var committed = MetaPage.Committed(a, b)!.Value.Meta;
            StoreKit.Referenced(medium, committed, medium.LastSeq, out var committedLedger);
            Assert.NotEmpty(committedLedger);
            Assert.All(committedLedger, p => Assert.Equal(LedgerState.Freed, writer.LedgerEntries[p].State));
            long mark = medium.LastSeq;
            for (int i = 0; i < 10; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(writer, 10 + i, $"s{i}"));
            Assert.DoesNotContain(medium.Writes, w => w.Seq > mark && committedLedger.Contains(w.Page));
            StoreKit.AssertR1(medium, mark);
        }
    }

    // ── R5: promotion ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void R5_every_page_a_committed_slot_names_was_synced_before_the_slot_was_written()
    {
        var (medium, _, store) = StoreKit.NewStore();
        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < 4; i++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, i, $"r{round}{i}", blobPages: 1));
            Assert.Equal(StoreStatus.Ok, store.Promote());
        }
        foreach (var slotWrite in medium.Writes.Where(w => w.Page < MetaPage.WorkSlot && w.Generation > 0))
        {
            long lastSync = medium.Syncs.Where(s => s.UpToSeq < slotWrite.Seq).Max(s => s.UpToSeq);
            var slot = MetaPage.Decode(medium.SnapshotAt(slotWrite.Page, slotWrite.Seq), slotWrite.Page)!;
            foreach (var page in StoreKit.Referenced(medium, slot, slotWrite.Seq, out _))
            {
                var lastWrite = medium.Writes.Last(w => w.Page == page && w.Seq < slotWrite.Seq);
                Assert.True(lastWrite.Seq <= lastSync, $"page {page} of slot {slotWrite.Page} was written after the last sync");
            }
        }
    }

    [Fact]
    public void R5_two_consecutive_promotions_write_different_slots_and_each_is_followed_by_its_retirement()
    {
        var (medium, _, store) = StoreKit.NewStore();
        for (int round = 0; round < 4; round++)
        {
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, round, "x"));
            Assert.Equal(StoreStatus.Ok, store.Promote());
        }
        var slotWrites = medium.Writes.Where(w => w.Page < MetaPage.WorkSlot && w.Generation > 0).ToList();
        Assert.Equal(4, slotWrites.Count);
        for (int i = 1; i < slotWrites.Count; i++) Assert.NotEqual(slotWrites[i - 1].Page, slotWrites[i].Page);
        foreach (var slotWrite in slotWrites)
        {
            var nextPage2 = medium.Writes.First(w => w.Seq > slotWrite.Seq && w.Page == MetaPage.WorkSlot);
            var retired = MetaPage.Decode(medium.SnapshotAt(MetaPage.WorkSlot, nextPage2.Seq), MetaPage.WorkSlot)!;
            Assert.Equal(slotWrite.Generation, retired.CommittedGeneration);
            Assert.False(retired.Promoting);
        }
    }

    /// <summary>Two run units promoting in turn: the second reads both slots before it chooses, so it never overwrites the
    /// first's committed meta, and its retirement runs on its own base.</summary>
    [Fact]
    public void R5_two_stores_promoting_in_turn_never_overwrite_each_others_committed_meta()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, _, a) = StoreKit.OpenAs(medium, "A");
        var (_, _, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(a!, 1, "a"));
        Assert.Equal(StoreStatus.Ok, a!.Promote());
        var afterA = MetaPage.Committed(StoreKit.MetasAt(medium, medium.LastSeq).A, StoreKit.MetasAt(medium, medium.LastSeq).B)!.Value;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(b!, 2, "b"));
        Assert.Equal(StoreStatus.Ok, b!.Promote());
        var (sa, sb, work) = StoreKit.MetasAt(medium, medium.LastSeq);
        var afterB = MetaPage.Committed(sa, sb)!.Value;
        Assert.NotEqual(afterA.Slot, afterB.Slot);
        Assert.Equal(afterA.Meta.Generation, (afterA.Slot == MetaPage.SlotA ? sa : sb)!.Generation);
        Assert.Equal(afterB.Meta.Generation, work!.CommittedGeneration);
        Assert.Equal(StoreStatus.Ok, a.Enter());
        Assert.Equal("1=a;2=b;", StoreKit.Show(a));
    }

    // ── A statement is atomic ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>A medium that refuses the n-th write of a statement — for every n the statement has — leaves the file at
    /// the previous statement and the statement at '30' (§9.1.13.6 item 1); the same run unit's next statement then
    /// completes.</summary>
    [Fact]
    public void A_statement_refused_at_any_write_leaves_the_file_at_the_previous_statement()
    {
        int writes = DryRunWrites(coherent: true);
        for (int n = 1; n <= writes; n++)
        {
            var (medium, handle, store) = StoreKit.NewStore();
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a", blobPages: 1));
            handle.FaultAtWrite(n, Fault.Refuse);
            Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(store, 2, "b", blobPages: 1));
            Assert.Equal("1=a;", StoreKit.Show(store));
            var (_, status, other) = StoreKit.OpenAs(medium, "B", writable: false);
            Assert.Equal(StoreStatus.Ok, status);
            Assert.Equal("1=a;", StoreKit.Show(other!));
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 3, "c"));
            Assert.Equal("1=a;3=c;", StoreKit.Show(store));
            StoreKit.AssertNoStampReused(medium);
        }
    }

    internal static int DryRunWrites(bool coherent)
    {
        var (_, handle, store) = StoreKit.NewStore(coherent);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "a", blobPages: 1));
        int before = handle.WritesPerformed;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "b", blobPages: 1));
        return handle.WritesPerformed - before;
    }

    private static uint LeafOf(PagedFile store, MemoryPageMedium medium)
    {
        var page = new byte[PageHeader.PageSize];
        Assert.Equal(PageCheck.Whole, store.Read(store.Meta.PrimeRoot, page));
        var child = PagePointer.Read(page.AsSpan(PageHeader.Bytes + 4));
        Assert.True(medium.PageCount > child.Page);
        return child.Page;
    }
}
