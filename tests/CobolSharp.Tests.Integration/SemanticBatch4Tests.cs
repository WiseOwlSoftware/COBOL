using CobolSharp.Compiler;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Tests for Batch 4 semantic/runtime fixes:
/// M428: SYMBOLIC CHARACTERS N:N count validation
/// M427: SORT Format 2 table sort runtime
/// </summary>
public class SemanticBatch4Tests : EndToEndTestBase
{
    // ==========================================
    // M428 — SYMBOLIC CHARACTERS N:N validation
    // ==========================================

    [Fact]
    public void SymbolicCharacters_NNValid_NoError()
    {
        // 2 names, 2 ordinals — valid
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYM01.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS SYM-A SYM-B ARE 65 66.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "Y".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Y", stdout);
    }

    [Fact]
    public void SymbolicCharacters_SingleValid_NoError()
    {
        // 1 name, 1 ordinal — valid
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYM02.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS SYM-X IS 88.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "Z".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Z", stdout);
    }

    [Fact]
    public void SymbolicCharacters_CountMismatch_ProducesError()
    {
        // 2 names, 1 ordinal — invalid per §12.3.7 rule 16c
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYM03.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS SYM-A SYM-B IS 65.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X VALUE "X".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                STOP RUN.
            """);

        // Should still compile (diagnostic is non-fatal) but diagnostic emitted
        // The first name maps to the single ordinal; second name has no mapping
        Assert.Contains("counts must be equal", stderr);
    }

    // ==========================================
    // M427 — SORT Format 2 (table sort) runtime
    // ==========================================

    [Fact]
    public void Sort_Format2_TableSort_AscendingKey()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSORT1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MY-TABLE.
                05 MY-ENTRY OCCURS 5 TIMES
                    ASCENDING KEY IS MY-KEY
                    INDEXED BY MY-IDX.
                    10 MY-KEY PIC 9(3).
                    10 MY-DATA PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 003 TO MY-KEY(1).
                MOVE "CCC" TO MY-DATA(1).
                MOVE 001 TO MY-KEY(2).
                MOVE "AAA" TO MY-DATA(2).
                MOVE 005 TO MY-KEY(3).
                MOVE "EEE" TO MY-DATA(3).
                MOVE 002 TO MY-KEY(4).
                MOVE "BBB" TO MY-DATA(4).
                MOVE 004 TO MY-KEY(5).
                MOVE "DDD" TO MY-DATA(5).
                SORT MY-ENTRY ON ASCENDING KEY MY-KEY.
                DISPLAY MY-DATA(1) MY-DATA(2) MY-DATA(3)
                        MY-DATA(4) MY-DATA(5).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("AAABBBCCCDDDEEE", stdout);
    }

    [Fact]
    public void Sort_Format2_TableSort_DescendingKey()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSORT2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MY-TABLE.
                05 MY-ENTRY OCCURS 3 TIMES
                    ASCENDING KEY IS MY-KEY
                    INDEXED BY MY-IDX.
                    10 MY-KEY PIC 9(3).
                    10 MY-DATA PIC X(1).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 001 TO MY-KEY(1).
                MOVE "A" TO MY-DATA(1).
                MOVE 003 TO MY-KEY(2).
                MOVE "C" TO MY-DATA(2).
                MOVE 002 TO MY-KEY(3).
                MOVE "B" TO MY-DATA(3).
                SORT MY-ENTRY ON DESCENDING KEY MY-KEY.
                DISPLAY MY-DATA(1) MY-DATA(2) MY-DATA(3).
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CBA", stdout);
    }
}
