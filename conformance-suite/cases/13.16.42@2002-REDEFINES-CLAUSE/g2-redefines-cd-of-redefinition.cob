      *>--- conformance-case
      *> id: g2-redefines-cd-of-redefinition
      *> rules: [2002-SR-13.16.42.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR8: "Multiple redefinitions of the same storage
      *> area shall each specify as data-name-2 the data-name of the entry that
      *> originally defined the area". In a record under an output CD, C3
      *> names the redefinition C2 instead of the original C1 and is refused.
      *> Accepted control (C1R, C1S and FILLER all naming C1):
      *> g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF29.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 PIC X(4).
           05 C2 REDEFINES C1 PIC 9(4).
           05 C3 REDEFINES C2 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
