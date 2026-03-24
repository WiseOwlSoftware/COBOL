// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler;
using CobolSharp.Compiler.Diagnostics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Validates that the COBOL-aware error strategy produces human-readable diagnostics
/// for common COBOL mistakes, tested against real NIST programs.
/// </summary>
public sealed class CobolErrorStrategyTests
{
    private const string NistProgramsDir = "tests/nist/programs";

    // ── BLANK WHEN ZERO not parsed in data description ──

    [Theory]
    [InlineData("NC134A.cob", "BLANK")]
    public void Detects_UnrecognizedClause(string file, string expectedFragment)
    {
        var diagnostics = CompileWithDiagnostics(file);
        Assert.True(
            HasDiagnosticContaining(diagnostics, expectedFragment),
            $"Expected diagnostic mentioning '{expectedFragment}' in {file}");
    }

    // STATUS in SPECIAL-NAMES (ON STATUS IS / OFF STATUS IS) and
    // PROGRAM in OBJECT-COMPUTER (PROGRAM COLLATING SEQUENCE) are now
    // properly parsed with dedicated grammar rules. Tests for the old
    // reserved-word conflict errors have been removed.

    // ── Error count cap ──

    [Fact]
    public void ErrorCountCap_NC203A_AtMost20()
    {
        var diagnostics = CompileWithDiagnostics("NC203A.cob");
        Assert.True(
            diagnostics.Length <= 20,
            $"Expected at most 20 diagnostics for NC203A, got {diagnostics.Length}");
    }

    // ── Helpers ──

    private static Diagnostic[] CompileWithDiagnostics(string fileName)
    {
        string path = Path.Combine(FindRepoRoot(), NistProgramsDir, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"NIST test program not found: {path}");

        var compilation = new Compilation();
        compilation.NistTestName = Path.GetFileNameWithoutExtension(fileName);
        var result = compilation.Compile(path, Path.Combine(Path.GetTempPath(), "diag_test.dll"));
        return result.Diagnostics.ToArray();
    }

    private static bool HasDiagnosticContaining(Diagnostic[] diagnostics, string fragment)
        => diagnostics.Any(d => DiagnosticNormalization.ContainsNormalized(d.Message, fragment));

    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repository root");
    }
}
