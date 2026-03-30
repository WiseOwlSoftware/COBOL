// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// M001 Stage 4: End-to-end integration tests verifying that COBOL programs
// using expressions in COMPUTE, PERFORM TIMES, PERFORM VARYING, FUNCTION calls,
// reference modification, subscripts, and DIVIDE REMAINDER compile and produce
// correct results through the IrExpression-based pipeline.
using Xunit;

namespace CobolSharp.Tests.Integration;

public class IrExpressionPipelineTests : EndToEndTestBase
{
    // ── COMPUTE with nested arithmetic ──

    [Fact]
    public void Compute_NestedArithmetic_ProducesCorrectResult()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-COMPUTE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(3) VALUE 10.
            01 B PIC 9(3) VALUE 3.
            01 R PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE R = (A + B) * 2 - 1.
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00025", stdout);
    }

    // ── PERFORM N TIMES (literal count) ──

    [Fact]
    public void PerformTimes_LiteralCount()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-PTIMES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CTR PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM COUNT-PARA 5 TIMES.
                DISPLAY CTR.
                STOP RUN.
            COUNT-PARA.
                ADD 1 TO CTR.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("005", stdout);
    }

    // ── PERFORM N TIMES (identifier count) ──

    [Fact]
    public void PerformTimes_IdentifierCount()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-PTIMESID.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC 9 VALUE 3.
            01 CTR PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM COUNT-PARA N TIMES.
                DISPLAY CTR.
                STOP RUN.
            COUNT-PARA.
                ADD 1 TO CTR.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("003", stdout);
    }

    // ── Inline PERFORM N TIMES ──

    [Fact]
    public void InlinePerformTimes()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-IPTIMES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CTR PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM 4 TIMES
                    ADD 1 TO CTR
                END-PERFORM.
                DISPLAY CTR.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("004", stdout);
    }

    // ── PERFORM VARYING with expression step ──

    [Fact]
    public void PerformVarying_SumLoop()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-PVARYING.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9(3) VALUE 0.
            01 TOTAL PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1
                    UNTIL I > 5
                    ADD I TO TOTAL
                END-PERFORM.
                DISPLAY TOTAL.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00015", stdout);
    }

    // ── FUNCTION call (numeric) ──

    [Fact]
    public void Function_MaxOfIdentifiers()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-FUNC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(3) VALUE 42.
            01 B PIC 9(3) VALUE 99.
            01 R PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE R = FUNCTION MAX(A B).
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("099", stdout);
    }

    // ── FUNCTION call in COMPUTE expression ──

    [Fact]
    public void Function_InComputeExpression()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-FEXPR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE R = FUNCTION MOD(17 5) + 10.
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00012", stdout);  // 17 mod 5 = 2, + 10 = 12
    }

    // ── Reference modification ──

    [Fact]
    public void RefMod_SubstringExtraction()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-REFMOD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "ABCDEFGHIJ".
            01 WS-SUB  PIC X(3) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-DATA(4:3) TO WS-SUB.
                DISPLAY WS-SUB.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DEF", stdout);
    }

    // ── Subscript with variable index ──

    [Fact]
    public void Subscript_VariableIndex()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-VSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING IDX FROM 1 BY 1
                    UNTIL IDX > 5
                    MOVE IDX TO ITEM(IDX)
                END-PERFORM.
                DISPLAY ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("3", stdout);
    }

    // ── COMPUTE with subscripted operands ──

    [Fact]
    public void Compute_SubscriptedOperands()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-CSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 NUM PIC 9(3) OCCURS 3 TIMES.
            01 R PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 10 TO NUM(1).
                MOVE 20 TO NUM(2).
                MOVE 30 TO NUM(3).
                COMPUTE R = NUM(1) + NUM(3).
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00040", stdout);
    }

    // ── DIVIDE GIVING REMAINDER ──

    [Fact]
    public void DivideGivingRemainder()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-REM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(3) VALUE 17.
            01 Q PIC 9(3) VALUE 0.
            01 R PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DIVIDE A BY 5 GIVING Q REMAINDER R.
                DISPLAY Q.
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("003\r\n002", stdout);
    }

    // ── MULTIPLY GIVING (synthetic expression lowering) ──

    [Fact]
    public void MultiplyGiving_UsesIrBinaryExpr()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-MULG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(3) VALUE 7.
            01 B PIC 9(3) VALUE 6.
            01 R PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MULTIPLY A BY B GIVING R.
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00042", stdout);
    }

    // ── SUBTRACT GIVING (synthetic expression lowering) ──

    [Fact]
    public void SubtractGiving_UsesIrBinaryExpr()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M001-SUBG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(3) VALUE 100.
            01 B PIC 9(3) VALUE 30.
            01 C PIC 9(3) VALUE 20.
            01 R PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT B C FROM A GIVING R.
                DISPLAY R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00050", stdout);
    }
}
