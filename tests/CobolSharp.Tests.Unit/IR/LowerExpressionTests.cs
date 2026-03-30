// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.IR;

public class LowerExpressionTests
{
    // ── Helpers ──

    /// <summary>
    /// Build a minimal Binder wired to a SemanticModel that contains the given data symbols.
    /// All symbols are placed at sequential offsets in WORKING-STORAGE.
    /// Returns the binder and the created DataSymbols (in same order as fields).
    /// </summary>
    private static (Binder binder, DataSymbol[] symbols) CreateBinder(
        params (string name, string pic, UsageKind usage)[] fields)
    {
        var prog = new ProgramSymbol("TEST", 1);
        var symbolTable = new SymbolTable("TEST", 1);
        var diagnostics = new DiagnosticBag();
        var model = new SemanticModel(prog, symbolTable, diagnostics);

        var syms = new DataSymbol[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            var (name, pic, usage) = fields[i];
            var sym = new DataSymbol(name, name, 77, pic, usage, null, null, 1);
            sym.ResolvedType = PicUsageResolver.ResolveForDataItem(
                name, pic, usage, diagnostics, 1, false, PicEnvironment.Default);
            prog.DataDivisionScope.TryDeclare(sym, out _);
            model.AddDataRecord(sym);
            syms[i] = sym;
        }

        model.SetDataItemsInOrder(syms);
        StorageLayoutComputer.ComputeLayout(model);

        var binder = new Binder(model, diagnostics);
        return (binder, syms);
    }

    private static BoundLiteralExpression Lit(decimal value) =>
        new(value, CobolCategory.Numeric);

    private static BoundIdentifierExpression Id(DataSymbol sym) =>
        new(sym, CobolCategory.Numeric);

    // ── Literal tests ──

    [Fact]
    public void Decimal_literal_lowers_to_IrLiteral()
    {
        var (binder, _) = CreateBinder();
        var result = binder._ctx.Expression.LowerExpression(Lit(42.5m));

        var lit = Assert.IsType<IrLiteral>(result);
        Assert.Equal(42.5m, lit.Value);
    }

    [Fact]
    public void Zero_literal_lowers_to_IrLiteral()
    {
        var (binder, _) = CreateBinder();
        var result = binder._ctx.Expression.LowerExpression(Lit(0m));

        var lit = Assert.IsType<IrLiteral>(result);
        Assert.Equal(0m, lit.Value);
    }

    [Fact]
    public void Negative_literal_lowers_to_IrLiteral()
    {
        var (binder, _) = CreateBinder();
        var result = binder._ctx.Expression.LowerExpression(Lit(-123m));

        var lit = Assert.IsType<IrLiteral>(result);
        Assert.Equal(-123m, lit.Value);
    }

    [Fact]
    public void String_literal_returns_null()
    {
        var (binder, _) = CreateBinder();
        var strLit = new BoundLiteralExpression("HELLO", CobolCategory.Alphanumeric);
        var result = binder._ctx.Expression.LowerExpression(strLit);

        Assert.Null(result);
    }

    // ── Identifier tests ──

    [Fact]
    public void Simple_identifier_lowers_to_IrLoadNumeric()
    {
        var (binder, syms) = CreateBinder(("WS-AMOUNT", "9(5)V99", UsageKind.Display));
        var result = binder._ctx.Expression.LowerExpression(Id(syms[0]));

        var load = Assert.IsType<IrLoadNumeric>(result);
        var loc = Assert.IsType<IrStaticLocation>(load.Source);
        Assert.Equal(StorageAreaKind.WorkingStorage, loc.Location.Area);
        Assert.Equal(0, loc.Location.Offset);
    }

    // ── Binary expression tests ──

    [Fact]
    public void Add_lowers_to_IrBinaryExpr()
    {
        var (binder, syms) = CreateBinder(("A", "9(3)", UsageKind.Display));
        var add = new BoundBinaryExpression(
            Id(syms[0]), BoundBinaryOperatorKind.Add, Lit(10m), CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(add);

        var bin = Assert.IsType<IrBinaryExpr>(result);
        Assert.Equal(IrArithmeticOp.Add, bin.Op);
        Assert.IsType<IrLoadNumeric>(bin.Left);
        var right = Assert.IsType<IrLiteral>(bin.Right);
        Assert.Equal(10m, right.Value);
    }

    [Fact]
    public void Nested_arithmetic_lowers_recursively()
    {
        // (A + 10) * 2
        var (binder, syms) = CreateBinder(("A", "9(3)", UsageKind.Display));
        var inner = new BoundBinaryExpression(
            Id(syms[0]), BoundBinaryOperatorKind.Add, Lit(10m), CobolCategory.Numeric);
        var outer = new BoundBinaryExpression(
            inner, BoundBinaryOperatorKind.Multiply, Lit(2m), CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(outer);

        var mul = Assert.IsType<IrBinaryExpr>(result);
        Assert.Equal(IrArithmeticOp.Multiply, mul.Op);

        var add = Assert.IsType<IrBinaryExpr>(mul.Left);
        Assert.Equal(IrArithmeticOp.Add, add.Op);
        Assert.IsType<IrLoadNumeric>(add.Left);
        Assert.IsType<IrLiteral>(add.Right);

        var right = Assert.IsType<IrLiteral>(mul.Right);
        Assert.Equal(2m, right.Value);
    }

    [Theory]
    [InlineData(BoundBinaryOperatorKind.Add, IrArithmeticOp.Add)]
    [InlineData(BoundBinaryOperatorKind.Subtract, IrArithmeticOp.Subtract)]
    [InlineData(BoundBinaryOperatorKind.Multiply, IrArithmeticOp.Multiply)]
    [InlineData(BoundBinaryOperatorKind.Divide, IrArithmeticOp.Divide)]
    [InlineData(BoundBinaryOperatorKind.Remainder, IrArithmeticOp.Remainder)]
    [InlineData(BoundBinaryOperatorKind.Power, IrArithmeticOp.Power)]
    public void All_arithmetic_operators_lower_correctly(
        BoundBinaryOperatorKind boundOp, IrArithmeticOp expectedOp)
    {
        var (binder, _) = CreateBinder();
        var bin = new BoundBinaryExpression(Lit(5m), boundOp, Lit(3m), CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(bin);

        var irBin = Assert.IsType<IrBinaryExpr>(result);
        Assert.Equal(expectedOp, irBin.Op);
    }

    [Fact]
    public void Comparison_operator_returns_null()
    {
        var (binder, _) = CreateBinder();
        var cmp = new BoundBinaryExpression(
            Lit(1m), BoundBinaryOperatorKind.Equal, Lit(2m), CobolCategory.Numeric);

        Assert.Null(binder._ctx.Expression.LowerExpression(cmp));
    }

    // ── Unary negation tests ──

    [Fact]
    public void Zero_minus_expr_lowers_to_IrUnaryExpr_Negate()
    {
        // 0 - A → IrUnaryExpr(Negate, IrLoadNumeric(A))
        var (binder, syms) = CreateBinder(("A", "9(3)", UsageKind.Display));
        var negate = new BoundBinaryExpression(
            Lit(0m), BoundBinaryOperatorKind.Subtract, Id(syms[0]), CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(negate);

        var unary = Assert.IsType<IrUnaryExpr>(result);
        Assert.Equal(IrUnaryOp.Negate, unary.Op);
        Assert.IsType<IrLoadNumeric>(unary.Operand);
    }

    [Fact]
    public void Nonzero_minus_expr_stays_as_subtract()
    {
        // 5 - A → IrBinaryExpr(Subtract, ...)
        var (binder, syms) = CreateBinder(("A", "9(3)", UsageKind.Display));
        var sub = new BoundBinaryExpression(
            Lit(5m), BoundBinaryOperatorKind.Subtract, Id(syms[0]), CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(sub);

        var bin = Assert.IsType<IrBinaryExpr>(result);
        Assert.Equal(IrArithmeticOp.Subtract, bin.Op);
    }

    // ── Intrinsic function call tests ──

    [Fact]
    public void Intrinsic_call_with_numeric_args_lowers_correctly()
    {
        // FUNCTION MAX(A, 100)
        var (binder, syms) = CreateBinder(("A", "9(5)", UsageKind.Display));
        var func = new BoundFunctionCallExpression("MAX",
            [Id(syms[0]), Lit(100m)], CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(func);

        var call = Assert.IsType<IrIntrinsicCall>(result);
        Assert.Equal("MAX", call.FunctionName);
        Assert.Equal(2, call.Arguments.Count);

        var arg0 = Assert.IsType<IrNumericArg>(call.Arguments[0]);
        Assert.IsType<IrLoadNumeric>(arg0.Expression);

        var arg1 = Assert.IsType<IrNumericArg>(call.Arguments[1]);
        var lit = Assert.IsType<IrLiteral>(arg1.Expression);
        Assert.Equal(100m, lit.Value);
    }

    [Fact]
    public void Intrinsic_call_with_string_literal_arg()
    {
        // FUNCTION LENGTH("HELLO")
        var (binder, _) = CreateBinder();
        var func = new BoundFunctionCallExpression("LENGTH",
            [new BoundLiteralExpression("HELLO", CobolCategory.Alphanumeric)],
            CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(func);

        var call = Assert.IsType<IrIntrinsicCall>(result);
        Assert.Equal("LENGTH", call.FunctionName);
        Assert.Single(call.Arguments);
        var arg = Assert.IsType<IrLiteralStringArg>(call.Arguments[0]);
        Assert.Equal("HELLO", arg.Value);
    }

    [Fact]
    public void Intrinsic_call_with_alphanumeric_field_arg()
    {
        // FUNCTION LENGTH(WS-NAME)
        var (binder, syms) = CreateBinder(("WS-NAME", "X(20)", UsageKind.Display));
        var idArg = new BoundIdentifierExpression(syms[0], CobolCategory.Alphanumeric);
        var func = new BoundFunctionCallExpression("LENGTH", [idArg], CobolCategory.Numeric);

        var result = binder._ctx.Expression.LowerExpression(func);

        var call = Assert.IsType<IrIntrinsicCall>(result);
        Assert.Single(call.Arguments);
        var arg = Assert.IsType<IrAlphanumericArg>(call.Arguments[0]);
        Assert.IsType<IrStaticLocation>(arg.Source);
    }
}
