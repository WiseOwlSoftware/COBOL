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

    // ISO §8.4.2.3 — a reference-modification operand is category alphanumeric. FUNCTION UPPER-CASE(X(1:4)) used
    // to return 0 (the substring was sent through the numeric arg path and decoded as a decimal); it now reads
    // the substring as a string.
    [Fact]
    public void RefModdedAlphanumericFunctionArg_ReadsAsString()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. RMARG.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-IN  PIC X(8) VALUE \"abcdefgh\".\n" +
            "       01 WS-OUT PIC X(4).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE FUNCTION UPPER-CASE(WS-IN(1:4)) TO WS-OUT.\n" +
            "           DISPLAY WS-OUT.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("ABCD", stdout);
    }

    // ISO §8.5.1.2 — COMP-1/COMP-2 are floating-point; arithmetic into them must not truncate the fraction to a
    // fixed-point scale. StoreArithmeticResult was scaling/rounding to the receiver's FractionDigits (0 for a
    // PIC-less float), so COMPUTE WS-F = 1.0/3.0 → 0 and 3.14159*2 → 6.
    [Fact]
    public void Compute_IntoFloatReceiver_PreservesFraction()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. COMPF.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-D  USAGE COMP-2.\n" +
            "       01 WS-O1 PIC 9V9(8).\n" +
            "       01 WS-O2 PIC 99V9(5).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           COMPUTE WS-D = 1.0 / 3.0.\n" +
            "           MOVE WS-D TO WS-O1.\n" +
            "           DISPLAY \"DIV=\" WS-O1.\n" +
            "           COMPUTE WS-D = 3.14159 * 2.\n" +
            "           MOVE WS-D TO WS-O2.\n" +
            "           DISPLAY \"MUL=\" WS-O2.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("DIV=033333333\r\nMUL=0628318", stdout);
    }

    // ISO §13.18.35 — raw DISPLAY of a COMP-1/COMP-2 (binary floating-point) item shows its natural decimal
    // magnitude (shortest round-trip; integral → no point), not the synthetic 18-digit fixed-point integer.
    [Fact]
    public void Display_OfFloatItem_ShowsNaturalMagnitude()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. COMPD.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-D USAGE COMP-2.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           COMPUTE WS-D = 3.14159 * 2.\n" +
            "           DISPLAY \"A=\" WS-D.\n" +
            "           COMPUTE WS-D = 100 * 3.\n" +
            "           DISPLAY \"B=\" WS-D.\n" +
            "           COMPUTE WS-D = -2.5 / 4.\n" +
            "           DISPLAY \"C=\" WS-D.\n" +
            "           MOVE ZERO TO WS-D.\n" +
            "           DISPLAY \"D=\" WS-D.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("A=6.28318\r\nB=300\r\nC=-0.625\r\nD=0", stdout);
    }
}
