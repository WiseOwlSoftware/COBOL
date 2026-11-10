      *>--- conformance-case
      *> id: 1985-GR-XVI_2_3_4-3
      *> rules: [1985-GR-XVI.2.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.3.4 GR3 (THE SEGMENT-LIMIT CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGLIM.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           PERFORM S49.
           PERFORM S49.
           STOP RUN.
       S49 SECTION 49.
       A1.
           GO TO A2.
       A2.
           DISPLAY "FIRST".
           ALTER A1 TO PROCEED TO A3.
           GO TO A4.
       A3.
           DISPLAY "SECOND".
       A4.
           EXIT.
