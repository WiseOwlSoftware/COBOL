// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <see cref="Table12StatementNames"/> IS ISO Table 12's 'Statement name' COLUMN, AND THIS RE-DERIVES IT FROM
/// THE SPEC — BOTH DIRECTIONS.
///
/// <para>§15.32.3 r3 makes the column the answer FUNCTION EXCEPTION-STATEMENT must give, and the resolver keys
/// on the parse rule because the spelled-token axis is wrong in a way no token can repair (kb/Work R04:
/// <c>GO PARA.</c> is a GO TO statement whose tokens never contain TO). A resolver nothing re-derives is a
/// hand-maintained list (CLAUDE.md rule 5) and a table nothing re-derives has never been contradicted
/// (feedback_a_dead_lookup_is_also_unverified) — so this scrapes Table 12 out of <c>specs/ISO_COBOL.md</c> and
/// the <c>statement</c> alternatives out of the grammar, feeds every alternative through the SAME
/// <see cref="Table12StatementNames.NameOfRule"/> path the compiler uses, and asserts containment forward
/// (every alternative resolves into the table or the adjudicated non-Table-12 set) and coverage backward
/// (every table row is produced by some alternative — a row with no rule is grammar or transcription drift).
/// A newly added statement rule fails here until it resolves.</para>
///
/// <para>⚠ Both scrapes assert the SHAPE they found before comparing, so a transcription reformat or a grammar
/// restructure fails loudly instead of silently comparing nothing (the vacuous-pass trap).</para>
/// </summary>
public sealed class Table12StatementNameDriftTests
{
    /// <summary>Grammar rules with no row in the 2023 Table 12, each with the reason its projected name is the
    /// adjudicated answer. Adding a name here is an adjudication, not a formality — conforming source cannot
    /// observe any of these under checking (&gt;&gt;TURN is COBOL-2002+ and each is pre-2002, non-procedural,
    /// or binds loud), and the projected name is strictly better than a wrong sibling's name if one escapes.</summary>
    private static readonly Dictionary<string, string> NonTable12 = new(StringComparer.Ordinal)
    {
        ["ALTER"] = "COBOL-85 element deleted from the standard before Table 12's 2023 edition",
        ["ENTRY"] = "a non-ISO extension statement the grammar carries; no Table 12 row at any edition",
        ["ENTER"] = "COBOL-85 obsolete element deleted from the standard before 2023",
        ["USE"] = "a declaratives header the grammar routes through `statement`; not a procedural statement",
        ["NEXT SENTENCE"] = "no 2023 Table 12 row (survives only as the IF/SEARCH-era branch form)",
        ["INLINE METHOD INVOCATION"] = "2023 inline method invocation used as a statement; no Table 12 row",
    };

    /// <summary>Table 12's 'Statement name' column, scraped from the transcription at its anchor.</summary>
    private static List<string> ScrapeTable12()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int anchor = spec.IndexOf("<a id=\"table-12\">", StringComparison.Ordinal);
        Assert.True(anchor >= 0, "the `table-12` anchor is gone from specs/ISO_COBOL.md — this guard must follow it");

        var rowLines = spec[anchor..].Split('\n').Select(l => l.TrimEnd('\r'))
            .SkipWhile(l => !l.StartsWith('|')).TakeWhile(l => l.StartsWith('|')).ToList();
        // Row 0 = the header, row 1 = the |---| separator, rows 2.. = the data.
        var names = rowLines.Skip(2).Select(l => l.Split('|')[1].Trim().Trim('*').Trim()).ToList();

        // Shape before content: 50 rows ACCEPT…WRITE, GO TO the only multi-word name. A count drift here means
        // the TRANSCRIPTION changed and the resolver's premise must be re-derived, not patched.
        Assert.Equal(50, names.Count);
        Assert.Equal("ACCEPT", names[0]);
        Assert.Equal("WRITE", names[^1]);
        Assert.Equal(["GO TO"], names.Where(n => n.Contains(' ')));
        return names;
    }

    /// <summary>The rule reference of every <c>statement</c> alternative, scraped from the grammar (comments
    /// stripped first — sibling rules are NAMED in the rule's comments). Asserts each alternative is a single
    /// rule reference, the shape <see cref="Table12StatementNames.NameOf"/> depends on.</summary>
    private static List<string> ScrapeStatementAlternatives()
    {
        string grammar = File.ReadAllText(TestRepo.Src(
            Path.Combine("Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4")));
        grammar = Regex.Replace(grammar, @"//[^\r\n]*", "");
        var m = Regex.Match(grammar, @"^statement\s*:(?<body>.*?);", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "the `statement` rule is gone from CobolParserCore.g4 — this guard must follow it");

        var alternatives = new List<string>();
        foreach (string alt in m.Groups["body"].Value.Split('|'))
        {
            var refs = Regex.Matches(alt, @"\b([a-zA-Z]\w*Statement)\b").Select(x => x.Groups[1].Value).ToList();
            Assert.True(refs.Count == 1,
                $"a `statement` alternative is not a single rule reference (found {refs.Count} in \"{alt.Trim()}\") "
                + "— Table12StatementNames.NameOf resolves child 0's rule and must follow this restructure");
            alternatives.Add(refs[0]);
        }
        Assert.True(alternatives.Count >= 55,
            $"scraped only {alternatives.Count} statement alternatives — the scrape lost the rule body");
        return alternatives;
    }

    [Fact]   // Forward: every grammar alternative resolves to a Table 12 name or an adjudicated exemption.
    public void EveryStatementRule_ResolvesToTable12_OrAdjudicatedExemption()
    {
        var table = ScrapeTable12();
        var unadjudicated = ScrapeStatementAlternatives()
            .Select(rule => (rule, name: Table12StatementNames.NameOfRule(rule)))
            .Where(x => !table.Contains(x.name) && !NonTable12.ContainsKey(x.name)).ToList();
        Assert.True(unadjudicated.Count == 0,
            "statement rule(s) resolve to a name that is neither a Table 12 row nor an adjudicated exemption — "
            + "map them in Table12StatementNames or adjudicate them here, with the reason:\n  "
            + string.Join("\n  ", unadjudicated.Select(x => $"{x.rule} → \"{x.name}\"")));
    }

    [Fact]   // Backward: every Table 12 row is produced by at least one grammar alternative.
    public void EveryTable12Row_IsProducedBySomeStatementRule()
    {
        var produced = ScrapeStatementAlternatives().Select(Table12StatementNames.NameOfRule).ToHashSet(StringComparer.Ordinal);
        var orphans = ScrapeTable12().Where(n => !produced.Contains(n)).ToList();
        Assert.True(orphans.Count == 0,
            "Table 12 row(s) with no grammar alternative resolving to them — grammar or transcription drift:\n  "
            + string.Join("\n  ", orphans));
    }

    [Fact]   // The R04 pin: the row that exposed the token axis, resolved through the general mechanism.
    public void GoTo_ResolvesFromTheRule_NeverFromTokens()
        => Assert.Equal("GO TO", Table12StatementNames.NameOfRule("goToStatement"));
}
