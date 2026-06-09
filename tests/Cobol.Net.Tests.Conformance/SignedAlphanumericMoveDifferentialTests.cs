// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// MOVE / compare of a SIGNED numeric to an ALPHANUMERIC operand (ISO/IEC 1989:2023 §14.9.25.4 GR6a): "if the sending
/// operand is described as being signed numeric, the operational sign is not moved" — so the de-signed magnitude
/// digits are transferred (an overpunched digit becomes a plain digit; a separate sign character is omitted), NOT the
/// zoned/overpunch image. A numeric-vs-alphanumeric comparison follows the same rule (§8.8.4.2.5 treats the numeric as
/// moved to an alphanumeric item). DISPLAY is unaffected — it shows the sign-aware image. Each result is derived from
/// the spec, then cross-checked against the legacy oracle.
/// </summary>
public sealed class SignedAlphanumericMoveDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSpecAndLegacy(string source, string expected)
    {
        string want = CutRunner.Normalize(expected);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(want, cout);                       // primary: conformance to ISO §14.9.25.4 GR6a
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        Assert.Equal(want, lout);                       // cross-check: the oracle agrees with the spec value
    }

    /// <summary>Assert COBOL.NET matches the SPEC-derived value, with NO legacy cross-check — used where the legacy
    /// differential oracle is non-conformant to ISO 2023 for the case (the spec is the authority; cf. the DISPLAY
    /// trailing-trim precedent).</summary>
    private static void AssertSpecOnly(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SGNMOVE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Fact]
    // A positive overpunch digit is de-signed on the move to alphanumeric (+1 → "1", not the zoned "A").
    public void SignedToAlphanumeric_PositiveOverpunchDeSigned()
        => AssertSpecAndLegacy(Program("01 SN PIC S9 VALUE 1.\n01 XR PIC X.",
            "    MOVE SN TO XR.\n    DISPLAY \"R=\" XR."), "R=1");

    [Fact]
    // A negative value moves its magnitude digits, sign dropped (-123 → "123").
    public void SignedToAlphanumeric_NegativeMagnitude()
        => AssertSpecAndLegacy(Program("01 SN PIC S9(3) VALUE -123.\n01 XR PIC X(3).",
            "    MOVE SN TO XR.\n    DISPLAY \"R=\" XR."), "R=123");

    [Fact]
    // The magnitude is zero-padded to the digit count (+7 in S9(3) → "007").
    public void SignedToAlphanumeric_ZeroPaddedMagnitude()
        => AssertSpecAndLegacy(Program("01 SN PIC S9(3) VALUE 7.\n01 XR PIC X(3).",
            "    MOVE SN TO XR.\n    DISPLAY \"R=\" XR."), "R=007");

    [Fact]
    // A signed numeric compared against an alphanumeric literal uses its de-signed magnitude (ISO §8.8.4.2.5 → the
    // numeric is "treated as though it were moved, according to the rules of the MOVE statement" → §14.9.25.4 GR6a, the
    // sign is not moved): -5 in two digits is "05", which equals the literal "05". SPEC-ONLY: the legacy oracle is
    // non-conformant here — it compares the overpunch image ("0N") and reports NE (cf. the DISPLAY trailing-trim
    // precedent in feedback_use_the_spec). INVESTIGATED as a VERSION-INVARIANT legacy bug, not a cross-edition change
    // (DEVLOG 517): COBOL-85 already de-signs the MOVE building-block (NC114M MOVE-TEST-16 "STRIP MINUS SIGN" golden),
    // and CCVS85 authors REDEFINE a signed item as X(n) precisely to image-compare its sign (NC116A GRP-09/10) —
    // implying a DIRECT numeric-vs-alphanumeric compare de-signs. No '85 golden requires the legacy image-compare, so
    // pin-to-spec for ALL dialects (see feedback_version_targeted_semantics / docs/VERSION_CHANGE_REFERENCE.md).
    public void SignedVsAlphanumericComparison_UsesDeSignedMagnitude()
        => AssertSpecOnly(Program("01 SN PIC S9(2) VALUE -5.",
            "    IF SN = \"05\" DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF."), "EQ");

    [Fact]
    // ISO §8.8.4.1: an alphanumeric GROUP receiver is treated as an elementary alphanumeric item, so a signed numeric
    // source also drops its sign moving into a group (§14.9.25.4 GR6a) — the EmitGroupMove de-sign gap (DEVLOG 516).
    // S9(3) -45 → "045". SPEC-ONLY: the legacy oracle is non-conformant for a group receiver — it copies the raw
    // overpunch image ("04N") rather than de-signing (cf. the DISPLAY trailing-trim precedent in feedback_use_the_spec).
    // INVESTIGATED version-invariant (DEVLOG 517): same MOVE de-sign rule the COBOL-85 corpus already exercises
    // (NC114M MOVE-TEST-16/17), with §8.8.4.1 (group = elementary alphanumeric) unchanged across editions → pin-to-spec
    // for ALL dialects (feedback_version_targeted_semantics / docs/VERSION_CHANGE_REFERENCE.md).
    public void SignedToAlphanumericGroup_DeSigned()
        => AssertSpecOnly(Program("01 SN PIC S9(3) VALUE -45.\n01 GRP.\n   05 G1 PIC X(3).",
            "    MOVE SN TO GRP.\n    DISPLAY \"R=\" GRP."), "R=045");

    [Fact]
    // A signed numeric that is the CANONICAL of a REDEFINES is stored as its sign-aware character image and read
    // through a RedefViewPlace window; it still de-signs on a move to alphanumeric — the RedefViewPlace source de-sign
    // gap (DEVLOG 516). SIGN LEADING SEPARATE +91275 has image "+91275"; the magnitude "91275" is moved (exact-fit
    // X(5) receiver, so no trailing space exposes the legacy DISPLAY-trim quirk).
    public void SignedRedefinesCanonicalToAlphanumeric_DeSigned()
        => AssertSpecAndLegacy(Program(
            "01 SN PIC S9(5) SIGN LEADING SEPARATE VALUE 91275.\n01 GRP REDEFINES SN PIC X(6).\n01 XR PIC X(5).",
            "    MOVE SN TO XR.\n    DISPLAY \"R=\" XR."), "R=91275");

    [Theory]
    // Boundary guard: the de-signing is scoped to the ALPHANUMERIC (string) comparison branch ONLY. A signed numeric
    // compared against a NUMERIC literal is an algebraic comparison (ISO §8.8.4.2.1) — the sign is significant and is
    // NOT dropped — so -5 ≠ 5 but -5 = -5. (Spec and the legacy oracle agree here.)
    [InlineData("    IF SN = 5 DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF.", "NE")]
    [InlineData("    IF SN = -5 DISPLAY \"EQ\" ELSE DISPLAY \"NE\" END-IF.", "EQ")]
    public void SignedVsNumericComparison_StaysAlgebraic(string proc, string expected)
        => AssertSpecAndLegacy(Program("01 SN PIC S9(2) VALUE -5.", proc), expected);
}
