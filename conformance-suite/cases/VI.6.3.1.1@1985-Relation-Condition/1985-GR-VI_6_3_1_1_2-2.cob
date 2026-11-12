      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_2-2
      *> rules: [1985-GR-VI.6.3.1.1.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.2 GR2 (Relation Condition). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the confirmation pass (the redraft of a
      *> WEAK or WRONG record, whose filed defect was fixed; re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELGRP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G1  PIC X VALUE "0".
           05  G2  PIC X VALUE "5".
       01  N   PIC 99 VALUE 5.
       01  SN  PIC S99 SIGN IS LEADING SEPARATE CHARACTER
               VALUE -5.
       01  H.
           05  H1  PIC X(3) VALUE "-05".
       PROCEDURE DIVISION.
       P1.
           IF N = G DISPLAY "EQUAL" ELSE DISPLAY "NOT EQUAL".
           MOVE " 5" TO G.
           IF N = G DISPLAY "EQUAL" ELSE DISPLAY "NOT EQUAL".
           IF SN = H DISPLAY "SIGNED EQUAL"
               ELSE DISPLAY "SIGNED NOT EQUAL".
           MOVE "05" TO H.
           IF SN = H DISPLAY "SIGNED EQUAL"
               ELSE DISPLAY "SIGNED NOT EQUAL".
           STOP RUN.
