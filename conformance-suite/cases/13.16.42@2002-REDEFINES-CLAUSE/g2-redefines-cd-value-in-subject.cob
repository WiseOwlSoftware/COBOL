      *>--- conformance-case
      *> id: g2-redefines-cd-value-in-subject
      *> rules: [2002-SR-13.16.42.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR10: "Neither this entry nor any entry subordinate
      *> to it shall contain a VALUE clause, unless that VALUE clause is
      *> specified in an entry with the level-number 88." C2, the subject of a
      *> REDEFINES in a record under a CD, carries a VALUE clause and is
      *> refused. Fails if the VALUE placement check skips the communication
      *> section. Accepted control (88 under a redefinition):
      *> g2-redefines-cd-record-storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF10.
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
           05 C2 REDEFINES C1 PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
