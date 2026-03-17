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

        var sentences = new List<BoundSentence>();
        foreach (var sentenceCtx in ctx.sentence())
        {
            var statements = new List<BoundStatement>();
            foreach (var stmtCtx in sentenceCtx.statement())
            {
                var bound = BindStatement(stmtCtx);
                if (bound != null)
                    statements.Add(bound);
            }
            if (statements.Count > 0)
                sentences.Add(new BoundSentence(statements));
        }

        _paragraphs.Add(new BoundParagraph(paraSym, sentences));
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
        if (ctx.gobackStatement() is { }) return new BoundStopStatement();
        if (ctx.exitStatement() is { }) return new BoundExitStatement();
        if (ctx.nextSentenceStatement() is { }) return new BoundNextSentenceStatement();
        if (ctx.openStatement() is { } openCtx) return BindOpen(openCtx);
        if (ctx.closeStatement() is { } closeCtx) return BindClose(closeCtx);
        if (ctx.readStatement() is { } readCtx) return BindRead(readCtx);
        if (ctx.addStatement() is { } addCtx) return BindAdd(addCtx);
        if (ctx.subtractStatement() is { } sub) return BindSubtract(sub);
        if (ctx.multiplyStatement() is { } mult) return BindMultiply(mult);
        if (ctx.divideStatement() is { } div) return BindDivide(div);
        if (ctx.computeStatement() is { } comp) return BindCompute(comp);
        if (ctx.evaluateStatement() is { } evalStmt) return BindEvaluate(evalStmt);

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
        var options = ctx.performOptions();

        // Inline form: PERFORM options+ imperativeStatement* END-PERFORM (no target)
        if (target == null)
        {
            BoundExpression? untilCond = null;
            BoundPerformVarying? varying = null;

            if (options != null)
            {
                foreach (var opt in options)
                {
                    if (opt.performUntil() is { } untilCtx)
                        untilCond = BindCondition(untilCtx.condition());
                    if (opt.performVarying() is { } varyCtx)
                        varying = BindPerformVaryingOption(varyCtx);
                }
            }

            // Bind inline statements
            var inlineStmts = new List<BoundStatement>();
            var impStmts = ctx.imperativeStatement();
            if (impStmts != null)
            {
                foreach (var imp in impStmts)
                    foreach (var stmt in imp.statement())
                    {
                        var bound = BindStatement(stmt);
                        if (bound != null) inlineStmts.Add(bound);
                    }
            }

            // Use the UNTIL condition from VARYING if present
            if (varying != null)
                untilCond = varying.UntilCondition;

            return new BoundPerformStatement(null, null, 0, untilCond, varying, inlineStmts);
        }

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

        // Check for TIMES, UNTIL, VARYING phrases
        int times = 0;
        BoundExpression? untilCondition = null;
        BoundPerformVarying? varyingOpt = null;

        if (options != null && options.Length > 0)
        {
            foreach (var opt in options)
            {
                if (opt.performTimes() is { } timesOpt)
                {
                    var intLit = timesOpt.integerLiteral();
                    if (intLit != null && int.TryParse(intLit.GetText(), out var t))
                        times = t;
                }
                if (opt.performUntil() is { } untilCtx)
                    untilCondition = BindCondition(untilCtx.condition());
                if (opt.performVarying() is { } varyCtx)
                    varyingOpt = BindPerformVaryingOption(varyCtx);
            }
        }

        // Use the UNTIL condition from VARYING if present
        if (varyingOpt != null)
            untilCondition = varyingOpt.UntilCondition;

        return new BoundPerformStatement(paraSym, thruSym, times, untilCondition, varyingOpt);
    }

    private BoundPerformVarying BindPerformVaryingOption(CobolParserCore.PerformVaryingContext ctx)
    {
        // Build innermost AFTER clauses first, then chain outward
        BoundPerformVarying? inner = null;
        var afterClauses = ctx.performVaryingAfter();
        if (afterClauses != null)
        {
            // Process AFTER clauses in reverse to build inside-out chain
            for (int i = afterClauses.Length - 1; i >= 0; i--)
            {
                var afterCtx = afterClauses[i];
                string afterId = afterCtx.identifier().GetText();
                var afterSym = _semantic.ResolveData(afterId)!;
                var afterExprs = afterCtx.arithmeticExpression();
                var afterInit = BindFullExpression(afterExprs[0]);
                var afterStep = BindFullExpression(afterExprs[1]);
                var afterUntil = BindCondition(afterCtx.condition());
                inner = new BoundPerformVarying(afterSym, afterInit, afterStep, afterUntil, inner);
            }
        }

        // Outer VARYING
        string idName = ctx.identifier().GetText();
        var indexSym = _semantic.ResolveData(idName)!;
        var arithExprs = ctx.arithmeticExpression();
        var initial = BindFullExpression(arithExprs[0]);  // FROM
        var step = BindFullExpression(arithExprs[1]);      // BY
        var untilCond = BindCondition(ctx.condition());

        return new BoundPerformVarying(indexSym, initial, step, untilCond, inner);
    }

    // ── EVALUATE ──

    private BoundEvaluateStatement BindEvaluate(CobolParserCore.EvaluateStatementContext ctx)
    {
        // Bind subjects — detect EVALUATE TRUE
        var subjects = new List<BoundExpression>();
        bool isEvaluateTrue = false;

        foreach (var subCtx in ctx.evaluateSubject())
        {
            if (subCtx.TRUE_() != null)
            {
                isEvaluateTrue = true;
                continue;
            }
            if (subCtx.arithmeticExpression() is { } arithCtx)
                subjects.Add(BindFullExpression(arithCtx));
        }

        int subjectCount = isEvaluateTrue ? 1 : subjects.Count;

        // Bind WHEN clauses
        var whens = new List<BoundEvaluateWhen>();
        List<BoundStatement>? whenOther = null;

        foreach (var whenClause in ctx.evaluateWhenClause())
        {
            // WHEN OTHER
            if (whenClause.OTHER() != null)
            {
                var otherStmts = new List<BoundStatement>();
                foreach (var imp in whenClause.imperativeStatement())
                    foreach (var stmt in imp.statement())
                    {
                        var bound = BindStatement(stmt);
                        if (bound != null) otherStmts.Add(bound);
                    }
                whenOther = otherStmts;
                continue;
            }

            // Normal WHEN: bind per-subject groups (separated by ALSO)
            var groups = whenClause.evaluateWhenGroup();
            var subjectConditions = new List<BoundEvaluateCondition>();

            for (int i = 0; i < subjectCount && i < groups.Length; i++)
            {
                subjectConditions.Add(BindEvaluateWhenGroup(groups[i], isEvaluateTrue));
            }
            // If fewer groups than subjects → semantic error; fill with "never match"
            // so the WHEN clause doesn't fire (missing subjects are non-matching)
            for (int i = groups.Length; i < subjectCount; i++)
                subjectConditions.Add(new BoundEvaluateValueCondition(
                    Array.Empty<BoundExpression>(), Array.Empty<BoundEvaluateRange>(), isAny: false));

            var stmts = new List<BoundStatement>();
            foreach (var imp in whenClause.imperativeStatement())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) stmts.Add(bound);
                }

            whens.Add(new BoundEvaluateWhen(subjectConditions, stmts));
        }

        return new BoundEvaluateStatement(subjects, whens, whenOther);
    }

    private BoundEvaluateCondition BindEvaluateWhenGroup(
        CobolParserCore.EvaluateWhenGroupContext groupCtx, bool isEvaluateTrue)
    {
        var items = groupCtx.evaluateWhenItem();

        if (isEvaluateTrue)
        {
            // For EVALUATE TRUE, the WHEN item is a condition
            if (items.Length > 0 && items[0].condition() is { } condCtx)
                return new BoundEvaluateConditionWhen(BindCondition(condCtx));
            // Fallback: try as arithmetic expression (bare identifier → condition name)
            if (items.Length > 0 && items[0].arithmeticExpression().Length > 0)
            {
                var expr = BindFullExpression(items[0].arithmeticExpression(0));
                // Check if the result resolves to a condition name
                expr = TryResolveConditionName(expr);
                return new BoundEvaluateConditionWhen(expr);
            }
            return new BoundEvaluateValueCondition(
                Array.Empty<BoundExpression>(), Array.Empty<BoundEvaluateRange>(), isAny: true);
        }

        // Normal EVALUATE: bind values and ranges
        var values = new List<BoundExpression>();
        var ranges = new List<BoundEvaluateRange>();
        bool isAny = false;

        foreach (var item in items)
        {
            if (item.ANY() != null)
            {
                isAny = true;
                continue;
            }

            var arithExprs = item.arithmeticExpression();
            if (arithExprs.Length == 2)
            {
                // Range: value THRU value
                var from = BindFullExpression(arithExprs[0]);
                var to = BindFullExpression(arithExprs[1]);
                ranges.Add(new BoundEvaluateRange(from, to));
            }
            else if (arithExprs.Length == 1)
            {
                values.Add(BindFullExpression(arithExprs[0]));
            }
            else if (item.condition() is { } condCtx)
            {
                values.Add(BindCondition(condCtx));
            }
        }

        return new BoundEvaluateValueCondition(values, ranges, isAny);
    }

    // ── WRITE ──

    private BoundWriteStatement? BindWrite(CobolParserCore.WriteStatementContext ctx)
    {
        var recordCtx = ctx.recordName();
        if (recordCtx == null) return null;

        string recordName = recordCtx.GetText();
        var recordSym = _semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        // Resolve file from record → FD relationship
        var fileSym = _semantic.ResolveFileForRecord(recordSym);
        return new BoundWriteStatement(fileSym, recordSym, null);
    }

    // ── OPEN ──

    private BoundStatement BindOpen(CobolParserCore.OpenStatementContext ctx)
    {
        var results = new List<BoundStatement>();
        foreach (var clause in ctx.openClause())
        {
            var modeCtx = clause.openMode();
            var mode = modeCtx.GetText().ToUpperInvariant() switch
            {
                "INPUT" => OpenMode.Input,
                "OUTPUT" => OpenMode.Output,
                "EXTEND" => OpenMode.Extend,
                _ => OpenMode.Output
            };

            var files = new List<FileSymbol>();
            foreach (var idCtx in clause.identifier())
            {
                string name = idCtx.GetText();
                var fileSym = _semantic.ResolveFile(name);
                if (fileSym != null)
                    files.Add(fileSym);
            }

            if (files.Count > 0)
                results.Add(new BoundOpenStatement(mode, files));
        }

        if (results.Count == 1) return results[0];
        return results.Count > 0 ? results[0]
            : new BoundOpenStatement(OpenMode.Output, Array.Empty<FileSymbol>());
    }

    // ── CLOSE ──

    private BoundStatement BindClose(CobolParserCore.CloseStatementContext ctx)
    {
        var idListCtx = ctx.identifierList();
        var files = new List<FileSymbol>();
        if (idListCtx != null)
        {
            foreach (var idCtx in idListCtx.identifier())
            {
                string name = idCtx.GetText();
                var fileSym = _semantic.ResolveFile(name);
                if (fileSym != null)
                    files.Add(fileSym);
            }
        }
        return new BoundCloseStatement(files);
    }

    // ── READ ──

    private BoundStatement? BindRead(CobolParserCore.ReadStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        string name = fileNameCtx.GetText();
        var fileSym = _semantic.ResolveFile(name);
        if (fileSym == null) return null;

        // INTO clause
        DataSymbol? intoSym = null;
        var intoCtx = ctx.readInto();
        if (intoCtx != null)
        {
            string intoName = intoCtx.identifier().GetText();
            intoSym = _semantic.ResolveData(intoName);
        }

        // AT END / NOT AT END
        var atEnd = new List<BoundStatement>();
        var notAtEnd = new List<BoundStatement>();
        var atEndCtx = ctx.readAtEnd();
        if (atEndCtx != null)
        {
            var impStmts = atEndCtx.imperativeStatement();
            if (impStmts.Length >= 1)
            {
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
            }
            if (impStmts.Length >= 2)
            {
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notAtEnd.Add(bound);
                }
            }
        }

        return new BoundReadStatement(fileSym, intoSym, atEnd, notAtEnd);
    }

    // ── MULTIPLY ──

    private BoundStatement BindMultiply(CobolParserCore.MultiplyStatementContext ctx)
    {
        // MULTIPLY operand BY target1 [ROUNDED] target2 [ROUNDED] ...
        var operand = BindSimpleOperand(ctx.multiplyOperand());

        // BY targets — each with per-item ROUNDED flag
        var byTargets = ctx.multiplyByTarget();
        if (byTargets.Length == 0)
            throw new InvalidOperationException($"MULTIPLY statement has no valid targets or operands (line {ctx.Start?.Line})");

        var targets = new List<BoundArithmeticTarget>();
        foreach (var bt in byTargets)
        {
            var sym = _semantic.ResolveData(bt.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, bt.ROUNDED() != null));
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"MULTIPLY statement has no valid targets or operands (line {ctx.Start?.Line})");

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

        var sizeError = BindSizeErrorClause(ctx.multiplyOnSizeError());
        return new BoundMultiplyStatement(operand, targets, givingTarget, sizeError);
    }

    // ── ADD ──

    private BoundStatement BindAdd(CobolParserCore.AddStatementContext ctx)
    {
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
            operands.Add(BindSimpleOperand(op));

        // TO targets (each with per-target ROUNDED) — optional for GIVING form
        var targets = new List<BoundArithmeticTarget>();
        var toPhrase = ctx.addToPhrase();
        if (toPhrase != null)
        {
            foreach (var t in toPhrase.addTarget())
            {
                var sym = _semantic.ResolveData(t.identifier().GetText());
                if (sym != null)
                    targets.Add(new BoundArithmeticTarget(sym, t.ROUNDED() != null));
            }
        }

        // GIVING phrase overrides TO targets
        bool isGiving = false;
        var givingPhrase = ctx.addGivingPhrase();
        if (givingPhrase != null)
        {
            var givingTargetCtxs = givingPhrase.addTarget();
            if (givingTargetCtxs.Length > 0)
            {
                isGiving = true;
                targets.Clear();
                foreach (var gt in givingTargetCtxs)
                {
                    var sym = _semantic.ResolveData(gt.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                }
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException(
                $"ADD statement has no targets (line {ctx.Start?.Line})");

        var sizeError = BindSizeErrorClause(ctx.addOnSizeError());
        return new BoundAddStatement(operands, targets, sizeError, isGiving);
    }

    // ── SUBTRACT ──

    private BoundStatement BindSubtract(CobolParserCore.SubtractStatementContext ctx)
    {
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
            operands.Add(BindSimpleOperand(op));

        // FROM targets (each with per-target ROUNDED)
        var fromPhrase = ctx.subtractFromPhrase();
        if (fromPhrase == null)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        var fromTargetCtxs = fromPhrase.subtractTarget();
        if (fromTargetCtxs.Length == 0)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        var targets = new List<BoundArithmeticTarget>();
        foreach (var t in fromTargetCtxs)
        {
            var sym = _semantic.ResolveData(t.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, t.ROUNDED() != null));
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets or operands (line {ctx.Start?.Line})");

        // GIVING phrase: SUBTRACT a FROM b GIVING c [ROUNDED] → c = b - a
        bool isGiving = false;
        BoundExpression? givingMinuend = null;
        var givingPhrase = ctx.subtractGivingPhrase();
        if (givingPhrase != null)
        {
            var givingTargetCtxs = givingPhrase.subtractTarget();
            if (givingTargetCtxs.Length > 0)
            {
                isGiving = true;
                // The first FROM target becomes the minuend (b in "SUBTRACT a FROM b GIVING c")
                if (targets.Count > 0)
                    givingMinuend = new BoundIdentifierExpression(targets[0].Symbol, CobolCategory.Numeric);
                targets.Clear();
                foreach (var gt in givingTargetCtxs)
                {
                    var sym = _semantic.ResolveData(gt.identifier().GetText());
                    if (sym != null)
                        targets.Add(new BoundArithmeticTarget(sym, gt.ROUNDED() != null));
                }
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"SUBTRACT statement has no valid targets (line {ctx.Start?.Line})");

        var sizeError = BindSizeErrorClause(ctx.subtractOnSizeError());
        return new BoundSubtractStatement(operands, targets, sizeError, isGiving, givingMinuend);
    }

    // ── DIVIDE ──

    private BoundStatement BindDivide(CobolParserCore.DivideStatementContext ctx)
    {
        // DIVIDE operand INTO/BY ...
        var operandCtx = ctx.divideOperand();
        if (operandCtx == null)
            throw new InvalidOperationException($"DIVIDE statement has no valid targets or operands (line {ctx.Start?.Line})");

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
            throw new InvalidOperationException($"DIVIDE statement has no valid targets or operands (line {ctx.Start?.Line})");

        // REMAINDER
        DataSymbol? remainderTarget = null;
        var remPhrase = ctx.divideRemainderPhrase();
        if (remPhrase != null)
        {
            remainderTarget = _semantic.ResolveData(remPhrase.identifier().GetText());
        }

        var sizeError = BindSizeErrorClause(ctx.divideOnSizeError());
        return new BoundDivideStatement(firstOperand, dividend, isByForm, targets,
            remainderTarget, sizeError);
    }

    // ── COMPUTE ──

    private BoundStatement BindCompute(CobolParserCore.ComputeStatementContext ctx)
    {
        // COMPUTE target1 [ROUNDED] target2 [ROUNDED] = expression
        var storeCtxs = ctx.computeStore();
        if (storeCtxs.Length == 0)
            throw new InvalidOperationException($"COMPUTE statement has no valid targets or operands (line {ctx.Start?.Line})");

        var targets = new List<BoundArithmeticTarget>();
        foreach (var s in storeCtxs)
        {
            var sym = _semantic.ResolveData(s.identifier().GetText());
            if (sym != null)
                targets.Add(new BoundArithmeticTarget(sym, s.ROUNDED() != null));
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"COMPUTE statement has no valid targets or operands (line {ctx.Start?.Line})");

        // Bind the full arithmetic expression (recursive tree walk)
        var expr = BindFullExpression(ctx.arithmeticExpression());

        var sizeError = BindSizeErrorClause(ctx.computeOnSizeError());
        return new BoundComputeStatement(expr, targets, sizeError);
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
                BoundBinaryOperatorKind.Power,
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

    /// <summary>
    /// If expr is a bare identifier or unresolved string that matches a level-88
    /// condition name, return a BoundConditionNameExpression; otherwise return expr unchanged.
    /// </summary>
    private BoundExpression TryResolveConditionName(BoundExpression expr)
    {
        string? name = null;
        if (expr is BoundIdentifierExpression idExpr)
            name = idExpr.Symbol.Name;
        else if (expr is BoundLiteralExpression litExpr && litExpr.Value is string s)
            name = s;

        if (name != null)
        {
            var condSym = _semantic.ResolveConditionName(name);
            if (condSym != null)
                return new BoundConditionNameExpression(condSym);
        }
        return expr;
    }

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
            // ALL "literal" — extract the string and produce a figurative with AllLiteral
            if (figCtx.ALL() != null)
            {
                string? allText = null;
                var allStr = figCtx.STRINGLIT();
                if (allStr != null)
                {
                    var raw = allStr.GetText();
                    if (raw.Length >= 2) allText = raw[1..^1];
                }
                var allHex = figCtx.HEXLIT();
                if (allHex != null)
                {
                    var raw = allHex.GetText(); // X"..." or X'...'
                    if (raw.Length >= 3)
                    {
                        var hexBody = raw[2..^1];
                        var sb = new System.Text.StringBuilder();
                        for (int i = 0; i + 1 < hexBody.Length; i += 2)
                            sb.Append((char)Convert.ToByte(hexBody.Substring(i, 2), 16));
                        allText = sb.ToString();
                    }
                }
                return new BoundFigurativeExpression(
                    (int)FigurativeKind.None, allText ?? "");
            }

            string figText = figCtx.GetText().ToUpperInvariant();
            return figText switch
            {
                "SPACE" or "SPACES" =>
                    new BoundFigurativeExpression((int)FigurativeKind.Space),
                "ZERO" or "ZEROS" or "ZEROES" =>
                    new BoundFigurativeExpression((int)FigurativeKind.Zero),
                "HIGH-VALUE" or "HIGH-VALUES" =>
                    new BoundFigurativeExpression((int)FigurativeKind.HighValue),
                "LOW-VALUE" or "LOW-VALUES" =>
                    new BoundFigurativeExpression((int)FigurativeKind.LowValue),
                "QUOTE" or "QUOTES" =>
                    new BoundFigurativeExpression((int)FigurativeKind.Quote),
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
        var bound = BindLogicalOr(orExpr);
        return RewriteAbbreviatedRelations(bound);
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
                BoundBinaryOperatorKind.Or,
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
                BoundBinaryOperatorKind.And,
                right, CobolCategory.Unknown);
        }
        return result;
    }

    private BoundExpression BindLogicalNot(CobolParserCore.LogicalNotExpressionContext ctx)
    {
        // logicalNotExpression is a pass-through to relationalExpression.
        // COBOL-85 has no general logical NOT — NOT lives only inside
        // relational operators (NOT EQUAL, NOT GREATER, NOT LESS).
        // Logical NOT for condition-names (IF NOT STATUS-ACTIVE) will be
        // re-added as a separate production when level-88 is implemented.
        return BindRelational(ctx.relationalExpression());
    }

    private BoundExpression BindRelational(CobolParserCore.RelationalExpressionContext ctx)
    {
        var operands = ctx.relationalOperand();
        var relOp = ctx.relationalOperator();
        var classNameCtx = ctx.className();

        // Class condition: operand IS? NOT? className
        if (classNameCtx != null && operands.Length >= 1)
        {
            var subject = BindRelationalOperand(operands[0]);
            bool isNegated = ctx.NOT() != null;
            var kind = classNameCtx.GetText().ToUpperInvariant() switch
            {
                "NUMERIC" => ClassConditionKind.Numeric,
                "ALPHABETIC" => ClassConditionKind.Alphabetic,
                "ALPHABETIC-LOWER" => ClassConditionKind.AlphabeticLower,
                "ALPHABETIC-UPPER" => ClassConditionKind.AlphabeticUpper,
                _ => throw new InvalidOperationException($"Unknown class condition: {classNameCtx.GetText()}")
            };
            return new BoundClassConditionExpression(subject, kind, isNegated);
        }

        if (operands.Length == 0)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);

        var left = BindRelationalOperand(operands[0]);

        if (operands.Length < 2 || relOp == null)
        {
            // Check if bare identifier is a level-88 condition name
            if (left is BoundIdentifierExpression idExpr)
            {
                var condSym = _semantic.ResolveConditionName(idExpr.Symbol.Name);
                if (condSym != null)
                    return new BoundConditionNameExpression(condSym);
            }
            // Also check unresolved identifiers that became string literals
            if (left is BoundLiteralExpression litExpr && litExpr.Value is string condName)
            {
                var condSym = _semantic.ResolveConditionName(condName);
                if (condSym != null)
                    return new BoundConditionNameExpression(condSym);
            }
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

    // ═══════════════════════════════════
    // Abbreviated relation rewriting
    // ═══════════════════════════════════
    // COBOL allows abbreviated relational conditions:
    //   IF A = B OR C         → (A = B) OR (A = C)
    //   IF A < B AND C        → (A < B) AND (A < C)
    //   IF A < B < C          → (A < B) AND (B < C)    [chained, not abbreviated]
    //
    // After binding, abbreviated forms appear as:
    //   BoundBinaryExpression(Or/And, relational_expr, bare_operand)
    // where bare_operand is an identifier or literal with no relational operator.
    //
    // The rewrite propagates the left relation's operator and subject to the
    // bare operand, producing an explicit relational expression.

    /// <summary>
    /// Rewrite abbreviated relations in a bound condition expression tree.
    /// Called once after BindCondition to normalize all abbreviated forms into
    /// explicit relational expressions before lowering.
    /// </summary>
    private static BoundExpression RewriteAbbreviatedRelations(BoundExpression expr)
    {
        if (expr is not BoundBinaryExpression bin)
            return expr;

        // Recursively rewrite children first (bottom-up)
        var left = RewriteAbbreviatedRelations(bin.Left);
        var right = RewriteAbbreviatedRelations(bin.Right);

        // Only AND/OR can contain abbreviated relations
        if (bin.OperatorKind is BoundBinaryOperatorKind.Or
                             or BoundBinaryOperatorKind.And)
        {
            return RewriteLogicalWithAbbreviation(bin.OperatorKind, left, right);
        }

        // Rebuild if children changed
        if (ReferenceEquals(left, bin.Left) && ReferenceEquals(right, bin.Right))
            return bin;
        return new BoundBinaryExpression(left, bin.OperatorKind, right, bin.Category);
    }

    /// <summary>
    /// If the right side of a logical AND/OR is a bare operand (not a relational
    /// or logical expression), expand it using the left side's relational operator
    /// and subject. Otherwise return the expression unchanged.
    /// </summary>
    private static BoundExpression RewriteLogicalWithAbbreviation(
        BoundBinaryOperatorKind logicalOp,
        BoundExpression left,
        BoundExpression right)
    {
        // Only rewrite if right is a bare operand (identifier or literal)
        // and left contains a relational expression we can propagate from.
        if (!IsBareOperand(right))
            return new BoundBinaryExpression(left, logicalOp, right, CobolCategory.Unknown);

        // Extract the nearest relational expression from the left side.
        // For nested AND/OR chains like (A < B AND C AND D), the left may be
        // another logical expression — we need to find the rightmost relational.
        var (subject, relOp) = ExtractRelationalContext(left);
        if (subject == null)
            return new BoundBinaryExpression(left, logicalOp, right, CobolCategory.Unknown);

        // Expand: bare_operand → subject relOp bare_operand
        var expandedRight = new BoundBinaryExpression(subject, relOp, right, CobolCategory.Unknown);
        return new BoundBinaryExpression(left, logicalOp, expandedRight, CobolCategory.Unknown);
    }

    /// <summary>
    /// Extract the subject (left operand) and relational operator from an expression.
    /// For a direct relational (A = B), returns (A, Equal).
    /// For a logical chain ((A = B) AND (A = C)), walks the rightmost branch
    /// to find the innermost relational.
    /// </summary>
    private static (BoundExpression? Subject, BoundBinaryOperatorKind RelOp) ExtractRelationalContext(
        BoundExpression expr)
    {
        if (expr is BoundBinaryExpression bin)
        {
            if (IsRelational(bin.OperatorKind))
                return (bin.Left, bin.OperatorKind);

            // For logical AND/OR chains, the rightmost child carries the
            // most recent relational context.
            if (bin.OperatorKind is BoundBinaryOperatorKind.And
                                or BoundBinaryOperatorKind.Or)
                return ExtractRelationalContext(bin.Right);
        }

        return (null, default);
    }

    private static bool IsRelational(BoundBinaryOperatorKind kind) =>
        kind is BoundBinaryOperatorKind.Equal
            or BoundBinaryOperatorKind.NotEqual
            or BoundBinaryOperatorKind.Less
            or BoundBinaryOperatorKind.LessOrEqual
            or BoundBinaryOperatorKind.Greater
            or BoundBinaryOperatorKind.GreaterOrEqual;

    private static bool IsBareOperand(BoundExpression expr) =>
        expr is BoundIdentifierExpression or BoundLiteralExpression;

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
    /// Bind ON SIZE ERROR / NOT ON SIZE ERROR clause shared by all arithmetic statements.
    /// Handles both forms: ON+NOT, ON-only, NOT-only.
    /// </summary>
    private BoundSizeErrorClause? BindSizeErrorClause(Antlr4.Runtime.ParserRuleContext? ctx)
    {
        if (ctx == null) return null;

        // Get all imperativeStatement children using the tree API
        var imperatives = new List<CobolParserCore.ImperativeStatementContext>();
        for (int i = 0; i < ctx.ChildCount; i++)
        {
            if (ctx.GetChild(i) is CobolParserCore.ImperativeStatementContext imp)
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
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
        }
        else
        {
            // ON SIZE ERROR (+ optional NOT ON SIZE ERROR)
            // First imperative is for ON SIZE ERROR
            foreach (var stmt in imperatives[0].statement())
            {
                var bound = BindStatement(stmt);
                if (bound != null) onSizeError.Add(bound);
            }
            // Second imperative (if present) is for NOT ON SIZE ERROR
            if (imperatives.Count > 1)
                foreach (var stmt in imperatives[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnSizeError.Add(bound);
                }
        }

        if (onSizeError.Count == 0 && notOnSizeError.Count == 0)
            return null;

        return new BoundSizeErrorClause(onSizeError, notOnSizeError);
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
