// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler;
using CobolSharp.Compiler.Diagnostics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Shared harness for diagnostic tests. Compiles COBOL source and inspects diagnostics.
/// </summary>
public abstract class DiagnosticTestBase
{
    /// <summary>
    /// Compile COBOL source text and return all diagnostics.
    /// </summary>
    protected static IReadOnlyList<Diagnostic> GetDiagnostics(string cobolSource)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "cobolsharp_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            string srcPath = Path.Combine(tempDir, "TEST.cbl");
            File.WriteAllText(srcPath, cobolSource);

            var compilation = new Compilation();
            var result = compilation.Compile(srcPath, Path.Combine(tempDir, "TEST.dll"));
            return result.Diagnostics;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    /// <summary>
    /// Assert that at least one diagnostic has the given code.
    /// </summary>
    protected static void AssertHasDiagnostic(IReadOnlyList<Diagnostic> diagnostics, string code)
    {
        Assert.Contains(diagnostics, d => d.Code == code);
    }

    /// <summary>
    /// Assert that no diagnostic has the given code.
    /// </summary>
    protected static void AssertNoDiagnostic(IReadOnlyList<Diagnostic> diagnostics, string code)
    {
        Assert.DoesNotContain(diagnostics, d => d.Code == code);
    }
}
