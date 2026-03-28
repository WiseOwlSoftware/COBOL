using Xunit;

namespace CobolSharp.Tests.Integration;

public class SortMergeTests : EndToEndTestBase
{
    [Fact]
    public void SortUsing_SortsRecordsByKey()
    {
        // Write 3 records out of order, SORT them, read back in order
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSORT1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "sortinp"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "sortout"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "sortwork".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-KEY PIC X(5).
               05 IN-DATA PIC X(5).
            FD OUT-FILE.
            01 OUT-REC.
               05 OUT-KEY PIC X(5).
               05 OUT-DATA PIC X(5).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(5).
               05 SORT-DATA PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "CCCCC11111" TO IN-REC.
                WRITE IN-REC.
                MOVE "AAAAA33333" TO IN-REC.
                WRITE IN-REC.
                MOVE "BBBBB22222" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
                    USING IN-FILE
                    GIVING OUT-FILE.
                OPEN INPUT OUT-FILE.
                PERFORM READ-LOOP UNTIL WS-EOF = 1.
                CLOSE OUT-FILE.
                STOP RUN.
            READ-LOOP.
                READ OUT-FILE
                    AT END
                        MOVE 1 TO WS-EOF
                    NOT AT END
                        DISPLAY OUT-REC
                END-READ.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("AAAAA33333", lines[0]);
        Assert.Equal("BBBBB22222", lines[1]);
        Assert.Equal("CCCCC11111", lines[2]);
    }

    [Fact]
    public void SortDescending_SortsRecordsDescending()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSORT2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT IN-FILE ASSIGN TO "sortinp2"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT OUT-FILE ASSIGN TO "sortout2"
                    ORGANIZATION IS SEQUENTIAL.
                SELECT SORT-FILE ASSIGN TO "sortwork2".
            DATA DIVISION.
            FILE SECTION.
            FD IN-FILE.
            01 IN-REC.
               05 IN-KEY PIC X(3).
               05 IN-DATA PIC X(7).
            FD OUT-FILE.
            01 OUT-REC.
               05 OUT-KEY PIC X(3).
               05 OUT-DATA PIC X(7).
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(3).
               05 SORT-DATA PIC X(7).
            WORKING-STORAGE SECTION.
            01 WS-EOF PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT IN-FILE.
                MOVE "AAA1111111" TO IN-REC.
                WRITE IN-REC.
                MOVE "CCC3333333" TO IN-REC.
                WRITE IN-REC.
                MOVE "BBB2222222" TO IN-REC.
                WRITE IN-REC.
                CLOSE IN-FILE.
                SORT SORT-FILE
                    ON DESCENDING KEY SORT-KEY
                    USING IN-FILE
                    GIVING OUT-FILE.
                OPEN INPUT OUT-FILE.
                PERFORM READ-LOOP UNTIL WS-EOF = 1.
                CLOSE OUT-FILE.
                STOP RUN.
            READ-LOOP.
                READ OUT-FILE
                    AT END
                        MOVE 1 TO WS-EOF
                    NOT AT END
                        DISPLAY OUT-REC
                END-READ.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("CCC3333333", lines[0]);
        Assert.Equal("BBB2222222", lines[1]);
        Assert.Equal("AAA1111111", lines[2]);
    }

    [Fact]
    public void SortInputOutputProcedure_ReleasesAndReturns()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TSORT3.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SORT-FILE ASSIGN TO "sortwork3".
            DATA DIVISION.
            FILE SECTION.
            SD SORT-FILE.
            01 SORT-REC.
               05 SORT-KEY PIC X(5).
               05 SORT-DATA PIC X(5).
            WORKING-STORAGE SECTION.
            01 WS-REC.
               05 WS-KEY PIC X(5).
               05 WS-DATA PIC X(5).
            01 WS-DONE PIC 9 VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SORT SORT-FILE
                    ON ASCENDING KEY SORT-KEY
                    INPUT PROCEDURE IS INPUT-PROC
                    OUTPUT PROCEDURE IS OUTPUT-PROC.
                STOP RUN.
            INPUT-PROC.
                MOVE "DELTA" TO SORT-KEY.
                MOVE "44444" TO SORT-DATA.
                RELEASE SORT-REC.
                MOVE "ALPHA" TO SORT-KEY.
                MOVE "11111" TO SORT-DATA.
                RELEASE SORT-REC.
                MOVE "GAMMA" TO SORT-KEY.
                MOVE "33333" TO SORT-DATA.
                RELEASE SORT-REC.
                MOVE "BETA " TO SORT-KEY.
                MOVE "22222" TO SORT-DATA.
                RELEASE SORT-REC.
            OUTPUT-PROC.
                PERFORM RETURN-LOOP UNTIL WS-DONE = 1.
            RETURN-LOOP.
                RETURN SORT-FILE RECORD
                    INTO WS-REC
                    AT END
                        MOVE 1 TO WS-DONE
                    NOT AT END
                        DISPLAY WS-REC
                END-RETURN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("ALPHA11111", lines[0]);
        Assert.Equal("BETA 22222", lines[1]);
        Assert.Equal("DELTA44444", lines[2]);
        Assert.Equal("GAMMA33333", lines[3]);
    }
}
