// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB6126 - the §2023:8.8.1.2 GR6a and GR6c refusals name the base and the exponent that broke the rule, on every carrier
/// (<c>CobolPower.ScreenRule6</c>): the diagnosis of <c>0 ** -1</c> or <c>(-2) ** 0.5</c> says which operand it was.
/// </summary>
public sealed class PowerRule6MessageTests
{
    private static string Message(Action body) => Assert.Throws<CobolSizeError>(body).Message;

    [Fact]
    public void Binary64_ZeroBase_NamesTheExponent() =>
        Assert.Contains("a zero base requires an exponent greater than zero, not -1 (", Message(() => CobolPower.Binary64(0, -1)));

    [Fact]
    public void Binary64_NegativeBase_NamesBothOperands() =>
        Assert.Contains("a negative base (-2) requires an integer exponent, not 0.5 (", Message(() => CobolPower.Binary64(-2, 0.5)));

    [Fact]
    public void Decimal_ZeroBase_NamesTheExponent() =>
        Assert.Contains("a zero base requires an exponent greater than zero, not 0 (",
            Message(() => CobolDec.Pow(CobolDec.From(0, 0), CobolDec.From(0, 0), CobolRounding.NearestEven)));

    [Fact]
    public void Decimal_NegativeBase_NamesBothOperands() =>
        Assert.Contains("a negative base (-2) requires an integer exponent, not 0.5 (",
            Message(() => CobolDec.Pow(CobolDec.From(-2, 0), CobolDec.From(5, 1), CobolRounding.NearestEven)));

    [Fact]
    public void Exact_ZeroBase_NamesTheIntegerExponent() =>
        Assert.Contains("a zero base requires an exponent greater than zero, not -3 (", Message(() => CobolExact.Pow(CobolExact.From(0, 0), -3)));
}
