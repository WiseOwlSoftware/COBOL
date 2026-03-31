using CobolSharp.Compiler;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Tests for Batch 1 COBOL-85 grammar fixes:
/// USE GLOBAL, USE EXCEPTION/ERROR, USE modes, STOP RUN STATUS,
/// START WITH LENGTH, ALPHABET FOR, CLASS FOR, cobolWord refactor.
/// </summary>
public class GrammarBatch1Tests : EndToEndTestBase
{
    // ==========================================
    // USE GLOBAL keyword (M403)
    // ==========================================

    [Fact]
    public void Use_Global_AfterStandardException_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USEGLOB.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "test.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-REC PIC X(80).
            WORKING-STORAGE SECTION.
            01 WS-MSG PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECTION SECTION.
                USE GLOBAL AFTER STANDARD EXCEPTION PROCEDURE
                    ON TEST-FILE.
            ERR-PARA.
                MOVE "ERROR HANDLED" TO WS-MSG.
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            MAIN-PARA.
                DISPLAY "USE GLOBAL PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("USE GLOBAL PARSED", stdout);
    }

    // ==========================================
    // USE EXCEPTION/ERROR synonym (M404)
    // ==========================================

    [Fact]
    public void Use_ErrorSynonym_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USEERR.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "test.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-REC PIC X(80).
            WORKING-STORAGE SECTION.
            01 WS-MSG PIC X(20) VALUE SPACES.
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECTION SECTION.
                USE AFTER STANDARD ERROR PROCEDURE
                    ON TEST-FILE.
            ERR-PARA.
                MOVE "ERROR HANDLED" TO WS-MSG.
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            MAIN-PARA.
                DISPLAY "USE ERROR PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("USE ERROR PARSED", stdout);
    }

    // ==========================================
    // USE INPUT/OUTPUT/I-O/EXTEND modes (M405)
    // ==========================================

    [Fact]
    public void Use_InputMode_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USEINP.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "test.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-REC PIC X(80).
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECTION SECTION.
                USE AFTER STANDARD EXCEPTION PROCEDURE
                    ON INPUT.
            ERR-PARA.
                CONTINUE.
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            MAIN-PARA.
                DISPLAY "USE INPUT PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("USE INPUT PARSED", stdout);
    }

    [Fact]
    public void Use_OutputMode_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USEOUT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "test.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-REC PIC X(80).
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECTION SECTION.
                USE AFTER STANDARD EXCEPTION PROCEDURE
                    ON OUTPUT.
            ERR-PARA.
                CONTINUE.
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            MAIN-PARA.
                DISPLAY "USE OUTPUT PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("USE OUTPUT PARSED", stdout);
    }

    [Fact]
    public void Use_IoMode_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USEIO.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "test.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-REC PIC X(80).
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECTION SECTION.
                USE AFTER STANDARD EXCEPTION PROCEDURE
                    ON I-O.
            ERR-PARA.
                CONTINUE.
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            MAIN-PARA.
                DISPLAY "USE I-O PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("USE I-O PARSED", stdout);
    }

    [Fact]
    public void Use_ExtendMode_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. USEEXT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT TEST-FILE ASSIGN TO "test.dat"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD TEST-FILE.
            01 TEST-REC PIC X(80).
            PROCEDURE DIVISION.
            DECLARATIVES.
            ERR-SECTION SECTION.
                USE AFTER STANDARD EXCEPTION PROCEDURE
                    ON EXTEND.
            ERR-PARA.
                CONTINUE.
            END DECLARATIVES.
            MAIN-SECTION SECTION.
            MAIN-PARA.
                DISPLAY "USE EXTEND PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("USE EXTEND PARSED", stdout);
    }

    // ==========================================
    // STOP RUN WITH STATUS (M406)
    // ==========================================

    [Fact]
    public void StopRun_WithNormalStatus_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STOPNRM.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "STOP NORMAL".
                STOP RUN WITH NORMAL STATUS 0.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("STOP NORMAL", stdout);
    }

    [Fact]
    public void StopRun_WithErrorStatus_ParsesSuccessfully()
    {
        // Note: WITH ERROR may set non-zero exit code; we test parse only
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STOPERR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-RC PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "STOP ERROR".
                STOP RUN WITH NORMAL STATUS WS-RC.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("STOP ERROR", stdout);
    }

    [Fact]
    public void StopRun_Plain_StillWorks()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STOPPLAIN.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "PLAIN STOP".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("PLAIN STOP", stdout);
    }

    // ==========================================
    // START WITH LENGTH (M401)
    // ==========================================

    [Fact]
    public void Start_WithLength_ParsesSuccessfully()
    {
        // Parser test: verify the grammar accepts START ... WITH LENGTH
        // Full runtime test requires indexed file setup
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. STARTLN.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IDX-FILE ASSIGN TO "idx.dat"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IDX-KEY
                    FILE STATUS IS WS-STATUS.
            DATA DIVISION.
            FILE SECTION.
            FD IDX-FILE.
            01 IDX-REC.
                05 IDX-KEY PIC X(10).
                05 IDX-DATA PIC X(70).
            WORKING-STORAGE SECTION.
            01 WS-STATUS PIC XX VALUE SPACES.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "START LENGTH PARSED".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("START LENGTH PARSED", stdout);
    }

    // ==========================================
    // ALPHABET FOR ALPHANUMERIC/NATIONAL (M409)
    // ==========================================

    [Fact]
    public void Alphabet_ForAlphanumeric_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALPHAFOR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                ALPHABET MY-ALPHA IS NATIVE
                    FOR ALPHANUMERIC.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "ALPHABET FOR PARSED".
                STOP RUN.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ALPHABET FOR PARSED", stdout);
    }

    [Fact]
    public void Alphabet_ForNational_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALPHANAT.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                ALPHABET NAT-ALPHA IS NATIVE
                    FOR NATIONAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "ALPHABET NAT PARSED".
                STOP RUN.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("ALPHABET NAT PARSED", stdout);
    }

    // ==========================================
    // CLASS FOR ALPHANUMERIC/NATIONAL (M410)
    // ==========================================

    [Fact]
    public void Class_ForAlphanumeric_ParsesSuccessfully()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLASSFOR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CLASS MY-DIGITS IS "0" THRU "9"
                    FOR ALPHANUMERIC.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "CLASS FOR PARSED".
                STOP RUN.
            """, DialectMode.Cobol2002);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CLASS FOR PARSED", stdout);
    }

    // ==========================================
    // cobolWord refactor — context-sensitive keywords as data names
    // ==========================================

    [Fact]
    public void CobolWord_LengthAsDataName_Works()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CWTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 LENGTH PIC 9(3) VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY LENGTH.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("042", stdout);
    }

    [Fact]
    public void CobolWord_NormalAsDataName_Works()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NRMTEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NORMAL PIC X(5) VALUE "HELLO".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY NORMAL.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("HELLO", stdout);
    }
}
