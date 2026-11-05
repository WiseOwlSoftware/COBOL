      *>--- conformance-case
      *> id: pb4677-cd-filler-clause
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR6: FILLER replaces a data-name within the series only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677RG.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS FILLER.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
