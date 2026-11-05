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
