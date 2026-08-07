// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⭐ THE ONE FRAGMENT RE-PARSE. Several places isolate a run of SOURCE TEXT and re-parse it through an isolated
/// fragment entry rule rather than growing a second expression compiler: the D18 subscript / reference-modifier
/// segment (<see cref="SubscriptExpressionFragment"/>), the D2 keyword-omitted argument list
/// (<see cref="FunctionArgFragment"/>) and the compile-time directive expression
/// (<see cref="DirectiveExpressionFragment"/>). Each needs the SAME five steps — lex the text, prime the lexer
/// for its region, normalize the token stream, parse at an edition, and return null on any syntax error — and
/// each had its own copy.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE COPIES DISAGREED, AND BOTH DISAGREEMENTS WERE DEFECTS (fix-queue PB50 + PB54). Exactly one of the
/// three applied <see cref="ZeroTokenRewriter"/>, which is what turns a figurative <c>ZERO</c> adjacent to an
/// arithmetic operator into the <c>ZERO_ARITH</c> token <c>arithmeticExpression</c> can match (§8.8.1.1 admits
/// "the figurative constant ZERO" as an arithmetic operand). The two that did not:
/// </para>
/// <list type="bullet">
/// <item><b>Subscript / ref-mod</b> — <c>E(ZERO + 1)</c> could not parse, so the D18 route returned null and the
/// reference ABORTED AT RUN TIME on legal source (PB50). The queue entry blamed a missing arm in
/// <c>RenderSegment</c>'s token switch; PB42 had already widened that switch's <c>default:</c> to route
/// everything unrenderable here, so the arm was not the cause — this omission was.</item>
/// <item><b>Keyword-omitted arguments</b> — <c>MIN(ZERO + 5, 2)</c> returned <b>0</b> while
/// <c>FUNCTION MIN(ZERO + 5, 2)</c> returned 2, because the un-rewritten <c>ZERO</c> ended one argument and
/// <c>+ 5</c> began another. A SILENT WRONG ANSWER, and §8.4.3.2 SR2 makes the two spellings the same reference
/// (PB54).</item>
/// </list>
/// <para>
/// ⚠ <paramref name="rewriteZero"/> IS AN EXPLICIT ARGUMENT, NOT A DEFAULT, so a new fragment has to answer the
/// question rather than inherit an answer by omission — which is exactly how the two defects above arose. It is
/// safe for an argument-list fragment despite §8.4.3.2.3 SR6: the rewriter keys on adjacency to an arithmetic
/// operator or a PLAIN paren, and a fragment's text is the content BETWEEN its delimiters, so a bare <c>ZERO</c>
/// argument has no adjacent operator and keeps the figurative identity §8.3.3.6.4 GR4 requires the BINDER to
/// resolve by the function's §15.3 argument type (the PB48 rule).
/// </para>
/// </remarks>
public static class FragmentParse
{
    /// <summary>Re-parse <paramref name="text"/> through <paramref name="rule"/>, or null on any syntax error
    /// from either recognizer — a partial parse is never returned, so every caller keeps its own loud posture.</summary>
    /// <param name="prime">The lexer-region prime for this fragment (<c>PrimeFunctionArgs</c>,
    /// <c>PrimeDirectiveExpr</c>, or null for the DEFAULT mode). The choice is semantic: the §8.3.3.3.2
    /// sign-adjacent literal twins shall fire inside an argument list and shall NOT fire inside a subscript,
    /// where <c>A -4</c> is the subtraction §8.7.1 makes it.</param>
    /// <param name="rewriteZero">Apply <see cref="ZeroTokenRewriter"/> to the token stream — required wherever
    /// the fragment's grammar can contain an ARITHMETIC EXPRESSION, since §8.8.1.1 admits the figurative ZERO as
    /// an operand and only the rewrite makes it matchable.</param>
    public static T? Parse<T>(string text, EditionInfo edition, System.Action<CobolLexer>? prime,
        bool rewriteZero, System.Func<CobolParserCore, T> rule) where T : class
    {
        var flag = new SyntaxErrorFlag();
        var lexer = new CobolLexer(new AntlrInputStream(text));
        prime?.Invoke(lexer);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(flag);
        var tokens = new CommonTokenStream(lexer);
        if (rewriteZero) ZeroTokenRewriter.Rewrite(tokens);
        var parser = new CobolParserCore(tokens) { Edition = edition };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(flag);
        var tree = rule(parser);
        return flag.HasError ? null : tree;
    }

    /// <summary>Error-presence flag for BOTH recognizers — the parser's token errors and the lexer's character
    /// errors. It was copied verbatim into all three fragment parsers; this is the one copy.</summary>
    private sealed class SyntaxErrorFlag : BaseErrorListener, IAntlrErrorListener<int>
    {
        public bool HasError { get; private set; }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
    }
}
