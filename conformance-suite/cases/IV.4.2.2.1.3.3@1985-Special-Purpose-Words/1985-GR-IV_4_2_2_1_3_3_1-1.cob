      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_3_3_1-1
      *> rules: [1985-GR-IV.4.2.2.1.3.3.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3.3.1 GR1 (Special Purpose Words). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.1.3.3.1-1: DEBUG-ITEM special register.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P10DEBUG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. MY-COMPUTER WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON P-TWO.
       DBG-1.
           DISPLAY "DBG-NAME=" DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P-ONE.
           PERFORM P-TWO.
           STOP RUN.
       P-TWO.
           DISPLAY "IN-P-TWO".
