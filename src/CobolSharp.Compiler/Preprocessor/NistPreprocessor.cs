// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Preprocessor;

/// <summary>
/// Preprocessor for NIST COBOL-85 test suite programs.
/// Replaces XXXXX### site-specific placeholders with CobolSharp-appropriate values.
///
/// NIST X-card convention (from CCVS85 executive routine):
///   XXXXX001     — Sequential file ASSIGN target (POPULATION-FILE)
///   XXXXX002     — Sequential file ASSIGN target (SOURCE-COBOL-PROGRAMS)
///   XXXXX051     — SPECIAL-NAMES implementor switch name 1
///   XXXXX052     — SPECIAL-NAMES implementor switch name 2
///   XXXXX055     — System printer file name (ASSIGN TO target for PRINT-FILE)
///   XXXXX056     — SPECIAL-NAMES display output device implementor-name
///   XXXXX057     — SPECIAL-NAMES accept input device implementor-name
///   XXXXX058     — Control card file ASSIGN target
///   XXXXX068     — OBJECT-COMPUTER MEMORY SIZE value (obsolete clause)
///   XXXXX081     — Non-COBOL characters string value
///   XXXXX082     — SOURCE-COMPUTER name
///   XXXXX083     — OBJECT-COMPUTER name
///   XXXXX084     — Implementor-specific label clause value (LABEL RECORDS)
///   XXXXX090     — CLASS definition: single character value ("A")
///   XXXXX091     — CLASS definition: character range end value ("D")
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
        // ── File assignments ──

        // XXXXX055: printer assignment → string literal with test name
        source = source.Replace("XXXXX055", $"\"{testName}\"");

        // XXXXX001: sequential file assignment (used by NC401M, SQ, IX, ST, RL tests)
        source = source.Replace("XXXXX001", "\"TFIL1\"");

        // XXXXX002: second sequential file assignment
        source = source.Replace("XXXXX002", "\"TFIL2\"");

        // XXXXX058: control card file assignment
        source = source.Replace("XXXXX058", "\"CONTROL\"");

        // ── SPECIAL-NAMES ──

        // XXXXX051: implementor switch name 1 (SPECIAL-NAMES ... IS switch-name)
        source = source.Replace("XXXXX051", "SWITCH-1");

        // XXXXX052: implementor switch name 2
        source = source.Replace("XXXXX052", "SWITCH-2");

        // XXXXX056: display output device implementor-name
        source = source.Replace("XXXXX056", "CONSOLE");

        // XXXXX057: accept input device implementor-name
        source = source.Replace("XXXXX057", "CONSOLE");

        // ── CONFIGURATION ──

        // XXXXX082: SOURCE-COMPUTER
        source = source.Replace("XXXXX082", "COBOLSHARP");

        // XXXXX083: OBJECT-COMPUTER
        source = source.Replace("XXXXX083", "DOTNET");

        // XXXXX068: MEMORY SIZE value (obsolete COBOL-74 clause, semantically inert)
        source = source.Replace("XXXXX068", "65535");

        // ── Data/FD clauses ──

        // XXXXX084: implementor-specific label clause value (LABEL RECORDS)
        source = source.Replace("XXXXX084", "STANDARD");

        // ── Literal values ──

        // XXXXX081: non-COBOL characters value (implementor-defined)
        source = source.Replace("XXXXX081", "\"!@#$%^&*\"");

        // XXXXX090: CLASS definition character value (ordinal "A")
        source = source.Replace("XXXXX090", "\"A\"");

        // XXXXX091: CLASS definition character range end value (ordinal "D")
        source = source.Replace("XXXXX091", "\"D\"");

        return source;
    }
}
