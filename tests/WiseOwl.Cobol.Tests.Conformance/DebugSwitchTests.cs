// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The COBOL-85 object time switch, end to end through <see cref="RunUnit.DebugSwitchVariable"/> (kb/Work PB4997).
///
/// <para>§1985:XV.1.3.4: "An object time switch dynamically activates the debugging code inserted by the compiler. This
/// switch cannot be addressed in the program; it is controlled outside the COBOL environment. If the switch is on, all
/// the effects of the debugging language written in the source program are permitted. If the switch is off, all the
/// effects described in the USE FOR DEBUGGING statement on page XV-5 are inhibited. Recompilation of the source program
/// is not required to provide or take away this facility." So ONE compiled program, built WITH DEBUGGING MODE, is run
/// with the switch on and off. Its debugging section names the paragraph P2 and adds 1 to a counter each time it runs,
/// which is before each execution of P2 (§1985:XV.3.2.4 GR3a: "Immediately before each execution of the named
/// procedure"); P2 is performed twice, so the counter is 2 with the switch on and 0 with it off, and P2's own work is
/// done twice either way. The expected values come from those rules, never from a run.</para>
/// </summary>
public sealed class DebugSwitchTests
{
    private const string ProgramId = "DBGSW85";

    /// <summary>The one program every case runs: compiled once at --std 85 (the compiled-program cache keys it).</summary>
    private static string Compile(string dir)
    {
        string src = CompiledProgramCache.StageSource(Path.Combine(dir, ProgramId + ".cob"), $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {ProgramId}.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   SOURCE-COMPUTER. WISEOWL WITH DEBUGGING MODE.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 DBG-COUNT PIC 9 VALUE 0.
                   01 WORK-COUNT PIC 9 VALUE 0.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   DBG SECTION.
                       USE FOR DEBUGGING ON P2.
                   DBG-P.
                       ADD 1 TO DBG-COUNT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   P1.
                       PERFORM P2.
                       PERFORM P2.
                       DISPLAY "DEBUG=" DBG-COUNT " WORK=" WORK-COUNT.
                       STOP RUN.
                   P2.
                       ADD 1 TO WORK-COUNT.
            """);
        string dll = Path.Combine(dir, ProgramId + ".dll");
        var compiled = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 85, SourceFormat: TestSourceFormat.Inline));
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        return dll;
    }

    /// <summary>Run the program with <see cref="RunUnit.DebugSwitchVariable"/> set to <paramref name="value"/> (null =
    /// unset) and return whether it completed, its standard output and the runner's detail.</summary>
    private static (bool Ok, string Stdout, string Detail) RunWithSwitch(string? value)
    {
        string dir = ScratchDirectory.Create("dbgsw");
        try
        {
            string dll = Compile(dir);
            var (ok, stdout, detail) = CutRunner.Run(dll, dir, null,
                new Dictionary<string, string?> { [RunUnit.DebugSwitchVariable] = value });
            return (ok, stdout, detail);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void SwitchUnset_IsOn_TheDebuggingSectionRuns()
    {
        var (ok, stdout, detail) = RunWithSwitch(null);
        Assert.True(ok, detail);
        Assert.Equal("DEBUG=2 WORK=2", stdout);
    }

    [Theory]
    [InlineData("ON")]
    [InlineData("on")]
    [InlineData(" On ")]
    public void SwitchOn_TheDebuggingSectionRuns(string value)
    {
        var (ok, stdout, detail) = RunWithSwitch(value);
        Assert.True(ok, detail);
        Assert.Equal("DEBUG=2 WORK=2", stdout);
    }

    /// <summary>The switch taken away from the same compiled program: every effect of the USE FOR DEBUGGING statement is
    /// inhibited, and the program's own work is not.</summary>
    [Theory]
    [InlineData("OFF")]
    [InlineData("off")]
    public void SwitchOff_InhibitsTheDebuggingSection_WithNoRecompilation(string value)
    {
        var (ok, stdout, detail) = RunWithSwitch(value);
        Assert.True(ok, detail);
        Assert.Equal("DEBUG=0 WORK=2", stdout);
    }

    /// <summary>⛔ A value the runtime cannot read stops the run, naming the variable and the value: read as on or as off, a
    /// mistyped setting would run the program the user did not ask for with no message (kb/Work PB5839's rule).</summary>
    [Fact]
    public void SwitchUnreadable_StopsTheRun_NamingTheVariable()
    {
        var (ok, stdout, detail) = RunWithSwitch("disabled");
        Assert.False(ok, $"an unreadable switch ran to completion, printing [{stdout}]");
        Assert.Contains("WISEOWL_COBOL_DEBUG='disabled'", detail, StringComparison.Ordinal);
    }
}
