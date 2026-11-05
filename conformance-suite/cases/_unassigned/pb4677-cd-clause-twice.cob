      *>--- conformance-case
      *> id: pb4677-cd-clause-twice
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.1 prints each clause once.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677R5.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS S1 STATUS KEY IS S2.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
