using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class MiscTests : EndToEndTestBase
{
    [Fact]
    public void HelloWorld_PrintsCorrectOutput()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HELLO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "Hello, World!".
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("Hello, World!", stdout);
    }


    [Fact]
    public void DisplayMultipleValues_Concatenates()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MULTI.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "Hello" " " "COBOL".
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("Hello COBOL", stdout);
    }


    [Fact]
    public void CopyStatement_ExpandsCopybook()
    {
        // Create a copybook file
        string copybookPath = Path.Combine(_tempDir, "WS-FIELDS.cpy");
        File.WriteAllText(copybookPath, """
            01 WS-MSG PIC X(20) VALUE "From Copybook".
            """);

        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COPYTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            COPY WS-FIELDS.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-MSG.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("From Copybook", stdout);
    }


    [Fact]
    public void FixedForm_CompilesCorrectly()
    {
        // Fixed-form: cols 1-6 sequence, 7 indicator, 8-72 source
        var (success, stdout, stderr) = CompileAndRun(
            "000100 IDENTIFICATION DIVISION.\r\n" +
            "000200 PROGRAM-ID. FIXTEST.\r\n" +
            "000300 PROCEDURE DIVISION.\r\n" +
            "000310 MAIN-PARA.\r\n" +
            "000400     DISPLAY \"Fixed-form works!\".\r\n" +
            "000500     STOP RUN.\r\n");

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Fixed-form works!", stdout);
    }


    [Fact]
    public void GobackStatement_ExitsProgram()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOBACKTEST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "Before".
                GOBACK.
                DISPLAY "After".
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Before", stdout);
    }


    [Fact]
    public void AcceptFromDate_GetsCurrentDate()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DATETEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATE PIC 9(8).
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT WS-DATE FROM DATE.
                DISPLAY WS-DATE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Should be 8 digits starting with 2026
        Assert.Equal(8, stdout.Length);
        Assert.StartsWith("2026", stdout);
    }


    [Fact]
    public void AcceptFromDate_6Digit()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACC1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 D PIC 9(6).
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT D FROM DATE.
                DISPLAY D.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal(6, stdout.Length);
        Assert.True(stdout.All(char.IsDigit), $"Expected all digits, got: {stdout}");
    }


    [Fact]
    public void AcceptFromTime_8Digit()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACC3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC 9(8).
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT T FROM TIME.
                DISPLAY T.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal(8, stdout.Length);
        Assert.True(stdout.All(char.IsDigit), $"Expected all digits, got: {stdout}");
        int hh = int.Parse(stdout[..2]);
        int mm = int.Parse(stdout[2..4]);
        int ss = int.Parse(stdout[4..6]);
        Assert.InRange(hh, 0, 23);
        Assert.InRange(mm, 0, 59);
        Assert.InRange(ss, 0, 59);
    }


    [Fact]
    public void AcceptFromDay_7Digit()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACC4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 DY PIC 9(7).
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT DY FROM DAY.
                DISPLAY DY.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal(7, stdout.Length);
        Assert.True(stdout.All(char.IsDigit), $"Expected all digits, got: {stdout}");
        int dayOfYear = int.Parse(stdout[4..]);
        Assert.InRange(dayOfYear, 1, 366);
    }


    [Fact]
    public void AcceptFromDayOfWeek_1Digit()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ACC5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 DW PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ACCEPT DW FROM DAY-OF-WEEK.
                DISPLAY DW.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal(1, stdout.Length);
        int dow = int.Parse(stdout);
        Assert.InRange(dow, 1, 7);
    }

    // ── OCCURS + Subscripts ──

}
