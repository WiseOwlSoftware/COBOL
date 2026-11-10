      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-7
      *> rules: [1985-GR-XV.3.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR7 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. XCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       01 W-A PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON P1.
       DBG-PARA.
           DISPLAY "DEBUG P1".
       DBG-A SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF W-A.
       DBG-A-PARA.
           DISPLAY "DEBUG W-A " W-A.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           ADD W-A W-A TO W-A.
           DISPLAY "ADDED".
           IF CNT = 0 MOVE 4 TO W-A MOVE 5 TO W-A.
           DISPLAY "MOVED".
           PERFORM P1 3 TIMES.
           STOP RUN.
       P1.
           ADD 1 TO CNT.
           DISPLAY "P1 " CNT.
