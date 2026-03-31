// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Arithmetic statement binding: BindMultiply, BindAdd, BindSubtract, BindDivide,
/// BindCompute, BindCorresponding, ValidatedArithmetic, BindArithmeticTargets,
/// BindSizeErrorClause.
/// </summary>
internal sealed class ArithmeticStatementBinder
{
    private readonly BindingContext _ctx;

    internal ArithmeticStatementBinder(BindingContext ctx) => _ctx = ctx;

    // ── MULTIPLY ──

    internal BoundStatement BindMultiply(CobolParserCore.MultiplyStatementContext ctx)
    {
        var operand = _ctx.Expression.BindSimpleOperand(ctx.multiplyOperand());

        var byCtxs = ctx.multiplyByOperand();
        if (byCtxs.Length == 0)
            throw new InvalidOperationException(
                $"MULTIPLY statement has no BY operand (line {ctx.Start?.Line})");

        // First BY operand is always the second factor
        var firstByReceiver = byCtxs[0].receivingOperand();
        BoundExpression byOperand;
        if (firstByReceiver.dataReference() != null)
            byOperand = _ctx.Expression.BindDataReferenceWithSubscripts(firstByReceiver.dataReference());
        else
            byOperand = _ctx.Expression.BindLiteral(firstByReceiver.literal());

        var givingCtx = ctx.multiplyGivingPhrase();
        bool isGiving = givingCtx != null;
        var targets = new List<BoundArithmeticTarget>();

        if (isGiving)
        {
            targets = BindArithmeticTargets(givingCtx!.receivingArithmeticOperand());
        }
        else
        {
            // Non-GIVING: each BY operand is both factor and receiving item
            foreach (var byCtx in byCtxs)
            {
                var receiver = byCtx.receivingOperand();
                if (receiver.dataReference() != null)
                {
                    var sym = _ctx.Expression.BindDataReferenceWithSubscripts(receiver.dataReference());
                    if (sym is BoundIdentifierExpression boundBt)
                        targets.Add(new BoundArithmeticTarget(boundBt, byCtx.ROUNDED() != null));
                }
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException(
                $"MULTIPLY statement has no valid receiving items (line {ctx.Start?.Line})");

        var sizeError = BindSizeErrorClause(ctx.arithmeticOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Multiply, new[] { operand }, byOperand, targets, isGiving, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
    }

    // ── ADD ──

    internal BoundStatement BindAdd(CobolParserCore.AddStatementContext ctx)
    {
        // ADD CORRESPONDING source TO target [ROUNDED] [ON SIZE ERROR ...]
        if (ctx.CORRESPONDING() != null)
        {
            return BindCorresponding(CorrespondingKind.Add, ctx.dataReference(), ctx,
                ctx.ROUNDED() != null, BindSizeErrorClause(ctx.arithmeticOnSizeError()))
                ?? throw new InvalidOperationException(
                    $"ADD CORRESPONDING: could not resolve operands (line {ctx.Start?.Line})");
        }

        // ADD operand(s) TO target1 [ROUNDED] target2 [ROUNDED] ...
        var operandList = ctx.addOperandList();
        if (operandList == null)
            throw new InvalidOperationException($"ADD statement has no valid targets or operands (line {ctx.Start?.Line})");

        var addOps = operandList.addOperand();
        if (addOps.Length == 0)
            throw new InvalidOperationException($"ADD statement has no valid targets or operands (line {ctx.Start?.Line})");

        // Bind all operands
        var operands = new List<BoundExpression>();
        foreach (var op in addOps)
            operands.Add(_ctx.Expression.BindSimpleOperand(op));

        // TO targets (each with per-target ROUNDED) — optional for GIVING form
        var targets = new List<BoundArithmeticTarget>();
        var toPhrase = ctx.addToPhrase();
        if (toPhrase != null)
            targets = BindArithmeticTargets(toPhrase.receivingArithmeticOperand());

        // GIVING phrase: ADD A TO B GIVING C → C = A + B.
        // The TO items become additional operands (sources), not targets.
        bool isGiving = false;
        var givingPhrase = ctx.addGivingPhrase();
        if (givingPhrase != null)
        {
            var givingTargetCtxs = givingPhrase.receivingArithmeticOperand();
            if (givingTargetCtxs.Length > 0)
            {
                isGiving = true;
                // Move TO items to operands (they're addends in GIVING form)
                foreach (var t in targets)
                    operands.Add(t.Target);
                targets = BindArithmeticTargets(givingTargetCtxs);
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException(
                $"ADD statement has no targets (line {ctx.Start?.Line})");

        var sizeError = BindSizeErrorClause(ctx.arithmeticOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Add, operands, null, targets, isGiving, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
    }

    // ── SUBTRACT ──

    internal BoundStatement BindSubtract(CobolParserCore.SubtractStatementContext ctx)
    {
        // SUBTRACT CORRESPONDING source FROM target [ROUNDED] [ON SIZE ERROR ...]
        if (ctx.CORRESPONDING() != null)
        {
            return BindCorresponding(CorrespondingKind.Subtract, ctx.dataReference(), ctx,
                ctx.ROUNDED() != null, BindSizeErrorClause(ctx.arithmeticOnSizeError()))
                ?? throw new InvalidOperationException(
                    $"SUBTRACT CORRESPONDING: could not resolve operands (line {ctx.Start?.Line})");
        }

        // SUBTRACT operand(s) FROM target1 [ROUNDED] target2 [ROUNDED] ...
        // Multiple operands: SUBTRACT A B C FROM T → T = T - (A + B + C)
        var operandList = ctx.subtractOperandList();
        if (operandList == null)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        var subOperands = operandList.subtractOperand();
        if (subOperands.Length == 0)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        // Bind all operands (simple identifiers or literals)
        var operands = new List<BoundExpression>();
        foreach (var op in subOperands)
            operands.Add(_ctx.Expression.BindSimpleOperand(op));

        // FROM targets or literal (subtractFromPhrase → subtractFromOperand)
        var fromPhrase = ctx.subtractFromPhrase();
        if (fromPhrase == null)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        var fromOperand = fromPhrase.subtractFromOperand();
        var fromTargetCtxs = fromOperand.receivingArithmeticOperand();
        var targets = new List<BoundArithmeticTarget>();
        bool fromIsLiteral = false;

        if (fromTargetCtxs.Length > 0)
        {
            // FROM identifier [ROUNDED] ... (Format 1)
            targets = BindArithmeticTargets(fromTargetCtxs);
        }
        else if (fromOperand.receivingOperand() != null)
        {
            // FROM literal (Format 2 — requires GIVING)
            fromIsLiteral = true;
        }

        // If no targets and no GIVING, it's an error
        if (targets.Count == 0 && ctx.subtractGivingPhrase() == null)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        // GIVING phrase: SUBTRACT a FROM b GIVING c [ROUNDED] → c = b - a
        bool isGiving = false;
        BoundExpression? givingMinuend = null;
        var givingPhrase = ctx.subtractGivingPhrase();
        if (givingPhrase != null)
        {
            var givingTargetCtxs = givingPhrase.receivingArithmeticOperand();
            if (givingTargetCtxs.Length > 0)
            {
                isGiving = true;
                // The FROM operand becomes the minuend (b in "SUBTRACT a FROM b GIVING c")
                if (fromIsLiteral)
                {
                    givingMinuend = _ctx.Expression.BindReceivingOperand(fromOperand.receivingOperand());
                }
                else if (targets.Count > 0)
                {
                    // Use the original BoundIdentifierExpression (preserves subscripts)
                    givingMinuend = targets[0].Target;
                }
                targets = BindArithmeticTargets(givingTargetCtxs);
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets (line {ctx.Start?.Line})");

        var sizeError = BindSizeErrorClause(ctx.arithmeticOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Subtract, operands, givingMinuend, targets, isGiving, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
    }

    // ── DIVIDE ──

    internal BoundStatement BindDivide(CobolParserCore.DivideStatementContext ctx)
    {
        // DIVIDE operand INTO/BY ...
        var operandCtx = ctx.divideOperand();
        if (operandCtx == null)
            throw new InvalidOperationException($"DIVIDE statement has no valid targets or operands (line {ctx.Start?.Line})");

        var firstOperand = _ctx.Expression.BindSimpleOperand(operandCtx);
        bool isByForm = ctx.divideByPhrase() != null;
        BoundExpression? dividend = null;

        // Determine targets based on INTO vs BY form
        var targets = new List<BoundArithmeticTarget>();

        if (isByForm)
        {
            // DIVIDE a BY b GIVING c → divisor=b, dividend=a, target=c
            var byOperand = ctx.divideByPhrase().divideOperand();
            dividend = firstOperand;
            firstOperand = _ctx.Expression.BindSimpleOperand(byOperand);

            var givingPhrase = ctx.divideGivingPhrase();
            if (givingPhrase != null)
                targets = BindArithmeticTargets(givingPhrase.receivingArithmeticOperand());
        }
        else
        {
            // DIVIDE a INTO b → divisor=a, target=b (b = b / a)
            // INTO operand: givingReceiver (identifier | literal)
            var intoPhrase = ctx.divideIntoPhrase();
            BoundExpression? intoLiteral = null;
            if (intoPhrase != null)
            {
                var intoOp = intoPhrase.divideIntoOperand();
                var intoTargets = intoOp.receivingArithmeticOperand();
                if (intoTargets.Length > 0)
                {
                    targets = BindArithmeticTargets(intoTargets);
                }
                else if (intoOp.literal() != null)
                {
                    intoLiteral = _ctx.Expression.BindLiteral(intoOp.literal());
                }
            }

            // GIVING overrides INTO targets as destinations
            var givingPhrase = ctx.divideGivingPhrase();
            if (givingPhrase != null)
            {
                if (intoLiteral != null)
                    dividend = intoLiteral;
                else if (targets.Count > 0)
                    dividend = targets[0].Target;  // preserve subscripts from INTO operand
                targets = BindArithmeticTargets(givingPhrase.receivingArithmeticOperand());
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"DIVIDE statement has no valid targets or operands (line {ctx.Start?.Line})");

        // REMAINDER
        BoundIdentifierExpression? remainderTarget = null;
        var remPhrase = ctx.divideRemainderPhrase();
        if (remPhrase != null)
        {
            var remExpr = _ctx.Expression.BindDataReferenceWithSubscripts(remPhrase.dataReference());
            remainderTarget = remExpr as BoundIdentifierExpression;
        }

        var sizeError = BindSizeErrorClause(ctx.arithmeticOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Divide, new[] { firstOperand }, dividend, targets,
            isGiving: dividend != null, isByForm: isByForm, remainderTarget: remainderTarget, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
    }

    // ── COMPUTE ──

    internal BoundStatement BindCompute(CobolParserCore.ComputeStatementContext ctx)
    {
        // COMPUTE target1 [ROUNDED] target2 [ROUNDED] = expression
        var storeCtxs = ctx.computeStore();
        if (storeCtxs.Length == 0)
            throw new InvalidOperationException($"COMPUTE statement has no valid targets or operands (line {ctx.Start?.Line})");

        var targets = new List<BoundArithmeticTarget>();
        foreach (var s in storeCtxs)
        {
            var sym = _ctx.Expression.BindDataReferenceWithSubscripts(s.dataReference());
            if (sym is BoundIdentifierExpression boundS)
                targets.Add(new BoundArithmeticTarget(boundS, s.ROUNDED() != null));
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"COMPUTE statement has no valid targets or operands (line {ctx.Start?.Line})");

        // Bind the full arithmetic expression (recursive tree walk)
        var expr = _ctx.Expression.BindAdditiveExpression(ctx.arithmeticExpression().additiveExpression());

        var sizeError = BindSizeErrorClause(ctx.computeOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Compute, new[] { expr }, null, targets, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
    }

    // ── CORRESPONDING ──

    internal BoundCorrespondingStatement? BindCorresponding(
        CorrespondingKind kind,
        CobolParserCore.DataReferenceContext[] ids,
        ParserRuleContext ctx,
        bool isRounded = false,
        BoundSizeErrorClause? sizeError = null)
    {
        if (ids.Length < 2) return null;

        var srcExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ids[0]);
        var dstExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ids[1]);

        if (srcExpr is not BoundIdentifierExpression srcId ||
            dstExpr is not BoundIdentifierExpression dstId)
            return null;

        var srcSym = srcId.Symbol;
        var dstSym = dstId.Symbol;
        var loc = new SourceLocation("<source>", 0, ctx.Start.Line, ctx.Start.Column);
        var span = TextSpan.Empty;
        var kindName = kind.ToString().ToUpperInvariant();
        bool hasError = false;

        // Check 11: CORRESPONDING excludes RENAMES items (ISO §14.9.26)
        if (srcSym.LevelNumber == 66)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0414,
                loc, span, kindName, srcSym.DisplayName);
            hasError = true;
        }
        if (dstSym.LevelNumber == 66)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0414,
                loc, span, kindName, dstSym.DisplayName);
            hasError = true;
        }

        // Source must be a group item
        if (srcSym.IsElementary)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0403,
                loc, span, kindName, srcSym.DisplayName);
            hasError = true;
        }

        // Target must be a group item
        if (dstSym.IsElementary)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0403,
                loc, span, kindName, dstSym.DisplayName);
            hasError = true;
        }

        if (hasError)
            return new BoundCorrespondingStatement(kind, srcSym, dstSym, []);

        var pairs = CorrespondingMatcher.ComputeCorrespondingPairs(
            srcSym, dstSym, kindName, _ctx.Diagnostics, loc);
        return new BoundCorrespondingStatement(kind, srcSym, dstSym, pairs, isRounded, sizeError);
    }

    // ── SIZE ERROR CLAUSE ──

    /// <summary>
    /// Bind ON SIZE ERROR / NOT ON SIZE ERROR clause shared by all arithmetic statements.
    /// Handles both forms: ON+NOT, ON-only, NOT-only.
    /// </summary>
    internal BoundSizeErrorClause? BindSizeErrorClause(ParserRuleContext? ctx)
    {
        if (ctx == null) return null;

        // Get all imperativeStatement children using the tree API
        var imperatives = new List<CobolParserCore.StatementBlockContext>();
        for (int i = 0; i < ctx.ChildCount; i++)
        {
            if (ctx.GetChild(i) is CobolParserCore.StatementBlockContext imp)
                imperatives.Add(imp);
        }

        if (imperatives.Count == 0) return null;

        // Determine form by checking first token
        var firstToken = ctx.Start;
        bool startsWithNot = firstToken?.Type == CobolParserCore.NOT;

        var onSizeError = new List<BoundStatement>();
        var notOnSizeError = new List<BoundStatement>();

        if (startsWithNot)
        {
            // NOT ON SIZE ERROR only — all imperatives go to notOnSizeError
            foreach (var imp in imperatives)
                foreach (var stmt in imp.statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
        }
        else
        {
            // ON SIZE ERROR (+ optional NOT ON SIZE ERROR)
            // First imperative is for ON SIZE ERROR
            foreach (var stmt in imperatives[0].statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) onSizeError.Add(bound);
            }
            // Second imperative (if present) is for NOT ON SIZE ERROR
            if (imperatives.Count > 1)
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
        }

        if (onSizeError.Count == 0 && notOnSizeError.Count == 0)
            return null;

        return new BoundSizeErrorClause(onSizeError, notOnSizeError);
    }

    // ── VALIDATED ARITHMETIC ──

    /// <summary>
    /// Construct and validate an arithmetic statement, emitting diagnostics for type violations.
    /// </summary>
    internal BoundArithmeticStatement ValidatedArithmetic(
        ArithmeticKind kind,
        IReadOnlyList<BoundExpression> operands,
        BoundExpression? receiver,
        IReadOnlyList<BoundArithmeticTarget> targets,
        bool isGiving = false,
        bool isByForm = false,
        BoundIdentifierExpression? remainderTarget = null,
        BoundSizeErrorClause? sizeError = null,
        int line = 0)
    {
        var stmt = new BoundArithmeticStatement(kind, operands, receiver, targets,
            isGiving, isByForm, remainderTarget, sizeError);
        var loc = new SourceLocation("<source>", 0, line, 0);
        var span = TextSpan.Empty;
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, _ctx.Diagnostics, loc, span);
        return stmt;
    }

    // ── ARITHMETIC TARGETS ──

    internal List<BoundArithmeticTarget> BindArithmeticTargets(
        CobolParserCore.ReceivingArithmeticOperandContext[] operands)
    {
        var targets = new List<BoundArithmeticTarget>();
        foreach (var op in operands)
        {
            var sym = _ctx.Expression.BindDataReferenceWithSubscripts(op.dataReference());
            if (sym is BoundIdentifierExpression bound)
                targets.Add(new BoundArithmeticTarget(bound, op.ROUNDED() != null));
        }
        return targets;
    }
}
