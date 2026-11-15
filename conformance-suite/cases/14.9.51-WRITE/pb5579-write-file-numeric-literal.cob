      *>--- conformance-case
      *> id: pb5579-write-file-numeric-literal
      *> rules: [SR-14.9.51.3-7, 2002-SR-14.8.47.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.47.2 SR7 b) "literal-1 shall be an alphanumeric, boolean, or national literal and shall
      *> not be a figurative constant": a numeric literal and the figurative constant SPACE are both refused. Expected: COBOL7352.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N05.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE FILE F FROM 12.
           WRITE FILE F FROM SPACE.
           CLOSE F.
           STOP RUN.
