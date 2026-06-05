// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// WS-SPEC conformance tests for Nucleus features that the baselined NIST suite under-tests
/// (docs/SPEC_GAP_INVENTORY.md "## Nucleus"). Every [Fact] asserts output observed from the
/// CLI and validated against ISO/IEC 1989:2023 (specs/ISO_COBOL.md).
/// </summary>
public sealed class NucleusSpecTests : EndToEndTestBase
{
    // §14.9.11 — DISPLAY … WITH NO ADVANCING suppresses the trailing line terminator, so a following DISPLAY
    // continues on the same physical line. (WS-SPEC fix: the phrase parsed but was a no-op; threaded NoAdvancing
    // through BoundDisplayStatement → IrPicDisplay → CilDataEmitter → Console.Write.)
    [Fact]
    public void Display_WithNoAdvancing_SuppressesNewline()
    {
        var (success, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. DISPNADV.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           DISPLAY \"AB\" WITH NO ADVANCING.\n" +
            "           DISPLAY \"CD\".\n" +
            "           STOP RUN.\n");

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABCD", stdout);
    }

    // §14.9.22 (Format 3) — a single INSPECT carrying BOTH a TALLYING and a REPLACING phrase.
    // GR ordering: the TALLYING is evaluated against the original operand before any
    // REPLACING is applied, so the count reflects the original character occurrences while
    // the receiver shows the substitution. SPEC_GAP: combined-format isolation.
    [Fact]
    public void Inspect_CombinedTallyingAndReplacing_CountsThenReplaces()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSPCOMB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            01 WS-COUNT PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA TALLYING WS-COUNT FOR ALL "A"
                    REPLACING ALL "A" BY "X".
                DISPLAY WS-COUNT.
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Six 'A's tallied against the original; every 'A' then replaced by 'X'.
        Assert.Equal("06\r\nXXBBXXCCXX", stdout);
    }

    // §14.9.43 — STRING with multiple sources, each with its own DELIMITED BY clause
    // (literal delimiter, SIZE, and a different literal delimiter), plus WITH POINTER.
    // Left-to-right transfer: "AB-CDE" DELIMITED BY "-" -> "AB"; "WXYZ" DELIMITED BY SIZE
    // -> "WXYZ"; "12X34" DELIMITED BY "X" -> "12". Pointer starts at 1, ends at 1+8 = 9.
    [Fact]
    public void String_MultiSourceMixedDelimiters_ConcatenatesAndAdvancesPointer()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STRMULTI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(6) VALUE "AB-CDE".
            01 WS-B PIC X(4) VALUE "WXYZ".
            01 WS-C PIC X(5) VALUE "12X34".
            01 WS-RESULT PIC X(20) VALUE ALL "*".
            01 WS-PTR PIC 99 VALUE 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STRING WS-A DELIMITED BY "-"
                       WS-B DELIMITED BY SIZE
                       WS-C DELIMITED BY "X"
                    INTO WS-RESULT
                    WITH POINTER WS-PTR.
                DISPLAY WS-RESULT.
                DISPLAY WS-PTR.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // "AB" + "WXYZ" + "12" = "ABWXYZ12"; remaining 12 bytes keep the '*' fill; pointer = 9.
        Assert.Equal("ABWXYZ12************\r\n09", stdout);
    }

    // §14.9.39 — SET external-switch mnemonic TO ON / OFF, observed through its
    // ON STATUS / OFF STATUS condition-names. SPEC_GAP: explicit SET sw TO ON/OFF
    // with both branch outcomes verified on output.
    [Fact]
    public void Set_ExternalSwitch_OnThenOff_DrivesConditionNames()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SETSWTCH.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SWITCH-1 IS SW-1
                    ON STATUS IS SW1-ON
                    OFF STATUS IS SW1-OFF.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET SW-1 TO ON.
                IF SW1-ON
                    DISPLAY "A-ON"
                ELSE
                    DISPLAY "A-OFF".
                SET SW-1 TO OFF.
                IF SW1-OFF
                    DISPLAY "B-OFF"
                ELSE
                    DISPLAY "B-ON".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("A-ON\r\nB-OFF", stdout);
    }

    // §14.9.20 GR 5.a.2 — when the FILLER phrase is NOT specified, elementary items with an
    // explicit/implicit FILLER clause are EXCLUDED as receiving operands of INITIALIZE, while
    // data items are reset by category default (alphanumeric -> spaces, numeric -> zeros).
    // This verifies the FILLER-exclusion rule deterministically.
    [Fact]
    public void Initialize_DefaultExcludesFiller_ResetsDataItems()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INITDFLT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GROUP.
               05 WS-A      PIC X(3) VALUE "AAA".
               05 FILLER    PIC X(2) VALUE "FF".
               05 WS-B      PIC 9(3) VALUE 777.
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE WS-GROUP.
                DISPLAY ">" WS-GROUP "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // WS-A -> 3 spaces; FILLER "FF" untouched; WS-B -> 000.
        Assert.Equal(">   FF000<", stdout);
    }

    // §8.4.2 — reference modification applied to a subscripted OCCURS element, both as a
    // sending operand (extract), as a receiving operand (write-back), and inside a relation
    // condition, with the (start:length) taken from data items. SPEC_GAP: ref-mod of a table
    // element (NC224A covers only scalar ref-mod).
    [Fact]
    public void RefMod_OnSubscriptedTableElement_ExtractWriteBackAndCompare()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMODTB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TBL.
               05 WS-ELEM PIC X(6) OCCURS 3 TIMES.
            01 WS-IDX  PIC 9 VALUE 2.
            01 WS-ST   PIC 9 VALUE 2.
            01 WS-LN   PIC 9 VALUE 3.
            01 WS-OUT  PIC X(3) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEF" TO WS-ELEM (1).
                MOVE "GHIJKL" TO WS-ELEM (2).
                MOVE "MNOPQR" TO WS-ELEM (3).
                MOVE WS-ELEM (WS-IDX) (WS-ST:WS-LN) TO WS-OUT.
                DISPLAY ">" WS-OUT "<".
                MOVE "789" TO WS-ELEM (3) (2:3).
                DISPLAY ">" WS-ELEM (3) "<".
                IF WS-ELEM (2) (1:2) = "GH"
                    DISPLAY "CMP-OK"
                ELSE
                    DISPLAY "CMP-NO".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // WS-ELEM(2)="GHIJKL"; (2:3) -> "HIJ". WS-ELEM(3)="MNOPQR"; (2:3):="789" -> "M789QR".
        // WS-ELEM(2)(1:2)="GH" matches "GH".
        Assert.Equal(">HIJ<\r\n>M789QR<\r\nCMP-OK", stdout);
    }

    // §14.9.1 / §D.31.2 — temporal ACCEPT from DATE, DAY-OF-WEEK, and TIME. The actual
    // values are non-deterministic, so the test asserts only that each receiver is numeric
    // and within the spec-defined range (DATE: mm 01-12, dd 01-31; DAY-OF-WEEK 1-7;
    // TIME: hh 00-23, mm/ss 00-59), emitting a deterministic "OK" flag. SPEC_GAP: no
    // baselined NC test exercises temporal ACCEPT on a passing path (NC214M is non-deterministic).
    [Fact]
    public void Accept_TemporalDateDowTime_ProduceInRangeValues()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCTEMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATE.
               05 WS-YY PIC 99.
               05 WS-MM PIC 99.
               05 WS-DD PIC 99.
            01 WS-DOW  PIC 9.
            01 WS-TIME.
               05 WS-HH PIC 99.
               05 WS-MI PIC 99.
               05 WS-SS PIC 99.
               05 WS-CC PIC 99.
            01 WS-FLAG PIC X(2) VALUE "OK".
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT WS-DATE FROM DATE.
                ACCEPT WS-DOW  FROM DAY-OF-WEEK.
                ACCEPT WS-TIME FROM TIME.
                IF WS-MM < 1 OR WS-MM > 12 MOVE "BD" TO WS-FLAG.
                IF WS-DD < 1 OR WS-DD > 31 MOVE "BD" TO WS-FLAG.
                IF WS-DOW < 1 OR WS-DOW > 7 MOVE "BW" TO WS-FLAG.
                IF WS-HH > 23 MOVE "BH" TO WS-FLAG.
                IF WS-MI > 59 MOVE "BM" TO WS-FLAG.
                IF WS-SS > 59 MOVE "BS" TO WS-FLAG.
                DISPLAY WS-FLAG.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }

    // §14.9.1 — temporal ACCEPT FROM DAY (Julian yyddd). Day-of-year is non-deterministic;
    // assert only that it is numeric and within 1..366.
    [Fact]
    public void Accept_FromDay_ProducesInRangeJulianDay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACCTEMP2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DAY.
               05 WS-DYY  PIC 99.
               05 WS-DDDD PIC 999.
            01 WS-FLAG PIC X(2) VALUE "OK".
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT WS-DAY FROM DAY.
                IF WS-DDDD < 1 OR WS-DDDD > 366 MOVE "BD" TO WS-FLAG.
                DISPLAY WS-FLAG.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }

    // §8.3.3.6 / §12.3.7.2 — SYMBOLIC CHARACTERS clause defines a figurative constant that
    // holds the byte at a given 1-based ordinal position of the program's native collating
    // sequence. Position 66 -> 0-based ordinal 65 -> ASCII 'A'; position 91 -> 'Z'.
    [Fact]
    public void SymbolicCharacters_Figurative_MapsToCollatingPosition()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMCHAR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS SYM-A SYM-Z ARE 66 91.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(2) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SYM-A TO WS-X (1:1).
                MOVE SYM-Z TO WS-X (2:1).
                DISPLAY ">" WS-X "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal(">AZ<", stdout);
    }
}

