      *>--- conformance-case
      *> id: pb4677-cd-io-record-size
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR14: 33 positions for an input-output CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677R2.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR I-O.
       01 R1 PIC X(34).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
