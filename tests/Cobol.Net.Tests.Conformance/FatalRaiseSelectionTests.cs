// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The fatal default at an EMITTED raise site (kb/Work PB1549). ISO §14.6.13.1.3 5): with checking enabled and an
/// applicable USE statement, "If execution of the declarative completes normally the execution of the run unit is
/// terminated abnormally"; 7): with no handler, "If checking for the exception condition is enabled, execution of
/// the run unit is terminated abnormally as specified in 14.6.12". Only a RESUME continues (NOTE 2).
/// <para>The SET … TO ADDRESS OF PROGRAM / FUNCTION raise sites dispatched their fatal conditions and then CONTINUED
/// the run unit, while the CALL arm of the same EC-PROGRAM-NOT-FOUND terminated it. The decision now lives in ONE
/// place (<c>EcEmitter.EmitSelection</c>, fatality from Table 13 via <c>EcEmitter.EmitConditionRaise</c>). A corpus
/// golden must exit 0, so the abnormal arms — observable only as the exit code, the stderr surface and the stdout
/// that stops — are asserted here; the RESUME arms are golden
/// <c>2002/pb1549_set_program_address_fatal_resume</c>.</para>
/// </summary>
public sealed class FatalRaiseSelectionTests
{
    private static void AssertAbnormal(int exit, string stdout, string stderr, string expectedStdout, string ecName)
    {
        Assert.Equal(expectedStdout, stdout);                       // nothing after the failing statement runs
        Assert.Equal(1, exit);                                      // §14.6.12 — the abnormal-termination exit
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains(ecName, stderr);
    }

    /// <summary>§8.4.3.13.4 GR4 + §14.6.13.1.3 7): the PB1549 repro — no ON EXCEPTION, no declarative.</summary>
    [Fact]
    public void SetAddressOfProgram_NotFound_NoHandler_TerminatesAbnormally()
    {
        const string source = """
            >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1549T1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PP USAGE PROGRAM-POINTER.
            01 PQ USAGE PROGRAM-POINTER.
            PROCEDURE DIVISION.
            MAIN.
                SET PQ TO ADDRESS OF PROGRAM "PB1549T9"
                IF PQ NOT = NULL DISPLAY "PQ=SET" END-IF
                SET PP TO ADDRESS OF PROGRAM "PB1549ZZ"
                DISPLAY "AFTER-SET"
                STOP RUN.
            END PROGRAM PB1549T1.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1549T9.
            PROCEDURE DIVISION.
            MAIN.
                GOBACK.
            END PROGRAM PB1549T9.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "PQ=SET", "EC-PROGRAM-NOT-FOUND");
    }

    /// <summary>§14.6.13.1.3 5): the declarative runs, completes normally, and the run unit STILL terminates.</summary>
    [Fact]
    public void SetAddressOfProgram_NotFound_DeclarativeCompletesNormally_TerminatesAbnormally()
    {
        const string source = """
            >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1549T2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PP USAGE PROGRAM-POINTER.
            PROCEDURE DIVISION.
            DECLARATIVES.
            D-NF SECTION.
                USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
            D-NF-P.
                DISPLAY "NF-HANDLED".
            END DECLARATIVES.
            MAIN SECTION.
            M-P.
                SET PP TO ADDRESS OF PROGRAM "PB1549ZZ"
                DISPLAY "AFTER-SET"
                STOP RUN.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "NF-HANDLED", "EC-PROGRAM-NOT-FOUND");
    }

    /// <summary>§14.6.13.1.3 8): with checking NOT enabled the continuation is the implementor's; this
    /// implementation continues with the GR4-defined NULL value (the raise is not emitted — §14.6.13.1.4's
    /// "not raised" reading the pointer emitter has always used). Pins that the fix did not over-reach.</summary>
    [Fact]
    public void SetAddressOfProgram_NotFound_CheckingOff_Continues()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1549T3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PP USAGE PROGRAM-POINTER.
            PROCEDURE DIVISION.
            MAIN.
                SET PP TO ADDRESS OF PROGRAM "PB1549ZZ"
                IF PP = NULL DISPLAY "PP=NULL" END-IF
                STOP RUN.
            """;
        var (exit, stdout, _) = new CobolNetCompiler(2002).CompileAndRunExit(source);
        Assert.Equal("PP=NULL", stdout);
        Assert.Equal(0, exit);
    }

    private const string Functions = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. PB1549FD.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-ARG PIC S9(4).
        01 L-RES PIC S9(9).
        PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
            COMPUTE L-RES = L-ARG * 2
            GOBACK.
        END FUNCTION PB1549FD.
        IDENTIFICATION DIVISION.
        FUNCTION-ID. PB1549FZ.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-RES PIC S9(9).
        PROCEDURE DIVISION RETURNING L-RES.
            MOVE 0 TO L-RES
            GOBACK.
        END FUNCTION PB1549FZ.
        """;

    /// <summary>The ADDRESS OF FUNCTION twin — §8.4.3.12.4 GR4 raises the fatal EC-FUNCTION-NOT-FOUND (Table 13);
    /// unhandled, §14.6.13.1.3 7) terminates the run unit.</summary>
    [Fact]
    public void SetAddressOfFunction_NotFound_NoHandler_TerminatesAbnormally()
    {
        const string source = Functions + "\n" + """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1549T4.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION PB1549FD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FPD USAGE FUNCTION-POINTER TO PB1549FD.
            01 WS-NAME PIC X(12) VALUE "NOSUCHFN".
            PROCEDURE DIVISION.
            MAIN.
                >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON
                DISPLAY "BEFORE"
                SET FPD TO ADDRESS OF FUNCTION WS-NAME
                DISPLAY "AFTER-SET"
                STOP RUN.
            END PROGRAM PB1549T4.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "BEFORE", "EC-FUNCTION-NOT-FOUND");
    }

    /// <summary>§14.9.39.4 GR14 raises the fatal EC-FUNCTION-PTR-INVALID when the located function's signature
    /// differs; unhandled, §14.6.13.1.3 7) terminates the run unit.</summary>
    [Fact]
    public void SetAddressOfFunction_SignatureMismatch_NoHandler_TerminatesAbnormally()
    {
        const string source = Functions + "\n" + """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1549T5.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION PB1549FD
                FUNCTION PB1549FZ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FPD USAGE FUNCTION-POINTER TO PB1549FD.
            01 WS-NAME PIC X(12) VALUE "PB1549FZ".
            PROCEDURE DIVISION.
            MAIN.
                >>TURN EC-FUNCTION-PTR-INVALID CHECKING ON
                DISPLAY "BEFORE"
                SET FPD TO ADDRESS OF FUNCTION WS-NAME
                DISPLAY "AFTER-SET"
                STOP RUN.
            END PROGRAM PB1549T5.
            """;
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "BEFORE", "EC-FUNCTION-PTR-INVALID");
    }
}
