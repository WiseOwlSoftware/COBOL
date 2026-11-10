      *>--- conformance-case
      *> id: 1985-SR-XVI_3_2_3-2
      *> rules: [1985-SR-XVI.3.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.3 SR2 (SEGMENT-NUMBERS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 10.
       M-1.
           PERFORM SUB-SEC.
           ALTER S-1 TO PROCEED TO S-3.
           PERFORM SUB-SEC.
           STOP RUN.
       SUB-SEC SECTION.
       S-1.
           GO TO S-2.
       S-2.
           DISPLAY "FIRST".
           GO TO S-4.
       S-3.
           DISPLAY "ALTERED".
       S-4.
           EXIT.
