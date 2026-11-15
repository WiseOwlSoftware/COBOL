      *>--- conformance-case
      *> id: pb5579-rewrite-file-numeric-function
      *> rules: [SR-14.9.35.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR8 "If identifier-1 references an intrinsic function and the FILE phrase is
      *> specified, identifier-1 shall reference an alphanumeric or national function." Expected: COBOL1914.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N08.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           REWRITE FILE F FROM FUNCTION LENGTH(W10).
           CLOSE F.
           STOP RUN.
