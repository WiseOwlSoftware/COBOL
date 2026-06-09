// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The three-operand <c>… TO/FROM … GIVING …</c> arithmetic forms (ISO/IEC 1989:2023 §14.9.1 / §14.9.42): the
/// TO/FROM operand participates in the sum/difference but is NOT a receiver — only the GIVING operands receive.
/// Pinned to the legacy oracle.
/// </summary>
public sealed class ArithmeticGivingDifferentialTests
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
        PROGRAM-ID. ARGIV.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    private const string Vars = "01 A PIC 9(4) VALUE 0010.\n01 B PIC 9(4) VALUE 0003.\n01 C PIC 9(4).";

    [Fact]
    public void AddToGiving_IncludesTheToOperand()
        // ADD A TO B GIVING C  →  C = B + A = 13 (the TO operand B is an addend; B is not modified).
        => AssertSameAsLegacy(Program(Vars, "    ADD A TO B GIVING C.\n    DISPLAY C \"|\" B."));   // 0013|0003

    [Fact]
    public void AddSeveralGiving()
        => AssertSameAsLegacy(Program(Vars, "    ADD A B GIVING C.\n    DISPLAY C."));   // 0013

    [Fact]
    public void SubtractFromGiving()
        // SUBTRACT A FROM B GIVING C  →  C = B - A (B not modified).
        => AssertSameAsLegacy(Program("01 A PIC 9(4) VALUE 0003.\n01 B PIC 9(4) VALUE 0010.\n01 C PIC 9(4).",
            "    SUBTRACT A FROM B GIVING C.\n    DISPLAY C \"|\" B."));   // 0007|0010

    [Fact]
    public void MultiplyGiving()
        => AssertSameAsLegacy(Program(Vars, "    MULTIPLY A BY B GIVING C.\n    DISPLAY C."));   // 0030

    [Fact]
    public void DivideIntoGiving()
        => AssertSameAsLegacy(Program("01 A PIC 9(4) VALUE 0003.\n01 B PIC 9(4) VALUE 0012.\n01 C PIC 9(4).",
            "    DIVIDE A INTO B GIVING C.\n    DISPLAY C."));   // 0004
}
