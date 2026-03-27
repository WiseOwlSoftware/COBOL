using System.Diagnostics;
using CobolSharp.Compiler;
using CobolSharp.Compiler.Diagnostics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Integration tests for 8 P0 bug fixes. Each test exercises a specific
/// fixed bug by compiling and running a minimal COBOL program.
/// </summary>
public class P0BugFixTests : EndToEndTestBase
{
    [Fact]
    public void OpenMultiClause_BothFilesOpened()
    {
        // Create the input file before running
        string inputPath = Path.Combine(_tempDir, "inpfile.txt");
        File.WriteAllText(inputPath, "INPUTDATA\n");

        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OPNMULTI.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT INP-FILE ASSIGN TO "inpfile"
                    ORGANIZATION IS LINE SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "outfile"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD INP-FILE.
            01 INP-REC PIC X(9).
            FD OUT-FILE.
            01 OUT-REC PIC X(9).
            WORKING-STORAGE SECTION.
            01 WS-BUF PIC X(9).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN INPUT INP-FILE OUTPUT OUT-FILE.
                MOVE "WRITEDATA" TO OUT-REC.
                WRITE OUT-REC.
                READ INP-FILE INTO WS-BUF
                    AT END DISPLAY "EMPTY"
                END-READ.
                CLOSE INP-FILE.
                CLOSE OUT-FILE.
                DISPLAY WS-BUF.
                OPEN INPUT OUT-FILE.
                READ OUT-FILE INTO WS-BUF
                    AT END DISPLAY "EMPTY"
                END-READ.
                CLOSE OUT-FILE.
                DISPLAY WS-BUF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("INPUTDATA", lines[0]);
        Assert.Equal("WRITEDATA", lines[1]);
    }


    [Fact]
    public void NumericEditedToNumericEdited_PreservesValue()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NUMEDIT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-ED1 PIC Z,ZZ9.99.
            01 WS-ED2 PIC Z,ZZ9.99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE 1234.56 TO WS-ED1.
                MOVE WS-ED1 TO WS-ED2.
                DISPLAY WS-ED2.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Contains("1234.56", stdout.Replace(",", "").Trim());
    }


    [Fact]
    public void LocalStorage_DoesNotCorruptWorkingStorage()
    {
        // Declaring LOCAL-STORAGE fields should not corrupt
        // WORKING-STORAGE field offsets or values.
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LOCSTOR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FIELD1 PIC X(10) VALUE "AAAAAAAAAA".
            01 WS-FIELD2 PIC 9(5) VALUE 12345.
            LOCAL-STORAGE SECTION.
            01 LS-FIELD PIC X(10) VALUE "LSVALUE".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-FIELD1.
                DISPLAY WS-FIELD2.
                MOVE "BBBBBBBBBB" TO WS-FIELD1.
                DISPLAY WS-FIELD1.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("AAAAAAAAAA", lines[0]);
        Assert.Equal("12345", lines[1]);
        Assert.Equal("BBBBBBBBBB", lines[2]);
    }


    [Fact]
    public void UserDefinedClass_ProducesDiagnostic()
    {
        // Compile a program with user-defined CLASS in SPECIAL-NAMES.
        // The grammar doesn't support user-defined class names in conditions,
        // so verify the compiler produces a parse diagnostic (not a crash).
        string sourcePath = Path.Combine(_tempDir, "classtest.cob");
        string outputPath = Path.Combine(_tempDir, "classtest.dll");
        File.WriteAllText(sourcePath, """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLSTEST.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CLASS MY-CLASS IS "ABC".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLD PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-FLD IS MY-CLASS
                    DISPLAY "CLASS-YES"
                ELSE
                    DISPLAY "CLASS-NO"
                END-IF.
                STOP RUN.
            """);

        var compilation = new Compilation();
        var result = compilation.Compile(sourcePath, outputPath);

        // The compiler should NOT crash — it should produce diagnostics
        // (either a parse error for unrecognized class name, or COBOL0413 warning)
        Assert.NotEmpty(result.Diagnostics);
        Assert.True(
            result.Diagnostics.Any(d => d.Code == "COBOL0413" || d.Code == "COBOL0001"),
            $"Expected COBOL0413 or COBOL0001, got: {string.Join(", ", result.Diagnostics.Select(d => d.Code))}");
    }


    [Fact]
    public void ClassCondition_RefMod_NoCrash()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. CLSREFMOD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-FLD PIC X(6) VALUE "123ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-FLD(1:3) IS NUMERIC
                    DISPLAY "NUM-YES"
                ELSE
                    DISPLAY "NUM-NO"
                END-IF.
                IF WS-FLD(4:3) IS NUMERIC
                    DISPLAY "ALPHA-NUM"
                ELSE
                    DISPLAY "ALPHA-NOT"
                END-IF.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("NUM-YES", lines[0]);
        Assert.Equal("ALPHA-NOT", lines[1]);
    }


    [Fact]
    public void FileStatus_CorrectCodes()
    {
        // Close a file that isn't open — should get status 42
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FSTEST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SEQ-FILE ASSIGN TO "fstest"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD SEQ-FILE.
            01 SEQ-REC PIC X(10).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                CLOSE SEQ-FILE.
                DISPLAY WS-FS.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("42", stdout);
    }


    [Fact]
    public void WriteDuplicateKey_FileStatusReflectsError()
    {
        // Write two records with the same key to an indexed file.
        // FILE STATUS should reflect the duplicate key error (status 22).
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. WRDUPKEY.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixdupkey"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IX-FILE.
            01 IX-REC.
               05 IX-KEY PIC X(3).
               05 IX-VAL PIC X(3).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IX-FILE.
                MOVE "AAA" TO IX-KEY.
                MOVE "111" TO IX-VAL.
                WRITE IX-REC.
                DISPLAY "FIRST=" WS-FS.
                MOVE "AAA" TO IX-KEY.
                MOVE "222" TO IX-VAL.
                WRITE IX-REC.
                DISPLAY "DUP=" WS-FS.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("FIRST=00", lines[0]);
        Assert.Equal("DUP=22", lines[1]);
    }


    [Fact]
    public void ReadNonExistentKey_FileStatusReflectsError()
    {
        // Read with a non-existent key from an indexed file.
        // FILE STATUS should reflect the key-not-found error (status 23).
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RDNOKEY.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixrdkey"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-KEY
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IX-FILE.
            01 IX-REC.
               05 IX-KEY PIC X(3).
               05 IX-VAL PIC X(3).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IX-FILE.
                MOVE "AAA" TO IX-KEY.
                MOVE "111" TO IX-VAL.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                MOVE "ZZZ" TO IX-KEY.
                READ IX-FILE.
                DISPLAY WS-FS.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("23", stdout);
    }
}
