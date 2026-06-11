// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// NIST programs whose original GOLDEN was legacy-tainted — the baselined expected file encoded a LEGACY
/// non-conformance. Each pin asserts the SPEC-derived outcome with its ISO citation. The goldens below were
/// RE-BASELINED to the conforming output (owner-approved, DEVLOG 569 — the legacy guard carries them in its
/// LEGACY_DIVERGENT list), so the programs are ALSO byte-locked in <see cref="NistDifferentialTests"/>;
/// these pins remain as the citation-bearing documentation of WHY each golden diverges from the legacy.
/// </summary>
public sealed class SpecPinnedNistTests
{
    /// <summary>NC236A: the golden marks SCH-TEST-F1-8 / F1-10 "TEST DELETED" — a legacy ARTIFACT: the legacy's
    /// serial SEARCH with <c>VARYING index-of-ANOTHER-table</c> (ISO §14.9.37.4 GR8b) falls through to the CCVS
    /// DE-LETE paragraph instead of executing the scan (verified by running the legacy directly, DEVLOG 533).
    /// Per GR8b the scan uses the searched table's own first index and varies the other index in step — both
    /// tests then PASS: the conforming run executes 010 OF 010 with nothing deleted.</summary>
    [Fact]
    public void NC236A_SearchVaryingOtherTableIndex_ExecutesAllTests()
    {
        string root = RepoRoot();
        string src = Path.Combine(root, "tests", "nist", "programs", "NC236A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC236A.dll"), NistTestName: "NC236A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC236A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc236a.txt"));

            Assert.Contains("PASS  SCH-TEST-F1-8", output);
            Assert.Contains("PASS  SCH-TEST-F1-10", output);
            Assert.Contains("010 OF 010  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>NC235A: the golden marks IDX-TEST-F2-9 / F2-12 "TEST DELETED" — a legacy artifact: its SEARCH ALL
    /// over an OCCURS-DEPENDING table with a CONDITION-NAME WHEN (<c>WHEN LASTA (IDX-1)</c>) fell through to the
    /// CCVS DE-LETE paragraph. Per ISO §14.9.37 Format 2 (a WHEN condition-name over a table element keyed by the
    /// search index) + §13.18.38 GR7 (the table's count IS data-name-1's value) both tests execute and PASS — the
    /// conforming run is 013 OF 013 with nothing deleted.</summary>
    [Fact]
    public void NC235A_SearchAllConditionNameOverOdo_ExecutesAllTests()
    {
        string root = RepoRoot();
        string src = Path.Combine(root, "tests", "nist", "programs", "NC235A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC235A.dll"), NistTestName: "NC235A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("; ", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC235A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc235a.txt"));

            Assert.Contains("PASS  IDX-TEST-F2-9", output);
            Assert.Contains("PASS  IDX-TEST-F2-12", output);
            Assert.Contains("013 OF 013  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "nist"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/nist) not found");
    }
}
