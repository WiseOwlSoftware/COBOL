// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The PB65 exact non-widening comparison and escape-checked alignment — <c>CobolNum.Compare</c>,
/// <c>RescaleEscape</c>, <c>RescaleStoreCap</c>, and the selection-by-value intrinsic bodies.
/// </summary>
/// <remarks>
/// The defect these pin out: aligning both comparison sides (or every MIN/MAX argument) to the common scale
/// widened with an UNCHECKED Int128 multiply, and at 39 aligned digits the operand wrapped silently —
/// <c>IF BIGV &gt; SMLV</c> over a <c>PIC 9(24)</c> and a <c>PIC 9V9(15)</c> answered FALSE, and MIN of two
/// positive arguments returned a negative value. A comparison has a defined answer for every legal pair, so
/// it must never widen at all (the sign-split + overflow-means-greater magnitude trick the unsigned lane
/// always used).
/// </remarks>
public sealed class CobolNumCompareScaledTests
{
    private static readonly Int128 Bigv = Int128.Parse("200000000000000000000000");   // 2×10²³ @ scale 0

    [Fact]
    public void Compare_39AlignedDigits_IsExact()
    {
        // BIGV (2e23 @ 0) vs SMLV (1e-15 as 1 @ 15): common scale 15 → 39 digits, past Int128.
        Assert.True(CobolNum.Compare(Bigv, 0, 1, 15) > 0);
        Assert.True(CobolNum.Compare(1, 15, Bigv, 0) < 0);
    }

    [Theory]
    [InlineData(5, 0, 50, 1, 0)]       // 5 == 5.0 across scales
    [InlineData(-5, 0, -50, 1, 0)]     // −5 == −5.0
    [InlineData(-1, 0, 1, 15, -1)]     // sign decides
    [InlineData(1, 15, 0, 0, 1)]       // 1e-15 > 0
    [InlineData(-1, 15, -1, 30, -1)]   // −1e-15 < −1e-30 (magnitude flip under the shared sign)
    public void Compare_SignAndScaleTable(long a, int sa, long b, int sb, int expectedSign)
    {
        int c = CobolNum.Compare(a, sa, b, sb);
        Assert.Equal(expectedSign, c == 0 ? 0 : c < 0 ? -1 : 1);
    }

    [Fact]
    public void RescaleEscape_RaisesAtTheBoundary_AndZeroNeverDoes()
    {
        Assert.Throws<CobolSizeError>(() => CobolNum.RescaleEscape(Bigv, 0, 15, CobolRounding.Truncation));
        Assert.Equal((Int128)0, CobolNum.RescaleEscape(0, 0, 30, CobolRounding.Truncation));
        Assert.Equal((Int128)1000, CobolNum.RescaleEscape(1, 0, 3, CobolRounding.Truncation));
    }

    [Fact]
    public void RescaleStoreCap_DropsHighOrderDecimally_NeverWrapsNegative()
    {
        // 2e23 widened to scale 15 exceeds 38 digits: store semantics keep the low decimal digits (all zero
        // here), never a binary wrap — composing with a receiver's own capacity mod exactly as Store does.
        Assert.Equal((Int128)0, CobolNum.RescaleStoreCap(Bigv, 0, 15, CobolRounding.Truncation));
        Assert.True(CobolNum.RescaleStoreCap(Bigv, 0, 15, CobolRounding.Truncation) >= 0);
        Assert.Equal((Int128)123000, CobolNum.RescaleStoreCap(123, 0, 3, CobolRounding.Truncation));
    }

    /// <summary>⛔ <c>Int128.Abs</c> THROWS on <c>Int128.MinValue</c>, and both widening siblings used to call it
    /// (kb/Work PB288's sibling sweep). The value is reachable: <c>CobolArgAdapt.ReadNumericCell</c>'s R10 bits
    /// contract delivers a 16-byte unsigned carrier's top-half content as exactly that <c>Int128</c>, and the
    /// CALL landing now widens through <c>RescaleStoreCap</c> on every access. The store-semantics arm must
    /// answer the low-order digits (its magnitude's 39th digit can never survive a ≤38-digit cap, so dropping it
    /// first is lossless); the escape arm must RAISE, because no scale-9 alignment of that magnitude exists.</summary>
    [Fact]
    public void BothWideningArms_SurviveInt128MinValue()
    {
        // |Int128.MinValue| × 10 = 1701411834604692317316873037158841057280 (40 digits); its low-order 38 are
        // 01411834604692317316873037158841057280, sign kept.
        Assert.Equal(Int128.Parse("-1411834604692317316873037158841057280"),
            CobolNum.RescaleStoreCap(Int128.MinValue, 0, 1, CobolRounding.Truncation));
        Assert.Throws<CobolSizeError>(() => CobolNum.RescaleEscape(Int128.MinValue, 0, 1, CobolRounding.Truncation));
        // The escape arm's NEGATIVE bound is a real bound, not the positive one mirrored by Abs.
        Assert.Equal((Int128)(-1000), CobolNum.RescaleEscape(-1, 0, 3, CobolRounding.Truncation));
    }

    /// <summary>The cap TEST changed from a digit-count loop to a power-of-ten compare when the CALL ABI put
    /// this on a per-access path (kb/Work PB288). Equivalence is asserted against the reference meaning — the
    /// low-order 38 digits of the exact shifted magnitude — across the whole boundary neighbourhood, so the
    /// O(1) rewrite cannot drift by one digit at the 38-digit edge.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(18)]
    [InlineData(37)]
    [InlineData(38)]
    [InlineData(39)]
    public void RescaleStoreCap_MatchesTheLowOrder38Digits_AtEveryShift(int shift)
    {
        System.Numerics.BigInteger p38 = System.Numerics.BigInteger.Pow(10, 38);
        foreach (Int128 seed in new Int128[]
                 {
                     0, 1, 7, Int128.Parse("999999999999999999"),
                     Int128.Parse("100000000000000000000000000000"),          // 10^29
                     Int128.Parse("12345678901234567890123456789012345678"),  // 38 digits
                     Int128.MaxValue,
                 })
        foreach (Int128 value in new[] { seed, -seed })
        {
            var big = (System.Numerics.BigInteger)value * System.Numerics.BigInteger.Pow(10, shift);
            var expected = System.Numerics.BigInteger.Abs(big) % p38;
            if (big < 0) expected = -expected;
            Assert.Equal(expected.ToString(),
                CobolNum.RescaleStoreCap(value, 0, shift, CobolRounding.Truncation).ToString());
        }
    }

    [Fact]
    public void SelectionBodies_39AlignedDigits_PickTheRightArgument()
    {
        Int128[] v = [Bigv, 1];
        int[] s = [0, 15];
        // Receiver-bound (store): MAX into a scale-0 receiver is BIGV exactly; MIN is 1e-15 → 0 by store rules.
        Assert.Equal(Bigv, CobolIntrinsics.MaxAt(0, true, v, s));
        Assert.Equal((Int128)0, CobolIntrinsics.MinAt(0, true, v, s));
        // Receiverless: MIN at the common scale is exact; MAX cannot represent → LOUD, never a wrong value.
        Assert.Equal((Int128)1, CobolIntrinsics.MinAt(15, false, v, s));
        Assert.Throws<CobolSizeError>(() => CobolIntrinsics.MaxAt(15, false, v, s));
        // Ordinals: pure selection, always defined (§15.71.4/§15.72.4), first-extreme ties.
        Assert.Equal(1, CobolIntrinsics.OrdMaxAt(v, s));
        Assert.Equal(2, CobolIntrinsics.OrdMinAt(v, s));
        Assert.Equal(1, CobolIntrinsics.OrdMaxAt([5, 5], [0, 0]));
    }
}
