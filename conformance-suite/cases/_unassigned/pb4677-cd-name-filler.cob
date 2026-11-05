      *>--- conformance-case
      *> id: pb4677-cd-name-filler
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - cd-name-1 is a user-defined word (ISO/IEC 1989:2002 A.1.2.1), and
      *> FILLER is a reserved word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677NF.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD FILLER FOR INPUT SYMBOLIC QUEUE IS Q.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
