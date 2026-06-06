// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolSharp.Runtime.Numeric;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime.Numeric;

/// <summary>
/// Unit tests for <see cref="CobolDecimal"/>, the exact base-10 / BigInteger fixed-point carrier that is
/// the numeric substrate of the .NET-native data model (docs/DATA_MODEL_ARCHITECTURE.md, owner-gated
/// decision #1). Covers decimal interop, exact arithmetic, scale-normalized value semantics, the eight
/// rounding modes in <see cref="CobolDecimal.RescaleTo"/>, and 31-digit values beyond decimal's range.
/// </summary>
public sealed class CobolDecimalTests
{
    // ---------- decimal interop ----------

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("123.45")]
    [InlineData("-123.45")]
    [InlineData("0.0001")]
    [InlineData("-0.0001")]
    [InlineData("79228162514264337593543950335")] // decimal.MaxValue
    [InlineData("-79228162514264337593543950335")]
    [InlineData("12.340")] // trailing zero preserved through the round-trip
    public void FromDecimal_ToDecimal_RoundTrips(string literal)
    {
        decimal d = decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
        var cd = CobolDecimal.FromDecimal(d);
        Assert.Equal(d, cd.ToDecimal());
    }

    [Fact]
    public void FromDecimal_PreservesScaleExactly()
    {
        var cd = CobolDecimal.FromDecimal(1.2300m);
        Assert.Equal(4, cd.Scale);
        Assert.Equal(new BigInteger(12300), cd.Unscaled);
    }

    [Fact]
    public void TryToDecimal_ReturnsFalse_BeyondDecimalRange()
    {
        // 31 nines — exceeds decimal's 28–29 digit capacity.
        var wide = new CobolDecimal(BigInteger.Parse("9999999999999999999999999999999"), 0);
        Assert.False(wide.TryToDecimal(out _));
        Assert.Throws<OverflowException>(() => wide.ToDecimal());
    }

    [Fact]
    public void TryToDecimal_ReturnsFalse_ForScaleAbove28()
    {
        var deepScale = new CobolDecimal(BigInteger.One, 29);
        Assert.False(deepScale.TryToDecimal(out _));
    }

    // ---------- value semantics (normalized across scale) ----------

    [Fact]
    public void Equality_NormalizesAcrossScale()
    {
        Assert.Equal(new CobolDecimal(1, 0), new CobolDecimal(10, 1));     // 1 == 1.0
        Assert.Equal(new CobolDecimal(15, 1), new CobolDecimal(1500, 3));  // 1.5 == 1.500
        Assert.NotEqual(new CobolDecimal(15, 1), new CobolDecimal(16, 1)); // 1.5 != 1.6
    }

    [Fact]
    public void EqualValues_HashEqually()
    {
        Assert.Equal(new CobolDecimal(1, 0).GetHashCode(), new CobolDecimal(1000, 3).GetHashCode());
        Assert.Equal(CobolDecimal.Zero.GetHashCode(), new CobolDecimal(0, 5).GetHashCode());
    }

    [Theory]
    [InlineData("1.5", "1.50", 0)]
    [InlineData("1.5", "1.6", -1)]
    [InlineData("-2", "-1.999", -1)]
    [InlineData("0", "0.0000", 0)]
    [InlineData("100", "99.9999", 1)]
    public void CompareTo_OrdersByValue(string a, string b, int expectedSign)
    {
        var ca = CobolDecimal.FromDecimal(decimal.Parse(a, System.Globalization.CultureInfo.InvariantCulture));
        var cb = CobolDecimal.FromDecimal(decimal.Parse(b, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expectedSign, Math.Sign(ca.CompareTo(cb)));
    }

    // ---------- exact arithmetic ----------

    [Fact]
    public void Add_Subtract_Multiply_AreExact()
    {
        var a = CobolDecimal.FromDecimal(12.34m);
        var b = CobolDecimal.FromDecimal(0.066m);
        Assert.Equal(CobolDecimal.FromDecimal(12.406m), a + b);
        Assert.Equal(CobolDecimal.FromDecimal(12.274m), a - b);
        Assert.Equal(CobolDecimal.FromDecimal(0.81444m), a * b);
    }

    [Fact]
    public void Multiply_DoesNotOverflow_BeyondDecimal()
    {
        // 10^20 × 10^20 = 10^40 — impossible in decimal, exact here.
        var big = new CobolDecimal(BigInteger.Pow(10, 20), 0);
        var product = big * big;
        Assert.Equal(BigInteger.Pow(10, 40), product.Unscaled);
        Assert.Equal(0, product.Scale);
    }

    [Fact]
    public void Negate_And_Abs()
    {
        var a = CobolDecimal.FromDecimal(-7.25m);
        Assert.Equal(CobolDecimal.FromDecimal(7.25m), a.Negate());
        Assert.Equal(CobolDecimal.FromDecimal(7.25m), a.Abs());
        Assert.Equal(CobolDecimal.FromDecimal(7.25m), (-a));
    }

    // ---------- RescaleTo (the eight rounding modes) ----------

    [Fact]
    public void RescaleTo_Increase_IsExact()
    {
        var a = CobolDecimal.FromDecimal(1.5m);
        var widened = a.RescaleTo(4, CobolRounding.Truncation);
        Assert.Equal(4, widened.Scale);
        Assert.Equal(new BigInteger(15000), widened.Unscaled);
        Assert.Equal(a, widened);
    }

    // 2.5 and -2.5 → integer, exercising each tie-resolution rule.
    [Theory]
    [InlineData(CobolRounding.Truncation, "2.5", "2")]
    [InlineData(CobolRounding.Truncation, "-2.5", "-2")]
    [InlineData(CobolRounding.NearestAwayFromZero, "2.5", "3")]
    [InlineData(CobolRounding.NearestAwayFromZero, "-2.5", "-3")]
    [InlineData(CobolRounding.AwayFromZero, "2.1", "3")]
    [InlineData(CobolRounding.AwayFromZero, "-2.1", "-3")]
    [InlineData(CobolRounding.NearestEven, "2.5", "2")]
    [InlineData(CobolRounding.NearestEven, "3.5", "4")]
    [InlineData(CobolRounding.NearestEven, "-2.5", "-2")]
    [InlineData(CobolRounding.NearestTowardZero, "2.5", "2")]
    [InlineData(CobolRounding.NearestTowardZero, "2.6", "3")]
    [InlineData(CobolRounding.NearestTowardZero, "-2.5", "-2")]
    [InlineData(CobolRounding.TowardGreater, "2.1", "3")]
    [InlineData(CobolRounding.TowardGreater, "-2.9", "-2")]
    [InlineData(CobolRounding.TowardLesser, "2.9", "2")]
    [InlineData(CobolRounding.TowardLesser, "-2.1", "-3")]
    [InlineData(CobolRounding.Prohibited, "2.5", "2")] // toward zero; SIZE-ERROR signalling is CobolNum's job
    public void RescaleTo_RoundsToInteger_PerMode(CobolRounding mode, string input, string expected)
    {
        var v = CobolDecimal.FromDecimal(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture));
        var r = v.RescaleTo(0, mode);
        Assert.Equal(0, r.Scale);
        Assert.Equal(BigInteger.Parse(expected), r.Unscaled);
    }

    [Fact]
    public void IsInexactAtScale_DetectsDroppedDigits()
    {
        var v = CobolDecimal.FromDecimal(2.25m);
        Assert.True(v.IsInexactAtScale(1));   // 2.25 → 2.2/2.3 drops a digit
        Assert.False(v.IsInexactAtScale(2));  // exact at two places
        Assert.False(v.IsInexactAtScale(5));  // widening is always exact
    }

    // ---------- formatting / digit count ----------

    [Theory]
    [InlineData("0", 0, "0")]
    [InlineData("123", 0, "123")]
    [InlineData("-123", 0, "-123")]
    [InlineData("12340", 1, "1234.0")]
    [InlineData("-5", 3, "-0.005")]
    [InlineData("5", 3, "0.005")]
    public void ToString_RendersPlainDecimal(string unscaled, int scale, string expected)
    {
        Assert.Equal(expected, new CobolDecimal(BigInteger.Parse(unscaled), scale).ToString());
    }

    [Theory]
    [InlineData("0", 1)]
    [InlineData("9", 1)]
    [InlineData("10", 2)]
    [InlineData("-999", 3)]
    [InlineData("9999999999999999999999999999999", 31)]
    public void DigitCount_CountsSignificand(string unscaled, int expected)
    {
        Assert.Equal(expected, new CobolDecimal(BigInteger.Parse(unscaled), 0).DigitCount);
    }

    [Fact]
    public void Constructor_RejectsNegativeScale()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CobolDecimal(BigInteger.One, -1));
    }
}
