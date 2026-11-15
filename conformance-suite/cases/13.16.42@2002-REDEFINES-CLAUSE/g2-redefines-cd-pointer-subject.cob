      *>--- conformance-case
      *> id: g2-redefines-cd-pointer-subject
      *> rules: [2002-SR-13.16.42.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR13: "The REDEFINES clause shall not be specified
      *> for a data item of class object or pointer or a strongly-typed group
      *> item." B, a USAGE POINTER subject of a REDEFINES in a record under a
      *> CD, is refused by the REDEFINES rule (the entry also violates the USAGE
      *> level rule §2002:13.16.58.2 SR13, a second diagnosis on the same
      *> entry). Fails if the class screen of the REDEFINES subject skips the
      *> communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF13.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(8).
           05 B REDEFINES A USAGE POINTER.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
