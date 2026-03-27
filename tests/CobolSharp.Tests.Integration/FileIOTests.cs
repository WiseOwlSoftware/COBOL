using System.Diagnostics;
using CobolSharp.Compiler;
using Xunit;

namespace CobolSharp.Tests.Integration;

public class FileIOTests : EndToEndTestBase
{
    [Fact]
    public void FileIO_WriteAndReadBack()
    {
        // Write 3 records, then read them back and display
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIO1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OUT-FILE ASSIGN TO "testfile"
                    ORGANIZATION IS SEQUENTIAL
                    ACCESS MODE IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD OUT-FILE.
            01 OUT-REC.
               05 OUT-TEXT PIC X(10).
            WORKING-STORAGE SECTION.
            01 WS-TEXT PIC X(10).
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT OUT-FILE.
                MOVE "AAAAAAAAAA" TO OUT-TEXT.
                WRITE OUT-REC.
                MOVE "BBBBBBBBBB" TO OUT-TEXT.
                WRITE OUT-REC.
                MOVE "CCCCCCCCCC" TO OUT-TEXT.
                WRITE OUT-REC.
                CLOSE OUT-FILE.
                OPEN INPUT OUT-FILE.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "UNEXPECTED-END"
                END-READ.
                DISPLAY WS-TEXT.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "UNEXPECTED-END"
                END-READ.
                DISPLAY WS-TEXT.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "UNEXPECTED-END"
                END-READ.
                DISPLAY WS-TEXT.
                READ OUT-FILE INTO WS-TEXT
                    AT END DISPLAY "AT-END-OK"
                END-READ.
                CLOSE OUT-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("AAAAAAAAAA", lines[0]);
        Assert.Equal("BBBBBBBBBB", lines[1]);
        Assert.Equal("CCCCCCCCCC", lines[2]);
        Assert.Equal("AT-END-OK", lines[3]);
    }


    [Fact]
    public void FileIO_ReadAtEnd_Branching()
    {
        // Create input file, then read until AT END
        // First: write a file with 2 records
        var (success1, _, stderr1) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOWR.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DAT-FILE ASSIGN TO "readtest"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD DAT-FILE.
            01 DAT-REC.
               05 DAT-NUM PIC 9(5).
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT DAT-FILE.
                MOVE 11111 TO DAT-NUM.
                WRITE DAT-REC.
                MOVE 22222 TO DAT-NUM.
                WRITE DAT-REC.
                CLOSE DAT-FILE.
                STOP RUN.
            """);
        Assert.True(success1, $"Write failed: {stderr1}");

        // Now read it back and count records
        var (success2, stdout2, stderr2) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIORD.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DAT-FILE ASSIGN TO "readtest"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD DAT-FILE.
            01 DAT-REC.
               05 DAT-NUM PIC 9(5).
            WORKING-STORAGE SECTION.
            01 WS-COUNT PIC 99 VALUE 0.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN INPUT DAT-FILE.
                PERFORM UNTIL WS-EOF = 1
                    READ DAT-FILE
                        AT END MOVE 1 TO WS-EOF
                        NOT AT END ADD 1 TO WS-COUNT
                    END-READ
                END-PERFORM.
                CLOSE DAT-FILE.
                DISPLAY WS-COUNT.
                STOP RUN.
            """);

        Assert.True(success2, $"Read failed: {stderr2}");
        Assert.Equal("02", stdout2);
    }


    [Fact]
    public void FileIO_OpenOutputCreatesFile()
    {
        // OPEN OUTPUT should create the file; WRITE should populate it
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOCR.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OUT-FILE ASSIGN TO "newfile"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD OUT-FILE.
            01 OUT-REC.
               05 OUT-DATA PIC X(5).
            WORKING-STORAGE SECTION.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT OUT-FILE.
                MOVE "HELLO" TO OUT-DATA.
                WRITE OUT-REC.
                CLOSE OUT-FILE.
                DISPLAY "DONE".
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("DONE", stdout);

        // Verify the file was created
        string filePath = Path.Combine(_tempDir, "newfile.txt");
        Assert.True(File.Exists(filePath), $"Output file not found at {filePath}");
        string content = File.ReadAllText(filePath);
        Assert.Contains("HELLO", content);
    }


    [Fact]
    public void FileIO_ReadNumericRoundTrip()
    {
        // Write numeric data, read it back, verify arithmetic works on it
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIONUM.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT NUM-FILE ASSIGN TO "numfile"
                    ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD NUM-FILE.
            01 NUM-REC.
               05 NUM-VAL PIC 9(5).
            WORKING-STORAGE SECTION.
            01 WS-SUM PIC 9(6) VALUE 0.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT NUM-FILE.
                MOVE 00100 TO NUM-VAL.
                WRITE NUM-REC.
                MOVE 00200 TO NUM-VAL.
                WRITE NUM-REC.
                MOVE 00300 TO NUM-VAL.
                WRITE NUM-REC.
                CLOSE NUM-FILE.
                OPEN INPUT NUM-FILE.
                PERFORM UNTIL WS-EOF = 1
                    READ NUM-FILE
                        AT END MOVE 1 TO WS-EOF
                        NOT AT END ADD NUM-VAL TO WS-SUM
                    END-READ
                END-PERFORM.
                CLOSE NUM-FILE.
                DISPLAY WS-SUM.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("000600", stdout);
    }


    [Fact]
    public void FileIO_FileStatus_00And10()
    {
        // Verify FILE STATUS is populated with "00" on success and "10" at end
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOSTATUS.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT DAT-FILE ASSIGN TO "statustest"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD DAT-FILE.
            01 DAT-REC.
               05 DAT-TEXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT DAT-FILE.
                DISPLAY FS.
                MOVE "HELLO" TO DAT-TEXT.
                WRITE DAT-REC.
                DISPLAY FS.
                CLOSE DAT-FILE.
                DISPLAY FS.
                OPEN INPUT DAT-FILE.
                DISPLAY FS.
                READ DAT-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY FS.
                READ DAT-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY FS.
                CLOSE DAT-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("00", lines[0]); // OPEN OUTPUT → 00
        Assert.Equal("00", lines[1]); // WRITE → 00
        Assert.Equal("00", lines[2]); // CLOSE → 00
        Assert.Equal("00", lines[3]); // OPEN INPUT → 00
        Assert.Equal("00", lines[4]); // READ (success) → 00
        Assert.Equal("10", lines[5]); // READ (at end) → 10
    }


    [Fact]
    public void FileIO_FileStatus_35_FileNotFound()
    {
        // Verify FILE STATUS is "35" when opening non-existent file for input
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOSTAT35.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT MISSING-FILE ASSIGN TO "nonexistent"
                    ORGANIZATION IS SEQUENTIAL
                    FILE STATUS IS FS.
            DATA DIVISION.
            FILE SECTION.
            FD MISSING-FILE.
            01 MISS-REC.
               05 MISS-TEXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN INPUT MISSING-FILE.
                DISPLAY FS.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("35", stdout);
    }


    [Fact]
    public void FileIO_Rewrite_ReplacesRecord()
    {
        // Write 2 records, reopen I-O, read first, rewrite it, read and verify
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIORW.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT RW-FILE ASSIGN TO "rwtest"
                    ORGANIZATION IS SEQUENTIAL
                    ACCESS MODE IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD RW-FILE.
            01 RW-REC.
               05 RW-TEXT PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-TEXT PIC X(5).
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT RW-FILE.
                MOVE "AAAAA" TO RW-TEXT.
                WRITE RW-REC.
                MOVE "BBBBB" TO RW-TEXT.
                WRITE RW-REC.
                CLOSE RW-FILE.
                OPEN INPUT RW-FILE.
                READ RW-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY RW-TEXT.
                READ RW-FILE
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY RW-TEXT.
                CLOSE RW-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("AAAAA", lines[0]);
        Assert.Equal("BBBBB", lines[1]);
    }


    [Fact]
    public void FileIO_LineSequential_RoundTrip()
    {
        // Explicit ORGANIZATION IS LINE SEQUENTIAL round-trip
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOLS.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT LS-FILE ASSIGN TO "lineseqtest"
                    ORGANIZATION IS LINE SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD LS-FILE.
            01 LS-REC.
               05 LS-TEXT PIC X(8).
            WORKING-STORAGE SECTION.
            01 WS-BUF PIC X(8).
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT LS-FILE.
                MOVE "LINESEQ1" TO LS-TEXT.
                WRITE LS-REC.
                MOVE "LINESEQ2" TO LS-TEXT.
                WRITE LS-REC.
                CLOSE LS-FILE.
                OPEN INPUT LS-FILE.
                READ LS-FILE INTO WS-BUF
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY WS-BUF.
                READ LS-FILE INTO WS-BUF
                    AT END MOVE 1 TO WS-EOF
                END-READ.
                DISPLAY WS-BUF.
                READ LS-FILE INTO WS-BUF
                    AT END DISPLAY "AT-END-OK"
                END-READ.
                CLOSE LS-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("LINESEQ1", lines[0]);
        Assert.Equal("LINESEQ2", lines[1]);
        Assert.Equal("AT-END-OK", lines[2]);
    }


    [Fact]
    public void FileIO_WriteFrom_CopiesBeforeWriting()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOWF.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT WF-FILE ASSIGN TO "wftest"
                    ORGANIZATION IS SEQUENTIAL
                    ACCESS MODE IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD WF-FILE.
            01 WF-REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-SRC PIC X(5) VALUE "HELLO".
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT WF-FILE.
                WRITE WF-REC FROM WS-SRC.
                CLOSE WF-FILE.
                OPEN INPUT WF-FILE.
                READ WF-FILE
                    AT END DISPLAY "EMPTY"
                END-READ.
                DISPLAY WF-REC.
                CLOSE WF-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("HELLO", stdout);
    }


    [Fact]
    public void FileIO_Delete_IndexedFile()
    {
        // Write two records, reopen, read first, delete it, verify only second remains
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIODEL.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixdel"
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
                MOVE "BBB" TO IX-KEY.
                MOVE "222" TO IX-VAL.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN I-O IX-FILE.
                READ IX-FILE.
                DELETE IX-FILE.
                DISPLAY WS-FS.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                READ IX-FILE.
                DISPLAY IX-KEY IX-VAL.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("00", lines[0]); // DELETE succeeded
        Assert.Equal("BBB222", lines[1]); // Only BBB remains
    }


    [Fact]
    public void FileIO_Start_PositionsForReadNext()
    {
        // Write 3 records to indexed file, START at key >= "BBB", READ NEXT
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FIOSTRT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixstart"
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
                MOVE "BBB" TO IX-KEY.
                MOVE "222" TO IX-VAL.
                WRITE IX-REC.
                MOVE "CCC" TO IX-KEY.
                MOVE "333" TO IX-VAL.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                MOVE "BBB" TO IX-KEY.
                START IX-FILE KEY IS IX-KEY.
                DISPLAY WS-FS.
                READ IX-FILE NEXT RECORD.
                DISPLAY IX-KEY IX-VAL.
                READ IX-FILE NEXT RECORD.
                DISPLAY IX-KEY IX-VAL.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("00", lines[0]); // START succeeded
        Assert.Equal("BBB222", lines[1]); // First READ NEXT after START
        Assert.Equal("CCC333", lines[2]); // Second READ NEXT
    }

    // ── INITIALIZE ──


    [Fact]
    public void FileIO_AlternateKey_ReadByAlternateKey()
    {
        // Write records with primary key (ID) and alternate key (NAME),
        // then READ by alternate key
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. ALTKEYT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IX-FILE ASSIGN TO "ixaltkey"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS IX-ID
                    ALTERNATE RECORD KEY IS IX-NAME
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD IX-FILE.
            01 IX-REC.
               05 IX-ID   PIC X(3).
               05 IX-NAME PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IX-FILE.
                MOVE "001" TO IX-ID.
                MOVE "ALICE" TO IX-NAME.
                WRITE IX-REC.
                MOVE "002" TO IX-ID.
                MOVE "BOB  " TO IX-NAME.
                WRITE IX-REC.
                MOVE "003" TO IX-ID.
                MOVE "CAROL" TO IX-NAME.
                WRITE IX-REC.
                CLOSE IX-FILE.
                OPEN INPUT IX-FILE.
                MOVE "002" TO IX-ID.
                READ IX-FILE.
                DISPLAY IX-ID IX-NAME.
                CLOSE IX-FILE.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        // READ by primary key "002" → should return "002BOB  "
        Assert.Equal("002BOB", stdout.TrimEnd());
    }

}
