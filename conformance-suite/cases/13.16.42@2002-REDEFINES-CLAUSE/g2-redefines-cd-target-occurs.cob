      *>--- conformance-case
      *> id: g2-redefines-cd-target-occurs
      *> rules: [2002-SR-13.16.42.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR6: "The data description entry for data-name-2
      *> shall not contain an OCCURS clause". In a record under an output CD,
      *> B REDEFINES A where A's own entry carries OCCURS 4; B is refused.
      *> Accepted control (data-name-2 subordinate to an OCCURS entry):
      *> g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF25.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A    PIC X OCCURS 4.
           05 B    REDEFINES A PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
