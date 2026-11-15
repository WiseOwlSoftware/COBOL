      *>--- conformance-case
      *> id: g2-redefines-cd-strong-object
      *> rules: [2002-SR-13.16.42.2-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR14: "Data-name-2 shall not be of class object or
      *> pointer, a strongly-typed group item, or an item subordinate to a
      *> strongly-typed group item." H REDEFINES G, where G is typed by the
      *> strong type ST in a record under a CD, is refused. Fails if a
      *> strongly-typed data-name-2 is admitted in the communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05 S1 PIC XX.
           05 S2 PIC XX.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G TYPE ST.
           05 H REDEFINES G PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
