      *>--- conformance-case
      *> id: pb4677-cd-record-global
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 13.16.25.2 SR1 b): a level 1 data description entry
      *> "specified in the file, working-storage, local-storage, or linkage section".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677GL.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT.
       01 R1 GLOBAL PIC X(87).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
