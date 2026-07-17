// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// OPTIONS ARITHMETIC IS STANDARD-DECIMAL (ISO §8.8.1.5): operations evaluate in SDIDI form — decimal128
/// semantics, 34 significant digits, per-operation INTERMEDIATE ROUNDING (§11.9.11, default
/// NEAREST-AWAY-FROM-ZERO) — with the statement's ROUNDED applied only at the final transfer (§14.7 NOTE 1).
/// SPEC-PINNED (expectations verified against an IEEE decimal128 reference computation; the legacy has no
/// standard-decimal mode). Also pins the per-edition gates (P10 Step 12): the OPTIONS paragraph is 2002+
/// (rejected at 85 naming 2002, accepted at 2002 with its 2002 ARITHMETIC clause); STANDARD-BINARY
/// documented-unsupported; plain STANDARD dropped at 2023.
/// </summary>
public sealed class StandardDecimalTests
{
    private static string Program(string options, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SDTEST.
        OPTIONS.
        {options}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    private static string Run(string options, string ws, string proc)
    {
        var (ok, output, detail) = new CobolNetCompiler(dialectLevel: 2023).CompileAndRun(Program(options, ws, proc));
        Assert.True(ok, detail);
        return output;
    }

    [Fact]
    public void ExactQuotient_Eighth()
        => Assert.Equal("012500", Run("    ARITHMETIC IS STANDARD-DECIMAL.",
            "01 W PIC 9V9(5).", "COMPUTE W = 1 / 8.\n    DISPLAY W."));

    /// <summary>The decimal128 signature case: 2/7 rounds to 34 digits (…2857), ×7 = 1.999…9|9 rounds per-op
    /// NEAREST-AWAY-FROM-ZERO to exactly 2 — where the native Int128 engine's truncated quotient yields 1.99999.</summary>
    [Fact]
    public void PerOpRounding_TwoSeventhsTimesSeven_IsExactlyTwo()
        => Assert.Equal("200000", Run("    ARITHMETIC IS STANDARD-DECIMAL.",
            "01 W PIC 9V9(5).", "COMPUTE W = 2 / 7 * 7.\n    DISPLAY W."));

    /// <summary>1/3 × 3 = 0.9999…(34 nines) in decimal128 — NOT 1 (§8.8.1.5; verified vs the reference).</summary>
    [Fact]
    public void OneThirdTimesThree_IsNotOne()
        => Assert.Equal("NE", Run("    ARITHMETIC IS STANDARD-DECIMAL.",
            "01 W PIC 9.", "IF 1 / 3 * 3 = 1 DISPLAY \"EQ\" ELSE DISPLAY \"NE\"."));

    /// <summary>INTERMEDIATE ROUNDING IS PROHIBITED + an inexact intermediate ⇒ EC-SIZE-TRUNCATION (§11.9.11),
    /// surfaced through the ON SIZE ERROR phrase until the EC model lands.</summary>
    [Fact]
    public void IntermediateProhibited_InexactQuotient_RaisesSizeError()
        => Assert.Equal("IRP\n0", Run(
            "    ARITHMETIC IS STANDARD-DECIMAL\n    INTERMEDIATE ROUNDING IS PROHIBITED.",
            "01 W PIC 9.", "COMPUTE W = 1 / 3 ON SIZE ERROR DISPLAY \"IRP\".\n    DISPLAY W."));

    [Fact]
    public void IntermediateTruncation_OneThird()
        => Assert.Equal("033333", Run(
            "    ARITHMETIC IS STANDARD-DECIMAL\n    INTERMEDIATE ROUNDING IS TRUNCATION.",
            "01 W PIC 9V9(5).", "COMPUTE W = 1 / 3.\n    DISPLAY W."));

    // ── The mode/edition gates ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The OPTIONS paragraph is a 2002 introduction (P10 Step 12 — the Annex E.2 item 21
    /// back-derivation: plain Standard Arithmetic was obsolete-but-present in 2014, so it and its container
    /// predate 2014): --std 85 rejects the paragraph naming 2002; --std 2002 ACCEPTS it (ARITHMETIC IS
    /// NATIVE|STANDARD was the 2002 clause).</summary>
    [Fact]
    public void OptionsParagraph_RejectedAt85_AcceptedAt2002_NamingTheEdition()
    {
        var (ok85, diags85) = EditionHarness.Compile(
            Program("    ARITHMETIC IS NATIVE.", "01 W PIC 9.", "DISPLAY W."), 85);
        Assert.False(ok85, "OPTIONS must be REJECTED at --std 85");
        EditionHarness.AssertHasDiagnostic(diags85, "COBOLNET0804");
        EditionHarness.AssertHasDiagnostic(diags85, "2002");

        var (ok2002, diags2002) = EditionHarness.Compile(
            Program("    ARITHMETIC IS NATIVE.", "01 W PIC 9.", "DISPLAY W."), 2002);
        Assert.True(ok2002, "OPTIONS + ARITHMETIC IS NATIVE must COMPILE at --std 2002: "
            + string.Join("\n", diags2002));
    }

    [Fact]
    public void RoundedModeIs_RejectedBelow2014()
    {
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RM85.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W PIC 9.
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE W ROUNDED MODE IS NEAREST-EVEN = 5 / 2.
                STOP RUN.
            """;
        var (ok, diags) = EditionHarness.Compile(src, 85);
        Assert.False(ok, "ROUNDED MODE IS must be REJECTED at --std 85");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0803");
    }

    [Fact]
    public void StandardBinary_DocumentedUnsupported()
    {
        var (ok, diags) = EditionHarness.Compile(
            Program("    ARITHMETIC IS STANDARD-BINARY.", "01 W PIC 9.", "DISPLAY W."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0806");
        EditionHarness.AssertHasDiagnostic(diags, "obsolete");
    }

    [Fact]
    public void CompositeOverThirtyOne_RejectedEverywhere()
    {
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(20)V9(11) VALUE 1.
            01 B PIC 9(11)V9(20) VALUE 2.
            01 C PIC 9(18).
            PROCEDURE DIVISION.
            MAIN.
                ADD A B GIVING C.
                STOP RUN.
            """;
        var (ok, diags) = EditionHarness.Compile(src, 2023);
        Assert.False(ok, "a 40-digit composite must be REJECTED (ISO §14.7 rule 2 caps at 31)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0805");
    }
}
