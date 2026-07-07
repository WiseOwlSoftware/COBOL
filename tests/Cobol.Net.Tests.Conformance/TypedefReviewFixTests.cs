// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Regression locks for the TYPEDEF (data-model D17) adversarial review (DEVLOG 664, wf_7d3b1492-01a — 7 confirmed
/// defects, all fixed): the compile-time negatives (three previously-unenforced §13.18.57.3 syntax rules) plus the two
/// false-positive fixes (the run-behavior of the nested-strong and cloned-ODO cases is byte-verified by the
/// <c>typedef_nested_strong</c> / <c>typedef_odo</c> corpus goldens). New diagnostics: 1536 (SR7), 1537 (SR2),
/// 1538 (SR5).
/// </summary>
public sealed class TypedefReviewFixTests
{
    /// <summary>§13.18.57.3 SR7 (fix #1) — a level-77 subject requires an ELEMENTARY type; a WEAK GROUP type on a 77
    /// subject was silently accepted (only the STRONG case was caught by SR6). Now COBOLNET1536, strength-invariant.</summary>
    [Fact]
    public void WeakGroupTypeOnLevel77_Rejected1536()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RF77.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FEAT TYPEDEF.
               05 KIND PIC X(4).
               05 CNT  PIC 9(3).
            77 F1 TYPE FEAT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a level-77 subject referencing a GROUP type must be rejected (ISO §13.18.57.3 SR7)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1536");
    }

    /// <summary>§13.18.57.3 SR2 (fix #2) — a TYPE entry followed immediately by a SUBORDINATE entry silently merged
    /// members (and leaked a raw Roslyn CS1061 for an elementary type). Now COBOLNET1537.</summary>
    [Fact]
    public void SubordinateAfterTypeEntry_Rejected1537()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RFSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FEAT TYPEDEF.
               05 KIND PIC X(4).
               05 CNT  PIC 9(3).
            01 F1 TYPE FEAT.
               05 EXTRA PIC X(2).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a subordinate entry immediately after a TYPE entry must be rejected (ISO §13.18.57.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1537");
    }

    /// <summary>§13.18.57.3 SR2 (fix #2) — a TYPE entry followed immediately by a LEVEL-88 entry (the 88 belongs in the
    /// type declaration). Now COBOLNET1537.</summary>
    [Fact]
    public void Level88AfterTypeEntry_Rejected1537()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RF88.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T1 TYPEDEF PIC 9(3).
            01 V1 TYPE T1.
               88 IS-ZERO VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a level-88 immediately after a TYPE entry must be rejected (ISO §13.18.57.3 SR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1537");
    }

    /// <summary>§13.18.57.3 SR5 (fix #3) — a group SUPERORDINATE to a TYPE subject carrying a USAGE (or SIGN) clause
    /// silently overrode the type's representation. Now COBOLNET1538.</summary>
    [Fact]
    public void UsageSuperordinateToTypeSubject_Rejected1538()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RFSR5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NUM-T TYPEDEF PIC 9(4).
            01 G USAGE COMP.
               05 X TYPE NUM-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234 TO X.
                DISPLAY X.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a USAGE clause on a group superordinate to a TYPE subject must be rejected (ISO §13.18.57.3 SR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1538");
    }

    /// <summary>Fixes #5 + #6 (false positives) — a STRONG type nested inside a STRONG type is legal (§13.18.57.3 SR6
    /// second arm), and a nested strong subgroup is the SAME type as a standalone item of that type (§8.5.3, by the
    /// NEAREST TYPE anchor). Both a same-type MOVE and a same-type compare must compile CLEAN (no false 1532/1533).
    /// (The <c>typedef_nested_strong</c> golden byte-verifies the run output.)</summary>
    [Fact]
    public void NestedStrongTypeSameTypeOps_CompileClean()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RFNEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 INNER-T TYPEDEF STRONG.
               05 IA PIC 9(4).
            01 OUTER-T TYPEDEF STRONG.
               05 SUB TYPE INNER-T.
               05 OC PIC 9(4).
            01 R1 TYPE OUTER-T.
            01 S1 TYPE INNER-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO IA OF SUB OF R1.
                MOVE SUB OF R1 TO S1.
                IF SUB OF R1 = S1
                    DISPLAY "EQ"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.True(ok, $"nested strong types + same-type ops (by nearest anchor) must compile clean: "
            + string.Join("; ", diag));
    }
}
