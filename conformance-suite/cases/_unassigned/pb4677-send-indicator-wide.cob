      *>--- conformance-case
      *> id: pb4677-send-indicator-wide
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.2.6.2 SR3: a one-character integer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677O3.
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
           SEND CO FROM W1 WITH N1.
           STOP RUN.
