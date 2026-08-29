// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="AcceptSource.Device"/> — the Format 1 record contract (ISO §14.9.1.4 GR2/GR4), pinned at the
/// runtime seam because two of its legs no compile-and-run golden can reach: the zero-length receiver (GR4b's
/// closing sentence — no static PICTURE is zero-length) and the exact record accounting across calls. The facts
/// run serially within this class, the assembly's ONLY in-process user of <c>Console.In</c> (the conformance
/// ACCEPT tests pipe stdin to a child process instead).
/// </summary>
public sealed class AcceptSourceTests
{
    private static T WithStdin<T>(string input, Func<T> body)
    {
        var saved = Console.In;
        try
        {
            Console.SetIn(new StringReader(input));
            return body();
        }
        finally { Console.SetIn(saved); }
    }

    // §14.9.1.4 GR4b's closing sentence: "If identifier-1 references a zero-length item, all the characters
    // of the transferred data are ignored" — the transfer HAPPENS: one record is consumed and discarded, so
    // the NEXT request sees record 2. (The old early-return never read, handing record 1 to the next ACCEPT.)
    [Fact]
    public void ZeroLengthReceiver_ConsumesItsRecord()
    {
        var (zero, next) = WithStdin("AAA\nBBB\n", () => (AcceptSource.Device(0), AcceptSource.Device(3)));
        Assert.Equal("", zero);
        Assert.Equal("BBB", next);
    }

    // §14.9.1.4 GR2: one transfer is EXACTLY one 80-character record. An input line longer than 80
    // characters is CONSECUTIVE records, each padded to the card image — line characters 81+ start record 2,
    // which pads to 80 before the next line contributes record 3.
    [Fact]
    public void LongLine_SplitsIntoPaddedRecords()
    {
        string line = new string('A', 80) + new string('B', 10);
        string got = WithStdin(line + "\nC\n", () => AcceptSource.Device(170));
        Assert.Equal(new string('A', 80) + new string('B', 10) + new string(' ', 70) + "C" + new string(' ', 9), got);
    }

    // §14.9.1.4 GR4b: a receiver needing only record 1 ignores the line's remaining records along with the
    // rest of record 1 — the next request reads a fresh input line, never leftover characters 81+.
    [Fact]
    public void LongLine_NarrowReceiver_RemainingRecordsIgnored()
    {
        var (a, b) = WithStdin("ABCDE" + new string('Z', 85) + "\nXY\n",
            () => (AcceptSource.Device(5), AcceptSource.Device(2)));
        Assert.Equal("ABCDE", a);
        Assert.Equal("XY", b);
    }

    // §14.9.1.4 GR4a: end-of-input mid-fill space-pads the remainder (unchanged behavior, pinned so the
    // record-splitting rewrite cannot regress it).
    [Fact]
    public void EndOfInput_SpaceFillsRemainder()
        => Assert.Equal("AB" + new string(' ', 88), WithStdin("AB\n", () => AcceptSource.Device(90)));

    // §14.9.1.4 GR1: the boolean-receiver conversion — '1' to boolean one, every other transferred character
    // (a '0', a pad space, any device character) to boolean zero, so the §13.18.40.4 GR14 representation
    // invariant holds for any input.
    [Fact]
    public void DeviceBoolean_MapsEveryNonOneToZero()
        => Assert.Equal("101000", WithStdin("1X1 0\n", () => AcceptSource.DeviceBoolean(6)));
}
