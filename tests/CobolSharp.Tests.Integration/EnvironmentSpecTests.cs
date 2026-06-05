// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Spec-conformance tests for Environment Division features that the NIST suite under-tests
/// (WS-SPEC workstream; docs/COBOL85_COMPLIANCE_PLAN.md §3, docs/SPEC_GAP_INVENTORY.md
/// "Environment Division" section). Every [Fact] asserts output observed from the CLI.
/// </summary>
public sealed class EnvironmentSpecTests : EndToEndTestBase
{
    // SPECIAL-NAMES: CURRENCY SIGN ... WITH PICTURE SYMBOL (distinct currency string vs
    // picture symbol). ISO/IEC 1989:2023 §12.3.7.2 (line 14128), §14407 GR23 / §45197:
    // "When the PICTURE SYMBOL phrase is specified, ... literal-8 is the associated currency
    // symbol" used in PICTURE strings, while literal-7 is the currency string placed into the
    // edited result. Here the currency string is "#" and the picture symbol is "@", so '@' is a
    // valid PICTURE editing symbol and the edited field shows the currency string '#'.
    // Not exercised by any NIST program (CLAUDE.md notes this was historically PICMODE-blocked).
    [Fact]
    public void CurrencySign_WithPictureSymbol_DistinctSymbol()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CURRTEST.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "@".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-AMT   PIC @99.99 VALUE 10.00.
            01 WS-FLOAT PIC @@@9.99 VALUE 12.34.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "[" WS-AMT "]".
                DISPLAY "[" WS-FLOAT "]".
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        // Fixed currency symbol position -> '#10.00'; floating -> currency string floats to the
        // left of the first significant digit with a leading blank: ' #12.34'.
        Assert.Equal("[#10.00]\r\n[ #12.34]".Replace("\r\n", "\n"),
            stdout.Replace("\r\n", "\n"));
    }

    // SPECIAL-NAMES: SYMBOLIC CHARACTERS clause. ISO/IEC 1989:2023 §12.3.7.2
    // symbolic-characters-clause (line 14235); GR16 (line 14311): integer-1 designates the
    // ORDINAL position (1-relative) in the native collating sequence to which the
    // symbolic-character is paired. FUNCTION ORD returns that same 1-relative ordinal, so
    // 'SYMBOLIC CHARACTERS HT IS 10' -> ORD(HT) = 10. The only NIST occurrence (NC401M) is in a
    // flagging M-module where the construct is flagged non-conforming, so no A-test exercises a
    // defined symbolic-character as a figurative constant.
    [Fact]
    public void SymbolicCharacters_DefinesFigurativeAtCollatingPosition()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMTEST.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS HT IS 10.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CHAR PIC X VALUE SPACE.
            01 WS-ORD  PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE HT TO WS-CHAR.
                COMPUTE WS-ORD = FUNCTION ORD(WS-CHAR).
                DISPLAY "ORD=" WS-ORD.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("ORD=010", stdout);
    }

    // SPECIAL-NAMES: CONSOLE IS mnemonic-name, then DISPLAY ... UPON mnemonic / UPON CONSOLE.
    // ISO/IEC 1989:2023 §12.3.7 (system-name relating, line 14092). No NIST A-test defines
    // CONSOLE IS mnemonic or does DISPLAY UPON CONSOLE (only generic device mnemonics in
    // M-modules). Verifies both the user mnemonic and the reserved CONSOLE name route to the
    // program's normal output stream.
    [Fact]
    public void SpecialNames_ConsoleMnemonic_DisplayUpon()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DISPCON.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CONSOLE IS CRT-DEV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "LINE1" UPON CRT-DEV.
                DISPLAY "LINE2" UPON CONSOLE.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("LINE1\r\nLINE2".Replace("\r\n", "\n"),
            stdout.Replace("\r\n", "\n"));
    }

    // SPECIAL-NAMES: device-name IS mnemonic-name, then DISPLAY ... UPON mnemonic.
    // ISO/IEC 1989:2023 §12.3.7.2 (device-name/feature-name IS mnemonic-name, line 14152).
    // No clean non-M A-test does DISPLAY ... UPON a device mnemonic; the form is only on a
    // passing path via flagging M-modules (NC220M). Here 'SYSOUT IS DISP-OUT' maps an output
    // device to a mnemonic and DISPLAY ... UPON DISP-OUT routes the text to that stream.
    [Fact]
    public void SpecialNames_DeviceMnemonic_DisplayUpon()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DISPMNE2.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYSOUT IS DISP-OUT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "HELLO" UPON DISP-OUT.
                STOP RUN.
            """);

        Assert.True(success, $"Execution failed: {stderr}");
        Assert.Equal("HELLO", stdout);
    }
}

