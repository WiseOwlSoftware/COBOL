// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Report Writer subsystem (ISO/IEC 1989:2023 §13.14–§13.18 report description clauses; §14.9.21 INITIATE /
/// §14.9.16 GENERATE / §14.9.46 TERMINATE; §8.4.3.15 report counters; §14.9.49 Format 2 USE BEFORE REPORTING) —
/// the spec-pinned conformance net (COBOLNET_REPORT_WRITER_DESIGN §8). Every expected value derives from the
/// cited GR, NOT from the legacy oracle: the NIST RW goldens compare only the CCVS print file, never the RWCS
/// report file, and the LEGACY'S REPORT-FILE CONTENT IS DEMONSTRABLY WRONG in two places this net pins as fixed
/// (a §13.18.53.4 GR1 numeric SOURCE byte-copied left-justified instead of MOVEd through the printable PICTURE;
/// a blank <c>SOURCE LINE-COUNTER</c>). Counter behavior surfaces through MOVE-to-stdout; report CONTENT
/// surfaces by reading the report file back through a second line-sequential SELECT on the same ASSIGN target.
/// </summary>
public sealed class ReportWriterConformanceTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    /// <summary>Compile-and-run on the greenfield compiler; assert the spec-derived stdout.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"COBOL.NET failed: {detail}");
        Assert.Equal(expected, stdout);
    }

    /// <summary>A one-report program: <paramref name="rdAndGroups"/> is the REPORT SECTION text after
    /// <c>REPORT SECTION.</c>, <paramref name="ws"/> extra WORKING-STORAGE, <paramref name="proc"/> the
    /// PROCEDURE DIVISION body (the program supplies OPEN; tests append GENERATE/TERMINATE/read-back). A second
    /// SELECT (<c>RBACK</c>, line sequential) reads the report file back so tests can assert report CONTENT —
    /// the surface the NIST goldens never check.</summary>
    private static string Program(string rdAndGroups, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWTST.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "RPTF".
            SELECT RBACK ASSIGN TO "RPTF" ORGANIZATION LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD RPT
            REPORT IS R-1.
        FD RBACK.
        01 RB-REC PIC X(40).
        WORKING-STORAGE SECTION.
        01 LC-V PIC 9(3).
        01 PC-V PIC 9(3).
        {ws}
        REPORT SECTION.
        {rdAndGroups}
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT RPT.
        {proc}
        """;

    /// <summary>The read-back tail: skip blank (spacing) records, DISPLAY the first 4 characters of each
    /// non-blank report record (markers placed at columns 1–4 by the tests; a page-advance FORM FEED trails the
    /// page's last record, so 4-character markers keep it past the slice — <c>Normalize</c> would otherwise
    /// split on it).</summary>
    private const string ReadBack = """
            OPEN INPUT RBACK.
        RB-LOOP.
            READ RBACK AT END GO TO RB-DONE.
            IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
            GO TO RB-LOOP.
        RB-DONE.
            CLOSE RBACK.
            STOP RUN.
        """;

    /// <summary>The 5-character-slice read-back (sum-counter content "CF 03"; the tests using it produce a
    /// single page — no form-feed hazard).</summary>
    private const string ReadBack5 = """
            OPEN INPUT RBACK.
        RB-LOOP.
            READ RBACK AT END GO TO RB-DONE.
            IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:5).
            GO TO RB-LOOP.
        RB-DONE.
            CLOSE RBACK.
            STOP RUN.
        """;

    // ── INITIATE (§14.9.21.4 GR1) ────────────────────────────────────────────────────────────────────────────

    [Fact]   // GR1b/GR1c: INITIATE sets LINE-COUNTER to 0 and PAGE-COUNTER to 1.
    public void Initiate_Gr1_CountersZeroAndOne()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                MOVE LINE-COUNTER TO LC-V.
                MOVE PAGE-COUNTER TO PC-V.
                DISPLAY "LC=" LC-V " PC=" PC-V.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """), "LC=000 PC=001");

    // ── GENERATE placement + page advance (§14.9.16.4 GR4–GR6 / §13.18.35.4 GR4–GR6) ───────────────────────

    [Fact]   // §13.18.35.4 GR5b3 + GR4 exemption: the chronologically FIRST body group takes NO page-fit test
             // and, relative, lands at FIRST DETAIL (default = HEADING default = 1) — its PLUS 2 is IGNORED.
    public void FirstGenerate_Gr5b3_LandsAtFirstDetail_RelativeValueIgnored()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 5 LINES.
            01 DET-1 TYPE DE LINE PLUS 2.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE LINE-COUNTER TO LC-V.
                DISPLAY "LC=" LC-V.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """), "LC=001");

    [Fact]   // §13.18.35.4 GR4c page-fit (trial = LINE-COUNTER + Σ relative values vs LAST DETAIL) + §14.9.16.4
             // GR6d/e + GR5b3: PAGE 5 (LAST DETAIL defaults 5), LINE PLUS 2 ⇒ lines 1,3,5; the fourth GENERATE
             // (trial 5+2=7 > 5) page-advances: PAGE-COUNTER 2, the body lands at FIRST DETAIL 1.
    public void PageFit_Gr4c_TrialSumOverflow_AdvancesAndResets()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 5 LINES.
            01 DET-1 TYPE DE LINE PLUS 2.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                PERFORM GEN-SHOW 4 TIMES.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            GEN-SHOW.
                GENERATE DET-1.
                MOVE LINE-COUNTER TO LC-V.
                MOVE PAGE-COUNTER TO PC-V.
                DISPLAY "LC=" LC-V " PC=" PC-V.
            """), "LC=001 PC=001\nLC=003 PC=001\nLC=005 PC=001\nLC=001 PC=002");

    // ── §13.18.53.4 GR1 content (the two legacy report-file bugs, pinned FIXED) ─────────────────────────────

    [Fact]   // GR1: SOURCE is the sending operand of an implicit MOVE to the printable item — a PIC 9(3) item
             // sourcing a PIC 9(6) value 1 prints "001" (right-aligned numeric MOVE), NOT the legacy's "000"
             // (a left-justified byte copy); and SOURCE LINE-COUNTER prints the line number, NOT blank.
    public void Source_Gr1_ImplicitMoveEditsThroughPicture()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC 9(3) SOURCE IS WS-COUNTER.
            """, "01 WS-COUNTER PIC 9(6) VALUE 1.", """
                INITIATE R-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "001");

    [Fact]   // §13.18.35.4 GR6 + §13.18.53.4 GR3 (the load-bearing ordering): LINE-COUNTER is set to the line's
             // number BEFORE the line composes, so a detail's SOURCE LINE-COUNTER prints its OWN line number.
    public void Source_Gr6_LineCounterComposesAfterCounterUpdate()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES FIRST DETAIL 3.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC 99 SOURCE IS LINE-COUNTER.
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "03\n04");

    [Fact]   // §13.18.35.4 GR6 on a PAGE HEADING (the RW103A shape): the PH's SOURCE LINE-COUNTER prints the
             // PH's own absolute line, and the first detail of each page lands at FIRST DETAIL.
    public void PageHeading_Gr6_SourcesItsOwnLine()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 4.
            01 PH-1 TYPE PH LINE 2.
                03 COLUMN 1 PIC X(2) VALUE "PH".
                03 COLUMN 4 PIC 9 SOURCE IS LINE-COUNTER.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS LINE-COUNTER.
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "PH 2\nDE 3\nDE 4\nPH 2\nDE 3");

    // ── First-GENERATE sequence + heading/footing presentation (§14.9.16.4 GR4/GR6; §13.18.57.4 GR6) ───────

    [Fact]   // §14.9.16.4 GR4a/GR4b + GR6f: the report heading prints ONCE, before the first page's heading;
             // a page advance reprints the PH (GR6f) but never the RH.
    public void FirstGenerate_Gr4_RhOncePhPerPage()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 4.
            01 RH-1 TYPE RH LINE 1.
                03 COLUMN 1 PIC X(4) VALUE "RH-1".
            01 PH-1 TYPE PH LINE 2.
                03 COLUMN 1 PIC X(4) VALUE "PH-1".
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "DE-1".
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "RH-1\nPH-1\nDE-1\nDE-1\nPH-1\nDE-1");

    [Fact]   // §14.9.16.4 GR6a + §13.18.35.4 GR5b4: the page footing prints at the page advance (before the
             // physical advance), placed at FOOTING + integer-2 (relative) — and again on the LAST page at
             // TERMINATE (§13.18.57.4 GR6f), immediately followed by the report footing (GR3c).
    public void PageAdvance_Gr6a_PfThenNewPage_TerminatePfRf()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2 LAST DETAIL 4 FOOTING 6.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "DE-1".
            01 PF-1 TYPE PF LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "PF-1".
            01 RF-1 TYPE RF LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "RF-1".
            """, "", """
                INITIATE R-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "DE-1\nDE-1\nDE-1\nPF-1\nDE-1\nPF-1\nRF-1");

    // ── TERMINATE (§14.9.46.4 GR2/GR3) ───────────────────────────────────────────────────────────────────────

    [Fact]   // GR2: with NO GENERATE between INITIATE and TERMINATE, NO report group is processed — the report
             // file stays empty (PH/RF defined but never printed); the sole effect is active → inactive.
    public void Terminate_Gr2_NoGenerate_NothingPrints()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 PH-1 TYPE PH LINE 1.
                03 COLUMN 1 PIC X(2) VALUE "PH".
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
            01 RF-1 TYPE RF LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "RF".
            """, "", """
                INITIATE R-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
                READ RBACK AT END DISPLAY "EMPTY" GO TO RB-DONE.
                DISPLAY "NOT-EMPTY".
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """), "EMPTY");

    // ── CONTROL hierarchy (§13.18.16.4 GR3/GR4; §14.9.16.4 GR4c/GR5a; §14.9.46.4 GR3a/b) ────────────────────

    [Fact]   // §14.9.16.4 GR4c (CH on the first GENERATE) + GR5a (break: CF then CH) + §13.18.16.4 GR4a (the CF
             // composes with the PRIOR control value restored; the CH with the new) + §14.9.46.4 GR3b (TERMINATE
             // prints the final CF as a most-major break, with the last group's values).
    public void ControlBreak_Gr4a_CfSeesPriorValue_ChSeesNew()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 CH-1 TYPE CH WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CH".
                03 COLUMN 4 PIC 9 SOURCE IS WS-KEY.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-KEY.
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 9 SOURCE IS WS-KEY.
            """, "01 WS-KEY PIC 9 VALUE 1.", """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 2 TO WS-KEY.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack), "CH 1\nDE 1\nCF 1\nCH 2\nDE 2\nCF 2");

    // ── SUM counters (§13.18.54.4 GR2/GR3/GR7) ──────────────────────────────────────────────────────────────

    [Fact]   // GR7c1 (accumulate on every GENERATE) + GR2 (reset at the end of the group it prints in) + the
             // GR7 ordering (the CF printed at a break shows the ENDED group's total — accumulation of the
             // breaking GENERATE's addend happens after the break processing): amounts 1,2 then 4 across a
             // break print totals 03 then 04.
    public void Sum_Gr7_AccumulatesPerGenerate_ResetsWherePrinted()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 99 SUM WS-AMT.
            """, """
        01 WS-KEY PIC 9 VALUE 1.
        01 WS-AMT PIC 9 VALUE 1.
        """, """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 2 TO WS-AMT.
                GENERATE DET-1.
                MOVE 2 TO WS-KEY.
                MOVE 4 TO WS-AMT.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack5), "DE\nDE\nCF 03\nDE\nCF 04");

    [Fact]   // GR7c2: with an UPON phrase the addend accumulates ONLY on a GENERATE of the named detail.
    public void Sum_Gr7c2_UponRestrictsToNamedDetail()
        => AssertSpec(Program("""
            RD R-1 PAGE LIMIT IS 20 LINES CONTROL IS WS-KEY.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "D1".
            01 DET-2 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "D2".
            01 CF-1 TYPE CF WS-KEY LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "CF".
                03 COLUMN 4 PIC 99 SUM WS-AMT UPON DET-1.
            """, """
        01 WS-KEY PIC 9 VALUE 1.
        01 WS-AMT PIC 9 VALUE 5.
        """, """
                INITIATE R-1.
                GENERATE DET-1.
                MOVE 7 TO WS-AMT.
                GENERATE DET-2.
                TERMINATE R-1.
                CLOSE RPT.
            """ + ReadBack5), "D1\nD2\nCF 05");

    // ── USE BEFORE REPORTING (§14.9.49 Format 2 GR8) ────────────────────────────────────────────────────────

    [Fact]   // GR8: the declarative is invoked just before the named report group is produced — a MOVE in it is
             // visible to the group's SOURCE items (the PD stores 0; the declarative overrides to 7).
    public void UseBeforeReporting_Gr8_RunsBeforeEachProduction()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWTSTU.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RPT ASSIGN TO "RPTF".
                SELECT RBACK ASSIGN TO "RPTF" ORGANIZATION LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RPT
                REPORT IS R-1.
            FD RBACK.
            01 RB-REC PIC X(40).
            WORKING-STORAGE SECTION.
            01 WS-FLAG PIC 9 VALUE 0.
            REPORT SECTION.
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(2) VALUE "DE".
                03 COLUMN 4 PIC 9 SOURCE IS WS-FLAG.
            PROCEDURE DIVISION.
            DECLARATIVES.
            BR-SEC SECTION. USE BEFORE REPORTING DET-1.
            BR-PARA.
                MOVE 7 TO WS-FLAG.
            END DECLARATIVES.
            MAIN SECTION.
            MAIN-PARA.
                OPEN OUTPUT RPT.
                INITIATE R-1.
                MOVE 0 TO WS-FLAG.
                GENERATE DET-1.
                TERMINATE R-1.
                CLOSE RPT.
                OPEN INPUT RBACK.
            RB-LOOP.
                READ RBACK AT END GO TO RB-DONE.
                IF RB-REC NOT EQUAL TO SPACES DISPLAY RB-REC(1:4).
                GO TO RB-LOOP.
            RB-DONE.
                CLOSE RBACK.
                STOP RUN.
            """, "DE 7");

    // ── Counter referencing rules (§8.4.3.15) ───────────────────────────────────────────────────────────────

    [Fact]   // SR3: LINE-COUNTER shall not be referenced as a receiving operand — a bind-time rejection, never a
             // silently-dropped receiver.
    public void LineCounter_Sr3_ReceivingOperandRejected()
    {
        var (ok, _, detail) = CobolNet.CompileAndRun(Program("""
            RD R-1 PAGE LIMIT IS 10 LINES.
            01 DET-1 TYPE DE LINE PLUS 1.
                03 COLUMN 1 PIC X(4) VALUE "BODY".
            """, "", """
                INITIATE R-1.
                MOVE 5 TO LINE-COUNTER.
                TERMINATE R-1.
                CLOSE RPT.
                STOP RUN.
            """));
        Assert.False(ok, "MOVE … TO LINE-COUNTER must be rejected (ISO §8.4.3.15.3 SR3)");
        Assert.Contains("LINE-COUNTER", detail);
    }
}
