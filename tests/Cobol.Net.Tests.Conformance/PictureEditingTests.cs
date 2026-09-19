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
    /// §13.18.40.5 editing rule 3): <c>PIC 99T99 EDITING T IS ":"</c>, MOVE 1230 → <c>12:30</c>.</summary>
    [Fact]
    public void SimpleInsertion_Renders()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 T-EDIT PIC 99T99 EDITING T IS \":\".",
                 "    MOVE 1230 TO T-EDIT.\n    DISPLAY \"[\" T-EDIT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[12:30]", stdout);
    }

    /// <summary>IS form with a REPEATED character-1 is legal — the simple-insertion form is NOT the sign-control
    /// symbol SR24 scopes (fork2): <c>PIC 9G9G9 EDITING G IS ":"</c>, MOVE 123 → <c>1:2:3</c>, sign-independent.</summary>
    [Fact]
    public void RepeatedSimpleInsertion_Renders()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G-EDIT PIC 9G9G9 EDITING G IS \":\".",
                 "    MOVE 123 TO G-EDIT.\n    DISPLAY \"[\" G-EDIT \"]\".\n"
               + "    MOVE -123 TO G-EDIT.\n    DISPLAY \"[\" G-EDIT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[1:2:3]", stdout);   // sign-independent — both moves render identically
    }

    /// <summary>FOR (extended sign control) form, single character-1 occurrence, single-character literals (Table 9
    /// / D.24): <c>PIC L999.99F EDITING L FOR NEGATIVE IS "(" EDITING F FOR NEGATIVE IS ")"</c>. The NEGATIVE
    /// literal lands on a negative value; the unspecified POSITIVE side defaults to a space (SR12c).</summary>
    [Fact]
    public void FixedSignControl_Renders()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 P-EDIT PIC L999.99F EDITING L FOR NEGATIVE IS \"(\"\n"
               + "                       EDITING F FOR NEGATIVE IS \")\".",
                 "    MOVE -12.34 TO P-EDIT.\n    DISPLAY \"[\" P-EDIT \"]\".\n"
               + "    MOVE 56.78 TO P-EDIT.\n    DISPLAY \"[\" P-EDIT \"]\"."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[(012.34)]", stdout);   // negative: L→'(', F→')'
        Assert.Contains("[ 056.78 ]", stdout);    // positive: L→space, F→space (SR12c)
    }

    // ── The VARIABLE-WIDTH render — Annex D.24's own examples (kb/Work PB491) ──
    // Both shapes below used to be staged loud with COBOLNET0899 and PINNED THERE BY TWO GREEN TESTS, which
    // reads as a decision rather than a defect ([[green_test_can_hold_a_gap_open]]). They are legal COBOL-2023
    // the standard demonstrates itself, so the tests now measure the IMAGE the standard states.

    /// <summary>A MULTI-CHARACTER literal, straight out of Annex D.24: <c>01 item PIC IS L999.99 EDITING L FOR
    /// NEGATIVE IS "DEBIT "</c> — "The statement 'MOVE -123.45 TO item' would result in 'DEBIT 123.45'" and
    /// "The statement 'MOVE 123.45 TO item' would result in 'bbbbbb123.45'". §13.18.40.4 GR14's 'es' entry gives
    /// the item 12 character positions (6 for literal-2, 6 for "999.99") and SR12c makes the unspecified POSITIVE
    /// side "the space character repeated for the number of characters in" literal-2.</summary>
    [Fact]
    public void MultiCharLiteral_RendersAtTheLiteralsWidth()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 D-EDIT PIC L999.99 EDITING L FOR NEGATIVE IS \"DEBIT \".",
                 "    MOVE -123.45 TO D-EDIT.\n    DISPLAY \"[\" D-EDIT \"]\".\n"
               + "    MOVE 123.45 TO D-EDIT.\n    DISPLAY \"[\" D-EDIT \"]\".\n"
               + "    DISPLAY \"LEN=\" FUNCTION LENGTH(D-EDIT)."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[DEBIT 123.45]", stdout);
        Assert.Contains("[      123.45]", stdout);
        Assert.Contains("LEN=12", stdout);
    }

    /// <summary>A FLOATING character-1 (the same character-1 twice or more under a FOR phrase) is a floating
    /// insertion string — §13.18.40.5 rule 6 lists "the extended editing sign control symbols, if specified" among
    /// the floating insertion symbols. Annex D.24: <c>PIC IS LLLL9.99F</c> with <c>"("</c> and <c>")"</c> renders
    /// -123.45 as 'b(123.45)'. GR14's 'es' sizes it at "one occurrence of literal-2 or literal-3 … plus one
    /// character for each repetition of character-1": 1 + 3 for the L's, 1 for the F, 4 for "9.99" — 9.</summary>
    [Fact]
    public void FloatingCharacter1_RendersAsAFloatingString()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 F-EDIT PIC LLLL9.99F EDITING L FOR NEGATIVE IS \"(\"\n"
               + "                        EDITING F FOR NEGATIVE IS \")\".",
                 "    MOVE -123.45 TO F-EDIT.\n    DISPLAY \"[\" F-EDIT \"]\".\n"
               + "    DISPLAY \"LEN=\" FUNCTION LENGTH(F-EDIT)."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[ (123.45)]", stdout);
        Assert.Contains("LEN=9", stdout);
    }

    /// <summary>The two widenings TOGETHER, and the size Annex D.24 states outright: "'PIC LLLL9,88 EDITING L
    /// FOR NEGATIVE IS "DEBIT "' would result in an item size of 13 characters: 6 for the first 'L', 3 for the
    /// next three, and 4 for the numbers." (The example's '88' is a slip for digit symbols; the SIZE arithmetic
    /// is GR14's 'es' floating entry and is what this measures.)</summary>
    [Fact]
    public void FloatingMultiCharLiteral_HasAnnexD24Size()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 G-EDIT PIC LLLL9.99 EDITING L FOR NEGATIVE IS \"DEBIT \".",
                 "    MOVE -123.45 TO G-EDIT.\n    DISPLAY \"[\" G-EDIT \"]\".\n"
               + "    DISPLAY \"LEN=\" FUNCTION LENGTH(G-EDIT)."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("LEN=13", stdout);
        Assert.Contains("[ DEBIT 123.45]", stdout);
    }

    /// <summary>The IS (simple insertion) form with a multi-character literal-1 on an ALPHANUMERIC-EDITED item:
    /// GR14's 'es' — "If character-1 is a simple insertion symbol … the size of literal-1 is counted in the size
    /// of the item" — makes <c>PIC XXTXX EDITING T IS "::"</c> a SIX-character item rendering "AB::CD". The
    /// under-count this replaces (LENGTH 5) is the lead appended to kb/Work PB491 from the PB492 report.</summary>
    [Fact]
    public void SimpleInsertionMultiCharLiteral_WidensTheItem()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            Prog("01 S-EDIT PIC XXTXX EDITING T IS \"::\".",
                 "    MOVE \"ABCD\" TO S-EDIT.\n    DISPLAY \"[\" S-EDIT \"]\".\n"
               + "    DISPLAY \"LEN=\" FUNCTION LENGTH(S-EDIT)."), 2023);
        Assert.True(ok, detail);
        Assert.Contains("[AB::CD]", stdout);
        Assert.Contains("LEN=6", stdout);
    }

    // ── The SR8–SR12 shape rules ──

    /// <summary>SR8 (ISO §13.18.40.3) — character-1 shall be a basic letter other than A B C D E N P R S V X Z or a
    /// CURRENCY-SIGN letter: <c>EDITING X</c> is rejected (COBOLNET1591).</summary>
    [Fact]
    public void ReservedLetterChar1_Rejected1591()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC 99X99 EDITING X IS \":\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1591");
    }

    /// <summary>SR11 — no two EDITING phrases may name the same character-1 (COBOLNET1592).</summary>
    [Fact]
    public void DuplicateChar1_Rejected1592()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC 9G9G9 EDITING G IS \":\" EDITING G IS \"-\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1592");
    }

    /// <summary>SR10 — character-1 shall appear at least once in the PICTURE character-string (COBOLNET1593).</summary>
    [Fact]
    public void Char1AbsentFromMask_Rejected1593()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC 999.99 EDITING T IS \":\".", "    DISPLAY \"X\"."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1593");
    }

    /// <summary>SR12a — the NEGATIVE and POSITIVE literals of a FOR phrase shall occupy the same number of
    /// character positions (COBOLNET1595).</summary>
    [Fact]
    public void UnequalForLiteralWidth_Rejected1595()
    {
        var (ok, diag) = EditionHarness.Compile(
            Prog("01 W PIC L999 EDITING L FOR NEGATIVE IS \"(\" POSITIVE IS \"++\".", "    DISPLAY \"X\"."), 2023);
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
            Prog("01 T-EDIT PIC 99T99 EDITING T IS \":\".", "    DISPLAY \"X\"."), edition);
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
