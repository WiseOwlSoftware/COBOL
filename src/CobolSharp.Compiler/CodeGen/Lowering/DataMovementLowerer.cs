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
/// Lowers COBOL data movement statements to IR: MOVE, MOVE CORRESPONDING,
/// INITIALIZE, SET condition-name, SET index-name.
/// </summary>
internal sealed class DataMovementLowerer
{
    private readonly LoweringContext _ctx;

    public DataMovementLowerer(LoweringContext ctx) => _ctx = ctx;

    public IrBasicBlock LowerCorresponding(BoundCorrespondingStatement corr, IrMethod method, IrBasicBlock block)
    {
        if (corr.CorrespondingKind == CorrespondingKind.Move)
        {
            foreach (var (src, dst) in corr.Pairs)
            {
                var srcLoc = _ctx.Location.ResolveLocation(src);
                var dstLoc = _ctx.Location.ResolveLocation(dst);
                if (srcLoc == null || dstLoc == null) continue;

                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dstLoc,
                    srcLoc.GetPic(), dstLoc.GetPic()));
            }
            return block;
        }

        block.Instructions.Add(new IrInitArithmeticStatus());
        int rounding = corr.IsRounded ? 1 : 0;

        foreach (var (src, dst) in corr.Pairs)
        {
            var srcLoc = _ctx.Location.ResolveLocation(src);
            var dstLoc = _ctx.Location.ResolveLocation(dst);
            if (srcLoc == null || dstLoc == null) continue;

            var accum = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
            block.Instructions.Add(new IrInitAccumulator(accum));
            block.Instructions.Add(new IrAccumulateField(accum, srcLoc));

            if (corr.CorrespondingKind == CorrespondingKind.Add)
                block.Instructions.Add(new IrAddAccumulatedToTarget(accum, dstLoc, rounding));
            else
                block.Instructions.Add(new IrSubtractAccumulatedFromTarget(accum, dstLoc, rounding));
        }

        return _ctx.Arithmetic.LowerSizeError(corr.SizeError, method, block);
    }

    public void LowerMove(BoundMoveStatement mv, IrBasicBlock block)
    {
        IrLocation? preResolvedSrc = null;
        if (mv.Source is not BoundFigurativeExpression and not BoundLiteralExpression
            and not BoundFunctionCallExpression)
        {
            preResolvedSrc = _ctx.Location.ResolveExpressionLocation(mv.Source);
            if (preResolvedSrc is IrElementRef or IrRefModLocation && mv.Targets.Count > 1)
                preResolvedSrc = new IrCachedLocation(preResolvedSrc, _ctx.NextCacheKey());
        }

        foreach (var t in mv.Targets)
        {
            var destLoc = _ctx.Location.ResolveExpressionLocation(t);
            if (destLoc == null) continue;

            if (mv.Source is BoundFigurativeExpression fig)
            {
                if (fig.AllLiteral != null)
                    block.Instructions.Add(new IrMoveAllLiteral(destLoc, fig.AllLiteral));
                else
                    block.Instructions.Add(new IrMoveFigurative(destLoc, fig.FigurativeKind));
            }
            else if (mv.Source is BoundLiteralExpression lit)
            {
                var destPic = destLoc.GetPic();
                if (lit.Value is string s)
                {
                    if (destPic.Category == CobolCategory.NumericEdited)
                    {
                        if (decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var numVal))
                            block.Instructions.Add(new IrPicMoveLiteralNumeric(destLoc, numVal));
                        else
                            block.Instructions.Add(new IrMoveStringToField(destLoc, s));
                    }
                    else
                    {
                        block.Instructions.Add(new IrMoveStringToField(destLoc, s));
                    }
                }
                else if (lit.Value is decimal d)
                {
                    if (destPic.Category.IsNumericLike())
                        block.Instructions.Add(new IrPicMoveLiteralNumeric(
                            destLoc, d, mv.IsRounded ? 1 : 0));
                    else
                    {
                        string display = lit.OriginalText != null
                            ? ExpressionLowerer.FormatLiteralForAlphanumeric(lit.OriginalText)
                            : ExpressionLowerer.FormatLiteralForAlphanumeric(d);
                        block.Instructions.Add(new IrMoveStringToField(destLoc, display));
                    }
                }
            }
            else if (mv.Source is BoundFunctionCallExpression func)
            {
                var irCall = _ctx.Expression.LowerExpression(func) as IrIntrinsicCall;
                if (irCall != null)
                    block.Instructions.Add(new IrFunctionCall(
                        irCall.FunctionName, irCall.Arguments, destLoc));
            }
            else
            {
                if (preResolvedSrc != null)
                {
                    block.Instructions.Add(new IrMoveFieldToField(
                        preResolvedSrc, destLoc,
                        preResolvedSrc.GetPic(), destLoc.GetPic(),
                        mv.IsRounded));
                }
            }
        }
    }

    public void LowerInitialize(BoundInitializeStatement stmt, IrBasicBlock block)
    {
        foreach (var target in stmt.Targets)
            InitializeDataItem(target, stmt, block);
    }

    private void InitializeDataItem(DataSymbol item, BoundInitializeStatement stmt, IrBasicBlock block)
    {
        if (item.IsFiller) return;

        if (item.IsGroup)
        {
            foreach (var child in item.Children)
            {
                if (child.Redefines != null) continue;
                InitializeDataItem(child, stmt, block);
            }
            return;
        }

        var loc = _ctx.Location.ResolveLocation(item);
        if (loc == null) return;

        var pic = loc.GetPic();
        var category = ClassifyInitializeCategory(pic.Category);

        foreach (var repl in stmt.CategoryReplacements)
        {
            if (repl.Category == category)
            {
                EmitInitializeAssignment(loc, repl.Value, block);
                return;
            }
        }

        if (stmt.CategoryReplacements.Count > 0)
            return;

        if (category == InitializeCategory.Numeric || category == InitializeCategory.NumericEdited)
            block.Instructions.Add(new IrPicMoveLiteralNumeric(loc, 0m));
        else
            block.Instructions.Add(new IrMoveFigurative(loc, FigurativeKind.Space));
    }

    internal static InitializeCategory ClassifyInitializeCategory(CobolCategory cat)
    {
        return cat switch
        {
            CobolCategory.Numeric => InitializeCategory.Numeric,
            CobolCategory.NumericEdited => InitializeCategory.NumericEdited,
            CobolCategory.Alphabetic => InitializeCategory.Alphabetic,
            CobolCategory.AlphanumericEdited => InitializeCategory.AlphanumericEdited,
            _ => InitializeCategory.Alphanumeric
        };
    }

    private void EmitInitializeAssignment(IrLocation dest, BoundExpression value, IrBasicBlock block)
    {
        var pic = dest.GetPic();
        if (value is BoundLiteralExpression lit)
        {
            if (lit.Value is decimal d)
            {
                if (pic.Category.IsNumericLike())
                    block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, d));
                else
                    block.Instructions.Add(new IrMoveStringToField(dest,
                        d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            else if (lit.Value is string s)
            {
                block.Instructions.Add(new IrMoveStringToField(dest, s));
            }
        }
        else if (value is BoundIdentifierExpression id)
        {
            var srcLoc = _ctx.Location.ResolveLocation(id);
            if (srcLoc != null)
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dest,
                    srcLoc.GetPic(), dest.GetPic()));
        }
    }

    public void LowerSetCondition(BoundSetConditionStatement stmt, IrBasicBlock block)
    {
        var parentSym = stmt.Condition.ParentDataItem;
        var parentLoc = _ctx.Location.ResolveLocation(parentSym);
        if (parentLoc == null) return;

        if (stmt.SetToTrue)
        {
            var ranges = stmt.Condition.ValueRanges;
            if (ranges.Count == 0) return;

            var firstVal = ranges[0].From;
            if (firstVal.IsNumeric)
                block.Instructions.Add(new IrPicMoveLiteralNumeric(parentLoc, firstVal.NumericValue!.Value));
            else if (firstVal.IsString)
                block.Instructions.Add(new IrMoveStringToField(parentLoc, firstVal.StringValue!));
        }
        else
        {
            var parentCat = parentLoc.GetPic().Category;
            if (parentCat.IsNumericLike())
            {
                var trueVals = stmt.Condition.ValueRanges.Select(r => r.From).ToList();
                decimal falseVal = 0m;
                foreach (var candidate in new[] { 0m, 1m, -1m, 99m })
                {
                    if (!trueVals.Any(v => v.IsNumeric && v.NumericValue == candidate))
                    {
                        falseVal = candidate;
                        break;
                    }
                }
                block.Instructions.Add(new IrPicMoveLiteralNumeric(parentLoc, falseVal));
            }
            else
            {
                block.Instructions.Add(new IrMoveFigurative(parentLoc, FigurativeKind.Space));
            }
        }
    }

    public void LowerSetIndex(BoundSetIndexStatement stmt, IrBasicBlock block)
    {
        var targetLoc = _ctx.Location.ResolveLocation(stmt.Target);
        if (targetLoc == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0510, SourceLocation.None, TextSpan.Empty, stmt.Target.Symbol.Name);
            return;
        }

        decimal? literalValue = ExpressionLowerer.TryEvalConstant(stmt.Value);
        IrLocation? valueLoc = null;
        if (literalValue == null && stmt.Value is BoundIdentifierExpression valId)
            valueLoc = _ctx.Location.ResolveLocation(valId);

        switch (stmt.Operation)
        {
            case SetOperation.Assign:
                if (literalValue.HasValue)
                {
                    if (targetLoc.GetPic().Category.IsNumericLike())
                        block.Instructions.Add(new IrPicMoveLiteralNumeric(targetLoc, literalValue.Value));
                    else
                        block.Instructions.Add(new IrMoveStringToField(targetLoc,
                            literalValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (valueLoc != null)
                {
                    block.Instructions.Add(new IrMoveFieldToField(
                        valueLoc, targetLoc,
                        valueLoc.GetPic(), targetLoc.GetPic()));
                }
                else
                {
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0511, SourceLocation.None, TextSpan.Empty, stmt.Target.Symbol.Name, stmt.Value.GetType().Name);
                }
                break;

            case SetOperation.UpBy:
                if (literalValue.HasValue)
                    block.Instructions.Add(new IrPicAddLiteral(targetLoc, literalValue.Value));
                else if (valueLoc != null)
                    block.Instructions.Add(new IrPicAdd(valueLoc, targetLoc));
                else
                {
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0512, SourceLocation.None, TextSpan.Empty, stmt.Target.Symbol.Name, stmt.Value.GetType().Name);
                }
                break;

            case SetOperation.DownBy:
                if (literalValue.HasValue)
                    block.Instructions.Add(new IrPicSubtractLiteral(targetLoc, literalValue.Value));
                else if (valueLoc != null)
                    block.Instructions.Add(new IrPicSubtract(valueLoc, targetLoc));
                else
                {
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0513, SourceLocation.None, TextSpan.Empty, stmt.Target.Symbol.Name, stmt.Value.GetType().Name);
                }
                break;
        }
    }

    public static string? FigurativeToStringHelper(BoundFigurativeExpression fig)
    {
        if (fig.AllLiteral != null) return fig.AllLiteral;
        return fig.FigurativeKind switch
        {
            FigurativeKind.Zero => "0",
            FigurativeKind.Space => " ",
            FigurativeKind.HighValue => "\xFF",
            FigurativeKind.LowValue => "\x00",
            FigurativeKind.Quote => "\"",
            _ => null
        };
    }
}
