// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Antlr4.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// CALL/ENTRY binding: BindCall, BindCancel, BindEntry.
/// </summary>
internal sealed class CallBinder
{
    private readonly BindingContext _ctx;

    internal CallBinder(BindingContext ctx) => _ctx = ctx;

    // ── CALL ──

    internal BoundStatement? BindCall(CobolParserCore.CallStatementContext ctx)
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
            targetName = dataRefCtx.cobolWord().GetText();
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
                    var expr = _ctx.Expression.BindDataReferenceWithSubscripts(byRef.dataReference());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByReference, expr));
                }
                else if (argCtx.callByContent() is { } byContent)
                {
                    BoundExpression? expr = null;
                    if (byContent.dataReference() is { } dr)
                        expr = _ctx.Expression.BindDataReferenceWithSubscripts(dr);
                    else if (byContent.literal() is { } lit)
                        expr = _ctx.Expression.BindLiteral(lit);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByContent, expr));
                }
                else if (argCtx.callByValue() is { } byValue)
                {
                    var expr = _ctx.Expression.BindAdditiveExpression(byValue.arithmeticExpression().additiveExpression());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByValue, expr));
                }
                else if (argCtx.dataReference() is { } bareRef)
                {
                    // Bare argument without BY keyword = BY REFERENCE (default)
                    var expr = _ctx.Expression.BindDataReferenceWithSubscripts(bareRef);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(ParameterMode.ByReference, expr));
                }
            }
        }

        // RETURNING
        BoundIdentifierExpression? returningTarget = null;
        if (ctx.callReturningPhrase() is { } retCtx)
        {
            var retExpr = _ctx.Expression.BindDataReferenceWithSubscripts(retCtx.dataReference());
            returningTarget = retExpr as BoundIdentifierExpression;
        }

        // ON EXCEPTION / NOT ON EXCEPTION (independently optional per spec)
        var onException = new List<BoundStatement>();
        var notOnException = new List<BoundStatement>();
        if (ctx.callOnExceptionPhrase() is { } excCtx)
        {
            foreach (var stmt in excCtx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) onException.Add(bound);
            }
        }
        if (ctx.callNotOnExceptionPhrase() is { } notExcCtx)
        {
            foreach (var stmt in notExcCtx.statementBlock().statement())
            {
                var bound = _ctx.BindStatement(stmt);
                if (bound != null) notOnException.Add(bound);
            }
        }

        return new BoundCallStatement(targetName, isDynamic, arguments, returningTarget,
            onException, notOnException);
    }

    // ── CANCEL ──

    internal BoundCancelStatement BindCancel(CobolParserCore.CancelStatementContext ctx)
    {
        var names = new List<string>();
        foreach (var target in ctx.cancelTarget())
        {
            if (target.literal() is { } lit)
                names.Add(lit.GetText().Trim('"', '\''));
            else if (target.dataReference() is { } dr)
                names.Add(dr.cobolWord().GetText());
        }
        return new BoundCancelStatement(names);
    }

    // ── ENTRY ──

    internal BoundEntryStatement? BindEntry(CobolParserCore.EntryStatementContext ctx)
    {
        string entryName = ctx.literal().GetText().Trim('"', '\'');

        var usingNames = new List<string>();
        if (ctx.usingClause() is { } usingCtx)
        {
            var dataRefs = usingCtx.dataReferenceList()?.dataReference();
            if (dataRefs != null)
            {
                foreach (var dr in dataRefs)
                    usingNames.Add(dr.cobolWord().GetText());
            }
        }

        return new BoundEntryStatement(entryName, usingNames);
    }
}
