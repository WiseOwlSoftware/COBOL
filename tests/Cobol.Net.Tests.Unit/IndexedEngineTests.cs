// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using CobolNet.Tests.Unit.Store;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE INDEXED ORGANIZATION ON THE KEYED STORE ENGINE, THROUGH ITS CONNECTOR (docs/rearchitecture/DESIGN-runtime-library.md
/// section 2.2.1; kb/Work PB3334). A record is n + 1 entries — the prime tree's and one per alternate key — and every change
/// to them is ONE statement of the page layer, so the rows of 2.2.1.16 and 2.2.1.17 that PB3333 ran through the relative
/// connector run here with two trees changing at once, the fault injected under the connector's own handle
/// (<see cref="KeyedStore.MediumObserver"/>):
/// <list type="bullet">
///   <item>2.2.1.16, "A changing statement (coherent)" and "(NO OTHER)", and "A promotion (CLOSE)": a process death after
///   EVERY write of a WRITE, a REWRITE (which moves an alternate entry), a DELETE and a CLOSE. The bar: the next OPEN reads
///   the file as the last COMPLETED statement left it — by the prime key AND by the alternate key, so no tree ever holds
///   half of a statement.</item>
///   <item>2.2.1.16, "Every statement REFUSED part-way": a host refusal at every write — the statement's '30' (§9.1.13.6
///   item 1), §9.1.13.1's permanent error replayed, the CLOSE ending it (DOC-A.1-105), the previous statement's file.</item>
///   <item>2.2.1.17 through the INDEXED connector (PendingStoreCases' PB3334 row): two run units' statements, §14.9.30.4
///   GR26's duplicate order kept across run units, the '02' look-ahead (GR27), the READ ONLY writer beside the INPUT reader
///   (finding R2-6), and a reader walking an alternate key's duplicates through a tree another run unit splits.</item>
///   <item>The tree's COBOL surface (PendingStoreCases' PB3334 row of 2.2.1.12): START with each relational operator, a
///   full key and a partial key, found and not found (§14.9.41.4 GR17); §14.9.35.4 GR24 a)'s untouched duplicate order of
///   the keys a REWRITE does not change; the <c>COBOLNET_KEYCHECK</c> opt-out's trees (DOC-A.1-129); the format's key
///   bounds ('39'); the whole-store formats ('39').</item>
/// </list>
/// Every expected value is the design's bar or a general rule's, never what the engine happens to do.
/// </summary>
[Trait("Host", "ReFS")]
public sealed class IndexedEngineTests
{
    private static string Tmp(string tag) => Path.Combine(Path.GetTempPath(), $"pb3334-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>An 8-character record: the prime key in positions 1-4, an alternate key WITH DUPLICATES in 5-8.</summary>
    private static void Register(FileRegistry reg, string name, string host, FileSharing? sharing,
        KeyedAccess access = KeyedAccess.Dynamic)
    {
        reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)access, primeKey: [new(0, 4)],
            varyMin: -1, varyMax: -1);
        reg.AddAlternateKey(name, [new(4, 4)], duplicates: true);
        if (sharing is { } s) reg.RegisterSharing(name, s, FileLockMode.Manual, multiple: false);
    }

    private static string Rec(int n, string alternate) => $"{n:0000}{alternate}";

    private static string Write(FileRegistry reg, string name, int n, string alternate) =>
        reg.WriteShared(name, Rec(n, alternate), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);

    private static string Rewrite(FileRegistry reg, string name, int n, string alternate) =>
        reg.RewriteShared(name, Rec(n, alternate), -1, FileRecordLock.None, FileRetryKind.None, 0);

    private static string Delete(FileRegistry reg, string name, int n) =>
        reg.DeleteShared(name, Rec(n, "    "), FileRetryKind.None, 0);

    private static string ReadKey(FileRegistry reg, string name, int keyIndex, string image, out string record) =>
        reg.ReadKeyedShared(name, keyIndex, image, FileRecordLock.None, ignoringLock: false, FileRetryKind.None, 0, out record);

    private static string ReadNext(FileRegistry reg, string name, out string image, bool previous = false) =>
        reg.ReadShared(name, previous, FileRecordLock.None, advancingOnLock: false, ignoringLock: false,
            FileRetryKind.None, 0, out image);

    private static void Seed(string host)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, sharing: null);
        seed.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", Write(seed, "S", 1, "AAAA"));
        Assert.Equal("00", Write(seed, "S", 2, "BBBB"));
        seed.Close("S");
        Assert.Equal("00", seed.Status("S"));
    }

    /// <summary>Every record of the file, read by the next run unit's OPEN INPUT: by the prime key (READ NEXT from the
    /// OPEN) and by the alternate key (START at its lowest value, then READ NEXT), each walk ending at end ('10', never
    /// '30': the file is not torn).</summary>
    private static (List<string> ByPrime, List<string> ByAlternate) Records(string host)
    {
        var reader = new FileRegistry();
        Register(reader, "R", host, sharing: null);
        reader.OpenStatic("R", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("R"));
        var byPrime = Walk();
        Assert.Equal("00", reader.StartIndexed("R", 0, ">=", Rec(0, "    "), StartKeyLength.OfWidth(4)));
        var byAlternate = Walk();
        reader.Close("R");
        return (byPrime, byAlternate);

        List<string> Walk()
        {
            var all = new List<string>();
            string status;
            while ((status = ReadNext(reader, "R", out string image)) is "00" or "02") all.Add(image);
            Assert.Equal("10", status);   // §14.9.30.4 GR24's at end
            return all;
        }
    }

    public enum Change { Write, Rewrite, Delete }

    /// <summary>Each change touches BOTH trees: the WRITE releases a duplicate of AAAA, the REWRITE moves record 2's
    /// alternate entry from BBBB to the end of AAAA's duplicates (§14.9.35.4 GR24 b)), the DELETE removes record 1's two
    /// entries.</summary>
    private static string Run(FileRegistry reg, Change change) => change switch
    {
        Change.Write => Write(reg, "A", 3, "AAAA"),
        Change.Rewrite => Rewrite(reg, "A", 2, "AAAA"),
        _ => Delete(reg, "A", 1),
    };

    private static readonly (string[] ByPrime, string[] ByAlternate) Before = (["0001AAAA", "0002BBBB"], ["0001AAAA", "0002BBBB"]);

    private static (string[] ByPrime, string[] ByAlternate) After(Change change) => change switch
    {
        Change.Write => (["0001AAAA", "0002BBBB", "0003AAAA"], ["0001AAAA", "0003AAAA", "0002BBBB"]),
        Change.Rewrite => (["0001AAAA", "0002AAAA"], ["0001AAAA", "0002AAAA"]),
        _ => (["0002BBBB"], ["0002BBBB"]),
    };

    public static TheoryData<Change, FileSharing> Statements()
    {
        var data = new TheoryData<Change, FileSharing>();
        foreach (var change in Enum.GetValues<Change>())
            foreach (var sharing in new[] { FileSharing.AllOther, FileSharing.ReadOnly, FileSharing.NoOther })
                data.Add(change, sharing);
        return data;
    }

    /// <summary>The writes one statement hands over, measured on a run that does not die.</summary>
    private static int WritesOf(Change change, FileSharing sharing, out int closeWrites)
    {
        string host = Tmp("measure");
        try
        {
            Seed(host);
            DyingPageMedium? medium = null;
            KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
            var a = new FileRegistry();
            try
            {
                Register(a, "A", host, sharing);
                a.OpenStatic("A", FileOpenMode.IO);
            }
            finally { KeyedStore.MediumObserver.Value = null; }
            int before = medium!.Writes;
            Assert.True(Run(a, change) is "00" or "02");
            int statement = medium.Writes - before;
            a.Close("A");
            closeWrites = medium.Writes - before - statement;
            return statement;
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.16's crash points of a changing statement and of the CLOSE's promotion, through the INDEXED connector:
    /// a death after each write <c>i</c> leaves the previous statement's file — in BOTH trees — until the statement's LAST
    /// write, its page 2, lands, and the statement's file from then on; a death at any write of the CLOSE that follows keeps
    /// the statement. For a coherent connector a second run unit B, open beside the dying A, then runs its own WRITE: '00'
    /// (claiming above whatever A claimed and minting a release ordinal above every one A drew — 2.2.1.3 decision 4), and
    /// B's record is in both trees beside A's completed ones (2.2.1.17, "A's statement dies mid-way").</summary>
    [Theory]
    [MemberData(nameof(Statements))]
    public void A_death_after_every_write_leaves_the_last_completed_statement(Change change, FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int writes = WritesOf(change, sharing, out int closeWrites);
        Assert.True(writes >= 3, $"a changing statement writes two trees' pages and then page 2 ({writes})");
        for (int i = 0; i <= writes + closeWrites; i++)
        {
            string host = Tmp($"die-{change}-{sharing}-{i}");
            try
            {
                Seed(host);
                DyingPageMedium? medium = null;
                KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
                var a = new FileRegistry();
                try
                {
                    Register(a, "A", host, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { KeyedStore.MediumObserver.Value = null; }
                FileRegistry? b = null;
                if (sharing != FileSharing.NoOther)
                {
                    b = new FileRegistry();
                    Register(b, "B", host, FileSharing.AllOther);
                    b.OpenStatic("B", sharing == FileSharing.ReadOnly ? FileOpenMode.Input : FileOpenMode.IO);
                    Assert.Equal("00", b.Status("B"));
                }
                medium!.DieAfter = medium.Writes + i;
                Run(a, change);   // what the dying run unit is told is not asked: it is dead from write i on
                a.Close("A");     // its handle and its locks go with it; none of its writes after the death land
                bool completed = i >= writes;
                var (byPrime, byAlternate) = completed ? After(change) : Before;
                var expectedPrime = byPrime.ToList();
                var expectedAlternate = byAlternate.ToList();
                if (b is not null)
                {
                    if (sharing == FileSharing.AllOther)
                    {
                        Assert.Equal("00", Write(b, "B", 5, "EEEE"));
                        expectedPrime.Add("0005EEEE");
                        expectedAlternate.Add("0005EEEE");
                    }
                    else
                    {
                        // The INPUT reader beside a READ ONLY writer follows page 2 and reads the completed file.
                        Assert.Equal(completed && change == Change.Delete ? "23" : "00", ReadKey(b, "B", -1, Rec(1, "    "), out _));
                    }
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
                var (gotPrime, gotAlternate) = Records(host);
                Assert.Equal(expectedPrime, gotPrime);
                Assert.Equal(expectedAlternate, gotAlternate);
            }
            finally
            {
                KeyedStore.MediumObserver.Value = null;
                TryDelete(host);
            }
        }
    }

    /// <summary>2.2.1.16's refused statements through the INDEXED connector: a host refusal of write <c>i</c> answers THAT
    /// statement '30' (§9.1.13.6 item 1: <i>"A permanent error exists and no further information is available"</i>), the
    /// same connector's next statement replays it and touches nothing (§9.1.13.1: <i>"The permanent error condition remains
    /// in effect for all subsequent input-output operations on the file"</i>), its CLOSE ends it ('00', DOC-A.1-105), and the
    /// next OPEN reads the previous statement's file in both trees. Another run unit's next WRITE succeeds (2.2.1.17, "A's
    /// statement is refused mid-way").</summary>
    [Theory]
    [MemberData(nameof(Statements))]
    public void A_refusal_at_every_write_is_the_statements_30_and_leaves_the_previous_statement(Change change, FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int writes = WritesOf(change, sharing, out _);
        for (int i = 0; i < writes; i++)
        {
            string host = Tmp($"refuse-{change}-{sharing}-{i}");
            int refuseAt = int.MaxValue, seen = 0;
            try
            {
                Seed(host);
                KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                    write: _ => seen++ == refuseAt ? new IOException("refused by the host") : null);
                var a = new FileRegistry();
                try
                {
                    Register(a, "A", host, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { KeyedStore.MediumObserver.Value = null; }
                refuseAt = seen + i;
                Assert.Equal("30", Run(a, change));
                Assert.Equal("30", Write(a, "A", 7, "GGGG"));                     // replayed, nothing touched
                Assert.Equal("30", ReadKey(a, "A", 0, Rec(0, "BBBB"), out string image));
                Assert.Equal("", image.Trim());
                var expectedPrime = Before.ByPrime.ToList();
                var expectedAlternate = Before.ByAlternate.ToList();
                if (sharing == FileSharing.AllOther)
                {
                    var b = new FileRegistry();
                    Register(b, "B", host, FileSharing.AllOther);
                    b.OpenStatic("B", FileOpenMode.IO);
                    Assert.Equal("02", Write(b, "B", 5, "AAAA"));   // a permitted duplicate of record 1's key: '02'
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                    expectedPrime.Add("0005AAAA");
                    expectedAlternate.Insert(1, "0005AAAA");
                }
                a.Close("A");
                Assert.Equal("00", a.Status("A"));
                var (gotPrime, gotAlternate) = Records(host);
                Assert.Equal(expectedPrime, gotPrime);
                Assert.Equal(expectedAlternate, gotAlternate);
            }
            finally
            {
                KeyedStore.MediumObserver.Value = null;
                TryDelete(host);
            }
        }
    }

    /// <summary>2.2.1.17, "A's statement ∥ B's statement", through the INDEXED connector: each run unit's statement starts
    /// from the file as the other's last statement left it — the invalid key conditions are the file's ('22' for a prime key
    /// the other wrote, '22' for a no-duplicates alternate it holds, '23' for a record the other deleted), and §14.9.30.4
    /// GR26's duplicate order is the FILE'S release order whichever run unit released the duplicates: the release mint is
    /// the meta page's, so B's WRITE after A's lands after it. A reader walking the duplicates of one value through
    /// another run unit's releases sees them in release order, '02' on every record but the last of the set (GR27 a)).</summary>
    [Fact]
    public void Two_run_units_share_one_duplicate_order_and_see_each_others_statements()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp("pair");
        try
        {
            Seed(host);
            var a = new FileRegistry();
            Register(a, "A", host, FileSharing.AllOther);
            a.OpenStatic("A", FileOpenMode.IO);
            var b = new FileRegistry();
            Register(b, "B", host, FileSharing.AllOther);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("02", Write(a, "A", 4, "AAAA"));       // released second under AAAA
            Assert.Equal("02", Write(b, "B", 3, "AAAA"));       // released third — B's mint follows A's
            Assert.Equal("22", Write(b, "B", 4, "ZZZZ"));       // §14.9.51.4 GR42 b): the prime key A wrote exists
            Assert.Equal("00", Delete(b, "B", 2));
            Assert.Equal("23", ReadKey(a, "A", -1, Rec(2, "    "), out _));   // §9.1.13.5 3 a): B deleted it
            Assert.Equal("00", ReadKey(a, "A", 0, Rec(0, "AAAA"), out string first));   // GR32: the FIRST released
            Assert.Equal("0001AAAA", first);
            // A's walk of AAAA's duplicates, with B releasing one more between A's reads.
            var seen = new List<(string Image, string Status)>();
            string status;
            while ((status = ReadNext(a, "A", out string next)) is "00" or "02")
            {
                seen.Add((next, status));
                if (seen.Count == 1) Assert.Equal("02", Write(b, "B", 9, "AAAA"));
            }
            Assert.Equal("10", status);
            Assert.Equal([("0004AAAA", "02"), ("0003AAAA", "02"), ("0009AAAA", "00")], seen);
            a.Close("A");
            b.Close("B");
            var (byPrime, byAlternate) = Records(host);
            Assert.Equal(["0001AAAA", "0003AAAA", "0004AAAA", "0009AAAA"], byPrime);
            Assert.Equal(["0001AAAA", "0004AAAA", "0003AAAA", "0009AAAA"], byAlternate);
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ FINDING R2-6 FOR THE INDEXED CONNECTOR (2.2.1.7): a <c>SHARING WITH READ ONLY</c> writer beside the
    /// <c>INPUT SHARING WITH ALL OTHER</c> reader Table 19 admits (§9.1.15 2)) runs under the store mutex, so every READ of
    /// the reader on another thread — by the prime key and by the alternate key — answers '00', '02', '10' or '23', never
    /// '30', the half-written path, while each WRITE changes two trees. Counted, not timed: the writer releases a fixed
    /// number of records and the reader reads until it has seen the last.</summary>
    [Fact]
    public async Task A_read_only_writer_and_an_input_reader_in_another_run_unit_never_meet_a_half_written_path()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        const int Records = 400;
        string host = Tmp("r2-6");
        try
        {
            Seed(host);
            var writer = new FileRegistry();
            Register(writer, "W", host, FileSharing.ReadOnly);
            writer.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", writer.Status("W"));
            var reader = new FileRegistry();
            Register(reader, "R", host, FileSharing.AllOther, KeyedAccess.Dynamic);
            reader.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal("00", reader.Status("R"));
            var writes = Task.Run(() =>
            {
                for (int n = 3; n < 3 + Records; n++)
                    Assert.True(Write(writer, "W", n, (n % 7).ToString("0000", System.Globalization.CultureInfo.InvariantCulture)) is "00" or "02");
            });
            var statuses = new HashSet<string>();
            bool last = false;
            while (!last && !writes.IsFaulted)
            {
                string byPrime = ReadKey(reader, "R", -1, Rec(2 + Records, "    "), out _);
                Assert.True(byPrime is "00" or "23", $"READ by the prime key answered '{byPrime}' beside the READ ONLY writer");
                last = byPrime == "00";
                string byAlternate = ReadKey(reader, "R", 0, Rec(0, "0003"), out _);
                Assert.True(byAlternate is "00" or "23", $"READ by the alternate key answered '{byAlternate}'");
                if (byAlternate != "00") continue;
                string status = ReadNext(reader, "R", out _);
                statuses.Add(status);
                Assert.True(status is "00" or "02" or "10", $"READ NEXT answered '{status}' beside the READ ONLY writer");
            }
            await writes;
            reader.Close("R");
            writer.Close("W");
            Assert.True(last);
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.12's tree cases with an alternate key: a READ NEXT through the duplicates of one value, while ANOTHER
    /// run unit releases further duplicates of it — splitting the alternate tree's leaves under the reader — selects every
    /// time GR21 e)'s next record in (key, release ordinal) order in the tree as it is NOW, so the reader sees the seeded
    /// duplicates and the other run unit's in release order, then the next value, then at end.</summary>
    [Fact]
    public void A_duplicate_walk_continues_through_a_tree_another_run_unit_splits()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp("split");
        try
        {
            var seed = new FileRegistry();
            Register(seed, "S", host, sharing: null);
            seed.OpenStatic("S", FileOpenMode.Output);
            for (int n = 1; n <= 400; n++) Assert.True(Write(seed, "S", n, "DDDD") is "00" or "02");
            Assert.Equal("00", Write(seed, "S", 9999, "ZZZZ"));
            seed.Close("S");
            var reader = new FileRegistry();
            Register(reader, "R", host, FileSharing.AllOther);
            reader.OpenStatic("R", FileOpenMode.Input);
            var writer = new FileRegistry();
            Register(writer, "W", host, FileSharing.AllOther);
            writer.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", reader.StartIndexed("R", 0, "==", Rec(0, "DDDD"), StartKeyLength.OfWidth(4)));
            var seen = new List<int>();
            string status;
            while ((status = ReadNext(reader, "R", out string image)) is "00" or "02")
            {
                int n = int.Parse(image[..4], System.Globalization.CultureInfo.InvariantCulture);
                seen.Add(n);
                if (n <= 400 && n % 2 == 0) Assert.Equal("02", Write(writer, "W", 1000 + n, "DDDD"));
            }
            Assert.Equal("10", status);
            var expected = Enumerable.Range(1, 400).Concat(Enumerable.Range(1, 200).Select(k => 1000 + 2 * k)).Append(9999);
            Assert.Equal(expected, seen);
            reader.Close("R");
            writer.Close("W");
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.11's INDEXED row, counted and never timed (the repository's rule): a WRITE, REWRITE or DELETE of a
    /// file with k alternate keys writes at most (k + 1) × d pages of its trees + 2 (ledger) + 2 (free batch) + 1 (page 2)
    /// + 1 (the claim, under the mutex only), d the trees' depth — a statement that promotes first (R2's ceiling, R5's
    /// ledger checkpoint) is the promotion row's, and is told apart by its syncs. A load of 4 000 records keeps its trees at depth ≤ 2
    /// (2.2.1.4's fan-out arithmetic: a leaf of these entries holds well over a hundred), so no statement of it writes more
    /// than 2 × 2 + 6 = 10 pages — and the last statement writes no more than the first statements after the trees reached
    /// two levels plus the depth growth, whatever the file's size: the cost is per statement, not per file (kb/Work
    /// PB2129's quadratic load is gone).</summary>
    [Theory]
    [InlineData(FileSharing.AllOther)]
    [InlineData(FileSharing.NoOther)]
    public void A_statement_writes_pages_bounded_by_its_trees_depth_whatever_the_files_size(FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp($"counts-{sharing}");
        DyingPageMedium? medium = null;
        try
        {
            KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
            var a = new FileRegistry();
            Register(a, "A", host, sharing);
            a.OpenStatic("A", FileOpenMode.Output);
            Assert.Equal("00", a.Status("A"));
            var perStatement = new List<int>();
            for (int n = 1; n <= 4000; n++)
            {
                int before = medium!.Writes, synced = medium.Syncs;
                Assert.True(Write(a, "A", n, (n % 50).ToString("0000", System.Globalization.CultureInfo.InvariantCulture)) is "00" or "02");
                if (medium.Syncs == synced) perStatement.Add(medium.Writes - before);
            }
            a.Close("A");
            Assert.Equal("00", a.Status("A"));
            // The REWRITE that moves an alternate entry and the DELETE of n + 1 entries, on the full file.
            a.OpenStatic("A", FileOpenMode.IO);
            foreach (var change in new Func<string>[] { () => Rewrite(a, "A", 2000, "XXXX"), () => Delete(a, "A", 3000) })
            {
                int before = medium!.Writes, synced = medium.Syncs;
                Assert.Equal("00", change());
                if (medium.Syncs == synced) perStatement.Add(medium.Writes - before);
            }
            a.Close("A");
            Assert.Equal("00", a.Status("A"));
            int bound = 2 * 2 + (sharing == FileSharing.NoOther ? 5 : 6);
            Assert.True(perStatement.Count > 3000, $"{perStatement.Count} statements promoted nothing");
            Assert.True(perStatement.Max() <= bound, $"a WRITE wrote {perStatement.Max()} pages (bound {bound})");
            Assert.True(perStatement[^100..].Average() <= perStatement[1000..1100].Average() + 1,
                "the per-statement cost grew with the file");
        }
        finally
        {
            KeyedStore.MediumObserver.Value = null;
            TryDelete(host);
        }
    }

    /// <summary>§14.9.41.4 GR17 over the trees, each relational operator with a FULL key and a PARTIAL one (GR17 b)'s length
    /// below the key's): the first record satisfying the comparison (for the LESS relations the LAST — the file searched in
    /// reverse order, GR17 d)) becomes the position, '00', and the next READ NEXT delivers it (§14.9.30.4 GR21 d) 1., the
    /// key value alone); none satisfies it: '23', the invalid key condition, with the file position indicator invalidated
    /// (GR7; the next sequential READ is '46'). Under an alternate key a START into a set of duplicates enters it at the end
    /// §14.9.30.4 GR26 names for the READ's direction: NEXT from the first released, PREVIOUS from the last.</summary>
    [Fact]
    public void Start_with_each_operator_full_and_partial_keys_found_and_not_found()
    {
        string host = Tmp("start");
        try
        {
            var w = Connector(host);
            Assert.Equal("00", w.Open(FileOpenMode.Output));
            foreach (string record in new[] { "AB20DDDD", "AA10DDDD", "AC10EEEE", "AA20CCCC", "AB10DDDD" })
                Assert.True(w.Write(record) is "00" or "02");
            Assert.Equal("00", w.Close());

            var r = Connector(host);
            Assert.Equal("00", r.Open(FileOpenMode.Input));
            (string Op, string Key, int Length, string? First)[] cases =
            [
                ("==", "AB", 2, "AB10"), (">", "AB", 2, "AC10"), (">=", "AB", 2, "AB10"), ("<", "AB", 2, "AA20"), ("<=", "AB", 2, "AB20"),
                ("==", "AD", 2, null), (">", "AC", 2, null), ("<", "AA", 2, null),
                ("==", "AB20", 4, "AB20"), ("==", "AB15", 4, null), (">", "AC10", 4, null), (">=", "AB15", 4, "AB20"),
                ("<", "AA10", 4, null), ("<=", "AA10", 4, "AA10"), ("<", "AB15", 4, "AB10"),
            ];
            foreach (var (op, key, length, first) in cases)
            {
                string started = r.Start(-1, op, key.PadRight(8), StartKeyLength.OfWidth(length));
                Assert.Equal(first is null ? "23" : "00", started);
                string next = r.ReadNext(out string image);
                if (first is null) Assert.Equal("46", next);   // §9.1.13.7 6 a): no valid next record
                else Assert.Equal(first, image[..4]);
            }
            // Duplicates of DDDD were released AB20, AA10, AB10: NEXT enters at the first released, PREVIOUS at the last.
            Assert.Equal("00", r.Start(0, "==", "    DDDD", StartKeyLength.OfWidth(4)));
            Assert.Equal("02", r.ReadNext(out string d1));
            Assert.Equal("02", r.ReadNext(out string d2));
            Assert.Equal("00", r.ReadNext(out string d3));
            Assert.Equal(["AB20", "AA10", "AB10"], new[] { d1[..4], d2[..4], d3[..4] });
            Assert.Equal("00", r.Start(0, "<=", "    DDDD", StartKeyLength.OfWidth(4)));
            Assert.Equal("02", r.ReadPrevious(out string p1));
            Assert.Equal("02", r.ReadPrevious(out string p2));
            Assert.Equal("00", r.ReadPrevious(out string p3));
            Assert.Equal(["AB10", "AA10", "AB20"], new[] { p1[..4], p2[..4], p3[..4] });
            Assert.Equal("00", r.ReadPrevious(out string below));     // the next lower value, CCCC
            Assert.Equal("AA20CCCC", below);
            Assert.Equal("00", r.Close());
        }
        finally { TryDelete(host); }

        static IndexedConnector Connector(string host)
        {
            var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, [new(0, 4)]);
            c.AddAlternateKey([new(4, 4)], duplicates: true);
            return c;
        }
    }

    /// <summary>⛔ §14.9.35.4 GR24 a) BY CONSTRUCTION: <i>"When the value of a specific alternate record key is not changed,
    /// the order of retrieval when that key is the key of reference remains unchanged"</i>. A REWRITE that changes alternate
    /// key 2 of 3 — away, and then back to its old value — leaves the trees of keys 1 and 3 byte-identical (nothing touched
    /// them), and puts the record LAST among key 2's duplicates (GR24 b), a fresh ordinal).</summary>
    [Fact]
    public void A_rewrite_changing_one_alternate_key_leaves_the_other_keys_trees_byte_identical()
    {
        string host = Tmp("gr24");
        try
        {
            var c = new IndexedConnector(host, 16, KeyedAccess.Dynamic, [new(0, 4)]);
            for (int k = 0; k < 3; k++) c.AddAlternateKey([new(4 + 4 * k, 4)], duplicates: true);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Assert.Equal("00", c.Write("0001AAAABBBBCCCC"));
            Assert.Equal("02", c.Write("0002AAAABBBBCCCC"));
            Assert.Equal("02", c.Write("0003AAAABBBBCCCC"));
            Assert.Equal("00", c.Close());
            var before = Trees(host);

            Assert.Equal("00", c.Open(FileOpenMode.IO));
            Assert.Equal("00", c.Rewrite("0001AAAAXXXXCCCC"));
            Assert.Equal("02", c.Rewrite("0001AAAABBBBCCCC"));   // §9.1.13.2 2 c): the change CREATED a duplicate
            Assert.Equal("00", c.Close());
            var after = Trees(host);

            Assert.Equal(before[0], after[0]);
            Assert.Equal(before[2], after[2]);
            Assert.NotEqual(before[1], after[1]);
            Assert.Equal(["BBBB->0002", "BBBB->0003", "BBBB->0001"], after[1].Select(e => e.Text));
            Assert.Equal(["AAAA->0001", "AAAA->0002", "AAAA->0003"], after[0].Select(e => e.Text));
        }
        finally { TryDelete(host); }

        // Each alternate tree's entries as (the whole entry key in hex, a readable "key->prime" form).
        static List<List<(string Key, string Text)>> Trees(string host)
        {
            using var handle = File.OpenHandle(host, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assert.Equal(StoreStatus.Ok, PagedFile.Open(new HandlePageMedium(handle), writable: false, coherent: false, out var file));
            return [.. file!.Meta.AlternateRoots.Select(root =>
            {
                var entries = new List<(string, string)>();
                var tree = new BTree(file, new CompositeKeyOrder(KeyOrders.Ordinal), root);
                Assert.Equal(StoreStatus.Ok, tree.First(out var cursor));
                while (cursor.HasCurrent)
                {
                    Assert.Equal(StoreStatus.Ok, cursor.ReadValue(out var prime));
                    entries.Add((Convert.ToHexString(cursor.Key), $"{Encoding.Latin1.GetString(cursor.Key[..4])}->{Encoding.Latin1.GetString(prime)}"));
                    Assert.Equal(StoreStatus.Ok, cursor.Next());
                }
                return entries;
            })];
        }
    }

    /// <summary>⛔ THE <c>COBOLNET_KEYCHECK</c> OPT-OUT'S TREES (docs/CONFORMANCE.md DOC-A.1-129). With the key table out of
    /// the validated set, a description whose key table differs from the file's opens it, and the FILE'S key table is the
    /// trees': a declared alternate key the file records no tree for provides no access path (a READ or START under it is
    /// '23'), and a tree the description does not declare is not maintained by its statements — a record it writes has no
    /// entry there, and an entry whose record it deleted or re-released is passed over, never delivered (the entry names
    /// a record that no longer carries its ordinal). The records themselves, by the prime key, are the file's.</summary>
    [Fact]
    public void The_keycheck_opt_out_drives_the_files_trees_by_key_number()
    {
        string host = Tmp("keycheck");
        string? saved = Environment.GetEnvironmentVariable(FileRegistry.KeyCheckVariable);
        try
        {
            Seed(host);   // prime 1-4, one alternate WITH DUPLICATES 5-8
            Environment.SetEnvironmentVariable(FileRegistry.KeyCheckVariable, "OFF");
            var wide = new FileRegistry();
            Register(wide, "W", host, sharing: null);
            wide.AddAlternateKey("W", [new(2, 2)], duplicates: true);   // a second alternate the file has no tree for
            wide.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", wide.Status("W"));
            Assert.Equal("23", ReadKey(wide, "W", 1, Rec(1, "AAAA"), out _));
            Assert.Equal("23", wide.StartIndexed("W", 1, ">=", Rec(0, "    "), StartKeyLength.OfWidth(2)));
            Assert.Equal("02", Write(wide, "W", 3, "AAAA"));       // the tree the file has is maintained
            wide.Close("W");

            var narrow = new FileRegistry();
            narrow.RegisterIndexed("N", host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);   // no alternate key: the file's tree is not its
            narrow.OpenStatic("N", FileOpenMode.IO);
            Assert.Equal("00", narrow.Status("N"));
            Assert.Equal("00", narrow.WriteShared("N", Rec(4, "AAAA"), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            Assert.Equal("00", narrow.DeleteShared("N", Rec(1, "    "), FileRetryKind.None, 0));
            narrow.Close("N");

            Environment.SetEnvironmentVariable(FileRegistry.KeyCheckVariable, saved);
            var (byPrime, byAlternate) = Records(host);
            Assert.Equal(["0002BBBB", "0003AAAA", "0004AAAA"], byPrime);
            Assert.Equal(["0003AAAA", "0002BBBB"], byAlternate);   // 1's entry passed over; 4 has none
        }
        finally
        {
            Environment.SetEnvironmentVariable(FileRegistry.KeyCheckVariable, saved);
            TryDelete(host);
        }
    }

    /// <summary>A file of the whole-store format — version 5, its magic at offset 0 (kb/Work PB1099), and version 4 (kb/Work
    /// PB2693) — is not a paged store: the INDEXED OPEN answers §14.9.27.4 GR10's '39' and leaves the file as it was (GR25,
    /// <i>"If the execution of the OPEN statement is unsuccessful, the file is not affected"</i>; 2.2.1.3 decision 1: there
    /// is no reader of the whole-store format).</summary>
    [Theory]
    [InlineData(5)]
    [InlineData(4)]
    public void A_whole_store_file_is_a_fixed_attribute_conflict_39(int version)
    {
        string host = Tmp($"format{version}");
        try
        {
            var declared = new IndexedConnector(host, 8, KeyedAccess.Random, [new(0, 4)]).DeclaredAttributes;
            using (var image = RecordFraming.ComposeStore(declared, [new StoredFrame("0001AAAA", null, [1])], generation: 1, releaseMint: 2))
            {
                byte[] bytes = image.ToArray();
                bytes["CBNFSTR"u8.Length] = (byte)version;   // the version byte after the whole-store magic
                File.WriteAllBytes(host, bytes);
            }
            byte[] before = File.ReadAllBytes(host);
            foreach (var mode in new[] { FileOpenMode.Input, FileOpenMode.IO, FileOpenMode.Extend })
            {
                var c = new IndexedConnector(host, 8, KeyedAccess.Sequential, [new(0, 4)]);
                Assert.Equal("39", c.Open(mode));
                Assert.Equal(before, File.ReadAllBytes(host));
            }
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ THE FORMAT BOUNDS A KEY BY ITS LEAF (2.2.1.4; DOC-A.1-129): a tree key holds at most
    /// <see cref="BTree.MaxKeyBytes"/> (1 016) bytes, one per character position — the prime key's whole, an alternate key's
    /// with its 8-byte release ordinal — so a declaration with a longer key is a fixed attribute the physical format cannot
    /// hold: §14.9.27.4 GR10's '39' at the OPEN, in every mode (OUTPUT included: the format is refused, not merely
    /// compared), and no file is touched (GR25). At the bound the keys work.</summary>
    [Theory]
    [InlineData(1017, 4, false)]
    [InlineData(4, 1009, false)]
    [InlineData(1016, 1008, true)]
    public void A_key_past_the_formats_bound_is_39_at_the_open(int primeLength, int alternateLength, bool holds)
    {
        string host = Tmp($"bound-{primeLength}-{alternateLength}");
        try
        {
            int width = primeLength + alternateLength;
            var c = new IndexedConnector(host, width, KeyedAccess.Dynamic, [new(0, primeLength)]);
            c.AddAlternateKey([new(primeLength, alternateLength)], duplicates: false);
            Assert.Equal(holds ? "00" : "39", c.Open(FileOpenMode.Output));
            if (!holds)
            {
                Assert.False(File.Exists(host), "an OPEN the format refuses creates no file");
                return;
            }
            string record = new string('P', primeLength) + new string('A', alternateLength);
            Assert.Equal("00", c.Write(record));
            Assert.Equal("22", c.Write(new string('Q', primeLength) + new string('A', alternateLength)));   // GR42 c)
            Assert.Equal("00", c.Close());
            Assert.Equal("00", c.Open(FileOpenMode.Input));
            Assert.Equal("00", c.ReadRandom(0, record, out string image));
            Assert.Equal(record, image);
            Assert.Equal("00", c.Close());
        }
        finally { TryDelete(host); }
    }

    /// <summary>The tree's COBOL surface for an INDEXED record statement whose tree read meets a TORN page (2.2.1.6 R3):
    /// '30' (§9.1.13.6 item 1) with the file unchanged, §9.1.13.1's permanent error replayed by every later statement,
    /// the CLOSE ending it (DOC-A.1-105).</summary>
    [Fact]
    public void A_torn_tree_page_is_30_at_the_statement_and_in_effect_until_the_close()
    {
        string host = Tmp("torn");
        try
        {
            var c = new IndexedConnector(host, 8, KeyedAccess.Dynamic, [new(0, 4)]);
            c.AddAlternateKey([new(4, 4)], duplicates: false);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            Assert.Equal("00", c.Write("0004DDDD"));
            Assert.Equal("00", c.Close());
            byte[] file = File.ReadAllBytes(host);
            int leaf = Enumerable.Range(3, file.Length / PageHeader.PageSize - 3)
                .Single(p => file[p * PageHeader.PageSize] == (byte)PageKind.Leaf
                    && PageHeader.ChecksumHolds(file.AsSpan(p * PageHeader.PageSize, PageHeader.PageSize))
                    && file.AsSpan(p * PageHeader.PageSize, PageHeader.PageSize).IndexOf("DDDD"u8) >= 0
                    && file.AsSpan(p * PageHeader.PageSize, PageHeader.PageSize).IndexOf("0004DDDD"u8) >= 0);
            file[leaf * PageHeader.PageSize + 100] ^= 0xFF;   // the prime tree's leaf, which holds the frame
            File.WriteAllBytes(host, file);
            var t = new IndexedConnector(host, 8, KeyedAccess.Dynamic, [new(0, 4)]);
            t.AddAlternateKey([new(4, 4)], duplicates: false);
            Assert.Equal("00", t.Open(FileOpenMode.IO));
            Assert.Equal("30", t.ReadRandom(-1, "0004    ", out _));
            Assert.Equal("30", t.Write("0005EEEE"));                  // replayed
            Assert.Equal("00", t.Close());                             // the CLOSE ends it (DOC-A.1-105)
        }
        finally { TryDelete(host); }
    }
}
