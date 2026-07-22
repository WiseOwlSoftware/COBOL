// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The STOP RUN / GOBACK termination-STATUS phrase → process exit-code wiring (ISO §14.9.42 STOP statement /
/// §14.9.18 GOBACK statement). §14.9.42.4 GR5 and §14.9.18.4 GR10 pass the status VALUE to the operating system;
/// GR2/GR3 (STOP) and GR7/GR8 (main-program GOBACK) select an error-vs-normal termination indication. On .NET the
/// single observable is the process exit code (<see cref="Runtime.RunUnit.ExitStatus"/> → <c>Environment.ExitCode</c>),
/// so this compiler's documented implementor mapping (Annex A required-behavior items 192/193; docs/CONFORMANCE.md
/// §4.2.16) collapses both into ONE integer: the STATUS value when present, else ERROR ⇒ 1 / NORMAL ⇒ 0.
/// These facts assert the NUMERIC exit code (the manifest golden harness only checks <c>ExitCode == 0</c> as a bool,
/// so the value is unobservable there). The below-edition rejection of the phrase is covered by the version matrix
/// (stop-run-status-2002 / goback-status-2023 → COBOLNET0900).
/// </summary>
public sealed class StopGobackExitCodeTests
{
    private static string Prog(string pid, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-CODE PIC 9(3) VALUE 13.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "ran".
            {proc}
        """;

    [Theory]
    // STOP RUN — §14.9.42.4 GR5 (value passed) wins over the ERROR/NORMAL indication (GR2/GR3).
    [InlineData("STOP RUN WITH ERROR STATUS 42.", 42)]     // GR5: the value is passed
    [InlineData("STOP RUN WITH ERROR.", 1)]                // GR2: no value → the error indication
    [InlineData("STOP RUN WITH NORMAL STATUS 7.", 7)]      // GR5: the value wins over NORMAL
    [InlineData("STOP RUN WITH NORMAL.", 0)]               // GR3: normal indication, no value
    [InlineData("STOP RUN WITH ERROR STATUS WS-CODE.", 13)]// GR5: a data-item status value
    [InlineData("STOP RUN.", 0)]                           // no status phrase — the default (regression lock)
    // main-program GOBACK — §14.9.18.4 GR3 ("operates as if executing a STOP statement") / GR10 (value passed).
    [InlineData("GOBACK WITH ERROR STATUS 5.", 5)]
    [InlineData("GOBACK WITH NORMAL STATUS 0.", 0)]
    [InlineData("GOBACK WITH ERROR.", 1)]
    [InlineData("GOBACK.", 0)]                             // no status phrase — the default
    public void TerminationStatus_SetsProcessExitCode(string proc, int expectedExit)
    {
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(Prog("SGEXIT", proc));
        Assert.Equal("ran", stdout);
        Assert.Equal(expectedExit, exit);
    }

    /// <summary>A GOBACK status phrase in a CALLED subprogram is INERT (ISO §14.9.18.4 GR2 returns to the
    /// activator; the STATUS/ERROR indication of GR3/GR7–GR10 applies "in a main program" only). The sub's
    /// <c>GOBACK WITH ERROR STATUS 9</c> must NOT set the exit code — the main resumes after the CALL and its
    /// plain STOP RUN leaves the exit code 0 (the <c>!__asCalled</c> emit guard).</summary>
    [Fact]
    public void CalledSubprogramGobackStatus_IsInert()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGMAIN.
            PROCEDURE DIVISION.
            MAIN.
                CALL "SGSUB".
                DISPLAY "resumed".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGSUB.
            PROCEDURE DIVISION.
            SUBMAIN.
                GOBACK WITH ERROR STATUS 9.
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("resumed", stdout);
        Assert.Equal(0, exit);
    }

    /// <summary>The in-group companion to the cross-assembly case below: a status-free MAIN CALLs a sub whose
    /// <c>STOP RUN … WITH STATUS</c> ends the whole run unit (ISO §14.9.42.4 GR6). STOP RUN passes its status
    /// regardless of whether it runs in the main or a called program (unlike GOBACK, GR2/GR3 — see
    /// <see cref="CalledSubprogramGobackStatus_IsInert"/>). Locks that the runtime-side flush (the
    /// <see cref="Runtime.RunUnit.ExitStatus"/> setter) — not a compile-time parse-tree scan — carries the status
    /// to the exit code (§14.9.42.4 GR5).</summary>
    [Fact]
    public void CalledSubprogram_StopRunWithStatus_SetsExitCode()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGSMAIN.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "ran".
                CALL "SGSSUB".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SGSSUB.
            PROCEDURE DIVISION.
            SUBP.
                STOP RUN WITH ERROR STATUS 24.
            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal("ran", stdout);   // the sub's STOP RUN ends the run unit before the main's own STOP RUN
        Assert.Equal(24, exit);
    }

    // ── V47 (§24 review ledger): STOP RUN … WITH STATUS in a SEPARATELY-COMPILED module crosses the boundary ──

    private static void CompileTo(string source, string dir, string name)
    {
        string src = Path.Combine(dir, name + ".cob");
        File.WriteAllText(src, source);
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: 2023));
        Assert.True(r.Success, $"compile {name}: {string.Join("; ", r.Errors)}");
    }

    /// <summary>V47 (§24 review ledger — CONFIRMED): STOP RUN terminates the WHOLE run unit from anywhere (ISO
    /// §14.9.42.4 GR6) and its STATUS is "passed to the operating system" (GR5). When the <c>STOP RUN … WITH
    /// STATUS</c> executes in a SEPARATELY-COMPILED CALLed module, the status is the RUN UNIT's, not the main
    /// program's — it must reach the process exit code even though the main program's own compilation group carries
    /// no status phrase (so a compile-time parse-tree scan of the main group can never see it). The exit-code flush
    /// is runtime-side (the <see cref="Runtime.RunUnit.ExitStatus"/> setter over the shared ambient run unit), so
    /// the sub's status crosses the assembly boundary. Regression lock for the pre-fix silent discard-to-0.</summary>
    [Fact]
    public void SeparatelyCompiledModule_StopRunWithStatus_CrossesAssemblyBoundary()
    {
        const string main = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V47MAIN.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "before".
                CALL "V47SUB".
                DISPLAY "unreached".
                STOP RUN.
            """;
        const string sub = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. V47SUB.
            PROCEDURE DIVISION.
            SUBP.
                STOP RUN WITH ERROR STATUS 16.
            """;
        string dir = CutRunner.NewTempDir("v47xasm");
        try
        {
            CompileTo(main, dir, "V47MAIN");
            CompileTo(sub, dir, "V47SUB");
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "V47MAIN.dll"), dir);
            Assert.Equal("before", stdout);   // the sub's STOP RUN ends the run unit — "unreached" never prints
            Assert.Equal(16, exit);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
