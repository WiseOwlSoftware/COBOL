// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// WS-SPEC spec-conformance tests for Data Division features the NIST suite under-tests
/// (docs/SPEC_GAP_INVENTORY.md "## Data Division"). Each [Fact] asserts output observed
/// directly from the compiler/runtime CLI; behaviour confirmed against ISO/IEC 1989:2023
/// (specs/ISO_COBOL.md §13.18 — USAGE, VALUE, PICTURE editing, SYNCHRONIZED).
/// </summary>
public sealed class DataDivisionSpecTests : EndToEndTestBase
{
    // ── USAGE COMP-3 / PACKED-DECIMAL (§13.18.60) ────────────────────────────
    // COMP-3 appears nowhere in the NIST corpus. Verify packed-decimal arithmetic
    // (ADD/SUBTRACT/MULTIPLY) and the edited MOVE for positive, negative, and zero,
    // including sign behaviour through an edited receiver.

    [Fact]
    public void Comp3_PackedDecimal_Arithmetic_AndEditedMove()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP3A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A    PIC S9(7)V99 COMP-3 VALUE 12345.67.
            01 WS-B    PIC S9(7)V99 COMP-3 VALUE 100.50.
            01 WS-C    PIC S9(7)V99 COMP-3 VALUE 0.
            01 WS-EDIT PIC -9(7).99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD WS-B TO WS-A.
                MOVE WS-A TO WS-EDIT.
                DISPLAY WS-EDIT.
                SUBTRACT WS-B FROM WS-C.
                MOVE WS-C TO WS-EDIT.
                DISPLAY WS-EDIT.
                MULTIPLY 2 BY WS-B.
                MOVE WS-B TO WS-EDIT.
                DISPLAY WS-EDIT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        // 12345.67 + 100.50 = 12446.17 ; positive sign -> leading space in -9(7).99
        Assert.Equal(" 0012446.17", lines[0]);
        // 0 - 100.50 = -100.50 ; negative -> leading '-'
        Assert.Equal("-0000100.50", lines[1]);
        // 100.50 * 2 = 201.00
        Assert.Equal(" 0000201.00", lines[2]);
    }

    [Fact]
    public void PackedDecimal_KeywordSynonym_StoresAndEdits()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PACKTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-P    PIC S9(5)V99 PACKED-DECIMAL VALUE -250.75.
            01 WS-EDIT PIC -9(5).99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-P TO WS-EDIT.
                DISPLAY WS-EDIT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("-00250.75", stdout);
    }

    [Fact]
    public void Computational3_KeywordSynonym_StoresAndEdits()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP3B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-P    PIC S9(5)V99 COMPUTATIONAL-3 VALUE -250.75.
            01 WS-EDIT PIC -9(5).99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-P TO WS-EDIT.
                DISPLAY WS-EDIT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("-00250.75", stdout);
    }

    // ── USAGE COMP-1 single-precision (§13.18.60) ────────────────────────────
    // COMP-1 absent from NIST corpus. Verify single-precision storage and the
    // COMP-1 -> COMP-1 -> numeric-edited MOVE chain. (NOTE: COMP-1 *arithmetic*
    // via COMPUTE currently truncates the fraction — recorded under needsFix —
    // so this test exercises storage/MOVE only.)

    [Fact]
    public void Comp1_SinglePrecision_StorageAndMove()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP1B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-F    COMP-1 VALUE 3.14159.
            01 WS-G    COMP-1.
            01 WS-EDIT PIC 9.9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-F TO WS-EDIT.
                DISPLAY WS-EDIT.
                MOVE WS-F TO WS-G.
                MOVE WS-G TO WS-EDIT.
                DISPLAY WS-EDIT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // 3.14159 single-precision, edited to 4 decimals (MOVE truncates, no ROUNDED)
        Assert.Equal("3.1415", lines[0]);
        Assert.Equal("3.1415", lines[1]);
    }

    // ── USAGE COMP-5 native binary (§13.18.60) ───────────────────────────────
    // COMP-5 uses the full native-binary capacity, NOT the PICTURE's decimal range.
    // A PIC 9(4) value of 60000 (> 9999) survives in COMP-5 (16-bit holds 65535)
    // but is truncated to the decimal range in ordinary COMP.

    [Fact]
    public void Comp5_NativeBinary_ExceedsPicDecimalRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP5B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N5   PIC 9(4) COMP-5 VALUE 0.
            01 WS-NC   PIC 9(4) COMP VALUE 0.
            01 WS-EDIT PIC 9(6).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 60000 TO WS-N5.
                MOVE WS-N5 TO WS-EDIT.
                DISPLAY "C5=" WS-EDIT.
                MOVE 60000 TO WS-NC.
                MOVE WS-NC TO WS-EDIT.
                DISPLAY "CMP=" WS-EDIT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // COMP-5 keeps the full native value
        Assert.Equal("C5=060000", lines[0]);
        // ordinary COMP truncates to the PIC 9(4) decimal range (60000 mod 10000 = 0)
        Assert.Equal("CMP=000000", lines[1]);
    }

    // ── VALUE level-88 multiple / THRU literals (§13.18.63) ──────────────────

    [Fact]
    public void Level88_MixedSingleAndThruValues()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LVL88T.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N    PIC 9(2) VALUE 0.
               88 WS-OK VALUES ARE 1, 3, 5 THRU 9.
            01 WS-I    PIC 9(2) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 10
                    MOVE WS-I TO WS-N
                    IF WS-OK
                        DISPLAY WS-I " TRUE"
                    ELSE
                        DISPLAY WS-I " FALSE"
                    END-IF
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(10, lines.Length);
        Assert.Equal("01 TRUE", lines[0]);
        Assert.Equal("02 FALSE", lines[1]);
        Assert.Equal("03 TRUE", lines[2]);
        Assert.Equal("04 FALSE", lines[3]);
        Assert.Equal("05 TRUE", lines[4]);
        Assert.Equal("06 TRUE", lines[5]);
        Assert.Equal("07 TRUE", lines[6]);
        Assert.Equal("08 TRUE", lines[7]);
        Assert.Equal("09 TRUE", lines[8]);
        Assert.Equal("10 FALSE", lines[9]);
    }

    [Fact]
    public void Level88_MultipleAlphanumericLiterals()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LVL88B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CODE PIC X VALUE " ".
               88 VOWEL VALUES "A", "E", "I", "O", "U".
            01 WS-I    PIC 9 VALUE 0.
            01 WS-TBL  PIC X(7) VALUE "ABEIXOU".
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 7
                    MOVE WS-TBL(WS-I:1) TO WS-CODE
                    IF VOWEL
                        DISPLAY WS-CODE "=V"
                    ELSE
                        DISPLAY WS-CODE "=N"
                    END-IF
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(7, lines.Length);
        Assert.Equal("A=V", lines[0]);
        Assert.Equal("B=N", lines[1]);
        Assert.Equal("E=V", lines[2]);
        Assert.Equal("I=V", lines[3]);
        Assert.Equal("X=N", lines[4]);
        Assert.Equal("O=V", lines[5]);
        Assert.Equal("U=V", lines[6]);
    }

    // ── PICTURE numeric-edited combination battery (§13.18.40.5/.6) ──────────
    // Maximal precedence-rule combinations: floating currency + asterisk
    // check-protect + CR; floating minus; floating plus; floating $; trailing
    // minus — each fed positive, negative, and zero senders.

    [Fact]
    public void NumericEdited_CombinationBattery()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PICEDIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM   PIC S9(5)V99 VALUE 0.
            01 WS-E1    PIC $**,**9.99CR.
            01 WS-E2    PIC --,--9.99.
            01 WS-E3    PIC ++,+++.99.
            01 WS-E4    PIC $$,$$9.99.
            01 WS-E5    PIC Z,ZZ9.99-.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234.56 TO WS-NUM.
                MOVE WS-NUM TO WS-E1.
                DISPLAY "P1:" WS-E1.
                MOVE WS-NUM TO WS-E2.
                DISPLAY "P2:" WS-E2.
                MOVE WS-NUM TO WS-E3.
                DISPLAY "P3:" WS-E3.
                MOVE WS-NUM TO WS-E4.
                DISPLAY "P4:" WS-E4.
                MOVE WS-NUM TO WS-E5.
                DISPLAY "P5:" WS-E5.
                MOVE -7.05 TO WS-NUM.
                MOVE WS-NUM TO WS-E1.
                DISPLAY "N1:" WS-E1.
                MOVE WS-NUM TO WS-E2.
                DISPLAY "N2:" WS-E2.
                MOVE WS-NUM TO WS-E3.
                DISPLAY "N3:" WS-E3.
                MOVE WS-NUM TO WS-E4.
                DISPLAY "N4:" WS-E4.
                MOVE WS-NUM TO WS-E5.
                DISPLAY "N5:" WS-E5.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(10, lines.Length);
        // Positive 1234.56
        Assert.Equal("P1:$*1,234.56", lines[0]);  // fixed $, asterisk check-protect, CR suppressed
        Assert.Equal("P2: 1,234.56", lines[1]);   // floating minus -> space for positive
        Assert.Equal("P3:+1,234.56", lines[2]);   // floating plus -> '+'
        Assert.Equal("P4:$1,234.56", lines[3]);   // floating $
        Assert.Equal("P5:1,234.56", lines[4]);    // trailing minus -> space, then trimmed
        // Negative -7.05
        Assert.Equal("N1:$*****7.05CR", lines[5]); // check-protect fills, CR for negative
        Assert.Equal("N2:    -7.05", lines[6]);    // floating minus
        Assert.Equal("N3:    -7.05", lines[7]);    // floating plus shows '-' for negative
        Assert.Equal("N4:    $7.05", lines[8]);    // floating $ (no sign symbol -> magnitude)
        Assert.Equal("N5:    7.05-", lines[9]);    // trailing minus
    }

    [Fact]
    public void NumericEdited_FixedSign_TrailingSign_Db_AndBlankWhenZero()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EDIT9.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM   PIC S9(5)V99 VALUE 0.
            01 WS-B1    PIC ZZZ9.99 BLANK WHEN ZERO.
            01 WS-S1    PIC +9(4).99.
            01 WS-S2    PIC 9(4).99+.
            01 WS-DB    PIC $9(4).99DB.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 42.50 TO WS-NUM.
                MOVE WS-NUM TO WS-B1.
                DISPLAY "B1V[" WS-B1 "]".
                MOVE WS-NUM TO WS-S1.
                DISPLAY "S1P[" WS-S1 "]".
                MOVE -42.50 TO WS-NUM.
                MOVE WS-NUM TO WS-S1.
                DISPLAY "S1N[" WS-S1 "]".
                MOVE WS-NUM TO WS-S2.
                DISPLAY "S2N[" WS-S2 "]".
                MOVE WS-NUM TO WS-DB.
                DISPLAY "DBN[" WS-DB "]".
                MOVE 42.50 TO WS-NUM.
                MOVE WS-NUM TO WS-DB.
                DISPLAY "DBP[" WS-DB "]".
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(6, lines.Length);
        Assert.Equal("B1V[  42.50]", lines[0]);     // BLANK WHEN ZERO non-zero: normal zero-suppressed edit
        Assert.Equal("S1P[+0042.50]", lines[1]);    // fixed leading '+' for positive
        Assert.Equal("S1N[-0042.50]", lines[2]);    // fixed leading '-' for negative
        Assert.Equal("S2N[0042.50-]", lines[3]);    // trailing '-' for negative
        Assert.Equal("DBN[$0042.50DB]", lines[4]);  // DB shown for negative
        Assert.Equal("DBP[$0042.50]", lines[5]);    // DB suppressed (spaces, trimmed) for positive
    }

    // ── SYNCHRONIZED clause (§13.18.55) ──────────────────────────────────────
    // SYNC on a binary item inserts slack bytes to align the item, which is
    // observable as a deterministic increase in the containing group's LENGTH.

    [Fact]
    public void Synchronized_TwoByteComp_AddsSlackByte_AffectsGroupLength()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYNLEN2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GRP.
               05 WS-A PIC X.
               05 WS-B PIC S9(4) COMP SYNC.
               05 WS-C PIC X.
            01 WS-PLAIN.
               05 WS-D PIC X.
               05 WS-E PIC S9(4) COMP.
               05 WS-F PIC X.
            01 WS-LEN PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION LENGTH(WS-GRP) TO WS-LEN.
                DISPLAY "SYNCLEN=" WS-LEN.
                MOVE FUNCTION LENGTH(WS-PLAIN) TO WS-LEN.
                DISPLAY "PLAINLEN=" WS-LEN.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // X(1) + 2-byte COMP (aligned to offset 2, 1 slack byte) + X(1) = 5
        Assert.Equal("SYNCLEN=0005", lines[0]);
        // X(1) + 2-byte COMP (no alignment) + X(1) = 4
        Assert.Equal("PLAINLEN=0004", lines[1]);
    }

    [Fact]
    public void Synchronized_FourByteComp_AlignsGroupLength()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYNLEN3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GRP.
               05 WS-A PIC X.
               05 WS-B PIC S9(8) COMP SYNC.
               05 WS-C PIC X.
            01 WS-PLAIN.
               05 WS-D PIC X.
               05 WS-E PIC S9(8) COMP.
               05 WS-F PIC X.
            01 WS-LEN PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION LENGTH(WS-GRP) TO WS-LEN.
                DISPLAY "SYNCLEN=" WS-LEN.
                MOVE FUNCTION LENGTH(WS-PLAIN) TO WS-LEN.
                DISPLAY "PLAINLEN=" WS-LEN.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // X(1) + 4-byte COMP (aligned to offset 4, 3 slack bytes) + X(1) = 9
        Assert.Equal("SYNCLEN=0009", lines[0]);
        // X(1) + 4-byte COMP (no alignment) + X(1) = 6
        Assert.Equal("PLAINLEN=0006", lines[1]);
    }
}

