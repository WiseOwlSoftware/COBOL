      *>--- conformance-case
      *> id: g2-redefines-cd-value-subordinate
      *> rules: [2002-SR-13.16.42.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.42.2 SR10: "Neither this entry nor any entry subordinate
      *> to it shall contain a VALUE clause, unless that VALUE clause is
      *> specified in an entry with the level-number 88." C3, subordinate to the
      *> redefining group C2 in a record under a CD, carries a VALUE clause at
      *> level 10 (not 88) and is refused. Fails if only the subject entry
      *> itself is checked. Accepted control: g2-redefines-cd-record-storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF11.
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
           05 C2 REDEFINES C1.
              10 C3 PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
