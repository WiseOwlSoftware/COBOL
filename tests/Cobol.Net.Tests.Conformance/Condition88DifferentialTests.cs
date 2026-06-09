// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2c capability checkpoint: <b>level-88 condition-names</b> and <b>sign conditions</b> (COBOLNET_DESIGN §3.5 /
/// §8.8.4.1). An 88 reference is a membership test over its conditional variable (singletons + THRU ranges, multiple
/// VALUEs); <c>SET cond TO TRUE</c> moves the first VALUE into the parent. Pinned to the legacy oracle.
/// </summary>
public sealed class Condition88DifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source)
    {
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(lout, cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. C88TEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void BooleanFlag_AndSetToTrue()
        => AssertSameAsLegacy(Program("""
            01 WS-FLAG PIC X VALUE "N".
               88 FLAG-YES VALUE "Y".
               88 FLAG-NO  VALUE "N".
            """, """
                IF FLAG-YES DISPLAY "Y1" ELSE DISPLAY "N1" END-IF.
                IF FLAG-NO  DISPLAY "ISNO" END-IF.
                SET FLAG-YES TO TRUE.
                IF FLAG-YES DISPLAY "Y2" ELSE DISPLAY "N2" END-IF.
                DISPLAY WS-FLAG.
            """));

    [Fact]
    public void NumericRange_Thru()
        => AssertSameAsLegacy(Program("""
            01 WS-GRADE PIC 9(2) VALUE 75.
               88 PASSING VALUE 60 THRU 100.
               88 FAILING VALUE 0 THRU 59.
               88 PERFECT VALUE 100.
            """, """
                IF PASSING DISPLAY "PASS" ELSE DISPLAY "NOPASS" END-IF.
                IF FAILING DISPLAY "FAIL" ELSE DISPLAY "NOFAIL" END-IF.
                IF PERFECT DISPLAY "PERFECT" ELSE DISPLAY "IMPERFECT" END-IF.
            """));

    [Fact]
    public void MultipleValues()
        => AssertSameAsLegacy(Program("""
            01 WS-CODE PIC X VALUE "E".
               88 VOWEL VALUE "A", "E", "I", "O", "U".
            """, """
                IF VOWEL DISPLAY "VOWEL" ELSE DISPLAY "CONSONANT" END-IF.
                MOVE "B" TO WS-CODE.
                IF VOWEL DISPLAY "VOWEL2" ELSE DISPLAY "CONSONANT2" END-IF.
            """));

    [Fact]
    public void ConditionInCompoundExpression()
        => AssertSameAsLegacy(Program("""
            01 WS-A PIC 9 VALUE 5.
               88 A-IS-FIVE VALUE 5.
            01 WS-B PIC X VALUE "Q".
               88 B-IS-Q VALUE "Q".
            """, """
                IF A-IS-FIVE AND B-IS-Q DISPLAY "BOTH" ELSE DISPLAY "NOTBOTH" END-IF.
                IF A-IS-FIVE OR B-IS-Q DISPLAY "EITHER" END-IF.
                IF NOT A-IS-FIVE DISPLAY "NOTFIVE" ELSE DISPLAY "ISFIVE" END-IF.
            """));

    [Theory]
    // Sign conditions (POSITIVE / NEGATIVE / ZERO), with and without NOT.
    [InlineData("01 N PIC S9(3) VALUE -5.", "    IF N IS NEGATIVE DISPLAY \"NEG\" END-IF.\n    IF N IS NOT POSITIVE DISPLAY \"NOTPOS\" END-IF.")]
    [InlineData("01 N PIC S9(3) VALUE 0.", "    IF N IS ZERO DISPLAY \"ZERO\" END-IF.\n    IF N IS POSITIVE DISPLAY \"POS\" ELSE DISPLAY \"NOTPOS\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 7.", "    IF N IS POSITIVE DISPLAY \"POS\" END-IF.")]
    public void SignConditions(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));
}
