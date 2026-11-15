      *>--- conformance-case
      *> id: g2-redefines-cd-level-mismatch
      *> rules: [2002-SR-13.16.42.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR2: "The level-numbers of data-name-2 and the
      *> subject of the entry shall be identical". In a record under an output
      *> CD, C2 at level 5 names C1 at level 10 and is refused. No entry
      *> stands between C1 and C2, so the refusal is SR2's, not SR5's. Fails
      *> if the level-number comparison is skipped in the communication
      *> section. Accepted control: g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF22.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1.
              10 C1 PIC X(4).
           05 C2 REDEFINES C1 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
