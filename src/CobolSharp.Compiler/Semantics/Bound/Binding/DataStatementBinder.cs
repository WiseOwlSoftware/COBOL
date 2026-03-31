// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Data movement statement binding: BindDisplay, BindMove, BindMoveSendingOperand,
/// BindSet (dispatch), BindSetSwitch, BindSetBoolean, BindSetToValue, BindSetIndex,
/// BindInitialize, ClassifyReplacingItem, BindReplacingValue, BindAccept.
/// </summary>
internal sealed class DataStatementBinder
{
    private readonly BindingContext _ctx;

    internal DataStatementBinder(BindingContext ctx) => _ctx = ctx;

    // ── DISPLAY ──

    internal BoundDisplayStatement BindDisplay(CobolParserCore.DisplayStatementContext ctx)
    {
        var operands = new List<BoundExpression>();

        foreach (var child in ctx.children)
        {
            if (child is ITerminalNode t)
            {
                var kind = t.Symbol.Type;
                if (kind == CobolLexer.DISPLAY || kind == CobolLexer.DOT)
                    continue;
            }

            if (child is CobolParserCore.DataReferenceContext idCtx)
            {
                operands.Add(_ctx.Expression.BindDataReferenceWithSubscripts(idCtx));
            }
            else if (child is CobolParserCore.LiteralContext litCtx)
            {
                operands.Add(_ctx.Expression.BindLiteral(litCtx));
            }
        }

        return new BoundDisplayStatement(operands);
    }

    // ── MOVE ──

    internal BoundStatement? BindMove(CobolParserCore.MoveStatementContext ctx)
    {
        // MOVE CORRESPONDING source TO target
        if (ctx.CORRESPONDING() != null)
            return _ctx.Arithmetic.BindCorresponding(CorrespondingKind.Move, ctx.dataReference(), ctx);

        var moveSource = ctx.moveSendingOperand();
        var moveTarget = ctx.moveReceivingPhrase();
        if (moveSource == null || moveTarget == null) return null;

        var source = BindMoveSendingOperand(moveSource);

        var targets = new List<BoundExpression>();
        var idList = moveTarget.dataReferenceList();
        if (idList != null)
        {
            foreach (var id in idList.dataReference())
                targets.Add(_ctx.Expression.BindDataReferenceWithSubscripts(id));
        }

        // MOVE type enforcement
        {
            var moveLoc = new SourceLocation("<source>", 0, ctx.Start?.Line ?? 0, 0);
            var moveSpan = TextSpan.Empty;
            foreach (var tgt in targets)
            {
                var tgtCat = tgt.Category;
                // Skip enforcement for group items (treated as alphanumeric byte move)
                if (tgt is BoundIdentifierExpression tgtId && tgtId.Symbol.IsGroup)
                    continue;
                // Skip enforcement for unknown categories
                if (tgtCat == CobolCategory.Unknown)
                    continue;

                // Determine effective source category for the MOVE check
                var effectiveSrcCat = source switch
                {
                    // ZERO/ZEROS/ZEROES is numerically compatible — treat as Numeric
                    BoundFigurativeExpression fig when fig.FigurativeKind == FigurativeKind.Zero
                        => CobolCategory.Numeric,
                    // Other figuratives (SPACE, HIGH-VALUE, etc.) are alphanumeric
                    BoundFigurativeExpression => CobolCategory.Alphanumeric,
                    // Numeric literals → Numeric
                    BoundLiteralExpression lit when lit.Category == CobolCategory.Numeric
                        => CobolCategory.Numeric,
                    // Other literals → their declared category
                    _ => source.Category,
                };

                if (effectiveSrcCat == CobolCategory.Unknown)
                    continue;

                if (!CategoryCompatibility.IsMoveLegal(effectiveSrcCat, tgtCat))
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0901, moveLoc, moveSpan, effectiveSrcCat, tgtCat);

                // Check 1: MOVE ZERO to Alphabetic
                if (source is BoundFigurativeExpression fig2
                    && fig2.FigurativeKind == FigurativeKind.Zero
                    && tgtCat == CobolCategory.Alphabetic)
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0908, moveLoc, moveSpan);

                // Check 2: HIGH-VALUE/LOW-VALUE/QUOTE to Numeric
                if (source is BoundFigurativeExpression fig3
                    && fig3.FigurativeKind is FigurativeKind.HighValue
                        or FigurativeKind.LowValue
                        or FigurativeKind.Quote
                    && tgtCat.IsNumericLike())
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0906, moveLoc, moveSpan, fig3.FigurativeKind);

                // Check 3: Numeric noninteger literal to Alphanumeric
                if (source is BoundLiteralExpression srcLit
                    && srcLit.Category == CobolCategory.Numeric
                    && srcLit.Value is decimal decVal
                    && decVal != decimal.Truncate(decVal)
                    && tgtCat.IsAlphanumericLike()
                    && !tgtCat.IsNumericLike())
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL0907, moveLoc, moveSpan);
            }
        }

        return new BoundMoveStatement(source, targets, isRounded: false);
    }

    internal BoundExpression BindMoveSendingOperand(CobolParserCore.MoveSendingOperandContext ctx)
    {
        // moveSource: literal | functionCall | dataReference (COBOL-85 + 1989 Amendment)
        var litCtx = ctx.literal();
        if (litCtx != null) return _ctx.Expression.BindLiteral(litCtx);

        if (ctx.functionCall() != null)
            return _ctx.Expression.BindFunctionCall(ctx.functionCall());

        if (ctx.dataReference() != null)
            return _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── SET ──

    internal BoundStatement? BindSet(CobolParserCore.SetStatementContext ctx)
    {
        // SET mnemonic-name+ TO {ON | OFF} (switch setting)
        if (ctx.setSwitchStatement() is { } swCtx)
            return BindSetSwitch(swCtx);

        // SET condition-name TO TRUE/FALSE
        if (ctx.setBooleanStatement() is { } boolCtx)
            return BindSetBoolean(boolCtx);

        // SET identifier TO value
        if (ctx.setToValueStatement() is { } toCtx)
            return BindSetToValue(toCtx);

        // SET identifier UP/DOWN BY integer
        if (ctx.setIndexStatement() is { } idxCtx)
            return BindSetIndex(idxCtx);

        return null;
    }

    internal BoundStatement? BindSetSwitch(CobolParserCore.SetSwitchStatementContext ctx)
    {
        // Grammar: SET (dataReference+ TO (ON | OFF))+
        // For the common case SET SW-1 SW-2 TO OFF, all refs share one ON/OFF.
        // For compound SET SW-1 TO ON SW-2 TO OFF, each group has its own.
        // Strategy: walk tokens by position to match refs to their ON/OFF.
        var switches = new List<(string Name, bool SetToOn)>();
        var refs = ctx.dataReference();
        var toTokens = ctx.TO();
        var onTokens = ctx.ON();
        var offTokens = ctx.OFF();

        int refIdx = 0;
        int onIdx = 0;
        int offIdx = 0;

        for (int toIdx = 0; toIdx < toTokens.Length; toIdx++)
        {
            int toPos = toTokens[toIdx].Symbol.TokenIndex;
            int nextToPos = (toIdx + 1 < toTokens.Length) ? toTokens[toIdx + 1].Symbol.TokenIndex : int.MaxValue;

            // Collect refs before this TO
            var targets = new List<string>();
            while (refIdx < refs.Length && refs[refIdx].Stop.TokenIndex < toPos)
            {
                targets.Add(refs[refIdx].cobolWord().GetText());
                refIdx++;
            }

            // Find the ON or OFF token between this TO and the next TO
            bool setToOn = false;
            if (onIdx < onTokens.Length && onTokens[onIdx].Symbol.TokenIndex > toPos && onTokens[onIdx].Symbol.TokenIndex < nextToPos)
            {
                setToOn = true;
                onIdx++;
            }
            else if (offIdx < offTokens.Length)
            {
                offIdx++;
            }

            foreach (var target in targets)
            {
                var switchInfo = _ctx.Semantic.ResolveImplementorSwitch(target);
                if (switchInfo != null)
                    switches.Add((switchInfo.ImplementorName, setToOn));
            }
        }

        if (switches.Count == 0) return null;
        // Return a bound node that the Binder can lower to IrSetSwitch
        return new BoundSetSwitchStatement(switches);
    }

    internal BoundStatement? BindSetBoolean(CobolParserCore.SetBooleanStatementContext ctx)
    {
        bool setToTrue = ctx.TRUE_() != null;
        var stmts = new List<BoundStatement>();

        foreach (var idCtx in ctx.dataReference())
        {
            string name = idCtx.cobolWord().GetText();
            var condSym = _ctx.Semantic.ResolveConditionName(name);
            if (condSym != null)
                stmts.Add(new BoundSetConditionStatement(condSym, setToTrue));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    internal BoundStatement? BindSetToValue(CobolParserCore.SetToValueStatementContext ctx)
    {
        var identifiers = ctx.dataReference();
        var valueExpr = _ctx.Expression.BindArithmeticExpr(ctx.arithmeticExpression());
        if (valueExpr == null) return null;

        var stmts = new List<BoundStatement>();
        foreach (var idCtx in identifiers)
        {
            // Check if it's a condition name first
            string name = idCtx.cobolWord().GetText();
            var condSym = _ctx.Semantic.ResolveConditionName(name);
            if (condSym != null)
            {
                stmts.Add(new BoundSetConditionStatement(condSym, true));
                continue;
            }

            // Regular data item: SET identifier TO value
            var targetId = _ctx.Expression.BindDataReferenceWithSubscripts(idCtx);
            if (targetId is not BoundIdentifierExpression boundTarget) continue;
            stmts.Add(new BoundSetIndexStatement(boundTarget, SetOperation.Assign, valueExpr));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    internal BoundStatement? BindSetIndex(CobolParserCore.SetIndexStatementContext ctx)
    {
        var op = ctx.UP() != null ? SetOperation.UpBy : SetOperation.DownBy;
        var deltaExpr = _ctx.Expression.BindArithmeticExpr(ctx.arithmeticExpression());
        if (deltaExpr == null) return null;

        var stmts = new List<BoundStatement>();
        foreach (var idCtx in ctx.dataReference())
        {
            var targetId = _ctx.Expression.BindDataReferenceWithSubscripts(idCtx);
            if (targetId is not BoundIdentifierExpression boundTarget) continue;
            stmts.Add(new BoundSetIndexStatement(boundTarget, op, deltaExpr));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    // ── INITIALIZE ──

    internal BoundStatement? BindInitialize(CobolParserCore.InitializeStatementContext ctx)
    {
        var targets = new List<DataSymbol>();
        var idList = ctx.dataReferenceList();
        if (idList == null) return null;

        foreach (var idCtx in idList.dataReference())
        {
            var sym = _ctx.Semantic.ResolveData(idCtx.cobolWord().GetText());
            if (sym != null) targets.Add(sym);
        }

        if (targets.Count == 0) return null;

        var categoryReplacements = new List<BoundInitializeCategoryReplacement>();
        var replacingPhrase = ctx.initializeReplacingPhrase();
        if (replacingPhrase != null)
        {
            foreach (var item in replacingPhrase.initializeReplacingItem())
            {
                var category = ClassifyReplacingItem(item);
                var valueExpr = BindReplacingValue(item);
                if (valueExpr != null)
                    categoryReplacements.Add(new BoundInitializeCategoryReplacement(category, valueExpr));
            }
        }

        return new BoundInitializeStatement(targets, categoryReplacements);
    }

    internal InitializeCategory ClassifyReplacingItem(CobolParserCore.InitializeReplacingItemContext ctx)
    {
        var cat = ctx.initializeCategory();
        if (cat.EDITED() != null || cat.ALPHANUMERIC_EDITED() != null || cat.NUMERIC_EDITED() != null)
        {
            if (cat.ALPHANUMERIC() != null || cat.ALPHANUMERIC_EDITED() != null) return InitializeCategory.AlphanumericEdited;
            return InitializeCategory.NumericEdited;
        }
        if (cat.ALPHABETIC() != null) return InitializeCategory.Alphabetic;
        if (cat.ALPHANUMERIC() != null) return InitializeCategory.Alphanumeric;
        return InitializeCategory.Numeric;
    }

    internal BoundExpression? BindReplacingValue(CobolParserCore.InitializeReplacingItemContext ctx)
    {
        var litCtx = ctx.literal();
        if (litCtx != null) return _ctx.Expression.BindLiteral(litCtx);

        var idCtx = ctx.dataReference();
        if (idCtx != null)
        {
            var sym = _ctx.Semantic.ResolveData(idCtx.cobolWord().GetText());
            if (sym != null) return new BoundIdentifierExpression(sym, sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric);
        }

        return null;
    }

    // ── ACCEPT ──

    internal BoundStatement? BindAccept(CobolParserCore.AcceptStatementContext ctx)
    {
        var targetId = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
        if (targetId is not BoundIdentifierExpression boundTarget) return null;

        var sourceKind = AcceptSourceKind.None;
        var sourceCtx = ctx.acceptSource();
        if (sourceCtx != null)
        {
            if (sourceCtx.DATE() != null && sourceCtx.YYYYMMDD() != null)
                sourceKind = AcceptSourceKind.DateYYYYMMDD;
            else if (sourceCtx.DATE() != null) sourceKind = AcceptSourceKind.Date;
            else if (sourceCtx.TIME() != null) sourceKind = AcceptSourceKind.Time;
            else if (sourceCtx.DAY_OF_WEEK() != null) sourceKind = AcceptSourceKind.DayOfWeek;
            else if (sourceCtx.DAY() != null && sourceCtx.YYYYDDD() != null)
                sourceKind = AcceptSourceKind.DayYYYYDDD;
            else if (sourceCtx.DAY() != null) sourceKind = AcceptSourceKind.Day;
        }

        return new BoundAcceptStatement(boundTarget, sourceKind);
    }
}
