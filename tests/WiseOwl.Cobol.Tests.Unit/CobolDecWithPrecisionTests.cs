// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN SDIDI OPERAND WHOSE PRECISION ONLY THE RUN TIME KNOWS HAS THE FRACTION DIGITS IT IS HELD AT — WRITTEN, DECLARED OR
/// FORMED, NEVER A COUNT OF ITS VALUE'S SIGNIFICANT DIGITS — AND A NESTED QUOTIENT CARRIES ITS OPERANDS' PRECISION, NEVER ITS
/// WORKING SCALE (kb/Work PB3538 review findings R3-1 and R4-1; CONFORMANCE.md DOC-A.1-123). A NUMVAL value, a power or a
/// function's result was given the precision 0, so a quotient of 10^-15 was truncated at 14 places, to zero; and counting
/// significant digits dropped the trailing zero of `NUMVAL ("2.50")`, whose quotient was truncated one place sooner than
/// `2.50`'s. The expected values are the rule's: the working scale is <c>max(receiver scale, precision) + 14</c>
/// (<see cref="CobolNum.NestedQuotientScale"/>), the quotient is truncated there, a sum or difference carries the larger
/// precision and a product the sum, and the scaled lane (<see cref="CobolNum.Divide"/> at the same scale, TRUNCATION)
/// forms the same value from an item of that precision.
/// </summary>
public sealed class CobolDecWithPrecisionTests
{
    [Theory]
    [InlineData(5, -16, 16)]     // 5 × 10^-16
    [InlineData(50, -17, 17)]    // 5.0 × 10^-16, held at 17 places: the scale it is written at, not its significant digits
    [InlineData(250, -2, 2)]     // 2.50E0 and NUMVAL ("2.50"): the 2 of 2.50 (review finding R4-1)
    [InlineData(20, -1, 1)]      // 2.0E0: the 1 of 2.0
    [InlineData(-15, -3, 3)]     // -0.015
    [InlineData(123, 2, 0)]      // 12300
    [InlineData(0, -5, 5)]       // 0.00000: a zero keeps its written places
    public void FractionDigits_AreThePlacesTheValueIsHeldAt(long sig, int exp, int expected) =>
        Assert.Equal(expected, CobolDec.FractionDigits(new CobolDec(sig, exp)));

    /// <summary>The NUMVAL family lifts its argument EXACTLY at the written scale, which is what makes
    /// <see cref="CobolDecWithPrecision.Own"/> of its value the argument's written places (§15.67.4 1) and §15.68.4 1):
    /// "The returned value is the numeric value represented by argument-1"; NUMVAL-F's SDIDI arm likewise). The value is the
    /// standard's; the precision a nested native quotient reads from it is this implementation's (DOC-A.1-123).</summary>
    [Theory]
    [InlineData("2.50", 2)]
    [InlineData("2.5", 1)]
    [InlineData("0.000000000000001", 15)]
    [InlineData("2", 0)]
    [InlineData("-0.10", 2)]
    public void ANumvalValuesPrecision_IsItsArgumentsWrittenPlaces(string text, int expected)
    {
        Assert.Equal(expected, CobolDecWithPrecision.Own(CobolIntrinsics.NumvalDec(text)).Precision);
        Assert.Equal(expected, CobolDecWithPrecision.Own(CobolIntrinsics.NumvalCDec(text, "$")).Precision);
        Assert.Equal(expected, CobolDecWithPrecision.Own(CobolIntrinsics.NumvalFDec(CobolRounding.NearestAwayFromZero, text)).Precision);
    }

    /// <summary>Review finding R4-1's relation: `NUMVAL ("2.50") / 3 * 1000000000000000 > 833333333333333` with no receiver.
    /// The precision is 2, so w = 16 and the quotient is 0.8333333333333333 (16 places), whose product is
    /// 833333333333333.3: GREATER, as for `2.50` (precision 1 gave w = 15 and exactly 833333333333333, NOT GREATER).</summary>
    [Fact]
    public void ATrailingZero_IsAPlaceOfThePrecision()
    {
        var q = CobolDecWithPrecision.DivTruncatedToOdd(CobolDecWithPrecision.Own(CobolIntrinsics.NumvalDec("2.50")), Known(3, 0, 0), 0);
        Assert.Equal(2, q.Precision);
        Assert.Equal(0, CobolDec.Compare(q.Value, new CobolDec(8333333333333333, -16)));
        var product = CobolDecWithPrecision.Mul(q, Known(1000000000000000, 0, 0), CobolRounding.NearestAwayFromZero);
        Assert.Equal(1, CobolDec.Compare(product.Value, new CobolDec(833333333333333, 0)));
    }

    [Theory]
    [InlineData(0, 0, 14)]       // a relation (no receiver) over operands of no fraction digits
    [InlineData(6, 0, 20)]       // PIC S9(9)V9(6)
    [InlineData(6, 15, 29)]      // the operands' 15 digits pass the receiver's 6
    [InlineData(0, 15, 29)]
    public void TheWorkingScale_IsTheLargerOfReceiverAndPrecision_PlusTheGuard(int receiverScale, int precision, int expected) =>
        Assert.Equal(expected, CobolNum.NestedQuotientScale(receiverScale, precision));

    private static CobolDecWithPrecision Known(long sig, int exp, int precision) => new(new CobolDec(sig, exp), precision);

    [Fact]
    public void ATinyValuesQuotient_KeepsItsOwnPrecision_AndIsNotZero()
    {
        // NUMVAL ("0.000000000000001") / 2 with no receiver: precision 15, w = 29, 5 × 10^-16.
        var q = CobolDecWithPrecision.DivTruncatedToOdd(CobolDecWithPrecision.Own(new CobolDec(1, -15)), Known(2, 0, 0), 0);
        Assert.Equal(15, q.Precision);
        Assert.Equal(0, CobolDec.Compare(q.Value, new CobolDec(5, -16)));
        // ... and again: the working scale stays 29, so 1.25 × 10^-16 at every depth.
        var q3 = CobolDecWithPrecision.DivTruncatedToOdd(CobolDecWithPrecision.DivTruncatedToOdd(q, Known(2, 0, 0), 0), Known(2, 0, 0), 0);
        Assert.Equal(15, q3.Precision);
        Assert.Equal(0, CobolDec.Compare(q3.Value, new CobolDec(125, -18)));
    }

    [Fact]
    public void AQuotientsPrecision_IsItsOperands_SoTheScaleDoesNotGrowWithDepth()
    {
        // NUMVAL ("2") / 3 / 3 into PIC S9(9)V9(6): w = 20 at both quotients, the value the scaled lane forms for an item 2.
        var q1 = CobolDecWithPrecision.DivTruncatedToOdd(CobolDecWithPrecision.Own(new CobolDec(2, 0)), Known(3, 0, 0), 6);
        var q2 = CobolDecWithPrecision.DivTruncatedToOdd(q1, Known(3, 0, 0), 6);
        Assert.Equal(0, q2.Precision);
        Int128 scaled = CobolNum.Divide(CobolNum.Divide(2, 0, 3, 0, 20, CobolRounding.Truncation), 20, 3, 0, 20, CobolRounding.Truncation);
        Assert.Equal(0, CobolDec.Compare(q2.Value, CobolDec.From(scaled, 20)));
    }

    [Fact]
    public void SumsDifferencesProductsAndNegations_CarryThePrecisionByTheExactLanesRules()
    {
        var tiny = CobolDecWithPrecision.Own(new CobolDec(1, -15));   // precision 15
        var half = Known(5, -1, 1);                                     // 0.5, precision 1
        Assert.Equal(15, CobolDecWithPrecision.Add(tiny, half, CobolRounding.NearestAwayFromZero).Precision);
        Assert.Equal(15, CobolDecWithPrecision.Sub(half, tiny, CobolRounding.NearestAwayFromZero).Precision);
        var product = CobolDecWithPrecision.Mul(tiny, half, CobolRounding.NearestAwayFromZero);
        Assert.Equal(16, product.Precision);
        Assert.Equal(0, CobolDec.Compare(product.Value, new CobolDec(5, -16)));
        var negated = CobolDecWithPrecision.Negate(product);
        Assert.Equal(16, negated.Precision);
        Assert.Equal(0, CobolDec.Compare(negated.Value, new CobolDec(-5, -16)));
    }

    [Fact]
    public void AZeroDivisor_IsTheSizeErrorCondition()
    {
        var error = Assert.Throws<CobolSizeError>(() =>
            CobolDecWithPrecision.DivTruncatedToOdd(CobolDecWithPrecision.Own(new CobolDec(2, 0)), Known(0, 0, 0), 6));
        Assert.Equal("EC-SIZE-ZERO-DIVIDE", error.EcName);
    }

    /// <summary>⛔ A ZERO CARRIES ITS PLACES LIKE ANY OTHER VALUE (review finding R5-1). ISO §8.8.1.5.3 performs the four
    /// operations as ISO/IEC 60559 5.4.1 does, whose exact results take the PREFERRED exponent: the smaller of a sum's two,
    /// the sum of a product's, the dividend's less the divisor's for a quotient — a zero operand's and a zero result's
    /// included. The sum with a zero term was held at the OTHER operand's places, so `0.000 + 2.5` had 1 where
    /// `0.001 + 2.5` has 3, and every zero result came back at exponent 0.</summary>
    [Theory]
    [InlineData(0, -3, 25, -1, false, 2500, -3)]     // 0.000 + 2.5 = 2.500, as 0.001 + 2.5 = 2.501
    [InlineData(25, -1, 0, -3, false, 2500, -3)]     // 2.5 + 0.000
    [InlineData(25, -1, 0, -3, true, 2500, -3)]      // 2.5 - 0.000
    [InlineData(0, -3, 25, -1, true, -2500, -3)]     // 0.000 - 2.5
    [InlineData(0, -1, 25, -3, false, 25, -3)]       // 0.0 + 0.025: the nonzero operand already has the smaller exponent
    [InlineData(0, -3, 0, -1, false, 0, -3)]         // 0.000 + 0.0 = 0.000
    [InlineData(1250, -3, 1250, -3, true, 0, -3)]    // 1.250 - 1.250 = 0.000: the cancelled zero keeps the places
    public void ASumWithAZero_IsHeldAtTheSmallerExponent(long aSig, int aExp, long bSig, int bExp, bool subtract,
        long sig, int exp)
    {
        CobolDec a = new(aSig, aExp), b = new(bSig, bExp);
        CobolDec r = subtract ? CobolDec.Sub(a, b, CobolRounding.NearestAwayFromZero) : CobolDec.Add(a, b, CobolRounding.NearestAwayFromZero);
        Assert.Equal(new CobolDec(sig, exp), r);
    }

    [Fact]
    public void AZeroProductQuotientPowerAndRoot_KeepThePreferredExponent()
    {
        var mode = CobolRounding.NearestAwayFromZero;
        Assert.Equal(new CobolDec(0, -4), CobolDec.Mul(new CobolDec(0, -3), new CobolDec(10, -1), mode));       // 0.000 × 1.0
        Assert.Equal(new CobolDec(0, -3), CobolDec.Div(new CobolDec(0, -3), new CobolDec(3, 0), mode));         // 0.000 / 3
        Assert.Equal(new CobolDec(0, -2), CobolDec.Div(new CobolDec(0, -3), new CobolDec(30, -1), mode));       // 0.000 / 3.0
        Assert.Equal(new CobolDec(0, -17), CobolDec.DivTruncatedToOdd(new CobolDec(0, -3), new CobolDec(3, 0), 17));
        Assert.Equal(new CobolDec(0, -6), CobolDec.Pow(new CobolDec(0, -3), new CobolDec(2, 0), mode));         // 0.000 ** 2
        Assert.Equal(new CobolDec(0, -6), CobolIntrinsics.PowNativeDec(new CobolDec(0, -3), 2));
        Assert.Equal(new CobolDec(0, -2), CobolDec.Sqrt(new CobolDec(0, -4), mode));                          // SQRT (0.0000)
        Assert.Equal(new CobolDec(0, -3), CobolDec.Sqrt(new CobolDec(0, -5), mode));                          // half, rounded down
        Assert.Equal(new CobolDec(0, -3), CobolDec.FromParsed(0, -3, mode));                                  // NUMVAL ("0.000")
        Assert.Equal(new CobolDec(0, -3), CobolDec.FromUnsigned(0, 3, mode));
        Assert.Equal(new CobolDec(0, -2), CobolDec.SubsidiaryQuotient(new CobolDec(0, -5), 2, 0));             // the receiver's scale
        Assert.Equal(3, CobolDecWithPrecision.Own(CobolIntrinsics.NumvalDec("0.000")).Precision);
        // 0E-40 + 7: as near the preferred -40 as the 34-digit significand holds, exactly 7.000…0 at 33 places
        Assert.Equal(new CobolDec(Int128.Parse("7000000000000000000000000000000000"), -33),
            CobolDec.Add(new CobolDec(0, -40), new CobolDec(7, 0), mode));
    }

    /// <summary>A zero's exponent stays inside decimal128's: at least 10^-6176, at most 10^(6144 - 33).</summary>
    [Fact]
    public void AZerosExponent_IsClampedToTheDecimal128Range()
    {
        var mode = CobolRounding.NearestAwayFromZero;
        Assert.Equal(new CobolDec(0, -6176), CobolDec.Mul(new CobolDec(0, -6000), new CobolDec(0, -6000), mode));
        Assert.Equal(new CobolDec(0, 6111), CobolDec.Mul(new CobolDec(0, 6000), new CobolDec(0, 6000), mode));
        Assert.Equal(new CobolDec(0, -6176), CobolDec.Zero(long.MinValue));
    }
}
