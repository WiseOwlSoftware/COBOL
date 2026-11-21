      *>--- conformance-case
      *> id: 1985-SR-VI_7_3-p4
      *> rules: [1985-SR-VI.7.3-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3 p4 (DEBUGGING IN THE NUCLEUS MODULE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P05SUC.
      * 1985 VI.7.3 p4: successive debugging lines are allowed; with
      * the mode on all three run, the last statement spans two.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ WITH DEBUGGING MODE.
       OBJECT-COMPUTER. XYZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "D1".
      D    DISPLAY "D2".
      D    ADD 1
      D        TO A.
           DISPLAY "A=" A.
           STOP RUN.
