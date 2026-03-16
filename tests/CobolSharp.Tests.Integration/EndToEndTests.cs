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

    [Fact(Skip = "EVALUATE statement not yet lowered to CIL")]
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

    [Fact(Skip = "SET statement not yet lowered to CIL")]
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

    [Fact(Skip = "INITIALIZE statement not yet lowered to CIL")]
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

    [Fact(Skip = "ACCEPT FROM DATE not yet lowered to CIL")]
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

    [Fact(Skip = "INSPECT REPLACING not yet lowered to CIL")]
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

    [Fact(Skip = "READ statement not yet lowered to CIL")]
    public void FileIO_WriteAndReadBack()
    {
        // Create a test data file path
        string dataFile = Path.Combine(_tempDir, "testdata.dat");

        var (success, stdout, stderr) = CompileAndRun($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FILETEST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "{{dataFile}}"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-RECORD PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-REC PIC X(20).
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT TEST-FILE.
                MOVE "Hello File" TO TEST-RECORD.
                WRITE TEST-RECORD.
                MOVE "Line Two" TO TEST-RECORD.
                WRITE TEST-RECORD.
                CLOSE TEST-FILE.
                OPEN INPUT TEST-FILE.
                READ TEST-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY TEST-RECORD.
                CLOSE TEST-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Should display the first record read back
        Assert.StartsWith("Hello File", stdout);
    }

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
}
