// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Control flow binding: BindPerform, BindEvaluate, BindIf, BindGoTo, BindAlter,
/// BindSearch, BindSearchAll, plus PERFORM VARYING and EVALUATE WHEN helpers.
/// </summary>
internal sealed class ControlFlowBinder
{
    private readonly BindingContext _ctx;

    internal ControlFlowBinder(BindingContext ctx) => _ctx = ctx;

    private static SourceLocation MakeLocation(ParserRuleContext ctx) =>
        new("<source>", 0, ctx.Start.Line, ctx.Start.Column);
    private static TextSpan MakeSpan(ParserRuleContext ctx) =>
        new(ctx.Start.StartIndex, ctx.Stop?.StopIndex ?? ctx.Start.StopIndex);

    private (SourceLocation loc, TextSpan span) DiagAt(int line)
        => (new SourceLocation("<source>", 0, line, 0), TextSpan.Empty);

    // ── PERFORM ──

    internal BoundPerformStatement? BindPerform(CobolParserCore.PerformStatementContext ctx)
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
                            timesExpr = _ctx.Expression.BindDataReferenceWithSubscripts(inlineTimesCtx.dataReference());
                    }
                    if (opt.performUntil() is { } untilCtx)
                    {
                        untilCond = _ctx.Condition.BindCondition(untilCtx.condition());
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
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) inlineStmts.Add(bound);
                }

            if (varying != null)
                untilCond = varying.UntilCondition;

            return new BoundPerformStatement(null, null, timesExpr, untilCond, varying, inlineStmts,
                isTestAfter);
        }

        // Out-of-line: first procedureName is the target (paragraph or section)
        string name = ProcedureNameResolver.ExtractProcedureNameText(procNames[0]);
        var (paraSym, sectionLastPara) = _ctx.ProcedureName.ResolveProcedureNameForPerform(name);
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
                timesExpr = _ctx.Expression.BindDataReferenceWithSubscripts(timesCtx.dataReference());

            return new BoundPerformStatement(paraSym, sectionLastPara, timesExpression: timesExpr);
        }

        // PERFORM para UNTIL cond
        if (ctx.performUntil() is { } untilCtx2)
        {
            var cond = _ctx.Condition.BindCondition(untilCtx2.condition());
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
            string thruName = ProcedureNameResolver.ExtractProcedureNameText(procNames[1]);
            var thruSym = _ctx.ProcedureName.ResolveProcedureNameForThruEnd(thruName);

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
                        timesExpr2 = _ctx.Expression.BindDataReferenceWithSubscripts(thruTimesCtx.dataReference());
                }
                if (opt.performUntil() is { } u)
                {
                    untilCond = _ctx.Condition.BindCondition(u.condition());
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

    internal BoundPerformVarying? BindPerformVaryingOption(CobolParserCore.PerformVaryingContext ctx)
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
                var afterExpr = _ctx.Expression.BindDataReferenceWithSubscripts(afterCtx.dataReference());
                var afterSym = ValidatePerformIndex(afterExpr);
                if (afterSym == null) continue;
                var afterExprs = afterCtx.arithmeticExpression();
                var afterInit = _ctx.Expression.BindAdditiveExpression(afterExprs[0].additiveExpression());
                var afterStep = _ctx.Expression.BindAdditiveExpression(afterExprs[1].additiveExpression());
                var afterUntil = _ctx.Condition.BindCondition(afterCtx.condition());
                inner = new BoundPerformVarying(afterSym, afterExpr, afterInit, afterStep, afterUntil, inner);
            }
        }

        // Outer VARYING
        var indexExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
        var indexSym = ValidatePerformIndex(indexExpr);
        if (indexSym == null) return null;
        var arithExprs = ctx.arithmeticExpression();
        var initial = _ctx.Expression.BindAdditiveExpression(arithExprs[0].additiveExpression());  // FROM
        var step = _ctx.Expression.BindAdditiveExpression(arithExprs[1].additiveExpression());      // BY
        var untilCond = _ctx.Condition.BindCondition(ctx.condition());

        return new BoundPerformVarying(indexSym, indexExpr, initial, step, untilCond, inner);
    }

    /// <summary>
    /// Validate a PERFORM VARYING index expression: must be a non-subscripted
    /// elementary numeric identifier. Returns the DataSymbol or null on failure.
    /// </summary>
    internal DataSymbol? ValidatePerformIndex(BoundExpression indexExpr)
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

    internal BoundEvaluateStatement BindEvaluate(CobolParserCore.EvaluateStatementContext ctx)
    {
        // Bind subjects — detect EVALUATE TRUE
        var subjects = new List<BoundExpression>();
        bool isEvaluateTrue = false;
        BoundClassConditionExpression? evaluateClassCondition = null;

        bool isEvaluateFalse = false;

        foreach (var subCtx in ctx.evaluateSubject())
        {
            if (subCtx.booleanLiteral()?.TRUE_() != null)
            {
                isEvaluateTrue = true;
                continue;
            }
            if (subCtx.booleanLiteral()?.FALSE_() != null)
            {
                isEvaluateFalse = true;
                continue;
            }
            // Check for class condition: EVALUATE X NUMERIC → treat as EVALUATE TRUE
            // where the implicit condition is "X IS [NOT] NUMERIC/ALPHABETIC"
            if (subCtx.classCondition() is { } classCtx && subCtx.valueOperand() is { } classVo)
            {
                isEvaluateTrue = true;
                BoundExpression classSubject;
                if (classVo.arithmeticExpression() is { } classArith)
                    classSubject = _ctx.Expression.BindAdditiveExpression(classArith.additiveExpression());
                else if (classVo.nonNumericLiteral() is { } classNonNum)
                    classSubject = _ctx.Expression.BindNonNumericLiteral(classNonNum);
                else
                    continue;
                bool isClassNot = subCtx.NOT() != null;
                var classKind = classCtx.NUMERIC() != null ? ClassConditionKind.Numeric
                    : classCtx.ALPHABETIC() != null ? ClassConditionKind.Alphabetic
                    : classCtx.ALPHABETIC_LOWER() != null ? ClassConditionKind.AlphabeticLower
                    : classCtx.ALPHABETIC_UPPER() != null ? ClassConditionKind.AlphabeticUpper
                    : ClassConditionKind.Numeric;
                // Store the class condition as implicit subject condition
                evaluateClassCondition = new BoundClassConditionExpression(classSubject, classKind, isClassNot);
                continue;
            }
            if (subCtx.valueOperand()?.arithmeticExpression() is { } arithCtx)
                subjects.Add(_ctx.Expression.BindAdditiveExpression(arithCtx.additiveExpression()));
            else if (subCtx.valueOperand()?.nonNumericLiteral() is { } nonNumCtx)
                subjects.Add(_ctx.Expression.BindNonNumericLiteral(nonNumCtx));
        }

        int subjectCount = (isEvaluateTrue || isEvaluateFalse) ? 1 : subjects.Count;

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
                        var bound = _ctx.BindStatement(stmt);
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
                subjectConditions.Add(BindEvaluateWhenGroup(groups[i], isEvaluateTrue || isEvaluateFalse, evaluateClassCondition));
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
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) stmts.Add(bound);
                }

            whens.Add(new BoundEvaluateWhen(subjectConditions, stmts));
        }

        return new BoundEvaluateStatement(subjects, whens, whenOther, isEvaluateFalse);
    }

    internal BoundEvaluateCondition BindEvaluateWhenGroup(
        CobolParserCore.EvaluateWhenGroupContext groupCtx, bool isEvaluateTrue,
        BoundClassConditionExpression? classConditionSubject = null)
    {
        var items = groupCtx.evaluateWhenItem();

        if (isEvaluateTrue)
        {
            // For EVALUATE TRUE with class condition subject (e.g., EVALUATE X NUMERIC WHEN TRUE),
            // inject the class condition as the WHEN's condition
            if (classConditionSubject != null && items.Length > 0)
            {
                var item = items[0];
                // Check if WHEN item is TRUE or FALSE via condition → booleanLiteral
                var boolLit = item.condition()?.logicalOrExpression()?.logicalAndExpression(0)
                    ?.unaryLogicalExpression(0)?.primaryCondition()?.booleanLiteral();
                if (boolLit?.TRUE_() != null)
                    return new BoundEvaluateConditionWhen(classConditionSubject);
                if (boolLit?.FALSE_() != null)
                    return new BoundEvaluateConditionWhen(
                        new BoundClassConditionExpression(classConditionSubject.Subject,
                            classConditionSubject.ClassKind, !classConditionSubject.IsNegated));
            }
            // For EVALUATE TRUE, the WHEN item is a condition
            if (items.Length > 0 && items[0].condition() is { } condCtx)
                return new BoundEvaluateConditionWhen(_ctx.Condition.BindCondition(condCtx));
            // Fallback: try as valueOperand (bare identifier → condition name)
            if (items.Length > 0 && items[0].valueOperand()?.arithmeticExpression() is { } arithFallback)
            {
                var expr = _ctx.Expression.BindAdditiveExpression(arithFallback.additiveExpression());
                // Check if the result resolves to a condition name
                expr = _ctx.Condition.TryResolveConditionName(expr);
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
                values.Add(_ctx.Condition.BindCondition(condCtx));
            }
        }

        bool isNot = groupCtx.NOT() != null;
        return new BoundEvaluateValueCondition(values, ranges, isAny, isNot);
    }

    internal BoundExpression BindValueOperand(CobolParserCore.ValueOperandContext vo)
    {
        if (vo.nonNumericLiteral() is { } nonNumCtx)
            return _ctx.Expression.BindNonNumericLiteral(nonNumCtx);
        return _ctx.Expression.BindAdditiveExpression(vo.arithmeticExpression().additiveExpression());
    }

    // ── IF ──

    internal BoundIfStatement? BindIf(CobolParserCore.IfStatementContext ctx)
    {
        var condCtx = ctx.condition();
        if (condCtx == null) return null;

        // Try to bind a real condition
        var condition = _ctx.Condition.BindCondition(condCtx);

        var thenStmts = new List<BoundStatement>();
        var elseStmts = new List<BoundStatement>();

        // Walk imperativeStatement* children
        var impStmts = ctx.statementBlock();
        if (impStmts.Length > 0)
        {
            foreach (var stmt in impStmts[0].statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) thenStmts.Add(bound);
            }
        }

        // ELSE block
        if (impStmts.Length > 1)
        {
            foreach (var stmt in impStmts[1].statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) elseStmts.Add(bound);
            }
        }

        return new BoundIfStatement(condition, thenStmts, elseStmts.Count > 0 ? elseStmts : null);
    }

    // ── GO TO ──

    internal BoundGoToStatement? BindGoTo(CobolParserCore.GoToStatementContext ctx)
    {
        var procNames = ctx.procedureName();

        // Bare GO TO (no target) — target set by ALTER at runtime
        if (procNames == null || procNames.Length == 0)
        {
            if (_ctx.Options.IsCobol2002OrLater)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3605,
                    MakeLocation(ctx), MakeSpan(ctx), _ctx.Options.DialectName);
                return null;
            }
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3606,
                MakeLocation(ctx), MakeSpan(ctx));
            return new BoundGoToStatement([]);
        }

        var targets = new List<ParagraphSymbol>();

        foreach (var pn in procNames)
        {
            string name = ProcedureNameResolver.ExtractProcedureNameText(pn);
            var paraSym = _ctx.ProcedureName.ResolveProcedureName(name);
            if (paraSym != null) targets.Add(paraSym);
        }

        if (targets.Count == 0) return null;

        // DEPENDING ON identifier (optional)
        BoundIdentifierExpression? dependingOn = null;
        if (ctx.DEPENDING() != null && ctx.dataReference() != null)
        {
            var depExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
            dependingOn = depExpr as BoundIdentifierExpression;
        }

        return new BoundGoToStatement(targets, dependingOn);
    }

    // ── ALTER ──

    internal BoundAlterStatement? BindAlter(CobolParserCore.AlterStatementContext ctx)
    {
        // Dialect check: ALTER deleted from COBOL-2002+
        if (_ctx.Options.IsCobol2002OrLater)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3601,
                MakeLocation(ctx), MakeSpan(ctx), _ctx.Options.DialectName);
            return null;
        }

        // Obsolete warning in COBOL-85 / Default mode
        _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3602,
            MakeLocation(ctx), MakeSpan(ctx));

        var entries = new List<BoundAlterEntry>();
        foreach (var entry in ctx.alterEntry())
        {
            var procNames = entry.procedureName();
            if (procNames.Length < 2) continue;

            string targetName = ProcedureNameResolver.ExtractProcedureNameText(procNames[0]);
            string destName = ProcedureNameResolver.ExtractProcedureNameText(procNames[1]);

            var targetSym = _ctx.ProcedureName.ResolveProcedureName(targetName);
            var destSym = _ctx.ProcedureName.ResolveProcedureName(destName);

            if (targetSym == null)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3603,
                    MakeLocation(entry), MakeSpan(entry), targetName);
                continue;
            }
            if (destSym == null)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL3603,
                    MakeLocation(entry), MakeSpan(entry), destName);
                continue;
            }

            entries.Add(new BoundAlterEntry(targetSym, destSym));
        }

        return entries.Count > 0 ? new BoundAlterStatement(entries) : null;
    }

    // ── SEARCH ──

    internal BoundStatement? BindSearch(CobolParserCore.SearchStatementContext ctx)
    {
        var dataRefs = ctx.dataReference();
        var tableExpr = _ctx.Expression.BindDataReferenceWithSubscripts(dataRefs[0]);
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind VARYING identifier (second dataReference, if present)
        BoundIdentifierExpression? varyingExpr = null;
        if (dataRefs.Length > 1)
        {
            var varyBound = _ctx.Expression.BindDataReferenceWithSubscripts(dataRefs[1]);
            if (varyBound is BoundIdentifierExpression varyId)
                varyingExpr = varyId;
        }

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        foreach (var whenCtx in ctx.searchWhenClause())
        {
            var cond = _ctx.Condition.BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            foreach (var imp in whenCtx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = _ctx.BindStatement(stmt);
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
                    var bound = _ctx.BindStatement(stmt);
                    if (bound != null) atEnd.Add(bound);
                }
        }

        // Extract index: find the first subscript used on a table element in WHEN conditions
        var index = ExtractSearchIndex(tableId.Symbol, whens);

        var searchStmt = new BoundSearchStatement(tableId, index, varyingExpr, whens, atEnd);
        ValidateSearchStatement(searchStmt, ctx.Start?.Line ?? 0);
        return searchStmt;
    }

    internal BoundStatement? BindSearchAll(CobolParserCore.SearchAllStatementContext ctx)
    {
        var tableExpr = _ctx.Expression.BindDataReferenceWithSubscripts(ctx.dataReference());
        if (tableExpr is not BoundIdentifierExpression tableId) return null;

        // Bind WHEN clauses
        var whens = new List<BoundSearchWhenClause>();
        foreach (var whenCtx in ctx.searchAllWhenClause())
        {
            var cond = _ctx.Condition.BindCondition(whenCtx.condition());
            var stmts = new List<BoundStatement>();
            foreach (var imp in whenCtx.statementBlock())
                foreach (var stmt in imp.statement())
                {
                    var bound = _ctx.BindStatement(stmt);
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
                    var bound = _ctx.BindStatement(stmt);
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
    internal BoundIdentifierExpression? ExtractSearchIndex(
        DataSymbol tableSymbol, List<BoundSearchWhenClause> whens)
    {
        foreach (var when in whens)
        {
            var idx = FindSubscriptOnTable(tableSymbol, when.Condition);
            if (idx != null) return idx;
        }
        return null;
    }

    internal BoundIdentifierExpression? FindSubscriptOnTable(
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
    internal static bool IsTableElement(DataSymbol sym, DataSymbol tableSymbol)
    {
        var current = sym;
        while (current != null)
        {
            if (current == tableSymbol) return true;
            current = current.Parent;
        }
        return false;
    }

    internal void ValidateSearchStatement(BoundSearchStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Table must have OCCURS
        if (stmt.Table.Symbol.Occurs == null)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1105, loc, span, stmt.Table.Symbol.DisplayName);
    }

    internal void ValidateSearchAllStatement(BoundSearchAllStatement stmt, int line)
    {
        var (loc, span) = DiagAt(line);
        // Table must have OCCURS
        if (stmt.Table.Symbol.Occurs == null)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1202, loc, span, stmt.Table.Symbol.DisplayName);
        // Table must have KEY clause
        var occurs = stmt.Table.Symbol.Occurs;
        if (occurs != null && occurs.AscendingKeys.Count == 0 && occurs.DescendingKeys.Count == 0)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1204, loc, span, stmt.Table.Symbol.DisplayName);

        // Check 10: SEARCH ALL WHEN must be equality comparison on key fields
        foreach (var when in stmt.Whens)
        {
            if (!IsSearchAllEqualityCondition(when.Condition))
                _ctx.Diagnostics.Report(DiagnosticDescriptors.CBL1206, loc, span);
        }
    }

    /// <summary>
    /// Returns true if the condition is a valid SEARCH ALL WHEN condition:
    /// a simple equality comparison, an AND of equality comparisons,
    /// or a condition-name (level-88) check.
    /// </summary>
    internal static bool IsSearchAllEqualityCondition(BoundExpression condition)
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
}
