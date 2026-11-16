      *>--- conformance-case
      *> id: g2-redefines-cd-88
      *> rules: [2002-SR-13.16.42.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR2: the level-numbers "shall not be 66 or 88". In a
      *> record under an output CD, the level-88 entry K2 writes REDEFINES K1
      *> (also level 88) and is refused. Fails if a level-88 REDEFINES is
      *> accepted in the communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF23.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 PIC X.
              88 K1 VALUE "A".
              88 K2 REDEFINES K1 VALUE "B".
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
