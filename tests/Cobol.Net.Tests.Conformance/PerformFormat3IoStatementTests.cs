// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1154 / PB3353 — the file-I/O statements an exception-checking PERFORM may carry, on their ABNORMAL paths as
/// fully as on their success path (owner 2026-10-17: SORT and file I-O tests cover both). The compile-time bans
/// (COBOL1605-1617: a multi-operand CLOSE / DELETE FILE / OPEN, a file SORT, a MERGE in imperative-statement-1) are
/// pinned by <c>PerformFormat3Tests</c> and, for their positions, <c>ExceptionPerformBanPositionTests</c>; this class
/// pins the LEGAL single-operand statement beside each ban, because the ban exists to make the WHEN phrase's answer to
/// "which implicit statement failed" unambiguous (§14.9.6.3 SR3, §14.9.10.3 SR4, §14.9.27.3 SR3), and that answer is
/// only worth having if the single-operand form delivers it for every I-O status the statement can answer.
/// <para>Expected statuses are the standard's: OPEN INPUT on an absent non-OPTIONAL file is '35' (§9.1.13.6 item 5, an
/// EC-I-O-PERMANENT-ERROR), CLOSE of a file that is not open is '42' (§14.9.6.4 GR1 and §9.1.13.7 item 2, an
/// EC-I-O-LOGIC-ERROR), OPEN or DELETE FILE of a file already in an open mode is '41' (§9.1.13.7 item 1, an
/// EC-I-O-LOGIC-ERROR), and DELETE FILE of a file that is not available is a SUCCESSFUL '05' (§9.1.13.2 item 4 b —
/// §14.9.10.4 GR20: "no action takes place if the mass storage file does not exist").</para>
/// <para>The statuses this class does not reach — lock contention '61' / '62' under RETRY, '37', '39', an OPTIONAL
/// file's '05', AT END, INVALID KEY, and the SORT / MERGE statements around the PERFORM — are in the sibling
/// <c>PerformFormat3IoConflictAndSortTests</c> (branch review round 1, kb/Work PB1154).</para>
/// </summary>
public sealed class PerformFormat3IoStatementTests
{
    private static readonly CobolNetCompiler CobolNet = new(2023);

    /// <summary>One LINE SEQUENTIAL file F, optionally with a FILE STATUS item FS and a USE declarative, then
    /// <paramref name="proc"/>. The file's path is unique per call and the file is absent unless <paramref name="proc"/>
    /// creates it.</summary>
    private static string Prog(string name, string proc, bool fileStatus, string checking = ">>TURN EC-ALL CHECKING ON",
        string? useBody = null)
    {
        string path = FileHarness.Tmp(name);
        string status = fileStatus ? " FILE STATUS FS" : "";
        string mainSection = useBody is null ? "" : "MAIN-SECTION SECTION.";   // §14.4.1: declaratives admit only sections
        string declaratives = useBody is null ? "" : $"""
            DECLARATIVES.
            IO-SECTION SECTION.
                USE AFTER STANDARD EXCEPTION PROCEDURE ON F.
            IO-PARA.
                {useBody}
            END DECLARATIVES.
            """;
        return $"""
            {checking}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {name}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "{path}" ORGANIZATION LINE SEQUENTIAL{status}.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 R PIC X(4).
            WORKING-STORAGE SECTION.
            01 FS PIC XX VALUE "??".
            PROCEDURE DIVISION.
            {declaratives}
            {mainSection}
            MAIN-PARA.
            {proc}
                DISPLAY "DONE"
                STOP RUN.
            """;
    }

    private static void AssertRuns(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    private static void AssertFatal(string source, string ecName, string expectedStdout)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean:\n{stdout}");
        Assert.Contains(ecName, detail);
        Assert.Equal(expectedStdout, stdout);
    }

    // ── SUCCESS: the single-operand statements complete, no WHEN phrase runs, the status is '00' ──────────────────

    [Fact]   // OPEN OUTPUT, WRITE and CLOSE of a single file in imperative-statement-1: legal, and nothing is raised.
    public void SingleFileOpenWriteClose_Success_RunsNoHandler() => AssertRuns(Prog("F3IOOK", """
            PERFORM
                OPEN OUTPUT F
                WRITE R FROM "AAAA"
                CLOSE F
            WHEN EC-I-O
                DISPLAY "HANDLER"
            END-PERFORM
            DISPLAY FS
        """, fileStatus: true), "00\nDONE");

    // ── OPEN INPUT of an absent file: '35', EC-I-O-PERMANENT-ERROR (§9.1.13.6 item 5) ─────────────────────────

    [Fact]   // a WHEN naming the level-3 exception-name is selected, sees the status, and RESUME NEXT STATEMENT continues
    public void OpenInputAbsentFile_WhenExceptionName_RunsHandler_WithStatus35() => AssertRuns(Prog("F3IO35N", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "HANDLER " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true), "HANDLER 35\nAFTER\nDONE");

    [Fact]   // GR3a file scope: WHEN EXCEPTION file-name matches any EC-I-O associated with that file
    public void OpenInputAbsentFile_WhenBareFile_RunsHandler() => AssertRuns(Prog("F3IO35F", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EXCEPTION F
                DISPLAY "BY-FILE " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true), "BY-FILE 35\nAFTER\nDONE");

    [Fact]   // §14.9.28.2 Format 3: exception-name-2 FILE file-name-2 (SR16: an EC-I-O name) pairs the name with the file
    public void OpenInputAbsentFile_WhenExceptionFile_PairedName_RunsHandler() => AssertRuns(Prog("F3IO35P", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-I-O-PERMANENT-ERROR FILE F
                DISPLAY "PAIRED " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true), "PAIRED 35\nAFTER\nDONE");

    [Fact]   // the same condition with NO FILE STATUS item: the WHEN phrase still runs (the status is the file's own)
    public void OpenInputAbsentFile_WithoutFileStatus_WhenStillRuns() => AssertRuns(Prog("F3IO35W", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "HANDLER"
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: false), "HANDLER\nAFTER\nDONE");

    [Fact]   // checking ON, no WHEN matches, no FILE STATUS and no USE: EC-I-O-PERMANENT-ERROR is fatal (§14.6.13.1)
    public void OpenInputAbsentFile_NoMatchingWhen_NoStatus_NoUse_IsFatal() => AssertFatal(Prog("F3IO35X", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, fileStatus: false), "EC-I-O-PERMANENT-ERROR", "");

    [Fact]   // checking OFF for EC-I-O: no exception condition is raised (§14.6.13.1.1); the status is simply set and
             // the program goes on. The WHEN phrase names an UNRELATED condition, so §14.9.28.4 GR14's implicit TURN
             // (the next test) enables nothing for the OPEN.
    public void OpenInputAbsentFile_CheckingOff_SetsStatusOnly() => AssertRuns(Prog("F3IO35Z", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER " FS
            WHEN EC-SIZE
                DISPLAY "HANDLER"
            END-PERFORM
        """, fileStatus: true, checking: ""), "AFTER 35\nDONE");

    [Fact]   // §14.9.28.4 GR14: a WHEN-named condition that no TURN enabled is enabled over imperative-statement-1, so
             // the SAME program with WHEN EC-I-O runs its handler although the program never turned checking on.
    public void OpenInputAbsentFile_CheckingOff_WhenNamesEcIo_Gr14EnablesIt() => AssertRuns(Prog("F3IO35G", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-I-O
                DISPLAY "HANDLER " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true, checking: ""), "HANDLER 35\nAFTER\nDONE");

    [Fact]   // §14.9.49.4 GR6: a condition no WHEN phrase matches reaches the USE declarative for the file
    public void OpenInputAbsentFile_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(Prog("F3IO35U", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, fileStatus: true, useBody: "DISPLAY \"USE \" FS RESUME NEXT STATEMENT."), "USE 35\nAFTER\nDONE");

    [Fact]   // §14.9.49.4 GR7 c: a FATAL EC-I-O whose declarative ends without RESUME is the implementor's action
             // (§9.1.13); WiseOwl COBOL terminates the run unit abnormally (§14.6.12), after the declarative has run.
    public void OpenInputAbsentFile_UnmatchedWhen_UseWithoutResume_FatalConditionTerminates() => AssertFatal(Prog("F3IO35T", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, fileStatus: true, useBody: "DISPLAY \"USE \" FS."), "EC-I-O-PERMANENT-ERROR", "USE 35");

    [Fact]   // ... and a condition a WHEN phrase DOES match is that phrase's, never the declarative's
    public void OpenInputAbsentFile_MatchedWhen_PreemptsUseDeclarative() => AssertRuns(Prog("F3IO35V", """
            PERFORM
                OPEN INPUT F
                DISPLAY "AFTER"
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "WHEN"
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true, useBody: "DISPLAY \"USE\" RESUME NEXT STATEMENT."), "WHEN\nAFTER\nDONE");

    // ── CLOSE of a file that is not open: '42', EC-I-O-LOGIC-ERROR (§14.9.6.4 GR1) ──────────────────────────

    [Fact]
    public void CloseUnopenedFile_WhenLogicError_RunsHandler_WithStatus42() => AssertRuns(Prog("F3IO42N", """
            PERFORM
                CLOSE F
                DISPLAY "AFTER"
            WHEN EC-I-O-LOGIC-ERROR
                DISPLAY "HANDLER " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true), "HANDLER 42\nAFTER\nDONE");

    [Fact]
    public void CloseUnopenedFile_NoMatchingWhen_NoStatus_NoUse_IsFatal() => AssertFatal(Prog("F3IO42X", """
            PERFORM
                CLOSE F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, fileStatus: false), "EC-I-O-LOGIC-ERROR", "");

    [Fact]
    public void CloseUnopenedFile_CheckingOff_SetsStatusOnly() => AssertRuns(Prog("F3IO42Z", """
            PERFORM
                CLOSE F
                DISPLAY "AFTER " FS
            WHEN EC-SIZE
                DISPLAY "HANDLER"
            END-PERFORM
        """, fileStatus: true, checking: ""), "AFTER 42\nDONE");

    // ── OPEN / DELETE FILE of a file already open: '41', EC-I-O-LOGIC-ERROR (§9.1.13.7 item 1) ───────────────

    [Fact]
    public void OpenAlreadyOpenFile_WhenLogicError_RunsHandler_WithStatus41() => AssertRuns(Prog("F3IO41O", """
            OPEN OUTPUT F
            PERFORM
                OPEN OUTPUT F
                DISPLAY "AFTER"
            WHEN EC-I-O-LOGIC-ERROR
                DISPLAY "HANDLER " FS
                RESUME NEXT STATEMENT
            END-PERFORM
            CLOSE F
        """, fileStatus: true), "HANDLER 41\nAFTER\nDONE");

    [Fact]
    public void DeleteFileOfOpenFile_WhenLogicError_RunsHandler_WithStatus41() => AssertRuns(Prog("F3IO41D", """
            OPEN OUTPUT F
            PERFORM
                DELETE FILE F
                DISPLAY "AFTER"
            WHEN EC-I-O-LOGIC-ERROR
                DISPLAY "HANDLER " FS
                RESUME NEXT STATEMENT
            END-PERFORM
            CLOSE F
        """, fileStatus: true), "HANDLER 41\nAFTER\nDONE");

    // ── DELETE FILE of an absent file: SUCCESSFUL, '05' — no WHEN phrase for an I-O error can run ─────────────

    [Fact]
    public void DeleteFileOfAbsentFile_IsSuccessful_Status05_RunsNoErrorHandler() => AssertRuns(Prog("F3IO05D", """
            PERFORM
                DELETE FILE F
                DISPLAY "AFTER " FS
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "PERM"
            WHEN EC-I-O-LOGIC-ERROR
                DISPLAY "LOGIC"
            END-PERFORM
        """, fileStatus: true), "AFTER 05\nDONE");

    [Fact]   // the nonzero successful status 05 is the EC-I-O-WARNING condition, raised only when it is enabled
    public void DeleteFileOfAbsentFile_WarningEnabled_WhenWarning_RunsHandler() => AssertRuns(Prog("F3IO05W", """
            PERFORM
                DELETE FILE F
                DISPLAY "AFTER"
            WHEN EC-I-O-WARNING
                DISPLAY "WARN " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """, fileStatus: true), "WARN 05\nAFTER\nDONE");
}
