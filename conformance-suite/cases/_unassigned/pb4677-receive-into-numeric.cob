      *>--- conformance-case
      *> id: pb4677-receive-into-numeric
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.2.5.2 SR2: category
      *> alphanumeric. A 2002 rule: X3.23-1985 prints none on
      *> identifier-1 (kb/Work PB5278), so it is refused at 2002 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677O1.
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
           RECEIVE CI MESSAGE INTO N1.
           STOP RUN.
