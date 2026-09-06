// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The compiler-directive roster is derived, not hand-kept (kb/Work PB725) — these tests are what keeps
/// "derived" true.
///
/// <para>The defect they close: <c>ConditionalCompilationProcessor</c> carried a flat
/// <c>KnownIgnoredDirectives</c> HashSet of directive NAMES with no edition column, so eleven ISO §7.3
/// directives — <c>&gt;&gt;PUSH</c>, <c>&gt;&gt;POP</c> and <c>&gt;&gt;DISPLAY</c> among them — compiled clean at
/// <c>--std 85</c>, an edition that has no compiler directives at all, while their siblings from the same
/// Annex E.2 item 5 list drew COBOLNET0900. A name set cannot be wrong about an edition it does not record, and
/// nothing could have failed. Now the roster IS the <c>directiveWords</c> column of
/// <c>tests/version-matrix/constructs.json</c>, and <see cref="Roster_Matches_TheSpecSection"/> re-derives it
/// from §7.3 itself, so a directive added to the standard's clause list without a row is a red test rather than
/// a silent under-rejection.</para>
/// </summary>
public sealed class CompilerDirectiveCatalogDriftTests
{
    /// <summary>Every clause under ISO §7.3 whose heading names a directive — the spec's own roster.</summary>
    private static Dictionary<string, string> SpecDirectives()
    {
        string path = TestRepo.Specs("ISO_COBOL.md");
        Assert.True(File.Exists(path),
            $"the ISO transcription is missing: {path} — run `git submodule update --init --recursive`");
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(File.ReadAllText(path),
                     @"^#{4}\s+(7\.3\.\d+)\s+(.+?)\s+directive\s*$", RegexOptions.Multiline))
        {
            // "SOURCE FORMAT directive" — the directive is headed by its FIRST word (§7.3.3 SR6: compiler-
            // instruction opens with ONE compiler-directive word; FORMAT and IS are optional words in the
            // §7.3.24.2 general format).
            string word = m.Groups[2].Value.Split(' ')[0].Trim();
            found[word] = m.Groups[1].Value;
        }

        return found;
    }

    /// <summary>
    /// THE spec-derived check: every §7.3.x directive the standard defines has a catalog row, and every row
    /// the catalog claims is a §7.3 directive is one — except the two Annex E.2 item 21 directives the 2023
    /// text REMOVED, which by construction no longer have a §7.3 clause to be found in and are named here so the
    /// exemption is explicit rather than a silently loose comparison.
    /// </summary>
    [Fact]
    public void Roster_Matches_TheSpecSection()
    {
        var spec = SpecDirectives();
        var catalog = CompilerDirectiveCatalog.Words.ToHashSet(StringComparer.Ordinal);

        // Removed in 2023 (Annex E.2 item 21): still recognized, and still gated, at the editions that HAD them,
        // so they are catalog rows with no 2023 clause. Every other exemption is a bug.
        string[] removedIn2023 = ["FLAG-85", "FLAG-NATIVE-ARITHMETIC"];
        foreach (string w in removedIn2023)
            Assert.True(CompilerDirectiveCatalog.Find(w)?.RemovedIn == 2023,
                $"{w} is exempted from the §7.3 roster check because Annex E.2 item 21 removed it in 2023 — "
                + "its row must say so");

        var missing = spec.Keys.Where(w => !catalog.Contains(w)).Order(StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0,
            $"ISO §7.3 defines {spec.Count} directives; these have NO constructs.json row and are therefore "
            + $"ungated at every edition: [{string.Join(", ", missing)}] — add a row with directiveWords and "
            + "re-run scripts/gen-constructs.ps1");

        // The reverse direction is asked PER ROW, not per word: §7.3.16's construct is spelled
        // >>IF / >>ELSE / >>END-IF and §7.3.13's is >>EVALUATE / >>WHEN / >>END-EVALUATE, so the companion words
        // have no clause heading of their own and must not be — a bare >>ELSE is not a directive. What must
        // hold is that every ROW is anchored: at least one of its words heads a §7.3 clause.
        var unanchored = ConstructRegistry.Entries
            .Where(e => e.DirectiveWords.Count > 0 && e.RemovedIn != 2023
                        && !e.DirectiveWords.Any(spec.ContainsKey))
            .Select(e => $"{e.Id} [{string.Join("/", e.DirectiveWords)}]").Order(StringComparer.Ordinal).ToList();
        Assert.True(unanchored.Count == 0,
            $"these rows claim directive words that head NO ISO §7.3 clause: [{string.Join(", ", unanchored)}] "
            + "— §7.3.3 SR9 reserves >>IMP for implementor directives, so a non-standard word needs either a "
            + "clause or a written exemption here");
    }

    /// <summary>
    /// The clause a row cites is the clause the spec puts the directive in. The PUSH/POP pairing is the reason
    /// this is a test and not a reading: §7.3.20 is POP and §7.3.22 is PUSH, the reverse of the order the old
    /// code comment listed them in (kb/Work PB725).
    /// </summary>
    [Fact]
    public void EveryRow_CitesItsOwnClause()
    {
        var wrong = new List<string>();
        foreach (var (word, clause) in SpecDirectives())
            if (CompilerDirectiveCatalog.Find(word) is { } row && !row.Citation.Contains("§" + clause + ";")
                && !row.Citation.Contains("§" + clause + " "))
                wrong.Add($"{word} is §{clause} but its row cites \"{row.Citation}\"");

        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    /// <summary>Structural sanity: a directive word heads at most one row, and every row that claims words
    /// gates them with the edition band the registry funnel emits.</summary>
    [Fact]
    public void Structural_Sanity()
    {
        var words = CompilerDirectiveCatalog.Words.ToList();
        Assert.True(words.Count >= 17, $"only {words.Count} directive words — §7.3 defines 17 directives");
        Assert.All(words, w => Assert.Matches("^[A-Z][A-Z0-9-]*$", w));
        Assert.Equal(words.Count, words.Distinct(StringComparer.Ordinal).Count());
        Assert.All(words, w =>
        {
            var row = CompilerDirectiveCatalog.Find(w)!;
            Assert.Contains(row.DiagnosticCode, new[] { "COBOLNET0900", "COBOLNET0902" });
            Assert.InRange(row.IntroducedIn, 2002, 2023);   // COBOL-85 has no compiler directives at all
        });
        Assert.True(CompilerDirectiveCatalog.IsDirective("push"));   // case-insensitive (§8.3.1: words fold)
        Assert.Null(CompilerDirectiveCatalog.Find("IFF"));           // a typo is NOT a directive
    }

    /// <summary>
    /// <c>Frontend.LeftDirectives</c> answers WHICH STAGE OWNS THE LINE; the catalog answers WHETHER THE WORD
    /// MAY APPEAR. They are different questions, but a word left for a downstream stage that no longer has a
    /// catalog row would sail past the gate — so the subset relation is asserted, not assumed.
    /// </summary>
    [Fact]
    public void LeftDirectives_AreAllCatalogRows()
    {
        var orphans = CobolNet.Frontend.Frontend.LeftDirectives
            .Where(w => !CompilerDirectiveCatalog.IsDirective(w)).Order(StringComparer.Ordinal).ToList();
        Assert.True(orphans.Count == 0,
            $"Frontend.LeftDirectives holds [{string.Join(", ", orphans)}] with no catalog row — those lines "
            + "would reach their stage with no edition gate");
    }

    /// <summary>
    /// The behavioural proof, and the one that would have failed before PB725: at <c>--std 85</c> EVERY
    /// recognized directive word is rejected, because COBOL-85 has no compiler directives. A green gate that
    /// never looked at what changed proves nothing (feedback_green_gates_arent_evidence), so this drives the
    /// real text-manipulation stage rather than inspecting the table.
    /// </summary>
    [Fact]
    public void EveryDirective_IsRejectedAt85()
    {
        var silent = new List<string>();
        foreach (string word in CompilerDirectiveCatalog.Words)
        {
            var bag = new DiagnosticBag();
            ConditionalCompilationProcessor.Process(
                $">>{word} ALL\nIDENTIFICATION DIVISION.\n", CobolNet.Frontend.Frontend.LeftDirectives,
                bag, "t.cob", dialectLevel: 85);
            if (!bag.Diagnostics.Any(d => d.Code == "COBOLNET0900")) silent.Add(word);
        }

        // >>SOURCE FORMAT is consumed one stage earlier, by ReferenceFormatProcessor, so the driver never sees
        // it — its gate is asserted separately below.
        silent.Remove("SOURCE");
        Assert.True(silent.Count == 0,
            $"[{string.Join(", ", silent)}] compile clean at --std 85, an edition with no compiler directives "
            + "(ISO §7.3 is a COBOL-2002 introduction) — the PB725 under-rejection has returned");
    }

    /// <summary>The one directive whose gate cannot live with its siblings: <c>&gt;&gt;SOURCE FORMAT</c> is
    /// consumed by the reference-format normalizer before the conditional-compilation driver runs, so its gate
    /// emits there — same row, same COBOLNET0900, one stage earlier.</summary>
    [Fact]
    public void SourceFormatDirective_IsRejectedAt85()
    {
        var bag = new DiagnosticBag();
        ReferenceFormatProcessor.NormalizeToFreeForm(
            ">>SOURCE FORMAT IS FREE\nIDENTIFICATION DIVISION.\n", 85, permissive: false, bag, "t.cob");
        Assert.Contains(bag.Diagnostics, d => d.Code == "COBOLNET0900");

        var ok = new DiagnosticBag();
        ReferenceFormatProcessor.NormalizeToFreeForm(
            ">>SOURCE FORMAT IS FREE\nIDENTIFICATION DIVISION.\n", 2002, permissive: false, ok, "t.cob");
        Assert.DoesNotContain(ok.Diagnostics, d => d.Code == "COBOLNET0900");
    }

    /// <summary>An UNRECOGNIZED <c>&gt;&gt;</c> word is not swallowed: it survives into the text so the parser
    /// names it, which is what catches a typo like <c>&gt;&gt;IFF</c>. The gate must not turn every unknown word
    /// into a consumed no-op.</summary>
    [Fact]
    public void UnrecognizedDirective_SurvivesForTheParser()
    {
        var bag = new DiagnosticBag();
        string outp = ConditionalCompilationProcessor.Process(
            ">>IFF X\nIDENTIFICATION DIVISION.\n", CobolNet.Frontend.Frontend.LeftDirectives, bag, "t.cob",
            dialectLevel: 2023);
        Assert.Contains(">>IFF", outp);
        Assert.DoesNotContain(bag.Diagnostics, d => d.Code == "COBOLNET0900");
    }
}
