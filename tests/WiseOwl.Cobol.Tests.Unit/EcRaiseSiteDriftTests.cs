// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE RULE ORDINALS <see cref="EcRaiseSite"/> PRINTS ARE RE-DERIVED HERE FROM THE STANDARD, not asserted
/// against a second copy of themselves (kb/Work PB388; CLAUDE.md rule 1 — "the failure mode is not inventing a
/// citation, it is INHERITING one").
///
/// <para>RAISE (§2023:14.9.29), GOBACK RAISING (§2023:14.9.18) and EXIT … RAISING (§2023:14.9.14) are bound by ONE path, and
/// each rule that path enforces is written down once per statement under a different ordinal. A table of those
/// ordinals is only as good as the day it was typed: this test looks each one up by the rule's OWN TEXT in
/// <c>spec-rule-catalog.json</c> and asserts the match is UNIQUE, so an edition that renumbers §2023:14.9.14.3 — or a
/// hand edit that "corrects" one of them — fails here instead of misdirecting a reader to the wrong rule.</para>
///
/// <para>The catalog is the generated enumeration of the standard's numbered rules; <c>CatalogCoverageDriftTests</c>
/// holds it honest against <c>specs/ISO_COBOL_2023.md</c>, so deriving from it is deriving from the spec.</para>
/// </summary>
public sealed class EcRaiseSiteDriftTests
{
    /// <summary>Every site the shared RAISE/RAISING path is entered from, and the statement each one is.</summary>
    public static TheoryData<string> Sites() => ["RAISE", "GOBACK", "EXIT PROGRAM", "EXIT FUNCTION", "EXIT METHOD"];

    private static EcRaiseSite SiteOf(string verb) => verb switch
    {
        "RAISE" => EcRaiseSite.Raise,
        "GOBACK" => EcRaiseSite.Goback,
        _ => EcRaiseSite.Exit(verb),
    };

    /// <summary>The syntax rules of one clause, ordinal → text (top-level rules only; a sub-item is part of its
    /// parent's text).</summary>
    private static Dictionary<int, string> SyntaxRules(string clause)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        var rules = new Dictionary<int, string>();
        foreach (var r in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            if (r.GetProperty("section").GetString() == clause
                && r.GetProperty("kind").GetString() == "SR"
                && r.GetProperty("sublist").GetInt32() == 1)
            {
                rules[r.GetProperty("ordinal").GetInt32()] = r.GetProperty("text").GetString()!;
            }
        }

        Assert.True(rules.Count > 0, $"the rule catalog carries no syntax rules for §{clause} — regenerate it "
            + "before trusting anything here");
        return rules;
    }

    /// <summary>The ONE rule of <paramref name="rules"/> whose text contains <paramref name="probe"/>. A probe
    /// that matches two rules, or none, is not evidence — it fails rather than picking one.</summary>
    private static int TheRuleSaying(Dictionary<int, string> rules, string probe, string clause)
    {
        var hits = rules.Where(kv => kv.Value.Contains(probe, StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Key).OrderBy(n => n).ToList();
        Assert.True(hits.Count == 1,
            $"§{clause}: {hits.Count} syntax rules contain \"{probe}\" ({string.Join(", ", hits)}) — the probe "
            + "no longer identifies ONE rule, so nothing derived from it is evidence");
        return hits[0];
    }

    [Theory]
    [MemberData(nameof(Sites))]
    public void Level3Rule_IsTheRuleRequiringALevel3ExceptionName(string verb)
    {
        var site = SiteOf(verb);
        var rules = SyntaxRules(site.Clause);
        Assert.Equal(TheRuleSaying(rules, "shall be a level-3 exception-name", site.Clause), site.Level3Rule);
    }

    [Theory]
    [MemberData(nameof(Sites))]
    public void ObjectRule_IsTheRuleConstrainingIdentifier1ToAnObjectReference(string verb)
    {
        var site = SiteOf(verb);
        var rules = SyntaxRules(site.Clause);
        Assert.Equal(TheRuleSaying(rules, "shall be an object reference", site.Clause), site.ObjectRule);
    }

    [Theory]
    [MemberData(nameof(Sites))]
    public void LastRule_IsTheRuleRestrictingTheLastPhrase(string verb)
    {
        var site = SiteOf(verb);
        if (site.LastRule == 0)
        {
            // RAISE has no RAISING phrase, so no LAST rule — and the standard shall not grow one behind us.
            Assert.DoesNotContain(SyntaxRules(site.Clause).Values,
                t => t.Contains("The LAST phrase may be specified", StringComparison.OrdinalIgnoreCase));
            return;
        }

        var rules = SyntaxRules(site.Clause);
        Assert.Equal(TheRuleSaying(rules, "The LAST phrase may be specified", site.Clause), site.LastRule);
    }

    /// <summary>The EC-USER requirement COBOL0717 prints is the SECOND sentence of the level-3 rule, not a
    /// rule of its own — which is why one ordinal serves both messages at the phrase-bearing statements.</summary>
    [Theory]
    [MemberData(nameof(Sites))]
    public void Level3Rule_AlsoCarriesTheEcUserRaisingPhraseRequirement(string verb)
    {
        var site = SiteOf(verb);
        string text = SyntaxRules(site.Clause)[site.Level3Rule];
        if (site.LastRule == 0)
        {
            Assert.DoesNotContain("RAISING phrase of the procedure division header", text,
                StringComparison.OrdinalIgnoreCase);   // RAISE: no such requirement anywhere in its rule
            return;
        }

        Assert.Contains("RAISING phrase of the procedure division header", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The three statements really do carry THREE different clauses — the premise the whole type
    /// exists for. If they ever collapsed to one, the threading would be ceremony and should be deleted.</summary>
    [Fact]
    public void TheThreeStatements_CiteThreeDifferentClauses()
    {
        var clauses = new[] { EcRaiseSite.Raise, EcRaiseSite.Goback, EcRaiseSite.Exit("EXIT PROGRAM") }
            .Select(s => s.Clause).ToList();
        Assert.Equal(3, clauses.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>And the ordinals differ too, which is what made the inherited citation invisible: every message
    /// named a REAL rule of a REAL clause, just not this statement's.</summary>
    [Fact]
    public void TheThreeStatements_NumberTheSameRequirementDifferently()
    {
        var level3 = new[] { EcRaiseSite.Raise, EcRaiseSite.Goback, EcRaiseSite.Exit("EXIT PROGRAM") }
            .Select(s => s.Level3Rule).ToList();
        Assert.Equal(3, level3.Distinct().Count());
    }

    /// <summary>The message subject a site prints: "RAISE" for the statement, "&lt;verb&gt; RAISING" for the phrase
    /// forms — the text every one of these diagnostics opens with.</summary>
    [Theory]
    [InlineData("RAISE", "RAISE")]
    [InlineData("GOBACK", "GOBACK RAISING")]
    [InlineData("EXIT PROGRAM", "EXIT PROGRAM RAISING")]
    [InlineData("EXIT METHOD", "EXIT METHOD RAISING")]
    public void Context_NamesTheStatementTheUserWrote(string verb, string expected)
        => Assert.Equal(expected, SiteOf(verb).Context);

    /// <summary>A citation renders as the repository writes one, sub-item included, in the edition compiled at (kb/Work PB6175).</summary>
    [Fact]
    public void Cite_RendersTheClauseAndOrdinalOfThisSite_AtEachEdition()
    {
        var exit = EcRaiseSite.Exit("EXIT PROGRAM");
        var e2023 = EditionInfo.Of(2023);
        Assert.Equal("ISO §2023:14.9.14.3 SR3", exit.Cite(e2023, EcRaiseRule.Level3));
        Assert.Equal("ISO §2023:14.9.14.3 SR5d", exit.Cite(e2023, EcRaiseRule.Object, "d"));
        Assert.Equal("ISO §2023:14.9.18.3 SR4a", EcRaiseSite.Goback.Cite(e2023, EcRaiseRule.Object, "a"));
        Assert.Equal("ISO §2014:14.9.13.2 SR2", exit.Cite(EditionInfo.Of(2014), EcRaiseRule.GlobalDeclarative));
        Assert.Equal("ISO §2014:14.9.17.2 SR1", EcRaiseSite.Goback.Cite(EditionInfo.Of(2014), EcRaiseRule.GlobalDeclarative));
        Assert.Equal("ISO §2002:14.8.17.2 SR1", EcRaiseSite.Goback.Cite(EditionInfo.Of(2002), EcRaiseRule.GlobalDeclarative));
        Assert.Equal("ISO §2002:14.8.28.2 SR1", EcRaiseSite.Raise.Cite(EditionInfo.Of(2002), EcRaiseRule.Level3));
        // GOBACK's RAISING phrase answers to EXIT PROGRAM's rules at COBOL-2002 (§2002:14.8.17.3 GR1).
        Assert.Equal("ISO §2002:14.8.13.2 SR3, by §2002:14.8.17.3 GR1",
            EcRaiseSite.Goback.Cite(EditionInfo.Of(2002), EcRaiseRule.Level3));
        // COBOL-85: EXIT PROGRAM's own GLOBAL rule; a statement or phrase it lacks cites COBOL-2002, which introduced it.
        var e85 = EditionInfo.Of(85);
        Assert.Equal("§1985:X.5.4.3 SR2", exit.Cite(e85, EcRaiseRule.GlobalDeclarative));
        Assert.Equal("ISO §2002:14.8.17.2 SR1", EcRaiseSite.Goback.Cite(e85, EcRaiseRule.GlobalDeclarative));
        Assert.Equal("ISO §2002:14.8.13.2 SR2", EcRaiseSite.Exit("EXIT FUNCTION").Cite(e85, EcRaiseRule.GlobalDeclarative));
        Assert.Equal("ISO §2002:14.8.13.2 SR3", exit.Cite(e85, EcRaiseRule.Level3));
        Assert.Throws<ArgumentOutOfRangeException>(() => EcRaiseSite.Raise.Cite(e2023, EcRaiseRule.Last));
    }

    /// <summary>The numbered syntax rules of <paramref name="clause"/> in COBOL-2002 or COBOL-2014, read from that edition's own
    /// text: the numbered paragraphs under the clause's heading, up to the next heading.</summary>
    private static Dictionary<int, string> EditionSyntaxRules(int edition, string clause)
    {
        // Each held transcription is named by a literal, as the build-targets read audit requires of every repository read.
        string spec = edition switch
        {
            2002 => TestRepo.Specs("ISO_COBOL_2002.md"),
            2014 => TestRepo.Specs("ISO_COBOL_2014.md"),
            _ => throw new ArgumentOutOfRangeException(nameof(edition), edition, "only 2002 and 2014 are read here"),
        };
        var lines = File.ReadAllLines(spec);
        int at = Array.FindIndex(lines, l => l.StartsWith('#') && l.Contains($" {clause} Syntax", StringComparison.OrdinalIgnoreCase));
        Assert.True(at >= 0, $"§{edition}:{clause}: no syntax-rules heading in {spec}");
        var rules = new Dictionary<int, string>();
        for (int i = at + 1; i < lines.Length && !lines[i].StartsWith("####", StringComparison.Ordinal); i++)   // a page marker (## Page) is no heading
            if (NumberedRule.Match(lines[i]) is { Success: true } m)
                rules[int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)] = m.Groups[2].Value;
        Assert.True(rules.Count > 0, $"§{edition}:{clause}: no numbered syntax rules read");
        return rules;
    }

    /// <summary>A rule paragraph as the transcriptions write one: <c>3\) Exception-name-1 shall be ...</c>.</summary>
    private static readonly System.Text.RegularExpressions.Regex NumberedRule = new(@"^(\d+)\\?\)\s*(.*)$");

    public static TheoryData<int, string> EarlierSites() => new()
    {
        { 2002, "RAISE" }, { 2002, "GOBACK" }, { 2002, "EXIT PROGRAM" },
        { 2014, "RAISE" }, { 2014, "GOBACK" }, { 2014, "EXIT PROGRAM" },
    };

    /// <summary>⛔ COBOL-2002 and COBOL-2014 number these rules as COBOL-2023 does (the premise of <see cref="EcRaiseSite.Cite"/>,
    /// kb/Work PB6175), re-derived from each edition's own text by the rule's words, as the 2023 ordinals are above. GOBACK at
    /// COBOL-2002 states only its GLOBAL-declarative rule, and its RAISING phrase answers to EXIT PROGRAM's (§2002:14.8.17.3 GR1).</summary>
    [Theory]
    [MemberData(nameof(EarlierSites))]
    public void EarlierEditions_NumberTheRulesAsCobol2023Does(int edition, string verb)
    {
        var site = SiteOf(verb);
        string clause = edition == 2002 ? site.Clause2002 : site.Clause2014;
        var rules = EditionSyntaxRules(edition, clause);
        if (site.GlobalDeclarativeRule != 0)
            Assert.Equal(TheRuleSaying(rules, "declarative procedure for which the GLOBAL phrase", clause), site.GlobalDeclarativeRule);
        if (site.RaisingRulesAreExitProgramsAt2002 && edition == 2002)
        {
            Assert.DoesNotContain(rules.Values, t => t.Contains("level-3 exception-name", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("the program operates as if executing an EXIT PROGRAM statement with the RAISING phrase",
                File.ReadAllText(TestRepo.Specs("ISO_COBOL_2002.md")), StringComparison.Ordinal);
            return;
        }
        Assert.Equal(TheRuleSaying(rules, "shall be a level-3 exception-name", clause), site.Level3Rule);
        Assert.Equal(TheRuleSaying(rules, "shall be an object reference", clause), site.ObjectRule);
        if (site.LastRule != 0)
            Assert.Equal(TheRuleSaying(rules, "The LAST phrase may be specified", clause), site.LastRule);
    }
}
