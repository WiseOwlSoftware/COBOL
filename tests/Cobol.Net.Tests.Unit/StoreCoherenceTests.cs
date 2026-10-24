// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using CobolNet.Tests.Unit.Store;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ COHERENCE ACROSS RUN UNITS AT PAGE GRANULARITY, THROUGH THE CONNECTORS (DESIGN-runtime-library.md 2.2.1.7, in
/// docs/rearchitecture; kb/Work PB3335). Within a run unit every connector over a physical file attaches to ONE store
/// (<see cref="StoreTable"/>), so a sibling's change is visible at once; across run units the FILE is the truth, and a
/// coherent statement learns of another run unit's change by reading ONE page — page 2, the work meta — and, when its
/// generation moved, adopts page 2 and DROPS the page cache instead of reloading anything. ISO §9.1.15 3), <i>"The sharing
/// with all other mode allows concurrent access to a physical file through other file connectors specifying input, I-O, or
/// extend mode"</i>; §9.1.15 2), <i>"restricts concurrent access to a physical file through file connectors other than this
/// one, to input mode"</i>. Every case runs both organizations, and every expected value is the design text's or a general
/// rule's, never what the engine happens to do:
/// <list type="bullet">
///   <item>2.2.1.17, "the connectors' coherence": a sibling's change
///   seen at its next statement with no page read but page 2; another run unit's change adopted at the next coherent entry
///   with the cache dropped — the statement after it reads exactly what a COLD statement reads (1 + the tree's depth), and a
///   statement with no foreign change reads page 2 alone; never the file.</item>
///   <item>A reader's entry writes nothing (2.2.1.7, <i>"an INPUT connector ignores the mint and the flag, which concern
///   writers only"</i>): an <c>OPEN INPUT</c> connector whose run unit's store a writable sibling holds meets a PROMOTING
///   flag another run unit's refused CLOSE left on page 2; its statement holds the mutex SHARED, so it leaves the flag to
///   the sibling's next statement, which holds it exclusively and resolves it.</item>
///   <item>Every adoption runs under the store mutex: the reader's re-adoption when the run unit's last writable connector
///   closes reads pages 0 to 2 only while the mutex is held, on the machine's region host and on the lock-file host.</item>
///   <item>2.2.1.12's last R6 row: an <c>INPUT SHARING WITH ALL OTHER</c> reader that opened a crashed file (page 2 torn,
///   rejected) reads the committed tree, statement after statement, until a writable OPEN in another run unit fences the
///   file; from then on it reads page 2's tree, and the crashed statement's record is in neither.</item>
///   <item>PB3330's quadratic case: two run units loading one file through <c>SHARING WITH ALL OTHER</c>, every statement
///   meeting the other's change, write a bounded page count per statement however large the file grows.</item>
///   <item>The predicate each engine asks follows the engine (2.2.1.13, finding R2-7): the keyed store asks
///   <see cref="FileLockPosture.AdmitsAnotherRunUnit"/>; so does the sequential release's flush (kb/Work PB1587,
///   §14.9.51.4 GR12), so a <c>SHARING WITH READ ONLY</c> sequential writer hands each record over at its WRITE, and only
///   <c>SHARING WITH NO OTHER</c> buffers to the CLOSE; the sequential organization asks
///   <see cref="FileLockPosture.AdmitsAnotherWriter"/> only for what another WRITER may do (its append stream, its
///   ordinal, its reader's re-anchoring).</item>
/// </list>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class StoreCoherenceTests
{
    /// <summary>Run <paramref name="open"/> with every store it makes over a <see cref="RefusingPageMedium"/>, and return the
    /// last one made (the run unit's medium on the file).</summary>
    private static RefusingPageMedium Observed(Action open, Func<uint, Exception?>? write = null, Func<uint, Exception?>? read = null,
        Func<Exception?>? sync = null)
    {
        RefusingPageMedium? medium = null;
        KeyedStore.MediumObserver.Value = inner => medium = new RefusingPageMedium(inner, write, sync, read);
        try { open(); }
        finally { KeyedStore.MediumObserver.Value = null; }
        return medium!;
    }

    public static TheoryData<FileOrg> Orgs() => new() { FileOrg.Relative, FileOrg.Indexed };

    /// <summary>2.2.1.17's row for the connectors' coherence. A has two connectors on the file (A1 I-O, A2 INPUT), B one
    /// (I-O), every one <c>SHARING WITH ALL OTHER</c>. A1's WRITE is visible to A2's next READ, which reads page 2 and
    /// nothing else — the run unit's one store holds the trees and the pages. B's REWRITE is adopted at the next coherent
    /// entry: page 2's generation moved, so the store adopts it and drops the page cache. A third run unit Q, whose only
    /// connector is INPUT, then reads exactly what a COLD READ reads (the first READ of a fresh run unit: page 2 and the
    /// tree's path, 1 + d); A's store is writable (A1), so its adoption also takes the LEDGER that page 2 names, because a
    /// writer allocates from it (2.2.1.7, "its roots and its ledger replace the ones in memory"; the ledger's path is at
    /// most 2 pages, 2.2.1.11). However large the file, never the file; and the READ after, with no foreign change, is
    /// page 2 alone again.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_siblings_change_is_seen_at_once_and_another_run_units_is_adopted_with_the_cache_dropped(FileOrg org)
    {
        string host = Tmp($"adopt-{org}");
        try
        {
            const int records = 9000;    // a tree several levels deep, a hundred pages and more
            SeedRecords(host, org, records);
            long filePages = new FileInfo(host).Length / PageHeader.PageSize;

            // The COLD cost of one READ by key: a fresh run unit's first statement after its OPEN.
            var cold = new FileRegistry();
            Register(cold, "C", host, org);
            var coldMedium = Observed(() => cold.OpenStatic("C", FileOpenMode.Input));
            int beforeCold = coldMedium.Reads;
            Assert.Equal("00", Read(cold, "C", org, 2, out _));
            int coldRead = coldMedium.Reads - beforeCold;
            cold.Close("C");
            Assert.InRange(coldRead, 2, 6);            // page 2 + the path: 1 + depth
            Assert.True(filePages > 10 * coldRead, $"the file ({filePages} pages) dwarfs one READ's {coldRead}");

            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A1", host, org);
            Register(a, "A2", host, org);
            Register(b, "B", host, org);
            var aMedium = Observed(() => a.OpenStatic("A1", FileOpenMode.IO));
            a.OpenStatic("A2", FileOpenMode.Input);
            b.OpenStatic("B", FileOpenMode.IO);
            var q = new FileRegistry();
            Register(q, "Q", host, org);
            var qMedium = Observed(() => q.OpenStatic("Q", FileOpenMode.Input));
            Assert.Equal("00", a.Status("A1"));
            Assert.Equal("00", a.Status("A2"));
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", q.Status("Q"));
            Assert.Equal("00", Read(a, "A2", org, 2, out _));   // warms the path
            Assert.Equal("00", Read(q, "Q", org, 2, out _));

            // A sibling's change: visible at once, read through the one store — page 2 and nothing else.
            Assert.Equal("00", Rewrite(a, "A1", org, 2, "SIBL"));
            int before = aMedium.Reads;
            Assert.Equal("00", Read(a, "A2", org, 2, out string sibling));
            Assert.Equal(Rec(2, "SIBL"), sibling);
            Assert.Equal(1, aMedium.Reads - before);

            // Another run unit's change: adopted at the next coherent entry, the cache dropped — a cold READ's cost.
            Assert.Equal("00", Rewrite(b, "B", org, 2, "OTHR"));
            before = aMedium.Reads;
            Assert.Equal("00", Read(a, "A2", org, 2, out string adopted));
            Assert.Equal(Rec(2, "OTHR"), adopted);
            Assert.InRange(aMedium.Reads - before, coldRead, coldRead + 2);   // + the ledger's path, a writer's store
            int qBefore = qMedium.Reads;
            Assert.Equal("00", Read(q, "Q", org, 2, out string inQ));
            Assert.Equal(Rec(2, "OTHR"), inQ);
            Assert.Equal(coldRead, qMedium.Reads - qBefore);

            // Nothing moved since: page 2 alone.
            before = aMedium.Reads;
            Assert.Equal("00", Read(a, "A2", org, 2, out _));
            Assert.Equal(1, aMedium.Reads - before);

            b.Close("B");
            q.Close("Q");
            a.Close("A2");
            a.Close("A1");
            Assert.Equal("00", a.Status("A1"));
            Assert.Equal("00", b.Status("B"));
        }
        finally { TryDelete(host); }
    }

    /// <summary>PB3330's quadratic case, gone: two run units loading ONE file through <c>SHARING WITH ALL OTHER</c>, their
    /// WRITEs interleaved so every statement meets the other's change. Each statement adopts page 2 and writes only its own
    /// pages — at most d + 5 + 1 (2.2.1.11's relative row, and the indexed row with no alternate key: (0 + 1) × d + 6; d ≤ 2
    /// at 2 000 small records) — whatever the file's size: the last hundred statements cost
    /// what the hundred after the first thousand did, give or take the depth growth. A statement that promotes first (R2's
    /// ceiling) is the promotion row's and is told apart by its syncs. Every record of both run units is in the file.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Two_run_units_loading_one_file_write_a_bounded_page_count_per_statement(FileOrg org)
    {
        string host = Tmp($"load-{org}");
        try
        {
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A", host, org);
            Register(b, "B", host, org);
            DyingPageMedium? aMedium = null, bMedium = null;
            KeyedStore.MediumObserver.Value = inner => aMedium = new DyingPageMedium(inner);
            try { a.OpenStatic("A", FileOpenMode.Output); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", a.Status("A"));
            a.Close("A");
            KeyedStore.MediumObserver.Value = inner => aMedium = new DyingPageMedium(inner);
            try { a.OpenStatic("A", FileOpenMode.IO); }
            finally { KeyedStore.MediumObserver.Value = null; }
            KeyedStore.MediumObserver.Value = inner => bMedium = new DyingPageMedium(inner);
            try { b.OpenStatic("B", FileOpenMode.IO); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));

            var writes = new List<int>();
            for (int n = 1; n <= 2000; n++)
            {
                var (reg, name, medium) = n % 2 == 1 ? (a, "A", aMedium!) : (b, "B", bMedium!);
                int before = medium.Writes, synced = medium.Syncs;
                Assert.Equal("00", Write(reg, name, org, n, "LOAD"));
                if (medium.Syncs == synced) writes.Add(medium.Writes - before);
            }
            const int bound = 2 + 5 + 1;
            Assert.True(writes.Count > 1500, $"{writes.Count} statements promoted nothing");
            Assert.True(writes.Max() <= bound, $"a WRITE wrote {writes.Max()} pages (bound {bound})");
            Assert.True(writes[^100..].Average() <= writes[1000..1100].Average() + 1, "the per-statement cost grew with the file");

            b.Close("B");
            a.Close("A");
            Assert.Equal("00", a.Status("A"));
            Assert.Equal("00", b.Status("B"));
            var check = new FileRegistry();
            Register(check, "R", host, org, sharing: null);
            check.OpenStatic("R", FileOpenMode.Input);
            foreach (int n in new[] { 1, 2, 999, 1000, 1999, 2000 })
            {
                Assert.Equal("00", Read(check, "R", org, n, out string image));
                Assert.Equal(Rec(n, "LOAD"), image);
            }
            check.Close("R");
        }
        finally
        {
            KeyedStore.MediumObserver.Value = null;
            TryDelete(host);
        }
    }

    /// <summary>Page 2 as the file holds it now.</summary>
    private static MetaPage WorkMeta(string host)
    {
        var page = new byte[PageHeader.PageSize];
        using (var fs = new FileStream(host, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            fs.Position = MetaPage.WorkSlot * (long)PageHeader.PageSize;
            fs.ReadExactly(page);
        }
        return MetaPage.Decode(page, MetaPage.WorkSlot) ?? throw new InvalidOperationException("page 2 is torn");
    }

    /// <summary>A reader's entry writes nothing. B's CLOSE is refused at its committed-slot write, so its promotion answers
    /// '30' (§9.1.13.6 1), <i>"A permanent error exists and no further information is available concerning the
    /// input-output operation"</i>) and leaves PROMOTING set on page 2 (2.2.1.6 R5). In run unit A, whose store is writable
    /// because its I-O connector A1 is open, the INPUT connector A2 READs: it holds the mutex shared, so it adopts B's
    /// record but writes NOTHING and leaves the flag set. A1's next READ holds the mutex exclusively and resolves it (R5's
    /// restart rule): the promotion's writes are its, and page 2's flag is clear after it.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_readers_entry_leaves_a_promoting_flag_to_the_next_writers_entry(FileOrg org)
    {
        string host = Tmp($"promoting-{org}");
        try
        {
            SeedRecords(host, org, 2);
            var a = new FileRegistry();
            var b = new FileRegistry();
            Register(a, "A1", host, org);
            Register(a, "A2", host, org);
            Register(b, "B", host, org);
            var aMedium = Observed(() => a.OpenStatic("A1", FileOpenMode.IO));
            a.OpenStatic("A2", FileOpenMode.Input);
            bool refuseSlots = false;
            Observed(() => b.OpenStatic("B", FileOpenMode.IO),
                write: page => refuseSlots && page is MetaPage.SlotA or MetaPage.SlotB ? new IOException("refused (test)") : null);
            Assert.Equal("00", b.Status("B"));

            Assert.Equal("00", Write(b, "B", org, 3, "CCCC"));
            refuseSlots = true;
            b.Close("B");
            Assert.Equal("30", b.Status("B"));
            Assert.True(WorkMeta(host).Promoting, "B's refused promotion leaves PROMOTING on page 2");

            int before = aMedium.Writes;
            Assert.Equal("00", Read(a, "A2", org, 3, out string seen));
            Assert.Equal(Rec(3, "CCCC"), seen);          // B's completed WRITE, adopted
            Assert.Equal(before, aMedium.Writes);         // a reader's entry writes nothing
            Assert.True(WorkMeta(host).Promoting, "the reader left the flag to a writer's entry");

            Assert.Equal("00", Read(a, "A1", org, 3, out _));
            Assert.True(aMedium.Writes > before, "the writer's entry resolved the flag");
            Assert.False(WorkMeta(host).Promoting);

            a.Close("A2");
            a.Close("A1");
            Assert.Equal("00", a.Status("A1"));
            var after = new FileRegistry();
            Register(after, "R", host, org, sharing: null);
            after.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal("00", Read(after, "R", org, 3, out string kept));
            Assert.Equal(Rec(3, "CCCC"), kept);
            after.Close("R");
        }
        finally { TryDelete(host); }
    }

    public static TheoryData<FileOrg, RegionHostKind> OrgsAndHosts()
    {
        var data = new TheoryData<FileOrg, RegionHostKind>();
        foreach (var org in FileHarness.KeyedOrgs)
            foreach (var host in Enum.GetValues<RegionHostKind>())
                data.Add(org, host);
        return data;
    }

    /// <summary>Every adoption runs under the store mutex (2.2.1.7). When A's last writable connector (A1) closes and its
    /// INPUT sibling (A2) stays, A's store is opened again as the reader's — R6 in memory, reading pages 0 to 2 — and
    /// every page read of that CLOSE is made while the mutex is held, as another run unit asking the host sees it; so is
    /// every read of a coherent OPEN. A2 then reads the file as A1 left it.</summary>
    [Theory]
    [MemberData(nameof(OrgsAndHosts))]
    public void The_readers_re_adoption_at_the_writers_close_runs_under_the_store_mutex(FileOrg org, RegionHostKind regionHost)
    {
        using var forced = RegionHostSeam.Use(regionHost);
        string host = Tmp($"detach-{org}");
        try
        {
            SeedRecords(host, org, 2);
            var a = new FileRegistry();
            Register(a, "A1", host, org);
            Register(a, "A2", host, org);
            bool watching = false;
            var unheld = new List<uint>();
            int watched = 0;
            Exception? Probe(uint page)
            {
                if (!watching) return null;
                watched++;
                if (!MutexHeld(host, writable: true)) unheld.Add(page);
                return null;
            }
            watching = true;
            Observed(() => a.OpenStatic("A1", FileOpenMode.IO), read: Probe);
            a.OpenStatic("A2", FileOpenMode.Input);
            watching = false;
            Assert.Equal("00", a.Status("A1"));
            Assert.Equal("00", Write(a, "A1", org, 3, "CCCC"));

            watching = true;
            a.Close("A1");
            watching = false;
            Assert.Equal("00", a.Status("A1"));
            Assert.True(watched > 0, "the CLOSE and the re-adoption read pages");
            Assert.True(unheld.Count == 0, "pages read outside the store mutex: " + string.Join(", ", unheld));

            Assert.Equal("00", Read(a, "A2", org, 3, out string image));
            Assert.Equal(Rec(3, "CCCC"), image);
            a.Close("A2");
        }
        finally { TryDelete(host); }
    }

    /// <summary>Tear page 2 of <paramref name="host"/>: one byte of its body flipped, so its checksum fails (2.2.1.6 R3).</summary>
    private static void TearWorkMeta(string host)
    {
        using var fs = new FileStream(host, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        fs.Position = (MetaPage.WorkSlot * (long)PageHeader.PageSize) + PageHeader.Bytes + 40;
        int b = fs.ReadByte();
        fs.Position--;
        fs.WriteByte((byte)(b ^ 0xFF));
    }

    /// <summary>2.2.1.12's last R6 row, through the connectors. D completes a WRITE of record 3 (page 2 names it) and dies
    /// before its CLOSE; page 2 is then torn, so the adoption rule rejects it. An <c>INPUT SHARING WITH ALL OTHER</c>
    /// reader R opens: it adopts the COMMITTED meta (records 1 and 2) and keeps it, statement after statement, while
    /// page 2 still holds the rejected slot — a torn page 2 under a live store leaves a reader on the tree it holds, never
    /// '30' (2.2.1.7; finding R2-5). Then E opens I-O in another run unit: its fence rewrites page 2 (the generation moves)
    /// and E WRITEs record 4. R's next statement adopts page 2's tree: record 4 is there, record 3 never was.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_input_reader_of_a_crashed_file_reads_the_committed_tree_until_a_writable_open_fences_it(FileOrg org)
    {
        string host = Tmp($"crashed-{org}");
        try
        {
            SeedRecords(host, org, 2);
            var d = new FileRegistry();
            Register(d, "D", host, org);
            DyingPageMedium? dying = null;
            KeyedStore.MediumObserver.Value = inner => dying = new DyingPageMedium(inner);
            try { d.OpenStatic("D", FileOpenMode.IO); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(d, "D", org, 3, "CCCC"));
            dying!.DieAfter = dying.Writes;   // the process dies here: nothing it hands over afterwards lands
            d.Close("D");                     // gives the dead run unit's handle back (the operating environment would)
            TearWorkMeta(host);

            var r = new FileRegistry();
            Register(r, "R", host, org);
            r.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal("00", r.Status("R"));
            for (int pass = 0; pass < 2; pass++)
            {
                Assert.Equal("00", Read(r, "R", org, 1, out string one));
                Assert.Equal(Rec(1, "AAAA"), one);
                Assert.Equal("23", Read(r, "R", org, 3, out _));   // the crashed statement's record: not in the committed tree
            }

            var e = new FileRegistry();
            Register(e, "E", host, org);
            e.OpenStatic("E", FileOpenMode.IO);
            Assert.Equal("00", e.Status("E"));
            Assert.Equal("00", Write(e, "E", org, 4, "DDDD"));

            Assert.Equal("00", Read(r, "R", org, 4, out string four));
            Assert.Equal(Rec(4, "DDDD"), four);
            Assert.Equal("23", Read(r, "R", org, 3, out _));
            Assert.Equal("00", Read(r, "R", org, 2, out string two));
            Assert.Equal(Rec(2, "AAAA"), two);

            e.Close("E");
            r.Close("R");
            Assert.Equal("00", e.Status("E"));
            Assert.Equal("00", r.Status("R"));
        }
        finally { TryDelete(host); }
    }

    /// <summary>Another run unit, D, over <paramref name="host"/>: it opens I-O, completes a WRITE of record 3 (page 2
    /// names it) and dies before its CLOSE; page 2 is then torn, as a death in the middle of its next page 2 write leaves
    /// it (2.2.1.16: "page 2 torn → FOREIGN → C"). Its committed meta C is its OPEN's fence: records 1 and 2.</summary>
    private static void DieOverTheFile(string host, FileOrg org)
    {
        var d = new FileRegistry();
        Register(d, "D", host, org);
        DyingPageMedium? dying = null;
        KeyedStore.MediumObserver.Value = inner => dying = new DyingPageMedium(inner);
        try { d.OpenStatic("D", FileOpenMode.IO); }
        finally { KeyedStore.MediumObserver.Value = null; }
        Assert.Equal("00", d.Status("D"));
        Assert.Equal("00", Write(d, "D", org, 3, "CCCC"));
        dying!.DieAfter = dying.Writes;   // the process dies here: nothing it hands over afterwards lands
        d.Close("D");                     // gives the dead run unit's handle back (the operating environment would)
        TearWorkMeta(host);
    }

    /// <summary>The READER is the entry, not the store (2.2.1.7; PB3335 review lead R1-L1). Run unit A has A1 I-O and A2
    /// INPUT open on the file, both <c>SHARING WITH ALL OTHER</c>, so A's one store is writable; D completes a WRITE and
    /// dies, and page 2 is torn. A2's READ holds the mutex SHARED and writes nothing: a page 2 that does not validate
    /// under a live store leaves a reader on the tree it holds, since nothing it can follow moved, so it answers '00' from
    /// records 1 and 2, as another run unit's reader would, statement after statement. A1's next statement is an
    /// EXCLUSIVE entry of a writable store, which cannot know what another run unit completed: '30' (§9.1.13.6 item 1,
    /// <i>"A permanent error exists and no further information is available concerning the input-output operation"</i>),
    /// which remains in effect for A1's later statements (§9.1.13.1, <i>"The permanent error condition remains in effect
    /// for all subsequent input-output operations on the file"</i>) and not for A2, whose file connector met none.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_input_readers_entry_beside_its_writable_sibling_keeps_its_tree_over_a_torn_page_2(FileOrg org)
    {
        string host = Tmp($"torn-sibling-{org}");
        try
        {
            SeedRecords(host, org, 2);
            var a = new FileRegistry();
            Register(a, "A1", host, org);
            Register(a, "A2", host, org);
            var aMedium = Observed(() => a.OpenStatic("A1", FileOpenMode.IO));
            a.OpenStatic("A2", FileOpenMode.Input);
            Assert.Equal("00", a.Status("A1"));
            Assert.Equal("00", a.Status("A2"));
            DieOverTheFile(host, org);

            int before = aMedium.Writes;
            for (int pass = 0; pass < 2; pass++)
            {
                Assert.Equal("00", Read(a, "A2", org, 1, out string one));
                Assert.Equal(Rec(1, "AAAA"), one);
                Assert.Equal("23", Read(a, "A2", org, 3, out _));   // D's record: never in the tree A holds
            }
            Assert.Equal(before, aMedium.Writes);                     // a reader's entry writes nothing

            Assert.Equal("30", Read(a, "A1", org, 1, out _));
            Assert.Equal("30", Read(a, "A1", org, 2, out _));        // §9.1.13.1: in effect for A1's later statements
            Assert.Equal("00", Read(a, "A2", org, 2, out string two));
            Assert.Equal(Rec(2, "AAAA"), two);

            a.Close("A2");
            a.Close("A1");
            Assert.Equal("00", a.Status("A2"));
        }
        finally { TryDelete(host); }
    }

    /// <summary>A page 2 the medium REFUSES to read is '30' to a reader's entry too (2.2.1.16's refused READ row: <i>"a
    /// refused read is no verdict on the page"</i>; §9.1.13.6 item 1; PB3335 review lead R1-L1), through the
    /// fault-injecting medium of 2.2.1.12. A2, the INPUT connector beside the writable sibling A1, READs while the host
    /// refuses page 2: '30', and the permanent error remains in effect for A2's next READ after the medium recovers
    /// (§9.1.13.1). A1's file connector met none: its READ and WRITE answer '00', and the next run unit reads the
    /// record A1 wrote.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_input_readers_entry_beside_its_writable_sibling_answers_30_to_a_refused_page_2_read(FileOrg org)
    {
        string host = Tmp($"refused-entry-{org}");
        try
        {
            SeedRecords(host, org, 2);
            var a = new FileRegistry();
            Register(a, "A1", host, org);
            Register(a, "A2", host, org);
            bool refusing = false;
            int refused = 0;
            Observed(() => a.OpenStatic("A1", FileOpenMode.IO), read: page =>
            {
                if (!refusing || page != MetaPage.WorkSlot) return null;
                refused++;
                return new IOException("refused by the host (test)");
            });
            a.OpenStatic("A2", FileOpenMode.Input);
            Assert.Equal("00", a.Status("A2"));

            refusing = true;
            Assert.Equal("30", Read(a, "A2", org, 1, out _));
            refusing = false;
            Assert.True(refused > 0, "the reader's entry read page 2, and the host refused it");
            Assert.Equal("30", Read(a, "A2", org, 2, out _));        // §9.1.13.1: in effect, nothing touched

            Assert.Equal("00", Read(a, "A1", org, 1, out string one));
            Assert.Equal(Rec(1, "AAAA"), one);
            Assert.Equal("00", Write(a, "A1", org, 3, "CCCC"));
            a.Close("A2");
            a.Close("A1");
            Assert.Equal("00", a.Status("A1"));

            var r = new FileRegistry();
            Register(r, "R", host, org, sharing: null);
            r.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal("00", Read(r, "R", org, 3, out string three));
            Assert.Equal(Rec(3, "CCCC"), three);
            r.Close("R");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The reader's re-adoption at the writer's CLOSE, REFUSED (PB3335 review lead R1-L2; 2.2.1.16's re-adoption
    /// row). A1 (I-O) closes while its INPUT sibling A2 stays, and the host refuses the reads of the re-adoption (2.2.1.7:
    /// pages 0 to 2): the CLOSE answers '30' (§9.1.13.6 item 1) and A1 is closed; A2 keeps reading the trees the store
    /// holds. The store's writable state no longer has a fence behind it: D then dies over the file and tears page 2,
    /// and A3's <c>OPEN I-O</c> in the same run unit must FENCE (2.2.1.6 R6: the torn page 2 is FOREIGN, the committed
    /// meta C is adopted) rather than take the store as a live writable one: its WRITE answers '00', D's uncommitted
    /// record is in no tree ('23'), and A2 reads A3's record after it.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_refused_re_adoption_at_the_writers_close_is_30_and_the_next_writable_open_fences(FileOrg org)
    {
        string host = Tmp($"refused-readopt-{org}");
        try
        {
            SeedRecords(host, org, 2);
            var a = new FileRegistry();
            Register(a, "A1", host, org);
            Register(a, "A2", host, org);
            Register(a, "A3", host, org);
            bool refusing = false;
            int refused = 0;
            var aMedium = Observed(() => a.OpenStatic("A1", FileOpenMode.IO), read: _ =>
            {
                if (!refusing) return null;
                refused++;
                return new IOException("refused by the host (test)");
            });
            a.OpenStatic("A2", FileOpenMode.Input);
            Assert.Equal("00", a.Status("A1"));
            Assert.Equal("00", a.Status("A2"));

            int writes = aMedium.Writes;
            refusing = true;
            a.Close("A1");                   // no statement since its OPEN's fence: the CLOSE only syncs, then re-adopts
            refusing = false;
            Assert.Equal("30", a.Status("A1"));
            Assert.True(refused > 0, "the re-adoption read pages, and the host refused them");
            Assert.Equal(writes, aMedium.Writes);   // no promotion ran: the '30' is the re-adoption's
            Assert.Equal("00", Read(a, "A2", org, 1, out string one));
            Assert.Equal(Rec(1, "AAAA"), one);

            DieOverTheFile(host, org);
            Assert.Equal("00", Read(a, "A2", org, 2, out string two));   // a reader keeps its tree over the torn page 2
            Assert.Equal(Rec(2, "AAAA"), two);

            a.OpenStatic("A3", FileOpenMode.IO);
            Assert.Equal("00", a.Status("A3"));
            Assert.True(aMedium.Writes > writes, "A3's OPEN fenced the file");
            Assert.Equal("00", Write(a, "A3", org, 4, "DDDD"));
            Assert.Equal("23", Read(a, "A3", org, 3, out _));   // D's crashed record: not in the committed tree
            Assert.Equal("00", Read(a, "A2", org, 4, out string four));
            Assert.Equal(Rec(4, "DDDD"), four);

            a.Close("A3");
            a.Close("A2");
            Assert.Equal("00", a.Status("A3"));
            Assert.Equal("00", a.Status("A2"));
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.13 (finding R2-7): the predicate each connector asks follows the question it answers. The keyed store
    /// writes at every statement, so its connector serializes against ANY other run unit
    /// (<see cref="FileLockPosture.AdmitsAnotherRunUnit"/>) and asks nothing else. The sequential organization asks two
    /// questions: its release (<c>SequentialConnector.ReleaseRecord</c>'s flush) asks about ANY other connector, a reader
    /// as well as a writer, through the ONE read <c>FileConnector.OtherConnectorsMayShare</c> (kb/Work PB670 review R1-1, which
    /// adds to the mode's answer the COBOL-85 rule that only an open sibling is owed; kb/Work PB1587: §14.9.51.4 GR12 releases the record to the operating environment at the WRITE,
    /// and §9.1.15 2) admits an INPUT reader beside a READ ONLY writer), while its shared append stream (its append
    /// position), its ordinal count (the release's and the <c>OPEN EXTEND</c>'s) and its reader's re-anchoring ask only
    /// about another WRITER (<see cref="FileLockPosture.AdmitsAnotherWriter"/>).
    /// Behaviourally: a <c>SHARING WITH NO OTHER</c> sequential writer keeps its record in its input-output area until the
    /// CLOSE, while a <c>SHARING WITH READ ONLY</c> or <c>SHARING WITH ALL OTHER</c> one hands each record over at its
    /// WRITE. The files are counted by the CLASS they declare, so a partial class's second file (the sequential connector's
    /// commit-control half, <c>SequentialConnector.CommitControl.cs</c>, kb/Work PB3340) is its class's: its kept handle asks
    /// <see cref="FileLockPosture.AdmitsAnotherRunUnit"/> as the keyed connector's does, and its journal's lock test asks
    /// <see cref="FileLockPosture.AdmitsAnotherWriter"/> as the shared append stream does.</summary>
    [Fact]
    public void The_keyed_store_asks_about_any_run_unit_and_the_sequential_release_about_any_other_connector()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        static bool Asks(string file, string predicate) => File.ReadAllLines(file)
            .Any(l => l.Contains($"FileLockPosture.{predicate}(", StringComparison.Ordinal)
                && !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
        static string ClassFile(string file) => Path.GetFileName(file).Split('.')[0] + ".cs";   // a partial class's files are its class's
        var runUnit = Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories)
            .Where(f => Asks(f, "AdmitsAnotherRunUnit")).Select(ClassFile).Distinct().Order(StringComparer.Ordinal).ToList();
        var writer = Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories)
            .Where(f => Asks(f, "AdmitsAnotherWriter")).Select(ClassFile).Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["FileConnector.cs", "KeyedConnector.cs", "SequentialConnector.cs"], runUnit);
        Assert.Equal(["FileSupport.cs", "SequentialConnector.cs"], writer);

        foreach (var (sharing, handedOver) in new[]
                 { (FileSharing.NoOther, false), (FileSharing.ReadOnly, true), (FileSharing.AllOther, true) })
        {
            string host = Tmp($"seq-{sharing}");
            try
            {
                var w = new FileRegistry();
                w.Register("W", host, recordWidth: 8, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
                w.RegisterSharing("W", sharing, FileLockMode.Manual, multiple: false);
                w.OpenStatic("W", FileOpenMode.Output);
                Assert.Equal("00", w.Status("W"));
                Assert.Equal("00", w.WriteShared("W", "0001AAAA", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
                Assert.Equal(handedOver, new FileInfo(host).Length > 0);
                w.Close("W");
                Assert.Equal(8, new FileInfo(host).Length);
            }
            finally { TryDelete(host); }
        }
    }
}
