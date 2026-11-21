      *>--- conformance-case
      *> id: 1985-GR-VI_7_3-p1
      *> rules: [1985-GR-VI.7.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.7.3 p1 (DEBUGGING IN THE NUCLEUS MODULE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P04BPAD.
      * 1985 VI.7.3 p1: a debugging line of spaces from margin A to
      * margin R is a blank line, so it may stand before OBJECT-COMPUTER
      * where a debugging line with text may not (VI.7.3 p5).
      D                                                                 
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ WITH DEBUGGING MODE.
       OBJECT-COMPUTER. XYZ.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BLANK DEBUG LINE ACCEPTED".
           STOP RUN.
