// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// G2-1a capability checkpoint: <b>group items</b> (→ nested <c>record struct</c>s), <b>qualified references</b>
/// (OF/IN, resolved through the <c>ByName</c> multimap), <b>figurative-constant VALUE</b> (ZERO/SPACE), and
/// member access through <c>ReferenceResolver</c>→<c>Place</c> in DISPLAY / MOVE / arithmetic. Pinned to the legacy
/// oracle on the NIST acceptance basis, or to the spec where the legacy's DISPLAY trailing-trim is non-conforming.
/// Scope still excludes OCCURS subscripts + ref-mod (G2-1b), level-88 (G2c), signed-DISPLAY overpunch (G2d), and
/// whole-group DISPLAY (G6) — those reference forms fail loud (<c>NotImplemented</c>) until their slice lands.
/// </summary>
public sealed class GroupDataDifferentialTests
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

    private static string Program(string workingStorage, string procedure) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. GRPTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {workingStorage}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {procedure}
            STOP RUN.
        """;

    private const string Rec = """
        01 WS-REC.
           05 WS-NAME PIC X(6) VALUE "BOB".
           05 WS-NUM  PIC 9(4) VALUE 42.
        """;

    [Fact]
    public void GroupLeaf_Display()
        => AssertSameAsLegacy(Program(Rec, "    DISPLAY WS-NAME.\n    DISPLAY WS-NUM."));

    [Fact]
    public void GroupLeaf_MoveThenDisplay()
        => AssertSameAsLegacy(Program(Rec, """
                MOVE "CAT" TO WS-NAME.
                MOVE 7 TO WS-NUM.
                DISPLAY WS-NAME.
                DISPLAY WS-NUM.
            """));

    [Fact]
    public void GroupLeaves_Arithmetic()
        => AssertSameAsLegacy(Program("""
            01 WS-REC.
               05 A PIC 9(3) VALUE 10.
               05 B PIC 9(3) VALUE 20.
               05 R PIC 9(4).
            """, "    ADD A B GIVING R.\n    DISPLAY R."));

    [Fact]
    public void NestedGroup_MemberAccess()
        => AssertSameAsLegacy(Program("""
            01 OUTER.
               05 MID.
                  10 LEAF  PIC 9(3) VALUE 5.
                  10 LEAF2 PIC X(2) VALUE "Z".
            """, "    DISPLAY LEAF.\n    DISPLAY LEAF2."));

    [Fact]
    public void QualifiedName_OfIn()
        => AssertSameAsLegacy(Program("""
            01 REC-A.
               05 FLD PIC X(3) VALUE "AAA".
            01 REC-B.
               05 FLD PIC X(3) VALUE "BBB".
            """, "    DISPLAY FLD OF REC-A.\n    DISPLAY FLD IN REC-B."));

    [Fact]
    public void QualifiedName_NestedScope()
        => AssertSameAsLegacy(Program("""
            01 GRP-A.
               05 SUB-A.
                  10 AMT PIC 9(3) VALUE 7.
            01 GRP-B.
               05 AMT PIC 9(3) VALUE 9.
            """, "    DISPLAY AMT OF SUB-A.\n    DISPLAY AMT OF GRP-B."));

    [Theory]
    // Figurative-constant VALUE that is trailing-clean → legacy is a valid oracle.
    [InlineData("01 WS-Z PIC 9(4) VALUE ZERO.", "    DISPLAY WS-Z.")]
    [InlineData("01 WS-Z PIC 9(2) VALUE ZEROS.", "    DISPLAY WS-Z.")]
    [InlineData("01 WS-AZ PIC X(3) VALUE ZEROS.", "    DISPLAY WS-AZ.")]
    public void FigurativeValue(string ws, string proc) => AssertSameAsLegacy(Program(ws, proc));

    [Theory]
    // Figurative VALUE exposing internal trailing fill (a trailing "]") → spec-pinned (the legacy trims).
    [InlineData("01 WS-S PIC X(5) VALUE SPACES.", "    DISPLAY \"[\" WS-S \"]\".", "[     ]")]
    [InlineData("01 WS-AZ PIC X(4) VALUE ZEROS.", "    DISPLAY \"[\" WS-AZ \"]\".", "[0000]")]
    public void FigurativeValue_FullFieldWidth(string ws, string proc, string expected)
        => AssertSpec(Program(ws, proc), expected);
}
