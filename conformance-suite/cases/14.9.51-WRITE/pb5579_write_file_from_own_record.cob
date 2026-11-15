      *>--- conformance-case
      *> id: pb5579_write_file_from_own_record
      *> rules: [GR-14.9.51.4-7]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2023:14.9.51.4 GR7 "For a WRITE statement with the FILE phrase in which identifier-1 references
      *> a record description entry that is associated with file-name-1, then the result of execution of the WRITE
      *> statement is equivalent to the result of execution of WRITE record-name-1, where record-name-1 is identified by
      *> identifier-1." The file has two records, R1 (10 characters) and R2 (4), sharing the record area (§2023:13.18.33.4
      *> GR3), and each is 10 bytes on the file (RECORD CONTAINS 10). R1 holds "REC1-ABCDE"; R2 redefines its first four
      *> positions. WRITE FILE F FROM R2 therefore writes what WRITE R2 does: the record area's first 10 positions, which
      *> are "REC1-ABCDE" (the bytes past R2 are the area's content, §2023:14.9.51.4 GR13 leaves them to the implementor and
      *> docs/conformance D-WRT1 states the area). EXPECTED: both records read back "REC1-ABCDE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5579OR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5579or.dat" ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD CONTAINS 10 CHARACTERS.
       01 R1 PIC X(10).
       01 R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 BUF PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "REC1-ABCDE" TO R1.
           WRITE FILE F FROM R2.
           DISPLAY "R2 " FS.
           WRITE FILE F FROM R1.
           DISPLAY "R1 " FS.
           CLOSE F.
           OPEN INPUT F.
           READ F INTO BUF.
           DISPLAY FS " [" BUF "]".
           READ F INTO BUF.
           DISPLAY FS " [" BUF "]".
           CLOSE F.
           STOP RUN.
