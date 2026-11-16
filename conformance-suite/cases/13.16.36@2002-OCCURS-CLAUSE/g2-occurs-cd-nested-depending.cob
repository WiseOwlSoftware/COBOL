      *>--- conformance-case
      *> id: g2-occurs-cd-nested-depending
      *> rules: [2002-SR-13.16.36.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR1b - the OCCURS clause shall not be
      *> specified in a data description entry that "Has a
      *> variable-occurrence data item subordinate to it", a
      *> variable-occurrence data item being "A table element that is
      *> repeated a variable number of times" (§2002:4). In a record
      *> after an OUTPUT CD, G has OCCURS 2 TIMES and its subordinate E
      *> has OCCURS 1 TO 3 TIMES DEPENDING ON N, so G's clause is
      *> refused. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN1B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 2.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  R1.
           05 G OCCURS 2 TIMES.
              10 E PIC X OCCURS 1 TO 3 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
