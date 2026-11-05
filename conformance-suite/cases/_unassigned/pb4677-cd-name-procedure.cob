      *>--- conformance-case
      *> id: pb4677-cd-name-procedure
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - cd-name-1 is a user-defined word (ISO/IEC 1989:2002 A.1.2.1): the
      *> data-name reading of PROCEDURE that NC205A needs is not a cd-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677NP.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD PROCEDURE FOR INPUT SYMBOLIC QUEUE IS Q.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
