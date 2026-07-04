// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// VERSION TEST MATRIX (docs/VERSION_TEST_MATRIX_DESIGN.md; Phase 1). Tests the compiler as N per-ISO-edition
/// compilers: a (construct × target-edition) matrix whose expected outcome is COMPUTED from each construct's
/// edition metadata — compiles iff <c>introducedIn ≤ V</c> and not removed by V (the design's <c>f(case, V)</c>).
///
/// The catalogue is the CANONICAL <c>tests/version-matrix/constructs.json</c> (design §10 #5 — the
/// VERSION_CHANGE_REFERENCE.md tables and the future in-code registry are renderings of that file; extend it,
/// never fork). Compilation goes through <see cref="EditionHarness"/> — the one per-edition compile path.
///
/// Covered today: INTRODUCTION-GATING both ways (INV-2) and CONTINUITY (INV-1, representative NIST rows per suite
/// family — the FULL sweep is <c>scripts/version-continuity-sweep.sh</c>). Not yet: removed-construct gating and
/// behavior variants (INV-3) — those await the EditionValidator (Phase 2); their rows join constructs.json with
/// <c>removedIn</c>/variant metadata as that lands.
/// </summary>
public sealed class VersionMatrixTests
{
    private sealed record Construct(string Id, string Description, int IntroducedIn, int? RemovedIn, string Vcr,
        string Source, string Status);

    private static readonly IReadOnlyList<Construct> Catalogue = LoadCatalogue();

    private static IReadOnlyList<Construct> LoadCatalogue()
    {
        string path = Path.Combine(EditionHarness.RepoRoot(), "tests", "version-matrix", "constructs.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var list = new List<Construct>();
        foreach (var e in doc.RootElement.GetProperty("constructs").EnumerateArray())
            list.Add(new Construct(
                e.GetProperty("id").GetString()!,
                e.GetProperty("description").GetString()!,
                e.GetProperty("introducedIn").GetInt32(),
                e.GetProperty("removedIn").ValueKind == JsonValueKind.Null ? null : e.GetProperty("removedIn").GetInt32(),
                e.GetProperty("vcr").GetString()!,
                e.GetProperty("source").GetString()!,
                e.TryGetProperty("status", out var s) ? s.GetString()! : "active"));
        return list;
    }

    /// <summary>The matrix expected-outcome function f(case, V) (design §2).</summary>
    private static bool ExpectCompiles(Construct c, int edition)
        => edition >= c.IntroducedIn && (c.RemovedIn is null || edition < c.RemovedIn);

    public static IEnumerable<object[]> Matrix()
    {
        // status:"pending" rows are catalogued (their edition metadata is frozen by the registry drift tests)
        // but not yet implemented — their compile assertions activate when the owning roadmap phase lands.
        foreach (var c in Catalogue.Where(c => c.Status == "active"))
            foreach (int v in EditionHarness.Editions)
                yield return [c.Id, v];
    }

    /// <summary>Every pending row must carry its activation contract: an owning-phase note in the description
    /// and edition metadata good enough to freeze — pending is a scheduling state, never a metadata hole.</summary>
    [Fact]
    public void PendingRows_AreCataloguedWithActivationContracts()
    {
        foreach (var c in Catalogue.Where(c => c.Status == "pending"))
        {
            Assert.Contains("PENDING", c.Description, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(c.Vcr), $"{c.Id}: pending row without a vcr citation");
            Assert.False(string.IsNullOrWhiteSpace(c.Source), $"{c.Id}: pending row without a source program");
        }
        Assert.All(Catalogue, c => Assert.True(c.Status is "active" or "pending", $"{c.Id}: bad status '{c.Status}'"));
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Construct_MatchesEditionExpectation(string constructId, int edition)
    {
        var c = Catalogue.First(x => x.Id == constructId);
        bool expectCompiles = ExpectCompiles(c, edition);
        var (ok, diagnostics) = EditionHarness.Compile(c.Source, edition);

        if (expectCompiles)
            Assert.True(ok, $"[{constructId}] expected to COMPILE at COBOL-{edition} (introduced {c.IntroducedIn}"
                + $"{(c.RemovedIn is { } r ? $", removed {r}" : "")}; {c.Vcr}) but failed:\n{string.Join("\n", diagnostics)}");
        else
            Assert.False(ok, $"[{constructId}] expected to be REJECTED at COBOL-{edition} (not valid until "
                + $"{c.IntroducedIn}{(c.RemovedIn is { } r ? $" / removed {r}" : "")}; {c.Vcr}) but it compiled.");
    }

    /// <summary>INV-1 (continuity): a COBOL-85 NIST program that compiles at 85 must still COMPILE at every later
    /// edition unless a VERSION_CHANGE_REFERENCE row documents a removal/reserved-word collision in it (design §3).
    /// Representative rows across the suite FAMILIES (NC nucleus, IF intrinsics, SM source-manipulation, IC
    /// inter-program, SQ sequential-I-O, RL relative, IX indexed, ST sort) — the FULL 85-greens × {2002,2014,2023}
    /// sweep is <c>scripts/version-continuity-sweep.sh</c>, proven clean 2026-06-10 (DEVLOG 531): zero breaks.</summary>
    [Theory]
    [InlineData("NC101A", 2002)]
    [InlineData("NC101A", 2014)]
    [InlineData("NC101A", 2023)]
    [InlineData("NC211A", 2023)]
    [InlineData("NC136A", 2023)]
    [InlineData("NC243A", 2023)]   // 7-dim tables + PERFORM VARYING
    [InlineData("NC116A", 2023)]   // SIGN clause inheritance
    [InlineData("IF101A", 2023)]
    [InlineData("SM101A", 2023)]
    [InlineData("IC101A", 2023)]
    [InlineData("SQ101M", 2023)]
    [InlineData("RL101A", 2023)]
    [InlineData("IX101A", 2023)]
    public void Cobol85Program_StillCompilesAtLaterEdition(string testName, int edition)
    {
        // Continuity is conditional on the program compiling at 85 at all (the greenfield doesn't bind every
        // suite's features yet) — a program not yet compilable at 85 cannot witness an edition BREAK.
        var (ok85, _) = EditionHarness.CompileNist(testName, 85);
        if (!ok85) return;   // not yet in the 85-compiling set; the sweep re-checks as features land

        var (ok, diagnostics) = EditionHarness.CompileNist(testName, edition);
        Assert.True(ok, $"[INV-1 continuity] {testName} (compiles at 85) failed at COBOL-{edition}; if this is a "
            + $"genuine removal/reserved-word collision it must trace to a VERSION_CHANGE_REFERENCE row, else it is "
            + $"a regression:\n{string.Join("\n", diagnostics)}");
    }
    /// <summary>The ST representative is a DOCUMENTED removal, not a continuity witness: every NIST SD writes the
    /// DATA RECORDS clause — an obsolete '85 element DELETED by ISO/IEC 1989:2002 (the 2023 SD format §13.4.6
    /// admits only the record clause; VERSION_CHANGE_REFERENCE Table 7 row 7.1) — so ST101A must compile at 85
    /// and REJECT at 2002+ with exactly the documented diagnostic.</summary>
    [Fact]
    public void St101A_SdDataRecords_DocumentedRemovalAt2002Plus()
    {
        var (ok85, _) = EditionHarness.CompileNist("ST101A", 85);
        Assert.True(ok85, "ST101A must compile at --std 85");
        var (ok, diagnostics) = EditionHarness.CompileNist("ST101A", 2023);
        Assert.False(ok, "ST101A's SD DATA RECORDS clause must be rejected at COBOL-2023");
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET0873"));
    }

}
