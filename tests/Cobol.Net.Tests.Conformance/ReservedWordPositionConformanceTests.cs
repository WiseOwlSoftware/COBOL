// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Per-edition enforcement of the screen/report allowlist-band reserved words through the POSITION-AWARE §8.9
/// funnel (P2.8 W2 — the RW104A adversarial review): a band context-keyword token (COLUMN, COL, SCREEN, BIT,
/// DEFAULT, LENGTH, NATIONAL are the table-backed ones) rejects with COBOLNET0901 (ISO §8.3.2.1 rule 1 /
/// §8.3.2.4.1: "reserved words shall not be used as user-defined words") ONLY when it occupies a provable
/// user-word position — the data entry-name slot (§13.16), a paragraph/section definition (§14.4.2/§14.4.3),
/// the SELECT file-name (§12.4.5.1), a program-name site (§11.4.2). KEYWORD occurrences that the permissive
/// grammar binds into optional entry-name slots — the report-group COLUMN clause (§13.18.14), the RW104A
/// false-reject the former blanket token-type exclusion parked — stay unflagged at every edition. Severity
/// routes through <c>EditionContext.Removed</c>: error strict / warning permissive (the 0901 band contract).
/// </summary>
public sealed class ReservedWordPositionConformanceTests
{
    // ── The RW104A shape: a report-group COLUMN clause is a KEYWORD use, never a user word ─────────────────

    /// <summary>A minimal RW104A-style report program: the report-group <c>COLUMN 27</c> clause keyword
    /// (§13.18.14) parses into the optional report-group entry-name slot under the permissive grammar.</summary>
    private const string ReportColumnProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPCOL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "RWPCOLF".
        DATA DIVISION.
        FILE SECTION.
        FD RPT
            REPORT IS R-1.
        REPORT SECTION.
        RD R-1.
        01 DET TYPE DETAIL.
            03 LINE 1.
                05 COLUMN 27 PIC X(4) VALUE "MARK".
                05 COLUMN NUMBER 70 PIC X(5) VALUE "PAGE ".
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT RPT.
            INITIATE R-1.
            GENERATE DET.
            TERMINATE R-1.
            CLOSE RPT.
            STOP RUN.
        """;

    /// <summary>COLUMN is §8.9-reserved at EVERY edition, yet its report-group CLAUSE use is a keyword use —
    /// the position-aware funnel must not flag it anywhere (the RW104A/CCVS-85 no-false-reject guarantee),
    /// on either severity axis.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ReportGroupColumnClause_KeywordUse_NoReservedWordDiagnostic(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ReportColumnProgram, edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    // ── The data entry-name slot (§13.16) ────────────────────────────────────────────────────────────────────

    /// <summary>A data item NAMED with the band word SCREEN (reserved 2002+ per reserved-words.json).</summary>
    private const string ScreenDataItemProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPSCR.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 SCREEN PIC X(3) VALUE "ABC".
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY SCREEN.
            STOP RUN.
        """;

    /// <summary>SCREEN was NOT reserved in X3.23-1985 — a data item so named is a conforming 85 program
    /// (the continuity leg of the interval).</summary>
    [Fact]
    public void DataItemNamedScreen_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ScreenDataItemProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>2002+ reserves SCREEN (§8.9 via reserved-words.json row SCREEN): the entry-name use is a
    /// provable user-word position, so strict rejects with 0901 naming the word.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void DataItemNamedScreen_2002Plus_Strict_Rejected0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(ScreenDataItemProgram, edition);
        Assert.False(ok, $"--std {edition} strict must reject a data item named with a reserved word");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'SCREEN'");
    }

    /// <summary>The same compile PERMISSIVE keeps the pre-reservation semantics and downgrades 0901 to a
    /// warning (the §10 #1 migration posture — <c>EditionContext.Removed</c> severity contract).</summary>
    [Fact]
    public void DataItemNamedScreen_2002Permissive_WarnsAndCompiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ScreenDataItemProgram, 2002, permissive: true);
        Assert.True(ok, $"--std 2002 --permissive: {string.Join("; ", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>COLUMN has been reserved CONTINUOUSLY since 1985 (reserved-words.json row COLUMN) — naming a
    /// data item COLUMN violates §8.3.2.1 rule 1 even at --std 85, though the same word as a report CLAUSE
    /// keyword is fine (the position distinction in one word).</summary>
    [Fact]
    public void DataItemNamedColumn_Rejected0901_EvenAt85()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPCDN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 COLUMN PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY COLUMN.
                STOP RUN.
            """, 85);
        Assert.False(ok, "--std 85 strict must reject a data item named COLUMN (reserved since 1985)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'COLUMN'");
    }

    // ── Paragraph definitions (§14.4.3) and the SELECT file-name (§12.4.5.1) ────────────────────────────────

    /// <summary>A paragraph NAMED with the band word COL (reserved 2002+).</summary>
    private const string ColParagraphProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPPAR.
        PROCEDURE DIVISION.
        MAIN.
            PERFORM COL.
            STOP RUN.
        COL.
            DISPLAY "C".
        """;

    /// <summary>COL was not reserved in 1985 — the paragraph name is conforming at 85.</summary>
    [Fact]
    public void ParagraphNamedCol_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ColParagraphProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>2002 reserved COL: the paragraph DEFINITION is a provable user-word position — strict rejects,
    /// permissive warns and runs with the pre-reservation semantics.</summary>
    [Fact]
    public void ParagraphNamedCol_2002_StrictRejects_PermissiveWarns()
    {
        var (ok, diags) = EditionHarness.Compile(ColParagraphProgram, 2002);
        Assert.False(ok, "--std 2002 strict must reject a paragraph named COL");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'COL'");

        var (okP, errorsP, warningsP) = EditionHarness.CompileFull(ColParagraphProgram, 2002, permissive: true);
        Assert.True(okP, $"--std 2002 --permissive: {string.Join("; ", errorsP)}");
        EditionHarness.AssertHasDiagnostic(warningsP, "COBOLNET0901");
    }

    /// <summary>A file NAMED with the band word DEFAULT (reserved 2002+) in its SELECT clause.</summary>
    private const string DefaultFileProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPSEL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT DEFAULT ASSIGN TO "RWPSELF" ORGANIZATION IS LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD DEFAULT.
        01 REC PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT DEFAULT.
            MOVE "HELLO" TO REC.
            WRITE REC.
            CLOSE DEFAULT.
            STOP RUN.
        """;

    /// <summary>DEFAULT was not reserved in 1985 — the file-name is conforming at 85.</summary>
    [Fact]
    public void SelectFileNamedDefault_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(DefaultFileProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>2002 reserved DEFAULT (the OPTIONS-paragraph family): the SELECT file-name is a provable
    /// user-word position — strict rejects with 0901.</summary>
    [Fact]
    public void SelectFileNamedDefault_2002_Strict_Rejected0901()
    {
        var (ok, diags) = EditionHarness.Compile(DefaultFileProgram, 2002);
        Assert.False(ok, "--std 2002 strict must reject a file named DEFAULT");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'DEFAULT'");
    }

    /// <summary>The full NIST RW104A program (the review's namesake) still compiles at --std 85 with NO
    /// reserved-word diagnostic — the report COLUMN clause keyword stays a keyword.</summary>
    [Fact]
    public void Rw104a_At85_NoReservedWordDiagnostic()
    {
        var (ok, diags) = EditionHarness.CompileNist("RW104A", 85);
        Assert.True(ok, $"RW104A --std 85: {string.Join("; ", diags)}");
    }

    // ── The W2 adversarial-review coverage additions (DEVLOG 595): the programName and section-name slots
    // end-to-end with a BAND token (the unit facts used IDENTIFIER names, which the funnel checks
    // position-blind anyway). The linkageProcedureParameter arm stays untested by design: its grammar rule
    // (parameterDescription, CobolData.g4:169) is 2002-gated UDF-prototype surface with no parseable
    // plain-program shape today — Phase 4(c) owns its witness.

    /// <summary>A program NAMED with a band word: user-definable at 85, 0901 at 2002+ strict (§8.3.2.1 r1;
    /// SCREEN reserved 2002 — the A.4.2 module word).</summary>
    [Fact]
    public void ProgramNamedScreen_At85Compiles_2002Strict0901()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCREEN.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """;
        var (ok85, errors85, warn85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join("; ", errors85));
        EditionHarness.AssertNoDiagnostic(warn85, "COBOLNET0901");
        var (ok02, diags02) = EditionHarness.Compile(source, 2002);
        Assert.False(ok02, "--std 2002 strict must reject a program named SCREEN");
        EditionHarness.AssertHasDiagnostic(diags02, "COBOLNET0901");
    }

    /// <summary>A SECTION defined with a band word (BIT reserved 2002): 85 clean, 2002 strict 0901.</summary>
    [Fact]
    public void SectionNamedBit_At85Compiles_2002Strict0901()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPSEC1.
            PROCEDURE DIVISION.
            BIT SECTION.
            MAIN.
                STOP RUN.
            """;
        var (ok85, errors85, warn85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join("; ", errors85));
        EditionHarness.AssertNoDiagnostic(warn85, "COBOLNET0901");
        var (ok02, diags02) = EditionHarness.Compile(source, 2002);
        Assert.False(ok02, "--std 2002 strict must reject a section named BIT");
        EditionHarness.AssertHasDiagnostic(diags02, "COBOLNET0901");
    }
}
