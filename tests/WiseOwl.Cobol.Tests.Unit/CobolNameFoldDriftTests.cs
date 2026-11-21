// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A COBOL WORD IS COMPARED BY ONE FOLD, ANNEX C, AND BY NOTHING ELSE (kb/Work PB1402; ISO §2023:8.1.3.2 GR3 b) and
/// GR4 b)), AND A KEYWORD ON THE BASIC LETTERS ALONE (kb/Work PB4240: 4) folds extended letters in user-defined words
/// only). Every name table and every comparison of two words in EVERY project under <c>src</c> goes through
/// <see cref="CobolNames"/> (kb/Work PB1965, PB2845), every comparison of a word with a keyword through
/// <see cref="CobolKeywords"/>; .NET's ordinal-ignore-case comparison may not appear there, and
/// neither may its <c>ToUpperInvariant</c> / <c>ToLowerInvariant</c> as a word's KEY or as the word a keyword is
/// compared with (<see cref="CobolNames.UpperFold"/> is that key), because it upper-cases by the host's case mapping
/// and disagrees with Annex C on the extended letters (it makes final sigma the same letter as sigma, long s the same
/// as s, and a lowercase Cherokee syllable the same as its capital). The projects are DERIVED from the directory list, so a project added tomorrow is scanned tomorrow; the
/// exceptions are named, never listed by inclusion: a source subtree that holds no COBOL word
/// (<see cref="NotCobolWordSubtrees"/>, each with its reason) and a single line that compares a string that is not a
/// COBOL word — a file-system path, a command-line option, an environment value — which says so with the marker
/// <c>// not a COBOL word: &lt;what it is&gt;</c>, so the exception is visible where it is made.
/// The fold's own answers, and the Annex B screen's, are pinned by value below.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CobolNameFoldDriftTests
{
    private const string Marker = "// not a COBOL word:";

    /// <summary>The source subtrees (relative to <c>src</c>, with <c>/</c>) that hold NO COBOL word, each with the
    /// reason; everything else under every <c>src/WiseOwl.Cobol.*</c> project is scanned.</summary>
    private static readonly (string Subtree, string Reason)[] NotCobolWordSubtrees =
    [
        ("WiseOwl.Cobol.Runtime/Collation", "the CLDR collation data and BCP 47 locale tags the collation engine reads: locale text, not words"),
    ];

    /// <summary>The host's upper-casing standing as a word's KEY or as the word a keyword is compared with: a C#
    /// identifier derived from a word (<c>Sanitize(x).ToUpperInvariant()</c>), a word looked up in the
    /// <c>&gt;&gt;COBOL-WORDS</c> map (<c>Resolve(x.ToUpperInvariant())</c>), an item's default externalized name
    /// (<c>CobolName.ToUpperInvariant()</c>) — each is <see cref="CobolNames.UpperFold"/> (or
    /// <c>DataItem.WordIdentifier</c>) now (kb/Work PB1965) — and, since kb/Work PB2845, the same host mapping as the
    /// argument of any table operation (<c>ByWord.TryGetValue((directive, word.ToUpperInvariant()), …)</c>) or of an
    /// environment lookup (<c>GetEnvironmentVariable(name.ToUpperInvariant())</c>, kb/Work PB2990), as an
    /// indexer, and as the left side of a keyword comparison (<c>x.ToUpperInvariant() == "ON"</c>, <c>is</c>,
    /// <c>switch</c>). A <c>char.ToUpperInvariant(c)</c> is a character, not a word, and carries empty parentheses on
    /// no call. Since kb/Work PB4240's R2-1 also a word handed to <c>CobolWordsMap.Resolve</c> already folded
    /// (<c>Resolve(CobolNames.UpperFold(x))</c>): the map spells the word itself (a keyword is asked on the word as written;
    /// since kb/Work PB4420 the fold of a user-defined word no longer turns an extended letter into a basic one, so the
    /// pre-fold is also pointless).</summary>
    private static readonly System.Text.RegularExpressions.Regex HostUpperKey = new(
        @"Sanitize\([^;]*\)\.ToUpperInvariant\(|\bResolve\([^;]*\.ToUpperInvariant\(|\bResolve\(CobolNames\.UpperFold\(|\bCobolName!?\.ToUpperInvariant\("
        + @"|\b(TryGetValue|ContainsKey|Contains|TryAdd|Add|Remove|Find|Lookup|GetValueOrDefault|GetEnvironmentVariable)\([^;]*\.To(Upper|Lower)Invariant\(\)"
        + @"|\[[^\]]*\.To(Upper|Lower)Invariant\(\)\]"
        + @"|\.To(Upper|Lower)Invariant\(\)\s*(==|!=|\bis\b|\bswitch\b)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Every project under <c>src</c>, derived from the directory list.</summary>
    private static IEnumerable<string> WordProjects() =>
        Directory.EnumerateDirectories(TestRepo.Src(), "WiseOwl.Cobol.*").Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal);

    /// <summary>The source files a word comparison can be written in: C#, and the grammars, whose actions and members are
    /// C# the ANTLR tool copies into the generated lexer and parser (kb/Work PB4240: the lexer's DEFINED retype compared by
    /// ordinal-ignore-case in a .g4 action, which the scan of the generated output's directory could never reach).</summary>
    private static readonly string[] ScannedKinds = ["*.cs", "*.g4"];

    /// <summary>Every non-comment source line of every scanned project, as <c>(path from the repository root, 1-based
    /// line, text)</c>; the line carrying the <see cref="Marker"/> is not returned.</summary>
    private static IEnumerable<(string Rel, int Line, string Text)> ScannedLines()
    {
        foreach (string project in WordProjects())
            foreach (string file in ScannedKinds.SelectMany(kind =>
                         Directory.EnumerateFiles(TestRepo.Src(project), kind, SearchOption.AllDirectories)))
            {
                string rel = Path.GetRelativePath(TestRepo.Root, file);
                if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(p => p is "obj" or "bin" or "Generated")) continue;
                string fromSrc = Path.GetRelativePath(TestRepo.Src(), file).Replace('\\', '/');
                if (NotCobolWordSubtrees.Any(n => fromSrc.StartsWith(n.Subtree + "/", StringComparison.Ordinal))) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)   // prose, not a comparison
                        && !lines[i].Contains(Marker, StringComparison.Ordinal))
                        yield return (rel, i + 1, lines[i].Trim());
            }
    }

    [Fact]
    public void TheScan_ReachesEveryProject_IncludingTheRuntime()
    {
        string[] projects = [.. WordProjects()];
        foreach (string expected in new[] { "WiseOwl.Cobol.Compiler", "WiseOwl.Cobol.Frontend", "WiseOwl.Cobol.Editions", "WiseOwl.Cobol.Runtime" })
            Assert.Contains(expected, projects);
        Assert.Contains(ScannedLines(), l => l.Rel.Contains("CobolNames.cs", StringComparison.Ordinal));
        Assert.Contains(ScannedLines(), l => l.Rel.EndsWith("CobolLexer.g4", StringComparison.Ordinal));   // the grammars too
        Assert.DoesNotContain(ScannedLines(), l => l.Rel.Contains("Collation", StringComparison.Ordinal) && l.Rel.Contains("CldrLocaleLoader", StringComparison.Ordinal));
        foreach (var (subtree, _) in NotCobolWordSubtrees)
            Assert.True(Directory.Exists(TestRepo.Src(subtree.Split('/'))), $"the excluded subtree '{subtree}' no longer exists: remove its entry");
    }

    [Fact]
    public void NoHostUpperCase_AsTheKeyOfACobolWord()
    {
        var hits = ScannedLines().Where(l => HostUpperKey.IsMatch(l.Text)).Select(l => $"{l.Rel}:{l.Line}: {l.Text}").ToList();
        Assert.True(hits.Count == 0,
            "a COBOL word's upper-case key is CobolNames.UpperFold (a C# identifier: DataItem.WordIdentifier), and a word "
            + "is compared with a keyword through CobolKeywords (Same, Spelling) — never the host's "
            + "ToUpperInvariant, which joins final sigma with sigma and long s with s; "
            + $"a string that is not a COBOL word carries '{Marker} <what>':\n" + string.Join("\n", hits));
    }

    /// <summary>The one upper-case spelling: Annex C's fold with only the basic letters raised.</summary>
    [Theory]
    [InlineData("kup", "KUP")]
    [InlineData("\u212AUP", "\u212AUP")]   // the KELVIN SIGN: (212A,006B) crosses to a basic letter and is not applied (kb/Work PB4420)
    [InlineData("café", "CAFé")]  // an extended letter stays in its fold
    [InlineData("CAFÉ", "CAFé")]
    [InlineData("Xς", "Xς")]     // final sigma: no mapping at 2023 (E.2 item 14), kept apart from sigma
    [InlineData("Xσ", "Xσ")]
    [InlineData("ıTEM", "ıTEM")]   // dotless i: no mapping at 2023 (E.2 item 14)
    [InlineData("D\u0130X", "D\u0130X")]   // (0130,0069) is not applied either (kb/Work PB4420): the dotted capital I keeps itself
    public void UpperFold_IsTheFoldWithTheBasicLettersRaised(string word, string upper)
    {
        Assert.Equal(upper, CobolNames.UpperFold(word));
        Assert.True(CobolNames.Same(word, upper));
    }

    [Fact]
    public void UpperFold_ReturnsAnUpperCaseBasicWordItself()
    {
        const string basic = "WS-TOTAL-1";
        Assert.Same(basic, CobolNames.UpperFold(basic));
    }

    [Fact]
    public void NoOrdinalIgnoreCase_OverACobolWord_InAnyProject()
    {
        var hits = ScannedLines().Where(l => l.Text.Contains("OrdinalIgnoreCase", StringComparison.Ordinal))
            .Select(l => $"{l.Rel}:{l.Line}: {l.Text}").ToList();
        Assert.True(hits.Count == 0,
            "compare a COBOL word with CobolNames (Comparer / Same / StartsWith / EndsWith / Contains), never with "
            + $"OrdinalIgnoreCase; a string that is not a COBOL word carries '{Marker} <what>':\n" + string.Join("\n", hits));
    }

    [Theory]
    [InlineData("CAFÉ", "café", true)]          // (00C9,00E9)
    [InlineData("GRÜẞE", "grüße", true)]        // (1E9E,00DF) — .NET's case-insensitive comparison keeps them apart
    [InlineData("X\u212A", "xk", false)]   // KELVIN SIGN (212A,006B): not applied (kb/Work PB4420)
    [InlineData("X\u212A", "X\u212A", true)]
    [InlineData("D\u0130X", "dix", false)]   // (0130,0069): not applied (kb/Work PB4420), a name of its own as in GnuCOBOL
    [InlineData("DİX", "dİx", true)]   // ... and U+0130 itself is still one letter: only the basic letters around it differ in case
    [InlineData("D\u0130X", "DIX", false)]
    [InlineData("Xς", "Xσ", false)]   // final sigma: (03C2,03C3) deleted at 2023 (E.2 item 14)
    [InlineData("ıTEM", "ITEM", false)]    // dotless i: (0131,0069) deleted at 2023 (E.2 item 14)
    [InlineData("XᎠ", "Xꭰ", false)]   // Cherokee: not folded (G.2 item 2)
    [InlineData("ABC", "abcd", false)]
    public void Same_IsTheAnnexCFold(string a, string b, bool same)
    {
        Assert.Equal(same, CobolNames.Same(a, b));
        Assert.Equal(same, CobolNames.Comparer.Equals(a, b));
        if (same) Assert.Equal(CobolNames.Comparer.GetHashCode(a), CobolNames.Comparer.GetHashCode(b));
        Assert.Equal(same, CobolNames.Fold(a) == CobolNames.Fold(b));
    }

    [Fact]
    public void SpanOperations_FoldLikeSame()
    {
        Assert.True(CobolNames.StartsWith("ÄRGER-1", "ärger"));
        Assert.True(CobolNames.EndsWith("ZÄHLER", "hler"));
        Assert.True(CobolNames.Contains("A.MÖVE", ".möve"));
        Assert.False(CobolNames.StartsWith("AB", "abc"));
    }

    /// <summary>Below 2023 the lexer writes E.2 item 14's two letters in their edition's fold, so the one comparer
    /// gives that edition's answer; at 2023 and for a basic word the spelling is the word itself.</summary>
    [Fact]
    public void EditionSpelling_AppliesTheDeletedMappingsBelow2023()
    {
        Assert.Equal("ıTEM", CobolNames.EditionSpelling("ıTEM", 2014));   // (0131,0069): applied at no edition
        Assert.Equal("Xσ", CobolNames.EditionSpelling("Xς", 2002));
        Assert.Equal("ıTEM", CobolNames.EditionSpelling("ıTEM", 2023));
        const string basic = "WS-TOTAL";
        Assert.Same(basic, CobolNames.EditionSpelling(basic, 2014));
    }

    /// <summary>A name is formed (spaces) and written in the edition's spelling (kb/Work PB2840, PB2991), so below 2023
    /// the name — a literal in source or the value of a data item — and the word it names meet.</summary>
    [Theory]
    [InlineData("  Xς  ", 2014, "Xσ")]
    [InlineData("  Xς  ", 2023, "Xς")]
    [InlineData("ıTEM", 2002, "ıTEM")]   // (0131,0069) is applied at no edition (kb/Work PB4420)
    [InlineData("SUB-P", 2014, "SUB-P")]
    public void FormInEdition_FormsAndWritesTheEditionSpelling(string written, int edition, string expected)
    {
        Assert.Equal(expected, ExternalizedNames.FormInEdition(written, edition));
        Assert.Equal(written.Trim(' '), ExternalizedNames.Form(written));
    }

    /// <summary>An exception-name is a COBOL word compared by the Annex C fold, and its open <c>EC-USER-</c> suffix is
    /// basic characters only (§2023:14.6.13.1.1): long s has no Annex C mapping, so it is not the basic letter s
    /// (the host's upper-casing made <c>EC-USER-ſ</c> the name <c>EC-USER-S</c>, kb/Work PB2845).</summary>
    [Theory]
    [InlineData("EC-USER-S", true)]
    [InlineData("ec-user-s", true)]
    [InlineData("EC-USER-\u017F", false)]
    public void ExceptionCatalog_FoldsByAnnexC_NotByTheHostCaseMapping(string name, bool known)
    {
        Assert.Equal(known, ExceptionCatalog.TryGet(name, out _));
    }

    [Theory]
    [InlineData("CAFÉ", 2023, null)]
    [InlineData("A€", 2023, "U+20AC, which is not a character")]           // EURO SIGN: not in Annex B
    [InlineData("́AB", 2023, "U+0301 as its first character")]             // B.3 item 2 first
    [InlineData("CAFÉ", 2023, null)]                                      // a combining mark may be last
    [InlineData("AB・", 2023, "U+30FB as its last character")]               // B.3 item 3 last (E.2 item 4)
    [InlineData("AB・", 2014, null)]                                        // anywhere before 2023
    [InlineData("A・B", 2023, null)]
    [InlineData("ͺX", 2014, null)]                                         // deleted at 2023 (E.2 item 4)
    [InlineData("ͺX", 2023, "U+037A, which is not a character")]
    [InlineData("KX", 2023, null)]                                         // first from 2023 (E.3.3 item 4)
    [InlineData("KX", 2014, "U+212A as its first character")]
    public void Violation_IsTheEditionsAnnexB(string word, int edition, string? expected)
    {
        string? v = CobolCharacterRepertoire.Violation(word, edition);
        if (expected is null) Assert.Null(v);
        else Assert.Contains(expected, v);
    }

    /// <summary>A KEYWORD matches on the basic letters alone (kb/Work PB4240; ISO §2023:8.1.3.2 3) a) and 4): the Annex C fold
    /// of an extended letter governs user-defined words only, owner decision 2026-10-31): a word written with one of the
    /// three letters Annex C folds to a BASIC letter is an IDENTIFIER, never the keyword its fold spells, at every edition,
    /// and so is the primed directive expression's DEFINED. <c>#</c> in <paramref name="word"/> stands for
    /// <paramref name="codePoint"/> (written in the body, so the case names stay ASCII).</summary>
    [Theory]
    [InlineData("D#SPLAY", 0x0130, 2023, false, "IDENTIFIER")] // (0130,0069): a user word folding to display, not DISPLAY
    [InlineData("#EY", 0x212A, 2023, false, "IDENTIFIER")]     // KELVIN SIGN (212A,006B)
    [InlineData("D#SPLAY", 0x0131, 2014, false, "IDENTIFIER")] // dotless i: no keyword at any edition (its pair is applied at none, kb/Work PB4420)
    [InlineData("D#SPLAY", 0x0131, 2023, false, "IDENTIFIER")]
    [InlineData("D#SPLAY", 0x0069, 2023, false, "DISPLAY")]    // the basic i, either case, is the keyword
    [InlineData("DEF#NED", 0x0130, 2023, true, "IDENTIFIER")]  // the directive expression's DEFINED (§2023:7.3.8.4.4) too
    [InlineData("DEF#NED", 0x0131, 2014, true, "IDENTIFIER")]  // ... its source text as written
    [InlineData("def#ned", 0x0069, 2023, true, "DEFINED")]
    [InlineData("DEF#NED", 0x0049, 2023, false, "IDENTIFIER")] // ... a user word everywhere else
    public void TheLexer_MatchesAKeywordOnTheBasicLettersAlone(string word, int codePoint, int edition, bool directive, string token)
    {
        string text = word.Replace("#", char.ConvertFromUtf32(codePoint), StringComparison.Ordinal);
        var lexer = new WiseOwl.Cobol.Frontend.Generated.CobolLexer(new Antlr4.Runtime.AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.SetCaseMappingEdition(edition);
        if (directive) lexer.PrimeDirectiveExpr();
        var t = lexer.NextToken();
        Assert.Equal(token, WiseOwl.Cobol.Frontend.Generated.CobolLexer.DefaultVocabulary.GetSymbolicName(t.Type));
        Assert.Equal(CobolNames.EditionSpelling(text, edition), t.Text);   // the source's spelling, in its edition
    }

    /// <summary><see cref="CobolKeywords"/> is that reading for the stages that find a keyword in text the lexer does not
    /// see: the basic letters of either case, every other character only itself — so neither the Annex C fold (U+0130,
    /// the Kelvin sign) nor the host's case mapping (the dotless i U+0131 and the long s U+017F, which .NET's
    /// ordinal-ignore-case upper-cases to I and S) makes a keyword of a word that is not one.</summary>
    [Theory]
    [InlineData("if", "IF", true)]
    [InlineData("End-If", "END-IF", true)]
    [InlineData("#F", "IF", false, 0x0130)]
    [InlineData("#F", "IF", false, 0x0131)]
    [InlineData("#EY", "KEY", false, 0x212A)]
    [InlineData("PU#H", "PUSH", false, 0x017F)]
    [InlineData("IF-", "IF", false)]
    [InlineData("I@", "I`", false)]   // '@' | 0x20 is '`': only a LETTER pairs with its other case
    public void CobolKeywords_MatchTheBasicLettersAlone(string written, string keyword, bool same, int codePoint = 0)
    {
        string text = codePoint == 0 ? written : written.Replace("#", char.ConvertFromUtf32(codePoint), StringComparison.Ordinal);
        Assert.Equal(same, CobolKeywords.Same(text, keyword));
        Assert.Equal(same, CobolKeywords.Comparer.Equals(text, keyword));
        if (same) Assert.Equal(CobolKeywords.Comparer.GetHashCode(keyword), CobolKeywords.Comparer.GetHashCode(text));
        Assert.Equal(same, CobolKeywords.Spelling(text) == keyword);
    }

    /// <summary>The directive line reads its directive word the same way: <c>&gt;&gt;İF</c> heads no directive (the word
    /// stays as written, its basic letters raised), so the conditional-compilation stage, the reference-format stage
    /// and the directive catalog never take it for <c>&gt;&gt;IF</c> (kb/Work PB4240).</summary>
    [Fact]
    public void TheDirectiveWord_IsAKeyword_NotAFoldedWord()
    {
        string dotted = ">>" + char.ConvertFromUtf32(0x0130) + "f X DEFINED";
        Assert.True(CompilerDirectiveLine.TryParse(dotted, out var d));
        Assert.Equal(char.ConvertFromUtf32(0x0130) + "F", d.Word);
        Assert.False(CompilerDirectiveCatalog.IsDirective(d.Word));
        Assert.True(CompilerDirectiveLine.TryParse(">>end-if", out var e));
        Assert.Equal("END-IF", e.Word);
        Assert.True(CompilerDirectiveCatalog.IsDirective(e.Word));
    }

    /// <summary>⛔ THE BY-NAME CLASSIFIER'S ONE COMPARISON (kb/Work PB4240, review finding R2-1): the words the lexer leaves
    /// an IDENTIFIER (§2023:8.10's CLASSIFICATION, SEGMENT-LIMIT, STEP, …, the §15 phrase words, the SET locale categories) are
    /// recognised by their text through <see cref="CobolWordsMap.Is"/> and <see cref="CobolWordsMap.Resolve"/>, with and
    /// without a <c>&gt;&gt;COBOL-WORDS</c> directive, and they match on the basic letters alone: a word written with
    /// U+0130 or the Kelvin sign is not the keyword the Annex C fold would make of it, while a SYNONYM (a fresh user-defined
    /// word, §2023:7.3.10.3 SR4) is found by the fold.</summary>
    [Fact]
    public void TheWordsMap_MatchesAKeywordOnTheBasicLettersAlone()
    {
        string dotted = char.ConvertFromUtf32(0x0130);
        string kelvin = char.ConvertFromUtf32(0x212A);
        var none = CobolWordsMap.Empty;
        var directed = new CobolWordsMap([
            new CobolWordsOp(CobolWordsAction.Equate, "CLASSIFICATION", "KLASSE", default),
            new CobolWordsOp(CobolWordsAction.Undefine, "SEGMENT-LIMIT", null, default)]);
        foreach (var map in new[] { none, directed })
        {
            Assert.True(map.Is("classification", "CLASSIFICATION"));
            Assert.False(map.Is("CLASS" + dotted + "F" + dotted + "CAT" + dotted + "ON", "CLASSIFICATION"));
            Assert.False(map.Is("CLASS" + dotted + "FICATION", "CLASSIFICATION"));
            Assert.False(map.Is(kelvin + "EY", "KEY"));
            Assert.Equal("DISPLAY", map.Resolve("display"));
            Assert.Equal("D" + dotted + "SPLAY", map.Resolve("d" + dotted + "splay"));
        }
        Assert.True(directed.Is("klasse", "CLASSIFICATION"));
        Assert.True(directed.Is("KLASSE", "CLASSIFICATION"));
        Assert.False(directed.Is("SEGMENT-LIMIT", "SEGMENT-LIMIT"));
        Assert.False(directed.Is("segment-l" + dotted + "m" + dotted + "t", "SEGMENT-LIMIT"));
        Assert.Null(directed.Resolve("segment-limit"));
        Assert.Equal("SEGMENT-L" + dotted + "M" + dotted + "T", directed.Resolve("segment-l" + dotted + "m" + dotted + "t"));
    }

    /// <summary>The token twin of <see cref="TheWordsMap_MatchesAKeywordOnTheBasicLettersAlone"/>
    /// (<c>CobolParserCoreBase.Word</c> asks it for every text predicate of the parser).</summary>
    [Fact]
    public void TheTokenTwin_MatchesAKeywordOnTheBasicLettersAlone()
    {
        string dotted = char.ConvertFromUtf32(0x0130);
        IToken Word(string text) => new CommonToken(CobolKeywordTokens.IdentifierType, text);
        foreach (var map in new[] { CobolWordsMap.Empty, new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Undefine, "SEGMENT-LIMIT", null, default)]) })
        {
            Assert.True(CobolWordsRewriter.TokenIs(Word("classification"), "CLASSIFICATION", map));
            Assert.False(CobolWordsRewriter.TokenIs(Word("CLASS" + dotted + "FICATION"), "CLASSIFICATION", map));
            Assert.Equal("L" + dotted + "M" + dotted + "T", CobolWordsRewriter.CanonicalWordOf(Word("l" + dotted + "m" + dotted + "t"), map));
        }
    }

    /// <summary>A text stage, the directive catalog and the directive-line parse compare a word with a KEYWORD written as a
    /// literal through <see cref="CobolKeywords"/>, never through the fold of a user-defined word (kb/Work PB4240): a
    /// <c>CobolNames</c> comparison whose argument is an upper-case keyword literal, or a <c>CobolNames.UpperFold</c>
    /// switched over or matched against one. A line that compares a word the rules fold on purpose says so with the marker
    /// <c>// Annex C: &lt;why&gt;</c>.</summary>
    [Fact]
    public void NoKeywordComparedByTheFold_InTheTextStages()
    {
        var keywordByFold = new System.Text.RegularExpressions.Regex(
            // A keyword reaches the comparison as a literal ("DISPLAY", "LC_", "IDENTIFICATION DIVISION") or as the
            // parameter a helper is handed (keyword, word): the second is how the first review round's R2-1 got through.
            @"CobolNames\.(Same|StartsWith|EndsWith|Contains)\([^;]*(""[A-Z][A-Z0-9_ -]*""|,\s*(keyword|word)\s*\))|CobolNames\.UpperFold\([^;]*\)\s*(is\s+(var\s+\w+\s*&&\s*\w+\s+is\s+)?""|==\s*""|switch)");
        var hits = ScannedLines()
            .Where(l => l.Rel.Replace('\\', '/') is var r
                        && (r.StartsWith("src/WiseOwl.Cobol.Frontend/", StringComparison.Ordinal)
                            || r.StartsWith("src/WiseOwl.Cobol.Editions/", StringComparison.Ordinal)))
            .Where(l => keywordByFold.IsMatch(l.Text) && !l.Text.Contains("// Annex C:", StringComparison.Ordinal))
            .Select(l => $"{l.Rel}:{l.Line}: {l.Text}").ToList();
        Assert.True(hits.Count == 0,
            "a keyword is matched on the basic letters alone (CobolKeywords.Same / StartsWith / Spelling / Comparer; ISO "
            + "§2023:8.1.3.2 4) folds extended letters in user-defined words only); a word folded on purpose carries "
            + "'// Annex C: <why>':\n" + string.Join("\n", hits));
    }

    /// <summary>A supplementary-plane pair (Adlam, added at 2023 by E.3.3 items 5 and 6) — built in the body, because
    /// a test-case NAME holding a surrogate pair is listed and reported in two spellings, which breaks the gate's
    /// population check.</summary>
    [Fact]
    public void SupplementaryPlaneLetters_FoldAndScreen()
    {
        string upper = char.ConvertFromUtf32(0x1E900) + char.ConvertFromUtf32(0x1E901);
        string lower = char.ConvertFromUtf32(0x1E922) + char.ConvertFromUtf32(0x1E923);
        Assert.True(CobolNames.Same(upper, lower));
        Assert.Equal(CobolNames.Comparer.GetHashCode(upper), CobolNames.Comparer.GetHashCode(lower));
        Assert.Null(CobolCharacterRepertoire.Violation(upper, 2023));
        Assert.Contains("U+1E900, which is not a character", CobolCharacterRepertoire.Violation(upper, 2014));
    }

    /// <summary>§2023:8.3.2.1's 63 characters are counted per character: a supplementary letter is one, a combining
    /// character is one of its own (§2023:8.1.3.2 GR4 c).</summary>
    [Fact]
    public void WordLength_CountsCharacters_NotUtf16Units()
    {
        string adlam63 = string.Concat(Enumerable.Repeat("\U0001E900", 63));
        Assert.Equal(63, CobolCharacterRepertoire.LengthInCharacters(adlam63));
        Assert.Null(CobolWordRule.LengthViolation(adlam63, 2023));
        Assert.NotNull(CobolWordRule.LengthViolation(adlam63 + "\U0001E900", 2023));
        Assert.Equal(5, CobolCharacterRepertoire.LengthInCharacters("CAFÉ"));
    }
}
