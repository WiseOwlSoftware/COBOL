      *>--- conformance-case
      *> id: pb5579-write-file-at-85
      *> rules: [FMT-14.9.51.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - the edition half of pb5579_write_file_sequential. §1985:VII.4.7.2 prints WRITE record-name-1
      *> [FROM identifier-1] with no FILE alternative; the 2002 format (14.8.47.1) adds it. Expected: COBOL0900 at 85, at FILE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N01.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE FILE F FROM W10.
           CLOSE F.
           STOP RUN.
