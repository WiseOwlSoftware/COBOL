      *>--- conformance-case
      *> id: pb4677-cd-initial-twice
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR2: INITIAL in only one CD entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677R8.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INITIAL INPUT STATUS KEY IS S1.
       CD C2 FOR INITIAL I-O STATUS KEY IS S2.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
