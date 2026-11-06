// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SORT RECORD'S MANAGED SLOTS TRAVEL WITH ITS CHARACTERS (kb/Work PB4772). ISO §14.9.32.4 GR2: "The execution of a
/// RELEASE statement causes the record named by record-name-1 to be released to the initial phase of a sort operation",
/// and §14.9.34.4 GR3: the RETURN makes "the next existing record ... in the record area". A strongly-typed record's
/// message-tag, object-reference or pointer leaf (§13.18.60.3 SR14) has no byte form, so <c>CobolSort.ReleaseStatement</c>
/// takes its leaf vector beside the image and <c>CobolSort.LastReturnedSlots</c> gives it back with the record the
/// RETURN delivers. The slots stay in memory while the characters spill into run files, so the cases here are the ones
/// a store that holds only strings gets wrong: a record far past the memory limit, a mix of records with and without
/// slots, a record that is released with an extent table and slots at once, and the statements that must NOT carry —
/// a RELEASE the phase test or the record-range test refuses, a failing merge, and the next statement on the same file.
/// Expected values are the records themselves: the vector released with a record is the vector returned with it.
/// </summary>
public sealed class SortManagedSlotTests : IDisposable
{
    private const long Limit = SortMemory.MinimumLimitBytes;
    private readonly string _dir = ScratchDirectory.Create("pb4772-sortslots-");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private static readonly CobolSort.Key[] AlnumKey = [new(0, 4, false, CobolSort.KeyClass.Alphanumeric, default)];

    private void InRunUnit(Action body) => RunUnit.Run(unit =>
    {
        unit.SortFiles.Memory = new SortMemory(Limit, _dir);
        body();
    });

    private static SortWork WorkOf(string sd) => RunUnit.Current.SortFiles.Get(sd).Work;

    /// <summary>A record: a 4-character key from a small alphabet (equal keys are common), the release ordinal, filler.</summary>
    private static string Image(int ordinal, int seed) =>
        $"{(char)('A' + new Random(seed + ordinal).Next(6))}{(char)('A' + (ordinal % 5))}{(char)('A' + (ordinal % 3))}{(char)('A' + (ordinal % 2))}"
        + $"{ordinal:D7}{new string('x', 24)}";

    /// <summary>The vector a record is released with: its ordinal and a reference the test can compare by identity.</summary>
    private static object?[] SlotsOf(int ordinal) => [ordinal, $"slot-{ordinal}"];

    private static int OrdinalOf(string image) => int.Parse(image.AsSpan(4, 7));

    [Fact]
    public void SlotsFollowTheirRecords_ThroughDiskRuns_InKeyOrder()
    {
        const string sd = "PB4772-SPILL";
        const int count = 20_000;
        var images = Enumerable.Range(0, count).Select(i => Image(i, seed: 7)).ToList();
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            CobolSort.EnterProcedure(sd, output: false);
            for (int i = 0; i < count; i++)
                CobolSort.ReleaseStatement(sd, images[i], 0, images[i].Length,
                    slots: i % 4 == 3 ? null : SlotsOf(i));   // every fourth record holds no slot
            var work = WorkOf(sd);
            CobolSort.Sort(sd, duplicatesInOrder: true);
            Assert.True(work.RunsWritten > 8, $"only {work.RunsWritten} runs were written: the records did not spill");
            CobolSort.EnterProcedure(sd, output: true);
            var expected = images.Select((img, i) => (img, i)).OrderBy(p => p.img[..4], StringComparer.Ordinal).ToList();
            foreach (var (img, i) in expected)
            {
                Assert.True(CobolSort.Return(sd, out var got));
                Assert.Equal(img, got);
                Assert.Equal(OrdinalOf(got), i);
                var slots = CobolSort.LastReturnedSlots(sd);
                if (i % 4 == 3) Assert.Null(slots);
                else
                {
                    Assert.NotNull(slots);
                    Assert.Equal(SlotsOf(i), slots);   // the vector this record was released with, not a neighbour's
                }
            }
            Assert.False(CobolSort.Return(sd, out _));
            Assert.Null(CobolSort.LastReturnedSlots(sd));   // at end: nothing is delivered
            CobolSort.Close(sd);
        });
    }

    /// <summary>The run-file head carries an extent table and a slot ticket together: both come back with the record.</summary>
    [Fact]
    public void ARecordWithAnExtentTableAndSlots_KeepsBothThroughARunFile()
    {
        const string sd = "PB4772-EXTENTS";
        var layout = new CobolContiguousLayout(4, [0], [1], [8L]);
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            CobolSort.EnterProcedure(sd, output: false);
            for (int i = 0; i < 4_000; i++)
            {
                string image = Image(i, seed: 3);
                CobolSort.ReleaseStatement(sd, image, 0, image.Length, extents: new RecordExtents([0], [i % 9], layout), slots: SlotsOf(i));
            }
            CobolSort.Sort(sd, duplicatesInOrder: true);
            CobolSort.EnterProcedure(sd, output: true);
            int returned = 0;
            while (CobolSort.Return(sd, out var image))
            {
                returned++;
                int ordinal = OrdinalOf(image);
                Assert.Equal(SlotsOf(ordinal), CobolSort.LastReturnedSlots(sd));
                Assert.Equal(ordinal % 9, CobolSort.LastReturnedExtents(sd)!.Lengths[0]);
            }
            Assert.Equal(4_000, returned);
            CobolSort.Close(sd);
        });
    }

    /// <summary>§14.9.32.4 GR1: a RELEASE outside an input procedure is EC-FLOW-RELEASE and releases nothing; with the
    /// checking on, the raise precedes the release, so the slots it was handed are never kept and the records that follow
    /// still return their own.</summary>
    [Fact]
    public void AReleaseTheStatementRefuses_CarriesNothing()
    {
        const string sd = "PB4772-REFUSED";
        string a = Image(1, 0), b = Image(2, 0);
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            try
            {
                ExceptionState.FlowReleaseChecking = true;
                // The store's phase is None here (no input procedure is executing), so GR1 raises.
                Assert.Equal("EC-FLOW-RELEASE",
                    Assert.Throws<CobolFatalException>(() => CobolSort.ReleaseStatement(sd, a, 0, a.Length, slots: SlotsOf(1))).EcName);
                CobolSort.EnterProcedure(sd, output: false);
                // §13.18.43.4 GR14 b): a size outside the record range is EC-SORT-MERGE-RELEASE, and it releases nothing.
                ExceptionState.SortMergeReleaseChecking = true;
                Assert.Equal("EC-SORT-MERGE-RELEASE",
                    Assert.Throws<CobolFatalException>(() => CobolSort.ReleaseStatement(sd, a, 0, a.Length - 1, slots: SlotsOf(1))).EcName);
            }
            finally
            {
                ExceptionState.FlowReleaseChecking = false;
                ExceptionState.SortMergeReleaseChecking = false;
            }
            Assert.True(CobolSort.ReleaseStatement(sd, b, 0, b.Length, slots: SlotsOf(2)));
            CobolSort.Sort(sd, duplicatesInOrder: true);
            CobolSort.EnterProcedure(sd, output: true);
            Assert.True(CobolSort.Return(sd, out var got));
            Assert.Equal(b, got);
            Assert.Equal(SlotsOf(2), CobolSort.LastReturnedSlots(sd));   // the refused records' vectors were never kept
            Assert.False(CobolSort.Return(sd, out _));
            CobolSort.Close(sd);
        });
    }

    /// <summary>The next statement on the same sort-merge file begins with no slot of the last one's: Init replaces the
    /// work, and the delivered vector is cleared with it.</summary>
    [Fact]
    public void TheNextStatementOnTheFile_InheritsNoSlots()
    {
        const string sd = "PB4772-NEXT";
        string a = Image(1, 0);
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey);
            CobolSort.EnterProcedure(sd, output: false);
            CobolSort.ReleaseStatement(sd, a, 0, a.Length, slots: SlotsOf(1));
            CobolSort.Sort(sd, duplicatesInOrder: true);
            CobolSort.EnterProcedure(sd, output: true);
            Assert.True(CobolSort.Return(sd, out _));
            Assert.NotNull(CobolSort.LastReturnedSlots(sd));
            CobolSort.Init(sd, AlnumKey);   // the second SORT on the file
            Assert.Null(CobolSort.LastReturnedSlots(sd));
            CobolSort.EnterProcedure(sd, output: false);
            CobolSort.ReleaseStatement(sd, a, 0, a.Length);   // a record with no slot
            CobolSort.Sort(sd, duplicatesInOrder: true);
            CobolSort.EnterProcedure(sd, output: true);
            Assert.True(CobolSort.Return(sd, out _));
            Assert.Null(CobolSort.LastReturnedSlots(sd));
            CobolSort.Close(sd);
        });
    }

    /// <summary>The ordinal sequence, except that comparing the two sentinel keys fails the way a work file does.</summary>
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

    /// <summary>D-SWK (b): a work-file failure part-way through a cascade merge of slot-carrying records ends the statement
    /// with every run file closed and the slot list dropped with the work — the same statement-end guarantee records
    /// with no slot have.</summary>
    [Fact]
    public void AFailingMerge_OfSlotRecords_ClosesEveryRunFile_AtTheStatementsEnd()
    {
        const string sd = "PB4772-FAIL";
        string Sentinel(string key) => $"{key}9999999{new string('x', 24)}";
        string first = Sentinel("ZZZ1"), second = Sentinel("ZZZ2");
        InRunUnit(() =>
        {
            CobolSort.Init(sd, AlnumKey, new FailingCollation("ZZZ1", "ZZZ2"));
            var work = WorkOf(sd);
            try
            {
                CobolSort.EnterProcedure(sd, output: false);
                CobolSort.ReleaseStatement(sd, first, 0, first.Length, slots: SlotsOf(-1));
                var e = Assert.Throws<CobolImplementorFatalException>(() =>
                {
                    bool released = false;
                    for (int i = 0; i < 20_000; i++)
                    {
                        string image = Image(i, seed: 6);
                        CobolSort.ReleaseStatement(sd, image, 0, image.Length, slots: SlotsOf(i));
                        if (released || work.RunsWritten != 1) continue;
                        CobolSort.ReleaseStatement(sd, second, 0, second.Length, slots: SlotsOf(-2));
                        released = true;
                    }
                });
                Assert.Equal(FailingCollation.Message, e.Message);
            }
            finally
            {
                CobolSort.Close(sd);
            }
            Assert.Equal(0, work.OpenRunFiles);
        });
    }
}
