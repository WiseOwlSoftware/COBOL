      *>--- conformance-case
      *> id: pb4677-cd-redefines-level-1
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 13.16.42 SR4: no REDEFINES at level 1 in the
      *> communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677RF.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT.
       01 R1 PIC X(87).
       01 R2 REDEFINES R1 PIC X(87).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
