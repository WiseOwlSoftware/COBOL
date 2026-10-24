// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SEQUENTIAL FILE UNDER APPLY COMMIT THROUGH THE RUN UNIT'S REGISTRY (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2;
/// kb/Work PB3340): the rows of 2.2.2.12, each run as a program of the run unit's statements — OPEN, WRITE, READ, REWRITE, CLOSE,
/// DELETE FILE, COMMIT, ROLLBACK — over a record sequential and a line sequential file, fault-free and with every operation of
/// the fault-injecting <see cref="FaultHostMedium"/> REFUSED (the statement's '30', fatal: §14.6.13.1.3 item 8's ROLLBACK
/// follows and the program ends), made the point of a DEATH and of a POWER LOSS; then "the next entry" (another run unit's
/// writable OPEN) settles what is left and the file is read.
/// <para>Every expected state comes from the rules: §9.1.18.3 and §14.9.7.4 GR3 (a COMMIT <i>"permanently applies any changes
/// made to the files specified in all the active APPLY COMMIT clauses"</i>), §14.9.36.4 GR3 (a ROLLBACK restores them <i>"to
/// the state they were in at the execution of the last COMMIT statement or the start of the run unit"</i>), the NOTE under
/// §14.9.10.4 GR15 (a DELETE FILE of such a file is undone by a rollback), §9.1.18.5 (an abnormal termination rolls back) and
/// §14.9.27.4 GR17 and GR18 (a creation's file is the state "no file" at its start, so its rollback removes it: an OPEN INPUT
/// then answers '35', or '05' for an OPTIONAL file).</para>
/// </summary>
public sealed class SequentialCommitControlTests
{
    /// <summary>A statement answered '30': the fatal EC-I-O-PERMANENT-ERROR of a file under commit control.</summary>
    private sealed class Fatal() : Exception("a statement answered '30' (fatal: item 8's ROLLBACK follows)");

    /// <summary>One run unit's connector F on the physical file.</summary>
    private sealed class Unit
    {
        internal const string N = "F";
        internal readonly FileRegistry Reg;
        private readonly FaultHostMedium? _medium;

        /// <summary>Whether the row's deciding COMMIT answered success, and the operations of the medium that wrote its decision
        /// (handed to the operating environment) and synced it.</summary>
        internal bool Told;
        internal int Handed = int.MaxValue, Synced = int.MaxValue;

        internal Unit(string host, bool covered, IHostMedium? medium = null, bool optional = false, bool line = false,
            FileSharing? sharing = null)
        {
            _medium = medium as FaultHostMedium;
            Reg = medium is null ? new FileRegistry() : new FileRegistry { Medium = medium };
            Reg.Register(N, host, recordWidth: 2, lineSequential: line, optional: optional, varyMin: -1, varyMax: -1);
            if (sharing is { } s) Reg.RegisterSharing(N, s, LockModeRequiredBy(s), multiple: false);
            if (covered) Reg.RegisterCommitControl(N);
        }

        internal string Open(FileOpenMode mode)
        {
            Reg.OpenStatic(N, mode);
            return Reg.Status(N);
        }

        internal string Write(string record) => Reg.WriteShared(N, record, -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);

        internal string Read(out string image) =>
            Reg.ReadShared(N, previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false, FileRetryKind.None, 0, out image);

        internal string Rewrite(string record) => Reg.RewriteShared(N, record, -1, FileRecordLock.None, FileRetryKind.None, 0);

        internal string Close()
        {
            Reg.Close(N);
            return Reg.Status(N);
        }

        internal string DeleteFile() => Reg.DeleteFile(N);

        /// <summary>COMMIT: true when it answered success. The first that does is the row's deciding COMMIT: its decision is the
        /// first write of a record after it began (a COMMIT writes nothing else to a journal before its decision).</summary>
        internal bool Commit()
        {
            int before = _medium?.Operations ?? 0;
            bool ok = Reg.Commit() is null;
            if (ok && !Told)
            {
                Told = true;
                if (_medium is { } m)
                {
                    int at = m.Log.FindIndex(before, op => op.StartsWith("write txn-", StringComparison.Ordinal));
                    if (at >= 0) (Handed, Synced) = (at + 1, at + 2);
                }
            }
            return ok;
        }

        /// <summary>ROLLBACK: true when it completed.</summary>
        internal bool Rollback() => Reg.Rollback() is null;
    }

    /// <summary>A status the row requires: '30' is the fatal condition (<see cref="Fatal"/>); anything else must be
    /// <paramref name="expected"/>.</summary>
    private static void Ok(string status, string expected = "00")
    {
        if (status == "30") throw new Fatal();
        Assert.Equal(expected, status);
    }

    private static void OkCommit(Unit u)
    {
        if (!u.Commit()) throw new Fatal();
    }

    private static void OkRollback(Unit u)
    {
        if (!u.Rollback()) throw new Fatal();
    }

    private delegate void Program(Unit u);

    private static string NewDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "scc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Remove a row's directory; a handle a failed row left open must not hide the row's own failure.</summary>
    private static void TryDeleteDirectory(string dir)
    {
        try { Directory.Delete(dir, recursive: true); }
        catch (IOException) { }
    }

    private static byte[]? State(string f) => File.Exists(f) ? File.ReadAllBytes(f) : null;

    private static string Show(byte[]? state) => state is null ? "<absent>" : Encoding.Latin1.GetString(state);

    private static byte[]? Bytes(string? text, bool line) =>
        text is null ? null : Encoding.Latin1.GetBytes(line ? string.Concat(text.Chunk(2).Select(r => new string(r) + Environment.NewLine)) : text);

    private static void AssertNoLitter(string f)
    {
        string lockDirectory = SequentialJournal.DirectoryOf(f);
        string[] left = Directory.Exists(lockDirectory)
            ? [.. Directory.GetFileSystemEntries(lockDirectory).Select(Path.GetFileName).Where(n => n!.StartsWith("txn-", StringComparison.Ordinal))!]
            : [];
        Assert.True(left.Length == 0, $"I3: left behind: {string.Join(", ", left)}");
    }

    /// <summary>The next entry: another run unit's writable OPEN (an uncovered OPEN EXTEND of an OPTIONAL file), whose entry
    /// step settles a dead transaction, closed at once — or nothing, for a file that is not there and has no journal.</summary>
    private static void NextEntry(string f, bool line)
    {
        var next = new Unit(f, covered: false, optional: true, line: line);
        string status = next.Open(FileOpenMode.Extend);
        next.Close();
        if (status == "05") File.Delete(f);   // the OPTIONAL EXTEND created an absent file: the state stays "no file"
    }

    /// <summary>Run the row fault-free and under every fault at every operation of the medium, and assert I1 and I3.</summary>
    private static void Sweep(string? initial, string? before, string? after, bool line, Program program, bool optional = false)
    {
        int total, synced, handed;
        {
            string dir = NewDirectory();
            string f = Path.Combine(dir, "f.dat");
            try
            {
                if (Bytes(initial, line) is { } seed) File.WriteAllBytes(f, seed);
                var medium = new FaultHostMedium();
                var u = new Unit(f, covered: true, medium, optional, line);
                program(u);
                u.Reg.CloseAll();
                total = medium.Operations;
                Assert.True(u.Told || before == after, "the fault-free run is told success");
                Assert.Equal(Show(Bytes(after, line)), Show(State(f)));
                AssertNoLitter(f);
                (handed, synced) = before == after ? (int.MaxValue, int.MaxValue) : (u.Handed, u.Synced);
            }
            finally { TryDeleteDirectory(dir); }
        }
        foreach (var fault in new[] { FaultHostMedium.Fault.Refuse, FaultHostMedium.Fault.Die, FaultHostMedium.Fault.Power, FaultHostMedium.Fault.PowerKeepingData })
            for (int n = 1; n <= total; n++)
            {
                string dir = NewDirectory();
                string f = Path.Combine(dir, "f.dat");
                var medium = new FaultHostMedium(fault, n);
                try
                {
                    if (Bytes(initial, line) is { } seed) File.WriteAllBytes(f, seed);
                    var u = new Unit(f, covered: true, medium, optional, line);
                    bool told = false;
                    try
                    {
                        bool fatal = false;
                        try { program(u); }
                        catch (Fatal) { fatal = true; _ = u.Rollback(); }   // item 8's unconditional ROLLBACK; the program ends
                        told = u.Told;
                        u.Reg.EndTransactions(abnormal: fatal);
                        u.Reg.CloseAll();
                    }
                    catch (Exception) when (medium.Fired && fault != FaultHostMedium.Fault.Refuse) { told = u.Told; }   // the run unit died
                    catch (Exception e) when (e is not Xunit.Sdk.XunitException)
                    {
                        throw new InvalidOperationException($"{fault} at operation {n} of {total} ({medium.Log.ElementAtOrDefault(n - 1)}) escaped the program", e);
                    }
                    medium.Kill();
                    NextEntry(f, line);
                    var state = State(f);
                    string where = $"{fault} at operation {n} of {total} ({medium.Log.ElementAtOrDefault(n - 1)}): F = {Show(state)}";
                    byte[]? b = Bytes(before, line), a = Bytes(after, line);
                    if (told) Assert.True(Same(state, a), "I1, told success: " + where);
                    else if (fault == FaultHostMedium.Fault.Refuse) Assert.True(Same(state, b), "I1, refused and rolled back: " + where);
                    else if (n > (fault == FaultHostMedium.Fault.Die ? handed : synced)) Assert.True(Same(state, a), "I1, decided: " + where);
                    else Assert.True(Same(state, b), "I1, not decided: " + where);
                    AssertNoLitter(f);
                }
                finally
                {
                    medium.Kill();
                    try { Directory.Delete(dir, recursive: true); }
                    catch (IOException) { }   // a handle a failed row left open must not hide the row's own failure
                }
            }
    }

    private static bool Same(byte[]? a, byte[]? b) => a is null ? b is null : b is not null && a.AsSpan().SequenceEqual(b);

    public static TheoryData<bool> Framings() => [false, true];

    [Theory]
    [MemberData(nameof(Framings))]
    public void First_write_under_extend_then_commit(bool line) =>
        Sweep("C1C2", "C1C2", "C1C2X1", line, u =>
        {
            Ok(u.Open(FileOpenMode.Extend));
            Ok(u.Write("X1"));
            OkCommit(u);
        });

    [Theory]
    [MemberData(nameof(Framings))]
    public void Rewrite_then_rewrite_again_then_rollback(bool line) =>
        Sweep("C1C2", "C1C2", "C1C2", line, u =>
        {
            Ok(u.Open(FileOpenMode.IO));
            Ok(u.Read(out _));
            Ok(u.Rewrite("Y1"));
            Ok(u.Close());
            Ok(u.Open(FileOpenMode.IO));
            Ok(u.Read(out string again));
            Assert.Equal("Y1", again);   // D.2.5: the run unit reads its own uncommitted change
            Ok(u.Rewrite("Y2"));
            OkRollback(u);
        });

    [Theory]
    [MemberData(nameof(Framings))]
    public void Capture_then_rewrite_then_rollback(bool line) =>
        // 2.2.2.12 "R1-1 capture then REWRITE": nothing after the capture is journaled; F = C1 C2.
        Sweep("C1C2", "C1C2", "C1C2", line, u =>
        {
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X1"));
            Ok(u.Write("X2"));
            Ok(u.Close());
            Ok(u.Open(FileOpenMode.IO));
            Ok(u.Read(out _));
            Ok(u.Rewrite("Y1"));
            OkRollback(u);
        });

    [Theory]
    [MemberData(nameof(Framings))]
    public void Append_then_capture_then_rollback(bool line) =>
        // "R1-1 append then capture": the before file holds C1 C2 X1; truncated to L after the restore: C1 C2.
        Sweep("C1C2", "C1C2", "C1C2", line, u =>
        {
            Ok(u.Open(FileOpenMode.Extend));
            Ok(u.Write("X1"));
            Ok(u.Close());
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X2"));
            OkRollback(u);
        });

    [Theory]
    [MemberData(nameof(Framings))]
    public void A_creation_rolled_back_removes_the_file(bool line) =>
        // "R1-2 created by OPEN OUTPUT": removed; the OPEN INPUT answers '35' (GR18: the state at the start was "no file").
        Sweep(null, null, null, line, u =>
        {
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X1"));
            OkRollback(u);
            Ok(u.Close());
            Ok(u.Open(FileOpenMode.Input), "35");
        });

    [Fact]
    public void An_optional_file_created_by_open_extend_rolled_back_is_absent_again() =>
        // "R1-2 OPTIONAL created by OPEN EXTEND": the file removed; the OPEN INPUT answers '05' (GR17).
        Sweep(null, null, null, line: false, optional: true, program: u =>
        {
            Ok(u.Open(FileOpenMode.Extend), "05");
            Ok(u.Write("X1"));
            OkRollback(u);
            Ok(u.Close());
            Ok(u.Open(FileOpenMode.Input), "05");
        });

    [Fact]
    public void A_creation_committed_keeps_the_file() =>
        Sweep(null, null, "X1", line: false, program: u =>
        {
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X1"));
            OkCommit(u);
        });

    [Fact]
    public void Delete_file_then_commit_leaves_the_file_absent() =>
        // "DELETE FILE then COMMIT": absent, durably; the NOTE under §14.9.10.4 GR15 lets the delete proceed.
        Sweep("C1C2", "C1C2", null, line: false, program: u =>
        {
            Ok(u.Open(FileOpenMode.Input));
            Ok(u.Close());                     // the CLOSE keeps the handle (GR9) — and the DELETE FILE is not '62' for it
            Ok(u.DeleteFile());
            OkCommit(u);
        });

    [Fact]
    public void Delete_file_then_recreate_then_rollback_restores_the_file() =>
        // "DELETE FILE then re-create then ROLLBACK": the re-creation is replaced-phase and unjournaled; C1 C2.
        Sweep("C1C2", "C1C2", "C1C2", line: false, program: u =>
        {
            Ok(u.DeleteFile());
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X1"));
            Ok(u.Close());
            OkRollback(u);
        });

    [Fact]
    public void Commit_then_more_then_rollback() =>
        // "COMMIT then more, ROLLBACK": C1 X1 — the second transaction's L is the first's committed length.
        Sweep("C1", "C1", "C1X1", line: false, program: u =>
        {
            Ok(u.Open(FileOpenMode.Extend));
            Ok(u.Write("X1"));
            OkCommit(u);
            Ok(u.Write("X2"));
            OkRollback(u);
        });

    [Fact]
    public void A_writer_whose_file_a_rollback_removed_creates_it_again_at_its_next_write() =>
        // 2.2.2.7: the writer's open mode is the program's; the state at the last COMMIT was "no file".
        Sweep(null, null, "X2", line: false, program: u =>
        {
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X1"));
            OkRollback(u);
            Ok(u.Write("X2"));
            OkCommit(u);
        });

    // ── The connector's own state, the lock test and the run unit's end ────────────────────────────────────────

    [Fact]
    public void After_a_rollback_the_reader_keeps_its_position_and_rereads_the_restored_file()
    {
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2C3", Encoding.Latin1);
            var u = new Unit(f, covered: true);
            Ok(u.Open(FileOpenMode.IO));
            Ok(u.Read(out string first));
            Assert.Equal("C1", first);
            Ok(u.Rewrite("Y1"));
            OkRollback(u);
            Ok(u.Read(out string second));     // the file position indicator is unchanged by the ROLLBACK (2.2.2.7)
            Assert.Equal("C2", second);
            Ok(u.Rewrite("Y2"));               // the REWRITE after the ROLLBACK targets the record the last READ made available
            OkCommit(u);
            u.Reg.CloseAll();
            Assert.Equal("C1Y2C3", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { TryDeleteDirectory(dir); }
    }

    [Fact]
    public void The_close_keeps_the_file_lock_until_the_commit()
    {
        // 2.2.2.12 "CLOSE under commit, then COMMIT": the handle kept through the CLOSE — another run unit's NO OTHER OPEN '61'
        // until the COMMIT (§14.9.6.4 GR9, §14.9.7.4 GR4) — released by the COMMIT.
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            var a = new Unit(f, covered: true);
            Ok(a.Open(FileOpenMode.Extend));
            Ok(a.Write("X1"));
            Ok(a.Close());
            var b = new Unit(f, covered: false, sharing: FileSharing.NoOther);
            Assert.Equal("61", b.Open(FileOpenMode.Input));
            OkCommit(a);
            Assert.Equal("00", b.Open(FileOpenMode.Input));
            Ok(b.Read(out string r1));
            Ok(b.Read(out string r2));
            Assert.Equal(("C1", "X1"), (r1, r2));
            b.Reg.CloseAll();
            a.Reg.CloseAll();
            AssertNoLitter(f);
        }
        finally { TryDeleteDirectory(dir); }
    }

    [Fact]
    public void A_live_transaction_refuses_another_run_units_write_and_open_output_and_delete_file()
    {
        // 2.2.2.3 and 2.2.2.8: under SHARING WITH ALL OTHER another run unit's WRITE meets the END holding ('51'); an OPEN OUTPUT
        // needs no other transaction on the file ('61'), and so does a DELETE FILE (its sharing conflict, §9.1.13.9 2): '62').
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            var a = new Unit(f, covered: true, sharing: FileSharing.AllOther);
            Ok(a.Open(FileOpenMode.Extend));
            Ok(a.Write("X1"));
            var b = new Unit(f, covered: false, sharing: FileSharing.AllOther);
            Ok(b.Open(FileOpenMode.Extend));
            Assert.Equal("51", b.Write("B1"));
            Ok(b.Close());
            Assert.Equal("61", b.Open(FileOpenMode.Output));
            Assert.Equal("62", b.DeleteFile());
            OkRollback(a);
            Ok(b.Open(FileOpenMode.Extend));
            Ok(b.Write("B1"));                 // the transaction is over: nothing holds the end
            b.Reg.CloseAll();
            a.Reg.CloseAll();
            Assert.Equal("C1C2B1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { TryDeleteDirectory(dir); }
    }

    [Fact]
    public void A_read_not_ignoring_lock_meets_the_records_a_live_transaction_holds()
    {
        // 2.2.2.3: a transaction holds every record it appended (those at or past its L) — beside the records its READs lock
        // under the implicit AUTOMATIC WITH LOCK ON MULTIPLE RECORDS posture (§9.1.16), which are region locks; under SHARING
        // WITH ALL OTHER another run unit's READ of a held record answers '51' (§9.1.13.8 1)), and of any other record '00'.
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2C3", Encoding.Latin1);
            var a = new Unit(f, covered: true, sharing: FileSharing.AllOther);
            Ok(a.Open(FileOpenMode.Extend));
            Ok(a.Write("X4"));                 // a holds the end: every record at or past its L (6)
            var b = new Unit(f, covered: false, sharing: FileSharing.AllOther);
            Ok(b.Open(FileOpenMode.Input));
            foreach (string expected in new[] { "C1", "C2", "C3" })
            {
                Ok(b.Read(out string record));
                Assert.Equal(expected, record);
            }
            Assert.Equal("51", b.Read(out _)); // record 4: a's own append, held by its live transaction
            Assert.Equal("51", b.Read(out _)); // still held: the refused READ moved nothing (§14.9.30.4 GR10)
            OkRollback(a);
            Assert.Equal("10", b.Read(out _)); // the ROLLBACK removed it: at end (§14.9.30.4 GR21 e))
            Ok(a.Write("X5"));
            OkCommit(a);
            Ok(b.Close());                     // after the at end condition a READ is '46' (§9.1.13.7 6)): read it afresh
            Ok(b.Open(FileOpenMode.Input));
            string committed = "";
            for (int n = 0; n < 4; n++) Ok(b.Read(out committed));
            Assert.Equal("X5", committed);     // the COMMIT made it the file's, and released it
            b.Reg.CloseAll();
            a.Reg.CloseAll();
            Assert.Equal("C1C2C3X5", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { TryDeleteDirectory(dir); }
    }

    [Fact]
    public void A_killed_run_units_transaction_is_rolled_back_by_the_next_writable_open()
    {
        // §9.1.18.5 for the terminations the runtime cannot see (2.2.2.4: the journal is durable for exactly this).
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            var medium = new FaultHostMedium();
            var a = new Unit(f, covered: true, medium);
            Ok(a.Open(FileOpenMode.IO));
            Ok(a.Read(out _));
            Ok(a.Rewrite("Y1"));
            Ok(a.Close());
            Ok(a.Open(FileOpenMode.Extend));
            Ok(a.Write("X1"));
            medium.Kill();                     // the run unit is killed: its handles and its holder go, nothing is rolled back
            Assert.StartsWith("Y1C2", File.ReadAllText(f, Encoding.Latin1), StringComparison.Ordinal);   // its REWRITE reached the file
            var b = new Unit(f, covered: false);
            Ok(b.Open(FileOpenMode.Extend));   // its entry step settles the dead transaction first
            b.Reg.CloseAll();
            Assert.Equal("C1C2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { TryDeleteDirectory(dir); }
    }

    [Fact]
    public void Normal_termination_commits_and_abnormal_termination_rolls_back()
    {
        // §14.6.11 1): "An implicit COMMIT statement is executed for the files specified in all active APPLY COMMIT clauses";
        // §9.1.18.5: an abnormal termination rolls them back.
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            var a = new Unit(f, covered: true);
            Ok(a.Open(FileOpenMode.Extend));
            Ok(a.Write("X1"));
            a.Reg.CloseAll();
            Assert.Equal("C1X1", File.ReadAllText(f, Encoding.Latin1));
            var b = new Unit(f, covered: true);
            Ok(b.Open(FileOpenMode.Extend));
            Ok(b.Write("X2"));
            b.Reg.EndTransactions(abnormal: true);
            b.Reg.CloseAll();
            Assert.Equal("C1X1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { TryDeleteDirectory(dir); }
    }

    [Fact]
    public void A_file_not_under_commit_control_pays_one_listing_per_writable_open_and_nothing_per_write()
    {
        // 2.2.2.13, measured through the medium's operation log: a writable OPEN of an uncovered file adds one directory listing
        // and nothing else (the listing is not a counted operation: it changes nothing), and its WRITEs add nothing.
        string dir = NewDirectory();
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            var medium = new FaultHostMedium();
            var u = new Unit(f, covered: false, medium);
            Ok(u.Open(FileOpenMode.Extend));
            Ok(u.Write("X1"));
            Ok(u.Write("X2"));
            Ok(u.Close());
            Assert.DoesNotContain(medium.Log, op => op.Contains("txn-", StringComparison.Ordinal) || op.StartsWith("syncdir", StringComparison.Ordinal));
            Assert.False(Directory.Exists(SequentialJournal.DirectoryOf(f)));
        }
        finally { TryDeleteDirectory(dir); }
    }
}
