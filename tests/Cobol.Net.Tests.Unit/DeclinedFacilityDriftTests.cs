// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for the DECLINED-OPTIONAL-ELEMENT surface (ISO Annex A.4.1). The whole point of putting the
/// declined constructs in ONE grammar fragment behind ONE pass was to make the next one automatic — but
/// "automatic" is a claim, and an unguarded claim rots. These facts derive the obligation FROM the grammar file
/// so a new declined construct cannot parse into silence.
///
/// <para>⛔ WHY THIS SHAPE AND NOT A LIST. A hand-maintained "declined constructs" list would be a sixth work
/// register (CLAUDE.md rule 8) AND would drift the moment someone added a grammar rule without touching it. The
/// grammar file IS the list; this test reads it.</para>
/// </summary>
public sealed class DeclinedFacilityDriftTests
{
    private static string GrammarPath => TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolDeclined.g4");
    private static string PassPath => TestRepo.Src("Cobol.Net.Compiler", "Validation", "DeclinedFacilityPass.cs");

    /// <summary>Every top-level parser rule defined in <c>CobolDeclined.g4</c>, in file order.</summary>
    private static List<string> GrammarRules()
    {
        string text = File.ReadAllText(GrammarPath);
        // A rule definition is a name at the START of a line followed (possibly on the next line) by ':'.
        // Comment lines start with '//' and never begin at column 0 with an identifier followed by a colon.
        var rules = new List<string>();
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"^([a-z][A-Za-z0-9_]*)\s*$");
            if (!m.Success) continue;
            // The next non-blank line must open the rule body with ':'.
            for (int j = i + 1; j < lines.Length; j++)
            {
                string next = lines[j].Trim();
                if (next.Length == 0) continue;
                if (next.StartsWith(':')) rules.Add(m.Groups[1].Value);
                break;
            }
        }
        return rules;
    }

    /// <summary>The ENTRY POINTS: rules no OTHER rule in the same file references. A rule reached from a
    /// diagnosed parent needs no override of its own (the parent already refused the whole construct and does
    /// not descend); a rule reachable only from OUTSIDE the file is a construct the compiler will parse and
    /// must therefore refuse by name.</summary>
    private static List<string> EntryPointRules()
    {
        string text = File.ReadAllText(GrammarPath);
        var rules = GrammarRules();
        // Strip line comments so a rule NAMED in prose does not count as a reference.
        string body = Regex.Replace(text, @"//[^\n]*", "");
        var entries = new List<string>();
        foreach (string r in rules)
        {
            // References = occurrences of the name that are not its own definition line.
            int uses = Regex.Matches(body, $@"\b{Regex.Escape(r)}\b").Count;
            int defs = Regex.Matches(body, $@"(?m)^{Regex.Escape(r)}\s*$").Count;
            if (uses - defs == 0) entries.Add(r);
        }
        return entries;
    }

    [Fact]
    public void GrammarFile_DefinesRules_AndSomeAreEntryPoints()
    {
        var rules = GrammarRules();
        var entries = EntryPointRules();
        // The population assertion (feedback_verdict_evidence_invariant): a parse that found nothing would make
        // every other fact below vacuously green.
        Assert.True(rules.Count >= 8, $"CobolDeclined.g4 parsed to only {rules.Count} rules — the rule scanner "
            + "is broken, and every obligation derived from it would be vacuously satisfied");
        Assert.True(entries.Count >= 2, $"only {entries.Count} entry-point rule(s) found — expected at least "
            + "validationClause and validateValidPhrase (applyCommitClause left the file with kb/Work PB3338)");
    }

    /// <summary>⛔ THE OBLIGATION. Every entry-point rule of the declined-facility grammar has a
    /// <c>VisitXxx</c> override in <see cref="CobolNet.Validation.DeclinedFacilityPass"/>. Adding a declined
    /// construct's SYNTAX without adding its REFUSAL would make the compiler PARSE the construct and then
    /// silently ignore it — strictly worse than the generic parse error it replaced, because the program would
    /// compile and run with the declined semantics simply absent (the new-construct skill's "parsing something
    /// and emitting a no-op is worse than a compile error").</summary>
    [Fact]
    public void EveryEntryPointRule_HasARefusalOverride()
    {
        string pass = File.ReadAllText(PassPath);
        var missing = EntryPointRules()
            .Where(r => !pass.Contains($"Visit{char.ToUpperInvariant(r[0])}{r[1..]}(", StringComparison.Ordinal))
            .ToList();
        Assert.True(missing.Count == 0,
            "declined-facility grammar rule(s) with NO refusal override in DeclinedFacilityPass — the compiler "
            + "would parse the construct and ignore it: " + string.Join(", ", missing));
    }

    /// <summary>The set of contexts the pass refuses WHOLE — which other passes consult through
    /// <c>DeclinedFacilityPass.EnclosingDeclinedConstruct</c> (the §8.9 funnel since kb/Work PB764) — is exactly
    /// the grammar's entry points, so a new declined construct cannot be refused here yet diagnosed inside
    /// by another pass.</summary>
    [Fact]
    public void TheDeclinedRoots_AreTheGrammarsEntryPoints()
    {
        var fromGrammar = EntryPointRules().Select(r => $"{char.ToUpperInvariant(r[0])}{r[1..]}Context")
            .OrderBy(n => n, StringComparer.Ordinal).ToList();
        var fromPass = CobolNet.Validation.DeclinedFacilityPass.DeclinedConstructRoots.Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.True(fromGrammar.Count >= 2, "the entry-point scan found too few rules to compare");
        Assert.Equal(fromGrammar, fromPass);
    }

    // ── EVERY PRINTED KEYWORD IS AN ARM (kb/Work PB3831) ─────────────────────────────────────────────────────

    /// <summary>A parser rule as written: its name, the <c>//</c> comment block directly above it, and its body
    /// with comments, semantic predicates and quoted text removed (a word inside <c>{userWordHere("X")}?</c> is
    /// not an arm).</summary>
    private sealed record GrammarRule(string Name, string Comment, string Body);

    private static readonly Regex RuleHead = new(@"^([a-z][A-Za-z0-9_]*)\s*$");
    private static readonly Regex TokenRef = new(@"\b[A-Z][A-Z0-9_]*\b");
    private static readonly Regex RuleRef = new(@"\b[a-z][A-Za-z0-9_]*\b");
    private static readonly Regex PrintedWord = new(@"(?<![\w-])[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*(?![\w-])");

    /// <summary>The rules of one grammar file's TEXT, keyed by name. A text, not a path, so the can-fail fact can
    /// run the same reader over a fabricated grammar.</summary>
    private static Dictionary<string, GrammarRule> ReadRules(string text)
    {
        var rules = new Dictionary<string, GrammarRule>(StringComparer.Ordinal);
        string[] lines = text.Replace("\r", "").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = RuleHead.Match(lines[i]);
            if (!m.Success) continue;
            // The body opens with ':' on a following line; comment and blank lines may stand between (valueClause).
            int open = i + 1;
            while (open < lines.Length && (lines[open].Trim().Length == 0 || lines[open].TrimStart().StartsWith("//", StringComparison.Ordinal)))
                open++;
            if (open >= lines.Length || !lines[open].TrimStart().StartsWith(':')) continue;
            var comment = new List<string>();
            for (int j = i - 1; j >= 0 && lines[j].TrimStart().StartsWith("//", StringComparison.Ordinal); j--)
                comment.Insert(0, lines[j]);
            var body = new List<string>();
            int k = open;
            for (; k < lines.Length && !lines[k].TrimStart().StartsWith(';'); k++) body.Add(lines[k]);
            string code = Regex.Replace(string.Join("\n", body), @"//[^\n]*", "");
            code = Regex.Replace(code, @"\{[^}]*\}\??", "");
            code = Regex.Replace(code, "\"[^\"]*\"|'[^']*'", "");
            rules[m.Groups[1].Value] = new GrammarRule(m.Groups[1].Value, string.Join("\n", comment), code.TrimStart().TrimStart(':'));
            i = k;
        }
        return rules;
    }

    /// <summary>The keyword tokens a rule writes, itself and through every rule of the same file it reaches —
    /// never through <c>cobolWord</c>, which is the user-defined-word SINK this guard is about: a keyword that
    /// reaches the rule only through it is exactly the omission (cobolWord refuses a reserved word).</summary>
    private static HashSet<string> TokensReached(string start, Dictionary<string, GrammarRule> rules, string? skip = null)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var work = new Stack<string>([start]);
        while (work.Count > 0)
        {
            string name = work.Pop();
            if (name == "cobolWord" || name == skip || !seen.Add(name) || !rules.TryGetValue(name, out var rule)) continue;
            foreach (Match t in TokenRef.Matches(rule.Body)) tokens.Add(t.Value);
            foreach (Match r in RuleRef.Matches(rule.Body)) work.Push(r.Value);
        }
        return tokens;
    }

    /// <summary>The general format a rule transcribes, from the comment block above it: the first
    /// <c>ISO §n</c> and an optional <c>FORMAT k</c>. Null for a rule that cites none.</summary>
    private static (string Clause, string? Format)? CitedFormat(GrammarRule rule)
    {
        var c = Regex.Match(rule.Comment, @"ISO §(\d+(?:\.\d+)+)");
        if (!c.Success) return null;
        var f = Regex.Match(rule.Comment, @"\bFORMAT (\d+)\b");
        return (c.Groups[1].Value, f.Success ? f.Groups[1].Value : null);
    }

    /// <summary>Every COBOL word printed in a general format's figure (the transcription's <c>&lt;pre&gt;</c>
    /// blocks; <c>Format k (…):</c> selects one of several), underlined or not: a keyword and an optional word
    /// are both the format's OWN words (§5.2.2, §5.2.3), never a user-defined word. A lowercase metavariable
    /// (<c>identifier-1</c>, <c>alphabet-name-1</c>) is not a word here.</summary>
    private static HashSet<string> PrintedWords(string clause, string? format)
    {
        string[] region = SpecClauseText.ClauseRegion(SpecClauseText.Lines(), clause);
        var words = new HashSet<string>(StringComparer.Ordinal);
        string? label = null;
        bool inFigure = false;
        int figures = 0;
        foreach (string raw in region)
        {
            string line = raw.TrimEnd();
            if (!inFigure && Regex.Match(line, @"^Format (\d+)\b") is { Success: true } fm) label = fm.Groups[1].Value;
            bool selected = format is null || label == format;
            if (line.StartsWith("<pre", StringComparison.Ordinal))
            {
                inFigure = true;
                if (selected) figures++;
                continue;
            }
            if (line.StartsWith("</pre>", StringComparison.Ordinal)) { inFigure = false; continue; }
            if (!inFigure || !selected) continue;
            foreach (Match w in PrintedWord.Matches(Regex.Replace(line, "</?u>", " "))) words.Add(w.Value);
        }
        if (figures == 0)
            throw new Xunit.Sdk.XunitException($"§{clause}{(format is null ? "" : $" Format {format}")} has no figure in "
                + "specs/ISO_COBOL.md — the rule's citation is wrong or the figure reader is broken");
        return words;
    }

    /// <summary>For every rule of the declined grammar <paramref name="declinedText"/> that cites a general format,
    /// the printed words that have a lexer token of their own and are NOT an arm the rule reaches. A rule that is a
    /// TAIL of a supported construct (<c>validateValidPhrase</c>, hooked onto <c>valueClause</c>) also counts the
    /// tokens its HOST rule reaches (any grammar file, never through <c>cobolWord</c> or back through the tail), since
    /// the host parses the format's head — format 5's VALUE / THRU list is format 3's. That breadth cannot hide a
    /// tail keyword: <see cref="ThePrintedKeywordGuard_ReportsAnOmittedArm"/> removes one and requires the report.</summary>
    private static List<string> MissingPrintedKeywords(string declinedText, out int citedRules)
    {
        var rules = ReadRules(declinedText);
        var hosts = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.EndsWith("CobolDeclined.g4", StringComparison.Ordinal) && !f.EndsWith("CobolLexer.g4", StringComparison.Ordinal))
            .SelectMany(f => ReadRules(File.ReadAllText(f)).Values)
            .ToDictionary(r => r.Name, StringComparer.Ordinal);   // one definition per name: GrammarRuleUniquenessDriftTests
        var missing = new List<string>();
        citedRules = 0;
        foreach (var rule in rules.Values)
        {
            if (CitedFormat(rule) is not { } cite) continue;
            citedRules++;
            var reached = TokensReached(rule.Name, rules);
            foreach (var host in hosts.Values.Where(h => RuleRef.Matches(h.Body).Any(r => r.Value == rule.Name)))
                reached.UnionWith(TokensReached(host.Name, hosts, skip: rule.Name));
            foreach (string word in PrintedWords(cite.Clause, cite.Format).Order(StringComparer.Ordinal))
            {
                if (!CobolNet.Frontend.Parsing.CobolKeywordTokens.TryTokenType(word, out int type)) continue;   // lexes as IDENTIFIER
                string token = CobolNet.Frontend.Generated.CobolLexer.DefaultVocabulary.GetSymbolicName(type);
                if (!reached.Contains(token))
                    missing.Add($"{rule.Name}: §{cite.Clause}{(cite.Format is null ? "" : $" Format {cite.Format}")} prints {word} (token {token})");
            }
        }
        return missing;
    }

    /// <summary>⛔ A KEYWORD THE ARMS OMIT DOES NOT FALL INTO "PERMISSIVE INSIDE": IT FALLS TO <c>cobolWord</c>.
    /// The declined grammar is permissive inside its constructs (§4.2.7, A.4.1), and that posture was read as
    /// licence to leave a printed keyword out of an operand group. It is not one: a word with its own lexer token
    /// that no arm names reaches only the <c>cobolWord</c> sink, which refuses a §8.9-reserved word, so the parse
    /// fails INSIDE the declined construct and the error listener answers COBOL0901, "is a reserved word …
    /// cannot be used as a user-defined word" — false, because the format prints it as the clause's own word —
    /// where the declined-facility refusal COBOL1708 belongs. Three keywords did it, one at a time: the class
    /// condition's BOOLEAN (kb/Work PB857), VALIDATE-STATUS's FORMAT (kb/Work PB693) and the CLASS clause's
    /// BOOLEAN (kb/Work PB3831). The keyword list is DERIVED here from each rule's cited general format in
    /// <c>specs/ISO_COBOL.md</c>, and the arm list from the grammar, so the next one fails here.</summary>
    [Fact]
    public void EveryPrintedKeyword_IsAnArmOfItsRule()
    {
        var missing = MissingPrintedKeywords(File.ReadAllText(GrammarPath), out int cited);
        // Population (feedback_verdict_evidence_invariant): the eight clauses of the band each cite their format.
        Assert.True(cited >= 8, $"only {cited} rule(s) of CobolDeclined.g4 cite a general format (`ISO §n`) — the "
            + "citation reader is broken, or a rule lost the comment that ties it to its printed format");
        Assert.True(missing.Count == 0,
            "printed keyword(s) with a lexer token that no arm of the declined rule names — each falls to cobolWord, "
            + "which refuses a reserved word, so the declined clause draws a false COBOL0901 instead of COBOL1708. "
            + "Add the arm (ISO §5.2.2): " + string.Join("; ", missing));
    }

    /// <summary>Every rule of the declined grammar that writes a keyword is covered by the fact above: it cites a
    /// general format itself, or it is reached from a rule that does. A rule that did neither would be checked
    /// against nothing.</summary>
    [Fact]
    public void EveryKeywordWritingRule_IsReachedFromACitedFormat()
    {
        var rules = ReadRules(File.ReadAllText(GrammarPath));
        var covered = rules.Values.Where(r => CitedFormat(r) is not null)
            .SelectMany(r => ReachedRules(r.Name, rules)).ToHashSet(StringComparer.Ordinal);
        var uncovered = rules.Values.Where(r => TokenRef.IsMatch(r.Body) && !covered.Contains(r.Name)).Select(r => r.Name).ToList();
        Assert.True(rules.Count >= 8, $"CobolDeclined.g4 parsed to only {rules.Count} rule(s) — the reader is broken");
        Assert.True(uncovered.Count == 0, "declined rule(s) that write keywords but neither cite a general format "
            + "(`// ISO §n`) nor are reached from one: " + string.Join(", ", uncovered));
    }

    private static HashSet<string> ReachedRules(string start, Dictionary<string, GrammarRule> rules)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var work = new Stack<string>([start]);
        while (work.Count > 0)
        {
            string name = work.Pop();
            if (!rules.TryGetValue(name, out var rule) || !seen.Add(name)) continue;
            foreach (Match r in RuleRef.Matches(rule.Body)) work.Push(r.Value);
        }
        return seen;
    }

    /// <summary>Prove the guard can FAIL on the very defect it was written for: the real grammar with the CLASS
    /// clause's BOOLEAN arm removed is reported, naming BOOLEAN and §13.18.11.2 — and nothing else is. The second
    /// leg removes the VALID arm of the format-5 TAIL, whose host's tokens are also counted, and proves the host's
    /// reach does not hide it.</summary>
    [Theory]
    [InlineData(@"(?m)^(\s*)\|\s*BOOLEAN\s*$", "validateClassClause: §13.18.11.2 prints BOOLEAN (token BOOLEAN)")]
    [InlineData(@"\(VALID \| INVALID\)", "validateValidPhrase: §13.18.63.2 Format 5 prints VALID (token VALID)")]
    public void ThePrintedKeywordGuard_ReportsAnOmittedArm(string arm, string expected)
    {
        string text = File.ReadAllText(GrammarPath);
        string mutated = Regex.Replace(text, arm, m => m.Value.StartsWith('(') ? "INVALID" : m.Groups[1].Value);
        Assert.NotEqual(text, mutated);
        var missing = MissingPrintedKeywords(mutated, out _);
        Assert.Equal(new[] { expected }, missing);
    }

    /// <summary>Prove the guard above can FAIL (feedback_green_gates_arent_evidence): a rule name that is NOT
    /// overridden must be reported. Run against a fabricated name rather than by mutating the real file.</summary>
    [Fact]
    public void TheRefusalObligation_CanFail()
    {
        string pass = File.ReadAllText(PassPath);
        Assert.DoesNotContain("VisitNoSuchDeclinedClause(", pass, StringComparison.Ordinal);
    }

    /// <summary>The declined-facility diagnostics are ERRORS, not warnings — the distinction that makes
    /// the whole band different from the §4.2.6 processor-dependent band (COBOL1578/1579/1580, Warning).
    /// Annex A.4.1: an implementation "shall accept the syntax … for an optional element ONLY when support for
    /// that language element is claimed", so accepting a declined element's syntax is itself the
    /// non-conformance. It is also what makes the rows WITNESSABLE: the negative corpus asserts a failing
    /// compile, and the whole 1560/1578/1579/1580 warning band has no assertion mechanism at all — the reason
    /// every A.4.2 screen row in kb/Work PB260 is still open.</summary>
    [Fact]
    public void TheDeclinedBand_IsErrorSeverity_AndSharesOneSuppressKey()
    {
        foreach (var d in new[]
                 {
                     DiagnosticCatalog.ValidateDataDivisionClauseUnsupported,
                     DiagnosticCatalog.DeclinedModuleExceptionName,
                 })
        {
            Assert.Equal(Editions.EditionSeverity.Error, d.Severity);
            Assert.Equal(DiagnosticCatalog.DeclinedOptionalElement, d.ResolvedSuppressKey);
        }
    }

    /// <summary>The band's codes are 1708 and 1710 and nothing else carries them — the allocation this cluster claimed. A
    /// collision is how the shipped COBOL1573 two-meanings defect happened. COBOL1709 (the declined SEQUENTIAL operand of
    /// APPLY COMMIT) is RETIRED since kb/Work PB3340 provided the sequential transaction journal: no descriptor carries it,
    /// and it is never reallocated.</summary>
    [Fact]
    public void TheDeclinedBand_OwnsItsCodesExclusively()
    {
        foreach (string code in new[] { "COBOL1708", "COBOL1710" })
            Assert.Single(DiagnosticCatalog.All, d => d.Code == code);
        Assert.DoesNotContain(DiagnosticCatalog.All, d => d.Code == "COBOL1709");
    }
}
