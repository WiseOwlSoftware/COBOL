      *>--- conformance-case
      *> id: pb4677-facility-removed-2014
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - the communication facility is not part of COBOL-2014 or 2023
      *> (communication-facility-removed-2014); the 2002 text says it is to be deleted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677X1.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS S1.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT C1 MESSAGE COUNT.
           STOP RUN.
