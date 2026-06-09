// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Figurative constants (ZERO / SPACE …) in operand position — MOVE source, DISPLAY operand, and comparison
/// (ISO §8.3.1.2). Ubiquitous in real COBOL (every NC program uses <c>MOVE ZERO</c>/<c>SPACES</c>), so this is on
/// the G3-core path to running a real NC program through the differential harness. The constant materializes to the
/// receiving / other operand's category and width.
/// </summary>
public sealed class FigurativeDifferentialTests
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

    private static void AssertSpec(string source, string expected)
    {
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"COBOL.NET failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FIGTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Theory]
    // MOVE a figurative to a numeric / alphanumeric receiver (filled to width); trailing-clean → legacy-valid.
    [InlineData("01 N PIC 9(4).", "    MOVE ZERO TO N.\n    DISPLAY N.")]
    [InlineData("01 X PIC X(5).", "    MOVE ZEROS TO X.\n    DISPLAY \"[\" X \"]\".")]   // "00000" trailing-clean
    [InlineData("01 N PIC 9(3) VALUE 7.", "    MOVE ZERO TO N.\n    DISPLAY N.")]
    // DISPLAY of a figurative → one occurrence (GR3).
    [InlineData("01 FILLER PIC X.", "    DISPLAY \"[\" ZERO \"]\".")]
    [InlineData("01 FILLER PIC X.", "    DISPLAY \"[\" SPACE \"]\".")]
    public void FigurativeMoveDisplay(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // MOVE SPACES to alphanumeric, exposed by a trailing "]" → spec-pinned (legacy trims trailing spaces).
    [InlineData("01 X PIC X(4).", "    MOVE SPACES TO X.\n    DISPLAY \"[\" X \"]\".", "[    ]")]
    public void FigurativeMove_FullFieldWidth(string ws, string proc, string expected) => AssertSpec(Program(ws, proc), expected);

    [Theory]
    // Comparison against a figurative — materialized to the other operand's category/width.
    [InlineData("01 N PIC 9(3) VALUE 0.", "    IF N = ZERO DISPLAY \"ISZERO\" ELSE DISPLAY \"NONZERO\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 5.", "    IF N = ZERO DISPLAY \"ISZERO\" ELSE DISPLAY \"NONZERO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE SPACES.", "    IF X = SPACES DISPLAY \"ALLSPACE\" ELSE DISPLAY \"NOT\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"AB\".", "    IF X = SPACES DISPLAY \"ALLSPACE\" ELSE DISPLAY \"NOT\" END-IF.")]
    [InlineData("01 N PIC 9(3) VALUE 0.", "    IF N IS ZERO DISPLAY \"SIGNZERO\" END-IF.")]
    public void FigurativeComparison(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));
}
