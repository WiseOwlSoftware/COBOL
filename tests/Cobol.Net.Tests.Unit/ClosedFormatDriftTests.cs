// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE GRAMMAR'S LAST TOTAL SINK (kb/Work PB829).
///
/// <para><b>Why it exists.</b> The word run <c>genericClause</c> matches ANY run of words,
/// so wherever it sits in an alternative list it swallows everything the named alternatives missed. It was ONE
/// grammar rule reached from SIX sites spanning EIGHT closed general formats, and closing it one site at a time
/// is exactly how it survived: kb/Work PB487 closed §13.16.2 and swept for siblings, but read the grammar by rule
/// NAME — so it counted four of the remaining five and missed the I-O-CONTROL paragraph's INLINE alternative,
/// which is not a named <c>xxxClause : genericClause</c> wrapper. "One production, one pass, one table" is only
/// a claim until something derives it mechanically, which is what these facts do.</para>
///
/// <para><b>Both directions, and from two independent sources.</b> The GRAMMAR facts read the <c>.g4</c> files
/// and require that the word-run matcher is referenced from the single error production and nowhere else — a new
/// catch-all cannot be introduced under a new name. The REFLECTION facts read the generated parser and require
/// that every context type able to hold an <c>unrecognizedClause</c> has a <see cref="ClosedFormats"/> row, and
/// that every row names a context type the grammar still produces — a new closed format cannot parse into
/// silence, and a stale row (a dead lookup, which this repository has learned is never contradicted) cannot
/// linger.</para>
/// </summary>
public sealed class ClosedFormatDriftTests : CobolNetTestBase
{
    /// <summary>Every parser grammar fragment the frontend compiles. Globbed rather than listed: a new .g4 file
    /// is exactly where an unnoticed catch-all would land.</summary>
    private static string[] GrammarFiles() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories)
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                          && !p.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}"))
                 .ToArray();

    /// <summary>The file's text with line comments stripped, so a rule NAMED in prose does not count as a
    /// reference (the DeclinedFacilityDriftTests mechanic — and load-bearing here, because every site this
    /// closed carries a comment block that mentions <c>genericClause</c> by name).</summary>
    private static string CodeOf(string path) => Regex.Replace(File.ReadAllText(path), @"//[^\n]*", "");

    [Fact]
    public void TheGrammarProbe_FindsTheFilesAndTheRule()
    {
        // A probe that finds nothing would make every obligation below vacuously green
        // (feedback_verdict_evidence_invariant: a MISSING observation is not a NEGATIVE one).
        var files = GrammarFiles();
        Assert.True(files.Length >= 5, $"only {files.Length} .g4 file(s) found under Grammar/ — the glob is broken");
        Assert.Contains(files, f => CodeOf(f).Contains("genericClause", StringComparison.Ordinal));
        Assert.Contains(files, f => CodeOf(f).Contains("unrecognizedClause", StringComparison.Ordinal));
    }

    /// <summary>⛔ THE GRAMMAR OBLIGATION. <c>genericClause</c> — the unbounded word-run matcher — may be
    /// referenced ONLY from its own definition and from the body of <c>unrecognizedClause</c>, the single error
    /// production. Any other reference is a new total sink over some alternative list, which is the defect
    /// PB487 and PB829 each spent a landing closing.
    /// <para>⚠ THE COMMENT-ENTRY SINKS ARE NOT THIS. <c>dateCompiledContent</c>, <c>securityContent</c> and
    /// <c>remarksContent</c> are also unbounded token runs and are CORRECT: a comment-entry is arbitrary text by
    /// definition, which is why ISO/IEC 1989:2023 defines no syntax for one anywhere. The distinction is what the
    /// sink sits over — a COMMENT-ENTRY (right) or a CLAUSE / PARAGRAPH LIST (this defect) — so they are
    /// deliberately outside this fact's reach.</para></summary>
    [Fact]
    public void GenericClause_IsReferencedOnlyByTheOneErrorProduction()
    {
        var offenders = new List<string>();
        foreach (string path in GrammarFiles())
        {
            string code = CodeOf(path);
            foreach (Match m in Regex.Matches(code, @"\bgenericClause\b"))
            {
                // The definition line itself: `genericClause` alone on a line, followed by the rule body.
                int lineStart = code.LastIndexOf('\n', Math.Max(0, m.Index - 1)) + 1;
                string line = code[lineStart..code.IndexOf('\n', m.Index)].Trim();
                if (line == "genericClause") continue;                    // its own definition
                if (line.StartsWith(": genericClause", StringComparison.Ordinal)) continue;  // a rule body — checked below
                offenders.Add($"{Path.GetFileName(path)}: {line}");
            }
        }
        // Only ONE rule body may be `: genericClause`, and it must be unrecognizedClause.
        var bodies = new List<string>();
        foreach (string path in GrammarFiles())
            foreach (Match m in Regex.Matches(CodeOf(path), @"(?m)^([a-z][A-Za-z0-9_]*)\s*\r?\n\s*:\s*genericClause\s*\r?\n\s*;"))
                bodies.Add(m.Groups[1].Value);

        Assert.True(offenders.Count == 0,
            "genericClause — the unbounded word-run matcher — is referenced "
            + "outside the ONE error production, so some alternative list has a total sink again and whatever it "
            + "swallows will be accepted in silence (kb/Work PB829): " + string.Join(" | ", offenders));
        Assert.Equal(["unrecognizedClause"], bodies);
    }

    /// <summary>The context types the GENERATED parser says can hold an <c>unrecognizedClause</c>: ANTLR emits a
    /// no-argument accessor returning <c>UnrecognizedClauseContext</c> on the context class of every rule whose
    /// body references it.</summary>
    private static IReadOnlyList<Type> ContextsCarryingTheErrorProduction() =>
        typeof(Core).GetNestedTypes(BindingFlags.Public)
            .Where(t => typeof(ParserRuleContext).IsAssignableFrom(t)
                        && t != typeof(Core.UnrecognizedClauseContext)
                        && t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Any(m => m.GetParameters().Length == 0
                                   && (m.ReturnType == typeof(Core.UnrecognizedClauseContext)
                                    || m.ReturnType == typeof(Core.UnrecognizedClauseContext[]))))
            .ToList();

    /// <summary>⛔ THE TABLE OBLIGATION, forward. A closed general format that gains the error production without
    /// a <see cref="ClosedFormats"/> row would refuse its residue with a message that cannot name the format —
    /// which is how the eight formats came to share one anonymous catch-all in the first place.</summary>
    [Fact]
    public void EveryContextCarryingTheErrorProduction_HasAClosedFormatRow()
    {
        var carriers = ContextsCarryingTheErrorProduction();
        Assert.True(carriers.Count >= 8,
            $"only {carriers.Count} context type(s) carry unrecognizedClause — the reflection probe is broken, "
            + "and both obligations below would be vacuously satisfied");
        var missing = carriers.Where(t => !ClosedFormats.ByContext.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0,
            "a closed general format ends in `unrecognizedClause` with no ClosedFormats.ByContext row, so "
            + "ClosedFormatPass cannot name the format it violates: " + string.Join(", ", missing)
            + ". Render the format's printed page, record its clause list above the alternative list in the .g4, "
            + "and add the row.");
    }

    /// <summary>⛔ THE TABLE OBLIGATION, backward. A row for a context type the grammar no longer produces is a
    /// dead lookup, and a dead lookup is never contradicted.</summary>
    [Fact]
    public void EveryClosedFormatRow_IsStillCarriedByTheGrammar()
    {
        var carriers = ContextsCarryingTheErrorProduction().ToHashSet();
        var dead = ClosedFormats.ByContext.Keys.Where(t => !carriers.Contains(t)).Select(t => t.Name).ToList();
        Assert.True(dead.Count == 0,
            "ClosedFormats.ByContext maps a context type that no longer ends in `unrecognizedClause`: "
            + string.Join(", ", dead));
    }

    /// <summary>Prove the guards can FAIL (feedback_green_gates_arent_evidence): a context type that does NOT
    /// carry the error production must be absent from the carrier set, and a fabricated rule name must not
    /// satisfy the grammar fact. Run against real-but-unrelated subjects rather than by mutating the files.</summary>
    [Fact]
    public void TheObligations_CanFail()
    {
        Assert.DoesNotContain(typeof(Core.ProcedureDivisionContext), ContextsCarryingTheErrorProduction());
        Assert.False(ClosedFormats.ByContext.ContainsKey(typeof(Core.ProcedureDivisionContext)));
        Assert.Null(ClosedFormats.Of(typeof(Core.ProcedureDivisionContext)));
        Assert.Null(ClosedFormats.Of(null));
    }

    /// <summary>Every row's message parts are non-empty and its code is one of the three the band owns. The §
    /// is checked as a shape, not by memory — <c>audit_code_citations.py</c> validates the clause numbers
    /// themselves against the standard.</summary>
    [Fact]
    public void EveryClosedFormatRow_IsWellFormed()
    {
        foreach (var (type, f) in ClosedFormats.ByContext)
        {
            Assert.Matches(@"^\d+(\.\d+)+$", f.Clause);
            Assert.False(string.IsNullOrWhiteSpace(f.Subject), $"{type.Name}: empty Subject");
            Assert.Contains(f.Noun, new[] { "clause", "paragraph" });
            Assert.Contains(f.Diagnostic.Code, new[] { "COBOLNET1941", "COBOLNET1970", "COBOLNET1971" });
            Assert.True(f.FormatLabel.Length == 0 || f.FormatLabel.EndsWith(' '),
                $"{type.Name}: FormatLabel must carry its own trailing space so the message needs no second template");
            // The noun and the code agree: a PARAGRAPH list is 1971, a CLAUSE list is 1970 (or 1941, the code
            // §13.16.2 kept from kb/Work PB487). A mismatch sends the reader to the wrong subclause.
            if (f.Noun == "paragraph") Assert.Equal("COBOLNET1971", f.Diagnostic.Code);
            else Assert.Contains(f.Diagnostic.Code, new[] { "COBOLNET1941", "COBOLNET1970" });
        }
    }

    /// <summary>⛔ THE PARSE LAYER'S KEY AGREES WITH THE TREE'S (kb/Work PB2533). <c>CobolErrorStrategy</c> refuses a
    /// RESERVED word at a closed format's error production by the RULE INDEX of the alternative list the ATN walk finds
    /// calling <c>unrecognizedClause</c>; <see cref="ClosedFormats.ByRuleIndex"/> must therefore hold every row, each
    /// under the rule whose generated context type is the row's key, and every rule that calls the production must be
    /// one of them — else a reserved word in that format would fall back to a bare parse error.</summary>
    [Fact]
    public void EveryRuleCallingTheErrorProduction_IsAClosedFormatRuleIndex()
    {
        Assert.Equal(ClosedFormats.ByContext.Count, ClosedFormats.ByRuleIndex.Count);
        foreach (var (type, f) in ClosedFormats.ByContext)
        {
            string rule = char.ToLowerInvariant(type.Name[0]) + type.Name[1..^"Context".Length];
            int index = Array.IndexOf(Core.ruleNames, rule);
            Assert.True(index >= 0, $"{type.Name}: no parser rule '{rule}'");
            Assert.Same(f, ClosedFormats.OfRule(index));
        }
        // Every rule whose ATN calls unrecognizedClause, read from the generated parser itself.
        var atn = Core._ATN;
        var callers = atn.states
            .Where(s => s is not null && Enumerable.Range(0, s.NumberOfTransitions).Any(i =>
                s.Transition(i) is RuleTransition rt
                && rt.target.ruleIndex == Core.RULE_unrecognizedClause))
            .Select(s => s.ruleIndex).ToHashSet();
        Assert.True(callers.Count >= 8, $"only {callers.Count} rule(s) call unrecognizedClause — the ATN probe is broken");
        var unmapped = callers.Where(r => ClosedFormats.OfRule(r) is null).Select(r => Core.ruleNames[r]).ToList();
        Assert.True(unmapped.Count == 0, "rules call unrecognizedClause with no ClosedFormats.ByRuleIndex entry: "
            + string.Join(", ", unmapped));
        Assert.Null(ClosedFormats.OfRule(Core.RULE_procedureDivision));   // the lookup can answer "none"
    }

    /// <summary>⛔ THE RESIDUE'S LITERAL TOKENS ARE EVERY ONE-TOKEN LITERAL BUT THE UNSIGNED INTEGER (kb/Work PB3373).
    /// <c>genericClause</c> opens at a <c>residueLiteral</c>, so <c>SOURCE-COMPUTER. XYZ "LIT".</c> reads XYZ as the
    /// computer-name; the unsigned integer is left out because it may be the next data description entry's
    /// level-number. The set is read from the generated parser: the token edges of <c>nonNumericLiteral</c> and
    /// <c>numericLiteralCore</c> that end their rule by themselves. A literal token added to either rule therefore fails
    /// here until <c>residueLiteral</c> decides it — the hand-written list cannot rot as <c>IsLiteral</c>'s three-token
    /// list did (STRINGLIT, INTEGERLIT and DECIMALLIT alone).</summary>
    [Fact]
    public void TheResidueLiterals_AreEverySingleTokenLiteral_ButTheUnsignedInteger()
    {
        var atn = Core._ATN;
        var single = new IntervalSet();
        foreach (int rule in new[] { Core.RULE_nonNumericLiteral, Core.RULE_numericLiteralCore })
            single.AddAll(SingleTokenAlternatives(atn, rule));
        Assert.True(single.Contains(CobolLexer.STRINGLIT) && single.Contains(CobolLexer.INTEGERLIT),
            $"the single-token literal probe found {single.ToString(Core.DefaultVocabulary)} — it is broken");
        var residue = atn.NextTokens(atn.ruleToStartState[Core.RULE_residueLiteral]);
        var expected = single.Subtract(IntervalSet.Of(CobolLexer.INTEGERLIT));
        Assert.True(expected.Equals(residue),
            $"residueLiteral is {residue.ToString(Core.DefaultVocabulary)}; every single-token literal but INTEGERLIT is "
            + expected.ToString(Core.DefaultVocabulary));
    }

    /// <summary>The token edges at <paramref name="rule"/>'s left edge (reached by epsilon edges, not into a called rule)
    /// whose target reaches the rule's stop state by epsilon edges alone: the alternatives that are one token.</summary>
    private static IntervalSet SingleTokenAlternatives(ATN atn, int rule)
    {
        var set = new IntervalSet();
        var stop = atn.ruleToStopState[rule];
        foreach (var state in EpsilonReach(atn.ruleToStartState[rule]))
            for (int i = 0; i < state.NumberOfTransitions; i++)
                if (state.Transition(i) is { IsEpsilon: false, Label: { } label } t
                    && EpsilonReach(t.target).Contains(stop))
                    set.AddAll(label);
        return set;
    }

    private static HashSet<ATNState> EpsilonReach(ATNState start)
    {
        var seen = new HashSet<ATNState>();
        var work = new Stack<ATNState>([start]);
        while (work.TryPop(out var s))
        {
            if (!seen.Add(s)) continue;
            for (int i = 0; i < s.NumberOfTransitions; i++)
                if (s.Transition(i) is { IsEpsilon: true } e and not RuleTransition) work.Push(e.target);
        }
        return seen;
    }

    /// <summary>The band is ERROR severity at every edition and every strictness. §4.2.2 makes a general format
    /// the definition of what may be written; there is no permissive reading of an unrecognized clause that is
    /// not "compile something the programmer did not write", so it never downgrades to a warning.</summary>
    [Fact]
    public void TheClosedFormatBand_IsErrorSeverity_AndOwnsItsCodesExclusively()
    {
        foreach (var d in new[]
                 {
                     DiagnosticCatalog.DataClauseUnrecognized,
                     DiagnosticCatalog.ClosedFormatUnrecognizedClause,
                     DiagnosticCatalog.ClosedFormatUnrecognizedParagraph,
                 })
            Assert.Equal(Editions.EditionSeverity.Error, d.Severity);

        foreach (string code in new[] { "COBOLNET1941", "COBOLNET1970", "COBOLNET1971" })
            Assert.Single(DiagnosticCatalog.All, d => d.Code == code);
    }
}
