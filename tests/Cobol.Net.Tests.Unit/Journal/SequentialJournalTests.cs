// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit.Journal;

/// <summary>
/// The sequential journal (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2; kb/Work PB3340) over REAL files through the
/// deterministic fault-injecting medium (<see cref="FaultingHostMedium"/>, 2.2.2.12): each row of 2.2.2.12's table, run once
/// cleanly and then with a REFUSAL, a KILL and a POWER LOSS (losing, and keeping, every unsynced write) at EVERY operation it
/// makes on the medium, followed by "the next entry" — a fresh run unit's entry step through the production medium. Every
/// run asserts the model's invariants: <b>I1</b> the file holds exactly the state at the last COMMIT or exactly the state the
/// COMMIT made durable, never a third, and the program was told success only in the second case (ISO §9.1.18.3: changes made
/// permanent by a COMMIT <i>"remain in that state even in the event of subsequent abnormal termination"</i>; §9.1.18.5: an
/// abnormal termination rolls back <i>"all file changes made since the previous commit"</i>); and <b>I3</b> no holder, record,
/// before file or lock directory is left behind. Expected states are computed from the cited rules, never from a run.
/// </summary>
public sealed class SequentialJournalTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cobolnet-journal-" + Guid.NewGuid().ToString("N"));
    private readonly string _file;

    public SequentialJournalTests()
    {
        Directory.CreateDirectory(_root);
        _file = Path.Combine(_root, "DATA.DAT");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static byte[] Bytes(string records) => Encoding.ASCII.GetBytes(records);

    /// <summary>One run unit's connector over the journal, as the sequential connector drives it: its kept handle, its
    /// transaction, and what the program was told.</summary>
    private sealed class Connector(FaultingHostMedium medium, string file)
    {
        public SequentialJournal? Journal;
        public Stream? Kept;
        public bool ToldCommitted;
        public readonly List<SequentialJournal> Unretired = [];

        private string FileDirectory => Path.GetDirectoryName(file)!;

        public void Open() => Kept = medium.OpenData(file);

        public void Begin(JournalStart start)
        {
            using var mutex = SequentialJournal.EntryMutex(file);
            SequentialJournal.Enter(medium, file, Unretired, () => new RestoreTarget(medium, file));
            Journal = SequentialJournal.Begin(medium, file, start);
        }

        /// <summary>A WRITE: R3, then the append at the file's end.</summary>
        public void Append(string record)
        {
            long length = medium.Length(Kept!);
            Journal!.NoteAppend(length);
            medium.Write(Kept!, length, Bytes(record));
        }

        /// <summary>A REWRITE of the fixed four-byte record <paramref name="ordinal"/>: R2, then the overwrite in place.</summary>
        public void Rewrite(long ordinal, string record)
        {
            long offset = ordinal * 4;
            var image = new byte[4];
            medium.Read(Kept!, offset, image);
            Journal!.NoteRewrite(ordinal, offset, image);
            medium.Write(Kept!, offset, Bytes(record));
        }

        /// <summary>An OPEN OUTPUT of the existing file: R4's capture, then the truncation.</summary>
        public void OpenOutput()
        {
            using var mutex = SequentialJournal.EntryMutex(file);
            Journal!.Capture(CaptureKind.OpenOutput, Kept!);
            medium.SetLength(Kept!, 0);
        }

        /// <summary>A DELETE FILE: R4's capture, the kept handle closed, the file removed and its directory synced.</summary>
        public void DeleteFile()
        {
            using var mutex = SequentialJournal.EntryMutex(file);
            Journal!.Capture(CaptureKind.DeleteFile, Kept!);
            Kept!.Dispose();
            Kept = null;
            medium.DeleteFile(file);
            medium.SyncDirectory(FileDirectory);
        }

        /// <summary>A creation (an OPEN OUTPUT of an absent file, in a transaction begun with START A or REPLACED by a capture).</summary>
        public void Create() => Kept = medium.OpenRestore(file, create: true);

        public void Commit()
        {
            var journal = Journal!;
            switch (journal.Commit(Kept))
            {
                case SequentialJournal.CommitOutcome.Committed:
                    ToldCommitted = true;
                    Journal = null;
                    if (!journal.Retired) Unretired.Add(journal);
                    return;
                case SequentialJournal.CommitOutcome.Unknown:
                    if (!journal.Reverse()) throw new IOException("the reversal was refused again");
                    break;
            }
            Rollback();   // item 8's unconditional ROLLBACK (§14.6.13.1.3 8)); the COMMIT answered '30'
            throw new IOException("the COMMIT was unsuccessful");
        }

        public void Rollback()
        {
            using var mutex = SequentialJournal.EntryMutex(file);
            Journal!.Rollback(new KeptTarget(this, medium, file));
            Journal = null;
        }

        /// <summary>The run unit's end after a refusal: an owed rollback run again (§14.6.12's file-only rollback), an unfinished
        /// retirement completed, every handle closed.</summary>
        public void End()
        {
            if (Journal is not null) Rollback();
            foreach (var j in Unretired) j.TryFinishRetirement();
            Kept?.Dispose();
            Kept = null;
        }
    }

    /// <summary>The owner's restore target: its kept handle.</summary>
    private sealed class KeptTarget(Connector connector, FaultingHostMedium medium, string file) : JournalTarget
    {
        /// <summary>The kept handle, or — when a DELETE FILE closed it and its removal was refused, so the file is still there —
        /// a handle opened for the restore.</summary>
        internal override Stream? Writable() =>
            connector.Kept ??= medium.Probe(file) == FilePresence.Absent ? null : medium.OpenRestore(file, create: false);

        internal override Stream Create() => connector.Kept = medium.OpenRestore(file, create: true);

        internal override void Remove()
        {
            connector.Kept?.Dispose();
            connector.Kept = null;
            medium.DeleteFile(file);
        }
    }

    /// <summary>A row: the file before (null: absent), the program, and the state its last COMMIT leaves (null: absent).</summary>
    private sealed record Row(string Name, string? Before, Action<Connector> Program, string? Committed);

    private static readonly Row[] Rows =
    [
        new("first WRITE under EXTEND, COMMIT", "C1__C2__", c => { c.Open(); c.Begin(JournalStart.Present); c.Append("X1__"); c.Commit(); },
            "C1__C2__X1__"),
        new("REWRITE twice, ROLLBACK", "C1__C2__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.Rewrite(0, "Y1__"); c.Rewrite(0, "Y2__"); c.Rollback(); }, "C1__C2__"),
        new("R1-1 append then capture, ROLLBACK", "C1__C2__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.Append("X1__"); c.OpenOutput(); c.Append("X2__"); c.Rollback(); }, "C1__C2__"),
        new("REWRITE then capture, ROLLBACK", "C1__C2__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.Rewrite(0, "Y1__"); c.OpenOutput(); c.Append("X1__"); c.Rollback(); },
            "C1__C2__"),
        new("R1-1 capture then REWRITE, ROLLBACK", "C1__C2__C3__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.OpenOutput(); c.Append("X1__"); c.Append("X2__"); c.Rewrite(0, "Y1__"); c.Rollback(); },
            "C1__C2__C3__"),
        new("DELETE FILE then COMMIT", "C1__C2__", c => { c.Open(); c.Begin(JournalStart.Present); c.DeleteFile(); c.Commit(); }, null),
        new("DELETE FILE, re-create, ROLLBACK", "C1__C2__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.DeleteFile(); c.Create(); c.Append("X1__"); c.Rollback(); }, "C1__C2__"),
        new("R1-2 created by OPEN OUTPUT, ROLLBACK", null,
            c => { c.Begin(JournalStart.Absent); c.Create(); c.Append("X1__"); c.Rollback(); }, null),
        new("created, COMMIT", null, c => { c.Begin(JournalStart.Absent); c.Create(); c.Append("X1__"); c.Commit(); }, "X1__"),
        new("create, DELETE FILE, re-create, COMMIT", null,
            c => { c.Begin(JournalStart.Absent); c.Create(); c.Append("X1__"); c.DeleteFile(); c.Create(); c.Append("X2__"); c.Commit(); }, "X2__"),
        new("COMMIT then more, ROLLBACK", "C1__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.Append("X1__"); c.Commit(); c.Begin(JournalStart.Present); c.Append("X2__"); c.Rollback(); },
            "C1__X1__"),
    ];

    public static TheoryData<string> RowNames() => [.. Rows.Select(r => r.Name)];

    private string? Content() => File.Exists(_file) ? Encoding.ASCII.GetString(FaultingHostMedium.ReadShared(_file)) : null;

    private string LockDirectory => SidecarLockFiles.LockDirectoryOf(_file);

    private string[] Leftovers() =>
        Directory.Exists(LockDirectory) ? [.. Directory.EnumerateFileSystemEntries(LockDirectory).Select(Path.GetFileName)!] : [];

    private void SetUp(Row row, FaultingHostMedium medium)
    {
        if (Directory.Exists(LockDirectory)) Directory.Delete(LockDirectory, recursive: true);
        if (row.Before is null) File.Delete(_file);
        else File.WriteAllBytes(_file, Bytes(row.Before));
        medium.TreatAsDurable(_file);
    }

    /// <summary>Run <paramref name="row"/> under <paramref name="medium"/>, then the next entry; answer whether the program was
    /// told its COMMIT succeeded, and the operations it made.</summary>
    private (bool told, int operations) Execute(Row row, FaultingHostMedium medium)
    {
        SetUp(row, medium);
        var connector = new Connector(medium, _file);
        bool dead = false;
        try { row.Program(connector); }
        catch (IOException) { }   // a refused statement: '30', fatal; item 8's ROLLBACK runs at the run unit's end below
        catch (MediumDeathException) { dead = true; }
        if (!dead) connector.End();
        medium.Kill();
        // The next entry: a fresh run unit's writable OPEN, through the production medium.
        using (SequentialJournal.EntryMutex(_file))
            SequentialJournal.Enter(HostMedium.Instance, _file, [], () => new RestoreTarget(HostMedium.Instance, _file));
        return (connector.ToldCommitted, medium.Operations);
    }

    [Theory]
    [MemberData(nameof(RowNames))]
    public void Every_row_leaves_the_state_of_the_last_commit_or_the_one_the_commit_made_durable_at_every_fault(string name)
    {
        var row = Rows.Single(r => r.Name == name);
        var clean = new FaultingHostMedium();
        var (cleanTold, operations) = Execute(row, clean);
        int firstDecision = clean.Decisions.Count > 0 ? clean.Decisions[0] : int.MaxValue;
        Assert.Equal(row.Committed, Content());
        Assert.Empty(Leftovers());
        bool commits = row.Committed != row.Before;
        Assert.Equal(commits, cleanTold);

        foreach (var fault in Enum.GetValues<MediumFault>())
        {
            for (int at = 0; at < operations; at++)
            {
                var medium = new FaultingHostMedium(at, fault);
                var (told, _) = Execute(row, medium);
                string where = $"{name}: {fault} at operation {at} ({(at < medium.Log.Count ? medium.Log[at] : "?")})";
                Assert.True(medium.Fired, where);
                string? content = Content();
                // I1: one of the two states, and success told only of the second.
                Assert.True(content == row.Before || content == row.Committed, $"{where}: a third state '{content}'");
                if (told) Assert.True(content == row.Committed, $"{where}: told committed, holds '{content}'");
                // Before the first COMMIT's decision was handed over, nothing is committed: the file is as it was.
                if (at < firstDecision) Assert.True(content == row.Before, $"{where}: undecided, holds '{content}'");
                if (fault == MediumFault.Refuse && !told) Assert.True(content == row.Before, $"{where}: refused, holds '{content}'");
                // I3: nothing left behind.
                Assert.True(Leftovers().Length == 0, $"{where}: left {string.Join(", ", Leftovers())}");
            }
        }
    }

    [Fact]
    public void A_covered_write_journals_its_committed_length_once_and_nothing_for_a_later_append()
    {
        // 2.2.2.13's measure: the transaction's first append adds one append and one sync of the record (R3), a later one none.
        var medium = new FaultingHostMedium();
        var row = new Row("three appends", "C1__", c => { c.Open(); c.Begin(JournalStart.Present); c.Append("X1__"); c.Append("X2__"); c.Append("X3__"); c.Commit(); },
            "C1__X1__X2__X3__");
        Execute(row, medium);
        var journalOps = medium.Log.Where(o => o.Contains("txn-0.journal", StringComparison.Ordinal)).ToList();
        Assert.Equal(["create txn-0.journal", "write txn-0.journal@0+24", "sync txn-0.journal", "write txn-0.journal@24+13",
            "sync txn-0.journal", "write txn-0.journal@37+6", "sync txn-0.journal", "delete txn-0.journal"], journalOps);
        Assert.Equal("C1__X1__X2__X3__", Content());
    }

    [Fact]
    public void The_write_ahead_rules_order_every_journal_operation_before_the_change_it_precedes()
    {
        // R1 (the header synced, then the directory, before the first change), R3 (L synced before the append), R2 (the image
        // synced before the overwrite), R4 (the before file synced, the directory synced, then K synced, then the truncation).
        var medium = new FaultingHostMedium();
        var row = new Row("all four", "C1__C2__",
            c => { c.Open(); c.Begin(JournalStart.Present); c.Rewrite(1, "Y2__"); c.Append("X1__"); c.OpenOutput(); c.Rollback(); }, "C1__C2__");
        Execute(row, medium);
        int Index(string op) => medium.Log.IndexOf(op) is var i and >= 0 ? i : throw new Xunit.Sdk.XunitException($"no '{op}' in {string.Join(" | ", medium.Log)}");
        string lockDir = Path.GetFileName(LockDirectory);
        Assert.True(Index("sync txn-0.journal") < Index($"syncdir {lockDir}"));                       // R1
        Assert.True(Index($"syncdir {lockDir}") < Index("write DATA.DAT@4+4"));                        // R1, R2
        int image = Index("write txn-0.journal@24+29");                                                // R: 1 + 20 + 4 + 4
        Assert.True(image < medium.Log.IndexOf("sync txn-0.journal", image) && medium.Log.IndexOf("sync txn-0.journal", image) < Index("write DATA.DAT@4+4"));
        int length = Index("write txn-0.journal@53+13");                                               // L
        Assert.True(length < medium.Log.IndexOf("sync txn-0.journal", length) && medium.Log.IndexOf("sync txn-0.journal", length) < Index("write DATA.DAT@8+4"));
        int copy = Index("sync txn-0.before");
        int k = Index("write txn-0.journal@66+14");                                                    // K
        Assert.True(copy < medium.Log.IndexOf($"syncdir {lockDir}", copy) && medium.Log.IndexOf($"syncdir {lockDir}", copy) < k);
        Assert.True(medium.Log.IndexOf("sync txn-0.journal", k) < Index("setlength DATA.DAT=0"));         // R4: K synced before the truncation
        Assert.Equal("C1__C2__", Content());
    }

    [Fact]
    public void A_refused_sync_of_a_handed_over_capture_record_is_fatal_and_the_rollback_leaves_the_file_and_no_litter()
    {
        // R5 (2.2.2.5): the K handed over, its sync refused: the statement is '30', item 8's rollback reads K from the operating
        // environment (the before file holds what F still holds, F was never truncated), and nothing is left behind.
        var probe = new FaultingHostMedium();
        var row = new Row("capture", "C1__C2__", c => { c.Open(); c.Begin(JournalStart.Present); c.Append("X1__"); c.OpenOutput(); c.Rollback(); }, "C1__C2__");
        Execute(row, probe);
        int kSync = probe.Log.IndexOf("sync txn-0.journal", probe.Log.IndexOf("write txn-0.journal@37+14"));
        var medium = new FaultingHostMedium(kSync, MediumFault.Refuse);
        var (told, _) = Execute(row, medium);
        Assert.False(told);
        Assert.Equal("C1__C2__", Content());
        Assert.Empty(Leftovers());
    }

    [Fact]
    public void A_torn_tail_ends_the_records_and_a_torn_header_is_litter()
    {
        var medium = new FaultingHostMedium();
        string directory = LockDirectory;
        SetUp(new Row("", "C1__", _ => { }, null), medium);
        var journal = SequentialJournal.Begin(medium, _file, JournalStart.Present);
        journal.NoteAppend(4);
        string record = Path.Combine(directory, "txn-0.journal");
        byte[] whole = FaultingHostMedium.ReadShared(record);
        var parsed = SequentialJournal.Parse(whole);
        Assert.Equal(4L, parsed!.CommittedLength);
        Assert.Null(SequentialJournal.Parse(whole.AsSpan(0, whole.Length - 1))!.CommittedLength);   // a short record: torn
        byte[] bad = (byte[])whole.Clone();
        bad[^1] ^= 0xFF;
        Assert.Null(SequentialJournal.Parse(bad)!.CommittedLength);                                  // a failed CRC: torn
        bad = (byte[])whole.Clone();
        bad[3] ^= 0xFF;
        Assert.Null(SequentialJournal.Parse(bad));                                                   // a torn header: litter
        medium.Kill();
    }

    [Theory]
    [InlineData("Write", 0, 0, "RecordLocked")]       // the end is held by every live transaction
    [InlineData("Record", 0, 0, "RecordLocked")]      // an ordinal it rewrote
    [InlineData("Record", 1, 4, "None")]              // below L, not rewritten: proceeds beside it
    [InlineData("Record", 2, 8, "RecordLocked")]      // at or past L: its own append
    [InlineData("Open", 0, 0, "None")]                // a writable OPEN beside a base transaction
    [InlineData("Exclusive", 0, 0, "FileLocked")]     // OPEN OUTPUT / DELETE FILE: no other transaction at all
    public void The_lock_test_meets_a_base_transactions_holdings(string accessName, long ordinal, long offset, string expectedName)
    {
        var access = Enum.Parse<JournalAccess>(accessName);
        var expected = Enum.Parse<JournalConflict>(expectedName);
        var other = new JournalContent(0, JournalStart.Present, CommittedLength: 8, [new RewriteImage(0, 0, Bytes("C1__"))], null, JournalDecision.Open);
        Assert.Equal(expected, SequentialJournal.Test([new OtherTransaction(0, other)], access, ordinal, offset));
    }

    [Theory]
    [InlineData("Open", "FileLocked")]
    [InlineData("Record", "RecordLocked")]
    public void A_replaced_or_unreadable_transaction_holds_the_whole_file(string accessName, string expectedName)
    {
        var access = Enum.Parse<JournalAccess>(accessName);
        var expected = Enum.Parse<JournalConflict>(expectedName);
        var replaced = new JournalContent(0, JournalStart.Absent, null, [], null, JournalDecision.Open);
        Assert.Equal(expected, SequentialJournal.Test([new OtherTransaction(0, replaced)], access, 5, 20));
        Assert.Equal(expected, SequentialJournal.Test([new OtherTransaction(1, null)], access, 5, 20));
        Assert.Equal(JournalConflict.None, SequentialJournal.Test([], access, 5, 20));
    }

    [Fact]
    public void A_live_transaction_of_another_run_unit_is_met_and_a_dead_ones_is_rolled_back_by_the_next_entry()
    {
        // 2.2.2.8: held → its holdings are met; free → settled from its record before the entering statement proceeds.
        var owner = new FaultingHostMedium();
        SetUp(new Row("", "C1__", _ => { }, null), owner);
        var connector = new Connector(owner, _file);
        connector.Open();
        connector.Begin(JournalStart.Present);
        connector.Append("X1__");
        using (SequentialJournal.EntryMutex(_file))
        {
            var others = SequentialJournal.Enter(HostMedium.Instance, _file, [], () => new RestoreTarget(HostMedium.Instance, _file));
            Assert.Equal(JournalConflict.RecordLocked, SequentialJournal.Test(others, JournalAccess.Write));
            Assert.Equal(4L, Assert.Single(others).Content!.CommittedLength);
        }
        Assert.Equal("C1__X1__", Content());
        owner.Kill();   // the owner dies with its append uncommitted
        var seen = SequentialJournal.Observe(HostMedium.Instance, _file, [], out bool anyDead);
        Assert.True(anyDead);
        Assert.Empty(seen);
        using (SequentialJournal.EntryMutex(_file))
            Assert.Empty(SequentialJournal.Enter(HostMedium.Instance, _file, [], () => new RestoreTarget(HostMedium.Instance, _file)));
        Assert.Equal("C1__", Content());   // §9.1.18.5: rolled back
        Assert.Empty(Leftovers());
    }
}
