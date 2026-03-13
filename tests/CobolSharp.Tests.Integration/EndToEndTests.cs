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
}
