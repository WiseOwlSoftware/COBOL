// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Regression tests for compiler bugs from docs/SPEC_FIX_BACKLOG.md, re-implemented on main after the
/// worktree-isolated fix workflow produced diffs against a stale base (see DEVLOG 334). Each [Fact] is CLI-verified.
/// </summary>
public sealed class SpecFixTests : EndToEndTestBase
{
    // ISO §15.18 — FUNCTION CONCAT is the synonym of CONCATENATE (added to the alphanumeric-function set; it was
    // typed numeric and crashed with InvalidCastException when used).
    [Fact]
    public void Concat_IsAlphanumeric_ReturnsConcatenation()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. CONCATT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-X PIC X(8) VALUE SPACES.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE FUNCTION CONCAT(\"AB\", \"CD\") TO WS-X.\n" +
            "           DISPLAY WS-X.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("ABCD", stdout);
    }

    // ISO §13.18.3 rule 27 — only A,B,C,D,E,N,P,R,S,V,X,Z are forbidden as the currency PICTURE SYMBOL; other
    // letters (e.g. 'U', as in the spec's own EUR/USD examples) are valid. CBL3124 used to reject every letter.
    [Fact]
    public void CurrencySign_NonReservedLetterSymbol_IsAccepted()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. CURU.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       CONFIGURATION SECTION.\n" +
            "       SPECIAL-NAMES.\n" +
            "           CURRENCY SIGN IS \"$\" WITH PICTURE SYMBOL \"U\".\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-C PIC U99 VALUE 42.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           DISPLAY WS-C.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("$42", stdout);
    }

    // ISO §13.18.3 — BLANK WHEN ZERO with a zero value yields a field of spaces of the PICTURE width; the display
    // path used to TrimEnd that all-blank field down to an empty string.
    [Fact]
    public void BlankWhenZero_ZeroValue_RendersBlankField()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. BWZT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-B PIC ZZ,ZZ9 BLANK WHEN ZERO.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE 0 TO WS-B.\n" +
            "           DISPLAY \"[\" WS-B \"]\".\n" +
            "           MOVE 123 TO WS-B.\n" +
            "           DISPLAY \"[\" WS-B \"]\".\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        // zero → 6-char blank field (ZZ,ZZ9); non-zero → normal edit.
        Assert.Equal("[      ]\r\n[   123]", stdout);
    }

    // ISO §15 — variadic string functions accept SPACE-separated literal arguments (each space before a literal
    // begins a new argument); previously only the first argument was passed (the rest were swallowed).
    [Fact]
    public void Concatenate_SpaceSeparatedLiteralArgs_PassesAll()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. VARCAT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-X PIC X(10) VALUE SPACES.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE FUNCTION CONCATENATE(\"ab\" \"cd\" \"ef\") TO WS-X.\n" +
            "           DISPLAY \"[\" WS-X \"]\".\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("[abcdef]", stdout);
    }

    // ISO §7.2.3 — COPY with a quoted-literal text-name (the literal-1 alternative). The reader used to stop at
    // the opening quote and resolve an empty name → "copybook not found".
    [Fact]
    public void Copy_QuotedLiteralTextName_Resolves()
    {
        File.WriteAllText(Path.Combine(_tempDir, "MYBOOK.cpy"),
            "       01 GREETING PIC X(8) VALUE \"HI THERE\".\n");
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. CPYLIT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       COPY \"MYBOOK\".\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           DISPLAY GREETING.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("HI THERE", stdout);
    }
}
