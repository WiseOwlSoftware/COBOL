      *>--- conformance-case
      *> id: g2-dde-cd-88-index
      *> rules: [2002-SR-13.14.2-22]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR22 - a condition-name shall not be associated
      *> with e) "A data item of the class index, object, or pointer":
      *> IX is USAGE INDEX. The entry is in a record after a CD.
      *> Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP22.
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
           05 IX USAGE INDEX.
              88 IX-1 VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
