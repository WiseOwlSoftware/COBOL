// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

public class ExpressionTypeTests
{
    private static DataSymbol MakeSym(string pic, UsageKind usage = UsageKind.Display)
    {
        var sym = new DataSymbol("TEST", "TEST", 77, pic, usage, null, null, 1);
        var diagBag = new DiagnosticBag();
        sym.ResolvedType = PicUsageResolver.ResolveForDataItem("TEST", pic, usage, diagBag, 1, false, PicEnvironment.Default);
        return sym;
    }

    [Fact]
    public void Integer_pic_yields_integer_expression_type()
    {
        var sym = MakeSym("9(5)");
        var et = ExpressionType.FromDataSymbol(sym);
        Assert.True(et.IsNumeric);
        Assert.True(et.IsInteger);
        Assert.Equal(NumericKind.Integer, et.Numeric!.Kind);
        Assert.Equal(5, et.Numeric.Precision);
        Assert.Equal(0, et.Numeric.Scale);
    }

    [Fact]
    public void Decimal_pic_yields_decimal_expression_type()
    {
        var sym = MakeSym("S9(5)V99");
        var et = ExpressionType.FromDataSymbol(sym);
        Assert.True(et.IsNumeric);
        Assert.False(et.IsInteger);
        Assert.Equal(NumericKind.Decimal, et.Numeric!.Kind);
        Assert.Equal(7, et.Numeric.Precision);
        Assert.Equal(2, et.Numeric.Scale);
        Assert.True(et.Numeric.IsSigned);
    }

    [Fact]
    public void Alphanumeric_pic_yields_alphanumeric()
    {
        var sym = MakeSym("X(10)");
        var et = ExpressionType.FromDataSymbol(sym);
        Assert.True(et.IsAlphanumeric);
        Assert.False(et.IsNumeric);
    }

    [Fact]
    public void Group_item_yields_group()
    {
        var child = MakeSym("X(5)");
        child = new DataSymbol("CHILD", "CHILD", 5, "X(5)", UsageKind.Display, null, null, 1);
        var group = new DataSymbol("GRP", "GRP", 1, null, UsageKind.Display, null, null, 1);
        group.AddChild(child);
        var et = ExpressionType.FromDataSymbol(group);
        Assert.Equal(ExpressionTypeKind.Group, et.Kind);
    }

    // ── Promotion ──

    [Fact]
    public void Promote_integer_and_decimal_yields_decimal()
    {
        var left = ExpressionType.MakeNumeric(NumericType.Integer(5, true));
        var right = ExpressionType.MakeNumeric(NumericType.Decimal(7, 2, true));
        var result = ExpressionType.Promote(left, right);
        Assert.True(result.IsNumeric);
        Assert.Equal(NumericKind.Decimal, result.Numeric!.Kind);
        Assert.Equal(2, result.Numeric.Scale);
    }

    [Fact]
    public void Promote_two_integers_yields_integer()
    {
        var left = ExpressionType.MakeNumeric(NumericType.Integer(5, false));
        var right = ExpressionType.MakeNumeric(NumericType.Integer(9, true));
        var result = ExpressionType.Promote(left, right);
        Assert.True(result.IsInteger);
        Assert.Equal(9, result.Numeric!.Precision);
        Assert.True(result.Numeric.IsSigned);
    }

    [Fact]
    public void Promote_with_floating_yields_floating()
    {
        var left = ExpressionType.MakeNumeric(NumericType.Integer(5, true));
        var right = ExpressionType.MakeNumeric(NumericType.Floating());
        var result = ExpressionType.Promote(left, right);
        Assert.Equal(NumericKind.Floating, result.Numeric!.Kind);
    }

    [Fact]
    public void Promote_non_numeric_yields_unknown()
    {
        var result = ExpressionType.Promote(ExpressionType.Alphanumeric, ExpressionType.Boolean);
        Assert.Equal(ExpressionTypeKind.Unknown, result.Kind);
    }
}
