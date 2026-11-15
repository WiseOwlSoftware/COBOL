      *>--- conformance-case
      *> id: pb5640-rewrite-file-bit-refmod-data-length
      *> rules: [SR-14.9.35.3-7, 2002-SR-14.8.33.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5640 review R1-1. §2023:14.9.35.3 SR7 a) (and the 2002/2014 forms): a bit identifier-1 with the FILE phrase has subscripting and reference modification of only fixed-point numeric literals, the length included, at every edition, so B8(1:L) with the data-name L is refused. Expected: COBOL7353.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5640R01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5640R01.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(1).
       WORKING-STORAGE SECTION.
       01 L PIC 9(2) VALUE 8.
       01 BG.
          05 B8 PIC 1(16) BIT VALUE B"1111111100000000".
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           REWRITE FILE F FROM B8(1:L).
           CLOSE F.
           STOP RUN.
