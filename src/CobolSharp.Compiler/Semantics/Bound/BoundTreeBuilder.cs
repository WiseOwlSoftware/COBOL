// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound;

/// <summary>
/// Walks the ANTLR parse tree with resolved symbols (SemanticModel) and
/// produces a typed, symbol-resolved bound tree (BoundProgram).
/// </summary>
public sealed class BoundTreeBuilder : CobolParserCoreBaseVisitor<object?>
{
    private readonly SemanticModel _semantic;
    private readonly DiagnosticBag _diagnostics;
    private readonly List<BoundParagraph> _paragraphs = new();

    public BoundTreeBuilder(SemanticModel semantic, DiagnosticBag diagnostics)
    {
        _semantic = semantic;
        _diagnostics = diagnostics;
    }

    public BoundProgram Build(CobolParserCore.CompilationUnitContext tree)
    {
        Visit(tree);
        return new BoundProgram(_semantic.Program, _paragraphs);
    }

    public override object? VisitParagraphDeclaration(CobolParserCore.ParagraphDeclarationContext ctx)
    {
        var nameCtx = ctx.paragraphName();
        if (nameCtx == null) return null;

        string name = nameCtx.GetText();
        var paraSym = _semantic.ResolveParagraph(name);
        if (paraSym == null) return null;

        var statements = new List<BoundStatement>();
        foreach (var sentence in ctx.sentence())
        {
            foreach (var stmtCtx in sentence.statement())
            {
                var bound = BindStatement(stmtCtx);
                if (bound != null)
                    statements.Add(bound);
            }
        }

        _paragraphs.Add(new BoundParagraph(paraSym, statements));
        return null;
    }

    // ═══════════════════════════════════
    // Statement binding
    // ═══════════════════════════════════

    private BoundStatement? BindStatement(CobolParserCore.StatementContext ctx)
    {
        if (ctx.displayStatement() is { } disp) return BindDisplay(disp);
        if (ctx.moveStatement() is { } mv) return BindMove(mv);
        if (ctx.performStatement() is { } perf) return BindPerform(perf);
        if (ctx.writeStatement() is { } wr) return BindWrite(wr);
        if (ctx.ifStatement() is { } iff) return BindIf(iff);
        if (ctx.goToStatement() is { } gt) return BindGoTo(gt);
        if (ctx.stopStatement() is { }) return new BoundStopStatement();
        if (ctx.exitStatement() is { }) return new BoundExitStatement();
        if (ctx.openStatement() is { }) return new BoundOpenStatement();
        if (ctx.closeStatement() is { }) return new BoundCloseStatement();
        if (ctx.addStatement() is { } addCtx) return BindAdd(addCtx);
        if (ctx.subtractStatement() is { } sub) return BindSubtract(sub);
        if (ctx.multiplyStatement() is { } mult) return BindMultiply(mult);
        if (ctx.divideStatement() is { } div) return BindDivide(div);
        if (ctx.computeStatement() is { } comp) return BindCompute(comp);

        // Unrecognized statement — skip
        return null;
    }

    // ── DISPLAY ──

    private BoundDisplayStatement BindDisplay(CobolParserCore.DisplayStatementContext ctx)
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

            if (child is CobolParserCore.IdentifierContext idCtx)
            {
                var sym = _semantic.ResolveData(idCtx.GetText());
                if (sym != null)
                    operands.Add(new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric));
                else
                    operands.Add(new BoundLiteralExpression(idCtx.GetText(), CobolCategory.Alphanumeric));
            }
            else if (child is CobolParserCore.LiteralContext litCtx)
            {
                operands.Add(BindLiteral(litCtx));
            }
        }

        return new BoundDisplayStatement(operands);
    }

    // ── MOVE ──

    private BoundMoveStatement? BindMove(CobolParserCore.MoveStatementContext ctx)
    {
        var moveSource = ctx.moveSource();
        var moveTarget = ctx.moveTarget();
        if (moveSource == null || moveTarget == null) return null;

        var source = BindMoveSource(moveSource);

        var targets = new List<BoundExpression>();
        var idList = moveTarget.identifierList();
        if (idList != null)
        {
            foreach (var id in idList.identifier())
                targets.Add(BindIdentifier(id));
        }

        return new BoundMoveStatement(source, targets, isRounded: false);
    }

    private BoundExpression BindMoveSource(CobolParserCore.MoveSourceContext ctx)
    {
        // moveSource: arithmeticExpression | literal
        var litCtx = ctx.literal();
        if (litCtx != null) return BindLiteral(litCtx);

        // arithmeticExpression → eventually resolves to identifier or literal
        return BindArithmeticExpr(ctx.arithmeticExpression());
    }

    // ── PERFORM ──

    private BoundPerformStatement? BindPerform(CobolParserCore.PerformStatementContext ctx)
    {
        var target = ctx.performTarget();
        if (target == null) return null;

        var procNames = target.procedureName();
        if (procNames.Length == 0) return null;

        string name = procNames[0].GetText();
        var paraSym = _semantic.ResolveParagraph(name);
        if (paraSym == null) return null;

        ParagraphSymbol? thruSym = null;
        if (procNames.Length > 1)
        {
            string thruName = procNames[1].GetText();
            thruSym = _semantic.ResolveParagraph(thruName);
        }

        // Check for TIMES phrase
        int times = 0;
        var options = ctx.performOptions();
        if (options != null && options.Length > 0)
        {
            var timesOpt = options[0].performTimes();
            if (timesOpt != null)
            {
                var intLit = timesOpt.integerLiteral();
                if (intLit != null && int.TryParse(intLit.GetText(), out var t))
                    times = t;
            }
        }

        return new BoundPerformStatement(paraSym, thruSym, times);
    }

    // ── WRITE ──

    private BoundWriteStatement? BindWrite(CobolParserCore.WriteStatementContext ctx)
    {
        var recordCtx = ctx.recordName();
        if (recordCtx == null) return null;

        string recordName = recordCtx.GetText();
        var recordSym = _semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        // TODO: resolve file from record → FD relationship
        return new BoundWriteStatement(null, recordSym, null);
    }

    // ── MULTIPLY ──

    private BoundStatement BindMultiply(CobolParserCore.MultiplyStatementContext ctx)
    {
        // MULTIPLY operand BY target1 [ROUNDED] target2 [ROUNDED] ...
        var operand = BindSimpleOperand(ctx.multiplyOperand());

        // BY targets — each with per-item ROUNDED flag
        var byTargets = ctx.multiplyByTarget();
        if (byTargets.Length == 0)
            return new BoundArithmeticStatement(BoundNodeKind.MultiplyStatement);

        var targets = new List<BoundArithmeticTarget>();
        foreach (var bt in byTargets)
        {
            var sym = _semantic.ResolveData(bt.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, bt.ROUNDED() != null));
        }

        if (targets.Count == 0)
            return new BoundArithmeticStatement(BoundNodeKind.MultiplyStatement);

        // GIVING targets (if present)
        DataSymbol? givingTarget = null;
        var givingCtx = ctx.multiplyGivingPhrase();
        if (givingCtx != null)
        {
            var givingTargets = givingCtx.multiplyByTarget();
            if (givingTargets.Length > 0)
            {
                var givingSym = _semantic.ResolveData(givingTargets[0].identifier().GetText());
                if (givingSym != null)
                {
                    givingTarget = givingSym;
                    // Replace targets with GIVING targets (GIVING overrides BY targets as destinations)
                    targets.Clear();
                    foreach (var gt in givingTargets)
                    {
                        var sym = _semantic.ResolveData(gt.identifier().GetText());
                        if (sym != null)
                            targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                    }
                }
            }
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR
        var onSizeError = new List<BoundStatement>();
        var notOnSizeError = new List<BoundStatement>();
        var sizeCtx = ctx.multiplyOnSizeError();
        if (sizeCtx != null)
        {
            var imperatives = sizeCtx.imperativeStatement();
            if (imperatives.Length > 0)
            {
                foreach (var stmt in imperatives[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onSizeError.Add(bound);
                }
            }
            if (imperatives.Length > 1)
            {
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
            }
        }

        return new BoundMultiplyStatement(operand, targets, givingTarget, onSizeError, notOnSizeError);
    }

    // ── ADD ──

    private BoundStatement BindAdd(CobolParserCore.AddStatementContext ctx)
    {
        // ADD operand(s) TO target1 [ROUNDED] target2 [ROUNDED] ...
        var operandList = ctx.addOperandList();
        if (operandList == null)
            return new BoundArithmeticStatement(BoundNodeKind.AddStatement);

        var addOps = operandList.addOperand();
        if (addOps.Length == 0)
            return new BoundArithmeticStatement(BoundNodeKind.AddStatement);

        // Bind all operands
        var operands = new List<BoundExpression>();
        foreach (var op in addOps)
            operands.Add(BindSimpleOperand(op));

        // TO targets (each with per-target ROUNDED)
        var toPhrase = ctx.addToPhrase();
        if (toPhrase == null)
            return new BoundArithmeticStatement(BoundNodeKind.AddStatement);

        var toTargetCtxs = toPhrase.addTarget();
        if (toTargetCtxs.Length == 0)
            return new BoundArithmeticStatement(BoundNodeKind.AddStatement);

        var targets = new List<BoundArithmeticTarget>();
        foreach (var t in toTargetCtxs)
        {
            var sym = _semantic.ResolveData(t.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, t.ROUNDED() != null));
        }

        if (targets.Count == 0)
            return new BoundArithmeticStatement(BoundNodeKind.AddStatement);

        // GIVING phrase overrides TO targets
        var givingPhrase = ctx.addGivingPhrase();
        if (givingPhrase != null)
        {
            var givingTargetCtxs = givingPhrase.addTarget();
            if (givingTargetCtxs.Length > 0)
            {
                targets.Clear();
                foreach (var gt in givingTargetCtxs)
                {
                    var sym = _semantic.ResolveData(gt.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                }
            }
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR
        var onSizeError = new List<BoundStatement>();
        var notOnSizeError = new List<BoundStatement>();
        var sizeCtx = ctx.addOnSizeError();
        if (sizeCtx != null)
        {
            var imperatives = sizeCtx.imperativeStatement();
            if (imperatives.Length > 0)
            {
                foreach (var stmt in imperatives[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onSizeError.Add(bound);
                }
            }
            if (imperatives.Length > 1)
            {
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
            }
        }

        return new BoundAddStatement(operands, targets, onSizeError, notOnSizeError);
    }

    // ── SUBTRACT ──

    private BoundStatement BindSubtract(CobolParserCore.SubtractStatementContext ctx)
    {
        // SUBTRACT operand(s) FROM target1 [ROUNDED] target2 [ROUNDED] ...
        // Multiple operands: SUBTRACT A B C FROM T → T = T - (A + B + C)
        var operandList = ctx.subtractOperandList();
        if (operandList == null)
            return new BoundArithmeticStatement(BoundNodeKind.SubtractStatement);

        var subOperands = operandList.subtractOperand();
        if (subOperands.Length == 0)
            return new BoundArithmeticStatement(BoundNodeKind.SubtractStatement);

        // Bind all operands (simple identifiers or literals)
        var operands = new List<BoundExpression>();
        foreach (var op in subOperands)
            operands.Add(BindSimpleOperand(op));

        // FROM targets (each with per-target ROUNDED)
        var fromPhrase = ctx.subtractFromPhrase();
        if (fromPhrase == null)
            return new BoundArithmeticStatement(BoundNodeKind.SubtractStatement);

        var fromTargetCtxs = fromPhrase.subtractTarget();
        if (fromTargetCtxs.Length == 0)
            return new BoundArithmeticStatement(BoundNodeKind.SubtractStatement);

        var targets = new List<BoundArithmeticTarget>();
        foreach (var t in fromTargetCtxs)
        {
            var sym = _semantic.ResolveData(t.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, t.ROUNDED() != null));
        }

        if (targets.Count == 0)
            return new BoundArithmeticStatement(BoundNodeKind.SubtractStatement);

        // GIVING phrase: SUBTRACT a FROM b GIVING c [ROUNDED] → c = b - a
        var givingPhrase = ctx.subtractGivingPhrase();
        if (givingPhrase != null)
        {
            var givingTargetCtxs = givingPhrase.subtractTarget();
            if (givingTargetCtxs.Length > 0)
            {
                targets.Clear();
                foreach (var gt in givingTargetCtxs)
                {
                    var sym = _semantic.ResolveData(gt.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                }
            }
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR
        var onSizeError = new List<BoundStatement>();
        var notOnSizeError = new List<BoundStatement>();
        var sizeCtx = ctx.subtractOnSizeError();
        if (sizeCtx != null)
        {
            var imperatives = sizeCtx.imperativeStatement();
            if (imperatives.Length > 0)
            {
                foreach (var stmt in imperatives[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onSizeError.Add(bound);
                }
            }
            if (imperatives.Length > 1)
            {
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
            }
        }

        return new BoundSubtractStatement(operands, targets, onSizeError, notOnSizeError);
    }

    // ── DIVIDE ──

    private BoundStatement BindDivide(CobolParserCore.DivideStatementContext ctx)
    {
        // DIVIDE operand INTO/BY ...
        var operandCtx = ctx.divideOperand();
        if (operandCtx == null)
            return new BoundArithmeticStatement(BoundNodeKind.DivideStatement);

        var firstOperand = BindSimpleOperand(operandCtx);
        bool isByForm = ctx.divideByPhrase() != null;
        BoundExpression? dividend = null;

        // Determine targets based on INTO vs BY form
        var targets = new List<BoundArithmeticTarget>();

        if (isByForm)
        {
            // DIVIDE a BY b GIVING c → divisor=b, dividend=a, target=c
            // The BY phrase contains the second operand
            var byOperand = ctx.divideByPhrase().divideOperand();
            dividend = firstOperand; // a is the dividend
            firstOperand = BindSimpleOperand(byOperand); // b is the divisor

            // GIVING targets
            var givingPhrase = ctx.divideGivingPhrase();
            if (givingPhrase != null)
            {
                foreach (var gt in givingPhrase.divideTarget())
                {
                    var sym = _semantic.ResolveData(gt.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                }
            }
        }
        else
        {
            // DIVIDE a INTO b → divisor=a, target=b (b = b / a)
            var intoPhrase = ctx.divideIntoPhrase();
            if (intoPhrase != null)
            {
                foreach (var it in intoPhrase.divideTarget())
                {
                    var sym = _semantic.ResolveData(it.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, it.ROUNDED() != null));
                }
            }

            // GIVING overrides INTO targets as destinations
            var givingPhrase = ctx.divideGivingPhrase();
            if (givingPhrase != null)
            {
                // INTO target becomes the dividend, GIVING targets are destinations
                if (targets.Count > 0)
                    dividend = new BoundIdentifierExpression(targets[0].Symbol, CobolCategory.Numeric);
                targets.Clear();
                foreach (var gt in givingPhrase.divideTarget())
                {
                    var sym = _semantic.ResolveData(gt.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                }
            }
        }

        if (targets.Count == 0)
            return new BoundArithmeticStatement(BoundNodeKind.DivideStatement);

        // REMAINDER
        DataSymbol? remainderTarget = null;
        var remPhrase = ctx.divideRemainderPhrase();
        if (remPhrase != null)
        {
            remainderTarget = _semantic.ResolveData(remPhrase.identifier().GetText());
        }

        // ON SIZE ERROR / NOT ON SIZE ERROR
        var onSizeError = new List<BoundStatement>();
        var notOnSizeError = new List<BoundStatement>();
        var sizeCtx = ctx.divideOnSizeError();
        if (sizeCtx != null)
        {
            var imperatives = sizeCtx.imperativeStatement();
            if (imperatives.Length > 0)
            {
                foreach (var stmt in imperatives[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onSizeError.Add(bound);
                }
            }
            if (imperatives.Length > 1)
            {
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
            }
        }

        return new BoundDivideStatement(firstOperand, dividend, isByForm, targets,
            remainderTarget, onSizeError, notOnSizeError);
    }

    // ── COMPUTE ──

    private BoundStatement BindCompute(CobolParserCore.ComputeStatementContext ctx)
    {
        // COMPUTE target1 [ROUNDED] target2 [ROUNDED] = expression
        var storeCtxs = ctx.computeStore();
        if (storeCtxs.Length == 0)
            return new BoundArithmeticStatement(BoundNodeKind.ComputeStatement);

        var targets = new List<BoundArithmeticTarget>();
        foreach (var s in storeCtxs)
        {
            var sym = _semantic.ResolveData(s.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, s.ROUNDED() != null));
        }

        if (targets.Count == 0)
            return new BoundArithmeticStatement(BoundNodeKind.ComputeStatement);

        // Bind the full arithmetic expression (recursive tree walk)
        var expr = BindFullExpression(ctx.arithmeticExpression());

        // ON SIZE ERROR / NOT ON SIZE ERROR
        var onSizeError = new List<BoundStatement>();
        var notOnSizeError = new List<BoundStatement>();
        var sizeCtx = ctx.computeOnSizeError();
        if (sizeCtx != null)
        {
            var imperatives = sizeCtx.imperativeStatement();
            if (imperatives.Length > 0)
            {
                foreach (var stmt in imperatives[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onSizeError.Add(bound);
                }
            }
            if (imperatives.Length > 1)
            {
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
            }
        }

        return new BoundComputeStatement(expr, targets, onSizeError, notOnSizeError);
    }

    /// <summary>
    /// Recursively bind an arithmetic expression tree for COMPUTE.
    /// Walks the parse tree: additiveExpression → multiplicativeExpression →
    /// powerExpression → unaryExpression → primaryExpression.
    /// </summary>
    private BoundExpression BindFullExpression(CobolParserCore.ArithmeticExpressionContext ctx)
    {
        return BindAdditiveExpression(ctx.additiveExpression());
    }

    private BoundExpression BindAdditiveExpression(CobolParserCore.AdditiveExpressionContext ctx)
    {
        var terms = ctx.multiplicativeExpression();
        var ops = ctx.addOp();

        var left = BindMultiplicativeExpression(terms[0]);
        for (int i = 0; i < ops.Length; i++)
        {
            var right = BindMultiplicativeExpression(terms[i + 1]);
            var opKind = ops[i].GetText() == "+"
                ? BoundBinaryOperatorKind.Add
                : BoundBinaryOperatorKind.Subtract;
            left = new BoundBinaryExpression(left, opKind, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression BindMultiplicativeExpression(CobolParserCore.MultiplicativeExpressionContext ctx)
    {
        var factors = ctx.powerExpression();
        var ops = ctx.mulOp();

        var left = BindPowerExpression(factors[0]);
        for (int i = 0; i < ops.Length; i++)
        {
            var right = BindPowerExpression(factors[i + 1]);
            var opKind = ops[i].GetText() == "*"
                ? BoundBinaryOperatorKind.Multiply
                : BoundBinaryOperatorKind.Divide;
            left = new BoundBinaryExpression(left, opKind, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression BindPowerExpression(CobolParserCore.PowerExpressionContext ctx)
    {
        var unaries = ctx.unaryExpression();
        var left = BindUnaryExpression(unaries[0]);
        if (unaries.Length > 1)
        {
            // a ** b
            var right = BindUnaryExpression(unaries[1]);
            // Power is not a standard BoundBinaryOperatorKind; use Multiply as placeholder
            // and handle at emit time. For now, use a dedicated representation.
            // Simple approach: emit as Math.Pow at runtime
            left = new BoundBinaryExpression(left,
                (BoundBinaryOperatorKind)99, // Power — extend enum
                right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression BindUnaryExpression(CobolParserCore.UnaryExpressionContext ctx)
    {
        var addOp = ctx.addOp();
        if (addOp != null)
        {
            var inner = BindUnaryExpression(ctx.unaryExpression());
            if (addOp.GetText() == "-")
            {
                // Negate: 0 - inner
                return new BoundBinaryExpression(
                    new BoundLiteralExpression(0m, CobolCategory.Numeric),
                    BoundBinaryOperatorKind.Subtract,
                    inner, CobolCategory.Numeric);
            }
            return inner; // unary + is identity
        }
        return BindPrimaryExpression(ctx.primaryExpression());
    }

    private BoundExpression BindPrimaryExpression(CobolParserCore.PrimaryExpressionContext ctx)
    {
        if (ctx.numericLiteral() != null)
            return BindNumericLiteral(ctx.numericLiteral());

        if (ctx.identifier() != null)
        {
            string name = ctx.identifier().GetText();
            var sym = _semantic.ResolveData(name);
            if (sym != null)
                return new BoundIdentifierExpression(sym, CobolCategory.Numeric);
            // Try as numeric literal (e.g., 100)
            if (decimal.TryParse(name, System.Globalization.CultureInfo.InvariantCulture, out var val))
                return new BoundLiteralExpression(val, CobolCategory.Numeric);
            return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);
        }

        if (ctx.arithmeticExpression() != null)
            return BindFullExpression(ctx.arithmeticExpression());

        // functionCall — bind as identifier for now
        if (ctx.functionCall() != null)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── IF ──

    private BoundIfStatement? BindIf(CobolParserCore.IfStatementContext ctx)
    {
        var condCtx = ctx.condition();
        if (condCtx == null) return null;

        // Try to bind a real condition
        var condition = BindCondition(condCtx);

        var thenStmts = new List<BoundStatement>();
        var elseStmts = new List<BoundStatement>();

        // Walk imperativeStatement* children
        var impStmts = ctx.imperativeStatement();
        if (impStmts.Length > 0)
        {
            foreach (var stmt in impStmts[0].statement())
            {
                var bound = BindStatement(stmt);
                if (bound != null) thenStmts.Add(bound);
            }
        }

        // ELSE block
        if (impStmts.Length > 1)
        {
            foreach (var stmt in impStmts[1].statement())
            {
                var bound = BindStatement(stmt);
                if (bound != null) elseStmts.Add(bound);
            }
        }

        return new BoundIfStatement(condition, thenStmts, elseStmts.Count > 0 ? elseStmts : null);
    }

    // ── GO TO ──

    private BoundGoToStatement? BindGoTo(CobolParserCore.GoToStatementContext ctx)
    {
        var idCtx = ctx.identifier();
        if (idCtx == null) return null;

        string name = idCtx.GetText();
        var paraSym = _semantic.ResolveParagraph(name);
        if (paraSym == null) return null;

        return new BoundGoToStatement(paraSym);
    }

    // ═══════════════════════════════════
    // Expression binding
    // ═══════════════════════════════════

    private BoundExpression BindLiteral(CobolParserCore.LiteralContext lit)
    {
        // literal: numericLiteral | nonNumericLiteral
        var numLit = lit.numericLiteral();
        if (numLit != null)
            return BindNumericLiteral(numLit);

        var nonNumLit = lit.nonNumericLiteral();
        if (nonNumLit != null)
            return BindNonNumericLiteral(nonNumLit);

        // Fallback
        return new BoundLiteralExpression(lit.GetText(), CobolCategory.Alphanumeric);
    }

    private BoundExpression BindNumericLiteral(CobolParserCore.NumericLiteralContext numLit)
    {
        var raw = numLit.GetText();
        if (decimal.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric);
        return new BoundLiteralExpression(raw, CobolCategory.Alphanumeric);
    }

    private BoundExpression BindNonNumericLiteral(CobolParserCore.NonNumericLiteralContext nonNum)
    {
        var s = nonNum.STRINGLIT();
        if (s != null)
        {
            var text = s.GetText();
            if (text.Length >= 2 &&
                ((text[0] == '"' && text[^1] == '"') ||
                 (text[0] == '\'' && text[^1] == '\'')))
                text = text[1..^1];
            return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
        }

        var figCtx = nonNum.figurativeConstant();
        if (figCtx != null)
        {
            string figText = figCtx.GetText().ToUpperInvariant();
            return figText switch
            {
                "SPACE" or "SPACES" =>
                    new BoundLiteralExpression(" ", CobolCategory.Alphanumeric),
                "ZERO" or "ZEROS" or "ZEROES" =>
                    new BoundLiteralExpression(0m, CobolCategory.Numeric),
                "HIGH-VALUE" or "HIGH-VALUES" =>
                    new BoundLiteralExpression("\xFF", CobolCategory.Alphanumeric),
                "LOW-VALUE" or "LOW-VALUES" =>
                    new BoundLiteralExpression("\x00", CobolCategory.Alphanumeric),
                "QUOTE" or "QUOTES" =>
                    new BoundLiteralExpression("\"", CobolCategory.Alphanumeric),
                _ => new BoundLiteralExpression(figText, CobolCategory.Alphanumeric)
            };
        }

        // HEXLIT, etc.
        return new BoundLiteralExpression(nonNum.GetText(), CobolCategory.Alphanumeric);
    }

    private BoundExpression BindIdentifier(CobolParserCore.IdentifierContext idCtx)
    {
        string name = idCtx.GetText();
        var sym = _semantic.ResolveData(name);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric);

        // Unresolved — treat as string literal for now
        return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);
    }

    /// <summary>
    /// Bind a condition expression. Handles simple relational: left op right.
    /// </summary>
    /// <summary>
    /// Bind a full condition expression with AND/OR/NOT and relational operators.
    /// condition → logicalOrExpression → logicalAndExpression → logicalNotExpression → relationalExpression
    /// </summary>
    private BoundExpression BindCondition(CobolParserCore.ConditionContext ctx)
    {
        var orExpr = ctx.logicalOrExpression();
        if (orExpr == null)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);
        return BindLogicalOr(orExpr);
    }

    private BoundExpression BindLogicalOr(CobolParserCore.LogicalOrExpressionContext ctx)
    {
        var andExprs = ctx.logicalAndExpression();
        var result = BindLogicalAnd(andExprs[0]);
        for (int i = 1; i < andExprs.Length; i++)
        {
            var right = BindLogicalAnd(andExprs[i]);
            // OR is represented as a binary expression; the binder/emitter knows
            // operator kinds > GreaterOrEqual are logical operators
            result = new BoundBinaryExpression(result,
                (BoundBinaryOperatorKind)20, // OR
                right, CobolCategory.Unknown);
        }
        return result;
    }

    private BoundExpression BindLogicalAnd(CobolParserCore.LogicalAndExpressionContext ctx)
    {
        var notExprs = ctx.logicalNotExpression();
        var result = BindLogicalNot(notExprs[0]);
        for (int i = 1; i < notExprs.Length; i++)
        {
            var right = BindLogicalNot(notExprs[i]);
            result = new BoundBinaryExpression(result,
                (BoundBinaryOperatorKind)21, // AND
                right, CobolCategory.Unknown);
        }
        return result;
    }

    private BoundExpression BindLogicalNot(CobolParserCore.LogicalNotExpressionContext ctx)
    {
        if (ctx.logicalNotExpression() != null)
        {
            // NOT condition → negate
            var inner = BindLogicalNot(ctx.logicalNotExpression());
            return new BoundBinaryExpression(
                inner,
                (BoundBinaryOperatorKind)22, // NOT (unary, right is dummy)
                new BoundLiteralExpression(0m, CobolCategory.Unknown),
                CobolCategory.Unknown);
        }
        return BindRelational(ctx.relationalExpression());
    }

    private BoundExpression BindRelational(CobolParserCore.RelationalExpressionContext ctx)
    {
        var operands = ctx.relationalOperand();
        var relOp = ctx.relationalOperator();

        if (operands.Length == 0)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);

        var left = BindRelationalOperand(operands[0]);

        if (operands.Length < 2 || relOp == null)
        {
            // Bare expression: IF A (means A <> 0 for numeric, A <> SPACE for alpha)
            return left;
        }

        var right = BindRelationalOperand(operands[1]);

        string opText = relOp.GetText().ToUpperInvariant()
            .Replace("IS", "").Replace("TO", "").Replace("THAN", "").Trim();
        var op = opText switch
        {
            "=" or "EQUAL" => BoundBinaryOperatorKind.Equal,
            "NOT=" or "NOTEQUAL" => BoundBinaryOperatorKind.NotEqual,
            ">" or "GREATER" => BoundBinaryOperatorKind.Greater,
            ">=" or "GREATEROREQUAL" => BoundBinaryOperatorKind.GreaterOrEqual,
            "<" or "LESS" => BoundBinaryOperatorKind.Less,
            "<=" or "LESSOREQUAL" => BoundBinaryOperatorKind.LessOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("GREATER") => BoundBinaryOperatorKind.LessOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("LESS") => BoundBinaryOperatorKind.GreaterOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("EQUAL") => BoundBinaryOperatorKind.NotEqual,
            _ when opText.Contains("EQUAL") => BoundBinaryOperatorKind.Equal,
            _ when opText.Contains("GREATER") => BoundBinaryOperatorKind.Greater,
            _ when opText.Contains("LESS") => BoundBinaryOperatorKind.Less,
            _ => BoundBinaryOperatorKind.Equal
        };

        return new BoundBinaryExpression(left, op, right, CobolCategory.Unknown);
    }

    private BoundExpression BindRelationalOperand(CobolParserCore.RelationalOperandContext ctx)
    {
        // relationalOperand: arithmeticExpression | nonNumericLiteral
        var nonNumLit = ctx.nonNumericLiteral();
        if (nonNumLit != null)
            return BindNonNumericLiteral(nonNumLit);

        // Use the recursive expression binder for full expression support
        var arithExpr = ctx.arithmeticExpression();
        if (arithExpr != null)
            return BindFullExpression(arithExpr);

        return BindArithmeticExpr(ctx.arithmeticExpression());
    }

    /// <summary>
    /// Bind a simple operand (identifier or literal) from ADD/SUBTRACT/MULTIPLY/DIVIDE.
    /// These statements accept only simple operands, not full expressions.
    /// </summary>
    private BoundExpression BindSimpleOperand(Antlr4.Runtime.ParserRuleContext ctx)
    {
        // The rule is: identifier | literal
        // Check for identifier child first
        if (ctx is CobolParserCore.AddOperandContext addOp)
        {
            if (addOp.identifier() != null)
                return BindIdentifierOrLiteral(addOp.identifier().GetText());
            if (addOp.literal() != null)
                return BindLiteral(addOp.literal());
        }
        else if (ctx is CobolParserCore.SubtractOperandContext subOp)
        {
            if (subOp.identifier() != null)
                return BindIdentifierOrLiteral(subOp.identifier().GetText());
            if (subOp.literal() != null)
                return BindLiteral(subOp.literal());
        }
        else if (ctx is CobolParserCore.MultiplyOperandContext mulOp)
        {
            if (mulOp.identifier() != null)
                return BindIdentifierOrLiteral(mulOp.identifier().GetText());
            if (mulOp.literal() != null)
                return BindLiteral(mulOp.literal());
        }
        else if (ctx is CobolParserCore.DivideOperandContext divOp)
        {
            if (divOp.identifier() != null)
                return BindIdentifierOrLiteral(divOp.identifier().GetText());
            if (divOp.literal() != null)
                return BindLiteral(divOp.literal());
        }

        // Fallback: try to parse the text
        string text = ctx.GetText();
        return BindIdentifierOrLiteral(text);
    }

    private BoundExpression BindIdentifierOrLiteral(string text)
    {
        if (decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric);

        var sym = _semantic.ResolveData(text);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric);

        return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
    }

    private BoundExpression BindArithmeticExpr(CobolParserCore.ArithmeticExpressionContext? ctx)
    {
        if (ctx == null)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        // primaryExpression now only allows numericLiteral | identifier | functionCall
        string text = ctx.GetText();
        if (decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric);

        var sym = _semantic.ResolveData(text);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric);

        return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
    }
}
