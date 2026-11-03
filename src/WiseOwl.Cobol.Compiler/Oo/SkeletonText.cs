// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace WiseOwl.Cobol.Compiler.Oo;

/// <summary>
/// ⛔ A PARAMETERIZED DEFINITION'S SOURCE AS A SKELETON PUBLISHES IT (ISO §9.3.12; DESIGN-external-repository §10.5): the
/// on-channel token text from <c>CLASS-ID</c> / <c>INTERFACE-ID</c> to the end marker, after text manipulation (COPY, REPLACE and
/// the directive stage have already run, so nothing of them is left to repeat), joined the way the text had it. Two tokens that
/// touched in the source touch here and every other pair is separated by one space: a picture string or a subscript is lexed by its
/// adjacency, so a join with a space between every pair would turn <c>X(10)</c> into three tokens. A comment, a line break and
/// the sequence area are gone, which no token's meaning depends on.
/// <para>The text is read back by <c>FragmentParse.ParseUnit</c> under the recorded edition and <c>&gt;&gt;COBOL-WORDS</c> map; the
/// token types are not recorded, because the same lexer and the same decisions produce them again.</para>
/// </summary>
internal static class SkeletonText
{
    /// <summary>The text of every on-channel token of <paramref name="definition"/>, in order.</summary>
    public static string Of(ParserRuleContext definition)
    {
        var text = new StringBuilder();
        IToken? previous = null;
        Visit(definition);
        return text.ToString();

        void Visit(IParseTree node)
        {
            if (node is ITerminalNode terminal)
            {
                var token = terminal.Symbol;
                if (token.Type == TokenConstants.EOF) return;
                if (previous is not null && !(previous.StopIndex >= 0 && token.StartIndex == previous.StopIndex + 1)) text.Append(' ');
                text.Append(token.Text);
                previous = token;
                return;
            }
            for (int i = 0; i < node.ChildCount; i++) Visit(node.GetChild(i));
        }
    }
}
