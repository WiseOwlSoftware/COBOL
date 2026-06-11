// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The LINAGE logical-page subsystem (ISO/IEC 1989:2023 §13.18.34 LINAGE clause, §8.4.3.14 LINAGE-COUNTER,
/// §14.9.51 WRITE GR25–GR28): the per-GR conformance net for the counter state machine, the GR26a/GR26b
/// end-of-page discrimination, and the GR6b data-name re-evaluation timing. Every behavioral test here is
/// SPEC-PINNED (expected values derived from the cited rules, not the legacy oracle): the legacy evaluates
/// LINAGE data-names ONLY at OPEN OUTPUT — a verified hole vs §13.18.34 GR6b2/GR6b3 (the SQ208M/SQ210M golden
/// re-baselines) — so it cannot be the authority for this subsystem. The physical stream is counter-only
/// (§13.18.34 GR8 — pages are contiguous, no margin spacing), so the observable surface is the LINAGE-COUNTER
/// register and the END-OF-PAGE branches, DISPLAYed to stdout.
/// </summary>
public sealed class LinageConformanceTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    /// <summary>Compile-and-run on the greenfield compiler; assert the spec-derived stdout.</summary>
    private static void AssertSpec(string source, string expected)
    {
        var (ok, stdout, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"COBOL.NET failed: {detail}");
        Assert.Equal(expected, stdout);
    }

    /// <summary>A one-LINAGE-file program: <paramref name="fdClauses"/> is the FD clause text (the LINAGE
    /// clause under test), <paramref name="ws"/> extra WORKING-STORAGE, <paramref name="proc"/> the COMPLETE
    /// procedure body (including CLOSE/STOP RUN, so tests may append helper paragraphs).</summary>
    private static string Program(string fdClauses, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. LNGTST.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT LPF ASSIGN TO "LNG-OUT".
        DATA DIVISION.
        FILE SECTION.
        FD LPF
            {fdClauses}.
        01 P-REC PIC X(20).
        WORKING-STORAGE SECTION.
        01 LC-VAL PIC 9(3).
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT LPF.
            MOVE SPACE TO P-REC.
        {proc}
        """;

    // ── GR7 counter rules (§13.18.34 GR7c1–c4 / GR7d) ─────────────────────────────────────────────────────

    [Fact]   // GR7d: LINAGE-COUNTER is set to one at OPEN OUTPUT.
    public void Gr7d_CounterIsOneAtOpenOutput()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=001");

    [Fact]   // GR7c3: a WRITE without the ADVANCING phrase increments the counter by one.
    public void Gr7c3_PlainWriteAddsOne()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=002");

    [Fact]   // GR7c2: WRITE ADVANCING n increments the counter by n.
    public void Gr7c2_AdvancingNAddsN()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 3 LINES.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=004");

    [Fact]   // §14.9.51 GR25c: ADVANCING 0 performs no repositioning; the counter gains 0 (GR7c2).
    public void Gr25c_AdvancingZero_CounterUnchanged()
        => AssertSpec(Program("LINAGE IS 5 LINES WITH FOOTING AT 4", "", """
                WRITE P-REC AFTER ADVANCING 0 LINES.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=001");

    [Fact]   // GR7c1: WRITE ADVANCING PAGE resets the counter to one.
    public void Gr7c1_AdvancingPageResetsCounterToOne()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 2 LINES.
                WRITE P-REC AFTER ADVANCING PAGE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "LC=001");

    [Fact]   // GR7c4 + §14.9.51 GR26a: crossing the page body repositions to LINE ONE of the next page —
             // counter := 1, never a modulo carry (8 past a 5-line body is 1, not 3) — with an overflow EOP.
    public void Gr7c4_OverflowCrossing_ResetsToOneNotModulo()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 3 LINES.
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP"
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "EOP\nLC=001");

    // ── GR26a vs GR26b end-of-page discrimination (§14.9.51) ──────────────────────────────────────────────

    [Fact]   // GR26b + GR27b: printing within the footing area is a FOOTING end-of-page — the AT branch reads
             // the POST-advance counter of the triggering write, and the counter is NOT reset (no overflow).
    public void Gr26b_FootingEop_AtBranchReadsPostAdvanceCounter()
        => AssertSpec(Program("LINAGE IS 5 LINES WITH FOOTING AT 4", "", """
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP MOVE LINAGE-COUNTER TO LC-VAL DISPLAY "EOP AT " LC-VAL
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "AFTER=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "EOP AT 004\nAFTER=004");

    [Fact]   // The footing area is [footing start, page size] INCLUSIVE (§13.18.34 GR3): a write landing
             // exactly ON the page size is a FOOTING end-of-page (counter stays), and overflow (GR26a) fires
             // only when the positioning actually PASSES the body (counter then resets to 1, GR7c4).
    public void Gr26ab_CounterEqualsBody_IsFootingEopNotOverflow()
        => AssertSpec(Program("LINAGE IS 5 LINES WITH FOOTING AT 4", "", """
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP1" END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                WRITE P-REC
                    AT EOP DISPLAY "EOP2" END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            """), "EOP1\nLC=005\nEOP2\nLC=001");

    [Fact]   // §13.18.34 GR1: no FOOTING phrase ⇒ no end-of-page condition independent of page overflow —
             // a write landing on the page size raises NOTHING (GR28: the NOT branch runs); only the
             // body-crossing write raises the (overflow) end-of-page.
    public void Gr1_NoFooting_EopIsOverflowOnly()
        => AssertSpec(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                WRITE P-REC
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """), "NO-EOP1\nEOP2");

    // ── GR6b data-name evaluation timing (§13.18.34 GR6b1/2/3 — the legacy's verified hole) ───────────────

    [Fact]   // GR6b1: a data-name operand's value is read at the COMPLETION of OPEN OUTPUT — the pre-open
             // MOVE governs the first page; a post-open MOVE has no effect until a page transition (GR6b
             // "the value applies to the next logical page").
    public void Gr6b1_DataNamesReadAtOpenOutput_MutationWaitsForTransition()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS WS-SIZE LINES.
            01 P-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 LC-VAL PIC 9(3).
            01 WS-SIZE PIC 99 VALUE 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 3 TO WS-SIZE.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                MOVE 99 TO WS-SIZE.
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                WRITE P-REC AFTER ADVANCING 4 LINES
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """,
            // Page 1 uses the open-time 3 (1+3 = 4 > 3 ⇒ overflow EOP, counter 1); the wrap re-evaluates
            // (GR6b3) ⇒ page 2 is 99 lines, so the 4-line advance stays inside it (NOT branch).
            "EOP1\nLC=001\nNO-EOP2");

    [Fact]   // GR6b3 + "applies to the next logical page": a mid-page MOVE never shrinks the CURRENT page —
             // the overflow decision is made against the OLD body, and the new values govern from the wrap on.
    public void Gr6b3_OverflowReEvaluation_AppliesToNextPage()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS WS-SIZE LINES.
            01 P-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-SIZE PIC 99 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                MOVE 2 TO WS-SIZE.
                WRITE P-REC AFTER ADVANCING 2 LINES
                    AT EOP DISPLAY "EOP1"
                    NOT AT EOP DISPLAY "NO-EOP1"
                END-WRITE.
                WRITE P-REC AFTER ADVANCING 3 LINES
                    AT EOP DISPLAY "EOP2"
                    NOT AT EOP DISPLAY "NO-EOP2"
                END-WRITE.
                WRITE P-REC AFTER ADVANCING 2 LINES
                    AT EOP DISPLAY "EOP3"
                    NOT AT EOP DISPLAY "NO-EOP3"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """,
            // 1+2 = 3 ≤ OLD body 5 ⇒ no EOP (the MOVE 2 did not shrink the current page); 3+3 = 6 > 5 ⇒
            // overflow against the OLD body, wrap to 1, re-evaluate ⇒ page = 2; 1+2 = 3 > 2 ⇒ overflow on
            // the NEW 2-line page.
            "NO-EOP1\nEOP2\nEOP3");

    [Fact]   // GR6b2: WRITE ADVANCING PAGE re-evaluates the data-names for the next logical page.
    public void Gr6b2_AdvancingPageReEvaluation()
        => AssertSpec($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS WS-SIZE LINES.
            01 P-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 WS-SIZE PIC 99 VALUE 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                MOVE 2 TO WS-SIZE.
                WRITE P-REC AFTER ADVANCING PAGE.
                WRITE P-REC AFTER ADVANCING 2 LINES
                    AT EOP DISPLAY "EOP"
                    NOT AT EOP DISPLAY "NO-EOP"
                END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """,
            // The PAGE write resets the counter (GR7c1) and re-reads the operands (GR6b2) ⇒ the next page is
            // 2 lines; 1+2 = 3 > 2 ⇒ overflow EOP. Without the re-evaluation it would be 3 ≤ 5 ⇒ no EOP.
            "EOP");

    // ── LINAGE-COUNTER as an operand (§8.4.3.14 SR1 — procedure-division references) ───────────────────────

    [Fact]   // The register in IF relations and PERFORM UNTIL conditions (plus MOVE — every test above).
    public void Counter_InIfAndPerformUntil()
        => AssertSpec(Program("LINAGE IS 9 LINES", "", """
                PERFORM FILL-LINE UNTIL LINAGE-COUNTER EQUAL 4.
                IF LINAGE-COUNTER EQUAL 4
                    DISPLAY "IF-OK"
                ELSE
                    DISPLAY "IF-BAD".
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LPF.
                STOP RUN.
            FILL-LINE.
                WRITE P-REC AFTER ADVANCING 1 LINE.
            """), "IF-OK\nLC=004");

    [Fact]   // §8.4.3.14 SR3 / §8.4.2.2: with more than one LINAGE file the register is QUALIFIED by
             // file-name — each file carries its own counter (§13.18.34 GR7a). No NIST coverage; spec-pinned.
    public void QualifiedCounter_TwoLinageFiles()
        => AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LP1 ASSIGN TO "LNG-Q1".
                SELECT LP2 ASSIGN TO "LNG-Q2".
            DATA DIVISION.
            FILE SECTION.
            FD LP1
                LINAGE IS 5 LINES.
            01 R1 PIC X(10).
            FD LP2
                LINAGE IS 7 LINES.
            01 R2 PIC X(10).
            WORKING-STORAGE SECTION.
            01 LC-VAL PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LP1 LP2.
                MOVE SPACE TO R1.
                MOVE SPACE TO R2.
                WRITE R1 AFTER ADVANCING 1 LINE.
                WRITE R2 AFTER ADVANCING 2 LINES.
                MOVE LINAGE-COUNTER OF LP1 TO LC-VAL.
                DISPLAY "P1=" LC-VAL.
                MOVE LINAGE-COUNTER IN LP2 TO LC-VAL.
                DISPLAY "P2=" LC-VAL.
                CLOSE LP1 LP2.
                STOP RUN.
            """, "P1=002\nP2=003");

    // ── Bind-time diagnostics (§14.9.51 SR13/SR18/SR19; §8.4.3.14 SR3) ─────────────────────────────────────

    [Fact]   // SR19 — THE silent-drop bug class: an END-OF-PAGE phrase on a file whose FD has no LINAGE
             // clause is a compile-time rejection, never a dropped branch.
    public void Sr19_EopWithoutLinage_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile(Program("RECORD CONTAINS 20 CHARACTERS", "", """
                WRITE P-REC AFTER ADVANCING 1 LINE
                    AT EOP DISPLAY "X" END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "SR19");
    }

    [Fact]   // SR18: ADVANCING PAGE and END-OF-PAGE shall not both appear in one WRITE.
    public void Sr18_AdvancingPageWithEop_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile(Program("LINAGE IS 5 LINES", "", """
                WRITE P-REC AFTER ADVANCING PAGE
                    AT EOP DISPLAY "X" END-WRITE.
                CLOSE LPF.
                STOP RUN.
            """), 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "SR18");
    }

    [Fact]   // SR13: a LINAGE file's ADVANCING phrase shall not name a SPECIAL-NAMES mnemonic.
    public void Sr13_MnemonicAdvancingOnLinageFile_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                VDEVICE IS FEED-MN.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LPF ASSIGN TO "LNG-OUT".
            DATA DIVISION.
            FILE SECTION.
            FD LPF
                LINAGE IS 5 LINES.
            01 P-REC PIC X(20).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LPF.
                MOVE SPACE TO P-REC.
                WRITE P-REC AFTER ADVANCING FEED-MN.
                CLOSE LPF.
                STOP RUN.
            """, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "SR13");
    }

    [Fact]   // §8.4.3.14 SR3 / §8.4.2.2: an UNQUALIFIED LINAGE-COUNTER is ambiguous when two files have
             // LINAGE clauses — rejected with a qualification diagnostic.
    public void UnqualifiedCounter_TwoLinageFiles_IsRejected()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LNGTST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LP1 ASSIGN TO "LNG-Q1".
                SELECT LP2 ASSIGN TO "LNG-Q2".
            DATA DIVISION.
            FILE SECTION.
            FD LP1
                LINAGE IS 5 LINES.
            01 R1 PIC X(10).
            FD LP2
                LINAGE IS 7 LINES.
            01 R2 PIC X(10).
            WORKING-STORAGE SECTION.
            01 LC-VAL PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LP1 LP2.
                MOVE LINAGE-COUNTER TO LC-VAL.
                DISPLAY "LC=" LC-VAL.
                CLOSE LP1 LP2.
                STOP RUN.
            """, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "8.4.3.14");
    }
}
