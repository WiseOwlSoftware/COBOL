// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ APPLY COMMIT, COMMIT AND ROLLBACK IN THE RUNTIME, THROUGH THE CONNECTORS (DESIGN-runtime-library.md 2.2.1.10, in
/// docs/rearchitecture; kb/Work PB3337). Each case is a row of 2.2.1.12's transaction, lock and dead-owner witnesses, run
/// through real connectors of two or three run units (one <see cref="FileRegistry"/> each) on one physical file, both
/// organizations where the row is not an alternate key's; the compiler's half (the clause, the statements' emission and the
/// data items) is kb/Work PB3338's, so the registry calls stand in for the statements. Every expected value is the
/// standard's or the design's:
/// <list type="bullet">
///   <item>§14.9.36.4 GR3: <i>"All files and data-items referenced in active APPLY COMMIT clauses are restored to the state
///   they were in at the execution of the last COMMIT statement or the start of the run unit if no COMMIT statements had
///   previously been executed."</i> — the run unit's OWN changes (2.2.1.10, finding R1-2): another run unit's records are
///   never touched.</item>
///   <item>§14.9.7.4 GR3: <i>"The execution of the COMMIT statement permanently applies any changes made to the files
///   specified in all the active APPLY COMMIT clauses and releases all record locks associated with those files."</i></item>
///   <item>§9.1.16: <i>"The implementor may specify circumstances other than a locked logical record that result in the
///   return of a locked record status."</i> — a record a live transaction changed (deleted ones included) and a unique key
///   value its change freed answer '51' to every other connector (DOC-A.1-152; findings R2-3, R2-4).</item>
///   <item>§9.1.18.5: <i>"When a run unit terminates abnormally, all file changes made since the previous commit or, if
///   none, the beginning of the run unit are rolled back for all files for which there are active APPLY COMMIT
///   clauses."</i> — by the first writable statement after the death (the dead-owner step, finding R3-1), and an INPUT
///   reader in between reads the file as it stands.</item>
///   <item>After EVERY statement of a scenario the HOLDINGS tree's key set equals the union of the UNDO trees' key sets,
///   walked here through the trees themselves (finding R5-2).</item>
/// </list>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class CommitRollbackRuntimeTests
{
    public enum Org { Relative, Indexed }

    public static TheoryData<Org> Orgs() => new() { Org.Relative, Org.Indexed };

    private static string Tmp(string tag) => Path.Combine(Path.GetTempPath(), $"pb3337-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>An 8-character record: the key (relative: the record number) in positions 1-4; for an indexed file an
    /// alternate key WITHOUT DUPLICATES in 5-8 (finding R2-4's unique values).</summary>
    private static FileRegistry Open(string host, Org org, bool commit, FileOpenMode mode = FileOpenMode.IO, string name = "F")
    {
        var reg = new FileRegistry();
        if (org == Org.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
        {
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
            reg.AddAlternateKey(name, [new(4, 4)], duplicates: false);
        }
        reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
        if (commit) reg.RegisterCommitControl(name);
        reg.OpenStatic(name, mode);
        Assert.Equal("00", reg.Status(name));
        return reg;
    }

    private static string Rec(int n, string tail) => $"{n:0000}{tail}";

    private static void Position(FileRegistry reg, Org org, int n, string name = "F")
    {
        if (org == Org.Relative) reg.SetRelativeKey(name, n);
    }

    private static string Write(FileRegistry reg, Org org, int n, string tail, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.WriteShared(name, Rec(n, tail), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    private static string Rewrite(FileRegistry reg, Org org, int n, string tail, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.RewriteShared(name, Rec(n, tail), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    private static string Delete(FileRegistry reg, Org org, int n, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.DeleteShared(name, Rec(n, "    "), FileRetryKind.None, 0);
    }

    private static string Read(FileRegistry reg, Org org, int n, out string image, bool ignoringLock = false,
        FileRecordLock phrase = FileRecordLock.None, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.ReadKeyedShared(name, keyIndex: -1, Rec(n, "    "), phrase, ignoringLock, FileRetryKind.None, 0, out image);
    }

    private static string ReadByAlternate(FileRegistry reg, string value, out string image) =>
        reg.ReadKeyedShared("F", keyIndex: 0, "0000" + value, FileRecordLock.None, ignoringLock: false, FileRetryKind.None, 0,
            out image);

    /// <summary>Records 1 to <paramref name="records"/>, each <c>nnnn</c> + <c>Annn</c> (a unique alternate value), committed.</summary>
    private static void Seed(string host, Org org, int records = 5)
    {
        var seed = Open(host, org, commit: false, FileOpenMode.Output);
        for (int n = 1; n <= records; n++) Assert.Equal("00", Write(seed, org, n, $"A{n:000}"));
        seed.Close("F");
        Assert.Equal("00", seed.Status("F"));
    }

    /// <summary>What a fresh run unit's OPEN INPUT reads of records 1 to 9 (absent: null).</summary>
    private static string?[] Contents(string host, Org org)
    {
        var reader = Open(host, org, commit: false, FileOpenMode.Input, name: "R");
        var records = new string?[10];
        for (int n = 1; n <= 9; n++)
            records[n] = Read(reader, org, n, out var image, name: "R") == "00" ? image : null;
        reader.CloseAll();
        return records;
    }

    private static string?[] Seeded(int records = 5)
    {
        var expected = new string?[10];
        for (int n = 1; n <= records; n++) expected[n] = Rec(n, $"A{n:000}");
        return expected;
    }

    /// <summary>⛔ The drift test of 2.2.1.12 (finding R5-2): the HOLDINGS tree's key set equals the union of the UNDO trees'
    /// key sets, walked through the trees (never through the engine's own enumeration), on the file as
    /// <paramref name="reg"/>'s store last saw it.</summary>
    private static void AssertHoldingsAreTheUndoKeys(FileRegistry reg, string name = "F")
    {
        var file = reg.PagedStoreOf(name)!;
        var holdings = Keys(file, file.Meta.HoldingsRoot);
        var undo = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (_, value) in Entries(file, file.Meta.TransactionsRoot))
            undo.UnionWith(Keys(file, TransactionKeys.PointerOf(value)));
        Assert.Equal(undo, holdings);
    }

    private static SortedSet<string> Keys(PagedFile file, PagePointer root) =>
        new(Entries(file, root).Select(e => Convert.ToHexString(e.Key)), StringComparer.Ordinal);

    private static List<(byte[] Key, byte[] Value)> Entries(PagedFile file, PagePointer root)
    {
        var entries = new List<(byte[], byte[])>();
        if (root.IsNone) return entries;
        Assert.Equal(StoreStatus.Ok, new BTree(file, KeyOrders.Ordinal, root).First(out var cursor));
        while (cursor.HasCurrent)
        {
            Assert.Equal(StoreStatus.Ok, cursor.ReadValue(out var value));
            entries.Add((cursor.Key, value));
            Assert.Equal(StoreStatus.Ok, cursor.Next());
        }
        return entries;
    }

    private static void AssertNoTransactionLeft(FileRegistry reg, string name = "F")
    {
        var meta = reg.PagedStoreOf(name)!.Meta;
        Assert.True(meta.HoldingsRoot.IsNone, "the HOLDINGS tree is empty once no transaction is open");
        Assert.True(meta.TransactionsRoot.IsNone, "the TRANSACTIONS tree is empty once no transaction is open");
    }

    /// <summary>§14.9.36.4 GR3, the run unit's own changes: a WRITE, a REWRITE and a DELETE under commit control, then
    /// ROLLBACK — one statement — leaves the file as at the start of the run unit, and nothing of the transaction behind.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Rollback_restores_the_run_units_changes_to_the_start_of_the_run_unit(Org org)
    {
        string host = Tmp($"rollback-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            AssertHoldingsAreTheUndoKeys(a);
            Assert.Equal("00", Rewrite(a, org, 2, "B002"));
            Assert.Equal("00", Rewrite(a, org, 2, "C002"));   // the FIRST image is the one to restore
            Assert.Equal("00", Delete(a, org, 3));
            AssertHoldingsAreTheUndoKeys(a);
            Assert.NotEqual(0ul, a.TransactionOf("F"));
            Assert.Null(a.Rollback());
            Assert.Equal(0ul, a.TransactionOf("F"));
            AssertNoTransactionLeft(a);
            Assert.Equal("00", Read(a, org, 2, out var two));
            Assert.Equal(Rec(2, "A002"), two);
            if (org == Org.Indexed)
            {
                Assert.Equal("00", ReadByAlternate(a, "A002", out var byOld));   // the alternate entry rebuilt from the image
                Assert.Equal(Rec(2, "A002"), byOld);
                Assert.Equal("23", ReadByAlternate(a, "C002", out _));
            }
            a.CloseAll();
            Assert.Equal(Seeded(), Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§14.9.7.4 GR3: what a COMMIT applied stays applied — a later ROLLBACK restores to THAT COMMIT — and it
    /// releases every lock (another run unit's REWRITE of the record then succeeds).</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Commit_makes_the_changes_permanent_and_a_later_rollback_restores_to_it(Org org)
    {
        string host = Tmp($"commit-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            var b = Open(host, org, commit: false);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            Assert.Equal("51", Rewrite(b, org, 7, "Z007"));   // A's WRITE holds K7 (§14.9.51.4 GR11's implied lock)
            Assert.Null(a.Commit());
            AssertNoTransactionLeft(a);
            Assert.Equal("00", Rewrite(a, org, 7, "B007"));
            Assert.Null(a.Rollback());
            Assert.Equal("00", Read(b, org, 7, out var seven));
            Assert.Equal(Rec(7, "A007"), seven);
            Assert.Equal("00", Rewrite(b, org, 7, "Z007"));   // the COMMIT and the ROLLBACK released everything
            // A READ under the commit posture locks its record (§9.1.16's implicit LOCK MODE IS AUTOMATIC, §12.4.5.9.4
            // GR4) until the next COMMIT or ROLLBACK, in the region like any automatic lock.
            Assert.Equal("00", Read(a, org, 7, out _));
            Assert.Equal("51", Rewrite(b, org, 7, "Y007"));
            Assert.Null(a.Commit());
            Assert.Equal("00", Rewrite(b, org, 7, "Y007"));
            a.CloseAll();
            b.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.12's lock scenarios (findings R2-3, R4-1): A under commit control DELETEs K5 with no READ; B (no commit
    /// control) WRITEs K5 → '51' — the deleted identity is held; A WRITEs K8, B's READ → '51', READ IGNORING LOCK → the
    /// record, REWRITE → '51'; the same with B a connector of A's OWN run unit that no APPLY COMMIT names; A's ROLLBACK
    /// restores K5 and B's refused statements then answer by the file ('22' for K5, '23' for K8).</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_transactions_changes_are_held_against_every_other_connector(Org org)
    {
        string host = Tmp($"locks-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            var b = Open(host, org, commit: false);
            // A sibling connector of A's own run unit, outside the APPLY COMMIT clause (D.2.5's independent connector).
            if (org == Org.Relative)
                a.RegisterRelative("S", host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                    relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
            else
            {
                a.RegisterIndexed("S", host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                    primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
                a.AddAlternateKey("S", [new(4, 4)], duplicates: false);
            }
            a.RegisterSharing("S", FileSharing.AllOther, FileLockMode.Manual, multiple: false);
            a.OpenStatic("S", FileOpenMode.IO);
            Assert.Equal("00", Delete(a, org, 5));
            Assert.Equal("00", Write(a, org, 8, "A008"));
            foreach (var (reg, name) in new[] { (b, "F"), (a, "S") })
            {
                Assert.Equal("51", Write(reg, org, 5, "Z005", name));
                Assert.Equal("51", Read(reg, org, 8, out _, name: name));
                Assert.Equal("00", Read(reg, org, 8, out var ignoring, ignoringLock: true, name: name));
                Assert.Equal(Rec(8, "A008"), ignoring);
                Assert.Equal("51", Rewrite(reg, org, 8, "Z008", name));
            }
            AssertHoldingsAreTheUndoKeys(a);
            Assert.Null(a.Rollback());
            Assert.Equal("22", Write(b, org, 5, "Z005"));
            Assert.Equal("23", Rewrite(b, org, 8, "Z008"));
            Assert.Equal("00", Read(b, org, 5, out var five));
            Assert.Equal(Rec(5, "A005"), five);
            a.CloseAll();
            b.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>Finding R2-4 and R5-2 (indexed): A REWRITEs K1's unique key A001 → X001 → Y001 and WRITEs K8 with W008 then
    /// DELETEs K8. B's WRITE with A001 (the value A's change freed) → '51', with Y001 → '22'; X001 and W008 are held too
    /// (freed by A's later changes, in no before-image). After A's COMMIT — or its ROLLBACK — every one of B's WRITEs
    /// answers by the file alone, and the HOLDINGS tree is EMPTY: no holding leaked.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Every_holding_of_a_transaction_is_found_by_its_commit_or_rollback(bool commit)
    {
        string host = Tmp($"holdings-{commit}");
        try
        {
            Seed(host, Org.Indexed);
            var a = Open(host, Org.Indexed, commit: true);
            var b = Open(host, Org.Indexed, commit: false);
            Assert.Equal("00", Rewrite(a, Org.Indexed, 1, "X001"));
            Assert.Equal("00", Rewrite(a, Org.Indexed, 1, "Y001"));
            Assert.Equal("00", Write(a, Org.Indexed, 8, "W008"));
            Assert.Equal("00", Delete(a, Org.Indexed, 8));
            AssertHoldingsAreTheUndoKeys(a);
            Assert.Equal("51", Write(b, Org.Indexed, 9, "A001"));
            Assert.Equal("51", Write(b, Org.Indexed, 9, "X001"));
            Assert.Equal("51", Write(b, Org.Indexed, 9, "W008"));
            Assert.Equal("22", Write(b, Org.Indexed, 9, "Y001"));   // invalid key first: K1 holds Y001 (GR42 ignores locks)
            Assert.Null(commit ? a.Commit() : a.Rollback());
            AssertNoTransactionLeft(a);
            Assert.Equal("00", Write(b, Org.Indexed, 9, "X001"));
            Assert.Equal("00", Write(b, Org.Indexed, 6, "W008"));
            Assert.Equal(commit ? "00" : "22", Write(b, Org.Indexed, 7, "A001"));   // ROLLBACK gave K1 back A001
            AssertNoTransactionLeft(b);
            a.CloseAll();
            b.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.12's dead owner (finding R3-1): A under commit control DELETEs K5 and REWRITEs K1, and dies (its later
    /// writes never reach the medium; the operating environment frees its liveness byte with its handle). An INPUT reader
    /// between the death and the next writer reads the file as it stands (K5 absent). B, live and not under commit control,
    /// then WRITEs K5: its entry rolls A back first, so the WRITE answers '22', and K1 and K5 read back as at A's last
    /// COMMIT; nothing of A is left in the file.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_dead_owner_is_rolled_back_by_the_next_writable_statement(Org org)
    {
        string host = Tmp($"dead-{org}");
        try
        {
            Seed(host, org);
            DyingPageMedium? medium = null;
            KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
            FileRegistry a;
            try { a = Open(host, org, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            var b = Open(host, org, commit: false);
            Assert.Equal("00", Delete(a, org, 5));
            Assert.Equal("00", Rewrite(a, org, 1, "X001"));
            medium!.DieAfter = medium.Writes;   // A dies here: nothing it does afterwards reaches the file
            a.CloseAll();                       // its handles and their bytes go with it

            var c = Open(host, org, commit: false, FileOpenMode.Input, name: "C");
            Assert.Equal("23", Read(c, org, 5, out _, name: "C"));   // the file as it stands, dead transaction included
            Assert.Equal("22", Write(b, org, 5, "Z005"));
            if (org == Org.Indexed) Assert.Equal("22", Write(b, org, 7, "A005"));   // K5's unique value is K5's again
            Assert.Equal("00", Read(b, org, 1, out var one));
            Assert.Equal(Rec(1, "A001"), one);
            AssertNoTransactionLeft(b);
            Assert.Equal("00", Read(c, org, 5, out var five, name: "C"));   // the reader adopts the restored file
            Assert.Equal(Rec(5, "A005"), five);
            b.CloseAll();
            c.CloseAll();
            Assert.Equal(Seeded(), Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.12's round-4 row (finding R4-1) through the RETRY phrase (§14.9.51.4 GR16, <i>"for the case where
    /// resources needed to write a record are locked by another run unit"</i>): B's WRITE K5 WITH RETRY meets A's holding
    /// while A lives ('51' at the first attempt); A dies during B's wait, outside B's mutex; B's next attempt re-enters,
    /// finds A dead, rolls A back and evaluates the WRITE again: '22', never '00'. B's WRITE K9, an identity nobody holds,
    /// answers '00' and is never overwritten by A's rollback.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_holder_that_dies_during_a_retry_is_rolled_back_and_the_write_evaluated_again(Org org)
    {
        string host = Tmp($"retry-{org}");
        try
        {
            Seed(host, org);
            DyingPageMedium? medium = null;
            KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
            FileRegistry a;
            try { a = Open(host, org, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            var b = Open(host, org, commit: false);
            Assert.Equal("00", Delete(a, org, 5));
            Assert.Equal("00", Write(b, org, 9, "Z009"));
            Assert.Equal("51", Write(b, org, 5, "Z005"));   // A alive at B's test, no RETRY phrase
            int pauses = 0;
            FileRegistry.PauseObserver.Value = _ =>
            {
                if (pauses++ > 0) return;
                medium!.DieAfter = medium.Writes;   // A dies while B waits outside its statement
                a.CloseAll();
            };
            string retried;
            try
            {
                Position(b, org, 5);
                retried = b.WriteShared("F", Rec(5, "Z005"), -1, FileRecordLock.None, FileRetryKind.Times, 3, page: null);
            }
            finally { FileRegistry.PauseObserver.Value = null; }
            Assert.Equal(1, pauses);
            Assert.Equal("22", retried);
            Assert.Equal("00", Read(b, org, 5, out var five));
            Assert.Equal(Rec(5, "A005"), five);
            Assert.Equal("00", Read(b, org, 9, out var nine));
            Assert.Equal(Rec(9, "Z009"), nine);
            AssertNoTransactionLeft(b);
            b.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.12's transaction witness: two run units under APPLY COMMIT on disjoint records. A's ROLLBACK restores
    /// A's records and leaves B's as B left them; B's COMMIT keeps B's; and a run unit with no commit control keeps every
    /// completed statement through both.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Two_transactions_on_one_file_roll_back_only_their_own_records(Org org)
    {
        string host = Tmp($"two-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            var b = Open(host, org, commit: true);
            var plain = Open(host, org, commit: false, name: "P");
            Assert.Equal("00", Rewrite(a, org, 1, "X001"));
            Assert.Equal("00", Rewrite(b, org, 2, "X002"));
            Assert.Equal("00", Write(plain, org, 6, "A006", "P"));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            Assert.Equal("00", Delete(b, org, 4));
            Assert.NotEqual(a.TransactionOf("F"), b.TransactionOf("F"));
            Assert.Null(a.Rollback());
            Assert.Null(b.Commit());
            a.CloseAll();
            b.CloseAll();
            plain.CloseAll();
            var expected = Seeded();
            expected[2] = Rec(2, "X002");
            expected[4] = null;
            expected[6] = Rec(6, "A006");
            Assert.Equal(expected, Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§14.9.7.4 GR4 / §9.1.15 (<i>"the file lock is removed by the COMMIT statement"</i>): a connector CLOSEd under
    /// commit control keeps its handle, so a ROLLBACK after the CLOSE still restores; a later OPEN INPUT finds the record
    /// the transaction had written absent ('23').</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_rollback_after_a_close_restores_the_last_commit(Org org)
    {
        string host = Tmp($"close-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            Assert.Null(a.Rollback());
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("23", Read(a, org, 7, out _));
            a.CloseAll();
            Assert.Equal(Seeded(), Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>§9.1.18.5: an ABNORMAL ending rolls the run unit's changes back (the file-only rollback of §14.6.12); a
    /// NORMAL ending commits them.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Termination_commits_a_normal_ending_and_rolls_back_an_abnormal_one(bool abnormal)
    {
        string host = Tmp($"end-{abnormal}");
        try
        {
            Seed(host, Org.Indexed);
            var a = Open(host, Org.Indexed, commit: true);
            Assert.Equal("00", Write(a, Org.Indexed, 7, "A007"));
            if (abnormal) a.EndTransactions(abnormal: true);
            a.CloseAll();
            var expected = Seeded();
            if (!abnormal) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Contents(host, Org.Indexed));
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.10's ceilings: the commit posture's READ locks have no ceiling but the host's (DOC-A.1-154/155) — a
    /// transaction that READs and REWRITEs 300 records answers no '53' or '54'.</summary>
    [Fact]
    public void A_transaction_of_300_records_meets_no_lock_ceiling()
    {
        string host = Tmp("ceiling");
        try
        {
            Seed(host, Org.Relative, records: 300);
            var a = Open(host, Org.Relative, commit: true);
            for (int n = 1; n <= 300; n++)
            {
                Assert.Equal("00", Read(a, Org.Relative, n, out _));
                Assert.Equal("00", Rewrite(a, Org.Relative, n, "B000"));
            }
            Assert.Null(a.Commit());
            a.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.12's R4-2 row: a first change under commit control refused at its first page write answers '30';
    /// the run unit holds no liveness byte and no transaction afterwards, and another run unit's first change draws an id
    /// with no wait and no refusal.</summary>
    [Fact]
    public void A_refused_first_change_gives_its_byte_back()
    {
        string host = Tmp("first");
        try
        {
            Seed(host, Org.Relative);
            bool refuse = false;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: page => refuse && page >= MetaPage.MetaPages ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, Org.Relative, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            refuse = true;
            Assert.Equal("30", Write(a, Org.Relative, 7, "A007"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            var b = Open(host, Org.Relative, commit: true);
            Assert.Equal("00", Write(b, Org.Relative, 8, "A008"));
            Assert.NotEqual(0ul, b.TransactionOf("F"));
            Assert.Null(b.Commit());
            a.CloseAll();
            b.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.10's COMMIT regions (finding R5-1), told as §14.6.13.1.3 item 8's fatal condition: '30' on the refused
    /// file. Refused at its SLOT WRITE (region 1): the transaction is intact and the unconditional ROLLBACK restores it.
    /// Its SYNC 2 refused (region 3): the COMMIT reverses under its own mutex hold and reports failure, and every later
    /// OPEN reads the previous COMMIT's file. In no run is a COMMIT reported failed with its transaction committed.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_refused_commit_is_never_reported_failed_with_its_transaction_committed(bool refuseSync)
    {
        string host = Tmp($"refused-{refuseSync}");
        try
        {
            Seed(host, Org.Indexed);
            bool armed = false;
            int syncs = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: page => armed && !refuseSync && page < MetaPage.WorkSlot ? new IOException("slot refused") : null,
                sync: () => armed && refuseSync && ++syncs == 2 ? new IOException("sync 2 refused") : null);
            FileRegistry a;
            try { a = Open(host, Org.Indexed, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(a, Org.Indexed, 7, "A007"));
            Assert.Equal("00", Rewrite(a, Org.Indexed, 1, "X001"));
            armed = true;
            Assert.Equal("F", a.Commit());
            Assert.Equal("30", a.Status("F"));
            armed = false;
            Assert.Null(a.Rollback());   // item 8's unconditional ROLLBACK
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
            Assert.Equal(Seeded(), Contents(host, Org.Indexed));
        }
        finally { TryDelete(host); }
    }
}
