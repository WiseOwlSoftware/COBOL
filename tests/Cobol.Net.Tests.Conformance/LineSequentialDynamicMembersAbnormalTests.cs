// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The abnormal arms of a LINE SEQUENTIAL file whose record holds dynamic-length members that a corpus golden cannot
/// witness (kb/Work PB1563, docs/CONFORMANCE.md §3 D-FRA (x); campaign PB3353's requirement that file I-O is tested on
/// its exception paths as fully as on its success paths). A golden must exit 0; the statements here are the ones that
/// meet an I-O exception on a file with NEITHER a FILE STATUS clause NOR an applicable USE procedure, which ISO
/// §9.1.13.1 makes fatal for a status beginning 3, 4 or 7 and §14.6.13.1.3 7) ends the run unit for. The arms that have a
/// FILE STATUS or a USE procedure are the golden <c>pb1563_line_sequential_dynamic_members_exceptions</c>.
/// <para>Each program stops at the failing statement: the stdout that stops, the exit code 1 and the stderr surface are
/// what is asserted.</para>
/// </summary>
public sealed class LineSequentialDynamicMembersAbnormalTests
{
    private const string Record = """
        01 LR.
           05 A PIC X DYNAMIC LENGTH LIMIT 9.
           05 KY PIC X(3).
           05 C PIC X DYNAMIC LENGTH LIMIT 8.
        """;

    private static void AssertAbnormal(string source, string expectedStdout, string status, string statement)
    {
        var (exit, stdout, stderr) = new CobolNetCompiler(2023).CompileAndRunExit(source);
        Assert.Equal(expectedStdout, stdout.Replace("\r\n", "\n").TrimEnd('\n'));   // nothing after the failing statement runs
        Assert.Equal(1, exit);                                                     // §14.6.12 — the abnormal-termination exit
        Assert.Contains("abnormal run-unit termination", stderr);
        Assert.Contains($"I-O status {status}", stderr);
        Assert.Contains(statement, stderr);
    }

    /// <summary>§14.9.30.4 GR21 + §9.1.13.1: a READ past the end of the file is '46' once the AT END condition has been
    /// raised, and with no FILE STATUS and no USE procedure that is fatal (the first READ delivers the record, the
    /// second is the at end condition handled by its AT END phrase, the third is '46').</summary>
    [Fact]
    public void Read_PastTheEnd_NoFileStatusNoUse_TerminatesAbnormally()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1563F1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LF ASSIGN TO "pb1563f1.dat" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD LF.
            {Record}
            PROCEDURE DIVISION.
            MAIN-1.
                OPEN OUTPUT LF
                MOVE "AA" TO A
                MOVE "KEY" TO KY
                MOVE "CC" TO C
                WRITE LR
                CLOSE LF
                OPEN INPUT LF
                READ LF AT END DISPLAY "EOF-1" END-READ
                DISPLAY "READ-1"
                READ LF AT END DISPLAY "EOF-2" END-READ
                DISPLAY "READ-2"
                READ LF
                DISPLAY "NOT-REACHED"
                STOP RUN.
            """;
        AssertAbnormal(source, "READ-1\nEOF-2\nREAD-2", "46", "(READ)");
    }

    /// <summary>§14.9.35.4 GR5 + §9.1.13.1: a REWRITE whose immediately previous statement was not a successful READ is
    /// '43', fatal with no FILE STATUS and no USE procedure; the record area is the dynamic-member group's.</summary>
    [Fact]
    public void Rewrite_WithoutAPriorRead_NoFileStatusNoUse_TerminatesAbnormally()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1563F2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LF ASSIGN TO "pb1563f2.dat" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD LF.
            {Record}
            PROCEDURE DIVISION.
            MAIN-1.
                OPEN OUTPUT LF
                MOVE "AA" TO A
                MOVE "KEY" TO KY
                MOVE "CC" TO C
                WRITE LR
                CLOSE LF
                OPEN I-O LF
                DISPLAY "OPENED"
                REWRITE LR
                DISPLAY "NOT-REACHED"
                STOP RUN.
            """;
        AssertAbnormal(source, "OPENED", "43", "(REWRITE)");
    }

    /// <summary>§14.9.35.4 GR17 d) + §9.1.13.1: the REWRITE of a record whose dynamic member (past the fixed run) holds
    /// a character outside the line sequential character set is '71', fatal with no FILE STATUS and no USE procedure.</summary>
    [Fact]
    public void Rewrite_ControlCharacterPastTheFixedRun_NoFileStatusNoUse_TerminatesAbnormally()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1563F3.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LF ASSIGN TO "pb1563f3.dat" ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD LF.
            {Record}
            PROCEDURE DIVISION.
            MAIN-1.
                OPEN OUTPUT LF
                MOVE "AA" TO A
                MOVE "KEY" TO KY
                MOVE "CCCC" TO C
                WRITE LR
                CLOSE LF
                OPEN I-O LF
                READ LF
                DISPLAY "READ-OK"
                MOVE "AA" TO A
                MOVE "KEY" TO KY
                MOVE X"01" TO C
                REWRITE LR
                DISPLAY "NOT-REACHED"
                STOP RUN.
            """;
        AssertAbnormal(source, "READ-OK", "71", "(REWRITE)");
    }

    /// <summary>§14.9.40.4 GR12 + §9.1.13.1: a SORT whose USING line sequential file is not there is '35' at the implicit
    /// OPEN, fatal with no FILE STATUS and no USE procedure; the run unit ends and the GIVING statement never runs.</summary>
    [Fact]
    public void Sort_UsingAnAbsentFile_NoFileStatusNoUse_TerminatesAbnormally()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1563F4.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT MISS ASSIGN TO "pb1563f4-never-written.dat" ORGANIZATION IS LINE SEQUENTIAL.
                SELECT SOUT ASSIGN TO "pb1563f4o.dat" ORGANIZATION IS LINE SEQUENTIAL.
                SELECT SRTF ASSIGN TO "pb1563f4.srt".
            DATA DIVISION.
            FILE SECTION.
            FD MISS.
            01 MR.
               05 MK PIC X(2).
               05 MA PIC X DYNAMIC LENGTH LIMIT 9.
            FD SOUT.
            01 OR1.
               05 OK PIC X(2).
               05 OA PIC X DYNAMIC LENGTH LIMIT 9.
            SD SRTF.
            01 SR.
               05 SK PIC X(2).
               05 SA PIC X DYNAMIC LENGTH LIMIT 9.
            PROCEDURE DIVISION.
            MAIN-1.
                DISPLAY "BEFORE"
                SORT SRTF ON ASCENDING KEY SK USING MISS GIVING SOUT
                DISPLAY "NOT-REACHED"
                STOP RUN.
            """;
        AssertAbnormal(source, "BEFORE", "35", "SORT USING");
    }
}
