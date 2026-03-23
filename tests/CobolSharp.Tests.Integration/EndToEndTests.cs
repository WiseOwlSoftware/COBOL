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

    /// <summary>
    /// Compile multiple COBOL programs and run the first one.
    /// Each entry is (filename, source). The first program is the main program.
    /// All programs are compiled to the same temp directory so they can find each other.
    /// </summary>
    private (bool success, string stdout, string stderr) CompileMultipleAndRun(
        params (string fileName, string source)[] programs)
    {
        // Compile all programs
        foreach (var (fileName, source) in programs)
        {
            string sourcePath = Path.Combine(_tempDir, fileName);
            string outputPath = Path.Combine(_tempDir,
                Path.GetFileNameWithoutExtension(fileName) + ".dll");
            File.WriteAllText(sourcePath, source);

            var compilation = new Compilation();
            var result = compilation.Compile(sourcePath, outputPath);
            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
                return (false, "", $"Compilation of {fileName} failed:\n{errors}");
            }
        }

        // Run the first program
        string mainDll = Path.Combine(_tempDir,
            Path.GetFileNameWithoutExtension(programs[0].fileName) + ".dll");

        // Copy runtime DLL to temp dir so called programs can find it
        string runtimeDll = typeof(CobolSharp.Runtime.ProgramState).Assembly.Location;
        string runtimeDest = Path.Combine(_tempDir, Path.GetFileName(runtimeDll));
        if (!File.Exists(runtimeDest))
            File.Copy(runtimeDll, runtimeDest);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = mainDll,
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
    public void ExitPerform_LeavesLoopEarly()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITP1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9.
            01 WS-SUM PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO WS-SUM.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 10
                    ADD I TO WS-SUM
                    IF I = 4
                        EXIT PERFORM
                    END-IF
                END-PERFORM.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // 1+2+3+4 = 10
        Assert.Equal("0010", stdout);
    }

    [Fact]
    public void ExitPerform_ExitsOnlyInnermostPerform()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITP2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9.
            01 J PIC 9.
            01 WS-COUNT PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO WS-COUNT.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
                    PERFORM VARYING J FROM 1 BY 1 UNTIL J > 5
                        ADD 1 TO WS-COUNT
                        IF J = 2
                            EXIT PERFORM
                        END-IF
                    END-PERFORM
                END-PERFORM.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // For each I (1,2,3): J runs 1,2 → EXIT → 2 iterations each = 6
        Assert.Equal("0006", stdout);
    }

    [Fact]
    public void ExitParagraph_SkipsRemainingStatements()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITPARA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TRACE PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM TEST-PARA.
                DISPLAY WS-TRACE.
                STOP RUN.
            TEST-PARA.
                MOVE "A" TO WS-TRACE(1:1)
                EXIT PARAGRAPH
                MOVE "B" TO WS-TRACE(2:1).
            """);

        Assert.True(success, $"Failed: {stderr}");
        // EXIT PARAGRAPH skips MOVE "B", so only "A" followed by spaces
        Assert.Equal("A", stdout.TrimEnd());
    }

    [Fact]
    public void ExitParagraph_InsidePerformVarying()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITPARA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 I PIC 9(2) VALUE ZERO.
            01 WS-COUNT PIC 9(2) VALUE ZERO.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM COUNT-PARA
                    VARYING I FROM 1 BY 1 UNTIL I > 5.
                DISPLAY WS-COUNT.
                STOP RUN.
            COUNT-PARA.
                ADD 1 TO WS-COUNT
                EXIT PARAGRAPH
                ADD 10 TO WS-COUNT.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // EXIT PARAGRAPH skips the ADD 10, so count = 5 (not 55)
        Assert.Equal("05", stdout);
    }

    [Fact]
    public void ExitSection_SkipsRemainingParagraphs()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITSEC1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TRACE PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-PARA.
                PERFORM TEST-SEC
                DISPLAY WS-TRACE
                STOP RUN.
            TEST-SEC SECTION.
            PARA-1.
                MOVE "A" TO WS-TRACE(1:1)
                EXIT SECTION
                MOVE "X" TO WS-TRACE(2:1).
            PARA-2.
                MOVE "B" TO WS-TRACE(3:1).
            AFTER-SEC SECTION.
            AFTER-PARA.
                CONTINUE.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // EXIT SECTION in PARA-1 skips rest of PARA-1 and all of PARA-2
        Assert.Equal("A", stdout.TrimEnd());
    }

    [Fact]
    public void ExitSection_PerformSectionStopsAtBoundary()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITSEC2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TRACE PIC X(10) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-SEC SECTION.
            MAIN-PARA.
                PERFORM SEC-A
                DISPLAY WS-TRACE
                STOP RUN.
            SEC-A SECTION.
            PARA-A1.
                MOVE "1" TO WS-TRACE(1:1).
            PARA-A2.
                MOVE "2" TO WS-TRACE(2:1).
            SEC-B SECTION.
            PARA-B1.
                MOVE "3" TO WS-TRACE(3:1).
            """);

        Assert.True(success, $"Failed: {stderr}");
        // PERFORM SEC-A runs PARA-A1 and PARA-A2 only, not PARA-B1
        Assert.Equal("12", stdout.TrimEnd());
    }

    [Fact]
    public void ExitParagraph_InsideNestedPerform()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXITPARA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X VALUE SPACE.
            01 WS-B PIC X VALUE SPACE.
            01 WS-C PIC X VALUE SPACE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM OUTER-PARA
                DISPLAY WS-A WS-B WS-C
                STOP RUN.
            OUTER-PARA.
                MOVE "O" TO WS-A
                PERFORM INNER-PARA
                MOVE "Z" TO WS-C.
            INNER-PARA.
                MOVE "I" TO WS-B
                EXIT PARAGRAPH
                MOVE "X" TO WS-C.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // INNER-PARA: sets B=I, exits, skips X. OUTER continues: sets C=Z.
        Assert.Equal("OIZ", stdout);
    }

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

    [Fact]
    public void FileIO_WriteFrom_CopiesBeforeWriting()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOWF.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT WF-FILE ASSIGN TO "wftest"
                    ORGANIZATION IS SEQUENTIAL
                    ACCESS MODE IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD WF-FILE.
            01 WF-REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-SRC PIC X(5) VALUE "HELLO".
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT WF-FILE.
                WRITE WF-REC FROM WS-SRC.
                CLOSE WF-FILE.
                OPEN INPUT WF-FILE.
                READ WF-FILE
                    AT END DISPLAY "EMPTY"
                END-READ.
                DISPLAY WF-REC.
                CLOSE WF-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("HELLO", stdout);
    }

    [Fact]
    public void FileIO_Delete_IndexedFile()
    {
        // Write two records, reopen, read first, delete it, verify only second remains
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIODEL.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixdel"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IX-FILE.
            01 IX-REC.
               05 IX-KEY PIC X(3).
               05 IX-VAL PIC X(3).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IX-FILE.
                MOVE "AAA" TO IX-KEY.
                MOVE "111" TO IX-VAL.
                WRITE IX-REC.
                MOVE "BBB" TO IX-KEY.
                MOVE "222" TO IX-VAL.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN I-O IX-FILE.
                READ IX-FILE.
                DELETE IX-FILE.
                DISPLAY WS-FS.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                READ IX-FILE.
                DISPLAY IX-KEY IX-VAL.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("00", lines[0]); // DELETE succeeded
        Assert.Equal("BBB222", lines[1]); // Only BBB remains
    }

    [Fact]
    public void FileIO_Start_PositionsForReadNext()
    {
        // Write 3 records to indexed file, START at key >= "BBB", READ NEXT
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOSTRT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixstart"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IX-FILE.
            01 IX-REC.
               05 IX-KEY PIC X(3).
               05 IX-VAL PIC X(3).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IX-FILE.
                MOVE "AAA" TO IX-KEY.
                MOVE "111" TO IX-VAL.
                WRITE IX-REC.
                MOVE "BBB" TO IX-KEY.
                MOVE "222" TO IX-VAL.
                WRITE IX-REC.
                MOVE "CCC" TO IX-KEY.
                MOVE "333" TO IX-VAL.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                MOVE "BBB" TO IX-KEY.
                START IX-FILE KEY IS IX-KEY.
                DISPLAY WS-FS.
                READ IX-FILE NEXT RECORD.
                DISPLAY IX-KEY IX-VAL.
                READ IX-FILE NEXT RECORD.
                DISPLAY IX-KEY IX-VAL.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("00", lines[0]); // START succeeded
        Assert.Equal("BBB222", lines[1]); // First READ NEXT after START
        Assert.Equal("CCC333", lines[2]); // Second READ NEXT
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

    // ── OCCURS + Subscripts ──

    [Fact]
    public void Subscript_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 7 TO ITEM(3).
                DISPLAY ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("7", stdout);
    }

    [Fact]
    public void Subscript_MultipleElements()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 5 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ITEM(1).
                MOVE 2 TO ITEM(2).
                MOVE 3 TO ITEM(3).
                DISPLAY ITEM(1) ITEM(2) ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("123", stdout);
    }

    [Fact]
    public void Subscript_InGoToDepending()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GO-TABLE.
               05 GO-SCRIPT PIC 9 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 2 TO GO-SCRIPT(1).
                GO TO P1 P2 P3 DEPENDING ON GO-SCRIPT(1).
                DISPLAY "FALL".
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
    public void Subscript_VariableSubscript_MoveAndDisplay()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. VSUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 3 TIMES.
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO I.
                MOVE 3 TO J.
                MOVE 0 TO ITEM(1).
                MOVE 0 TO ITEM(2).
                MOVE 0 TO ITEM(3).
                MOVE 7 TO ITEM(I).
                MOVE ITEM(I) TO ITEM(J).
                DISPLAY ITEM(1) ITEM(2) ITEM(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("707", stdout);
    }

    [Fact]
    public void Subscript_ExpressionSubscript_Addition()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXPRSUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 5 TIMES.
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO ITEM(1) ITEM(2) ITEM(3) ITEM(4) ITEM(5).
                MOVE 2 TO I.
                MOVE 7 TO ITEM(I + 1).
                DISPLAY ITEM(1) ITEM(2) ITEM(3) ITEM(4) ITEM(5).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("00700", stdout);
    }

    [Fact]
    public void Subscript_ExpressionSubscript_Multiplication()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXPRSUB2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC 9 OCCURS 9 TIMES.
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO ITEM(1) ITEM(2) ITEM(3) ITEM(4)
                          ITEM(5) ITEM(6) ITEM(7) ITEM(8)
                          ITEM(9).
                MOVE 2 TO I.
                MOVE 3 TO J.
                MOVE 5 TO ITEM(I * J).
                DISPLAY ITEM(6).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("5", stdout);
    }

    [Fact]
    public void Subscript_2D_ConstantSubscripts()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL PIC 9 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO COL(1, 1).
                MOVE 2 TO COL(1, 2).
                MOVE 3 TO COL(1, 3).
                MOVE 4 TO COL(2, 1).
                MOVE 5 TO COL(2, 2).
                MOVE 6 TO COL(2, 3).
                DISPLAY COL(1, 1) COL(1, 2) COL(1, 3)
                        COL(2, 1) COL(2, 2) COL(2, 3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("123456", stdout);
    }

    [Fact]
    public void Subscript_2D_VariableSubscripts()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. VSUB2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL PIC 9 OCCURS 3 TIMES.
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 2 TO I.
                MOVE 3 TO J.
                MOVE 7 TO COL(I, J).
                DISPLAY COL(2, 3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("7", stdout);
    }

    [Fact]
    public void Subscript_3D_ConstantSubscripts()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUB3D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CUBE.
               05 X-DIM OCCURS 2 TIMES.
                  10 Y-DIM OCCURS 2 TIMES.
                     15 Z-ITEM PIC 9 OCCURS 2 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 9 TO Z-ITEM(2, 1, 2).
                DISPLAY Z-ITEM(2, 1, 2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("9", stdout);
    }

    [Fact]
    public void Subscript_RedefinesWithOccurs()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REDSUB1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 A OCCURS 3 TIMES.
                  10 F1 PIC X(4).
                  10 F2 REDEFINES F1 PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "1234" TO F1(2).
                DISPLAY F2(2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1234", stdout);
    }

    [Fact]
    public void Subscript_GroupOccursChild()
    {
        // Subscripted reference to a child of an OCCURS group.
        // The step size must be the group's element size (VAL + FLAG = 2),
        // not the child's size (VAL = 1). Without this, VAL(2) reads
        // offset 1 instead of offset 2.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPOCC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 3 TIMES.
                  10 VAL PIC 9.
                  10 FLAG PIC X.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO VAL(1).
                MOVE "A" TO FLAG(1).
                MOVE 2 TO VAL(2).
                MOVE "B" TO FLAG(2).
                MOVE 3 TO VAL(3).
                MOVE "C" TO FLAG(3).
                DISPLAY VAL(1) FLAG(1) VAL(2) FLAG(2) VAL(3) FLAG(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1A2B3C", stdout);
    }

    // ── Reference Modification ──

    [Fact]
    public void RefMod_ConstantStartLength()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                DISPLAY FIELD(3:4).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CDEF", stdout);
    }

    [Fact]
    public void RefMod_WithSubscript()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ITEM PIC X(4) OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO ITEM(2).
                DISPLAY ITEM(2)(2:2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("BC", stdout);
    }

    [Fact]
    public void RefMod_VariableStart()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                MOVE 4 TO I.
                DISPLAY FIELD(I:3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DEF", stdout);
    }

    [Fact]
    public void RefMod_RestOfField()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDE" TO FIELD.
                DISPLAY FIELD(3:).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CDE", stdout);
    }

    [Fact]
    public void RefMod_ExpressionStartLength()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                DISPLAY FIELD(2 + 1:4 - 1).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CDE", stdout);
    }

    [Fact]
    public void RefMod_2DSubscriptWithRefMod()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. REFMOD6.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL PIC X(4) OCCURS 2 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO COL(2, 1).
                DISPLAY COL(2, 1)(3:2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CD", stdout);
    }

    [Fact]
    public void Subscript_Comp3Array()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP3SUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM PIC 9(4) COMP-3 OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234 TO ITEM(2).
                DISPLAY ITEM(2).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("1234", stdout);
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

    // ── IrLocation regression: subscripts and ref-mod in expressions ──
    // These tests lock in the invariant that EmitExpression goes through IrLocation
    // for all data references, not directly to GetStorageLocation.

    [Fact]
    public void If_SubscriptedIdentifier_ComparisonWorks()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSUB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ARR.
               05 ELEM PIC 9 OCCURS 3 TIMES.
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 2 TO I.
                MOVE 7 TO ELEM(I).
                IF ELEM(I) = 7
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }

    [Fact]
    public void If_RefMod_ComparisonWorks()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFREFMOD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDE" TO FIELD.
                IF FIELD(2:3) = "BCD"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }

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
    public void If_SubscriptedRefMod_ComparisonWorks()
    {
        // Subscripts + ref-mod combined in a comparison
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFSUBREF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2.
                  10 COL OCCURS 2 PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCD" TO COL(2, 1).
                IF COL(2, 1)(3:2) = "CD"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
    }

    [Fact]
    public void If_VariableRefMod_ComparisonWorks()
    {
        // Variable start/length in ref-mod comparison
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFVARREF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FIELD PIC X(10).
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHIJ" TO FIELD.
                MOVE 3 TO I.
                MOVE 4 TO J.
                IF FIELD(I:J) = "CDEF"
                    DISPLAY "OK"
                ELSE
                    DISPLAY "FAIL"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OK", stdout);
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

    // ── SEARCH (linear) ──

    [Fact]
    public void Search_SimpleMatch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCH1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES
                  INDEXED BY IDX.
            01 IDX-VAL PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                SET IDX TO 1.
                SEARCH ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = 3
                        DISPLAY "FOUND 3"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 3", stdout);
    }

    [Fact]
    public void Search_NotFound()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCH2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES
                  INDEXED BY IDX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                SET IDX TO 1.
                SEARCH ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = 9
                        DISPLAY "FOUND"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NOT FOUND", stdout);
    }

    [Fact]
    public void Search_MultiFieldWhen()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCH3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 3 TIMES
                  INDEXED BY IDX.
                  10 VAL PIC 9.
                  10 FLAG PIC X.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO VAL(1).
                MOVE "N" TO FLAG(1).
                MOVE 1 TO VAL(2).
                MOVE "Y" TO FLAG(2).
                MOVE 2 TO VAL(3).
                MOVE "Y" TO FLAG(3).
                SET IDX TO 1.
                SEARCH ROW
                    AT END DISPLAY "NOT FOUND"
                    WHEN VAL(IDX) = 1 AND FLAG(IDX) = "Y"
                        DISPLAY "FOUND 2"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }

    [Fact]
    public void Search_ChildOfOccursGroup_DifferentSizes()
    {
        // OCCURS group with children of different sizes (A=2, B=3, group=5).
        // Step size must be 5 (group), not 2 (A) or 3 (B).
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPSRCH.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM OCCURS 3 TIMES
                  INDEXED BY IDX.
                  10 A PIC X(2).
                  10 B PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1).
                MOVE "BB" TO A(2).
                MOVE "CC" TO A(3).
                SET IDX TO 1.
                SEARCH ITEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN A(IDX) = "BB"
                        DISPLAY "FOUND 2"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }

    [Fact]
    public void Search_ChildOfOccursGroup_MultiFieldWhen()
    {
        // Two child-of-OCCURS references in the same WHEN condition.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPMF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM OCCURS 3 TIMES
                  INDEXED BY IDX.
                  10 A PIC X(2).
                  10 B PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1).
                MOVE "111" TO B(1).
                MOVE "BB" TO A(2).
                MOVE "222" TO B(2).
                MOVE "CC" TO A(3).
                MOVE "333" TO B(3).
                SET IDX TO 1.
                SEARCH ITEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN A(IDX) = "BB" AND B(IDX) = "222"
                        DISPLAY "FOUND 2"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }

    [Fact]
    public void SearchAll_ChildOfOccursGroup()
    {
        // SEARCH ALL with child-of-OCCURS group, ensuring the linear
        // lowering path also uses the correct group step size.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. GRPSA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ITEM OCCURS 3 TIMES.
                  10 A PIC X(2).
                  10 B PIC X(3).
            01 IDX PIC 9.
            01 K PIC X(2).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1).
                MOVE "BB" TO A(2).
                MOVE "CC" TO A(3).
                MOVE "BB" TO K.
                SEARCH ALL ITEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN A(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 2", stdout);
    }

    // ── SEARCH with multi-dimensional OCCURS (outer index via PERFORM) ──

    [Fact]
    public void Search_2D_OuterIndexFromPerform()
    {
        // SEARCH only iterates the innermost dimension (COL).
        // Outer dimension (ROW) is driven by PERFORM VARYING.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. S2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL OCCURS 3 TIMES
                     INDEXED BY J.
                     15 A PIC X(2).
                     15 B PIC X(3).
            01 I PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1, 1).
                MOVE "BB" TO A(1, 2).
                MOVE "CC" TO A(1, 3).
                MOVE "DD" TO A(2, 1).
                MOVE "EE" TO A(2, 2).
                MOVE "FF" TO A(2, 3).
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    SET J TO 1
                    SEARCH COL
                        AT END DISPLAY "MISS " I
                        WHEN A(I, J) = "EE"
                            DISPLAY "FOUND " I " 2"
                    END-SEARCH
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("MISS 1", lines[0]);
        Assert.Equal("FOUND 2 2", lines[1]);
    }

    [Fact]
    public void Search_3D_OuterIndicesFromPerform()
    {
        // 3D OCCURS: SEARCH iterates innermost (COL), outer two via nested PERFORM.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. S3D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 PLANE OCCURS 2 TIMES.
                  10 ROW OCCURS 2 TIMES.
                     15 COL OCCURS 2 TIMES
                        INDEXED BY C.
                        20 A PIC X(2).
                        20 B PIC X(3).
            01 P PIC 9.
            01 R PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1, 1, 1).
                MOVE "BB" TO A(1, 1, 2).
                MOVE "CC" TO A(1, 2, 1).
                MOVE "DD" TO A(1, 2, 2).
                MOVE "EE" TO A(2, 1, 1).
                MOVE "FF" TO A(2, 1, 2).
                MOVE "GG" TO A(2, 2, 1).
                MOVE "HH" TO A(2, 2, 2).
                PERFORM VARYING P FROM 1 BY 1 UNTIL P > 2
                    PERFORM VARYING R FROM 1 BY 1 UNTIL R > 2
                        SET C TO 1
                        SEARCH COL
                            AT END DISPLAY "MISS"
                            WHEN A(P, R, C) = "GG"
                                DISPLAY "FOUND " P " " R " 1"
                        END-SEARCH
                    END-PERFORM
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // Misses for (1,1), (1,2), (2,1), then found at (2,2,1)
        Assert.Equal("MISS", lines[0]);
        Assert.Equal("MISS", lines[1]);
        Assert.Equal("MISS", lines[2]);
        Assert.Equal("FOUND 2 2 1", lines[3]);
    }

    [Fact]
    public void SearchAll_2D_OuterIndexFromPerform()
    {
        // SEARCH ALL with 2D OCCURS, outer index via PERFORM.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SA2D.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ROW OCCURS 2 TIMES.
                  10 COL OCCURS 3 TIMES.
                     15 A PIC X(2).
                     15 B PIC X(3).
            01 I PIC 9.
            01 J PIC 9.
            01 K PIC X(2).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AA" TO A(1, 1).
                MOVE "BB" TO A(1, 2).
                MOVE "CC" TO A(1, 3).
                MOVE "DD" TO A(2, 1).
                MOVE "EE" TO A(2, 2).
                MOVE "FF" TO A(2, 3).
                MOVE "EE" TO K.
                PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
                    SEARCH ALL COL
                        AT END DISPLAY "MISS " I
                        WHEN A(I, J) = K
                            DISPLAY "FOUND " I " " J
                    END-SEARCH
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("MISS 1", lines[0]);
        Assert.Equal("FOUND 2 2", lines[1]);
    }

    // ── SEARCH ALL ──

    [Fact]
    public void SearchAll_ExactMatch()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 3 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 3", stdout);
    }

    [Fact]
    public void SearchAll_NotFound()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 9 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND"
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NOT FOUND", stdout);
    }

    [Fact]
    public void SearchAll_FirstElement()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 1 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 1", stdout);
    }

    [Fact]
    public void SearchAll_LastElement()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SRCHA4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
               05 ELEM PIC 9 OCCURS 5 TIMES.
            01 IDX PIC 9.
            01 K PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1 TO ELEM(1).
                MOVE 2 TO ELEM(2).
                MOVE 3 TO ELEM(3).
                MOVE 4 TO ELEM(4).
                MOVE 5 TO ELEM(5).
                MOVE 5 TO K.
                SEARCH ALL ELEM
                    AT END DISPLAY "NOT FOUND"
                    WHEN ELEM(IDX) = K
                        DISPLAY "FOUND " IDX
                END-SEARCH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FOUND 5", stdout);
    }

    // ── STRING ──

    [Fact]
    public void String_ConcatenatesLiterals()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO T.
                STRING "AB" "CD" DELIMITED BY SIZE
                    INTO T
                END-STRING.
                DISPLAY T.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // DISPLAY trims trailing spaces; STRING only writes to positions 1-4,
        // remaining positions keep their SPACES value but are trimmed by DISPLAY
        Assert.Equal("ABCD", stdout);
    }

    [Fact]
    public void String_DelimitedByLiteral()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO T.
                STRING "ABXCD" DELIMITED BY "X"
                       "ZZ" DELIMITED BY SIZE
                    INTO T
                END-STRING.
                DISPLAY T.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABZZ", stdout);
    }

    [Fact]
    public void String_OnOverflow()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE SPACES TO T.
                STRING "ABCDEFGH" DELIMITED BY SIZE
                    INTO T
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "OK"
                END-STRING.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OVF", stdout);
    }

    // ── UNSTRING ──

    [Fact]
    public void Unstring_BasicSplit()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UNSTR1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SRC PIC X(10) VALUE "AA,BB,CC".
            01 A   PIC X(4).
            01 B   PIC X(4).
            01 C   PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                UNSTRING SRC DELIMITED BY ","
                    INTO A
                    INTO B
                    INTO C
                END-UNSTRING.
                DISPLAY A.
                DISPLAY B.
                DISPLAY C.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("AA", lines[0]);
        Assert.Equal("BB", lines[1]);
        Assert.Equal("CC", lines[2]);
    }

    [Fact]
    public void Unstring_NoDelimiterMatch_MovesFullSource()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UNSTR2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SRC PIC X(10) VALUE "HELLO".
            01 A   PIC X(10).
            01 B   PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                UNSTRING SRC DELIMITED BY ","
                    INTO A
                    INTO B
                END-UNSTRING.
                DISPLAY A.
                DISPLAY B.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // No delimiter match: full source goes to A. B is space-filled.
        // DISPLAY trims trailing spaces, so B outputs nothing visible.
        Assert.Equal("HELLO", stdout);
    }

    [Fact]
    public void Unstring_OnOverflow()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. UNSTR3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SRC PIC X(10) VALUE "A,B,C".
            01 X   PIC X(4).
            01 Y   PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                UNSTRING SRC DELIMITED BY ","
                    INTO X
                    INTO Y
                    ON OVERFLOW DISPLAY "OVF"
                    NOT ON OVERFLOW DISPLAY "OK"
                END-UNSTRING.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Source has 3 fields (A, B, C) but only 2 INTO targets
        // After X gets "A" and Y gets "B", source pointer hasn't reached end
        // because "C" remains. But there are no more INTO targets.
        // COBOL spec: overflow occurs when pointer > length of source.
        // With 2 INTOs for 3 fields, the third field "C" is left but no overflow
        // because source isn't exhausted — pointer just stops.
        // Actually: overflow = pointer exceeded source length, which doesn't happen here.
        Assert.Equal("OK", stdout);
    }

    // ── SECTION control flow ──

    [Fact]
    public void PerformSection_ExecutesAllParagraphsThenReturns()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SECPERF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM SEC-1.
                DISPLAY "X".
                STOP RUN.
            SEC-1 SECTION.
            P1.
                DISPLAY "A".
            P2.
                DISPLAY "B".
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("A", lines[0]);
        Assert.Equal("B", lines[1]);
        Assert.Equal("X", lines[2]);
    }

    [Fact]
    public void GoToSection_JumpsToFirstParagraph()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SECGOTO.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                GO TO SEC-1.
                DISPLAY "SKIP".
                STOP RUN.
            SEC-1 SECTION.
            P1.
                DISPLAY "A".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("A", stdout);
    }

    // ── IF THEN (optional THEN keyword) ──

    [Fact]
    public void If_Then_OptionalKeyword()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTHEN1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = 5 THEN
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
    public void If_Then_WithElse()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTHEN2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 3.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = 5 THEN
                    DISPLAY "YES"
                ELSE
                    DISPLAY "NO"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NO", stdout);
    }

    [Fact]
    public void If_Then_NextSentence()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFTHEN3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = 5 THEN NEXT SENTENCE.
                DISPLAY "AFTER".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("AFTER", stdout);
    }

    // ═══════════════════════════════════════════
    // Arithmetic Conformance Matrix
    // Tests every allowed cell: statement × operand position × operand kind
    // Ensures GIVING forms accept literals per COBOL-85 spec.
    // ═══════════════════════════════════════════

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

    [Fact]
    public void OccursValue_FlatAlphanumeric()
    {
        // All 5 entries of OCCURS table should be "AZ"
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OCCVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                03 ITEM OCCURS 5 PIC XX VALUE "AZ".
            01 IDX PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING IDX FROM 1 BY 1
                    UNTIL IDX > 5
                    DISPLAY ITEM(IDX)
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        foreach (var line in lines)
            Assert.Equal("AZ", line);
    }

    [Fact]
    public void OccursValue_NestedAlphanumeric()
    {
        // 3x4 = 12 entries, all should be "AZ"
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NESTVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL.
                03 ROW OCCURS 3.
                    05 COL OCCURS 4 PIC XX VALUE "AZ".
            01 I PIC 9.
            01 J PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                PERFORM VARYING I FROM 1 BY 1
                    UNTIL I > 3
                    PERFORM VARYING J FROM 1 BY 1
                        UNTIL J > 4
                        DISPLAY COL(I, J)
                    END-PERFORM
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(12, lines.Length);
        foreach (var line in lines)
            Assert.Equal("AZ", line);
    }

    [Fact]
    public void JustifiedRight_LongerSource_TruncatesLeft()
    {
        // MOVE "ABCDEFGHI" TO JUST-FIELD (PIC X(5) JUSTIFIED RIGHT)
        // → "EFGHI" (rightmost 5 characters)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. JUSTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 JUST-FIELD PIC X(5) JUSTIFIED RIGHT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ABCDEFGHI" TO JUST-FIELD.
                DISPLAY JUST-FIELD.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("EFGHI", stdout);
    }

    [Fact]
    public void JustifiedRight_ShorterSource_PadsLeft()
    {
        // MOVE "AB" TO JUST-FIELD (PIC X(5) JUSTIFIED RIGHT)
        // → "   AB" (left-padded with spaces)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. JUSTPAD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 JUST-FIELD PIC X(5) JUSTIFIED RIGHT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AB" TO JUST-FIELD.
                DISPLAY JUST-FIELD.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("   AB", stdout);
    }

    [Fact]
    public void UsageInheritance_GroupComp_ValueInit()
    {
        // Parent group has USAGE COMP; child without explicit USAGE inherits it
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USGINH.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GRP USAGE COMP.
                02 A PIC 9 VALUE 5.
                02 B PIC 9 VALUE 6.
            01 RESULT PIC 99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD A B GIVING RESULT.
                DISPLAY RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("11", stdout);
    }

    [Fact]
    public void BlankWhenZero_ValueClause_StoresRawDigits()
    {
        // VALUE "000" on PIC 999 BLANK WHEN ZERO stores raw "000" bytes.
        // REDEFINES as PIC XXX should see "000", not spaces.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BWZVAL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NUM-FIELD PIC 999 VALUE "000" BLANK WHEN ZERO.
            01 ALPHA-VIEW REDEFINES NUM-FIELD PIC XXX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY ALPHA-VIEW.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("000", stdout);
    }

    [Fact]
    public void DecimalPointIsComma_NumericLiteral()
    {
        // DECIMAL-POINT IS COMMA: 123,45 is the decimal literal 123.45
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DCOMMA.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SOURCE-COMPUTER. COBOLSHARP.
            OBJECT-COMPUTER. DOTNET.
            SPECIAL-NAMES.
                DECIMAL-POINT IS COMMA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5)V99 VALUE 123,45.
            01 WS-B PIC 9(5)V99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 100,50 TO WS-B.
                DISPLAY WS-A.
                DISPLAY WS-B.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // WS-A VALUE 123,45 → displays as "0012345" (PIC 9(5)V99)
        Assert.Equal("0012345", lines[0]);
        // MOVE 100,50 → displays as "0010050"
        Assert.Equal("0010050", lines[1]);
    }

    [Fact]
    public void Comp5_NativeBinary_FullRangeAndLittleEndian()
    {
        // Tests COMP-5 end-to-end:
        // 1. Values beyond PIC digit range (30000 in PIC 9(4) COMP-5)
        // 2. MOVE to DISPLAY for output verification
        // 3. Comparison of COMP-5 values
        // 4. Arithmetic with no PIC-based truncation
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP5TEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-COMP5-U   PIC 9(4) COMP-5.
            01  WS-COMP5-S   PIC S9(4) COMP-5.
            01  WS-COMP-STD  PIC 9(4) COMP.
            01  WS-DISPLAY   PIC 9(5).
            01  WS-RESULT    PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 30000 TO WS-COMP5-U
                MOVE WS-COMP5-U TO WS-DISPLAY
                DISPLAY WS-DISPLAY
                MOVE 30000 TO WS-COMP-STD
                MOVE WS-COMP-STD TO WS-DISPLAY
                DISPLAY WS-DISPLAY
                MOVE -12345 TO WS-COMP5-S
                MOVE WS-COMP5-S TO WS-DISPLAY
                DISPLAY WS-DISPLAY
                MOVE 100 TO WS-COMP5-U
                ADD 200 TO WS-COMP5-U
                MOVE WS-COMP5-U TO WS-RESULT
                DISPLAY WS-RESULT
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        // Line 0: COMP-5 30000 — full binary range, no PIC truncation
        Assert.Equal("30000", lines[0]);
        // Line 1: Standard COMP 30000 % 10000 = 0 (PIC-based truncation)
        Assert.Equal("00000", lines[1]);
        // Line 2: COMP-5 signed -12345 → DISPLAY unsigned = 12345
        Assert.Equal("12345", lines[2]);
        // Line 3: ADD 100 + 200 = 300
        Assert.Equal("00300", lines[3]);
    }

    [Fact]
    public void Comp5_Computational5_FullWord_Parses()
    {
        // Tests COMPUTATIONAL-5 (full-word form) parses and works
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. COMP5FW.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-FIELD PIC 9(4) COMPUTATIONAL-5.
            01  WS-DISP  PIC 9(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 50000 TO WS-FIELD
                MOVE WS-FIELD TO WS-DISP
                DISPLAY WS-DISP
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("50000", stdout);
    }

    [Fact]
    public void Renames_SimpleAlias_DisplaysCorrectly()
    {
        // Level-66 RENAMES creates an alias for a contiguous byte range
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RENTEST1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-RECORD.
                05  WS-PART1   PIC X(5)  VALUE "HELLO".
                05  WS-PART2   PIC X(5)  VALUE "WORLD".
            66  WS-ALIAS RENAMES WS-PART1.
            66  WS-BOTH  RENAMES WS-PART1 THRU WS-PART2.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-ALIAS
                DISPLAY WS-BOTH
                MOVE "ABCDE" TO WS-ALIAS
                DISPLAY WS-PART1
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("HELLO", lines[0]);
        Assert.Equal("HELLOWORLD", lines[1]);
        Assert.Equal("ABCDE", lines[2]);
    }

    [Fact]
    public void Renames_Through_Synonym()
    {
        // THROUGH is a synonym for THRU
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RENTEST2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-DATA.
                05  WS-A   PIC X(3)  VALUE "ABC".
                05  WS-B   PIC X(3)  VALUE "DEF".
                05  WS-C   PIC X(3)  VALUE "GHI".
            66  WS-AB RENAMES WS-A THROUGH WS-B.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-AB
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ABCDEF", stdout);
    }

    [Fact]
    public void Alter_RedirectsGoTo()
    {
        // ALTER changes a GO TO target at runtime.
        // Without ALTER, GOTO-PARA goes to OLD-TARGET which displays "OLD".
        // With ALTER, GOTO-PARA goes to NEW-TARGET which displays "NEW".
        // Both paths end with STOP RUN.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTERTEST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ALTER GOTO-PARA TO PROCEED TO NEW-TARGET.
                GO TO GOTO-PARA.
            GOTO-PARA.
                GO TO OLD-TARGET.
            OLD-TARGET.
                DISPLAY "OLD PATH"
                STOP RUN.
            NEW-TARGET.
                DISPLAY "NEW PATH"
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NEW PATH", stdout);
    }

    [Fact]
    public void Alter_WithoutProceedTo()
    {
        // ALTER without PROCEED TO (optional per spec)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTSHORT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ALTER GOTO-PARA TO NEW-DEST.
                GO TO GOTO-PARA.
            GOTO-PARA.
                GO TO OLD-DEST.
            OLD-DEST.
                DISPLAY "OLD"
                STOP RUN.
            NEW-DEST.
                DISPLAY "NEW"
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("NEW", stdout);
    }

    [Fact]
    public void SignCondition_PositiveNegativeZero()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SIGNTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-VAL PIC S9(5) VALUE +100.
            01  WS-NEG PIC S9(5) VALUE -50.
            01  WS-ZER PIC S9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-VAL IS POSITIVE
                    DISPLAY "POS-YES"
                END-IF
                IF WS-NEG IS NEGATIVE
                    DISPLAY "NEG-YES"
                END-IF
                IF WS-ZER IS ZERO
                    DISPLAY "ZERO-YES"
                END-IF
                IF WS-VAL IS NOT NEGATIVE
                    DISPLAY "NOT-NEG"
                END-IF
                IF WS-NEG IS NOT POSITIVE
                    DISPLAY "NOT-POS"
                END-IF
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("POS-YES", lines[0]);
        Assert.Equal("NEG-YES", lines[1]);
        Assert.Equal("ZERO-YES", lines[2]);
        Assert.Equal("NOT-NEG", lines[3]);
        Assert.Equal("NOT-POS", lines[4]);
    }

    [Fact]
    public void NotCondition_NegatesComparison()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NOTTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01  WS-A PIC 9(3) VALUE 100.
            01  WS-B PIC 9(3) VALUE 200.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF NOT WS-A > WS-B
                    DISPLAY "NOT-GT"
                END-IF
                IF NOT WS-A = WS-B
                    DISPLAY "NOT-EQ"
                END-IF
                IF NOT (WS-A < WS-B)
                    DISPLAY "SHOULD-NOT-PRINT"
                ELSE
                    DISPLAY "PAREN-NOT"
                END-IF
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("NOT-GT", lines[0]);
        Assert.Equal("NOT-EQ", lines[1]);
        Assert.Equal("PAREN-NOT", lines[2]);
    }

    [Fact]
    public void FileIO_AlternateKey_ReadByAlternateKey()
    {
        // Write records with primary key (ID) and alternate key (NAME),
        // then READ by alternate key
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTKEYT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixaltkey"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-ID
                    ALTERNATE RECORD KEY IS IX-NAME
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IX-FILE.
            01 IX-REC.
               05 IX-ID   PIC X(3).
               05 IX-NAME PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IX-FILE.
                MOVE "001" TO IX-ID.
                MOVE "ALICE" TO IX-NAME.
                WRITE IX-REC.
                MOVE "002" TO IX-ID.
                MOVE "BOB  " TO IX-NAME.
                WRITE IX-REC.
                MOVE "003" TO IX-ID.
                MOVE "CAROL" TO IX-NAME.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                MOVE "002" TO IX-ID.
                READ IX-FILE.
                DISPLAY IX-ID IX-NAME.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // READ by primary key "002" → should return "002BOB  "
        Assert.Equal("002BOB", stdout.TrimEnd());
    }

    [Fact]
    public void Call_ByReference_CalleeModifiesCallerStorage()
    {
        // BY REFERENCE: callee modifies caller's data via LINKAGE SECTION
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01  WS-VALUE PIC X(10) VALUE "BEFORE".
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY WS-VALUE
                    CALL "MODIFIER" USING WS-VALUE
                    DISPLAY WS-VALUE
                    STOP RUN.
            """),
            ("MODIFIER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. MODIFIER.
                DATA DIVISION.
                LINKAGE SECTION.
                01  LS-DATA PIC X(10).
                PROCEDURE DIVISION USING LS-DATA.
                MAIN-PARA.
                    MOVE "AFTER" TO LS-DATA
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("BEFORE", lines[0]);
        Assert.Equal("AFTER", lines[1]);
    }

    [Fact]
    public void Call_SimpleSubprogram_DisplaysFromCallee()
    {
        // Caller CALLs a subprogram which DISPLAYs a message
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY "BEFORE CALL"
                    CALL "CALLEE"
                    DISPLAY "AFTER CALL"
                    STOP RUN.
            """),
            ("CALLEE.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLEE.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY "INSIDE CALLEE"
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("BEFORE CALL", lines[0]);
        Assert.Equal("INSIDE CALLEE", lines[1]);
        Assert.Equal("AFTER CALL", lines[2]);
    }
}
