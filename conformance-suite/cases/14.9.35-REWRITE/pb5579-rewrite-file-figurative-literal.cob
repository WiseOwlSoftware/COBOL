      *>--- conformance-case
      *> id: pb5579-rewrite-file-figurative-literal
      *> rules: [SR-14.9.35.3-10, 2002-SR-14.8.33.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR10 b) "literal-1 shall be an alphanumeric, boolean, or national literal and shall
      *> not be a figurative constant": ALL "AB" and a numeric literal are refused. Expected: COBOL7352.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N06.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           REWRITE FILE F FROM ALL "AB".
           REWRITE FILE F FROM 7.
           CLOSE F.
           STOP RUN.
