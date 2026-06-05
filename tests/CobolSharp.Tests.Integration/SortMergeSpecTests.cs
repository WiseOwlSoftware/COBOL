// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Spec-conformance tests for SORT/MERGE features the baselined NIST ST suite under-tests
/// (docs/SPEC_GAP_INVENTORY.md "## Sort-Merge"). Every [Fact] asserts output observed from the
/// CLI. All authority citations are ISO/IEC 1989:2023.
/// </summary>
public sealed class SortMergeSpecTests : EndToEndTestBase
{
    // §14.9.40.4 GR3a: when WITH DUPLICATES IN ORDER is specified and a USING input file feeds the
    // SORT, records whose keys are equal are returned in the order in which they were accessed from
    // the input file (input-file order), not in implementation-arbitrary order. The baselined ST127A
    // covers DUPLICATES only with an INPUT PROCEDURE; no baselined test covers DUPLICATES on a USING
    // file. Here key "A" records were written 1,3,4 and key "B" records 2,5; the duplicate "A" group
    // must emerge in released order A1,A3,A4 and the "B" group as B2,B5.
    [Fact]
    public void SortDuplicatesUsing_PreservesInputFileOrder()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SMDUP.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "smdupin"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "smdupout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "smdupwk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-KEY PIC X(1).
               05 IN-SEQ PIC X(1).
            FD OUT-FILE.
            01 OUT-REC PIC X(2).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(1).
               05 SORT-SEQ PIC X(1).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "A1" TO IN-REC.
                WRITE IN-REC.
                MOVE "B2" TO IN-REC.
                WRITE IN-REC.
                MOVE "A3" TO IN-REC.
                WRITE IN-REC.
                MOVE "A4" TO IN-REC.
                WRITE IN-REC.
                MOVE "B5" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
                    WITH DUPLICATES IN ORDER
                    USING IN-FILE
                    GIVING OUT-FILE.
                OPEN INPUT OUT-FILE.
                PERFORM READ-LOOP UNTIL WS-EOF = 1.
                CLOSE OUT-FILE.
                STOP RUN.
            READ-LOOP.
                READ OUT-FILE
                    AT END
                        MOVE 1 TO WS-EOF
                    NOT AT END
                        DISPLAY OUT-REC
                END-READ.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        // Major key ascending (A before B); within each key, duplicates in input-file order.
        Assert.Equal("A1", lines[0]);
        Assert.Equal("A3", lines[1]);
        Assert.Equal("A4", lines[2]);
        Assert.Equal("B2", lines[3]);
        Assert.Equal("B5", lines[4]);
    }

    // §14.9.40.4 GR2: the order of significance of keys is the order in which they are written, and
    // §14.9.40.4 GR1: ASCENDING/DESCENDING is transitive only until the next direction word. A SORT
    // with ON ASCENDING KEY major ON DESCENDING KEY minor must order the major key ascending and,
    // within equal majors, the minor key descending. No baselined ST test isolates a mixed-direction
    // multi-key SORT. Input pairs (A3,B1,A1,B2,A2) must yield A3,A2,A1 (A group, minor desc) then
    // B2,B1 (B group, minor desc).
    [Fact]
    public void SortMultiKey_AscendingMajorDescendingMinor()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SMMK.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "smmkin"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "smmkout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "smmkwk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-MAJ PIC X(1).
               05 IN-MIN PIC X(1).
            FD OUT-FILE.
            01 OUT-REC PIC X(2).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-MAJ PIC X(1).
               05 SORT-MIN PIC X(1).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "A3" TO IN-REC.
                WRITE IN-REC.
                MOVE "B1" TO IN-REC.
                WRITE IN-REC.
                MOVE "A1" TO IN-REC.
                WRITE IN-REC.
                MOVE "B2" TO IN-REC.
                WRITE IN-REC.
                MOVE "A2" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-MAJ
                    ON DESCENDING KEY SORT-MIN
                    USING IN-FILE
                    GIVING OUT-FILE.
                OPEN INPUT OUT-FILE.
                PERFORM READ-LOOP UNTIL WS-EOF = 1.
                CLOSE OUT-FILE.
                STOP RUN.
            READ-LOOP.
                READ OUT-FILE
                    AT END
                        MOVE 1 TO WS-EOF
                    NOT AT END
                        DISPLAY OUT-REC
                END-READ.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("A3", lines[0]);
        Assert.Equal("A2", lines[1]);
        Assert.Equal("A1", lines[2]);
        Assert.Equal("B2", lines[3]);
        Assert.Equal("B1", lines[4]);
    }

    // §14.9.40.4 GR5a: a statement-level COLLATING SEQUENCE phrase establishes the collating sequence
    // for alphanumeric key comparison, taking precedence over the program/native collating sequence.
    // No baselined ST program places the COLLATING SEQUENCE phrase on a Format-1 SORT (every
    // "COLLATING SEQUENCE" in ST108A/118A/127A is a MOVE literal). Here REV-ALPHA ranks Z..A so that
    // C < B < A; an ASCENDING SORT under that phrase must therefore emit C,B,A — the reverse of the
    // native order — proving the phrase, not native collating, drives the sort.
    [Fact]
    public void SortCollatingSequencePhrase_OverridesNativeOrder()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SMCOLL.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                ALPHABET REV-ALPHA IS "Z", "Y", "X", "C", "B", "A".
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "smcollin"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "smcollout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "smcollwk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC PIC X(2).
            FD OUT-FILE.
            01 OUT-REC PIC X(2).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(1).
               05 SORT-FIL PIC X(1).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "A1" TO IN-REC.
                WRITE IN-REC.
                MOVE "B2" TO IN-REC.
                WRITE IN-REC.
                MOVE "C3" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
                    COLLATING SEQUENCE IS REV-ALPHA
                    USING IN-FILE
                    GIVING OUT-FILE.
                OPEN INPUT OUT-FILE.
                PERFORM READ-LOOP UNTIL WS-EOF = 1.
                CLOSE OUT-FILE.
                STOP RUN.
            READ-LOOP.
                READ OUT-FILE
                    AT END
                        MOVE 1 TO WS-EOF
                    NOT AT END
                        DISPLAY OUT-REC
                END-READ.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        // REV-ALPHA ranks C below B below A: ascending => C, B, A (reverse of native).
        Assert.Equal("C3", lines[0]);
        Assert.Equal("B2", lines[1]);
        Assert.Equal("A1", lines[2]);
    }

    // §14.9.40.2 Format 2 (table) + §14.9.40.3 rules 13-14: SORT data-name-2 where data-name-2 has an
    // OCCURS clause sorts the table in place; the KEY is a data item subordinate to data-name-2. NIST
    // CCVS (a COBOL-85 suite) contains no Format-2 SORT, so this whole path is unbaselined. Here a
    // 5-element table of groups is sorted ON DESCENDING KEY on the numeric ENT-KEY subfield; the
    // elements must end up in descending key order 50,40,30,20,10 with their data carried along.
    [Fact]
    public void SortTableFormat2_GroupKey_Descending()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SMTBL2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 THE-TABLE.
               05 ENT OCCURS 5 TIMES.
                  10 ENT-KEY PIC 9(2).
                  10 ENT-DATA PIC X(3).
            01 IDX PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "30AAA" TO ENT(1).
                MOVE "10BBB" TO ENT(2).
                MOVE "50CCC" TO ENT(3).
                MOVE "20DDD" TO ENT(4).
                MOVE "40EEE" TO ENT(5).
                SORT ENT ON DESCENDING KEY ENT-KEY.
                PERFORM VARYING IDX FROM 1 BY 1 UNTIL IDX > 5
                    DISPLAY ENT(IDX)
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("50CCC", lines[0]);
        Assert.Equal("40EEE", lines[1]);
        Assert.Equal("30AAA", lines[2]);
        Assert.Equal("20DDD", lines[3]);
        Assert.Equal("10BBB", lines[4]);
    }

    // §14.9.40.2 Format 2 (table) with an alphanumeric subordinate key sorted ASCENDING. Complements
    // the descending/numeric Format-2 case above by exercising the alphanumeric collating path of the
    // in-place table sort. Input keys D,B,A,C must end up A,B,C,D with data carried along.
    [Fact]
    public void SortTableFormat2_AlphanumericKey_Ascending()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SMTBL4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 THE-TABLE.
               05 ENT OCCURS 4 TIMES.
                  10 ENT-KEY PIC X(1).
                  10 ENT-DATA PIC X(2).
            01 IDX PIC 9.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "D44" TO ENT(1).
                MOVE "B22" TO ENT(2).
                MOVE "A11" TO ENT(3).
                MOVE "C33" TO ENT(4).
                SORT ENT ON ASCENDING KEY ENT-KEY.
                PERFORM VARYING IDX FROM 1 BY 1 UNTIL IDX > 4
                    DISPLAY ENT(IDX)
                END-PERFORM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("A11", lines[0]);
        Assert.Equal("B22", lines[1]);
        Assert.Equal("C33", lines[2]);
        Assert.Equal("D44", lines[3]);
    }
}

