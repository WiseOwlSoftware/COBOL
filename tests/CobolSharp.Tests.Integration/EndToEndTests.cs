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
                INSPECT WS-DATA REPLACING ALL "AA" BY "XX".
                DISPLAY WS-DATA.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("XXBBXXCCXX", stdout);
    }

    [Fact]
    public void CallStatement_EmitsDiagnostic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CALLTEST.
            PROCEDURE DIVISION.
                CALL "SUBPROG".
                DISPLAY "After call".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("After call", stdout);
        Assert.Contains("CALL not supported", stderr);
    }

    [Fact]
    public void FileIO_EmitsDiagnostic()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FILETEST.
            PROCEDURE DIVISION.
                DISPLAY "Before file ops".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Before file ops", stdout);
    }
}
