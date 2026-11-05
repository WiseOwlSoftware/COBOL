      *>--- conformance-case
      *> id: pb4677-cd-name-reserved
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - cd-name-1 is a user-defined word, so a word ISO 8.9 reserves draws
      *> the funnel: DESTINATION is a keyword word that rides cobolWord.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677NR.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD DESTINATION FOR INPUT SYMBOLIC QUEUE IS Q.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
