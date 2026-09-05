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
/// <para><b>This rewriter is HALF the mechanism, and it cannot be the other half</b> (kb/Work PB250). It can only
/// reach a word the lexer makes a keyword TOKEN; a word that lexes as a plain IDENTIFIER has no token type to
/// retype, so the retype is a silent no-op for it in BOTH directions. Measured against ISO §8.9 ∪ §8.10, 88 words
/// are in that class — ANYCASE and LOCALE among them, which is why <c>&gt;&gt;COBOL-WORDS EQUATE "LOCALE" …</c>
/// used to make TEST-NUMVAL-C reject legal source. Those words, and every intrinsic-function name, are reached at
/// the by-name classification points through <see cref="CobolWordsMap.Resolve"/> — the ONE rule both halves
/// share. <see cref="CobolWordsMap.Empty"/> ⇒ a no-op (byte-identical token stream). Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.</para>
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

    /// <summary>
    /// The canonical COBOL word an ALREADY-LEXED token denotes under <paramref name="map"/> (UPPER-CASE), or null
    /// when the directive de-reserved it and it is a user-defined word now.
    /// <para>⛔ THE ONE PLACE THAT KNOWS WHICH WORDS THIS REWRITER ALREADY RESOLVED, and the reason a bare
    /// <see cref="CobolWordsMap.Resolve"/> on token text is WRONG. <see cref="Rewrite"/> applies the directive to
    /// every word it can reach and RE-SPELLS it canonically, so a token that is not an <c>IDENTIFIER</c> has been
    /// resolved already: resolving it a second time reads a SUBSTITUTE'd literal-4 — which is canonical AND
    /// de-reserved — as "not a keyword", and loses the synonym literal-5 the user legally wrote (measured:
    /// <c>SUBSTITUTE "LEADING" BY "LEFTMOST"</c> then <c>FUNCTION TRIM(X LEFTMOST)</c>). Only an IDENTIFIER can
    /// still carry an unresolved word — either a synonym for a keyword the lexer does not tokenize, or a
    /// de-reserved keyword this rewriter just turned into one. The directive is applied to a word EXACTLY ONCE
    /// (kb/Work PB250).</para>
    /// </summary>
    public static string? CanonicalWordOf(IToken? tok, CobolWordsMap map)
        => tok is null ? null
         : map.IsEmpty || tok.Type != CobolKeywordTokens.IdentifierType ? tok.Text.ToUpperInvariant()
         : map.Resolve(tok.Text.ToUpperInvariant());

    /// <summary>True when an already-lexed <paramref name="tok"/> denotes <paramref name="keyword"/> — the
    /// token-aware twin of <see cref="CobolWordsMap.Is"/>, carrying the same once-only guarantee as
    /// <see cref="CanonicalWordOf"/>. Allocation-free when the group has no directive.</summary>
    public static bool TokenIs(IToken? tok, string keyword, CobolWordsMap map)
    {
        if (tok is null) return false;
        if (map.IsEmpty || tok.Type != CobolKeywordTokens.IdentifierType)
            return string.Equals(tok.Text, keyword, StringComparison.OrdinalIgnoreCase);
        return map.Is(tok.Text, keyword);
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
            else if (deReservedTypes.Contains(tok.Type) && map.DeReserved.Contains(tok.Text))
            {
                // A de-reserved keyword becomes a user word — keep the source spelling (it is now the data-name).
                // ⛔ THE TEXT TEST IS LOAD-BEARING, NOT BELT-AND-BRACES (kb/Work PB250). One token type can carry
                // SEVERAL COBOL words (`ZERO : 'ZERO' | 'ZEROS' | 'ZEROES'`, `PIC : 'PICTURE' | 'PIC'`), while
                // GR3/GR4 de-reserve exactly the ONE word literal-3/literal-4 names — so retyping by TYPE alone
                // would strip ZEROS and ZEROES of their reservation on an `UNDEFINE "ZERO"`, and PICTURE on an
                // `UNDEFINE "PIC"`. Invisible until CobolKeywordTokens learned to resolve multi-spelling rules.
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
