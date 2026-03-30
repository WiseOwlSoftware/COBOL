// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Lowers BoundExpression trees to IrExpression trees (M001 contract).
/// Also handles compile-time constant folding and literal formatting.
/// </summary>
internal sealed class ExpressionLowerer
{
    private readonly LoweringContext _ctx;

    public ExpressionLowerer(LoweringContext ctx) => _ctx = ctx;

    /// <summary>
    /// Lower a BoundExpression tree into an IR-native IrExpression tree.
    /// Resolves all data-reference leaf nodes to IrLocations during lowering,
    /// eliminating the need for ResolvedLocations sidecars.
    /// Returns null if a required location cannot be resolved (diagnostic already emitted).
    /// </summary>
    public IrExpression? LowerExpression(BoundExpression expr)
    {
        switch (expr)
        {
            case BoundLiteralExpression lit:
                return lit.Value switch
                {
                    decimal d => new IrLiteral(d),
                    int i => new IrLiteral(i),
                    long l => new IrLiteral(l),
                    _ => null // string literals are not numeric expressions
                };

            case BoundIdentifierExpression id:
            {
                var loc = _ctx.Location.ResolveLocation(id);
                return loc != null ? new IrLoadNumeric(loc) : null;
            }

            case BoundReferenceModificationExpression refMod:
            {
                var loc = _ctx.Location.ResolveRefModLocation(refMod);
                return loc != null ? new IrLoadNumeric(loc) : null;
            }

            case BoundBinaryExpression bin:
            {
                var irOp = bin.OperatorKind switch
                {
                    BoundBinaryOperatorKind.Add => IrArithmeticOp.Add,
                    BoundBinaryOperatorKind.Subtract => IrArithmeticOp.Subtract,
                    BoundBinaryOperatorKind.Multiply => IrArithmeticOp.Multiply,
                    BoundBinaryOperatorKind.Divide => IrArithmeticOp.Divide,
                    BoundBinaryOperatorKind.Remainder => IrArithmeticOp.Remainder,
                    BoundBinaryOperatorKind.Power => IrArithmeticOp.Power,
                    _ => (IrArithmeticOp?)null
                };
                if (irOp == null) return null; // comparison/logical ops are not arithmetic expressions

                var left = LowerExpression(bin.Left);
                var right = LowerExpression(bin.Right);
                if (left == null || right == null) return null;

                // Detect unary negation pattern: (0 - expr) → IrUnaryExpr(Negate, expr)
                if (irOp == IrArithmeticOp.Subtract && left is IrLiteral { Value: 0m })
                    return new IrUnaryExpr(IrUnaryOp.Negate, right);

                return new IrBinaryExpr(irOp.Value, left, right);
            }

            case BoundFunctionCallExpression func:
            {
                var args = new List<IrFunctionArg>(func.Arguments.Count);
                foreach (var arg in func.Arguments)
                {
                    switch (arg)
                    {
                        case BoundLiteralExpression litArg when litArg.Value is string s:
                            args.Add(new IrLiteralStringArg(s));
                            break;

                        case BoundIdentifierExpression idArg
                            when idArg.Category is Runtime.CobolCategory.Alphanumeric
                                or Runtime.CobolCategory.Alphabetic
                                or Runtime.CobolCategory.AlphanumericEdited:
                        {
                            var loc = _ctx.Location.ResolveLocation(idArg);
                            if (loc == null) return null;
                            args.Add(new IrAlphanumericArg(loc));
                            break;
                        }

                        default:
                        {
                            var lowered = LowerExpression(arg);
                            if (lowered == null) return null;
                            args.Add(new IrNumericArg(lowered));
                            break;
                        }
                    }
                }
                return new IrIntrinsicCall(func.FunctionName, args);
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// Attempts compile-time constant folding for an expression.
    /// Returns the constant value if the entire expression is statically evaluable.
    /// </summary>
    public static decimal? TryEvalConstant(BoundExpression expr)
    {
        if (expr is BoundLiteralExpression lit && lit.Value is decimal d)
            return d;

        if (expr is BoundBinaryExpression bin)
        {
            var left = TryEvalConstant(bin.Left);
            var right = TryEvalConstant(bin.Right);
            if (left.HasValue && right.HasValue)
            {
                return bin.OperatorKind switch
                {
                    BoundBinaryOperatorKind.Add => left.Value + right.Value,
                    BoundBinaryOperatorKind.Subtract => left.Value - right.Value,
                    BoundBinaryOperatorKind.Multiply => left.Value * right.Value,
                    BoundBinaryOperatorKind.Divide when right.Value != 0 => left.Value / right.Value,
                    _ => (decimal?)null
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if an expression is a negative literal (0 - literal pattern).
    /// </summary>
    public static bool TryExtractNegativeLiteral(BoundExpression expr, out decimal value)
    {
        if (expr is BoundBinaryExpression neg
            && neg.OperatorKind == BoundBinaryOperatorKind.Subtract
            && neg.Left is BoundLiteralExpression zl && zl.Value is decimal zd && zd == 0m
            && neg.Right is BoundLiteralExpression il && il.Value is decimal id)
        {
            value = -id;
            return true;
        }
        value = 0m;
        return false;
    }

    /// <summary>
    /// Format a numeric literal for MOVE to alphanumeric field.
    /// Per ISO §14.19.4: the literal is treated as an unsigned integer field
    /// whose size is the number of digits specified in the literal.
    /// </summary>
    public static string FormatLiteralForAlphanumeric(string originalText)
    {
        var sb = new System.Text.StringBuilder(originalText.Length);
        foreach (char c in originalText)
        {
            if (char.IsDigit(c))
                sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "0";
    }

    public static string FormatLiteralForAlphanumeric(decimal d)
    {
        decimal abs = Math.Abs(d);
        string raw = abs.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return raw.Replace(".", "");
    }
}
