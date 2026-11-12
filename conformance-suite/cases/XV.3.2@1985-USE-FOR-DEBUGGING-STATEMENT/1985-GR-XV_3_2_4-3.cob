      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-3
      *> rules: [1985-GR-XV.3.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR3 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGPROC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANYCOMP.
       DATA DIVISION.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-A SECTION.
           USE FOR DEBUGGING ON PARA-A.
       DBG-A-P.
           DISPLAY "DEBUG PARA-A".
       DBG-SW SECTION.
           USE FOR DEBUGGING ON SW-PARA.
       DBG-SW-P.
           DISPLAY "DEBUG SW-PARA".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "START".
           PERFORM PARA-A.
           DISPLAY "BEFORE ALTER".
           ALTER SW-PARA TO PROCEED TO PARA-C.
           DISPLAY "AFTER ALTER".
           GO TO SW-PARA.
       PARA-A.
           DISPLAY "IN PARA-A".
       SW-PARA.
           GO TO PARA-B.
       PARA-B.
           DISPLAY "IN PARA-B".
           STOP RUN.
       PARA-C.
           DISPLAY "IN PARA-C".
           STOP RUN.
