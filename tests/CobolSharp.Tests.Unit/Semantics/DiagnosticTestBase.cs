// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Shared harness for diagnostic tests. Compiles COBOL source and inspects diagnostics.
/// </summary>
public abstract class DiagnosticTestBase
{
    /// <summary>
    /// Compile COBOL source text in the permissive Default dialect and return all diagnostics.
    /// </summary>
    protected static IReadOnlyList<Diagnostic> GetDiagnostics(string cobolSource)
        => GetDiagnostics(cobolSource, DialectMode.Default);

    /// <summary>
    /// Compile COBOL source text under the given dialect and return all diagnostics. Use
    /// <see cref="DialectMode.StrictCobol85"/> to exercise dialect-gated diagnostics (e.g. CBL3128, CBL0814).
    /// </summary>
    protected static IReadOnlyList<Diagnostic> GetDiagnostics(string cobolSource, DialectMode dialect)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "cobolsharp_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            string srcPath = Path.Combine(tempDir, "TEST.cbl");
            File.WriteAllText(srcPath, cobolSource);

            var compilation = new Compilation { Options = new CompilationOptions { Dialect = dialect } };
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
