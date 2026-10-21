// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;
using static CobolNet.Tests.Unit.Store.TransactionTrees;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ A PROCESS DEATH, A POWER LOSS AND A REFUSED WRITE, SYNC OR READ AT EVERY POINT OF EVERY TRANSACTION PATH, AND A
/// SECOND RUN UNIT INTERLEAVED AT EVERY STEP, LEAVE THE FILE AS THE DESIGN SAYS (DESIGN-runtime-library.md 2.2.1.16 and
/// 2.2.1.17, in docs/rearchitecture; kb/Work PB3337, the rows <c>PendingStoreCases</c> held for the transaction slice).
/// <para>Every run unit is a real <see cref="FileRegistry"/> with real handles on one real host file — its file lock, its
/// region locks, the store mutex and the transactions' liveness bytes are the host's — while its PAGES go through
/// <see cref="KeyedStore.MediumObserver"/> to one shared <see cref="MemoryPageMedium"/>, the deterministic fault-injecting
/// medium of 2.2.1.12, so a power loss can lose every write no sync made durable. Over each run unit's handle on it sit a
/// <see cref="DyingPageMedium"/> (the run unit dies after its n-th write: what it handed over stays, nothing after it
/// reaches the medium) and a <see cref="RefusingPageMedium"/> whose seam counts every read, write and sync (refusing the
/// one a case names, or delivering ANOTHER run unit's death at that instant). A case first measures the path on a run
/// that is not faulted, then faults it at every point of that measurement.</para>
/// <para>Expected values are the standard's and the design's: §9.1.18.5 (<i>"When a run unit terminates abnormally, all
/// file changes made since the previous commit or, if none, the beginning of the run unit are rolled back for all files
/// for which there are active APPLY COMMIT clauses."</i>); §14.9.7.4 GR3 and §14.9.36.4 GR3 (a COMMIT applies, a
/// ROLLBACK restores to the last COMMIT); §9.1.13.6 item 1 (<i>"A permanent error exists and no further information is
/// available concerning the input-output operation"</i>: '30' for every refusal); §9.1.16's locked-record circumstance
/// ('51', DOC-A.1-152); and 2.2.1.10's three regions of a COMMIT — decided by its slot write, answered by the sync after it.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class TransactionFaultMatrixTests
{
    public static TheoryData<FileOrg> Orgs() => [.. KeyedOrgs];

    private static readonly IOException Refusal = new("refused by the host");

    /// <summary>One run unit: a registry with connector <c>F</c> (and <c>G</c> for a second physical file) and its seams.</summary>
    private sealed class Unit(FileOrg org)
    {
        public FileRegistry Reg { get; } = new();

        /// <summary>The run unit's handle onto each physical file it opened, through which it dies.</summary>
        public List<DyingPageMedium> Media { get; } = [];

        /// <summary>Its handle onto the first file (connector <c>F</c>).</summary>
        public DyingPageMedium Dying => Media[0];

        /// <summary>Every page operation the run unit's seam saw, in order: <c>R&lt;page&gt;</c>, <c>W&lt;page&gt;</c>, <c>S</c>.</summary>
        public List<string> Log { get; } = [];

        /// <summary>The refusal the host answers at operation n (an index into <see cref="Log"/>), or null.</summary>
        public Func<int, string, Exception?>? Refuse { get; set; }

        /// <summary>Called before operation n is made: another run unit's death delivered at that instant.</summary>
        public Action<int>? AtOperation { get; set; }

        public bool Closed { get; private set; }

        public Exception? Seam(string op)
        {
            int n = Log.Count;
            Log.Add(op);
            AtOperation?.Invoke(n);
            return Refuse?.Invoke(n, op);
        }

        /// <summary>The run unit DIES: nothing it does from now reaches the medium, and the operating environment releases
        /// its handles — its file lock, its region locks and its transactions' liveness bytes with them.</summary>
        public void Kill()
        {
            if (Closed) return;
            (Refuse, AtOperation) = (null, null);
            foreach (var medium in Media) medium.DieAfter = medium.Writes;
            Reg.CloseAll();
            Closed = true;
        }

        /// <summary>The run unit ENDS NORMALLY (§9.1.18.5's first sentence: its transactions are committed).</summary>
        public void End()
        {
            if (Closed) return;
            (Refuse, AtOperation) = (null, null);
            Reg.CloseAll();
            Closed = true;
        }

        public string Write(int n, string tail, string name = "F") => FileHarness.Write(Reg, name, org, n, tail);
        public string Rewrite(int n, string tail, string name = "F") => FileHarness.Rewrite(Reg, name, org, n, tail);
        public string Delete(int n, string name = "F") => FileHarness.Delete(Reg, name, org, n);

        /// <summary>Records 1 to 9 as this run unit reads them (absent: null).</summary>
        public string?[] Contents(string name = "F")
        {
            var records = new string?[10];
            for (int n = 1; n <= 9; n++)
                records[n] = Read(Reg, name, org, n, out var image) == "00" ? image : null;
            return records;
        }

        public ulong Transaction(string name = "F") => Reg.TransactionOf(name);
    }

    /// <summary>One physical file: a real host file (handles, file locks, region locks) whose pages live in
    /// <see cref="Memory"/>. Disposing it kills every run unit still alive and deletes the host file.</summary>
    private sealed class World(string tag, FileOrg org) : IDisposable
    {
        public string Host { get; } = Tmp($"pb3337-{tag}-{org}");
        public MemoryPageMedium Memory { get; } = new();
        private readonly List<Unit> _units = [];

        /// <summary>Open connector <paramref name="name"/> on this file in a new run unit, or in <paramref name="into"/>'s.
        /// Under <paramref name="commit"/> an active APPLY COMMIT clause names it (2.2.1.10's implicit posture). An INDEXED
        /// file has an alternate key WITHOUT DUPLICATES over positions 5-8 (finding R2-4's unique values).</summary>
        public Unit Open(string unit, bool commit, FileOpenMode mode = FileOpenMode.IO, string name = "F", Unit? into = null)
        {
            var handle = Memory.Open(unit);
            var dying = new DyingPageMedium(handle);
            var u = into ?? new Unit(org);
            u.Media.Add(dying);
            KeyedStore.MediumObserver.Value = _ => new RefusingPageMedium(dying,
                write: p => u.Seam($"W{p}"), sync: () => u.Seam("S"), read: p => u.Seam($"R{p}"));
            try
            {
                if (org == FileOrg.Relative)
                    u.Reg.RegisterRelative(name, Host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                        relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
                else
                {
                    u.Reg.RegisterIndexed(name, Host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                        primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
                    u.Reg.AddAlternateKey(name, [new(4, 4)], duplicates: false);
                }
                u.Reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
                if (commit) u.Reg.RegisterCommitControl(name);
                u.Reg.OpenStatic(name, mode);
            }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", u.Reg.Status(name));
            if (into is null) _units.Add(u);
            return u;
        }

        /// <summary>Records 1 to 5, each <c>nnnn</c> + <c>Annn</c> (a unique alternate value), committed and synced.</summary>
        public World Seeded()
        {
            var seed = Open("seed", commit: false, FileOpenMode.Output);
            for (int n = 1; n <= 5; n++) Assert.Equal("00", seed.Write(n, $"A{n:000}"));
            seed.End();
            Assert.Equal("00", seed.Reg.Status("F"));
            return this;
        }

        /// <summary>Every run unit still alive dies, and then the power fails: every write no sync made durable is lost
        /// (2.2.1.16's power-loss variant).</summary>
        public void PowerLoss(Func<MediumWrite, bool>? keep = null)
        {
            foreach (var u in _units) u.Kill();
            Memory.PowerLoss(keep);
        }

        public void Dispose()
        {
            try
            {
                foreach (var u in _units) u.Kill();
            }
            finally { TryDelete(Host); }
        }
    }

    // ── The scenario every row starts from ────────────────────────────────────────────────────────────────────────

    /// <summary>A's transaction: it DELETEs K5 (freeing its unique value A005), REWRITEs K1 to X001 (freeing A001) and
    /// WRITEs K7.</summary>
    private static void Change(Unit a)
    {
        Assert.Equal("00", a.Delete(5));
        Assert.Equal("00", a.Rewrite(1, "X001"));
        Assert.Equal("00", a.Write(7, "A007"));
        Assert.NotEqual(0ul, a.Transaction());
        AssertHoldingsAreTheUndoKeys(a.Reg);
    }

    private static string?[] Seed()
    {
        var expected = new string?[10];
        for (int n = 1; n <= 5; n++) expected[n] = Rec(n, $"A{n:000}");
        return expected;
    }

    /// <summary>The file once A's transaction is committed.</summary>
    private static string?[] Committed()
    {
        var expected = Seed();
        expected[1] = Rec(1, "X001");
        expected[5] = null;
        expected[7] = Rec(7, "A007");
        return expected;
    }

    private static string?[] With(string?[] expected, int n, string tail)
    {
        var copy = (string?[])expected.Clone();
        copy[n] = Rec(n, tail);
        return copy;
    }

    /// <summary>The file as a fresh run unit's writable OPEN finds it (R6, with its dead-owner step), checked for no
    /// transaction left.</summary>
    private static void AssertReopened(World w, string?[] expected)
    {
        var c = w.Open($"reopen-{Guid.NewGuid():N}", commit: false);
        Assert.Equal(expected, c.Contents());
        AssertNoTransactionLeft(c.Reg);
        c.End();
    }

    /// <summary>The operations a path makes, measured on a run that is not faulted.</summary>
    private static List<string> Measure(Unit u, Action path)
    {
        int before = u.Log.Count;
        path();
        return u.Log.Skip(before).ToList();
    }

    private static int SlotWrite(List<string> ops) => ops.FindIndex(op => op is "W0" or "W1");

    public static TheoryData<FileOrg, string> OrgsByContinuation()
    {
        var data = new TheoryData<FileOrg, string>();
        foreach (var org in KeyedOrgs)
            foreach (var continuation in new[] { "writer", "open", "power-loss" })
                data.Add(org, continuation);
        return data;
    }

    // ── 2.2.1.16: COMMIT, killed at every write ─────────────────────────────────────────────────────────────────

    /// <summary>2.2.1.16's COMMIT rows (round 4): the owner killed before its claim, after it (before or after sync 1),
    /// after the slot write, after sync 2 and inside the retirement. Whole or not at all, decided by the slot alone: killed
    /// before the slot write was handed over, the transaction is rolled back (by the next writer's dead-owner step, or the
    /// next OPEN's fence and step 5); after it, committed (the restart rule, or the fence, completes the retirement). Under a
    /// power loss the slot is durable only once sync 2 ran. The continuations: a live run unit's next statement, a fresh
    /// OPEN, and a power loss followed by an OPEN; every one leaves no transaction in the file.</summary>
    [Theory]
    [MemberData(nameof(OrgsByContinuation))]
    public void A_commit_killed_at_every_write_is_rolled_back_before_its_slot_write_and_committed_after(FileOrg org,
        string continuation)
    {
        List<string> writes;
        using (var m = new World("measure-commit", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            if (continuation == "writer") m.Open("B", commit: false);
            Change(a);
            writes = Measure(a, () => Assert.Null(a.Reg.Commit())).Where(op => op[0] == 'W').ToList();
        }
        int slot = SlotWrite(writes);
        Assert.True(slot > 0, $"a COMMIT writes its claim before its slot ({string.Join(' ', writes)})");
        for (int k = 0; k <= writes.Count; k++)
        {
            using var w = new World($"kill-commit-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = continuation == "writer" ? w.Open("B", commit: false) : null;
            Change(a);
            a.Dying.DieAfter = a.Dying.Writes + k;
            _ = a.Reg.Commit();   // its answer never reaches a program: the run unit dies inside it
            a.Kill();
            bool committed = continuation == "power-loss" ? k >= slot + 2 : k > slot;
            var expected = committed ? Committed() : Seed();
            if (b is not null)
            {
                Assert.Equal("00", b.Write(9, "B009"));
                Assert.Equal(With(expected, 9, "B009"), b.Contents());
                AssertNoTransactionLeft(b.Reg);
                continue;
            }
            if (continuation == "power-loss") w.PowerLoss();
            AssertReopened(w, expected);
        }
    }

    // ── 2.2.1.16's refused table: COMMIT refused at every operation, and the reversal ───────────────────────────

    /// <summary>2.2.1.16's refused rows for COMMIT, at every read, write and sync it makes (round 5, finding R5-1), the
    /// owner alive and told '30' on the file (COMMIT has no I-O status of its own, §9.1.13.1). Refused before or at the slot
    /// write, or at sync 1 (region 1): the transaction is intact — B's statements meet its holdings ('51') — and item 8's
    /// unconditional ROLLBACK restores it. Sync 2 refused (region 3): the COMMIT REVERSES before it gives the mutex back, so
    /// B's next statement meets the state of the previous COMMIT ('22' for K5) and the ROLLBACK finds no transaction. Refused
    /// in the retirement: committed, reported successful, and B's next entry completes the retirement. In no run is a COMMIT
    /// reported failed with its transaction committed, or successful and then rolled back.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_commit_refused_at_every_operation_answers_by_its_region(FileOrg org)
    {
        List<string> ops;
        using (var m = new World("measure-refuse-commit", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            m.Open("B", commit: false);
            Change(a);
            ops = Measure(a, () => Assert.Null(a.Reg.Commit()));
        }
        int slot = SlotWrite(ops), sync2 = ops.FindIndex(slot, op => op == "S");
        Assert.True(slot > 0 && sync2 > slot, string.Join(' ', ops));
        for (int k = 0; k < ops.Count; k++)
        {
            using var w = new World($"refuse-commit-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: false);
            Change(a);
            int at = a.Log.Count + k;
            a.Refuse = (n, _) => n == at ? Refusal : null;
            string? refused = a.Reg.Commit();
            a.Refuse = null;
            string?[] expected;
            if (k > sync2)
            {
                Assert.Null(refused);
                Assert.Equal(0ul, a.Transaction());
                Assert.Equal("00", b.Write(5, "B005"));   // committed: K5 is gone and nobody holds it
                expected = With(Committed(), 5, "B005");
            }
            else
            {
                Assert.Equal("F", refused);
                Assert.Equal("30", a.Reg.Status("F"));
                if (k == sync2) Assert.Equal(0ul, a.Transaction());       // reversed: nothing left to roll back
                else Assert.NotEqual(0ul, a.Transaction());               // intact
                Assert.Equal(k == sync2 ? "22" : "51", b.Write(5, "B005"));
                expected = Seed();
            }
            Assert.Null(a.Reg.Rollback());   // §14.6.13.1.3 item 8's unconditional ROLLBACK
            Assert.Equal(0ul, a.Transaction());
            Assert.Equal(expected, b.Contents());
            AssertNoTransactionLeft(b.Reg);
            a.End();
            b.End();
            AssertReopened(w, expected);
        }
    }

    /// <summary>2.2.1.16's reversal rows (round 5): sync 2 refused AND the reversal refused at every one of its operations
    /// after it — its statement's reads and writes, its promotion's claim, syncs, slot and retirement. The file is MARKED
    /// UNACKNOWLEDGED and the COMMIT answers '30'; every ROLLBACK answers '30' while the medium refuses and success once a
    /// promotion's sync lands, and then the file is the previous COMMIT's for every later crash: a power loss and an OPEN
    /// find it so.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_refused_reversal_marks_the_file_and_the_rollback_answers_by_its_sync(FileOrg org)
    {
        List<string> ops;
        int sync2;
        using (var m = new World("measure-reversal", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            Change(a);
            int start = a.Log.Count, syncs = -1;
            a.Refuse = RefuseSync2(n => syncs = n);
            ops = Measure(a, () => Assert.Equal("F", a.Reg.Commit()));
            sync2 = syncs - start;
            a.Refuse = null;
            Assert.Equal(0ul, a.Transaction());
        }
        Assert.True(sync2 > 0 && ops.Count > sync2 + 1, string.Join(' ', ops));
        for (int j = sync2 + 1; j < ops.Count; j++)
        {
            using var w = new World($"reversal-{j}", org).Seeded();
            var a = w.Open("A", commit: true);
            Change(a);
            int start = a.Log.Count;
            a.Refuse = (n, _) => n == start + sync2 || n == start + j ? Refusal : null;
            Assert.Equal("F", a.Reg.Commit());
            Assert.Equal("30", a.Reg.Status("F"));
            Assert.NotEqual(0ul, a.Transaction());   // UNACKNOWLEDGED: the transaction is not over until a sync lands
            a.Refuse = (_, op) => op[0] is 'W' or 'S' ? Refusal : null;
            Assert.Equal("F", a.Reg.Rollback());
            a.Refuse = null;
            Assert.Null(a.Reg.Rollback());
            Assert.Equal(0ul, a.Transaction());
            AssertReopened(w, Seed());   // A's connector replays its '30' until its CLOSE (DOC-A.1-105), so another reads
            w.PowerLoss();
            AssertReopened(w, Seed());
        }
    }

    /// <summary>A refusal of A's next sync 2 (the first sync after its first slot write), observed on A's seam.</summary>
    private static Func<int, string, Exception?> RefuseSync2(Action<int>? atSync2 = null)
    {
        int slot = -1, sync2 = -1;
        return (n, op) =>
        {
            if (slot < 0 && op is "W0" or "W1") slot = n;
            if (slot >= 0 && op == "S" && sync2 < 0)
            {
                sync2 = n;
                atSync2?.Invoke(n);
            }
            return n == sync2 ? Refusal : null;
        };
    }

    /// <summary>2.2.1.16's reversal rows (round 5), the owner KILLED at every write of its reversal — its statement, its
    /// promotion's claim, slot and retirement — after sync 2 was refused. During the reversal statement, before its page 2:
    /// the COMMIT's slot landed in the operating environment, so the next writer's restart rule (or the next OPEN's fence
    /// over the PROMOTION SOURCE) completes the COMMIT — committed, and the owner died untold. From the reversal's page 2 on:
    /// page 2 is a DESCENDANT of the COMMIT's slot holding the previous COMMIT's records, which the next writer adopts and
    /// the next OPEN adopts and fences — rolled back. A power loss that loses every unsynced write leaves the claim (sync 1)
    /// and rolls back; one that keeps the COMMIT's slot and loses the reversal's pages finds the PROMOTION SOURCE — committed
    /// — until the reversal's promotion's first sync makes the reversal durable, and rolled back from then on, for every
    /// later crash.</summary>
    [Theory]
    [MemberData(nameof(ReversalContinuations))]
    public void A_commit_killed_inside_its_reversal_is_committed_before_the_reversal_lands_and_rolled_back_after(FileOrg org,
        string continuation)
    {
        List<string> after;
        using (var m = new World("measure-reversal-kill", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            if (continuation == "writer") m.Open("B", commit: false);
            Change(a);
            int sync2 = -1;
            a.Refuse = RefuseSync2(n => sync2 = n);
            int start = a.Log.Count;
            var ops = Measure(a, () => Assert.Equal("F", a.Reg.Commit()));
            a.Refuse = null;
            after = ops.Skip(sync2 - start + 1).Where(op => op[0] == 'W').ToList();
        }
        int reversalPage2 = after.IndexOf("W2"), promotionClaim = after.IndexOf("W2", reversalPage2 + 1);
        Assert.True(reversalPage2 > 0 && promotionClaim > reversalPage2, string.Join(' ', after));
        for (int k = 0; k <= after.Count; k++)
        {
            using var w = new World($"reversal-kill-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = continuation == "writer" ? w.Open("B", commit: false) : null;
            Change(a);
            a.Refuse = RefuseSync2(_ => a.Dying.DieAfter = a.Dying.Writes + k);
            _ = a.Reg.Commit();
            a.Kill();
            bool committed = continuation switch
            {
                "power-loss" => false,
                "power-loss-keeping-the-slot" => k < promotionClaim + 2,
                _ => k <= reversalPage2,
            };
            var expected = committed ? Committed() : Seed();
            if (b is not null)
            {
                Assert.Equal("00", b.Write(9, "B009"));
                Assert.Equal(With(expected, 9, "B009"), b.Contents());
                AssertNoTransactionLeft(b.Reg);
                continue;
            }
            if (continuation == "power-loss") w.PowerLoss();
            else if (continuation == "power-loss-keeping-the-slot")
            {
                long slotSeq = w.Memory.Writes.Last(x => x.Unit == "A" && x.Page < MetaPage.WorkSlot).Seq;
                w.PowerLoss(keep: x => x.Seq == slotSeq);
            }
            AssertReopened(w, expected);
        }
    }

    public static TheoryData<FileOrg, string> ReversalContinuations()
    {
        var data = new TheoryData<FileOrg, string>();
        foreach (var org in KeyedOrgs)
            foreach (var continuation in new[] { "writer", "open", "power-loss", "power-loss-keeping-the-slot" })
                data.Add(org, continuation);
        return data;
    }

    // ── 2.2.1.17: the reversal rows beside a second run unit ─────────────────────────────────────────────────────

    /// <summary>2.2.1.17, "A's COMMIT, sync 2 refused (the reversal) ∥ B's statement": B waits on the mutex A holds from its
    /// claim through the reversal and its promotion, then adopts the reversed state — A's records as at A's previous COMMIT,
    /// no entry of A's — so B never completes a COMMIT A was told failed.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reversed_commit_is_never_completed_by_another_run_unit(FileOrg org)
    {
        using var w = new World("reversed-b", org).Seeded();
        var a = w.Open("A", commit: true);
        var b = w.Open("B", commit: false);
        Change(a);
        a.Refuse = RefuseSync2();
        Assert.Equal("F", a.Reg.Commit());
        a.Refuse = null;
        Assert.Equal("22", b.Write(5, "B005"));
        Assert.Equal(Seed(), b.Contents());
        AssertNoTransactionLeft(b.Reg);
        Assert.Null(a.Reg.Rollback());
        w.PowerLoss();
        AssertReopened(w, Seed());
    }

    /// <summary>2.2.1.17, "A's reversal refused at a page write (the flag left beside the landed slot) ∥ B's statement,
    /// before A's ROLLBACK": B's entry completes A's COMMIT by the restart rule (the slot landed); A's ROLLBACK, finding the
    /// entry gone without its own doing, answers '30' — A is told "unacknowledged", which is the truth — and once B's sync
    /// lands the medium keeps the COMMIT.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reversal_refused_at_its_first_write_is_completed_by_the_next_writer_and_the_rollback_answers_30(FileOrg org)
    {
        using var w = new World("reversal-b", org).Seeded();
        var a = w.Open("A", commit: true);
        var b = w.Open("B", commit: false);
        Change(a);
        int slot = -1, sync2 = -1;
        a.Refuse = (n, op) =>
        {
            if (slot < 0 && op is "W0" or "W1") slot = n;
            if (slot >= 0 && op == "S" && sync2 < 0) sync2 = n;
            return n == sync2 || sync2 >= 0 && op[0] == 'W' ? Refusal : null;
        };
        Assert.Equal("F", a.Reg.Commit());
        a.Refuse = null;
        Assert.NotEqual(0ul, a.Transaction());
        Assert.Equal("00", b.Write(9, "B009"));     // B's entry completes A's COMMIT
        Assert.Equal(With(Committed(), 9, "B009"), b.Contents());
        AssertNoTransactionLeft(b.Reg);
        Assert.Equal("F", a.Reg.Rollback());
        Assert.Equal("30", a.Reg.Status("F"));
        b.End();                                    // B's CLOSE syncs: the medium is settled on the COMMIT
        w.PowerLoss();
        AssertReopened(w, With(Committed(), 9, "B009"));
    }

    /// <summary>2.2.1.17, "A's reversal landed, its promotion's sync refused ∥ B's promotion": B's CLOSE promotes a
    /// descendant of the reversed state, and once B's sync lands A's next ROLLBACK answers success — never while the
    /// medium's highest valid slot is A's COMMIT's.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reversal_whose_promotion_sync_is_refused_is_settled_by_another_run_units_promotion(FileOrg org)
    {
        using var w = new World("reversal-promotion-b", org).Seeded();
        var a = w.Open("A", commit: true);
        var b = w.Open("B", commit: false);
        Change(a);
        int slot = -1, sync2 = -1, syncsAfter = 0;
        a.Refuse = (n, op) =>
        {
            if (slot < 0 && op is "W0" or "W1") slot = n;
            if (slot >= 0 && op == "S" && sync2 < 0) sync2 = n;
            else if (sync2 >= 0 && op == "S") syncsAfter++;
            return n == sync2 || syncsAfter > 0 && op == "S" ? Refusal : null;
        };
        Assert.Equal("F", a.Reg.Commit());
        a.Refuse = null;
        Assert.NotEqual(0ul, a.Transaction());
        Assert.Equal("22", b.Write(5, "B005"));     // B adopts the reversed state
        b.End();
        Assert.Null(a.Reg.Rollback());
        Assert.Equal(0ul, a.Transaction());
        w.PowerLoss();
        AssertReopened(w, Seed());
    }

    // ── 2.2.1.16: ROLLBACK and the dead-owner step ───────────────────────────────────────────────────────────────

    /// <summary>2.2.1.16, "ROLLBACK (one statement)": the owner killed at every write of its ROLLBACK — before its page 2
    /// the next writer's dead-owner step redoes it (idempotent), after it nothing is left to do; the next OPEN the same.
    /// Either way the file is A's last COMMIT's (the seed's) and B's own WRITE stands.</summary>
    [Theory]
    [MemberData(nameof(OrgsByContinuation))]
    public void A_rollback_killed_at_every_write_is_redone_by_the_next_writer(FileOrg org, string continuation)
    {
        int writes;
        using (var m = new World("measure-rollback", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            if (continuation == "writer") m.Open("B", commit: false);
            Change(a);
            writes = Measure(a, () => Assert.Null(a.Reg.Rollback())).Count(op => op[0] == 'W');
        }
        Assert.True(writes > 0);
        for (int k = 0; k <= writes; k++)
        {
            using var w = new World($"kill-rollback-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = continuation == "writer" ? w.Open("B", commit: false) : null;
            Change(a);
            a.Dying.DieAfter = a.Dying.Writes + k;
            _ = a.Reg.Rollback();
            a.Kill();
            if (b is not null)
            {
                Assert.Equal("00", b.Write(9, "B009"));
                Assert.Equal(With(Seed(), 9, "B009"), b.Contents());
                AssertNoTransactionLeft(b.Reg);
                continue;
            }
            if (continuation == "power-loss") w.PowerLoss();
            AssertReopened(w, Seed());
        }
    }

    /// <summary>2.2.1.16's refused row "ROLLBACK, at any page", at every read, write and sync: '30' on the file, the entry,
    /// the UNDO tree and the holdings standing — B's lock tests meet them ('51') while the owner lives — and the next
    /// ROLLBACK, once the medium allows, restores the last COMMIT.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_rollback_refused_at_every_operation_keeps_the_transaction_for_the_next(FileOrg org)
    {
        int ops;
        using (var m = new World("measure-refuse-rollback", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            m.Open("B", commit: false);
            Change(a);
            ops = Measure(a, () => Assert.Null(a.Reg.Rollback())).Count;
        }
        for (int k = 0; k < ops; k++)
        {
            using var w = new World($"refuse-rollback-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: false);
            Change(a);
            int at = a.Log.Count + k;
            a.Refuse = (n, _) => n == at ? Refusal : null;
            Assert.Equal("F", a.Reg.Rollback());
            a.Refuse = null;
            Assert.Equal("30", a.Reg.Status("F"));
            Assert.NotEqual(0ul, a.Transaction());
            Assert.Equal("51", b.Write(5, "B005"));
            AssertHoldingsAreTheUndoKeys(b.Reg);
            Assert.Null(a.Reg.Rollback());
            Assert.Equal(0ul, a.Transaction());
            Assert.Equal("22", b.Write(5, "B005"));
            Assert.Equal(Seed(), b.Contents());
            AssertNoTransactionLeft(b.Reg);
        }
    }

    /// <summary>2.2.1.16's row "ROLLBACK ..., and the dead-owner step" with B, the run unit rolling dead A back at its
    /// entry, itself KILLED at every write of that statement: the next writer's entry redoes the rollback (idempotent). And
    /// 2.2.1.16's refused row "The dead-owner rollback run at A's entry, at any page", at every operation of B's
    /// statement that rolls dead A back: B's statement answers '30' and B's connector replays it until its CLOSE
    /// (§9.1.13.1's permanent error, DOC-A.1-105); the next writer's entry redoes the rollback (idempotent), so the file is
    /// A's last COMMIT's and nothing of A is left.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void The_dead_owner_rollback_killed_or_refused_at_every_point_is_redone_by_the_next_writer(FileOrg org)
    {
        List<string> measured;
        using (var m = new World("measure-dead-owner", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            var b = m.Open("B", commit: false);
            m.Open("C", commit: false);
            Change(a);
            a.Kill();
            measured = Measure(b, () => Assert.Equal("00", b.Write(9, "B009")));
        }
        int ops = measured.Count, writes = measured.Count(op => op[0] == 'W');
        for (int k = 0; k <= writes; k++)
        {
            // Killed at every write instead: its last write is its own WRITE's page 2, so K9 stands only after it.
            using var w = new World($"kill-dead-owner-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: false);
            var c = w.Open("C", commit: false);
            Change(a);
            a.Kill();
            b.Dying.DieAfter = b.Dying.Writes + k;
            _ = b.Write(9, "B009");
            b.Kill();
            Assert.Equal("00", c.Write(6, "C006"));
            var expected = With(Seed(), 6, "C006");
            if (k == writes) expected = With(expected, 9, "B009");
            Assert.Equal(expected, c.Contents());
            AssertNoTransactionLeft(c.Reg);
        }
        for (int k = 0; k < ops; k++)
        {
            using var w = new World($"refuse-dead-owner-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: false);
            var c = w.Open("C", commit: false);
            Change(a);
            a.Kill();
            int at = b.Log.Count + k;
            b.Refuse = (n, _) => n == at ? Refusal : null;
            Assert.Equal("30", b.Write(9, "B009"));
            b.Refuse = null;
            Assert.Equal("30", b.Write(8, "B008"));   // the permanent error replays, touching nothing
            Assert.Equal("00", c.Write(6, "C006"));
            Assert.Equal(With(Seed(), 6, "C006"), c.Contents());
            AssertNoTransactionLeft(c.Reg);
        }
    }

    // ── 2.2.1.16: a transaction's first change ───────────────────────────────────────────────────────────────────

    /// <summary>2.2.1.16, "A transaction's first change ... before page 2": killed at every write, no entry is visible and
    /// the byte went with the handle; refused at every operation (finding R4-2), '30', no transaction, the byte given back,
    /// and another run unit's first change draws an id with no wait and no refusal.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_first_change_killed_or_refused_at_every_point_leaves_no_transaction(FileOrg org)
    {
        List<string> ops;
        using (var m = new World("measure-first", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            m.Open("B", commit: true);
            ops = Measure(a, () => Assert.Equal("00", a.Write(7, "A007")));
        }
        int writes = ops.Count(op => op[0] == 'W');
        for (int k = 0; k < writes; k++)
        {
            using var w = new World($"kill-first-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: true);
            a.Dying.DieAfter = a.Dying.Writes + k;
            _ = a.Write(7, "A007");
            a.Kill();
            Assert.Equal("00", b.Write(8, "B008"));
            Assert.NotEqual(0ul, b.Transaction());
            Assert.Null(b.Reg.Commit());
            Assert.Equal(With(Seed(), 8, "B008"), b.Contents());
            AssertNoTransactionLeft(b.Reg);
        }
        for (int k = 0; k < ops.Count; k++)
        {
            using var w = new World($"refuse-first-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: true);
            int at = a.Log.Count + k;
            a.Refuse = (n, _) => n == at ? Refusal : null;
            Assert.Equal("30", a.Write(7, "A007"));
            a.Refuse = null;
            Assert.Equal(0ul, a.Transaction());
            Assert.Equal("00", b.Write(8, "B008"));
            Assert.NotEqual(0ul, b.Transaction());
            Assert.Null(b.Reg.Commit());
            Assert.Equal(With(Seed(), 8, "B008"), b.Contents());
            AssertNoTransactionLeft(b.Reg);
        }
    }

    // ── 2.2.1.17's round-4 table: the holder's death at every point of another statement ──────────────────────────

    /// <summary>A's statements against B's transaction, by name: WRITE K5 (an identity B deleted), WRITE K7 with A001 (a
    /// unique value B's REWRITE freed; indexed only) and WRITE K9 (an identity nobody holds).</summary>
    public static TheoryData<FileOrg, string> HolderDeathStatements()
    {
        var data = new TheoryData<FileOrg, string>();
        foreach (var org in KeyedOrgs)
            foreach (var statement in org == FileOrg.Indexed ? new[] { "K5", "K7-A001", "K9" } : new[] { "K5", "K9" })
                data.Add(org, statement);
        return data;
    }

    private static string Run(Unit a, string statement) => statement switch
    {
        "K5" => a.Write(5, "Z005"),
        "K7-A001" => a.Write(7, "A001"),
        _ => a.Write(9, "Z009"),
    };

    /// <summary>⛔ 2.2.1.17's round-4 table (finding R4-1): B owns a transaction holding K5 (deleted) and the unique value
    /// A001 (freed), is idle and holds no mutex, so its death can land at ANY instant of A's statement — before A's entry,
    /// after A's entry probe, between A's lookups, after A's claim, between two of A's page writes, after A's page 2. A
    /// holding is a page of the file and a rollback touches holdings only, so A's WRITE K5 and WRITE K7 with A001 answer
    /// '22' (B rolled back at A's entry or test) or '51' (B alive at A's test; the next attempt rolls B back: '22'), never
    /// '00'; A's WRITE K9 answers '00' in every window and no rollback ever overwrites it; K5 reads back as at B's last
    /// COMMIT.</summary>
    [Theory]
    [MemberData(nameof(HolderDeathStatements))]
    public void A_holders_death_at_every_point_of_another_statement_never_lets_it_take_a_held_record(FileOrg org,
        string statement)
    {
        int ops;
        using (var m = new World("measure-holder", org).Seeded())
        {
            var b = m.Open("B", commit: true);
            var a = m.Open("A", commit: false);
            Assert.Equal("00", b.Delete(5));
            Assert.Equal("00", b.Rewrite(1, "X001"));
            b.Reg.ReleaseLivenessForWitness("F");
            ops = Measure(a, () => Run(a, statement)).Count;
        }
        for (int n = 0; n <= ops; n++)
        {
            using var w = new World($"holder-{statement}-{n}", org).Seeded();
            var b = w.Open("B", commit: true);
            var a = w.Open("A", commit: false);
            Assert.Equal("00", b.Delete(5));
            Assert.Equal("00", b.Rewrite(1, "X001"));
            int at = a.Log.Count + n;
            bool died = false;
            void Die()
            {
                if (died) return;
                died = true;
                b.Reg.ReleaseLivenessForWitness("F");
            }
            a.AtOperation = i => { if (i == at) Die(); };
            string answer = Run(a, statement);
            a.AtOperation = null;
            if (statement == "K9") Assert.Equal("00", answer);
            else
            {
                Assert.Contains(answer, new[] { "22", "51" });
                if (answer == "51")
                {
                    Die();
                    Assert.Equal("22", Run(a, statement));
                }
            }
            if (!died) Die();
            Assert.Equal("00", a.Write(8, "Z008"));    // any later entry rolls dead B back
            var expected = With(Seed(), 8, "Z008");
            if (statement == "K9") expected = With(expected, 9, "Z009");
            Assert.Equal(expected, a.Contents());
            AssertNoTransactionLeft(a.Reg);
        }
    }

    // ── 2.2.1.12's adoption matrix, the transaction column ──────────────────────────────────────────────────────

    /// <summary>2.2.1.12 R6 "every matrix row × a transaction open at the crash": A under commit control completed a WRITE
    /// and dies at every write of its next statement (a REWRITE). An INPUT OPEN in the interval reads the file as it stands
    /// (A's completed WRITE included: the dead transaction is not yet rolled back, D.2.5's tolerated read) and leaves it in
    /// place; the first writable OPEN — or, after a power loss, the OPEN that adopts — rolls every before-image of A back.</summary>
    [Theory]
    [MemberData(nameof(OrgsByContinuation))]
    public void A_transaction_open_at_a_crash_inside_its_next_statement_is_rolled_back_by_the_first_writable_open(FileOrg org,
        string continuation)
    {
        int writes;
        using (var m = new World("measure-matrix", org).Seeded())
        {
            var a = m.Open("A", commit: true);
            Assert.Equal("00", a.Write(7, "A007"));
            writes = Measure(a, () => Assert.Equal("00", a.Rewrite(1, "X001"))).Count(op => op[0] == 'W');
        }
        for (int k = 0; k <= writes; k++)
        {
            using var w = new World($"matrix-{k}", org).Seeded();
            var a = w.Open("A", commit: true);
            Assert.Equal("00", a.Write(7, "A007"));
            a.Dying.DieAfter = a.Dying.Writes + k;
            _ = a.Rewrite(1, "X001");
            a.Kill();
            if (continuation == "power-loss") w.PowerLoss();
            else
            {
                var reader = w.Open("R", commit: false, FileOpenMode.Input);
                Assert.Equal(Rec(7, "A007"), reader.Contents()[7]);
                reader.End();
                if (continuation == "writer")
                {
                    var b = w.Open("B", commit: false);
                    Assert.Equal("00", b.Write(9, "B009"));
                    Assert.Equal(With(Seed(), 9, "B009"), b.Contents());
                    AssertNoTransactionLeft(b.Reg);
                    continue;
                }
            }
            AssertReopened(w, Seed());
        }
    }

    /// <summary>2.2.1.12 R6 "an OPEN by a third run unit after one of two holders died, with a transaction": A dies with a
    /// transaction, B lives with its own; C's writable OPEN (or B's next statement, whichever comes first) rolls A back and
    /// leaves B's records and B's transaction untouched — B's holding still answers '51' to C, and B's COMMIT then keeps
    /// them.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_open_after_one_of_two_holders_died_rolls_back_only_the_dead_one(FileOrg org)
    {
        foreach (bool bFirst in new[] { false, true })
        {
            using var w = new World($"two-holders-{bFirst}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: true);
            Change(a);
            Assert.Equal("00", b.Rewrite(2, "B002"));
            a.Kill();
            if (bFirst) Assert.Equal("00", b.Write(9, "B009"));
            var c = w.Open("C", commit: false);
            Assert.NotEqual(0ul, b.Transaction());
            Assert.Equal("51", c.Rewrite(2, "C002"));
            Assert.Equal("22", c.Write(5, "C005"));     // A's DELETE rolled back
            Assert.Null(b.Reg.Commit());
            var expected = With(Seed(), 2, "B002");
            if (bFirst) expected = With(expected, 9, "B009");
            Assert.Equal(expected, c.Contents());
            AssertNoTransactionLeft(c.Reg);
        }
    }

    // ── 2.2.1.17: two run units' transactions ─────────────────────────────────────────────────────────────────────

    /// <summary>2.2.1.17, "A's COMMIT ∥ B's ROLLBACK" in either order: A's changes durable and B's restored; and "B's
    /// transaction begins ∥ A's dead-owner step": a live owner's entry is left exactly as it stands.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_commit_beside_another_run_units_rollback_keeps_the_one_and_restores_the_other(FileOrg org)
    {
        foreach (bool commitFirst in new[] { true, false })
        {
            using var w = new World($"commit-rollback-{commitFirst}", org).Seeded();
            var a = w.Open("A", commit: true);
            var b = w.Open("B", commit: true);
            var plain = w.Open("P", commit: false);
            Change(a);
            Assert.Equal("00", b.Rewrite(2, "B002"));
            Assert.Equal("00", plain.Write(9, "P009"));   // B alive: P's entry leaves B's transaction as it stands
            Assert.NotEqual(0ul, b.Transaction());
            Assert.Equal("51", plain.Rewrite(2, "P002"));
            if (commitFirst) Assert.Null(a.Reg.Commit());
            Assert.Null(b.Reg.Rollback());
            if (!commitFirst) Assert.Null(a.Reg.Commit());
            var expected = With(Committed(), 9, "P009");
            Assert.Equal(expected, plain.Contents());
            AssertNoTransactionLeft(plain.Reg);
            w.PowerLoss();
            AssertReopened(w, expected);
        }
    }

    // ── Power loss: what a COMMIT makes durable ─────────────────────────────────────────────────────────────────

    /// <summary>2.2.1.10 (kb/Work PB3337's first test): COMMIT makes the statements since the last COMMIT survive a power
    /// loss — the memory medium loses every unsynced write — and the statements after it, uncommitted, are rolled back
    /// (or lost) by the OPEN after it; without a COMMIT the power loss leaves the seed.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_commit_survives_a_power_loss_and_what_follows_it_does_not(FileOrg org)
    {
        foreach (bool commit in new[] { true, false })
        {
            using var w = new World($"power-{commit}", org).Seeded();
            var a = w.Open("A", commit: true);
            Change(a);
            if (commit) Assert.Null(a.Reg.Commit());
            Assert.Equal("00", a.Write(8, "A008"));
            w.PowerLoss();
            AssertReopened(w, commit ? Committed() : Seed());
        }
    }

    /// <summary>2.2.1.10's per-file COMMIT (kb/Work PB3454, the owner's "Per file"): one run unit's COMMIT over two
    /// physical files, refused at the second file's slot write, answers '30' on THAT file; the first is committed — it
    /// survives a power loss — and the second is at the previous COMMIT once item 8's unconditional ROLLBACK has run.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_commit_over_two_files_refused_at_the_second_leaves_the_first_committed(FileOrg org)
    {
        using var f = new World("two-files-f", org).Seeded();
        using var g = new World("two-files-g", org).Seeded();
        var a = f.Open("A", commit: true);
        g.Open("A", commit: true, name: "G", into: a);
        Change(a);
        Assert.Equal("00", a.Rewrite(2, "G002", "G"));
        // F is committed first (the order of the run unit's open stores), so the COMMIT's second slot write is G's.
        int slots = 0;
        a.Refuse = (_, op) => op is "W0" or "W1" && ++slots == 2 ? Refusal : null;
        Assert.Equal("G", a.Reg.Commit());
        a.Refuse = null;
        Assert.Equal("30", a.Reg.Status("G"));
        Assert.Equal(0ul, a.Transaction("F"));
        Assert.NotEqual(0ul, a.Transaction("G"));
        Assert.Null(a.Reg.Rollback());
        AssertReopened(g, Seed());   // G's connector replays its '30' until its CLOSE (DOC-A.1-105)
        f.PowerLoss();
        g.PowerLoss();
        AssertReopened(f, Committed());
        AssertReopened(g, Seed());
    }
}
