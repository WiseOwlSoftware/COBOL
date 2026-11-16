      *>--- conformance-case
      *> id: g2-occurs-cd-depending-not-integer
      *> rules: [2002-SR-13.16.36.2-16]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR16 - data-name-1 shall describe an integer.
      *> WNF is PIC 9V9, and the table is in a record under an OUTPUT
      *> CD, so the DEPENDING ON phrase is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WNF PIC 9V9 VALUE 3.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 TB PIC X OCCURS 1 TO 3 DEPENDING ON WNF.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
