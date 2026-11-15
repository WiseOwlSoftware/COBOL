      *>--- conformance-case
      *> id: g2-dde-cd-level-50
      *> rules: [2002-SR-13.14.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR1 - "Level-number may be 77 or 1 through 49."
      *> Refusal beside the accepted g2-dde-cd-record-levels: level 50
      *> in a record description entry after a CD is outside 1-49
      *> (also §2002:13.16.31.2 SR2). Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1.
              50 A50 PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
