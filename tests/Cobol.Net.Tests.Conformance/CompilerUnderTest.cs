// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet;                                          // CompilerDriver (the greenfield compiler)
using LegacyCompilation = CobolSharp.Compiler.Compilation;
using LegacyState = CobolSharp.Runtime.ProgramState;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// A compiler the differential harness can drive uniformly: compile a COBOL source string to a runnable assembly
/// and run it, returning its stdout. Two implementations — <see cref="LegacyCompiler"/> (the byte-engine oracle,
/// 364-NIST-green) and <see cref="CobolNetCompiler"/> (the greenfield typed-native compiler) — let a test assert
/// that COBOL.NET produces <b>byte-identical stdout to the legacy</b> for a program (COBOLNET_DESIGN §2 / §18 #7).
/// This is the verification backbone the G2 checkpoint demands ("binds and DISPLAYs its data byte-identically to
/// the legacy"); hand-typed expected strings would re-derive the very semantics under test (padding, scale,
/// edited/overpunch images).
/// </summary>
public interface ICompilerUnderTest
{
    /// <summary>A short label for assertion messages (e.g. <c>"legacy"</c> / <c>"cobolnet"</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Compile <paramref name="source"/> to a console assembly and run it in an isolated temp directory.
    /// Returns whether it compiled-and-ran cleanly, its stdout (newline-canonicalized to <c>\r\n</c> then
    /// trailing-trimmed — matching both engines' test bases so the two are compared apples-to-apples), and a
    /// human-readable detail string for a failure.
    /// </summary>
    (bool ok, string stdout, string detail) CompileAndRun(string source);
}

/// <summary>Shared process-run plumbing for the two compiler-under-test implementations.</summary>
internal static class CutRunner
{
    /// <summary>
    /// Canonicalize a compiled program's stdout to the <b>NIST acceptance basis</b> — exactly the guard's
    /// <c>normalize()</c> (<c>scripts/guard.sh</c>): drop CR, then strip trailing spaces <i>per line</i>. This is
    /// the criterion the legacy oracle's 364-NIST-green status was validated against, so comparing on this basis
    /// makes the legacy a sound differential oracle. It also neutralizes the legacy's one known DISPLAY
    /// non-conformance — it trims trailing spaces off alphanumeric operands (so <c>DISPLAY WS-X</c> of a
    /// <c>PIC X(10)</c> holding <c>"HI"</c> emits <c>"HI"</c>, not <c>"HI        "</c>), contradicting ISO
    /// §14.9.11.4 GR1/GR6 ("the content of each operand … the size … is the sum of the sizes of the operands").
    /// COBOL.NET emits the spec-correct full field; a single-/trailing-operand DISPLAY then matches the legacy once
    /// per-line trailing spaces are stripped. (A case that exposes <i>internal</i> trailing spaces — e.g.
    /// <c>DISPLAY WS-X "]"</c> — is pinned to the spec value instead, since the legacy is non-conforming there.)
    /// </summary>
    public static string Normalize(string s)
    {
        var lines = s.ReplaceLineEndings("\n").Split('\n').Select(line => line.TrimEnd(' '));
        return string.Join("\n", lines).TrimEnd('\n');
    }

    /// <summary>Run <c>dotnet &lt;dll&gt;</c> in <paramref name="workDir"/> and capture normalized stdout.
    /// <paramref name="stdinFile"/> (when given) is piped to the program's stdin — ACCEPT device input (ISO
    /// §14.9.1 F1); stdin is always redirected and closed so an over-reading ACCEPT sees EOF, never the console.</summary>
    public static (bool ok, string stdout, string detail) Run(string dllPath, string workDir, string? stdinFile = null,
        IReadOnlyDictionary<string, string>? env = null)
    {
        var (code, stdout, detail) = RunExit(dllPath, workDir, stdinFile, env);
        return (code == 0, stdout, detail);
    }

    /// <summary>Like <see cref="Run"/> but returns the NUMERIC process exit code (the run-unit termination status
    /// "passed to the operating system" — ISO §14.9.42.4 GR5 / §14.9.18.4 GR10), not just success. The exit-code
    /// tests assert the value; the golden harness only needs the <c>ok</c> bool. A timeout returns <c>-1</c>.</summary>
    public static (int exitCode, string stdout, string detail) RunExit(string dllPath, string workDir,
        string? stdinFile = null, IReadOnlyDictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"")
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (env is not null)
            foreach (var (k, v) in env) psi.Environment[k] = v;
        using var proc = Process.Start(psi)!;
        // ACCEPT device input (ISO §14.9.1 F1): pipe the NIST .dat to stdin (EOF when none) — guard.sh:135-138 parity.
        if (stdinFile is not null) proc.StandardInput.Write(File.ReadAllText(stdinFile));
        proc.StandardInput.Close();
        var outTask = proc.StandardOutput.ReadToEndAsync();
        var errTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(30000))
        {
            proc.Kill();
            proc.WaitForExit(2000);
            return (-1, Normalize(outTask.IsCompleted ? outTask.Result : ""), "process timed out after 30s");
        }
        return (proc.ExitCode, Normalize(outTask.Result), Normalize(errTask.Result));
    }

    /// <summary>Create a fresh isolated temp directory for one compile-and-run.</summary>
    public static string NewTempDir(string tag)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"CobolNet_Diff_{tag}_{Guid.NewGuid():N}"[..40]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Best-effort recursive delete of a temp directory.</summary>
    public static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
    }
}

/// <summary>
/// The greenfield COBOL.NET compiler under test: drives <see cref="CompilerDriver"/> (COBOL → typed-native C# →
/// Roslyn) into an isolated temp dir, then runs the produced assembly.
/// </summary>
/// <param name="dialectLevel">The targeted ISO edition (default 85 — the differential harness compiles at the
/// legacy oracle's edition; spec-pinned tests of post-85 features pass their own, e.g. 2023 for the wide tier).</param>
public sealed class CobolNetCompiler(int dialectLevel = 85) : ICompilerUnderTest
{
    public string Name => "cobolnet";

    public (bool ok, string stdout, string detail) CompileAndRun(string source)
    {
        string dir = CutRunner.NewTempDir("cn");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            File.WriteAllText(src, source);

            var result = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel));
            if (!result.Success)
                return (false, "", $"[cobolnet compile] {result.Status}: {string.Join("\n", result.Errors)}");

            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>Compile+run and return the NUMERIC process exit code — the STOP RUN / GOBACK termination status
    /// "passed to the operating system" (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10). A compile failure returns
    /// <c>(-1, "", detail)</c>.</summary>
    public (int exitCode, string stdout, string detail) CompileAndRunExit(string source)
    {
        string dir = CutRunner.NewTempDir("cn");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            File.WriteAllText(src, source);

            var result = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: dialectLevel));
            if (!result.Success)
                return (-1, "", $"[cobolnet compile] {result.Status}: {string.Join("\n", result.Errors)}");

            return CutRunner.RunExit(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}

/// <summary>
/// The legacy byte-engine compiler as the differential oracle: drives the in-process <c>CobolSharp.Compiler</c>
/// <see cref="LegacyCompilation"/> API (the same path <c>EndToEndTestBase</c> uses, 364-NIST-green) into an
/// isolated temp dir, then runs the produced assembly. Default (permissive) dialect, matching the legacy base.
/// </summary>
public sealed class LegacyCompiler : ICompilerUnderTest
{
    public string Name => "legacy";

    public (bool ok, string stdout, string detail) CompileAndRun(string source)
    {
        string dir = CutRunner.NewTempDir("lg");
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            string dll = Path.Combine(dir, "prog.dll");
            File.WriteAllText(src, source);

            var compilation = new LegacyCompilation();
            var result = compilation.Compile(src, dll);
            if (!result.Success)
                return (false, "", $"[legacy compile] {string.Join("\n", result.Diagnostics.Select(d => d.ToString()))}");

            // Deploy the legacy runtime next to the output (defensive — mirrors EndToEndTestBase's multi-program path).
            string runtime = typeof(LegacyState).Assembly.Location;
            string dest = Path.Combine(dir, Path.GetFileName(runtime));
            if (!File.Exists(dest)) File.Copy(runtime, dest);

            return CutRunner.Run(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
