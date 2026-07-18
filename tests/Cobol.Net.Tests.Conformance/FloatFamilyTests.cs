// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The COBOL-2014 IEEE-754 interchange float USAGE family (ISO §13.18.60.4 GR14-18; PHASE-12 wave 3). GR14/GR15
/// PIN FLOAT-BINARY-32/64 to ISO/IEC 60559:2020 binary32/binary64 — mapped EXACTLY to native float/double (LIVE).
/// GR16-18 PIN FLOAT-BINARY-128 / FLOAT-DECIMAL-16/34 to binary128 / decimal64/128, which .NET provides no type
/// for; backing them by double/System.Decimal would be NON-conforming, so they are documented processor-dependent
/// non-support (Annex A.3 items 17/19) — rejected LOUD with COBOLNET1564, never a silent wrong representation
/// (the P12 re-scout inverted the plan's "conforming implementor choice per GR13" premise). All five are 2014
/// introductions (COBOLNET0900 below 2014). The run behavior is the <c>float_binary</c> conformance corpus.
/// </summary>
public sealed class FloatFamilyTests
{
    private static string Prog(string entry) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FFG.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + entry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>FLOAT-BINARY-32/64 (§13.18.60.4 GR14/GR15) are LIVE — a well-formed item compiles clean at 2014.</summary>
    [Theory]
    [InlineData("01 WS-F USAGE FLOAT-BINARY-32.")]
    [InlineData("01 WS-F USAGE FLOAT-BINARY-64.")]
    public void BinaryLive_CompilesAt2014(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.True(ok, $"a well-formed FLOAT-BINARY-32/64 item must compile at 2014:\n{string.Join("\n", diag)}");
    }

    /// <summary>FLOAT-BINARY-128 / FLOAT-DECIMAL-16 / FLOAT-DECIMAL-34 (§13.18.60.4 GR16-18) are processor-dependent
    /// non-support (Annex A.3 items 17/19): rejected LOUD with COBOLNET1564 (never a silent non-conforming
    /// double/System.Decimal representation).</summary>
    [Theory]
    [InlineData("01 WS-F USAGE FLOAT-BINARY-128.")]
    [InlineData("01 WS-F USAGE FLOAT-DECIMAL-16.")]
    [InlineData("01 WS-F USAGE FLOAT-DECIMAL-34.")]
    public void ProcessorDependent_Rejected1564(string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), 2014);
        Assert.False(ok, "a processor-dependent (unsupported) IEEE float format must be rejected (ISO Annex A.3)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1564");
    }

    /// <summary>The COBOL-2014 introduction gate: FLOAT-BINARY-32/64 are rejected below 2014 with COBOLNET0900
    /// (VersionConformancePass UsageConstructId).</summary>
    [Theory]
    [InlineData(85, "01 WS-F USAGE FLOAT-BINARY-32.")]
    [InlineData(2002, "01 WS-F USAGE FLOAT-BINARY-32.")]
    [InlineData(85, "01 WS-F USAGE FLOAT-BINARY-64.")]
    [InlineData(2002, "01 WS-F USAGE FLOAT-BINARY-64.")]
    public void BinaryBelowIntroduction_Rejected0900(int edition, string entry)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(entry), edition);
        Assert.False(ok, $"FLOAT-BINARY-32/64 must be rejected at COBOL-{edition} (introduced 2014)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0900");
    }

    /// <summary>PICTURE is prohibited with a floating-point usage (§13.18.60.2 — the item is picture-less):
    /// COBOLNET1521, the whole-float-family guard.</summary>
    [Fact]
    public void PictureWithFloatBinary_Rejected1521()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("01 WS-F PIC 9(4) USAGE FLOAT-BINARY-32."), 2014);
        Assert.False(ok, "PICTURE with FLOAT-BINARY-32 must be rejected (ISO §13.18.60.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1521");
    }
}
