      *>--- conformance-case
      *> id: pb5579-rewrite-file-object-reference
      *> rules: [SR-14.9.35.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR12 "the description of identifier-1, including its subordinate data items, shall
      *> not contain a data item described with a USAGE OBJECT REFERENCE clause". Expected: COBOL7352.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N12.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N12.dat" ORGANIZATION IS SEQUENTIAL.
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
           OPEN I-O F.
           REWRITE FILE F FROM GRP.
           CLOSE F.
           STOP RUN.
