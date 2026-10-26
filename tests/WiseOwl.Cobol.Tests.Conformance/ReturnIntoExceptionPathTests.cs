// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The exception paths of RETURN ... INTO that need no USE declarative or end in a terminated run unit (kb/Work PB722,
/// campaign PB3353: sort and file I/O tests cover the abnormal and exception paths as fully as the success paths). ISO
/// §14.9.34.4 GR1: a RETURN executed outside an output procedure being executed by a SORT/MERGE that references the
/// file sets the EC-FLOW-RETURN exception condition; GR3: a RETURN executed in the current output procedure after the
/// AT END phrase ran sets EC-SORT-MERGE-RETURN; both are Table 13 fatal, so with checking enabled and no handler
/// "execution of the run unit is terminated abnormally" (§14.6.13.1.3 7)) and with checking not enabled the
/// continuation is the implementor's (§14.6.13.1.3 8)). Then §14.9.34.4 GR5 b): "The implied MOVE statement does not occur if the
/// execution of the RETURN statement was unsuccessful", so the INTO receiver keeps its value in every arm. Golden
/// <c>2023/pb722_return_into_flow_ec</c> is the declarative (RESUME) twin; <c>2023/pb722_return_into_unsuccessful</c>
/// the AT END and item-identification witnesses.
/// </summary>
public sealed class ReturnIntoExceptionPathTests
{
    private const string Header = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB722T.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT SRTF ASSIGN TO "pb722t.tmp".
        DATA DIVISION.
        FILE SECTION.
        SD SRTF RECORD IS VARYING IN SIZE FROM 3 TO 20
               DEPENDING ON WS-SLEN.
        01 S-REC.
           05 S-KEY PIC X(3).
           05 S-DATA PIC X(17).
        WORKING-STORAGE SECTION.
        01 WS-SLEN PIC 9(4) VALUE 8.
        01 WS-J12 PIC X(12) JUSTIFIED RIGHT.
        01 TBL.
           05 TE PIC X(8) OCCURS 3 TIMES.
        01 IDX PIC 9 VALUE 5.
        """;

    private const string InputProcedure = """
        IN-ONE SECTION.
        IN-ONE-P.
            MOVE 8 TO WS-SLEN.
            MOVE "K01" TO S-KEY.
            MOVE "PQRST" TO S-DATA.
            RELEASE S-REC.
        """;

    /// <summary>The SORT whose output procedure runs <paramref name="outputBody"/>.</summary>
    private static string SortProgram(string turn, string declaratives, string outputBody) =>
        $"""
        {turn}
        {Header}
        PROCEDURE DIVISION.
        {declaratives}
        MAIN SECTION.
        MAIN-P.
            SORT SRTF ON ASCENDING KEY S-KEY
                INPUT PROCEDURE IS IN-ONE
                OUTPUT PROCEDURE IS OUT-P.
            DISPLAY "AFTER-SORT R=[" WS-J12 "]".
            STOP RUN.
        {InputProcedure}
        OUT-P SECTION.
        OUT-P-P.
        {outputBody}
        """;

    /// <summary>One record: RETURN 0 takes it, RETURN 1 finds the end, RETURN 2 (INTO) is the offender.</summary>
    private const string TwoReturnsAfterAtEnd = """
        RETURN SRTF
            AT END DISPLAY "AT-END-0"
        END-RETURN.
        RETURN SRTF
            AT END DISPLAY "AT-END-1"
        END-RETURN.
        MOVE "KEEP" TO WS-J12.
        DISPLAY "BEFORE-2".
        RETURN SRTF INTO WS-J12
            AT END DISPLAY "AT-END-2"
            NOT AT END DISPLAY "NOT-AT-END-2"
        END-RETURN.
        DISPLAY "AFTER-2 R=[" WS-J12 "]".
        """;

    /// <summary>The expected stdout: the lines, LF-joined; the actual stdout is compared with <see cref="Normalized"/>.</summary>
    private static string Lines(params string[] lines) => string.Join('\n', lines);

    /// <summary>The captured stdout with LF line endings and no trailing line end (a terminated run unit flushes none).</summary>
    private static string Normalized(string stdout) => stdout.ReplaceLineEndings("\n").TrimEnd('\n');

    private static void AssertAbnormal(int exit, string stdout, string stderr, string expectedStdout, string ecName)
    {
        Assert.Equal(expectedStdout, Normalized(stdout));                    // nothing after the failing RETURN runs
        Assert.Equal(1, exit);                                      // §14.6.12 — the abnormal-termination exit
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains(ecName, stderr);
    }

    /// <summary>GR3 + §14.6.13.1.3 7): a second RETURN INTO after AT END, checking ON, no declarative — the run unit
    /// terminates at the RETURN, before its INTO move (and before either phrase).</summary>
    [Fact]
    public void SortMergeReturn_AfterAtEnd_NoDeclarative_TerminatesAbnormally()
    {
        string source = SortProgram(">>TURN EC-SORT-MERGE-RETURN CHECKING ON", "", TwoReturnsAfterAtEnd);
        var (exit, stdout, stderr) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, Lines("AT-END-1", "BEFORE-2"), "EC-SORT-MERGE-RETURN");
    }

    /// <summary>§14.6.13.1.3 5): the declarative runs and completes normally, the run unit still terminates, and the
    /// receiver the declarative sees is untouched (the raise precedes the INTO move, GR5 b)).</summary>
    [Fact]
    public void SortMergeReturn_AfterAtEnd_DeclarativeCompletesNormally_TerminatesAbnormally()
    {
        const string declaratives = """
            DECLARATIVES.
            H SECTION.
                USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-RETURN.
            H-P.
                DISPLAY "HANDLED R=[" WS-J12 "]".
            END DECLARATIVES.
            """;
        string source = SortProgram(">>TURN EC-SORT-MERGE-RETURN CHECKING ON", declaratives, TwoReturnsAfterAtEnd);
        var (exit, stdout, stderr) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, Lines("AT-END-1", "BEFORE-2", "HANDLED R=[        KEEP]"),
            "EC-SORT-MERGE-RETURN");
    }

    /// <summary>§14.6.13.1.3 8) with the checking NOT enabled: no raise, the store reports the end again (CobolSort
    /// output-procedure state), the AT END phrase runs, the INTO move does not, and the run unit ends normally.</summary>
    [Fact]
    public void SortMergeReturn_AfterAtEnd_CheckingOff_ReportsAtEndAgainAndLeavesTheReceiver()
    {
        string source = SortProgram("", "", TwoReturnsAfterAtEnd);
        var (exit, stdout, _) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        Assert.Equal(Lines("AT-END-1", "BEFORE-2", "AT-END-2", "AFTER-2 R=[        KEEP]",
            "AFTER-SORT R=[        KEEP]"), Normalized(stdout));
        Assert.Equal(0, exit);
    }

    /// <summary>GR1 + §14.6.13.1.3 7): RETURN INTO executed with no SORT/MERGE active, checking ON, no declarative —
    /// the run unit terminates at the RETURN; neither phrase runs and the receiver is untouched.</summary>
    [Fact]
    public void FlowReturn_OutsideOutputProcedure_NoDeclarative_TerminatesAbnormally()
    {
        string source = $"""
            >>TURN EC-FLOW-RETURN CHECKING ON
            {Header}
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE "KEEP" TO WS-J12.
                DISPLAY "BEFORE".
                RETURN SRTF INTO WS-J12
                    AT END DISPLAY "AT-END"
                    NOT AT END DISPLAY "NOT-AT-END"
                END-RETURN.
                DISPLAY "AFTER R=[" WS-J12 "]".
                STOP RUN.
            """;
        var (exit, stdout, stderr) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, Lines("BEFORE"), "EC-FLOW-RETURN");
    }

    /// <summary>§14.9.34.4 GR5 b) with identifier-1 = TE (IDX), IDX out of bounds: item identification happens after
    /// the record has been read, so a SUCCESSFUL RETURN raises the fatal EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2) and with
    /// no declarative the run unit terminates.</summary>
    [Fact]
    public void ItemIdentification_BoundSubscript_NoDeclarative_TerminatesAbnormally()
    {
        string source = SortProgram(">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", "", """
            RETURN SRTF INTO TE (IDX)
                AT END DISPLAY "AT-END"
            END-RETURN.
            DISPLAY "RETURNED".
            """);
        var (exit, stdout, stderr) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        AssertAbnormal(exit, stdout, stderr, "", "EC-BOUND-SUBSCRIPT");
    }

    /// <summary>The negative twin: the RETURN that finds the end performs no item identification, so the same
    /// unhandled fatal condition is NOT raised and the run unit ends normally.</summary>
    [Fact]
    public void ItemIdentification_BoundSubscript_AtEnd_NoDeclarative_RaisesNothing()
    {
        string source = SortProgram(">>TURN EC-BOUND-SUBSCRIPT CHECKING ON", "", """
            RETURN SRTF
                AT END DISPLAY "AT-END-0"
            END-RETURN.
            RETURN SRTF
                AT END DISPLAY "AT-END-1"
            END-RETURN.
            RETURN SRTF INTO TE (IDX)
                AT END DISPLAY "AT-END-2"
            END-RETURN.
            DISPLAY "RETURNED".
            """);
        var (exit, stdout, _) = new WiseOwlCobolCompiler(2023).CompileAndRunExit(source);
        Assert.Equal(Lines("AT-END-1", "AT-END-2", "RETURNED", "AFTER-SORT R=[            ]"), Normalized(stdout));
        Assert.Equal(0, exit);
    }
}
