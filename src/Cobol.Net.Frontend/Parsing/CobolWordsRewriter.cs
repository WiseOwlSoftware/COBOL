// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The post-lex <c>&gt;&gt;COBOL-WORDS</c> token rewriter (ISO §7.3.10.4 GR2/GR3/GR4) — runs between lexing and
/// parsing (beside <see cref="ZeroTokenRewriter"/> in <c>Frontend.LexAndParse</c>), retyping tokens per the
/// <see cref="CobolWordsMap"/> so the static ANTLR lexer needs no per-group regeneration (the owner's recorded
/// direction). Two disjoint retypes:
/// <list type="bullet">
/// <item>SYNONYM (EQUATE literal-2 / SUBSTITUTE literal-5): an <c>IDENTIFIER</c> whose text is a synonym is
/// retyped to the canonical reserved/context word's token type — so it matches wherever that keyword is required.</item>
/// <item>DE-RESERVED (UNDEFINE literal-3 / SUBSTITUTE literal-4): every token of the de-reserved word's keyword
/// type is retyped to <c>IDENTIFIER</c> — so the word drops out of its keyword syntax and is usable as a user
/// word.</item>
/// </list>
/// A word with no keyword token type (a pure intrinsic-function-name) is left alone — the binder resolves those
/// through the map. <see cref="CobolWordsMap.Empty"/> ⇒ a no-op (byte-identical token stream). Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public static class CobolWordsRewriter
{
    /// <summary>The KEYWORD token types the directive DE-RESERVES (UNDEFINE literal-3 / SUBSTITUTE literal-4) — the
    /// set the lexer must treat as a data-name trigger (so a following <c>(</c> opens a SUBSCRIPT before the
    /// post-lex retype runs). Empty when no de-reserved word is a keyword lexer token.</summary>
    public static IReadOnlySet<int> DeReservedTokenTypes(CobolWordsMap map)
    {
        var types = new HashSet<int>();
        if (!map.IsEmpty)
            foreach (string word in map.DeReserved)
                if (CobolKeywordTokens.TryTokenType(word, out int kt))
                    types.Add(kt);
        return types;
    }

    /// <summary>Retype the filled token stream per <paramref name="map"/>. Must run after
    /// <see cref="CommonTokenStream.Fill"/> and before parsing.</summary>
    public static void Rewrite(CommonTokenStream tokenStream, CobolWordsMap map)
    {
        if (map.IsEmpty) return;

        // SYNONYM: the synonym IDENTIFIER text → the canonical word's token type (only when the canonical is a
        // reserved/context keyword; a canonical intrinsic-function name has no token type and is handled in the binder).
        var synonymToType = new Dictionary<string, (int Type, string Canonical)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (synonym, canonical) in map.Synonyms)
            if (CobolKeywordTokens.TryTokenType(canonical, out int kt))
                synonymToType[synonym] = (kt, canonical);

        // DE-RESERVED: every token of one of these keyword types → IDENTIFIER (the word is a user word now).
        var deReservedTypes = DeReservedTokenTypes(map);

        if (synonymToType.Count == 0 && deReservedTypes.Count == 0) return;

        tokenStream.Fill();
        var tokens = tokenStream.GetTokens();
        if (tokens is null || tokens.Count == 0) return;
        int idType = CobolKeywordTokens.IdentifierType;

        for (int i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            if (tok.Channel != Lexer.DefaultTokenChannel) continue;   // never touch hidden/whitespace tokens
            if (tok.Type == idType)
            {
                // A synonym written as a user word takes over its canonical keyword — spell it canonically so any
                // downstream GetText() sees the real keyword (the parser matches by type; text is for fidelity).
                if (synonymToType.TryGetValue(tok.Text, out var target))
                    Retype(tokens, i, target.Type, target.Canonical);
            }
            else if (deReservedTypes.Contains(tok.Type))
            {
                // A de-reserved keyword becomes a user word — keep the source spelling (it is now the data-name).
                Retype(tokens, i, idType, tok.Text);
            }
        }
        tokenStream.Seek(0);
    }

    private static void Retype(IList<IToken> tokens, int i, int newType, string text)
    {
        var original = tokens[i];
        tokens[i] = new CommonToken(original) { Type = newType, Text = text };
    }
}
