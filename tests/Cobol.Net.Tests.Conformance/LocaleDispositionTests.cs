// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The ISO Annex A §A.4.9 locale-module disposition, increment by increment (owner decision Q1, 2026-08-18;
/// DESIGN-locale-facility §12). LIVE: the FOUR locale FUNCTIONS (LOCALE-COMPARE §15.51 / LOCALE-DATE §15.52 /
/// LOCALE-TIME §15.53 / LOCALE-TIME-FROM-SECONDS §15.54 — kb/Work PB64 T4; the facts below assert they COMPILE and
/// bind their locale-name) and STANDARD-COMPARE (PB101 T7). Still DOCUMENTED NON-SUPPORT (conformant per ISO §4.2.7 +
/// Annex A §A.4.1: an implementation accepts an optional element's syntax only when support is claimed), rejected
/// with <c>COBOLNET1518</c> at bind time BY NAME: the LOCALE keyword PHRASE of the otherwise-supported LOWER-CASE
/// §15.57 / UPPER-CASE §15.97 / NUMVAL-C §15.68 / TEST-NUMVAL-C §15.94 (T5/T6). The same functions WITHOUT a LOCALE
/// phrase stay fully supported (the zero-regression proof).
/// <para>⚠ STANDARD-COMPARE §15.85 was the fifth, and is not any more — see the fact below: it is A.4.9 item 11
/// but travels on §A.3 item 25 (an ISO/IEC 14651:2020 dependency), and that support is now claimed (kb/Work
/// PB101 T7). Each remaining arm is deleted as its increment lands (owner decision Q1, 2026-08-18).</para>
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

    // ── The four locale functions (§15.51/52/53/54) are LIVE since kb/Work PB64 T4: each COMPILES CLEAN at 2023, with
    //    and without its locale-name-1 (a SPECIAL-NAMES LOCALE clause's name). ⚠ The former probes wrote INTEGER
    //    literals for LOCALE-DATE/-TIME (`LOCALE-TIME(120000)`), which §15.52.3 r1 / §15.53.3 r1 forbid (class
    //    alphanumeric or national, 8 / 6 positions) — they were refused by name before any argument was screened,
    //    so the mistake never showed; they are STRING arguments now, and the integer form is a class error.
    [Theory]
    [InlineData("FUNCTION LOCALE-COMPARE(\"A\" \"B\")", "01 WS-R PIC X.")]
    [InlineData("FUNCTION LOCALE-COMPARE(\"A\" \"B\" FR)", "01 WS-R PIC X.")]
    [InlineData("FUNCTION LOCALE-DATE(\"20240229\")", "01 WS-R PIC X(10).")]
    [InlineData("FUNCTION LOCALE-DATE(\"20240229\" FR)", "01 WS-R PIC X(10).")]
    [InlineData("FUNCTION LOCALE-TIME(\"120000\")", "01 WS-R PIC X(10).")]
    [InlineData("FUNCTION LOCALE-TIME(\"120000\" FR)", "01 WS-R PIC X(10).")]
    [InlineData("FUNCTION LOCALE-TIME-FROM-SECONDS(3600)", "01 WS-R PIC X(10).")]
    [InlineData("FUNCTION LOCALE-TIME-FROM-SECONDS(3600.5 FR)", "01 WS-R PIC X(10).")]
    public void LocaleFunctions_AreLive(string funcCall, string receiver)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LOCLIVE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                LOCALE FR IS "fr-FR".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {receiver}
            PROCEDURE DIVISION.
            MAIN.
                MOVE {funcCall} TO WS-R.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>The integer-literal shape the pre-T4 probes used is a §15.52.3 r1 / §15.53.3 r1 CLASS violation now that
    /// the functions bind — the §15.3 screen names it (COBOLNET1627), never a by-name refusal.</summary>
    [Theory]
    [InlineData("FUNCTION LOCALE-DATE(20240229)")]
    [InlineData("FUNCTION LOCALE-TIME(120000)")]
    public void LocaleDateTime_IntegerArgument_IsAClassViolation(string funcCall)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Move(funcCall, "01 WS-R PIC X(10).", "WS-R"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1627");
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));
    }

    /// <summary>
    /// ⛔ STANDARD-COMPARE IS THE ONE A.4.9-LISTED FUNCTION THAT IS **SUPPORTED** (kb/Work PB101 T7, owner
    /// decision Q4). It is A.4.9 item 11 but ordering-table-dependent, not locale-dependent, and its route is
    /// §A.3 item 25 — "The implementor need not accept the syntax … when support for ISO/IEC 14651:2020 is not
    /// provided" — which COBOL.NET now DOES provide, over the derived CLDR/UCA collation engine. This test used
    /// to assert the COBOLNET1518 rejection and its §A.3 citation; it asserts the claim instead, in both the
    /// default-table form and the ORDER TABLE form, because a suite that only ever pinned the refusal would go
    /// green on a compiler that silently stopped binding the function at all.
    /// </summary>
    [Fact]
    public void StandardCompare_IsSupported_NotA49NonSupport()
    {
        foreach (string program in new[]
        {
            Move("FUNCTION STANDARD-COMPARE(\"A\" \"B\")"),                       // §15.85.3 r5's default table
            Move("FUNCTION STANDARD-COMPARE(\"A\" \"B\" 2)"),                     // argument-4, no ordering-name
            """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LOCDISPOT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                ORDER TABLE OT1 IS "ISO 14651_2020_TABLE1".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC X.
            PROCEDURE DIVISION.
            MAIN.
                MOVE FUNCTION STANDARD-COMPARE("A" "B" OT1 3) TO WS-R.
                STOP RUN.
            """,
        })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(program, 2023);
            Assert.True(ok, $"FUNCTION STANDARD-COMPARE must compile (A.3 item 25 support IS claimed): "
                + string.Join("; ", errors));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));
        }
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
