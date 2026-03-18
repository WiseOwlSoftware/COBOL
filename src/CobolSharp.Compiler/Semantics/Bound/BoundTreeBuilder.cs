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

    /// <summary>
    /// Resolve a procedure name (paragraph or section) to a ParagraphSymbol.
    /// For sections, returns the first paragraph in the section.
    /// For paragraphs, returns the paragraph directly.
    /// </summary>
    private ParagraphSymbol? ResolveProcedureName(string name)
    {
        var para = _semantic.ResolveParagraph(name);
        var sec = _semantic.ResolveSection(name);

        // CS0870: Ambiguous procedure name — used as both section and paragraph
        if (para != null && sec != null)
        {
            _diagnostics.ReportWarning("CS0870",
                $"Procedure name '{name}' is used as both a section and a paragraph; resolving as paragraph.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
            return para;
        }

        if (para != null) return para;

        if (sec != null)
        {
            var sectionParas = _semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
                return _semantic.ResolveParagraph(sectionParas[0]);

            // CS0871: Section has no paragraphs
            _diagnostics.ReportWarning("CS0871",
                $"Section '{name}' contains no paragraphs.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
            return null;
        }

        // CS0872: Undefined procedure name
        _diagnostics.ReportError("CS0872",
            $"Procedure name '{name}' does not refer to a paragraph or section.",
            new Common.SourceLocation("<source>", 0, 0, 0),
            new Common.TextSpan(0, 0));
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
            _diagnostics.ReportWarning("CS0870",
                $"Procedure name '{name}' is used as both a section and a paragraph; resolving as paragraph.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
            return para;
        }

        if (para != null) return para;

        if (sec != null)
        {
            var sectionParas = _semantic.GetSectionParagraphs(name);
            if (sectionParas != null && sectionParas.Count > 0)
                return _semantic.ResolveParagraph(sectionParas[^1]); // LAST paragraph

            _diagnostics.ReportWarning("CS0871",
                $"Section '{name}' contains no paragraphs.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
            return null;
        }

        _diagnostics.ReportError("CS0872",
            $"Procedure name '{name}' does not refer to a paragraph or section.",
            new Common.SourceLocation("<source>", 0, 0, 0),
            new Common.TextSpan(0, 0));
        return null;
    }

    private (ParagraphSymbol? first, ParagraphSymbol? last) ResolveProcedureNameForPerform(string name)
    {
        var para = _semantic.ResolveParagraph(name);
        var sec = _semantic.ResolveSection(name);

        if (para != null && sec != null)
        {
            _diagnostics.ReportWarning("CS0870",
                $"Procedure name '{name}' is used as both a section and a paragraph; resolving as paragraph.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
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

            _diagnostics.ReportWarning("CS0871",
                $"Section '{name}' contains no paragraphs.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
            return (null, null);
        }

        _diagnostics.ReportError("CS0872",
            $"Procedure name '{name}' does not refer to a paragraph or section.",
            new Common.SourceLocation("<source>", 0, 0, 0),
            new Common.TextSpan(0, 0));
        return (null, null);
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
        if (ctx.exitStatement() is { } exitCtx)
        {
            if (exitCtx.PERFORM() != null)
                return new BoundExitPerformStatement();
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

        // CS0873: Unrecognized or unimplemented statement
        _diagnostics.ReportWarning("CS0873",
            $"Statement not recognized or not yet implemented: '{ctx.GetText()[..Math.Min(30, ctx.GetText().Length)]}...'",
            new Common.SourceLocation("<source>", 0, ctx.Start?.Line ?? 0, 0),
            new Common.TextSpan(0, 0));
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
                operands.Add(BindIdentifierWithSubscripts(idCtx));
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
        // moveSource: literal | identifier (COBOL-85)
        var litCtx = ctx.literal();
        if (litCtx != null) return BindLiteral(litCtx);

        if (ctx.identifier() != null)
            return BindIdentifierWithSubscripts(ctx.identifier());

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
                        else if (inlineTimesCtx.identifier() != null)
                            timesExpr = BindIdentifierWithSubscripts(inlineTimesCtx.identifier());
                    }
                    if (opt.performUntil() is { } untilCtx)
                        untilCond = BindCondition(untilCtx.condition());
                    if (opt.performVarying() is { } varyCtx)
                        varying = BindPerformVaryingOption(varyCtx);
                }
            }

            var inlineStmts = new List<BoundStatement>();
            foreach (var imp in ctx.imperativeStatement())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) inlineStmts.Add(bound);
                }

            if (varying != null)
                untilCond = varying.UntilCondition;

            return new BoundPerformStatement(null, null, timesExpr, untilCond, varying, inlineStmts);
        }

        // Out-of-line: first procedureName is the target (paragraph or section)
        string name = procNames[0].GetText();
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
                timesExpr = BindIdentifierWithSubscripts(timesCtx.identifier());

            return new BoundPerformStatement(paraSym, sectionLastPara, timesExpression: timesExpr);
        }

        // PERFORM para UNTIL cond
        if (ctx.performUntil() is { } untilCtx2)
        {
            var cond = BindCondition(untilCtx2.condition());
            return new BoundPerformStatement(paraSym, sectionLastPara, untilCondition: cond);
        }

        // PERFORM para VARYING ...
        if (ctx.performVarying() is { } varyCtx2)
        {
            var varying = BindPerformVaryingOption(varyCtx2);
            return new BoundPerformStatement(paraSym, sectionLastPara, varying: varying,
                untilCondition: varying?.UntilCondition);
        }

        // PERFORM para THRU para2 [options]
        if (procNames.Length > 1)
        {
            string thruName = procNames[1].GetText();
            var thruSym = ResolveProcedureNameForThruEnd(thruName);

            // Check for options on the THRU form
            BoundExpression? timesExpr2 = null;
            BoundExpression? untilCond = null;
            BoundPerformVarying? varyOpt = null;
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
                    else if (thruTimesCtx.identifier() != null)
                        timesExpr2 = BindIdentifierWithSubscripts(thruTimesCtx.identifier());
                }
                if (opt.performUntil() is { } u)
                    untilCond = BindCondition(u.condition());
                if (opt.performVarying() is { } v)
                {
                    varyOpt = BindPerformVaryingOption(v);
                    untilCond = varyOpt?.UntilCondition;
                }
            }

            return new BoundPerformStatement(paraSym, thruSym, timesExpr2, untilCond, varyOpt);
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
                var afterExpr = BindIdentifierWithSubscripts(afterCtx.identifier());
                var afterSym = ValidatePerformIndex(afterExpr);
                if (afterSym == null) continue;
                var afterExprs = afterCtx.arithmeticExpression();
                var afterInit = BindFullExpression(afterExprs[0]);
                var afterStep = BindFullExpression(afterExprs[1]);
                var afterUntil = BindCondition(afterCtx.condition());
                inner = new BoundPerformVarying(afterSym, afterInit, afterStep, afterUntil, inner);
            }
        }

        // Outer VARYING
        var indexExpr = BindIdentifierWithSubscripts(ctx.identifier());
        var indexSym = ValidatePerformIndex(indexExpr);
        if (indexSym == null) return null;
        var arithExprs = ctx.arithmeticExpression();
        var initial = BindFullExpression(arithExprs[0]);  // FROM
        var step = BindFullExpression(arithExprs[1]);      // BY
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

        if (id.IsSubscripted)
        {
            _diagnostics.ReportError("CS0861",
                $"PERFORM VARYING index '{id.Symbol.Name}' must not be subscripted.",
                new Common.SourceLocation("<source>", 0, 0, 0),
                new Common.TextSpan(0, 0));
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

        // Parse BEFORE/AFTER ADVANCING clause
        int? advancingLines = null;
        bool isAfterAdvancing = true;
        var advCtx = ctx.writeBeforeAfter();
        if (advCtx != null)
        {
            isAfterAdvancing = advCtx.GetChild(0).GetText().Equals("AFTER", StringComparison.OrdinalIgnoreCase);
            // Parse the advancing value — integer literal or PAGE
            var intLit = advCtx.integerLiteral();
            if (intLit != null)
            {
                advancingLines = int.Parse(intLit.GetText());
            }
            else
            {
                // Could be PAGE or an identifier — for PAGE, use a large value
                var idCtx = advCtx.identifier();
                if (idCtx != null && idCtx.IDENTIFIER().GetText().Equals("PAGE", StringComparison.OrdinalIgnoreCase))
                    advancingLines = 0; // PAGE = no line advance, just form-feed (handled separately)
                else
                    advancingLines = 1; // Default: 1 line
            }
        }

        BoundExpression? from = null;
        if (ctx.writeFrom() is { } fromCtx)
            from = BindIdentifierWithSubscripts(fromCtx.identifier());

        return new BoundWriteStatement(fileSym, recordSym, from, advancingLines, isAfterAdvancing);
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
            foreach (var idCtx in clause.identifier())
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
                string name = idCtx.IDENTIFIER().GetText();
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
        BoundIdentifierExpression? intoId = null;
        var intoCtx = ctx.readInto();
        if (intoCtx != null)
        {
            var intoExpr = BindIdentifierWithSubscripts(intoCtx.identifier());
            intoId = intoExpr as BoundIdentifierExpression;
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

        return new BoundReadStatement(fileSym, intoId, atEnd, notAtEnd);
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

        return new BoundRewriteStatement(fileSym, recordSym);
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
            var impStmts = ikCtx.imperativeStatement();
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
            keyCondition = BindRelational(keyCtx.relationalExpression());

        var invalidKey = new List<BoundStatement>();
        var notInvalidKey = new List<BoundStatement>();
        if (ctx.startInvalidKeyPhrase() is { } ikCtx)
        {
            var impStmts = ikCtx.imperativeStatement();
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

    // ── ACCEPT ──

    private BoundStatement? BindAccept(CobolParserCore.AcceptStatementContext ctx)
    {
        var targetId = BindIdentifierWithSubscripts(ctx.identifier());
        if (targetId is not BoundIdentifierExpression boundTarget) return null;

        var sourceKind = AcceptSourceKind.None;
        var sourceCtx = ctx.acceptSource();
        if (sourceCtx != null)
        {
            if (sourceCtx.DATE() != null) sourceKind = AcceptSourceKind.Date;
            else if (sourceCtx.TIME() != null) sourceKind = AcceptSourceKind.Time;
            else if (sourceCtx.DAY_OF_WEEK() != null) sourceKind = AcceptSourceKind.DayOfWeek;
            else if (sourceCtx.DAY() != null) sourceKind = AcceptSourceKind.Day;
        }

        return new BoundAcceptStatement(boundTarget, sourceKind);
    }

    // ── INSPECT ──

    private BoundStatement? BindInspect(CobolParserCore.InspectStatementContext ctx)
    {
        var targetExpr = BindIdentifierWithSubscripts(ctx.identifier());
        if (targetExpr is not BoundIdentifierExpression targetId) return null;

        var tallying = new List<BoundInspectTallyingItem>();
        var replacing = new List<BoundInspectReplacingItem>();
        BoundInspectConverting? converting = null;

        var tallyPhrase = ctx.inspectTallyingPhrase();
        if (tallyPhrase != null)
        {
            foreach (var item in tallyPhrase.inspectTallyingItem())
            {
                var counterExpr = BindIdentifierWithSubscripts(item.identifier());
                if (counterExpr is not BoundIdentifierExpression counterId) continue;

                foreach (var forClause in item.inspectForClause())
                {
                    var countPhrase = forClause.inspectCountPhrase();
                    InspectTallyKind kind;
                    string? pattern = null;

                    if (countPhrase.CHARACTERS() != null)
                    {
                        kind = InspectTallyKind.Characters;
                    }
                    else if (countPhrase.LEADING() != null)
                    {
                        kind = InspectTallyKind.Leading;
                        pattern = ExtractInspectChar(countPhrase.inspectChar());
                    }
                    else
                    {
                        kind = InspectTallyKind.All;
                        pattern = ExtractInspectChar(countPhrase.inspectChar());
                    }

                    var region = BindInspectDelimiters(countPhrase.inspectDelimiters());
                    tallying.Add(new BoundInspectTallyingItem(counterId, kind, pattern, region));
                }
            }
        }

        var replPhrase = ctx.inspectReplacingPhrase();
        if (replPhrase != null)
        {
            foreach (var item in replPhrase.inspectReplacingItem())
            {
                InspectReplaceKind kind;
                if (item.FIRST() != null) kind = InspectReplaceKind.First;
                else if (item.LEADING() != null) kind = InspectReplaceKind.Leading;
                else kind = InspectReplaceKind.All;

                string pattern;
                string replacement;

                var inspChars = item.inspectChar();
                if (item.CHARACTERS() != null)
                {
                    // CHARACTERS BY x — replace every character
                    pattern = "";
                    replacement = inspChars.Length > 0 ? ExtractInspectChar(inspChars[0]) ?? "" : "";
                }
                else
                {
                    // Pattern BY replacement (two inspectChar nodes)
                    pattern = inspChars.Length > 0 ? ExtractInspectChar(inspChars[0]) ?? "" : "";
                    replacement = inspChars.Length > 1 ? ExtractInspectChar(inspChars[1]) ?? "" : "";
                }

                var region = BindInspectDelimiters(item.inspectDelimiters());
                replacing.Add(new BoundInspectReplacingItem(kind, pattern, replacement, region));
            }
        }

        var convPhrase = ctx.inspectConvertingPhrase();
        if (convPhrase != null)
        {
            var inspChars = convPhrase.inspectChar();
            string fromSet = inspChars.Length > 0 ? ExtractInspectChar(inspChars[0]) ?? "" : "";
            string toSet = inspChars.Length > 1 ? ExtractInspectChar(inspChars[1]) ?? "" : "";
            // CONVERTING uses inspectBeforeAfterPhrase*, map to BoundInspectRegion
            var region = BindInspectBeforeAfter(convPhrase.inspectBeforeAfterPhrase());
            converting = new BoundInspectConverting(fromSet, toSet, region);
        }

        return new BoundInspectStatement(targetId, tallying, replacing, converting);
    }

    private string? ExtractInspectChar(CobolParserCore.InspectCharContext? ctx)
    {
        if (ctx == null) return null;
        if (ctx.literal() != null) return ExtractLiteralString(ctx.literal());
        if (ctx.identifier() != null) return ctx.identifier().IDENTIFIER().GetText();
        if (ctx.figurativeConstant() != null)
        {
            if (ctx.figurativeConstant().SPACE() != null) return " ";
            if (ctx.figurativeConstant().ZERO() != null) return "0";
            return ctx.figurativeConstant().GetText();
        }
        return null;
    }

    private BoundInspectRegion BindInspectBeforeAfter(
        CobolParserCore.InspectBeforeAfterPhraseContext[]? phrases)
    {
        if (phrases == null || phrases.Length == 0)
            return BoundInspectRegion.Empty;

        string? beforePattern = null;
        bool beforeInitial = false;
        string? afterPattern = null;
        bool afterInitial = false;

        foreach (var p in phrases)
        {
            if (p.BEFORE() != null)
            {
                beforePattern = ExtractInspectChar(p.inspectChar());
                beforeInitial = p.INITIAL_() != null;
            }
            else if (p.AFTER() != null)
            {
                afterPattern = ExtractInspectChar(p.inspectChar());
                afterInitial = p.INITIAL_() != null;
            }
        }

        return new BoundInspectRegion(beforePattern, beforeInitial, afterPattern, afterInitial);
    }

    private string ExtractStringValue(
        CobolParserCore.IdentifierContext[]? ids,
        CobolParserCore.LiteralContext[]? lits)
    {
        // Return the first available literal or identifier text
        if (lits != null && lits.Length > 0) return ExtractLiteralString(lits[0]);
        if (ids != null && ids.Length > 0) return ids[0].GetText();
        return "";
    }

    private string ExtractNthStringValue(
        CobolParserCore.IdentifierContext[]? ids,
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
            string raw = nonNum.GetText();
            if (raw.StartsWith("\"") && raw.EndsWith("\""))
                return raw.Substring(1, raw.Length - 2);
            if (raw.StartsWith("'") && raw.EndsWith("'"))
                return raw.Substring(1, raw.Length - 2);
            return raw;
        }
        return lit.GetText();
    }

    private BoundInspectRegion BindInspectDelimiters(CobolParserCore.InspectDelimitersContext? ctx)
    {
        if (ctx == null) return BoundInspectRegion.Empty;

        string? beforePattern = null;
        bool beforeInitial = false;
        string? afterPattern = null;
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
                beforePattern = chars.Length > 0 ? ExtractInspectChar(chars[0]) : null;
                beforeInitial = initials.Length > 0;
                afterPattern = chars.Length > 1 ? ExtractInspectChar(chars[1]) : null;
                afterInitial = initials.Length > 1;
            }
            else
            {
                afterPattern = chars.Length > 0 ? ExtractInspectChar(chars[0]) : null;
                afterInitial = initials.Length > 0;
                beforePattern = chars.Length > 1 ? ExtractInspectChar(chars[1]) : null;
                beforeInitial = initials.Length > 1;
            }
        }
        else if (ctx.BEFORE() != null)
        {
            beforePattern = chars.Length > 0 ? ExtractInspectChar(chars[0]) : null;
            beforeInitial = initials.Length > 0;
        }
        else if (ctx.AFTER() != null)
        {
            afterPattern = chars.Length > 0 ? ExtractInspectChar(chars[0]) : null;
            afterInitial = initials.Length > 0;
        }

        return new BoundInspectRegion(beforePattern, beforeInitial, afterPattern, afterInitial);
    }

    // ── SEARCH ──

    private BoundStatement? BindSearch(CobolParserCore.SearchStatementContext ctx)
    {
        var tableExpr = BindIdentifierWithSubscripts(ctx.identifier());
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        foreach (var whenCtx in ctx.searchWhenClause())
        {
            var cond = BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            foreach (var imp in whenCtx.imperativeStatement())
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
            foreach (var imp in atEndCtx.imperativeStatement())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index: find the first subscript used on a table element in WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        return new BoundSearchStatement(tableId, index, whens, atEnd);
    }

    private BoundStatement? BindSearchAll(CobolParserCore.SearchAllStatementContext ctx)
    {
        var tableExpr = BindIdentifierWithSubscripts(ctx.identifier());
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind WHEN clauses
        // SEARCH ALL has exactly one WHEN clause (spec-enforced in grammar)
        var whens = new List<BoundSearchWhenClause>();
        var whenCtx = ctx.searchAllWhenClause();
        if (whenCtx != null)
        {
            var cond = BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            foreach (var imp in whenCtx.imperativeStatement())
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
            foreach (var imp in atEndCtx.imperativeStatement())
                foreach (var stmt in imp.statement())
                {
                    var bound = BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index from WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        return new BoundSearchAllStatement(tableId, index, whens, atEnd);
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
                        if (current.OccursCount > 1)
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
            // Grammar: (identifier | literal) (DELIMITED BY (ALL)? (identifier | literal | SIZE))?
            // ANTLR returns identifier() and literal() as arrays across both positions.
            var identifiers = phrase.identifier();
            var literals = phrase.literal();

            // First identifier or literal is the value
            BoundExpression value;
            int idIdx = 0, litIdx = 0;
            if (identifiers.Length > 0)
            {
                value = BindIdentifierWithSubscripts(identifiers[0]);
                idIdx = 1;
            }
            else if (literals.Length > 0)
            {
                value = BindLiteral(literals[0]);
                litIdx = 1;
            }
            else
                continue;

            BoundExpression? delimiter = null;
            bool delimitedBySize = false;

            if (phrase.DELIMITED() != null)
            {
                if (phrase.SIZE() != null)
                {
                    delimitedBySize = true;
                }
                else
                {
                    // Delimiter is the next unused identifier or literal
                    if (idIdx < identifiers.Length)
                        delimiter = BindIdentifierWithSubscripts(identifiers[idIdx]);
                    else if (litIdx < literals.Length)
                        delimiter = BindLiteral(literals[litIdx]);
                }
            }

            sendings.Add(new BoundStringSending(value, delimiter, delimitedBySize));
        }

        // INTO
        var intoPhrase = ctx.stringIntoPhrase();
        if (intoPhrase == null) return null;
        var intoExpr = BindIdentifierWithSubscripts(intoPhrase.identifier());

        // POINTER
        BoundExpression? pointer = null;
        if (ctx.stringWithPointer() is { } ptrCtx)
            pointer = BindIdentifierWithSubscripts(ptrCtx.identifier());

        // ON OVERFLOW / NOT ON OVERFLOW
        var onOverflow = new List<BoundStatement>();
        var notOnOverflow = new List<BoundStatement>();
        if (ctx.stringOnOverflow() is { } ovCtx)
        {
            var impStmts = ovCtx.imperativeStatement();
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

        return new BoundStringStatement(sendings, intoExpr, pointer, onOverflow, notOnOverflow);
    }

    // ── UNSTRING ──

    private BoundStatement? BindUnstring(CobolParserCore.UnstringStatementContext ctx)
    {
        // Source identifier
        var sourceExpr = BindIdentifierWithSubscripts(ctx.identifier());

        // DELIMITED BY phrase (optional)
        BoundExpression? delimiter = null;
        bool delimitedByAll = false;
        if (ctx.unstringDelimiterPhrase() is { } delimCtx)
        {
            delimitedByAll = delimCtx.ALL() != null;
            if (delimCtx.identifier() is { } delimId)
                delimiter = BindIdentifierWithSubscripts(delimId);
            else if (delimCtx.literal() is { } delimLit)
                delimiter = BindLiteral(delimLit);
        }

        // INTO phrases (one or more)
        var intos = new List<BoundUnstringInto>();
        foreach (var intoPhrase in ctx.unstringIntoPhrase())
        {
            var identifiers = intoPhrase.identifier();
            int idIdx = 0;

            // First identifier is the INTO target
            var targetExpr = BindIdentifierWithSubscripts(identifiers[idIdx++]);

            // DELIMITER IN (optional)
            BoundExpression? delimiterIn = null;
            if (intoPhrase.DELIMITER() != null && idIdx < identifiers.Length)
                delimiterIn = BindIdentifierWithSubscripts(identifiers[idIdx++]);

            // COUNT IN (optional)
            BoundExpression? countIn = null;
            if (intoPhrase.COUNT() != null && idIdx < identifiers.Length)
                countIn = BindIdentifierWithSubscripts(identifiers[idIdx++]);

            intos.Add(new BoundUnstringInto(targetExpr, countIn, delimiterIn));
        }

        // WITH POINTER (optional)
        BoundExpression? pointer = null;
        if (ctx.unstringWithPointer() is { } ptrCtx)
            pointer = BindIdentifierWithSubscripts(ptrCtx.identifier());

        // TALLYING IN (optional)
        BoundExpression? tallying = null;
        if (ctx.unstringTallying() is { } tallyCtx)
            tallying = BindIdentifierWithSubscripts(tallyCtx.identifier());

        // ON OVERFLOW / NOT ON OVERFLOW
        var onOverflow = new List<BoundStatement>();
        var notOnOverflow = new List<BoundStatement>();
        if (ctx.unstringOnOverflow() is { } ovCtx)
        {
            var impStmts = ovCtx.imperativeStatement();
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

        return new BoundUnstringStatement(sourceExpr, delimiter, delimitedByAll,
            intos, pointer, tallying, onOverflow, notOnOverflow);
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
        string name = ctx.identifier().IDENTIFIER().GetText();
        var condSym = _semantic.ResolveConditionName(name);
        if (condSym != null)
        {
            bool setToTrue = ctx.TRUE_() != null;
            return new BoundSetConditionStatement(condSym, setToTrue);
        }

        // Not a condition name — treat as a data item SET TO TRUE/FALSE (unusual but valid)
        return null;
    }

    private BoundStatement? BindSetToValue(CobolParserCore.SetToValueStatementContext ctx)
    {
        string name = ctx.identifier().IDENTIFIER().GetText();

        // Check if it's a condition name first
        var condSym = _semantic.ResolveConditionName(name);
        if (condSym != null)
        {
            // SET condition-name TO TRUE/FALSE can also come through setToValueStatement
            // if the grammar matched it that way
            return new BoundSetConditionStatement(condSym, true);
        }

        // Regular data item: SET identifier TO value
        var targetId = BindIdentifierWithSubscripts(ctx.identifier());
        if (targetId is not BoundIdentifierExpression boundTarget) return null;

        var valueExpr = BindArithmeticExpr(ctx.arithmeticExpression());
        if (valueExpr == null) return null;

        return new BoundSetIndexStatement(boundTarget, SetOperation.Assign, valueExpr);
    }

    private BoundStatement? BindSetIndex(CobolParserCore.SetIndexStatementContext ctx)
    {
        var targetId = BindIdentifierWithSubscripts(ctx.identifier());
        if (targetId is not BoundIdentifierExpression boundTarget) return null;

        var intLit = ctx.integerLiteral();
        if (intLit == null) return null;

        if (!int.TryParse(intLit.GetText(), out int delta)) return null;
        var deltaExpr = new BoundLiteralExpression((decimal)delta, CobolCategory.Numeric);

        var op = ctx.UP() != null ? SetOperation.UpBy : SetOperation.DownBy;
        return new BoundSetIndexStatement(boundTarget, op, deltaExpr);
    }

    // ── INITIALIZE ──

    private BoundStatement? BindInitialize(CobolParserCore.InitializeStatementContext ctx)
    {
        var targets = new List<DataSymbol>();
        var idList = ctx.identifierList();
        if (idList == null) return null;

        foreach (var idCtx in idList.identifier())
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
        // The grammar alternatives are ordered:
        // ALPHANUMERIC DATA BY ... | NUMERIC DATA BY ... | ALPHANUMERIC EDITED DATA BY ... | NUMERIC EDITED DATA BY ...
        // Check for EDITED presence and ALPHANUMERIC/NUMERIC token
        if (ctx.EDITED() != null)
        {
            if (ctx.ALPHANUMERIC() != null) return InitializeCategory.AlphanumericEdited;
            return InitializeCategory.NumericEdited;
        }
        if (ctx.ALPHANUMERIC() != null) return InitializeCategory.Alphanumeric;
        return InitializeCategory.Numeric;
    }

    private BoundExpression? BindReplacingValue(CobolParserCore.InitializeReplacingItemContext ctx)
    {
        var litCtx = ctx.literal();
        if (litCtx != null) return BindLiteral(litCtx);

        var idCtx = ctx.identifier();
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
        // MULTIPLY has two forms:
        //   MULTIPLY A BY B [ROUNDED]          — result = A * B, stored in B
        //   MULTIPLY A BY B GIVING C [ROUNDED] — result = A * B, stored in C
        // In both forms, A is the first factor (Operand) and the first BY item
        // is the second factor (ByOperand). In the non-GIVING form, the BY item
        // is also the receiving item. In the GIVING form, it's just a factor.

        var operand = BindSimpleOperand(ctx.multiplyOperand());

        var byTargets = ctx.multiplyByTarget();
        if (byTargets.Length == 0)
            throw new InvalidOperationException(
                $"MULTIPLY statement has no BY operand (line {ctx.Start?.Line})");

        // The first BY item is always the second factor
        BoundExpression byOperand;
        var firstBy = byTargets[0];
        if (firstBy.identifier() != null)
            byOperand = BindIdentifierWithSubscripts(firstBy.identifier());
        else if (firstBy.literal() != null)
            byOperand = BindLiteral(firstBy.literal());
        else
            throw new InvalidOperationException(
                $"MULTIPLY BY operand is neither identifier nor literal (line {ctx.Start?.Line})");

        var givingCtx = ctx.multiplyGivingPhrase();
        bool isGiving = givingCtx != null;

        var targets = new List<BoundArithmeticTarget>();

        if (isGiving)
        {
            // GIVING form: targets are the GIVING items, not the BY items
            foreach (var gt in givingCtx!.multiplyByTarget())
            {
                if (gt.identifier() == null) continue;
                var sym = BindIdentifierWithSubscripts(gt.identifier());
                if (sym is BoundIdentifierExpression boundGt)
                    targets.Add(new BoundArithmeticTarget(boundGt, gt.ROUNDED() != null));
            }
        }
        else
        {
            // Non-GIVING form: BY items are both factors and receiving items.
            // Each BY item gets: result = operand * byItem, stored in byItem.
            foreach (var bt in byTargets)
            {
                if (bt.identifier() == null) continue;
                var sym = BindIdentifierWithSubscripts(bt.identifier());
                if (sym is BoundIdentifierExpression boundBt)
                    targets.Add(new BoundArithmeticTarget(boundBt, bt.ROUNDED() != null));
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException(
                $"MULTIPLY statement has no valid receiving items (line {ctx.Start?.Line})");

        var sizeError = BindSizeErrorClause(ctx.multiplyOnSizeError());
        return new BoundMultiplyStatement(operand, byOperand, targets, isGiving, sizeError);
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
                var sym = BindIdentifierWithSubscripts(t.identifier());
                if (sym is BoundIdentifierExpression boundT)
                    targets.Add(new BoundArithmeticTarget(boundT, t.ROUNDED() != null));
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
                    var sym = BindIdentifierWithSubscripts(gt.identifier());
                    if (sym is BoundIdentifierExpression boundGt)
                        targets.Add(new BoundArithmeticTarget(boundGt, gt.ROUNDED() != null));
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
            var sym = BindIdentifierWithSubscripts(t.identifier());
            if (sym is BoundIdentifierExpression boundT2)
                targets.Add(new BoundArithmeticTarget(boundT2, t.ROUNDED() != null));
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
                    givingMinuend = new BoundIdentifierExpression(targets[0].Target.Symbol, CobolCategory.Numeric);
                targets.Clear();
                foreach (var gt in givingTargetCtxs)
                {
                    var sym = BindIdentifierWithSubscripts(gt.identifier());
                    if (sym is BoundIdentifierExpression boundGt)
                        targets.Add(new BoundArithmeticTarget(boundGt, gt.ROUNDED() != null));
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
                    var sym = BindIdentifierWithSubscripts(gt.identifier());
                    if (sym is BoundIdentifierExpression boundGt)
                        targets.Add(new BoundArithmeticTarget(boundGt, gt.ROUNDED() != null));
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
                    var sym = BindIdentifierWithSubscripts(it.identifier());
                    if (sym is BoundIdentifierExpression boundIt)
                        targets.Add(new BoundArithmeticTarget(boundIt, it.ROUNDED() != null));
                }
            }

            // GIVING overrides INTO targets as destinations
            var givingPhrase = ctx.divideGivingPhrase();
            if (givingPhrase != null)
            {
                // INTO target becomes the dividend, GIVING targets are destinations
                if (targets.Count > 0)
                    dividend = new BoundIdentifierExpression(targets[0].Target.Symbol, CobolCategory.Numeric);
                targets.Clear();
                foreach (var gt in givingPhrase.divideTarget())
                {
                    var sym = BindIdentifierWithSubscripts(gt.identifier());
                    if (sym is BoundIdentifierExpression boundGt)
                        targets.Add(new BoundArithmeticTarget(boundGt, gt.ROUNDED() != null));
                }
            }
        }

        if (targets.Count == 0)
            throw new InvalidOperationException($"DIVIDE statement has no valid targets or operands (line {ctx.Start?.Line})");

        // REMAINDER
        BoundIdentifierExpression? remainderTarget = null;
        var remPhrase = ctx.divideRemainderPhrase();
        if (remPhrase != null)
        {
            var remExpr = BindIdentifierWithSubscripts(remPhrase.identifier());
            remainderTarget = remExpr as BoundIdentifierExpression;
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
            var sym = BindIdentifierWithSubscripts(s.identifier());
            if (sym is BoundIdentifierExpression boundS)
                targets.Add(new BoundArithmeticTarget(boundS, s.ROUNDED() != null));
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
            return BindIdentifierWithSubscripts(ctx.identifier());
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
        var idContexts = ctx.identifier();
        if (idContexts == null || idContexts.Length == 0) return null;

        // Check for DEPENDING ON — last identifier after DEPENDING is the selector
        bool hasDepending = ctx.DEPENDING() != null;

        // Target paragraphs: all identifiers except the DEPENDING ON one
        int targetCount = hasDepending ? idContexts.Length - 1 : idContexts.Length;
        var targets = new List<ParagraphSymbol>();

        for (int i = 0; i < targetCount; i++)
        {
            string name = idContexts[i].IDENTIFIER().GetText();
            var paraSym = ResolveProcedureName(name);
            if (paraSym != null) targets.Add(paraSym);
        }

        if (targets.Count == 0) return null;

        BoundIdentifierExpression? dependingOn = null;
        if (hasDepending && idContexts.Length > targetCount)
        {
            var depExpr = BindIdentifierWithSubscripts(idContexts[targetCount]);
            dependingOn = depExpr as BoundIdentifierExpression;
        }

        return new BoundGoToStatement(targets, dependingOn);
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
        return BindIdentifierWithSubscripts(idCtx);
    }

    /// <summary>
    /// Extract just the data name from an identifier context (strips subscripts).
    /// Use this instead of idCtx.IDENTIFIER().GetText() when resolving symbol names.
    /// </summary>
    private static string GetIdentifierName(CobolParserCore.IdentifierContext idCtx)
    {
        return idCtx.IDENTIFIER().GetText();
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
                return BindIdentifierWithSubscripts(addOp.identifier());
            if (addOp.literal() != null)
                return BindLiteral(addOp.literal());
        }
        else if (ctx is CobolParserCore.SubtractOperandContext subOp)
        {
            if (subOp.identifier() != null)
                return BindIdentifierWithSubscripts(subOp.identifier());
            if (subOp.literal() != null)
                return BindLiteral(subOp.literal());
        }
        else if (ctx is CobolParserCore.MultiplyOperandContext mulOp)
        {
            if (mulOp.identifier() != null)
                return BindIdentifierWithSubscripts(mulOp.identifier());
            if (mulOp.literal() != null)
                return BindLiteral(mulOp.literal());
        }
        else if (ctx is CobolParserCore.DivideOperandContext divOp)
        {
            if (divOp.identifier() != null)
                return BindIdentifierWithSubscripts(divOp.identifier());
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

        // Walk the expression tree properly
        return BindAdditiveExpr(ctx.additiveExpression());
    }

    private BoundExpression BindAdditiveExpr(CobolParserCore.AdditiveExpressionContext ctx)
    {
        var left = BindMultiplicativeExpr(ctx.multiplicativeExpression(0));
        for (int i = 0; i < ctx.addOp().Length; i++)
        {
            var right = BindMultiplicativeExpr(ctx.multiplicativeExpression(i + 1));
            var op = ctx.addOp(i).GetText() == "+"
                ? BoundBinaryOperatorKind.Add : BoundBinaryOperatorKind.Subtract;
            left = new BoundBinaryExpression(left, op, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression BindMultiplicativeExpr(CobolParserCore.MultiplicativeExpressionContext ctx)
    {
        var left = BindPowerExpr(ctx.powerExpression(0));
        for (int i = 0; i < ctx.mulOp().Length; i++)
        {
            var right = BindPowerExpr(ctx.powerExpression(i + 1));
            var op = ctx.mulOp(i).GetText() == "*"
                ? BoundBinaryOperatorKind.Multiply : BoundBinaryOperatorKind.Divide;
            left = new BoundBinaryExpression(left, op, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression BindPowerExpr(CobolParserCore.PowerExpressionContext ctx)
    {
        var left = BindUnaryExpr(ctx.unaryExpression(0));
        if (ctx.unaryExpression().Length > 1)
        {
            var right = BindUnaryExpr(ctx.unaryExpression(1));
            left = new BoundBinaryExpression(left, BoundBinaryOperatorKind.Power, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression BindUnaryExpr(CobolParserCore.UnaryExpressionContext ctx)
    {
        if (ctx.primaryExpression() != null)
            return BindPrimaryExpr(ctx.primaryExpression());

        // Unary +/-
        var inner = BindUnaryExpr(ctx.unaryExpression());
        if (ctx.addOp().GetText() == "-")
        {
            return new BoundBinaryExpression(
                new BoundLiteralExpression(0m, CobolCategory.Numeric),
                BoundBinaryOperatorKind.Subtract, inner, CobolCategory.Numeric);
        }
        return inner; // unary + is identity
    }

    private BoundExpression BindPrimaryExpr(CobolParserCore.PrimaryExpressionContext ctx)
    {
        // numericLiteral
        var numLit = ctx.numericLiteral();
        if (numLit != null)
            return BindNumericLiteral(numLit);

        // functionCall
        var funcCall = ctx.functionCall();
        if (funcCall != null)
        {
            // TODO: proper function binding
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);
        }

        // (arithmeticExpression) — parenthesized
        var parenExpr = ctx.arithmeticExpression();
        if (parenExpr != null)
            return BindArithmeticExpr(parenExpr);

        // identifier (possibly subscripted)
        var idCtx = ctx.identifier();
        if (idCtx != null)
            return BindIdentifierWithSubscripts(idCtx);

        return new BoundLiteralExpression(ctx.GetText(), CobolCategory.Alphanumeric);
    }

    /// <summary>
    /// Bind an identifier that may have subscripts: IDENTIFIER (LPAREN subscriptList RPAREN)?
    /// </summary>
    private BoundExpression BindIdentifierWithSubscripts(CobolParserCore.IdentifierContext idCtx)
    {
        string name = idCtx.IDENTIFIER().GetText();
        var sym = _semantic.ResolveData(name);
        if (sym == null)
            return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);

        var cat = sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric;

        var subList = idCtx.subscriptList();
        if (subList == null)
        {
            var plainId = new BoundIdentifierExpression(sym, cat);
            var refModCtxNoSub = idCtx.refModSpec();
            if (refModCtxNoSub != null)
                return BindReferenceModification(plainId, refModCtxNoSub);
            return plainId;
        }

        // Parse subscript expressions
        var subs = new List<BoundExpression>();
        foreach (var arithCtx in subList.arithmeticExpression())
            subs.Add(BindArithmeticExpr(arithCtx));

        // ── Subscript validation (COBOL-85 semantic rules) ──

        // Compute OCCURS depth: how many OCCURS levels this item is under
        int occursDepth = 0;
        var current = sym;
        while (current != null)
        {
            if (current.OccursCount > 1)
                occursDepth++;
            current = current.Parent;
        }

        int subscriptCount = subs.Count;
        int line = idCtx.Start?.Line ?? 0;
        var loc = new Common.SourceLocation("<source>", 0, line, 0);
        var span = new Common.TextSpan(0, 0);

        // CS0850: Subscripted a non-OCCURS item
        if (subscriptCount > 0 && occursDepth == 0)
        {
            _diagnostics.ReportError("CS0850",
                $"Item '{sym.Name}' is not defined with OCCURS and cannot be subscripted.",
                loc, span);
        }

        // CS0851: Too many subscripts for the OCCURS depth
        if (subscriptCount > occursDepth && occursDepth > 0)
        {
            _diagnostics.ReportError("CS0851",
                $"Item '{sym.Name}' has {occursDepth} OCCURS level(s) but was referenced with {subscriptCount} subscript(s).",
                loc, span);
        }

        // CS0852: More than 3 OCCURS levels (COBOL-85 limit)
        if (occursDepth > 3)
        {
            _diagnostics.ReportError("CS0852",
                $"Item '{sym.Name}' exceeds the COBOL-85 limit of 3 OCCURS levels (found {occursDepth}).",
                loc, span);
        }

        // CS0853: More than 3 subscripts supplied
        if (subscriptCount > 3)
        {
            _diagnostics.ReportError("CS0853",
                $"A maximum of 3 subscripts is permitted in COBOL-85; found {subscriptCount}.",
                loc, span);
        }

        // CS0854: Too few subscripts for elementary item under OCCURS
        if (sym.IsElementary && occursDepth > 0 && subscriptCount > 0 && subscriptCount < occursDepth)
        {
            _diagnostics.ReportError("CS0854",
                $"Item '{sym.Name}' requires {occursDepth} subscript(s) but was referenced with {subscriptCount}.",
                loc, span);
        }

        var baseId = new BoundIdentifierExpression(sym, cat, subs);

        // Reference modification: identifier(start:length)
        var refModCtx = idCtx.refModSpec();
        if (refModCtx != null)
            return BindReferenceModification(baseId, refModCtx);

        return baseId;
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
}
