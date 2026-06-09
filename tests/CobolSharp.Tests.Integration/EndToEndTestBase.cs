using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class EndToEndTestBase : IDisposable
{
    protected readonly string _tempDir;

    public EndToEndTestBase()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CobolSharp_Tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>
    /// Canonicalize a compiled program's captured output to CRLF, then trim the trailing newline. The expected
    /// strings in these tests are written with <c>\r\n</c>, but a compiled program's <c>DISPLAY</c> uses
    /// <c>Console.WriteLine</c> = the platform newline (<c>\r\n</c> on Windows, <c>\n</c> on Linux/CI). Without
    /// this, every multi-line-output assertion would pass on Windows and fail on Linux. Normalizing on the
    /// comparison side (here) keeps the legacy engine — the differential oracle being retired at G8 — untouched;
    /// the COBOL.NET deliverable instead sets <c>Console.Out.NewLine</c> in its generated program for
    /// deterministic output.
    /// </summary>
    private static string NormalizeOutput(string s) => s.ReplaceLineEndings("\r\n").TrimEnd();

    protected (bool success, string stdout, string stderr) CompileAndRun(
        string cobolSource,
        CobolSharp.Compiler.Semantics.DialectMode dialect = CobolSharp.Compiler.Semantics.DialectMode.Default,
        bool enableTypedFields = false)
    {
        // Write source to temp file
        string sourcePath = Path.Combine(_tempDir, "test.cob");
        string outputPath = Path.Combine(_tempDir, "test.dll");
        File.WriteAllText(sourcePath, cobolSource);

        // Compile
        var compilation = new Compilation();
        if (dialect != CobolSharp.Compiler.Semantics.DialectMode.Default)
            compilation.Options.Dialect = dialect;
        compilation.Options.EnableTypedFields = enableTypedFields;
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
        // Async reads + bounded wait + kill-on-timeout: a synchronous ReadToEnd() pair can deadlock on a full
        // pipe, and accessing ExitCode after a timed-out WaitForExit throws. A generous timeout keeps a slow
        // process under heavy parallel test load from being mistaken for a hang (the source of the transient
        // file-I/O guard flakes — FileIO_Start, ReadPrevious_AfterStartEqual; DEVLOG 352/355).
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            process.WaitForExit(2000);
            return (false, NormalizeOutput(stdoutTask.IsCompleted ? stdoutTask.Result : ""), "Process timed out after 30s");
        }
        return (process.ExitCode == 0, NormalizeOutput(stdoutTask.Result), NormalizeOutput(stderrTask.Result));
    }

    /// <summary>
    /// Compile a NIST test program and run it. Handles --nist preprocessing.
    /// Returns stdout output. The source file is read from tests/nist/programs/.
    /// </summary>
    protected (bool success, string stdout, string stderr) CompileNistAndRun(
        string testName, Dictionary<string, string>? envVars = null)
    {
        string nistDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..",
            "tests", "nist", "programs"));
        string sourcePath = Path.Combine(nistDir, testName + ".cob");
        string outputPath = Path.Combine(_tempDir, testName + ".dll");

        var compilation = new Compilation { NistTestName = testName };
        var result = compilation.Compile(sourcePath, outputPath);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            return (false, "", $"Compilation failed:\n{errors}");
        }

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

        if (envVars != null)
        {
            foreach (var (key, value) in envVars)
                psi.EnvironmentVariables[key] = value;
        }

        using var process = Process.Start(psi)!;
        // Use async reads to avoid deadlock when process hangs
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(15000);
        if (!exited)
        {
            process.Kill();
            process.WaitForExit(2000); // wait for kill to complete
            string partialOut = stdoutTask.IsCompleted ? stdoutTask.Result : "";
            return (false, NormalizeOutput(partialOut), "Process timed out after 15s");
        }
        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;

        return (process.ExitCode == 0, NormalizeOutput(stdout), NormalizeOutput(stderr));
    }

    /// <summary>
    /// Compile a NIST program under a given dialect (no <c>--nist</c> preprocessing, so dialect flagging fires)
    /// and return the compiler diagnostics rendered as strings (each contains its <c>CBL####</c> code). Used by
    /// the flagging-conformance harness (WS-FLAG) to assert that obsolete / non-conforming constructs are
    /// flagged under <c>--standard cobol85</c>. See docs/COBOL85_COMPLIANCE_PLAN.md §3.
    /// </summary>
    protected IReadOnlyList<string> CompileNistDiagnostics(
        string testName, CobolSharp.Compiler.Semantics.DialectMode dialect)
    {
        string nistDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..",
            "tests", "nist", "programs"));
        string sourcePath = Path.Combine(nistDir, testName + ".cob");
        string outputPath = Path.Combine(_tempDir, testName + ".dll");

        var compilation = new Compilation();
        compilation.Options.Dialect = dialect;
        var result = compilation.Compile(sourcePath, outputPath);
        return result.Diagnostics.Select(d => d.ToString()!).ToList();
    }

    /// <summary>
    /// Compile multiple COBOL programs and run the first one.
    /// Each entry is (filename, source). The first program is the main program.
    /// All programs are compiled to the same temp directory so they can find each other.
    /// </summary>
    protected (bool success, string stdout, string stderr) CompileMultipleAndRun(
        params (string fileName, string source)[] programs)
        => CompileMultipleAndRun(CobolSharp.Compiler.Semantics.DialectMode.Default, programs);

    /// <summary>
    /// Compile multiple programs under a given dialect and run the first. Used where a callee needs a
    /// version-gated construct (e.g. a COBOL-2002 <c>PROCEDURE DIVISION … RETURNING</c>).
    /// </summary>
    protected (bool success, string stdout, string stderr) CompileMultipleAndRun(
        CobolSharp.Compiler.Semantics.DialectMode dialect,
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
            if (dialect != CobolSharp.Compiler.Semantics.DialectMode.Default)
                compilation.Options.Dialect = dialect;
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
        // Async reads + bounded wait + kill-on-timeout: a synchronous ReadToEnd() pair can deadlock on a full
        // pipe, and accessing ExitCode after a timed-out WaitForExit throws. A generous timeout keeps a slow
        // process under heavy parallel test load from being mistaken for a hang (the source of the transient
        // file-I/O guard flakes — FileIO_Start, ReadPrevious_AfterStartEqual; DEVLOG 352/355).
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            process.WaitForExit(2000);
            return (false, NormalizeOutput(stdoutTask.IsCompleted ? stdoutTask.Result : ""), "Process timed out after 30s");
        }
        return (process.ExitCode == 0, NormalizeOutput(stdoutTask.Result), NormalizeOutput(stderrTask.Result));
    }
}
