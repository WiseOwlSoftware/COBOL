// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB4569 - a native INTEGER base raised to an integer exponent is the exact power (<c>CobolExact.Pow</c>: exact on the
/// 256-bit wide form while the VALUE fits its 77 digits, <c>CobolIntrinsics.PowNativeDec</c>'s round-to-odd chain past it),
/// never the binary64 fallback the former <c>PowNativeIntDec</c> took past the <see cref="Int128"/> carrier (NUMERIC_DESIGN D19
/// (a), superseded by the owner on 2026-11-04). §8.8.1.3 leaves native evaluation to the implementor, so CLAUDE.md rule 1 sends
/// it to GnuCOBOL, whose run-time power is an exact <c>mpz_pow_ui</c>. Expected values are <see cref="BigInteger"/> powers, an
/// arbitrary-precision oracle independent of the wide form.
/// <para>The bases 0, 1 and -1 return at once whatever the exponent (kb/Work PB2632): <c>B ** 999999999999999999</c> is a
/// constant, and a regression is a test run that does not return, which the gate's test-host timeout reports (a wall-clock
/// assertion in the test itself is forbidden). The §8.8.1.2 rule 6 screen is unchanged: <c>0 ** 0</c> and a zero base with a
/// negative exponent are EC-SIZE-EXPONENTIATION.</para>
/// </summary>
public sealed class NativeIntegerPowerExactTests
{
    private static readonly BigInteger Pow20 = BigInteger.Pow(10, 20);

    /// <summary>The oracle value as a <see cref="CobolExact"/>, built from its base-10^20 chunks (Horner) by exact multiply and add.</summary>
    private static CobolExact Expected(BigInteger v)
    {
        CobolExact acc = CobolExact.From(0, 0), radix = CobolExact.From((Int128)Pow20, 0);
        BigInteger magnitude = BigInteger.Abs(v);
        var chunks = new Stack<Int128>();
        do
        {
            magnitude = BigInteger.DivRem(magnitude, Pow20, out BigInteger chunk);
            chunks.Push((Int128)chunk);
        } while (!magnitude.IsZero);
        foreach (Int128 chunk in chunks)
            acc = CobolExact.Add(CobolExact.Mul(acc, radix, CobolRounding.Truncation), CobolExact.From(chunk, 0), CobolRounding.Truncation);
        return v.Sign < 0 ? CobolExact.Negate(acc) : acc;
    }

    private static CobolExact Pow(Int128 b, Int128 e) => CobolExact.Pow(CobolExact.From(b, 0), e, CobolRounding.Truncation);

    [Theory]
    [InlineData(1, 999999999999999999, 1)]
    [InlineData(1, 999999999999999998, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(0, 999999999999999999, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(-1, 999999999999999999, -1)]
    [InlineData(-1, 999999999999999998, 1)]
    [InlineData(-1, 0, 1)]
    [InlineData(-1, 1, -1)]
    public void ABaseOfZeroOneOrMinusOne_IsAnswered_WithoutLoopingOverTheExponent(long b, long e, long expected) =>
        Assert.Equal(0, CobolExact.Compare(Expected(expected), Pow(b, e)));

    [Fact]
    public void ABaseOfOneOrMinusOne_WithAnExponentBeyondLong_StillAnswersAtOnce()
    {
        Int128 e = Int128.Parse("99999999999999999999999999999999999999");
        Assert.Equal(0, CobolExact.Compare(Expected(1), Pow(1, e)));
        Assert.Equal(0, CobolExact.Compare(Expected(-1), Pow(-1, e)));
        Assert.Equal(0, CobolExact.Compare(Expected(1), Pow(-1, e - 1)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public void AZeroBaseWithoutAPositiveExponent_RaisesTheExponentiationSizeError(long b, long e) =>
        Assert.Throws<CobolSizeError>(() => Pow(b, e));

    /// <summary>Exact while the VALUE fits the wide form's 77 digits: the Int128 carrier's last powers (2^126, (-2)^125), the
    /// pieces past it that the former double approximation landed at 16 or 17 digits (2^130 is 40 digits, 3^81 39), and the
    /// widest the form holds (7^90 is 76 digits).</summary>
    [Theory]
    [InlineData(2, 10)]
    [InlineData(-3, 3)]
    [InlineData(7, 40)]
    [InlineData(2, 126)]
    [InlineData(-2, 125)]
    [InlineData(10, 38)]
    [InlineData(2, 130)]
    [InlineData(-2, 131)]
    [InlineData(3, 81)]
    [InlineData(2, 200)]
    [InlineData(7, 90)]
    public void AnIntegerBase_IsExact_WhileTheValueFitsTheWideForm(long b, int e) =>
        Assert.Equal(0, CobolExact.Compare(Expected(BigInteger.Pow(b, e)), Pow(b, e)));

    /// <summary>|Int128.MinValue| = 2^127 is past the carrier's positive range (kb/Work PB2698): its magnitude comes from the
    /// wide form, not <c>Int128.Abs</c>, so its first two powers are exact (39 and 77 digits).</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void TheInt128MinValueBase_IsExactAndNeverAnOverflowException(int exponent) =>
        Assert.Equal(0, CobolExact.Compare(Expected(BigInteger.Pow(Int128.MinValue, exponent)), Pow(Int128.MinValue, exponent)));

    /// <summary>Past 77 digits the SDIDI chain takes over (34 digits, round-to-odd): the value is the power to within the image's
    /// 34th digit, and Int128.MinValue cubed (116 digits) does not throw.</summary>
    [Theory]
    [InlineData(2, 300)]
    [InlineData(-3, 201)]
    public void APowerPastTheWideForm_IsTheRoundToOddImage(long b, int e)
    {
        double expected = (double)BigInteger.Pow(b, e);
        Assert.InRange(Pow(b, e).ToDouble() / expected, 1 - 1e-12, 1 + 1e-12);
    }

    [Fact]
    public void TheInt128MinValueBaseCubed_IsTheRoundToOddImage_NotAnOverflowException()
    {
        double expected = (double)BigInteger.Pow(Int128.MinValue, 3);
        Assert.InRange(Pow(Int128.MinValue, 3).ToDouble() / expected, 1 - 1e-12, 1 + 1e-12);
    }

    /// <summary>§8.8.1.5.4 r3's reciprocal: 3 ** -1 is 1 / 3 to the SDIDI's 34 digits where the former double fallback kept 16.</summary>
    [Fact]
    public void ANegativeExponent_IsTheSdidiReciprocal_To34Digits()
    {
        CobolDec third = Pow(3, -1).Image;
        Assert.Equal(34, CobolDec.FractionDigits(third));
        Assert.Equal(BigInteger.Parse("3333333333333333333333333333333333"), (BigInteger)third.Sig);
    }
}
