// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Parsing;

/// <summary>
/// Post-lexer token rewriting pass that converts ZERO tokens to INTEGERLIT("0")
/// when they appear in arithmetic contexts. This avoids adding ZERO to the
/// grammar's primaryExpression rule, which causes exponential ANTLR prediction time.
///
/// Arithmetic context is detected by adjacency to arithmetic operators:
///   ZERO followed by  +, -, *, /, **   → rewrite to INTEGERLIT
///   ZERO preceded by  +, -, *, /, **   → rewrite to INTEGERLIT
///   ZERO followed by  )                → rewrite (closing a parenthesized expression)
///   ZERO preceded by  (                → rewrite (opening a parenthesized expression)
///
/// All other ZERO tokens are left unchanged — they remain figurative constants
/// for VALUE, MOVE, comparison, and other non-arithmetic contexts.
/// </summary>
internal static class ZeroTokenRewriter
{
    /// <summary>
    /// The set of token types that, when adjacent to ZERO, indicate arithmetic context.
    /// </summary>
    private static readonly HashSet<int> ArithmeticOperators = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
    };

    /// <summary>
    /// Token types that indicate ZERO is inside an expression when they precede it.
    /// Includes arithmetic operators and LPAREN (e.g., "(ZERO + 1)").
    /// </summary>
    private static readonly HashSet<int> PrecedingArithmeticContext = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
        CobolLexer.LPAREN,
    };

    /// <summary>
    /// Token types that indicate ZERO is inside an expression when they follow it.
    /// Includes arithmetic operators and RPAREN (e.g., "(1 + ZERO)").
    /// </summary>
    private static readonly HashSet<int> FollowingArithmeticContext = new()
    {
        CobolLexer.PLUS,
        CobolLexer.MINUS,
        CobolLexer.STAR,
        CobolLexer.SLASH,
        CobolLexer.POWER,
        CobolLexer.RPAREN,
    };

    /// <summary>
    /// Scans all tokens in the stream and replaces ZERO tokens that appear in
    /// arithmetic contexts with ZERO_ARITH virtual tokens (text "0").
    /// Must be called after <see cref="CommonTokenStream.Fill"/> so all tokens
    /// are available, and before parsing begins.
    /// </summary>
    public static void Rewrite(CommonTokenStream tokenStream)
    {
        // Fill forces the lexer to tokenize the entire input so we can
        // inspect the full token list. This is idempotent if already filled.
        tokenStream.Fill();

        var tokens = tokenStream.GetTokens();
        if (tokens == null || tokens.Count == 0)
            return;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Type != CobolLexer.ZERO)
                continue;

            // Look at the next non-hidden token to the right
            int nextType = GetAdjacentTokenType(tokens, i, forward: true);
            if (nextType != -1 && FollowingArithmeticContext.Contains(nextType))
            {
                ReplaceWithZeroArith(tokens, i);
                continue;
            }

            // Look at the previous non-hidden token to the left
            int prevType = GetAdjacentTokenType(tokens, i, forward: false);
            if (prevType != -1 && PrecedingArithmeticContext.Contains(prevType))
            {
                ReplaceWithZeroArith(tokens, i);
                continue;
            }
        }

        // Reset the stream position so the parser reads from the beginning.
        tokenStream.Seek(0);
    }

    /// <summary>
    /// Finds the nearest non-hidden token in the given direction and returns its type.
    /// Returns -1 if no such token exists (beginning/end of stream).
    /// </summary>
    private static int GetAdjacentTokenType(IList<IToken> tokens, int index, bool forward)
    {
        int step = forward ? 1 : -1;
        int pos = index + step;

        while (pos >= 0 && pos < tokens.Count)
        {
            var t = tokens[pos];
            if (t.Type == TokenConstants.EOF)
                return -1;
            if (t.Channel == Lexer.DefaultTokenChannel)
                return t.Type;
            pos += step;
        }

        return -1;
    }

    /// <summary>
    /// Replaces the token at the given index with a ZERO_ARITH virtual token
    /// that has text "0" but preserves the original token's position information.
    /// </summary>
    private static void ReplaceWithZeroArith(IList<IToken> tokens, int index)
    {
        var original = tokens[index];

        var synthetic = new CommonToken(original)
        {
            Type = CobolParserCore.ZERO_ARITH,
            Text = "0",
        };

        tokens[index] = synthetic;
    }
}
