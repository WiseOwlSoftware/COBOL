// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SEQUENTIAL TRANSACTION JOURNAL OVER REAL BYTES (docs/rearchitecture/DESIGN-runtime-library.md 2.2.2.4 to 2.2.2.8 and
/// 2.2.2.12; kb/Work PB3340): <see cref="SequentialJournal"/> driven as its owner drives it, through the fault-injecting
/// <see cref="FaultHostMedium"/>, with every operation of each named row REFUSED, made the point of a DEATH and the point of a
/// POWER LOSS in turn, and the invariants of 2.2.2.12 asserted after "the next entry" (a fresh run unit's entry step):
/// <b>I1</b> F holds exactly the state at the last COMMIT or exactly the state the COMMIT made durable, never a third, and the
/// program was told success only in the second case — and, when the fault was a REFUSAL, which the process survives and item
/// 8's ROLLBACK follows (§14.6.13.1.3 8), exactly the state at the last COMMIT unless the COMMIT answered success; <b>I3</b>
/// no holder, record, before file or lock directory is left behind. Expected states come from the rules: §9.1.18.3 (a COMMIT
/// makes the changes permanent), §14.9.36.4 GR3 (a ROLLBACK restores the files <i>"to the state they were in at the execution
/// of the last COMMIT statement or the start of the run unit"</i>), §9.1.18.5 (an abnormal termination rolls them back).
/// </summary>
public sealed class SequentialJournalTests
{
    private static readonly byte[] C1C2 = "C1C2"u8.ToArray();

    // ── The owner, as a run unit's registry and connector drive the journal ────────────────────────────────────────

    /// <summary>One run unit's covered connector on F: its kept handle and its transaction.</summary>
    private sealed class Owner(IHostMedium medium, string hostPath) : IJournalData
    {
        internal SequentialJournal? Journal;
        internal FileStream? Handle;

        public FileStream Writable(bool create) =>
            Handle ??= medium.OpenConnectorStream(hostPath, create ? FileMode.OpenOrCreate : FileMode.Open, FileAccess.ReadWrite, FileShare.None, FileOptions.None);

        public void Release()
        {
            Handle?.Dispose();
            Handle = null;
        }

        /// <summary>Run <paramref name="body"/> under the entry mutex, after the entry step.</summary>
        internal void InMutex(Action body)
        {
            var mutex = medium.EntryMutex(hostPath);
            try
            {
                Assert.Equal(RegionLockResult.Ok, mutex.Hold(HostRegionLocks.EntryMutexByte, exclusive: true, wait: true));
                _ = SequentialJournal.Enter(medium, hostPath, Journal?.Id, mutex);
                body();
            }
            finally { mutex.Dispose(); }
        }

        internal void Begin(SequentialJournal.StartState start) =>
            InMutex(() => Journal ??= SequentialJournal.Begin(medium, hostPath, start));

        internal void Append(string record)
        {
            var h = Writable(create: false);
            Journal!.NoteFirstAppend(h.Length);
            h.Seek(0, SeekOrigin.End);
            h.Write(Encoding.Latin1.GetBytes(record));
        }

        internal void Rewrite(long ordinal, int width, string record)
        {
            var h = Writable(create: false);
            long offset = ordinal * width;
            if (Journal!.NeedsImage(ordinal, offset))
            {
                byte[] image = new byte[width];
                h.Position = offset;
                h.ReadExactly(image);
                Journal.NoteImage(ordinal, offset, image);
            }
            h.Position = offset;
            h.Write(Encoding.Latin1.GetBytes(record));
        }

        internal void Capture(SequentialJournal.CaptureVerb verb) => InMutex(() => Journal!.Capture(verb, Writable(create: false)));

        /// <summary>COMMIT; true when it answered success. A reversed decision is rolled back at once (2.2.2.6 region 3).</summary>
        internal bool Commit()
        {
            var outcome = Journal!.Commit(Handle);
            if (outcome == CommitOutcome.Committed) { Journal = null; return true; }
            if (outcome == CommitOutcome.Reversed) Rollback();
            return false;
        }

        /// <summary>ROLLBACK (the statement, item 8's, the termination path's); true when it completed.</summary>
        internal bool Rollback()
        {
            bool done = false;
            InMutex(() => done = Journal!.Rollback(this));
            if (done) Journal = null;
            return done;
        }

        /// <summary>Item 8's unconditional ROLLBACK after a refused statement, when a transaction stands.</summary>
        internal void Item8()
        {
            if (Journal is not null) _ = Rollback();
        }
    }

    // ── The sweep ───────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A row of 2.2.2.12: the program, run as one run unit's statements over F, and true when its last COMMIT answered
    /// success. A refusal (an <see cref="IOException"/>) is the statement's '30', fatal: item 8's ROLLBACK follows and the
    /// program ends.</summary>
    private delegate bool Program(Owner owner, IHostMedium medium, string f);

    private static (string Directory, string File) NewFile(byte[]? initial)
    {
        string dir = ScratchDirectory.Create("sjt-");
        string f = Path.Combine(dir, "f.dat");
        if (initial is not null) File.WriteAllBytes(f, initial);
        return (dir, f);
    }

    private static byte[]? State(string f) => File.Exists(f) ? File.ReadAllBytes(f) : null;

    private static string Show(byte[]? state) => state is null ? "<absent>" : Encoding.Latin1.GetString(state);

    private static void AssertNoLitter(string f)
    {
        string lockDirectory = SequentialJournal.DirectoryOf(f);
        string[] left = Directory.Exists(lockDirectory) ? Directory.GetFileSystemEntries(lockDirectory) : [];
        Assert.True(left.Length == 0, $"I3: left behind in the lock directory: {string.Join(", ", left.Select(Path.GetFileName))}");
    }

    /// <summary>Run one program under every fault at every operation, and assert I1 and I3 after the next entry.</summary>
    private static void Sweep(byte[]? initial, byte[]? committedBefore, byte[]? committedAfter, Program program)
    {
        int total, synced, handed;
        {
            var (dir, f) = NewFile(initial);
            try
            {
                var medium = new FaultHostMedium();
                var owner = new Owner(medium, f);
                bool told = program(owner, medium, f);
                owner.Release();
                total = medium.Operations;
                // The operations that decide a COMMIT: the write of its decision (handed to the operating environment, which a
                // death leaves in the cache, so every live process reads it) and that write's sync (which a power loss needs).
                // A fault after the one its mode needs may leave the program untold, but the state the COMMIT made durable is
                // then the only one (2.2.2.6 regions 2 and 3).
                synced = committedAfter is null && committedBefore is null || Same(committedBefore, committedAfter)
                    ? int.MaxValue
                    : medium.Log.FindLastIndex(op => op.StartsWith("sync txn-", StringComparison.Ordinal) && op.EndsWith(".journal", StringComparison.Ordinal)) + 1;
                handed = synced == int.MaxValue ? int.MaxValue : synced - 1;
                Assert.True(told || committedBefore is null && committedAfter is null || Same(committedBefore, committedAfter),
                    "the fault-free run is told success");
                Assert.Equal(Show(committedAfter), Show(State(f)));
                AssertNoLitter(f);
            }
            finally { ScratchDirectory.Remove(dir); }
        }
        Assert.True(total > 0, "the row reaches the medium");
        foreach (var fault in new[] { FaultHostMedium.Fault.Refuse, FaultHostMedium.Fault.Die, FaultHostMedium.Fault.Power, FaultHostMedium.Fault.PowerKeepingData })
            for (int n = 1; n <= total; n++)
            {
                var (dir, f) = NewFile(initial);
                try
                {
                    var medium = new FaultHostMedium(fault, n);
                    var owner = new Owner(medium, f);
                    bool told = false;
                    try { told = program(owner, medium, f); }
                    catch (FaultHostMedium.Death) { }
                    catch (IOException) { try { owner.Item8(); } catch (IOException) { } }
                    medium.Kill();   // the run unit ends: its handles and its holder go with it
                    _ = SequentialJournal.Enter(HostMedium.Production, f, null, null);   // the next entry
                    var state = State(f);
                    string where = $"{fault} at operation {n} of {total} ({medium.Log.ElementAtOrDefault(n - 1)}): F = {Show(state)}";
                    if (told) Assert.True(Same(state, committedAfter), "I1, told success: " + where);
                    else if (fault == FaultHostMedium.Fault.Refuse) Assert.True(Same(state, committedBefore), "I1, refused and rolled back: " + where);
                    else if (n > (fault == FaultHostMedium.Fault.Die ? handed : synced))
                        Assert.True(Same(state, committedAfter), "I1, the decision was durable: " + where);
                    else Assert.True(Same(state, committedBefore), "I1, the decision was not durable: " + where);
                    AssertNoLitter(f);
                }
                finally { ScratchDirectory.Remove(dir); }
            }
    }

    private static bool Same(byte[]? a, byte[]? b) => a is null ? b is null : b is not null && a.AsSpan().SequenceEqual(b);

    [Fact]
    public void First_write_under_extend_then_commit()
    {
        // 2.2.2.12 "first WRITE under EXTEND": no journal and F intact / header torn: nothing to undo / F at L after the entry's
        // rollback / committed only after the decision.
        Sweep(C1C2, C1C2, "C1C2X1"u8.ToArray(), (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.Append("X1");
            return o.Commit();
        });
    }

    [Fact]
    public void Rewrite_twice_then_rollback_restores_one_image()
    {
        // "REWRITE twice, ROLLBACK": one image (C1); F's record as before.
        Sweep(C1C2, C1C2, C1C2, (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.Rewrite(0, 2, "Y1");
            o.Rewrite(0, 2, "Y2");
            return o.Rollback();
        });
    }

    [Fact]
    public void Append_then_capture_then_rollback_restores_the_before_file_and_truncates_to_L()
    {
        // "R1-1 append then capture": the before file holds C1 C2 X1; truncated to L after the restore: C1 C2.
        Sweep(C1C2, C1C2, C1C2, (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.Append("X1");
            o.Capture(SequentialJournal.CaptureVerb.OpenOutput);
            o.Handle!.SetLength(0);
            o.Append("X2");
            return o.Rollback();
        });
    }

    [Fact]
    public void Capture_then_rewrite_journals_nothing_after_the_capture()
    {
        // "R1-1 capture then REWRITE": F = C1 C2; OPEN OUTPUT, WRITE X1 X2, ..., REWRITE Y1, ROLLBACK: F = C1 C2.
        Sweep(C1C2, C1C2, C1C2, (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.Capture(SequentialJournal.CaptureVerb.OpenOutput);
            o.Handle!.SetLength(0);
            o.Append("X1");
            o.Append("X2");
            o.Rewrite(0, 2, "Y1");
            return o.Rollback();
        });
    }

    [Fact]
    public void Rewrite_then_capture_applies_the_base_phase_image_to_the_restored_file()
    {
        // "REWRITE then capture": the base phase's image applies to the restored file: C1 C2.
        Sweep(C1C2, C1C2, C1C2, (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.Rewrite(0, 2, "Y1");
            o.Capture(SequentialJournal.CaptureVerb.OpenOutput);
            o.Handle!.SetLength(0);
            o.Append("X1");
            return o.Rollback();
        });
    }

    [Fact]
    public void Delete_file_then_commit_leaves_the_file_absent_durably()
    {
        // "DELETE FILE then COMMIT": absent, durably (the directory synced before the decision).
        Sweep(C1C2, C1C2, null, (o, m, f) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.InMutex(() =>
            {
                o.Journal!.Capture(SequentialJournal.CaptureVerb.DeleteFile, o.Writable(create: false));
                o.Release();
                m.DeleteFile(f);
                m.SyncDirectory(Path.GetDirectoryName(f)!);
            });
            return o.Commit();
        });
    }

    [Fact]
    public void Delete_file_then_recreate_then_rollback_restores_the_before_file()
    {
        // "DELETE FILE then re-create then ROLLBACK": the re-creation is replaced-phase and unjournaled; C1 C2.
        Sweep(C1C2, C1C2, C1C2, (o, m, f) =>
        {
            o.Begin(SequentialJournal.StartState.Present);
            o.InMutex(() =>
            {
                o.Journal!.Capture(SequentialJournal.CaptureVerb.DeleteFile, o.Writable(create: false));
                o.Release();
                m.DeleteFile(f);
                m.SyncDirectory(Path.GetDirectoryName(f)!);
            });
            _ = o.Writable(create: true);
            o.Append("X1");
            return o.Rollback();
        });
    }

    [Fact]
    public void A_creation_rolled_back_removes_the_file()
    {
        // "R1-2 created by OPEN OUTPUT": removed; a later OPEN INPUT answers '35'.
        Sweep(null, null, null, (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Absent);
            _ = o.Writable(create: true);
            o.Append("X1");
            return o.Rollback();
        });
    }

    [Fact]
    public void A_creation_committed_keeps_the_file()
    {
        // "create, ..., COMMIT": X1; a refusal or death anywhere before the decision: absent.
        Sweep(null, null, "X1"u8.ToArray(), (o, _, _) =>
        {
            o.Begin(SequentialJournal.StartState.Absent);
            _ = o.Writable(create: true);
            o.Append("X1");
            return o.Commit();
        });
    }

    [Fact]
    public void Commit_then_more_then_rollback_truncates_to_the_second_transactions_L()
    {
        // "COMMIT then more, ROLLBACK": C1 X1 — the second transaction's L is the first's committed length.
        var c1 = "C1"u8.ToArray();
        var (dir, f) = NewFile(c1);
        try
        {
            var o = new Owner(HostMedium.Production, f);
            o.Begin(SequentialJournal.StartState.Present);
            o.Append("X1");
            Assert.True(o.Commit());
            o.Begin(SequentialJournal.StartState.Present);
            o.Append("X2");
            Assert.Equal(4, o.Journal!.CommittedLength);
            Assert.True(o.Rollback());
            o.Release();
            Assert.Equal("C1X1", Show(State(f)));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    // ── The format and the entry step ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_record_is_its_header_then_whole_records_and_a_torn_tail_ends_them()
    {
        // 2.2.2.4: magic WOLJ, format 1, the id, the START, a CRC-32C; then records, each with its own CRC; a torn tail ends them.
        var (dir, f) = NewFile(C1C2);
        try
        {
            var o = new Owner(HostMedium.Production, f);
            o.Begin(SequentialJournal.StartState.Present);
            o.Append("X1");
            o.Rewrite(1, 2, "Y2");
            string record = Path.Combine(SequentialJournal.DirectoryOf(f), $"txn-{o.Journal!.Id}.journal");
            byte[] bytes;
            using (var read = new FileStream(record, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                bytes = new byte[read.Length];
                read.ReadExactly(bytes);
            }
            Assert.Equal("WOLJ", Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Equal(1u, BitConverter.ToUInt32(bytes, 4));
            Assert.Equal((byte)'P', bytes[16]);
            Assert.Equal(SequentialJournal.JournalHeaderBytes + 13 + 1 + 20 + 2 + 4, bytes.Length);   // header, L, R(2 bytes)
            var whole = SequentialJournal.ReadRecord(new MemoryStream(bytes))!;
            Assert.Equal(4, whole.CommittedLength);
            Assert.Equal([(1L, 2L, "C2")], whole.Images.Select(i => (i.Ordinal, i.Offset, Encoding.Latin1.GetString(i.Bytes))));
            // One byte short of the R record: it is torn, and the records end at the L.
            var torn = SequentialJournal.ReadRecord(new MemoryStream(bytes[..^1]))!;
            Assert.Equal(4, torn.CommittedLength);
            Assert.Empty(torn.Images);
            // A header whose checksum fails is litter: nothing to undo.
            byte[] bad = (byte[])bytes.Clone();
            bad[9] ^= 1;
            Assert.Null(SequentialJournal.ReadRecord(new MemoryStream(bad)));
            Assert.True(o.Rollback());
            o.Release();
            Assert.Equal(C1C2, State(f));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The journal's bytes up to and including its <c>L</c> record, then an <c>R</c> record's kind and fixed head whose
    /// length word is <paramref name="lengthWord"/> and whose payload is <paramref name="payload"/> bytes of 'x' (none: a tail torn
    /// at the head). 2.2.2.4: the header, then records each with its own CRC-32C; a torn tail ends them.</summary>
    private static byte[] JournalWithTornImage(int lengthWord, int payload)
    {
        var (dir, f) = NewFile(C1C2);
        try
        {
            var o = new Owner(HostMedium.Production, f);
            o.Begin(SequentialJournal.StartState.Present);
            o.Append("X1");
            string record = Path.Combine(SequentialJournal.DirectoryOf(f), $"txn-{o.Journal!.Id}.journal");
            byte[] prefix;
            using (var read = new FileStream(record, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                prefix = new byte[SequentialJournal.JournalHeaderBytes + 13];   // the header and the L record
                read.ReadExactly(prefix);
            }
            Assert.True(o.Rollback());
            o.Release();
            var tail = new byte[1 + 20 + payload];
            tail[0] = (byte)'R';
            BitConverter.TryWriteBytes(tail.AsSpan(1 + 16), lengthWord);
            tail.AsSpan(21).Fill((byte)'x');
            return [.. prefix, .. tail];
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A record file read from memory for its first <c>prefix.Length</c> bytes and from there as zeros up to
    /// <paramref name="length"/>: a journal over 2 GiB that no test has to write.</summary>
    private sealed class SparseStream(byte[] prefix, long length) : Stream
    {
        private long _position;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get => _position; set => _position = value; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = (int)Math.Min(count, length - _position);
            for (int k = 0; k < n; k++) buffer[offset + k] = _position + k < prefix.Length ? prefix[_position + k] : (byte)0;
            _position += n;
            return n;
        }
        public override long Seek(long offset, SeekOrigin origin) =>
            _position = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => _position + offset, _ => length + offset };
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(int.MaxValue)]            // 1 + 20 + length + 4 wraps negative
    [InlineData(int.MaxValue - 10)]       // wraps too
    [InlineData(int.MaxValue - 24)]       // 1 + 20 + length + 4 is exactly 2^31: wraps to int.MinValue
    [InlineData(int.MaxValue - 60)]       // fits an int, exceeds the record: no 2 GiB allocation for a record that cannot fill it
    [InlineData(1 << 20)]                 // an ordinary oversized word
    [InlineData(-1)]                      // negative
    public void An_image_record_whose_length_word_the_record_cannot_hold_is_a_torn_tail_not_an_exception(int lengthWord)
    {
        // 2.2.2.4: "a torn tail ends them". The R record carries 2 payload bytes of the length it claims and no checksum.
        byte[] bytes = JournalWithTornImage(lengthWord, payload: 2);
        var contents = SequentialJournal.ReadRecord(new MemoryStream(bytes))!;
        Assert.Equal(4, contents.CommittedLength);
        Assert.Empty(contents.Images);
    }

    [Fact]
    public void A_journal_over_two_gibibytes_reads_its_records_without_loading_it_whole()
    {
        // The reader streams, so a record file past int.MaxValue bytes is read as any other: its whole records, then its torn
        // tail (here the first byte of the zeros that follow the records, an unknown kind).
        byte[] prefix = JournalWithTornImage(2, payload: 2 + 4);   // a WHOLE-LENGTH image claim; its checksum is zeros: torn
        using var sparse = new SparseStream(prefix, 3L * 1024 * 1024 * 1024);
        var contents = SequentialJournal.ReadRecord(sparse)!;
        Assert.Equal(4, contents.CommittedLength);
        Assert.Empty(contents.Images);
        // The same journal with a length word claiming more than the record still holds: torn at the head, whatever the file size.
        using var claims = new SparseStream(JournalWithTornImage(int.MaxValue - 60, payload: 0), 3L * 1024 * 1024 * 1024);
        Assert.Empty(SequentialJournal.ReadRecord(claims)!.Images);
    }

    [Fact]
    public void A_dead_transactions_record_with_a_wrapped_image_length_is_rolled_back_by_the_next_entry()
    {
        // 2.2.2.7 / §14.9.36.4 GR3: the settle reads the record through ReadRecord, so a tail torn by a length word that wraps an
        // int bound is the end of the records, and the entering run unit restores F to its state at the last COMMIT.
        var (dir, f) = NewFile(C1C2);
        try
        {
            var medium = new FaultHostMedium();
            var a = new Owner(medium, f);
            a.Begin(SequentialJournal.StartState.Present);
            a.Append("X1");
            string record = Path.Combine(SequentialJournal.DirectoryOf(f), $"txn-{a.Journal!.Id}.journal");
            a.Release();
            byte[] tail = new byte[1 + 20];
            tail[0] = (byte)'R';
            BitConverter.TryWriteBytes(tail.AsSpan(1 + 16), int.MaxValue - 10);
            using (var raw = new FileStream(record, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) raw.Write(tail);
            medium.Kill();
            var after = SequentialJournal.Enter(HostMedium.Production, f, null, null);
            Assert.False(after.Any);
            Assert.Equal("C1C2", Show(State(f)));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_live_transactions_holdings_are_read_and_a_dead_ones_settled_by_the_next_entry()
    {
        // 2.2.2.3 and 2.2.2.8: a live base transaction holds the end, its rewritten records and every record at or past its L;
        // a replaced one holds the whole file; a dead one is rolled back by the entering run unit.
        var (dir, f) = NewFile("C1C2C3"u8.ToArray());
        try
        {
            var medium = new FaultHostMedium();
            var a = new Owner(medium, f);
            a.Begin(SequentialJournal.StartState.Present);
            a.Rewrite(1, 2, "Y2");
            a.Append("X4");
            a.Release();   // the handle goes; the transaction and its holder stand
            var seen = SequentialJournal.Enter(HostMedium.Production, f, null, null);
            Assert.True(seen.Any);                                   // the end: a WRITE answers '51'
            Assert.Null(seen.RefusesOpen(exclusive: false));         // a base transaction admits another writable OPEN
            Assert.Equal("61", seen.RefusesOpen(exclusive: true));   // an OPEN OUTPUT or a DELETE FILE needs no transaction at all
            Assert.False(seen.HoldsRecordAt(0));                     // a record it neither rewrote nor appended
            Assert.True(seen.HoldsRecordAt(2));                      // its rewritten record
            Assert.True(seen.HoldsRecordAt(6));                      // its own append, at L
            medium.Kill();                                           // the owner dies
            var after = SequentialJournal.Enter(HostMedium.Production, f, null, null);
            Assert.False(after.Any);
            Assert.Equal("C1C2C3", Show(State(f)));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void Ids_are_drawn_lowest_first_and_a_replaced_transaction_refuses_every_writable_open()
    {
        var (dir, f) = NewFile(C1C2);
        try
        {
            var a = new Owner(HostMedium.Production, f);
            a.Begin(SequentialJournal.StartState.Present);
            Assert.Equal(0, a.Journal!.Id);
            a.Capture(SequentialJournal.CaptureVerb.OpenOutput);
            var seen = SequentialJournal.Enter(HostMedium.Production, f, null, null);
            Assert.Equal("61", seen.RefusesOpen(exclusive: false));
            Assert.True(seen.HoldsRecordAt(0));
            // The owner's own entry step sees nothing of its own transaction.
            Assert.False(SequentialJournal.Enter(HostMedium.Production, f, a.Journal.Id, null).Any);
            Assert.True(a.Rollback());
            a.Release();
            Assert.Equal(C1C2, State(f));
            AssertNoLitter(f);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void A_first_append_writes_one_record_and_one_sync_and_later_appends_write_nothing()
    {
        // 2.2.2.13, measured through the medium's operation log: a covered WRITE adds zero journal operations after the
        // transaction's first, which adds one append and one sync (R3).
        var (dir, f) = NewFile(C1C2);
        try
        {
            var medium = new FaultHostMedium();
            var o = new Owner(medium, f);
            o.Begin(SequentialJournal.StartState.Present);
            _ = o.Writable(create: false);
            int before = medium.Log.Count;
            o.Append("X1");
            Assert.Equal(["write txn-0.journal", "sync txn-0.journal", "write f.dat"], medium.Log.Skip(before));
            before = medium.Log.Count;
            o.Append("X2");
            Assert.Equal(["write f.dat"], medium.Log.Skip(before));
            Assert.True(o.Rollback());
            o.Release();
            Assert.Equal(C1C2, State(f));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
