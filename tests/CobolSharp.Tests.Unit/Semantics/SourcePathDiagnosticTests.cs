// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Item 7 (DEVLOG 304): every post-parse diagnostic now carries the real source file path instead of
/// the "&lt;source&gt;" placeholder, and the bare ad-hoc "SEM" code was retired in favor of registry
/// descriptors — CBL3120 (PERFORM/GO TO target), CBL3121 (file operand), CBL3122 (phantom paragraph).
/// </summary>
public class SourcePathDiagnosticTests : DiagnosticTestBase
{
    private const string PerformUndefinedSource = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM NOSUCH-PARA.
           STOP RUN.
";

    [Fact]
    public void PerformUndefinedTarget_ReportsCBL3120_NotBareSem()
    {
        var diags = GetDiagnostics(PerformUndefinedSource);
        AssertHasDiagnostic(diags, "CBL3120");
        // The bare "SEM" code (no registry descriptor) must no longer be produced anywhere.
        AssertNoDiagnostic(diags, "SEM");
    }

    [Fact]
    public void ReferenceDiagnostic_CarriesRealSourcePath_NotPlaceholder()
    {
        var diags = GetDiagnostics(PerformUndefinedSource);
        var perform = Assert.Single(diags, d => d.Code == "CBL3120");
        // The harness compiles a temp file named TEST.cbl; the diagnostic must report that path,
        // not the legacy "<source>" placeholder.
        Assert.EndsWith("TEST.cbl", perform.Location.FileName);
        Assert.NotEqual("<source>", perform.Location.FileName);
    }

    [Fact]
    public void ValidProgram_NoReferenceErrors_NoBareSem()
    {
        var source = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM OTHER-PARA.
           STOP RUN.
       OTHER-PARA.
           CONTINUE.
";
        var diags = GetDiagnostics(source);
        AssertNoDiagnostic(diags, "CBL3120");
        AssertNoDiagnostic(diags, "CBL3121");
        AssertNoDiagnostic(diags, "SEM");
    }
}
