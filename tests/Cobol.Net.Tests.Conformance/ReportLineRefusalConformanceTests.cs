// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A REPORT LINE THE FILE REFUSES IS NOT AN I-O STATEMENT'S EXCEPTION (kb/Work PB1603; docs/CONFORMANCE.md
/// DOC-A.1-159, DOC-A.1-103). GENERATE, INITIATE and TERMINATE write the report file's lines, and a line the medium
/// refuses — a character with no byte image in the file coded character set ('91', or '71' on a LINE SEQUENTIAL
/// file; owner decision kb/Work R47) or a medium the host reports full ('34') — "does not take place"
/// (§14.9.51.4 GR15). The question these tests pin is what the PROGRAM can observe of the refusal.
/// <para>The standard's answer: "The value of the I-O status is set during the execution of a CLOSE, DELETE, OPEN,
/// READ, REWRITE, START, UNLOCK or WRITE statement" (§9.1.13.1) — GENERATE, INITIATE and TERMINATE are not on that
/// list, and no rule of §14.9.16.4, §14.9.21.4 or §14.9.46.4 writes the line "as if a WRITE statement" (as
/// §14.9.24.4 GR12 does for MERGE). So the report file's FILE STATUS item keeps the value OPEN stored, no
/// EC-I-O condition is raised and no USE procedure of the file runs, and the run continues
/// (the choice after a fatal status is the implementor's, §9.1.13.1: "The implementor may either continue or
/// terminate the execution of the run unit"). Where the standard leaves the choice, GnuCOBOL 3.2.0 answers
/// (CLAUDE.md rule 1) and it agrees: measured 2026-10-18 on a report file ASSIGNed to <c>/dev/full</c>, whose
/// plain WRITE answers FILE STATUS '34' at the 68th record, the report's FILE STATUS stays '00' through 300
/// GENERATEs, TERMINATE and CLOSE, and its USE AFTER EXCEPTION procedure never runs.</para>
/// <para>The tests cover the refusal both ways it arises ('91'/'71' on every host, '34' on the host that has a full
/// medium by construction) and read the outcome from every place a program could look: the FILE STATUS item after
/// each report statement, the file's USE procedure, and the medium itself.</para>
/// </summary>
public sealed class ReportLineRefusalConformanceTests
{
    private static readonly CobolNetCompiler CobolNet2023 = new(2023);

    private static string Host(string s) => s.Replace("{nl}", Environment.NewLine, StringComparison.Ordinal);

    /// <summary>A report file with FILE STATUS and a USE procedure, three detail lines of which the middle one holds
    /// U+20AC. Every report statement is followed by a DISPLAY of the FILE STATUS item and the USE counter.</summary>
    private static string Program(string id, string file, string org) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT PRT ASSIGN TO "{file}" ORGANIZATION IS {org}
                FILE STATUS IS FS.
        DATA DIVISION.
        FILE SECTION.
        FD PRT REPORT IS R-A.
        WORKING-STORAGE SECTION.
        01 FS     PIC XX.
        01 WS-A   PIC X(3).
        01 WS-USE PIC 9 VALUE 0.
        REPORT SECTION.
        RD R-A PAGE LIMIT 20 LINES.
        01 DET TYPE DE LINE PLUS 1.
           03 COLUMN 1 PIC X(3) SOURCE WS-A.
        PROCEDURE DIVISION.
        DECLARATIVES.
        U SECTION.
            USE AFTER STANDARD EXCEPTION PROCEDURE ON PRT.
        U1.
            ADD 1 TO WS-USE.
        END DECLARATIVES.
        MAIN SECTION.
            OPEN OUTPUT PRT
            DISPLAY "OPEN FS=" FS " USE=" WS-USE
            INITIATE R-A
            DISPLAY "INITIATE FS=" FS " USE=" WS-USE
            MOVE "A1B" TO WS-A
            GENERATE DET
            DISPLAY "GENERATE-1 FS=" FS " USE=" WS-USE
            MOVE "A€B" TO WS-A
            GENERATE DET
            DISPLAY "GENERATE-2 FS=" FS " USE=" WS-USE
            MOVE "AéB" TO WS-A
            GENERATE DET
            DISPLAY "GENERATE-3 FS=" FS " USE=" WS-USE
            TERMINATE R-A
            DISPLAY "TERMINATE FS=" FS " USE=" WS-USE
            CLOSE PRT
            DISPLAY "CLOSE FS=" FS " USE=" WS-USE
            STOP RUN.
        """;

    /// <summary>The refused line is invisible to the program: FILE STATUS keeps OPEN's '00' through every report
    /// statement, the USE procedure never runs, the run reaches STOP RUN, and the only trace is the blank line on the
    /// medium (§14.9.51.4 GR15 — the refused line, its advance included, does not take place, so the next line's
    /// advance covers its slot). The two organizations refuse the same line with different statuses ('91' and '71'),
    /// and neither reaches the program.</summary>
    [Theory]
    [InlineData("SEQUENTIAL", "RLR1", "rlr1.rpt")]
    [InlineData("LINE SEQUENTIAL", "RLR2", "rlr2.rpt")]
    public void RefusedReportLine_IsNotAnIoStatementException(string org, string id, string file)
    {
        var (ok, stdout, detail, bytes) = CobolNet2023.CompileRunAndReadFile(Program(id, file, org), file);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(
            """
            OPEN FS=00 USE=0
            INITIATE FS=00 USE=0
            GENERATE-1 FS=00 USE=0
            GENERATE-2 FS=00 USE=0
            GENERATE-3 FS=00 USE=0
            TERMINATE FS=00 USE=0
            CLOSE FS=00 USE=0
            """.ReplaceLineEndings("\n"),
            stdout.ReplaceLineEndings("\n").TrimEnd('\n'));
        Assert.NotNull(bytes);
        Assert.Equal(Host("A1B{nl}{nl}AéB{nl}"), Encoding.Latin1.GetString(bytes!));
    }

    /// <summary>The same silence on a medium the host reports full ('34', §9.1.13.6 item 3): Linux's
    /// <c>/dev/full</c> refuses every write with ENOSPC. The writer buffers, so the refusal surfaces on a LATER line's
    /// write (the 68th plain record in GnuCOBOL), and the report statements that meet it answer the program
    /// nothing: FILE STATUS '00' and no USE procedure after the whole run of GENERATEs and after TERMINATE. (CLOSE is
    /// an I-O statement of §9.1.13.1 with its own status, deliberately not asserted here.) A host without the device
    /// must not be Linux, so this cannot pass on the CI host by not running.</summary>
    [Fact]
    public void ReportLine_RefusedByAFullMedium_IsNotAnIoStatementException()
    {
        const string full = "/dev/full";
        if (!File.Exists(full))
        {
            Assert.False(OperatingSystem.IsLinux(), "a Linux host provides /dev/full; the boundary was not exercised");
            return;
        }
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RLR3.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT PRT ASSIGN TO "/dev/full" FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD PRT REPORT IS R-A.
            WORKING-STORAGE SECTION.
            01 FS     PIC XX.
            01 WS-A   PIC X(60) VALUE ALL "A".
            01 WS-N   PIC 9(4) VALUE 0.
            01 WS-BAD PIC 9(4) VALUE 0.
            01 WS-USE PIC 9 VALUE 0.
            REPORT SECTION.
            RD R-A PAGE LIMIT 60 LINES.
            01 DET TYPE DE LINE PLUS 1.
               03 COLUMN 1 PIC X(60) SOURCE WS-A.
            PROCEDURE DIVISION.
            DECLARATIVES.
            U SECTION.
                USE AFTER STANDARD EXCEPTION PROCEDURE ON PRT.
            U1.
                ADD 1 TO WS-USE.
            END DECLARATIVES.
            MAIN SECTION.
                OPEN OUTPUT PRT
                DISPLAY "OPEN FS=" FS " USE=" WS-USE
                INITIATE R-A
                PERFORM VARYING WS-N FROM 1 BY 1 UNTIL WS-N > 300
                    GENERATE DET
                    IF FS NOT = "00"
                        ADD 1 TO WS-BAD
                    END-IF
                END-PERFORM
                DISPLAY "GENERATE FS=" FS " USE=" WS-USE " NOT-00=" WS-BAD
                TERMINATE R-A
                DISPLAY "TERMINATE FS=" FS " USE=" WS-USE
                CLOSE PRT
                STOP RUN.
            """;
        var (ok, stdout, detail) = CobolNet2023.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(
            """
            OPEN FS=00 USE=0
            GENERATE FS=00 USE=0 NOT-00=0000
            TERMINATE FS=00 USE=0
            """.ReplaceLineEndings("\n"),
            stdout.ReplaceLineEndings("\n").TrimEnd('\n'));
    }
}
