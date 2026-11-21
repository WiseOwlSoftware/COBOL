      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_7-p1
      *> rules: [1985-GR-VI.6.4.7-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.7 p1 (COMMON OPTIONS AND RULES FOR STATEMENTS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P02CLS.
      * 1985 VI.6.4.7 p1: "Except for the class condition" - a class
      * condition on content not compatible with the PICTURE is
      * defined: the NUMERIC test answers false.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  N   PIC 9(3).
       01  GX REDEFINES G PIC X(3).
       PROCEDURE DIVISION.
       P1.
           MOVE "A1B" TO GX.
           IF N NUMERIC
               DISPLAY "NUMERIC"
           ELSE
               DISPLAY "NOT NUMERIC"
           END-IF.
           MOVE "123" TO GX.
           IF N NUMERIC
               DISPLAY "NUMERIC"
           ELSE
               DISPLAY "NOT NUMERIC"
           END-IF.
           STOP RUN.
