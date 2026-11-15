      *>--- conformance-case
      *> id: g2-occurs-cd-key-is-table
      *> rules: [2002-SR-13.16.36.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR6 - the key item shall not contain an OCCURS
      *> clause unless it is the subject of the entry. K1 has OCCURS 2
      *> and is not TB, in a record under an OUTPUT CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 TB OCCURS 3 ASCENDING KEY IS K1.
             10 K1 PIC 9 OCCURS 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
