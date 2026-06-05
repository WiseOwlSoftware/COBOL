// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// WS-SPEC extra-coverage tests for the Sequential/Relative/Indexed I-O module — features the
/// baselined NIST SQ/RL/IX suite under-tests (docs/SPEC_GAP_INVENTORY.md §"Sequential/Relative/Indexed I-O").
/// Each program is self-contained: it creates its own file, writes records, then reads them back and
/// DISPLAYs the result, so the observed stdout is fully deterministic.
/// </summary>
public sealed class FileIoSpecTests : EndToEndTestBase
{
    /// <summary>
    /// CLOSE ... WITH NO REWIND (ISO §14.9.7). The NO REWIND phrase is exercised on a passing path only by
    /// the unbaselined SQ401M. Verify the file closes, re-opens, and its data is intact — i.e. NO REWIND is
    /// accepted and benign, with file status 00 at every I-O step.
    /// </summary>
    [Fact]
    public void CloseNoRewind_FileReopenableAndDataIntact()
    {
        var (success, stdout, stderr) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. SQNOREW2.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT SEQ-FILE ASSIGN TO "SQNOREW2-DATA"
                           ORGANIZATION IS SEQUENTIAL
                           FILE STATUS IS WS-STAT.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  SEQ-FILE.
                   01  SEQ-REC PIC X(10).
                   WORKING-STORAGE SECTION.
                   01  WS-STAT PIC XX VALUE "99".
                   01  WS-IN PIC X(10).
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       OPEN OUTPUT SEQ-FILE.
                       DISPLAY "S1=" WS-STAT.
                       MOVE "REC-AAAAAA" TO SEQ-REC.
                       WRITE SEQ-REC.
                       DISPLAY "S2=" WS-STAT.
                       MOVE "REC-BBBBBB" TO SEQ-REC.
                       WRITE SEQ-REC.
                       CLOSE SEQ-FILE WITH NO REWIND.
                       DISPLAY "S3=" WS-STAT.
                       OPEN INPUT SEQ-FILE.
                       DISPLAY "S4=" WS-STAT.
                       READ SEQ-FILE INTO WS-IN
                           AT END CONTINUE.
                       DISPLAY "S5=" WS-STAT.
                       DISPLAY "FIRST=" WS-IN.
                       CLOSE SEQ-FILE.
                       STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(6, lines.Length);
        Assert.Equal("S1=00", lines[0]);
        Assert.Equal("S2=00", lines[1]);
        Assert.Equal("S3=00", lines[2]);
        Assert.Equal("S4=00", lines[3]);
        Assert.Equal("S5=00", lines[4]);
        // Data survives the NO REWIND close + re-open: the first record reads back unchanged.
        Assert.Equal("FIRST=REC-AAAAAA", lines[5]);
    }

    /// <summary>
    /// READ ... PREVIOUS RECORD on an indexed file (ISO §14.9.30; §13.5.1 reverse-order traversal). After
    /// reading forward to the highest key (file position indicator established by a READ), each READ PREVIOUS
    /// returns the first record whose key is strictly less than the indicator (ISO §14.9.30 GR for indexed
    /// PREVIOUS) — i.e. descending order. Exercises the COBOL-2002+/2023 PREVIOUS phrase the NIST suite never uses.
    /// </summary>
    [Fact]
    public void ReadPrevious_ReturnsKeysInDescendingOrderAfterForwardRead()
    {
        var (success, stdout, stderr) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. IXPREV3.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT IX-FILE ASSIGN TO "IXPREV3-DATA"
                           ORGANIZATION IS INDEXED
                           ACCESS MODE IS DYNAMIC
                           RECORD KEY IS IX-KEY.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  IX-FILE.
                   01  IX-REC.
                       05 IX-KEY PIC 9(3).
                       05 IX-DATA PIC X(5).
                   WORKING-STORAGE SECTION.
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       OPEN OUTPUT IX-FILE.
                       MOVE 100 TO IX-KEY. MOVE "AAAAA" TO IX-DATA.
                       WRITE IX-REC.
                       MOVE 200 TO IX-KEY. MOVE "BBBBB" TO IX-DATA.
                       WRITE IX-REC.
                       MOVE 300 TO IX-KEY. MOVE "CCCCC" TO IX-DATA.
                       WRITE IX-REC.
                       CLOSE IX-FILE.
                       OPEN INPUT IX-FILE.
                       READ IX-FILE NEXT RECORD
                           AT END CONTINUE.
                       DISPLAY "N1=" IX-KEY.
                       READ IX-FILE NEXT RECORD
                           AT END CONTINUE.
                       DISPLAY "N2=" IX-KEY.
                       READ IX-FILE NEXT RECORD
                           AT END CONTINUE.
                       DISPLAY "N3=" IX-KEY.
                       READ IX-FILE PREVIOUS RECORD
                           AT END DISPLAY "P1-ATEND"
                           NOT AT END DISPLAY "P1=" IX-KEY
                       END-READ.
                       READ IX-FILE PREVIOUS RECORD
                           AT END DISPLAY "P2-ATEND"
                           NOT AT END DISPLAY "P2=" IX-KEY
                       END-READ.
                       CLOSE IX-FILE.
                       STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        // Forward (NEXT) traversal: ascending keys.
        Assert.Equal("N1=100", lines[0]);
        Assert.Equal("N2=200", lines[1]);
        Assert.Equal("N3=300", lines[2]);
        // PREVIOUS after a forward read: first record whose key < file position indicator (descending).
        Assert.Equal("P1=200", lines[3]);
        Assert.Equal("P2=100", lines[4]);
    }

    /// <summary>
    /// USE ... AFTER STANDARD ERROR PROCEDURE ON file (ISO §14.9.49). A random READ for a non-existent indexed
    /// key raises the invalid-key exception (file status 23). With no inline INVALID KEY phrase on the READ, the
    /// matching error declarative is dispatched — verify the declarative ran (it stamps a sentinel) and the
    /// FILE STATUS register holds "23". This is the file-I-O error-declarative path the SQ/RL/IX suite under-tests.
    /// </summary>
    [Fact]
    public void UseAfterStandardError_DeclarativeFiresOnInvalidKey()
    {
        var (success, stdout, stderr) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. USEERR.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT IX-FILE ASSIGN TO "USEERR-DATA"
                           ORGANIZATION IS INDEXED
                           ACCESS MODE IS DYNAMIC
                           RECORD KEY IS IX-KEY
                           FILE STATUS IS WS-STAT.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  IX-FILE.
                   01  IX-REC.
                       05 IX-KEY PIC 9(3).
                       05 IX-DATA PIC X(5).
                   WORKING-STORAGE SECTION.
                   01  WS-STAT PIC XX VALUE "00".
                   01  WS-MSG PIC X(20) VALUE SPACES.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   ERR-SECT SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
                   ERR-PARA.
                       MOVE "DECL-FIRED" TO WS-MSG.
                   END DECLARATIVES.
                   MAIN-SECT SECTION.
                   MAIN-PARA.
                       OPEN OUTPUT IX-FILE.
                       MOVE 100 TO IX-KEY. MOVE "AAAAA" TO IX-DATA.
                       WRITE IX-REC.
                       CLOSE IX-FILE.
                       OPEN I-O IX-FILE.
                       MOVE 999 TO IX-KEY.
                       READ IX-FILE RECORD.
                       DISPLAY "STAT=" WS-STAT.
                       DISPLAY "MSG=" WS-MSG.
                       CLOSE IX-FILE.
                       STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // Invalid-key (record not found) status, then proof the error declarative was dispatched.
        Assert.Equal("STAT=23", lines[0]);
        Assert.Equal("MSG=DECL-FIRED", lines[1]);
    }

    /// <summary>
    /// MULTIPLE FILE TAPE clause in I-O-CONTROL (ISO §12.5; obsolete in COBOL-85, removed in 2002+). It is a
    /// listing/volume-grouping directive with no run-time effect on record content. Verify a program containing
    /// MULTIPLE FILE TAPE still compiles and performs ordinary sequential I-O correctly (the clause is a benign
    /// no-op at run time; the obsolete-element diagnostic itself belongs to the WS-FLAG flagging harness).
    /// </summary>
    [Fact]
    public void MultipleFileTape_ObsoleteClauseIsBenignAtRuntime()
    {
        var (success, stdout, stderr) = CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. MULTIF.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT FILE-A ASSIGN TO "MULTIF-A"
                           ORGANIZATION IS SEQUENTIAL.
                       SELECT FILE-B ASSIGN TO "MULTIF-B"
                           ORGANIZATION IS SEQUENTIAL.
                   I-O-CONTROL.
                       MULTIPLE FILE TAPE CONTAINS FILE-A FILE-B.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  FILE-A.
                   01  REC-A PIC X(10).
                   FD  FILE-B.
                   01  REC-B PIC X(10).
                   WORKING-STORAGE SECTION.
                   01  WS-IN PIC X(10).
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       OPEN OUTPUT FILE-A.
                       MOVE "HELLO-AAAA" TO REC-A.
                       WRITE REC-A.
                       CLOSE FILE-A.
                       OPEN INPUT FILE-A.
                       READ FILE-A INTO WS-IN
                           AT END CONTINUE.
                       DISPLAY "GOT=" WS-IN.
                       CLOSE FILE-A.
                       STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("GOT=HELLO-AAAA", stdout);
    }
}
