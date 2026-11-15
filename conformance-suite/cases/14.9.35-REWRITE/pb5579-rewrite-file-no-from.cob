      *>--- conformance-case
      *> id: pb5579-rewrite-file-no-from
      *> rules: [SR-14.9.35.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR10 "If the FILE phrase is specified, the FROM phrase shall also be specified"
      *> (§2023:14.9.35.3 SR10). Expected: COBOL7351.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N04.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           REWRITE FILE F.
           CLOSE F.
           STOP RUN.
