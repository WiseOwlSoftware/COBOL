// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// SORT/MERGE collating-sequence tests (ISO/IEC 1989:2023 14.9.40 SORT, 14.9.22 MERGE).
/// The alphanumeric program collating sequence applies to alphanumeric sort/merge keys
/// unless a statement-level COLLATING SEQUENCE phrase overrides it. Numeric keys never collate.
/// </summary>
public class SortMergeCollatingTests : EndToEndTestBase
{
    // PROGRAM COLLATING SEQUENCE drives sort-key ordering: an alphabet that ranks
    // B before A must make an ascending SORT emit the B-keyed record first.
    [Fact]
    public void SortProgramCollatingSequence_ReversesKeyOrder()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CSORTPCS.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            OBJECT-COMPUTER. X86
                PROGRAM COLLATING SEQUENCE IS REV-ORDER.
            SPECIAL-NAMES.
                ALPHABET REV-ORDER IS "B", "A".
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "cpcsin"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "cpcsout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "cpcswk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-KEY PIC X(1).
               05 IN-DATA PIC X(4).
            FD OUT-FILE.
            01 OUT-REC PIC X(5).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(1).
               05 SORT-DATA PIC X(4).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "AXXXX" TO IN-REC.
                WRITE IN-REC.
                MOVE "BYYYY" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
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
        Assert.Equal(2, lines.Length);
        // REV-ORDER ranks B (weight 0) below A (weight 1): ascending => B-key first.
        Assert.Equal("BYYYY", lines[0]);
        Assert.Equal("AXXXX", lines[1]);
    }

    // A statement COLLATING SEQUENCE phrase overrides the program collating sequence.
    // PCS ranks B before A, but the phrase's FWD alphabet ranks A before B, so the
    // phrase wins and ascending order is native (A then B).
    [Fact]
    public void SortCollatingPhrase_OverridesProgramCollating()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CSORTOVR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            OBJECT-COMPUTER. X86
                PROGRAM COLLATING SEQUENCE IS REV-ORDER.
            SPECIAL-NAMES.
                ALPHABET REV-ORDER IS "B", "A"
                ALPHABET FWD-ORDER IS "A", "B".
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "covrin"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "covrout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "covrwk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-KEY PIC X(1).
               05 IN-DATA PIC X(4).
            FD OUT-FILE.
            01 OUT-REC PIC X(5).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(1).
               05 SORT-DATA PIC X(4).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "BYYYY" TO IN-REC.
                WRITE IN-REC.
                MOVE "AXXXX" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
                    COLLATING SEQUENCE IS FWD-ORDER
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
        Assert.Equal(2, lines.Length);
        // FWD-ORDER (phrase) overrides REV-ORDER (PCS): ascending => A-key first.
        Assert.Equal("AXXXX", lines[0]);
        Assert.Equal("BYYYY", lines[1]);
    }

    // Numeric sort keys compare by value and must ignore any collating sequence:
    // even under REV-ORDER, an ascending numeric sort orders 1 before 2.
    // (Regression guard for the BuildKeySpecField numeric-classification fix: keys are now
    // classified via the live ResolveLocation().GetPic() path, not the dead pic registry.)
    [Fact]
    public void SortNumericKey_IgnoresCollatingSequence()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CSORTNUM.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            OBJECT-COMPUTER. X86
                PROGRAM COLLATING SEQUENCE IS REV-ORDER.
            SPECIAL-NAMES.
                ALPHABET REV-ORDER IS "9", "8", "7", "6", "5", "4", "3", "2", "1", "0".
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "cnumin"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "cnumout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "cnumwk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-KEY PIC 9(1).
               05 IN-DATA PIC X(4).
            FD OUT-FILE.
            01 OUT-REC PIC X(5).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC 9(1).
               05 SORT-DATA PIC X(4).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "2BBBB" TO IN-REC.
                WRITE IN-REC.
                MOVE "1AAAA" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
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
        Assert.Equal(2, lines.Length);
        // Numeric key: value order wins regardless of REV-ORDER => 1 before 2.
        Assert.Equal("1AAAA", lines[0]);
        Assert.Equal("2BBBB", lines[1]);
    }

    // MERGE honors the program collating sequence for alphanumeric keys.
    [Fact]
    public void MergeProgramCollatingSequence_ReversesKeyOrder()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CMRGPCS.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            OBJECT-COMPUTER. X86
                PROGRAM COLLATING SEQUENCE IS REV-ORDER.
            SPECIAL-NAMES.
                ALPHABET REV-ORDER IS "B", "A".
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE-1 ASSIGN TO "cmrg1"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT IN-FILE-2 ASSIGN TO "cmrg2"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "cmrgout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT MERGE-FILE ASSIGN TO "cmrgwk".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE-1.
            01 IN-REC-1 PIC X(5).
            FD IN-FILE-2.
            01 IN-REC-2 PIC X(5).
            FD OUT-FILE.
            01 OUT-REC PIC X(5).
            SD MERGE-FILE.
            01 MERGE-REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE-1.
                MOVE "AXXXX" TO IN-REC-1.
                WRITE IN-REC-1.
                CLOSE IN-FILE-1.
                OPEN OUTPUT IN-FILE-2.
                MOVE "BYYYY" TO IN-REC-2.
                WRITE IN-REC-2.
                CLOSE IN-FILE-2.
                MERGE MERGE-FILE
                    ON ASCENDING KEY MERGE-REC
                    USING IN-FILE-1 IN-FILE-2
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
        Assert.Equal(2, lines.Length);
        // REV-ORDER ranks B below A: ascending merge => B-key first.
        Assert.Equal("BYYYY", lines[0]);
        Assert.Equal("AXXXX", lines[1]);
    }
}
