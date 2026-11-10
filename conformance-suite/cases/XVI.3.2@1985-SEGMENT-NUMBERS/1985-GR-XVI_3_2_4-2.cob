      *>--- conformance-case
      *> id: 1985-GR-XVI_3_2_4-2
      *> rules: [1985-GR-XVI.3.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.4 GR2 (SEGMENT-NUMBERS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 0.
       P0.
           PERFORM S49.
           ALTER B1 TO PROCEED TO B3.
           PERFORM S10.
           PERFORM S49.
           STOP RUN.
       S10 SECTION 10.
       B1.
           GO TO B2.
       B2.
           DISPLAY "S10 INITIAL".
           GO TO B4.
       B3.
           DISPLAY "S10 ALTERED".
       B4.
           EXIT.
       S49 SECTION 49.
       A1.
           GO TO A2.
       A2.
           DISPLAY "S49 FIRST".
           ALTER A1 TO PROCEED TO A3.
           GO TO A4.
       A3.
           DISPLAY "S49 LATER".
       A4.
           EXIT.
