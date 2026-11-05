      *>--- conformance-case
      *> id: pb4677-cd-duplicate-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.2 SR6: the data-names shall be unique
      *> within the CD entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677RC.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS S1 END KEY IS S1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
