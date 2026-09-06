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
}
