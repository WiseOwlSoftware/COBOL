      *>--- conformance-case
      *> id: g2-redefines-cd-pointer-object
      *> rules: [2002-SR-13.16.42.2-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR14: "Data-name-2 shall not be of class object or
      *> pointer, a strongly-typed group item, or an item subordinate to a
      *> strongly-typed group item." B REDEFINES A, where A is a USAGE POINTER
      *> item in a record under a CD, is refused by the REDEFINES rule (A also
      *> draws the USAGE level rule §2002:13.16.58.2 SR13). Fails if the class
      *> screen of data-name-2 skips the communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF15.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A USAGE POINTER.
           05 B REDEFINES A PIC X(8).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
