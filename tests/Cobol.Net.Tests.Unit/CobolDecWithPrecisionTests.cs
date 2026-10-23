// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN SDIDI OPERAND WHOSE PRECISION ONLY THE RUN TIME KNOWS HAS ITS VALUE'S FRACTION DIGITS, AND A NESTED QUOTIENT
/// CARRIES ITS OPERANDS' PRECISION, NEVER ITS WORKING SCALE (kb/Work PB3538 review finding R3-1; CONFORMANCE.md
/// DOC-A.1-123). A NUMVAL value, a power or a function's result was given the precision 0, so a quotient of 10^-15 was
/// truncated at 14 places, to zero. The expected values are the rule's: the working scale is
/// <c>max(receiver scale, precision) + 14</c> (<see cref="CobolNum.NestedQuotientScale"/>), the quotient is truncated there,
/// a sum or difference carries the larger precision and a product the sum, and the scaled lane
/// (<see cref="CobolNum.Divide"/> at the same scale, TRUNCATION) forms the same value from an item of that precision.
/// </summary>
public sealed class CobolDecWithPrecisionTests
{
    [Theory]
    [InlineData(5, -16, 16)]     // 5 × 10^-16
    [InlineData(50, -17, 16)]    // the same value with a trailing zero: the VALUE's digits, not the representation's
    [InlineData(20, -1, 0)]      // 2.0E0 is the integer 2
    [InlineData(-15, -3, 3)]     // -0.015
    [InlineData(123, 2, 0)]      // 12300
    [InlineData(0, -5, 0)]       // zero has no fraction digits
    public void FractionDigits_AreTheValuesOwn(long sig, int exp, int expected) =>
        Assert.Equal(expected, CobolDec.FractionDigits(new CobolDec(sig, exp)));

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
}
