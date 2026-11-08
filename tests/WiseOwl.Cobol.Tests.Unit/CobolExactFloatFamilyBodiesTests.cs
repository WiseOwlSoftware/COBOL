// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE AND ANNUITY OVER A NESTED SELECTION OF A FLOAT ITEM ARE BINARY64 (kb/Work PB4979;
/// docs/CONFORMANCE.md DOC-A.1-92b). MAX returns "the content of the argument" (§2023:15.59.4 1)), so a float item's content may reach these
/// bodies as an exact value carrying the item's binary64 (<see cref="CobolExact.TryBinary64"/>); the body then evaluates the equivalent
/// arithmetic expression in binary64 as the float lane does for the item written directly, and the value is an approximation (the
/// floating-math family: it lands by its shortest round-trip decimal). A list of fixed-point values stays exact. The expected values are
/// Python floats.
/// </summary>
public sealed class CobolExactFloatFamilyBodiesTests
{
    private static readonly CobolRounding Away = CobolRounding.NearestAwayFromZero;
    private static CobolExact F => CobolExact.FromBinary64(0.1);
    private static CobolExact Half => CobolExact.From(5, 1);

    [Fact]
    public void Variance_AndStandardDeviation_OfABinary64Argument_AreTheBinary64Body_AsAnApproximation()
    {
        CobolExact variance = CobolIntrinsics.VarianceExact(Away, F, Half);
        Assert.True(variance.TryBinary64(out double v) && v == 0.04);
        Assert.False(variance.IsFloatContent);
        CobolExact deviation = CobolIntrinsics.StdDevExact(Away, F, Half);
        Assert.True(deviation.TryBinary64(out double s) && s == 0.2);
        Assert.False(deviation.IsFloatContent);
    }

    [Fact]
    public void Variance_OfFixedPointArguments_StaysTheExactValue()
    {
        CobolExact variance = CobolIntrinsics.VarianceExact(Away, CobolExact.From(5, 1), CobolExact.From(5, 2));
        Assert.False(variance.TryBinary64(out _));
        Assert.Equal(0, CobolDec.Compare(new CobolDec(50625, -6), variance.Image));
    }

    [Fact]
    public void PresentValue_OfABinary64RateOrAmount_IsTheBinary64Body()
    {
        // 0.1 / 1.5 + 0.5 / 1.5 ** 2 over the exact rate 0.5 and the float amount 0.1.
        CobolExact amount = CobolIntrinsics.PresentValueExact(Away, Half, F, Half);
        Assert.True(amount.TryBinary64(out double a) && a == 0.28888888888888886);
        // A float rate: the discount base is the binary64 1 + 0.1.
        CobolExact rate = CobolIntrinsics.PresentValueExact(Away, F, CobolExact.From(11, 1));
        Assert.True(rate.TryBinary64(out double r) && r == 11d / 10d / (1 + 0.1));
        // Fixed-point arguments: exact.
        Assert.False(CobolIntrinsics.PresentValueExact(Away, Half, CobolExact.From(1, 0)).TryBinary64(out _));
    }

    [Fact]
    public void Annuity_OfABinary64Rate_IsTheBinary64Body_AndAFixedRateStaysTheSdidiValue()
    {
        CobolDec three = CobolDec.From(3, 0);
        CobolExact binary = CobolIntrinsics.AnnuityExact(Away, F, three);
        Assert.True(binary.TryBinary64(out double d) && d == CobolIntrinsics.Annuity(0.1, 3));
        Assert.False(binary.IsFloatContent);
        CobolExact fixedRate = CobolIntrinsics.AnnuityExact(Away, CobolExact.From(1, 1), three);
        Assert.False(fixedRate.TryBinary64(out _));
        Assert.Equal(CobolIntrinsics.AnnuityDec(Away, CobolDec.From(1, 1), three), fixedRate.Image);
    }

    [Fact]
    public void SubsidiaryQuotient_OfAFloatItemsContent_IsTheTruncatedBinary64Quotient()
    {
        // 0.5 / 0.1: the exact operands' quotient is 4.99999999999999972244, the binary64 quotient is 5.0.
        CobolExact subsidiary = CobolExact.SubsidiaryQuotient(Half, F, 5);
        Assert.Equal(0, CobolExact.Compare(CobolExact.From(5, 0), subsidiary));
        // The same divisor as an unmarked exact decimal (its 34-digit image) is the exact quotient, truncated one unit lower.
        CobolExact unmarked = CobolExact.FromDec(F.Image);
        Assert.False(unmarked.TryBinary64(out _));
        Assert.Equal(0, CobolExact.Compare(CobolExact.From(499999, 5), CobolExact.SubsidiaryQuotient(Half, unmarked, 5)));
    }
}
