      *>--- conformance-case
      *> id: g2-dde-cd-same-as-justified
      *> rules: [2002-SR-13.14.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR12 - "The SAME AS clause shall not be
      *> specified in the same data description entry with any clauses
      *> except entry-name, EXTERNAL, GLOBAL, level-number, and
      *> OCCURS." B1 writes SAME AS with JUSTIFIED in a record after a
      *> CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WT PIC X(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 B1 SAME AS WT JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
           STOP RUN.
