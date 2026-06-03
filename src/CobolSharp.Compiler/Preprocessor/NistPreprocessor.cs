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

        // Data-file ASSIGN targets shared across run units by X-card number, mapped (per SELECT…period
        // entry, after COPY expansion) to one "TF###" literal so a producer's output is read by the matching
        // consumer in a shared directory:
        //   • XXXXD### (consume) — ALWAYS shared. It deliberately reads a file another program produced
        //     (XXXXP002 written by SM203A ↔ XXXXD002 read by SM204A; ST104A's XXXXP001 ↔ ST105A's XXXXD001),
        //     including an OPTIONAL consumer that expects the file present (SQ203A's "FILE PRESENT" test).
        //   • XXXXP### (produce) and the RELATIVE/INDEXED XXXXX### (permanent) variant — shared too, but
        //     ONLY for a non-OPTIONAL SELECT (e.g. RL108A creates XXXXX061, RL109A/RL110A consume it).
        // A SEQUENTIAL XXXXX### is left as an implementor-name so the Binder qualifies it per program-id for
        // absent-file isolation (DEVLOG 244 — SQ130A's XXXXX014/062). A SELECT OPTIONAL file tests
        // presence/absence PER PROGRAM, so its own/produce target (XXXXP###, or a RELATIVE/INDEXED XXXXX###)
        // is likewise left implementor-name-qualified — otherwise another run unit's leftover TF### makes an
        // "absent optional" file appear present (IX216A/217A/218A: OPEN INPUT/EXTEND of an absent optional
        // indexed file must give 05 / READ → AT END 10, not 00 from a producer's shared TF###).
        // Region pattern: a SELECT entry runs from the SELECT keyword to its terminating period. Two
        // hazards drive the exact shape of this regex, both rooted in NormalizeToFreeForm rewriting
        // fixed-form indicator-column lines into free-form "*> …" comment lines that survive in this text:
        //   (1) It must NOT stop at the first literal '.', because CCVS routinely interposes "*> …" comment
        //       lines (e.g. SM203A/SM204A's "…DURING EXTRACTION." note) BETWEEN `SELECT TEST-FILE ASSIGN TO`
        //       and the `XXXXD002.` operand. A naive `[\s\S]*?\.` stops at the comment's period, leaving the
        //       operand (which sits AFTER the comments) unmapped → producer and consumer then qualify TF002
        //       per-program and stop sharing a file (SM204A read an empty file). So the alternation consumes
        //       whole "*> …" comment lines (periods and all), terminating only at the entry's REAL period.
        //   (2) But (1) makes the body skip comment periods, so it MUST be anchored to a real-code SELECT —
        //       `(?m)^[ \t]*SELECT` — never a "SELECT" sitting INSIDE a "*> …" comment. The file-I/O suites
        //       comment out an optional scratch-file SELECT (indicator 'P', e.g. SQ130A/141A/142A's INDEXED
        //       RAW-DATA on X-card 62). Matching that commented "SELECT" would let the comment-skipping body
        //       run past the whole comment block into real code (no real period until then), and because the
        //       comment says INDEXED it would wrongly map the following SEQUENTIAL XXXXX001/014 — destroying
        //       the per-program isolation those absent-file status tests depend on.
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?m)^[ \t]*SELECT\b(?:\*>[^\n]*\n|[^.])*\.",
            m =>
            {
                string sel = System.Text.RegularExpressions.Regex.Replace(
                    m.Value, @"XXXXD(\d+)", "\"TF$1\"");
                bool optional = System.Text.RegularExpressions.Regex.IsMatch(sel, @"\bSELECT\s+OPTIONAL\b");
                if (!optional)
                {
                    sel = System.Text.RegularExpressions.Regex.Replace(sel, @"XXXXP(\d+)", "\"TF$1\"");
                    if (System.Text.RegularExpressions.Regex.IsMatch(sel, @"\b(RELATIVE|INDEXED)\b"))
                        sel = System.Text.RegularExpressions.Regex.Replace(
                            sel, @"(ASSIGN\s+(?:TO\s+)?)XXXXX(\d+)", "$1\"TF$2\"");
                }
                return sel;
            });

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
