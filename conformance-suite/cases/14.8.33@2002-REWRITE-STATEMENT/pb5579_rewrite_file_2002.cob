*>--- conformance-case
*> id: pb5579_rewrite_file_2002
*> rules: [2002-SR-14.8.33.2-10, 2002-GR-14.8.33.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - (the 2002 twin of pb5579_rewrite_file: one program, the 2002 rules it witnesses) - REWRITE FILE file-name-1 [RECORD] FROM { identifier-1 | literal-1 }, the FILE alternative of the
*> REWRITE statement (Annex A.4.13 item 1; required from COBOL-2002, §2002:3.1.1), on a sequential and an indexed file.
*> THE RULE (cite.py --check, each OK):
*>   §2002:14.8.33.3 GR8 "The result of execution of a REWRITE statement with the FILE phrase is equivalent to the
*>     execution of the following in the order specified: - The statement MOVE identifier-1 TO implicit-record-1 or
*>     MOVE literal-1 TO implicit-record-1 - The statement REWRITE implicit-record-1 where implicit-record-1 refers
*>     to the record area for file-name-1 and is treated: ... b) when identifier-1 does not reference an intrinsic
*>     function, as though implicit-record-1 were a record description entry subordinate to the file description
*>     entry having the same description as identifier-1, or c) when literal-1 is specified, as though
*>     implicit-record-1 were a record description entry subordinate to the file description entry having the same
*>     class, category, usage, and length as literal-1" (§2023:14.9.35.4 GR9).
*>   §2002:14.8.33.2 SR10 "If the FILE phrase is specified, the FROM phrase shall also be specified".
*> EXPECTED: the sequential file (an FD with no record description entries, RECORD CONTAINS 10, so only the FILE form
*> binds, §2023:13.4.5.3 SR3 b)) holds "OLD-RECORD", "SECOND-REC". After READ of the first record, REWRITE FILE FROM the
*> group W (4 digits "0042" then 6 characters "NEWREC") replaces it with the 10 bytes "0042NEWREC"; the second record is
*> rewritten FROM a 10-character literal "LIT-REWRIT". Read back: "0042NEWREC", "LIT-REWRIT". The indexed file's 9-byte
*> record "KEY-OLD-1" (prime key "KEY") is read by key and rewritten FROM W9, "KEY-NEW-9": the key is unchanged, so the
*> rewrite is valid, and the record read back is "KEY-NEW-9". Every REWRITE ends with status 00. Each leg can fail: a
*> REWRITE of the record area instead of the operand would leave the old content in place.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579X2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb5579rw2.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT FI ASSIGN TO "pb5579ri2.dat" ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC RECORD KEY IS KEY1 FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 10 CHARACTERS.
FD FI.
01 RI.
   05 KEY1 PIC X(3).
   05 DAT1 PIC X(6).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 W.
   05 N1 PIC 9(4) VALUE 42.
   05 C1 PIC X(6) VALUE "NEWREC".
01 W9 PIC X(9) VALUE "KEY-NEW-9".
01 BUF PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    WRITE FILE F FROM "OLD-RECORD".
    WRITE FILE F FROM "SECOND-REC".
    CLOSE F.
    OPEN I-O F.
    READ F INTO BUF.
    REWRITE FILE F FROM W.
    DISPLAY "RW1 " FS.
    READ F INTO BUF.
    REWRITE FILE F RECORD FROM "LIT-REWRIT".
    DISPLAY "RW2 " FS.
    CLOSE F.
    OPEN INPUT F.
    READ F INTO BUF.
    DISPLAY "[" BUF "]".
    READ F INTO BUF.
    DISPLAY "[" BUF "]".
    CLOSE F.
    OPEN OUTPUT FI.
    WRITE RI FROM "KEY-OLD-1".
    CLOSE FI.
    OPEN I-O FI.
    MOVE "KEY" TO KEY1.
    READ FI.
    REWRITE FILE FI FROM W9
        INVALID KEY DISPLAY "FI invalid " FS
        NOT INVALID KEY DISPLAY "FI rewritten " FS
    END-REWRITE.
    MOVE "KEY" TO KEY1.
    READ FI.
    DISPLAY "FI read [" RI "]".
    CLOSE FI.
    STOP RUN.
