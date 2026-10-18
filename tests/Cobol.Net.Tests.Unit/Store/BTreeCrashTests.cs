// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;
using Xunit;
using Fault = CobolNet.Tests.Unit.Store.MemoryPageMedium.Fault;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ A B+ TREE STATEMENT KILLED OR REFUSED AT EVERY ONE OF ITS WRITES LEAVES THE FILE AS THE LAST COMPLETED STATEMENT LEFT
/// IT (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.16, "A changing statement": after some data, FREE-tree or
/// ledger pages, before page 2; the refused-statement table's rows; kb/Work PB3332).
/// <para>The statement under test is one a connector will issue: appends that split the rightmost leaf, deletes that
/// merge leaves, an overflow value replacing an inline one and an inline value replacing an overflow one — its overflow
/// pages, its tree pages, the FREE-tree pages its reservation changes and its ledger pages, then page 2. It runs once
/// to count its writes, then once per write with the run unit killed before the write, killed just after it, and
/// refused at it (§9.1.13.6 item 1's '30'). Each run is continued, as 2.2.1.16 lists: by the next writer of another
/// live run unit, by a fresh OPEN, and by a power loss and then an OPEN; a refused run unit is also continued by its
/// own next statement and its CLOSE. The expected file is the design's bar: the records of the last COMPLETED
/// statement — the statement under test exactly when its page 2 was handed to the medium (killed just after its last
/// write), the previous one otherwise — and, after a power loss, the records a sync made durable.</para>
/// </summary>
public sealed class BTreeCrashTests
{
    private const int Records = 1200;

    /// <summary>The scene: a two-level tree of <see cref="Records"/> records (<see cref="Base"/>)
    /// written and CLOSEd — so durable, with a FREE tree — then opened by B, a live writer, and A, the run unit under
    /// test (or A alone, under NO OTHER).</summary>
    private static (MemoryPageMedium Medium, MemoryPageMedium.Handle HandleA, PagedFile A, PagedFile? B) NewScene(bool coherent)
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, store.Enter());
        var tree = StoreKit.Prime(store);
        foreach (var (id, text, blob) in Base()) Assert.Equal(StoreStatus.Ok, tree.Insert(StoreKit.Key(id), StoreKit.Value(text, blob), out _));
        Assert.Equal(StoreStatus.Ok, StoreKit.Commit(store, tree));
        for (int id = 10; id <= 200; id += 10) Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, id));   // released pages
        Assert.Equal(StoreStatus.Ok, store.Close());                                                       // ... in the FREE tree
        PagedFile? b = null;
        if (coherent)
        {
            var (_, sb, opened) = StoreKit.OpenAs(medium, "B");
            Assert.Equal(StoreStatus.Ok, sb);
            b = opened;
        }
        var (ha, sa, a) = StoreKit.OpenAs(medium, "A", coherent: coherent);
        Assert.Equal(StoreStatus.Ok, sa);
        Assert.True(a!.FreePages.Count > 0, "the scene's statement takes FREE pages");
        return (medium, ha, a, b);
    }

    /// <summary>The scene's records: every 40th, and the twenty the scene deletes, with two-page overflow values (so the
    /// deletions release enough pages to leave a FREE tree after the retirements' own reservations).</summary>
    private static IEnumerable<(int Id, string Text, int Blob)> Base() =>
        Enumerable.Range(1, Records).Select(i => (i * 10, $"r{i}", i % 40 == 0 || i <= 20 ? 2 : 0));

    private static SortedDictionary<int, string> BaseRecords()
    {
        var records = new SortedDictionary<int, string>(Base().ToDictionary(r => r.Id, r => r.Text));
        for (int id = 10; id <= 200; id += 10) records.Remove(id);
        return records;
    }

    /// <summary>The statement under test, on <paramref name="a"/>: three hundred appends past the last key (the rightmost
    /// leaf splits), every record from 210 to 6 200 deleted (leaves merge), record 10 000 given a two-page overflow value
    /// and record 12 000 (an overflow record) an inline one.</summary>
    private static StoreStatus Statement(PagedFile a)
    {
        if (a.Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        var tree = StoreKit.Prime(a);
        foreach (var (id, text, blob) in Changes())
        {
            var status = text is null ? tree.Delete(StoreKit.Key(id), out _) : tree.Put(StoreKit.Key(id), StoreKit.Value(text, blob));
            if (status != StoreStatus.Ok) return status;
        }
        return StoreKit.Commit(a, tree);
    }

    private static IEnumerable<(int Id, string? Text, int Blob)> Changes() =>
        Enumerable.Range(1, 300).Select(i => (Records * 10 + i, (string?)$"n{i}", 0))
            .Concat(Enumerable.Range(21, 600).Select(i => (i * 10, (string?)null, 0)))
            .Append((10000, "big", 2))
            .Append((12000, "small", 0));

    private static SortedDictionary<int, string> AfterRecords()
    {
        var records = BaseRecords();
        foreach (var (id, text, _) in Changes())
            if (text is null) records.Remove(id);
            else records[id] = text;
        return records;
    }

    private static string Show(SortedDictionary<int, string> records) => string.Concat(records.Select(kv => $"{kv.Key}={kv.Value};"));

    private static int CountWrites(bool coherent)
    {
        var (_, handle, a, _) = NewScene(coherent);
        int before = handle.WritesPerformed;
        Assert.Equal(StoreStatus.Ok, Statement(a));
        return handle.WritesPerformed - before;
    }

    [Theory]
    [InlineData(true, nameof(Fault.Death))]
    [InlineData(true, nameof(Fault.DeathAfter))]
    [InlineData(true, nameof(Fault.Refuse))]
    [InlineData(false, nameof(Fault.Death))]
    [InlineData(false, nameof(Fault.DeathAfter))]
    [InlineData(false, nameof(Fault.Refuse))]
    public void A_tree_statement_faulted_at_every_write_leaves_the_last_completed_statement(bool coherent, string faultName)
    {
        var fault = Enum.Parse<Fault>(faultName);
        int writes = CountWrites(coherent);
        Assert.True(writes >= 9, $"the statement writes {writes} pages");
        string before = Show(BaseRecords()), after = Show(AfterRecords());
        for (int n = 1; n <= writes; n++)
        {
            bool completed = fault == Fault.DeathAfter && n == writes;
            string expected = completed ? after : before;
            foreach (var continuation in new[] { "next", "open", "power" })
            {
                var (medium, handle, a, b) = NewScene(coherent);
                long mark = medium.LastSeq;
                handle.FaultAtWrite(n, fault);
                if (fault == Fault.Refuse) Assert.Equal(StoreStatus.PermanentError, Statement(a));
                else Assert.Throws<SimulatedDeath>(() => Statement(a));
                string at = $"{(coherent ? "coherent" : "NO OTHER")} {fault} at write {n} of {writes}, then {continuation}";
                if (continuation == "next")
                {
                    if (b is not null)
                    {
                        // Another live run unit's next statement claims above the dead or refused claim (finding R3-4).
                        Assert.Equal(StoreStatus.Ok, StoreKit.Put(b, 6, "six"));
                        Assert.True(StoreKit.Show(b) == "6=six;" + expected, $"{at}: {StoreKit.Show(b)}");
                        expected = "6=six;" + expected;
                    }
                    if (fault == Fault.Refuse)
                    {
                        // The refused run unit lives on: its own next statement and its CLOSE (finding R4-3).
                        handle.Heal();
                        Assert.Equal(StoreStatus.Ok, StoreKit.Put(a, 7, "seven"));
                        Assert.True(StoreKit.Show(a) == expected.Insert(expected.StartsWith("6=") ? 6 : 0, "7=seven;"), at);
                        Assert.Equal(StoreStatus.Ok, a.Close());
                        expected = expected.Insert(expected.StartsWith("6=") ? 6 : 0, "7=seven;");
                    }
                }
                else if (continuation == "power")
                {
                    medium.PowerLoss();
                    expected = before;   // nothing since the scene's CLOSE was synced
                }
                var (_, status, opened) = StoreKit.OpenAs(medium, "C");
                Assert.True(status == StoreStatus.Ok, at);
                Assert.True(StoreKit.Show(opened!) == expected, $"{at}: {StoreKit.Show(opened!)}");
                Assert.Equal(StoreStatus.Ok, StoreKit.Put(opened!, 8, "eight"));
                StoreKit.AssertR1(medium, mark);
                StoreKit.AssertR2(medium);
                StoreKit.AssertNoStampReused(medium);
                StoreKit.AssertMintsNeverDecrease(medium);
                expected = completed ? after : before;
            }
        }
    }

    // ── Refused READS: '30', never a verdict on the page (branch review R1-3, PB3331's R1-2 on the tree) ──────────────

    private static int CountReads(bool coherent)
    {
        var (_, handle, a, b) = NewScene(coherent);
        if (b is not null) Assert.Equal(StoreStatus.Ok, StoreKit.Put(b, 5, "five"));
        int before = handle.ReadsPerformed;
        Assert.Equal(StoreStatus.Ok, Statement(a));
        return handle.ReadsPerformed - before;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_tree_statement_refused_at_every_one_of_its_reads_answers_30_writes_nothing_and_the_retry_completes(bool coherent)
    {
        // The statement's reads: page 2 at the coherent entry and, after B's statement, the RESIDENT ledger it adopts; the
        // tree pages each change's path fetches; the overflow chain the inline value replaces (to supersede its pages);
        // the FREE-tree pages the reservation's batch walks. Every one refused answers '30' (§9.1.13.6 item 1) BEFORE
        // anything is claimed or written (the reservation precedes the claim), and the run unit's next statement completes.
        int reads = CountReads(coherent);
        Assert.True(reads >= 10, $"the statement reads {reads} pages");
        var after = AfterRecords();
        if (coherent) after[5] = "five";
        for (int n = 1; n <= reads; n++)
        {
            var (medium, handle, a, b) = NewScene(coherent);
            if (b is not null) Assert.Equal(StoreStatus.Ok, StoreKit.Put(b, 5, "five"));
            long mark = medium.LastSeq;
            string at = $"{(coherent ? "coherent" : "NO OTHER")} refused at read {n} of {reads}";
            handle.FaultAtRead(n, Fault.Refuse);
            Assert.True(Statement(a) == StoreStatus.PermanentError, at);
            Assert.True(medium.LastSeq == mark, $"{at}: a refused read wrote {medium.LastSeq - mark} pages");
            handle.Heal();
            Assert.Equal(StoreStatus.Ok, Statement(a));
            Assert.True(StoreKit.Show(a) == Show(after), $"{at}: {StoreKit.Show(a)}");
            var (_, status, opened) = StoreKit.OpenAs(medium, "C");
            Assert.True(status == StoreStatus.Ok, at);
            Assert.True(StoreKit.Show(opened!) == Show(after), at);
            StoreKit.AssertR1(medium, mark);
            StoreKit.AssertR2(medium);
            StoreKit.AssertNoStampReused(medium);
        }
    }

    [Fact]
    public void A_scan_refused_at_every_one_of_its_reads_answers_30_and_the_next_scan_reads_every_record()
    {
        // READ NEXT's walk (a cursor's leaf crossings and the overflow values it reads) refused at each read: '30', the
        // store unchanged and readable at the next scan.
        // Every scan runs COLD: the store's page cache (2.2.1.13) holds the pages this store wrote, and a read the cache
        // serves never reaches the medium to be refused.
        var (medium, handle, a, _) = NewScene(coherent: false);
        string before = Show(BaseRecords());
        a.DropCache();   // a COLD scan (the page cache, 2.2.1.13, would serve every page the scene wrote)
        int start = handle.ReadsPerformed;
        Assert.Equal(before, StoreKit.Show(a));
        int reads = handle.ReadsPerformed - start;
        Assert.True(reads > 20, $"the scan reads {reads} pages");
        long mark = medium.LastSeq;
        for (int n = 1; n <= reads; n++)
        {
            a.DropCache();
            handle.FaultAtRead(n, Fault.Refuse);
            Assert.True(StoreKit.ReadAll(a) is null, $"refused at read {n} of {reads}");
            handle.Heal();
            Assert.Equal(before, StoreKit.Show(a));
        }
        Assert.Equal(mark, medium.LastSeq);
    }

    /// <summary>A store that holds a durable base and then a DESCENDANT work slot — three statements after the last CLOSE,
    /// one with a two-page overflow value — whose run unit died without a CLOSE: the next OPEN reads the descendant's
    /// resident ledger and verifies each of its <c>fresh</c> pages (2.2.1.6 R6 step 3).</summary>
    private static (MemoryPageMedium Medium, string Expected) DescendantScene()
    {
        var medium = StoreKit.ClosedStore("1=one", "2=two", "3=three");
        var (_, status, a) = StoreKit.OpenAs(medium, "A");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(a!, 4, "four", blobPages: 2));
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(a!, 2));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(a!, 5, "five"));
        return (medium, "1=one;3=three;4=four;5=five;");   // A dies here: no CLOSE
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_open_refused_at_every_one_of_its_reads_answers_30_and_never_rejects_the_descendant(bool writable)
    {
        // R6 step 3 rejects a descendant whose pages fail to VERIFY; a REFUSED read is no verdict on a page. So an OPEN
        // refused at any read answers '30' (§9.1.13.6 item 1), and the next OPEN still adopts the descendant: refused
        // before the fence wrote anything, the work slot itself (DESCENDANT, adopted); after, the fence's promotion of it.
        var (clean, expected) = DescendantScene();
        var counter = clean.Open("C");
        Assert.Equal(StoreStatus.Ok, PagedFile.Open(counter, writable, coherent: true, out var counted));
        Assert.True(counted!.Adoption is { WorkSlot: WorkSlotClass.Descendant, WorkAdopted: true });
        int reads = counter.ReadsPerformed, beforeAnyWrite = 0;
        Assert.True(reads > 3, $"the OPEN reads {reads} pages");
        for (int n = 1; n <= reads; n++)
        {
            var (medium, _) = DescendantScene();
            string at = $"{(writable ? "I-O" : "INPUT")} OPEN refused at read {n} of {reads}";
            var handle = medium.Open("C");
            handle.FaultAtRead(n, Fault.Refuse);
            Assert.True(PagedFile.Open(handle, writable, coherent: true, out _) == StoreStatus.PermanentError, at);
            var (_, status, next) = StoreKit.OpenAs(medium, "D");
            Assert.True(status == StoreStatus.Ok, at);
            Assert.True(StoreKit.Show(next!) == expected, $"{at}: {StoreKit.Show(next!)}");
            if (handle.WritesPerformed == 0)
            {
                beforeAnyWrite++;
                Assert.True(next!.Adoption is { WorkSlot: WorkSlotClass.Descendant, WorkAdopted: true }, $"{at}: {next.Adoption}");
            }
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(next!, 6, "six"));
            StoreKit.AssertR2(medium);
            StoreKit.AssertNoStampReused(medium);
        }
        Assert.True(beforeAnyWrite >= 4, $"{beforeAnyWrite} refusals fell before the fence's first write");
    }

    [Theory]
    [InlineData(nameof(Fault.Death))]
    [InlineData(nameof(Fault.DeathAfter))]
    [InlineData(nameof(Fault.Refuse))]
    public void A_statement_dropping_a_long_overflow_chain_faulted_at_every_write_leaves_the_last_completed_statement(string faultName)
    {
        // Branch review R1-1's statement: a DELETE of a committed record of 1 000 overflow pages, whose ledger takes a
        // freed entry per page, killed or refused at each of its writes.
        var fault = Enum.Parse<Fault>(faultName);
        static (MemoryPageMedium, MemoryPageMedium.Handle, PagedFile) Scene()
        {
            var (medium, _, store) = StoreKit.NewStore();
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "big", blobPages: 1000));
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "small"));
            Assert.Equal(StoreStatus.Ok, store.Close());
            var (handle, status, a) = StoreKit.OpenAs(medium, "A");
            Assert.Equal(StoreStatus.Ok, status);
            return (medium, handle, a!);
        }
        var (_, countHandle, counted) = Scene();
        int start = countHandle.WritesPerformed;
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(counted, 1));
        int writes = countHandle.WritesPerformed - start;
        Assert.True(writes >= 12, $"the DELETE writes {writes} pages (its ledger holds the chain's 1 000 freed entries)");
        for (int n = 1; n <= writes; n++)
        {
            var (medium, handle, a) = Scene();
            long mark = medium.LastSeq;
            string at = $"{fault} at write {n} of {writes}";
            handle.FaultAtWrite(n, fault);
            if (fault == Fault.Refuse) Assert.True(StoreKit.Delete(a, 1) == StoreStatus.PermanentError, at);
            else Assert.Throws<SimulatedDeath>(() => StoreKit.Delete(a, 1));
            string expected = fault == Fault.DeathAfter && n == writes ? "2=small;" : "1=big;2=small;";
            var (_, status, opened) = StoreKit.OpenAs(medium, "C");
            Assert.True(status == StoreStatus.Ok, at);
            Assert.True(StoreKit.Show(opened!) == expected, $"{at}: {StoreKit.Show(opened!)}");
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(opened!, 8, "eight"));
            StoreKit.AssertR1(medium, mark);
            StoreKit.AssertR2(medium);
            StoreKit.AssertNoStampReused(medium);
        }
    }
}