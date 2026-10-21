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
/// section 2.2.1; kb/Work PB3333). The page layer's and the tree's own cases (PB3331, PB3332) run over an in-memory medium; these
/// run the SAME rows through <see cref="RelativeConnector"/> and <see cref="FileRegistry"/> over a real file, with the
/// fault injected under the connector's own handle (<see cref="KeyedStore.MediumObserver"/>):
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
[Trait("Host", "ReFS")]
public sealed class RelativeEngineTests
{
    /// <summary>The two records every case starts from: 0001AAAA and 0002BBBB.</summary>
    private static readonly string[] Seeded = ["AAAA", "BBBB"];

    /// <summary>Every record of the file, read by the next run unit's OPEN INPUT and READ NEXT loop.</summary>
    private static List<string> Records(string host)
    {
        var reader = new FileRegistry();
        FileHarness.Register(reader, "R", host, FileOrg.Relative, sharing: null);
        reader.OpenStatic("R", FileOpenMode.Input);
        Assert.Equal("00", reader.Status("R"));
        var all = new List<string>();
        string status;
        while ((status = FileHarness.ReadNext(reader, "R", out string image)) == "00") all.Add(image);
        Assert.Equal("10", status);   // §14.9.30.4 GR24's at end, never '30': the file is not torn
        reader.Close("R");
        return all;
    }

    public enum Change { Write, Rewrite, Delete }

    private static string Run(FileRegistry reg, Change change) => change switch
    {
        Change.Write => FileHarness.Write(reg, "A", FileOrg.Relative, 3, "CCCC"),
        Change.Rewrite => FileHarness.Rewrite(reg, "A", FileOrg.Relative, 2, "ZZZZ"),
        _ => FileHarness.Delete(reg, "A", FileOrg.Relative, 1),
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
        string host = FileHarness.Tmp("measure");
        try
        {
            FileHarness.Seed(host, FileOrg.Relative, Seeded);
            DyingPageMedium? medium = null;
            KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
            var a = new FileRegistry();
            try
            {
                FileHarness.Register(a, "A", host, FileOrg.Relative, sharing);
                a.OpenStatic("A", FileOpenMode.IO);
            }
            finally { KeyedStore.MediumObserver.Value = null; }
            int before = medium!.Writes;
            Assert.Equal("00", Run(a, change));
            int statement = medium.Writes - before;
            a.Close("A");
            closeWrites = medium.Writes - before - statement;
            return statement;
        }
        finally { FileHarness.TryDelete(host); }
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
            string host = FileHarness.Tmp($"die-{change}-{sharing}-{i}");
            try
            {
                FileHarness.Seed(host, FileOrg.Relative, Seeded);
                DyingPageMedium? medium = null;
                KeyedStore.MediumObserver.Value = inner => medium = new DyingPageMedium(inner);
                var a = new FileRegistry();
                try
                {
                    FileHarness.Register(a, "A", host, FileOrg.Relative, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { KeyedStore.MediumObserver.Value = null; }
                FileRegistry? b = null;
                if (sharing != FileSharing.NoOther)
                {
                    b = new FileRegistry();
                    FileHarness.Register(b, "B", host, FileOrg.Relative, FileSharing.AllOther);
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
                        Assert.Equal("00", FileHarness.Write(b, "B", FileOrg.Relative, 5, "EEEE"));
                        expected.Add("0005EEEE");
                    }
                    else
                    {
                        // The INPUT reader beside a READ ONLY writer follows page 2 and reads the completed file.
                        Assert.Equal(completed && change == Change.Delete ? "23" : "00", FileHarness.Read(b, "B", FileOrg.Relative, 1, out _));
                    }
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
                Assert.Equal(expected, Records(host));
            }
            finally
            {
                KeyedStore.MediumObserver.Value = null;
                FileHarness.TryDelete(host);
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
            string host = FileHarness.Tmp($"refuse-{change}-{sharing}-{i}");
            int refuseAt = int.MaxValue, seen = 0;
            try
            {
                FileHarness.Seed(host, FileOrg.Relative, Seeded);
                KeyedStore.MediumObserver.Value = inner => new RefusingPageMedium(inner,
                    write: _ => seen++ == refuseAt ? new IOException("refused by the host") : null);
                var a = new FileRegistry();
                try
                {
                    FileHarness.Register(a, "A", host, FileOrg.Relative, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { KeyedStore.MediumObserver.Value = null; }
                refuseAt = seen + i;
                Assert.Equal("30", Run(a, change));
                Assert.Equal("30", FileHarness.Write(a, "A", FileOrg.Relative, 7, "GGGG"));                   // replayed, nothing touched
                Assert.Equal("30", FileHarness.Read(a, "A", FileOrg.Relative, 2, out string image));
                Assert.Equal("", image.Trim());
                if (sharing == FileSharing.AllOther)
                {
                    var b = new FileRegistry();
                    FileHarness.Register(b, "B", host, FileOrg.Relative, FileSharing.AllOther);
                    b.OpenStatic("B", FileOpenMode.IO);
                    Assert.Equal("00", FileHarness.Write(b, "B", FileOrg.Relative, 5, "EEEE"));
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
                KeyedStore.MediumObserver.Value = null;
                FileHarness.TryDelete(host);
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
        string host = FileHarness.Tmp("pair");
        try
        {
            FileHarness.Seed(host, FileOrg.Relative, Seeded);
            var a = new FileRegistry();
            FileHarness.Register(a, "A", host, FileOrg.Relative, FileSharing.AllOther);
            a.OpenStatic("A", FileOpenMode.IO);
            Assert.Equal("00", FileHarness.Write(a, "A", FileOrg.Relative, 3, "CCCC"));
            var b = new FileRegistry();                       // a writable OPEN after A's statement: adopts and fences it
            FileHarness.Register(b, "B", host, FileOrg.Relative, FileSharing.AllOther);
            b.OpenStatic("B", FileOpenMode.IO);
            Assert.Equal("00", FileHarness.Read(b, "B", FileOrg.Relative, 3, out string image));
            Assert.Equal("0003CCCC", image);
            Assert.Equal("22", FileHarness.Write(b, "B", FileOrg.Relative, 3, "XXXX"));      // §14.9.51.4 GR33 a): the record A wrote exists
            Assert.Equal("00", FileHarness.Rewrite(b, "B", FileOrg.Relative, 3, "DDDD"));
            Assert.Equal("00", FileHarness.Delete(b, "B", FileOrg.Relative, 1));
            Assert.Equal("00", FileHarness.Read(a, "A", FileOrg.Relative, 3, out image)); // A's entry adopts B's page 2 after B's fence
            Assert.Equal("0003DDDD", image);
            Assert.Equal("23", FileHarness.Read(a, "A", FileOrg.Relative, 1, out _));      // §9.1.13.5 3 a): B deleted it
            Assert.Equal("23", FileHarness.Delete(a, "A", FileOrg.Relative, 1));
            var c = new FileRegistry();                       // an INPUT OPEN beside two writers
            FileHarness.Register(c, "C", host, FileOrg.Relative, FileSharing.AllOther, access: KeyedAccess.Sequential);
            c.OpenStatic("C", FileOpenMode.Input);
            var seen = new List<string>();
            while (FileHarness.ReadNext(c, "C", out string next) == "00")
            {
                seen.Add(next);
                if (seen.Count == 1) Assert.Equal("00", FileHarness.Write(a, "A", FileOrg.Relative, 9, "IIII"));   // released past C's position
            }
            Assert.Equal(["0002BBBB", "0003DDDD", "0009IIII"], seen);
            c.Close("C");
            a.Close("A");
            b.Close("B");
            Assert.Equal(["0002BBBB", "0003DDDD", "0009IIII"], Records(host));
        }
        finally { FileHarness.TryDelete(host); }
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
        string host = FileHarness.Tmp("r2-6");
        try
        {
            FileHarness.Seed(host, FileOrg.Relative, Seeded);
            var writer = new FileRegistry();
            FileHarness.Register(writer, "W", host, FileOrg.Relative, FileSharing.ReadOnly);
            writer.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", writer.Status("W"));
            var reader = new FileRegistry();
            FileHarness.Register(reader, "R", host, FileOrg.Relative, FileSharing.AllOther, access: KeyedAccess.Dynamic);
            reader.OpenStatic("R", FileOpenMode.Input);
            Assert.Equal("00", reader.Status("R"));
            var writes = Task.Run(() =>
            {
                for (int n = 3; n < 3 + Records; n++)
                    Assert.Equal("00", FileHarness.Write(writer, "W", FileOrg.Relative, n, "WWWW"));
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
                string status = FileHarness.ReadNext(reader, "R", out string image);
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
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The PB3333 row of 2.2.1.12's tree cases: a READ NEXT that steps across leaves ANOTHER run unit's inserts split
    /// between its reads selects, every time, §14.9.30.4 GR21 c)'s <i>"first existing record in the physical file whose
    /// relative key number is greater than the file position indicator"</i> — in the tree as it is NOW — so the reader sees
    /// every record, its own sequence and the other run unit's, in ascending order; at the edge, AT END ('10').</summary>
    [Fact]
    public void A_sequential_reader_continues_through_a_tree_another_run_unit_splits()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = FileHarness.Tmp("split");
        try
        {
            var seed = new FileRegistry();
            FileHarness.Register(seed, "S", host, FileOrg.Relative, sharing: null);
            seed.OpenStatic("S", FileOpenMode.Output);
            for (int n = 1; n < 800; n += 2) Assert.Equal("00", FileHarness.Write(seed, "S", FileOrg.Relative, n, "ODD "));
            seed.Close("S");
            var reader = new FileRegistry();
            FileHarness.Register(reader, "R", host, FileOrg.Relative, FileSharing.AllOther, access: KeyedAccess.Sequential);
            reader.OpenStatic("R", FileOpenMode.Input);
            var writer = new FileRegistry();
            FileHarness.Register(writer, "W", host, FileOrg.Relative, FileSharing.AllOther);
            writer.OpenStatic("W", FileOpenMode.IO);
            var seen = new List<int>();
            string status;
            while ((status = FileHarness.ReadNext(reader, "R", out string image)) == "00")
            {
                int n = int.Parse(image[..4], System.Globalization.CultureInfo.InvariantCulture);
                seen.Add(n);
                if (n % 2 == 1) Assert.Equal("00", FileHarness.Write(writer, "W", FileOrg.Relative, n + 1, "EVEN"));   // just past the reader's position
            }
            Assert.Equal("10", status);
            Assert.Equal(Enumerable.Range(1, 800), seen);
            Assert.Equal("46", FileHarness.ReadNext(reader, "R", out _));   // §9.1.13.7 6 b): "The preceding READ statement ... was unsuccessful"
            reader.Close("R");
            writer.Close("W");
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>A relative file round-trips through CLOSE and OPEN with gaps (a record in the billions is one leaf entry),
    /// RECORD VARYING frames at their exact lengths (§13.18.43 GR13/GR15), an extent table (D-FRA (v), kb/Work PB1053), and
    /// a CODE-SET (§13.18.13.4 GR6: the payload in the file's character set on the medium, the native one in the record
    /// area; the framing never converted, 2.2.1.5).</summary>
    [Fact]
    public void A_relative_store_round_trips_gaps_varying_frames_extent_tables_and_a_code_set()
    {
        string host = FileHarness.Tmp("roundtrip");
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
        finally { FileHarness.TryDelete(host); }
    }

    private static bool Contains(byte[] haystack, byte[] needle) =>
        haystack.AsSpan().IndexOf(needle) >= 0;

    /// <summary>A file of the whole-store format (version 5, its magic at offset 0, kb/Work PB1099) is not a paged store:
    /// the RELATIVE OPEN answers §14.9.27.4 GR10's '39' and leaves the file as it was (GR25), exactly as a format-3 file
    /// does (2.2.1.3 decision 1: there is no reader of the whole-store format).</summary>
    [Fact]
    public void A_whole_store_file_is_a_fixed_attribute_conflict_39()
    {
        string host = FileHarness.Tmp("format5");
        try
        {
            File.WriteAllBytes(host, WholeStoreFile.Bytes('R', 5, "0001AAAA"));
            byte[] before = File.ReadAllBytes(host);
            var c = new RelativeConnector(host, 8, KeyedAccess.Random, 4);
            Assert.Equal("39", c.Open(FileOpenMode.IO));
            Assert.Equal(before, File.ReadAllBytes(host));
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The PB3333 rows of 2.2.1.12's tree cases through the connector: a WRITE of a present record number is
    /// declined ('22', §14.9.51.4 GR33 a)) and an absent one released ('00'); a READ, REWRITE and DELETE of a key the tree
    /// does not hold answer '23' (§9.1.13.5 3 a)); and a record statement whose tree read meets a TORN page answers '30'
    /// (§9.1.13.6 item 1, 2.2.1.6 R3) with the file unchanged and §9.1.13.1's permanent error in effect until the CLOSE.</summary>
    [Fact]
    public void Present_and_absent_keys_and_a_torn_page_answer_the_general_rules_statuses()
    {
        string host = FileHarness.Tmp("keys");
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
        finally { FileHarness.TryDelete(host); }
    }

    // ── 2.2.1.16's OPEN and CLOSE rows through the connector (review finding R1-4) ───────────────────────────────────

    public static TheoryData<FileSharing> Sharings() => [FileSharing.AllOther, FileSharing.ReadOnly, FileSharing.NoOther];

    /// <summary>2.2.1.16's refused table, "A promotion (CLOSE ...)" before and after the slot write, through the connector:
    /// a host refusal of EVERY write and sync of the CLOSE that follows a completed WRITE answers the CLOSE '30' (§9.1.13.6
    /// item 1: <i>"A permanent error exists and no further information is available concerning the input-output
    /// operation"</i>) and leaves the connector closed — its next OPEN succeeds — and the WRITE's record is in the file,
    /// since page 2 already named it (2.2.1.6). Under ALL OTHER, another run unit open beside it runs its next statement
    /// ('00'), whose entry runs the promotion again.</summary>
    [Theory]
    [MemberData(nameof(Sharings))]
    public void A_refusal_at_every_write_and_sync_of_the_CLOSE_is_30_and_keeps_the_completed_statement(FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int closeOps = CloseOperationsAfterAWrite(sharing);
        Assert.True(closeOps >= 3, $"a promotion writes its claim and its slot and syncs ({closeOps})");
        for (int k = 0; k < closeOps; k++)
        {
            string host = FileHarness.Tmp($"refuse-close-{sharing}-{k}");
            int refuseAt = int.MaxValue, seen = 0;
            try
            {
                FileHarness.Seed(host, FileOrg.Relative, Seeded);
                RelativeStore.MediumObserver.Value = inner => RefusingPageMedium.AtOperation(inner, () => refuseAt, () => seen++, () => seen);
                var a = new FileRegistry();
                try
                {
                    FileHarness.Register(a, "A", host, FileOrg.Relative, sharing);
                    a.OpenStatic("A", FileOpenMode.IO);
                    Assert.Equal("00", a.Status("A"));
                }
                finally { RelativeStore.MediumObserver.Value = null; }
                FileRegistry? b = null;
                if (sharing == FileSharing.AllOther)
                {
                    b = new FileRegistry();
                    FileHarness.Register(b, "B", host, FileOrg.Relative, FileSharing.AllOther);
                    b.OpenStatic("B", FileOpenMode.IO);
                    Assert.Equal("00", b.Status("B"));
                }
                Assert.Equal("00", FileHarness.Write(a, "A", FileOrg.Relative, 3, "CCCC"));
                refuseAt = seen + k;
                a.Close("A");
                Assert.Equal("30", a.Status("A"));
                refuseAt = int.MaxValue;
                var expected = After(Change.Write).ToList();
                if (b is not null)
                {
                    Assert.Equal("00", FileHarness.Write(b, "B", FileOrg.Relative, 5, "EEEE"));
                    expected.Add("0005EEEE");
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
                a.OpenStatic("A", FileOpenMode.IO);                  // the CLOSE left the connector closed
                Assert.Equal("00", a.Status("A"));
                Assert.Equal("00", FileHarness.Read(a, "A", FileOrg.Relative, 3, out string image));
                Assert.Equal("0003CCCC", image);
                a.Close("A");
                Assert.Equal("00", a.Status("A"));
                Assert.Equal(expected, Records(host));
            }
            finally
            {
                RelativeStore.MediumObserver.Value = null;
                FileHarness.TryDelete(host);
            }
        }
    }

    /// <summary>The writes and syncs of a CLOSE after one completed WRITE, measured on a run that is not refused.</summary>
    private static int CloseOperationsAfterAWrite(FileSharing sharing)
    {
        string host = FileHarness.Tmp("measure-close");
        try
        {
            FileHarness.Seed(host, FileOrg.Relative, Seeded);
            int seen = 0;
            RelativeStore.MediumObserver.Value = inner => RefusingPageMedium.AtOperation(inner, () => -1, () => seen++, () => seen);
            var a = new FileRegistry();
            try
            {
                FileHarness.Register(a, "A", host, FileOrg.Relative, sharing);
                a.OpenStatic("A", FileOpenMode.IO);
            }
            finally { RelativeStore.MediumObserver.Value = null; }
            Assert.Equal("00", FileHarness.Write(a, "A", FileOrg.Relative, 3, "CCCC"));
            int before = seen;
            a.Close("A");
            Assert.Equal("00", a.Status("A"));
            return seen - before;
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The writes and syncs an OPEN hands over, measured on a run that is not refused: the fence of an
    /// <c>OPEN I-O</c> of the seeded file, or an <c>OPEN OUTPUT</c>'s creation over it.</summary>
    private static int OpenOperations(FileOpenMode mode, FileSharing sharing)
    {
        string host = FileHarness.Tmp("measure-open");
        try
        {
            FileHarness.Seed(host, FileOrg.Relative, Seeded);
            int seen = 0;
            RelativeStore.MediumObserver.Value = inner => RefusingPageMedium.AtOperation(inner, () => -1, () => seen++, () => seen);
            var a = new FileRegistry();
            try
            {
                FileHarness.Register(a, "A", host, FileOrg.Relative, sharing, access: FileHarness.AccessFor(mode));   // §14.9.27.3 SR2 (kb/Work PB3479)
                FileHarness.Open(a, "A", mode);
                Assert.Equal("00", a.Status("A"));
            }
            finally { RelativeStore.MediumObserver.Value = null; }
            int opened = seen;
            a.Close("A");
            return opened;
        }
        finally { FileHarness.TryDelete(host); }
    }

    public static TheoryData<FileOpenMode, FileSharing> Opens()
    {
        var data = new TheoryData<FileOpenMode, FileSharing>();
        foreach (var mode in new[] { FileOpenMode.IO, FileOpenMode.Extend, FileOpenMode.Output })
            foreach (var sharing in new[] { FileSharing.AllOther, FileSharing.NoOther })
                data.Add(mode, sharing);
        return data;
    }

    /// <summary>The records the next OPEN reads after an OPEN of the seeded file that a process death cut short: the fence
    /// of an <c>OPEN I-O</c> or <c>OPEN EXTEND</c> changes no record (2.2.1.16, "The fence"), so the seeded records; an
    /// <c>OPEN OUTPUT</c> whose creation's commit write — its FIRST write, the empty meta into the slot not holding the
    /// previous committed meta (PagedFile.Create) — was handed over holds none (2.2.1.16's creation rows: §14.9.27.4
    /// GR18's file <i>"contains no records"</i>), and one that died before it left the seeded store.</summary>
    private static string[] AfterAnUnfinishedOpen(FileOpenMode mode, bool committed) =>
        mode == FileOpenMode.Output && committed ? [] : Before;

    /// <summary>2.2.1.16's refused table, "The fence (R6 step 4), at any point" and the two <c>OPEN OUTPUT</c> rows (at any
    /// write or sync but the last sync; at its last sync), through the connector: a host refusal of EVERY write and sync
    /// of the OPEN answers the OPEN '30' (§9.1.13.6
    /// item 1) and leaves the connector closed (<see cref="FileConnector.AbandonOpen"/> detaches the store and gives the
    /// handle back), so the SAME connector's next OPEN succeeds and reads the file; another run unit's OPEN succeeds too.
    /// Neither reads a record the unfinished OPEN invented or misses one it could not have removed.</summary>
    [Theory]
    [MemberData(nameof(Opens))]
    public void A_refusal_at_every_write_and_sync_of_the_OPEN_is_30_and_leaves_the_connector_closed(FileOpenMode mode, FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int openOps = OpenOperations(mode, sharing);
        Assert.True(openOps >= 2, $"an OPEN through a writable handle writes and syncs ({openOps})");
        for (int k = 0; k < openOps; k++)
        {
            string host = FileHarness.Tmp($"refuse-open-{mode}-{sharing}-{k}");
            int refuseAt = k, seen = 0;
            try
            {
                FileHarness.Seed(host, FileOrg.Relative, Seeded);
                var a = new FileRegistry();
                RelativeStore.MediumObserver.Value = inner => RefusingPageMedium.AtOperation(inner, () => refuseAt, () => seen++, () => seen);
                try
                {
                    FileHarness.Register(a, "A", host, FileOrg.Relative, sharing, access: FileHarness.AccessFor(mode));   // §14.9.27.3 SR2 (kb/Work PB3479)
                    FileHarness.Open(a, "A", mode);
                    Assert.Equal("30", a.Status("A"));
                }
                finally { RelativeStore.MediumObserver.Value = null; }
                // The creation is §14.9.27.4 GR25's unsuccessful OPEN, "the file is not affected": a refusal at any of its
                // writes or syncs gives the file its previous bytes back (PagedFile.Create's undo), so the seeded records
                // survive; only a refused FINAL sync, after the truncation, leaves the created empty store (2.2.1.16's
                // refused OPEN OUTPUT rows). A fence changes no record either way.
                List<string> expected = mode == FileOpenMode.Output && k == openOps - 1 ? [] : [.. Before];
                if (sharing == FileSharing.AllOther)
                {
                    var b = new FileRegistry();
                    FileHarness.Register(b, "B", host, FileOrg.Relative, FileSharing.AllOther);
                    b.OpenStatic("B", FileOpenMode.IO);
                    Assert.Equal("00", b.Status("B"));
                    Assert.Equal("00", FileHarness.Write(b, "B", FileOrg.Relative, 5, "EEEE"));
                    expected.Add("0005EEEE");
                    b.Close("B");
                    Assert.Equal("00", b.Status("B"));
                }
                a.OpenStatic("A", FileOpenMode.IO);                  // the refused OPEN left the connector closed
                Assert.Equal("00", a.Status("A"));
                a.Close("A");
                Assert.Equal("00", a.Status("A"));
                Assert.Equal(expected, Records(host));
            }
            finally
            {
                RelativeStore.MediumObserver.Value = null;
                FileHarness.TryDelete(host);
            }
        }
    }

    /// <summary>2.2.1.16's crash points of an OPEN through the connector: a process death after EVERY write of the fence
    /// of an <c>OPEN I-O</c> or <c>OPEN EXTEND</c>, or of an <c>OPEN OUTPUT</c>'s creation, leaves a file the next run
    /// unit's OPEN adopts ('00'), with the records the table's rows name: the seeded ones after a fence (it changes no
    /// record), none after a creation whose commit write was handed over, and the seeded ones after a creation that died
    /// before it.</summary>
    [Theory]
    [MemberData(nameof(Opens))]
    public void A_death_after_every_write_of_the_OPEN_leaves_a_file_the_next_OPEN_adopts(FileOpenMode mode, FileSharing sharing)
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        int openOps = OpenOperations(mode, sharing);
        for (int i = 0; i <= openOps; i++)
        {
            string host = FileHarness.Tmp($"die-open-{mode}-{sharing}-{i}");
            try
            {
                FileHarness.Seed(host, FileOrg.Relative, Seeded);
                var a = new FileRegistry();
                int dieAfter = i;
                RelativeStore.MediumObserver.Value = inner => new DyingPageMedium(inner) { DieAfter = dieAfter };
                try
                {
                    FileHarness.Register(a, "A", host, FileOrg.Relative, sharing, access: FileHarness.AccessFor(mode));   // §14.9.27.3 SR2 (kb/Work PB3479)
                    FileHarness.Open(a, "A", mode);   // what the dying run unit is told is not asked
                }
                finally { RelativeStore.MediumObserver.Value = null; }
                a.CloseIfOpen("A");           // its handle goes with it; none of its writes after the death land
                // A creation that died at write 0 never handed over its commit write; from write 1 on it had.
                Assert.Equal(AfterAnUnfinishedOpen(mode, committed: i > 0), Records(host));
            }
            finally
            {
                RelativeStore.MediumObserver.Value = null;
                FileHarness.TryDelete(host);
            }
        }
    }

    /// <summary>⛔ REVIEW FINDING R1-2 OF kb/Work PB3333 — ONLY AN EXCLUSIVE ENTRY WRITES (2.2.1.7). Run unit X holds the
    /// file open twice, INPUT and I-O; run unit Y's CLOSE is refused after its claim, so page 2 carries PROMOTING (2.2.1.16's
    /// refused table, "A promotion ... before the slot write"). X's INPUT connector holds the store mutex SHARED (a read-only
    /// handle cannot hold it exclusive) and so must not resolve the flag — two run units' readers doing so at once would
    /// claim from the same mints and write the same slot and page 2 interleaved, breaking R1 — yet it reads the record Y's
    /// completed WRITE left ('00'). X's I-O connector's next statement, under its EXCLUSIVE hold, resolves it; the file then
    /// holds every completed statement's record.</summary>
    [Fact]
    public void An_input_connector_beside_its_writable_sibling_reads_a_promoting_page_2_and_writes_nothing()
    {
        using var forced = RegionHostSeam.Use(RegionHostKind.OfThisProcess);
        string host = FileHarness.Tmp("r1-2");
        try
        {
            FileHarness.Seed(host, FileOrg.Relative, Seeded);
            int xWrites = 0;
            var x = new FileRegistry();
            RelativeStore.MediumObserver.Value = inner => new RefusingPageMedium(inner, write: _ => { xWrites++; return null; });
            try
            {
                FileHarness.Register(x, "XI", host, FileOrg.Relative, FileSharing.AllOther, access: KeyedAccess.Random);
                x.OpenStatic("XI", FileOpenMode.Input);
                Assert.Equal("00", x.Status("XI"));
                FileHarness.Register(x, "XO", host, FileOrg.Relative, FileSharing.AllOther);
                x.OpenStatic("XO", FileOpenMode.IO);
                Assert.Equal("00", x.Status("XO"));
            }
            finally { RelativeStore.MediumObserver.Value = null; }

            int refuseAt = int.MaxValue, seen = 0;
            var y = new FileRegistry();
            RelativeStore.MediumObserver.Value = inner => RefusingPageMedium.AtOperation(inner, () => refuseAt, () => seen++, () => seen);
            try
            {
                FileHarness.Register(y, "Y", host, FileOrg.Relative, FileSharing.AllOther);
                y.OpenStatic("Y", FileOpenMode.IO);
                Assert.Equal("00", y.Status("Y"));
            }
            finally { RelativeStore.MediumObserver.Value = null; }
            Assert.Equal("00", FileHarness.Write(y, "Y", FileOrg.Relative, 3, "CCCC"));
            // The CLOSE's promotion: its claim (page 2 with PROMOTING), sync 1, then the slot write — refused.
            refuseAt = seen + 2;
            y.Close("Y");
            Assert.Equal("30", y.Status("Y"));
            Assert.True(StoreKit.PageTwoPromoting(host), "the refused promotion left PROMOTING on page 2");

            int before = xWrites;
            Assert.Equal("00", FileHarness.Read(x, "XI", FileOrg.Relative, 3, out string image));
            Assert.Equal("0003CCCC", image);
            Assert.Equal(before, xWrites);                        // the shared entry wrote nothing
            Assert.True(StoreKit.PageTwoPromoting(host), "the INPUT connector left the flag to an exclusive entry");

            Assert.Equal("00", FileHarness.Read(x, "XO", FileOrg.Relative, 3, out image));   // the exclusive entry resolves it (R5's restart rule)
            Assert.Equal("0003CCCC", image);
            Assert.True(xWrites > before, "the I-O connector's entry ran the promotion again");
            Assert.False(StoreKit.PageTwoPromoting(host), "the promotion completed");
            x.Close("XI");
            x.Close("XO");
            Assert.Equal("00", x.Status("XO"));
            Assert.Equal(After(Change.Write), Records(host));
        }
        finally
        {
            RelativeStore.MediumObserver.Value = null;
            FileHarness.TryDelete(host);
        }
    }
}
