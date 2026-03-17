using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class EndToEndTests : IDisposable
{
    private readonly string _tempDir;

    public EndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CobolSharp_Tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private (bool success, string stdout, string stderr) CompileAndRun(string cobolSource)
    {
        // Write source to temp file
        string sourcePath = Path.Combine(_tempDir, "test.cob");
        string outputPath = Path.Combine(_tempDir, "test.dll");
        File.WriteAllText(sourcePath, cobolSource);

        // Compile
        var compilation = new Compilation();
        var result = compilation.Compile(sourcePath, outputPath);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            return (false, "", $"Compilation failed:\n{errors}");
        }

        // Run the compiled assembly
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = outputPath,
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(10000);

        return (process.ExitCode == 0, stdout.TrimEnd(), stderr.TrimEnd());
    }

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
    public void MoveAndDisplay_NumericField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOVETEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM PIC 9(3) VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-NUM.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("042", stdout);
    }

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
    public void IfElse_ThenBranch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-X > 3
                    DISPLAY "BIG"
                ELSE
                    DISPLAY "SMALL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("BIG", stdout);
    }

    [Fact]
    public void PerformParagraph()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PERFTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-COUNT PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM ADD-ONE.
                PERFORM ADD-ONE.
                PERFORM ADD-ONE.
                DISPLAY WS-COUNT.
                STOP RUN.
            ADD-ONE.
                ADD 1 TO WS-COUNT.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("003", stdout);
    }

    [Fact]
    public void PerformThru_ExecutesParagraphRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRUTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM STEP-A THRU STEP-C.
                DISPLAY WS-RESULT.
                STOP RUN.
            STEP-A.
                MOVE "ABC" TO WS-RESULT.
            STEP-B.
                DISPLAY "In B".
            STEP-C.
                DISPLAY "In C".
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("In B", lines[0]);
        Assert.Equal("In C", lines[1]);
        Assert.Equal("ABC", lines[2]);
    }

    [Fact]
    public void PerformThru_StopsAtTarget()
    {
        // C must NOT execute
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRSTOP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU B.
                DISPLAY R.
                STOP RUN.
            A.
                ADD 1 TO R.
            B.
                ADD 2 TO R.
            C.
                ADD 4 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("03", stdout);
    }

    [Fact]
    public void PerformThru_NestedThru()
    {
        // Two separate PERFORM THRU ranges
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRNEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU B.
                PERFORM C THRU D.
                DISPLAY R.
                STOP RUN.
            A. ADD 1 TO R.
            B. ADD 2 TO R.
            C. ADD 3 TO R.
            D. ADD 4 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("10", stdout);
    }

    [Fact]
    public void PerformThru_MultipleSentences()
    {
        // Paragraph A has two sentences
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRSENT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU B.
                DISPLAY R.
                STOP RUN.
            A.
                ADD 1 TO R.
                ADD 1 TO R.
            B.
                ADD 2 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("04", stdout);
    }

    [Fact]
    public void PerformThru_SingleParagraph()
    {
        // PERFORM A THRU A is the same as PERFORM A
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. THRSING.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM A THRU A.
                DISPLAY R.
                STOP RUN.
            A.
                ADD 5 TO R.
            B.
                ADD 9 TO R.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("05", stdout);
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
    public void MoveStringToField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MOVESTR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(10) VALUE "World".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "Hello " WS-NAME.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("Hello World", stdout);
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
    public void EvaluateStatement_SelectsCorrectBranch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EVALTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CODE PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE WS-CODE
                    WHEN 1
                        DISPLAY "One"
                    WHEN 2
                        DISPLAY "Two"
                    WHEN OTHER
                        DISPLAY "Other"
                END-EVALUATE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Two", stdout);
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
    public void SetStatement_SetsValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SETTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-IDX PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET WS-IDX TO 42.
                DISPLAY WS-IDX.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("042", stdout);
    }

    [Fact]
    public void InitializeStatement_ResetsFields()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INITTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NUM PIC 9(3) VALUE 123.
            01 WS-STR PIC X(5) VALUE "Hello".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE WS-NUM.
                INITIALIZE WS-STR.
                DISPLAY WS-NUM.
                DISPLAY ">" WS-STR "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("000", lines[0]);
        // After INITIALIZE, alphanumeric is spaces. DISPLAY trims trailing spaces.
        Assert.Equal("><", lines[1]);
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
    public void InspectReplacing_ReplacesCharacters()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING ALL "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXBBXXCCXX", stdout);
    }

    [Fact(Skip = "CALL statement not yet lowered to CIL")]
    public void CallStatement_EmitsDiagnostic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CALLTEST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "SUBPROG".
                DISPLAY "After call".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("After call", stdout);
        Assert.Contains("CALL:", stderr); // program not found diagnostic
    }

    // (FileIO_WriteAndReadBack moved to end of file with proper implementation)

    // ═══════════════════════════════════════════
    // EVALUATE tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Evaluate_SingleSubjectWithRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 3.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A
                    WHEN 1
                        MOVE 1 TO RESULT
                    WHEN 2 THRU 4
                        MOVE 2 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("2", stdout);
    }

    [Fact]
    public void Evaluate_MultiSubjectAlsoExactMatch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 3.
            01 B PIC 9 VALUE 5.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 3 ALSO 5
                        MOVE 1 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1", stdout);
    }

    [Fact]
    public void Evaluate_MultiSubjectAlsoWithRanges()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 4.
            01 B PIC 9 VALUE 7.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 1 ALSO 5 THRU 6
                        MOVE 1 TO RESULT
                    WHEN 4 THRU 6 ALSO 7
                        MOVE 2 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("2", stdout);
    }

    [Fact]
    public void Evaluate_PartialMatchMustFail()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 2.
            01 B PIC 9 VALUE 9.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 2 ALSO 5
                        MOVE 1 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("9", stdout);
    }

    [Fact]
    public void Evaluate_MismatchedAlsoArity_FallsToOther()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ET5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 1.
            01 B PIC 9 VALUE 2.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE A ALSO B
                    WHEN 1
                        MOVE 1 TO RESULT
                    WHEN OTHER
                        MOVE 9 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("9", stdout);
    }

    [Fact]
    public void EvaluateTrue_ConditionMatching()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EVTRUE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(3) VALUE 15.
            01 WS-B PIC 9(3) VALUE 10.
            01 RESULT PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE TRUE
                    WHEN WS-A > WS-B
                        MOVE 1 TO RESULT
                    WHEN WS-A = WS-B
                        MOVE 2 TO RESULT
                    WHEN OTHER
                        MOVE 3 TO RESULT
                END-EVALUATE.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1", stdout);
    }

    // ═══════════════════════════════════════════
    // PERFORM VARYING / AFTER tests
    // ═══════════════════════════════════════════

    [Fact]
    public void PerformVarying_SumOneToFive()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PV1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-I PIC 9(3) VALUE 0.
            01 WS-SUM PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM ADD-PARA VARYING WS-I FROM 1 BY 1
                    UNTIL WS-I > 5.
                DISPLAY WS-SUM.
                STOP RUN.
            ADD-PARA.
                ADD WS-I TO WS-SUM.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00015", stdout);
    }

    [Fact]
    public void PerformUntil_DecrementToZero()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PU1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9(3) VALUE 10.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM DEC-PARA UNTIL WS-N = 0.
                DISPLAY "Done".
                STOP RUN.
            DEC-PARA.
                SUBTRACT 1 FROM WS-N.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Done", stdout);
    }

    [Fact]
    public void PerformVaryingInline_SumOneToFive()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PVI1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-I PIC 9(3) VALUE 0.
            01 WS-SUM PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING WS-I FROM 1 BY 1
                    UNTIL WS-I > 5
                    ADD WS-I TO WS-SUM
                END-PERFORM.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00015", stdout);
    }

    [Fact]
    public void PerformVaryingAfter_InnerUntilTrueImmediately()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    AFTER J FROM 1 BY 1 UNTIL J > 0
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0000", stdout);
    }

    [Fact]
    public void PerformVaryingAfter_Classic2D()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                    AFTER J FROM 1 BY 1 UNTIL J > 2
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0006", stdout);
    }

    [Fact]
    public void PerformVaryingAfter_OuterDependsOnInner()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL WS-COUNT > 3
                    AFTER J FROM 1 BY 1 UNTIL J > 2
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0004", stdout);
    }

    [Fact]
    public void PerformVaryingAfter_ThreeLevel()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PA4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 K PIC 9 VALUE 0.
            01 WS-COUNT PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    AFTER J FROM 1 BY 1 UNTIL J > 3
                    AFTER K FROM 1 BY 1 UNTIL K > 2
                    ADD 1 TO WS-COUNT
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0012", stdout);
    }

    [Fact]
    public void EvaluateInsideVarying_MixedBranching()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EV1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 WS-SUM PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
                    EVALUATE I
                        WHEN 1
                            ADD 1 TO WS-SUM
                        WHEN 2
                            ADD 1 TO WS-SUM
                        WHEN 3 THRU 4
                            ADD 2 TO WS-SUM
                        WHEN OTHER
                            ADD 3 TO WS-SUM
                    END-EVALUATE
                END-PERFORM.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("0009", stdout);
    }

    [Fact]
    public void EvaluateAlsoInsideNestedVarying()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EV2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9 VALUE 0.
            01 J PIC 9 VALUE 0.
            01 COUNT1 PIC 9(4) VALUE 0.
            01 COUNT2 PIC 9(4) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    AFTER J FROM 1 BY 1 UNTIL J > 3
                    EVALUATE I ALSO J
                        WHEN 1 ALSO 1
                            ADD 1 TO COUNT1
                        WHEN 1 ALSO 2
                            ADD 1 TO COUNT1
                        WHEN 2 ALSO 3
                            ADD 1 TO COUNT2
                        WHEN OTHER
                            CONTINUE
                    END-EVALUATE
                END-PERFORM.
                DISPLAY COUNT1 COUNT2.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00020001", stdout);
    }

    // ═══════════════════════════════════════════
    // SIGN clause tests
    // ═══════════════════════════════════════════

    [Fact]
    public void SignTrailingSeparate_NegativeResult()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS TRAILING SEPARATE CHARACTER
               VALUE 30.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT 50 FROM WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 30 - 50 = -20, trailing separate → "020-"
        Assert.Equal("020-", stdout);
    }

    [Fact]
    public void SignDefault_TrailingOverpunch()
    {
        // PIC S9(3) with no SIGN clause → default trailing overpunch
        // Positive 42 → last digit '2' overpunched positive → 04B
        // Negative -42 → last digit '2' overpunched negative → 04K
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OVPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-POS PIC S9(3) VALUE 42.
            01 WS-NEG PIC S9(3) VALUE -42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-POS.
                DISPLAY WS-NEG.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // Positive 042: last digit '2' → 'B' (positive overpunch)
        Assert.Equal("04B", lines[0]);
        // Negative 042: last digit '2' → 'K' (negative overpunch)
        Assert.Equal("04K", lines[1]);
    }

    [Fact]
    public void SignDefault_OverpunchArithmeticRoundTrip()
    {
        // Verify arithmetic works correctly with overpunched fields
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OVPART.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(5) VALUE 100.
            01 WS-B PIC S9(5) VALUE 250.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SUBTRACT WS-B FROM WS-A.
                DISPLAY WS-A.
                ADD 300 TO WS-A.
                DISPLAY WS-A.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // 100 - 250 = -150, trailing overpunch: 0015P (P = negative 7... wait, 0 → })
        // Actually: -150 → digits "00150", last digit 0, negative → '}'
        Assert.Equal("0015}", lines[0]);
        // -150 + 300 = 150, positive: 00150, last digit 0 → '{'
        Assert.Equal("0015{", lines[1]);
    }

    [Fact]
    public void SignLeadingSeparate_Arithmetic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSEPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER
               VALUE -25.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD 50 TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // -25 + 50 = 25, leading separate positive → "+025"
        Assert.Equal("+025", stdout);
    }

    [Fact]
    public void SignLeadingOverpunch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LOVPTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS LEADING VALUE -37.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                ADD 100 TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // -37: leading overpunch on first digit '0' → '}', rest "37" → "}37"
        Assert.Equal("}37", lines[0]);
        // -37 + 100 = 63: leading overpunch on '0' → '{', rest "63" → "{63"
        Assert.Equal("{63", lines[1]);
    }

    [Fact]
    public void SignTrailingSeparate_PositiveValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNPOS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC S9(3) SIGN IS TRAILING SEPARATE CHARACTER
               VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD 8 TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 42 + 8 = 50, trailing separate positive → "050+"
        Assert.Contains("050", stdout);
    }

    [Fact]
    public void MoveNumeric_DisplayToCompRoundTrip()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NMOVE1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 DS-LS PIC S9(5) SIGN IS LEADING SEPARATE CHARACTER
               VALUE -12345.
            01 DS-TS PIC S9(5) SIGN IS TRAILING SEPARATE CHARACTER
               VALUE 0.
            01 CU-005 PIC 9(5) COMP VALUE 0.
            01 CS-005 PIC S9(5) COMP VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE DS-LS TO CU-005.
                MOVE CU-005 TO DS-TS.
                DISPLAY DS-TS.
                MOVE DS-LS TO CS-005.
                MOVE CS-005 TO DS-TS.
                DISPLAY DS-TS.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // CU unsigned: stores absolute 12345, then moved to trailing separate → "12345+"
        Assert.Equal("12345+", lines[0]);
        // CS signed: stores -12345, then moved to trailing separate → "12345-"
        Assert.Equal("12345-", lines[1]);
    }

    [Fact]
    public void MoveNumeric_ToEdited_ZeroSuppressed()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EDIT1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC S9(5) VALUE 42.
            01 WS-E PIC ZZ,ZZ9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-N TO WS-E.
                DISPLAY ">" WS-E "<".
                MOVE ZERO TO WS-N.
                MOVE WS-N TO WS-E.
                DISPLAY ">" WS-E "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // 42 in ZZ,ZZ9 → "    42"
        Assert.Equal(">    42<", lines[0]);
        // 0 in ZZ,ZZ9 → "     0"
        Assert.Equal(">     0<", lines[1]);
    }

    [Fact]
    public void FigurativeConstants_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIGTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5).
            01 WS-N PIC 9(5) VALUE ZERO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO WS-A.
                DISPLAY ">" WS-A "<".
                MOVE ALL "AB" TO WS-A.
                DISPLAY WS-A.
                DISPLAY WS-N.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("><", lines[0]);       // SPACES fills with ' ', display trims trailing spaces
        Assert.Equal("ABABA", lines[1]);    // ALL "AB" repeats
        Assert.Equal("00000", lines[2]);    // VALUE ZERO
    }

    [Fact]
    public void FigurativeConstants_HighValueLowValueZeros()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIGTEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(3).
            01 WS-B PIC X(3).
            01 WS-C PIC X(3).
            01 WS-N PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE HIGH-VALUES TO WS-A.
                MOVE LOW-VALUES TO WS-B.
                MOVE ZEROS TO WS-C.
                DISPLAY WS-C.
                MOVE ZEROS TO WS-N.
                DISPLAY WS-N.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // MOVE ZEROS to alphanumeric field fills with '0'
        Assert.Equal("000", lines[0]);
        // MOVE ZEROS to numeric field produces numeric zero
        Assert.Equal("000", lines[1]);
    }

    // ═══════════════════════════════════════════
    // DIVIDE — all spec-true forms
    // ═══════════════════════════════════════════

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
    public void Compare_NegativeLiteral_EqualAndNotEqual()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEGLITCMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(5) VALUE -8036.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A EQUAL TO -8036
                    DISPLAY "EQ-YES"
                ELSE
                    DISPLAY "EQ-NO"
                END-IF.
                IF WS-A NOT EQUAL TO -8036
                    DISPLAY "NEQ-YES"
                ELSE
                    DISPLAY "NEQ-NO"
                END-IF.
                IF WS-A NOT EQUAL TO -9999
                    DISPLAY "DIFF-YES"
                ELSE
                    DISPLAY "DIFF-NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("EQ-YES", lines[0]);
        Assert.Equal("NEQ-NO", lines[1]);
        Assert.Equal("DIFF-YES", lines[2]);
    }

    // ═══════════════════════════════════════════
    // GIVING forms (ADD and SUBTRACT)
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
    public void Level88_ConditionName_SingleValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88TEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-STATUS PIC 9 VALUE 1.
               88 STATUS-ACTIVE VALUE 1.
               88 STATUS-INACTIVE VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF STATUS-ACTIVE
                    DISPLAY "ACTIVE"
                ELSE
                    DISPLAY "NOT ACTIVE"
                END-IF.
                MOVE 0 TO WS-STATUS.
                IF STATUS-INACTIVE
                    DISPLAY "INACTIVE"
                ELSE
                    DISPLAY "NOT INACTIVE"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("ACTIVE", lines[0]);
        Assert.Equal("INACTIVE", lines[1]);
    }

    [Fact]
    public void Level88_ConditionName_MultipleValues()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88MULTI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DAY PIC 9 VALUE 6.
               88 WEEKEND VALUES 6 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WEEKEND
                    DISPLAY "WEEKEND"
                ELSE
                    DISPLAY "WEEKDAY"
                END-IF.
                MOVE 3 TO WS-DAY.
                IF WEEKEND
                    DISPLAY "WEEKEND"
                ELSE
                    DISPLAY "WEEKDAY"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("WEEKEND", lines[0]);
        Assert.Equal("WEEKDAY", lines[1]);
    }

    [Fact]
    public void Level88_ConditionName_InEvaluateTrue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88EVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CODE PIC 9 VALUE 2.
               88 CODE-A VALUE 1.
               88 CODE-B VALUE 2.
               88 CODE-C VALUE 3.
            PROCEDURE DIVISION.
            MAIN-PARA.
                EVALUATE TRUE
                    WHEN CODE-A
                        DISPLAY "A"
                    WHEN CODE-B
                        DISPLAY "B"
                    WHEN CODE-C
                        DISPLAY "C"
                    WHEN OTHER
                        DISPLAY "OTHER"
                END-EVALUATE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("B", stdout);
    }

    [Fact]
    public void Level88_ConditionName_AlphanumericParent()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88ALPHA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLAG PIC X VALUE "Y".
               88 FLAG-YES VALUE "Y".
               88 FLAG-NO VALUE "N".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF FLAG-YES
                    DISPLAY "YES"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("YES", stdout);
    }

    [Fact]
    public void Level88_ConditionName_ThruRange()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88THRU.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-SCORE PIC 99 VALUE 75.
               88 PASSING VALUE 50 THRU 100.
               88 FAILING VALUE 0 THRU 49.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF PASSING
                    DISPLAY "PASS"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                MOVE 30 TO WS-SCORE.
                IF FAILING
                    DISPLAY "LOW"
                ELSE
                    DISPLAY "HIGH"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("PASS\r\nLOW", stdout);
    }

    [Fact]
    public void Level88_ConditionName_MultipleThruRanges()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. L88MTHR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GRADE PIC 99 VALUE 85.
               88 GRADE-A VALUES 90 THRU 100.
               88 GRADE-B VALUES 80 THRU 89.
               88 GRADE-F VALUES 0 THRU 59.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF GRADE-B
                    DISPLAY "B"
                ELSE
                    DISPLAY "NOT-B"
                END-IF.
                MOVE 95 TO WS-GRADE.
                IF GRADE-A
                    DISPLAY "A"
                ELSE
                    DISPLAY "NOT-A"
                END-IF.
                MOVE 40 TO WS-GRADE.
                IF GRADE-F
                    DISPLAY "F"
                ELSE
                    DISPLAY "NOT-F"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("B\r\nA\r\nF", stdout);
    }

    [Fact]
    public void ClassCondition_IsNumeric()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLSNUM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5) VALUE "12345".
            01 WS-B PIC X(5) VALUE "12A45".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A IS NUMERIC
                    DISPLAY "A-NUM"
                ELSE
                    DISPLAY "A-NOT"
                END-IF.
                IF WS-B IS NUMERIC
                    DISPLAY "B-NUM"
                ELSE
                    DISPLAY "B-NOT"
                END-IF.
                IF WS-B IS NOT NUMERIC
                    DISPLAY "B-NOTNUM"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A-NUM", lines[0]);
        Assert.Equal("B-NOT", lines[1]);
        Assert.Equal("B-NOTNUM", lines[2]);
    }

    [Fact]
    public void ClassCondition_IsAlphabetic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLSALPHA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5) VALUE "ABCDE".
            01 WS-B PIC X(5) VALUE "abcde".
            01 WS-C PIC X(5) VALUE "AB1DE".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A IS ALPHABETIC
                    DISPLAY "A-ALPHA"
                END-IF.
                IF WS-A IS ALPHABETIC-UPPER
                    DISPLAY "A-UPPER"
                END-IF.
                IF WS-B IS ALPHABETIC-LOWER
                    DISPLAY "B-LOWER"
                END-IF.
                IF WS-C IS NOT ALPHABETIC
                    DISPLAY "C-NOTALPHA"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A-ALPHA", lines[0]);
        Assert.Equal("A-UPPER", lines[1]);
        Assert.Equal("B-LOWER", lines[2]);
        Assert.Equal("C-NOTALPHA", lines[3]);
    }

    // ── NEXT SENTENCE ──

    [Fact]
    public void NextSentence_SkipsRestOfSentence()
    {
        // NEXT SENTENCE jumps to the first statement after the next period.
        // Sentence 1: IF ... NEXT SENTENCE END-IF MOVE "BAD" TO RESULT.
        // Sentence 2: MOVE "GOOD" TO RESULT.  (NEXT SENTENCE lands here)
        // Sentence 3: DISPLAY RESULT STOP RUN.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEXTSENT1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FLAG PIC 9 VALUE 1.
            01 RESULT PIC X(10).
            PROCEDURE DIVISION.
            SENT-1.
                IF FLAG = 1
                    NEXT SENTENCE
                END-IF
                MOVE "BAD" TO RESULT.
                MOVE "GOOD" TO RESULT.
                DISPLAY RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("GOOD", stdout);
    }

    [Fact]
    public void NextSentence_SkipsMultipleStatements()
    {
        // Sentence 1: NEXT SENTENCE MOVE "BAD1" MOVE "BAD2".
        // Sentence 2 (in SENT-2): MOVE "GOOD" ... DISPLAY ... STOP.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEXTSENT2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 RESULT PIC X(10).
            PROCEDURE DIVISION.
            SENT-1.
                NEXT SENTENCE
                MOVE "BAD1" TO RESULT
                MOVE "BAD2" TO RESULT.
            SENT-2.
                MOVE "GOOD" TO RESULT.
                DISPLAY RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("GOOD", stdout);
    }

    [Fact]
    public void NextSentence_NestedIf()
    {
        // NEXT SENTENCE exits both IFs and jumps past the period.
        // Sentence 1: IF A=1 IF B=1 NEXT SENTENCE END-IF END-IF MOVE "BAD".
        // Sentence 2: MOVE "OK" ... DISPLAY ... STOP.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NEXTSENT3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 1.
            01 B PIC 9 VALUE 1.
            01 RESULT PIC X(10).
            PROCEDURE DIVISION.
            SENT-1.
                IF A = 1
                    IF B = 1
                        NEXT SENTENCE
                    END-IF
                END-IF
                MOVE "BAD" TO RESULT.
                MOVE "OK" TO RESULT.
                DISPLAY RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("OK", stdout);
    }

    // ── ABBREVIATED RELATIONS ──

    [Fact]
    public void AbbreviatedRelation_EqualOrBare()
    {
        // IF A = B OR C  →  (A = B) OR (A = C)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = B OR C
                    DISPLAY "MATCH"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("MATCH", stdout);
    }

    [Fact]
    public void AbbreviatedRelation_EqualOrBare_NoMatch()
    {
        // IF A = B OR C  →  (A = B) OR (A = C), neither true
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR1N.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = B OR C
                    DISPLAY "MATCH"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("NO", stdout);
    }

    [Fact]
    public void AbbreviatedRelation_GreaterAndBare()
    {
        // IF A > B AND C  →  (A > B) AND (A > C)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 9.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A > B AND C
                    DISPLAY "BOTH"
                ELSE
                    DISPLAY "NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("BOTH", stdout);
    }

    [Fact]
    public void AbbreviatedRelation_GreaterAndBare_OneFails()
    {
        // IF A > B AND C  →  (A > B) AND (A > C), second fails
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR2N.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            01 B PIC 9 VALUE 3.
            01 C PIC 9 VALUE 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A > B AND C
                    DISPLAY "BOTH"
                ELSE
                    DISPLAY "NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("NOT", stdout);
    }

    [Fact]
    public void AbbreviatedRelation_ExplicitNotRewritten()
    {
        // IF A < B AND B < C  → already explicit, no rewrite
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ABBR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 1.
            01 B PIC 9 VALUE 5.
            01 C PIC 9 VALUE 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A < B AND B < C
                    DISPLAY "CHAIN"
                ELSE
                    DISPLAY "NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("CHAIN", stdout);
    }

    // ── FILE I/O ──

    [Fact]
    public void FileIO_WriteAndReadBack()
    {
        // Write 3 records, then read them back and display
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIO1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OUT-FILE ASSIGN TO "testfile"
                    ORGANIZATION IS SEQUENTIAL
                    ACCESS MODE IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD OUT-FILE.
            01 OUT-REC.
               05 OUT-TEXT PIC X(10).
            WORKING-STORAGE SECTION.
            01 WS-TEXT PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT OUT-FILE.
                MOVE "AAAAAAAAAA" TO OUT-TEXT.
                WRITE OUT-REC.
                MOVE "BBBBBBBBBB" TO OUT-TEXT.
                WRITE OUT-REC.
                MOVE "CCCCCCCCCC" TO OUT-TEXT.
                WRITE OUT-REC.
                CLOSE OUT-FILE.
                OPEN INPUT OUT-FILE.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "UNEXPECTED-END"
                END-READ.
                DISPLAY WS-TEXT.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "UNEXPECTED-END"
                END-READ.
                DISPLAY WS-TEXT.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "UNEXPECTED-END"
                END-READ.
                DISPLAY WS-TEXT.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "AT-END-OK"
                END-READ.
                CLOSE OUT-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("AAAAAAAAAA", lines[0]);
        Assert.Equal("BBBBBBBBBB", lines[1]);
        Assert.Equal("CCCCCCCCCC", lines[2]);
        Assert.Equal("AT-END-OK", lines[3]);
    }

    [Fact]
    public void FileIO_ReadAtEnd_Branching()
    {
        // Create input file, then read until AT END
        // First: write a file with 2 records
        var (success1, _, stderr1) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOWR.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DAT-FILE ASSIGN TO "readtest"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD DAT-FILE.
            01 DAT-REC.
               05 DAT-NUM PIC 9(5).
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT DAT-FILE.
                MOVE 11111 TO DAT-NUM.
                WRITE DAT-REC.
                MOVE 22222 TO DAT-NUM.
                WRITE DAT-REC.
                CLOSE DAT-FILE.
                STOP RUN.
            """);
        Assert.True(success1, $"Write failed: {stderr1}");

        // Now read it back and count records
        var (success2, stdout2, stderr2) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIORD.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DAT-FILE ASSIGN TO "readtest"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD DAT-FILE.
            01 DAT-REC.
               05 DAT-NUM PIC 9(5).
            WORKING-STORAGE SECTION.
            01 WS-COUNT PIC 99 VALUE 0.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN INPUT DAT-FILE.
                PERFORM UNTIL WS-EOF = 1
                    READ DAT-FILE
                        AT END MOVE 1 TO WS-EOF
                        NOT AT END ADD 1 TO WS-COUNT
                    END-READ
                END-PERFORM.
                CLOSE DAT-FILE.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success2, $"Read failed: {stderr2}");
        Assert.Equal("02", stdout2);
    }

    [Fact]
    public void FileIO_OpenOutputCreatesFile()
    {
        // OPEN OUTPUT should create the file; WRITE should populate it
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOCR.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OUT-FILE ASSIGN TO "newfile"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD OUT-FILE.
            01 OUT-REC.
               05 OUT-DATA PIC X(5).
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT OUT-FILE.
                MOVE "HELLO" TO OUT-DATA.
                WRITE OUT-REC.
                CLOSE OUT-FILE.
                DISPLAY "DONE".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DONE", stdout);

        // Verify the file was created
        string filePath = Path.Combine(_tempDir, "newfile.txt");
        Assert.True(File.Exists(filePath), $"Output file not found at {filePath}");
        string content = File.ReadAllText(filePath);
        Assert.Contains("HELLO", content);
    }

    [Fact]
    public void FileIO_ReadNumericRoundTrip()
    {
        // Write numeric data, read it back, verify arithmetic works on it
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIONUM.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT NUM-FILE ASSIGN TO "numfile"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD NUM-FILE.
            01 NUM-REC.
               05 NUM-VAL PIC 9(5).
            WORKING-STORAGE SECTION.
            01 WS-SUM PIC 9(6) VALUE 0.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT NUM-FILE.
                MOVE 00100 TO NUM-VAL.
                WRITE NUM-REC.
                MOVE 00200 TO NUM-VAL.
                WRITE NUM-REC.
                MOVE 00300 TO NUM-VAL.
                WRITE NUM-REC.
                CLOSE NUM-FILE.
                OPEN INPUT NUM-FILE.
                PERFORM UNTIL WS-EOF = 1
                    READ NUM-FILE
                        AT END MOVE 1 TO WS-EOF
                        NOT AT END ADD NUM-VAL TO WS-SUM
                    END-READ
                END-PERFORM.
                CLOSE NUM-FILE.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("000600", stdout);
    }

    [Fact]
    public void FileIO_FileStatus_00And10()
    {
        // Verify FILE STATUS is populated with "00" on success and "10" at end
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOSTATUS.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DAT-FILE ASSIGN TO "statustest"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD DAT-FILE.
            01 DAT-REC.
               05 DAT-TEXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT DAT-FILE.
                DISPLAY FS.
                MOVE "HELLO" TO DAT-TEXT.
                WRITE DAT-REC.
                DISPLAY FS.
                CLOSE DAT-FILE.
                DISPLAY FS.
                OPEN INPUT DAT-FILE.
                DISPLAY FS.
                READ DAT-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY FS.
                READ DAT-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY FS.
                CLOSE DAT-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("00", lines[0]); // OPEN OUTPUT → 00
        Assert.Equal("00", lines[1]); // WRITE → 00
        Assert.Equal("00", lines[2]); // CLOSE → 00
        Assert.Equal("00", lines[3]); // OPEN INPUT → 00
        Assert.Equal("00", lines[4]); // READ (success) → 00
        Assert.Equal("10", lines[5]); // READ (at end) → 10
    }

    [Fact]
    public void FileIO_FileStatus_35_FileNotFound()
    {
        // Verify FILE STATUS is "35" when opening non-existent file for input
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOSTAT35.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT MISSING-FILE ASSIGN TO "nonexistent"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD MISSING-FILE.
            01 MISS-REC.
               05 MISS-TEXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN INPUT MISSING-FILE.
                DISPLAY FS.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("35", stdout);
    }

    [Fact]
    public void FileIO_Rewrite_ReplacesRecord()
    {
        // Write 2 records, reopen I-O, read first, rewrite it, read and verify
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIORW.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RW-FILE ASSIGN TO "rwtest"
                    ORGANIZATION IS SEQUENTIAL
                    ACCESS MODE IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RW-FILE.
            01 RW-REC.
               05 RW-TEXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-TEXT PIC X(5).
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RW-FILE.
                MOVE "AAAAA" TO RW-TEXT.
                WRITE RW-REC.
                MOVE "BBBBB" TO RW-TEXT.
                WRITE RW-REC.
                CLOSE RW-FILE.
                OPEN INPUT RW-FILE.
                READ RW-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY RW-TEXT.
                READ RW-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY RW-TEXT.
                CLOSE RW-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("AAAAA", lines[0]);
        Assert.Equal("BBBBB", lines[1]);
    }

    [Fact]
    public void FileIO_LineSequential_RoundTrip()
    {
        // Explicit ORGANIZATION IS LINE SEQUENTIAL round-trip
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOLS.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LS-FILE ASSIGN TO "lineseqtest"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD LS-FILE.
            01 LS-REC.
               05 LS-TEXT PIC X(8).
            WORKING-STORAGE SECTION.
            01 WS-BUF PIC X(8).
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LS-FILE.
                MOVE "LINESEQ1" TO LS-TEXT.
                WRITE LS-REC.
                MOVE "LINESEQ2" TO LS-TEXT.
                WRITE LS-REC.
                CLOSE LS-FILE.
                OPEN INPUT LS-FILE.
                READ LS-FILE INTO WS-BUF
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY WS-BUF.
                READ LS-FILE INTO WS-BUF
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY WS-BUF.
                READ LS-FILE INTO WS-BUF
                    AT END DISPLAY "AT-END-OK"
                END-READ.
                CLOSE LS-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("LINESEQ1", lines[0]);
        Assert.Equal("LINESEQ2", lines[1]);
        Assert.Equal("AT-END-OK", lines[2]);
    }

    // ── INITIALIZE ──

    [Fact]
    public void Initialize_GroupWithMixedChildren()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INIT2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 N1 PIC 9(3) VALUE 123.
               05 A1 PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE G1.
                DISPLAY N1.
                DISPLAY ">" A1 "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("000", lines[0]);
        Assert.Equal("><", lines[1]); // spaces trimmed by DISPLAY
    }

    [Fact]
    public void Initialize_Redefines_InitializesUnderlying()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INIT3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 N1 PIC 9(3) VALUE 123.
               05 R1 REDEFINES N1 PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE G1.
                DISPLAY N1.
                DISPLAY R1.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // N1 is numeric → initialized to 000
        // R1 REDEFINES N1, shares same storage → shows "000"
        Assert.Equal("000", lines[0]);
        Assert.Equal("000", lines[1]);
    }

    [Fact]
    public void Initialize_ReplacingNumericAndAlphanumeric()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INIT4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G1.
               05 N1 PIC 9(3) VALUE 123.
               05 A1 PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INITIALIZE G1
                    REPLACING NUMERIC DATA BY 7
                              ALPHANUMERIC DATA BY "Q".
                DISPLAY N1.
                DISPLAY A1.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("007", lines[0]);
        Assert.Equal("Q", lines[1]); // "Q" moved to X(3), right-padded with spaces, trimmed
    }

    // ── SET ──

    [Fact]
    public void Set_ConditionName_ToTrue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SET3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FLAG PIC X.
               88 FLAG-ON VALUE "Y".
               88 FLAG-OFF VALUE "N".
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET FLAG-ON TO TRUE.
                IF FLAG-ON
                    DISPLAY "ON"
                ELSE
                    DISPLAY "OFF"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ON", stdout);
    }

    [Fact]
    public void Set_ConditionName_ToFalse()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SET4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FLAG PIC X VALUE "Y".
               88 FLAG-ON VALUE "Y".
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET FLAG-ON TO FALSE.
                IF FLAG-ON
                    DISPLAY "ON"
                ELSE
                    DISPLAY "OFF"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OFF", stdout);
    }

    [Fact]
    public void Set_Index_UpByDownBy()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SET2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-IDX PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET WS-IDX TO 2.
                SET WS-IDX UP BY 2.
                DISPLAY WS-IDX.
                SET WS-IDX DOWN BY 1.
                DISPLAY WS-IDX.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("004", lines[0]);
        Assert.Equal("003", lines[1]);
    }

    // ── INSPECT ──

    [Fact]
    public void Inspect_TallyingAll()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            01 WS-COUNT PIC 99 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA TALLYING WS-COUNT
                    FOR ALL "AA".
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("03", stdout);
    }

    [Fact]
    public void Inspect_ReplacingFirst()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AABBAACCAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING FIRST "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXBBAACCAA", stdout);
    }

    [Fact]
    public void Inspect_ReplacingLeading()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "AAAABBCCAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING LEADING "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXXXBBCCAA", stdout);
    }

    [Fact]
    public void Inspect_Converting()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(10) VALUE "ABCABCABCA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA CONVERTING "ABC" TO "XYZ".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XYZXYZXYZX", stdout);
    }

    [Fact]
    public void Inspect_ReplacingAllBeforeAfter()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INSP5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DATA PIC X(12) VALUE "AABBCCAADDAA".
            PROCEDURE DIVISION.
            MAIN-PARA.
                INSPECT WS-DATA REPLACING ALL "AA" BY "XX"
                    AFTER "CC" BEFORE "DD".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("AABBCCXXDDAA", stdout);
    }

    // ── ACCEPT ──

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

    // ── GO TO DEPENDING ──

    [Fact]
    public void GoToDepending_SelectsCorrectTarget()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOTO1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO P1 P2 P3 DEPENDING ON IDX.
                DISPLAY "FALLTHROUGH".
                STOP RUN.
            P1.
                DISPLAY "ONE".
                STOP RUN.
            P2.
                DISPLAY "TWO".
                STOP RUN.
            P3.
                DISPLAY "THREE".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("TWO", stdout);
    }

    [Fact]
    public void GoToDepending_OutOfRange_FallsThrough()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOTO2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO P1 P2 DEPENDING ON IDX.
                DISPLAY "DONE".
                STOP RUN.
            P1.
                DISPLAY "ONE".
                STOP RUN.
            P2.
                DISPLAY "TWO".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DONE", stdout);
    }

    [Fact]
    public void GoToDepending_FallsIntoNextParagraph()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GOTO3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 IDX PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO P1 P2 P3 DEPENDING ON IDX.
                DISPLAY "AFTER-GOTO".
                STOP RUN.
            P1.
                DISPLAY "ONE".
                STOP RUN.
            P2.
                DISPLAY "TWO".
            P3.
                DISPLAY "THREE".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("TWO", lines[0]);
        Assert.Equal("THREE", lines[1]);
    }
}
