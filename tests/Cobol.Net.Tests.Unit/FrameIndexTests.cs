// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NO READ OF A RECORD VARYING FILE HOLDS ONE OFFSET PER RECORD (kb/Work PB3350;
/// docs/rearchitecture/DESIGN-runtime-library.md 2.2.3). <c>RecordFraming.FrameStarts</c> built a <c>List&lt;long&gt;</c> of
/// every frame's start — kept for the life of the OPEN by a backward READ, and built in full only to be counted by
/// <c>CountRecords</c>. The count is now <c>RecordFraming.CountFrames</c> (stores nothing) and the start of a frame is
/// <see cref="FrameIndex"/>'s answer: a checkpoint per <see cref="FrameIndex.Stride"/> frames and a window of at most
/// <see cref="FrameIndex.Stride"/> exact starts.
/// <para><b>So the guards are the shape, measured structurally and never by a clock:</b> the allocation delta of a count
/// over 2^20 frames is bounded by a constant; the index's checkpoints are ⌈frames / stride⌉ and its window at most the
/// stride; every ordinal's start agrees with the bytes the file holds, whatever order the ordinals are asked in; a
/// backward step inside a span costs no read and a step into another costs one window build (the stream's own read count
/// is the witness); and no member of the sequential I/O layer declares a per-record offset list.</para>
/// </summary>
public sealed class FrameIndexTests
{
    private static byte[] Word(uint w)
    {
        var b = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(b, w);
        return b;
    }

    /// <summary>The bytes of <paramref name="count"/> frames — alternating a plain frame and a one-component extent-table
    /// frame, with payloads of 0 to 6 characters — and the start each begins at.</summary>
    private static (byte[] Bytes, List<long> Starts) MixedFile(int count)
    {
        var bytes = new List<byte>();
        var starts = new List<long>();
        for (int k = 0; k < count; k++)
        {
            starts.Add(bytes.Count);
            string payload = new('x', k % 7);
            if (k % 2 == 0) bytes.AddRange(Word((uint)payload.Length));
            else bytes.AddRange([.. Word(0x80000000u | (uint)payload.Length), .. Word(1), .. Word(0), .. Word((uint)payload.Length)]);
            bytes.AddRange(System.Text.Encoding.Latin1.GetBytes(payload));
        }
        return ([.. bytes], starts);
    }

    /// <summary>A stream that counts the frame heads a walk takes: the one decoder seeks over each frame's payload exactly
    /// once, relative to the position, after its head is read — so the relative seeks ARE the heads read.</summary>
    private sealed class HeadCountingStream(byte[] bytes) : MemoryStream(bytes, writable: true)
    {
        public int Heads { get; set; }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (loc == SeekOrigin.Current) Heads++;
            return base.Seek(offset, loc);
        }
    }

    public static TheoryData<int> Strides() => new(1, 2, 3, 7, 64);

    [Theory]
    [MemberData(nameof(Strides))]
    public void EveryOrdinal_StartsWhereTheFileHoldsIt_InAnyOrder(int stride)
    {
        const int Count = 200;
        var (bytes, starts) = MixedFile(Count);
        using var fs = new MemoryStream(bytes);

        // Forward, backward and a fixed scattered order, each on a fresh index and then on one reused throughout.
        var scattered = Enumerable.Range(0, Count).Select(i => (i * 37 + 11) % Count).ToList();
        foreach (var order in new[] { Enumerable.Range(0, Count), Enumerable.Range(0, Count).Reverse(), scattered })
        {
            var index = new FrameIndex(stride);
            foreach (int n in order)
            {
                Assert.True(index.TryStartOf(fs, n + 1, out long start), $"ordinal {n + 1}");
                Assert.Equal(starts[n], start);
                Assert.True(index.WindowCount <= stride);
            }
            Assert.Equal(Count, index.Frames);
            Assert.Equal((Count + stride - 1) / stride, index.Checkpoints);
        }
    }

    [Theory]
    [MemberData(nameof(Strides))]
    public void NoSuchOrdinal_IsNotAStart(int stride)
    {
        var (bytes, _) = MixedFile(30);
        using var fs = new MemoryStream(bytes);
        var index = new FrameIndex(stride);
        Assert.False(index.TryStartOf(fs, 0, out _));
        Assert.False(index.TryStartOf(fs, -3, out _));
        Assert.False(index.TryStartOf(fs, 31, out _));
        Assert.False(index.TryStartOf(fs, 1_000_000, out _));
        Assert.True(index.TryStartOf(fs, 30, out _));
        Assert.Equal(30, index.Frames);
    }

    [Fact]
    public void AnEmptyFile_HoldsNoRecord()
    {
        using var fs = new MemoryStream();
        var index = new FrameIndex();
        Assert.False(index.TryStartOf(fs, 1, out _));
        Assert.Equal(0, index.Extend(fs));
        Assert.Equal(0, index.Checkpoints);
    }

    [Fact]
    public void FramesAppendedAfterTheWalk_AreFoundWhenATargetReachesThem()
    {
        // §14.9.51.4 sharing: a sibling's appends lie past every walked frame; the walk resumes where it stopped.
        var (bytes, _) = MixedFile(10);
        using var fs = new MemoryStream();
        fs.Write(bytes);
        var index = new FrameIndex(4);
        Assert.Equal(10, index.Extend(fs));
        Assert.False(index.TryStartOf(fs, 11, out _));

        var (more, moreStarts) = MixedFile(3);
        fs.Seek(0, SeekOrigin.End);
        fs.Write(more);
        Assert.True(index.TryStartOf(fs, 12, out long start));
        Assert.Equal(bytes.Length + moreStarts[1], start);
        Assert.Equal(12, index.Frames);
    }

    [Fact]
    public void TheForwardWalk_IndexesTheFileForFree()
    {
        const int Count = 50;
        var (bytes, starts) = MixedFile(Count);
        var index = new FrameIndex(8);
        for (int k = 0; k < Count; k++)
            index.Observe(starts[k], k + 1 < Count ? starts[k + 1] : bytes.Length);
        Assert.Equal(Count, index.Frames);
        Assert.Equal(7, index.Checkpoints);   // frames 0, 8, 16, 24, 32, 40, 48

        // A frame seen again (a re-read after a backward step) or out of order is not a new frame.
        index.Observe(starts[3], starts[4]);
        index.Observe(starts[Count - 1] + 100, starts[Count - 1] + 120);
        Assert.Equal(Count, index.Frames);

        // Every extension was free: the stream is asked only for the one window each target needs.
        using var fs = new HeadCountingStream(bytes);
        Assert.True(index.TryStartOf(fs, 49, out long start));
        Assert.Equal(starts[48], start);
        Assert.True(fs.Heads <= 8 * 2, $"{fs.Heads} heads");
    }

    [Fact]
    public void ABackwardStep_CostsAWindowBuildOnlyWhenItLeavesTheSpan()
    {
        const int Stride = 8;
        var (bytes, starts) = MixedFile(40);
        using var fs = new HeadCountingStream(bytes);
        var index = new FrameIndex(Stride);
        Assert.Equal(40, index.Extend(fs));

        fs.Heads = 0;
        Assert.True(index.TryStartOf(fs, 9, out long start));   // ordinal Stride + 1: the first frame of span 2
        Assert.Equal(starts[8], start);
        Assert.Equal(Stride, fs.Heads);                         // one window build: at most Stride frame heads

        fs.Heads = 0;
        Assert.True(index.TryStartOf(fs, 12, out start));       // the same span: answered from the window
        Assert.True(index.TryStartOf(fs, 10, out _));
        Assert.Equal(starts[11], start);
        Assert.Equal(0, fs.Heads);

        Assert.True(index.TryStartOf(fs, 8, out start));        // the span before: one more window build
        Assert.Equal(starts[7], start);
        Assert.Equal(Stride, fs.Heads);

        fs.Heads = 0;
        Assert.True(index.TryStartOf(fs, 1, out start));        // ordinal 1: the same span, no read
        Assert.Equal(0, start);
        Assert.Equal(0, fs.Heads);
    }

    [Fact]
    public void AllocationAndMemory_DoNotGrowWithTheFile()
    {
        // 2^20 empty-payload frames: 4 MB, one million records.
        const int Frames = 1 << 20;
        var bytes = new byte[4 * Frames];   // every length word is 0: a frame with an empty payload
        using var fs = new MemoryStream(bytes);

        RecordFraming.CountFrames(fs, 0, 4 * 1024);   // JIT warm-up, outside the measurement
        long before = GC.GetAllocatedBytesForCurrentThread();
        long counted = RecordFraming.CountFrames(fs);
        long counting = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(Frames, counted);
        Assert.True(counting < 16 * 1024, $"the count allocated {counting} bytes over {Frames} frames");

        var index = new FrameIndex();
        before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(Frames, index.Extend(fs));
        long indexing = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(Frames / FrameIndex.DefaultStride, index.Checkpoints);   // 256: one long per 4096 records
        Assert.True(indexing < 64 * 1024, $"the index walk allocated {indexing} bytes over {Frames} frames");

        Assert.True(index.TryStartOf(fs, Frames, out long last));
        Assert.Equal(4L * (Frames - 1), last);
        Assert.Equal(FrameIndex.DefaultStride, index.WindowCount);
        Assert.True(index.TryStartOf(fs, 1, out long first));
        Assert.Equal(0, first);
        Assert.Equal(FrameIndex.DefaultStride, index.WindowCount);
    }

    [Fact]
    public void NoMemberOfTheSequentialIoLayer_HoldsAPerRecordOffsetList()
    {
        // The defect's shape: a List<long> (or long[]) sized by the file's RECORDS. The only list of longs the layer may
        // declare is the index's own checkpoint list, one entry per Stride records.
        var offenders = new List<string>();
        foreach (string file in Directory.GetFiles(TestRepo.Src("Cobol.Net.Runtime", "IO"), "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name is not ("RecordFraming.cs" or "SequentialConnector.cs" or "FrameIndex.cs")) continue;
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var generic in root.DescendantNodes().OfType<GenericNameSyntax>())
            {
                if (generic.Identifier.Text != "List") continue;
                if (generic.TypeArgumentList.Arguments is not [PredefinedTypeSyntax { Keyword.Text: "long" }]) continue;
                bool checkpoints = name == "FrameIndex.cs"
                    && generic.Ancestors().OfType<FieldDeclarationSyntax>().Any(f =>
                        f.Declaration.Variables.Any(v => v.Identifier.Text == "_checkpoints"));
                if (!checkpoints) offenders.Add($"{name}: {generic}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A per-record offset list was declared (kb/Work PB3350; DESIGN-runtime-library.md 2.2.3):\n  " + string.Join("\n  ", offenders));
    }
}
