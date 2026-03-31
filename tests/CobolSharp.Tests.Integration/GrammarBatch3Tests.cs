using CobolSharp.Compiler;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Tests for Batch 3 COBOL-85 grammar fixes:
/// M407: CURRENCY SIGN WITH PICTURE SYMBOL (PICMODE refactor)
/// M411: SCREEN SECTION (grammar island) — tests added separately
/// </summary>
public class GrammarBatch3Tests : EndToEndTestBase
{
    // ==========================================
    // M407 — CURRENCY SIGN WITH PICTURE SYMBOL
    // ==========================================

    [Fact]
    public void CurrencySign_WithoutPictureSymbol_ParsesAndFormats()
    {
        // Basic CURRENCY SIGN IS literal — no PICTURE SYMBOL
        // literal-7 is both the PIC symbol and the output char
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURR01.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "#".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-AMOUNT PIC ###,##9.99.
            01 WS-RESULT PIC X(12).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234.56 TO WS-AMOUNT.
                MOVE WS-AMOUNT TO WS-RESULT.
                DISPLAY ">" WS-RESULT "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Floating # should produce "  #1,234.56"
        Assert.Contains("#1,234.56", stdout);
    }

    [Fact]
    public void CurrencySign_WithPictureSymbol_ParsesSuccessfully()
    {
        // CURRENCY SIGN IS literal-7 WITH PICTURE SYMBOL literal-8
        // literal-7 = output char ("#"), literal-8 = PIC symbol ("@")
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURR02.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "@".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-AMOUNT PIC @@@,@@9.99.
            01 WS-RESULT PIC X(12).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234.56 TO WS-AMOUNT.
                MOVE WS-AMOUNT TO WS-RESULT.
                DISPLAY ">" WS-RESULT "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // PIC uses @, but output uses # (literal-7)
        Assert.Contains("#1,234.56", stdout);
        Assert.DoesNotContain("@", stdout);
    }

    [Fact]
    public void CurrencySign_WithPictureSymbol_FixedCurrency()
    {
        // Fixed currency: single @ in PIC, output as #
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURR03.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "@".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-PRICE PIC @99.99.
            01 WS-RESULT PIC X(7).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 12.34 TO WS-PRICE.
                MOVE WS-PRICE TO WS-RESULT.
                DISPLAY ">" WS-RESULT "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // Fixed currency: @ becomes # in output
        Assert.Contains("#12.34", stdout);
    }

    [Fact]
    public void CurrencySign_DefaultDollar_Unaffected()
    {
        // Default $ with no CURRENCY SIGN clause — existing behavior preserved
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURR04.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-AMOUNT PIC $$$,$$9.99.
            01 WS-RESULT PIC X(12).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234.56 TO WS-AMOUNT.
                MOVE WS-AMOUNT TO WS-RESULT.
                DISPLAY ">" WS-RESULT "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("$1,234.56", stdout);
    }

    [Fact]
    public void CurrencySign_WithPictureSymbol_BlankWhenZero()
    {
        // BLANK WHEN ZERO with custom currency — field should be all spaces
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURR05.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "@".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-AMOUNT PIC @@@,@@9.99 BLANK WHEN ZERO.
            01 WS-RESULT PIC X(12).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 0 TO WS-AMOUNT.
                MOVE WS-AMOUNT TO WS-RESULT.
                DISPLAY ">" WS-RESULT "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // BLANK WHEN ZERO: field is all spaces, DISPLAY trims to empty
        Assert.Contains("><", stdout);
    }

    [Fact]
    public void CurrencySign_WithPictureSymbol_ExplicitDollar()
    {
        // Explicit CURRENCY SIGN IS "$" WITH PICTURE SYMBOL "!" — $ in output, ! in PIC
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURR06.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "$" WITH PICTURE SYMBOL "!".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-AMOUNT PIC !!!,!!9.99.
            01 WS-RESULT PIC X(12).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 99.50 TO WS-AMOUNT.
                MOVE WS-AMOUNT TO WS-RESULT.
                DISPLAY ">" WS-RESULT "<".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // PIC uses !, output uses $
        Assert.Contains("$99.50", stdout);
        Assert.DoesNotContain("!", stdout);
    }

    // ==========================================
    // M411 — SCREEN SECTION (grammar island)
    // ==========================================

    [Fact]
    public void ScreenSection_Empty_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR01.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "Y".
            SCREEN SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Y", stdout);
    }

    [Fact]
    public void ScreenSection_SimpleValueLineCol_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR02.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "Z".
            SCREEN SECTION.
            01 MY-SCREEN.
               05 VALUE "Hello" LINE 1 COL 1.
               05 VALUE "World" LINE 2 COL 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Z", stdout);
    }

    [Fact]
    public void ScreenSection_PicUsing_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR03.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(10) VALUE "TEST".
            SCREEN SECTION.
            01 INPUT-SCREEN.
               05 PIC X(10) USING WS-NAME.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-NAME.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("TEST", stdout);
    }

    [Fact]
    public void ScreenSection_PicFromTo_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR04.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-SRC PIC X(5) VALUE "HELLO".
            01 WS-DST PIC X(5).
            SCREEN SECTION.
            01 EDIT-SCREEN.
               05 PIC X(5) FROM WS-SRC TO WS-DST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-SRC.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("HELLO", stdout);
    }

    [Fact]
    public void ScreenSection_AllAttributes_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR05.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "A".
            SCREEN SECTION.
            01 ATTR-SCREEN.
               05 VALUE "Test" BELL BLINK HIGHLIGHT
                  REVERSE-VIDEO UNDERLINE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("A", stdout);
    }

    [Fact]
    public void ScreenSection_Colors_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR06.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "C".
            SCREEN SECTION.
            01 COLOR-SCREEN.
               05 VALUE "Colored" FOREGROUND-COLOR 7
                  BACKGROUND-COLOR 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("C", stdout);
    }

    [Fact]
    public void ScreenSection_LinePlusColPlus_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR07.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "P".
            SCREEN SECTION.
            01 POS-SCREEN.
               05 VALUE "A" LINE 1 COL 1.
               05 VALUE "B" LINE NUMBER PLUS 1
                  COLUMN NUMBER PLUS 5.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("P", stdout);
    }

    [Fact]
    public void ScreenSection_BlankScreen_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR08.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "B".
            SCREEN SECTION.
            01 BLANK-SCREEN.
               05 BLANK SCREEN.
               05 VALUE "Header" LINE 1 COL 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("B", stdout);
    }

    [Fact]
    public void ScreenSection_EraseEolEos_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR09.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "E".
            SCREEN SECTION.
            01 ERASE-SCREEN.
               05 ERASE EOL.
               05 ERASE EOS.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("E", stdout);
    }

    [Fact]
    public void ScreenSection_SecureRequired_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR10.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-PWD PIC X(20).
            01 WS-X PIC X VALUE "S".
            SCREEN SECTION.
            01 LOGIN-SCREEN.
               05 PIC X(20) TO WS-PWD SECURE REQUIRED.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("S", stdout);
    }

    [Fact]
    public void ScreenSection_NestedLevels_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR11.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "N".
            SCREEN SECTION.
            01 OUTER-SCREEN.
               05 INNER-GROUP.
                  10 VALUE "X" LINE 1 COL 1.
                  10 VALUE "Y" LINE 2 COL 1.
               05 VALUE "Z" LINE 3 COL 1.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("N", stdout);
    }

    [Fact]
    public void ScreenSection_AutoFull_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCR12.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FIELD PIC X(10).
            01 WS-X PIC X VALUE "F".
            SCREEN SECTION.
            01 AUTO-SCREEN.
               05 PIC X(10) USING WS-FIELD AUTO FULL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("F", stdout);
    }
}
