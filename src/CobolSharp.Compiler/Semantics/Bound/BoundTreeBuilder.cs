// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;

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
        foreach (var stmtCtx in ctx.statement())
        {
            var bound = BindStatement(stmtCtx);
            if (bound != null)
                statements.Add(bound);
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
        if (ctx.addStatement() is { }) return new BoundArithmeticStatement(BoundNodeKind.AddStatement);
        if (ctx.subtractStatement() is { }) return new BoundArithmeticStatement(BoundNodeKind.SubtractStatement);
        if (ctx.multiplyStatement() is { }) return new BoundArithmeticStatement(BoundNodeKind.MultiplyStatement);
        if (ctx.divideStatement() is { }) return new BoundArithmeticStatement(BoundNodeKind.DivideStatement);
        if (ctx.computeStatement() is { }) return new BoundArithmeticStatement(BoundNodeKind.ComputeStatement);

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
                    operands.Add(new BoundIdentifierExpression(sym, CobolType.Alphanumeric));
                else
                    operands.Add(new BoundLiteralExpression(idCtx.GetText(), CobolType.String));
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

    // ── IF ──

    private BoundIfStatement? BindIf(CobolParserCore.IfStatementContext ctx)
    {
        var condCtx = ctx.condition();
        if (condCtx == null) return null;

        // Minimal condition binding: treat as a literal boolean for now
        var condition = new BoundLiteralExpression(true, CobolType.Boolean);

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
        var s = lit.STRINGLIT();
        if (s != null)
        {
            var text = s.GetText();
            if (text.Length >= 2 &&
                ((text[0] == '"' && text[^1] == '"') ||
                 (text[0] == '\'' && text[^1] == '\'')))
                text = text[1..^1];
            return new BoundLiteralExpression(text, CobolType.String);
        }

        var numCtx = lit.signedNumericLiteral();
        if (numCtx != null)
        {
            var raw = numCtx.GetText();
            if (decimal.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var val))
                return new BoundLiteralExpression(val, CobolType.Numeric);
            return new BoundLiteralExpression(raw, CobolType.String);
        }

        // figurativeConstant
        var figCtx = lit.figurativeConstant();
        if (figCtx != null)
        {
            string figText = figCtx.GetText().ToUpperInvariant();
            return figText switch
            {
                "SPACE" or "SPACES" =>
                    new BoundLiteralExpression(" ", CobolType.Alphanumeric),
                "ZERO" or "ZEROS" or "ZEROES" =>
                    new BoundLiteralExpression("0", CobolType.Numeric),
                "HIGH-VALUE" or "HIGH-VALUES" =>
                    new BoundLiteralExpression("\xFF", CobolType.Alphanumeric),
                "LOW-VALUE" or "LOW-VALUES" =>
                    new BoundLiteralExpression("\x00", CobolType.Alphanumeric),
                "QUOTE" or "QUOTES" =>
                    new BoundLiteralExpression("\"", CobolType.Alphanumeric),
                _ => new BoundLiteralExpression(figText, CobolType.String)
            };
        }

        // HEXLIT, etc.
        return new BoundLiteralExpression(lit.GetText(), CobolType.String);
    }

    private BoundExpression BindIdentifier(CobolParserCore.IdentifierContext idCtx)
    {
        string name = idCtx.GetText();
        var sym = _semantic.ResolveData(name);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolType.Alphanumeric);

        // Unresolved — treat as string literal for now
        return new BoundLiteralExpression(name, CobolType.String);
    }

    private BoundExpression BindArithmeticExpr(CobolParserCore.ArithmeticExpressionContext? ctx)
    {
        if (ctx == null)
            return new BoundLiteralExpression(0m, CobolType.Numeric);

        // Minimal: just get the text and try to parse as number or identifier
        string text = ctx.GetText();
        if (decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolType.Numeric);

        var sym = _semantic.ResolveData(text);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolType.Alphanumeric);

        return new BoundLiteralExpression(text, CobolType.String);
    }
}
