      *>--- conformance-case
      *> id: g2-dde-cd-88-sync-group
      *> rules: [2002-SR-13.14.2-22]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR22 - a condition-name shall not be associated
      *> with d) "A group containing items described with a JUSTIFIED
      *> or SYNCHRONIZED clause": G1 holds B1 SYNCHRONIZED. The entry
      *> is in a record after a CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP19.
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
              88 G1-X VALUE "AB".
              10 B1 PIC X(2) SYNCHRONIZED.
       PROCEDURE DIVISION.
           STOP RUN.
