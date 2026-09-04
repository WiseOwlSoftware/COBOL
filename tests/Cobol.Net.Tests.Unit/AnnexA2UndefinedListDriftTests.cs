// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

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
        PythonInstrument.Run(TestRepo.Scripts("spec", "extract_annex_a2.py"), args);

    /// <summary>The committed list is what the standard's Annex A.2 says today, re-derived.</summary>
    [Fact]
    public void TheUndefinedElementList_StillMatchesTheStandard()
    {
        var r = Run("--check");
        Assert.True(r.ExitCode == 0,
            "tests/version-matrix/annex-a2-undefined.json no longer agrees with specs/ISO_COBOL.md — "
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

/// <summary>
/// ⛔ THE GATE OVER THE DERIVATION REGISTER — <c>scripts/spec/audit_derivations.py</c>, the Python half of
/// §1.1's evidence kind.
/// </summary>
/// <remarks>
/// The C# half is <c>SpecTraceabilityInventoryDriftTests.EveryDerivation_StandsUnderItsOwnArm</c> and its parity
/// fact. This class runs the OTHER engine over the same live inventory, so a rule that stopped holding on one
/// side turns the battery red whichever side it was.
/// </remarks>
public sealed class DerivationRegisterDriftTests
{
    private static ProcessObservation Run(params string[] args) =>
        PythonInstrument.Run(TestRepo.Scripts("spec", "audit_derivations.py"), args);

    /// <summary>Every live derivation still stands under the Python engine, over a population it asserts.</summary>
    [Fact]
    public void EveryLiveDerivation_StandsUnderThePythonEngine()
    {
        var r = Run("--check", "--json");
        string? line = r.Stdout.Split('\n').Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("JSON ", StringComparison.Ordinal));
        Assert.True(line is not null,
            $"audit_derivations.py --check --json emitted no JSON line.\n{r.Stdout}{r.Stderr}");
        using var doc = JsonDocument.Parse(line!["JSON ".Length..]);

        var claimed = doc.RootElement.GetProperty("claimed").EnumerateArray()
            .Select(x => x.GetString()!).ToList();
        Assert.True(claimed.Count > 0,
            "no inventory row claims a derivation, so 'no findings' is a statement about nothing — the "
            + "inventory or the schema is what changed (kb/Work PB386).");

        var findings = doc.RootElement.GetProperty("findings").EnumerateArray()
            .Select(x => x.GetString()!).ToList();
        Assert.True(findings.Count == 0,
            $"{findings.Count} derivation finding(s):\n  " + string.Join("\n  ", findings.Take(20)));
        Assert.Equal(0, r.ExitCode);
    }

    /// <summary>⛔ The audit's own proof that each of its checks can fail.</summary>
    [Fact]
    public void TheDerivationAudit_ProvesEveryCheckCanFail()
    {
        var r = Run("--self-test");
        Assert.Contains("self-test case(s) passed", r.Stdout);
        Assert.DoesNotContain("  FAIL  ", r.Stdout);
        Assert.Equal(0, r.ExitCode);
    }
}
