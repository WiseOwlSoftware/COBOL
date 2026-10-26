// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Store;

/// <summary>
/// ⛔ THE TREE'S SHAPE AND ITS PAGE I/O ARE THE ONES docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4 AND 2.2.1.11
/// STATE, MEASURED IN PAGES, NEVER IN TIME (kb/Work PB3332): a sequential load fills its pages and has the depth the
/// fan-out arithmetic predicts; a READ by key reads exactly the depth cold and a scan reads each page once; a statement
/// of one change writes exactly the depth (one path copy, R1) and reuses the pages the statement before it released
/// (R2); a page served at an older version under a stamped pointer is refused as torn at the read (R3); the FREE tree
/// gives its lowest pages first and the file grows only when the released pages and the FREE tree are exhausted; and
/// the trees survive a promotion. The key orders' own rules are checked here too.
/// </summary>
public sealed class BTreeShapeTests
{
    /// <summary>A relative record's frame in these tests: 8 bytes, so a leaf entry is 2 + 4 + 8 + 8 = 22 bytes.</summary>
    private const int FrameBytes = 8;

    /// <summary>Records per full leaf: ⌊4 064 / 22⌋.</summary>
    private const int LeafRecords = PageHeader.BodyBytes / (2 + 4 + 8 + FrameBytes);

    /// <summary>Children of a full internal page: ⌊(4 064 − 12) / 22⌋ keys of 8 bytes, plus the leftmost child.</summary>
    private const int RootChildren = (PageHeader.BodyBytes - PagePointer.Bytes) / (2 + 8 + PagePointer.Bytes) + 1;

    [Fact]
    public void The_fan_out_arithmetic_is_the_page_layouts()
    {
        Assert.Equal(184, LeafRecords);
        Assert.Equal(185, RootChildren);   // 184 keys; an append split leaves 184 children in the left page
    }

    [Fact]
    public void A_sequential_load_fills_its_pages_and_has_the_predicted_depth()
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        long loaded = 0;
        void LoadTo(long target)
        {
            while (loaded < target)
            {
                Assert.Equal(StoreStatus.Ok, store.Enter());
                var tree = StoreKit.Prime(store);
                for (long end = Math.Min(target, loaded + 4000); loaded < end;)
                    Assert.Equal(StoreStatus.Ok, tree.Insert(KeyOrders.RecordNumberKey((ulong)++loaded), Frame(loaded), out _));
                Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
            }
        }
        LoadTo(LeafRecords);
        Assert.Equal([1], Levels(medium, store));
        LoadTo(LeafRecords + 1);
        Assert.Equal([1, 2], Levels(medium, store));
        LoadTo(LeafRecords * RootChildren);
        Assert.Equal([1, RootChildren], Levels(medium, store));
        LoadTo(LeafRecords * RootChildren + 1);
        // The root split at the right edge: 184 children on the left, the new leaf's two on the right.
        Assert.Equal([1, 2, RootChildren + 1], Levels(medium, store));
        Assert.Equal(3, BTreeModelTests.AuditShape(medium, store.Meta.PrimeRoot, KeyOrders.Ordinal, out int entries, out _));
        Assert.Equal(LeafRecords * RootChildren + 1, entries);

        // A READ by key reads exactly the depth, cold (the page cache dropped, 2.2.1.11 witness (c)'s seam), and none
        // warm; a scan reads every page of the tree exactly once.
        foreach (long key in new long[] { 1, 2, LeafRecords, 999, 17_001, LeafRecords * RootChildren + 1 })
        {
            store.DropCache();
            long before = store.Counters.PagesRead;
            Assert.Equal(StoreStatus.Ok, StoreKit.Prime(store).TryGet(KeyOrders.RecordNumberKey((ulong)key), out var value));
            Assert.Equal(Frame(key), value);
            Assert.Equal(3, store.Counters.PagesRead - before);
            Assert.Equal(StoreStatus.Ok, StoreKit.Prime(store).TryGet(KeyOrders.RecordNumberKey((ulong)key), out _));
            Assert.Equal(3, store.Counters.PagesRead - before);   // warm: every page of the path from the cache
        }
        store.DropCache();
        long scanFrom = store.Counters.PagesRead;
        Assert.Equal(StoreStatus.Ok, StoreKit.Prime(store).First(out var cursor));
        long seen = 0;
        while (cursor.HasCurrent)
        {
            Assert.Equal((ulong)++seen, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(cursor.Key));
            Assert.Equal(StoreStatus.Ok, cursor.Next());
        }
        Assert.Equal(entries, seen);
        Assert.Equal(1 + 2 + RootChildren + 1, store.Counters.PagesRead - scanFrom);

        // Further on, every internal page but the rightmost of its level keeps the 184 children the append split left it.
        LoadTo(LeafRecords * (3 * (RootChildren - 1) + 2));
        var children = SecondLevelChildren(medium, store);
        Assert.Equal(4, children.Count);
        Assert.All(children.SkipLast(1), c => Assert.Equal(RootChildren - 1, c));
    }

    /// <summary>The child counts of the internal pages one level below the root, left to right.</summary>
    private static List<int> SecondLevelChildren(MemoryPageMedium medium, PagedFile store) =>
        StoreKit.ReadNode(medium, store.Meta.PrimeRoot)!.Children.Select(c => StoreKit.ReadNode(medium, c)!.Children.Count).ToList();

    [Fact]
    public void A_read_of_an_overflow_value_reads_its_chain_and_nothing_else()
    {
        var (_, _, store) = StoreKit.NewStore(coherent: false);
        var big = new byte[OverflowChain.PayloadBytes + 9];   // 4 065 bytes: two chain pages
        new Random(17).NextBytes(big);
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = StoreKit.Prime(store);
        Assert.Equal(StoreStatus.Ok, tree.Insert(KeyOrders.RecordNumberKey(1), big, out _));
        Assert.Equal(1 + 2, tree.PagesToWrite);
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        store.DropCache();   // cold
        long before = store.Counters.PagesRead;
        Assert.Equal(StoreStatus.Ok, StoreKit.Prime(store).TryGet(KeyOrders.RecordNumberKey(1), out var value));
        Assert.Equal(big, value);
        Assert.Equal(1 + 2, store.Counters.PagesRead - before);
    }

    [Fact]
    public void A_statement_of_one_change_writes_the_depth_and_reuses_the_pages_the_last_statement_released()
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        int checkedStatements = 0, maxLedgerWrites = 0, reusedOnly = 0;
        for (long record = 1; record <= 3000; record++)
        {
            Assert.Equal(StoreStatus.Ok, store.Enter());
            var tree = StoreKit.Prime(store);
            Assert.Equal(StoreStatus.Ok, tree.Insert(KeyOrders.RecordNumberKey((ulong)record), Frame(record), out _));
            int depthBefore = Levels(medium, store).Count, leavesBefore = depthBefore == 0 ? 0 : Levels(medium, store)[^1];
            int treePages = tree.PagesToWrite;
            ulong pageCount = store.Meta.PageCount;
            int ledgerPagesBefore = LedgerPages(medium, store).Count;
            var releasedBefore = store.LedgerEntries.Where(e => e.Value.State == LedgerState.Released).Select(e => e.Key).ToList();
            long mark = medium.LastSeq;
            Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
            var writes = medium.Writes.Where(w => w.Seq > mark).ToList();
            var levels = Levels(medium, store);
            // The first statements build the pool of released pages the later ones reuse; a split of the tree or of the
            // ledger needs a page more than the statement before released, and a promotion writes its own pages.
            if (record <= 8 || writes.Any(w => w.Page < MetaPage.WorkSlot) || levels[^1] != leavesBefore
                || depthBefore != levels.Count || LedgerPages(medium, store).Count != ledgerPagesBefore) continue;
            // No split and no promotion: one path copy, the ledger's path, page 2 — and no growth.
            var prime = StoreKit.Referenced(medium, store.Meta with { LedgerRoot = PagePointer.None, FreeRoot = PagePointer.None }, medium.LastSeq, out _);
            var ledger = LedgerPages(medium, store);
            Assert.Equal(levels.Count, treePages);
            Assert.Equal(treePages, writes.Count(w => prime.Contains(w.Page)));
            int ledgerWrites = writes.Count(w => ledger.Contains(w.Page));
            maxLedgerWrites = Math.Max(maxLedgerWrites, ledgerWrites);
            Assert.Equal(treePages + ledgerWrites + 1, writes.Count);
            Assert.Equal(MetaPage.WorkSlot, writes[^1].Page);
            // R2's reuse: the file grows only once every page the statements before released has been taken.
            if (store.Meta.PageCount != pageCount)
                Assert.All(releasedBefore, p => Assert.Contains(writes, w => w.Page == p));
            else reusedOnly++;
            checkedStatements++;
        }
        Assert.True(checkedStatements > 2500, $"{checkedStatements} statements without a split or a promotion");
        Assert.True(reusedOnly > checkedStatements * 9 / 10, $"{reusedOnly} of {checkedStatements} statements wrote only reused pages");
        Assert.InRange(maxLedgerWrites, 1, 2);   // 2.2.1.11: the ledger's path is at most 2 pages
        StoreKit.AssertR2(medium);
        StoreKit.AssertNoStampReused(medium);
    }

    [Fact]
    public void A_page_served_at_an_older_version_under_a_stamped_pointer_is_refused_as_torn_at_the_read()
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        for (long record = 1; record <= 400; record++)
        {
            Assert.Equal(StoreStatus.Ok, store.Enter());
            var t = StoreKit.Prime(store);
            Assert.Equal(StoreStatus.Ok, t.Put(KeyOrders.RecordNumberKey((ulong)(record % 250 + 1)), Frame(record)));
            Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, t));
        }
        var root = store.Meta.PrimeRoot;
        Assert.True(medium.Writes.Count(w => w.Page == root.Page) > 1, "the test needs a reused internal page");
        Assert.Equal(PageKind.Internal, StoreKit.ReadNode(medium, root)!.Kind);
        medium.ServeOlder(root.Page);
        store.DropCache();   // the store's page cache (2.2.1.13) holds the page as this store wrote it; the medium's version is what a COLD read meets
        var tree = StoreKit.Prime(store);
        Assert.Equal(StoreStatus.PermanentError, tree.TryGet(KeyOrders.RecordNumberKey(1), out _));
        Assert.Equal(StoreStatus.PermanentError, tree.First(out var cursor));
        Assert.False(cursor.HasCurrent);
        Assert.Equal(StoreStatus.PermanentError, tree.Seek(KeyOrders.RecordNumberKey(9), SeekRelation.GreaterOrEqual, out _));
        // An edit that meets the stale page changes nothing and leaves nothing pending.
        Assert.Equal(StoreStatus.PermanentError, tree.Put(KeyOrders.RecordNumberKey(7), Frame(7)));
        Assert.False(tree.HasPendingChanges);
        Assert.Null(StoreKit.ReadAll(store));
    }

    [Fact]
    public void An_overflow_page_served_old_or_torn_fails_the_read_of_its_value_and_its_deletion()
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "first", blobPages: 2));
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, 1));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "second", blobPages: 2));   // reuses the released chain pages
        var head = StoreKit.ReadNode(medium, store.Meta.PrimeRoot)!.Values[0].Head;
        Assert.True(medium.Writes.Count(w => w.Page == head.Page) > 1, "the test needs a reused overflow page");
        medium.ServeOlder(head.Page);
        store.DropCache();   // the store's page cache (2.2.1.13) holds the page as this store wrote it; the medium's version is what a COLD read meets
        Assert.Equal(StoreStatus.Ok, StoreKit.Prime(store).First(out var cursor));
        Assert.Equal(StoreStatus.PermanentError, cursor.ReadValue(out _));
        var tree = StoreKit.Prime(store);
        Assert.Equal(StoreStatus.PermanentError, tree.Delete(StoreKit.Key(2), out bool deleted));
        Assert.False(deleted);
        Assert.False(tree.HasPendingChanges);
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Delete(store, 2));
    }

    [Fact]
    public void The_free_tree_gives_its_lowest_pages_first_and_the_file_grows_only_when_it_is_exhausted()
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        for (int id = 1; id <= 60; id++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, id, $"r{id}", blobPages: 3));
        for (int id = 1; id <= 60; id += 2) Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, id));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, status, reopened) = StoreKit.OpenAs(medium, "B", coherent: false);
        Assert.Equal(StoreStatus.Ok, status);
        var b = reopened!;
        // The promotions moved the released and freed pages into the FREE tree (the ledger's own quarantined, R4).
        StoreKit.AssertR2(medium);
        Assert.True(b.FreePages.Count > 80, $"{b.FreePages.Count} free pages");
        string expected = string.Concat(Enumerable.Range(1, 60).Where(i => i % 2 == 0).Select(i => $"{i}=r{i};"));
        Assert.Equal(expected, StoreKit.Show(b));
        int statements = 0;
        for (int id = 100; ; id++)
        {
            var free = b.FreePages.ToList();
            int released = b.LedgerEntries.Count(e => e.Value.State == LedgerState.Released);
            ulong pageCount = b.Meta.PageCount;
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(b, id, $"n{id}", blobPages: 2));
            statements++;
            var after = b.FreePages.ToList();
            // What left the FREE tree is its LOWEST pages: the rest is unchanged.
            Assert.Equal(free.Skip(free.Count - after.Count), after);
            if (b.Meta.PageCount == pageCount) continue;
            // The file grew: the released pages and the FREE tree together could not serve this statement's reservation
            // (its four pages of its own and the ledger's bound, 2.2.1.6 R2).
            Assert.True(free.Count + released < 40, $"growth with {free.Count} FREE and {released} released pages");
            break;
        }
        Assert.True(statements > 10, $"the FREE tree served {statements} statements");
        StoreKit.AssertR1(medium);
        StoreKit.AssertR2(medium);
        StoreKit.AssertNoStampReused(medium);
        Assert.Equal(StoreStatus.Ok, b.Close());
        var (_, again, c) = StoreKit.OpenAs(medium, "C", writable: false);
        Assert.Equal(StoreStatus.Ok, again);
        Assert.StartsWith(expected, StoreKit.Show(c!));
    }

    [Fact]
    public void A_cursor_under_a_root_another_run_unit_replaced_re_seeks_from_its_key_after_the_adoption()
    {
        var (medium, _, store) = StoreKit.NewStore();
        for (int id = 10; id <= 2000; id += 10) Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, id, $"a{id}"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, sa, a) = StoreKit.OpenAs(medium, "A");
        var (_, sb, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, sa);
        Assert.Equal(StoreStatus.Ok, sb);
        Assert.Equal(StoreStatus.Ok, a!.Enter());
        var tree = StoreKit.Prime(a);
        Assert.Equal(StoreStatus.Ok, tree.Seek(StoreKit.Key(500), SeekRelation.Equal, out var cursor));
        Assert.True(cursor.HasCurrent);
        // B's statements change the tree around A's position and reuse the pages they release.
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(b!, 505, "b505"));
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(b!, 510));
        for (int id = 3000; id < 3040; id++) Assert.Equal(StoreStatus.Ok, StoreKit.Put(b!, id, $"b{id}"));
        // A's next statement: its coherent entry adopts page 2, the tree adopts the root, the cursor re-seeks.
        Assert.Equal(StoreStatus.Ok, a.Enter());
        tree.Adopt(a.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, cursor.Next());
        Assert.Equal(StoreKit.Key(505), cursor.Key);
        Assert.Equal(StoreStatus.Ok, cursor.Next());
        Assert.Equal(StoreKit.Key(520), cursor.Key);
        Assert.Equal(StoreStatus.Ok, cursor.Previous());
        Assert.Equal(StoreKit.Key(505), cursor.Key);
        StoreKit.AssertR1(medium);
        StoreKit.AssertNoStampReused(medium);
    }

    [Fact]
    public void A_cursor_stepped_after_an_edit_of_a_node_the_pending_edit_already_copied_re_seeks_from_its_key()
    {
        // Branch review R1-2: the second edit of one pending leaf changes it IN PLACE and leaves the root the same, so a
        // cursor that compared roots stepped through the shifted lists — the entry twice, or one skipped.
        var (_, _, store) = StoreKit.NewStore(coherent: false);
        var tree = StoreKit.Prime(store);
        foreach (int id in new[] { 1, 2, 3 }) Assert.Equal(StoreStatus.Ok, tree.Put(StoreKit.Key(id), StoreKit.Value($"v{id}", 0)));
        Assert.Equal(StoreStatus.Ok, tree.Seek(StoreKit.Key(1), SeekRelation.Equal, out var cursor));
        Assert.Equal(StoreStatus.Ok, tree.Insert(StoreKit.Key(0), StoreKit.Value("v0", 0), out _));
        Assert.Equal(StoreStatus.Ok, cursor.Next());
        Assert.Equal(StoreKit.Key(2), cursor.Key);                    // not 1 again
        Assert.Equal(StoreStatus.Ok, tree.Put(StoreKit.Key(4), StoreKit.Value("v4", 0)));
        Assert.Equal(StoreStatus.Ok, tree.Delete(StoreKit.Key(1), out _));
        Assert.Equal(StoreStatus.Ok, cursor.Next());
        Assert.Equal(StoreKit.Key(3), cursor.Key);                    // not 4: 3 is not skipped
        Assert.Equal(StoreStatus.Ok, tree.Replace(StoreKit.Key(3), StoreKit.Value("w3", 0), out _));
        Assert.Equal(StoreStatus.Ok, cursor.ReadValue(out var value));
        Assert.Equal(StoreKit.Value("w3", 0), value);                 // the current key's value as the edit left it
        Assert.Equal(StoreStatus.Ok, tree.Delete(StoreKit.Key(3), out _));
        Assert.Throws<InvalidOperationException>(() => cursor.ReadValue(out _));   // a deleted current entry: the caller's defect
        Assert.Equal(StoreStatus.Ok, cursor.Previous());
        Assert.Equal(StoreKit.Key(2), cursor.Key);
        tree.Discard();                                               // the pending edit dropped: an empty tree again
        Assert.Equal(StoreStatus.Ok, cursor.Next());
        Assert.False(cursor.HasCurrent);
    }

    [Theory]
    [InlineData(3401, true)]
    [InlineData(3402, false)]
    public void A_cursor_stepped_between_edits_and_flushes_agrees_with_a_sorted_model(int seed, bool forward)
    {
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        var tree = StoreKit.Prime(store);
        var model = new SortedDictionary<int, string>();
        for (int id = 10; id <= 12000; id += 10)
        {
            Assert.Equal(StoreStatus.Ok, tree.Put(StoreKit.Key(id), StoreKit.Value($"v{id}", 0)));
            model[id] = $"v{id}";
        }
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        Assert.True(Levels(medium, store).Count >= 2, "a tree of two levels or more");
        var random = new Random(seed);
        Assert.Equal(StoreStatus.Ok, forward ? tree.First(out var cursor) : tree.Last(out cursor));
        int steps = 0;
        while (cursor.HasCurrent)
        {
            int current = (int)KeyOrders.PageOf(cursor.Key);
            Assert.True(model.ContainsKey(current), $"step {steps}: the cursor is on {current}, which the model does not hold");
            for (int edits = random.Next(4); edits > 0; edits--)
            {
                int key = random.Next(1, 12100);
                switch (random.Next(3))
                {
                    case 0:
                        Assert.Equal(StoreStatus.Ok, tree.Put(StoreKit.Key(key), StoreKit.Value($"p{key}", 0)));
                        model[key] = $"p{key}";
                        break;
                    case 1 when model.Count > 0:
                        int victim = random.Next(5) == 0 ? current : model.Keys.ElementAt(random.Next(model.Count));
                        Assert.Equal(StoreStatus.Ok, tree.Delete(StoreKit.Key(victim), out _));
                        model.Remove(victim);
                        break;
                    default:
                        Assert.Equal(StoreStatus.Ok, tree.Replace(StoreKit.Key(current), StoreKit.Value($"r{current}", 0), out bool replaced));
                        if (replaced) model[current] = $"r{current}";
                        break;
                }
            }
            if (model.TryGetValue(current, out var text))
            {
                Assert.Equal(StoreStatus.Ok, cursor.ReadValue(out var value));
                Assert.Equal(StoreKit.Value(text, 0), value);
            }
            if (++steps % 97 == 0) Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));   // a flush between steps
            int? expected = forward ? model.Keys.Where(k => k > current).Cast<int?>().FirstOrDefault()
                : model.Keys.Where(k => k < current).Cast<int?>().LastOrDefault();
            Assert.Equal(StoreStatus.Ok, forward ? cursor.Next() : cursor.Previous());
            Assert.True(expected is null ? !cursor.HasCurrent : cursor.HasCurrent && cursor.Key.SequenceEqual(StoreKit.Key(expected.Value)),
                $"step {steps} from {current}: expected {expected?.ToString() ?? "none"}, got {(cursor.HasCurrent ? KeyOrders.PageOf(cursor.Key) : "none")}");
        }
        Assert.True(steps > 1000, $"{steps} steps");
    }

    [Theory]
    [InlineData(1000)]   // about 4 MB
    [InlineData(2069)]   // 8 MiB
    public void A_statement_that_drops_a_record_of_thousands_of_overflow_pages_reserves_the_ledger_pages_its_releases_take(int blobPages)
    {
        // Branch review R1-1: the reservation sized the ledger by the pages the statement RESERVES only, so a DELETE or a
        // shortening REWRITE of a committed record of k overflow pages — k freed ledger entries — ran out of reserved
        // pages and escaped as an InvalidOperationException. The record is legal (2.2.1.4: the host's record size).
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "big", blobPages));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "small"));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 3, "big3", blobPages));
        Assert.Equal(StoreStatus.Ok, store.Promote());          // the chains are COMMITTED: each page's release is one freed entry
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, 1));
        Assert.True(StoreKit.Pages(store.LedgerEntries, LedgerState.Freed).Length >= blobPages, "the dropped chain is freed");
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 3, "short"));   // a REWRITE to a short value: the same path
        Assert.Equal("2=small;3=short;", StoreKit.Show(store));
        // An uncommitted chain dropped by the statement after the one that wrote it: every page RELEASED.
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 4, "big4", blobPages));
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, 4));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, status, opened) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal("2=small;3=short;", StoreKit.Show(opened!));
        Assert.True(opened!.FreePages.Count >= 2 * blobPages, $"the chains' pages are in the FREE tree: {opened.FreePages.Count}");
        StoreKit.AssertR2(medium);
        StoreKit.AssertNoStampReused(medium);
    }

    [Fact]
    public void The_key_orders_compare_as_their_rules_state()
    {
        // ORDINAL: unsigned bytes, a shorter key below a longer one it begins; prefixes compare their leading positions.
        Assert.True(KeyOrders.Ordinal.Compare([0x01], [0xFF]) < 0);
        Assert.True(KeyOrders.Ordinal.Compare([0x41], [0x41, 0x00]) < 0);
        Assert.Equal(0, KeyOrders.Ordinal.ComparePrefix([0x41, 0x42, 0x43], [0x41, 0x42]));
        // NUMERIC: a relative record number big-endian, so byte order is numeric order.
        Assert.True(KeyOrders.Ordinal.Compare(KeyOrders.RecordNumberKey(255), KeyOrders.RecordNumberKey(256)) < 0);
        // WEIGHTED: the declared alphabet "ZYX…A" (§12.3.7.4 GR7) reverses the letters' order.
        var reversed = new WeightedKeyOrder(null, BTreeModelTests.ReversedAlphabet(), 2);
        Assert.True(reversed.Compare("ZZ"u8, "AA"u8) < 0);
        Assert.True(reversed.ComparePrefix("ZA"u8, "Z"u8) == 0);
        Assert.True(reversed.ComparePrefix("ZA"u8, "Y"u8) < 0);
        // WEIGHTED with a CODE-SET and no COLLATING SEQUENCE: the bytes decoded, then the native order (§12.4.5.7.4 GR4).
        var codeSet = new CodeSetConversion(Enumerable.Range(0, 256).Select(u => (char)(255 - u)).ToArray());
        var decoded = new WeightedKeyOrder(codeSet, null, 2);
        Assert.True(decoded.Compare([0x01], [0x02]) > 0);
        // COMPOSITE: the key part's order, then the release ordinal; a prefix compares the key part only.
        var composite = new CompositeKeyOrder(KeyOrders.Ordinal);
        Assert.True(composite.Compare(CompositeKeyOrder.Compose("AB"u8, 9), CompositeKeyOrder.Compose("AC"u8, 1)) < 0);
        Assert.True(composite.Compare(CompositeKeyOrder.Compose("AB"u8, 1), CompositeKeyOrder.Compose("AB"u8, 2)) < 0);
        Assert.Equal(0, composite.ComparePrefix(CompositeKeyOrder.Compose("ABC"u8, 7), "AB"u8));
        // PREFIX NARROWING: every order that compares position by position (§8.8.4.2.7 1)) narrows a prefix of every
        // length; a LOCALE-BASED sequence (§8.8.4.2.11) only a prefix as long as the key, which §14.9.41.4 GR17 c) then
        // truncates nothing of, and a composite key over one the same.
        Assert.True(KeyOrders.Ordinal.PrefixNarrows(1));
        Assert.True(reversed.PrefixNarrows(1));
        Assert.True(decoded.PrefixNarrows(1));
        Assert.True(composite.PrefixNarrows(1));
        var locale = new WeightedKeyOrder(null, new LocaleCollation("root"), 2);
        Assert.False(locale.PrefixNarrows(1));
        Assert.True(locale.PrefixNarrows(2));
        Assert.False(new CompositeKeyOrder(locale).PrefixNarrows(1));
        Assert.True(new CompositeKeyOrder(locale).PrefixNarrows(2));
        // Why: "áa" is below "ab" (primary a,a < a,b), while its one-character prefix "á" is above "ab"'s "a" (secondary).
        Assert.True(locale.Compare(Latin1("áa"), Latin1("ab")) < 0);
        Assert.True(locale.ComparePrefix(Latin1("áa"), Latin1("á")) == 0);
        Assert.True(locale.ComparePrefix(Latin1("ab"), Latin1("á")) < 0);
        // A whole-length prefix is the order itself: nothing is truncated.
        Assert.Equal(Math.Sign(locale.Compare(Latin1("ab"), Latin1("áa"))), Math.Sign(locale.ComparePrefix(Latin1("ab"), Latin1("áa"))));
        // A key or prefix longer than the key's length is the caller's defect (GR14 bounds a START's length by the key's),
        // refused for every weighted order, positional or not.
        Assert.Throws<ArgumentException>(() => locale.Compare(Latin1("abc"), Latin1("ab")));
        Assert.Throws<ArgumentException>(() => locale.ComparePrefix(Latin1("ab"), Latin1("abc")));
        Assert.Throws<ArgumentException>(() => reversed.Compare("ZZ"u8, "ZZZ"u8));
    }

    /// <summary>⛔ A START'S PARTIAL KEY UNDER A LOCALE-BASED SEQUENCE IS THE SEQUENTIAL SEARCH §14.9.41.4 GR17 STATES
    /// (review w1122-a R1-1): <i>"the file is searched sequentially"</i> for EQUAL and the GREATER relations, <i>"in reverse
    /// order"</i> for the LESS ones, each stored key truncated to the partial key's length. Under the root locale the keys
    /// order "áa" &lt; "ab" &lt; "ác" &lt; "ad" while their one-character prefixes are á, a, á, a, so the qualifying entries
    /// are not one run of the key order and a binary search answered "ác" for NOT LESS THAN "á" (GR17 c) takes "áa") and
    /// "ab" for LESS THAN "á" (GR17 d) takes "ad").</summary>
    [Fact]
    public void A_START_prefix_under_a_locale_sequence_is_the_sequential_search_of_GR17()
    {
        var (_, _, store) = StoreKit.NewStore();
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), 2);
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var key in new[] { "ad", "áa", "ác", "ab" })
        {
            Assert.Equal(StoreStatus.Ok, tree.Insert(Latin1(key), Latin1(key), out bool inserted));
            Assert.True(inserted);
        }
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        var read = new BTree(store, order, store.Meta.PrimeRoot);
        Assert.Equal(StoreStatus.Ok, read.First(out var walk));
        var inOrder = new List<string>();
        for (; walk.HasCurrent; Assert.Equal(StoreStatus.Ok, walk.Next())) inOrder.Add(Encoding.Latin1.GetString(walk.Key));
        Assert.Equal(new[] { "áa", "ab", "ác", "ad" }, inOrder);

        string? Start(string prefix, SeekRelation relation)
        {
            Assert.Equal(StoreStatus.Ok, read.Seek(Latin1(prefix), relation, out var cursor, prefix: true));
            return cursor.HasCurrent ? Encoding.Latin1.GetString(cursor.Key) : null;
        }
        Assert.Equal("áa", Start("á", SeekRelation.GreaterOrEqual));
        Assert.Equal("áa", Start("á", SeekRelation.Equal));
        Assert.Null(Start("á", SeekRelation.GreaterThan));          // no prefix is above "á": the invalid key condition
        Assert.Equal("ad", Start("á", SeekRelation.LessThan));
        Assert.Equal("ad", Start("á", SeekRelation.LessOrEqual));
        Assert.Equal("ab", Start("a", SeekRelation.Equal));
        Assert.Equal("áa", Start("a", SeekRelation.GreaterThan));
        Assert.Equal("ad", Start("a", SeekRelation.LessOrEqual));
        Assert.Null(Start("a", SeekRelation.LessThan));
        // The cursor a search leaves steps on in key order, like any other (READ NEXT after the START).
        Assert.Equal(StoreStatus.Ok, read.Seek(Latin1("á"), SeekRelation.GreaterOrEqual, out var after, prefix: true));
        Assert.Equal(StoreStatus.Ok, after.Next());
        Assert.Equal("ab", Encoding.Latin1.GetString(after.Key));
        // A key longer than the record key is refused at the edit that would store it, before it can reach a START.
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var edit = new BTree(store, order, store.Meta.PrimeRoot);
        Assert.Throws<ArgumentException>(() => edit.Insert(Latin1("abc"), Latin1("abc"), out _));
    }

    /// <summary>⛔ A WHOLE-LENGTH START KEY UNDER A LOCALE-BASED SEQUENCE IS A BINARY SEARCH, NOT A WALK OF THE FILE (review
    /// w1122-a R2-1; kb/Work PB3330's bar: no statement reads the whole file). When the START's key is as long as the
    /// record key (§14.9.41.4 GR17 b)), GR17 c) and d) truncate nothing, so the comparison is the key order's own, the
    /// qualifying entries are one run of it, and the seek reads the tree's depth, a leaf or two more where it settles across
    /// a leaf boundary — never the leaves before its answer. Every relation is checked against the sequential search's
    /// answer over the sorted keys, at present keys, absent keys, below the first and above the last (the invalid key
    /// condition of GR17 e) 2. for GREATER THAN).</summary>
    [Fact]
    public void A_whole_length_START_key_under_a_locale_sequence_reads_the_depth_and_not_the_file()
    {
        const int keyLength = 6;
        const string letters = "aáàAbBcç";
        var (medium, _, store) = StoreKit.NewStore(coherent: false);
        var order = new WeightedKeyOrder(null, new LocaleCollation("root"), keyLength);
        var random = new Random(3306);
        string NewKey() => string.Create(keyLength, random, static (span, r) =>
        {
            for (int i = 0; i < span.Length; i++) span[i] = letters[r.Next(letters.Length)];
        });
        var sorted = new SortedSet<byte[]>(Comparer<byte[]>.Create((a, b) => order.Compare(a, b)));
        while (sorted.Count < 3000) sorted.Add(Latin1(NewKey()));
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = new BTree(store, order, store.Meta.PrimeRoot);
        foreach (var key in sorted) Assert.Equal(StoreStatus.Ok, tree.Insert(key, key, out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        var levels = Levels(medium, store);
        int depth = levels.Count;
        Assert.Equal(2, depth);
        Assert.True(levels[^1] > 8, $"{levels[^1]} leaves");
        var keys = sorted.ToList();

        var probes = new List<byte[]> { Latin1("000000"), Latin1("zzzzzz"), keys[0], keys[^1] };
        for (int i = 0; i < 12; i++) probes.Add(keys[random.Next(keys.Count)]);
        for (int i = 0; i < 12; i++) probes.Add(Latin1(NewKey()));
        int widest = 0;
        foreach (var probe in probes)
        {
            Assert.True(order.PrefixNarrows(probe.Length));
            foreach (var relation in Enum.GetValues<SeekRelation>())
            {
                int expected = relation switch
                {
                    SeekRelation.Equal => keys.FindIndex(k => order.ComparePrefix(k, probe) == 0),
                    SeekRelation.GreaterOrEqual => keys.FindIndex(k => order.ComparePrefix(k, probe) >= 0),
                    SeekRelation.GreaterThan => keys.FindIndex(k => order.ComparePrefix(k, probe) > 0),
                    SeekRelation.LessOrEqual => keys.FindLastIndex(k => order.ComparePrefix(k, probe) <= 0),
                    _ => keys.FindLastIndex(k => order.ComparePrefix(k, probe) < 0),
                };
                store.DropCache();   // cold (2.2.1.11 witness (c)'s seam): the seek's own reads, not the page cache's
                long before = store.Counters.PagesRead;
                Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).Seek(probe, relation, out var cursor, prefix: true));
                int read = (int)(store.Counters.PagesRead - before);
                widest = Math.Max(widest, read);
                Assert.Equal(expected >= 0, cursor.HasCurrent);
                if (expected >= 0) Assert.Equal(keys[expected], cursor.Key);
                Assert.InRange(read, depth, depth + 2);   // a LESS relation settles forward, then back across one boundary
            }
        }
        Assert.True(widest < levels[^1], $"a seek read {widest} pages of a tree of {levels[^1]} leaves");
        // The answer above the last key is the invalid key condition, read in the depth (the reviewer's scenario), cold.
        store.DropCache();
        long last = store.Counters.PagesRead;
        Assert.Equal(StoreStatus.Ok, new BTree(store, order, store.Meta.PrimeRoot).Seek(Latin1("zzzzzz"), SeekRelation.GreaterThan, out var none, prefix: true));
        Assert.False(none.HasCurrent);
        Assert.Equal(depth, store.Counters.PagesRead - last);
    }

    private static byte[] Latin1(string text) => Encoding.Latin1.GetBytes(text);

    [Fact]
    public void A_key_past_the_limit_is_the_callers_defect_and_a_long_key_with_a_long_value_overflows()
    {
        var (_, _, store) = StoreKit.NewStore(coherent: false);
        var tree = StoreKit.Prime(store);
        Assert.Throws<ArgumentException>(() => tree.Put(new byte[BTree.MaxKeyBytes + 1], []));
        // A 1 016-byte key with a 1 016-byte value: inline by the value's limit, but the entry would pass half a page.
        Assert.Equal(StoreStatus.Ok, tree.Put(new byte[BTree.MaxKeyBytes], new byte[BTree.MaxInlineValueBytes]));
        Assert.Equal(1 + 1, tree.PagesToWrite);   // the leaf and one overflow page
        Assert.Equal(StoreStatus.Ok, tree.Put([0x01], new byte[BTree.MaxInlineValueBytes]));
        Assert.Equal(1 + 1, tree.PagesToWrite);   // a short key's 1 016-byte value is inline
    }

    /// <summary>The frame of relative record <paramref name="record"/> in these tests.</summary>
    private static byte[] Frame(long record) => KeyOrders.RecordNumberKey((ulong)record * 7);

    /// <summary>The pages of the ledger tree the store's meta names, read from the raw medium.</summary>
    private static HashSet<uint> LedgerPages(MemoryPageMedium medium, PagedFile store)
    {
        StoreKit.Referenced(medium, store.Meta with { PrimeRoot = PagePointer.None, FreeRoot = PagePointer.None }, medium.LastSeq, out var ledger);
        return ledger;
    }

    /// <summary>The prime tree's node count per level, root first, read from the raw medium.</summary>
    private static List<int> Levels(MemoryPageMedium medium, PagedFile store)
    {
        var levels = new List<int>();
        var level = new List<PagePointer>();
        if (!store.Meta.PrimeRoot.IsNone) level.Add(store.Meta.PrimeRoot);
        while (level.Count > 0)
        {
            levels.Add(level.Count);
            var next = new List<PagePointer>();
            foreach (var p in level)
            {
                var node = StoreKit.ReadNode(medium, p)!;
                if (node.Kind == PageKind.Internal) next.AddRange(node.Children);
            }
            level = next;
        }
        return levels;
    }
}
