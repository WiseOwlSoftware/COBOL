// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Item 5 (DEVLOG 305): CBL3128 undefined-data-name detection. A reference whose base name resolves to
/// no data item / condition-name / index-name / file connector / special register is a compile error
/// under named-strict dialect modes (ISO §8.4.2.1). Default / --nist stay permissive (staged rollout),
/// so the NIST corpus is unaffected by construction. The check is one centralized ReferenceResolver pass.
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
    public void Default_MoveToUndefinedName_NoCBL3128()
    {
        // Staged rollout: permissive Default (and --nist) must NOT fire — the gate that keeps the 349
        // NIST baselines green by construction.
        AssertNoDiagnostic(GetDiagnostics(MoveToUndefined), "CBL3128");
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
