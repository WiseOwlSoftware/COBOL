      *>--- conformance-case
      *> id: 1985-GR-XV_1_3_4-p2
      *> rules: [1985-GR-XV.1.3.4-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.1.3.4 p2 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p03: no WITH DEBUGGING MODE (1985 XV.1.3.4 p2): run
      * with the switch ON: no DBG line, as if never compiled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P03DBG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE FOR DEBUGGING ON P1 ALL REFERENCES OF W.
       D-PARA.
           DISPLAY "DBG " DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           PERFORM P1.
           MOVE 5 TO W.
           DISPLAY "W " W.
           STOP RUN.
       P1.
           DISPLAY "P1".
