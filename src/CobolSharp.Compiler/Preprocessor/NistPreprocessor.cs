// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Preprocessor;

/// <summary>
/// Preprocessor for NIST COBOL-85 test suite programs.
/// Replaces XXXXX### site-specific placeholders with CobolSharp-appropriate values.
///
/// NIST placeholder convention (from CCVS85 documentation):
///   XXXXX055 — System printer file name (ASSIGN TO target for PRINT-FILE)
///   XXXXX082 — SOURCE-COMPUTER name
///   XXXXX083 — OBJECT-COMPUTER name
///
/// Additional placeholders (for future test programs):
///   XXXXX056-058 — Additional file assignments
///   XXXXX084     — Implementor-specific values
/// </summary>
public static class NistPreprocessor
{
    /// <summary>
    /// Replace NIST XXXXX placeholders in COBOL source text.
    /// </summary>
    /// <param name="source">Raw or normalized COBOL source text.</param>
    /// <param name="testName">NIST test program name (e.g., "NC101A"). Used to derive
    /// the output file name so each test writes to its own file.</param>
    /// <returns>Source text with placeholders replaced.</returns>
    public static string Process(string source, string testName)
    {
        // XXXXX055: printer assignment → string literal with test name
        // This makes ASSIGN TO resolve to "<testname>.txt" via our implementor-defined mapping
        source = source.Replace("XXXXX055", $"\"{testName}\"");

        // XXXXX082: SOURCE-COMPUTER
        source = source.Replace("XXXXX082", "COBOLSHARP");

        // XXXXX083: OBJECT-COMPUTER
        source = source.Replace("XXXXX083", "DOTNET");

        return source;
    }
}
