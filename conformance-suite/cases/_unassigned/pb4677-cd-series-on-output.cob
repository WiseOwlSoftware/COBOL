      *>--- conformance-case
      *> id: pb4677-cd-series-on-output
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.1 prints the series in Formats 1 and 3 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677RB.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR OUTPUT A1 A2 A3.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
