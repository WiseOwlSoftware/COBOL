      *>--- conformance-case
      *> id: g2-renames-cd-bit-fraction
      *> rules: [2002-SR-13.16.43.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR9 - "The area described by data-name-2 THROUGH
      *> data-name-3 shall define an integral number of bytes." B1 THRU B2
      *> spans two one-bit USAGE BIT items, 2 bits, under a CD: refused.
      *> cite.py --check of the quoted rule: OK. Refused at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RENN9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  A.
           05 B1 PIC 1 USAGE BIT.
           05 B2 PIC 1 USAGE BIT.
           05 A2 PIC X(2).
       66  R RENAMES B1 THRU B2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
