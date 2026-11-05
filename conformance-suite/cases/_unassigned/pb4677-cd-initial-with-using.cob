      *>--- conformance-case
      *> id: pb4677-cd-initial-with-using
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR2: not with a USING phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677R9.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC X.
       COMMUNICATION SECTION.
       CD C1 FOR INITIAL INPUT STATUS KEY IS S1.
       PROCEDURE DIVISION USING L1.
       MAIN.
           GOBACK.
