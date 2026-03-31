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

    protected (bool success, string stdout, string stderr) CompileAndRun(
        string cobolSource,
        CobolSharp.Compiler.Semantics.DialectMode dialect = CobolSharp.Compiler.Semantics.DialectMode.Default)
    {
        // Write source to temp file
        string sourcePath = Path.Combine(_tempDir, "test.cob");
        string outputPath = Path.Combine(_tempDir, "test.dll");
        File.WriteAllText(sourcePath, cobolSource);

        // Compile
        var compilation = new Compilation();
        if (dialect != CobolSharp.Compiler.Semantics.DialectMode.Default)
            compilation.Options.Dialect = dialect;
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
            return (false, partialOut.TrimEnd(), "Process timed out after 15s");
        }
        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;

        return (process.ExitCode == 0, stdout.TrimEnd(), stderr.TrimEnd());
    }

    /// <summary>
    /// Compile multiple COBOL programs and run the first one.
    /// Each entry is (filename, source). The first program is the main program.
    /// All programs are compiled to the same temp directory so they can find each other.
    /// </summary>
    protected (bool success, string stdout, string stderr) CompileMultipleAndRun(
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
}
