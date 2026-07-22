// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The reverse map <c>keyword word → ANTLR lexer token type</c>, derived once from
/// <see cref="CobolLexer.DefaultVocabulary"/>. Every reserved word and context-sensitive word is a literal lexer
/// token (<c>MOVE : 'MOVE'</c>; <c>caseInsensitive = true</c>), so its uppercase spelling maps to a token type.
/// This is the single source the <c>&gt;&gt;COBOL-WORDS</c> post-lex token rewriter uses to retype IDENTIFIER
/// tokens to a keyword type (EQUATE/SUBSTITUTE) and keyword tokens back to IDENTIFIER (UNDEFINE/SUBSTITUTE), and
/// the compiler's SR3/SR4 category validation uses to decide whether a word is a reserved/context word. Intrinsic
/// function names are NOT here — they are IDENTIFIERs (see <c>IntrinsicCatalog</c>), a separate category.
/// </summary>
public static class CobolKeywordTokens
{
    private static readonly Dictionary<string, int> ByWord = Build();

    private static Dictionary<string, int> Build()
    {
        var vocab = CobolLexer.DefaultVocabulary;
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        // GetLiteralName returns null for out-of-range indices (and for rule-defined tokens), so an upper bound
        // generously above the lexer's token count is safe — the COBOL lexer has a few hundred tokens.
        for (int t = 1; t < 8192; t++)
        {
            string? lit = vocab.GetLiteralName(t);   // e.g. "'MOVE'"; null for rule-defined tokens (IDENTIFIER, …)
            if (lit is not ['\'', .., '\''] || lit.Length < 3) continue;
            string word = lit[1..^1];
            // A COBOL word starts with a letter and contains only letters/digits/hyphens; this excludes the
            // operator/punctuation literals ('(', '*>', …) and figurative/special-character tokens.
            if (word.Length == 0 || !char.IsLetter(word[0])
                || !word.All(c => char.IsLetterOrDigit(c) || c == '-')) continue;
            map[word] = t;   // keyword literals are unique, so no meaningful collision
        }
        return map;
    }

    /// <summary>The lexer token type for a reserved/context-sensitive <paramref name="word"/> (case-insensitive),
    /// or false when the word is not a keyword lexer token.</summary>
    public static bool TryTokenType(string word, out int type) => ByWord.TryGetValue(word, out type);

    /// <summary>True when <paramref name="word"/> is a reserved word or context-sensitive word (a keyword lexer
    /// token). False for user words and for intrinsic-function-only names.</summary>
    public static bool IsKeyword(string word) => ByWord.ContainsKey(word);

    /// <summary>The IDENTIFIER token type — the user-word type the rewriter retypes UNDEFINE/SUBSTITUTE keywords
    /// to, and the source type it retypes EQUATE/SUBSTITUTE synonyms from.</summary>
    public static int IdentifierType => CobolLexer.IDENTIFIER;
}
