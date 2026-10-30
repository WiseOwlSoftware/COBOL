// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

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
/// <para><b>Both directions, and from two independent sources.</b> The GRAMMAR facts read the generated parser's ATN
/// and require that the word run's pieces are called from the error production's two forms and nowhere else — a new
/// catch-all cannot be built from them under a new name — and that each list writes the form its follow set calls for
/// (kb/Work PB3390). The REFLECTION facts read the generated parser and require that every context type able to hold
/// the error production has a <see cref="ClosedFormats"/> row, and that every row names a context type the grammar
/// still produces — a new closed format cannot parse into silence, and a stale row (a dead lookup, which this
/// repository has learned is never contradicted) cannot linger.</para>
/// </summary>
public sealed class ClosedFormatDriftTests : WiseOwlCobolTestBase
{
    /// <summary>The error production's two forms, by context type (kb/Work PB3390).</summary>
    private static readonly Type[] ErrorProductionContexts =
        [typeof(Core.UnrecognizedClauseContext), typeof(Core.UnrecognizedDataClauseContext)];

    /// <summary>The rules that make up the word run: its opening, its tail item and the integer-led literals.</summary>
    private static readonly int[] RunPieces = [Core.RULE_residueOpening, Core.RULE_residueItem, Core.RULE_residueInteger];

    /// <summary>Every (calling rule, called rule) pair of the generated parser's ATN.</summary>
    private static IEnumerable<(int Caller, int Callee, RuleTransition Call)> Calls(ATN atn) =>
        atn.states.Where(s => s is not null)
            .SelectMany(s => Enumerable.Range(0, s.NumberOfTransitions).Select(i => (s, t: s.Transition(i))))
            .Where(x => x.t is RuleTransition)
            .Select(x => (x.s.ruleIndex, x.t.target.ruleIndex, (RuleTransition)x.t));

    [Fact]
    public void TheGrammarProbe_FindsTheProductionAndItsCallers()
    {
        // A probe that finds nothing would make every obligation below vacuously green
        // (feedback_verdict_evidence_invariant: a MISSING observation is not a NEGATIVE one).
        var calls = Calls(Core._ATN).ToList();
        Assert.True(calls.Count > 1000, $"only {calls.Count} rule call(s) in the ATN — the probe is broken");
        Assert.Equal(ClosedFormats.ErrorProductionRules.Count, ErrorProductionContexts.Length);
        foreach (int form in ClosedFormats.ErrorProductionRules)
            Assert.Contains(calls, c => c.Callee == form);
        foreach (int piece in RunPieces)
            Assert.Contains(calls, c => c.Callee == piece);
    }

    /// <summary>⛔ THE GRAMMAR OBLIGATION. The word run's pieces — the unbounded word-run matcher — may be called ONLY
    /// from the error production's two forms (and from one another: a tail item is an opening, or an `&amp;` operand).
    /// Any other call is a new total sink over some alternative list, which is the defect PB487 and PB829 each spent a
    /// landing closing. And the two forms differ in ONE thing, the integer-led literals (kb/Work PB3390): only
    /// <c>unrecognizedClause</c> calls <c>residueInteger</c>, and no FIRST set of the data form's run holds INTEGERLIT,
    /// so the data division's run can never swallow the next entry's level-number.
    /// <para>⚠ THE COMMENT-ENTRY SINK IS NOT THIS. <c>commentEntry</c> (the one body of the six identification comment
    /// paragraphs) is also an unbounded token run and is CORRECT: a comment-entry is arbitrary text by
    /// definition, which is why ISO/IEC 1989:2023 defines no syntax for one anywhere. The distinction is what the
    /// sink sits over — a COMMENT-ENTRY (right) or a CLAUSE / PARAGRAPH LIST (this defect) — so it is
    /// deliberately outside this fact's reach.</para></summary>
    [Fact]
    public void TheWordRun_IsCalledOnlyByTheErrorProduction()
    {
        var atn = Core._ATN;
        var allowed = ClosedFormats.ErrorProductionRules.Concat(RunPieces).ToHashSet();
        var offenders = Calls(atn).Where(c => RunPieces.Contains(c.Callee) && !allowed.Contains(c.Caller))
            .Select(c => $"{Core.ruleNames[c.Caller]} -> {Core.ruleNames[c.Callee]}").Distinct().ToList();
        Assert.True(offenders.Count == 0,
            "the word run is called outside the error production, so some alternative list has a total sink again "
            + "and whatever it swallows will be accepted in silence (kb/Work PB829): " + string.Join(" | ", offenders));

        var integerCallers = Calls(atn).Where(c => c.Callee == Core.RULE_residueInteger)
            .Select(c => Core.ruleNames[c.Caller]).Distinct().ToList();
        Assert.Equal(["unrecognizedClause"], integerCallers);
        foreach (int rule in new[] { Core.RULE_unrecognizedDataClause, Core.RULE_residueOpening, Core.RULE_residueItem })
            Assert.False(atn.NextTokens(atn.ruleToStartState[rule]).Contains(CobolLexer.INTEGERLIT),
                $"{Core.ruleNames[rule]} can begin with INTEGERLIT: the data division's run would swallow a level-number");
        Assert.True(atn.NextTokens(atn.ruleToStartState[Core.RULE_unrecognizedClause]).Contains(CobolLexer.INTEGERLIT),
            "unrecognizedClause cannot open at an unsigned integer: SOURCE-COMPUTER. XYZ 12. would misread XYZ");
    }

    /// <summary>⛔ EACH LIST WRITES THE FORM ITS FOLLOW SET CALLS FOR (kb/Work PB3390). The data form exists because an
    /// unsigned integer after a list's separator period may be the next entry's level-number (ISO §8.3.2.2.13); the
    /// general form holds the integer because nothing after the other lists begins with one. Read from the generated
    /// parser: from each call of the production, the separator periods reachable without consuming a token, and what
    /// may begin after each (entering rules, leaving them to every caller, never into the production itself). A list
    /// after whose period INTEGERLIT may come must call <c>unrecognizedDataClause</c>; every other list
    /// <c>unrecognizedClause</c>. A closed format added tomorrow is held to the same answer.</summary>
    [Fact]
    public void EachList_WritesTheFormItsFollowSetCallsFor()
    {
        var atn = Core._ATN;
        var callers = Calls(atn).Where(c => ClosedFormats.IsErrorProduction(c.Callee)).ToList();
        Assert.True(callers.Count >= 10, $"only {callers.Count} call(s) of the error production — the probe is broken");
        var wrong = new List<string>();
        foreach (var (caller, callee, call) in callers)
        {
            var after = AfterThePeriod(atn, call.followState);
            Assert.True(after.Count > 0, $"{Core.ruleNames[caller]}: no token found after its period — the probe is broken");
            bool levelNumberMayFollow = after.Contains(CobolLexer.INTEGERLIT);
            int expected = levelNumberMayFollow ? Core.RULE_unrecognizedDataClause : Core.RULE_unrecognizedClause;
            if (callee != expected)
                wrong.Add($"{Core.ruleNames[caller]} calls {Core.ruleNames[callee]}, but an unsigned integer "
                          + (levelNumberMayFollow ? "may" : "cannot") + $" follow its period: write {Core.ruleNames[expected]}");
        }
        Assert.True(wrong.Count == 0, string.Join(" | ", wrong));
        // The probe can tell the two apart: the data description entry's list is the data form's.
        Assert.Contains(callers, c => c.Caller == Core.RULE_dataDescriptionClause && c.Callee == Core.RULE_unrecognizedDataClause);
        Assert.Contains(callers, c => c.Caller == Core.RULE_objectComputerClause && c.Callee == Core.RULE_unrecognizedClause);
    }

    /// <summary>The tokens that may begin what follows a separator period closing the list whose production call
    /// resumes at <paramref name="resume"/>: the DOT edges reachable from it without consuming a token, then the
    /// first tokens after each.</summary>
    private static IntervalSet AfterThePeriod(ATN atn, ATNState resume)
    {
        var callersOf = Calls(atn).ToLookup(c => c.Callee, c => c.Call.followState);
        var after = new IntervalSet();
        var seen = new HashSet<int>();
        var work = new Stack<ATNState>([resume]);
        while (work.TryPop(out var s))
        {
            if (!seen.Add(s.stateNumber)) continue;
            if (s is RuleStopState) { foreach (var f in callersOf[s.ruleIndex]) work.Push(f); continue; }
            for (int i = 0; i < s.NumberOfTransitions; i++)
            {
                var t = s.Transition(i);
                if (t is RuleTransition call)
                {
                    // A call that must consume a token ends the search on that path; a nullable one is stepped over.
                    if (atn.NextTokens(call.target).Contains(TokenConstants.EPSILON)) work.Push(call.followState);
                }
                else if (t.IsEpsilon) work.Push(t.target);
                else if (t is not NotSetTransition && t.Label?.Contains(CobolLexer.DOT) == true)   // `~DOT` excludes it
                    after.AddAll(FirstTokens(atn, t.target, callersOf));
            }
        }
        return after;
    }

    /// <summary>The tokens that may come first from <paramref name="start"/>: rules entered (but never the error
    /// production, whose own opening is not "what follows"), a rule's end leaving to the call that entered it, or to
    /// every caller where the search entered none.</summary>
    private static IntervalSet FirstTokens(ATN atn, ATNState start, ILookup<int, ATNState> callersOf)
    {
        var first = new IntervalSet();
        var seen = new HashSet<(int, Frame?)>();
        var work = new Stack<(ATNState State, Frame? Calls)>([(start, null)]);
        while (work.TryPop(out var c))
        {
            var (s, calls) = c;
            if (!seen.Add((s.stateNumber, calls))) continue;
            if (s is RuleStopState)
            {
                if (calls is not null) work.Push((atn.states[calls.FollowState], calls.Next));
                else foreach (var f in callersOf[s.ruleIndex]) work.Push((f, null));
                continue;
            }
            for (int i = 0; i < s.NumberOfTransitions; i++)
            {
                var t = s.Transition(i);
                if (t is RuleTransition call)
                {
                    if (!ClosedFormats.IsErrorProduction(call.target.ruleIndex))
                        work.Push((call.target, new Frame(call.followState.stateNumber, calls)));
                }
                else if (t.IsEpsilon) work.Push((t.target, calls));
                else if (t is not NotSetTransition and not WildcardTransition && t.Label is { } label) first.AddAll(label);
                else   // a `~set` or `.` edge: every token type it matches, so a sink that begins a construct is not missed
                    for (int type = TokenConstants.MinUserTokenType; type <= atn.maxTokenType; type++)
                        if (t.Matches(type, TokenConstants.MinUserTokenType, atn.maxTokenType)) first.Add(type);
            }
        }
        return first;
    }

    private sealed record Frame(int FollowState, Frame? Next);

    /// <summary>The context types the GENERATED parser says can hold the error production: ANTLR emits a no-argument
    /// accessor returning the production's context type (either form) on the context class of every rule whose body
    /// references it.</summary>
    private static IReadOnlyList<Type> ContextsCarryingTheErrorProduction() =>
        typeof(Core).GetNestedTypes(BindingFlags.Public)
            .Where(t => typeof(ParserRuleContext).IsAssignableFrom(t)
                        && !ErrorProductionContexts.Contains(t)
                        && t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Any(m => m.GetParameters().Length == 0
                                   && ErrorProductionContexts.Any(p => m.ReturnType == p || m.ReturnType == p.MakeArrayType())))
            .ToList();

    /// <summary>⛔ THE TABLE OBLIGATION, forward. A closed general format that gains the error production without
    /// a <see cref="ClosedFormats"/> row would refuse its residue with a message that cannot name the format —
    /// which is how the eight formats came to share one anonymous catch-all in the first place.</summary>
    [Fact]
    public void EveryContextCarryingTheErrorProduction_HasAClosedFormatRow()
    {
        var carriers = ContextsCarryingTheErrorProduction();
        Assert.True(carriers.Count >= 8,
            $"only {carriers.Count} context type(s) carry the error production — the reflection probe is broken, "
            + "and both obligations below would be vacuously satisfied");
        var missing = carriers.Where(t => !ClosedFormats.ByContext.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0,
            "a closed general format ends in the error production with no ClosedFormats.ByContext row, so "
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
            "ClosedFormats.ByContext maps a context type that no longer ends in the error production: "
            + string.Join(", ", dead));
    }

    /// <summary>Prove the guards can FAIL (feedback_green_gates_arent_evidence): a context type that does NOT
    /// carry the error production must be absent from the carrier set, and a fabricated rule name must not
    /// satisfy the grammar fact. Run against real-but-unrelated subjects rather than by mutating the files.</summary>
    [Fact]
    public void TheObligations_CanFail()
    {
        Assert.DoesNotContain(typeof(Core.ProcedureDivisionContext), ContextsCarryingTheErrorProduction());
        Assert.False(ClosedFormats.IsErrorProduction(Core.RULE_procedureDivision));
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
            Assert.Contains(f.Diagnostic.Code, new[] { "COBOL1941", "COBOL1970", "COBOL1971" });
            Assert.True(f.FormatLabel.Length == 0 || f.FormatLabel.EndsWith(' '),
                $"{type.Name}: FormatLabel must carry its own trailing space so the message needs no second template");
            // The noun and the code agree: a PARAGRAPH list is 1971, a CLAUSE list is 1970 (or 1941, the code
            // §13.16.2 kept from kb/Work PB487). A mismatch sends the reader to the wrong subclause.
            if (f.Noun == "paragraph") Assert.Equal("COBOL1971", f.Diagnostic.Code);
            else Assert.Contains(f.Diagnostic.Code, new[] { "COBOL1941", "COBOL1970" });
        }
    }

    /// <summary>⛔ THE PARSE LAYER'S KEY AGREES WITH THE TREE'S (kb/Work PB2533). <c>CobolErrorStrategy</c> refuses a
    /// RESERVED word at a closed format's error production by the RULE INDEX of the alternative list the ATN walk finds
    /// calling the production (either form); <see cref="ClosedFormats.ByRuleIndex"/> must therefore hold every row, each
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
        // Every rule whose ATN calls the error production (either form), read from the generated parser itself.
        var callers = Calls(Core._ATN).Where(c => ClosedFormats.IsErrorProduction(c.Callee)).Select(c => c.Caller).ToHashSet();
        Assert.True(callers.Count >= 8, $"only {callers.Count} rule(s) call the error production — the ATN probe is broken");
        var unmapped = callers.Where(r => ClosedFormats.OfRule(r) is null).Select(r => Core.ruleNames[r]).ToList();
        Assert.True(unmapped.Count == 0, "rules call the error production with no ClosedFormats.ByRuleIndex entry: "
            + string.Join(", ", unmapped));
        Assert.Null(ClosedFormats.OfRule(Core.RULE_procedureDivision));   // the lookup can answer "none"
    }

    /// <summary>⛔ THE RESIDUE'S LITERAL TOKENS ARE EVERY ONE-TOKEN LITERAL BUT THE UNSIGNED INTEGER (kb/Work PB3373).
    /// The run opens at a <c>residueLiteral</c>, so <c>SOURCE-COMPUTER. XYZ "LIT".</c> reads XYZ as the
    /// computer-name; the unsigned integer is left out because it may be the next data description entry's
    /// level-number (it is <c>residueInteger</c>'s, which only the general form holds: kb/Work PB3390). The set is read from the generated parser: the token edges of <c>nonNumericLiteral</c> and
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

    /// <summary>⛔ THE RUN HOLDS EVERY NUMERIC LITERAL SHAPE EXACTLY ONCE (kb/Work PB3390). <c>numericLiteralCore</c>'s
    /// alternatives are split between the run's opening — its one-token literals (<c>residueLiteral</c>) and the
    /// leading-comma decimal <c>,5</c>, which the opening writes itself because <c>numericLiteralCore</c> also holds the
    /// unsigned integer — and <c>residueInteger</c>, the INTEGERLIT-led ones. Read as token sequences from the
    /// generated parser, so an alternative added to <c>numericLiteralCore</c> fails here until the run decides it.</summary>
    [Fact]
    public void TheRun_SplitsEveryNumericLiteralShape_BetweenTheOpeningAndTheIntegerLedLiterals()
    {
        var atn = Core._ATN;
        var core = TokenSequences(atn, Core.RULE_numericLiteralCore);
        Assert.True(core.Count >= 8, $"numericLiteralCore has {core.Count} token-sequence alternative(s) — the probe is broken");
        var opening = TokenSequences(atn, Core.RULE_residueOpening);
        var integerLed = TokenSequences(atn, Core.RULE_residueInteger);
        var singles = atn.NextTokens(atn.ruleToStartState[Core.RULE_residueLiteral]).ToIntegerList()
            .Select(t => t.ToString()).ToHashSet();
        var held = integerLed.Concat(opening).Concat(singles).ToHashSet();
        var missing = core.Where(s => !held.Contains(s)).ToList();
        Assert.True(missing.Count == 0, "numericLiteralCore shapes the run does not hold: " + string.Join(" | ", missing));
        Assert.All(integerLed, s => Assert.Equal(CobolLexer.INTEGERLIT.ToString(), s.Split(' ')[0]));
        Assert.True(integerLed.All(core.Contains), "residueInteger holds a shape numericLiteralCore does not");
        Assert.Empty(integerLed.Intersect(opening));
    }

    /// <summary>Each alternative of <paramref name="rule"/> that is a straight run of token edges (epsilon and predicate
    /// edges passed over, never a rule call), as its token types joined by spaces.</summary>
    private static HashSet<string> TokenSequences(ATN atn, int rule)
    {
        var result = new HashSet<string>();
        var stop = atn.ruleToStopState[rule];
        var work = new Stack<(ATNState State, string Path)>([(atn.ruleToStartState[rule], "")]);
        var seen = new HashSet<(int, string)>();
        while (work.TryPop(out var c))
        {
            if (!seen.Add((c.State.stateNumber, c.Path))) continue;
            if (c.State == stop) { if (c.Path.Length > 0) result.Add(c.Path.TrimStart()); continue; }
            for (int i = 0; i < c.State.NumberOfTransitions; i++)
            {
                var t = c.State.Transition(i);
                if (t is RuleTransition) continue;
                if (t.IsEpsilon) work.Push((t.target, c.Path));
                else if (t.Label is { Count: 1 } label) work.Push((t.target, c.Path + " " + label.MinElement));
            }
        }
        return result;
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

        foreach (string code in new[] { "COBOL1941", "COBOL1970", "COBOL1971" })
            Assert.Single(DiagnosticCatalog.All, d => d.Code == code);
    }
}
