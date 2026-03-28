// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Integration;

public class IntrinsicFunctionTests : EndToEndTestBase
{
    [Fact]
    public void Function_Sqrt_ReturnsCorrectValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SQRTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION SQRT(144) TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00012", stdout);
    }

    [Fact]
    public void Function_Sqrt_InCompute()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SQCTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5)V99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION SQRT(144).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("0001200", stdout);
    }

    [Fact]
    public void Function_Pi_NoArgs()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PITEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9V9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION PI TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("31415", stdout);
    }

    [Fact]
    public void Function_Length_ReturnsFieldSize()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LENTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(10) VALUE "HELLO".
            01 WS-LEN  PIC 9(3)  VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION LENGTH(WS-NAME) TO WS-LEN.
                DISPLAY WS-LEN.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("010", stdout);
    }

    [Fact]
    public void Function_UpperCase_ConvertsString()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UCTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-INPUT  PIC X(5) VALUE "hello".
            01 WS-RESULT PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION UPPER-CASE(WS-INPUT)
                    TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("HELLO", stdout);
    }

    [Fact]
    public void Function_CurrentDate_Returns21Chars()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CDTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATE PIC X(21) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION CURRENT-DATE TO WS-DATE.
                DISPLAY WS-DATE.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        // CURRENT-DATE returns 21 chars: YYYYMMDDHHMMSSssGMTdiff
        Assert.Equal(21, stdout.Length);
        // First 8 chars should be today's date in YYYYMMDD format
        Assert.StartsWith("2026", stdout);
    }

    // ═══════════════════════════════════════════════════
    // Math functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Function_Abs_NegativeValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABSTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION ABS(-42).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00042", stdout);
    }

    [Fact]
    public void Function_Integer_Floor()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-INPUT  PIC 9(3)V9 VALUE 3.7.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION INTEGER(WS-INPUT).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00003", stdout);
    }

    [Fact]
    public void Function_Mod_FloorBased()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MODTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION MOD(-11, 5).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00004", stdout);
    }

    [Fact]
    public void Function_Factorial_Five()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FACTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION FACTORIAL(5).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00120", stdout);
    }

    [Fact]
    public void Function_Max_ReturnsLargest()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MAXTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION MAX(3, 7, 2).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00007", stdout);
    }

    [Fact]
    public void Function_Min_ReturnsSmallest()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MINTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION MIN(3, 7, 2).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00002", stdout);
    }

    [Fact]
    public void Function_Mean_Average()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MEANTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION MEAN(10, 20, 30).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00020", stdout);
    }

    [Fact]
    public void Function_Range_SpreadOfValues()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RNGTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION RANGE(10, 50, 30).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00040", stdout);
    }

    [Fact]
    public void Function_Rem_Remainder()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REMTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION REM(17, 5).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00002", stdout);
    }

    // ═══════════════════════════════════════════════════
    // String functions (use field arguments)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Function_LowerCase_ConvertsField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LCTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-INPUT  PIC X(5) VALUE "HELLO".
            01 WS-RESULT PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION LOWER-CASE(WS-INPUT)
                    TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("hello", stdout);
    }

    [Fact]
    public void Function_Reverse_ReversesField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REVTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-INPUT  PIC X(5) VALUE "ABCDE".
            01 WS-RESULT PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION REVERSE(WS-INPUT)
                    TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("EDCBA", stdout);
    }

    [Fact]
    public void Function_Concatenate_JoinsFields()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CATTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A      PIC X(2) VALUE "AB".
            01 WS-B      PIC X(2) VALUE "CD".
            01 WS-RESULT PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION CONCATENATE(WS-A, WS-B)
                    TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("ABCD", stdout);
    }

    // ═══════════════════════════════════════════════════
    // Date functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Function_DateOfInteger_KnownDate()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DOITEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(8) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION DATE-OF-INTEGER(1).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("16010101", stdout);
    }

    [Fact]
    public void Function_IntegerOfDate_KnownDate()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IODTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(8) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION INTEGER-OF-DATE(16010101).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00000001", stdout);
    }

    // ═══════════════════════════════════════════════════
    // Other functions
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Function_Ord_ReturnsPosition()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ORDTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CHAR   PIC X(1) VALUE "A".
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION ORD(WS-CHAR).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00065", stdout);
    }

    [Fact]
    public void Function_IntegerPart_Truncates()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-INPUT  PIC 9(3)V9 VALUE 3.7.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION INTEGER-PART(WS-INPUT).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00003", stdout);
    }

    [Fact]
    public void Function_Midrange_Average()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MIDTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT =
                    FUNCTION MIDRANGE(10, 50, 30).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00030", stdout);
    }

    [Fact]
    public void Function_Char_ReturnsCharacter()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CHRTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC X(5) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION CHAR(65) TO WS-RESULT.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("A", stdout);
    }

    // ═══════════════════════════════════════════════════
    // Reserved-word function names (SIGN, SUM, RANDOM)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Function_Sign_Positive()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION SIGN(42).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00001", stdout);
    }

    [Fact]
    public void Function_Sum_Total()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUMTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION SUM(10, 20, 30).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00060", stdout);
    }

    [Fact]
    public void Function_Random_InRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RNDTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9V9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = FUNCTION RANDOM(42).
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        // RANDOM returns 0 <= X < 1; with PIC 9V9(4) the output is 5 digits
        var value = decimal.Parse(stdout.Insert(1, "."));
        Assert.True(value >= 0m && value < 1m,
            $"RANDOM result {value} not in [0, 1)");
    }
}
