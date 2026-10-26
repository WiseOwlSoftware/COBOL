// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY TABLE 12 [NOT] PHRASE OF EVERY STATEMENT IS THE ONE <c>conditionalPhrases</c> RULE, AND ITS OWNERS ARE TABLE 12
/// (kb/Work PB3830; owner 2026-10-26: "all COBOL paired conditional phrases should be handled grammatically in the same
/// consistent manner").
///
/// <para><b>The rule.</b> ISO §14.5.1 Table 12 lists each statement's conditional phrases, and §14.5.3.3 gives ONE
/// association rule for all of them: "Any phrase encountered is the next phrase of the most-recently preceding
/// unterminated statement with which that phrase may be syntactically associated." The grammar therefore spells every
/// phrase keyword in ONE rule (<c>conditionalPhraseHead</c>), every owning statement writes the ONE
/// <c>conditionalPhrases</c> rule, and ONE table (<see cref="ConditionalPhraseAssociation.Owners"/>) says which phrases
/// each owner's general formats print — the table the association predicate asks.</para>
///
/// <para><b>What this holds, from the spec and the grammar rather than from a list.</b> (1) Table 12's conditional-phrase
/// column, scraped from <c>specs/ISO_COBOL.md</c>, equals the owners' phrases at the latest edition, statement by
/// statement, both directions — so a statement cannot be missed and a phrase cannot be invented. (2) The grammar rules
/// that write <c>conditionalPhrases</c> are exactly the table's owners, each writing it bare (the rule matches empty, and
/// a quantified reference would move the predicate one rule deep, where ANTLR ignores a context-dependent predicate).
/// (3) No other rule spells a phrase keyword in phrase position, so no per-statement copy can come back. (4) Every
/// alternative of the head is classified by the ONE token → family map, and every family has an alternative.</para>
/// </summary>
public sealed class ConditionalPhraseDriftTests
{
    /// <summary>The [NOT]-pair kinds one Table 12 conditional-phrase cell lists (WHEN, THEN and ELSE are not in the
    /// population: they carry their own grammar).</summary>
    private static IEnumerable<ConditionalPhraseKind> KindsOfCell(string cell)
    {
        foreach (string raw in cell.Split("<br>"))
        {
            string part = raw.Trim();
            bool pair = part.StartsWith("[NOT] ", StringComparison.Ordinal);
            if (pair) part = part["[NOT] ".Length..];
            string head = part.Split(' ').FirstOrDefault(w => w is not ("ON" or "AT")) ?? "";
            ConditionalPhraseFamily? family = head switch
            {
                "SIZE" => ConditionalPhraseFamily.SizeError,
                "EXCEPTION" => ConditionalPhraseFamily.Exception,
                "OVERFLOW" => ConditionalPhraseFamily.Overflow,
                "INVALID" => ConditionalPhraseFamily.InvalidKey,
                "END" => ConditionalPhraseFamily.AtEnd,
                "END-OF-PAGE" => ConditionalPhraseFamily.EndOfPage,
                _ => null,
            };
            if (family is not { } f) continue;
            yield return new(f, Negated: false);
            if (pair) yield return new(f, Negated: true);
        }
    }

    /// <summary>The Table 12 statement names an owning grammar rule stands for: a statement rule its own name, and a
    /// phrase-carrying sub-rule (the screen positioning tail) every statement rule that references it.</summary>
    private static IEnumerable<string> StatementsOf(string ownerRule, Dictionary<string, string> grammar) =>
        ownerRule.EndsWith("Statement", StringComparison.Ordinal)
            ? [Table12StatementNames.NameOfRule(ownerRule)]
            : grammar.Where(r => r.Key.EndsWith("Statement", StringComparison.Ordinal)
                                 && Regex.IsMatch(r.Value, $@"\b{ownerRule}\b"))
                .Select(r => Table12StatementNames.NameOfRule(r.Key));

    [Fact]
    public void Table12_IsTheOwnersPhraseSet_AtTheLatestEdition()
    {
        var expected = Table12StatementNameDriftTests.ScrapeTable12Rows()
            .Select(r => (r.Name, Kinds: KindsOfCell(r.Phrase).ToHashSet()))
            .Where(r => r.Kinds.Count > 0)
            .ToDictionary(r => r.Name, r => r.Kinds, StringComparer.Ordinal);
        // Shape before content: ACCEPT … WRITE, 19 rows, every family present (a transcription reformat must fail here).
        Assert.Equal(19, expected.Count);
        Assert.Equal(Enum.GetValues<ConditionalPhraseFamily>().Order(),
            expected.Values.SelectMany(k => k).Select(k => k.Family).Distinct().Order());

        var grammar = Table12StatementNameDriftTests.ScrapeGrammarRules();
        var actual = new Dictionary<string, HashSet<ConditionalPhraseKind>>(StringComparer.Ordinal);
        foreach (var (rule, formats) in ConditionalPhraseAssociation.Owners)
            foreach (string statement in StatementsOf(CobolParserCore.ruleNames[rule], grammar))
            {
                var kinds = actual.TryGetValue(statement, out var k) ? k : actual[statement] = [];
                foreach (var f in formats.Where(f => f.Construct is null || ConstructRegistry.IsAvailable(EditionInfo.Latest, f.Construct)))
                    kinds.UnionWith(f.Phrases);
            }

        var problems = new List<string>();
        foreach (var name in expected.Keys.Union(actual.Keys).Order(StringComparer.Ordinal))
        {
            var want = expected.GetValueOrDefault(name) ?? [];
            var have = actual.GetValueOrDefault(name) ?? [];
            if (!want.SetEquals(have))
                problems.Add($"{name}: Table 12 lists [{string.Join(", ", want.Select(k => k.Words).Order())}], the "
                    + $"owners print [{string.Join(", ", have.Select(k => k.Words).Order())}]");
        }
        Assert.True(problems.Count == 0, "ConditionalPhraseAssociation.Owners drifted from ISO §14.5.1 Table 12 (kb/Work "
            + "PB3830):\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void ExactlyTheOwners_WriteTheSharedRule_AndWriteItBare()
    {
        var grammar = Table12StatementNameDriftTests.ScrapeGrammarRules();
        var writers = grammar.Where(r => r.Key != "conditionalPhrases" && Regex.IsMatch(r.Value, @"\bconditionalPhrases\b"))
            .Select(r => r.Key).Order(StringComparer.Ordinal).ToList();
        var owners = ConditionalPhraseAssociation.Owners.Keys.Select(i => CobolParserCore.ruleNames[i])
            .Order(StringComparer.Ordinal).ToList();
        Assert.True(writers.Count >= 20, $"only {writers.Count} rules write conditionalPhrases — the scrape broke");
        Assert.Equal(owners, writers);
        var quantified = writers.Where(w => Regex.IsMatch(grammar[w], @"\bconditionalPhrases\s*[?*+]")).ToList();
        Assert.True(quantified.Count == 0, "conditionalPhrases written with a quantifier in: " + string.Join(", ", quantified)
            + " — the rule matches empty; a quantified reference puts the association predicate one rule deep, where "
            + "ANTLR does not evaluate a context-dependent predicate during prediction");
    }

    /// <summary>A phrase keyword in phrase position — the shapes the 18 deleted per-statement rules spelled.</summary>
    private static readonly Regex PhraseKeyword = new(
        @"\bSIZE\s+ERROR\b|\bINVALID\s+KEY\b|\bEND_OF_PAGE\b|\bEOP\b|\bON\?\s*\(?\s*(EXCEPTION|OVERFLOW)\b"
        + @"|\bAT\?\s*END\b|\bNOT\s+(ON|AT|INVALID)\b|\bNOT\?\s+conditionalPhraseHead\b",
        RegexOptions.Compiled);

    [Fact]
    public void NoRuleButTheSharedOnes_SpellsAPhraseKeyword()
    {
        var grammar = Table12StatementNameDriftTests.ScrapeGrammarRules();
        Assert.Matches(PhraseKeyword, grammar["conditionalPhraseHead"]);   // the negative control: the pattern sees them
        var copies = grammar.Where(r => r.Key is not ("conditionalPhraseHead" or "conditionalPhrase")
                                        && PhraseKeyword.IsMatch(r.Value))
            .Select(r => $"{r.Key}: '{PhraseKeyword.Match(r.Value).Value}'").ToList();
        Assert.True(copies.Count == 0, "a Table 12 [NOT] phrase is spelled outside the shared conditionalPhrases rule "
            + "(kb/Work PB3830): " + string.Join(", ", copies) + ". Write `conditionalPhrases` at the position the general "
            + "format prints the phrases and give the rule its row in ConditionalPhraseAssociation.Owners.");
    }

    // ── Format selectors (kb/Work PB3830 branch review R1-1) ─────────────────────────────────────────────────────

    /// <summary>The [NOT]-pair families each printed general format of <paramref name="statement"/> prints, one set per
    /// format, scraped from its §14.9.n.2 figures (the figure notes, which quote phrases, are not read).</summary>
    private static List<HashSet<ConditionalPhraseFamily>> FormatFamilies(string spec, string statement)
    {
        var heading = Regex.Match(spec, $@"^#### (14\.9\.\d+) {Regex.Escape(statement)} statement\s*$", RegexOptions.Multiline);
        Assert.True(heading.Success, $"no §14.9 heading for the {statement} statement in specs/ISO_COBOL.md");
        var formats = Regex.Match(spec[heading.Index..], @"^##### 14\.9\.\d+\.2 General formats?\s*$(?<body>.*?)^##### ",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(formats.Success, $"no General format(s) subclause under §{heading.Groups[1].Value}");
        var sets = new List<HashSet<ConditionalPhraseFamily>>();
        HashSet<ConditionalPhraseFamily>? current = null;
        bool inFigure = false;
        foreach (string raw in formats.Groups["body"].Value.Split('\n'))
        {
            string line = Regex.Replace(raw.TrimEnd('\r'), "<[^>]+>", "");
            if (raw.StartsWith("<pre", StringComparison.Ordinal)) { inFigure = true; continue; }
            if (raw.StartsWith("</pre>", StringComparison.Ordinal)) { inFigure = false; continue; }
            if (!inFigure && Regex.IsMatch(line, @"^Format \d+\b")) { sets.Add(current = []); continue; }
            if (!inFigure) continue;
            if (current is null) sets.Add(current = []);
            foreach (var (family, pattern) in FamilyPatterns)
                if (Regex.IsMatch(line, pattern)) current.Add(family);
        }
        return sets;
    }

    private static readonly (ConditionalPhraseFamily Family, string Pattern)[] FamilyPatterns =
    [
        (ConditionalPhraseFamily.SizeError, @"\bSIZE\s+ERROR\b"),
        (ConditionalPhraseFamily.Exception, @"\bON\s+EXCEPTION\b"),
        (ConditionalPhraseFamily.Overflow, @"\bON\s+OVERFLOW\b"),
        (ConditionalPhraseFamily.InvalidKey, @"\bINVALID\s+KEY\b"),
        (ConditionalPhraseFamily.EndOfPage, @"\bEND-OF-PAGE\b|\bEOP\b"),
        (ConditionalPhraseFamily.AtEnd, @"\bAT\s+END\b(?!-)"),
    ];

    /// <summary>⛔ HOW EACH STATEMENT WHOSE GENERAL FORMATS PRINT DIFFERENT PHRASE BRACKETS TELLS THEM APART. Either the
    /// grammar parses the formats with different rules ("rule": each owning rule then owns one bracket), or one rule
    /// parses several formats and the syntax elements only some of them print select among them ("selector": its
    /// <see cref="ConditionalPhraseAssociation.Format.Unprinted"/>). §14.5.3.3 associates a phrase with a statement only
    /// when a format of the statement prints it beside everything the statement wrote, so a "selector" statement whose
    /// selectors are missing lets an inner statement take its enclosing statement's phrase (review R1-1: READ g NEXT took
    /// the enclosing READ's NOT INVALID KEY). The population is scraped from the spec; a statement that joins it fails
    /// here until it is classified.</summary>
    private static readonly Dictionary<string, string> FormatChoice = new(StringComparer.Ordinal)
    {
        ["ACCEPT"] = "rule",     // only Format 3 (screen) prints the pair: screenTail owns it
        ["DISPLAY"] = "rule",    // only Format 2 (screen) prints the pair: screenTail owns it
        ["DELETE"] = "rule",     // deleteStatement (record, INVALID KEY) and deleteFileStatement (file, EXCEPTION)
        ["COMPUTE"] = "selector", // Format 2 (boolean-compute) prints no SIZE ERROR: the boolean-expression selects it
        ["READ"] = "selector",    // NEXT / PREVIOUS / ADVANCING ON LOCK select Format 1, KEY IS Format 2
        ["WRITE"] = "selector",   // ADVANCING selects Format 1
    };

    [Fact]
    public void EveryStatementWithDifferingFormatBrackets_IsTold_ApartByRuleOrBySelector()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        var statements = Table12StatementNameDriftTests.ScrapeTable12Rows()
            .Where(r => KindsOfCell(r.Phrase).Any()).Select(r => r.Name).ToList();
        Assert.Equal(19, statements.Count);
        var differing = statements
            .Where(s => FormatFamilies(spec, s).Select(f => string.Join(",", f.Order())).Distinct().Count() > 1)
            .Order(StringComparer.Ordinal).ToList();
        Assert.Equal(FormatChoice.Keys.Order(StringComparer.Ordinal), differing);

        var grammar = Table12StatementNameDriftTests.ScrapeGrammarRules();
        foreach (var (statement, choice) in FormatChoice)
        {
            var owners = ConditionalPhraseAssociation.Owners
                .Where(o => StatementsOf(CobolParserCore.ruleNames[o.Key], grammar).Contains(statement)).ToList();
            Assert.True(owners.Count > 0, $"{statement}: no owning rule");
            if (choice == "selector")
                Assert.True(owners.Any(o => o.Value.Any(f => f.Unprinted.Count > 0)),
                    $"{statement}: its general formats print different phrase brackets within one grammar rule, and no "
                    + "format of its owner carries the selectors that tell them apart (ConditionalPhraseAssociation.Format.Unprinted)");
            else
                foreach (var (rule, formats) in owners)
                    Assert.True(formats.Select(f => string.Join(",", f.Phrases.Select(k => k.Family).Distinct().Order())).Distinct().Count() == 1,
                        $"{statement}: owner {CobolParserCore.ruleNames[rule]} holds more than one phrase bracket, so it is not split by rule");
        }
    }

    /// <summary>A selector is looked for among the owner's OWN syntax: a child rule, or the alternative a child bracket
    /// rule chose (<see cref="ConditionalPhraseAssociation.Selector"/>). Each selector's rule must be written that close
    /// to its owner in the grammar, or <c>Writes</c> could never see it and the format it rules out would stay open.</summary>
    [Fact]
    public void EverySelector_IsTheOwnersChild_OrItsBracketChildsAlternative()
    {
        var grammar = Table12StatementNameDriftTests.ScrapeGrammarRules();
        bool Mentions(string rule, string name) => Regex.IsMatch(grammar[rule], $@"\b{name}\b");
        int seen = 0;
        foreach (var (rule, formats) in ConditionalPhraseAssociation.Owners)
        {
            string owner = CobolParserCore.ruleNames[rule];
            var children = Regex.Matches(grammar[owner], @"\b([a-z]\w*)\b").Select(m => m.Groups[1].Value)
                .Where(grammar.ContainsKey).Distinct().ToList();
            foreach (var selector in formats.SelectMany(f => f.Unprinted))
            {
                seen++;
                string name = CobolParserCore.ruleNames[selector.Rule];
                Assert.True(Mentions(owner, name) || children.Any(c => c != "conditionalPhrases" && Mentions(c, name)),
                    $"selector {name} ('{selector.Words}') is neither a child rule of {owner} nor an alternative of one");
            }
        }
        Assert.True(seen >= 5, $"only {seen} selectors — READ, WRITE and COMPUTE carry at least five");
    }

    [Fact]
    public void EveryHeadAlternative_NamesExactlyOneFamily_AndEveryFamilyHasOne()
    {
        string body = Table12StatementNameDriftTests.ScrapeGrammarRules()["conditionalPhraseHead"];
        var vocabulary = CobolParserCore.DefaultVocabulary;
        var byName = Enumerable.Range(1, CobolParserCore._ATN.maxTokenType)
            .Where(t => vocabulary.GetSymbolicName(t) is not null)
            .ToDictionary(t => vocabulary.GetSymbolicName(t)!, t => t, StringComparer.Ordinal);
        var seen = new HashSet<ConditionalPhraseFamily>();
        foreach (string alternative in body.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var families = Regex.Matches(alternative, @"\b[A-Z][A-Z_]*\b").Select(m => m.Value)
                .Select(n => byName.TryGetValue(n, out int t) ? ConditionalPhrases.FamilyOfHeadToken(t) : null)
                .OfType<ConditionalPhraseFamily>().Distinct().ToList();
            Assert.True(families.Count <= 1, $"head alternative '{alternative}' names {families.Count} families");
            seen.UnionWith(families);
        }
        Assert.Equal(Enum.GetValues<ConditionalPhraseFamily>().Order(), seen.Order());
    }
}
