      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-1
      *> rules: [1985-GR-IV.6.4.2.1.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR1 (Conditional Statements and Sentences).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONDST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           IF X = 1
               DISPLAY "IF-TRUE"
           ELSE
               DISPLAY "IF-FALSE".
           EVALUATE X
               WHEN 1 DISPLAY "EVAL-ONE"
               WHEN OTHER DISPLAY "EVAL-OTHER".
           STOP RUN.
