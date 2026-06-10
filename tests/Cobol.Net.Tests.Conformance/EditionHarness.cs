// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// THE per-edition compile/diagnostic harness (VERSION_TEST_MATRIX_DESIGN.md Phase 1): compile a source text or a
/// NIST program AS a specific ISO edition (the four-compilers-in-one mission — <c>--std 85|2002|2014|2023</c>) and
/// inspect the outcome. Every edition-targeted test goes through here so the matrix, the continuity sweep, and the
/// (future) negative corpus share ONE compile path and ONE diagnostic-assertion idiom.
/// </summary>
public static class EditionHarness
{
    /// <summary>The supported ISO editions, in order.</summary>
    public static readonly int[] Editions = [85, 2002, 2014, 2023];

    /// <summary>Compile <paramref name="source"/> targeting <paramref name="edition"/>; returns success and the
    /// diagnostics (empty on success).</summary>
    public static (bool Ok, IReadOnlyList<string> Diagnostics) Compile(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Ed_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"), DialectLevel: edition));
            return (r.Success, r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>Compile a NIST CCVS program (X-card preprocessing applied) targeting <paramref name="edition"/>.</summary>
    public static (bool Ok, IReadOnlyList<string> Diagnostics) CompileNist(string testName, int edition)
    {
        string src = Path.Combine(RepoRoot(), "tests", "nist", "programs", testName + ".cob");
        Assert.True(File.Exists(src), $"NIST source not found: {src}");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Ed_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, testName + ".dll"), NistTestName: testName, DialectLevel: edition));
            return (r.Success, r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>The diagnostics of compiling <paramref name="source"/> at <paramref name="edition"/> (empty when it
    /// compiles clean).</summary>
    public static IReadOnlyList<string> GetDiagnostics(string source, int edition) => Compile(source, edition).Diagnostics;

    /// <summary>Assert some diagnostic contains <paramref name="expectedSubstring"/> (case-insensitive). The negative
    /// corpus asserts the QUALITY of a rejection this way — e.g. that a too-new construct's diagnostic names the
    /// required edition — per the matrix's reject cells. (Until the EditionValidator lands, grammar-gate rejections
    /// are generic parse errors; rows assert content only once their diagnostic is implemented.)</summary>
    public static void AssertHasDiagnostic(IEnumerable<string> diagnostics, string expectedSubstring)
    {
        var all = diagnostics.ToList();
        Assert.True(all.Any(d => d.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase)),
            $"expected a diagnostic containing '{expectedSubstring}'; got:\n{string.Join("\n", all.DefaultIfEmpty("(none)"))}");
    }

    /// <summary>Walk up from the test assembly to the repository root.</summary>
    public static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "nist"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/nist) not found");
    }
}
