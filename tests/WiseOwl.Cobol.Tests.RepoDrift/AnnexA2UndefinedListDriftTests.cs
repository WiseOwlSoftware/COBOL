// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE GATE THAT HOLDS THE GENERATED ANNEX A.2 LIST EQUAL TO THE STANDARD —
/// <c>scripts/spec/extract_annex_a2.py</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>tests/version-matrix/annex-a2-undefined.json</c> is the mechanical half of §1.1's derivation evidence kind
/// (owner decision <c>kb/Work/PB386</c>, 2026-09-03): an inventory row may close on "A.2 item <c>n</c>" only if
/// item <c>n</c>'s OWN citation in the standard resolves to that row. The resolution is generated rather than
/// parsed twice — see <c>DESIGN-spec-conformance-review.md</c> §8.2 — and a generated artifact that nothing
/// re-derives is a snapshot of what the spec said the day someone ran a script.
/// </para>
/// <para>
/// So the extractor runs every build, following <see cref="AnnexA1RegisterDriftTests"/>'s precedent for a Python
/// gate wired into the Unit project: the per-commit wave-local gate, battery phase 1 and both CI unit jobs at
/// once. A missing interpreter is a LOUD failure and not a skip.
/// </para>
/// </remarks>
public sealed class AnnexA2UndefinedListDriftTests
{
    private static ProcessObservation Run(params string[] args) =>
        PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("spec", "extract_annex_a2.py"), args);

    /// <summary>The committed list is what the standard's Annex A.2 says today, re-derived.</summary>
    [Fact]
    public void TheUndefinedElementList_StillMatchesTheStandard()
    {
        var r = Run("--check");
        Assert.True(r.ExitCode == 0,
            "tests/version-matrix/annex-a2-undefined.json no longer agrees with specs/ISO_COBOL_2023.md — "
            + $"run python scripts/spec/extract_annex_a2.py\n{r.Stdout}{r.Stderr}");
    }

    /// <summary>
    /// The artifact's POPULATION, asserted before any check over it is believed. A file that parsed to zero
    /// items would satisfy every membership test by refusing everything, and a MISSING observation is not a
    /// NEGATIVE one (<c>feedback_verdict_evidence_invariant</c>).
    /// </summary>
    [Fact]
    public void TheUndefinedElementList_IsPopulated_AndResolvesRules()
    {
        string path = TestRepo.VersionMatrix("annex-a2-undefined.json");
        Assert.True(File.Exists(path), $"missing: {path} — run python scripts/spec/extract_annex_a2.py");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.True(items.Count > 60,
            $"Annex A.2 parsed to {items.Count} item(s) — the transcription or the extractor is what changed");
        int resolving = items.Count(i => i.GetProperty("rule-ids").GetArrayLength() > 0);
        Assert.True(resolving > 40,
            $"only {resolving} of {items.Count} A.2 items resolve to a catalog rule — the citation parser or "
            + "the catalog is what changed, and an A.2 arm would then be refused for the wrong reason");
    }

    /// <summary>
    /// ⛔ THE EVIDENCE THAT THE EXTRACTOR'S OWN CHECKS INSPECT ANYTHING. Its <c>--self-test</c> drives the
    /// citation parser against a fabricated spec, including the "General rules 6d and 14.6.13.2" case that made
    /// the first draft invent a phantom General rule 1.
    /// </summary>
    [Fact]
    public void TheExtractor_ProvesEveryCheckCanFail()
    {
        var r = Run("--self-test");
        Assert.Contains("self-test case(s) passed", r.Stdout);
        Assert.DoesNotContain("  FAIL  ", r.Stdout);
        Assert.Equal(0, r.ExitCode);
    }
}
