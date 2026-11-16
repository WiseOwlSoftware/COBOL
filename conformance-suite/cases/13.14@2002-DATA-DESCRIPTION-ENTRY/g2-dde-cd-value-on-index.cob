      *>--- conformance-case
      *> id: g2-dde-cd-value-on-index
      *> rules: [2002-SR-13.14.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR10 - "The VALUE clause shall not be specified
      *> for data items of class index, object, or pointer." IX is
      *> USAGE INDEX with a VALUE in a record after a CD. Accepting it
      *> fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP05.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 B1 PIC X(4).
           05 IX USAGE INDEX VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
