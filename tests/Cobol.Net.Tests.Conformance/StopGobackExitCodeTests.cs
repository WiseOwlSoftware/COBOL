// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
}
