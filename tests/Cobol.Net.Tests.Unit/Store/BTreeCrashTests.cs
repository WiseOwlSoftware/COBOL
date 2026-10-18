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
}
