      *>--- conformance-case
      *> id: g2-gu-cd-bit-subject-usage
      *> rules: [2002-SR-13.16.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.27.2 SR2 - "A USAGE clause shall not be specified for
      *> the subject of the entry": CG, in a record after a CD, writes
      *> GROUP-USAGE BIT USAGE BIT. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CG GROUP-USAGE BIT USAGE BIT.
              10 CA PIC 1(4).
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED".
           STOP RUN.
