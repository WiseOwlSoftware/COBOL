// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime.Numeric;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime.Numeric;

/// <summary>
/// Unit tests for <see cref="CobolNum.FormatUnsignedDisplay"/> — the typed-numeric (S4) display formatter. It
/// must reproduce the byte path's stored DISPLAY image for an unsigned <c>PIC 9(n)</c>: the low <c>n</c> decimal
/// digits, zero-padded (the differential coverage lives in the integration <c>TypedFieldFlipTests</c>; these pin
/// the helper's edge cases directly).
/// </summary>
public sealed class CobolNumFormatTests
{
    [Theory]
    [InlineData(42, 5, "00042")]
    [InlineData(0, 5, "00000")]
    [InlineData(7, 1, "7")]
    [InlineData(99999, 5, "99999")]
    [InlineData(1234567, 5, "34567")]   // high-order truncation: low 5 digits
    [InlineData(1000000000000000000, 18, "000000000000000000")] // 10^18 mod 10^18 = 0
    [InlineData(123456789012345678, 18, "123456789012345678")]
    public void FormatUnsignedDisplay_ProducesZeroPaddedLowDigits(long value, int digits, string expected)
        => Assert.Equal(expected, CobolNum.FormatUnsignedDisplay(value, digits));

    [Fact]
    public void FormatUnsignedDisplay_NegativeValue_RendersUnsignedMagnitudeDigits()
        => Assert.Equal("00042", CobolNum.FormatUnsignedDisplay(-42, 5));

    [Fact]
    public void FormatUnsignedDisplay_ZeroDigits_IsEmpty()
        => Assert.Equal("", CobolNum.FormatUnsignedDisplay(123, 0));
}
