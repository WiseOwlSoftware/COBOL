// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;
using Xunit;
using Fault = CobolNet.Tests.Unit.Store.MemoryPageMedium.Fault;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ A PROCESS DEATH, A POWER LOSS OR A REFUSED WRITE OR SYNC AT EVERY POINT OF EVERY PATH THE PAGE LAYER OWNS LEAVES THE
/// FILE AS THE LAST COMPLETED STATEMENT LEFT IT, NEVER TORN (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12's
/// adoption matrix, 2.2.1.16's crash points and refused-statement table, 2.2.1.17's interleavings; kb/Work PB3331).
/// <para>The paths: a changing statement (coherent and NO OTHER), growth, a promotion (CLOSE and the checkpoint), the
/// fence (a writable OPEN) and creation. Each is run once to count its operations (writes, syncs and the truncation) and
/// its reads, then once per operation with the run unit killed BEFORE it, killed AFTER it, refused AT it and — at a write
/// — refused because the file cannot grow further, and once per READ with the read refused; each run is then continued
/// by the next writer (another live run unit's statement), by a fresh OPEN, and by a power loss followed by an OPEN. Every
/// expected state is the design's bar: the records of the last COMPLETED statement — a statement completed exactly when
/// its page 2 completion was handed to the medium — or, after a power loss, of the last completed statement a sync made
/// durable. The rows whose paths later groups build (the record statements' locks, the transactions, the dead owner)
/// are <see cref="PendingStoreCases"/>.</para>
/// </summary>
public sealed class PagedFileCrashMatrixTests
{
    private const string Base = "1=one;2=two;";

    /// <summary>The scenario every point-loop starts from: a store holding two records, CLOSEd (so durable), then opened
    /// by A (the run unit under test) and — when A shares the file (every sharing mode but NO OTHER) — by B (a live
    /// writer) and R (a live INPUT reader). A NO OTHER connector has no other run unit beside it: Table 19 refuses every
    /// other opener (kb/Work PB833), so its scene has neither.</summary>
    private sealed record Scene(MemoryPageMedium Medium, MemoryPageMedium.Handle HandleA, PagedFile A, PagedFile? B, PagedFile? Reader,
        MemoryPageMedium.Handle? HandleB = null, MemoryPageMedium.Handle? HandleR = null);

    private static Scene NewScene(bool coherentA = true)
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "one"));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 2, "two", blobPages: 2));
        Assert.Equal(StoreStatus.Ok, store.Close());
        PagedFile? b = null, reader = null;
        MemoryPageMedium.Handle? hb = null, hr = null;
        if (coherentA)
        {
            (hb, var sb, b) = StoreKit.OpenAs(medium, "B");
            (hr, var sr, reader) = StoreKit.OpenAs(medium, "R", writable: false);
            Assert.Equal(StoreStatus.Ok, sb);
            Assert.Equal(StoreStatus.Ok, sr);
        }
        var (ha, sa, a) = StoreKit.OpenAs(medium, "A", coherent: coherentA);
        Assert.Equal(StoreStatus.Ok, sa);
        return new Scene(medium, ha, a!, b, reader, hb, hr);
    }

    /// <summary>The paths of the page layer, each a step A takes on the scene.</summary>
    public enum PagePath
    {
        CoherentStatement,
        NoOtherStatement,
        GrowingStatement,
        Close,
        Checkpoint,
        Fence,
    }

    private static StoreStatus Run(Scene s, PagePath path) => path switch
    {
        PagePath.CoherentStatement or PagePath.NoOtherStatement => StoreKit.Put(s.A, 3, "three"),
        PagePath.GrowingStatement => StoreKit.Put(s.A, 3, "three", blobPages: 6),
        PagePath.Close => s.A.Close(),
        PagePath.Checkpoint => s.A.Promote(),
        PagePath.Fence => PagedFile.Open(s.HandleA, writable: true, coherent: true, out _),
        _ => throw new ArgumentOutOfRangeException(nameof(path)),
    };

    private static bool Changes(PagePath path) =>
        path is PagePath.CoherentStatement or PagePath.NoOtherStatement or PagePath.GrowingStatement;

    private static List<string> DryRun(PagePath path)
    {
        var s = NewScene(coherentA: path != PagePath.NoOtherStatement);
        int before = s.HandleA.Operations.Count;
        Assert.Equal(StoreStatus.Ok, Run(s, path));
        return s.HandleA.Operations.Skip(before).ToList();
    }

    public static TheoryData<PagePath, string> PathsAndFaults()
    {
        var data = new TheoryData<PagePath, string>();
        foreach (var path in Enum.GetValues<PagePath>())
            foreach (var fault in new[] { Fault.Death, Fault.DeathAfter, Fault.Refuse, Fault.SizeLimit })
                data.Add(path, fault.ToString());
        return data;
    }

    /// <summary>2.2.1.16 for the page layer's paths, at EVERY operation: the next writer, a fresh OPEN and a power loss
    /// each find the last completed statement; the live reader never reads a torn tree; no stamp is ever reused. A write
    /// the host refuses because the file cannot grow further is the BOUNDARY at a changing statement (2.2.1.9, §9.1.13.5
    /// item 4's '24': the file unchanged) and '30' at an OPEN, a CLOSE and a promotion, which have no '24'.</summary>
    [Theory]
    [MemberData(nameof(PathsAndFaults))]
    public void Every_point_of_every_path_leaves_the_last_completed_statement(PagePath path, string faultName)
    {
        var fault = Enum.Parse<Fault>(faultName);
        var ops = DryRun(path);
        Assert.NotEmpty(ops);
        for (int n = 1; n <= ops.Count; n++)
            foreach (var continuation in new[] { "next-writer", "open", "power-loss" })
                OnePoint(path, fault, n, ops, continuation);
    }

    private static void OnePoint(PagePath path, Fault fault, int n, List<string> ops, string continuation)
    {
        string where = $"{path} {fault} at op {n} ({ops[n - 1]}) then {continuation}";
        if (fault == Fault.SizeLimit && !ops[n - 1].StartsWith('W')) return;   // a host answers EFBIG to a write only
        bool refused = fault is Fault.Refuse or Fault.SizeLimit;
        var s = NewScene(coherentA: path != PagePath.NoOtherStatement);
        s.HandleA.FaultAtOperation(n, fault);
        bool died = false;
        StoreStatus status = StoreStatus.Ok;
        try { status = Run(s, path); }
        catch (SimulatedDeath) { died = true; }
        if (refused)
            Assert.True((fault == Fault.SizeLimit && Changes(path) ? StoreStatus.Boundary : StoreStatus.PermanentError) == status, where);
        else Assert.True(died, where);
        // The statement completed exactly when its last operation — page 2's completion — was handed over.
        bool completed = Changes(path) && fault == Fault.DeathAfter && n == ops.Count;
        string expected = completed ? Base + "3=three;" : Base;

        // The live INPUT reader follows page 2 and never meets a torn tree.
        if (s.Reader is not null)
        {
            Assert.Equal(StoreStatus.Ok, s.Reader.Enter());
            Assert.Equal(expected, StoreKit.Show(s.Reader));
        }

        switch (continuation)
        {
            case "next-writer":
                if (refused)
                {
                    s.HandleA.Heal();
                    // The refused run unit is alive: its own next statement and its CLOSE (which ends the condition;
                    // a refused CLOSE has closed the connector already) claim above the refused claim (finding R4-3).
                    if (path is not (PagePath.Close or PagePath.Fence))
                    {
                        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 8, "eight"));
                        Assert.Equal(StoreStatus.Ok, s.A.Close());
                        expected += "8=eight;";
                    }
                }
                // The next writer: another live run unit, or — beside a NO OTHER connector, which admits none — the same
                // run unit if it lives, else nobody (the next OPEN, the case below).
                var next = s.B;
                if (next is null) break;
                Assert.Equal(StoreStatus.Ok, StoreKit.Put(next, 9, "nine"));
                Assert.Equal(expected + "9=nine;", StoreKit.Show(next));
                Assert.Equal(StoreStatus.Ok, next.Close());
                StoreKit.AssertSlotsAndWorkAgree(s.Medium);
                break;
            case "open":
                if (refused) s.HandleA.Heal();
                var (_, opened, c) = StoreKit.OpenAs(s.Medium, "C");
                Assert.Equal(StoreStatus.Ok, opened);
                Assert.Equal(expected, StoreKit.Show(c!));
                Assert.True(c!.Adoption.Fenced, where);
                StoreKit.AssertSlotsAndWorkAgree(s.Medium);
                Assert.Equal(StoreStatus.Ok, StoreKit.Put(c, 9, "nine"));
                break;
            case "power-loss":
                s.Medium.PowerLoss();
                var (_, reopened, d) = StoreKit.OpenAs(s.Medium, "D");
                Assert.Equal(StoreStatus.Ok, reopened);
                Assert.Equal(Base, StoreKit.Show(d!));   // the last durable completed statement: the scene's CLOSE
                StoreKit.AssertSlotsAndWorkAgree(s.Medium);
                break;
        }
        StoreKit.AssertNoStampReused(s.Medium);
        StoreKit.AssertMintsNeverDecrease(s.Medium);
        StoreKit.AssertR1(s.Medium);
    }

    /// <summary>2.2.1.16 "OPEN OUTPUT (creation)" over a previous store, at EVERY operation — the commit slot's write,
    /// its sync, page 2, the other slot, the truncation, the last sync. The one durable write decides (§14.9.27.4 GR18,
    /// <i>"After the creation of the file, the file contains no records"</i>): a death before the commit slot was handed
    /// over leaves the previous store whole, after it the EMPTY store, and a power loss keeps the empty store only once the
    /// commit's sync ran. A REFUSAL is GR25's unsuccessful OPEN, <i>"the file is not affected"</i>: '30' with the previous
    /// store whole at every point but the last sync, which follows the truncation and has nothing left to undo. In every
    /// cell an OPEN OUTPUT recreates, and the new store never reissues a stamp the previous one wrote (R3).</summary>
    [Fact]
    public void Every_point_of_creation_leaves_the_previous_store_or_the_empty_store_and_a_refusal_affects_nothing()
    {
        const string Previous = "1=old;2=older;";
        var probe = StoreKit.ClosedStore("1=old", "2=older");
        var ph = probe.Open("A");
        Assert.Equal(StoreStatus.Ok, PagedFile.Create(ph, StoreKit.Relative, true, out _));
        var ops = ph.Operations.ToList();
        Assert.Equal(6, ops.Count);
        Assert.Equal(new[] { "S", "W2", $"T{MetaPage.MetaPages * PageHeader.PageSize}", "S" }, new[] { ops[1], ops[2], ops[4], ops[5] });
        Assert.NotEqual(ops[0], ops[3]);
        for (int n = 1; n <= ops.Count; n++)
            foreach (var fault in new[] { Fault.Death, Fault.DeathAfter, Fault.Refuse })
                foreach (var continuation in new[] { "open", "power-loss" })
                {
                    string where = $"{fault} at op {n} ({ops[n - 1]}) then {continuation}";
                    var medium = StoreKit.ClosedStore("1=old", "2=older");
                    var previousBytes = medium.Contents();
                    var h = medium.Open("A");
                    h.FaultAtOperation(n, fault);
                    bool died = false;
                    try { Assert.True(PagedFile.Create(h, StoreKit.Relative, true, out _) == StoreStatus.PermanentError, where); }
                    catch (SimulatedDeath) { died = true; }
                    Assert.Equal(fault != Fault.Refuse, died);
                    // GR25: a refusal before the last sync leaves the file byte for byte as it was.
                    if (fault == Fault.Refuse && n < ops.Count)
                        Assert.True(previousBytes.AsSpan().SequenceEqual(medium.Contents()), $"{where}: the file was affected");
                    // The commit write was handed over at op 1 (performed by DeathAfter at 1, by any fault at a later op
                    // but a refusal, which is undone); it is durable once op 2, its sync, was performed.
                    bool handedOver = fault == Fault.Refuse ? n == ops.Count : fault == Fault.DeathAfter || n > 1;
                    bool durable = fault == Fault.Refuse ? n == ops.Count : fault == Fault.DeathAfter ? n >= 2 : n > 2;
                    if (continuation == "power-loss") medium.PowerLoss();
                    string expected = (continuation == "power-loss" ? durable : handedOver) ? "" : Previous;
                    var (_, status, c) = StoreKit.OpenAs(medium, "C");
                    Assert.True(status == StoreStatus.Ok, where);
                    Assert.True(expected == StoreKit.Show(c!), $"{where}: {StoreKit.Show(c!)}");
                    StoreKit.AssertSlotsAndWorkAgree(medium);
                    var again = medium.Open("D");
                    Assert.Equal(StoreStatus.Ok, PagedFile.Create(again, StoreKit.Relative, true, out var fresh));
                    Assert.Equal("", StoreKit.Show(fresh!));
                    Assert.Equal(StoreStatus.Ok, StoreKit.Put(fresh!, 7, "new", blobPages: 3));
                    StoreKit.AssertNoStampReused(medium);
                    StoreKit.AssertR1(medium);
                }
    }

    /// <summary>The previous files a creation meets that hold no store: empty, and foreign bytes of every length class.
    /// With no committed slot the commit write goes to slot A, page 0 (<see cref="MetaPage.SlotA"/>), so the classes are:
    /// inside page 0 (1 000 bytes: the commit slot's page partly held — a format-5 whole store of a few short records —
    /// slot B's page and page 2 past the end; branch review R3-1); inside page 1 (5 000: page 0 whole, slot B's page partly
    /// held, page 2 past the end); exactly two pages (8 192: page 2 just past the end); inside page 2 (12 000: page 2
    /// partly held); exactly four pages; and past three pages with a partial tail (20 000) — a format-5 whole store, or any
    /// non-store file, need not be a whole number of pages.</summary>
    public static TheoryData<int> PreviousFileLengths => new() { 0, 1_000, 5_000, 2 * PageHeader.PageSize, 12_000, 4 * PageHeader.PageSize, 20_000 };

    /// <summary>Creation over a file with no store in it: no committed slot exists until the commit write, so a death
    /// before it leaves the file as it was (the next OPEN answers '30') and after it the EMPTY store; a refusal at any point
    /// but the last sync is §14.9.27.4 GR25's unsuccessful OPEN, <i>"the file is not affected"</i>: every byte back AND the
    /// length to the byte (finding R2-1: the undo once kept the length in whole pages, so it cut a 5 000-byte file to
    /// 4 096 and never restored a partly held page), so an empty file stays empty.</summary>
    [Theory]
    [MemberData(nameof(PreviousFileLengths))]
    public void Every_point_of_creation_over_a_file_with_no_store_leaves_it_byte_for_byte_or_the_empty_store(int length)
    {
        var previous = Enumerable.Range(0, length).Select(i => (byte)(i * 7 + 3)).ToArray();
        int ops = 6;
        for (int n = 1; n <= ops; n++)
            foreach (var fault in new[] { Fault.Death, Fault.DeathAfter, Fault.Refuse })
            {
                string where = $"{length}-byte file, {fault} at op {n}";
                var medium = MemoryPageMedium.Holding(previous);
                var h = medium.Open("A");
                h.FaultAtOperation(n, fault);
                bool died = false;
                try { Assert.True(PagedFile.Create(h, StoreKit.Relative, true, out _) == StoreStatus.PermanentError, where); }
                catch (SimulatedDeath) { died = true; }
                Assert.Equal(fault != Fault.Refuse, died);
                bool created = fault == Fault.Refuse ? n == ops : fault == Fault.DeathAfter || n > 1;
                if (!created)
                {
                    Assert.True(length == medium.Length, $"{where}: length {medium.Length}");
                    Assert.True(previous.AsSpan().SequenceEqual(medium.Contents()), $"{where}: the bytes differ");
                }
                var (_, status, c) = StoreKit.OpenAs(medium, "C");
                Assert.True((created ? StoreStatus.Ok : StoreStatus.PermanentError) == status, where);
                if (created) Assert.Equal("", StoreKit.Show(c!));
            }
    }

    // ── A REFUSED READ at every read of every path: '30', never a verdict on the page (§9.1.13.6 item 1) ───────────

    /// <summary>Every read the page layer makes — OPEN's three meta pages, R6 step 3's verification of a DESCENDANT's
    /// <c>fresh</c> pages, a statement's entry (page 2), the promotion's read of both slots, the retirement's ledger and
    /// FREE chains, a tree page through its stamped pointer — refused by the medium, one at a time: the path answers '30'
    /// (§9.1.13.6 item 1, <i>"A permanent error exists and no further information is available concerning the
    /// input-output operation"</i>) and decides NOTHING from the unread page — no rejection of a work slot, no slot chosen
    /// for a promotion — so the file is the last completed statement for the refused run unit's next statement, for the
    /// next writer and for a fresh OPEN.</summary>
    [Theory]
    [MemberData(nameof(Paths))]
    public void A_refused_read_at_every_read_of_every_path_answers_30_and_decides_nothing(PagePath path)
    {
        int reads = DryReads(path);
        Assert.True(reads > 0, $"{path} reads nothing");
        for (int n = 1; n <= reads; n++)
        {
            string where = $"{path}: read {n} of {reads} refused";
            var s = NewScene(coherentA: path != PagePath.NoOtherStatement);
            s.HandleA.FaultAtRead(n, Fault.Refuse);
            Assert.True(StoreStatus.PermanentError == Run(s, path), where);
            s.HandleA.Heal();
            string expected = Base;
            if (path is not (PagePath.Close or PagePath.Fence))
            {
                Assert.True(StoreStatus.Ok == StoreKit.Put(s.A, 8, "eight"), where);
                expected += "8=eight;";
                Assert.Equal(StoreStatus.Ok, s.A.Close());
            }
            if (s.B is { } b)
            {
                Assert.True(StoreStatus.Ok == StoreKit.Put(b, 9, "nine"), where);
                expected += "9=nine;";
                Assert.True(expected == StoreKit.Show(b), $"{where}: {StoreKit.Show(b)}");
                Assert.Equal(StoreStatus.Ok, b.Close());
            }
            var (_, opened, c) = StoreKit.OpenAs(s.Medium, "C");
            Assert.True(StoreStatus.Ok == opened, where);
            Assert.True(expected == StoreKit.Show(c!), $"{where}: {StoreKit.Show(c!)}");
            StoreKit.AssertSlotsAndWorkAgree(s.Medium);
            StoreKit.AssertNoStampReused(s.Medium);
            StoreKit.AssertR1(s.Medium);
        }
    }

    public static TheoryData<PagePath> Paths() => new(Enum.GetValues<PagePath>());

    private static int DryReads(PagePath path)
    {
        var s = NewScene(coherentA: path != PagePath.NoOtherStatement);
        int before = s.HandleA.ReadsPerformed;
        Assert.Equal(StoreStatus.Ok, Run(s, path));
        return s.HandleA.ReadsPerformed - before;
    }

    /// <summary>The reads of the other run units' paths: an INPUT OPEN's three meta pages, a live reader's entry and its
    /// tree reads, and the RESTART RULE's read of both slots when a dead promoter left PROMOTING — each refused, each '30'
    /// (a reader's tree read: <see cref="PageCheck.Unreadable"/>), and the next try, healed, finds the last completed
    /// statement and completes the dead promotion.</summary>
    [Fact]
    public void A_refused_read_by_a_reader_an_INPUT_OPEN_and_the_restart_rule_answers_30_and_decides_nothing()
    {
        var s = NewScene();
        // A live reader: its entry (page 2) and its tree reads, after another run unit's completed statement.
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        s.HandleR!.FaultAtRead(1, Fault.Refuse);
        Assert.Equal(StoreStatus.PermanentError, s.Reader!.Enter());
        Assert.Equal(StoreStatus.Ok, s.Reader!.Enter());
        var root = new byte[PageHeader.PageSize];
        s.HandleR.FaultAtRead(1, Fault.Refuse);
        Assert.Equal(PageCheck.Unreadable, s.Reader!.Read(s.Reader.Meta.PrimeRoot, root));
        Assert.Equal(Base + "3=three;", StoreKit.Show(s.Reader!));

        // An INPUT OPEN, at each of its reads.
        for (int n = 1; n <= MetaPage.MetaPages; n++)
        {
            var h = s.Medium.Open($"I{n}");
            h.FaultAtRead(n, Fault.Refuse);
            Assert.Equal(StoreStatus.PermanentError, PagedFile.Open(h, writable: false, coherent: true, out var none));
            Assert.Null(none);
        }

        // The restart rule: A dies after its promotion's claim; B's entry reads both slots, and that read is refused.
        var ops = DryRun(PagePath.Close);
        s.HandleA.FaultAtOperation(ops.IndexOf("S") + 1, Fault.DeathAfter);   // the claim landed and synced
        Assert.Throws<SimulatedDeath>(() => s.A.Close());
        Assert.True(MetaPage.Decode(s.Medium.Snapshot(MetaPage.WorkSlot), MetaPage.WorkSlot)!.Promoting);
        // B's statement, its every read refused in turn (page 2, the chains it adopts, both slots, the retirement's
        // chains, its tree pages): '30' each time, until the fault lies past its last read and the statement completes.
        int refusedReads = 0;
        for (int n = 1; ; n++)
        {
            s.HandleB!.FaultAtRead(n, Fault.Refuse);
            var status = StoreKit.Put(s.B!, 4, "four");
            s.HandleB.Heal();
            if (status == StoreStatus.Ok) break;
            Assert.True(StoreStatus.PermanentError == status, $"read {n}");
            refusedReads++;
        }
        Assert.True(refusedReads >= 3, $"only {refusedReads} reads");
        Assert.False(s.B!.Meta.Promoting);
        Assert.Equal(Base + "3=three;4=four;", StoreKit.Show(s.B!));
        StoreKit.AssertNoStampReused(s.Medium);
        StoreKit.AssertR1(s.Medium);
    }

    // ── 2.2.1.12's adoption matrix (R6), the no-transaction column ─────────────────────────────────────────────

    [Fact]
    public void Matrix_a_CLOSE_then_an_OPEN_adopts_the_work_slot_and_fences()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "one"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, status, c) = StoreKit.OpenAs(medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(new Adoption(WorkSlotClass.Descendant, WorkAdopted: true, Fenced: true), c!.Adoption);
        Assert.Equal("1=one;", StoreKit.Show(c));
        StoreKit.AssertSlotsAndWorkAgree(medium);
    }

    [Fact]
    public void Matrix_death_after_a_completed_statement_and_death_mid_statement_adopt_the_last_completed_one()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        s.HandleA.FaultAtWrite(2, Fault.Death);
        Assert.Throws<SimulatedDeath>(() => StoreKit.Put(s.A, 4, "four"));
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.True(c!.Adoption.WorkAdopted);
        Assert.Equal(Base + "3=three;", StoreKit.Show(c));
    }

    /// <summary>Process death between a promotion's slot write and its retirement's page 2: the PROMOTION SOURCE, the
    /// committed meta (the same tree) adopted, the retirement completed by the writable opener's fence.</summary>
    [Fact]
    public void Matrix_death_between_the_slot_write_and_the_retirement_is_the_promotion_source()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        s.HandleA.FaultAtSync(2, Fault.DeathAfter);   // sync 2 landed; the retirement never ran
        Assert.Throws<SimulatedDeath>(() => s.A.Close());
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(new Adoption(WorkSlotClass.PromotionSource, WorkAdopted: false, Fenced: true), c!.Adoption);
        Assert.Equal(Base + "3=three;", StoreKit.Show(c));
        StoreKit.AssertSlotsAndWorkAgree(s.Medium);
        StoreKit.AssertR2(s.Medium);
    }

    /// <summary>A fence whose slot write landed and whose retirement did not: the next OPEN finds the PROMOTION SOURCE
    /// and fences again, above the first fence.</summary>
    [Fact]
    public void Matrix_a_fence_landed_and_its_retirement_did_not()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "one"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var h = medium.Open("A");
        h.FaultAtSync(2, Fault.DeathAfter);
        Assert.Throws<SimulatedDeath>(() => PagedFile.Open(h, true, true, out _));
        ulong firstFence = MetaPage.Committed(StoreKit.MetasAt(medium, medium.LastSeq).A, StoreKit.MetasAt(medium, medium.LastSeq).B)!.Value.Meta.Generation;
        var (_, status, c) = StoreKit.OpenAs(medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(WorkSlotClass.PromotionSource, c!.Adoption.WorkSlot);
        Assert.True(c.Meta.CommittedGeneration > firstFence + PagedFile.GenerationCeiling);
        Assert.Equal("1=one;", StoreKit.Show(c));
        StoreKit.AssertNoStampReused(medium);
    }

    [Fact]
    public void Matrix_power_loss_with_every_write_durable_adopts_the_work_slot()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 4, "four"));
        s.Medium.PowerLoss(keep: _ => true);
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.True(c!.Adoption.WorkAdopted);
        Assert.Equal(Base + "3=three;4=four;", StoreKit.Show(c));
    }

    /// <summary>Power loss losing the last k writes, for every k: the committed meta (the last promotion) unless every
    /// page the surviving work slot lists verifies — never a torn state.</summary>
    [Fact]
    public void Matrix_power_loss_losing_the_last_k_writes_adopts_the_committed_meta_unless_every_listed_page_verifies()
    {
        var probe = NewScene();
        long start = probe.Medium.LastSeq;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(probe.A, 3, "three"));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(probe.A, 4, "four", blobPages: 1));
        int written = (int)(probe.Medium.LastSeq - start);
        for (int k = 0; k <= written; k++)
        {
            var s = NewScene();
            long from = s.Medium.LastSeq;
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 4, "four", blobPages: 1));
            long lastKept = s.Medium.LastSeq - k;
            s.Medium.PowerLoss(keep: w => w.Seq <= lastKept);
            var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
            Assert.Equal(StoreStatus.Ok, status);
            string shown = StoreKit.Show(c!);
            Assert.True(shown is Base or Base + "3=three;" or Base + "3=three;4=four;", $"k={k}: {shown}");
            Assert.Equal(c!.Adoption.WorkAdopted ? shown : Base, shown);
            _ = from;
        }
    }

    /// <summary>A torn fresh page and a torn work slot: the committed meta, with the rejected ledger's pages returned to
    /// the FREE set by the fence.</summary>
    [Fact]
    public void Matrix_a_torn_fresh_page_or_a_torn_work_slot_adopts_the_committed_meta()
    {
        foreach (bool tearWork in new[] { false, true })
        {
            var s = NewScene();
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three", blobPages: 1));
            var fresh = s.A.LedgerEntries.Where(e => e.Value.State == LedgerState.Fresh).Select(e => e.Key).ToList();
            s.Medium.Tear(tearWork ? MetaPage.WorkSlot : fresh[0]);
            var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
            Assert.Equal(StoreStatus.Ok, status);
            Assert.Equal(tearWork ? WorkSlotClass.Foreign : WorkSlotClass.Descendant, c!.Adoption.WorkSlot);
            Assert.False(c.Adoption.WorkAdopted);
            Assert.Equal(Base, StoreKit.Show(c));
            // The rejected history's never-committed pages are reclaimed: each is in the FREE set, or taken by the
            // fence's own retirement, or past the committed page count (reached by nothing; the next growth reuses it).
            if (!tearWork)
                Assert.All(fresh, p => Assert.True(c.FreePages.Contains(p) || c.LedgerEntries.ContainsKey(p) || p >= c.Meta.PageCount,
                    $"rejected page {p} leaked"));
            StoreKit.AssertSlotsAndWorkAgree(s.Medium);
            StoreKit.AssertR2(s.Medium);
        }
    }

    /// <summary>A torn committed slot — the one the promotion was writing: the other committed slot is the committed meta,
    /// and the work slot, synced before the tear, is a DESCENDANT that verifies.</summary>
    [Fact]
    public void Matrix_a_torn_committed_slot_adopts_the_work_slot()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        long mark = s.Medium.LastSeq;
        s.HandleA.FaultAtSync(2, Fault.Death);   // the slot handed over, sync 2 never ran
        Assert.Throws<SimulatedDeath>(() => s.A.Close());
        var slotWrite = s.Medium.Writes.Single(w => w.Seq > mark && w.Page < MetaPage.WorkSlot);
        s.Medium.Tear(slotWrite.Page);
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(new Adoption(WorkSlotClass.Descendant, WorkAdopted: true, Fenced: true), c!.Adoption);
        Assert.Equal(Base + "3=three;", StoreKit.Show(c));
    }

    [Fact]
    public void Matrix_both_committed_slots_torn_answers_30_and_writes_nothing()
    {
        var s = NewScene();
        s.Medium.Tear(MetaPage.SlotA);
        s.Medium.Tear(MetaPage.SlotB);
        long before = s.Medium.LastSeq;
        var (_, status, _) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.PermanentError, status);
        Assert.Equal(before, s.Medium.LastSeq);
        var (_, input, _) = StoreKit.OpenAs(s.Medium, "R2", writable: false);
        Assert.Equal(StoreStatus.PermanentError, input);
    }

    /// <summary>An OPEN by a second run unit while the first holds the file: the opener adopts the holder's last completed
    /// statement and fences it; the holder's next statement adopts page 2 and drops its cache.</summary>
    [Fact]
    public void Matrix_an_OPEN_by_a_second_run_unit_while_the_first_holds_the_file()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.True(c!.Adoption.WorkAdopted);
        Assert.Equal(Base + "3=three;", StoreKit.Show(c));
        int epoch = s.A.CacheEpoch;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 4, "four"));
        Assert.Equal(epoch + 1, s.A.CacheEpoch);
        Assert.Equal(c.Meta.CommittedGeneration, s.A.Meta.CommittedGeneration);
        Assert.Equal(StoreStatus.Ok, c.Enter());
        Assert.Equal(Base + "3=three;4=four;", StoreKit.Show(c));
    }

    /// <summary>An OPEN by a third run unit after one of two holders died mid-statement: the last completed statement,
    /// fenced; the live holder's next statement continues on the fence.</summary>
    [Fact]
    public void Matrix_an_OPEN_by_a_third_run_unit_after_one_of_two_holders_died()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 3, "three"));
        s.HandleA.FaultAtWrite(3, Fault.Death);
        Assert.Throws<SimulatedDeath>(() => StoreKit.Put(s.A, 4, "four"));
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(Base + "3=three;", StoreKit.Show(c!));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 5, "five"));
        Assert.Equal(Base + "3=three;5=five;", StoreKit.Show(s.B!));
        StoreKit.AssertNoStampReused(s.Medium);
    }

    /// <summary>Finding R2-5: an INPUT OPEN of a crashed file walks the COMMITTED tree, and keeps walking it until a
    /// writable opener's fence changes page 2's generation — then page 2's tree, never the rejected one.</summary>
    [Fact]
    public void Matrix_an_INPUT_OPEN_of_a_crashed_file_then_a_writable_OPEN_while_the_reader_is_live()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        var fresh = s.A.LedgerEntries.First(e => e.Value.State == LedgerState.Fresh).Key;
        s.Medium.PowerLoss(keep: _ => true);
        s.Medium.Tear(fresh);
        var (_, rs, reader) = StoreKit.OpenAs(s.Medium, "R2", writable: false);
        Assert.Equal(StoreStatus.Ok, rs);
        Assert.False(reader!.Adoption.WorkAdopted);
        Assert.False(reader.Adoption.Fenced);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(StoreStatus.Ok, reader.Enter());
            Assert.Equal(Base, StoreKit.Show(reader));
        }
        var (_, ws, writer) = StoreKit.OpenAs(s.Medium, "W");
        Assert.Equal(StoreStatus.Ok, ws);
        Assert.Equal(StoreStatus.Ok, reader.Enter());
        Assert.Equal(Base, StoreKit.Show(reader));
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(writer!, 7, "seven"));
        Assert.Equal(StoreStatus.Ok, reader.Enter());
        Assert.Equal(Base + "7=seven;", StoreKit.Show(reader));
    }

    /// <summary>Finding R3-4's row: a death mid-statement in A, a completed statement of B, then a power loss keeping A's
    /// dead pages and losing B's writes over them — the committed meta, and no record of either WRITE.</summary>
    [Fact]
    public void Matrix_a_dead_statement_a_live_completed_one_and_a_power_loss_keeping_the_dead_pages()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, s.B!.Enter());
        s.HandleA.FaultAtWrite(3, Fault.Death);
        Assert.Throws<SimulatedDeath>(() => StoreKit.Put(s.A, 3, "dead"));
        var deadPages = s.Medium.Writes.Where(w => w.Unit == "A" && w.Page >= 3 && !w.Synced).Select(w => w.Page).ToHashSet();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 4, "live"));
        Assert.Contains(s.Medium.Writes, w => w.Unit == "B" && !w.Synced && deadPages.Contains(w.Page));
        s.Medium.PowerLoss(keep: w => w.Unit == "A" || !deadPages.Contains(w.Page));
        var (_, status, c) = StoreKit.OpenAs(s.Medium, "C");
        Assert.Equal(StoreStatus.Ok, status);
        Assert.False(c!.Adoption.WorkAdopted);
        Assert.Equal(Base, StoreKit.Show(c));
    }

    /// <summary>A promoter's death between its claim and its retirement's page 2 while another run unit is live, at each
    /// of those points: that run unit's next statement completes the promotion before its own work (the work slot = the
    /// completed promotion plus the live statement), and the retirement's stamps lie above the dead one's claim.</summary>
    [Fact]
    public void Matrix_a_promoters_death_while_another_run_unit_is_live_is_completed_by_its_next_statement()
    {
        var ops = DryRun(PagePath.Close);
        for (int n = 2; n <= ops.Count; n++)
        {
            var s = NewScene();
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
            s.HandleA.FaultAtOperation(n, Fault.Death);
            Assert.Throws<SimulatedDeath>(() => s.A.Close());
            var claimed = MetaPage.Decode(s.Medium.Snapshot(MetaPage.WorkSlot), MetaPage.WorkSlot)!;
            long mark = s.Medium.LastSeq;
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 4, "four"));
            Assert.Equal(Base + "3=three;4=four;", StoreKit.Show(s.B!));
            Assert.False(s.B!.Meta.Promoting);
            if (claimed.Promoting)
                Assert.All(s.Medium.Writes.Where(w => w.Seq > mark && w.Page >= 3), w => Assert.True(w.Generation > claimed.StampMint));
            var (sa, sb, work) = StoreKit.MetasAt(s.Medium, s.Medium.LastSeq);
            Assert.Equal(MetaPage.Committed(sa, sb)!.Value.Meta.Generation, work!.CommittedGeneration);
            StoreKit.AssertNoStampReused(s.Medium);
            StoreKit.AssertR1(s.Medium);
        }
    }

    // ── R5's restart rule: a flag left by a dead or refused promoter ───────────────────────────────────────────

    /// <summary>R5's restart with a nonzero <c>committing</c> and an empty TRANSACTIONS tree: a COMMIT's claim whose slot
    /// never landed is UNWOUND (page 2 unflagged, <c>committing</c> 0, nothing else written); one whose slot landed is
    /// COMPLETED (its retirement clears <c>committing</c>).</summary>
    [Fact]
    public void The_restart_unwinds_an_unlanded_COMMIT_claim_and_completes_a_landed_one()
    {
        foreach (bool landed in new[] { false, true })
        {
            var s = NewScene();
            var meta = MetaPage.Decode(s.Medium.Snapshot(MetaPage.WorkSlot), MetaPage.WorkSlot)!;
            var claim = meta with { Generation = meta.StampMint + 1, StampMint = meta.StampMint + 2, Promoting = true, Committing = 7 };
            s.Medium.Poke(MetaPage.WorkSlot, claim.Encode(MetaPage.WorkSlot));
            if (landed)
            {
                var (sa, sb, _) = StoreKit.MetasAt(s.Medium, s.Medium.LastSeq);
                uint target = MetaPage.Committed(sa, sb)!.Value.Slot == MetaPage.SlotA ? MetaPage.SlotB : MetaPage.SlotA;
                s.Medium.Poke(target, (claim with { Promoting = false, CommittedGeneration = claim.Generation }).Encode(target));
            }
            long mark = s.Medium.LastSeq;
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 4, "four"));
            var first = s.Medium.Writes.First(w => w.Seq > mark);
            var after = MetaPage.Decode(s.Medium.SnapshotAt(MetaPage.WorkSlot, first.Seq), MetaPage.WorkSlot)!;
            Assert.Equal(MetaPage.WorkSlot, first.Page);
            if (!landed)
            {
                Assert.False(after.Promoting);
                Assert.Equal(0ul, after.Committing);
                Assert.Equal(claim.PrimeRoot, after.PrimeRoot);
                Assert.Equal(claim.CommittedGeneration, after.CommittedGeneration);
            }
            else
                Assert.Equal(claim.Generation, s.B!.Meta.CommittedGeneration);
            Assert.Equal(0ul, s.B!.Meta.Committing);
            Assert.Equal(Base + "4=four;", StoreKit.Show(s.B!));
        }
    }

    // ── 2.2.1.17: two run units interleaved (the rows the page layer's paths own) ──────────────────────────────

    [Fact]
    public void Interleaving_a_later_writer_adopts_page_2_and_claims_above_the_mint_two_readers_see_one_tree()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        int epoch = s.B!.CacheEpoch;
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 4, "four"));
        Assert.Equal(epoch + 1, s.B!.CacheEpoch);
        Assert.True(s.B!.Meta.Generation > s.A.Meta.Generation);
        var (_, _, r2) = StoreKit.OpenAs(s.Medium, "R2", writable: false);
        Assert.Equal(StoreStatus.Ok, s.Reader!.Enter());
        Assert.Equal(StoreStatus.Ok, r2!.Enter());
        Assert.Equal(StoreKit.Show(s.Reader!), StoreKit.Show(r2));
        Assert.Equal(Base + "3=three;4=four;", StoreKit.Show(r2));
    }

    /// <summary>A stamp-mint change alone (a dead or refused attempt) keeps the cache and raises the claim; a generation
    /// change adopts page 2 and drops it; an INPUT store acts on the generation only.</summary>
    [Fact]
    public void Interleaving_a_mint_change_alone_keeps_the_cache_a_generation_change_drops_it()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, s.B!.Enter());
        Assert.Equal(StoreStatus.Ok, s.Reader!.Enter());
        int epochB = s.B!.CacheEpoch, epochR = s.Reader!.CacheEpoch;
        s.HandleA.FaultAtWrite(2, Fault.Refuse);
        Assert.Equal(StoreStatus.PermanentError, StoreKit.Put(s.A, 3, "refused"));
        ulong mint = MetaPage.Decode(s.Medium.Snapshot(MetaPage.WorkSlot), MetaPage.WorkSlot)!.StampMint;
        Assert.Equal(StoreStatus.Ok, s.Reader!.Enter());
        Assert.Equal(epochR, s.Reader!.CacheEpoch);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.B!, 4, "four"));
        Assert.Equal(epochB, s.B!.CacheEpoch);
        Assert.True(s.B!.Meta.Generation > mint);
        Assert.Equal(StoreStatus.Ok, s.Reader!.Enter());
        Assert.Equal(epochR + 1, s.Reader!.CacheEpoch);
        Assert.Equal(Base + "4=four;", StoreKit.Show(s.Reader!));
    }

    /// <summary>A's statement ∥ B's checkpoint: A's next statement adopts the retirement's page 2 — the new base and the
    /// new ledger — and drops its cache.</summary>
    [Fact]
    public void Interleaving_a_statement_after_anothers_checkpoint_adopts_the_new_base()
    {
        var s = NewScene();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
        Assert.Equal(StoreStatus.Ok, s.B!.Enter());
        Assert.Equal(StoreStatus.Ok, s.B!.Promote());
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 4, "four"));
        Assert.Equal(s.B!.Meta.CommittedGeneration, s.A.Meta.CommittedGeneration);
        Assert.Equal(Base + "3=three;4=four;", StoreKit.Show(s.A));
        StoreKit.AssertR1(s.Medium);
        StoreKit.AssertR2(s.Medium);
    }

    /// <summary>A's writable OPEN ∥ B's writable OPEN: the second reads the slots and page 2 afresh — the first's fence and
    /// its retirement (a DESCENDANT) — and fences again above it.</summary>
    [Fact]
    public void Interleaving_two_writable_OPENs_fence_in_turn()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "one"));
        Assert.Equal(StoreStatus.Ok, store.Close());
        var (_, _, a) = StoreKit.OpenAs(medium, "A");
        var (_, _, b) = StoreKit.OpenAs(medium, "B");
        Assert.Equal(WorkSlotClass.Descendant, b!.Adoption.WorkSlot);
        Assert.True(b.Meta.CommittedGeneration > a!.Meta.CommittedGeneration + PagedFile.GenerationCeiling);
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(a, 2, "two"));
        Assert.Equal(StoreStatus.Ok, b.Enter());
        Assert.Equal("1=one;2=two;", StoreKit.Show(b));
    }

    /// <summary>A's writable OPEN ∥ B's INPUT OPEN of a crashed file, in both orders: the reader never walks the rejected
    /// tree.</summary>
    [Fact]
    public void Interleaving_a_writable_and_an_INPUT_OPEN_of_a_crashed_file_in_both_orders()
    {
        foreach (bool readerFirst in new[] { true, false })
        {
            var s = NewScene();
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(s.A, 3, "three"));
            s.Medium.PowerLoss(keep: _ => true);
            s.Medium.Tear(s.A.LedgerEntries.First(e => e.Value.State == LedgerState.Fresh).Key);
            PagedFile? reader = null;
            if (readerFirst) reader = StoreKit.OpenAs(s.Medium, "R2", writable: false).Store;
            var writer = StoreKit.OpenAs(s.Medium, "W").Store;
            reader ??= StoreKit.OpenAs(s.Medium, "R2", writable: false).Store;
            Assert.Equal(StoreStatus.Ok, reader!.Enter());
            Assert.Equal(Base, StoreKit.Show(reader));
            Assert.Equal(Base, StoreKit.Show(writer!));
        }
    }
}
