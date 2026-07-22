// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// The <see cref="BooleanExpressionResolver"/> grouping (ISO/IEC 1989:2023 §8.8.2 rule 7). The resolver is
/// generic over the combine operations; here it is instantiated with <c>T = string</c> to render a fully
/// parenthesized form that makes the grouping directly assertable — independent of the compile-time bit-string
/// fold and the runtime bound-tree build that are its real consumers. The focus is the context-inherited shift
/// precedence (rule 7b), which a context-free grammar cannot express and which the resolver re-derives from the
/// operand/operator sequence.
/// </summary>
public sealed class BooleanExpressionResolverTests
{
    /// <summary>Render a boolean expression through the resolver: a leaf is its source text, B-NOT is <c>~x</c>,
    /// a binary op is <c>(l·op·r)</c>, and a shift is <c>[operand KIND count]</c> — so the bracketing shows the
    /// grouping the resolver chose.</summary>
    private static string Render(string text)
    {
        var flag = new ErrorFlag();
        var lexer = new CobolLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(flag);
        var parser = new Core(new CommonTokenStream(lexer)) { Edition = EditionInfo.Of(2023) };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(flag);
        Core.BooleanExpressionContext ctx = parser.booleanExpression();
        Assert.False(flag.HasError, $"parse error in boolean fragment '{text}'");
        // The whole fragment must be consumed — without an EOF anchor ANTLR would stop at the first valid
        // sub-expression, hiding a mis-parse. (A '(' after a boolean operator only lexes as a grouping paren under
        // the directive-expression lexer priming; these bare-harness cases use only leading/parenthesis-free forms.)
        Assert.Equal(TokenConstants.EOF, parser.CurrentToken.Type);

        return BooleanExpressionResolver.Resolve<string>(
            ctx,
            leaf: vo => vo.GetText(),
            not: x => $"~{x}",
            binary: (l, op, r) => $"({l}{op}{r})",
            shift: (x, suf) =>
            {
                string kind = suf.B_SHIFT_LC() is not null ? "LC"
                            : suf.B_SHIFT_RC() is not null ? "RC"
                            : suf.B_SHIFT_L() is not null ? "L"
                            : "R";
                return $"[{x} {kind} {suf.arithmeticExpression().GetText()}]";
            });
    }

    [Theory]
    // Plain binary precedence B-AND > B-XOR > B-OR, left-to-right (rules 7b/7c) — the grammar tiers already give this.
    [InlineData("A B-AND B", "(A&B)")]
    [InlineData("A B-OR B B-XOR C", "(A|(B^C))")]            // XOR binds tighter than OR
    [InlineData("A B-AND B B-OR C", "((A&B)|C)")]           // AND binds tighter than OR
    [InlineData("A B-OR B B-OR C", "((A|B)|C)")]            // equal precedence → left-to-right
    // B-NOT is the tightest (factor level, rule 7b 1st).
    [InlineData("B-NOT A B-AND B", "(~A&B)")]
    [InlineData("A B-AND B-NOT B", "(A&~B)")]
    // Parentheses evaluate first (rule 7a) and override precedence.
    [InlineData("(A B-OR B) B-AND C", "((A|B)&C)")]
    // ── The rule-7b context-inherited SHIFT precedence (the case a CFG cannot express) ──
    // A shift after B-AND inherits B-AND precedence → left-to-right → the AND happens first.
    [InlineData("A B-AND B B-SHIFT-L 2", "[(A&B) L 2]")]
    // A shift after B-OR inherits B-OR precedence → the OR happens first.
    [InlineData("A B-OR B B-SHIFT-L 2", "[(A|B) L 2]")]
    // A leading shift (no preceding operation) takes B-AND precedence (rule 7b tail).
    [InlineData("A B-SHIFT-L 2 B-AND C", "([A L 2]&C)")]
    // A parenthesized group is atomic — a following shift applies to the whole group, not just its last operand.
    [InlineData("(A B-AND B) B-SHIFT-L 2", "[(A&B) L 2]")]
    // Consecutive shifts associate left-to-right.
    [InlineData("A B-SHIFT-L 2 B-SHIFT-R 1", "[[A L 2] R 1]")]
    // A shift after B-OR then B-AND: shift inherits the immediately preceding B-AND → AND+shift bind before OR.
    [InlineData("A B-OR B B-AND C B-SHIFT-L 2", "(A|[(B&C) L 2])")]
    public void Groups_Per_Rule7(string source, string expected) => Assert.Equal(expected, Render(source));

    private sealed class ErrorFlag : BaseErrorListener, IAntlrErrorListener<int>
    {
        public bool HasError { get; private set; }
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg, RecognitionException e) => HasError = true;
    }
}
