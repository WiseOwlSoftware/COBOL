// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Subscripted Tier-B REDEFINES views (GAP-1) + the B2 layout accounting (ISO §13.18.44: a redefined table lays
/// its occurrences end-to-end in the ONE backing; an inner REDEFINES starts at its target's first position and
/// adds no width; a sibling table contributes width × OCCURS), plus NEXT SENTENCE (§14.9.19 GR6). Pinned to the
/// legacy oracle (NIST-85 green over the REDEFINES table series).
/// </summary>
public sealed class RedefinesSubscriptedViewDifferentialTests
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

    [Fact]
    public void TableRedefinesPicture_SubscriptedElementReads()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TABLE-A PIC X(10) VALUE "0102030405".
            01 TABLE-1 REDEFINES TABLE-A.
               02 ELM PIC 99 OCCURS 5 TIMES INDEXED BY IX-1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY ELM(1) " " ELM(3) " " ELM(5).
                SET IX-1 TO 4.
                DISPLAY ELM(IX-1).
                STOP RUN.
            """);

    [Fact]
    public void SubscriptedViewWrite_VisibleThroughBacking()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TABLE-A PIC X(10) VALUE "AABBCCDDEE".
            01 TABLE-1 REDEFINES TABLE-A.
               02 ELM PIC XX OCCURS 5 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "ZZ" TO ELM(3).
                DISPLAY TABLE-A.
                STOP RUN.
            """);

    [Fact]
    public void NestedOccursInRedefines_TwoSubscripts()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TABLE-A PIC X(12) VALUE "ABCDEFGHIJKL".
            01 TABLE-1 REDEFINES TABLE-A.
               02 ROW-G OCCURS 2 TIMES.
                  03 CEL PIC XX OCCURS 3 TIMES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY CEL(1, 1) " " CEL(1, 3) " " CEL(2, 1) " " CEL(2, 3).
                STOP RUN.
            """);

    [Fact]
    public void SiblingAfterTable_OffsetCountsAllOccurrences()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 AREA-A PIC X(8) VALUE "11223344".
            01 AREA-1 REDEFINES AREA-A.
               02 T PIC XX OCCURS 3 TIMES.
               02 TAIL-X PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY TAIL-X.
                STOP RUN.
            """);

    /// <summary>NEXT SENTENCE transfers to the implicit CONTINUE after the current sentence's period (§14.9.19
    /// GR6): the TRUE branch's trailing statements (same sentence, no END-IF in '85) are skipped; the FOLLOWING
    /// sentence runs. Both branches exercised.</summary>
    [Fact]
    public void NextSentence_SkipsTrailOfOwnSentence()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RSV5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC 9 VALUE 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF W = 1 NEXT SENTENCE
                    DISPLAY "TRUE-TAIL".
                DISPLAY "SENTENCE-2".
                MOVE 2 TO W.
                IF W = 1 NEXT SENTENCE ELSE DISPLAY "ELSE-ARM".
                DISPLAY "SENTENCE-4".
                STOP RUN.
            """);
}
