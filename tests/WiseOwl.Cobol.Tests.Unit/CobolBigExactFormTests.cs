// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE ARBITRARY-PRECISION THIRD FORM OF <see cref="CobolExact"/> AGAINST AN EXACT ORACLE (kb/Work PB4592, PB4587, PB4701, PB4702,
/// PB4730; docs/CONFORMANCE.md DOC-A.1-123): past the 256-bit wide form a native sum, difference, product, truncated quotient and
/// integer power is exact, and every consumer that lowers it reduces the EXACT value to the SDIDI's 34 digits ONCE, by round-to-odd.
/// The oracle is <see cref="BigInteger"/> arithmetic written out in this file, from the value's definition and not from the code
/// under test.
/// </summary>
public sealed class CobolBigExactFormTests
{
    /// <summary>An exact decimal <c>value × 10^-scale</c>.</summary>
    private readonly record struct Dec(BigInteger Value, int Scale);

    private static Dec Parse(string text)
    {
        int point = text.IndexOf('.', StringComparison.Ordinal);
        string digits = point < 0 ? text : text.Remove(point, 1);
        return new(BigInteger.Parse(digits, CultureInfo.InvariantCulture), point < 0 ? 0 : text.Length - point - 1);
    }

    private static CobolExact Exact(Dec d) =>
        d.Value >= Int128.MinValue && d.Value <= Int128.MaxValue
            ? CobolExact.From((Int128)d.Value, d.Scale)
            : throw new InvalidOperationException("an oracle operand is built within Int128");

    private static BigInteger Ten(int n) => BigInteger.Pow(10, n);

    private static int Digits(BigInteger magnitude) => magnitude.IsZero ? 1 : magnitude.ToString(CultureInfo.InvariantCulture).Length;

    /// <summary>The exact value <c>numerator / denominator × 10^exp10</c> reduced ONCE to 34 digits by round-to-odd, as a rational
    /// <c>(significand, exponent)</c>: the oracle of every lowering.</summary>
    private static (BigInteger Significand, int Exponent) RoundToOdd34(BigInteger numerator, BigInteger denominator, int exp10)
    {
        if (numerator.IsZero) return (0, exp10);
        bool negative = numerator.Sign < 0 != denominator.Sign < 0;
        BigInteger n = BigInteger.Abs(numerator), d = BigInteger.Abs(denominator);
        int up = Math.Max(0, 40 + Digits(d) - Digits(n));
        BigInteger q = BigInteger.DivRem(n * Ten(up), d, out BigInteger rem);
        bool sticky = !rem.IsZero;
        int exp = exp10 - up;
        int drop = Math.Max(0, Digits(q) - 34);
        BigInteger kept = BigInteger.DivRem(q, Ten(drop), out BigInteger dropped);
        sticky |= !dropped.IsZero;
        exp += drop;
        if (sticky && kept.IsEven) kept += 1;
        if (kept == Ten(34)) { kept /= 10; exp++; }
        return (negative ? -kept : kept, exp);
    }

    private static void AssertImage(BigInteger numerator, BigInteger denominator, int exp10, CobolExact actual, string what)
    {
        var (significand, exponent) = RoundToOdd34(numerator, denominator, exp10);
        CobolDec image = actual.Image;
        Assert.True(CobolDec.Compare(image, new CobolDec((Int128)significand, exponent)) == 0,
            $"{what}: expected {significand}E{exponent}, got {image.Sig}E{image.Exp}");
    }

    public static IEnumerable<object[]> Bases() =>
        new[] { "2", "3", "7", "-3", "1.01", "2.5", "0.7", "1.5", "-0.3", "123456789.123456789", "1000", "99999999999999999999999999999999" }
            .Select(b => new object[] { b });

    /// <summary>⛔ A POWER IS THE EXACT POWER REDUCED ONCE (kb/Work PB4587), and a negative exponent its exact reciprocal reduced once:
    /// the chain of round-to-odd products it replaces drifted 2 to 8 units in the 34th digit (<c>1.01 ** 119</c> stored …852 for …851).</summary>
    [Theory, MemberData(nameof(Bases))]
    public void Pow_IsTheExactPowerReducedOnce(string text)
    {
        Dec b = Parse(text);
        foreach (int e in new[] { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 100, 119, 181, 200, 297 })
        {
            BigInteger power = BigInteger.Pow(b.Value, e);
            if (Digits(BigInteger.Abs(power)) - b.Scale * e > 6144) continue;   // past the published bound: ThePublishedBound_IsTheSdidiRange
            AssertImage(power, BigInteger.One, -b.Scale * e, CobolExact.Pow(Exact(b), e), $"{text} ** {e}");
            AssertImage(BigInteger.One, power, b.Scale * e, CobolExact.Pow(Exact(b), -e), $"{text} ** -{e}");
        }
    }

    /// <summary>The sum, difference, product and truncated quotient of values past the wide form are exact (kb/Work PB4592, PB4702): the
    /// cancellation of two equal large values leaves nothing, where their 34-digit images left the rounding residue.</summary>
    [Fact]
    public void ArithmeticPastTheWideForm_IsExact()
    {
        Dec a = Parse("7"), b = Parse("3"), c = Parse("1.01");
        CobolExact big7 = CobolExact.Pow(Exact(a), 90), big3 = CobolExact.Pow(Exact(b), 200), small = CobolExact.Pow(Exact(c), 150);
        BigInteger n7 = BigInteger.Pow(7, 90), n3 = BigInteger.Pow(3, 200), n101 = BigInteger.Pow(101, 150);
        // exact sums, differences and products at their common scale
        AssertImage(n7 * Ten(600) + n101, BigInteger.One, -300, CobolExact.Add(CobolExact.Mul(big7, CobolExact.Pow(Exact(Parse("10")), 300)), small), "7^90 * 10^300 + 1.01^150");
        AssertImage(n7 * n3, BigInteger.One, 0, CobolExact.Mul(big7, big3), "7^90 * 3^200");
        AssertImage(n3 - n7, BigInteger.One, 0, CobolExact.Sub(big3, big7), "3^200 - 7^90");
        // cancellation: (A + B) - A is B, exactly
        CobolExact sum = CobolExact.Add(big3, small);
        Assert.Equal(0, CobolExact.Compare(CobolExact.Sub(sum, big3), small));
        Assert.Equal(1, CobolExact.Compare(sum, big3));
        // the truncated quotient keeps every integer digit (PB4702): 10^75 / 3 at scale 10 is an 85-digit integer past the form
        CobolExact quotient = CobolExact.SubsidiaryQuotient(CobolExact.Pow(Exact(Parse("10")), 75), CobolExact.From(3, 0), 10);
        AssertImage(Ten(85) / 3, BigInteger.One, -10, quotient, "10^75 / 3 truncated at 10 places");
        // an exact quotient of exact values is not the quotient of their images (PB4599 sibling): 3^200 / 3^199 = 3
        Assert.Equal(0, CobolDec.Compare(CobolDec.From(3, 0), CobolExact.DivToOdd(big3, CobolExact.Pow(Exact(b), 199), CobolRounding.Truncation)));
    }

    /// <summary>A float ITEM's content is its exact binary expansion (kb/Work PB4730): 2.5E+40 is the 41-digit integer, 0.1 its 55-place expansion.</summary>
    [Fact]
    public void FromBinary64_IsTheExactExpansion()
    {
        // 25000000000000000155002161260194579873792 = 25000000000000000155002161260194579 * 10^6 + 873792
        CobolExact expansion = CobolExact.Add(
            CobolExact.Mul(CobolExact.From(Int128.Parse("25000000000000000155002161260194579", CultureInfo.InvariantCulture), 0), CobolExact.From(1_000_000, 0)),
            CobolExact.From(873792, 0));
        Assert.Equal(0, CobolExact.Compare(CobolExact.FromBinary64(2.5E+40), expansion));
        Assert.Equal(0, CobolExact.Compare(CobolExact.FromBinary64(-2.5E+40), CobolExact.Negate(expansion)));
        // 0.1 is 0.1000000000000000055511151231257827021181583404541015625: its first 34 places end in 7 (odd), so one rounding
        // of the full expansion keeps them, where the place after the 34th is nonzero
        Assert.Equal(new CobolDec(Int128.Parse("1000000000000000055511151231257827", CultureInfo.InvariantCulture), -34), CobolExact.FromBinary64(0.1).Image);
        Assert.Equal(0, CobolExact.Compare(CobolExact.FromBinary64(0.5), CobolExact.From(5, 1)));
        Assert.Equal(0, CobolExact.Compare(CobolExact.FromBinary64(-3.0), CobolExact.From(-3, 0)));
        Assert.Equal(0, CobolExact.Compare(CobolExact.FromBinary64(0.0), CobolExact.From(0, 0)));
        Assert.Throws<CobolSizeError>(() => CobolExact.FromBinary64(double.NaN));
        Assert.Throws<CobolSizeError>(() => CobolExact.FromBinary64(double.PositiveInfinity));
    }

    /// <summary>The final-transfer quotient lands from the EXACT quotient (kb/Work PB4701): a quotient the receiver cannot hold keeps the
    /// exact quotient's own low-order digits, not those of its 34-digit image; checked, it is EC-SIZE-TRUNCATION; PROHIBITED raises on an inexact one.</summary>
    [Fact]
    public void QuotientAtScale_LandsTheExactQuotient()
    {
        // (10^42 + 3000) / 1000 = 10^39 + 3 exactly: its low-order 38 digits are 000…003, where the 34-digit image ends in zeros
        CobolExact dividend = CobolExact.Add(CobolExact.Pow(Exact(Parse("10")), 42), CobolExact.From(3000, 0));
        Assert.Equal((Int128)3, CobolExact.QuotientAtScale(dividend, CobolExact.From(1000, 0), 0, CobolRounding.Truncation, checkedTransfer: false));
        Assert.Throws<CobolSizeError>(() =>
            CobolExact.QuotientAtScale(dividend, CobolExact.From(1000, 0), 0, CobolRounding.Truncation, checkedTransfer: true));
        // a quotient that fits lands rounded once from the true remainder
        Assert.Equal((Int128)3333, CobolExact.QuotientAtScale(CobolExact.From(10000, 0), CobolExact.From(3, 0), 0, CobolRounding.Truncation, false));
        Assert.Equal((Int128)3334, CobolExact.QuotientAtScale(CobolExact.From(10000, 0), CobolExact.From(3, 0), 0, CobolRounding.AwayFromZero, false));
        Assert.Throws<CobolSizeError>(() =>
            CobolExact.QuotientAtScale(CobolExact.From(10000, 0), CobolExact.From(3, 0), 0, CobolRounding.Prohibited, false));
        Assert.Throws<CobolSizeError>(() =>
            CobolExact.QuotientAtScale(dividend, CobolExact.From(0, 0), 0, CobolRounding.Truncation, false));
    }

    /// <summary>The published bound: an exact intermediate is below 10^6145 in magnitude (EC-SIZE-OVERFLOW past it), and a power whose
    /// integer would pass the working cap is refused before it is formed.</summary>
    [Fact]
    public void ThePublishedBound_IsTheSdidiRange()
    {
        CobolExact ten = CobolExact.From(10, 0);
        CobolExact atTheBound = CobolExact.Pow(ten, 6144);
        Assert.Equal(1, atTheBound.Sign);
        Assert.Throws<CobolSizeError>(() => CobolExact.Pow(ten, 6145));
        Assert.Throws<CobolSizeError>(() => CobolExact.Pow(CobolExact.From(2, 0), 100_000_000));
        // a base below one raised past the working cap is too small to be contained, not too large
        CobolSizeError tiny = Assert.Throws<CobolSizeError>(() => CobolExact.Pow(CobolExact.From(5, 1), 100_000_000));
        Assert.Equal("EC-SIZE-UNDERFLOW", tiny.EcName);
        CobolSizeError huge = Assert.Throws<CobolSizeError>(() => CobolExact.Pow(CobolExact.From(5, 0), 100_000_000));
        Assert.Equal("EC-SIZE-OVERFLOW", huge.EcName);
        // a value on the wide form at a negative scale is checked where it is lowered: 10^6145 is past the decimal128 range
        Assert.Throws<CobolSizeError>(() => CobolExact.Mul(atTheBound, CobolExact.From(10, 0)).Image);
    }

    /// <summary>The remainder and integer-part members of the third form (MOD and REM past the wide form, kb/Work PB4592 S2).</summary>
    [Fact]
    public void ModAndRem_PastTheWideForm_AreExact()
    {
        CobolExact p2 = CobolExact.Pow(CobolExact.From(7, 0), 100);   // 7^100, 85 digits
        BigInteger n = BigInteger.Pow(7, 100);
        CobolDec rem = CobolIntrinsics.RemExact(CobolRounding.Truncation, p2, CobolExact.From(1000, 0));
        CobolDec mod = CobolIntrinsics.ModExact(CobolRounding.Truncation, p2, CobolExact.From(1000, 0));
        Assert.Equal((Int128)(n % 1000), rem.Sig);
        Assert.Equal((Int128)(n % 1000), mod.Sig);
        CobolDec negMod = CobolIntrinsics.ModExact(CobolRounding.Truncation, CobolExact.Negate(p2), CobolExact.From(1000, 0));
        Assert.Equal((Int128)(1000 - n % 1000), negMod.Sig);
        CobolDec negRem = CobolIntrinsics.RemExact(CobolRounding.Truncation, CobolExact.Negate(p2), CobolExact.From(1000, 0));
        Assert.Equal((Int128)(-(n % 1000)), negRem.Sig);
    }
    /// <summary>The oracle of a COMPUTED value's one rounding to 34 significant digits in a §14.7.4.3 mode: the digit dropped (the round
    /// digit) and everything below it decide, in integers, from the value's definition.</summary>
    private static BigInteger Round34(BigInteger value, CobolRounding mode)
    {
        bool negative = value.Sign < 0;
        BigInteger magnitude = BigInteger.Abs(value);
        int drop = Digits(magnitude) - 34;
        BigInteger kept = BigInteger.DivRem(magnitude, Ten(drop), out BigInteger tail);
        BigInteger half = 5 * Ten(drop - 1);
        bool up = mode switch
        {
            CobolRounding.Truncation => false,
            CobolRounding.NearestAwayFromZero => tail >= half,
            CobolRounding.NearestEven => tail > half || (tail == half && !kept.IsEven),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
        if (up) kept += 1;
        return (negative ? -kept : kept) * Ten(drop);
    }

    /// <summary>⛔ THE ROUND DIGIT IS READ (review R1-1): a SUM, RANGE, MOD or REM value past the wide form is rounded to 34 digits in the
    /// intermediate mode from the 35th digit and the tail beneath it, as the same value on the wide form is. Cut to exactly 34 digits first,
    /// the digit was gone and NEAREST-AWAY-FROM-ZERO and NEAREST-EVEN never rounded up. The values are 7^k (and their negations) for
    /// a spread of k, and 35-digit numbers ending in 5 followed by zeros (the exact tie), in every mode 11.9.11.1 offers (PROHIBITED raises
    /// on an inexact value and is held below).</summary>
    [Theory]
    [InlineData(CobolRounding.Truncation)]
    [InlineData(CobolRounding.NearestAwayFromZero)]
    [InlineData(CobolRounding.NearestEven)]
    public void ToDecRounded_PastTheWideForm_RoundsFromTheRoundDigit(CobolRounding mode)
    {
        var values = new List<BigInteger>();
        for (int k = 95; k <= 140; k++) values.Add(BigInteger.Pow(7, k));
        BigInteger odd = BigInteger.Parse("12345678901234567890123456789012345", CultureInfo.InvariantCulture);   // 35 digits, last 5: an exact tie
        BigInteger even = BigInteger.Parse("12345678901234567890123456789012355", CultureInfo.InvariantCulture);  // last kept digit odd
        foreach (int zeros in new[] { 50, 60, 100 }) { values.Add(odd * Ten(zeros)); values.Add(even * Ten(zeros)); values.Add(odd * Ten(zeros) + 1); }
        foreach (BigInteger positive in values.ToArray()) values.Add(-positive);
        foreach (BigInteger v in values)
        {
            CobolExact exact = ExactOf(v);
            Assert.True(exact.IsBig, $"{v} is past the wide form");
            CobolDec rounded = exact.ToDecRounded(mode);
            BigInteger expected = Round34(v, mode);
            BigInteger actual = (BigInteger)rounded.Sig * Ten(rounded.Exp);
            Assert.True(expected == actual, $"{mode} of {v}: expected {expected}, got {actual}");
        }
    }

    /// <summary>INTERMEDIATE ROUNDING IS PROHIBITED (§11.9.11.1) raises on a value past the wide form that is inexact in 34 digits and
    /// reduces one that is exact.</summary>
    [Fact]
    public void ToDecRounded_PastTheWideForm_ProhibitedRaisesOnlyOnAnInexactValue()
    {
        CobolExact inexact = ExactOf(BigInteger.Pow(7, 120));
        Assert.Throws<CobolSizeError>(() => inexact.ToDecRounded(CobolRounding.Prohibited));
        CobolExact exact = CobolExact.Mul(ExactOf(BigInteger.Parse("12345678901234567890123456789012345", CultureInfo.InvariantCulture)), CobolExact.Pow(CobolExact.From(10, 0), 60));
        // 35 significant digits ending in 5 then zeros: inexact in 34 digits
        Assert.Throws<CobolSizeError>(() => exact.ToDecRounded(CobolRounding.Prohibited));
        CobolExact fits = CobolExact.Mul(ExactOf(BigInteger.Parse("1234567890123456789012345678901234", CultureInfo.InvariantCulture)), CobolExact.Pow(CobolExact.From(10, 0), 60));
        CobolDec landed = fits.ToDecRounded(CobolRounding.Prohibited);
        Assert.Equal(BigInteger.Parse("1234567890123456789012345678901234", CultureInfo.InvariantCulture) * Ten(60), (BigInteger)landed.Sig * Ten(landed.Exp));
    }

    /// <summary>An integer of any size as an exact value, built from products of <see cref="Int128"/> parts.</summary>
    private static CobolExact ExactOf(BigInteger v)
    {
        if (v.Sign < 0) return CobolExact.Negate(ExactOf(-v));
        CobolExact result = CobolExact.From(0, 0);
        BigInteger chunk = BigInteger.Pow(10, 36);
        CobolExact place = CobolExact.From(1, 0), step = CobolExact.Pow(CobolExact.From(10, 0), 36);
        for (; !v.IsZero; v /= chunk)
        {
            result = CobolExact.Add(result, CobolExact.Mul(CobolExact.From((Int128)(v % chunk), 0), place));
            place = CobolExact.Mul(place, step);
        }
        return result;
    }

    /// <summary>The exact value <c>numerator / denominator</c>'s binary64 neighbors by the oracle: the largest binary64 not above it and
    /// the smallest not below it, from the exact comparison <c>m × 2^e</c> against the value (never a rounding).</summary>
    private static (double Floor, double Ceiling, bool Tie) Binary64Around(BigInteger numerator, BigInteger denominator)
    {
        // the value is numerator / denominator with both positive; compare each candidate c = m × 2^e as c × denominator against numerator
        static int Compare(double c, BigInteger n, BigInteger d)
        {
            long bits = BitConverter.DoubleToInt64Bits(c);
            int exponent = (int)((bits >> 52) & 0x7FF);
            BigInteger m = (bits & 0xFFFFFFFFFFFFFL) | (exponent == 0 ? 0 : 1L << 52);
            int e = (exponent == 0 ? 1 : exponent) - 1075;
            BigInteger left = m * d, right = n;
            if (e >= 0) left <<= e; else right <<= -e;
            return left.CompareTo(right);
        }
        double near = (double)numerator / (double)denominator;   // a starting point only: the neighbors are found by exact comparison
        while (Compare(near, numerator, denominator) > 0) near = Math.BitDecrement(near);
        while (Compare(Math.BitIncrement(near), numerator, denominator) <= 0) near = Math.BitIncrement(near);
        double floor = near, ceiling = Compare(near, numerator, denominator) == 0 ? near : Math.BitIncrement(near);
        bool tie = false;
        if (floor != ceiling)
        {
            // the midpoint of floor and ceiling is (floor + ceiling) / 2, exact in binary64 plus one bit: compare 2 × value against the sum
            BigInteger lowUnits = Units(floor), highUnits = Units(ceiling);
            // value × 2^1075 == lowUnits + highUnits  <=>  numerator × 2^1075 == (lowUnits + highUnits) × denominator
            tie = (numerator << 1075) == (lowUnits + highUnits) * denominator;
        }
        return (floor, ceiling, tie);

        static BigInteger Units(double c)
        {
            long bits = BitConverter.DoubleToInt64Bits(c);
            int exponent = (int)((bits >> 52) & 0x7FF);
            BigInteger m = (bits & 0xFFFFFFFFFFFFFL) | (exponent == 0 ? 0 : 1L << 52);
            int e = (exponent == 0 ? 1 : exponent) - 1075;
            return m << (e + 1074);
        }
    }

    /// <summary>The landing the oracle owes mode <paramref name="mode"/> for a value between two binary64 neighbors.</summary>
    private static double Expected(double floor, double ceiling, bool tie, CobolRounding mode, bool negative, BigInteger numerator, BigInteger denominator)
    {
        if (floor == ceiling) return floor;
        // nearer neighbor by exact comparison of the value against the midpoint
        double nearer;
        if (tie) nearer = double.NaN;
        else
        {
            BigInteger mid = Units(floor) + Units(ceiling);
            nearer = (numerator << 1075).CompareTo(mid * denominator) < 0 ? floor : ceiling;
        }
        return mode switch
        {
            CobolRounding.Truncation => floor,
            CobolRounding.AwayFromZero => ceiling,
            CobolRounding.TowardGreater => negative ? floor : ceiling,   // magnitudes: the negative's greater value is its smaller magnitude
            CobolRounding.TowardLesser => negative ? ceiling : floor,
            CobolRounding.NearestAwayFromZero => tie ? ceiling : nearer,
            CobolRounding.NearestTowardZero => tie ? floor : nearer,
            CobolRounding.NearestEven => tie ? (BitConverter.DoubleToInt64Bits(floor) % 2 == 0 ? floor : ceiling) : nearer,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        static BigInteger Units(double c)
        {
            long bits = BitConverter.DoubleToInt64Bits(c);
            int exponent = (int)((bits >> 52) & 0x7FF);
            BigInteger m = (bits & 0xFFFFFFFFFFFFFL) | (exponent == 0 ? 0 : 1L << 52);
            int e = (exponent == 0 ? 1 : exponent) - 1075;
            return m << (e + 1074);
        }
    }

    public static IEnumerable<object[]> FloatLandingModes() =>
        new[] { CobolRounding.Truncation, CobolRounding.NearestAwayFromZero, CobolRounding.NearestEven, CobolRounding.NearestTowardZero,
                CobolRounding.AwayFromZero, CobolRounding.TowardGreater, CobolRounding.TowardLesser }.Select(m => new object[] { m });

    /// <summary>⛔ AN EXACT VALUE LANDS IN A BINARY64 FROM ALL ITS DIGITS (review R1-3, kb/Work PB4730): the receiver's mode rounds the exact
    /// value ONCE, and a value that is a binary64 lands as itself in every mode. The values are the decimals 0.1, 0.001, 1/3 scaled,
    /// the integers 7^k and their products past the wide form, a float item's 41-digit content (2.5E+40), the exact tie
    /// 1 + 2^-53, and their negations; the expectation is the exact comparison against the binary64 neighbors, never a rounding.</summary>
    [Theory, MemberData(nameof(FloatLandingModes))]
    public void FromExact_LandsFromEveryDigit(CobolRounding mode)
    {
        var cases = new List<(CobolExact Value, BigInteger Numerator, BigInteger Denominator)>
        {
            (CobolExact.From(1, 1), 1, 10),
            (CobolExact.From(1, 3), 1, 1000),
            (CobolExact.From(12345678901234567, 20), 12345678901234567, Ten(20)),
            (CobolExact.Pow(CobolExact.From(7, 0), 100), BigInteger.Pow(7, 100), 1),
            (CobolExact.Pow(CobolExact.From(7, 0), 300), BigInteger.Pow(7, 300), 1),
            (CobolExact.Pow(CobolExact.From(3, 1), 80), BigInteger.Pow(3, 80), Ten(80)),
            (CobolExact.FromBinary64(2.5E+40), (BigInteger)2.5E+40, 1),
            (CobolExact.FromBinary64(0.1), BigInteger.Parse("3602879701896397", CultureInfo.InvariantCulture), BigInteger.One << 55),
            (CobolExact.Add(CobolExact.From(1, 0), CobolExact.Pow(CobolExact.From(5, 1), 53)), (BigInteger.One << 53) + 1, BigInteger.One << 53),
        };
        foreach (var (value, numerator, denominator) in cases)
        {
            foreach (bool negative in new[] { false, true })
            {
                CobolExact signed = negative ? CobolExact.Negate(value) : value;
                var (floor, ceiling, tie) = Binary64Around(numerator, denominator);
                double expected = Expected(floor, ceiling, tie, mode, negative, numerator, denominator);
                double actual = FloatResultant.FromExact(signed, mode, single: false);
                Assert.True((negative ? -expected : expected) == actual,
                    $"{mode} of {(negative ? "-" : "")}{numerator}/{denominator}: expected {(negative ? -expected : expected):R}, got {actual:R}");
            }
        }
    }

    /// <summary>An engine's approximation lands as the binary64 it is, in every mode (its decimal, truncated, would sit one below
    /// 0.001's double); a PROHIBITED landing of an inexact value raises, an exact one does not; a checked landing past the format's range
    /// is the size error; a binary32 receiver rounds once.</summary>
    [Fact]
    public void FromExact_ApproximationsOverflowsProhibitedAndBinary32()
    {
        Assert.Equal(0.001, FloatResultant.FromExact(CobolExact.FromApproximation(0.001), CobolRounding.Truncation, single: false));
        Assert.NotEqual(0.001, FloatResultant.FromExact(CobolExact.From(1, 3), CobolRounding.Truncation, single: false));   // the decimal truncates below it
        Assert.Equal(-0.001, FloatResultant.FromExact(CobolExact.FromApproximation(-0.001), CobolRounding.TowardLesser, single: false));
        Assert.Equal(0.25, FloatResultant.FromExact(CobolExact.From(25, 2), CobolRounding.Prohibited, single: false));       // exactly representable
        Assert.Throws<CobolSizeError>(() => FloatResultant.FromExact(CobolExact.From(1, 1), CobolRounding.Prohibited, single: false));
        // 0.1 into a binary32: truncation lands below it, the nearest above (Python struct)
        Assert.Equal(0.0999999940395355224609375, FloatResultant.FromExact(CobolExact.From(1, 1), CobolRounding.Truncation, single: true));
        Assert.Equal(0.100000001490116119384765625, FloatResultant.FromExact(CobolExact.From(1, 1), CobolRounding.NearestEven, single: true));
        // 10^400 is past the binary64 range: unchecked, the format's overflow result; checked, EC-SIZE-TRUNCATION
        CobolExact huge = CobolExact.Pow(CobolExact.From(10, 0), 400);
        Assert.Equal(double.MaxValue, FloatResultant.FromExact(huge, CobolRounding.Truncation, single: false));
        Assert.Equal(double.PositiveInfinity, FloatResultant.FromExact(huge, CobolRounding.AwayFromZero, single: false));
        Assert.False(FloatResultant.TryFromExact(huge, CobolRounding.Truncation, single: false, out _));
        Assert.Throws<CobolSizeError>(() => FloatResultant.FromExactOrRaise(huge, CobolRounding.Truncation, single: false));
        Assert.Equal(0.0, FloatResultant.FromExact(CobolExact.From(0, 0), CobolRounding.NearestEven, single: false));
    }

    /// <summary>INTEGER, INTEGER-PART and FRACTION-PART of a value past the wide form keep every digit (kb/Work PB4592): a negative value
    /// with a fraction floors one below its truncation, and its fraction part keeps the sign.</summary>
    [Fact]
    public void IntegerAndFractionParts_PastTheWideForm_AreExact()
    {
        CobolExact power = CobolExact.Pow(CobolExact.From(7, 0), 100);                       // 7^100, 85 digits
        CobolExact value = CobolExact.Add(CobolExact.Negate(power), CobolExact.From(-5, 1));  // -(7^100 + 0.5)
        Assert.Equal(0, CobolExact.Compare(CobolIntrinsics.TruncateExact(value), CobolExact.Negate(power)));
        Assert.Equal(0, CobolExact.Compare(CobolIntrinsics.FloorExact(value), CobolExact.Sub(CobolExact.Negate(power), CobolExact.From(1, 0))));
        Assert.Equal(0, CobolExact.Compare(CobolIntrinsics.FractionPartExact(CobolRounding.Truncation, value), CobolExact.From(-5, 1)));
        Assert.Equal(0, CobolExact.Compare(CobolIntrinsics.FloorExact(CobolExact.Negate(power)), CobolExact.Negate(power)));
    }
}
