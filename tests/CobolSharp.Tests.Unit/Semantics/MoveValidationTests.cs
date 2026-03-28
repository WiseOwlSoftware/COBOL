// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Tests for MOVE validation checks added in BoundTreeBuilder:
/// - MOVE ZERO to Alphabetic (CBL0908)
/// - HIGH-VALUE/LOW-VALUE/QUOTE to Numeric (CBL0906)
/// - Numeric noninteger to Alphanumeric (CBL0907)
/// - Sign condition on non-numeric (CBL2606)
/// - CORRESPONDING excludes RENAMES (COBOL0414)
/// </summary>
public class MoveValidationTests : DiagnosticTestBase
{
    // ══════════════════════════════════════
    // CBL0908: MOVE ZERO to Alphabetic
    // ══════════════════════════════════════

    [Fact]
    public void CBL0908_MoveZeroToAlphabetic()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC A(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ZERO TO WS-ALPHA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0908");
    }

    [Fact]
    public void MoveSpaceToAlphabetic_NoError()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC A(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACE TO WS-ALPHA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL0908");
    }

    // ══════════════════════════════════════
    // CBL0906: HIGH-VALUE/LOW-VALUE/QUOTE to Numeric
    // ══════════════════════════════════════

    [Fact]
    public void CBL0906_MoveHighValueToNumeric()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE HIGH-VALUE TO WS-NUM.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0906");
    }

    [Fact]
    public void CBL0906_MoveLowValueToNumeric()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE LOW-VALUE TO WS-NUM.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0906");
    }

    [Fact]
    public void CBL0906_MoveQuoteToNumeric()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE QUOTE TO WS-NUM.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0906");
    }

    [Fact]
    public void MoveZeroToNumeric_NoError()
    {
        // ZERO to Numeric is legal
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ZERO TO WS-NUM.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL0906");
    }

    // ══════════════════════════════════════
    // CBL0907: Numeric noninteger to Alphanumeric
    // ══════════════════════════════════════

    [Fact]
    public void CBL0907_MoveNonintegerToAlphanumeric()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 3.14 TO WS-ALPHA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0907");
    }

    [Fact]
    public void MoveIntegerToAlphanumeric_NoError()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 42 TO WS-ALPHA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL0907");
    }

    // ══════════════════════════════════════
    // CBL2606: Sign condition on non-numeric
    // ══════════════════════════════════════

    [Fact]
    public void CBL2606_SignConditionOnAlphanumeric()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-ALPHA IS POSITIVE
               DISPLAY ""YES""
           END-IF.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2606");
    }

    [Fact]
    public void SignConditionOnNumeric_NoError()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC S9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-NUM IS POSITIVE
               DISPLAY ""YES""
           END-IF.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL2606");
    }

    // ══════════════════════════════════════
    // Alphabetic category MOVE compatibility
    // ══════════════════════════════════════

    [Fact]
    public void MoveAlphabeticToAlphanumeric_NoError()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC A(10).
       01 WS-ALPHANUM PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-ALPHA TO WS-ALPHANUM.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL0901");
    }

    [Fact]
    public void MoveNumericToAlphabetic_Error()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(5).
       01 WS-ALPHA PIC A(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-NUM TO WS-ALPHA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0901");
    }
}
