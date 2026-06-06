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
                var srcLoc = ResolveCorrespondingChildLocation(src, corr.SourceGroupExpr);
                var dstLoc = ResolveCorrespondingChildLocation(dst, corr.TargetGroupExpr);
                if (srcLoc == null || dstLoc == null) continue;

                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dstLoc,
                    srcLoc.GetPic(), dstLoc.GetPic()));
            }
            return block;
        }

        block.Instructions.Add(new IrInitArithmeticStatus());
        int rounding = corr.RoundingMode;

        foreach (var (src, dst) in corr.Pairs)
        {
            var srcLoc = ResolveCorrespondingChildLocation(src, corr.SourceGroupExpr);
            var dstLoc = ResolveCorrespondingChildLocation(dst, corr.TargetGroupExpr);
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

    /// <summary>
    /// Resolves a CORRESPONDING child's location, applying the group's subscripts.
    /// When the group is subscripted, the child's offset is computed relative to
    /// the subscripted group base, preserving runtime subscript semantics.
    /// When the group is not subscripted, falls back to plain symbol resolution.
    /// </summary>
    private IrLocation? ResolveCorrespondingChildLocation(DataSymbol child, BoundIdentifierExpression groupExpr)
    {
        if (!groupExpr.IsSubscripted)
            return _ctx.Location.ResolveLocation(child);

        // Resolve the group expression (with subscripts) to get the base location
        var groupLoc = _ctx.Location.ResolveLocation(groupExpr);
        if (groupLoc == null) return null;

        // Get the child's and group's unsubscripted storage locations
        var childStorage = _ctx.Semantic.GetStorageLocation(child);
        var groupStorage = _ctx.Semantic.GetStorageLocation(groupExpr.Symbol);
        if (!childStorage.HasValue || !groupStorage.HasValue) return null;

        int delta = childStorage.Value.Offset - groupStorage.Value.Offset;
        var childPic = childStorage.Value.Pic;
        int childLen = childStorage.Value.Length;

        if (groupLoc is IrStaticLocation staticGroup)
        {
            // Constant subscript: compute final offset at compile time
            return new IrStaticLocation(
                new StorageLocation(staticGroup.Location.Area,
                    staticGroup.Location.Offset + delta,
                    childLen, childPic));
        }
        else if (groupLoc is IrElementRef elemRef)
        {
            // Variable subscript: create a new IrElementRef with child's offset baked in.
            // Adjust the base location offset by delta, keep same subscripts/multipliers.
            var adjustedBase = new StorageLocation(
                elemRef.BaseLocation.Area,
                elemRef.BaseLocation.Offset + delta,
                childLen, childPic);
            return new IrElementRef(adjustedBase, elemRef.Subscripts, elemRef.Multipliers,
                childLen, childPic);
        }

        return null;
    }

    public void LowerMove(BoundMoveStatement mv, IrBasicBlock block)
    {
        IrLocation? preResolvedSrc = null;
        if (mv.Source is not BoundFigurativeExpression and not BoundLiteralExpression
            and not BoundFunctionCallExpression and not BoundLinageCounterExpression
            and not BoundLineCounterExpression and not BoundPageCounterExpression)
        {
            preResolvedSrc = _ctx.Location.ResolveExpressionLocation(mv.Source);
            if (preResolvedSrc is IrElementRef or IrRefModLocation && mv.Targets.Count > 1)
                preResolvedSrc = new IrCachedLocation(preResolvedSrc, _ctx.NextCacheKey());
        }

        foreach (var t in mv.Targets)
        {
            // MOVE target is a receiving operand: an ODO group whose DEPENDING ON object is
            // within it uses its maximum length (ISO OCCURS GR 7) so every occurrence is written.
            var destLoc = _ctx.Location.ResolveExpressionLocation(t, receiving: true);
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
            else if (mv.Source is BoundLinageCounterExpression
                or BoundLineCounterExpression or BoundPageCounterExpression)
            {
                // LINAGE-COUNTER / LINE-COUNTER / PAGE-COUNTER are runtime numeric values (no storage):
                // evaluate and store into the receiving item with normal numeric MOVE conversion.
                var irExpr = _ctx.Expression.LowerExpression(mv.Source);
                if (irExpr != null)
                    block.Instructions.Add(new IrComputeStore(irExpr, destLoc, mv.IsRounded ? 1 : 0));
            }
            else if (mv.Source is BoundFunctionCallExpression userFn
                     && _ctx.IsUserFunction(userFn.FunctionName)
                     && _ctx.LowerUserFunctionCall(userFn, destLoc, block))
            {
                // MOVE FUNCTION user-name(args) TO dest → CALL "user-name" USING args RETURNING dest (UDF slice 3).
            }
            else if (mv.Source is BoundFunctionCallExpression func)
            {
                var irCall = _ctx.Expression.LowerExpression(func) as IrIntrinsicCall;
                if (irCall != null)
                {
                    // Carry the binder-computed result category to the emitter so it can
                    // dispatch string-vs-numeric correctly — including category-polymorphic
                    // MAX/MIN, which a static name list cannot classify.
                    bool returnsString = func.Category is Runtime.CobolCategory.Alphanumeric
                        or Runtime.CobolCategory.Alphabetic
                        or Runtime.CobolCategory.AlphanumericEdited;
                    block.Instructions.Add(new IrFunctionCall(
                        irCall.FunctionName, irCall.Arguments, destLoc, returnsString)
                    {
                        CollatingSequence = irCall.CollatingSequence
                    });
                }
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
            InitializeDataItem(target, [], stmt, block);
    }

    /// <summary>
    /// Lower INITIALIZE for one data item, expanding OCCURS tables so that every
    /// occurrence is initialized independently (ISO 1989:1985 14.x: INITIALIZE
    /// applies to each occurrence of a table element). <paramref name="subscriptPath"/>
    /// accumulates the 1-based occurrence index for each enclosing OCCURS level,
    /// outermost first, mirroring the order LocationResolver collects OCCURS levels.
    /// </summary>
    private void InitializeDataItem(
        DataSymbol item, List<int> subscriptPath, BoundInitializeStatement stmt, IrBasicBlock block)
    {
        // FILLER items are skipped unless WITH FILLER was specified (ISO §14.9.20).
        if (item.IsFiller && !stmt.WithFiller) return;

        // A table (OCCURS) item — whether group or elementary — is initialized one
        // occurrence at a time. Resolving the whole array as a single field is wrong:
        // for COMP/BINARY tables it can't even be encoded (a 10-element S9(3) COMP
        // array is 20 bytes, not a single 20-byte integer) and silently leaves the
        // storage untouched.
        if (item.Occurs is { MaxOccurs: > 1 })
        {
            for (int i = 1; i <= item.Occurs.MaxOccurs; i++)
            {
                subscriptPath.Add(i);
                InitializeOccurrence(item, subscriptPath, stmt, block);
                subscriptPath.RemoveAt(subscriptPath.Count - 1);
            }
            return;
        }

        InitializeOccurrence(item, subscriptPath, stmt, block);
    }

    /// <summary>
    /// Initialize a single occurrence (this item's OCCURS, if any, is already accounted
    /// for in <paramref name="subscriptPath"/>): recurse into group members, or assign
    /// the per-occurrence elementary location.
    /// </summary>
    private void InitializeOccurrence(
        DataSymbol item, List<int> subscriptPath, BoundInitializeStatement stmt, IrBasicBlock block)
    {
        if (item.IsGroup)
        {
            foreach (var child in item.Children)
            {
                if (child.LevelNumber == 66) continue; // RENAMES are aliases
                if (child.Redefines != null) continue;
                InitializeDataItem(child, subscriptPath, stmt, block);
            }
            return;
        }

        var loc = ResolveInitializeLocation(item, subscriptPath);
        if (loc == null) return;

        var pic = loc.GetPic();
        var category = ClassifyInitializeCategory(pic.Category);

        // Precedence per item (ISO §14.9.20): TO VALUE → REPLACING → category default.
        // (1) TO VALUE: an item with a VALUE clause (matching the optional category filter) is set to it.
        if (stmt.ToValue && ItemHasValueClause(item)
            && (stmt.ToValueCategory == null || stmt.ToValueCategory == category))
        {
            EmitValueClauseInit(item, loc, block);
            return;
        }

        // (2) REPLACING category DATA BY value.
        foreach (var repl in stmt.CategoryReplacements)
        {
            if (repl.Category == category)
            {
                EmitInitializeAssignment(loc, repl.Value, block);
                return;
            }
        }

        // (3) Category default — the legacy COBOL-85 form (no phrases) or an explicit THEN TO DEFAULT.
        // When TO VALUE is given without TO DEFAULT, items not covered above are left unchanged; likewise
        // an unmatched REPLACING leaves the item unchanged (the prior behavior, preserved here).
        bool applyDefault = stmt.ToDefault
            || (!stmt.ToValue && stmt.CategoryReplacements.Count == 0);
        if (!applyDefault) return;

        if (category == InitializeCategory.Numeric || category == InitializeCategory.NumericEdited)
            block.Instructions.Add(new IrPicMoveLiteralNumeric(loc, 0m));
        else
            block.Instructions.Add(new IrMoveFigurative(loc, FigurativeKind.Space));
    }

    /// <summary>True if the data item carries a VALUE clause (literal, figurative, or ALL-literal).</summary>
    private static bool ItemHasValueClause(DataSymbol item)
        => item.InitialValue != null || item.FigurativeInit != null || item.AllLiteralPattern != null;

    /// <summary>
    /// Emit the move that initializes <paramref name="item"/> to its declared VALUE (for INITIALIZE … TO
    /// VALUE), mirroring program-start VALUE semantics: figurative → fill; ALL "x" → repeat-fill; numeric
    /// literal → numeric move; alphanumeric literal → string move (clean, quote-stripped at bind time).
    /// </summary>
    private void EmitValueClauseInit(DataSymbol item, IrLocation loc, IrBasicBlock block)
    {
        if (item.AllLiteralPattern is { } pattern)
        {
            block.Instructions.Add(new IrMoveAllLiteral(loc, pattern));
            return;
        }
        if (item.FigurativeInit is { } fig)
        {
            block.Instructions.Add(new IrMoveFigurative(loc, fig));
            return;
        }
        if (item.InitialValue is { } val)
        {
            if (loc.GetPic().Category.IsNumericLike()
                && decimal.TryParse(val, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var d))
                block.Instructions.Add(new IrPicMoveLiteralNumeric(loc, d));
            else
                block.Instructions.Add(new IrMoveStringToField(loc, val));
        }
    }

    /// <summary>
    /// Resolve the storage location for one occurrence of an elementary item. With no
    /// enclosing OCCURS the whole field is resolved directly; otherwise a constant
    /// subscript list selects the occurrence, reusing LocationResolver's compile-time
    /// offset folding (which also narrows the PIC descriptor to a single element).
    /// </summary>
    private IrLocation? ResolveInitializeLocation(DataSymbol item, List<int> subscriptPath)
    {
        if (subscriptPath.Count == 0)
            return _ctx.Location.ResolveLocation(item);

        var subscripts = new List<BoundExpression>(subscriptPath.Count);
        foreach (var index in subscriptPath)
            subscripts.Add(new BoundLiteralExpression((decimal)index, CobolCategory.Numeric));

        var category = item.ResolvedType?.Category ?? CobolCategory.Numeric;
        return _ctx.Location.ResolveLocation(
            new BoundIdentifierExpression(item, category, subscripts));
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
