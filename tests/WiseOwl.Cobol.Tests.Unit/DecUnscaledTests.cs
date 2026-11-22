// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The one unscaled-decimal reduction primitive (kb/Work PB2657): its digit count is read off the power-of-ten table, so
/// every boundary 10^n - 1 / 10^n is pinned, and its trailing-zero strip keeps the divide loops' answers, including the
/// zero significand the loops drove straight to the ceiling. Expected values are the decimal digit counts of the literals.
/// </summary>
public sealed class DecUnscaledTests
{
    [Fact]
    public void DigitCount_EveryPowerOfTenBoundary()
    {
        Assert.Equal(1, DecUnscaled.DigitCount(UInt128.Zero));
        UInt128 p = 1;
        for (int n = 1; n <= 38; n++)
        {
            Assert.Equal(n, DecUnscaled.DigitCount(p * 10 - 1));   // 10^n - 1 has n digits
            p *= 10;
            Assert.Equal(n + 1, DecUnscaled.DigitCount(p));         // 10^n has n + 1 digits
        }
        Assert.Equal(39, DecUnscaled.DigitCount(UInt128.MaxValue)); // 340282366920938463463374607431768211455
    }

    [Theory]
    [InlineData("12300", -4, 0, "123", -2)]    // 1.2300 -> 1.23
    [InlineData("12300", -1, 0, "1230", 0)]    // the ceiling stops the strip at exponent 0
    [InlineData("12300", -1, int.MaxValue, "123", 1)]
    [InlineData("-500", -3, 0, "-5", -1)]
    [InlineData("7", -2, 0, "7", -2)]
    [InlineData("0", -5, 0, "0", 0)]           // 0.00000 is an integer value: 0 x 10^0
    [InlineData("0", 3, 0, "0", 3)]            // already at or past the ceiling
    public void StripTrailingZeros_MovesZerosIntoTheExponent(string sig, int exp, int ceiling, string wantSig, int wantExp)
    {
        var (s, e) = DecUnscaled.StripTrailingZeros(Int128.Parse(sig), exp, ceiling);
        Assert.Equal(Int128.Parse(wantSig), s);
        Assert.Equal(wantExp, e);
    }
}
