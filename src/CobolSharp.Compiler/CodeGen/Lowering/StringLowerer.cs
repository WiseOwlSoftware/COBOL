// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Lowers COBOL string manipulation statements to IR:
/// INSPECT (TALLYING/REPLACING/CONVERTING), STRING, UNSTRING.
/// M002 Stage 4: methods move here from Binder.
/// </summary>
internal sealed class StringLowerer
{
    private readonly LoweringContext _ctx;

    public StringLowerer(LoweringContext ctx) => _ctx = ctx;

    public void LowerInspect(BoundInspectStatement stmt, IrBasicBlock block)
    {
        var targetLoc = _ctx.Location.ResolveLocation(stmt.Target);
        if (targetLoc == null) return;

        foreach (var t in stmt.Tallying)
        {
            var counterLoc = _ctx.Location.ResolveLocation(t.Counter);
            if (counterLoc == null) continue;

            block.Instructions.Add(new IrInspectTally(
                targetLoc, counterLoc, (IR.InspectTallyKind)t.Kind,
                LowerInspectPattern(t.Pattern),
                LowerInspectPattern(t.Region.BeforePattern), t.Region.BeforeInitial,
                LowerInspectPattern(t.Region.AfterPattern), t.Region.AfterInitial));
        }

        foreach (var r in stmt.Replacing)
        {
            block.Instructions.Add(new IrInspectReplace(
                targetLoc, (IR.InspectReplaceKind)r.Kind,
                LowerInspectPattern(r.Pattern)!,
                LowerInspectPattern(r.Replacement)!,
                LowerInspectPattern(r.Region.BeforePattern), r.Region.BeforeInitial,
                LowerInspectPattern(r.Region.AfterPattern), r.Region.AfterInitial));
        }

        if (stmt.Converting != null)
        {
            block.Instructions.Add(new IrInspectConvert(
                targetLoc,
                LowerInspectPattern(stmt.Converting.FromSet)!,
                LowerInspectPattern(stmt.Converting.ToSet)!,
                LowerInspectPattern(stmt.Converting.Region.BeforePattern),
                stmt.Converting.Region.BeforeInitial,
                LowerInspectPattern(stmt.Converting.Region.AfterPattern),
                stmt.Converting.Region.AfterInitial));
        }
    }

    /// <summary>
    /// Convert a bound InspectPatternValue to an IR IrInspectPatternValue.
    /// Literals pass through. Data refs are resolved to IrLocations.
    /// </summary>
    private IR.IrInspectPatternValue? LowerInspectPattern(InspectPatternValue? pv)
    {
        if (pv == null) return null;
        if (pv.IsLiteral) return IR.IrInspectPatternValue.FromLiteral(pv.Literal!);
        if (pv.IsDataRef)
        {
            var loc = _ctx.Location.ResolveExpressionLocation(pv.DataRef!);
            if (loc != null) return IR.IrInspectPatternValue.FromLocation(loc);
        }
        return IR.IrInspectPatternValue.FromLiteral("");
    }

    public IrBasicBlock LowerString(BoundStringStatement str, IrMethod method, IrBasicBlock block)
    {
        var destLoc = _ctx.Location.ResolveExpressionLocation(str.Into);
        if (destLoc == null) return block;

        // Pointer: resolve if present, null if no WITH POINTER clause
        IrLocation? ptrLoc = null;
        if (str.Pointer != null)
            ptrLoc = _ctx.Location.ResolveExpressionLocation(str.Pointer);

        // Build sending specs — handle figurative constants and field delimiters
        var sendings = new List<IrStringSending>();
        foreach (var sending in str.Sendings)
        {
            string? delimiter = null;
            IrLocation? delimiterLoc = null;
            if (sending.Delimiter is BoundLiteralExpression delimLit && delimLit.Value is string ds)
                delimiter = ds;
            else if (sending.Delimiter is BoundFigurativeExpression delimFig)
                delimiter = DataMovementLowerer.FigurativeToStringHelper(delimFig);
            else if (sending.Delimiter is BoundIdentifierExpression)
                delimiterLoc = _ctx.Location.ResolveExpressionLocation(sending.Delimiter);

            if (sending.Value is BoundLiteralExpression litVal && litVal.Value is string sv)
            {
                sendings.Add(new IrStringSending(sv, null, delimiter, delimiterLoc, sending.DelimitedBySize));
            }
            else if (sending.Value is BoundFigurativeExpression figVal)
            {
                var figStr = DataMovementLowerer.FigurativeToStringHelper(figVal);
                if (figStr != null)
                    sendings.Add(new IrStringSending(figStr, null, delimiter, delimiterLoc, sending.DelimitedBySize));
            }
            else
            {
                var srcLoc = _ctx.Location.ResolveExpressionLocation(sending.Value);
                if (srcLoc != null)
                    sendings.Add(new IrStringSending(null, srcLoc, delimiter, delimiterLoc, sending.DelimitedBySize));
            }
        }

        // Emit single IrStringStatement
        var overflowResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrStringStatement(destLoc, sendings, ptrLoc, overflowResult));

        // ON OVERFLOW / NOT ON OVERFLOW branching
        if (str.OnOverflow.Count > 0 || str.NotOnOverflow.Count > 0)
        {
            var onBlock = method.CreateBlock("string.overflow");
            var notBlock = method.CreateBlock("string.not.overflow");
            var afterBlock = method.CreateBlock("string.after");

            block.Instructions.Add(new IrBranchIfFalse(overflowResult, notBlock));
            block.Instructions.Add(new IrJump(onBlock));

            method.Blocks.Add(onBlock);
            var onCurrent = onBlock;
            foreach (var stmt in str.OnOverflow)
                onCurrent = _ctx.LowerStatement(stmt, method, onCurrent);
            onCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(notBlock);
            var notCurrent = notBlock;
            foreach (var stmt in str.NotOnOverflow)
                notCurrent = _ctx.LowerStatement(stmt, method, notCurrent);
            notCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    public IrBasicBlock LowerUnstring(BoundUnstringStatement unstr, IrMethod method, IrBasicBlock block)
    {
        var srcLoc = _ctx.Location.ResolveExpressionLocation(unstr.Source);
        if (srcLoc == null) return block;

        // Resolve delimiter — literal, figurative constant, or field reference
        string? literalDelimiter = null;
        if (unstr.Delimiter is BoundLiteralExpression delimLit && delimLit.Value is string ds)
            literalDelimiter = ds;
        else if (unstr.Delimiter is BoundFigurativeExpression fig)
        {
            literalDelimiter = fig.FigurativeKind switch
            {
                Runtime.FigurativeKind.Zero => "0",
                Runtime.FigurativeKind.Space => " ",
                Runtime.FigurativeKind.Quote => "\"",
                Runtime.FigurativeKind.HighValue => "\xFF",
                Runtime.FigurativeKind.LowValue => "\x00",
                _ => fig.AllLiteral ?? "0"
            };
        }
        else if (unstr.Delimiter is BoundLiteralExpression numDelim && numDelim.Value is decimal dv)
            literalDelimiter = dv.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Resolve identifier-based delimiter (data reference)
        IrLocation? delimiterLoc = null;
        if (literalDelimiter == null && unstr.Delimiter is BoundIdentifierExpression delimId)
            delimiterLoc = _ctx.Location.ResolveExpressionLocation(delimId);

        // Pointer: resolve if present
        IrLocation? ptrLoc = null;
        if (unstr.Pointer != null)
            ptrLoc = _ctx.Location.ResolveExpressionLocation(unstr.Pointer);

        // Tallying: resolve if present
        IrLocation? tallyLoc = null;
        if (unstr.Tallying != null)
            tallyLoc = _ctx.Location.ResolveExpressionLocation(unstr.Tallying);

        // Build INTO specs
        var intos = new List<IrUnstringInto>();
        foreach (var into in unstr.Intos)
        {
            var targetLoc = _ctx.Location.ResolveExpressionLocation(into.Target);
            if (targetLoc == null) continue;

            IrLocation? countLoc = null;
            if (into.CountIn != null)
                countLoc = _ctx.Location.ResolveExpressionLocation(into.CountIn);

            IrLocation? delimInLoc = null;
            if (into.DelimiterIn != null)
                delimInLoc = _ctx.Location.ResolveExpressionLocation(into.DelimiterIn);

            intos.Add(new IrUnstringInto(targetLoc, countLoc, delimInLoc));
        }

        // Emit single IrUnstringStatement
        var overflowResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrUnstringStatement(srcLoc, literalDelimiter, unstr.DelimitedByAll,
            intos, ptrLoc, tallyLoc, overflowResult, delimiterLoc));

        // ON OVERFLOW / NOT ON OVERFLOW branching (same pattern as STRING)
        if (unstr.OnOverflow.Count > 0 || unstr.NotOnOverflow.Count > 0)
        {
            var onBlock = method.CreateBlock("unstring.overflow");
            var notBlock = method.CreateBlock("unstring.not.overflow");
            var afterBlock = method.CreateBlock("unstring.after");

            block.Instructions.Add(new IrBranchIfFalse(overflowResult, notBlock));
            block.Instructions.Add(new IrJump(onBlock));

            method.Blocks.Add(onBlock);
            var onCurrent = onBlock;
            foreach (var stmt in unstr.OnOverflow)
                onCurrent = _ctx.LowerStatement(stmt, method, onCurrent);
            onCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(notBlock);
            var notCurrent = notBlock;
            foreach (var stmt in unstr.NotOnOverflow)
                notCurrent = _ctx.LowerStatement(stmt, method, notCurrent);
            notCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }
}
