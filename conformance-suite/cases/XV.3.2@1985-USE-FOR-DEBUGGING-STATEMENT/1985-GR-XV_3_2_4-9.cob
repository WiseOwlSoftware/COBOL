      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-9
      *> rules: [1985-GR-XV.3.2.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR9 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the 1985 audit's
      *> drafts pass (a draft that mismatched on an older build, re-run
      *> to MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGQUAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANSI-85 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON S2.
       D1-P.
           DISPLAY "DEBUG S2".
       END DECLARATIVES.
       S1 SECTION.
       S1-P.
           DISPLAY "START".
           GO TO P2 OF S2.
       S2 SECTION.
       P1.
           DISPLAY "P1".
       P2.
           DISPLAY "P2".
           STOP RUN.
