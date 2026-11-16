      *>--- conformance-case
      *> id: g2-redefines-cd-qualified
      *> rules: [2002-SR-13.16.42.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR7: "Data-name-2 shall not be qualified". In a
      *> record under an output CD, B writes REDEFINES A OF G and is refused.
      *> Accepted control (a non-unique data-name-2 written bare):
      *> g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF28.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G.
              10 A    PIC X(4).
              10 B    REDEFINES A OF G PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
