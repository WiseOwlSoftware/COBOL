      *>--- conformance-case
      *> id: 1985-GR-XV_2_1_3-2
      *> rules: [1985-GR-XV.2.1.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.2.1.3 GR2 (THE WITH DEBUGGING MODE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPX.
       OBJECT-COMPUTER. COMPX.
       DATA DIVISION.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON P1.
       DBG-PARA.
           DISPLAY "DEBUG".
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           DISPLAY "MAIN".
           STOP RUN.
