      *>--- conformance-case
      *> id: g2-dde-cd-type-justified
      *> rules: [2002-SR-13.14.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR13 - "The TYPE clause shall not be specified
      *> in the same data description entry with any clauses except
      *> BASED, CLASS, DEFAULT, DESTINATION" and the rest of its list,
      *> which does not name JUSTIFIED. B1 writes TYPE with JUSTIFIED
      *> in a record after a CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TD IS TYPEDEF PIC X(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 B1 TYPE TD JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
           STOP RUN.
