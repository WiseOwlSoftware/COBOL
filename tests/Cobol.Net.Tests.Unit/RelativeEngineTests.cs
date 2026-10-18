// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using CobolNet.Tests.Unit.Store;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE RELATIVE ORGANIZATION ON THE KEYED STORE ENGINE, THROUGH ITS CONNECTOR (docs/rearchitecture/DESIGN-runtime-library.md
/// §2.2.1; kb/Work PB3333). The page layer's and the tree's own cases (PB3331, PB3332) run over an in-memory medium; these
/// run the SAME rows through <see cref="RelativeConnector"/> and <see cref="FileRegistry"/> over a real file, with the
/// fault injected under the connector's own handle (<see cref="RelativeStore.MediumObserver"/>):
/// <list type="bullet">
///   <item>2.2.1.16, "A changing statement (coherent)" and "(NO OTHER)", and "A promotion (CLOSE)": a process death after
///   EVERY write of a WRITE, a REWRITE, a DELETE and a CLOSE. The bar: the next OPEN reads the file as the last COMPLETED
///   statement left it, never torn — the statement whose page 2 landed is in it, the one whose did not is not.</item>
///   <item>2.2.1.16, "Every statement REFUSED part-way": a host refusal at every write of each statement — the statement's
///   '30' (§9.1.13.6 item 1), §9.1.13.1's permanent error replayed by the same connector, the CLOSE ending it
///   (DOC-A.1-105), the next OPEN reading the previous statement's file, and another run unit's next statement
///   succeeding.</item>
///   <item>2.2.1.17: two run units' statements in every sharing mode but NO OTHER (the coherence condition,
///   <see cref="FileLockPosture.AdmitsAnotherRunUnit"/>), a death or refusal of one ∥ the other's next statement, a
///   statement ∥ a writable or an INPUT OPEN, and a sequential reader whose tree another run unit splits.</item>
/// </list>
/// Every expected value is the design's bar or a general rule's, never what the engine happens to do.
/// </summary>
public sealed class RelativeEngineTests
{
    private static string Tmp(string tag) => Path.Combine(Path.GetTempPath(), $"pb3333-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        try { File.Delete(host); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static void Register(FileRegistry reg, string name, string host, FileSharing? sharing,
        KeyedAccess access = KeyedAccess.Dynamic)
    {
        reg.RegisterRelative(name, host, recordWidth: 8, optional: false, accessMode: (int)access, relativeKeyDigits: 4,
            varyMin: -1, varyMax: -1);
        if (sharing is { } s) reg.RegisterSharing(name, s, FileLockMode.Manual, multiple: false);
    }

    private static string Rec(int n, string payload) => $"{n:0000}{payload}";

    private static string Write(FileRegistry reg, string name, int n, string payload)
    {
        reg.SetRelativeKey(name, n);
        return reg.WriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null);
    }

    private static string Rewrite(FileRegistry reg, string name, int n, string payload)
    {
        reg.SetRelativeKey(name, n);
        return reg.RewriteShared(name, Rec(n, payload), -1, FileRecordLock.None, FileRetryKind.None, 0);
    }

    private static string Delete(FileRegistry reg, string name, int n)
    {
        reg.SetRelativeKey(name, n);
        return reg.DeleteShared(name, Rec(n, "    "), FileRetryKind.None, 0);
    }

    private static string ReadKey(FileRegistry reg, string name, int n, out string image)
    {
        reg.SetRelativeKey(name, n);
        return reg.ReadKeyedShared(name, -1, Rec(n, "    "), FileRecordLock.None, ignoringLock: false,
            FileRetryKind.None, 0, out image);
    }

    private static string ReadNext(FileRegistry reg, string name, out string image) =>
        reg.ReadShared(name, previous: false, FileRecordLock.None, advancingOnLock: false, ignoringLock: false,
            FileRetryKind.None, 0, out image);

    private static void Seed(string host)
    {
        var seed = new FileRegistry();
        Register(seed, "S", host, sharing: null);
        seed.OpenStatic("S", FileOpenMode.Output);
        Assert.Equal("00", Write(seed, "S", 1, "AAAA"));
        Assert.Equal("00", Write(seed, "S", 2, "BBBB"));
        seed.Close("S");
        Assert.Equal("00", seed.Status("S"));
    }

    /// <summary>Every record of the file, read by the next run unit's OPEN INPUT and READ NEXT loop.</summary>
    private static List<string> Records(string host)
    {
        var reader = new FileRegistry();
        Register(reader, "R", host, sharing: null);
        reader.OpenStatic("R", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("R"));
        var all = new List<string>();
        string status;
        while ((status = ReadNext(reader, "R", out string image)) == "00") all.Add(image);
        Assert.Equal("10", status);   // §14.9.30.4 GR24's at end, never '30': the file is not torn
        reader.Close("R");
        return all;
    }

    public enum Change { Write, Rewrite, Delete }

    private static string Run(FileRegistry reg, Change change) => change switch
    {
        Change.Write => Write(reg, "A", 3, "CCCC"),
        Change.Rewrite => Rewrite(reg, "A", 2, "ZZZZ"),
        _ => Delete(reg, "A", 1),
    };

    private static readonly string[] Before = ["0001AAAA", "0002BBBB"];

    private static string[] After(Change change) => change switch
    {
        Change.Write => ["0001AAAA", "0002BBBB", "0003CCCC"],
        Change.Rewrite => ["0001AAAA", "0002ZZZZ"],
        _ => ["0002BBBB"],
    };

    public static TheoryData<Change, FileSharing> Statements()
    {
        var data = new TheoryData<Change, FileSharing>();
        foreach (var change in Enum.GetValues<Change>())
            foreach (var sharing in new[] { FileSharing.AllOther, FileSharing.ReadOnly, FileSharing.NoOther })
                data.Add(change, sharing);
        return data;
    }

    /// <summary>The writes one statement hands over, measured on a run that does not die.</summary>
    private static int WritesOf(Change change, FileSharing sharing, out int closeWrites)
    {
        string host = Tmp("measure");
        try
        {
            Seed(host);
            DyingPageMedium? medium = null;
            RelativeStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
            var a = new FileRegistry();
            try
            {
                Register(a, "A", host, sharing);
                a.OpenStatic("A", FileOpenMode.IO);
            }
            finally { RelativeStore.MediumObserver.Value = null; }
            int before = medium!.Writes;
            Assert.Equal("00", Run(a, change));
            int statement = medium.Writes - before;
            a.Close("A");
            closeWrites = medium.Writes - before - statement;
            return statement;
        }
        finally { TryDelete(host); }
    }

    /// <summary>2.2.1.16's crash points of a changing statement and of the CLOSE's promotion, through the connector: a death
    /// after each write <c>i</c> of the statement leaves the previous statement's file until the statement's LAST write —
    /// its page 2 — lands, and the statement's file from then on; a death at any write of the CLOSE that follows keeps the
    /// statement (its page 2 already named it). For a coherent connector a second run unit B, open beside the dying A,
    /// then runs its own WRITE: it succeeds ('00', claiming above whatever A claimed — finding R3-4), A's unfinished change
    /// stays invisible, and B's record is in the file beside A's completed ones (2.2.1.17, "A's statement dies mid-way").</summary>
    [Theory]
    [MemberData(nameof(Statements))]
    public void A_death_after_every_write_leaves_the_last_completed_statement(Change change, FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int writes = WritesOf(change, sharing, out int closeWrites);
        Assert.True(writes >= 2, $"a changing statement writes its pages and then page 2 ({writes})");
        for (int i = 0; i <= writes + closeWrites; i++)
        {
            string host = Tmp($"die-{change}-{sharing}-{i}");
            try
            {
                Seed(host);
                DyingPageMedium? medium = null;
                RelativeStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
                var a = new FileRegistry();
                try
                {
                    Register(a, "A", host, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { RelativeStore.MediumObserver.Value = null; }
                FileRegistry? b = null;
                if (sharing != FileSharing.NoOther)
                {
                    b = new FileRegistry();
                    Register(b, "B", host, FileSharing.AllOther);
                    b.OpenStatic("B", sharing == FileSharing.ReadOnly ? FileOpenMode.Input : FileOpenMode.IO);
                    Assert.Equal("00", b.Status("B"));
                }
                medium!.DieAfter = medium.Writes + i;
                Run(a, change);   // what the dying run unit is told is not asked: it is dead from write i on
                a.Close("A");     // its handle and its locks go with it; none of its writes after the death land
                bool completed = i >= writes;
                var expected = (completed ? After(change) : Before).ToList();
                if (b is not null)
                {
                    if (sharing == FileSharing.AllOther)
                    {
                        Assert.Equal("00", Write(b, "B", 5, "EEEE"));
                        expected.Add("0005EEEE");
                    }
                    else
                    {
                        // The INPUT reader beside a READ ONLY writer follows page 2 and reads the completed file.
                        Assert.Equal(completed && change == Change.Delete ? "23" : "00", ReadKey(b, "B", 1, out _));
                    }
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
                Assert.Equal(expected, Records(host));
            }
            finally
            {
                RelativeStore.MediumObserver.Value = null;
                TryDelete(host);
            }
        }
    }

    /// <summary>2.2.1.16's refused statements: a host refusal of write <c>i</c> of a changing statement answers THAT
    /// statement '30' (§9.1.13.6 item 1: <i>"A permanent error exists and no further information is available"</i>), the same
    /// connector's next statement replays it and touches nothing (§9.1.13.1: <i>"The permanent error condition remains in
    /// effect for all subsequent input-output operations on the file"</i>), its CLOSE ends it ('00', DOC-A.1-105), and the
    /// next OPEN reads the previous statement's file. Another run unit's next statement succeeds (2.2.1.17, "A's
    /// statement is refused mid-way").</summary>
    [Theory]
    [MemberData(nameof(Statements))]
    public void A_refusal_at_every_write_is_the_statements_30_and_leaves_the_previous_statement(Change change, FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int writes = WritesOf(change, sharing, out _);
        for (int i = 0; i < writes; i++)
        {
            string host = Tmp($"refuse-{change}-{sharing}-{i}");
            int refuseAt = int.MaxValue, seen = 0;
            try
            {
                Seed(host);
                RelativeStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                    write: _ => seen++ == refuseAt ? new IOException("refused by the host") : null);
                var a = new FileRegistry();
                try
                {
                    Register(a, "A", host, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { RelativeStore.MediumObserver.Value = null; }
                refuseAt = seen + i;
                Assert.Equal("30", Run(a, change));
                Assert.Equal("30", Write(a, "A", 7, "GGGG"));                   // replayed, nothing touched
                Assert.Equal("30", ReadKey(a, "A", 2, out string image));
                Assert.Equal("", image.Trim());
                if (sharing == FileSharing.AllOther)
                {
                    var b = new FileRegistry();
                    Register(b, "B", host, FileSharing.AllOther);
                    b.OpenStatic("B", FileOpenMode.IO);
                    Assert.Equal("00", Write(b, "B", 5, "EEEE"));
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
                a.Close("A");
                Assert.Equal("00", a.Status("A"));
                var expected = Before.ToList();
                if (sharing == FileSharing.AllOther) expected.Add("0005EEEE");
                Assert.Equal(expected, Records(host));
            }
            finally
            {
                RelativeStore.MediumObserver.Value = null;
                TryDelete(host);
            }
        }
    }

    /// <summary>2.2.1.17, "A's statement ∥ B's statement", in the modes that admit another run unit: each run unit's record
    /// statement starts from the file as the other's last statement left it — B reads A's WRITE, A reads B's REWRITE,
    /// A's READ NEXT skips B's DELETE, and the invalid key conditions are the file's: '22' for a record the other wrote,
    /// '23' for one the other deleted. A writable OPEN and an INPUT OPEN beside a writer (2.2.1.17, "A's statement ∥ B's
    /// writable OPEN / INPUT OPEN") read the writer's completed statements, and the writer, after the other's fence, reads
    /// the other's.</summary>
    [Fact]
    public void Two_run_units_see_each_others_statements_at_every_statement()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp("pair");
        try
        {
            Seed(host);
            var a = new FileRegistry();
            Register(a, "A", host, FileSharing.AllOther);
            a.OpenStatic("A", FileOpenMode.IO);
            Assert.Equal("00", Write(a, "A", 3, "CCCC"));
            var b = new FileRegistry();                       // a writable OPEN after A's statement: adopts and fences it
            Register(b, "B", host, FileSharing.AllOther);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", ReadKey(b, "B", 3, out string image));
            Assert.Equal("0003CCCC", image);
            Assert.Equal("22", Write(b, "B", 3, "XXXX"));      // §14.9.51.4 GR33 a): the record A wrote exists
            Assert.Equal("00", Rewrite(b, "B", 3, "DDDD"));
            Assert.Equal("00", Delete(b, "B", 1));
            Assert.Equal("00", ReadKey(a, "A", 3, out image)); // A's entry adopts B's page 2 after B's fence
            Assert.Equal("0003DDDD", image);
            Assert.Equal("23", ReadKey(a, "A", 1, out _));      // §9.1.13.5 3 a): B deleted it
            Assert.Equal("23", Delete(a, "A", 1));
            var c = new FileRegistry();                       // an INPUT OPEN beside two writers
            Register(c, "C", host, FileSharing.AllOther, KeyedAccess.Sequential);
            c.OpenStatic("C", FileOpenMode.Input);
            var seen = new List<string>();
            while (ReadNext(c, "C", out string next) == "00")
            {
                seen.Add(next);
                if (seen.Count == 1) Assert.Equal("00", Write(a, "A", 9, "IIII"));   // released past C's position
            }
            Assert.Equal(["0002BBBB", "0003DDDD", "0009IIII"], seen);
            c.Close("C");
            a.Close("A");
            b.Close("B");
            Assert.Equal(["0002BBBB", "0003DDDD", "0009IIII"], Records(host));
        }
        finally { TryDelete(host); }
    }

    /// <summary>⛔ THE REGRESSION FINDING R2-6 NAMED (2.2.1.7; 2.2.1.14): a <c>SHARING WITH READ ONLY</c> writer beside the
    /// <c>INPUT SHARING WITH ALL OTHER</c> reader Table 19 admits (§9.1.15 2): <i>"restricts concurrent access to a physical
    /// file through file connectors other than this one, to input mode"</i>). The engine writes at every statement, so the
    /// writer runs under the store mutex (<see cref="FileLockPosture.AdmitsAnotherRunUnit"/>) and every READ NEXT of the
    /// reader, running concurrently on another thread, answers '00' or '10' — never '30', the half-written path. Counted,
    /// not timed: the writer releases a fixed number of records and the reader reads until it has seen them all.</summary>
    [Fact]
    public async Task A_read_only_writer_and_an_input_reader_in_another_run_unit_never_meet_a_half_written_path()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        const int Records = 600;   // past one leaf and one internal level of 26-byte entries: splits and reused pages
        string host = Tmp("r2-6");
        try
        {
            Seed(host);
            var writer = new FileRegistry();
            Register(writer, "W", host, FileSharing.ReadOnly);
            writer.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", writer.Status("W"));
            var reader = new FileRegistry();
            Register(reader, "R", host, FileSharing.AllOther, KeyedAccess.Dynamic);
            reader.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal("00", reader.Status("R"));
            var writes = Task.Run(() =>
            {
                for (int n = 3; n < 3 + Records; n++)
                    Assert.Equal("00", Write(writer, "W", n, "WWWW"));
            });
            var statuses = new HashSet<string>();
            int highest = 0;
            while (highest < 2 + Records)
            {
                // The next record past the last one seen: none yet is the invalid key ('23', §14.9.41.4 GR9 c)), and the
                // reader asks again — the writer is still releasing.
                string started = reader.StartRelative("R", ">", highest);
                Assert.True(started is "00" or "23", $"START answered '{started}' beside the READ ONLY writer");
                if (writes.IsFaulted) break;
                if (started == "23") continue;
                string status = ReadNext(reader, "R", out string image);
                statuses.Add(status);
                Assert.True(status is "00" or "10", $"READ NEXT answered '{status}' beside the READ ONLY writer");
                if (status == "00") highest = int.Parse(image[..4], System.Globalization.CultureInfo.InvariantCulture);
                if (writes.IsFaulted) break;
            }
            await writes;
            reader.Close("R");
            writer.Close("W");
            Assert.Contains("00", statuses);
        }
        finally { TryDelete(host); }
    }

    /// <summary>The PB3333 row of 2.2.1.12's tree cases: a READ NEXT that steps across leaves ANOTHER run unit's inserts split
    /// between its reads selects, every time, §14.9.30.4 GR21 c)'s <i>"first existing record in the physical file whose
    /// relative key number is greater than the file position indicator"</i> — in the tree as it is NOW — so the reader sees
    /// every record, its own sequence and the other run unit's, in ascending order; at the edge, AT END ('10').</summary>
    [Fact]
    public void A_sequential_reader_continues_through_a_tree_another_run_unit_splits()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = Tmp("split");
        try
        {
            var seed = new FileRegistry();
            Register(seed, "S", host, sharing: null);
            seed.OpenStatic("S", FileOpenMode.Output);
            for (int n = 1; n < 800; n += 2) Assert.Equal("00", Write(seed, "S", n, "ODD "));
            seed.Close("S");
            var reader = new FileRegistry();
            Register(reader, "R", host, FileSharing.AllOther, KeyedAccess.Sequential);
            reader.OpenStatic("R", FileOpenMode.Input);
            var writer = new FileRegistry();
            Register(writer, "W", host, FileSharing.AllOther);
            writer.OpenStatic("W", FileOpenMode.IO);
            var seen = new List<int>();
            string status;
            while ((status = ReadNext(reader, "R", out string image)) == "00")
            {
                int n = int.Parse(image[..4], System.Globalization.CultureInfo.InvariantCulture);
                seen.Add(n);
                if (n % 2 == 1) Assert.Equal("00", Write(writer, "W", n + 1, "EVEN"));   // just past the reader's position
            }
            Assert.Equal("10", status);
            Assert.Equal(Enumerable.Range(1, 800), seen);
            Assert.Equal("46", ReadNext(reader, "R", out _));   // §9.1.13.7 6 b): "The preceding READ statement ... was unsuccessful"
            reader.Close("R");
            writer.Close("W");
        }
        finally { TryDelete(host); }
    }

    /// <summary>A relative file round-trips through CLOSE and OPEN with gaps (a record in the billions is one leaf entry),
    /// RECORD VARYING frames at their exact lengths (§13.18.43 GR13/GR15), an extent table (D-FRA (v), kb/Work PB1053), and
    /// a CODE-SET (§13.18.13.4 GR6: the payload in the file's character set on the medium, the native one in the record
    /// area; the framing never converted, 2.2.1.5).</summary>
    [Fact]
    public void A_relative_store_round_trips_gaps_varying_frames_extent_tables_and_a_code_set()
    {
        string host = Tmp("roundtrip");
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var ebcdic = new CodeSetConversion(Encoding.GetEncoding(37).GetChars([.. Enumerable.Range(0, 256).Select(b => (byte)b)]));
            var extents = new CobolNet.Runtime.RecordExtents([0, 3], [3, 2]);
            var w = new RelativeConnector(host, 10, KeyedAccess.Random, 10, varyMin: 1, varyMax: 10) { CodeSet = ebcdic };
            Assert.Equal("00", w.Open(FileOpenMode.Output));
            (long Key, string Image, CobolNet.Runtime.RecordExtents? Extents)[] records =
                [(2, "AB", null), (9, "ABCDEFGHIJ", null), (2_000_000_000, "KLMNO", extents)];
            foreach (var (key, image, ext) in records)
            {
                w.SetPendingKey(key);
                Assert.Equal("00", w.Write(image, image.Length, ext));
            }
            Assert.Equal("00", w.Close());
            byte[] file = File.ReadAllBytes(host);
            Assert.True(file.Length <= 16 * PageHeader.PageSize, $"{file.Length} bytes for three records");
            Assert.True(Contains(file, [0xC1, 0xC2, 0xC3, 0xC4]), "the payload is in the CODE-SET (CCSID 37 'ABCD')");
            Assert.False(Contains(file, "ABCDEFGHIJ"u8.ToArray()), "no native image of the record reaches the medium");

            var r = new RelativeConnector(host, 10, KeyedAccess.Random, 10, varyMin: 1, varyMax: 10) { CodeSet = ebcdic };
            Assert.Equal("00", r.Open(FileOpenMode.Input));
            foreach (var (key, image, ext) in records)
            {
                r.SetPendingKey(key);
                Assert.Equal("00", r.ReadRandom(out string got));
                Assert.Equal(image, got[..image.Length]);
                Assert.Equal(image.Length, r.LastReadLength);
                Assert.Equal(ext?.Lengths, r.CurrentRecordExtents?.Lengths);
            }
            r.SetPendingKey(5);
            Assert.Equal("23", r.ReadRandom(out _));                // a gap: §9.1.13.5 3 a)
            Assert.Equal("00", r.Close());
        }
        finally { TryDelete(host); }
    }

    private static bool Contains(byte[] haystack, byte[] needle) =>
        haystack.AsSpan().IndexOf(needle) >= 0;

    /// <summary>A file of the whole-store format (version 5, its magic at offset 0, kb/Work PB1099) is not a paged store:
    /// the RELATIVE OPEN answers §14.9.27.4 GR10's '39' and leaves the file as it was (GR25), exactly as a format-3 file
    /// does (2.2.1.3 decision 1: there is no reader of the whole-store format).</summary>
    [Fact]
    public void A_whole_store_file_is_a_fixed_attribute_conflict_39()
    {
        string host = Tmp("format5");
        try
        {
            var declared = new RelativeConnector(host, 8, KeyedAccess.Random, 4).DeclaredAttributes;
            using (var image = RecordFraming.ComposeStore(declared, [new StoredFrame("0001AAAA", null)], generation: 1, releaseMint: 0))
                File.WriteAllBytes(host, image.ToArray());
            byte[] before = File.ReadAllBytes(host);
            var c = new RelativeConnector(host, 8, KeyedAccess.Random, 4);
            Assert.Equal("39", c.Open(FileOpenMode.IO));
            Assert.Equal(before, File.ReadAllBytes(host));
        }
        finally { TryDelete(host); }
    }

    /// <summary>The PB3333 rows of 2.2.1.12's tree cases through the connector: a WRITE of a present record number is
    /// declined ('22', §14.9.51.4 GR33 a)) and an absent one released ('00'); a READ, REWRITE and DELETE of a key the tree
    /// does not hold answer '23' (§9.1.13.5 3 a)); and a record statement whose tree read meets a TORN page answers '30'
    /// (§9.1.13.6 item 1, 2.2.1.6 R3) with the file unchanged and §9.1.13.1's permanent error in effect until the CLOSE.</summary>
    [Fact]
    public void Present_and_absent_keys_and_a_torn_page_answer_the_general_rules_statuses()
    {
        string host = Tmp("keys");
        try
        {
            var c = new RelativeConnector(host, 8, KeyedAccess.Random, 4);
            Assert.Equal("00", c.Open(FileOpenMode.Output));
            c.SetPendingKey(4);
            Assert.Equal("00", c.Write("0004DDDD"));
            Assert.Equal("22", c.Write("0004XXXX"));
            Assert.Equal("00", c.Close());
            Assert.Equal("00", c.Open(FileOpenMode.IO));
            c.SetPendingKey(6);
            Assert.Equal("23", c.ReadRandom(out _));
            Assert.Equal("23", c.Rewrite("0006FFFF"));
            Assert.Equal("23", c.Delete());
            Assert.Equal("00", c.Close());

            // Tear the record's leaf (the only tree page): the first page past the three meta pages that the tree uses.
            var t = new RelativeConnector(host, 8, KeyedAccess.Random, 4);
            byte[] file = File.ReadAllBytes(host);
            int leaf = Enumerable.Range(3, file.Length / PageHeader.PageSize - 3)
                .Single(p => file[p * PageHeader.PageSize] == (byte)PageKind.Leaf
                    && PageHeader.ChecksumHolds(file.AsSpan(p * PageHeader.PageSize, PageHeader.PageSize))
                    && file.AsSpan(p * PageHeader.PageSize, PageHeader.PageSize).IndexOf("DDDD"u8) >= 0);
            file[leaf * PageHeader.PageSize + 100] ^= 0xFF;
            File.WriteAllBytes(host, file);
            Assert.Equal("00", t.Open(FileOpenMode.IO));
            t.SetPendingKey(4);
            Assert.Equal("30", t.ReadRandom(out _));
            Assert.Equal("30", t.Write("0004XXXX"));                  // replayed
            Assert.Equal("00", t.Close());                             // the CLOSE ends it (DOC-A.1-105)
        }
        finally { TryDelete(host); }
    }
}
