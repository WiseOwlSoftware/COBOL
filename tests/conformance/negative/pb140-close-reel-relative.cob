      *> reject-at: 2023
      *> ISO 14.9.6.3 SR1: the NO REWIND, REEL and UNIT phrases may be used
      *> only with files of sequential organization (kb/Work PB140: the old
      *> acceptance degraded to a stale FILE STATUS value at run time).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "r.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           CLOSE R UNIT
           STOP RUN.
