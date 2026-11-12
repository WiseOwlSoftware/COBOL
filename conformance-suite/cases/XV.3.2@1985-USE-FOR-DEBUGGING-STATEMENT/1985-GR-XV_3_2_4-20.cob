      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-20
      *> rules: [1985-GR-XV.3.2.4-20]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR20 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGPRF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(6).
       01  WS-CONT PIC X(12).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON PARA-A.
       DBG-1.
           MOVE DEBUG-NAME TO WS-NAME.
           MOVE DEBUG-CONTENTS TO WS-CONT.
           DISPLAY WS-NAME.
           DISPLAY WS-CONT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           PERFORM PARA-A.
           DISPLAY "DONE".
           STOP RUN.
       PARA-A.
           DISPLAY "IN PARA-A".
