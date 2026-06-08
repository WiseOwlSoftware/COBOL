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

    // ── INVOKE (OO COBOL, ISO §14.9.23) ──

    /// <summary>
    /// Bind <c>INVOKE {class-name|object} "method" [RETURNING obj]</c> (slice 1). <c>"NEW"</c> on a class-name
    /// target is the built-in factory (construct an instance); any other method on an object reference is an
    /// instance call. The target is syntactically a <c>dataReference</c>; NEW vs instance is decided by the
    /// method name + whether the target resolves to an object-reference data item. USING args / value RETURNING
    /// are later slices.
    /// </summary>
    internal BoundStatement? BindInvoke(CobolParserCore.InvokeStatementContext ctx)
    {
        var targetRef = ctx.invokeTarget()?.objectReference()?.dataReference();
        if (targetRef == null) return null; // SELF/SUPER/NULL targets are later slices
        string targetText = targetRef.cobolWord().GetText();

        // Method name: a literal "NEW"/"method" (slice 1; a data-name method selector is a later slice).
        var methodLit = ctx.invokeMethodName()?.literal();
        if (methodLit == null) return null;
        string methodName = methodLit.GetText().Trim('"', '\'');

        // RETURNING obj — for NEW the object-reference item that receives the instance; for an instance call the
        // data item that receives the method's RETURNING value (slice 2).
        DataSymbol? returning = ctx.invokeReturning()?.dataReference() is { } retRef
            ? _ctx.Semantic.ResolveData(retRef.cobolWord().GetText())
            : null;

        if (string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
            // INVOKE class-name "NEW" RETURNING obj — construct an instance of the target class.
            return new BoundInvokeStatement(isNew: true, className: targetText, targetObject: null,
                targetClassName: null, methodName: methodName, args: System.Array.Empty<BoundExpression>(),
                returning: returning);

        // INVOKE obj "method" USING … RETURNING … — instance invocation; the receiver's declared class drives
        // dispatch. Slice 2 USING args are data references (BY REFERENCE — the COBOL default; BY VALUE/CONTENT and
        // literal args are a later refinement).
        var args = new List<BoundExpression>();
        if (ctx.invokeUsing() is { } using_)
            foreach (var argCtx in using_.invokeArgument())
                if (argCtx.dataReference() is { } argRef
                    && _ctx.Expression.BindDataReferenceWithSubscripts(argRef) is { } boundArg)
                    args.Add(boundArg);

        var targetObject = _ctx.Semantic.ResolveData(targetText);
        return new BoundInvokeStatement(isNew: false, className: null, targetObject: targetObject,
            targetClassName: targetObject?.ObjectClassName, methodName: methodName, args: args,
            returning: returning);
    }

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

        // USING arguments. The BY REFERENCE / BY CONTENT / BY VALUE phrase is transitive: it applies
        // to every argument that follows it until another such phrase is encountered (ISO §14.8 CALL,
        // general rule 5). Before any phrase the default is BY REFERENCE. The grammar attaches a
        // phrase only to its first data-name, so bare arguments inherit the most recent explicit mode.
        var arguments = new List<BoundCallArgument>();
        if (ctx.callUsingPhrase() is { } usingCtx)
        {
            var currentMode = ParameterMode.ByReference;
            foreach (var argCtx in usingCtx.callArgument())
            {
                if (argCtx.callByReference() is { } byRef)
                {
                    currentMode = ParameterMode.ByReference;
                    var expr = _ctx.Expression.BindDataReferenceWithSubscripts(byRef.dataReference());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
                else if (argCtx.callByContent() is { } byContent)
                {
                    currentMode = ParameterMode.ByContent;
                    BoundExpression? expr = null;
                    if (byContent.dataReference() is { } dr)
                        expr = _ctx.Expression.BindDataReferenceWithSubscripts(dr);
                    else if (byContent.literal() is { } lit)
                        expr = _ctx.Expression.BindLiteral(lit);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
                else if (argCtx.callByValue() is { } byValue)
                {
                    currentMode = ParameterMode.ByValue;
                    var expr = _ctx.Expression.BindAdditiveExpression(byValue.arithmeticExpression().additiveExpression());
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
                }
                else if (argCtx.dataReference() is { } bareRef)
                {
                    // Bare argument: inherit the most recent explicit mode (transitive); default BY REFERENCE.
                    var expr = _ctx.Expression.BindDataReferenceWithSubscripts(bareRef);
                    if (expr != null)
                        arguments.Add(new BoundCallArgument(currentMode, expr));
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
        var targets = new List<BoundCancelTarget>();
        foreach (var target in ctx.cancelTarget())
        {
            if (target.literal() is { } lit)
                // CANCEL "literal" — the program-name is the literal value (static).
                targets.Add(new BoundCancelTarget(lit.GetText().Trim('"', '\''), IsDynamic: false));
            else if (target.dataReference() is { } dr)
                // CANCEL identifier — the program-name is the data item's runtime content (dynamic).
                targets.Add(new BoundCancelTarget(dr.cobolWord().GetText(), IsDynamic: true));
        }
        return new BoundCancelStatement(targets);
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
