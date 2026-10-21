// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;
using static CobolNet.Tests.Unit.Store.TransactionTrees;

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
    public static TheoryData<FileOrg> Orgs() => [.. KeyedOrgs];

    /// <summary>An 8-character record: the key (relative: the record number) in positions 1-4; for an indexed file an
    /// alternate key WITHOUT DUPLICATES in 5-8 (finding R2-4's unique values).</summary>
    private static FileRegistry Open(string host, FileOrg org, bool commit, FileOpenMode mode = FileOpenMode.IO, string name = "F",
        FileSharing sharing = FileSharing.AllOther)
    {
        var reg = new FileRegistry();
        Declare(reg, host, org, commit, name, sharing);
        reg.OpenStatic(name, mode);
        Assert.Equal("00", reg.Status(name));
        return reg;
    }

    /// <summary>Register connector <paramref name="name"/> in <paramref name="reg"/> as <see cref="Open"/> declares it.</summary>
    private static void Declare(FileRegistry reg, string host, FileOrg org, bool commit, string name = "F",
        FileSharing sharing = FileSharing.AllOther)
    {
        if (org == FileOrg.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
        {
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: false, accessMode: (int)KeyedAccess.Dynamic,
                primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
            reg.AddAlternateKey(name, [new(4, 4)], duplicates: false);
        }
        reg.RegisterSharing(name, sharing, LockModeRequiredBy(sharing), multiple: false);
        if (commit) reg.RegisterCommitControl(name);
    }

    private static void Position(FileRegistry reg, FileOrg org, int n, string name = "F")
    {
        if (org == FileOrg.Relative) reg.SetRelativeKey(name, n);
    }

    private static string Write(FileRegistry reg, FileOrg org, int n, string tail, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.WriteShared(name, Rec(n, tail), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    private static string Rewrite(FileRegistry reg, FileOrg org, int n, string tail, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.RewriteShared(name, Rec(n, tail), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    private static string Delete(FileRegistry reg, FileOrg org, int n, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.DeleteShared(name, Rec(n, "    "), FileRetryKind.None, 0);
    }

    private static string Read(FileRegistry reg, FileOrg org, int n, out string image, bool ignoringLock = false,
        FileRecordLock phrase = FileRecordLock.None, string name = "F")
    {
        Position(reg, org, n, name);
        return reg.ReadKeyedShared(name, keyIndex: -1, Rec(n, "    "), phrase, ignoringLock, FileRetryKind.None, 0, out image);
    }

    private static string ReadByAlternate(FileRegistry reg, string value, out string image) =>
        reg.ReadKeyedShared("F", keyIndex: 0, "0000" + value, FileRecordLock.None, ignoringLock: false, FileRetryKind.None, 0,
            out image);

    /// <summary>Records 1 to <paramref name="records"/>, each <c>nnnn</c> + <c>Annn</c> (a unique alternate value), committed.</summary>
    private static void Seed(string host, FileOrg org, int records = 5)
    {
        var seed = Open(host, org, commit: false, FileOpenMode.Output);
        for (int n = 1; n <= records; n++) Assert.Equal("00", Write(seed, org, n, $"A{n:000}"));
        seed.Close("F");
        Assert.Equal("00", seed.Status("F"));
    }

    /// <summary>What a fresh run unit's OPEN INPUT reads of records 1 to 9 (absent: null).</summary>
    private static string?[] Contents(string host, FileOrg org)
    {
        var reader = Open(host, org, commit: false, FileOpenMode.Input, name: "R");
        var records = new string?[10];
        for (int n = 1; n <= 9; n++)
            records[n] = Read(reader, org, n, out var image, name: "R") == "00" ? image : null;
        reader.CloseAll();
        return records;
    }

    /// <summary>What a fresh run unit reads after its WRITABLE OPEN, whose dead-owner step (2.2.1.6 R6 step 5) rolls back
    /// every transaction left without a live owner — so a change reported committed reads back only if it truly is.</summary>
    private static string?[] Settled(string host, FileOrg org)
    {
        var writer = Open(host, org, commit: false, name: "S");
        AssertNoTransactionLeft(writer, "S");
        writer.CloseAll();
        return Contents(host, org);
    }

    private static string?[] Seeded(int records = 5)
    {
        var expected = new string?[10];
        for (int n = 1; n <= records; n++) expected[n] = Rec(n, $"A{n:000}");
        return expected;
    }

    /// <summary>§14.9.36.4 GR3, the run unit's own changes: a WRITE, a REWRITE and a DELETE under commit control, then
    /// ROLLBACK — one statement — leaves the file as at the start of the run unit, and nothing of the transaction behind.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Rollback_restores_the_run_units_changes_to_the_start_of_the_run_unit(FileOrg org)
    {
        string host = Tmp($"pb3337-rollback-{org}");
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
            if (org == FileOrg.Indexed)
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
    public void Commit_makes_the_changes_permanent_and_a_later_rollback_restores_to_it(FileOrg org)
    {
        string host = Tmp($"pb3337-commit-{org}");
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
    public void A_transactions_changes_are_held_against_every_other_connector(FileOrg org)
    {
        string host = Tmp($"pb3337-locks-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            var b = Open(host, org, commit: false);
            // A sibling connector of A's own run unit, outside the APPLY COMMIT clause (D.2.5's independent connector).
            if (org == FileOrg.Relative)
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
        string host = Tmp($"pb3337-holdings-{commit}");
        try
        {
            Seed(host, FileOrg.Indexed);
            var a = Open(host, FileOrg.Indexed, commit: true);
            var b = Open(host, FileOrg.Indexed, commit: false);
            Assert.Equal("00", Rewrite(a, FileOrg.Indexed, 1, "X001"));
            Assert.Equal("00", Rewrite(a, FileOrg.Indexed, 1, "Y001"));
            Assert.Equal("00", Write(a, FileOrg.Indexed, 8, "W008"));
            Assert.Equal("00", Delete(a, FileOrg.Indexed, 8));
            AssertHoldingsAreTheUndoKeys(a);
            Assert.Equal("51", Write(b, FileOrg.Indexed, 9, "A001"));
            Assert.Equal("51", Write(b, FileOrg.Indexed, 9, "X001"));
            Assert.Equal("51", Write(b, FileOrg.Indexed, 9, "W008"));
            Assert.Equal("22", Write(b, FileOrg.Indexed, 9, "Y001"));   // invalid key first: K1 holds Y001 (GR42 ignores locks)
            Assert.Null(commit ? a.Commit() : a.Rollback());
            AssertNoTransactionLeft(a);
            Assert.Equal("00", Write(b, FileOrg.Indexed, 9, "X001"));
            Assert.Equal("00", Write(b, FileOrg.Indexed, 6, "W008"));
            Assert.Equal(commit ? "00" : "22", Write(b, FileOrg.Indexed, 7, "A001"));   // ROLLBACK gave K1 back A001
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
    public void A_dead_owner_is_rolled_back_by_the_next_writable_statement(FileOrg org)
    {
        string host = Tmp($"pb3337-dead-{org}");
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
            if (org == FileOrg.Indexed) Assert.Equal("22", Write(b, org, 7, "A005"));   // K5's unique value is K5's again
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
    public void A_holder_that_dies_during_a_retry_is_rolled_back_and_the_write_evaluated_again(FileOrg org)
    {
        string host = Tmp($"pb3337-retry-{org}");
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
    public void Two_transactions_on_one_file_roll_back_only_their_own_records(FileOrg org)
    {
        string host = Tmp($"pb3337-two-{org}");
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
    public void A_rollback_after_a_close_restores_the_last_commit(FileOrg org)
    {
        string host = Tmp($"pb3337-close-{org}");
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

    /// <summary>§14.9.7.4 GR4 (<i>"For files specified in active APPLY COMMIT clauses that have been closed and not reopened
    /// prior to the COMMIT statement, the file locks are released."</i>) — so until then they are not: while a connector
    /// CLOSEd under commit control keeps its file lock, another run unit's OPEN WITH NO OTHER is refused '61' (§9.1.13.9
    /// 1), Table 19), on a host whose share modes are the lock and on one whose run-unit lock is the published column alike;
    /// the COMMIT (or the ROLLBACK) releases it and the same OPEN then succeeds. §9.1.15 (<i>"The file lock is removed by an
    /// explicit or implicit CLOSE statement executed for that file connector, except where the file is subject to an
    /// active APPLY COMMIT clause."</i>) keys the rule on the clause, so a connector that only READ before its CLOSE keeps
    /// the lock as one that changed a record does (<paramref name="changed"/>; review finding R1-2). A re-OPEN of the closed
    /// connector itself replaces the kept lock and is never refused by it, and the run unit's end releases the lock its
    /// implicit CLOSE kept (§9.1.18.5, <i>"In both cases all file and record locks are then released"</i>).</summary>
    [Theory]
    [InlineData(FileOrg.Relative, true, true)]
    [InlineData(FileOrg.Relative, false, true)]
    [InlineData(FileOrg.Indexed, true, true)]
    [InlineData(FileOrg.Indexed, false, true)]
    [InlineData(FileOrg.Relative, true, false)]
    [InlineData(FileOrg.Indexed, false, false)]
    public void A_file_closed_under_commit_control_keeps_its_file_lock_until_the_commit(FileOrg org, bool commit, bool changed)
    {
        string host = Tmp($"pb3337-gr4-{org}-{commit}-{changed}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", changed ? Write(a, org, 7, "A007") : Read(a, org, 1, out _));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            var b = new FileRegistry();
            Register(b, "B", host, org, FileSharing.NoOther);
            if (org == FileOrg.Indexed) b.AddAlternateKey("B", [new(4, 4)], duplicates: false);   // GR10: the same attributes
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("61", b.Status("B"));
            Assert.Null(commit ? a.Commit() : a.Rollback());
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            b.Close("B");
            Assert.Equal("00", b.Status("B"));
            // The connector's own re-OPEN after a CLOSE under commit control: the kept lock is replaced, not a refusal.
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 8, "A008"));
            a.Close("F");
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            a.CloseAll();
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            b.CloseAll();
            var expected = Seeded();
            if (commit && changed) expected[7] = Rec(7, "A007");
            expected[8] = Rec(8, "A008");
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A TRANSACTION CARRIES OVER A CLOSE AND A RE-OPEN OF ITS CONNECTOR (§14.9.7.4 GR3, §14.9.36.4 GR3; review
    /// finding R1-1): the CLOSE under commit control keeps the connector's attachment to the run unit's store, and the
    /// re-OPEN carries it on — the run unit reads its own uncommitted record, changes more in the same transaction (its id
    /// unchanged), and the COMMIT makes every change permanent, or the ROLLBACK restores the start, as a later WRITABLE OPEN
    /// reads it (its dead-owner step rolls back any transaction left without a live owner). Both re-OPEN shapes: one that
    /// takes the same handle back (I-O after I-O) and one that takes another (INPUT after I-O) — beside the kept handle
    /// under SHARING WITH ALL OTHER, after giving it back under SHARING WITH NO OTHER (the I-O default), whose share mode
    /// refuses any second handle. Through the INPUT re-OPEN — whose handle is writable while the transaction stands, the
    /// handle the COMMIT or ROLLBACK writes through (2.2.1.10 As built (3)) — the transaction stays alive: another run
    /// unit's REWRITE of the record it wrote answers '51' (§9.1.16, DOC-A.1-152), never rolls it back.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, FileSharing.AllOther, true)]
    [InlineData(FileOrg.Relative, FileSharing.AllOther, false)]
    [InlineData(FileOrg.Relative, FileSharing.NoOther, true)]
    [InlineData(FileOrg.Relative, FileSharing.NoOther, false)]
    [InlineData(FileOrg.Indexed, FileSharing.AllOther, true)]
    [InlineData(FileOrg.Indexed, FileSharing.AllOther, false)]
    [InlineData(FileOrg.Indexed, FileSharing.NoOther, true)]
    [InlineData(FileOrg.Indexed, FileSharing.NoOther, false)]
    public void A_transaction_carries_over_a_close_and_reopen_of_its_connector(FileOrg org, FileSharing sharing, bool commit)
    {
        string host = Tmp($"pb3337-reopen-{org}-{sharing}-{commit}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: sharing);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            Assert.Equal("00", Read(a, org, 7, out var own));
            Assert.Equal(Rec(7, "A007"), own);
            Assert.Equal("00", Write(a, org, 8, "A008"));
            AssertHoldingsAreTheUndoKeys(a);
            a.Close("F");
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            Assert.Equal("00", Read(a, org, 8, out own));
            Assert.Equal(Rec(8, "A008"), own);
            if (sharing == FileSharing.AllOther)
            {
                var b = Open(host, org, commit: false, name: "B");
                Assert.Equal("51", Rewrite(b, org, 7, "B007", name: "B"));
                b.CloseAll();
            }
            Assert.Null(commit ? a.Commit() : a.Rollback());
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
            var expected = Seeded();
            if (commit) (expected[7], expected[8]) = (Rec(7, "A007"), Rec(8, "A008"));
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A RE-OPEN WITH NO TRANSACTION STANDING TAKES THE ACCESS OF ITS OWN MODE (review finding R2-1; §9.1.15 3),
    /// <i>"The successful opening of a file establishes a file lock for the applicable sharing rules, thereby preventing other
    /// run units from opening that file with incompatible sharing rules"</i>): a connector under commit control that only
    /// READ is CLOSEd — its handle kept — and re-OPENed INPUT under SHARING WITH ALL OTHER; another run unit's OPEN under
    /// SHARING WITH READ ONLY, INPUT or I-O, is a normal open beside that INPUT opener (Table 19), so it answers '00' and
    /// reads, and the COMMIT, which has nothing to write, succeeds. Before the fix the re-OPEN's handle was writable and the
    /// host's share mode refused the other OPEN '61'.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, FileOpenMode.Input)]
    [InlineData(FileOrg.Relative, FileOpenMode.IO)]
    [InlineData(FileOrg.Indexed, FileOpenMode.Input)]
    [InlineData(FileOrg.Indexed, FileOpenMode.IO)]
    public void A_reopen_with_no_transaction_standing_admits_what_Table_19_admits_beside_its_mode(FileOrg org, FileOpenMode other)
    {
        string host = Tmp($"pb3337-reopen-read-{org}-{other}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Read(a, org, 1, out _));
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("00", a.Status("F"));
            var b = new FileRegistry();
            Declare(b, host, org, commit: false, name: "B", sharing: FileSharing.ReadOnly);
            b.OpenStatic("B", other);
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", Read(b, org, 1, out var image, name: "B"));
            Assert.Equal(Rec(1, "A001"), image);
            b.CloseAll();
            Assert.Null(a.Commit());
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ AN UNSUCCESSFUL RE-OPEN LEAVES WHAT THE CLOSE KEPT AS IT WAS (review lead R2-2; §14.9.27.4 GR25, <i>"If the
    /// execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.7.4 GR4, <i>"For files specified
    /// in active APPLY COMMIT clauses that have been closed and not reopened prior to the COMMIT statement, the file locks are
    /// released."</i>): a connector under commit control and SHARING WITH ALL OTHER changes a record and is CLOSEd; its re-OPEN
    /// INPUT adopts the kept handle (the transaction stands, so the handle is the writable one) and meets a medium that refuses
    /// every page read, write and sync ('30', §9.1.13.6 item 1). The re-OPEN published its own Table 19 column (all other,
    /// INPUT) on the kept lock's hold and the unsuccessful OPEN put the kept column (all other, non-INPUT) back, so another run
    /// unit's OPEN I-O SHARING WITH READ ONLY — which Table 19 admits beside INPUT and refuses beside I-O — is refused '61'
    /// (§9.1.13.9 1)) until the ROLLBACK, which restores the record and releases the lock, after which the same OPEN succeeds.
    /// (Review lead R3-2 of PB3337: with A SHARING WITH NO OTHER its handle refused the other OPEN on every host by itself and
    /// the case passed with the column restore removed. The kept handle here admits the other OPEN's share mode on a host that
    /// carries the column lock instead of mandatory share modes, so there only the restored column refuses it.)</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_unsuccessful_reopen_leaves_the_kept_file_lock_standing(FileOrg org)
    {
        string host = Tmp($"pb3337-reopen-refused-{org}");
        try
        {
            Seed(host, org);
            bool refuse = false;
            IOException? Refusal() => refuse ? new IOException("refused by the host") : null;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, write: _ => Refusal(), sync: Refusal,
                read: _ => Refusal());
            FileRegistry a;
            try { a = Open(host, org, commit: true, sharing: FileSharing.AllOther); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.PagedStoreOf("F")!.DropCache();   // the kept attachment's store: the re-OPEN reads its pages from the medium
            refuse = true;
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("30", a.Status("F"));
            refuse = false;
            var b = new FileRegistry();
            Declare(b, host, org, commit: false, name: "B", sharing: FileSharing.ReadOnly);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("61", b.Status("B"));
            Assert.Null(a.Rollback());
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            b.CloseAll();
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A RE-OPEN ANOTHER RUN UNIT REFUSES KEEPS THE HANDLE THE CLOSE KEPT (kb/Work PB3483; §9.1.13.9 1), <i>"A file
    /// sharing conflict condition exists because an OPEN statement is attempted on a physical file and that physical file is
    /// already open by another file connector in a manner that conflicts with this request"</i>; §14.9.27.4 GR25, <i>"If the
    /// execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.7.4 GR4, the file locks of a file
    /// closed and not reopened are released only by the COMMIT): A, under commit control and SHARING WITH ALL OTHER, changes a
    /// record and is CLOSEd; B opens the file I-O beside A's kept handle (Table 19: all other beside all other); A's re-OPEN
    /// under SHARING WITH READ ONLY is refused '61' by B — and by A's own kept handle, whose share mode a host that
    /// enforces them per handle (Windows) lets refuse the new one, which is why the OPEN gives the kept handle back and tries
    /// again. B's refusal must not cost A the kept handle: the transaction's liveness byte stays held by a live run unit,
    /// so a third run unit's writable OPEN (its dead-owner step) does not roll the transaction back and its REWRITE of the
    /// record answers '51'; and the file lock stands, so an OPEN SHARING WITH NO OTHER is refused '61' after B has gone.
    /// Before the fix the give-back was never undone, and both answered as if A held nothing. The same re-OPEN, once B has
    /// closed, succeeds ('00') and is the transaction's. (A host whose share modes are advisory refuses the OPEN through the
    /// Table 19 column lock before any handle is taken, so there the case passes without the restore.)</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_refused_by_another_run_unit_keeps_the_kept_handle_and_its_transaction(FileOrg org)
    {
        string host = Tmp($"pb3483-reopen-refused-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.AllOther);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            var b = Open(host, org, commit: false, name: "B", sharing: FileSharing.AllOther);
            a.RegisterSharing("F", FileSharing.ReadOnly, FileLockMode.Automatic, multiple: true);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("61", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            // A third run unit's writable OPEN runs the dead-owner step: A is alive, so it rolls nothing back.
            var c = Open(host, org, commit: false, name: "C", sharing: FileSharing.AllOther);
            Assert.Equal("51", Rewrite(c, org, 7, "C007", name: "C"));
            c.CloseAll();
            b.CloseAll();
            // The kept handle's file lock stands for the connector that is closed and not reopened.
            var d = new FileRegistry();
            Declare(d, host, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("61", d.Status("D"));
            // Nobody refuses it now: the same re-OPEN is the transaction's.
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            Assert.Equal("00", Read(a, org, 7, out var own));
            Assert.Equal(Rec(7, "A007"), own);
            Assert.Null(a.Rollback());
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A RE-OPEN THAT IS UNSUCCESSFUL AFTER IT SWAPPED HANDLES PUTS THE KEPT HANDLE BACK (kb/Work PB3483; §14.9.27.4
    /// GR25, <i>"If the execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.7.4 GR4): A, under
    /// commit control and SHARING WITH ALL OTHER, changes a record and is CLOSEd; its re-OPEN under SHARING WITH NO OTHER takes
    /// a handle the kept one's share mode refuses, so the kept handle is given back and the new one taken — and the medium then
    /// refuses the adoption's first read ('30', §9.1.13.6 item 1). The kept handle is back as the CLOSE left it: the
    /// transaction's liveness byte is held by a live run unit, so another run unit's writable OPEN does not roll it back (its
    /// REWRITE of the record answers '51', never '23' for a record the rollback removed), the file lock stands (an OPEN SHARING
    /// WITH NO OTHER is refused '61', §9.1.13.9 1)), and the ROLLBACK restores the file.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_unsuccessful_reopen_after_the_handle_swap_gives_the_kept_handle_back(FileOrg org)
    {
        string host = Tmp($"pb3483-swap-refused-{org}");
        try
        {
            Seed(host, org);
            bool refuseOnce = false;
            IOException? Refusal()
            {
                if (!refuseOnce) return null;
                refuseOnce = false;
                return new IOException("refused by the host");
            }
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, write: _ => null, sync: () => null, read: _ => Refusal());
            FileRegistry a;
            try { a = Open(host, org, commit: true, sharing: FileSharing.AllOther); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.RegisterSharing("F", FileSharing.NoOther, FileLockMode.Automatic, multiple: true);
            refuseOnce = true;
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("30", a.Status("F"));
            Assert.False(refuseOnce);
            Assert.Equal(id, a.TransactionOf("F"));
            var c = Open(host, org, commit: false, name: "C", sharing: FileSharing.AllOther);   // the kept lock admits it (Table 19)
            Assert.Equal("51", Rewrite(c, org, 7, "C007", name: "C"));
            c.CloseAll();
            var d = new FileRegistry();
            Declare(d, host, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("61", d.Status("D"));
            Assert.Null(a.Rollback());
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("00", d.Status("D"));
            d.CloseAll();
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ THE HANDLE A CLOSE KEPT IS NOT "ANOTHER FILE CONNECTOR" TO ITS OWN CONNECTOR'S OPEN OUTPUT (kb/Work PB3483;
    /// §9.1.13.9 1) e), <i>"An attempt is made to open a physical file in the output mode and the physical file is currently
    /// open by another file connector"</i>): a connector under commit control writes, is CLOSEd and opens OUTPUT again — the
    /// file is created anew ('00', §14.9.27.4 GR18) with nothing of the first OPEN left in it. On a host whose share modes
    /// are the lock (Windows) the OUTPUT question is a probe for a handle on the file, and the kept handle answered it:
    /// '61' for a file nobody else had open.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_output_is_not_refused_by_the_handle_its_own_close_kept(FileOrg org)
    {
        string host = Tmp($"pb3483-output-reopen-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.NoOther, mode: FileOpenMode.Output);
            Assert.Equal("00", Write(a, org, 1, "A001"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 2, "A002"));
            a.CloseAll();
            var expected = new string?[10];
            expected[2] = Rec(2, "A002");
            Assert.Equal(expected, Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The same OPEN OUTPUT is refused '61' when another run unit holds the file (§9.1.13.9 1) e)), and the refusal
    /// leaves the kept state as the CLOSE left it (§14.9.27.4 GR25): the transaction is alive for a third run unit
    /// ('51'), and the ROLLBACK restores the file.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_output_refused_by_another_run_unit_keeps_the_kept_state(FileOrg org)
    {
        string host = Tmp($"pb3483-output-refused-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.AllOther);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            a.Close("F");
            var b = Open(host, org, commit: false, name: "B", sharing: FileSharing.AllOther);
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("61", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            var c = Open(host, org, commit: false, name: "C", sharing: FileSharing.AllOther);
            Assert.Equal("51", Rewrite(c, org, 7, "C007", name: "C"));
            c.CloseAll();
            b.CloseAll();
            Assert.Null(a.Rollback());
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A TRANSACTION A ROLLBACK IS OWED IS NEVER COMMITTED (review finding R1-4; §9.1.18.5, <i>"When a run unit
    /// terminates abnormally, all file changes made since the previous commit or, if none, the beginning of the run unit are
    /// rolled back"</i>; 2.2.1.16's refused row "ROLLBACK, at any page", the termination path's rollback the same): an
    /// abnormal ending whose file-only ROLLBACK the medium refuses ('30') leaves the transaction standing, and the implicit
    /// CLOSE's COMMIT that follows refuses it and rolls back again — restoring the file when the medium now allows
    /// (<c>once</c>), else leaving it to the dead-owner step of the next writable OPEN (<c>always</c>). A program's COMMIT after
    /// its refused ROLLBACK is refused the same ('30', the file's name returned), and the next ROLLBACK restores.</summary>
    [Theory]
    [InlineData("abnormal-once")]
    [InlineData("abnormal-always")]
    [InlineData("commit-after-refused-rollback")]
    public void A_transaction_whose_rollback_was_refused_is_never_committed(string path)
    {
        string host = Tmp($"pb3337-owed-{path}");
        try
        {
            Seed(host, FileOrg.Indexed);
            int refusals = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: page => refusals > 0 && page >= MetaPage.MetaPages && (path == "abnormal-always" || refusals-- > 0)
                    ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, FileOrg.Indexed, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(a, FileOrg.Indexed, 7, "A007"));
            Assert.Equal("00", Rewrite(a, FileOrg.Indexed, 1, "X001"));
            refusals = 1;
            if (path == "commit-after-refused-rollback")
            {
                Assert.Equal("F", a.Rollback());
                Assert.Equal("30", a.Status("F"));
                Assert.Equal("F", a.Commit());
                Assert.NotEqual(0ul, a.TransactionOf("F"));
                Assert.Null(a.Rollback());
            }
            else
            {
                a.EndTransactions(abnormal: true);
                Assert.Equal("30", a.Status("F"));
                Assert.NotEqual(0ul, a.TransactionOf("F"));
            }
            refusals = path == "abnormal-always" ? 1 : 0;
            a.CloseAll();
            refusals = 0;
            Assert.Equal(Seeded(), Settled(host, FileOrg.Indexed));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The run unit's own boundary (review lead R1-5): <see cref="RunUnit.Run"/>'s exception filter records how
    /// the run unit ended (<c>RunUnit.NoteEnding</c>) and its <see cref="RunUnit.Terminate"/> chooses by it — a body that
    /// returns, or ends by STOP RUN, ends NORMALLY and commits (§9.1.18.5's first sentence); one that a runtime failure
    /// ends, ABNORMALLY, and rolls back (§9.1.18.5's second; §14.6.12's file-only ROLLBACK).</summary>
    [Theory]
    [InlineData("returns")]
    [InlineData("stop-run")]
    [InlineData("fails")]
    public void A_run_unit_boundary_commits_a_normal_ending_and_rolls_back_an_abnormal_one(string ending)
    {
        string host = Tmp($"pb3337-boundary-{ending}");
        try
        {
            Seed(host, FileOrg.Relative);
            void Body(RunUnit ru)
            {
                Declare(ru.Files, host, FileOrg.Relative, commit: true);
                ru.Files.OpenStatic("F", FileOpenMode.IO);
                Assert.Equal("00", ru.Files.Status("F"));
                Assert.Equal("00", Write(ru.Files, FileOrg.Relative, 7, "A007"));
                if (ending == "stop-run") throw new StopRun();
                if (ending == "fails") throw new InvalidOperationException("a runtime failure no condition names");
            }
            switch (ending)
            {
                case "returns": RunUnit.Run(Body); break;
                case "stop-run": Assert.Throws<StopRun>(() => RunUnit.Run(Body)); break;
                default: Assert.Throws<InvalidOperationException>(() => RunUnit.Run(Body)); break;
            }
            var expected = Seeded();
            if (ending != "fails") expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host, FileOrg.Relative));
        }
        finally { TryDelete(host); }
    }

    /// <summary>A program that runs <paramref name="body"/> as the run unit's main program and as a CALLed one: the stand-in
    /// for a compiled program, so the boundaries of <see cref="ProgramTable"/> are reached without the compiler's half of the
    /// facility (kb/Work PB3338).</summary>
    private sealed class BodyProgram(Action body) : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) => body();
        public void Activate() => body();
        public void CloseFiles() { }
    }

    /// <summary>⛔ THE PROGRAM TABLE'S BOUNDARIES RECORD HOW THE RUN UNIT ENDED (kb/Work PB3481, review lead R1-5 of
    /// PB3337; §9.1.18.5, <i>"When a run unit terminates abnormally, all file changes made since the previous commit or, if
    /// none, the beginning of the run unit are rolled back"</i>; §14.6.12, <i>"If commit and rollback is in effect, a
    /// rollback is executed"</i>). A main program's boundary (<c>ProgramTable.RunMain</c>, whose <c>RunOnThisThread</c> the
    /// standalone <c>Main</c> enters through <c>RunModule</c> too) and a .NET host's (<c>CallFromHost</c>, reached by a CALL
    /// with no calling program) each end the run unit through <see cref="RunUnit.Terminate"/>, which chooses the implicit
    /// CLOSE's COMMIT for a normal ending and the file-only ROLLBACK for an abnormal one by what <c>RunUnit.NoteEnding</c>
    /// recorded at that boundary. A program writes a record under commit control and ends: by returning (the main boundary
    /// only: a host's activation that returns does not end the run unit), by STOP RUN (<c>StopRun</c> unwinds to the
    /// boundary: a normal ending, committed), by a fatal exception condition (<c>CobolFatalException</c>), an
    /// implementor-defined stop (<c>CobolImplementorFatalException</c>) or a CALL fatal (<c>CobolCallException</c>, which only
    /// the main boundary ends the run unit for: a host's activation fails with it and continues), or by a runtime failure no
    /// condition names (an <see cref="InvalidOperationException"/>, which the main boundary rethrows) — every ending but the
    /// first two abnormal: rolled back, and the file reads as it stood at the start.</summary>
    [Theory]
    [InlineData("main", "returns", true)]
    [InlineData("main", "stop-run", true)]
    [InlineData("main", "fatal", false)]
    [InlineData("main", "implementor-fatal", false)]
    [InlineData("main", "call-fatal", false)]
    [InlineData("main", "fails", false)]
    [InlineData("host", "stop-run", true)]
    [InlineData("host", "fatal", false)]
    [InlineData("host", "implementor-fatal", false)]
    public void A_program_boundary_commits_a_normal_ending_and_rolls_back_an_abnormal_one(string boundary, string ending,
        bool committed)
    {
        string host = Tmp($"pb3481-{boundary}-{ending}");
        try
        {
            Seed(host, FileOrg.Relative);
            var ru = new RunUnit();
            ru.Programs.RegisterModule("Cobol.PB3481.__CobolModule", RuntimeAbi.Version.ToString(), RuntimeAbi.CallAbi, () =>
                ru.Programs.Register("PB3481", "PB3481", null, initial: false, common: false, recursive: false, _ =>
                    new BodyProgram(() =>
                    {
                        Declare(ru.Files, host, FileOrg.Relative, commit: true);
                        ru.Files.OpenStatic("F", FileOpenMode.IO);
                        Assert.Equal("00", ru.Files.Status("F"));
                        Assert.Equal("00", Write(ru.Files, FileOrg.Relative, 7, "A007"));
                        switch (ending)
                        {
                            case "stop-run": throw new StopRun();
                            case "fatal": throw new CobolFatalException("EC-DATA-PTR-NULL", "a NULL BASED item");
                            case "implementor-fatal": throw new CobolImplementorFatalException("an implementor-defined stop");
                            case "call-fatal": throw new CobolCallException("the program cannot be located");
                            case "fails": throw new InvalidOperationException("a runtime failure no condition names");
                        }
                    })));
            if (boundary == "main")
            {
                if (ending is "stop-run") Assert.Throws<StopRun>(() => ru.Programs.RunMain("PB3481"));
                else if (ending is "fails") Assert.Throws<InvalidOperationException>(() => ru.Programs.RunMain("PB3481"));
                else ru.Programs.RunMain("PB3481");
                if (ending is "returns") Assert.Equal(0, ru.ExitStatus);
                else if (ending is "fatal" or "implementor-fatal" or "call-fatal") Assert.Equal(1, ru.ExitStatus);   // §14.6.12's ERROR indication
            }
            else
            {
                var ended = Assert.Throws<RunUnitEndedException>(() => ru.Programs.CallProgram("PB3481", null, [], null));
                Assert.Equal(!committed, ended.Abnormal);
            }
            var expected = Seeded();
            if (committed) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host, FileOrg.Relative));
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
        string host = Tmp($"pb3337-end-{abnormal}");
        try
        {
            Seed(host, FileOrg.Indexed);
            var a = Open(host, FileOrg.Indexed, commit: true);
            Assert.Equal("00", Write(a, FileOrg.Indexed, 7, "A007"));
            if (abnormal) a.EndTransactions(abnormal: true);
            a.CloseAll();
            var expected = Seeded();
            if (!abnormal) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Contents(host, FileOrg.Indexed));
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.10's ceilings: the commit posture's READ locks have no ceiling but the host's (DOC-A.1-154/155) — a
    /// transaction that READs and REWRITEs 300 records answers no '53' or '54'.</summary>
    [Fact]
    public void A_transaction_of_300_records_meets_no_lock_ceiling()
    {
        string host = Tmp("pb3337-ceiling");
        try
        {
            Seed(host, FileOrg.Relative, records: 300);
            var a = Open(host, FileOrg.Relative, commit: true);
            for (int n = 1; n <= 300; n++)
            {
                Assert.Equal("00", Read(a, FileOrg.Relative, n, out _));
                Assert.Equal("00", Rewrite(a, FileOrg.Relative, n, "B000"));
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
        string host = Tmp("pb3337-first");
        try
        {
            Seed(host, FileOrg.Relative);
            bool refuse = false;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: page => refuse && page >= MetaPage.MetaPages ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, FileOrg.Relative, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            refuse = true;
            Assert.Equal("30", Write(a, FileOrg.Relative, 7, "A007"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            var b = Open(host, FileOrg.Relative, commit: true);
            Assert.Equal("00", Write(b, FileOrg.Relative, 8, "A008"));
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
        string host = Tmp($"pb3337-refused-{refuseSync}");
        try
        {
            Seed(host, FileOrg.Indexed);
            bool armed = false;
            int syncs = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: page => armed && !refuseSync && page < MetaPage.WorkSlot ? new IOException("slot refused") : null,
                sync: () => armed && refuseSync && ++syncs == 2 ? new IOException("sync 2 refused") : null);
            FileRegistry a;
            try { a = Open(host, FileOrg.Indexed, commit: true); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(a, FileOrg.Indexed, 7, "A007"));
            Assert.Equal("00", Rewrite(a, FileOrg.Indexed, 1, "X001"));
            armed = true;
            Assert.Equal("F", a.Commit());
            Assert.Equal("30", a.Status("F"));
            armed = false;
            Assert.Null(a.Rollback());   // item 8's unconditional ROLLBACK
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
            Assert.Equal(Seeded(), Contents(host, FileOrg.Indexed));
        }
        finally { TryDelete(host); }
    }
}
