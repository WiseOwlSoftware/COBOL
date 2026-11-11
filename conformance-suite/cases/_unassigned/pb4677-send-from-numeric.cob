      *>--- conformance-case
      *> id: pb4677-send-from-numeric
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.2.6.2 SR2: class
      *> alphanumeric. A 2002 rule: X3.23-1985 restricts identifier-1
      *> only as a function (kb/Work PB5278), so 2002 refuses it alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677O2.
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
           SEND CO FROM N1.
           STOP RUN.
