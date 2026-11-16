      *>--- conformance-case
      *> id: g2-redefines-cd-odo
      *> rules: [2002-SR-13.16.42.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR6: "Neither the original definition nor the
      *> redefinition shall include a variable-occurrence data item" (§2002:4:
      *> "A table element that is repeated a variable number of times"). In a
      *> record under an output CD, the redefinition B of A holds BT OCCURS 1
      *> TO 4 DEPENDING ON N and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF27.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 N    PIC 9.
           05 A    PIC X(4).
           05 B    REDEFINES A.
              10 BT PIC X OCCURS 1 TO 4 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
