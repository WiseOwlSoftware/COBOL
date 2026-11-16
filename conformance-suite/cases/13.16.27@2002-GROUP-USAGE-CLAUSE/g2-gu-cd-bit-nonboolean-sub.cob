      *>--- conformance-case
      *> id: g2-gu-cd-bit-nonboolean-sub
      *> rules: [2002-SR-13.16.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.27.2 SR2 - every elementary item subordinate to a
      *> GROUP-USAGE BIT subject shall be usage bit, class and category
      *> boolean: CA PIC X(4) under CG in a CD record. Accepting it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU05.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CG GROUP-USAGE BIT.
              10 CA PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED".
           STOP RUN.
