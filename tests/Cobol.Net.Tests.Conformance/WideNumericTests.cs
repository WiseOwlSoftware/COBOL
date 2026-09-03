// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The WIDE numeric tier (numeric design D1 / SSOT §18 #4): PIC 9(19..31) items store as <see cref="Int128"/>
/// and compute exactly through the Int128 carrier — a COBOL-2002+ feature (ISO §8.3.3.3.2 caps literals/items at
/// 31 digits; COBOL-85 capped at 18). SPEC-PINNED (the legacy reference is a COBOL-85 implementation with a
/// 28-digit decimal engine — it cannot oracle this tier). Includes the edition gates BOTH ways: 19+ digits
/// REJECTED at --std 85 with a diagnostic NAMING the required edition; >31 rejected at every edition.
/// </summary>
public sealed class WideNumericTests
{
    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. WIDETEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    private static string Run(string ws, string proc)
    {
        var (ok, output, detail) = new CobolNetCompiler(dialectLevel: 2023).CompileAndRun(Program(ws, proc));
        Assert.True(ok, detail);
        return output;
    }

    [Fact]
    public void Pic19_StoresAndDisplaysExactly()
        => Assert.Equal("1234567890123456789", Run("01 W PIC 9(19) VALUE 1234567890123456789.", "DISPLAY W."));

    [Fact]
    public void Pic31_WideLiteralMove_Exact()
        => Assert.Equal("1234567890123456789012345678901",
            Run("01 W PIC 9(31).", "MOVE 1234567890123456789012345678901 TO W.\n    DISPLAY W."));

    /// <summary>A 34-digit exact intermediate product (16×15-digit operands × 1000) stores into a 31-digit
    /// receiver with HIGH-ORDER truncation (the no-ON-SIZE-ERROR behavior, ISO §14.7) — proving the carrier held
    /// every digit exactly up to the store: the low 31 digits match the true product
    /// 9999999999999989000000000000001000.</summary>
    [Fact]
    public void WideProduct_34DigitIntermediate_TruncatesHighOrderAtStore()
        => Assert.Equal("9999999999989000000000000001000",
            Run("01 A PIC 9(16) VALUE 9999999999999999.\n01 B PIC 9(15) VALUE 999999999999999.\n01 W PIC 9(31).",
                "COMPUTE W = A * B * 1000.\n    DISPLAY W."));

    [Fact]
    public void WideProduct_FitsReceiver_Exact()
        => Assert.Equal("0999999999999999999999999999999",
            Run("01 A PIC 9(15) VALUE 999999999999999.\n01 B PIC 9(16) VALUE 1000000000000001.\n01 W PIC 9(31).",
                "COMPUTE W = A * B.\n    DISPLAY W."));

    [Fact]
    public void WideAddSubtract_Exact()
        => Assert.Equal("2000000000000000000000000000000",
            Run("01 A PIC 9(31) VALUE 1999999999999999999999999999999.\n01 W PIC 9(31).",
                "COMPUTE W = A + 1.\n    DISPLAY W."));

    [Fact]
    public void SignedWide_NegativeArithmetic()
        => Assert.Equal("-0000000000000000000001",
            Run("01 A PIC S9(22) VALUE 1.\n01 W PIC S9(22) SIGN LEADING SEPARATE.",
                "COMPUTE W = A - 2.\n    DISPLAY W."));

    [Fact]
    public void EditionGate_Pic19_RejectedAt85_NamingRequiredEdition()
    {
        var (ok, diags) = EditionHarness.Compile(Program("01 W PIC 9(19) VALUE 1.", "DISPLAY W."), 85);
        Assert.False(ok, "PIC 9(19) must be REJECTED at --std 85 (the 18-digit cap)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0802");
        EditionHarness.AssertHasDiagnostic(diags, "2002");
    }

    [Fact]
    public void EditionGate_WideLiteral_RejectedAt85()
    {
        var (ok, diags) = EditionHarness.Compile(Program("01 W PIC 9(18).", "MOVE 1234567890123456789 TO W.\n    DISPLAY W."), 85);
        Assert.False(ok, "a 19-digit literal must be REJECTED at --std 85");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0802");
    }

    [Fact]
    public void EditionGate_Pic32_RejectedAtEveryEdition()
    {
        foreach (int edition in EditionHarness.Editions)
        {
            var (ok, diags) = EditionHarness.Compile(Program("01 W PIC 9(32) VALUE 1.", "DISPLAY W."), edition);
            Assert.False(ok, $"PIC 9(32) must be REJECTED at --std {edition} (the ISO 31-digit cap)");
            EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0801");
        }
    }
}
