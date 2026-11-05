      *>--- conformance-case
      *> id: pb4677-cd-input-record-size
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR5: a record of an input CD
      *> "shall describe a record of exactly 87 alphanumeric character positions".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677R1.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT.
       01 R1 PIC X(86).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
