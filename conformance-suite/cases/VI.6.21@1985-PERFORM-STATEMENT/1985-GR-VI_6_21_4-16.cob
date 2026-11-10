      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-16
      *> rules: [1985-GR-VI.6.21.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR16 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGPRF.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M1.
           GO TO I1.
       FIX-SEC SECTION 10.
       F1.
           DISPLAY "FIXED".
       IND-SEC SECTION 50.
       I1.
           PERFORM FIX-SEC.
           PERFORM I2.
           STOP RUN.
       I2.
           DISPLAY "INDEP".
