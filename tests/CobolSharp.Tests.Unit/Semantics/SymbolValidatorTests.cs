// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Tests for SymbolValidator: duplicate detection via scope rejections,
/// REDEFINES validation, and linkage section rules.
/// All tests use full compilation through DiagnosticTestBase.
/// </summary>
public class SymbolValidatorTests : DiagnosticTestBase
{
    // ═══════════════════════════════════════════════════════════════
    // Valid program — no diagnostics from CBL31xx
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ValidProgram_NoDuplicateDiagnostics()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       01 WS-B PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-A.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL3101");
        AssertNoDiagnostic(diags, "CBL3102");
        AssertNoDiagnostic(diags, "CBL3103");
        AssertNoDiagnostic(diags, "CBL3104");
        AssertNoDiagnostic(diags, "CBL3107");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3101: Duplicate data-name
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3101_Duplicate01LevelDataNames()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TOTAL PIC 9.
       01 TOTAL PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3101");
    }

    [Fact]
    public void CBL3101_Duplicate77LevelDataNames()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 COUNTER PIC 9(3).
       77 COUNTER PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3101");
    }

    [Fact]
    public void CBL3101_DuplicateIndexedByNames()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TABLE-A.
          05 ITEM-A PIC X OCCURS 5 TIMES
             INDEXED BY IDX-1.
       01 TABLE-B.
          05 ITEM-B PIC X OCCURS 5 TIMES
             INDEXED BY IDX-1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3101");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3102: Duplicate condition-name (88-level)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3102_Duplicate88UnderSameParent()
    {
        // Two 88-levels with the same name under the same parent
        // Both go into DataDivisionScope; the second is rejected
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FLAG PIC X.
          88 IS-YES VALUE ""Y"".
          88 IS-YES VALUE ""N"".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3102");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3107: Cross-type name conflicts
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ConditionNameEqualsParentDataName_NoFalsePositive()
    {
        // COBOL allows condition-name = parent data-name (context-typed resolution)
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FLAG PIC X.
          88 FLAG VALUE ""Y"".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL3107");
        AssertNoDiagnostic(diags, "CBL3101");
    }

    [Fact]
    public void ConditionNameEqualsSiblingDataName_NoFalsePositive()
    {
        // COBOL allows condition-name = sibling data-name (context-typed resolution)
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 FIELD-A PIC X.
          05 FIELD-B PIC X.
             88 FIELD-A VALUE ""Y"".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL3107");
        AssertNoDiagnostic(diags, "CBL3101");
    }

    [Fact]
    public void SubordinateDataNameDuplicates_NoFalsePositive()
    {
        // COBOL allows subordinate items with same name under different groups
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GRP-A.
          05 FIELD-X PIC X.
       01 GRP-B.
          05 FIELD-X PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL3101");
    }

    [Fact]
    public void CBL3107_SectionNameEqualsParagraphName()
    {
        // Paragraph declared globally in ProcedureDivisionScope collides with section
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3107");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3103: Duplicate section names
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3103_DuplicateSectionNames()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       PROCEDURE DIVISION.
       SEC-A SECTION.
       PARA-1.
           DISPLAY WS-A.
       SEC-A SECTION.
       PARA-2.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3103");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3104: Duplicate paragraph names
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3104_DuplicateParagraphNames()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       PROCEDURE DIVISION.
       PARA-1.
           DISPLAY WS-A.
       PARA-1.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3104");
    }

    // ═══════════════════════════════════════════════════════════════
    // CBL3112: REDEFINES level mismatch
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3112_RedefinesLevelMismatch()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-A.
          05 FIELD-A PIC X(10).
          10 FIELD-B REDEFINES FIELD-A PIC 9(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3112");
    }

    // ═══════════════════════════════════════════════════════════════
    // REDEFINES within OCCURS — legal in COBOL (no diagnostic)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RedefinesWithinOccurs_NoDiagnostic()
    {
        // REDEFINES within an OCCURS group is perfectly valid COBOL
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TABLE-REC.
          05 TABLE-ITEM OCCURS 10 TIMES.
             10 FIELD-A PIC X(5).
             10 FIELD-B REDEFINES FIELD-A PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL3114");
        AssertNoDiagnostic(diags, "CBL3112");
    }

    // CBL3113: REDEFINES of special-level item (66/88).
    // Level-88 items are ConditionSymbol (not DataSymbol) — REDEFINES resolution
    // can't target them. Level-66 RENAMES stored as DataSymbol could be tested
    // if RENAMES is fully supported. Check is a safety net for future binder changes.

    // ═══════════════════════════════════════════════════════════════
    // CBL3110/3111: Linkage section rules (existing, verify still work)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3110_LinkageValueOnNon88()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-A PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3110");
    }

    [Fact]
    public void CBL3111_LinkageRedefinesOn01()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-A PIC X(10).
       01 LS-B REDEFINES LS-A PIC 9(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3111");
    }

    // ═══════════════════════════════════════════════════════════════
    // Duplicate file names
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CBL3107_DuplicateFileNames()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO ""A.DAT"".
           SELECT FILE-A ASSIGN TO ""B.DAT"".
       DATA DIVISION.
       FILE SECTION.
       FD FILE-A.
       01 REC-A PIC X(80).
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        var diags = GetDiagnostics(source);
        AssertHasDiagnostic(diags, "CBL3107");
    }
}
