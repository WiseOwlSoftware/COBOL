// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The ISO Annex A §A.4.9 locale-module DOCUMENTED-NON-SUPPORT disposition (P11 Step 8; ratified decision 3 —
/// conformant per ISO §4.2.7 + Annex A §A.4.1: an implementation accepts an optional element's syntax only when
/// support is claimed). Two surfaces reject with <c>COBOLNET1518</c> at bind time BY NAME: the five locale
/// FUNCTIONS (LOCALE-COMPARE §15.51 / LOCALE-DATE §15.52 / LOCALE-TIME §15.53 / LOCALE-TIME-FROM-SECONDS §15.54 /
/// STANDARD-COMPARE §15.85 — the last also citing §A.3 item 25, being ISO/IEC 14651:2020-ordering-dependent, not
/// locale-dependent), and the LOCALE keyword PHRASE of the otherwise-supported LOWER-CASE §15.57 / UPPER-CASE
/// §15.97 / NUMVAL-C §15.68 / TEST-NUMVAL-C §15.94. The same functions WITHOUT a LOCALE phrase stay fully
/// supported (the zero-regression proof). Diagnostic values hand-derived in PHASE-11-scout-notes.md (spec:locale).
/// </summary>
public sealed class LocaleDispositionTests
{
    private static string Move(string funcCall, string receiver = "01 WS-R PIC X.", string target = "WS-R") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. LOCDISP.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {receiver}
        PROCEDURE DIVISION.
        MAIN.
            MOVE {funcCall} TO {target}.
            STOP RUN.
        """;

    private static void AssertA49(string source, int edition = 2023)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, "an A.4.9 locale element shall be rejected (documented non-support)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1518");
        EditionHarness.AssertHasDiagnostic(errors, "A.4.9");
    }

    // ── The five locale functions (§15.51/52/53/54/85) → COBOLNET1518 ──────────────────────────────────────

    [Fact] public void LocaleCompare_A49() => AssertA49(Move("FUNCTION LOCALE-COMPARE(\"A\" \"B\")"));
    [Fact] public void LocaleDate_A49() => AssertA49(Move("FUNCTION LOCALE-DATE(20240229)", "01 WS-R PIC X(10).", "WS-R"));
    [Fact] public void LocaleTime_A49() => AssertA49(Move("FUNCTION LOCALE-TIME(120000)", "01 WS-R PIC X(10).", "WS-R"));
    [Fact] public void LocaleTimeFromSeconds_A49() => AssertA49(Move("FUNCTION LOCALE-TIME-FROM-SECONDS(3600)", "01 WS-R PIC 9(6).", "WS-R"));

    [Fact]
    public void StandardCompare_A49_AlsoCitesA3()
    {
        // §15.85 STANDARD-COMPARE is A.4.9 item 11 but is ordering-table-dependent (SPECIAL-NAMES ORDER TABLE,
        // §12.3.7), not locale-dependent — its independent non-support route is §A.3 item 25 (no ISO/IEC
        // 14651:2020 implementation). The diagnostic cites BOTH.
        var (ok, errors, _) = EditionHarness.CompileFull(Move("FUNCTION STANDARD-COMPARE(\"A\" \"B\")"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1518");
        EditionHarness.AssertHasDiagnostic(errors, "A.4.9");
        EditionHarness.AssertHasDiagnostic(errors, "A.3 item 25");
    }

    // ── The LOCALE keyword phrase of the otherwise-supported functions (§15.57/97/68/94) → COBOLNET1518 ─────

    [Fact] public void LowerCaseLocale_A49() =>
        AssertA49(Move("FUNCTION LOWER-CASE(WS-X LOCALE LOC1)", "01 WS-X PIC X(3) VALUE \"AbC\".\n01 WS-R PIC X(3).", "WS-R"));
    [Fact] public void UpperCaseLocale_A49() =>
        AssertA49(Move("FUNCTION UPPER-CASE(WS-X LOCALE LOC1)", "01 WS-X PIC X(3) VALUE \"AbC\".\n01 WS-R PIC X(3).", "WS-R"));
    [Fact] public void NumvalCLocale_A49() =>
        AssertA49(Move("FUNCTION NUMVAL-C(\"12,34\" LOCALE LOC1)", "01 WS-R PIC 9(4)V99.", "WS-R"));
    [Fact] public void TestNumvalCLocale_A49() =>
        AssertA49(Move("FUNCTION TEST-NUMVAL-C(\"12\" LOCALE LOC1)", "01 WS-R PIC 9(2).", "WS-R"));

    [Fact]
    public void NumvalCLocale_BareKeyword_A49() =>
        // §15.68.2: LOCALE with the locale-name omitted (the current-locale form) — still A.4.9 non-support.
        AssertA49(Move("FUNCTION NUMVAL-C(\"12.34\" LOCALE)", "01 WS-R PIC 9(4)V99.", "WS-R"));

    // ── Zero-regression: the SAME functions without a LOCALE phrase remain fully supported ──────────────────

    [Fact]
    public void SupportedFunctions_WithoutLocale_StillWork()
    {
        // The non-LOCALE forms are NOT optional (§15.57.4 rule 4 gives an implementor correspondence when no
        // locale is in effect; NUMVAL-C without LOCALE uses the compilation-unit currency) — they must not
        // regress to a 1518.
        foreach (var (call, recv, tgt) in new[]
        {
            ("FUNCTION LOWER-CASE(WS-X)", "01 WS-X PIC X(3) VALUE \"AbC\".\n01 WS-R PIC X(3).", "WS-R"),
            ("FUNCTION UPPER-CASE(WS-X)", "01 WS-X PIC X(3) VALUE \"AbC\".\n01 WS-R PIC X(3).", "WS-R"),
            ("FUNCTION NUMVAL-C(\"$1,234.56\")", "01 WS-R PIC 9(4)V99.", "WS-R"),
        })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Move(call, recv, tgt), 2023);
            Assert.True(ok, $"{call} without a LOCALE phrase must compile: {string.Join("; ", errors)}");
        }
    }
}
