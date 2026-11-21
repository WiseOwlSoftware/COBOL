      *>--- conformance-case
      *> id: 1985-GR-XV_1_2-p1
      *> rules: [1985-GR-XV.1.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.1.2 p1 (INTRODUCTION TO THE DEBUG MODULE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p01: Debug level 1 selective procedure monitoring
      * (1985 XV.1.2 p1). Only P1 is monitored; P2 is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01DBG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE FOR DEBUGGING ON P1.
       D-PARA.
           DISPLAY "DBG " DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           PERFORM P1.
           PERFORM P2.
           PERFORM P1.
           STOP RUN.
       P1.
           DISPLAY "P1".
       P2.
           DISPLAY "P2".
