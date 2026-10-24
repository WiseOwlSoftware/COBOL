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
        FileSharing sharing = FileSharing.AllOther, bool optional = false)
    {
        if (org == FileOrg.Relative)
            reg.RegisterRelative(name, host, recordWidth: 8, optional: optional, accessMode: (int)KeyedAccess.Dynamic,
                relativeKeyDigits: 4, varyMin: -1, varyMax: -1);
        else
        {
            reg.RegisterIndexed(name, host, recordWidth: 8, optional: optional, accessMode: (int)KeyedAccess.Dynamic,
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
            // §14.9.6.4 GR9: the CLOSE released no record lock of a file in an APPLY COMMIT clause, so the record a's READ
            // locked is still a's (kb/Work PB3480; this case used to pin '00' here, the CLOSE's wrong release); the next
            // record, which nobody locked, reads.
            Assert.Equal("51", Read(b, org, 1, out _, name: "B"));
            Assert.Equal("00", Read(b, org, 2, out var image, name: "B"));
            Assert.Equal(Rec(2, "A002"), image);
            b.CloseAll();
            Assert.Null(a.Commit());
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ AN INPUT OPENER THAT STILL OWES ITS TRANSACTION'S WRITES IS STILL AN INPUT OPENER (kb/Work PB3482; §9.1.15 2),
    /// <i>"The sharing with read only mode restricts concurrent access to a physical file through file connectors other than
    /// this one, to input mode"</i>; Table 19, the cells of the SHARING WITH READ ONLY INPUT and EXTEND / I-O rows against the
    /// existing SHARING WITH ALL OTHER INPUT column are <i>"Normal open"</i>): a connector under commit control changes a record,
    /// is CLOSEd and re-OPENed INPUT while its transaction stands, and the handle it holds is writable all the same — the one
    /// the COMMIT or ROLLBACK writes through (2.2.1.10 As built (3)). Another run unit's OPEN under SHARING WITH READ ONLY, INPUT
    /// or I-O, is a normal open beside it and answers '00', on a host whose share modes are mandatory as on one that is not;
    /// the record the transaction changed answers '51' to it (DOC-A.1-152), the others are read, and the COMMIT or ROLLBACK
    /// the INPUT opener then executes succeeds with that run unit's connector still open. The opener that follows it is
    /// still held to the rules of the READ ONLY connector it found — an OPEN I-O under SHARING WITH ALL OTHER is refused '61'
    /// (Table 19, the ALL OTHER EXTEND / I-O row against the READ ONLY INPUT column) — and an OPEN OUTPUT is too.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, FileOpenMode.Input, true)]
    [InlineData(FileOrg.Relative, FileOpenMode.Input, false)]
    [InlineData(FileOrg.Relative, FileOpenMode.IO, true)]
    [InlineData(FileOrg.Relative, FileOpenMode.IO, false)]
    [InlineData(FileOrg.Indexed, FileOpenMode.Input, true)]
    [InlineData(FileOrg.Indexed, FileOpenMode.Input, false)]
    [InlineData(FileOrg.Indexed, FileOpenMode.IO, true)]
    [InlineData(FileOrg.Indexed, FileOpenMode.IO, false)]
    public void An_input_opener_that_owes_its_transactions_writes_admits_what_Table_19_admits(FileOrg org, FileOpenMode other, bool commit)
    {
        string host = Tmp($"pb3482-owed-{org}-{other}-{commit}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("00", a.Status("F"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));

            var b = new FileRegistry();
            Declare(b, host, org, commit: false, name: "B", sharing: FileSharing.ReadOnly);
            b.OpenStatic("B", other);
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", Read(b, org, 1, out var image, name: "B"));
            Assert.Equal(Rec(1, "A001"), image);
            Assert.Equal("51", Read(b, org, 7, out _, name: "B"));

            var c = new FileRegistry();
            Declare(c, host, org, commit: false, name: "C", sharing: FileSharing.AllOther);
            c.OpenStatic("C", FileOpenMode.IO);
            Assert.Equal("61", c.Status("C"));
            c.OpenStatic("C", FileOpenMode.Output);
            Assert.Equal("61", c.Status("C"));

            Assert.Null(commit ? a.Commit() : a.Rollback());
            Assert.Equal(0ul, a.TransactionOf("F"));
            b.CloseAll();
            a.CloseAll();
            var expected = Seeded();
            if (commit) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ THE OTHER ARM OF THE OWED-WRITES FACT: AN INPUT OPENER WHOSE COLUMN IS TAKEN AFRESH STILL PUBLISHES IT (kb/Work
    /// PB3499; §9.1.15 2); Table 19, the SHARING WITH READ ONLY INPUT row against the existing READ ONLY INPUT column is
    /// <i>"Normal open"</i>). A connector under commit control and SHARING WITH NO OTHER changes a record and is CLOSEd: its kept
    /// handle excludes every other open and publishes no column, so its re-OPEN INPUT under SHARING WITH READ ONLY takes its
    /// column afresh, after the body, through the writable handle that replaced the kept one (the transaction stands). That
    /// opener owes its writes exactly as one that amended a kept column does, so another run unit's OPEN INPUT under SHARING
    /// WITH READ ONLY is a normal open beside it: '00' on a host whose share modes are mandatory as on one that is not.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_input_opener_whose_column_is_taken_afresh_after_a_NO_OTHER_close_publishes_that_it_owes_writes(FileOrg org)
    {
        string host = Tmp($"pb3499-noother-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.NoOther);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.RegisterSharing("F", FileSharing.ReadOnly, FileLockMode.Automatic, multiple: true);
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("00", a.Status("F"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));

            var b = new FileRegistry();
            Declare(b, host, org, commit: false, name: "B", sharing: FileSharing.ReadOnly);
            b.OpenStatic("B", FileOpenMode.Input);
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", Read(b, org, 1, out var image, name: "B"));
            Assert.Equal(Rec(1, "A001"), image);
            b.CloseAll();

            Assert.Null(a.Rollback());
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

    /// <summary>⛔ A RE-OPEN THAT IS REFUSED ON ITS SECOND ATTEMPT TOO LEAVES WHAT THE CLOSE KEPT AS IT WAS, AND MAKES NO THIRD
    /// (kb/Work PB3498; §9.1.13.9 1), <i>"A file sharing conflict condition exists because an OPEN statement is attempted on a
    /// physical file and that physical file is already open by another file connector in a manner that conflicts with this
    /// request"</i>; §14.9.27.4 GR25, <i>"If the execution of the OPEN statement is unsuccessful, the file is not
    /// affected"</i>; §14.9.7.4 GR4, <i>"For files specified in active APPLY COMMIT clauses that have been closed and not
    /// reopened prior to the COMMIT statement, the file locks are released."</i>): B, under commit control and SHARING WITH READ
    /// ONLY, reads and is CLOSEd (its handle and its Table 19 column kept) beside another run unit's INPUT opener that owes
    /// writes (kb/Work PB3499; the second attempt is made only beside one); a program
    /// outside the protocol reads the file with a share mode that refuses writers, so B's OPEN I-O is refused by the host on a
    /// host whose share modes are mandatory, its column held, and made again with the share mode that admits what an INPUT
    /// opener may owe (<see cref="FileLockPosture.WhenOwedWritesAreAdmitted"/>), which the outside reader refuses too: '61'.
    /// Each attempt replaces the kept column by its own and an unsuccessful one puts the kept column back, so the host is asked
    /// four times (replace, restore, replace, restore) and not a fifth. When a third run unit publishes a column that Table 19
    /// refuses against B's OPEN I-O between the two attempts instead (<paramref name="thirdRunUnitRacesTheSecond"/>), the
    /// second attempt is refused at the column, before any handle is asked: three requests, and the kept column stands as the
    /// CLOSE left it. Either way the kept column still refuses what it refused before (an OPEN I-O under SHARING WITH ALL OTHER,
    /// Table 19's ALL OTHER I-O row against the READ ONLY INPUT column), the kept handle still refuses a writer on a host with
    /// mandatory share modes, B's re-OPEN INPUT adopts it, and the ROLLBACK gives both back. A host whose share modes are
    /// advisory admits the OPEN I-O at its first attempt.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, false)]
    [InlineData(FileOrg.Relative, true)]
    [InlineData(FileOrg.Indexed, false)]
    [InlineData(FileOrg.Indexed, true)]
    public void A_reopen_refused_on_its_second_attempt_too_keeps_what_the_close_kept_and_makes_no_third_attempt(FileOrg org,
        bool thirdRunUnitRacesTheSecond)
    {
        string host = Tmp($"pb3498-twice-{org}-{thirdRunUnitRacesTheSecond}");
        RunUnitFileLock? thirdRunUnit = null, owingOpener = null;
        try
        {
            Seed(host, org);
            // Another run unit's INPUT opener that OWES WRITES (kb/Work PB3499: the second attempt is made only beside one): the
            // file lock a CLOSE under commit control kept (SHARING WITH ALL OTHER on I-O), replaced by its INPUT re-OPEN's column.
            Assert.Equal(RunUnitFileLock.Outcome.Held,
                RunUnitFileLock.Take(RunUnitFileLock.HostOfThisProcess, host, FileSharing.AllOther, FileOpenMode.IO, out owingOpener));
            Assert.Equal(RunUnitFileLock.Outcome.Held, owingOpener!.Amend(FileSharing.AllOther, FileOpenMode.Input, owesWrites: true));
            var b = new FileRegistry();
            Declare(b, host, org, commit: true, name: "B", sharing: FileSharing.ReadOnly);
            var log = new ColumnRequestLog();
            var (kept, replacing) = (Table19.Column(FileSharing.ReadOnly, FileOpenMode.Input), Table19.Column(FileSharing.ReadOnly, FileOpenMode.IO));
            ColumnRequestLog.Request restore = new(kept, Replaces: true), replace = new(replacing, Replaces: true);
            // The third run unit: SHARING WITH READ ONLY on INPUT publishes the column B's OPEN I-O refuses and neither B's
            // kept column nor the INPUT opener's refuses it.
            (FileSharing, FileOpenMode) racer = (FileSharing.ReadOnly, FileOpenMode.Input);
            Assert.True(Table19.Conflicts((FileSharing.ReadOnly, FileOpenMode.IO), racer));
            Assert.False(Table19.Conflicts(racer, (FileSharing.ReadOnly, FileOpenMode.Input)));
            Assert.False(Table19.Conflicts(racer, (FileSharing.AllOther, FileOpenMode.Input)));
            bool mandatory = HostFile.ShareModesAreMandatory;
            using (log.Install())
            {
                b.OpenStatic("B", FileOpenMode.Input);
                Assert.Equal("00", b.Status("B"));
                Assert.Equal("00", Read(b, org, 1, out _, name: "B"));
                b.Close("B");
                Assert.Equal("00", b.Status("B"));
                Assert.Equal(0ul, b.TransactionOf("B"));
            }
            log.Clear();
            log.Before = index =>
            {
                if (thirdRunUnitRacesTheSecond && index == 2)
                    Assert.Equal(RunUnitFileLock.Outcome.Held,
                        RunUnitFileLock.Take(RunUnitFileLock.HostOfThisProcess, host, racer.Item1, racer.Item2, out thirdRunUnit));
            };
            using (var outsideReader = new FileStream(host, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                b.OpenStatic("B", FileOpenMode.IO);
                if (mandatory)
                {
                    Assert.Equal("61", b.Status("B"));
                    Assert.Equal(thirdRunUnitRacesTheSecond ? [replace, restore, replace] : [replace, restore, replace, restore], log.Requests);
                    Assert.Equal(thirdRunUnitRacesTheSecond, thirdRunUnit is not null);
                }
                else
                {
                    Assert.Equal("00", b.Status("B"));
                    Assert.Equal([replace], log.Requests);
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
            }
            thirdRunUnit?.Dispose();
            // What the CLOSE kept stands: its column (an OPEN I-O under ALL OTHER is refused by it alone, the INPUT opener's
            // column admitting it) and, where share modes are mandatory, its handle (a writer is refused by its share mode).
            Assert.Equal(RunUnitFileLock.Outcome.Refused, Ask(host, FileSharing.AllOther, FileOpenMode.IO));
            if (mandatory)
            {
                var writer = Assert.ThrowsAny<IOException>(() => new FileStream(host, FileMode.Open, FileAccess.Write, FileShare.ReadWrite).Dispose());
                Assert.True(HostFile.IsSharingRefusal(writer));
                b.OpenStatic("B", FileOpenMode.Input);
                Assert.Equal("00", b.Status("B"));
                b.Close("B");
                Assert.Equal("00", b.Status("B"));
            }
            Assert.Null(b.Rollback());
            Assert.Equal(RunUnitFileLock.Outcome.Held, Ask(host, FileSharing.AllOther, FileOpenMode.IO));
            using (new FileStream(host, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { }
            owingOpener.Dispose();
            b.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally
        {
            thirdRunUnit?.Dispose();
            owingOpener?.Dispose();
            TryDelete(host);
        }
    }

    /// <summary>What an OPEN in (<paramref name="sharing"/>, <paramref name="mode"/>) would be told by the Table 19 column
    /// lock of this host, the request itself left nowhere.</summary>
    private static RunUnitFileLock.Outcome Ask(string host, FileSharing sharing, FileOpenMode mode)
    {
        var outcome = RunUnitFileLock.Take(RunUnitFileLock.HostOfThisProcess, host, sharing, mode, out var held);
        held?.Dispose();
        return outcome;
    }

    /// <summary>⛔ THE TWO-ATTEMPT OPEN OVER A KEPT HANDLE AND ITS RECORD LOCKS (kb/Work PB3501; §9.1.15 2), <i>"The sharing with read
    /// only mode restricts concurrent access to a physical file through file connectors other than this one, to input mode"</i>;
    /// §14.9.6.4 GR9, <i>"Except when the file is specified in an APPLY COMMIT clause, the file lock and any record locks
    /// associated with the file connector referenced by file-name-1 are released by the execution of the CLOSE statement"</i>;
    /// §14.9.27.4 GR25, <i>"If the execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §9.1.16, <i>"While
    /// locked by a given file connector, a record is not accessible to another file connector in the same or a different run
    /// unit"</i>): run unit A, under commit control, changes a record, is CLOSEd and re-OPENed INPUT, so it holds the writable
    /// handle its transaction owes writes through (kb/Work PB3482). Run unit B, under commit control and SHARING WITH READ ONLY,
    /// OPENs I-O beside it (a normal open, Table 19; the host's share mode refuses it, so the OPEN is made again with the share
    /// mode that admits an INPUT opener's owed writes, <c>FileLockPosture.WhenOwedWritesAreAdmitted</c>), READs one record (its
    /// record lock), REWRITEs another (its transaction) and is CLOSEd, keeping a writable handle, its Table 19 column, the record
    /// lock and the transaction's liveness byte. B's re-OPEN INPUT is refused by the host on its first attempt after the kept
    /// handle was given back and the new one tried; the unsuccessful attempt takes the kept handle again with its record locks
    /// (<c>KeyedConnector.RetakeKeptHandle</c>, kb/Work PB3483), and the second attempt succeeds and rehomes them again
    /// (<c>KeyedConnector.Attach</c>). Whichever half regressed - the retake not republishing the locks, or the give-back left set
    /// between the attempts - a third run unit would READ the record B locked with '00' where §9.1.16 owes it '51', and the record B changed
    /// with '00' where its transaction's holding owes '51' (DOC-A.1-152). The COMMIT then makes both readable ('00') and the
    /// record B changed holds B's value; the column requests of the re-OPEN are the two attempts' (replace, restore, replace) on
    /// a host whose share modes are mandatory and one (replace) elsewhere, where the first attempt is admitted.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_two_attempt_reopen_over_a_kept_handle_keeps_the_record_locks_it_was_closed_with(FileOrg org)
    {
        string host = Tmp($"pb3501-twice-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.OpenStatic("F", FileOpenMode.Input);
            Assert.Equal("00", a.Status("F"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));

            var b = new FileRegistry();
            Declare(b, host, org, commit: true, name: "B", sharing: FileSharing.ReadOnly);
            b.RegisterSharing("B", FileSharing.ReadOnly, FileLockMode.Automatic, multiple: true);
            var log = new ColumnRequestLog();
            var (kept, own) = (Table19.Column(FileSharing.ReadOnly, FileOpenMode.IO), Table19.Column(FileSharing.ReadOnly, FileOpenMode.Input));
            using (log.Install())
            {
                b.OpenStatic("B", FileOpenMode.IO);
                Assert.Equal("00", b.Status("B"));
                Assert.Equal("00", Read(b, org, 2, out _, name: "B"));
                Assert.Equal("00", Rewrite(b, org, 3, "B003", name: "B"));
                ulong id = b.TransactionOf("B");
                Assert.NotEqual(0ul, id);
                b.Close("B");
                Assert.Equal("00", b.Status("B"));
                log.Clear();
                b.OpenStatic("B", FileOpenMode.Input);
                Assert.Equal("00", b.Status("B"));
                Assert.Equal(id, b.TransactionOf("B"));
            }
            Assert.Equal(HostFile.ShareModesAreMandatory ? [(own, true), (kept, true), (own, true)] : [(own, true)],
                log.Requests.Select(r => (r.Own, r.Replaces)));

            // C takes part in record locking (a LOCK MODE clause, which SHARING WITH ALL OTHER requires of a file not under APPLY COMMIT, §14.9.27.3 SR8), so it meets B's.
            var c = Open(host, org, commit: false, FileOpenMode.Input, name: "C");
            Assert.Equal("51", Read(c, org, 2, out _, name: "C"));
            Assert.Equal("51", Read(c, org, 3, out _, name: "C"));
            Assert.Equal("00", Read(c, org, 1, out var untouched, name: "C"));
            Assert.Equal(Rec(1, "A001"), untouched);

            Assert.Null(b.Commit());
            Assert.Equal("00", Read(c, org, 2, out _, name: "C"));
            Assert.Equal("00", Read(c, org, 3, out var changed, name: "C"));
            Assert.Equal(Rec(3, "B003"), changed);
            c.CloseAll();
            b.CloseAll();
            Assert.Null(a.Rollback());
            a.CloseAll();
            var expected = Seeded();
            expected[3] = Rec(3, "B003");
            Assert.Equal(expected, Settled(host, org));
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

    /// <summary>The same, when the re-OPEN changes the sharing mode and the CLOSE kept a Table 19 column lock with its handle
    /// (on Windows a read handle of the connector's own, kb/Work PB3482): the kept lock is replaced by the re-OPEN's (kb/Work
    /// PB3483), and neither it nor the kept handle is "another file connector" (§9.1.13.9 1) e)) or a holder Table 19 refuses
    /// the re-OPEN beside. An OPEN OUTPUT under SHARING WITH ALL OTHER is asked the OUTPUT question beside the column holder;
    /// a re-OPEN under SHARING WITH NO OTHER takes an exclusive handle, which the column holder gives way to; and a re-OPEN
    /// after a CLOSE under SHARING WITH NO OTHER, whose kept handle excludes every other open (the column holder's included),
    /// takes its column through the handle that replaces it — also after a NO OTHER re-OPEN of a lock kept under ALL OTHER.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_is_not_refused_by_the_column_lock_its_own_close_kept(FileOrg org)
    {
        var scenarios = new (FileSharing First, (FileSharing Sharing, FileOpenMode Mode)[] Reopens)[]
        {
            (FileSharing.AllOther, [(FileSharing.AllOther, FileOpenMode.Output)]),
            (FileSharing.AllOther, [(FileSharing.NoOther, FileOpenMode.IO)]),
            (FileSharing.NoOther, [(FileSharing.AllOther, FileOpenMode.IO)]),
            (FileSharing.AllOther, [(FileSharing.NoOther, FileOpenMode.IO), (FileSharing.AllOther, FileOpenMode.IO)]),
        };
        foreach (var (first, reopens) in scenarios)
        {
            string host = Tmp($"pb3482-kept-column-reopen-{org}-{first}-{reopens.Length}-{reopens[0].Sharing}");
            try
            {
                Seed(host, org);
                var a = Open(host, org, commit: true, sharing: first);
                Assert.Equal("00", Write(a, org, 7, "A007"));
                foreach (var (sharing, mode) in reopens)
                {
                    a.Close("F");
                    Assert.Equal("00", a.Status("F"));
                    a.OpenShared("F", mode, hasSharingOverride: true, sharing, FileRetryKind.None, 0,
                        OpenTapePhrase.None, a.HostPathOf("F"), assignDynamic: false, page: null);
                    Assert.Equal("00", a.Status("F"));
                }
                // The re-OPEN's own lock stands: a third run unit's SHARING WITH READ ONLY INPUT, which Table 19 refuses
                // beside an ALL OTHER connector open in a mode other than input and beside a NO OTHER one, is refused, and
                // admitted once the first run unit ends.
                var c = new FileRegistry();
                Declare(c, host, org, commit: false, name: "C", sharing: FileSharing.ReadOnly);
                c.OpenStatic("C", FileOpenMode.Input);
                Assert.Equal("61", c.Status("C"));
                a.CloseAll();
                c.OpenStatic("C", FileOpenMode.Input);
                Assert.Equal("00", c.Status("C"));
                c.CloseAll();
            }
            finally { TryDelete(host); }
        }
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

    /// <summary>The other caller of the retake (kb/Work PB3497; <c>KeyedConnector.HeldByAnotherHandle</c>, the OPEN OUTPUT's probe for a
    /// handle on the file): the kept handle is given back for the instant of the question, another run unit's handle answers
    /// it ('61', §9.1.13.9 1) e)), and the medium refuses the retake's read of the store the give-back left without a file
    /// ('30', §9.1.13.6 item 1). The handle is kept all the same (§14.9.27.4 GR25), so a third run unit meets the record lock the
    /// CLOSE kept ('51', §14.9.6.4 GR9; §9.1.16) and the transaction's holding ('51'), and the connector's re-OPEN, which reads
    /// the store again, is the transaction's and ends at the ROLLBACK.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_output_refused_by_another_run_unit_with_the_medium_refusing_the_retake_keeps_the_kept_state(FileOrg org)
    {
        string host = Tmp($"pb3497-output-refused-{org}");
        try
        {
            Seed(host, org);
            bool armed = false;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, read: _ => armed ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, org, commit: true, sharing: FileSharing.AllOther); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Read(a, org, 1, out _));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.PagedStoreOf("F")!.DropCache();
            var b = Open(host, org, commit: false, name: "B", sharing: FileSharing.AllOther);
            armed = true;
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("61", a.Status("F"));
            armed = false;
            Assert.Equal(id, a.TransactionOf("F"));
            var c = Open(host, org, commit: false, name: "C", sharing: FileSharing.AllOther);
            Assert.Equal("51", Read(c, org, 1, out _, name: "C"));
            Assert.Equal("51", Rewrite(c, org, 7, "C007", name: "C"));
            c.CloseAll();
            b.CloseAll();
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            Assert.Null(a.Rollback());
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ AN OPEN OUTPUT OVER THE FILE ITS CONNECTOR'S CLOSE KEPT LEAVES THE KEPT STATE UNTIL THE CREATION HAS LANDED
    /// (kb/Work PB3493; §14.9.27.4 GR18, <i>"the successful execution of the OPEN statement creates the file"</i>; GR25, <i>"If
    /// the execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.7.4 GR4): A, under commit control,
    /// changes record 7 and is CLOSEd; its re-OPEN OUTPUT creates the file in a store of its own, and the medium refuses the
    /// creation at its second page write (<c>refusal</c> "write") or at its first sync ("sync"). The creation is undone, so the
    /// OPEN answers '30' and the file is not affected: the transaction stands (its liveness byte is held by a live run unit, so
    /// another run unit's REWRITE of record 7 answers '51' where the transaction as a death leaves it would be rolled back by
    /// that run unit's writable OPEN), the file lock stands (a NO OTHER OPEN is refused '61'), the other records read as they
    /// were, and the ROLLBACK restores the file. Before the fix the kept state was given back before the OUTPUT took its own
    /// handle, and the refusal left it gone.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "write")]
    [InlineData(FileOrg.Relative, "sync")]
    [InlineData(FileOrg.Indexed, "write")]
    [InlineData(FileOrg.Indexed, "sync")]
    public void A_reopen_output_whose_creation_is_refused_keeps_the_kept_state(FileOrg org, string refusal)
    {
        string host = Tmp($"pb3493-creation-refused-{org}-{refusal}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.AllOther);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            int writes = 0, syncs = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: _ => refusal == "write" && ++writes == 2 ? new IOException("refused by the host") : null,
                sync: () => refusal == "sync" && ++syncs == 1 ? new IOException("refused by the host") : null);
            try { a.OpenStatic("F", FileOpenMode.Output); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("30", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            var c = Open(host, org, commit: false, name: "C", sharing: FileSharing.AllOther);
            Assert.Equal("51", Rewrite(c, org, 7, "C007", name: "C"));
            Assert.Equal("00", Read(c, org, 1, out var untouched, name: "C"));
            Assert.Equal(Rec(1, "A001"), untouched);
            c.CloseAll();
            var d = new FileRegistry();
            Declare(d, host, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("61", d.Status("D"));
            Assert.Null(a.Rollback());
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The one refusal that lands the creation is the final sync's, after the truncation (<see cref="PagedFile.Create"/>:
    /// the medium's state is unknown, '30' either way): the previous file is gone all the same, so what the CLOSE kept ends with
    /// it. The OPEN answers '30', the transaction is no more, and the file is the created, empty store (§14.9.27.4 GR18).</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_output_whose_final_sync_is_refused_ends_the_kept_state_with_the_file(FileOrg org)
    {
        string host = Tmp($"pb3493-final-sync-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.AllOther);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            int syncs = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, sync: () => ++syncs == 2 ? new IOException("refused by the host") : null);
            try { a.OpenStatic("F", FileOpenMode.Output); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("30", a.Status("F"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            var d = new FileRegistry();
            Declare(d, host, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("00", d.Status("D"));   // the kept file lock went with the file
            d.CloseAll();
            Assert.Null(a.Rollback());
            a.CloseAll();
            Assert.Equal(new string?[10], Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A REFUSED CREATION WHOSE UNDO THE MEDIUM REFUSES TOO LEAVES THE PREVIOUS FILE GONE (review finding R1-1 of
    /// kb/Work PB3493; 2.2.1.16's OPEN OUTPUT rows): A changes record 7 under commit control and is CLOSEd; its re-OPEN OUTPUT
    /// writes the empty meta at N into the commit slot, and the medium then refuses the creation (page 2's write, or the commit
    /// slot's sync) AND the undo's restore of that slot. §14.9.27.4 GR25's <i>"the file is not affected"</i> is out of reach:
    /// the commit slot holds the empty store, which R6 adopts at every later OPEN. So the OPEN answers '30' (§9.1.13.6 item 1)
    /// and what the CLOSE kept ends with the previous file, as at a refused final sync: no transaction stands (a COMMIT and a
    /// ROLLBACK find none and touch nothing), the kept file lock is gone (a NO OTHER OPEN of another run unit is '00'), and the
    /// file reads as the empty store. Before the fix the kept transaction stood over the old store, and the next COMMIT
    /// promoted it beside the creation's slot and answered success over a file every later OPEN read as empty.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "write")]
    [InlineData(FileOrg.Relative, "sync")]
    [InlineData(FileOrg.Indexed, "write")]
    [InlineData(FileOrg.Indexed, "sync")]
    public void A_reopen_output_whose_undo_is_refused_ends_the_kept_state_with_the_file(FileOrg org, string refusal)
    {
        string host = Tmp($"pb3493-undo-refused-{org}-{refusal}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.AllOther);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            // "write": the creation's 2nd write (page 2) and the undo's (the commit slot's restore); "sync": the creation's 1st
            // sync (the commit point) and the undo's write.
            int writes = 0, syncs = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                write: _ => ++writes == 2 || refusal == "write" && writes == 3 ? new IOException("refused by the host") : null,
                sync: () => refusal == "sync" && ++syncs == 1 ? new IOException("refused by the host") : null);
            try { a.OpenStatic("F", FileOpenMode.Output); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("30", a.Status("F"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            var d = new FileRegistry();
            Declare(d, host, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("00", d.Status("D"));   // the kept file lock went with the previous file
            d.CloseAll();
            Assert.Null(a.Commit());
            Assert.Null(a.Rollback());
            a.CloseAll();
            Assert.Equal(new string?[10], Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>An OPEN OUTPUT whose creation lands ends what the CLOSE kept with the file it was kept on (§14.9.27.4 GR18): the
    /// transaction (A's record 7 is not in the new file and no ROLLBACK brings it back), its record lock on record 1 (another
    /// run unit's READ of record 1, which answered '51' before, finds the new file empty) and the file lock; the connector's
    /// next WRITE begins a transaction of the new file, which the COMMIT makes permanent.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_output_that_lands_ends_what_the_close_kept(FileOrg org)
    {
        string host = Tmp($"pb3493-landed-{org}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.AllOther);
            Assert.Equal("00", Read(a, org, 1, out _));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            var b = Open(host, org, commit: false, name: "B", sharing: FileSharing.AllOther);
            Assert.Equal("23", Read(b, org, 1, out _, name: "B"));
            b.CloseAll();
            Assert.Equal("00", Write(a, org, 2, "A002"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));
            Assert.Null(a.Commit());
            a.CloseAll();
            var expected = new string?[10];
            expected[2] = Rec(2, "A002");
            Assert.Equal(expected, Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The same decision where the file a connector's CLOSE kept is not there at the re-OPEN (kb/Work PB3493's sibling arms,
    /// the absent OPTIONAL file of §14.9.27.4 GR13 on INPUT and of GR17 on I-O): what the CLOSE kept belonged to a file that was
    /// removed outside the run unit, so the OPEN ('05') retires it (kb/Work PB3576) instead of attaching beside it, and a later OPEN of the file made
    /// anew by another run unit reads THAT file's records, never the removed file's from the kept store. On I-O the OPEN creates
    /// the file (GR17) and the CLOSE keeps the file lock its opening established (§9.1.15), so another run unit's OPEN OUTPUT is
    /// '61' until the COMMIT removes it. A host that keeps a file
    /// while a handle on it stands (Windows: the kept handle's share mode admits no delete) refuses the removal, which is the
    /// host's own protection of the kept state.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, FileOpenMode.Input)]
    [InlineData(FileOrg.Relative, FileOpenMode.IO)]
    [InlineData(FileOrg.Indexed, FileOpenMode.Input)]
    [InlineData(FileOrg.Indexed, FileOpenMode.IO)]
    public void A_reopen_of_an_optional_file_that_vanished_retires_what_the_close_kept(FileOrg org, FileOpenMode mode)
    {
        string host = Tmp($"pb3493-vanished-{org}-{mode}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true, optional: true);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            try { File.Delete(host); }
            catch (IOException) when (OperatingSystem.IsWindows())
            {
                Assert.Null(a.Rollback());   // the host kept the file with the handle: there is no vanished file to re-OPEN
                a.CloseAll();
                return;
            }
            a.OpenStatic("F", mode);
            Assert.Equal("05", a.Status("F"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            if (mode == FileOpenMode.IO)
            {
                // The I-O OPEN created the file (§14.9.27.4 GR17), and its successful opening established a file lock that the
                // CLOSE under APPLY COMMIT keeps (§9.1.15): ALL OTHER admits input, I-O and extend only, so another run unit's
                // OPEN OUTPUT is refused ('61') until the COMMIT removes the lock.
                var refused = new FileRegistry();
                Declare(refused, host, org, commit: false, name: "S");
                refused.OpenStatic("S", FileOpenMode.Output);
                Assert.Equal("61", refused.Status("S"));
                Assert.Null(a.Commit());
            }
            var maker = Open(host, org, commit: false, FileOpenMode.Output, name: "S");
            Assert.Equal("00", Write(maker, org, 2, "B002", name: "S"));
            maker.CloseAll();
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Read(a, org, 2, out var image));
            Assert.Equal(Rec(2, "B002"), image);
            Assert.Equal("23", Read(a, org, 7, out _));
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>The absent OPTIONAL I-O arm's two refusals over a kept state (§14.9.27.4 GR17 creates the file; GR25, <i>"If the
    /// execution of the OPEN statement is unsuccessful, the file is not affected"</i>; kb/Work PB3493, review finding R1-5; PB3576):
    /// the file A's CLOSE under commit control kept is removed outside the run unit, and A's re-OPEN I-O finds it absent. The kept
    /// state belongs to the removed file, not to A, so it RETIRES to the registry before the OPEN reaches either refusal: when the
    /// host refuses the handle that would create the new file (<c>handle</c>: the directory is gone too, §9.1.13.6 item 1's '30'),
    /// and when the handle is taken but the new file's store cannot be made (<c>attach</c>: the medium refuses the empty store's
    /// first write, '30'), the transaction A made stands in the registry, as the CLOSE left it, until the ROLLBACK ends it, and A
    /// holds none. A host that keeps a file while a handle on it stands (Windows) refuses the removal, which is the host's own
    /// protection of the kept state.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "handle")]
    [InlineData(FileOrg.Relative, "attach")]
    [InlineData(FileOrg.Indexed, "handle")]
    [InlineData(FileOrg.Indexed, "attach")]
    public void A_refused_reopen_of_an_optional_file_that_vanished_leaves_the_retired_kept_state_as_the_close_left_it(FileOrg org,
        string refusal)
    {
        string dir = Tmp($"pb3493-vanished-refused-{org}-{refusal}");
        Directory.CreateDirectory(dir);
        string host = Path.Combine(dir, "F.dat");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true, optional: true);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            try
            {
                File.Delete(host);
                if (refusal == "handle") Directory.Delete(dir);
            }
            catch (IOException) when (OperatingSystem.IsWindows())
            {
                Assert.Null(a.Rollback());   // the host kept the file with the handle: there is no vanished file to re-OPEN
                a.CloseAll();
                return;
            }
            if (refusal == "attach")
                KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, write: _ => new IOException("refused by the host"));
            try { a.OpenStatic("F", FileOpenMode.IO); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("30", a.Status("F"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            Assert.Equal(id, a.RetiredTransactionOf("F"));
            Assert.Null(a.Rollback());
            Assert.Equal(0ul, a.RetiredTransactionOf("F"));
            a.CloseAll();
        }
        finally
        {
            TryDelete(host);
            ScratchDirectory.Remove(dir);
        }
    }

    /// <summary>Outside the run unit, take the physical file at <paramref name="host"/> away — <c>removed</c> (unlinked) or
    /// <c>renamed</c> to <paramref name="moved"/> — and make another one there, holding record 2 (<c>B002</c>), through another run
    /// unit's OPEN OUTPUT. False when the host refuses to take away a file a handle holds (Windows: the kept handle's share mode
    /// admits no delete), which is the host's own protection of the kept state.</summary>
    private static bool ReplaceOutsideTheRunUnit(string host, string moved, string how, FileOrg org)
    {
        try
        {
            if (how == "renamed") File.Move(host, moved);
            else File.Delete(host);
        }
        catch (IOException) when (OperatingSystem.IsWindows()) { return false; }
        catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows()) { return false; }
        var maker = Open(host, org, commit: false, FileOpenMode.Output, name: "S");
        Assert.Equal("00", Write(maker, org, 2, "B002", name: "S"));
        maker.CloseAll();
        return true;
    }

    /// <summary>⛔ THE KEPT STATE IS OF A PHYSICAL FILE, AND A PATH NAMES ONE ONLY WHEN IT IS ASKED (kb/Work PB3533, PB3576; the
    /// decision of kb/Work PB3493 for a kept file that is not there, DESIGN-runtime-library.md 2.2.1.10): A changes record 7 under
    /// commit control and is CLOSEd; outside the run unit the file is removed (or renamed away) and another made at the same path,
    /// holding record 2. A's re-OPEN is an ordinary OPEN of the file the path names: it reads record 2 and finds no record 7 or 1
    /// ('23'), no transaction of A's stands in it, and a change reaches the new file. What the CLOSE kept RETIRES with the file it
    /// was kept on (the registry keeps it as it keeps the state of a connector associated with another file, kb/Work PB3494), so
    /// the COMMIT (§14.9.7.4 GR3, <i>"permanently applies any changes made to the files specified in all the active APPLY COMMIT
    /// clauses"</i>) commits BOTH files' changes: record 8 in the file at the path and record 7 in the renamed one, never abandoned
    /// to the next writable OPEN's rollback. Before the fix the re-OPEN adopted the kept handle on the removed file and read its
    /// records ('23' for record 2, '00' for record 7); before kb/Work PB3576 the kept state was ended, and record 7 of the renamed
    /// file was rolled back unseen.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, FileOpenMode.IO, "removed")]
    [InlineData(FileOrg.Relative, FileOpenMode.Input, "renamed")]
    [InlineData(FileOrg.Indexed, FileOpenMode.IO, "renamed")]
    [InlineData(FileOrg.Indexed, FileOpenMode.Input, "removed")]
    public void A_reopen_of_a_file_replaced_outside_the_run_unit_retires_what_the_close_kept(FileOrg org, FileOpenMode mode, string how)
    {
        string host = Tmp($"pb3533-replaced-{org}-{mode}-{how}");
        string moved = host + ".old";
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            if (!ReplaceOutsideTheRunUnit(host, moved, how, org))
            {
                Assert.Null(a.Rollback());
                a.CloseAll();
                return;
            }
            a.OpenStatic("F", mode);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            Assert.Equal("00", Read(a, org, 2, out var image));
            Assert.Equal(Rec(2, "B002"), image);
            Assert.Equal("23", Read(a, org, 7, out _));
            Assert.Equal("23", Read(a, org, 1, out _));
            if (mode == FileOpenMode.IO) Assert.Equal("00", Write(a, org, 8, "B008"));
            Assert.Null(a.Commit());
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
            var expected = new string?[10];
            expected[2] = Rec(2, "B002");
            if (mode == FileOpenMode.IO) expected[8] = Rec(8, "B008");
            Assert.Equal(expected, Contents(host, org));
            if (how == "renamed")
            {
                var renamed = Seeded();
                renamed[7] = Rec(7, "A007");   // the COMMIT reached the file the run unit changed, under its new name
                Assert.Equal(renamed, Settled(moved, org));
            }
        }
        finally
        {
            TryDelete(host);
            TryDelete(moved);
        }
    }

    /// <summary>The same decision when the re-OPEN of the file made at the path is UNSUCCESSFUL (kb/Work PB3533, PB3576; §14.9.27.4
    /// GR25, <i>"If the execution of the OPEN statement is unsuccessful, the file is not affected"</i>, speaks of the file the path
    /// names, and the kept one is not it): the path names a file that is not a keyed file (GR10's fixed file attribute conflict,
    /// '39'), or one another run unit holds under SHARING WITH NO OTHER (§9.1.13.9 1), '61'). The kept state has retired all the
    /// same — A holds no transaction of its own, and the ROLLBACK restores the retired one — and the file at the path is as the other
    /// party left it.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "39")]
    [InlineData(FileOrg.Relative, "61")]
    [InlineData(FileOrg.Indexed, "39")]
    [InlineData(FileOrg.Indexed, "61")]
    public void An_unsuccessful_reopen_of_a_file_replaced_outside_the_run_unit_retires_what_the_close_kept(FileOrg org, string refusal)
    {
        string host = Tmp($"pb3533-replaced-refused-{org}-{refusal}");
        FileRegistry? holder = null;
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            try { File.Delete(host); }
            catch (IOException) when (OperatingSystem.IsWindows())
            {
                Assert.Null(a.Rollback());   // the host kept the file with the handle: nothing was made in its place
                a.CloseAll();
                return;
            }
            if (refusal == "39") File.WriteAllText(host, "not a keyed file\n");
            else
            {
                holder = Open(host, org, commit: false, FileOpenMode.Output, name: "S", sharing: FileSharing.NoOther);
                Assert.Equal("00", Write(holder, org, 2, "B002", name: "S"));
            }
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal(refusal, a.Status("F"));
            Assert.Equal(0ul, a.TransactionOf("F"));
            Assert.Null(a.Rollback());
            a.CloseAll();
            if (holder is null)
                Assert.Equal("not a keyed file\n", File.ReadAllText(host));
            else
            {
                holder.CloseAll();
                holder = null;
                var expected = new string?[10];
                expected[2] = Rec(2, "B002");
                Assert.Equal(expected, Contents(host, org));
            }
        }
        finally
        {
            holder?.CloseAll();
            TryDelete(host);
        }
    }

    /// <summary>⛔ TWO PHYSICAL FILES AT ONE PATH IN ONE RUN UNIT, EACH WITH ITS OWN STORE AND TRANSACTION (kb/Work PB3533, the
    /// run-unit tables' sibling of the kept state): connector F, under commit control, holds the file open and changes record 7;
    /// outside the run unit the file is removed and another made at the path, holding record 2; connector G of the same run unit
    /// OPENs the path. G reaches the file the path names now — record 2, no record 1 or 7 — while F goes on reading the removed
    /// file it holds; and the COMMIT commits BOTH transactions, one per file. Before the fix the run unit's store table gave G the
    /// removed file's store (G read record 1 and record 7), and the COMMIT, telling files by their path, committed only one.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Two_connectors_on_a_path_whose_file_was_replaced_each_reach_their_own_file(FileOrg org)
    {
        string host = Tmp($"pb3533-two-files-{org}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true, name: "F");
            Declare(a, host, org, commit: true, name: "G");
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 7, "A007", name: "F"));
            if (!ReplaceOutsideTheRunUnit(host, host + ".old", "removed", org))
            {
                Assert.Null(a.Rollback());
                a.CloseAll();
                return;
            }
            a.OpenStatic("G", FileOpenMode.IO);
            Assert.Equal("00", a.Status("G"));
            Assert.Equal("00", Read(a, org, 2, out var image, name: "G"));
            Assert.Equal(Rec(2, "B002"), image);
            Assert.Equal("23", Read(a, org, 7, out _, name: "G"));
            Assert.Equal("23", Read(a, org, 1, out _, name: "G"));
            Assert.Equal("00", Read(a, org, 1, out image, name: "F"));
            Assert.Equal(Rec(1, "A001"), image);
            Assert.Equal("00", Write(a, org, 8, "B008", name: "G"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));
            Assert.NotEqual(0ul, a.TransactionOf("G"));
            Assert.Null(a.Commit());
            Assert.Equal(0ul, a.TransactionOf("F"));
            Assert.Equal(0ul, a.TransactionOf("G"));
            a.CloseAll();
            var expected = new string?[10];
            expected[2] = Rec(2, "B002");
            expected[8] = Rec(8, "B008");
            Assert.Equal(expected, Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ THE RUN UNIT'S FILE AND RECORD LOCKS ARE OF A PHYSICAL FILE, NOT OF A PATH (kb/Work PB3576; §9.1.16, <i>"While
    /// locked by a given file connector, a record is not accessible to another file connector"</i>; §9.1.13.9 1), a file opened by
    /// a connector with SHARING WITH NO OTHER refuses another connector's OPEN): connector F holds the file open and READs record 2
    /// WITH LOCK; outside the run unit the file is removed (or renamed away) and another made at the path, holding a record 2 of its
    /// own (<c>B002</c>); connector G of the same run unit OPENs the path. G meets nothing of F's: its OPEN is '00' even where F holds
    /// the old file under NO OTHER (an OPEN against the Table 19 entry of a file that is not at the path, '61', before the fix), and
    /// its READ of record 2 WITH LOCK is '00' and reads <c>B002</c> ('51' before the fix: F's lock on a record of the OLD file); F
    /// reads <c>A002</c> from the file it holds. Both connectors keep their own locks to the end.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "removed", FileSharing.AllOther)]
    [InlineData(FileOrg.Relative, "renamed", FileSharing.NoOther)]
    [InlineData(FileOrg.Indexed, "renamed", FileSharing.AllOther)]
    [InlineData(FileOrg.Indexed, "removed", FileSharing.NoOther)]
    public void Two_connectors_on_a_path_whose_file_was_replaced_meet_none_of_each_others_locks(FileOrg org, string how,
        FileSharing sharing)
    {
        string host = Tmp($"pb3576-locks-{org}-{how}-{sharing}");
        string moved = host + ".old";
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: false, name: "F", sharing);
            Declare(a, host, org, commit: false, name: "G", sharing);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            if (sharing == FileSharing.AllOther)
                Assert.Equal("00", Read(a, org, 2, out _, phrase: FileRecordLock.WithLock, name: "F"));
            if (!ReplaceOutsideTheRunUnit(host, moved, how, org))
            {
                a.CloseAll();   // Windows: the host kept the file with the handle, there is no replaced file to meet
                return;
            }
            a.OpenStatic("G", FileOpenMode.IO);
            Assert.Equal("00", a.Status("G"));
            Assert.Equal("00", Read(a, org, 2, out var image, phrase: sharing == FileSharing.AllOther ? FileRecordLock.WithLock : FileRecordLock.None, name: "G"));
            Assert.Equal(Rec(2, "B002"), image);
            Assert.Equal("00", Read(a, org, 2, out image, name: "F"));
            Assert.Equal(Rec(2, "A002"), image);
            a.CloseAll();
        }
        finally
        {
            TryDelete(host);
            TryDelete(moved);
        }
    }

    /// <summary>⛔ DELETE FILE ASKS ABOUT THE PHYSICAL FILE, NOT ITS PATH (kb/Work PB3576; §9.1.13.9 2), a DELETE FILE statement
    /// refused when the physical file is <i>"currently open by another file connector"</i>): connector F holds the file open;
    /// outside the run unit the file is removed (or renamed away) and another made at the path; connector G's DELETE FILE deletes
    /// THE FILE AT THE PATH ('00', the file gone), because F is open on a file that is not at the path any more — a control
    /// DELETE FILE beside an F that holds the file at the path is '62'. Before the fix the registry compared F's path with G's
    /// and refused the new file's deletion ('62').</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "removed")]
    [InlineData(FileOrg.Indexed, "renamed")]
    public void A_delete_file_of_a_path_whose_file_was_replaced_is_refused_by_no_connector_open_on_the_old_file(FileOrg org, string how)
    {
        string host = Tmp($"pb3576-delete-{org}-{how}");
        string moved = host + ".old";
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            Declare(a, host, org, commit: false, name: "F");
            Declare(a, host, org, commit: false, name: "G");
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("62", a.DeleteFile("G"));   // F holds the file at the path: another connector has it open
            if (!ReplaceOutsideTheRunUnit(host, moved, how, org))
            {
                a.CloseAll();   // Windows: the host kept the file with the handle, there is no replaced file to delete
                return;
            }
            Assert.Equal("00", a.DeleteFile("G"));
            Assert.False(File.Exists(host));
            Assert.Equal("00", Read(a, org, 1, out var image, name: "F"));   // F goes on reading the file it holds
            Assert.Equal(Rec(1, "A001"), image);
            a.CloseAll();
        }
        finally
        {
            TryDelete(host);
            TryDelete(moved);
        }
    }

    /// <summary>⛔ A FILE MADE AFTER ANOTHER WAS REMOVED MAY BE GIVEN THE REMOVED FILE'S NUMBER, AND IS STILL THE FILE ITS CONNECTOR
    /// OPENED (review R1-1 of kb/Work PB3576; §9.1.13.9 1) a), a file opened by one connector with SHARING WITH NO OTHER refuses
    /// another connector's OPEN; §9.1.13.9 2), DELETE FILE of a file open by another connector is '62'): F OPENs OUTPUT (creating
    /// the file), CLOSEs and DELETEs the file, then OPENs OUTPUT again. A host that gives a freed file number to the next file made
    /// (Linux ext4) gives the new file the number the first file's state still claimed. Connector G of the run unit then meets F's
    /// Table 19 entry at once: DELETE FILE is '62' and OPEN INPUT is '61', as they are beside any other open connector.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_file_made_with_the_number_of_a_removed_file_is_met_by_the_connector_that_made_it(FileOrg org)
    {
        string host = Tmp($"pb3576-number-{org}");
        try
        {
            var a = new FileRegistry();
            Declare(a, host, org, commit: false, name: "F", FileSharing.NoOther);
            Declare(a, host, org, commit: false, name: "G", FileSharing.NoOther);
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("00", a.Status("F"));
            a.Close("F");
            Assert.Equal("00", a.DeleteFile("F"));
            Assert.False(File.Exists(host));
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("62", a.DeleteFile("G"));
            a.OpenStatic("G", FileOpenMode.Input);
            Assert.Equal("61", a.Status("G"));
            a.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ THE LOCKS A RETIRED KEPT STATE HOLDS ARE THE OLD FILE'S, AND THE COMMIT GIVES THEM BACK WITH IT (kb/Work PB3576,
    /// PB3578; §9.1.16, <i>"A COMMIT statement releases all record locks for files subject to commit and rollback"</i>): on a host
    /// whose region locks answer unavailable for record bytes, F, under commit control, READs record 2 (the lock is in this run
    /// unit's table alone) and is CLOSEd, which keeps it; outside the run unit the file is renamed away and another made at the
    /// path, holding a record 2 of its own. F's re-OPEN is an ordinary OPEN of the new file: F holds no record lock there, and
    /// connector H of the run unit READs the new record 2 ('00', never the old file's lock's '51'). The retired kept state still holds
    /// its lock on the renamed file until the COMMIT, which gives it back: no record lock stays in the run unit's table for a
    /// connector with nothing left to release it, and a connector of the run unit that OPENs the renamed file meets '51' on record
    /// 2 until then and '00' after.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_retired_kept_states_unpublished_record_locks_belong_to_its_file_and_end_with_the_commit(FileOrg org)
    {
        string host = Tmp($"pb3576-retired-locks-{org}");
        string moved = host + ".old";
        try
        {
            Seed(host, org);
            var unavailable = new RecordBytesUnavailableHost(HostRegionLocks.HostOfThisProcess);
            using var forced = HostRegionLocks.Forcing(unavailable);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true);
            Declare(a, host, org, commit: false, name: "H");
            Declare(a, moved, org, commit: false, name: "M");
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Read(a, org, 2, out _));
            Assert.True(unavailable.RecordBytesRefused > 0, "the READ's record lock never reached the host as a record byte");
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(1, a.RecordLocksHeldBy("F"));
            if (!ReplaceOutsideTheRunUnit(host, moved, "renamed", org))
            {
                Assert.Null(a.Rollback());   // Windows: the kept handle's share mode admits no rename
                a.CloseAll();
                return;
            }
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(0, a.RecordLocksHeldBy("F"));
            a.OpenStatic("H", FileOpenMode.IO);
            Assert.Equal("00", a.Status("H"));
            Assert.Equal("00", Read(a, org, 2, out var image, name: "H"));
            Assert.Equal(Rec(2, "B002"), image);
            a.OpenStatic("M", FileOpenMode.IO);
            Assert.Equal("00", a.Status("M"));
            Assert.Equal("51", Read(a, org, 2, out _, name: "M"));   // the retired state still holds F's lock on the renamed file
            Assert.Null(a.Commit());
            Assert.Equal("00", Read(a, org, 2, out image, name: "M"));
            Assert.Equal(Rec(2, "A002"), image);
            a.CloseAll();
        }
        finally
        {
            TryDelete(host);
            TryDelete(moved);
        }
    }

    /// <summary>Open connector <paramref name="name"/> of <paramref name="reg"/> in I-O mode on the physical file the content of
    /// its ASSIGN USING data item names (§12.4.5.3 GR3 b): the association is made by the OPEN, so one connector reaches
    /// different physical files over a run unit.</summary>
    private static void OpenAssigned(FileRegistry reg, string host, string name = "F") =>
        reg.OpenShared(name, FileOpenMode.IO, hasSharingOverride: false, FileSharing.AllOther, FileRetryKind.None, 0,
            OpenTapePhrase.None, host, assignDynamic: true, page: null);

    /// <summary>⛔ WHAT A CLOSE UNDER COMMIT CONTROL KEPT BELONGS TO THE PHYSICAL FILE, NOT TO THE CONNECTOR (kb/Work PB3494;
    /// §12.4.5.3 GR3 b, <i>"the file connector referenced by file-name-1 is associated with a physical file identified by the
    /// content of the data item referenced by data-name-1 in the runtime element that executes the OPEN"</i>; §14.9.7.4 GR3 and
    /// GR4; §14.9.36.4 GR3; §9.1.16): A, under commit control, changes record 7 of host 1, READs record 1 and is CLOSEd; a MOVE
    /// to the ASSIGN USING item and an OPEN associate it with host 2, which holds three records of its own. The OPEN is an
    /// ordinary OPEN of host 2 - it adopts neither host 1's handle nor its store, so record 5 is '23' and record 2 is host 2's -
    /// while host 1's transaction, record lock and file lock stand (another run unit's REWRITE of record 7 answers '51', its READ
    /// of record 1 '51', a NO OTHER OPEN '61') until the COMMIT or ROLLBACK ends them at host 1, host 2 untouched, or until the
    /// connector is associated with host 1 again and OPENs it ("back"), where the transaction goes on, or until the run unit's end
    /// ("end"), whose implicit COMMIT (§9.1.18.5, <i>"When a run unit terminates normally, all file changes are permanently committed"</i>) makes
    /// host 1's change permanent and gives the locks back. Before the fix the OPEN
    /// of host 2 took host 1's kept handle and store: record 5 was host 1's and the new file got no file lock.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "commit")]
    [InlineData(FileOrg.Relative, "rollback")]
    [InlineData(FileOrg.Relative, "back")]
    [InlineData(FileOrg.Relative, "end")]
    [InlineData(FileOrg.Indexed, "commit")]
    [InlineData(FileOrg.Indexed, "rollback")]
    [InlineData(FileOrg.Indexed, "back")]
    [InlineData(FileOrg.Indexed, "end")]
    public void A_kept_state_stays_with_the_physical_file_its_connector_is_associated_away_from(FileOrg org, string ending)
    {
        string host1 = Tmp($"pb3494-first-{org}-{ending}");
        string host2 = Tmp($"pb3494-second-{org}-{ending}");
        try
        {
            Seed(host1, org);
            var seed = Open(host2, org, commit: false, FileOpenMode.Output, name: "S");
            for (int n = 1; n <= 3; n++) Assert.Equal("00", Write(seed, org, n, $"B{n:000}", name: "S"));
            seed.CloseAll();
            var theirs = new string?[10];
            for (int n = 1; n <= 3; n++) theirs[n] = Rec(n, $"B{n:000}");
            var a = new FileRegistry();
            Declare(a, host1, org, commit: true);
            OpenAssigned(a, host1);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Read(a, org, 1, out _));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            OpenAssigned(a, host2);
            Assert.Equal("00", a.Status("F"));
            Assert.True(HostFile.PhysicalFileComparer.Equals(host2, a.HostPathOf("F")));
            Assert.Equal("23", Read(a, org, 5, out _));
            Assert.Equal("00", Read(a, org, 2, out var second));
            Assert.Equal(Rec(2, "B002"), second);
            Assert.Equal(0ul, a.TransactionOf("F"));
            // Host 1's state is held without a connector on it.
            var c = Open(host1, org, commit: false, name: "C", sharing: FileSharing.AllOther);
            Assert.Equal("51", Rewrite(c, org, 7, "C007", name: "C"));
            Assert.Equal("51", Read(c, org, 1, out _, name: "C"));
            c.CloseAll();
            var d = new FileRegistry();
            Declare(d, host1, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("61", d.Status("D"));
            if (ending == "back")
            {
                a.Close("F");
                Assert.Equal("00", a.Status("F"));
                OpenAssigned(a, host1);
                Assert.Equal("00", a.Status("F"));
                Assert.Equal(id, a.TransactionOf("F"));
                Assert.Equal("00", Read(a, org, 7, out var own));
                Assert.Equal(Rec(7, "A007"), own);
                Assert.Null(a.Rollback());
            }
            else if (ending == "end") a.CloseAll();   // the run unit's normal end (§9.1.18.5): the implicit COMMIT, then the locks go
            else Assert.Null(ending == "commit" ? a.Commit() : a.Rollback());
            if (ending != "back")
            {
                d.OpenStatic("D", FileOpenMode.IO);
                Assert.Equal("00", d.Status("D"));   // host 1's file lock ended with the COMMIT or ROLLBACK
                d.CloseAll();
            }
            a.CloseAll();
            var expected = Seeded();
            if (ending is "commit" or "end") expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host1, org));
            Assert.Equal(theirs, Contents(host2, org));
        }
        finally { TryDelete(host1); TryDelete(host2); }
    }

    /// <summary>The run unit's end ends the state kept on a file its connector was associated away from (§9.1.18.5, <i>"all file
    /// and record locks are then released"</i>; kb/Work PB3494), and a ROLLBACK the medium refuses at that file keeps its
    /// transaction for the dead-owner step and is told through the file-name (§9.1.13.1): the connector's status is '30' and the
    /// name returned, while the other file's COMMIT is not touched.</summary>
    [Fact]
    public void A_refused_rollback_of_a_file_its_connector_was_associated_away_from_is_told_through_the_file_name()
    {
        const FileOrg org = FileOrg.Relative;
        string host1 = Tmp("pb3494-refused-first");
        string host2 = Tmp("pb3494-refused-second");
        try
        {
            Seed(host1, org);
            Seed(host2, org, records: 3);
            bool refuse = false;
            IOException? Refusal() => refuse ? new IOException("refused by the host") : null;
            var a = new FileRegistry();
            Declare(a, host1, org, commit: true);
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, write: _ => Refusal(), sync: Refusal);
            try { OpenAssigned(a, host1); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            OpenAssigned(a, host2);
            Assert.Equal("00", a.Status("F"));
            refuse = true;
            Assert.Equal("F", a.Rollback());
            Assert.Equal("30", a.Status("F"));
            refuse = false;
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host1, org));   // the dead-owner step of the next writable OPEN rolled the transaction back
        }
        finally { TryDelete(host1); TryDelete(host2); }
    }

    /// <summary>⛔ A COMMIT TAKES THE RUN UNIT'S FILES IN THE ORDER OF ITS OPEN STORES, a retired state's file at its own place
    /// (DESIGN-runtime-library.md 2.2.1.10, <i>"Files are committed one at a time, in the order of the run unit's open
    /// stores"</i>; review findings R1-2 and R1-5 of kb/Work PB3494), and a refused COMMIT of a state its connector was
    /// associated away from is told through the file-name (§9.1.13.1): G opens host 3 and then F host 1, both under commit
    /// control, and each changes record 7; F is CLOSEd and an ASSIGN USING OPEN associates it with host 2. The COMMIT, with host
    /// 1 refusing every write and sync, commits host 3 first and stops at host 1: F's status is '30' and its name returned. The
    /// ROLLBACK that §14.6.13.1.3 item 8 then executes restores host 1 and finds host 3 committed. Before the fix every
    /// retired state came first, so the COMMIT stopped at host 1 before host 3 and the ROLLBACK undid host 3's change.</summary>
    [Fact]
    public void A_commit_takes_a_retired_files_transaction_at_its_place_in_the_order_of_the_open_stores()
    {
        const FileOrg org = FileOrg.Relative;
        string host1 = Tmp("pb3494-order-first");
        string host2 = Tmp("pb3494-order-second");
        string host3 = Tmp("pb3494-order-third");
        try
        {
            Seed(host1, org);
            Seed(host2, org, records: 3);
            Seed(host3, org);
            var a = new FileRegistry();
            Declare(a, host3, org, commit: true, name: "G");
            Declare(a, host1, org, commit: true);
            a.OpenStatic("G", FileOpenMode.IO);
            Assert.Equal("00", a.Status("G"));
            bool refuse = false;
            IOException? Refusal() => refuse ? new IOException("refused by the host") : null;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, write: _ => Refusal(), sync: Refusal);
            try { OpenAssigned(a, host1); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Write(a, org, 7, "G007", name: "G"));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            a.Close("F");
            OpenAssigned(a, host2);
            Assert.Equal("00", a.Status("F"));
            refuse = true;
            Assert.Equal("F", a.Commit());
            Assert.Equal("30", a.Status("F"));
            refuse = false;
            Assert.Null(a.Rollback());
            a.CloseAll();
            var committed = Seeded();
            committed[7] = Rec(7, "G007");
            Assert.Equal(committed, Settled(host3, org));
            Assert.Equal(Seeded(), Settled(host1, org));
        }
        finally { TryDelete(host1); TryDelete(host2); TryDelete(host3); }
    }

    /// <summary>⛔ A CLOSE UNDER COMMIT CONTROL KEEPS THE RECORD LOCKS OF ITS FILE UNTIL THE COMMIT OR ROLLBACK (§14.9.6.4 GR9,
    /// <i>"Except when the file is specified in an APPLY COMMIT clause, the file lock and any record locks associated with the
    /// file connector referenced by file-name-1 are released by the execution of the CLOSE statement"</i>; §9.1.16, <i>"A
    /// COMMIT statement releases all record locks for files subject to commit and rollback"</i>, and the ROLLBACK's the same;
    /// kb/Work PB3480): a connector READs record 1 under the implicit automatic locking (§12.4.5.9.4 GR4), is CLOSEd, and
    /// another run unit's READ of record 1 answers '51' (§9.1.13.8) while a record nobody locked reads; the same after the
    /// connector's own re-OPEN (<paramref name="reopen"/>), the kept handle adopted (I-O) or given back for another (INPUT) —
    /// the locks are published through the handle's endpoint, which the re-OPEN moves them off before it gives the handle
    /// back, so the one release at the COMMIT or ROLLBACK is the only one; and a second CLOSE of the closed file ('42', GR1)
    /// releases nothing. The COMMIT or ROLLBACK then releases them.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "", true)]
    [InlineData(FileOrg.Relative, "", false)]
    [InlineData(FileOrg.Relative, "input", true)]
    [InlineData(FileOrg.Relative, "input", false)]
    [InlineData(FileOrg.Relative, "io", true)]
    [InlineData(FileOrg.Relative, "io", false)]
    [InlineData(FileOrg.Indexed, "", true)]
    [InlineData(FileOrg.Indexed, "", false)]
    [InlineData(FileOrg.Indexed, "input", true)]
    [InlineData(FileOrg.Indexed, "input", false)]
    [InlineData(FileOrg.Indexed, "io", true)]
    [InlineData(FileOrg.Indexed, "io", false)]
    public void A_close_under_commit_control_keeps_its_record_locks_until_the_commit_or_rollback(FileOrg org, string reopen, bool commit)
    {
        string host = Tmp($"pb3480-close-{org}-{reopen}-{commit}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Read(a, org, 1, out _));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            if (reopen == "")
            {
                a.Close("F");
                Assert.Equal("42", a.Status("F"));   // GR1: an unsuccessful CLOSE performs none of its actions
            }
            else
            {
                a.OpenStatic("F", reopen == "io" ? FileOpenMode.IO : FileOpenMode.Input);
                Assert.Equal("00", a.Status("F"));
            }
            var b = Open(host, org, commit: false, name: "B");
            Assert.Equal("51", Read(b, org, 1, out _, name: "B"));
            Assert.Equal("00", Read(b, org, 2, out _, name: "B"));
            Assert.Null(commit ? a.Commit() : a.Rollback());
            Assert.Equal("00", Read(b, org, 1, out var image, name: "B"));
            Assert.Equal(Rec(1, "A001"), image);
            b.CloseAll();
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ AN UNSUCCESSFUL RE-OPEN LEAVES THE KEPT RECORD LOCKS WHERE THE CLOSE KEPT THEM (§14.9.27.4 GR25, <i>"If the
    /// execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.6.4 GR9; kb/Work PB3480): an
    /// INDEXED connector under commit control OPENs INPUT, READs record 1 and is CLOSEd; its re-OPEN EXTEND takes a handle of
    /// its own (the kept one is read-only), the record locks move to it (the store mutex held), and the OPEN is refused at its
    /// last step, the read of the prime tree's root that finds the highest key (§14.9.27.4 GR15; §9.1.13.6 item 1: '30') -
    /// after the attach. The locks go back to the handle the CLOSE kept, so another run unit's READ of record 1 still answers
    /// '51' until the ROLLBACK.</summary>
    [Fact]
    public void A_refused_reopen_leaves_the_kept_record_locks_standing()
    {
        const FileOrg org = FileOrg.Indexed;
        string host = Tmp("pb3480-refused");
        try
        {
            Seed(host, org);
            bool armed = false;
            uint root = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                read: page => armed && page == root ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, org, commit: true, FileOpenMode.Input); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Read(a, org, 1, out _));
            root = a.PagedStoreOf("F")!.Meta.PrimeRoot.Page;
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.PagedStoreOf("F")!.DropCache();
            armed = true;
            a.OpenStatic("F", FileOpenMode.Extend);
            Assert.Equal("30", a.Status("F"));
            armed = false;
            var b = Open(host, org, commit: false, name: "B");
            Assert.Equal("51", Read(b, org, 1, out _, name: "B"));
            Assert.Null(a.Rollback());
            Assert.Equal("00", Read(b, org, 1, out _, name: "B"));
            b.CloseAll();
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The same, when the kept handle had to be GIVEN BACK EARLY (review finding R1-2): under an OPEN phrase SHARING WITH NO OTHER the
    /// kept handle (SHARING WITH ALL OTHER) refuses the exclusive one the re-OPEN takes, so it goes first and the record locks published through it leave the host with
    /// it; the OPEN is then refused at its last step, and its own handle goes too. The locks are still this run unit's, in its
    /// table, until the ROLLBACK - a second connector of the same run unit reading record 1 meets '51' (§14.9.27.4 GR25;
    /// §14.9.6.4 GR9), where the first fix released them with the failed OPEN's handle and answered '00'. And they are
    /// ANOTHER run unit's '51' too: the unsuccessful OPEN takes the kept handle again (kb/Work PB3483, RetakeKeptHandle) and
    /// publishes them through it again (PhysicalFileTable.Republish), where the merge of PB3483 under PB3480 first left them in
    /// the table alone and answered B '00'.</summary>
    [Fact]
    public void A_refused_reopen_that_gave_the_kept_handle_back_leaves_the_record_locks_standing()
    {
        const FileOrg org = FileOrg.Indexed;
        string host = Tmp("pb3480-refused-early");
        try
        {
            Seed(host, org);
            bool armed = false;
            uint root = 0;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                read: page => armed && page == root ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, org, commit: true, FileOpenMode.Input); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Read(a, org, 1, out _));
            root = a.PagedStoreOf("F")!.Meta.PrimeRoot.Page;
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.PagedStoreOf("F")!.DropCache();
            armed = true;
            a.OpenShared("F", FileOpenMode.Extend, hasSharingOverride: true, FileSharing.NoOther, FileRetryKind.None, 0,
                OpenTapePhrase.None, assign: "", assignDynamic: false, page: null);
            Assert.Equal("30", a.Status("F"));
            armed = false;
            Declare(a, host, org, commit: false, name: "G");
            a.OpenStatic("G", FileOpenMode.IO);
            Assert.Equal("00", a.Status("G"));
            Assert.Equal("51", Read(a, org, 1, out _, name: "G"));
            Assert.Equal("00", Read(a, org, 2, out _, name: "G"));
            var b = Open(host, org, commit: false, name: "B");
            Assert.Equal("51", Read(b, org, 1, out _, name: "B"));
            Assert.Equal("00", Read(b, org, 2, out _, name: "B"));
            // The retaken handle's endpoint is the one they are published through now (KeptFile.Endpoint): a re-OPEN that takes a
            // handle of its own beside it moves them off it before it gives it back, so B still meets them.
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("51", Read(b, org, 1, out _, name: "B"));
            Assert.Null(a.Rollback());
            Assert.Equal("00", Read(a, org, 1, out _, name: "G"));
            Assert.Equal("00", Read(b, org, 1, out _, name: "B"));
            b.CloseAll();
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A RE-OPEN WHOSE KEPT HANDLE CANNOT BE STORE-ADOPTED AGAIN STILL LEAVES WHAT THE CLOSE KEPT STANDING (kb/Work PB3497;
    /// §14.9.27.4 GR25, <i>"If the execution of the OPEN statement is unsuccessful, the file is not affected"</i>; §14.9.6.4 GR9,
    /// <i>"Except when the file is specified in an APPLY COMMIT clause, the file lock and any record locks associated with the file
    /// connector referenced by file-name-1 are released by the execution of the CLOSE statement"</i>; §9.1.16, <i>"While locked by a
    /// given file connector, a record is not accessible to another file connector in the same or a different run unit"</i>):
    /// A, under commit control and SHARING WITH ALL OTHER, READs record 1 (its record lock) and WRITEs record 7 (its
    /// transaction), and is CLOSEd. Its re-OPEN I-O under SHARING WITH NO OTHER takes a handle the kept one's share mode refuses,
    /// so the kept handle is given back (and with it, the store's last handle: the store has no file) and the new one taken -
    /// and the medium refuses the adoption's first read ('30', §9.1.13.6 item 1). The OPEN's unsuccessful end retakes the kept
    /// handle (<c>KeyedConnector.RetakeKeptHandle</c>), and the medium refuses the retake's read of the store too, so the retake
    /// cannot adopt the store again - the case the first fix of PB3483 left without the handle, its record locks unpublished and
    /// its transaction's liveness byte free. The handle is kept all the same: the medium, which answers again, is not what the
    /// CLOSE kept. Another run unit's READ of record 1 answers '51' and so does its READ and its REWRITE of the record the
    /// transaction wrote (the dead-owner step of its writable OPEN found the owner alive and rolled nothing back), its OPEN
    /// SHARING WITH NO OTHER is refused '61' (§9.1.13.9 1)), and A's re-OPEN, which reads the store again, is the transaction's
    /// and ends at a ROLLBACK that restores the file. A host that enforces share modes per handle (Windows) is the one that
    /// takes the retake; elsewhere the kept handle is never given back and the case passes through the OPEN's own refusal.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reopen_whose_kept_handle_cannot_adopt_the_store_again_leaves_what_the_close_kept_standing(FileOrg org)
    {
        string host = Tmp($"pb3497-retake-refused-{org}");
        try
        {
            Seed(host, org);
            bool armed = false;
            KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, read: _ => armed ? new IOException("refused by the host") : null);
            FileRegistry a;
            try { a = Open(host, org, commit: true, sharing: FileSharing.AllOther); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Read(a, org, 1, out _));
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            a.PagedStoreOf("F")!.DropCache();
            armed = true;
            a.OpenShared("F", FileOpenMode.IO, hasSharingOverride: true, FileSharing.NoOther, FileRetryKind.None, 0,
                OpenTapePhrase.None, assign: "", assignDynamic: false, page: null);
            Assert.Equal("30", a.Status("F"));
            armed = false;
            Assert.Equal(id, a.TransactionOf("F"));
            var b = Open(host, org, commit: false, name: "B");   // its dead-owner step: A's liveness byte is held
            Assert.Equal("51", Read(b, org, 1, out _, name: "B"));
            Assert.Equal("00", Read(b, org, 2, out _, name: "B"));
            Assert.Equal("51", Read(b, org, 7, out _, name: "B"));
            Assert.Equal("51", Rewrite(b, org, 7, "B007", name: "B"));
            b.CloseAll();
            var d = new FileRegistry();
            Declare(d, host, org, commit: false, name: "D", sharing: FileSharing.NoOther);
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("61", d.Status("D"));
            // The medium answers again: the re-OPEN reads the store, and is the transaction's.
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            Assert.Equal("00", Read(a, org, 7, out var own));
            Assert.Equal(Rec(7, "A007"), own);
            Assert.Null(a.Rollback());
            a.CloseAll();
            d.OpenStatic("D", FileOpenMode.IO);
            Assert.Equal("00", d.Status("D"));
            d.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>A second connector's registration with a CLOSEd connector's name (the activation of its element again after
    /// a CANCEL, kb/Work PB168) DISPLACES the first, and what its CLOSE under commit control kept goes on being the COMMIT's
    /// and the ROLLBACK's (§12.4.6.3.4 GR5: a canceled runtime element's APPLY COMMIT clause stays active until a COMMIT or
    /// ROLLBACK deactivates it; §14.9.7.4 GR3 and GR4: the COMMIT applies the changes made to the files of the active clauses
    /// and releases the file locks of those closed and not reopened; kb/Work PB3484): another run unit's OPEN WITH NO OTHER is
    /// refused '61' until then (§9.1.13.9 1)), the changes the displaced connector made are committed or rolled back, and
    /// the same OPEN then succeeds — before the fix the kept handle was dropped with the connector, held its lock for the life
    /// of the process and was never reached by either statement. The connector is displaced CLOSEd, or still OPEN (an
    /// activation that ended abnormally: its implicit CLOSE, <see cref="FileRegistry.CloseIfOpen"/>, kb/Work PB168).</summary>
    [Theory]
    [InlineData(FileOrg.Relative, true, true, true)]
    [InlineData(FileOrg.Relative, true, true, false)]
    [InlineData(FileOrg.Relative, true, false, true)]
    [InlineData(FileOrg.Relative, false, true, true)]
    [InlineData(FileOrg.Relative, false, true, false)]
    [InlineData(FileOrg.Indexed, true, true, true)]
    [InlineData(FileOrg.Indexed, true, true, false)]
    [InlineData(FileOrg.Indexed, true, false, false)]
    [InlineData(FileOrg.Indexed, false, true, true)]
    [InlineData(FileOrg.Indexed, false, true, false)]
    public void A_connector_displaced_by_a_registration_leaves_what_its_close_kept_to_the_commit(
        FileOrg org, bool closedFirst, bool changed, bool commit)
    {
        string host = Tmp($"pb3484-displaced-{org}-{closedFirst}-{changed}-{commit}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.NoOther);
            Assert.Equal("00", changed ? Write(a, org, 7, "A007") : Read(a, org, 1, out _));
            if (closedFirst)
            {
                a.Close("F");
                Assert.Equal("00", a.Status("F"));
            }
            Declare(a, host, org, commit: true, sharing: FileSharing.NoOther);   // the registration displaces the first connector
            var b = new FileRegistry();
            Register(b, "B", host, org, FileSharing.NoOther);
            if (org == FileOrg.Indexed) b.AddAlternateKey("B", [new(4, 4)], duplicates: false);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("61", b.Status("B"));
            Assert.Null(commit ? a.Commit() : a.Rollback());
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            b.CloseAll();
            a.CloseAll();
            var expected = Seeded();
            if (commit && changed) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The element's NEXT activation opens the file again beside the displaced connector's kept state
    /// (§12.4.6.3.4 GR5; kb/Work PB3484): the replacement attaches to the run unit's store, so the transaction the displaced
    /// connector began is the replacement's too (one transaction per run unit and physical file, 2.2.1.10) - it reads the
    /// uncommitted record, changes another, and one COMMIT or ROLLBACK ends both connectors' share of it (the displaced
    /// one's kept handle is given back with it).</summary>
    [Theory]
    [InlineData(FileOrg.Relative, true)]
    [InlineData(FileOrg.Relative, false)]
    [InlineData(FileOrg.Indexed, true)]
    [InlineData(FileOrg.Indexed, false)]
    public void A_replacement_connector_shares_the_transaction_of_the_connector_it_displaced(FileOrg org, bool commit)
    {
        string host = Tmp($"pb3484-replacement-{org}-{commit}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true);
            Assert.Equal("00", Write(a, org, 7, "A007"));
            ulong id = a.TransactionOf("F");
            Assert.NotEqual(0ul, id);
            a.Close("F");
            Declare(a, host, org, commit: true);   // the registration displaces the CLOSEd connector
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            Assert.Equal("00", Read(a, org, 7, out var own));
            Assert.Equal(Rec(7, "A007"), own);
            Assert.Equal("00", Write(a, org, 8, "A008"));
            Assert.Null(commit ? a.Commit() : a.Rollback());
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
            var expected = Seeded();
            if (commit) (expected[7], expected[8]) = (Rec(7, "A007"), Rec(8, "A008"));
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The replacement's OPEN under SHARING WITH NO OTHER (the I-O default) is not refused by the connector it
    /// displaced (review finding R1-1): that connector is closed (or abandoned open, and closed at the displacement) and of
    /// THIS run unit, and §9.1.13.9 1) owes '61' only against a physical file <i>"currently open by another file connector"</i>
    /// - the kept handle is what is left of the closed connector's file lock (§9.1.15), which prevents OTHER run units. The
    /// element's next activation takes the transaction over as a re-OPEN of the connector does, another run unit's OPEN stays
    /// refused '61' until the COMMIT or ROLLBACK, and that one COMMIT or ROLLBACK ends the replacement's and the displaced
    /// connector's share of it.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "write-closed", true)]
    [InlineData(FileOrg.Relative, "write-closed", false)]
    [InlineData(FileOrg.Relative, "read-closed", true)]
    [InlineData(FileOrg.Relative, "write-open", true)]
    [InlineData(FileOrg.Relative, "write-open", false)]
    [InlineData(FileOrg.Relative, "read-open", false)]
    [InlineData(FileOrg.Indexed, "write-closed", true)]
    [InlineData(FileOrg.Indexed, "write-closed", false)]
    [InlineData(FileOrg.Indexed, "read-closed", true)]
    [InlineData(FileOrg.Indexed, "write-open", true)]
    [InlineData(FileOrg.Indexed, "write-open", false)]
    [InlineData(FileOrg.Indexed, "read-open", false)]
    public void A_replacement_connector_opens_a_file_under_no_other_the_displaced_connector_kept(FileOrg org, string how, bool commit)
    {
        string host = Tmp($"pb3484-noother-{org}-{how}-{commit}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.NoOther);
            bool writes = how.StartsWith("write", StringComparison.Ordinal);
            Assert.Equal("00", writes ? Write(a, org, 7, "A007") : Read(a, org, 1, out _));
            ulong id = a.TransactionOf("F");
            if (how.EndsWith("closed", StringComparison.Ordinal)) a.Close("F");
            Declare(a, host, org, commit: true, sharing: FileSharing.NoOther);   // the element's next activation, after its CANCEL
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(id, a.TransactionOf("F"));
            if (writes) Assert.Equal("00", Read(a, org, 7, out _));
            var other = new FileRegistry();
            Register(other, "B", host, org, FileSharing.NoOther);
            if (org == FileOrg.Indexed) other.AddAlternateKey("B", [new(4, 4)], duplicates: false);
            other.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("61", other.Status("B"));   // the file lock stands against the other run unit until the COMMIT
            Assert.Null(commit ? a.Commit() : a.Rollback());
            Assert.Equal(0ul, a.TransactionOf("F"));
            a.CloseAll();
            other.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", other.Status("B"));
            other.CloseAll();
            var expected = Seeded();
            if (commit && writes) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The same for the instance-file connector an object's deletion drops (§9.1.4, kb/Work PB603:
    /// <see cref="FileRegistry.CloseAndDrop"/>, and the GC-requested close <see cref="FileRegistry.CloseAll"/> drains): what the
    /// connector's CLOSE under commit control kept goes on being the next COMMIT's or ROLLBACK's, or the run unit's end's
    /// (§9.1.18.5, <i>"all file and record locks are then released"</i>; kb/Work PB3484) — another run unit's OPEN WITH NO
    /// OTHER is refused '61' until then and succeeds after, and the object's changes are committed or rolled back.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "drop", true)]
    [InlineData(FileOrg.Relative, "drop", false)]
    [InlineData(FileOrg.Indexed, "drop", true)]
    [InlineData(FileOrg.Indexed, "drop", false)]
    [InlineData(FileOrg.Relative, "queued", true)]
    [InlineData(FileOrg.Indexed, "queued", true)]
    public void An_instance_file_dropped_with_its_object_leaves_what_its_close_kept_to_the_commit(FileOrg org, string how, bool commit)
    {
        string host = Tmp($"pb3484-dropped-{org}-{how}-{commit}");
        try
        {
            Seed(host, org);
            var a = new FileRegistry();
            string key = a.MintInstanceKey("F");
            Declare(a, host, org, commit: true, name: key, sharing: FileSharing.NoOther);
            a.OpenStatic(key, FileOpenMode.IO);
            Assert.Equal("00", a.Status(key));
            Assert.Equal("00", Write(a, org, 7, "A007", name: key));
            var b = new FileRegistry();
            Register(b, "B", host, org, FileSharing.NoOther);
            if (org == FileOrg.Indexed) b.AddAlternateKey("B", [new(4, 4)], duplicates: false);
            if (how == "drop")
            {
                a.CloseAndDrop(key);
                b.OpenStatic("B", FileOpenMode.IO);
                Assert.Equal("61", b.Status("B"));
                Assert.Null(commit ? a.Commit() : a.Rollback());
            }
            else
            {
                a.EnqueueInstanceClose(key);   // the GC finalizer's request; the run unit's end performs it
                a.CloseAll();
            }
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            b.CloseAll();
            a.CloseAll();
            var expected = Seeded();
            if (commit) expected[7] = Rec(7, "A007");
            Assert.Equal(expected, Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>A run unit's start (<see cref="FileRegistry.Reset"/>, which runs the implicit CLOSE of whatever an earlier run
    /// unit left, §14.6.11 2)) and its end (<see cref="FileRegistry.CloseAll"/>) give back what a CLOSE under commit control
    /// kept — the file lock, the handle, the record locks (§9.1.18.5) — so another run unit's OPEN WITH NO OTHER succeeds and
    /// its READ of the record the first run unit locked answers '00' (kb/Work PB3484; the sibling arms of the displaced and
    /// dropped connectors).</summary>
    [Theory]
    [InlineData(FileOrg.Relative, "reset")]
    [InlineData(FileOrg.Relative, "close-all")]
    [InlineData(FileOrg.Indexed, "reset")]
    [InlineData(FileOrg.Indexed, "close-all")]
    public void A_run_units_boundary_gives_back_what_a_close_under_commit_control_kept(FileOrg org, string boundary)
    {
        string host = Tmp($"pb3484-boundary-{org}-{boundary}");
        try
        {
            Seed(host, org);
            var a = Open(host, org, commit: true, sharing: FileSharing.NoOther);
            Assert.Equal("00", Read(a, org, 1, out _));
            a.Close("F");
            var b = new FileRegistry();
            Register(b, "B", host, org, FileSharing.NoOther);
            if (org == FileOrg.Indexed) b.AddAlternateKey("B", [new(4, 4)], duplicates: false);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("61", b.Status("B"));
            if (boundary == "reset") a.Reset(); else a.CloseAll();
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", b.Status("B"));
            Assert.Equal("00", Read(b, org, 1, out _, name: "B"));
            b.CloseAll();
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

    /// <summary>Leave an ABANDONED transaction on <paramref name="host"/>, as the <c>abnormal-always</c> row above does: a
    /// run unit under commit control changes records 7 and 1, ends abnormally with every rollback refused by the medium
    /// ('30'), and ends (its implicit CLOSE refused the same), so the transaction stands "as a death leaves it" (2.2.1.10)
    /// for the next writable OPEN's dead-owner step. The run unit's connectors are opened through <paramref name="forced"/>
    /// when one is named.</summary>
    private static void Abandon(string host, FileOrg org, IRegionLockHost? forced = null)
    {
        int refusals = 0;
        KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
            write: page => refusals > 0 && page >= MetaPage.MetaPages ? new IOException("refused by the host") : null);
        FileRegistry a;
        try
        {
            using (forced is null ? null : HostRegionLocks.Forcing(forced)) a = Open(host, org, commit: true);
        }
        finally { KeyedStore.MediumObserver.Value = null; }
        Assert.Equal("00", Write(a, org, 7, "A007"));
        Assert.Equal("00", Rewrite(a, org, 1, "X001"));
        refusals = 1;
        a.EndTransactions(abnormal: true);
        Assert.Equal("30", a.Status("F"));
        Assert.NotEqual(0ul, a.TransactionOf("F"));
        a.CloseAll();
        refusals = 0;
    }

    /// <summary>⛔ AN ABANDONED TRANSACTION IS ROLLED BACK BY THE NEXT WRITABLE OPEN WHILE ANOTHER COPY OF ITS OWNER'S HANDLE
    /// LIVES (kb/Work PB3549; §9.1.18.5, <i>"When a run unit terminates abnormally, all file changes made since the previous
    /// commit or, if none, the beginning of the run unit are rolled back"</i>, and <i>"all file and record locks are then
    /// released"</i>): the kept file's liveness byte is unlocked by its give-back (<c>KeptFile.GiveBack</c>, the endpoint's
    /// disposal), never left to the handle's close. A <c>fork</c> in another thread holds a copy of every descriptor until
    /// its <c>exec</c>, and while it does a lock left to the close stands, so the next writable OPEN read the dead owner as
    /// ALIVE and left its transaction in the file (the intermittent Linux red of
    /// <see cref="A_transaction_whose_rollback_was_refused_is_never_committed"/> in a parallel run). The copy is made here
    /// deterministically, for every handle the abandoning run unit opens (<see cref="DescriptionCopyingHost"/>), and kept
    /// open until the file has been settled.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_abandoned_transaction_is_rolled_back_while_a_copy_of_its_owners_handle_lives(FileOrg org)
    {
        string host = Tmp($"pb3549-copy-{org}");
        var copies = new DescriptionCopyingHost(HostRegionLocks.HostOfThisProcess);
        try
        {
            Seed(host, org);
            Abandon(host, org, copies);
            Assert.True(copies.Copied > 0 || !(OperatingSystem.IsLinux() || OperatingSystem.IsWindows()),
                "a copy of the abandoning run unit's handle lives while the file is settled");
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally
        {
            copies.Dispose();
            TryDelete(host);
        }
    }

    /// <summary>The same row UNDER LOAD (kb/Work PB3549's stress harness): round after round, a transaction is abandoned
    /// while another thread starts child processes without pause — every one a real <c>fork</c> holding a copy of every
    /// descriptor until its <c>exec</c> — and then <see cref="ParallelOpeners"/> run units OPEN the file I-O at once, racing
    /// for its dead-owner step: the first to take the store mutex rolls the transaction back (§9.1.18.5), and every OPEN,
    /// whichever it is, finds no transaction left once it is open; the file reads as it was seeded.</summary>
    [Fact]
    public void An_abandoned_transaction_is_rolled_back_by_parallel_writable_opens_while_child_processes_start()
    {
        const int Rounds = 6;
        var churn = new ChildProcessChurn();
        try
        {
            for (int round = 0; round < Rounds; round++)
            {
                string host = Tmp($"pb3549-stress-{round}");
                try
                {
                    Seed(host, FileOrg.Indexed);
                    Abandon(host, FileOrg.Indexed);
                    var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
                    using var start = new Barrier(ParallelOpeners);
                    var openers = Enumerable.Range(0, ParallelOpeners).Select(i => new Thread(() =>
                    {
                        try
                        {
                            start.SignalAndWait();
                            var writer = Open(host, FileOrg.Indexed, commit: false, name: $"S{i}");
                            AssertNoTransactionLeft(writer, $"S{i}");
                            writer.CloseAll();
                        }
                        catch (Exception e) { failures.Enqueue(e); }
                    })).ToList();
                    openers.ForEach(t => t.Start());
                    openers.ForEach(t => t.Join());
                    Assert.Empty(failures);
                    Assert.Equal(Seeded(), Contents(host, FileOrg.Indexed));
                }
                finally { TryDelete(host); }
            }
        }
        finally { churn.Dispose(); }
        Assert.Null(churn.Failure);
        Assert.True(churn.Started > 0, "child processes were started while the rounds ran");
    }

    /// <summary>How many run units OPEN the file at once in the stress harness.</summary>
    private const int ParallelOpeners = 4;

    /// <summary>A thread that starts a trivial child process again and again until disposed (the stress harness's
    /// forks); the first start is awaited, so the rounds begin with the churn running.</summary>
    private sealed class ChildProcessChurn : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _first = new();
        private volatile bool _stop;
        private int _started;

        internal ChildProcessChurn()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "pb3549-fork-churn" };
            _thread.Start();
            _first.Wait();
        }

        /// <summary>How many children were started.</summary>
        internal int Started => Volatile.Read(ref _started);

        /// <summary>What stopped the churn early, or null.</summary>
        internal Exception? Failure { get; private set; }

        private void Run()
        {
            try
            {
                while (!_stop)
                {
                    using var child = System.Diagnostics.Process.Start(OperatingSystem.IsWindows()
                        ? new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c exit 0") { UseShellExecute = false, CreateNoWindow = true }
                        : new System.Diagnostics.ProcessStartInfo("/bin/sh", "-c true") { UseShellExecute = false });
                    child?.WaitForExit();
                    Interlocked.Increment(ref _started);
                    _first.Set();
                }
            }
            catch (Exception e) { Failure = e; }
            finally { _first.Set(); }
        }

        public void Dispose()
        {
            _stop = true;
            _thread.Join();
            _first.Dispose();
        }
    }

    /// <summary>A region-lock host that delegates to its inner host and, for every handle it is given, makes a second handle
    /// on the same open file (<see cref="DescriptionCopy"/>): the deterministic stand-in for a <c>fork</c> in another thread
    /// that has not reached its <c>exec</c>. The copies live until the host is disposed.</summary>
    private sealed class DescriptionCopyingHost(IRegionLockHost inner) : IRegionLockHost, IDisposable
    {
        private readonly List<Microsoft.Win32.SafeHandles.SafeFileHandle> _copies = [];

        /// <summary>How many copies were made.</summary>
        public int Copied { get { lock (_copies) return _copies.Count; } }

        public RegionLocks Open(Microsoft.Win32.SafeHandles.SafeFileHandle handle, string hostPath, bool writable)
        {
            if (DescriptionCopy.Of(handle) is { } copy)
                lock (_copies) _copies.Add(copy);
            return inner.Open(handle, hostPath, writable);
        }

        public void Dispose()
        {
            lock (_copies)
            {
                foreach (var copy in _copies) copy.Dispose();
                _copies.Clear();
            }
        }
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

    /// <summary>A RETIRED connector's record locks the host could not publish are given back by the COMMIT or the ROLLBACK as a
    /// registered one's are (review finding R1-3): §9.1.16, <i>"A COMMIT statement releases all record locks for files subject
    /// to commit and rollback"</i> (and the ROLLBACK's sentence). An object's instance file READs record 1 on a host whose
    /// region locks answer unavailable for record bytes (the lock is recorded in this run unit's table alone), the object is
    /// deleted (<see cref="FileRegistry.CloseAndDrop"/>, its minted key never named again), and another connector of the run
    /// unit meets '51' on record 1 until the COMMIT or ROLLBACK - then '00'.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, true)]
    [InlineData(FileOrg.Relative, false)]
    [InlineData(FileOrg.Indexed, true)]
    [InlineData(FileOrg.Indexed, false)]
    public void A_dropped_instance_files_unpublished_record_locks_end_with_the_commit_or_rollback(FileOrg org, bool commit)
    {
        string host = Tmp($"pb3484-unpublished-{org}-{commit}");
        try
        {
            Seed(host, org);
            var unavailable = new RecordBytesUnavailableHost(HostRegionLocks.HostOfThisProcess);
            using var forced = HostRegionLocks.Forcing(unavailable);
            var a = new FileRegistry();
            string key = a.MintInstanceKey("F");
            Declare(a, host, org, commit: true, name: key);
            a.OpenStatic(key, FileOpenMode.IO);
            Assert.Equal("00", a.Status(key));
            Assert.Equal("00", Read(a, org, 1, out _, name: key));
            Assert.True(unavailable.RecordBytesRefused > 0, "the READ's record lock never reached the host as a record byte");
            a.CloseAndDrop(key);
            Declare(a, host, org, commit: false, name: "B");
            a.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", a.Status("B"));
            Assert.Equal("51", Read(a, org, 1, out _, name: "B"));
            Assert.Equal("00", Read(a, org, 2, out _, name: "B"));
            Assert.Null(commit ? a.Commit() : a.Rollback());
            Assert.Equal("00", Read(a, org, 1, out _, name: "B"));
            a.CloseAll();
            Assert.Equal(Seeded(), Settled(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ A KEPT STATE THAT ENDS INSIDE THE CONNECTOR GIVES BACK THE RECORD LOCKS THE HOST COULD NOT PUBLISH TOO (kb/Work
    /// PB3578; §14.9.27.4 GR18, <i>"the successful execution of the OPEN statement creates the file"</i>; §9.1.16): on a host whose
    /// region locks answer unavailable for record bytes, connector F, under commit control, READs record 2 (the lock is in this run
    /// unit's table alone), is CLOSEd (the CLOSE keeps it, §14.9.6.4 GR9: one lock held) and re-OPENed OUTPUT, which creates the file
    /// anew and ends what the CLOSE kept (PB3493: the transaction ended with the file it was in). Nothing of the old file is held by
    /// F any more: no lock stays in the table for a connector with nothing left to release it. Before the fix
    /// <c>KeyedConnector.ReleaseKept</c> released only the locks its endpoint published, so the unpublished one stayed.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_open_output_over_the_kept_file_gives_back_the_record_locks_the_host_could_not_publish(FileOrg org)
    {
        string host = Tmp($"pb3578-output-{org}");
        try
        {
            Seed(host, org);
            var unavailable = new RecordBytesUnavailableHost(HostRegionLocks.HostOfThisProcess);
            using var forced = HostRegionLocks.Forcing(unavailable);
            var a = new FileRegistry();
            Declare(a, host, org, commit: true);
            a.OpenStatic("F", FileOpenMode.IO);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal("00", Read(a, org, 2, out _));
            Assert.True(unavailable.RecordBytesRefused > 0, "the READ's record lock never reached the host as a record byte");
            Assert.Equal(1, a.RecordLocksHeldBy("F"));
            a.Close("F");
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(1, a.RecordLocksHeldBy("F"));   // §14.9.6.4 GR9: the CLOSE under commit control keeps it
            a.OpenStatic("F", FileOpenMode.Output);
            Assert.Equal("00", a.Status("F"));
            Assert.Equal(0, a.RecordLocksHeldBy("F"));
            a.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>A region-lock host that delegates to its inner host but cannot carry a RECORD's byte (a filesystem without
    /// byte-range locks, answering <see cref="RegionLockResult.Unavailable"/>): the store mutex, the liveness bytes and the
    /// presence byte are carried as ever.</summary>
    private sealed class RecordBytesUnavailableHost(IRegionLockHost inner) : IRegionLockHost
    {
        /// <summary>How many record bytes were refused: the witness that the host was asked one (review lead R2-1).</summary>
        public int RecordBytesRefused => _refused;

        private int _refused;

        public RegionLocks Open(Microsoft.Win32.SafeHandles.SafeFileHandle handle, string hostPath, bool writable) =>
            new Endpoint(inner.Open(handle, hostPath, writable), this);

        private sealed class Endpoint(RegionLocks endpoint, RecordBytesUnavailableHost owner) : RegionLocks(endpoint.Writable)
        {
            private bool IsRecordByte(long offset)
            {
                if (!HostRegionLocks.IsRecordLockByte(offset)) return false;
                Interlocked.Increment(ref owner._refused);
                return true;
            }

            internal override bool ExclusiveNeedsWritableHandle => endpoint.ExclusiveNeedsWritableHandle;

            internal override RegionLockResult Hold(long offset, bool exclusive, bool wait) =>
                IsRecordByte(offset) ? RegionLockResult.Unavailable : endpoint.Hold(offset, exclusive, wait);

            internal override RegionLockResult HeldByAnother(long offset) =>
                IsRecordByte(offset) ? RegionLockResult.Unavailable : endpoint.HeldByAnother(offset);

            internal override void Release(long offset) => endpoint.Release(offset);

            public override void Dispose() => endpoint.Dispose();
        }
    }
}
