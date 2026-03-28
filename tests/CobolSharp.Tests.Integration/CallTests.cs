using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class CallTests : EndToEndTestBase
{
    [Fact]
    public void CallStatement_UnresolvedProgram_OnException()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CALLTEST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "SUBPROG"
                    ON EXCEPTION
                        DISPLAY "Not found"
                    NOT ON EXCEPTION
                        DISPLAY "After call"
                END-CALL.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("Not found", stdout);
    }

    // (FileIO_WriteAndReadBack moved to end of file with proper implementation)

    // ═══════════════════════════════════════════
    // EVALUATE tests
    // ═══════════════════════════════════════════


    [Fact]
    public void Call_ByReference_CalleeModifiesCallerStorage()
    {
        // BY REFERENCE: callee modifies caller's data via LINKAGE SECTION
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01  WS-VALUE PIC X(10) VALUE "BEFORE".
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY WS-VALUE
                    CALL "MODIFIER" USING WS-VALUE
                    DISPLAY WS-VALUE
                    STOP RUN.
            """),
            ("MODIFIER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. MODIFIER.
                DATA DIVISION.
                LINKAGE SECTION.
                01  LS-DATA PIC X(10).
                PROCEDURE DIVISION USING LS-DATA.
                MAIN-PARA.
                    MOVE "AFTER" TO LS-DATA
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("BEFORE", lines[0]);
        Assert.Equal("AFTER", lines[1]);
    }


    [Fact]
    public void Call_SimpleSubprogram_DisplaysFromCallee()
    {
        // Caller CALLs a subprogram which DISPLAYs a message
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY "BEFORE CALL"
                    CALL "CALLEE"
                    DISPLAY "AFTER CALL"
                    STOP RUN.
            """),
            ("CALLEE.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLEE.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY "INSIDE CALLEE"
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("BEFORE CALL", lines[0]);
        Assert.Equal("INSIDE CALLEE", lines[1]);
        Assert.Equal("AFTER CALL", lines[2]);
    }


    [Fact]
    public void Call_Cancel_RemovesFromRegistry()
    {
        // CANCEL removes program from registry; next CALL re-resolves
        var (success, stdout, stderr) = CompileMultipleAndRun(
            ("CALLER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. CALLER.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    CALL "HELPER"
                    CANCEL "HELPER"
                    CALL "HELPER"
                    STOP RUN.
            """),
            ("HELPER.cob", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. HELPER.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    DISPLAY "CALLED"
                    EXIT PROGRAM.
            """));

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("CALLED", lines[0]);
        Assert.Equal("CALLED", lines[1]);
    }


    [Fact]
    public void Call_OnException_UnresolvableProgram()
    {
        // CALL a non-existent program triggers ON EXCEPTION
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. EXCTEST.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CALL "NONEXISTENT"
                    ON EXCEPTION
                        DISPLAY "CAUGHT"
                    NOT ON EXCEPTION
                        DISPLAY "OK"
                END-CALL
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("CAUGHT", stdout);
    }


    [Fact]
    public void BatchCompilation_TwoProgramsInOneSource_CallBetween()
    {
        // Two programs in one source file with END PROGRAM headers
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BATCH-MAIN.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "IN MAIN"
                CALL "BATCH-SUB"
                DISPLAY "BACK IN MAIN"
                STOP RUN.
            END PROGRAM BATCH-MAIN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BATCH-SUB.
            PROCEDURE DIVISION.
            SUB-PARA.
                DISPLAY "IN SUB"
                EXIT PROGRAM.
            END PROGRAM BATCH-SUB.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("IN MAIN", lines[0]);
        Assert.Equal("IN SUB", lines[1]);
        Assert.Equal("BACK IN MAIN", lines[2]);
    }


    [Fact]
    public void NistIC222A_BatchCompilation_TwoPrograms()
    {
        // IC222A: Two programs in one source file testing CALL with END PROGRAM
        var (success, stdout, stderr) = CompileNistAndRun("IC222A",
            new Dictionary<string, string> { { "COBOL_SWITCH_1", "ON" } });

        Assert.True(success, $"Failed: {stderr}");
        // Should contain test output lines, no FAIL*
        Assert.DoesNotContain("FAIL*", stdout);
    }


    [Fact]
    public void NistIC223A_BatchCompilation_ByReference()
    {
        // IC223A: Two programs - tests BY REFERENCE parameter passing
        var (success, stdout, stderr) = CompileNistAndRun("IC223A",
            new Dictionary<string, string> { { "COBOL_SWITCH_1", "ON" } });

        Assert.True(success, $"Failed: {stderr}");
        Assert.DoesNotContain("FAIL*", stdout);
    }


    [Fact]
    public void NistIC224A_BatchCompilation_ByContent()
    {
        // IC224A: Two programs - tests BY CONTENT parameter passing
        var (success, stdout, stderr) = CompileNistAndRun("IC224A",
            new Dictionary<string, string> { { "COBOL_SWITCH_1", "ON" } });

        Assert.True(success, $"Failed: {stderr}");
        Assert.DoesNotContain("FAIL*", stdout);
    }


    [Fact]
    public void NistIC225A_BatchCompilation_InitialProgram()
    {
        // IC225A: Two programs - tests INITIAL program attribute
        var (success, stdout, stderr) = CompileNistAndRun("IC225A",
            new Dictionary<string, string> { { "COBOL_SWITCH_1", "ON" } });

        Assert.True(success, $"Failed: {stderr}");
        Assert.DoesNotContain("FAIL*", stdout);
    }


    [Fact]
    public void NistIC226A_BatchCompilation_ExternalData()
    {
        // IC226A: Two programs - tests EXTERNAL data
        var (success, stdout, stderr) = CompileNistAndRun("IC226A",
            new Dictionary<string, string> { { "COBOL_SWITCH_1", "ON" } });

        Assert.True(success, $"Failed: {stderr}");
        Assert.DoesNotContain("FAIL*", stdout);
    }


    // IC237A: Requires REDEFINES in LINKAGE SECTION (CBL3111 blocks it)
    // IC227A: Requires IS EXTERNAL on FD (not yet supported)
    // IC228A+: Requires GLOBAL clause for nested programs (not yet implemented)


    [Fact]
    public void NestedProgram_ContainedProgramCall()
    {
        // Nested (contained) program: IC228A-style structure
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OUTER.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "OUTER START"
                CALL "INNER"
                DISPLAY "OUTER END"
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INNER.
            PROCEDURE DIVISION.
            INNER-PARA.
                DISPLAY "INNER"
                EXIT PROGRAM.
            END PROGRAM INNER.
            END PROGRAM OUTER.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("OUTER START", lines[0]);
        Assert.Equal("INNER", lines[1]);
        Assert.Equal("OUTER END", lines[2]);
    }

}
