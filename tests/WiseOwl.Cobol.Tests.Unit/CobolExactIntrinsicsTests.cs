// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE NATIVE SDIDI LANE OVER EXACT ARGUMENTS (kb/Work PB4111, PB4568; docs/CONFORMANCE.md DOC-A.1-123): <see cref="CobolExact"/>
/// and the <c>CobolIntrinsics.*Exact</c> bodies. Every expected value comes from the function's returned-value rule evaluated by an
/// oracle the runtime does not use — <see cref="BigInteger"/> for integers, Python <c>fractions.Fraction</c> divided once by
/// <c>decimal</c> at 34 digits, ROUND_HALF_UP (NEAREST-AWAY-FROM-ZERO, §11.9.11.2 3) a)) for quotients — and each body is also
/// driven past the 256-bit wide form, where it must answer exactly as the SDIDI body over the images does (DOC-A.1-123's
/// determination past 77 digits).
/// </summary>
public sealed class CobolExactIntrinsicsTests
{
    private const CobolRounding Away = CobolRounding.NearestAwayFromZero;

    private static readonly BigInteger Big18 = BigInteger.Pow(10, 18) - 1;

    /// <summary>P = (10^18 − 1)² × 999, the 39-digit product of the PB4111 / PB4568 probes.</summary>
    private static readonly BigInteger P = Big18 * Big18 * 999;

    private static CobolWide W(BigInteger v)
    {
        BigInteger chunk = BigInteger.Pow(10, 26);
        BigInteger magnitude = BigInteger.Abs(v);
        CobolWide w = default;
        foreach (BigInteger limb in new[] { magnitude / chunk / chunk, magnitude / chunk % chunk, magnitude % chunk })
            w = CobolWide.Add(CobolWide.Mul(w, CobolWide.From((Int128)chunk)), CobolWide.From((Int128)limb));
        return v.Sign < 0 ? CobolWide.Negate(w) : w;
    }

    /// <summary>An exact wide argument (an argument expression of 39 to 77 digits) at <paramref name="scale"/>.</summary>
    private static CobolExact Wide(BigInteger v, int scale = 0) => CobolExact.FromWide(W(v), scale);

    private static CobolExact Fixed(long v, int scale = 0) => CobolExact.From(v, scale);

    /// <summary>An SDIDI argument, <paramref name="sig"/> × 10^<paramref name="exp"/>.</summary>
    private static CobolExact Sdidi(string sig, int exp = 0) => CobolExact.FromDec(new CobolDec(Int128.Parse(sig, CultureInfo.InvariantCulture), exp));

    /// <summary>A decimal text as the SDIDI value it names, significand and exponent both (the representation is part of the
    /// answer: an exact quotient is held at its preferred exponent).</summary>
    private static CobolDec D(string text)
    {
        int point = text.IndexOf('.', StringComparison.Ordinal);
        string digits = point < 0 ? text : text.Remove(point, 1);
        return new CobolDec(Int128.Parse(digits, CultureInfo.InvariantCulture), point < 0 ? 0 : -(text.Length - point - 1));
    }

    [Fact]
    public void Compare_IsTheAlgebraicOrder_OnEveryPairOfCarriers()
    {
        CobolExact q = Sdidi("998999999999999998002", 18);                        // P − 999, exactly
        Assert.Equal(1, CobolExact.Compare(Wide(P), q));
        Assert.Equal(-1, CobolExact.Compare(q, Wide(P)));
        Assert.Equal(1, CobolExact.Compare(Wide(P), Wide(P - 1)));                  // equal 34-digit images, ordered exactly
        Assert.Equal(0, CobolExact.Compare(Fixed(1000), Sdidi("1", 3)));
        Assert.Equal(0, CobolExact.Compare(Fixed(25, 1), Sdidi("250", -2)));          // 2.5 at two exponents
        Assert.Equal(1, CobolExact.Compare(Sdidi("1", 6000), Wide(P)));            // past the 256-bit form: larger than any wide value
        Assert.Equal(-1, CobolExact.Compare(Sdidi("-1", 6000), Wide(-P)));
        Assert.Equal(1, CobolExact.Compare(Wide(P), Sdidi("1", -6000)));
    }

    [Fact]
    public void TheSelections_OrderExactly_AndTakeTheLeftmostTie()
    {
        // §15.71.4 2) / §15.72.4 2): the simple-condition order; 3): the leftmost of equal values.
        Assert.Equal(2, CobolIntrinsics.OrdMaxExact(Wide(P - 1), Wide(P), Sdidi("20", -1)));
        Assert.Equal(2, CobolIntrinsics.OrdMinExact(Wide(P), Wide(P - 1), Sdidi("1", 50)));
        Assert.Equal(1, CobolIntrinsics.OrdMaxExact(Fixed(2), Sdidi("20", -1)));
        Assert.Equal(2, CobolIntrinsics.OrdMinExact(Fixed(3), Sdidi("-1", 6000), Sdidi("1", -6000)));
        // MAX's content: an SDIDI as itself; a wide value of 39 digits rounded once to 34 in the mode.
        Assert.Equal(new CobolDec(Int128.Parse("1", CultureInfo.InvariantCulture), 50), CobolIntrinsics.MaxExact(Away, Wide(P), Sdidi("1", 50)));
        Assert.Equal(D("9989999999999999980020000000000000") with { Exp = 5 }, CobolIntrinsics.MaxExact(Away, Wide(P), Sdidi("20", -1)));
        Assert.Equal(D("9989999999999999980020000000000000") with { Exp = 5 },        // P + 98999 ends …0000|99998
            CobolIntrinsics.MaxExact(CobolRounding.Truncation, Wide(P + 98999), Fixed(1)));
        Assert.Equal(D("9989999999999999980020000000000001") with { Exp = 5 },
            CobolIntrinsics.MaxExact(Away, Wide(P + 98999), Fixed(1)));
        // A 38-digit wide value is the native Dec lane's exact operand, unrounded.
        BigInteger d38 = BigInteger.Pow(10, 38) - 1;
        Assert.Equal(new CobolDec(Int128.Parse(d38.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture), 0),
            CobolIntrinsics.MinExact(Away, Wide(d38), Sdidi("1", 50)));
    }

    [Fact]
    public void TheStatistics_CancelExactly_AndRoundOnce()
    {
        CobolExact negQ = Sdidi("-998999999999999998002", 18);
        Assert.Equal(D("999"), CobolIntrinsics.SumExact(Away, Wide(P), negQ));
        Assert.Equal(D("2000"), CobolIntrinsics.RangeExact(Away, Wide(P), Wide(P - 2000), Sdidi("998999999999999998002", 18)));
        Assert.Equal(D("499.5"), CobolIntrinsics.MeanExact(Away, Wide(P), negQ));
        Assert.Equal(D("499.5"), CobolIntrinsics.MidrangeExact(Away, Wide(P), negQ));
        Assert.Equal(D("499.5"), CobolIntrinsics.MedianExact(Away, Wide(P), negQ));
        // An odd count's MEDIAN is the middle argument's content: here the SDIDI P − 999 itself.
        Assert.Equal(new CobolDec(Int128.Parse("998999999999999998002", CultureInfo.InvariantCulture), 18),
            CobolIntrinsics.MedianExact(Away, Wide(P), Wide(P - 2000), Sdidi("998999999999999998002", 18)));
        // MEAN of the 39-digit sum (P + 1) / 2: one rounding of 499499999999999999001000000000000000500 at 34 digits.
        Assert.Equal(D("4994999999999999990010000000000000") with { Exp = 5 }, CobolIntrinsics.MeanExact(Away, Wide(P), Fixed(1)));
    }

    [Fact]
    public void ModAndRem_TakeEveryDigit_WithTheirSignRules()
    {
        // §15.64.4 1) floors (the divisor's sign), §15.77.4 1) truncates (the dividend's): P = 1024 × 975585937499999998048828125000000000 + 999.
        Assert.Equal(D("999"), CobolIntrinsics.ModExact(Away, Wide(P), Sdidi("1024")));
        Assert.Equal(D("999"), CobolIntrinsics.RemExact(Away, Wide(P), Sdidi("1024")));
        Assert.Equal(D("25"), CobolIntrinsics.ModExact(Away, Wide(-P), Sdidi("1024")));
        Assert.Equal(D("-999"), CobolIntrinsics.RemExact(Away, Wide(-P), Sdidi("1024")));
        Assert.Equal(D("-25"), CobolIntrinsics.ModExact(Away, Wide(P), Sdidi("-1024")));
        Assert.Equal((int)(BigInteger.Remainder(P, 7) + 7) % 7, (int)CobolIntrinsics.ModExact(Away, Wide(P), Fixed(7)).Sig);
    }

    /// <summary>⛔ EVERY ARGUMENT AN SDIDI WITH A POSITIVE EXPONENT aligns at the NEGATIVE common scale, exactly (review finding R1-1 of
    /// kb/Work PB4568): <see cref="CobolExact.TryAt"/> refused it and every body fell to the 34-digit images, where 10^40 + 10^5
    /// rounds to 10^40. Expected values are the <see cref="BigInteger"/> evaluation of each equivalent arithmetic expression.</summary>
    [Fact]
    public void PositiveExponentArguments_AlignAtTheirNegativeCommonScale()
    {
        BigInteger e40 = BigInteger.Pow(10, 40);
        CobolExact plus40 = Sdidi("10", 39), minus40 = Sdidi("-10", 39);              // 1.0E40 and -1.0E40, as the literals carry them
        Assert.Equal(0, CobolDec.Compare(D("100000"), CobolIntrinsics.SumExact(Away, plus40, Sdidi("10", 4), minus40)));
        Assert.Equal(0, CobolDec.Compare(D("100000"), CobolIntrinsics.MeanExact(Away, plus40, Sdidi("30", 4), minus40)));
        Assert.Equal(0, CobolDec.Compare(D("150000"), CobolIntrinsics.MedianExact(Away, plus40, Sdidi("10", 4), Sdidi("20", 4), minus40)));
        // §15.64.4 1) floored, §15.77.4 1) truncated: 10^40 mod 300000.
        int mod = (int)BigInteger.Remainder(e40, 300000);
        Assert.Equal(100000, mod);
        Assert.Equal(0, CobolDec.Compare(D("100000"), CobolIntrinsics.ModExact(Away, plus40, Sdidi("30", 4))));
        Assert.Equal(0, CobolDec.Compare(D("100000"), CobolIntrinsics.RemExact(Away, plus40, Sdidi("30", 4))));
        Assert.Equal(0, CobolDec.Compare(D("200000"), CobolIntrinsics.ModExact(Away, minus40, Sdidi("30", 4))));
        Assert.Equal(0, CobolDec.Compare(D("-100000"), CobolIntrinsics.RemExact(Away, minus40, Sdidi("30", 4))));
        // §15.98.4 1): {10^43 + 10^10, 10^43} deviate by ±5·10^9 from the exact mean, so the variance is 2.5·10^19; the 34-digit
        // mean of the images rounds 10^43 + 5·10^9 to 10^43 + 10^10 and answered 5·10^19.
        Assert.Equal(0, CobolDec.Compare(new CobolDec(25, 18),
            CobolIntrinsics.VarianceExact(Away, Sdidi("1000000000000000000000000000000001", 10), Sdidi("1", 43))));
    }

    [Fact]
    public void VarianceAndPresentValue_AreTheExactExpression_DividedOnce()
    {
        Assert.Equal(D("0.25"), CobolIntrinsics.VarianceExact(Away, Wide(P), Wide(P - 1)));
        Assert.Equal(D("0.5"), CobolIntrinsics.StdDevExact(Away, Wide(P), Wide(P - 1)));
        Assert.Equal(D("1.555555555555555555555555555555556"), CobolIntrinsics.VarianceExact(Away, Fixed(1), Fixed(2), Fixed(4)));
        CobolExact e30 = Fixed(1, -30), e30p1 = CobolExact.From(Int128.Parse("1000000000000000000000000000001", CultureInfo.InvariantCulture), 0);
        Assert.Equal(D("0.2222222222222222222222222222222222"), CobolIntrinsics.VarianceExact(Away, e30, e30p1, e30p1));
        Assert.Equal(D("0"), CobolIntrinsics.VarianceExact(Away, Wide(P)));                 // one argument: no deviation
        // §15.74.4 1): 100 / 1.05 + 100 / 1.05²; 1 / 1.5 − 2 / 1.5² + 0.7 / 1.5³; rate 0 sums the amounts.
        Assert.Equal(D("185.9410430839002267573696145124717"), CobolIntrinsics.PresentValueExact(Away, Fixed(5, 2), Fixed(100), Fixed(100)));
        Assert.Equal(D("-0.01481481481481481481481481481481481"),
            CobolIntrinsics.PresentValueExact(Away, Fixed(5, 1), Fixed(1), Fixed(-2), Fixed(7, 1)));
        Assert.Equal(D("1"), CobolIntrinsics.PresentValueExact(Away, Fixed(0), Wide(P), Wide(1 - P)));
        Assert.Equal(D("0.25"), CobolIntrinsics.PresentValueExact(Away, Fixed(3),
            Sdidi("1000000000000000000000000000000001"), Sdidi("-4", 33)));
    }

    [Fact]
    public void PresentValue_ScreensTheExactRate_ThroughTheOneRaiseSite()
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = false;
        try
        {
            Assert.Equal(0, CobolIntrinsics.PresentValueExact(Away, Fixed(-1), Fixed(5)).Sig);                       // §15.74.3 r2: the §15.3 default
            Assert.Equal(0, CobolIntrinsics.PresentValueExact(Away, Sdidi("-1000000000000000000001", -21), Fixed(5)).Sig);
            Assert.NotEqual(0, CobolIntrinsics.PresentValueExact(Away, Sdidi("-999999999999999999999", -21), Fixed(5)).Sig);
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>⛔ Past the 256-bit wide form each body answers as the SDIDI body over the images (DOC-A.1-123 past 77 digits).</summary>
    [Fact]
    public void PastTheWideForm_TheBodyIsTheSdidiBodyOverTheImages()
    {
        CobolExact tiny = Sdidi("1", -6000), huge = Sdidi("1", 100);
        CobolExact[] list = [Fixed(1), tiny, Fixed(3)];
        CobolDec[] images = [D("1"), new CobolDec(1, -6000), D("3")];
        Assert.Equal(CobolIntrinsics.SumDec(Away, images), CobolIntrinsics.SumExact(Away, list));
        Assert.Equal(CobolIntrinsics.MeanDec(Away, images), CobolIntrinsics.MeanExact(Away, list));
        Assert.Equal(CobolIntrinsics.VarianceDec(Away, images), CobolIntrinsics.VarianceExact(Away, list));
        Assert.Equal(CobolIntrinsics.ModDec(Away, D("5"), new CobolDec(1, 100)), CobolIntrinsics.ModExact(Away, Fixed(5), huge));
        // A rate of 70 places: the powers of 10^70 leave the wide form at two periods.
        CobolExact rate = CobolExact.From(1, 70);
        Assert.Equal(CobolIntrinsics.PresentValueDec(Away, new CobolDec(1, -70), D("2"), D("3")),
            CobolIntrinsics.PresentValueExact(Away, rate, Fixed(2), Fixed(3)));
        // The ordering never needs the fallback.
        Assert.Equal(2, CobolIntrinsics.OrdMinExact(Fixed(1), tiny, Fixed(3)));
    }
}
