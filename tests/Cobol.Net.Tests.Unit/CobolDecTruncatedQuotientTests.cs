// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NESTED NATIVE QUOTIENT ON THE SDIDI IS THE QUOTIENT TRUNCATED AT ITS WORKING SCALE (kb/Work PB3538 review finding
/// R2-1; CONFORMANCE.md DOC-A.1-123). <c>CobolDec.DivTruncatedToOdd</c> forms the same value the scaled lane
/// (<c>CobolNum.Divide</c> at that scale, TRUNCATION) and the wide form (<c>CobolWide.DivTruncated</c>) form, then holds it
/// to the SDIDI's 34 significant digits by ROUND-TO-ODD. The oracle is <see cref="BigInteger"/> (truncating division) and
/// the round-to-odd rule written out here: keep the 34-digit truncation, and make its last digit odd when a dropped digit
/// of the TRUNCATED quotient is nonzero. Digits below the working scale are not part of the value.
/// </summary>
public sealed class CobolDecTruncatedQuotientTests
{
    private static readonly BigInteger Limit34 = BigInteger.Pow(10, 34);

    /// <summary>The expected SDIDI value of a ÷ b truncated at <paramref name="scale"/> and held to 34 digits by round-to-odd.</summary>
    private static CobolDec Expected(BigInteger aSig, int aExp, BigInteger bSig, int bExp, int scale)
    {
        int up = aExp - bExp + scale;
        BigInteger t = up >= 0 ? aSig * BigInteger.Pow(10, up) / bSig : aSig / (bSig * BigInteger.Pow(10, -up));
        int sign = t.Sign;
        BigInteger mag = BigInteger.Abs(t);
        int exp = -scale;
        bool dropped = false;
        while (mag >= Limit34)
        {
            dropped |= !(mag % 10).IsZero;
            mag /= 10;
            exp++;
        }
        if (dropped && mag.IsEven) mag += 1;
        return mag.IsZero ? new CobolDec(0, 0) : new CobolDec((Int128)(sign * mag), exp);
    }

    private static Int128 I(string s) => Int128.Parse(s, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("2", 0, "7", 0, 20)]                                       // 0.28571428571428571428: 20 digits, exact
    [InlineData("-2", 0, "7", 0, 20)]
    [InlineData("2", 0, "-7", 0, 14)]
    [InlineData("1", 0, "3", 0, 40)]                                       // 40 threes: odd 34th digit kept
    [InlineData("2", 0, "3", 0, 40)]                                       // 40 sixes: the even 34th digit becomes 7
    [InlineData("1", 0, "8", 0, 2)]                                        // 0.125 at scale 2 is 0.12, exactly
    [InlineData("1000000000000000000000000000000", 0, "3", 0, 20)]         // 50 digits at scale 20
    [InlineData("7000000000000000000000000000000000001", 0, "7", 0, 0)]    // 10^36 + 1/7 at scale 0: 10^36, exact
    [InlineData("7000000000000000000000000000000000001", 0, "7", 0, 1)]    // ... at scale 1: 10^37 + 1, inexact at 34 digits
    [InlineData("123456789", 0, "1", 5, 2)]                                // the divisor scaled up (1234.56789 → 1234.56)
    [InlineData("1", 0, "3", 40, 2)]                                       // a quotient below 10^-2: 0
    [InlineData("1", 0, "3", 50, 2)]                                       // a divisor past 10^39 at the scale: 0
    [InlineData("5", 10, "2", 12, 2)]                                      // 0.025 at scale 2 is 0.02
    [InlineData("170141183460469231731687303715884105727", 0, "3", 0, 20)] // a 39-digit dividend, Int128.MaxValue
    [InlineData("1", 0, "170141183460469231731687303715884105727", 0, 60)] // a 39-digit divisor
    [InlineData("2", -6, "7", 0, 20)]                                      // a scaled dividend
    public void TheQuotient_IsTruncatedAtTheScale_ThenHeldTo34DigitsByRoundToOdd(string a, int aExp, string b, int bExp, int scale)
    {
        CobolDec got = CobolDec.DivTruncatedToOdd(new CobolDec(I(a), aExp), new CobolDec(I(b), bExp), scale);
        CobolDec want = Expected(BigInteger.Parse(a, CultureInfo.InvariantCulture), aExp,
            BigInteger.Parse(b, CultureInfo.InvariantCulture), bExp, scale);
        Assert.True(CobolDec.Compare(got, want) == 0, $"{a}e{aExp} / {b}e{bExp} at scale {scale}: got {got}, want {want}");
    }

    [Fact]
    public void TheSdidiLane_FormsTheScaledLanesTruncatedQuotient()
    {
        // The lane is a representation: wherever the scaled Int128 lane holds the quotient, the SDIDI holds the same one.
        (long A, int As, long B, int Bs, int R)[] cases = [(2, 0, 7, 0, 20), (-2, 0, 7, 0, 20), (8, 0, 2, 0, 18), (1, 0, 3, 4, 18), (123456789, 4, -97, 2, 9)];
        foreach (var (a, aScale, b, bScale, r) in cases)
            Assert.Equal(CobolNum.Divide(a, aScale, b, bScale, r, CobolRounding.Truncation),
                CobolDec.DivTruncatedToOdd(CobolDec.From(a, aScale), CobolDec.From(b, bScale), r).ToUnscaled(r, CobolRounding.Truncation));
    }

    [Fact]
    public void TheExactQuotientsRoundToOdd_DiffersBelowTheScale_WhichIsWhyTheTruncatedFormExists()
    {
        // 10^36 + 1/7 truncated at scale 0 is exactly 10^36; DivToOdd keeps the 1/7 as an odd last digit.
        var a = new CobolDec(I("7000000000000000000000000000000000001"), 0);
        var b = new CobolDec(7, 0);
        Assert.True(CobolDec.Compare(CobolDec.DivTruncatedToOdd(a, b, 0), new CobolDec(I("1000000000000000000000000000000000"), 3)) == 0);
        Assert.True(CobolDec.Compare(CobolDec.DivToOdd(a, b), new CobolDec(I("1000000000000000000000000000000000"), 3)) > 0);
    }

    [Fact]
    public void AZeroDivisor_IsTheZeroDivideCondition_AndAZeroDividendIsZero()
    {
        var error = Assert.Throws<CobolSizeError>(() => CobolDec.DivTruncatedToOdd(new CobolDec(1, 0), new CobolDec(0, 0), 4));
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", error.EcName);
        Assert.True(CobolDec.Compare(CobolDec.DivTruncatedToOdd(new CobolDec(0, 0), new CobolDec(3, 0), 40), new CobolDec(0, 0)) == 0);
    }
}
