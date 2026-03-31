// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;
using CobolSharp.Compiler.Semantics.Bound.Binding;

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
    internal readonly BindingContext _ctx;

    public BoundTreeBuilder(SemanticModel semantic, DiagnosticBag diagnostics, CompilationOptions? options = null)
    {
        _semantic = semantic;
        _diagnostics = diagnostics;
        _options = options ?? new CompilationOptions();
        _ctx = new BindingContext(semantic, diagnostics, _options);
        _ctx.ProcedureName = new ProcedureNameResolver(_ctx);
        _ctx.Expression = new ExpressionBinder(_ctx);
        _ctx.Condition = new ConditionBinder(_ctx);
        _ctx.Arithmetic = new ArithmeticStatementBinder(_ctx);
        _ctx.Data = new DataStatementBinder(_ctx);
        _ctx.ControlFlow = new ControlFlowBinder(_ctx);
        _ctx.FileIo = new FileIoBinder(_ctx);
        _ctx.Call = new CallBinder(_ctx);
        _ctx.String = new StringStatementBinder(_ctx);
        _ctx.BindStatement = BindStatement;
        _ctx.Typed = expr => Typed(expr);
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
                    var bound = _ctx.FileIo.BindUse(useCtx);
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
        if (ctx.displayStatement() is { } disp) return _ctx.Data.BindDisplay(disp);
        if (ctx.moveStatement() is { } mv) return _ctx.Data.BindMove(mv);
        if (ctx.performStatement() is { } perf) return _ctx.ControlFlow.BindPerform(perf);
        if (ctx.writeStatement() is { } wr) return _ctx.FileIo.BindWrite(wr);
        if (ctx.ifStatement() is { } iff) return _ctx.ControlFlow.BindIf(iff);
        if (ctx.goToStatement() is { } gt) return _ctx.ControlFlow.BindGoTo(gt);
        if (ctx.alterStatement() is { } alt) return _ctx.ControlFlow.BindAlter(alt);
        if (ctx.entryStatement() is { } entry) return _ctx.Call.BindEntry(entry);
        if (ctx.cancelStatement() is { } cancel) return _ctx.Call.BindCancel(cancel);
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
        if (ctx.openStatement() is { } openCtx) return _ctx.FileIo.BindOpen(openCtx);
        if (ctx.closeStatement() is { } closeCtx) return _ctx.FileIo.BindClose(closeCtx);
        if (ctx.readStatement() is { } readCtx) return _ctx.FileIo.BindRead(readCtx);
        if (ctx.addStatement() is { } addCtx) return _ctx.Arithmetic.BindAdd(addCtx);
        if (ctx.subtractStatement() is { } sub) return _ctx.Arithmetic.BindSubtract(sub);
        if (ctx.multiplyStatement() is { } mult) return _ctx.Arithmetic.BindMultiply(mult);
        if (ctx.divideStatement() is { } div) return _ctx.Arithmetic.BindDivide(div);
        if (ctx.computeStatement() is { } comp) return _ctx.Arithmetic.BindCompute(comp);
        if (ctx.evaluateStatement() is { } evalStmt) return _ctx.ControlFlow.BindEvaluate(evalStmt);
        if (ctx.rewriteStatement() is { } rewriteCtx) return _ctx.FileIo.BindRewrite(rewriteCtx);
        if (ctx.initializeStatement() is { } initCtx) return _ctx.Data.BindInitialize(initCtx);
        if (ctx.setStatement() is { } setCtx) return _ctx.Data.BindSet(setCtx);
        if (ctx.inspectStatement() is { } inspCtx) return _ctx.String.BindInspect(inspCtx);
        if (ctx.acceptStatement() is { } accCtx) return _ctx.Data.BindAccept(accCtx);
        if (ctx.searchStatement() is { } searchCtx) return _ctx.ControlFlow.BindSearch(searchCtx);
        if (ctx.searchAllStatement() is { } searchAllCtx) return _ctx.ControlFlow.BindSearchAll(searchAllCtx);
        if (ctx.stringStatement() is { } stringCtx) return _ctx.String.BindString(stringCtx);
        if (ctx.unstringStatement() is { } unstringCtx) return _ctx.String.BindUnstring(unstringCtx);
        if (ctx.deleteStatement() is { } delCtx) return _ctx.FileIo.BindDelete(delCtx);
        if (ctx.startStatement() is { } startCtx) return _ctx.FileIo.BindStart(startCtx);
        if (ctx.returnStatement() is { } retCtx) return _ctx.FileIo.BindReturn(retCtx);
        if (ctx.sortStatement() is { } sortCtx) return _ctx.FileIo.BindSort(sortCtx);
        if (ctx.mergeStatement() is { } mergeCtx) return _ctx.FileIo.BindMerge(mergeCtx);
        if (ctx.releaseStatement() is { } relCtx) return _ctx.FileIo.BindRelease(relCtx);
        if (ctx.callStatement() is { } callCtx) return _ctx.Call.BindCall(callCtx);
        if (ctx.continueStatement() != null) return new BoundExitStatement(); // CONTINUE is a no-op
        if (ctx.useStatement() is { }) return new BoundExitStatement(); // USE is a no-op stub

        _diagnostics.Report(DiagnosticDescriptors.COBOL0110,
            new Common.SourceLocation("<source>", 0, ctx.Start?.Line ?? 0, 0),
            Common.TextSpan.Empty,
            $"{ctx.GetText()[..Math.Min(30, ctx.GetText().Length)]}...");
        return null;
    }
}
