      *>--- conformance-case
      *> id: g2-redefines-cd-subscripted
      *> rules: [2002-SR-13.16.42.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR6: data-name-2 may be subordinate to an item
      *> whose entry contains an OCCURS clause; "In this case, the reference
      *> to data-name-2 in the REDEFINES clause shall not be subscripted". In
      *> a record under an output CD, T2 writes REDEFINES T1(1) and is
      *> refused. Accepted control (the same entry unsubscripted):
      *> g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF26.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 T OCCURS 2.
              10 T1 PIC XX.
              10 T2 REDEFINES T1(1) PIC 99.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
