// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB1154 / PB3353 (branch review round 1, R1-2) — the rest of the file-I/O abnormal paths of the legal
/// statements an exception-checking PERFORM may carry, beside <c>PerformFormat3IoStatementTests</c> (OPEN 35/41, CLOSE
/// 42, DELETE FILE 41/05): under a RETRY phrase, the file sharing conflict statuses 61 and 62 and the record lock
/// conflict 51 (the w1121c review, round 1, R1-2), the OPEN statuses 37, 39 and 05, the AT END and INVALID KEY conditions against their own phrases, and the SORT / MERGE statements that are legal around
/// the PERFORM (a table SORT in imperative-statement-1, a file SORT or MERGE in a WHEN phrase) on their success and
/// on their USING-file failure paths (owner 2026-10-17: SORT and file I-O tests cover both); a failure of the
/// sort-merge WORK file itself is <c>PerformFormat3SortWorkFileFailureTests</c>.
/// <para>Each status is delivered, per §9.1.12 and §14.9.28.4 GR17, to the FIRST of: a WHEN phrase naming the
/// exception, the USE declarative for the file (§14.9.49.4 GR6), the file's FILE STATUS item with nothing else
/// applicable. Which of the three runs is pinned for each status here, with the NONFATAL / FATAL split the exception
/// table (§14.6.13.1.6, the EC-I-O names) gives: 51 (EC-I-O-RECORD-OPERATION), 61 and 62 (EC-I-O-FILE-SHARING), 05 (EC-I-O-WARNING), 10 (EC-I-O-AT-END) and 23
/// (EC-I-O-INVALID-KEY) are NF and the run goes on; 37 and 39 (EC-I-O-PERMANENT-ERROR) are Fatal.</para>
/// <para>Expected statuses are the standard's: §9.1.13.8 item 1 ('51': a record another file connector has locked),
/// §9.1.13.9 item 1 e) ('61': OPEN OUTPUT of a physical file another file
/// connector has open) and item 2 ('62': DELETE FILE of a physical file another file connector has open), §9.1.13.6
/// items 6 and 7 ('37' insufficient authority or a file that will not support the write operation, '39' a fixed-
/// attribute conflict), §9.1.13.2 item 4 a) ('05': an OPTIONAL file absent), §9.1.13.5 ('23', an invalid key).</para>
/// <para>A RETRY phrase cannot clear a conflict whose holder is a connector of THIS run unit (kb/Work PB142, the
/// conformance case <c>pb142_retry_open_sharing</c>): every retry form answers the conflict status, which is what is
/// asserted after the statement.</para>
/// </summary>
public sealed class PerformFormat3IoConflictAndSortTests
{
    private static readonly WiseOwlCobolCompiler WiseOwlCobol = new(2023);

    private const string On = ">>TURN EC-ALL CHECKING ON";

    /// <summary>A free-form program from its parts. <paramref name="use"/> adds a USE AFTER STANDARD EXCEPTION
    /// declarative on one file whose body is the given statements (§14.4.1: with declaratives present every
    /// paragraph of the main part lives in a section).</summary>
    private static string Prog(string name, string select, string fd, string ws, string proc,
        string checking = On, (string File, string Body)? use = null, string paras = "")
    {
        string declaratives = use is not { } u ? "" : $"""
            DECLARATIVES.
            IO-SECTION SECTION.
                {(u.File.StartsWith("EC-", StringComparison.Ordinal) ? $"USE AFTER EXCEPTION CONDITION {u.File}." : $"USE AFTER STANDARD EXCEPTION PROCEDURE ON {u.File}.")}
            IO-PARA.
                {u.Body}
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            """;
        return $"""
            {checking}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {name}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
            {select}
            DATA DIVISION.
            FILE SECTION.
            {fd}
            WORKING-STORAGE SECTION.
            {ws}
            PROCEDURE DIVISION.
            {declaratives}
            MAIN-PARA.
            {proc}
                DISPLAY "DONE"
                STOP RUN.
            {paras}
            """;
    }

    private static void AssertRuns(string source, string expected)
    {
        var (ok, stdout, detail) = WiseOwlCobol.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    private static void AssertFatal(string source, string ecName, string expectedStdout)
    {
        var (ok, stdout, detail) = WiseOwlCobol.CompileAndRun(source);
        Assert.False(ok, $"expected abnormal termination on {ecName}; ran clean:\n{stdout}");
        Assert.Contains(ecName, detail);
        Assert.Equal(expectedStdout, stdout);
    }

    // ── lock contention: two connectors on ONE physical file (§9.1.13.9) ──────────────────────────────────────

    /// <summary>F holds the physical file open (OUTPUT); G is a second connector over the same path.</summary>
    private static string Share(string name, string proc, bool gStatus = true, string checking = On,
        (string File, string Body)? use = null)
    {
        string path = FileHarness.Tmp(name);
        return Prog(name,
            select: $"""
                SELECT F ASSIGN "{path}" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
                SELECT G ASSIGN "{path}" ORGANIZATION LINE SEQUENTIAL{(gStatus ? " FILE STATUS GS" : "")}.
                """,
            fd: """
                FD F.
                01 R PIC X(4).
                FD G.
                01 GR PIC X(4).
                """,
            ws: """
                01 FS PIC XX VALUE "??".
                01 GS PIC XX VALUE "??".
                """,
            proc: "    OPEN OUTPUT F\n" + proc + "\n    CLOSE F",
            checking, use);
    }

    [Fact]   // §9.1.13.9 1) e): OPEN OUTPUT of a physical file another connector has open is '61'; the WHEN is selected
    public void OpenOutputConflict_Retry_WhenFileSharing_RunsHandler_WithStatus61() => AssertRuns(Share("F3SH61N", """
            PERFORM
                OPEN OUTPUT RETRY 2 TIMES G
                DISPLAY "AFTER " GS
            WHEN EC-I-O-FILE-SHARING
                DISPLAY "HANDLER " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HANDLER 61\nAFTER 61\nDONE");

    [Fact]   // the paired form: exception-name FILE file-name (§14.9.28.2 Format 3) selects only G's exception
    public void OpenOutputConflict_Retry_WhenFileSharingPairedWithTheFile_RunsHandler() => AssertRuns(Share("F3SH61P", """
            PERFORM
                OPEN OUTPUT RETRY FOREVER G
                DISPLAY "AFTER " GS
            WHEN EC-I-O-FILE-SHARING FILE G
                DISPLAY "PAIRED " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "PAIRED 61\nAFTER 61\nDONE");

    [Fact]   // a WHEN paired with ANOTHER file does not match G's conflict; no USE and a FILE STATUS: the NF run goes on
    public void OpenOutputConflict_WhenPairedWithOtherFile_DoesNotRun() => AssertRuns(Share("F3SH61O", """
            PERFORM
                OPEN OUTPUT RETRY 1 TIMES G
                DISPLAY "AFTER " GS
            WHEN EC-I-O-FILE-SHARING FILE F
                DISPLAY "WRONG"
            END-PERFORM
        """), "AFTER 61\nDONE");

    [Fact]   // §14.9.49.4 GR6: nothing in the PERFORM matches, so the USE declarative for G is executed
    public void OpenOutputConflict_Retry_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(Share("F3SH61U", """
            PERFORM
                OPEN OUTPUT RETRY 2 TIMES G
                DISPLAY "AFTER " GS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("G", "DISPLAY \"USE \" GS.")), "USE 61\nAFTER 61\nDONE");

    [Fact]   // EC-I-O-FILE-SHARING is NONFATAL: no WHEN, no USE, no FILE STATUS, and the run goes on (status unobservable)
    public void OpenOutputConflict_NoWhen_NoUse_NoStatus_NonfatalRunContinues() => AssertRuns(Share("F3SH61X", """
            PERFORM
                OPEN OUTPUT RETRY 2 TIMES G
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, gStatus: false), "AFTER\nDONE");

    [Fact]   // checking OFF, the WHEN names an unrelated condition: no WHEN runs. The USE declarative still does: a USE
             // for a file is executed "upon the unsuccessful execution of an input-output operation" (§14.9.49.4 GR6)
             // whether or not a TURN directive enabled EC-I-O, and the status is the file's own.
    public void OpenOutputConflict_CheckingOff_RunsNoWhen_ButTheUseDeclarativeStillRuns() => AssertRuns(Share("F3SH61Z", """
            PERFORM
                OPEN OUTPUT RETRY 2 TIMES G
                DISPLAY "AFTER " GS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, checking: "", use: ("G", "DISPLAY \"USE \" GS.")), "USE 61\nAFTER 61\nDONE");

    [Fact]   // §9.1.13.9 2): DELETE FILE of a physical file another connector has open is '62'
    public void DeleteFileConflict_Retry_WhenFileSharing_RunsHandler_WithStatus62() => AssertRuns(Share("F3SH62N", """
            PERFORM
                DELETE FILE G RETRY 2 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-I-O-FILE-SHARING
                DISPLAY "HANDLER " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HANDLER 62\nAFTER 62\nDONE");

    [Fact]
    public void DeleteFileConflict_Retry_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(Share("F3SH62U", """
            PERFORM
                DELETE FILE G RETRY FOR 1 SECONDS
                DISPLAY "AFTER " GS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("G", "DISPLAY \"USE \" GS.")), "USE 62\nAFTER 62\nDONE");

    [Fact]
    public void DeleteFileConflict_NoWhen_NoUse_NoStatus_NonfatalRunContinues() => AssertRuns(Share("F3SH62X", """
            PERFORM
                DELETE FILE G RETRY 2 TIMES
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, gStatus: false), "AFTER\nDONE");

    [Fact]
    public void DeleteFileConflict_CheckingOff_SetsStatusOnly() => AssertRuns(Share("F3SH62Z", """
            PERFORM
                DELETE FILE G RETRY 2 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, checking: ""), "AFTER 62\nDONE");

    // ── record lock contention: '51', the record operation conflict (§9.1.13.8 1)) ────────────────────────────

    /// <summary>Two connectors on ONE relative file, both SHARING WITH ALL OTHER and LOCK MODE IS AUTOMATIC (the
    /// <c>l1_read_lock_mode_governs</c> shape): F's plain READ of record 1 locks it (§12.4.5.9.4 GR4), so G's READ of
    /// the same record in <paramref name="proc"/> is refused with '51' (§14.9.30.4 GR9), which is
    /// EC-I-O-RECORD-OPERATION, NONFATAL (the exception table, §14.6.13.1.6).</summary>
    private static string Lock(string name, string proc, bool gStatus = true, string checking = On,
        (string File, string Body)? use = null)
    {
        string path = FileHarness.Tmp(name);
        return Prog(name,
            select: $"""
                SELECT S ASSIGN "{path}" ORGANIZATION RELATIVE ACCESS SEQUENTIAL FILE STATUS SS.
                SELECT F ASSIGN "{path}" ORGANIZATION RELATIVE ACCESS RANDOM RELATIVE KEY FK FILE STATUS FS
                    SHARING WITH ALL OTHER LOCK MODE IS AUTOMATIC.
                SELECT G ASSIGN "{path}" ORGANIZATION RELATIVE ACCESS RANDOM RELATIVE KEY GK{(gStatus ? " FILE STATUS GS" : "")}
                    SHARING WITH ALL OTHER LOCK MODE IS AUTOMATIC.
                """,
            fd: """
                FD S.
                01 SR PIC X(4).
                FD F.
                01 FR PIC X(4).
                FD G.
                01 GR PIC X(4).
                """,
            ws: """
                01 SS PIC XX VALUE "??".
                01 FS PIC XX VALUE "??".
                01 GS PIC XX VALUE "??".
                01 FK PIC 9(4) VALUE 1.
                01 GK PIC 9(4) VALUE 1.
                """,
            proc: """
                    OPEN OUTPUT S
                    MOVE "R001" TO SR
                    WRITE SR
                    CLOSE S
                    OPEN INPUT F
                    OPEN INPUT G
                    READ F
                    DISPLAY "HOLD " FS " " FR
                """ + "\n" + proc + "\n    CLOSE G\n    CLOSE F",
            checking, use);
    }

    [Fact]   // §9.1.13.8 1): the READ of a record another connector has locked is '51'; the WHEN is selected
    public void ReadOfALockedRecord_Retry_WhenRecordOperation_RunsHandler_WithStatus51() => AssertRuns(Lock("F3LK51N", """
            PERFORM
                READ G RETRY 2 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-I-O-RECORD-OPERATION
                DISPLAY "HANDLER " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HOLD 00 R001\nHANDLER 51\nAFTER 51\nDONE");

    [Fact]   // the paired form selects only G's exception
    public void ReadOfALockedRecord_Retry_WhenRecordOperationPairedWithTheFile_RunsHandler() => AssertRuns(Lock("F3LK51P", """
            PERFORM
                READ G RETRY 2 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-I-O-RECORD-OPERATION FILE G
                DISPLAY "PAIRED " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HOLD 00 R001\nPAIRED 51\nAFTER 51\nDONE");

    [Fact]   // RETRY FOREVER on a lock THIS run unit holds is the one deadlock the implementor detects, '52' (§9.1.13.8
             // 2); docs/CONFORMANCE.md DOC-A.1-109), the same EC-I-O-RECORD-OPERATION ("5x"), so the same WHEN runs
    public void ReadOfALockedRecord_RetryForever_IsADeadlock_WhenRecordOperation_RunsHandler_WithStatus52() => AssertRuns(Lock("F3LK52N", """
            PERFORM
                READ G RETRY FOREVER
                DISPLAY "AFTER " GS
            WHEN EC-I-O-RECORD-OPERATION FILE G
                DISPLAY "DEADLOCK " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HOLD 00 R001\nDEADLOCK 52\nAFTER 52\nDONE");

    [Fact]   // a WHEN paired with the HOLDER's file does not match G's conflict; G has a FILE STATUS and no USE
    public void ReadOfALockedRecord_WhenPairedWithTheHoldersFile_DoesNotRun() => AssertRuns(Lock("F3LK51O", """
            PERFORM
                READ G RETRY 1 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-I-O-RECORD-OPERATION FILE F
                DISPLAY "WRONG"
            END-PERFORM
        """), "HOLD 00 R001\nAFTER 51\nDONE");

    [Fact]   // §14.9.49.4 GR6: nothing in the PERFORM matches, so the USE declarative for G is executed
    public void ReadOfALockedRecord_Retry_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(Lock("F3LK51U", """
            PERFORM
                READ G RETRY 2 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("G", "DISPLAY \"USE \" GS.")), "HOLD 00 R001\nUSE 51\nAFTER 51\nDONE");

    [Fact]   // EC-I-O-RECORD-OPERATION is NONFATAL: no WHEN, no USE, no FILE STATUS, and the run goes on
    public void ReadOfALockedRecord_NoWhen_NoUse_NoStatus_NonfatalRunContinues() => AssertRuns(Lock("F3LK51X", """
            PERFORM
                READ G RETRY 2 TIMES
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, gStatus: false), "HOLD 00 R001\nAFTER\nDONE");

    [Fact]   // checking OFF, the WHEN names an unrelated condition: no WHEN runs, the FILE STATUS records the conflict
    public void ReadOfALockedRecord_CheckingOff_SetsStatusOnly() => AssertRuns(Lock("F3LK51Z", """
            PERFORM
                READ G RETRY 2 TIMES
                DISPLAY "AFTER " GS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, checking: ""), "HOLD 00 R001\nAFTER 51\nDONE");

    // ── OPEN '39' (fixed-attribute conflict), '05' (OPTIONAL file absent), '37' (insufficient authority) ─────────

    /// <summary>A RELATIVE file of 20-byte records, then a second description of the same physical file with 40-byte
    /// records: §14.9.27.4 GR10 compares the attributes the program gives with the file's and answers '39'
    /// (§9.1.13.6 item 7). <paramref name="proc"/> runs after the 20-byte file has been created.</summary>
    private static string Attribute39(string name, string proc, bool status = true, string checking = On,
        (string File, string Body)? use = null)
    {
        string path = FileHarness.Tmp(name);
        return Prog(name,
            select: $"""
                SELECT MK ASSIGN "{path}" ORGANIZATION RELATIVE ACCESS SEQUENTIAL FILE STATUS MS.
                SELECT BG ASSIGN "{path}" ORGANIZATION RELATIVE ACCESS SEQUENTIAL{(status ? " FILE STATUS BS" : "")}.
                """,
            fd: """
                FD MK.
                01 MR PIC X(20).
                FD BG.
                01 BR PIC X(40).
                """,
            ws: """
                01 MS PIC XX VALUE "??".
                01 BS PIC XX VALUE "??".
                """,
            proc: """
                    OPEN OUTPUT MK
                    MOVE "ALPHA" TO MR
                    WRITE MR
                    CLOSE MK
                """ + "\n" + proc,
            checking, use);
    }

    [Fact]   // '39' is EC-I-O-PERMANENT-ERROR (first digit 3)
    public void OpenFixedAttributeConflict_WhenPermanentError_RunsHandler_WithStatus39() => AssertRuns(Attribute39("F3AT39N", """
            PERFORM
                OPEN INPUT BG
                DISPLAY "AFTER " BS
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "HANDLER " BS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HANDLER 39\nAFTER 39\nDONE");

    [Fact]
    public void OpenFixedAttributeConflict_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(Attribute39("F3AT39U", """
            PERFORM
                OPEN INPUT BG
                DISPLAY "AFTER " BS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("BG", "DISPLAY \"USE \" BS RESUME NEXT STATEMENT.")), "USE 39\nAFTER 39\nDONE");

    [Fact]   // Fatal, no WHEN, no USE, no FILE STATUS: the run unit terminates abnormally
    public void OpenFixedAttributeConflict_NoWhen_NoUse_NoStatus_IsFatal() => AssertFatal(Attribute39("F3AT39X", """
            PERFORM
                OPEN INPUT BG
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, status: false), "EC-I-O-PERMANENT-ERROR", "");

    [Fact]
    public void OpenFixedAttributeConflict_CheckingOff_SetsStatusOnly() => AssertRuns(Attribute39("F3AT39Z", """
            PERFORM
                OPEN INPUT BG
                DISPLAY "AFTER " BS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, checking: ""), "AFTER 39\nDONE");

    /// <summary>One OPTIONAL file that is absent (§9.1.13.2 item 4 a: OPEN INPUT is SUCCESSFUL, '05').</summary>
    private static string Optional05(string name, string proc, bool status = true, string checking = On,
        (string File, string Body)? use = null) => Prog(name,
        select: $"""
            SELECT OPTIONAL OP ASSIGN "{FileHarness.Tmp(name)}" ORGANIZATION LINE SEQUENTIAL{(status ? " FILE STATUS OS" : "")}.
            """,
        fd: """
            FD OP.
            01 OR1 PIC X(4).
            """,
        ws: """
            01 OS PIC XX VALUE "??".
            """,
        proc, checking, use);

    [Fact]   // '05' is the nonfatal EC-I-O-WARNING, raised only when explicitly enabled (§14.9.28.4 GR14: by the WHEN)
    public void OpenOptionalAbsent_WhenWarning_RunsHandler_WithStatus05() => AssertRuns(Optional05("F3OP05N", """
            PERFORM
                OPEN INPUT OP
                DISPLAY "AFTER " OS
            WHEN EC-I-O-WARNING
                DISPLAY "WARN " OS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "WARN 05\nAFTER 05\nDONE");

    [Fact]   // EC-ALL never enables EC-I-O-WARNING (>>TURN, rule 4), so a USE declarative for the file is not run either
    public void OpenOptionalAbsent_WarningNotEnabled_RunsNothing_StatusIs05() => AssertRuns(Optional05("F3OP05X", """
            PERFORM
                OPEN INPUT OP
                DISPLAY "AFTER " OS
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("OP", "DISPLAY \"USE\".")), "AFTER 05\nDONE");

    [Fact]   // the OPTIONAL file's successful 05 is not an error: no handler for the error exceptions runs
    public void OpenOptionalAbsent_WithoutFileStatus_Succeeds() => AssertRuns(Optional05("F3OP05W", """
            PERFORM
                OPEN INPUT OP
                DISPLAY "AFTER"
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "WRONG"
            END-PERFORM
        """, status: false), "AFTER\nDONE");

    /// <summary>'37' on OPEN OUTPUT over a PRESENT file this process may not write (§14.9.27.4 GR3: "If the file
    /// associated with file-name-1 is present and insufficient authority exists to open the file ..."; §9.1.13.6 item
    /// 6 a) 1.: "the EXTEND or OUTPUT phrase is specified but the file will not support write operations", and 6 b):
    /// "insufficient authority exists to access the file"). The host's ability to refuse is MEASURED, not assumed
    /// (kb/Work PB795): a process that bypasses file permissions (a root Linux user) has nothing to assert, and that
    /// is recorded, never silent.</summary>
    private static bool MakeUnwritable(string path)
    {
        File.WriteAllText(path, "HELLO");
        File.SetAttributes(path, FileAttributes.ReadOnly);
        bool refused = false;
        try { using (File.Open(path, FileMode.Open, FileAccess.Write)) { } }
        catch (UnauthorizedAccessException) { refused = true; }
        var denial = HostCapability.Classify(refused, applied: true, $"a read-only file {path}", "File.SetAttributes(ReadOnly)");
        if (!refused)
            Assert.True(denial.Verdict == HostCapability.DenialVerdict.BypassedByThisIdentity, denial.Because);
        return refused;
    }

    private static string Output37(string name, string path, string proc, bool status = true, string checking = On,
        (string File, string Body)? use = null) => Prog(name,
        select: $"""
            SELECT W ASSIGN "{path}" ORGANIZATION LINE SEQUENTIAL{(status ? " FILE STATUS WS1" : "")}.
            """,
        fd: """
            FD W.
            01 WR PIC X(4).
            """,
        ws: """
            01 WS1 PIC XX VALUE "??".
            """,
        proc, checking, use);

    private static void WithUnwritableFile(string name, Action<string> test)
    {
        string path = FileHarness.Tmp(name);
        try
        {
            if (!MakeUnwritable(path)) return;   // measured: this identity bypasses permissions
            test(path);
        }
        finally
        {
            FileHarness.TryDelete(path);
        }
    }

    [Fact]
    public void OpenOutputOverUnwritableFile_WhenPermanentError_RunsHandler_WithStatus37() =>
        WithUnwritableFile("F3PM37N", path => AssertRuns(Output37("F3PM37N", path, """
            PERFORM
                OPEN OUTPUT W
                DISPLAY "AFTER " WS1
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "HANDLER " WS1
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HANDLER 37\nAFTER 37\nDONE"));

    [Fact]
    public void OpenOutputOverUnwritableFile_UnmatchedWhen_FallsToUseDeclarative() =>
        WithUnwritableFile("F3PM37U", path => AssertRuns(Output37("F3PM37U", path, """
            PERFORM
                OPEN OUTPUT W
                DISPLAY "AFTER " WS1
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("W", "DISPLAY \"USE \" WS1 RESUME NEXT STATEMENT.")), "USE 37\nAFTER 37\nDONE"));

    [Fact]
    public void OpenOutputOverUnwritableFile_NoWhen_NoUse_NoStatus_IsFatal() =>
        WithUnwritableFile("F3PM37X", path => AssertFatal(Output37("F3PM37X", path, """
            PERFORM
                OPEN OUTPUT W
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, status: false), "EC-I-O-PERMANENT-ERROR", ""));

    // ── CLOSE 42 and DELETE FILE 41: the USE and the no-FILE-STATUS arms ──────────────────────────────────────

    private static string OneFile(string name, string proc, bool status = true, string checking = On,
        (string File, string Body)? use = null) => Prog(name,
        select: $"""
            SELECT F ASSIGN "{FileHarness.Tmp(name)}" ORGANIZATION LINE SEQUENTIAL{(status ? " FILE STATUS FS" : "")}.
            """,
        fd: """
            FD F.
            01 R PIC X(4).
            """,
        ws: """
            01 FS PIC XX VALUE "??".
            """,
        proc, checking, use);

    [Fact]   // §14.9.6.4 GR1 / §9.1.13.7 item 2: CLOSE of a file not open is '42', Fatal; the USE runs when no WHEN matches
    public void CloseUnopenedFile_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(OneFile("F3CL42U", """
            PERFORM
                CLOSE F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("F", "DISPLAY \"USE \" FS RESUME NEXT STATEMENT.")), "USE 42\nAFTER\nDONE");

    [Fact]   // a fatal condition whose declarative ends without RESUME terminates the run unit after the declarative
    public void CloseUnopenedFile_UseWithoutResume_FatalConditionTerminates() => AssertFatal(OneFile("F3CL42T", """
            PERFORM
                CLOSE F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("F", "DISPLAY \"USE \" FS.")), "EC-I-O-LOGIC-ERROR", "USE 42");

    [Fact]   // §9.1.13.7 item 1: DELETE FILE of a file in an open mode is '41', Fatal
    public void DeleteFileOfOpenFile_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(OneFile("F3DF41U", """
            OPEN OUTPUT F
            PERFORM
                DELETE FILE F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
            CLOSE F
        """, use: ("F", "DISPLAY \"USE \" FS RESUME NEXT STATEMENT.")), "USE 41\nAFTER\nDONE");

    [Fact]
    public void DeleteFileOfOpenFile_NoWhen_NoUse_NoStatus_IsFatal() => AssertFatal(OneFile("F3DF41X", """
            OPEN OUTPUT F
            PERFORM
                DELETE FILE F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, status: false), "EC-I-O-LOGIC-ERROR", "");

    [Fact]
    public void DeleteFileOfOpenFile_CheckingOff_SetsStatusOnly() => AssertRuns(OneFile("F3DF41Z", """
            OPEN OUTPUT F
            PERFORM
                DELETE FILE F
                DISPLAY "AFTER " FS
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
            CLOSE F
        """, checking: ""), "AFTER 41\nDONE");

    [Fact]   // OPEN of a file already open: '41' with a USE and no WHEN match
    public void OpenAlreadyOpenFile_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(OneFile("F3OP41U", """
            OPEN OUTPUT F
            PERFORM
                OPEN OUTPUT F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
            CLOSE F
        """, use: ("F", "DISPLAY \"USE \" FS RESUME NEXT STATEMENT.")), "USE 41\nAFTER\nDONE");

    // ── AT END and INVALID KEY: the statement's OWN phrase wins over the WHEN phrase (§9.1.13.1 text above 9.1.13.2)

    /// <summary>A LINE SEQUENTIAL file holding one record, closed, then read from the PERFORM's imperative statement.</summary>
    private static string OneRecord(string name, string proc, bool status = true, string checking = On,
        (string File, string Body)? use = null) => OneFile(name, """
                OPEN OUTPUT F
                MOVE "ONE1" TO R
                WRITE R
                CLOSE F
                OPEN INPUT F
            """ + "\n" + proc + "\n    CLOSE F", status, checking, use);

    [Fact]   // §9.1.13 / "If the exception condition EC-I-O-AT-END ... exists and the statement ... is specified with an AT END
             // phrase ... no other applicable exception processing statements shall be executed"
    public void ReadAtEnd_WithAtEndPhrase_RunsThePhrase_NotTheWhen() => AssertRuns(OneRecord("F3AE10P", """
            PERFORM
                READ F
                READ F AT END DISPLAY "AT-END-PHRASE " FS
                END-READ
            WHEN EC-I-O-AT-END
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("F", "DISPLAY \"WRONG-USE\".")), "AT-END-PHRASE 10\nDONE");

    [Fact]   // no AT END phrase: the WHEN naming EC-I-O-AT-END is selected
    public void ReadAtEnd_WithoutAtEndPhrase_WhenAtEnd_RunsHandler() => AssertRuns(OneRecord("F3AE10N", """
            PERFORM
                READ F
                READ F
                DISPLAY "AFTER"
            WHEN EC-I-O-AT-END
                DISPLAY "HANDLER " FS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HANDLER 10\nAFTER\nDONE");

    [Fact]   // no phrase, no matching WHEN: the USE declarative for the file is executed
    public void ReadAtEnd_WithoutAtEndPhrase_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(OneRecord("F3AE10U", """
            PERFORM
                READ F
                READ F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("F", "DISPLAY \"USE \" FS RESUME NEXT STATEMENT.")), "USE 10\nAFTER\nDONE");

    [Fact]   // EC-I-O-AT-END is NF: nothing applicable, no FILE STATUS item, the run goes on
    public void ReadAtEnd_NoPhrase_NoWhen_NoUse_NoStatus_NonfatalRunContinues() => AssertRuns(OneRecord("F3AE10X", """
            PERFORM
                READ F
                READ F
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, status: false), "AFTER\nDONE");

    /// <summary>A RELATIVE file with record 1 only, read at random: key 9 is absent (§9.1.13.5, '23').</summary>
    private static string Relative(string name, string proc, bool status = true, string checking = On,
        (string File, string Body)? use = null) => Prog(name,
        select: $"""
            SELECT RX ASSIGN "{FileHarness.Tmp(name)}" ORGANIZATION RELATIVE ACCESS RANDOM RELATIVE KEY K{(status ? " FILE STATUS RS" : "")}.
            """,
        fd: """
            FD RX.
            01 RR PIC X(4).
            """,
        ws: """
            01 K PIC 9 VALUE 1.
            01 RS PIC XX VALUE "??".
            """,
        proc: """
                OPEN OUTPUT RX
                MOVE "ONE1" TO RR
                WRITE RR
                CLOSE RX
                OPEN INPUT RX
                MOVE 9 TO K
            """ + "\n" + proc + "\n    CLOSE RX", checking, use);

    [Fact]
    public void ReadInvalidKey_WithInvalidKeyPhrase_RunsThePhrase_NotTheWhen() => AssertRuns(Relative("F3IK23P", """
            PERFORM
                READ RX INVALID KEY DISPLAY "INVALID-KEY-PHRASE " RS
                END-READ
            WHEN EC-I-O-INVALID-KEY
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("RX", "DISPLAY \"WRONG-USE\".")), "INVALID-KEY-PHRASE 23\nDONE");

    [Fact]
    public void ReadInvalidKey_WithoutPhrase_WhenInvalidKey_RunsHandler() => AssertRuns(Relative("F3IK23N", """
            PERFORM
                READ RX
                DISPLAY "AFTER"
            WHEN EC-I-O-INVALID-KEY
                DISPLAY "HANDLER " RS
                RESUME NEXT STATEMENT
            END-PERFORM
        """), "HANDLER 23\nAFTER\nDONE");

    [Fact]
    public void ReadInvalidKey_WithoutPhrase_UnmatchedWhen_FallsToUseDeclarative() => AssertRuns(Relative("F3IK23U", """
            PERFORM
                READ RX
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, use: ("RX", "DISPLAY \"USE \" RS RESUME NEXT STATEMENT.")), "USE 23\nAFTER\nDONE");

    [Fact]
    public void ReadInvalidKey_NoPhrase_NoWhen_NoUse_NoStatus_NonfatalRunContinues() => AssertRuns(Relative("F3IK23X", """
            PERFORM
                READ RX
                DISPLAY "AFTER"
            WHEN EC-SIZE
                DISPLAY "WRONG"
            END-PERFORM
        """, status: false), "AFTER\nDONE");

    // ── SORT / MERGE around the PERFORM (§14.9.40.3 SR3, §14.9.24.3 SR1: banned IN imperative-statement-1, legal in a WHEN)

    private const string SortSelect = """
        SELECT F ASSIGN "{0}" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
        SELECT G ASSIGN "{1}" ORGANIZATION LINE SEQUENTIAL FILE STATUS GS.
        SELECT H ASSIGN "{2}" ORGANIZATION LINE SEQUENTIAL{3}.
        SELECT J ASSIGN "{4}" ORGANIZATION LINE SEQUENTIAL FILE STATUS JS.
        SELECT SF ASSIGN "{5}".
        """;

    /// <summary>F holds three unordered records (DDDD AAAA CCCC), G is the GIVING file, H an ABSENT file, J a second
    /// GIVING file and SF the sort-merge work file; <c>TE</c> is a four-entry table.</summary>
    private static string Sort(string name, string proc, bool hStatus = true, string checking = On,
        (string File, string Body)? use = null, string paras = "")
    {
        return Prog(name,
            select: string.Format(SortSelect, FileHarness.Tmp(name + "F"), FileHarness.Tmp(name + "G"), FileHarness.Tmp(name + "H"),
                hStatus ? " FILE STATUS HS" : "", FileHarness.Tmp(name + "J"), FileHarness.Tmp(name + "S")),
            fd: """
                FD F.
                01 R PIC X(4).
                FD G.
                01 GR PIC X(4).
                FD H.
                01 HR PIC X(4).
                FD J.
                01 JR PIC X(4).
                SD SF.
                01 SR.
                   05 SK PIC X(4).
                """,
            ws: """
                01 FS PIC XX VALUE "??".
                01 GS PIC XX VALUE "??".
                01 HS PIC XX VALUE "??".
                01 JS PIC XX VALUE "??".
                01 TBL.
                   05 TE PIC 9 OCCURS 4 VALUE 0.
                """,
            proc: """
                    OPEN OUTPUT F
                    MOVE "DDDD" TO R WRITE R
                    MOVE "AAAA" TO R WRITE R
                    MOVE "CCCC" TO R WRITE R
                    CLOSE F
                """ + "\n" + proc, checking, use, paras);
    }

    /// <summary>Read G to its end, displaying each record.</summary>
    private const string ShowG = """
            OPEN INPUT G
            READ G AT END DISPLAY "EMPTY" NOT AT END DISPLAY GR END-READ
            READ G AT END CONTINUE NOT AT END DISPLAY GR END-READ
            READ G AT END CONTINUE NOT AT END DISPLAY GR END-READ
            CLOSE G
        """;

    [Fact]   // Format 2 (table) SORT is legal in imperative-statement-1 (the ban is on the FILE format): sorted, no handler
    public void TableSortInImp1_Success_RunsNoHandler() => AssertRuns(Sort("F3ST01", """
            MOVE 3 TO TE(1) MOVE 1 TO TE(2) MOVE 4 TO TE(3) MOVE 2 TO TE(4)
            PERFORM
                SORT TE ASCENDING KEY TE
                DISPLAY "TBL " TE(1) TE(2) TE(3) TE(4)
            WHEN EC-SORT-MERGE
                DISPLAY "HANDLER"
            WHEN EC-I-O
                DISPLAY "HANDLER-IO"
            END-PERFORM
        """), "TBL 1234\nDONE");

    [Fact]   // a file SORT in a WHEN phrase (legal): the SORT's implicit OPEN, READ, WRITE and CLOSE all answer '00'
    public void FileSortInWhen_Success_SortsTheFile() => AssertRuns(Sort("F3ST02", """
            PERFORM
                OPEN INPUT H
                DISPLAY "AFTER-H " HS
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "HANDLER-H " HS
                SORT SF ASCENDING KEY SK USING F GIVING G
                DISPLAY "SORTED " FS GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """ + "\n" + ShowG), "HANDLER-H 35\nSORTED 0000\nAFTER-H 35\nAAAA\nCCCC\nDDDD\nDONE");

    /// <summary>The imperative-statement-1 that earns the WHEN phrase a file SORT / MERGE: OPEN INPUT of the ABSENT
    /// file H answers '35' (EC-I-O-PERMANENT-ERROR), the WHEN runs <paramref name="body"/> and RESUMEs.
    /// <para>⛔ A WHEN body binds under TURN OFF ALL (§14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL
    /// is assumed at the end of imperative-statement-1"), and a TURN directive may NOT be written inside the
    /// exception-checking PERFORM at all (§7.3.25.3 SR5: "A TURN directive shall not be specified within an exception
    /// processing PERFORM statement"; the compiler warns, COBOL2187). A SORT / MERGE whose EC-SORT-MERGE conditions
    /// must be raised therefore lives in a PARAGRAPH compiled under the program's own EC-ALL directive and is
    /// PERFORMed from the WHEN body (<c>paras</c> of <see cref="Sort"/>): the shape a conforming program has.</para></summary>
    private static string InWhen(string body) => $"""
            PERFORM
                OPEN INPUT H
            WHEN EC-I-O-PERMANENT-ERROR
                {body}
                RESUME NEXT STATEMENT
            END-PERFORM
        """;

    /// <summary>OPEN INPUT of G in its own Format-3 PERFORM: G is absent exactly when no SORT / MERGE created it.</summary>
    private const string GAbsent = """
            PERFORM
                OPEN INPUT G
            WHEN EC-I-O-PERMANENT-ERROR
                DISPLAY "G-ABSENT " GS
                RESUME NEXT STATEMENT
            END-PERFORM
        """;

    private const string Nl = "\n                ";

    [Fact]   // §14.9.40.4 GR12 a)/b): the USING file's implicit OPEN INPUT answers '35', FATAL; the SORT is TERMINATED
             // (GR12 b: "if a fatal exception condition exists ... the SORT is terminated"), the FILE STATUS item holds
             // '35', the run unit goes on, and no GIVING file was created (the GIVING phase never began)
    public void FileSortInWhen_UsingFileAbsent_WithFileStatus_TerminatesTheSort_RunContinues() => AssertRuns(Sort("F3ST03",
        InWhen("SORT SF ASCENDING KEY SK USING H GIVING G" + Nl + "DISPLAY \"SORTED \" HS GS") + "\n" + GAbsent),
        "SORTED 35??\nG-ABSENT 35\nDONE");

    [Fact]   // the same failure with a USE declarative for the USING file: it runs ONCE, for the implicit OPEN (the
             // imp-1 OPEN was answered by the WHEN, which precedes the USE); GR12's closing text "such that any
             // applicable USE procedures are executed"
    public void FileSortInWhen_UsingFileAbsent_UseDeclarativeRunsForTheImplicitOpen() => AssertRuns(Sort("F3ST04",
        InWhen("SORT SF ASCENDING KEY SK USING H GIVING G" + Nl + "DISPLAY \"SORTED \" HS") + "\n" + GAbsent,
        use: ("H", "DISPLAY \"USE \" HS.")), "USE 35\nSORTED 35\nG-ABSENT 35\nDONE");

    [Fact]   // no FILE STATUS item and no USE for the USING file, checking on: nothing covers the fatal status, so the
             // implicit OPEN's EC-I-O-PERMANENT-ERROR ends the run unit (DOC-A.1-103)
    public void FileSortInWhen_UsingFileAbsent_NoStatus_NoUse_IsFatal() => AssertFatal(Sort("F3ST05",
        InWhen("SORT SF ASCENDING KEY SK USING H GIVING G" + Nl + "DISPLAY \"NOT REACHED\""), hStatus: false),
        "EC-I-O-PERMANENT-ERROR", "");

    [Fact]   // §14.9.40.4 GR9 a): a USING file in an open mode at the SORT is EC-SORT-MERGE-FILE-OPEN; §14.6.13.1.3 2)
             // makes the SORT rules apply, so the declarative runs, the SORT is terminated and the run unit continues
    public void FileSortInWhen_UsingFileOpen_SortMergeFileOpen_TerminatesTheSort() => AssertRuns(Sort("F3ST06",
        "    OPEN INPUT F\n" + InWhen("PERFORM SORT-PARA" + Nl + "DISPLAY \"CONTINUED\"")
        + "\n    CLOSE F\n" + GAbsent, use: ("EC-SORT-MERGE-FILE-OPEN", "DISPLAY \"USE-SM\"."),
        paras: SortPara("SORT SF ASCENDING KEY SK USING F GIVING G")),
        "USE-SM\nCONTINUED\nG-ABSENT 35\nDONE");

    [Fact]   // the same condition with NO declarative for it: EC-SORT-MERGE-FILE-OPEN is checked and unhandled, yet a
             // fatal condition in a SORT does NOT end the run unit (§14.6.12: "unless the exception occurs in a MERGE
             // statement, a SORT statement, or an I-O statement for which the implementor specifies otherwise, the
             // runtime system attempts to perform ..." the abnormal termination); the SORT is terminated and control
             // goes on, with no GIVING file created
    public void FileSortInWhen_UsingFileOpen_NoDeclarative_TerminatesTheSort_RunContinues() => AssertRuns(Sort("F3ST07",
        "    OPEN INPUT F\n" + InWhen("PERFORM SORT-PARA" + Nl + "DISPLAY \"CONTINUED\"")
        + "\n    CLOSE F\n" + GAbsent, paras: SortPara("SORT SF ASCENDING KEY SK USING F GIVING G")),
        "CONTINUED\nG-ABSENT 35\nDONE");

    /// <summary>The paragraph a conforming program PERFORMs from a WHEN body to run a SORT or MERGE with the
    /// EC-SORT-MERGE conditions checked (§7.3.25.3 SR5 forbids the TURN directive inside the exception-checking
    /// PERFORM; the paragraph is compiled under the program's own EC-ALL directive).</summary>
    private static string SortPara(string statement) => $"""
            SORT-PARA.
                {statement}.
            """;

    [Fact]   // §14.9.24.4 GR12: MERGE in a WHEN phrase (legal); both USING files are in key order, the GIVING file is merged
    public void MergeInWhen_Success_MergesTheFiles() => AssertRuns(Sort("F3MG01", """
            OPEN OUTPUT G
            MOVE "BBBB" TO GR WRITE GR
            MOVE "EEEE" TO GR WRITE GR
            CLOSE G
            OPEN OUTPUT F
            MOVE "AAAA" TO R WRITE R
            MOVE "CCCC" TO R WRITE R
            CLOSE F
            PERFORM
                OPEN INPUT H
            WHEN EC-I-O-PERMANENT-ERROR
                MERGE SF ASCENDING KEY SK USING F G GIVING J
                DISPLAY "MERGED " FS GS JS
                RESUME NEXT STATEMENT
            END-PERFORM
            OPEN INPUT J
            READ J AT END CONTINUE NOT AT END DISPLAY JR END-READ
            READ J AT END CONTINUE NOT AT END DISPLAY JR END-READ
            READ J AT END CONTINUE NOT AT END DISPLAY JR END-READ
            READ J AT END CONTINUE NOT AT END DISPLAY JR END-READ
            CLOSE J
        """), "MERGED 000000\nAAAA\nBBBB\nCCCC\nEEEE\nDONE");

    [Fact]   // §14.9.24.4 GR6: a USING file out of key order is EC-SORT-MERGE-SEQUENCE; the MERGE is terminated, the
             // declarative runs and the run unit continues
    public void MergeInWhen_UsingFileOutOfSequence_TerminatesTheMerge() => AssertRuns(Sort("F3MG02", """
            OPEN OUTPUT G
            MOVE "BBBB" TO GR WRITE GR
            CLOSE G
        """ + "\n" + InWhen("PERFORM SORT-PARA" + Nl + "DISPLAY \"CONTINUED\""),
        use: ("EC-SORT-MERGE-SEQUENCE", "DISPLAY \"USE-SEQ\"."),
        paras: SortPara("MERGE SF ASCENDING KEY SK USING F G GIVING J")), "USE-SEQ\nCONTINUED\nDONE");
}
