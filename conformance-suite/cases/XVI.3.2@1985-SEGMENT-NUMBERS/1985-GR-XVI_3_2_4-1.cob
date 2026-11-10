      *>--- conformance-case
      *> id: 1985-GR-XVI_3_2_4-1
      *> rules: [1985-GR-XVI.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.4 GR1 (SEGMENT-NUMBERS). An output test: the
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
       M-1.
           GO TO A1-P.
       M-2.
           DISPLAY "DONE".
           STOP RUN.
       SEG-A1 SECTION 60.
       A1-P.
           ALTER A2-SW TO PROCEED TO A2-ALT.
           GO TO A2-SW.
       MID-SEC SECTION 10.
       MID-P.
           DISPLAY "MID".
       SEG-A2 SECTION 60.
       A2-SW.
           GO TO A2-INI.
       A2-INI.
           DISPLAY "INITIAL".
           GO TO M-2.
       A2-ALT.
           DISPLAY "ALTERED".
           GO TO M-2.
