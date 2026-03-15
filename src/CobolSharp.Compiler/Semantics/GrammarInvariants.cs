// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Generated;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Debug-time invariant checker for the COBOL sentence model.
/// Validates that no statement owns DOT and every sentence ends with DOT.
/// </summary>
public static class GrammarInvariants
{
    [Conditional("DEBUG")]
    public static void ValidateSentenceAndStatementBoundaries(ParserRuleContext root)
    {
        Walk(root);
    }

    private static void Walk(IParseTree node)
    {
        if (node is ParserRuleContext ctx)
        {
            if (ctx is CobolParserCore.SentenceContext sentence)
            {
                if (sentence.Stop?.Type != CobolLexer.DOT)
                    throw new InvalidOperationException(
                        $"Sentence at line {sentence.Start?.Line} does not end with DOT.");
            }
            else if (ctx is CobolParserCore.StatementContext stmt)
            {
                if (stmt.Stop?.Type == CobolLexer.DOT)
                    throw new InvalidOperationException(
                        $"Statement illegally ends with DOT at line {stmt.Stop.Line}, col {stmt.Stop.Column}.");
            }
        }

        for (int i = 0; i < node.ChildCount; i++)
            Walk(node.GetChild(i));
    }
}
