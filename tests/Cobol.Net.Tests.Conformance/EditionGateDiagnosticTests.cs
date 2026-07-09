// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Introduction-diagnostic QUALITY facts (VERSION_TEST_MATRIX_DESIGN P2.8): a too-new construct compiled below its
/// edition must name the required edition on the COBOLNET0900 band through the ONE <c>ConstructRegistry.Check</c>
/// funnel (superset parse + bind-time gate → the VersionConformancePass — every construct now gates at BIND). The
/// matrix reject cells assert code PRESENCE per row (expectDiagnostic); these facts assert message QUALITY
/// (edition-naming) for one representative construct of each class, plus the vendor JSON/XML disposition (COBOL0313
/// via <c>CobolErrorStrategy</c>) and the 0860/0861 collision migration. Sources mirror the constructs.json rows.
/// </summary>
public sealed class EditionGateDiagnosticTests
{
    private static IReadOnlyList<string> ErrorsAt(string source, int edition)
        => EditionHarness.CompileFull(source, edition).Errors;

    private static void AssertNames(IReadOnlyList<string> errors, string requiredEdition, string targeting)
    {
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors, $"requires COBOL-{requiredEdition}");
        EditionHarness.AssertHasDiagnostic(errors, $"targeting COBOL-{targeting}");
    }

    /// <summary>Statement-keyword class — ALLOCATE (ISO §14.9.3, 2002) at 85 names both editions.</summary>
    [Fact]
    public void Allocate_At85_Names0900AndBothEditions()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
                ALLOCATE 100 CHARACTERS RETURNING W.
                STOP RUN.
            """, 85), "2002", "85");

    /// <summary>The single 2023-edge grammar gate — DELETE FILE (ISO 2023 §14.9.10 Format 2) at 2014.</summary>
    [Fact]
    public void DeleteFile_At2014_Names0900AndCobol2023()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F1 ASSIGN TO "egd2.dat".
            DATA DIVISION.
            FILE SECTION.
            FD F1.
            01 R1 PIC X(10).
            PROCEDURE DIVISION.
            MAIN.
                DELETE FILE F1.
                STOP RUN.
            """, 2014), "2023", "2014");

    /// <summary>Optional-statement-tail class — GOBACK RETURNING (ISO §14.9.18, 2002) at 85: the enclosing
    /// rule may have popped by report time, so the token-adjacency fallback carries the mapping.</summary>
    [Fact]
    public void GobackReturning_At85_Names0900()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                GOBACK RETURNING W.
            """, 85), "2002", "85");

    /// <summary>Data-description-clause class — BASED (ISO §13.18.5, 2002) at 85.</summary>
    [Fact]
    public void BasedClause_At85_Names0900()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X(4) BASED.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """, 85), "2002", "85");

    /// <summary>SPECIAL-NAMES class — the FOR NATIONAL phrase (ISO §12.3.7, 2002) at 85.</summary>
    [Fact]
    public void AlphabetForNational_At85_Names0900()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD5.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                ALPHABET AB1 IS STANDARD-1 FOR NATIONAL.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """, 85), "2002", "85");

    /// <summary>Compilation-unit class — a CLASS-ID unit (ISO §11.2/§11.3, 2002) at 85: the failure reports
    /// at the unit start, so the CLASS-ID lookahead scan carries the mapping.</summary>
    [Fact]
    public void ClassDefinition_At85_Names0900()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            CLASS-ID. EGD6.
            END CLASS EGD6.
            """, 85), "2002", "85");

    /// <summary>CALL-argument class — BY VALUE (ISO §14.9.4, 2002) at 85.</summary>
    [Fact]
    public void CallByValue_At85_Names0900()
        => AssertNames(ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD7.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC 9(4) VALUE 3.
            PROCEDURE DIVISION.
            MAIN.
                CALL "EGDSUB" USING BY VALUE W.
                STOP RUN.
            """, 85), "2002", "85");

    /// <summary>The 0860 collision migration: READ PREVIOUS (ISO §14.9.30 Format 1, 2002) at 85 now routes
    /// through the registry — 0900, never the WRITE-END-OF-PAGE-colliding COBOLNET0860.</summary>
    [Fact]
    public void ReadPrevious_At85_Registry0900_Not0860()
    {
        var errors = ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD8.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F3 ASSIGN TO "egd8.dat"
                    ORGANIZATION INDEXED ACCESS DYNAMIC RECORD KEY RK3.
            DATA DIVISION.
            FILE SECTION.
            FD F3.
            01 R3.
               05 RK3 PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                READ F3 PREVIOUS RECORD.
                STOP RUN.
            """, 85);
        AssertNames(errors, "2002", "85");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0860");
    }

    /// <summary>The 0861 collision migration: START FIRST (ISO §14.9.41, 2002) at 85 → registry 0900.</summary>
    [Fact]
    public void StartFirst_At85_Registry0900_Not0861()
    {
        var errors = ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD9.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F4 ASSIGN TO "egd9.dat"
                    ORGANIZATION INDEXED ACCESS DYNAMIC RECORD KEY RK4.
            DATA DIVISION.
            FILE SECTION.
            FD F4.
            01 R4.
               05 RK4 PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                START F4 FIRST.
                STOP RUN.
            """, 85);
        AssertNames(errors, "2002", "85");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0861");
    }

    /// <summary>JSON GENERATE is NOT ISO (0 spec hits; owner decision 2, DEVLOG 581). As of rearch P1 (the
    /// non-ISO JSON/XML grammar was hard-deleted) it parse-errors at EVERY edition; the diagnostic is the vendor
    /// hint (COBOL0313 — <c>CobolErrorStrategy</c> keys off the hard-reserved <c>JSON</c>/<c>XML</c> token), never
    /// the 0900 band (no ISO edition has the construct, so "requires COBOL-2014" would be a lie). This test pins
    /// the &lt;2014 leg; the removal's behavioral change (≥2014 was formerly parse-then-runtime-loud) is intended
    /// hardening.</summary>
    [Fact]
    public void JsonGenerate_Below2014_VendorDisposition_Not0900()
    {
        var errors = ErrorsAt("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD10.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC X(40).
            01 G.
               05 A PIC 9(3) VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                JSON GENERATE W FROM G.
                STOP RUN.
            """, 85);
        Assert.NotEmpty(errors);
        EditionHarness.AssertHasDiagnostic(errors, "not an ISO/IEC 1989 construct");
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0900");
    }

    /// <summary>Negative control: the mapping must not fire where the construct IS available — DELETE FILE
    /// at 2023 has its own (non-0900) outcome, and STOP RUN WITH at 2002 compiles clean.</summary>
    [Fact]
    public void StopRunStatus_At2002_CompilesWithout0900()
    {
        var (ok, errors, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD11.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 RC PIC 99 VALUE 3.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN WITH NORMAL STATUS RC.
            """, 2002);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>The dual-obligation window rows' INTRODUCTION leg (the W2 adversarial review's untested-85-leg
    /// finding): EXIT METHOD / EXIT FUNCTION at 85 reject 0900 naming COBOL-2002 (the removal leg at 2023 is
    /// the negative corpus's job — different code, 0902).</summary>
    [Theory]
    [InlineData("METHOD")]
    [InlineData("FUNCTION")]
    public void ExitMethodFunction_At85_Introduction0900(string keyword)
    {
        var errors = ErrorsAt($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EGD12{keyword[0]}.
            PROCEDURE DIVISION.
            MAIN.
                EXIT {keyword}.
                STOP RUN.
            """, 85);
        AssertNames(errors, "2002", "85");
    }
}
