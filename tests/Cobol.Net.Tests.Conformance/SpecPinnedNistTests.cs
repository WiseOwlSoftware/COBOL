// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// NIST programs whose GOLDEN is legacy-tainted — the baselined expected file encodes a LEGACY non-conformance,
/// so the spec-correct greenfield output cannot byte-match it while the legacy guard still consumes that golden
/// (it retires at G8 cut-over, when these goldens are re-baselined). Each pin asserts the SPEC-derived outcome
/// with its ISO citation. (The differential corpus stays the net everywhere the legacy is sound.)
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

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "nist"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/nist) not found");
    }
}
