      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1-p2
      *> rules: [1985-SR-IV.4.2.2.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1 p2 (Character-Strings). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * PB5453 s-99: 1985-SR-IV.4.2.2.1-p2, within one source program
      * a COBOL word may be a user-defined word and a function-name
      * (item 1b), and the context classifies each occurrence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G1WORDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MAX     PIC 9(2) VALUE 5.
       01  RESULT  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           COMPUTE RESULT = FUNCTION MAX (MAX 9 2).
           IF RESULT = 9 AND MAX = 5
               DISPLAY "WORDS-OK"
           ELSE
               DISPLAY "WORDS-BAD " RESULT " " MAX.
           STOP RUN.
