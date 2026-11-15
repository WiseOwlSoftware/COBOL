// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB5575 — the run-time standard intermediate data item of ARITHMETIC IS STANDARD
/// (<see cref="CobolDec.ToSidi"/>, <see cref="CobolDec.ToSidiRounded"/>): "a precision of 32 decimal digits" in the range
/// 10^-999 through 10^1000 - 10^968 (§2002:8.8.1.3.1.1), a result "truncated to 32 significant digits" (§2002:8.8.1.3.2), the size
/// error and EC-SIZE-OVERFLOW / EC-SIZE-UNDERFLOW for a value outside it (§2002:8.8.1.3.1), and the rounding to 31 digits
/// §2002:8.8.1.3.1.3 gives a comparison, a function argument and a move without ROUNDED. Every expected value is those rules
/// applied by hand to the value written in the test, never read off the engine.
/// </summary>
public sealed class StandardIntermediateDataItemTests
{
    private static CobolDec Digits(string digits, int exp, bool negative = false) =>
        new(negative ? -(Int128)BigInteger.Parse(digits) : (Int128)BigInteger.Parse(digits), exp);

    [Fact]
    public void ToSidi_TruncatesTowardZeroToThirtyTwoSignificantDigits()
    {
        // 34 digits ...901299 cut to 32 digits: the two dropped digits 99 are never rounded up, whatever the sign.
        Assert.Equal(Digits("12345678901234567890123456789012", -8),
            Digits("1234567890123456789012345678901299", -10).ToSidi());
        Assert.Equal(Digits("12345678901234567890123456789012", -8, negative: true),
            Digits("1234567890123456789012345678901299", -10, negative: true).ToSidi());
    }

    [Fact]
    public void ToSidi_KeepsAValueOfAtMostThirtyTwoDigitsAndZero()
    {
        var value = Digits("12345678901234567890123456789012", -5);
        Assert.Equal(value, value.ToSidi());
        Assert.Equal(new CobolDec(0, 0), new CobolDec(0, 0).ToSidi());
    }

    [Theory]
    [InlineData("1", 999, false)]                                   // 10^999: the leading digit's exponent is 999, inside
    [InlineData("99999999999999999999999999999999", 968, false)]    // 10^1000 - 10^968, the largest value
    [InlineData("1", 1000, true)]                                   // 10^1000: past the largest value
    public void ToSidi_RaisesOverflowPastTheMaximum(string sig, int exp, bool overflows)
    {
        var value = Digits(sig, exp);
        if (!overflows) { Assert.Equal(value, value.ToSidi()); return; }
        Assert.Equal("EC-SIZE-OVERFLOW", Assert.Throws<CobolSizeError>(() => value.ToSidi()).EcName);
    }

    [Theory]
    [InlineData("1", -999, false)]   // 10^-999: the smallest nonzero magnitude, inside
    [InlineData("1", -1000, true)]   // 10^-1000: "Underflow is treated as a SIZE ERROR and is not rounded to zero"
    public void ToSidi_RaisesUnderflowBelowTheMinimum(string sig, int exp, bool underflows)
    {
        var value = Digits(sig, exp);
        if (!underflows) { Assert.Equal(value, value.ToSidi()); return; }
        Assert.Equal("EC-SIZE-UNDERFLOW", Assert.Throws<CobolSizeError>(() => value.ToSidi()).EcName);
    }

    [Fact]
    public void ToSidi_UnderProhibitedRaisesTruncationOnlyForAnInexactValue()
    {
        var inexact = Digits("1234567890123456789012345678901234", 0);   // 34 digits: the last two are dropped
        Assert.True(Assert.Throws<CobolSizeError>(() => inexact.ToSidi(CobolRounding.Prohibited)).IsProhibitedInexact);
        var zerosDropped = Digits("1234567890123456789012345678901200", 0);   // the two dropped digits are zeros: exact
        Assert.Equal(Digits("12345678901234567890123456789012", 2), zerosDropped.ToSidi(CobolRounding.Prohibited));
    }

    [Fact]
    public void ToSidiRounded_LeavesAValueOfAtMostThirtyOneDigits()
    {
        var value = Digits("1999999999999999999999999999999", -30);   // 1.999...9 with 31 digits
        Assert.Equal(value, value.ToSidiRounded(CobolRounding.NearestAwayFromZero));
    }

    [Fact]
    public void ToSidiRounded_RoundsThirtyTwoDigitsToThirtyOneAsTheRoundedPhraseDoes()
    {
        // 1.9999999999999999999999999999999 has 32 digits: the dropped 9 rounds up through every nine to 2.000...0
        var nines = Digits("19999999999999999999999999999999", -31);
        Assert.Equal(0, CobolDec.Compare(new CobolDec(2, 0), nines.ToSidiRounded(CobolRounding.NearestAwayFromZero)));
        // A dropped 5 is a tie: NEAREST-AWAY-FROM-ZERO goes up, NEAREST-EVEN goes to the even digit.
        var oddTie = Digits("10000000000000000000000000000015", -31);    // 1.000...015: the kept 31st digit is 1, odd
        Assert.Equal(Digits("1000000000000000000000000000002", -30), oddTie.ToSidiRounded(CobolRounding.NearestAwayFromZero));
        Assert.Equal(Digits("1000000000000000000000000000002", -30), oddTie.ToSidiRounded(CobolRounding.NearestEven));
        var evenTie = Digits("10000000000000000000000000000025", -31);   // the kept digit is 2, even
        Assert.Equal(Digits("1000000000000000000000000000003", -30), evenTie.ToSidiRounded(CobolRounding.NearestAwayFromZero));
        Assert.Equal(Digits("1000000000000000000000000000002", -30), evenTie.ToSidiRounded(CobolRounding.NearestEven));
        // A negative value rounds by magnitude.
        Assert.Equal(Digits("1000000000000000000000000000002", -30, negative: true),
            Digits("10000000000000000000000000000015", -31, negative: true).ToSidiRounded(CobolRounding.NearestAwayFromZero));
    }

    [Fact]
    public void ToSidiRounded_RoundingTheMaximumPastTheRangeIsASizeError()
    {
        // 32 nines at the top of the range round to 10^1000 at 31 digits: "Rounding ... may cause the size error condition".
        var maximum = Digits("99999999999999999999999999999999", 968);
        Assert.Equal("EC-SIZE-OVERFLOW",
            Assert.Throws<CobolSizeError>(() => maximum.ToSidiRounded(CobolRounding.NearestAwayFromZero)).EcName);
    }

    [Fact]
    public void ToSidiRounded_UnderProhibitedKeepsAnExactThirtyTwoDigitValue()
    {
        // §2014:8.8.1.3.1.3.3 lists no 31-digit rounding situation: a 32-digit value is exact in the item (PB5575 R2-1).
        var value = Digits("19999999999999999999999999999999", -31);
        Assert.Equal(value, value.ToSidiRounded(CobolRounding.Prohibited));
    }

    [Fact]
    public void Pow_UnderStandardArithmeticIsTheEquivalentExpressionWithEveryMultiplicationTruncatedToThirtyTwoDigits()
    {
        // §2002:8.8.1.3.6 rule 2c: B ** 3 is ((B * B) * B), each multiplication truncated to 32 digits (§2002:8.8.1.3.4).
        // By hand (decimal, precision 32, round-down): B*B = 96.636535477422589672232728616631; times B = 949.97467433864546250475409034044.
        var b = Digits("9830388368595748906828836075983", -30);
        var cube = CobolDec.Pow(b, new CobolDec(3, 0), CobolRounding.Truncation, standardIntermediate: true).ToSidi();
        Assert.Equal(Digits("94997467433864546250475409034044", -29), cube);
    }
}
