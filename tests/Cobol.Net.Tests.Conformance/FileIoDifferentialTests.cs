// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Sequential file I/O (ISO/IEC 1989:2023 §14.9; COBOLNET_DESIGN §8): OPEN/CLOSE/WRITE/READ/REWRITE over a typed-native
/// connector. Each test round-trips through the file (WRITE then READ back, or query FILE STATUS) and DISPLAYs the
/// result, so the existing stdout differential harness pins COBOL.NET to the legacy oracle (364-NIST-green) — the
/// file content itself is verified indirectly, through the program's own read-back. The printer WRITE … ADVANCING path
/// is exercised end-to-end by the NC101A NIST program; here the focus is the data-file verbs and the status machine.
/// </summary>
public sealed class FileIoDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source)
    {
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(lout, cout);
    }

    /// <summary>A program with a single SELECTed file; <paramref name="select"/> is the SELECT clause body (after the
    /// file-name), <paramref name="fd"/> the FD record description(s), and <paramref name="proc"/> the procedure body.
    /// A distinct ASSIGN target per test keeps the host files from colliding across the (isolated) runs.</summary>
    private static string Program(string select, string fd, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FIOTEST.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F {select}.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {fd}
        WORKING-STORAGE SECTION.
        01 WS-EOF PIC X VALUE "N".
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    // The read-back fields are displayed as the LAST operand so the comparison is unaffected by the legacy's known
    // DISPLAY trailing-space non-conformance (a bracketed field would expose internal trailing spaces — see
    // CutRunner.Normalize / CompilerUnderTest); the per-line trailing trim then washes the field width out.
    [Fact]
    public void WriteThenReadBack_RecordSequential()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT1\"", "01 F-REC PIC X(10).", "",
            """
                OPEN OUTPUT F.
                MOVE "HELLO" TO F-REC. WRITE F-REC.
                MOVE "WORLD" TO F-REC. WRITE F-REC.
                CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "R=" F-REC
                    END-READ
                END-PERFORM.
                CLOSE F.
            """));

    [Fact]
    public void WriteFrom_AndReadInto()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT2\"", "01 F-REC PIC X(8).",
            "01 WS-SEND PIC X(8) VALUE \"ABCDEFGH\".\n01 WS-RECV PIC X(8).",
            """
                OPEN OUTPUT F.
                WRITE F-REC FROM WS-SEND.
                CLOSE F.
                OPEN INPUT F.
                READ F INTO WS-RECV AT END MOVE "Y" TO WS-EOF
                    NOT AT END DISPLAY "GOT=" WS-RECV
                END-READ.
                CLOSE F.
            """));

    [Fact]
    public void Extend_AppendsAfterExistingRecords()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT3\"", "01 F-REC PIC X(4).", "",
            """
                OPEN OUTPUT F. MOVE "AAAA" TO F-REC. WRITE F-REC. CLOSE F.
                OPEN EXTEND F. MOVE "BBBB" TO F-REC. WRITE F-REC. CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY F-REC
                    END-READ
                END-PERFORM.
                CLOSE F.
            """));

    [Fact]
    public void FileStatus_SuccessAndEof()
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT4\" FILE STATUS IS WS-ST", "01 F-REC PIC X(3).",
            "01 WS-ST PIC XX.",
            """
                OPEN OUTPUT F. DISPLAY "OPEN=" WS-ST.
                MOVE "XYZ" TO F-REC. WRITE F-REC. DISPLAY "WRITE=" WS-ST.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END CONTINUE NOT AT END CONTINUE END-READ. DISPLAY "READ1=" WS-ST.
                READ F AT END CONTINUE NOT AT END CONTINUE END-READ. DISPLAY "READ2=" WS-ST.
                CLOSE F.
            """));

    [Fact]
    public void OptionalAbsent_OpenInput_IsAtEnd()
        // SELECT OPTIONAL precedes the file-name (ISO §12.4.5.2); an OPTIONAL file absent at OPEN INPUT opens with
        // status 05 and the first READ raises AT END (ISO §9.1.13.2).
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOOPT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OPTIONAL F ASSIGN TO "FIO-NOEXIST-XYZ".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                OPEN INPUT F.
                READ F AT END DISPLAY "ATEND"
                    NOT AT END DISPLAY "GOT " F-REC
                END-READ.
                CLOSE F.
                STOP RUN.
            """);

    [Fact]
    public void MultipleRecordsUnderOneFd_ShareTheArea()
        // ISO §9.1.2: two 01s under one FD occupy the same record area — MOVE into one, WRITE the other writes the
        // same bytes (the NC101A PRINT-REC / DUMMY-RECORD pattern).
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT5\"", "01 REC-A PIC X(6).\n01 REC-B PIC X(6).", "",
            """
                OPEN OUTPUT F.
                MOVE "SHARED" TO REC-A.
                WRITE REC-B.
                CLOSE F.
                OPEN INPUT F.
                READ F AT END MOVE "Y" TO WS-EOF NOT AT END DISPLAY "READ=" REC-A END-READ.
                CLOSE F.
            """));

    [Fact]
    public void WriteAfterAdvancing_LineSequentialReadBack()
        // A printer-style WRITE … AFTER ADVANCING stream read back line-by-line (LINE SEQUENTIAL): the advancing
        // newline structure (a leading blank line per AFTER) is observable as the read records.
        => AssertSameAsLegacy(Program("ASSIGN TO \"FIO-RT6\" ORGANIZATION IS LINE SEQUENTIAL", "01 F-REC PIC X(5).", "",
            """
                OPEN OUTPUT F.
                MOVE "LINE1" TO F-REC. WRITE F-REC AFTER ADVANCING 1 LINES.
                MOVE "LINE2" TO F-REC. WRITE F-REC AFTER ADVANCING 1 LINES.
                CLOSE F.
                OPEN INPUT F.
                PERFORM UNTIL WS-EOF = "Y"
                    READ F AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "R=" F-REC
                    END-READ
                END-PERFORM.
                CLOSE F.
            """));
}
