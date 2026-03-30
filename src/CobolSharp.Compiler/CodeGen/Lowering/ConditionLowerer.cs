// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Lowers COBOL conditions to IR: comparisons, class tests (IS NUMERIC),
/// sign tests (IS POSITIVE), condition names (88-level), and conditional branching.
/// Includes the comparison operand normalization and dispatch matrix.
/// </summary>
internal sealed class ConditionLowerer
{
    private readonly LoweringContext _ctx;

    public ConditionLowerer(LoweringContext ctx) => _ctx = ctx;

    // ── Comparison operand types ──

    internal enum ComparisonOperandKind { Location, NumericLiteral, StringLiteral, Figurative, ArithmeticExpression }

    internal sealed class ComparisonOperand
    {
        public ComparisonOperandKind Kind { get; }
        public IrLocation? Location { get; init; }
        public CobolCategory Category { get; init; }
        public decimal NumericValue { get; init; }
        public string? StringValue { get; init; }
        public FigurativeKind FigurativeKind { get; init; }
        public string? AllLiteral { get; init; }
        public int FieldWidth { get; init; }
        public BoundBinaryExpression? ArithExpr { get; init; }

        private ComparisonOperand(ComparisonOperandKind kind) { Kind = kind; }

        public static ComparisonOperand FromLocation(IrLocation loc, CobolCategory cat, int width) =>
            new(ComparisonOperandKind.Location) { Location = loc, Category = cat, FieldWidth = width };
        public static ComparisonOperand FromNumeric(decimal value) =>
            new(ComparisonOperandKind.NumericLiteral) { NumericValue = value, Category = CobolCategory.Numeric };
        public static ComparisonOperand FromString(string value) =>
            new(ComparisonOperandKind.StringLiteral) { StringValue = value, Category = CobolCategory.Alphanumeric };
        public static ComparisonOperand FromFigurative(FigurativeKind kind, string? allLiteral = null) =>
            new(ComparisonOperandKind.Figurative) { FigurativeKind = kind, AllLiteral = allLiteral };
        public static ComparisonOperand FromArithmeticExpression(BoundBinaryExpression expr) =>
            new(ComparisonOperandKind.ArithmeticExpression) { ArithExpr = expr, Category = CobolCategory.Numeric };
    }

    // ── Operand normalization ──

    internal ComparisonOperand? NormalizeOperand(BoundExpression expr)
    {
        switch (expr)
        {
            case BoundIdentifierExpression:
            case BoundReferenceModificationExpression:
            {
                var loc = _ctx.Location.ResolveExpressionLocation(expr);
                if (loc == null) return null;
                var pic = loc.GetPic();
                return ComparisonOperand.FromLocation(loc, pic.Category, pic.StorageLength);
            }

            case BoundLiteralExpression lit:
                if (lit.Value is decimal d) return ComparisonOperand.FromNumeric(d);
                if (lit.Value is string s) return ComparisonOperand.FromString(s);
                if (lit.Value is bool b) return ComparisonOperand.FromNumeric(b ? 1m : 0m);
                return null;

            case BoundFigurativeExpression fig:
                return ComparisonOperand.FromFigurative(
                    (FigurativeKind)fig.FigurativeKind, fig.AllLiteral);

            case BoundBinaryExpression neg
                when neg.OperatorKind == BoundBinaryOperatorKind.Subtract
                     && neg.Left is BoundLiteralExpression zl && zl.Value is decimal zd && zd == 0m
                     && neg.Right is BoundLiteralExpression il && il.Value is decimal id:
                return ComparisonOperand.FromNumeric(-id);

            case BoundBinaryExpression arith
                when arith.OperatorKind is BoundBinaryOperatorKind.Add
                    or BoundBinaryOperatorKind.Subtract
                    or BoundBinaryOperatorKind.Multiply
                    or BoundBinaryOperatorKind.Divide
                    or BoundBinaryOperatorKind.Power:
                return ComparisonOperand.FromArithmeticExpression(arith);

            default:
                return null;
        }
    }

    internal static string MakeFigurativeString(FigurativeKind kind, int width, string? allLiteral)
    {
        char fillChar = kind switch
        {
            FigurativeKind.Space => ' ',
            FigurativeKind.Zero => '0',
            FigurativeKind.HighValue => '\xFF',
            FigurativeKind.LowValue => '\x00',
            FigurativeKind.Quote => '"',
            _ => ' '
        };

        if (allLiteral != null)
        {
            if (width <= 0 || allLiteral.Length == 0) return allLiteral;
            var sb = new System.Text.StringBuilder(width);
            while (sb.Length < width) sb.Append(allLiteral);
            return sb.ToString(0, width);
        }

        return width > 0 ? new string(fillChar, width) : fillChar.ToString();
    }

    // ── Main condition dispatch ──

    public void LowerCondition(BoundExpression cond, IrValue result, IrBasicBlock block)
    {
        if (cond is BoundClassConditionExpression cc)
        {
            LowerClassCondition(cc, result, block);
            return;
        }

        if (cond is BoundUserClassConditionExpression ucc)
        {
            LowerUserClassCondition(ucc, result, block);
            return;
        }

        if (cond is BoundSignConditionExpression sc)
        {
            LowerSignCondition(sc, result, block);
            return;
        }

        if (cond is BoundConditionNameExpression cn)
        {
            LowerConditionName(cn, result, block);
            return;
        }

        if (cond is BoundSwitchConditionExpression sw)
        {
            block.Instructions.Add(new IrTestSwitch(result, sw.Switch.ImplementorName, sw.TestsOnState));
            return;
        }

        if (cond is BoundBinaryExpression binCond)
        {
            switch (binCond.OperatorKind)
            {
                case BoundBinaryOperatorKind.Or:
                {
                    var leftVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.Or));
                    return;
                }
                case BoundBinaryOperatorKind.And:
                {
                    var leftVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.And));
                    return;
                }
                case BoundBinaryOperatorKind.Not:
                {
                    var innerVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, innerVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, innerVal, innerVal, IrLogicalOp.Not));
                    return;
                }
            }

            LowerComparison(binCond, result, block);
            return;
        }

        if (cond is BoundIdentifierExpression)
        {
            var loc = _ctx.Location.ResolveExpressionLocation(cond);
            if (loc != null)
            {
                block.Instructions.Add(new IrPicCompareLiteral(loc, 0m, result, (int)BoundBinaryOperatorKind.NotEqual));
                return;
            }
        }

        if (cond is BoundLiteralExpression condLit)
        {
            if (condLit.Value is bool boolVal)
            {
                block.Instructions.Add(new IrSetBool(result, boolVal));
                return;
            }
            if (condLit.Value is decimal dv)
            {
                block.Instructions.Add(new IrSetBool(result, dv != 0m));
                return;
            }
            if (condLit.Value is string sv)
            {
                block.Instructions.Add(new IrSetBool(result, !string.IsNullOrWhiteSpace(sv)));
                return;
            }
        }

        if (cond is BoundAbbreviatedExpression)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0503, SourceLocation.None, TextSpan.Empty,
                "BoundAbbreviatedExpression (unresolved)");
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0503, SourceLocation.None, TextSpan.Empty, cond.GetType().Name);
    }

    // ── Comparison matrix dispatch ──

    public void LowerComparison(BoundBinaryExpression binCond, IrValue result, IrBasicBlock block)
    {
        var left = NormalizeOperand(binCond.Left);
        var right = NormalizeOperand(binCond.Right);

        if (left == null || right == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0504, SourceLocation.None, TextSpan.Empty, binCond.Left.GetType().Name, binCond.Right.GetType().Name);
            return;
        }

        int op = (int)binCond.OperatorKind;

        if (left.Kind != ComparisonOperandKind.Location && right.Kind == ComparisonOperandKind.Location)
        {
            (left, right) = (right, left);
            op = (int)FlipComparisonOp(binCond.OperatorKind);
        }

        bool useNumeric = IsNumericComparison(left, right);

        switch (left.Kind, right.Kind)
        {
            case (ComparisonOperandKind.Location, ComparisonOperandKind.Location):
                if (useNumeric)
                {
                    block.Instructions.Add(new IrPicCompare(left.Location!, right.Location!, result, op));
                }
                else
                {
                    bool eitherNumeric = left.Category == CobolCategory.Numeric
                                     || right.Category == CobolCategory.Numeric;
                    if (eitherNumeric)
                        block.Instructions.Add(new IrPicCompare(left.Location!, right.Location!, result, op));
                    else if (_ctx.Semantic.ProgramCollatingSequence is { } seq)
                        block.Instructions.Add(new IrStringCompareWithSequence(left.Location!, right.Location!, seq, result, op));
                    else
                        block.Instructions.Add(new IrStringCompare(left.Location!, right.Location!, result, op));
                }
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.NumericLiteral):
                block.Instructions.Add(new IrPicCompareLiteral(left.Location!, right.NumericValue, result, op));
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.StringLiteral):
                if (useNumeric && decimal.TryParse(right.StringValue,
                    System.Globalization.CultureInfo.InvariantCulture, out var numFromStr))
                    block.Instructions.Add(new IrPicCompareLiteral(left.Location!, numFromStr, result, op));
                else if (_ctx.Semantic.ProgramCollatingSequence is { } litSeq)
                    block.Instructions.Add(new IrStringCompareLiteralWithSequence(left.Location!, right.StringValue!, litSeq, result, op));
                else
                    block.Instructions.Add(new IrStringCompareLiteral(left.Location!, right.StringValue!, result, op));
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.Figurative):
                EmitLocationVsFigurative(left, right, result, op, block);
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.ArithmeticExpression):
            {
                var accumulator = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
                var irRight = _ctx.Expression.LowerExpression(right.ArithExpr!) ?? new IrLiteral(0m);
                block.Instructions.Add(new IrComputeIntoAccumulator(accumulator, irRight));
                block.Instructions.Add(new IrPicCompareAccumulator(left.Location!, accumulator, result, op));
                break;
            }

            case (ComparisonOperandKind.NumericLiteral, ComparisonOperandKind.NumericLiteral):
                int cmp = Math.Sign(left.NumericValue.CompareTo(right.NumericValue));
                bool constResult = EvaluateComparisonResult(cmp, (BoundBinaryOperatorKind)op);
                block.Instructions.Add(new IrSetBool(result, constResult));
                break;

            case (ComparisonOperandKind.StringLiteral, ComparisonOperandKind.StringLiteral):
                int strCmp = string.Compare(left.StringValue, right.StringValue, StringComparison.Ordinal);
                bool strResult = EvaluateComparisonResult(Math.Sign(strCmp), (BoundBinaryOperatorKind)op);
                block.Instructions.Add(new IrSetBool(result, strResult));
                break;

            case (ComparisonOperandKind.ArithmeticExpression, ComparisonOperandKind.ArithmeticExpression):
            {
                var leftAcc = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
                var rightAcc = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
                var irLeft = _ctx.Expression.LowerExpression(left.ArithExpr!) ?? new IrLiteral(0m);
                var irRightE = _ctx.Expression.LowerExpression(right.ArithExpr!) ?? new IrLiteral(0m);
                block.Instructions.Add(new IrComputeIntoAccumulator(leftAcc, irLeft));
                block.Instructions.Add(new IrComputeIntoAccumulator(rightAcc, irRightE));
                block.Instructions.Add(new IrDecimalCompare(leftAcc, rightAcc, result, op));
                break;
            }

            case (ComparisonOperandKind.ArithmeticExpression, ComparisonOperandKind.NumericLiteral):
            {
                var accumulator = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
                var irLeftE = _ctx.Expression.LowerExpression(left.ArithExpr!) ?? new IrLiteral(0m);
                block.Instructions.Add(new IrComputeIntoAccumulator(accumulator, irLeftE));
                block.Instructions.Add(new IrDecimalCompareLiteral(accumulator, right.NumericValue, result, op));
                break;
            }

            case (ComparisonOperandKind.NumericLiteral, ComparisonOperandKind.ArithmeticExpression):
            {
                var accumulator = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
                var irRightE2 = _ctx.Expression.LowerExpression(right.ArithExpr!) ?? new IrLiteral(0m);
                block.Instructions.Add(new IrComputeIntoAccumulator(accumulator, irRightE2));
                block.Instructions.Add(new IrDecimalCompareLiteral(accumulator, left.NumericValue, result, (int)FlipComparisonOp((BoundBinaryOperatorKind)op)));
                break;
            }

            default:
                _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0505, SourceLocation.None, TextSpan.Empty, left.Kind, right.Kind);
                break;
        }
    }

    // ── Classification helpers ──

    internal static bool IsNumericComparison(ComparisonOperand left, ComparisonOperand right)
    {
        return IsStrictlyNumeric(left) && IsStrictlyNumeric(right);
    }

    internal static bool IsStrictlyNumeric(ComparisonOperand op)
    {
        return op.Kind switch
        {
            ComparisonOperandKind.Location => op.Category == CobolCategory.Numeric,
            ComparisonOperandKind.NumericLiteral => true,
            ComparisonOperandKind.Figurative => op.FigurativeKind == FigurativeKind.Zero,
            ComparisonOperandKind.StringLiteral => false,
            ComparisonOperandKind.ArithmeticExpression => true,
            _ => false
        };
    }

    private void EmitLocationVsFigurative(ComparisonOperand loc, ComparisonOperand fig,
        IrValue result, int op, IrBasicBlock block)
    {
        var fk = fig.FigurativeKind;
        bool isNumeric = loc.Category.IsNumericLike();

        if (isNumeric && fk == FigurativeKind.Zero)
        {
            block.Instructions.Add(new IrPicCompareLiteral(loc.Location!, 0m, result, op));
            return;
        }

        string figStr = MakeFigurativeString(fk, loc.FieldWidth, fig.AllLiteral);
        block.Instructions.Add(new IrStringCompareLiteral(loc.Location!, figStr, result, op));
    }

    internal static bool EvaluateComparisonResult(int sign, BoundBinaryOperatorKind op)
    {
        return op switch
        {
            BoundBinaryOperatorKind.Equal => sign == 0,
            BoundBinaryOperatorKind.NotEqual => sign != 0,
            BoundBinaryOperatorKind.Less => sign < 0,
            BoundBinaryOperatorKind.LessOrEqual => sign <= 0,
            BoundBinaryOperatorKind.Greater => sign > 0,
            BoundBinaryOperatorKind.GreaterOrEqual => sign >= 0,
            _ => false
        };
    }

    internal static BoundBinaryOperatorKind FlipComparisonOp(BoundBinaryOperatorKind op)
    {
        return op switch
        {
            BoundBinaryOperatorKind.Less => BoundBinaryOperatorKind.Greater,
            BoundBinaryOperatorKind.LessOrEqual => BoundBinaryOperatorKind.GreaterOrEqual,
            BoundBinaryOperatorKind.Greater => BoundBinaryOperatorKind.Less,
            BoundBinaryOperatorKind.GreaterOrEqual => BoundBinaryOperatorKind.LessOrEqual,
            _ => op
        };
    }

    // ── Sign, class, user-class, condition-name ──

    public void LowerSignCondition(BoundSignConditionExpression sc, IrValue result, IrBasicBlock block)
    {
        var op = sc.SignKind switch
        {
            SignConditionKind.Positive => BoundBinaryOperatorKind.Greater,
            SignConditionKind.Negative => BoundBinaryOperatorKind.Less,
            SignConditionKind.Zero => BoundBinaryOperatorKind.Equal,
            _ => BoundBinaryOperatorKind.Equal
        };

        var zero = new BoundLiteralExpression(0m, CobolCategory.Numeric);
        var comparison = new BoundBinaryExpression(sc.Subject, op, zero, CobolCategory.Unknown);

        if (sc.IsNegated)
        {
            var tmp = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(comparison, tmp, block);
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Not));
        }
        else
        {
            LowerCondition(comparison, result, block);
        }
    }

    public void LowerClassCondition(BoundClassConditionExpression cc, IrValue result, IrBasicBlock block)
    {
        var loc = _ctx.Location.ResolveExpressionLocation(cc.Subject);
        if (loc == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0503, SourceLocation.None, TextSpan.Empty,
                $"class condition on {cc.Subject.GetType().Name}");
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var tmp = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrClassCondition(loc, (int)cc.ClassKind, tmp));

        if (cc.IsNegated)
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Not));
        else
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Or));
    }

    public void LowerUserClassCondition(BoundUserClassConditionExpression ucc, IrValue result, IrBasicBlock block)
    {
        var loc = _ctx.Location.ResolveExpressionLocation(ucc.Subject);
        if (loc == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0503, SourceLocation.None, TextSpan.Empty,
                $"user class condition on {ucc.Subject.GetType().Name}");
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var tmp = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrUserClassCondition(loc, ucc.ClassDef.ValidBytes, tmp));

        if (ucc.IsNegated)
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Not));
        else
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Or));
    }

    public void LowerConditionName(BoundConditionNameExpression cn, IrValue result, IrBasicBlock block)
    {
        var parentSym = cn.Condition.ParentDataItem;
        var parentLoc = cn.ParentExpression != null
            ? _ctx.Location.ResolveExpressionLocation(cn.ParentExpression)
            : _ctx.Location.ResolveLocation(parentSym);
        if (parentLoc == null)
        {
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var ranges = cn.Condition.ValueRanges;
        if (ranges.Count == 0)
        {
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var parentCat = parentLoc.GetPic().Category;
        var matchResults = new List<IrValue>();

        foreach (var range in ranges)
        {
            var from = range.From;
            var to = range.To;
            var matchVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);

            if (to == null)
            {
                if (from.IsNumeric)
                {
                    block.Instructions.Add(new IrPicCompareLiteral(
                        parentLoc, from.NumericValue!.Value, matchVal,
                        (int)BoundBinaryOperatorKind.Equal));
                }
                else if (from.IsString)
                {
                    if (parentCat.IsNumericLike() && decimal.TryParse(from.StringValue!,
                        System.Globalization.CultureInfo.InvariantCulture, out var numVal))
                    {
                        block.Instructions.Add(new IrPicCompareLiteral(
                            parentLoc, numVal, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                    else
                    {
                        block.Instructions.Add(new IrStringCompareLiteral(
                            parentLoc, from.StringValue!, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                }
            }
            else
            {
                var geVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                var leVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);

                if (from.IsNumeric && to.IsNumeric)
                {
                    block.Instructions.Add(new IrPicCompareLiteral(
                        parentLoc, from.NumericValue!.Value, geVal,
                        (int)BoundBinaryOperatorKind.GreaterOrEqual));
                    block.Instructions.Add(new IrPicCompareLiteral(
                        parentLoc, to.NumericValue!.Value, leVal,
                        (int)BoundBinaryOperatorKind.LessOrEqual));
                }
                else if (from.IsString && to.IsString)
                {
                    block.Instructions.Add(new IrStringCompareLiteral(
                        parentLoc, from.StringValue!, geVal,
                        (int)BoundBinaryOperatorKind.GreaterOrEqual));
                    block.Instructions.Add(new IrStringCompareLiteral(
                        parentLoc, to.StringValue!, leVal,
                        (int)BoundBinaryOperatorKind.LessOrEqual));
                }

                block.Instructions.Add(new IrBinaryLogical(matchVal, geVal, leVal, IrLogicalOp.And));
            }

            matchResults.Add(matchVal);
        }

        if (matchResults.Count == 1)
        {
            block.Instructions.Add(new IrBinaryLogical(result, matchResults[0], matchResults[0], IrLogicalOp.Or));
        }
        else
        {
            var accumulated = matchResults[0];
            for (int i = 1; i < matchResults.Count; i++)
            {
                var orResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                block.Instructions.Add(new IrBinaryLogical(orResult, accumulated, matchResults[i], IrLogicalOp.Or));
                accumulated = orResult;
            }
            block.Instructions.Add(new IrBinaryLogical(result, accumulated, accumulated, IrLogicalOp.Or));
        }

        if (cn.IsNegated)
        {
            var notResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrBinaryLogical(notResult, result, result, IrLogicalOp.Not));
            block.Instructions.Add(new IrBinaryLogical(result, notResult, notResult, IrLogicalOp.Or));
        }
    }

    // ── Conditional branching (AT END, INVALID KEY, ON EXCEPTION) ──

    public IrBasicBlock LowerConditionalBranch(
        IReadOnlyList<BoundStatement> onTrue,
        IReadOnlyList<BoundStatement> onFalse,
        IrValue conditionResult,
        IrMethod method,
        IrBasicBlock block,
        string labelPrefix)
    {
        var trueBlock = method.CreateBlock($"{labelPrefix}.true");
        var falseBlock = method.CreateBlock($"{labelPrefix}.false");
        var afterBlock = method.CreateBlock($"{labelPrefix}.after");

        block.Instructions.Add(new IrBranchIfFalse(conditionResult, falseBlock));
        block.Instructions.Add(new IrJump(trueBlock));

        method.Blocks.Add(trueBlock);
        var current = trueBlock;
        foreach (var stmt in onTrue)
            current = _ctx.LowerStatement(stmt, method, current);
        current.Instructions.Add(new IrJump(afterBlock));

        method.Blocks.Add(falseBlock);
        current = falseBlock;
        foreach (var stmt in onFalse)
            current = _ctx.LowerStatement(stmt, method, current);
        current.Instructions.Add(new IrJump(afterBlock));

        method.Blocks.Add(afterBlock);
        return afterBlock;
    }
}
