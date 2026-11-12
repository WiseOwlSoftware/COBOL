      *>--- conformance-case
      *> id: 1985-GR-XVI_3_2_4-3
      *> rules: [1985-GR-XVI.3.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.4 GR3 (SEGMENT-NUMBERS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the confirmation pass (the redraft of a
      *> WEAK or WRONG record, whose filed defect was fixed; re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGTST.
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       P0.
           PERFORM IND-SEC.
           PERFORM IND-SEC.
           DISPLAY "FIXED".
           STOP RUN.
       IND-SEC SECTION 50.
       P1.
           GO TO P2.
       P2.
           DISPLAY "INITIAL".
           ALTER P1 TO PROCEED TO P3.
           GO TO P4.
       P3.
           DISPLAY "LAST-USED".
       P4.
           EXIT.
