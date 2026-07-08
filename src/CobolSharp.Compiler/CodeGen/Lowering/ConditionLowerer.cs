// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
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
        // Any numeric-valued expression evaluated via the arithmetic accumulator path
        // (arithmetic binary expression, or a numeric intrinsic-function call).
        public BoundExpression? ArithExpr { get; init; }

        private ComparisonOperand(ComparisonOperandKind kind) { Kind = kind; }

        public static ComparisonOperand FromLocation(IrLocation loc, CobolCategory cat, int width) =>
            new(ComparisonOperandKind.Location) { Location = loc, Category = cat, FieldWidth = width };
        public static ComparisonOperand FromNumeric(decimal value) =>
            new(ComparisonOperandKind.NumericLiteral) { NumericValue = value, Category = CobolCategory.Numeric };
        public static ComparisonOperand FromString(string value) =>
            new(ComparisonOperandKind.StringLiteral) { StringValue = value, Category = CobolCategory.Alphanumeric };
        public static ComparisonOperand FromFigurative(FigurativeKind kind, string? allLiteral = null) =>
            new(ComparisonOperandKind.Figurative) { FigurativeKind = kind, AllLiteral = allLiteral };
        public static ComparisonOperand FromArithmeticExpression(BoundExpression expr) =>
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

            // A numeric intrinsic-function call (FUNCTION ACOS(...), FUNCTION MEAN(...), …)
            // used directly as a comparison operand is evaluated like an arithmetic
            // expression — lowered to the decimal accumulator and compared numerically.
            case BoundFunctionCallExpression fn when fn.Category.IsNumericLike():
                return ComparisonOperand.FromArithmeticExpression(fn);

            // LINAGE-COUNTER special register: a runtime numeric value, compared like an arithmetic
            // expression (lowered to the decimal accumulator → IrLinageCounter).
            case BoundLinageCounterExpression lc:
                return ComparisonOperand.FromArithmeticExpression(lc);

            // LINE-COUNTER / PAGE-COUNTER special registers: runtime numeric values (Report Writer),
            // compared like an arithmetic expression (lowered to IrLineCounter / IrPageCounter).
            case BoundLineCounterExpression lineCtr:
                return ComparisonOperand.FromArithmeticExpression(lineCtr);
            case BoundPageCounterExpression pageCtr:
                return ComparisonOperand.FromArithmeticExpression(pageCtr);

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
            FigurativeKind.Null => '\x00',   // NULL = the all-zero pointer handle (ISO §8.4.3.10)
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
                case BoundBinaryOperatorKind.Xor:
                {
                    // Logical exclusive-or (ISO §8.8.4.9): true iff exactly one operand condition is true.
                    var leftVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.Xor));
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

    /// <summary>
    /// If one side of the comparison is an alphanumeric intrinsic-function call, lower it to a
    /// string-value comparison (IrStringExprCompare) against the other operand (a literal,
    /// figurative, or field). The function is normalized to the left, flipping the operator if
    /// it was on the right. Returns false if neither side is an alphanumeric function (or the
    /// other operand can't be resolved), so the caller uses the normal comparison path.
    /// </summary>
    private bool TryLowerStringFunctionComparison(BoundBinaryExpression bin, IrValue result, IrBasicBlock block)
    {
        bool leftIsStrFn = bin.Left is BoundFunctionCallExpression lf && !lf.Category.IsNumericLike();
        bool rightIsStrFn = bin.Right is BoundFunctionCallExpression rf && !rf.Category.IsNumericLike();
        if (!leftIsStrFn && !rightIsStrFn) return false;

        BoundExpression fnExpr;
        BoundExpression other;
        int op = (int)bin.OperatorKind;
        if (leftIsStrFn)
        {
            fnExpr = bin.Left; other = bin.Right;
        }
        else
        {
            fnExpr = bin.Right; other = bin.Left;
            op = (int)FlipComparisonOp((BoundBinaryOperatorKind)op);
        }

        var fnIr = _ctx.Expression.LowerExpression(fnExpr);
        if (fnIr == null) return false;

        string? rightLiteral = null;
        IrLocation? rightLocation = null;
        switch (other)
        {
            case BoundLiteralExpression { Value: string s }:
                rightLiteral = s;
                break;
            case BoundFigurativeExpression figg:
                rightLiteral = MakeFigurativeString((FigurativeKind)figg.FigurativeKind, 0, figg.AllLiteral);
                break;
            default:
                rightLocation = _ctx.Location.ResolveExpressionLocation(other);
                if (rightLocation == null) return false;
                break;
        }

        block.Instructions.Add(new IrStringExprCompare(fnIr, rightLocation, rightLiteral, result, op));
        return true;
    }

    /// <summary>
    /// Stage-4 pointers: if this is a pointer relation (<c>p = / NOT = q</c> or <c>p = / NOT = NULL</c>), emit an
    /// <see cref="IrPointerCompare"/> (address identity on the managed pointer fields) and return true. Returns false
    /// for any non-pointer comparison so the caller uses the normal byte/numeric path.
    /// </summary>
    private bool TryLowerPointerComparison(BoundBinaryExpression bin, IrValue result, IrBasicBlock block)
    {
        static bool IsPtr(BoundExpression e) =>
            e is BoundIdentifierExpression id && id.Symbol.ResolvedType?.Category == CobolCategory.Pointer;
        static bool IsNull(BoundExpression e) =>
            e is BoundFigurativeExpression f && f.FigurativeKind == FigurativeKind.Null;

        var left = bin.Left;
        var right = bin.Right;
        if (!IsPtr(left) && !IsPtr(right))
            return false;   // not a pointer comparison

        // Only equality/inequality are defined for pointers (ISO §8.8.4.1.4).
        if (bin.OperatorKind is not (BoundBinaryOperatorKind.Equal or BoundBinaryOperatorKind.NotEqual))
            return false;

        // Normalize the pointer operand to the left (= / NOT = are symmetric, so no operator flip is needed).
        if (!IsPtr(left))
            (left, right) = (right, left);

        if (left is not BoundIdentifierExpression leftId
            || !_ctx.PointerFieldRefs.TryGetValue(leftId.Symbol, out var leftField))
            return false;

        string? rightField;
        if (IsNull(right))
            rightField = null;
        else if (right is BoundIdentifierExpression rightId
                 && _ctx.PointerFieldRefs.TryGetValue(rightId.Symbol, out var rf))
            rightField = rf;
        else
            return false;   // pointer vs non-pointer/non-NULL — fall through to the generic path/diagnostic

        bool negated = bin.OperatorKind == BoundBinaryOperatorKind.NotEqual;
        block.Instructions.Add(new IrPointerCompare(leftField, rightField, result, negated));
        return true;
    }

    // ── Comparison matrix dispatch ──

    public void LowerComparison(BoundBinaryExpression binCond, IrValue result, IrBasicBlock block)
    {
        // Pointers (Stage-4): a pointer compared (= / NOT =) against another pointer or NULL is an address-identity
        // test on the managed ManagedPointer fields, not a byte compare (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10).
        // Intercept BEFORE NormalizeOperand — a pointer item has no byte storage location to normalize.
        if (TryLowerPointerComparison(binCond, result, block))
            return;

        // An alphanumeric intrinsic-function call as a comparison operand (e.g.
        // IF FUNCTION UPPER-CASE(X) = "ABC") is evaluated to a string and compared by value.
        // (Numeric function calls are handled by NormalizeOperand's arithmetic-expression path.)
        if (TryLowerStringFunctionComparison(binCond, result, block))
            return;

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

        // A figurative ZERO compared with a numeric arithmetic expression (e.g. IF LINE-COUNTER = ZERO) is
        // the numeric value 0 (ISO §8.3.1.2), so normalize it to a numeric literal and let the numeric
        // arithmetic-expression cases handle it.
        if (left.Kind == ComparisonOperandKind.ArithmeticExpression
            && right.Kind == ComparisonOperandKind.Figurative && right.FigurativeKind == FigurativeKind.Zero)
            right = ComparisonOperand.FromNumeric(0m);
        else if (right.Kind == ComparisonOperandKind.ArithmeticExpression
            && left.Kind == ComparisonOperandKind.Figurative && left.FigurativeKind == FigurativeKind.Zero)
            left = ComparisonOperand.FromNumeric(0m);

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
                    // When PROGRAM COLLATING SEQUENCE is active and at least one operand is
                    // non-numeric, the comparison uses the custom collating sequence (ISO §14.9.17).
                    // The eitherNumeric shortcut to IrPicCompare must be bypassed in that case.
                    if (eitherNumeric && _ctx.Semantic.ProgramCollatingSequence == null)
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

        if (_ctx.Semantic.ProgramCollatingSequence is { } seq)
        {
            // Under a custom collating sequence, LOW-VALUE and HIGH-VALUE must be
            // remapped to the characters that hold the minimum/maximum weight,
            // and all figurative comparisons must use sequence-aware comparison.
            string figStr = MakeFigurativeStringWithSequence(fk, loc.FieldWidth, fig.AllLiteral, seq);
            block.Instructions.Add(new IrStringCompareLiteralWithSequence(loc.Location!, figStr, seq, result, op));
            return;
        }

        string figStrNative = MakeFigurativeString(fk, loc.FieldWidth, fig.AllLiteral);
        block.Instructions.Add(new IrStringCompareLiteral(loc.Location!, figStrNative, result, op));
    }

    /// <summary>
    /// Creates a figurative string with LOW-VALUE/HIGH-VALUE remapped to the characters
    /// that have the minimum/maximum weight in the custom collating sequence.
    /// </summary>
    internal static string MakeFigurativeStringWithSequence(FigurativeKind kind, int width,
        string? allLiteral, byte[] collatingSequence)
    {
        char fillChar = kind switch
        {
            FigurativeKind.Space => ' ',
            FigurativeKind.Zero => '0',
            FigurativeKind.Quote => '"',
            FigurativeKind.LowValue => FindCharWithMinWeight(collatingSequence),
            FigurativeKind.HighValue => FindCharWithMaxWeight(collatingSequence),
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

    /// <summary>
    /// Finds the character whose ordinal has the lowest weight in the collating sequence.
    /// When multiple characters share the same minimum weight, returns the one with the
    /// lowest ordinal (matching COBOL semantics for LOW-VALUE).
    /// </summary>
    private static char FindCharWithMinWeight(byte[] collatingSequence)
    {
        int minWeight = collatingSequence[0];
        int minOrdinal = 0;
        for (int i = 1; i < 256 && i < collatingSequence.Length; i++)
        {
            if (collatingSequence[i] < minWeight)
            {
                minWeight = collatingSequence[i];
                minOrdinal = i;
            }
        }
        return (char)minOrdinal;
    }

    /// <summary>
    /// Finds the character whose ordinal has the highest weight in the collating sequence.
    /// When multiple characters share the same maximum weight, returns the one with the
    /// highest ordinal (matching COBOL semantics for HIGH-VALUE).
    /// </summary>
    private static char FindCharWithMaxWeight(byte[] collatingSequence)
    {
        int maxWeight = collatingSequence[0];
        int maxOrdinal = 0;
        for (int i = 1; i < 256 && i < collatingSequence.Length; i++)
        {
            if (collatingSequence[i] > maxWeight)
            {
                maxWeight = collatingSequence[i];
                maxOrdinal = i;
            }
        }
        return (char)maxOrdinal;
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
                    var compareStr = from.StringValue!;

                    // ALL literal: repeat the pattern to fill the parent field length
                    if (from.IsAllLiteral && compareStr.Length > 0)
                    {
                        int parentLen = parentLoc.GetPic().StorageLength;
                        if (compareStr.Length < parentLen)
                        {
                            var sb = new System.Text.StringBuilder(parentLen);
                            while (sb.Length < parentLen)
                                sb.Append(compareStr);
                            compareStr = sb.ToString(0, parentLen);
                        }
                    }

                    if (parentCat.IsNumericLike() && decimal.TryParse(compareStr,
                        System.Globalization.CultureInfo.InvariantCulture, out var numVal))
                    {
                        block.Instructions.Add(new IrPicCompareLiteral(
                            parentLoc, numVal, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                    else
                    {
                        block.Instructions.Add(new IrStringCompareLiteral(
                            parentLoc, compareStr, matchVal,
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
