// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The UNSTRING kernel's two calls (ISO/IEC 1989:2023 §14.9.48.4): <see cref="CobolStringOps.UnstringExtract"/>
/// examines ONE receiving area and consumes ONE occurrence of the matched delimiter (GR7, GR9–GR11, GR13), and
/// <see cref="CobolStringOps.UnstringSkipRun"/> consumes the contiguous repeats of an ALL delimiter (GR7) over the
/// delimiter and sender the caller hands it — the emitter runs it after the receiving area's INTO store
/// (docs/CONFORMANCE.md D-UNS2, kb/Work PB1909). Expected values are read from the rules, not from an oracle.
/// </summary>
public sealed class UnstringKernelTests
{
    [Fact]
    public void Extract_ConsumesOneOccurrence_AndNamesTheMatchedDelimiter()
    {
        long pointer = 1;
        // GR10: the earliest match wins, a tie goes to the first listed; GR13: the pointer passes ONE occurrence.
        int count = CobolStringOps.UnstringExtract("ab;;cd", [",", ";"], 0, ref pointer, out string field, out string delimiter, out int matched);
        Assert.Equal(2, count);
        Assert.Equal("ab", field);
        Assert.Equal(";", delimiter);
        Assert.Equal(1, matched);
        Assert.Equal(4, pointer);
    }

    [Fact]
    public void Extract_EndOfSender_MatchesNothing()
    {
        long pointer = 1;
        int count = CobolStringOps.UnstringExtract("abc", [";"], 0, ref pointer, out string field, out string delimiter, out int matched);
        Assert.Equal(3, count);
        Assert.Equal("abc", field);
        Assert.Equal("", delimiter);
        Assert.Equal(-1, matched);
        Assert.Equal(4, pointer);
    }

    [Fact]
    public void Extract_ExhaustedSender_IsNotActedUpon_AndLeavesNothingMatched()
    {
        long pointer = 4;
        int count = CobolStringOps.UnstringExtract("abc", [";"], 0, ref pointer, out _, out string delimiter, out int matched);
        Assert.Equal(-1, count);
        Assert.Equal("", delimiter);
        Assert.Equal(-1, matched);
        Assert.Equal(4, pointer);
    }

    [Fact]
    public void Extract_ZeroLengthDelimiterIsIgnored_AndTheMatchIndexIsTheListedPosition()
    {
        long pointer = 1;
        CobolStringOps.UnstringExtract("a,b", ["", ","], 0, ref pointer, out _, out _, out int matched);
        Assert.Equal(1, matched);
    }

    [Fact]
    public void SkipRun_ConsumesTheContiguousRepeatsOfAnAllDelimiter()
    {
        // "a;;;b": the first ";" is at 2; Extract left the pointer at 3, the run covers positions 3 and 4.
        long pointer = 3;
        CobolStringOps.UnstringSkipRun("a;;;b", [";"], [true], 0, ref pointer);
        Assert.Equal(5, pointer);
    }

    [Fact]
    public void SkipRun_MultiCharacterDelimiter_ConsumesWholeOccurrencesOnly()
    {
        // "a------b" with ALL "--": after the first occurrence (positions 2-3) two whole repeats remain, then "-b".
        long pointer = 4;
        CobolStringOps.UnstringSkipRun("a------b", ["--"], [true], 0, ref pointer);
        Assert.Equal(8, pointer);
    }

    [Fact]
    public void SkipRun_ReadsTheDelimiterAsItIsPassed_SoAnOverwrittenDelimiterGovernsTheRun()
    {
        // GR18 / Annex A.2 item 61 leave the overwrite undefined; the run is measured against the delimiter handed in.
        long pointer = 3;
        CobolStringOps.UnstringSkipRun("a;aab", ["a"], [true], 0, ref pointer);
        Assert.Equal(5, pointer);
        pointer = 3;
        CobolStringOps.UnstringSkipRun("a;;b", ["a"], [true], 0, ref pointer);
        Assert.Equal(3, pointer);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void SkipRun_NotAnAllMatch_MovesNothing(int matched)
    {
        // matched −1: the examination ended at the end of the sender; matched 0 is a delimiter without the ALL phrase.
        long pointer = 3;
        CobolStringOps.UnstringSkipRun("a;;;b", [";"], new[] { matched < 0 }, matched, ref pointer);
        Assert.Equal(3, pointer);
    }

    [Fact]
    public void SkipRun_AmongSeveralDelimiters_OnlyTheMatchedOnesAllPhraseCounts()
    {
        // DELIMITED BY ALL ";" OR "," — the second is a single occurrence (GR7 collapses the ALL phrase's repeats only).
        string[] delimiters = [";", ","];
        bool[] allFlags = [true, false];
        long pointer = 3;
        CobolStringOps.UnstringSkipRun("a,,b", delimiters, allFlags, 1, ref pointer);
        Assert.Equal(3, pointer);
        pointer = 3;
        CobolStringOps.UnstringSkipRun("a;;b", delimiters, allFlags, 0, ref pointer);
        Assert.Equal(4, pointer);
    }

    [Fact]
    public void SkipRun_ZeroLengthDelimiter_MovesNothingAndDoesNotLoop()
    {
        long pointer = 2;
        CobolStringOps.UnstringSkipRun("abc", [""], [true], 0, ref pointer);
        Assert.Equal(2, pointer);
    }

    [Fact]
    public void SkipRun_RunEndsAtTheEndOfTheSender()
    {
        long pointer = 3;
        CobolStringOps.UnstringSkipRun("a;;;", [";"], [true], 0, ref pointer);
        Assert.Equal(5, pointer);
    }
}
