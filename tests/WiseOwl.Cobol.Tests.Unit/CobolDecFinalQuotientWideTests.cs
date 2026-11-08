// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FLOATING-POINT EDITED RECEIVER OF 33 TO 36 SIGNIFICAND DIGITS STILL ROUNDS THE FINAL-TRANSFER QUOTIENT ONCE (kb/Work PB4975).
/// <para>Round-to-odd at 34 digits serves the receiver's one rounding only while the receiver is at least two digits coarser, so
/// <c>CobolDec.DivFinal</c> and <c>CobolExact.DivToOdd</c> keep the receiver's significand plus two digits when it passes 32
/// (<c>CobolBig.QuotientToOdd</c>, an <see cref="Int128"/> significand of up to 38 digits). The exact quotients below are derived
/// with Python <c>fractions</c>.</para>
/// </summary>
public sealed class CobolDecFinalQuotientWideTests
{
    private static CobolDec Dec(string digits, int exp) => new(Int128.Parse(digits), exp);

    // 0.9999999999999999999999999999999999 / 2 = 0.49999999999999999999999999999999995 (35 digits, exact).
    private static readonly CobolDec NearlyOne = Dec("9999999999999999999999999999999999", -34);
    private static readonly CobolDec Two = Dec("20", -1);

    [Theory]
    [InlineData(33)]
    [InlineData(34)]
    [InlineData(35)]
    [InlineData(36)]
    public void An_exact_quotient_of_35_digits_is_kept_whole_for_a_wide_significand(int significand)
    {
        CobolDec q = CobolDec.DivFinal(NearlyOne, Two, CobolRounding.NearestEven, significand);
        Assert.Equal(0, CobolDec.Compare(Dec("49999999999999999999999999999999995", -35), q));
    }

    [Fact]
    public void A_narrow_significand_keeps_the_34_digit_round_to_odd_quotient()
    {
        // 35 digits do not fit the 34: the exact tail 5 is kept as an odd last digit of the 34-digit value, never rounded away.
        CobolDec narrow = CobolDec.DivFinal(NearlyOne, Two, CobolRounding.NearestEven, 32);
        Assert.Equal(CobolDec.DivFinal(NearlyOne, Two, CobolRounding.NearestEven), narrow);
    }

    [Theory]
    [InlineData(1, 3, 36, "33333333333333333333333333333333333333")]   // odd last digit stays
    [InlineData(2, 3, 36, "66666666666666666666666666666666666667")]   // inexact with an even last digit: jammed odd
    [InlineData(2, 3, 33, "66666666666666666666666666666666667")]      // 33 + 2 = 35 digits
    public void An_inexact_quotient_keeps_an_odd_last_digit_at_the_significand_plus_two(int dividend, int divisor, int significand, string expected)
    {
        CobolDec q = CobolDec.DivFinal(Dec(dividend.ToString(), 0), Dec(divisor.ToString(), 0), CobolRounding.NearestEven, significand);
        Assert.Equal(Int128.Parse(expected), q.Sig);
        Assert.Equal(expected.Length, BigInteger.Abs(q.Sig).ToString().Length);
        Assert.Equal(-expected.Length, q.Exp);
    }

    [Fact]
    public void A_negative_quotient_and_a_zero_dividend_follow_the_preferred_exponent()
    {
        CobolDec negative = CobolDec.DivFinal(Dec("-1", 0), Dec("3", 0), CobolRounding.Truncation, 36);
        Assert.Equal(-Int128.Parse("33333333333333333333333333333333333333"), negative.Sig);
        CobolDec zero = CobolDec.DivFinal(Dec("0", -2), Dec("3", 1), CobolRounding.Truncation, 36);
        Assert.Equal(0, zero.Sig);
        Assert.Equal(-3, zero.Exp);
    }

    [Fact]
    public void A_zero_divisor_raises_and_PROHIBITED_keeps_its_screen()
    {
        Assert.Throws<CobolSizeError>(() => CobolDec.DivFinal(Dec("1", 0), Dec("0", 0), CobolRounding.NearestEven, 36));
        Assert.Throws<CobolSizeError>(() => CobolDec.DivFinal(Dec("1", 0), Dec("3", 0), CobolRounding.Prohibited, 36));
    }

    [Fact]
    public void The_exact_form_divides_the_exact_values_to_the_same_width()
    {
        // A = 123456789012345678901: A * A / 7 = 2.17736839331983382149106193793237093(142857...)E+39.
        BigInteger a = BigInteger.Parse("123456789012345678901");
        CobolExact value = CobolExact.Mul(CobolExact.From((Int128)a, 0), CobolExact.From((Int128)a, 0));
        CobolDec q = CobolExact.DivToOdd(value, CobolExact.From(7, 0), CobolRounding.Truncation, 36);
        Assert.Equal(Int128.Parse("21773683933198338214910619379323709383"), q.Sig);
        Assert.Equal(2, q.Exp);
    }
}
