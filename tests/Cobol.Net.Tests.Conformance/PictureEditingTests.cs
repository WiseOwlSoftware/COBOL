// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The PICTURE clause EDITING phrase (ISO/IEC 1989:2023 §13.18.40.2 Format 1; the new-in-2023 reserved word EDITING,
/// Annex E.3.3 item 19). User-defined picture editing: the simple-insertion (IS) form places a literal at each
/// character-1 position UNCONDITIONALLY (editing rule 3), and the extended sign-control (FOR) form selects the
/// NEGATIVE literal on a negative value / the POSITIVE literal (or spaces, SR12c) otherwise — the sign map derived
/// from Table 9 + Annex D.24 (the extracted Table 8 is sign-INVERTED; DEVLOG). The single-character render (IS form
/// any occurrence; FOR form at a single character-1 occurrence) LANDS; multi-character literals and floating
/// (character-1 repeated ≥2 under a FOR phrase) are a documented P14 render GAP staged loud (COBOLNET0899). The
/// SR8–SR12 shape rules are COBOLNET1591–1596; the below-2023 introduction gate is COBOLNET0900; EDITING stays a
/// legal user word below 2023 (COBOLNET0901 at 2023).
/// </summary>
public sealed class PictureEditingTests
{
    private static string Prog(string entry, string body) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PEDIT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + entry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
        """ + "\n" + body + "\n" + """
            STOP RUN.
        """;

    // ── The single-character render (LANDABLE) ──

    /// <summary>IS (simple insertion) form — character-1 inserts literal-1 at its position unconditionally (ISO
    /// §13.18.40.5 editing rule 3): <c>PIC 99T99 EDITING "T" IS ":"</c>, MOVE 1230 → <c>12:30</c>.</summary>
    [Fact]
    public void SimpleInsertion_Renders()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-EDIT PIC 99T99 EDITING \"T\" IS \":\".",
                 "    MOVE 1230 TO T-EDIT.\n    DISPLAY \"[\" T-EDIT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[12:30]", stdout);
    }

    /// <summary>IS form with a REPEATED character-1 is legal — the simple-insertion form is NOT the sign-control
    /// symbol SR24 scopes (fork2): <c>PIC 9G9G9 EDITING "G" IS ":"</c>, MOVE 123 → <c>1:2:3</c>, sign-independent.</summary>
    [Fact]
    public void RepeatedSimpleInsertion_Renders()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G-EDIT PIC 9G9G9 EDITING \"G\" IS \":\".",
                 "    MOVE 123 TO G-EDIT.\n    DISPLAY \"[\" G-EDIT \"]\".\n"
               + "    MOVE -123 TO G-EDIT.\n    DISPLAY \"[\" G-EDIT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[1:2:3]", stdout);   // sign-independent — both moves render identically
    }

    /// <summary>FOR (extended sign control) form, single character-1 occurrence, single-character literals (Table 9
    /// / D.24): <c>PIC L999.99F EDITING "L" FOR NEGATIVE IS "(" EDITING "F" FOR NEGATIVE IS ")"</c>. The NEGATIVE
    /// literal lands on a negative value; the unspecified POSITIVE side defaults to a space (SR12c).</summary>
    [Fact]
    public void FixedSignControl_Renders()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 P-EDIT PIC L999.99F EDITING \"L\" FOR NEGATIVE IS \"(\"\n"
               + "                       EDITING \"F\" FOR NEGATIVE IS \")\".",
                 "    MOVE -12.34 TO P-EDIT.\n    DISPLAY \"[\" P-EDIT \"]\".\n"
               + "    MOVE 56.78 TO P-EDIT.\n    DISPLAY \"[\" P-EDIT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[(012.34)]", stdout);   // negative: L→'(', F→')'
        Assert.Contains("[ 056.78 ]", stdout);    // positive: L→space, F→space (SR12c)
    }

    // ── The documented P14 render GAP (staged loud) ──

    /// <summary>A MULTI-CHARACTER literal ("DEBIT ") needs the variable-width render model — a documented P14 GAP:
    /// recognized, bound, SR-validated, then staged loud with COBOLNET0899 (never a wrong-width image).</summary>
    [Fact]
    public void MultiCharLiteral_StagedGap0899()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 D-EDIT PIC L999.99 EDITING \"L\" FOR NEGATIVE IS \"DEBIT \".",
                 "    DISPLAY \"X\"."), 2023);
        Assert.False(ok, "a multi-character EDITING literal is a P14 render GAP (COBOLNET0899)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0899");
    }

    /// <summary>A FLOATING character-1 (the same character-1 twice under a FOR phrase) is a floating string — the
    /// P14 render GAP (COBOLNET0899).</summary>
    [Fact]
    public void FloatingCharacter1_StagedGap0899()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 F-EDIT PIC LL999.99 EDITING \"L\" FOR NEGATIVE IS \"(\".",
                 "    DISPLAY \"X\"."), 2023);
        Assert.False(ok, "a floating (repeated) character-1 under FOR is a P14 render GAP (COBOLNET0899)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0899");
    }

    // ── The SR8–SR12 shape rules ──

    /// <summary>SR8 (ISO §13.18.40.3) — character-1 shall be a basic letter other than A B C D E N P R S V X Z or a
    /// CURRENCY-SIGN letter: <c>EDITING "X"</c> is rejected (COBOLNET1591).</summary>
    [Fact]
    public void ReservedLetterChar1_Rejected1591()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC 99X99 EDITING \"X\" IS \":\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1591");
    }

    /// <summary>SR11 — no two EDITING phrases may name the same character-1 (COBOLNET1592).</summary>
    [Fact]
    public void DuplicateChar1_Rejected1592()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC 9G9G9 EDITING \"G\" IS \":\" EDITING \"G\" IS \"-\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1592");
    }

    /// <summary>SR10 — character-1 shall appear at least once in the PICTURE character-string (COBOLNET1593).</summary>
    [Fact]
    public void Char1AbsentFromMask_Rejected1593()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC 999.99 EDITING \"T\" IS \":\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1593");
    }

    /// <summary>SR12a — the NEGATIVE and POSITIVE literals of a FOR phrase shall occupy the same number of
    /// character positions (COBOLNET1595).</summary>
    [Fact]
    public void UnequalForLiteralWidth_Rejected1595()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC L999 EDITING \"L\" FOR NEGATIVE IS \"(\" POSITIVE IS \"++\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1595");
    }

    // ── Edition gating ──

    /// <summary>The COBOL-2023 introduction gate (§13.18.40.2; Annex E.3.3 item 19): a PICTURE EDITING phrase is
    /// rejected below 2023 with COBOLNET0900 (VersionConformancePass ParseArm.VisitPictureClause).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void BelowIntroduction_Rejected0900(int edition)
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 T-EDIT PIC 99T99 EDITING \"T\" IS \":\".", "    DISPLAY \"X\"."), edition);
        Assert.False(ok, $"PICTURE EDITING must be rejected at COBOL-{edition} (introduced 2023)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    /// <summary>EDITING stays a legal user-defined word below 2023 (the cobolWord funnel admission): <c>01 EDITING
    /// PIC 9</c> compiles clean at COBOL-85, and is rejected as a reserved word at 2023 (COBOLNET0901).</summary>
    [Fact]
    public void EditingAsUserWord_LegalBelow2023()
    {
        var (ok85, _) = EditionHarness.Compile(
            Prog("01 EDITING PIC 9 VALUE 1.", "    DISPLAY EDITING."), 85);
        Assert.True(ok85, "a data-name EDITING must compile at COBOL-85 (user word until 2023)");

        var (ok23, diag23) = EditionHarness.Compile(
            Prog("01 EDITING PIC 9 VALUE 1.", "    DISPLAY EDITING."), 2023);
        Assert.False(ok23, "EDITING is a reserved word at COBOL-2023");
        EditionHarness.AssertHasDiagnostic(diag23, "COBOLNET0901");
    }
}
