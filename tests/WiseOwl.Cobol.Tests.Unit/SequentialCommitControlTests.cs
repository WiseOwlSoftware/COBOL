// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using static WiseOwl.Cobol.Tests.Shared.FileHarness;

namespace WiseOwl.Cobol.Tests.Unit;

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
/// <para>It is a file-system race witness (<c>[Trait("Host", "ReFS")]</c>, kb/Work PB3022): its interleaving case drives two run
/// units' statements from two workers through the host's locks.</para>
/// </summary>
[Trait("Host", "ReFS")]
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

        internal string Write(string record, FileRetryKind retry = FileRetryKind.None, long amount = 0) =>
            Reg.WriteShared(N, record, -1, FileRecordLock.None, retry, amount, page: null);

        internal string Read(out string image, FileRetryKind retry = FileRetryKind.None, long amount = 0, bool ignoringLock = false) =>
            Reg.ReadShared(N, previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock, retry, amount, out image);

        internal string Rewrite(string record, FileRetryKind retry = FileRetryKind.None, long amount = 0) =>
            Reg.RewriteShared(N, record, -1, FileRecordLock.None, retry, amount);

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
            string dir = ScratchDirectory.Create("scc-");
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
            finally { ScratchDirectory.Remove(dir); }
        }
        foreach (var fault in new[] { FaultHostMedium.Fault.Refuse, FaultHostMedium.Fault.Die, FaultHostMedium.Fault.Power, FaultHostMedium.Fault.PowerKeepingData })
            for (int n = 1; n <= total; n++)
            {
                string dir = ScratchDirectory.Create("scc-");
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
                    ScratchDirectory.Remove(dir);   // never throws: a handle a failed row left open cannot hide the row's own failure
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

    /// <summary>The same under SHARING WITH ALL OTHER (kb/Work PB3492 review finding R1-2's sibling sweep): the Table 19 column the
    /// OPEN published — on Windows a handle on the file of its own — leaves the file with it at the ROLLBACK's removal and is
    /// published again when the WRITE creates the file, so another run unit's NO OTHER OPEN is refused ('61') again.</summary>
    [Fact]
    public void A_writer_under_all_other_whose_file_a_rollback_removed_creates_it_again_at_its_next_write()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            var a = new Unit(f, covered: true, sharing: FileSharing.AllOther);
            Ok(a.Open(FileOpenMode.Output));
            Ok(a.Write("X1"));
            OkRollback(a);
            Assert.False(File.Exists(f));   // §14.9.36.4 GR3: the state at the start of the run unit had no file
            Ok(a.Write("X2"));
            var b = new Unit(f, covered: false, sharing: FileSharing.NoOther);
            Assert.Equal("61", b.Open(FileOpenMode.Input));
            OkCommit(a);
            a.Reg.CloseAll();
            Assert.Equal("X2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ A FILE ANOTHER RUN UNIT MAKES AT THE PATH AFTER A ROLLBACK REMOVED THIS CONNECTOR'S IS NOT THIS WRITE'S TO USE
    /// (kb/Work PB3635; the keyed twin is <c>KeyedConnector.CreateTheFileAgain</c>). The ROLLBACK released the file lock of a file not
    /// open at the last COMMIT (§9.1.15) and reversed only this run unit's own changes (§14.9.36.4 GR3), so what another run unit
    /// then creates there is protected from this one (§9.1.17): the WRITE that would create the file again answers '30' (§9.1.13.6
    /// item 1), never appends to the other's file, and the later ROLLBACK or COMMIT leaves that file as its creator wrote it.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void A_write_creating_the_file_again_meets_a_file_another_run_unit_made_meanwhile_30(bool line, bool commit)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            var a = new Unit(f, covered: true, line: line);
            Ok(a.Open(FileOpenMode.Output));
            Ok(a.Write("A1"));
            OkRollback(a);
            Assert.False(File.Exists(f));   // §14.9.36.4 GR3: the state at the start of the run unit had no file
            var b = new Unit(f, covered: false, line: line);
            Ok(b.Open(FileOpenMode.Output));
            Ok(b.Write("B1"));
            Ok(b.Close());
            Assert.Equal("30", a.Write("A2"));   // §9.1.13.6 item 1: the file is another run unit's now
            if (commit) Assert.True(a.Commit()); else Assert.True(a.Rollback());
            a.Reg.CloseAll();
            Assert.Equal(Show(Bytes("B1", line)), Show(State(f)));
            AssertNoLitter(f);
            NextEntry(f, line);
            Assert.Equal(Show(Bytes("B1", line)), Show(State(f)));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The same window with the other run unit's file still OPEN: the refused WRITE withdraws the journal it began
    /// (2.2.2.14: an unsuccessful creation that did not affect the file leaves no transaction), so no <c>txn-</c> litter stands and
    /// a later run unit's OPEN EXTEND settles nothing of it and finds the other run unit's file.</summary>
    [Fact]
    public void A_write_creating_the_file_again_meets_a_file_another_run_unit_holds_open_30_and_leaves_no_journal()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            var a = new Unit(f, covered: true);
            Ok(a.Open(FileOpenMode.Output));
            Ok(a.Write("A1"));
            OkRollback(a);
            var b = new Unit(f, covered: false);
            Ok(b.Open(FileOpenMode.Output));
            Ok(b.Write("B1"));
            Assert.Equal("30", a.Write("A2"));
            AssertNoLitter(f);
            Ok(b.Close());
            var c = new Unit(f, covered: false);
            Ok(c.Open(FileOpenMode.Extend));
            Ok(c.Close());
            Assert.Equal("B1", File.ReadAllText(f, Encoding.Latin1));
            Assert.True(a.Rollback());
            a.Reg.CloseAll();
            Assert.Equal("B1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A medium that makes <paramref name="f"/> with <paramref name="content"/> once, just before the first
    /// <paramref name="operation"/> (a <c>kind name</c> log entry) after <paramref name="armed"/> answers true: another run unit's
    /// OPEN OUTPUT (which takes no entry mutex) landing between this run unit's presence probe and the handle that creates the file.</summary>
    private static FaultHostMedium MakingTheFile(string f, string content, string operation, Func<bool>? armed = null)
    {
        bool made = false;
        return new()
        {
            BeforeOperation = op =>
            {
                if (made || op != operation || armed?.Invoke() == false) return;
                made = true;
                File.WriteAllText(f, content, Encoding.Latin1);
            },
        };
    }

    /// <summary>⛔ THE SEQUENTIAL WRITE'S CREATING HANDLE REFUSES A FILE ANOTHER RUN UNIT MADE AFTER THE PRESENCE PROBE (kb/Work PB3635,
    /// review finding R1-2 of the first round): the probe under the entry mutex found no file, and the other run unit's OPEN OUTPUT
    /// (which takes none) made one before this WRITE's handle opened. The handle is opened <see cref="FileMode.CreateNew"/>, so the
    /// WRITE answers '30' (§9.1.13.6 item 1), the other run unit's file is as it wrote it (§9.1.17), and the journal the WRITE began
    /// is withdrawn (2.2.2.14), with no <c>txn-</c> file left.</summary>
    [Fact]
    public void A_write_creating_the_file_again_meets_a_file_made_between_the_probe_and_the_handle_30()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            bool armed = false;
            var a = new Unit(f, covered: true, MakingTheFile(f, "B1", "open f.dat", () => armed));
            Ok(a.Open(FileOpenMode.Output));
            Ok(a.Write("A1"));
            OkRollback(a);
            Assert.False(File.Exists(f));
            armed = true;
            Assert.Equal("30", a.Write("A2"));
            Assert.Equal("B1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
            Assert.True(a.Rollback());
            a.Reg.CloseAll();
            Assert.Equal("B1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ AN OPEN THAT CREATES THE FILE TAKES A HANDLE THAT REFUSES A FILE MADE SINCE ITS PRESENCE PROBE, AND IS MADE AGAIN OVER
    /// THE FILE THAT STANDS (kb/Work PB3635, review finding R1-1 of the first round; <c>FileConnector.Open</c>). Another run unit's OPEN
    /// OUTPUT takes no entry mutex, so a file can appear between the probe and the handle. The OPEN of the absent OPTIONAL file
    /// (§14.9.27.4 GR17) then meets an existing one: it is the OPEN of an existing file — '00', not '05' — and neither truncates the
    /// other run unit's records (I-O) nor adopts them as its own creation (EXTEND), which a covered run unit's ROLLBACK would remove
    /// with the file (§14.9.36.4 GR3 restores only this run unit's own changes). Both ends of the order are checked: the records
    /// before the ROLLBACK or COMMIT, and the file after.</summary>
    [Theory]
    [InlineData(FileOpenMode.IO, false, false)]
    [InlineData(FileOpenMode.IO, true, false)]
    [InlineData(FileOpenMode.IO, true, true)]
    [InlineData(FileOpenMode.Extend, false, false)]
    [InlineData(FileOpenMode.Extend, true, false)]
    [InlineData(FileOpenMode.Extend, true, true)]
    public void An_open_creating_the_optional_file_meets_a_file_made_meanwhile_and_opens_it(FileOpenMode mode, bool covered, bool commit)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            // The first writable handle on the file: the covered OPEN's kept handle or the uncovered creating handle.
            var medium = MakingTheFile(f, "B1", covered || mode is FileOpenMode.Extend ? "open f.dat" : "aux f.dat");
            var a = new Unit(f, covered, medium, optional: true);
            Ok(a.Open(mode));   // '00': the file that stands is opened, not created ('05' is the OPEN of an absent OPTIONAL file)
            Assert.True(medium.Log.Count > 0);
            if (mode is FileOpenMode.IO)
            {
                Ok(a.Read(out string image));
                Assert.Equal("B1", image);
            }
            else
            {
                Ok(a.Write("A1"));
            }
            if (covered) { if (commit) OkCommit(a); else OkRollback(a); }
            Ok(a.Close());
            a.Reg.CloseAll();
            string expected = mode is FileOpenMode.IO || covered && !commit ? "B1" : "B1A1";
            Assert.Equal(expected, File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The same window for an OPEN OUTPUT under commit control: the OPEN is made again over the file that stands, so it
    /// REPLACES it (§14.9.27.4 GR18) after the capture of its records, and the run unit's ROLLBACK restores them
    /// (§14.9.36.4 GR3: the state at the last COMMIT, which includes the file the other run unit made before this OPEN
    /// replaced it) rather than removing the file as a creation of its own; its COMMIT keeps the replacement.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_covered_open_output_meets_a_file_made_between_the_probe_and_the_handle(bool commit)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            var medium = MakingTheFile(f, "B1", "open f.dat");
            var a = new Unit(f, covered: true, medium);
            Ok(a.Open(FileOpenMode.Output));
            Ok(a.Write("A1"));
            Ok(a.Close());
            if (commit) OkCommit(a); else OkRollback(a);
            a.Reg.CloseAll();
            Assert.Equal(commit ? "A1" : "B1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A path that is made again after every attempt's probe answers the last attempt's '30' (§9.1.13.6 item 1): at most
    /// <c>FileConnector.MaxFileMadeRetries</c> retries (three attempts), the journals each withdrew leaving no <c>txn-</c> file, and the other run unit's
    /// file untouched.</summary>
    [Fact]
    public void An_open_whose_creation_is_met_by_a_new_file_at_every_attempt_answers_30_and_leaves_nothing()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            int attempts = 0;
            var medium = new FaultHostMedium
            {
                // Each attempt's entry step finds the path empty again (the other run unit removed its file), so its probe answers
                // Absent, and the other run unit makes the file again before the creating handle opens.
                OnEntryMutex = () => File.Delete(f),
                BeforeOperation = op =>
                {
                    if (op != "open f.dat") return;
                    attempts++;
                    File.WriteAllText(f, "B1", Encoding.Latin1);
                },
            };
            var a = new Unit(f, covered: true, medium, optional: true);
            Assert.Equal("30", a.Open(FileOpenMode.Extend));
            Assert.Equal(3, attempts);
            Assert.Equal("B1", File.ReadAllText(f, Encoding.Latin1));
            a.Reg.CloseAll();
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ A DIRECTORY AT THE PATH IS NOT A FILE MADE MEANWHILE (kb/Work PB3635, review finding R2-1): the presence probe answers
    /// Absent for a directory, the creating handle (<see cref="FileMode.CreateNew"/>) is refused with an access error on Windows and
    /// with <c>EEXIST</c> on Unix, and both hosts answer §14.9.27.4 GR3's '37' on the one attempt, covered or not, leaving no journal.</summary>
    [Theory]
    [InlineData(FileOpenMode.Output, false)]
    [InlineData(FileOpenMode.Output, true)]
    [InlineData(FileOpenMode.IO, false)]
    [InlineData(FileOpenMode.IO, true)]
    [InlineData(FileOpenMode.Extend, false)]
    [InlineData(FileOpenMode.Extend, true)]
    public void An_open_creating_the_file_meets_a_directory_at_the_path_37_at_once(FileOpenMode mode, bool covered)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        Directory.CreateDirectory(f);
        try
        {
            int attempts = 0;
            var medium = new FaultHostMedium { BeforeOperation = op => { if (op == "open f.dat") attempts++; } };
            var a = new Unit(f, covered, medium, optional: true);
            Assert.Equal("37", a.Open(mode));
            Assert.True(attempts <= 1, $"the OPEN was made again: {attempts} creating handles");
            a.Reg.CloseAll();
            Assert.True(Directory.Exists(f));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    // ── The connector's own state, the lock test and the run unit's end ────────────────────────────────────────

    [Fact]
    public void After_a_rollback_the_reader_keeps_its_position_and_rereads_the_restored_file()
    {
        string dir = ScratchDirectory.Create("scc-");
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
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void The_close_keeps_the_file_lock_until_the_commit()
    {
        // 2.2.2.12 "CLOSE under commit, then COMMIT": the handle kept through the CLOSE — another run unit's NO OTHER OPEN '61'
        // until the COMMIT (§14.9.6.4 GR9, §14.9.7.4 GR4) — released by the COMMIT.
        string dir = ScratchDirectory.Create("scc-");
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
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ AN UNSUCCESSFUL DELETE FILE AFTER A CLOSE UNDER COMMIT CONTROL TAKES BACK WHAT IT GAVE BACK (kb/Work PB3492, review
    /// finding R1-1, the sequential arm; MANDATORY-PRACTICES I11): the kept handle goes for the host's probe and the Table 19 file
    /// lock for the removal, and a DELETE FILE refused at the capture's write of the before file ('30', §9.1.13.6) or at the
    /// removal takes both back, so another run unit's NO OTHER OPEN is still '61' (§14.9.6.4 GR9, §14.9.7.4 GR4: the file lock
    /// stands until the COMMIT), and the COMMIT keeps the record the transaction wrote (§14.9.7.4 GR3).</summary>
    [Theory]
    [InlineData("capture")]
    [InlineData("delete")]
    public void A_refused_delete_file_after_a_close_keeps_the_file_lock_until_the_commit(string refused)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            int target;
            {
                File.WriteAllText(f, "C1", Encoding.Latin1);
                var medium = new FaultHostMedium();
                var dry = new Unit(f, covered: true, medium);
                Ok(dry.Open(FileOpenMode.Extend));
                Ok(dry.Write("X1"));
                Ok(dry.Close());
                int from = medium.Operations;
                Ok(dry.DeleteFile());
                int at = medium.Log.FindIndex(from, op => refused == "capture"
                    ? op.StartsWith("write ", StringComparison.Ordinal) && op.EndsWith(".before", StringComparison.Ordinal)
                    : op == "delete f.dat");
                Assert.True(at >= 0, $"the DELETE FILE reaches no '{refused}': {string.Join(", ", medium.Log.Skip(from))}");
                target = at + 1;
                OkRollback(dry);
                dry.Reg.CloseAll();
                medium.Kill();
            }
            File.WriteAllText(f, "C1", Encoding.Latin1);
            var faulty = new FaultHostMedium(FaultHostMedium.Fault.Refuse, target);
            try
            {
                var a = new Unit(f, covered: true, faulty);
                Ok(a.Open(FileOpenMode.Extend));
                Ok(a.Write("X1"));
                Ok(a.Close());
                Assert.Equal("30", a.DeleteFile());
                Assert.True(faulty.Fired, "the refusal fired inside the DELETE FILE");
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
            finally { faulty.Kill(); }
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_live_transaction_refuses_another_run_units_write_and_open_output_and_delete_file()
    {
        // 2.2.2.3 and 2.2.2.8: under SHARING WITH ALL OTHER another run unit's WRITE meets the END holding ('51'); an OPEN OUTPUT
        // needs no other transaction on the file ('61'), and so does a DELETE FILE (its sharing conflict, §9.1.13.9 2): '62').
        string dir = ScratchDirectory.Create("scc-");
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
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_read_not_ignoring_lock_meets_the_records_a_live_transaction_holds()
    {
        // 2.2.2.3: a transaction holds every record it appended (those at or past its L) — beside the records its READs lock
        // under the implicit AUTOMATIC WITH LOCK ON MULTIPLE RECORDS posture (§9.1.16), which are region locks; under SHARING
        // WITH ALL OTHER another run unit's READ of a held record answers '51' (§9.1.13.8 1)), and of any other record '00'.
        string dir = ScratchDirectory.Create("scc-");
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
        finally { ScratchDirectory.Remove(dir); }
    }

    public static TheoryData<string> JournalArms() => ["WRITE", "READ"];

    /// <summary>Another run unit's live transaction <paramref name="a"/> holds the end of the file (its append), and
    /// <paramref name="b"/> stands where its <paramref name="statement"/> meets that holding — a WRITE at the end, a READ of the
    /// appended record — only in the journal, no region lock covering it (2.2.2.3). Returns b's statement under a RETRY phrase.</summary>
    private static Func<FileRetryKind, long, string> MeetJournalHolding(string statement, Unit a, Unit b)
    {
        Ok(a.Open(FileOpenMode.Extend));
        Ok(a.Write("X3"));
        if (statement == "WRITE")
        {
            Ok(b.Open(FileOpenMode.Extend));
            return (kind, amount) => b.Write("B3", kind, amount);
        }
        Ok(b.Open(FileOpenMode.Input));
        Ok(b.Read(out _));
        Ok(b.Read(out _));
        return (kind, amount) => b.Read(out _, kind, amount);
    }

    [Theory]
    [MemberData(nameof(JournalArms))]
    public void A_statement_meeting_another_run_units_journal_holding_retries_under_its_retry_phrase(string statement)
    {
        // §14.7.9.3 GR4 b): a statement unsuccessful at its first attempt "because of ... a record operation conflict condition"
        // ('51', §9.1.13.8 1)) attempts again as GR1 and GR3 specify, and "If the mass storage control system permits the
        // requested access on one of these attempts, the statement is successful"; otherwise the conflict's own status (the
        // closing paragraph). The journal arm of the lock test is such a conflict (2.2.2.3: "held, '51' (RETRY applies)").
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        int pauses = 0;
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            var a = new Unit(f, covered: true, sharing: FileSharing.AllOther);
            var b = new Unit(f, covered: false, sharing: FileSharing.AllOther);
            var attempt = MeetJournalHolding(statement, a, b);
            FileRegistry.PauseObserver.Value = _ => pauses++;
            Assert.Equal("51", attempt(FileRetryKind.None, 0));               // GR4 a): no RETRY phrase, no wait
            Assert.Equal(0, pauses);
            Assert.Equal("51", attempt(FileRetryKind.Times, 2));              // GR1: two attempts after the failure, then '51'
            Assert.Equal(2, pauses);
            pauses = 0;
            FileRegistry.PauseObserver.Value = _ => { if (++pauses == 3) OkCommit(a); };
            Assert.Equal("00", attempt(FileRetryKind.Forever, 0));            // GR3: until the holder's COMMIT releases it
            Assert.Equal(3, pauses);
            FileRegistry.PauseObserver.Value = null;
            b.Reg.CloseAll();
            a.Reg.CloseAll();
            Assert.Equal(statement == "WRITE" ? "C1C2X3B3" : "C1C2X3", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally
        {
            FileRegistry.PauseObserver.Value = null;
            ScratchDirectory.Remove(dir);
        }
    }

    [Fact]
    public void A_holding_of_this_run_units_own_transaction_is_not_waited_for()
    {
        // §14.7.9.3 GR1 to GR3 wait for a holder that can release: this run unit's own transaction, met through a connector no
        // APPLY COMMIT names (2.2.2.3: "this run unit's through an uncovered connector"), cannot release while the statement
        // runs, so RETRY n TIMES ends at once with '51' and RETRY FOREVER with the deadlock '52' (§9.1.13.8 2), A.1 item 109).
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        int pauses = 0;
        var reg = new FileRegistry();
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            foreach (string name in new[] { "F", "G" })
            {
                reg.Register(name, f, recordWidth: 2, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
                reg.RegisterSharing(name, FileSharing.AllOther, LockModeRequiredBy(FileSharing.AllOther), multiple: false);
            }
            reg.RegisterCommitControl("F");
            reg.OpenStatic("F", FileOpenMode.Extend);
            Ok(reg.WriteShared("F", "X3", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.OpenStatic("G", FileOpenMode.Extend);
            Ok(reg.Status("G"));
            FileRegistry.PauseObserver.Value = _ => pauses++;
            Assert.Equal("51", reg.WriteShared("G", "G3", -1, FileRecordLock.None, FileRetryKind.Times, 3, page: null));
            Assert.Equal("52", reg.WriteShared("G", "G3", -1, FileRecordLock.None, FileRetryKind.Forever, 0, page: null));
            Assert.Equal(0, pauses);
            Assert.Null(reg.Commit());
            Ok(reg.WriteShared("G", "G3", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.CloseAll();
            Assert.Equal("C1C2X3G3", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally
        {
            FileRegistry.PauseObserver.Value = null;
            reg.CloseAll();
            ScratchDirectory.Remove(dir);
        }
    }

    public static TheoryData<FileSharing, bool> KeptHolds() => new()
    {
        { FileSharing.NoOther, false },   // the kept handle's file lock: Table 19's NO OTHER column refuses every OPEN
        { FileSharing.AllOther, true },   // a replaced transaction (an OPEN OUTPUT of the file): its journal refuses a writable OPEN
    };

    [Theory]
    [MemberData(nameof(KeptHolds))]
    public void An_open_or_delete_file_refused_by_what_this_run_unit_holds_is_not_waited_for(FileSharing kept, bool replaced)
    {
        // §14.7.9.3 GR3 waits for a holder that can release (kb/Work PB1163); what a CLOSE under commit control kept stands until
        // the COMMIT or ROLLBACK (§14.9.7.4 GR4), and the run unit's own transaction until its end, so neither releases while
        // another connector's statement of the same run unit waits: RETRY FOREVER on the OPEN lands '61' at once (§9.1.13.9 1)),
        // on the DELETE FILE '62' (§9.1.13.9 2)) — DOC-A.1-109's file-conflict rule — and only the COMMIT lets them succeed.
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        int pauses = 0;
        var reg = new FileRegistry();
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            reg.Register("F", f, recordWidth: 2, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
            reg.RegisterSharing("F", kept, LockModeRequiredBy(kept), multiple: false);
            reg.RegisterCommitControl("F");
            reg.Register("H", f, recordWidth: 2, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
            reg.RegisterSharing("H", FileSharing.AllOther, LockModeRequiredBy(FileSharing.AllOther), multiple: false);
            reg.OpenStatic("F", replaced ? FileOpenMode.Output : FileOpenMode.Extend);
            Ok(reg.Status("F"));
            Ok(reg.WriteShared("F", "X2", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("F");
            Ok(reg.Status("F"));
            FileRegistry.PauseObserver.Value = _ =>
            {
                if (++pauses > 3) throw new InvalidOperationException("the RETRY waits for a holder of its own run unit");
            };
            reg.OpenShared("H", FileOpenMode.Extend, hasSharingOverride: false, FileSharing.AllOther, FileRetryKind.Forever, 0,
                OpenTapePhrase.None, f, assignDynamic: false, page: null);
            Assert.Equal("61", reg.Status("H"));
            Assert.Equal("62", reg.DeleteFile("H", FileRetryKind.Forever, 0));
            Assert.Equal(0, pauses);
            Assert.Null(reg.Commit());
            reg.OpenShared("H", FileOpenMode.Extend, hasSharingOverride: false, FileSharing.AllOther, FileRetryKind.Forever, 0,
                OpenTapePhrase.None, f, assignDynamic: false, page: null);
            Ok(reg.Status("H"));
            reg.CloseAll();
            Assert.Equal(replaced ? "X2" : "C1X2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally
        {
            FileRegistry.PauseObserver.Value = null;
            reg.CloseAll();
            ScratchDirectory.Remove(dir);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_lock_directory_this_process_may_not_list_is_the_authority_an_open_or_delete_file_lacks(bool covered)
    {
        // §9.1.13.6 6) b): "insufficient authority exists to access the file" ('37'; §14.9.10.4 GR16 for the DELETE FILE). The
        // entry step cannot tell whether a transaction lives on the file, and no holder is known to conflict, so the answer is
        // the missing authority, never a '61' or '62' sharing conflict; the file is unaffected (§14.9.27.4 GR25).
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            var u = new Unit(f, covered, new FaultHostMedium { RefusesListing = true });
            Assert.Equal("37", u.Open(FileOpenMode.Extend));
            Assert.Equal("37", u.Open(FileOpenMode.Output));
            if (covered) Assert.Equal("37", u.DeleteFile());   // the covered DELETE FILE's entry step (the uncovered one runs none)
            Ok(u.Open(FileOpenMode.Input));                     // an OPEN INPUT runs no entry step
            Ok(u.Read(out string record));
            Assert.Equal("C1", record);
            u.Reg.CloseAll();
            Assert.Equal("C1", File.ReadAllText(f, Encoding.Latin1));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_killed_run_units_transaction_is_rolled_back_by_the_next_writable_open()
    {
        // §9.1.18.5 for the terminations the runtime cannot see (2.2.2.4: the journal is durable for exactly this).
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            var medium = new FaultHostMedium();
            using (medium.OwnColumnLocks())
            {
                var a = new Unit(f, covered: true, medium);
                Ok(a.Open(FileOpenMode.IO));
                Ok(a.Read(out _));
                Ok(a.Rewrite("Y1"));
                Ok(a.Close());
                Ok(a.Open(FileOpenMode.Extend));
                Ok(a.Write("X1"));
            }
            medium.Kill();                     // the run unit is killed: its handles, holder and file lock go, nothing is rolled back
            Assert.StartsWith("Y1C2", File.ReadAllText(f, Encoding.Latin1), StringComparison.Ordinal);   // its REWRITE reached the file
            var b = new Unit(f, covered: false);
            Ok(b.Open(FileOpenMode.Extend));   // its entry step settles the dead transaction first
            b.Reg.CloseAll();
            Assert.Equal("C1C2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void Normal_termination_commits_and_abnormal_termination_rolls_back()
    {
        // §14.6.11 1): "An implicit COMMIT statement is executed for the files specified in all active APPLY COMMIT clauses";
        // §9.1.18.5: an abnormal termination rolls them back.
        string dir = ScratchDirectory.Create("scc-");
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
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>F's bytes as they stand while connectors hold it open for writing (a reader that shares with them).</summary>
    private static byte[] ReadBesideTheWriters(string f)
    {
        using var read = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var copy = new MemoryStream();
        read.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>A medium that REFUSES every <paramref name="operation"/> (a <c>kind name</c> log entry) while <paramref name="armed"/>
    /// answers true: the refusal of 2.2.2.12, kept up across a statement's retry.</summary>
    private static FaultHostMedium Refusing(string operation, Func<bool> armed, Action? onEntryMutex = null) => new()
    {
        BeforeOperation = op =>
        {
            if (op == operation && armed()) throw new IOException($"the test medium refused {op}");
        },
        OnEntryMutex = onEntryMutex,
    };

    /// <summary>⛔ A TRANSACTION WHOSE ROLLBACK THE MEDIUM REFUSED IS NEVER COMMITTED (review finding R1-3 of kb/Work PB3340;
    /// §14.9.36.4 GR3, <i>"restored to the state they were in at the execution of the last COMMIT statement"</i>; design 2.2.2.7,
    /// the keyed rule of 2.2.1.10 (5) in its one place, <c>FileRegistry.CommitHolder</c>): C1C2 replaced by an OPEN OUTPUT (the
    /// capture), X1 written, CLOSE, and a ROLLBACK whose restore the medium refuses at the copy of the before file into F, which
    /// its truncation has already emptied ('30'; the program does not test it). The run unit's normal termination (its implicit
    /// COMMIT, §14.6.11 1)) or a program COMMIT is refused while the rollback is owed — the COMMIT '30', the file's name returned —
    /// and the ROLLBACK that follows restores C1C2; committed, F stayed empty and the before file was retired with it.</summary>
    [Theory]
    [InlineData("termination")]
    [InlineData("commit")]
    public void A_transaction_whose_rollback_the_medium_refused_is_never_committed(string next)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            bool refuse = false;
            var medium = Refusing("write f.dat", () => refuse);
            var u = new Unit(f, covered: true, medium);
            Ok(u.Open(FileOpenMode.Output));
            Ok(u.Write("X1"));
            Ok(u.Close());
            refuse = true;
            Assert.Equal(Unit.N, u.Reg.Rollback());
            Assert.Equal("30", u.Reg.Status(Unit.N));
            refuse = false;
            Assert.Equal("", File.ReadAllText(f, Encoding.Latin1));   // the restore stopped after its truncation: the rollback is owed
            if (next == "commit")
            {
                Assert.Equal(Unit.N, u.Reg.Commit());   // refused before it touches the medium
                Assert.Equal("30", u.Reg.Status(Unit.N));
                Assert.Equal("", File.ReadAllText(f, Encoding.Latin1));
                Assert.Null(u.Reg.Rollback());          // the owed rollback, run again, restores
                Assert.Equal("C1C2", File.ReadAllText(f, Encoding.Latin1));
            }
            u.Reg.CloseAll();   // the implicit COMMIT of a normal termination: refused while owed, and the ROLLBACK it falls back on restores
            Assert.Equal("C1C2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ A REFUSED ROLLBACK CLOSES THE HANDLE IT OPENED FOR THE PURPOSE (review finding R1-5 of kb/Work PB3340; design
    /// 2.2.2.7): C1C2 extended by X1 (START P, L = 4) and CLOSEd; a ROLLBACK refused at the truncation to L leaves the transaction
    /// owed, and the clause then deactivated (kb/Work PB3541's owed case) leaves no covered connector on the file, so the next
    /// ROLLBACK reaches F through a handle of the medium opened for it — refused at the truncation again, it closes that handle
    /// (a Windows handle opened without delete sharing, left to the finalizer, refused the owed rollback's own START A removal
    /// and another run unit's exclusive OPEN); the third ROLLBACK restores C1C2.</summary>
    [Fact]
    public void A_refused_rollback_closes_the_handle_it_opened_for_the_purpose()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            bool refuse = false;
            var medium = Refusing("setlength f.dat", () => refuse);
            var u = new Unit(f, covered: true, medium);
            Ok(u.Open(FileOpenMode.Extend));
            Ok(u.Write("X1"));
            Ok(u.Close());
            refuse = true;
            Assert.NotNull(u.Reg.Rollback());
            u.Reg.ReleaseCommitControl(Unit.N);
            Assert.NotNull(u.Reg.Rollback());
            Assert.Contains("aux f.dat", medium.Log);   // the second rollback reached F through a handle opened for it
            Assert.Equal(0, medium.LiveAuxiliaryHandles);
            refuse = false;
            Assert.Null(u.Reg.Rollback());
            Assert.Equal(0, medium.LiveAuxiliaryHandles);
            Assert.Equal("C1C2", File.ReadAllText(f, Encoding.Latin1));
            u.Reg.CloseAll();
            Assert.Equal("C1C2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ A DEAD TRANSACTION WHOSE FILE IS SHORTER THAN ITS COMMITTED LENGTH IS SETTLED (review finding R1-4 of kb/Work
    /// PB3340; design 2.2.2.7, §9.1.18.5 for the terminations the runtime cannot see): L counts what the operating environment
    /// was handed, another connector's released and never synced appends included, and a power loss takes those; the external
    /// truncation below stands for it (F loses the bytes past C1, the killed run unit's X1 with them). None of the transaction's
    /// appends survived, so the next writable OPEN settles it leaving F as it stands — the settle used to refuse a truncation
    /// that would extend F, and every later writable OPEN answered '30' until an administrator removed the lock directory.</summary>
    [Fact]
    public void A_dead_transaction_whose_file_is_shorter_than_its_committed_length_is_settled()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1C2", Encoding.Latin1);
            var medium = new FaultHostMedium();
            using (medium.OwnColumnLocks())
            {
                var a = new Unit(f, covered: true, medium);
                Ok(a.Open(FileOpenMode.Extend));
                Ok(a.Write("X1"));             // L = 4
            }
            medium.Kill();
            using (var cut = new FileStream(f, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) cut.SetLength(2);
            var b = new Unit(f, covered: false);
            Ok(b.Open(FileOpenMode.Extend));   // its entry step settles the dead transaction first
            Ok(b.Write("B1"));
            b.Reg.CloseAll();
            Assert.Equal("C1B1", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ AN UNCOVERED WRITER'S RECORD IS NEVER CUT BY A TRANSACTION THAT BEGINS DURING ITS WRITE (review finding R1-1 of
    /// kb/Work PB3340; design 2.2.2.3, <c>SequentialConnector.UncoveredChangeGuard</c>; §14.9.51.4 GR19, <i>"the added records
    /// follow the records present in the physical file when it was opened"</i>, and GR12's release): run unit B, no APPLY COMMIT,
    /// and run unit A, APPLY COMMIT ON F, both OPEN EXTEND under SHARING WITH ALL OTHER. A's first WRITE is run at the instant
    /// B's WRITE releases its record to the medium (the fault-injecting medium's <c>BeforeOperation</c> seam), and B waits until
    /// A's WRITE has either finished or asked for the entry mutex while B holds it. B's lock test and release are one step under
    /// the mutex, so A's transaction begins after B's record is in F and its L counts it: B's next WRITE meets A's END holding
    /// ('51'), and A's ROLLBACK leaves B1. Tested outside the mutex, A began, read L = 2 and appended before B's bytes landed past
    /// it, and A's ROLLBACK truncated B's record away.</summary>
    [Fact]
    public async Task An_uncovered_writers_record_is_never_cut_by_a_transaction_that_begins_during_its_write()
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            File.WriteAllText(f, "C1", Encoding.Latin1);
            int aAsked = 0, bAsked = 0;
            var a = new Unit(f, covered: true, new FaultHostMedium { OnEntryMutex = () => Interlocked.Exchange(ref aAsked, 1) },
                sharing: FileSharing.AllOther);
            Ok(a.Open(FileOpenMode.Extend));
            Task<string>? aWrite = null;
            var bMedium = new FaultHostMedium
            {
                OnEntryMutex = () => Interlocked.Exchange(ref bAsked, 1),
                BeforeOperation = op =>
                {
                    if (op != "write f.dat" || aWrite is not null) return;
                    aWrite = Task.Run(() => a.Write("X1"));
                    SpinWait.SpinUntil(() => aWrite.IsCompleted || (Volatile.Read(ref aAsked) == 1 && Volatile.Read(ref bAsked) == 1));
                },
            };
            var b = new Unit(f, covered: false, bMedium, sharing: FileSharing.AllOther);
            Ok(b.Open(FileOpenMode.Extend));
            Interlocked.Exchange(ref bAsked, 0);   // only the WRITE's own hold counts
            Ok(b.Write("B1"));
            Assert.NotNull(aWrite);
            Ok(await aWrite!);   // A's first change waited for B's release: its L counts B1
            Assert.Equal("51", b.Write("B2"));       // A's transaction holds the end of the file
            OkRollback(a);
            Assert.Equal("C1B1", Encoding.Latin1.GetString(ReadBesideTheWriters(f)));
            Ok(b.Write("B2"));
            b.Reg.CloseAll();
            a.Reg.CloseAll();
            Assert.Equal("C1B1B2", File.ReadAllText(f, Encoding.Latin1));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_file_not_under_commit_control_pays_one_listing_per_writable_open_and_nothing_per_write()
    {
        // 2.2.2.13, measured through the medium's operation log: a writable OPEN of an uncovered file adds one directory listing
        // and nothing else (the listing is not a counted operation: it changes nothing), and its WRITEs add nothing.
        string dir = ScratchDirectory.Create("scc-");
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
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ A TRANSACTION'S HOLDER THAT CANNOT BE PUBLISHED REFUSES THE COVERED STATEMENT '30' FOR EITHER CAUSE, WITH THE FILE
    /// UNAFFECTED (kb/Work PB3675; design 2.2.2.5 R1; §9.1.13.6 item 1 and, for an OPEN, §14.9.27.4 GR25's <i>"the file is not
    /// affected"</i>): the publication answers <c>Unavailable</c> (the host takes no lock here) or <c>Held</c> (every try lost its
    /// race), the journal's <c>Begin</c> refuses before the first byte of F changes or F is created, and nothing of the journal is
    /// left behind. The statements are the three that begin a transaction on a sequential file: OPEN OUTPUT of an existing file (a
    /// capture), OPEN OUTPUT of an absent file (a creation) and the first WRITE under EXTEND.</summary>
    [Theory]
    [InlineData(false, "output-present")]
    [InlineData(true, "output-present")]
    [InlineData(false, "output-absent")]
    [InlineData(true, "output-absent")]
    [InlineData(false, "extend-write")]
    [InlineData(true, "extend-write")]
    public void A_transactions_holder_that_cannot_be_published_refuses_the_statement_30_whatever_the_cause(bool lostRace, string statement)
    {
        string dir = ScratchDirectory.Create("scc-");
        string f = Path.Combine(dir, "f.dat");
        try
        {
            if (statement != "output-absent") File.WriteAllText(f, "C1C2", Encoding.Latin1);
            var medium = new FaultHostMedium { PublicationAnswer = lostRace ? RegionLockResult.Held : RegionLockResult.Unavailable };
            var u = new Unit(f, covered: true, medium);
            string status = statement == "extend-write"
                ? (u.Open(FileOpenMode.Extend) == "00" ? u.Write("X1") : "open refused")
                : u.Open(FileOpenMode.Output);
            Assert.Equal("30", status);
            Assert.Contains(medium.Log, op => op.StartsWith("publish txn-", StringComparison.Ordinal));
            u.Reg.CloseAll();
            Assert.Equal(statement == "output-absent" ? "<absent>" : "C1C2", Show(State(f)));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
