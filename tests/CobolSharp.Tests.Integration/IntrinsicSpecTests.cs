// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Spec-conformance tests for intrinsic functions the NIST IF suite under-tests
/// (WS-SPEC workstream; docs/SPEC_GAP_INVENTORY.md "## Intrinsic Functions").
/// Every assertion below was verified against compiled+executed output from the CLI.
/// </summary>
public sealed class IntrinsicSpecTests : EndToEndTestBase
{
    // ── ABS — numeric absolute value (§8.11) — untested by IF suite ──
    [Fact]
    public void Abs_NumericAbsoluteValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFABS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(3).99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-R = FUNCTION ABS(-7.5).
                DISPLAY WS-R.
                COMPUTE WS-R = FUNCTION ABS(3).
                DISPLAY WS-R.
                COMPUTE WS-R = FUNCTION ABS(0).
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("007.50", lines[0]);
        Assert.Equal("003.00", lines[1]);
        Assert.Equal("000.00", lines[2]);
    }

    // ── SIGN — integer -1/0/+1 (§8.11) — untested by IF suite ──
    [Fact]
    public void Sign_ReturnsMinusOneZeroPlusOne()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSIGN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-S PIC -9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-S = FUNCTION SIGN(-4).
                DISPLAY WS-S.
                COMPUTE WS-S = FUNCTION SIGN(0).
                DISPLAY WS-S.
                COMPUTE WS-S = FUNCTION SIGN(9).
                DISPLAY WS-S.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("-1", lines[0]);
        Assert.Equal(" 0", lines[1]);
        Assert.Equal(" 1", lines[2]);
    }

    // ── FRACTION-PART — numeric (§8.11) — untested by IF suite ──
    [Fact]
    public void FractionPart_ReturnsFractionalDigits()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFFRAC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-E PIC -9.99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-E = FUNCTION FRACTION-PART(3.75).
                DISPLAY WS-E.
                COMPUTE WS-E = FUNCTION FRACTION-PART(-3.75).
                DISPLAY WS-E.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal(" 0.75", lines[0]);
        Assert.Equal("-0.75", lines[1]);
    }

    // ── EXP (e^x) and EXP10 (10^x) — numeric (§8.11) — untested by IF suite ──
    [Fact]
    public void ExpAndExp10()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFEXP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(4).99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-R = FUNCTION EXP(0).
                DISPLAY WS-R.
                COMPUTE WS-R = FUNCTION EXP10(2).
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("0001.00", lines[0]);   // e^0 = 1
        Assert.Equal("0100.00", lines[1]);   // 10^2 = 100
    }

    // ── PI — numeric constant (§8.11) — untested by IF suite ──
    [Fact]
    public void Pi_ConstantAndFourArctan()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFPI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9.9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-R = FUNCTION PI.
                DISPLAY WS-R.
                COMPUTE WS-R = 4 * FUNCTION ATAN(1).
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("3.14159", lines[0]);          // FUNCTION PI
        Assert.Equal("3.14159", lines[1]);          // 4*ATAN(1) ~= PI
    }

    // ── E — Euler's number (§8.11) — untested by IF suite ──
    [Fact]
    public void E_EulersNumber()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9.9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-R = FUNCTION E.
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("2.7182", stdout);
    }

    // ── CONCATENATE — variable-arg string concat (§8.11) — untested by IF suite ──
    // NOTE: arguments MUST be comma-separated; the space-separated string-literal form
    // collapses to the first argument only (recorded under needsFix).
    [Fact]
    public void Concatenate_JoinsStringArguments()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFCONCAT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC X(6).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION CONCATENATE("AB", "CD", "EF") TO WS-R.
                DISPLAY WS-R "|".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABCDEF|", stdout);
    }

    // ── SUBSTITUTE — replace all from/to pairs (§8.11) — unknown coverage; verified dispatched ──
    [Fact]
    public void Substitute_ReplacesAllOccurrences()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSUBST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC X(6).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION SUBSTITUTE("ABABAB", "A", "X") TO WS-R.
                DISPLAY WS-R "|".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XBXBXB|", stdout);
    }

    // ── TRIM — leading / trailing / both keyword forms (§8.11, §15) — untested by IF suite ──
    // A 1-char '|' sentinel follows the receiver so the directional difference is visible
    // (DISPLAY otherwise drops trailing spaces).
    [Fact]
    public void Trim_LeadingTrailingBothForms()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTRIM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-IN PIC X(6) VALUE "  AB  ".
            01 WS-GRP.
               05 WS-R   PIC X(6).
               05 FILLER PIC X VALUE "|".
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO WS-R.
                MOVE FUNCTION TRIM(WS-IN) TO WS-R.
                DISPLAY WS-GRP.
                MOVE SPACES TO WS-R.
                MOVE FUNCTION TRIM(WS-IN LEADING) TO WS-R.
                DISPLAY WS-GRP.
                MOVE SPACES TO WS-R.
                MOVE FUNCTION TRIM(WS-IN TRAILING) TO WS-R.
                DISPLAY WS-GRP.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        // TRIM (both): "AB" left-justified in X(6)
        Assert.Equal("AB    |", lines[0]);
        // TRIM LEADING: leading spaces removed -> "AB" + original trailing spaces -> "AB    "
        Assert.Equal("AB    |", lines[1]);
        // TRIM TRAILING: trailing spaces removed, leading 2 spaces preserved -> "  AB"
        Assert.Equal("  AB  |", lines[2]);
    }

    // ── BYTE-LENGTH — integer byte count of an item (§8.11) — untested by IF suite ──
    [Fact]
    public void ByteLength_OfAlphanumericItem()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFBLEN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(5).
            01 WS-N PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-N = FUNCTION BYTE-LENGTH(WS-X).
                DISPLAY WS-N.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("005", stdout);
    }

    // ── TEST-NUMVAL (validation, 0=valid) and NUMVAL-F (float parse) (§8.11) — untested by IF suite ──
    [Fact]
    public void TestNumval_AndNumvalF()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTNV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9.
            01 WS-F PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-N = FUNCTION TEST-NUMVAL("12.3").
                DISPLAY WS-N.
                COMPUTE WS-N = FUNCTION TEST-NUMVAL("XY").
                DISPLAY WS-N.
                COMPUTE WS-F = FUNCTION NUMVAL-F("1.5E2").
                DISPLAY WS-F.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("0", lines[0]);       // "12.3" is a valid NUMVAL string
        Assert.Equal("1", lines[1]);       // "XY" is invalid
        Assert.Equal("0150", lines[2]);    // 1.5E2 = 150
    }

    // ── DATE-TO-YYYYMMDD — windowed 2-digit-year expansion (§8.11) — untested by IF suite ──
    // Deterministic for the current (2000s) century window: yy >= 50 -> 19yy, yy < 50 -> 20yy.
    [Fact]
    public void DateToYyyymmdd_WindowedYearExpansion()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFD2Y.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-D PIC 9(8).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-D = FUNCTION DATE-TO-YYYYMMDD(850101, 50).
                DISPLAY WS-D.
                COMPUTE WS-D = FUNCTION DATE-TO-YYYYMMDD(150101, 50).
                DISPLAY WS-D.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("19850101", lines[0]);   // 85 >= 50 -> previous century
        Assert.Equal("20150101", lines[1]);   // 15 <  50 -> current century
    }

    // ── YEAR-TO-YYYY — windowed 2-digit-year expansion (§8.11) — untested by IF suite ──
    [Fact]
    public void YearToYyyy_WindowedExpansion()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFYR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-Y PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-Y = FUNCTION YEAR-TO-YYYY(85, 50).
                DISPLAY WS-Y.
                COMPUTE WS-Y = FUNCTION YEAR-TO-YYYY(15, 50).
                DISPLAY WS-Y.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("1985", lines[0]);   // 85 >= 50 -> previous century
        Assert.Equal("2015", lines[1]);   // 15 <  50 -> current century
    }

    // ── Integer-class function used directly as an OCCURS subscript (§15.2 type 5) ──
    // FUNCTION INTEGER and FUNCTION MOD return integer-class and select the correct element.
    [Fact]
    public void IntegerFunctionAsSubscript()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSUBI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TBL.
               05 WS-ELT PIC X(3) OCCURS 5 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AAA" TO WS-ELT(1).
                MOVE "BBB" TO WS-ELT(2).
                MOVE "CCC" TO WS-ELT(3).
                MOVE "DDD" TO WS-ELT(4).
                MOVE "EEE" TO WS-ELT(5).
                DISPLAY WS-ELT(FUNCTION INTEGER(2.9)).
                DISPLAY WS-ELT(FUNCTION MOD(7, 3)).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("BBB", lines[0]);   // INTEGER(2.9) = 2 -> element 2
        Assert.Equal("AAA", lines[1]);   // MOD(7,3) = 1 -> element 1
    }

    // ── Nested mixed/variadic function as argument (§8.4.3.2) ──
    // MAX over a numeric arg, an integer-function arg, and another integer-function arg.
    [Fact]
    public void NestedMixedFunctionArguments()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFNEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-R =
                   FUNCTION MAX(FUNCTION INTEGER(4.2), 3,
                                FUNCTION FACTORIAL(3)).
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // MAX(INTEGER(4.2)=4, 3, FACTORIAL(3)=6) = 6
        Assert.Equal("006", stdout);
    }
}
