// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Item 5 (DEVLOG 305) + Default-flip (DEVLOG 310): CBL3128 undefined-data-name detection. A reference
/// whose base name resolves to no data item / condition-name / index-name / file connector / special
/// register / inherited GLOBAL is a compile error in ALL dialects (ISO §8.4.2.1) — the staged rollout
/// flipped to Default-on after the corpus dry-run came back clean. One centralized ReferenceResolver pass.
/// </summary>
public class UndefinedDataNameTests : DiagnosticTestBase
{
    private const string MoveToUndefined = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO NONEXISTENT-ITEM.
           STOP RUN.
";

    [Fact]
    public void Strict_MoveToUndefinedName_ReportsCBL3128()
    {
        var diags = GetDiagnostics(MoveToUndefined, DialectMode.StrictCobol85);
        AssertHasDiagnostic(diags, "CBL3128");
    }

    [Fact]
    public void Default_MoveToUndefinedName_ReportsCBL3128()
    {
        // CBL3128 is now active in ALL dialects (DEVLOG 310), including permissive Default, after the
        // staged rollout's corpus dry-run came back clean (the IC228A inherited-GLOBAL fix removed the
        // lone false positive).
        AssertHasDiagnostic(GetDiagnostics(MoveToUndefined), "CBL3128");
    }

    [Fact]
    public void Strict_DisplayUndefinedOperand_ReportsCBL3128()
    {
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY UNDEF-VAR.
           STOP RUN.
";
        AssertHasDiagnostic(GetDiagnostics(src, DialectMode.StrictCobol85), "CBL3128");
    }

    [Fact]
    public void Strict_AllNamesDefined_NoCBL3128()
    {
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(4).
       01 WS-GRP.
          05 WS-A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO WS-NUM.
           DISPLAY WS-A.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src, DialectMode.StrictCobol85), "CBL3128");
    }

    [Fact]
    public void Strict_QualifiedReference_BaseDefined_NoCBL3128()
    {
        // The OF/IN qualifier word lives in a nested qualification node — only the base name (FLD-A) is
        // checked, and it is defined, so no CBL3128.
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GRP-A.
          05 FLD-A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACE TO FLD-A OF GRP-A.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src, DialectMode.StrictCobol85), "CBL3128");
    }

    // ── Default-flip blockers found by adversarial review (DEVLOG 310): valid COBOL that must NOT flag ──

    [Fact]
    public void Default_Option2SwitchCondition_NoCBL3128()
    {
        // ISO §12.3 Option-2 switch (no mnemonic): the ON/OFF status condition-names must be captured and
        // whitelisted so referencing them is not flagged.
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SWTEST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 ON STATUS IS SW-ON OFF STATUS IS SW-OFF.
       PROCEDURE DIVISION.
       MAIN.
           IF SW-ON
              DISPLAY ""ON""
           END-IF.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src), "CBL3128");
    }

    [Fact]
    public void Default_ScreenName_NoCBL3128()
    {
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SCRTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(5).
       SCREEN SECTION.
       01 MAIN-SCREEN.
          05 LINE 1 COLUMN 1 PIC X(5) FROM WS-X.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY MAIN-SCREEN.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src), "CBL3128");
    }

    [Fact]
    public void Default_InheritedGlobalCondNameAndIndex_NoCBL3128()
    {
        // ISO §8.4.5: a contained program may reference a containing program's IS GLOBAL condition-names
        // (GLO-OK), index-names (GLO-IDX), and data members (GLO-ENT) — none may be flagged.
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GLO-GRP IS GLOBAL.
          05 GLO-FLD PIC 9.
             88 GLO-OK VALUE 1.
       01 GLO-TBL IS GLOBAL.
          05 GLO-ENT PIC X OCCURS 3 INDEXED BY GLO-IDX.
       PROCEDURE DIVISION.
       OUTER-MAIN.
           CALL ""INNERP"".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERP.
       PROCEDURE DIVISION.
       INNER-MAIN.
           IF GLO-OK
              SET GLO-IDX TO 1
           END-IF.
           MOVE ""X"" TO GLO-ENT (GLO-IDX).
           EXIT PROGRAM.
       END PROGRAM INNERP.
       END PROGRAM OUTERP.
";
        AssertNoDiagnostic(GetDiagnostics(src), "CBL3128");
    }

    [Fact]
    public void Default_ChannelMnemonic_NoCBL3128()
    {
        // §12.4.4 CHANNEL integer IS mnemonic: the printer-channel mnemonic (C01) referenced in
        // WRITE ... ADVANCING must be whitelisted, not flagged undefined.
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHANTEST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CHANNEL 1 IS C01.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO ""PRT"".
       DATA DIVISION.
       FILE SECTION.
       FD PRT.
       01 PRT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           WRITE PRT-REC FROM WS-X AFTER ADVANCING C01.
           CLOSE PRT.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src), "CBL3128");
    }

    [Fact]
    public void Default_SpecialRegisters_NoCBL3128()
    {
        // Recognized COBOL special registers that lex as identifiers must not be flagged "undefined"
        // (RETURN-CODE/SORT-* are vendor-universal; DEBUG-ITEM is the COBOL-85 debugging register).
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REGTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO RETURN-CODE.
           MOVE SORT-RETURN TO WS-N.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src), "CBL3128");
    }

    [Fact]
    public void Default_InheritedGlobalFileRecord_NoCBL3128()
    {
        // ISO §8.4.6.2: a contained program may reference a containing program's FD ... IS GLOBAL file
        // record (GREC), its fields (GFLD), and their condition-names (GFLD-OK) — none may be flagged.
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GFILE ASSIGN TO ""GF"".
       DATA DIVISION.
       FILE SECTION.
       FD GFILE IS GLOBAL.
       01 GREC.
          05 GFLD PIC 9.
             88 GFLD-OK VALUE 1.
       PROCEDURE DIVISION.
       OUTER-MAIN.
           CALL ""INNERF"".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERF.
       PROCEDURE DIVISION.
       INNER-MAIN.
           IF GFLD-OK
              MOVE 5 TO GFLD
           END-IF.
           MOVE SPACE TO GREC.
           EXIT PROGRAM.
       END PROGRAM INNERF.
       END PROGRAM OUTERF.
";
        AssertNoDiagnostic(GetDiagnostics(src), "CBL3128");
    }

    [Fact]
    public void Strict_DuplicatedSubordinateName_ResolvesToFirst_NoCBL3128()
    {
        // A subordinate name shared across groups (qualified at use) is legal COBOL; the base resolves
        // to the first declaration, so the undefined-name check must NOT fire.
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GRP-A.
          05 FLD PIC X(3).
       01 GRP-B.
          05 FLD PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACE TO FLD OF GRP-A.
           STOP RUN.
";
        AssertNoDiagnostic(GetDiagnostics(src, DialectMode.StrictCobol85), "CBL3128");
    }
}
