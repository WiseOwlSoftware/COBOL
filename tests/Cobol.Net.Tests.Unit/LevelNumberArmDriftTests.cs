// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SECTION-KEYED SYNTAX RULE IS ONLY AS COMPLETE AS ITS LIST OF GRAMMAR ARMS, AND THAT LIST IS A CLASSIFIER.
/// ISO §13.18.33.3 states FOUR level-number sets, one per DATA DIVISION section, and <c>LevelNumberPass</c>
/// screens them by classifying a <c>levelNumber</c> parse node from its ancestry. Two lists are load-bearing
/// there: the ENTRY rules that spell a level-number, and — for a data description entry, whose set depends on the
/// section — the SECTION rules that contain one.
/// <para>Both lists were previously not lists at all: kb/Work PB485 recorded "two parser arms reach
/// <c>levelNumber</c>" and the grammar had FOUR, while the typed-native front end screened ZERO of them, so
/// <c>78 K VALUE 5.</c> compiled clean in the strict <c>--std 2023</c> lane and aborted at run time. A classifier
/// that silently returns "no arm" for a rule it does not know reproduces exactly that failure, quietly.</para>
/// <para>So the lists are DERIVED from the grammar here rather than remembered. A new grammar rule that spells a
/// level-number, or a new section that hosts a data description entry, fails this test until <c>LevelNumberPass</c>
/// names it — which is what makes the NEXT arm automatic instead of the next silent hole.</para>
/// </summary>
public sealed class LevelNumberArmDriftTests
{
    private static string PassSource => File.ReadAllText(TestRepo.Src(Path.Combine(
        "Cobol.Net.Compiler", "Validation", "LevelNumberPass.cs")));

    /// <summary>The body of <c>LevelNumberRules.Classify</c> — the ONE switch that decides a level-number's arm.
    /// <para>⛔ Scanning the whole FILE instead was tried first and was a false green: deleting the screen arm
    /// from <c>Classify</c> left the same context type standing in the neighbouring <c>EntryName</c> switch, and
    /// the test stayed green over a hole it exists to catch (feedback_green_gates_arent_evidence — the break was
    /// injected and observed). The region is bounded so only the decision counts.</para></summary>
    private static string ClassifyBody()
    {
        string pass = PassSource;
        int start = pass.IndexOf("LevelNumberArm? Classify(", StringComparison.Ordinal);
        Assert.True(start >= 0, "LevelNumberRules.Classify not found — this test scans a region that moved");
        int end = pass.IndexOf("internal static string EntryName(", start, StringComparison.Ordinal);
        Assert.True(end > start, "the end of Classify not found — this test scans a region that moved");
        return pass[start..end];
    }

    /// <summary>The parser rules of every composite-grammar fragment, as (name, body) — comments stripped so a
    /// rule NAMED in a comment is not mistaken for a rule that references it.</summary>
    private static IEnumerable<(string Name, string Body)> ParserRules()
    {
        string grammarDir = TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar"));
        foreach (string file in Directory.EnumerateFiles(grammarDir, "*.g4", SearchOption.AllDirectories))
        {
            string text = Regex.Replace(File.ReadAllText(file), @"//[^\r\n]*", "");
            foreach (string chunk in text.Split(';'))
            {
                var m = Regex.Match(chunk, @"\A\s*(?<name>[a-z]\w*)\s*:(?<body>.*)\z", RegexOptions.Singleline);
                if (m.Success) yield return (m.Groups["name"].Value, m.Groups["body"].Value);
            }
        }
    }

    /// <summary>Grammar rules whose body references <paramref name="target"/>.</summary>
    private static List<string> RulesReferencing(string target) => ParserRules()
        .Where(r => Regex.IsMatch(r.Body, $@"\b{Regex.Escape(target)}\b"))
        .Select(r => r.Name)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToList();

    /// <summary>ANTLR's generated context type for a parser rule.</summary>
    private static string ContextType(string rule) => char.ToUpperInvariant(rule[0]) + rule[1..] + "Context";

    /// <summary>Every grammar rule that spells a level-number is classified into a §13.18.33.3 arm.</summary>
    [Fact]
    public void EveryLevelNumberBearingRule_IsClassifiedByThePass()
    {
        var hosts = RulesReferencing("levelNumber");

        // The scan must FIND the arms — an empty result would make the assertion below vacuous
        // (feedback_verdict_evidence_invariant: a run must assert its population).
        Assert.True(hosts.Count >= 4,
            $"the grammar scan found only {hosts.Count} rule(s) referencing levelNumber "
            + $"({string.Join(", ", hosts)}) — the scan itself is broken, not the classifier");

        string classify = ClassifyBody();
        var unclassified = hosts.Where(h => !classify.Contains(ContextType(h), StringComparison.Ordinal)).ToList();
        Assert.True(unclassified.Count == 0,
            $"grammar rule(s) spell a level-number that LevelNumberPass.Classify does not recognize: "
            + $"{string.Join(", ", unclassified)}.{Environment.NewLine}"
            + $"Classify returns null there, so ISO §13.18.33.3 is not enforced on that arm and an out-of-range "
            + $"level compiles clean — the PB485 shape. Add the arm to LevelNumberRules.Classify with the "
            + $"§13.18.33.3 syntax rule its section obeys.");
    }

    /// <summary>A data description entry's permitted set depends on the SECTION that contains it (SR2 for a
    /// record area, SR5 for working-storage / local-storage / linkage), so every rule that hosts one must be
    /// classified too — a section the switch does not know falls through to null and is never screened.</summary>
    [Fact]
    public void EverySectionHostingADataDescriptionEntry_IsClassifiedByThePass()
    {
        var hosts = RulesReferencing("dataDescriptionEntry");

        Assert.True(hosts.Count >= 5,
            $"the grammar scan found only {hosts.Count} rule(s) referencing dataDescriptionEntry "
            + $"({string.Join(", ", hosts)}) — the scan itself is broken, not the classifier");

        string classify = ClassifyBody();
        var unclassified = hosts.Where(h => !classify.Contains(ContextType(h), StringComparison.Ordinal)).ToList();
        Assert.True(unclassified.Count == 0,
            $"grammar rule(s) host a data description entry whose SECTION LevelNumberPass.Classify does not "
            + $"recognize: {string.Join(", ", unclassified)}.{Environment.NewLine}"
            + $"Decide which ISO §13.18.33.3 syntax rule governs that section (SR2 for an FD/SD record area, "
            + $"SR5 for working-storage / local-storage / linkage) and add it to the inner switch.");
    }

    /// <summary>The four permitted sets are written ONCE, in the §13.18.33.3 table. Four copies of "is this
    /// level legal here" spread across DataBinder, DataBinder.Reports and ScreenFacility is precisely the shape
    /// that carried zero copies for as long as nobody counted (feedback_one_rule_one_place).</summary>
    [Fact]
    public void TheLevelNumberRuleIsWrittenOnce()
    {
        string pass = PassSource;
        Assert.Equal(4, Regex.Matches(pass, @"\[LevelNumberArm\.\w+\] = new\(").Count);
        Assert.Single(Regex.Matches(pass, @"internal bool Permits\(int level\)"));

        // No OTHER compiler source may re-decide the permitted set: a second range test would be a second rule.
        string validation = TestRepo.Src(Path.Combine("Cobol.Net.Compiler"));
        var offenders = Directory
            .EnumerateFiles(validation, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("LevelNumberPass.cs", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal))
            // A file that cites the clause AND names the pass is a POINTER (DataBinder's read of the level
            // number documents that the screen already ran); one that cites it WITHOUT naming the pass is a
            // second copy of the rule.
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"13\.18\.33\.3")
                        && !File.ReadAllText(f).Contains("LevelNumberPass", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            $"file(s) outside LevelNumberPass.cs decide ISO §13.18.33.3 for themselves: "
            + $"{string.Join(", ", offenders)} — the level-number sets belong in ONE place; a second copy is how "
            + $"one arm gets fixed and the others do not. A pointer comment naming LevelNumberPass is fine.");
    }
}
