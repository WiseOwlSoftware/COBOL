      *>--- conformance-case
      *> id: pb4677-cd-series-length
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.1: Format 1's series prints eleven data-names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677RA.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT A1 A2 A3.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
