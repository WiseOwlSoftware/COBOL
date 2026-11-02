// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using static WiseOwl.Cobol.Tests.Shared.FileHarness;
using static WiseOwl.Cobol.Tests.Unit.Store.TransactionTrees;

namespace WiseOwl.Cobol.Tests.Unit.Store;

/// <summary>
/// ⛔ A DEAD APPLY COMMIT OWNER'S UNCOMMITTED CHANGES ARE NOT IN THE FILE FOR ANY READER (kb/Work PB4097; DESIGN-runtime-library.md
/// 2.2.1.10 "The dead owner's rollback is every reader's too"). §9.1.18.5: <i>"When a run unit terminates abnormally, all file
/// changes made since the previous commit or, if none, the beginning of the run unit are rolled back for all files for which there
/// are active APPLY COMMIT clauses."</i> — and only then <i>"In both cases all file and record locks are then released"</i>; so no
/// interval exists in which the dead run unit's locks are gone and its uncommitted change is still in the file (§9.1.17, §9.1.16;
/// §9.1.18.3: the changes <i>"remain in that state even in the event of subsequent abnormal termination"</i> are the committed
/// ones). The OPEN of a connector with no writable handle settles the dead owners through a handle opened for the purpose, and so
/// does a reader's statement entry when an owner dies beside an open reader; the IGNORING LOCK read ignores a LIVE owner's lock
/// (§14.9.30.4 GR12), never a rollback that is owed.
/// <para>The death is the witness of the rest of 2.2.1.12: the run unit's writes after <see cref="DyingPageMedium.DieAfter"/> never
/// reach the file and <c>CloseAll</c> frees its handles and liveness byte as the operating environment does. Expected values are
/// the file as of the dead run unit's last COMMIT (<see cref="CommitRollbackRuntimeHarness.Seeded"/>).</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class DeadOwnerReaderTests : CommitRollbackRuntimeHarness
{
    public static TheoryData<FileOrg, FileSharing> OrgsBySharing()
    {
        var data = new TheoryData<FileOrg, FileSharing>();
        foreach (var org in KeyedOrgs)
            foreach (var sharing in new[] { FileSharing.ReadOnly, FileSharing.AllOther }) data.Add(org, sharing);
        return data;
    }

    /// <summary>A run unit under commit control that DELETEd K5, REWROTE K1 and WROTE K7 after its last COMMIT and died: its
    /// writes reached the file. Returns the owner and its medium; <see cref="Die"/> kills it, so a test can read beside it first.</summary>
    private static (FileRegistry Owner, DyingPageMedium Medium) Change(string host, FileOrg org, FileSharing sharing = FileSharing.AllOther)
    {
        DyingPageMedium? medium = null;
        KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
        FileRegistry a;
        try { a = Open(host, org, commit: true, sharing: sharing); }
        finally { KeyedStore.MediumObserver.Value = null; }
        Assert.Equal("00", Delete(a, org, 5));
        Assert.Equal("00", Rewrite(a, org, 1, "X001"));
        Assert.Equal("00", Write(a, org, 7, "A007"));
        return (a, medium!);
    }

    private static void Die(FileRegistry owner, DyingPageMedium medium)
    {
        medium.DieAfter = medium.Writes;   // nothing it does afterwards reaches the file
        owner.CloseAll();                  // its handles and their bytes go with it
    }

    /// <summary>The OPEN INPUT reader the standard's own words describe — no IGNORING LOCK, no sharing phrase of its own beyond the
    /// implementor default (READ ONLY) — and one that shares with everybody: after the death it reads K1 as COMMITted, K5 present, K7
    /// absent (the probe of the note: the relative file's R-SEVEN and the indexed file's uncommitted change were read with '00').</summary>
    [Theory]
    [MemberData(nameof(OrgsBySharing))]
    public void A_reader_that_opens_after_the_owner_died_reads_the_file_as_the_rollback_leaves_it(FileOrg org, FileSharing sharing)
    {
        string host = Tmp($"pb4097-open-{org}-{sharing}");
        try
        {
            Seed(host, org);
            var (a, medium) = Change(host, org);
            Die(a, medium);
            var reader = Open(host, org, commit: false, FileOpenMode.Input, name: "C", sharing: sharing);
            Assert.Equal(Seeded(), ReadAll(reader, org));
            AssertNoTransactionLeft(reader, "C");
            reader.CloseAll();
            Assert.Equal(Seeded(), Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The IGNORING LOCK read after the death returns the rolled-back state too: it bypasses a LIVE owner's locks
    /// (§14.9.30.4 GR12), not a rollback §9.1.18.5 already owes.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void The_ignoring_lock_read_after_the_death_reads_the_rolled_back_file(FileOrg org)
    {
        string host = Tmp($"pb4097-ignoring-{org}");
        try
        {
            Seed(host, org);
            var (a, medium) = Change(host, org);
            Die(a, medium);
            var reader = Open(host, org, commit: false, FileOpenMode.Input, name: "C");
            Assert.Equal(Seeded(), ReadAll(reader, org, ignoringLock: true));
            reader.CloseAll();
        }
        finally { TryDelete(host); }
    }

    /// <summary>The owner dies BESIDE an open reader (SHARING WITH ALL OTHER on both): while it lives the reader is refused its
    /// record ('51', §9.1.16's implementor-specified circumstance, DOC-A.1-152) and an IGNORING LOCK read sees the live change
    /// (§14.9.30.4 GR12); once the owner is dead the next statement of the reader — READ, IGNORING LOCK READ — enters, finds the
    /// dead transaction, settles it, and reads the rolled-back file.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void An_owner_that_dies_beside_an_open_reader_is_rolled_back_at_the_readers_next_statement(FileOrg org)
    {
        foreach (bool ignoringLock in new[] { false, true })
        {
            string host = Tmp($"pb4097-beside-{org}-{ignoringLock}");
            try
            {
                Seed(host, org);
                var reader = Open(host, org, commit: false, FileOpenMode.Input, name: "C");
                var (a, medium) = Change(host, org);
                Assert.Equal("51", Read(reader, org, 1, out _, name: "C"));                       // alive: the holding refuses
                Assert.Equal("00", Read(reader, org, 1, out var live, ignoringLock: true, name: "C"));
                Assert.Equal(Rec(1, "X001"), live);                                               // GR12: a live owner's lock is ignored
                Die(a, medium);
                Assert.Equal(Seeded(), ReadAll(reader, org, ignoringLock));
                AssertNoTransactionLeft(reader, "C");
                reader.CloseAll();
                Assert.Equal(Seeded(), Contents(host, org));
            }
            finally { TryDelete(host); }
        }
    }

    /// <summary>The owner LIVES: the reader's OPEN is refused as before ('61', §9.1.13.9 1) a): the owner's SHARING WITH NO OTHER
    /// holds the file), and nothing of the owner is rolled back — its transaction stands for its own COMMIT.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_live_owner_is_not_rolled_back_by_a_reader_and_still_refuses_its_open(FileOrg org)
    {
        string host = Tmp($"pb4097-live-{org}");
        try
        {
            Seed(host, org);
            var (a, _) = Change(host, org, FileSharing.NoOther);
            var reader = new FileRegistry();
            Declare(reader, host, org, commit: false, "C", FileSharing.ReadOnly);
            reader.OpenStatic("C", FileOpenMode.Input);
            Assert.Equal("61", reader.Status("C"));
            Assert.NotEqual(0ul, a.TransactionOf("F"));
            Assert.Null(a.Commit());
            a.CloseAll();
            reader.CloseAll();
            var committed = Seeded();
            committed[1] = Rec(1, "X001");
            committed[5] = null;
            committed[7] = Rec(7, "A007");
            Assert.Equal(committed, Contents(host, org));
        }
        finally { TryDelete(host); }
    }

    /// <summary>A reader that cannot perform the rollback is refused, never shown the dead change: the file is read-only to this
    /// process, so the settle's writable handle is refused by the host and the OPEN answers the authority it lacks ('37',
    /// §14.9.27.4 GR3). The check skips a host where the process may write a read-only file anyway (a superuser).</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_reader_that_cannot_write_the_file_is_refused_not_shown_the_dead_change(FileOrg org)
    {
        string host = Tmp($"pb4097-readonly-{org}");
        try
        {
            Seed(host, org);
            var (a, medium) = Change(host, org);
            Die(a, medium);
            File.SetAttributes(host, FileAttributes.ReadOnly);
            if (HostFile.PermitsWrite(host)) return;
            var reader = new FileRegistry();
            Declare(reader, host, org, commit: false, "C", FileSharing.ReadOnly);
            reader.OpenStatic("C", FileOpenMode.Input);
            Assert.Equal("37", reader.Status("C"));
            reader.CloseAll();
        }
        finally
        {
            if (File.Exists(host)) File.SetAttributes(host, FileAttributes.Normal);
            TryDelete(host);
        }
    }

    /// <summary>The same refusal at a STATEMENT's entry (review round 1, R1-4): the reader is open (SHARING WITH ALL OTHER) when the
    /// owner dies and the file becomes read-only to this process, so the reader's next READ finds the dead transaction and cannot
    /// settle it. The entry's host failure is a permanent error ('30', §9.1.13.6 1); <c>KeyedConnector.Enter</c>), in effect for the
    /// file's later statements (§9.1.13.1); the dead change is never returned, the IGNORING LOCK read (§14.9.30.4 GR12) included.</summary>
    [Theory]
    [MemberData(nameof(OrgsByIgnoring))]
    public void A_reader_whose_statement_entry_cannot_settle_the_dead_owner_answers_30_not_the_dead_change(FileOrg org, bool ignoringLock)
    {
        string host = Tmp($"pb4097-entry-{org}-{ignoringLock}");
        try
        {
            Seed(host, org);
            var reader = Open(host, org, commit: false, FileOpenMode.Input, name: "C");
            var (a, medium) = Change(host, org);
            File.SetAttributes(host, FileAttributes.ReadOnly);
            if (HostFile.PermitsWrite(host)) { Die(a, medium); reader.CloseAll(); return; }
            Die(a, medium);
            Assert.Equal("30", Read(reader, org, 1, out var image, ignoringLock, name: "C"));
            Assert.NotEqual(Rec(1, "X001"), image);
            Assert.Equal("30", Read(reader, org, 5, out _, ignoringLock, name: "C"));   // the permanent error stays in effect
            reader.CloseAll();
        }
        finally
        {
            if (File.Exists(host)) File.SetAttributes(host, FileAttributes.Normal);
            TryDelete(host);
        }
    }

    public static TheoryData<FileOrg, bool> OrgsByIgnoring()
    {
        var data = new TheoryData<FileOrg, bool>();
        foreach (var org in KeyedOrgs)
            foreach (bool ignoring in new[] { false, true }) data.Add(org, ignoring);
        return data;
    }

    /// <summary>Records 1 to 9 as one reader reads them (absent: null), ignoring locks when asked.</summary>
    private static string?[] ReadAll(FileRegistry reader, FileOrg org, bool ignoringLock = false)
    {
        var records = new string?[10];
        for (int n = 1; n <= 9; n++)
            records[n] = Read(reader, org, n, out var image, ignoringLock, name: "C") == "00" ? image : null;
        return records;
    }
}
