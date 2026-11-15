*>--- conformance-case
*> id: pb5579_write_file_exceptions
*> rules: [GR-14.9.51.4-8, GR-14.9.51.4-14, GR-14.9.51.4-3]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - the exception paths of WRITE FILE file-name-1 FROM { identifier-1 | literal-1 } (I11): the operand IS the
*> implicit record (§2023:14.9.51.4 GR8), so its own length decides the status.
*> §2023:14.9.51.4 GR14 "The number of bytes in the runtime representation of literal-1, the data item referenced by
*> identifier-1, or the record referenced by record-name-1 ... shall not be larger than the largest or smaller than the smallest
*> number of bytes allowed by the RECORD IS VARYING clause ... If this rule is violated, the execution of the WRITE statement is
*> unsuccessful and the I-O status in the write file connector is set to '44'": a RECORD VARYING FROM 5 TO 10 file takes a
*> 3-byte operand (44), a 12-byte operand (44) and a 7-byte literal (00). A file not open is status 48 (GR3), and its USE
*> AFTER STANDARD EXCEPTION procedure runs. A duplicate relative key is the invalid key condition ('22') and takes INVALID KEY.
*> A zero-length literal on a file whose minimum is 0 writes a length-0 record (GR14), and a boolean literal is written as its
*> character positions (§2023:13.18.24.4 GR2c: PIC 1(n)): "0101".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579WX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT V ASSIGN TO "pb5579wv.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT Z ASSIGN TO "pb5579wz.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT R ASSIGN TO "pb5579wr.dat" ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM RELATIVE KEY IS RK FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD V RECORD VARYING FROM 5 TO 10 CHARACTERS.
FD Z RECORD VARYING FROM 0 TO 10 CHARACTERS.
FD R RECORD CONTAINS 4 CHARACTERS.
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 RK PIC 9 VALUE 1.
01 W3 PIC X(3) VALUE "ABC".
01 W12 PIC X(12) VALUE "ABCDEFGHIJKL".
01 BUF PIC X(10).
PROCEDURE DIVISION.
DECLARATIVES.
CLOSED-USE SECTION.
    USE AFTER STANDARD EXCEPTION PROCEDURE ON V.
CLOSED-PARA.
    DISPLAY "declarative " FS.
END DECLARATIVES.
MAIN SECTION.
MAIN-P.
    OPEN OUTPUT V.
    WRITE FILE V FROM W3.
    DISPLAY "V3 " FS.
    WRITE FILE V FROM W12.
    DISPLAY "V12 " FS.
    WRITE FILE V FROM "SEVENCH".
    DISPLAY "V7 " FS.
    CLOSE V.
    WRITE FILE V FROM "SEVENCH".
    DISPLAY "CLOSED " FS.
    OPEN OUTPUT R.
    WRITE FILE R FROM "AAAA".
    WRITE FILE R FROM "BBBB"
        INVALID KEY DISPLAY "R dup " FS
        NOT INVALID KEY DISPLAY "R new " FS
    END-WRITE.
    CLOSE R.
    OPEN OUTPUT Z.
    WRITE FILE Z FROM "".
    DISPLAY "Z0 " FS.
    WRITE FILE Z FROM B"0101".
    DISPLAY "ZB " FS.
    CLOSE Z.
    OPEN INPUT Z.
    MOVE ALL "*" TO BUF.
    READ Z INTO BUF.
    DISPLAY "[" BUF "]".
    MOVE ALL "*" TO BUF.
    READ Z INTO BUF.
    DISPLAY "[" BUF "]".
    CLOSE Z.
    STOP RUN.
