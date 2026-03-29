// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Common;
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
    private readonly CompilationOptions _options;
    private readonly List<BoundParagraph> _paragraphs = new();

    public BoundTreeBuilder(SemanticModel semantic, DiagnosticBag diagnostics, CompilationOptions? options = null)
    {
        _semantic = semantic;
        _diagnostics = diagnostics;
        _options = options ?? new CompilationOptions();
    }

    private static SourceLocation MakeLocation(ParserRuleContext ctx) =>
        new("<source>", 0, ctx.Start.Line, ctx.Start.Column);
    private static TextSpan MakeSpan(ParserRuleContext ctx) =>
        new(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex);

    /// <summary>
    /// Attach an ExpressionType to a bound expression based on its category and symbol.
    /// </summary>
    private static T Typed<T>(T expr) where T : BoundExpression
    {
        expr.ResultType = expr switch
        {
            BoundIdentifierExpression id => ExpressionType.FromDataSymbol(id.Symbol),
            BoundReferenceModificationExpression => ExpressionType.Alphanumeric,
            BoundConditionNameExpression => ExpressionType.Boolean,
            BoundClassConditionExpression => ExpressionType.Boolean,
            BoundUserClassConditionExpression => ExpressionType.Boolean,
            BoundFigurativeExpression => ExpressionType.Alphanumeric,
            BoundBinaryExpression bin => bin.OperatorKind switch
            {
                BoundBinaryOperatorKind.Equal or BoundBinaryOperatorKind.NotEqual
                    or BoundBinaryOperatorKind.Less or BoundBinaryOperatorKind.LessOrEqual
                    or BoundBinaryOperatorKind.Greater or BoundBinaryOperatorKind.GreaterOrEqual
                    or BoundBinaryOperatorKind.And or BoundBinaryOperatorKind.Or
                    or BoundBinaryOperatorKind.Not
                    => ExpressionType.Boolean,
                _ => (bin.Left.ResultType != null && bin.Right.ResultType != null)
                    ? ExpressionType.Promote(bin.Left.ResultType, bin.Right.ResultType)
                    : ExpressionType.MakeNumeric(NumericType.Integer(18, true)),
            },
            BoundLiteralExpression lit => lit.Category == CobolCategory.Numeric
                ? ExpressionType.MakeNumeric(NumericType.Integer(18, true))
                : ExpressionType.Alphanumeric,
            _ => ExpressionType.Unknown,
        };
        return expr;
    }

    /// <summary>
    /// Resolve a procedure name (paragraph or section) to a ParagraphSymbol.
    /// For sections, returns the first paragraph in the section.
    /// For paragraphs, returns the paragraph directly.
    /// </summary>
    /// <summary>Extract the paragraph/section name from a procedureName context.
    /// Uses first IDENTIFIER/INTEGERLIT token only, ignoring OF/IN qualifiers.</summary>
    private static string ExtractProcedureNameText(CobolParserCore.ProcedureNameContext ctx)
    {
        var ids = ctx.IDENTIFIER();
        if (ids.Length > 0) return ids[0].GetText();
        var ints = ctx.INTEGERLIT();
        if (ints.Length > 0) return ints[0].GetText();
        return ctx.GetText();
    }

    private ParagraphSymbol? ResolveProcedureName(string name)
    {
        var para = _semantic.ResolveParagraph(name);
        var sec = _semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return para;
        }

        if (para != null) return para;

        if (sec != null)
        {
            var sectionParas = _semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
                return _semantic.ResolveParagraph(sectionParas[0]);

            _diagnostics.Report(DiagnosticDescriptors.COBOL0401,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return null;
        }

        _diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, name);
        return null;
    }

    /// <summary>
    /// Resolve a procedure name for THRU end targets.
    /// For sections, returns the LAST paragraph (end of section range).
    /// For paragraphs, returns the paragraph itself.
    /// </summary>
    private ParagraphSymbol? ResolveProcedureNameForThruEnd(string name)
    {
        var para = _semantic.ResolveParagraph(name);
        var sec = _semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return para;
        }

        if (para != null) return para;

        if (sec != null)
        {
            var sectionParas = _semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
                return _semantic.ResolveParagraph(sectionParas[^1]); // LAST paragraph

            _diagnostics.Report(DiagnosticDescriptors.COBOL0401,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return null;
        }

        _diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, name);
        return null;
    }

    private (ParagraphSymbol? first, ParagraphSymbol? last) ResolveProcedureNameForPerform(string name)
    {
        var para = _semantic.ResolveParagraph(name);
        var sec = _semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0400,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return (para, null);
        }

        if (para != null) return (para, null);

        if (sec != null)
        {
            var sectionParas = _semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
            {
                var first = _semantic.ResolveParagraph(sectionParas[0]);
                var last = _semantic.ResolveParagraph(sectionParas[^1]);
                return (first, sectionParas.Count > 1 ? last : null);
            }

            _diagnostics.Report(DiagnosticDescriptors.COBOL0401,
                Common.SourceLocation.None,
                Common.TextSpan.Empty, name);
            return (null, null);
        }

        _diagnostics.Report(DiagnosticDescriptors.COBOL0402,
            Common.SourceLocation.None,
            Common.TextSpan.Empty, name);
        return (null, null);
    }

    public BoundProgram Build(Antlr4.Runtime.ParserRuleContext tree)
    {
        Visit(tree);
        return new BoundProgram(_semantic.Program, _paragraphs);
    }

    public override object? VisitDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx)
    {
        // Extract section name
        string sectionName = ctx.sectionName()?.GetText() ?? "";

        // The first sentence in a declarative section contains the USE statement.
        // Extract it and register the USE association with the semantic model.
        var sentences = ctx.sentence();
        if (sentences.Length > 0)
        {
            foreach (var stmtCtx in sentences[0].statement())
            {
                if (stmtCtx.useStatement() is { } useCtx)
                {
                    var bound = BindUse(useCtx);
                    if (!bound.IsBeforeReporting)
                    {
                        foreach (var fileName in bound.FileNames)
                            _semantic.RegisterUseDeclarative(fileName, sectionName);
                    }
                }
            }
        }

        // Continue visiting children (declarative paragraphs become bound paragraphs)
        return base.VisitDeclarativeSection(ctx);
    }

    public override object? VisitParagraphDefinition(CobolParserCore.ParagraphDefinitionContext ctx)
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

    public override object? VisitDeclarativeParagraph(CobolParserCore.DeclarativeParagraphContext ctx)
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

        _paragraphs.Add(new BoundParagraph(paraSym, sentences, isDeclarative: true));
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
        if (ctx.alterStatement() is { } alt) return BindAlter(alt);
        if (ctx.entryStatement() is { } entry) return BindEntry(entry);
        if (ctx.cancelStatement() is { } cancel) return BindCancel(cancel);
        if (ctx.stopStatement() is { }) return new BoundStopStatement();
        if (ctx.gobackStatement() is { }) return new BoundGoBackStatement();
        if (ctx.exitStatement() is { } exitCtx)
        {
            if (exitCtx.PROGRAM() != null)
                return new BoundExitProgramStatement();
            if (exitCtx.PERFORM() != null)
                return new BoundExitPerformStatement(isCycle: exitCtx.CYCLE() != null);
            if (exitCtx.PARAGRAPH() != null)
                return new BoundExitParagraphStatement();
            if (exitCtx.SECTION() != null)
                return new BoundExitSectionStatement();
            return new BoundExitStatement();
        }
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
        if (ctx.rewriteStatement() is { } rewriteCtx) return BindRewrite(rewriteCtx);
        if (ctx.initializeStatement() is { } initCtx) return BindInitialize(initCtx);
        if (ctx.setStatement() is { } setCtx) return BindSet(setCtx);
        if (ctx.inspectStatement() is { } inspCtx) return BindInspect(inspCtx);
        if (ctx.acceptStatement() is { } accCtx) return BindAccept(accCtx);
        if (ctx.searchStatement() is { } searchCtx) return BindSearch(searchCtx);
        if (ctx.searchAllStatement() is { } searchAllCtx) return BindSearchAll(searchAllCtx);
        if (ctx.stringStatement() is { } stringCtx) return BindString(stringCtx);
        if (ctx.unstringStatement() is { } unstringCtx) return BindUnstring(unstringCtx);
        if (ctx.deleteStatement() is { } delCtx) return BindDelete(delCtx);
        if (ctx.startStatement() is { } startCtx) return BindStart(startCtx);
        if (ctx.returnStatement() is { } retCtx) return BindReturn(retCtx);
        if (ctx.sortStatement() is { } sortCtx) return BindSort(sortCtx);
        if (ctx.mergeStatement() is { } mergeCtx) return BindMerge(mergeCtx);
        if (ctx.releaseStatement() is { } relCtx) return BindRelease(relCtx);
        if (ctx.callStatement() is { } callCtx) return BindCall(callCtx);
        if (ctx.continueStatement() != null) return new BoundExitStatement(); // CONTINUE is a no-op
        if (ctx.useStatement() is { }) return new BoundExitStatement(); // USE is a no-op stub

        _diagnostics.Report(DiagnosticDescriptors.COBOL0110,
            new Common.SourceLocation("<source>", 0, ctx.Start?.Line ?? 0, 0),
            Common.TextSpan.Empty,
            $"{ctx.GetText()[..Math.Min(30, ctx.GetText().Length)]}...");
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

            if (child is CobolParserCore.DataReferenceContext idCtx)
            {
                operands.Add(BindDataReferenceWithSubscripts(idCtx));
            }
            else if (child is CobolParserCore.LiteralContext litCtx)
            {
                operands.Add(BindLiteral(litCtx));
            }
        }

        return new BoundDisplayStatement(operands);
    }

    // ── MOVE ──

    private BoundStatement? BindMove(CobolParserCore.MoveStatementContext ctx)
    {
        // MOVE CORRESPONDING source TO target
        if (ctx.CORRESPONDING() != null)
            return BindCorresponding(CorrespondingKind.Move, ctx.dataReference(), ctx);

        var moveSource = ctx.moveSendingOperand();
        var moveTarget = ctx.moveReceivingPhrase();
        if (moveSource == null || moveTarget == null) return null;

        var source = BindMoveSendingOperand(moveSource);

        var targets = new List<BoundExpression>();
        var idList = moveTarget.dataReferenceList();
        if (idList != null)
        {
            foreach (var id in idList.dataReference())
                targets.Add(BindDataReferenceWithSubscripts(id));
        }

        // MOVE type enforcement
        {
            var moveLoc = new Common.SourceLocation("<source>", 0, ctx.Start?.Line ?? 0, 0);
            var moveSpan = Common.TextSpan.Empty;
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
                    _diagnostics.Report(DiagnosticDescriptors.CBL0901, moveLoc, moveSpan, effectiveSrcCat, tgtCat);

                // Check 1: MOVE ZERO to Alphabetic
                if (source is BoundFigurativeExpression fig2
                    && fig2.FigurativeKind == Runtime.FigurativeKind.Zero
                    && tgtCat == CobolCategory.Alphabetic)
                    _diagnostics.Report(DiagnosticDescriptors.CBL0908, moveLoc, moveSpan);

                // Check 2: HIGH-VALUE/LOW-VALUE/QUOTE to Numeric
                if (source is BoundFigurativeExpression fig3
                    && fig3.FigurativeKind is Runtime.FigurativeKind.HighValue
                        or Runtime.FigurativeKind.LowValue
                        or Runtime.FigurativeKind.Quote
                    && tgtCat.IsNumericLike())
                    _diagnostics.Report(DiagnosticDescriptors.CBL0906, moveLoc, moveSpan, fig3.FigurativeKind);

                // Check 3: Numeric noninteger literal to Alphanumeric
                if (source is BoundLiteralExpression srcLit
                    && srcLit.Category == CobolCategory.Numeric
                    && srcLit.Value is decimal decVal
                    && decVal != decimal.Truncate(decVal)
                    && tgtCat.IsAlphanumericLike()
                    && !tgtCat.IsNumericLike())
                    _diagnostics.Report(DiagnosticDescriptors.CBL0907, moveLoc, moveSpan);
            }
        }

        return new BoundMoveStatement(source, targets, isRounded: false);
    }

    private BoundCorrespondingStatement? BindCorresponding(
        CorrespondingKind kind,
        CobolParserCore.DataReferenceContext[] ids,
        Antlr4.Runtime.ParserRuleContext ctx,
        bool isRounded = false,
        BoundSizeErrorClause? sizeError = null)
    {
        if (ids.Length < 2) return null;

        var srcExpr = BindDataReferenceWithSubscripts(ids[0]);
        var dstExpr = BindDataReferenceWithSubscripts(ids[1]);

        if (srcExpr is not BoundIdentifierExpression srcId ||
            dstExpr is not BoundIdentifierExpression dstId)
            return null;

        var srcSym = srcId.Symbol;
        var dstSym = dstId.Symbol;
        var loc = new Common.SourceLocation("<source>", 0, ctx.Start.Line, ctx.Start.Column);
        var span = Common.TextSpan.Empty;
        var kindName = kind.ToString().ToUpperInvariant();
        bool hasError = false;

        // Check 11: CORRESPONDING excludes RENAMES items (ISO §14.9.26)
        if (srcSym.LevelNumber == 66)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0414,
                loc, span, kindName, srcSym.DisplayName);
            hasError = true;
        }
        if (dstSym.LevelNumber == 66)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0414,
                loc, span, kindName, dstSym.DisplayName);
            hasError = true;
        }

        // Source must be a group item
        if (srcSym.IsElementary)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0403,
                loc, span, kindName, srcSym.DisplayName);
            hasError = true;
        }

        // Target must be a group item
        if (dstSym.IsElementary)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0403,
                loc, span, kindName, dstSym.DisplayName);
            hasError = true;
        }

        if (hasError)
            return new BoundCorrespondingStatement(kind, srcSym, dstSym, []);

        var pairs = CorrespondingMatcher.ComputeCorrespondingPairs(
            srcSym, dstSym, kindName, _diagnostics, loc);
        return new BoundCorrespondingStatement(kind, srcSym, dstSym, pairs, isRounded, sizeError);
    }

    private BoundExpression BindMoveSendingOperand(CobolParserCore.MoveSendingOperandContext ctx)
    {
        // moveSource: literal | functionCall | dataReference (COBOL-85 + 1989 Amendment)
        var litCtx = ctx.literal();
        if (litCtx != null) return BindLiteral(litCtx);

        if (ctx.functionCall() != null)
            return BindFunctionCall(ctx.functionCall());

        if (ctx.dataReference() != null)
            return BindDataReferenceWithSubscripts(ctx.dataReference());

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── PERFORM ──

    private BoundPerformStatement? BindPerform(CobolParserCore.PerformStatementContext ctx)
    {
        var procNames = ctx.procedureName();

        // Inline forms: no procedureName
        if (procNames.Length == 0)
        {
            BoundExpression? untilCond = null;
            BoundPerformVarying? varying = null;
            bool isTestAfter = false;

            BoundExpression? timesExpr = null;
            var options = ctx.performOptions();
            if (options != null)
            {
                foreach (var opt in options)
                {
                    if (opt.performTimes() is { } inlineTimesCtx)
                    {
                        if (inlineTimesCtx.integerLiteral() != null)
                            timesExpr = new BoundLiteralExpression(
                                decimal.Parse(inlineTimesCtx.integerLiteral().GetText()),
                                CobolCategory.Numeric);
                        else if (inlineTimesCtx.dataReference() != null)
                            timesExpr = BindDataReferenceWithSubscripts(inlineTimesCtx.dataReference());
                    }
                    if (opt.performUntil() is { } untilCtx)
                    {
                        untilCond = BindCondition(untilCtx.condition());
                        if (untilCtx.AFTER() != null)
                            isTestAfter = true;
                    }
                    if (opt.performVarying() is { } varyCtx)
                    {
                        varying = BindPerformVaryingOption(varyCtx);
                        if (varyCtx.AFTER() != null)
                            isTestAfter = true;
                    }
                }
            }

            var inlineStmts = new List<BoundStatement>();
            foreach (var imp in ctx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) inlineStmts.Add(bound);
                }

            if (varying != null)
                untilCond = varying.UntilCondition;

            return new BoundPerformStatement(null, null, timesExpr, untilCond, varying, inlineStmts,
                isTestAfter);
        }

        // Out-of-line: first procedureName is the target (paragraph or section)
        string name = ExtractProcedureNameText(procNames[0]);
        var (paraSym, sectionLastPara) = ResolveProcedureNameForPerform(name);
        if (paraSym == null) return null;

        // PERFORM para N TIMES
        if (ctx.performTimes() is { } timesCtx)
        {
            BoundExpression timesExpr;
            if (timesCtx.integerLiteral() != null)
                timesExpr = new BoundLiteralExpression(
                    decimal.Parse(timesCtx.integerLiteral().GetText()),
                    CobolCategory.Numeric);
            else
                timesExpr = BindDataReferenceWithSubscripts(timesCtx.dataReference());

            return new BoundPerformStatement(paraSym, sectionLastPara, timesExpression: timesExpr);
        }

        // PERFORM para UNTIL cond
        if (ctx.performUntil() is { } untilCtx2)
        {
            var cond = BindCondition(untilCtx2.condition());
            return new BoundPerformStatement(paraSym, sectionLastPara, untilCondition: cond,
                isTestAfter: untilCtx2.AFTER() != null);
        }

        // PERFORM para VARYING ...
        if (ctx.performVarying() is { } varyCtx2)
        {
            var varying = BindPerformVaryingOption(varyCtx2);
            return new BoundPerformStatement(paraSym, sectionLastPara, varying: varying,
                untilCondition: varying?.UntilCondition,
                isTestAfter: varyCtx2.AFTER() != null);
        }

        // PERFORM para THRU para2 [options]
        if (procNames.Length > 1)
        {
            string thruName = ExtractProcedureNameText(procNames[1]);
            var thruSym = ResolveProcedureNameForThruEnd(thruName);

            // Check for options on the THRU form
            BoundExpression? timesExpr2 = null;
            BoundExpression? untilCond = null;
            BoundPerformVarying? varyOpt = null;
            bool isTestAfter = false;
            var options = ctx.performOptions();
            if (options != null && options.Length > 0)
            {
                var opt = options[0];
                if (opt.performTimes() is { } thruTimesCtx)
                {
                    if (thruTimesCtx.integerLiteral() != null)
                        timesExpr2 = new BoundLiteralExpression(
                            decimal.Parse(thruTimesCtx.integerLiteral().GetText()),
                            CobolCategory.Numeric);
                    else if (thruTimesCtx.dataReference() != null)
                        timesExpr2 = BindDataReferenceWithSubscripts(thruTimesCtx.dataReference());
                }
                if (opt.performUntil() is { } u)
                {
                    untilCond = BindCondition(u.condition());
                    if (u.AFTER() != null) isTestAfter = true;
                }
                if (opt.performVarying() is { } v)
                {
                    varyOpt = BindPerformVaryingOption(v);
                    untilCond = varyOpt?.UntilCondition;
                    if (v.AFTER() != null) isTestAfter = true;
                }
            }

            return new BoundPerformStatement(paraSym, thruSym, timesExpr2, untilCond, varyOpt,
                isTestAfter: isTestAfter);
        }

        // Simple PERFORM para (or PERFORM section → implicit THRU)
        return new BoundPerformStatement(paraSym, sectionLastPara);
    }

    private BoundPerformVarying? BindPerformVaryingOption(CobolParserCore.PerformVaryingContext ctx)
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
                var afterExpr = BindDataReferenceWithSubscripts(afterCtx.dataReference());
                var afterSym = ValidatePerformIndex(afterExpr);
                if (afterSym == null) continue;
                var afterExprs = afterCtx.arithmeticExpression();
                var afterInit = BindAdditiveExpression(afterExprs[0].additiveExpression());
                var afterStep = BindAdditiveExpression(afterExprs[1].additiveExpression());
                var afterUntil = BindCondition(afterCtx.condition());
                inner = new BoundPerformVarying(afterSym, afterInit, afterStep, afterUntil, inner);
            }
        }

        // Outer VARYING
        var indexExpr = BindDataReferenceWithSubscripts(ctx.dataReference());
        var indexSym = ValidatePerformIndex(indexExpr);
        if (indexSym == null) return null;
        var arithExprs = ctx.arithmeticExpression();
        var initial = BindAdditiveExpression(arithExprs[0].additiveExpression());  // FROM
        var step = BindAdditiveExpression(arithExprs[1].additiveExpression());      // BY
        var untilCond = BindCondition(ctx.condition());

        return new BoundPerformVarying(indexSym, initial, step, untilCond, inner);
    }

    /// <summary>
    /// Validate a PERFORM VARYING index expression: must be a non-subscripted
    /// elementary numeric identifier. Returns the DataSymbol or null on failure.
    /// </summary>
    private DataSymbol? ValidatePerformIndex(BoundExpression indexExpr)
    {
        if (indexExpr is not BoundIdentifierExpression id)
        {
            // CS0860: PERFORM VARYING index must be an identifier
            // (ref-mod or other expression not allowed)
            return null;
        }

        return id.Symbol;
    }

    // ── EVALUATE ──

    private BoundEvaluateStatement BindEvaluate(CobolParserCore.EvaluateStatementContext ctx)
    {
        // Bind subjects — detect EVALUATE TRUE
        var subjects = new List<BoundExpression>();
        bool isEvaluateTrue = false;

        foreach (var subCtx in ctx.evaluateSubject())
        {
            if (subCtx.booleanLiteral()?.TRUE_() != null)
            {
                isEvaluateTrue = true;
                continue;
            }
            if (subCtx.valueOperand()?.arithmeticExpression() is { } arithCtx)
                subjects.Add(BindAdditiveExpression(arithCtx.additiveExpression()));
            else if (subCtx.valueOperand()?.nonNumericLiteral() is { } nonNumCtx)
                subjects.Add(BindNonNumericLiteral(nonNumCtx));
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
                foreach (var imp in whenClause.statementBlock())
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
            foreach (var imp in whenClause.statementBlock())
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
            // Fallback: try as valueOperand (bare identifier → condition name)
            if (items.Length > 0 && items[0].valueOperand()?.arithmeticExpression() is { } arithFallback)
            {
                var expr = BindAdditiveExpression(arithFallback.additiveExpression());
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

            if (item.valueRange() is { } rangeCtx)
            {
                // Range: value THRU value
                var from = BindValueOperand(rangeCtx.valueOperand(0));
                var to = BindValueOperand(rangeCtx.valueOperand(1));
                ranges.Add(new BoundEvaluateRange(from, to));
            }
            else if (item.valueOperand() is { } voCtx)
            {
                values.Add(BindValueOperand(voCtx));
            }
            else if (item.condition() is { } condCtx)
            {
                values.Add(BindCondition(condCtx));
            }
        }

        return new BoundEvaluateValueCondition(values, ranges, isAny);
    }

    private BoundExpression BindValueOperand(CobolParserCore.ValueOperandContext vo)
    {
        if (vo.nonNumericLiteral() is { } nonNumCtx)
            return BindNonNumericLiteral(nonNumCtx);
        return BindAdditiveExpression(vo.arithmeticExpression().additiveExpression());
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

        // Parse BEFORE/AFTER ADVANCING clause
        int? advancingLines = null;
        bool isAfterAdvancing = true;
        BoundExpression? advancingExpression = null;
        var advCtx = ctx.writeBeforeAfter();
        if (advCtx != null)
        {
            isAfterAdvancing = advCtx.GetChild(0).GetText().Equals("AFTER", StringComparison.OrdinalIgnoreCase);
            // Parse the advancing value — integer literal, PAGE, or identifier
            if (advCtx.PAGE() != null)
            {
                advancingLines = -1; // PAGE = form-feed (sentinel value)
            }
            else
            {
                var intLit = advCtx.integerLiteral();
                if (intLit != null)
                {
                    advancingLines = int.Parse(intLit.GetText());
                }
                else
                {
                    // Could be a data identifier referencing a data field
                    var idCtx = advCtx.dataReference();
                    if (idCtx != null)
                    {
                        // Data identifier — bind as expression, read at runtime
                        advancingExpression = BindDataReferenceWithSubscripts(idCtx);
                        advancingLines = 0; // Sentinel: will be overridden at runtime
                    }
                    else
                    {
                        advancingLines = 1; // Default: 1 line
                    }
                }
            }
        }

        BoundExpression? from = null;
        if (ctx.writeFrom() is { } fromCtx)
            from = BindDataReferenceWithSubscripts(fromCtx.dataReference());

        // INVALID KEY / NOT INVALID KEY
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.writeInvalidKey() is { } wikCtx)
        {
            var impStmts = wikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundWriteStatement(fileSym, recordSym, from, advancingLines, isAfterAdvancing, invalidKey, notInvalidKey,
            advancingExpression: advancingExpression);
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
                "I-O" => OpenMode.IO,
                "EXTEND" => OpenMode.Extend,
                _ => OpenMode.Output
            };

            var files = new List<FileSymbol>();
            foreach (var idCtx in clause.dataReference())
            {
                string name = idCtx.IDENTIFIER().GetText();
                var fileSym = _semantic.ResolveFile(name);
                if (fileSym != null)
                    files.Add(fileSym);
            }

            if (files.Count > 0)
                results.Add(new BoundOpenStatement(mode, files));
        }

        if (results.Count == 1) return results[0];
        return results.Count > 1 ? new BoundCompoundStatement(results)
            : new BoundOpenStatement(OpenMode.Output, Array.Empty<FileSymbol>());
    }

    // ── CLOSE ──

    private BoundStatement BindClose(CobolParserCore.CloseStatementContext ctx)
    {
        var phrases = new List<BoundCloseFilePhrase>();
        foreach (var phraseCtx in ctx.closeFilePhrase())
        {
            var fn = phraseCtx.fileName();
            if (fn == null) continue;
            string name = fn.GetText();
            var fileSym = _semantic.ResolveFile(name);
            if (fileSym == null) continue;

            var option = CloseOption.None;
            var optCtx = phraseCtx.closeOption();
            if (optCtx != null)
            {
                if (optCtx.LOCK() != null) option = CloseOption.Lock;
                else if (optCtx.NO() != null) option = CloseOption.NoRewind;
                else if (optCtx.REEL() != null) option = CloseOption.Reel;
                else if (optCtx.UNIT() != null) option = CloseOption.Unit;
            }
            phrases.Add(new BoundCloseFilePhrase(fileSym, option));
        }
        return new BoundCloseStatement(phrases);
    }

    // ── READ ──

    private BoundStatement? BindRead(CobolParserCore.ReadStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        string name = fileNameCtx.GetText();
        var fileSym = _semantic.ResolveFile(name);
        if (fileSym == null) return null;

        // NEXT/PREVIOUS direction
        var direction = ReadDirection.None;
        var dirCtx = ctx.readDirection();
        if (dirCtx != null)
        {
            if (dirCtx.PREVIOUS() != null)
                direction = ReadDirection.Previous;
            else
                direction = ReadDirection.Next;
        }

        // KEY IS data-name
        string? keyDataName = null;
        if (ctx.readKey() is { } keyCtx)
            keyDataName = keyCtx.dataReference().IDENTIFIER().GetText();

        // INTO clause
        BoundIdentifierExpression? intoId = null;
        var intoCtx = ctx.readInto();
        if (intoCtx != null)
        {
            var intoExpr = BindDataReferenceWithSubscripts(intoCtx.dataReference());
            intoId = intoExpr as BoundIdentifierExpression;
        }

        // AT END / NOT AT END
        var atEnd = new List<BoundStatement>();
        var notAtEnd = new List<BoundStatement>();
        var atEndCtx = ctx.readAtEnd();
        if (atEndCtx != null)
        {
            var impStmts = atEndCtx.statementBlock();
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

        // INVALID KEY / NOT INVALID KEY (separate from AT END for keyed/random reads)
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.readInvalidKey() is { } ikCtx)
        {
            var impStmts = ikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundReadStatement(fileSym, intoId, direction, keyDataName, atEnd, notAtEnd, invalidKey, notInvalidKey);
    }

    // ── REWRITE ──

    private BoundStatement? BindRewrite(CobolParserCore.RewriteStatementContext ctx)
    {
        var recordCtx = ctx.recordName();
        if (recordCtx == null) return null;

        string recordName = recordCtx.GetText();
        var recordSym = _semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        var fileSym = _semantic.ResolveFileForRecord(recordSym);
        if (fileSym == null) return null;

        // FROM clause
        BoundExpression? from = null;
        var fromCtx = ctx.rewriteFrom()?.dataReference();
        if (fromCtx != null)
            from = BindDataReferenceWithSubscripts(fromCtx);

        // INVALID KEY / NOT INVALID KEY
        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.rewriteInvalidKeyPhrase() is { } rikCtx)
        {
            var impStmts = rikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundRewriteStatement(fileSym, recordSym, from, invalidKey, notInvalidKey);
    }

    // ── DELETE ──

    private BoundStatement? BindDelete(CobolParserCore.DeleteStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.deleteInvalidKeyPhrase() is { } ikCtx)
        {
            var impStmts = ikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundDeleteStatement(fileSym, invalidKey, notInvalidKey);
    }

    // ── START ──

    private BoundStatement? BindStart(CobolParserCore.StartStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // KEY IS relationalExpression (optional)
        BoundExpression? keyCondition = null;
        if (ctx.startKeyPhrase() is { } keyCtx)
            keyCondition = BindComparison(keyCtx.comparisonExpression());

        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.startInvalidKeyPhrase() is { } ikCtx)
        {
            var impStmts = ikCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) invalidKey.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notInvalidKey.Add(bound);
                }
        }

        return new BoundStartStatement(fileSym, keyCondition, invalidKey, notInvalidKey);
    }

    // ── RETURN ──

    private BoundStatement? BindReturn(CobolParserCore.ReturnStatementContext ctx)
    {
        var fileNameCtx = ctx.fileName();
        if (fileNameCtx == null) return null;

        var fileSym = _semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // INTO clause
        BoundIdentifierExpression? intoId = null;
        var intoCtx = ctx.dataReference();
        if (intoCtx != null)
        {
            var intoExpr = BindDataReferenceWithSubscripts(intoCtx);
            intoId = intoExpr as BoundIdentifierExpression;
        }

        // AT END / NOT AT END
        var atEnd = new List<BoundStatement>();
        var notAtEnd = new List<BoundStatement>();
        if (ctx.returnAtEndPhrase() is { } atEndCtx)
        {
            var impStmts = atEndCtx.statementBlock();
            if (impStmts.Length >= 1)
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
            if (impStmts.Length >= 2)
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notAtEnd.Add(bound);
                }
        }

        return new BoundReturnStatement(fileSym, intoId, atEnd, notAtEnd);
    }

    // ── SORT ──

    private BoundStatement? BindSort(CobolParserCore.SortStatementContext ctx)
    {
        var fileNameCtx = ctx.sortFileName()?.dataReference();
        if (fileNameCtx == null) return null;

        var fileSym = _semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        // Parse sort keys
        var keys = BindSortKeys(ctx.sortKeyPhrase(), fileSym);

        bool duplicates = ctx.sortDuplicatesPhrase() != null;

        // USING / INPUT PROCEDURE
        IReadOnlyList<FileSymbol>? usingFiles = null;
        ParagraphSymbol? inputProc = null, inputProcThru = null;
        if (ctx.sortUsingPhrase() is { } usingCtx)
        {
            usingFiles = ResolveFileList(usingCtx.dataReferenceList());
        }
        else if (ctx.sortInputProcedurePhrase() is { } inputCtx)
        {
            var procNames = inputCtx.procedureName();
            if (procNames.Length >= 1)
                inputProc = ResolveProcedureName(ExtractProcedureNameText(procNames[0]));
            if (procNames.Length >= 2)
                inputProcThru = ResolveProcedureNameForThruEnd(ExtractProcedureNameText(procNames[1]));
        }

        // GIVING / OUTPUT PROCEDURE
        IReadOnlyList<FileSymbol>? givingFiles = null;
        ParagraphSymbol? outputProc = null, outputProcThru = null;
        if (ctx.sortGivingPhrase() is { } givingCtx)
        {
            givingFiles = ResolveFileList(givingCtx.dataReferenceList());
        }
        else if (ctx.sortOutputProcedurePhrase() is { } outputCtx)
        {
            var procNames = outputCtx.procedureName();
            if (procNames.Length >= 1)
                outputProc = ResolveProcedureName(ExtractProcedureNameText(procNames[0]));
            if (procNames.Length >= 2)
                outputProcThru = ResolveProcedureNameForThruEnd(ExtractProcedureNameText(procNames[1]));
        }

        return new BoundSortStatement(fileSym, keys, duplicates,
            usingFiles, givingFiles,
            inputProc, inputProcThru,
            outputProc, outputProcThru);
    }

    // ── MERGE ──

    private BoundStatement? BindMerge(CobolParserCore.MergeStatementContext ctx)
    {
        var fileNameCtx = ctx.mergeFileName()?.dataReference();
        if (fileNameCtx == null) return null;

        var fileSym = _semantic.ResolveFile(fileNameCtx.GetText());
        if (fileSym == null) return null;

        var keys = BindMergeKeys(ctx.mergeKeyPhrase(), fileSym);

        // USING (required for MERGE)
        var usingFiles = ResolveFileList(ctx.mergeUsingPhrase().dataReferenceList());

        // GIVING / OUTPUT PROCEDURE
        IReadOnlyList<FileSymbol>? givingFiles = null;
        ParagraphSymbol? outputProc = null, outputProcThru = null;
        if (ctx.mergeGivingPhrase() is { } givingCtx)
        {
            givingFiles = ResolveFileList(givingCtx.dataReferenceList());
        }
        else if (ctx.mergeOutputProcedurePhrase() is { } outputCtx)
        {
            var procNames = outputCtx.procedureName();
            if (procNames.Length >= 1)
                outputProc = ResolveProcedureName(ExtractProcedureNameText(procNames[0]));
            if (procNames.Length >= 2)
                outputProcThru = ResolveProcedureNameForThruEnd(ExtractProcedureNameText(procNames[1]));
        }

        return new BoundMergeStatement(fileSym, keys, usingFiles, givingFiles,
            outputProc, outputProcThru);
    }

    // ── RELEASE ──

    private BoundStatement? BindRelease(CobolParserCore.ReleaseStatementContext ctx)
    {
        // record-name-1 is the first dataReference — must be a record in an SD
        var recordRef = ctx.dataReference();
        if (recordRef == null) return null;

        string recordName = recordRef.GetText();
        var recordSym = _semantic.ResolveData(recordName);
        if (recordSym == null) return null;

        // Find the SD file for this record
        var fileSym = _semantic.ResolveFileForRecord(recordSym);
        if (fileSym == null) return null;

        // FROM clause
        BoundExpression? fromExpr = null;
        var fromCtx = ctx.releaseFrom()?.dataReference();
        if (fromCtx != null)
        {
            fromExpr = BindDataReferenceWithSubscripts(fromCtx);
        }

        return new BoundReleaseStatement(fileSym, recordSym, fromExpr);
    }

    // ── Sort/merge key binding helpers ──

    private List<BoundSortKey> BindSortKeys(
        CobolParserCore.SortKeyPhraseContext[] keyPhrases, FileSymbol file)
    {
        var keys = new List<BoundSortKey>();
        foreach (var phrase in keyPhrases)
        {
            bool ascending = phrase.ASCENDING() != null;
            foreach (var dataRef in phrase.dataReferenceList().dataReference())
            {
                var keySym = _semantic.ResolveData(dataRef.GetText());
                if (keySym != null)
                    keys.Add(new BoundSortKey(keySym, ascending));
            }
        }
        return keys;
    }

    private List<BoundSortKey> BindMergeKeys(
        CobolParserCore.MergeKeyPhraseContext[] keyPhrases, FileSymbol file)
    {
        var keys = new List<BoundSortKey>();
        foreach (var phrase in keyPhrases)
        {
            bool ascending = phrase.ASCENDING() != null;
            foreach (var dataRef in phrase.dataReferenceList().dataReference())
            {
                var keySym = _semantic.ResolveData(dataRef.GetText());
                if (keySym != null)
                    keys.Add(new BoundSortKey(keySym, ascending));
            }
        }
        return keys;
    }

    private List<FileSymbol> ResolveFileList(CobolParserCore.DataReferenceListContext listCtx)
    {
        var files = new List<FileSymbol>();
        foreach (var dataRef in listCtx.dataReference())
        {
            var fileSym = _semantic.ResolveFile(dataRef.GetText());
            if (fileSym != null)
                files.Add(fileSym);
        }
        return files;
    }

    // ── CALL ──

    private BoundStatement? BindCall(CobolParserCore.CallStatementContext ctx)
    {
        var targetCtx = ctx.callTarget();
        if (targetCtx == null) return null;

        // Extract target name: literal or data reference
        string targetName;
        bool isDynamic;
        if (targetCtx.literal() is { } litCtx)
        {
            // CALL "LITERAL" — static call (program name known at compile time)
            targetName = litCtx.GetText().Trim('"', '\'');
            isDynamic = false;
        }
        else if (targetCtx.dataReference() is { } dataRefCtx)
        {
            // CALL identifier — dynamic call (program name computed at runtime)
            targetName = dataRefCtx.IDENTIFIER().GetText();
            isDynamic = true;
        }
        else
        {
            return null;
        }

        // USING arguments
        var arguments = new List<BoundCallArgument>();
        if (ctx.callUsingPhrase() is { } usingCtx)
        {
            foreach (var argCtx in usingCtx.callArgument())
            {
                if (argCtx.callByReference() is { } byRef)
                {
                    var expr = BindDataReferenceWithSubscripts(byRef.dataReference());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByReference, expr));
                }
                else if (argCtx.callByContent() is { } byContent)
                {
                    BoundExpression? expr = null;
                    if (byContent.dataReference() is { } dr)
                        expr = BindDataReferenceWithSubscripts(dr);
                    else if (byContent.literal() is { } lit)
                        expr = BindLiteral(lit);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByContent, expr));
                }
                else if (argCtx.callByValue() is { } byValue)
                {
                    var expr = BindAdditiveExpression(byValue.arithmeticExpression().additiveExpression());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByValue, expr));
                }
                else if (argCtx.dataReference() is { } bareRef)
                {
                    // Bare argument without BY keyword = BY REFERENCE (default)
                    var expr = BindDataReferenceWithSubscripts(bareRef);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByReference, expr));
                }
            }
        }

        // RETURNING
        BoundIdentifierExpression? returningTarget = null;
        if (ctx.callReturningPhrase() is { } retCtx)
        {
            var retExpr = BindDataReferenceWithSubscripts(retCtx.dataReference());
            returningTarget = retExpr as BoundIdentifierExpression;
        }

        // ON EXCEPTION / NOT ON EXCEPTION (independently optional per spec)
        var onException = new List<BoundStatement>();
        var notOnException = new List<BoundStatement>();
        if (ctx.callOnExceptionPhrase() is { } excCtx)
        {
            foreach (var stmt in excCtx.statementBlock().statement())
            {
                var bound = BindStatement(stmt);
                if (bound != null) onException.Add(bound);
            }
        }
        if (ctx.callNotOnExceptionPhrase() is { } notExcCtx)
        {
            foreach (var stmt in notExcCtx.statementBlock().statement())
            {
                var bound = BindStatement(stmt);
                if (bound != null) notOnException.Add(bound);
            }
        }

        return new BoundCallStatement(targetName, isDynamic, arguments, returningTarget,
            onException, notOnException);
    }

    // ── ACCEPT ──

    private BoundStatement? BindAccept(CobolParserCore.AcceptStatementContext ctx)
    {
        var targetId = BindDataReferenceWithSubscripts(ctx.dataReference());
        if (targetId is not BoundIdentifierExpression boundTarget) return null;

        var sourceKind = AcceptSourceKind.None;
        var sourceCtx = ctx.acceptSource();
        if (sourceCtx != null)
        {
            if (sourceCtx.DATE() != null && sourceCtx.YYYYMMDD() != null)
                sourceKind = AcceptSourceKind.DateYYYYMMDD;
            else if (sourceCtx.DATE() != null) sourceKind = AcceptSourceKind.Date;
            else if (sourceCtx.TIME() != null) sourceKind = AcceptSourceKind.Time;
            else if (sourceCtx.DAY_OF_WEEK() != null) sourceKind = Runtime.AcceptSourceKind.DayOfWeek;
            else if (sourceCtx.DAY() != null && sourceCtx.YYYYDDD() != null)
                sourceKind = AcceptSourceKind.DayYYYYDDD;
            else if (sourceCtx.DAY() != null) sourceKind = AcceptSourceKind.Day;
        }

        return new BoundAcceptStatement(boundTarget, sourceKind);
    }

    // ── INSPECT ──

    private BoundStatement? BindInspect(CobolParserCore.InspectStatementContext ctx)
    {
        var targetExpr = BindDataReferenceWithSubscripts(ctx.dataReference());
        if (targetExpr is not BoundIdentifierExpression targetId) return null;

        var tallying = new List<BoundInspectTallyingItem>();
        var replacing = new List<BoundInspectReplacingItem>();
        BoundInspectConverting? converting = null;

        var tallyPhrase = ctx.inspectTallyingPhrase();
        if (tallyPhrase != null)
        {
            foreach (var item in tallyPhrase.inspectTallyingItem())
            {
                var counterExpr = BindDataReferenceWithSubscripts(item.dataReference());
                if (counterExpr is not BoundIdentifierExpression counterId) continue;

                foreach (var forClause in item.inspectForClause())
                {
                    foreach (var countPhrase in forClause.inspectCountPhrase())
                    {
                        InspectTallyKind kind;
                        InspectPatternValue? pattern = null;

                        if (countPhrase.CHARACTERS() != null)
                        {
                            kind = InspectTallyKind.Characters;
                        }
                        else if (countPhrase.LEADING() != null)
                        {
                            kind = InspectTallyKind.Leading;
                            pattern = ExtractInspectPattern(countPhrase.inspectChar());
                        }
                        else
                        {
                            kind = InspectTallyKind.All;
                            pattern = ExtractInspectPattern(countPhrase.inspectChar());
                        }

                        var region = BindInspectDelimiters(countPhrase.inspectDelimiters());
                        tallying.Add(new BoundInspectTallyingItem(counterId, kind, pattern, region));
                    }
                }
            }
        }

        var replPhrase = ctx.inspectReplacingPhrase();
        if (replPhrase != null)
        {
            foreach (var item in replPhrase.inspectReplacingItem())
            {
                InspectReplaceKind kind;
                if (item.CHARACTERS() != null) kind = InspectReplaceKind.Characters;
                else if (item.FIRST() != null) kind = InspectReplaceKind.First;
                else if (item.LEADING() != null) kind = InspectReplaceKind.Leading;
                else kind = InspectReplaceKind.All;

                InspectPatternValue pattern;
                InspectPatternValue replacement;

                var inspChars = item.inspectChar();
                if (item.CHARACTERS() != null)
                {
                    pattern = InspectPatternValue.FromLiteral("");
                    replacement = inspChars.Length > 0
                        ? ExtractInspectPattern(inspChars[0]) ?? InspectPatternValue.FromLiteral("")
                        : InspectPatternValue.FromLiteral("");
                }
                else
                {
                    pattern = inspChars.Length > 0
                        ? ExtractInspectPattern(inspChars[0]) ?? InspectPatternValue.FromLiteral("")
                        : InspectPatternValue.FromLiteral("");
                    replacement = inspChars.Length > 1
                        ? ExtractInspectPattern(inspChars[1]) ?? InspectPatternValue.FromLiteral("")
                        : InspectPatternValue.FromLiteral("");
                }

                var region = BindInspectDelimiters(item.inspectDelimiters());
                replacing.Add(new BoundInspectReplacingItem(kind, pattern, replacement, region));
            }
        }

        var convPhrase = ctx.inspectConvertingPhrase();
        if (convPhrase != null)
        {
            var inspChars = convPhrase.inspectChar();
            var fromSet = inspChars.Length > 0
                ? ExtractInspectPattern(inspChars[0]) ?? InspectPatternValue.FromLiteral("")
                : InspectPatternValue.FromLiteral("");
            var toSet = inspChars.Length > 1
                ? ExtractInspectPattern(inspChars[1]) ?? InspectPatternValue.FromLiteral("")
                : InspectPatternValue.FromLiteral("");
            // CONVERTING uses inspectBeforeAfterPhrase*, map to BoundInspectRegion
            var region = BindInspectBeforeAfter(convPhrase.inspectBeforeAfterPhrase());
            converting = new BoundInspectConverting(fromSet, toSet, region);
        }

        var inspectStmt = new BoundInspectStatement(targetId, tallying, replacing, converting);
        ValidateInspectStatement(inspectStmt, ctx.Start?.Line ?? 0);
        return inspectStmt;
    }

    private InspectPatternValue? ExtractInspectPattern(CobolParserCore.InspectCharContext? ctx)
    {
        if (ctx == null) return null;
        if (ctx.literal() != null)
            return InspectPatternValue.FromLiteral(ExtractLiteralString(ctx.literal()));
        if (ctx.dataReference() != null)
        {
            var bound = BindDataReferenceWithSubscripts(ctx.dataReference());
            if (bound is BoundIdentifierExpression idExpr)
                return InspectPatternValue.FromDataRef(idExpr);
            // Fallback: unresolved identifier → use name as literal
            return InspectPatternValue.FromLiteral(ctx.dataReference().IDENTIFIER().GetText());
        }
        if (ctx.figurativeConstant() != null)
        {
            if (ctx.figurativeConstant().SPACE() != null) return InspectPatternValue.FromLiteral(" ");
            if (ctx.figurativeConstant().ZERO() != null) return InspectPatternValue.FromLiteral("0");
            if (ctx.figurativeConstant().HIGH_VALUE() != null) return InspectPatternValue.FromLiteral("\xFF");
            if (ctx.figurativeConstant().LOW_VALUE() != null) return InspectPatternValue.FromLiteral("\x00");
            if (ctx.figurativeConstant().QUOTE_() != null) return InspectPatternValue.FromLiteral("\"");
            return InspectPatternValue.FromLiteral(ctx.figurativeConstant().GetText());
        }
        return null;
    }

    /// <summary>
    private BoundInspectRegion BindInspectBeforeAfter(
        CobolParserCore.InspectBeforeAfterPhraseContext[]? phrases)
    {
        if (phrases == null || phrases.Length == 0)
            return BoundInspectRegion.Empty;

        InspectPatternValue? beforePattern = null;
        bool beforeInitial = false;
        InspectPatternValue? afterPattern = null;
        bool afterInitial = false;

        foreach (var p in phrases)
        {
            if (p.BEFORE() != null)
            {
                beforePattern = ExtractInspectPattern(p.inspectChar());
                beforeInitial = p.INITIAL_() != null;
            }
            else if (p.AFTER() != null)
            {
                afterPattern = ExtractInspectPattern(p.inspectChar());
                afterInitial = p.INITIAL_() != null;
            }
        }

        return new BoundInspectRegion(beforePattern, beforeInitial, afterPattern, afterInitial);
    }

    private string ExtractStringValue(
        CobolParserCore.DataReferenceContext[]? ids,
        CobolParserCore.LiteralContext[]? lits)
    {
        // Return the first available literal or identifier text
        if (lits != null && lits.Length > 0) return ExtractLiteralString(lits[0]);
        if (ids != null && ids.Length > 0) return ids[0].GetText();
        return "";
    }

    private string ExtractNthStringValue(
        CobolParserCore.DataReferenceContext[]? ids,
        CobolParserCore.LiteralContext[]? lits,
        int n)
    {
        // Combine identifiers and literals in parse order, pick nth
        // For simplicity: literals first, then identifiers
        var all = new List<string>();
        if (ids != null) foreach (var id in ids) all.Add(id.GetText());
        if (lits != null) foreach (var lit in lits) all.Add(ExtractLiteralString(lit));

        // Actually need to respect parse order. Use child index ordering.
        // Simpler approach: just use the grammar structure.
        // For REPLACING: first id/lit pair is pattern, second is replacement
        // The grammar puts them as separate children: ALL <id|lit> BY <id|lit>
        // So we need ordered extraction.
        var ordered = new List<(int index, string value)>();
        if (ids != null)
            foreach (var id in ids)
                ordered.Add((id.SourceInterval.a, id.GetText()));
        if (lits != null)
            foreach (var lit in lits)
                ordered.Add((lit.SourceInterval.a, ExtractLiteralString(lit)));
        ordered.Sort((a, b) => a.index.CompareTo(b.index));

        return n < ordered.Count ? ordered[n].value : "";
    }

    private string ExtractLiteralString(CobolParserCore.LiteralContext lit)
    {
        var nonNum = lit.nonNumericLiteral();
        if (nonNum != null)
        {
            // Handle figurative constants (SPACE, ZERO, etc.) inside nonNumericLiteral
            var fig = nonNum.figurativeConstant();
            if (fig != null)
            {
                if (fig.SPACE() != null) return " ";
                if (fig.ZERO() != null) return "0";
                if (fig.HIGH_VALUE() != null) return "\xFF";
                if (fig.LOW_VALUE() != null) return "\x00";
                if (fig.QUOTE_() != null) return "\"";
                // ALL "literal" — extract the literal string
                if (fig.STRINGLIT() != null)
                {
                    string raw = fig.STRINGLIT().GetText();
                    if (raw.Length >= 2) return raw[1..^1];
                }
                return fig.GetText();
            }

            string text = nonNum.GetText();
            if (text.Length >= 2 &&
                ((text[0] == '"' && text[^1] == '"') ||
                 (text[0] == '\'' && text[^1] == '\'')))
            {
                char q = text[0];
                return text[1..^1].Replace(new string(q, 2), new string(q, 1));
            }
            return text;
        }
        return lit.GetText();
    }

    private BoundInspectRegion BindInspectDelimiters(CobolParserCore.InspectDelimitersContext? ctx)
    {
        if (ctx == null) return BoundInspectRegion.Empty;

        InspectPatternValue? beforePattern = null;
        bool beforeInitial = false;
        InspectPatternValue? afterPattern = null;
        bool afterInitial = false;

        // Grammar: BEFORE INITIAL? inspectChar (AFTER INITIAL? inspectChar)?
        //        | AFTER INITIAL? inspectChar (BEFORE INITIAL? inspectChar)?
        var chars = ctx.inspectChar();
        var initials = ctx.INITIAL_();

        if (ctx.BEFORE() != null && ctx.AFTER() != null)
        {
            // Both present — first matches the leading keyword
            if (ctx.BEFORE().Symbol.TokenIndex < ctx.AFTER().Symbol.TokenIndex)
            {
                beforePattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
                beforeInitial = initials.Length > 0;
                afterPattern = chars.Length > 1 ? ExtractInspectPattern(chars[1]) : null;
                afterInitial = initials.Length > 1;
            }
            else
            {
                afterPattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
                afterInitial = initials.Length > 0;
                beforePattern = chars.Length > 1 ? ExtractInspectPattern(chars[1]) : null;
                beforeInitial = initials.Length > 1;
            }
        }
        else if (ctx.BEFORE() != null)
        {
            beforePattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
            beforeInitial = initials.Length > 0;
        }
        else if (ctx.AFTER() != null)
        {
            afterPattern = chars.Length > 0 ? ExtractInspectPattern(chars[0]) : null;
            afterInitial = initials.Length > 0;
        }

        return new BoundInspectRegion(beforePattern, beforeInitial, afterPattern, afterInitial);
    }

    // ── SEARCH ──

    private BoundStatement? BindSearch(CobolParserCore.SearchStatementContext ctx)
    {
        var dataRefs = ctx.dataReference();
        var tableExpr = BindDataReferenceWithSubscripts(dataRefs[0]);
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind VARYING identifier (second dataReference, if present)
        BoundIdentifierExpression? varyingExpr = null;
        if (dataRefs.Length > 1)
        {
            var varyBound = BindDataReferenceWithSubscripts(dataRefs[1]);
            if (varyBound is BoundIdentifierExpression varyId)
                varyingExpr = varyId;
        }

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        foreach (var whenCtx in ctx.searchWhenClause())
        {
            var cond = BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            foreach (var imp in whenCtx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) stmts.Add(bound);
                }
            whens.Add(new BoundSearchWhenClause(cond, stmts));
        }

        // Bind AT END
        var atEnd = new List<BoundStatement>();
        if (ctx.searchAtEndClause() is { } atEndCtx)
        {
            foreach (var imp in atEndCtx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index: find the first subscript used on a table element in WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        var searchStmt = new BoundSearchStatement(tableId, index, varyingExpr, whens, atEnd);
        ValidateSearchStatement(searchStmt, ctx.Start?.Line ?? 0);
        return searchStmt;
    }

    private BoundStatement? BindSearchAll(CobolParserCore.SearchAllStatementContext ctx)
    {
        var tableExpr = BindDataReferenceWithSubscripts(ctx.dataReference());
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        foreach (var whenCtx in ctx.searchAllWhenClause())
        {
            var cond = BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            foreach (var imp in whenCtx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) stmts.Add(bound);
                }
            whens.Add(new BoundSearchWhenClause(cond, stmts));
        }

        // Bind AT END
        var atEnd = new List<BoundStatement>();
        if (ctx.searchAtEndClause() is { } atEndCtx)
        {
            foreach (var imp in atEndCtx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index from WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        var searchAllStmt = new BoundSearchAllStatement(tableId, index, whens, atEnd);
        ValidateSearchAllStatement(searchAllStmt, ctx.Start?.Line ?? 0);
        return searchAllStmt;
    }

    /// <summary>
    /// Walk WHEN conditions to find the subscript variable used on the table.
    /// For WHEN TBL(IDX) = 3, extracts IDX as the search index.
    /// </summary>
    private BoundIdentifierExpression? ExtractSearchIndex(
        DataSymbol tableSymbol, List<BoundSearchWhenClause> whens)
    {
        foreach (var when in whens)
        {
            var idx = FindSubscriptOnTable(tableSymbol, when.Condition);
            if (idx != null) return idx;
        }
        return null;
    }

    private BoundIdentifierExpression? FindSubscriptOnTable(
        DataSymbol tableSymbol, BoundExpression expr)
    {
        switch (expr)
        {
            case BoundIdentifierExpression id when id.IsSubscripted:
                if (IsTableElement(id.Symbol, tableSymbol))
                {
                    // Find which subscript corresponds to the SEARCH table's dimension.
                    // Walk the parent chain collecting OCCURS levels (outermost first).
                    // Subscripts are positional: subscripts[0]→outermost, subscripts[N-1]→innermost.
                    // The SEARCH table's position in the OCCURS list gives the subscript index.
                    var occursLevels = new List<DataSymbol>();
                    var current = id.Symbol;
                    while (current != null)
                    {
                        if (current.Occurs != null)
                            occursLevels.Insert(0, current);
                        current = current.Parent;
                    }

                    for (int i = 0; i < occursLevels.Count && i < id.Subscripts!.Count; i++)
                    {
                        if (occursLevels[i] == tableSymbol
                            && id.Subscripts[i] is BoundIdentifierExpression subId)
                            return subId;
                    }
                }
                break;

            case BoundBinaryExpression bin:
                return FindSubscriptOnTable(tableSymbol, bin.Left)
                    ?? FindSubscriptOnTable(tableSymbol, bin.Right);

            case BoundReferenceModificationExpression refMod:
                return FindSubscriptOnTable(tableSymbol, refMod.Base);
        }
        return null;
    }

    /// <summary>
    /// Check if sym is the table symbol itself, or a child (subordinate) of it.
    /// For OCCURS groups like TBL containing VAL and FLAG children,
    /// a reference to VAL(IDX) should still identify IDX as the table index.
    /// </summary>
    private static bool IsTableElement(DataSymbol sym, DataSymbol tableSymbol)
    {
        var current = sym;
        while (current != null)
        {
            if (current == tableSymbol) return true;
            current = current.Parent;
        }
        return false;
    }

    // ── STRING ──

    private BoundStatement? BindString(CobolParserCore.StringStatementContext ctx)
    {
        var sendings = new List<BoundStringSending>();
        foreach (var phrase in ctx.stringSendingPhrase())
        {
            // Grammar: (identifier | literal | figurativeConstant) delimitedByPhrase?
            BoundExpression value;
            if (phrase.dataReference() is { } valId)
                value = BindDataReferenceWithSubscripts(valId);
            else if (phrase.literal() is { } valLit)
                value = BindLiteral(valLit);
            else if (phrase.figurativeConstant() is { } valFig)
                value = BindFigurativeConstantExpression(valFig);
            else
                continue;

            BoundExpression? delimiter = null;
            bool delimitedBySize = false;

            if (phrase.delimitedByPhrase() is { } delim)
            {
                if (delim.SIZE() != null)
                {
                    delimitedBySize = true;
                }
                else if (delim.dataReference() is { } delimId)
                {
                    delimiter = BindDataReferenceWithSubscripts(delimId);
                }
                else if (delim.literal() is { } delimLit)
                {
                    delimiter = BindLiteral(delimLit);
                }
                else if (delim.figurativeConstant() is { } delimFig)
                {
                    delimiter = BindFigurativeConstantExpression(delimFig);
                }
            }

            sendings.Add(new BoundStringSending(value, delimiter, delimitedBySize));
        }

        // INTO
        var intoPhrase = ctx.stringIntoPhrase();
        if (intoPhrase == null) return null;
        var intoExpr = BindDataReferenceWithSubscripts(intoPhrase.dataReference());

        // POINTER
        BoundExpression? pointer = null;
        if (ctx.stringWithPointer() is { } ptrCtx)
            pointer = BindDataReferenceWithSubscripts(ptrCtx.dataReference());

        // ON OVERFLOW / NOT ON OVERFLOW
        var onOverflow = new List<BoundStatement>();
        var notOnOverflow = new List<BoundStatement>();
        if (ctx.stringOnOverflow() is { } ovCtx)
        {
            var impStmts = ovCtx.statementBlock();
            if (impStmts.Length >= 1)
            {
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onOverflow.Add(bound);
                }
            }
            if (impStmts.Length >= 2)
            {
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnOverflow.Add(bound);
                }
            }
        }

        var stringStmt = new BoundStringStatement(sendings, intoExpr, pointer, onOverflow, notOnOverflow);
        ValidateStringStatement(stringStmt, ctx.Start?.Line ?? 0);
        return stringStmt;
    }

    // ── UNSTRING ──

    private BoundStatement? BindUnstring(CobolParserCore.UnstringStatementContext ctx)
    {
        // Source identifier
        var sourceExpr = BindDataReferenceWithSubscripts(ctx.dataReference());

        // DELIMITED BY phrase (optional) — supports OR-separated delimiters
        var delimiterItems = new List<(BoundExpression Expr, bool IsAll)>();
        if (ctx.unstringDelimiterPhrase() is { } delimCtx)
        {
            foreach (var item in delimCtx.unstringDelimiterItem())
            {
                bool itemAll = item.ALL() != null;
                BoundExpression itemExpr;
                if (item.dataReference() is { } delimId)
                    itemExpr = BindDataReferenceWithSubscripts(delimId);
                else if (item.literal() is { } delimLit)
                    itemExpr = BindLiteral(delimLit);
                else
                    itemExpr = BindFigurativeConstantExpression(item.figurativeConstant());
                delimiterItems.Add((itemExpr, itemAll));
            }
        }
        // For backwards compatibility, expose first delimiter as primary
        BoundExpression? delimiter = delimiterItems.Count > 0 ? delimiterItems[0].Expr : null;
        bool delimitedByAll = delimiterItems.Count > 0 && delimiterItems[0].IsAll;

        // INTO phrases (one or more)
        var intos = new List<BoundUnstringInto>();
        foreach (var intoPhrase in ctx.unstringIntoPhrase())
        {
            foreach (var target in intoPhrase.unstringIntoTarget())
            {
                var identifiers = target.dataReference();
                int idIdx = 0;

                // First identifier is the INTO target
                var targetExpr = BindDataReferenceWithSubscripts(identifiers[idIdx++]);

                // DELIMITER IN (optional)
                BoundExpression? delimiterIn = null;
                if (target.DELIMITER() != null && idIdx < identifiers.Length)
                    delimiterIn = BindDataReferenceWithSubscripts(identifiers[idIdx++]);

                // COUNT IN (optional)
                BoundExpression? countIn = null;
                if (target.COUNT() != null && idIdx < identifiers.Length)
                    countIn = BindDataReferenceWithSubscripts(identifiers[idIdx++]);

                intos.Add(new BoundUnstringInto(targetExpr, countIn, delimiterIn));
            }
        }

        // WITH POINTER (optional)
        BoundExpression? pointer = null;
        if (ctx.unstringWithPointer() is { } ptrCtx)
            pointer = BindDataReferenceWithSubscripts(ptrCtx.dataReference());

        // TALLYING IN (optional)
        BoundExpression? tallying = null;
        if (ctx.unstringTallying() is { } tallyCtx)
            tallying = BindDataReferenceWithSubscripts(tallyCtx.dataReference());

        // ON OVERFLOW / NOT ON OVERFLOW
        var onOverflow = new List<BoundStatement>();
        var notOnOverflow = new List<BoundStatement>();
        if (ctx.unstringOnOverflow() is { } ovCtx)
        {
            var impStmts = ovCtx.statementBlock();
            if (impStmts.Length >= 1)
            {
                foreach (var stmt in impStmts[0].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) onOverflow.Add(bound);
                }
            }
            if (impStmts.Length >= 2)
            {
                foreach (var stmt in impStmts[1].statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) notOnOverflow.Add(bound);
                }
            }
        }

        var unstringStmt = new BoundUnstringStatement(sourceExpr, delimiter, delimitedByAll,
            intos, pointer, tallying, onOverflow, notOnOverflow);
        ValidateUnstringStatement(unstringStmt, ctx.Start?.Line ?? 0);
        return unstringStmt;
    }

    // ── SET ──

    private BoundStatement? BindSet(CobolParserCore.SetStatementContext ctx)
    {
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

    private BoundStatement? BindSetBoolean(CobolParserCore.SetBooleanStatementContext ctx)
    {
        bool setToTrue = ctx.TRUE_() != null;
        var stmts = new List<BoundStatement>();

        foreach (var idCtx in ctx.dataReference())
        {
            string name = idCtx.IDENTIFIER().GetText();
            var condSym = _semantic.ResolveConditionName(name);
            if (condSym != null)
                stmts.Add(new BoundSetConditionStatement(condSym, setToTrue));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    private BoundStatement? BindSetToValue(CobolParserCore.SetToValueStatementContext ctx)
    {
        var identifiers = ctx.dataReference();
        var valueExpr = BindArithmeticExpr(ctx.arithmeticExpression());
        if (valueExpr == null) return null;

        var stmts = new List<BoundStatement>();
        foreach (var idCtx in identifiers)
        {
            // Check if it's a condition name first
            string name = idCtx.IDENTIFIER().GetText();
            var condSym = _semantic.ResolveConditionName(name);
            if (condSym != null)
            {
                stmts.Add(new BoundSetConditionStatement(condSym, true));
                continue;
            }

            // Regular data item: SET identifier TO value
            var targetId = BindDataReferenceWithSubscripts(idCtx);
            if (targetId is not BoundIdentifierExpression boundTarget) continue;
            stmts.Add(new BoundSetIndexStatement(boundTarget, SetOperation.Assign, valueExpr));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    private BoundStatement? BindSetIndex(CobolParserCore.SetIndexStatementContext ctx)
    {
        var op = ctx.UP() != null ? SetOperation.UpBy : SetOperation.DownBy;
        var deltaExpr = BindArithmeticExpr(ctx.arithmeticExpression());
        if (deltaExpr == null) return null;

        var stmts = new List<BoundStatement>();
        foreach (var idCtx in ctx.dataReference())
        {
            var targetId = BindDataReferenceWithSubscripts(idCtx);
            if (targetId is not BoundIdentifierExpression boundTarget) continue;
            stmts.Add(new BoundSetIndexStatement(boundTarget, op, deltaExpr));
        }

        if (stmts.Count == 0) return null;
        if (stmts.Count == 1) return stmts[0];
        return new BoundCompoundStatement(stmts);
    }

    // ── INITIALIZE ──

    private BoundStatement? BindInitialize(CobolParserCore.InitializeStatementContext ctx)
    {
        var targets = new List<DataSymbol>();
        var idList = ctx.dataReferenceList();
        if (idList == null) return null;

        foreach (var idCtx in idList.dataReference())
        {
            var sym = _semantic.ResolveData(idCtx.IDENTIFIER().GetText());
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

    private InitializeCategory ClassifyReplacingItem(CobolParserCore.InitializeReplacingItemContext ctx)
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

    private BoundExpression? BindReplacingValue(CobolParserCore.InitializeReplacingItemContext ctx)
    {
        var litCtx = ctx.literal();
        if (litCtx != null) return BindLiteral(litCtx);

        var idCtx = ctx.dataReference();
        if (idCtx != null)
        {
            var sym = _semantic.ResolveData(idCtx.IDENTIFIER().GetText());
            if (sym != null) return new BoundIdentifierExpression(sym, sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric);
        }

        return null;
    }

    // ── MULTIPLY ──

    private BoundStatement BindMultiply(CobolParserCore.MultiplyStatementContext ctx)
    {
        var operand = BindSimpleOperand(ctx.multiplyOperand());

        var byCtxs = ctx.multiplyByOperand();
        if (byCtxs.Length == 0)
            throw new InvalidOperationException(
                $"MULTIPLY statement has no BY operand (line {ctx.Start?.Line})");

        // First BY operand is always the second factor
        var firstByReceiver = byCtxs[0].receivingOperand();
        BoundExpression byOperand;
        if (firstByReceiver.dataReference() != null)
            byOperand = BindDataReferenceWithSubscripts(firstByReceiver.dataReference());
        else
            byOperand = BindLiteral(firstByReceiver.literal());

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
                    var sym = BindDataReferenceWithSubscripts(receiver.dataReference());
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

    private BoundStatement BindAdd(CobolParserCore.AddStatementContext ctx)
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
            operands.Add(BindSimpleOperand(op));

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

    private BoundStatement BindSubtract(CobolParserCore.SubtractStatementContext ctx)
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
            operands.Add(BindSimpleOperand(op));

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
                    givingMinuend = BindReceivingOperand(fromOperand.receivingOperand());
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
            var byOperand = ctx.divideByPhrase().divideOperand();
            dividend = firstOperand;
            firstOperand = BindSimpleOperand(byOperand);

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
                    intoLiteral = BindLiteral(intoOp.literal());
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
            var remExpr = BindDataReferenceWithSubscripts(remPhrase.dataReference());
            remainderTarget = remExpr as BoundIdentifierExpression;
        }

        var sizeError = BindSizeErrorClause(ctx.arithmeticOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Divide, new[] { firstOperand }, dividend, targets,
            isGiving: dividend != null, isByForm: isByForm, remainderTarget: remainderTarget, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
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
            var sym = BindDataReferenceWithSubscripts(s.dataReference());
            if (sym is BoundIdentifierExpression boundS)
                targets.Add(new BoundArithmeticTarget(boundS, s.ROUNDED() != null));
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"COMPUTE statement has no valid targets or operands (line {ctx.Start?.Line})");

        // Bind the full arithmetic expression (recursive tree walk)
        var expr = BindAdditiveExpression(ctx.arithmeticExpression().additiveExpression());

        var sizeError = BindSizeErrorClause(ctx.computeOnSizeError());
        return ValidatedArithmetic(ArithmeticKind.Compute, new[] { expr }, null, targets, sizeError: sizeError, line: ctx.Start?.Line ?? 0);
    }

    /// <summary>
    /// Recursively bind an arithmetic expression tree for COMPUTE.
    /// Walks the parse tree: additiveExpression → multiplicativeExpression →
    /// powerExpression → unaryExpression → primaryExpression.
    /// </summary>
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

        if (ctx.dataReference() != null)
        {
            return BindDataReferenceWithSubscripts(ctx.dataReference());
        }

        if (ctx.arithmeticExpression() != null)
            return BindAdditiveExpression(ctx.arithmeticExpression().additiveExpression());

        // Intrinsic function call (1989 Amendment)
        if (ctx.functionCall() != null)
        {
            return BindFunctionCall(ctx.functionCall());
        }

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── FUNCTION CALL ──

    /// <summary>
    /// Intrinsic function result categories per ISO/IEC 1989:2023 §15.
    /// String functions return Alphanumeric, everything else returns Numeric.
    /// </summary>
    private static readonly HashSet<string> _alphanumericFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "LOWER-CASE", "UPPER-CASE", "REVERSE", "TRIM", "CONCATENATE",
        "SUBSTITUTE", "CHAR", "CURRENT-DATE", "WHEN-COMPILED"
    };

    private BoundExpression BindFunctionCall(CobolParserCore.FunctionCallContext ctx)
    {
        // FUNCTION functionName subscriptPart? — the function name comes from the
        // functionName rule (IDENTIFIER or a reserved-word alternative like SIGN/SUM/RANDOM).
        // Arguments (if any) are captured as subscriptPart tokens by the SUBSCRIPT lexer mode.
        var funcName = ctx.functionName()?.GetText() ?? "UNKNOWN";

        var args = new List<BoundExpression>();
        var subPart = ctx.subscriptPart();
        if (subPart != null)
        {
            var subOrRefMod = subPart.subscriptOrRefMod();
            if (subOrRefMod != null)
            {
                // Reuse the subscript token interpreter — it splits comma-separated
                // expressions which is exactly what function arguments are.
                var (subExprs, _) = InterpretSubscriptTokens(subOrRefMod);
                args.AddRange(subExprs);
            }
        }

        // FUNCTION LENGTH returns the defined size of the operand, not its content length.
        // Per ISO §15.24: "the value returned is the number of character positions
        // in argument-1". Resolved at bind time — no runtime call needed.
        if (funcName.Equals("LENGTH", StringComparison.OrdinalIgnoreCase) && args.Count == 1)
        {
            decimal lengthValue = 0;
            if (args[0] is BoundIdentifierExpression idExpr)
                lengthValue = idExpr.Symbol.ElementSize;
            else if (args[0] is BoundLiteralExpression litExpr && litExpr.Value is string s)
                lengthValue = s.Length;
            else if (args[0] is BoundLiteralExpression numLit && numLit.Value is decimal d)
                lengthValue = d; // already a number (e.g., from nested function)
            return new BoundLiteralExpression(lengthValue, CobolCategory.Numeric);
        }

        var category = _alphanumericFunctions.Contains(funcName)
            ? CobolCategory.Alphanumeric
            : CobolCategory.Numeric;

        return new BoundFunctionCallExpression(funcName, args.AsReadOnly(), category);
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
        var impStmts = ctx.statementBlock();
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
        var procNames = ctx.procedureName();

        // Bare GO TO (no target) — target set by ALTER at runtime
        if (procNames == null || procNames.Length == 0)
        {
            if (_options.IsCobol2002OrLater)
            {
                _diagnostics.Report(DiagnosticDescriptors.CBL3605,
                    MakeLocation(ctx), MakeSpan(ctx), _options.DialectName);
                return null;
            }
            _diagnostics.Report(DiagnosticDescriptors.CBL3606,
                MakeLocation(ctx), MakeSpan(ctx));
            return new BoundGoToStatement([]);
        }

        var targets = new List<ParagraphSymbol>();

        foreach (var pn in procNames)
        {
            string name = ExtractProcedureNameText(pn);
            var paraSym = ResolveProcedureName(name);
            if (paraSym != null) targets.Add(paraSym);
        }

        if (targets.Count == 0) return null;

        // DEPENDING ON identifier (optional)
        BoundIdentifierExpression? dependingOn = null;
        if (ctx.DEPENDING() != null && ctx.dataReference() != null)
        {
            var depExpr = BindDataReferenceWithSubscripts(ctx.dataReference());
            dependingOn = depExpr as BoundIdentifierExpression;
        }

        return new BoundGoToStatement(targets, dependingOn);
    }

    // ── ALTER ──

    private BoundAlterStatement? BindAlter(CobolParserCore.AlterStatementContext ctx)
    {
        // Dialect check: ALTER deleted from COBOL-2002+
        if (_options.IsCobol2002OrLater)
        {
            _diagnostics.Report(DiagnosticDescriptors.CBL3601,
                MakeLocation(ctx), MakeSpan(ctx), _options.DialectName);
            return null;
        }

        // Obsolete warning in COBOL-85 / Default mode
        _diagnostics.Report(DiagnosticDescriptors.CBL3602,
            MakeLocation(ctx), MakeSpan(ctx));

        var entries = new List<BoundAlterEntry>();
        foreach (var entry in ctx.alterEntry())
        {
            var procNames = entry.procedureName();
            if (procNames.Length < 2) continue;

            string targetName = ExtractProcedureNameText(procNames[0]);
            string destName = ExtractProcedureNameText(procNames[1]);

            var targetSym = ResolveProcedureName(targetName);
            var destSym = ResolveProcedureName(destName);

            if (targetSym == null)
            {
                _diagnostics.Report(DiagnosticDescriptors.CBL3603,
                    MakeLocation(entry), MakeSpan(entry), targetName);
                continue;
            }
            if (destSym == null)
            {
                _diagnostics.Report(DiagnosticDescriptors.CBL3603,
                    MakeLocation(entry), MakeSpan(entry), destName);
                continue;
            }

            entries.Add(new BoundAlterEntry(targetSym, destSym));
        }

        return entries.Count > 0 ? new BoundAlterStatement(entries) : null;
    }

    // ── CANCEL ──

    private BoundCancelStatement BindCancel(CobolParserCore.CancelStatementContext ctx)
    {
        var names = new List<string>();
        foreach (var target in ctx.cancelTarget())
        {
            if (target.literal() is { } lit)
                names.Add(lit.GetText().Trim('"', '\''));
            else if (target.dataReference() is { } dr)
                names.Add(dr.IDENTIFIER().GetText());
        }
        return new BoundCancelStatement(names);
    }

    // ── ENTRY ──

    private BoundEntryStatement? BindEntry(CobolParserCore.EntryStatementContext ctx)
    {
        string entryName = ctx.literal().GetText().Trim('"', '\'');

        var usingNames = new List<string>();
        if (ctx.usingClause() is { } usingCtx)
        {
            var dataRefs = usingCtx.dataReferenceList()?.dataReference();
            if (dataRefs != null)
            {
                foreach (var dr in dataRefs)
                    usingNames.Add(dr.IDENTIFIER().GetText());
            }
        }

        return new BoundEntryStatement(entryName, usingNames);
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
        var normalized = SemanticBuilder.NormalizeNumericLiteralText(numLit);
        var originalText = numLit.GetText();
        if (decimal.TryParse(normalized, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric, originalText: originalText);
        return new BoundLiteralExpression(originalText, CobolCategory.Alphanumeric);
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
            {
                char quoteChar = text[0];
                text = text[1..^1];
                // Un-escape doubled quotes: "" → " (ISO §8.3.1.2)
                text = text.Replace(new string(quoteChar, 2), new string(quoteChar, 1));
            }
            return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
        }

        var figCtx = nonNum.figurativeConstant();
        if (figCtx != null)
            return BindFigurativeConstantExpression(figCtx);

        // HEXLIT, etc.
        return new BoundLiteralExpression(nonNum.GetText(), CobolCategory.Alphanumeric);
    }

    private BoundExpression BindFigurativeConstantExpression(CobolParserCore.FigurativeConstantContext figCtx)
    {
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
                var raw = allHex.GetText();
                if (raw.Length >= 3)
                {
                    var hexBody = raw[2..^1];
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i + 1 < hexBody.Length; i += 2)
                        sb.Append((char)Convert.ToByte(hexBody[i..(i + 2)], 16));
                    allText = sb.ToString();
                }
            }
            return new BoundFigurativeExpression(FigurativeKind.None, allText ?? "");
        }

        string figText = figCtx.GetText().ToUpperInvariant();
        return figText switch
        {
            "SPACE" or "SPACES" => new BoundFigurativeExpression(FigurativeKind.Space),
            "ZERO" or "ZEROS" or "ZEROES" => new BoundFigurativeExpression(FigurativeKind.Zero),
            "HIGH-VALUE" or "HIGH-VALUES" => new BoundFigurativeExpression(FigurativeKind.HighValue),
            "LOW-VALUE" or "LOW-VALUES" => new BoundFigurativeExpression(FigurativeKind.LowValue),
            "QUOTE" or "QUOTES" => new BoundFigurativeExpression(FigurativeKind.Quote),
            _ => new BoundLiteralExpression(figText, CobolCategory.Alphanumeric)
        };
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
        return ExpandAbbreviatedConditions(bound);
    }

    private BoundExpression BindLogicalOr(CobolParserCore.LogicalOrExpressionContext ctx)
    {
        // First child is always a logicalAndExpression
        var andExprs = ctx.logicalAndExpression();
        var result = BindLogicalAnd(andExprs[0]);

        // Iterate through children after the first logicalAndExpression,
        // matching OR tokens with their alternatives (logicalAndExpression or abbreviatedRelation)
        for (int i = 1; i < ctx.ChildCount; i++)
        {
            var child = ctx.GetChild(i);
            if (child is Antlr4.Runtime.Tree.ITerminalNode)
                continue; // skip OR tokens

            BoundExpression right;
            if (child is CobolParserCore.LogicalAndExpressionContext andCtx)
            {
                right = BindLogicalAnd(andCtx);
            }
            else if (child is CobolParserCore.AbbreviatedAndChainContext chainCtx)
            {
                right = BindAbbreviatedAndChain(chainCtx);
            }
            else
                continue;

            result = new BoundBinaryExpression(result,
                BoundBinaryOperatorKind.Or,
                right, CobolCategory.Unknown);
        }
        return result;
    }

    private BoundExpression BindLogicalAnd(CobolParserCore.LogicalAndExpressionContext ctx)
    {
        // First child is always a unaryLogicalExpression
        var notExprs = ctx.unaryLogicalExpression();
        var result = BindUnaryLogical(notExprs[0]);

        // Iterate through children after the first unaryLogicalExpression
        for (int i = 1; i < ctx.ChildCount; i++)
        {
            var child = ctx.GetChild(i);
            if (child is Antlr4.Runtime.Tree.ITerminalNode)
                continue; // skip AND tokens

            BoundExpression right;
            if (child is CobolParserCore.UnaryLogicalExpressionContext unaryCtx)
            {
                right = BindUnaryLogical(unaryCtx);
            }
            else if (child is CobolParserCore.AbbreviatedRelationContext abbrevCtx)
            {
                right = BindAbbreviatedRelation(abbrevCtx);
            }
            else
                continue;

            result = new BoundBinaryExpression(result,
                BoundBinaryOperatorKind.And,
                right, CobolCategory.Unknown);
        }
        return result;
    }

    /// <summary>
    /// Bind an abbreviated relational condition (COBOL-85 §6.3.4.2).
    /// An abbreviated relation has an optional operator and a comparisonOperand.
    /// The missing left operand (and possibly missing operator) will be filled in
    /// by RewriteAbbreviatedRelations after the entire condition is bound.
    /// </summary>
    private BoundExpression BindAbbreviatedRelation(CobolParserCore.AbbreviatedRelationContext ctx)
    {
        var operandCtx = ctx.comparisonOperand();
        var operatorCtx = ctx.comparisonOperator();

        var right = BindComparisonOperand(operandCtx);
        var op = ParseComparisonOperator(operatorCtx);

        // Use a sentinel BoundAbbreviatedExpression to mark this for rewriting.
        // The right operand is the value; the operator is parsed.
        // Left operand will be filled from context by RewriteAbbreviatedRelations.
        return new BoundAbbreviatedExpression(op, right);
    }

    /// <summary>
    /// Bind an abbreviated AND chain: one or more abbreviated relations connected by AND.
    /// Used after OR when abbreviated forms include AND chaining:
    ///   IF A = B OR = C AND = D  →  OR (= C AND = D)
    /// </summary>
    private BoundExpression BindAbbreviatedAndChain(CobolParserCore.AbbreviatedAndChainContext ctx)
    {
        var abbrevs = ctx.abbreviatedRelation();
        var result = BindAbbreviatedRelation(abbrevs[0]);
        for (int i = 1; i < abbrevs.Length; i++)
        {
            var right = BindAbbreviatedRelation(abbrevs[i]);
            result = new BoundBinaryExpression(result,
                BoundBinaryOperatorKind.And,
                right, CobolCategory.Unknown);
        }
        return result;
    }

    private BoundExpression BindUnaryLogical(CobolParserCore.UnaryLogicalExpressionContext ctx)
    {
        // NOT primaryCondition (non-recursive per COBOL-85 §6.3.4)
        if (ctx.NOT() != null && ctx.primaryCondition() is { } negated)
        {
            var inner = BindPrimaryCondition(negated);
            return new BoundBinaryExpression(inner, BoundBinaryOperatorKind.Not, inner, CobolCategory.Unknown);
        }

        var primary = ctx.primaryCondition();
        if (primary == null)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);

        return BindPrimaryCondition(primary);
    }

    private BoundExpression BindPrimaryCondition(CobolParserCore.PrimaryConditionContext primary)
    {
        // Sign condition: operand IS [NOT] POSITIVE/NEGATIVE/ZERO
        // Listed first in grammar — more specific than comparisonExpression
        if (primary.signCondition() is { } signCtx)
            return BindSignCondition(signCtx);

        // Comparison expression (relational, class condition, bare identifier)
        if (primary.comparisonExpression() is { } comp)
            return BindComparison(comp);

        // Boolean literal: TRUE/FALSE
        if (primary.booleanLiteral() is { } boolLit)
        {
            bool value = boolLit.TRUE_() != null;
            return new BoundLiteralExpression(value, CobolCategory.Unknown);
        }

        // Parenthesized condition: (condition)
        if (primary.condition() is { } parenCond)
            return BindCondition(parenCond);

        return new BoundLiteralExpression(true, CobolCategory.Unknown);
    }

    private BoundExpression BindSignCondition(CobolParserCore.SignConditionContext ctx)
    {
        var operandCtx = ctx.valueOperand();
        var subject = operandCtx != null
            ? BindValueOperand(operandCtx)
            : new BoundLiteralExpression(0m, CobolCategory.Numeric);

        bool isNegated = ctx.NOT() != null;
        var kind = ctx.POSITIVE() != null ? SignConditionKind.Positive
            : ctx.NEGATIVE() != null ? SignConditionKind.Negative
            : SignConditionKind.Zero;

        // Check 12: Sign condition requires a numeric operand (ISO §6.3.4.1)
        if (!subject.Category.IsNumericLike() && subject.Category != CobolCategory.Unknown)
        {
            var (loc, span) = DiagAt(ctx.Start?.Line ?? 0);
            _diagnostics.Report(DiagnosticDescriptors.CBL2606, loc, span);
        }

        return new BoundSignConditionExpression(subject, kind, isNegated);
    }

    private BoundExpression BindComparison(CobolParserCore.ComparisonExpressionContext ctx)
    {
        var operands = ctx.comparisonOperand();
        var relOp = ctx.comparisonOperator();
        var classNameCtx = ctx.className();

        // Class condition: operand IS? NOT? className
        if (classNameCtx != null && operands.Length >= 1)
        {
            var subject = BindComparisonOperand(operands[0]);
            bool isNegated = ctx.NOT() != null;
            var classText = classNameCtx.GetText().ToUpperInvariant();
            ClassConditionKind? kind = classText switch
            {
                "NUMERIC" => ClassConditionKind.Numeric,
                "ALPHABETIC" => ClassConditionKind.Alphabetic,
                "ALPHABETIC-LOWER" => ClassConditionKind.AlphabeticLower,
                "ALPHABETIC-UPPER" => ClassConditionKind.AlphabeticUpper,
                _ => null
            };
            if (kind == null)
            {
                // Check for user-defined CLASS from SPECIAL-NAMES
                var classDef = _semantic.ResolveClassDefinition(classText);
                if (classDef != null)
                    return new BoundUserClassConditionExpression(subject, classDef, isNegated);

                _diagnostics.Report(DiagnosticDescriptors.COBOL0413,
                    Common.SourceLocation.None, Common.TextSpan.Empty, classNameCtx.GetText());
                return new BoundLiteralExpression(false, CobolCategory.Unknown);
            }
            return new BoundClassConditionExpression(subject, kind.Value, isNegated);
        }

        if (operands.Length == 0)
            return new BoundLiteralExpression(true, CobolCategory.Unknown);

        var left = BindComparisonOperand(operands[0]);

        if (operands.Length < 2 || relOp == null)
        {
            // Check if bare identifier is a level-88 condition name or switch condition
            string? condNameStr = null;
            if (left is BoundIdentifierExpression idExpr)
                condNameStr = idExpr.Symbol.Name;
            else if (left is BoundLiteralExpression litExpr && litExpr.Value is string s)
                condNameStr = s;

            if (condNameStr != null)
            {
                var condSym = _semantic.ResolveConditionName(condNameStr);
                if (condSym != null)
                    return new BoundConditionNameExpression(condSym);

                var swCond = _semantic.ResolveSwitchCondition(condNameStr);
                if (swCond != null)
                    return new BoundSwitchConditionExpression(swCond.Value.Switch, swCond.Value.IsOn);
            }
            // Bare expression: IF A (means A <> 0 for numeric, A <> SPACE for alpha)
            return left;
        }

        var right = BindComparisonOperand(operands[1]);
        var op = ParseComparisonOperator(relOp);

        return new BoundBinaryExpression(left, op, right, CobolCategory.Unknown);
    }

    /// <summary>
    /// Parse a comparison operator context into a BoundBinaryOperatorKind.
    /// Shared by BindComparison and BindAbbreviatedRelation.
    /// </summary>
    private static BoundBinaryOperatorKind ParseComparisonOperator(CobolParserCore.ComparisonOperatorContext relOp)
    {
        string opText = relOp.GetText().ToUpperInvariant()
            .Replace("IS", "").Replace("TO", "").Replace("THAN", "").Trim();
        return opText switch
        {
            "=" or "EQUAL" => BoundBinaryOperatorKind.Equal,
            "NOT=" or "NOTEQUAL" or "<>" => BoundBinaryOperatorKind.NotEqual,
            ">" or "GREATER" => BoundBinaryOperatorKind.Greater,
            ">=" or "GREATEROREQUAL" => BoundBinaryOperatorKind.GreaterOrEqual,
            "<" or "LESS" => BoundBinaryOperatorKind.Less,
            "<=" or "LESSOREQUAL" => BoundBinaryOperatorKind.LessOrEqual,
            "NOT>" => BoundBinaryOperatorKind.LessOrEqual,       // NOT > means <=
            "NOT<" => BoundBinaryOperatorKind.GreaterOrEqual,    // NOT < means >=
            "NOT>=" => BoundBinaryOperatorKind.Less,             // NOT >= means <
            "NOT<=" => BoundBinaryOperatorKind.Greater,          // NOT <= means >
            _ when opText.Contains("NOT") && opText.Contains("GREATER") => BoundBinaryOperatorKind.LessOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("LESS") => BoundBinaryOperatorKind.GreaterOrEqual,
            _ when opText.Contains("NOT") && opText.Contains("EQUAL") => BoundBinaryOperatorKind.NotEqual,
            _ when opText.Contains("EQUAL") => BoundBinaryOperatorKind.Equal,
            _ when opText.Contains("GREATER") => BoundBinaryOperatorKind.Greater,
            _ when opText.Contains("LESS") => BoundBinaryOperatorKind.Less,
            _ => BoundBinaryOperatorKind.Equal
        };
    }

    /// <summary>
    /// Negate a relational operator: = → ≠, > → ≤, etc.
    /// Used for abbreviated NOT conditions (IF A > B AND NOT < C).
    /// </summary>
    private static BoundBinaryOperatorKind NegateOperator(BoundBinaryOperatorKind op) => op switch
    {
        BoundBinaryOperatorKind.Equal => BoundBinaryOperatorKind.NotEqual,
        BoundBinaryOperatorKind.NotEqual => BoundBinaryOperatorKind.Equal,
        BoundBinaryOperatorKind.Greater => BoundBinaryOperatorKind.LessOrEqual,
        BoundBinaryOperatorKind.GreaterOrEqual => BoundBinaryOperatorKind.Less,
        BoundBinaryOperatorKind.Less => BoundBinaryOperatorKind.GreaterOrEqual,
        BoundBinaryOperatorKind.LessOrEqual => BoundBinaryOperatorKind.Greater,
        _ => op
    };

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
    /// Expand abbreviated combined relation conditions per COBOL-85 §6.3.4.2.
    /// Walks the bound expression tree top-down, maintaining (subject, operator)
    /// context from the most recently encountered relation condition. Bare operands
    /// and BoundAbbreviatedExpression nodes are expanded into full relationals.
    /// Condition-names, class, sign, and switch conditions are left untouched —
    /// they are complete simple conditions that do not participate in abbreviation.
    /// </summary>
    private static BoundExpression ExpandAbbreviatedConditions(BoundExpression root)
        => ExpandAbbrev(root, null, default);

    private static BoundExpression ExpandAbbrev(
        BoundExpression expr,
        BoundExpression? subject,
        BoundBinaryOperatorKind op)
    {
        // Already-resolved simple conditions: never participate in abbreviation
        if (expr is BoundConditionNameExpression
                or BoundSwitchConditionExpression
                or BoundClassConditionExpression
                or BoundUserClassConditionExpression
                or BoundSignConditionExpression)
            return expr;

        // Grammar-level abbreviated (operator + right operand, no subject)
        if (expr is BoundAbbreviatedExpression abbrev)
        {
            if (subject == null) return expr;
            return new BoundBinaryExpression(subject, abbrev.OperatorKind, abbrev.Right, CobolCategory.Unknown);
        }

        // Bare operand (identifier/literal/arithmetic not resolved as condition-name):
        // expand using inherited context if available
        if (expr is BoundIdentifierExpression or BoundLiteralExpression)
        {
            if (subject != null && IsRelational(op))
                return new BoundBinaryExpression(subject, op, expr, CobolCategory.Unknown);
            return expr;
        }

        if (expr is not BoundBinaryExpression bin)
            return expr;

        // Arithmetic expression used as abbreviated operand (e.g., IF A = B OR C - 1)
        if (IsArithmeticOp(bin.OperatorKind))
        {
            if (subject != null && IsRelational(op))
                return new BoundBinaryExpression(subject, op, expr, CobolCategory.Unknown);
            return expr;
        }

        // AND/OR: propagate context through children
        if (bin.OperatorKind is BoundBinaryOperatorKind.Or
                             or BoundBinaryOperatorKind.And)
        {
            var left = ExpandAbbrev(bin.Left, subject, op);

            // Extract relational context from expanded left for use by right
            var (newSubject, newOp) = ExtractContext(left);
            newSubject ??= subject;
            if (!IsRelational(newOp)) newOp = op;

            var right = ExpandAbbrev(bin.Right, newSubject, newOp);

            if (ReferenceEquals(left, bin.Left) && ReferenceEquals(right, bin.Right))
                return bin;
            return new BoundBinaryExpression(left, bin.OperatorKind, right, CobolCategory.Unknown);
        }

        // NOT: expand inner expression with inherited context
        if (bin.OperatorKind == BoundBinaryOperatorKind.Not)
        {
            var inner = ExpandAbbrev(bin.Left, subject, op);
            if (ReferenceEquals(inner, bin.Left))
                return bin;
            return new BoundBinaryExpression(inner, BoundBinaryOperatorKind.Not, inner, CobolCategory.Unknown);
        }

        // Relational: already a complete comparison — no expansion needed
        return bin;
    }

    /// <summary>
    /// Extract relational context (subject, operator) from an expanded expression.
    /// Used to carry subject/operator forward through AND/OR chains.
    /// </summary>
    private static (BoundExpression? Subject, BoundBinaryOperatorKind Op) ExtractContext(
        BoundExpression expr)
    {
        if (expr is BoundBinaryExpression bin)
        {
            if (IsRelational(bin.OperatorKind))
                return (bin.Left, bin.OperatorKind);

            // Look through NOT to find the inner relation
            if (bin.OperatorKind == BoundBinaryOperatorKind.Not)
                return ExtractContext(bin.Left);

            // For AND/OR chains, the rightmost child carries the most recent context
            if (bin.OperatorKind is BoundBinaryOperatorKind.And
                                or BoundBinaryOperatorKind.Or)
                return ExtractContext(bin.Right);
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

    private static bool IsArithmeticOp(BoundBinaryOperatorKind kind) =>
        kind is BoundBinaryOperatorKind.Add
            or BoundBinaryOperatorKind.Subtract
            or BoundBinaryOperatorKind.Multiply
            or BoundBinaryOperatorKind.Divide
            or BoundBinaryOperatorKind.Power;

    private BoundExpression BindComparisonOperand(CobolParserCore.ComparisonOperandContext ctx)
        => BindValueOperand(ctx.valueOperand());

    /// <summary>
    /// Bind ON SIZE ERROR / NOT ON SIZE ERROR clause shared by all arithmetic statements.
    /// Handles both forms: ON+NOT, ON-only, NOT-only.
    /// </summary>
    private BoundSizeErrorClause? BindSizeErrorClause(Antlr4.Runtime.ParserRuleContext? ctx)
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
    /// Construct and validate an arithmetic statement, emitting diagnostics for type violations.
    /// </summary>
    private BoundArithmeticStatement ValidatedArithmetic(
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
        var loc = new Common.SourceLocation("<source>", 0, line, 0);
        var span = Common.TextSpan.Empty;
        ArithmeticTypeSystem.ValidateArithmeticStatement(stmt, _diagnostics, loc, span);
        return stmt;
    }

    // ═══════════════════════════════════
    // Statement-level semantic validation
    // ═══════════════════════════════════

    private (Common.SourceLocation loc, Common.TextSpan span) DiagAt(int line)
        => (new Common.SourceLocation("<source>", 0, line, 0), Common.TextSpan.Empty);

    private void ValidateStringStatement(BoundStringStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // INTO must be alphanumeric or group
        if (stmt.Into is BoundIdentifierExpression intoId && !intoId.Symbol.IsGroup
            && intoId.Category != CobolCategory.Alphanumeric
            && intoId.Category != CobolCategory.AlphanumericEdited)
            _diagnostics.Report(DiagnosticDescriptors.CBL1301, loc, span);
        // POINTER must be integer numeric
        if (stmt.Pointer is BoundIdentifierExpression ptrId
            && !CategoryCompatibility.IsNumericFamily(ptrId.Category))
            _diagnostics.Report(DiagnosticDescriptors.CBL1304, loc, span);
    }

    private void ValidateUnstringStatement(BoundUnstringStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Source must be alphanumeric or group
        if (stmt.Source is BoundIdentifierExpression srcId && !srcId.Symbol.IsGroup
            && srcId.Category != CobolCategory.Alphanumeric
            && srcId.Category != CobolCategory.AlphanumericEdited)
            _diagnostics.Report(DiagnosticDescriptors.CBL1401, loc, span);
        // POINTER must be integer numeric
        if (stmt.Pointer is BoundIdentifierExpression ptrId
            && !CategoryCompatibility.IsNumericFamily(ptrId.Category))
            _diagnostics.Report(DiagnosticDescriptors.CBL1405, loc, span);
        // TALLYING must be integer numeric
        if (stmt.Tallying is BoundIdentifierExpression tallyId
            && !CategoryCompatibility.IsNumericFamily(tallyId.Category))
            _diagnostics.Report(DiagnosticDescriptors.CBL1406, loc, span);
    }

    private void ValidateInspectStatement(BoundInspectStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Target must be alphanumeric or group
        if (stmt.Target.Symbol.IsElementary
            && stmt.Target.Category != CobolCategory.Alphanumeric
            && stmt.Target.Category != CobolCategory.AlphanumericEdited)
            _diagnostics.Report(DiagnosticDescriptors.CBL1501, loc, span);
        // TALLYING counters must be integer numeric
        foreach (var item in stmt.Tallying)
        {
            if (!CategoryCompatibility.IsNumericFamily(item.Counter.Category))
                _diagnostics.Report(DiagnosticDescriptors.CBL1502, loc, span);
        }
    }

    private void ValidateSearchStatement(BoundSearchStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Table must have OCCURS
        if (stmt.Table.Symbol.Occurs == null)
            _diagnostics.Report(DiagnosticDescriptors.CBL1105, loc, span, stmt.Table.Symbol.DisplayName);
    }

    private void ValidateSearchAllStatement(BoundSearchAllStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Table must have OCCURS
        if (stmt.Table.Symbol.Occurs == null)
            _diagnostics.Report(DiagnosticDescriptors.CBL1202, loc, span, stmt.Table.Symbol.DisplayName);
        // Table must have KEY clause
        var occurs = stmt.Table.Symbol.Occurs;
        if (occurs != null && occurs.AscendingKeys.Count == 0 && occurs.DescendingKeys.Count == 0)
            _diagnostics.Report(DiagnosticDescriptors.CBL1204, loc, span, stmt.Table.Symbol.DisplayName);

        // Check 10: SEARCH ALL WHEN must be equality comparison on key fields
        foreach (var when in stmt.Whens)
        {
            if (!IsSearchAllEqualityCondition(when.Condition))
                _diagnostics.Report(DiagnosticDescriptors.CBL1206, loc, span);
        }
    }

    /// <summary>
    /// Returns true if the condition is a valid SEARCH ALL WHEN condition:
    /// a simple equality comparison, an AND of equality comparisons,
    /// or a condition-name (level-88) check.
    /// </summary>
    private static bool IsSearchAllEqualityCondition(BoundExpression condition)
    {
        return condition switch
        {
            BoundBinaryExpression { OperatorKind: BoundBinaryOperatorKind.Equal } => true,
            BoundBinaryExpression { OperatorKind: BoundBinaryOperatorKind.And } and_ =>
                IsSearchAllEqualityCondition(and_.Left) && IsSearchAllEqualityCondition(and_.Right),
            BoundConditionNameExpression => true, // Level-88 condition-name
            _ => false,
        };
    }

    /// <summary>
    /// Bind a givingReceiver (identifier | literal) — unified GIVING-form operand.
    /// </summary>
    private BoundExpression BindReceivingOperand(CobolParserCore.ReceivingOperandContext ctx)
    {
        if (ctx.dataReference() != null)
            return BindDataReferenceWithSubscripts(ctx.dataReference());
        if (ctx.literal() != null)
            return BindLiteral(ctx.literal());
        throw new InvalidOperationException("givingReceiver has neither identifier nor literal");
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
            if (addOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(addOp.dataReference());
            if (addOp.literal() != null)
                return BindLiteral(addOp.literal());
        }
        else if (ctx is CobolParserCore.SubtractOperandContext subOp)
        {
            if (subOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(subOp.dataReference());
            if (subOp.literal() != null)
                return BindLiteral(subOp.literal());
        }
        else if (ctx is CobolParserCore.MultiplyOperandContext mulOp)
        {
            if (mulOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(mulOp.dataReference());
            if (mulOp.literal() != null)
                return BindLiteral(mulOp.literal());
        }
        else if (ctx is CobolParserCore.DivideOperandContext divOp)
        {
            if (divOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(divOp.dataReference());
            if (divOp.literal() != null)
                return BindLiteral(divOp.literal());
        }

        // Fallback: try to parse the text
        string text = ctx.GetText();
        return BindDataReferenceOrLiteral(text);
    }

    private BoundExpression BindDataReferenceOrLiteral(string text)
    {
        if (decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric, originalText: text);

        var sym = _semantic.ResolveData(text);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric);

        _diagnostics.Report(DiagnosticDescriptors.COBOL0110,
            SourceLocation.None, TextSpan.Empty,
            $"Unresolved identifier '{text}'");
        return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
    }

    private BoundExpression BindArithmeticExpr(CobolParserCore.ArithmeticExpressionContext? ctx)
        => ctx != null ? BindAdditiveExpression(ctx.additiveExpression()) : new BoundLiteralExpression(0m, CobolCategory.Numeric);

    /// <summary>
    /// <summary>
    /// Bind an array of receivingArithmeticOperand contexts into BoundArithmeticTarget list.
    /// Each operand carries a data reference and an optional ROUNDED flag.
    /// </summary>
    private List<BoundArithmeticTarget> BindArithmeticTargets(
        CobolParserCore.ReceivingArithmeticOperandContext[] operands)
    {
        var targets = new List<BoundArithmeticTarget>();
        foreach (var op in operands)
        {
            var sym = BindDataReferenceWithSubscripts(op.dataReference());
            if (sym is BoundIdentifierExpression bound)
                targets.Add(new BoundArithmeticTarget(bound, op.ROUNDED() != null));
        }
        return targets;
    }

    /// Bind a data reference: IDENTIFIER with optional qualification (OF/IN),
    /// subscripts, and reference modification.
    /// Qualified names are resolved right-to-left: A OF B OF C → resolve C, then B in C, then A in B.
    /// </summary>
    private BoundExpression BindDataReferenceWithSubscripts(CobolParserCore.DataReferenceContext idCtx)
    {
        string name = idCtx.IDENTIFIER().GetText();
        var tails = idCtx.dataReferenceSuffix();

        // Extract qualifications, subscripts, and refmod from dataNameTail*
        var qualifiers = new List<string>();
        CobolParserCore.SubscriptOrRefModContext? subOrRefMod = null;
        CobolParserCore.RefModSpecContext? refModCtx = null;

        foreach (var tail in tails)
        {
            if (tail.qualification() != null)
            {
                var qual = tail.qualification();
                qualifiers.Add(qual.IDENTIFIER().GetText());
                // Extract subscripts/refmods attached to the qualifier (e.g., AX-2 IN AX(I))
                var qualSubs = qual.subscriptPart();
                if (qualSubs.Length > 0 && subOrRefMod == null)
                    subOrRefMod = qualSubs[0].subscriptOrRefMod();
                var qualRefMods = qual.refModPart();
                if (qualRefMods.Length > 0 && refModCtx == null)
                    refModCtx = qualRefMods[0].refModSpec();
            }
            else if (tail.subscriptPart() != null && subOrRefMod == null)
            {
                subOrRefMod = tail.subscriptPart().subscriptOrRefMod();
            }
            else if (tail.refModPart() != null && refModCtx == null)
            {
                refModCtx = tail.refModPart().refModSpec();
            }
        }

        // Resolve the data symbol — qualified or unqualified
        DataSymbol? sym;
        if (qualifiers.Count > 0)
        {
            // Right-to-left narrowing: resolve outermost qualifier first,
            // then walk inward to the leftmost identifier.
            sym = ResolveQualifiedName(name, qualifiers);
        }
        else
        {
            sym = _semantic.ResolveData(name);
        }

        if (sym == null)
        {
            // Check for SYMBOLIC CHARACTER from SPECIAL-NAMES
            var symChar = _semantic.ResolveSymbolicCharacter(name);
            if (symChar.HasValue)
            {
                // Symbolic character: produce a 1-byte string literal
                string charValue = ((char)symChar.Value).ToString();
                return Typed(new BoundLiteralExpression(charValue, CobolCategory.Alphanumeric));
            }

            return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);
        }

        var cat = sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric;

        if (subOrRefMod == null)
        {
            var plainId = Typed(new BoundIdentifierExpression(sym, cat));
            if (refModCtx != null)
                return Typed(BindReferenceModification(plainId, refModCtx));
            return plainId;
        }

        // Interpret the flat SUBSCRIPT-mode token sequence
        var (subExprs, isRefMod) = InterpretSubscriptTokens(subOrRefMod);

        if (isRefMod)
        {
            var startExpr = subExprs.Count > 0 ? subExprs[0] : new BoundLiteralExpression(1m, CobolCategory.Numeric);
            BoundExpression? lengthExpr = subExprs.Count > 1 ? subExprs[1] : null;
            var refModBase = Typed(new BoundIdentifierExpression(sym, cat));
            return Typed(new BoundReferenceModificationExpression(refModBase, startExpr, lengthExpr));
        }

        var subs = subExprs;

        // ── Subscript validation (COBOL-85 semantic rules) ──

        int occursDepth = 0;
        var current = sym;
        while (current != null)
        {
            if (current.Occurs != null)
                occursDepth++;
            current = current.Parent;
        }

        int subscriptCount = subs.Count;
        int line = idCtx.Start?.Line ?? 0;
        var loc = new Common.SourceLocation("<source>", 0, line, 0);
        var span = Common.TextSpan.Empty;

        if (subscriptCount > 0 && occursDepth == 0)
            _diagnostics.Report(DiagnosticDescriptors.COBOL0405, loc, span, sym.Name);

        if (subscriptCount > occursDepth && occursDepth > 0)
            _diagnostics.Report(DiagnosticDescriptors.COBOL0406, loc, span, sym.Name, occursDepth, subscriptCount);

        // COBOL-85 standard specifies 3 OCCURS levels; we support up to 7 (NIST suite exercises 7).
        // Emit a warning (not error) beyond 3 levels to note departure from strict COBOL-85.
        if (occursDepth > 7)
            _diagnostics.Report(DiagnosticDescriptors.COBOL0407, loc, span, sym.Name, occursDepth);

        if (subscriptCount > 7)
            _diagnostics.Report(DiagnosticDescriptors.COBOL0408, loc, span, subscriptCount);

        if (sym.IsElementary && occursDepth > 0 && subscriptCount > 0 && subscriptCount < occursDepth)
            _diagnostics.Report(DiagnosticDescriptors.COBOL0409, loc, span, sym.Name, occursDepth, subscriptCount);

        var baseId = Typed(new BoundIdentifierExpression(sym, cat, subs));

        if (refModCtx != null)
            return Typed(BindReferenceModification(baseId, refModCtx));

        return baseId;
    }

    /// <summary>
    /// Interpret the flat SUBSCRIPT-mode token sequence into expressions.
    /// Returns (expressions, isRefMod). If SUB_COLON is present, it's ref-mod
    /// and expressions[0] = start, expressions[1] = length. Otherwise it's subscripts.
    /// </summary>
    private (List<BoundExpression> Exprs, bool IsRefMod) InterpretSubscriptTokens(
        CobolParserCore.SubscriptOrRefModContext ctx)
    {
        // Collect all leaf tokens from the subToken+ tree
        var tokens = new List<Antlr4.Runtime.IToken>();
        CollectLeafTokens(ctx, tokens);

        // Check for colon → ref-mod
        int colonIdx = tokens.FindIndex(t => t.Type == CobolParserCore.SUB_COLON);
        if (colonIdx >= 0)
        {
            // Ref-mod: split on colon, parse each half as arithmetic expression
            var startTokens = tokens.GetRange(0, colonIdx);
            var lengthTokens = colonIdx + 1 < tokens.Count
                ? tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1)
                : new List<Antlr4.Runtime.IToken>();
            var exprs = new List<BoundExpression>();
            exprs.Add(BindSubscriptTokensAsArithmetic(startTokens));
            if (lengthTokens.Any(t => t.Type != CobolParserCore.SUB_WS))
                exprs.Add(BindSubscriptTokensAsArithmetic(lengthTokens));
            return (exprs, true);
        }

        // Subscripts: split on multi-space (SUB_WS with 2+ chars) or SUB_COMMA boundaries
        var segments = SplitSubscriptTokens(tokens);
        var subs = new List<BoundExpression>();
        foreach (var seg in segments)
            subs.Add(BindSubscriptSegment(seg));
        return (subs, false);
    }

    private static void CollectLeafTokens(Antlr4.Runtime.Tree.IParseTree node, List<Antlr4.Runtime.IToken> tokens)
    {
        if (node is Antlr4.Runtime.Tree.ITerminalNode term)
        {
            tokens.Add(term.Symbol);
            return;
        }
        for (int i = 0; i < node.ChildCount; i++)
            CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split token list into subscript segments on WS/COMMA boundaries.</summary>
    private static List<List<Antlr4.Runtime.IToken>> SplitSubscriptTokens(List<Antlr4.Runtime.IToken> tokens)
    {
        var segments = new List<List<Antlr4.Runtime.IToken>>();
        var current = new List<Antlr4.Runtime.IToken>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_COMMA || t.Type == CobolParserCore.SUB_SEMICOLON)
            {
                if (current.Count > 0) segments.Add(current);
                current = new List<Antlr4.Runtime.IToken>();
                continue;
            }
            if (t.Type == CobolParserCore.SUB_WS)
            {
                // Multi-space is a subscript separator. Single space could be part of
                // relative subscripting (IDENT + N). Check: if next non-WS is a sign
                // token (SUB_PLUS/SUB_MINUS) and current ends with an identifier,
                // it MIGHT be relative. But SIGNED_INTEGERLIT already handled the
                // adjacent-sign case. If we see SUB_PLUS/SUB_MINUS after WS, it's
                // part of relative subscripting (operator separated by space).
                // Split only when what follows starts a new subscript:
                //   SIGNED_INTEGERLIT, SUB_IDENTIFIER, SUB_INTEGERLIT, SUB_ALL
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == CobolParserCore.SUB_WS)
                    next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    int nextType = tokens[next].Type;
                    // Only split if next token starts a new subscript AND current
                    // segment doesn't end with an operator (which would mean the
                    // WS is inside a relative subscript: IDENT + N)
                    var lastNonWs = current.FindLast(x => x.Type != CobolParserCore.SUB_WS);
                    bool endsWithOperator = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_PLUS || lastNonWs.Type == CobolParserCore.SUB_MINUS);
                    // Don't split after OF/IN — these are qualification keywords
                    bool endsWithQualifier = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_OF || lastNonWs.Type == CobolParserCore.SUB_IN);

                    if (!endsWithOperator && !endsWithQualifier &&
                        (nextType == CobolParserCore.SIGNED_INTEGERLIT
                         || nextType == CobolParserCore.SUB_IDENTIFIER
                         || nextType == CobolParserCore.SUB_INTEGERLIT
                         || nextType == CobolParserCore.SUB_ALL))
                    {
                        segments.Add(current);
                        current = new List<Antlr4.Runtime.IToken>();
                        i = next - 1; // skip consumed WS
                        continue;
                    }
                }
                // Part of relative subscripting — keep in current segment
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Bind a single subscript segment (list of SUBSCRIPT-mode tokens).</summary>
    private BoundExpression BindSubscriptSegment(List<Antlr4.Runtime.IToken> tokens)
    {
        // Remove leading/trailing WS
        while (tokens.Count > 0 && tokens[0].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(0);
        while (tokens.Count > 0 && tokens[^1].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(tokens.Count - 1);

        if (tokens.Count == 0)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        // Single SIGNED_INTEGERLIT: +8, -3
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SIGNED_INTEGERLIT)
        {
            decimal value = decimal.Parse(tokens[0].Text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // Single SUB_INTEGERLIT: 1, 10
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SUB_INTEGERLIT)
        {
            decimal value = decimal.Parse(tokens[0].Text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // ALL
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SUB_ALL)
            return new BoundLiteralExpression("ALL", CobolCategory.Alphanumeric);

        // Identifier with optional qualification (OF/IN) and relative offset
        // Extract identifier and qualifiers first
        string? baseName = null;
        var qualNames = new List<string>();
        bool expectingQualifier = false;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_WS) continue;
            if (t.Type == CobolParserCore.SUB_OF || t.Type == CobolParserCore.SUB_IN)
            {
                expectingQualifier = true;
                continue;
            }
            if (t.Type == CobolParserCore.SUB_IDENTIFIER)
            {
                if (baseName == null) baseName = t.Text;
                else if (expectingQualifier) { qualNames.Add(t.Text); expectingQualifier = false; }
                continue;
            }
            // Remaining tokens are operator + offset (relative subscript)
            break;
        }

        if (baseName == null)
            return BindSubscriptTokensAsArithmetic(tokens);

        DataSymbol? sym2;
        if (qualNames.Count > 0)
            sym2 = ResolveQualifiedName(baseName, qualNames);
        else
            sym2 = _semantic.ResolveData(baseName);

        BoundExpression baseExpr2 = sym2 != null
            ? new BoundIdentifierExpression(sym2, sym2.ResolvedType?.Category ?? CobolCategory.Numeric)
            : new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);

        // Check for relative offset (+/- integer) in remaining tokens
        var remaining = tokens.SkipWhile(t =>
            t.Type == CobolParserCore.SUB_WS || t.Type == CobolParserCore.SUB_IDENTIFIER
            || t.Type == CobolParserCore.SUB_OF || t.Type == CobolParserCore.SUB_IN).ToList();
        if (remaining.Count >= 2)
        {
            var opTok = remaining.FirstOrDefault(t => t.Type == CobolParserCore.SUB_PLUS || t.Type == CobolParserCore.SUB_MINUS);
            var numTok = remaining.FirstOrDefault(t => t.Type == CobolParserCore.SUB_INTEGERLIT);
            if (opTok != null && numTok != null)
            {
                var offset = decimal.Parse(numTok.Text, System.Globalization.CultureInfo.InvariantCulture);
                var op = opTok.Type == CobolParserCore.SUB_MINUS
                    ? BoundBinaryOperatorKind.Subtract : BoundBinaryOperatorKind.Add;
                return new BoundBinaryExpression(baseExpr2, op,
                    new BoundLiteralExpression(offset, CobolCategory.Numeric), CobolCategory.Numeric);
            }
        }

        return baseExpr2;
    }

    /// <summary>Bind a token list as an arithmetic expression (for ref-mod or relative subscript).</summary>
    private BoundExpression BindSubscriptTokensAsArithmetic(List<Antlr4.Runtime.IToken> tokens)
    {
        // Remove leading/trailing WS
        while (tokens.Count > 0 && tokens[0].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(0);
        while (tokens.Count > 0 && tokens[^1].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(tokens.Count - 1);

        if (tokens.Count == 0)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        // Build expression from tokens: handle identifiers, integers, +/- operators
        BoundExpression? result = null;
        BoundBinaryOperatorKind pendingOp = default;
        bool hasPendingOp = false;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_WS) continue;

            BoundExpression? term = null;
            if (t.Type == CobolParserCore.SUB_IDENTIFIER)
            {
                var sym = _semantic.ResolveData(t.Text);
                term = sym != null
                    ? new BoundIdentifierExpression(sym, sym.ResolvedType?.Category ?? CobolCategory.Numeric)
                    : (BoundExpression)new BoundLiteralExpression(t.Text, CobolCategory.Alphanumeric);
            }
            else if (t.Type == CobolParserCore.SUB_INTEGERLIT || t.Type == CobolParserCore.SIGNED_INTEGERLIT)
            {
                term = new BoundLiteralExpression(
                    decimal.Parse(t.Text, System.Globalization.CultureInfo.InvariantCulture),
                    CobolCategory.Numeric);
            }
            else if (t.Type == CobolParserCore.SUB_PLUS)
            {
                pendingOp = BoundBinaryOperatorKind.Add;
                hasPendingOp = true;
                continue;
            }
            else if (t.Type == CobolParserCore.SUB_MINUS)
            {
                pendingOp = BoundBinaryOperatorKind.Subtract;
                hasPendingOp = true;
                continue;
            }
            else continue; // skip OF, IN, etc. for now

            if (term != null)
            {
                if (result == null)
                    result = term;
                else if (hasPendingOp)
                {
                    result = new BoundBinaryExpression(result, pendingOp, term, CobolCategory.Numeric);
                    hasPendingOp = false;
                }
            }
        }

        return result ?? new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    /// <summary>
    /// Bind a subscript entry per COBOL-85 §5.3. SUBSCRIPT lexer mode provides
    /// sign-adjacency disambiguation: SIGNED_INTEGERLIT (+N) vs SUB_PLUS SUB_WS SUB_INTEGERLIT (+ N).
    /// </summary>
    private BoundExpression BindSubscriptEntry(CobolParserCore.SubscriptEntryContext ctx)
    {
        // Signed integer literal: +8, -3, +1 (sign adjacent to digits)
        if (ctx.SIGNED_INTEGERLIT() is { } signedLit)
        {
            string text = signedLit.GetText();
            decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // Unsigned integer literal: 1, 10, 300
        if (ctx.SUB_INTEGERLIT() is { } intLit)
        {
            decimal value = decimal.Parse(intLit.GetText(), System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // ALL (for SEARCH ALL)
        if (ctx.SUB_ALL() != null)
            return new BoundLiteralExpression("ALL", CobolCategory.Alphanumeric);

        // Data-name / index-name with optional qualification and relative offset
        if (ctx.SUB_IDENTIFIER() is { } idToken)
        {
            string baseName = idToken.GetText();

            // Handle qualifications
            var quals = ctx.subscriptQualification();
            DataSymbol? baseSym;
            if (quals.Length > 0)
            {
                var qualNames = new List<string>();
                foreach (var q in quals)
                    qualNames.Add(q.SUB_IDENTIFIER().GetText());
                baseSym = ResolveQualifiedName(baseName, qualNames);
            }
            else
            {
                baseSym = _semantic.ResolveData(baseName);
            }

            BoundExpression baseExpr;
            if (baseSym != null)
            {
                var baseCat = baseSym.ResolvedType?.Category ?? CobolCategory.Numeric;
                baseExpr = new BoundIdentifierExpression(baseSym, baseCat);
            }
            else
            {
                baseExpr = new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);
            }

            // Relative subscript offset: data-name + N or data-name - N
            if (ctx.relativeOffset() is { } relOff)
            {
                decimal offset = decimal.Parse(relOff.SUB_INTEGERLIT().GetText(),
                    System.Globalization.CultureInfo.InvariantCulture);
                var offsetLit = new BoundLiteralExpression(offset, CobolCategory.Numeric);
                var op = relOff.SUB_MINUS() != null
                    ? BoundBinaryOperatorKind.Subtract
                    : BoundBinaryOperatorKind.Add;
                return new BoundBinaryExpression(baseExpr, op, offsetLit, CobolCategory.Numeric);
            }

            return baseExpr;
        }

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    /// <summary>
    /// Resolve a qualified name using right-to-left narrowing.
    /// A OF B OF C → resolve C (outermost), then B within C, then A within B.
    /// </summary>
    private DataSymbol? ResolveQualifiedName(string name, List<string> qualifiers)
    {
        // Start from the rightmost (outermost) qualifier
        DataSymbol? context = _semantic.ResolveData(qualifiers[^1]);
        if (context == null) return null;

        // Walk qualifiers right-to-left (skip the last one, already resolved)
        for (int i = qualifiers.Count - 2; i >= 0; i--)
        {
            context = FindChild(context, qualifiers[i]);
            if (context == null) return null;
        }

        // Resolve the target name within the final context
        return FindChild(context, name);
    }

    /// <summary>
    /// Find a child data symbol by name within a group item.
    /// Searches recursively through the group's children.
    /// </summary>
    private static DataSymbol? FindChild(DataSymbol parent, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                return child;
            // Search deeper (intermediate groups)
            var deep = FindChild(child, name);
            if (deep != null) return deep;
        }
        return null;
    }

    private BoundExpression BindReferenceModification(
        BoundIdentifierExpression baseId,
        CobolParserCore.RefModSpecContext ctx)
    {
        var arithExprs = ctx.arithmeticExpression();
        var startExpr = BindArithmeticExpr(arithExprs[0]);

        BoundExpression? lengthExpr = null;
        if (arithExprs.Length > 1)
            lengthExpr = BindArithmeticExpr(arithExprs[1]);

        return new BoundReferenceModificationExpression(baseId, startExpr, lengthExpr);
    }

    // ── USE (declaratives) ──

    private BoundUseStatement BindUse(CobolParserCore.UseStatementContext ctx)
    {
        // USE BEFORE REPORTING report-name
        if (ctx.BEFORE() != null && ctx.REPORTING() != null)
        {
            string reportName = ctx.procedureName() != null ? ExtractProcedureNameText(ctx.procedureName()) : "";
            return new BoundUseStatement(isBeforeReporting: true, [], reportName);
        }

        // USE AFTER STANDARD ERROR PROCEDURE ON file-name+
        var fileNames = new List<string>();
        foreach (var fn in ctx.fileName())
        {
            fileNames.Add(fn.GetText());
        }
        return new BoundUseStatement(isBeforeReporting: false, fileNames, reportName: null);
    }
}
