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

    // ── --standard reaches the parser DialectLevel (Phase A regression net) ──────────────────────
    //
    // The CLI maps --standard <ver> → Options.Dialect → Options.Config.ParserLevel → parser.DialectLevel
    // (the same path the conformance harness drives). These end-to-end tests, through the real Program.Main,
    // lock that plumbing so a version-gated grammar feature is accepted ONLY from the standard that introduced
    // it — pinning each --standard to the correct parser level. They use grammar gates (parse-time {isYYYY()}?
    // predicates), so the result is purely the dialect threshold, independent of semantics/codegen.

    // GOBACK RETURNING (ISO §14.9.16) + PROCEDURE DIVISION RETURNING are gated {is2002()}?: rejected under
    // cobol85, accepted from cobol2002 onward. (Two units so the RETURNING item lives in a LINKAGE SECTION.)
    private const string Goback2002Source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           CALL ""GBSUB"" USING R RETURNING R.
           STOP RUN.
       END PROGRAM GBMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS PIC 9(4) VALUE 7.
       LINKAGE SECTION.
       01 LK-A PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-A RETURNING LK-R.
       P.
           GOBACK RETURNING WS.
       END PROGRAM GBSUB.
";

    // DELETE FILE (ISO §14.9.10) is gated {is2023()}?: rejected under cobol85/2002/2014, accepted from cobol2023.
    private const string DeleteFile2023Source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO ""cli-dialect-x.dat""
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           DELETE FILE F.
           STOP RUN.
       END PROGRAM DELF.
";

    [Theory]
    // GOBACK RETURNING — the 2002 boundary.
    [InlineData("cobol85", false)]
    [InlineData("cobol2002", true)]
    [InlineData("cobol2014", true)]
    [InlineData("cobol2023", true)]
    public void Cli_Standard_GatesGoback2002Feature(string standard, bool shouldCompile)
        => AssertDialectGate(Goback2002Source, standard, shouldCompile);

    [Theory]
    // DELETE FILE — the 2023 boundary.
    [InlineData("cobol85", false)]
    [InlineData("cobol2002", false)]
    [InlineData("cobol2014", false)]
    [InlineData("cobol2023", true)]
    public void Cli_Standard_GatesDeleteFile2023Feature(string standard, bool shouldCompile)
        => AssertDialectGate(DeleteFile2023Source, standard, shouldCompile);

    private static void AssertDialectGate(string source, string standard, bool shouldCompile)
    {
        string? cli = FindCliDll();
        if (cli == null) return; // CLI not built in this run — skip
        string tempDir = Path.Combine(Path.GetTempPath(), "cobolsharp_cli_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            string src = Path.Combine(tempDir, "DG.cob");
            File.WriteAllText(src, source);
            var (exit, stdout) = RunCli(cli, src, "-o", Path.Combine(tempDir, "DG.dll"), "--standard", standard);
            if (shouldCompile)
            {
                Assert.Equal(0, exit);
                Assert.Contains("Compiled successfully", stdout);
            }
            else
            {
                Assert.Equal(1, exit); // a normal diagnosed parse failure (the feature is not in this dialect)
            }
        }
        finally { try { Directory.Delete(tempDir, true); } catch { } }
    }
}
