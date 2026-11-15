*>--- conformance-case
*> id: pb5579_write_file_sequential
*> rules: [FMT-14.9.51.2, SR-14.9.51.3-7, SR-14.9.51.3-10, GR-14.9.51.4-8]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - WRITE FILE file-name-1 FROM { identifier-1 | literal-1 }, the FILE alternative of the WRITE
*> statement (Annex A.4.13 item 2; required from COBOL-2002, §2002:3.1.1).
*> THE RULE (cite.py --check, each OK):
*>   §2002:14.8.47.3 GR5 "The result of execution of a WRITE statement with the FILE phrase is equivalent to the
*>     execution of the following in the order specified: MOVE identifier-1 TO implicit-record-1 or MOVE literal-1
*>     TO implicit-record-1 ... WRITE implicit-record-1 where implicit-record-1 refers to the record area for
*>     file-name-1 and is treated: a) when identifier-1 references an intrinsic function, as though implicit-record-1
*>     were a record description entry subordinate to the file description entry having the same class, category,
*>     usage, and length as the returned value of the intrinsic function, or b) when identifier-1 does not
*>     reference an intrinsic function, as though implicit-record-1 were a record description entry subordinate to
*>     the file description entry having the same description as identifier-1, or c) when literal-1 is specified, as
*>     though implicit-record-1 were a record description entry subordinate to the file description entry having
*>     the same class, category, usage, and length as literal-1" (§2023:14.9.51.4 GR8 states the same).
*> So the record released is the FROM operand under its own description, whatever the FD's record descriptions
*> are: here the FD has NONE (§2023:13.4.5.3 SR3 b) - "a FILE phrase specifying file-name-1 and the FROM phrase
*> shall be specified on all WRITE and REWRITE statements associated with the file"), and READ ... INTO (SR3 c))
*> is how the program sees the records back.
*> EXPECTED, from GR5: the file holds four 10-byte records - the 10 characters of W1, the group W2 as its
*> storage (4 DISPLAY digits then 6 characters), the 10-character literal, and the 10-character value of the
*> function UPPER-CASE. Each WRITE ends with I-O status 00 (the FILE STATUS item), and READ INTO returns them.
*> Each leg can fail: a whole-record-area write (the pre-PB5579 arm) would carry the area's stale content, not
*> the operand; a refused FILE phrase would end the compile with COBOL1706's successor, COBOL0900 at 85 only.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579WS.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb5579ws.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 10 CHARACTERS.
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 W1 PIC X(10) VALUE "ABCDEFGHIJ".
01 W2.
   05 N1 PIC 9(4) VALUE 1234.
   05 C1 PIC X(6) VALUE "XYZXYZ".
01 BUF PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    WRITE FILE F FROM W1.
    DISPLAY "W1 " FS.
    WRITE FILE F FROM W2.
    DISPLAY "W2 " FS.
    WRITE FILE F FROM "LITERAL-10".
    DISPLAY "LIT " FS.
    WRITE FILE F FROM FUNCTION UPPER-CASE("function10").
    DISPLAY "FUN " FS.
    CLOSE F.
    OPEN INPUT F.
    PERFORM 4 TIMES
        READ F INTO BUF
        DISPLAY "[" BUF "] " FS
    END-PERFORM.
    CLOSE F.
    STOP RUN.
