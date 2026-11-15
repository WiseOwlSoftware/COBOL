      *>--- conformance-case
      *> id: pb5579-write-file-object-reference
      *> rules: [SR-14.9.51.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.47.2 SR8 "the description of identifier-1, including its subordinate data items, shall
      *> not contain a data item described with a USAGE OBJECT REFERENCE clause". Expected: COBOL7352.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N11.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N11.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       01 GRP.
          05 OBJ USAGE OBJECT REFERENCE.
          05 XX PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE FILE F FROM GRP.
           CLOSE F.
           STOP RUN.
