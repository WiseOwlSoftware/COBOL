      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-4
      *> rules: [1985-GR-XV.3.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR4 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGALL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       OBJECT-COMPUTER. X.
       DATA DIVISION.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES.
       DBG-PARA.
           IF DEBUG-NAME = "P2" DISPLAY "DEBUG P2".
           IF DEBUG-NAME = "P3" DISPLAY "DEBUG P3".
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           PERFORM P2.
           DISPLAY "BEFORE ALTER".
           ALTER P3 TO PROCEED TO P5.
           DISPLAY "AFTER ALTER".
           GO TO P3.
       P2.
           DISPLAY "IN P2".
       P3.
           GO TO P4.
       P4.
           DISPLAY "IN P4".
           STOP RUN.
       P5.
           DISPLAY "IN P5".
           STOP RUN.
