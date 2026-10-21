// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE KEYED STORE'S PERFORMANCE BAR, MEASURED IN PAGES, NEVER IN TIME (docs/rearchitecture/DESIGN-runtime-library.md
/// 2.2.1.11's witnesses (a) to (c); kb/Work PB3336, PB3330). Every case drives the REAL connectors through a
/// <see cref="FileRegistry"/> and reads the run unit's <see cref="PagedFile.Counters"/> across one statement; the pages
/// live in a <see cref="PageArrayMedium"/>, so a load of 4N records costs no host I/O and the counts are exact on every
/// host. Each file shape is one derived class ({RELATIVE, INDEXED with no alternate key, INDEXED with two: one WITH
/// DUPLICATES and one unique}), and each of its cells one key order (ascending, or a fixed random permutation) × one sharing
/// mode (<c>NO OTHER</c>, where no statement reads page 2, and <c>ALL OTHER</c>, where every statement is a coherent entry
/// under the store mutex, 2.2.1.7). The expected values are 2.2.1.11's table rows, with d each tree's depth read from the
/// file, and the general rules' statuses, never what the engine happens to do:
/// <list type="bullet">
///   <item>(a) LINEARITY: a load of N and then of 4N records, the two sizes straddling a root split. Every WRITE stays
///   within its row — (k + 1) × d for k alternate keys (none for RELATIVE), + the ledger's path copy (at most the ledger
///   tree's pages, which R2's checkpoint bounds whatever the file's size), + 2 (the FREE batch) + 1 (meta), + 1 for the
///   claim under the mutex, + the pages its splits add (one per level that splits, which the trees' page count gains), +
///   the promotion row for a statement that promotes first (told apart by its syncs) — the ledger never lists more than the
///   checkpoint, and the mean pages written per statement at 4N exceed the mean at N by at most the trees' depth growth,
///   the ledger's growth and the FREE batch.</item>
///   <item>(b) OPEN READS THE HEADER: after a CLOSE, an OPEN reads at most 3 + 7 pages whatever N; an INPUT OPEN writes and
///   syncs nothing, a writable one (the fence) writes at most 8 and syncs twice; after a process death (no CLOSE) the OPEN
///   reads the 3 slots, the ledger's pages and its fresh pages (R6's verification), and the dead run unit's completed
///   statements are in the file (R6: a DESCENDANT whose fresh pages verify is adopted).</item>
///   <item>(c) RANDOM READ IS O(log n): 1 000 random READs by the prime key on the 4N store, the page cache dropped before
///   each, read exactly the path (d), + 1 (page 2) under the mutex; by the unique alternate key, its tree's path and the
///   prime tree's, + 1 when the prefix seek of a (key value, release ordinal) composite crosses into the next leaf.</item>
///   <item>The READ NEXT walk of PB2129's and PB2755's scenarios: each statement reads at most d pages (+ 1), the walk reads
///   each page of the tree once, and the READ that meets the end answers '10' (ISO §14.9.30.4 GR24 a), the AT END
///   condition) within the same bound; along the alternate key WITH DUPLICATES each READ answers '02' exactly when the
///   next record carries the same key value (§14.9.30.4 GR27 a)).</item>
///   <item>The exception paths cost no more than the success and write NOTHING when they change nothing: a WRITE refused
///   '22' — the prime key or a unique alternate key a record holds (§14.9.51.4 GR42 b) and c); GR33 a) for RELATIVE) — and
///   §14.9.51.4 GR15's <i>"the write operation does not take place"</i>; a READ and a DELETE refused '23' (§9.1.13.5 3)
///   a)); a WRITE the medium refuses answers '30' (§9.1.13.6 1)), and after the CLOSE the next run unit's WRITE is within
///   its row and the refused record is not in the file; the permanent error then answers every later statement on the
///   connector (§9.1.13.1) — WRITE, READ, READ NEXT, START, REWRITE and DELETE, one case each in both sharing modes —
///   reading, writing and syncing NOTHING, not even the coherent entry's page 2 (docs/CONFORMANCE.md DOC-A.1-105: such a
///   statement <i>"does not touch the physical file"</i>), and outranks another run unit's lock; a READ that
///   meets another run unit's record lock answers '51' (§9.1.13.8 1)) writing nothing, a RETRY repeats only that bounded
///   statement (§14.7.9.3 GR1), and a RETRY whose wait sees the lock released succeeds (§14.7.9.3 GR4).</item>
/// </list>
/// </summary>
public abstract class KeyedStorePerformanceWitnessTests(KeyedStorePerformanceWitnessTests.Shape shape, ITestOutputHelper output)
{
    public enum Shape { Relative, Indexed, IndexedTwoAlternates }

    public enum Order { Ascending, Random }

    /// <summary>The smaller load. A leaf holds about 90 of these records and an internal page about 185 children, so N
    /// fills a prime tree of depth 2 and 4N one of depth 3: the depth growth the linearity witness allows is exercised (the
    /// case asserts it).</summary>
    private const int N = 5_000;

    private const int RecordBytes = 24;

    /// <summary>The promotion row's writes (2.2.1.11): the claim with PROMOTING, a committed slot, the retirement (≤ 2
    /// ledger + 3 free tree + 1 meta).</summary>
    private const int PromotionWrites = 1 + 1 + 2 + 3 + 1;

    /// <summary>The most pages the ledger tree held at R2's checkpoint in the twelve load cells (2.2.1.11's ℓ): a MEASURED
    /// maximum for this workload, not a bound that follows from the entry count. The ledger lists at most 1 024 entries
    /// plus the pages of the one statement that begins at the checkpoint (1 028 under two alternate keys, the largest cell).
    /// A full leaf holds ⌊(4 096 − 32) / (6 + 4 + 9)⌋ = 213 entries, so 1 028 need at least 5 leaves; but the ledger is
    /// keyed by page number and a scattered load splits leaves at the middle, leaving each anywhere from half full to full,
    /// so the page count depends on that fill (the measured cell is 9 pages: the root over 8 leaves, about 60 percent
    /// full). The assertion pins the measured number: a ledger layout that let ℓ grow past it would put ℓ in every
    /// statement's row out of line with the figure the design states. A change to the entry size, the page size or the
    /// split rule moves the fill, and the constant is then re-measured, never re-derived from the entry count.</summary>
    private const int LedgerPagesAtCheckpoint = 9;

    /// <summary>The FREE batch a statement takes (2.2.1.11): at most 2 pages, at most once per statement.</summary>
    private const int FreeBatchPages = 2;

    /// <summary>The linearity witness compares MEANS over a window of statements; the ledger's and the FREE batch's own
    /// pages fall in a window at a rate that does not depend on the file's size, so two windows' means differ by a few
    /// thousandths of a page whatever the depth. A cost that grew with the file would differ by whole pages.</summary>
    private const double WindowNoise = 0.1;

    /// <summary>The cells of one file shape (each shape is its own class, so the three run in parallel).</summary>
    public static TheoryData<Order, FileSharing> Cells()
    {
        var data = new TheoryData<Order, FileSharing>();
        foreach (var order in Enum.GetValues<Order>())
            foreach (var sharing in new[] { FileSharing.NoOther, FileSharing.AllOther })
                data.Add(order, sharing);
        return data;
    }

    /// <summary>One physical file: its host path (the connectors' handles and locks), its pages, its shape, the order its
    /// records are loaded in, and the alternate key values WITH DUPLICATES it already holds (a WRITE that adds one answers
    /// '02', §9.1.13.2 2)).</summary>
    private sealed class Cell(Shape shape, Order order, FileSharing sharing) : IDisposable
    {
        public readonly string Host = FileHarness.Tmp($"pb3336-{shape}-{order}-{sharing}");
        public readonly PageArrayMedium Pages = new();
        public readonly Shape Shape = shape;
        public readonly FileSharing Sharing = sharing;
        public bool Coherent => Sharing != FileSharing.NoOther;
        public int AlternateKeys => Shape == Shape.IndexedTwoAlternates ? 2 : 0;
        private readonly HashSet<int> _duplicated = [];

        /// <summary>The record numbers 1..4N in load order.</summary>
        public readonly int[] Keys = LoadOrder(order);

        private static int[] LoadOrder(Order order)
        {
            var keys = Enumerable.Range(1, 4 * N).ToArray();
            if (order == Order.Random) new Random(3336).Shuffle(keys);
            return keys;
        }

        /// <summary>The status a successful WRITE of record n answers: '02' when the alternate key WITH DUPLICATES of
        /// record n duplicates one the file holds (§9.1.13.2 2), "a duplicate key is detected"), '00' otherwise.</summary>
        public string Written(int n) => AlternateKeys > 0 && !_duplicated.Add(n % 97) ? "02" : "00";

        public void Register(FileRegistry reg, string name)
        {
            if (Shape == Shape.Relative)
                reg.RegisterRelative(name, Host, RecordBytes, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                    relativeKeyDigits: 8, varyMin: -1, varyMax: -1);
            else
            {
                reg.RegisterIndexed(name, Host, RecordBytes, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                    primeKey: [new(0, 8)], varyMin: -1, varyMax: -1);
                if (Shape == Shape.IndexedTwoAlternates)
                {
                    reg.AddAlternateKey(name, [new(8, 8)], duplicates: true);
                    reg.AddAlternateKey(name, [new(16, 8)], duplicates: false);
                }
            }
            reg.RegisterSharing(name, Sharing, FileLockMode.Manual, multiple: false);
        }

        /// <summary>A new run unit with one connector on the file, opened in <paramref name="mode"/> over its own view of
        /// the pages.</summary>
        public (FileRegistry Reg, PageArrayMedium.View View) Open(string name, FileOpenMode mode, bool commit = false)
        {
            var reg = new FileRegistry();
            Register(reg, name);
            if (commit) reg.RegisterCommitControl(name);
            PageArrayMedium.View? view = null;
            KeyedStore.MediumObserver.Value = _ => view = Pages.Open();
            try { reg.OpenStatic(name, mode); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", reg.Status(name));
            return (reg, view!);
        }

        /// <summary>Load the records <c>Keys[from..to)</c> through a new run unit's <c>OPEN OUTPUT</c> (from 0) or
        /// <c>OPEN I-O</c>, and CLOSE it.</summary>
        public void Load(int from, int to)
        {
            var (reg, _) = Open("L", from == 0 ? FileOpenMode.Output : FileOpenMode.IO);
            for (int i = from; i < to; i++) Assert.Equal(Written(Keys[i]), Write(reg, "L", this, Keys[i]));
            reg.Close("L");
            Assert.Equal("00", reg.Status("L"));
        }

        public void Dispose()
        {
            FileHarness.TryDelete(Host);
        }
    }

    /// <summary>Record n: its prime key (the relative record number's digits for RELATIVE), an alternate key shared by
    /// every 97th record (WITH DUPLICATES), and a unique alternate key.</summary>
    private static string Rec(int n) => $"{n:D8}{n % 97:D8}{99_999_999 - n:D8}";

    private static string Write(FileRegistry reg, string name, Cell cell, int n, string? image = null)
    {
        if (cell.Shape == Shape.Relative) reg.SetRelativeKey(name, n);
        return reg.WriteShared(name, image ?? Rec(n), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    /// <summary>A READ by key: the prime key (key 0), or alternate key <paramref name="key"/> (1-based, in declaration
    /// order).</summary>
    private static string Read(FileRegistry reg, string name, Cell cell, int n, out string image, int key = 0,
        FileRecordLock phrase = FileRecordLock.None, FileRetryKind retry = FileRetryKind.None, long retryAmount = 0)
    {
        if (cell.Shape == Shape.Relative) reg.SetRelativeKey(name, n);
        return reg.ReadKeyedShared(name, key - 1, Rec(n), phrase, ignoringLock: false, retry, retryAmount, out image);
    }

    private static string ReadNext(FileRegistry reg, string name, out string image) =>
        reg.ReadShared(name, previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false,
            FileRetryKind.None, 0, out image);

    private static string Delete(FileRegistry reg, string name, Cell cell, int n, FileRetryKind retry = FileRetryKind.None,
        long retryAmount = 0)
    {
        if (cell.Shape == Shape.Relative) reg.SetRelativeKey(name, n);
        return reg.DeleteShared(name, Rec(n), retry, retryAmount);
    }

    private static string Rewrite(FileRegistry reg, string name, Cell cell, int n, string? image = null)
    {
        if (cell.Shape == Shape.Relative) reg.SetRelativeKey(name, n);
        return reg.RewriteShared(name, image ?? Rec(n), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    private static PagedFile Store(FileRegistry reg, string name) =>
        reg.PagedStoreOf(name) ?? throw new InvalidOperationException($"'{name}' is attached to no paged store");

    /// <summary>The roots of the file's record trees: the prime tree, then one per alternate key.</summary>
    private static IEnumerable<PagePointer> Roots(MetaPage meta) => [meta.PrimeRoot, .. meta.AlternateRoots];

    /// <summary>The depth of the tree at <paramref name="root"/>: the pages on its leftmost path (every leaf of a B+ tree
    /// lies at one depth, 2.2.1.4).</summary>
    private static int Depth(PagedFile store, PagePointer root)
    {
        var page = new byte[PageHeader.PageSize];
        int depth = 0;
        for (var at = root; !at.IsNone;)
        {
            Assert.Equal(PageCheck.Whole, store.Read(at, page));
            depth++;
            if (PageHeader.Decode(page).Kind == PageKind.Leaf) break;
            at = PagePointer.Read(page.AsSpan(PageHeader.Bytes));
        }
        return depth;
    }

    private static int[] Depths(PagedFile store) => [.. Roots(store.Meta).Select(r => Depth(store, r))];

    /// <summary>Every page of the tree at <paramref name="root"/> (2.2.1.4's internal-page layout: the leftmost child, then
    /// each separator key with the child to its right).</summary>
    private static HashSet<uint> TreePages(PagedFile store, PagePointer root)
    {
        var page = new byte[PageHeader.PageSize];
        var pages = new HashSet<uint>();
        var level = root.IsNone ? [] : new List<PagePointer> { root };
        while (level.Count > 0)
        {
            var next = new List<PagePointer>();
            foreach (var p in level)
            {
                pages.Add(p.Page);
                Assert.Equal(PageCheck.Whole, store.Read(p, page));
                var h = PageHeader.Decode(page);
                if (h.Kind == PageKind.Leaf) continue;
                var body = page.AsSpan(PageHeader.Bytes);
                next.Add(PagePointer.Read(body));
                int at = PagePointer.Bytes;
                for (int i = 0; i < h.Entries; i++)
                {
                    at += 2 + BinaryPrimitives.ReadUInt16LittleEndian(body[at..]);
                    next.Add(PagePointer.Read(body[at..]));
                    at += PagePointer.Bytes;
                }
            }
            level = next;
        }
        return pages;
    }

    private static int RecordTreePages(PagedFile store) => Roots(store.Meta).Sum(r => TreePages(store, r).Count);

    /// <summary>2.2.1.11's WRITE row for the cell at depth <paramref name="depth"/>, before its splits: (k + 1) × d (k
    /// alternate keys; none for RELATIVE) + the ledger's path copy — at most every page of the ledger tree, which the
    /// ledger checkpoint bounds (<paramref name="ledgerPages"/>) — + 2 (free batch) + 1 (meta); + 1 (the claim) under the
    /// mutex.</summary>
    private static int WriteBound(Cell cell, int depth, int ledgerPages) =>
        (cell.AlternateKeys + 1) * depth + ledgerPages + FreeBatchPages + 1 + (cell.Coherent ? 1 : 0);

    /// <summary>The pages of the ledger tree the store holds.</summary>
    private static int LedgerPages(PagedFile store) => TreePages(store, store.Meta.LedgerRoot).Count;

    private static IoCounters Since(PagedFile store, IoCounters before)
    {
        var now = store.Counters;
        return new(now.PagesRead - before.PagesRead, now.PagesWritten - before.PagesWritten, now.Syncs - before.Syncs);
    }

    /// <summary>What a load phase measured over its last window of statements that did not promote: the mean pages a
    /// WRITE wrote, and the mean pages of the ledger tree after it.</summary>
    private readonly record struct Phase(double MeanWritten, double MeanLedgerPages);

    /// <summary>One load phase through the connector <paramref name="name"/>: WRITE the records <c>Keys[from..to)</c>,
    /// asserting every statement against its row. A statement writes more than the row's no-split bound only by the new
    /// pages its splits add — at most one per level of each tree it changes — so the excess summed over the phase is at
    /// most the pages the trees gained. The ledger never lists more than R2's checkpoint (1 024 pages) and one statement's
    /// own, whatever the file's size, and its tree never holds more than <see cref="LedgerPagesAtCheckpoint"/> pages (the
    /// most this workload measured): the checkpoint is what bounds the ledger's share of every statement.</summary>
    private Phase Load(FileRegistry reg, string name, Cell cell, int from, int to, int window, out int promoted)
    {
        var store = Store(reg, name);
        int pagesBefore = RecordTreePages(store);
        var written = new Queue<long>();
        var ledger = new Queue<int>();
        promoted = 0;
        long worst = 0, excess = 0;
        int mostListed = 0, mostLedgerPages = 0;
        for (int i = from; i < to; i++)
        {
            var before = store.Counters;
            Assert.Equal(cell.Written(cell.Keys[i]), Write(reg, name, cell, cell.Keys[i]));
            var cost = Since(store, before);
            int[] depths = Depths(store);
            int ledgerPages = LedgerPages(store);
            mostListed = Math.Max(mostListed, store.LedgerEntries.Count);
            mostLedgerPages = Math.Max(mostLedgerPages, ledgerPages);
            int bound = WriteBound(cell, depths.Max(), ledgerPages) + (cost.Syncs > 0 ? PromotionWrites : 0);
            if (cost.PagesWritten > bound + depths.Sum())
                Assert.Fail($"WRITE of record {cell.Keys[i]} (statement {i + 1}, depths [{string.Join(",", depths)}], ledger {ledgerPages} pages) wrote {cost.PagesWritten} pages; 2.2.1.11's row allows {bound} + a split at every level");
            excess += Math.Max(0, cost.PagesWritten - bound);
            if (cost.Syncs > 0) { promoted++; continue; }
            worst = Math.Max(worst, cost.PagesWritten);
            written.Enqueue(cost.PagesWritten);
            ledger.Enqueue(ledgerPages);
            if (written.Count > window) { written.Dequeue(); ledger.Dequeue(); }
        }
        int gained = RecordTreePages(store) - pagesBefore;
        output.WriteLine($"{cell.Shape}/{cell.Sharing}: records {from + 1}..{to}: worst WRITE {worst} pages, mean of the last {written.Count} {written.Average():0.###} (ledger {ledger.Average():0.##} pages), {promoted} promoted, {excess} pages over the no-split row, the trees gained {gained}, depths [{string.Join(",", Depths(store))}], the ledger at most {mostListed} entries on {mostLedgerPages} pages");
        Assert.InRange(excess, 0, gained);
        Assert.InRange(mostListed, 0, PagedFile.LedgerCheckpoint + MostPagesOfOneStatement(cell, Depths(store).Max()));
        Assert.InRange(mostLedgerPages, 1, LedgerPagesAtCheckpoint);
        return new(written.Average(), ledger.Average());
    }

    /// <summary>The most pages one WRITE can add to the ledger: every record tree's path copied with a split at every
    /// level, its released pages and its fresh ones.</summary>
    private static int MostPagesOfOneStatement(Cell cell, int depth) => 2 * 2 * (cell.AlternateKeys + 1) * depth + 8;

    /// <summary>The OPEN row (2.2.1.11) after a CLOSE: reads ≤ 3 + 7; INPUT writes and syncs nothing; a writable OPEN (the
    /// fence) writes ≤ 8 and syncs twice. Answers the pages the INPUT OPEN read.</summary>
    private long OpenAfterClose(Cell cell, string tag)
    {
        var (reader, _) = cell.Open("I", FileOpenMode.Input);
        var input = Store(reader, "I").Counters;
        reader.Close("I");
        Assert.Equal("00", reader.Status("I"));
        var (writer, _) = cell.Open("W", FileOpenMode.IO);
        var writable = Store(writer, "W").Counters;
        writer.Close("W");
        Assert.Equal("00", writer.Status("W"));
        output.WriteLine($"OPEN after a CLOSE at {tag}: INPUT {input}; I-O {writable}");
        Assert.InRange(input.PagesRead, 3, 3 + 7);
        Assert.Equal(0, input.PagesWritten);
        Assert.Equal(0, input.Syncs);
        Assert.InRange(writable.PagesRead, 3, 3 + 7);
        Assert.InRange(writable.PagesWritten, 1, 8);
        Assert.Equal(2, writable.Syncs);
        return input.PagesRead;
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void A_load_is_linear_an_open_reads_the_header_and_a_random_read_reads_one_path(Order order, FileSharing sharing)
    {
        using var cell = new Cell(shape, order, sharing);

        // (a) The load of N through OPEN OUTPUT, then of 4N through OPEN I-O.
        var (loader, _) = cell.Open("L", FileOpenMode.Output);
        var atN = Load(loader, "L", cell, 0, N, window: 2_000, out int promotedAtN);
        int[] depthsAtN = Depths(Store(loader, "L"));
        loader.Close("L");
        Assert.Equal("00", loader.Status("L"));
        Assert.Equal(1, promotedAtN);   // R2's ceiling: the 4 097th statement after a promotion promotes first

        // (b) at N.
        long openReadsAtN = OpenAfterClose(cell, "N");

        (loader, _) = cell.Open("L", FileOpenMode.IO);
        var at4N = Load(loader, "L", cell, N, 4 * N, window: 2_000, out _);
        int[] depthsAt4N = Depths(Store(loader, "L"));
        loader.Close("L");
        Assert.Equal("00", loader.Status("L"));
        Assert.Equal(depthsAtN[0] + 1, depthsAt4N[0]);   // the sizes straddle the prime tree's root split
        Assert.All(depthsAt4N.Zip(depthsAtN), d => Assert.InRange(d.First - d.Second, 0, 1));
        // What may grow between the sizes: the trees' depth; the ledger, which the checkpoint bounds (Load asserts it); and
        // the FREE batch (≤ 2), which a load meets once a promotion has freed pages. Nothing that grows with the file.
        int growth = depthsAt4N.Zip(depthsAtN, (big, small) => big - small).Sum();
        double ledgerGrowth = Math.Max(0, at4N.MeanLedgerPages - atN.MeanLedgerPages);
        Assert.True(at4N.MeanWritten <= atN.MeanWritten + growth + ledgerGrowth + FreeBatchPages + WindowNoise,
            $"a WRITE at 4N wrote {at4N.MeanWritten:0.###} pages on average, at N {atN.MeanWritten:0.###}: more than the depth growth {growth}, the ledger's {ledgerGrowth:0.##} and the FREE batch's {FreeBatchPages}");

        // (b) at 4N: the same bound, whatever N.
        long openReadsAt4N = OpenAfterClose(cell, "4N");
        Assert.InRange(openReadsAt4N, 3, openReadsAtN + 7);

        // (c) 1 000 random READs by key on the 4N store, each with the cache cold.
        var (reader, _) = cell.Open("R", FileOpenMode.Input);
        var store = Store(reader, "R");
        int entry = cell.Coherent ? 1 : 0;
        int[] depths = Depths(store);
        var random = new Random(1000);
        for (int i = 0; i < 1_000; i++)
        {
            int n = random.Next(1, 4 * N + 1);
            store.DropCache();
            var before = store.Counters;
            Assert.Equal("00", Read(reader, "R", cell, n, out string image));
            Assert.Equal(Rec(n), image);
            var cost = Since(store, before);
            Assert.Equal(depths[0] + entry, cost.PagesRead);
            Assert.Equal(0, cost.PagesWritten);
            Assert.Equal(0, cost.Syncs);
        }
        // By the unique alternate key: its own tree's path, then the prime tree's (the record lives in the prime tree). The
        // alternate tree holds (key value, release ordinal) composites (2.2.1.3), so the seek for a key value is a prefix
        // seek, which lands past the last entry of a leaf when the value's entry begins the next leaf: one more page then.
        if (cell.AlternateKeys > 0)
        {
            int crossed = 0;
            for (int i = 0; i < 1_000; i++)
            {
                int n = random.Next(1, 4 * N + 1);
                store.DropCache();
                var before = store.Counters;
                Assert.Equal("00", Read(reader, "R", cell, n, out string image, key: 2));
                Assert.Equal(Rec(n), image);
                var cost = Since(store, before);
                Assert.InRange(cost.PagesRead, depths[2] + depths[0] + entry, depths[2] + depths[0] + entry + 1);
                if (cost.PagesRead > depths[2] + depths[0] + entry) crossed++;
                Assert.Equal(0, cost.PagesWritten);
            }
            output.WriteLine($"{shape}/{order}/{sharing}: {crossed} of 1 000 READs by the unique alternate key crossed a leaf");
            Assert.InRange(crossed, 0, 100);   // only the values that begin a leaf: a few in a hundred
        }
        reader.Close("R");
        Assert.Equal("00", reader.Status("R"));
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void A_read_next_walk_reads_each_page_once_and_the_end_answers_ten(Order order, FileSharing sharing)
    {
        using var cell = new Cell(shape, order, sharing);
        cell.Load(0, N);

        var (reader, _) = cell.Open("R", FileOpenMode.Input);
        var store = Store(reader, "R");
        int entry = cell.Coherent ? 1 : 0;
        int[] depths = Depths(store);
        int treePages = TreePages(store, store.Meta.PrimeRoot).Count;
        store.DropCache();
        var walkFrom = store.Counters;
        int[] held = [.. cell.Keys[..N].Order()];   // ascending key order, whatever the load order
        int seen = 0;
        string status;
        while (true)
        {
            var before = store.Counters;
            status = ReadNext(reader, "R", out string image);
            var cost = Since(store, before);
            Assert.InRange(cost.PagesRead, 0, 1 + depths[0] + entry);
            Assert.Equal(0, cost.PagesWritten);
            if (status != "00") break;
            Assert.Equal(Rec(held[seen++]), image);
        }
        Assert.Equal("10", status);   // the AT END condition: the AT END phrase's branch is taken, NOT AT END's is not
        Assert.Equal(N, seen);
        var walk = Since(store, walkFrom);
        output.WriteLine($"{shape}/{order}/{sharing}: READ NEXT walk of {N} records read {walk.PagesRead} pages; the tree has {treePages}");
        Assert.InRange(walk.PagesRead, treePages, treePages + (long)(N + 1) * entry);

        // Along the alternate key WITH DUPLICATES: its tree's walk and one prime lookup per record; '02' exactly when the
        // next record in that key's order carries the same value (§14.9.30.4 GR27 a)).
        if (cell.AlternateKeys > 0)
        {
            Assert.Equal("00", reader.StartIndexed("R", 0, ">=", Rec(97), StartKeyLength.OfWidth(8)));
            store.DropCache();
            var walked = new List<(string Status, string Image)>();
            for (int i = 0; i < 2_000; i++)
            {
                var before = store.Counters;
                walked.Add((ReadNext(reader, "R", out string image), image));
                var cost = Since(store, before);
                Assert.InRange(cost.PagesRead, 0, 1 + depths[1] + depths[0] + entry);
                Assert.Equal(0, cost.PagesWritten);
            }
            for (int i = 0; i + 1 < walked.Count; i++)
                Assert.Equal(walked[i].Image[8..16] == walked[i + 1].Image[8..16] ? "02" : "00", walked[i].Status);
        }
        reader.Close("R");
        Assert.Equal("00", reader.Status("R"));
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void An_exception_path_costs_no_more_than_the_success_and_writes_nothing_when_it_changes_nothing(Order order,
        FileSharing sharing)
    {
        using var cell = new Cell(shape, order, sharing);
        cell.Load(0, N);
        var (writer, view) = cell.Open("W", FileOpenMode.IO);
        var store = Store(writer, "W");
        int depth = Depths(store).Max();
        int entry = cell.Coherent ? 1 : 0;
        int readBound = (cell.AlternateKeys + 1) * depth + entry;

        // A WRITE of a record whose key the file holds: '22', the INVALID KEY branch, and nothing written.
        var before = store.Counters;
        Assert.Equal("22", Write(writer, "W", cell, cell.Keys[7]));
        var cost = Since(store, before);
        Assert.Equal(0, cost.PagesWritten);
        Assert.Equal(0, cost.Syncs);
        Assert.InRange(cost.PagesRead, 0, readBound);
        if (cell.AlternateKeys > 0)
        {
            // A new prime key whose UNIQUE alternate key another record holds: '22' too, and still nothing written.
            int n = 4 * N + 1;
            string clash = $"{n:D8}{n % 97:D8}{99_999_999 - cell.Keys[7]:D8}";
            before = store.Counters;
            Assert.Equal("22", Write(writer, "W", cell, n, clash));
            cost = Since(store, before);
            Assert.Equal(0, cost.PagesWritten);
            Assert.InRange(cost.PagesRead, 0, readBound);
        }

        // A READ and a DELETE of a record the file does not hold: '23', the INVALID KEY branch, nothing written.
        int absent = 4 * N + 2;
        store.DropCache();
        before = store.Counters;
        Assert.Equal("23", Read(writer, "W", cell, absent, out _));
        cost = Since(store, before);
        Assert.Equal(0, cost.PagesWritten);
        Assert.Equal(Depths(store)[0] + entry, cost.PagesRead);
        before = store.Counters;
        Assert.Equal("23", Delete(writer, "W", cell, absent));
        cost = Since(store, before);
        Assert.Equal(0, cost.PagesWritten);
        Assert.Equal(0, cost.Syncs);

        // A WRITE the medium refuses at its first page write: '30' (§9.1.13.6 1)); the CLOSE ends the permanent error
        // (DOC-A.1-105's technique). Its replay on every verb is A_replayed_permanent_error_touches_nothing.
        view.RefuseWrite = _ => new IOException("the medium is full");
        Assert.Equal("30", Write(writer, "W", cell, cell.Keys[N]));
        view.RefuseWrite = null;
        writer.Close("W");
        Assert.Equal("00", writer.Status("W"));

        // The next run unit's WRITE is within its row, and the file holds its record and not the refused one.
        (writer, _) = cell.Open("W", FileOpenMode.IO);
        store = Store(writer, "W");
        before = store.Counters;
        Assert.Equal(cell.Written(cell.Keys[N + 1]), Write(writer, "W", cell, cell.Keys[N + 1]));
        cost = Since(store, before);
        int[] depths = Depths(store);
        int ledgerPages = LedgerPages(store);
        output.WriteLine($"{shape}/{order}/{sharing}: the WRITE after the refusal: {cost}");
        Assert.InRange(cost.PagesWritten, 1, WriteBound(cell, depths.Max(), ledgerPages) + depths.Sum());
        Assert.Equal(0, cost.Syncs);
        writer.Close("W");
        Assert.Equal("00", writer.Status("W"));
        var (reader, _) = cell.Open("R", FileOpenMode.Input);
        Assert.Equal("00", Read(reader, "R", cell, cell.Keys[N + 1], out string kept));
        Assert.Equal(Rec(cell.Keys[N + 1]), kept);
        Assert.Equal("23", Read(reader, "R", cell, cell.Keys[N], out _));
        reader.Close("R");
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void An_open_after_a_process_death_reads_the_header_and_the_fresh_pages_and_keeps_the_completed_statements(
        Order order, FileSharing sharing)
    {
        using var cell = new Cell(shape, order, sharing);
        cell.Load(0, N);

        // A run unit WRITEs 500 records and dies: no CLOSE reaches the file (its handle and locks go with it).
        var (dying, view) = cell.Open("D", FileOpenMode.IO);
        for (int i = N; i < N + 500; i++) Assert.Equal(cell.Written(cell.Keys[i]), Write(dying, "D", cell, cell.Keys[i]));
        view.Die();
        dying.Close("D");

        var (reader, _) = cell.Open("R", FileOpenMode.Input);
        var store = Store(reader, "R");
        var open = store.Counters;
        Assert.True(store.Adoption.WorkAdopted, "the dead run unit's verified DESCENDANT is adopted (2.2.1.6 R6)");
        var verified = TreePages(store, store.Meta.LedgerRoot);
        verified.UnionWith(store.LedgerEntries.Where(e => e.Value.State == LedgerState.Fresh).Select(e => e.Key));
        output.WriteLine($"{shape}/{order}/{sharing}: OPEN INPUT after a death: {open}; the ledger's pages and its fresh pages: {verified.Count}");
        Assert.InRange(open.PagesRead, 3 + verified.Count, 3 + verified.Count + 2);
        Assert.InRange(TreePages(store, store.Meta.LedgerRoot).Count, 1, LedgerPagesAtCheckpoint);
        Assert.Equal(0, open.PagesWritten);
        Assert.Equal(0, open.Syncs);
        // The fresh pages it verified are bounded by R2's checkpoint plus the one statement that began at it (2.2.1.11's
        // OPEN row): PagedFile.Begin promotes only a ledger that already lists more than LedgerCheckpoint.
        Assert.InRange(store.LedgerEntries.Count(e => e.Value.State == LedgerState.Fresh), 1,
            PagedFile.LedgerCheckpoint + MostPagesOfOneStatement(cell, Depths(store).Max()));
        foreach (int i in new[] { N, N + 1, N + 250, N + 499 })
        {
            Assert.Equal("00", Read(reader, "R", cell, cell.Keys[i], out string image));
            Assert.Equal(Rec(cell.Keys[i]), image);
        }
        reader.Close("R");

        // The writable OPEN fences: it reads the same, and writes the fence's claim, slot and retirement.
        var (writer, _) = cell.Open("W", FileOpenMode.IO);
        var fenced = Store(writer, "W").Counters;
        output.WriteLine($"{shape}/{order}/{sharing}: OPEN I-O after a death: {fenced}");
        Assert.InRange(fenced.PagesRead, 3 + verified.Count, 3 + verified.Count + 2 + 3);
        Assert.InRange(fenced.PagesWritten, 1, 8);
        Assert.Equal(2, fenced.Syncs);
        writer.Close("W");
        Assert.Equal("00", writer.Status("W"));
    }

    /// <summary>The statements a replayed permanent error is witnessed on, one case each.</summary>
    public enum Verb { Write, Read, ReadNext, Start, Rewrite, Delete }

    /// <summary>Each verb in each sharing mode: under <c>NO OTHER</c> no statement enters the store, under <c>ALL OTHER</c>
    /// every other statement is a coherent entry under the store mutex (2.2.1.7), and the replay is answered before it.</summary>
    public static TheoryData<FileSharing, Verb> ReplayCells()
    {
        var data = new TheoryData<FileSharing, Verb>();
        foreach (var sharing in new[] { FileSharing.NoOther, FileSharing.AllOther })
            foreach (var verb in Enum.GetValues<Verb>())
                data.Add(sharing, verb);
        return data;
    }

    /// <summary>⛔ A REPLAYED PERMANENT ERROR TOUCHES NOTHING. After a WRITE the medium refuses ('30', §9.1.13.6 1)),
    /// §9.1.13.1 keeps the condition <i>"in effect for all subsequent input-output operations on the file"</i>, and the
    /// technique this processor provides (docs/CONFORMANCE.md DOC-A.1-105) says such a statement <i>"does not touch the
    /// physical file, it sets the same I-O status again"</i>: the verb answers '30', with the page cache cold, reading,
    /// writing and syncing nothing — not the coherent entry's page 2, so it takes no store mutex either
    /// (<c>KeyedConnector.BeginStatement</c>; 2.2.1.11's unsuccessful row) — and the file is as it was: the CLOSE ends the
    /// condition, the record the verb named is unchanged, and the refused record is absent.</summary>
    [Theory]
    [MemberData(nameof(ReplayCells))]
    public void A_replayed_permanent_error_touches_nothing(FileSharing sharing, Verb verb)
    {
        const int Held = 200;
        using var cell = new Cell(shape, Order.Ascending, sharing);
        cell.Load(0, Held);
        var (writer, view) = cell.Open("W", FileOpenMode.IO);
        var store = Store(writer, "W");
        view.RefuseWrite = _ => new IOException("the medium is full");
        Assert.Equal("30", Write(writer, "W", cell, cell.Keys[Held]));
        view.RefuseWrite = null;

        int named = cell.Keys[3];
        store.DropCache();
        var before = store.Counters;
        string status = verb switch
        {
            Verb.Write => Write(writer, "W", cell, cell.Keys[Held + 1]),
            Verb.Read => Read(writer, "W", cell, named, out _),
            Verb.ReadNext => ReadNext(writer, "W", out _),
            Verb.Start => cell.Shape == Shape.Relative
                ? writer.StartRelative("W", ">=", named)
                : writer.StartIndexed("W", -1, ">=", Rec(named), StartKeyLength.OfWidth(8)),
            Verb.Rewrite => Rewrite(writer, "W", cell, named),
            Verb.Delete => Delete(writer, "W", cell, named),
            _ => throw new ArgumentOutOfRangeException(nameof(verb)),
        };
        Assert.Equal("30", status);
        Assert.Equal("30", writer.Status("W"));
        Assert.Equal(new IoCounters(0, 0, 0), Since(store, before));
        writer.Close("W");
        Assert.Equal("00", writer.Status("W"));

        var (reader, _) = cell.Open("R", FileOpenMode.Input);
        Assert.Equal("00", Read(reader, "R", cell, named, out string image));
        Assert.Equal(Rec(named), image);
        Assert.Equal("23", Read(reader, "R", cell, cell.Keys[Held], out _));
        Assert.Equal("23", Read(reader, "R", cell, cell.Keys[Held + 1], out _));
        reader.Close("R");
        Assert.Equal("00", reader.Status("R"));
    }

    /// <summary>Record n with its alternate key WITH DUPLICATES moved (the prime and the unique key kept): a REWRITE that
    /// changes the record's bytes and, under two alternate keys, one alternate tree.</summary>
    private static string Changed(int n) => $"{n:D8}{(n + 1) % 97:D8}{99_999_999 - n:D8}";

    /// <summary>⛔ WITNESS (e) OF 2.2.1.11 (kb/Work PB3337): A TRANSACTION COSTS ITS OWN RECORDS ONLY. Run unit A under commit
    /// control REWRITEs m records of the loaded file and B, beside it, REWRITEs others. A's ROLLBACK writes at most
    /// m × ((k + 1) × d) + the HOLDINGS pages its entries sit on + the statement's own bookkeeping (the WRITE row's ledger
    /// path copy, FREE batch, page 2 and claim, which one statement pays once) — never a cost that grows with the file —
    /// and leaves every record of B's byte-identical (§14.9.36.4 GR3, the run unit's OWN changes). A's COMMIT after m more
    /// changes reads its UNDO tree once (u + its leaves: one walk lists every R and V holding, finding R5-2) besides the
    /// promotion's own reads, never a page per holding.</summary>
    [Fact]
    public void A_transaction_costs_its_own_records_only()
    {
        const int M = 40;
        using var cell = new Cell(shape, Order.Random, FileSharing.AllOther);
        cell.Load(0, N);
        var (a, _) = cell.Open("A", FileOpenMode.IO, commit: true);
        var (b, _) = cell.Open("B", FileOpenMode.IO);
        int[] ours = cell.Keys[100..(100 + M)], theirs = cell.Keys[200..220];
        // A REWRITE whose alternate key WITH DUPLICATES meets another record's value answers '02' (§9.1.13.2 2)).
        foreach (int n in theirs) Assert.Matches("^0[02]$", Rewrite(b, "B", cell, n, Changed(n)));
        foreach (int n in ours) Assert.Matches("^0[02]$", Rewrite(a, "A", cell, n, Changed(n)));

        var store = Store(a, "A");
        int depth = Depths(store).Max(), holdings = TreePages(store, store.Meta.HoldingsRoot).Count;
        int bound = M * ((cell.AlternateKeys + 1) * depth) + holdings + WriteBound(cell, depth: 0, LedgerPages(store));
        var before = store.Counters;
        Assert.Null(a.Rollback());
        var cost = Since(store, before);
        output.WriteLine($"{shape}: ROLLBACK of {M} records wrote {cost.PagesWritten} pages (d {depth}, holdings {holdings}, bound {bound})");
        Assert.InRange(cost.PagesWritten, 1, bound);
        Assert.Equal(0, cost.Syncs);
        foreach (int n in ours)
        {
            Assert.Equal("00", Read(b, "B", cell, n, out string image));
            Assert.Equal(Rec(n), image);
        }
        foreach (int n in theirs)
        {
            Assert.Equal("00", Read(b, "B", cell, n, out string image));
            Assert.Equal(Changed(n), image);
        }

        foreach (int n in ours) Assert.Matches("^0[02]$", Rewrite(a, "A", cell, n, Changed(n)));
        int undo = TransactionTrees.UndoRoots(store).Sum(root => TreePages(store, root).Count);
        holdings = TreePages(store, store.Meta.HoldingsRoot).Count;
        int ledger = LedgerPages(store);
        store.DropCache();
        before = store.Counters;
        Assert.Null(a.Commit());
        cost = Since(store, before);
        // page 2 + the TRANSACTIONS tree (at most 2) + the UNDO tree once + the HOLDINGS pages the retirement edits + the
        // promotion's reads (both slots, the ledger, the FREE tree's path: 2.2.1.11's promotion row).
        int reads = 1 + 2 + undo + holdings + 2 + ledger + 3;
        output.WriteLine($"{shape}: COMMIT of {M} records read {cost.PagesRead} pages (UNDO {undo}, holdings {holdings}, bound {reads})");
        Assert.InRange(cost.PagesRead, undo, reads);
        a.Close("A");
        b.Close("B");
        Assert.Equal("00", a.Status("A"));
        Assert.Equal("00", b.Status("B"));
    }

    [Fact]
    public void A_read_that_meets_another_run_units_lock_answers_fifty_one_and_a_retry_repeats_only_the_bounded_statement()
    {
        using var cell = new Cell(shape, Order.Random, FileSharing.AllOther);
        cell.Load(0, N);

        var (a, _) = cell.Open("A", FileOpenMode.IO);
        var (b, bView) = cell.Open("B", FileOpenMode.IO);
        int n = cell.Keys[123];
        Assert.Equal("00", Read(a, "A", cell, n, out _, phrase: FileRecordLock.WithLock));
        var store = Store(b, "B");
        int bound = Depths(store)[0] + 1;

        store.DropCache();
        var before = store.Counters;
        Assert.Equal("51", Read(b, "B", cell, n, out _));
        var cost = Since(store, before);
        Assert.InRange(cost.PagesRead, 0, bound);
        Assert.Equal(0, cost.PagesWritten);

        var pauses = new List<TimeSpan>();
        FileRegistry.PauseObserver.Value = pauses.Add;
        try
        {
            // RETRY 3 TIMES: three more attempts after the first, each the same bounded statement, then '51'.
            before = store.Counters;
            Assert.Equal("51", Read(b, "B", cell, n, out _, retry: FileRetryKind.Times, retryAmount: 3));
            cost = Since(store, before);
            Assert.Equal(3, pauses.Count);
            Assert.InRange(cost.PagesRead, 0, 4 * bound);
            Assert.Equal(0, cost.PagesWritten);

            // The lock is released during the second wait: the next attempt reads the record.
            pauses.Clear();
            FileRegistry.PauseObserver.Value = t =>
            {
                pauses.Add(t);
                if (pauses.Count == 2) a.Unlock("A", records: true);
            };
            Assert.Equal("00", Read(b, "B", cell, n, out string image, retry: FileRetryKind.Times, retryAmount: 3));
            Assert.Equal(Rec(n), image);
            Assert.Equal(2, pauses.Count);

            // A permanent error in effect OUTRANKS the lock (§9.1.13.1; DOC-A.1-105, "The condition outranks every other
            // status the later statement would have taken"): with A holding n again, B's medium refuses a WRITE ('30'), and
            // B's READ and DELETE of n then answer '30', not '51' — no RETRY pause, no page read.
            b.Unlock("B", records: true);
            Assert.Equal("00", Read(a, "A", cell, n, out _, phrase: FileRecordLock.WithLock));
            bView.RefuseWrite = _ => new IOException("the medium is full");
            Assert.Equal("30", Write(b, "B", cell, cell.Keys[N]));
            bView.RefuseWrite = null;
            pauses.Clear();
            FileRegistry.PauseObserver.Value = pauses.Add;
            before = store.Counters;
            Assert.Equal("30", Read(b, "B", cell, n, out _, retry: FileRetryKind.Times, retryAmount: 3));
            Assert.Equal("30", Delete(b, "B", cell, n, retry: FileRetryKind.Times, retryAmount: 3));
            Assert.Empty(pauses);
            Assert.Equal(new IoCounters(0, 0, 0), Since(store, before));
        }
        finally { FileRegistry.PauseObserver.Value = null; }
        b.Close("B");
        a.Close("A");
        Assert.Equal("00", a.Status("A"));
        Assert.Equal("00", b.Status("B"));
    }
}

/// <summary>2.2.1.11's witnesses on a RELATIVE file (one tree keyed by the relative record number).</summary>
public sealed class RelativeStorePerformanceWitnessTests(ITestOutputHelper output)
    : KeyedStorePerformanceWitnessTests(Shape.Relative, output);

/// <summary>2.2.1.11's witnesses on an INDEXED file with no alternate key.</summary>
public sealed class IndexedStorePerformanceWitnessTests(ITestOutputHelper output)
    : KeyedStorePerformanceWitnessTests(Shape.Indexed, output);

/// <summary>2.2.1.11's witnesses on an INDEXED file with two alternate keys, one WITH DUPLICATES and one unique.</summary>
public sealed class IndexedTwoAlternatesStorePerformanceWitnessTests(ITestOutputHelper output)
    : KeyedStorePerformanceWitnessTests(Shape.IndexedTwoAlternates, output);
