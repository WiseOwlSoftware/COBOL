// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime.IO;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPORT COUNTERS HAVE ONE IMPLICIT DESCRIPTION, AND THE COMPILER AND THE RUNTIME STATE IT IN ONE PLACE EACH
/// (kb/Work PB1746). PAGE-COUNTER and LINE-COUNTER are "temporary unsigned integer data items" (ISO §8.4.3.15.4 GR1)
/// of no size or usage the standard names, so the latitude order takes GnuCOBOL's — an unsigned 32-bit binary
/// integer (cobc 3.2.0 <c>CB_USAGE_UNSIGNED_INT</c>). The compiler states it once, as <see cref="PicInfo.ReportCounterItem"/>
/// (the extremes, the 10-digit image, the 4-byte length and the usage screens all read it through
/// <c>AlgebraicRanges.DescribeCounter</c>); the runtime states its twin once, as <see cref="CobolReport.CounterModulus"/>
/// (every assignment of either counter reduces modulo it). This suite pins the two together, so a change of the width
/// in one place cannot leave the other holding a value its description does not.
/// Every expected value is COMPUTED from the modulus — never from observed output.
/// </summary>
public sealed class ReportCounterDescriptionDriftTests
{
    private static CobolReport NewReport() => new("R", "P::F", 80, 80, true, 10, 1, 2, 2, 8, 9);

    [Fact]
    public void Description_IsAnUnsignedBinaryIntegerAsWideAsTheRuntimeModulus()
    {
        var pic = PicInfo.ReportCounterItem();
        Assert.False(pic.Signed);
        Assert.Equal(0, pic.Scale);
        Assert.Equal(Usage.BinaryLong, pic.Usage);
        // The modulus is a power of two, 2^bits, and the description's container has exactly those bits.
        Assert.Equal(CobolReport.CounterModulus, 1L << (8 * pic.StorageWidth));
    }

    [Fact]
    public void Description_CapacityIsTheLargestValueTheRuntimeCarrierHolds()
    {
        var pic = PicInfo.ReportCounterItem();
        var range = AlgebraicRanges.Of(pic)!.Value;
        string capacity = (CobolReport.CounterModulus - 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(capacity, range.Farthest);
        Assert.Null(range.FarthestNegative);          // unsigned: no negative extreme (§8.4.3.15.4 GR1)
        Assert.Equal("1", range.Nearest);
        Assert.Equal("0", range.Zero);
        // The character image is the capacity's digit count, so the all-nines is not what sizes it.
        Assert.Equal(capacity.Length, pic.Digits);
    }

    [Fact]
    public void SetPageCounter_StoresAValueInsideTheCapacityUnchanged()
    {
        var report = NewReport();
        report.SetPageCounter(CobolReport.CounterModulus - 1);
        Assert.Equal(CobolReport.CounterModulus - 1, report.PageCounter);
        report.SetPageCounter(0);
        Assert.Equal(0, report.PageCounter);
    }

    [Theory]
    [InlineData(0L, 0L)]                                   // exactly one lap
    [InlineData(1L, 1L)]
    [InlineData(705032704L, 705032704L)]
    public void SetPageCounter_ReducesAValueBeyondTheCapacityModuloTheCapacity(long overshoot, long expected)
    {
        // cobc 3.2.0 stores 5,000,000,000 into PAGE-COUNTER as 705,032,704 and wraps 4,294,967,295 + 1 to 0.
        var report = NewReport();
        report.SetPageCounter(CobolReport.CounterModulus + overshoot);
        Assert.Equal(expected, report.PageCounter);
    }

    [Fact]
    public void AnIncrementPastTheCapacity_WrapsToZero()
    {
        // The engine's page advance is `PageCounter = PageCounter + 1` through the same reducing setter, so a counter
        // at its capacity advances to 0, as cobc's ADD 1 TO PAGE-COUNTER does (§8.4.3.15.4 GR1 sets no larger size).
        var report = NewReport();
        report.SetPageCounter(CobolReport.CounterModulus - 1);
        report.SetPageCounter(report.PageCounter + 1);
        Assert.Equal(0, report.PageCounter);
    }
}
