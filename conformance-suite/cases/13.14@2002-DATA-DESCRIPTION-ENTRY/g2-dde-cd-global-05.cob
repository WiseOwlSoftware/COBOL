      *>--- conformance-case
      *> id: g2-dde-cd-global-05
      *> rules: [2002-SR-13.14.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR6 - "The GLOBAL clause may be specified only in
      *> data description entries whose level-number is 1."
      *> Leg: in a record after a CD, the level 5 entry G is written
      *> IS GLOBAL. (§2002:13.16.25.2 SR1 does not list a communication
      *> section record either.) The granted level 1 form is
      *> g2-dde-global-level-1. Accepting the entry fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE19.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(2) VALUE "AB".
           05 G PIC X(2) IS GLOBAL.
       PROCEDURE DIVISION.
           DISPLAY CDREC
           STOP RUN.
