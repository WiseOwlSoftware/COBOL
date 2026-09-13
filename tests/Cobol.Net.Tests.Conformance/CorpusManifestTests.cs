// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

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
    // What must never drift is that they are two VIEWS OF ONE MANIFEST: tests/nist/corpus.tsv. The facts below
    // assert exactly that, so "the guard and the goldens agree" is a structural property rather than something
    // re-checked by hand after every landing. (The dynamic half — every declared program produced exactly the
    // verdict the manifest predicts, for the compiler that ran — is scripts/guard-nist-audit.sh.)

    /// <summary>The guard's NIST population, parsed from the <c>NIST_TESTS="…"</c> block in
    /// <c>scripts/guard.sh</c> — the ONE place it is written down (guard-fast.sh extracts the same block by
    /// <c>sed</c> so the two guards cannot drift).</summary>
    private static IReadOnlyList<string> GuardPopulation()
    {
        var names = new List<string>();
        bool inBlock = false;
        foreach (string line in File.ReadLines(TestRepo.Scripts("guard.sh")))
        {
            if (!inBlock)
            {
                if (line.StartsWith("NIST_TESTS=\"", StringComparison.Ordinal)) inBlock = true;
                continue;
            }
            if (line.Trim() == "\"") break;
            names.AddRange(line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        Assert.True(names.Count > 0, "could not parse NIST_TESTS out of scripts/guard.sh — the parser, not the guard, is broken");
        return names;
    }

    /// <summary>Every program the guard runs is a manifest row. Without this the guard could measure a program
    /// the golden suite has never heard of, and no expectation could be derived for it.</summary>
    [Fact]
    public void GuardNistPopulation_IsDrawnFromTheManifest()
    {
        var listed = CorpusManifest.Rows.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unmanifested = GuardPopulation().Where(n => !listed.Contains(n)).Order().ToList();
        Assert.True(unmanifested.Count == 0,
            $"scripts/guard.sh runs programs absent from tests/nist/corpus.tsv: {string.Join(", ", unmanifested)}");
    }

    /// <summary>⭐ THE CONTAINMENT THAT MAKES THE TWO RUNNERS RECONCILABLE: the CLI-level guard runs every
    /// program the in-process golden suite asserts. A program that fell out of the guard's list would still be
    /// asserted by NistDifferentialTests, so the two legs would quietly be measuring different corpora — the
    /// shape of drift that let a green guard line stand beside a red golden for a whole battery.</summary>
    [Fact]
    public void GuardNistPopulation_RunsEveryProgramTheGoldenSuiteAsserts()
    {
        var guard = GuardPopulation().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = CorpusManifest.Green().Select(r => r.Name).Where(n => !guard.Contains(n)).Order().ToList();
        Assert.True(missing.Count == 0,
            "NistDifferentialTests asserts programs the guard's NIST_TESTS does not run — the two NIST legs are "
            + $"no longer one population: {string.Join(", ", missing)}");
    }

    /// <summary>And the surplus is only ever <c>pending</c> rows — programs the golden suite has NOT adopted.
    /// So on every program both legs measure, both derive their expectation from the same manifest row: neither
    /// can call a program green that the other calls red without one of them contradicting corpus.tsv.</summary>
    [Fact]
    public void GuardNistPopulation_SurplusIsOnlyPendingPrograms()
    {
        var status = CorpusManifest.Rows.ToDictionary(r => r.Name, r => r.Status, StringComparer.OrdinalIgnoreCase);
        var asserted = CorpusManifest.Green().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var wrong = GuardPopulation()
            .Where(n => !asserted.Contains(n) && status.TryGetValue(n, out string? s) && s != "pending")
            .Order().ToList();
        Assert.True(wrong.Count == 0,
            $"guard-only programs that are not `pending` in corpus.tsv: {string.Join(", ", wrong)}");
    }

    /// <summary>⛔ THE PB750 REGRESSION TEST. Both guards resolved
    /// <c>src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll</c> — the LEGACY byte engine, whose project graph
    /// contains no <c>Cobol.Net.Compiler</c> — and drove the whole NIST leg through it, so every battery's
    /// <c>guard NIST: 353 MATCH</c> was a true statement about the ORACLE and no statement at all about the
    /// shipping compiler. This fact fails the moment either guard grows its own CLI path again instead of
    /// asking <c>scripts/guard-compiler.sh</c>, which asserts the binary's identity against its own
    /// <c>.deps.json</c> before any measurement is taken.</summary>
    [Fact]
    public void BothGuards_ResolveTheCompilerThroughOnePlace()
    {
        foreach (string script in new[] { "guard.sh", "guard-fast.sh", "run-suite.sh" })
        {
            string text = File.ReadAllText(TestRepo.Scripts(script));
            Assert.True(text.Contains("guard-compiler.sh", StringComparison.Ordinal),
                $"scripts/{script} does not resolve its compiler through scripts/guard-compiler.sh (PB750)");
            // A hard-coded PATH, not a mention: guard.sh's run-isolation prose legitimately names both binaries.
            Assert.False(text.Contains("CobolSharp.CLI/bin", StringComparison.OrdinalIgnoreCase),
                $"scripts/{script} hard-codes the LEGACY CLI's bin path again — that is exactly kb/Work/PB750");
        }

        // And the default is COBOL.NET: the legacy path exists ONLY behind the opt-in differential switch.
        string resolver = File.ReadAllText(TestRepo.Scripts("guard-compiler.sh"));
        Assert.Contains("COBOLSHARP_LEGACY_DIFFERENTIAL", resolver, StringComparison.Ordinal);
        Assert.Contains("src/Cobol.Net.Cli", resolver, StringComparison.Ordinal);
    }

    /// <summary>The fold is provably LOSSLESS: green∪divergent equals the committed snapshot of the former
    /// <c>[InlineData]</c> names (<c>corpus-green-baseline.txt</c>). If step 8 or any later edit adds/drops a green
    /// program, this fails until the baseline is deliberately re-pinned.</summary>
    [Fact]
    public void GreenSet_MatchesInlineDataBaseline()
    {
        string baselineFile = TestRepo.Tests("Cobol.Net.Tests.Conformance", "corpus-green-baseline.txt");
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
    /// test. Distinct from the ordinary <c>divergent</c> reason (the LEGACY diverges from an ISO-conforming
    /// golden), which never changes a report's PASS/FAIL column.</summary>
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
