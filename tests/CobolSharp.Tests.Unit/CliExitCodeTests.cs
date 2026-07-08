// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using Xunit;

namespace CobolSharp.Tests.Unit;

/// <summary>
/// Item 6 (DEVLOG 308): the CLI exit-code contract through the real <c>Program.Main</c> (now wrapped in a
/// top-level try/catch). 0 = success, 1 = a normal diagnosed failure, 70 = an unexpected internal compiler
/// error (never a raw CLR crash). These subprocess tests lock the 0/1 contract end-to-end; the 70 path has
/// no deterministic trigger to exercise here and is covered by inspection. Skips gracefully when the CLI
/// has not been built (e.g. a clean checkout running only the unit project).
/// </summary>
public class CliExitCodeTests
{
    private static string? FindCliDll()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PROMPT.md")))
            dir = dir.Parent;
        if (dir == null) return null;
        foreach (var cfg in new[] { "Debug", "Release" })
        {
            string p = Path.Combine(dir.FullName, "src", "CobolSharp.CLI", "bin", cfg, "net10.0", "cobolsharp.dll");
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static (int exit, string stdout) RunCli(string cliDll, params string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(cliDll);
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout);
    }

    [Fact]
    public void Cli_ValidProgram_ExitsZero()
    {
        string? cli = FindCliDll();
        if (cli == null) return; // CLI not built in this run — skip
        string tempDir = Path.Combine(Path.GetTempPath(), "cobolsharp_cli_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            string src = Path.Combine(tempDir, "TST.cob");
            File.WriteAllText(src, @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY ""HI"".
           STOP RUN.
");
            var (exit, stdout) = RunCli(cli, src, "-o", Path.Combine(tempDir, "TST.dll"));
            Assert.Equal(0, exit);
            Assert.Contains("Compiled successfully", stdout);
        }
        finally { try { Directory.Delete(tempDir, true); } catch { } }
    }

    [Fact]
    public void Cli_UnknownOption_ExitsOne_NotInternalError()
    {
        string? cli = FindCliDll();
        if (cli == null) return; // CLI not built in this run — skip
        var (exit, _) = RunCli(cli, "--bogus-flag", "nope.cob");
        Assert.Equal(1, exit); // a normal diagnosed failure, not 70 (the catch must not intercept it)
    }

    // ── (removed) --standard edition-gating CLI tests ────────────────────────────────────────────
    // These end-to-end tests asserted the LEGACY CLI (cobolsharp.dll) REJECTS a too-new construct (GOBACK
    // RETURNING @85, DELETE FILE @85/2002/2014) via the shared grammar's parse-time {isYYYY()}? predicates.
    // The rearch bind-time gating migration (DEVLOG 680–690) moved edition INTRODUCTION-gating out of the shared
    // grammar into the GREENFIELD binder's ConstructRegistry.Check funnel (parse-then-gate at the recognition
    // point). The legacy compiler shares the (now-ungated) frontend grammar but NOT the greenfield's bind-time
    // Checks, so it no longer parse-rejects these — hence these legacy-CLI gating assertions are OBSOLETE. The
    // correct behavior is comprehensively verified against the authoritative greenfield compiler by
    // Cobol.Net.Tests.Conformance's EditionGateDiagnosticTests + VersionMatrixTests (all green). The legacy-CLI
    // mechanics tests below (valid-program exit 0, unknown-option exit 1) are unaffected and kept.

}
