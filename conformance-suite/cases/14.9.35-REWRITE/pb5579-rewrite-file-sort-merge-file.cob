      *>--- conformance-case
      *> id: pb5579-rewrite-file-sort-merge-file
      *> rules: [SR-14.9.35.3-11, 2002-SR-14.8.33.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR11 "File-name-1 shall not reference a report file or a sort-merge file description
      *> entry": REWRITE FILE S is refused. Expected: COBOL1692.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N14.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT S ASSIGN TO "P5579N14s.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       SD S.
       01 RS PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           REWRITE FILE S FROM W10.
           STOP RUN.
