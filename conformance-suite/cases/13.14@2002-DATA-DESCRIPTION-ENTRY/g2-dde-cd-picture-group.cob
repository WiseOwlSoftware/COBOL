      *>--- conformance-case
      *> id: g2-dde-cd-picture-group
      *> rules: [2002-SR-13.14.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR11 - "The SYNCHRONIZED, PICTURE, JUSTIFIED,
      *> and BLANK WHEN ZERO clauses may be specified only for an
      *> elementary data item." G1 is a group written with PICTURE in a
      *> record after a CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP10.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1 PIC X(4).
              10 B1 PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
