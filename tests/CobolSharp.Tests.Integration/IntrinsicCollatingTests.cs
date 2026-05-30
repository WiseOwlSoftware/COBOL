// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// FUNCTION CHAR / ORD under a program collating sequence (ISO/IEC 1989:2023 §15.15 CHAR,
/// §15.36 ORD — both use the alphanumeric program collating sequence; ordinal positions are
/// 1-based). With no PROGRAM COLLATING SEQUENCE the native ordinal order applies.
/// </summary>
public class IntrinsicCollatingTests : EndToEndTestBase
{
    // Under ALPHABET REV IS "B","A": weights B=0, A=1. So ORD("A")=2, ORD("B")=1 and the
    // inverse CHAR(1)="B", CHAR(2)="A". (CHAR returns the first code holding that position.)
    [Fact]
    public void CharOrd_HonorProgramCollatingSequence()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CHORDPCS.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            OBJECT-COMPUTER. X86
                PROGRAM COLLATING SEQUENCE IS REV-ORDER.
            SPECIAL-NAMES.
                ALPHABET REV-ORDER IS "B", "A".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-ORD-A   PIC 9(3).
            01 WS-ORD-B   PIC 9(3).
            01 WS-CHAR-1  PIC X(1).
            01 WS-CHAR-2  PIC X(1).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION ORD("A") TO WS-ORD-A.
                MOVE FUNCTION ORD("B") TO WS-ORD-B.
                MOVE FUNCTION CHAR(1)  TO WS-CHAR-1.
                MOVE FUNCTION CHAR(2)  TO WS-CHAR-2.
                DISPLAY "ORDA=" WS-ORD-A.
                DISPLAY "ORDB=" WS-ORD-B.
                DISPLAY "CH1=" WS-CHAR-1.
                DISPLAY "CH2=" WS-CHAR-2.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("ORDA=002", lines[0]); // A has weight 1 -> ordinal position 2
        Assert.Equal("ORDB=001", lines[1]); // B has weight 0 -> ordinal position 1
        Assert.Equal("CH1=B", lines[2]);    // position 1 (weight 0) is B
        Assert.Equal("CH2=A", lines[3]);    // position 2 (weight 1) is A
    }

    // Without a PROGRAM COLLATING SEQUENCE, CHAR/ORD use native (ASCII) ordinal order:
    // ORD("A") = 66 (code 65, 1-based), CHAR(66) = "A".
    [Fact]
    public void CharOrd_NativeWhenNoProgramCollatingSequence()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CHORDNAT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-ORD-A  PIC 9(3).
            01 WS-CHAR   PIC X(1).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION ORD("A")  TO WS-ORD-A.
                MOVE FUNCTION CHAR(66)  TO WS-CHAR.
                DISPLAY "ORDA=" WS-ORD-A.
                DISPLAY "CH=" WS-CHAR.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("ORDA=066", lines[0]); // native: code 65 -> ordinal 66
        Assert.Equal("CH=A", lines[1]);     // native: ordinal 66 -> code 65 = 'A'
    }
}
