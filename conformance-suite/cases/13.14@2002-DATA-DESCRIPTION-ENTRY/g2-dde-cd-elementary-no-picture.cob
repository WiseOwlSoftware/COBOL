      *>--- conformance-case
      *> id: g2-dde-cd-elementary-no-picture
      *> rules: [2002-SR-13.14.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR8 - "For any other entry describing an
      *> elementary item, a PICTURE clause shall be specified except as
      *> indicated in syntax rule 9." M is elementary, of usage
      *> display, with no VALUE literal (so SR9 does not apply), in a
      *> record after a CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(2).
           05 M.
       PROCEDURE DIVISION.
           STOP RUN.
