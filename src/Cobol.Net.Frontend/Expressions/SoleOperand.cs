// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

using Core = CobolParserCore;

/// <summary>⛔ THE ONE "is this arithmetic expression a single unparenthesized primary?" DESCENT, for BOTH
/// assemblies (kb/Work PB172, completed by PB224).
///
/// <para>The reduction is load-bearing in three different rules at once: §8.8.4.7.3 SR2's "a single data item …
/// and that name shall not be enclosed in parentheses", §8.8.1.1's sole-vs-compound operand boundary, and the
/// compile-time-directive operand of §7.3.11 / §13.10.3. Any operator, unary sign, or enclosing parenthesis
/// gives some node on the way down more than one child and stops the descent — which is exactly what all three
/// rules mean by "single".</para>
///
/// <para>⚠ IT REPLACED SIX COPIES OF ITSELF, and the last two were in DIFFERENT ASSEMBLIES, which is why the
/// first collapse (PB172, four copies inside <c>ConditionBinder</c>) could not finish the job: a compiler-side
/// helper is unreachable from <c>CompileTimeExpressionEvaluator</c>, which lives one layer down in
/// <c>Cobol.Net.Frontend</c>. The descent operates on PARSE TREES and on nothing else, so the frontend is where
/// it belongs and both layers can read it. The fifth copy was <c>IntrinsicBinder.SoleDataReference</c>, an
/// independently-written tier-by-tier list-pattern version that agreed with the others by coincidence rather
/// than by construction; the sixth was this file's former twin inside the compile-time evaluator.</para></summary>
public static class SoleOperand
{
    /// <summary>The sole unparenthesized <c>primaryExpression</c> of <paramref name="expr"/>, or
    /// <see langword="null"/> when the expression is compound, signed, or parenthesized (or absent).</summary>
    public static Core.PrimaryExpressionContext? Primary(Core.ArithmeticExpressionContext? expr)
    {
        IParseTree? n = expr;
        if (n is null) return null;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return (Core.PrimaryExpressionContext)n;
    }

    /// <summary>The sole <c>dataReference</c> primary of <paramref name="expr"/>, or null.</summary>
    public static Core.DataReferenceContext? DataRef(Core.ArithmeticExpressionContext? expr) =>
        Primary(expr)?.dataReference();
}
