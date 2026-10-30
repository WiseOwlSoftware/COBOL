// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE EXACT INTRINSIC FAMILY ON THE EXACT WIDE INTERMEDIATE (kb/Work PB3953; docs/CONFORMANCE.md DOC-A.1-123): the
/// <c>CobolIntrinsics.*Wide</c> bodies, the <see cref="CobolWide"/> primitives they stand on, and the temporary DISPLAY shows
/// (<see cref="FunctionValueField.OfWide"/>). Every expected value is computed by <see cref="BigInteger"/>, an independent
/// arbitrary-precision oracle the runtime deliberately does not use (DESIGN §1.2 invariant 2), from the function's returned-value
/// rule: ABS §15.7.4, INTEGER §15.44.4, INTEGER-PART §15.49.4, FRACTION-PART §15.42.4, MOD §15.64.4 (floored), REM §15.77.4
/// (truncated), MEDIAN §15.61.4, MIDRANGE §15.62.4.
/// </summary>
public sealed class CobolWideIntrinsicsTests
{
    private static readonly BigInteger Big18 = BigInteger.Pow(10, 18) - 1;

    /// <summary>The 39-digit product the PB3953 probe used: (10^18 − 1)² × 999.</summary>
    private static readonly BigInteger Product39 = Big18 * Big18 * 999;

    private static CobolWide W(BigInteger v)
    {
        // Three base-10^26 limbs hold any magnitude below 10^78.
        BigInteger chunk = BigInteger.Pow(10, 26);
        BigInteger magnitude = BigInteger.Abs(v);
        CobolWide w = default;
        foreach (BigInteger limb in new[] { magnitude / chunk / chunk, magnitude / chunk % chunk, magnitude % chunk })
            w = CobolWide.Add(CobolWide.Mul(w, CobolWide.From((Int128)chunk)), CobolWide.From((Int128)limb));
        return v.Sign < 0 ? CobolWide.Negate(w) : w;
    }

    private static BigInteger Of(CobolWide w) => BigInteger.Parse(w.MagnitudeDigits(), CultureInfo.InvariantCulture) * w.Sign;

    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    [InlineData("10000000000000000000")]
    [InlineData("998999999999999998002000000000000000999")]
    [InlineData("-99999999999999999999999999999999999999999999999999999999999999999999999999999")]
    public void MagnitudeDigits_AndTheRoundTrip_AreExact(string text)
    {
        BigInteger v = BigInteger.Parse(text, CultureInfo.InvariantCulture);
        Assert.Equal(v, Of(W(v)));
        Assert.Equal(BigInteger.Abs(v).ToString(CultureInfo.InvariantCulture), W(v).MagnitudeDigits());
    }

    [Fact]
    public void DivRem_TruncatesTowardZero_AndTheRemainderHasTheDividendsSign()
    {
        foreach (var (a, b) in new[] { (Product39, (BigInteger)1000), (-Product39, (BigInteger)1000), (Product39, (BigInteger)(-7)), (-Product39, Big18) })
        {
            CobolWide q = CobolWide.DivRem(W(a), W(b), out CobolWide r);
            Assert.Equal(BigInteger.Divide(a, b), Of(q));
            Assert.Equal(BigInteger.Remainder(a, b), Of(r));
        }
    }

    [Fact]
    public void TheUnaryBodies_ComputeTheirReturnedValueRules()
    {
        // −(Product39) × 1.5 at scale 1: −1498499999999999997003000000000000001498.5
        BigInteger unscaled = -Product39 * 15;
        Assert.Equal(Product39, Of(CobolIntrinsics.AbsWide(W(-Product39))));
        Assert.Equal(-1, CobolIntrinsics.SignWide(W(-Product39)));
        Assert.Equal(0, CobolIntrinsics.SignWide(default));
        Assert.Equal(BigInteger.Divide(unscaled, 10) - 1, Of(CobolIntrinsics.FloorWide(W(unscaled), 1)));        // floor of −…498.5 is −…499
        Assert.Equal(BigInteger.Divide(unscaled, 10), Of(CobolIntrinsics.TruncateWide(W(unscaled), 1)));
        Assert.Equal(BigInteger.Remainder(unscaled, 10), Of(CobolIntrinsics.FractionPartWide(W(unscaled), 1)));   // −5 at scale 1
        Assert.Equal(Product39 * 100, Of(CobolIntrinsics.TruncateWide(W(Product39), -2)));                       // trailing P: an integer already
        Assert.Equal(0, Of(CobolIntrinsics.FractionPartWide(W(Product39), 0)));
    }

    /// <summary>Product39 ends …000999, so a − b × INTEGER (a / b) is 999 / 1 / −1 / −999 by the sign table of §15.64.4's NOTE (the
    /// result takes the divisor's sign), and a − b × INTEGER-PART (a / b) keeps the dividend's: 999 or −999.</summary>
    [Theory]
    [InlineData(1, 1000, 999, 999)]
    [InlineData(-1, 1000, 1, -999)]
    [InlineData(1, -1000, -1, 999)]
    [InlineData(-1, -1000, -999, -999)]
    public void ModIsFloored_AndRemIsTruncated(int dividendSign, int divisor, int mod, int rem)
    {
        BigInteger a = dividendSign * Product39;
        Assert.Equal(mod, Of(CobolIntrinsics.ModWide(W(a), W(divisor))));
        Assert.Equal(rem, Of(CobolIntrinsics.RemWide(W(a), W(divisor))));
    }

    [Fact]
    public void TheSelectionsAndStatistics_OrderEveryDigit()
    {
        CobolWide low = W(Product39), high = W(Product39 + 1), other = W(Big18 * Big18 * 998);
        Assert.Equal(Product39 + 1, Of(CobolIntrinsics.MaxWide(low, high)));
        Assert.Equal(Product39, Of(CobolIntrinsics.MinWide(high, low)));
        Assert.Equal(2, CobolIntrinsics.OrdMaxWide(low, high));
        Assert.Equal(1, CobolIntrinsics.OrdMaxWide(high, high));      // the leftmost of equal values
        Assert.Equal(2, CobolIntrinsics.OrdMinWide(high, low));
        Assert.Equal(Product39 * 2 + 1, Of(CobolIntrinsics.SumWide(low, high)));
        Assert.Equal(Product39 - Big18 * Big18 * 998, Of(CobolIntrinsics.RangeWide(low, other)));
        Assert.Equal((Product39 + Big18 * Big18 * 998) * 5, Of(CobolIntrinsics.MedianWide(low, other)));   // the mean, one place further
        Assert.Equal(Big18 * Big18 * 998 * 10, Of(CobolIntrinsics.MedianWide(low, W(1), other)));        // the middle, one place further
        Assert.Equal((Product39 + 1) * 5, Of(CobolIntrinsics.MidrangeWide(low, W(1), other)));
    }

    [Fact]
    public void TheIntegerIntake_KeepsEveryDigit_AndTruncatesAFractionWithCheckingOff()
    {
        Assert.Equal(Product39, Of(CobolIntrinsics.IntegerArgWide(W(Product39 * 100), 2)));
        Assert.Equal(Product39, Of(CobolIntrinsics.IntegerArgWide(W(Product39 * 10 + 7), 1)));
    }

    [Fact]
    public void TheTemporary_ShowsEveryDigit_AndOrdersByTheExactValueWhereTheImagesAgree()
    {
        FunctionValueField low = FunctionValueField.OfWide(W(Product39), 0);
        FunctionValueField high = FunctionValueField.OfWide(W(Product39 + 1), 0);
        Assert.Equal("998999999999999998002000000000000000999", low.DeviceText(commaMode: false));
        Assert.Equal("-998999999999999998002000000000000000999", FunctionValueField.OfWide(W(-Product39), 0).DeviceText(false));
        Assert.Equal("+998999999999999998002000000000000000999", FunctionValueField.OfWide(W(-Product39), 0).Abs().DeviceText(false));
        Assert.Equal(39, low.ByteLength);
        // The two values share their 34-digit round-to-odd image, so only the exact refinement tells them apart.
        Assert.Equal(W(Product39).ToDec(0), W(Product39 + 1).ToDec(0));
        Assert.Equal("998999999999999998002000000000000001000", FunctionValueField.Max(low, high).DeviceText(false));
        Assert.Equal("998999999999999998002000000000000000999", FunctionValueField.Min(high, low).DeviceText(false));
        Assert.Equal("998999999999999998002000000000000000999.5", FunctionValueField.Median(high, low).DeviceText(false));
        // A value that fits Int128 is the SDIDI's temporary, exactly as OfValue describes it.
        Assert.Equal(FunctionValueField.OfValue(CobolDec.From(12345, 2)).DeviceText(false), FunctionValueField.OfWide(CobolWide.From(12345), 2).DeviceText(false));
    }
}
