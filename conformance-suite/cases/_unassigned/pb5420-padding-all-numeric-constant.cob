      *>--- conformance-case
      *> id: pb5420-padding-all-numeric-constant
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5420 review - §2023:8.3.3.6.3 SR2: the literal-1 of ALL is not numeric; PADDING CHARACTER IS ALL K with K numeric is refused (R1-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5420N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO "PB5420N"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS ALL K.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A.
       01  REC-A PIC X(4).
       WORKING-STORAGE SECTION.
       01  K CONSTANT AS 5.
       PROCEDURE DIVISION.
           STOP RUN.
