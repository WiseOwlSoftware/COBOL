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
/// ⛔ AN INTRODUCTION GATE IS ONLY AS COMPLETE AS ITS LIST OF GRAMMAR SITES, AND THAT LIST WAS HAND-MAINTAINED.
/// The boolean operators are a COBOL-2002 introduction and the four boolean SHIFT operators a COBOL-2023 one
/// (ISO §8.7.2 / §8.8.2 rule 8), gated in <c>VersionConformancePass</c> at the ALTITUDE of each grammar rule
/// that hosts a top-level <c>booleanExpression</c> — per site, never per node, because the tiers nest through
/// parentheses and a per-node gate would multiply the diagnostic.
/// <para>For as long as there were two such sites the list was invisible. PB46 added a third
/// (<c>INVOKE … USING BY CONTENT boolean-expression-1</c>, §14.9.23.2) and the omission would have been silent:
/// <c>BY CONTENT B1 B-SHIFT-L 2</c> compiling clean under <c>--std 2002</c> — a 2023 construct inside a 2002
/// statement, accepted, with every gate green (feedback_edition_gate_sweep).</para>
/// <para>So the list is DERIVED from the grammar here rather than remembered. A new rule that admits a boolean
/// expression fails this test until it is either gated or explicitly adjudicated as exempt — the exemption
/// carries its reason, which is the part a comment in the pass could not enforce.</para>
/// </summary>
public sealed class BooleanExpressionGateSiteDriftTests
{
    /// <summary>Grammar rules that reference <c>booleanExpression</c> but must NOT carry a gate call, each with
    /// the reason it is exempt. Adding a name here is an adjudication, not a formality.</summary>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal)
    {
        ["booleanFactor"] =
            "the parenthesized sub-expression — a NESTED occurrence of the same expression, gated once at the "
            + "enclosing site; gating here would fire one diagnostic per level of parentheses",
        ["compileTimeOperand"] =
            "a compile-time DIRECTIVE-expression fragment (ISO §7.3.7): reachable only from the frontend's "
            + "directive re-parse and referenced by nothing in compilationUnit, so the VersionConformancePass "
            + "walk over the compilation unit never reaches it",
        ["cceRelationOrBoolean"] =
            "a constant-conditional-expression fragment (ISO §7.3.8) — the same directive re-parse as "
            + "compileTimeOperand, outside the compilation-unit walk",
    };

    /// <summary>The parser rules of every composite-grammar fragment, as (name, body) — comments stripped so a
    /// rule NAMED in a comment is not mistaken for a rule that references it (this file's own subject appears
    /// in several grammar comments).</summary>
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

    /// <summary>The <c>VersionConformancePass</c> override bodies, keyed by the rule they visit.</summary>
    private static Dictionary<string, string> GateOverrides()
    {
        string pass = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "Validation", "VersionConformancePass.cs")));
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var matches = Regex.Matches(pass, @"public override object\? Visit(?<rule>\w+)\(").ToList();
        for (int i = 0; i < matches.Count; i++)
        {
            int start = matches[i].Index;
            int end = i + 1 < matches.Count ? matches[i + 1].Index : pass.Length;
            string rule = matches[i].Groups["rule"].Value;
            result[char.ToLowerInvariant(rule[0]) + rule[1..]] = pass[start..end];
        }
        return result;
    }

    /// <summary>Every grammar rule that admits a boolean expression is gated, or exempt for a stated reason.</summary>
    [Fact]
    public void EveryBooleanExpressionSite_IsGatedOrAdjudicatedExempt()
    {
        var hosts = ParserRules()
            .Where(r => Regex.IsMatch(r.Body, @"\bbooleanExpression\b"))
            .Select(r => r.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // The scan must FIND the sites — an empty or near-empty result would make every assertion below vacuous
        // (feedback_verdict_evidence_invariant: a run must assert its population).
        Assert.True(hosts.Count >= 4,
            $"the grammar scan found only {hosts.Count} rule(s) referencing booleanExpression "
            + $"({string.Join(", ", hosts)}) — the scan itself is broken, not the gate");

        var overrides = GateOverrides();
        var ungated = hosts
            .Where(h => !Exempt.ContainsKey(h))
            .Where(h => !overrides.TryGetValue(h, out string? body)
                        || !body.Contains("GateBooleanOperators", StringComparison.Ordinal))
            .ToList();

        Assert.True(ungated.Count == 0,
            $"grammar rule(s) admit a booleanExpression with no introduction gate: {string.Join(", ", ungated)}."
            + $"{Environment.NewLine}A boolean SHIFT operator there compiles clean below COBOL-2023. Add a "
            + $"Visit<Rule> override calling GateBooleanOperators(ctx.booleanExpression()) in "
            + $"VersionConformancePass, or adjudicate the rule into this test's Exempt map WITH ITS REASON.");
    }

    /// <summary>The exemptions stay honest: a name that no longer references a boolean expression is stale and
    /// must go, or it will silently excuse a future rule that reuses the name.</summary>
    [Fact]
    public void NoExemptionIsStale()
    {
        var hosts = ParserRules()
            .Where(r => Regex.IsMatch(r.Body, @"\bbooleanExpression\b"))
            .Select(r => r.Name).ToHashSet(StringComparer.Ordinal);
        var stale = Exempt.Keys.Where(k => !hosts.Contains(k)).ToList();
        Assert.True(stale.Count == 0,
            $"exempt rule(s) no longer reference a booleanExpression: {string.Join(", ", stale)}");
    }

    /// <summary>The gate itself is written ONCE. Three call sites are fine; three COPIES of the two Check calls
    /// are how the third site came to be missing in the first place (feedback_one_rule_one_place).</summary>
    [Fact]
    public void TheGateBodyIsWrittenOnce()
    {
        string pass = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "Validation", "VersionConformancePass.cs")));
        Assert.Single(Regex.Matches(pass, @"Constructs\.BooleanShiftOperators2023"));
        Assert.Single(Regex.Matches(pass, @"Constructs\.BooleanOperators2002"));
    }
}
