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
}
