      *>--- conformance-case
      *> id: pb4677-receive-output-cd
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.2.5.2 SR1: an input or input-output CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677S6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X(5).
       01 N1 PIC 9(4).
       COMMUNICATION SECTION.
       CD CI FOR INPUT STATUS KEY IS S1.
       CD CO FOR OUTPUT STATUS KEY IS S2.
       CD CX FOR I-O STATUS KEY IS S3.
       PROCEDURE DIVISION.
       MAIN.
           RECEIVE CO MESSAGE INTO W1.
           STOP RUN.
