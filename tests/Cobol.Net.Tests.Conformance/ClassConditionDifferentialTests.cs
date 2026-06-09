// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Class conditions (ISO §8.8.4.1.4): <c>IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-UPPER | ALPHABETIC-LOWER}</c>.
/// ALPHABETIC is the closed Latin set {A–Z, a–z, space} (not <c>char.IsLetter</c>); a typed-numeric field IS NUMERIC
/// folds to true. Pinned to the legacy oracle.
/// </summary>
public sealed class ClassConditionDifferentialTests
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
        PROGRAM-ID. CLSTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Theory]
    [InlineData("01 X PIC X(3) VALUE \"ABC\".", "    IF X IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"A1C\".", "    IF X IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"AB\".", "    IF X IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]   // "AB " — space is alphabetic
    [InlineData("01 X PIC X(3) VALUE \"123\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"12A\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"1 3\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]   // embedded space → not numeric
    [InlineData("01 N PIC 9(3) VALUE 42.", "    IF N IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 42.", "    IF N IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]   // digits not alphabetic
    [InlineData("01 X PIC X(3) VALUE \"ABC\".", "    IF X IS ALPHABETIC-UPPER DISPLAY \"UP\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"abc\".", "    IF X IS ALPHABETIC-UPPER DISPLAY \"UP\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"abc\".", "    IF X IS ALPHABETIC-LOWER DISPLAY \"LO\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"A1C\".", "    IF X IS NOT NUMERIC DISPLAY \"NN\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"ABC\".", "    IF X IS NOT ALPHABETIC DISPLAY \"NA\" ELSE DISPLAY \"AL\" END-IF.")]
    public void ClassConditions(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));
}
