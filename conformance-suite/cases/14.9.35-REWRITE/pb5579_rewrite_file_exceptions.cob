*>--- conformance-case
*> id: pb5579_rewrite_file_exceptions
*> rules: [GR-14.9.35.4-9, GR-14.9.35.4-16, GR-14.9.35.4-21]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - the exception paths of REWRITE FILE file-name-1 FROM identifier-1 (I11): the operand IS the implicit record
*> (§2023:14.9.35.4 GR9), so its own byte count decides the status.
*> §2023:14.9.35.4 GR16 "For a record sequential file, if the number of bytes in the data item referenced by identifier-1 ... is
*> not equal to the number of bytes in the record being replaced, the execution of the REWRITE statement is unsuccessful and the
*> I-O status in the rewrite file connector is set to '44'." The sequential file holds 10-byte records: a 3-byte operand gives 44
*> and leaves the record unchanged, a 10-byte operand gives 00.
*> §2023:14.9.35.4 GR21 "If the file does not contain the record specified by the key, the invalid key condition exists" ('23'):
*> REWRITE FILE of relative record 5 of a 2-record file takes its INVALID KEY branch, and a present record takes NOT INVALID KEY.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579RX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb5579rx.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT R ASSIGN TO "pb5579rr.dat" ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM RELATIVE KEY IS RK FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 10 CHARACTERS.
FD R RECORD CONTAINS 4 CHARACTERS.
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 RK PIC 9 VALUE 0.
01 W3 PIC X(3) VALUE "ABC".
01 W10 PIC X(10) VALUE "ABCDEFGHIJ".
01 W4 PIC X(4) VALUE "WXYZ".
01 BUF PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    WRITE FILE F FROM "0123456789".
    CLOSE F.
    OPEN I-O F.
    READ F INTO BUF.
    REWRITE FILE F FROM W3.
    DISPLAY "RW3 " FS.
    CLOSE F.
    OPEN INPUT F.
    READ F INTO BUF.
    DISPLAY "[" BUF "]".
    CLOSE F.
    OPEN I-O F.
    READ F INTO BUF.
    REWRITE FILE F FROM W10.
    DISPLAY "RW10 " FS.
    CLOSE F.
    OPEN INPUT F.
    READ F INTO BUF.
    DISPLAY "[" BUF "]".
    CLOSE F.
    OPEN OUTPUT R.
    MOVE 1 TO RK.
    WRITE FILE R FROM "AAAA".
    MOVE 2 TO RK.
    WRITE FILE R FROM "BBBB".
    CLOSE R.
    OPEN I-O R.
    MOVE 5 TO RK.
    REWRITE FILE R FROM W4
        INVALID KEY DISPLAY "R5 invalid " FS
        NOT INVALID KEY DISPLAY "R5 rewritten " FS
    END-REWRITE.
    MOVE 2 TO RK.
    REWRITE FILE R FROM W4
        INVALID KEY DISPLAY "R2 invalid " FS
        NOT INVALID KEY DISPLAY "R2 rewritten " FS
    END-REWRITE.
    CLOSE R.
    STOP RUN.
