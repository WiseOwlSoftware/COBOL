// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The ONE float-data-item → standard-decimal-intermediate conversion (kb/Work PB1606): ISO §8.8.1.5.1 leaves "the
/// mechanisms for conversion of data items described with a usage of float-short, float-long, or float-extended into
/// standard-decimal intermediate data items" to the implementor, and §8.8.1.5.2 r1 rounds an operand value that "cannot
/// be expressed exactly in an SDIDI" by the INTERMEDIATE ROUNDING rules (cite.py --check, both OK). WiseOwl COBOL's
/// determination is the binary64's exact value rounded once to 34 digits under the mode
/// (<see cref="CobolDec.FromBinary64"/>); an implementor-defined approximation is its shortest round-trip decimal
/// (<see cref="CobolDec.FromApproximation"/>). Expected values are computed OUTSIDE the runtime: the table from exact
/// rational arithmetic (Python <c>Fraction</c>/<c>Decimal</c>, the derivation in the case headers), the property test from
/// a <see cref="BigInteger"/> division in this file (the runtime itself bans that type, so it is an independent oracle).
/// </summary>
public sealed class CobolDecBinary64ConversionTests
{
    private static CobolDec Dec(string sig, int exp) => new(Int128.Parse(sig), exp);

    // 0.1 = 0.1000000000000000055511151231257827021181583404541015625 (55 digits); the 35th digit is 0, so every mode
    // that rounds to nearest keeps 34 digits and TRUNCATION does too.
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero)]
    [InlineData(CobolRounding.NearestEven)]
    [InlineData(CobolRounding.Truncation)]
    public void PointOne_IsItsExactValueRoundedToThirtyFourDigits(CobolRounding mode) =>
        Assert.Equal(Dec("1000000000000000055511151231257827", -34), CobolDec.FromBinary64(0.1, mode));

    // 0.3 = 0.299999999999999988897769753748434595763683319091796875: the 35th digit is 9 - the modes part here.
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, "2999999999999999888977697537484346")]
    [InlineData(CobolRounding.NearestEven, "2999999999999999888977697537484346")]
    [InlineData(CobolRounding.Truncation, "2999999999999999888977697537484345")]
    public void PointThree_RoundsAtTheThirtyFifthDigitPerMode(CobolRounding mode, string sig) =>
        Assert.Equal(Dec(sig, -34), CobolDec.FromBinary64(0.3, mode));

    // 2^-50 = 8.8817841970012523233890533447265625E-16: 35 digits, the 35th a 5 - an exact TIE. The kept digit 2 is even.
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, "8881784197001252323389053344726563")]
    [InlineData(CobolRounding.NearestEven, "8881784197001252323389053344726562")]
    [InlineData(CobolRounding.Truncation, "8881784197001252323389053344726562")]
    public void ExactTie_KeptDigitEven(CobolRounding mode, string sig) =>
        Assert.Equal(Dec(sig, -49), CobolDec.FromBinary64(Math.Pow(2, -50), mode));

    // 3 * 2^-48 = 1.0658141036401502788066864013671875E-14: a tie whose kept digit 7 is ODD.
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero, "1065814103640150278806686401367188")]
    [InlineData(CobolRounding.NearestEven, "1065814103640150278806686401367188")]
    [InlineData(CobolRounding.Truncation, "1065814103640150278806686401367187")]
    public void ExactTie_KeptDigitOdd(CobolRounding mode, string sig) =>
        Assert.Equal(Dec(sig, -47), CobolDec.FromBinary64(3 * Math.Pow(2, -48), mode));

    [Fact]
    public void NegativeValues_RoundByMagnitude() =>
        Assert.Equal(Dec("-2899999999999999800159855567471823", -34),
            CobolDec.FromBinary64(-0.29, CobolRounding.NearestAwayFromZero));

    // A value that fits 34 digits is held at the fewest places that carry it, as the shortest form was: 0.5 is 5 x 10^-1.
    [Theory]
    [InlineData(0.5, "5", -1)]
    [InlineData(0.25, "25", -2)]
    [InlineData(12345.0, "12345", 0)]
    [InlineData(-3.0, "-3", 0)]
    [InlineData(1.0e22, "1", 22)]
    [InlineData(1.5, "15", -1)]
    public void ExactValues_AreHeldAtTheirOwnPlaces(double d, string sig, int exp) =>
        Assert.Equal(Dec(sig, exp), CobolDec.FromBinary64(d, CobolRounding.NearestAwayFromZero));

    [Fact]
    public void LargeIntegerValuedDouble_IsItsExactIntegerNotItsDecimalSpelling() =>
        Assert.Equal(Dec("1000000000000000019884624838656", 0),
            CobolDec.FromBinary64(1.0e30, CobolRounding.NearestAwayFromZero));   // 1.0E+30 as a double is 2^99.66 -> this integer

    [Fact]
    public void BinaryExtremes_AreRoundedAtTheirOwnMagnitude()
    {
        // 2^-1074 = 4.94065645841246544176568792868221372365059802...E-324 (751 digits): the 35th digit is 3
        Assert.Equal(Dec("4940656458412465441765687928682214", -357), CobolDec.FromBinary64(double.Epsilon, CobolRounding.NearestAwayFromZero));
        // double.MaxValue = 1.7976931348623157081452742373170435679807...E+308: the 35th digit is 5, then 679...
        Assert.Equal(Dec("1797693134862315708145274237317044", 275), CobolDec.FromBinary64(double.MaxValue, CobolRounding.NearestAwayFromZero));
        Assert.Equal(Dec("1797693134862315708145274237317043", 275), CobolDec.FromBinary64(double.MaxValue, CobolRounding.Truncation));
    }

    [Fact]
    public void Zero_IsZero_WhateverItsSign()
    {
        Assert.Equal(0, CobolDec.FromBinary64(0.0, CobolRounding.NearestAwayFromZero).Sig);
        Assert.Equal(0, CobolDec.FromBinary64(-0.0, CobolRounding.NearestAwayFromZero).Sig);
    }

    // §11.9.11.2 r3 d): PROHIBITED and a value "cannot be represented exactly in SDIDI form" -> EC-SIZE-TRUNCATION.
    [Theory]
    [InlineData(0.1)]
    [InlineData(0.29)]
    [InlineData(1.0 / 3)]
    public void Prohibited_InexactValue_IsTheTruncationCondition(double d)
    {
        var e = Assert.Throws<CobolSizeError>(() => CobolDec.FromBinary64(d, CobolRounding.Prohibited));
        Assert.Equal("EC-SIZE-TRUNCATION", e.EcName);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(-0.375)]
    [InlineData(12345.0)]
    [InlineData(1.0e22)]
    public void Prohibited_ExactValue_Passes(double d) =>
        Assert.Equal(0, CobolDec.Compare(CobolDec.FromBinary64(d, CobolRounding.Truncation),
            CobolDec.FromBinary64(d, CobolRounding.Prohibited)));

    [Fact]
    public void Prohibited_ThirtyFiveDigitExpansion_IsInexact() =>
        Assert.Throws<CobolSizeError>(() => CobolDec.FromBinary64(Math.Pow(2, -50), CobolRounding.Prohibited));

    [Fact]
    public void NonFinite_FollowsTheSdidiRules()
    {
        Assert.Equal("EC-DATA-INCOMPATIBLE", Assert.Throws<CobolSizeError>(() => CobolDec.FromBinary64(double.NaN, CobolRounding.NearestAwayFromZero)).EcName);
        Assert.Equal("EC-SIZE-OVERFLOW", Assert.Throws<CobolSizeError>(() => CobolDec.FromBinary64(double.PositiveInfinity, CobolRounding.NearestAwayFromZero)).EcName);
        Assert.Equal("EC-SIZE-OVERFLOW", Assert.Throws<CobolSizeError>(() => CobolDec.FromBinary64(double.NegativeInfinity, CobolRounding.NearestAwayFromZero)).EcName);
    }

    // ── The independent oracle ───────────────────────────────────────────────────────────────────────────────────

    private static readonly BigInteger Ten33 = BigInteger.Pow(10, 33), Ten34 = BigInteger.Pow(10, 34);

    /// <summary>The binary64's exact value rounded once to 34 significant digits, by BigInteger division.</summary>
    private static (BigInteger Sig, int Exp, bool Inexact) Oracle(double d, CobolRounding mode)
    {
        long bits = BitConverter.DoubleToInt64Bits(d);
        bool neg = bits < 0;
        int biased = (int)((bits >> 52) & 0x7FF);
        long man = bits & 0xF_FFFF_FFFF_FFFFL;
        if (biased == 0) biased = 1; else man |= 1L << 52;
        int e2 = biased - 1075;                       // |d| = man x 2^e2
        BigInteger num = man, den = BigInteger.One;
        if (e2 >= 0) num <<= e2; else den <<= -e2;
        int s = 33 - (int)Math.Floor(Math.Log10(Math.Abs(d)));   // |d| x 10^s lies near [10^33, 10^34)
        while (true)
        {
            BigInteger n = s >= 0 ? num * BigInteger.Pow(10, s) : num;
            BigInteger m = s >= 0 ? den : den * BigInteger.Pow(10, -s);
            BigInteger q = BigInteger.DivRem(n, m, out BigInteger r);
            if (q >= Ten34) { s--; continue; }
            if (q < Ten33) { s++; continue; }
            bool inexact = !r.IsZero;
            if (inexact)
            {
                BigInteger twice = r * 2;
                bool up = mode switch
                {
                    CobolRounding.Truncation => false,
                    CobolRounding.NearestEven => twice > m || (twice == m && !q.IsEven),
                    _ => twice >= m,
                };
                if (up) q += 1;
            }
            int exp = -s;
            if (q == Ten34) { q = Ten33; exp++; }
            return (neg ? -q : q, exp, inexact);
        }
    }

    private static (BigInteger Sig, int Exp) Canonical(BigInteger sig, int exp)
    {
        if (sig.IsZero) return (0, 0);
        while ((sig % 10).IsZero) { sig /= 10; exp++; }
        return (sig, exp);
    }

    public static IEnumerable<object[]> RandomDoubles()
    {
        var rng = new Random(1606);
        var buf = new byte[8];
        int made = 0;
        while (made < 600)
        {
            rng.NextBytes(buf);
            double d = BitConverter.ToDouble(buf, 0);
            if (!double.IsFinite(d) || d == 0) continue;
            made++;
            yield return new object[] { d };
        }
        // decimal-looking values: the shapes programs hold
        foreach (double d in new[] { 0.1, 0.2, 0.3, 0.7, 1.1, 2.35, 8.2, 1.15, 9.99, 123456.789, 1e-7, 1e15, 1e16, 1e17, 1e23, 5e-324, 2.2250738585072014E-308 })
            yield return new object[] { d };
        // ties and near ties around the 34th digit
        for (int k = 40; k <= 60; k++) yield return new object[] { Math.Pow(2, -k) };
        for (int k = 40; k <= 60; k++) yield return new object[] { 3 * Math.Pow(2, -k) };
        for (int k = 40; k <= 60; k++) yield return new object[] { 5 * Math.Pow(2, -k) };
    }

    [Theory]
    [MemberData(nameof(RandomDoubles))]
    public void EveryDouble_MatchesTheIndependentOracle_InEveryMode(double d)
    {
        foreach (CobolRounding mode in new[] { CobolRounding.NearestAwayFromZero, CobolRounding.NearestEven, CobolRounding.Truncation })
        {
            var (sig, exp, _) = Oracle(d, mode);
            CobolDec got = CobolDec.FromBinary64(d, mode);
            Assert.Equal(Canonical(sig, exp), Canonical((BigInteger)got.Sig, got.Exp));
            Assert.True(BigInteger.Abs((BigInteger)got.Sig) < Ten34, $"{d:R} {mode}: a {got.Sig} significand is past the SDIDI's 34 digits");
        }
    }

    [Theory]
    [MemberData(nameof(RandomDoubles))]
    public void Prohibited_RaisesExactlyWhenTheExpansionPastThirtyFourDigitsIsNonzero(double d)
    {
        bool inexact = Oracle(d, CobolRounding.Truncation).Inexact;
        if (inexact) Assert.Throws<CobolSizeError>(() => CobolDec.FromBinary64(d, CobolRounding.Prohibited));
        else Assert.Equal(0, CobolDec.Compare(CobolDec.FromBinary64(d, CobolRounding.Prohibited), CobolDec.FromBinary64(d, CobolRounding.Truncation)));
    }

    // ── The floating-point numeric-edited sender's form: the exact value as a round-to-odd 38-digit significand ───

    [Theory]
    [MemberData(nameof(RandomDoubles))]
    public void RoundToOdd_KeepsTheValueOnTheSameSideOfEveryBoundaryAThirtySixDigitCutHas(double d)
    {
        CobolDec odd = CobolDec.FromBinary64ToOdd(d);
        Assert.True(BigInteger.Abs((BigInteger)odd.Sig) < BigInteger.Pow(10, 38));
        // Rounding the folded significand to 34 digits agrees with rounding the exact value, in every mode.
        foreach (CobolRounding mode in new[] { CobolRounding.NearestAwayFromZero, CobolRounding.NearestEven, CobolRounding.Truncation })
        {
            var (sig, exp, _) = Oracle(d, mode);
            CobolDec viaOdd = CobolDec.FromParsed(odd.Sig, odd.Exp, mode);
            Assert.Equal(Canonical(sig, exp), Canonical((BigInteger)viaOdd.Sig, viaOdd.Exp));
        }
    }

    // ── The approximation lift keeps the shortest round-trip decimal ─────────────────────────────────────────────

    [Theory]
    [InlineData(0.1, "1", -1)]
    [InlineData(0.8414709848078965, "8414709848078965", -16)]
    [InlineData(1.4142135623730951, "14142135623730951", -16)]
    [InlineData(1.0e30, "1", 30)]
    [InlineData(-2.5, "-25", -1)]
    public void Approximation_IsTheShortestRoundTripDecimal(double d, string sig, int exp) =>
        Assert.Equal(Dec(sig, exp), CobolDec.FromApproximation(d));

    [Fact]
    public void Approximation_NonFinite_FollowsTheSdidiRules()
    {
        Assert.Equal("EC-DATA-INCOMPATIBLE", Assert.Throws<CobolSizeError>(() => CobolDec.FromApproximation(double.NaN)).EcName);
        Assert.Equal("EC-SIZE-OVERFLOW", Assert.Throws<CobolSizeError>(() => CobolDec.FromApproximation(double.PositiveInfinity)).EcName);
    }
}
