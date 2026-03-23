// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Tests for BoundTreeValidator: expression type enforcement on
/// PERFORM, IF, and EVALUATE statements.
/// </summary>
public class BoundTreeValidatorTests : DiagnosticTestBase
{
    // ═══════════════════════════════════════════════════════════════
    // Valid programs — no CBL23xx/24xx/25xx diagnostics
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidProgram_NoControlFlowDiagnostics()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 0.
       01 WS-B PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-A = 0
               DISPLAY ""ZERO""
           END-IF.
           PERFORM WORK-PARA.
           PERFORM WORK-PARA 3 TIMES.
           PERFORM WORK-PARA UNTIL WS-A > WS-B.
           EVALUATE WS-A
               WHEN 0
                   DISPLAY ""NONE""
               WHEN OTHER
                   DISPLAY ""SOME""
           END-EVALUATE.
           STOP RUN.
       WORK-PARA.
           ADD 1 TO WS-A.
";
        var diags = GetDiagnostics(source);
        // No PERFORM errors
        AssertNoDiagnostic(diags, "CBL2303");
        AssertNoDiagnostic(diags, "CBL2304");
        AssertNoDiagnostic(diags, "CBL2305");
        AssertNoDiagnostic(diags, "CBL2306");
        AssertNoDiagnostic(diags, "CBL2307");
        AssertNoDiagnostic(diags, "CBL2308");
        // No IF errors
        AssertNoDiagnostic(diags, "CBL2401");
        // No EVALUATE errors
        AssertNoDiagnostic(diags, "CBL2501");
        AssertNoDiagnostic(diags, "CBL2502");
        AssertNoDiagnostic(diags, "CBL2503");
    }

    [Fact]
    public void ValidPerformVarying_NoDiagnostics()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-IDX PIC 9(3) VALUE 0.
       01 WS-MAX PIC 9(3) VALUE 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM WORK-PARA
               VARYING WS-IDX FROM 1 BY 1
               UNTIL WS-IDX > WS-MAX.
           STOP RUN.
       WORK-PARA.
           DISPLAY WS-IDX.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL2305");
        AssertNoDiagnostic(diags, "CBL2306");
        AssertNoDiagnostic(diags, "CBL2307");
        AssertNoDiagnostic(diags, "CBL2308");
    }

    [Fact]
    public void ValidEvaluateTrue_NoDiagnostics()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE TRUE
               WHEN WS-A > 3
                   DISPLAY ""BIG""
               WHEN OTHER
                   DISPLAY ""SMALL""
           END-EVALUATE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL2503");
        AssertNoDiagnostic(diags, "CBL2502");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL2502: EVALUATE missing WHEN OTHER (warning)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL2502_EvaluateMissingWhenOther()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE WS-A
               WHEN 1
                   DISPLAY ""ONE""
               WHEN 2
                   DISPLAY ""TWO""
           END-EVALUATE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2502");
    }

    [Fact]
    public void CBL2502_EvaluateTrueMissingWhenOther()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE TRUE
               WHEN WS-A > 3
                   DISPLAY ""BIG""
           END-EVALUATE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2502");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL2302: PERFORM THRU out of order
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL2302_PerformThruOutOfOrder()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM PARA-B THRU PARA-A.
           STOP RUN.
       PARA-A.
           DISPLAY ""A"".
       PARA-B.
           DISPLAY ""B"".
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2302");
    }

    [Fact]
    public void PerformThruInOrder_NoDiagnostic()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM PARA-A THRU PARA-B.
           STOP RUN.
       PARA-A.
           DISPLAY ""A"".
       PARA-B.
           DISPLAY ""B"".
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL2302");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL2303: PERFORM TIMES must be numeric
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL2303_PerformTimesNonNumeric()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC X(5) VALUE ""HELLO"".
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM WORK-PARA WS-ALPHA TIMES.
           STOP RUN.
       WORK-PARA.
           DISPLAY ""WORK"".
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2303");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL2305: PERFORM VARYING control must be numeric
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL2305_PerformVaryingNonNumericControl()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM WORK-PARA
               VARYING WS-ALPHA FROM 1 BY 1
               UNTIL WS-ALPHA > 10.
           STOP RUN.
       WORK-PARA.
           DISPLAY ""WORK"".
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2305");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL2501: EVALUATE WHEN type incompatible with subject
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL2501_EvaluateWhenTypeMismatch()
    {
        // Use data reference (not literal) so Typed<T> sets ResultType
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9 VALUE 5.
       01 WS-ALPHA PIC X(3) VALUE ""ABC"".
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE WS-NUM
               WHEN WS-ALPHA
                   DISPLAY ""MATCH""
               WHEN OTHER
                   DISPLAY ""OTHER""
           END-EVALUATE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2501");
    }

    // ═══════════════════════════════════════════════════════════════
    // Intentionally unreachable diagnostics
    // ═══════════════════════════════════════════════════════════════
    // CBL2304 (PERFORM UNTIL boolean): BindCondition always produces boolean
    // CBL2306 (VARYING FROM numeric): grammar enforces arithmeticExpression
    // CBL2307 (VARYING BY numeric): grammar enforces arithmeticExpression
    // CBL2308 (VARYING UNTIL boolean): BindCondition always produces boolean
    // CBL2401 (IF condition boolean): BindCondition always produces boolean
    // CBL2402 (comparison operands incompatible): COBOL allows cross-type comparisons
    // CBL2503 (EVALUATE TRUE WHEN boolean): BindCondition always produces boolean
    //
    // These checks remain as safety nets for future binder changes that
    // might relax the boolean/numeric enforcement at binding time.

    // ═══════════════════════════════════════════════════════════════
    // File I/O organization enforcement
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidFileIO_NoDiagnostics()
    {
        // OPEN/READ/WRITE on sequential file — all legal
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE
               AT END MOVE 1 TO WS-EOF
           END-READ.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL1601");
        AssertNoDiagnostic(diags, "CBL1901");
        AssertNoDiagnostic(diags, "CBL2001");
    }

    [Fact]
    public void CBL1601_StartOnSequentialFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O SEQ-FILE.
           START SEQ-FILE.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL1601");
    }

    [Fact]
    public void CBL1901_RewriteOnSequentialFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O SEQ-FILE.
           REWRITE SEQ-REC.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL1901");
    }

    [Fact]
    public void CBL2001_DeleteOnSequentialFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O SEQ-FILE.
           DELETE SEQ-FILE.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2001");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL0701: OPEN EXTEND on non-sequential file
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL0701_OpenExtendOnIndexedFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO ""IDX.DAT""
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IDX-FILE.
       01 IDX-REC.
          05 IDX-KEY PIC X(10).
          05 IDX-DATA PIC X(70).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN EXTEND IDX-FILE.
           CLOSE IDX-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0701");
    }

    [Fact]
    public void CBL0701_OpenExtendOnRelativeFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO ""REL.DAT""
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC.
       DATA DIVISION.
       FILE SECTION.
       FD REL-FILE.
       01 REL-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN EXTEND REL-FILE.
           CLOSE REL-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0701");
    }

    [Fact]
    public void ValidOpenExtendOnSequentialFile_NoDiagnostic()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN EXTEND SEQ-FILE.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL0701");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL1701: READ NEXT on random-access file
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL1701_ReadNextOnRandomAccessFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO ""IDX.DAT""
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IDX-FILE.
       01 IDX-REC.
          05 IDX-KEY PIC X(10).
          05 IDX-DATA PIC X(70).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE NEXT RECORD
               AT END DISPLAY ""END""
           END-READ.
           CLOSE IDX-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL1701");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL1702: READ KEY on non-indexed file
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL1702_ReadKeyOnSequentialFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       WORKING-STORAGE SECTION.
       01 WS-KEY PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE KEY IS WS-KEY
               AT END DISPLAY ""END""
           END-READ.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL1702");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL1703: READ KEY not a record key of file
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL1703_ReadKeyNotRecordKey()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO ""IDX.DAT""
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IDX-FILE.
       01 IDX-REC.
          05 IDX-KEY PIC X(10).
          05 IDX-DATA PIC X(70).
       WORKING-STORAGE SECTION.
       01 WS-WRONG PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE KEY IS WS-WRONG
               AT END DISPLAY ""END""
           END-READ.
           CLOSE IDX-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL1703");
    }

    [Fact]
    public void ValidReadKeyMatchesRecordKey_NoDiagnostic()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO ""IDX.DAT""
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IDX-FILE.
       01 IDX-REC.
          05 IDX-KEY PIC X(10).
          05 IDX-DATA PIC X(70).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE KEY IS IDX-KEY
               AT END DISPLAY ""END""
           END-READ.
           CLOSE IDX-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL1702");
        AssertNoDiagnostic(diags, "CBL1703");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL2101: RETURN on non-sort/merge file
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL2101_ReturnOnNonSortFile()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           RETURN SEQ-FILE RECORD
               AT END MOVE 1 TO WS-EOF
           END-RETURN.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL2101");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3310: Dynamic CALL warning
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3310_DynamicCallWarning()
    {
        // Dynamic CALL uses a data-name (runtime-resolved), not a literal
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-PROG PIC X(8) VALUE ""SUBPROG"".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL WS-PROG.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3310");
    }

    [Fact]
    public void CallBindsWithoutCrash()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL ""SUBPROG"".
           STOP RUN.
";
        // Just verify compilation completes without exception
        var diags = GetDiagnostics(source);
        Assert.NotNull(diags);
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL0601: FD without matching SELECT
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL0601_FdWithoutSelect()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       FILE SECTION.
       FD ORPHAN-FILE.
       01 ORPHAN-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL0601");
    }

    [Fact]
    public void ValidFdWithSelect_NoCBL0601()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL0601");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL1801: WRITE FROM source incompatible with record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidWriteFrom_NoDiagnostic()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO ""SEQ.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-FILE.
       01 SEQ-REC PIC X(80).
       WORKING-STORAGE SECTION.
       01 WS-DATA PIC X(80) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           WRITE SEQ-REC FROM WS-DATA.
           CLOSE SEQ-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL1801");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL1901/CBL1902: REWRITE validation
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidRewriteFrom_NoDiagnosticOnIndexed()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO ""IDX.DAT""
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IDX-FILE.
       01 IDX-REC.
          05 IDX-KEY PIC X(10).
          05 IDX-DATA PIC X(70).
       WORKING-STORAGE SECTION.
       01 WS-DATA PIC X(80).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O IDX-FILE.
           REWRITE IDX-REC FROM WS-DATA.
           CLOSE IDX-FILE.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL1901");
        AssertNoDiagnostic(diags, "CBL1902");
    }
}
