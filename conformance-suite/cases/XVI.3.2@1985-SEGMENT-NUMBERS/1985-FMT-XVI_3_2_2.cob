      *>--- conformance-case
      *> id: 1985-FMT-XVI_3_2_2
      *> rules: [1985-FMT-XVI.3.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.2 (SEGMENT-NUMBERS). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the confirmation pass (the redraft of a WEAK or
      *> WRONG record, whose filed defect was fixed; re-run to MATCH
      *> with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGTST.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 30.
       P1.
           PERFORM FIX-SEC.
           PERFORM FIX-SEC.
           PERFORM IND-SEC.
           PERFORM IND-SEC.
           STOP RUN.
       FIX-SEC SECTION 40.
       F1.
           GO TO F2.
       F2.
           DISPLAY "SEG 40 FIRST".
           ALTER F1 TO PROCEED TO F3.
           GO TO F4.
       F3.
           DISPLAY "SEG 40 ALTERED".
       F4.
           EXIT.
       IND-SEC SECTION 60.
       I1.
           GO TO I2.
       I2.
           DISPLAY "SEG 60 FIRST".
           ALTER I1 TO PROCEED TO I3.
           GO TO I4.
       I3.
           DISPLAY "SEG 60 ALTERED".
       I4.
           EXIT.
