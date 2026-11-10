      *>--- conformance-case
      *> id: 1985-GR-XV_2_1_3-1
      *> rules: [1985-GR-XV.2.1.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.2.1.3 GR1 (THE WITH DEBUGGING MODE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGMODE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       DATA DIVISION.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-A SECTION.
           USE FOR DEBUGGING ON MAIN-PARA.
       DBG-A-PARA.
           DISPLAY "DEBUG A".
       DBG-B SECTION.
           USE FOR DEBUGGING ON NEXT-PARA.
       DBG-B-PARA.
           DISPLAY "DEBUG B".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           DISPLAY "MAIN".
       NEXT-PARA.
           DISPLAY "NEXT".
           STOP RUN.
