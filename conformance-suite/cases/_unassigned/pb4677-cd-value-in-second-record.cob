      *>--- conformance-case
      *> id: pb4677-cd-value-in-second-record
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR5: "only the first redefinition
      *> may contain VALUE clauses".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677R3.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT.
       01 R1 PIC X(87).
       01 R2.
          02 F1 PIC X(87) VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
