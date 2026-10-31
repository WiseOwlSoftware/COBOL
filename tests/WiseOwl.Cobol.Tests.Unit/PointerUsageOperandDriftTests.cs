// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE TO-LESS POINTER OPERAND (kb/Work PB848, PB4235).
///
/// <para>ISO/IEC 1989:2023 §13.18.60.2 (rendered, folio 503) underlines POINTER / FUNCTION-POINTER /
/// PROGRAM-POINTER and never TO, so by §5.2.3 TO is an optional word and the operand may follow the keyword
/// directly. The TO-less operand is a bare <c>cobolWord</c> at the END of a clause, so the grammar guards it with
/// <c>pointerOperandHere()</c>: the word is the operand unless it is the KEYWORD of what follows the usage in the
/// entry being parsed. <c>cobolWord</c>'s reservation gate is NOT enough on its own — it exempts the §15 function
/// names, and BIT and NATIONAL are also bare USAGE keywords (this test caught <c>USAGE POINTER BIT</c> binding BIT
/// as a type-name when the guard was briefly removed), and a first cut that excluded only the clause-leading words
/// let <c>USAGE POINTER HIGH-ORDER-RIGHT</c> bind the endianness phrase as a type-name.</para>
/// <para>WHAT FOLLOWS IS PER ENTRY (kb/Work PB4235). The usage clause is a clause of THREE entries — the data
/// description entry, the report group description entry and the screen description entry — and the guard once
/// excluded the context-free FOLLOW(usageKeyword), the union of all three, so the §8.10 screen words (AUTO, BELL,
/// SECURE …: context-sensitive in the "screen description entry", user-defined words elsewhere by §8.3.2.1 3)) were
/// refused as a data entry's type-name. So the oracle here is each entry's OWN follow, read off the generated ATN
/// along that entry's chain of rules, independently of the parser's live-context walk, and the per-word fact is an
/// IFF over EVERY word <c>cobolWord</c> can match: the word is the TO-less operand exactly when it does not follow
/// the usage in that entry.</para>
/// <para>The overlap facts flip the axis the subject holds fixed (feedback_probe_the_shape_the_subject_hides): they
/// require each entry's follow to hold the words that make the guard necessary, so the per-word fact can never pass
/// vacuously.</para>
/// </summary>
public sealed class PointerUsageOperandDriftTests
{
    private const string NL = "\n";

    private static (CobolParserCore.CompilationUnitContext? Tree, DiagnosticBag Diags) Parse(string src, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_ptr_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = edition }.Parse(path, diags);
            return (tree, diags);
        }
        finally { FileHarness.TryDelete(path); }
    }

    private static string Lines(params string[] lines) => string.Join(NL, lines) + NL;

    private static string Prog(string entries) =>
        Lines("IDENTIFICATION DIVISION.", "PROGRAM-ID. PTROPND.", "DATA DIVISION.", "WORKING-STORAGE SECTION.")
        + entries + Lines("PROCEDURE DIVISION.", "MAIN.", "    STOP RUN.");

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }

    /// <summary>One entry the usage clause is a clause of: the program that writes <c>USAGE {usage} {word}</c> as one
    /// of its clauses, and the chain of rules from <c>usageClause</c> out to the entry (innermost first), along which
    /// the entry's own follow of a usage keyword is read.</summary>
    private sealed record Entry(string Name, Func<string, string> Program, int[] Chain);

    private static readonly Entry Data = new("data description entry",
        clause => Prog(Lines($"01 P {clause}.")),
        [CobolParserCore.RULE_usageClause, CobolParserCore.RULE_dataDescriptionClause,
         CobolParserCore.RULE_dataDescriptionClauses, CobolParserCore.RULE_dataDescriptionBody,
         CobolParserCore.RULE_dataDescriptionEntry]);

    private static readonly Entry Report = new("report group description entry",
        clause => Lines("IDENTIFICATION DIVISION.", "PROGRAM-ID. PTROPND.", "ENVIRONMENT DIVISION.",
                        "INPUT-OUTPUT SECTION.", "FILE-CONTROL.", "    SELECT REP-FILE ASSIGN TO \"REPF\".",
                        "DATA DIVISION.", "FILE SECTION.", "FD REP-FILE REPORT IS REP-1.", "REPORT SECTION.", "RD REP-1.",
                        "01 TYPE IS DETAIL.",
                        $"   05 P COLUMN 1 {clause}.", "PROCEDURE DIVISION.", "MAIN.", "    STOP RUN."),
        [CobolParserCore.RULE_usageClause, CobolParserCore.RULE_reportGroupClause, CobolParserCore.RULE_reportGroupEntry]);

    private static readonly Entry Screen = new("screen description entry",
        clause => Lines("IDENTIFICATION DIVISION.", "PROGRAM-ID. PTROPND.", "DATA DIVISION.", "SCREEN SECTION.",
                        "01 S.", $"   05 P {clause}.", "PROCEDURE DIVISION.", "MAIN.", "    STOP RUN."),
        [CobolParserCore.RULE_usageClause, CobolParserCore.RULE_screenClause, CobolParserCore.RULE_screenDescriptionBody,
         CobolParserCore.RULE_screenDescriptionEntry]);

    private static readonly Dictionary<string, Entry> Entries = new[] { Data, Report, Screen }.ToDictionary(e => e.Name);

    /// <summary>The tokens that can follow a usage keyword in <paramref name="entry"/>: from every call of
    /// <c>usageKeyword</c> inside <c>usageClause</c>, then out through the entry's chain wherever the inner rule can end
    /// (<see cref="ATN.NextTokens(ATNState)"/> holds EPSILON there).</summary>
    private static IntervalSet FollowIn(Entry entry)
    {
        var atn = CobolParserCore._ATN;
        var result = new IntervalSet();
        int callee = CobolParserCore.RULE_usageKeyword;
        foreach (int caller in entry.Chain)
        {
            bool canEnd = false, called = false;
            foreach (var state in atn.states.Where(s => s is not null && s.ruleIndex == caller))
                for (int i = 0; i < state.NumberOfTransitions; i++)
                    if (state.Transition(i) is RuleTransition rt && rt.target.ruleIndex == callee)
                    {
                        called = true;
                        var next = atn.NextTokens(rt.followState);
                        canEnd |= next.Contains(TokenConstants.EPSILON);
                        result.AddAll(next);
                    }
            Assert.True(called, $"{entry.Name}: rule {CobolParserCore.ruleNames[caller]} no longer calls "
                + $"{CobolParserCore.ruleNames[callee]} — re-derive the entry's chain");
            if (!canEnd)
            {
                result.Remove(TokenConstants.EPSILON);
                return result;
            }
            callee = caller;
        }
        throw new Xunit.Sdk.XunitException($"{entry.Name}: the chain ends while the entry can still end — extend it");
    }

    private static string Spelling(int t) => CobolParserCore.DefaultVocabulary.GetLiteralName(t)?.Trim('\'')
                                             ?? CobolParserCore.DefaultVocabulary.GetSymbolicName(t).Replace('_', '-');

    /// <summary>Every word <c>cobolWord</c> can match but a plain user word (an IDENTIFIER IS the operand).</summary>
    private static List<int> KeywordsCobolWordAdmits()
    {
        var atn = CobolParserCore._ATN;
        return atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_cobolWord]).ToList()
            .Where(t => t != CobolParserCore.IDENTIFIER).ToList();
    }

    /// <summary>The §8.10 words whose construct is the screen description entry (the PB4235 repro set).</summary>
    private static readonly string[] ScreenWords =
    [
        "AUTO", "BACKGROUND-COLOR", "BELL", "BLINK", "ERASE", "FOREGROUND-COLOR", "FULL", "HIGHLIGHT", "LOWLIGHT",
        "REQUIRED", "REVERSE-VIDEO", "SECURE", "UNDERLINE",
    ];

    [Fact]
    public void EachEntrysFollow_IsReadFromTheAtn_AndHoldsTheWordsThatNeedTheGuard()
    {
        var words = KeywordsCobolWordAdmits();
        List<string> In(Entry e) { var f = FollowIn(e); return words.Where(f.Contains).Select(Spelling).ToList(); }
        var data = In(Data);
        var screen = In(Screen);
        // HIGH-ORDER-RIGHT is a USAGE-clause tail phrase and SIGNED the binary sign phrase; both are cobolWord
        // alternatives, so if either drops out the derivation (not the grammar) is what broke.
        Assert.Contains("HIGH-ORDER-RIGHT", data);
        Assert.Contains("SIGNED", data);
        // The screen words begin the screen entry's clauses and none of the data entry's (PB4235).
        foreach (string w in ScreenWords)
        {
            Assert.Contains(w, screen);
            Assert.DoesNotContain(w, data);
        }
    }

    public static IEnumerable<object[]> Cases() =>
    [
        [Data.Name, 85], [Data.Name, 2002], [Data.Name, 2014], [Data.Name, 2023],
        [Report.Name, 2023], [Screen.Name, 2023],
    ];

    /// <summary>The TO-less operand is EXACTLY every word that does not follow a usage keyword in its entry: for every
    /// keyword <c>cobolWord</c> admits, <c>USAGE {usage} W</c> binds W as the operand iff W is not in that entry's
    /// follow, at every edition (the grammar parses the pointer usages as a superset at every edition; the
    /// introduction gate is the binder's).</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void TheToLessOperand_IsEveryWordThatDoesNotFollowTheUsageInItsEntry(string entryName, int edition)
    {
        var entry = Entries[entryName];
        var follow = FollowIn(entry);
        int keywords = 0, operands = 0;
        foreach (int t in KeywordsCobolWordAdmits())
        {
            string w = Spelling(t);
            bool follows = follow.Contains(t);
            foreach (string usage in new[] { "POINTER", "PROGRAM-POINTER", "FUNCTION-POINTER" })
            {
                var (tree, diags) = Parse(entry.Program($"USAGE {usage} {w}"), edition);
                var kw = tree is null ? null : Descendants<CobolParserCore.UsageKeywordContext>(tree).FirstOrDefault();
                var operand = kw?.dataPointerUsage()?.cobolWord() ?? kw?.programPointerUsage()?.cobolWord()
                              ?? kw?.functionPointerUsage()?.cobolWord();
                if (follows)
                    // A SYNTAX ERROR is also a pass: the entry is well-formed whenever W is taken as the operand, so
                    // an entry that fails to parse (W began a clause whose remainder is missing) proves W was not.
                    Assert.True(operand is null, $"{entry.Name}, USAGE {usage} {w} at --std {edition}: {w} follows "
                        + "the usage in this entry, so it begins its clause — it was bound as the TO-less operand");
                else
                    Assert.True(operand?.GetText() == w, $"{entry.Name}, USAGE {usage} {w} at --std {edition}: {w} "
                        + "cannot follow the usage in this entry, so it is the TO-less operand (§13.18.60.2, §8.3.2.1 3)) "
                        + $"— got '{operand?.GetText()}': "
                        + string.Join("; ", diags.Diagnostics.Select(d => d.ToString())));
            }
            if (follows) keywords++; else operands++;
        }
        Assert.True(keywords > 0 && operands > 0, $"{entry.Name}: {keywords} keyword(s), {operands} operand(s) — "
            + "a run that checked one side only proves nothing");
    }

    /// <summary>The guard reads the pointer rule's END as the operand slot's exit, which holds only while the optional
    /// operand is each guarded rule's last element: every grammar rule that asks <c>pointerOperandHere()</c> is
    /// <c>KEYWORD (TO cobolWord | {pointerOperandHere()}? cobolWord)?</c> and nothing more.</summary>
    [Fact]
    public void EveryGuardedRule_EndsInItsOptionalOperand()
    {
        var rule = new Regex(@"^(?<name>[a-zA-Z]\w*)\s*:(?<body>[^;]*);", RegexOptions.Multiline);
        var shape = new Regex(@"^\s*[A-Z_]+\s*\(TO cobolWord \| \{pointerOperandHere\(\)\}\? cobolWord\)\?\s*$");
        var guarded = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar"), "*.g4",
                     SearchOption.AllDirectories))
            foreach (Match m in rule.Matches(File.ReadAllText(file)))
                if (m.Groups["body"].Value.Contains("pointerOperandHere()", StringComparison.Ordinal))
                {
                    string name = m.Groups["name"].Value;
                    guarded.Add(name);
                    Assert.True(shape.IsMatch(m.Groups["body"].Value), $"{Path.GetFileName(file)}#{name}: "
                        + "pointerOperandHere() reads the rule's end as the operand slot's exit — keep the optional "
                        + "operand the rule's last element");
                }
        Assert.Equal(["dataPointerUsage", "functionPointerUsage", "programPointerUsage"], guarded.Order());
    }

    [Theory]
    [InlineData("POINTER", "01 PT-T IS TYPEDEF USAGE POINTER REC-T.")]
    [InlineData("POINTER", "01 PT-T IS TYPEDEF USAGE POINTER TO REC-T.")]
    [InlineData("PROGRAM-POINTER", "01 PP-T IS TYPEDEF USAGE PROGRAM-POINTER REC-T.")]
    [InlineData("FUNCTION-POINTER", "01 FP USAGE FUNCTION-POINTER REC-T.")]
    [InlineData("FUNCTION-POINTER", "01 FP FUNCTION-POINTER TO REC-T.")]
    public void TheOperand_IsRecognized_WithOrWithoutTo(string usage, string entry)
    {
        var (tree, diags) = Parse(Prog(Lines("01 REC-T IS TYPEDEF.", "   05 A PIC X.", entry)), 2023);
        Assert.NotNull(tree);
        Assert.False(diags.HasErrors, string.Join("; ", diags.Diagnostics.Select(d => d.ToString())));
        var kw = Descendants<CobolParserCore.UsageKeywordContext>(tree!).Single();
        CobolParserCore.CobolWordContext? operand = usage switch
        {
            "POINTER" => kw.dataPointerUsage()?.cobolWord(),
            "PROGRAM-POINTER" => kw.programPointerUsage()?.cobolWord(),
            _ => kw.functionPointerUsage()?.cobolWord(),
        };
        Assert.Equal("REC-T", operand?.GetText());
    }
}
