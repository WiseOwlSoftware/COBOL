// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The corpus-manifest drift guard (rearchitecture P0 step 7) — makes the fold of the three former green-NIST sources
/// into <c>tests/nist/corpus.tsv</c> provably lossless and self-consistent. Mirrors the existing
/// <c>ConstructRegistryDriftTests</c> discipline: a green program is a manifest row, and nothing silently diverges.
/// </summary>
public sealed class CorpusManifestTests
{
    private static string NistProgramsDir => TestRepo.Nist("programs");

    [Fact]
    public void EveryProgramOnDisk_IsListed()
    {
        var listed = CorpusManifest.Rows.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = Directory.EnumerateFiles(NistProgramsDir, "*.cob")
            .Select(p => Path.GetFileNameWithoutExtension(p)!)
            .Where(n => !listed.Contains(n)).Order().ToList();
        Assert.True(missing.Count == 0, $"programs on disk not in corpus.tsv (add them): {string.Join(", ", missing)}");
    }

    [Fact]
    public void EveryGreenOrDivergent_HasGolden()
    {
        var without = CorpusManifest.Green().Where(r => !r.HasGolden).Select(r => r.Name).ToList();
        Assert.True(without.Count == 0, $"green/divergent rows lack a tests/nist/valid/<name>.txt golden: {string.Join(", ", without)}");
    }

    [Fact]
    public void EveryDivergent_CitesSpec()
    {
        var uncited = CorpusManifest.Rows.Where(r => r.Status == "divergent" && !r.Note.Contains('§'))
            .Select(r => r.Name).ToList();
        Assert.True(uncited.Count == 0, $"divergent rows must carry an ISO § citation in their note: {string.Join(", ", uncited)}");
    }

    [Fact]
    public void NoDuplicateNames()
    {
        var dupes = CorpusManifest.Rows.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(dupes.Count == 0, $"duplicate names in corpus.tsv: {string.Join(", ", dupes)}");
    }

    // ── THE TWO NIST RUNNERS ARE ONE POPULATION (kb/Work/PB750) ──────────────────────────────────────────
    // NIST is measured twice, on purpose and by two different paths: this assembly's NistDifferentialTests
    // partitions drive CompilerDriver IN-PROCESS over the green∪divergent rows on every OS, and
    // scripts/guard-fast.sh drives the `cobol` CLI as a SEPARATE PROCESS over the whole in-scope corpus from
    // bash on Linux. Neither subsumes the other — one covers the library API and Windows, the other covers the
    // shipped exe, the CCVS chain-isolation model and the golden-less programs' compile+run health.
    // What must never drift is that they are two VIEWS OF ONE MANIFEST: tests/nist/corpus.tsv. The guard's population
    // is DERIVED from it (scripts/guard-population.sh#guard_nist_population: the asserted rows, which are
    // NistDifferentialTests' population, plus their declared chain predecessors), so "the guard and the goldens agree"
    // holds by construction (kb/Work PB925, PB3383); the facts below keep the construction in place, and that
    // script's --self-test proves the derivation itself. (The dynamic half — every declared program produced exactly
    // the verdict the manifest predicts, for the compiler that ran — is scripts/guard-nist-audit.sh.)

    /// <summary>The guard's population by the shell's rule (<c>guard_nist_population</c>): every asserted row
    /// (<see cref="CorpusManifest.Green"/>) plus every program such a row declares as a chain predecessor, at any
    /// depth.</summary>
    private static IReadOnlySet<string> GuardPopulation()
    {
        var members = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(CorpusManifest.Green().Select(r => r.Name));
        while (queue.TryDequeue(out string? name))
        {
            if (!members.Add(name)) continue;
            if (CorpusManifest.Chains.TryGetValue(name, out string[]? preds))
                foreach (string pred in preds) queue.Enqueue(pred);
        }
        return members;
    }

    /// <summary>⭐ THE GUARD'S POPULATION IS DERIVED, NEVER WRITTEN OUT (kb/Work PB925, PB3383). The guard reads it
    /// from the manifest through <c>guard_nist_population</c>, so it runs every program the in-process golden suite
    /// asserts by construction. It used to be a hand-written <c>NIST_TESTS</c> block in the retired serial guard, which
    /// these facts could hold only to CONTAINMENT: a new green row that neither guard ran passed them all.</summary>
    [Fact]
    public void GuardNistPopulation_IsDerivedFromTheManifest()
    {
        string guard = File.ReadAllText(TestRepo.Scripts("guard-fast.sh"));
        Assert.True(guard.Contains("guard_nist_population >", StringComparison.Ordinal),
            "scripts/guard-fast.sh no longer takes its population from guard_nist_population (scripts/guard-population.sh)");
        string reader = File.ReadAllText(TestRepo.Scripts("guard-population.sh"));
        Assert.True(reader.Contains("guard_nist_population()", StringComparison.Ordinal),
            "scripts/guard-population.sh no longer defines guard_nist_population, the guard's one population");

        var named = Directory.EnumerateFiles(TestRepo.Scripts(), "guard*.sh").Order()
            .Where(script => File.ReadLines(script).Any(line =>
                !line.TrimStart().StartsWith('#') && line.Contains("NIST_TESTS", StringComparison.Ordinal)))
            .Select(Path.GetFileName).ToList();
        Assert.True(named.Count == 0,
            $"a guard script reads or writes a NIST_TESTS population again (kb/Work PB925): {string.Join(", ", named)}");
    }

    /// <summary>Every program the guard runs, a declared chain predecessor included, is a manifest row with a program
    /// on disk. A predecessor with no row is a program the guard could never compile (<c>guard_nist_population</c>
    /// refuses the run), and one with no source would leave its consumers to run without their input.</summary>
    [Fact]
    public void GuardNistPopulation_IsDrawnFromTheManifest()
    {
        var listed = CorpusManifest.Rows.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unrunnable = GuardPopulation()
            .Where(n => !listed.Contains(n) || !File.Exists(Path.Combine(NistProgramsDir, n + ".cob")))
            .Order().ToList();
        Assert.True(unrunnable.Count == 0,
            $"the guard's population names programs with no corpus.tsv row or no source: {string.Join(", ", unrunnable)}");
    }

    /// <summary>And the surplus over the asserted rows is only ever <c>pending</c> predecessors, programs the golden
    /// suite has NOT adopted. So on every program both legs measure, both derive their expectation from the same
    /// manifest row: neither can call a program green that the other calls red without one of them contradicting
    /// corpus.tsv.</summary>
    [Fact]
    public void GuardNistPopulation_SurplusIsOnlyPendingPredecessors()
    {
        var status = CorpusManifest.Rows.ToDictionary(r => r.Name, r => r.Status, StringComparer.OrdinalIgnoreCase);
        var asserted = CorpusManifest.Green().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var surplus = GuardPopulation().Where(n => !asserted.Contains(n)).Order().ToList();
        Assert.NotEmpty(surplus);   // ST102A and its kind: a vacuous surplus would assert over nothing
        var wrong = surplus.Where(n => status.GetValueOrDefault(n) != "pending").ToList();
        Assert.True(wrong.Count == 0,
            $"guard-only programs that are not `pending` in corpus.tsv: {string.Join(", ", wrong)}");
    }

    /// <summary>⛔ THE PB750 REGRESSION TEST. The guards once resolved the former legacy engine's CLI, whose project
    /// graph contains no <c>WiseOwl.Cobol.Compiler</c>, and drove the whole NIST leg through it, so every battery's
    /// <c>guard NIST: 353 MATCH</c> was a true statement about the ORACLE and no statement at all about the
    /// shipping compiler. This fact fails the moment the guard grows its own CLI path again instead of
    /// asking <c>scripts/guard-compiler.sh</c>, which asserts the binary's identity against its own
    /// <c>.deps.json</c> before any measurement is taken.</summary>
    [Fact]
    public void TheGuard_ResolvesTheCompilerThroughOnePlace()
    {
        string text = File.ReadAllText(TestRepo.Scripts("guard-fast.sh"));
        Assert.True(text.Contains("guard_select_compiler", StringComparison.Ordinal),
            "scripts/guard-fast.sh does not resolve its compiler through scripts/guard-compiler.sh (PB750)");
        Assert.True(text.Contains("guard_assert_compiler_identity", StringComparison.Ordinal),
            "scripts/guard-fast.sh no longer asserts the resolved binary's identity before measuring (PB750)");
        // A hard-coded output PATH, not a mention: the selection's paths live in guard-compiler.sh alone.
        Assert.False(text.Contains("bin/Debug/net10.0", StringComparison.OrdinalIgnoreCase),
            "scripts/guard-fast.sh hard-codes a compiler's bin path again — that is exactly kb/Work/PB750");

        // And the one place resolves WiseOwl COBOL.
        string resolver = File.ReadAllText(TestRepo.Scripts("guard-compiler.sh"));
        Assert.Contains("src/WiseOwl.Cobol.Cli", resolver, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⛔ THE GUARD DRIVES ONE COMPILER (kb/Work R69, PB2109). A second (legacy) compiler was reachable from the guard
    /// scripts through a selector arm (<c>GUARD_COMPILER=legacy</c>, the <c>…_LEGACY_DIFFERENTIAL=1</c> switch), an
    /// exemption list for the programs it diverged on (<c>LEGACY_DIVERGENT</c>, <c>GUARD_DIVERGENT</c>) and its own
    /// Unit and Integration legs. All of it is retired with the engine, and this fact keeps every guard script from
    /// naming any of it again: a gate that can be pointed at another compiler is the PB750 shape. The engine's product
    /// name is no word here: the RETIRED NAMES audit (scripts/rename_retired_names.py, kb/Work PB3837) refuses it in
    /// every tracked file, so this fact keeps the switch by the part of its name no prefix rename can move.
    /// </summary>
    [Fact]
    public void GuardScripts_NameNoLegacyEngine()
    {
        string[] retired = ["LEGACY_DIFFERENTIAL", "LEGACY_DIVERGENT", "GUARD_DIVERGENT"];
        var offenders = new List<string>();
        var scripts = Directory.EnumerateFiles(TestRepo.Scripts(), "guard*.sh").Order().ToList();
        Assert.True(scripts.Count >= 8, $"expected the guard family under scripts/, found {scripts.Count} — the scan would assert over nothing");
        foreach (string script in scripts)
        {
            int lineNo = 0;
            foreach (string line in File.ReadLines(script))
            {
                lineNo++;
                foreach (string word in retired)
                    if (line.Contains(word, StringComparison.Ordinal))
                        offenders.Add($"{Path.GetFileName(script)}:{lineNo} names {word}: {line.Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "a guard script names the retired legacy engine or its exemption (kb/Work PB2109):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// ⛔ THE DIVERGENT SET IS DERIVED FROM THE MANIFEST, NEVER COPIED INTO A SCRIPT (kb/Work PB898).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>scripts/guard-nist-audit.sh</c> derived each row's EXPECTED verdict straight out of the <c>divergent</c>
    /// column of <c>corpus.tsv</c>, while the RUNNER carried the divergent set as a hand-written string that
    /// <c>guard-fast.sh</c> then <c>sed</c>-extracted — so the fact was written down three times and nothing
    /// compared them. It had drifted: THIRTEEN divergent rows, TWELVE names, <c>SQ212A</c> missing, and its
    /// expected difference therefore scored as a REGRESSION while the audit beside it expected an exemption.
    /// (That exemption list is itself retired with the former legacy engine, kb/Work PB2109; the shape this fact
    /// forbids is not.)
    /// </para>
    /// <para>
    /// The repair is the derivation (<c>scripts/guard-population.sh</c>), which makes the equality true BY
    /// CONSTRUCTION; this fact is what keeps "by construction" true (CLAUDE.md rule 5 — pair the structure with
    /// a drift test). It is deliberately stated over ANY NIST-shaped name list in ANY guard script rather than
    /// over one variable, because the defect is the SHAPE — a hand-maintained list where a
    /// manifest column exists — and the next one will have a different variable name
    /// (<c>feedback_scan_all_similar</c>). The assignment may span lines: the last population written out, the retired
    /// serial guard's <c>NIST_TESTS</c> (kb/Work PB925), was a 27-line block a one-line scan could not see.
    /// </para>
    /// </remarks>
    [Fact]
    public void GuardScripts_CarryNoHandMaintainedNistNameList()
    {
        // A NIST program name: two letters, three digits, a letter (NC101A, SQ212A, IX214A).
        var name = new System.Text.RegularExpressions.Regex(@"^[A-Z]{2}[0-9]{3}[A-Z]$");
        // NAME="…", the quoted value running over as many lines as it likes ([^"] matches a newline).
        var assignment = new System.Text.RegularExpressions.Regex(@"^[ \t]*([A-Za-z_][A-Za-z0-9_]*)=""([^""]*)""",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        var offenders = new List<string>();
        foreach (string script in Directory.EnumerateFiles(TestRepo.Scripts(), "guard*.sh").Order())
        {
            string source = File.ReadAllText(script);
            foreach (System.Text.RegularExpressions.Match m in assignment.Matches(source))
            {
                int names = m.Groups[2].Value
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                    .Count(t => name.IsMatch(t));
                int lineNo = source.AsSpan(0, m.Index).Count('\n') + 1;
                if (names >= 2) offenders.Add($"{Path.GetFileName(script)}:{lineNo} {m.Groups[1].Value} ({names} names)");
            }
        }

        Assert.True(offenders.Count == 0,
            "a guard script writes a NIST program list out by hand where tests/nist/corpus.tsv already holds it "
            + "(kb/Work PB898). Derive it — scripts/guard-population.sh is the ONE reader, and "
            + "scripts/guard-nist-audit.sh derives the same column to decide each row's expected verdict:\n  "
            + string.Join("\n  ", offenders));

        // The derivation exists, reads the manifest, and keys on the status column the manifest actually uses.
        string helper = TestRepo.Scripts("guard-population.sh");
        Assert.True(File.Exists(helper), $"the ONE reader of the manifest's divergent column is missing: {helper}");
        string text = File.ReadAllText(helper);
        Assert.Contains("tests/nist/corpus.tsv", text, StringComparison.Ordinal);
        Assert.Contains("\"divergent\"", text, StringComparison.Ordinal);

        // The guard and its program runner both read it — the two-arm check: fixing one arm and leaving the other is
        // this repo's most reproducible defect shape (feedback_two_arm_dispatch).
        foreach (string script in new[] { "guard-fast.sh", "guard-run-group.sh" })
        {
            Assert.Contains("guard-population.sh", File.ReadAllText(TestRepo.Scripts(script)), StringComparison.Ordinal);
        }

        // And the manifest really has divergent rows to derive — a reader over an empty column would pass every
        // check above while reading nothing (feedback_a_dead_lookup_is_also_unverified).
        Assert.True(CorpusManifest.Rows.Count(r => r.Status == "divergent") > 0,
            "tests/nist/corpus.tsv declares no `divergent` rows, so this fact is asserting over an empty set.");
    }

    /// <summary>
    /// ⛔ A <c>divergent</c> ROW IS ONE OF TWO KINDS, AND BOTH NIST RUNNERS TELL THEM APART BY ONE GRAMMAR
    /// (kb/Work PB1955).
    /// </summary>
    /// <remarks>
    /// A note beginning <c>TERMINATES EC-…</c> inverts the reading of a <c>divergent</c> row: the golden records a
    /// run that continued past a fatal I-O status nothing covers, and WiseOwl COBOL's ISO §9.1.13.1 choice (Annex A.1
    /// item 103) ends the run unit there, so the run must end naming that exception. The CLI guard read
    /// every <c>divergent</c> row the plain way, so PB322's two TERMINATES rows turned CI's guard red (CI run
    /// 37217227958) while this assembly's NistDifferentialTests, which knew the marker, was green. The shell has
    /// ONE reader (<c>scripts/guard-population.sh</c>, which the guard, its program runner and the audit ask) and C#
    /// has one (<see cref="CorpusRow.ExpectedTermination"/>); this fact holds their grammar equal, and the guard's
    /// scripts to the reader, so the two NIST legs cannot read the same row two ways again.
    /// </remarks>
    [Fact]
    public void TerminatesMarker_IsOneGrammarForBothReaders()
    {
        string helper = File.ReadAllText(TestRepo.Scripts("guard-population.sh"));
        var shell = System.Text.RegularExpressions.Regex.Match(helper, @"^GUARD_TERMINATES_MARKER='([^']+)'$",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        Assert.True(shell.Success, "scripts/guard-population.sh no longer states GUARD_TERMINATES_MARKER — the shell's one reader of the marker");

        // The C# pattern captures the exception-name; the awk ERE matches the same text without the group.
        string csharpGrammar = CorpusRow.TerminatesMarkerPattern.Replace("(", "", StringComparison.Ordinal).Replace(")", "", StringComparison.Ordinal);
        Assert.Equal(csharpGrammar, shell.Groups[1].Value);

        // The C# reader on the shapes guard-population.sh's self-test feeds the shell reader (its case 3).
        static string? Ec(string status, string note) => new CorpusRow("ZZ999A", "ZZ", status, [], true, note).ExpectedTermination;
        Assert.Equal("EC-I-O-PERMANENT-ERROR", Ec("divergent", "TERMINATES EC-I-O-PERMANENT-ERROR - ISO 9.1.13.1: the run unit ends"));
        Assert.Null(Ec("divergent", "ISO 14.9.28.4 CCVS-DEFECT: the golden carries one failure"));
        Assert.Null(Ec("divergent", "ISO 9.1.13.1: another compiler TERMINATES EC-I-O where this does not"));
        Assert.Null(Ec("green", "TERMINATES EC-I-O-LOGIC-ERROR - a green row is never a TERMINATES row"));

        // The guard, its program runner and the audit read the declaration through that file, never on their own (the
        // two-arm check).
        foreach (string script in new[] { "guard-fast.sh", "guard-run-group.sh", "guard-nist-audit.sh" })
        {
            string text = File.ReadAllText(TestRepo.Scripts(script));
            Assert.True(text.Contains("guard-population.sh", StringComparison.Ordinal),
                $"scripts/{script} does not source scripts/guard-population.sh, the one shell reader of corpus.tsv (PB1955)");
            Assert.False(System.Text.RegularExpressions.Regex.IsMatch(text, @"^[^#]*TERMINATES EC-\[", System.Text.RegularExpressions.RegexOptions.Multiline),
                $"scripts/{script} restates the TERMINATES marker instead of asking scripts/guard-population.sh (PB1955)");
        }
        Assert.Contains("guard_termination_verdict", File.ReadAllText(TestRepo.Scripts("guard-run-group.sh")), StringComparison.Ordinal);
    }

    /// <summary>A note that MEANS the marker must SAY it in the one grammar both readers parse: a row whose note
    /// begins with the word <c>TERMINATES</c> and is not a <c>divergent</c> row with a well-formed marker naming a
    /// FATAL level-3 exception-name (ISO §14.6.13.1.6 Table 13, <see cref="ExceptionCatalog"/>) would be read by
    /// both runners as a plain row, and each would score its termination as a regression.</summary>
    [Fact]
    public void EveryTerminatesNote_IsAWellFormedMarkerOnADivergentRow()
    {
        var malformed = new List<string>();
        foreach (CorpusRow row in CorpusManifest.Rows.Where(r => r.Note.StartsWith("TERMINATES", StringComparison.OrdinalIgnoreCase)))
        {
            string? ec = row.ExpectedTermination;
            if (ec is null)
            {
                malformed.Add($"{row.Name}: status `{row.Status}`, note `{row.Note}` — not a divergent row with `TERMINATES EC-<NAME>`");
            }
            else if (!ExceptionCatalog.TryGet(ec, out EcInfo info) || info.Level != 3 || !info.IsFatal)
            {
                malformed.Add($"{row.Name}: {ec} is not a fatal level-3 exception-name of Table 13");
            }
        }
        Assert.True(malformed.Count == 0, "corpus.tsv TERMINATES rows that neither reader can act on:\n  " + string.Join("\n  ", malformed));
    }

    /// <summary>The fold is provably LOSSLESS: green∪divergent equals the committed snapshot of the former
    /// <c>[InlineData]</c> names (<c>corpus-green-baseline.txt</c>). If step 8 or any later edit adds/drops a green
    /// program, this fails until the baseline is deliberately re-pinned.</summary>
    [Fact]
    public void GreenSet_MatchesInlineDataBaseline()
    {
        string baselineFile = TestRepo.Tests("WiseOwl.Cobol.Tests.Conformance", "corpus-green-baseline.txt");
        var baseline = File.ReadLines(baselineFile)
            .Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var green = CorpusManifest.Green().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = baseline.Except(green).Order().ToList();
        var extra = green.Except(baseline).Order().ToList();
        Assert.True(missing.Count == 0 && extra.Count == 0,
            $"green set drifted from the [InlineData] baseline — missing [{string.Join(", ", missing)}] extra [{string.Join(", ", extra)}]");
    }

    /// <summary>The token a <c>corpus.tsv</c> note carries to declare that the CCVS PROGRAM'S OWN EXPECTATION,
    /// not the compiler, is what the ISO text contradicts — so its golden legitimately records a failing CCVS
    /// test. Distinct from the ordinary <c>divergent</c> reason (the golden was re-baselined to the ISO-conforming
    /// output), which never changes a report's PASS/FAIL column.</summary>
    private const string CcvsDefectMarker = "CCVS-DEFECT";

    /// <summary>⛔ THE GOLDEN-BASELINE INVARIANT, IN BOTH DIRECTIONS. A CCVS report footer reading
    /// <c>NNN TEST(S) FAILED</c> inside a committed golden is how a bad baseline hides: every later run then
    /// matches it and the suite is green over a program that fails its own self-check (the IX108A shape).
    /// <c>scripts/guard-verdict.sh</c> used to police this by scoring any non-zero footer as a REGRESSION, but
    /// that check sits AFTER a byte-exact match with the golden, so it could only ever fire on a golden defect —
    /// and it fired on the one golden that is deliberately allowed to carry a failure. The rule therefore moved
    /// here, where it is a real audit of the BASELINE rather than of the run, and it is stated as a SET EQUALITY
    /// so neither direction can rot: a golden that quietly acquires a failure is caught, and a declaration left
    /// behind after a golden is repaired is caught too (feedback_measure_the_selectors_complement).
    /// <para>The one declared member today is <b>NC201A</b>: PFM-TEST-F4-23 ("ORDER OF INITIALISATION OF VARYING
    /// IDENTIFIERS") asserts SIX body executions for <c>VARYING A … AFTER B FROM A …</c> under TEST BEFORE,
    /// while ISO §14.9.28.4 GR13 e) 2 a–c set the inner induction variable to its initialization value BEFORE
    /// augmenting the one to its left — so B is reset from the PRE-augment A and the statement runs EIGHT.
    /// The determination is in <c>docs/CONFORMANCE.md</c> §3; the citation-bearing pin is in
    /// <c>SpecPinnedNistTests</c>; kb/Work PB436.</para></summary>
    [Fact]
    public void GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects()
    {
        var declared = CorpusManifest.Rows
            .Where(r => r.Note.Contains(CcvsDefectMarker, StringComparison.Ordinal))
            .Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The CCVS footer, read from the golden itself. "NO  TEST(S) FAILED" is the clean form and does not match.
        var footer = new System.Text.RegularExpressions.Regex(@"^\s*(\d+) TEST\(S\) FAILED",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        var carrying = Directory.EnumerateFiles(TestRepo.Nist("valid"), "*.txt")
            .Where(f => footer.Matches(File.ReadAllText(f)).Any(m => int.Parse(m.Groups[1].Value) > 0))
            .Select(f => Path.GetFileNameWithoutExtension(f)!).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var undeclared = carrying.Except(declared).Order().ToList();
        var stale = declared.Except(carrying).Order().ToList();
        Assert.True(undeclared.Count == 0 && stale.Count == 0,
            $"goldens carrying a CCVS failure but not declared {CcvsDefectMarker} in tests/nist/corpus.tsv "
            + $"[{string.Join(", ", undeclared)}]; declared but carrying none [{string.Join(", ", stale)}]");

        // A declaration is a spec adjudication, so it takes the divergent status (whose note EveryDivergent_CitesSpec
        // already forces to carry an ISO §) — a `green` row could otherwise declare one with no citation at all.
        var notDivergent = CorpusManifest.Rows
            .Where(r => r.Note.Contains(CcvsDefectMarker, StringComparison.Ordinal) && r.Status != "divergent")
            .Select(r => r.Name).Order().ToList();
        Assert.True(notDivergent.Count == 0,
            $"{CcvsDefectMarker} rows must be status `divergent` so their ISO citation is enforced: {string.Join(", ", notDivergent)}");
    }
}
