      *>--- conformance-case
      *> id: g2-occurs-cd-key-under-nested-table
      *> rules: [2002-SR-13.16.36.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR4 - a key subordinate to a group within the
      *> table shall not sit under a group that has an OCCURS clause. K
      *> is under G (OCCURS 2) in a record under an OUTPUT CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 T OCCURS 3 ASCENDING KEY IS K INDEXED BY IX.
             10 G OCCURS 2.
                15 K PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
