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
/// ⛔ THE GRAMMAR SHAPE THAT MAKES THE NEXT FORMAT AUTOMATIC, HELD TRUE (kb/Work PB412, PB421).
///
/// <para>Two defects in one wave had the same shape: a grammar rule spelled the UNION of its clause's printed
/// general formats instead of one alternative per format, and the complement — every shape the formats do not
/// print — was accepted and answered downstream, by a bind arm that discarded operands or by a run-time loud
/// stage. The repair was structural (the parser now refuses the complement), and this class is what keeps the
/// structure from quietly re-collapsing: both invariants below are DERIVED, from the spec transcription and
/// from the grammar text, never from a hand-kept list of rules someone audited (CLAUDE.md rules 5 and 8).</para>
///
/// <para>⚠ Each scrape asserts its own SHAPE before comparing — the clause is found, the format count is the
/// one the printed page carries, and the keyword sweep finds the statement rules it is supposed to find — so a
/// regex that stopped matching fails loudly instead of passing vacuously
/// (feedback_green_gates_arent_evidence).</para>
/// </summary>
public sealed class PrintedFormatAlternativeDriftTests
{
    private static readonly string[] GrammarFiles =
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories);

    /// <summary>Every grammar file with its comments stripped — an invariant about what the GRAMMAR admits must
    /// not be satisfiable by a word inside a comment.</summary>
    private static IEnumerable<(string File, string Text)> Grammars() => GrammarFiles
        .Where(f => !f.EndsWith("CobolLexer.g4", StringComparison.Ordinal))
        .Select(f => (Path.GetFileName(f), StripComments(File.ReadAllText(f))));

    private static string StripComments(string t) =>
        Regex.Replace(Regex.Replace(t, @"//[^\n]*", ""), @"/\*.*?\*/", "", RegexOptions.Singleline);

    /// <summary>The body of parser rule <paramref name="rule"/>, comments stripped.</summary>
    private static string RuleBody(string rule)
    {
        foreach (var (_, text) in Grammars())
        {
            var m = Regex.Match(text, @"^" + Regex.Escape(rule) + @"\s*\n?\s*:(.*?);\s*$",
                                RegexOptions.Singleline | RegexOptions.Multiline);
            if (m.Success) return m.Groups[1].Value;
        }
        throw new Xunit.Sdk.XunitException($"grammar rule '{rule}' was not found in any .g4 — the scrape is broken, "
            + "not the grammar");
    }

    /// <summary>Top-level alternatives of a rule body: a <c>|</c> outside every bracket and parenthesis.</summary>
    private static int TopLevelAlternatives(string body)
    {
        int depth = 0, alts = 1;
        foreach (char c in body)
        {
            if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;
            else if (c == '|' && depth == 0) alts++;
        }
        return alts;
    }

    /// <summary>How many <c>Format N</c> figures clause <paramref name="clause"/>'s "General formats" section
    /// prints, counted from the transcription's <c>&lt;pre&gt;</c> blocks — the SAME count the printed page
    /// carries, so a future edition's extra format moves this number without anyone editing a test.</summary>
    private static int PrintedFormats(string clause)
    {
        string[] lines = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md")).Split('\n');
        bool inSection = false, inPre = false, sectionSeen = false;
        int blocks = 0;
        foreach (string raw in lines)
        {
            string line = raw.TrimEnd('\r');
            var head = Regex.Match(line, @"^#+\s+(\d+(?:\.\d+)*)\s+General formats?\s*$");
            if (head.Success)
            {
                inSection = head.Groups[1].Value == clause;
                sectionSeen |= inSection;
                continue;
            }
            if (inSection && Regex.IsMatch(line, @"^#+\s+\d")) inSection = false;
            if (!inSection) continue;
            if (line.StartsWith("<pre", StringComparison.Ordinal)) { inPre = true; continue; }
            if (inPre && line.StartsWith("</pre>", StringComparison.Ordinal)) { inPre = false; blocks++; }
        }
        Assert.True(sectionSeen, $"§{clause} General formats was not found in specs/ISO_COBOL.md — the scrape is broken");
        Assert.True(blocks > 0, $"§{clause} General formats printed no figure — the scrape is broken");
        return blocks;
    }

    /// <summary>⛔ ONE <c>goToStatement</c> ALTERNATIVE PER PRINTED §14.9.17.2 FORMAT, PLUS EXACTLY ONE
    /// ADJUDICATED ARM. That one arm is the ANSI X3.23-1985 target-less <c>GO TO.</c>, which the 2023 standard
    /// prints NO format for: it stays in the grammar so the removal can be reported by name
    /// (COBOLNET0811 / <c>BareGotoRemoved2002</c>) instead of as a no-viable-alternative parse error, which the
    /// four-compilers rule would not accept. Any OTHER extra alternative is the union shape coming back — the
    /// rule was once <c>GO TO? procedureName? (procedureName)* (DEPENDING ON? dataReference)?</c>, a single
    /// alternative covering both formats and the legacy arm, and the two shapes in its complement were accepted
    /// at every edition.</summary>
    [Fact]
    public void GoToStatement_HasOneAlternativePerPrintedFormatPlusTheGatedLegacyArm()
    {
        int formats = PrintedFormats("14.9.17.2");
        Assert.Equal(2, formats);                       // shape assertion: the printed page carries two
        Assert.Equal(formats + 1, TopLevelAlternatives(RuleBody("goToStatement")));
    }

    /// <summary>⛔ A FORMAT-SELECTING KEYWORD IS SPELLED BY THE STATEMENT RULE, NEVER BY A PHRASE RULE IT
    /// COMPOSES WITH. <c>moveReceivingPhrase</c> carried a second alternative
    /// <c>(CORRESPONDING | CORR) dataReference TO dataReference</c>, which composed with
    /// <c>MOVE moveSendingOperand moveReceivingPhrase</c> to admit
    /// <c>MOVE &lt;sending-operand&gt; CORRESPONDING id-3 TO id-4</c> — a shape NEITHER §14.9.25.2 format
    /// prints, because a phrase rule cannot know what the statement rule already spelled.
    /// <para>The invariant is stated over the WHOLE grammar rather than over the one rule that broke it: the
    /// CORRESPONDING/CORR pair selects a general format in three clauses (§14.9.2.2, §14.9.25.2, §14.9.44.2),
    /// and in every one of them the format prints the keyword immediately after the verb. A new phrase rule
    /// that names it is the same defect in a new clause, and fails here.</para></summary>
    [Fact]
    public void TheCorrespondingKeyword_IsNamedOnlyByStatementRules()
    {
        var offenders = new List<string>();
        var seen = new List<string>();
        foreach (var (file, text) in Grammars())
            foreach (Match m in Regex.Matches(text, @"^([a-z][A-Za-z0-9_]*)\s*\n?\s*:(.*?);\s*$",
                                              RegexOptions.Singleline | RegexOptions.Multiline))
            {
                string rule = m.Groups[1].Value, body = m.Groups[2].Value;
                if (!Regex.IsMatch(body, @"\b(CORRESPONDING|CORR)\b")) continue;
                seen.Add(rule);
                if (!rule.EndsWith("Statement", StringComparison.Ordinal))
                    offenders.Add($"{file}: rule '{rule}' names a format-selecting CORRESPONDING/CORR keyword");
            }

        // Shape assertion: the sweep must still find the three statement rules whose general formats print the
        // keyword, or it is matching nothing and would pass on an empty grammar.
        Assert.Equal(
            new[] { "addStatement", "moveStatement", "subtractStatement" },
            seen.Distinct().OrderBy(r => r, StringComparer.Ordinal).ToArray());
        Assert.Empty(offenders);
    }

    /// <summary>The MOVE statement rule itself carries one alternative per printed format — the other half of
    /// the same invariant, and the one that makes the sweep above sufficient: Format 2 is spelled WHOLE by
    /// <c>moveStatement</c>'s first alternative, so nothing downstream needs to re-spell it.</summary>
    [Fact]
    public void MoveStatement_HasOneAlternativePerPrintedFormat()
    {
        int formats = PrintedFormats("14.9.25.2");
        Assert.Equal(2, formats);                       // shape assertion: the printed page carries two
        Assert.Equal(formats, TopLevelAlternatives(RuleBody("moveStatement")));
    }

    /// <summary>⛔ THE MIRROR DIRECTION, AND THE ONE THAT REJECTS LEGAL SOURCE (kb/Work PB402, PB407). The
    /// standard prints <c>LAST EXCEPTION</c> in three general formats — §14.9.14.2 Format 2 (EXIT PROGRAM,
    /// canonical PDF page 653), §14.9.18.2 (GOBACK, page 661) and §14.9.38.2 Format 2 (SEND, page 756) — and on
    /// all three, rendered and measured, RAISING and LAST carry underline rectangles while the EXCEPTION after
    /// LAST does not. §5.2.3 makes a non-underlined uppercase word an OPTIONAL word, so <c>RAISING LAST</c> is
    /// legal at every one of those sites, and §7.3.21.4 rule 2 is the standard citing itself — the PROPAGATE
    /// rule writes the phrase back as "as though a GOBACK RAISING LAST statement were executed".
    /// <para>This is stated over the GRAMMAR TEXT rather than over one rule because the defect recurred by
    /// COPY: PB407 relaxed the shared <c>raisingPhrase</c>, and <c>mcsSendStatement</c>'s inline spelling of the
    /// same two words — a rule that cannot share <c>raisingPhrase</c>, because SEND's brace prints two
    /// alternatives where GOBACK's prints three — kept the falsely-restrictive form and went on rejecting
    /// <c>SEND … RAISING LAST</c>. A fourth site tomorrow, sharing or inline, fails here.</para></summary>
    [Fact]
    public void EveryPrintedLastExceptionSpelling_MakesTheSecondExceptionOptional()
    {
        var sites = new List<string>();
        var offenders = new List<string>();
        foreach (var (file, text) in Grammars())
            foreach (Match m in Regex.Matches(text, @"\b(SET\s+)?LAST\s+EXCEPTION(\??)"))
            {
                if (m.Groups[1].Success) continue;      // §14.9.39 Format 13's own figure — see the test below
                sites.Add($"{file}@{m.Index}");
                if (m.Groups[2].Value != "?")
                    offenders.Add($"{file}: 'LAST EXCEPTION' with a REQUIRED second EXCEPTION near offset {m.Index}");
            }

        // Shape assertion: the sweep must find the sites it exists to check — `raisingPhrase` and
        // `mcsSendStatement` today — or it is matching nothing and would pass on an empty grammar.
        Assert.True(sites.Count >= 2,
            $"the LAST EXCEPTION sweep found {sites.Count} site(s) — the scrape is broken, not the grammar");
        Assert.Empty(offenders);
    }

    /// <summary>⚠ THE SITE THAT MAY NOT BE SWEPT WITH THE OTHERS, named so the sweep above cannot be misread.
    /// <c>SET LAST EXCEPTION TO OFF</c> (§14.9.39 Format 13) prints its own figure with its own underlining, and
    /// a sweep that treated every <c>LAST EXCEPTION</c> in the language as one fact would relax it on this
    /// grammar rule's authority rather than on its own page's
    /// (feedback_a_real_clause_can_answer_a_different_question). It is excluded HERE by spelling, not by
    /// judgement: the statement rule writes the two words with the statement keyword SET in front of them, and
    /// this test pins that spelling so the exclusion is visible rather than accidental.</summary>
    [Fact]
    public void TheSetLastExceptionStatement_IsADistinctFigureAndIsNotSweptHere()
        => Assert.Matches(@"SET\s+LAST\s+EXCEPTION\s+TO\s+OFF", RuleBody("setLastExceptionStatement"));
}
