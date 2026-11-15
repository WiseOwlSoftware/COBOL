      *>--- conformance-case
      *> id: pb5579-write-file-no-from
      *> rules: [SR-14.9.51.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.47.2 SR7 "If the FILE phrase is specified, the FROM phrase shall also be specified"
      *> (§2023:14.9.51.3 SR7). A WRITE FILE names no record, so without FROM it releases nothing. Expected: COBOL7351.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N03.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE FILE F.
           CLOSE F.
           STOP RUN.
