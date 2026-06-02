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

        // XXXXX001 / XXXXX002 are data-file ASSIGN targets. They are NOT special-cased to a shared literal
        // here — they flow through the organization-aware mapping below (line 72) like every other
        // XXXXX### data file: a RELATIVE/INDEXED file maps to a shared "TF###" (so producer/consumer run
        // units share one physical file), while a SEQUENTIAL file's target is left as the implementor-name
        // so the Binder qualifies it per program-id for test isolation (DEVLOG 244/255). A blanket
        // `XXXXX001 → "TFIL1"` literal here previously defeated that isolation, so an OPEN-INPUT-on-an-
        // absent-file test (SQ141A/SQ142A) saw a leftover shared file and got status 00 instead of 35.

        // XXXXX058: control card file assignment
        source = source.Replace("XXXXX058", "\"CONTROL\"");

        // XXXXP### / XXXXD### : produce/consume sequential data files passed between CCVS
        // programs. The X-card convention pairs them by number — XXXXP002 (written by SM203A via
        // its COPY'd FILE-CONTROL) and XXXXD002 (read by SM204A) denote the SAME physical file,
        // as do ST104A's XXXXP001 and ST105A's XXXXD001. Map both to one quoted filename keyed by
        // the number so the producer's output is read by the consumer (run in a shared directory),
        // independent of the differing SELECT names. Runs after COPY expansion (see
        // Compilation.Preprocess) so placeholders inside copied library text are mapped too.
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"XXXX[PD](\d+)", "\"TF$1\"");

        // XXXXX### as an ASSIGN TO operand denotes a data file identified by X-card number ### (a test
        // header reads e.g. `X-61 - "LITERAL" IN "ASSIGN TO" CLAUSE FOR ... DATA FILE`). For RELATIVE and
        // INDEXED files the X (permanent) variant is the SAME physical file as the matching
        // XXXXP###/XXXXD### produce/consume variants and is shared across run units (e.g. RL108A creates
        // `XXXXX061`, RL109A/RL110A consume it; RL107A creates `XXXXX022`, RL117A consumes it), so map it
        // to a shared "TF###" literal. SEQUENTIAL files are deliberately left alone: their XXXXX###
        // ASSIGN targets stay program-id-qualified for test isolation (DEVLOG 244 — e.g. SQ130A's
        // XXXXX014/062 absent-file status checks must not collide with another run unit's file). The
        // organization is therefore the discriminator. Anchored to the SELECT entry (SELECT…period) so a
        // RELATIVE/INDEXED entry's operand is mapped while a sequential entry's is not; runs after COPY
        // expansion (copied FILE-CONTROL is in place) and after the specific 001/002/055/058 numbers.
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"SELECT\b[\s\S]*?\.",
            m => System.Text.RegularExpressions.Regex.IsMatch(m.Value, @"\b(RELATIVE|INDEXED)\b")
                ? System.Text.RegularExpressions.Regex.Replace(
                    m.Value, @"(ASSIGN\s+(?:TO\s+)?)XXXXX(\d+)", "$1\"TF$2\"")
                : m.Value);

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
