// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Compiler;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB75 — a size error raised OUTSIDE an arithmetic statement's own catch is the fatal exception condition
/// ISO §14.7.5's no-phrase rules name ("EC-SIZE-OVERFLOW … is set to exist, and processing proceeds as specified in
/// 14.6.13.1.3"), never a raw CLR crash. The golden <c>pb75_sdidi_overflow_outside_arithmetic</c> pins #4/#5 (USE /
/// PERFORM WHEN dispatch with RESUME); this class pins the two TERMINATING dispositions: #7 (checking enabled, no
/// handler resumes) and #8 (checking NOT enabled — the implementor's choice, documented as loud termination). Before
/// this landing every one of these was an unhandled <c>CobolSizeError</c> stack trace, exit 127.
/// </summary>
public sealed class SizeErrorDispositionTests
{
    private static string Prog(string turn, string body) => $$"""
        {{turn}}
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB75DISP.
               OPTIONS.
                   ARITHMETIC IS STANDARD-DECIMAL.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-X PIC 9(5).
               PROCEDURE DIVISION.
               MAIN-P.
                   DISPLAY "BEFORE".
                   {{body}}
                   DISPLAY "AFTER".
                   STOP RUN.
        """;

    private static (int Exit, string Stdout, string Stderr) Run(string source)
    {
        string dir = CutRunner.NewTempDir("pb75");
        try
        {
            string src = Path.Combine(dir, "prog.cob"), dll = Path.Combine(dir, "prog.dll");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 2023));
            Assert.True(r.Success, "[compile] " + string.Join("\n", r.Errors));
            return CutRunner.RunExit(dll, dir);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>§14.6.13.1.3 #8 — checking NOT enabled: the SDIDI range overflow in a CONDITION reaches the run-unit
    /// boundary and terminates loudly (this implementation's documented choice) — the abnormal-termination surface
    /// names the condition, exit 1; never a .NET stack trace / exit 127.</summary>
    [Theory]
    [InlineData("IF 10 ** 100000 > 5 DISPLAY \"GT\" ELSE DISPLAY \"LE\" END-IF.")]
    [InlineData("DISPLAY \"V=\" FUNCTION ABS(10 ** 100000).")]
    [InlineData("MOVE FUNCTION INTEGER-PART(10 ** 100000) TO WS-X.")]
    [InlineData("COMPUTE WS-X = 10 ** 100000.")]
    public void CheckingOff_TerminatesLoudly_NeverARawCrash(string body)
    {
        var (exit, stdout, stderr) = Run(Prog("", body));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal)", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
        Assert.DoesNotContain("   at ", stderr);   // no CLR stack trace
    }

    /// <summary>§14.6.13.1.3 #7 — checking enabled and nothing resumes: the guarded statement sets the status,
    /// finds no USE / WHEN, and the run unit terminates abnormally naming the condition.</summary>
    [Fact]
    public void CheckingOn_NoHandler_TerminatesAbnormally()
    {
        var (exit, stdout, stderr) = Run(Prog(">>TURN EC-SIZE-OVERFLOW CHECKING ON",
            "IF 10 ** 100000 > 5 DISPLAY \"GT\" ELSE DISPLAY \"LE\" END-IF."));
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.DoesNotContain("AFTER", stdout);
        Assert.Contains("EC-SIZE-OVERFLOW", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
    }

    /// <summary>ONE dispatch per raise (§14.6.13.1.3 #5 → #7): a fatal condition raised by a statement INSIDE a
    /// PERFORM is processed by that statement's guard — the USE declarative runs once — and, unresumed, terminates
    /// the run unit; the enclosing PERFORM's guard lets the already-dispatched condition pass. Before this landing
    /// every enclosing statement re-dispatched it: the declarative ran once per nesting level.</summary>
    [Fact]
    public void FatalRaiseInsideAPerform_IsDispatchedOnce_ThenTerminates()
    {
        string src = """
                  >>TURN EC-BOUND-REF-MOD CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB75ONCE.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC X(5) VALUE "HELLO".
                   01 Y PIC X(2).
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   H SECTION.
                       USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
                   H-P.
                       DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       DISPLAY "R1".
                       PERFORM 2 TIMES
                           MOVE X(9:1) TO Y
                           DISPLAY "IN-LOOP"
                       END-PERFORM.
                       DISPLAY "R2".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = Run(src);
        Assert.Equal(1, exit);
        Assert.Equal(1, stdout.Split("CAUGHT=").Length - 1);   // exactly ONE dispatch
        Assert.DoesNotContain("IN-LOOP", stdout);
        Assert.Contains("abnormal run-unit termination: EC-BOUND-REF-MOD (fatal)", stderr);
    }

    /// <summary>The native carrier's receiverless-lane raise (§8.8.1 alignment past the Int128 escape boundary —
    /// <c>CobolNum.RescaleEscape</c>, kb/Work PB69's "stays loud") is the same condition and takes the same
    /// disposition: a subscript expression whose alignment overflows terminates loudly, not with a stack trace.</summary>
    [Fact]
    public void NativeReceiverlessOverflow_TerminatesLoudly()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB75NAT.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 BIG PIC 9(31) VALUE 9999999999999999999999999999999.
                   01 T.
                      05 E PIC X OCCURS 3.
                   01 F PIC 9V9(9) VALUE 1.5.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       DISPLAY "BEFORE".
                       DISPLAY FUNCTION MAX(BIG F).
                       DISPLAY "AFTER".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = Run(src);
        Assert.Equal(1, exit);
        Assert.Contains("BEFORE", stdout);
        Assert.Contains("abnormal run-unit termination: EC-SIZE-OVERFLOW (fatal)", stderr);
        Assert.DoesNotContain("Unhandled exception", stderr);
    }
}
