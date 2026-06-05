// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// WS-SPEC conformance tests for the Report Writer module (RWCS), ISO/IEC 1989:2023 §13.18.46 (REPORT
/// clause) and §14.9.16/§14.9.21/§14.9.46 (GENERATE / INITIATE / TERMINATE). These exercise features the
/// baselined NIST RW-suite (RW101A–RW104A) does not output-verify: the NIST RW programs compare only their
/// DISPLAY-based CCVS audit report, never the bytes of the RWCS report file itself. Each test here drives the
/// RWCS to write its report to a sequential file, then re-OPENs that same file as LINE SEQUENTIAL and reads
/// it back, DISPLAYing each record wrapped in <c>[...]</c> so the report content becomes deterministic stdout
/// (the established write-then-read-back pattern of <see cref="FileIOTests"/>). The leading <c>[]</c> line is
/// the RWCS line-buffer's initial blank advance.
/// </summary>
public sealed class ReportWriterSpecTests : EndToEndTestBase
{
    private static string[] Lines(string stdout) =>
        stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// §14.9.21/§14.9.16/§14.9.46: INITIATE then two GENERATE of a DETAIL group then TERMINATE. The DETAIL
    /// group sources a working-storage alphanumeric item by COLUMN; each GENERATE presents one detail line.
    /// </summary>
    [Fact]
    public void Detail_InitiateGenerateTerminate_PresentsEachDetailLine()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWG1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWG1OUT".
                SELECT IN-FILE ASSIGN TO "RWG1OUT"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-LINE PIC X(11) VALUE "DETAIL-LINE".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT PAGE LIMIT IS 20 LINES.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(11) SOURCE WS-LINE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                GENERATE DET.
                GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = Lines(stdout);
        Assert.Equal(3, lines.Length);
        Assert.Equal("[]", lines[0]);
        Assert.Equal("[DETAIL-LINE]", lines[1]);
        Assert.Equal("[DETAIL-LINE]", lines[2]);
    }

    /// <summary>
    /// §13.18.46 + §13.18.x DETAIL group with multiple elementary fields each at its own COLUMN: an
    /// alphanumeric SOURCE at column 1 (width 5) and a numeric SOURCE at column 10 (width 3, zero-padded).
    /// Verifies independent left-to-right placement by COLUMN within a single detail line.
    /// </summary>
    [Fact]
    public void Detail_MultipleSourceFields_PlacedByColumn()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWG4.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWG4OUT".
                SELECT IN-FILE ASSIGN TO "RWG4OUT"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(60).
            WORKING-STORAGE SECTION.
            01  WS-NAME PIC X(5) VALUE "ALPHA".
            01  WS-NUM  PIC 9(3) VALUE 042.
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT PAGE LIMIT IS 20 LINES.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1  PIC X(5) SOURCE WS-NAME.
                03 COLUMN 10 PIC 9(3) SOURCE WS-NUM.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                GENERATE DET.
                GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = Lines(stdout);
        Assert.Equal(3, lines.Length);
        Assert.Equal("[]", lines[0]);
        // "ALPHA" at col 1 (5 wide), "042" at col 10 -> cols 6-9 blank (4 spaces).
        Assert.Equal("[ALPHA    042]", lines[1]);
        Assert.Equal("[ALPHA    042]", lines[2]);
    }

    /// <summary>
    /// §14.9.21 GR2/GR3 (PAGE-COUNTER set to 1 by INITIATE) + a TYPE PAGE HEADING group whose fields are a
    /// VALUE literal and a SOURCE PAGE-COUNTER special register. The RWCS auto-presents the page heading at the
    /// chronologically first GENERATE; the heading line must read "PAGE-01" (PAGE-COUNTER = 1, PIC 9(2)).
    /// </summary>
    [Fact]
    public void PageHeading_AutoPresented_WithPageCounterSource()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWG3.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWG3OUT".
                SELECT IN-FILE ASSIGN TO "RWG3OUT"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-LINE PIC X(10) VALUE "DETAIL-ROW".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT PAGE LIMIT IS 20 LINES.
            01  PH-GRP TYPE IS PAGE HEADING.
                03 LINE NUMBER IS 1.
                   05 COLUMN 1 PICTURE X(5) VALUE "PAGE-".
                   05 COLUMN 6 PICTURE 9(2) SOURCE PAGE-COUNTER.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(10) SOURCE WS-LINE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                GENERATE DET.
                GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = Lines(stdout);
        Assert.Equal(4, lines.Length);
        Assert.Equal("[]", lines[0]);
        Assert.Equal("[PAGE-01]", lines[1]);
        Assert.Equal("[DETAIL-ROW]", lines[2]);
        Assert.Equal("[DETAIL-ROW]", lines[3]);
    }

    /// <summary>
    /// Page mechanics (§13.18.x PAGE clause HEADING / FIRST DETAIL / LAST DETAIL / FOOTING + §14.9.21 GR2
    /// PAGE-COUNTER updated +1 per page advance). PAGE LIMIT 6 with HEADING 1, FIRST DETAIL 2, LAST DETAIL 4,
    /// FOOTING 5: three details fit per page (lines 2-4); the 4th GENERATE overflows LAST DETAIL, so the RWCS
    /// presents the PAGE FOOTING, advances to a fresh page (PAGE-COUNTER 1 -> 2), re-presents the PAGE HEADING,
    /// and resumes details at FIRST DETAIL. Verifies the full heading/detail/footing presentation order and the
    /// page-counter increment across the break.
    /// </summary>
    [Fact]
    public void PageMechanics_HeadingFootingOverflow_AdvancesPageAndIncrementsCounter()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWG2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWG2OUT".
                SELECT IN-FILE ASSIGN TO "RWG2OUT"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-LINE PIC X(3) VALUE "DET".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT
                PAGE LIMIT IS 6 LINES
                HEADING 1
                FIRST DETAIL 2
                LAST DETAIL 4
                FOOTING 5.
            01  PH-GRP TYPE IS PAGE HEADING.
                03 LINE NUMBER IS 1.
                   05 COLUMN 1 PIC X(5) VALUE "PAGE-".
                   05 COLUMN 6 PIC 9(1) SOURCE PAGE-COUNTER.
            01  PF-GRP TYPE IS PAGE FOOTING.
                03 LINE NUMBER IS 5 COLUMN 1 PIC X(3) VALUE "END".
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PIC X(3) SOURCE WS-LINE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                PERFORM 5 TIMES
                    GENERATE DET
                END-PERFORM.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = Lines(stdout);
        Assert.Equal(9, lines.Length);
        Assert.Equal("[]", lines[0]);
        Assert.Equal("[PAGE-1]", lines[1]);   // page 1 heading
        Assert.Equal("[DET]", lines[2]);       // FIRST DETAIL (line 2)
        Assert.Equal("[DET]", lines[3]);
        Assert.Equal("[DET]", lines[4]);       // LAST DETAIL (line 4)
        Assert.Equal("[END]", lines[5]);       // page 1 footing (line 5)
        Assert.Equal("[PAGE-2]", lines[6]);    // page 2 heading, PAGE-COUNTER incremented
        Assert.Equal("[DET]", lines[7]);
        Assert.Equal("[DET]", lines[8]);
    }

    /// <summary>
    /// §13.18.46 REPORT(S) clause plural form: one FD with REPORTS ARE r1 r2 binds two independent reports.
    /// INITIATE both, GENERATE a detail in each, TERMINATE both; both reports' detail lines must appear in the
    /// single output file in write order, each driven by its own RD.
    /// </summary>
    [Fact]
    public void TwoReportsOnOneFile_BothPresentDetails()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWG5.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWG5OUT".
                SELECT IN-FILE ASSIGN TO "RWG5OUT"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORTS ARE RPT-A RPT-B.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-A PIC X(8) VALUE "AAA".
            01  WS-B PIC X(8) VALUE "BBB".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  RPT-A PAGE LIMIT IS 20 LINES.
            01  DA TYPE IS DETAIL.
                03 LINE NUMBER IS PLUS 1 COLUMN 1 PIC X(8) SOURCE WS-A.
            RD  RPT-B PAGE LIMIT IS 20 LINES.
            01  DB TYPE IS DETAIL.
                03 LINE NUMBER IS PLUS 1 COLUMN 1 PIC X(8) SOURCE WS-B.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE RPT-A RPT-B.
                GENERATE DA.
                GENERATE DB.
                TERMINATE RPT-A RPT-B.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = Lines(stdout);
        Assert.Equal(3, lines.Length);
        Assert.Equal("[]", lines[0]);
        Assert.Equal("[AAA]", lines[1]);
        Assert.Equal("[BBB]", lines[2]);
    }

    /// <summary>
    /// §13.18.63 — a VALUE literal in a body (DETAIL) group is a constant printable field. BuildReportLines
    /// skipped every field whose SOURCE was null, so a VALUE-literal column was dropped; it is now placed.
    /// Here a constant "ID: " precedes a working-storage SOURCE in the same detail line.
    /// </summary>
    [Fact]
    public void Detail_ValueLiteralField_IsPlaced()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWVL.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWVLOUT".
                SELECT IN-FILE ASSIGN TO "RWVLOUT"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-ID PIC X(3) VALUE "ABC".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT PAGE LIMIT IS 20 LINES.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(4) VALUE "ID: ".
                03 COLUMN 5 PICTURE X(3) SOURCE WS-ID.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = Lines(stdout);
        Assert.Equal(2, lines.Length);
        Assert.Equal("[]", lines[0]);
        Assert.Equal("[ID: ABC]", lines[1]);
    }

    /// <summary>
    /// §14.9.16.4 GR4a / §13.18.57 GR6g — TYPE REPORT HEADING is auto-presented once at the first GENERATE
    /// (before any PAGE HEADING) and TYPE REPORT FOOTING at TERMINATE (after all footings). The lowerer/runtime
    /// registered only PAGE heading/footing; this generalizes the auto-presented-group mechanism to 4 slots.
    /// </summary>
    [Fact]
    public void ReportHeadingAndFooting_ArePresented()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWRHRF.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWRHRFO".
                SELECT IN-FILE ASSIGN TO "RWRHRFO"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-ID PIC X(3) VALUE "ABC".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT PAGE LIMIT IS 20 LINES.
            01  RPT-HEAD TYPE IS REPORT HEADING.
                02 LINE NUMBER IS 1.
                   03 COLUMN 1 PICTURE X(5) VALUE "TITLE".
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(3) SOURCE WS-ID.
            01  RPT-FOOT TYPE IS REPORT FOOTING.
                02 LINE NUMBER IS 3.
                   03 COLUMN 1 PICTURE X(3) VALUE "END".
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var joined = string.Join("|", Lines(stdout));
        int iTitle = joined.IndexOf("TITLE", StringComparison.Ordinal);
        int iAbc = joined.IndexOf("ABC", StringComparison.Ordinal);
        int iEnd = joined.IndexOf("END", StringComparison.Ordinal);
        Assert.True(iTitle >= 0, $"REPORT HEADING not presented: {joined}");
        Assert.True(iEnd >= 0, $"REPORT FOOTING not presented: {joined}");
        // RH precedes the detail; RF follows it (ISO §14.9.16.4 GR4a / §13.18.57 GR6g).
        Assert.True(iTitle < iAbc && iAbc < iEnd, $"wrong presentation order: {joined}");
    }

    /// <summary>
    /// §13.18.53 — a SOURCE of arbitrary working-storage data in an auto-presented group (PAGE HEADING here)
    /// is read live at presentation time. ClassifyPageField returned kind 3 (blank-fill) for any non-counter
    /// SOURCE; the auto-group field plan now keeps the storage location and the runtime reads it when it
    /// presents the group.
    /// </summary>
    [Fact]
    public void PageHeading_DataSource_IsPresented()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPHSRC.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWPHSRCO".
                SELECT IN-FILE ASSIGN TO "RWPHSRCO"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-HDR PIC X(5) VALUE "PAGE!".
            01  WS-ID PIC X(3) VALUE "ABC".
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT PAGE LIMIT IS 20 LINES.
            01  PG-HEAD TYPE IS PAGE HEADING.
                02 LINE NUMBER IS 1.
                   03 COLUMN 1 PICTURE X(5) SOURCE WS-HDR.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(3) SOURCE WS-ID.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var joined = string.Join("|", Lines(stdout));
        int iHdr = joined.IndexOf("PAGE!", StringComparison.Ordinal);
        int iAbc = joined.IndexOf("ABC", StringComparison.Ordinal);
        Assert.True(iHdr >= 0, $"PAGE HEADING data SOURCE not presented (was blank): {joined}");
        Assert.True(iHdr < iAbc, $"page heading should precede the detail: {joined}");
    }

    /// <summary>
    /// §13.18.16 / §14.9.16.4 / §13.18.57 — CONTROL break detection + CONTROL FOOTING presentation. A change in
    /// the CONTROL data item between GENERATEs ends the current control group: its CONTROL FOOTING is presented
    /// (showing the ENDING group's value — the runtime restores the prior control value before presenting it),
    /// and a final break at TERMINATE presents the last group's footing. The break/footing machinery was in the
    /// symbol model but never honored.
    /// </summary>
    [Fact]
    public void ControlBreak_ControlFooting_IsPresentedWithEndingValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWCTL.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWCTLO".
                SELECT IN-FILE ASSIGN TO "RWCTLO"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-DEPT PIC X(1).
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT CONTROL IS WS-DEPT PAGE LIMIT IS 60 LINES.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(1) SOURCE WS-DEPT.
            01  CF-DEPT TYPE IS CONTROL FOOTING WS-DEPT LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(3) VALUE "ZF-".
                03 COLUMN 5 PICTURE X(1) SOURCE WS-DEPT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                MOVE "A" TO WS-DEPT. GENERATE DET.
                MOVE "A" TO WS-DEPT. GENERATE DET.
                MOVE "B" TO WS-DEPT. GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var joined = string.Join("|", Lines(stdout));
        int iCfA = joined.IndexOf("ZF- A", StringComparison.Ordinal);   // footing for dept A (prior-restore)
        int iCfB = joined.IndexOf("ZF- B", StringComparison.Ordinal);   // footing for dept B (at TERMINATE)
        Assert.True(iCfA >= 0, $"CONTROL FOOTING for dept A not presented with the ending value: {joined}");
        Assert.True(iCfB >= 0, $"CONTROL FOOTING at TERMINATE not presented: {joined}");
        Assert.True(iCfA < iCfB, $"dept-A footing should precede the TERMINATE footing: {joined}");
    }

    /// <summary>
    /// §13.18.54 — a SUM counter in a CONTROL FOOTING accumulates a data item over the details of its control
    /// group and prints the subtotal at the break, then resets (GR2). The accumulation happens after the
    /// control-break processing so a footing sums the details that preceded the break (GR7). This is the
    /// canonical control-break subtotal report.
    /// </summary>
    [Fact]
    public void ControlFooting_SumCounter_SubtotalsAndResets()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWSUM.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT-FILE ASSIGN TO "RWSUMO".
                SELECT IN-FILE ASSIGN TO "RWSUMO"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  RPT-FILE
                REPORT IS THE-REPORT.
            FD  IN-FILE.
            01  IN-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01  WS-DEPT PIC X(1).
            01  WS-AMT  PIC 9(3).
            01  WS-EOF PIC X VALUE "N".
            REPORT SECTION.
            RD  THE-REPORT CONTROL IS WS-DEPT PAGE LIMIT IS 60 LINES.
            01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(1) SOURCE WS-DEPT.
                03 COLUMN 3 PICTURE 9(3) SOURCE WS-AMT.
            01  CF-DEPT TYPE IS CONTROL FOOTING WS-DEPT LINE NUMBER IS PLUS 1.
                03 COLUMN 1 PICTURE X(2) VALUE "T=".
                03 COLUMN 3 PICTURE 9(4) SUM WS-AMT.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RPT-FILE.
                INITIATE THE-REPORT.
                MOVE "A" TO WS-DEPT. MOVE 010 TO WS-AMT. GENERATE DET.
                MOVE "A" TO WS-DEPT. MOVE 020 TO WS-AMT. GENERATE DET.
                MOVE "B" TO WS-DEPT. MOVE 005 TO WS-AMT. GENERATE DET.
                TERMINATE THE-REPORT.
                CLOSE RPT-FILE.
                OPEN INPUT IN-FILE.
                PERFORM UNTIL WS-EOF = "Y"
                    READ IN-FILE
                        AT END MOVE "Y" TO WS-EOF
                        NOT AT END DISPLAY "[" IN-REC "]"
                    END-READ
                END-PERFORM.
                CLOSE IN-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var joined = string.Join("|", Lines(stdout));
        int iTa = joined.IndexOf("T=0030", StringComparison.Ordinal);   // dept A subtotal 10+20
        int iTb = joined.IndexOf("T=0005", StringComparison.Ordinal);   // dept B subtotal 5 (at TERMINATE; proves reset)
        Assert.True(iTa >= 0, $"CONTROL FOOTING SUM for dept A (=0030) not presented: {joined}");
        Assert.True(iTb >= 0, $"CONTROL FOOTING SUM for dept B (=0005) at TERMINATE not presented: {joined}");
        Assert.True(iTa < iTb, $"dept-A subtotal should precede dept-B subtotal: {joined}");
    }
}

