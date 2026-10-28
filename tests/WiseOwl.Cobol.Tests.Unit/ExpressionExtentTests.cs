// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Procedure;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The exact extent of an arithmetic expression's value (<see cref="ExpressionExtent"/>, kb/Work PB3974): the digits the
/// implementor's intermediate result item of an EVALUATE selection subject needs to hold the value §14.9.13.4 GR3 c)
/// assigns it. Each expectation is the arithmetic of the bound: a value below 10^i plus one below 10^j is below
/// 10^(max(i, j)+1), a product is below 10^(i+j), and the fraction digits add under a product and take the larger under
/// a sum. The field-operand arms (a PICTURE's digits and scale) are held by the
/// <c>pb3974_evaluate_expression_subject_width</c> golden, which needs a data division to build them.
/// </summary>
public sealed class ExpressionExtentTests
{
    private static BoundExpr Lit(string text) => new BoundNumLiteral(text);

    [Theory]
    [InlineData("999", 3, 0)]
    [InlineData("0.5", 0, 1)]
    [InlineData("12.345", 2, 3)]
    [InlineData("-12.5", 2, 1)]
    [InlineData("0000012", 2, 0)]
    public void ALiteral_IsItsOwnDigits(string text, int integerDigits, int scale) =>
        Assert.Equal(new ExactExtent(integerDigits, scale), ExpressionExtent.Of(Lit(text)));

    [Fact]
    public void AProduct_AddsTheIntegerDigitsAndTheScales() =>
        // 99 * 9.9 < 10^2 * 10^1 = 10^3 at scale 1: 980.1 is the largest.
        Assert.Equal(new ExactExtent(3, 1), ExpressionExtent.Of(new BoundBinary(Lit("99"), '*', Lit("9.9"))));

    [Fact]
    public void ASum_TakesOneMoreIntegerDigitAndTheWiderScale() =>
        // 99 + 9.99 < 10^3 at scale 2.
        Assert.Equal(new ExactExtent(3, 2), ExpressionExtent.Of(new BoundBinary(Lit("99"), '+', Lit("9.99"))));

    [Fact]
    public void ADifference_IsBoundedLikeASum() =>
        Assert.Equal(new ExactExtent(3, 2), ExpressionExtent.Of(new BoundBinary(Lit("99"), '-', Lit("9.99"))));

    [Fact]
    public void ANegation_ChangesNeitherDigits() =>
        Assert.Equal(new ExactExtent(1, 1), ExpressionExtent.Of(new BoundNegate(Lit("9.9"))));

    [Fact]
    public void ANestedProductAndSum_ComposeTheBounds() =>
        // (9999999999 * 9999999999) is 20 digits; the third factor makes 30; adding 1 makes 31.
        Assert.Equal(new ExactExtent(31, 0), ExpressionExtent.Of(
            new BoundBinary(
                new BoundBinary(new BoundBinary(Lit("9999999999"), '*', Lit("9999999999")), '*', Lit("9999999999")),
                '+', Lit("1"))));

    [Fact]
    public void AQuotient_HasNoExactExtent() =>
        // A quotient is truncated at a working scale the receiver decides (NumericRenderer.Divide).
        Assert.Null(ExpressionExtent.Of(new BoundBinary(Lit("1"), '/', Lit("3"))));

    [Fact]
    public void AnOperandWithNoExactExtent_TakesTheWholeExpressionWithIt() =>
        Assert.Null(ExpressionExtent.Of(new BoundBinary(Lit("2"), '*', new BoundBinary(Lit("1"), '/', Lit("3")))));

    [Theory]
    [InlineData("1.5E+0")]
    [InlineData("1.0E-5")]
    public void AFloatingPointLiteral_IsAnSdidiOperandNotAFixedPointExtent(string text) =>
        Assert.Null(ExpressionExtent.Of(Lit(text)));

    [Fact]
    public void APower_HasNoExactExtent() =>
        Assert.Null(ExpressionExtent.Of(new BoundPower(Lit("2"), Lit("10"))));
}
