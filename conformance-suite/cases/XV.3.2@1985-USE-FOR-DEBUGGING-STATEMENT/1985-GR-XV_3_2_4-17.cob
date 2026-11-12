      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-17
      *> rules: [1985-GR-XV.3.2.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR17 (THE USE FOR DEBUGGING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGALT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANYCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-NAME PIC X(6).
       01  W-CONT PIC X(6).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON PARA-A.
       DBG-1.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY W-NAME " " W-CONT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           ALTER PARA-A TO PROCEED TO PARA-C.
           DISPLAY "END".
           STOP RUN.
       PARA-A.
           GO TO PARA-B.
       PARA-B.
           STOP RUN.
       PARA-C.
           STOP RUN.
