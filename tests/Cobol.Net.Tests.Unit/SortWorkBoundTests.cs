// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SORT OR MERGE KEEPS AT MOST ITS MEMORY LIMIT OF RECORDS IN MEMORY, WHATEVER THE INPUT'S SIZE, AND ORDERS EXACTLY
/// AS IT DID WHEN EVERYTHING FIT (kb/Work PB3339; docs/CONFORMANCE.md §3 D-SWK). Each test runs in its own run unit with
/// the floor limit (64 KiB) and its own work directory, releases many times the limit, and checks three things: the
/// result equals the order the standard prescribes, computed here independently (a stable sort — LINQ's OrderBy is
/// stable — for §14.9.40.4 GR3 a)/b) "The order in which these records are released by an input procedure"; the
/// stream-ordered merge for §14.9.24.4 GR4 a) "Follows the order of the associated input files as specified in the MERGE
/// statement"); the MEASURED peak of the store's resident records never passed the limit while the input was at least
/// eight times it; and after the statement's end the work directory is empty.
/// </summary>
public sealed class SortWorkBoundTests : IDisposable
{
    private const long Limit = SortMemory.MinimumLimitBytes;
    private readonly string _dir = ScratchDirectory.Create("pb3339-sortwork-");

    public void Dispose()
    {
        ScratchDirectory.Remove(_dir);
    }

    private static readonly CobolSort.Key[] AlnumKey = [new(0, 4, false, CobolSort.KeyClass.Alphanumeric, default)];

    /// <summary>A zoned unsigned 4-digit key (USAGE DISPLAY, §8.8.4.2.4 — algebraic, never collated).</summary>
    private static readonly NumProfile Zoned4 = new()
    {
        Digits = 4, FractionDigits = 0, Signed = false, Truncation = NumericTruncation.DigitCount,
        ByteForm = NumericByteForm.Zoned,
    };

    /// <summary>Run <paramref name="body"/> in a fresh run unit whose sort memory is <see cref="Limit"/> in this test's
    /// work directory.</summary>
    private void InRunUnit(Action body) => RunUnit.Run(unit =>
    {
        unit.SortFiles.Memory = new SortMemory(Limit, _dir);
        body();
    });

    private static SortWork WorkOf(string sd) => RunUnit.Current.SortFiles.Get(sd).Work;

    /// <summary>How many of this test's temporary run files the process still holds open. On Windows a run file is
    /// delete-on-close, so an open one is still listed in the work directory; on Linux it is unlinked as soon as it is
    /// created (<c>HostFile.CreateSortWork</c>), so the directory is always empty and an open one is seen only as a
    /// <c>/proc/self/fd</c> link naming it.</summary>
    private int WorkFilesHeldOpen()
    {
        if (!OperatingSystem.IsLinux()) return Directory.EnumerateFileSystemEntries(_dir).Count();
        int held = 0;
        foreach (string fd in Directory.EnumerateFileSystemEntries("/proc/self/fd"))
        {
            string? target;
            try { target = new FileInfo(fd).LinkTarget; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { continue; }   // closed since listed
            if (target?.StartsWith(_dir + Path.DirectorySeparatorChar, StringComparison.Ordinal) == true) held++;
        }
        return held;
    }

    private bool WorkDirectoryIsEmpty() => WorkFilesHeldOpen() == 0;

    /// <summary>The ordinal sequence, except that comparing the two sentinel keys fails the way a work file does
    /// (D-SWK (d)). Each sentinel is released into a different chunk, so no chunk's own sort compares them: only a
    /// merge of two runs does, part-way through writing its output.</summary>
    private sealed class FailingCollation(string a, string b) : CobolCollation
    {
        public const string Message = "injected work-file failure";

        public override int Compare(string? left, string? right) =>
            (left == a && right == b) || (left == b && right == a)
                ? throw new CobolImplementorFatalException(Message)
                : string.CompareOrdinal(left, right);

        public override int Weight(char c) => c;
        public override int PositionCount => char.MaxValue + 1;
        public override int CharAt(long position) => position is >= 0 and <= char.MaxValue ? (int)position : -1;
        public override char HighValue => char.MaxValue;
        public override char LowValue => char.MinValue;
        internal override bool UsesStandardComparison => true;
    }

    /// <summary>A record whose key is <paramref name="key"/>, shaped like <see cref="Records"/>' and above all of them.</summary>
    private static string Sentinel(string key) => $"{key}9999999{new string('x', 24)}";

    private static List<string> Drain(string sd)
    {
        var got = new List<string>();
        while (CobolSort.Return(sd, out var image)) got.Add(image);
        return got;
    }

    /// <summary>Deterministic records: a 4-character key drawn from a small alphabet (so equal keys are common and
    /// stability is observable), then the release ordinal, then filler — about 160 resident bytes each.</summary>
    private static List<string> Records(int count, int seed)
    {
        var rng = new Random(seed);
        var records = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            string key = new([.. Enumerable.Range(0, 4).Select(_ => (char)('A' + rng.Next(6)))]);
            records.Add($"{key}{i:D7}{new string('x', 24)}");
        }
        return records;
    }

    [Fact]
    public void ASortManyTimesTheLimit_OrdersStably_WithinTheLimit_AndLeavesNoFile()
    {
        const string sd = "PB3339-SORT";
        var records = Records(20_000, seed: 1);
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            CobolSort.EnterProcedure(sd, output: false);
            foreach (var r in records) CobolSort.ReleaseStatement(sd, r, 0, r.Length);
            var work = WorkOf(sd);
            CobolSort.Sort(sd, duplicatesInOrder: true);
            CobolSort.EnterProcedure(sd, output: true);
            var got = Drain(sd);
            Assert.Equal(records.OrderBy(r => r[..4], StringComparer.Ordinal).ToList(), got);   // stable: release order on ties
            long input = records.Sum(r => 2L * r.Length);
            Assert.True(input >= 8 * Limit, $"the input ({input} bytes of characters) is not eight times the limit");
            Assert.InRange(work.PeakResidentBytes, 1, Limit);   // ⛔ the measured bound
            Assert.True(work.RunsWritten > 8, $"only {work.RunsWritten} runs were written");
            Assert.True(work.OpenRunFiles > 0, "the disk runs are open until the statement ends");
            CobolSort.Close(sd);
            Assert.Equal(0, work.OpenRunFiles);
        });
        Assert.True(WorkDirectoryIsEmpty(), "a temporary run file outlived its statement");
    }

    [Fact]
    public void EveryGivingFile_ReceivesTheWholeResult_FromDiskRuns()
    {
        const string sd = "PB3339-REWIND";
        var records = Records(8_000, seed: 2);
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            foreach (var r in records) CobolSort.Release(sd, r, r.Length, 0, r.Length);
            CobolSort.Sort(sd, duplicatesInOrder: true);
            CobolSort.Rewind(sd);
            var first = Drain(sd);
            CobolSort.Rewind(sd);   // §14.9.40.4 GR15: the next GIVING file
            var second = Drain(sd);
            Assert.Equal(records.Count, first.Count);
            Assert.Equal(first, second);
            Assert.True(WorkOf(sd).RunsWritten > 1);
            CobolSort.Close(sd);
        });
        Assert.True(WorkDirectoryIsEmpty());
    }

    /// <summary>Every key lane through the runs: a DESCENDING numeric (zoned) key first, then an ascending NATIONAL key
    /// (its UTF-16BE byte pairs, §13.18.60.4 GR8 / D-N1) — the expected order computed from the values, not the bytes.</summary>
    [Fact]
    public void NumericDescendingAndNationalKeys_OrderTheSameThroughRuns()
    {
        const string sd = "PB3339-KEYS";
        CobolSort.Key[] keys =
        [
            new(0, 4, true, CobolSort.KeyClass.Numeric, Zoned4),
            new(4, 4, false, CobolSort.KeyClass.National, default),
        ];
        var rng = new Random(3);
        var rows = Enumerable.Range(0, 12_000).Select(i =>
        {
            int n = rng.Next(40);
            char a = (char)('a' + rng.Next(3)), b = (char)(0x3B1 + rng.Next(3));   // Latin and Greek national characters
            string nat = $"{(char)(a >> 8)}{(char)(a & 0xFF)}{(char)(b >> 8)}{(char)(b & 0xFF)}";
            return (Num: n, Nat: $"{a}{b}", Image: $"{n:D4}{nat}{i:D6}{new string('y', 20)}");
        }).ToList();
        InRunUnit(() =>
        {
            CobolSort.Init(sd, keys);
            foreach (var r in rows) CobolSort.Release(sd, r.Image, r.Image.Length, 0, r.Image.Length);
            CobolSort.Sort(sd, duplicatesInOrder: true);
            var got = Drain(sd);
            var expected = rows.OrderByDescending(r => r.Num).ThenBy(r => r.Nat, StringComparer.Ordinal).Select(r => r.Image);
            Assert.Equal(expected.ToList(), got);
            Assert.True(WorkOf(sd).RunsWritten > 1);
            Assert.InRange(WorkOf(sd).PeakResidentBytes, 1, Limit);
            CobolSort.Close(sd);
        });
    }

    /// <summary>A MERGE of three ordered USING streams many times the limit: equal keys follow the input files' order
    /// (§14.9.24.4 GR4 a)) and each file keeps its own order (GR4 b)), and the streams' runs are deleted at the end.</summary>
    [Fact]
    public void AMergeManyTimesTheLimit_KeepsInputFileOrder_WithinTheLimit()
    {
        const string sd = "PB3339-MERGE";
        var streams = Enumerable.Range(0, 3).Select(s => Records(6_000, seed: 10 + s)
            .Select(r => $"{r[..4]}{s}{r[4..]}").OrderBy(r => r[..4], StringComparer.Ordinal).ToList()).ToList();
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            foreach (var stream in streams)
            {
                CobolSort.NextInput(sd);
                foreach (var r in stream) CobolSort.Release(sd, r, r.Length, 0, r.Length);
            }
            CobolSort.Merge(sd);
            var got = Drain(sd);
            var expected = streams.SelectMany((stream, s) => stream.Select((r, i) => (r, s, i)))
                .OrderBy(t => t.r[..4], StringComparer.Ordinal).ThenBy(t => t.s).ThenBy(t => t.i).Select(t => t.r);
            Assert.Equal(expected.ToList(), got);
            Assert.True(WorkOf(sd).RunsWritten >= 3);
            Assert.InRange(WorkOf(sd).PeakResidentBytes, 1, Limit);
            CobolSort.Close(sd);
        });
        Assert.True(WorkDirectoryIsEmpty());
    }

    /// <summary>§14.9.24.4 GR6 on a stream that was written to its run before the merge: the out-of-order record is
    /// found as it arrives and raised by the MERGE, naming its record number, exactly as for a stream in memory.</summary>
    [Fact]
    public void AnOutOfOrderStreamOnDisk_RaisesSequence_NamingTheRecord()
    {
        const string sd = "PB3339-SEQ";
        var ordered = Records(6_000, seed: 20).OrderBy(r => r[..4], StringComparer.Ordinal).ToList();
        var unordered = new List<string>(ordered);
        unordered.Insert(5_000, "AAAA9999999" + new string('z', 24));   // record 5001 is below its predecessor
        InRunUnit(() =>
        {
            try
            {
                ExceptionState.SortMergeSequenceChecking = true;
                CobolSort.Init(sd, AlnumKey);
                CobolSort.NextInput(sd);
                foreach (var r in ordered) CobolSort.Release(sd, r, r.Length, 0, r.Length);
                CobolSort.NextInput(sd);
                foreach (var r in unordered) CobolSort.Release(sd, r, r.Length, 0, r.Length);
                Assert.True(WorkOf(sd).RunsWritten >= 1);
                var e = Assert.Throws<CobolFatalException>(() => CobolSort.Merge(sd));
                Assert.Equal("EC-SORT-MERGE-SEQUENCE", e.EcName);
                Assert.Contains("record 5001 of USING file 2", e.Message, StringComparison.Ordinal);
            }
            finally
            {
                ExceptionState.SortMergeSequenceChecking = false;
                CobolSort.Close(sd);
            }
        });
        Assert.True(WorkDirectoryIsEmpty());
    }

    /// <summary>D-SWK (b) on a failure: a SORT that fails while a cascade merges two runs into a third (the output
    /// created, the inputs about to leave the run list) still closes every temporary file at the statement's end —
    /// the review's R1-1, where the cascade's output belonged to nothing and stayed open after the close.</summary>
    [Fact]
    public void ASortFailingInACascadeMerge_ClosesEveryWorkFile_AtTheStatementsEnd()
    {
        const string sd = "PB3339-FAIL-SORT";
        string first = Sentinel("ZZZ1"), second = Sentinel("ZZZ2");
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey, new FailingCollation("ZZZ1", "ZZZ2"));
            var work = WorkOf(sd);
            try
            {
                CobolSort.Release(sd, first, first.Length, 0, first.Length);   // into the first chunk
                var e = Assert.Throws<CobolImplementorFatalException>(() =>
                {
                    bool released = false;
                    foreach (var r in Records(20_000, seed: 6))
                    {
                        CobolSort.Release(sd, r, r.Length, 0, r.Length);
                        if (released || work.RunsWritten != 1) continue;
                        CobolSort.Release(sd, second, second.Length, 0, second.Length);   // into the second chunk
                        released = true;
                    }
                });
                Assert.Equal(FailingCollation.Message, e.Message);
                Assert.Equal(3, work.RunsWritten);   // two level-0 runs and the cascade's output, part-written
                Assert.Equal(3, WorkFilesHeldOpen());
            }
            finally
            {
                CobolSort.Close(sd);   // the statement's finally
            }
            Assert.Equal(0, work.OpenRunFiles);
            Assert.Equal(0, WorkFilesHeldOpen());
        });
    }

    /// <summary>D-SWK (b) on a failure in a MERGE's sequence phase: three USING streams on disk, more than one merge
    /// reads (F = 2 at the floor limit), so the last two are merged first — after the streams have left the stream list
    /// — and that merge fails. Every stream's run and the merge's output are still closed at the statement's end.</summary>
    [Fact]
    public void AMergeFailingWhileItMergesStreams_ClosesEveryWorkFile_AtTheStatementsEnd()
    {
        const string sd = "PB3339-FAIL-MERGE";
        var streams = Enumerable.Range(0, 3).Select(s => Records(6_000, seed: 30 + s)
            .OrderBy(r => r[..4], StringComparer.Ordinal).ToList()).ToList();
        streams[1].Add(Sentinel("ZZZ1"));   // each stream stays in KEY order (§14.9.24.4 GR6)
        streams[2].Add(Sentinel("ZZZ2"));
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey, new FailingCollation("ZZZ1", "ZZZ2"));
            var work = WorkOf(sd);
            try
            {
                foreach (var stream in streams)
                {
                    CobolSort.NextInput(sd);
                    foreach (var r in stream) CobolSort.Release(sd, r, r.Length, 0, r.Length);
                }
                Assert.Equal(3, work.OpenRunFiles);   // every stream spilled to its own run
                var e = Assert.Throws<CobolImplementorFatalException>(() => CobolSort.Merge(sd));
                Assert.Equal(FailingCollation.Message, e.Message);
                Assert.Equal(4, work.RunsWritten);   // the three streams and the merge's output, part-written
                Assert.Equal(4, WorkFilesHeldOpen());
            }
            finally
            {
                CobolSort.Close(sd);
            }
            Assert.Equal(0, work.OpenRunFiles);
            Assert.Equal(0, WorkFilesHeldOpen());
        });
    }

    /// <summary>A run file keeps every record exactly: the whole UTF-16 repertoire (HIGH-VALUE U+FFFF, a lone surrogate,
    /// U+0000) and the extent table with its layout OBJECT, which the fixed form of a GIVING file asks
    /// (<c>CobolSort.FillTo</c>; D-FRA (v), (vi)).</summary>
    [Fact]
    public void ARunFile_KeepsTheRepertoireAndTheExtentTable()
    {
        const string sd = "PB3339-FRAME";
        var layout = new CobolContiguousLayout(4, [0], [1], [8L]);
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            for (int i = 0; i < 4_000; i++)
            {
                string image = $"K{i % 7:D3}" + "￿\uD800\u0000" + i.ToString("D6");
                CobolSort.Release(sd, image, image.Length, 0, image.Length,
                    i % 2 == 0 ? new RecordExtents([0], [i % 9], layout) : null);
            }
            CobolSort.Sort(sd, duplicatesInOrder: true);
            Assert.True(WorkOf(sd).RunsWritten > 1);
            int seen = 0;
            while (CobolSort.Return(sd, out var image))
            {
                int i = int.Parse(image[^6..]);
                Assert.Equal($"K{i % 7:D3}" + "￿\uD800\u0000" + i.ToString("D6"), image);
                var extents = CobolSort.LastReturnedExtents(sd);
                if (i % 2 == 0)
                {
                    Assert.NotNull(extents);
                    Assert.Same(layout, extents.Layout);
                    Assert.Equal(new[] { 0 }, extents.FixedAt);
                    Assert.Equal(new[] { i % 9 }, extents.Lengths);
                }
                else Assert.Null(extents);
                seen++;
            }
            Assert.Equal(4_000, seen);
            CobolSort.Close(sd);
        });
    }

    /// <summary>A statement whose records fit writes nothing (no disk cost), and an unwritable work directory is a loud
    /// stop naming it (D-SWK (d)) — never a lost record.</summary>
    [Fact]
    public void AFitPaysNoDisk_AndAMissingWorkDirectoryStopsLoudly()
    {
        string missing = Path.Combine(_dir, "no-such-directory");
        RunUnit.Run(unit =>
        {
            unit.SortFiles.Memory = new SortMemory(Limit, missing);
            CobolSort.Init("PB3339-FIT", AlnumKey);
            foreach (var r in Records(100, seed: 4)) CobolSort.Release("PB3339-FIT", r, r.Length, 0, r.Length);
            CobolSort.Sort("PB3339-FIT", duplicatesInOrder: true);
            Assert.Equal(100, Drain("PB3339-FIT").Count);
            Assert.Equal(0, WorkOf("PB3339-FIT").RunsWritten);
            CobolSort.Close("PB3339-FIT");

            CobolSort.Init("PB3339-NODIR", AlnumKey);
            var e = Assert.Throws<CobolImplementorFatalException>(() =>
            {
                foreach (var r in Records(4_000, seed: 5)) CobolSort.Release("PB3339-NODIR", r, r.Length, 0, r.Length);
            });
            Assert.Contains(missing, e.Message, StringComparison.Ordinal);
            Assert.Contains(SortMemory.DirectoryVariable, e.Message, StringComparison.Ordinal);
            CobolSort.Close("PB3339-NODIR");
        });
    }

    [Theory]
    [InlineData("65536", 65536L)]
    [InlineData("64K", 65536L)]
    [InlineData("128m", 128L * 1024 * 1024)]
    [InlineData(" 2G ", 2L * 1024 * 1024 * 1024)]
    [InlineData("", null)]
    [InlineData("lots", null)]
    [InlineData("-5M", null)]
    [InlineData("99999999999999999999", null)]
    public void TheMemoryVariablesVocabulary(string text, long? expected) => Assert.Equal(expected, SortMemory.ParseSize(text));

    [Fact]
    public void TheVariablesAreRegistered_AndTheLimitHasAFloor()
    {
        Assert.NotNull(RuntimeConfig.Find(SortMemory.MemoryVariable));
        Assert.NotNull(RuntimeConfig.Find(SortMemory.DirectoryVariable));
        Assert.Equal(2, new SortMemory(SortMemory.MinimumLimitBytes, "").FanIn);
        Assert.Equal(16, new SortMemory(SortMemory.DefaultLimitBytes, "").FanIn);
    }
}
