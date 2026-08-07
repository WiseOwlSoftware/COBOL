// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The D2 keyword-omitted argument re-parse (P7 Step 12; ISO §8.4.3.2 SR2/SR6). A keyword-omitted function
/// reference <c>name(args)</c> parses as a subscripted <c>dataReference</c> (D2 — the discriminator is
/// semantic, so a grammar alternative would be an irreducible ambiguity), which captures the argument text as
/// SUBSCRIPT-mode tokens. When the binder proves the head IS a function name (§8.4.3.2 SR6 — the left paren is
/// then ALWAYS the argument list), this re-parse runs the captured source text through the ONE
/// <c>functionArgList</c> grammar rule — the same rule, token shapes, and binding path as the FUNCTION-keyword
/// form. The fragment lexer is primed as a function-argument region (<see cref="CobolLexer.PrimeFunctionArgs"/>)
/// so the §8.3.3.3.2 sign-adjacent literal twins fire exactly as they do inside real argument parens.
/// </summary>
public static class FunctionArgFragment
{
    /// <summary>Parse function-argument text (the content BETWEEN the reference's parentheses, verbatim from
    /// the source char stream so spacing survives) to the fragment CST, or <see langword="null"/> on any
    /// syntax error — the caller reports and stays loud, never a partial parse.</summary>
    public static CobolParserCore.FunctionArgListFragmentContext? Parse(string text, EditionInfo edition) =>
        FragmentParse.Parse(text, edition, static l => l.PrimeFunctionArgs(), rewriteZero: true,
            static p => p.functionArgListFragment());
}
