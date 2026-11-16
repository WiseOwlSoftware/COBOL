      *>--- conformance-case
      *> id: g2-redefines-cd-lower-level-between
      *> rules: [2002-SR-13.16.42.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR5: "No entry having a level-number numerically
      *> lower than the level-number of data-name-2 may occur between the data
      *> description entries of data-name-2 and the subject of the entry". In
      *> a record under an output CD, the level-5 redefinition G2 REDEFINES
      *> G1, which defines no new storage area (so §2002:13.16.42.2 SR11 is
      *> not broken too), stands between C1 (level 10) and C2 REDEFINES C1
      *> (level 10); C2 is refused. Fails if the lower-level check is skipped
      *> in the communication section.
      *> Accepted control (higher levels between): g2-redefines-cd-entry-rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF24.
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
           05 G2 REDEFINES G1.
              10 C2 REDEFINES C1 PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
