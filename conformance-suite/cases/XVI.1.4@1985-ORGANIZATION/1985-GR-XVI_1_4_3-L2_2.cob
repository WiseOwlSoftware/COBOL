      *>--- conformance-case
      *> id: 1985-GR-XVI_1_4_3-L2_2
      *> rules: [1985-GR-XVI.1.4.3-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.1.4.3 GRL2.2 (ORGANIZATION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       S50 SECTION 50.
       P1.
           ALTER P2 TO PROCEED TO P4.
           CALL "SUBPROG".
           GO TO P2.
       P2.
           GO TO P3.
       P3.
           DISPLAY "INITIAL".
           STOP RUN.
       P4.
           DISPLAY "LAST-USED".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG.
       PROCEDURE DIVISION.
       S1.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
       END PROGRAM MAINPROG.
