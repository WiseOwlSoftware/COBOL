      *>--- conformance-case
      *> id: 1985-FMT-XV_3_2_2
      *> rules: [1985-FMT-XV.3.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.2 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE FOR DEBUGGING ON P1.
       D-PARA.
           DISPLAY "DEBUG".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           PERFORM P1.
           STOP RUN.
       P1.
           DISPLAY "P1".
