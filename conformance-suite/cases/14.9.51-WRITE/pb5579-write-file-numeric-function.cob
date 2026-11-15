      *>--- conformance-case
      *> id: pb5579-write-file-numeric-function
      *> rules: [SR-14.9.51.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.47.2 SR10 "If identifier-1 references an intrinsic function and the FILE phrase is
      *> specified, identifier-1 shall reference an alphanumeric or national function." FUNCTION LENGTH is numeric. Expected: COBOL1914.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N07.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE FILE F FROM FUNCTION LENGTH(W10).
           CLOSE F.
           STOP RUN.
