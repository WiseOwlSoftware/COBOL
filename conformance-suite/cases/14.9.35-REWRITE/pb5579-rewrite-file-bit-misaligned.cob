      *>--- conformance-case
      *> id: pb5579-rewrite-file-bit-misaligned
      *> rules: [SR-14.9.35.3-7, 2002-SR-14.8.33.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR7 b) "it is aligned on a byte boundary": B2 starts at bit 3 of BT. Expected: COBOL7353.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N10.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N10.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       01 BT.
          05 B1 PIC 1(3) BIT.
          05 B2 PIC 1(8) BIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           REWRITE FILE F FROM B2.
           CLOSE F.
           STOP RUN.
