      *>--- conformance-case
      *> id: pb5579-write-file-sort-merge-file
      *> rules: [SR-14.9.51.3-12, 2002-SR-13.3.5.2-3, 2002-SR-13.3.5.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:13.3.5.2 SR3 "File-name-1 shall not be specified in an input-output statement" and SR4 "A
      *> record description entry associated with file-name-1 shall not be specified in an input-output statement other than
      *> following the word FROM or the word INTO" (the sort-merge file description entry): WRITE FILE S and WRITE RS are
      *> both refused. Expected: COBOL1692.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N13.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N13.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT S ASSIGN TO "P5579N13s.dat".
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
           WRITE FILE S FROM W10.
           WRITE RS FROM W10.
           STOP RUN.
