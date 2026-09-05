// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <b>The &gt;&gt;COBOL-WORDS REACH invariant</b> (ISO §7.3.10; kb/Work PB250). The directive renames, removes and
/// reserves COBOL words for a compilation group, and this compiler applies it through TWO mechanisms that must
/// together cover every word the directive is allowed to name:
/// <list type="number">
///   <item>the post-lex TOKEN RETYPE (<see cref="CobolWordsRewriter"/>), which reaches a word only when the lexer
///   makes it a keyword token — so <see cref="CobolKeywordTokens"/> must know EVERY such word; and</item>
///   <item>the NAME-LEVEL resolution (<see cref="CobolWordsMap.Resolve"/> / <see cref="CobolWordsMap.Is"/>), the
///   only mechanism that can reach the §8.9/§8.10 words the lexer leaves as bare IDENTIFIERs.</item>
/// </list>
/// <para>The class summary of <c>CobolKeywordTokens</c> used to ASSERT the first half's completeness — "Every
/// reserved word and context-sensitive word is a literal lexer token" — with nothing measuring it, and the claim
/// was false for 17 words whose lexer rule carries several spellings (<c>ZERO : 'ZERO' | 'ZEROS' | 'ZEROES'</c>
/// publishes no ANTLR literal NAME) and for 88 more that are no token at all. These tests are that measurement.
/// Content-filter rule: report counts and at most a few offending words, never a list.</para>
/// </summary>
public sealed class CobolWordsReachDriftTests
{
    /// <summary>Mechanism 1 must be COMPLETE over the population the directive can NAME: every §8.9 ∪ §8.10 word
    /// the lexer grammar spells as a literal resolves to a token type through
    /// <see cref="CobolKeywordTokens.TryTokenType"/>. Derived from the grammar and the two spec tables, so a new
    /// multi-spelling rule is covered automatically rather than by a hand-maintained list — and so a future
    /// narrowing of the map's derivation fails HERE instead of silently making a directive inert.
    /// <para>Scoped to §8.9 ∪ §8.10 deliberately: the grammar also spells literals that are not COBOL words at all
    /// (the <c>B</c>/<c>BX</c>/<c>N</c> literal prefixes, the <c>E</c> exponent) or are compiler-directive words,
    /// which §7.3.10.4 GR6 puts out of the directive's reach — "A COBOL-WORDS directive does not affect any
    /// Compiler directing statements or Compiler directives." Those have no token to retype and no rule that
    /// needs one.</para></summary>
    [Fact]
    public void EveryLexedSpecWord_ResolvesToATokenType()
    {
        var literals = LexerRuleLiterals();
        Assert.True(literals.Count > 300, $"only {literals.Count} lexer word literals found — the .g4 scrape broke");

        var namable = SpecWords();
        var lexedSpecWords = literals.Where(namable.Contains).ToList();
        Assert.True(lexedSpecWords.Count > 400,
            $"only {lexedSpecWords.Count} lexed §8.9/§8.10 words — one of the inputs failed to load");

        var missing = lexedSpecWords.Where(w => !CobolKeywordTokens.TryTokenType(w, out _))
                                    .OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0,
            $"{missing.Count} lexed §8.9/§8.10 word(s) have no token type — >>COBOL-WORDS is silently inert for "
            + $"them (e.g. {string.Join(", ", missing.Take(5))})");
    }

    /// <summary>⛔ THE LEX PROBE IS LOAD-BEARING, NOT BELT-AND-BRACES — this is the assertion that fails if it is
    /// removed. ANTLR publishes a literal NAME only for a token defined by exactly ONE literal, so the words of a
    /// multi-spelling rule (<c>ZERO : 'ZERO' | 'ZEROS' | 'ZEROES'</c>, <c>PIC : 'PICTURE' | 'PIC'</c>) are absent
    /// from the vocabulary walk and reachable only by asking the lexer itself. Before that fallback existed the
    /// test above failed with 17 such words and <c>&gt;&gt;COBOL-WORDS UNDEFINE "ZERO"</c> was silently inert
    /// (kb/Work PB250).</summary>
    [Fact]
    public void MultiSpellingKeywords_AreReachedOnlyByTheLexProbe()
    {
        var vocab = CobolLexer.DefaultVocabulary;
        var literalNamed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int t = 1; t < 8192; t++)
            if (vocab.GetLiteralName(t) is ['\'', .., '\''] and { Length: >= 3 } lit)
                literalNamed.Add(lit[1..^1]);

        foreach (string w in new[] { "ZEROS", "ZEROES", "PICTURE", "HIGH-VALUES", "LOW-VALUES", "SPACES", "QUOTES" })
        {
            Assert.False(literalNamed.Contains(w),
                $"{w} now HAS a vocabulary literal name — its lexer rule was split; the claim below needs a new witness");
            Assert.True(CobolKeywordTokens.TryTokenType(w, out _),
                $"{w} resolves to no token type — the lex-probe fallback in CobolKeywordTokens was removed or broken, "
                + "and every >>COBOL-WORDS directive naming a multi-spelling keyword is now silently inert");
        }

        // The spellings of ONE rule share a token type — which is exactly why CobolWordsRewriter's de-reserve arm
        // must match the token TEXT as well: UNDEFINE "ZERO" may not de-reserve ZEROS and ZEROES with it.
        Assert.True(CobolKeywordTokens.TryTokenType("ZERO", out int zero));
        Assert.True(CobolKeywordTokens.TryTokenType("ZEROES", out int zeroes));
        Assert.Equal(zero, zeroes);
    }

    /// <summary>The lex probe and the vocabulary walk are two answers to ONE question, so where both answer they
    /// must AGREE. This is what makes the probe trustworthy as the fallback: if lexing a word standing alone did
    /// not reproduce the type the pipeline gives it, this fails.</summary>
    [Fact]
    public void LexProbe_Agrees_With_TheVocabularyWalk()
    {
        var vocab = CobolLexer.DefaultVocabulary;
        int checkedCount = 0;
        var disagree = new List<string>();
        for (int t = 1; t < 8192; t++)
        {
            string? lit = vocab.GetLiteralName(t);
            if (lit is not ['\'', .., '\''] || lit.Length < 3) continue;
            string word = lit[1..^1];
            if (word.Length == 0 || !char.IsLetter(word[0])
                || !word.All(c => char.IsLetterOrDigit(c) || c == '-')) continue;
            checkedCount++;
            Assert.True(CobolKeywordTokens.TryTokenType(word, out int got));
            if (got != t) disagree.Add(word);
        }
        Assert.True(checkedCount > 300, $"only {checkedCount} vocabulary literals — the walk broke");
        Assert.True(disagree.Count == 0,
            $"{disagree.Count} word(s) resolve to a different type than the vocabulary says (e.g. "
            + $"{string.Join(", ", disagree.Take(5))})");
    }

    /// <summary>Mechanism 2 must be REACHABLE for the rest: every §8.9 ∪ §8.10 word that is NOT a keyword token is
    /// exactly the population <see cref="CobolWordsMap.Resolve"/> exists for, and this pins that the split is real
    /// and non-empty in both directions — a change that tokenized everything, or nothing, would land here. The
    /// §8.9 ∪ §8.10 population itself is asserted against the spec by
    /// <see cref="ContextSensitiveWordsDriftTests"/> and <c>ReservedWordsDriftTests</c>.</summary>
    [Fact]
    public void SpecWordPopulation_IsPartitionedByTheTwoMechanisms()
    {
        var all = SpecWords();
        Assert.True(all.Count > 500, $"only {all.Count} §8.9 ∪ §8.10 words — a table failed to load");

        int tokenPath = all.Count(w => CobolKeywordTokens.TryTokenType(w, out _));
        int namePath = all.Count - tokenPath;
        Assert.True(tokenPath > 400, $"only {tokenPath} spec words reach the TOKEN mechanism");
        Assert.True(namePath > 0, "no spec word reaches the NAME mechanism — CobolWordsMap.Resolve would be dead code");

        // The two words whose inertness opened PB250 are on the NAME path by construction: both are §8.9
        // RESERVED (not §8.10 context-sensitive, as a binder comment used to claim) and both are deliberately
        // left as bare IDENTIFIERs so the §15.94.2 / §15.68.2 phrase parses as ordinary arguments.
        foreach (string w in new[] { "ANYCASE", "LOCALE" })
        {
            Assert.True(all.Contains(w), $"{w} missing from the §8.9 ∪ §8.10 population");
            Assert.False(CobolKeywordTokens.TryTokenType(w, out _),
                $"{w} became a lexer token — the §15 phrase detection reads it as a bare argument (IntrinsicBinder"
                + ".KeywordWordOf); tokenizing it needs an fnArgPhraseWord alternative in the same change");
            Assert.NotNull(ReservedWords.Find(w));
        }
    }

    /// <summary>The ONE rule, exercised directly on all four options (§7.3.10.4 GR2–GR5). A regression in
    /// <see cref="CobolWordsMap.Resolve"/> silently disarms every by-name classifier at once.</summary>
    [Fact]
    public void Resolve_Implements_GR2_GR3_GR4()
    {
        var map = new CobolWordsMap(
        [
            new CobolWordsOp(CobolWordsAction.Equate, "LOCALE", "LOCALITY", 0),
            new CobolWordsOp(CobolWordsAction.Undefine, "ANYCASE", null, 1),
            new CobolWordsOp(CobolWordsAction.Substitute, "LEADING", "LEFTMOST", 2),
            new CobolWordsOp(CobolWordsAction.Reserve, null, "MYWORD", 3),
        ]);

        // GR2 — literal-2 is a synonym usable in any syntax requiring literal-1; literal-1 keeps its status.
        Assert.Equal("LOCALE", map.Resolve("LOCALITY"));
        Assert.Equal("LOCALE", map.Resolve("LOCALE"));
        Assert.True(map.Is("locality", "LOCALE"));      // GR1 — case-insensitive

        // GR3 — an UNDEFINE'd word is no longer reserved and its syntax is not available.
        Assert.Null(map.Resolve("ANYCASE"));
        Assert.False(map.Is("ANYCASE", "ANYCASE"));

        // GR4 — literal-5 takes over literal-4's role AND literal-4 stops being a keyword.
        Assert.Equal("LEADING", map.Resolve("LEFTMOST"));
        Assert.Null(map.Resolve("LEADING"));

        // GR5 — RESERVE bars a user word but does not make it denote a keyword.
        Assert.Equal("MYWORD", map.Resolve("MYWORD"));
        Assert.Contains("MYWORD", map.Reserved);

        // An untouched word, and the zero-overhead empty map.
        Assert.Equal("MOVE", map.Resolve("MOVE"));
        Assert.Equal("ANYCASE", CobolWordsMap.Empty.Resolve("ANYCASE"));
        Assert.True(CobolWordsMap.Empty.Is("ANYCASE", "ANYCASE"));
    }

    /// <summary>⛔ THE DIRECTIVE IS APPLIED TO A WORD EXACTLY ONCE. The token retype already resolved every word it
    /// could reach AND re-spelled it canonically, so a second resolution of that text reads a SUBSTITUTE'd
    /// literal-4 — canonical and de-reserved at once — as "not a keyword". Measured: it lost
    /// <c>FUNCTION TRIM(X LEFTMOST)</c> after <c>SUBSTITUTE "LEADING" BY "LEFTMOST"</c>.</summary>
    [Fact]
    public void TokenIs_DoesNotResolveAWordTheRetypeAlreadyResolved()
    {
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Substitute, "LEADING", "LEFTMOST", 0)]);
        Assert.True(CobolKeywordTokens.TryTokenType("LEADING", out int leadingType));

        // What the rewriter produces for the synonym: the LEADING token type, re-spelled canonically.
        var retyped = new Antlr4.Runtime.CommonToken(leadingType) { Text = "LEADING" };
        Assert.True(CobolWordsRewriter.TokenIs(retyped, "LEADING", map));
        Assert.Equal("LEADING", CobolWordsRewriter.CanonicalWordOf(retyped, map));

        // What the rewriter produces for the de-reserved word itself: an IDENTIFIER keeping the source spelling.
        var deReserved = new Antlr4.Runtime.CommonToken(CobolKeywordTokens.IdentifierType) { Text = "LEADING" };
        Assert.False(CobolWordsRewriter.TokenIs(deReserved, "LEADING", map));
        Assert.Null(CobolWordsRewriter.CanonicalWordOf(deReserved, map));

        // And a synonym for a word the lexer does NOT tokenize still arrives as an IDENTIFIER to resolve.
        var nameMap = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Equate, "LOCALE", "LOCALITY", 0)]);
        var ident = new Antlr4.Runtime.CommonToken(CobolKeywordTokens.IdentifierType) { Text = "LOCALITY" };
        Assert.True(CobolWordsRewriter.TokenIs(ident, "LOCALE", nameMap));
    }

    /// <summary>Every COBOL word spelled as a literal anywhere in a lexer rule of <c>CobolLexer.g4</c> (comments
    /// stripped) — the ground truth for "the lexer makes this word a keyword token", independent of whether ANTLR
    /// publishes a literal NAME for it.</summary>
    private static HashSet<string> LexerRuleLiterals()
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4");
        Assert.True(File.Exists(path), $"lexer grammar missing: {path}");
        string g4 = File.ReadAllText(path);
        g4 = Regex.Replace(g4, @"//[^\r\n]*", "");
        g4 = Regex.Replace(g4, @"/\*.*?\*/", "", RegexOptions.Singleline);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(g4, @"'([A-Za-z][A-Za-z0-9_-]*)'"))
            set.Add(m.Groups[1].Value.ToUpperInvariant());
        return set;
    }

    /// <summary>The §8.9 ∪ §8.10 population, from the two generated tables.</summary>
    private static HashSet<string> SpecWords()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("reserved-words.json")));
        foreach (var e in doc.RootElement.GetProperty("words").EnumerateArray())
            if (e.GetProperty("r85").GetBoolean() || e.GetProperty("r2002").GetBoolean()
                || e.GetProperty("r2014").GetBoolean() || e.GetProperty("r2023").GetBoolean())
                set.Add(e.GetProperty("word").GetString()!);
        using var ctx = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("context-sensitive-words.json")));
        foreach (var e in ctx.RootElement.GetProperty("words").EnumerateArray())
            set.Add(e.GetProperty("word").GetString()!);
        return set;
    }
}
