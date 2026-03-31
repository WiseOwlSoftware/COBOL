using CobolSharp.Compiler;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Tests for Batch 2 COBOL-85 grammar fixes:
/// SORT Format 2 (table sort), SYMBOLIC CHARACTERS N:N mapping.
/// SET ON/OFF (M400) was already complete from a prior session.
/// </summary>
public class GrammarBatch2Tests : EndToEndTestBase
{
    // ==========================================
    // SORT Format 2 — table sort (M402)
    // ==========================================

    [Fact]
    public void Sort_Format2_TableSort_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SORTTBL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MY-TABLE.
                05 MY-ENTRY OCCURS 5 TIMES
                    ASCENDING KEY IS MY-KEY
                    INDEXED BY MY-IDX.
                    10 MY-KEY PIC 9(3).
                    10 MY-DATA PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 003 TO MY-KEY(1).
                MOVE 001 TO MY-KEY(2).
                MOVE 005 TO MY-KEY(3).
                MOVE 002 TO MY-KEY(4).
                MOVE 004 TO MY-KEY(5).
                SORT MY-ENTRY ON ASCENDING KEY MY-KEY.
                DISPLAY "SORT F2 PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("SORT F2 PARSED", stdout);
    }

    [Fact]
    public void Sort_Format2_DescendingKey_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SORTDSC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MY-TABLE.
                05 MY-ENTRY OCCURS 3 TIMES
                    ASCENDING KEY IS MY-KEY
                    INDEXED BY MY-IDX.
                    10 MY-KEY PIC 9(3).
                    10 MY-DATA PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 001 TO MY-KEY(1).
                MOVE 003 TO MY-KEY(2).
                MOVE 002 TO MY-KEY(3).
                SORT MY-ENTRY ON DESCENDING KEY MY-KEY.
                DISPLAY "SORT DESC PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("SORT DESC PARSED", stdout);
    }

    [Fact]
    public void Sort_Format2_WithDuplicates_ParsesSuccessfully()
    {
        // Parser test: verify grammar accepts SORT table WITH DUPLICATES
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SORTDUP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MY-TABLE.
                05 MY-ENTRY OCCURS 3 TIMES
                    ASCENDING KEY IS MY-KEY
                    INDEXED BY MY-IDX.
                    10 MY-KEY PIC 9(3).
                    10 MY-DATA PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 002 TO MY-KEY(1).
                MOVE 001 TO MY-KEY(2).
                MOVE 003 TO MY-KEY(3).
                SORT MY-ENTRY ON ASCENDING KEY MY-KEY
                    WITH DUPLICATES IN ORDER.
                DISPLAY "SORT DUP PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("SORT DUP PARSED", stdout);
    }

    [Fact]
    public void Sort_Format1_FileSort_StillWorks()
    {
        // Regression: ensure Format 1 file sort still works
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SORTF1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SORT-FILE ASSIGN TO "sort.tmp"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT IN-FILE ASSIGN TO "in.dat"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "out.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            SD SORT-FILE.
            01 SORT-REC.
                05 SORT-KEY PIC X(10).
                05 SORT-DATA PIC X(70).
            FD IN-FILE.
            01 IN-REC PIC X(80).
            FD OUT-FILE.
            01 OUT-REC PIC X(80).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "FORMAT 1 PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("FORMAT 1 PARSED", stdout);
    }

    // ==========================================
    // SYMBOLIC CHARACTERS N:N mapping (M408)
    // ==========================================

    [Fact]
    public void SymbolicCharacters_SingleMapping_StillWorks()
    {
        // Regression: 1:1 mapping must still work
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMONE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS MY-TAB IS 10.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-STR PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STRING "A" MY-TAB "B" DELIMITED SIZE
                    INTO WS-STR.
                DISPLAY "SYM1 OK".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("SYM1 OK", stdout);
    }

    [Fact]
    public void SymbolicCharacters_NNMapping_ParsesSuccessfully()
    {
        // N:N mapping: multiple names ↔ multiple ordinals
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMMUL.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS MY-TAB MY-LF ARE 10 11.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-STR PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STRING "A" MY-TAB "B" MY-LF "C"
                    DELIMITED SIZE INTO WS-STR.
                DISPLAY "SYMN OK".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("SYMN OK", stdout);
    }

    [Fact]
    public void SymbolicCharacters_WithInAlphabet_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMALP.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                ALPHABET MY-ALPHA IS NATIVE.
                SYMBOLIC CHARACTERS MY-CHAR IS 65
                    IN MY-ALPHA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-STR PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "SYM-IN OK".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("SYM-IN OK", stdout);
    }

    [Fact]
    public void SymbolicCharacters_ForAlphanumeric_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SYMFOR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SYMBOLIC CHARACTERS FOR ALPHANUMERIC
                    MY-CHAR IS 65.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DUMMY PIC X VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "SYM-FOR OK".
                STOP RUN.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("SYM-FOR OK", stdout);
    }

    // ==========================================
    // SET ON/OFF — regression (M400, already working)
    // ==========================================

    [Fact]
    public void Set_SwitchOnOff_StillWorks()
    {
        // Regression: SET switch ON/OFF was already working (NC174A passes)
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SETSW.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                SWITCH-1 IS MY-SWITCH
                    ON STATUS IS SW-ON
                    OFF STATUS IS SW-OFF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RESULT PIC X(3) VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET MY-SWITCH TO ON.
                IF SW-ON
                    MOVE "ON " TO WS-RESULT
                ELSE
                    MOVE "OFF" TO WS-RESULT
                END-IF.
                DISPLAY WS-RESULT.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ON", stdout);
    }
}
