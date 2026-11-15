      *>--- conformance-case
      *> id: g2-lvl-cd-entry-without-level
      *> rules: [2002-SR-13.16.31.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.31.2 SR1 - "A level-number is required as the first
      *> element in each data description or screen description entry."
      *> A record description entry after a CD (a data description
      *> entry, §2002:A.1.1) written with no level-number. Accepting it
      *> fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LVL01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
           CDREC PIC X(10).
       PROCEDURE DIVISION.
           STOP RUN.
