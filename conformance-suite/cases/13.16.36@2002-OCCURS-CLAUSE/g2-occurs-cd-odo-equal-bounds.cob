      *>--- conformance-case
      *> id: g2-occurs-cd-odo-equal-bounds
      *> rules: [2002-SR-13.16.36.2-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR15 - integer-2 shall be greater than
      *> integer-1. OCCURS 3 TO 3 in a record under an OUTPUT CD makes
      *> them equal, so the clause is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN15.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 3.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 TB PIC X OCCURS 3 TO 3 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
