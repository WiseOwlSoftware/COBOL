// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⛔ THE ONE CLASSIFICATION OF THE LEXER'S TOKENS FOR THE SOURCE LISTING (DESIGN-listing.md section 7.2; kb/Work PB5949). The
/// listing colours its source from what the compiler READ, never from a pattern over the text: each token of the final stream of
/// the listed pass (after every retype: the §2023:8.9 reservation gate's, the COBOL-WORDS directive's, the debugging lines', the
/// embedded blocks') is classified by its TYPE (<see cref="FixedClassOf"/>), refined by the parse that read it (the rule that
/// holds it: a name slot, a figurative constant, a level-number, a comment-entry) and, for a word the compiler read as a keyword,
/// by the reserved words of the EDITION IN FORCE. So a word a later edition reserves, written as a name in a 1985 program, is the
/// gate's <c>IDENTIFIER</c> in a name slot and is a user-defined word, and the same word read as a keyword at 2002 is reserved.
/// <para>Every token type the lexer or a rewriter makes has its answer: <see cref="FixedClassOf"/> names the types that are no
/// word, and every other type is a keyword token (<c>SourceTextClassDriftTests</c> proves each from the vocabulary), so a new token
/// kind is classified by construction or turns the test red.</para>
/// </summary>
internal static class SourceTextClassifier
{
    /// <summary>The class a token of <paramref name="type"/> has whatever reads it, or null for a WORD: an <c>IDENTIFIER</c> or a
    /// keyword token, whose class the parse and the edition decide (<see cref="Classify"/>). A switch over every type that is no
    /// word; <c>SourceTextClassDriftTests</c> holds that each other type is a keyword token.</summary>
    internal static SourceTextClass? FixedClassOf(int type) => type switch
    {
        CobolLexer.STRINGLIT or CobolLexer.HEXLIT => SourceTextClass.AlphanumericLiteral,
        CobolLexer.NATLIT => SourceTextClass.NationalLiteral,
        CobolLexer.BOOLLIT => SourceTextClass.BooleanLiteral,
        CobolLexer.INTEGERLIT or CobolLexer.DECIMALLIT or CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT
            or CobolLexer.SIGNED_INTEGERLIT or CobolLexer.SIGNED_DECIMALLIT or CobolLexer.FN_SIGNED_INTEGERLIT
            or CobolLexer.FN_SIGNED_DECIMALLIT or CobolLexer.FN_SIGNED_COMMA_DECIMALLIT or CobolLexer.FN_SIGNED_FLOATLIT
            or CobolLexer.FN_SIGNED_COMMA_FLOATLIT => SourceTextClass.NumericLiteral,
        CobolLexer.PIC_STRING => SourceTextClass.Picture,
        CobolParserCore.ZERO_ARITH => SourceTextClass.FigurativeConstant,
        CobolLexer.DOT or CobolLexer.COMMA or CobolLexer.SEMICOLON or CobolLexer.FNARG_SEPARATOR or CobolLexer.LPAREN
            or CobolLexer.RPAREN or CobolLexer.FNARG_LPAREN or CobolLexer.FNARG_RPAREN or CobolLexer.REF_LPAREN
            or CobolLexer.REF_RPAREN or CobolLexer.COLON => SourceTextClass.Separator,
        // The space is a separator too (§2023:8.3.5), but the lexer skips it, so no stretch of text is classed by it.
        CobolLexer.WS or CobolLexer.PIC_WS or CobolLexer.COMMENT_END => SourceTextClass.Separator,
        CobolLexer.POWER or CobolLexer.LTEQUAL or CobolLexer.GTEQUAL or CobolLexer.NOTEQUAL or CobolLexer.LT or CobolLexer.GT
            or CobolLexer.EQUALS or CobolLexer.PLUS or CobolLexer.MINUS or CobolLexer.STAR or CobolLexer.SLASH
            or CobolLexer.COLONCOLON or CobolLexer.AMPERSAND => SourceTextClass.Operator,
        // Skipped by the lexer; an inline comment reaches the listing through the lexer's record of where each begins.
        CobolLexer.COMMENT_START or CobolLexer.COMMENT_TEXT or CobolLexer.PIC_COMMENT => SourceTextClass.Comment,
        // A debugging line's carrier, which the reference format writes in place of its 'D' indicator: it stands before the
        // line's first origin span, so it is placed on no physical character.
        CobolLexer.DEBUG_LINE or CobolLexer.DEBUG_LINE_IN_REGION => SourceTextClass.Indicator,
        CobolParserCore.EMBEDDED_BLOCK => SourceTextClass.EmbeddedText,
        CobolLexer.ANY_CHAR => SourceTextClass.Unrecognized,
        _ => null,
    };

    /// <summary>The keyword token types the vocabulary publishes no spelling for: DEFINED, a keyword only inside a directive
    /// expression (the lexer retypes the word there), and PIC_IS, the IS of a PICTURE clause in the PICTURE mode. With the types
    /// the vocabulary spells, they are every word type <see cref="FixedClassOf"/> leaves to the parse (the drift test's proof).</summary>
    internal static readonly IReadOnlySet<int> UnspelledKeywords = new HashSet<int> { CobolLexer.DEFINED, CobolLexer.PIC_IS };

    /// <summary>The classified stretches of the listed pass's resultant <paramref name="text"/>: one per token of
    /// <paramref name="tokens"/> (the final stream, every channel) and one per inline comment, which runs from its
    /// <paramref name="commentStarts"/> entry to the end of its line. <paramref name="tree"/> is the parse of the stream (recovered
    /// or not: a token it does not hold is classified by its type and spelling alone); <paramref name="edition"/> and
    /// <paramref name="reserved"/> are the edition in force and the compilation group's reserved words.</summary>
    internal static List<SourceTextSpan> Classify(IList<IToken> tokens, IParseTree tree,
        IReadOnlyList<(int Line, int Column)> commentStarts, string text, int edition, ReservedWordSet reserved)
    {
        var read = new SourceTextClass?[tokens.Count];
        var inTree = new bool[tokens.Count];
        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is ITerminalNode terminal)
            {
                int index = terminal.Symbol.TokenIndex;
                if (index < 0 || index >= tokens.Count) continue;
                inTree[index] = true;
                read[index] = ReadAs(terminal);
                continue;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }

        var spans = new List<SourceTextSpan>(tokens.Count + commentStarts.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Type == TokenConstants.EOF || token.StartIndex < 0 || token.StopIndex < token.StartIndex) continue;
            var cls = token.Channel switch
            {
                CobolLexer.ABSENT_DEBUG_LINE => SourceTextClass.Comment,
                CobolLexer.EMBEDDED_BLOCK_TEXT => SourceTextClass.EmbeddedText,
                _ => read[i] ?? FixedClassOf(token.Type) ?? WordClass(token, inTree[i], edition, reserved),
            };
            spans.Add(new SourceTextSpan(token.Line, token.Column, token.StopIndex - token.StartIndex + 1, cls));
        }
        if (commentStarts.Count > 0)
        {
            var lineStarts = LineStarts(text);
            foreach (var (line, column) in commentStarts)
            {
                if (line < 1 || line > lineStarts.Count) continue;
                int start = lineStarts[line - 1] + column;
                int end = text.IndexOf('\n', start);
                spans.Add(new SourceTextSpan(line, column, (end < 0 ? text.Length : end) - start, SourceTextClass.Comment));
            }
        }
        return spans;
    }

    /// <summary>What the rule holding <paramref name="terminal"/> makes of it, or null when the rule says nothing of its class.</summary>
    private static SourceTextClass? ReadAs(ITerminalNode terminal) => terminal.Parent switch
    {
        CobolParserCore.CommentEntryContext => SourceTextClass.CommentEntry,
        CobolParserCore.CobolWordContext word => word.Parent is CobolParserCore.FunctionNameContext
            ? SourceTextClass.FunctionName : SourceTextClass.UserWord,
        CobolParserCore.IntrinsicKeywordNameContext => SourceTextClass.FunctionName,
        CobolParserCore.ZeroWordContext or CobolParserCore.SpaceWordContext or CobolParserCore.HighValueWordContext
            or CobolParserCore.LowValueWordContext or CobolParserCore.QuoteWordContext => SourceTextClass.FigurativeConstant,
        CobolParserCore.FigurativeConstantContext when terminal.Symbol.Type == CobolLexer.ALL => SourceTextClass.FigurativeConstant,
        CobolParserCore.LevelNumberContext => SourceTextClass.LevelNumber,
        // A procedure-name written as digits is a user-defined word (§2023:8.3.2.2), not a literal.
        CobolParserCore.ProcedureNameContext when terminal.Symbol.Type == CobolLexer.INTEGERLIT => SourceTextClass.UserWord,
        _ => null,
    };

    /// <summary>The class of a word no name slot holds. An <c>IDENTIFIER</c> outside the parse is a name; one the parse read
    /// elsewhere is a word a general format recognizes by its spelling: reserved at <paramref name="edition"/>, a context-sensitive
    /// word, or else a name the format reads by its position (a language-name, a system-name). A keyword token is reserved at the
    /// edition, or else a keyword the edition does not reserve.</summary>
    private static SourceTextClass WordClass(IToken token, bool inTree, int edition, ReservedWordSet reserved)
    {
        string word = CobolNames.UpperFold(token.Text ?? "");
        if (token.Type == CobolLexer.IDENTIFIER)
            return !inTree ? SourceTextClass.UserWord
                : reserved.RejectsAt(word, edition) ? SourceTextClass.ReservedWord
                : ContextSensitiveWords.Contains(word) ? SourceTextClass.ContextWord
                : SourceTextClass.UserWord;
        return reserved.KeywordToken.RejectsAt(word, edition) ? SourceTextClass.ReservedWord : SourceTextClass.ContextWord;
    }

    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int at = text.IndexOf('\n'); at >= 0; at = text.IndexOf('\n', at + 1)) starts.Add(at + 1);
        return starts;
    }
}
