using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class ArithmeticTests : EndToEndTestBase
{
    [Fact]
    public void AddToField_UpdatesValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ADDTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TOTAL PIC 9(3) VALUE 10.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD 5 TO WS-TOTAL.
                DISPLAY WS-TOTAL.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("015", stdout);
    }


    [Fact]
    public void SubtractFromField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUBTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-BALANCE PIC 9(5) VALUE 100.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT 30 FROM WS-BALANCE.
                DISPLAY WS-BALANCE.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00070", stdout);
    }


    [Fact]
    public void ComputeExpression()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE WS-RESULT = 3 + 4 * 2.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("00011", stdout);
    }


    [Fact]
    public void MultiplyStatement_CorrectResult()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MULTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(5) VALUE 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MULTIPLY 6 BY WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00042", stdout);
    }


    [Fact]
    public void DivideStatement_CorrectResult()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DIVTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9(5) VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DIVIDE 7 INTO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00006", stdout);
    }


    [Fact]
    public void Divide_AllForms()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DIVALL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(5) VALUE 10.
            01 WS-B PIC S9(5) VALUE 30.
            01 WS-C PIC S9(5) VALUE 0.
            01 WS-D PIC S9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DIVIDE WS-A BY 3 GIVING WS-C REMAINDER WS-D.
                DISPLAY WS-C.
                DISPLAY WS-D.
                DIVIDE 2 INTO WS-B.
                DISPLAY WS-B.
                DIVIDE 5 INTO WS-A GIVING WS-C.
                DISPLAY WS-C.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        // 10 / 3 = 3 remainder 1 (trailing overpunch: 3→'C', 1→'A')
        Assert.Equal("0000C", lines[0]);
        Assert.Equal("0000A", lines[1]);
        // 30 / 2 = 15 (5→'E')
        Assert.Equal("0001E", lines[2]);
        // 10 / 5 = 2 (2→'B')
        Assert.Equal("0000B", lines[3]);
    }

    // ═══════════════════════════════════════════
    // Negative literal comparisons
    // ═══════════════════════════════════════════


    [Fact]
    public void SubtractGiving_CorrectResult()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUBGIV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(5) VALUE 30.
            01 WS-B PIC S9(5) VALUE 100.
            01 WS-C PIC S9(5) VALUE 0.
            01 WS-D PIC S9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT WS-A FROM WS-B GIVING WS-C.
                DISPLAY WS-C.
                SUBTRACT 10 20 FROM WS-B GIVING WS-D.
                DISPLAY WS-D.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // 100 - 30 = 70 (overpunch: 0→'{')
        Assert.Equal("0007{", lines[0]);
        // 100 - (10+20) = 70
        Assert.Equal("0007{", lines[1]);
    }

    // ═══════════════════════════════════════════
    // Level-88 condition name tests
    // ═══════════════════════════════════════════


    [Fact]
    public void Add_SubscriptedOperand_Works()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ADDSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ELEM PIC 9 OCCURS 3 TIMES.
            01 RESULT PIC 99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 5 TO ELEM(2).
                MOVE 10 TO RESULT.
                ADD ELEM(2) TO RESULT.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("15", stdout);
    }


    [Fact]
    public void Subtract_SubscriptedOperand_Works()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUBSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ELEM PIC 9 OCCURS 3 TIMES.
            01 RESULT PIC 99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 3 TO ELEM(1).
                MOVE 10 TO RESULT.
                SUBTRACT ELEM(1) FROM RESULT.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("07", stdout);
    }


    [Fact]
    public void Multiply_SubscriptedOperand_Works()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MULSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ELEM PIC 9 OCCURS 3 TIMES.
            01 RESULT PIC 99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 4 TO ELEM(3).
                MOVE 5 TO RESULT.
                MULTIPLY ELEM(3) BY RESULT.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("20", stdout);
    }


    [Fact]
    public void Subtract_FromLiteral_GivingIdentifier()
    {
        // SUBTRACT A FROM 100 GIVING C → C = 100 - 30 = 70
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUBFLIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5) VALUE 30.
            01 WS-C PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT WS-A FROM 100 GIVING WS-C.
                DISPLAY WS-C.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00070", stdout);
    }


    [Fact]
    public void Subtract_FromSubscripted_GivingIdentifier()
    {
        // SUBTRACT A FROM TBL(3) GIVING C — subscript on FROM operand must be preserved
        // This test catches the bug where BoundIdentifierExpression was reconstructed
        // from just the Symbol, dropping subscripts (targets[0].Target.Symbol).
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUBSCRGIVTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                02 NUM PIC 9V9 OCCURS 4.
            01 WS-A PIC 9V9 VALUE 1.1.
            01 WS-R PIC 9V9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1.1 TO NUM(1).
                MOVE 2.2 TO NUM(2).
                MOVE 3.3 TO NUM(3).
                MOVE 4.4 TO NUM(4).
                SUBTRACT WS-A FROM NUM(3) GIVING WS-R.
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 3.3 - 1.1 = 2.2 → PIC 9V9 displays as "22"
        Assert.Equal("22", stdout);
    }


    [Fact]
    public void Add_ToSubscripted_GivingIdentifier()
    {
        // ADD A TO TBL(2) GIVING C — subscript on TO operand
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ADDSUBGIVTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                02 NUM PIC 9(3) OCCURS 4.
            01 WS-A PIC 9(3) VALUE 10.
            01 WS-R PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 100 TO NUM(1).
                MOVE 200 TO NUM(2).
                MOVE 300 TO NUM(3).
                ADD WS-A TO NUM(2) GIVING WS-R.
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 200 + 10 = 210 — but ADD TO GIVING: R = sum of operands, not target += sum
        // Actually ADD A TO B GIVING C → C = A + B = 10 + 200 = 210
        Assert.Equal("210", stdout);
    }


    [Fact]
    public void Multiply_BySubscripted_GivingIdentifier()
    {
        // MULTIPLY A BY TBL(3) GIVING C — subscript on BY operand
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MULSUBGIVTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                02 NUM PIC 9(3) OCCURS 4.
            01 WS-A PIC 9(3) VALUE 5.
            01 WS-R PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 100 TO NUM(3).
                MULTIPLY WS-A BY NUM(3) GIVING WS-R.
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 5 * 100 = 500
        Assert.Equal("00500", stdout);
    }


    [Fact]
    public void Divide_IntoSubscripted_GivingIdentifier()
    {
        // DIVIDE A INTO TBL(4) GIVING C — subscript on INTO operand
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DIVSUBGIVTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                02 NUM PIC 9(3) OCCURS 4.
            01 WS-A PIC 9(3) VALUE 4.
            01 WS-R PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 100 TO NUM(4).
                DIVIDE WS-A INTO NUM(4) GIVING WS-R.
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 100 / 4 = 25
        Assert.Equal("025", stdout);
    }


    [Fact]
    public void Compute_WithSubscriptedOperand()
    {
        // COMPUTE C = TBL(2) + TBL(3) — subscripted expression operands
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMPSUBTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                02 NUM PIC 9(3) OCCURS 4.
            01 WS-R PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 100 TO NUM(2).
                MOVE 200 TO NUM(3).
                COMPUTE WS-R = NUM(2) + NUM(3).
                DISPLAY WS-R.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 100 + 200 = 300
        Assert.Equal("300", stdout);
    }


    [Fact]
    public void Divide_IntoLiteral_GivingIdentifier()
    {
        // DIVIDE 2 INTO 864.36 GIVING C → C = 864.36 / 2 = 432.18
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DIVFLIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DIV PIC 9(3) VALUE 2.
            01 WS-RES PIC 9(5)V99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DIVIDE WS-DIV INTO 864.36 GIVING WS-RES.
                DISPLAY WS-RES.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0043218", stdout);
    }


    [Fact]
    public void Multiply_ByLiteral_GivingIdentifier()
    {
        // MULTIPLY A BY 3 GIVING C → C = 25 * 3 = 75
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MULFLIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5) VALUE 25.
            01 WS-C PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MULTIPLY WS-A BY 3 GIVING WS-C.
                DISPLAY WS-C.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00075", stdout);
    }


    [Fact]
    public void Divide_ByLiteral_GivingIdentifier()
    {
        // DIVIDE 100 BY 4 GIVING C → C = 100 / 4 = 25
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DIVBLIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-C PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DIVIDE 100 BY 4 GIVING WS-C.
                DISPLAY WS-C.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00025", stdout);
    }

}
